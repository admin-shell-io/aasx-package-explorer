using System;
using System.Collections.Generic;
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

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für AasxFileRepoControl.xaml
    /// </summary>
    public partial class AasxFileRepoControl : UserControl
    {
        //
        // External properties
        //

        public event Action QueryClick;
        public event Action<AasxFileRepository.FileItem> FileDoubleClick;

        //public delegate void FileDoubleClickHandler(AasxFileRepository.FileItem fi);
        //public event FileDoubleClickHandler FileDoubleClick;

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
                this.FileDoubleClick?.Invoke(this.RepoList.SelectedItem as AasxFileRepository.FileItem);
        }

        private void ButtonQuery_Click(object sender, RoutedEventArgs e)
        {
            if (sender == this.ButtonQuery)
                this.QueryClick?.Invoke();
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
        }
    }
}
