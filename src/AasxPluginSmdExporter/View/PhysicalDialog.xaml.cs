/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AasxIntegrationBase;

namespace AasxPluginSmdExporter.View
{
    /// <summary>
    /// Interaktionslogik für PhysicalDialog.xaml
    /// </summary>
    public partial class PhysicalDialog : UserControl
    {
        public PhysicalDialog()
        {
            InitializeComponent();
        }

        public string Result;

        public event IFlyoutControlAction ControlClosed;

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
        }

        public void ControlStart()
        {
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Result = null;
            if (ControlClosed != null) ControlClosed();
        }

        private void PhysicalClick(object sender, RoutedEventArgs e)
        {
            this.Result = "Physical";
            if (ControlClosed != null) ControlClosed();
        }

        private void SignalClick(object sender, RoutedEventArgs e)
        {
            this.Result = "Signal";
            if (ControlClosed != null) ControlClosed();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
