/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPackageLogic;
using Microsoft.JSInterop;

namespace BlazorUI.Utils
{
    public static class BlazorUtils
    {
        public static async Task DisplayOrDownloadFile(IJSRuntime runtime, string fn, string mimeType = null)
        {
            // access
            if (runtime == null || !fn.HasContent())
                return;

            // safe
            try
            {

                // extension of fn?
                var ext = System.IO.Path.GetExtension(fn).ToLower().Trim();

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types/Common_types
                var browserHandles = new[]
                {
                    ".html", "text/html",
                    ".htm", "text/html",
                    ".jpeg", "image/jpeg",
                    ".jpg", "image/jpeg",
                    ".png", "image/png",
                    ".bmp", "image/bmp",
                    ".gif", "image/gif",
                    ".tiff", "image/tiff",
                    ".tif", "image/tif",
                    ".svg", "image/svg+xml",
                    ".mpg", "video/mpg",
                    ".mp4", "video/mp4",
                    ".mp3", "video/mp3",
                    ".pdf", "application/pdf",
                };

                // find? -> display
                var display = false;
                for (int i = 0; i < browserHandles.Length; i += 2)
                {
                    // know filename?
                    if (browserHandles[i + 0] == ext)
                    {
                        display = true;
                        if (!mimeType.HasContent())
                            mimeType = browserHandles[i + 1];
                    }
                    else
                    // known mimetype also OK
                    if (mimeType != null && mimeType.ToLower().Trim() == browserHandles[i + 1])
                        display = true;
                }

                // prepare file
                var file = await System.IO.File.ReadAllBytesAsync(fn);
                var fileName = System.IO.Path.GetFileName(fn);
                // Send the data to JS to actually download the file

                if (display)
                    await runtime.InvokeVoidAsync("BlazorDisplayFile", fileName, mimeType, file);
                else
                    await runtime.InvokeVoidAsync("BlazorDownloadFile", fileName, mimeType, file);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "Display or download file");
            }
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

    }
}
