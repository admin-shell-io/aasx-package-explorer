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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using AasxIntegrationBase;
using AasxWpfControlLibrary.PackageCentral;
using AdminShellNS;
using Newtonsoft.Json;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace AasxWpfControlLibrary.AasxFileRepo
{
    /// <summary>
    /// This interface allows to find some <c>AasxFileRepository.FileItem</c> by asking for AAS or AssetId.
    /// It does not intend to be a full fledged query interface, but allow to retrieve what is usful for
    /// automatic Reference link following etc.
    /// </summary>
    public interface IRepoFind
    {
        AasxFileRepository.FileItem FindByAssetId(string aid);
        AasxFileRepository.FileItem FindByAasId(string aid);
        IEnumerable<AasxFileRepository.FileItem> EnumerateItems();
        bool Contains(AasxFileRepository.FileItem fi);
    }

    /// <summary>
    /// This simple file repository holds associations between AssetId and Filenames of AASX packages.
    /// Additionally, it has some view model capabilities in order to animate some visual indications
    /// </summary>
    public class AasxFileRepository : IRepoFind
    {
        public class FileItem : INotifyPropertyChanged
        {
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

            /// <summary>
            /// Asset Id of the respective Adminstration Shell
            /// </summary>
            /// 
            [JsonProperty(PropertyName = "assetId")]
            private string assetId = "";

            [JsonIgnore]
            public string AssetId
            {
                get { return assetId; }
                set { assetId = value; OnPropertyChanged("InfoIds"); }
            }

            /// <summary>
            /// AAS Id, which is associated to th asset id.
            /// </summary>
            [JsonProperty(PropertyName = "aasId")]
            private string aasId = "";

            [JsonIgnore]
            public string AasId
            {
                get { return aasId; }
                set { aasId = value; OnPropertyChanged("InfoIds"); }
            }

            /// <summary>
            /// Description; help for the human user.
            /// </summary>
            [JsonProperty(PropertyName = "description")]
            private string description = "";

            [JsonIgnore]
            public string Description
            {
                get { return description; }
                set { description = value; OnPropertyChanged("InfoIds"); }
            }

            /// <summary>
            /// 3-5 letters of Tag to be displayed in user interface
            /// </summary>
            [JsonProperty(PropertyName = "tag")]
            private string tag = "";

            [JsonIgnore]
            public string Tag
            {
                get { return tag; }
                set { tag = value; OnPropertyChanged("InfoIds"); }
            }

            /// <summary>
            /// QR or DMC format of the assit id
            /// </summary>
            [JsonProperty(PropertyName = "code")]
            public string CodeType2D = "";

            /// <summary>
            /// AASX file name to load
            /// </summary>
            [JsonProperty(PropertyName = "fn")]
            private string filename = "";

            [JsonIgnore]
            public string Filename
            {
                get { return filename; }
                set { filename = value; OnPropertyChanged("InfoFilename"); }
            }

            //
            // Container options
            //

            /// <summary>
            /// Options for the package. Could be <c>null</c>!
            /// </summary>
            public PackageContainerOptionsBase Options;

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
                    var info = "";

                    if (this.Description.HasContent())
                        info = info.AddWithDelimiter("" + this.Description + "", delimter: Environment.NewLine);
                    if (this.AssetId.HasContent())
                        info = info.AddWithDelimiter("" + this.AssetId + " (Asset)", delimter: Environment.NewLine);
                    if (this.AasId.HasContent())
                        info = info.AddWithDelimiter("" + this.AasId + " (AAS)", delimter: Environment.NewLine);

                    return info;
                }
            }

            [JsonIgnore]
            public string InfoFilename
            {
                get { return "" + this.Filename; }
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
            public Brush VisualLabelBackground
            {
                get
                {
                    if (VisualState == VisualStateEnum.Idle)
                        return Brushes.Transparent;

                    var col = Colors.Green;
                    if (VisualState == VisualStateEnum.ReadFrom)
                        col = Colors.Blue;
                    if (VisualState == VisualStateEnum.WriteTo)
                        col = Colors.Orange;

                    if (visualTime > 2.0)
                        col.ScA = 1.0f;
                    else if (visualTime > 0.001)
                        col.ScA = (float)(visualTime / 2.0);
                    else
                    {
                        visualTime = 0.0;
                        col = Colors.Transparent;
                        VisualState = VisualStateEnum.Idle;
                    }

                    return new SolidColorBrush(col);
                }
            }

            // Constructor

            public FileItem() { }

            public FileItem(string assetId, string fn, string aasId = null, string description = "",
                string tag = "", string code = "")
            {
                this.AssetId = assetId;
                this.Filename = fn;
                this.AasId = aasId;
                this.Description = description;
                this.Tag = tag;
                this.CodeType2D = code;
            }
        }

        public string Header;

        [JsonProperty(PropertyName = "filemaps")]
        public ObservableCollection<FileItem> FileMap = new ObservableCollection<FileItem>();

        [JsonIgnore]
        public string Filename = null;

        [JsonIgnore]
        public double DefaultAnimationTime = 2.0d;

        public void Add(FileItem fi)
        {
            this.FileMap?.Add(fi);
        }

        public void Remove(FileItem fi)
        {
            if (fi == null || this.FileMap == null)
                return;
            if (!this.FileMap.Contains(fi))
                return;
            this.FileMap.Remove(fi);
        }

        public void MoveUp(FileItem fi)
        {
            this.MoveElementInListUpwards<FileItem>(this.FileMap, fi);
        }

        public void MoveDown(FileItem fi)
        {
            this.MoveElementInListDownwards<FileItem>(this.FileMap, fi);
        }

        //
        // IFindRepo interface
        //

        public FileItem FindByAssetId(string aid)
        {
            return this.FileMap?.FirstOrDefault((fi) =>
            {
                return fi.AssetId.Trim() == aid.Trim();
            });
        }

        public FileItem FindByAasId(string aid)
        {
            return this.FileMap?.FirstOrDefault((fi) =>
            {
                return fi.AasId.Trim() == aid.Trim();
            });
        }

        public IEnumerable<FileItem> EnumerateItems()
        {
            if (this.FileMap != null)
                foreach (var fi in this.FileMap)
                    yield return fi;
        }

        public bool Contains(AasxFileRepository.FileItem fi)
        {
            return true == this.FileMap?.Contains(fi);
        }

        //
        // more
        //

        public void DecreaseVisualTimeBy(double amount)
        {
            if (this.FileMap != null)
                foreach (var fm in this.FileMap)
                    fm.VisualTime = Math.Max(0.0, fm.VisualTime - amount);
        }

        public void StartAnimation(FileItem fi, FileItem.VisualStateEnum state)
        {
            // access
            if (fi == null || this.FileMap == null || !this.FileMap.Contains(fi))
                return;

            // stop?
            if (state == FileItem.VisualStateEnum.Idle)
            {
                fi.VisualState = FileItem.VisualStateEnum.Idle;
                fi.VisualTime = 0.0d;
                return;
            }

            // start
            fi.VisualState = state;
            fi.VisualTime = this.DefaultAnimationTime;
        }

        // file oerations

        public void SaveAs(string fn)
        {
            using (var s = new StreamWriter(fn))
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                s.WriteLine(json);
            }

            // record
            this.Filename = fn;
        }

        public string GetFullFilename(FileItem fi)
        {
            // access
            if (fi?.Filename == null)
                return null;

            // relative to this?
            var fn = fi.Filename;
            try
            {
                bool doFull = true;

                if (Path.IsPathRooted(fn))
                    doFull = false;

                if (fn.Contains("://")) // contains scheme
                    doFull = false;

                if (doFull && this.Filename != null)
                    fn = Path.Combine(Path.GetDirectoryName(this.Filename), fn);
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                return null;
            }

            // result 
            return fn;
        }

        public void MakeFilenamesRelative()
        {
            // access
            if (this.FileMap == null || this.Filename == null)
                return;

            // base path
            var basePath = Path.GetDirectoryName(Path.GetFullPath(this.Filename));
            if (basePath == null)
                return;
            if (!basePath.EndsWith("\\"))
                basePath += "\\";

            // each file
            foreach (var fi in this.FileMap)
                try
                {
                    // make 2 kinds of URIs
                    var baseUri = new Uri(basePath);
                    var fileUri = new Uri(Path.GetFullPath(fi.Filename));

                    var relUri = baseUri.MakeRelativeUri(fileUri);
                    var relPath = relUri.ToString().Replace("/", "\\");

                    fi.Filename = relPath;
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
        }

        public void AddByAas(AdminShell.AdministrationShellEnv env, AdminShell.AdministrationShell aas, string fn)
        {
            // access
            if (env == null || aas?.identification == null)
                return;
            var aasId = "" + aas.identification.id;

            // demand also asset
            var asset = env.FindAsset(aas.assetRef);
            if (asset?.identification == null)
                return;
            var assetId = "" + asset.identification.id;

            // try determine tag
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            var tag = "";
            try
            {
                var threeFn = Path.GetFileNameWithoutExtension(fn);
                tag = AdminShellUtil.ExtractPascalCasingLetters(asset.idShort);
                if (tag == null || tag.Length < 2)
                    tag = AdminShellUtil.ExtractPascalCasingLetters(threeFn).SubstringMax(0, 3);
                if (tag == null || tag.Length < 2)
                    tag = ("" + asset.idShort).SubstringMax(0, 3).ToUpper();
                if (tag == null || tag.Length < 3)
                    tag = ("" + threeFn).SubstringMax(0, 3).ToUpper();
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }
            // ReSharper enable ConditionIsAlwaysTrueOrFalse

            // build description
            var desc = "";
            if (aas.idShort.HasContent())
                desc += $"\"{aas.idShort}\"";
            if (asset.idShort.HasContent())
            {
                if (desc.HasContent())
                    desc += ",";
                desc += $"\"{asset.idShort}\"";
            }

            // ok, add
            var fi = new FileItem(
                assetId: assetId, aasId: aasId, fn: fn, tag: "" + tag, description: desc);
            fi.VisualState = FileItem.VisualStateEnum.ReadFrom;
            fi.VisualTime = 2.0;
            this.Add(fi);
        }

        public void AddByAasxFn(string fn)
        {
            try
            {
                // load
                var pkg = new AdminShellPackageEnv(fn);

                // for each Admin Shell and then each Asset
                if (pkg.AasEnv?.AdministrationShells?.Count > 0)
                    foreach (var aas in pkg.AasEnv.AdministrationShells)
                    {
                        this.AddByAas(pkg.AasEnv, aas, fn);
                    }

                // close directly!
                pkg.Close();
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }
        }

        // Converter

        public AdminShellPackageEnv MakeUpFakePackage()
        {
            // create fake
            var pkg = new AdminShellPackageEnv();

            // all files
            int i = 0;
            foreach (var fi in this.FileMap)
            {
                // sure?
                if (fi == null)
                    continue;
                i++;

                // aas
                var aas = new AdminShell.AdministrationShell(String.Format("AAS{0:00}_{1}", i, fi.Tag));
                aas.AddDescription("en?", "" + fi.Description);
                aas.identification = new AdminShell.Identification(
                    AdminShell.Identification.IRI, "" + fi.AasId);

                // asset
                var asset = new AdminShell.Asset(String.Format("Asset{0:00}_{1}", i, fi.Tag));
                asset.AddDescription("en?", "" + fi.Description);
                asset.identification = new AdminShell.Identification(
                    AdminShell.Identification.IRI, "" + fi.AssetId);
                aas.assetRef = asset.GetAssetReference();

                // add
                pkg.AasEnv?.AdministrationShells.Add(aas);
                pkg.AasEnv?.Assets.Add(asset);
            }

            //ok
            return pkg;
        }

        // Generators

        public static AasxFileRepository CreateDemoData()
        {
            var tr = new AasxFileRepository();

            tr.Add(new AasxFileRepository.FileItem("http://pk.festo.com/111111111111", "1.aasx"));
            tr.Add(new AasxFileRepository.FileItem("http://pk.festo.com/222222222222", "2.aasx"));
            tr.Add(new AasxFileRepository.FileItem("http://pk.festo.com/333333333333", "3.aasx"));

            tr.FileMap[0].Description = "Additional info";
            tr.FileMap[0].AasId = "http://smart.festo.com/cdscsdbdsbchjdsbjhcbdshjcbhjdsbchjsdbhjcsdbhjcdsbhjcsbdhj";
            tr.FileMap[0].VisualState = AasxFileRepository.FileItem.VisualStateEnum.Activated;
            tr.FileMap[0].VisualTime = 6.0;

            tr.FileMap[1].Description = "Additional info";
            tr.FileMap[1].VisualState = AasxFileRepository.FileItem.VisualStateEnum.ReadFrom;
            tr.FileMap[1].VisualTime = 3.0;

            tr.FileMap[2].VisualState = AasxFileRepository.FileItem.VisualStateEnum.WriteTo;
            tr.FileMap[2].VisualTime = 4.5;

            return tr;
        }

        public static AasxFileRepository Load(string fn)
        {
            // from file
            if (!File.Exists(fn))
                return null;
            var init = File.ReadAllText(fn);
            var repo = JsonConvert.DeserializeObject<AasxFileRepository>(init);

            // record
            repo.Filename = fn;

            // return
            return repo;
        }

        public static bool GenerateRepositoryFromFileNames(string[] inputFns, string outputFn)
        {
            var res = true;

            // new repo
            var repo = new AasxFileRepository();

            // make records
            foreach (var ifn in inputFns)
            {
                // get one or multiple asset ids
                var assetIds = new List<string>();
                try
                {
                    var pkg = new AdminShellPackageEnv();
                    pkg.Load(ifn);
                    if (pkg.AasEnv != null && pkg.AasEnv.Assets != null)
                        foreach (var asset in pkg.AasEnv.Assets)
                            if (asset.identification != null)
                                assetIds.Add(asset.identification.id);
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    res = false;
                }

                // make the record(s)
                foreach (var assetid in assetIds)
                {
                    var fmi = new AasxFileRepository.FileItem();
                    fmi.Filename = ifn;
                    fmi.CodeType2D = "DMC";
                    fmi.AssetId = assetid;
                    fmi.Description = "TODO";
                    fmi.Tag = "TODO";

                    // add it
                    repo.FileMap.Add(fmi);
                }
            }

            // save
            using (var s = new StreamWriter(outputFn))
            {
                var json = JsonConvert.SerializeObject(repo, Formatting.Indented);
                s.WriteLine(json);
            }

            return res;
        }

        //
        // Internal
        //

        // List manipulations
        // TODO (MIHO, 2020-08-05): refacture this with DispEditHelper.cs

        private void MoveElementInListUpwards<T>(IList<T> list, T entity)
        {
            if (list == null || list.Count < 2 || entity == null)
                return;
            int ndx = list.IndexOf(entity);
            if (ndx < 1)
                return;
            list.RemoveAt(ndx);
            list.Insert(Math.Max(ndx - 1, 0), entity);
        }

        private void MoveElementInListDownwards<T>(IList<T> list, T entity)
        {
            if (list == null || list.Count < 2 || entity == null)
                return;
            int ndx = list.IndexOf(entity);
            if (ndx < 0 || ndx >= list.Count - 1)
                return;
            list.RemoveAt(ndx);
            list.Insert(Math.Min(ndx + 1, list.Count), entity);
        }
    }
}
