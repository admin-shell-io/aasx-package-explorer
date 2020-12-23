/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using AasxIntegrationBase;
using AasxWpfControlLibrary;
using AdminShellNS;
using AnyUi;

namespace AasxPackageExplorer
{
    public partial class DispEditAasxEntity : UserControl
    {

        private PackageCentral packages = null;
        private VisualElementGeneric theEntity = null;

        private ModifyRepo theModifyRepo = new ModifyRepo();

        private DispEditHelperEntities helper = new DispEditHelperEntities();

        #region Public events and properties
        //
        // Public events and properties
        //

        public DispEditAasxEntity()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Timer for below
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // check for wishes from the modify repo
            // TODO MIHO: remove
            if (theModifyRepo != null && theModifyRepo.WishForOutsideAction != null)
            {
                while (theModifyRepo.WishForOutsideAction.Count > 0)
                {
                    var temp = theModifyRepo.WishForOutsideAction[0];
                    theModifyRepo.WishForOutsideAction.RemoveAt(0);

                    // trivial?
                    if (temp is ModifyRepo.LambdaActionNone)
                        continue;

                    // what?
                    if (temp is ModifyRepo.LambdaActionRedrawEntity)
                    {
                        // redraw ourselves?
                        if (packages != null && theEntity != null)
                            DisplayOrEditVisualAasxElement(
                                packages, theEntity, helper.editMode, helper.hintMode,
                                flyoutProvider: helper.flyoutProvider);
                    }

                    // all other elements refer to superior functionality
                    this.WishForOutsideAction.Add(temp);
                }
            }

            if (helper?.context is AnyUiDisplayContextWpf dcwpf && dcwpf.WishForOutsideAction != null)
            {
                while (dcwpf.WishForOutsideAction.Count > 0)
                {
                    var temp = dcwpf.WishForOutsideAction[0];
                    dcwpf.WishForOutsideAction.RemoveAt(0);

                    // trivial?
                    if (temp is ModifyRepo.LambdaActionNone)
                        continue;

                    // what?
                    if (temp is ModifyRepo.LambdaActionRedrawEntity)
                    {
                        // redraw ourselves?
                        if (packages != null && theEntity != null)
                            DisplayOrEditVisualAasxElement(
                                packages, theEntity, helper.editMode, helper.hintMode,
                                flyoutProvider: helper.flyoutProvider);
                    }

                    // all other elements refer to superior functionality
                    this.WishForOutsideAction.Add(temp as ModifyRepo.LambdaAction);
                }
            }
        }

        private void ContentUndo_Click(object sender, RoutedEventArgs e)
        {
            if (theModifyRepo != null)
                theModifyRepo.CallUndoChanges();
        }

        public List<ModifyRepo.LambdaAction> WishForOutsideAction = new List<ModifyRepo.LambdaAction>();

        public void CallUndo()
        {
            if (theModifyRepo != null)
                theModifyRepo.CallUndoChanges();
        }

        public void AddWishForOutsideAction(ModifyRepo.LambdaAction action)
        {
            if (action != null && WishForOutsideAction != null)
                WishForOutsideAction.Add(action);
        }

        #endregion


        #region Element View Drawing


        //
        //
        // --- Overall calling function
        //
        //

        public AnyUiStackPanel ClearDisplayDefautlStack()
        {
            theMasterPanel.Children.Clear();
            var sp = new AnyUiStackPanel();
            // TODO MIHO
            var spwpf = new Label(); // sp.GetOrCreateWpfElement();
            DockPanel.SetDock(spwpf, Dock.Top);
            theMasterPanel.Children.Add(spwpf);
            return sp;
        }

        public void ClearHighlight()
        {
            if (this.helper != null)
                this.helper.ClearHighlights();
        }

        public class DisplayRenderHints
        {
            public bool scrollingPanel = true;
            public bool showDataPanel = true;
        }

        public DisplayRenderHints DisplayOrEditVisualAasxElement(
            PackageCentral packages,
            VisualElementGeneric entity,
            bool editMode, bool hintMode = false,
            IFlyoutProvider flyoutProvider = null,
            DispEditHighlight.HighlightFieldInfo hightlightField = null)
        {
            //
            // Start
            //

            var renderHints = new DisplayRenderHints();

            if (theMasterPanel == null || entity == null)
            {
                renderHints.showDataPanel = false;
                return renderHints;
            }

#if MONOUI
            var stack = ClearDisplayDefautlStack();
#else
            var stack = new AnyUiStackPanel();
#endif

            // create display context for WPF
            var displayContext = new AnyUiDisplayContextWpf(helper.repo, helper.flyoutProvider, packages);

            // ReSharper disable CoVariantArrayConversion            
            var levelColors = new DispLevelColors()
            {
                MainSection = new AnyUiBrushTuple(
                    displayContext.GetAnyUiBrush((SolidColorBrush)
                        System.Windows.Application.Current.Resources["DarkestAccentColor"]),
                    AnyUiBrushes.White),
                SubSection = new AnyUiBrushTuple(
                    displayContext.GetAnyUiBrush((SolidColorBrush)
                        System.Windows.Application.Current.Resources["LightAccentColor"]),
                    AnyUiBrushes.Black),
                SubSubSection = new AnyUiBrushTuple(
                    displayContext.GetAnyUiBrush((SolidColorBrush)
                        System.Windows.Application.Current.Resources["LightAccentColor"]),
                    AnyUiBrushes.Black),
                HintSeverityHigh = new AnyUiBrushTuple(
                    displayContext.GetAnyUiBrush((SolidColorBrush)
                        System.Windows.Application.Current.Resources["FocusErrorBrush"]),
                    AnyUiBrushes.White),
                HintSeverityNotice = new AnyUiBrushTuple(
                    displayContext.GetAnyUiBrush((SolidColorBrush)
                        System.Windows.Application.Current.Resources["LightAccentColor"]),
                    displayContext.GetAnyUiBrush((SolidColorBrush)
                        System.Windows.Application.Current.Resources["DarkestAccentColor"]))
            };
            // ReSharper enable CoVariantArrayConversion

            // hint mode disable, when not edit
            hintMode = hintMode && editMode;

            // remember objects for UI thread / redrawing
            this.packages = packages;
            this.theEntity = entity;
            helper.packages = packages;
            helper.flyoutProvider = flyoutProvider;
            helper.levelColors = levelColors;
            helper.highlightField = hightlightField;

            // modify repository
            ModifyRepo repo = null;
            if (editMode)
            {
                repo = theModifyRepo;
                repo.Clear();
            }
            helper.editMode = editMode;
            helper.hintMode = hintMode;
            helper.repo = repo;
            helper.context = displayContext;

            //
            // Test for Blazor
            //
#if __test_blazor
            if (false)
            {
                var lab = new AnyUiLabel();
                lab.Content = "Hallo";
                lab.Foreground = AnyUiBrushes.DarkBlue;
                stack.Children.Add(lab);

                if (editMode)
                {
                    var tb = new AnyUiTextBox();
                    tb.Foreground = AnyUiBrushes.Black;
                    tb.Text = "Initial";
                    stack.Children.Add(tb);
                    repo.RegisterControl(tb, (o) =>
                    {
                        Log.Singleton.Info($"Text changed to .. {""+o}");                        
                        return new ModifyRepo.LambdaActionNone();
                    });

                    var btn = new AnyUiButton();
                    btn.Content = "Click me!";
                    stack.Children.Add(btn);
                    repo.RegisterControl(btn, (o) =>
                    {
                        Log.Singleton.Error("Button clicked!");
                        return new ModifyRepo.LambdaActionRedrawAllElements(null);
                    });
                }
            }
#endif

            //
            // Dispatch
            //

            if (entity is VisualElementEnvironmentItem)
            {
                var x = entity as VisualElementEnvironmentItem;
                helper.DisplayOrEditAasEntityAasEnv(
                    packages, x.theEnv, x.theItemType, editMode, repo, stack, hintMode: hintMode);
            }
            else if (entity is VisualElementAdminShell)
            {
                var x = entity as VisualElementAdminShell;
                helper.DisplayOrEditAasEntityAas(
                    packages, x.theEnv, x.theAas, editMode, repo, stack, hintMode: hintMode);
            }
            else if (entity is VisualElementAsset)
            {
                var x = entity as VisualElementAsset;
                helper.DisplayOrEditAasEntityAsset(
                    packages, x.theEnv, x.theAsset, editMode, repo, stack, hintMode: hintMode);
            }
            else if (entity is VisualElementSubmodelRef)
            {
                var x = entity as VisualElementSubmodelRef;
                AdminShell.AdministrationShell aas = null;
                if (x.Parent is VisualElementAdminShell xpaas)
                    aas = xpaas.theAas;
                helper.DisplayOrEditAasEntitySubmodelOrRef(
                    packages, x.theEnv, aas, x.theSubmodelRef, x.theSubmodel, editMode, repo, stack, 
                    hintMode: hintMode);
            }
            else if (entity is VisualElementSubmodel)
            {
                var x = entity as VisualElementSubmodel;
                helper.DisplayOrEditAasEntitySubmodelOrRef(
                    packages, x.theEnv, null, null, x.theSubmodel, editMode, repo, stack, 
                    hintMode: hintMode);
            }
            else if (entity is VisualElementSubmodelElement)
            {
                var x = entity as VisualElementSubmodelElement;
                helper.DisplayOrEditAasEntitySubmodelElement(
                    packages, x.theEnv, x.theContainer, x.theWrapper, x.theWrapper.submodelElement, editMode,
                    repo, stack, hintMode: hintMode);
            }
            else if (entity is VisualElementOperationVariable)
            {
                var x = entity as VisualElementOperationVariable;
                helper.DisplayOrEditAasEntityOperationVariable(
                    packages, x.theEnv, x.theContainer, x.theOpVar, editMode, repo,
                    stack, hintMode: hintMode);
            }
            else if (entity is VisualElementConceptDescription)
            {
                var x = entity as VisualElementConceptDescription;
                helper.DisplayOrEditAasEntityConceptDescription(
                    packages, x.theEnv, null, x.theCD, editMode, repo, stack, hintMode: hintMode);
            }
            else if (entity is VisualElementView)
            {
                var x = entity as VisualElementView;
                if (x.Parent != null && x.Parent is VisualElementAdminShell xpaas)
                    helper.DisplayOrEditAasEntityView(
                        packages, x.theEnv, xpaas.theAas, x.theView, editMode, repo, stack, 
                        hintMode: hintMode);
                else
                    helper.AddGroup(stack, "View is corrupted!", helper.levelColors.MainSection);
            }
            else if (entity is VisualElementReference)
            {
                var x = entity as VisualElementReference;
                if (x.Parent != null && x.Parent is VisualElementView xpev)
                    helper.DisplayOrEditAasEntityViewReference(
                        packages, x.theEnv, xpev.theView, (AdminShell.ContainedElementRef)x.theReference,
                        editMode, repo, stack);
                else
                    helper.AddGroup(stack, "Reference is corrupted!", helper.levelColors.MainSection);
            }
            else
            if (entity is VisualElementSupplementalFile)
            {
                var x = entity as VisualElementSupplementalFile;
                helper.DisplayOrEditAasEntitySupplementaryFile(packages, x.theFile, editMode, repo, stack);
            }
            else if (entity is VisualElementPluginExtension)
            {
                // get data
                var x = entity as VisualElementPluginExtension;

                // create controls
                object result = null;

                try
                {
                    // replace at top level
                    theMasterPanel.Children.Clear();
                    if (x.thePlugin != null)
                        result = x.thePlugin.InvokeAction(
                            "fill-panel-visual-extension", x.thePackage, x.theReferable, theMasterPanel);
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

                // add?
                if (result == null)
                {
                    // re-init display!
#if MONOUI
                    stack = ClearDisplayDefautlStack();
#else
                    stack = new AnyUiStackPanel();
#endif

                    // helping message
                    helper.AddGroup(
                        stack, "Entity from Plugin cannot be rendered!", helper.levelColors.MainSection);
                }
                else
                {
                }

                // show no panel nor scroll
                renderHints.scrollingPanel = false;
                renderHints.showDataPanel = false;

            }
            else
                helper.AddGroup(stack, "Entity is unknown!", helper.levelColors.MainSection);

            // now render master stack

#if MONOUI
#else
            theMasterPanel.Children.Clear();
            var spwpf = displayContext.GetOrCreateWpfElement(stack);
            helper.ShowLastHighlights();
            DockPanel.SetDock(spwpf, Dock.Top);
            theMasterPanel.Children.Add(spwpf);
#endif

            // return render hints
            return renderHints;
        }

#endregion
    }
}
