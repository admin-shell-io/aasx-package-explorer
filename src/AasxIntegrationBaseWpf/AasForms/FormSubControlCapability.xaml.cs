/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// ReSharper disable UnusedType.Global

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
    public partial class FormSubControlCapability : UserControl
    {
        /// <summary>
        /// Is true while <c>UpdateDisplay</c> takes place, in order to distinguish between user updates and
        /// program logic
        /// </summary>
        protected bool UpdateDisplayInCharge = false;

        public class ViewModel : WpfViewModelBase
        {
            // for extension, see FormSubControlRelationshipElement
        }

        private ViewModel theViewModel = new ViewModel();

        public class IndividualDataContext
        {
            public FormInstanceCapability instance;
            public FormDescCapability desc;
            public AdminShell.Capability refElem;

            public static IndividualDataContext CreateDataContext(object dataContext)
            {
                var dc = new IndividualDataContext();
                dc.instance = dataContext as FormInstanceCapability;
                dc.desc = dc.instance?.desc as FormDescCapability;
                dc.refElem = dc.instance?.sme as AdminShell.Capability;

                if (dc.instance == null || dc.desc == null || dc.refElem == null)
                    return null;
                return dc;
            }
        }

        public FormSubControlCapability()
        {
            InitializeComponent();
            GridOuter.DataContext = this.theViewModel;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // save data context
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            // forward the data context to the base cub contol
            SubControlBase.DataContext = this.DataContext;

            // first update
            UpdateDisplay();
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

            // release flag
            UpdateDisplayInCharge = false;
        }

    }
}
