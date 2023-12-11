/*
Copyright (c) 2022 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2022 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Aas = AasCore.Aas3_0;

// ReSharper disable MethodHasAsyncOverload

namespace AasxPackageLogic
{
    /// <summary>
    /// Single item for <c>VisualElementHistoryStack</c>
    /// </summary>
    public class VisualElementHistoryItem
    {
        public VisualElementGeneric VisualElement = null;
        public string ReferableAasId = null;
        public Aas.IReference ReferableReference = null;

        public VisualElementHistoryItem(VisualElementGeneric VisualElement,
            string ReferableAasId = null, Aas.IReference ReferableReference = null)
        {
            this.VisualElement = VisualElement;
            this.ReferableAasId = ReferableAasId;
            this.ReferableReference = ReferableReference;
        }
    }

    /// <summary>
    /// This class realizes a stack of history references of AASes and elements.
    /// Can be used to realize a back / forth navigation of editing locations.
    /// </summary>
    public class VisualElementHistoryStack
    {
        // members

        public event EventHandler<VisualElementHistoryItem> VisualElementRequested = null;

        public event EventHandler<bool> HistoryActive = null;

        private List<VisualElementHistoryItem> history = new List<VisualElementHistoryItem>();

        // init

        public void Start()
        {
            // initial state: without content
            HistoryActive?.Invoke(null, false);
        }

        // functions

        /// <summary>
        /// Clears the history
        /// </summary>
        public void Clear()
        {
            // clear
            history.Clear();

            // not enabled
            HistoryActive?.Invoke(null, false);
        }

        public void Push(VisualElementGeneric ve)
        {
            // access
            if (ve == null)
                return;

            // for ve, try to find the AAS (in the parent hierarchy)
            var veAas = ve.FindAllParents((v) => { return v is VisualElementAdminShell; },
                includeThis: true).FirstOrDefault();

            // for ve, find the IReferable to be ve or superordinate ..
            var veRef = ve.FindAllParents((v) =>
            {
                var derefdo = v?.GetDereferencedMainDataObject();
                // success implies IGetReference as well
                return derefdo is Aas.IReferable;
            }, includeThis: true).FirstOrDefault();

            // check, if ve can identify a IReferable, to which a symbolic link can be done ..
            string aasid = null;
            Aas.IReference refref = null;

            if (veAas != null && veRef != null)
            {
                aasid = (veAas as VisualElementAdminShell)?.theAas?.Id;

                var derefdo = veRef.GetDereferencedMainDataObject();
                refref = (derefdo as Aas.IReferable)?.GetReference();
            }

            // some more special cases
            if (refref == null && ve is VisualElementConceptDescription vecd)
                refref = vecd.theCD?.GetReference();

            // found some referable Reference?
            if (refref == null)
                return;

            // in case of plug in, make it more specific
            if (ve is VisualElementPluginExtension vepe && vepe.theExt?.Tag != null)
            {
                refref = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>() { new Aas.Key(Aas.KeyTypes.FragmentReference, "Plugin:" + vepe.theExt.Tag) });
            }

            // add, only if not already there
            if (history.Count < 1 || history[history.Count - 1].VisualElement != ve)
                history.Add(new VisualElementHistoryItem(ve, aasid, refref));

            // is enabled
            HistoryActive?.Invoke(null, true);
        }

        public void Pop()
        {
            // anything
            if (history == null || history.Count < 1)
                return;

            // pop last (as this the already displayed one)
            history.RemoveAt(history.Count - 1);

            // may be disable ..
            HistoryActive?.Invoke(null, history.Count > 0);

            // give back the one prior to it
            if (history.Count < 1)
                return;
            var ve = history[history.Count - 1];

            // trigger event
            VisualElementRequested?.Invoke(this, ve);
        }
    }
}