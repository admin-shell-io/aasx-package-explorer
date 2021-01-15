/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AasxWpfControlLibrary.PackageCentral;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    public partial class SelectFromRepositoryFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        public PackageContainerRepoItem ResultItem = null;

        private List<PackageContainerRepoItem> _listFileItems;

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

        public bool LoadAasxRepoFile(IEnumerable<PackageContainerRepoItem> items = null)
        {
            try
            {
                this._listFileItems = new List<PackageContainerRepoItem>();

                if (items != null)
                {
                    // from RAM
                    this._listFileItems.AddRange(items);
                }

                if (_listFileItems == null || _listFileItems.Count < 1)
                    return false;

                // rework buttons
                this.StackPanelTags.Children.Clear();
                foreach (var fm in this._listFileItems)
                {
                    var tag = fm.Tag.Trim();
                    if (tag != "")
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
                        b.Tag = fm;
                    }
                }

            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                this._listFileItems = null;
                return false;
            }

            return true;
        }

        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            if (b?.Tag != null && this._listFileItems != null && this._listFileItems.Contains(b.Tag))
            {
                this.ResultItem = b.Tag as PackageContainerRepoItem;
                ControlClosed?.Invoke();
            }
        }

        //
        // Mechanics
        //

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            ResultItem = null;
            ControlClosed?.Invoke();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.TextBoxAssetId.Text = "";
            this.TextBoxAssetId.Focus();
            this.TextBoxAssetId.Select(0, 999);
            FocusManager.SetFocusedElement(this, this.TextBoxAssetId);
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            // get text
            var aid = TextBoxAssetId.Text.Trim().ToLower();

            // first compare against tags
            if (this._listFileItems != null && this._listFileItems != null)
                foreach (var fm in this._listFileItems)
                    if (aid == fm.Tag.Trim().ToLower())
                    {
                        this.ResultItem = fm;
                        ControlClosed?.Invoke();
                        return;
                    }

            // if not, compare asset ids
            if (this._listFileItems != null && this._listFileItems != null)
                foreach (var fm in this._listFileItems)
                    foreach (var id in fm.EnumerateAssetIds())
                        if (aid == id.Trim().ToLower())
                        {
                            this.ResultItem = fm;
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
                ResultItem = null;
                ControlClosed?.Invoke();
            }
        }
    }
}
