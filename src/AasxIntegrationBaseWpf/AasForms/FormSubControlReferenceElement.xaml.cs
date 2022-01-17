/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using AdminShellNS;

namespace AasxIntegrationBase.AasForms
{
    public partial class FormSubControlReferenceElement : UserControl
    {
        /// <summary>
        /// Is true while <c>UpdateDisplay</c> takes place, in order to distinguish between user updates and
        /// program logic
        /// </summary>
        protected bool UpdateDisplayInCharge = false;

        public class ViewModel : WpfViewModelBase
        {
            // data binded properties
            private AdminShell.Reference storedReference = null;
            public AdminShell.Reference StoredReference
            {
                get
                {
                    return storedReference;
                }
                set
                {
                    storedReference = value;
                    OnPropertyChanged("InfoReference");
                }
            }
            public string InfoReference
            {
                get
                {
                    if (storedReference == null)
                        return "(no reference set)";
                    if (storedReference.Count < 1)
                        return "(no Keys)";
                    return storedReference.ToString(format: 1, delimiter: Environment.NewLine);
                }
            }
        }

        private ViewModel theViewModel = new ViewModel();

        public class IndividualDataContext
        {
            public FormInstanceReferenceElement instance;
            public FormDescReferenceElement desc;
            public AdminShell.ReferenceElement refElem;

            public static IndividualDataContext CreateDataContext(object dataContext)
            {
                var dc = new IndividualDataContext();
                dc.instance = dataContext as FormInstanceReferenceElement;
                dc.desc = dc.instance?.desc as FormDescReferenceElement;
                dc.refElem = dc.instance?.sme as AdminShell.ReferenceElement;

                if (dc.instance == null || dc.desc == null || dc.refElem == null)
                    return null;
                return dc;
            }
        }

        public FormSubControlReferenceElement()
        {
            InitializeComponent();
            TextBlockReference.DataContext = this.theViewModel;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // save data context
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            // first update
            this.theViewModel.StoredReference = dc.refElem?.value;
            UpdateDisplay();

            // attach lambdas for clear
            ButtonClear.Click += (object sender5, RoutedEventArgs e5) =>
            {
                dc.instance.Touch();
                dc.refElem.value = null;
                this.theViewModel.StoredReference = dc.refElem.value;
            };

            // attach lambdas for select
            ButtonSelect.Click += (object sender6, RoutedEventArgs e6) =>
            {
                // try find topmost instance
                var top = FormInstanceHelper.GetTopMostParent(dc.instance);
                var topBase = top as FormInstanceBase;
                if (topBase != null && topBase.outerEventStack != null)
                {
                    // give over to event stack
                    var ev = new AasxIntegrationBase.AasxPluginResultEventSelectAasEntity();
                    ev.filterEntities = AdminShell.Key.AllElements;
                    ev.showAuxPackage = true;
                    ev.showRepoFiles = true;
                    topBase.outerEventStack.PushEvent(ev);

                    // .. and receive incoming event
                    topBase.subscribeForNextEventReturn = (revt) =>
                    {
                        if (revt is AasxPluginEventReturnSelectAasEntity rsel && rsel.resultKeys != null)
                        {
                            dc.instance.Touch();
                            dc.refElem.value = AdminShell.Reference.CreateNew(rsel.resultKeys);
                            this.theViewModel.StoredReference = dc.refElem.value;
                        }
                    };
                }

            };
        }

        private void UpdateDisplay()
        {
            // access
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            // set flag
            UpdateDisplayInCharge = true;

            // set plain fields
            TextBlockIndex.Text = (!dc.instance.ShowIndex) ? "" : "#" + (1 + dc.instance.Index);

            // note: the reference is already shown by data binding

            // release flag
            UpdateDisplayInCharge = false;
        }

    }
}
