/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
//using System.Windows;
//using System.Windows.Controls;
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;

namespace AasxPackageLogic
{
    /// <summary>
    /// This class extends the basic helper functionalities of DispEditHelper by providing functionality
    /// for cut/copy/paste of different elements
    /// </summary>
    public class DispEditHelperCopyPaste : DispEditHelperBasics
    {
        //
        // Data structures
        //

        public class CopyPasteItemBase
        {
        }

        public class CopyPasteItemIdentifiable : CopyPasteItemBase
        {
            public object parentContainer = null;
            public AdminShell.Identifiable entity = null;

            public CopyPasteItemIdentifiable() { }

            public CopyPasteItemIdentifiable(
                object parentContainer,
                AdminShell.Identifiable entity)
            {
                this.parentContainer = parentContainer;
                this.entity = entity;
            }
        }


        public class CopyPasteItemSubmodel : CopyPasteItemBase
        {
            public object parentContainer = null;
            public object entity = null;
            public AdminShell.SubmodelRef smref = null;
            public AdminShell.Submodel sm = null;

            public CopyPasteItemSubmodel() { }

            public CopyPasteItemSubmodel(
                object parentContainer,
                object entity,
                AdminShell.SubmodelRef smref,
                AdminShell.Submodel sm)
            {
                this.parentContainer = parentContainer;
                this.entity = entity;
                this.smref = smref;
                this.sm = sm;
            }
        }

        public class CopyPasteItemSME : CopyPasteItemBase
        {
            public AdminShell.AdministrationShellEnv env = null;
            public AdminShell.Referable parentContainer = null;
            public AdminShell.SubmodelElementWrapper wrapper = null;
            public AdminShell.SubmodelElement sme = null;

            public CopyPasteItemSME() { }

            public CopyPasteItemSME(
                AdminShell.AdministrationShellEnv env,
                AdminShell.Referable parentContainer, AdminShell.SubmodelElementWrapper wrapper,
                AdminShell.SubmodelElement sme)
            {
                this.env = env;
                this.parentContainer = parentContainer;
                this.wrapper = wrapper;
                this.sme = sme;
            }
        }

        public class CopyPasteBuffer
        {
            public bool Valid = true;
            public bool Duplicate = false;
            public AdminShell.EnumerationPlacmentBase Placement = null;
            public CopyPasteItemBase Item;

            public void Clear()
            {
                this.Valid = false;
                this.Duplicate = false;
                this.Item = null;
            }

            public static Tuple<string[], AdminShell.KeyList[]> PreparePresetsForListKeys(
                CopyPasteBuffer cpb, string label = "Paste")
            {
                // add from Copy Buffer
                AdminShell.KeyList bufferKey = null;
                if (cpb != null && cpb.Valid)
                {
                    if (cpb.Item is CopyPasteItemIdentifiable cpbi && cpbi.entity?.identification != null)
                        bufferKey = AdminShell.KeyList.CreateNew(
                            new AdminShell.Key("" + cpbi.entity.GetElementName(), false,
                                    cpbi.entity.identification.idType, cpbi.entity.identification.id));

                    if (cpb.Item is CopyPasteItemSubmodel cpbsm && cpbsm.sm?.GetSemanticKey() != null)
                        bufferKey = AdminShell.KeyList.CreateNew(cpbsm.sm.GetSemanticKey());

                    if (cpb.Item is CopyPasteItemSME cpbsme && cpbsme.sme != null
                        && cpbsme.env.Submodels != null)
                    {
                        // index parents for ALL Submodels -> parent for our SME shall be set by this ..
                        foreach (var sm in cpbsme.env?.Submodels)
                            sm?.SetAllParents();

                        // collect buffer list
                        bufferKey = new AdminShell.KeyList();
                        cpbsme.sme.CollectReferencesByParent(bufferKey);
                    }
                }

                // result
                return new Tuple<string[], AdminShell.KeyList[]>(
                    (bufferKey == null) ? null : new[] { label },
                    (bufferKey == null) ? null : new[] { bufferKey }
                );
            }
        }

        //
        // Helper functions
        //

        public void DispSmeCutCopyPasteHelper(
            AnyUiPanel stack,
            ModifyRepo repo,
            AdminShell.AdministrationShellEnv env,
            AdminShell.Referable parentContainer,
            CopyPasteBuffer cpb,
            AdminShell.SubmodelElementWrapper wrapper,
            AdminShell.SubmodelElement sme,
            string label = "Buffer:")
        {
            // access
            if (parentContainer == null || cpb == null || sme == null)
                return;

            // use an action
            this.AddAction(
                stack, label,
                new[] { "Cut", "Copy", "Paste above", "Paste below", "Paste into" }, repo,
                (buttonNdx) =>
                {
                    if (buttonNdx == 0 || buttonNdx == 1)
                    {
                        // store info
                        cpb.Valid = true;
                        cpb.Duplicate = buttonNdx == 1;
                        if (parentContainer is AdminShell.IEnumerateChildren enc)
                            cpb.Placement = enc.GetChildrenPlacement(sme);
                        cpb.Item = new CopyPasteItemSME(env, parentContainer, wrapper, sme);

                        // special case?

                        // user feedback
                        Log.Singleton.Info(
                            StoredPrint.Color.Blue,
                            "Stored SubmodelElement '{0}'({1}) to internal buffer.{2}", "" + sme.idShort,
                            "" + sme?.GetElementName(),
                            cpb.Duplicate
                                ? " Paste will duplicate."
                                : " Paste will cut at original position.");
                    }

                    if (buttonNdx == 2 || buttonNdx == 3 || buttonNdx == 4)
                    {
                        // present
                        var item = cpb?.Item as CopyPasteItemSME;
                        if (!cpb.Valid || item?.sme == null || item?.wrapper == null ||
                            item?.parentContainer == null)
                        {
                            this.context?.MessageBoxFlyoutShow(
                                    "No (valid) information for SubmodelElements in copy/paste buffer.",
                                    "Copy & Paste",
                                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                            return new AnyUiLambdaActionNone();
                        }

                        // user feedback
                        Log.Singleton.Info(
                            "Pasting buffer with SubmodelElement '{0}'({1}) to internal buffer.",
                            "" + item.sme.idShort, "" + item.sme.GetElementName());

                        // apply info
                        var smw2 = new AdminShell.SubmodelElementWrapper(item.sme, shallowCopy: false);
                        object nextBusObj = smw2.submodelElement;

                        // insertation depends on parent container
                        if (buttonNdx == 2)
                        {
                            if (parentContainer is AdminShell.Submodel pcsm && wrapper != null)
                                this.AddElementInListBefore<AdminShell.SubmodelElementWrapper>(
                                    pcsm.submodelElements, smw2, wrapper);

                            if (parentContainer is AdminShell.SubmodelElementCollection pcsmc &&
                                    wrapper != null)
                                this.AddElementInListBefore<AdminShell.SubmodelElementWrapper>(
                                    pcsmc.value, smw2, wrapper);

                            if (parentContainer is AdminShell.Entity pcent &&
                                    wrapper != null)
                                this.AddElementInListBefore<AdminShell.SubmodelElementWrapper>(
                                    pcent.statements, smw2, wrapper);

                            if (parentContainer is AdminShell.AnnotatedRelationshipElement pcarel &&
                                    wrapper != null)
                                this.AddElementInListBefore<AdminShell.SubmodelElementWrapper>(
                                    pcarel.annotations, smw2, wrapper);

                            // TODO (Michael Hoffmeister, 2020-08-01): Operation complete?
                            if (parentContainer is AdminShell.Operation pcop && wrapper?.submodelElement != null)
                            {
                                var place = pcop.GetChildrenPlacement(wrapper.submodelElement) as 
                                    AdminShell.Operation.EnumerationPlacmentOperationVariable;
                                if (place?.OperationVariable != null)
                                {
                                    var op = new AdminShell.OperationVariable();
                                    op.value = smw2;
                                    this.AddElementInListBefore<AdminShell.OperationVariable>(
                                        pcop[place.Direction], op, place.OperationVariable);
                                    nextBusObj = op;
                                }
                            }
                        }
                        
                        if (buttonNdx == 3)
                        {
                            if (parentContainer is AdminShell.Submodel pcsm && wrapper != null)
                                this.AddElementInListAfter<AdminShell.SubmodelElementWrapper>(
                                    pcsm.submodelElements, smw2, wrapper);

                            if (parentContainer is AdminShell.SubmodelElementCollection pcsmc &&
                                    wrapper != null)
                                this.AddElementInListAfter<AdminShell.SubmodelElementWrapper>(
                                    pcsmc.value, smw2, wrapper);

                            if (parentContainer is AdminShell.Entity pcent && wrapper != null)
                                this.AddElementInListAfter<AdminShell.SubmodelElementWrapper>(
                                    pcent.statements, smw2, wrapper);

                            if (parentContainer is AdminShell.AnnotatedRelationshipElement pcarel &&
                                    wrapper != null)
                                this.AddElementInListAfter<AdminShell.SubmodelElementWrapper>(
                                    pcarel.annotations, smw2, wrapper);

                            // TODO (Michael Hoffmeister, 2020-08-01): Operation complete?
                            if (parentContainer is AdminShell.Operation pcop && wrapper?.submodelElement != null)
                            {
                                var place = pcop.GetChildrenPlacement(wrapper.submodelElement) as
                                    AdminShell.Operation.EnumerationPlacmentOperationVariable;
                                if (place?.OperationVariable != null)
                                {
                                    var op = new AdminShell.OperationVariable();
                                    op.value = smw2;
                                    this.AddElementInListAfter<AdminShell.OperationVariable>(
                                        pcop[place.Direction], op, place.OperationVariable);
                                    nextBusObj = op;
                                }
                            }
                        }

                        if (buttonNdx == 4)
                        {
                            if (sme is AdminShell.IEnumerateChildren smeec)
                                smeec.AddChild(smw2, cpb.Placement);
                        }

                        // may delete original
                        if (!cpb.Duplicate)
                        {
                            if (item.parentContainer is AdminShell.Submodel pcsm && item.wrapper != null)
                                this.DeleteElementInList<AdminShell.SubmodelElementWrapper>(
                                    pcsm.submodelElements, item.wrapper, null);

                            if (item.parentContainer is AdminShell.SubmodelElementCollection pcsmc
                                && item.wrapper != null)
                                this.DeleteElementInList<AdminShell.SubmodelElementWrapper>(
                                    pcsmc.value, item.wrapper, null);

                            // the buffer is tainted
                            cpb.Clear();
                        }

                        // try to focus
                        return new AnyUiLambdaActionRedrawAllElements(
                            nextFocus: nextBusObj, isExpanded: true);
                    }

                    return new AnyUiLambdaActionNone();
                });
        }

        public void DispSubmodelCutCopyPasteHelper<T>(
            AnyUiPanel stack,
            ModifyRepo repo,
            CopyPasteBuffer cpb,
            List<T> parentContainer,
            T entity,
            Func<T, T> cloneEntity,
            AdminShell.SubmodelRef smref,
            AdminShell.Submodel sm,
            string label = "Buffer:") where T : new()
        {
            // access
            if (parentContainer == null || cpb == null || sm == null || cloneEntity == null)
                return;

            // integrity
            // ReSharper disable RedundantCast
            if (entity as object != smref as object && entity as object != sm as object)
                return;
            // ReSharper enable RedundantCast

            // use an action
            this.AddAction(
                stack, label,
                new[] { "Cut", "Copy", "Paste above", "Paste below", "Paste into" }, repo,
                (buttonNdx) =>
                {
                    if (buttonNdx == 0 || buttonNdx == 1)
                    {
                        // store info
                        cpb.Valid = true;
                        cpb.Duplicate = buttonNdx == 1;
                        cpb.Item = new CopyPasteItemSubmodel(parentContainer, entity, smref, sm);

                        // user feedback
                        Log.Singleton.Info(
                            StoredPrint.Color.Blue,
                            "Stored Submodel '{0}' to internal buffer.{1}", "" + sm.idShort,
                            cpb.Duplicate
                                ? " Paste will duplicate."
                                : " Paste will cut at original position.");
                    }

                    if (buttonNdx == 2 || buttonNdx == 3)
                    {
                        // what type?
                        if (cpb?.Item is CopyPasteItemSubmodel item)
                        {
                            // for pasting into, buffer item(s) need to be SME
                            if (!cpb.Valid || item?.sm == null
                                || item?.parentContainer == null)
                            {
                                this.context?.MessageBoxFlyoutShow(
                                        "No (valid) information for Submodels in copy/paste buffer.",
                                        "Copy & Paste",
                                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                                return new AnyUiLambdaActionNone();
                            }

                            // user feedback
                            Log.Singleton.Info(
                                "Pasting buffer with Submodel '{0}' to internal buffer.",
                                "" + item.sm.idShort);

                            object entity2 = cloneEntity((T)item.entity);

                            // different cases
                            if (buttonNdx == 2)
                                this.AddElementInListBefore<T>(parentContainer, (T)entity2, entity);
                            if (buttonNdx == 3)
                                this.AddElementInListAfter<T>(parentContainer, (T)entity2, entity);

                            // may delete original
                            if (!cpb.Duplicate)
                            {
                                this.DeleteElementInList<T>(
                                        item.parentContainer as List<T>, (T)item.entity, null);

                                // the buffer is tainted
                                cpb.Clear();
                            }

                            // try to focus
                            return new AnyUiLambdaActionRedrawAllElements(
                                nextFocus: entity2, isExpanded: true);
                        }
                        else
                        {
                            this.context?.MessageBoxFlyoutShow(
                                "Entities for paste above/ below Submodels need to Submodels (/-References).",
                                "Copy & Paste",
                                AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                        }
                    }

                    if (buttonNdx == 4)
                    {
                        // what type?
                        if (cpb?.Item is CopyPasteItemSME item)
                        {
                            // for pasting into, buffer item(s) need to be SME
                            if (item?.sme == null || item?.wrapper == null ||
                                item?.parentContainer == null)
                            {
                                this.context?.MessageBoxFlyoutShow(
                                        "No (valid) information for SubmodelElements in copy/paste buffer.",
                                        "Copy & Paste",
                                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                                return new AnyUiLambdaActionNone();
                            }

                            // user feedback
                            Log.Singleton.Info(
                                "Pasting buffer with SubmodelElement '{0}'({1}) to internal buffer.",
                                "" + item.sme.idShort, "" + item.sme.GetElementName());

                            // apply info
                            var smw2 = new AdminShell.SubmodelElementWrapper(item.sme, shallowCopy: false);

                            if (sm is AdminShell.IEnumerateChildren smeec)
                                smeec.AddChild(smw2);

                            // may delete original
                            if (!cpb.Duplicate)
                            {
                                if (item.parentContainer is AdminShell.Submodel pcsm && item.wrapper != null)
                                    this.DeleteElementInList<AdminShell.SubmodelElementWrapper>(
                                        pcsm.submodelElements, item.wrapper, null);

                                if (item.parentContainer is AdminShell.SubmodelElementCollection pcsmc
                                    && item.wrapper != null)
                                    this.DeleteElementInList<AdminShell.SubmodelElementWrapper>(
                                        pcsmc.value, item.wrapper, null);

                                // the buffer is tainted
                                cpb.Clear();
                            }

                            // try to focus
                            return new AnyUiLambdaActionRedrawAllElements(
                                nextFocus: smw2.submodelElement, isExpanded: true);
                        }
                        else
                        {
                            this.context?.MessageBoxFlyoutShow(
                                "Entities for paste into Submodels need to SubmodelElements.",
                                "Copy & Paste",
                                AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                        }
                    }

                    return new AnyUiLambdaActionNone();
                });
        }

        public void DispPlainIdentifiableCutCopyPasteHelper<T>(
            AnyUiPanel stack,
            ModifyRepo repo,
            CopyPasteBuffer cpb,
            List<T> parentContainer,
            T entity,
            Func<T, T> cloneEntity,
            string label = "Buffer:") where T : AdminShell.Identifiable, new()
        {
            // access
            if (parentContainer == null || cpb == null || entity == null || cloneEntity == null)
                return;

            // use an action
            this.AddAction(
                stack, label,
                new[] { "Cut", "Copy", "Paste above", "Paste below" }, repo,
                (buttonNdx) =>
                {
                    if (buttonNdx == 0 || buttonNdx == 1)
                    {
                        // store info
                        cpb.Valid = true;
                        cpb.Duplicate = buttonNdx == 1;
                        cpb.Item = new CopyPasteItemIdentifiable(parentContainer, entity);

                        // user feedback
                        Log.Singleton.Info(
                            StoredPrint.Color.Blue,
                            "Stored {0} '{1}' to internal buffer.{1}",
                            "" + entity.GetElementName(),
                            "" + entity.idShort,
                            cpb.Duplicate
                                ? " Paste will duplicate."
                                : " Paste will cut at original position.");
                    }

                    if (buttonNdx == 2 || buttonNdx == 3)
                    {
                        // what type?
                        if (cpb?.Item is CopyPasteItemIdentifiable item)
                        {
                            // for pasting into, buffer item(s) need to be SME
                            if (!cpb.Valid || item?.entity == null
                                || item?.parentContainer == null)
                            {
                                this.context?.MessageBoxFlyoutShow(
                                        "No (valid) information in copy/paste buffer.",
                                        "Copy & Paste",
                                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                                return new AnyUiLambdaActionNone();
                            }

                            // user feedback
                            Log.Singleton.Info(
                                "Pasting buffer with {0} '{1}' to internal buffer.",
                                "" + item.entity.GetElementName(),
                                "" + item.entity.idShort);

                            object entity2 = cloneEntity((T)item.entity);

                            // different cases
                            if (buttonNdx == 2)
                                this.AddElementInListBefore<T>(parentContainer, (T)entity2, entity);
                            if (buttonNdx == 3)
                                this.AddElementInListAfter<T>(parentContainer, (T)entity2, entity);

                            // may delete original
                            if (!cpb.Duplicate)
                            {
                                this.DeleteElementInList<T>(
                                        item.parentContainer as List<T>, (T)item.entity, null);

                                // the buffer is tainted
                                cpb.Clear();
                            }

                            // try to focus
                            return new AnyUiLambdaActionRedrawAllElements(
                                nextFocus: entity2, isExpanded: true);
                        }
                        else
                        {
                            this.context?.MessageBoxFlyoutShow(
                                "Entities for paste above/ below need to match list type.",
                                "Copy & Paste",
                                AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                        }
                    }

                    return new AnyUiLambdaActionNone();
                });
        }
    }
}
