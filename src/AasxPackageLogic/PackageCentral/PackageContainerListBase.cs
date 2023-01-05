/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0_RC02;
using AasxIntegrationBase;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// This class takes over source code from the legacy <c>AasxFileRepoBase</c>.
    /// It therefore realizes a list of items, which associated some ids (AasId, AssetId, SubmodelIds) with
    /// AASX Package files.
    /// In DEC 2020, the PackageComtainers were introduced to leverage the hosting of AASX Package files and
    /// to enhance and abstract them by providing online connections and event management.
    /// In JAN 2021, the idea is now to join these capabilities. 
    /// This class implements a list of <c>PackageContainerRepoItem</c>, so it can provide information, which
    /// AASX Package file is to be loaded and hosted and functinality, HOW this can be done.
    /// This class is intended to be a base class, so classes for local repos, AAS repos, AAS registries are
    /// deriving from it.
    /// </summary>
    public class PackageContainerListBase : IPackageContainerFind
    {
        //
        // Members
        //

        /// <summary>
        /// Header, which is shown to the user and SERIALIZED to the JSON file repo
        /// </summary>
        public string Header;

        /// <summary>
        /// List of <c>PackageContainerRepoItem</c>, which holds the actual information on the individual
        /// AASX package files.
        /// </summary>
        [JsonProperty(PropertyName = "filemaps")]
        public ObservableCollection<PackageContainerRepoItem> FileMap =
            new ObservableCollection<PackageContainerRepoItem>();

        /// <summary>
        /// Length of the fading effect of animations in [sec]
        /// </summary>
        [JsonIgnore]
        public double DefaultAnimationTime = 2.0d;

        //
        // Basic memeber management
        //

        public void Add(PackageContainerRepoItem fi)
        {
            if (fi == null)
                return;
            this.FileMap?.Add(fi);
            fi.ContainerList = this;
        }

        public void Remove(PackageContainerRepoItem fi)
        {
            if (fi == null || this.FileMap == null)
                return;
            if (!this.FileMap.Contains(fi))
                return;
            this.FileMap.Remove(fi);
        }

        public void MoveUp(PackageContainerRepoItem fi)
        {
            this.MoveElementInListUpwards<PackageContainerRepoItem>(this.FileMap, fi);
        }

        public void MoveDown(PackageContainerRepoItem fi)
        {
            this.MoveElementInListDownwards<PackageContainerRepoItem>(this.FileMap, fi);
        }

        //
        // IFindRepo interface
        //

        public PackageContainerRepoItem FindByAssetId(string aid)
        {
            return this.FileMap?.FirstOrDefault((fi) =>
            {
                foreach (var id in fi.EnumerateAssetIds())
                    if (id?.Trim() == aid.Trim())
                        return true;
                return false;
            });
        }

        public PackageContainerRepoItem FindByAasId(string aid)
        {
            return this.FileMap?.FirstOrDefault((fi) =>
            {
                foreach (var id in fi.EnumerateAasIds())
                    if (id?.Trim() == aid.Trim())
                        return true;
                return false;
            });
        }

        public IEnumerable<PackageContainerRepoItem> EnumerateItems()
        {
            if (this.FileMap != null)
                foreach (var fi in this.FileMap)
                    yield return fi;
        }

        public bool Contains(PackageContainerRepoItem fi)
        {
            return true == this.FileMap?.Contains(fi);
        }

        //
        // Visual effects
        //

        public void DecreaseVisualTimeBy(double amount)
        {
            if (this.FileMap != null)
                foreach (var fm in this.FileMap)
                    fm.VisualTime = Math.Max(0.0, fm.VisualTime - amount);
        }

        public void StartAnimation(PackageContainerRepoItem fi, PackageContainerRepoItem.VisualStateEnum state)
        {
            // access
            if (fi == null || this.FileMap == null || !this.FileMap.Contains(fi))
                return;

            // stop?
            if (state == PackageContainerRepoItem.VisualStateEnum.Idle)
            {
                fi.VisualState = PackageContainerRepoItem.VisualStateEnum.Idle;
                fi.VisualTime = 0.0d;
                return;
            }

            // start
            fi.VisualState = state;
            fi.VisualTime = this.DefaultAnimationTime;
        }

        //
        // Find & file operations
        //

        /// <summary>
        /// Retrieve the full location specification of the item w.r.t. to persistency container 
        /// (filesystem, HTTP, ..)
        /// </summary>
        /// <returns></returns>
        public virtual string GetFullItemLocation(string location)
        {
            return null;
        }

        protected JsonSerializerSettings GetSerializerSettings()
        {
            // need special settings (to handle different typs of child classes of PackageContainer)
            var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(
                new[] { typeof(PackageContainerListBase), typeof(PackageContainerLocalFile),
                    typeof(PackageContainerNetworkHttpFile) });
            return settings;
        }

        public void SaveAsLocalFile(string fn)
        {
            using (var s = new StreamWriter(fn))
            {
                var settings = GetSerializerSettings();
                settings.TypeNameHandling = TypeNameHandling.Auto;
                var json = JsonConvert.SerializeObject(this, Formatting.Indented, settings);
                s.WriteLine(json);
            }
        }

        public void AddByAasPackage(PackageCentral packageCentral, AdminShellPackageEnv env, string fn)
        {
            // access
            if (env == null)
                return;

            // ok, add
            var fi = PackageContainerFactory.GuessAndCreateFor(
                packageCentral,
                location: fn,
                fullItemLocation: fn,
                overrideLoadResident: false,
                containerOptions: PackageContainerOptionsBase.CreateDefault(Options.Curr));

            if (fi is PackageContainerRepoItem ri)
            {
                fi.Env = env;
                ri.CalculateIdsTagAndDesc();
                ri.VisualState = PackageContainerRepoItem.VisualStateEnum.ReadFrom;
                ri.VisualTime = 2.0;
                this.Add(ri);
            }
        }

        public void AddByAasxFn(PackageCentral packageCentral, string fn)
        {
            try
            {
                // load
                var pkg = new AdminShellPackageEnv(fn);

                // for each Admin Shell and then each AssetInformation
                this.AddByAasPackage(packageCentral, pkg, fn);

                // close directly!
                pkg.Close();
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }
        }


        public virtual void DeletePackageFromServer(PackageContainerRepoItem repoItem)
        {
            Remove(repoItem);
        }

        //
        // Converters & generators
        //

        public void PopulateFakePackage(AdminShellPackageEnv pkg)
        {
            // access
            if (pkg == null)
                return;

            // all files
            int i = 0;
            foreach (var fi in this.FileMap)
            {
                // sure?
                if (fi == null)
                    continue;
                i++;

                // aas
                if (fi.AasIds != null)
                    foreach (var id in fi.AasIds)
                    {
                        var aas = new AssetAdministrationShell("", new AssetInformation(AssetKind.Instance),idShort:String.Format("AAS{0:00}_{1}", i, fi.Tag));
                        aas.Description = new List<LangString>() { new LangString("en?", "" + fi.Description) };
                        aas.Id = id;
                        pkg.AasEnv?.AssetAdministrationShells.Add(aas);
                    }

                // asset
                if (fi.AssetIds != null)
                    foreach (var id in fi.AssetIds)
                    {
                        var asset = new AssetInformation(AssetKind.Instance);
                        //TODO:jtikekar globalAssetId or SpecficAssetId??
                        asset.GlobalAssetId = new AasCore.Aas3_0_RC02.Reference(ReferenceTypes.GlobalReference, new List<AasCore.Aas3_0_RC02.Key>() { new AasCore.Aas3_0_RC02.Key(AasCore.Aas3_0_RC02.KeyTypes.GlobalReference, id) });
                        //asset.identification = new Identification(
                        //    Identification.IRI, "" + id);
                    }
            }
        }

        public static PackageContainerListBase CreateDemoData()
        {
            var tr = new PackageContainerListBase();

            tr.Add(new PackageContainerRepoItem("http://pk.festo.com/111111111111", "1.aasx"));
            tr.Add(new PackageContainerRepoItem("http://pk.festo.com/222222222222", "2.aasx"));
            tr.Add(new PackageContainerRepoItem("http://pk.festo.com/333333333333", "3.aasx"));

            tr.FileMap[0].Description = "Additional info";
            tr.FileMap[0].AasIds.Add("http://smart.festo.com/cdscsdbdsbchjdsbjhcbhjdsbchjsdbhjcsdbhjcdsbhjcsbdhj");
            tr.FileMap[0].VisualState = PackageContainerRepoItem.VisualStateEnum.Activated;
            tr.FileMap[0].VisualTime = 6.0;

            tr.FileMap[1].Description = "Additional info";
            tr.FileMap[1].VisualState = PackageContainerRepoItem.VisualStateEnum.ReadFrom;
            tr.FileMap[1].VisualTime = 3.0;

            tr.FileMap[2].VisualState = PackageContainerRepoItem.VisualStateEnum.WriteTo;
            tr.FileMap[2].VisualTime = 4.5;

            return tr;
        }

        public bool LoadFromLocalFile(string fn)
        {
            // from file
            if (!System.IO.File.Exists(fn))
                return false;

            // need special settings (to handle different typs of child classes of PackageContainer)
            var settings = GetSerializerSettings();

            var init = System.IO.File.ReadAllText(fn);
            JsonConvert.PopulateObject(init, this, settings);

            // return
            return true;
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
