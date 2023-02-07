/*
Copyright (c) 2022 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2022 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic.PackageCentral;
using AasxPredefinedConcepts.Convert;
using AasxSignature;
using AnyUi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using System.Windows;
using Microsoft.VisualBasic.Logging;

// ReSharper disable MethodHasAsyncOverload

namespace AasxPackageLogic
{
    /// <summary>
    /// This class uses abstract dialogs provided by <c>AnyUiContextPlusDialogs</c> to
    /// provide menu functions involving user interactions, but or not technology specific.
    /// </summary>
    public class MainWindowAnyUiDialogs : MainWindowHeadless
    {
        public AnyUiContextPlusDialogs DisplayContextPlus
        {
            get => DisplayContext as AnyUiContextPlusDialogs;
        }

        /// <summary>
        /// General dispatch for menu functions, which base on AnyUI dialogues.
        /// </summary>
        public async Task CommandBinding_GeneralDispatchAnyUiDialogs(
                    string cmd,
                    AasxMenuItemBase menuItem,
                    AasxMenuActionTicket ticket)
        {
            //
            // Start
            //

            if (cmd == null || ticket == null || DisplayContextPlus == null || MainWindow == null)
                return;

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
                    MainWindow.ClearAllViews();
                    // create new AASX package
                    PackageCentral.MainItem.New();
                    // redraw
                    MainWindow.CommandExecution_RedrawAll();
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "when creating new AASX");
                    return;
                }
            }

            if (cmd == "open" || cmd == "openaux")
            {
                // start
                ticket.StartExec();

                // filename
                var fn = (await DisplayContextPlus.MenuSelectOpenFilenameAsync(
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
                        MainWindow.UiLoadPackageWithNew(
                            PackageCentral.MainItem, null, fn, onlyAuxiliary: false,
                            storeFnToLRU: fn);
                        break;
                    case "openaux":
                        MainWindow.UiLoadPackageWithNew(
                            PackageCentral.AuxItem, null, fn, onlyAuxiliary: true);
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
                if (!PackageCentral.MainStorable)
                {
                    LogErrorToTicket(ticket, "No open AASX file to be saved.");
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
                    MainWindow.CheckIfToFlushEvents();

                    // as saving changes the structure of pending supplementary files, re-display
                    MainWindow.RedrawAllAasxElements(keepFocus: true);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "when saving AASX");
                    return;
                }

                Log.Singleton.Info("AASX saved successfully: {0}", PackageCentral.MainItem.Filename);
            }

            if (cmd == "saveas")
            {
                // start
                ticket.StartExec();

                // open?
                if (!PackageCentral.MainAvailable || PackageCentral.MainItem.Container == null)
                {
                    LogErrorToTicket(ticket, "No open AASX file to be saved.");
                    return;
                }

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
                var ucsf = await DisplayContextPlus.MenuSelectSaveFilenameAsync(
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
                            LogErrorToTicket(ticket,
                                "Not able to copy current AASX file to local file. Aborting!");
                            return;
                        }

                        // re-load
                        MainWindow.UiLoadPackageWithNew(
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
                    DisplayContextPlus.RememberForInitialDirectory(targetFn);
                    await PackageCentral.MainItem.SaveAsAsync(targetFn, prefFmt: prefFmt);

                    // backup (only for AASX)
                    if (ucsf.FilterIndex == 0)
                        if (Options.Curr.BackupDir != null)
                            PackageCentral.MainItem.Container.BackupInDir(
                                System.IO.Path.GetFullPath(Options.Curr.BackupDir),
                                Options.Curr.BackupFiles,
                                PackageContainerBase.BackupType.FullCopy);

                    // as saving changes the structure of pending supplementary files, re-display
                    MainWindow.RedrawAllAasxElements();

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
                        && DisplayContextPlus.WebBrowserServicesAllowed())
                    {
                        try
                        {
                            await DisplayContextPlus.WebBrowserDisplayOrDownloadFile(targetFn, "application/octet-stream");
                            Log.Singleton.Info("Download initiated.");
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(
                                ex, $"When downloading saved file");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "when saving AASX");
                    return;
                }
                Log.Singleton.Info("AASX saved successfully as: {0}", targetFn);
            }

            if (cmd == "close" && PackageCentral?.Main != null)
            {
                // start
                ticket.StartExec();

                if (!ticket.ScriptMode
                    && AnyUiMessageBoxResult.Yes != await DisplayContext.MessageBoxFlyoutShowAsync(
                    "Do you want to close the open package? Please make sure that you have saved before.",
                    "Close Package?", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                    return;

                // do
                try
                {
                    PackageCentral.MainItem.Close();
                    MainWindow.RedrawAllAasxElements();
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "when closing AASX");
                }
            }

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
                    await CommandBinding_GeneralDispatchHeadless(cmd, ticket);

                    // update
                    MainWindow.RedrawAllAasxElements();
                    MainWindow.RedrawElementView();
                    return;
                }

                if (cmd == "validatecertificate" && (ticket.Submodel != null || ticket.SubmodelElement != null))
                {
                    // start
                    ticket.StartExec();

                    // further to logic
                    await CommandBinding_GeneralDispatchHeadless(cmd, ticket);
                    return;
                }

                // Porting (MIHO): this seems to be executed, if above functions are not engaged
                // suspecting: for whole AAS/ package or so ..

                // filename source
                if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "Source",
                    "Select source AASX file to be processed",
                    null,
                    "AASX package files (*.aasx)|*.aasx",
                    "For package sign/ validate/ encrypt: No valid filename for source given!")))
                    return;

                if (cmd == "encrypt")
                {
                    // filename cert
                    if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
                        ticket, "Certificate",
                        "Select certificate file",
                        null,
                        ".cer files (*.cer)|*.cer",
                        "For package sign/ validate/ encrypt: No valid filename for certificate given!")))
                        return;

                    // ask also for target fn
                    if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
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
                    if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
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
                ticket.InvokeMessage = async (err, msg) =>
                {
                    return await DisplayContext.MessageBoxFlyoutShowAsync(
                        msg, "Operation", AnyUiMessageBoxButton.OKCancel,
                        err ? AnyUiMessageBoxImage.Error : AnyUiMessageBoxImage.Information);
                };

                // further to logic
                await CommandBinding_GeneralDispatchHeadless(cmd, ticket);
            }

            if ((cmd == "decrypt") && PackageCentral.Main != null)
            {
                // start
                ticket.StartExec();

                // filename source
                if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "Source",
                    "Select source encrypted AASX file to be processed",
                    null,
                    "AASX2 encrypted package files (*.aasx2)|*.aasx2",
                    "For package decrypt: No valid filename for source given!")))
                    return;

                // filename cert
                if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "Certificate",
                    "Select source AASX file to be processed",
                    null,
                    ".pfx files (*.pfx)|*.pfx",
                    "For package decrypt: No valid filename for certificate given!")))
                    return;

                // ask also for target fn
                if (!(await DisplayContextPlus.MenuSelectSaveFilenameToTicketAsync(
                    ticket, "Target",
                    "Write decoded AASX package file",
                    null,
                    "AASX package files (*.aasx)|*.aasx",
                    "For package decrypt: No valid filename for target given!")))
                    return;

                // now, generally start
                ticket.StartExec();

                // as OZ designed, put user feedback on the screen
                ticket.InvokeMessage = async (err, msg) =>
                {
                    return await DisplayContext.MessageBoxFlyoutShowAsync(
                        msg, "Operation", AnyUiMessageBoxButton.OKCancel,
                        err ? AnyUiMessageBoxImage.Error : AnyUiMessageBoxImage.Information);
                };

                // further to logic
                await CommandBinding_GeneralDispatchHeadless(cmd, ticket);
            }

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
                    LogErrorToTicket(ticket, ex, "when closing auxiliary AASX");
                }
            }
        }

    }
}