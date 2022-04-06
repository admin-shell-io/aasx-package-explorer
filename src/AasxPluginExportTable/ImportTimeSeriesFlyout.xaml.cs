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
using AasxPluginExportTable.TimeSeries;
using AasxPluginExportTable.Uml;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPluginExportTable
{
    public partial class ImportTimeSeriesFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        protected string _caption;

        public ImportTimeSeriesOptions Result = new ImportTimeSeriesOptions();

        //
        // Init
        //

        public ImportTimeSeriesFlyout(string caption = null)
        {
            InitializeComponent();
            _caption = caption;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // combo Formats
            ComboBoxFormat.Items.Clear();
            foreach (var f in ImportTimeSeriesOptions.FormatNames)
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

        private ImportTimeSeriesOptions ThisToPreset()
        {
            var x = new ImportTimeSeriesOptions();

            x.Format = (ImportTimeSeriesOptions.FormatEnum)ComboBoxFormat.SelectedIndex;

            int i = 0;

            if (int.TryParse(TextBoxRowHeader.Text, out i))
                x.RowData = i;

            if (int.TryParse(TextBoxRowData.Text, out i))
                x.RowData = i;

            if (int.TryParse(TextBoxColumnTime.Text, out i))
                x.ColTime = i;

            if (int.TryParse(TextBoxColumnData.Text, out i))
                x.ColData = i;

            if (int.TryParse(TextBoxNumData.Text, out i))
                x.NumData = i;

            x.SetSmSemantic = CheckBoxSetSmSemId.IsChecked == true;

            return x;
        }

        private void ThisFromPreset(ImportTimeSeriesOptions preset)
        {
            // access
            if (preset == null)
                return;

            // take over
            ComboBoxFormat.SelectedIndex = (int)preset.Format;
            TextBoxRowHeader.Text = "" + preset.RowHeader;
            TextBoxRowData.Text = "" + preset.RowData;
            TextBoxColumnTime.Text = "" + preset.ColTime;
            TextBoxColumnData.Text = "" + preset.ColData;
            TextBoxNumData.Text = "" + preset.NumData;
            CheckBoxSetSmSemId.IsChecked = preset.SetSmSemantic;
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
