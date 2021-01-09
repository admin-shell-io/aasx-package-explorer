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

namespace AasxWpfControlLibrary.PackageCentral
{
    /// <summary>
    /// A container list ("repository" of local files) which lies on local storage.
    /// Just a deriative from <c>PackageContainerListBase</c>. Only small additions.
    /// </summary>
    public class PackageContainerListLocal : PackageContainerListBase
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

        public static PackageContainerListLocal Load(string fn)
        {
            // make sub type, but populate base type
            var repo = new PackageContainerListLocal();
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
        public override string GetFullItemLocation(PackageContainerRepoItem fi)
        {
            // access
            if (fi?.Location == null)
                return null;

            // relative to this?
            var fn = fi.Location;
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
}
