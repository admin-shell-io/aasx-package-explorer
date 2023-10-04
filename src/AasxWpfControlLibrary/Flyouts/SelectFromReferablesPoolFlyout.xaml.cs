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
using AasxPackageLogic;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    /// <summary>
    /// Creates a flyout in order to select items from a list
    /// </summary>
    public partial class SelectFromReferablesPoolFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        public AasxPredefinedConcepts.DefinitionsPool DataSourcePools = null;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataSelectReferableFromPool DiaData = new AnyUiDialogueDataSelectReferableFromPool();

        public SelectFromReferablesPoolFlyout(AasxPredefinedConcepts.DefinitionsPool dataSourcePools)
        {
            InitializeComponent();
            DataSourcePools = dataSourcePools;
        }

        protected static object _lastDomainSelected = null;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // fill caption
            if (DiaData.Caption != null)
                TextBlockCaption.Text = "" + DiaData.Caption;

            // entities
            DataGridEntities.Items.Clear();

            // fill listbox
            this.ListBoxDomains.Items.Clear();
            if (DataSourcePools != null)
            {
                var domains = DataSourcePools.GetDomains().ToList();
                domains.Sort();
                foreach (var d in domains)
                    this.ListBoxDomains.Items.Add(d);

                if (_lastDomainSelected != null)
                {
                    var i = this.ListBoxDomains.Items.IndexOf(_lastDomainSelected);
                    this.ListBoxDomains.SelectedIndex = i;
                }
                else
                {
                    if (this.ListBoxDomains.Items.Count > 0)
                        this.ListBoxDomains.SelectedIndex = 0;
                }
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

        public void LambdaActionAvailable(AnyUiLambdaActionBase la)
        {
        }

        //
        // Mechanics
        //


        private void ListBoxDomains_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dom = this.ListBoxDomains.SelectedItem as string;
            if (dom == null)
                return;
            var ld = this.DataSourcePools?.GetEntitiesForDomain(dom)?.ToList();
            if (ld != null)
            {
                ld.Sort((x1, x2) => x1.DisplayName.CompareTo(x2.DisplayName));
                DataGridEntities.Items.Clear();
                foreach (var ent in ld)
                    DataGridEntities.Items.Add(ent);
                _lastDomainSelected = ListBoxDomains.SelectedItem;
            }
        }

        private bool PrepareResult()
        {
            if (DataGridEntities.SelectedItem != null && DataGridEntities.SelectedIndex >= 0)
            {
                DiaData.ResultIndex = DataGridEntities.SelectedIndex;
                DiaData.ResultItem = DataGridEntities.SelectedItem;
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
            DiaData.ResultIndex = -1;
            DiaData.ResultItem = null;
            ControlClosed?.Invoke();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PrepareResult())
            {
                DiaData.Result = true;
                ControlClosed?.Invoke();
            }
        }

    }
}
