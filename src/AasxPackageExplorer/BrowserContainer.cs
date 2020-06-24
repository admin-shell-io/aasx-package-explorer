using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Drawing;
using System.Windows.Controls;
using AasxIntegrationBase;

#if WITHCEF
using CefSharp;
using CefSharp.Wpf;
using CefSharp.Internals;
using CefSharp.Event;
using CefSharp.Wpf.Internals;
using CefSharp.Wpf.Rendering;
using CefSharp.OffScreen;
using CefSharp.RenderProcess;
#endif

namespace AasxPackageExplorer
{
    /// <summary>
    /// Goal: abstract on the use of a FakeBrowser or the CefSharp Browser
    /// </summary>
    public class BrowserContainer
    {

        #region Members
        //=============

#if WITHCEF
        private CefSharp.Wpf.ChromiumWebBrowser theOnscreenBrowser = null;
        private CefSharp.OffScreen.ChromiumWebBrowser theOffscreenBrowser = null;
        private string browserHandlesFiles = ".jpeg .jpg .png .bmp .pdf .xml .txt *";
        public CefSharp.Wpf.ChromiumWebBrowser BrowserControl { get { return theOnscreenBrowser; } }

        public double ZoomLevel
        {
            get { return (theOnscreenBrowser == null) ? 0.0 : theOnscreenBrowser.ZoomLevel; }
            set { if (theOnscreenBrowser != null) theOnscreenBrowser.ZoomLevel = value; }
        }
#elif WITHBROWSERPLUGIN
        private static Plugins.PluginInstance browserPlugin = null;
        private static Grid theOnscreenBrowser = null;
        private string browserHandlesFiles = ".jpeg .jpg .png .bmp .pdf .xml .txt *";

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
#else
        private FakeBrowser theBrowser = null;
        private string browserHandlesFiles = ".jpeg .jpg .png .bmp";
        public FakeBrowser BrowserControl { get { return theBrowser; } }

        public double ZoomLevel
        {
            get { return 1.0; }
            set { ; }
        }
#endif

        private bool useAlwaysInternalBrowser = false;
        private bool useOffscreen = false;



        #endregion
        #region Constructors
        //==================

        public void Start(string startUrl, bool useAlwaysInternalBrowser, bool useOffscreen = false)
        {
#if WITHFAKEBROWSER
            this.theBrowser = new FakeBrowser();
            GoToContentBrowserAddress(startUrl);
#endif

#if WITHCEF
            if (!useOffscreen)
            {
                this.theOnscreenBrowser = new CefSharp.Wpf.ChromiumWebBrowser();
                GoToContentBrowserAddress(startUrl);
            }

#endif

#if WITHBROWSERPLUGIN

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
#endif

            this.useAlwaysInternalBrowser = useAlwaysInternalBrowser;
            this.useOffscreen = useOffscreen;
        }

        public void GoToContentBrowserAddress(string url)
        {
#if WITHFAKEBROWSER
            if (this.theBrowser != null)
            {
                this.theBrowser.Address = url;
                this.theBrowser.InvalidateVisual();
            }
#endif
#if WITHCEF
            if (theOnscreenBrowser == null)
                return;

            try
            {
                theOnscreenBrowser.Address = url;
                theOnscreenBrowser.InvalidateVisual();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Showing content page " + url);
            }
#endif
#if WITHBROWSERPLUGIN
            if (BrowserContainer.browserPlugin != null && BrowserContainer.theOnscreenBrowser != null)
            {
                BrowserContainer.browserPlugin.InvokeAction("go-to-address", url);
            }
            else
            {
                this.theFallbackBrowser.Address = url;
                this.theFallbackBrowser.InvalidateVisual();
            }
#endif
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

        public void StartOffscreenRender(string url)
        {
#if WITHCEF
            var settings = new CefSharp.OffScreen.CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache"),
            };

            // play with surfaces
            settings.SetOffScreenRenderingBestPerformanceArgs();

            //Perform dependency check to make sure all relevant resources are in our output directory.
            // Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

            var rqs = new RequestContextSettings();
            rqs.CachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache");
            var rq = new RequestContext(rqs, null);

            var bs = new BrowserSettings();
            bs.BackgroundColor = 0xff000000;

            this.theOffscreenBrowser = new CefSharp.OffScreen.ChromiumWebBrowser(url, bs, rq);
            this.theOffscreenBrowser.Size = new System.Drawing.Size(400, 800);
            this.theOffscreenBrowser.LoadingStateChanged += TheBrowser_LoadingStateChanged;
            this.theOffscreenBrowser.FrameLoadEnd += TheOffscreenBrowser_FrameLoadEnd;

            private void TheOffscreenBrowser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
            {
                ;
            }
#endif
        }



        #endregion
        #region Callbacks from the browse
        //==============================

#if WITHCEF
        private void TheBrowser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            // see: https://github.com/cefsharp/CefSharp.MinimalExample/blob/master/
            // CefSharp.MinimalExample.OffScreen/Program.cs
            if (theOffscreenBrowser == null)
                return;

            // Check to see if loading is complete - this event is called twice, one when loading starts
            // second time when it's finished
            // (rather than an iframe within the main frame).
            if (!e.IsLoading)
            {
                // Remove the load event handler, because we only want one snapshot of the initial page.
                this.theOffscreenBrowser.LoadingStateChanged -= TheBrowser_LoadingStateChanged;

                var scriptTask = this.theOffscreenBrowser.EvaluateScriptAsync(
                    "document.getElementById('lst-ib').value = 'CefSharp Was Here!'");

                scriptTask.ContinueWith(t =>
                {
                    //Give the browser a little time to render
                    Thread.Sleep(5000);
                    // Wait for the screenshot to be taken.
                    var task = this.theOffscreenBrowser.ScreenshotAsync();
                    task.ContinueWith(x =>
                    {
                        // Make a file to save it to (e.g. C:\Users\jan\Desktop\CefSharp screenshot.png)
                        var screenshotPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");

                        Console.WriteLine();
                        Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

                        // Save the Bitmap to the path.
                        // The image type is auto-detected via the ".png" extension.
                        System.Drawing.Bitmap orig = task.Result;
                        var rect = new System.Drawing.Rectangle(
                            new System.Drawing.Point(3, 59), new System.Drawing.Size(376, 532));
                        var cutout = CropImage(orig, rect);

                        cutout.Save(screenshotPath);

                        // We no longer need the Bitmap.
                        // Dispose it to avoid keeping the memory alive.  Especially important in 32-bit applications.
                        task.Result.Dispose();
                        cutout.Dispose();

                    }, TaskScheduler.Default);
                });
            }
        }
#endif
        #endregion

        public Bitmap CropImage(Bitmap source, Rectangle section)
        {
            var bitmap = new Bitmap(section.Width, section.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
                return bitmap;
            }
        }
    }
}
