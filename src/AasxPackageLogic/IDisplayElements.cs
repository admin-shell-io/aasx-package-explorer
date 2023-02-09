/*
Copyright (c) 2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


using AasxPackageLogic;
using AnyUi;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System;
using Org.Webpki.JsonCanonicalizer;
using System.IO;
using System.Windows;
using AasxIntegrationBase;
using Jose;
using System.Threading;
using AasxPackageLogic.PackageCentral;
using Newtonsoft.Json.Serialization;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxPackageExplorer
{
    /// <summary>
    /// UI abstracting interface to displayed AAS tree elements (middle section)
    /// </summary>
    public interface IDisplayElements
    {
        /// <summary>
        /// If it boils down to one item, which is the selected item.
        /// </summary>
        VisualElementGeneric GetSelectedItem();

        /// <summary>
        /// Clears only selection.
        /// </summary>
		void ClearSelection();

        /// <summary>
        /// Carefully checks and tries to select a tree item which is identified
        /// by the main data object (e.g. an AAS, SME, ..)
        /// </summary>
        bool TrySelectMainDataObject(object dataObject, bool? wishExpanded);
    }
   
}