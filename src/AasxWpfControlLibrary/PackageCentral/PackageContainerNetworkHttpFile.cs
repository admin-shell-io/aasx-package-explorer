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
using System.Threading;

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

        public PackageContainerNetworkHttpFile(
            PackageCentral packageCentral,
            string sourceFn)
            : base (packageCentral)
        {
            Init();
            SetNewSourceFn(sourceFn);
        }

        // nice discussion on how to name factory-like methods

        public static async Task<PackageContainerNetworkHttpFile> CreateAsync(
            PackageCentral packageCentral,
            string sourceFn, PackageContainerOptionsBase containerOptions = null,
            PackageContainerRuntimeOptions runtimeOptions = null)
        {
            var res = new PackageContainerNetworkHttpFile(packageCentral, sourceFn);

            if (containerOptions != null)
                res.ContainerOptions = containerOptions;
            if (true == res.ContainerOptions?.LoadResident)
                await res.InternalLoadFromSourceAsync(runtimeOptions);
            return res;
        }

        //
        // Mechanics
        //

        public override string Filename { get { return SourceUri.ToString(); } }

        private void Init()
        {
            this.LoadFromSource = this.InternalLoadFromSourceSync;
            this.SaveAsToSource = this.InternalSaveToSourceSync;
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

        private async Task DownloadFromSource(Uri sourceUri,
            PackageContainerRuntimeOptions runtimeOptions = null)
        {
            // read via HttpClient (uses standard proxies)
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", "application/aas");
            client.BaseAddress = new Uri(sourceUri.GetLeftPart(UriPartial.Authority));
            var requestPath = sourceUri.PathAndQuery;

            // Log
            runtimeOptions?.Log?.Info($"HttpClient() with base-address {client.BaseAddress} " +
                $"and request {requestPath} .. ");

            // get response?
            using (var response = await client.GetAsync(requestPath,
                HttpCompletionOption.ResponseHeadersRead))
            {
                var contentLength = response.Content.Headers.ContentLength;
                var contentFn = response.Content.Headers.ContentDisposition?.FileName;

                // log
                runtimeOptions?.Log?.Info($".. response with header-content-len {contentLength} " +
                    $"and file-name {contentFn} ..");

                var contentStream = await response?.Content?.ReadAsStreamAsync();
                if (contentStream == null)
                    throw new PackageContainerException(
                    $"While getting data bytes from {SourceUri.ToString()} via HttpClient " +
                    $"no data-content was responded!");

                // create temp file and write to it
                var givenFn = SourceUri.ToString();
                if (contentFn != null)
                    givenFn = contentFn;
                TempFn = CreateNewTempFn(givenFn, IsFormat);
                runtimeOptions?.Log?.Info($".. downloading to temp-file {TempFn}");

                using (var file = new FileStream(TempFn, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // copy with progress
                    var bufferSize = 512*1024;
                    var buffer = new byte[bufferSize];
                    long totalBytesRead = 0;
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length,
                        default(CancellationToken)).ConfigureAwait(false)) != 0)
                    {
                        await file.WriteAsync(buffer, 0, bytesRead,
                            default(CancellationToken)).ConfigureAwait(false);
                                                                      
                        totalBytesRead += bytesRead;
                        runtimeOptions?.ProgressChanged?.Invoke(contentLength, totalBytesRead);
                    }

                    // assume bytes read to be total bytes
                    runtimeOptions?.ProgressChanged?.Invoke(totalBytesRead, totalBytesRead);

                    // log                
                    runtimeOptions?.Log?.Info($".. download done with {totalBytesRead} bytes read!");
                }

            }
        }

        private void InternalLoadFromSourceSync(PackageContainerRuntimeOptions runtimeOptions = null)
        {
            var task = Task.Run(() => InternalLoadFromSourceAsync(runtimeOptions));
            task.Wait();
        }

        private async Task InternalLoadFromSourceAsync(
            PackageContainerRuntimeOptions runtimeOptions = null)
        {
            // buffer to temp file
            try
            {
                await DownloadFromSource(SourceUri, runtimeOptions);
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
            PackageContainerRuntimeOptions runtimeOptions = null)
        {
            // read via HttpClient (uses standard proxies)
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(serverUri.GetLeftPart(UriPartial.Authority));
            var requestPath = serverUri.PathAndQuery;

            // Log
            runtimeOptions?.Log?.Info($"HttpClient() with base-address {client.BaseAddress} " +
                $"and request {requestPath} .. ");

            // make base64
            var ba = File.ReadAllBytes(copyFn);
            var base64 = Convert.ToBase64String(ba);
            var msBase64 = new MemoryStream(Encoding.UTF8.GetBytes(base64 ?? ""));

            // var data = new StringContent(base64, Encoding.UTF8, "application/base64");
            var download = new Download();
            download.RuntimeOptions = runtimeOptions;
            var data = new ProgressableStreamContent(msBase64, download);

            // get response?
            using (var response = await client.PutAsync(requestPath, data))
            {
                await response.Content.ReadAsStringAsync();
            }
        }

        private void InternalSaveToSourceSync(string saveAsNewFileName = null,
            AdminShellPackageEnv.SerializationFormat prefFmt = AdminShellPackageEnv.SerializationFormat.None,
            PackageContainerRuntimeOptions runtimeOptions = null)
        {
            var task = Task.Run(() => InternalSaveToSourceAsync(saveAsNewFileName, prefFmt, runtimeOptions));
            task.Wait();
        }

        protected async Task InternalSaveToSourceAsync(string saveAsNewFileName = null,
            AdminShellPackageEnv.SerializationFormat prefFmt = AdminShellPackageEnv.SerializationFormat.None,
            PackageContainerRuntimeOptions runtimeOptions = null)
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
                    Env.TemporarilySaveCloseAndReOpenPackage(() => {
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
                await UploadToServerAsync(copyFn, SourceUri, runtimeOptions);
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While uploading to {SourceUri.ToString()} from temp-file {copyFn} " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }
    }

    // see: https://stackoverflow.com/questions/35320238/how-to-display-upload-progress-using-c-sharp-httpclient-postasync

    public enum DownloadState { Idle, PendingUpload, Uploading, PendingResponse }

    internal class Download
    {
        public PackageContainerRuntimeOptions RuntimeOptions;
        public void ChangeState(DownloadState state)
        {
            ;
        }
    }

    internal class ProgressableStreamContent : HttpContent
    {
        private const int defaultBufferSize = 4096;

        private Stream content;
        private int bufferSize;
        private bool contentConsumed;
        private Download downloader;

        public ProgressableStreamContent(Stream content, Download downloader) : this(content, defaultBufferSize, downloader) { }

        public ProgressableStreamContent(Stream content, int bufferSize, Download downloader)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException("bufferSize");
            }

            this.content = content;
            this.bufferSize = bufferSize;
            this.downloader = downloader;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            PrepareContent();

            return Task.Run(() =>
            {
                var buffer = new Byte[this.bufferSize];
                var size = content.Length;
                var uploaded = 0;

                downloader.ChangeState(DownloadState.PendingUpload);

                using (content) while (true)
                    {
                        var length = content.Read(buffer, 0, buffer.Length);
                        if (length <= 0) break;

                        uploaded += length;
                        downloader.RuntimeOptions?.ProgressChanged(null, uploaded);

                        stream.Write(buffer, 0, length);

                        downloader.ChangeState(DownloadState.Uploading);
                    }

                downloader.ChangeState(DownloadState.PendingResponse);
            });
        }

        protected override bool TryComputeLength(out long length)
        {
            length = content.Length;
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                content.Dispose();
            }
            base.Dispose(disposing);
        }


        private void PrepareContent()
        {
            if (contentConsumed)
            {
                // If the content needs to be written to a target stream a 2nd time, then the stream must support
                // seeking (e.g. a FileStream), otherwise the stream can't be copied a second time to a target 
                // stream (e.g. a NetworkStream).
                if (content.CanSeek)
                {
                    content.Position = 0;
                }
                else
                {
                    throw new InvalidOperationException("SR.net_http_content_stream_already_read");
                }
            }

            contentConsumed = true;
        }
    }
}
