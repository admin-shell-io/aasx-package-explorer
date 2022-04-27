/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using AnyUi;
using System.Windows.Media.Imaging;

namespace AasxIntegrationBaseWpf
{
    public class AnyUiBitmapHelper
    {
        public static AnyUiBitmapInfo CreateAnyUiBitmapInfo(BitmapSource source, bool doFreeze = true)
        {
            var res = new AnyUiBitmapInfo();

            if (source != null)
            {
                // take over direct data
                if (doFreeze)
                    source.Freeze();
                res.ImageSource = source;
                res.PixelWidth = source.PixelWidth;
                res.PixelHeight = source.PixelHeight;

                // provide PNG as well
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));

                using (var memStream = new System.IO.MemoryStream())
                {
                    encoder.Save(memStream);
                    res.PngData = memStream.ToArray();
                }
            }

            return res;
        }
    }
}
