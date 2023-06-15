/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AdminShellNS;
using AdminShellNS.Display;
using AnyUi;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Aas = AasCore.Aas3_0;

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

            public static CopyPasteItemBase FactoryConvertFrom(Aas.IReferable rf)
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
            public Aas.IIdentifiable entity = null;

            public override object GetMainDataObject() { return entity; }

            public CopyPasteItemIdentifiable() { }

            public CopyPasteItemIdentifiable(
                object parentContainer,
                Aas.IIdentifiable entity)
            {
                this.parentContainer = parentContainer;
                this.entity = entity;
            }

            public static CopyPasteItemIdentifiable ConvertFrom(Aas.IReferable rf)
            {
                // access
                var idf = rf as Aas.IIdentifiable;
                if (idf == null
                    || !(idf is Aas.AssetAdministrationShell
                         || idf is Aas.AssetInformation || idf is Aas.ConceptDescription))
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
            public Aas.IReference smref = null;
            public Aas.ISubmodel sm = null;

            public override object GetMainDataObject() { return sm; }

            public CopyPasteItemSubmodel() { }

            public CopyPasteItemSubmodel(
                object parentContainer,
                object entity,
                Aas.IReference smref,
                Aas.ISubmodel sm)
            {
                this.parentContainer = parentContainer;
                this.smref = smref;
                this.sm = sm;
                TryFixSmRefIfNull();
            }

            public void TryFixSmRefIfNull()
            {
                if (smref == null && sm?.Id != null)
                {
                    smref = new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>() { new Aas.Key(Aas.KeyTypes.Submodel, sm.Id) });
                }
            }

            public static CopyPasteItemSubmodel ConvertFrom(Aas.IReferable rf)
            {
                // access
                var sm = rf as Aas.Submodel;
                if (sm == null || sm.Id == null)
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
            public Aas.Environment env = null;
            public Aas.IReferable parentContainer = null;
            public Aas.ISubmodelElement wrapper = null;
            public Aas.ISubmodelElement sme = null;
            public EnumerationPlacmentBase Placement = null;

            public override object GetMainDataObject() { return sme; }


            public CopyPasteItemSME() { }

            public CopyPasteItemSME(
                Aas.Environment env,
                Aas.IReferable parentContainer, Aas.ISubmodelElement wrapper,
                Aas.ISubmodelElement sme,
                EnumerationPlacmentBase placement = null)
            {
                this.env = env;
                this.parentContainer = parentContainer;
                this.wrapper = wrapper;
                this.sme = sme;
                this.Placement = placement;
            }

            public static CopyPasteItemSME ConvertFrom(Aas.IReferable rf)
            {
                // access
                var sme = rf as Aas.ISubmodelElement;
                if (sme == null)
                    return null;

                // new wrapper
                Aas.ISubmodelElement wrapper;
                wrapper = sme;

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

            public static Tuple<string[], List<Aas.IKey>[]> PreparePresetsForListKeys(
                CopyPasteBuffer cpb, string label = "Paste")
            {
                // add from Copy Buffer
                List<Aas.IKey> bufferKey = null;
                if (cpb != null && cpb.Valid && cpb.Items != null && cpb.Items.Count == 1)
                {
                    if (cpb.Items[0] is CopyPasteItemIdentifiable cpbi && cpbi.entity?.Id != null)
                        bufferKey = new List<Aas.IKey>() { new Aas.Key((Aas.KeyTypes)Aas.Stringification.KeyTypesFromString(cpbi.entity.GetSelfDescription().AasElementName), cpbi.entity.Id) };

                    if (cpb.Items[0] is CopyPasteItemSubmodel cpbsm && cpbsm.sm?.SemanticId != null)
                        //bufferKey = List<Key>.CreateNew(cpbsm.sm.GetReference()?.First);
                        bufferKey = new List<Aas.IKey>() { cpbsm.sm.GetReference().Keys.First() };

                    if (cpb.Items[0] is CopyPasteItemSME cpbsme && cpbsme.sme != null
                        && cpbsme.env.Submodels != null)
                    {
                        // index parents for ALL Submodels -> parent for our SME shall be set by this ..
                        foreach (var sm in cpbsme.env?.Submodels)
                            sm?.SetAllParents();

                        // collect buffer list
                        bufferKey = new List<Aas.IKey>();
                        cpbsme.sme.CollectReferencesByParent(bufferKey);
                    }
                }

                // result
                return new Tuple<string[], List<Aas.IKey>[]>(
                    (bufferKey == null) ? null : new[] { label },
                    (bufferKey == null) ? null : new[] { bufferKey }
                );
            }

            private string PrepareClipboadString()
            {
#if V20
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
                    new[] { typeof(AasxIntegrationBase.AdminShellEvents.AasEventMsgEnvelope) });
                settings.TypeNameHandling = TypeNameHandling.None;
                settings.Formatting = Formatting.Indented;
                var json = JsonConvert.SerializeObject(objToSerialize, settings);
#else
                var oar = new List<Aas.IClass>();
                if (Items != null)
                    foreach (var it in Items)
                    {
                        var o = it.GetMainDataObject();
                        if (o is Aas.IClass oir)
                            oar.Add(oir);
                    }

                var nodes = ExtendIReferable.ToJsonObject(oar);
                var json = nodes.ToJsonString(
                    new System.Text.Json.JsonSerializerOptions()
                    {
                        WriteIndented = true
                    });
#endif

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
                        // TODO (MIHO, 2021-06-22): think of converting IReferable to IAasElement
                        var obj = AdminShellSerializationHelper.DeserializeFromJSON<Aas.IReferable>(cps);

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
                        //var objarr = AdminShellSerializationHelper
                        //    .DeserializePureObjectFromJSON<List<Aas.IReferable>>(cps);
                        var node = System.Text.Json.Nodes.JsonNode.Parse(cps);
                        var objarr = ExtendIReferable.ListOfIReferableFrom(node);
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
            if (item.parentContainer is Aas.Submodel pcsm && item.wrapper != null)
                this.DeleteElementInList<Aas.ISubmodelElement>(
                    pcsm.SubmodelElements, item.wrapper, null);

            if (item.parentContainer is Aas.SubmodelElementCollection pcsmc
                && item.wrapper != null)
                this.DeleteElementInList<Aas.ISubmodelElement>(
                    pcsmc.Value, item.wrapper, null);

            if (item.parentContainer is Aas.Operation pcop && item.wrapper != null)
            {
                var placement = pcop.GetChildrenPlacement(item.sme) as
                    EnumerationPlacmentOperationVariable;
                if (placement != null)
                //pcop[placement.Direction].Remove(placement.OperationVariable);
                {
                    if (placement.Direction == OperationVariableDirection.In)
                    {
                        pcop.InputVariables.Remove(placement.OperationVariable);
                    }
                    else if (placement.Direction == OperationVariableDirection.Out)
                    {
                        pcop.OutputVariables.Remove(placement.OperationVariable);
                    }
                    else if (placement.Direction == OperationVariableDirection.InOut)
                    {
                        pcop.InoutputVariables.Remove(placement.OperationVariable);
                    }
                }

            }
        }

        public void DispSmeCutCopyPasteHelper(
            AnyUiPanel stack,
            ModifyRepo repo,
            Aas.Environment env,
            Aas.IReferable parentContainer,
            CopyPasteBuffer cpbInternal,
            Aas.ISubmodelElement wrapper,
            Aas.ISubmodelElement sme,
            string label = "Buffer:",
            AasxMenu superMenu = null)
        {
            // access
            if (parentContainer == null || cpbInternal == null || sme == null)
                return;

            // use an action
            this.AddActionPanel(
                stack, label,
                repo: repo,
                ticketMenu: new AasxMenu()
                    .AddAction("aas-elem-cut", "Cut",
                        "Removes the currently selected element and places it in the paste buffer.",
                        inputGesture: "Ctrl+X")
                    .AddAction("aas-elem-copy", "Copy",
                        "Places the currently selected element in the paste buffer.",
                        inputGesture: "Ctrl+C")
                    .AddAction("aas-elem-paste-above", "Paste above",
                        "Adds the content of the paste buffer before (above) the currently selected element.",
                        inputGesture: "Ctrl+Shift+V")
                    .AddAction("aas-elem-paste-below", "Paste below",
                        "Adds the content of the paste buffer after (below) the currently selected element.",
                        inputGesture: "Ctrl+V")
                    .AddAction("aas-elem-paste-into", "Paste into",
                        "Adds the content of the paste buffer into the currently selected collection-like element.",
                        inputGesture: "Ctrl+Alt+V"),
                ticketAction: (buttonNdx, ticket) =>
                {
                    if (buttonNdx == 0 || buttonNdx == 1)
                    {
                        // store info
                        cpbInternal.Clear();
                        cpbInternal.Valid = true;
                        cpbInternal.Duplicate = buttonNdx == 1;
                        EnumerationPlacmentBase placement = null;
                        //if (parentContainer is IEnumerateChildren enc) //No IEnumerateChildren in V3
                        placement = parentContainer.GetChildrenPlacement(sme);
                        cpbInternal.Items = new ListOfCopyPasteItem(
                            new CopyPasteItemSME(env, parentContainer, wrapper, sme, placement));
                        cpbInternal.CopyToClipboard(context, cpbInternal.Watermark);

                        // special case?

                        // user feedback
                        Log.Singleton.Info(
                            StoredPrint.Color.Blue,
                            "Stored SubmodelElement '{0}'({1}) to internal buffer.{2}", "" + sme.IdShort,
                            "" + sme?.GetSelfDescription().AasElementName,
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
                            var smw2 = item.sme.Copy();
                            nextBusObj = smw2;
                            var createAtIndex = -1;

#if old
                            // make this unique (e.g. for event following)
                            if (cpb.Duplicate || cpb.ExternalSource)
                                this.MakeNewReferableUnique(smw2);
#endif
                            // make this unique later (e.g. for event following)
                            var makeUnique = (cpb.Duplicate || cpb.ExternalSource);

                            // insertation depends on parent container
                            if (buttonNdx == 2)
                            {
                                // handle parent explicitely, as not covered by AddElementInListBefore/after
                                smw2.Parent = parentContainer;

                                if (parentContainer is Aas.Submodel pcsm && wrapper != null)
                                    createAtIndex = this.AddElementInSmeListBefore<Aas.ISubmodelElement>(
                                        pcsm.SubmodelElements, smw2, wrapper, makeUnique);

                                if (parentContainer is Aas.SubmodelElementCollection pcsmc && wrapper != null)
                                    createAtIndex = this.AddElementInSmeListBefore<Aas.ISubmodelElement>(
                                        pcsmc.Value, smw2, wrapper, makeUnique);

                                if (parentContainer is Aas.SubmodelElementList pcsml && wrapper != null)
                                    createAtIndex = this.AddElementInSmeListBefore<Aas.ISubmodelElement>(
                                        pcsml.Value, smw2, wrapper, makeUnique);

                                if (parentContainer is Aas.Entity pcent && wrapper != null)
                                    createAtIndex = this.AddElementInSmeListBefore<Aas.ISubmodelElement>(
                                        pcent.Statements, smw2, wrapper, makeUnique);

                                if (parentContainer is Aas.AnnotatedRelationshipElement pcarel &&
                                        wrapper != null)
                                {
                                    var annotations = new List<Aas.ISubmodelElement>(pcarel.Annotations);
                                    createAtIndex = this.AddElementInSmeListBefore<Aas.ISubmodelElement>(
                                        annotations, smw2, wrapper, makeUnique);
                                }

                                // TODO (Michael Hoffmeister, 2020-08-01): Operation complete?
                                if (parentContainer is Aas.Operation pcop && wrapper != null)
                                {
                                    var place = pcop.GetChildrenPlacement(wrapper) as
                                        EnumerationPlacmentOperationVariable;
                                    if (place?.OperationVariable != null)
                                    {
                                        var op = new Aas.OperationVariable(smw2);
                                        var opVariables = pcop.GetVars(place.Direction);
                                        createAtIndex = this.AddElementInListBefore<Aas.IOperationVariable>(
                                            opVariables, op, place.OperationVariable);
                                        nextBusObj = op;
                                    }
                                }
                            }

                            if (buttonNdx == 3)
                            {
                                // handle parent explicitely, as not covered by AddElementInListBefore/after
                                smw2.Parent = parentContainer;

                                if (parentContainer is Aas.Submodel pcsm && wrapper != null)
                                    createAtIndex = this.AddElementInSmeListAfter<Aas.ISubmodelElement>(
                                        pcsm.SubmodelElements, smw2, wrapper, makeUnique);

                                if (parentContainer is Aas.SubmodelElementCollection pcsmc && wrapper != null)
                                    createAtIndex = this.AddElementInSmeListAfter<Aas.ISubmodelElement>(
                                        pcsmc.Value, smw2, wrapper, makeUnique);

                                if (parentContainer is Aas.SubmodelElementList pcsml && wrapper != null)
                                    createAtIndex = this.AddElementInSmeListAfter<Aas.ISubmodelElement>(
                                        pcsml.Value, smw2, wrapper, makeUnique);

                                if (parentContainer is Aas.Entity pcent && wrapper != null)
                                    createAtIndex = this.AddElementInSmeListAfter<Aas.ISubmodelElement>(
                                        pcent.Statements, smw2, wrapper, makeUnique);

                                if (parentContainer is Aas.AnnotatedRelationshipElement pcarel &&
                                        wrapper != null)
                                {
                                    var annotations = new List<Aas.ISubmodelElement>(pcarel.Annotations);
                                    createAtIndex = this.AddElementInSmeListAfter<Aas.ISubmodelElement>(
                                        annotations, smw2, wrapper, makeUnique);
                                }

                                // TODO (Michael Hoffmeister, 2020-08-01): Operation complete?
                                if (parentContainer is Aas.Operation pcop && wrapper != null)
                                {
                                    var place = pcop.GetChildrenPlacement(wrapper) as
                                        EnumerationPlacmentOperationVariable;
                                    if (place?.OperationVariable != null)
                                    {
                                        var op = new Aas.OperationVariable(smw2);
                                        var opVariables = pcop.GetVars(place.Direction);
                                        createAtIndex = this.AddElementInListAfter<Aas.IOperationVariable>(
                                            opVariables, op, place.OperationVariable);
                                        nextBusObj = op;
                                    }
                                }
                            }

                            if (buttonNdx == 4)
                            {
                                if (makeUnique)
                                {
                                    var found = false;
                                    foreach (var ch in sme.DescendOnce().OfType<Aas.ISubmodelElement>())
                                        if (ch?.IdShort?.Trim() == smw2?.IdShort?.Trim())
                                            found = true;
                                    if (found)
                                        this.MakeNewReferableUnique(smw2);
                                }

                                // aprent set automatically
                                // TODO (MIHO, 2021-08-18): createAtIndex missing here
                                sme.AddChild(smw2, item.Placement);
                            }

                            // emit event
                            this.AddDiaryEntry(smw2,
                                new DiaryEntryStructChange(StructuralChangeReason.Create,
                                    createAtIndex: createAtIndex));

                            // may delete original
                            if (!cpb.Duplicate && !cpb.ExternalSource)
                            {
                                DispDeleteCopyPasteItem(item);

                                this.AddDiaryEntry(item.sme,
                                    new DiaryEntryStructChange(StructuralChangeReason.Delete));
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
            Aas.IReference smref,
            Aas.ISubmodel sm,
            string label = "Buffer:",
            Func<T, T, bool> checkEquality = null,
            Action<T, bool> modifyAfterClone = null,
            Action<CopyPasteItemBase> extraAction = null,
            AasxMenu superMenu = null) /*where T : new()*/ //TODO:jtikekar Test
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
            this.AddActionPanel(
                stack, label,
                repo: repo,
                superMenu: superMenu,
                ticketMenu: new AasxMenu()
                    .AddAction("aas-elem-cut", "Cut",
                        "Removes the currently selected element and places it in the paste buffer.",
                        inputGesture: "Ctrl+X")
                    .AddAction("aas-elem-copy", "Copy",
                        "Places the currently selected element in the paste buffer.",
                        inputGesture: "Ctrl+C")
                    .AddAction("aas-elem-paste-above", "Paste above",
                        "Adds the content of the paste buffer before (above) the currently selected element.",
                        inputGesture: "Ctrl+Shift+V")
                    .AddAction("aas-elem-paste-below", "Paste below",
                        "Adds the content of the paste buffer after (below) the currently selected element.",
                        inputGesture: "Ctrl+V")
                    .AddAction("aas-elem-paste-into", "Paste into",
                        "Adds the content of the paste buffer into the currently selected collection-like element.",
                        inputGesture: "Ctrl+Alt+V"),
                ticketAction: (buttonNdx, ticket) =>
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
                            "Stored Submodel '{0}' to internal buffer.{1}", "" + sm.IdShort,
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
                            if (typeof(T).IsAssignableFrom(typeof(Aas.IReference)))
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
                            if (foundEqual && modifyAfterClone == null)
                            {
                                // if modifyAfterClone cannot change, abort!
                                Log.Singleton.Error("When pasting AAS elements, an element was found to be " +
                                    "already existing.");
                                continue;
                            }

                            // apply
                            object entity2 = cloneEntity((T)src);
                            nextBusObj = entity2;

                            // modify
                            modifyAfterClone?.Invoke((T)entity2, foundEqual);

                            // emit event
                            this.AddDiaryEntry(entity2 as Aas.IReferable,
                                new DiaryEntryStructChange(StructuralChangeReason.Create));

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

                                this.AddDiaryEntry(src as Aas.IReferable,
                                    new DiaryEntryStructChange(StructuralChangeReason.Delete));
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
                            var smw2 = item.sme.Copy();
                            nextBusObj = smw2;

                            //if (sm is IEnumerateChildren smeec)
                            //    smeec.AddChild(smw2);

                            sm.AddChild(smw2);

                            // emit event
                            this.AddDiaryEntry(item.sme, new DiaryEntryStructChange(StructuralChangeReason.Create));

                            // may delete original
                            if (!cpb.Duplicate)
                            {
                                DispDeleteCopyPasteItem(item);

                                this.AddDiaryEntry(item.sme,
                                    new DiaryEntryStructChange(StructuralChangeReason.Delete));
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
            Func<CopyPasteItemBase, bool, object> doPasteInto = null,
            AasxMenu superMenu = null)
                where T : Aas.IIdentifiable/*, new()*/ //TODO:jtikekar Test
        {
            // access
            if (parentContainer == null || cpbInternal == null || entity == null || cloneEntity == null)
                return;

            // use an action
            this.AddActionPanel(
                stack, label,
                repo: repo,
                superMenu: superMenu,
                ticketMenu: new AasxMenu()
                    .AddAction("aas-elem-cut", "Cut",
                        "Removes the currently selected element and places it in the paste buffer.",
                        inputGesture: "Ctrl+X")
                    .AddAction("aas-elem-copy", "Copy",
                        "Places the currently selected element in the paste buffer.",
                        inputGesture: "Ctrl+C")
                    .AddAction("aas-elem-paste-above", "Paste above",
                        "Adds the content of the paste buffer before (above) the currently selected element.",
                        inputGesture: "Ctrl+Shift+V")
                    .AddAction("aas-elem-paste-below", "Paste below",
                        "Adds the content of the paste buffer after (below) the currently selected element.",
                        inputGesture: "Ctrl+V")
                    .AddAction("aas-elem-paste-into", "Paste into",
                        "Adds the content of the paste buffer into the currently selected collection-like element.",
                        inputGesture: "Ctrl+Alt+V"),
                ticketAction: (buttonNdx, ticket) =>
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
                            "" + entity.GetSelfDescription().AasElementName,
                            "" + entity.IdShort,
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

                            // make this pseudo-unique
                            this.MakeNewIdentifiableUnique((T)entity2);

                            // different cases
                            int ndx = -1;
                            if (buttonNdx == 2)
                                ndx = this.AddElementInListBefore<T>(parentContainer, (T)entity2, entity);
                            if (buttonNdx == 3)
                                ndx = this.AddElementInListAfter<T>(parentContainer, (T)entity2, entity);

                            // Identifiable: just state as newly created
                            this.AddDiaryEntry((T)entity2, new DiaryEntryStructChange(
                                AasxIntegrationBase.AdminShellEvents.StructuralChangeReason.Create,
                                createAtIndex: ndx));

                            // may delete original
                            if (!cpb.Duplicate)
                            {
                                this.DeleteElementInList<T>(
                                        item.parentContainer as List<T>, (T)item.entity, null);

                                this.AddDiaryEntry((T)entity, new DiaryEntryStructChange(
                                    AasxIntegrationBase.AdminShellEvents.StructuralChangeReason.Delete));
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
            Func<CopyPasteItemBase, bool, object> lambdaPasteInto = null,
            AasxMenu superMenu = null)
                where T : Aas.IIdentifiable/*, new()*/   //TODO: jtikekar test
        {
            // access
            if (cpbInternal == null || lambdaPasteInto == null)
                return;

            // use an action
            this.AddActionPanel(
                stack, label,
                repo: repo,
                superMenu: superMenu,
                ticketMenu: new AasxMenu()
                    .AddAction("aas-elem-paste-into", "Paste into",
                        "Adds the content of the paste buffer into the currently selected collection-like element.",
                        inputGesture: "Ctrl+Alt+V"),
                ticketAction: (buttonNdx, ticket) =>
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
