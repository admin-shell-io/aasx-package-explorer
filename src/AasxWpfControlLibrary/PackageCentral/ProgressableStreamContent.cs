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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AasxPackageExplorer;
using AdminShellNS;

namespace AasxWpfControlLibrary.PackageCentral
{
    // see: https://stackoverflow.com/questions/35320238/
    // how-to-display-upload-progress-using-c-sharp-httpclient-postasync

    public class ProgressableStreamContent : HttpContent
    {
        private const int _defaultBufferSize = 4024;
        private const int _deltaSizeForProgress = 256 * 1024;

        private Stream _content;
        private int _bufferSize;
        private bool _contentConsumed;
        private PackCntRuntimeOptions _runtimeOptions = null;

        public ProgressableStreamContent(Stream content, PackCntRuntimeOptions runtimeOptions = null)
            : this(content, _defaultBufferSize, runtimeOptions) { }

        public ProgressableStreamContent(Stream content, int bufferSize,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            this._content = content ?? throw new ArgumentNullException(nameof(content));
            this._bufferSize = bufferSize;
            this._runtimeOptions = runtimeOptions;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            PrepareContent();

            return Task.Run(() =>
            {
                var buffer = new Byte[this._bufferSize];
                var uploaded = 0;
                var lastUploaded = 0;

                _runtimeOptions?.ProgressChanged(
                    PackCntRuntimeOptions.Progress.Starting, null, uploaded);

                using (_content)
                    while (true)
                    {
                        var length = _content.Read(buffer, 0, buffer.Length);
                        if (length <= 0)
                            break;

                        uploaded += length;

                        if (uploaded > lastUploaded + _deltaSizeForProgress)
                        {
                            _runtimeOptions?.ProgressChanged(
                                PackCntRuntimeOptions.Progress.Ongoing, null, uploaded);
                            lastUploaded = uploaded;
                        }

                        stream.Write(buffer, 0, length);
                    }

                _runtimeOptions?.ProgressChanged(
                    PackCntRuntimeOptions.Progress.Final, null, uploaded);
            });
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _content.Length;
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _content.Dispose();
            }
            base.Dispose(disposing);
        }


        private void PrepareContent()
        {
            if (_contentConsumed)
            {
                // If the content needs to be written to a target stream a 2nd time, then the stream must support
                // seeking (e.g. a FileStream), otherwise the stream can't be copied a second time to a target 
                // stream (e.g. a NetworkStream).
                if (_content.CanSeek)
                {
                    _content.Position = 0;
                }
                else
                {
                    throw new InvalidOperationException("SR.net_http_content_stream_already_read");
                }
            }

            _contentConsumed = true;
        }
    }
}
