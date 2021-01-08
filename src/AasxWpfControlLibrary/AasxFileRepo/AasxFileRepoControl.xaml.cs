/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AasxWpfControlLibrary.PackageCentral;

namespace AasxWpfControlLibrary.AasxFileRepo
{
    public partial class AasxFileRepoControl : UserControl
    {
        //
        // External properties
        //

        public enum CustomButton { Query, Context }

        public event Action<AasxFileRepository, CustomButton, Button> ButtonClick;
        public event Action<AasxFileRepository, AasxFileRepository.FileItem> FileDoubleClick;
        public event Action<AasxFileRepository, string[]> FileDrop;

        private AasxFileRepository theFileRepository = null;
        public AasxFileRepository FileRepository
        {
            get { return theFileRepository; }
            set
            {
                this.theFileRepository = value;
                this.RepoList.ItemsSource = this.theFileRepository?.FileMap;
                this.RepoList.UpdateLayout();
            }
        }

        public AasxFileRepoControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // might attach to data context
            if (DataContext is AasxFileRepository fr)
            {
                this.theFileRepository = fr;
                this.RepoList.ItemsSource = this.theFileRepository?.FileMap;
                this.RepoList.UpdateLayout();
            }

            // set icon
            var icon = "\U0001F4BE";
            if (FileRepository is AasxFileRepository)
                icon = "\U0001f4d6";
            if (FileRepository is AasxFileRepository)
                icon = "\u2601";
            TextBoxRepoIcon.Text = icon;

            // set header
            var header = FileRepository?.Header;
            if (!header.HasContent())
                header = "Unnamed repository";
            TextBoxRepoHeader.Text = "" + header;

            // Timer for animations
            System.Windows.Threading.DispatcherTimer MainTimer = new System.Windows.Threading.DispatcherTimer();
            MainTimer.Tick += MainTimer_Tick;
            MainTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            MainTimer.Start();
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
                this.FileDoubleClick?.Invoke(theFileRepository, this.RepoList.SelectedItem as AasxFileRepository.FileItem);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == this.ButtonQuery)
                this.ButtonClick?.Invoke(theFileRepository, CustomButton.Query, this.ButtonQuery);
            if (sender == this.ButtonContext)
                this.ButtonClick?.Invoke(theFileRepository, CustomButton.Context, this.ButtonContext);
        }

        private void RepoList_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private AasxFileRepository.FileItem rightClickSelectedItem = null;

        private void RepoList_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender == this.RepoList && e.ChangedButton == MouseButton.Right)
            {
                // store selected item for later (when context menu selection is done)
                var fi = this.RepoList.SelectedItem as AasxFileRepository.FileItem;
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
                if (cb != null && fi?.Options != null)
                    cb.IsChecked = fi.Options.LoadResident;

                cb = AasxWpfBaseUtils.FindChildLogicalTree<CheckBox>(cm, "CheckBoxStayConnected");
                if (cb != null && fi?.Options != null)
                    cb.IsChecked = fi.Options.StayConnected;

                x = AasxWpfBaseUtils.FindChildLogicalTree<TextBox>(cm, "TextBoxUpdatePeriod");
                if (x != null && fi?.Options != null)
                    x.Text = "" + fi.Options.UpdatePeriod;

                // show context menu
                cm.PlacementTarget = sender as Button;
                cm.IsOpen = true;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var fi = this.rightClickSelectedItem;

            if (mi?.Name == "MenuItemDelete" && fi != null)
            {
                this.FileRepository?.Remove(fi);
            }

            if (mi?.Name == "MenuItemMoveUp" && fi != null)
            {
                this.FileRepository?.MoveUp(fi);
            }

            if (mi?.Name == "MenuItemMoveDown" && fi != null)
            {
                this.FileRepository?.MoveDown(fi);
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
                if (fi.Options == null)
                    fi.Options = PackageContainerOptionsBase.CreateDefault(Options.Curr);
                if (Int32.TryParse("" + tb.Text, out int i))
                    fi.Options.UpdatePeriod = Math.Max(OptionsInformation.MinimumUpdatePeriod, i);
            }
        }

        private void CheckBoxContextMenu_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var fi = this.rightClickSelectedItem;

            if (cb?.Name == "CheckBoxLoadResident" && fi != null)
            {
                if (fi.Options == null)
                    fi.Options = PackageContainerOptionsBase.CreateDefault(Options.Curr);
                fi.Options.LoadResident = true == cb?.IsChecked;
            }

            if (cb?.Name == "CheckBoxStayConnected" && fi != null)
            {
                if (fi.Options == null)
                    fi.Options = PackageContainerOptionsBase.CreateDefault(Options.Curr);
                fi.Options.StayConnected = true == cb?.IsChecked;
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
                FileDrop?.Invoke(FileRepository, files);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == TextBoxRepoHeader && FileRepository != null)
                FileRepository.Header = TextBoxRepoHeader.Text;
        }
    }
}
