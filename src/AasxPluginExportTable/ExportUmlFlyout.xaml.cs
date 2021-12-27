/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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
using Newtonsoft.Json;
using AasxPluginExportTable.Uml;

namespace AasxPluginExportTable
{
    public partial class ExportUmlFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        protected string _caption;

        public ExportUmlOptions Result = new ExportUmlOptions();

        //
        // Init
        //

        public ExportUmlFlyout(string caption = null)
        {
            InitializeComponent();
            _caption = caption;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // combo Formats
            ComboBoxFormat.Items.Clear();
            foreach (var f in ExportUmlOptions.FormatNames)
                ComboBoxFormat.Items.Add("" + f);

            // set given values
            ThisFromPreset(Result);            
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

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Result = null;
            ControlClosed?.Invoke();
        }

        //
        // Mechanics
        //

        private ExportUmlOptions ThisToPreset()
        {
            var x = new ExportUmlOptions();

            x.Format = (ExportUmlOptions.ExportFormat)ComboBoxFormat.SelectedIndex;

            if (int.TryParse(TextBoxLimitValues.Text, out int i))
                x.LimitInitialValue = i;

            x.CopyToPasteBuffer = CheckBoxCopyTo.IsChecked == true;

            return x;
        }

        private void ThisFromPreset(ExportUmlOptions preset)
        {
            // access
            if (preset == null)
                return;

            // take over
            ComboBoxFormat.SelectedIndex = (int)preset.Format;
            TextBoxLimitValues.Text = "" + preset.LimitInitialValue;
            CheckBoxCopyTo.IsChecked = preset.CopyToPasteBuffer;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonStart)
            {
                this.Result = this.ThisToPreset();
                ControlClosed?.Invoke();
            }
        }
    }
}
