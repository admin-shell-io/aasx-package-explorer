/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AasxPackageLogic.AdminShellEvents;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPackageLogic.PackageCentral
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
    public class PackCntRuntimeOptions
    {
        public enum Progress { Idle, Starting, Ongoing, Final }

        public delegate void ProgressChangedHandler(Progress state, long? totalFileSize, long totalBytesDownloaded);

        public delegate void AskForSelectFromListHandler(
            string caption, List<AnyUiDialogueListItem> list,
            TaskCompletionSource<AnyUiDialogueListItem> propagateResult);

        public delegate void AskForCredentialsHandler(
            string caption, TaskCompletionSource<PackageContainerCredentials> propagateResult);

        public LogInstance Log;
        public ProgressChangedHandler ProgressChanged;

        public AskForSelectFromListHandler AskForSelectFromList;

        public AskForCredentialsHandler AskForCredentials;

        public delegate AnyUiMessageBoxResult ShowMessageDelegate(
                            string content, string text, string caption,
                            AnyUiMessageBoxButton buttons = 0);
        public ShowMessageDelegate ShowMesssageBox;
    }

    public enum PackCntChangeEventReason
    {
        /// <summary>
        /// A "session" of multiple possible changes is started
        /// </summary>
        StartOfChanges,

        /// <summary>
        /// A Referable is created within the "typical" enumeration of another Referable.
        /// </summary>
        Create,

        /// <summary>
        /// An existing referable was moved from its current position to the new indxed position
        /// denoted by <c>NewIndex</c>
        /// </summary>
        MoveToIndex,

        /// <summary>
        /// Multiple changes (dreate, delete, move) are summarized w.r.t to a Referable
        /// </summary>
        StructuralUpdate,

        /// <summary>
        /// A Referable is deleted
        /// </summary>
        Delete,

        /// <summary>
        /// A value upatde of a single Referable (not children) is performed
        /// </summary>
        ValueUpdateSingle,

        /// <summary>
        /// A value upatde of a Referable including possible children is performed
        /// </summary>
        ValueUpdateHierarchy,

        /// <summary>
        /// An exception prevented the successful execution of the change.
        /// The exception is detailed in the info message.
        /// </summary>
        Exception,

        /// <summary>
        /// A "session" of multiple possible changes is finalized
        /// </summary>
        EndOfChanges
    }

    /// <summary>
    /// Simplified change event data, which is emitted by Package Container logic to the rest of the main application in
    /// order to report on changes.. of container / AAS contents.
    /// </summary>
    public class PackCntChangeEventData
    {
        /// <summary>
        /// Identification of the container
        /// </summary>
        public PackageContainerBase Container;

        /// <summary>
        /// The reason for emiting the event
        /// </summary>
        public PackCntChangeEventReason Reason;

        /// <summary>
        /// Changed AAS element itself (typically a Referable, but could also be a SubmodelRef)
        /// </summary>
        public AdminShell.IAasElement ThisElem;

        /// <summary>
        /// Parent object/ structure of the changed AAS element. Often a Referable, but could also be a SubmodelRef.
        /// Can be also <c>null</c>, if the type of the ThisObj is already indicating the parent structure (e.g. 
        /// for Assets, ConceptDescriptions, ..)
        /// </summary>
        public AdminShell.IAasElement ParentElem;

        /// <summary>
        /// If create, at which index; else: -1
        /// </summary>
        public int NewIndex = -1;

        /// <summary>
        /// Human readable explanation, help, annotation, info
        /// </summary>
        public string Info;

        /// <summary>
        /// Disables the re-display of the selected AAS element, e.g. for speed reasons
        /// </summary>
        public bool DisableSelectedTreeItemChange;

        public PackCntChangeEventData() { }

        /// <summary>
        /// Create new event data.
        /// </summary>
        /// <param name="container">Identification of the container</param>
        /// <param name="reason">The reason</param>
        /// <param name="thisRef">Changed Referable itself</param>
        /// <param name="parentRef">A Referable, which contains the changed Referable.</param>
        /// <param name="createAtIndex">If create, at which index; else: -1</param>
        /// <param name="info">Human readable information</param>
        public PackCntChangeEventData(PackageContainerBase container,
            PackCntChangeEventReason reason,
            AdminShell.Referable thisRef = null,
            AdminShell.Referable parentRef = null,
            int createAtIndex = -1,
            string info = null)
        {
            Container = container;
            Reason = reason;
            ThisElem = thisRef;
            ParentElem = parentRef;
            NewIndex = createAtIndex;
            Info = info;
        }
    }

    /// <summary>
    /// Main application can register for a handler.
    /// </summary>
    /// <param name="data">Data as given by the event data structure. Might be queued by the main application.</param>
    public delegate bool PackCntChangeEventHandler(PackCntChangeEventData data);

    /// <summary>
    /// The container wraps an AdminShellPackageEnv with the availability to upload, download, re-new the package env
    /// and to transport further information (future use).
    /// </summary>
    public class PackageContainerBase : IPackageConnectorManageEvents
    {
        public enum Format { Unknown = 0, AASX, XML, JSON }
        public static string[] FormatExt = { ".bin", ".aasx", ".xml", ".json" };

        public enum BackupType { XML = 0, FullCopy }

        [Flags]
        public enum CopyMode { None = 0, Serialized = 1, BusinessData = 2 }

        [JsonIgnore]
        public AdminShellPackageEnv Env = new AdminShellPackageEnv();
        [JsonIgnore]
        public Format IsFormat = Format.Unknown;

        /// <summary>
        /// Limks to the PackageCentral. Only on init.
        /// </summary>
        [JsonIgnore]
        public PackageCentral PackageCentral { get { return _packageCentral; } }
        private PackageCentral _packageCentral;

        /// <summary>
        /// Holds the container (user) options in a base oder derived class.
        /// </summary>
        [JsonProperty(PropertyName = "Options")]
        public PackageContainerOptionsBase ContainerOptions = new PackageContainerOptionsBase();

        /// <summary>
        /// Links (optionally) to the ContainerList, which hold this Container.
        /// To be set after adding to the list.
        /// </summary>
        [JsonIgnore]
        public PackageContainerListBase ContainerList;

        /// <summary>
        /// If the connection shall stay alive, a appropriate connector needs to be created.
        /// Note, that the connector is an indeppendent object, but will have a link to this
        /// container!
        /// </summary>
        [JsonIgnore]
        public PackageConnectorBase ConnectorPrimary;

        /// <summary>
        /// Holds secondary connectors, which might also want to register to the SAME AAS!
        /// Examples could be plugins for different interface standards.
        /// </summary>
        [JsonIgnore]
        public List<PackageConnectorBase> ConnectorSecondary = new List<PackageConnectorBase>();

        protected string _location = "";

        /// <summary>
        /// Location of the Container in a certain storage container, e.g. a local or network based
        /// repository. In this base implementation, it maps to a empty string.
        /// </summary>
        public virtual string Location
        {
            get { return _location; }
            set { _location = value; }
        }

        //
        // Constructors
        //

        public PackageContainerBase() { }

        public PackageContainerBase(PackageCentral packageCentral)
        {
            _packageCentral = packageCentral;
        }

        public PackageContainerBase(CopyMode mode, PackageContainerBase other, PackageCentral packageCentral = null)
        {
            if ((mode & CopyMode.Serialized) > 0 && other != null)
            {
                // nothing here
            }
            if (packageCentral != null)
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


        [JsonIgnore]
        public bool IsOpen { get { return Env != null && Env.IsOpen; } }

        public virtual void Close()
        {
            if (!IsOpen)
                return;
            Env.Close();
            Env = null;
        }

        public virtual async Task LoadResidentIfPossible(string fullItemLocation)
        {
            await Task.Yield();
        }

        public virtual void BackupInDir(string backupDir, int maxFiles, BackupType backupType = BackupType.XML)
        {
        }

        public virtual async Task LoadFromSourceAsync(
            string fullItemLocation,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            await Task.Yield();
        }

        public virtual async Task<bool> SaveLocalCopyAsync(
            string targetFilename,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            await Task.Yield();
            return false;
        }

        public virtual async Task SaveToSourceAsync(string saveAsNewFileName = null,
            AdminShellPackageEnv.SerializationFormat prefFmt = AdminShellPackageEnv.SerializationFormat.None,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            await Task.Yield();
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

        private bool CheckPushedEventInternal(AasEventMsgEnvelope ev)
        {
            // access
            if (ev == null)
                return false;

            // to be applicable, the event message Observable has to relate into this's environment
            var foundObservable = Env?.AasEnv?.FindReferableByReference(ev?.ObservableReference);
            if (foundObservable == null)
                return false;

            //
            // Update value?
            //
            // Note: an update will only be executed, if NOT ALREADY marked as being updated in the
            //       event message. MOST LIKELY, the AAS update will be done in the connector, already!
            //
            foreach (var pluv in ev.GetPayloads<AasPayloadUpdateValue>())
            {
                if (pluv.Values != null
                    && !pluv.IsAlreadyUpdatedToAAS
                    && foundObservable is AdminShell.IEnumerateChildren
                    && (foundObservable is AdminShell.Submodel || foundObservable is AdminShell.SubmodelElement))
                {
                    // will later access children ..
                    var wrappers = ((foundObservable as AdminShell.IEnumerateChildren).EnumerateChildren())?.ToList();
                    var changedSomething = false;

                    // go thru all value updates
                    if (pluv.Values != null)
                        foreach (var vl in pluv.Values)
                        {
                            if (vl == null)
                                continue;

                            // Note: currently only updating Properties
                            // TODO (MIHO, 2021-01-03): check to handle more SMEs for AasEventMsgUpdateValue

                            AdminShell.SubmodelElement smeToModify = null;
                            if (vl.Path == null && foundObservable is AdminShell.Property fop)
                                smeToModify = fop;
                            else if (vl.Path != null && vl.Path.Count >= 1 && wrappers != null)
                            {
                                var x = AdminShell.SubmodelElementWrapper.FindReferableByReference(
                                    wrappers, AdminShell.Reference.CreateNew(vl.Path), keyIndex: 0);
                                if (x is AdminShell.Property fpp)
                                    smeToModify = fpp;
                            }

                            // something to modify?
                            if (smeToModify is AdminShell.Property prop)
                            {
                                if (vl.Value != null)
                                    prop.value = vl.Value;
                                if (vl.ValueId != null)
                                    prop.valueId = vl.ValueId;
                                changedSomething = true;
                            }
                        }

                    // if something was changed, the event messages is to be consumed
                    if (changedSomething)
                        return true;
                }
            }

            // no
            return false;
        }

        public bool PushEvent(AasEventMsgEnvelope ev)
        {
            // access
            if (ev == null)
                return false;

            // internal?
            if (CheckPushedEventInternal(ev))
                return true;

            // use enumerator
            var consume = false;
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
