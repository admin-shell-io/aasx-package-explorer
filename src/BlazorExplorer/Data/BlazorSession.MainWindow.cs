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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using AnyUi;
using BlazorExplorer;
using BlazorExplorer.Shared;
using Microsoft.JSInterop;

namespace BlazorUI.Data
{
    /// <summary>
    /// This partial class holds the parts which are similar to the MainWindow of
    /// PAckage Explorer.
    /// </summary>
    public partial class BlazorSession : IDisposable, IMainWindow
    {
        /// <summary>
        /// Check for menu switch and flush events, if required.
        /// </summary>
        public void CheckIfToFlushEvents()
		{
			if (MainMenu?.IsChecked("CompressEvents") == true)
			{
				var evs = _eventCompressor?.Flush();
				if (evs != null)
					foreach (var ev in evs)
						PackageCentral?.PushEvent(ev);
			}
		}

        /// <summary>
        /// Clears the status line and pending errors.
        /// </summary>
        public void StatusLineClear()
        {
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.None, SessionId,
                    newLambdaAction: new AnyUiLambdaActionStatusLineClear()));
        }

        /// <summary>
        /// Show log in a window / list perceivable for the user.
        /// </summary>
        public async void LogShow()
        {
            var uc = new AnyUiDialogueDataTextEditor(
                caption: "Log display",
                mimeType: "text/plain");

            var sb = new StringBuilder();
            foreach (var sp in Log.Singleton.GetStoredLongTermPrints())
            {
                var line = "";
                if (sp.color == StoredPrint.Color.Blue || sp.color == StoredPrint.Color.Yellow)
                    line += "!";
                if (sp.color == StoredPrint.Color.Red)
                    line += "Error: ";
                line += sp.msg;
                if (sp.linkTxt != null)
                    line += $" ({sp.linkTxt} -> {sp.linkUri})";
                sb.AppendLine(line);
            }
            uc.Text = sb.ToString();
            uc.ReadOnly = true;

            await DisplayContext.StartFlyoverModalAsync(uc);
        }

        public void CommandExecution_RedrawAll()
        {
            // redraw everything
            RedrawAllAasxElements();
            RedrawElementView();
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

        public void RestartUIafterNewPackage(bool onlyAuxiliary = false)
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
                RedrawElementView();
                // ShowContentBrowser(Options.Curr.ContentHome, silent: true);
                // _eventHandling.Reset();
            }
        }

        /// <summary>
        /// Redraw window title, AAS info?, entity view (right), element tree (middle)
        /// </summary>
        /// <param name="keepFocus">Try remember which element was focussed and focus it after redrawing.</param>
        /// <param name="nextFocusMdo">Focus a new main data object attached to an tree element.</param>
        /// <param name="wishExpanded">If focussing, expand this item.</param>
        public void RedrawAllAasxElements(
            bool keepFocus = false,
            object nextFocusMdo = null,
            bool wishExpanded = true)
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
                PackageCentral, PackageCentral.Selector.Main, this.EditMode,
                lazyLoadingFirst: true);

            // ok .. try re-focus!!
            if (keepFocus || nextFocusMdo != null)
            {
                // make sure that Submodel is expanded
                this.DisplayElements.ExpandAllItems();

                // still proceed?
                var veFound = this.DisplayElements.SearchVisualElementOnMainDataObject(
                    (nextFocusMdo != null) ? nextFocusMdo : focusMdo,
                    alsoDereferenceObjects: true);

                // select?
                if (veFound != null)
                    DisplayElements.TrySelectVisualElement(veFound, wishExpanded: wishExpanded);
            }

            // Info box ..
            RedrawElementView();

            // display again
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.RebuildTreeKeepOpen, this.SessionId));
            DisplayElements.Refresh();

#if _log_times
            Log.Singleton.Info("Time 90 is: " + DateTime.Now.ToString("hh:mm:ss.fff"));
#endif
        }

        /// <summary>
        /// Based on save information, will redraw the AAS entity (element) view (right).
        /// </summary>
        /// <param name="hightlightField">Highlight field (for find/ replace)</param>
        public void RedrawElementView(DispEditHighlight.HighlightFieldInfo hightlightField = null)
        {
            if (DisplayElements == null)
                return;

            // no cached plugin
            DisposeLoadedPlugin();

            // the AAS will cause some more visual effects
            if (DisplayElements.SelectedItem is VisualElementAdminShell veaas)
                InfoBox.SetInfos(veaas.theAas, veaas.thePackage);
        }

        /// <summary>
        /// Clear AAS info, tree section, browser window
        /// </summary>
        public void ClearAllViews()
        {
            // left side
            InfoBox.AasId = "<id missing!>";
            InfoBox.HtmlImageData = "";
            InfoBox.AssetId = "<id missing!>";

            // middle side
            DisplayElements.Clear();
        }

        public void DisplayElements_SelectedItemChanged(object sender, EventArgs e)
        {
            // access
            if (DisplayElements == null || sender != DisplayElements)
                return;

            // try identify the business object
            if (DisplayElements.SelectedItem != null)
            {
                // ButtonHistory.Push(DisplayElements.SelectedItem);
            }

            // may be flush events
            CheckIfToFlushEvents();

            // redraw view
            RedrawElementView();
        }

        /// <summary>
        /// Make sure the file repo is visible
        /// </summary>
        public void UiShowRepositories(bool visible)
        {
            // for Blazor: nothing
            ;
        }

        /// <summary>
        /// Give a signal to redraw the repositories (because something has changed)
        /// </summary>
        public void RedrawRepositories()
        {
            // Blazor: simply redraw all
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.None, SessionId));
        }

        // REFACTOR: for later refactoring
        /// <summary>
        /// Signal a redrawing and execute focussing afterwards.
        /// </summary>
        public void RedrawAllElementsAndFocus(object nextFocus = null, bool isExpanded = true)
        {
            // Blazor: refer 
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.RebuildTreeKeepOpen, SessionId,
                    new AnyUiLambdaActionRedrawAllElements(nextFocus: nextFocus, isExpanded: isExpanded)));
        }

        /// <summary>
        /// Gets the interface to the components which manages the AAS tree elements (middle section)
        /// </summary>
        public IDisplayElements GetDisplayElements() => DisplayElements;

        /// <summary>
        /// Allows an other class to inject a lambda action.
        /// This will be perceived by the main window, most likely.
        /// </summary>
        public void AddWishForToplevelAction(AnyUiLambdaActionBase action)
        {
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.RebuildTreeKeepOpen, SessionId,
                    action));
        }

        //
        // Subject to refactor
        //

        protected class LoadFromFileRepositoryInfo
        {
            public Aas.IReferable Referable;
            public object BusinessObject;
        }

        protected async Task<LoadFromFileRepositoryInfo> LoadFromFileRepository(PackageContainerRepoItem fi,
            Aas.Reference requireReferable = null)
        {
            // access single file repo
            var fileRepo = PackageCentral.Repositories.FindRepository(fi);
            if (fileRepo == null)
                return null;

            // which file?
            var location = fileRepo.GetFullItemLocation(fi?.Location);
            if (location == null)
                return null;

            // try load (in the background/ RAM) first..
            PackageContainerBase container = null;
            try
            {
                Log.Singleton.Info($"Auto-load file from repository {location} into container");
                container = await PackageContainerFactory.GuessAndCreateForAsync(
                    PackageCentral,
                    location,
                    location,
                    overrideLoadResident: true,
                    null, null,
                    PackageContainerOptionsBase.CreateDefault(Options.Curr),
                    runtimeOptions: PackageCentral.CentralRuntimeOptions);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"When auto-loading {location}");
            }

            // if successfull ..
            if (container != null)
            {
                // .. try find business object!
                LoadFromFileRepositoryInfo res = new LoadFromFileRepositoryInfo();
                if (requireReferable != null)
                {
                    var rri = new ExtendEnvironment.ReferableRootInfo();
                    res.Referable = container.Env?.AasEnv.FindReferableByReference(requireReferable, rootInfo: rri);
                    res.BusinessObject = res.Referable;

                    // do some special decoding because auf Meta Model V3
                    if (rri.Asset != null)
                        res.BusinessObject = rri.Asset;
                }

                // only proceed, if business object was found .. else: close directly
                if (requireReferable != null && res.Referable == null)
                    container.Close();
                else
                {
                    // make sure the user wants to change
                    if (MainMenu?.IsChecked("FileRepoLoadWoPrompt") != true)
                    {
                        // ask double question
                        if (AnyUiMessageBoxResult.OK != await DisplayContext.MessageBoxFlyoutShowAsync(
                                "Load file from AASX file repository?",
                                "AASX File Repository",
                                AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
                            return null;
                    }

                    // start animation
                    fileRepo.StartAnimation(fi, PackageContainerRepoItem.VisualStateEnum.ReadFrom);

                    // activate
                    UiLoadPackageWithNew(PackageCentral.MainItem,
                        takeOverContainer: container, onlyAuxiliary: false);

                    Log.Singleton.Info($"Successfully loaded AASX {location}");
                }

                // return bo to focus
                return res;
            }

            return null;
        }

        public async Task UiHandleNavigateTo(
            Aas.Reference targetReference,
            bool alsoDereferenceObjects = true)
        {
            // access
            if (targetReference == null || targetReference.Keys.Count < 1)
                return;

            // make a copy of the Reference for searching
            VisualElementGeneric veFound = null;
            var work = targetReference.Copy();

            try
            {
                // remember some further supplementary search information
                var sri = ListOfVisualElement.StripSupplementaryReferenceInformation(work);
                work = sri.CleanReference;

                // for later search in visual elements, expand them all in order to be absolutely 
                // sure to find business object
                this.DisplayElements.ExpandAllItems();

                // incrementally make it unprecise
                while (work.Keys.Count > 0)
                {
                    // try to find a business object in the package
                    object bo = null;
                    if (PackageCentral.MainAvailable && PackageCentral.Main.AasEnv != null)
                        bo = PackageCentral.Main.AasEnv.FindReferableByReference(work);

                    // if not, may be in aux package
                    if (bo == null && PackageCentral.Aux != null && PackageCentral.Aux.AasEnv != null)
                        bo = PackageCentral.Aux.AasEnv.FindReferableByReference(work);

                    // if not, may look into the AASX file repo
                    if (bo == null && PackageCentral.Repositories != null)
                    {
                        // find?
                        PackageContainerRepoItem fi = null;
                        if (work.Keys[0].Type == Aas.KeyTypes.GlobalReference) //TODO: jtikekar KeyTypes.AssetInformation
                            fi = PackageCentral.Repositories.FindByAssetId(work.Keys[0].Value.Trim());
                        if (work.Keys[0].Type == Aas.KeyTypes.AssetAdministrationShell)
                            fi = PackageCentral.Repositories.FindByAasId(work.Keys[0].Value.Trim());

                        var boInfo = await LoadFromFileRepository(fi, work);
                        bo = boInfo?.BusinessObject;
                    }

                    // still yes?
                    if (bo != null)
                    {
                        // try to look up in visual elements
                        if (this.DisplayElements != null)
                        {
                            var ve = this.DisplayElements.SearchVisualElementOnMainDataObject(bo,
                                alsoDereferenceObjects: alsoDereferenceObjects, sri: sri);
                            if (ve != null)
                            {
                                veFound = ve;
                                break;
                            }
                        }
                    }

                    // make it more unprecice
                    work.Keys.RemoveAt(work.Keys.Count - 1);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While retrieving element requested for navigate to");
            }

            // if successful, try to display it
            try
            {
                if (veFound != null)
                {
                    // show ve
                    DisplayElements.TrySelectVisualElement(veFound, wishExpanded: true);
                    // remember in history
                    // ButtonHistory.Push(veFound);
                    // fake selection
                    RedrawElementView();
                    DisplayElements.Refresh();
                    // ContentTakeOver.IsEnabled = false;
                }
                else
                {
                    // everything is in default state, push adequate button history
                    var veTop = DisplayElements.GetDefaultVisualElement();
                    // ButtonHistory.Push(veTop);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While displaying element requested for navigate to");
            }
        }

    }

}
