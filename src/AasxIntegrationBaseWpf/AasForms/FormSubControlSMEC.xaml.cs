/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AdminShellNS;

namespace AasxIntegrationBase.AasForms
{
    public partial class FormSubControlSMEC : UserControl, IFormListControl
    {
        // Members

        /// <summary>
        /// Is true while <c>UpdateDisplay</c> takes place,
        /// in order to distinguish between user updates and program logic
        /// </summary>
        protected bool UpdateDisplayInCharge = false;

        // Constructors & access

        public FormSubControlSMEC()
        {
            InitializeComponent();
        }

        public class IndividualDataContext
        {
            public FormInstanceSubmodelElementCollection instance;
            public FormDescSubmodelElementCollection desc;
            public AdminShell.SubmodelElementCollection smec;

            public static IndividualDataContext CreateDataContext(object dataContext)
            {
                var dc = new IndividualDataContext();
                dc.instance = dataContext as FormInstanceSubmodelElementCollection;
                dc.desc = dc.instance?.desc as FormDescSubmodelElementCollection;
                dc.smec = dc.instance?.sme as AdminShell.SubmodelElementCollection;

                if (dc.instance == null || dc.desc == null || dc.smec == null)
                    return null;
                return dc;
            }
        }

        // External interfaces

        /// <summary>
        /// Set the property, given by enum <c>IFormListControlPropertyType</c> to the
        /// value
        /// </summary>
        public void SetProperty(IFormListControlPropertyType property, object value)
        {
            // access
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            // own properties

            // pass on
            // ReSharper disable SuspiciousTypeConversion.Global
            if (dc.instance.PairInstances != null)
                foreach (var pf in dc.instance.PairInstances)
                    if (pf != null && pf.instances != null && pf.instances.SubInstances != null)
                        foreach (var si in pf.instances.SubInstances)
                            if (si != null && si is IFormListControl)
                                (si as IFormListControl).SetProperty(property, value);
            // ReSharper enable SuspiciousTypeConversion.Global
        }

        /// <summary>
        /// Called from outside allows to show / collapse the detailed list view
        /// </summary>
        public void ShowContentItems(bool completelyVisible)
        {
            var tlc = this.TheListControl as IFormListControl;
            if (tlc != null)
                tlc.ShowContentItems(completelyVisible);
        }

        /// <summary>
        /// Tries to set the keyboard focus (cursor) to the first content field
        /// </summary>
        public void ContentFocus()
        {
            var tlc = this.TheListControl as IFormListControl;
            if (tlc != null)
                tlc.ContentFocus();
        }

        // Callbacks

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // save data context
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            // then update
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
            TextBlockIndex.Text = (!dc.instance.ShowIndex) ? "" : "#" + (1 + dc.instance.Index);

            // just assign the control
            TheListControl.DataContext = dc.instance;

            // release flag
            UpdateDisplayInCharge = false;
        }

    }
}
