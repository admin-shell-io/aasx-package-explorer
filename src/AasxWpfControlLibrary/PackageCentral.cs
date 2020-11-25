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
    /// The source of a package in terms of where the package was loaded from.
    /// Source infornmation shall be derived from this base class
    /// </summary>
    public class PackageSourceBase
    {
    }

    /// <summary>
    /// Package source from a local file. Can be uses as open package.
    /// </summary>
    public class PackageSourceLocalFile : PackageSourceBase
    {
        public string Path;
    }

    /// <summary>
    /// Package source from a registry. Package needs to be loaded and stored there.
    /// </summary>
    public class PackageSourceRegistry : PackageSourceBase
    {
        public Uri Server;
        public string AasxIndex;
    }

    /// <summary>
    /// The container wraps an AdminShellPackageEnv with the availability to re-new the package env
    /// and to transport further information (future use)
    /// </summary>
    public class PackageContainer
    {
        public AdminShellPackageEnv Env;
    }

    /// <summary>
    /// This class (more: a singleton) centralized the information on open package environments for the
    /// Pakcage Explorer. It hold Main and Aux packages, but also the open file repositories. In future,
    /// it might store multiple open packages for the same time, as AASX Server might require, and Main
    /// might only an index into these.
    /// </summary>
    public class PackageCentral
    {
        public enum Selector { Main, MainAux, MainAuxFileRepo }

        private PackageContainer main = new PackageContainer();
        private PackageContainer aux = new PackageContainer();

        public PackageContainer MainContainer
        {
            get { return main; }
        }

        public PackageContainer AuxContainer
        {
            get { return aux; }
        }

        public AdminShellPackageEnv Main
        {
            get { return main?.Env; }
            set { main.Env = value; }
        }

        public AdminShellPackageEnv Aux
        {
            get { return aux?.Env; }
            set { aux.Env = value; }
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
