﻿/*
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
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
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
        
        public void SetInfos(Aas.AssetAdministrationShell aas, AdminShellPackageEnv env)
        {
            // access
            AasId = "";
            if (aas == null)
                return;

            // basic data
            if (aas.Id?.HasContent() == true)
                AasId = aas.Id;
            AssetId = "";
            if (aas.AssetInformation?.GlobalAssetId != null)
                AssetId = aas.AssetInformation.GlobalAssetId.ToStringExtended(2);

            // image data?
            try
            {
                if (env != null)
                {
                    System.IO.Stream s = null;
                    try
                    {
                        s = env.GetLocalThumbnailStream();
                    }
                    catch
                    {
                        s = null;
                    }
                    if (s != null)
                    {
                        using (var m = new System.IO.MemoryStream())
                        {
                            s.CopyTo(m);
                            HtmlImageData = System.Convert.ToBase64String(m.ToArray());
                        }

                        // it is indespensible to properly close the thumbnail stream!
                        // practice proofed not to use using ..
                        s.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            // TODO: missng for WPF refactoring
            // asset thumbnail
            //        try
            //        {
            //            // identify which stream to use..
            //            if (_packageCentral.MainAvailable)
            //                try
            //                {
            //                    using (var thumbStream = _packageCentral.Main.GetLocalThumbnailStream())
            //                    {
            //                        // load image
            //                        if (thumbStream != null)
            //                        {
            //                            var bi = new BitmapImage();
            //                            bi.BeginInit();

            //                            // See https://stackoverflow.com/a/5346766/1600678
            //                            bi.CacheOption = BitmapCacheOption.OnLoad;

            //                            bi.StreamSource = thumbStream;
            //                            bi.EndInit();
            //                            this.AssetPic.Source = bi;
            //                        }
            //                    }
            //                }
            //                catch (Exception ex)
            //                {
            //                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            //                }
        }
    }
}
