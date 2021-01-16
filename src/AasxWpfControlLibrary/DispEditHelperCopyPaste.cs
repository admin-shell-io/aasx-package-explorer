/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AasxIntegrationBase;
using AdminShellNS;

namespace AasxPackageExplorer
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
            public bool valid = true;
            public bool duplicate = false;
            public CopyPasteItemBase item;

            public void Clear()
            {
                this.valid = false;
                this.duplicate = false;
                this.item = null;
            }

            public static Tuple<string[], AdminShell.KeyList[]> PreparePresetsForListKeys(
                CopyPasteBuffer cpb, string label = "Paste")
            {
                // add from Copy Buffer
                AdminShell.KeyList bufferKey = null;
                if (cpb != null && cpb.valid)
                {
                    if (cpb.item is CopyPasteItemIdentifiable cpbi && cpbi.entity?.identification != null)
                        bufferKey = AdminShell.KeyList.CreateNew(
                            new AdminShell.Key("" + cpbi.entity.GetElementName(), false,
                                    cpbi.entity.identification.idType, cpbi.entity.identification.id));

                    if (cpb.item is CopyPasteItemSubmodel cpbsm && cpbsm.sm?.GetSemanticKey() != null)
                        bufferKey = AdminShell.KeyList.CreateNew(cpbsm.sm.GetSemanticKey());

                    if (cpb.item is CopyPasteItemSME cpbsme && cpbsme.sme != null
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
            Panel stack, ModifyRepo repo,
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
                        cpb.valid = true;
                        cpb.duplicate = buttonNdx == 1;
                        cpb.item = new CopyPasteItemSME(env, parentContainer, wrapper, sme);

                        // user feedback
                        AasxPackageExplorer.Log.Singleton.Info(
                            StoredPrint.Color.Blue,
                            "Stored SubmodelElement '{0}'({1}) to internal buffer.{2}", "" + sme.idShort,
                            "" + sme?.GetElementName(),
                            cpb.duplicate
                                ? " Paste will duplicate."
                                : " Paste will cut at original position.");
                    }

                    if (buttonNdx == 2 || buttonNdx == 3 || buttonNdx == 4)
                    {
                        // present
                        var item = cpb?.item as CopyPasteItemSME;
                        if (!cpb.valid || item?.sme == null || item?.wrapper == null ||
                            item?.parentContainer == null)
                        {
                            if (this.flyoutProvider != null)
                                this.flyoutProvider.MessageBoxFlyoutShow(
                                    "No (valid) information for SubmodelElements in copy/paste buffer.",
                                    "Copy & Paste",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            return new ModifyRepo.LambdaActionNone();
                        }

                        // user feedback
                        AasxPackageExplorer.Log.Singleton.Info(
                            "Pasting buffer with SubmodelElement '{0}'({1}) to internal buffer.",
                            "" + item.sme.idShort, "" + item.sme.GetElementName());

                        // apply info
                        var smw2 = new AdminShell.SubmodelElementWrapper(item.sme, shallowCopy: false);

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

                            // TODO (Michael Hoffmeister, 2020-08-01): Operation mssing here?
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

                            // TODO (Michael Hoffmeister, 2020-08-01): Operation mssing here?
                        }
                        if (buttonNdx == 4)
                        {
                            if (sme is AdminShell.IEnumerateChildren smeec)
                                smeec.AddChild(smw2);
                        }

                        // may delete original
                        if (!cpb.duplicate)
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
                        return new ModifyRepo.LambdaActionRedrawAllElements(
                            nextFocus: smw2.submodelElement, isExpanded: true);
                    }

                    return new ModifyRepo.LambdaActionNone();
                });
        }

        public void DispSubmodelCutCopyPasteHelper<T>(
            Panel stack, ModifyRepo repo,
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
                        cpb.valid = true;
                        cpb.duplicate = buttonNdx == 1;
                        cpb.item = new CopyPasteItemSubmodel(parentContainer, entity, smref, sm);

                        // user feedback
                        AasxPackageExplorer.Log.Singleton.Info(
                            StoredPrint.Color.Blue,
                            "Stored Submodel '{0}' to internal buffer.{1}", "" + sm.idShort,
                            cpb.duplicate
                                ? " Paste will duplicate."
                                : " Paste will cut at original position.");
                    }

                    if (buttonNdx == 2 || buttonNdx == 3)
                    {
                        // what type?
                        if (cpb?.item is CopyPasteItemSubmodel item)
                        {
                            // for pasting into, buffer item(s) need to be SME
                            if (!cpb.valid || item?.sm == null
                                || item?.parentContainer == null)
                            {
                                if (this.flyoutProvider != null)
                                    this.flyoutProvider.MessageBoxFlyoutShow(
                                        "No (valid) information for Submodels in copy/paste buffer.",
                                        "Copy & Paste",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                return new ModifyRepo.LambdaActionNone();
                            }

                            // user feedback
                            AasxPackageExplorer.Log.Singleton.Info(
                                "Pasting buffer with Submodel '{0}' to internal buffer.",
                                "" + item.sm.idShort);

                            object entity2 = cloneEntity((T)item.entity);

                            // different cases
                            if (buttonNdx == 2)
                                this.AddElementInListBefore<T>(parentContainer, (T)entity2, entity);
                            if (buttonNdx == 3)
                                this.AddElementInListAfter<T>(parentContainer, (T)entity2, entity);

                            // may delete original
                            if (!cpb.duplicate)
                            {
                                this.DeleteElementInList<T>(
                                        item.parentContainer as List<T>, (T)item.entity, null);

                                // the buffer is tainted
                                cpb.Clear();
                            }

                            // try to focus
                            return new ModifyRepo.LambdaActionRedrawAllElements(
                                nextFocus: entity2, isExpanded: true);
                        }
                        else
                        {
                            this.flyoutProvider?.MessageBoxFlyoutShow(
                                "Entities for paste above/ below Submodels need to Submodels (/-References).",
                                "Copy & Paste",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    if (buttonNdx == 4)
                    {
                        // what type?
                        if (cpb?.item is CopyPasteItemSME item)
                        {
                            // for pasting into, buffer item(s) need to be SME
                            if (item?.sme == null || item?.wrapper == null ||
                                item?.parentContainer == null)
                            {
                                if (this.flyoutProvider != null)
                                    this.flyoutProvider.MessageBoxFlyoutShow(
                                        "No (valid) information for SubmodelElements in copy/paste buffer.",
                                        "Copy & Paste",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                return new ModifyRepo.LambdaActionNone();
                            }

                            // user feedback
                            AasxPackageExplorer.Log.Singleton.Info(
                                "Pasting buffer with SubmodelElement '{0}'({1}) to internal buffer.",
                                "" + item.sme.idShort, "" + item.sme.GetElementName());

                            // apply info
                            var smw2 = new AdminShell.SubmodelElementWrapper(item.sme, shallowCopy: false);

                            if (sm is AdminShell.IEnumerateChildren smeec)
                                smeec.AddChild(smw2);

                            // may delete original
                            if (!cpb.duplicate)
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
                            return new ModifyRepo.LambdaActionRedrawAllElements(
                                nextFocus: smw2.submodelElement, isExpanded: true);
                        }
                        else
                        {
                            this.flyoutProvider?.MessageBoxFlyoutShow(
                                "Entities for paste into Submodels need to SubmodelElements.",
                                "Copy & Paste",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    return new ModifyRepo.LambdaActionNone();
                });
        }

        public void DispPlainIdentifiableCutCopyPasteHelper<T>(
            Panel stack, ModifyRepo repo,
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
                        cpb.valid = true;
                        cpb.duplicate = buttonNdx == 1;
                        cpb.item = new CopyPasteItemIdentifiable(parentContainer, entity);

                        // user feedback
                        AasxPackageExplorer.Log.Singleton.Info(
                            StoredPrint.Color.Blue,
                            "Stored {0} '{1}' to internal buffer.{1}",
                            "" + entity.GetElementName(),
                            "" + entity.idShort,
                            cpb.duplicate
                                ? " Paste will duplicate."
                                : " Paste will cut at original position.");
                    }

                    if (buttonNdx == 2 || buttonNdx == 3)
                    {
                        // what type?
                        if (cpb?.item is CopyPasteItemIdentifiable item)
                        {
                            // for pasting into, buffer item(s) need to be SME
                            if (!cpb.valid || item?.entity == null
                                || item?.parentContainer == null)
                            {
                                if (this.flyoutProvider != null)
                                    this.flyoutProvider.MessageBoxFlyoutShow(
                                        "No (valid) information in copy/paste buffer.",
                                        "Copy & Paste",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                return new ModifyRepo.LambdaActionNone();
                            }

                            // user feedback
                            AasxPackageExplorer.Log.Singleton.Info(
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
                            if (!cpb.duplicate)
                            {
                                this.DeleteElementInList<T>(
                                        item.parentContainer as List<T>, (T)item.entity, null);

                                // the buffer is tainted
                                cpb.Clear();
                            }

                            // try to focus
                            return new ModifyRepo.LambdaActionRedrawAllElements(
                                nextFocus: entity2, isExpanded: true);
                        }
                        else
                        {
                            this.flyoutProvider?.MessageBoxFlyoutShow(
                                "Entities for paste above/ below need to match list type.",
                                "Copy & Paste",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    return new ModifyRepo.LambdaActionNone();
                });
        }
    }
}
