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
using AasCore.Aas3_0_RC02;
using AdminShellNS;

namespace AasxIntegrationBase.AasForms
{
    public partial class FormSubControlSMEBase : UserControl
    {
        /// <summary>
        /// Is true while <c>UpdateDisplay</c> takes place,
        /// in order to distinguish between user updates and program logic
        /// </summary>
        protected bool UpdateDisplayInCharge = false;

        public FormSubControlSMEBase()
        {
            InitializeComponent();
        }

        public class IndividualDataContext
        {
            public FormInstanceSubmodelElement instance;
            public FormDescSubmodelElement desc;
            public ISubmodelElement sme;

            public static IndividualDataContext CreateDataContext(object dataContext)
            {
                // this "header" (base) element uses the same context as the actual SME
                var dc = new IndividualDataContext();
                dc.instance = dataContext as FormInstanceSubmodelElement;
                dc.desc = dc.instance?.desc as FormDescSubmodelElement;
                dc.sme = dc.instance?.sme;

                if (dc.instance == null || dc.desc == null || dc.sme == null)
                    return null;
                return dc;
            }
        }

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
            if (dc?.desc == null || dc.sme == null)
                return;

            // set flag
            UpdateDisplayInCharge = true;

            // do something


            // release flag
            UpdateDisplayInCharge = false;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // access
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            // Resharper disable once RedundantJumpStatement
            if (dc == null)
                return;
        }
    }
}
