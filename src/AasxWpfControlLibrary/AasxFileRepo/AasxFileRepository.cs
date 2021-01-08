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
    /// This simple file repository holds associations between locations of AASX packages and their
    /// respective assetIds, aasIds and submodelIds.
    /// Starting with JAN 2021, Lists of these Ids will be maintained. Goal is to describe, WHAT is in
    /// an package (from outside perspective) and if it is worth to be inspected closer.
    /// Note: to make this easy, only the value-strings of the Ids are maintained. A 2nd check needs
    /// to ensure full AAS KeyList compatibility.
    /// Additionally, it has some view model capabilities in order to animate some visual indications
    /// </summary>
    public class AasxFileRepository : IRepoFind
    {
        public string Header;

        [JsonProperty(PropertyName = "filemaps")]
        public ObservableCollection<AasxFileRepositoryItem> FileMap = new ObservableCollection<AasxFileRepositoryItem>();

        [JsonIgnore]
        public string Filename = null;

        [JsonIgnore]
        public double DefaultAnimationTime = 2.0d;

        public void Add(AasxFileRepositoryItem fi)
        {
            this.FileMap?.Add(fi);
        }

        public void Remove(AasxFileRepositoryItem fi)
        {
            if (fi == null || this.FileMap == null)
                return;
            if (!this.FileMap.Contains(fi))
                return;
            this.FileMap.Remove(fi);
        }

        public void MoveUp(AasxFileRepositoryItem fi)
        {
            this.MoveElementInListUpwards<AasxFileRepositoryItem>(this.FileMap, fi);
        }

        public void MoveDown(AasxFileRepositoryItem fi)
        {
            this.MoveElementInListDownwards<AasxFileRepositoryItem>(this.FileMap, fi);
        }

        //
        // IFindRepo interface
        //

        public AasxFileRepositoryItem FindByAssetId(string aid)
        {
            return this.FileMap?.FirstOrDefault((fi) =>
            {                
                foreach (var id in fi.EnumerateAssetIds())
                    if (id?.Trim() == aid.Trim())
                        return true;
                return false;
            });
        }

        public AasxFileRepositoryItem FindByAasId(string aid)
        {
            return this.FileMap?.FirstOrDefault((fi) =>
            {
                foreach (var id in fi.EnumerateAasIds())
                    if (id?.Trim() == aid.Trim())
                        return true;
                return false;
            });
        }

        public IEnumerable<AasxFileRepositoryItem> EnumerateItems()
        {
            if (this.FileMap != null)
                foreach (var fi in this.FileMap)
                    yield return fi;
        }

        public bool Contains(AasxFileRepositoryItem fi)
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

        public void StartAnimation(AasxFileRepositoryItem fi, AasxFileRepositoryItem.VisualStateEnum state)
        {
            // access
            if (fi == null || this.FileMap == null || !this.FileMap.Contains(fi))
                return;

            // stop?
            if (state == AasxFileRepositoryItem.VisualStateEnum.Idle)
            {
                fi.VisualState = AasxFileRepositoryItem.VisualStateEnum.Idle;
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

        public string GetFullFilename(AasxFileRepositoryItem fi)
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
            var fi = new AasxFileRepositoryItem(
                assetId: assetId, aasId: aasId, fn: fn, tag: "" + tag, description: desc);
            fi.VisualState = AasxFileRepositoryItem.VisualStateEnum.ReadFrom;
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
                        var aas = new AdminShell.AdministrationShell(String.Format("AAS{0:00}_{1}", i, fi.Tag));
                        aas.AddDescription("en?", "" + fi.Description);
                        aas.identification = new AdminShell.Identification(
                            AdminShell.Identification.IRI, "" + id);
                        pkg.AasEnv?.AdministrationShells.Add(aas);
                    }

                // asset
                if (fi.AssetIds != null)
                    foreach (var id in fi.AssetIds)
                    {
                        var asset = new AdminShell.Asset(String.Format("Asset{0:00}_{1}", i, fi.Tag));
                        asset.AddDescription("en?", "" + fi.Description);
                        asset.identification = new AdminShell.Identification(
                            AdminShell.Identification.IRI, "" + id);
                        pkg.AasEnv?.Assets.Add(asset);
                    }
            }
        }

        // Generators

        public static AasxFileRepository CreateDemoData()
        {
            var tr = new AasxFileRepository();

            tr.Add(new AasxFileRepositoryItem("http://pk.festo.com/111111111111", "1.aasx"));
            tr.Add(new AasxFileRepositoryItem("http://pk.festo.com/222222222222", "2.aasx"));
            tr.Add(new AasxFileRepositoryItem("http://pk.festo.com/333333333333", "3.aasx"));

            tr.FileMap[0].Description = "Additional info";
            tr.FileMap[0].AasIds.Add("http://smart.festo.com/cdscsdbdsbchjdsbjhcbhjdsbchjsdbhjcsdbhjcdsbhjcsbdhj");
            tr.FileMap[0].VisualState = AasxFileRepositoryItem.VisualStateEnum.Activated;
            tr.FileMap[0].VisualTime = 6.0;

            tr.FileMap[1].Description = "Additional info";
            tr.FileMap[1].VisualState = AasxFileRepositoryItem.VisualStateEnum.ReadFrom;
            tr.FileMap[1].VisualTime = 3.0;

            tr.FileMap[2].VisualState = AasxFileRepositoryItem.VisualStateEnum.WriteTo;
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
