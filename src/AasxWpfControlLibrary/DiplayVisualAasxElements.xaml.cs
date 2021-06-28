/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

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
using AasxPackageLogic;
using AasxWpfControlLibrary;
using AasxPackageLogic.PackageCentral;
using AasxWpfControlLibrary.PackageCentral;
using AdminShellNS;
using JetBrains.Annotations;
using System.Reflection;
using AnyUi;

namespace AasxPackageExplorer
{
    public interface ITreeViewSelectable
    {
        bool IsSelected { get; set; }
    }

    public partial class DiplayVisualAasxElements : UserControl, IManageVisualAasxElements
    {
        private ListOfVisualElement displayedTreeViewLines = new ListOfVisualElement();
        private TreeViewLineCache treeViewLineCache = null;
        private bool _lastEditMode = false;

        #region Public events and properties
        //
        // Public events and properties
        //

        public int MultiSelect = 2;

        public event EventHandler SelectedItemChanged = null;

        // Future use?
        [JetBrains.Annotations.UsedImplicitly]
        public event EventHandler DoubleClick = null;

        public VisualElementGeneric SelectedItem
        {
            get
            {
                if (this.MultiSelect != 2 || _selectedItems == null)
                    return treeViewInner.SelectedItem as VisualElementGeneric;

                // ok, only return definitve results
                if (_selectedItems.Count == 1)
                    return _selectedItems[0];
                    
                return null;
            }
        }

        public VisualElementGeneric TrySynchronizeToInternalTreeState()
        {
            var x = this.SelectedItem;
            if (x == null && treeViewInner.SelectedItem != null)
            {
                x = treeViewInner.SelectedItem as VisualElementGeneric;

                SuppressSelectionChangeNotification(() => {
                    SetSelectedState(x, true);
                });
            }
            return x;
        }

        public VisualElementGeneric GetSelectedItem()
        {
            return this.SelectedItem;
        }

        public ListOfVisualElementBasic SelectedItems
        {
            get
            {
                return _selectedItems;
            }
        }

        public ListOfVisualElementBasic GetSelectedItems()
        {
            return this.SelectedItems;
        }

        // Enumerate all the descendants of the visual object.
        public void EnumVisual(Visual myVisual, object dataObject)
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
                    if ((object)tvi is VisualElementGeneric ve)
                        if (!(_selectedItems.Contains(ve)))
                            _selectedItems.Add(ve);
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
            if (_selectedItems.Contains(displayedTreeViewLines[0]))
                _selectedItems.Remove(displayedTreeViewLines[0]);
            EnumVisual(treeViewInner, dataObject);
            treeViewInner.UpdateLayout();
            FireSelectedItem();
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

        //
        // see: https://stackoverflow.com/questions/3225940/prevent-automatic-horizontal-scroll-in-treeview
        //

        private bool mSuppressRequestBringIntoView;

        private void TreeViewElem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Alternative (1): not working completely properly
#if __not_working_completely_properly
            Alternative 1
            base.BringIntoView();
            var scrollViewer = treeViewInner.Template.FindName("_tv_scrollviewer_", treeViewInner) as ScrollViewer;
            if (scrollViewer != null)
                Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Loaded,
                    (Action)(() => scrollViewer.ScrollToLeftEnd()));
            */
#endif

            // Alternative (2): seems working fine

            // Ignore re-entrant calls
            if (mSuppressRequestBringIntoView)
                return;

            // Cancel the current scroll attempt
            e.Handled = true;

            // Call BringIntoView using a rectangle that extends into "negative space" to the left of our
            // actual control. This allows the vertical scrolling behaviour to operate without adversely
            // affecting the current horizontal scroll position.
            mSuppressRequestBringIntoView = true;

            TreeViewItem tvi = sender as TreeViewItem;
            if (tvi != null)
            {
                Rect newTargetRect = new Rect(-1000, 0, tvi.ActualWidth + 1000, tvi.ActualHeight);
                tvi.BringIntoView(newTargetRect);
            }

            mSuppressRequestBringIntoView = false;
        }

        // Correctly handle programmatically selected items
        private void TreeViewElem_OnSelected(object sender, RoutedEventArgs e)
        {
            ((TreeViewItem)sender).BringIntoView();
            e.Handled = true;
        }

        //
        // fix: SelectedItemChanged
        // As the SelectedItemChanged event is also fired due to internal operations,
        // it is suppressed from time to time.
        //

        private bool preventSelectedItemChanged = false;

        public void DisableSelectedItemChanged()
        {
            preventSelectedItemChanged = true;
        }

        public void EnableSelectedItemChanged()
        {
            preventSelectedItemChanged = false;
        }

        private void TreeViewInner_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (this.MultiSelect == 1)
                return;

            if (sender != treeViewInner || preventSelectedItemChanged)
                return;

            if (SelectedItemChanged != null)
                SelectedItemChanged(this, e);
        }

        //
        // further functions
        //

        public void Refresh()
        {
            preventSelectedItemChanged = true;
            _selectedItems = new ListOfVisualElementBasic();
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
        // Event queuing
        //

        private List<AnyUiLambdaActionBase> _eventQueue = new List<AnyUiLambdaActionBase>();

        public void PushEvent(AnyUiLambdaActionBase la)
        {
            lock (_eventQueue)
            {
                _eventQueue.Add(la);
            }
        }

        public void UpdateFromQueuedEvents()
        {
            if (displayedTreeViewLines == null)
                return;

            lock (_eventQueue)
            {
                foreach (var lab in _eventQueue)
                {
                    if (lab is AnyUiLambdaActionPackCntChange lapcc && lapcc.Change != null)
                    {
                        // shortcut
                        var e = lapcc.Change;

                        // for speed reasons?
                        if (e.DisableSelectedTreeItemChange)
                            DisableSelectedItemChanged();

                        displayedTreeViewLines.UpdateByEvent(e, treeViewLineCache);

                        if (e.DisableSelectedTreeItemChange)
                            EnableSelectedItemChanged();
                    }

                    if (lab is AnyUiLambdaActionSelectMainObjects labsmo)
                    {
                        this.TrySelectMainDataObjects(labsmo.MainObjects);
                    }
                }
                
                _eventQueue.Clear();
            }
        }        

        //
        // Element management
        //

        public IEnumerable<VisualElementGeneric> FindAllVisualElement()
        {
            if (displayedTreeViewLines != null)
                foreach (var ve in displayedTreeViewLines.FindAllVisualElement())
                    yield return ve;
        }

        public IEnumerable<VisualElementGeneric> FindAllVisualElement(Predicate<VisualElementGeneric> p)
        {
            if (displayedTreeViewLines != null)
                foreach (var ve in displayedTreeViewLines.FindAllVisualElement(p))
                    yield return ve;
        }

        public bool Contains(VisualElementGeneric ve)
        {
            if (displayedTreeViewLines != null)
                return displayedTreeViewLines.ContainsDeep(ve);
            return false;
        }

        public VisualElementGeneric SearchVisualElementOnMainDataObject(object dataObject,
            bool alsoDereferenceObjects = false,
            ListOfVisualElement.SupplementaryReferenceInformation sri = null)
        {
            if (displayedTreeViewLines != null)
                return displayedTreeViewLines.FindFirstVisualElementOnMainDataObject(
                    dataObject, alsoDereferenceObjects, sri);
            return null;
        }

        public VisualElementGeneric GetDefaultVisualElement()
        {
            if (displayedTreeViewLines == null || displayedTreeViewLines.Count < 1)
                return null;

            return displayedTreeViewLines[0];
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

        public void SelectSingleVisualElement(VisualElementGeneric ve, bool preventFireItem = false)
        {
            if (ve == null)
                return;
            ve.IsSelected = true;
            _selectedItems.Clear();
            _selectedItems.Add(ve);
            if (!preventFireItem)
                FireSelectedItem();
        }

        public bool TrySelectVisualElement(VisualElementGeneric ve, bool wishExpanded)
        {
            // access?
            if (ve == null)
                return false;

            // select (but no callback!)
            SelectSingleVisualElement(ve, preventFireItem: true);

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
        public bool FilterLeavesOfVisualElements(VisualElementGeneric mem, string fullFilterElementName)
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
                    if (FilterLeavesOfVisualElements(x, fullFilterElementName))
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

        public int RefreshAllChildsFromMainData(VisualElementGeneric root)
        {
            /* TODO (MIHO, 2021-01-04): check to replace all occurences of RefreshFromMainData() by
             * making the tree-items ObservableCollection and INotifyPropertyChanged */

            // access
            if (root == null)
                return 0;

            // self
            var sum = 1;
            root.RefreshFromMainData();

            // children?
            if (root.Members != null)
                foreach (var child in root.Members)
                    sum += RefreshAllChildsFromMainData(child);

            // ok
            return sum;
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
            PackageCentral packages,
            PackageCentral.Selector selector,
            bool editMode = false, string filterElementName = null,
            bool lazyLoadingFirst = false)
        {
            // clear tree
            displayedTreeViewLines.Clear();
            _lastEditMode = editMode;

            // valid?
            if (packages.MainAvailable)
            {

                // generate lines, add
                displayedTreeViewLines.AddVisualElementsFromShellEnv(
                    treeViewLineCache, packages.Main?.AasEnv, packages.Main,
                    packages.MainItem?.Filename, editMode, expandMode: 1, lazyLoadingFirst: lazyLoadingFirst);

                // more?
                if (packages.AuxAvailable &&
                    (selector == PackageCentral.Selector.MainAux
                        || selector == PackageCentral.Selector.MainAuxFileRepo))
                {
                    displayedTreeViewLines.AddVisualElementsFromShellEnv(
                        treeViewLineCache, packages.Aux?.AasEnv, packages.Aux,
                        packages.AuxItem?.Filename, editMode, expandMode: 1, lazyLoadingFirst: lazyLoadingFirst);
                }

                // more?
                if (packages.Repositories != null && selector == PackageCentral.Selector.MainAuxFileRepo)
                {
                    var pkg = new AdminShellPackageEnv();
                    foreach (var fr in packages.Repositories)
                        fr.PopulateFakePackage(pkg);

                    displayedTreeViewLines.AddVisualElementsFromShellEnv(
                        treeViewLineCache, pkg?.AasEnv, pkg, 
                        null, editMode, expandMode: 1, lazyLoadingFirst: lazyLoadingFirst);
                }

                // may be filter
                if (filterElementName != null)
                    foreach (var dtl in displayedTreeViewLines)
                        // it is not likely, that we have to delete on this level, therefore don't care
                        FilterLeavesOfVisualElements(dtl, filterElementName);

                // any of these lines?
                if (displayedTreeViewLines.Count < 1)
                {
                    // emergency
                    displayedTreeViewLines.Add(
                        new VisualElementEnvironmentItem(
                            null /* no parent */, treeViewLineCache, packages.Main, packages.Main?.AasEnv,
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
            if (this.MultiSelect != 1)
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
            if (this.MultiSelect != 1)
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
            if (this.MultiSelect != 1)
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

        public void Test2()
        {
            foreach (var it in displayedTreeViewLines)
            {
                // it.MyProperty = !it.MyProperty;
                it.TriggerAnimateUpdate();
            }
        }

        private void TreeViewInner_Expanded(object sender, RoutedEventArgs e)
        {
            // access and check
            var tvi = e?.OriginalSource as TreeViewItem;
            var ve = tvi?.Header as VisualElementGeneric;
            if (ve == null || !ve.NeedsLazyLoading)
                return;

            // try execute, may take some time
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                displayedTreeViewLines?.ExecuteLazyLoading(ve, forceExpanded: true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Tries to expand all items, which aren't currently yet, e.g. because of lazy loading.
        /// Is found to be a valid pre-requisite in case of lazy loading for 
        /// <c>SearchVisualElementOnMainDataObject</c>.
        /// Potentially a expensive operation.
        /// </summary>
        public void ExpandAllItems()
        {
            if (displayedTreeViewLines == null)
                return;

            // try execute, may take some time
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                // search (materialized)
                var candidates = FindAllVisualElement((ve) => ve.NeedsLazyLoading).ToList();

                // susequently approach
                foreach (var ve in candidates)
                    displayedTreeViewLines.ExecuteLazyLoading(ve);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "when expanding all visual AASX elements");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        //
        // Test further method:
        // https://stackoverflow.com/questions/1163801/wpf-treeview-with-multiple-selection/6681993#6681993
        //

        private static readonly PropertyInfo IsSelectionChangeActiveProperty
            = typeof(TreeView).GetProperty
            (
                "IsSelectionChangeActive",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private ListOfVisualElementBasic _selectedItems = new ListOfVisualElementBasic();

        private void FireSelectedItem()
        {
            if (!preventSelectedItemChanged && SelectedItemChanged != null)
                SelectedItemChanged(this, null);
        }

        private void SuppressSelectionChangeNotification(Action lambda)
        {
            // suppress selection change notification
            // select all selected items
            // then restore selection change notifications
            var isSelectionChangeActive =
              IsSelectionChangeActiveProperty.GetValue(treeViewInner, null);

            IsSelectionChangeActiveProperty.SetValue(treeViewInner, true, null);

            lambda.Invoke();

            IsSelectionChangeActiveProperty.SetValue
            (
              treeViewInner,
              isSelectionChangeActive,
              null
            );
        }

        private void SetSelectedState (VisualElementGeneric ve, bool newState)
        {
            // ok?
            if (ve == null)
                return;

            // new state?
            if (newState)
            {
                ve.IsSelected = true;
                if (!_selectedItems.Contains(ve))
                    _selectedItems.Add(ve);
            }
            else
            {
                ve.IsSelected = false;
                if (_selectedItems.Contains(ve))
                    _selectedItems.Remove(ve);

            }
        }

        private void TreeViewMutiSelect_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var treeViewItem = treeViewInner.SelectedItem as VisualElementGeneric;
            if (treeViewItem == null) return;

            // prevention completely diables behaviour
            if (preventSelectedItemChanged)
                return;

            // allow multiple selection
            var toogleActiveItem = true;
            // when control key is pressed
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                SuppressSelectionChangeNotification(() => {
                    _selectedItems.ForEach(item => item.IsSelected = true);
                });
            }
            else
            // when shift key is pressed
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                SuppressSelectionChangeNotification(() => {

                    // make sure active treeViewItem item is in
                    SetSelectedState(treeViewItem, true);

                    // try check if this gives a homogenous pictur
                    var nx = _selectedItems.GetIndexedParentInfo();
                    if (nx != null && nx.SharedParent?.Members != null)
                    {
                        for (int i = nx.MinIndex; i <= nx.MaxIndex; i++)
                            SetSelectedState(nx.SharedParent.Members[i], true);
                    }

                    toogleActiveItem = false;
                });
            }
            else
            {
                // deselect all selected items except the current one
                _selectedItems.ForEach(item => item.IsSelected = (item == treeViewItem));
                _selectedItems.Clear();
            }

            // still toggle active?
            if (toogleActiveItem)
            {
                if (!_selectedItems.Contains(treeViewItem))
                {
                    _selectedItems.Add(treeViewItem);
                }
                else
                {
                    // deselect if already selected
                    treeViewItem.IsSelected = false;
                    _selectedItems.Remove(treeViewItem);
                }
            }

            // fire event
            FireSelectedItem();
        }

        public bool TrySelectVisualElements(ListOfVisualElementBasic ves, bool preventFireItem = false)
        {
            // access?
            if (ves == null)
                return false;

            // suppressed
            SuppressSelectionChangeNotification(() =>
            {

                // deselect all
                foreach (var si in _selectedItems)
                    si.IsSelected = false;
                _selectedItems.Clear();

                // select
                foreach (var ve in ves)
                {
                    if (ve == null)
                        continue;
                    ve.IsSelected = true;
                    _selectedItems.Add(ve);
                }
            });

            // fire
            if (!preventFireItem)
                FireSelectedItem();

            treeViewInner.UpdateLayout();

            // OK
            return true;
        }

        public void TrySelectMainDataObjects(IEnumerable<object> mainObjects, bool preventFireItem = false)
        {
            // gather objects
            var ves = new ListOfVisualElementBasic();
            if (mainObjects != null)
                foreach (var mo in mainObjects)
                {
                    var ve = SearchVisualElementOnMainDataObject(mo);
                    if (ve != null)
                        ves.Add(ve);
                }

            // select
            TrySelectVisualElements(ves, preventFireItem);

            // fire event
            FireSelectedItem();
        }

    }
}



