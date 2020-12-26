/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxPackageExplorer;
using AdminShellNS;

namespace AasxWpfControlLibrary.PackageCentral
{
    /// <summary>
    /// Excpetions thrown when handling PackageContainer or PackageCentral
    /// </summary>
    public class PackageContainerException : Exception
    {
        public PackageContainerException() { }
        public PackageContainerException(string message) : base(message) { }
    }

    /// <summary>
    /// The container wraps an AdminShellPackageEnv with the availability to upload, download, re-new the package env
    /// and to transport further information (future use).
    /// </summary>
    public class PackageContainerBase
    {
        public enum Format { Unknown = 0, AASX, XML, JSON }
        public static string[] FormatExt = { ".bin", ".aasx", ".xml", ".json" };

        public AdminShellPackageEnv Env;
        public Format IsFormat = Format.Unknown;

        /// <summary>
        /// If true, then PackageContainer will try to automatically load the contents of the package
        /// on application level.
        /// </summary>
        public bool LoadResident;

        //
        // Different capabilities are modelled as delegates, which can be present or not (null), depening
        // on dynamic protocoll capabilities
        //

        /// <summary>
        /// Can load an AASX from (already) given data source
        /// </summary>
        public delegate void CapabilityLoadFromSource();

        /// <summary>
        /// Can save the (edited) AASX to an already given or new dta source name
        /// </summary>
        /// <param name="saveAsNewFilename"></param>
        public delegate void CapabilitySaveAsToSource(string saveAsNewFilename = null);

        // the derived classes will selctively set the capabilities
        public CapabilityLoadFromSource LoadFromSource = null;
        public CapabilitySaveAsToSource SaveAsToSource = null;

        //
        // Base functions
        //

        public static Format EvalFormat(string fn)
        {
            Format res = Format.Unknown;
            var ext = Path.GetExtension(fn).ToLower();
            foreach (var en in (Format[])Enum.GetValues(typeof(Format)))
                if (ext == FormatExt[(int)en])
                    res = en;
            return res;
        }

        public bool IsOpen { get { return Env != null && Env.IsOpen; } }

        public void Close()
        {
            if (!IsOpen)
                return;
            Env.Close();
            Env = null;
        }
    }

    /// <summary>
    /// This container was taken over from AasxPackageEnv and lacks therefore further
    /// load/ store information
    /// </summary>
    public class PackageContainerTakenOver : PackageContainerBase
    {
    }

    /// <summary>
    /// This container add functionalities for "indirect load/ save" and backing up file contents
    /// </summary>
    public class PackageContainerBuffered : PackageContainerBase
    {
        public bool IndirectLoadSave = false;

        public string TempFn;

        public string CreateNewTempFn(string sourceFn, Format fmt)
        {
            // TODO (MIHO, 2020-12-25): think of creating a temp file which resemebles the source file
            // name (for ease of handling)
            var res = System.IO.Path.GetTempFileName().Replace(".tmp", FormatExt[(int)fmt]);
            return res;
        }
    }

    public static class PackageContainerFactory
    {
        public static PackageContainerBase GuessAndCreateFor(string location, bool loadResident)
        {
            // access
            if (location == null)
                return null;
            var ll = location.ToLower();

            // starts with http ?
            if (ll.StartsWith("http://") || ll.StartsWith("https://"))
            {
                // direct evidence of /getaasx/
                if (ll.Contains("/server/getaasx/"))
                    return new PackageContainerNetworkHttpFile(location, loadResident);
            }

            // check FileInfo for (possible?) local file
            try
            {
                var fi = new FileInfo(location);
                if (fi != null)
                    // seems to be a valid (possible) file
                    return new PackageContainerLocalFile(location, loadResident);
            } catch { }

            return null;
        }        
    }
}
