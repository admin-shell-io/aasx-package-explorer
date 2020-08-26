using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using AdminShellNS;
using JetBrains.Annotations;

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).
*/

namespace AasxPackageExplorer
{
    public interface ITreeViewSelectable
    {
        bool IsSelected { get; set; }
    }

    /// <summary>
    /// Interaktionslogik für DiplayAasxElements.xaml
    /// </summary>
    public partial class DiplayVisualAasxElements : UserControl
    {
        private List<VisualElementGeneric> displayedTreeViewLines = new List<VisualElementGeneric>();
        private TreeViewLineCache treeViewLineCache = null;

        #region Public events and properties
        //
        // Public events and properties
        //

        public bool MultiSelect = false;

        public event EventHandler SelectedItemChanged = null;

        // Future use?
        [JetBrains.Annotations.UsedImplicitly]
        public event EventHandler DoubleClick = null;

        public VisualElementGeneric SelectedItem
        {
            get
            {
                return treeViewInner.SelectedItem as VisualElementGeneric;
            }
        }

        // Enumerate all the descendants of the visual object.
        public static void EnumVisual(Visual myVisual, object dataObject)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(myVisual); i++)
            {
                // Retrieve child visual at specified index value.
                Visual childVisual = (Visual)VisualTreeHelper.GetChild(myVisual, i);

                // Do processing of the child visual object.
                var tvi = childVisual as TreeViewItem;
                if (tvi != null && tvi.DataContext != null && tvi.DataContext == dataObject)
                {
                    tvi.BringIntoView();
                    tvi.IsSelected = true;
                    tvi.Focus();
                    return;
                }

                // Enumerate children of the child visual object.
                EnumVisual(childVisual, dataObject);
            }
        }

        public void Woodoo(object dataObject)
        {
            if (dataObject == null)
                return;
            // VisualTreeHelper.GetChild(tv1, )
            displayedTreeViewLines[0].IsSelected = false;
            EnumVisual(treeViewInner, dataObject);
            treeViewInner.UpdateLayout();
        }

        /// <summary>
        /// Activates the caching of the "expanded" states of the tree, even if the tree is multiple
        /// times rebuilt via <code>RebuildAasxElements</code>.
        /// </summary>
        public void ActivateElementStateCache()
        {
            this.treeViewLineCache = new TreeViewLineCache();
        }

        public new Brush Background
        {
            get
            {
                return treeViewInner.Background;
            }
            set
            {
                treeViewInner.Background = value;
            }
        }

        #endregion

        #region XAML
        //
        // XAML / UI
        //

        public DiplayVisualAasxElements()
        {
            InitializeComponent();
        }

        private void TreeViewElem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            base.BringIntoView();

            var scrollViewer = treeViewInner.Template.FindName("_tv_scrollviewer_", treeViewInner) as ScrollViewer;
            if (scrollViewer != null)
                Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Loaded,
                    (Action)(() => scrollViewer.ScrollToLeftEnd()));
        }

        /// <summary>
        /// As the SelectedItemChanged event is also fired due to internal operations,
        /// it is suppressed from time to time.
        /// </summary>
        private bool preventSelectedItemChanged = false;

        public void DisableSelectedItemChanged()
        {
            preventSelectedItemChanged = true;
        }

        public void EnableSelectedItemChanged()
        {
            preventSelectedItemChanged = true;
        }

        private void TreeViewInner_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (this.MultiSelect)
                return;

            if (sender != treeViewInner || preventSelectedItemChanged)
                return;

            if (SelectedItemChanged != null)
                SelectedItemChanged(this, e);
        }

        public void Refresh()
        {
            preventSelectedItemChanged = true;
            treeViewInner.Items.Refresh();
            treeViewInner.UpdateLayout();
            preventSelectedItemChanged = false;
        }

        public void ExpandOnlyBranchWithin(VisualElementGeneric node)
        {
            // make a list of all top-level nodes
            var list = new List<VisualElementGeneric>();
            foreach (var dl in displayedTreeViewLines)
                dl.CollectListOfTopLevelNodes(list);
            // contract ALL nodes
            foreach (var tl in list)
                tl.ForAllDescendents((x) => { x.IsExpanded = false; });
            // expand all nodes having descendent
            foreach (var tl in list)
                if (tl.CheckIfDescendent(node))
                    tl.ForAllDescendents((x) => { x.IsExpanded = true; });
        }

        #endregion

        #region Elememt view drawing / handling

        //
        // Element management
        //

        private IEnumerable<VisualElementGeneric> FindAllVisualElementInternal(VisualElementGeneric root)
        {
            yield return root;
            if (root?.Members != null)
                foreach (var m in root.Members)
                    foreach (var e in FindAllVisualElementInternal(m))
                        yield return e;
        }

        public IEnumerable<VisualElementGeneric> FindAllVisualElement()
        {
            if (displayedTreeViewLines == null)
                yield break;
            foreach (var tvl in displayedTreeViewLines)
                foreach (var e in FindAllVisualElementInternal(tvl))
                    yield return e;
        }

        public IEnumerable<VisualElementGeneric> FindAllVisualElement(Predicate<VisualElementGeneric> p)
        {
            if (p == null)
                yield break;

            foreach (var e in this.FindAllVisualElement())
                if (p(e))
                    yield return e;
        }

        public bool Contains(VisualElementGeneric ve)
        {
            foreach (var e in FindAllVisualElement((o) => { return ve == o; }))
                return true;
            return false;
        }

        private VisualElementGeneric SearchInListOfVisualElements(VisualElementGeneric tvl, object dataObject)
        {
            if (tvl == null || dataObject == null)
                return null;

            // Test for VirtualEntities. Allow a string comparison
            var mdo = tvl.GetMainDataObject();
            if (mdo == null)
                return null;
            var s1 = mdo as string;
            var s2 = dataObject as string;
            if (s1 != null && s1 == s2)
                return tvl;

            // normal comparison
            if (tvl.GetMainDataObject() == dataObject)
                return tvl;

            // recursion
            foreach (var mem in tvl.Members)
            {
                var x = SearchInListOfVisualElements(mem, dataObject);
                if (x != null)
                    return x;
            }
            return null;
        }

        public VisualElementGeneric SearchVisualElementOnMainDataObject(object dataObject)
        {
            if (displayedTreeViewLines == null)
                return null;
            foreach (var tvl in displayedTreeViewLines)
            {
                var x = SearchInListOfVisualElements(tvl, dataObject);
                if (x != null)
                    return x;
            }
            return null;
        }

        public bool TrySelectMainDataObject(object dataObject, bool wishExpanded)
        {
            // access?
            var ve = SearchVisualElementOnMainDataObject(dataObject);
            if (ve == null)
                return false;

            // select
            return TrySelectVisualElement(ve, wishExpanded);
        }

        public bool TrySelectVisualElement(VisualElementGeneric ve, bool wishExpanded)
        {
            // access?
            if (ve == null)
                return false;

            // select
            ve.IsSelected = true;
            if (wishExpanded)
            {
                // go upward the tree in order to expand, as well
                var sii = ve;
                while (sii != null)
                {
                    sii.IsExpanded = true;
                    sii = sii.Parent;
                }
            }
            if (wishExpanded == false)
                ve.IsExpanded = false;
            Woodoo(ve);

            // OK
            return true;
        }

        /// <summary>
        /// Return true, if <code>mem</code> has to be deleted, because not in filter.
        /// </summary>
        /// <param name="mem"></param>
        /// <param name="fullFilterElementName"></param>
        /// <returns></returns>
        public bool FilterLeafsOfVisualElements(VisualElementGeneric mem, string fullFilterElementName)
        {
            if (fullFilterElementName == null)
                return (false);
            fullFilterElementName = fullFilterElementName.Trim().ToLower();
            if (fullFilterElementName == "")
                return (false);

            // has Members -> is not leaf!
            if (mem.Members != null && mem.Members.Count > 0)
            {
                // go into non-leafs mode -> simply go over list
                var todel = new List<VisualElementGeneric>();
                foreach (var x in mem.Members)
                    if (FilterLeafsOfVisualElements(x, fullFilterElementName))
                        todel.Add(x);
                // delete items on list
                foreach (var td in todel)
                    mem.Members.Remove(td);
            }
            else
            {
                // this member is a leaf!!
                var isIn = false;
                var mdo = mem.GetMainDataObject();
                if (mdo != null && mdo is AdminShell.Referable)
                {
                    var mdoen = (mdo as AdminShell.Referable).GetElementName().Trim().ToLower();
                    isIn = fullFilterElementName.IndexOf(mdoen, StringComparison.Ordinal) >= 0;
                }
                if (mdo != null && mdo is AdminShell.Reference)
                {
                    var mdoen = (mdo as AdminShell.Reference).GetElementName().Trim().ToLower();
                    isIn = fullFilterElementName.IndexOf(mdoen, StringComparison.Ordinal) >= 0;
                }
                return !isIn;
            }
            return false;
        }

        //
        // Element View Drawing
        //

        public void Clear()
        {
            treeViewInner.ItemsSource = null;
            treeViewInner.UpdateLayout();
        }

        public void RebuildAasxElements(
            AdminShell.AdministrationShellEnv env = null,
            AdminShellPackageEnv package = null,
            AdminShellPackageEnv[] auxPackages = null, bool editMode = false, string filterElementName = null)
        {
            // clear tree
            displayedTreeViewLines = new List<VisualElementGeneric>();

            // valid?
            if (env != null)
            {

                // generate lines, add
                var x = Generators.GenerateVisualElementsFromShellEnv(
                    treeViewLineCache, env, package, editMode, expandMode: 1);
                foreach (var xx in x)
                    displayedTreeViewLines.Add(xx);

                // more?
                if (auxPackages != null)
                    foreach (var aux in auxPackages)
                    {
                        var x2 = Generators.GenerateVisualElementsFromShellEnv(
                            treeViewLineCache, aux.AasEnv, aux, editMode, expandMode: 1);
                        foreach (var xx in x2)
                            displayedTreeViewLines.Add(xx);
                    }

                // may be filter
                if (filterElementName != null)
                    foreach (var dtl in displayedTreeViewLines)
                        // it is not likely, that we have to delete on this level, therefore don't care
                        FilterLeafsOfVisualElements(dtl, filterElementName);

                // any of these lines?
                if (displayedTreeViewLines.Count < 1)
                {
                    // emergency
                    displayedTreeViewLines.Add(
                        new VisualElementEnvironmentItem(
                            null /* no parent */, treeViewLineCache, package, env,
                            VisualElementEnvironmentItem.ItemType.EmptySet));
                }

            }

            // redraw
            treeViewInner.ItemsSource = displayedTreeViewLines;
            treeViewInner.UpdateLayout();

            // select 1st
            if (displayedTreeViewLines.Count > 0)
                displayedTreeViewLines[0].IsSelected = true;
        }

        #endregion

        // MIHO1
        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender != treeViewInner)
                return;
            if (DoubleClick != null)
                DoubleClick(this, e);
        }

        //
        // Extension to allow multi select
        // see: https://stackoverflow.com/questions/459375/customizing-the-treeview-to-allow-multi-select
        //

        // Used in shift selections
        private TreeViewItem _lastItemSelected;
        // Used when clicking on a selected item to check if we want to deselect it or to drag the current selection
        private TreeViewItem _itemToCheck;


        // may kick off the selection of multiple items (referring to 2nd function)
        private void TreeViewInner_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!this.MultiSelect)
                return;

            // If clicking on the + of the tree
            if (e.OriginalSource is Shape || e.OriginalSource is Grid || e.OriginalSource is Border)
                return;

            TreeViewItem item = this.GetTreeViewItemClicked((FrameworkElement)e.OriginalSource);

            if (item != null && item.Header != null)
            {
                this.SelectedItemChangedHandler(item);
            }
        }

        // Check done to avoid deselecting everything when clicking to drag
        private void TreeViewInner_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!this.MultiSelect)
                return;

            if (_itemToCheck != null)
            {
                TreeViewItem item = this.GetTreeViewItemClicked((FrameworkElement)e.OriginalSource);

                if (item != null && item.Header != null)
                {
                    if (!this.IsCtrlPressed)
                    {
                        GetTreeViewItems(true)
                            .Select(t => t.Header)
                            .Cast<ITreeViewSelectable>()
                            .ToList()
                            .ForEach(f => f.IsSelected = false);
                        ((ITreeViewSelectable)_itemToCheck.Header).IsSelected = true;
                        _lastItemSelected = _itemToCheck;
                    }
                    else
                    {
                        ((ITreeViewSelectable)_itemToCheck.Header).IsSelected = false;
                        _lastItemSelected = null;
                    }
                }
            }
        }

        // does the real multi select
        private void SelectedItemChangedHandler(TreeViewItem item)
        {
            if (!this.MultiSelect)
                return;

            ITreeViewSelectable content = (ITreeViewSelectable)item.Header;

            _itemToCheck = null;

            if (content.IsSelected)
            {
                // Check it at the mouse up event to avoid deselecting everything when clicking to drag
                _itemToCheck = item;
            }
            else
            {
                if (!this.IsCtrlPressed)
                {
                    GetTreeViewItems(true)
                        .Select(t => t.Header)
                        .Cast<ITreeViewSelectable>()
                        .ToList()
                        .ForEach(f => f.IsSelected = false);
                }

                if (this.IsShiftPressed && _lastItemSelected != null)
                {
                    foreach (TreeViewItem tempItem in GetTreeViewItemsBetween(_lastItemSelected, item))
                    {
                        ((ITreeViewSelectable)tempItem.Header).IsSelected = true;
                        _lastItemSelected = tempItem;
                    }
                }
                else
                {
                    content.IsSelected = true;
                    _lastItemSelected = item;
                    this.treeViewInner.Items.Refresh();
                    this.treeViewInner.UpdateLayout();
                }
            }
        }

        // allow left + right keys

        private bool IsCtrlPressed
        {
            get
            {
                return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            }
        }

        private bool IsShiftPressed
        {
            get
            {
                return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            }
        }

        // deliver certain intervals of items

        private TreeViewItem GetTreeViewItemClicked(UIElement sender)
        {
            Point point = sender.TranslatePoint(new Point(0, 0), this.treeViewInner);
            DependencyObject visualItem = this.treeViewInner.InputHitTest(point) as DependencyObject;
            while (visualItem != null && !(visualItem is TreeViewItem))
            {
                visualItem = VisualTreeHelper.GetParent(visualItem);
            }

            return visualItem as TreeViewItem;
        }

        private IEnumerable<TreeViewItem> GetTreeViewItemsBetween(TreeViewItem start, TreeViewItem end)
        {
            List<TreeViewItem> items = this.GetTreeViewItems(false);

            int startIndex = items.IndexOf(start);
            int endIndex = items.IndexOf(end);

            // It's possible that the start element has been removed after it was selected,
            // I don't find a way to happen on the end but I add the code to handle the situation just in case
            if (startIndex == -1 && endIndex == -1)
            {
                return new List<TreeViewItem>();
            }
            else if (startIndex == -1)
            {
                return new List<TreeViewItem>() { end };
            }
            else if (endIndex == -1)
            {
                return new List<TreeViewItem>() { start };
            }
            else
            {
                return startIndex > endIndex
                    ? items.GetRange(endIndex, startIndex - endIndex + 1)
                    : items.GetRange(startIndex, endIndex - startIndex + 1);
            }
        }

        private List<TreeViewItem> GetTreeViewItems(bool includeCollapsedItems)
        {
            List<TreeViewItem> returnItems = new List<TreeViewItem>();

            for (int index = 0; index < this.treeViewInner.Items.Count; index++)
            {
                TreeViewItem item = (TreeViewItem)this.treeViewInner.ItemContainerGenerator.ContainerFromIndex(index);
                returnItems.Add(item);
                if (includeCollapsedItems || item.IsExpanded)
                {
                    returnItems.AddRange(GetTreeViewItemItems(item, includeCollapsedItems));
                }
            }

            return returnItems;
        }

        private static IEnumerable<TreeViewItem> GetTreeViewItemItems(
            TreeViewItem treeViewItem, bool includeCollapsedItems)
        {
            List<TreeViewItem> returnItems = new List<TreeViewItem>();

            for (int index = 0; index < treeViewItem.Items.Count; index++)
            {
                TreeViewItem item = (TreeViewItem)treeViewItem.ItemContainerGenerator.ContainerFromIndex(index);
                if (item != null)
                {
                    returnItems.Add(item);
                    if (includeCollapsedItems || item.IsExpanded)
                    {
                        returnItems.AddRange(GetTreeViewItemItems(item, includeCollapsedItems));
                    }
                }
            }

            return returnItems;
        }

        public void Test()
        {
            foreach (var x in treeViewInner.Items)
                if (x is TreeViewItem tvi)
                    // TODO (MIHO, 2020-07-21): was because of multi-select
                    //// if (tvi.Header is ITreeViewSelectable tvih)
                    tvi.IsSelected = true;

            treeViewInner.Items.Refresh();
            treeViewInner.UpdateLayout();
        }
    }
}
