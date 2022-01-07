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
using System.Globalization;
using System.Linq;
using AasxIntegrationBase;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;

// ReSharper disable VirtualMemberCallInConstructor

namespace AasxPackageLogic
{
    /// <summary>
    /// This interface is implemented by the visual tree, which shows the different element of
    /// one or more AASX packages, environments, AAS and so forth.
    /// </summary>
    public interface IManageVisualAasxElements
    {
        VisualElementGeneric GetSelectedItem();
    }

    public class TreeViewLineCache
    {
        public Dictionary<object, bool> IsExpanded = new Dictionary<object, bool>();
    }

    public class VisualElementGeneric : INotifyPropertyChanged, IAnyUiSelectedItem
    {
        // bi-directional tree
        public VisualElementGeneric Parent = null;
        public ListOfVisualElement Members { get; set; }

        /// <summary>
        /// Number of of members at the top of the list, whcih are virtual (e.g. by plug-ins)
        /// and not represented by the AAS-element's children.
        /// </summary>
        public int VirtualMembersAtTop = 0;

        // cache for expanded states
        public TreeViewLineCache Cache = null;

        // members (some dedicated for list / tree like visualisation
        private bool _isExpanded = false;
        private bool _isExpandedTouched = false;
        private bool _isSelected = false;
        public string TagString { get; set; }

        private string _caption = "";
        public string Caption
        {
            get { return _caption; }
            set { _caption = value; this.OnPropertyChanged("Caption"); }
        }

        private string _info = "";
        public string Info
        {
            get { return _info; }
            set { _info = value; this.OnPropertyChanged("Info"); }
        }

        private bool _animateUpdate;
        public bool AnimateUpdate
        {
            get { return _animateUpdate; }
            set { _animateUpdate = value; this.OnPropertyChanged("AnimateUpdate"); }
        }

        public void TriggerAnimateUpdate()
        {
            AnimateUpdate = !AnimateUpdate; this.OnPropertyChanged("AnimateUpdate");
            AnimateUpdate = !AnimateUpdate; this.OnPropertyChanged("AnimateUpdate");
        }

        public string Value { get; set; }
        public string ValueInfo { get; set; }
        public AnyUiColor Background { get; set; }
        public AnyUiColor Border { get; set; }
        public AnyUiColor TagFg { get; set; }
        public AnyUiColor TagBg { get; set; }
        public bool IsTopLevel = false;

        public VisualElementGeneric()
        {
            this.Members = new ListOfVisualElement();
        }

        /// <summary>
        /// Get the data object the visual element is directly associated, e.g. a submodel reference inside an AAS
        /// </summary>
        /// <returns></returns>
        public virtual object GetMainDataObject()
        {
            return null;
        }

        /// <summary>
        /// In case of a reference, get the data object behind it. For a SubmodelRef, it will be the Submodel
        /// </summary>
        /// <returns></returns>
        public virtual object GetDereferencedMainDataObject()
        {
            // by default, its the main data object
            return GetMainDataObject();
        }

        /// <summary>
        /// Returns the state of the IsExpanded from the cache.
        /// The cache associates with the MainDataObject and therefore survives,
        /// even if the the TreeViewLines are completely rebuilt.
        /// </summary>
        public bool GetExpandedStateFromCache()
        {
            var o = this.GetMainDataObject();
            if (o != null && Cache != null && Cache.IsExpanded.ContainsKey(o))
                return Cache.IsExpanded[o];
            return false;
        }

        /// <summary>
        /// Restores the state of the IsExpanded from an cache.
        /// The cache associates with the MainDataObject and therefore survives,
        /// even if the the TreeViewLines are completely rebuilt.
        /// </summary>
        public void RestoreFromCache()
        {
            var o = this.GetMainDataObject();
            if (o != null && Cache != null && Cache.IsExpanded.ContainsKey(o))
            {
                this._isExpanded = Cache.IsExpanded[o];
                this._isExpandedTouched = true;
            }
        }

        /// <summary>
        /// For each different sub-class type of TreeViewLineGeneric,
        /// this methods refreshes attributes such as Caption and Info.
        /// Required, if updates to the MainDataObject shall be reflected on the UI.
        /// </summary>
        public virtual void RefreshFromMainData()
        {
            // to be overloaded for sub-classes!
        }

        /// <summary>
        /// Gets/sets whether the TreeViewItem
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                // store in cache
                var o = this.GetMainDataObject();
                if (o != null && Cache != null)
                    Cache.IsExpanded[o] = value;

                _isExpandedTouched = true;
            }
        }

        public void SetIsExpandedIfNotTouched(bool isExpanded)
        {
            if (!_isExpandedTouched)
                _isExpanded = isExpanded;
        }

        /// <summary>
        /// Gets/sets whether the TreeViewItem
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                //// if (value != _isSelected)
                //// {
                _isSelected = value;
                this.OnPropertyChanged("IsSelected");
                //// }

                // TODO (MIHO, 2020-07-31): check if commented out because of non-working multi-select?
                //// ITreeViewSelectablethis.OnPropertyChanged("IsSelected");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        //
        // Descendent/ parent management
        //

        private bool SearchForDescendentAndCallIfFound(
            VisualElementGeneric descendent, Action<VisualElementGeneric> lambda)
        {
            var res = false;
            if (this == descendent)
            {
                if (lambda != null)
                    lambda(this);
                res = true; // meaning: in this sub-tree, descendent was found!
            }
            if (this.Members != null)
                foreach (var mem in this.Members)
                    res = res || mem.SearchForDescendentAndCallIfFound(descendent, lambda);
            return res;
        }

        public void ForAllDescendents(Action<VisualElementGeneric> lambda)
        {
            if (lambda != null)
                lambda(this);
            if (this.Members != null)
                foreach (var mem in this.Members)
                    mem.ForAllDescendents(lambda);
        }

        public void CollectListOfTopLevelNodes(List<VisualElementGeneric> list)
        {
            if (list == null)
                return;
            if (this.IsTopLevel)
            {
                list.Add(this);
                if (this.Members != null)
                    foreach (var mem in this.Members)
                        mem.CollectListOfTopLevelNodes(list);
            }
        }

        public bool CheckIfDescendent(VisualElementGeneric descendent)
        {
            if (descendent == null)
                return false;
            if (this == descendent)
                return true;
            var res = false;
            if (this.Members != null)
                foreach (var mem in this.Members)
                    res = res || mem.CheckIfDescendent(descendent);
            return res;
        }

        public IEnumerable<VisualElementGeneric> FindAllParents(bool includeThis = false)
        {
            // this?
            if (includeThis)
                yield return this;

            // any parent?
            if (this.Parent == null)
                yield break;

            // yes
            yield return this.Parent;

            // any up?
            foreach (var p in this.Parent.FindAllParents())
                yield return p;
        }

        public IEnumerable<VisualElementGeneric> FindAllParents(
            Predicate<VisualElementGeneric> p, bool includeThis = false)
        {
            // access
            if (p == null)
                yield break;

            // use above
            foreach (var e in this.FindAllParents(includeThis))
                if (p(e))
                    yield return e;
        }

        public VisualElementGeneric FindFirstParent(
            Predicate<VisualElementGeneric> p, bool includeThis = false)
        {
            foreach (var x in FindAllParents(p, includeThis))
                return x;
            return null;
        }

        public VisualElementGeneric FindSibling(bool before = true, bool after = true)
        {
            // need parent -> members
            return Parent?.Members?.FindSibling(this);
        }

        public AdminShell.KeyList BuildKeyListToTop(
            bool includeAas = false)
        {
            // prepare result
            var res = new AdminShell.KeyList();
            var ve = this;
            while (ve != null)
            {
                if (ve is VisualElementSubmodelRef smr)
                {
                    // import special case, as Submodel ref is important part of the chain!
                    if (smr.theSubmodel != null)
                        res.Insert(
                            0,
                            AdminShell.Key.CreateNew(
                                smr.theSubmodel.GetElementName(), smr.theSubmodel.id.value));

                    // include aas
                    if (includeAas && ve.Parent is VisualElementAdminShell veAas
                        && veAas.theAas?.id != null)
                    {
                        res.Insert(
                            0,
                            AdminShell.Key.CreateNew(
                                AdminShell.Key.AAS, veAas.theAas.id.value));
                    }

                    break;
                }
                else
                if (ve.GetMainDataObject() is AdminShell.Identifiable iddata)
                {
                    // a Identifiable will terminate the list of keys
                    res.Insert(
                        0,
                        AdminShell.Key.CreateNew(iddata.GetElementName(), iddata.id.value));
                    break;
                }
                else
                if (ve.GetMainDataObject() is AdminShell.Referable rf)
                {
                    // add a key and go up ..
                    res.Insert(
                        0,
                        AdminShell.Key.CreateNew(rf.GetElementName(), rf.idShort));
                }
                else
                // uups!
                { }
                // need to go up
                ve = ve.Parent;
            }

            return res;
        }

        //
        // Lazy loading
        //

        public bool NeedsLazyLoading
        {
            get
            {
                if (this.Members != null && this.Members.Count == 1
                    && this.Members[0] is VisualElementEnvironmentItem veei
                    && veei.theItemType == VisualElementEnvironmentItem.ItemType.DummyNode)
                    return true;
                return false;
            }
        }
    }

    /// <summary>
    /// Maintains together multiple visual elements and provided.
    /// Note: there is some overlapping with <c>ListOfVisualElement</c>, which is indeed a
    /// observable collection.
    /// </summary>
    public class ListOfVisualElementBasic : List<VisualElementGeneric>
    {
        public bool ExactlyOne { get { return this.Count == 1; } }

        /// <summary>
        /// Check that, if any, all elements share the same parent element.
        /// </summary>
        /// <returns></returns>
        public bool AllWithSameParent()
        {
            VisualElementGeneric theParent = null;
            foreach (var ve in this)
                if (ve?.Parent != null)
                    if (theParent == null)
                        theParent = ve.Parent;
                    else
                        if (theParent != ve.Parent)
                        return false;
            return true;
        }

        /// <summary>
        /// Check, if elements are present and all of same type.
        /// </summary>
        /// <typeparam name="T">Desired subclass of <c>VisualElementGeneric</c></typeparam>
        public bool AllOfElementType<T>() where T : VisualElementGeneric
        {
            if (this.Count < 1)
                return false;
            foreach (var ve in this)
                if (!(ve is T))
                    return false;
            return true;
        }

        public class IndexInfo
        {
            public VisualElementGeneric SharedParent;
            public int MinIndex, MaxIndex;
        }

        public IndexInfo GetIndexedParentInfo()
        {
            // make sure this is possible
            if (this.Count == 0 || !AllWithSameParent())
                return null;

            // subsequently, the parent is obvious
            var parent = this.First()?.Parent;
            if (parent == null)
                // makes no sense
                return null;

            // ok, evaluate indices
            int mini = int.MaxValue, maxi = int.MinValue;
            foreach (var ve in this)
            {
                // emergency exit
                if (ve.Parent != parent || ve.Parent.Members == null || !ve.Parent.Members.Contains(ve))
                    return null;

                // index?
                int ndx = ve.Parent.Members.IndexOf(ve);
                if (ndx < mini)
                    mini = ndx;
                if (ndx > maxi)
                    maxi = ndx;
            }

            // ok result valid?
            if (mini == int.MaxValue || maxi < 0)
                return null;

            return new IndexInfo() { SharedParent = parent, MinIndex = mini, MaxIndex = maxi };
        }

        public List<T> GetListOfBusinessObjects<T>(bool alsoDereferenceObjects = false)
        {
            var res = new List<T>();
            foreach (var x in this)
            {
                var bo = (alsoDereferenceObjects) ? x?.GetDereferencedMainDataObject() : x?.GetMainDataObject();
                if (bo is T bot)
                    res.Add(bot);
            }
            return res;
        }

        public List<T> GetListOfMapResults<T, S>(Func<S, T> lambda) where S : class
        {
            var res = new List<T>();
            foreach (var x in this)
            {
                if (lambda == null || !(x is S))
                    continue;
                var r = lambda.Invoke(x as S);
                if (r != null)
                    res.Add(r);
            }
            return res;
        }

    }

    public class VisualElementEnvironmentItem : VisualElementGeneric
    {
        public enum ItemType
        {
            Env = 0, Shells, Assets, ConceptDescriptions, Package, OrphanSubmodels, AllSubmodels, SupplFiles,
            EmptySet, DummyNode
        };

        public static string[] ItemTypeNames = new string[] {
            "Environment", "AdministrationShells", "Assets", "ConceptDescriptions", "Package", "Orphan Submodels",
            "All Submodels", "Supplementary files", "Empty", "Dummy" };

        public enum ConceptDescSortOrder { None = 0, IdShort, Id, BySubmodel, BySme }

        public string thePackageSourceFn;
        public AdminShellPackageEnv thePackage = null;
        public AdminShell.AdministrationShellEnv theEnv = null;
        public ItemType theItemType = ItemType.Env;
        private AdminShell.IAasElement _mainDataObject;
        private static ConceptDescSortOrder _cdSortOrder = ConceptDescSortOrder.None;

        public VisualElementEnvironmentItem(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShellPackageEnv package,
            AdminShell.AdministrationShellEnv env, ItemType itemType,
            string packageSourceFn = null,
            AdminShell.IAasElement mainDataObject = null)
        : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.thePackage = package;
            this.theEnv = env;
            this.theItemType = itemType;
            this.thePackageSourceFn = packageSourceFn;
            if (mainDataObject != null)
                _mainDataObject = mainDataObject;

            this.Background = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkAccentColor);
            this.Border = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkestAccentColor);
            this.TagBg = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkestAccentColor);
            this.TagFg = AnyUiColors.White;

            this.Caption = $"\"{ItemTypeNames[(int)itemType]}\"";
            this.Info = "";
            this.IsTopLevel = true;
            this.TagString = "Env";
            if (theItemType == ItemType.EmptySet)
            {
                this.TagString = "\u2205";
                this.Caption = "No information available";
            }
            if (theItemType == ItemType.Package && thePackage != null)
            {
                this.TagString = "\u25a2";
                if (thePackageSourceFn != null)
                    this.Info += "" + thePackageSourceFn;
                else
                    this.Info += "" + thePackage.Filename;
            }
            RestoreFromCache();
        }

        public static object GiveAliasDataObject(ItemType t)
        {
            return ItemTypeNames[(int)t];
        }

        public AdminShell.IAasElement MainDataObject { set { _mainDataObject = value; } }

        public override object GetMainDataObject()
        {
            if (_mainDataObject != null)
                return _mainDataObject;
            return GiveAliasDataObject(theItemType);
        }

        public override void RefreshFromMainData()
        {
        }

        public ConceptDescSortOrder CdSortOrder
        {
            get
            {
                return VisualElementEnvironmentItem._cdSortOrder;
            }
            set
            {
                VisualElementEnvironmentItem._cdSortOrder = value;
            }
        }
    }

    public class VisualElementAdminShell : VisualElementGeneric
    {
        public AdminShellPackageEnv thePackage = null;
        public AdminShell.AdministrationShellEnv theEnv = null;
        public AdminShell.AdministrationShell theAas = null;

        public VisualElementAdminShell(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShellPackageEnv package,
            AdminShell.AdministrationShellEnv env, AdminShell.AdministrationShell aas)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.thePackage = package;
            this.theEnv = env;
            this.theAas = aas;

            this.Background = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkAccentColor);
            this.Border = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkestAccentColor);
            this.TagBg = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkestAccentColor);
            this.TagFg = AnyUiColors.White;

            this.TagString = "AAS";
            RefreshFromMainData();
            RestoreFromCache();
        }

        public override object GetMainDataObject()
        {
            return theAas;
        }

        public override void RefreshFromMainData()
        {
            if (theAas != null)
            {
                var ci = theAas.ToCaptionInfo();
                this.Caption = ci.Item1;
                this.Info = ci.Item2;
                var asset = theAas.assetInformation;
                if (asset != null)
                    this.Info += $" of [{asset.globalAssetId.GetAsIdentifier()}, {asset.assetKind.kind}]";
            }
        }
    }

    public class VisualElementAsset : VisualElementGeneric
    {
        public AdminShell.AdministrationShellEnv theEnv = null;
        public AdminShell.AdministrationShell theAas = null;
        public AdminShell.AssetInformation theAsset = null;

        public VisualElementAsset(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShell.AdministrationShellEnv env,
            AdminShell.AdministrationShell aas, AdminShell.AssetInformation asset)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.theEnv = env;
            this.theAas = aas;
            this.theAsset = asset;

            this.Background = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkAccentColor);
            this.Border = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkestAccentColor);
            this.TagBg = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkestAccentColor);
            this.TagFg = AnyUiColors.White;

            this.TagString = "Asset";
            RefreshFromMainData();
            RestoreFromCache();
        }

        public override object GetMainDataObject()
        {
            return theAsset;
        }

        public override void RefreshFromMainData()
        {
            if (theAsset != null)
            {
                var ci = theAsset.ToCaptionInfo();
                this.Caption = "" + ci.Item1;
                this.Info = ci.Item2;
            }
        }
    }

    public class VisualElementSubmodelRef : VisualElementGeneric
    {
        public AdminShell.AdministrationShellEnv theEnv = null;
        public AdminShellPackageEnv thePackage = null;
        public AdminShell.SubmodelRef theSubmodelRef = null;
        public AdminShell.Submodel theSubmodel = null;

        public VisualElementSubmodelRef(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShell.AdministrationShellEnv env,
            AdminShellPackageEnv package,
            AdminShell.SubmodelRef smr, AdminShell.Submodel sm)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.theEnv = env;
            this.thePackage = package;
            this.theSubmodelRef = smr;
            this.theSubmodel = sm;

            this.Background = Options.Curr.GetColor(OptionsInformation.ColorNames.LightAccentColor);
            this.Border = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkestAccentColor);
            this.TagBg = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkestAccentColor);
            this.TagFg = AnyUiColors.White;

            this.TagString = "SM";
            RefreshFromMainData();
            RestoreFromCache();
        }

        public override object GetMainDataObject()
        {
            return theSubmodelRef;
        }

        public override object GetDereferencedMainDataObject()
        {
            return theSubmodel;
        }

        public override void RefreshFromMainData()
        {
            if (theSubmodel != null)
            {
                var ci = theSubmodel.ToCaptionInfo();
                this.Caption = ((theSubmodel.kind != null && theSubmodel.kind.IsTemplate) ? "<T> " : "") + ci.Item1;
                this.Info = ci.Item2;
            }
            else
            {
                this.Caption = "Missing Submodel for Reference!";
                this.Info = "->" + ((this.theSubmodelRef == null) ? "<null>" : this.theSubmodelRef.ToString());
            }
        }

    }

    public class VisualElementSubmodel : VisualElementGeneric
    {
        public AdminShell.AdministrationShellEnv theEnv = null;
        public AdminShell.Submodel theSubmodel = null;

        public VisualElementSubmodel(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShell.AdministrationShellEnv env,
            AdminShell.Submodel sm)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.theEnv = env;
            this.theSubmodel = sm;

            this.Background = new AnyUiColor(0xffd0d0d0u);
            this.Border = new AnyUiColor(0xff606060u);
            this.TagBg = new AnyUiColor(0xff707070u);
            this.TagFg = AnyUiColors.White;

            this.TagString = "SM";
            RefreshFromMainData();
            RestoreFromCache();
        }

        public override object GetMainDataObject()
        {
            return theSubmodel;
        }

        public override void RefreshFromMainData()
        {
            if (theSubmodel != null)
            {
                var ci = theSubmodel.ToCaptionInfo();
                this.Caption = ((theSubmodel.kind != null && theSubmodel.kind.IsTemplate) ? "<T> " : "") + ci.Item1;
                this.Info = ci.Item2;
            }
        }

    }

    public class VisualElementSubmodelElement : VisualElementGeneric
    {
        public AdminShell.AdministrationShellEnv theEnv = null;
        public AdminShell.Referable theContainer = null;
        public AdminShell.SubmodelElementWrapper theWrapper = null;

        private AdminShell.ConceptDescription _cachedCD = null;

        public AdminShell.ConceptDescription CachedCD { get { return _cachedCD; } }

        public VisualElementSubmodelElement(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShell.AdministrationShellEnv env,
            AdminShell.Referable parentContainer, AdminShell.SubmodelElementWrapper wrap)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.theEnv = env;
            this.theContainer = parentContainer;
            this.theWrapper = wrap;

            this.Background = AnyUiColors.White;
            this.Border = AnyUiColors.White;
            this.TagBg = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkestAccentColor);
            this.TagFg = AnyUiColors.White;

            this.TagString = wrap.GetElementAbbreviation();

            RefreshFromMainData();
            RestoreFromCache();
        }

        public override object GetMainDataObject()
        {
            return theWrapper.submodelElement;
        }

        public static void EnrichInfoString(AdminShell.SubmodelElement sme, ref string info, ref bool showCDinfo)
        {
            // access
            if (sme == null || info == null)
                return;

            // case specific
            switch (sme)
            {
                case AdminShell.Property smep:
                    if (smep.value != null && smep.value != "")
                        info += "= " + smep.value;
                    else if (smep.valueId != null && !smep.valueId.IsEmpty)
                        info += "<= " + smep.valueId.ToString();
                    showCDinfo = true;
                    break;

                case AdminShell.Range rng:
                    var txtMin = rng.min == null ? "{}" : rng.min.ToString();
                    var txtMax = rng.max == null ? "{}" : rng.max.ToString();
                    info += $"= {txtMin} .. {txtMax}";
                    showCDinfo = true;
                    break;

                case AdminShell.MultiLanguageProperty mlp:
                    if (mlp.value != null)
                        info += "-> " + mlp.value.GetDefaultStr();
                    break;

                case AdminShell.File smef:
                    if (smef.value != null && smef.value != "")
                        info += "-> " + smef.value;
                    break;

                case AdminShell.ReferenceElement smere:
                    if (smere.value != null && !smere.value.IsEmpty)
                        info += "~> " + smere.value.ToString();
                    break;

                case AdminShell.SubmodelElementCollection smc:
                    if (smc.value != null)
                        info += "(" + smc.value.Count + " elements)";
                    break;
            }

        }

        public override void RefreshFromMainData()
        {
            if (theWrapper != null)
            {
                // start
                var sme = theWrapper.submodelElement;
                var ci = sme.ToCaptionInfo();
                var ciinfo = ci.Item2;
                var showCDinfo = false;

                // extra function
                EnrichInfoString(sme, ref ciinfo, ref showCDinfo);

                // decode
                this.Caption = ((sme.kind != null && sme.kind.IsTemplate) ? "<T> " : "") + ci.Item1;
                this.Info = ciinfo;

                // Show CD / unikts ..
                if (showCDinfo)
                {
                    // cache ConceptDescription?
                    if (sme.semanticId != null && !sme.semanticId.IsEmpty)
                    {
                        if (this._cachedCD == null)
                            this._cachedCD = this.theEnv.FindConceptDescription(sme.semanticId);
                        var iecprop = this._cachedCD?.GetIEC61360();
                        if (iecprop != null)
                        {
                            if (iecprop.unit != null && iecprop.unit != "")
                                this.Info += " [" + iecprop.unit + "]";
                        }
                    }
                }

                // Qualifiers?
                if (sme.qualifiers != null && sme.qualifiers.Count > 0)
                {
                    foreach (var q in sme.qualifiers)
                    {
                        var qt = q.type ?? "";
                        if (qt == "" && q.semanticId != null)
                            qt = "semId";
                        var qv = q.value ?? "";
                        if (qv == "" && q.valueId != null)
                            qv = "valueId";
                        if (qv != "")
                            qv = "=" + qv;
                        this.Info += " @{" + qt + qv + "}";
                    }
                }
            }
        }

    }

    public class VisualElementOperationVariable : VisualElementGeneric
    {
        public AdminShell.AdministrationShellEnv theEnv = null;
        public AdminShell.Referable theContainer = null;
        public AdminShell.OperationVariable theOpVar = null;
        public AdminShell.OperationVariable.Direction theDir = AdminShell.OperationVariable.Direction.In;

        public VisualElementOperationVariable(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShell.AdministrationShellEnv env,
            AdminShell.Referable parentContainer, AdminShell.OperationVariable opvar,
            AdminShell.OperationVariable.Direction dir)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.theEnv = env;
            this.theContainer = parentContainer;
            this.theOpVar = opvar;
            this.theDir = dir;

            this.Background = AnyUiColors.White;
            this.Border = AnyUiColors.White;
            this.TagBg = new AnyUiColor(0xff707070u);
            this.TagFg = AnyUiColors.White;

            this.TagString = "In";
            if (this.theDir == AdminShell.OperationVariable.Direction.Out)
                this.TagString = "Out";
            if (this.theDir == AdminShell.OperationVariable.Direction.InOut)
                this.TagString = "InOut";

            RefreshFromMainData();
            RestoreFromCache();
        }

        public override object GetMainDataObject()
        {
            return theOpVar;
        }

        public override void RefreshFromMainData()
        {
            if (theOpVar != null)
            {
                if (theOpVar.value != null && theOpVar.value.submodelElement != null)
                {
                    // normal stuff
                    var ci2 = theOpVar.value.submodelElement.ToCaptionInfo();
                    var ci2info = ci2.Item2;

                    // add values
                    var showCDinfo = false;
                    VisualElementSubmodelElement.EnrichInfoString(
                        theOpVar.value.submodelElement, ref ci2info, ref showCDinfo);

                    // decode
                    this.Caption = "" + ci2.Item1;
                    this.Info = ci2info;

                }
                else
                {
                    this.Caption = "<no value!>";
                    this.Info = "";
                }
            }
        }
    }


    public class VisualElementConceptDescription : VisualElementGeneric
    {
        public AdminShell.AdministrationShellEnv theEnv = null;
        public AdminShell.ConceptDescription theCD = null;

        public VisualElementConceptDescription(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShell.AdministrationShellEnv env,
            AdminShell.ConceptDescription cd)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.theEnv = env;
            this.theCD = cd;

            this.Background = new AnyUiColor(0xffd0d0d0u);
            this.Border = new AnyUiColor(0xff606060u);
            this.TagBg = new AnyUiColor(0xff707070u);
            this.TagFg = AnyUiColors.White;

            this.TagString = "CD";

            RefreshFromMainData();
            RestoreFromCache();
        }

        public override object GetMainDataObject()
        {
            return theCD;
        }

        public override void RefreshFromMainData()
        {
            if (theCD != null)
            {
                var ci = theCD.ToCaptionInfo();
                this.Caption = "" + ci.Item1 + " ";
                this.Info = ci.Item2;
            }
        }

        // sorting

        public class ComparerIdShort : IComparer<VisualElementGeneric>
        {
            public int Compare(VisualElementGeneric a, VisualElementGeneric b)
            {
                var id1 = (a as VisualElementConceptDescription)?.theCD?.idShort;
                var id2 = (b as VisualElementConceptDescription)?.theCD?.idShort;
                return String.Compare(id1, id2,
                    CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
            }
        }

        public class ComparerIdentification : IComparer<VisualElementGeneric>
        {
            public int Compare(VisualElementGeneric a, VisualElementGeneric b)
            {
                var id1 = (a as VisualElementConceptDescription)?.theCD?.id;
                var id2 = (b as VisualElementConceptDescription)?.theCD?.id;

                if (id1 == null)
                    return -1;
                if (id2 == null)
                    return +1;

                return String.Compare(id1.value, id2.value,
                    CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
            }
        }

#if _not_required
        public class ComparerUsedSubmodel : IComparer<VisualElementGeneric>
        {
            private MultiValueDictionary<AdminShell.ConceptDescription, AdminShell.Submodel> _cdToSm;

            public ComparerUsedSubmodel(MultiValueDictionary<AdminShell.ConceptDescription, AdminShell.Submodel> 
                cdToSm)
            {
                _cdToSm = cdToSm;
            }

            public int Compare(VisualElementGeneric a, VisualElementGeneric b)
            {
                var cd1 = (a as VisualElementConceptDescription)?.theCD;
                var cd2 = (b as VisualElementConceptDescription)?.theCD;

                AdminShell.Identification id1 = null, id2 = null;
                if (cd1 != null && _cdToSm != null && _cdToSm.ContainsKey(cd1))
                    id1 = _cdToSm[cd1].FirstOrDefault()?.identification;
                if (cd2 != null && _cdToSm != null && _cdToSm.ContainsKey(cd2))
                    id2 = _cdToSm[cd2].FirstOrDefault()?.identification;

                if (id1 == null)
                    return +1;
                if (id2 == null)
                    return +1;

                var vc = String.Compare(id1.idType, id2.idType,
                        CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
                if (vc != 0)
                    return vc;

                return String.Compare(id1.id, id2.id,
                    CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
            }
        }
#endif
    }

    public class VisualElementSupplementalFile : VisualElementGeneric
    {
        public AdminShellPackageEnv thePackage = null;
        public AdminShellPackageSupplementaryFile theFile = null;

        public VisualElementSupplementalFile(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShellPackageEnv package,
            AdminShellPackageSupplementaryFile sf)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.thePackage = package;
            this.theFile = sf;

            this.Background = new AnyUiColor(0xffd0d0d0u);
            this.Border = new AnyUiColor(0xff606060u);
            this.TagBg = new AnyUiColor(0xff707070u);
            this.TagFg = AnyUiColors.White;

            this.TagString = "\u25a4";

            RefreshFromMainData();
            RestoreFromCache();
        }

        public override object GetMainDataObject()
        {
            return theFile;
        }

        public override void RefreshFromMainData()
        {
            if (theFile != null)
            {
                this.Caption = "" + theFile.Uri.ToString();
                this.Info = "";

                if (theFile.Location == AdminShellPackageSupplementaryFile.LocationType.AddPending)
                    this.Info += "(add pending) ";
                if (theFile.Location == AdminShellPackageSupplementaryFile.LocationType.DeletePending)
                    this.Info += "(delete pending) ";
                if (theFile.SourceLocalPath != null)
                    this.Info += "\u2b60 " + theFile.SourceLocalPath;

                if (theFile.SpecialHandling == AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail)
                    this.Info += " [Thumbnail]";
            }
        }

    }

    public class VisualElementPluginExtension : VisualElementGeneric
    {
        public AdminShellPackageEnv thePackage = null;
        public AdminShell.Referable theReferable = null;

        public Plugins.PluginInstance thePlugin = null;
        public AasxIntegrationBase.AasxPluginResultVisualExtension theExt = null;

        public VisualElementPluginExtension(
            VisualElementGeneric parent,
            TreeViewLineCache cache,
            AdminShellPackageEnv package,
            AdminShell.Referable referable,
            Plugins.PluginInstance plugin,
            AasxIntegrationBase.AasxPluginResultVisualExtension ext)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.thePackage = package;
            this.theReferable = referable;
            this.thePlugin = plugin;
            this.theExt = ext;

            this.Background = new AnyUiColor(0xffa0a0a0u);
            this.Border = new AnyUiColor(0xff707070u);
            this.TagBg = Options.Curr.GetColor(OptionsInformation.ColorNames.DarkestAccentColor);
            this.TagFg = AnyUiColors.White;

            this.TagString = "" + ext?.Tag;

            RefreshFromMainData();
            RestoreFromCache();
        }

        public override object GetMainDataObject()
        {
            // challenge: there is no constant business object for these virtual instances
            // idea: create a string in a deterministic way; during comparison it shall give the same result?
            // to be checked: only one virtual entity per submodel?

            // need to check Parent, shall be a SubmodelRef!
            var mdoParent = this.Parent?.GetMainDataObject();
            if (mdoParent == null)
                return null;
            var st = String.Format(
                "MDO:VisualElementPluginExtension:{0:X08}:{1:X08}", thePlugin.GetHashCode(), mdoParent.GetHashCode());
            return st;
        }

        public override void RefreshFromMainData()
        {
            if (theExt != null)
            {
                this.Caption = "" + theExt.Caption;
                this.Info = "ready";
            }
        }

    }

    //
    // Generators
    //

    public class ListOfVisualElement : ObservableCollection<VisualElementGeneric>
    {
        public bool OptionLazyLoadingFirst = false;
        public int OptionExpandMode = 0;

        private List<Plugins.PluginInstance> _pluginsToCheck = new List<Plugins.PluginInstance>();

        // need some attach points, which are determined by initial rendering and
        // kept in the class
        private VisualElementEnvironmentItem
            tiPackage = null, tiEnv = null, tiShells = null, tiAssets = null, tiCDs = null;

        private MultiValueDictionary<AdminShell.ConceptDescription, VisualElementGeneric> _cdReferred =
            new MultiValueDictionary<AdminShell.ConceptDescription, VisualElementGeneric>();

        private MultiValueDictionary<AdminShell.ConceptDescription, AdminShell.Submodel> _cdToSm =
            new MultiValueDictionary<AdminShell.ConceptDescription, AdminShell.Submodel>();

        public ListOfVisualElement()
        {
            // interested plug-ins
            _pluginsToCheck.Clear();
            if (Plugins.LoadedPlugins != null)
                foreach (var lpi in Plugins.LoadedPlugins.Values)
                {
                    try
                    {
                        var x =
                            lpi.InvokeAction(
                                "get-check-visual-extension") as AasxIntegrationBase.AasxPluginResultBaseObject;
                        if (x != null && (bool)x.obj)
                            _pluginsToCheck.Add(lpi);
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }
                }
        }

        public VisualElementGeneric FindSibling(VisualElementGeneric item, bool before = true, bool after = true)
        {
            // find index of item
            var i = this.IndexOf(item);
            if (i < 0)
                return null;

            // before?
            if (before && i > 0)
                return this[i - 1];

            // after
            if (after && i < this.Count - 1)
                return this[i + 1];

            // no
            return null;
        }

        private VisualElementGeneric GenerateVisualElementsFromShellEnvAddElements(
            TreeViewLineCache cache, AdminShell.AdministrationShellEnv env,
            AdminShell.Submodel sm, VisualElementGeneric parent,
            AdminShell.Referable parentContainer, AdminShell.SubmodelElementWrapper el)
        {
            // add itself
            var ti = new VisualElementSubmodelElement(parent, cache, env, parentContainer, el);
            parent.Members.Add(ti);

            // bookkeeping
            if (ti.CachedCD != null)
            {
                _cdReferred.Add(ti.CachedCD, ti);
                _cdToSm.Add(ti.CachedCD, sm);
            }

            // nested cd?
            if (tiCDs?.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.BySme
                && ti.CachedCD != null)
            {
                var tiCD = new VisualElementConceptDescription(ti, cache, env, ti.CachedCD);
                ti.Members.Add(tiCD);
            }

            // Recurse: SMC
            if (el.submodelElement is AdminShell.SubmodelElementCollection elc && elc.value != null)
                foreach (var elcc in elc.value)
                    GenerateVisualElementsFromShellEnvAddElements(cache, env, sm, ti, elc, elcc);

            // Recurse: Entity
            // ReSharper disable ExpressionIsAlwaysNull
            if (el.submodelElement is AdminShell.Entity ele && ele.statements != null)
                foreach (var eles in ele.statements)
                    GenerateVisualElementsFromShellEnvAddElements(cache, env, sm, ti, ele, eles);
            // ReSharper enable ExpressionIsAlwaysNull

            // Recurse: Operation
            if (el.submodelElement is AdminShell.Operation elo)
            {
                if (elo.inputVariable != null)
                    foreach (var vin in elo.inputVariable)
                        ti.Members.Add(
                            new VisualElementOperationVariable(
                                ti, cache, env, el.submodelElement, vin, AdminShell.OperationVariable.Direction.In));
                if (elo.outputVariable != null)
                    foreach (var vout in elo.outputVariable)
                        ti.Members.Add(
                            new VisualElementOperationVariable(
                                ti, cache, env, el.submodelElement, vout, AdminShell.OperationVariable.Direction.Out));
                if (elo.inoutputVariable != null)
                    foreach (var vout in elo.inoutputVariable)
                        ti.Members.Add(
                            new VisualElementOperationVariable(
                                ti, cache, env, el.submodelElement, vout,
                                AdminShell.OperationVariable.Direction.InOut));
            }

            // Recurse: AnnotatedRelationshipElement
            if (el.submodelElement is AdminShell.AnnotatedRelationshipElement ela && ela.annotations != null)
                foreach (var elaa in ela.annotations)
                    GenerateVisualElementsFromShellEnvAddElements(cache, env, sm, ti, ela, elaa);

            // return topmost
            return ti;
        }

        private void GenerateInnerElementsForSubmodelRef(
            TreeViewLineCache cache, AdminShell.AdministrationShellEnv env, AdminShellPackageEnv package,
            AdminShell.Submodel sm,
            VisualElementSubmodelRef tiSm)
        {
            // access
            if (sm == null || tiSm == null)
                return;

            // check for visual extensions
            if (_pluginsToCheck != null)
                foreach (var lpi in _pluginsToCheck)
                {
                    try
                    {
                        var ext = lpi.InvokeAction(
                            "call-check-visual-extension", sm)
                            as AasxIntegrationBase.AasxPluginResultVisualExtension;
                        if (ext != null)
                        {
                            var tiExt = new VisualElementPluginExtension(
                                tiSm, cache, package, sm, lpi, ext);
                            tiSm.Members.Add(tiExt);
                            tiSm.VirtualMembersAtTop++;
                        }
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }
                }

            // recursively into the submodel elements
            if (sm.submodelElements != null)
                foreach (var sme in sm.submodelElements)
                    GenerateVisualElementsFromShellEnvAddElements(cache, env, sm, tiSm, sm, sme);
        }

        private VisualElementSubmodelRef GenerateVisuElemForVisualElementSubmodelRef(
            AdminShell.Submodel sm,
            AdminShell.SubmodelRef smr,
            VisualElementGeneric parent,
            TreeViewLineCache cache, AdminShell.AdministrationShellEnv env, AdminShellPackageEnv package = null)
        {
            // trivial
            if (smr == null || sm == null)
                return null;

            // item (even if sm is null)
            var tiSm = new VisualElementSubmodelRef(parent, cache, env, package, smr, sm);
            tiSm.SetIsExpandedIfNotTouched(OptionExpandMode > 1);

            if (OptionLazyLoadingFirst && !tiSm.GetExpandedStateFromCache())
            {
                // set lazy loading first
                SetElementToLazyLoading(cache, env, package, tiSm);
            }
            else
            {
                // inner items directly
                GenerateInnerElementsForSubmodelRef(cache, env, package, sm, tiSm);
            }

            // ok
            return tiSm;
        }

        private VisualElementAdminShell GenerateVisuElemForAAS(
            AdminShell.AdministrationShell aas,
            TreeViewLineCache cache, AdminShell.AdministrationShellEnv env, AdminShellPackageEnv package = null,
            bool editMode = false)
        {
            // trivial
            if (aas == null)
                return null;

            // item
            var tiAas = new VisualElementAdminShell(null, cache, package, env, aas);
            tiAas.SetIsExpandedIfNotTouched(OptionExpandMode > 0);

            // add asset as well (visual legacy of V2.0)
            var asset = aas.assetInformation;
            if (asset != null)
            {
                // item
                var tiAsset = new VisualElementAsset(tiAas, cache, env, aas, asset);
                tiAas.Members.Add(tiAsset);
            }

            // have submodels?
            if (aas.submodelRefs != null)
                foreach (var smr in aas.submodelRefs)
                {
                    var sm = env.FindSubmodel(smr);
                    if (sm == null)
                    {
                        // notify user
                        Log.Singleton.Error("Cannot find some submodel!");

                        // make reference with NO submodel behind
                        var tiNoSm = new VisualElementSubmodelRef(
                            tiAas, cache, env, package, smr, sm: null);
                        tiAas.Members.Add(tiNoSm);
                    }

                    // generate
                    var tiSm = GenerateVisuElemForVisualElementSubmodelRef(
                        sm, smr, tiAas, cache, env, package);

                    // add
                    if (tiSm != null)
                        tiAas.Members.Add(tiSm);
                }

            // ok
            return tiAas;
        }

        // see: https://stackoverflow.com/questions/19112922/sort-observablecollectionstring-through-c-sharp
        private static void ObservableCollectionSort<T>(ObservableCollection<T> collection, IComparer<T> comparison)
        {
            var sortableList = new List<T>(collection);
            sortableList.Sort(comparison);

            for (int i = 0; i < sortableList.Count; i++)
            {
                collection.Move(collection.IndexOf(sortableList[i]), i);
            }
        }

        private void GenerateInnerElementsForConceptDescriptions(
            TreeViewLineCache cache, AdminShell.AdministrationShellEnv env,
            VisualElementEnvironmentItem tiCDs,
            VisualElementGeneric root,
            bool doSort = true)
        {
            // access
            if (env == null || tiCDs == null || root == null)
                return;

            //
            // create 
            //

            foreach (var cd in env.ConceptDescriptions)
            {
                // item
                var tiCD = new VisualElementConceptDescription(tiCDs, cache, env, cd);

                // stop criteria for adding?
                if (tiCDs.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.BySme
                    && _cdReferred.ContainsKey(cd))
                    continue;

                if (tiCDs.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.BySubmodel
                    && _cdToSm.ContainsKey(cd))
                    continue;

                // add
                root.Members.Add(tiCD);
            }

            //
            // sort
            //

            if (doSort)
            {
                // sort CDs?
                if (tiCDs.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.IdShort)
                {
                    tiCDs.Info = "(dynamically sorted: idShort)";
                    ObservableCollectionSort<VisualElementGeneric>(
                        tiCDs.Members, new VisualElementConceptDescription.ComparerIdShort());
                }

                if (tiCDs.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.Id)
                {
                    tiCDs.Info = "(dynamically sorted: Identification)";
                    ObservableCollectionSort<VisualElementGeneric>(
                        tiCDs.Members, new VisualElementConceptDescription.ComparerIdentification());
                }

                if (tiCDs.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.BySme)
                {
                    tiCDs.Info = "(only CD not referenced by any SubmodelElement)";
                }

                if (tiCDs.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.BySubmodel)
                {
                    tiCDs.Info = "(only CD not referenced by any Submodel)";
                }
            }
        }

        public void AddVisualElementsFromShellEnv(
            TreeViewLineCache cache, AdminShell.AdministrationShellEnv env, AdminShellPackageEnv package = null,
            string packageSourceFn = null,
            bool editMode = false, int expandMode = 0, bool lazyLoadingFirst = false)
        {
            // temporary tree
            var res = new ListOfVisualElement();

            // valid?
            if (env == null)
                return;

            // remember options
            OptionExpandMode = expandMode;
            OptionLazyLoadingFirst = lazyLoadingFirst;

            // quickly connect the Identifiables to the environment
            {
                foreach (var aas in env.AdministrationShells)
                    if (aas != null)
                        aas.parent = env;

                foreach (var sm in env.Submodels)
                    if (sm != null)
                        sm.parent = env;

                foreach (var cd in env.ConceptDescriptions)
                    if (cd != null)
                        cd.parent = env;
            }

            // many operations
            try
            {
                if (editMode)
                {
                    // package
                    tiPackage = new VisualElementEnvironmentItem(
                        null /* Parent */, cache, package, env, VisualElementEnvironmentItem.ItemType.Package,
                        packageSourceFn);
                    tiPackage.SetIsExpandedIfNotTouched(true);
                    res.Add(tiPackage);

                    // env
                    tiEnv = new VisualElementEnvironmentItem(
                        tiPackage, cache, package, env, VisualElementEnvironmentItem.ItemType.Env);
                    tiEnv.SetIsExpandedIfNotTouched(expandMode > 0);
                    tiPackage.Members.Add(tiEnv);

                    // concept descriptions
                    // note: will be added later to the overall tree
                    tiCDs = new VisualElementEnvironmentItem(
                        tiEnv, cache, package, env, VisualElementEnvironmentItem.ItemType.ConceptDescriptions,
                        mainDataObject: env.ConceptDescriptions);
                    tiCDs.SetIsExpandedIfNotTouched(expandMode > 0);

                    // the selected sort order may cause disabling of lazy loading for this class!
                    if (tiCDs.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.BySubmodel
                        || tiCDs.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.BySme)
                    {
                        OptionLazyLoadingFirst = false;
                    }

                    // shells
                    tiShells = new VisualElementEnvironmentItem(
                        tiEnv, cache, package, env, VisualElementEnvironmentItem.ItemType.Shells);
                    tiShells.SetIsExpandedIfNotTouched(expandMode > 0);
                    tiEnv.Members.Add(tiShells);

                    // assets
                    tiAssets = new VisualElementEnvironmentItem(
                        tiEnv, cache, package, env, VisualElementEnvironmentItem.ItemType.Assets);
                    tiAssets.SetIsExpandedIfNotTouched(expandMode > 0);
                    tiEnv.Members.Add(tiAssets);
                }

                // over all Admin shells
                foreach (var aas in env.AdministrationShells)
                {
                    // item
                    var tiAas = GenerateVisuElemForAAS(aas, cache, env, package, editMode);

                    // add item
                    if (tiAas != null)
                    {
                        if (editMode)
                        {
                            tiAas.Parent = tiShells;
                            tiShells.Members.Add(tiAas);
                        }
                        else
                        {
                            res.Add(tiAas);
                        }
                    }
                }

                // if edit mode, then display further ..
                if (editMode)
                {
                    //
                    // over all Submodels (not the refs)
                    //
                    var tiAllSubmodels = new VisualElementEnvironmentItem(
                        tiEnv, cache, package, env, VisualElementEnvironmentItem.ItemType.AllSubmodels,
                        mainDataObject: env.Submodels);
                    tiAllSubmodels.SetIsExpandedIfNotTouched(expandMode > 0);
                    tiEnv.Members.Add(tiAllSubmodels);

                    // show all Submodels
                    foreach (var sm in env.Submodels)
                    {
                        // Submodel
                        var tiSm = new VisualElementSubmodel(tiAllSubmodels, cache, env, sm);
                        tiSm.SetIsExpandedIfNotTouched(expandMode > 1);
                        tiAllSubmodels.Members.Add(tiSm);

                        // render ConceptDescriptions?
                        if (tiCDs.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.BySubmodel)
                        {
                            foreach (var cd in env.ConceptDescriptions)
                            {
                                var found = false;
                                if (_cdToSm.ContainsKey(cd))
                                    foreach (var x in _cdToSm[cd])
                                        if (x == sm)
                                        {
                                            found = true;
                                            break;
                                        }

                                if (found)
                                {
                                    // item
                                    var tiCD = new VisualElementConceptDescription(tiSm, cache, env, cd);
                                    tiSm.Members.Add(tiCD);
                                }
                            }
                        }
                    }

                    //
                    // over all concept descriptions
                    //
                    tiEnv.Members.Add(tiCDs);

                    if (OptionLazyLoadingFirst)
                    {
                        // set lazy loading first
                        SetElementToLazyLoading(cache, env, package, tiCDs);
                    }
                    else
                    {
                        GenerateInnerElementsForConceptDescriptions(cache, env, tiCDs, tiCDs);
                    }

                }

                // package as well?
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (editMode && package != null && tiPackage != null)
                {
                    // file folder
                    var tiFiles = new VisualElementEnvironmentItem(
                        tiPackage, cache, package, env, VisualElementEnvironmentItem.ItemType.SupplFiles);
                    tiFiles.SetIsExpandedIfNotTouched(expandMode > 0);
                    tiPackage.Members.Add(tiFiles);

                    if (OptionLazyLoadingFirst)
                    {
                        // set lazy loading first
                        SetElementToLazyLoading(cache, env, package, tiFiles);
                    }
                    else
                    {
                        // single files
                        var files = package.GetListOfSupplementaryFiles();
                        foreach (var fi in files)
                            tiFiles.Members.Add(new VisualElementSupplementalFile(tiFiles, cache, package, fi));
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "Generating tree of visual elements");
            }

            // end
            foreach (var r in res)
                this.Add(r);
        }

        private void SetElementToLazyLoading(
            TreeViewLineCache cache, AdminShell.AdministrationShellEnv env, AdminShellPackageEnv package,
            VisualElementGeneric parent)
        {
            var tiDummy = new VisualElementEnvironmentItem(parent, cache, package, env,
                                        VisualElementEnvironmentItem.ItemType.DummyNode);
            parent.Members.Add(tiDummy);
            parent.IsExpanded = false;
        }

        public void ExecuteLazyLoading(VisualElementGeneric ve, bool forceExpanded = false)
        {
            // access
            if (ve == null || !ve.NeedsLazyLoading)
                return;

            // try trigger loading
            if (ve is VisualElementEnvironmentItem veei
                && veei.theItemType == VisualElementEnvironmentItem.ItemType.ConceptDescriptions)
            {
                ve.Members.Clear();
                GenerateInnerElementsForConceptDescriptions(veei.Cache, veei.theEnv, veei, ve);

                ve.RestoreFromCache();
                if (forceExpanded)
                    ve.IsExpanded = true;
            }

            if (ve is VisualElementEnvironmentItem vesf
                && vesf.theItemType == VisualElementEnvironmentItem.ItemType.SupplFiles)
            {
                ve.Members.Clear();

                // single files
                if (vesf.thePackage != null)
                {
                    var files = vesf.thePackage.GetListOfSupplementaryFiles();
                    foreach (var fi in files)
                        ve.Members.Add(new VisualElementSupplementalFile(ve, vesf.Cache, vesf.thePackage, fi));
                }

                ve.RestoreFromCache();
                if (forceExpanded)
                    ve.IsExpanded = true;
            }

            if (ve is VisualElementSubmodelRef vesmr)
            {
                ve.Members.Clear();
                GenerateInnerElementsForSubmodelRef(vesmr.Cache, vesmr.theEnv, vesmr.thePackage, vesmr.theSubmodel,
                    vesmr);

                ve.RestoreFromCache();
                if (forceExpanded)
                    ve.IsExpanded = true;
            }
        }

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
            foreach (var tvl in this)
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

        public bool ContainsDeep(VisualElementGeneric ve)
        {
            // ReSharper disable UnusedVariable
            foreach (var e in FindAllVisualElement((o) => { return ve == o; }))
                return true;
            // ReSharper enable UnusedVariable
            return false;
        }

        private IEnumerable<VisualElementGeneric> FindAllInListOfVisualElements(
            VisualElementGeneric tvl, object dataObject, bool alsoDereferenceObjects = false,
            int recursionDepth = int.MaxValue)
        {
            if (tvl == null || dataObject == null)
                yield break;

            // Test for VirtualEntities. Allow a string comparison
            var mdo = tvl.GetMainDataObject();
            if (mdo == null)
                yield break;
            var s1 = mdo as string;
            var s2 = dataObject as string;
            if (s1 != null && s1 == s2)
                yield return tvl;

            // normal comparison
            if (tvl.GetMainDataObject() == dataObject)
                yield return tvl;

            // extended?
            if (alsoDereferenceObjects && tvl.GetDereferencedMainDataObject() == dataObject)
                yield return tvl;

            // recursion
            if (recursionDepth > 0)
                foreach (var mem in tvl.Members)
                {
                    foreach (var x in FindAllInListOfVisualElements(mem, dataObject, alsoDereferenceObjects,
                                        recursionDepth - 1))
                        if (x != null)
                            yield return x;
                }
        }

        private IEnumerable<VisualElementGeneric> InternalFindAllVisualElementOnMainDataObject(object dataObject,
            bool alsoDereferenceObjects = false)
        {
            foreach (var tvl in this)
            {
                foreach (var x in FindAllInListOfVisualElements(tvl, dataObject, alsoDereferenceObjects))
                    if (x != null)
                        yield return x;
            }
        }

        public class SupplementaryReferenceInformation
        {
            public AdminShell.ModelReference CleanReference;

            public string SearchPluginTag = null;
        }

        public static SupplementaryReferenceInformation StripSupplementaryReferenceInformation(
            AdminShell.ModelReference rf)
        {
            // in any case, provide record
            var sri = new SupplementaryReferenceInformation();
            sri.CleanReference = new AdminShell.ModelReference(rf);

            // plug-in?
            var srl = sri.CleanReference.Last;
            if (srl?.type == AdminShell.Key.FragmentReference && srl?.value?.StartsWith("Plugin:") == true)
            {
                sri.SearchPluginTag = srl.value.Substring("Plugin:".Length);
                sri.CleanReference.Keys.Remove(srl);
            }

            // ok
            return sri;
        }

        public IEnumerable<VisualElementGeneric> FindAllVisualElementOnMainDataObject(object dataObject,
            bool alsoDereferenceObjects = false,
            SupplementaryReferenceInformation sri = null)
        {
            // call internal
            foreach (var ve0 in InternalFindAllVisualElementOnMainDataObject(dataObject, alsoDereferenceObjects))
            {
                // trivial
                var ve = ve0;
                if (ve == null)
                    continue;

                // refine ve?
                if (sri != null)
                {
                    // plugin?
                    if (sri.SearchPluginTag != null && ve is VisualElementSubmodelRef veSm
                        && veSm.Members != null)
                        foreach (var vem in veSm.Members)
                            if (vem is VisualElementPluginExtension vepe)
                                if (vepe.theExt?.Tag?.Trim().ToLower() == sri.SearchPluginTag.Trim().ToLower())
                                {
                                    ve = vepe;
                                    break;
                                }
                }

                // yield this
                yield return ve;
            }
        }

        public IEnumerable<T> FindAllVisualElementOnMainDataObject<T>(object dataObject,
            bool alsoDereferenceObjects = false,
            SupplementaryReferenceInformation sri = null) where T : VisualElementGeneric
        {
            foreach (var ve in
                FindAllVisualElementOnMainDataObject(dataObject, alsoDereferenceObjects, sri))
            {
                var vet = ve as T;
                if (vet != null)
                    yield return vet;
            }
        }

        public VisualElementGeneric FindFirstVisualElementOnMainDataObject(object dataObject,
            bool alsoDereferenceObjects = false,
            SupplementaryReferenceInformation sri = null)
        {
            return FindAllVisualElementOnMainDataObject(dataObject, alsoDereferenceObjects, sri).FirstOrDefault();
        }

        //
        // Feedback of VE information back to hiearchy of AAS elements
        //

        /// <summary>
        /// This functions uses the VE hierarchy to safeguard a correct <c>parent</c> setting for
        /// an edited AAS element. Is it called by the editor.
        /// </summary>
        /// <param name="entity"></param>
        public static void SetParentsBasedOnChildHierarchy(VisualElementGeneric entity)
        {
            if (entity is VisualElementEnvironmentItem veei)
            {
            }
            else if (entity is VisualElementAdminShell veaas && veaas.theAas != null)
            {
                // maintain parent. If in doubt, set null
                veaas.theAas.parent = veaas.theEnv;
            }
            else if (entity is VisualElementAsset veas && veas.theAsset != null)
            {
                // maintain parent. If in doubt, set null
                veas.theAsset.parent = veas.theEnv;
            }
            else if (entity is VisualElementSubmodelRef vesmref)
            {
                // no parent maintained
            }
            else if (entity is VisualElementSubmodel vesm && vesm.theSubmodel != null)
            {
                // maintain parent. If in doubt, set null
                vesm.theSubmodel.parent = vesm.theEnv;
            }
            else if (entity is VisualElementSubmodelElement vesme)
            {
                var parVe = entity.Parent;
                var currRf = vesme.GetDereferencedMainDataObject() as AdminShell.Referable;
                while (parVe != null && currRf != null)
                {
                    var parMdo = parVe?.GetDereferencedMainDataObject();
                    if (parMdo is AdminShell.Referable parMdoRf)
                    {
                        // set parent
                        currRf.parent = parMdoRf;

                        // go next
                        currRf = parMdoRf;
                        parVe = parVe.Parent;
                    }
                    else
                    {
                        // simply stop
                        break;
                    }
                }
            }
            else if (entity is VisualElementOperationVariable vepv)
            {
                // try access element itself
                if (vepv.GetMainDataObject() is AdminShell.SubmodelElement sme)
                {
                    // be careful
                    sme.parent = null;

                    // try get parent data
                    if (entity.Parent is VisualElementSubmodelElement parVe
                        && parVe.GetMainDataObject() is AdminShell.Operation parVeOp)
                    {
                        // set parent
                        sme.parent = parVeOp;

                        // recurse?
                        SetParentsBasedOnChildHierarchy(entity.Parent);
                    }
                }
            }
            else if (entity is VisualElementConceptDescription vecd && vecd.theCD != null)
            {
                // maintain parent. If in doubt, set null
                vecd.theCD.parent = vecd.theEnv;
            }
            else
            if (entity is VisualElementSupplementalFile vesf)
            {
                // not applicable
            }
            else if (entity is VisualElementPluginExtension vepe)
            {
                // not applicable
            }
            else
            {
            }
        }

        //
        // Implementation of event queue
        //

#if _relaize_in_DisplayVisualAasxElements
        private List<PackCntChangeEventData> _eventQueue = new List<PackCntChangeEventData>();

        public void PushEvent(PackCntChangeEventData data)
        {
            lock (_eventQueue)
            {
                _eventQueue.Add(data);
            }
        }

        public void UpdateFromQueuedEvents(TreeViewLineCache cache, bool editMode = false)
        {
            lock (_eventQueue)
            {
                foreach (var e in _eventQueue)
                    UpdateByEvent(e, cache, editMode);
                _eventQueue.Clear();
            }
        }
#endif

        private int UpdateByEventTryMoveGenericVE(PackCntChangeEventData data)
        {
            // count moves
            int res = 0;

            // find the correct parent(s)
            foreach (var parentVE in FindAllVisualElementOnMainDataObject(
                data.ParentElem, alsoDereferenceObjects: true))
            {
                // trivial
                var ni = parentVE.VirtualMembersAtTop + data.NewIndex;
                if (parentVE?.Members == null || ni < 0 || ni >= parentVE.Members.Count)
                    continue;

                // now, below these find direct childs matching the SME (only these can be removed)
                VisualElementGeneric childVE = null;
                foreach (var x in parentVE.Members)
                    if (x.GetMainDataObject() == data.ThisElem)
                        childVE = x;
                if (childVE == null)
                    continue;

                // remove child
                parentVE.Members.Remove(childVE);

                // insert child
                parentVE.Members.Insert(ni, childVE);

                // moved
                res++;
            }

            // ok
            return res;
        }

        private int UpdateByEventTryDeleteGenericVE(PackCntChangeEventData data)
        {
            // count moves
            int res = 0;

            // find the correct parent(s)
            foreach (var parentVE in FindAllVisualElementOnMainDataObject(
                data.ParentElem, alsoDereferenceObjects: true))
            {
                // trivial
                if (parentVE?.Members == null)
                    continue;

                // now, below these find direct childs matching the SME (only these can be removed)
                var childsToDel = new List<VisualElementGeneric>();
                foreach (var x in parentVE.Members)
                    if (x.GetMainDataObject() == data.ThisElem)
                        childsToDel.Add(x);

                // AFTER iterating, do the removal
                foreach (var ctd in childsToDel)
                {
                    parentVE.Members.Remove(ctd);
                    // moved
                    res++;
                }
            }

            // ok
            return res;
        }

        public bool UpdateByEvent(
            PackCntChangeEventData data,
            TreeViewLineCache cache)
        {
            //
            // Create
            //

            if (data.Reason == PackCntChangeEventReason.Create)
            {
                if (data.ParentElem is AdminShell.AdministrationShell parentAas
                    && data.ThisElem is AdminShell.Submodel thisSm)
                {
                    // try find according visual elements by business objects == Referables
                    // presumably, this is only one AAS Element
                    foreach (var parentVE in FindAllVisualElementOnMainDataObject<VisualElementAdminShell>(
                        data.ParentElem, alsoDereferenceObjects: false))
                    {
                        if (parentVE == null)
                            continue;

                        // figure out the SubmodelRef
                        var smr = parentAas.FindSubmodelRef(thisSm.id);
                        if (smr == null)
                            continue;

                        // generate
                        var tiSm = GenerateVisuElemForVisualElementSubmodelRef(
                            thisSm, smr, parentVE, cache,
                            data.Container?.Env?.AasEnv, data.Container?.Env);

                        // add
                        if (tiSm != null)
                            parentVE.Members.Add(tiSm);
                    }

                    // additionally, there might be also as pure Submodel item
                    foreach (var tiAllSubmodels in FindAllVisualElement((ve) =>
                        (ve is VisualElementEnvironmentItem veei
                         && veei.theItemType == VisualElementEnvironmentItem.ItemType.AllSubmodels)))
                    {
                        var tiSm = new VisualElementSubmodel(tiAllSubmodels, cache,
                                    data.Container?.Env?.AasEnv, thisSm);
                        tiSm.SetIsExpandedIfNotTouched(false);
                        tiAllSubmodels.Members.Add(tiSm);
                    }

                    // just good
                    return true;
                }
                else
                if (data.ParentElem is AdminShell.Submodel parentSm
                    && data.ThisElem is AdminShell.SubmodelElement thisSme)
                {
                    // try specifically SubmodelRef visual elements by Submodel business object,
                    // as these are the carriers of child information
                    foreach (var parentVE in FindAllVisualElementOnMainDataObject<VisualElementSubmodelRef>(
                        parentSm, alsoDereferenceObjects: true))
                    {
                        if (parentVE == null)
                            continue;

                        // try find wrapper for sme 
                        var foundSmw = parentSm.submodelElements.FindSubModelElement(thisSme);
                        if (foundSmw == null)
                            continue;

                        // add to parent
                        GenerateVisualElementsFromShellEnvAddElements(
                            cache, data.Container?.Env?.AasEnv, parentSm, parentVE,
                            data.ParentElem as AdminShell.Referable, foundSmw);
                    }

                    // just good
                    return true;
                }
                else
                if (data.ParentElem is AdminShell.IManageSubmodelElements parentMgr
                    && data.ParentElem is AdminShell.IEnumerateChildren parentEnum
                    && data.ThisElem is AdminShell.SubmodelElement thisSme2)
                {
                    // try find according visual elements by business objects == Referables
                    foreach (var parentVE in FindAllVisualElementOnMainDataObject(
                        data.ParentElem, alsoDereferenceObjects: false))
                    {
                        if (parentVE == null)
                            continue;

                        // try find wrapper for sme 
                        AdminShell.SubmodelElementWrapper foundSmw = null;
                        foreach (var smw in parentEnum.EnumerateChildren())
                            if (smw?.submodelElement == thisSme2)
                            {
                                foundSmw = smw;
                                break;
                            }

                        if (foundSmw == null)
                            continue;

                        // add to parent
                        // TODO (MIHO, 2021-06-11): Submodel needs to be set in the long run
                        var ti = GenerateVisualElementsFromShellEnvAddElements(
                            cache, data.Container?.Env?.AasEnv, null, parentVE,
                            data.ParentElem as AdminShell.Referable, foundSmw);

                        // animate
                        if (ti != null)
                        {
                            // do not TriggerAnimateUpdate(), but set to true in order to animate
                            // when realized
                            ti.AnimateUpdate = true;
                        }
                    }

                    // just good
                    return true;
                }
            }

            //
            // Delete
            //

            if (data.Reason == PackCntChangeEventReason.Delete)
            {
                if (data.ParentElem is AdminShell.IManageSubmodelElements parentMgr
                    && data.ThisElem is AdminShell.SubmodelElement sme)
                {
                    return 0 < UpdateByEventTryDeleteGenericVE(data);
                }

                if (data.ParentElem is AdminShell.AdministrationShell aas
                    && data.ThisElem is AdminShell.SubmodelRef smr)
                {
                    return 0 < UpdateByEventTryDeleteGenericVE(data);
                }

                if (data.ParentElem is AdminShell.ListOfConceptDescriptions
                    && data.ThisElem is AdminShell.ConceptDescription cd)
                {
                    // as the CD might be rendered below mayn different elements (SME, SM, LoCD, ..)
                    // brutally delete all occurences
                    var childsToDel = new List<VisualElementGeneric>();
                    foreach (var ve in FindAllVisualElementOnMainDataObject(
                       cd, alsoDereferenceObjects: true))
                    {
                        // valid
                        if (ve.Parent == null)
                            continue;

                        // remember
                        childsToDel.Add(ve);
                    }

                    // AFTER iterating, do the removal
                    foreach (var ctd in childsToDel)
                        ctd?.Parent?.Members.Remove(ctd);

                    // just good
                    return true;
                }

            }

            //
            // MoveToIndex
            //

            if (data.Reason == PackCntChangeEventReason.MoveToIndex)
            {
                if (data.ParentElem is AdminShell.IManageSubmodelElements parentMgr
                    && data.ThisElem is AdminShell.SubmodelElement sme)
                {
                    return 0 < UpdateByEventTryMoveGenericVE(data);
                }

                if (data.ParentElem is AdminShell.AdministrationShell aas
                    && data.ThisElem is AdminShell.SubmodelRef smref)
                {
                    return 0 < UpdateByEventTryMoveGenericVE(data);
                }

                if (data.ParentElem is AdminShell.ListOfConceptDescriptions cds
                    && data.ThisElem is AdminShell.ConceptDescription cd)
                {
                    return 0 < UpdateByEventTryMoveGenericVE(data);
                }
            }

            //
            // Update
            //

            if (data.Reason == PackCntChangeEventReason.ValueUpdateSingle)
            {
                if (data.ThisElem is AdminShell.SubmodelElement sme)
                {
                    // find the correct parent(s)
                    foreach (var ve in FindAllVisualElementOnMainDataObject(
                        data.ThisElem, alsoDereferenceObjects: false))
                    {
                        // trivial
                        if (ve == null)
                            continue;

                        // trigger update, SME value is supposed to be actual
                        ve.RefreshFromMainData();
                        ve.TriggerAnimateUpdate();
                    }
                }
            }

            //
            // StructuralUpdate
            //

            if (data.Reason == PackCntChangeEventReason.StructuralUpdate)
            {
                if (data.ThisElem is AdminShell.ListOfConceptDescriptions lcds)
                {
                    // find the correct parent(s)
                    foreach (var ve in FindAllVisualElementOnMainDataObject(
                        data.ThisElem, alsoDereferenceObjects: false))
                        if (ve is VisualElementEnvironmentItem veit
                            && veit.theItemType == VisualElementEnvironmentItem.ItemType.ConceptDescriptions
                            && veit.theEnv != null)
                        {
                            // rebuild
                            veit.Members.Clear();
                            GenerateInnerElementsForConceptDescriptions(cache, veit.theEnv, veit, veit);
                        }
                }
            }


            return false;
        }

    }

}
