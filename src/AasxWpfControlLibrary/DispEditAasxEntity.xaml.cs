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
using AasxPackageLogic;
using AasxWpfControlLibrary;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    public partial class DispEditAasxEntity : UserControl
    {

        private PackageCentral packages = null;
        private VisualElementGeneric theEntity = null;
        private DispEditHelperEntities helper = new DispEditHelperEntities();
        private AnyUiUIElement lastRenderedRootElement = null;
        private AnyUiDisplayContextWpf displayContext = null;

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

            if (helper?.context is AnyUiDisplayContextWpf dcwpf && dcwpf.WishForOutsideAction != null)
            {
                while (dcwpf.WishForOutsideAction.Count > 0)
                {
                    var temp = dcwpf.WishForOutsideAction[0];
                    dcwpf.WishForOutsideAction.RemoveAt(0);

                    // trivial?
                    if (temp is AnyUiLambdaActionNone)
                        continue;

                    // what?
                    if (temp is AnyUiLambdaActionRedrawEntity)
                    {
                        // redraw ourselves?
                        if (packages != null && theEntity != null)
                            DisplayOrEditVisualAasxElement(
                                packages, theEntity, helper.editMode, helper.hintMode);
                    }

                    // all other elements refer to superior functionality
                    this.WishForOutsideAction.Add(temp as AnyUiLambdaActionBase);
                }
            }
        }

        private void ContentUndo_Click(object sender, RoutedEventArgs e)
        {
            CallUndo();
        }

        public List<AnyUiLambdaActionBase> WishForOutsideAction = new List<AnyUiLambdaActionBase>();

        public void CallUndo()
        {
            try
            {
                var changes = true == displayContext?.CallUndoChanges(lastRenderedRootElement);
                if (changes)
                    displayContext.EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());

            } catch (Exception ex)
            {
                Log.Singleton.Error(ex, "undoing last changes");
            }
        }

        public void AddWishForOutsideAction(AnyUiLambdaActionBase action)
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
            lastRenderedRootElement = null;
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

            // hint mode disable, when not edit
            hintMode = hintMode && editMode;

            // remember objects for UI thread / redrawing
            this.packages = packages;
            this.theEntity = entity;
            helper.packages = packages;
            helper.highlightField = hightlightField;

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
            displayContext = new AnyUiDisplayContextWpf(flyoutProvider, packages);

            // ReSharper disable CoVariantArrayConversion            
            var levelColors = new DispLevelColors()
            {
                MainSection = new AnyUiBrushTuple(
                    AnyUiDisplayContextWpf.GetAnyUiBrush((SolidColorBrush)
                        System.Windows.Application.Current.Resources["DarkestAccentColor"]),
                    AnyUiBrushes.White),
                SubSection = new AnyUiBrushTuple(
                    AnyUiDisplayContextWpf.GetAnyUiBrush((SolidColorBrush)
                        System.Windows.Application.Current.Resources["LightAccentColor"]),
                    AnyUiBrushes.Black),
                SubSubSection = new AnyUiBrushTuple(
                    AnyUiDisplayContextWpf.GetAnyUiBrush((SolidColorBrush)
                        System.Windows.Application.Current.Resources["LightAccentColor"]),
                    AnyUiBrushes.Black),
                HintSeverityHigh = new AnyUiBrushTuple(
                    AnyUiDisplayContextWpf.GetAnyUiBrush((SolidColorBrush)
                        System.Windows.Application.Current.Resources["FocusErrorBrush"]),
                    AnyUiBrushes.White),
                HintSeverityNotice = new AnyUiBrushTuple(
                    AnyUiDisplayContextWpf.GetAnyUiBrush((SolidColorBrush)
                        System.Windows.Application.Current.Resources["LightAccentColor"]),
                    AnyUiDisplayContextWpf.GetAnyUiBrush((SolidColorBrush)
                        System.Windows.Application.Current.Resources["DarkestAccentColor"]))
            };
            // ReSharper enable CoVariantArrayConversion

            helper.levelColors = levelColors;

            // modify repository
            ModifyRepo repo = null;
            if (editMode)
            {
                repo = new ModifyRepo();
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
                        return new AnyUiLambdaActionNone();
                    });

                    var btn = new AnyUiButton();
                    btn.Content = "Click me!";
                    stack.Children.Add(btn);
                    repo.RegisterControl(btn, (o) =>
                    {
                        Log.Singleton.Error("Button clicked!");
                        return new AnyUiLambdaActionRedrawAllElements(null);
                    });
                }
            }
#endif

            //
            // Dispatch
            //

            var inhibitRenderStackToPanel = false;

            if (entity is VisualElementEnvironmentItem)
            {
                var x = entity as VisualElementEnvironmentItem;
                helper.DisplayOrEditAasEntityAasEnv(
                    packages, x.theEnv, x.theItemType, editMode, stack, hintMode: hintMode);
            }
            else if (entity is VisualElementAdminShell)
            {
                var x = entity as VisualElementAdminShell;
                helper.DisplayOrEditAasEntityAas(
                    packages, x.theEnv, x.theAas, editMode, stack, hintMode: hintMode);
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
                    packages, x.theEnv, aas, x.theSubmodelRef, x.theSubmodel, editMode, stack, 
                    hintMode: hintMode);
            }
            else if (entity is VisualElementSubmodel)
            {
                var x = entity as VisualElementSubmodel;
                helper.DisplayOrEditAasEntitySubmodelOrRef(
                    packages, x.theEnv, null, null, x.theSubmodel, editMode, stack, 
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
                    packages, x.theEnv, x.theContainer, x.theOpVar, editMode, 
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
                        packages, x.theEnv, xpaas.theAas, x.theView, editMode, stack, 
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
                        editMode, stack);
                else
                    helper.AddGroup(stack, "Reference is corrupted!", helper.levelColors.MainSection);
            }
            else
            if (entity is VisualElementSupplementalFile)
            {
                var x = entity as VisualElementSupplementalFile;
                helper.DisplayOrEditAasEntitySupplementaryFile(packages, x.theFile, editMode, stack);
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
                    // this is natively done; do NOT render Any UI to WPF
                    inhibitRenderStackToPanel = true;
                }

                // show no panel nor scroll
                renderHints.scrollingPanel = false;
                renderHints.showDataPanel = false;

            }
            else
                helper.AddGroup(stack, "Entity is unknown!", helper.levelColors.MainSection);

            // now render master stack
#if __export_BLAZOR
            var fn = @"file.json";
            if (!editMode)
            {
                count = 0;
                var jsonSerializerSettings = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.All,
                    Formatting = Formatting.Indented
                };
                var json = JsonConvert.SerializeObject(stack, jsonSerializerSettings);
                System.IO.File.WriteAllText(fn, json);
            }
            if (editMode)
            {
                if (true && count == 2)
                {
                    count = 0;
                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        // SerializationBinder = new DisplayNameSerializationBinder(
                        //    new[] { typeof(AasxIntegrationBase.AasForms.FormDescListOfElement), typeof(AasxIntegrationBase.AasForms.FormDescProperty) }),
                        // SerializationBinder = new DisplayNameSerializationBinder(
                        //     new[] { typeof(AnyUiStackPanel), typeof(AnyUiUIElement) }),
                        // NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Error,
                        TypeNameHandling = TypeNameHandling.All,
                        Formatting = Formatting.Indented
                    };

                    //if (stack is AnyUiPanel pan)
                    //{
                    //    for (int i = 0; i < pan.Children.Count; i++)
                    //    {
                    //        var json = JsonConvert.SerializeObject(pan.Children[i], settings);
                    //        System.IO.File.WriteAllText(fn+"."+i, json);
                    //    }
                    //}
                    var json = JsonConvert.SerializeObject(stack, settings);
                    System.IO.File.WriteAllText(fn, json);
                }
                count++;
                /*
                var writer = new System.Xml.Serialization.XmlSerializer(typeof(AnyUiUIElement));
                var wfile = new System.IO.StreamWriter(@"c:\development\fileEdit.xml");
                writer.Serialize(wfile, stack);
                wfile.Close();
                */
            }

#endif
#if MONOUI
#else
            // render Any UI to WPF?
            if (!inhibitRenderStackToPanel)
            {
                theMasterPanel.Children.Clear();
                var spwpf = displayContext.GetOrCreateWpfElement(stack);
                helper.ShowLastHighlights();
                DockPanel.SetDock(spwpf, Dock.Top);
                theMasterPanel.Children.Add(spwpf);
            }

            // keep the stack
            lastRenderedRootElement = stack;
#endif

            // return render hints
            return renderHints;
        }

        static int count = 0;

#endregion
    }
}
