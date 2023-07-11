/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AasxIntegrationBase;
using AnyUi;
using BlazorUI.Data;
using System.Windows.Controls;
using AasxPackageExplorer;

namespace BlazorUI
{
    /// <summary>
    /// This class "hosts" the collection of visual elements
    /// </summary>
    public class BlazorVisualElements : IDisplayElements
    {
        public ListOfVisualElement TreeItems = new ListOfVisualElement();
        private TreeViewLineCache _treeLineCache = null;
        private bool _lastEditMode = false;

        /// <summary>
        /// If it boils down to one item, which is the selected item.
        /// </summary>
        public VisualElementGeneric SelectedItem
        {
            get
            {
                if (_selectedItems == null || _selectedItems.Count < 1)
                    return null;

                return _selectedItems[0];
            }
            set
            {
                _selectedItems ??= new ListOfVisualElementBasic();
                _selectedItems.Clear();
                if (value != null)
                    _selectedItems.Add(value);
            }
        }
        // private VisualElementGeneric _selectedItem = null;

        /// <summary>
        /// If it boils down to one item, which is the selected item.
        /// </summary>
        public VisualElementGeneric GetSelectedItem() => SelectedItem;

        /// <summary>
        /// In case of multiple selected items, use this list.
        /// </summary>
        public ListOfVisualElementBasic SelectedItems
        {
            get
            {
                return _selectedItems;
            }
        }
        private ListOfVisualElementBasic _selectedItems = new ListOfVisualElementBasic();

        // public IList<VisualElementGeneric> ExpandedItems = new List<VisualElementGeneric>();

        /// <summary>
        /// Clears tree and selection, but not cache.
        /// </summary>
        public void Clear()
        {
            TreeItems.Clear();
            _selectedItems.Clear();
        }

        /// <summary>
        /// Gets the first element or a suitable one.
        /// </summary>
        /// <returns></returns>
        public VisualElementGeneric GetDefaultVisualElement()
        {
            if (TreeItems == null || TreeItems.Count < 1)
                return null;

            return TreeItems[0];
        }

        /// <summary>
        /// Clears only selection.
        /// </summary>
		public void ClearSelection()
        {
            _selectedItems.Clear();
        }

        /// <summary>
        /// Activates the caching of the "expanded" states of the tree, even if the tree is multiple
        /// times rebuilt via <code>RebuildAasxElements</code>.
        /// </summary>
        public void ActivateElementStateCache()
        {
            this._treeLineCache = new TreeViewLineCache();
        }

        /// <summary>
        /// Return true, if <code>mem</code> has to be deleted, because not in filter.
        /// </summary>
        /// <param name="mem">Element in current recursion</param>
        /// <param name="fullFilterElementName">Filter string</param>
        /// <param name="firstLeafFound">If null, we be set to very first leaf</param>
        public bool FilterLeavesOfVisualElements(
            VisualElementGeneric mem, string fullFilterElementName,
            ref VisualElementGeneric firstLeafFound)
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
                    if (FilterLeavesOfVisualElements(x, fullFilterElementName, ref firstLeafFound))
                        todel.Add(x);
                // delete items on list
                foreach (var td in todel)
                    mem.Members.Remove(td);
            }
            else
            {
                // consider lazy loading
                if (mem is VisualElementEnvironmentItem memei
                    && memei.theItemType == VisualElementEnvironmentItem.ItemType.DummyNode)
                    return false;

                // this member is a leaf!!
                var isIn = false;
                var filterName = mem.GetFilterElementInfo()?.Trim().ToLower();
                if (filterName != null)
                    isIn = fullFilterElementName.IndexOf(filterName, StringComparison.Ordinal) >= 0;

                if (isIn && firstLeafFound == null)
                    firstLeafFound = mem;

                return !isIn;
            }
            return false;
        }

        public void RebuildAasxElements(
            PackageCentral packages,
            PackageCentral.Selector selector,
            bool editMode = false, string filterElementName = null,
            bool lazyLoadingFirst = false,
            int expandModePrimary = 1,
            int expandModeAux = 0)
        {
            // clear tree
            TreeItems.Clear();
            SelectedItem = null;
            _lastEditMode = editMode;

            // valid?
            if (packages.MainAvailable)
            {

                // generate lines, add
                TreeItems.AddVisualElementsFromShellEnv(
                    _treeLineCache, packages.Main?.AasEnv, packages.Main,
                    packages.MainItem?.Filename, editMode, expandMode: expandModePrimary, lazyLoadingFirst: lazyLoadingFirst);

                // more?
                if (packages.AuxAvailable &&
                    (selector == PackageCentral.Selector.MainAux
                        || selector == PackageCentral.Selector.MainAuxFileRepo))
                {
                    TreeItems.AddVisualElementsFromShellEnv(
                        _treeLineCache, packages.Aux?.AasEnv, packages.Aux,
                        packages.AuxItem?.Filename, editMode, expandMode: expandModeAux, lazyLoadingFirst: lazyLoadingFirst);
                }

                // more?
                if (packages.Repositories != null && selector == PackageCentral.Selector.MainAuxFileRepo)
                {
                    var pkg = new AdminShellPackageEnv();
                    foreach (var fr in packages.Repositories)
                        fr.PopulateFakePackage(pkg);

                    TreeItems.AddVisualElementsFromShellEnv(
                        _treeLineCache, pkg?.AasEnv, pkg,
                        null, editMode, expandMode: expandModeAux, lazyLoadingFirst: lazyLoadingFirst);
                }

                // may be filter
                if (filterElementName != null)
                {
                    VisualElementGeneric firstLeafFound = null;
                    foreach (var dtl in TreeItems)
                        // it is not likely, that we have to delete on this level, therefore don't care
                        FilterLeavesOfVisualElements(dtl, filterElementName, ref firstLeafFound);

                    // expand first leaf ..
                    if (firstLeafFound != null && expandModePrimary != 0)
                        foreach (var n in firstLeafFound.FindAllParents(includeThis: true))
                            n.IsExpanded = true;
                }

                // any of these lines?
                if (TreeItems.Count < 1)
                {
                    // emergency
                    TreeItems.Add(
                        new VisualElementEnvironmentItem(
                            null /* no parent */, _treeLineCache, packages.Main, packages.Main?.AasEnv,
                            VisualElementEnvironmentItem.ItemType.EmptySet));
                }

            }

            // select 1st
            if (TreeItems.Count > 0)
                SelectedItem = TreeItems[0];
        }

        /// <summary>
        /// Tries to expand all items, which aren't currently yet, e.g. because of lazy loading.
        /// Is found to be a valid pre-requisite in case of lazy loading for 
        /// <c>SearchVisualElementOnMainDataObject</c>.
        /// Potentially a expensive operation.
        /// </summary>
        public void ExpandAllItems()
        {
            if (TreeItems == null)
                return;

            // try execute, may take some time
            try
            {
                // search (materialized)
                var candidates = FindAllVisualElement((ve) => ve.NeedsLazyLoading).ToList();

                // susequently approach
                foreach (var ve in candidates)
                    TreeItems.ExecuteLazyLoading(ve);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "when expanding all visual AASX elements");
            }
        }

        //
        // Element management
        //

        public IEnumerable<VisualElementGeneric> FindAllVisualElement()
        {
            if (TreeItems != null)
                foreach (var ve in TreeItems.FindAllVisualElement())
                    yield return ve;
        }

        public IEnumerable<VisualElementGeneric> FindAllVisualElement(Predicate<VisualElementGeneric> p)
        {
            if (TreeItems != null)
                foreach (var ve in TreeItems.FindAllVisualElement(p))
                    yield return ve;
        }

        public bool Contains(VisualElementGeneric ve)
        {
            if (TreeItems != null)
                return TreeItems.ContainsDeep(ve);
            return false;
        }

        public VisualElementGeneric SearchVisualElementOnMainDataObject(object dataObject,
            bool alsoDereferenceObjects = false,
            ListOfVisualElement.SupplementaryReferenceInformation sri = null)
        {
            if (TreeItems != null)
                return TreeItems.FindFirstVisualElementOnMainDataObject(
                    dataObject, alsoDereferenceObjects, sri);
            return null;
        }

        public void SelectSingleVisualElement(VisualElementGeneric ve, bool preventFireItem = false)
        {
            if (ve == null)
                return;
            ve.IsSelected = true;
            _selectedItems.Clear();
            _selectedItems.Add(ve);
            //if (!preventFireItem)
            //    FireSelectedItem();
        }


        public bool TrySelectVisualElement(VisualElementGeneric ve, bool? wishExpanded)
        {
            // access?
            if (ve == null)
                return false;

            // select (but no callback!)
            SelectSingleVisualElement(ve, preventFireItem: true);

            if (wishExpanded == true)
            {
                // go upward the tree in order to expand, as well
                var sii = ve;
                while (sii != null)
                {
                    //if (!(ExpandedItems.Contains(sii)))
                    //    ExpandedItems.Add(sii);
                    sii.IsExpanded = true;
                    sii = sii.Parent;
                }
            }

            //if (wishExpanded == false && ExpandedItems.Contains(ve))
            //    ExpandedItems.Remove(ve);

            // OK
            return true;
        }

        /// <summary>
        /// Carefully checks and tries to select a tree item which is identified
        /// by the main data object (e.g. an AAS, SME, ..)
        /// </summary>
        public bool TrySelectMainDataObject(object dataObject, bool? wishExpanded)
        {
            // access?
            var ve = SearchVisualElementOnMainDataObject(dataObject);
            if (ve == null)
                return false;

            // select
            return TrySelectVisualElement(ve, wishExpanded);
        }

        public void Refresh()
        {
            ;
        }

        //public void NotifyExpansionState(VisualElementGeneric ve, bool expanded)
        //{

        //}

        // this is bascially a copy from DiplayVisualAasxElements.xaml.cs
        private void SetSelectedState(VisualElementGeneric ve, bool newState)
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

        public void SetExpanded(VisualElementGeneric ve, bool state)
        {
            ve.IsExpanded = state;

            if (state)
            {
                try
                {
                    TreeItems?.ExecuteLazyLoading(ve, state);
                }
                catch (Exception ex)
                {
                    LogInternally.That.CompletelyIgnoredError(ex);
                }
            }
        }

        // this is bascially a copy from DiplayVisualAasxElements.xaml.cs
        public void NotifyTreeSelectionChanged(VisualElementGeneric ve, BlazorInput.KeyboardModifiers modi)
        {
            // trivial
            if (ve == null)
                return;

            // the toggle action could be used multiple times
            var toogleActiveItem = true;

            // look at modifiers
            if (modi == BlazorInput.KeyboardModifiers.Ctrl)
            {
                // keep internal list and (extenal) model in sync
                _selectedItems.ForEach(item => item.IsSelected = true);
            }
            else
            if (modi == BlazorInput.KeyboardModifiers.Shift)
            {
                // make sure active treeViewItem item is in
                SetSelectedState(ve, true);

                // try check if this gives a homogenous pictur
                var nx = _selectedItems.GetIndexedParentInfo();
                if (nx != null && nx.SharedParent?.Members != null)
                {
                    for (int i = nx.MinIndex; i <= nx.MaxIndex; i++)
                        SetSelectedState(nx.SharedParent.Members[i], true);
                }

                toogleActiveItem = false;
            }
            else
            {
                // normal behaviour
                // deselect all selected items (internal + external) except the current one
                // add current
                _selectedItems.ForEach(item => item.IsSelected = (item == ve));
                _selectedItems.Clear();
            }

            // still toggle active?
            if (toogleActiveItem)
            {
                SetSelectedState(ve, !_selectedItems.Contains(ve));
                //if (!_selectedItems.Contains(ve))
                //{
                //    _selectedItems.Add(ve);
                //    ve.IsSelected = true;
                //}
                //else
                //{
                //    // deselect if already selected
                //    ve.IsSelected = false;
                //    _selectedItems.Remove(ve);
                //}
            }
        }
    }
}
