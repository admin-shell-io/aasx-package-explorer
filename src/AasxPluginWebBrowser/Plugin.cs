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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AdminShellNS;
using JetBrains.Annotations;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        private AasxPluginWebBrowser.WebBrowserOptions _options = new AasxPluginWebBrowser.WebBrowserOptions();

        private Grid browserGrid = null;
        private CefSharp.Wpf.ChromiumWebBrowser _browser = null;

        public new void InitPlugin(string[] args)
        {
            // start ..
            PluginName = "AasxPluginWebBrowser";
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = AasxPluginWebBrowser.WebBrowserOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<AasxPluginWebBrowser.WebBrowserOptions>(
                        this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    this._options = newOpt;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception when reading default options {1}");
            }
        }

        public new AasxPluginActionDescriptionBase[] ListActions()
        {
            _log.Info("ListActions() called");
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

        public new AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            // can basic helper help to reduce lines of code?
            var help = ActivateActionBasicHelper(action, ref _options, args,
                disableDefaultLicense: true);
            if (help != null)
                return help;

            // rest follows

            if (action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                lic.shortLicense = "The browser functionality is licensed under the cefSharp license (see below).";

                lic.isStandardLicense = true;
                lic.longLicense = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                    "LICENSE.txt", Assembly.GetExecutingAssembly());

                return lic;
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

                this._browser = new CefSharp.Wpf.ChromiumWebBrowser();
                this._browser.Address = url;
                this._browser.InvalidateVisual();

                this.browserGrid.Children.Add(this._browser);
                Grid.SetRow(this._browser, 0);
                Grid.SetColumn(this._browser, 0);

                // TODO (MIHO, 2020-08-02): when dragging the divider between elements tree and browser window,
                // distortions can be seen (diagonal shifting of pixels). Interestingly, this problem was not
                // appearant, when the browser was integrated within the main application and is new for the 
                // plugin approach. Would be great if somebody would find a solution.

                // give object back
                var res = new AasxPluginResultBaseObject();
                res.obj = this.browserGrid;
                return res;
            }

            if (action == "go-to-address" && args != null && args.Length >= 1 && args[0] is string)
            {
                // args
                var url = args[0] as string;

                // check, if possible
                if (this.browserGrid != null && this._browser != null)
                {
                    // try execute
                    this._browser.Address = url;
                    this._browser.InvalidateVisual();
                    this.browserGrid.InvalidateVisual();

                    // indicate OK
                    return new AasxPluginResultBaseObject("OK", true);
                }
            }

            if (action == "set-zoom-level" && args != null && args.Length >= 1 && args[0] is double)
            {
                // args
                var zoom = (double)args[0];

                // check, if possible
                if (this.browserGrid != null && this._browser != null)
                {
                    // try execute
                    this._browser.ZoomLevel = zoom;

                    // indicate OK
                    return new AasxPluginResultBaseObject("OK", true);
                }
            }

            // default
            return null;
        }

    }
}
