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
using AdminShellNS;
using AnyUi;
using BlazorExplorer;
using ImageMagick;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;

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
                this.HintMode = MainMenu?.IsChecked("HintMenu") == true;

                // edit mode affects the total element view
                RedrawAllAasxElements(nextFocusMdo: currMdo);

                //// signalNewData should be sufficient:
                //// this.StateHasChanged(); 
                //Program.signalNewData(
                //    new Program.NewDataAvailableArgs(
                //        Program.DataRedrawMode.RebuildTreeKeepOpen, this.SessionId));

                //// fake selection
                //// RedrawElementView();
                //// select last object
                //if (currMdo != null)
                //{
                //    DisplayElements.TrySelectMainDataObject(currMdo, wishExpanded: true);
                //}
            }

            // dispatching directly to PackageLogic

            if (cmd == "filerepoquery")
                Logic?.CommandBinding_GeneralDispatch(cmd, ticket);

            if (cmd == "XXXXX")
            {
                Log.Singleton.Info("Time is " + DateTime.Now.ToString(CultureInfo.InvariantCulture));
            }

            //
            // The following commands should be ideally shared with WPF PackageExplorer,
            // but Blazor (often) seems to require the async options in order to have
            // the modal dialogs working
            //

            if (cmd == "new")
            {
                // start
                ticket.StartExec();

                // check user
                if (!ticket.ScriptMode
                    && AnyUiMessageBoxResult.Yes != await DisplayContext.MessageBoxFlyoutShowAsync(
                    "Create new Adminshell environment? This operation can not be reverted!", "AAS-ENV",
                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                    return;

                // do
                try
                {
                    // clear
                    ClearAllViews();
                    // create new AASX package
                    PackageCentral.MainItem.New();
                    // redraw
                    CommandExecution_RedrawAll();
                }
                catch (Exception ex)
                {
                    Logic?.LogErrorToTicket(ticket, ex, "when creating new AASX");
                    return;
                }
            }

            // REFACTOR: the same
            if (cmd == "open" || cmd == "openaux")
            {
                // start
                ticket.StartExec();

                // filename
                var fn = (await DisplayContext.MenuSelectOpenFilenameAsync(
                    ticket, "File",
                    "Open AASX",
                    null,
                    "AASX package files (*.aasx)|*.aasx|AAS XML file (*.xml)|*.xml|" +
                        "AAS JSON file (*.json)|*.json|All files (*.*)|*.*",
                    "Open AASX: No valid filename."))?.TargetFileName;
                if (fn == null)
                    return;

                switch (cmd)
                {
                    case "open":
                        UiLoadPackageWithNew(
                            PackageCentral.MainItem, null, fn, onlyAuxiliary: false,
                            storeFnToLRU: fn);
                        break;
                    case "openaux":
                        UiLoadPackageWithNew(
                            PackageCentral.AuxItem, null, fn, onlyAuxiliary: true);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected {nameof(cmd)}: {cmd}");
                }
            }

            // REFACTOR: the same
			if (cmd == "save")
			{
				// start
				ticket.StartExec();

				// open?
				if (!PackageCentral.MainStorable)
				{
					Logic?.LogErrorToTicket(ticket, "No open AASX file to be saved.");
					return;
				}

				// do
				try
				{
					// save
					await PackageCentral.MainItem.SaveAsAsync(runtimeOptions: PackageCentral.CentralRuntimeOptions);

					// backup
					if (Options.Curr.BackupDir != null)
						PackageCentral.MainItem.Container.BackupInDir(
							System.IO.Path.GetFullPath(Options.Curr.BackupDir),
							Options.Curr.BackupFiles,
							PackageContainerBase.BackupType.FullCopy);

					// may be was saved to index
					if (PackageCentral?.MainItem?.Container?.Env?.AasEnv != null)
						PackageCentral.MainItem.Container.SignificantElements
							= new IndexOfSignificantAasElements(PackageCentral.MainItem.Container.Env.AasEnv);

					// may be was saved to flush events
					CheckIfToFlushEvents();

					// as saving changes the structure of pending supplementary files, re-display
					RedrawAllAasxElements(keepFocus: true);
				}
				catch (Exception ex)
				{
					Logic?.LogErrorToTicket(ticket, ex, "when saving AASX");
					return;
				}

				Log.Singleton.Info("AASX saved successfully: {0}", PackageCentral.MainItem.Filename);
			}

            // REFACTOR: 20% change (supports user files & downloads also)
            if (cmd == "saveas")
            {
                // start
                ticket.StartExec();

                // open?
                if (!PackageCentral.MainAvailable || PackageCentral.MainItem.Container == null)
                {
                    Logic?.LogErrorToTicket(ticket, "No open AASX file to be saved.");
                    return;
                }

                //var uc = new AnyUiDialogueDataSaveFile("Test Caption", message: "Halli hallo",
                // filter: "AASX package files (*.aasx)|*.aasx|AASX package files w/ JSON (*.aasx)|*.aasx|" + 
                //    "AAS XML file (*.xml)|*.xml|AAS JSON file (*.json)|*.json|" +
                // "All files (*.*)|*.*", proposeFn: "Vorschlag");
                //uc.AllowUserFiles = PackageContainerUserFile.CheckForUserFilesPossible();
                //await DisplayContext.StartFlyoverModalAsync(uc);

                // shall be a local/ user file?!
                var isLocalFile = PackageCentral.MainItem.Container is PackageContainerLocalFile;
                var isUserFile = PackageCentral.MainItem.Container is PackageContainerUserFile;
                if (!isLocalFile && !isUserFile)
                    if (!ticket.ScriptMode
                        && AnyUiMessageBoxResult.Yes != await DisplayContext.MessageBoxFlyoutShowAsync(
                        "Current AASX file is not a local or user file. Proceed and convert to such file?",
                        "Save", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Hand))
                        return;

                // filename
                var ucsf = await DisplayContext.MenuSelectSaveFilenameAsync(
                    ticket, "File",
                    "Save AASX package",
                    PackageCentral.Main.Filename,
                    "AASX package files (*.aasx)|*.aasx|AASX package files w/ JSON (*.aasx)|*.aasx|" +
                        (!isLocalFile ? "" : "AAS XML file (*.xml)|*.xml|AAS JSON file (*.json)|*.json|") +
                        "All files (*.*)|*.*",
                    "Save AASX: No valid filename.");
                if (ucsf?.Result != true)
                    return;

				// do
				var targetFn = ucsf.TargetFileName;
				var targetFnForLRU = targetFn;
				
                try
				{
                    // establish target filename
					if (ucsf.Location == AnyUiDialogueDataSaveFile.LocationKind.User)
					{
						targetFn = PackageContainerUserFile.BuildUserFilePath(ucsf.TargetFileName);
                        targetFnForLRU = null;
					}
					if (ucsf.Location == AnyUiDialogueDataSaveFile.LocationKind.Download)
                    {
                        // produce a .tmp file
						targetFn = System.IO.Path.GetTempFileName();
						targetFnForLRU = null;

						// rename better
						var _filterItems = AnyUiDialogueDataOpenFile.DecomposeFilter(ucsf.Filter);
                        targetFn = AnyUiDialogueDataOpenFile.ApplyFilterItem(
                            fi: _filterItems[ucsf.FilterIndex],
                            fn: targetFn,
                            final: 2);
					}

					// if not local, do a bit of voodoo ..
					if (!isLocalFile && !isUserFile && PackageCentral.MainItem.Container != null)
                    {
                        // establish local
                        if (!await PackageCentral.MainItem.Container.SaveLocalCopyAsync(
							targetFn,
                            runtimeOptions: PackageCentral.CentralRuntimeOptions))
                        {
                            // Abort
                            Logic?.LogErrorToTicket(ticket,
                                "Not able to copy current AASX file to local file. Aborting!");
                            return;
                        }

                        // re-load
                        UiLoadPackageWithNew(
                            PackageCentral.MainItem, null, targetFn, onlyAuxiliary: false,
                            storeFnToLRU: targetFnForLRU);
                        return;
                    }

                    //
                    // ELSE .. already local
                    //

                    // preferred format
                    var prefFmt = AdminShellPackageEnv.SerializationFormat.None;
                    if (ucsf.FilterIndex == 1)
                        prefFmt = AdminShellPackageEnv.SerializationFormat.Xml;
                    if (ucsf.FilterIndex == 2)
                        prefFmt = AdminShellPackageEnv.SerializationFormat.Json;

                    // save 
                    DisplayContext.RememberForInitialDirectory(targetFn);
                    await PackageCentral.MainItem.SaveAsAsync(targetFn, prefFmt: prefFmt);

                    // backup (only for AASX)
                    if (ucsf.FilterIndex == 0)
                        if (Options.Curr.BackupDir != null)
                            PackageCentral.MainItem.Container.BackupInDir(
                                System.IO.Path.GetFullPath(Options.Curr.BackupDir),
                                Options.Curr.BackupFiles,
                                PackageContainerBase.BackupType.FullCopy);

                    // as saving changes the structure of pending supplementary files, re-display
                    RedrawAllAasxElements();

					// LRU?
					// record in LRU?
					try
					{
						var lru = PackageCentral?.Repositories?.FindLRU();
						if (lru != null && targetFnForLRU != null)
							lru.Push(PackageCentral?.MainItem?.Container as PackageContainerRepoItem, targetFnForLRU);
					}
					catch (Exception ex)
					{
						Log.Singleton.Error(
							ex, $"When managing LRU files");
						return;
					}

                    // if it is a download, provide link
                    if (ucsf.Location == AnyUiDialogueDataSaveFile.LocationKind.Download
                        && renderJsRuntime != null)
                    {
                        try
                        {
                            // prepare
                            //byte[] file = System.IO.File.ReadAllBytes(targetFn);
                            //string onlyFn = System.IO.Path.GetFileName(targetFn);

                            // send the data to JS to actually download the file
                            // await renderJsRuntime.InvokeVoidAsync("BlazorDownloadFile", targetFn, "application/octet-stream", file);

                            //InvokeAsync(async () =>
                            //{
                            await BlazorUI.Utils.BlazorUtils.DisplayOrDownloadFile(renderJsRuntime, targetFn, "application/octet-stream");
                            //    this.StateHasChanged();
                            //});

                            //await Task.Delay(5000);

                            //var uc = new AnyUiDialogueDataDownloadFile(
                            //    caption: "Save file as download ..",
                            //    message: "Please activate download!",
                            //    source: targetFn);
                            //await DisplayContext.StartFlyoverModalAsync(uc);

                            Log.Singleton.Info("Download initiated.");

                        } catch (Exception ex)
						{
							Log.Singleton.Error(
								ex, $"When downloading saved file");
							return;
						}
					}
				}
                catch (Exception ex)
                {
                    Logic?.LogErrorToTicket(ticket, ex, "when saving AASX");
                    return;
                }
                Log.Singleton.Info("AASX saved successfully as: {0}", targetFn);                
            }

            // REFACTOR: 1% change
            if (cmd == "close" && PackageCentral?.Main != null)
            {
                // start
                ticket.StartExec();

                if (!ticket.ScriptMode && AnyUiMessageBoxResult.Yes != await DisplayContext.MessageBoxFlyoutShowAsync(
                    "Do you want to close the open package? Please make sure that you have saved before.",
                    "Close Package?", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                    return;

                // do
                try
                {
                    PackageCentral.MainItem.Close();
                    RedrawAllAasxElements();
                }
                catch (Exception ex)
                {
                    Logic?.LogErrorToTicket(ticket, ex, "when closing AASX");
                }
            }

            // REFACTOR: 10% change .. SYNC lambda! .. race condition??
            if ((cmd == "sign" || cmd == "validatecertificate" || cmd == "encrypt") && PackageCentral?.Main != null)
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
                        useX509 = (AnyUiMessageBoxResult.Yes == await DisplayContext.MessageBoxFlyoutShowAsync(
                            "Use X509 (yes) or Verifiable Credential (No)?",
                            "X509 or VerifiableCredential",
                            AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Hand));
                    ticket["UseX509"] = useX509;

                    // further to logic
                    await Logic?.CommandBinding_GeneralDispatch(cmd, ticket);

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
                    await Logic?.CommandBinding_GeneralDispatch(cmd, ticket);
                    return;
                }

                // Porting (MIHO): this seems to be executed, if above functions are not engaged
                // suspecting: for whole AAS/ package or so ..

                // filename source
                if (!(await DisplayContext.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "Source",
                    "Select source AASX file to be processed",
                    null,
                    "AASX package files (*.aasx)|*.aasx",
                    "For package sign/ validate/ encrypt: No valid filename for source given!")))
                    return;

                if (cmd == "encrypt")
                {
                    // filename cert
                    if (!(await DisplayContext.MenuSelectOpenFilenameToTicketAsync(
                        ticket, "Certificate",
                        "Select certificate file",
                        null,
                        ".cer files (*.cer)|*.cer",
                        "For package sign/ validate/ encrypt: No valid filename for certificate given!")))
                        return;

                    // ask also for target fn
                    if (!(await DisplayContext.MenuSelectSaveFilenameToTicketAsync(
                        ticket, "Target",
                        "Write encoded AASX package file",
                        ticket["Source"] + "2",
                        "AASX2 encrypted package files (*.aasx2)|*.aasx2",
                        "For package sign/ validate/ encrypt: No valid filename for target given!")))
                        return;

                }

                if (cmd == "sign")
                {
                    // filename cert is required here
                    if (!(await DisplayContext.MenuSelectOpenFilenameToTicketAsync(
                        ticket, "Certificate",
                        "Select certificate file",
                        null,
                        ".pfx files (*.pfx)|*.pfx",
                        "For package sign/ validate/ encrypt: No valid filename for certificate given!")))
                        return;
                }

                // now, generally start
                ticket.StartExec();

                // as OZ designed, put user feedback on the screen
                ticket.InvokeMessage = (err, msg) =>
                {
                    return DisplayContext.MessageBoxFlyoutShow(
                        msg, "Operation", AnyUiMessageBoxButton.OKCancel,
                        err ? AnyUiMessageBoxImage.Error : AnyUiMessageBoxImage.Information);
                };

                // further to logic
                await Logic?.CommandBinding_GeneralDispatch(cmd, ticket);
            }

            // REFACTOR: 10% change .. SYNC lambda!
            if ((cmd == "decrypt") && PackageCentral.Main != null)
            {
                // start
                ticket.StartExec();

                // filename source
                if (!(await DisplayContext.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "Source",
                    "Select source encrypted AASX file to be processed",
                    null,
                    "AASX2 encrypted package files (*.aasx2)|*.aasx2",
                    "For package decrypt: No valid filename for source given!")))
                    return;

                // filename cert
                if (!(await DisplayContext.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "Certificate",
                    "Select source AASX file to be processed",
                    null,
                    ".pfx files (*.pfx)|*.pfx",
                    "For package decrypt: No valid filename for certificate given!")))
                    return;

                // ask also for target fn
                if (!(await DisplayContext.MenuSelectSaveFilenameToTicketAsync(
                    ticket, "Target",
                    "Write decoded AASX package file",
                    null,
                    "AASX package files (*.aasx)|*.aasx",
                    "For package decrypt: No valid filename for target given!")))
                    return;

                // now, generally start
                ticket.StartExec();

                // as OZ designed, put user feedback on the screen
                ticket.InvokeMessage = (err, msg) =>
                {
                    return DisplayContext.MessageBoxFlyoutShow(
                        msg, "Operation", AnyUiMessageBoxButton.OKCancel,
                        err ? AnyUiMessageBoxImage.Error : AnyUiMessageBoxImage.Information);
                };

                // further to logic
                await Logic?.CommandBinding_GeneralDispatch(cmd, ticket);
            }

            // REFACTOR: 0% change
            if (cmd == "closeaux" && PackageCentral.AuxAvailable)
            {
                // start
                ticket.StartExec();

                //do
                try
                {
                    PackageCentral.AuxItem.Close();
                }
                catch (Exception ex)
                {
                    Logic?.LogErrorToTicket(ticket, ex, "when closing auxiliary AASX");
                }
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
            }

			// REFACTOR: 100% change
			if (cmd == "helpgithub")
			{
				// start
				ticket.StartExec();

				// do
				await BlazorUI.Utils.BlazorUtils.ShowNewBrowserWindow(renderJsRuntime,
				    "https://github.com/admin-shell/aasx-package-explorer/blob/master/help/index.md");
			}

			// REFACTOR: 100% change
			if (cmd == "faqgithub")
			{
				// start
				ticket.StartExec();

				// do
				await BlazorUI.Utils.BlazorUtils.ShowNewBrowserWindow(renderJsRuntime,
					"https://github.com/admin-shell-io/questions-and-answers/blob/master/README.md");
			}

			// REFACTOR: 100% change
			if (cmd == "helpissues")
			{
				// start
				ticket.StartExec();

				// do
				await BlazorUI.Utils.BlazorUtils.ShowNewBrowserWindow(renderJsRuntime,
					"https://github.com/admin-shell-io/aasx-package-explorer/issues");
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
			}

			// REFACTOR: 0% change
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

			// REFACTOR: 0% change
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

			// REFACTOR: TODO
			if (cmd == "exportsmd")
            {
                Logic.LogErrorToTicket(ticket, "ExportSmd not implemented, yet.");
            }

			// REFACTOR: TODO
			if (cmd == "printasset")
			{
				Logic.LogErrorToTicket(ticket, "PrintAsset not implemented, yet.");
			}

			//
			// File repo
			//

			// REFACTOR: 10% change (async)
			if (cmd == "filereponew")
			{
				ticket.StartExec();

				if (ticket.ScriptMode != true 
                    && AnyUiMessageBoxResult.OK != await DisplayContext.MessageBoxFlyoutShowAsync(
						"Create new (empty) file repository? It will be added to list of repos on the lower/ " +
						"left of the screen.",
						"AASX File Repository",
						AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
					return;

				this.UiAssertFileRepository(visible: true);
				PackageCentral.Repositories.AddAtTop(new PackageContainerListLocal());
			}

            // REFACTOR: 10% change: async, RedrawRepos()
            if (cmd == "filerepoopen")
			{
				// start
				ticket.StartExec();

                // filename
                var ucof = await DisplayContext.MenuSelectOpenFilenameAsync(
                    ticket, "File",
                    "Select AASX file repository JSON file",
                    null,
                    "JSON files (*.JSON)|*.json|All files (*.*)|*.*",
                    "AASX file repository open: No valid filename.");
                if (ucof?.Result != true)
					return;

				// ok
				var fr = this.UiLoadFileRepository(ucof.TargetFileName);
				this.UiAssertFileRepository(visible: true);
				PackageCentral.Repositories.AddAtTop(fr);
                RedrawRepos();
			}

            // REFACTOR: 20% change: use AnyUiTextBox, RedrawRepos()
            if (cmd == "filerepoconnectrepository")
			{
				ticket.StartExec();

				// read server address
				var endpoint = ticket["Endpoint"] as string;
				if (endpoint?.HasContent() != true)
				{
                    var uc = new AnyUiDialogueDataTextBox("REST endpoint (without \"/server/listaas\"):",
                    symbol: AnyUiMessageBoxImage.Question);
                    uc.Text = Options.Curr.DefaultConnectRepositoryLocation;
                    await DisplayContext.StartFlyoverModalAsync(uc);
                    if (!uc.Result)
                        return;
					endpoint = uc.Text;
				}

				if (endpoint?.HasContent() != true)
				{
					Logic?.LogErrorToTicket(ticket, "No endpoint for repository given!");
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
					PackageCentral.Repositories.AddAtTop(fr);
                    RedrawRepos();
                }
			}

            // REFACTOR: 0% change
            if (cmd == "filerepoquery")
				Logic?.CommandBinding_GeneralDispatch(cmd, ticket);

            // REFACTOR: 10% change: async, RedrawRepos()
            if (cmd == "filerepocreatelru")
			{
				if (ticket.ScriptMode != true 
                    && AnyUiMessageBoxResult.OK != await DisplayContext.MessageBoxFlyoutShowAsync(
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
					var lruExist = PackageCentral?.Repositories?.FindLRU();
					if (lruExist != null)
						PackageCentral.Repositories.Remove(lruExist);
					var lruNew = new PackageContainerListLastRecentlyUsed();
					lruNew.Header = "Last Recently Used";
					lruNew.SaveAs(lruFn);
					this.UiAssertFileRepository(visible: true);
					PackageCentral?.Repositories?.AddAtTop(lruNew);
                    RedrawRepos();
                }
				catch (Exception ex)
				{
					Logic?.LogErrorToTicket(ticket, ex,
						$"while initializing last recently used file in {lruFn}.");
				}
			}
		}


		// ---------------------------------------------------------------------------
		#region Utilities


		#endregion
	}
}
