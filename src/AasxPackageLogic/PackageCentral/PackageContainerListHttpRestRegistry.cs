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
    /// AasxFileRepository, which is build temporarily from a AAS registry query. 
    /// Just a deriative from <c>PackageContainerListBase</c>. Only small additions.
    /// </summary>
    // Resharper disable once ClassNeverInstantiated.Global
    public class PackageContainerListHttpRestRegistry : PackageContainerListHttpRestBase
    {
    }
}
