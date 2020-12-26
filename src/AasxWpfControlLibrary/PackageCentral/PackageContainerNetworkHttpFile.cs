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
using AdminShellNS;
using AasxPackageExplorer;
using System.Net.Http;
using System.Net;
using System.IO;

namespace AasxWpfControlLibrary.PackageCentral
{
    /// <summary>
    /// This container represents a file, which is retrieved by an network/ HTTP commands and
    /// buffered locally.
    /// </summary>
    public class PackageContainerNetworkHttpFile : PackageContainerBuffered
    {
        /// <summary>
        /// Uri of an AASX retrieved by HTTP
        /// </summary>
        public Uri SourceUri;

        //
        // Constructors
        //

        public PackageContainerNetworkHttpFile()
        {
            Init();
        }

        public PackageContainerNetworkHttpFile(string sourceFn, bool loadResident = false)
        {
            Init();
            SetNewSourceFn(sourceFn);
            LoadResident = loadResident;
            if (LoadResident)
                LoadFromSource();
        }

        private void Init()
        {
            this.LoadFromSource = this.InternalLoadFromSource;
            // this.SaveAsToSource = this.InternalSaveToSource;
        }

        private void SetNewSourceFn(string sourceUri)
        {
            SourceUri = new Uri(sourceUri);
            IsFormat = Format.AASX;
            IndirectLoadSave = true;
        }

        public override string ToString()
        {
            return "HTTP file: " + SourceUri;
        }

        protected void InternalLoadFromSource()
        {
            // buffer to temp file
            try
            {                
                // read via HttpClient (uses standard proxies)
                var handler = new HttpClientHandler();
                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;

                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("Accept", "application/aas");
                client.BaseAddress = new Uri(SourceUri.GetLeftPart(UriPartial.Authority));
                var requestPath = SourceUri.PathAndQuery;

                // get response?
                // later, see: https://stackoverflow.com/questions/20661652/progress-bar-with-httpclient
                var response2 = client.GetAsync(requestPath).GetAwaiter().GetResult();
                var contentFn = response2?.Content?.Headers?.ContentDisposition?.FileName;
                var contentData = response2?.Content?.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                if (contentData == null)
                    throw new PackageContainerException(
                    $"While getting data bytes from {SourceUri.ToString()} via HttpClient " +
                    $"no data-content was responded!");

                // create temp file and write to it
                var givenFn = SourceUri.ToString();
                if (contentFn != null)
                    givenFn = contentFn;
                TempFn = CreateNewTempFn(givenFn, IsFormat);
                File.WriteAllBytes(TempFn, contentData);
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While buffering aasx from {SourceUri.ToString()} via HttpClient " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }

            // open
            try
            {
                Env = new AdminShellPackageEnv(TempFn, indirectLoadSave: false);
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While opening buffered aasx {TempFn} from source {this.ToString()} " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }
    }
}
