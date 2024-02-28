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
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;

namespace AasxWpfControlLibrary.PackageCentral
{
    public partial class PackageContainerListControl : UserControl
    {
        //
        // External properties
        //

        public enum CustomButton { Query, Context }

        public event Func<Control, PackageContainerListBase, CustomButton, Button, Task>
            ButtonClick;
        public event Action<Control, PackageContainerListBase, PackageContainerRepoItem>
            FileDoubleClick;
        public event Action<Control, PackageContainerListBase, string[]>
            FileDrop;

        private PackageContainerListBase theFileRepository = null;
        public PackageContainerListBase FileRepository
        {
            get { return theFileRepository; }
            set
            {
                this.theFileRepository = value;
                this.RepoList.ItemsSource = this.theFileRepository?.FileMap;
                this.RepoList.UpdateLayout();
            }
        }

        public PackageContainerListControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // might attach to data context
            if (DataContext is PackageContainerListBase fr)
            {
                this.theFileRepository = fr;
                this.RepoList.ItemsSource = this.theFileRepository?.FileMap;
                this.RepoList.UpdateLayout();
            }

            // redraw
            RedrawStatus();

            // Timer for animations
            System.Windows.Threading.DispatcherTimer MainTimer = new System.Windows.Threading.DispatcherTimer();
            MainTimer.Tick += MainTimer_Tick;
            MainTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            MainTimer.Start();
        }

        public void RedrawStatus()
        {
            // set icon
            TextBoxRepoIcon.Foreground = Brushes.Black;
            var icon = "\U0001F4BE";
            if (FileRepository is PackageContainerListHttpRestRegistry)
                icon = "\U0001f4d6";
            if (FileRepository is PackageContainerListHttpRestRepository)
                icon = "\u2601";
            if (FileRepository is PackageContainerListLastRecentlyUsed)
            {
                icon = "\u2749";
                TextBoxRepoHeader.IsReadOnly = true;
                TextBoxRepoHeader.IsReadOnlyCaretVisible = false;
                TextBoxRepoHeader.IsHitTestVisible = false; // work around for above
            }

            var oidc = (theFileRepository as PackageContainerListHttpRestBase)?.OpenIdClient;
            if (icon == "\u2601" && oidc != null && oidc.token != "")
            {
                icon = "\u2600";
                TextBoxRepoIcon.Foreground = Brushes.Green;
            }
            TextBoxRepoIcon.Text = icon;

            // set header
            var header = FileRepository?.Header;
            if (!header.HasContent())
                header = "Unnamed repository";
            TextBoxRepoHeader.Text = "" + header;
        }

        private void MainTimer_Tick(object sender, EventArgs e)
        {
            if (this.theFileRepository != null)
                this.theFileRepository.DecreaseVisualTimeBy(0.1);
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            this.RepoList.UnselectAll();
        }

        private void RepoList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender == this.RepoList && e.LeftButton == MouseButtonState.Pressed)
                // hoping, that correct item is selected
                this.FileDoubleClick?.Invoke(this, theFileRepository,
                    this.RepoList.SelectedItem as PackageContainerRepoItem);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == this.ButtonQuery)
                await this.ButtonClick?.Invoke(this, theFileRepository, CustomButton.Query, this.ButtonQuery);
            if (sender == this.ButtonContext)
                await this.ButtonClick?.Invoke(this, theFileRepository, CustomButton.Context, this.ButtonContext);
        }

        private void RepoList_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private PackageContainerRepoItem rightClickSelectedItem = null;

        private void RepoList_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender == this.RepoList && e.ChangedButton == MouseButton.Right)
            {
                // store selected item for later (when context menu selection is done)
                var fi = this.RepoList.SelectedItem as PackageContainerRepoItem;
                this.rightClickSelectedItem = fi;

                // find context menu
                ContextMenu cm = this.FindResource("ContextMenuFileItem") as ContextMenu;
                if (cm == null)
                    return;

                // set some fields in context menu
                var x = AasxWpfBaseUtils.FindChildLogicalTree<TextBox>(cm, "TextBoxTag");
                if (x != null && fi != null)
                    x.Text = "" + fi.Tag;

                x = AasxWpfBaseUtils.FindChildLogicalTree<TextBox>(cm, "TextBoxDescription");
                if (x != null && fi != null)
                    x.Text = "" + fi.Description;

                x = AasxWpfBaseUtils.FindChildLogicalTree<TextBox>(cm, "TextBoxCode");
                if (x != null && fi != null)
                    x.Text = "" + fi.CodeType2D;

                var cb = AasxWpfBaseUtils.FindChildLogicalTree<CheckBox>(cm, "CheckBoxLoadResident");
                if (cb != null && fi?.ContainerOptions != null)
                    cb.IsChecked = fi.ContainerOptions.LoadResident;

                cb = AasxWpfBaseUtils.FindChildLogicalTree<CheckBox>(cm, "CheckBoxStayConnected");
                if (cb != null && fi?.ContainerOptions != null)
                    cb.IsChecked = fi.ContainerOptions.StayConnected;

                x = AasxWpfBaseUtils.FindChildLogicalTree<TextBox>(cm, "TextBoxUpdatePeriod");
                if (x != null && fi?.ContainerOptions != null)
                    x.Text = "" + fi.ContainerOptions.UpdatePeriod;

                // show context menu
                cm.PlacementTarget = sender as Button;
                cm.IsOpen = true;
            }
        }

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var fi = this.rightClickSelectedItem;

            if (mi?.Name == "MenuItemDelete" && fi != null)
            {
                this.FileRepository?.Remove(fi);
            }

            if (mi?.Name == "MenuItemDeleteFromFileRepo" && fi != null)
            {
                this.FileRepository?.DeletePackageFromServer(fi);
            }

            if (mi?.Name == "MenuItemMoveUp" && fi != null)
            {
                this.FileRepository?.MoveUp(fi);
            }

            if (mi?.Name == "MenuItemMoveDown" && fi != null)
            {
                this.FileRepository?.MoveDown(fi);
            }

			if (mi?.Name == "MenuItemLoad" && fi != null)
			{
				await fi.LoadResidentIfPossible(this.FileRepository?.GetFullItemLocation(fi.Location));
                Log.Singleton.Info($"Repository item {fi.Location} loaded.");
			}

			if (mi?.Name == "MenuItemUnload" && fi != null)
            {
                fi.Close();
				Log.Singleton.Info($"Repository item {fi.Location} unloaded.");
			}

			if (mi?.Name == "MenuItemRecalc" && fi != null)
            {
                await fi.LoadResidentIfPossible(theFileRepository?.GetFullItemLocation(fi.Location));

                if (fi.Env?.AasEnv == null)
                {
                    Log.Singleton.Error("AAS information not already loaded for this item.");
                    return;
                }

                fi.CalculateIdsTagAndDesc();
            }
        }

        private void TextBoxContextMenu_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            var fi = this.rightClickSelectedItem;

            if (tb?.Name == "TextBoxTag" && fi != null)
                fi.Tag = tb.Text;

            if (tb?.Name == "TextBoxDescription" && fi != null)
                fi.Description = tb.Text;

            if (tb?.Name == "TextBoxCode" && fi != null)
                fi.CodeType2D = tb.Text;

            if (tb?.Name == "TextBoxUpdatePeriod" && fi != null)
            {
                if (fi.ContainerOptions == null)
                    fi.ContainerOptions = PackageContainerOptionsBase.CreateDefault(Options.Curr);
                if (Int32.TryParse("" + tb.Text, out int i))
                    fi.ContainerOptions.UpdatePeriod = Math.Max(OptionsInformation.MinimumUpdatePeriod, i);
            }
        }

        private void CheckBoxContextMenu_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var fi = this.rightClickSelectedItem;

            if (cb?.Name == "CheckBoxLoadResident" && fi != null)
            {
                if (fi.ContainerOptions == null)
                    fi.ContainerOptions = new PackageContainerOptionsBase();
                fi.ContainerOptions.LoadResident = true == cb?.IsChecked;
            }

            if (cb?.Name == "CheckBoxStayConnected" && fi != null)
            {
                if (fi.ContainerOptions == null)
                    fi.ContainerOptions = new PackageContainerOptionsBase();
                fi.ContainerOptions.StayConnected = true == cb?.IsChecked;
            }
        }

        private void RepoControl_Drop(object sender, DragEventArgs e)
        {
            // Appearantly you need to figure out if OriginalSource would have handled the Drop?
            if (!e.Handled && e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // simply pass over to upper layer to decide, how to finally handle
                e.Handled = true;
                FileDrop?.Invoke(this, FileRepository, files);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == TextBoxRepoHeader && FileRepository != null)
                FileRepository.Header = TextBoxRepoHeader.Text;
        }
    }
}
