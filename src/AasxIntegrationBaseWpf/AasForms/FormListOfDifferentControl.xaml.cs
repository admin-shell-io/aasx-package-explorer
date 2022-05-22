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
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;

namespace AasxIntegrationBase.AasForms
{
    public partial class FormListOfDifferentControl : UserControl, IFormListControl
    {
        // Members

        /// <summary>
        /// Show Submodel or SMEC header in form
        /// </summary>
        public bool ShowHeader = true;

        /// <summary>
        /// Show less information in header
        /// </summary>
        public bool FormSmaller = false;

        // Constructor and access

        public class IndividualDataContext
        {
            public FormInstanceSubmodel smInst;
            public FormInstanceSubmodelElementCollection smecInst;

            public FormDescSubmodel smDesc;
            public FormDescSubmodelElementCollection smecDesc;

            public FormDescListOfElement listOfElements;

            public static IndividualDataContext CreateDataContext(object dataContext)
            {
                var dc = new IndividualDataContext();

                dc.smInst = dataContext as FormInstanceSubmodel;
                dc.smecInst = dataContext as FormInstanceSubmodelElementCollection;

                dc.smDesc = dc.smInst?.desc as FormDescSubmodel;
                dc.smecDesc = dc.smecInst?.desc as FormDescSubmodelElementCollection;

                var directLoE = dataContext as FormDescListOfElement;

                if (dc.smInst != null && dc.smInst.desc != null && dc.smInst.desc is FormDescSubmodel fdsm)
                    dc.listOfElements = fdsm.SubmodelElements;

                if (dc.smecInst != null && dc.smecInst.desc != null &&
                    dc.smecInst.desc is FormDescSubmodelElementCollection fdsmc)
                    dc.listOfElements = fdsmc.value;

                if (directLoE != null)
                    dc.listOfElements = directLoE;

                if (dc.listOfElements == null)
                    return null;

                return dc;
            }
        }

        public FormListOfDifferentControl()
        {
            InitializeComponent();
        }

        // External interfaces

        /// <summary>
        /// Set the property, given by enum <c>IFormListControlPropertyType</c> to the
        /// value
        /// </summary>
        public void SetProperty(IFormListControlPropertyType property, object value)
        {
            // need data context of the UC coming from Submodel, SMEC or at least listOfElements
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null || StackPanelElements == null)
                return;

            // own properties?
            if (property == IFormListControlPropertyType.FormSmaller && value is bool
                && TextBlockHeaderFormInfo != null)
                TextBlockHeaderFormInfo.Visibility = (bool)value ? Visibility.Visible : Visibility.Collapsed;

            // pass on
#if USE_WPF
            FormInstanceListOfDifferent lod = null;
            if (dc.smInst != null)
                lod = dc.smInst.PairInstances;
            if (dc.smecInst != null)
                lod = dc.smecInst.PairInstances;
            if (lod != null && lod.Count > 0)
                foreach (var ld in lod)
                    if (ld.instances != null && ld.instances.subControl != null &&
                        ld.instances.subControl is FormListOfSameControl flsc)
                        flsc.SetProperty(property, value);
#endif
        }

        /// <summary>
        /// Called from outside allows to show / collapse the detailed list view
        /// </summary>
        public void ShowContentItems(bool completelyVisible)
        {
            // access
            var spe = StackPanelElements;
            var bic = ButtonInstanceCollapse;
            if (spe == null || bic == null)
                return;

            // very stupid toggle
            if (completelyVisible)
            {
                spe.Visibility = Visibility.Visible;
                bic.Content = "\u25bc";
            }
            else
            {
                spe.Visibility = Visibility.Collapsed;
                bic.Content = "\u25b6";
            }
        }

        /// <summary>
        /// Tries to set the keyboard focus (cursor) to the first content field
        /// </summary>
        public void ContentFocus()
        {
            // need data context of the UC coming from Submodel, SMEC or at least listOfElements
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null || StackPanelElements == null)
                return;

#if USE_WPF
            FormInstanceListOfDifferent lod = null;
            if (dc.smInst != null)
                lod = dc.smInst.PairInstances;
            if (dc.smecInst != null)
                lod = dc.smecInst.PairInstances;
            if (lod != null && lod.Count > 0 && lod[0].instances != null
                && lod[0].instances.SubInstances != null && lod[0].instances.SubInstances.Count > 0)
            {
                var sc00 = lod[0].instances.SubInstances[0].subControl;
                if (sc00 != null && sc00 is IFormListControl)
                    (sc00 as IFormListControl).ContentFocus();
            }
#endif
        }

        // Display functionality

        private void UpdateDisplay()
        {
            // need data context of the UC coming from Submodel, SMEC or at least listOfElements
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null || StackPanelElements == null)
                return;

            // simply populate
            StackPanelElements.Children.Clear();
            GridHeader.Visibility = Visibility.Collapsed;

            // Form tools only visible on top level
            GridToolbar.Visibility = Visibility.Collapsed;

            // Submodel
            if (dc.smInst != null && dc.smDesc != null)
            {
                // toolbar
                if (dc.smInst.parentInstance == null)
                    GridToolbar.Visibility = Visibility.Visible;

                // header
                if (ShowHeader)
                {
                    GridHeader.Visibility = Visibility.Visible;
                    GridHeader.Background = System.Windows.Media.Brushes.White;
                    TextBlockHeaderFormTitle.Text =
                        (dc.smDesc.FormTitle.Trim().Length < 1) ? "Submodel" : dc.smDesc.FormTitle;
                    TextBlockHeaderFormInfo.Text = dc.smDesc.FormInfo;
                }

                // populate elements
                foreach (var pair in dc.smInst.PairInstances)
                {
                    var cntl = new FormListOfSameControl();
#if USE_WPF
                    pair.instances.subControl = cntl;
#endif
                    cntl.DataContext = pair.instances;
                    StackPanelElements.Children.Add(cntl);
                }
            }

            // SMEC
            if (dc.smecInst != null && dc.smecDesc != null)
            {
                // toolbar
                if (dc.smecInst.parentInstance == null)
                    GridToolbar.Visibility = Visibility.Visible;

                // header
                if (ShowHeader)
                {
                    GridHeader.Visibility = Visibility.Visible;
                    GridHeader.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xf0, 0xf0, 0xf0));
                    TextBlockHeaderFormTitle.Text = (dc.smecDesc.FormTitle.Trim().Length < 1)
                        ? "SubmodelElementCollection"
                        : dc.smecDesc.FormTitle;
                    TextBlockHeaderFormInfo.Text = dc.smecDesc.FormInfo;
                }

                // populate elements
                foreach (var pair in dc.smecInst.PairInstances)
                {
                    var cntl = new FormListOfSameControl();
#if USE_WPF
                    pair.instances.subControl = cntl;
#endif
                    cntl.DataContext = pair.instances;
                    StackPanelElements.Children.Add(cntl);
                }
            }

        }

        // Callbacks

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // need data context of the UC coming from Submodel, SMEC or at least listOfElements
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null || StackPanelElements == null)
                return;

            UpdateDisplay();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // access
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            if (sender == ButtonInstanceCollapse)
            {
                // access
                var spe = StackPanelElements;
                if (spe == null)
                    return;

                // very stupid toggle
                if (spe.Visibility == Visibility.Collapsed || spe.Visibility == Visibility.Hidden)
                    this.ShowContentItems(true);
                else
                    this.ShowContentItems(false);
            }

            if (sender == ButtonFormSmaller)
            {
                this.FormSmaller = !this.FormSmaller;
                this.SetProperty(IFormListControlPropertyType.FormSmaller, this.FormSmaller);
            }
        }
    }
}
