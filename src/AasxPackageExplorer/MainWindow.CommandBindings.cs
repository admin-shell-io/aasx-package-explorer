/*
Copyright (c) 2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxMqttClient;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Aas = AasCore.Aas3_0;

namespace AasxPackageExplorer
{
    /// <summary>
    /// This partial class contains all command bindings, such as for the main menu, in order to reduce the
    /// complexity of MainWindow.xaml.cs
    /// </summary>
    public partial class MainWindow : Window, IFlyoutProvider
    {
        private string lastFnForInitialDirectory = null;

        //// Note for UltraEdit:
        //// <MenuItem Header="([^"]+)"\s*(|InputGestureText="([^"]+)")\s*Command="{StaticResource (\w+)}"/>
        //// .AddWpf\(name: "\4", header: "\1", inputGesture: "\3"\)
        //// or
        //// <MenuItem Header="([^"]+)"\s+([^I]|InputGestureText="([^"]+)")(.*?)Command="{StaticResource (\w+)}"/>
        //// .AddWpf\(name: "\5", header: "\1", inputGesture: "\3", \4\)


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

        /// <summary>
        /// Redraw tree elements (middle), AAS entitty (right side)
        /// </summary>
        public void CommandExecution_RedrawAll()
        {
            // redraw everything
            RedrawAllAasxElements();
            RedrawElementView();
        }

        /// <summary>
        /// Set to <c>true</c>, if the application shall be shut down via script
        /// </summary>
        public bool ScriptModeShutdown = false;

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

            Logic?.FillSelectedItem(
                DisplayElements.SelectedItem, DisplayElements.SelectedItems, ticket);

            //
            // Dispatch
            //

            // REFACTOR: DIFFERENT
            if (cmd == "exit")
            {
                // start
                ticket.StartExec();

                // do
                ScriptModeShutdown = true;
                System.Windows.Application.Current.Shutdown();
            }

            if (cmd == "connectopcua")
                MessageBoxFlyoutShow(
                    "In future versions, this feature will allow connecting to an online Administration Shell " +
                    "via OPC UA or similar.",
                    "Connect", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Hand);

            // REFACTOR: DIFFERENT
            if (cmd == "about")
            {
                // start
                ticket.StartExec();

                // do
                var ab = new AboutBox(_pref);
                ab.ShowDialog();
            }

            // REFACTOR: DIFFERENT
            if (cmd == "helpgithub")
            {
                // start
                ticket.StartExec();

                // do
                ShowHelp();
            }

            // REFACTOR: DIFFERENT
            if (cmd == "faqgithub")
            {
                // start
                ticket.StartExec();

                // do
                BrowserDisplayLocalFile(
                    @"https://github.com/admin-shell-io/questions-and-answers/blob/master/README.md");
            }

            // REFACTOR: DIFFERENT
            if (cmd == "helpissues")
            {
                // start
                ticket.StartExec();

                // do
                BrowserDisplayLocalFile(
                    @"https://github.com/admin-shell-io/aasx-package-explorer/issues");
            }

            // REFACTOR: DIFFERENT
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

            //
            // Flag handling .. (no refactor)
            //

            if (cmd == "editkey")
                MainMenu?.SetChecked("EditMenu", MainMenu?.IsChecked("EditMenu") != true);

            if (cmd == "hintskey")
                MainMenu?.SetChecked("HintsMenu", MainMenu?.IsChecked("HintsMenu") != true);

            if (cmd == "showirikey")
                MainMenu?.SetChecked("ShowIriMenu", MainMenu?.IsChecked("ShowIriMenu") != true);

            if (cmd == "editmenu" || cmd == "editkey"
                || cmd == "hintsmenu" || cmd == "hintskey"
                || cmd == "showirimenu" || cmd == "showirikey")
            {
                // start
                ticket.StartExec();

                if (ticket.ScriptMode && cmd == "editmenu" && ticket["Mode"] is bool editMode)
                {
                    MainMenu?.SetChecked("EditMenu", editMode);
                }

                if (ticket.ScriptMode && cmd == "hintsmenu" && ticket["Mode"] is bool hintsMode)
                {
                    MainMenu?.SetChecked("HintsMenu", hintsMode);
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

            // REFACTOR: DIFFERENT
            if (cmd == "test")
            {
                // start
                ticket.StartExec();

                // do
                DisplayElements.Test();
            }

            // REFACTOR: 10% DIFFERENT
            if (cmd == "bufferclear")
            {
                // start
                ticket.StartExec();

                // do
                DispEditEntityPanel.ClearPasteBuffer();
                Log.Singleton.Info("Internal copy/ paste buffer cleared. Pasting of external JSON elements " +
                    "enabled.");
            }

            // REFACTOR: LEAVE HERE
            if (cmd == "exportsmd")
                CommandBinding_ExportSMD(ticket);

            // REFACTOR: LEAVE HERE
            if (cmd == "printasset")
                CommandBinding_PrintAsset(ticket);

            if (cmd == "importdictsubmodel" || cmd == "importdictsubmodelelements")
                CommandBinding_ImportDictToSubmodel(cmd, ticket);

            // TODO (MIHO, 2022-11-19): stays in WPF (tightly integrated, command line shall do own version)
            if (cmd == "opcuaexportnodesetuaplugin")
                await CommandBinding_ExportNodesetUaPlugin(cmd, ticket);

            // stays in WPF
            if (cmd == "serverrest")
                CommandBinding_ServerRest();

            // stays in WPF
            if (cmd == "mqttpub")
                await CommandBinding_MQTTPub(ticket);

            // stays in WPF
            if (cmd == "connectintegrated")
                CommandBinding_ConnectIntegrated();

            // stays in WPF
            if (cmd == "connectsecure")
                CommandBinding_ConnectSecure();

            // stays in WPF, ask OZ
            if (cmd == "connectrest")
                CommandBinding_ConnectRest();
            // dead-csharp off
            // REFACTOR: STAYS HERE
            //if (cmd == "exporttable")
            //    await CommandBinding_ExportImportTableUml(cmd, ticket, import: false);

            // REFACTOR: STAYS HERE
            if (cmd == "importtable")
                await CommandBinding_ExportImportTableUml(cmd, ticket, import: true);

            // REFACTOR: STAYS HERE
            //if (cmd == "exportuml")
            //    await CommandBinding_ExportImportTableUml(cmd, ticket, exportUml: true);

            // REFACTOR: STAYS HERE
            //if (cmd == "importtimeseries")
            //    await CommandBinding_ExportImportTableUml(cmd, ticket, importTimeSeries: true);
            // dead-csharp on
            // REFACTOR: STAYS HERE
            if (cmd == "serverpluginemptysample")
                CommandBinding_ExecutePluginServer(
                    "EmptySample", "server-start", "server-stop", "Empty sample plug-in.");

            // REFACTOR: STAYS HERE
            if (cmd == "serverpluginopcua")
                CommandBinding_ExecutePluginServer(
                    "AasxPluginUaNetServer", "server-start", "server-stop", "Plug-in for OPC UA Server for AASX.");

            // REFACTOR: STAYS HERE
            if (cmd == "serverpluginmqtt")
                CommandBinding_ExecutePluginServer(
                    "AasxPluginMqttServer", "MQTTServer-start", "server-stop", "Plug-in for MQTT Server for AASX.");

            // REFACTOR: STAYS
            if (cmd == "toolsfindtext" || cmd == "toolsfindforward" || cmd == "toolsfindbackward"
                || cmd == "toolsreplacetext" || cmd == "toolsreplacestay" || cmd == "toolsreplaceforward"
                || cmd == "toolsreplaceall") await CommandBinding_ToolsFind(cmd, ticket);

            // REFACTOR: STAYS
            if (cmd == "checkandfix")
                CommandBinding_CheckAndFix();

            // REFACTOR: STAYS
            if (cmd == "eventsresetlocks")
            {
                Log.Singleton.Info($"Event interlocking reset. Status was: " +
                    $"update-value-pending={_eventHandling.UpdateValuePending}");

                _eventHandling.Reset();
            }

            // REFACTOR: STAYS
            if (cmd == "eventsshowlogkey")
                MainMenu?.SetChecked("EventsShowLogMenu", MainMenu?.IsChecked("EventsShowLogMenu") != true);

            // REFACTOR: STAYS
            if (cmd == "eventsshowlogkey" || cmd == "eventsshowlogmenu")
            {
                PanelConcurrentSetVisibleIfRequired(PanelConcurrentCheckIsVisible());
            }

            // REFACTOR: STAYS
            if (cmd == "attachfileassoc" || cmd == "removefileassoc")
                await CommandBinding_RegistryTools(cmd, ticket);

			// pass dispatch on to next (lower) level of menu functions
			await Logic.CommandBinding_GeneralDispatchAnyUiDialogs(cmd, menuItem, ticket);
        }

        public bool PanelConcurrentCheckIsVisible()
        {
            return MainMenu?.IsChecked("EventsShowLogMenu") == true;
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
            var env = PackageCentral.Main?.AasEnv;
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
                PackageCentral.Main.SaveAs("noname.xml", true, AdminShellPackageEnv.SerializationFormat.Xml, ms,
                    saveOnlyCopy: true);
                ms.Flush();
                ms.Position = 0;
                AasSchemaValidation.ValidateXML(recs, ms);
                ms.Close();

                // validate as JSON
                var ms2 = new MemoryStream();
                PackageCentral.Main.SaveAs("noname.json", true, AdminShellPackageEnv.SerializationFormat.Json, ms2,
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
                PackageCentral,
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
                        PackageCentral.MainItem, null, takeOverContainer: uc.ResultContainer, onlyAuxiliary: false);
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

            if (ticket.AAS == null || string.IsNullOrEmpty(ticket.AssetInfo?.GlobalAssetId))
            {
                Logic?.LogErrorToTicket(ticket,
                    "No asset selected or no asset identification for printing code sheet.");
                return;
            }

            // ok!
            // Note: WPF based; no command line possible
            try
            {
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                AasxPrintFunctions.PrintSingleAssetCodeSheet(ticket.AssetInfo.GlobalAssetId, ticket.AAS.IdShort);
                if (Options.Curr.UseFlyovers) this.CloseFlyover();
            }
            catch (Exception ex)
            {
                Logic?.LogErrorToTicket(ticket, ex, "When printing");
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
            public AasxMqttClient.AnyUiDialogueDataMqttPublisher DiaData;
            public AasxMqttClient.GrapevineLoggerToStoredPrints Logger;
            public AasxMqttClient.MqttClient Client;
            public BackgroundWorker Worker;
        }

        public async Task CommandBinding_MQTTPub(AasxMenuActionTicket ticket)
        {

            // make an agent
            var agent = new FlyoutAgentMqttPublisher();

			// ask for preferences
#if __WPF_based
            agent.DiaData = AasxMqttClient.AnyUiDialogueDataMqttPublisher.CreateWithOptions("AASX MQTT publisher ..",
                        jtoken: Options.Curr.MqttPublisherOptions);
            var uc1 = new MqttPublisherFlyout(agent.DiaData);
            this.StartFlyoverModal(uc1);
            if (!uc1.Result)
                return;
#else
			agent.DiaData = AasxMqttClient.AnyUiDialogueDataMqttPublisher.CreateWithOptions("AASX MQTT publisher ..",
						jtoken: Options.Curr.MqttPublisherOptions);
			var uc1 = new AnyUiDialogueDataModalPanel(agent.DiaData.Caption);
            uc1.DisableScrollArea = true;
			uc1.ActivateRenderPanel(agent.DiaData,
				(uci) =>
				{
                    // create panel
					var panel = new AnyUiStackPanel();
					var helper = new AnyUiSmallWidgetToolkit();

                    var data = uci.Data as AnyUiDialogueDataMqttPublisher;
                    if (data == null)
                        return panel;

                    // outer grid
					var g = helper.AddSmallGrid(13, 3, new[] { "#", "5:", "*" },
								padding: new AnyUiThickness(0, 5, 0, 5));

                    int row = 0;

                    // Row : MQTT broker
                    helper.AddSmallLabelTo(g, row, 0, content: "Format:", verticalCenter: true);
					AnyUiUIElement.SetStringFromControl(
						helper.AddSmallTextBoxTo(g, row, 2,
							margin: new AnyUiThickness(0, 2, 2, 2),
							text: "" + data.BrokerUrl,
							verticalCenter: true),
						(str) => { data.BrokerUrl = str; });

					// Row : retain
					AnyUiUIElement.SetBoolFromControl(
						helper.Set(
							helper.AddSmallCheckBoxTo(g, ++row, 2,
								content: "Set retain flag in MQTT messages",
								isChecked: data.MqttRetain,
								verticalContentAlignment: AnyUiVerticalAlignment.Center)),
							(b) => { data.MqttRetain = b; });

                    // VSpace
                    helper.AddVerticalSpaceTo(g, ++row);

					// Row : first time publish
					helper.AddSmallLabelTo(g, ++row, 0, content: "First time publish:", verticalCenter: true);
					AnyUiUIElement.SetBoolFromControl(
						helper.AddSmallCheckBoxTo(g, row, 2,
							content: "Enable publishing",
							isChecked: data.EnableFirstPublish,
							verticalContentAlignment: AnyUiVerticalAlignment.Center),
						(b) => { data.EnableFirstPublish = b; });

                    // Row : Topic AAS
                    helper.Set(
                        helper.AddSmallLabelTo(g, ++row, 0, content: "Topic AAS:", verticalCenter: true),
                        horizontalAlignment: AnyUiHorizontalAlignment.Right,
                        horizontalContentAlignment: AnyUiHorizontalAlignment.Right);
					AnyUiUIElement.SetStringFromControl(
						helper.AddSmallTextBoxTo(g, row, 2,
							margin: new AnyUiThickness(0, 2, 2, 2),
							text: "" + data.FirstTopicAAS,
							verticalCenter: true),
						(str) => { data.FirstTopicAAS = str; });

					// Row : Topic Submodel
					helper.Set(
						helper.AddSmallLabelTo(g, ++row, 0, content: "Topic Submodel:", verticalCenter: true),
						horizontalAlignment: AnyUiHorizontalAlignment.Right,
						horizontalContentAlignment: AnyUiHorizontalAlignment.Right);
					AnyUiUIElement.SetStringFromControl(
						helper.AddSmallTextBoxTo(g, row, 2,
							margin: new AnyUiThickness(0, 2, 2, 2),
							text: "" + data.FirstTopicSubmodel,
							verticalCenter: true),
						(str) => { data.FirstTopicSubmodel = str; });

					// VSpace
					helper.AddVerticalSpaceTo(g, ++row);

					// Row : continous event time publish
					helper.AddSmallLabelTo(g, ++row, 0, content: "Continous event publish:", verticalCenter: true);
					AnyUiUIElement.SetBoolFromControl(
						helper.AddSmallCheckBoxTo(g, row, 2,
							content: "Enable publishing",
							isChecked: data.EnableEventPublish,
							verticalContentAlignment: AnyUiVerticalAlignment.Center),
						(b) => { data.EnableEventPublish = b; });

					// Row : Topic event publish
					helper.Set(
						helper.AddSmallLabelTo(g, ++row, 0, content: "Topic:", verticalCenter: true),
						horizontalAlignment: AnyUiHorizontalAlignment.Right,
						horizontalContentAlignment: AnyUiHorizontalAlignment.Right);
					AnyUiUIElement.SetStringFromControl(
						helper.AddSmallTextBoxTo(g, row, 2,
							margin: new AnyUiThickness(0, 2, 2, 2),
							text: "" + data.EventTopic,
							verticalCenter: true),
						(str) => { data.EventTopic = str; });

					// VSpace
					helper.AddVerticalSpaceTo(g, ++row);

					// Row : single value publish
					helper.AddSmallLabelTo(g, ++row, 0, content: "Single value publish:", verticalCenter: true);
					AnyUiUIElement.SetBoolFromControl(
						helper.AddSmallCheckBoxTo(g, row, 2,
							content: "Enable publishing",
							isChecked: data.SingleValuePublish,
							verticalContentAlignment: AnyUiVerticalAlignment.Center),
						(b) => { data.SingleValuePublish = b; });
					
                    // Row : single value first time
                    AnyUiUIElement.SetBoolFromControl(
						helper.AddSmallCheckBoxTo(g, ++row, 2,
							content: "First time",
							isChecked: data.SingleValueFirstTime,
							verticalContentAlignment: AnyUiVerticalAlignment.Center),
						(b) => { data.SingleValueFirstTime = b; });

					// Row : Topic single value publish
					helper.Set(
						helper.AddSmallLabelTo(g, ++row, 0, content: "Topic:", verticalCenter: true),
						horizontalAlignment: AnyUiHorizontalAlignment.Right,
						horizontalContentAlignment: AnyUiHorizontalAlignment.Right);
					AnyUiUIElement.SetStringFromControl(
						helper.AddSmallTextBoxTo(g, row, 2,
							margin: new AnyUiThickness(0, 2, 2, 2),
							text: "" + data.SingleValueTopic,
							verticalCenter: true),
						(str) => { data.SingleValueTopic = str; });

					// give back
					return g;
				});

			if (!ticket.ScriptMode)
			{
				// do the dialogue
				if (!(await DisplayContext.StartFlyoverModalAsync(uc1)))
					return;

				// stop
				await Task.Delay(2000);
			}
#endif

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
                    await agent.Client.StartAsync(PackageCentral.Main, agent.DiaData, agent.Logger);
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
                    ExtendEnvironment.ReferableRootInfo foundRI = null;
                    if (PackageCentral != null && ev.Source?.Keys != null)
                        foreach (var pck in PackageCentral.GetAllPackageEnv())
                        {
                            var ri = new ExtendEnvironment.ReferableRootInfo();
                            var res = pck?.AasEnv?.FindReferableByReference(ev.Source, rootInfo: ri);
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
                        if (PackageCentral.MainAvailable)
                            PackageCentral.MainItem.Close();
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
                                PackageCentral.MainItem,
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
                        if (PackageCentral.MainAvailable)
                            PackageCentral.MainItem.Close();
                        System.IO.File.Delete(AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx");
                        await AasxOpenIdClient.OpenIDClient.Run(tag, value/*, this*/);

                        if (System.IO.File.Exists(AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx"))
                            UiLoadPackageWithNew(
                                PackageCentral.MainItem,
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
                    pi.InvokeAction(actionName, PackageCentral.Main, totalArgs.ToArray());

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
            out Aas.Environment env,
            out Aas.ISubmodel sm,
            out Aas.IReference smr,
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
                Logic?.LogErrorToTicket(ticket, "Submodel Read: No valid SubModel selected.");
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

                if (true == dlg.ShowDialog())
                {
                    RememberForInitialDirectory(sourceFn);
                    sourceFn = dlg.FileName;
                }

                if (Options.Curr.UseFlyovers) this.CloseFlyover();
            }

            if (sourceFn?.HasContent() != true)
            {
                Logic?.LogErrorToTicketOrSilent(ticket, msg);
                return false;
            }

            return true;
        }

        public void CommandBinding_ImportDictToSubmodel(
            string cmd,
            AasxMenuActionTicket ticket = null)
        {
            // These 2 functions are using WPF and cannot migrated to PackageLogic

            // REFACTOR: NO
            if (cmd == "importdictsubmodel")
            {
                // start
                ticket?.StartExec();

                // which item selected?
                Aas.Environment env = PackageCentral.Main.AasEnv;
                Aas.IAssetAdministrationShell aas = null;
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
                        Logic?.LogErrorToTicket(ticket,
                            "Dictionary Import: Please select the administration shell for the submodel import.");
                        return;
                    }
                }

#if !DoNotUseAasxDictionaryImport
                var dataChanged = false;
                try
                {
                    dataChanged = AasxDictionaryImport.Import.ImportSubmodel(this, env, Options.Curr.DictImportDir, aas);
                }
                catch (Exception ex)
                {
                    Logic?.LogErrorToTicket(ticket, ex, "An error occurred during the Dictionary import.");
                }

                if (dataChanged)
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    RestartUIafterNewPackage();
                    Mouse.OverrideCursor = null;
                }
#endif
            }

            // REFACTOR: NO
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
                    dataChanged = AasxDictionaryImport.Import.ImportSubmodelElements(
                        this, env, Options.Curr.DictImportDir, sm);
                }
                catch (Exception ex)
                {
                    Logic?.LogErrorToTicket(ticket, ex, "An error occurred during the submodel element import.");
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


        public async Task CommandBinding_ExportNodesetUaPlugin(
            string cmd,
            AasxMenuActionTicket ticket)
        {
            if (cmd == "opcuaexportnodesetuaplugin")
            {
                // filename
                // ReSharper disable UnusedVariable
                var uc = await DisplayContext.MenuSelectSaveFilenameAsync(
                    ticket, "File",
                    "Select Nodeset2.XML file to be exported",
                    "new.xml",
                    "OPC UA Nodeset2 files (*.xml)|*.xml|All files (*.*)|*.*",
                    "Export OPC UA Nodeset2 via plugin: No valid filename.");
                if (uc?.Result != true)
                    return;
                // ReSharper enable UnusedVariable
                try
                {
                    RememberForInitialDirectory(uc.TargetFileName);
                    CommandBinding_ExecutePluginServer(
                        "AasxPluginUaNetServer",
                        "server-start",
                        "server-stop",
                        "Export Nodeset2 via OPC UA Server...",
                        new[] { "-export-nodeset", uc.TargetFileName }
                        );
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(
                        ex, "When exporting UA nodeset via plug-in, an error occurred");
                }
            }
        }

        public async Task CommandBinding_ExportImportTableUml(
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
                    Logic?.LogErrorToTicket(ticket, ex,
                        $"Import/Export: While displaying html-based help.");
                }
            };
            // dead-csharp off
            //if (cmd == "exporttable" || cmd == "importtable")
            //{
            //    if (ticket?.ScriptMode != true)
            //    {
            //        // interactive
            //        // handle the export dialogue
            //        var uc = new ExportTableFlyout((cmd == "exporttable")
            //            ? "Export SubmodelElements as Table"
            //            : "Import SubmodelElements from Table");
            //        uc.Presets = Logic?.GetImportExportTablePreset().Item1;

            //        StartFlyoverModal(uc);

            //        if (uc.CloseForHelp)
            //        {
            //            callHelp?.Invoke();
            //            return;
            //        }

            //        if (uc.Result == null)
            //            return;

            //        // have a result
            //        var record = uc.Result;

            //        // be a little bit specific
            //        var dlgTitle = "Select text file to be exported";
            //        var dlgFileName = "";
            //        var dlgFilter = "";

            //        if (record.Format == (int)ImportExportTableRecord.FormatEnum.TSF)
            //        {
            //            dlgFileName = "new.txt";
            //            dlgFilter =
            //                "Tab separated file (*.txt)|*.txt|Tab separated file (*.tsf)|*.tsf|All files (*.*)|*.*";
            //        }
            //        if (record.Format == (int)ImportExportTableRecord.FormatEnum.LaTex)
            //        {
            //            dlgFileName = "new.tex";
            //            dlgFilter = "LaTex file (*.tex)|*.tex|All files (*.*)|*.*";
            //        }
            //        if (record.Format == (int)ImportExportTableRecord.FormatEnum.Excel)
            //        {
            //            dlgFileName = "new.xlsx";
            //            dlgFilter = "Microsoft Excel (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            //        }
            //        if (record.Format == (int)ImportExportTableRecord.FormatEnum.Word)
            //        {
            //            dlgFileName = "new.docx";
            //            dlgFilter = "Microsoft Word (*.docx)|*.docx|All files (*.*)|*.*";
            //        }
            //        if (record.Format == (int)ImportExportTableRecord.FormatEnum.NarkdownGH)
            //        {
            //            dlgFileName = "new.md";
            //            dlgFilter = "Markdown (*.md)|*.md|All files (*.*)|*.*";
            //        }

            //        // store
            //        ticket["Record"] = record;

            //        // ask now for a filename
            //        if (!(await DisplayContext.MenuSelectSaveFilenameToTicketAsync(
            //            ticket, "File",
            //            dlgTitle,
            //            dlgFileName,
            //            dlgFilter,
            //            "Import/ export table: No valid filename.")))
            //            return;
            //    }

            //    // pass on
            //    try
            //    {
            //        Logic?.CommandBinding_GeneralDispatchHeadless(cmd, null, ticket);
            //    }
            //    catch (Exception ex)
            //    {
            //        Logic?.LogErrorToTicket(ticket, ex, "Import/export table: passing on.");
            //    }
            //}

            //if (cmd == "importtimeseries")
            //{
            //    if (ticket?.ScriptMode != true)
            //    {
            //        // interactive
            //        // handle the export dialogue
            //        var uc = new ImportTimeSeriesFlyout();
            //        uc.Result = Logic?.GetImportExportTablePreset().Item3 ?? new ImportTimeSeriesRecord();

            //        StartFlyoverModal(uc);

            //        if (uc.Result == null)
            //            return;

            //        // have a result
            //        var result = uc.Result;

            //        // store
            //        ticket["Record"] = result;

            //        // be a little bit specific
            //        var dlgTitle = "Select file for time series import ..";
            //        var dlgFilter = "All files (*.*)|*.*";

            //        if (result.Format == (int)ImportTimeSeriesRecord.FormatEnum.Excel)
            //        {
            //            dlgFilter =
            //                "Tab separated file (*.txt)|*.txt|Tab separated file (*.tsf)|*.tsf|All files (*.*)|*.*";
            //        }

            //        // ask now for a filename
            //        if (!(await DisplayContext.MenuSelectOpenFilenameToTicketAsync(
            //            ticket, "File",
            //            dlgTitle,
            //            null,
            //            dlgFilter,
            //            "Import time series: No valid filename.")))
            //            return;
            //    }

            //    // pass on
            //    try
            //    {
            //        Logic?.CommandBinding_GeneralDispatchHeadless(cmd, null, ticket);
            //    }
            //    catch (Exception ex)
            //    {
            //        Logic?.LogErrorToTicket(ticket, ex, "Import time series: passing on.");
            //    }
            //}
            // dead-csharp on
            // redraw
            CommandExecution_RedrawAll();
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
                ToolFindReplace.TheAasEnv = PackageCentral.Main?.AasEnv;

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

        public async Task CommandBinding_RegistryTools(
            string cmd,
            AasxMenuActionTicket ticket)
        {
            var regtxt = "";
            var propFn = "";

			// see: https://learn.microsoft.com/de-de/windows/win32/shell/
            // how-to-assign-a-custom-icon-to-a-file-type?redirectedfrom=MSDN

			if (cmd == "attachfileassoc")
            {
                regtxt = AdminShellUtil.CleanHereStringWithNewlines(
                    @"Windows Registry Editor Version 5.00

                    [HKEY_CLASSES_ROOT\.aasx\DefaultIcon]
                    @ = ""%EXE%""");
                propFn = "attach_aasx.reg";
            }

			if (cmd == "removefileassoc")
			{
				regtxt = AdminShellUtil.CleanHereStringWithNewlines(
					@"Windows Registry Editor Version 5.00

                    [-HKEY_CLASSES_ROOT\.aasx\DefaultIcon]
                    [-HKEY_CLASSES_ROOT\.aasx]");
				propFn = "remove_aasx.reg";
			}

			if (cmd.HasContent() && propFn.HasContent())
            {
				if (!(await DisplayContext.MenuSelectSaveFilenameToTicketAsync(
                        ticket, "File",
                        "Create regedit for for .aasx file associations",
                        propFn,
						"RegEdit files (*.reg)|*.reg",
                        "No valid filename.", 
                        reworkSpecialFn: true)))
                    return;

                try
                {
                    var exepath = "" + Assembly.GetExecutingAssembly()?.Location;
                    exepath = exepath.Replace(@"\", @"\\");

                    regtxt = regtxt.Replace(@"%EXE%", exepath);

					File.WriteAllText(ticket["File"] as string, regtxt);

                    // ok, give hints
                    Log.Singleton.Info(StoredPrint.Color.Blue, "Click windows start menu, type regedit.exe " +
                        "and right-click and select 'Run as Administrator'. Import written file. Note: " +
                        "You might point Regedit.exe to desktop of your current user.");
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "when creating regedit file");
                }
            }
		}

    }
}
