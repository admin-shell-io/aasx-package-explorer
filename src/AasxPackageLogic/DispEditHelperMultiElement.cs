﻿/*
Copyright(c) 2018 - 2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0_RC02;
using AasxIntegrationBase;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AdminShellNS.Display;
using AnyUi;
using Extensions;

namespace AasxPackageLogic
{
    /// <summary>
    /// This class extends the basic helper functionalities of DispEditHelper for multi element
    /// handling.
    /// </summary>
    public class DispEditHelperMultiElement : DispEditHelperEntities
    {
        public void DispMultiElementCutCopyPasteHelper(
            AnyUiPanel stack,
            ModifyRepo repo,
            Aas.Environment env,
            Aas.IClass parentContainer,
            CopyPasteBuffer cpb,
            ListOfVisualElementBasic entities,
            string label = "Buffer:",
            AasxMenu superMenu = null)
        {
            // access
            if (parentContainer == null || cpb == null || entities == null)
                return;

            // use an action
            this.AddActionPanel(
                stack, label,
                repo: repo, superMenu: superMenu,
                ticketMenu: new AasxMenu()
                    .AddAction("aas-multi-elem-cut", "Cut",
                        "Removes the currently selected element and places it in the paste buffer.",
                        inputGesture: "Ctrl+X")
                    .AddAction("aas-multi-elem-copy", "Copy",
                        "Places the currently selected element in the paste buffer.",
                        inputGesture: "Ctrl+C"),
                ticketAction: (buttonNdx, ticket) =>
                {
                    if (buttonNdx == 0 || buttonNdx == 1)
                    {
                        // store info
                        cpb.Clear();
                        cpb.Valid = true;
                        cpb.Duplicate = buttonNdx == 1;

                        cpb.Items = new ListOfCopyPasteItem();
                        foreach (var el in entities)
                        {
                            if (el is VisualElementSubmodelElement vesme
                                && parentContainer is Aas.IReferable pcref)
                            {
                                var sme = vesme.theWrapper;
                                EnumerationPlacmentBase placement = null;
                                //if (parentContainer is IEnumerateChildren enc)
                                placement = pcref.GetChildrenPlacement(sme);
                                cpb.Items.Add(new CopyPasteItemSME(env, pcref,
                                    vesme.theWrapper, sme, placement));
                            }

                            if (el is VisualElementSubmodelRef vesmr)
                                cpb.Items.Add(new CopyPasteItemSubmodel(parentContainer, vesmr.theSubmodelRef,
                                    vesmr.theSubmodelRef, vesmr.theSubmodel));

                            if (el is VisualElementSubmodel vesm)
                                cpb.Items.Add(new CopyPasteItemSubmodel(parentContainer, vesm.theSubmodel,
                                    null, vesm.theSubmodel));

                            if (el is VisualElementOperationVariable veopv
                                && parentContainer is Aas.IReferable pcref2)
                                if (veopv.theOpVar?.Value != null)
                                    cpb.Items.Add(new CopyPasteItemSME(env, pcref2,
                                        veopv.theOpVar.Value, veopv.theOpVar.Value));

                            if (el is VisualElementConceptDescription vecd)
                                cpb.Items.Add(new CopyPasteItemIdentifiable(parentContainer, vecd.theCD));

                            //if (el is VisualElementAsset veass)
                            //    cpb.Items.Add(new CopyPasteItemIdentifiable(parentContainer, veass.theAsset));

                            if (el is VisualElementAdminShell veaas)
                                cpb.Items.Add(new CopyPasteItemIdentifiable(parentContainer, veaas.theAas));
                        }

                        // corner case
                        if (cpb.Items.Count < 1)
                        {
                            Log.Singleton.Error("When cut/ copy multiple elements to paste buffer, all single " +
                                "elements do nat match the overall type");
                            return new AnyUiLambdaActionNone();
                        }

                        // prepare clipboard
                        cpb.CopyToClipboard(context, cpb.Watermark);

                        // user feedback
                        Log.Singleton.Info(
                            StoredPrint.Color.Blue,
                            "Stored {0} AAS elements to internal buffer.{1}", cpb.Items.Count,
                            cpb.Duplicate
                                ? " Paste will duplicate."
                                : " Paste will cut at original position.");
                    }

                    return new AnyUiLambdaActionNone();
                });
        }

        /// <summary>
        /// This is a special helper for multiple selected entities in a list of entoties of type <c>T</c>.
        /// It is separate from <c>EntityListUpDownDeleteHelper</c> in order to minimize cross effecrs
        /// </summary>
        public void EntityListMultipleUpDownDeleteHelper<T>(
            AnyUiPanel stack, ModifyRepo repo,
            List<T> list, List<T> entities, ListOfVisualElementBasic.IndexInfo indexInfo,
            object alternativeFocus = null, string label = "Entities:",
            PackCntChangeEventData sendUpdateEvent = null, bool preventMove = false, bool reFocus = false,
            AasxMenu superMenu = null)
        {
            // access
            if (entities == null || indexInfo == null || list == null)
                return;

            // ask
            AddActionPanel(
                stack, label,
                repo: repo,
                superMenu: superMenu,
                ticketMenu: new AasxMenu()
                    .AddAction("aas-multi-elem-move-up", "Move up",
                        "Moves the currently selected element up in containing collection.",
                        inputGesture: "Shift+Ctrl+Up")
                    .AddAction("aas-multi-elem-move-down", "Move down",
                        "Moves the currently selected element down in containing collection.",
                        inputGesture: "Shift+Ctrl+Down")
                    .AddAction("aas-multi-elem-move-top", "Move top",
                        "Moves the currently selected element to the top in containing collection.",
                        inputGesture: "Shift+Ctrl+Home")
                    .AddAction("aas-multi-elem-move-end", "Move end",
                        "Moves the currently selected element to the end in containing collection.",
                        inputGesture: "Shift+Ctrl+End")
                    .AddAction("aas-multi-elem-delete", "Delete",
                        "Deletes the currently selected element.",
                        inputGesture: "Ctrl+Shift+Delete"),
                ticketAction: (buttonNdx, ticket) =>
                {
                    if (buttonNdx >= 0 && buttonNdx <= 3)
                    {
                        // special sort order
                        if (preventMove)
                        {
                            this.context.MessageBoxFlyoutShow(
                                "Moving within list is not possible, as list of entities has dynamic " +
                                "sort order.",
                                "Move entities", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);
                            return new AnyUiLambdaActionNone();
                        }

                        // get more informations

                        var newndx = -1;
                        if (buttonNdx == 0)
                            newndx = MoveElementsToStartingIndex<T>(list, entities, indexInfo.MinIndex - 1);

                        if (buttonNdx == 1)
                            newndx = MoveElementsToStartingIndex<T>(list, entities, indexInfo.MinIndex + 1);

                        if (buttonNdx == 2)
                            newndx = MoveElementsToStartingIndex<T>(list, entities, 0);

                        if (buttonNdx == 3)
                            newndx = MoveElementsToStartingIndex<T>(list, entities, int.MaxValue);

                        if (newndx >= 0)
                        {
                            if (sendUpdateEvent != null)
                            {
                                sendUpdateEvent.Reason = PackCntChangeEventReason.StructuralUpdate;
                                sendUpdateEvent.DisableSelectedTreeItemChange = true;
                                var la1 = new AnyUiLambdaActionPackCntChange(sendUpdateEvent);
                                var la2 = new AnyUiLambdaActionSelectMainObjects((IEnumerable<object>)entities);
                                return new AnyUiLambdaActionList(la1, la2);
                            }
                            else
                            {
                                object fo = null;
                                if (reFocus && newndx < list.Count)
                                    fo = list[newndx];
                                if (alternativeFocus != null)
                                    fo = alternativeFocus;
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: fo, isExpanded: null);
                            }
                        }
                        else
                            return new AnyUiLambdaActionNone();
                    }

                    if (buttonNdx == 4)

                        if (this.context.ActualShiftState
                            || AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                "Delete selected entities? This operation can not be reverted!", "AAS-ENV",
                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                        {
                            DeleteElementsInList<T>(list, entities);
                            if (sendUpdateEvent != null)
                            {
                                sendUpdateEvent.Reason = PackCntChangeEventReason.StructuralUpdate;
                                return new AnyUiLambdaActionPackCntChange(sendUpdateEvent);
                            }
                            else
                                return new AnyUiLambdaActionRedrawAllElements(
                                    nextFocus: alternativeFocus, isExpanded: null);
                        }

                    return new AnyUiLambdaActionNone();
                });
        }

        public string PerformWildcardReplace(string input, string pattern)
        {
            // access
            if (input == null || pattern == null)
                return input;

            // pop stack principle
            var res = "";
            while (pattern.Length > 0)
            {
                // pop
                var p0 = pattern[0];
                pattern = pattern.Remove(0, 1);

                // special case
                var metaChar = false;

                if (p0 == '*')
                {
                    metaChar = true;
                    res += input;
                    input = "";
                }

                if (p0 == '^')
                {
                    metaChar = true;
                    res += input.ToUpper();
                    input = "";
                }

                if (p0 == '§')
                {
                    metaChar = true;
                    res += input.ToLower();
                    input = "";
                }

                // only with input
                if (input.Length > 0)
                {
                    if (p0 == '?')
                    {
                        metaChar = true;
                        res += input[0];
                        input = input.Remove(0, 1);
                    }

                    if (p0 == '~')
                    {
                        metaChar = true;
                        input = input.Remove(0, 1);
                    }

                    if (p0 == '<')
                    {
                        metaChar = true;
                        input = input.Reverse().ToString();
                    }
                }

                // ordinary?
                if (!metaChar)
                    res += p0;
            }

            // ok
            return res;
        }

        public void ChangeElementAttributes(Aas.IClass el, AnyUiDialogueDataChangeElementAttributes dia)
        {
            // access
            if (el == null || dia == null)
                return;

            if (dia.AttributeToChange == AnyUiDialogueDataChangeElementAttributes.AttributeEnum.IdShort &&
                el is Aas.IReferable rf1)
            {
                rf1.IdShort = PerformWildcardReplace(rf1.IdShort, dia.Pattern);
            }

            if (dia.AttributeToChange == AnyUiDialogueDataChangeElementAttributes.AttributeEnum.Description &&
                el is Aas.IReferable rf2)
            {
                //var input = (rf2.Description?.LangStrings == null) ? "" : rf2.Description.LangStrings[dia.AttributeLang];
                var rf2LangString = rf2.Description.Where(s => s.Language.Equals(dia.AttributeLang)).First();
                var input = (rf2.Description == null) ? "" : rf2LangString.Text;
                var nd = PerformWildcardReplace(input, dia.Pattern);
                if (nd != null)
                {
                    if (rf2.Description == null)
                        rf2.Description = new List<Aas.LangString>();
                    rf2LangString.Text = nd;
                }
            }

            if (dia.AttributeToChange == AnyUiDialogueDataChangeElementAttributes.AttributeEnum.ValueText &&
                el is Aas.ISubmodelElement sme)
            {
                var nd = PerformWildcardReplace(sme.ValueAsText(dia.AttributeLang), dia.Pattern);
                if (nd != null)
                    sme.ValueFromText(nd);
            }
        }

        /// <summary>
        /// This is a special helper for multiple selected entities in a list of entoties of type <c>T</c>.
        /// It is separate from <c>EntityListUpDownDeleteHelper</c> in order to minimize cross effecrs
        /// </summary>
        public void EntityListSupplementaryFileHelper(
            AnyUiPanel stack, ModifyRepo repo,
            ListOfVisualElementBasic entities,
            object alternativeFocus = null, string label = "Entities:",
            AasxMenu superMenu = null)
        {
            // access
            if (entities == null)
                return;

            // ask
            AddActionPanel(
                stack, label,
                repo: repo, superMenu: superMenu,
                ticketMenu: new AasxMenu()
                    .AddAction("aas-multi-elem-del", "Delete",
                        "Deletes currently selected element(s).",
                        inputGesture: "Ctrl+Shift+Delete"),
                ticketAction: (buttonNdx, ticket) =>
                {
                    if (buttonNdx == 0)

                        if (this.context.ActualShiftState
                            || AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                "Delete selected entities? This operation can not be reverted and may directly " +
                                "affect the loaded package!", "AAS-ENV",
                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                        {
                            // single deletes
                            int num = 0;
                            foreach (var ent in entities)
                                if (ent is VisualElementSupplementalFile vesf && vesf.theFile != null)
                                {
                                    try
                                    {
                                        packages.Main.DeleteSupplementaryFile(vesf.theFile);
                                        num++;
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Singleton.Error(ex, "Deleting file in package");
                                    }
                                }

                            // further infos
                            Log.Singleton.Info(
                                $"Added {num} items to pending package items to be deleted. " +
                                "A save-operation might be required.");

                            // finalize
                            return new AnyUiLambdaActionRedrawAllElements(
                                nextFocus: alternativeFocus, isExpanded: true);
                        }

                    return new AnyUiLambdaActionNone();
                });
        }

        public void DisplayOrEditAasEntityMultipleElements(
            PackageCentral.PackageCentral packages,
            ListOfVisualElementBasic entities,
            bool editMode, AnyUiStackPanel stack,
            VisualElementEnvironmentItem.ConceptDescSortOrder? cdSortOrder = null,
            AasxMenu superMenu = null)
        {
            //
            // View
            //
            this.AddGroup(stack, "Multiple selected elements", this.levelColors.MainSection);

            if (editMode)
            {
                // any elements
                if (entities == null || entities.Count < 1)
                {
                    this.AddHintBubble(
                        stack, hintMode,
                        new HintCheck(() => true,
                        "No selected elements available. Select AAS elements in the hierarchical tree.",
                        severityLevel: HintCheck.Severity.High));
                    return;
                }

                // consistent information
                var sameParent = entities.AllWithSameParent();
                var indexInfo = entities.GetIndexedParentInfo();

                if (!sameParent)
                {
                    this.AddHintBubble(
                        stack, hintMode,
                        new HintCheck(() => true,
                        "Selected AAS elements do not share the same parent. This is a precondition for further " +
                        "editor functionality.",
                        severityLevel: HintCheck.Severity.High));
                    return;
                }

                if (indexInfo == null)
                {
                    this.AddHintBubble(
                        stack, hintMode,
                        new HintCheck(() => true,
                        "Could not determine index information for selected AAS elements. Aborting.",
                        severityLevel: HintCheck.Severity.High));
                    return;
                }

                // which type?
                var first = entities.First();
                Aas.IClass parent = indexInfo.SharedParent.GetDereferencedMainDataObject()
                                                as Aas.IClass;

                // TODO (MIHO, 2021-07-08): check for completeness
                if (first is VisualElementSubmodel vesm)
                {
                    // up down delete
                    var bos = entities.GetListOfBusinessObjects<Aas.Submodel>();
                    EntityListMultipleUpDownDeleteHelper(stack, repo,
                        vesm.theEnv?.Submodels, bos, indexInfo, reFocus: true, superMenu: superMenu);

                    // cut copy
                    DispMultiElementCutCopyPasteHelper(stack, repo, vesm.theEnv, parent, this.theCopyPaste,
                        entities, superMenu: superMenu);
                }

                if (first is VisualElementSubmodelRef vesmr && parent is Aas.AssetAdministrationShell aas
                    && aas.Submodels != null)
                {
                    // up down delete
                    var bos = entities.GetListOfMapResults<Aas.Reference,
                       VisualElementSubmodelRef>((ve) => ve?.theSubmodelRef);
                    EntityListMultipleUpDownDeleteHelper(stack, repo,
                        aas.Submodels, bos, indexInfo, reFocus: true, superMenu: superMenu);

                    // cut copy
                    DispMultiElementCutCopyPasteHelper(stack, repo, vesmr.theEnv, parent, this.theCopyPaste,
                        entities, superMenu: superMenu);
                }

                if (first is VisualElementSubmodelElement sme)
                {
                    // up down delete
                    var bos = entities.GetListOfMapResults<Aas.ISubmodelElement,
                        VisualElementSubmodelElement>((ve) => ve?.theWrapper);

                    if (bos.Count > 0 && parent is Aas.Submodel sm)
                        EntityListMultipleUpDownDeleteHelper<Aas.ISubmodelElement>(stack, repo,
                            sm.SubmodelElements, bos, indexInfo, reFocus: true, superMenu: superMenu);

                    if (bos.Count > 0 && parent is Aas.SubmodelElementCollection smec)
                        EntityListMultipleUpDownDeleteHelper<Aas.ISubmodelElement>(stack, repo,
                            smec.Value, bos, indexInfo, reFocus: true, superMenu: superMenu);

                    DispMultiElementCutCopyPasteHelper(stack, repo, sme.theEnv, parent, this.theCopyPaste,
                        entities, superMenu: superMenu);
                }

                if (first is VisualElementOperationVariable opv && parent is Aas.Operation oppa)
                {
                    // sanity check: same dir?
                    var sameDir = true;
                    foreach (var ent in entities)
                        if (ent is VisualElementOperationVariable entopv)
                            if (entopv.theDir != opv.theDir)
                                sameDir = false;

                    // up down delete
                    if (sameDir)
                    {
                        var bos = entities.GetListOfMapResults<Aas.OperationVariable,
                           VisualElementOperationVariable>((ve) => ve?.theOpVar);
                        var operationVariables = oppa.GetVars(opv.theDir);
                        EntityListMultipleUpDownDeleteHelper(stack, repo,
                            operationVariables, bos, indexInfo, reFocus: true,
                            alternativeFocus: oppa, superMenu: superMenu);
                    }

                    // cut copy
                    DispMultiElementCutCopyPasteHelper(stack, repo, opv.theEnv, parent, this.theCopyPaste,
                        entities, superMenu: superMenu);
                }

                if (first is VisualElementConceptDescription vecd)
                {
                    // up down delete
                    var bos = entities.GetListOfBusinessObjects<Aas.ConceptDescription>();

                    EntityListMultipleUpDownDeleteHelper(
                        stack, repo,
                        vecd.theEnv?.ConceptDescriptions, bos, indexInfo, superMenu: superMenu,
                        preventMove: cdSortOrder.HasValue
                            && cdSortOrder.Value != VisualElementEnvironmentItem.ConceptDescSortOrder.None,
                        sendUpdateEvent: new PackCntChangeEventData()
                        {
                            Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == vecd.theEnv)
                                                 .FirstOrDefault(),
                            ThisElem = vecd.theEnv,
                            ThisElemLocation = PackCntChangeEventLocation.ListOfConceptDescriptions
                        });

                    // cut copy paste
                    DispMultiElementCutCopyPasteHelper(
                        stack, repo, vecd.theEnv, vecd.theEnv,
                        this.theCopyPaste, entities, superMenu: superMenu);
                }

                if (first is VisualElementAdminShell veaas)
                {
                    // up down delete
                    var bos = entities.GetListOfBusinessObjects<Aas.AssetAdministrationShell>();

                    EntityListMultipleUpDownDeleteHelper(stack, repo,
                        veaas.theEnv?.AssetAdministrationShells, bos, indexInfo, superMenu: superMenu,
                        sendUpdateEvent: new PackCntChangeEventData()
                        {
                            Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == veaas.theEnv)
                                                 .FirstOrDefault(),
                            ThisElem = (Aas.IClass)(veaas.theEnv?.AssetAdministrationShells)
                        }); ;

                    // cut copy paste
                    DispMultiElementCutCopyPasteHelper(stack, repo, veaas.theEnv, (Aas.IClass)(veaas.theEnv?.AssetAdministrationShells),
                        this.theCopyPaste, entities, superMenu: superMenu);
                }

                //
                // Change element attributes?
                //
                {
                    var bos = entities.GetListOfBusinessObjects<Aas.IReferable>();
                    if (bos.Count > 0 &&
                        !(first is VisualElementSupplementalFile))
                    {
                        AddActionPanel(stack, "Actions:",
                            repo: repo, superMenu: superMenu,
                            ticketMenu: new AasxMenu()
                                .AddAction("aas-elem-cut", "Change attribute ..",
                                    "Changes common attributes of multiple selected elements."),
                            ticketAction: (buttonNdx, ticket) =>
                            {
                                if (buttonNdx == 0)
                                {
                                    var uc = new AnyUiDialogueDataChangeElementAttributes();
                                    if (this.context.StartFlyoverModal(uc))
                                    {
                                        object nf = null;
                                        foreach (var bo in bos)
                                        {
                                            ChangeElementAttributes(bo, uc);
                                            nf = bo;
                                        }
                                        return new AnyUiLambdaActionRedrawAllElements(nextFocus: nf);
                                    }
                                }
                                return new AnyUiLambdaActionNone();
                            });
                    }
                }

                //
                // Suppl files?
                //
                if (first is VisualElementSupplementalFile)
                {
                    // Delete
                    EntityListSupplementaryFileHelper(stack, repo, entities, superMenu: superMenu,
                        alternativeFocus: VisualElementEnvironmentItem.GiveAliasDataObject(
                                VisualElementEnvironmentItem.ItemType.SupplFiles));
                }
            }

        }
    }
}