/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Media.Imaging;
using AasxIntegrationBase;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using Microsoft.JSInterop;

namespace BlazorUI.Data
{
    /// <summary>
    /// This class is used by Blazor to auto-create session information.
    /// A session holds almost all data a user concerns with, as multiple users/ roles might use the same
    /// server application.
    /// </summary>
    public class BlazorSession : IDisposable
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
        /// All loaded packages, repos are user-specific
        /// </summary>
        // TODO (MIHO, 2023-01-15): Think of a concept of sharing? locking? repo items
        public PackageCentral PackageCentral = null;

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
        /// Holds the stack panel of widgets for active AAS element.
        /// </summary>
        public AnyUiStackPanel ElementPanel = new AnyUiStackPanel();

        /// <summary>
        /// Position of the dividing column (of 12) between left & right panel
        /// </summary>
        public int DividerTreeAndElement = 4;

        // old stuff, to be refactored

        public AdminShellPackageEnv env = null;
        public IndexOfSignificantAasElements significantElements = null;

        public string[] aasxFiles = new string[1];
        public string aasxFileSelected = "";
        public bool editMode = false;
        public bool hintMode = true;
        public PackageContainerListHttpRestRepository repository = null;
        public DispEditHelperEntities helper = null;
        public ModifyRepo repo = null;
        public PackageContainerBase container = null;

        public AnyUiStackPanel stack = new AnyUiStackPanel();
        public AnyUiStackPanel stack2 = new AnyUiStackPanel();


        public string thumbNail = null;

        public IJSRuntime renderJsRuntime = null;

        public ListOfItems items = null;
        public Thread htmlDotnetThread = null;

        public static int totalIndexTimer = 0;

        public Plugins.PluginInstance LoadedPluginInstance = null;
        public object LoadedPluginSessionId = null;

        /// <summary>
        /// Called to create a new session
        /// </summary>
        public BlazorSession()
        {
            // Statistics
            SessionId = ++SessionIndex;
            SessionNumActive++;

            // create a new session for plugin / event handling
            AnyUiDisplayContextHtml.addSession(SessionId);

            // create a new package central
            PackageCentral = new PackageCentral();

            // display elements have a cache
            DisplayElements.ActivateElementStateCache();

            // logical main menu
            var logicalMainMenu = CreateMainMenu();

            // top level children have other color
            logicalMainMenu.DefaultForeground = AnyUiColors.Black;
            foreach (var mi in logicalMainMenu)
                if (mi is AasxMenuItem mii)
                    mii.Foreground = AnyUiColors.White;

            // Main menu
            MainMenu = new AasxMenuBlazor();
            MainMenu.LoadAndRender(logicalMainMenu, null, null);

            // show Logo?
            if (Options.Curr.LogoFile != null)
                try
                {
                    //var fullfn = System.IO.Path.GetFullPath(Options.Curr.LogoFile);
                    //var bi = new BitmapImage(new Uri(fullfn, UriKind.RelativeOrAbsolute));
                    //this.LogoImage.Source = bi;
                    //this.LogoImage.UpdateLayout();
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
                //var fr2 = UiLoadFileRepository(Options.Curr.AasxRepositoryFn);
                //if (fr2 != null)
                //{
                //    this.UiAssertFileRepository(visible: true);
                //    _packageCentral.Repositories.AddAtTop(fr2);
                //}
            }

            // initialize menu
            //_mainMenu?.SetChecked("FileRepoLoadWoPrompt", Options.Curr.LoadWithoutPrompt);
            //_mainMenu?.SetChecked("ShowIriMenu", Options.Curr.ShowIdAsIri);
            //_mainMenu?.SetChecked("VerboseConnect", Options.Curr.VerboseConnect);
            //_mainMenu?.SetChecked("AnimateElements", Options.Curr.AnimateElements);
            //_mainMenu?.SetChecked("ObserveEvents", Options.Curr.ObserveEvents);
            //_mainMenu?.SetChecked("CompressEvents", Options.Curr.CompressEvents);

            // the UI application might receive events from items in the package central
            PackageCentral.ChangeEventHandler = (data) =>
            {
                // if (data.Reason == PackCntChangeEventReason.Exception)
                    Log.Singleton.Info("PackageCentral events: " + data.Info);
                //DisplayElements.PushEvent(new AnyUiLambdaActionPackCntChange() { Change = data });
                return false;
            };

            // nearly last task here ..
            Log.Singleton.Info("Application started ..");

            // start with a new file
            PackageCentral.MainItem.New();
            RedrawAllAasxElements();

            // pump all pending log messages(from plugins) into the
            // log / status line, before setting the last information
            // MainTimer_HandleLogMessages();

            // Try to load?            
            if (Options.Curr.AasxToLoad != null)
            {
                var location = Options.Curr.AasxToLoad;
                try
                {
                    Log.Singleton.Info($"Auto-load file at application start " +
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

            //
            // OLD
            //

            env = null;

            helper = new DispEditHelperEntities();
            helper.levelColors = DispLevelColors.GetLevelColorsFromOptions(Options.Curr);
            
            // some functionality still uses repo != null to detect editMode!!
            repo = new ModifyRepo();
            helper.editMode = editMode;
            helper.hintMode = hintMode;
            helper.repo = repo;
            helper.context = null;
            helper.packages = PackageCentral;

            ElementPanel = new AnyUiStackPanel() { Orientation = AnyUiOrientation.Vertical };

            if (env?.AasEnv?.AssetAdministrationShells != null)
                helper.DisplayOrEditAasEntityAas(PackageCentral, env.AasEnv,
                    env.AasEnv.AssetAdministrationShells[0], editMode, ElementPanel, hintMode: hintMode);

            htmlDotnetThread = new Thread(AnyUiDisplayContextHtml.htmlDotnetLoop);
            htmlDotnetThread.Start();
        }

        /// <summary>
        /// Called by Blazor to dispose a session
        /// </summary>
        public void Dispose()
        {
            AnyUiDisplayContextHtml.deleteSession(SessionId);
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
                LoadedPluginInstance = null;
                LoadedPluginSessionId = null;
                return;
            }

            // try release
            try
            {
                LoadedPluginInstance.InvokeAction("dispose-anyui-visual-extension",
                    LoadedPluginSessionId);

                LoadedPluginInstance = null;
                LoadedPluginSessionId = null;
            }
            catch (Exception ex)
            {
                LogInternally.That.CompletelyIgnoredError(ex);
            }
        }

        private PackCntRuntimeOptions UiBuildRuntimeOptionsForMainAppLoad()
        {
            var ro = new PackCntRuntimeOptions()
            {
                Log = Log.Singleton,
                ProgressChanged = (state, tfs, tbd) =>
                {
                    ;
                },
                ShowMesssageBox = (content, text, title, buttons) =>
                {
                    return AnyUiMessageBoxResult.Cancel;
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
                if (!packItem.Load(PackageCentral, loadLocalFilename, loadLocalFilename,
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
                //if (!doNotNavigateAfterLoaded)
                //    UiCheckIfActivateLoadedNavTo();

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
                var lru = PackageCentral?.Repositories?.FindLRU();
                if (lru != null && storeFnToLRU.HasContent())
                    lru.Push(packItem?.Container as PackageContainerRepoItem, storeFnToLRU);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"When managing LRU files");
                return;
            }

            // done
            Log.Singleton.Info("AASX {0} loaded.", info);
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
                // _mainMenu?.SetChecked("EditMenu", false);
                // ClearAllViews();
                RedrawAllAasxElements();
                // RedrawElementView();
                // ShowContentBrowser(Options.Curr.ContentHome, silent: true);
                // _eventHandling.Reset();
            }
        }

        public void RedrawAllAasxElements(bool keepFocus = false)
        {
            // focus info
            var focusMdo = DisplayElements.SelectedItem?.GetDereferencedMainDataObject();

            // TODO: Can we set title of the browser tab?
            //var t = "AASX Package Explorer V3RC02";  //TODO:jtikekar remove V3RC02
            //if (PackageCentral.MainAvailable)
            //    t += " - " + PackageCentral.MainItem.ToString();
            //if (PackageCentral.AuxAvailable)
            //    t += " (auxiliary AASX: " + PackageCentral.AuxItem.ToString() + ")";            
            // this.Title = t;

#if _log_times
            Log.Singleton.Info("Time 10 is: " + DateTime.Now.ToString("hh:mm:ss.fff"));
#endif

            // clear the right section, first (might be rebuild by callback from below)
            // DispEditEntityPanel.ClearDisplayDefautlStack();
            // ContentTakeOver.IsEnabled = false;

            // rebuild middle section
            DisplayElements.RebuildAasxElements(
                PackageCentral, PackageCentral.Selector.Main, this.editMode,
                lazyLoadingFirst: false);

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

        public void StartSession()
        {
            


        }

        public void RebuildTree()
        {
            Log.Singleton.Error("Implement REBUILD TREE");
        }
        
        /// <summary>
        /// WRONG!!! REFACTOR!!!
        /// </summary>
        /// <returns></returns>
        public AasxMenu CreateMainMenu()
        {
            //
            // Start
            //

            var menu = new AasxMenu();

            //
            // File
            //

            menu.AddMenu(header: "File",
                childs: (new AasxMenu())
                .AddWpf(name: "New", header: "_New …", inputGesture: "Ctrl+N",
                    help: "Create new AASX package.")
                .AddWpf(name: "Open", header: "_Open …", inputGesture: "Ctrl+O",
                    help: "Open existing AASX package.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("File", "Source filename including a path and extension."))
                .AddWpf(name: "ConnectIntegrated", header: "Connect …", inputGesture: "Ctrl+Shift+O")
                .AddWpf(name: "Save", header: "_Save", inputGesture: "Ctrl+S")
                .AddWpf(name: "SaveAs", header: "_Save as …")
                .AddWpf(name: "Close", header: "_Close …")
                .AddWpf(name: "CheckAndFix", header: "Check, validate and fix …")
                .AddMenu(header: "Security …", childs: (new AasxMenu())
                    .AddWpf(name: "Sign", header: "_Sign (Submodel, Package) …",
                        help: "Sign a Submodel or SubmodelElement.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("UseX509", "Use X509 (true) or Verifiable Credential (false)")
                            .Add("Source", "Source package (.aasx) file.")
                            .Add("Certificate", "Certificate (.cer) file.")
                            .Add("Target", "Target package (.aasx2) file."))
                    .AddWpf(name: "ValidateCertificate", header: "_Validate (Submodel, Package) …",
                        help: "Validate a already signed Submodel or SubmodelElement.")
                    .AddWpf(name: "Encrypt", header: "_Encrypt (Package) …",
                        help: "Encrypts a Submodel, SubmodelElement or Package. For the latter, the arguments " +
                              "are required.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Source", "Source package (.aasx) file.")
                            .Add("Certificate", "Certificate (.cer) file.")
                            .Add("Target", "Target package (.aasx2) file."))
                    .AddWpf(name: "Decrypt", header: "_Decrypt (Package) …",
                        help: "Decrypts a Package.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Source", "Source package (.aasx2) file.")
                            .Add("Certificate", "Certificate (.pfx) file.")
                            .Add("Target", "Target package (.aasx) file.")))
                .AddSeparator()
                .AddWpf(name: "OpenAux", header: "Open Au_xiliary AAS …", inputGesture: "Ctrl+X",
                    help: "Open existing AASX package to the auxiliary buffer (non visible in the tree).",
                    args: new AasxMenuListOfArgDefs()
                        .Add("File", "Source filename including a path and extension."))
                .AddWpf(name: "CloseAux", header: "Close Auxiliary AAS")
                .AddSeparator()
                .AddMenu(header: "Further connect options …", childs: (new AasxMenu())
                    .AddWpf(name: "ConnectSecure", header: "Secure Connect …", inputGesture: "Ctrl+Shift+O")
                    .AddWpf(name: "ConnectOpcUa", header: "Connect via OPC-UA …")
                    .AddWpf(name: "ConnectRest", header: "Connect via REST …", inputGesture: "F6"))
                .AddSeparator()
                .AddMenu(header: "AASX File Repository …", childs: (new AasxMenu())
                    .AddWpf(name: "FileRepoNew", header: "New (local) repository …",
                        help: "Create new (empty) file repository.")
                    .AddWpf(name: "FileRepoOpen", header: "Open (local) repository …",
                        help: "Opens an existing AASX file repository and adds it to the list of open repos.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "Path and filename of existing AASX file repository."))
                    .AddWpf(name: "FileRepoConnectRepository", header: "Connect HTTP/REST repository …",
                        help: "Connects to an online repository via HTTP/REST.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Endpoint", "Endpoint of repo (without \"/server/listaas\")."))
                    .AddWpf(name: "FileRepoConnectRegistry", header: "Query HTTP/REST registry …")
                    .AddSeparator()
                    .AddWpf(name: "FileRepoCreateLRU", header: "Create last recently used list …")
                    .AddSeparator()
                    .AddWpf(name: "FileRepoQuery", header: "Query open repositories …", inputGesture: "F12",
                        help: "Selects and repository item (AASX) from the open AASX file repositories.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Index", "Zero-based integer index to the list of all open repos.")
                            .Add("AAS", "String with AAS-Id")
                            .Add("Asset", "String with Asset-Id.")))
                .AddSeparator()
                .AddMenu(header: "Import …", childs: (new AasxMenu())
                    .AddWpf(name: "ImportAML", header: "Import AutomationML into AASX …",
                        help: "Import AML file with AAS entities to overall AAS environment.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "AML file with AAS entities data."))
                    .AddWpf(name: "SubmodelRead", header: "Import Submodel from JSON …",
                        help: "Read Submodel from JSON and add/ replace existing to current AAS.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON file with Submodel data."))
                    .AddWpf(name: "SubmodelGet", header: "GET Submodel from URL …",
                        help: "Get Submodel from REST server.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("URL", "URL to get Submodel data from."))
                    .AddWpf(name: "ImportDictSubmodel", header: "Import Submodel from Dictionary …",
                        help: "UI assisted import from dictionaries such as ECLASS and IEC CDD to a Submodel.")
                    .AddWpf(name: "ImportDictSubmodelElements", header: "Import Submodel Elements from Dictionary …",
                        help: "UI assisted import from dictionaries such as ECLASS and IEC CDD to SubmodelElement.")
                    .AddWpf(name: "BMEcatImport", header: "Import BMEcat-file into SubModel …",
                        help: "Import BMEcat data into an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "BMEcat file with data."))
                    .AddWpf(name: "SubmodelTDImport", header: "Import Thing Description JSON LD document into SubModel …",
                        help: "Import Thing Description (TD) file in JSON LD format into an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON LD file with TD data."))
                    .AddWpf(name: "CSVImport", header: "Import CSV-file into SubModel …",
                        help: "Import comma separated values (CSV) into an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "CSV file with data."))
                    .AddWpf(name: "OPCUAi4aasImport", header: "Import AAS from i4aas-nodeset …")
                    .AddWpf(name: "OpcUaImportNodeSet", header: "Import OPC UA nodeset.xml as Submodel …",
                        help: "Import OPC UA nodeset.xml into an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "OPC UA Nodeset file."))
                    .AddWpf(name: "OPCRead", header: "Read OPC values into SubModel …",
                        help: "Use Qualifiers attributed in a Submodel to read actual OPC UA values.")
                    .AddWpf(name: "RDFRead", header: "Import BAMM RDF into AASX …",
                        help: "Import BAMM RDF into AASX.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "BAMM file with RDF data."))
                    .AddWpf(name: "ImportTimeSeries", header: "Read time series values into SubModel …",
                            help: "Import sets of time series values from an table in common format.",
                            args: new AasxMenuListOfArgDefs()
                                .Add("File", "Filename and path of file to imported.")
                                .Add("Format", "Format to be 'Excel'.")
                                .Add("Record", "Record data", hidden: true)
                                .AddFromReflection(new ImportTimeSeriesRecord()))
                    .AddWpf(name: "ImportTable", header: "Import SubmodelElements from Table …",
                            help: "Import sets of SubmodelElements from table datat in multiple common formats.",
                            args: new AasxMenuListOfArgDefs()
                                .Add("File", "Filename and path of file to imported.")
                                .Add("Preset", "Name of preset to load.")
                                .Add("Format", "Format to be either " +
                                        "'Tab separated', 'LaTex', 'Word', 'Excel', 'Markdown'.")
                                .Add("Record", "Record data", hidden: true)))
                .AddMenu(header: "Export …", childs: (new AasxMenu())
                    .AddWpf(name: "ExportAML", header: "Export AutomationML …",
                        help: "Export AML file with AAS entities from AAS environment.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "AML file with AAS entities data.")
                            .Add("FilterIndex", "Set FilterIndex=2 for compact AML format."))
                    .AddWpf(name: "SubmodelWrite", header: "Export Submodel to JSON …",
                        help: "Write Submodel to JSON.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON file to write Submodel data to."))
                    .AddWpf(name: "SubmodelPut", header: "PUT Submodel to URL …",
                        help: "Put Submodel to REST server.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("URL", "URL to put Submodel data to."))
                    .AddWpf(name: "ExportCst", header: "Export to TeamCenter CST …",
                        help: "Export data to SIEMENS TeamCenter containing list of properties.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "Head-part of filenames to write data to."))
                    .AddWpf(name: "ExportJsonSchema", header: "Export JSON schema for Submodel Templates …",
                        help: "Export data in JSON schema format to describe AAS Submodel Templates.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON schema file to write data to."))
                    .AddWpf(name: "OPCUAi4aasExport", header: "Export AAS as i4aas-nodeset …",
                        help: "Export OPC UA Nodeset2.xml format as i4aas-nodeset.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "OPC UA Nodeset2.xml file to write."))
                    .AddWpf(name: "OpcUaExportNodeSetUaPlugin",
                        header: "Export OPC UA Nodeset2.xml (via UA server plug-in) …",
                        help: "Export OPC UA Nodeset2.xml format by starting OPC UA server in plugin and " +
                            "execute a post-process command.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "OPC UA Nodeset2.xml file to write."))
                    .AddWpf(name: "CopyClipboardElementJson", header: "Copy selected element JSON to clipboard", inputGesture: "Shift+Ctrl+C")
                    .AddWpf(name: "ExportGenericForms", header: "Export Submodel as options for GenericForms …")
                    .AddWpf(name: "ExportPredefineConcepts", header: "Export Submodel as snippet for PredefinedConcepts …")
                    .AddWpf(name: "SubmodelTDExport", header: "Export Submodel as Thing Description JSON LD document",
                        help: "Export Thing Description (TD) file in JSON LD format from an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON LD file with TD data."))
                    .AddWpf(name: "PrintAsset", header: "Print Asset as code sheet …",
                        help: "Prints a sheet with 2D codes for the selected asset.")
                    .AddWpf(name: "ExportSMD", header: "Export TeDZ Simulation Model Description (SMD) …",
                        help: "Export TeDZ Simulation Model Description (SMD).",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Machine", "Designation of the machine/ equipment.")
                            .Add("Model", "Model type, either 'Physical' or 'Signal'."))
                    .AddWpf(name: "ExportTable", header: "Export SubmodelElements as Table …",
                        help: "Export table(s) for sets of SubmodelElements in multiple common formats.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "Filename and path of file to exported.")
                            .Add("Preset", "Name of preset to load.")
                            .Add("Format", "Format to be either " +
                                    "'Tab separated', 'LaTex', 'Word', 'Excel', 'Markdown'.")
                            .Add("Record", "Record data", hidden: true))
                    .AddWpf(name: "ExportUml", header: "Export SubmodelElements as UML …",
                        help: "Export UML of SubmodelElements in multiple common formats.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "Filename and path of file to exported.")
                            .Add("Format", "Format to be either 'XMI v1.1', 'XML v2.1', 'PlantUML'.")
                            .Add("Record", "Record data", hidden: true)
                            .AddFromReflection(new ExportUmlRecord())))
                .AddSeparator()
                .AddMenu(header: "Server …", childs: (new AasxMenu())
                    .AddWpf(name: "ServerRest", header: "Serve AAS as REST …", inputGesture: "Shift+F6")
                    .AddWpf(name: "MQTTPub", header: "Publish AAS via MQTT …")
                    .AddSeparator()
                    .AddWpf(name: "ServerPluginEmptySample", header: "Plugin: Empty Sample …")
                    .AddWpf(name: "ServerPluginOPCUA", header: "Plugin: OPC UA …")
                    .AddWpf(name: "ServerPluginMQTT", header: "Plugin: MQTT …"))
                .AddSeparator()
                .AddWpf(name: "Exit", header: "_Exit", inputGesture: "Alt+F4"));

            //
            // Workspace
            //

            menu.AddMenu(header: "Workspace",
                childs: (new AasxMenu())
                .AddWpf(name: "EditMenu", header: "_Edit", inputGesture: "Ctrl+E",
                    onlyDisplay: true, isCheckable: true,
                    args: new AasxMenuListOfArgDefs()
                            .Add("Mode", "'True' to activate edit mode, 'False' to turn off."))
                .AddWpf(name: "HintsMenu", header: "_Hints", inputGesture: "Ctrl+H",
                    onlyDisplay: true, isCheckable: true, isChecked: true,
                    args: new AasxMenuListOfArgDefs()
                        .Add("Mode", "'True' to activate hints mode, 'False' to turn off."))
                .AddWpf(name: "Test", header: "Test")
                .AddSeparator()
                .AddWpf(name: "ToolsFindText", header: "Find …", inputGesture: "Ctrl+F",
                    args: new AasxMenuListOfArgDefs()
                        .AddFromReflection(new AasxSearchUtil.SearchOptions()))
                .AddWpf(name: "ToolsReplaceText", header: "Replace …", inputGesture: "Ctrl+Shift+H",
                    args: new AasxMenuListOfArgDefs()
                        .AddFromReflection(new AasxSearchUtil.SearchOptions())
                        .Add("Do", "Either do 'stay', 'forward' or 'all'."))
                .AddWpf(name: "ToolsFindForward", header: "Find Forward", inputGesture: "F3", isHidden: true)
                .AddWpf(name: "ToolsFindBackward", header: "Find Backward", inputGesture: "Shift+F3", isHidden: true)
                .AddWpf(name: "ToolsReplaceStay", header: "Replace and stay", isHidden: true)
                .AddWpf(name: "ToolsReplaceForward", header: "Replace and stay", isHidden: true)
                .AddWpf(name: "ToolsReplaceAll", header: "Replace all", isHidden: true)
                .AddSeparator()
                .AddMenu(header: "Editing locations …", childs: (new AasxMenu())
                    .AddWpf(name: "LocationPush", header: "Push location", inputGesture: "Ctrl+Shift+P")
                    .AddWpf(name: "LocationPop", header: "Pop location", inputGesture: "Ctrl+Shift+O"))
                .AddSeparator()
                .AddMenu(header: "Plugins …", childs: (new AasxMenu())
                    .AddWpf(name: "NewSubmodelFromPlugin", header: "New Submodel", inputGesture: "Ctrl+Shift+M",
                            help: "Creates a new Submodel based on defintions provided by plugin.",
                            args: new AasxMenuListOfArgDefs()
                                .Add("Name", "Name of the Submodel (partially)")
                                .Add("Record", "Record data", hidden: true)
                                .Add("SmRef", "Return: Submodel generated", hidden: true)))
                .AddSeparator()
                .AddWpf(name: "ConvertElement", header: "Convert …",
                        help: "Asks plugins if these could make offers to convert the current elements and " +
                            "subsequently converts the element.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Name", "Name of the potential offer (partially)")
                            .Add("Record", "Record data", hidden: true))
                .AddSeparator()
                .AddMenu(header: "Buffer …", childs: (new AasxMenu())
                    .AddWpf(name: "BufferClear", header: "Clear internal paste buffer"))
                .AddSeparator()
                .AddMenu(header: "Events …", childs: (new AasxMenu())
                    .AddWpf(name: "EventsShowLogMenu", header: "_Event log", inputGesture: "Ctrl+L",
                        onlyDisplay: true, isCheckable: true)
                    .AddWpf(name: "EventsResetLocks", header: "Reset interlocking"))
                .AddMenu(header: "Scripts …", childs: (new AasxMenu())
                    .AddWpf(name: "ScriptEditLaunch", header: "Edit & launch …", inputGesture: "Ctrl+Shift+L")));

            //
            // Options
            //

            menu.AddMenu(header: "Option",
                childs: (new AasxMenu())
                .AddWpf(name: "ShowIriMenu", header: "Show id as IRI", inputGesture: "Ctrl+I", isCheckable: true)
                .AddWpf(name: "VerboseConnect", header: "Verbose connect", isCheckable: true)
                .AddWpf(name: "FileRepoLoadWoPrompt", header: "Load without prompt", isCheckable: true)
                .AddWpf(name: "AnimateElements", header: "Animate elements", isCheckable: true)
                .AddWpf(name: "ObserveEvents", header: "ObserveEvents", isCheckable: true)
                .AddWpf(name: "CompressEvents", header: "Compress events", isCheckable: true));

            //
            // Help
            //

            menu.AddMenu(header: "Help",
                childs: (new AasxMenu())
                .AddWpf(name: "About", header: "About …")
                .AddWpf(name: "HelpGithub", header: "Help on Github …")
                .AddWpf(name: "FaqGithub", header: "FAQ on Github …")
                .AddWpf(name: "HelpIssues", header: "Issues on Github …")
                .AddWpf(name: "HelpOptionsInfo", header: "Available options …"));

            //
            // Hotkeys
            //

            menu.AddHotkey(name: "EditKey", gesture: "Ctrl+E")
                .AddHotkey(name: "HintsKey", gesture: "Ctrl+H")
                .AddHotkey(name: "ShowIriKey", gesture: "Ctrl+I")
                .AddHotkey(name: "EventsShowLogKey", gesture: "Ctrl+L");

            for (int i = 0; i < 9; i++)
                menu.AddHotkey(name: $"LaunchScript{i}", gesture: $"Ctrl+Shift+{i}");

            //
            // End
            //

            // menu.DefaultActionAsync = CommandBinding_GeneralDispatch;

            return menu;
        }


    }
}
