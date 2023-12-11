/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AdminShellNS;

namespace AasxPackageLogic.PackageCentral
{
    // see: https://stackoverflow.com/questions/35320238/
    // how-to-display-upload-progress-using-c-sharp-httpclient-postasync

    // for the Proxy issue:
    // https://github.com/dotnet/runtime/issues/25413

    public class ProgressableStreamContent : HttpContent
    {
        private const int _defaultBufferSize = 4024;
        private const int _deltaSizeForProgress = 256 * 1024;

        private byte[] _content;
        private int _bufferSize;
        private PackCntRuntimeOptions _runtimeOptions = null;

        public ProgressableStreamContent(byte[] content, PackCntRuntimeOptions runtimeOptions = null)
            : this(content, _defaultBufferSize, runtimeOptions) { }

        public ProgressableStreamContent(byte[] content, int bufferSize,
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
            return Task.Run(() =>
            {
                var buffer = new Byte[this._bufferSize];
                var uploaded = 0;
                var lastUploaded = 0;

                _runtimeOptions?.ProgressChanged(
                    PackCntRuntimeOptions.Progress.Starting, _content.Length, uploaded);

                using (var _instream = new MemoryStream(_content))
                    while (true)
                    {
                        var length = _instream.Read(buffer, 0, buffer.Length);
                        if (length <= 0)
                            break;

                        uploaded += length;

                        if (uploaded > lastUploaded + _deltaSizeForProgress)
                        {
                            _runtimeOptions?.ProgressChanged(
                                PackCntRuntimeOptions.Progress.Ongoing, _content.Length, uploaded);
                            lastUploaded = uploaded;
                        }

                        stream.Write(buffer, 0, length);
                    }

                _runtimeOptions?.ProgressChanged(
                    PackCntRuntimeOptions.Progress.Final, _content.Length, uploaded);
            });
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _content.Length;
            return true;
        }

    }
}
