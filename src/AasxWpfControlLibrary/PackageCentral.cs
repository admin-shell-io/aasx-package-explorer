/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxPackageExplorer;
using AdminShellNS;

namespace AasxWpfControlLibrary
{
    /// <summary>
    /// Excpetions thrown when handling PackageContainer or PackageCentral
    /// </summary>
    public class PackageCentralException : Exception
    {
        public PackageCentralException() { }
        public PackageCentralException(string message) : base(message) { }
    }

    /// <summary>
    /// This class is an item maintained by the PackageCentral.
    /// Note: this class works on application level; it means to be resilient, reports errors to the Log
    /// instead of throwing exceptions, works in small portions and such ..
    /// </summary>
    public class PackageCentralItem
    {
        public PackageContainerBase Container;

        public void New()
        {
            try
            {
                if (Container != null)
                {
                    if (Container.IsOpen)
                        Container.Close();
                    Container = null;
                }
            } catch (Exception ex)
            {
                throw new PackageCentralException(
                    $"PackageCentral: while performing new " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        public bool Load(string location, bool loadResident)
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
                Container = new PackageContainerLocalFile(location, loadResident: true);

                // success!
                return true;
            }
            catch (Exception ex)
            {
                throw new PackageCentralException(
                    $"PackageCentral: while performing load from {location} " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
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

        public bool SaveAs(string saveAsNewFileName = null)
        {
            try
            {
                if (Container is IPackageContainerLoadSave cls)
                {
                    cls.SaveToSource(saveAsNewFileName);
                    return true;
                }

                // no success
                return false;
            }
            catch (Exception ex)
            {
                throw new PackageCentralException(
                    $"PackageCentral: while saving {"" + Container?.ToString()} " +
                    $"with new filename {""  + saveAsNewFileName}" +
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

        private PackageCentralItem main = new PackageCentralItem();
        private PackageCentralItem aux = new PackageCentralItem();

        public PackageCentralItem MainContainer
        {
            get { return main; }
            set { main = value; }
        }

        public PackageCentralItem AuxContainer
        {
            get { return aux; }
        }

        public AdminShellPackageEnv Main
        {
            get { return main?.Container?.Env; }
            // set { main.Env = value; }
        }

        public AdminShellPackageEnv Aux
        {
            get { return aux?.Container?.Env; }
            // set { aux.Env = value; }
        }

        private AasxFileRepository fileRepository;

        public AasxFileRepository FileRepository
        {
            get { return fileRepository; }
            set { fileRepository = value; }
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

    }
}
