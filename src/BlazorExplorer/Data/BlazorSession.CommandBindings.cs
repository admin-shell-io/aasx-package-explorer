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
using Microsoft.JSInterop;

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

            if (cmd == "locationpop")
            {
                Log.Singleton.Info("Time is " + DateTime.Now.ToString(CultureInfo.InvariantCulture));
            }

            //
            // The following commands should be ideally shared with WPF PackageExplorer,
            // but Blazor (often) seems to require the async options in order to have
            // the modal dialogs working
            //

            // REFACTOR: the same
            if (cmd == "open" || cmd == "openaux")
            {
                // start
                ticket.StartExec();

                // filename
                var fn = (await MenuSelectOpenFilenameAsync(
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
		}

        // ---------------------------------------------------------------------------
        #region Utilities

        private string lastFnForInitialDirectory = null;
        public void RememberForInitialDirectory(string fn)
        {
            this.lastFnForInitialDirectory = fn;
        }

        /// <summary>
        /// Selects a filename to read either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public async Task<AnyUiDialogueDataOpenFile> MenuSelectOpenFilenameAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg)
        {
            // filename
            var sourceFn = ticket?[argName] as string;

            if (sourceFn?.HasContent() != true)
            {
                var uc = new AnyUiDialogueDataOpenFile(
                    caption: caption, 
                    message: "Select filename by uploading it or from stored user files.",
                    filter: filter, proposeFn: proposeFn);
                uc.AllowUserFiles = PackageContainerUserFile.CheckForUserFilesPossible();

				if (await DisplayContext.StartFlyoverModalAsync(uc))
                {
                    // house keeping
                    RememberForInitialDirectory(uc.TargetFileName);

                    // modify
                    if (uc.ResultUserFile)
                        uc.TargetFileName = PackageContainerUserFile.Scheme + uc.TargetFileName;

                    // ok
					return uc;
                }
            }

            if (sourceFn?.HasContent() != true)
            {
                Logic.LogErrorToTicketOrSilent(ticket, msg);
                return null;
            }

            return new AnyUiDialogueDataOpenFile()
            {
                OriginalFileName = sourceFn,
                TargetFileName = sourceFn
            };
        }

        /// <summary>
        /// If ticket does not contain the filename named by <c>argName</c>,
        /// read it by the user.
        /// </summary>
        public async Task<bool> MenuSelectOpenFilenameToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg)
        {
            var uc = await MenuSelectOpenFilenameAsync(ticket, argName, caption, proposeFn, filter, msg);
            if (uc.Result)
            {
                ticket[argName] = uc.TargetFileName;                
                return true;
            }
            return false;
        }

        #endregion
    }
}
