/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase.AdminShellEvents;
using AdminShellNS;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;
using AasxIntegrationBase;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// Excpetions thrown when handling PackageContainer or PackageCentral
    /// </summary>
    public class PackageCentralException : Exception
    {
        public PackageCentralException() { }
        public PackageCentralException(string message, Exception innerException = null)
            : base(message, innerException) { }
    }

    public delegate Aas.IReferable QuickLookupIdentifiable(string idKey);

    /// <summary>
    /// This class is an item maintained by the PackageCentral.
    /// Note: this class works on application level; it means to be resilient, reports errors to the Log
    /// instead of throwing exceptions, works in small portions and such ..
    /// </summary>
    public class PackageCentralItem
    {
        public PackageContainerBase Container;

        public string Filename { get { return Container?.Location; } }

        public override string ToString() { return (Container == null) ? "No container!" : Container?.ToString(); }

        public void New()
        {
            try
            {
                // close old
                if (Container != null)
                {
                    if (Container.IsOpen)
                        Container.Close();
                    Container = null;
                }

                // new container
                Container = new PackageContainerLocalFile();
            }
            catch (Exception ex)
            {
                throw new PackageCentralException(
                    $"PackageCentral: while performing new " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        public bool Load(
            PackageCentral packageCentral,
            string location,
            string fullItemLocation,
            bool overrideLoadResident,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            try
            {
                // close old one
                if (Container != null)
                {
                    if (Container.IsOpen)
                        Container.Close();
                    Container = null;
                }

                // figure out, what to load
                var task = Task.Run(async () => await PackageContainerFactory.GuessAndCreateForAsync(
                    packageCentral,
                    location,
                    fullItemLocation,
                    overrideLoadResident,
                    null, null,
                    containerOptions,
                    runtimeOptions));
                var guess = task.Result;

                if (guess == null)
                    return false;

                // success!
                Container = guess;
                return true;
            }
            catch (Exception ex)
            {
                throw new PackageCentralException(
                    $"PackageCentral: while performing load from {location} " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}", ex);
            }
        }

        public bool TakeOver(AdminShellPackageEnv env)
        {
            try
            {
                // close old one
                if (Container != null)
                {
                    if (Container.IsOpen)
                        Container.Close();
                    Container = null;
                }

                // figure out, what to load
                Container = new PackageContainerTakenOver();
                Container.Env = env;

                // success!
                return true;
            }
            catch (Exception ex)
            {
                throw new PackageCentralException(
                    $"PackageCentral: while performing takeover " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        public bool TakeOver(PackageContainerBase container)
        {
            try
            {
                // close old one
                if (Container != null)
                {
                    if (Container.IsOpen)
                        Container.Close();
                    Container = null;
                }

                // figure out, what to load
                Container = container;

                // success!
                return true;
            }
            catch (Exception ex)
            {
                throw new PackageCentralException(
                    $"PackageCentral: while performing takeover " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        public async Task<bool> SaveAsAsync(string saveAsNewFileName = null,
            AdminShellPackageEnv.SerializationFormat prefFmt = AdminShellPackageEnv.SerializationFormat.None,
            PackCntRuntimeOptions runtimeOptions = null,
            bool doNotRememberLocation = false)
        {
            try
            {
                await Container.SaveToSourceAsync(saveAsNewFileName, prefFmt, runtimeOptions,
                    doNotRememberLocation: doNotRememberLocation);
                return true;
            }
            catch (Exception ex)
            {
                throw new PackageCentralException(
                    $"PackageCentral: while saving {"" + Container?.ToString()} " +
                    $"with new filename {"" + saveAsNewFileName}" +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        public void Close()
        {
            try
            {
                // close old one
                if (Container != null)
                {
                    if (Container.IsOpen)
                        Container.Close();
                    Container = null;
                }
            }
            catch (Exception ex)
            {
                throw new PackageCentralException(
                     $"PackageCentral: while performing close " +
                     $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// This class (more: a singleton) centralized the information on open package environments for the
    /// Pakcage Explorer. It hold Main and Aux packages, but also the open file repositories. In future,
    /// it might store multiple open packages for the same time, as AASX Server might require, and Main
    /// might only an index into these.
    /// Note: this class works on application level; it means to be resilient, reports errors to the Log
    /// instead of throwing exceptions, works in small portions and such ..
    /// </summary>
    public class PackageCentral
    {
        public enum Selector { Main, MainAux, MainAuxFileRepo }

        //
        // Non-container  members
        //

        /// <summary>
        /// Runtime options allow Logging, Progress and dialogues go to a certain "place". This holds the
        /// "default" place, e.g. the main app.
        /// </summary>
        public PackCntRuntimeOptions CentralRuntimeOptions = null;

        /// <summary>
        /// Main application can register for change events
        /// </summary>
        public PackCntChangeEventHandler ChangeEventHandler = null;

        //
        // Container members
        //

        private PackageCentralItem _main = new PackageCentralItem();
        private PackageCentralItem _aux = new PackageCentralItem();

        public PackageCentralItem MainItem
        {
            get { return _main; }
            set { _main = value; }
        }

        public PackageCentralItem AuxItem
        {
            get { return _aux; }
        }

        public AdminShellPackageEnv Main
        {
            get { return _main?.Container?.Env; }
        }

        public AdminShellPackageEnv Aux
        {
            get { return _aux?.Container?.Env; }
        }

        // TODO (MIHO, 2021-01-07): rename to plural
        private PackageContainerListOfList _repositories;

        public PackageContainerListOfList Repositories
        {
            get { return _repositories; }
            set { _repositories = value; }
        }

        public bool MainAvailable
        {
            get { return Main?.AasEnv != null; }
        }

        public bool MainStorable
        {
            get { return Main?.AasEnv != null && Main.IsOpen; }
        }

        public bool AuxAvailable
        {
            get { return Aux?.AasEnv != null; }
        }

        //
        // Container management
        //

        public IEnumerable<PackageContainerBase> GetAllContainer()
        {
            if (_main?.Container != null)
                yield return _main?.Container;
            if (_aux?.Container != null)
                yield return _aux?.Container;
            if (_repositories != null)
                foreach (var repo in _repositories)
                    foreach (var ri in repo.EnumerateItems())
                        yield return ri;
        }

        public IEnumerable<PackageContainerBase> GetAllContainer(Func<PackageContainerBase, bool> lambda)
        {
            foreach (var cnr in GetAllContainer())
                if (lambda == null || lambda.Invoke(cnr))
                    yield return cnr;
        }

        public IEnumerable<AdminShellPackageEnv> GetAllPackageEnv()
        {
            if (_main?.Container?.Env != null)
                yield return _main?.Container.Env;
            if (_aux?.Container?.Env != null)
                yield return _aux?.Container.Env;
            if (_repositories != null)
                foreach (var repo in _repositories)
                    foreach (var ri in repo.EnumerateItems())
                        if (ri.Env != null)
                            yield return ri.Env;
        }

        public IEnumerable<AdminShellPackageEnv> GetAllPackageEnv(Func<AdminShellPackageEnv, bool> lambda)
        {
            foreach (var pe in GetAllPackageEnv())
                if (lambda == null || lambda.Invoke(pe))
                    yield return pe;
        }

        //
        // Access to identifiables
        //

        public void ReIndexIdentifiables()
        {
            foreach (var cnt in GetAllContainer())
                cnt.ReIndexIdentifiables();
		}

		/// <summary>
		/// This provides a "quick" lookup of Identifiables, e.g. based on hashes/ dictionaries.
		/// May be not 100% reliable, but quick.
		/// </summary>
		public IEnumerable<Tuple<PackageContainerBase, Aas.IReferable>> QuickLookupAllIdent(
            string idKey,
            bool deepLookup = false)
        {
            if (idKey?.HasContent() != true)
                yield break;

            foreach (var cnt in GetAllContainer())
            {
                var res = new Dictionary<Aas.IReferable, Aas.IReferable>();

                if (cnt.IdentifiableLookup != null)
                    foreach (var idf in cnt.IdentifiableLookup.LookupAllIdent(idKey))
                        if (!res.ContainsKey(idf))
                            res.Add(idf, idf);
                
                if (deepLookup && cnt.Env?.AasEnv != null)
                    foreach (var rfi in cnt.Env?.AasEnv.FindAllReferable(onlyIdentifiables: true))
                        if (rfi is Aas.IIdentifiable idf && idf.Id?.Trim() == idKey.Trim())
                            res.Add(idf, idf);
                            
                foreach (var idf in res.Keys)
                    yield return new Tuple<PackageContainerBase, IReferable>(cnt, idf);
            }
        }

		/// <summary>
		/// This provides a "quick" lookup of Identifiables, e.g. based on hashes/ dictionaries.
		/// May be not 100% reliable, but quick.
		/// </summary>
		public Aas.IReferable QuickLookupFirstIdent(string idKey)
        {
            return QuickLookupAllIdent(idKey).FirstOrDefault()?.Item2;
		}

		/// <summary>
		/// This provides a "quick" lookup of Identifiables, e.g. based on hashes/ dictionaries.
		/// May be not 100% reliable, but quick.
		/// </summary>
		public T QuickLookupFirstIdent<T>(string idKey) where T : class, Aas.IReferable
		{
			return QuickLookupAllIdent(idKey).FirstOrDefault()?.Item2 as T;
		}

        /// <summary>
        /// Will go to all accessible containers to find identifiables of a certain type
        /// </summary>
		public IEnumerable<T> FindAllReferables<T>() where T : class, Aas.IReferable
        {
            foreach (var cnt in GetAllContainer())
            {
                if (cnt.Env?.AasEnv != null)
                    foreach (var rfi in cnt.Env?.AasEnv.FindAllReferable(onlyIdentifiables: true))
                        if (rfi is T found)
                            yield return found;
            }
        }

        /// <summary>
        /// Will go to all accessible containers to find Referables by a provided reference
        /// </summary>
		public IEnumerable<Aas.IReferable> FindAllReferablesWith(Aas.IReference reference) 
        {
            if (reference == null || reference.Count() < 1)
                yield break;

            foreach (var cnt in GetAllContainer())
            {
                if (cnt.Env?.AasEnv != null)
                {
                    var rf = cnt.Env.AasEnv.FindReferableByReference(reference);
                    if (rf != null)
                        yield return rf;
                }
            }
        }

        //
        // Event management
        //

        private PackageConnectorEventStore _eventStore = null; // replaced by store within AasEventCollectionViewer
        // reason: update to ObservableCollection needs to be done in DispatcherThread
        public PackageConnectorEventStore EventStore { get { return _eventStore; } }

        private PackageConnectorEventStore _eventBufferEditor = new PackageConnectorEventStore(null);
        public PackageConnectorEventStore EventBufferEditor { get { return _eventBufferEditor; } }

        public IEnumerable<IPackageConnectorManageEvents> GetAllInstanceManageEvents()
        {
            if (_eventStore != null)
                yield return _eventStore;

            if (_eventBufferEditor != null)
                yield return _eventBufferEditor;

            foreach (var cnt in GetAllContainer())
                yield return cnt;
        }

        public bool PushEvent(AasEventMsgEnvelope ev)
        {
            // access
            if (ev == null)
                return false;
            var consume = false;

            // use enumerator
            foreach (var con in GetAllInstanceManageEvents())
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
}
