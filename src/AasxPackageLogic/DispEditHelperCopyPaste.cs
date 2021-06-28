/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            // members for all

            public virtual object GetMainDataObject() { return null; }

            // Factory

            public static CopyPasteItemBase FactoryConvertFrom(AdminShell.Referable rf)
            {
                // try fake a copy paste item (order matters!)
                CopyPasteItemBase res = CopyPasteItemSME.ConvertFrom(rf);
                if (res == null)
                    res = CopyPasteItemSubmodel.ConvertFrom(rf);
                if (res == null)
                    res = CopyPasteItemIdentifiable.ConvertFrom(rf);

                // ok
                return res;
            }
        }

        public class ListOfCopyPasteItem : List<CopyPasteItemBase>
        {
            public ListOfCopyPasteItem() { }

            public ListOfCopyPasteItem(CopyPasteItemBase item)
            {
                this.Add(item);
            }

            /// <summary>
            /// Check, if elements are present and all of same type.
            /// </summary>
            /// <typeparam name="T">Desired subclass of <c>VisualElementGeneric</c></typeparam>
            public bool AllOfElementType<T>() where T : CopyPasteItemBase
            {
                if (this.Count < 1)
                    return false;
                foreach (var ve in this)
                    if (!(ve is T))
                        return false;
                return true;
            }
        }

        public class CopyPasteItemIdentifiable : CopyPasteItemBase
        {
            public object parentContainer = null;
            public AdminShell.Identifiable entity = null;

            public override object GetMainDataObject() { return entity; }

            public CopyPasteItemIdentifiable() { }

            public CopyPasteItemIdentifiable(
                object parentContainer,
                AdminShell.Identifiable entity)
            {
                this.parentContainer = parentContainer;
                this.entity = entity;
            }

            public static CopyPasteItemIdentifiable ConvertFrom(AdminShell.Referable rf)
            {
                // access
                var idf = rf as AdminShell.Identifiable;
                if (idf == null
                    || !(idf is AdminShell.AdministrationShell
                         || idf is AdminShell.Asset || idf is AdminShell.ConceptDescription))
                    return null;

                // create
                return new CopyPasteItemIdentifiable()
                {
                    entity = idf
                };
            }
        }

        public class CopyPasteItemSubmodel : CopyPasteItemBase
        {
            public object parentContainer = null;
            public AdminShell.SubmodelRef smref = null;
            public AdminShell.Submodel sm = null;

            public override object GetMainDataObject() { return sm; }

            public CopyPasteItemSubmodel() { }

            public CopyPasteItemSubmodel(
                object parentContainer,
                object entity,
                AdminShell.SubmodelRef smref,
                AdminShell.Submodel sm)
            {
                this.parentContainer = parentContainer;
                this.smref = smref;
                this.sm = sm;
                TryFixSmRefIfNull();
            }

            public void TryFixSmRefIfNull()
            {
                if (smref == null && sm?.identification != null)
                {
                    smref = new AdminShell.SubmodelRef(new AdminShell.Reference(new AdminShell.Key(
                    AdminShell.Key.Submodel, true, sm.identification.idType, sm.identification.id)));
                }
            }

            public static CopyPasteItemSubmodel ConvertFrom(AdminShell.Referable rf)
            {
                // access
                var sm = rf as AdminShell.Submodel;
                if (sm == null || sm.identification == null)
                    return null;

                // create
                var res = new CopyPasteItemSubmodel() { sm = sm };

                // fake smref
                res.TryFixSmRefIfNull();

                // ok
                return res;
            }
        }

        public class CopyPasteItemSME : CopyPasteItemBase
        {
            public AdminShell.AdministrationShellEnv env = null;
            public AdminShell.Referable parentContainer = null;
            public AdminShell.SubmodelElementWrapper wrapper = null;
            public AdminShell.SubmodelElement sme = null;
            public AdminShell.EnumerationPlacmentBase Placement = null;

            public override object GetMainDataObject() { return sme; }


            public CopyPasteItemSME() { }

            public CopyPasteItemSME(
                AdminShell.AdministrationShellEnv env,
                AdminShell.Referable parentContainer, AdminShell.SubmodelElementWrapper wrapper,
                AdminShell.SubmodelElement sme,
                AdminShell.EnumerationPlacmentBase placement = null)
            {
                this.env = env;
                this.parentContainer = parentContainer;
                this.wrapper = wrapper;
                this.sme = sme;
                this.Placement = placement;
            }

            public static CopyPasteItemSME ConvertFrom(AdminShell.Referable rf)
            {
                // access
                var sme = rf as AdminShell.SubmodelElement;
                if (sme == null)
                    return null;

                // new wrapper
                var wrapper = new AdminShell.SubmodelElementWrapper();
                wrapper.submodelElement = sme;

                // create
                return new CopyPasteItemSME()
                {
                    sme = sme,
                    wrapper = wrapper
                };
            }
        }

        public class CopyPasteBuffer
        {
            public bool Valid = false;
            public bool ExternalSource = false;
            public bool Duplicate = false;

            public string Watermark = null;

            public ListOfCopyPasteItem Items;

            public bool ContentAvailable { get { return Items != null && Items.Count > 0 && Valid; } }

            public CopyPasteBuffer()
            {
                GenerateWatermark();
            }

            public void GenerateWatermark()
            {
                var r = new Random();
                Watermark = String.Format("{0:000000}", r.Next(1, 999999));
            }

            public void Clear()
            {
                this.Valid = false;
                this.ExternalSource = false;
                this.Duplicate = false;
                GenerateWatermark();
                this.Items = null;
            }

            public static Tuple<string[], AdminShell.KeyList[]> PreparePresetsForListKeys(
                CopyPasteBuffer cpb, string label = "Paste")
            {
                // add from Copy Buffer
                AdminShell.KeyList bufferKey = null;
                if (cpb != null && cpb.Valid && cpb.Items != null && cpb.Items.Count == 1)
                {
                    if (cpb.Items[0] is CopyPasteItemIdentifiable cpbi && cpbi.entity?.identification != null)
                        bufferKey = AdminShell.KeyList.CreateNew(
                            new AdminShell.Key("" + cpbi.entity.GetElementName(), false,
                                    cpbi.entity.identification.idType, cpbi.entity.identification.id));

                    if (cpb.Items[0] is CopyPasteItemSubmodel cpbsm && cpbsm.sm?.GetSemanticKey() != null)
                        bufferKey = AdminShell.KeyList.CreateNew(cpbsm.sm.GetSemanticKey());

                    if (cpb.Items[0] is CopyPasteItemSME cpbsme && cpbsme.sme != null
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

            private string PrepareClipboadString()
            {
                // make a pure array of objects
                // (absolutely no unnecessary stuff to the public)
                var oar = new List<object>();
                if (Items != null)
                    foreach (var it in Items)
                    {
                        var o = it.GetMainDataObject();
                        if (o != null)
                            oar.Add(o);
                    }

                // nothing? what to serialize?
                if (oar.Count < 1)
                    return null;
                var objToSerialize = (oar.Count == 1) ? oar[0] : oar;

                // make JSON
                var settings = AasxIntegrationBase.AasxPluginOptionSerialization.GetDefaultJsonSettings(
                    new[] { typeof(AdminShellEvents.AasEventMsgEnvelope) });
                settings.TypeNameHandling = TypeNameHandling.None;
                settings.Formatting = Formatting.Indented;
                var json = JsonConvert.SerializeObject(objToSerialize, settings);

                // ok
                return json;
            }

            public void CopyToClipboard(AnyUiContextBase context, string watermark = null)
            {
                // access
                if (context == null)
                    return;

                // get something?
                var s = PrepareClipboadString();
                if (s == null)
                    return;

                // prepare clipboard data
                context.ClipboardSet(new AnyUiClipboardData()
                {
                    Watermark = watermark,
                    Text = s
                });
            }

            public static CopyPasteBuffer FromClipboardString(string cps)
            {
                // access
                if (cps == null)
                    return null;
                cps = cps.Trim();
                if (cps.Length < 1)
                    return null;

                // quite likely to crash
                try
                {
                    // be very straight for allowed formats
                    var isSingleObject = cps.StartsWith("{");
                    var isArrayObject = cps.StartsWith("[");

                    // try simple way
                    if (isSingleObject)
                    {
                        // TODO (MIHO, 2021-06-22): think of converting Referable to IAasElement
                        var obj = AdminShellSerializationHelper.DeserializeFromJSON<AdminShell.Referable>(cps);

                        // try fake a copy paste item (order matters!)
                        var cpi = CopyPasteItemBase.FactoryConvertFrom(obj);
                        if (cpi != null)
                            return new CopyPasteBuffer()
                            {
                                Valid = true,
                                ExternalSource = true,
                                Duplicate = true,
                                Items = new ListOfCopyPasteItem(cpi)
                            };
                    }
                    else
                    if (isArrayObject)
                    {
                        // make array of object
                        var objarr = AdminShellSerializationHelper
                            .DeserializePureObjectFromJSON<List<AdminShell.Referable>>(cps);
                        if (objarr != null)
                        {
                            // overall structure
                            var cpb = new CopyPasteBuffer()
                            {
                                Valid = true,
                                ExternalSource = true,
                                Duplicate = true,
                                Items = new ListOfCopyPasteItem()
                            };

                            // single items
                            foreach (var obj in objarr)
                            {
                                var cpi = CopyPasteItemBase.FactoryConvertFrom(obj);
                                if (cpi != null)
                                    cpb.Items.Add(cpi);
                            }

                            // be picky with validity
                            if (cpb.Items.Count > 0)
                                return cpb;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "when trying to decode clipboad text");
                }

                // ups
                return null;
            }

            /// <summary>
            /// Either returns itself as valid copy paste buffer or adopts an temporary one
            /// by taking the clipboard data.
            /// </summary>
            public CopyPasteBuffer CheckIfUseExternalCopyPasteBuffer(AnyUiClipboardData cbd)
            {
                // easy way out
                var res = this;
                if (cbd == null)
                    return res;

                // try get external one
                var ext = FromClipboardString(cbd.Text);
                if (ext == null)
                    // default way out
                    return res;

                // if this buffer is not valid, but clipboard can provide? 
                if (!this.Valid && ext.Valid)
                    return ext;

                // ok, default is us
                return res;
            }
        }

        //
        // Helper functions
        //

        protected void DispDeleteCopyPasteItem(CopyPasteItemSME item)
        {
            // access
            if (item == null)
                return;

            // differentiate
            if (item.parentContainer is AdminShell.Submodel pcsm && item.wrapper != null)
                this.DeleteElementInList<AdminShell.SubmodelElementWrapper>(
                    pcsm.submodelElements, item.wrapper, null);

            if (item.parentContainer is AdminShell.SubmodelElementCollection pcsmc
                && item.wrapper != null)
                this.DeleteElementInList<AdminShell.SubmodelElementWrapper>(
                    pcsmc.value, item.wrapper, null);

            if (item.parentContainer is AdminShell.Operation pcop && item.wrapper != null)
            {
                var placement = pcop.GetChildrenPlacement(item.sme) as
                    AdminShell.Operation.EnumerationPlacmentOperationVariable;
                if (placement != null)
                    pcop[placement.Direction].Remove(placement.OperationVariable);
            }
        }

        public void DispSmeCutCopyPasteHelper(
            AnyUiPanel stack,
            ModifyRepo repo,
            AdminShell.AdministrationShellEnv env,
            AdminShell.Referable parentContainer,
            CopyPasteBuffer cpbInternal,
            AdminShell.SubmodelElementWrapper wrapper,
            AdminShell.SubmodelElement sme,
            string label = "Buffer:")
        {
            // access
            if (parentContainer == null || cpbInternal == null || sme == null)
                return;

            // use an action
            this.AddAction(
                stack, label,
                new[] { "Cut", "Copy", "Paste above", "Paste below", "Paste into" }, repo,
                actionTags: new[] { "aas-elem-cut", "aas-elem-copy", "aas-elem-paste-above",
                    "aas-elem-paste-below", "aas-elem-paste-into" },
                action: (buttonNdx) =>
                {
                    if (buttonNdx == 0 || buttonNdx == 1)
                    {
                        // store info
                        cpbInternal.Clear();
                        cpbInternal.Valid = true;
                        cpbInternal.Duplicate = buttonNdx == 1;
                        AdminShell.EnumerationPlacmentBase placement = null;
                        if (parentContainer is AdminShell.IEnumerateChildren enc)
                            placement = enc.GetChildrenPlacement(sme);
                        cpbInternal.Items = new ListOfCopyPasteItem(
                            new CopyPasteItemSME(env, parentContainer, wrapper, sme, placement));
                        cpbInternal.CopyToClipboard(context, cpbInternal.Watermark);

                        // special case?

                        // user feedback
                        Log.Singleton.Info(
                            StoredPrint.Color.Blue,
                            "Stored SubmodelElement '{0}'({1}) to internal buffer.{2}", "" + sme.idShort,
                            "" + sme?.GetElementName(),
                            cpbInternal.Duplicate
                                ? " Paste will duplicate."
                                : " Paste will cut at original position.");
                    }

                    if (buttonNdx == 2 || buttonNdx == 3 || buttonNdx == 4)
                    {
                        // which buffer?
                        var cbdata = context?.ClipboardGet();
                        var cpb = cpbInternal.CheckIfUseExternalCopyPasteBuffer(cbdata);

                        // content?
                        if (!cpb.ContentAvailable)
                        {
                            this.context?.MessageBoxFlyoutShow(
                                "No sufficient infomation in internal paste buffer or external clipboard.",
                                "Copy & Paste",
                                AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                            return new AnyUiLambdaActionNone();
                        }

                        // uniform?
                        if (!cpb.Items.AllOfElementType<CopyPasteItemSME>())
                        {
                            this.context?.MessageBoxFlyoutShow(
                                    "No (valid) information for SubmodelElements in copy/paste buffer.",
                                    "Copy & Paste",
                                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                            return new AnyUiLambdaActionNone();
                        }

                        // user feedback
                        Log.Singleton.Info($"Pasting {cpb.Items.Count} SubmodelElements from paste buffer");

                        // loop over items
                        object nextBusObj = null;
                        foreach (var it in cpb.Items)
                        {
                            // access
                            var item = it as CopyPasteItemSME;
                            if (item?.sme == null || item.wrapper == null
                                || (!cpb.Duplicate && item?.parentContainer == null))
                            {
                                Log.Singleton.Error("When pasting SME, an element was invalid.");
                                continue;
                            }

                            // apply info
                            var smw2 = new AdminShell.SubmodelElementWrapper(item.sme, shallowCopy: false);
                            nextBusObj = smw2.submodelElement;

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
                                    smeec.AddChild(smw2, item.Placement);
                            }

                            // may delete original
                            if (!cpb.Duplicate && !cpb.ExternalSource)
                            {
                                DispDeleteCopyPasteItem(item);
                            }
                        }

                        // the buffer is tainted
                        cpb.Clear();

                        // try to focus next
                        return new AnyUiLambdaActionRedrawAllElements(
                            nextFocus: nextBusObj, isExpanded: true);
                    }

                    return new AnyUiLambdaActionNone();
                });
        }

        public void DispSubmodelCutCopyPasteHelper<T>(
            AnyUiPanel stack,
            ModifyRepo repo,
            CopyPasteBuffer cpbInternal,
            List<T> parentContainer,
            T entity,
            Func<T, T> cloneEntity,
            AdminShell.SubmodelRef smref,
            AdminShell.Submodel sm,
            string label = "Buffer:",
            Func<T, T, bool> checkEquality = null,
            Action<CopyPasteItemBase> extraAction = null) where T : new()
        {
            // access
            if (parentContainer == null || cpbInternal == null || sm == null || cloneEntity == null)
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
                        cpbInternal.Clear();
                        cpbInternal.Valid = true;
                        cpbInternal.Duplicate = buttonNdx == 1;
                        cpbInternal.Items = new ListOfCopyPasteItem(
                            new CopyPasteItemSubmodel(parentContainer, entity, smref, sm));
                        cpbInternal.CopyToClipboard(context, cpbInternal.Watermark);

                        // user feedback
                        Log.Singleton.Info(
                            StoredPrint.Color.Blue,
                            "Stored Submodel '{0}' to internal buffer.{1}", "" + sm.idShort,
                            cpbInternal.Duplicate
                                ? " Paste will duplicate."
                                : " Paste will cut at original position.");
                    }

                    if (buttonNdx == 2 || buttonNdx == 3)
                    {
                        // which buffer?
                        var cbdata = context?.ClipboardGet();
                        var cpb = cpbInternal.CheckIfUseExternalCopyPasteBuffer(cbdata);

                        // content?
                        if (!cpb.ContentAvailable)
                        {
                            this.context?.MessageBoxFlyoutShow(
                                "No sufficient infomation in internal paste buffer or external clipboard.",
                                "Copy & Paste",
                                AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                            return new AnyUiLambdaActionNone();
                        }

                        // pasting above/ below means: Submodels
                        if (!cpb.Items.AllOfElementType<CopyPasteItemSubmodel>())
                        {
                            this.context?.MessageBoxFlyoutShow(
                                    "No (valid) information for Submodels in copy/paste buffer.",
                                    "Copy & Paste",
                                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                            return new AnyUiLambdaActionNone();
                        }

                        // user feedback
                        Log.Singleton.Info($"Pasting {cpb.Items.Count} Submodels from paste buffer");

                        // loop over items
                        object nextBusObj = null;
                        List<CopyPasteItemBase> seq = cpb.Items;
                        if (buttonNdx == 3)
                            seq.Reverse();
                        foreach (var it in seq)
                        {
                            // access
                            var item = it as CopyPasteItemSubmodel;
                            if (item?.sm == null || (!cpb.Duplicate && item?.parentContainer == null))
                            {
                                Log.Singleton.Error("When pasting AAS elements, an element was invalid.");
                                continue;
                            }

                            // determine, what to clone
                            object src = item.sm;
                            if (typeof(T) == typeof(AdminShell.SubmodelRef))
                                src = item.smref;

                            if (src == null)
                            {
                                Log.Singleton.Error("When pasting AAS elements, an element was not determined.");
                                continue;
                            }

                            // check for equality
                            bool foundEqual = false;
                            if (checkEquality != null && src is T x)
                                foreach (var p in parentContainer)
                                    if (checkEquality(p, x))
                                        foundEqual = true;
                            if (foundEqual)
                            {
                                Log.Singleton.Error("When pasting AAS elements, an element was found to be " +
                                    "already existing.");
                                continue;
                            }

                            // apply
                            object entity2 = cloneEntity((T)src);
                            nextBusObj = entity2;

                            // different cases
                            if (buttonNdx == 2)
                                this.AddElementInListBefore<T>(parentContainer, (T)entity2, entity);
                            if (buttonNdx == 3)
                                this.AddElementInListAfter<T>(parentContainer, (T)entity2, entity);

                            // extra action
                            extraAction?.Invoke(it);

                            // may delete original
                            if (!cpb.Duplicate)
                            {
                                this.DeleteElementInList<T>(
                                        item.parentContainer as List<T>, (T)src, null);

                            }
                        }

                        // the buffer is tainted
                        cpb.Clear();

                        // try to focus
                        return new AnyUiLambdaActionRedrawAllElements(
                            nextFocus: nextBusObj, isExpanded: true);
                    }

                    if (buttonNdx == 4)
                    {
                        // which buffer?
                        var cbdata = context?.ClipboardGet();
                        var cpb = cpbInternal.CheckIfUseExternalCopyPasteBuffer(cbdata);

                        // content?
                        if (!cpb.ContentAvailable)
                        {
                            this.context?.MessageBoxFlyoutShow(
                                "No sufficient infomation in internal paste buffer or external clipboard.",
                                "Copy & Paste",
                                AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                            return new AnyUiLambdaActionNone();
                        }

                        // pasting above/ below means: Submodels
                        if (!cpb.Items.AllOfElementType<CopyPasteItemSME>())
                        {
                            this.context?.MessageBoxFlyoutShow(
                                    "No (valid) information for SubmodelsElements in copy/paste buffer.",
                                    "Copy & Paste",
                                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                            return new AnyUiLambdaActionNone();
                        }

                        // user feedback
                        Log.Singleton.Info($"Pasting {cpb.Items.Count} SubmodelElements from paste buffer");

                        // loop over items
                        object nextBusObj = null;
                        foreach (var it in cpb.Items)
                        {
                            // access
                            var item = it as CopyPasteItemSME;
                            if (item?.sme == null || item?.wrapper == null
                                || (!cpb.Duplicate && item?.parentContainer == null))
                            {
                                Log.Singleton.Error("When pasting SubmodelElements, an element was invalid.");
                                continue;
                            }

                            // apply
                            var smw2 = new AdminShell.SubmodelElementWrapper(item.sme, shallowCopy: false);
                            nextBusObj = smw2.submodelElement;

                            if (sm is AdminShell.IEnumerateChildren smeec)
                                smeec.AddChild(smw2);

                            // may delete original
                            if (!cpb.Duplicate)
                            {
                                DispDeleteCopyPasteItem(item);
                            }
                        }

                        // the buffer is tainted
                        cpb.Clear();

                        // try to focus
                        return new AnyUiLambdaActionRedrawAllElements(
                            nextFocus: nextBusObj, isExpanded: true);
                    }

                    return new AnyUiLambdaActionNone();
                });
        }

        public void DispPlainIdentifiableCutCopyPasteHelper<T>(
            AnyUiPanel stack,
            ModifyRepo repo,
            CopyPasteBuffer cpbInternal,
            List<T> parentContainer,
            T entity,
            Func<T, T> cloneEntity,
            string label = "Buffer:",
            Func<CopyPasteBuffer, bool> checkPasteInfo = null,
            Func<CopyPasteItemBase, bool, object> doPasteInto = null)
                where T : AdminShell.Identifiable, new()
        {
            // access
            if (parentContainer == null || cpbInternal == null || entity == null || cloneEntity == null)
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
                        cpbInternal.Clear();
                        cpbInternal.Valid = true;
                        cpbInternal.Duplicate = buttonNdx == 1;
                        cpbInternal.Items = new ListOfCopyPasteItem(
                            new CopyPasteItemIdentifiable(parentContainer, entity));
                        cpbInternal.CopyToClipboard(context, cpbInternal.Watermark);

                        // user feedback
                        Log.Singleton.Info(
                            StoredPrint.Color.Blue,
                            "Stored {0} '{1}' to internal buffer.{2}",
                            "" + entity.GetElementName(),
                            "" + entity.idShort,
                            cpbInternal.Duplicate
                                ? " Paste will duplicate."
                                : " Paste will cut at original position.");
                    }

                    if (buttonNdx == 2 || buttonNdx == 3)
                    {
                        // which buffer?
                        var cbdata = context?.ClipboardGet();
                        var cpb = cpbInternal.CheckIfUseExternalCopyPasteBuffer(cbdata);
                        if (!cpb.ContentAvailable)
                        {
                            this.context?.MessageBoxFlyoutShow(
                                "No sufficient infomation in internal paste buffer or external clipboard.",
                                "Copy & Paste",
                                AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                            return new AnyUiLambdaActionNone();
                        }

                        // pasting above/ below means: Submodels
                        if (!cpb.Items.AllOfElementType<CopyPasteItemIdentifiable>())
                        {
                            this.context?.MessageBoxFlyoutShow(
                                    "No (valid) information for Identifiables in copy/paste buffer.",
                                    "Copy & Paste",
                                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                            return new AnyUiLambdaActionNone();
                        }

                        // user feedback
                        Log.Singleton.Info($"Pasting {cpb.Items.Count} Identifiables from paste buffer");

                        // loop over items
                        object nextBusObj = null;
                        foreach (var it in cpb.Items)
                        {
                            // access
                            var item = it as CopyPasteItemIdentifiable;
                            if (item?.entity == null || (!cpb.Duplicate && item?.parentContainer == null))
                            {
                                Log.Singleton.Error("When pasting Identifiables, an element was invalid.");
                                continue;
                            }

                            // apply
                            object entity2 = cloneEntity((T)item.entity);
                            nextBusObj = entity2;

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

                            }
                        }

                        // the buffer is tainted
                        cpb.Clear();

                        // try to focus
                        return new AnyUiLambdaActionRedrawAllElements(
                            nextFocus: nextBusObj, isExpanded: true);
                    }

                    if (buttonNdx == 4)
                    {
                        // which buffer?
                        var cbdata = context?.ClipboardGet();
                        var cpb = cpbInternal.CheckIfUseExternalCopyPasteBuffer(cbdata);
                        if (cpb == null)
                        {
                            Log.Singleton.Error("Internal error in CheckIfUseExternalCopyPasteBuffer()");
                            return new AnyUiLambdaActionNone();
                        }

                        if (!cpb.ContentAvailable)
                        {
                            this.context?.MessageBoxFlyoutShow(
                                "No sufficient infomation in internal paste buffer or external clipboard.",
                                "Copy & Paste",
                                AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                            return new AnyUiLambdaActionNone();
                        }

                        // pasting above/ below means: Submodels
                        if (checkPasteInfo != null && !checkPasteInfo(cpb))
                        {
                            this.context?.MessageBoxFlyoutShow(
                                    "No (valid) information for Identifiables in copy/paste buffer.",
                                    "Copy & Paste",
                                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                            return new AnyUiLambdaActionNone();
                        }

                        // user feedback
                        Log.Singleton.Info($"Pasting {cpb.Items.Count} AAS elements from paste buffer");

                        // loop over items
                        object nextBusObj = null;
                        foreach (var it in cpb.Items)
                        {
                            // try
                            var obj = doPasteInto?.Invoke(it, !cpb.Duplicate);
                            if (obj == null)
                            {
                                Log.Singleton.Error("When pasting AAS elements, an element was invalid.");
                            }
                            else
                                nextBusObj = obj;
                        }

                        // the buffer is tainted
                        cpb.Clear();

                        // try to focus
                        return new AnyUiLambdaActionRedrawAllElements(
                            nextFocus: nextBusObj, isExpanded: true);
                    }

                    return new AnyUiLambdaActionNone();
                });
        }

        // resharper disable once UnusedTypeParameter
        public void DispPlainListOfIdentifiablePasteHelper<T>(
            AnyUiPanel stack,
            ModifyRepo repo,
            CopyPasteBuffer cpbInternal,
            string label = "Buffer:",
            Func<CopyPasteItemBase, bool, object> lambdaPasteInto = null)
                where T : AdminShell.Identifiable, new()
        {
            // access
            if (cpbInternal == null || lambdaPasteInto == null)
                return;

            // use an action
            this.AddAction(
                stack, label,
                new[] { "Paste into" }, repo,
                (buttonNdx) =>
                {
                    if (buttonNdx == 0)
                    {
                        // which buffer
                        var cbdata = context?.ClipboardGet();
                        var cpb = cpbInternal.CheckIfUseExternalCopyPasteBuffer(cbdata);
                        if (!cpb.ContentAvailable)
                        {
                            this.context?.MessageBoxFlyoutShow(
                                "No sufficient infomation in internal paste buffer or external clipboard.",
                                "Copy & Paste",
                                AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                            return new AnyUiLambdaActionNone();
                        }

                        // user feedback
                        Log.Singleton.Info($"Pasting {cpb.Items.Count} AAS elements from paste buffer");

                        // loop over items
                        object nextBusObj = null;
                        var doDelete = !cpb.Duplicate && !cpb.ExternalSource;
                        foreach (var it in cpb.Items)
                        {
                            // try
                            var obj = lambdaPasteInto(it, doDelete);
                            if (obj == null)
                            {
                                Log.Singleton.Error("When pasting AAS elements, an element was invalid.");
                            }
                            else
                                nextBusObj = obj;
                        }

                        // the buffer is tainted
                        cpb.Clear();

                        // try to focus
                        return new AnyUiLambdaActionRedrawAllElements(
                            nextFocus: nextBusObj, isExpanded: true);
                    }

                    return new AnyUiLambdaActionNone();
                });
        }
    }
}
