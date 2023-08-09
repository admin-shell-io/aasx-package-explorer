/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPackageLogic;
using AnyUi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aas = AasCore.Aas3_0;

namespace AasxPackageExplorer
{
    //
    // Data model
    //

    // ReSharper disable ClassNeverInstantiated.Global .. used by JSON

    public class QualifierPreset
    {
        public string name = "";
        public Aas.Qualifier qualifier = new Aas.Qualifier("", Aas.DataTypeDefXsd.String);
    }

    // ReSharper enable ClassNeverInstantiated.Global

    public partial class SelectQualifierPresetFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataSelectQualifierPreset DiaData = new AnyUiDialogueDataSelectQualifierPreset();

        private List<QualifierPreset> thePresets = new List<QualifierPreset>();

        public SelectQualifierPresetFlyout(string presetFn)
        {
            InitializeComponent();

            try
            {
                var init = System.IO.File.ReadAllText(presetFn);
                thePresets = JsonConvert.DeserializeObject<List<QualifierPreset>>(init);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"While loading qualifier preset file ({presetFn})");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // fill listbox
            ListBoxPresets.Items.Clear();
            foreach (var p in thePresets)
                ListBoxPresets.Items.Add("" + p.name);
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

        private bool PrepareResult()
        {
            var i = ListBoxPresets.SelectedIndex;
            if (thePresets != null && i >= 0 && i < thePresets.Count)
            {
                DiaData.ResultQualifier = thePresets[i].qualifier;
                return true;
            }

            // uups
            return false;
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (PrepareResult())
            {
                DiaData.Result = true;
                ControlClosed?.Invoke();
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DiaData.Result = false;
            DiaData.ResultQualifier = null;
            ControlClosed?.Invoke();
        }

        private void ListBoxPresets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PrepareResult())
            {
                DiaData.Result = true;
                ControlClosed?.Invoke();
            }
        }
    }
}
