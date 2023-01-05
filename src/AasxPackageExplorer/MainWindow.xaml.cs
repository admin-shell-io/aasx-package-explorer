/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Aas = AasCore.Aas3_0_RC02;
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AasxPackageLogic.PackageCentral.AasxFileServerInterface;
using AasxWpfControlLibrary;
using AasxWpfControlLibrary.PackageCentral;
using AdminShellNS;
using AdminShellNS.DiaryData;
using AnyUi;
using Extensions;
using Microsoft.Win32;
using Newtonsoft.Json;
using ExhaustiveMatch = ExhaustiveMatching.ExhaustiveMatch;

namespace AasxPackageExplorer
{
    public partial class MainWindow : Window, IFlyoutProvider, IPushApplicationEvent
    {
        #region Dependencies
        // (mristin, 2020-11-18): consider injecting OptionsInformation, Package environment *etc.* to the main window
        // to make it traceable and testable.
        private readonly Pref _pref;
        #endregion

        #region Members
        // ============

        public PackageCentral _packageCentral = new PackageCentral();

        public AasxMenuWpf _mainMenu = new AasxMenuWpf();

        private string showContentPackageUri = null;
        private string showContentPackageMime = null;
        private VisualElementGeneric currentEntityForUpdate = null;
        private IFlyoutControl currentFlyoutControl = null;

        private BrowserContainer theContentBrowser = new BrowserContainer();

        private AasxIntegrationBase.IAasxOnlineConnection theOnlineConnection = null;

        private AasEventCompressor _eventCompressor = new AasEventCompressor();

        protected MainWindowDispatch _logic = new MainWindowDispatch();

        protected AasxMenuWpf _dynamicMenu = new AasxMenuWpf();

        #endregion
        #region Init Component
        //====================

        public MainWindow(Pref pref)
        {
            _pref = pref;
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
            if (!silent)
                BrowserDisplayLocalFile(
                    @"https://github.com/admin-shell/aasx-package-explorer/blob/master/help/index.md");
        }

        /// <summary>
        /// Calls the browser. Note: does NOT catch exceptions!
        /// </summary>
        private void BrowserDisplayLocalFile(string url, string mimeType = null, bool preferInternal = false)
        {
            if (theContentBrowser.CanHandleFileNameExtension(url, mimeType) || preferInternal)
            {
                // try view in browser
                Log.Singleton.Info($"Displaying {url} with mimeType {"" + mimeType} locally in embedded browser ..");
                ShowContentBrowser(url);
            }
            else
            {
                // open externally
                Log.Singleton.Info($"Displaying {this.showContentPackageUri} with mimeType {"" + mimeType} " +
                    $"remotely in external viewer ..");

                Process proc = new Process();
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName = url;
                proc.Start();

                // System.Diagnostics.Process.Start(url);
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

        public void RedrawAllAasxElements(bool keepFocus = false)
        {
            // focus info
            var focusMdo = DisplayElements.SelectedItem?.GetDereferencedMainDataObject();

            var t = "AASX Package Explorer V3RC02";  //TODO:jtikekar remove V3RC02
            if (_packageCentral.MainAvailable)
                t += " - " + _packageCentral.MainItem.ToString();
            if (_packageCentral.AuxAvailable)
                t += " (auxiliary AASX: " + _packageCentral.AuxItem.ToString() + ")";
            this.Title = t;

#if _log_times
            Log.Singleton.Info("Time 10 is: " + DateTime.Now.ToString("hh:mm:ss.fff"));
#endif

            // clear the right section, first (might be rebuild by callback from below)
            DispEditEntityPanel.ClearDisplayDefautlStack();
            ContentTakeOver.IsEnabled = false;

            // rebuild middle section
            DisplayElements.RebuildAasxElements(
                _packageCentral, PackageCentral.Selector.Main, _mainMenu?.IsChecked("EditMenu") == true,
                lazyLoadingFirst: true);

            // ok .. try re-focus!!
            if (keepFocus)
            {
                // make sure that Submodel is expanded
                this.DisplayElements.ExpandAllItems();

                // still proceed?
                var veFound = this.DisplayElements.SearchVisualElementOnMainDataObject(focusMdo,
                        alsoDereferenceObjects: true);

                if (veFound != null)
                    DisplayElements.TrySelectVisualElement(veFound, wishExpanded: true);
            }

            // display again
            DisplayElements.Refresh();

#if _log_times
            Log.Singleton.Info("Time 90 is: " + DateTime.Now.ToString("hh:mm:ss.fff"));
#endif
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
                _mainMenu?.SetChecked("EditMenu", false);
                ClearAllViews();
                RedrawAllAasxElements();
                RedrawElementView();
                ShowContentBrowser(Options.Curr.ContentHome, silent: true);
                _eventHandling.Reset();
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



        private PackCntRuntimeOptions UiBuildRuntimeOptionsForMainAppLoad()
        {
            var ro = new PackCntRuntimeOptions()
            {
                Log = Log.Singleton,
                ProgressChanged = (state, tfs, tbd) =>
                {
                    if (state == PackCntRuntimeOptions.Progress.Starting
                        || state == PackCntRuntimeOptions.Progress.Ongoing)
                        SetProgressBar(
                            Math.Min(100.0, 100.0 * tbd / (tfs.HasValue ? tfs.Value : 5 * 1024 * 1024)),
                            AdminShellUtil.ByteSizeHumanReadable(tbd));

                    if (state == PackCntRuntimeOptions.Progress.Final)
                    {
                        // clear
                        SetProgressBar();

                        // close message boxes
                        if (currentFlyoutControl is IntegratedConnectFlyout)
                            CloseFlyover(threadSafe: true);
                    }
                },
                ShowMesssageBox = (content, text, title, buttons) =>
                {
                    // not verbose
                    if (_mainMenu?.IsChecked("VerboseConnect") == false)
                    {
                        // give specific default answers
                        if (title?.ToLower().Trim() == "Select certificate chain".ToLower())
                            return AnyUiMessageBoxResult.Yes;

                        // default answer
                        return AnyUiMessageBoxResult.OK;
                    }

                    // make sure the correct flyout is loaded
                    if (currentFlyoutControl != null && !(currentFlyoutControl is IntegratedConnectFlyout))
                        return AnyUiMessageBoxResult.Cancel;
                    if (currentFlyoutControl == null)
                        StartFlyover(new IntegratedConnectFlyout(_packageCentral, "Connecting .."));

                    // ok -- perform dialogue in dedicated function / frame
                    var ucic = currentFlyoutControl as IntegratedConnectFlyout;
                    if (ucic == null)
                        return AnyUiMessageBoxResult.Cancel;
                    else
                        return ucic.MessageBoxShow(content, text, title, buttons);
                }
            };
            return ro;
        }

        public void UiLoadPackageWithNew(
            PackageCentralItem packItem,
            AdminShellPackageEnv takeOverEnv = null,
            string loadLocalFilename = null,
            string info = null,
            bool onlyAuxiliary = false,
            bool doNotNavigateAfterLoaded = false,
            PackageContainerBase takeOverContainer = null,
            string storeFnToLRU = null,
            bool indexItems = false)
        {
            // access
            if (packItem == null)
                return;

            if (loadLocalFilename != null)
            {
                if (info == null)
                    info = loadLocalFilename;
                Log.Singleton.Info("Loading new AASX from: {0} as auxiliary {1} ..", info, onlyAuxiliary);
                if (!packItem.Load(_packageCentral, loadLocalFilename, loadLocalFilename,
                    overrideLoadResident: true,
                    PackageContainerOptionsBase.CreateDefault(Options.Curr)))
                {
                    Log.Singleton.Error($"Loading local-file {info} as auxiliary {onlyAuxiliary} did not " +
                        $"return any result!");
                    return;
                }
            }
            else
            if (takeOverEnv != null)
            {
                Log.Singleton.Info("Loading new AASX from: {0} as auxiliary {1} ..", info, onlyAuxiliary);
                packItem.TakeOver(takeOverEnv);
            }
            else
            if (takeOverContainer != null)
            {
                Log.Singleton.Info("Loading new AASX from container: {0} as auxiliary {1} ..",
                    "" + takeOverContainer.ToString(), onlyAuxiliary);
                packItem.TakeOver(takeOverContainer);
            }
            else
            {
                Log.Singleton.Error("UiLoadPackageWithNew(): no information what to load!");
                return;
            }

            // displaying
            try
            {
                RestartUIafterNewPackage(onlyAuxiliary);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"When displaying element tree of {info}, an error occurred");
                return;
            }

            // further actions
            try
            {
                // TODO (MIHO, 2020-12-31): check for ANYUI MIHO
                if (!doNotNavigateAfterLoaded)
                    UiCheckIfActivateLoadedNavTo();

                if (indexItems && packItem?.Container?.Env?.AasEnv != null)
                    packItem.Container.SignificantElements
                        = new IndexOfSignificantAasElements(packItem.Container.Env.AasEnv);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"When performing actions after load of {info}, an error occurred");
                return;
            }

            // record in LRU?
            try
            {
                var lru = _packageCentral?.Repositories?.FindLRU();
                if (lru != null && storeFnToLRU.HasContent())
                    lru.Push(packItem?.Container as PackageContainerRepoItem, storeFnToLRU);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"When managing LRU files");
                return;
            }

            /* TODO (MIHO, 2021-12-27): consider extending for better testing or
             * script running */
#if __leave_in_for_accelerated_tet
            if (false)
            {
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
                timer.Tick += (sender, args) =>
                {
                    timer.Stop();
                    var pluginName = "AasxPluginExportTable";
                    var actionName = "export-uml";
                    var pi = Plugins.FindPluginInstance(pluginName);
                    pi?.InvokeAction(actionName, this, _packageCentral?.Main?.AasEnv,
                        _packageCentral?.Main?.AasEnv?.Submodels[0], "test.uml");

                };
                timer.Start();
            }
#endif

            // done
            Log.Singleton.Info("AASX {0} loaded.", info);
        }

        public PackageContainerListBase UiLoadFileRepository(string fn)
        {
            try
            {
                Log.Singleton.Info(
                    $"Loading aasx file repository {fn} ..");

                var fr = PackageContainerListFactory.GuessAndCreateNew(fn);

                if (fr != null)
                    return fr;
                else
                    Log.Singleton.Info(
                        $"File not found when loading aasx file repository {fn}");
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"When loading aasx file repository {Options.Curr.AasxRepositoryFn}");
            }

            return null;
        }

        /// <summary>
        /// Using the currently loaded AASX, will check if a CD_AasxLoadedNavigateTo elements can be
        /// found to be activated
        /// </summary>
        public bool UiCheckIfActivateLoadedNavTo()
        {
            // access
            if (_packageCentral.Main?.AasEnv == null || this.DisplayElements == null)
                return false;

            // use convenience function
            foreach (var sm in _packageCentral.Main.AasEnv.FindAllSubmodelGroupedByAAS())
            {
                // check for ReferenceElement
                var navTo = sm?.SubmodelElements?.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.ReferenceElement>(
                    AasxPredefinedConcepts.PackageExplorer.Static.CD_AasxLoadedNavigateTo.GetSingleKey(),  //TODO:jtikekar Test
                    MatchMode.Relaxed);
                if (navTo?.Value == null)
                    continue;

                // remember some further supplementary search information
                var sri = ListOfVisualElement.StripSupplementaryReferenceInformation(navTo.Value);

                // lookup business objects
                var bo = _packageCentral.Main?.AasEnv.FindReferableByReference(sri.CleanReference);
                if (bo == null)
                    return false;

                // make sure that Submodel is expanded
                this.DisplayElements.ExpandAllItems();

                // still proceed?
                var veFound = this.DisplayElements.SearchVisualElementOnMainDataObject(bo,
                        alsoDereferenceObjects: true, sri: sri);
                if (veFound == null)
                    return false;

                // ok .. focus!!
                DisplayElements.TrySelectVisualElement(veFound, wishExpanded: true);
                // remember in history
                ButtonHistory.Push(veFound);
                // fake selection
                RedrawElementView();
                DisplayElements.Refresh();
                ContentTakeOver.IsEnabled = false;

                // finally break
                return true;
            }

            // nothing found
            return false;
        }


        public void UiAssertFileRepository(bool visible)
        {
            // ALWAYS assert an accessible repo (even if invisble)
            if (_packageCentral.Repositories == null)
            {
                _packageCentral.Repositories = new PackageContainerListOfList();
                RepoListControl.RepoList = _packageCentral.Repositories;
            }

            if (!visible)
            {
                // disable completely
                RowDefinitonForRepoList.Height = new GridLength(0.0);
            }
            else
            {
                // enable, what has been stored
                RowDefinitonForRepoList.Height =
                        new GridLength(this.ColumnAasRepoGrid.ActualHeight / 2);
            }
        }

        public void PrepareDispEditEntity(
            AdminShellPackageEnv package, ListOfVisualElementBasic entities,
            bool editMode, bool hintMode, bool showIriMode,
            DispEditHighlight.HighlightFieldInfo hightlightField = null)
        {
            // determine some flags
            var tiCds = DisplayElements.SearchVisualElementOnMainDataObject(package?.AasEnv?.ConceptDescriptions) as
                VisualElementEnvironmentItem;

            // update element view?
            _dynamicMenu.Menu.Clear();
            var renderHints = DispEditEntityPanel.DisplayOrEditVisualAasxElement(
                    _packageCentral, entities, editMode, hintMode, showIriMode, tiCds?.CdSortOrder,
                    flyoutProvider: this,
                    appEventProvider: this,
                    hightlightField: hightlightField,
                    superMenu: _dynamicMenu.Menu);

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
            if (ElementTabControl.SelectedIndex != 0)
                Dispatcher.BeginInvoke((Action)(() => ElementTabControl.SelectedIndex = 0));

            // some entities require special handling
            if (entities?.ExactlyOne == true && entities.First() is VisualElementSubmodelElement sme &&
                sme?.theWrapper is AasCore.Aas3_0_RC02.File file)
            {
                ShowContent.IsEnabled = true;
                this.showContentPackageUri = file.Value;
                this.showContentPackageMime = file.ContentType;
                DragSource.Foreground = Brushes.Black;
            }

            if (entities?.ExactlyOne == true
                && this.theOnlineConnection != null && this.theOnlineConnection.IsValid() &&
                this.theOnlineConnection.IsConnected())
            {
                UpdateContent.IsEnabled = true;
                this.currentEntityForUpdate = entities.First();
            }
        }

        public void RedrawElementView(DispEditHighlight.HighlightFieldInfo hightlightField = null)
        {
            if (DisplayElements == null)
                return;

            // the AAS will cause some more visual effects
            var tvlaas = DisplayElements.SelectedItem as VisualElementAdminShell;
            if (_packageCentral.MainAvailable && tvlaas != null && tvlaas.theAas != null && tvlaas.theEnv != null)
            {
                // AAS
                // update graphic left

                // what is AAS specific?
                this.AasId.Text = WpfStringAddWrapChars(
                    AdminShellUtil.EvalToNonNullString("{0}", tvlaas.theAas.Id, "<id missing!>"));

                // what is asset specific?
                this.AssetPic.Source = null;
                this.AssetId.Text = "<id missing!>";
                var asset = tvlaas.theAas.AssetInformation;
                if (asset != null)
                {

                    // text id
                    if (asset.GlobalAssetId != null)
                        this.AssetId.Text = WpfStringAddWrapChars(
                            AdminShellUtil.EvalToNonNullString("{0}", asset.GlobalAssetId.GetAsIdentifier()));

                    // asset thumbnail
                    try
                    {
                        // identify which stream to use..
                        if (_packageCentral.MainAvailable)
                            try
                            {
                                using (var thumbStream = _packageCentral.Main.GetLocalThumbnailStream())
                                {
                                    // load image
                                    if (thumbStream != null)
                                    {
                                        var bi = new BitmapImage();
                                        bi.BeginInit();

                                        // See https://stackoverflow.com/a/5346766/1600678
                                        bi.CacheOption = BitmapCacheOption.OnLoad;

                                        bi.StreamSource = thumbStream;
                                        bi.EndInit();
                                        this.AssetPic.Source = bi;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                            }

                        if (this.theOnlineConnection != null && this.theOnlineConnection.IsValid() &&
                            this.theOnlineConnection.IsConnected())
                            try
                            {
                                using (var thumbStream = this.theOnlineConnection.GetThumbnailStream())
                                {
                                    if (thumbStream != null)
                                    {
                                        using (var ms = new MemoryStream())
                                        {
                                            thumbStream.CopyTo(ms);
                                            ms.Flush();
                                            var bitmapdata = ms.ToArray();

                                            var bi = (BitmapSource)new ImageSourceConverter().ConvertFrom(bitmapdata);
                                            this.AssetPic.Source = bi;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                            }

                    }
                    catch (Exception ex)
                    {
                        // no error, intended behaviour, as thumbnail might not exist / be faulty in some way
                        // (not violating the spec)
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }
                }
            }

            // for all, prepare the display
            PrepareDispEditEntity(
                _packageCentral.Main,
                DisplayElements.SelectedItems,
                 _mainMenu?.IsChecked("EditMenu") == true,
                 _mainMenu?.IsChecked("HintsMenu") == true,
                 _mainMenu?.IsChecked("ShowIriMenu") == true,
                hightlightField: hightlightField);

        }

        #endregion
        #region Callbacks
        //===============

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // making up "empty" picture
            this.AasId.Text = "<id unknown!>";
            this.AssetId.Text = "<id unknown!>";

            // logical main menu
            var logicalMainMenu = CreateMainMenu();

            // top level children have other color
            logicalMainMenu.DefaultForeground = AnyUiColors.Black;
            foreach (var mi in logicalMainMenu)
                if (mi is AasxMenuItem mii)
                    mii.Foreground = AnyUiColors.White;

            // WPF main menu
            _mainMenu = new AasxMenuWpf();
            _mainMenu.LoadAndRender(logicalMainMenu, MenuMain, this.CommandBindings, this.InputBindings);

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
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

            // adding the CEF Browser conditionally
            theContentBrowser.Start(Options.Curr.ContentHome, Options.Curr.InternalBrowser);
            CefContainer.Child = theContentBrowser.BrowserControl;

            // window size?
            if (Options.Curr.WindowLeft > 0) this.Left = Options.Curr.WindowLeft;
            if (Options.Curr.WindowTop > 0) this.Top = Options.Curr.WindowTop;
            if (Options.Curr.WindowWidth > 0) this.Width = Options.Curr.WindowWidth;
            if (Options.Curr.WindowHeight > 0) this.Height = Options.Curr.WindowHeight;
            if (Options.Curr.WindowMaximized)
                this.WindowState = WindowState.Maximized;

            // Timer for below
            System.Windows.Threading.DispatcherTimer MainTimer = new System.Windows.Threading.DispatcherTimer();
            MainTimer.Tick += async (s, a) =>
            {
                await MainTimer_Tick(s, a);
            };
            MainTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            MainTimer.Start();

            // attach result search
            ToolFindReplace.Flyout = this;
            ToolFindReplace.ResultSelected += ToolFindReplace_ResultSelected;
            ToolFindReplace.SetProgressBar += SetProgressBar;

            // Package Central starting ..
            _packageCentral.CentralRuntimeOptions = UiBuildRuntimeOptionsForMainAppLoad();

            // start with empty repository and load, if given by options
            RepoListControl.PackageCentral = _packageCentral;
            RepoListControl.FlyoutProvider = this;
            RepoListControl.ManageVisuElems = DisplayElements;
            this.UiAssertFileRepository(visible: false);

            // event viewer
            UserContrlEventCollection.FlyoutProvider = this;

            // LRU repository?
            var lruFn = PackageContainerListLastRecentlyUsed.BuildDefaultFilename();
            try
            {
                if (System.IO.File.Exists(lruFn))
                {
                    var lru = PackageContainerListLastRecentlyUsed.Load<PackageContainerListLastRecentlyUsed>(lruFn);
                    _packageCentral?.Repositories.Add(lru);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"while loading last recently used file {lruFn}");
            }

            // Repository pointed by the Options
            if (Options.Curr.AasxRepositoryFn.HasContent())
            {
                var fr2 = UiLoadFileRepository(Options.Curr.AasxRepositoryFn);
                if (fr2 != null)
                {
                    this.UiAssertFileRepository(visible: true);
                    _packageCentral.Repositories.AddAtTop(fr2);
                }
            }

            // what happens on a repo file click
            RepoListControl.FileDoubleClick += async (senderList, repo, fi) =>
            {
                // access
                if (repo == null || fi == null)
                    return;

                // safety?
                if (_mainMenu?.IsChecked("FileRepoLoadWoPrompt") == false)
                {
                    // ask double question
                    if (AnyUiMessageBoxResult.OK != MessageBoxFlyoutShow(
                            "Load file from AASX file repository?",
                            "AASX File Repository",
                            AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
                        return;
                }

                // start animation
                repo.StartAnimation(fi, PackageContainerRepoItem.VisualStateEnum.ReadFrom);

                // container options
                var copts = PackageContainerOptionsBase.CreateDefault(Options.Curr);
                if (fi.ContainerOptions != null)
                    copts = fi.ContainerOptions;

                // try load ..
#if TODO
                if (repo is PackageContainerAasxFileRepository restRepository)
                {
                    if (restRepository.IsAspNetConnection)
                    {
                        var container = await restRepository.LoadAasxFileFromServer(fi.PackageId, _packageCentral.CentralRuntimeOptions);
                        if (container != null)
                        {
                            UiLoadPackageWithNew(_packageCentral.MainItem,
                            takeOverContainer: container, onlyAuxiliary: false,
                            storeFnToLRU: fi.PackageId);
                        }

                        Log.Singleton.Info($"Successfully loaded AASX Package with PackageId {fi.PackageId}");

                        if (senderList is PackageContainerListControl pclc)
                            pclc.RedrawStatus();
                    }
                }
                else
#endif
                {
                    var location = repo.GetFullItemLocation(fi.Location);
                    if (location == null)
                        return;
                    Log.Singleton.Info($"Auto-load file from repository {location} into container");

                    try
                    {
                        var container = await PackageContainerFactory.GuessAndCreateForAsync(
                            _packageCentral,
                            location,
                            location,
                            overrideLoadResident: true,
                            takeOver: fi,
                            fi.ContainerList,
                            containerOptions: copts,
                            runtimeOptions: _packageCentral.CentralRuntimeOptions);

                        if (container == null)
                            Log.Singleton.Error($"Failed to load AASX from {location}");
                        else
                            UiLoadPackageWithNew(_packageCentral.MainItem,
                                takeOverContainer: container, onlyAuxiliary: false,
                                storeFnToLRU: location);

                        Log.Singleton.Info($"Successfully loaded AASX {location}");

                        if (senderList is PackageContainerListControl pclc)
                            pclc.RedrawStatus();
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, $"When auto-loading {location}");
                    }
                }


            };

            // what happens on a file drop -> dispatch
            RepoListControl.FileDrop += (senderList, fr, files) =>
            {
                // access
                if (files == null || files.Length < 1)
                    return;

                // more than one?
                foreach (var fn in files)
                {
                    // repo?
                    var ext = Path.GetExtension(fn).ToLower();
                    if (ext == ".json")
                    {
                        // try handle as repository
                        var newRepo = UiLoadFileRepository(fn);
                        if (newRepo != null)
                        {
                            _packageCentral.Repositories.AddAtTop(newRepo);
                        }
                        // no more files ..
                        return;
                    }

                    // aasx?
                    if (fr != null && ext == ".aasx")
                    {
                        // add?
                        fr.AddByAasxFn(_packageCentral, fn);
                    }
                }
            };

#if __Create_Demo_Daten
            if (true)
            {
                fr = AasxFileRepository.CreateDemoData();
            }
#endif

            // initialize menu
            _mainMenu?.SetChecked("FileRepoLoadWoPrompt", Options.Curr.LoadWithoutPrompt);
            _mainMenu?.SetChecked("ShowIriMenu", Options.Curr.ShowIdAsIri);
            _mainMenu?.SetChecked("VerboseConnect", Options.Curr.VerboseConnect);
            _mainMenu?.SetChecked("AnimateElements", Options.Curr.AnimateElements);
            _mainMenu?.SetChecked("ObserveEvents", Options.Curr.ObserveEvents);
            _mainMenu?.SetChecked("CompressEvents", Options.Curr.CompressEvents);

            // the UI application might receive events from items in the package central
            _packageCentral.ChangeEventHandler = (data) =>
            {
                if (data.Reason == PackCntChangeEventReason.Exception)
                    Log.Singleton.Info("PackageCentral events: " + data.Info);
                DisplayElements.PushEvent(new AnyUiLambdaActionPackCntChange() { Change = data });
                return false;
            };

            // nearly last task here ..
            Log.Singleton.Info("Application started ..");

            // start with a new file
            _packageCentral.MainItem.New();
            RedrawAllAasxElements();

            // pump all pending log messages (from plugins) into the
            // log / status line, before setting the last information
            MainTimer_HandleLogMessages();

            // Try to load?            
            if (Options.Curr.AasxToLoad != null)
            {
                var location = Options.Curr.AasxToLoad;
                try
                {
                    Log.Singleton.Info($"Auto-load file at application start " +
                        $"from {location} into container");

                    var container = await PackageContainerFactory.GuessAndCreateForAsync(
                        _packageCentral,
                        location,
                        location,
                        overrideLoadResident: true,
                        containerOptions: PackageContainerOptionsBase.CreateDefault(Options.Curr),
                        runtimeOptions: _packageCentral.CentralRuntimeOptions);

                    if (container == null)
                        Log.Singleton.Error($"Failed to auto-load AASX from {location}");
                    else
                        UiLoadPackageWithNew(_packageCentral.MainItem,
                            takeOverContainer: container, onlyAuxiliary: false, indexItems: true);

                    Log.Singleton.Info($"Successfully auto-loaded AASX {location}");
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"When auto-loading {location}");
                }
            }

            // open last UI elements
            if (Options.Curr.ShowEvents)
                PanelConcurrentSetVisibleIfRequired(true, targetEvents: true);

            // script file to launch?
            if (Options.Curr.ScriptFn.HasContent())
            {
                try
                {
                    Log.Singleton.Info("Opening and executing '{0}' for script commands.", Options.Curr.ScriptFn);
                    if (_aasxScript == null)
                        _aasxScript = new AasxScript();
                    var script = System.IO.File.ReadAllText(Options.Curr.ScriptFn);
                    _aasxScript.StartEnginBackground(
                        script, Options.Curr.ScriptLoglevel,
                        _mainMenu?.Menu, this);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"when executing script file {Options.Curr.ScriptFn}");
                }
            }

            // script file to launch?
            if (Options.Curr.ScriptCmd.HasContent())
            {
                try
                {
                    Log.Singleton.Info("Executing '{0}' as direct script commands.", Options.Curr.ScriptCmd);
                    if (_aasxScript == null)
                        _aasxScript = new AasxScript();
                    _aasxScript.StartEnginBackground(
                        Options.Curr.ScriptCmd, Options.Curr.ScriptLoglevel,
                        _mainMenu?.Menu, this);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"when executing script file {Options.Curr.ScriptCmd}");
                }
            }
        }

        private void ToolFindReplace_ResultSelected(AasxSearchUtil.SearchResultItem resultItem)
        {
            // have a result?
            if (resultItem == null || resultItem.businessObject == null)
                return;

            // for valid display, app needs to be in edit mode
            if (_mainMenu.IsChecked("EditMenu") != true)
            {
                this.MessageBoxFlyoutShow(
                    "The application needs to be in edit mode to show found entities correctly. Aborting.",
                    "Find and Replace",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Hand);
                return;
            }

            // add to "normal" event quoue
            DispEditEntityPanel.AddWishForOutsideAction(
                new AnyUiLambdaActionRedrawAllElements(
                    nextFocus: resultItem.businessObject,
                    highlightField: new DispEditHighlight.HighlightFieldInfo(
                        resultItem.containingObject, resultItem.foundObject, resultItem.foundHash),
                    onlyReFocus: true));
        }

        private void MainTimer_HandleLogMessages()
        {
            // pop log messages from the plug-ins into the Stored Prints in Log
            Plugins.PumpPluginLogsIntoLog(this.FlyoutLoggingPush);

            // check for Stored Prints in Log
            StoredPrint sp;
            while ((sp = Log.Singleton.PopLastShortTermPrint()) != null)
            {
                // pop
                Message.Content = "" + sp.msg;

                // display
                switch (sp.color)
                {
                    default:
                        throw ExhaustiveMatch.Failed(sp.color);
                    case StoredPrint.Color.Black:
                        {
                            Message.Background = Brushes.White;
                            Message.Foreground = Brushes.Black;
                            Message.FontWeight = FontWeights.Normal;
                            break;
                        }
                    case StoredPrint.Color.Blue:
                        {
                            Message.Background = Brushes.LightBlue;
                            Message.Foreground = Brushes.Black;
                            Message.FontWeight = FontWeights.Normal;
                            break;
                        }
                    case StoredPrint.Color.Yellow:
                        {
                            Message.Background = Brushes.Yellow;
                            Message.Foreground = Brushes.Black;
                            Message.FontWeight = FontWeights.Bold;
                            break;
                        }
                    case StoredPrint.Color.Red:
                        {
                            Message.Background = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                            Message.Foreground = Brushes.White;
                            Message.FontWeight = FontWeights.Bold;
                            break;
                        }
                }

                // message window
                _messageReportWindow?.AddStoredPrint(sp);
            }

            // always tell the errors
            var ne = Log.Singleton.NumberErrors;
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

        private async Task MainTimer_HandleLambdaAction(AnyUiLambdaActionBase lab)
        {
            // nothing
            if (lab == null)
                return;

            // recurse??
            if (lab is AnyUiLambdaActionList list && list.Actions != null)
                foreach (var ac in list.Actions)
                    await MainTimer_HandleLambdaAction(ac);

            // what to do?
            if (lab is AnyUiLambdaActionRedrawAllElementsBase wish)
            {
                // 2022-02-28: Try to kee focus
                if (wish.RedrawCurrentEntity && wish.NextFocus == null)
                {
                    // figure out the current business object
                    if (DisplayElements != null && DisplayElements.SelectedItem != null &&
                        DisplayElements.SelectedItem != null)
                        wish.NextFocus = DisplayElements.SelectedItem.GetMainDataObject();
                }

                // edit mode affects the total element view
                if (!wish.OnlyReFocus)
                    RedrawAllAasxElements();

                // the selection will be shifted ..
                if (wish.NextFocus != null && DisplayElements != null)
                {
                    // for later search in visual elements, expand them all in order to be absolutely 
                    // sure to find business object
                    DisplayElements.ExpandAllItems();

                    // now: search
                    DisplayElements.TrySelectMainDataObject(wish.NextFocus, wish.IsExpanded);
                }

                // fake selection
                DispEditHighlight.HighlightFieldInfo hfi = null;
                if (lab is AnyUiLambdaActionRedrawAllElements wishhl)
                    hfi = wishhl.HighlightField;
                RedrawElementView(hightlightField: hfi);

                // ok
                DisplayElements.Refresh();
                ContentTakeOver.IsEnabled = false;
            }

            if (lab is AnyUiLambdaActionContentsChanged)
            {
                // enable button
                ContentTakeOver.IsEnabled = true;
            }

            if (lab is AnyUiLambdaActionContentsTakeOver)
            {
                // rework list
                ContentTakeOver_Click(null, null);
            }

            if (lab is AnyUiLambdaActionNavigateTo tempNavTo)
            {
                // do some more adoptions
                var rf = tempNavTo.targetReference.Copy();

                if (tempNavTo.translateAssetToAAS
                    && rf.Keys.Count == 1
                    && rf.Keys.First().Type == AasCore.Aas3_0_RC02.KeyTypes.GlobalReference) //TODO:jtikekar KeyType.AssetInformation
                {
                    // try to find possible environments containg the asset and try making
                    // replacement
                    foreach (var pe in _packageCentral.GetAllPackageEnv())
                    {
                        if (pe?.AasEnv?.AssetAdministrationShells == null)
                            continue;

                        foreach (var aas in pe.AasEnv.AssetAdministrationShells)
                            if (aas.AssetInformation?.GlobalAssetId.Matches(rf, MatchMode.Relaxed) == true)
                            {
                                rf = aas.GetReference();
                                break;
                            }
                    }
                }

                // handle it by UI
                await UiHandleNavigateTo(rf, alsoDereferenceObjects: tempNavTo.alsoDereferenceObjects);
            }

            if (lab is AnyUiLambdaActionDisplayContentFile tempDispCont)
            {
                try
                {
                    BrowserDisplayLocalFile(tempDispCont.fn, tempDispCont.mimeType,
                        preferInternal: tempDispCont.preferInternalDisplay);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(
                        ex, $"While displaying content file {tempDispCont.fn} requested by lambda");
                }
            }

            if (lab is AnyUiLambdaActionPackCntChange
                || lab is AnyUiLambdaActionSelectMainObjects)
            {
                DisplayElements.PushEvent(lab);
            }

            if (lab is AnyUiLambdaActionPluginUpdateAnyUi update)
            {
                // A plugin asks to re-render an exisiting panel.
                // Can get this information?
                var renderedInfo = DispEditEntityPanel.GetLastRenderedRoot();

                if (renderedInfo != null
                    && renderedInfo.Item2 is AnyUiPanel renderedPanel
                    && renderedPanel.Children != null
                    && renderedPanel.Children.Count > 0)
                {
                    // first step: invoke plugin?
                    var plugin = Plugins.FindPluginInstance(update.PluginName);
                    if (plugin != null && plugin.HasAction("update-anyui-visual-extension"))
                    {
                        try
                        {
                            plugin.InvokeAction(
                                "update-anyui-visual-extension", renderedPanel, renderedInfo.Item1,
                                AnyUiDisplayContextWpf.SessionSingletonWpf);
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(ex,
                                $"update AnyUI based visual extension for plugin {update.PluginName}");
                        }
                    }

                    // 2nd step: redisplay                                                          
                    DispEditEntityPanel.RedisplayRenderedRoot(
                        renderedPanel,
                        update.UpdateMode,
                        useInnerGrid: update.UseInnerGrid);
                }
                else
                {
                    // hard re-display
                    throw new NotImplementedException();
                }
            }
        }

        private async Task MainTimer_HandleEntityPanel()
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

                        await MainTimer_HandleLambdaAction(temp);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While responding to a user interaction");
            }
        }

        private async Task<AasCore.Aas3_0_RC02.IReferable> LoadFromFileRepository(PackageContainerRepoItem fi,
            AasCore.Aas3_0_RC02.Reference requireReferable = null)
        {
            // access single file repo
            var fileRepo = _packageCentral.Repositories.FindRepository(fi);
            if (fileRepo == null)
                return null;

            // which file?
            var location = fileRepo.GetFullItemLocation(fi?.Location);
            if (location == null)
                return null;

            // try load (in the background/ RAM first..
            PackageContainerBase container = null;
            try
            {
                Log.Singleton.Info($"Auto-load file from repository {location} into container");
                container = await PackageContainerFactory.GuessAndCreateForAsync(
                    _packageCentral,
                    location,
                    location,
                    overrideLoadResident: true,
                    null, null,
                    PackageContainerOptionsBase.CreateDefault(Options.Curr),
                    runtimeOptions: _packageCentral.CentralRuntimeOptions);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"When auto-loading {location}");
            }

            // if successfull ..
            if (container != null)
            {
                // .. try find business object!
                AasCore.Aas3_0_RC02.IReferable bo = null;
                if (requireReferable != null)
                    bo = container.Env?.AasEnv.FindReferableByReference(requireReferable);

                // only proceed, if business object was found .. else: close directly
                if (requireReferable != null && bo == null)
                    container.Close();
                else
                {
                    // make sure the user wants to change
                    if (_mainMenu?.IsChecked("FileRepoLoadWoPrompt") != false)
                    {
                        // ask double question
                        if (AnyUiMessageBoxResult.OK != MessageBoxFlyoutShow(
                                "Load file from AASX file repository?",
                                "AASX File Repository",
                                AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
                            return null;
                    }

                    // start animation
                    fileRepo.StartAnimation(fi, PackageContainerRepoItem.VisualStateEnum.ReadFrom);

                    // activate
                    UiLoadPackageWithNew(_packageCentral.MainItem,
                        takeOverContainer: container, onlyAuxiliary: false);

                    Log.Singleton.Info($"Successfully loaded AASX {location}");
                }

                // return bo to focus
                return bo;
            }

            return null;
        }

        private void UiHandleReRenderAnyUiInEntityPanel(
            string pluginName, AnyUiRenderMode mode, bool useInnerGrid = false)
        {
            // A plugin asks to re-render an exisiting panel.
            // Can get this information?
            var renderedInfo = DispEditEntityPanel.GetLastRenderedRoot();

            if (renderedInfo != null
                && renderedInfo.Item2 is AnyUiPanel renderedPanel
                && renderedPanel.Children != null
                && renderedPanel.Children.Count > 0)
            {
                // first step: invoke plugin?
                // Note: is OK to have plugin name to null in order to disable calling plugin
                var plugin = Plugins.FindPluginInstance(pluginName);
                if (plugin != null && plugin.HasAction("update-anyui-visual-extension"))
                {
                    try
                    {
                        plugin.InvokeAction(
                            "update-anyui-visual-extension", renderedPanel, null,
                            AnyUiDisplayContextWpf.SessionSingletonWpf);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex,
                            $"update AnyUI based visual extension for plugin {pluginName}");
                    }
                }

                // 2nd step: redisplay
                DispEditEntityPanel.RedisplayRenderedRoot(
                    renderedPanel,
                    mode: mode,
                    useInnerGrid: useInnerGrid);
            }
            else
            {
                // hard re-display
                throw new NotImplementedException();
            }
        }

        private async Task UiHandleNavigateTo(
            AasCore.Aas3_0_RC02.Reference targetReference,
            bool alsoDereferenceObjects = true)
        {
            // access
            if (targetReference == null || targetReference.Keys.Count < 1)
                return;

            // make a copy of the Reference for searching
            VisualElementGeneric veFound = null;
            var work = targetReference.Copy();

            try
            {
                // remember some further supplementary search information
                var sri = ListOfVisualElement.StripSupplementaryReferenceInformation(work);
                work = sri.CleanReference;

                // for later search in visual elements, expand them all in order to be absolutely 
                // sure to find business object
                this.DisplayElements.ExpandAllItems();

                // incrementally make it unprecise
                while (work.Keys.Count > 0)
                {
                    // try to find a business object in the package
                    AasCore.Aas3_0_RC02.IReferable bo = null;
                    if (_packageCentral.MainAvailable && _packageCentral.Main.AasEnv != null)
                        bo = _packageCentral.Main.AasEnv.FindReferableByReference(work);

                    // if not, may be in aux package
                    if (bo == null && _packageCentral.Aux != null && _packageCentral.Aux.AasEnv != null)
                        bo = _packageCentral.Aux.AasEnv.FindReferableByReference(work);

                    // if not, may look into the AASX file repo
                    if (bo == null && _packageCentral.Repositories != null)
                    {
                        // find?
                        PackageContainerRepoItem fi = null;
                        if (work.Keys[0].Type == AasCore.Aas3_0_RC02.KeyTypes.GlobalReference) //TODO: jtikekar KeyTypes.AssetInformation
                            fi = _packageCentral.Repositories.FindByAssetId(work.Keys[0].Value.Trim());
                        if (work.Keys[0].Type == AasCore.Aas3_0_RC02.KeyTypes.AssetAdministrationShell)
                            fi = _packageCentral.Repositories.FindByAasId(work.Keys[0].Value.Trim());

                        bo = await LoadFromFileRepository(fi, work);
                    }

                    // still yes?
                    if (bo != null)
                    {
                        // try to look up in visual elements
                        if (this.DisplayElements != null)
                        {
                            var ve = this.DisplayElements.SearchVisualElementOnMainDataObject(bo,
                                alsoDereferenceObjects: alsoDereferenceObjects, sri: sri);
                            if (ve != null)
                            {
                                veFound = ve;
                                break;
                            }
                        }
                    }

                    // make it more unprecice
                    work.Keys.RemoveAt(work.Keys.Count - 1);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While retrieving element requested for navigate to");
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
                else
                {
                    // everything is in default state, push adequate button history
                    var veTop = this.DisplayElements.GetDefaultVisualElement();
                    ButtonHistory.Push(veTop);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While displaying element requested for navigate to");
            }
        }

        private async Task HandleApplicationEvent(
            AasxIntegrationBase.AasxPluginResultEventBase evt,
            Plugins.PluginInstance pluginInstance)
        {
            try
            {
                // Navigate To
                //============

                if (evt is AasxIntegrationBase.AasxPluginResultEventNavigateToReference evtNavTo
                    && evtNavTo.targetReference != null && evtNavTo.targetReference.Keys.Count > 0)
                {
                    await UiHandleNavigateTo(evtNavTo.targetReference);
                }

                // Display Content Url
                //====================

                if (evt is AasxIntegrationBase.AasxPluginResultEventDisplayContentFile evtDispCont
                    && evtDispCont.fn != null)
                    try
                    {
                        BrowserDisplayLocalFile(evtDispCont.fn, evtDispCont.mimeType,
                            preferInternal: evtDispCont.preferInternalDisplay);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(
                            ex, $"While displaying content file {evtDispCont.fn} requested by plug-in");
                    }

                // Redraw All
                //===========

                if (evt is AasxIntegrationBase.AasxPluginResultEventRedrawAllElements)
                {
                    if (DispEditEntityPanel != null)
                    {
                        // figure out the current business object
                        object nextFocus = null;
                        if (DisplayElements != null && DisplayElements.SelectedItem != null &&
                            DisplayElements.SelectedItem != null)
                            nextFocus = DisplayElements.SelectedItem.GetMainDataObject();

                        // add to "normal" event quoue
                        DispEditEntityPanel.AddWishForOutsideAction(
                            new AnyUiLambdaActionRedrawAllElements(nextFocus));
                    }
                }

                // Select AAS entity
                //=======================

                var evSelectEntity = evt as AasxIntegrationBase.AasxPluginResultEventSelectAasEntity;
                if (evSelectEntity != null)
                {
                    var uc = new SelectAasEntityFlyout(
                        _packageCentral, PackageCentral.Selector.MainAuxFileRepo,
                        evSelectEntity.filterEntities);
                    this.StartFlyoverModal(uc);
                    if (uc.DiaData.ResultKeys != null)
                    {
                        // formulate return event
                        var retev = new AasxIntegrationBase.AasxPluginEventReturnSelectAasEntity();
                        retev.sourceEvent = evt;
                        retev.resultKeys = uc.DiaData.ResultKeys;

                        // fire back
                        pluginInstance?.InvokeAction("event-return", retev,
                            AnyUiDisplayContextWpf.SessionSingletonWpf);
                    }
                }

                // Select File
                //============

                if (evt is AasxIntegrationBase.AasxPluginResultEventSelectFile fileSel)
                {
                    // ask
                    if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                    FileDialog dlg = null;
                    if (fileSel.SaveDialogue)
                        dlg = new Microsoft.Win32.SaveFileDialog();
                    else
                        dlg = new Microsoft.Win32.OpenFileDialog();
                    dlg.InitialDirectory = DetermineInitialDirectory(_packageCentral.MainItem.Filename);
                    if (fileSel.Title != null)
                        dlg.Title = fileSel.Title;
                    if (fileSel.FileName != null)
                        dlg.FileName = fileSel.FileName;
                    if (fileSel.DefaultExt != null)
                        dlg.DefaultExt = fileSel.DefaultExt;
                    if (fileSel.Filter != null)
                        dlg.Filter = fileSel.Filter;
                    if (dlg is Microsoft.Win32.OpenFileDialog ofd)
                        ofd.Multiselect = fileSel.MultiSelect;
                    var res = dlg.ShowDialog();
                    if (Options.Curr.UseFlyovers) this.CloseFlyover();

                    // act
                    if (res == true)
                    {
                        // formulate return event
                        var retev = new AasxIntegrationBase.AasxPluginEventReturnSelectFile();
                        retev.sourceEvent = evt;
                        retev.FileNames = dlg.FileNames;

                        // fire back
                        pluginInstance?.InvokeAction("event-return", retev,
                            AnyUiDisplayContextWpf.SessionSingletonWpf);
                    }
                }

                // Message Box
                //============

                if (evt is AasxIntegrationBase.AasxPluginResultEventMessageBox evMsgBox)
                {
                    // modal
                    var uc = new MessageBoxFlyout(evMsgBox.Message, evMsgBox.Caption,
                                    evMsgBox.Buttons, evMsgBox.Image);
                    this.StartFlyoverModal(uc);

                    // fire back
                    pluginInstance?.InvokeAction("event-return",
                        new AasxIntegrationBase.AasxPluginEventReturnMessageBox()
                        {
                            sourceEvent = evt,
                            Result = uc.Result
                        },
                        AnyUiDisplayContextWpf.SessionSingletonWpf);
                }

                // Re-render Any UI Panels
                //========================

                if (evt is AasxIntegrationBase.AasxPluginEventReturnUpdateAnyUi update)
                {
                    UiHandleReRenderAnyUiInEntityPanel(update.PluginName, update.Mode, useInnerGrid: true);
                }

#endregion
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"While responding to a event; may be from plug-in {"" + pluginInstance?.name}");
            }
        }

        private List<AasxIntegrationBase.AasxPluginResultEventBase> _applicationEvents
            = new List<AasxPluginResultEventBase>();

        public void PushApplicationEvent(AasxIntegrationBase.AasxPluginResultEventBase evt)
        {
            if (evt == null)
                return;
            _applicationEvents.Add(evt);
        }

        private async Task MainTimer_HandleApplicationEvents()
        {
            // check if a plug-in has some work to do ..
            foreach (var lpi in Plugins.LoadedPlugins.Values)
            {
                var evt = lpi.InvokeAction("get-events") as AasxIntegrationBase.AasxPluginResultEventBase;
                if (evt != null)
                    await HandleApplicationEvent(evt, lpi);
            }

            // check for application events from main app
            while (_applicationEvents.Count > 0)
            {
                var evt = _applicationEvents[0];
                _applicationEvents.RemoveAt(0);
                await HandleApplicationEvent(evt, null);
            }
        }

        public class EventHandlingStatus
        {
            public DateTime LastQueuedEventRequest = DateTime.Now;
            public DateTime LastReceivedEventTimeStamp = DateTime.MinValue;
            public bool UpdateValuePending = false;
            public bool GetEventsPending = false;
            public int UserErrorsIndicated = 0;
            public bool UserErrorsSuppress = false;

            public void Reset()
            {
                LastQueuedEventRequest = DateTime.Now;
                UpdateValuePending = false;
                GetEventsPending = false;
                UserErrorsIndicated = 0;
                UserErrorsSuppress = false;
            }
        }

        private AnimateDemoValues _mainTimer_AnimateDemoValues = new AnimateDemoValues();

        private void MainTimer_CheckAnimationElements(
            double deltaSecs,
            AasCore.Aas3_0_RC02.Environment env,
            IndexOfSignificantAasElements significantElems)
        {
            // trivial
            if (env == null || significantElems == null || _mainMenu?.IsChecked("AnimateElements") != true)
                return;

            // find elements?
            foreach (var rec in significantElems.Retrieve(env, SignificantAasElement.ValueAnimation))
            {
                // valid?
                if (rec?.Reference == null || rec.Reference.Keys.Count < 1 || rec.LiveObject == null)
                    continue;

                // which SME?
                if (rec.LiveObject is AasCore.Aas3_0_RC02.Property prop)
                {
                    _mainTimer_AnimateDemoValues.Animate(prop,
                        emitEvent: (prop2, evi2) =>   
                        {
                            // Animate the event visually; create a change event for this.
                            // Note: this might by not ideal, final state
                            /* TODO (MIHO, 2021-10-28): Check, if a better solution exists 
                             * to instrument event updates in a way that they're automatically
                             * visualized */
                            DisplayElements.PushEvent(new AnyUiLambdaActionPackCntChange()
                            {
                                Change = new PackCntChangeEventData()
                                {
                                    Container = _packageCentral.MainItem.Container,
                                    Reason = PackCntChangeEventReason.ValueUpdateSingle,
                                    ThisElem = prop2,
                                    ParentElem = prop2?.Parent,
                                    Info = "Animated value update"
                                }
                            });
                        });
                }


            }
        }

        private void MainTimer_CheckDiaryDateToEmitEvents(
            DateTime lastTime,
            AasCore.Aas3_0_RC02.Environment env,
            IndexOfSignificantAasElements significantElems,
            bool directEmit)
        {
            // trivial
            if (env == null || significantElems == null || _mainMenu?.IsChecked("ObserveEvents") != true)
                return;

            // do this twice
            for (int i = 0; i < 2; i++)
            {
                // divider
                var see = (new[] {
                    SignificantAasElement.EventStructureChangeOutwards,
                    SignificantAasElement.EventUpdateValueOutwards})[i];

                // update events?
                foreach (var rec in significantElems.Retrieve(env, see))
                {
                    // valid?
                    if (rec?.Reference == null || rec.Reference.Keys.Count < 1 || rec.LiveObject == null)
                        continue;
                    var refEv = rec.LiveObject as AasCore.Aas3_0_RC02.BasicEventElement;
                    if (refEv == null)
                        continue;

                    // now, find the observable (with timestamping!)
                    var observable = (IDiaryData)env.FindReferableByReference(refEv.Observed);  

                    // some special cases
                    if (true == refEv.Observed?.Matches(
                            AasCore.Aas3_0_RC02.KeyTypes.GlobalReference, "AASENV",
                            MatchMode.Relaxed))
                    {
                        observable = env;
                    }

                    // diary data available
                    if (observable?.DiaryData == null)
                        continue;

                    // get the flags
                    var newCreate = observable.DiaryData
                        .TimeStamp[(int)DiaryDataDef.TimeStampKind.Create] >= lastTime;

                    var newUpdate = observable.DiaryData
                        .TimeStamp[(int)DiaryDataDef.TimeStampKind.Update] >= lastTime;

                    // first check
                    if (!newCreate && !newUpdate)
                        continue;

                    // prepare event payloads
                    var plStruct = new AasPayloadStructuralChange();
                    var plUpdate = new AasPayloadUpdateValue();

                    // for the overall change check, we rely on the timestamping
                    if ((i == 0) || ((i == 1) && newUpdate))
                    {
                        // closure logic
                        var storedI = i;

                        //if (observable is IRecurseOnReferables recurse)
                        if (observable is AasCore.Aas3_0_RC02.IReferable referable)
                            referable.RecurseOnReferables(null,
                                includeThis: true,
                                lambda: (o, parents, rf) =>
                                {
                                    // further interest?
                                    if (rf == null || rf.DiaryData == null ||
                                    ((rf.DiaryData.TimeStamp[(int)DiaryDataDef.TimeStampKind.Create]
                                       < lastTime)
                                      &&
                                      (rf.DiaryData.TimeStamp[(int)DiaryDataDef.TimeStampKind.Update]
                                       < lastTime)))
                                        return false;

                                    // yes, inspect further and also go deeper
                                    if (rf.DiaryData.Entries != null)
                                    {
                                        var todel = new List<IAasDiaryEntry>();
                                        foreach (var de in rf.DiaryData.Entries)
                                        {
                                            if (storedI == 0 && de is AasPayloadStructuralChangeItem sci)
                                            {
                                                // TODO (MIHO, 2021-10-09): prepare path to be relative

                                                // queue event
                                                plStruct.Changes.Add(sci);

                                                // delete
                                                todel.Add(de);
                                            }

                                            if (storedI == 1 && de is AasPayloadUpdateValueItem uvi)
                                            {
                                                // TODO (MIHO, 2021-10-09): prepare path to be relative

                                                // queue event
                                                plUpdate.Values.Add(uvi);

                                                // delete
                                                todel.Add(de);
                                            }
                                        }
                                        foreach (var de in todel)
                                            rf.DiaryData.Entries.Remove(de);
                                    }

                                    // deeper
                                    return true;
                                });
                        if (observable is AasCore.Aas3_0_RC02.Environment environment)
                            environment.RecurseOnReferables(null,
                                includeThis: true,
                                lambda: (o, parents, rf) =>
                                {
                                    // further interest?
                                    if (rf == null || rf.DiaryData == null ||
                                    ((rf.DiaryData.TimeStamp[(int)DiaryDataDef.TimeStampKind.Create]
                                       < lastTime)
                                      &&
                                      (rf.DiaryData.TimeStamp[(int)DiaryDataDef.TimeStampKind.Update]
                                       < lastTime)))
                                        return false;

                                    // yes, inspect further and also go deeper
                                    if (rf.DiaryData.Entries != null)
                                    {
                                        var todel = new List<IAasDiaryEntry>();
                                        foreach (var de in rf.DiaryData.Entries)
                                        {
                                            if (storedI == 0 && de is AasPayloadStructuralChangeItem sci)
                                            {
                                                // TODO (MIHO, 2021-10-09): prepare path to be relative

                                                // queue event
                                                plStruct.Changes.Add(sci);

                                                // delete
                                                todel.Add(de);
                                            }

                                            if (storedI == 1 && de is AasPayloadUpdateValueItem uvi)
                                            {
                                                // TODO (MIHO, 2021-10-09): prepare path to be relative

                                                // queue event
                                                plUpdate.Values.Add(uvi);

                                                // delete
                                                todel.Add(de);
                                            }
                                        }
                                        foreach (var de in todel)
                                            rf.DiaryData.Entries.Remove(de);
                                    }

                                    // deeper
                                    return true;
                                });
                    }

                    // send event?
                    if (plStruct.Changes.Count < 1 && plUpdate.Values.Count < 1)
                        continue;

                    // send event
                    var ev = new AasEventMsgEnvelope(
                        DateTime.UtcNow,
                        source: refEv.GetReference(),
                        sourceSemanticId: refEv.SemanticId,
                        observableReference: refEv.Observed,
                        //observableSemanticId: (observable as IGetSemanticId)?.GetSemanticId());
                        observableSemanticId: null); // TODO:jtikekar IDiaryData support

                    if (plStruct.Changes.Count >= 1)
                        ev.PayloadItems.Add(plStruct);

                    if (plUpdate.Values.Count >= 1)
                        ev.PayloadItems.Add(plUpdate);

                    // emit it to PackageCentral or to buffer?
                    if (directEmit)
                        _packageCentral?.PushEvent(ev);
                    else
                        _eventCompressor?.Push(ev);
                }
            }
        }

        protected EventHandlingStatus _eventHandling = new EventHandlingStatus();

        private void MainTimer_PeriodicalTaskForSelectedEntity()
        {
            // some container options are required
            var copts = _packageCentral?.MainItem?.Container?.ContainerOptions;

            //
            // Investigate on Update Value Events
            // Note: for the time being, Events will be only valid, if Event and observed entity are 
            // within the SAME Submodel
            //
            var veSelected = DisplayElements.SelectedItem;

            if (veSelected != null
                && true == copts?.StayConnected
                && Options.Curr.StayConnectOptions.HasContent()
                && Options.Curr.StayConnectOptions.ToUpper().Contains("SIM")
                && !_eventHandling.UpdateValuePending
                && (DateTime.Now - _eventHandling.LastQueuedEventRequest).TotalMilliseconds > copts.UpdatePeriod)
            {
                _eventHandling.LastQueuedEventRequest = DateTime.Now;

                try
                {
                    // for update values, do not concern about plugins, but use superior Submodel,
                    // as they will relate to this
                    var veSubject = veSelected;
                    if (veSelected is VisualElementPluginExtension)
                        veSubject = veSelected.Parent;

                    // now, filter for know applications
                    if (!(veSubject is VisualElementSubmodelRef || veSubject is VisualElementSubmodelElement))
                        return;

                    // will always require a root Submodel
                    var smrSel = veSelected.FindFirstParent((ve) => (ve is VisualElementSubmodelRef), includeThis: true)
                        as VisualElementSubmodelRef;
                    if (smrSel != null && smrSel.theSubmodel != null)
                    {
                        // parents need to be set
                        var rootSm = smrSel.theSubmodel;
                        rootSm.SetAllParents();

                        // check, if the Submodel has interesting events
                        foreach (var ev in smrSel.theSubmodel.SubmodelElements.FindDeep<AasCore.Aas3_0_RC02.BasicEventElement>((x) =>
                            (true == x?.SemanticId?.Matches(
                                AasxPredefinedConcepts.AasEvents.Static.CD_UpdateValueOutwards.Id
                                ))))
                        {
                            // Submodel defines an events for outgoing value updates -> does the observed scope
                            // lie in the selection?
                            var klObserved = ev.Observed?.Keys;
                            var klSelected = veSubject.BuildKeyListToTop(includeAas: false);
                            // no, klSelected shall lie in klObserved
                            if (klObserved != null && klSelected != null &&
                                klSelected.StartsWith(klObserved,
                                emptyIsTrue: false, matchMode: MatchMode.Relaxed))
                            {
                                // take a shortcut
                                if (_packageCentral?.MainItem?.Container is PackageContainerNetworkHttpFile cntHttp
                                    && cntHttp.ConnectorPrimary is PackageConnectorHttpRest connRest)
                                {
                                    Task.Run(async () =>
                                    {
                                        try
                                        {
                                            var evSnd = await
                                                connRest.SimulateUpdateValuesEventByGetAsync(
                                                    smrSel.theSubmodel,
                                                    ev,
                                                    veSubject.GetDereferencedMainDataObject() as AasCore.Aas3_0_RC02.IReferable,
                                                    timestamp: DateTime.Now,
                                                    topic: "MY-TOPIC",
                                                    subject: "ANY-SUBJECT");
                                            if (evSnd)
                                            {
                                                _eventHandling.UpdateValuePending = true;
                                                _eventHandling.UserErrorsSuppress = false;
                                                _eventHandling.UserErrorsIndicated = 0;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            if (!_eventHandling.UserErrorsSuppress)
                                            {
                                                Log.Singleton.Error(ex,
                                                    "periodically triggering event for simulated update (time-out)");
                                                _eventHandling.UserErrorsIndicated++;

                                                if (_eventHandling.UserErrorsIndicated > 3)
                                                {
                                                    Log.Singleton.Info("Too many repetitive time-outs. Disabling!");
                                                    _eventHandling.UserErrorsSuppress = true;
                                                }
                                            }
                                        }
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "periodically checking for triggering events");
                }
            }

            // Kick off all event updates?
            if (copts != null
                && Options.Curr.StayConnectOptions.HasContent()
                && Options.Curr.StayConnectOptions.ToUpper().Contains("REST-QUEUE")
                && !_eventHandling.GetEventsPending
                && _packageCentral?.MainItem?.Container is PackageContainerNetworkHttpFile cntHttp2
                && cntHttp2.ConnectorPrimary is PackageConnectorHttpRest connRest2
                && (DateTime.Now - _eventHandling.LastQueuedEventRequest).TotalMilliseconds > copts.UpdatePeriod)
            {
                // mutex!
                _eventHandling.GetEventsPending = true;
                _eventHandling.LastQueuedEventRequest = DateTime.Now;

                // async
                Task.Run(async () =>
                {
                    try
                    {
                        // prepare query
                        var qst = "/geteventmessages";
                        if (_eventHandling.LastReceivedEventTimeStamp != DateTime.MinValue)
                            qst += "/time/"
                                   + AasEventMsgEnvelope.TimeToString(_eventHandling.LastReceivedEventTimeStamp);

                        // execute and digest results
                        var lastTS = await
                            connRest2.PullEvents(qst);

                        // remember for next time
                        if (lastTS != DateTime.MinValue)
                            _eventHandling.LastReceivedEventTimeStamp = lastTS;
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex,
                            "pulling events from REST (time-out?)");
                    }
                    finally
                    {
                        // unlock mutex
                        _eventHandling.GetEventsPending = false;
                    }
                });
            }
        }

        public void MainTaimer_HandleIncomingAasEvents()
        {
            int nEvent = 0;
            while (true)
            {
                // try handle a reasonable number of events ..
                nEvent += 1;
                if (nEvent > 100)
                    break;

                // access
                var ev = _packageCentral?.EventBufferEditor?.PopEvent();
                if (ev == null)
                    break;

                // log viewer
                UserContrlEventCollection.PushEvent(ev);

                // inform current Flyover?
                if (currentFlyoutControl is IFlyoutAgent fosc)
                    fosc.GetAgent()?.PushEvent(ev);
                // dead-csharp off
                // inform agents?
                foreach (var fa in UserControlAgentsView.Children)
                    fa.GetAgent()?.PushEvent(ev);

                // push into plugins
                Plugins.PushEventIntoPlugins(ev);

                // to be applicable, the event message Observable has to relate into Main's environment
                var foundObservable = _packageCentral?.Main?.AasEnv?.FindReferableByReference(ev?.ObservableReference);
                if (foundObservable == null)
                    return;

                //
                // Update values?
                //
                var changedSomething = false;
                if (foundObservable is AasCore.Aas3_0_RC02.Submodel || foundObservable is AasCore.Aas3_0_RC02.ISubmodelElement)
                    foreach (var pluv in ev.GetPayloads<AasPayloadUpdateValue>())
                    {
                        changedSomething = changedSomething || (pluv.Values != null && pluv.Values.Count > 0);

                        // update value received
                        _eventHandling.UpdateValuePending = false;
                    }

                // stupid
                if (Options.Curr.StayConnectOptions.ToUpper().Contains("SIM")
                    && changedSomething)
                {
                    // just for test
                    DisplayElements.RefreshAllChildsFromMainData(DisplayElements.SelectedItem);
                    DisplayElements.Refresh();

                    // apply white list for automatic redisplay
                    // Note: do not re-display plugins!!
                    var ves = DisplayElements.SelectedItem;
                    if (ves != null && (ves is VisualElementSubmodelRef || ves is VisualElementSubmodelElement))
                        RedrawElementView();
                }
            }
        }

        private DateTime _mainTimer_LastCheckForDiaryEvents;
        private DateTime _mainTimer_LastCheckForAnimationElements = DateTime.Now;

        private async Task MainTimer_Tick(object sender, EventArgs e)
        {
            MainTimer_HandleLogMessages();
            await MainTimer_HandleEntityPanel();
            await MainTimer_HandleApplicationEvents();

            if (_packageCentral?.MainItem?.Container?.SignificantElements != null)
            {
                MainTimer_CheckDiaryDateToEmitEvents(
                    _mainTimer_LastCheckForDiaryEvents,
                    _packageCentral.MainItem.Container.Env?.AasEnv,
                    _packageCentral.MainItem.Container.SignificantElements,
                    directEmit: !_mainMenu?.IsChecked("CompressEvents") != true);
                _mainTimer_LastCheckForDiaryEvents = DateTime.UtcNow;

                // do animation?
                var deltaSecs = (DateTime.Now - _mainTimer_LastCheckForAnimationElements).TotalSeconds;

                if (deltaSecs >= 0.1)
                {
                    MainTimer_CheckAnimationElements(
                        deltaSecs,
                        _packageCentral.MainItem.Container.Env?.AasEnv,
                        _packageCentral.MainItem.Container.SignificantElements);
                    _mainTimer_LastCheckForAnimationElements = DateTime.Now;
                }
            }

            MainTimer_PeriodicalTaskForSelectedEntity();
            MainTaimer_HandleIncomingAasEvents();
            DisplayElements.UpdateFromQueuedEvents();
        }

        private void SetProgressBar()
        {
            SetProgressBar(0.0, "");
        }

        private void SetProgressBar(double? percent, string message = null)
        {
            if (percent.HasValue)
                ProgressBarInfo.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() => ProgressBarInfo.Value = percent.Value));

            if (message != null)
                LabelProgressBarInfo.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => LabelProgressBarInfo.Content = message));
        }

        private void ButtonHistory_HomeRequested(object sender, EventArgs e)
        {
            // be careful
            try
            {
                UiCheckIfActivateLoadedNavTo();
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While displaying home element");
            }
        }

        private async void ButtonHistory_ObjectRequested(object sender, VisualElementHistoryItem hi)
        {
            // be careful
            try
            {
                // try access visual element directly?
                var ve = hi?.VisualElement;
                if (ve != null && DisplayElements.Contains(ve))
                {
                    // is directly contain in actual tree
                    // show it
                    if (DisplayElements.TrySelectVisualElement(ve, wishExpanded: true))
                    {
                        // fake selection
                        RedrawElementView();
                        DisplayElements.Refresh();
                        ContentTakeOver.IsEnabled = false;

                        // done
                        return;
                    }
                }

                // no? .. is there a way to another file?
                if (_packageCentral.Repositories != null && hi?.ReferableAasId != null
                    && hi.ReferableReference != null)
                {
                    ;

                    // try lookup file in file repository
                    var fi = _packageCentral.Repositories.FindByAasId(hi.ReferableAasId.Trim());
                    if (fi == null)
                    {
                        Log.Singleton.Error(
                            $"Cannot lookup aas id {hi.ReferableAasId} in file repository.");
                        return;
                    }

                    // remember some further supplementary search information
                    var sri = ListOfVisualElement.StripSupplementaryReferenceInformation(hi.ReferableReference);

                    // load it (safe)
                    AasCore.Aas3_0_RC02.IReferable bo = null;
                    try
                    {
                        bo = await LoadFromFileRepository(fi, sri.CleanReference);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(
                            ex, $"While retrieving file for {hi.ReferableAasId} from file repository");
                    }

                    // still proceed?
                    VisualElementGeneric veFocus = null;
                    if (bo != null && this.DisplayElements != null)
                    {
                        veFocus = this.DisplayElements.SearchVisualElementOnMainDataObject(bo,
                            alsoDereferenceObjects: true, sri: sri);
                        if (veFocus == null)
                        {
                            Log.Singleton.Error(
                                $"Cannot lookup requested element within loaded file from repository.");
                            return;
                        }
                    }

                    // if successful, try to display it
                    try
                    {
                        // show ve
                        DisplayElements?.TrySelectVisualElement(veFocus, wishExpanded: true);
                        // remember in history
                        ButtonHistory.Push(veFocus);
                        // fake selection
                        RedrawElementView();
                        DisplayElements.Refresh();
                        ContentTakeOver.IsEnabled = false;
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(
                            ex, "While displaying element requested by back button.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While displaying element requested by plug-in");
            }
        }

        protected MessageReportWindow _messageReportWindow = null;

        private void ButtonReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonClear)
            {
                Log.Singleton.ClearNumberErrors();
                Message.Content = "";
                Message.Background = Brushes.White;
                Message.Foreground = Brushes.Black;
                Message.FontWeight = FontWeights.Normal;
                SetProgressBar();
            }

            if (sender == ButtonReport)
            {
#if __disabled
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
                |Please consider attaching the AASX package (you might rename this to .zip),
                |you were working on, as well as an screen shot.
                |
                |Please issue directly to github: https://github.com/admin-shell/aasx-package-explorer/issues
                |
                |Below, you're finding the history of log messages. Please check, if non-public information
                |is contained here.
                |----------------------------------------------------------------------------------------------------";

                // Substitute
                head += "\n";
                head = head.Replace("{0}", "" + Message?.Content);
                head = Regex.Replace(head, @"^(\s+)\|", "", RegexOptions.Multiline);

                // Collect all the stored log prints
                IEnumerable<StoredPrint> Prints()
                {
                    var prints = Log.Singleton.GetStoredLongTermPrints();
                    if (prints != null)
                    {
                        yield return new StoredPrint(head);

                        foreach (var sp in prints)
                        {
                            yield return sp;
                            if (sp.stackTrace != null)
                                yield return new StoredPrint("    Stacktrace: " + sp.stackTrace);
                        }
                    }
                }

                // show dialogue
                var dlg = new MessageReportWindow(Prints());
                dlg.ShowDialog();
#endif

                // show only if not present
                if (_messageReportWindow != null)
                    return;

                // Collect all the stored log prints
                IEnumerable<StoredPrint> Prints()
                {
                    var prints = Log.Singleton.GetStoredLongTermPrints();
                    if (prints != null)
                    {
                        foreach (var sp in prints)
                        {
                            yield return sp;
                            if (sp.stackTrace != null)
                                yield return new StoredPrint("    Stacktrace: " + sp.stackTrace);
                        }
                    }
                }

                // show (non modal)
                _messageReportWindow = new MessageReportWindow(Prints());
                _messageReportWindow.Closed += (s2, e2) =>
                {
                    _messageReportWindow = null;
                };
                _messageReportWindow.Show();
            }
        }

        //private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    // decode
        //    var ruic = e?.Command as RoutedUICommand;
        //    if (ruic == null)
        //        return;
        //    var cmd = ruic.Text?.Trim().ToLower();

        //    // see: MainWindow.CommandBindings.cs
        //    try
        //    {
        //        this.CommandBinding_GeneralDispatch(cmd);
        //    }
        //    catch (Exception err)
        //    {
        //        throw new InvalidOperationException(
        //            $"Failed to execute the command {cmd}: {err}");
        //    }

        //}


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

            // may be flush events
            CheckIfToFlushEvents();

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

            var positiveQuestion =
                Options.Curr.UseFlyovers &&
                AnyUiMessageBoxResult.Yes == MessageBoxFlyoutShow(
                    "Do you want to proceed closing the application? Make sure, that you have saved your data before.",
                    "Exit application?",
                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question);

            if (!positiveQuestion)
            {
                e.Cancel = true;
                return;
            }

            Log.Singleton.Info("Closing main package ..");
            try
            {
                _packageCentral?.MainItem?.Close();
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            try
            {
                // save LRU
                var lru = _packageCentral?.Repositories?.FindLRU();
                if (lru != null)
                {
                    Log.Singleton.Info("Saving LRU ..");
                    var lruFn = PackageContainerListLastRecentlyUsed.BuildDefaultFilename();
                    lru.SaveAsLocalFile(lruFn);
                }

                // also close log silently
                if (_messageReportWindow != null)
                    _messageReportWindow.Close();
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            e.Cancel = false;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.ActualWidth > 800)
            {
                if (MainSpaceGrid != null && MainSpaceGrid.ColumnDefinitions.Count >= 3)
                {
                    MainSpaceGrid.ColumnDefinitions[0].Width = new GridLength(this.ActualWidth / 5);
                    MainSpaceGrid.ColumnDefinitions[4].Width = new GridLength(this.ActualWidth / 2.5);
                }
            }
        }

        private void ShowContent_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ShowContent && this.showContentPackageUri != null && _packageCentral.MainAvailable)
            {
                Log.Singleton.Info("Trying display content {0} ..", this.showContentPackageUri);
                try
                {
                    var contentUri = this.showContentPackageUri;

                    // if local in the package, then make a tempfile
                    if (!this.showContentPackageUri.ToLower().Trim().StartsWith("http://")
                        && !this.showContentPackageUri.ToLower().Trim().StartsWith("https://"))
                    {
                        // make it as file
                        contentUri = _packageCentral.Main.MakePackageFileAvailableAsTempFile(
                            this.showContentPackageUri);
                    }

                    BrowserDisplayLocalFile(contentUri, this.showContentPackageMime);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(
                        ex, $"When displaying content {this.showContentPackageUri}, an error occurred");
                    return;
                }
                Log.Singleton.Info("Content {0} displayed.", this.showContentPackageUri);
            }
        }

        private void UpdateContent_Click(object sender, RoutedEventArgs e)
        {
            // have a online connection?
            if (this.theOnlineConnection != null && this.theOnlineConnection.IsValid() &&
                this.theOnlineConnection.IsConnected())
            {
                // current entity is a property
                if (this.currentEntityForUpdate != null && this.currentEntityForUpdate is VisualElementSubmodelElement)
                {
                    var viselem = this.currentEntityForUpdate as VisualElementSubmodelElement;
                    if (viselem != null && viselem.theEnv != null &&
                        viselem.theContainer != null && viselem.theContainer is AasCore.Aas3_0_RC02.Submodel &&
                        viselem.theWrapper != null && viselem.theWrapper != null &&
                        viselem.theWrapper is AasCore.Aas3_0_RC02.Property)
                    {
                        // access a valid property
                        var p = viselem.theWrapper as AasCore.Aas3_0_RC02.Property;
                        if (p != null)
                        {
                            // use online connection
                            var x = this.theOnlineConnection.UpdatePropertyValue(
                                viselem.theEnv, viselem.theContainer as AasCore.Aas3_0_RC02.Submodel, p);
                            p.Value = x;

                            // refresh
                            var y = DisplayElements.SelectedItem;
                            y?.RefreshFromMainData();
                            DisplayElements.Refresh();
                        }
                    }
                }
            }
        }

        private void ContentUndo_Click(object sender, RoutedEventArgs e)
        {
            DispEditEntityPanel.CallUndo();
        }

        private void CheckIfToFlushEvents()
        {
            if (_mainMenu?.IsChecked("CompressEvents") == true)
            {
                var evs = _eventCompressor?.Flush();
                if (evs != null)
                    foreach (var ev in evs)
                        _packageCentral?.PushEvent(ev);
            }
        }

        private void ContentTakeOver_Click(object sender, RoutedEventArgs e)
        {
            // some more "OK, good to go" 
            CheckIfToFlushEvents();

            // refresh display
            var x = DisplayElements.SelectedItem;
            if (x == null)
            {
                // TODO (MIHO, 2021-06-08): find the root cause instead of doing a quick-fix
                // some copy/paste operation seems to leave the DisplayElements-sate in the wrong state
                x = DisplayElements.TrySynchronizeToInternalTreeState();
            }
            x?.RefreshFromMainData();
            DisplayElements.Refresh();

            // re-enable
            ContentTakeOver.IsEnabled = false;
        }

        private void DispEditEntityPanel_ContentsChanged(object sender, int kind)
        {
        }

        private void mainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == System.Windows.Input.Key.OemPlus || e.Key == System.Windows.Input.Key.Add) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (theContentBrowser != null)
                    theContentBrowser.ZoomLevel += 0.25;
                e.Handled = true;
                return;
            }

            if ((e.Key == System.Windows.Input.Key.OemMinus || e.Key == System.Windows.Input.Key.Subtract) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (theContentBrowser != null)
                    theContentBrowser.ZoomLevel -= 0.25;
                e.Handled = true;
                return;
            }

            if (this.IsInFlyout() && currentFlyoutControl != null)
            {
                currentFlyoutControl.ControlPreviewKeyDown(e);
            }

            // DispEditEntityPanel.HandleGlobalKeyDown(e, preview: true);

            // global handling
            var la = _dynamicMenu?.HandleGlobalKeyDown(e, preview: true);
            if (la != null && !(la is AnyUiLambdaActionNone))
            {
                // add to "normal" event quoue
                DispEditEntityPanel.AddWishForOutsideAction(la);
            }

            // test
            if (e.Key == System.Windows.Input.Key.T
                && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                var ve = DisplayElements?.FindAllVisualElement()?.FirstOrDefault();
                if (ve != null)
                {
                    DisplayElements.Test2();
                }
            }
        }

#region Modal Flyovers
        //====================

        private List<StoredPrint> flyoutLogMessages = null;

        public void FlyoutLoggingStart()
        {
            if (flyoutLogMessages == null)
            {
                flyoutLogMessages = new List<StoredPrint>();
                return;
            }

            lock (flyoutLogMessages)
            {
                flyoutLogMessages = new List<StoredPrint>();
            }
        }

        public void FlyoutLoggingStop()
        {
            if (flyoutLogMessages == null)
                return;

            lock (flyoutLogMessages)
            {
                flyoutLogMessages = null;
            }
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

        public void CloseFlyover(bool threadSafe = false)
        {
            Action lambda = () =>
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
            };

            if (!threadSafe)
                lambda.Invoke();
            else
                Dispatcher.BeginInvoke(lambda);
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

            // register the frame
            var frame = new DispatcherFrame();
            ucfoc.ControlClosed += () =>
            {
                frame.Continue = false; // stops the frame
            };

            // main application needs to know
            currentFlyoutControl = ucfoc;

            // agent behaviour
            var preventClosingAction = false;
            // dead-csharp off
            //  if (uc is IFlyoutAgent ucag)
            // {
            //    // register for minimize
            //   ucag.ControlMinimize += () =>
            //{
            // only execute if preconditions are well
            // if (ucag.GetAgent() != null && ucag.GetAgent().GenerateFlyoutMini != null)
            //{
            // do not execute directly
            // preventClosingAction = true;

            // make a mini
            // var mini = ucag.GetAgent().GenerateFlyoutMini.Invoke();

            // be careful
            // if (mini is UserControl miniUc)
            // {
            // push the agent
            //    UserControlAgentsView.Add(miniUc);

            // wrap provided closing action in own closing action
            //    if (ucag.GetAgent() != null)
            //        ucag.GetAgent().ClosingAction = () =>
            //   {
            // 1st delete agent
            //         UserControlAgentsView.Remove(miniUc);

            // finally, call user provided closing action
            //           closingAction?.Invoke();
            //         };

            // show the panel
            //       PanelConcurrentSetVisibleIfRequired(true, targetAgents: true);

            // remove the flyover
            //         frame.Continue = false; // stops the frame
            //       }
            //     }
            //   };
            //} */
            // dead-csharp on
            // start (focus)
            ucfoc.ControlStart();

            // This will "block" execution of the current dispatcher frame
            // and run our frame until the dialog is closed.
            Dispatcher.PushFrame(frame);

            // call the closing action (before releasing!)
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (closingAction != null && !preventClosingAction)
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

        public AnyUiMessageBoxResult MessageBoxFlyoutShow(
            string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image)
        {
            if (!Options.Curr.UseFlyovers)
            {
                return AnyUiMessageBoxResult.Cancel;
            }

            var uc = new MessageBoxFlyout(message, caption, buttons, image);
            StartFlyoverModal(uc);
            return uc.Result;
        }

        public AnyUiMessageBoxResult MessageBoxFlyoutLogOrShow(
            bool log, StoredPrint.Color logColor,
            string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image)
        {
            if (log)
            {
                if (logColor == StoredPrint.Color.Red)
                    Log.Singleton.Error(caption + ": " + message);
                else
                    Log.Singleton.Info(logColor, caption + ": " + message);
                return AnyUiMessageBoxResult.OK;
            }
            else
                return MessageBoxFlyoutShow(message, caption, buttons, image);
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
            // Appearantly you need to figure out if OriginalSource would have handled the Drop?
            if (!e.Handled && e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                if (files != null && files.Length > 0)
                {
                    string fn = files[0];
                    try
                    {
                        UiLoadPackageWithNew(
                            _packageCentral.MainItem, null, loadLocalFilename: fn, onlyAuxiliary: false);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, $"while receiving file drop to window");
                    }
                }
            }
        }

        private bool isDragging = false;
        private Point dragStartPoint = new Point(0, 0);

        private void DragSource_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // MIHO 2020-09-14: removed this from the check below
            //// && (Math.Abs(dragStartPoint.X) < 0.001 && Math.Abs(dragStartPoint.Y) < 0.001)
            if (e.LeftButton == MouseButtonState.Pressed && !isDragging && this.showContentPackageUri != null &&
                _packageCentral.MainAvailable)
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
                        var tempfile = _packageCentral.Main.MakePackageFileAvailableAsTempFile(
                            this.showContentPackageUri, keepFilename: true);

                        // Package the data.
                        DataObject data = new DataObject();
                        data.SetFileDropList(new System.Collections.Specialized.StringCollection() { tempfile });

                        // Inititate the drag-and-drop operation.
                        DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(
                            ex, $"When dragging content {this.showContentPackageUri}, an error occurred");
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

        public string CreateTempFileForKeyboardShortcuts()
        {
            try
            {
                //
                // HTML statr
                //

                // create a temp HTML file
                var tmpfn = System.IO.Path.GetTempFileName();

                // rename to html file
                var htmlfn = tmpfn.Replace(".tmp", ".html");
                System.IO.File.Move(tmpfn, htmlfn);

                // create html content as string
                var htmlHeader = AdminShellUtil.CleanHereStringWithNewlines(
                    @"<!doctype html>
                    <html lang=en>
                    <head>
                    <style>
                    body {
                      background-color: #FFFFE0;
                      font-size: small;
                      font-family: Arial, Helvetica, sans-serif;
                    }
                    table {
                      font-family: arial, sans-serif;
                      border-collapse: collapse;
                      width: 100%;
                    }
                    td, th {
                      border: 1px solid #dddddd;
                      text-align: left;
                      padding: 8px;
                    }
                    </style>
                    <meta charset=utf-8>
                    <title>blah</title>
                    </head>
                    <body>");

                var htmlFooter = AdminShellUtil.CleanHereStringWithNewlines(
                    @"</body>
                    </html>");

                var html = new StringBuilder();

                html.Append(htmlHeader);

                var color = false;

                //
                // Keyboard shortcuts
                //

                html.AppendLine("<h3>Keyboard shortcuts</h3>");

                html.Append(AdminShellUtil.CleanHereStringWithNewlines(
                    @"<table style=""width:100%"">
                    <tr>
                    <th>Modifiers & Keys</th>
                    <th>Function</th>
                    <th>Description</th>
                    </tr>"));

                var rowfmt = AdminShellUtil.CleanHereStringWithNewlines(
                    @"<tr style=""background-color: {0}"">
                    <td>{1}</th>
                    <td>{2}</th>
                    <td>{3}</th>
                    </tr>");

                foreach (var sc in DispEditEntityPanel.EnumerateShortcuts())
                {
                    // Function
                    var fnct = "";
                    if (sc.Element is AnyUiButton btn)
                        fnct = "" + btn.Content;

                    // fill
                    html.Append(String.Format(rowfmt,
                        (color) ? "#ffffe0" : "#fffff0",
                        "" + sc.GestureToString(fmt: 0),
                        "" + fnct,
                        "" + sc.Info));

                    // color change
                    color = !color;
                }

                html.Append(AdminShellUtil.CleanHereStringWithNewlines(
                    @"</table>"));

                //
                // Menu command
                //

                // ReSharper disable AccessToModifiedClosure

                Action<AasxMenu> lambdaMenu = (menu) =>
                {

                    html.Append(AdminShellUtil.CleanHereStringWithNewlines(
                        @"<table style=""width:100%"">
                    <tr>
                    <th>Keyboard</th>
                    <th>Menu header</th>
                    <th>ToolCmd / <br><i>Argument</i></th>
                    <th>Description</th>
                    </tr>"));

                    var rowfmtTC = AdminShellUtil.CleanHereStringWithNewlines(
                        @"<tr style=""background-color: {0}"">
                    <td>{1}</td>
                    <td>{2}</td>
                    <td>{3}</td>
                    <td>{4}</td>
                    </tr>");

                    var rowfmtTCAD = AdminShellUtil.CleanHereStringWithNewlines(
                        @"<tr style=""background-color: {0}"">
                    <td colspan=""2"" 
                     style=""border-top:none;border-bottom:none;border-left:none;background-color:#FFFFE0"">
                    </td>
                    <td><i>{1}</i></td>
                    <td><i>{2}</i></td>
                    </tr>");

                    foreach (var mib in menu.FindAll((x) => x is AasxMenuItem))
                    {
                        // access
                        if (!(mib is AasxMenuItem mi) || mi.Name?.HasContent() != true)
                            continue;

                        // filter header
                        var header = mi.Header.Replace("_", "");

                        // fill
                        html.Append(String.Format(rowfmtTC,
                            (color) ? "#ffffe0" : "#fffff0",
                            "" + mi.InputGesture,
                            "" + header,
                            "" + mi.Name,
                            "" + mi.HelpText));

                        // arguments
                        if (mi.ArgDefs != null)
                            foreach (var ad in mi.ArgDefs)
                            {
                                if (ad.Hidden)
                                    continue;
                                html.Append(String.Format(rowfmtTCAD,
                                    (color) ? "#ffffe0" : "#fffff0",
                                    "" + ad.Name,
                                    "" + ad.Help));
                            }

                        // color change
                        color = !color;
                    }

                    html.Append(AdminShellUtil.CleanHereStringWithNewlines(
                        @"</table>"));
                };

                // ReSharper enable AccessToModifiedClosure

                html.AppendLine("<h3>Menu and script commands</h3>");
                lambdaMenu(_mainMenu.Menu);

                html.AppendLine("<h3>Displayed entity and script commands</h3>");
                lambdaMenu(_dynamicMenu.Menu);

                //
                // Script command
                //

                var script = new AasxScript();
                script.PrepareHelp();

                html.AppendLine("<h3>Script built-in commands</h3>");

                html.Append(AdminShellUtil.CleanHereStringWithNewlines(
                    @"<table style=""width:100%"">
                    <tr>
                    <th>Keyword</th>
                    <th>Argument</th>
                    <th>Description</th>
                    </tr>"));

                var rowfmtSC = AdminShellUtil.CleanHereStringWithNewlines(
                    @"<tr style=""background-color: {0}"">
                    <td>{1}</td>
                    <td colspan=""2"">{2}</td>
                    </tr>");

                var rowfmtSCAD = AdminShellUtil.CleanHereStringWithNewlines(
                    @"<tr style=""background-color: {0}"">
                    <td  
                     style=""border-top:none;border-bottom:none;border-left:none;background-color:#FFFFE0"">
                    </td>
                    <td><i>{1}</i></td>
                    <td><i>{2}</i></td>
                    </tr>");

                foreach (var hr in script.ListOfHelp)
                {
                    // fill
                    html.Append(String.Format(rowfmtSC,
                        (color) ? "#ffffe0" : "#fffff0",
                        "" + hr.Keyword,
                        "" + hr.Description));

                    // arguments
                    if (hr.ArgDefs != null)
                        foreach (var ad in hr.ArgDefs)
                        {
                            if (ad.Hidden)
                                continue;
                            html.Append(String.Format(rowfmtSCAD,
                                (color) ? "#ffffe0" : "#fffff0",
                                "" + HttpUtility.HtmlEncode(ad.Name),
                                "" + ad.Help));
                        }

                    // color change
                    color = !color;
                }

                //
                // HTMLend
                //

                html.Append(htmlFooter);

                // write
                System.IO.File.WriteAllText(htmlfn, html.ToString());
                return htmlfn;
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "Creating HTML file for keyboard shortcuts");
            }
            return null;
        }

        private void ButtonKeyboard_Click(object sender, RoutedEventArgs e)
        {
            var htmlfn = CreateTempFileForKeyboardShortcuts();
            BrowserDisplayLocalFile(htmlfn, System.Net.Mime.MediaTypeNames.Text.Html,
                                    preferInternal: true);
        }
    }
}
