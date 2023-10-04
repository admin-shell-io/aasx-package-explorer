/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// In order to use this define, a reference to System.Drawing.Common in required
#define UseMagickNet

#if UseMagickNet

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Xml.Schema;
using System.Xml;
using AdminShellNS;
using AnyUi;
using ImageMagick;

namespace AasxIntegrationBaseGdi
{
    /// <summary>
    /// This class exists multiple time in source code, controlled by #defines.
    /// Only one #define shall be given.
    /// This class understands the term GDI to be "graphics dependent inteface" ;-)
    /// </summary>
    public static class AnyUiGdiHelper
    {
        public static AnyUiBitmapInfo CreateAnyUiBitmapInfo(MagickImage source, bool doFreeze = true)
        {
            var res = new AnyUiBitmapInfo();

            if (source != null)
            {
                // take over direct data
                res.ImageSource = source;
                res.PixelWidth = source.Width;
                res.PixelHeight = source.Height;

                // provide PNG as well
                using (var cloneImage = source.Clone())
                {
                    cloneImage.Format = MagickFormat.Png;
                    res.PngData = cloneImage.ToByteArray();
                }
            }

            return res;
        }

        public static AnyUiBitmapInfo CreateAnyUiBitmapInfo(string path)
        {
            var bi = new MagickImage(path);
            return CreateAnyUiBitmapInfo(bi);
        }

        public static AnyUiBitmapInfo CreateAnyUiBitmapFromResource(string path,
            Assembly assembly = null)
        {
            try
            {
                if (assembly == null)
                    assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(path))
                {
                    if (stream == null)
                        return null;
                    var bi = new MagickImage(stream);
                    return CreateAnyUiBitmapInfo(bi);
                }
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            return null;
        }

        public static AnyUiBitmapInfo LoadBitmapInfoFromPackage(AdminShellPackageEnv package, string path)
        {
            if (package == null || path == null)
                return null;

            try
            {
                var thumbStream = package.GetLocalStreamFromPackage(path);
                if (thumbStream == null)
                    return null;

                // load image
                var bi = new MagickImage(thumbStream);
                var binfo = CreateAnyUiBitmapInfo(bi);
                thumbStream.Close();

                // give this back
                return binfo;
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            return null;
        }

        // TODO (MIHO, 2023-02-23): make the whole thing async!!

        public static AnyUiBitmapInfo MakePreviewFromPackageOrUrl(
            AdminShellPackageEnv package, string path,
            double dpi = 75)
        {
            if (path == null)
                return null;

            AnyUiBitmapInfo res = null;

            try
            {
                System.IO.Stream thumbStream = null;
                if (true /*= package?.IsLocalFile(path)*/)
                    thumbStream = package.GetLocalStreamFromPackage(path);
                else
                {
                    // try download
#if __old
                    var wc = new WebClient();                    
                    thumbStream = wc.OpenRead(path);
#else
                    // dead-csharp off
                    /*
                     * 
                    // upgrade to HttpClient and follow re-directs
                    var hc = new HttpClient();
                    var response = hc.GetAsync(path).GetAwaiter().GetResult();

                    // if you call response.EnsureSuccessStatusCode here it will throw an exception
                    if (response.StatusCode == HttpStatusCode.Moved
                        || response.StatusCode == HttpStatusCode.Found)
                    {
                        var location = response.Headers.Location;
                        response = hc.GetAsync(location).GetAwaiter().GetResult();
                    }

                    response.EnsureSuccessStatusCode();
                    thumbStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                    */
                    // dead-csharp on
#endif
                }

                if (thumbStream == null)
                    return null;

                using (var images = new MagickImageCollection())
                {
                    var settings = new MagickReadSettings();
                    settings.Density = new Density(dpi);
                    settings.FrameIndex = 0; // First page
                    settings.FrameCount = 1; // Number of pages

                    // Read only the first page of the pdf file
                    images.Read(thumbStream, settings);

                    if (images.Count > 0 && images[0] is MagickImage img)
                    {
                        res = CreateAnyUiBitmapInfo(img);
                    }
                }

                thumbStream.Close();
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            return res;
        }
    }
}

#endif
