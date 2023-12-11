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
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AasxWpfControlLibrary.PackageCentral;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    public partial class SelectFromRepositoryFlyout : UserControl, IFlyoutControl
    {
        public AnyUiDialogueDataSelectFromRepository DiaData = new AnyUiDialogueDataSelectFromRepository();

        public event IFlyoutControlAction ControlClosed;

        public SelectFromRepositoryFlyout()
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

        private bool LoadAasxRepoFile(IEnumerable<PackageContainerRepoItem> items)
        {
            if (items == null)
                return false;

            try
            {
                // rework buttons
                this.StackPanelTags.Children.Clear();
                int numButtons = 0;
                foreach (var ri in items)
                {
                    var tag = ri.Tag.Trim();
                    numButtons++;
                    if (tag != "" && numButtons < AnyUiDialogueDataSelectFromRepository.MaxButtonsToShow)
                    {
                        var b = new Button();
                        b.Style = (Style)FindResource("TranspRoundCorner");
                        b.Content = "" + tag;
                        b.Height = 40;
                        b.Width = 40;
                        b.Margin = new Thickness(5, 0, 5, 0);
                        b.Foreground = Brushes.White;
                        b.Click += TagButton_Click;
                        this.StackPanelTags.Children.Add(b);
                        b.Tag = ri;
                    }
                }

            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                return false;
            }

            return true;
        }

        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            if (b?.Tag != null && DiaData?.Items != null && DiaData.Items.Contains(b.Tag))
            {
                DiaData.Result = true;
                DiaData.ResultItem = b.Tag as PackageContainerRepoItem;
                ControlClosed?.Invoke();
            }
        }

        //
        // Mechanics
        //

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DiaData.Result = false;
            DiaData.ResultItem = null;
            ControlClosed?.Invoke();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // load items
            LoadAasxRepoFile(DiaData?.Items);

            // window default
            this.TextBoxAssetId.Text = "";
            this.TextBoxAssetId.Focus();
            this.TextBoxAssetId.Select(0, 999);
            FocusManager.SetFocusedElement(this, this.TextBoxAssetId);
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            // search
            var ri = DiaData?.SearchId(TextBoxAssetId.Text);
            if (ri != null)
            {
                DiaData.Result = true;
                DiaData.ResultItem = ri;
                ControlClosed?.Invoke();
                return;
            }
        }

        private void TextBoxAssetId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                // fake click
                this.ButtonOk_Click(null, null);
            }
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // quit
                DiaData.Result = false;
                DiaData.ResultItem = null;
                ControlClosed?.Invoke();
            }
        }
    }
}
