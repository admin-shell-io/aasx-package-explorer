/*
Copyright (c) 2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


using AasxPackageLogic;
using AnyUi;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System;
using Org.Webpki.JsonCanonicalizer;
using System.IO;
using System.Windows;
using AasxIntegrationBase;
using Jose;
using System.Threading;
using AasxPackageLogic.PackageCentral;
using Newtonsoft.Json.Serialization;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxPackageExplorer
{
    /// <summary>
    /// An 'real' main window needs to implement these interfaces, to allow underlying
    /// business logic to trigger important state changes visible to the user, 
    /// e.g. loading / switching.
    /// </summary>
    public interface IMainWindow
    {
        /// <summary>
        /// Gets the interface to the components which manages the AAS tree elements (middle section)
        /// </summary>
        IDisplayElements GetDisplayElements();

        /// <summary>
        /// Clear AAS info, tree section, browser window
        /// </summary>
        void ClearAllViews();

        /// <summary>
        /// Clears the status line and pending errors.
        /// </summary>
        void StatusLineClear();

        /// <summary>
        /// Show log in a window / list perceivable for the user.
        /// </summary>
        void LogShow();

        /// <summary>
        /// Make sure the file repo is visible
        /// </summary>
		void UiShowRepositories(bool visible);

        /// <summary>
        /// Give a signal to redraw the repositories (because something has changed)
        /// </summary>
        public void RedrawRepositories();

        /// <summary>
        /// Redraw tree elements (middle), AAS entitty (right side)
        /// </summary>
        void CommandExecution_RedrawAll();

        /// <summary>
        /// Redraw window title, AAS info?, entity view (right), element tree (middle)
        /// </summary>
        /// <param name="keepFocus">Try remember which element was focussed and focus it after redrawing.</param>
        /// <param name="nextFocusMdo">Focus a new main data object attached to an tree element.</param>
        /// <param name="wishExpanded">If focussing, expand this item.</param>
        void RedrawAllAasxElements(
            bool keepFocus = false,
            object nextFocusMdo = null,
            bool wishExpanded = true);

        /// <summary>
        /// Based on save information, will redraw the AAS entity (element) view (right).
        /// </summary>
        /// <param name="hightlightField">Highlight field (for find/ replace)</param>
        void RedrawElementView(DispEditHighlight.HighlightFieldInfo hightlightField = null);

        // REFACTOR: for later refactoring
        /// <summary>
        /// Signal a redrawing and execute focussing afterwards.
        /// </summary>
        public void RedrawAllElementsAndFocus(object nextFocus = null, bool isExpanded = true);

        /// <summary>
        /// Large extend. Basially redraws everything after new package has been loaded.
        /// </summary>
        /// <param name="onlyAuxiliary">Only tghe AUX package has been altered.</param>
        void RestartUIafterNewPackage(bool onlyAuxiliary = false);

        /// <summary>
        /// This function serve as a kind of unified contact point for all kind
        /// of business functions to trigger loading an item to PackageExplorer data 
        /// represented by an item of PackageCentral. This function triggers UI procedures.
        /// </summary>
        /// <param name="packItem">PackageCentral item to load to</param>
        /// <param name="takeOverEnv">Already loaded environment to take over (alternative 1)</param>
        /// <param name="loadLocalFilename">Local filename to read (alternative 2)</param>
        /// <param name="info">Human information what is loaded</param>
        /// <param name="onlyAuxiliary">Treat as auxiliary load, not main item load</param>
        /// <param name="doNotNavigateAfterLoaded">Disable automatic navigate to behaviour</param>
        /// <param name="takeOverContainer">Already loaded container to take over (alternative 3)</param>
        /// <param name="storeFnToLRU">Store this filename into last recently used list</param>
        /// <param name="indexItems">Index loaded contents, e.g. for animate of event sending</param>
        void UiLoadPackageWithNew(
			PackageCentralItem packItem,
			AdminShellPackageEnv takeOverEnv = null,
			string loadLocalFilename = null,
			string info = null,
			bool onlyAuxiliary = false,
			bool doNotNavigateAfterLoaded = false,
			PackageContainerBase takeOverContainer = null,
			string storeFnToLRU = null,
			bool indexItems = false);

        /// <summary>
        /// Check for menu switch and flush events, if required.
        /// </summary>
        public void CheckIfToFlushEvents();

        /// <summary>
        /// Returns the <c>AasxMenu</c> of the main menu of the application.
        /// Purpose: script automation
        /// </summary>
        public AasxMenu GetMainMenu();

        /// <summary>
        /// Returns the <c>AasxMenu</c> of the dynmaically built menu of the application.
        /// Purpose: script automation
        /// </summary>
        public AasxMenu GetDynamicMenu();

        /// <summary>
        /// Returns the quite concise script interface of the application
        /// to allow script automation.
        /// </summary>
        public IAasxScriptRemoteInterface GetRemoteInterface();

        /// <summary>
        /// Allows an other class to inject a lambda action.
        /// This will be perceived by the main window, most likely.
        /// </summary>
        void AddWishForToplevelAction(AnyUiLambdaActionBase action);

    }
}