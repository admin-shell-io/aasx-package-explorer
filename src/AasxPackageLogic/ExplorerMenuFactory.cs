/*
Copyright (c) 2019 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPackageLogic;
using Extensions;

namespace AasxPackageExplorer
{
    /// <summary>
    /// This partial class contains the dynamic definition of the main menu (only).
    /// </summary>
    public static class ExplorerMenuFactory
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
        public static AasxMenu CreateMainMenu()
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
                .AddWpfBlazor(name: "New", header: "_New …", inputGesture: "Ctrl+N",
                    help: "Create new AASX package.")
                .AddWpfBlazor(name: "Open", header: "_Open …", inputGesture: "Ctrl+O",
                    help: "Open existing AASX package.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("File", "Source filename including a path and extension."))
                .AddWpfBlazor(name: "ConnectIntegrated", header: "Connect …", inputGesture: "Ctrl+Shift+I")
                .AddWpfBlazor(name: "Save", header: "_Save", inputGesture: "Ctrl+S")
                .AddWpfBlazor(name: "SaveAs", header: "_Save as …")
                .AddWpfBlazor(name: "Close", header: "_Close …")
                .AddWpfBlazor(name: "CheckAndFix", header: "Check, validate and fix …")
                .AddMenu(header: "Security …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "Sign", header: "_Sign (Submodel, Package) …",
                        help: "Sign a Submodel or SubmodelElement.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("UseX509", "Use X509 (true) or Verifiable Credential (false)")
                            .Add("Source", "Source package (.aasx) file.")
                            .Add("Certificate", "Certificate (.cer) file.")
                            .Add("Target", "Target package (.aasx2) file."))
                    .AddWpfBlazor(name: "ValidateCertificate", header: "_Validate (Submodel, Package) …",
                        help: "Validate a already signed Submodel or SubmodelElement.")
                    .AddWpfBlazor(name: "Encrypt", header: "_Encrypt (Package) …",
                        help: "Encrypts a Submodel, SubmodelElement or Package. For the latter, the arguments " +
                              "are required.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Source", "Source package (.aasx) file.")
                            .Add("Certificate", "Certificate (.cer) file.")
                            .Add("Target", "Target package (.aasx2) file."))
                    .AddWpfBlazor(name: "Decrypt", header: "_Decrypt (Package) …",
                        help: "Decrypts a Package.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Source", "Source package (.aasx2) file.")
                            .Add("Certificate", "Certificate (.pfx) file.")
                            .Add("Target", "Target package (.aasx) file.")))
                .AddMenu(header: "Reports …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "AssessSmt", header: "_Assess Submodel template …",
                        help: "Checks for a set of defined features for a Submodel template " +
                            "and reports the results. ",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Target", "Target report file (*.txt, *.xlsx)."))
                    .AddWpfBlazor(name: "CompareSmt", header: "_Compare Submodel template in main and auxiliary …",
                        help: "Compares Submodel templates given in main and auxiliary packages.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Target", "Target report file (*.txt, *.xlsx).")))
                .AddSeparator()
                .AddWpfBlazor(name: "OpenAux", header: "Open Au_xiliary AAS …", inputGesture: "Ctrl+X",
                    help: "Open existing AASX package to the auxiliary buffer (non visible in the tree).",
                    args: new AasxMenuListOfArgDefs()
                        .Add("File", "Source filename including a path and extension."))
                .AddWpfBlazor(name: "CloseAux", header: "Close Auxiliary AAS")
                .AddSeparator()
                .AddMenu(header: "Further connect options …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "ConnectSecure", header: "Secure Connect …", inputGesture: "Ctrl+Shift+S")
                    .AddWpfBlazor(name: "ConnectOpcUa", header: "Connect via OPC-UA …")
                    .AddWpfBlazor(name: "ConnectRest", header: "Connect via REST …", inputGesture: "F6"))
                .AddSeparator()
                .AddMenu(header: "AASX File Repository …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "FileRepoNew", header: "New (local) repository …",
                        help: "Create new (empty) file repository.")
                    .AddWpfBlazor(name: "FileRepoOpen", header: "Open (local) repository …",
                        help: "Opens an existing AASX file repository and adds it to the list of open repos.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "Path and filename of existing AASX file repository."))
                    .AddWpfBlazor(name: "FileRepoConnectRepository", header: "Connect HTTP/REST repository …",
                        help: "Connects to an online repository via HTTP/REST.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Endpoint", "Endpoint of repo (without \"/server/listaas\")."))
                    .AddWpfBlazor(name: "FileRepoConnectRegistry", header: "Query HTTP/REST registry …")
                    .AddSeparator()
                    .AddWpfBlazor(name: "FileRepoCreateLRU", header: "Create last recently used list …")
                    .AddSeparator()
                    .AddWpfBlazor(name: "FileRepoQuery", header: "Query open repositories …", inputGesture: "F12",
                        help: "Selects and repository item (AASX) from the open AASX file repositories.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Index", "Zero-based integer index to the list of all open repos.")
                            .Add("AAS", "String with AAS-Id")
                            .Add("Asset", "String with Asset-Id.")))
                .AddSeparator()
                .AddMenu(header: "Import …", attachPoint: "import", childs: (new AasxMenu())
					.AddWpfBlazor(name: "ImportAASX", header: "Import further AASX file into AASX …",
						help: "Import AASX file(s) with entities to overall AAS environment.",
						args: new AasxMenuListOfArgDefs()
							.Add("Files", "One or multiple AASX file(s) with AAS entities data."))
					.AddWpfBlazor(name: "ImportAML", header: "Import AutomationML into AASX …",
                        help: "Import AML file with AAS entities to overall AAS environment.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "AML file with AAS entities data."))
                    .AddWpfBlazor(name: "SubmodelRead", header: "Import Submodel from JSON …",
                        help: "Read Submodel from JSON and add/ replace existing to current AAS.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON file with Submodel data."))
                    .AddWpfBlazor(name: "SubmodelGet", header: "GET Submodel from URL …",
                        help: "Get Submodel from REST server.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("URL", "URL to get Submodel data from."))
                    .AddWpfBlazor(name: "ImportDictSubmodel", header: "Import Submodel from Dictionary …",
                        help: "UI assisted import from dictionaries such as ECLASS and IEC CDD to a Submodel.")
                    .AddWpfBlazor(name: "ImportDictSubmodelElements", header: "Import Submodel Elements from Dictionary …",
                        help: "UI assisted import from dictionaries such as ECLASS and IEC CDD to SubmodelElement.")
                    .AddWpfBlazor(name: "BMEcatImport", header: "Import BMEcat-file into SubModel …",
                        help: "Import BMEcat data into an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "BMEcat file with data."))
                    .AddWpfBlazor(name: "SubmodelTDImport", header: "Import Thing Description JSON LD document into SubModel …",
                        help: "Import Thing Description (TD) file in JSON LD format into an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON LD file with TD data."))
					.AddWpfBlazor(name: "SammAspectImport", header: "Import SAMM aspect into ConceptDescriptions …",
						help: "Import SAMM (Semantic Aspect Meta Model) aspect data into dedicated ConceptDescriptions.",
						args: new AasxMenuListOfArgDefs()
							.Add("File", "SAMM file (*.ttl, ..) with aspect model."))
					.AddWpfBlazor(name: "CSVImport", header: "Import CSV-file into SubModel …",
                        help: "Import comma separated values (CSV) into an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "CSV file with data."))
                    .AddWpfBlazor(name: "OPCUAi4aasImport", header: "Import AAS from i4aas-nodeset …")
                    .AddWpfBlazor(name: "OpcUaImportNodeSet", header: "Import OPC UA nodeset.xml as Submodel …",
                        help: "Import OPC UA nodeset.xml into an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "OPC UA Nodeset file."))
                    .AddWpfBlazor(name: "OPCRead", header: "Read OPC values into SubModel …",
                        help: "Use Qualifiers attributed in a Submodel to read actual OPC UA values.")
                    .AddWpfBlazor(name: "RDFRead", header: "Import BAMM RDF into AASX …",
                        help: "Import BAMM RDF into AASX.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "BAMM file with RDF data.")))
                .AddMenu(header: "Export …", attachPoint: "Export", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "ExportAML", header: "Export AutomationML …",
                        help: "Export AML file with AAS entities from AAS environment.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "AML file with AAS entities data.")
                            .Add("Location", "Location selection", hidden: true)
                            .Add("FilterIndex", "Set FilterIndex=2 for compact AML format."))
                    .AddWpfBlazor(name: "SubmodelWrite", header: "Export Submodel to JSON …",
                        help: "Write Submodel to JSON.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON file to write Submodel data to.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "SubmodelPut", header: "PUT Submodel to URL …",
                        help: "Put Submodel to REST server.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("URL", "URL to put Submodel data to."))
                    .AddWpfBlazor(name: "ExportCst", header: "Export to TeamCenter CST …",
                        help: "Export data to SIEMENS TeamCenter containing list of properties.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "Head-part of filenames to write data to.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "ExportJsonSchema", header: "Export JSON schema for Submodel Templates …",
                        help: "Export data in JSON schema format to describe AAS Submodel Templates.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON schema file to write data to.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "OPCUAi4aasExport", header: "Export AAS as i4aas-nodeset …",
                        help: "Export OPC UA Nodeset2.xml format as i4aas-nodeset.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "OPC UA Nodeset2.xml file to write.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "OpcUaExportNodeSetUaPlugin",
                        header: "Export OPC UA Nodeset2.xml (via UA server plug-in) …",
                        help: "Export OPC UA Nodeset2.xml format by starting OPC UA server in plugin and " +
                            "execute a post-process command.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "OPC UA Nodeset2.xml file to write.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "CopyClipboardElementJson",
                        header: "Copy selected element JSON to clipboard", inputGesture: "Shift+Ctrl+C")
                    .AddWpfBlazor(name: "ExportGenericForms",
                        header: "Export Submodel as options for GenericForms …",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "OPC UA Nodeset2.xml file to write.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "ExportPredefineConcepts",
                        header: "Export Submodel as snippet for PredefinedConcepts …",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "OPC UA Nodeset2.xml file to write.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "SubmodelTDExport", header: "Export Submodel as Thing Description JSON LD document",
                        help: "Export Thing Description (TD) file in JSON LD format from an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON LD file with TD data.")
                            .Add("Location", "Location selection", hidden: true))
					.AddWpfBlazor(name: "SammAspectExport", header: "Export SAMM aspect model by selected CD",
						help: "Export SAMM aspect model in Turtle (.ttl) format from an selected ConceptDescription.",
						args: new AasxMenuListOfArgDefs()
							.Add("File", "Turtle file with SAMM data.")
							.Add("Location", "Location selection", hidden: true))
					.AddWpfBlazor(name: "PrintAsset", header: "Print Asset as code sheet …",
                        help: "Prints a sheet with 2D codes for the selected asset.")
                    .AddWpfBlazor(name: "ExportSMD", header: "Export TeDZ Simulation Model Description (SMD) …",
                        help: "Export TeDZ Simulation Model Description (SMD).",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Machine", "Designation of the machine/ equipment.")
                            .Add("Model", "Model type, either 'Physical' or 'Signal'.")))
                .AddSeparator(filter: AasxMenuFilter.NotBlazor)
                .AddMenu(header: "Server …", filter: AasxMenuFilter.NotBlazor,
                         attachPoint: "Server", childs: (new AasxMenu())
                    .AddWpf(name: "ServerRest", header: "Serve AAS as REST …", inputGesture: "Shift+F6")
                    .AddWpf(name: "MQTTPub", header: "Publish AAS via MQTT …")
                    .AddSeparator()
                    .AddWpf(name: "ServerPluginEmptySample", header: "Plugin: Empty Sample …")
                    .AddWpf(name: "ServerPluginOPCUA", header: "Plugin: OPC UA …")
                    .AddWpf(name: "ServerPluginMQTT", header: "Plugin: MQTT …"))
                .AddSeparator(filter: AasxMenuFilter.NotBlazor)
				.AddMenu(header: "System …", filter: AasxMenuFilter.NotBlazor,
						 attachPoint: "System", childs: (new AasxMenu())
					.AddWpf(name: "AttachFileAssoc", header: "Attach .aasx file associations",
					    args: new AasxMenuListOfArgDefs()
							.Add("File", "Windows RegEdit file to write.")
							.Add("Location", "Location selection", hidden: true))
					.AddWpf(name: "RemoveFileAssoc", header: "Remove .aasx file associations",
						args: new AasxMenuListOfArgDefs()
							.Add("File", "Windows RegEdit file to write.")
							.Add("Location", "Location selection", hidden: true)))
				.AddWpfBlazor(name: "Exit", header: "_Exit", inputGesture: "Alt+F4"));

            //
            // Workspace
            //

            menu.AddMenu(header: "Workspace",
                childs: (new AasxMenu())
                .AddWpfBlazor(name: "EditMenu", header: "_Edit", inputGesture: "Ctrl+E",
                    onlyDisplay: true, isCheckable: true,
                    args: new AasxMenuListOfArgDefs()
                            .Add("Mode", "'True' to activate edit mode, 'False' to turn off."))
                .AddWpfBlazor(name: "HintsMenu", header: "_Hints", inputGesture: "Ctrl+H",
                    onlyDisplay: true, isCheckable: true, isChecked: true,
                    args: new AasxMenuListOfArgDefs()
                        .Add("Mode", "'True' to activate hints mode, 'False' to turn off."))
                .AddWpfBlazor(name: "Test", header: "Test")
                .AddSeparator(filter: AasxMenuFilter.Wpf)
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
                .AddMenu(header: "Navigation …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "NavigateBack", header: "Back", inputGesture: "Ctrl+Shift+Left")
                    .AddWpfBlazor(name: "NavigateHome", header: "Home", inputGesture: "Ctrl+Shift+Home"))
                .AddSeparator()
                .AddMenu(header: "Editing locations …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "LocationPush", header: "Push location", inputGesture: "Ctrl+Shift+P")
                    .AddWpfBlazor(name: "LocationPop", header: "Pop location", inputGesture: "Ctrl+Shift+O"))
                .AddSeparator()
                .AddMenu(header: "Create …", attachPoint: "Plugins", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "NewSubmodelFromPlugin", header: "New Submodel from plugin", inputGesture: "Ctrl+Shift+M",
                            help: "Creates a new Submodel based on defintions provided by plugin.",
                            args: new AasxMenuListOfArgDefs()
                                .Add("Name", "Name of the Submodel (partially)")
                                .Add("Record", "Record data", hidden: true)
                                .Add("SmRef", "Return: Submodel generated", hidden: true))
                    .AddWpfBlazor(name: "NewSubmodelFromKnown", header: "New Submodel from pool of known",
                            help: "Creates a new Submodel based on defintions provided by a pool of known definitions.",
                            args: new AasxMenuListOfArgDefs()
                                .Add("Domain", "Domain of knowledge/ name of the Submodel (partially)")
                                .Add("SmRef", "Return: Submodel generated", hidden: true))
					.AddWpfBlazor(name: "MissingCdsFromKnown", header: "Missing ConceptDescriptions from pool of known",
							help: "For the selected element: checks which SME refer to missing " +
                                  "ConceptDescriptions, which can be created from pool of known definitions.")
					.AddWpfBlazor(name: "SubmodelInstanceFromSammAspect", 
                        header: "New Submodel instance from selected SAMM aspect",
						help: "Creates a new Submodel instance from an selected ConceptDescription with a SAMM Aspect element.",
						args: null))
				.AddMenu(header: "Visualize …", attachPoint: "Visualize")
                .AddSeparator()
                .AddWpfBlazor(name: "ConvertElement", header: "Convert …",
                        help: "Asks plugins if these could make offers to convert the current elements and " +
                            "subsequently converts the element.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Name", "Name of the potential offer (partially)")
                            .Add("Record", "Record data", hidden: true))
                .AddSeparator()
                .AddMenu(header: "Buffer …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "BufferClear", header: "Clear internal paste buffer"))
                .AddMenu(header: "Log …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "StatusClear", header: "Clear status line and errors")
                    .AddWpfBlazor(name: "LogShow", header: "Show log"))
                .AddSeparator(filter: AasxMenuFilter.NotBlazor)
                .AddMenu(header: "Events …", childs: (new AasxMenu())
                    .AddWpf(name: "EventsShowLogMenu", header: "_Event log", inputGesture: "Ctrl+L",
                        onlyDisplay: true, isCheckable: true)
                    .AddWpf(name: "EventsResetLocks", header: "Reset interlocking"))
                .AddMenu(header: "Scripts …", filter: AasxMenuFilter.WpfBlazor, childs: (new AasxMenu())
                    .AddWpfBlazor(name: "ScriptEditLaunch", header: "Edit & launch …", inputGesture: "Ctrl+Shift+L")));

            //
            // Options
            //

            menu.AddMenu(header: "Option",
                childs: (new AasxMenu())
                .AddWpfBlazor(name: "ShowIriMenu", header: "Show id as IRI", inputGesture: "Ctrl+I", isCheckable: true)
                .AddWpfBlazor(name: "VerboseConnect", header: "Verbose connect", isCheckable: true)
                .AddWpfBlazor(name: "FileRepoLoadWoPrompt", header: "Load without prompt", isCheckable: true)
                .AddWpfBlazor(name: "AnimateElements", header: "Animate elements", isCheckable: true)
                .AddWpfBlazor(name: "ObserveEvents", header: "ObserveEvents", isCheckable: true)
                .AddWpfBlazor(name: "CompressEvents", header: "Compress events", isCheckable: true)
				.AddWpfBlazor(name: "CheckSmtElements", header: "Check SMT elements (slow!)", isCheckable: true));

            //
            // Help
            //

            menu.AddMenu(header: "Help",
                childs: (new AasxMenu())
                .AddWpfBlazor(name: "About", header: "About …")
                .AddWpfBlazor(name: "HelpGithub", header: "Help on Github …")
                .AddWpfBlazor(name: "FaqGithub", header: "FAQ on Github …")
                .AddWpfBlazor(name: "HelpIssues", header: "Issues on Github …")
                .AddWpfBlazor(name: "HelpOptionsInfo", header: "Available options …"));

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
            // Try attach plugins
            //

            foreach (var mi in menu.FindAll<AasxMenuItem>((test) => test.AttachPoint?.HasContent() == true))
            {
                // this is worth a search in the plugins
                foreach (var pi in Plugins.LoadedPlugins.Values)
                {
                    // menu items?
                    if (pi.MenuItems == null)
                        continue;

                    // search here
                    foreach (var pmi in pi.MenuItems)
                        if (pmi.AttachPoint.Equals(mi.AttachPoint,
                            System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            // double the data
                            var newMi = pmi.MenuItem.Copy();

                            // say, that it goes to a plugin
                            newMi.PluginToAction = pi.name;

                            // yes! can attach!
                            mi.Add(newMi);
                        }
                }
            }

            //
            // End
            //

            return menu;
        }
    }
}
