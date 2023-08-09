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
    /// A little factory class to help creating the correct instances from <c>AasxFileRepoBase</c>.
    /// </summary>
    // Resharper disable once ClassNeverInstantiated.Global
    public class PackageContainerListFactory
    {
        public static PackageContainerListBase GuessAndCreateNew(string location)
        {
            // access
            if (!location.HasContent())
                return null;

            // http based?
            var ll = location.Trim().ToLower();
            if (ll.StartsWith("http://") || ll.StartsWith("https"))
            {
                var repo = new PackageContainerListHttpRestRepository(location);
                return repo;
            }

            // default
            return PackageContainerListLocal.Load<PackageContainerListLocal>(location);
        }
    }
}
