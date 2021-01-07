/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using AdminShellNS;

// ReSharper disable VirtualMemberCallInConstructor

namespace AasxPackageExplorer
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

    public class VisualElementGeneric : INotifyPropertyChanged, ITreeViewSelectable
    {
        // bi-directional tree
        public VisualElementGeneric Parent = null;
        public ObservableCollection<VisualElementGeneric> Members { get; set; }

        // cache for expanded states
        public TreeViewLineCache Cache = null;

        // members (some dedicated for list / tree like visualisation
        private bool _isExpanded = false;
        private bool _isExpandedTouched = false;
        private bool _isSelected = false;
        public string TagString { get; set; }
        public string Caption { get; set; }
        public string Info { get; set; }
        public string Value { get; set; }
        public string ValueInfo { get; set; }
        public Brush Background { get; set; }
        public Brush Border { get; set; }
        public Brush TagFg { get; set; }
        public Brush TagBg { get; set; }
        public bool IsTopLevel = false;

        public VisualElementGeneric()
        {
            this.Members = new ObservableCollection<VisualElementGeneric>();
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
                                smr.theSubmodel.GetElementName(), true,
                                smr.theSubmodel.identification.idType,
                                smr.theSubmodel.identification.id));

                    // include aas
                    if (includeAas && ve.Parent is VisualElementAdminShell veAas
                        && veAas.theAas?.identification != null)
                    {
                        res.Insert(
                            0,
                            AdminShell.Key.CreateNew(
                                AdminShell.Key.AAS, true,
                                veAas.theAas.identification.idType,
                                veAas.theAas.identification.id));
                    }

                    break;
                }
                else
                if (ve.GetMainDataObject() is AdminShell.Identifiable iddata)
                {
                    // a Identifiable will terminate the list of keys
                    if (iddata != null)
                        res.Insert(
                            0,
                            AdminShell.Key.CreateNew(
                                iddata.GetElementName(), true, iddata.identification.idType, iddata.identification.id));
                    break;
                }
                else
                if (ve.GetMainDataObject() is AdminShell.Referable rf)
                {
                    // add a key and go up ..
                    if (rf != null)
                        res.Insert(
                            0,
                            AdminShell.Key.CreateNew(rf.GetElementName(), true, "IdShort", rf.idShort));
                }
                else
                // uups!
                { }
                // need to go up
                ve = ve.Parent;
            }

            return res;
        }
    }

    public class VisualElementEnvironmentItem : VisualElementGeneric
    {
        public enum ItemType
        {
            Env = 0, Shells, Assets, ConceptDescriptions, Package, OrphanSubmodels, AllSubmodels, SupplFiles,
            EmptySet
        };
        public static string[] ItemTypeNames = new string[] {
            "Environment", "AdministrationShells", "Assets", "ConceptDescriptions", "Package", "Orphan Submodels",
            "All Submodels", "Supplementary files", "Empty" };

        public string thePackageSourceFn;
        public AdminShellPackageEnv thePackage = null;
        public AdminShell.AdministrationShellEnv theEnv = null;
        public ItemType theItemType = ItemType.Env;

        public VisualElementEnvironmentItem(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShellPackageEnv package,
            AdminShell.AdministrationShellEnv env, ItemType itemType,
            string packageSourceFn = null)
        : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.thePackage = package;
            this.theEnv = env;
            this.theItemType = itemType;
            this.thePackageSourceFn = packageSourceFn;

            this.Background = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkAccentColor"];
            this.Border = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagBg = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagFg = Brushes.White;

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

        public static object GiveDataObject(ItemType t)
        {
            return ItemTypeNames[(int)t];
        }

        public override object GetMainDataObject()
        {
            return GiveDataObject(theItemType);
        }

        public override void RefreshFromMainData()
        {
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

            this.Background = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkAccentColor"];
            this.Border = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagBg = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagFg = Brushes.White;

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
                var asset = theEnv.FindAsset(theAas.assetRef);
                if (asset != null)
                    this.Info += $" of [{asset.identification.idType}, {asset.identification.id}, {asset.kind.kind}]";
            }
        }
    }

    public class VisualElementAsset : VisualElementGeneric
    {
        public AdminShell.AdministrationShellEnv theEnv = null;
        public AdminShell.Asset theAsset = null;

        public VisualElementAsset(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShell.AdministrationShellEnv env,
            AdminShell.Asset asset)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.theEnv = env;
            this.theAsset = asset;

            this.Background = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkAccentColor"];
            this.Border = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagBg = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagFg = Brushes.White;

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
        public AdminShell.SubmodelRef theSubmodelRef = null;
        public AdminShell.Submodel theSubmodel = null;

        public VisualElementSubmodelRef(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShell.AdministrationShellEnv env,
            AdminShell.SubmodelRef smr, AdminShell.Submodel sm)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.theEnv = env;
            this.theSubmodelRef = smr;
            this.theSubmodel = sm;

            this.Background = (SolidColorBrush)System.Windows.Application.Current.Resources["LightAccentColor"];
            this.Border = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagBg = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagFg = Brushes.White;

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
                this.Info = "->" + this.theSubmodelRef.ToString();
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

            this.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#D0D0D0"));
            this.Border = (SolidColorBrush)(new BrushConverter().ConvertFrom("#606060"));
            this.TagBg = (SolidColorBrush)(new BrushConverter().ConvertFrom("#707070")); ;
            this.TagFg = Brushes.White;

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

    public class VisualElementView : VisualElementGeneric
    {
        public AdminShell.AdministrationShellEnv theEnv = null;
        public AdminShell.View theView = null;

        public VisualElementView(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShell.AdministrationShellEnv env,
            AdminShell.View vw)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.theEnv = env;
            this.theView = vw;

            this.Background = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkAccentColor"];
            this.Border = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagBg = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagFg = Brushes.White;

            this.TagString = "View";
            RefreshFromMainData();
            RestoreFromCache();
        }

        public override object GetMainDataObject()
        {
            return theView;
        }

        public override void RefreshFromMainData()
        {
            if (theView != null)
            {
                var ci = theView.ToCaptionInfo();
                this.Caption = "" + ci.Item1;
                this.Info = ci.Item2;
            }
        }

    }

    public class VisualElementReference : VisualElementGeneric
    {
        public AdminShell.AdministrationShellEnv theEnv = null;
        public AdminShell.Reference theReference = null;

        public VisualElementReference(
            VisualElementGeneric parent, TreeViewLineCache cache, AdminShell.AdministrationShellEnv env,
            AdminShell.Reference rf)
            : base()
        {
            this.Parent = parent;
            this.Cache = cache;
            this.theEnv = env;
            this.theReference = rf;

            this.Background = Brushes.White;
            this.Border = Brushes.White;
            this.TagBg = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagFg = Brushes.White;

            this.TagString = "\u2b95";
            RefreshFromMainData();
            RestoreFromCache();
        }

        public override object GetMainDataObject()
        {
            return theReference;
        }

        public override void RefreshFromMainData()
        {
            if (theReference != null && theReference.Keys != null)
            {
                this.Caption = "";
                this.Info = theReference.ListOfValues("/ ");
            }
        }

    }

    public class VisualElementSubmodelElement : VisualElementGeneric
    {
        public AdminShell.AdministrationShellEnv theEnv = null;
        public AdminShell.Referable theContainer = null;
        public AdminShell.SubmodelElementWrapper theWrapper = null;

        private AdminShell.ConceptDescription cachedCD = null;

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

            this.Background = Brushes.White;
            this.Border = Brushes.White;
            this.TagBg = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagFg = Brushes.White;

            this.TagString = wrap.GetElementAbbreviation();

            RefreshFromMainData();
            RestoreFromCache();
        }

        public override object GetMainDataObject()
        {
            return theWrapper.submodelElement;
        }

        public override void RefreshFromMainData()
        {
            if (theWrapper != null)
            {
                var sme = theWrapper.submodelElement;
                var ci = sme.ToCaptionInfo();
                this.Caption = ((sme.kind != null && sme.kind.IsTemplate) ? "<T> " : "") + ci.Item1;
                this.Info = ci.Item2;

                var showCDinfo = false;

                switch (sme)
                {
                    case AdminShell.Property smep:
                        if (smep.value != null && smep.value != "")
                            this.Info += "= " + smep.value;
                        else if (smep.valueId != null && !smep.valueId.IsEmpty)
                            this.Info += "<= " + smep.valueId.ToString();
                        showCDinfo = true;
                        break;

                    case AdminShell.Range rng:
                        var txtMin = rng.min == null ? "{}" : rng.min.ToString();
                        var txtMax = rng.max == null ? "{}" : rng.max.ToString();
                        this.Info += $"= {txtMin} .. {txtMax}";
                        showCDinfo = true;
                        break;

                    case AdminShell.MultiLanguageProperty mlp:
                        if (mlp.value != null)
                            this.Info += "-> " + mlp.value.GetDefaultStr();
                        break;

                    case AdminShell.File smef:
                        if (smef.value != null && smef.value != "")
                            this.Info += "-> " + smef.value;
                        break;

                    case AdminShell.ReferenceElement smere:
                        if (smere.value != null && !smere.value.IsEmpty)
                            this.Info += "~> " + smere.value.ToString();
                        break;

                    case AdminShell.SubmodelElementCollection smc:
                        if (smc.value != null)
                            this.Info += "(" + smc.value.Count + " elements)";
                        break;
                }

                // Show CD / unikts ..
                if (showCDinfo)
                {
                    // cache ConceptDescription?
                    if (sme.semanticId != null && sme.semanticId.Keys != null)
                    {
                        if (this.cachedCD == null)
                            this.cachedCD = this.theEnv.FindConceptDescription(sme.semanticId.Keys);
                        var iecprop = this.cachedCD?.GetIEC61360();
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

            this.Background = Brushes.White;
            this.Border = Brushes.White;
            this.TagBg = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagFg = Brushes.White;

            this.TagString = "In";
            if (this.theDir == AdminShell.OperationVariable.Direction.Out)
                this.TagString = "Out";
            if (this.theDir == AdminShell.OperationVariable.Direction.InOut)
                this.TagString = "InOut";

            this.TagBg = (SolidColorBrush)(new BrushConverter().ConvertFrom("#707070")); ;
            this.TagFg = Brushes.White;
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
                    var ci2 = theOpVar.value.submodelElement.ToCaptionInfo();
                    this.Caption = "" + ci2.Item1;
                    this.Info = ci2.Item2;
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

            this.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#D0D0D0"));
            this.Border = (SolidColorBrush)(new BrushConverter().ConvertFrom("#606060"));
            this.TagBg = (SolidColorBrush)(new BrushConverter().ConvertFrom("#707070")); ;
            this.TagFg = Brushes.White;

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

            this.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#D0D0D0"));
            this.Border = (SolidColorBrush)(new BrushConverter().ConvertFrom("#606060"));
            this.TagBg = (SolidColorBrush)(new BrushConverter().ConvertFrom("#707070")); ;
            this.TagFg = Brushes.White;

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

            this.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#A0A0A0"));
            this.Border = (SolidColorBrush)(new BrushConverter().ConvertFrom("#707070"));
            this.TagBg = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            this.TagFg = Brushes.White;

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

    public static class Generators
    {
        private static void GenerateVisualElementsFromShellEnvAddElements(
            TreeViewLineCache cache, AdminShell.AdministrationShellEnv env, VisualElementGeneric parent,
            AdminShell.Referable parentContainer, AdminShell.SubmodelElementWrapper el)
        {
            var ti = new VisualElementSubmodelElement(parent, cache, env, parentContainer, el);
            parent.Members.Add(ti);

            // Recurse: SMC
            if (el.submodelElement is AdminShell.SubmodelElementCollection elc && elc.value != null)
                foreach (var elcc in elc.value)
                    GenerateVisualElementsFromShellEnvAddElements(cache, env, ti, elc, elcc);

            // Recurse: Entity
            // ReSharper disable ExpressionIsAlwaysNull
            if (el.submodelElement is AdminShell.Entity ele && ele.statements != null)
                foreach (var eles in ele.statements)
                    GenerateVisualElementsFromShellEnvAddElements(cache, env, ti, ele, eles);
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
                    GenerateVisualElementsFromShellEnvAddElements(cache, env, ti, ela, elaa);
        }

        public static List<VisualElementGeneric> GenerateVisualElementsFromShellEnv(
            TreeViewLineCache cache, AdminShell.AdministrationShellEnv env, AdminShellPackageEnv package = null,
            string packageSourceFn = null,
            bool editMode = false, int expandMode = 0)
        {
            // clear tree
            var res = new List<VisualElementGeneric>();
            // valid?
            if (env == null)
                return res;

            // need some attach points
            VisualElementEnvironmentItem tiPackage = null, tiEnv = null, tiShells = null, tiAssets = null, tiCDs = null;

            // tracking references of Submodels
            var referencedSubmodels = new List<AdminShell.Submodel>();

            // interested plug-ins
            var pluginsToCheck = new List<Plugins.PluginInstance>();
            if (Plugins.LoadedPlugins != null)
                foreach (var lpi in Plugins.LoadedPlugins.Values)
                {
                    try
                    {
                        var x =
                            lpi.InvokeAction(
                                "get-check-visual-extension") as AasxIntegrationBase.AasxPluginResultBaseObject;
                        if (x != null && (bool)x.obj)
                            pluginsToCheck.Add(lpi);
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }
                }

            // many operations -> make it bulletproof
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

                    // concept descriptions
                    tiCDs = new VisualElementEnvironmentItem(
                        tiEnv, cache, package, env, VisualElementEnvironmentItem.ItemType.ConceptDescriptions);
                    tiCDs.SetIsExpandedIfNotTouched(expandMode > 0);
                    tiEnv.Members.Add(tiCDs);
                }

                // over all Admin shells
                foreach (var aas in env.AdministrationShells)
                {
                    // item
                    var tiAas = new VisualElementAdminShell(null, cache, package, env, aas);
                    tiAas.SetIsExpandedIfNotTouched(expandMode > 0);

                    // add item
                    if (editMode)
                    {
                        tiAas.Parent = tiShells;
                        tiShells.Members.Add(tiAas);
                    }
                    else
                    {
                        res.Add(tiAas);
                    }

                    // have submodels?
                    if (aas.submodelRefs != null)
                        foreach (var smr in aas.submodelRefs)
                        {
                            var sm = env.FindSubmodel(smr);
                            if (sm == null)
                                AasxPackageExplorer.Log.Singleton.Error("Cannot find some submodel!");
                            else
                                referencedSubmodels.Add(sm);

                            // item (even if sm is null)
                            var tiSm = new VisualElementSubmodelRef(tiAas, cache, env, smr, sm);
                            tiSm.SetIsExpandedIfNotTouched(expandMode > 1);

                            // check for visual extensions
                            foreach (var lpi in pluginsToCheck)
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
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                                }
                            }

                            // recursively into the submodel elements
                            if (sm != null)
                                if (sm.submodelElements != null)
                                    foreach (var sme in sm.submodelElements)
                                        GenerateVisualElementsFromShellEnvAddElements(cache, env, tiSm, sm, sme);

                            // add
                            tiAas.Members.Add(tiSm);
                        }

                    // have views?
                    if (aas.views != null && aas.views.views != null)
                        foreach (var vw in aas.views.views)
                        {
                            // item
                            var tiVw = new VisualElementView(tiAas, cache, env, vw);
                            tiVw.SetIsExpandedIfNotTouched(expandMode > 1);
                            // recursion -> submodel elements
                            if (vw.containedElements != null && vw.containedElements.reference != null)
                                foreach (var ce in vw.containedElements.reference)
                                {
                                    var tiRf = new VisualElementReference(tiVw, cache, env, ce);
                                    tiVw.Members.Add(tiRf);
                                }
                            // add
                            tiAas.Members.Add(tiVw);
                        }
                }

                // if edit mode, then display further ..
                if (editMode)
                {
                    // over all assets
                    foreach (var asset in env.Assets)
                    {
                        // item
                        var tiAsset = new VisualElementAsset(tiAssets, cache, env, asset);
                        tiAssets.Members.Add(tiAsset);
                    }

                    // over all concept descriptions
                    foreach (var cd in env.ConceptDescriptions)
                    {
                        // item
                        var tiCD = new VisualElementConceptDescription(tiCDs, cache, env, cd);
                        tiCDs.Members.Add(tiCD);
                    }

                    // alternative code deleted
                    {
                        // head
                        var tiAllSubmodels = new VisualElementEnvironmentItem(
                            tiEnv, cache, package, env, VisualElementEnvironmentItem.ItemType.AllSubmodels);
                        tiAllSubmodels.SetIsExpandedIfNotTouched(expandMode > 0);
                        tiEnv.Members.Add(tiAllSubmodels);

                        // show all Submodels
                        foreach (var sm in env.Submodels)
                        {
                            var tiSm = new VisualElementSubmodel(tiAllSubmodels, cache, env, sm);
                            tiSm.SetIsExpandedIfNotTouched(expandMode > 1);
                            tiAllSubmodels.Members.Add(tiSm);
                        }
                    }
                }

                // package as well?
                if (editMode && package != null && tiPackage != null)
                {
                    // file folder
                    var tiFiles = new VisualElementEnvironmentItem(
                        tiPackage, cache, package, env, VisualElementEnvironmentItem.ItemType.SupplFiles);
                    tiFiles.SetIsExpandedIfNotTouched(expandMode > 0);
                    tiPackage.Members.Add(tiFiles);

                    // single files
                    var files = package.GetListOfSupplementaryFiles();
                    foreach (var fi in files)
                        tiFiles.Members.Add(new VisualElementSupplementalFile(tiFiles, cache, package, fi));
                }

            }
            catch (Exception ex)
            {
                AasxPackageExplorer.Log.Singleton.Error(ex, "Generating tree of visual elements");
            }

            // end
            return res;

        }

    }

}
