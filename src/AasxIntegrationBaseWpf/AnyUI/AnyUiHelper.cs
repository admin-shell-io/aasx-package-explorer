/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

// ReSharper disable UnusedType.Global

namespace AasxIntegrationBaseWpf
{
    public static class AnyUiHelper
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

        public static object CreatePluginTimer(int intervalMs, EventHandler eventHandler)
        {
            // see: https://stackoverflow.com/questions/5143599/detecting-whether-on-ui-thread-in-wpf-and-winforms
            var dispatcher = System.Windows.Threading.Dispatcher.FromThread(System.Threading.Thread.CurrentThread);
            if (dispatcher != null)
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

        public static void TakeOverGridPosition(
            System.Windows.UIElement uieDst,
            System.Windows.UIElement uieSrc)
        {
            Grid.SetRow(uieDst, Grid.GetRow(uieSrc));
            Grid.SetRowSpan(uieDst, Grid.GetRowSpan(uieSrc));
            Grid.SetColumn(uieDst, Grid.GetColumn(uieSrc));
            Grid.SetColumnSpan(uieDst, Grid.GetColumnSpan(uieSrc));
        }
    }
}
