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
using AdminShellNS;
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
                    Program.DataRedrawMode.None, this.SessionId));
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

    }

}
