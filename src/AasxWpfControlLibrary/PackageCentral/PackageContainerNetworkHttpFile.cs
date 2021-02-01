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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AasxOpenIdClient;
using AasxPackageExplorer;
using AdminShellNS;
using IdentityModel.Client;
using Newtonsoft.Json;

namespace AasxWpfControlLibrary.PackageCentral
{
    /// <summary>
    /// This container represents a file, which is retrieved by an network/ HTTP commands and
    /// buffered locally.
    /// </summary>
    public class PackageContainerNetworkHttpFile : PackageContainerBuffered
    {
        /// <summary>
        /// Location of the Container in a certain storage container, e.g. a local or network based
        /// repository. In this implementation, the Location refers to a HTTP network ressource.
        /// </summary>
        public override string Location
        {
            get { return _location; }
            set { SetNewLocation(value); OnPropertyChanged("InfoLocation"); }
        }
        //
        // Constructors
        //

        public PackageContainerNetworkHttpFile()
        {
            Init();
        }

        public PackageContainerNetworkHttpFile(
            PackageCentral packageCentral,
            string sourceFn, PackageContainerOptionsBase containerOptions = null)
            : base(packageCentral)
        {
            Init();
            SetNewLocation(sourceFn);
            if (containerOptions != null)
                ContainerOptions = containerOptions;
        }

        public PackageContainerNetworkHttpFile(CopyMode mode, PackageContainerBase other,
            PackageCentral packageCentral = null,
            string sourceUri = null, PackageContainerOptionsBase containerOptions = null)
            : base(mode, other, packageCentral)
        {
            if ((mode & CopyMode.Serialized) > 0 && other != null)
            {
            }
            if ((mode & CopyMode.BusinessData) > 0 && other is PackageContainerNetworkHttpFile o)
            {
                sourceUri = o.Location;
            }
            if (sourceUri != null)
                SetNewLocation(sourceUri);
            if (containerOptions != null)
                ContainerOptions = containerOptions;
        }


        public static async Task<PackageContainerNetworkHttpFile> CreateAndLoadAsync(
            PackageCentral packageCentral,
            string location,
            string fullItemLocation,
            bool overrideLoadResident,
            PackageContainerBase takeOver = null,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            var res = new PackageContainerNetworkHttpFile(CopyMode.Serialized, takeOver,
                packageCentral, location, containerOptions);

            if (overrideLoadResident || true == res.ContainerOptions?.LoadResident)
                await res.LoadFromSourceAsync(fullItemLocation, runtimeOptions);

            return res;
        }

        //
        // Mechanics
        //

        private void Init()
        {
        }

        private void SetNewLocation(string sourceUri)
        {
            _location = sourceUri;
            IsFormat = Format.AASX;
            IndirectLoadSave = true;
        }

        public override string ToString()
        {
            return "HTTP file: " + Location;
        }

        private async Task DownloadFromSource(Uri sourceUri,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            // read via HttpClient (uses standard proxies)
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            handler.AllowAutoRedirect = false;

            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("Accept", "application/aas");
            client.BaseAddress = new Uri(sourceUri.GetLeftPart(UriPartial.Authority));
            var requestPath = sourceUri.PathAndQuery;

            // Log
            runtimeOptions?.Log?.Info($"HttpClient() with base-address {client.BaseAddress} " +
                $"and request {requestPath} .. ");

            if (OpenIDClient.token != "")
                client.SetBearerToken(OpenIDClient.token);

            bool repeat = true;

            while (repeat)
            {
                // get response?
                var response = await client.GetAsync(requestPath,
                    HttpCompletionOption.ResponseHeadersRead);

                if (response.StatusCode == System.Net.HttpStatusCode.TemporaryRedirect)
                {
                    string redirectUrl = response.Headers.Location.ToString();
                    string[] splitResult = redirectUrl.Split(new string[] { "?" },
                        StringSplitOptions.RemoveEmptyEntries);
                    Console.WriteLine("Redirect to:" + splitResult[0]);
                    OpenIDClient.authServer = splitResult[0];

                    runtimeOptions?.Log?.Info($".. authentication at auth server {OpenIDClient.authServer} needed");

                    var response2 = await OpenIDClient.RequestTokenAsync(null);
                    OpenIDClient.token = response2.AccessToken;
                    client.SetBearerToken(OpenIDClient.token);

                    repeat = true;
                    continue;

                    // throw new PackageContainerException($".. authentication at auth server {authServer} needed");
                }

                repeat = false;

                var contentLength = response.Content.Headers.ContentLength;
                var contentFn = response.Content.Headers.ContentDisposition?.FileName;

                // log
                runtimeOptions?.Log?.Info($".. response with header-content-len {contentLength} " +
                    $"and file-name {contentFn} ..");

                var contentStream = await response?.Content?.ReadAsStreamAsync();
                if (contentStream == null)
                    throw new PackageContainerException(
                    $"While getting data bytes from {Location} via HttpClient " +
                    $"no data-content was responded!");

                // create temp file and write to it
                var givenFn = Location;
                if (contentFn != null)
                    givenFn = contentFn;
                TempFn = CreateNewTempFn(givenFn, IsFormat);
                runtimeOptions?.Log?.Info($".. connecting to server with base-address {client.BaseAddress} " +
                    $"and request {requestPath} .. ");

                using (var file = new FileStream(TempFn, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // copy with progress
                    var bufferSize = 4024;
                    var deltaSize = 512 * 1024;
                    var buffer = new byte[bufferSize];
                    long totalBytesRead = 0;
                    long lastBytesRead = 0;
                    int bytesRead;

                    runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.Starting,
                            contentLength, totalBytesRead);

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length,
                        default(CancellationToken)).ConfigureAwait(false)) != 0)
                    {
                        await file.WriteAsync(buffer, 0, bytesRead,
                            default(CancellationToken)).ConfigureAwait(false);

                        totalBytesRead += bytesRead;

                        if (totalBytesRead > lastBytesRead + deltaSize)
                        {
                            runtimeOptions?.Log?.Info($".. downloading to temp-file {TempFn}");
                            runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.Ongoing,
                                contentLength, totalBytesRead);
                            lastBytesRead = totalBytesRead;
                        }
                    }

                    // assume bytes read to be total bytes
                    runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.Final,
                        totalBytesRead, totalBytesRead);

                    // log                
                    runtimeOptions?.Log?.Info($".. download done with {totalBytesRead} bytes read!");
                }
            }
        }

        public override async Task LoadFromSourceAsync(
            string fullItemLocation,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            // buffer to temp file
            try
            {
                await DownloadFromSource(new Uri(fullItemLocation), runtimeOptions);
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While buffering aasx from {Location} full-location {fullItemLocation} via HttpClient " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }

            // open
            try
            {
                Env = new AdminShellPackageEnv(TempFn, indirectLoadSave: false);
                runtimeOptions?.Log?.Info($".. successfully opened as AASX environment: {Env?.AasEnv?.ToString()}");
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While opening buffered aasx {TempFn} from source {this.ToString()} " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        private async Task UploadToServerAsync(string copyFn, Uri serverUri,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            // read via HttpClient (uses standard proxies)
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;

            var client = new HttpClient(handler);

            if (OpenIDClient.token != "")
                client.SetBearerToken(OpenIDClient.token);

            // BEGIN Workaround behind some proxies
            // Stream is sent twice, if proxy-authorization header is not set
            string proxyFile = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/proxy.dat";
            string username = "";
            string password = "";
            if (File.Exists(proxyFile))
            {
                using (StreamReader sr = new StreamReader(proxyFile))
                {
                    // ReSharper disable MethodHasAsyncOverload
                    sr.ReadLine();
                    username = sr.ReadLine();
                    password = sr.ReadLine();
                    // ReSharper enable MethodHasAsyncOverload
                }
            }
            if (username != "" && password != "")
            {
                var authToken = Encoding.ASCII.GetBytes(username + ":" + password);
                client.DefaultRequestHeaders.ProxyAuthorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(authToken));
            }
            // END Workaround behind some proxies

            client.BaseAddress = new Uri(serverUri.GetLeftPart(UriPartial.Authority));
            var requestPath = serverUri.PathAndQuery;

            // Log
            runtimeOptions?.Log?.Info($"HttpClient() with base-address {client.BaseAddress} " +
                $"and request {requestPath} .. ");

            // make base64
            var ba = File.ReadAllBytes(copyFn);
            var base64 = Convert.ToBase64String(ba);
            //// var msBase64 = new MemoryStream(Encoding.UTF8.GetBytes(base64));

            // customised HttpContent to track progress
            var data = new ProgressableStreamContent(Encoding.UTF8.GetBytes(base64), runtimeOptions);

            // get response?
            using (var response = await client.PutAsync(requestPath, data))
            {
                await response.Content.ReadAsStringAsync();
            }
        }

        public override async Task SaveToSourceAsync(string saveAsNewFileName = null,
            AdminShellPackageEnv.SerializationFormat prefFmt = AdminShellPackageEnv.SerializationFormat.None,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            // check extension
            if (IsFormat == Format.Unknown)
                throw new PackageContainerException(
                    "While saving aasx, unknown file format/ extension was encountered!");

            // check open package
            if (Env == null || !Env.IsOpen)
            {
                Env = null;
                throw new PackageContainerException(
                    "While saving aasx, package was indeed not existng or not open!");
            }

            // will use an file-copy for upload
            var copyFn = CreateNewTempFn(Env.Filename, IsFormat);

            // divert on indirect load/ save, to have dedicated try&catch
            if (IndirectLoadSave)
            {
                // do a close, execute and re-open cycle
                try
                {
                    Env.TemporarilySaveCloseAndReOpenPackage(() =>
                    {
                        System.IO.File.Copy(Env.Filename, copyFn, overwrite: true);
                    });
                }
                catch (Exception ex)
                {
                    throw new PackageContainerException(
                        $"While indirect-saving aasx to temp-file {copyFn} " +
                        $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                }
            }
            else
            {
                // just save as a copy
                try
                {
                    Env.SaveAs(copyFn, saveOnlyCopy: true);
                }
                catch (Exception ex)
                {
                    throw new PackageContainerException(
                        $"While direct-saving aasx to temp-file {copyFn} " +
                        $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                }
            }

            // now, try to upload this
            try
            {
                await UploadToServerAsync(copyFn, new Uri(Location), runtimeOptions);
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While uploading to {Location} from temp-file {copyFn} " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }
    }

}
