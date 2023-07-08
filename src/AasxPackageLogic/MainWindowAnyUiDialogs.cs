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
using AdminShellNS;
using AnyUi;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;

// ReSharper disable MethodHasAsyncOverload

namespace AasxPackageLogic
{
    /// <summary>
    /// This class uses abstract dialogs provided by <c>AnyUiContextPlusDialogs</c> to
    /// provide menu functions involving user interactions, but not technology specific.
    /// </summary>
    public class MainWindowAnyUiDialogs : MainWindowHeadless
    {
        /// <summary>
        /// History of locations = AAS + Reference. For faster navigation between different
        /// packages (i.e. of a repository)
        /// </summary>
        public VisualElementHistoryStack LocationHistory = new VisualElementHistoryStack();

        /// <summary>
        /// Element of stack of editing locations. For faster jumping within a single
        /// package.
        /// </summary>
        protected class EditingLocation
        {
            public object MainDataObject;
            public bool IsExpanded;
        }

        /// <summary>
        /// Stack of editing location. For faster jumping.
        /// </summary>
		protected List<EditingLocation> _editingLocations = new List<EditingLocation>();

        /// <summary>
        /// Remembers user input for menu action
        /// </summary>
        protected static string _userLastPutUrl = "http://???:51310";

        /// <summary>
        /// Remembers user input for menu action
        /// </summary>
        protected static string _userLastGetUrl = "http://???:51310";

        /// <summary>
        /// Display context with more features for UI
        /// </summary>
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
                    PackageCentral.MainItem.Filename,
                    "AASX package files (*.aasx)|*.aasx|AASX package files w/ JSON (*.aasx)|*.aasx|" +
                        (!isLocalFile ? "" : "AAS XML file (*.xml)|*.xml|AAS JSON file (*.json)|*.json|") +
                        "All files (*.*)|*.*",
                    "Save AASX: No valid filename.",
                    reworkSpecialFn: true);
                if (ucsf?.Result != true)
                    return;

                // do
                try
                {
                    //// establish target filename
                    //if (ucsf.Location == AnyUiDialogueDataSaveFile.LocationKind.User)
                    //{
                    //    targetFn = PackageContainerUserFile.BuildUserFilePath(ucsf.TargetFileName);
                    //    targetFnForLRU = null;
                    //}

                    //if (ucsf.Location == AnyUiDialogueDataSaveFile.LocationKind.Download)
                    //{
                    //    // produce a .tmp file
                    //    targetFn = System.IO.Path.GetTempFileName();
                    //    targetFnForLRU = null;

                    //    // rename better
                    //    var _filterItems = AnyUiDialogueDataOpenFile.DecomposeFilter(ucsf.Filter);
                    //    targetFn = AnyUiDialogueDataOpenFile.ApplyFilterItem(
                    //        fi: _filterItems[ucsf.FilterIndex],
                    //        fn: targetFn,
                    //        userFn: ucsf.TargetFileName,
                    //        final: 3);
                    //}

                    //// if not local, do a bit of voodoo ..
                    //if (!isLocalFile && !isUserFile && PackageCentral.MainItem.Container != null)
                    //{
                    //    // establish local
                    //    if (!await PackageCentral.MainItem.Container.SaveLocalCopyAsync(
                    //        targetFn,
                    //        runtimeOptions: PackageCentral.CentralRuntimeOptions))
                    //    {
                    //        // Abort
                    //        LogErrorToTicket(ticket,
                    //            "Not able to copy current AASX file to local file. Aborting!");
                    //        return;
                    //    }

                    //    // re-load
                    //    MainWindow.UiLoadPackageWithNew(
                    //        PackageCentral.MainItem, null, targetFn, onlyAuxiliary: false,
                    //        storeFnToLRU: targetFnForLRU);
                    //    return;
                    //}

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
                    DisplayContextPlus.RememberForInitialDirectory(ucsf.TargetFileName);
                    await PackageCentral.MainItem.SaveAsAsync(ucsf.TargetFileName, prefFmt: prefFmt,
                        doNotRememberLocation: ucsf.Location != AnyUiDialogueDataSaveFile.LocationKind.Local);

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
                        if (lru != null && ucsf.Location == AnyUiDialogueDataSaveFile.LocationKind.Local)
                            lru.Push(PackageCentral?.MainItem?.Container as PackageContainerRepoItem,
                                ucsf.TargetFileName);
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
                            await DisplayContextPlus.WebBrowserDisplayOrDownloadFile(
                                ucsf.TargetFileName, "application/octet-stream");
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
                Log.Singleton.Info("AASX saved successfully as: {0}", ucsf.TargetFileName);
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
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

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
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);
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
                await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);
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
                    "For package decrypt: No valid filename for target given!",
                    reworkSpecialFn: true,
                    argLocation: "Location")))
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

                // delegate work
                await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                // browser?
                await DisplayContextPlus.CheckIfDownloadAndStart(
                    ticket, Log.Singleton, "File", "Location");
            }

            if (cmd == "assesssmt")
            {
                // start
                ticket.StartExec();

                //do
                try
                {
                    var val = new MenuFuncValidateSmt();
                    val.PerformValidation(package: PackageCentral.Main, fn: PackageCentral.MainItem.Filename);
                    await val.PerformDialogue(ticket, DisplayContext,
                        "Assess Submodel template ..");
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "when assessing Submodel template");
                }
            }

            if (cmd == "comparesmt")
            {
                // start
                ticket.StartExec();

                // extra check
                if (PackageCentral.Main?.AasEnv == null
                    || PackageCentral.Aux?.AasEnv == null)
                {
                    Log.Singleton.Error("Compare SMT: Two Submodel templates need to be given in main and " +
                        "auxiliary packages. Aborting!");
                    return;
                }

                //do
                try
                {
                    var val = new MenuFuncCompareSmt();
                    val.PerformCompare(
                        firstEnv: PackageCentral.Aux?.AasEnv, firstFn: PackageCentral.AuxItem.Filename,
                        secondEnv: PackageCentral.Main?.AasEnv, secondFn: PackageCentral.MainItem.Filename);
                    await val.PerformDialogue(ticket, DisplayContext,
                        "Compare Submodel template ..");
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "when assessing Submodel template");
                }
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

            if (cmd == "navigateback"
                && LocationHistory != null)
            {
                LocationHistory?.Pop();
            }

            if (cmd == "navigatehome")
            {
                // be careful
                try
                {
                    UiCheckIfActivateLoadedNavTo();
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "While displaying home element");
                }
            }

            if (cmd == "locationpush"
                && _editingLocations != null
                && MainWindow.GetDisplayElements()?.GetSelectedItem() is VisualElementGeneric vege
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
                MainWindow.GetDisplayElements()?.ClearSelection();
                MainWindow.GetDisplayElements()?.TrySelectMainDataObject(loc.MainDataObject, wishExpanded: loc.IsExpanded);
            }

            if (cmd == "statusclear")
            {
                ticket.StartExec();
                MainWindow?.StatusLineClear();
            }

            if (cmd == "logshow")
            {
                ticket.StartExec();
                MainWindow?.LogShow();
            }

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

                MainWindow.UiShowRepositories(visible: true);
                PackageCentral.Repositories.AddAtTop(new PackageContainerListLocal());
            }

            if (cmd == "filerepoopen")
            {
                // start
                ticket.StartExec();

                // filename
                var ucof = await DisplayContextPlus.MenuSelectOpenFilenameAsync(
                    ticket, "File",
                    "Select AASX file repository JSON file",
                    null,
                    "JSON files (*.JSON)|*.json|All files (*.*)|*.*",
                    "AASX file repository open: No valid filename.");
                if (ucof?.Result != true)
                    return;

                // ok
                var fr = this.UiLoadFileRepository(ucof.TargetFileName);
                MainWindow.UiShowRepositories(visible: true);
                PackageCentral.Repositories.AddAtTop(fr);
                MainWindow.RedrawRepositories();
            }

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
                    LogErrorToTicket(ticket, "No endpoint for repository given!");
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
                    MainWindow.UiShowRepositories(visible: true);
                    PackageCentral.Repositories.AddAtTop(fr);
                    MainWindow.RedrawRepositories();
                }
            }

            if (cmd == "filerepoquery")
                // TODO (MIHO, 2023-02-08): move the referred code here, as it is UI related
                await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

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
                    MainWindow.UiShowRepositories(visible: true);
                    var lruExist = PackageCentral?.Repositories?.FindLRU();
                    if (lruExist != null)
                        PackageCentral.Repositories.Remove(lruExist);
                    var lruNew = new PackageContainerListLastRecentlyUsed();
                    lruNew.Header = "Last Recently Used";
                    lruNew.SaveAs(lruFn);
                    MainWindow.UiShowRepositories(visible: true);
                    PackageCentral?.Repositories?.AddAtTop(lruNew);
                    MainWindow.RedrawRepositories();
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        $"while initializing last recently used file in {lruFn}.");
                }
            }

            if (cmd == "opcread")
            {
                // start
                ticket?.StartExec();

                // further to logic
                await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                // update
                MainWindow.RedrawAllAasxElements();
                MainWindow.RedrawElementView();
            }

            if (cmd == "submodelread")
            {
                // start
                ticket?.StartExec();

                // filename
                if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "File",
                    "Read Submodel from JSON data",
                    "Submodel_" + ticket?.Submodel?.IdShort + ".json",
                    "JSON files (*.JSON)|*.json|All files (*.*)|*.*",
                    "Submodel Read: No valid filename.")))
                    return;

                try
                {
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                    MainWindow.RedrawAllAasxElements();
                    MainWindow.RedrawElementView();
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "Submodel Read");
                }
            }

            if (cmd == "submodelwrite")
            {
                // start
                ticket.StartExec();

                // filename
                if (!(await DisplayContextPlus.MenuSelectSaveFilenameToTicketAsync(
                    ticket, "File",
                    "Write Submodel to JSON data",
                    "Submodel_" + ticket.Submodel?.IdShort + ".json",
                    "JSON files (*.JSON)|*.json|All files (*.*)|*.*",
                    "Submodel Read: No valid filename.",
                    reworkSpecialFn: true,
                    argLocation: "Location")))
                    return;

                // do it directly
                try
                {
                    // delegate work
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                    // browser?
                    await DisplayContextPlus.CheckIfDownloadAndStart(
                        ticket, Log.Singleton, "File", "Location");
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "Submodel Write");
                }
            }

            if (cmd == "submodelput")
            {
                // start
                ticket.StartExec();

                // URL
                if (!(await DisplayContextPlus.MenuSelectTextToTicketAsync(
                    ticket, "URL",
                    "REST server adress:",
                    _userLastPutUrl,
                    "Submodel Put: No valid URL selected,")))
                    return;

                _userLastPutUrl = ticket["URL"] as string;

                try
                {
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "Submodel Put");
                }
            }

            if (cmd == "submodelget")
            {
                // start
                ticket?.StartExec();

                // URL
                if (!(await DisplayContextPlus.MenuSelectTextToTicketAsync(
                    ticket, "URL",
                    "REST server adress:",
                    _userLastGetUrl,
                    "Submodel Get: No valid URL selected,")))
                    return;

                _userLastGetUrl = ticket["URL"] as string;

                try
                {
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "Submodel Get");
                }
            }

            if (cmd == "rdfread")
            {
                // filename
                if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "File",
                    "Select RDF file to be imported",
                    null,
                    "BAMM files (*.ttl)|*.ttl|All files (*.*)|*.*",
                    "RDF Read: No valid filename.")))
                    return;

                // do it
                try
                {
                    // do it
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                    // redisplay
                    MainWindow.RedrawAllAasxElements();
                    MainWindow.RedrawElementView();
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When importing, an error occurred");
                }
            }

            if (cmd == "bmecatimport")
            {
                // filename
                if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "File",
                    "Select BMEcat file to be imported",
                    null,
                    "BMEcat XML files (*.bmecat)|*.bmecat|All files (*.*)|*.*",
                    "RDF Read: No valid filename.")))
                    return;

                // do it
                try
                {
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When importing BMEcat, an error occurred");
                }
            }

            if (cmd == "csvimport")
            {
                // filename
                if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "File",
                    "Select CSF file to be imported",
                    null,
                    "CSV files (*.CSV)|*.csv|All files (*.*)|*.*",
                    "CSF import: No valid filename.")))
                    return;

                // do it
                try
                {
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When importing CSV, an error occurred");
                }
            }

            if (cmd == "submodeltdimport")
            {
                // filename
                if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "File",
                    "Select Thing Description (TD) file to be imported",
                    null,
                    "JSON files (*.JSONLD)|*.jsonld",
                    "TD import: No valid filename.")))
                    return;

                // do it
                try
                {
                    // delegate futher
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                    // redisplay
                    MainWindow.RedrawAllAasxElements();
                    MainWindow.RedrawElementView();
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When importing JSON LD for Thing Description, an error occurred");
                }
            }

            if (cmd == "submodeltdexport")
            {
                // filename
                if (!(await DisplayContextPlus.MenuSelectSaveFilenameToTicketAsync(
                    ticket, "File",
                    "Thing Description (TD) export",
                    "Submodel_" + ticket.Submodel?.IdShort + ".jsonld",
                    "JSON files (*.JSONLD)|*.jsonld",
                    "Thing Description (TD) export: No valid filename.",
                    reworkSpecialFn: true,
                    argLocation: "Location")))
                    return;

                // do it
                try
                {
                    // delegate work
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                    // browser?
                    await DisplayContextPlus.CheckIfDownloadAndStart(
                        ticket, Log.Singleton, "File", "Location");
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When exporting Thing Description (TD), an error occurred");
                }
            }

            if (cmd == "opcuaimportnodeset")
            {
                // filename
                if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "File",
                    "Select OPC UA Nodeset to be imported",
                    null,
                    "OPC UA NodeSet XML files (*.XML)|*.XML|All files (*.*)|*.*",
                    "OPC UA Nodeset import: No valid filename.")))
                    return;

                // do it
                try
                {
                    // do it
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                    // redisplay
                    MainWindow.RedrawAllAasxElements();
                    MainWindow.RedrawElementView();
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When importing OPC UA Nodeset, an error occurred");
                }
            }

            if (cmd == "importaml")
            {
                // start
                ticket?.StartExec();

                // filename
                if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
                    ticket, "File",
                    "Select AML file to be imported",
                    null,
                    "AutomationML files (*.aml)|*.aml|All files (*.*)|*.*",
                    "Import AML: No valid filename.")))
                    return;

                try
                {
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);
                    MainWindow.RestartUIafterNewPackage();
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "When importing AML, an error occurred");
                }
            }

            if (cmd == "exportaml")
            {
                // start
                ticket?.StartExec();

                // filename
                if (!(await DisplayContextPlus.MenuSelectSaveFilenameToTicketAsync(
                    ticket, "File",
                    "Select AML file to be exported",
                    "new.aml",
                    "AutomationML files (*.aml)|*.aml|AutomationML files (*.aml) (compact)|" +
                    "*.aml|All files (*.*)|*.*",
                    "Export AML: No valid filename.",
                    argFilterIndex: "FilterIndex",
                    reworkSpecialFn: true,
                    argLocation: "Location")))
                    return;

                try
                {
                    // delegate work
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                    // browser?
                    await DisplayContextPlus.CheckIfDownloadAndStart(
                        ticket, Log.Singleton, "File", "Location");
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "When exporting AML, an error occurred");
                }
            }

            // TODO (MIHO, 2023-02-08): Take over from AasxToolkit sources
            if (cmd == "exportcst")
            {
                // start
                ticket?.StartExec();

                LogErrorToTicket(ticket, "Currently, this export is only implemented in AasxToolkit!");
            }

            if (cmd == "exportjsonschema")
            {
                // start
                ticket?.StartExec();

                // filename prepare
                var fnPrep = "" + (MainWindow.GetDisplayElements()?.GetSelectedItem()?
                        .GetDereferencedMainDataObject() as Aas.IReferable)?.IdShort;
                if (!fnPrep.HasContent())
                    fnPrep = "new";

                // filename
                if (!(await DisplayContextPlus.MenuSelectSaveFilenameToTicketAsync(
                    ticket, "File",
                    "Select JSON schema file for Submodel templates to be written",
                    $"Submodel_Schema_{fnPrep}.json",
                    "JSON files (*.JSON)|*.json|All files (*.*)|*.*",
                    "Export JSON schema: No valid filename.",
                    argFilterIndex: "FilterIndex",
                    reworkSpecialFn: true,
                    argLocation: "Location")))
                    return;

                try
                {
                    // delegate work
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                    // browser?
                    await DisplayContextPlus.CheckIfDownloadAndStart(
                        ticket, Log.Singleton, "File", "Location");
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "When exporting JSON schema, an error occurred");
                }
            }

            if (cmd == "opcuai4aasexport")
            {
                // start
                ticket.StartExec();

                // try to access I4AAS export information
                try
                {
                    var xstream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                        "AasxPackageLogic.Resources.i4AASCS.xml");
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "when accessing i4AASCS.xml mapping types.");
                    return;
                }
                Log.Singleton.Info("Mapping types loaded.");

                // filename
                if (!(await DisplayContextPlus.MenuSelectSaveFilenameToTicketAsync(
                    ticket, "File",
                    "Select Nodeset file to be exported",
                    "new.xml",
                    "XML File (.xml)|*.xml|Text documents (.txt)|*.txt",
                    "Export i4AAS based OPC UA nodeset: No valid filename.",
                    reworkSpecialFn: true,
                    argLocation: "Location")))
                    return;

                // ReSharper enable PossibleNullReferenceException
                try
                {
                    // delegate work
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                    // browser?
                    await DisplayContextPlus.CheckIfDownloadAndStart(
                        ticket, Log.Singleton, "File", "Location");
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "when exporting i4AAS based OPC UA mapping.");
                }
            }

            if (cmd == "opcuai4aasimport")
            {
                // filename
                if (!(await DisplayContextPlus.MenuSelectOpenFilenameToTicketAsync(
                ticket, "File",
                    "Select Nodeset file to be imported",
                    "Document",
                    "XML File (.xml)|*.xml|Text documents (.txt)|*.txt",
                    "Import i4AAS based OPC UA nodeset: No valid filename.")))
                    return;

                // do
                try
                {
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                    // TODO (MIHO, 2022-11-17): not very elegant
                    if (ticket.PostResults != null && ticket.PostResults.ContainsKey("TakeOver")
                        && ticket.PostResults["TakeOver"] is AdminShellPackageEnv pe)
                        PackageCentral.MainItem.TakeOver(pe);

                    MainWindow.RestartUIafterNewPackage();
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "when importing i4AAS based OPC UA mapping.");
                }
            }

            if (cmd == "copyclipboardelementjson")
            {
                // get the selected element
                var ve = MainWindow.GetDisplayElements()?.GetSelectedItem();

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

                if (ve == null || mdo == null || !(mdo is IClass))
                {
                    await DisplayContext.MessageBoxFlyoutShowAsync(
                        "No valid element selected.", "Copy selected elements",
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }

                // ok, for Serialization we just want the plain element with no BLOBs..
                var jsonStr = Aas.Jsonization.Serialize.ToJsonObject(mdo as IClass)
                    .ToJsonString(new System.Text.Json.JsonSerializerOptions()
                    {
                        WriteIndented = true
                    });

                // copy to clipboard
                if (jsonStr != "")
                {
                    DisplayContext.ClipboardSet(new AnyUiClipboardData(jsonStr));
                    Log.Singleton.Info("Copied selected element to clipboard.");
                }
                else
                {
                    Log.Singleton.Info("No JSON text could be generated for selected element.");
                }
            }

            if (cmd == "exportgenericforms")
            {
                // start
                ticket?.StartExec();

                // filename
                if (!(await DisplayContextPlus.MenuSelectSaveFilenameToTicketAsync(
                    ticket, "File",
                    "Select options file for GenericForms to be exported",
                    "new.add-options.json",
                    "Options file for GenericForms (*.add-options.json)|*.add-options.json|All files (*.*)|*.*",
                    "Export GenericForms: No valid filename.",
                    argFilterIndex: "FilterIndex",
                    reworkSpecialFn: true,
                    argLocation: "Location")))
                    return;

                try
                {
                    // delegate work
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                    // browser?
                    await DisplayContextPlus.CheckIfDownloadAndStart(
                        ticket, Log.Singleton, "File", "Location");
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "When exporting GenericForms, an error occurred");
                }
            }

            if (cmd == "exportpredefineconcepts")
            {
                // start
                ticket?.StartExec();

                // filename
                if (!(await DisplayContextPlus.MenuSelectSaveFilenameToTicketAsync(
                    ticket, "File",
                    "Select text file for PredefinedConcepts to be exported",
                    "new.txt",
                    "Text file for PredefinedConcepts (*.txt)|*.txt|All files (*.*)|*.*",
                    "Export PredefinedConcepts: No valid filename.",
                    argFilterIndex: "FilterIndex",
                    reworkSpecialFn: true,
                    argLocation: "Location")))
                    return;

                try
                {
                    // delegate work
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

                    // browser?
                    await DisplayContextPlus.CheckIfDownloadAndStart(
                        ticket, Log.Singleton, "File", "Location");
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "When exporting PredefinedConcepts, an error occurred");
                }
            }

            if (cmd == "newsubmodelfromplugin")
            {
                // create a list of plugins, which are capable of generating Submodels
                var listOfSm = new List<AnyUiDialogueListItem>();
                var list = GetPotentialGeneratedSubmodels();
                if (list != null)
                    foreach (var rec in list)
                        listOfSm.Add(new AnyUiDialogueListItem(
                            "" + rec.Item1.name + " | " + "" + rec.Item2, rec));

                // could be nothing
                if (listOfSm.Count < 1)
                {
                    LogErrorToTicket(ticket, "New Submodel from plugin: No Submodels available " +
                        "to be generated by plugins.");
                    return;
                }

                // prompt if no name is given
                if (ticket["Name"] == null)
                {
                    var uc = new AnyUiDialogueDataSelectFromList(
                        "Select Plug-in and Submodel to be generated ..");
                    uc.ListOfItems = listOfSm;
                    if (!(await DisplayContext.StartFlyoverModalAsync(uc))
                        || uc.ResultItem == null)
                        return;

                    ticket["Record"] = uc.ResultItem.Tag;
                }

                // do it
                try
                {
                    // delegate futher
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When generating Submodel from plugins, an error occurred");
                }

                // redisplay
                MainWindow.RedrawAllElementsAndFocus(nextFocus: ticket["SmRef"]);
            }

            if (cmd == "newsubmodelfromknown")
            {
                // create a list of Submodels form the known pool
                var listOfSm = new List<AnyUiDialogueListItem>();
                foreach (var dom in AasxPredefinedConcepts.DefinitionsPool.Static.GetDomains())
                    listOfSm.Add(new AnyUiDialogueListItem("" + dom, dom));

                // could be nothing
                if (listOfSm.Count < 1)
                {
                    LogErrorToTicket(ticket, "New Submodel from pool of known: No Submodels available " +
                        "to be generated.");
                    return;
                }

                // prompt if no name is given
                if (ticket["Domain"] == null)
                {
                    var uc = new AnyUiDialogueDataSelectFromList(
                        "Select domain of known entities to be generated ..");
                    uc.ListOfItems = listOfSm;
                    if (!(await DisplayContext.StartFlyoverModalAsync(uc))
                        || uc.ResultItem == null)
                        return;

                    ticket["Domain"] = uc.ResultItem.Tag;
                }

                // do it
                try
                {
                    // delegate futher
                    await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When generating Submodel from known entities, an error occurred");
                }

                // redisplay
                MainWindow.RedrawAllElementsAndFocus(nextFocus: ticket["SmRef"]);
            }

            if (cmd == "convertelement")
            {
                // check
                var rf = ticket.DereferencedMainDataObject as Aas.IReferable;
                if (rf == null)
                {
                    LogErrorToTicket(ticket,
                        "Convert Referable: No valid Referable selected for conversion.");
                    return;
                }

                // try to get offers?
                if ((ticket["Name"] as string)?.HasContent() != true)
                {
                    var offers = AasxPredefinedConcepts.Convert.ConvertPredefinedConcepts.CheckForOffers(rf);
                    if (offers == null || offers.Count < 1)
                    {
                        LogErrorToTicket(ticket,
                            "Convert Referable: No valid conversion offers found for this Referable. Aborting.");
                        return;
                    }

                    // convert these to list items
                    var fol = new List<AnyUiDialogueListItem>();
                    foreach (var o in offers)
                        fol.Add(new AnyUiDialogueListItem(o.OfferDisplay, o));

                    // show a list
                    // prompt for this list
                    var uc = new AnyUiDialogueDataSelectFromList(
                        "Select Conversion action to be executed ..");
                    uc.ListOfItems = fol;
                    if (!(await DisplayContext.StartFlyoverModalAsync(uc))
                        || uc.ResultItem == null)
                        return;

                    ticket["Record"] = uc.ResultItem.Tag;
                }

                // pass on
                try
                {
                    {
                        await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);
                    }
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "Executing user defined conversion");
                }

                // redisplay
                MainWindow.RedrawAllElementsAndFocus(nextFocus: ticket.MainDataObject);
            }

            //
            // Any UI panels (from plugins)
            //

            // check if a plugin is attached to the name
            if (menuItem is AasxMenuItem mi && mi.PluginToAction?.HasContent() == true)
                // simply pass on, the headless function will check again
                await CommandBinding_GeneralDispatchHeadless(cmd, menuItem, ticket);

            //
            // Scripting : allow for server?
            //

            if (cmd == "scripteditlaunch")
            {
                // trivial things
                if (!PackageCentral.MainAvailable)
                {
                    await DisplayContext.MessageBoxFlyoutShowAsync(
                        "An AASX package needs to be available", "Error",
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Exclamation);
                    return;
                }

                // trivial things
                if (_aasxScript?.IsExecuting == true)
                {
                    if (AnyUiMessageBoxResult.No == await DisplayContext.MessageBoxFlyoutShowAsync(
                        "An AASX script is already executed! Continue anyway?", "Warning",
                        AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                        return;
                    else
                        // brutal
                        _aasxScript = null;
                }

                // prompt for the script
                var uc = new AnyUiDialogueDataTextEditorWithContextMenu("Edit script to be launched ..");
                uc.MimeType = "application/csharp";
                uc.Presets = Options.Curr.ScriptPresets;
                uc.Text = _currentScriptText;

                // context menu
                uc.ContextMenuCreate = () =>
                {
                    return new AasxMenu()
                            .AddAction("Clip", "Copy JSON to clipboard", "\U0001F4CB");
                };

                uc.ContextMenuAction = (cmd, mi, ticket) =>
                {
                    if (cmd == "clip")
                    {
                        var text = uc.Text;
                        var lines = text?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var sb = new StringBuilder();
                        sb.AppendLine("[");
                        if (lines != null)
                            foreach (var ln in lines)
                            {
                                var ln2 = ln.Replace("\"", "\\\"");
                                ln2 = ln2.Replace("\t", "    ");
                                sb.AppendLine($"\"{ln2}\",");
                            }
                        sb.AppendLine("]");
                        var jsonStr = sb.ToString();
                        DisplayContext.ClipboardSet(new AnyUiClipboardData(jsonStr));
                        Log.Singleton.Info("Copied JSON to clipboard.");
                    }
                };

                // execute
                await DisplayContext.StartFlyoverModalAsync(uc);

                // HACK: wait for modal window to close
                // TODO: remove
                await Task.Delay(1000);

                // always remember script
                _currentScriptText = uc.Text;

                // execute?
                if (uc.Result && uc.Text.HasContent())
                {
                    try
                    {
                        // create first
                        if (_aasxScript == null)
                            _aasxScript = new AasxScript();

                        // executing
                        _aasxScript.StartEnginBackground(
                            uc.Text, Options.Curr.ScriptLoglevel,
                            MainWindow.GetMainMenu(), MainWindow.GetRemoteInterface());
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "when executing script");
                    }
                }
            }

            // REFACTOR: SAME
            for (int i = 0; i < 9; i++)
                if (cmd == $"launchscript{i}"
                    && Options.Curr.ScriptPresets != null)
                {
                    // order in human sense
                    var scriptIndex = (i == 0) ? 9 : (i - 1);
                    if (scriptIndex >= Options.Curr.ScriptPresets.Count
                        || Options.Curr.ScriptPresets[scriptIndex]?.Text?.HasContent() != true)
                        return;

                    // still running?
                    if (_aasxScript?.IsExecuting == true)
                    {
                        if (AnyUiMessageBoxResult.No == await DisplayContext.MessageBoxFlyoutShowAsync(
                            "An AASX script is already executed! Continue anyway?", "Warning",
                            AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                            return;
                        else
                            // brutal
                            _aasxScript = null;
                    }

                    // prompting
                    if (!Options.Curr.ScriptLaunchWithoutPrompt)
                    {
                        if (AnyUiMessageBoxResult.Yes != await DisplayContext.MessageBoxFlyoutShowAsync(
                            $"Executing script preset #{1 + scriptIndex} " +
                            $"'{Options.Curr.ScriptPresets[scriptIndex].Name}'. \nContinue?",
                            "Question", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                            return;
                    }

                    // execute
                    try
                    {
                        // create first
                        if (_aasxScript == null)
                            _aasxScript = new AasxScript();

                        // executing
                        _aasxScript.StartEnginBackground(
                            Options.Curr.ScriptPresets[scriptIndex].Text, Options.Curr.ScriptLoglevel,
                            MainWindow.GetMainMenu(), MainWindow.GetRemoteInterface());
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "when executing script");
                    }
                }

        }

        //
        // some functions in close relation to UI menu functions
        //

        public PackageContainerListBase UiLoadFileRepository(string fn)
        {
            try
            {
                Log.Singleton.Info(
                    $"Loading aasx file repository {fn} ..");

                var fr = PackageContainerListFactory.GuessAndCreateNew(fn);

                if (fr != null)
                    return fr;
                else
                    Log.Singleton.Info(
                        $"File not found when loading aasx file repository {fn}");
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"When loading aasx file repository {Options.Curr.AasxRepositoryFn}");
            }

            return null;
        }

        /// <summary>
        /// Using the currently loaded AASX, will check if a CD_AasxLoadedNavigateTo elements can be
        /// found to be activated
        /// </summary>
        public bool UiCheckIfActivateLoadedNavTo()
        {
            // access
            if (PackageCentral.Main?.AasEnv == null || MainWindow.GetDisplayElements() == null)
                return false;

            // use convenience function
            foreach (var sm in PackageCentral.Main.AasEnv.FindAllSubmodelGroupedByAAS())
            {
                // check for ReferenceElement
                var navTo = sm?.SubmodelElements?.FindFirstSemanticIdAs<Aas.ReferenceElement>(
                    AasxPredefinedConcepts.PackageExplorer.Static.CD_AasxLoadedNavigateTo.GetSingleKey(),  //TODO:jtikekar Test
                    MatchMode.Relaxed);
                if (navTo?.Value == null)
                    continue;

                // remember some further supplementary search information
                var sri = ListOfVisualElement.StripSupplementaryReferenceInformation(navTo.Value);

                // lookup business objects
                var bo = PackageCentral.Main?.AasEnv.FindReferableByReference(sri.CleanReference);
                if (bo == null)
                    return false;

                // make sure that Submodel is expanded
                MainWindow.GetDisplayElements().ExpandAllItems();

                // still proceed?
                var veFound = MainWindow.GetDisplayElements().SearchVisualElementOnMainDataObject(bo,
                        alsoDereferenceObjects: true, sri: sri);
                if (veFound == null)
                    return false;

                // ok .. focus!!
                MainWindow.GetDisplayElements().TrySelectVisualElement(veFound, wishExpanded: true);

                // remember in history
                LocationHistory?.Push(veFound);

                // fake selection
                MainWindow.RedrawElementView();
                MainWindow.TakeOverContentEnable(false);
                MainWindow.UpdateDisplay();

                // finally break
                return true;
            }

            // nothing found
            return false;
        }

        public static bool SaveFilenameReworkTargetFilename(
            AnyUiDialogueDataSaveFile ucsf)
        {
            // access
            if (ucsf == null)
                return false;
            var notLocal = false;

            // establish target filename
            if (ucsf.Location == AnyUiDialogueDataSaveFile.LocationKind.User)
            {
                ucsf.TargetFileName = PackageContainerUserFile
                    .BuildUserFilePath(ucsf.TargetFileName);
                notLocal = true;
            }

            if (ucsf.Location == AnyUiDialogueDataSaveFile.LocationKind.Download)
            {
                // produce a .tmp file
                var targetFn = System.IO.Path.GetTempFileName();
                notLocal = true;

                // rename better?
                var _filterItems = AnyUiDialogueDataOpenFile.DecomposeFilter(ucsf.Filter);
                ucsf.TargetFileName = AnyUiDialogueDataOpenFile.ApplyFilterItem(
                    fi: _filterItems[ucsf.FilterIndex],
                    fn: targetFn,
                    userFn: ucsf.TargetFileName,
                    final: 3);
            }

            return notLocal;
        }
    }
}