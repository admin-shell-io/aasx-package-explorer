/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AasxIntegrationBase;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    //
    // Data model
    //

    // ReSharper disable ClassNeverInstantiated.Global .. used by JSON

    public class QualifierPreset
    {
        public string name = "";
        public AdminShell.Qualifier qualifier = new AdminShell.Qualifier();
    }

    // ReSharper enable ClassNeverInstantiated.Global

    public partial class SelectQualifierPresetFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        public AdminShell.Qualifier ResultQualifier = null;

        private List<QualifierPreset> ThePresets = new List<QualifierPreset>();

        public SelectQualifierPresetFlyout(string presetFn)
        {
            InitializeComponent();

            try
            {
                var init = File.ReadAllText(presetFn);
                ThePresets = JsonConvert.DeserializeObject<List<QualifierPreset>>(init);
            }
            catch (Exception ex)
            {
                AasxPackageExplorer.Log.Singleton.Error(ex, $"While loading qualifier preset file ({presetFn})");
            }
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

        //
        // Mechanics
        //

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // fill listbox
            ListBoxPresets.Items.Clear();
            foreach (var p in ThePresets)
                ListBoxPresets.Items.Add("" + p.name);
        }

        private bool PrepareResult()
        {
            var i = ListBoxPresets.SelectedIndex;
            if (ThePresets != null && i >= 0 && i < ThePresets.Count)
            {
                this.ResultQualifier = ThePresets[i].qualifier;
                return true;
            }

            // uups
            return false;
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (PrepareResult())
                ControlClosed?.Invoke();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.ResultQualifier = null;
            ControlClosed?.Invoke();
        }

        private void ListBoxPresets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PrepareResult())
                ControlClosed?.Invoke();
        }
    }
}
