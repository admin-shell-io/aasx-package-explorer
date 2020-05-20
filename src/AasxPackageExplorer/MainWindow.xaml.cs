﻿
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using System.Threading;
using System.Text;

using AdminShellNS;
using AasxGlobalLogging;
using AasxIntegrationBase;
// using SampleClient; // Read property values by OPC UA

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License - v 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IFlyoutProvider
    {
        #region Members
        // ============


        public AdminShellPackageEnv thePackageEnv = new AdminShellPackageEnv();
        public AdminShellPackageEnv thePackageAux = null;
        private string showContentPackageUri = null;
        private VisualElementGeneric currentEntityForUpdate = null;
        private IFlyoutControl currentFlyoutControl = null;

        private BrowserContainer theContentBrowser = new BrowserContainer();
#if OFFSCREENBROWSER
        private BrowserContainer theOffscreenBrowser = new BrowserContainer();
#endif

        private AasxIntegrationBase.IAasxOnlineConnection theOnlineConnection = null;

        #endregion
        #region Init Component
        //====================

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion
        #region Utility functions
        //=======================

        public static string WpfStringAddWrapChars(string str)
        {
            var res = "";
            foreach (var c in str)
                res += c + "\u200b";
            return res;
        }

        /// <summary>
        /// Directly browse and show an url page
        /// </summary>
        public void ShowContentBrowser(string url, bool silent = false)
        {
            theContentBrowser.GoToContentBrowserAddress(url);
            if (!silent)
                Dispatcher.BeginInvoke((Action)(() => ElementTabControl.SelectedIndex = 1));
        }

        /// <summary>
        /// Directly browse and show help page
        /// </summary>
        public void ShowHelp(bool silent = false)
        {
            // ShowContentBrowser(@"https://github.com/admin-shell/aasx-package-explorer/blob/master/help/index.md", silent);
            if (!silent)
                BrowserDisplayLocalFile(@"https://github.com/admin-shell/aasx-package-explorer/blob/master/help/index.md");
        }

        /// <summary>
        /// Calls the browser. Note: does NOT catch exceptions!
        /// </summary>
        private void BrowserDisplayLocalFile(string url, bool preferInternal = false)
        {
            if (theContentBrowser.CanHandleFileNameExtension(url) || preferInternal)
            {
                // try view in browser
                Log.Info($"Displaying {url} locally in embedded browser ..");
                ShowContentBrowser(url);
            }
            else
            {
                // open externally
                Log.Info($"Displaying {this.showContentPackageUri} remotely in external viewer ..");
                System.Diagnostics.Process.Start(url);
            }
        }

        public void ClearAllViews()
        {
            // left side
            this.AasId.Text = "<id missing!>";
            this.AssetPic.Source = null;
            this.AssetId.Text = "<id missing!>";

            // middle side
            DisplayElements.Clear();

            // right side
            theContentBrowser.GoToContentBrowserAddress(Options.Curr.ContentHome);
        }

        public void RedrawAllAasxElements()
        {
            var t = "AASX Package Explorer";
            if (thePackageEnv != null)
                t += " - " + System.IO.Path.GetFileName(thePackageEnv.Filename);
            if (thePackageAux != null)
                t += " (auxiliary AASX: " + System.IO.Path.GetFileName(thePackageAux.Filename) + ")";
            this.Title = t;

            // clear the right section, first (might be rebuild by callback from below)
            DispEditEntityPanel.ClearDisplayDefautlStack();
            ContentTakeOver.IsEnabled = false;

            // rebuild middle section
            DisplayElements.RebuildAasxElements(this.thePackageEnv.AasEnv, this.thePackageEnv, null, MenuItemWorkspaceEdit.IsChecked);
            DisplayElements.Refresh();

        }

        private void RestartUIafterNewPackage(bool onlyAuxiliary = false)
        {
            if (onlyAuxiliary)
            {
                // reduced, in the background
                RedrawAllAasxElements();
            }
            else
            {
                // visually a new content
                // switch off edit mode -> will will cause the browser to show the AAS as selected element
                // and -> this will update the left side of the screen correctly!
                MenuItemWorkspaceEdit.IsChecked = false;
                ClearAllViews();
                RedrawAllAasxElements();
                RedrawElementView();
                ShowContentBrowser(Options.Curr.ContentHome, silent: true);
            }
        }

        private AdminShellPackageEnv LoadPackageFromFile(string fn)
        {
            if (fn.Trim().ToLower().EndsWith(".aml"))
            {
                var res = new AdminShellPackageEnv();
                AasxAmlImExport.AmlImport.ImportInto(res, fn);
                return res;
            }
            else
                return new AdminShellPackageEnv(fn, Options.Curr.IndirectLoadSave);
        }

        public void UiLoadPackageWithNew(ref AdminShellPackageEnv packenv, AdminShellPackageEnv packnew, string info = "", bool onlyAuxiliary = false)
        {
            Log.Info("Loading new AASX from: {0} as auxiliary {1} ..", info, onlyAuxiliary);
            // loading
            try
            {
                if (packenv != null)
                    packenv.Close();
                packenv = packnew;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"When loading {info}, an error occurred");
                return;
            }
            // displaying
            try
            {
                RestartUIafterNewPackage(onlyAuxiliary);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"When displaying element tree of {info}, an error occurred");
                return;
            }
            Log.Info("AASX {0} loaded.", info);

        }

        public void PrepareDispEditEntity(AdminShellPackageEnv package, VisualElementGeneric entity, bool editMode, bool hintMode, DispEditHighlight.HighlightFieldInfo hightlightField = null)
        {
            // make UI visible settings ..
            // update element view
            var renderHints = DispEditEntityPanel.DisplayOrEditVisualAasxElement(package, entity, editMode, hintMode,
                (thePackageAux == null) ? null : new AdminShellPackageEnv[] { thePackageAux }, flyoutProvider: this, hightlightField: hightlightField);

            // panels
            var panelHeight = 48;
            if (renderHints != null && renderHints.showDataPanel == false)
            {
                ContentPanelNoEdit.Visibility = Visibility.Collapsed;
                ContentPanelEdit.Visibility = Visibility.Collapsed;
                panelHeight = 0;
            }
            else
            {
                if (!editMode)
                {
                    ContentPanelNoEdit.Visibility = Visibility.Visible;
                    ContentPanelEdit.Visibility = Visibility.Hidden;
                }
                else
                {
                    ContentPanelNoEdit.Visibility = Visibility.Hidden;
                    ContentPanelEdit.Visibility = Visibility.Visible;
                }
            }
            RowContentPanels.Height = new GridLength(panelHeight);

            // scroll or not
            if (renderHints != null && renderHints.scrollingPanel == false)
            {
                ScrollViewerElement.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }
            else
            {
                ScrollViewerElement.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            }

            // further
            ShowContent.IsEnabled = false;
            DragSource.Foreground = Brushes.DarkGray;
            UpdateContent.IsEnabled = false;
            this.showContentPackageUri = null;

            // show it
            Dispatcher.BeginInvoke((Action)(() => ElementTabControl.SelectedIndex = 0));

            // some entities require special handling
            if (entity is VisualElementSubmodelElement && (entity as VisualElementSubmodelElement).theWrapper.submodelElement is AdminShell.File)
            {
                var elem = (entity as VisualElementSubmodelElement).theWrapper.submodelElement;
                ShowContent.IsEnabled = true;
                this.showContentPackageUri = (elem as AdminShell.File).value;
                DragSource.Foreground = Brushes.Black;
            }

            if (this.theOnlineConnection != null && this.theOnlineConnection.IsValid() && this.theOnlineConnection.IsConnected())
            {
                UpdateContent.IsEnabled = true;
                this.currentEntityForUpdate = entity;
            }
        }

        public void RedrawElementView(DispEditHighlight.HighlightFieldInfo hightlightField = null)
        {
            if (DisplayElements == null)
                return;

            // the AAS will cause some more visual effects
            var tvlaas = DisplayElements.SelectedItem as VisualElementAdminShell;
            if (thePackageEnv != null && tvlaas != null && tvlaas.theAas != null && tvlaas.theEnv != null)
            {
                // AAS
                // update graphic left

                // what is AAS specific?
                this.AasId.Text = WpfStringAddWrapChars(AdminShellUtil.EvalToNonNullString("{0}", tvlaas.theAas.identification.id, "<id missing!>"));

                // what is asset specific?
                this.AssetPic.Source = null;
                this.AssetId.Text = "<id missing!>";
                var asset = tvlaas.theEnv.FindAsset(tvlaas.theAas.assetRef);
                if (asset != null)
                {

                    // text id
                    if (asset.identification != null)
                        this.AssetId.Text = WpfStringAddWrapChars(AdminShellUtil.EvalToNonNullString("{0}", asset.identification.id, ""));

                    // asset thumbail
                    try
                    {
                        // identify which stream to use..
                        if (thePackageEnv != null)
                            try
                            {
                                var thumbStream = thePackageEnv.GetLocalThumbnailStream();

                                // load image
                                if (thumbStream != null)
                                {
                                    var bi = new BitmapImage();
                                    bi.BeginInit();
                                    bi.CacheOption = BitmapCacheOption.OnLoad;
                                    bi.StreamSource = thumbStream;
                                    bi.EndInit();
                                    this.AssetPic.Source = bi;
                                    // note: no closing required!
                                }

                            }
                            catch { }

                        if (this.theOnlineConnection != null && this.theOnlineConnection.IsValid() && this.theOnlineConnection.IsConnected())
                            try
                            {
                                var thumbStream = this.theOnlineConnection.GetThumbnailStream();

                                if (thumbStream != null)
                                {
                                    var ms = new MemoryStream();
                                    thumbStream.CopyTo(ms);
                                    ms.Flush();
                                    var bitmapdata = ms.ToArray();

                                    ms.Close();
                                    thumbStream.Close();

                                    var bi = (BitmapSource)new ImageSourceConverter().ConvertFrom(bitmapdata);
                                    this.AssetPic.Source = bi;
                                }
                            }
                            catch { }

                    }
                    catch
                    {
                        // no error, intended behaviour, as thumbnail might not exist / be faulty in some way (not violating the spec)
                        // Log.Error(ex, "Error loading package's thumbnail");
                    }
                }
            }

            // for all, prepare the display
            PrepareDispEditEntity(thePackageEnv, DisplayElements.SelectedItem, MenuItemWorkspaceEdit.IsChecked, MenuItemWorkspaceHints.IsChecked, hightlightField: hightlightField);

        }

        #endregion
        #region Callbacks
        //===============

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // making up "empty" picture
            this.AasId.Text = "<id unknown!>";
            this.AssetId.Text = "<id unknown!>";

            // display elements has a cache
            DisplayElements.ActivateElementStateCache();

            // show Logo?
            if (Options.Curr.LogoFile != null)
                try
                {
                    var fullfn = System.IO.Path.GetFullPath(Options.Curr.LogoFile);
                    var bi = new BitmapImage(new Uri(fullfn, UriKind.RelativeOrAbsolute));
                    this.LogoImage.Source = bi;
                    this.LogoImage.UpdateLayout();
                }
                catch { }

            // adding the CEF Browser conditionally
            theContentBrowser.Start(Options.Curr.ContentHome, Options.Curr.InternalBrowser);
            CefContainer.Child = theContentBrowser.BrowserControl;

#if OFFSCREENBROWSER
            theOffscreenBrowser.Start(Options.Curr.ContentHome, true, useOffscreen: true);
#endif

            // window size?
            if (Options.Curr.WindowLeft > 0) this.Left = Options.Curr.WindowLeft;
            if (Options.Curr.WindowTop > 0) this.Top = Options.Curr.WindowTop;
            if (Options.Curr.WindowWidth > 0) this.Width = Options.Curr.WindowWidth;
            if (Options.Curr.WindowHeight > 0) this.Height = Options.Curr.WindowHeight;
            if (Options.Curr.WindowMaximized)
                this.WindowState = WindowState.Maximized;

            // Timer for below
            System.Windows.Threading.DispatcherTimer MainTimer = new System.Windows.Threading.DispatcherTimer();
            MainTimer.Tick += new EventHandler(MainTimer_Tick);
            MainTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            MainTimer.Start();

            // attach result search
            ToolFindReplace.ResultSelected += ToolFindReplace_ResultSelected;

            // Last task here ..
            Log.Info("Application started ..");

            // Try to load?
            if (Options.Curr.AasxToLoad != null)
            {
                // make a fully qualified filename from that one provided as input argument
                var fn = System.IO.Path.GetFullPath(Options.Curr.AasxToLoad);
                // UiLoadPackageEnv(fn);          
                try
                {
                    var pkg = LoadPackageFromFile(fn);
                    UiLoadPackageWithNew(ref thePackageEnv, pkg, fn, onlyAuxiliary: false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"When auto-loading {fn}");
                }
            }
        }

        private void ToolFindReplace_ResultSelected(AdminShellUtil.SearchResultItem resultItem)
        {
            // have a result?
            if (resultItem == null || resultItem.businessObject == null)
                return;

            // for valid display, app needs to be in edit mode
            if (!MenuItemWorkspaceEdit.IsChecked)
            {
                this.MessageBoxFlyoutShow("The application needs to be in edit mode to show found entities correctly. Aborting.", "Find and Replace", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            // add to "normal" event quoue
            DispEditEntityPanel.AddWishForOutsideAction(
                new ModifyRepo.LambdaActionRedrawAllElements(
                    nextFocus: resultItem.businessObject,
                    highlightField: new DispEditHighlight.HighlightFieldInfo(resultItem.containingObject, resultItem.foundObject, resultItem.foundHash),
                    onlyReFocus: true));
        }

        private void MainTimer_HandleLogMessages()
        {
            // pop log messages from the plug-ins into the Stored Prints in Log
            Plugins.PumpPluginLogsIntoLog(this.FlyoutLoggingPush);

            // check for Stored Prints in Log
            StoredPrint sp;
            while ((sp = Log.LogInstance.PopLastShortTermPrint()) != null)
            {
                // pop
                Message.Content = "" + sp.msg;

                // display
                if (sp.color == StoredPrint.ColorBlack)
                {
                    Message.Background = Brushes.White;
                    Message.Foreground = Brushes.Black;
                    Message.FontWeight = FontWeights.Normal;
                }
                if (sp.color == StoredPrint.ColorBlue)
                {
                    Message.Background = Brushes.LightBlue;
                    Message.Foreground = Brushes.Black;
                    Message.FontWeight = FontWeights.Normal;
                }
                if (sp.color == StoredPrint.ColorRed)
                {
                    Message.Background = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                    Message.Foreground = Brushes.White;
                    Message.FontWeight = FontWeights.Bold;
                }
            }

            // always tell the errors
            var ne = Log.LogInstance.NumberErrors;
            if (ne > 0)
            {
                LabelNumberErrors.Content = "Errors: " + ne;
                LabelNumberErrors.Background = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
            }
            else
            {
                LabelNumberErrors.Content = "No errors";
                LabelNumberErrors.Background = Brushes.White;
            }
        }

        private void MainTimer_HandleEntityPanel()
        {
            // check if Display/ Edit Control has some work to do ..
            try
            {
                if (DispEditEntityPanel != null && DispEditEntityPanel.WishForOutsideAction != null)
                {
                    while (DispEditEntityPanel.WishForOutsideAction.Count > 0)
                    {
                        var temp = DispEditEntityPanel.WishForOutsideAction[0];
                        DispEditEntityPanel.WishForOutsideAction.RemoveAt(0);

                        // what to do?
                        if (temp is ModifyRepo.LambdaActionRedrawAllElements)
                        {
                            var wish = temp as ModifyRepo.LambdaActionRedrawAllElements;
                            // edit mode affects the total element view
                            if (!wish.OnlyReFocus)
                                RedrawAllAasxElements();
                            // the selection will be shifted ..
                            if (wish.NextFocus != null)
                            {
                                DisplayElements.TrySelectMainDataObject(wish.NextFocus, wish.IsExpanded == true);
                            }
                            // fake selection
                            RedrawElementView(hightlightField: wish.HighlightField);
                            DisplayElements.Refresh();
                            ContentTakeOver.IsEnabled = false;
                        }

                        if (temp is ModifyRepo.LambdaActionContentsChanged)
                        {
                            // enable button
                            ContentTakeOver.IsEnabled = true;
                        }

                        if (temp is ModifyRepo.LambdaActionContentsTakeOver)
                        {
                            // rework list
                            ContentTakeOver_Click(null, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "While responding to a user interaction");
            }
        }

        private void MainTimer_HandlePlugins()
        {
            // check if a plug-in has some work to do ..
            foreach (var lpi in Plugins.LoadedPlugins.Values)
            {
                try
                {
                    var evt = lpi.InvokeAction("get-events") as AasxIntegrationBase.AasxPluginResultEventBase;

                    #region Navigate To
                    //=================

                    var evtNavTo = evt as AasxIntegrationBase.AasxPluginResultEventNavigateToReference;
                    if (evtNavTo != null && evtNavTo.targetReference != null && evtNavTo.targetReference.Count > 0)
                    {
                        // make a copy of the Reference for searching
                        VisualElementGeneric veFound = null;
                        var work = new AdminShell.Reference(evtNavTo.targetReference);

                        try
                        {
                            // incrementally make it unprecise
                            while (work.Count > 0)
                            {
                                // try to find a business object in the package
                                AdminShell.Referable bo = null;
                                if (thePackageEnv != null && thePackageEnv.AasEnv != null)
                                    bo = thePackageEnv.AasEnv.FindReferableByReference(work);

                                // if not, may be in aux package
                                if (bo == null && thePackageAux != null && thePackageAux.AasEnv != null)
                                    bo = thePackageAux.AasEnv.FindReferableByReference(work);

                                // yes?
                                if (bo != null)
                                {
                                    // try to look up in visual elements
                                    if (this.DisplayElements != null)
                                    {
                                        var ve = this.DisplayElements.SearchVisualElementOnMainDataObject(bo);
                                        if (ve != null)
                                        {
                                            veFound = ve;
                                            break;
                                        }
                                    }
                                }

                                // make it more unprecice
                                work.Keys.RemoveAt(work.Count - 1);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "While retrieving element requested by plug-in");
                        }

                        // if successful, try to display it
                        try
                        {
                            if (veFound != null)
                            {
                                // show ve
                                DisplayElements.TrySelectVisualElement(veFound, wishExpanded: true);
                                // remember in history
                                ButtonHistory.Push(veFound);
                                // fake selection
                                RedrawElementView();
                                DisplayElements.Refresh();
                                ContentTakeOver.IsEnabled = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "While displaying element requested by plug-in");
                        }
                    }
                    #endregion

                    #region Display content file
                    //==========================

                    var evtDispCont = evt as AasxIntegrationBase.AasxPluginResultEventDisplayContentFile;
                    if (evtDispCont != null && evtDispCont.fn != null)
                        try
                        {
                            BrowserDisplayLocalFile(evtDispCont.fn, evtDispCont.preferInternalDisplay);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"While displaying content file {evtDispCont.fn} requested by plug-in");
                        }

                    #endregion
                    #region Offscreen render file
                    //===========================
#if OFFSCREENBROWSER
                    var evtRender = evt as AasxIntegrationBase.AasxPluginResultEventOffscreenRenderFile;
                    if (evtRender != null && evtRender.fn != null)
                        try
                        {
                            theOffscreenBrowser?.StartOffscreenRender(evtRender.fn);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"While starting offscreen render of file {evtRender.fn} requested by plug-in");
                        }
#endif
                    #endregion
                    #region Redisplay explorer contents
                    //=================================

                    var evtRedrawAll = evt as AasxIntegrationBase.AasxPluginResultEventRedrawAllElements;
                    if (evtRedrawAll != null)
                    {
                        if (DispEditEntityPanel != null)
                        {
                            // figure out the current business object
                            object nextFocus = null;
                            if (DisplayElements != null && DisplayElements.SelectedItem != null && DisplayElements.SelectedItem != null)
                                nextFocus = DisplayElements.SelectedItem.GetMainDataObject();

                            // add to "normal" event quoue
                            DispEditEntityPanel.AddWishForOutsideAction(new ModifyRepo.LambdaActionRedrawAllElements(nextFocus));
                        }
                    }

                    #endregion
                    #region Select AAS entity
                    //=======================

                    var evSelectEntity = evt as AasxIntegrationBase.AasxPluginResultEventSelectAasEntity;
                    if (evSelectEntity != null)
                    {
                        var uc = new SelectAasEntityFlyout(thePackageEnv.AasEnv, evSelectEntity.filterEntities, thePackageEnv);
                        this.StartFlyoverModal(uc);
                        if (uc.ResultKeys != null)
                        {
                            // formulate return event
                            var retev = new AasxIntegrationBase.AasxPluginEventReturnSelectAasEntity();
                            retev.sourceEvent = evt;
                            retev.resultKeys = uc.ResultKeys;

                            // fire back
                            lpi.InvokeAction("event-return", retev);
                        }
                    }


                    #endregion
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"While responding to a event from plug-in {"" + lpi?.name}");
                }
            }
        }

        private void MainTimer_Tick(object sender, EventArgs e)
        {
            MainTimer_HandleLogMessages();
            MainTimer_HandleEntityPanel();
            MainTimer_HandlePlugins();
        }

        private void ButtonHistory_ObjectRequested(object sender, VisualElementGeneric ve)
        {
            // be careful
            try
            {
                if (ve != null)
                {
                    // show ve
                    if (DisplayElements.TrySelectVisualElement(ve, wishExpanded: true))
                    {
                        // fake selection
                        RedrawElementView();
                        DisplayElements.Refresh();
                        ContentTakeOver.IsEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "While displaying element requested by plug-in");
            }
        }

        private void ButtonReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonClear)
            {
                Log.LogInstance.ClearNumberErrors();
                Message.Content = "";
                Message.Background = Brushes.White;
                Message.Foreground = Brushes.Black;
                Message.FontWeight = FontWeights.Normal;
            }
            if (sender == ButtonReport)
            {
                // report on message / exception
                var head = @"
                |Dear user,
                |thank you for reporting an error / bug / unexpected behaviour back to the AASX package explorer team. 
                |Please provide the following details:
                |
                |  User: <who was working with the application>
                |
                |  Steps to reproduce: <what was the user doing, when the unexpected behaviour occurred>
                |
                |  Expected results: <what should happen>
                |
                |  Actual Results: <what was actually happening>
                |
                |  Latest message: {0}
                |
                |Please consider attaching the AASX package (you might rename this to .zip), you were working on, as well as an screen shot.
                |
                |Please mail your report to: michael.hoffmeister@festo.com
                |or you can directly add it at github: https://github.com/admin-shell/aasx-package-explorer/issues
                |
                |Below, you're finding the history of log messages. Please check, if non-public information is contained here.
                |-------------------------------------------------------------------------------------------------------------";

                // Substitute
                head += "\n";
                head = head.Replace("{0}", "" + Message?.Content?.ToString());
                head = Regex.Replace(head, @"^(\s+)\|", "", RegexOptions.Multiline);

                // test
                if (false)
                {
                    Log.Info(0, StoredPrint.ColorBlue, "This is blue");
                    Log.Info(0, StoredPrint.ColorRed, "This is red");
                    Log.Error("This is an error!");
                    Log.InfoWithHyperlink(0, "This is an link", "(Link)", "https://www.google.de");
                }

                // create window

                var dlg = new MessageReportWindow();
                dlg.Append(new StoredPrint(head));

                if (true)
                {
                    var prints = Log.LogInstance.GetStoredLongTermPrints();
                    if (prints != null)
                        foreach (var sp in prints)
                        {
                            dlg.Append(sp);
                            if (sp.stackTrace != null)
                                dlg.Append(new StoredPrint("    Stacktrace: " + sp.stackTrace));
                        }
                }

                // show dialogue
                dlg.ShowDialog();
            }
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // decode
            if (e == null || e.Command == null || !(e.Command is RoutedUICommand))
                return;
            var cmd = (e.Command as RoutedUICommand).Text.Trim().ToLower();

            // see: MainWindow.CommandBindings.cs
            this.CommandBinding_GeneralDispatch(cmd);
        }


        private void DisplayElements_SelectedItemChanged(object sender, EventArgs e)
        {
            // access
            if (DisplayElements == null || sender != DisplayElements)
                return;

            // try identify the business object
            if (DisplayElements.SelectedItem != null)
            {
                ButtonHistory.Push(DisplayElements.SelectedItem);
            }

            // redraw view
            RedrawElementView();
        }

        private void DisplayElements_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // we're assuming, that SelectedItem point to the right business object
            if (DisplayElements.SelectedItem == null)
                return;

            // redraw view
            RedrawElementView();

            // "simulate" click on "ShowContents"
            this.ShowContent_Click(this.ShowContent, null);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (this.IsInFlyout())
            {
                e.Cancel = true;
                return;
            }

            var positiveQuestion = Options.Curr.UseFlyovers && MessageBoxResult.Yes == MessageBoxFlyoutShow("Do you want to proceed closing the application? Make sure, that you have saved your data before.", "Exit application?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (!positiveQuestion)
            {
                e.Cancel = true;
                return;
            }

            Log.Info("Closing ..");
            try
            {
                thePackageEnv.Close();
            }
            catch (Exception)
            { }

            e.Cancel = false;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.ActualWidth > 800)
            {
                // Log.Info($"Resizing window to {this.ActualWidth}x{this.ActualHeight} ..");
                if (MainSpaceGrid != null && MainSpaceGrid.ColumnDefinitions.Count >= 3)
                {
                    MainSpaceGrid.ColumnDefinitions[0].Width = new GridLength(this.ActualWidth / 5);
                    MainSpaceGrid.ColumnDefinitions[4].Width = new GridLength(this.ActualWidth / 3);
                }
            }
        }

        private void ShowContent_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ShowContent && this.showContentPackageUri != null && this.thePackageEnv != null)
            {
                Log.Info("Trying display content {0} ..", this.showContentPackageUri);
                try
                {
                    var contentUri = this.showContentPackageUri;

                    // if local in the package, then make a tempfile
                    if (!this.showContentPackageUri.ToLower().Trim().StartsWith("http://")
                        && !this.showContentPackageUri.ToLower().Trim().StartsWith("https://"))
                    {
                        // make it as file
                        contentUri = thePackageEnv.MakePackageFileAvailableAsTempFile(this.showContentPackageUri);
                    }

                    BrowserDisplayLocalFile(contentUri);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"When displaying content {this.showContentPackageUri}, an error occurred");
                    return;
                }
                Log.Info("Content {0} displayed.", this.showContentPackageUri);
            }
        }

        private void UpdateContent_Click(object sender, RoutedEventArgs e)
        {
            // have a online connection?
            if (this.theOnlineConnection != null && this.theOnlineConnection.IsValid() && this.theOnlineConnection.IsConnected())
            {
                // current entity is a property
                if (this.currentEntityForUpdate != null && this.currentEntityForUpdate is VisualElementSubmodelElement)
                {
                    var viselem = this.currentEntityForUpdate as VisualElementSubmodelElement;
                    if (viselem != null && viselem.theEnv != null
                        && viselem.theContainer != null && viselem.theContainer is AdminShell.Submodel
                        && viselem.theWrapper != null && viselem.theWrapper.submodelElement != null && viselem.theWrapper.submodelElement is AdminShell.Property)
                    {
                        // access a valid property
                        var p = viselem.theWrapper.submodelElement as AdminShell.Property;

                        // use online connection
                        var x = this.theOnlineConnection.UpdatePropertyValue(viselem.theEnv, viselem.theContainer as AdminShell.Submodel, p);
                        p.value = x;

                        var y = DisplayElements.SelectedItem;
                        if (y != null && y is VisualElementGeneric)
                            y.RefreshFromMainData();
                        DisplayElements.Refresh();
                    }
                }
            }
        }

        private void ContentUndo_Click(object sender, RoutedEventArgs e)
        {
            DispEditEntityPanel.CallUndo();
        }

        private void ContentTakeOver_Click(object sender, RoutedEventArgs e)
        {
            var x = DisplayElements.SelectedItem;
            if (x != null && x is VisualElementGeneric)
                x.RefreshFromMainData();
            DisplayElements.Refresh();
            ContentTakeOver.IsEnabled = false;
        }

        private void DispEditEntityPanel_ContentsChanged(object sender, int kind)
        {
        }

        private void mainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.OemPlus || e.Key == Key.Add) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (theContentBrowser != null)
                    theContentBrowser.ZoomLevel += 0.25;
            }

            if ((e.Key == Key.OemMinus || e.Key == Key.Subtract) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (theContentBrowser != null)
                    theContentBrowser.ZoomLevel -= 0.25;
            }

            if (this.IsInFlyout() && currentFlyoutControl != null)
            {
                currentFlyoutControl.ControlPreviewKeyDown(e);
            }
        }

        #endregion
        #region Modal Flyovers
        //====================

        private List<StoredPrint> flyoutLogMessages = null;

        public void FlyoutLoggingStart()
        {
            flyoutLogMessages = new List<StoredPrint>();
        }

        public void FlyoutLoggingStop()
        {
            flyoutLogMessages = null;
        }

        public void FlyoutLoggingPush(StoredPrint msg)
        {
            if (flyoutLogMessages == null)
                return;

            lock (flyoutLogMessages)
            {
                flyoutLogMessages.Add(msg);
            }
        }

        public StoredPrint FlyoutLoggingPop()
        {
            if (flyoutLogMessages != null)
                lock (flyoutLogMessages)
                {
                    if (flyoutLogMessages.Count > 0)
                    {
                        var msg = flyoutLogMessages[0];
                        flyoutLogMessages.RemoveAt(0);
                        return msg;
                    }
                }
            return null;
        }

        public bool IsInFlyout()
        {
            if (this.GridFlyover.Children.Count > 0)
                return true;
            return false;
        }

        public void StartFlyover(UserControl uc)
        {
            // uc needs to implement IFlyoverControl
            var ucfoc = uc as IFlyoutControl;
            if (ucfoc == null)
                return;

            // blur the normal grid
            this.InnerGrid.IsEnabled = false;
            var blur = new BlurEffect();
            blur.Radius = 5;
            this.InnerGrid.Opacity = 0.5;
            this.InnerGrid.Effect = blur;

            // populate the flyover grid
            this.GridFlyover.Visibility = Visibility.Visible;
            this.GridFlyover.Children.Clear();
            this.GridFlyover.Children.Add(uc);

            // register the event
            ucfoc.ControlClosed += Ucfoc_ControlClosed;
            currentFlyoutControl = ucfoc;

            // start (focus)
            ucfoc.ControlStart();
        }

        private void Ucfoc_ControlClosed()
        {
            CloseFlyover();
        }

        public void CloseFlyover()
        {
            // blur the normal grid
            this.InnerGrid.Opacity = 1.0;
            this.InnerGrid.Effect = null;
            this.InnerGrid.IsEnabled = true;

            // un-populate the flyover grid
            this.GridFlyover.Children.Clear();
            this.GridFlyover.Visibility = Visibility.Hidden;

            // unregister
            currentFlyoutControl = null;
        }

        public void StartFlyoverModal(UserControl uc, Action closingAction = null)
        {
            // uc needs to implement IFlyoverControl
            var ucfoc = uc as IFlyoutControl;
            if (ucfoc == null)
                return;

            // blur the normal grid
            this.InnerGrid.IsEnabled = false;
            var blur = new BlurEffect();
            blur.Radius = 5;
            this.InnerGrid.Opacity = 0.5;
            this.InnerGrid.Effect = blur;

            // populate the flyover grid
            this.GridFlyover.Visibility = Visibility.Visible;
            this.GridFlyover.Children.Clear();
            this.GridFlyover.Children.Add(uc);

            // register the event
            var frame = new DispatcherFrame();
            ucfoc.ControlClosed += () =>
            {
                frame.Continue = false; // stops the frame
            };

            currentFlyoutControl = ucfoc;

            // start (focus)
            ucfoc.ControlStart();

            // This will "block" execution of the current dispatcher frame
            // and run our frame until the dialog is closed.
            Dispatcher.PushFrame(frame);

            // call the closing action (before releasing!)
            if (closingAction != null)
                closingAction();

            // blur the normal grid
            this.InnerGrid.Opacity = 1.0;
            this.InnerGrid.Effect = null;
            this.InnerGrid.IsEnabled = true;

            // un-populate the flyover grid
            this.GridFlyover.Children.Clear();
            this.GridFlyover.Visibility = Visibility.Hidden;

            // unregister
            currentFlyoutControl = null;
        }

        public MessageBoxResult MessageBoxFlyoutShow(string message, string caption, MessageBoxButton buttons, MessageBoxImage image)
        {
            if (!Options.Curr.UseFlyovers)
            {
                return MessageBox.Show(this, message, caption, buttons, image);
            }

            var uc = new MessageBoxFlyout(message, caption, buttons, image);
            StartFlyoverModal(uc);
            return uc.Result;
        }

        public Window GetWin32Window()
        {
            return this;
        }

        #endregion
        #region Drag&Drop
        //===============

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("myFormat") || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            // stupid! appearantly need to figure out, if OriginalSource would have handled the Drop???
            /*
            var enable = true;
            if (e.OriginalSource != null && e.OriginalSource is UIElement && (e.OriginalSource as UIElement).AllowDrop)
                enable = false;
                */

            if (!e.Handled && e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                if (files.Length > 0)
                {
                    string fn = files[0];
                    try
                    {
                        UiLoadPackageWithNew(ref thePackageEnv, LoadPackageFromFile(fn), fn, onlyAuxiliary: false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"while receiving file drop to window");
                    }
                }
            }
        }

        private bool isDragging = false;
        private Point dragStartPoint = new Point(0, 0);

        private void DragSource_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !isDragging && (dragStartPoint.X != 0 && dragStartPoint.Y != 0)
                && this.showContentPackageUri != null && this.thePackageEnv != null)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // check if it an address in the package only
                    if (!this.showContentPackageUri.Trim().StartsWith("/"))
                        return;

                    // lock
                    isDragging = true;

                    // fail safe
                    try
                    {
                        // hastily prepare temp file ..
                        var tempfile = thePackageEnv.MakePackageFileAvailableAsTempFile(this.showContentPackageUri);

                        // Package the data.
                        DataObject data = new DataObject();
                        // data.SetData(DataFormats.FileDrop, tempfile);
                        data.SetFileDropList(new System.Collections.Specialized.StringCollection() { tempfile });

                        // Inititate the drag-and-drop operation.
                        DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"When dragging content {this.showContentPackageUri}, an error occurred");
                        return;
                    }

                    // unlock
                    isDragging = false;
                }
            }
        }

        private void DragSource_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = e.GetPosition(null);
        }

        #endregion

        private void ButtonTools_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonToolsClose)
            {
                ToolsGrid.Visibility = Visibility.Collapsed;
                if (DispEditEntityPanel != null)
                    DispEditEntityPanel.ClearHighlight();
            }
        }
    }
}
