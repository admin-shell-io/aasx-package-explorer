/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

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

        public static object CreatePluginTimer(int intervalMs, EventHandler eventHandler)
        {
            if (true)
            {
                var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += eventHandler;
                dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, intervalMs);
                dispatcherTimer.Start();
                return dispatcherTimer;
            }
            else
            {
                // Note: this timer shall work for all sorts of applications?
                // see: https://stackoverflow.com/questions/21041299/c-sharp-dispatchertimer-in-dll-application-never-triggered
                var _timer2 = new System.Timers.Timer(intervalMs);
                _timer2.Elapsed += (s, e) => eventHandler?.Invoke(s, e);
                _timer2.Enabled = true;
                _timer2.Start();
                return _timer2;
            }
        }
    }
}
