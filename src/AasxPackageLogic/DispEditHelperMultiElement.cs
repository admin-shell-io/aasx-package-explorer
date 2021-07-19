/*
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
using AasxIntegrationBase;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;

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
            AdminShell.AdministrationShellEnv env,
            AdminShell.IAasElement parentContainer,
            CopyPasteBuffer cpb,
            ListOfVisualElementBasic entities,
            string label = "Buffer:")
        {
            // access
            if (parentContainer == null || cpb == null || entities == null)
                return;

            // use an action
            this.AddAction(
                stack, label,
                new[] { "Cut", "Copy" }, repo,
                actionTags: new[] { "aas-multi-elem-cut", "aas-multi-elem-copy" },
                action: (buttonNdx) =>
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
                                && parentContainer is AdminShell.Referable pcref)
                            {
                                var sme = vesme.theWrapper?.submodelElement;
                                AdminShell.EnumerationPlacmentBase placement = null;
                                if (parentContainer is AdminShell.IEnumerateChildren enc)
                                    placement = enc.GetChildrenPlacement(sme);
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
                                && parentContainer is AdminShell.Referable pcref2)
                                if (veopv.theOpVar?.value != null)
                                    cpb.Items.Add(new CopyPasteItemSME(env, pcref2,
                                        veopv.theOpVar.value, veopv.theOpVar.value?.submodelElement));

                            if (el is VisualElementConceptDescription vecd)
                                cpb.Items.Add(new CopyPasteItemIdentifiable(parentContainer, vecd.theCD));

                            if (el is VisualElementAsset veass)
                                cpb.Items.Add(new CopyPasteItemIdentifiable(parentContainer, veass.theAsset));

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
            PackCntChangeEventData sendUpdateEvent = null, bool preventMove = false, bool reFocus = false)
        {
            // access
            if (entities == null || indexInfo == null || list == null)
                return;

            // ask
            AddAction(
                stack, label,
                new[] { "Move up", "Move down", "Move top", "Move end", "Delete" },
                actionTags: new[] { "aas-elem-move-up", "aas-elem-move-down",
                    "aas-elem-move-top", "aas-elem-move-end", "aas-elem-delete" },
                repo: repo,
                action: (buttonNdx) =>
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
                                "Delete selected entities? This operation can not be reverted!", "AASX",
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

        public void ChangeElementAttributes(AdminShell.IAasElement el, AnyUiDialogueDataChangeElementAttributes dia)
        {
            // access
            if (el == null || dia == null)
                return;

            if (dia.AttributeToChange == AnyUiDialogueDataChangeElementAttributes.AttributeEnum.IdShort &&
                el is AdminShell.Referable rf1)
            {
                rf1.idShort = PerformWildcardReplace(rf1.idShort, dia.Pattern);
            }

            if (dia.AttributeToChange == AnyUiDialogueDataChangeElementAttributes.AttributeEnum.Description &&
                el is AdminShell.Referable rf2)
            {
                var input = (rf2.description?.langString == null) ? "" : rf2.description.langString[dia.AttributeLang];
                var nd = PerformWildcardReplace(input, dia.Pattern);
                if (nd != null)
                {
                    if (rf2.description?.langString == null)
                        rf2.description = new AdminShell.Description();
                    rf2.description.langString[dia.AttributeLang] = nd;
                }
            }

            if (dia.AttributeToChange == AnyUiDialogueDataChangeElementAttributes.AttributeEnum.ValueText &&
                el is AdminShell.SubmodelElement sme)
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
            object alternativeFocus = null, string label = "Entities:")
        {
            // access
            if (entities == null)
                return;

            // ask
            AddAction(
                stack, label,
                new[] { "Delete" },
                actionTags: new[] { "aas-elem-delete" },
                repo: repo,
                action: (buttonNdx) =>
                {
                    if (buttonNdx == 0)

                        if (this.context.ActualShiftState
                            || AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                "Delete selected entities? This operation can not be reverted and may directly " +
                                "affect the loaded package!", "AASX",
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
            VisualElementEnvironmentItem.ConceptDescSortOrder? cdSortOrder = null)
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
                AdminShell.IAasElement parent = indexInfo.SharedParent.GetDereferencedMainDataObject()
                                                as AdminShell.IAasElement;

                // TODO (MIHO, 2021-07-08): check for completeness
                if (first is VisualElementSubmodel vesm)
                {
                    // up down delete
                    var bos = entities.GetListOfBusinessObjects<AdminShell.Submodel>();
                    EntityListMultipleUpDownDeleteHelper(stack, repo,
                        vesm.theEnv?.Submodels, bos, indexInfo, reFocus: true);

                    // cut copy
                    DispMultiElementCutCopyPasteHelper(stack, repo, vesm.theEnv, parent, this.theCopyPaste, entities);
                }

                if (first is VisualElementSubmodelRef vesmr && parent is AdminShell.AdministrationShell aas
                    && aas.submodelRefs != null)
                {
                    // up down delete
                    var bos = entities.GetListOfMapResults<AdminShell.SubmodelRef,
                       VisualElementSubmodelRef>((ve) => ve?.theSubmodelRef);
                    EntityListMultipleUpDownDeleteHelper(stack, repo,
                        aas.submodelRefs, bos, indexInfo, reFocus: true);

                    // cut copy
                    DispMultiElementCutCopyPasteHelper(stack, repo, vesmr.theEnv, parent, this.theCopyPaste, entities);
                }

                if (first is VisualElementSubmodelElement sme)
                {
                    // up down delete
                    var bos = entities.GetListOfMapResults<AdminShell.SubmodelElementWrapper,
                        VisualElementSubmodelElement>((ve) => ve?.theWrapper);

                    if (bos.Count > 0 && parent is AdminShell.Submodel sm)
                        EntityListMultipleUpDownDeleteHelper<AdminShell.SubmodelElementWrapper>(stack, repo,
                            sm.submodelElements, bos, indexInfo, reFocus: true);

                    if (bos.Count > 0 && parent is AdminShell.SubmodelElementCollection smec)
                        EntityListMultipleUpDownDeleteHelper<AdminShell.SubmodelElementWrapper>(stack, repo,
                            smec.value, bos, indexInfo, reFocus: true);

                    DispMultiElementCutCopyPasteHelper(stack, repo, sme.theEnv, parent, this.theCopyPaste, entities);
                }

                if (first is VisualElementOperationVariable opv && parent is AdminShell.Operation oppa)
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
                        var bos = entities.GetListOfMapResults<AdminShell.OperationVariable,
                           VisualElementOperationVariable>((ve) => ve?.theOpVar);
                        EntityListMultipleUpDownDeleteHelper(stack, repo,
                            oppa[opv.theDir], bos, indexInfo, reFocus: true,
                            alternativeFocus: oppa);
                    }

                    // cut copy
                    DispMultiElementCutCopyPasteHelper(stack, repo, opv.theEnv, parent, this.theCopyPaste, entities);
                }

                if (first is VisualElementConceptDescription vecd)
                {
                    // up down delete
                    var bos = entities.GetListOfBusinessObjects<AdminShell.ConceptDescription>();

                    EntityListMultipleUpDownDeleteHelper(stack, repo,
                        vecd.theEnv?.ConceptDescriptions, bos, indexInfo,
                        preventMove: cdSortOrder.HasValue
                            && cdSortOrder.Value != VisualElementEnvironmentItem.ConceptDescSortOrder.None,
                        sendUpdateEvent: new PackCntChangeEventData()
                        {
                            Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == vecd.theEnv)
                                                 .FirstOrDefault(),
                            ThisElem = vecd.theEnv?.ConceptDescriptions
                        });

                    // cut copy paste
                    DispMultiElementCutCopyPasteHelper(stack, repo, vecd.theEnv, vecd.theEnv?.ConceptDescriptions,
                        this.theCopyPaste, entities);
                }

                if (first is VisualElementAsset veass)
                {
                    // up down delete
                    var bos = entities.GetListOfBusinessObjects<AdminShell.Asset>();

                    EntityListMultipleUpDownDeleteHelper(stack, repo,
                        veass.theEnv?.Assets, bos, indexInfo,
                        sendUpdateEvent: new PackCntChangeEventData()
                        {
                            Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == veass.theEnv)
                                                 .FirstOrDefault(),
                            ThisElem = veass.theEnv?.Assets
                        });

                    // cut copy paste
                    DispMultiElementCutCopyPasteHelper(stack, repo, veass.theEnv, veass.theEnv?.Assets,
                        this.theCopyPaste, entities);
                }

                if (first is VisualElementAdminShell veaas)
                {
                    // up down delete
                    var bos = entities.GetListOfBusinessObjects<AdminShell.AdministrationShell>();

                    EntityListMultipleUpDownDeleteHelper(stack, repo,
                        veaas.theEnv?.AdministrationShells, bos, indexInfo,
                        sendUpdateEvent: new PackCntChangeEventData()
                        {
                            Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == veaas.theEnv)
                                                 .FirstOrDefault(),
                            ThisElem = veaas.theEnv?.AdministrationShells
                        }); ;

                    // cut copy paste
                    DispMultiElementCutCopyPasteHelper(stack, repo, veaas.theEnv, veaas.theEnv?.AdministrationShells,
                        this.theCopyPaste, entities);
                }

                //
                // Change element attributes?
                //
                {
                    var bos = entities.GetListOfBusinessObjects<AdminShell.Referable>();
                    if (bos.Count > 0 && 
                        !(first is VisualElementSupplementalFile))
                    {
                        this.AddAction(stack, "Actions:", new[] { "Change attribute .." }, repo, (buttonNdx) =>
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
                    EntityListSupplementaryFileHelper(stack, repo, entities,
                        alternativeFocus: VisualElementEnvironmentItem.GiveAliasDataObject(
                                VisualElementEnvironmentItem.ItemType.SupplFiles));                    
                }
            }

        }
    }
}