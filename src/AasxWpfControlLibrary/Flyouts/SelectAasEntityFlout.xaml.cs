/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AnyUi;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Aas = AasCore.Aas3_0;

namespace AasxPackageExplorer
{
    public partial class SelectAasEntityFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataSelectAasEntity DiaData = new AnyUiDialogueDataSelectAasEntity();

        PackageCentral packages = null;

        public SelectAasEntityFlyout(
            PackageCentral packages,
            PackageCentral.Selector? selector = null,
            string filter = null)
        {
            InitializeComponent();
            this.packages = packages;
            if (selector.HasValue)
                DiaData.Selector = selector.Value;
            if (filter != null)
                DiaData.Filter = filter;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayElements.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            DisplayElements.ActivateElementStateCache();

            // fill combo box
            ComboBoxFilter.Items.Add("All");
            foreach (var x in Enum.GetNames(typeof(Aas.KeyTypes)))
                ComboBoxFilter.Items.Add(x);

            // select an item
            if (DiaData.Filter != null)
                foreach (var x in ComboBoxFilter.Items)
                    if (x.ToString().Trim().ToLower() == DiaData.Filter.Trim().ToLower())
                    {
                        _disableValueCallbacks = true;
                        ComboBoxFilter.SelectedItem = x;
                        _disableValueCallbacks = false;
                        break;
                    }

            // fill contents
            FilterFor(DiaData.Filter);
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

        // automatic re-filtering might be a time driver
        protected bool _disableValueCallbacks = false;

        private void ComboBoxFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_disableValueCallbacks)
                return;

            if (sender == ComboBoxFilter)
            {
                DiaData.Filter = ComboBoxFilter.SelectedItem.ToString();
                if (DiaData.Filter == "All")
                    DiaData.Filter = null;
                // fill contents
                FilterFor(DiaData.Filter);
            }
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (DiaData?.PrepareResult(DisplayElements.SelectedItem, DiaData?.Filter) == true)
            {
                DiaData.Result = true;
                ControlClosed?.Invoke();
            }
        }

        private void FilterFor(string filter)
        {
            filter = AnyUiDialogueDataSelectAasEntity.ApplyFullFilterString(filter);
            DisplayElements.RebuildAasxElements(packages, DiaData.Selector, true, filter,
                // expandModePrimary: (filter?.ToLower().Contains("ConceptDescription") == true) ? 1 : 0,
                expandModePrimary: 1, expandModeAux: 0,
                lazyLoadingFirst: true);
        }


        private void DisplayElements_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DiaData?.PrepareResult(DisplayElements.SelectedItem, DiaData?.Filter) == true)
            {
                DiaData.Result = true;
                ControlClosed?.Invoke();
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DiaData.Result = false;
            ControlClosed?.Invoke();
        }
    }
}
