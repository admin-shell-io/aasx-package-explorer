/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
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
    public partial class SelectFromListFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataSelectFromList DiaData = new AnyUiDialogueDataSelectFromList();

        public SelectFromListFlyout()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // fill caption
            if (DiaData.Caption != null)
                TextBlockCaption.Text = "" + DiaData.Caption;

            // fill listbox
            ListBoxPresets.Items.Clear();
            foreach (var loi in DiaData.ListOfItems)
                ListBoxPresets.Items.Add("" + loi.Text);

            // alternative buttons
            if (DiaData.AlternativeSelectButtons != null)
            {
                this.ButtonsPanel.Children.Clear();
                foreach (var txt in DiaData.AlternativeSelectButtons)
                {
                    var b = new Button();
                    b.Content = "" + txt;
                    b.Foreground = Brushes.White;
                    b.FontSize = 18;
                    b.Padding = new Thickness(4);
                    b.Margin = new Thickness(4);
                    this.ButtonsPanel.Children.Add(b);
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


        private bool PrepareResult()
        {
            var i = ListBoxPresets.SelectedIndex;
            if (DiaData.ListOfItems != null && i >= 0 && i < DiaData.ListOfItems.Count)
            {
                DiaData.ResultIndex = i;
                DiaData.ResultItem = DiaData.ListOfItems[i];
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
