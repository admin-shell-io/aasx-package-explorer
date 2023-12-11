/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    /// <summary>
    /// Creates a flyout in order to select items from a list
    /// </summary>
    public partial class ShowValidationResultsFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        public IEnumerable ValidationItems = null;

        public bool FixSelected = false;

        public bool DoHint, DoWarning, DoSpecViolation, DoSchemaViolation;

        public ShowValidationResultsFlyout()
        {
            InitializeComponent();
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
        }

        public void LambdaActionAvailable(AnyUiLambdaActionBase la)
        {
        }

        //
        // Mechanics
        //

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // fill table
            DataGridTable.ItemsSource = ValidationItems;

        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            this.FixSelected = true;
            this.DoHint = this.CheckBoxHint.IsChecked == true;
            this.DoWarning = this.CheckBoxWarning.IsChecked == true;
            this.DoSpecViolation = this.CheckBoxSpecViolation.IsChecked == true;
            this.DoSchemaViolation = this.CheckBoxSchemaViolation.IsChecked == true;
            ControlClosed?.Invoke();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.FixSelected = false;
            ControlClosed?.Invoke();
        }

    }
}
