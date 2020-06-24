﻿using AdminShellNS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The Newtonsoft.JSON serialization is licensed under the MIT License (MIT).

The Microsoft Microsoft Automatic Graph Layout, MSAGL, is licensed under the MIT license (MIT).
*/

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : IAasxPluginInterface
    {
        private LogInstance Log = new LogInstance();
        private PluginEventStack eventStack = new PluginEventStack();
        private AasxPluginWebBrowser.WebBrowserOptions options = new AasxPluginWebBrowser.WebBrowserOptions();

        private Grid browserGrid = null;
        private CefSharp.Wpf.ChromiumWebBrowser theBrowser = null;

        public string GetPluginName()
        {
            return "AasxPluginWebBrowser";
        }

        public void InitPlugin(string[] args)
        {
            // start ..
            Log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            options = AasxPluginWebBrowser.WebBrowserOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<AasxPluginWebBrowser.WebBrowserOptions>(
                        this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    this.options = newOpt;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception when reading default options {1}");
            }
        }

        public object CheckForLogMessage()
        {
            return Log.PopLastShortTermPrint();
        }

        public AasxPluginActionDescriptionBase[] ListActions()
        {
            Log.Info("ListActions() called");
            var res = new List<AasxPluginActionDescriptionBase>();
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "set-json-options", "Sets plugin-options according to provided JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-json-options", "Gets plugin-options as a JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-licenses", "Reports about used licenses."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-events", "Pops and returns the earliest event from the event stack."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-browser-grid", "Returns a new instance of a Grid with a Chromium web-browser control."));
            res.Add(new AasxPluginActionDescriptionBase("set-zoom-level", "Set a normalizd (1.0-based) zoom leve."));
            res.Add(new AasxPluginActionDescriptionBase("go-to-address", "Will go to a web address."));
            return res.ToArray();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt = Newtonsoft.Json.JsonConvert.DeserializeObject<AasxPluginWebBrowser.WebBrowserOptions>(
                    (args[0] as string));
                if (newOpt != null)
                    this.options = newOpt;
            }

            if (action == "get-json-options")
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(
                    this.options, Newtonsoft.Json.Formatting.Indented);
                return new AasxPluginResultBaseObject("OK", json);
            }

            if (action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                lic.shortLicense = "The browser functionality is licensed under the cefSharp license (see below).";

                lic.longLicense = "";
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    "AasxPluginWebBrowser.Resources.LICENSE.txt"))
                {
                    if (stream != null)
                    {
                        TextReader tr = new StreamReader(stream);
                        lic.longLicense += tr.ReadToEnd();
                    }
                }

                return lic;
            }

            if (action == "get-events" && this.eventStack != null)
            {
                // try access
                return this.eventStack.PopEvent();
            }

            if (action == "get-browser-grid" && args != null && args.Length >= 1 && args[0] is string)
            {
                // args
                var url = args[0] as string;

                // build visual
                this.browserGrid = new Grid();

                this.browserGrid.RowDefinitions.Add(
                    new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Star) });
                this.browserGrid.ColumnDefinitions.Add(
                    new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });

                this.theBrowser = new CefSharp.Wpf.ChromiumWebBrowser();
                this.theBrowser.Address = url;
                this.theBrowser.InvalidateVisual();

                this.browserGrid.Children.Add(this.theBrowser);
                Grid.SetRow(this.theBrowser, 0);
                Grid.SetColumn(this.theBrowser, 0);

                // give object back
                var res = new AasxPluginResultBaseObject();
                res.obj = this.browserGrid;
                return res;
            }

            if (action == "go-to-address" && args != null && args.Length >= 1 && args[0] is string)
            {
                // args
                var url = args[0] as string;
                Log.Info("AasxPluginWebBrowser go to {0}", url);

                // check, if possible
                if (this.browserGrid != null && this.theBrowser != null)
                {
                    // try execute
                    this.theBrowser.Address = url;
                    this.theBrowser.InvalidateVisual();
                    this.browserGrid.InvalidateVisual();

                    // indicate OK
                    return new AasxPluginResultBaseObject("OK", true);
                }
            }

            if (action == "gset-zoom-level" && args != null && args.Length >= 1 && args[0] is double)
            {
                // args
                var zoom = (double)args[0];

                // check, if possible
                if (this.browserGrid != null && this.theBrowser != null)
                {
                    // try execute
                    this.theBrowser.ZoomLevel = zoom;

                    // indicate OK
                    return new AasxPluginResultBaseObject("OK", true);
                }
            }

            // default
            return null;
        }

    }
}
