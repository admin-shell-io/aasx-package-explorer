/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using AasxIntegrationBase;
using AnyUi;
using BlazorUI.Data;

namespace BlazorUI
{
    /// <summary>
    /// This class "hosts" the collection of visual elements
    /// </summary>
    public class BlazorVisualElements
    {
        public ListOfVisualElement TreeItems = new ListOfVisualElement();
        private TreeViewLineCache _treeLineCache = null;
        private bool _lastEditMode = false;

        public VisualElementGeneric SelectedItem
        {
            get
            {
                ;                

                return null;
            }
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
                // consider lazy loading
                if (mem is VisualElementEnvironmentItem memei
                    && memei.theItemType == VisualElementEnvironmentItem.ItemType.DummyNode)
                    return false;

                // this member is a leaf!!
                var isIn = false;
                var mdo = mem.GetMainDataObject();
                if (mdo is Aas.IReferable mdorf)
                {
                    var mdoen = mdorf.GetSelfDescription().AasElementName.Trim().ToLower();
                    isIn = fullFilterElementName.IndexOf(mdoen, StringComparison.Ordinal) >= 0;
                }
                else
                if (mdo is Aas.IClass mdoic)
                {
                    // this special case was intruduced because of AssetInformation
                    var mdoen = mdoic.GetType().Name.ToLower();
                    isIn = fullFilterElementName.IndexOf(mdoen, StringComparison.Ordinal) >= 0;
                }
                else
                if (mdo is Aas.Reference)
                {
                    // very special case because of importance
                    var mdoen = (mdo as Aas.Reference).GetSelfDescription().AasElementName.Trim().ToLower();
                    isIn = fullFilterElementName.IndexOf(mdoen, StringComparison.Ordinal) >= 0;
                }
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
                    foreach (var dtl in TreeItems)
                        // it is not likely, that we have to delete on this level, therefore don't care
                        FilterLeavesOfVisualElements(dtl, filterElementName);

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
                TreeItems[0].IsSelected = true;
        }

        /// <summary>
        /// Tries to expand all items, which aren't currently yet, e.g. because of lazy loading.
        /// Is found to be a valid pre-requisite in case of lazy loading for 
        /// <c>SearchVisualElementOnMainDataObject</c>.
        /// Potentially a expensive operation.
        /// </summary>
        public void ExpandAllItems()
        {

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

        public bool TrySelectVisualElement(VisualElementGeneric ve, bool? wishExpanded)
        {
            return false;
        }

        public void Refresh()
        {
            ;
        }
    }
}
