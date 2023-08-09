/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// In order to use this define, a reference to System.Drawing.Common in required
// #define UseSysDrawComm

#if UseSysDrawComm

using System;
using AdminShellNS;
using AnyUi;

namespace AasxIntegrationBaseGdi
{
    public class AnyUiGdiHelper
    {
        public static AnyUiBitmapInfo CreateAnyUiBitmapInfo(System.Drawing.Image source, bool doFreeze = true)
        {
            var res = new AnyUiBitmapInfo();

            if (source != null)
            {
                // take over direct data
                res.ImageSource = source;
                res.PixelWidth = source.Width;
                res.PixelHeight = source.Height;

                // provide PNG as well
                using (var memStream = new System.IO.MemoryStream())
                {
                    // Console.WriteLine($"4|Try save as png");
                    source.Save(memStream, System.Drawing.Imaging.ImageFormat.Png);
                    res.PngData = memStream.ToArray();
                }
            }

            return res;
        }

        public static AnyUiBitmapInfo CreateAnyUiBitmapInfo(string path, bool doFreeze = true)
        {
            // Console.WriteLine($"2|Try load file {path}");
            var bi = System.Drawing.Image.FromFile(path);
            // Console.WriteLine($"3|Has image width {bi.Width}");
            return CreateAnyUiBitmapInfo(bi);
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
                var bi = System.Drawing.Image.FromStream(thumbStream);
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

        public static AnyUiBitmapInfo MakePreviewFromPackageOrUrl(
            AdminShellPackageEnv package, string path,
            double dpi = 75)
        {
            // makes only sense for ImageMagick
            return null;
        }


    }
}

#endif
