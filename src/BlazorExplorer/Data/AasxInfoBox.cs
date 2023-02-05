/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using BlazorExplorer;
using BlazorExplorer.Shared;
using Microsoft.JSInterop;

namespace BlazorUI.Data
{
    /// <summary>
    /// The info box is the small figure of the AAS with ids and image inside
    /// </summary>
    public class AasxInfoBox
    {
        /// <summary>
        /// AAS id to be displayed
        /// </summary>
        public string AasId { get; set; }

        /// <summary>
        /// Asset id to be displayed
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// Converted base64 image data to be displayed
        /// </summary>
        public string HtmlImageData { get; set; }
        
    }
}
