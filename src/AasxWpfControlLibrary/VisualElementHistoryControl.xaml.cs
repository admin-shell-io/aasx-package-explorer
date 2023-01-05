/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aas = AasCore.Aas3_0_RC02;
using AasxPackageLogic;
using AdminShellNS;
using Extensions;

namespace AasxPackageExplorer
{
    public class VisualElementHistoryItem
    {
        public VisualElementGeneric VisualElement = null;
        public string ReferableAasId = null;
        public Aas.Reference ReferableReference = null;

        public VisualElementHistoryItem(VisualElementGeneric VisualElement,
            string ReferableAasId = null, Aas.Reference ReferableReference = null)
        {
            this.VisualElement = VisualElement;
            this.ReferableAasId = ReferableAasId;
            this.ReferableReference = ReferableReference;
        }
    }

    public partial class VisualElementHistoryControl : UserControl
    {
        // members

        public event EventHandler<VisualElementHistoryItem> VisualElementRequested = null;

        public event EventHandler HomeRequested = null;

        private List<VisualElementHistoryItem> history = new List<VisualElementHistoryItem>();

        // init

        public VisualElementHistoryControl()
        {
            InitializeComponent();

            // initial state: without content
            buttonBack.IsEnabled = false;
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
            buttonBack.IsEnabled = false;
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
            Aas.Reference refref = null;

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
                refref = new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>() { new Aas.Key(Aas.KeyTypes.FragmentReference, "Plugin:" + vepe.theExt.Tag) });
            }

            // add, only if not already there
            if (history.Count < 1 || history[history.Count - 1].VisualElement != ve)
                history.Add(new VisualElementHistoryItem(ve, aasid, refref));

            // is enabled
            buttonBack.IsEnabled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == buttonBack)
            {
                // anything
                if (history == null || history.Count < 1)
                    return;

                // pop last (as this the already displayed one)
                history.RemoveAt(history.Count - 1);

                // may be disable ..
                buttonBack.IsEnabled = history.Count > 0;

                // give back the one prior to it
                if (history.Count < 1)
                    return;
                var ve = history[history.Count - 1];

                // trigger event
                this.VisualElementRequested?.Invoke(this, ve);
            }

            if (sender == buttonHome)
            {
                // just trigger
                this.HomeRequested?.Invoke(this, null);
            }
        }
    }
}
