/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AasxIntegrationBase;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// A container list ("repository" of local files) which lies on local storage.
    /// Just a deriative from <c>PackageContainerListBase</c>. Only small additions.
    /// Note: this is a base class for the PackageContainerListLocal, PackageContainerListLastRecentlyUsed
    /// </summary>
    public class PackageContainerListLocalBase : PackageContainerListBase
    {
        //
        // Members
        //

        [JsonIgnore]
        public string Filename = null;

        //
        // Specific for files
        //

        public void SaveAs(string fn)
        {
            base.SaveAsLocalFile(fn);

            // record
            this.Filename = fn;
        }

        public static T Load<T>(string fn) where T : PackageContainerListLocalBase, new()
        {
            // make sub type, but populate base type
            var repo = new T();
            if (!repo.LoadFromLocalFile(fn))
                return null;

            // record
            repo.Filename = fn;

            // return
            return repo;
        }

        /// <summary>
        /// Retrieve the full location specification of the item w.r.t. to persistency container 
        /// (filesystem, HTTP, ..)
        /// </summary>
        /// <returns></returns>
        public override string GetFullItemLocation(string location)
        {
            // access
            if (location == null)
                return null;

            // relative to this?
            var fn = location;
            try
            {
                bool doFull =
                    !(Path.IsPathRooted(fn))
                      && !fn.Contains("://"); // contains scheme

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

        /// <summary>
        /// Make all locations of file items as short as possible and relative to the location
        /// of the overall file repo. This helps re-locating and sharing repositories
        /// </summary>
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
                    var fileUri = new Uri(Path.GetFullPath(fi.Location));

                    var relUri = baseUri.MakeRelativeUri(fileUri);
                    var relPath = relUri.ToString().Replace("/", "\\");

                    fi.Location = relPath;
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
        }

    }

    /// <summary>
    /// A container list ("repository" of local files) which lies on local storage.
    /// Just a deriative from <c>PackageContainerListBase</c>. Only small additions.
    /// </summary>
    public class PackageContainerListLocal : PackageContainerListLocalBase
    {
    }

    /// <summary>
    /// A container list ("repository" of local files) which lies on local storage.
    /// Just a deriative from <c>PackageContainerListBase</c>. Only small additions.
    /// </summary>
    public class PackageContainerListLastRecentlyUsed : PackageContainerListLocalBase
    {
        /// <summary>
        /// Maximum number of items. Oldest will be deleted. 
        /// New items are added to the top.
        /// </summary>
        public const int MaxItems = 30;

        /// <summary>
        /// Use this to get the supposed default name of LRU in the binary folder of the application
        /// </summary>
        /// <returns>Full path</returns>
        public static string BuildDefaultFilename()
        {
            var exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            var fn = Path.Combine(Path.GetDirectoryName(exePath),
                        "last-recently-used.json");
            return fn;
        }

        public void Push(PackageContainerRepoItem item, string fullPath)
        {
            // access
            if (item == null || !fullPath.HasContent())
                return;

            // make a COPY (flexible in types)
            var jsonCopy = JsonConvert.SerializeObject(item);
            var itemCopy = JsonConvert.DeserializeObject<PackageContainerRepoItem>(jsonCopy);
            if (itemCopy == null)
                return;

            // record new location
            itemCopy.Location = fullPath;

            // check, if already in
            PackageContainerRepoItem foundItem = null;
            foreach (var it in EnumerateItems())
                if (it?.Location?.Trim().ToLower() == fullPath.Trim().ToLower())
                {
                    foundItem = it;
                    break;
                }

            // if so, delete it
            if (foundItem != null)
                this.Remove(foundItem);

            // add at top
            FileMap.Insert(0, itemCopy);
            itemCopy.ContainerList = this;

            // if to large, crop
            if (FileMap.Count > MaxItems)
                FileMap.Remove(FileMap.Last());
        }
    }
}
