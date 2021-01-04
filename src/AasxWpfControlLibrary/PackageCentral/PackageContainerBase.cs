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
using AasxIntegrationBase;
using AasxPackageExplorer;
using AdminShellEvents;
using AdminShellNS;

namespace AasxWpfControlLibrary.PackageCentral
{
    /// <summary>
    /// Exceptions thrown when handling PackageContainer or PackageCentral
    /// </summary>
    public class PackageContainerException : Exception
    {
        public PackageContainerException() { }
        public PackageContainerException(string message) : base(message) { }
    }

    public class PackageContainerCredentials
    {
        public string Username;
        public string Password;
    }

    /// <summary>
    /// Extendable run-time options 
    /// </summary>
    public class PackageContainerRuntimeOptions
    {
        public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded);

        public delegate void AskForSelectFromListHandler(
            string caption, List<SelectFromListFlyoutItem> list,
            TaskCompletionSource<SelectFromListFlyoutItem> propagateResult);

        public delegate void AskForCredentialsHandler(
            string caption, TaskCompletionSource<PackageContainerCredentials> propagateResult);

        public LogInstance Log;
        public ProgressChangedHandler ProgressChanged;
        
        public AskForSelectFromListHandler AskForSelectFromList;

        public AskForCredentialsHandler AskForCredentials;
    }

    /// <summary>
    /// The container wraps an AdminShellPackageEnv with the availability to upload, download, re-new the package env
    /// and to transport further information (future use).
    /// </summary>
    public class PackageContainerBase : IPackageConnectorManageEvents
    {
        public enum Format { Unknown = 0, AASX, XML, JSON }
        public static string[] FormatExt = { ".bin", ".aasx", ".xml", ".json" };

        public enum BackupType { XML = 0, FullCopy }

        public AdminShellPackageEnv Env;
        public Format IsFormat = Format.Unknown;

        private PackageCentral _packageCentral;
        public PackageCentral PackageCentral { get { return _packageCentral; } }

        /// <summary>
        /// If true, then PackageContainer will try to automatically load the contents of the package
        /// on application level.
        /// </summary>
        public bool LoadResident;

        /// <summary>
        /// If the connection shall stay alive, a appropriate connector needs to be created.
        /// Note, that the connector is an indeppendent object, but will have a link to this
        /// container!
        /// </summary>
        public PackageConnectorBase ConnectorPrimary;

        /// <summary>
        /// Holds secondary connectors, which might also want to register to the SAME AAS!
        /// Examples could be plugins for different interface standards.
        /// </summary>
        public List<PackageConnectorBase> ConnectorSecondary = new List<PackageConnectorBase>();

        //
        // Different capabilities are modelled as delegates, which can be present or not (null), depening
        // on dynamic protocoll capabilities
        //

        /// <summary>
        /// Can load an AASX from (already) given data source
        /// </summary>
        public delegate void CapabilityLoadFromSource(
            PackageContainerRuntimeOptions runtimeOptions = null);

        /// <summary>
        /// Can save the (edited) AASX to an already given or new dta source name
        /// </summary>
        /// <param name="saveAsNewFilename"></param>
        public delegate void CapabilitySaveAsToSource(
            string saveAsNewFilename = null,
            AdminShellPackageEnv.SerializationFormat prefFmt = AdminShellPackageEnv.SerializationFormat.None);

        // the derived classes will selctively set the capabilities
        public CapabilityLoadFromSource LoadFromSource = null;
        public CapabilitySaveAsToSource SaveAsToSource = null;

        //
        // Constructors
        //

        public PackageContainerBase() { }

        public PackageContainerBase(PackageCentral packageCentral)
        {
            _packageCentral = packageCentral;
        }

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

        public virtual string Filename { get { return null; } }

        public bool IsOpen { get { return Env != null && Env.IsOpen; } }

        public void Close()
        {
            if (!IsOpen)
                return;
            Env.Close();
            Env = null;
        }

        public virtual void BackupInDir(string backupDir, int maxFiles, BackupType backupType = BackupType.XML) 
        {
        }

        //
        // Connector management
        //

        public IEnumerable<PackageConnectorBase> GetAllConnectors()
        {
            if (ConnectorPrimary != null)
                yield return ConnectorPrimary;
            if (ConnectorSecondary != null)
                foreach (var conn in ConnectorSecondary)
                    yield return conn;
        }

        //
        // Event management
        //

        public bool PushEvent(AasEventMsgBase ev)
        {
            // access
            if (ev == null)
                return false;
            var consume = false;

            // use enumerator
            foreach (var con in GetAllConnectors())
            {
                var br = con.PushEvent(ev);
                if (br)
                {
                    consume = true;
                    break;
                }
            }

            // done
            return consume;
        }
    }

    /// <summary>
    /// This container was taken over from AasxPackageEnv and lacks therefore further
    /// load/ store information
    /// </summary>
    public class PackageContainerTakenOver : PackageContainerBase
    {
    }
}
