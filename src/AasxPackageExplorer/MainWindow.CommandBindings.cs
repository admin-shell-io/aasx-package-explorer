/*
Copyright (c) 2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AasxIntegrationBase;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using AnyUi;
using Jose;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    /// <summary>
    /// This partial class contains all command bindings, such as for the main menu, in order to reduce the
    /// complexity of MainWindow.xaml.cs
    /// </summary>
    public partial class MainWindow : Window, IFlyoutProvider, IAasxScriptRemoteInterface
    {
        private string lastFnForInitialDirectory = null;

        //// Note for UltraEdit:
        //// <MenuItem Header="([^"]+)"\s*(|InputGestureText="([^"]+)")\s*Command="{StaticResource (\w+)}"/>
        //// .AddWpf\(name: "\4", header: "\1", inputGesture: "\3"\)
        //// or
        //// <MenuItem Header="([^"]+)"\s+([^I]|InputGestureText="([^"]+)")(.*?)Command="{StaticResource (\w+)}"/>
        //// .AddWpf\(name: "\5", header: "\1", inputGesture: "\3", \4\)

        /// <summary>
        /// Dynamic construction of the main menu
        /// </summary>
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
                .AddWpf(name: "New", header: "_New …")
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

            menu.DefaultActionAsync = CommandBinding_GeneralDispatch;

            return menu;
        }

        //
        // Rest
        //

        public void RememberForInitialDirectory(string fn)
        {
            this.lastFnForInitialDirectory = fn;
        }

        public string DetermineInitialDirectory(string existingFn = null)
        {
            string res = null;

            if (existingFn != null)
                try
                {
                    res = System.IO.Path.GetDirectoryName(existingFn);
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

            // may be can used last?
            if (res == null && lastFnForInitialDirectory != null)
                try
                {
                    res = System.IO.Path.GetDirectoryName(lastFnForInitialDirectory);
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

            return res;
        }

        private void CommandExecution_RedrawAll()
        {
            // redraw everything
            RedrawAllAasxElements();
            RedrawElementView();
        }


        private void FillSelectedItem(AasxMenuActionTicket ticket = null)
        {
            // access
            if (ticket == null)
                return;

            // basics
            var ve = DisplayElements.SelectedItem;
            if (ve != null)
            {
                ticket.MainDataObject = ve.GetMainDataObject();
                ticket.DereferencedMainDataObject = ve.GetDereferencedMainDataObject();
            }

            // set
            if (DisplayElements.SelectedItem is VisualElementEnvironmentItem veei)
            {
                ticket.Package = veei.thePackage;
                ticket.Env = veei.theEnv;
            }

            if (DisplayElements.SelectedItem is VisualElementAdminShell veaas)
            {
                ticket.Package = veaas.thePackage;
                ticket.Env = veaas.theEnv;
                ticket.AAS = veaas.theAas;
            }

            if (DisplayElements.SelectedItem is VisualElementAsset veasset)
            {
                ticket.Env = veasset.theEnv;
                ticket.AssetInfo = veasset.theAsset;
            }

            if (DisplayElements.SelectedItem is VisualElementSubmodelRef vesmr)
            {
                ticket.Package = vesmr.thePackage;
                ticket.Env = vesmr.theEnv;
                ticket.Submodel = vesmr.theSubmodel;
                ticket.SubmodelRef = vesmr.theSubmodelRef;
            }

            if (DisplayElements.SelectedItem is VisualElementSubmodel vesm)
            {
                ticket.Package = _packageCentral?.Main;
                ticket.Env = vesm.theEnv;
                ticket.Submodel = vesm.theSubmodel;
            }

            if (DisplayElements.SelectedItem is VisualElementSubmodelElement vesme)
            {
                ticket.Package = _packageCentral?.Main;
                ticket.Env = vesme.theEnv;
                ticket.SubmodelElement = vesme.theWrapper;
            }

        }

        private async Task CommandBinding_GeneralDispatch(
            string cmd,
            AasxMenuItemBase menuItem,
            AasxMenuActionTicket ticket)
        {
            //
            // Start
            //

            if (cmd == null || ticket == null)
                return;

            var scriptmode = ticket.ScriptMode;

            FillSelectedItem(ticket);

            //
            // Dispatch
            //

            if (cmd == "new")
            {
                // start
                ticket.StartExec();

                // check user
                if (!scriptmode
                    && AnyUiMessageBoxResult.Yes != MessageBoxFlyoutShow(
                    "Create new Adminshell environment? This operation can not be reverted!", "AAS-ENV",
                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                    return;

                // do
                try
                {
                    // clear
                    ClearAllViews();
                    // create new AASX package
                    _packageCentral.MainItem.New();
                    // redraw
                    CommandExecution_RedrawAll();
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "when creating new AASX");
                    return;
                }
            }

            if (cmd == "open" || cmd == "openaux")
            {
                // start
                ticket.StartExec();

                // filename
                if (!MenuSelectOpenFilename(
                    ticket, "File",
                    "Open AASX",
                    null,
                    "AASX package files (*.aasx)|*.aasx|AAS XML file (*.xml)|*.xml|" +
                        "AAS JSON file (*.json)|*.json|All files (*.*)|*.*",
                    out var fn,
                    "Open AASX: No valid filename."))
                    return;

                // ok
                RememberForInitialDirectory(fn);

                switch (cmd)
                {
                    case "open":
                        UiLoadPackageWithNew(
                            _packageCentral.MainItem, null, fn, onlyAuxiliary: false,
                            storeFnToLRU: fn);
                        break;
                    case "openaux":
                        UiLoadPackageWithNew(
                            _packageCentral.AuxItem, null, fn, onlyAuxiliary: true);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected {nameof(cmd)}: {cmd}");
                }
            }

            if (cmd == "save")
            {
                // start
                ticket.StartExec();

                // open?
                if (!_packageCentral.MainStorable)
                {
                    _logic?.LogErrorToTicket(ticket, "No open AASX file to be saved.");
                    return;
                }

                // do
                try
                {
                    // save
                    await _packageCentral.MainItem.SaveAsAsync(runtimeOptions: _packageCentral.CentralRuntimeOptions);

                    // backup
                    if (Options.Curr.BackupDir != null)
                        _packageCentral.MainItem.Container.BackupInDir(
                            System.IO.Path.GetFullPath(Options.Curr.BackupDir),
                            Options.Curr.BackupFiles,
                            PackageContainerBase.BackupType.FullCopy);

                    // may be was saved to index
                    if (_packageCentral?.MainItem?.Container?.Env?.AasEnv != null)
                        _packageCentral.MainItem.Container.SignificantElements
                            = new IndexOfSignificantAasElements(_packageCentral.MainItem.Container.Env.AasEnv);

                    // may be was saved to flush events
                    CheckIfToFlushEvents();

                    // as saving changes the structure of pending supplementary files, re-display
                    RedrawAllAasxElements(keepFocus: true);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "when saving AASX");
                    return;
                }

                Log.Singleton.Info("AASX saved successfully: {0}", _packageCentral.MainItem.Filename);
            }

            if (cmd == "saveas")
            {
                // start
                ticket.StartExec();

                // open?
                if (!_packageCentral.MainStorable)
                {
                    _logic?.LogErrorToTicket(ticket, "No open AASX file to be saved.");
                    return;
                }

                // shall be a local file?!
                var isLocalFile = _packageCentral.MainItem.Container is PackageContainerLocalFile;
                if (!isLocalFile)
                    if (!scriptmode && AnyUiMessageBoxResult.Yes != MessageBoxFlyoutShow(
                        "Current AASX file is not a local file. Proceed and convert to local AASX file?",
                        "Save", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Hand))
                        return;

                // filename
                if (!MenuSelectSaveFilename(
                    ticket, "File",
                    "Save AASX package",
                    _packageCentral.Main.Filename,
                    "AASX package files (*.aasx)|*.aasx|AASX package files w/ JSON (*.aasx)|*.aasx|" +
                        (!isLocalFile ? "" : "AAS XML file (*.xml)|*.xml|AAS JSON file (*.json)|*.json|") +
                        "All files (*.*)|*.*",
                    out var fn, out var filterIndex,
                    "Save AASX: No valid filename."))
                    return;

                // do
                try
                {
                    // if not local, do a bit of voodoo ..
                    if (!isLocalFile && _packageCentral.MainItem.Container != null)
                    {
                        // establish local
                        if (!await _packageCentral.MainItem.Container.SaveLocalCopyAsync(
                            fn,
                            runtimeOptions: _packageCentral.CentralRuntimeOptions))
                        {
                            // Abort
                            MessageBoxFlyoutLogOrShow(
                                scriptmode, StoredPrint.Color.Red,
                                "Not able to copy current AASX file to local file. Aborting!",
                                "Save", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Hand);
                            return;
                        }

                        // re-load
                        UiLoadPackageWithNew(
                            _packageCentral.MainItem, null, fn, onlyAuxiliary: false,
                            storeFnToLRU: fn);
                        return;
                    }

                    //
                    // ELSE .. already local
                    //

                    // preferred format
                    var prefFmt = AdminShellPackageEnv.SerializationFormat.None;
                    if (filterIndex == 1)
                        prefFmt = AdminShellPackageEnv.SerializationFormat.Xml;
                    if (filterIndex == 2)
                        prefFmt = AdminShellPackageEnv.SerializationFormat.Json;

                    // save 
                    RememberForInitialDirectory(fn);
                    await _packageCentral.MainItem.SaveAsAsync(fn, prefFmt: prefFmt);

                    // backup (only for AASX)
                    if (filterIndex == 0)
                        if (Options.Curr.BackupDir != null)
                            _packageCentral.MainItem.Container.BackupInDir(
                                System.IO.Path.GetFullPath(Options.Curr.BackupDir),
                                Options.Curr.BackupFiles,
                                PackageContainerBase.BackupType.FullCopy);

                    // as saving changes the structure of pending supplementary files, re-display
                    RedrawAllAasxElements();
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "when saving AASX");
                    return;
                }
                Log.Singleton.Info("AASX saved successfully as: {0}", fn);

                // LRU?
                // record in LRU?
                try
                {
                    var lru = _packageCentral?.Repositories?.FindLRU();
                    if (lru != null)
                        lru.Push(_packageCentral?.MainItem?.Container as PackageContainerRepoItem, fn);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(
                        ex, $"When managing LRU files");
                    return;
                }
            }

            if (cmd == "close" && _packageCentral?.Main != null)
            {
                // start
                ticket.StartExec();

                if (!scriptmode && AnyUiMessageBoxResult.Yes != MessageBoxFlyoutShow(
                    "Do you want to close the open package? Please make sure that you have saved before.",
                    "Close Package?", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                    return;

                // do
                try
                {
                    _packageCentral.MainItem.Close();
                    RedrawAllAasxElements();
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "when closing AASX");
                }
            }

            if ((cmd == "sign" || cmd == "validatecertificate" || cmd == "encrypt") && _packageCentral?.Main != null)
            {
                // differentiate
                if (cmd == "sign" && (ticket.Submodel != null || ticket.SubmodelElement != null))
                {
                    // start
                    ticket.StartExec();

                    // ask user
                    var useX509 = false;
                    if (ticket["UseX509"] is bool buse)
                        useX509 = buse;
                    else
                        useX509 = (AnyUiMessageBoxResult.Yes == MessageBoxFlyoutShow(
                            "Use X509 (yes) or Verifiable Credential (No)?",
                            "X509 or VerifiableCredential",
                            AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Hand));
                    ticket["UseX509"] = useX509;

                    // further to logic
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);

                    // update
                    RedrawAllAasxElements();
                    RedrawElementView();
                    return;
                }

                if (cmd == "validatecertificate" && (ticket.Submodel != null || ticket.SubmodelElement != null))
                {
                    // start
                    ticket.StartExec();

                    // further to logic
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                    return;
                }

                // Porting (MIHO): this seems to be executed, if above functions are not engaged
                // suspecting: for whole AAS/ package or so ..

                // filename source
                if (!MenuSelectOpenFilenameToTicket(
                    ticket, "Source",
                    "Select source AASX file to be processed",
                    null,
                    "AASX package files (*.aasx)|*.aasx",
                    "For package sign/ validate/ encrypt: No valid filename for source given!"))
                    return;

                if (cmd == "encrypt")
                {
                    // filename cert
                    if (!MenuSelectOpenFilenameToTicket(
                        ticket, "Certificate",
                        "Select certificate file",
                        null,
                        ".cer files (*.cer)|*.cer",
                        "For package sign/ validate/ encrypt: No valid filename for certificate given!"))
                        return;

                    // ask also for target fn
                    if (!MenuSelectSaveFilenameToTicket(
                        ticket, "Target",
                        "Write encoded AASX package file",
                        ticket["Source"] + "2",
                        "AASX2 encrypted package files (*.aasx2)|*.aasx2",
                        "For package sign/ validate/ encrypt: No valid filename for target given!"))
                        return;

                }

                if (cmd == "sign")
                {
                    // filename cert is required here
                    if (!MenuSelectOpenFilenameToTicket(
                        ticket, "Certificate",
                        "Select certificate file",
                        null,
                        ".pfx files (*.pfx)|*.pfx",
                        "For package sign/ validate/ encrypt: No valid filename for certificate given!"))
                        return;
                }

                // now, generally start
                ticket.StartExec();

                // as OZ designed, put user feedback on the screen
                ticket.InvokeMessage = (err, msg) =>
                {
                    return MessageBoxFlyoutShow(
                        msg, "Operation", AnyUiMessageBoxButton.OKCancel,
                        err ? AnyUiMessageBoxImage.Error : AnyUiMessageBoxImage.Information);
                };

                // further to logic
                _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
            }

            if ((cmd == "decrypt") && _packageCentral.Main != null)
            {
                // start
                ticket.StartExec();

                // filename source
                if (!MenuSelectOpenFilenameToTicket(
                    ticket, "Source",
                    "Select source encrypted AASX file to be processed",
                    null,
                    "AASX2 encrypted package files (*.aasx2)|*.aasx2",
                    "For package decrypt: No valid filename for source given!"))
                    return;

                // filename cert
                if (!MenuSelectOpenFilenameToTicket(
                    ticket, "Certificate",
                    "Select source AASX file to be processed",
                    null,
                    ".pfx files (*.pfx)|*.pfx",
                    "For package decrypt: No valid filename for certificate given!"))
                    return;

                // ask also for target fn
                if (!MenuSelectSaveFilenameToTicket(
                    ticket, "Target",
                    "Write decoded AASX package file",
                    null,
                    "AASX package files (*.aasx)|*.aasx",
                    "For package decrypt: No valid filename for target given!"))
                    return;

                // now, generally start
                ticket.StartExec();

                // as OZ designed, put user feedback on the screen
                ticket.InvokeMessage = (err, msg) =>
                {
                    return MessageBoxFlyoutShow(
                        msg, "Operation", AnyUiMessageBoxButton.OKCancel,
                        err ? AnyUiMessageBoxImage.Error : AnyUiMessageBoxImage.Information);
                };

                // further to logic
                _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
            }

            if (cmd == "closeaux" && _packageCentral.AuxAvailable)
            {
                // start
                ticket.StartExec();

                //do
                try
                {
                    _packageCentral.AuxItem.Close();
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "when closing auxiliary AASX");
                }
            }

            if (cmd == "exit")
            {
                // start
                ticket.StartExec();

                // do
                System.Windows.Application.Current.Shutdown();
            }

            if (cmd == "connectopcua")
                MessageBoxFlyoutShow(
                    "In future versions, this feature will allow connecting to an online Administration Shell " +
                    "via OPC UA or similar.",
                    "Connect", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Hand);

            if (cmd == "about")
            {
                // start
                ticket.StartExec();

                // do
                var ab = new AboutBox(_pref);
                ab.ShowDialog();
            }

            if (cmd == "helpgithub")
            {
                // start
                ticket.StartExec();

                // do
                ShowHelp();
            }

            if (cmd == "faqgithub")
            {
                // start
                ticket.StartExec();

                // do
                BrowserDisplayLocalFile(
                    @"https://github.com/admin-shell-io/questions-and-answers/blob/master/README.md");
            }

            if (cmd == "helpissues")
            {
                // start
                ticket.StartExec();

                // do
                BrowserDisplayLocalFile(
                    @"https://github.com/admin-shell-io/aasx-package-explorer/issues");
            }

            if (cmd == "helpoptionsinfo")
            {
                // start
                ticket.StartExec();

                // do
                var st = Options.ReportOptions(Options.ReportOptionsFormat.Markdown, Options.Curr);
                var dlg = new MessageReportWindow(st,
                    windowTitle: "Report on active and possible options");
                dlg.ShowDialog();
            }

            if (cmd == "editkey")
                _mainMenu?.SetChecked("EditMenu", _mainMenu?.IsChecked("EditMenu") != true);

            if (cmd == "hintskey")
                _mainMenu?.SetChecked("HintsMenu", _mainMenu?.IsChecked("HintsMenu") != true);

            if (cmd == "showirikey")
                _mainMenu?.SetChecked("ShowIriMenu", _mainMenu?.IsChecked("ShowIriMenu") != true);

            if (cmd == "editmenu" || cmd == "editkey"
                || cmd == "hintsmenu" || cmd == "hintskey"
                || cmd == "showirimenu" || cmd == "showirikey")
            {
                // start
                ticket.StartExec();

                if (ticket.ScriptMode && cmd == "editmenu" && ticket["Mode"] is bool editMode)
                {
                    _mainMenu?.SetChecked("EditMenu", editMode);
                }

                if (ticket.ScriptMode && cmd == "hintsmenu" && ticket["Mode"] is bool hintsMode)
                {
                    _mainMenu?.SetChecked("HintsMenu", hintsMode);
                }

                // try to remember current selected data object
                object currMdo = null;
                if (DisplayElements.SelectedItem != null)
                    currMdo = DisplayElements.SelectedItem.GetMainDataObject();

                // edit mode affects the total element view
                RedrawAllAasxElements();
                // fake selection
                RedrawElementView();
                // select last object
                if (currMdo != null)
                {
                    DisplayElements.TrySelectMainDataObject(currMdo, wishExpanded: true);
                }
            }

            if (cmd == "test")
            {
                // start
                ticket.StartExec();

                // do
                DisplayElements.Test();
            }

            if (cmd == "bufferclear")
            {
                // start
                ticket.StartExec();

                // do
                DispEditEntityPanel.ClearPasteBuffer();
                Log.Singleton.Info("Internal copy/ paste buffer cleared. Pasting of external JSON elements " +
                    "enabled.");
            }

            if (cmd == "locationpush"
                && _editingLocations != null
                && DisplayElements.SelectedItem is VisualElementGeneric vege
                && vege.GetMainDataObject() != null)
            {
                ticket.StartExec();

                var loc = new EditingLocation()
                {
                    MainDataObject = vege.GetMainDataObject(),
                    IsExpanded = vege.IsExpanded
                };
                _editingLocations.Add(loc);
                Log.Singleton.Info("Editing Locations: pushed location.");
            }

            if (cmd == "locationpop"
                && _editingLocations != null
                && _editingLocations.Count > 0)
            {
                ticket.StartExec();

                var loc = _editingLocations.Last();
                _editingLocations.Remove(loc);
                Log.Singleton.Info("Editing Locations: popping location.");
                DisplayElements.ClearSelection();
                DisplayElements.TrySelectMainDataObject(loc.MainDataObject, wishExpanded: loc.IsExpanded);
            }

            if (cmd == "exportsmd")
                CommandBinding_ExportSMD(ticket);

            if (cmd == "printasset")
                CommandBinding_PrintAsset(ticket);

            if (cmd.StartsWith("filerepo"))
                await CommandBinding_FileRepoAll(cmd, ticket);

            if (cmd == "opcread")
            {
                // start
                ticket?.StartExec();

                // further to logic
                _logic?.CommandBinding_GeneralDispatch(cmd, ticket);

                // update
                RedrawAllAasxElements();
                RedrawElementView();
            }

            if (cmd == "submodelread" || cmd == "submodelwrite"
                || cmd == "submodelput" || cmd == "submodelget")
                CommandBinding_SubmodelReadWritePutGet(cmd, ticket);

            if (cmd == "rdfread")
                CommandBinding_RDFRead(cmd, ticket);

            if (cmd == "bmecatimport")
                CommandBinding_BMEcatImport(cmd, ticket);

            if (cmd == "csvimport")
                CommandBinding_CSVImport(cmd, ticket);

            if (cmd == "submodeltdimport" || cmd == "submodeltdexport")
                CommandBinding_SubmodelTdExportImport(cmd, ticket);

            if (cmd == "opcuaimportnodeset")
                CommandBinding_OpcUaImportNodeSet(cmd, ticket);

            if (cmd == "importdictsubmodel" || cmd == "importdictsubmodelelements")
                CommandBinding_ImportDictToSubmodel(cmd, ticket);

            if (cmd == "importaml" || cmd == "exportaml")
                CommandBinding_ImportExportAML(cmd, ticket);

            if (cmd == "opcuai4aasimport" || cmd == "opcuai4aasexport")
                CommandBinding_ExportOPCUANodeSet(cmd, ticket);

            // TODO (MIHO, 2022-11-19): stays in WPF (tightly integrated, command line shall do own version)
            if (cmd == "opcuaexportnodesetuaplugin")
                CommandBinding_ExportNodesetUaPlugin(cmd, ticket);

            // stays in WPF
            if (cmd == "serverrest")
                CommandBinding_ServerRest();

            // stays in WPF
            if (cmd == "mqttpub")
                CommandBinding_MQTTPub();

            // stays in WPF
            if (cmd == "connectintegrated")
                CommandBinding_ConnectIntegrated();

            // stays in WPF
            if (cmd == "connectsecure")
                CommandBinding_ConnectSecure();

            // stays in WPF, ask OZ
            if (cmd == "connectrest")
                CommandBinding_ConnectRest();

            // stays in WPF
            if (cmd == "copyclipboardelementjson")
                CommandBinding_CopyClipboardElementJson();

            if (cmd == "exportgenericforms")
            {
                // start
                ticket?.StartExec();

                // filename
                if (!MenuSelectSaveFilenameToTicket(
                    ticket, "File",
                    "Select options file for GenericForms to be exported",
                    "new.add-options.json",
                    "Options file for GenericForms (*.add-options.json)|*.add-options.json|All files (*.*)|*.*",
                    "Export GenericForms: No valid filename.",
                    argFilterIndex: "FilterIndex"))
                    return;

                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "When exporting GenericForms, an error occurred");
                }
            }

            if (cmd == "exportpredefineconcepts")
            {
                // start
                ticket?.StartExec();

                // filename
                if (!MenuSelectSaveFilenameToTicket(
                    ticket, "File",
                    "Select text file for PredefinedConcepts to be exported",
                    "new.txt",
                    "Text file for PredefinedConcepts (*.txt)|*.txt|All files (*.*)|*.*",
                    "Export PredefinedConcepts: No valid filename.",
                    argFilterIndex: "FilterIndex"))
                    return;

                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "When exporting PredefinedConcepts, an error occurred");
                }
            }

            if (cmd == "exporttable")
                CommandBinding_ExportImportTableUml(cmd, ticket, import: false);

            if (cmd == "importtable")
                CommandBinding_ExportImportTableUml(cmd, ticket, import: true);

            if (cmd == "exportuml")
                CommandBinding_ExportImportTableUml(cmd, ticket, exportUml: true);

            if (cmd == "importtimeseries")
                CommandBinding_ExportImportTableUml(cmd, ticket, importTimeSeries: true);

            if (cmd == "serverpluginemptysample")
                CommandBinding_ExecutePluginServer(
                    "EmptySample", "server-start", "server-stop", "Empty sample plug-in.");

            if (cmd == "serverpluginopcua")
                CommandBinding_ExecutePluginServer(
                    "AasxPluginUaNetServer", "server-start", "server-stop", "Plug-in for OPC UA Server for AASX.");

            if (cmd == "serverpluginmqtt")
                CommandBinding_ExecutePluginServer(
                    "AasxPluginMqttServer", "MQTTServer-start", "server-stop", "Plug-in for MQTT Server for AASX.");

            if (cmd == "newsubmodelfromplugin")
                CommandBinding_NewSubmodelFromPlugin(cmd, ticket);

            if (cmd == "convertelement")
                CommandBinding_ConvertElement(cmd, ticket);

            if (cmd == "toolsfindtext" || cmd == "toolsfindforward" || cmd == "toolsfindbackward"
                || cmd == "toolsreplacetext" || cmd == "toolsreplacestay" || cmd == "toolsreplaceforward"
                || cmd == "toolsreplaceall") await CommandBinding_ToolsFind(cmd, ticket);

            if (cmd == "checkandfix")
                CommandBinding_CheckAndFix();

            if (cmd == "eventsresetlocks")
            {
                Log.Singleton.Info($"Event interlocking reset. Status was: " +
                    $"update-value-pending={_eventHandling.UpdateValuePending}");

                _eventHandling.Reset();
            }

            if (cmd == "eventsshowlogkey")
                _mainMenu?.SetChecked("EventsShowLogMenu", _mainMenu?.IsChecked("EventsShowLogMenu") != true);

            if (cmd == "eventsshowlogkey" || cmd == "eventsshowlogmenu")
            {
                PanelConcurrentSetVisibleIfRequired(PanelConcurrentCheckIsVisible());
            }

            if (cmd == "scripteditlaunch" || cmd.StartsWith("launchscript"))
            {
                CommandBinding_ScriptEditLaunch(cmd, menuItem);
            }
        }

        public class EditingLocation
        {
            public object MainDataObject;
            public bool IsExpanded;
        }

        protected List<EditingLocation> _editingLocations = new List<EditingLocation>();

        public bool PanelConcurrentCheckIsVisible()
        {
            return _mainMenu?.IsChecked("EventsShowLogMenu") == true;
        }

        public void PanelConcurrentSetVisibleIfRequired(
            bool targetState, bool targetAgents = false, bool targetEvents = false)
        {
            if (!targetState)
            {
                RowDefinitionConcurrent.Height = new GridLength(0);
            }
            else
            {
                if (RowDefinitionConcurrent.Height.Value < 1.0)
                {
                    var desiredH = Math.Max(140.0, this.Height / 3.0);
                    RowDefinitionConcurrent.Height = new GridLength(desiredH);
                }

                if (targetEvents)
                    TabControlConcurrent.SelectedItem = TabItemConcurrentEvents;

                if (targetAgents)
                    TabControlConcurrent.SelectedItem = TabItemConcurrentAgents;
            }
        }

        public void CommandBinding_CheckAndFix()
        {
            // work on package
            var msgBoxHeadline = "Check, validate and fix ..";
            var env = _packageCentral.Main?.AasEnv;
            if (env == null)
            {
                MessageBoxFlyoutShow(
                    "No package/ environment open. Aborting.", msgBoxHeadline,
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // try to get results
            AasValidationRecordList recs = null;
            try
            {
                // validate (logically)
                recs = env.ValidateAll();

                // validate as XML
                var ms = new MemoryStream();
                _packageCentral.Main.SaveAs("noname.xml", true, AdminShellPackageEnv.SerializationFormat.Xml, ms,
                    saveOnlyCopy: true);
                ms.Flush();
                ms.Position = 0;
                AasSchemaValidation.ValidateXML(recs, ms);
                ms.Close();

                // validate as JSON
                var ms2 = new MemoryStream();
                _packageCentral.Main.SaveAs("noname.json", true, AdminShellPackageEnv.SerializationFormat.Json, ms2,
                    saveOnlyCopy: true);
                ms2.Flush();
                ms2.Position = 0;
                AasSchemaValidation.ValidateJSONAlternative(recs, ms2);
                ms2.Close();
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "Checking model contents");
                MessageBoxFlyoutShow(
                    "Error while checking model contents. Aborting.", msgBoxHeadline,
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // could be nothing
            if (recs.Count < 1)
            {
                MessageBoxFlyoutShow(
                   "No issues found. Done.", msgBoxHeadline,
                   AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                return;
            }

            // prompt for this list
            var uc = new ShowValidationResultsFlyout();
            uc.ValidationItems = recs;
            this.StartFlyoverModal(uc);
            if (uc.FixSelected)
            {
                // fix
                var fixes = recs.FindAll((r) =>
                {
                    var res = uc.DoHint && r.Severity == AasValidationSeverity.Hint
                        || uc.DoWarning && r.Severity == AasValidationSeverity.Warning
                        || uc.DoSpecViolation && r.Severity == AasValidationSeverity.SpecViolation
                        || uc.DoSchemaViolation && r.Severity == AasValidationSeverity.SchemaViolation;
                    return res;
                });

                int done = 0;
                try
                {
                    done = env.AutoFix(fixes);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "Fixing model contents");
                    MessageBoxFlyoutShow(
                        "Error while fixing issues. Aborting.", msgBoxHeadline,
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }

                // info
                MessageBoxFlyoutShow(
                   $"Corresponding {done} issues were fixed. Please check the changes and consider saving " +
                   "with a new filename.", msgBoxHeadline,
                   AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);

                // redraw
                CommandExecution_RedrawAll();
            }
        }

        public async Task CommandBinding_FileRepoAll(
            string cmd,
            AasxMenuActionTicket ticket = null)
        {
            // rely on ticket availability
            if (ticket == null)
                return;

            if (cmd == "filereponew")
            {
                ticket.StartExec();

                if (ticket.ScriptMode != true && AnyUiMessageBoxResult.OK != MessageBoxFlyoutShow(
                        "Create new (empty) file repository? It will be added to list of repos on the lower/ " +
                        "left of the screen.",
                        "AASX File Repository",
                        AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
                    return;

                this.UiAssertFileRepository(visible: true);
                _packageCentral.Repositories.AddAtTop(new PackageContainerListLocal());
            }

            if (cmd == "filerepoopen")
            {
                // start
                ticket.StartExec();

                // filename
                if (!MenuSelectOpenFilename(
                    ticket, "File",
                    "Select AASX file repository JSON file",
                    null,
                    "JSON files (*.JSON)|*.json|All files (*.*)|*.*",
                    out var repoFn,
                    "AASX file repository open: No valid filename."))
                    return;

                // ok
                var fr = this.UiLoadFileRepository(repoFn);
                this.UiAssertFileRepository(visible: true);
                _packageCentral.Repositories.AddAtTop(fr);
            }

            if (cmd == "filerepoconnectrepository")
            {
                ticket.StartExec();

                // read server address
                var endpoint = ticket["Endpoint"] as string;
                if (endpoint?.HasContent() != true)
                {
                    var uc = new TextBoxFlyout("REST endpoint (without \"/server/listaas\"):",
                            AnyUiMessageBoxImage.Question);
                    uc.Text = "" + Options.Curr.DefaultConnectRepositoryLocation;
                    this.StartFlyoverModal(uc);
                    if (!uc.Result)
                        return;
                    endpoint = uc.Text;
                }

                if (endpoint?.HasContent() != true)
                {
                    _logic?.LogErrorToTicket(ticket, "No endpoint for repository given!");
                    return;
                }

                // ok
                if (endpoint.Contains("asp.net"))
                {
#if TODO
                    var fileRepository = new PackageContainerAasxFileRepository(endpoint);
                    fileRepository.GeneratePackageRepository();
                    this.UiAssertFileRepository(visible: true);
                    _packageCentral.Repositories.AddAtTop(fileRepository);
#endif
                }
                else
                {
                    var fr = new PackageContainerListHttpRestRepository(endpoint);
                    await fr.SyncronizeFromServerAsync();
                    this.UiAssertFileRepository(visible: true);
                    _packageCentral.Repositories.AddAtTop(fr);
                }
            }

            if (cmd == "filerepoquery")
            {
                ticket.StartExec();

                // access
                if (_packageCentral.Repositories == null || _packageCentral.Repositories.Count < 1)
                {
                    _logic?.LogErrorToTicket(ticket,
                        "AASX File Repository: No repository currently available! Please open.");
                    return;
                }

                // make a lambda
                Action<PackageContainerRepoItem> lambda = (ri) =>
                {
                    var fr = _packageCentral.Repositories?.FindRepository(ri);

                    if (fr != null && ri?.Location != null)
                    {
                        // which file?
                        var loc = fr?.GetFullItemLocation(ri.Location);
                        if (loc == null)
                            return;

                        // start animation
                        fr.StartAnimation(ri,
                            PackageContainerRepoItem.VisualStateEnum.ReadFrom);

                        try
                        {
                            // load
                            Log.Singleton.Info("Switching to AASX repository location {0} ..", loc);
                            UiLoadPackageWithNew(
                                _packageCentral.MainItem, null, loc, onlyAuxiliary: false);
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(
                                ex, $"When switching to AASX repository location {loc}.");
                        }
                    }
                };

                // get the list of items
                var repoItems = _packageCentral.Repositories.EnumerateItems().ToList();

                // scripted?
                if (ticket["Index"] is int)
                {
                    var ri = (int)ticket["Index"];
                    if (ri < 0 || ri >= repoItems.Count)
                    {
                        _logic?.LogErrorToTicket(ticket, "Repo Query: Index out of bounds");
                        return;
                    }
                    lambda(repoItems[ri]);
                }
                else
                if (ticket["AAS"] is string aasid)
                {
                    var ri = _packageCentral.Repositories.FindByAasId(aasid);
                    if (ri == null)
                    {
                        _logic?.LogErrorToTicket(ticket, "Repo Query: AAS-Id not found");
                        return;
                    }
                    lambda(ri);
                }
                else
                if (ticket["Asset"] is string aid)
                {
                    var ri = _packageCentral.Repositories.FindByAssetId(aid);
                    if (ri == null)
                    {
                        _logic?.LogErrorToTicket(ticket, "Repo Query: Asset-Id not found");
                        return;
                    }
                    lambda(ri);
                }
                else
                {
                    // dialogue
                    var uc = new SelectFromRepositoryFlyout();
                    uc.Margin = new Thickness(10);
                    if (uc.LoadAasxRepoFile(items: repoItems))
                    {
                        uc.ControlClosed += () =>
                        {
                            lambda(uc.ResultItem);
                        };
                        this.StartFlyover(uc);
                    }
                }
            }

            if (cmd == "filerepocreatelru")
            {
                if (ticket.ScriptMode != true && AnyUiMessageBoxResult.OK != MessageBoxFlyoutShow(
                        "Create new (empty) \"Last Recently Used (LRU)\" list? " +
                        "It will be added to list of repos on the lower/ left of the screen. " +
                        "It will be saved under \"last-recently-used.json\" in the binaries folder. " +
                        "It will replace an existing LRU list w/o prompt!",
                        "Last Recently Used AASX Packages",
                        AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
                    return;

                ticket.StartExec();

                var lruFn = PackageContainerListLastRecentlyUsed.BuildDefaultFilename();
                try
                {
                    this.UiAssertFileRepository(visible: true);
                    var lruExist = _packageCentral?.Repositories?.FindLRU();
                    if (lruExist != null)
                        _packageCentral.Repositories.Remove(lruExist);
                    var lruNew = new PackageContainerListLastRecentlyUsed();
                    lruNew.Header = "Last Recently Used";
                    lruNew.SaveAs(lruFn);
                    this.UiAssertFileRepository(visible: true);
                    _packageCentral?.Repositories?.AddAtTop(lruNew);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex,
                        $"while initializing last recently used file in {lruFn}.");
                }
            }

            // Note: rest of the commands migrated to AasxRepoListControl
        }

        public void CommandBinding_ConnectSecure()
        {
            // make dialgue flyout
            var uc = new SecureConnectFlyout();
            uc.LoadPresets(Options.Curr.SecureConnectPresets);

            // modal dialogue
            this.StartFlyoverModal(uc, closingAction: () =>
            {
            });

            // succss?
            if (uc.Result == null)
                return;
            var preset = uc.Result;

            // make listing flyout
            var logger = new LogInstance();
            var uc2 = new LogMessageFlyout("Secure connecting ..", "Start secure connect ..", () =>
            {
                return logger.PopLastShortTermPrint();
            });
            uc2.EnableLargeScreen();

            // do some statistics
            Log.Singleton.Info("Start secure connect ..");
            Log.Singleton.Info("Protocol: {0}", preset.Protocol.Value);
            Log.Singleton.Info("AuthorizationServer: {0}", preset.AuthorizationServer.Value);
            Log.Singleton.Info("AasServer: {0}", preset.AasServer.Value);
            Log.Singleton.Info("CertificateFile: {0}", preset.CertificateFile.Value);
            Log.Singleton.Info("Password: {0}", preset.Password.Value);

            logger.Info("Protocol: {0}", preset.Protocol.Value);
            logger.Info("AuthorizationServer: {0}", preset.AuthorizationServer.Value);
            logger.Info("AasServer: {0}", preset.AasServer.Value);
            logger.Info("CertificateFile: {0}", preset.CertificateFile.Value);
            logger.Info("Password: {0}", preset.Password.Value);

            // start CONNECT as a worker (will start in the background)
            var worker = new BackgroundWorker();
            AdminShellPackageEnv envToload = null;
            worker.DoWork += (s1, e1) =>
            {
                for (int i = 0; i < 15; i++)
                {
                    var sb = new StringBuilder();
                    for (double j = 0; j < 1; j += 0.0025)
                        sb.Append($"{j}");
                    logger.Info("The output is: {0} gives {1} was {0}", i, sb.ToString());
                    logger.Info(StoredPrint.Color.Blue, "This is blue");
                    logger.Info(StoredPrint.Color.Red, "This is red");
                    logger.Error("This is an error!");
                    logger.InfoWithHyperlink(0, "This is an link", "(Link)", "https://www.google.de");
                    logger.Info("----");
                    Thread.Sleep(2134);
                }

                envToload = null;
            };
            worker.RunWorkerCompleted += (s1, e1) =>
            {
            };
            worker.RunWorkerAsync();

            // modal dialogue
            this.StartFlyoverModal(uc2, closingAction: () =>
            {
                // clean up
            });

            // commit Package
            if (envToload != null)
            {
            }

            // done
            Log.Singleton.Info("Secure connect done.");
        }

        public void CommandBinding_ConnectIntegrated()
        {
            // make dialogue flyout
            var uc = new IntegratedConnectFlyout(
                _packageCentral,
                initialLocation: "" /* "http://admin-shell-io.com:51310/server/getaasx/0" */,
                logger: new LogInstance());
            uc.LoadPresets(Options.Curr.IntegratedConnectPresets);

            // modal dialogue
            this.StartFlyoverModal(uc, closingAction: () =>
            {
            });

            // execute
            if (uc.Result && uc.ResultContainer != null)
            {
                Log.Singleton.Info($"For integrated connection, trying to take over " +
                    $"{uc.ResultContainer.ToString()} ..");
                try
                {
                    UiLoadPackageWithNew(
                        _packageCentral.MainItem, null, takeOverContainer: uc.ResultContainer, onlyAuxiliary: false);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"When opening {uc.ResultContainer.ToString()}");
                }
            }
        }

        public void CommandBinding_PrintAsset(
            AasxMenuActionTicket ticket)
        {
            // rely on ticket availability
            if (ticket == null)
                return;

            // start
            ticket?.StartExec();

            if (ticket.AAS == null || ticket.AssetInfo?.GlobalAssetId?.IsValid() != true)
            {
                _logic?.LogErrorToTicket(ticket,
                    "No asset selected or no asset identification for printing code sheet.");
                return;
            }

            // ok!
            // Note: WPF based; no command line possible
            try
            {
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                AasxPrintFunctions.PrintSingleAssetCodeSheet(ticket.AssetInfo.GlobalAssetId.Keys[0].Value, ticket.AAS.IdShort);
                if (Options.Curr.UseFlyovers) this.CloseFlyover();
            }
            catch (Exception ex)
            {
                _logic?.LogErrorToTicket(ticket, ex, "When printing");
            }
        }

        public void CommandBinding_ServerRest()
        {
#if TODO
            // make a logger
            var logger = new AasxRestServerLibrary.GrapevineLoggerToListOfStrings();

            // make listing flyout
            var uc = new LogMessageFlyout("AASX REST Server", "Starting REST server ..", () =>
            {
                var st = logger.Pop();
                return (st == null) ? null : new StoredPrint(st);
            });

            // start REST as a worker (will start in the background)
            var worker = new BackgroundWorker();
            worker.DoWork += (s1, e1) =>
            {
                AasxRestServerLibrary.AasxRestServer.Start(
                    _packageCentral.Main, Options.Curr.RestServerHost, Options.Curr.RestServerPort, logger);
            };
            worker.RunWorkerAsync();

            // modal dialogue
            this.StartFlyoverModal(uc, closingAction: () =>
            {
                AasxRestServerLibrary.AasxRestServer.Stop();
            });
#endif
        }

        public class FlyoutAgentMqttPublisher : FlyoutAgentBase
        {
#if TODO
            public AasxMqttClient.AnyUiDialogueDataMqttPublisher DiaData;
            public AasxMqttClient.GrapevineLoggerToStoredPrints Logger;
            public AasxMqttClient.MqttClient Client;
            public BackgroundWorker Worker;
#endif
        }

        public void CommandBinding_MQTTPub()
        {
#if TODO
            // make an agent
            var agent = new FlyoutAgentMqttPublisher();

            // ask for preferences
            agent.DiaData = AasxMqttClient.AnyUiDialogueDataMqttPublisher.CreateWithOptions("AASQ MQTT publisher ..",
                        jtoken: Options.Curr.MqttPublisherOptions);
            var uc1 = new MqttPublisherFlyout(agent.DiaData);
            this.StartFlyoverModal(uc1);
            if (!uc1.Result)
                return;

            // make a logger
            agent.Logger = new AasxMqttClient.GrapevineLoggerToStoredPrints();

            // make listing flyout
            var uc2 = new LogMessageFlyout("AASX MQTT Publisher", "Starting MQTT Client ..", () =>
            {
                var sp = agent.Logger.Pop();
                return sp;
            });
            uc2.Agent = agent;

            // start MQTT Client as a worker (will start in the background)
            agent.Client = new AasxMqttClient.MqttClient();
            agent.Worker = new BackgroundWorker();
            agent.Worker.DoWork += async (s1, e1) =>
            {
                try
                {
                    await agent.Client.StartAsync(_packageCentral.Main, agent.DiaData, agent.Logger);
                }
                catch (Exception e)
                {
                    agent.Logger.Error(e);
                }
            };
            agent.Worker.RunWorkerAsync();

            // wire events
            agent.EventTriggered += (ev) =>
            {
                // trivial
                if (ev == null)
                    return;

                // safe
                try
                {
                    // potentially expensive .. get more context for the event source
                    AdminShell.ReferableRootInfo foundRI = null;
                    if (_packageCentral != null && ev.Source?.Keys != null)
                        foreach (var pck in _packageCentral.GetAllPackageEnv())
                        {
                            var ri = new AdminShell.ReferableRootInfo();
                            var res = pck?.AasEnv?.FindReferableByReference(ev.Source.Keys, rootInfo: ri);
                            if (res != null && ri.IsValid)
                                foundRI = ri;
                        }

                    // publish
                    agent.Client?.PublishEvent(ev, foundRI);
                }
                catch (Exception e)
                {
                    agent.Logger.Error(e);
                }
            };

            agent.GenerateFlyoutMini = () =>
            {
                var storedAgent = agent;
                var mini = new LogMessageMiniFlyout("AASX MQTT Publisher", "Executing minimized ..", () =>
                {
                    var sp = storedAgent.Logger.Pop();
                    return sp;
                });
                mini.Agent = agent;
                return mini;
            };

            // modal dialogue
            this.StartFlyoverModal(uc2, closingAction: () => { });
#endif
        }

        static string lastConnectInput = "";
        public async void CommandBinding_ConnectRest()
        {
            var uc = new TextBoxFlyout("REST server adress:", AnyUiMessageBoxImage.Question);
            if (lastConnectInput == "")
            {
                uc.Text = "http://" + Options.Curr.RestServerHost + ":" + Options.Curr.RestServerPort;
            }
            else
            {
                uc.Text = lastConnectInput;
            }
            this.StartFlyoverModal(uc);
            if (uc.Result)
            {
                string value = "";
                string input = uc.Text.ToLower();
                lastConnectInput = input;
                if (!input.StartsWith("http://localhost:1111"))
                {
                    string tag = "";
                    bool connect = false;

                    if (input.Contains("/getaasxbyassetid/")) // get by AssetID
                    {
                        if (_packageCentral.MainAvailable)
                            _packageCentral.MainItem.Close();
                        System.IO.File.Delete(AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx");

                        var handler = new HttpClientHandler();
                        handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                        //// handler.AllowAutoRedirect = false;

                        string dataServer = new Uri(input).GetLeftPart(UriPartial.Authority);

                        var client = new HttpClient(handler)
                        {
                            BaseAddress = new Uri(dataServer)
                        };
                        input = input.Substring(dataServer.Length, input.Length - dataServer.Length);
                        client.DefaultRequestHeaders.Add("Accept", "application/aas");
                        var response2 = await client.GetAsync(input);

                        // ReSharper disable PossibleNullReferenceException
                        var contentStream = await response2?.Content?.ReadAsStreamAsync();
                        if (contentStream == null)
                            return;
                        // ReSharper enable PossibleNullReferenceException

                        string outputDir = ".";
                        Console.WriteLine("Writing file: " + outputDir + "\\" + "download.aasx");
                        using (var file = new FileStream(outputDir + "\\" + "download.aasx",
                            FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await contentStream.CopyToAsync(file);
                        }

                        if (System.IO.File.Exists(AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx"))
                            UiLoadPackageWithNew(
                                _packageCentral.MainItem,
                                null,
                                AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx", onlyAuxiliary: false);
                        return;
                    }
                    else
                    {
                        tag = "http";
                        tag = input.Substring(0, tag.Length);
                        if (tag == "http")
                        {
                            connect = true;
                            tag = "openid ";
                            value = input;
                        }
                        else
                        {
                            tag = "openid1";
                            tag = input.Substring(0, tag.Length);
                            if (tag == "openid " || tag == "openid1" || tag == "openid2" || tag == "openid3")
                            {
                                connect = true;
                                value = input.Substring(tag.Length);
                            }
                        }
                    }

                    if (connect)
                    {
                        if (_packageCentral.MainAvailable)
                            _packageCentral.MainItem.Close();
                        System.IO.File.Delete(AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx");
                        await AasxOpenIdClient.OpenIDClient.Run(tag, value/*, this*/);

                        if (System.IO.File.Exists(AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx"))
                            UiLoadPackageWithNew(
                                _packageCentral.MainItem,
                                null,
                                AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx", onlyAuxiliary: false);
                    }
                }
                else
                {
                    var url = uc.Text;
                    Log.Singleton.Info($"Connecting to REST server {url} ..");

                    try
                    {
#if TODO
                        var client = new AasxRestServerLibrary.AasxRestClient(url);
                        theOnlineConnection = client;
                        var pe = client.OpenPackageByAasEnv();
                        if (pe != null)
                            UiLoadPackageWithNew(_packageCentral.MainItem, pe, info: uc.Text, onlyAuxiliary: false);
#endif
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, $"Connecting to REST server {url}");
                    }
                }
            }
        }

        public void CommandBinding_BMEcatImport(
            string cmd,
            AasxMenuActionTicket ticket = null)
        {
            // rely on ticket availability
            if (ticket == null)
                return;

            if (cmd == "bmecatimport")
            {
                // filename
                if (!MenuSelectOpenFilenameToTicket(
                    ticket, "File",
                    "Select BMEcat file to be imported",
                    null,
                    "BMEcat XML files (*.bmecat)|*.bmecat|All files (*.*)|*.*",
                    "RDF Read: No valid filename."))
                    return;


                // do it
                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex,
                        "When importing BMEcat, an error occurred");
                }
            }
        }

        public void CommandBinding_CSVImport(
            string cmd,
            AasxMenuActionTicket ticket = null)
        {
            // rely on ticket availability
            if (ticket == null)
                return;

            if (cmd == "csvimport")
            {
                // filename
                if (!MenuSelectOpenFilenameToTicket(
                    ticket, "File",
                    "Select CSF file to be imported",
                    null,
                    "CSV files (*.CSV)|*.csv|All files (*.*)|*.*",
                    "CSF inmport: No valid filename."))
                    return;


                // do it
                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex,
                        "When importing CSV, an error occurred");
                }
            }
        }

        public void CommandBinding_OpcUaImportNodeSet(
            string cmd,
            AasxMenuActionTicket ticket = null)
        {
            // rely on ticket availability
            if (ticket == null)
                return;

            if (cmd == "opcuaimportnodeset")
            {
                // filename
                if (!MenuSelectOpenFilenameToTicket(
                    ticket, "File",
                    "Select OPC UA Nodeset to be imported",
                    null,
                    "OPC UA NodeSet XML files (*.XML)|*.XML|All files (*.*)|*.*",
                    "OPC UA Nodeset import: No valid filename."))
                    return;

                // do it
                try
                {
                    // do it
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);

                    // redisplay
                    RedrawAllAasxElements();
                    RedrawElementView();
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex,
                        "When importing OPC UA Nodeset, an error occurred");
                }
            }
        }

        private void CommandBinding_ExecutePluginServer(
            string pluginName, string actionName, string stopName, string caption, string[] additionalArgs = null)
        {
            // check
            var pi = Plugins.FindPluginInstance(pluginName);
            if (pi == null || !pi.HasAction(actionName) || !pi.HasAction(stopName))
            {
                var res = MessageBoxFlyoutShow(
                        $"This function requires a binary plug-in file named '{pluginName}', " +
                        $"which needs to be added to the command line, with an action named '{actionName}'. " +
                        "Press 'OK' to show help page on GitHub.",
                        "Plug-in not present",
                        AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand);
                if (res == AnyUiMessageBoxResult.OK)
                {
                    ShowHelp();
                }
                return;
            }

            // activate server via plugin
            // make listing flyout
            var uc = new LogMessageFlyout(caption, $"Starting plug-in {pluginName}, action {actionName} ..", () =>
            {
                return this.FlyoutLoggingPop();
            });

            this.FlyoutLoggingStart();

            uc.ControlCloseWarnTime = 10000;
            uc.ControlWillBeClosed += () =>
            {
                uc.LogMessage("Initiating closing (wait at max 10sec) ..");
                pi.InvokeAction(stopName);
            };
            uc.AddPatternError(new Regex(@"^\[1\]"));

            // start server as a worker (will start in the background)
            var worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (s1, e1) =>
            {
                try
                {
                    // total argument list
                    var totalArgs = new List<string>();
                    if (pi.args != null)
                        totalArgs.AddRange(pi.args);
                    if (additionalArgs != null)
                        totalArgs.AddRange(additionalArgs);

                    // invoke
                    pi.InvokeAction(actionName, _packageCentral.Main, totalArgs.ToArray());

                }
                catch (Exception ex)
                {
                    uc.LogMessage("Exception in plug-in: " + ex.Message + " in " + ex.StackTrace);
                    uc.LogMessage("Stopping...");
                    Thread.Sleep(5000);
                }
            };
            worker.RunWorkerCompleted += (s1, e1) =>
            {
                // in any case, close flyover
                this.FlyoutLoggingStop();
                uc.LogMessage("Completed.");
                uc.CloseControlExplicit();
            };
            worker.RunWorkerAsync();

            // modal dialogue
            this.StartFlyoverModal(uc, closingAction: () =>
            {
#if FALSE
                if (false && worker.IsBusy)
                    try
                    {
                        worker.CancelAsync();
                        worker.Dispose();
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }
#endif
            });
        }

        /// <summary>
        /// Selects Submodel and Env from DisplayElements.
        /// In future, may be take from ticket.
        /// Checks, if these are not <c>NULL</c> or logs a message.
        /// </summary>
        /// <returns>Success</returns>
        public bool MenuSelectEnvSubmodel(
            AasxMenuActionTicket ticket,
            out AasCore.Aas3_0_RC02.Environment env,
            out AasCore.Aas3_0_RC02.Submodel sm,
            out AasCore.Aas3_0_RC02.Reference smr,
            string msg)
        {
            env = null;
            sm = null;
            smr = null;
            if (DisplayElements.SelectedItem is VisualElementSubmodelRef vesmr)
            {
                env = vesmr.theEnv;
                sm = vesmr.theSubmodel;
                smr = vesmr.theSubmodelRef;
            }
            if (DisplayElements.SelectedItem is VisualElementSubmodel vesm)
            {
                env = vesm.theEnv;
                sm = vesm.theSubmodel;
            }

            if (sm == null || env == null)
            {
                _logic?.LogErrorToTicket(ticket, "Submodel Read: No valid SubModel selected.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Selects a filename to read either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public bool MenuSelectOpenFilename(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            out string sourceFn,
            string msg)
        {
            // filename
            sourceFn = ticket?[argName] as string;

            if (sourceFn?.HasContent() != true)
            {
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                if (caption != null)
                    dlg.Title = caption;
                if (proposeFn != null)
                    dlg.FileName = proposeFn;
                if (filter != null)
                    dlg.Filter = filter;

                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                if (true == dlg.ShowDialog())
                    sourceFn = dlg.FileName;
                if (Options.Curr.UseFlyovers) this.CloseFlyover();
            }

            if (sourceFn?.HasContent() != true)
            {
                _logic?.LogErrorToTicketOrSilent(ticket, msg);
                return false;
            }

            return true;
        }

        /// <summary>
        /// If ticket does not contain the filename named by <c>argName</c>,
        /// read it by the user.
        /// </summary>
        public bool MenuSelectOpenFilenameToTicket(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg)
        {
            if (MenuSelectOpenFilename(ticket, argName, caption, proposeFn, filter, out var sourceFn, msg))
            {
                ticket[argName] = sourceFn;
                RememberForInitialDirectory(sourceFn);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects a filename to write either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public bool MenuSelectSaveFilename(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            out string targetFn,
            out int filterIndex,
            string msg)
        {
            // filename
            targetFn = ticket?[argName] as string;
            filterIndex = 0;

            if (targetFn?.HasContent() != true)
            {
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                if (caption != null)
                    dlg.Title = caption;
                if (proposeFn != null)
                    dlg.FileName = proposeFn;
                if (filter != null)
                    dlg.Filter = filter;

                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                if (true == dlg.ShowDialog())
                {
                    targetFn = dlg.FileName;
                    filterIndex = dlg.FilterIndex;
                }
                if (Options.Curr.UseFlyovers) this.CloseFlyover();
            }

            if (targetFn?.HasContent() != true)
            {
                _logic?.LogErrorToTicketOrSilent(ticket, msg);
                return false;
            }

            return true;
        }

        /// <summary>
        /// If ticket does not contain the filename named by <c>argName</c>,
        /// read it by the user.
        /// </summary>
        public bool MenuSelectSaveFilenameToTicket(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg,
            string argFilterIndex = null)
        {
            if (MenuSelectSaveFilename(ticket, argName, caption, proposeFn, filter,
                    out var targetFn, out var filterIndex, msg))
            {
                RememberForInitialDirectory(targetFn);
                ticket[argName] = targetFn;
                if (argFilterIndex?.HasContent() == true)
                    ticket[argFilterIndex] = filterIndex;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public bool MenuSelectText(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeText,
            out string targetText,
            string msg)
        {
            // filename
            targetText = ticket?[argName] as string;

            if (targetText?.HasContent() != true)
            {
                var uc = new TextBoxFlyout(caption, AnyUiMessageBoxImage.Question);
                uc.Text = proposeText;
                this.StartFlyoverModal(uc);
                if (uc.Result)
                    targetText = uc.Text;
            }

            if (targetText?.HasContent() != true)
            {
                _logic?.LogErrorToTicketOrSilent(ticket, msg);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public bool MenuSelectTextToTicket(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeText,
            string msg)
        {
            if (MenuSelectText(ticket, argName, caption, proposeText, out var targetText, msg))
            {
                ticket[argName] = targetText;
                return true;
            }
            return false;
        }

        protected static string _userLastPutUrl = "http://???:51310";
        protected static string _userLastGetUrl = "http://???:51310";

        public void CommandBinding_SubmodelReadWritePutGet(
            string cmd,
            AasxMenuActionTicket ticket = null)
        {
            // rely on ticket availability
            if (ticket == null)
                return;

            if (cmd == "submodelread")
            {
                // start
                ticket?.StartExec();

                // filename
                if (!MenuSelectOpenFilenameToTicket(
                    ticket, "File",
                    "Read Submodel from JSON data",
                    "Submodel_" + ticket?.Submodel?.IdShort + ".json",
                    "JSON files (*.JSON)|*.json|All files (*.*)|*.*",
                    "Submodel Read: No valid filename."))
                    return;

                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);

                    RedrawAllAasxElements();
                    RedrawElementView();
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "Submodel Read");
                }
            }

            if (cmd == "submodelwrite")
            {
                // start
                ticket.StartExec();

                // filename
                if (!MenuSelectSaveFilenameToTicket(
                    ticket, "File",
                    "Write Submodel to JSON data",
                    "Submodel_" + ticket.Submodel?.IdShort + ".json",
                    "JSON files (*.JSON)|*.json|All files (*.*)|*.*",
                    "Submodel Read: No valid filename."))
                    return;

                // do it directly
                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "Submodel Write");
                }
            }

            if (cmd == "submodelput")
            {
                // start
                ticket.StartExec();

                // URL
                if (!MenuSelectTextToTicket(
                    ticket, "URL",
                    "REST server adress:",
                    _userLastPutUrl,
                    "Submodel Put: No valid URL selected,"))
                    return;

                _userLastPutUrl = ticket["URL"] as string;

                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "Submodel Put");
                }
            }

            if (cmd == "submodelget")
            {
                // start
                ticket?.StartExec();

                // URL
                if (!MenuSelectTextToTicket(
                    ticket, "URL",
                    "REST server adress:",
                    _userLastGetUrl,
                    "Submodel Get: No valid URL selected,"))
                    return;

                _userLastGetUrl = ticket["URL"] as string;

                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "Submodel Get");
                }
            }

        }

        public void CommandBinding_ImportDictToSubmodel(
            string cmd,
            AasxMenuActionTicket ticket = null)
        {
            // These 2 functions are using WPF and cannot migrated to PackageLogic

            if (cmd == "importdictsubmodel")
            {
                // start
                ticket?.StartExec();

                // which item selected?
                AasCore.Aas3_0_RC02.Environment env = _packageCentral.Main.AasEnv;
                AasCore.Aas3_0_RC02.AssetAdministrationShell aas = null;
                if (DisplayElements.SelectedItem != null)
                {
                    if (DisplayElements.SelectedItem is VisualElementAdminShell aasItem)
                    {
                        // AAS is selected --> import into AAS
                        env = aasItem.theEnv;
                        aas = aasItem.theAas;
                    }
                    else if (DisplayElements.SelectedItem is VisualElementEnvironmentItem envItem &&
                            envItem.theItemType == VisualElementEnvironmentItem.ItemType.EmptySet)
                    {
                        // Empty environment is selected --> create new AAS
                        env = envItem.theEnv;
                    }
                    else
                    {
                        // Other element is selected --> error
                        _logic?.LogErrorToTicket(ticket,
                            "Dictionary Import: Please select the administration shell for the submodel import.");
                        return;
                    }
                }

#if !DoNotUseAasxDictionaryImport
                var dataChanged = false;
                try
                {
#if TODO
                    dataChanged = AasxDictionaryImport.Import.ImportSubmodel(this, env, Options.Curr.DictImportDir, aas);
#endif
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "An error occurred during the Dictionary import.");
                }

                if (dataChanged)
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    RestartUIafterNewPackage();
                    Mouse.OverrideCursor = null;
                }
#endif
                }

            if (cmd == "importdictsubmodelelements")
            {
                // start
                ticket?.StartExec();

                // current Submodel
                // ReSharper disable UnusedVariable
                if (!MenuSelectEnvSubmodel(
                    ticket,
                    out var env, out var sm, out var smr,
                    "Dictionary import: No valid Submodel selected."))
                    return;
                // ReSharper enable UnusedVariable

#if !DoNotUseAasxDictionaryImport
                var dataChanged = false;
                try
                {
#if TODO
                    dataChanged = AasxDictionaryImport.Import.ImportSubmodelElements(
                        this, env, Options.Curr.DictImportDir, sm);
#endif
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "An error occurred during the submodel element import.");
                }

                if (dataChanged)
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    RestartUIafterNewPackage();
                    Mouse.OverrideCursor = null;
                }
#endif
                }
        }


        public void CommandBinding_ImportExportAML(
            string cmd,
            AasxMenuActionTicket ticket = null)
        {
            // rely on ticket availability
            if (ticket == null)
                return;

            if (cmd == "importaml")
            {
                // start
                ticket?.StartExec();

                // filename
                if (!MenuSelectOpenFilenameToTicket(
                    ticket, "File",
                    "Select AML file to be imported",
                    null,
                    "AutomationML files (*.aml)|*.aml|All files (*.*)|*.*",
                    "Import AML: No valid filename."))
                    return;

                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                    this.RestartUIafterNewPackage();
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "When importing AML, an error occurred");
                }
            }

            if (cmd == "exportaml")
            {
                // start
                ticket?.StartExec();

                // filename
                if (!MenuSelectSaveFilenameToTicket(
                    ticket, "File",
                    "Select AML file to be exported",
                    "new.aml",
                    "AutomationML files (*.aml)|*.aml|AutomationML files (*.aml) (compact)|" +
                    "*.aml|All files (*.*)|*.*",
                    "Export AML: No valid filename.",
                    argFilterIndex: "FilterIndex"))
                    return;

                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "When exporting AML, an error occurred");
                }
            }
        }

        public void CommandBinding_RDFRead(
            string cmd,
            AasxMenuActionTicket ticket = null)

        {
            // rely on ticket availability
            if (ticket == null)
                return;

            if (cmd == "rdfread")
            {
                // filename
                if (!MenuSelectOpenFilenameToTicket(
                    ticket, "File",
                    "Select RDF file to be imported",
                    null,
                    "BAMM files (*.ttl)|*.ttl|All files (*.*)|*.*",
                    "RDF Read: No valid filename."))
                    return;

                // do it
                try
                {
                    // do it
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);

                    // redisplay
                    RedrawAllAasxElements();
                    RedrawElementView();
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex,
                        "When importing, an error occurred");
                }
            }
        }

        public void CommandBinding_ExportNodesetUaPlugin(
            string cmd,
            AasxMenuActionTicket ticket)
        {
            if (cmd == "opcuaexportnodesetuaplugin")
            {
                // filename
                // ReSharper disable UnusedVariable
                if (!MenuSelectSaveFilename(
                    ticket, "File",
                    "Select Nodeset2.XML file to be exported",
                    "new.xml",
                    "OPC UA Nodeset2 files (*.xml)|*.xml|All files (*.*)|*.*",
                    out var targetFn, out var filterIndex,
                    "Export OPC UA Nodeset2 via plugin: No valid filename."))
                    return;
                // ReSharper enable UnusedVariable
                try
                {
                    RememberForInitialDirectory(targetFn);
                    CommandBinding_ExecutePluginServer(
                        "AasxPluginUaNetServer",
                        "server-start",
                        "server-stop",
                        "Export Nodeset2 via OPC UA Server...",
                        new[] { "-export-nodeset", targetFn }
                        );
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(
                        ex, "When exporting UA nodeset via plug-in, an error occurred");
                }
            }
        }

        public void CommandBinding_CopyClipboardElementJson()
        {
            // get the selected element
            var ve = DisplayElements.SelectedItem;

            // allow only some elements
            if (!(ve is VisualElementConceptDescription
                || ve is VisualElementSubmodelElement
                || ve is VisualElementAdminShell
                || ve is VisualElementAsset
                || ve is VisualElementOperationVariable
                || ve is VisualElementReference
                || ve is VisualElementSubmodel
                || ve is VisualElementSubmodelRef))
                ve = null;

            // need to have business object
            var mdo = ve?.GetMainDataObject();

            if (ve == null || mdo == null)
            {
                MessageBoxFlyoutShow(
                    "No valid element selected.", "Copy selected elements",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // ok, for Serialization we just want the plain element with no BLOBs..
            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new AdminShellConverters.AdaptiveFilterContractResolver(
                deep: false, complete: false);
            var jsonStr = JsonConvert.SerializeObject(mdo, Formatting.Indented, settings);

            // copy to clipboard
            if (jsonStr != "")
            {
                System.Windows.Clipboard.SetText(jsonStr);
                Log.Singleton.Info("Copied selected element to clipboard.");
            }
            else
            {
                Log.Singleton.Info("No JSON text could be generated for selected element.");
            }
        }

        public void CommandBinding_ConvertElement(
            string cmd,
            AasxMenuActionTicket ticket)
        {
            // rely on ticket availability
            if (ticket == null)
                return;

            // check
            var rf = ticket.DereferencedMainDataObject as AasCore.Aas3_0_RC02.IReferable;
            if (rf == null)
            {
                _logic?.LogErrorToTicket(ticket,
                    "Convert Referable: No valid Referable selected for conversion.");
                return;
            }

            // try to get offers?
            if ((ticket["Name"] as string)?.HasContent() != true)
            {
                var offers = AasxPredefinedConcepts.Convert.ConvertPredefinedConcepts.CheckForOffers(rf);
                if (offers == null || offers.Count < 1)
                {
                    _logic?.LogErrorToTicket(ticket,
                        "Convert Referable: No valid conversion offers found for this Referable. Aborting.");
                    return;
                }

                // convert these to list items
                var fol = new List<AnyUiDialogueListItem>();
                foreach (var o in offers)
                    fol.Add(new AnyUiDialogueListItem(o.OfferDisplay, o));

                // show a list
                // prompt for this list
                var uc = new SelectFromListFlyout();
                uc.DiaData.Caption = "Select Conversion action to be executed ..";
                uc.DiaData.ListOfItems = fol;
                this.StartFlyoverModal(uc);
                if (uc.DiaData.ResultItem != null)
                    ticket["Record"] = uc.DiaData.ResultItem.Tag;
            }

            // pass on
            try
            {
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "Executing user defined conversion");
            }

            // redisplay
            // add to "normal" event quoue
            DispEditEntityPanel.AddWishForOutsideAction(
                new AnyUiLambdaActionRedrawAllElements(ticket.MainDataObject));
        }

        public void CommandBinding_ExportImportTableUml(
            string cmd,
            AasxMenuActionTicket ticket,
            bool import = false, bool exportUml = false, bool importTimeSeries = false)
        {
            // rely on ticket availability
            if (ticket == null)
                return;

            // start
            ticket.StartExec();

            // help (called later)
            Action callHelp = () =>
            {
                try
                {
                    BrowserDisplayLocalFile(
                        "https://github.com/admin-shell-io/aasx-package-explorer/" +
                        "tree/master/src/AasxPluginExportTable/help",
                        System.Net.Mime.MediaTypeNames.Text.Html,
                        preferInternal: true);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex,
                        $"Import/Export: While displaying html-based help.");
                }
            };

            if (cmd == "exporttable" || cmd == "importtable")
            {
                if (ticket?.ScriptMode != true)
                {
                    // interactive
                    // handle the export dialogue
                    var uc = new ExportTableFlyout((cmd == "exporttable")
                        ? "Export SubmodelElements as Table"
                        : "Import SubmodelElements from Table");
                    uc.Presets = _logic?.GetImportExportTablePreset().Item1;

                    StartFlyoverModal(uc);

                    if (uc.CloseForHelp)
                    {
                        callHelp?.Invoke();
                        return;
                    }

                    if (uc.Result == null)
                        return;

                    // have a result
                    var record = uc.Result;

                    // be a little bit specific
                    var dlgTitle = "Select text file to be exported";
                    var dlgFileName = "";
                    var dlgFilter = "";

                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.TSF)
                    {
                        dlgFileName = "new.txt";
                        dlgFilter =
                            "Tab separated file (*.txt)|*.txt|Tab separated file (*.tsf)|*.tsf|All files (*.*)|*.*";
                    }
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.LaTex)
                    {
                        dlgFileName = "new.tex";
                        dlgFilter = "LaTex file (*.tex)|*.tex|All files (*.*)|*.*";
                    }
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.Excel)
                    {
                        dlgFileName = "new.xlsx";
                        dlgFilter = "Microsoft Excel (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                    }
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.Word)
                    {
                        dlgFileName = "new.docx";
                        dlgFilter = "Microsoft Word (*.docx)|*.docx|All files (*.*)|*.*";
                    }
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.NarkdownGH)
                    {
                        dlgFileName = "new.md";
                        dlgFilter = "Markdown (*.md)|*.md|All files (*.*)|*.*";
                    }

                    // store
                    ticket["Record"] = record;

                    // ask now for a filename
                    if (!MenuSelectSaveFilenameToTicket(
                        ticket, "File",
                        dlgTitle,
                        dlgFileName,
                        dlgFilter,
                        "Import/ export table: No valid filename."))
                        return;
                }

                // pass on
                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "Import/export table: passing on.");
                }
            }

            if (cmd == "exportuml")
            {
                bool copyLater = false;
                if (ticket?.ScriptMode != true)
                {
                    // interactive
                    // handle the export dialogue
                    var uc = new ExportUmlFlyout();
                    uc.Result = _logic?.GetImportExportTablePreset().Item2 ?? new ExportUmlRecord();

                    StartFlyoverModal(uc);

                    if (uc.Result == null)
                        return;

                    // have a result
                    var result = uc.Result;
                    copyLater = result.CopyToPasteBuffer;

                    // store
                    ticket["Record"] = result;

                    // ask now for a filename
                    if (!MenuSelectSaveFilenameToTicket(
                        ticket, "File",
                        "Select file for UML export ..",
                        "new.uml",
                        "PlantUML text file (*.uml)|*.uml|All files (*.*)|*.*",
                        "Import/ export UML: No valid filename."))
                        return;
                }

                // pass on
                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "Import/export table: passing on.");
                }

                // copy?
                if (copyLater)
                    try
                    {
                        var lines = System.IO.File.ReadAllText(ticket["File"] as string);
                        Clipboard.SetData(DataFormats.Text, lines);
                        Log.Singleton.Info("Export UML data copied to paste buffer.");
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }
            }

            if (cmd == "importtimeseries")
            {
                if (ticket?.ScriptMode != true)
                {
                    // interactive
                    // handle the export dialogue
                    var uc = new ImportTimeSeriesFlyout();
                    uc.Result = _logic?.GetImportExportTablePreset().Item3 ?? new ImportTimeSeriesRecord();

                    StartFlyoverModal(uc);

                    if (uc.Result == null)
                        return;

                    // have a result
                    var result = uc.Result;

                    // store
                    ticket["Record"] = result;

                    // be a little bit specific
                    var dlgTitle = "Select file for time series import ..";
                    var dlgFilter = "All files (*.*)|*.*";

                    if (result.Format == (int)ImportTimeSeriesRecord.FormatEnum.Excel)
                    {
                        dlgFilter =
                            "Tab separated file (*.txt)|*.txt|Tab separated file (*.tsf)|*.tsf|All files (*.*)|*.*";
                    }

                    // ask now for a filename
                    if (!MenuSelectOpenFilenameToTicket(
                        ticket, "File",
                        dlgTitle,
                        null,
                        dlgFilter,
                        "Import time series: No valid filename."))
                        return;
                }

                // pass on
                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "Import time series: passing on.");
                }
            }

            // redraw
            CommandExecution_RedrawAll();
        }

        public void CommandBinding_SubmodelTdExportImport(
            string cmd,
            AasxMenuActionTicket ticket = null)
        {
            // rely on ticket availability
            if (ticket == null)
                return;

            if (cmd == "submodeltdimport")
            {
                // filename
                if (!MenuSelectOpenFilenameToTicket(
                    ticket, "File",
                    "Select Thing Description (TD) file to be imported",
                    null,
                    "JSON files (*.JSONLD)|*.jsonld",
                    "TD import: No valid filename."))
                    return;

                // do it
                try
                {
                    // delegate futher
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);

                    // redisplay
                    RedrawAllAasxElements();
                    RedrawElementView();
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex,
                        "When importing JSON LD for Thing Description, an error occurred");
                }
            }

            if (cmd == "submodeltdexport")
            {
                // filename
                if (!MenuSelectSaveFilenameToTicket(
                    ticket, "File",
                    "Thing Description (TD) export",
                    "Submodel_" + ticket.Submodel?.IdShort + ".jsonld",
                    "JSON files (*.JSONLD)|*.jsonld",
                    "Thing Description (TD) export: No valid filename."))
                    return;

                // do it
                try
                {
                    // delegate futher
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex,
                        "When exporting Thing Description (TD), an error occurred");
                }
            }
        }

        public void CommandBinding_NewSubmodelFromPlugin(
            string cmd,
            AasxMenuActionTicket ticket)
        {
            // rely on ticket availability
            if (ticket == null)
                return;

            // create a list of plugins, which are capable of generating Submodels
            var listOfSm = new List<AnyUiDialogueListItem>();
            var list = _logic?.GetPotentialGeneratedSubmodels();
            if (list != null)
                foreach (var rec in list)
                    listOfSm.Add(new AnyUiDialogueListItem(
                        "" + rec.Item1.name + " | " + "" + rec.Item2, rec));

            // could be nothing
            if (listOfSm.Count < 1)
            {
                _logic?.LogErrorToTicket(ticket, "New Submodel from plugin: No Submodels available " +
                    "to be generated by plugins.");
                return;
            }

            // prompt if no name is given
            if (ticket["Name"] == null)
            {
                var uc = new SelectFromListFlyout();
                uc.DiaData.Caption = "Select Plug-in and Submodel to be generated ..";
                uc.DiaData.ListOfItems = listOfSm;
                this.StartFlyoverModal(uc);
                if (uc.DiaData.ResultItem == null)
                    return;
                ticket["Record"] = uc.DiaData.ResultItem.Tag;
            }

            // do it
            try
            {
                // delegate futher
                _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
            }
            catch (Exception ex)
            {
                _logic?.LogErrorToTicket(ticket, ex,
                    "When generating Submodel from plugins, an error occurred");
            }

            // redisplay
            // add to "normal" event quoue
            DispEditEntityPanel.AddWishForOutsideAction(new AnyUiLambdaActionRedrawAllElements(ticket["SmRef"]));
        }

        public async Task CommandBinding_ToolsFind(
            string cmd,
            AasxMenuActionTicket ticket)
        {
            // access
            if (ToolsGrid == null || TabControlTools == null || TabItemToolsFind == null || ToolFindReplace == null)
                return;

            if (cmd == "toolsfindtext" || cmd == "toolsreplacetext")
            {
                // start
                ticket.StartExec();

                // make panel visible
                ToolsGrid.Visibility = Visibility.Visible;
                TabControlTools.SelectedItem = TabItemToolsFind;

                // set the link to the AAS environment
                // Note: dangerous, as it might change WHILE the find tool is opened!
                ToolFindReplace.TheAasEnv = _packageCentral.Main?.AasEnv;

                // cursor
                ToolFindReplace.FocusFirstField();

                // if in script mode, directly start
                if (ticket.ScriptMode)
                {
                    if (cmd == "toolsfindtext" || cmd == "toolsreplacetext")
                        ToolFindReplace.FindStart(ticket);

                    var dos = (ticket["Do"] as string).Trim().ToLower();
                    if (cmd == "toolsreplacetext" && dos == "stay")
                        ToolFindReplace.ReplaceStay(ticket);

                    if (cmd == "toolsreplacetext" && dos == "forward")
                        ToolFindReplace.ReplaceForward(ticket);

                    if (cmd == "toolsreplacetext" && dos == "all")
                        ToolFindReplace.ReplaceAll(ticket);

                    // update on screen
                    await MainTimer_HandleEntityPanel();
                    ticket.SleepForVisual = 2;
                }
            }

            if (cmd == "toolsfindforward" || cmd == "toolsfindbackward"
                || cmd == "toolsreplacestay"
                || cmd == "toolsreplaceforward"
                || cmd == "toolsreplaceall")
            {
                // start
                ticket.StartExec();

                if (cmd == "toolsfindforward")
                    ToolFindReplace.FindForward(ticket);

                if (cmd == "toolsfindbackward")
                    ToolFindReplace.FindBackward(ticket);

                if (cmd == "toolsreplacestay")
                    ToolFindReplace.ReplaceStay(ticket);

                if (cmd == "toolsreplaceforward")
                    ToolFindReplace.ReplaceForward(ticket);

                if (cmd == "toolsreplaceall")
                    ToolFindReplace.ReplaceAll(ticket);

                // complete the selection
                if (ticket.ScriptMode)
                {
                    await MainTimer_HandleEntityPanel();
                    ticket.SleepForVisual = 2;
                }
            }
        }

        public void CommandBinding_ExportOPCUANodeSet(
            string cmd,
            AasxMenuActionTicket ticket)
        {
            // rely on ticket availability
            if (ticket == null)
                return;

            if (cmd == "opcuai4aasexport")
            {
                // start
                ticket.StartExec();

                // try to access I4AAS export information
                try
                {
                    var xstream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                        "AasxPackageExplorer.Resources.i4AASCS.xml");
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "when accessing i4AASCS.xml mapping types.");
                    return;
                }
                Log.Singleton.Info("Mapping types loaded.");

                // filename
                if (!MenuSelectSaveFilenameToTicket(
                    ticket, "File",
                    "Select Nodeset file to be exported",
                    "new.xml",
                    "XML File (.xml)|*.xml|Text documents (.txt)|*.txt",
                    "Export i4AAS based OPC UA nodeset: No valid filename."))
                    return;

                // ReSharper enable PossibleNullReferenceException
                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "when exporting i4AAS based OPC UA mapping.");
                }
            }

            if (cmd == "opcuai4aasimport")
            {
                // filename
                if (!MenuSelectOpenFilenameToTicket(
                ticket, "File",
                    "Select Nodeset file to be imported",
                    "Document",
                    "XML File (.xml)|*.xml|Text documents (.txt)|*.txt",
                    "Import i4AAS based OPC UA nodeset: No valid filename."))
                    return;

                // do
                try
                {
                    _logic?.CommandBinding_GeneralDispatch(cmd, ticket);

                    // TODO (MIHO, 2022-11-17): not very elegant
                    if (ticket.PostResults != null && ticket.PostResults.ContainsKey("TakeOver")
                        && ticket.PostResults["TakeOver"] is AdminShellPackageEnv pe)
                        _packageCentral.MainItem.TakeOver(pe);

                    RestartUIafterNewPackage();
                }
                catch (Exception ex)
                {
                    _logic?.LogErrorToTicket(ticket, ex, "when importing i4AAS based OPC UA mapping.");
                }
            }
        }

        public void CommandBinding_ExportSMD(
            AasxMenuActionTicket ticket)
        {
#if TODO
            // Note: the plugin is currently WPF based!
            // rely on ticket availability
            if (ticket == null)
                return;

            // start
            ticket.StartExec();

            // trivial things
            if (!_packageCentral.MainStorable)
            {
                _logic?.LogErrorToTicket(ticket, "An AASX package needs to be open!");
                return;
            }

            // check, if required plugin can be found
            var pluginName = "AasxPluginSmdExporter";
            var actionName = "generate-SMD";
            var pi = Plugins.FindPluginInstance(pluginName);
            if (pi == null || !pi.HasAction(actionName))
            {
                _logic?.LogErrorToTicket(ticket,
                    $"This function requires a binary plug-in file named '{pluginName}', " +
                    $"which needs to be added to the command line, with an action named '{actionName}'.");
                return;
            }
            //-----------------------------------
            // make a logger
            var logger = new AasxRestServerLibrary.GrapevineLoggerToListOfStrings();

            AasxRestServerLibrary.AasxRestServer.Start(_packageCentral.Main,
                                                        Options.Curr.RestServerHost,
                                                        Options.Curr.RestServerPort,
                                                        logger);

            Queue<string> stack = new Queue<string>();

            // Invoke Plugin
            var ret = pi.InvokeAction(actionName,
                                      this,
                                      stack,
                                      $"http://{Options.Curr.RestServerHost}:{Options.Curr.RestServerPort}/",
                                      "",
                                      ticket);

            if (ret == null) return;

            // make listing flyout
            var uc = new LogMessageFlyout("SMD Exporter", "Generating SMD ..", () =>
            {
                string st;
                if (stack.Count != 0)
                    st = stack.Dequeue();
                else
                    st = null;
                return (st == null) ? null : new StoredPrint(st);
            });

            this.StartFlyoverModal(uc, closingAction: () =>
            {
                AasxRestServerLibrary.AasxRestServer.Stop();
            });
            //--------------------------------
            // Redraw for changes to be visible
            RedrawAllAasxElements();
            //-----------------------------------
#endif
        }

    }
}
