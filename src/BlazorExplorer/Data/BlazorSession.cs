/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
// using System.Windows.Media;
// using System.Windows.Media.Imaging;
using AasxIntegrationBase;
using AasxIntegrationBaseGdi;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using BlazorExplorer;
// using ExhaustiveMatching;
using Extensions;
using Microsoft.JSInterop;
using System.Linq;
using AasxIntegrationBase.AdminShellEvents;
using AasCore.Aas3_0;

namespace BlazorUI.Data
{
    /// <summary>
    /// This class is used by Blazor to auto-create session information.
    /// A session holds almost all data a user concerns with, as multiple users/ roles might use the same
    /// server application.
    /// </summary>
    public partial class BlazorSession : IDisposable
    {
        /// <summary>
        /// Monotonous index to be counted upwards to generate SessionId
        /// </summary>
        public static int SessionIndex = 0;

        /// <summary>
        /// Numerical session id of that specific session.
        /// </summary>
        public int SessionId = 0;

        /// <summary>
        /// Number of active session; incremented an decremented (on disposal)
        /// </summary>
        public static int SessionNumActive = 0;

        /// <summary>
        /// Abstracted menu functions to be wrapped by functions triggering
        /// more UI feedback.
        /// Remark: "Scripting" is only the highest of the functionality levels
        /// of the "stacked classes"
        /// </summary>
        public MainWindowScripting Logic = new MainWindowScripting();

        /// <summary>
        /// All repositories, files, .. are user-specific and therefore hosted in the session.
        /// However, PacgaeCentral resides in the logic; therefore this symbol is only a 
        /// link to the abstract main-windows class.
        /// </summary>
        public PackageCentral PackageCentral
        {
            get => Logic?.PackageCentral;
        }

        /// <summary>
        /// This object wrap the visual elements, that is, the main tree
        /// </summary>
        public BlazorVisualElements DisplayElements = new BlazorVisualElements();

        /// <summary>
        /// The main menu holds the options, which are provided by a top/left positioned 
        /// application menu
        /// </summary>
        public AasxMenuBlazor MainMenu = new AasxMenuBlazor();

        /// <summary>
        /// Thsi menu holds the menu items which are provided by action buttons within
        /// the dynamically created element panel.
        /// </summary>
        public AasxMenuBlazor DynamicMenu = new AasxMenuBlazor();

        /// <summary>
        /// Helper class to "compress events" (group AAS event payloads together).
        /// No relation to UI stuff.
        /// </summary>
		private AasEventCompressor _eventCompressor = new AasEventCompressor();

        /// <summary>
        /// The top-most display data required for razor pages to render elements.
        /// </summary>
        public AnyUiDisplayContextHtml DisplayContext = null;

        /// <summary>
        /// Holds the stack panel of widgets for active AAS element.
        /// </summary>
        public AnyUiStackPanel ElementPanel = new AnyUiStackPanel();

        /// <summary>
        /// Helper class to hold data on the AAS preview left side.
        /// </summary>
        public AasxInfoBox InfoBox = new AasxInfoBox();

        /// <summary>
        /// Position of the dividing column (of 12) between left & right panel
        /// </summary>
        public int DividerTreeAndElement = 4;

        /// <summary>
        /// Session is in edit mode. This setting shall be controlled by the menu / hotkey/ script
        /// functionality.
        /// </summary>
        public bool EditMode = false;

        /// <summary>
        /// Session is in edit mode. This setting shall be controlled by the menu / hotkey/ script
        /// functionality.
        /// </summary>
        public bool HintMode = true;

        // to be refactored in a class?

        public VisualElementGeneric LoadedPluginNode = null;
        public Plugins.PluginInstance LoadedPluginInstance = null;
        public object LoadedPluginSessionId = null;

        /// <summary>
        /// Set by timer functions if entered. Used to interlock double timers,
        /// mostly for debugger-friendlyness.
        /// </summary>
        public bool InTimer = false;

        //
        // PROTECTED
        //

        //
        // OLD
        //

        ///// <summary>
        ///// Content of the status line. View model for a blazor component; therefore too frequent
        ///// updates to be avoided.
        ///// </summary>
        // dead-csharp off
        //public string Message = "Initialized.";
        // dead-csharp on
        // old stuff, to be refactored

        public AdminShellPackageEnv env = null;
        public IndexOfSignificantAasElements significantElements = null;

        public string[] aasxFiles = new string[1];
        public string aasxFileSelected = "";
        public PackageContainerListHttpRestRepository repository = null;
        public DispEditHelperMultiElement helper = null;
        public ModifyRepo repo = null;
        public PackageContainerBase container = null;

        public AnyUiStackPanel stack = new AnyUiStackPanel();
        public AnyUiStackPanel stack2 = new AnyUiStackPanel();


        public string thumbNail = null;

        public IJSRuntime renderJsRuntime = null;

        public ListOfItems items = null;
        public Thread htmlDotnetThread = null;

        public static int totalIndexTimer = 0;



        /// <summary>
        /// Called to create a new session
        /// </summary>
        public BlazorSession()
        {
            // Statistics
            SessionId = ++SessionIndex;
            SessionNumActive++;

            // initalize the abstract main window logic
            DisplayContext = new AnyUiDisplayContextHtml(PackageCentral, this);
            Logic.DisplayContext = DisplayContext;
            Logic.MainWindow = this;

            // create a new session for plugin / event handling
            AnyUiDisplayContextHtml.AddEventSession(SessionId);

            // display elements have a cache
            DisplayElements.ActivateElementStateCache();

            // logical main menu
            var logicalMainMenu = ExplorerMenuFactory.CreateMainMenu();
            logicalMainMenu.DefaultActionAsync = CommandBinding_GeneralDispatch;

            // top level children have other color
            logicalMainMenu.DefaultForeground = AnyUiColors.Black;
            foreach (var mi in logicalMainMenu)
                if (mi is AasxMenuItem mii)
                    mii.Foreground = AnyUiColors.White;

            // Main menu
            MainMenu = new AasxMenuBlazor();
            // MainMenu.LoadAndRender(logicalMainMenu, null, null);

            // show Logo?
            if (Options.Curr.LogoFile != null)
                try
                {
                    // dead-csharp off
                    //var fullfn = System.IO.Path.GetFullPath(Options.Curr.LogoFile);
                    //var bi = new BitmapImage(new Uri(fullfn, UriKind.RelativeOrAbsolute));
                    //this.LogoImage.Source = bi;
                    //this.LogoImage.UpdateLayout();
                    // dead-csharp on
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

            // Package Central starting ..
            PackageCentral.CentralRuntimeOptions = UiBuildRuntimeOptionsForMainAppLoad();

            // LRU repository?
            var lruFn = PackageContainerListLastRecentlyUsed.BuildDefaultFilename();
            try
            {
                if (System.IO.File.Exists(lruFn))
                {
                    var lru = PackageContainerListLastRecentlyUsed.Load<PackageContainerListLastRecentlyUsed>(lruFn);
                    PackageCentral?.Repositories.Add(lru);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"while loading last recently used file {lruFn}");
            }

            // Repository pointed by the Options
            if (Options.Curr.AasxRepositoryFn.HasContent())
            {
                var fr2 = Logic.UiLoadFileRepository(Options.Curr.AasxRepositoryFn);
                if (fr2 != null)
                {
                    PackageCentral.Repositories ??= new PackageContainerListOfList();
                    PackageCentral.Repositories.AddAtTop(fr2);
                }
            }

            // initialize menu
            MainMenu?.SetChecked("FileRepoLoadWoPrompt", Options.Curr.LoadWithoutPrompt);
            MainMenu?.SetChecked("ShowIriMenu", Options.Curr.ShowIdAsIri);
            MainMenu?.SetChecked("VerboseConnect", Options.Curr.VerboseConnect);
            MainMenu?.SetChecked("AnimateElements", Options.Curr.AnimateElements);
            MainMenu?.SetChecked("ObserveEvents", Options.Curr.ObserveEvents);
            MainMenu?.SetChecked("CompressEvents", Options.Curr.CompressEvents);

            // the UI application might receive events from items in the package central
            PackageCentral.ChangeEventHandler = (data) =>
            {
                Log.Singleton.Info("PackageCentral events: " + data.Info);
                return false;
            };

            // nearly last task here ..
            Log.Singleton.Info("Application started ..");

            // start with a new file
            PackageCentral.MainItem.New();
            RedrawAllAasxElements();

            // Try to load?            
            if (Options.Curr.AasxToLoad != null)
            {
                var location = Options.Curr.AasxToLoad;
                try
                {
                    Log.Singleton.Info($"Auto-load main package at application start " +
                        $"from {location} into container");

                    var container = PackageContainerFactory.GuessAndCreateFor(
                        PackageCentral,
                        location,
                        location,
                        overrideLoadResident: true,
                        containerOptions: PackageContainerOptionsBase.CreateDefault(Options.Curr),
                        runtimeOptions: PackageCentral.CentralRuntimeOptions);

                    if (container == null)
                        Log.Singleton.Error($"Failed to auto-load AASX from {location}");
                    else
                        UiLoadPackageWithNew(PackageCentral.MainItem,
                            takeOverContainer: container, onlyAuxiliary: false, indexItems: true);

                    Log.Singleton.Info($"Successfully auto-loaded AASX {location}");
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"When auto-loading {location}");
                }
            }

            if (Options.Curr.AuxToLoad != null)
            {
                var location = Options.Curr.AuxToLoad;
                try
                {
                    Log.Singleton.Info($"Auto-load auxiliary package at application start " +
                        $"from {location} into container");

                    var container = PackageContainerFactory.GuessAndCreateFor(
                        PackageCentral,
                        location,
                        location,
                        overrideLoadResident: true,
                        containerOptions: PackageContainerOptionsBase.CreateDefault(Options.Curr),
                        runtimeOptions: PackageCentral.CentralRuntimeOptions);

                    if (container == null)
                        Log.Singleton.Error($"Failed to auto-load AASX from {location}");
                    else
                        UiLoadPackageWithNew(PackageCentral.AuxItem,
                            takeOverContainer: container, onlyAuxiliary: true, indexItems: false);

                    Log.Singleton.Info($"Successfully auto-loaded AASX {location}");
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"When auto-loading {location}");
                }
            }

            //
            // OLD
            //

            env = null;

            helper = new DispEditHelperMultiElement();
            helper.levelColors = DispLevelColors.GetLevelColorsFromOptions(Options.Curr);

            // some functionality still uses repo != null to detect editMode!!
            repo = new ModifyRepo();
            helper.editMode = EditMode;
            helper.hintMode = HintMode;
            helper.repo = repo;
            helper.context = null;
            helper.packages = PackageCentral;

            ElementPanel = new AnyUiStackPanel() { Orientation = AnyUiOrientation.Vertical };

            if (env?.AasEnv?.AssetAdministrationShells != null)
                helper.DisplayOrEditAasEntityAas(PackageCentral, env.AasEnv,
                    env.AasEnv.AssetAdministrationShells[0], EditMode, ElementPanel, hintMode: HintMode);

            htmlDotnetThread = new Thread(AnyUiDisplayContextHtml.htmlDotnetLoop);
            htmlDotnetThread.Start();

        }

        /// <summary>
        /// Called by Blazor to dispose a session
        /// </summary>
        public void Dispose()
        {
            AnyUiDisplayContextHtml.DeleteEventSession(SessionId);
            SessionNumActive--;
            if (env != null)
                env.Close();
        }

        /// <summary>
        /// Sends a dispose signal to the loaded plugin in order to properly
        /// release its resources before session might be disposed or plugin might
        /// be changed.
        /// </summary>
        public void DisposeLoadedPlugin()
        {
            // access
            if (LoadedPluginInstance == null || LoadedPluginSessionId == null)
            {
                LoadedPluginNode = null;
                LoadedPluginInstance = null;
                LoadedPluginSessionId = null;
                return;
            }

            // try release
            try
            {
                LoadedPluginInstance.InvokeAction("dispose-anyui-visual-extension",
                    LoadedPluginSessionId);

                LoadedPluginNode = null;
                LoadedPluginInstance = null;
                LoadedPluginSessionId = null;
            }
            catch (Exception ex)
            {
                LogInternally.That.CompletelyIgnoredError(ex);
            }
        }

        public void ClearPasteBuffer()
        {
            if (helper.theCopyPaste != null)
                helper.theCopyPaste.Clear();
        }

        /// <summary>
        /// This functions prepares display data and element panel to be rendered
        /// by a razor page.
        /// Note: in BlazorUI was in Index.razor; however complex code and better
        /// maintained in Session.
        /// </summary>
        /// <returns>If contents could be rendered</returns>
        public bool PrepareDisplayDataAndElementPanel(
            IJSRuntime jsRuntime,
            AnyUiDisplayContextHtml displayContext,
            ref DispEditHelperMultiElement helper,
            ref AnyUiStackPanel elementPanel,
            ref AasxMenuBlazor dynamicMenu)
        {
            // access possible
            var sn = DisplayElements.SelectedItem;
            var bo = sn?.GetMainDataObject();
            if (sn == null || bo == null)
                return false;

            // brutally remember some data
            renderJsRuntime = jsRuntime;
            helper.editMode = EditMode;
            helper.hintMode = HintMode;
            helper.repo = (EditMode) ? repo : null;

            // create new context
            helper.context = displayContext;

            // menu will collect dynamic data
            dynamicMenu ??= new AasxMenuBlazor();
            var superMenu = dynamicMenu.Menu;

            // clean view
            if (elementPanel == null)
                elementPanel = new AnyUiStackPanel();
            else
                elementPanel.Children.Clear();

            // determine some flags
            var tiCds = DisplayElements.SearchVisualElementOnMainDataObject(
                PackageCentral.Main?.AasEnv?.ConceptDescriptions) as
                VisualElementEnvironmentItem;

            // first special case: multiple elements
            if (DisplayElements.SelectedItems != null
                && DisplayElements.SelectedItems.Count > 1)
            {
                // multi select
                helper.DisplayOrEditAasEntityMultipleElements(
                PackageCentral,
                DisplayElements.SelectedItems,
                EditMode, elementPanel,
                tiCds?.CdSortOrder ?? VisualElementEnvironmentItem.ConceptDescSortOrder.None,
                superMenu: superMenu);
            }
            else
            {
                // try to delegate to common routine
                var common = helper.DisplayOrEditCommonEntity(
                    PackageCentral,
                    elementPanel,
                    superMenu, EditMode, HintMode,
                    tiCds?.CdSortOrder ?? VisualElementEnvironmentItem.ConceptDescSortOrder.None,
                    DisplayElements.SelectedItem);

                if (common)
                {
                    // can reset plugin
                    DisposeLoadedPlugin();
                }
                else
                {
                    // some special cases

                    if (DisplayElements.SelectedItem is VisualElementPluginExtension vepe)
                    {
                        // Try to figure out plugin rendering approach (1=WPF, 2=AnyUI)
                        var approach = 0;
                        var hasWpf = vepe.thePlugin?.HasAction("fill-panel-visual-extension") == true;
                        var hasAnyUi = vepe.thePlugin?.HasAction("fill-anyui-visual-extension") == true;

                        if (hasWpf && Options.Curr.PluginPrefer?.ToUpper().Contains("WPF") == true)
                            approach = 1;

                        if (hasAnyUi && Options.Curr.PluginPrefer?.ToUpper().Contains("ANYUI") == true)
                            approach = 2;

                        if (approach == 0 && hasAnyUi)
                            approach = 2;

                        if (approach == 0 && hasWpf)
                            approach = 1;

                        // the default behaviour is to update a plugin content,
                        // only if no / invalid content is available fill new

                        var pluginOnlyUpdate = true;

                        // may dispose old (other plugin)
                        if (LoadedPluginInstance == null
                            || LoadedPluginNode != DisplayElements.SelectedItem
                            || LoadedPluginInstance != vepe.thePlugin)
                        {
                            // invalidate, fill new
                            DisposeLoadedPlugin();
                            pluginOnlyUpdate = false;
                        }

                        // NEW: Differentiate behaviour ..
                        if (approach == 2)
                        {
                            //
                            // Render panel via ANY UI !!
                            //

                            try
                            {
                                if (pluginOnlyUpdate)
                                {
                                    vepe.thePlugin?.InvokeAction(
                                        "update-anyui-visual-extension",
                                        elementPanel, displayContext,
                                        SessionId);
                                }
                                else
                                {
                                    var opContext = new PluginOperationContextBase()
                                    {
                                        DisplayMode = (EditMode)
                                        ? PluginOperationDisplayMode.MayAddEdit
                                        : PluginOperationDisplayMode.JustDisplay
                                    };

                                    vepe.thePlugin?.InvokeAction(
                                        "fill-anyui-visual-extension", vepe.thePackage, vepe.theReferable,
                                        elementPanel,
                                        displayContext,
                                        SessionId,
                                        opContext);

                                    // remember
                                    LoadedPluginNode = DisplayElements.SelectedItem;
                                    LoadedPluginInstance = vepe.thePlugin;
                                    LoadedPluginSessionId = SessionId;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(ex,
                                    $"render AnyUI based visual extension for plugin {vepe.thePlugin.name}");
                            }
                        }
                        else
                        {
                            //
                            // SWAP panel with NATIVE WPF CONTRAL and try render via WPF !!
                            //

                            // create controls
                            object result = null;

                            if (approach == 1)
                                try
                                {
                                    // replace at top level
                                    elementPanel.Children.Clear();
                                    if (vepe.thePlugin != null)
                                        result = vepe.thePlugin.InvokeAction(
                                            "fill-panel-visual-extension",
                                            vepe.thePackage, vepe.theReferable, elementPanel);
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(ex,
                                    $"render WPF based visual extension for plugin {vepe.thePlugin.name}");
                                }

                            // add?
                            if (result == null)
                            {
                                // re-init display!
                                elementPanel = new AnyUiStackPanel();
                                helper.AddGroup(
                                    elementPanel, "Entity from Plugin cannot be rendered!",
                                    helper.levelColors.MainSection);
                            }
                        }

                    }
                    else
                    {
                        helper.AddGroup(
                            elementPanel, "Entity is unknown!", helper.levelColors.MainSection);
                    }
                }
            }

            // okay
            return true;
        }


        /// <summary>
        /// This is the main session timer callback. It is either activated by the session itself
        /// or by the index page (proper initialized / disposed cycle).
        /// </summary>
        public void MainTimerTick()
        {

        }

        /// <summary>
        /// Nearly the same as copied.
        /// </summary>
        public async Task ContainerListItemLoad(PackageContainerListBase repo, PackageContainerRepoItem fi)
        {
            {
                // access
                if (repo == null || fi == null)
                    return;

                // safety?
                if (MainMenu?.IsChecked("FileRepoLoadWoPrompt") == false)
                {
                    // ask double question
                    if (AnyUiMessageBoxResult.OK != DisplayContext.MessageBoxFlyoutShow(
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
                            PackageCentral,
                            location,
                            location,
                            overrideLoadResident: true,
                            takeOver: fi,
                            fi.ContainerList,
                            containerOptions: copts,
                            runtimeOptions: PackageCentral.CentralRuntimeOptions);

                        if (container == null)
                            Log.Singleton.Error($"Failed to load AASX from {location}");
                        else
                            UiLoadPackageWithNew(PackageCentral.MainItem,
                                takeOverContainer: container, onlyAuxiliary: false,
                                storeFnToLRU: location);

                        Log.Singleton.Info($"Successfully loaded AASX {location}");

                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, $"When auto-loading {location}");
                    }
                }

            };
        }

        public async Task FileDropped(AnyUiDialogueDataOpenFile ddof, BlazorInput.KeyboardModifiers modi)
        {
            // stop complaining
            await Task.Delay(1);

            // access
            if (ddof?.OriginalFileName?.HasContent() != true || ddof.TargetFileName.HasContent() != true)
                return;

            var ext = Path.GetExtension(ddof.OriginalFileName).ToLower();

            // AASX to load (no Ctrl key)
            if (ext == ".aasx" && (modi & BlazorInput.KeyboardModifiers.Ctrl) == 0)
            {
                Log.Singleton.Info($"Load file {ddof.TargetFileName} originally from from {ddof.OriginalFileName} into container ..");

                try
                {
                    var container = await PackageContainerFactory.GuessAndCreateForAsync(
                        PackageCentral,
                        ddof.TargetFileName,
                        ddof.TargetFileName,
                        overrideLoadResident: true,
                        runtimeOptions: PackageCentral.CentralRuntimeOptions);

                    if (container == null)
                        Log.Singleton.Error($"Failed to load AASX from {ddof.TargetFileName}");
                    else
                        UiLoadPackageWithNew(PackageCentral.MainItem,
                            takeOverContainer: container, onlyAuxiliary: false);

                    Log.Singleton.Info($"Successfully loaded AASX {ddof.OriginalFileName}");
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"When loading {ddof.OriginalFileName}");
                }
            }

            // AASX to add (to last repo)
            var fr = PackageCentral.Repositories?.LastOrDefault();
            if (ext == ".aasx"
                && (modi & BlazorInput.KeyboardModifiers.Ctrl) != 0
                && fr != null && (fr is PackageContainerListLocal))
            {
                // add
                fr.AddByAasxFn(PackageCentral, ddof.TargetFileName);

                // update
                Program.signalNewData(
                        new Program.NewDataAvailableArgs(
                            Program.DataRedrawMode.None, this.SessionId));
            }

            // JSON -> Repo
            if (ext == ".json")
            {
                // try handle as repository
                var newRepo = Logic.UiLoadFileRepository(ddof.TargetFileName);
                if (newRepo != null)
                {
                    // add
                    PackageCentral.Repositories.AddAtTop(newRepo);

                    // redisplay
                    Program.signalNewData(
                        new Program.NewDataAvailableArgs(
                            Program.DataRedrawMode.None, this.SessionId));
                }
            }
        }


        //
        // Scripting
        //

        /// <summary>
        /// Returns the <c>AasxMenu</c> of the main menu of the application.
        /// Purpose: script automation
        /// </summary>
        public AasxMenu GetMainMenu()
        {
            return MainMenu?.Menu;
        }

        /// <summary>
        /// Returns the <c>AasxMenu</c> of the dynmaically built menu of the application.
        /// Purpose: script automation
        /// </summary>
        public AasxMenu GetDynamicMenu()
        {
            return DynamicMenu?.Menu;
        }

        // <summary>
        /// Returns the quite concise script interface of the application
        /// to allow script automation.
        /// </summary>
        public IAasxScriptRemoteInterface GetRemoteInterface()
        {
            return Logic;
        }
    }
}
