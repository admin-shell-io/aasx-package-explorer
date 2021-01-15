/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AasxIntegrationBase;

namespace AasxPackageExplorer
{
    /// <summary>
    /// Goal: abstract on the use of a FakeBrowser or the CefSharp Browser
    /// </summary>
    public class BrowserContainer
    {

        #region Members
        //=============

        private static Plugins.PluginInstance browserPlugin = null;
        private static Grid theOnscreenBrowser = null;
        private string browserHandlesFiles = ".html .htm .jpeg .jpg .png .bmp .pdf .xml .txt .md *";

        private FakeBrowser theFallbackBrowser = null;
        private string fallbackBrowserHandlesFiles = ".jpeg .jpg .png .bmp";

        public UIElement BrowserControl
        {
            get
            {
                if (theOnscreenBrowser != null)
                    return theOnscreenBrowser;
                return theFallbackBrowser;
            }
        }

        private static double cachedZoomLevel = 1.0;
        public double ZoomLevel
        {
            get { return cachedZoomLevel; }
            set
            {
                // cache
                cachedZoomLevel = value;

                // send out the request
                if (BrowserContainer.browserPlugin != null && BrowserContainer.theOnscreenBrowser != null)
                {
                    BrowserContainer.browserPlugin.InvokeAction("set-zoom-level", value);
                }
            }
        }

        private bool useAlwaysInternalBrowser = false;


        #endregion
        #region Constructors
        //==================

        public void Start(string startUrl, bool useAlwaysInternalBrowser, bool useOffscreen = false)
        {
            // due to the nature of the plug-in, this forms as SINGLETON
            if (BrowserContainer.browserPlugin != null && BrowserContainer.theOnscreenBrowser != null)
                // always fine
                return;

            var pluginName = "AasxPluginWebBrowser";
            var actionName = "get-browser-grid";
            BrowserContainer.browserPlugin = Plugins.FindPluginInstance(pluginName);

            if (BrowserContainer.browserPlugin == null || !BrowserContainer.browserPlugin.HasAction(actionName))
            {
                // ok, fallback
                BrowserContainer.browserPlugin = null;
                BrowserContainer.theOnscreenBrowser = null;

                this.theFallbackBrowser = new FakeBrowser();
                this.browserHandlesFiles = fallbackBrowserHandlesFiles;
                GoToContentBrowserAddress(startUrl);
            }
            else
            {
                // setup
                var res = BrowserContainer.browserPlugin.InvokeAction(actionName, startUrl);
                if (res != null && res is AasxPluginResultBaseObject)
                {
                    BrowserContainer.theOnscreenBrowser = (res as AasxPluginResultBaseObject).obj as Grid;
                }
            }

            this.useAlwaysInternalBrowser = useAlwaysInternalBrowser;
        }

        public void GoToContentBrowserAddress(string url)
        {
            if (BrowserContainer.browserPlugin != null && BrowserContainer.theOnscreenBrowser != null)
            {
                BrowserContainer.browserPlugin.InvokeAction("go-to-address", url);
            }
            else
            {
                this.theFallbackBrowser.Address = url;
                this.theFallbackBrowser.InvalidateVisual();
            }
        }

        #endregion
        #region Functions to the outside
        //==============================

        public bool CanHandleFileNameExtension(string fn)
        {
            // prepare extension
            var ext = System.IO.Path.GetExtension(fn.ToLower());
            if (ext == "")
                ext = "*";

            // check
            if (browserHandlesFiles.Contains(ext) || this.useAlwaysInternalBrowser)
                return true;
            else
                return false;
        }

        #endregion
    }
}
