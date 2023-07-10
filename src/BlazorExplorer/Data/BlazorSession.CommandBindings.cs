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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using BlazorExplorer;
using ImageMagick;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Newtonsoft.Json;
using AasCore.Aas3_0;

namespace BlazorUI.Data
{
    /// <summary>
    /// This partial class holds the bindings to the menu commands of a session.
    /// </summary>
    public partial class BlazorSession : IDisposable
    {
        public async Task CommandBinding_GeneralDispatch(
                    string cmd,
                    AasxMenuItemBase menuItem,
                    AasxMenuActionTicket ticket)
        {
            //
            // Start
            //

            if (cmd == null || ticket == null)
                return;

            Logic?.FillSelectedItem(DisplayElements.SelectedItem, ticket);

            // change of edit field display

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

                // in session, set these all important settings
                this.EditMode = MainMenu?.IsChecked("EditMenu") == true;
                this.HintMode = MainMenu?.IsChecked("HintsMenu") == true;

                // edit mode affects the total element view
                RedrawAllAasxElements(nextFocusMdo: currMdo);

                return;
            }

            // dispatching directly to PackageLogic

            if (cmd == "XXXXX")
            {
                Log.Singleton.Info("Time is " + DateTime.Now.ToString(CultureInfo.InvariantCulture));
                return;
            }

            // REFACTOR: 80% change
            if (cmd == "exit")
            {
                // start
                ticket.StartExec();

                // check user
                if (!ticket.ScriptMode
                    && AnyUiMessageBoxResult.Yes != await DisplayContext.MessageBoxFlyoutShowAsync(
                    "Close browser window? Unsaved date will get lost!", "Browser session",
                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                    return;

                // do
                await BlazorUI.Utils.BlazorUtils.CloseBrowserWindow(renderJsRuntime);

                // exit to NOT pass on
                return;
            }

            // REFACTOR: 100% change
            if (cmd == "about")
            {
                // start
                ticket.StartExec();

                var _pref = AasxPackageLogic.Pref.Read();
                if (_pref == null)
                {
                    Log.Singleton.Error("Unable to create preference data.");
                    return;
                }

                var headerTxt = "Copyright (c) 2018-2023 Festo SE & Co. KG, " +
                "Phoenix Contact, Microsoft, Hochschule Karlsruhe, " +
                "Fraunhofer e.V. and further (see GitHub).\n" +
                "Authors: " + _pref.Authors + " (see GitHub)\n" +
                "This software is licensed under the Apache License 2.0 (see GitHub)" + "\n" +
                "See: https://github.com/admin-shell-io/aasx-package-explorer/ (OK will follow)" + "\n" +
                "Version: " + _pref.Version + "\n" +
                "Build date: " + _pref.BuildDate;

                // do
                if (AnyUiMessageBoxResult.OK == await DisplayContext.MessageBoxFlyoutShowAsync(
                    headerTxt,
                    "AASX Package Explorer",
                    AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Information))
                {
                    await BlazorUI.Utils.BlazorUtils.ShowNewBrowserWindow(renderJsRuntime,
                    "https://github.com/admin-shell-io/aasx-package-explorer/");
                }

                return;
            }

            // REFACTOR: 100% change
            if (cmd == "helpgithub")
            {
                // start
                ticket.StartExec();

                // do
                await BlazorUI.Utils.BlazorUtils.ShowNewBrowserWindow(renderJsRuntime,
                    "https://github.com/admin-shell/aasx-package-explorer/blob/master/help/index.md");

                return;
            }

            // REFACTOR: 100% change
            if (cmd == "faqgithub")
            {
                // start
                ticket.StartExec();

                // do
                await BlazorUI.Utils.BlazorUtils.ShowNewBrowserWindow(renderJsRuntime,
                    "https://github.com/admin-shell-io/questions-and-answers/blob/master/README.md");

                return;
            }

            // REFACTOR: 100% change
            if (cmd == "helpissues")
            {
                // start
                ticket.StartExec();

                // do
                await BlazorUI.Utils.BlazorUtils.ShowNewBrowserWindow(renderJsRuntime,
                    "https://github.com/admin-shell-io/aasx-package-explorer/issues");

                return;
            }

            // REFACTOR: 70% change
            if (cmd == "helpoptionsinfo")
            {
                // start
                ticket.StartExec();

                // do
                try
                {
                    // make HTML string
                    var htmlStr = Options.ReportOptions(Options.ReportOptionsFormat.Html, Options.Curr);

                    // write to temp file
                    var tempFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".html");
                    System.IO.File.WriteAllText(tempFn, htmlStr);

                    // show
                    await BlazorUI.Utils.BlazorUtils.DisplayOrDownloadFile(renderJsRuntime, tempFn, "text/html");
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "Generating options information");
                }

                return;
            }

            // REFACTOR: 10% change, no good entity for ClearPasteBuffer()
            if (cmd == "bufferclear")
            {
                // start
                ticket.StartExec();

                // do
                ClearPasteBuffer();
                Log.Singleton.Info("Internal copy/ paste buffer cleared. Pasting of external JSON elements " +
                    "enabled.");

                return;
            }

            //TODO (??, 0000-00-00): REFACTOR
            if (cmd == "exportsmd")
            {
                Logic.LogErrorToTicket(ticket, "ExportSmd not implemented, yet.");

                return;
            }

            //TODO (??, 0000-00-00): REFACTOR
            if (cmd == "printasset")
            {
                Logic.LogErrorToTicket(ticket, "PrintAsset not implemented, yet.");

                return;
            }

            // REFACTOR: WPF required
            if (cmd == "importdictsubmodel")
            {
                Logic.LogErrorToTicket(ticket, "ImportDictSubmodel not implemented, yet.");

                return;
            }

            // REFACTOR: WPF required
            if (cmd == "importdictsubmodelelements")
            {
                Logic.LogErrorToTicket(ticket, "ImportDictSubmodelElements not implemented, yet.");

                return;
            }

            // REFACTOR: WPF required
            if (cmd == "opcuaexportnodesetuaplugin")
            {
                Logic.LogErrorToTicket(ticket, "OpcUaExportNodesetUaPlugin not implemented, yet.");

                return;
            }

            // REFACTOR: WPF required
            if (cmd == "serverrest"
                || cmd == "mqttpub"
                || cmd == "connectintegrated"
                || cmd == "connectsecure"
                || cmd == "connectrest")
            {
                Logic.LogErrorToTicket(ticket, "Some dialogs not implemented, yet.");

                return;
            }


            //// REFACTOR: WPF required
            //if (cmd == "exporttable"
            //    ||  cmd == "importtable"
            //    /* || cmd == "exportuml" */
            //    /* || cmd == "importtimeseries" */)
            //{
            //    Logic.LogErrorToTicket(ticket, "ExportInport Table UML not implemented, yet.");

            //    return;
            //}

            // REFACTOR: WPF required
            if (cmd == "serverpluginemptysample"
                || cmd == "serverpluginopcua"
                || cmd == "serverpluginmqtt")
            {
                Logic.LogErrorToTicket(ticket, "Some servers not implemented, yet.");

                return;
            }

            // REFACTOR: Find/ replace not in blazor
            if (cmd == "toolsfindtext" || cmd == "toolsfindforward" || cmd == "toolsfindbackward"
                || cmd == "toolsreplacetext" || cmd == "toolsreplacestay" || cmd == "toolsreplaceforward"
                || cmd == "toolsreplaceall")
            {
                Logic.LogErrorToTicket(ticket, "Find/ replace not implemented, yet.");

                return;
            }

            // REFACTOR: What does AAS core provide?
            if (cmd == "checkandfix")
            {
                Logic.LogErrorToTicket(ticket, "Check&fix not implemented, yet. AAS core might provide other means.");

                return;
            }

            // REFACTOR: WPF required
            if (cmd == "eventsresetlocks")
            {
                InTimer = false;
                Log.Singleton.Info("Events reset.");
            }

            if (cmd == "eventsshowlogkey"
                || cmd == "eventsshowlogmenu")
            {
                Logic.LogErrorToTicket(ticket, "Showing events not implemented, yet.");

                return;
            }

            // pass dispatch on to next (lower) level of menu functions
            await Logic.CommandBinding_GeneralDispatchAnyUiDialogs(cmd, menuItem, ticket);
        }

    }
}
