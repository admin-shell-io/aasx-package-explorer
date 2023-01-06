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
    /// This partial class contains the dynamic definition of the main menu (only).
    /// </summary>
    public partial class MainWindow : Window, IFlyoutProvider, IAasxScriptRemoteInterface
    {
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

            menu.DefaultActionAsync = CommandBinding_GeneralDispatch;

            return menu;
        }
    }
}
