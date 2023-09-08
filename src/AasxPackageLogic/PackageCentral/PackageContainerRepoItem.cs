/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// This (rather base) container class integrates the former <c>AasxFileRepoItem</c> class, which implemented
    /// back in JAN 2021 the (updated) AASX file repository (a serialized list of file informations, which was 
    /// extended to a set of repo lists clickable by the user).
    /// In this version, this class takes over (on source code level) the members of <c>AasxFileRepoItem</c>
    /// and associated code and makes the legacy code therefore OBSOLETE (and to be removed).
    /// The idea is, to inject the visual properties and the ability of serialization of information to 
    /// JSON files directly into the single container class and therefore unify the capabilities of these
    /// two class systems.
    /// Note: As the would mean, that deriatives from this class will be serialized by JSON, for all
    ///       runtime properties a [JsonIgnore] attribute is required in ALL DERIVED CLASSES.
    /// </summary>
    public class PackageContainerRepoItem : PackageContainerBase, INotifyPropertyChanged
    {
        //
        // Members
        //

        // duty from INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        // Visual state
        public enum VisualStateEnum { Idle, Activated, ReadFrom, WriteTo }

        // static members, to be persisted

        //
        // Note 1: this is the new version of the Repository from 8 JAN 2021.
        //         Target is to switch to maintaining LISTS of assetId, aasId, submodelId
        //
        // Note 2: the single id properties are supported for READing old JSONs.
        //

        /// <summary>
        /// AssetInformation Ids of the respective AASX Package.
        /// Note: to make this easy, only the value-strings of the Ids are maintained. A 2nd check needs
        /// to ensure full AAS List<Key> compatibility.
        /// </summary>
        /// 
        [JsonProperty(PropertyName = "AssetIds")]
        private List<string> _assetIds = new List<string>();

        [JsonIgnore]
        public List<string> AssetIds
        {
            get { return _assetIds; }
            set { _assetIds = value; OnPropertyChanged("InfoIds"); }
        }

        // for compatibility before JAN 2021
        [JsonProperty(PropertyName = "assetId")]
        private string _legacyAssetId { set { _assetIds.Add(value); OnPropertyChanged("InfoIds"); } }


        /// <summary>
        /// AAS Ids of the respective AASX Package.
        /// Note: to make this easy, only the value-strings of the Ids are maintained. A 2nd check needs
        /// to ensure full AAS List<Key> compatibility.
        /// </summary>
        [JsonProperty(PropertyName = "AasIds")]
        private List<string> _aasIds = new List<string>();

        [JsonIgnore]
        public List<string> AasIds
        {
            get { return _aasIds; }
            set { _aasIds = value; OnPropertyChanged("InfoIds"); }
        }

        /// <summary>
        /// Submodel Ids of the respective AASX Package.
        /// Note: to make this easy, only the value-strings of the Ids are maintained. A 2nd check needs
        /// to ensure full AAS List<Key> compatibility.
        /// </summary>
        [JsonProperty(PropertyName = "SubmodelIds")]
        private List<string> _submodelIds = new List<string>();

        [JsonIgnore]
        public List<string> SubmodelIds
        {
            get { return _submodelIds; }
            set { _submodelIds = value; OnPropertyChanged("InfoIds"); }
        }

        // for compatibility before JAN 2021
        [JsonProperty(PropertyName = "aasId")]
        private string _legacyAaasId { set { _aasIds.Add(value); OnPropertyChanged("InfoIds"); } }

        // TODO (MIHO, 2021-01-08): add SubmodelIds

        /// <summary>
        /// Description; help for the human user.
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        private string _description = "";

        [JsonIgnore]
        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged("InfoIds"); }
        }

        /// <summary>
        /// 3-5 letters of Tag to be displayed in user interface
        /// </summary>
        [JsonProperty(PropertyName = "tag")]
        private string _tag = "";

        [JsonIgnore]
        public string Tag
        {
            get { return _tag; }
            set { _tag = value; OnPropertyChanged("InfoIds"); }
        }

        /// <summary>
        /// QR or DMC format of the assit id
        /// </summary>
        [JsonProperty(PropertyName = "code")]
        public string CodeType2D = "";

        /// <summary>
        /// Location of the Container in a certain storage container, e.g. a local or network based
        /// repository. In this base implementation, it maps to a empty string.
        /// </summary>
        public override string Location
        {
            get { return _location; }
            set { _location = value; OnPropertyChanged("InfoLocation"); }
        }

        // for compatibility before JAN 2021
        [JsonProperty(PropertyName = "fn")]
        private string _legacyFilename { set { Location = value; OnPropertyChanged("InfoLocation"); } }

        //
        // dynamic members, to be not persisted            
        //

        /// <summary>
        /// Visual animation, currently displayed
        /// </summary>
        [JsonIgnore]
        public VisualStateEnum VisualState = VisualStateEnum.Idle;

        /// <summary>
        /// Tine in seconds, how long an animation shall be displayed.
        /// </summary>
        [JsonIgnore]
        private double visualTime = 0.0;

        [JsonIgnore]
        public double VisualTime
        {
            get { return visualTime; }
            set
            {
                this.visualTime = value;
                OnPropertyChanged("VisualLabelText");
                OnPropertyChanged("VisualLabelBackground");
            }
        }

        // Getters used by the binding
        [JsonIgnore]
        public string InfoIds
        {
            get
            {
                // Resharper disable AccessToModifiedClosure
                var info = "";
                Action<string, List<string>> lambdaAddIdList = (head, ids) =>
                {
                    if (ids != null && ids.Count > 0)
                    {
                        if (info != "")
                            info += System.Environment.NewLine;
                        info += head;
                        foreach (var id in ids)
                            info += "" + id + ",";
                        info = info.TrimEnd(',');
                    }
                };

                if (Description.HasContent())
                    info += Description;

                lambdaAddIdList("Assets: ", _assetIds);
                lambdaAddIdList("AAS: ", _aasIds);

                return info;
                // Resharper enable AccessToModifiedClosure
            }
        }

        [JsonIgnore]
        public string InfoLocation
        {
            get { return "" + this.Location; }
        }

        [JsonIgnore]
        public string VisualLabelText
        {
            get
            {
                switch (VisualState)
                {
                    case VisualStateEnum.Activated:
                        return "A";
                    case VisualStateEnum.ReadFrom:
                        return "R";
                    case VisualStateEnum.WriteTo:
                        return "W";
                    default:
                        return "";
                }
            }
        }

        [JsonIgnore]
        public AnyUiBrush VisualLabelBackground
        {
            get
            {
                if (VisualState == VisualStateEnum.Idle)
                    return AnyUiBrushes.Transparent;

                var col = AnyUiColors.Green;
                if (VisualState == VisualStateEnum.ReadFrom)
                    col = AnyUiColors.Blue;
                if (VisualState == VisualStateEnum.WriteTo)
                    col = AnyUiColors.Orange;

                if (visualTime > 2.0)
                    col.ScA = 1.0f;
                else if (visualTime > 0.001)
                    col.ScA = (float)(visualTime / 2.0);
                else
                {
                    visualTime = 0.0;
                    col = AnyUiColors.Transparent;
                    VisualState = VisualStateEnum.Idle;
                }

                return new AnyUiBrush(col);
            }
        }

        /// <summary>
        /// True, if the actual editor window is editing exactly this one repo item.
        /// </summary>
        [JsonIgnore]
        public bool IsEdited
        {
            get { return _isEdited; }
            set { _isEdited = value; OnPropertyChanged("VisualIsEdited"); }
        }
        private bool _isEdited = false;

        [JsonIgnore]
        public AnyUiVisibility VisualIsEdited
        {
            get
            {
                return _isEdited ? AnyUiVisibility.Visible : AnyUiVisibility.Hidden;
            }
        }

        /// <summary>
        /// True, if <c>Env</c> is loaded with contents, e.g. due to "LoadResident".
        /// State of this flag is required to be maintained by the class logic.
        /// </summary>
        [JsonIgnore]
        protected bool IsLoaded
        {
            get { return _isLoaded; }
            set { _isLoaded = value; OnPropertyChanged("VisualIsLoaded"); }
        }

        // Resharper disable ValueParameterNotUsed
        private bool _isLoaded
        {
            get
            {
                return true == Env?.IsOpen;
            }
            set
            {
            }
        }
        // Resharper enable ValueParameterNotUsed

        [JsonIgnore]
        public AnyUiVisibility VisualIsLoaded
        {
            get
            {
                return _isLoaded ? AnyUiVisibility.Visible : AnyUiVisibility.Hidden;
            }
        }

        //This is for Asp.NetCore Rest APIs
        public string PackageId { get; internal set; }

        public override void Close()
        {
            base.Close();
            OnPropertyChanged("VisualIsLoaded");
        }

        //
        // Constructors
        //

        public PackageContainerRepoItem() : base() { }

        public PackageContainerRepoItem(PackageCentral packageCentral) : base(packageCentral) { }

        public PackageContainerRepoItem(string assetId, string fn, string aasId = null, string description = "",
            string tag = "", string code = "", PackageCentral packageCentral = null)
            : base(packageCentral)
        {
            _location = fn;
            if (assetId != null)
                this.AssetIds.Add(assetId);
            if (aasId != null)
                this.AasIds.Add(aasId);
            _description = description;
            _tag = tag;
            this.CodeType2D = code;
        }

        public PackageContainerRepoItem(CopyMode mode, PackageContainerBase other,
            PackageCentral packageCentral = null)
            : base(mode, other, packageCentral)
        {
            if ((mode & CopyMode.Serialized) > 0 && other is PackageContainerRepoItem o)
            {
                _assetIds = new List<string>(o.AssetIds);
                _aasIds = new List<string>(o.AasIds);
                _location = "" + o.Location;
                _description = "" + o.Description;
                _tag = "" + o.Tag;
                this.CodeType2D = "" + o.CodeType2D;
            }
        }

        //
        // enumerations as important interface to the outside
        //

        public IEnumerable<string> EnumerateAssetIds()
        {
            if (_assetIds != null)
                foreach (var id in _assetIds)
                    yield return id;
        }

        public IEnumerable<string> EnumerateAasIds()
        {
            if (_aasIds != null)
                foreach (var id in _aasIds)
                    yield return id;
        }

        public IEnumerable<string> EnumerateAllIds()
        {
            foreach (var id in EnumerateAssetIds())
                yield return id;
            foreach (var id in EnumerateAasIds())
                yield return id;
        }

        //
        // Repopoulate
        //

        /// <summary>
        /// Just clear all AssetIds, AasIds, SubmodelIds
        /// </summary>
        public void CleanIds()
        {
            _aasIds = new List<string>();
            _assetIds = new List<string>();
            _submodelIds = new List<string>();
        }

        /// <summary>
        /// This function accesses the AAS, AssetInformation and Aas.Submodel information of the environment and
        /// re-calculates the particulare lists of ids. If the tag and/ or description is empty, 
        /// it will also build a generated tag or descriptions
        /// </summary>
        public void CalculateIdsTagAndDesc(bool force = false)
        {
            // Ids

            CleanIds();

            Env?.AasEnv?.AssetAdministrationShells?.ForEach((x) =>
            {
                if (true == x?.Id.HasContent())
                    _aasIds.Add(x.Id);
            });

            Env?.AasEnv?.Submodels?.ForEach((x) =>
            {
                if (true == x?.Id.HasContent())
                    _submodelIds.Add(x.Id);
            });

            // get some descriptiive data
            var threeFn = Path.GetFileNameWithoutExtension(Location);
            var aas0 = Env?.AasEnv?.AssetAdministrationShells?.FirstOrDefault();

            // Tag
            if (!Tag.HasContent() || force)
            {
                // ReSharper disable ConditionIsAlwaysTrueOrFalse
                var tag = "";
                try
                {
                    tag = "";
                    if (tag == null || tag.Length < 2)
                        tag = AdminShellUtil.ExtractPascalCasingLetters(threeFn).SubstringMax(0, 3);
                    if ((tag == null || tag.Length < 2) && aas0 != null)
                        tag = ("" + aas0.IdShort).SubstringMax(0, 3).ToUpper();
                    if (tag == null || tag.Length < 3)
                        tag = ("" + threeFn).SubstringMax(0, 3).ToUpper();
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
                Tag = tag;
                // ReSharper enable ConditionIsAlwaysTrueOrFalse
            }

            // Description
            if (!Description.HasContent() || force)
            {
                var desc = "";
                if (aas0?.IdShort.HasContent() == true)
                    desc += $"{aas0.IdShort}";
                Description = desc;
            }
        }

    }
}
