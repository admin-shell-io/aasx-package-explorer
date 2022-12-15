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
using AasxPackageLogic.PackageCentral;
using AasxWpfControlLibrary;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;
using static AnyUi.AnyUiDisplayContextWpf;

namespace AasxPackageExplorer
{
    public partial class DispEditAasxEntity : UserControl
    {
        private PackageCentral _packages = null;
        private ListOfVisualElementBasic _theEntities = null;
        private DispEditHelperMultiElement _helper = new DispEditHelperMultiElement();
        private AnyUiUIElement _lastRenderedRootElement = null;
        private AnyUiDisplayContextWpf _displayContext = null;

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

            if (_helper?.context is AnyUiDisplayContextWpf dcwpf && dcwpf.WishForOutsideAction != null)
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
                        if (_packages != null && _theEntities != null)
                            DisplayOrEditVisualAasxElement(
                                _packages, _theEntities, _helper.editMode, _helper.hintMode,
                                flyoutProvider: _displayContext?.FlyoutProvider,
                                appEventProvider: _helper?.appEventsProvider);
                    }

                    // all other elements refer to superior functionality
                    this.WishForOutsideAction.Add(temp);
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
                var changes = true == _displayContext?.CallUndoChanges(_lastRenderedRootElement);
                if (changes)
                    _displayContext.EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());

            }
            catch (Exception ex)
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
            var spwpf = new Label();
            DockPanel.SetDock(spwpf, Dock.Top);
            theMasterPanel.Children.Add(spwpf);
            _lastRenderedRootElement = null;
            return sp;
        }

        public void ClearHighlight()
        {
            if (this._helper != null)
                this._helper.ClearHighlights();
        }

        public void ClearPasteBuffer()
        {
            if (this._helper.theCopyPaste != null)
                this._helper.theCopyPaste.Clear();
        }

        public class DisplayRenderHints
        {
            public bool scrollingPanel = true;
            public bool showDataPanel = true;
            public bool useInnerGrid = false;
        }

        // TODO (MIHO, 2020-12-24): check if required
        private DispLevelColors GetLevelColorsFromResources()
        {
            // ReSharper disable CoVariantArrayConversion            
            var res = new DispLevelColors()
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
            return res;
        }

#if _not_needed
        public DisplayRenderHints DisplayMessage(string message)
        {
            // reset
            _packages = null;
            _theEntity = null;
            _displayContext = null;
            _lastRenderedRootElement = null;

            // Grid to fill full page
            var g = new Grid();
            g.Background = Brushes.DarkGray;
            g.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0) }) ;
            g.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0) });

            // textblock
            var tb = new TextBlock();
            tb.Foreground = Brushes.White;
            tb.FontSize = 18.0;
            tb.Text = "" + message;
            tb.FontWeight = FontWeights.Bold;
            tb.HorizontalAlignment = HorizontalAlignment.Center;
            tb.VerticalAlignment = VerticalAlignment.Center;
            g.Children.Add(tb);
            Grid.SetRow(tb, 1);
            Grid.SetColumn(tb, 1);

            // manually render
            theMasterPanel.Children.Clear();
            theMasterPanel.Children.Add(g);

            // render hints
            var rh = new DisplayRenderHints()
            {
                scrollingPanel = false,
                showDataPanel = false
            };
            return rh;
        }
#endif

        public DisplayRenderHints DisplayOrEditVisualAasxElement(
            PackageCentral packages,
            ListOfVisualElementBasic entities,
            bool editMode, bool hintMode = false, bool showIriMode = false,
            VisualElementEnvironmentItem.ConceptDescSortOrder? cdSortOrder = null,
            IFlyoutProvider flyoutProvider = null,
            IPushApplicationEvent appEventProvider = null,
            DispEditHighlight.HighlightFieldInfo hightlightField = null,
            AasxMenu superMenu = null)
        {
            //
            // Start
            //

            // hint mode disable, when not edit
            hintMode = hintMode && editMode;

            // remember objects for UI thread / redrawing
            this._packages = packages;
            this._theEntities = entities;
            _helper.packages = packages;
            _helper.highlightField = hightlightField;
            _helper.appEventsProvider = appEventProvider;

            // primary access
            var renderHints = new DisplayRenderHints();
            if (theMasterPanel == null || entities == null || entities.Count < 1)
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
            _displayContext = new AnyUiDisplayContextWpf(flyoutProvider, packages);
            _helper.levelColors = DispLevelColors.GetLevelColorsFromOptions(Options.Curr);

            // modify repository
            ModifyRepo repo = null;
            if (editMode)
            {
                // some functionality still uses repo != null to detect editMode!!
                repo = new ModifyRepo();
            }
            _helper.editMode = editMode;
            _helper.hintMode = hintMode;
            _helper.repo = repo;
            _helper.showIriMode = showIriMode;
            _helper.context = _displayContext;

            // inform plug that their potential panel might not shown anymore
            Plugins.AllPluginsInvoke("clear-panel-visual-extension");

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

            var inhibitRenderStackToPanel = false;

            if (entities.ExactlyOne)
            {
                //
                // Dispatch: ONE item
                //
                var entity = entities.First();

                // maintain parent. If in doubt, set null
                ListOfVisualElement.SetParentsBasedOnChildHierarchy(entity);

                //
                // Dispatch
                //

                if (entity is VisualElementEnvironmentItem veei)
                {
                    _helper.DisplayOrEditAasEntityAasEnv(
                        packages, veei.theEnv, veei, editMode, stack, hintMode: hintMode,
                        superMenu: superMenu);
                }
                else if (entity is VisualElementAdminShell veaas)
                {
                    _helper.DisplayOrEditAasEntityAas(
                        packages, veaas.theEnv, veaas.theAas, editMode, stack, hintMode: hintMode,
                        superMenu: superMenu);
                }
                else if (entity is VisualElementAsset veas)
                {
                    _helper.DisplayOrEditAasEntityAsset(
                        packages, veas.theEnv, veas.theAsset, editMode, repo, stack, hintMode: hintMode,
                        superMenu: superMenu);
                }
                else if (entity is VisualElementSubmodelRef vesmref)
                {
                    // data
                    AdminShell.AdministrationShell aas = null;
                    if (vesmref.Parent is VisualElementAdminShell xpaas)
                        aas = xpaas.theAas;

                    // edit
                    _helper.DisplayOrEditAasEntitySubmodelOrRef(
                        packages, vesmref.theEnv, aas, vesmref.theSubmodelRef, vesmref.theSubmodel, editMode, stack,
                        hintMode: hintMode);
                }
                else if (entity is VisualElementSubmodel vesm && vesm.theSubmodel != null)
                {
                    _helper.DisplayOrEditAasEntitySubmodelOrRef(
                        packages, vesm.theEnv, null, null, vesm.theSubmodel, editMode, stack,
                        hintMode: hintMode);
                }
                else if (entity is VisualElementSubmodelElement vesme)
                {
                    _helper.DisplayOrEditAasEntitySubmodelElement(
                        packages, vesme.theEnv, vesme.theContainer, vesme.theWrapper, vesme.theWrapper.submodelElement,
                        editMode,
                        repo, stack, hintMode: hintMode, superMenu: superMenu,
                        nestedCds: cdSortOrder.HasValue &&
                            cdSortOrder.Value == VisualElementEnvironmentItem.ConceptDescSortOrder.BySme);
                }
                else if (entity is VisualElementOperationVariable vepv)
                {
                    _helper.DisplayOrEditAasEntityOperationVariable(
                        packages, vepv.theEnv, vepv.theContainer, vepv.theOpVar, editMode,
                        stack, hintMode: hintMode, superMenu: superMenu);
                }
                else if (entity is VisualElementConceptDescription vecd)
                {
                    _helper.DisplayOrEditAasEntityConceptDescription(
                        packages, vecd.theEnv, null, vecd.theCD, editMode, repo, stack, hintMode: hintMode,
                        superMenu: superMenu,
                        preventMove: cdSortOrder.HasValue &&
                            cdSortOrder.Value != VisualElementEnvironmentItem.ConceptDescSortOrder.None);
                }
                else if (entity is VisualElementView vevw)
                {
                    if (vevw.Parent != null && vevw.Parent is VisualElementAdminShell xpaas)
                        _helper.DisplayOrEditAasEntityView(
                            packages, vevw.theEnv, xpaas.theAas, vevw.theView, editMode, stack,
                            hintMode: hintMode, superMenu: superMenu);
                    else
                        _helper.AddGroup(stack, "View is corrupted!", _helper.levelColors.MainSection);
                }
                else if (entity is VisualElementReference verf)
                {
                    if (verf.Parent != null && verf.Parent is VisualElementView xpev)
                        _helper.DisplayOrEditAasEntityViewReference(
                            packages, verf.theEnv, xpev.theView, (AdminShell.ContainedElementRef)verf.theReference,
                            editMode, stack);
                    else
                        _helper.AddGroup(stack, "Reference is corrupted!", _helper.levelColors.MainSection);
                }
                else
                if (entity is VisualElementSupplementalFile vesf)
                {
                    _helper.DisplayOrEditAasEntitySupplementaryFile(packages, vesf, vesf.theFile, editMode, stack,
                        superMenu: superMenu);
                }
                else if (entity is VisualElementPluginExtension vepe)
                {
                    // Try to figure out plugin rendering approach (1=WPF, 2=AnyUI)
                    var approach = 0;
                    var hasWpf = vepe.thePlugin?.HasAction("fill-panel-visual-extension") == true;
                    var hasAnyUi = vepe.thePlugin?.HasAction("fill-anyui-visual-extension") == true;

                    if (hasWpf && Options.Curr.PluginPrefer?.ToUpper().Contains("WPF") == true)
                        approach = 1;

                    if (hasAnyUi && Options.Curr.PluginPrefer?.ToUpper().Contains("ANYUI") == true)
                        approach = 2;

                    if (approach == 0 && hasAnyUi)
                        approach = 2;

                    if (approach == 0 && hasWpf)
                        approach = 1;

                    // NEW: Differentiate behaviour ..
                    if (approach == 2)
                    {
                        //
                        // Render panel via ANY UI !!
                        //

                        try
                        {
                            var opContext = new PluginOperationContextBase()
                            {
                                DisplayMode = (editMode)
                                            ? PluginOperationDisplayMode.MayAddEdit
                                            : PluginOperationDisplayMode.JustDisplay
                            };

                            vepe.thePlugin?.InvokeAction(
                                "fill-anyui-visual-extension", vepe.thePackage, vepe.theReferable,
                                stack, _displayContext, AnyUiDisplayContextWpf.SessionSingletonWpf,
                                opContext);
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(ex,
                                $"render AnyUI based visual extension for plugin {vepe.thePlugin.name}");
                        }

                        // show no panel nor scroll
                        renderHints.scrollingPanel = false;
                        renderHints.showDataPanel = false;
                        renderHints.useInnerGrid = true;
                    }
                    else
                    {
                        //
                        // SWAP panel with NATIVE WPF CONTRAL and try render via WPF !!
                        //

                        // create controls
                        object result = null;

                        if (approach == 1)
                            try
                            {
                                // replace at top level
                                theMasterPanel.Children.Clear();
                                if (vepe.thePlugin != null)
                                    result = vepe.thePlugin.InvokeAction(
                                        "fill-panel-visual-extension",
                                        vepe.thePackage, vepe.theReferable, theMasterPanel);
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(ex,
                                    $"render WPF based visual extension for plugin {vepe.thePlugin.name}");
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
                            _helper.AddGroup(
                                stack, "Entity from Plugin cannot be rendered!", _helper.levelColors.MainSection);
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

                }
                else
                    _helper.AddGroup(stack, "Entity is unknown!", _helper.levelColors.MainSection);
            }
            else
            {
                //
                // Dispatch: MULTIPLE items
                //
                _helper.DisplayOrEditAasEntityMultipleElements(packages, entities, editMode, stack, cdSortOrder, 
                    superMenu: superMenu);
            }

            // now render master stack
#if __export_BLAZOR
            var fn = @"fileEdit.json";
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
                        //    new[] { typeof(AasxIntegrationBase.AasForms.FormDescListOfElement), 
                        //      typeof(AasxIntegrationBase.AasForms.FormDescProperty) }),
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
                // rendering
                theMasterPanel.Children.Clear();
                UIElement spwpf = null;
                if (renderHints.useInnerGrid
                    && stack?.Children != null
                    && stack.Children.Count == 1
                    && stack.Children[0] is AnyUiGrid grid)
                {
                    spwpf = _displayContext.GetOrCreateWpfElement(grid);
                }
                else
                {
                    spwpf = _displayContext.GetOrCreateWpfElement(stack);
                    DockPanel.SetDock(spwpf, Dock.Top);
                }
                _helper.ShowLastHighlights();

                theMasterPanel.Children.Add(spwpf);

                // register key shortcuts
                var num = _displayContext.PrepareNameList(stack);
                if (num > 0)
                {
                    _displayContext.RegisterKeyShortcut(
                        "aas-elem-move-up", ModifierKeys.Shift | ModifierKeys.Control, Key.Up,
                        "Move current AAS element up by one position.");

                    _displayContext.RegisterKeyShortcut(
                        "aas-elem-move-down", ModifierKeys.Shift | ModifierKeys.Control, Key.Down,
                        "Move current AAS element down by one position.");

                    _displayContext.RegisterKeyShortcut(
                        "aas-elem-move-top", ModifierKeys.Shift | ModifierKeys.Control, Key.Home,
                        "Move current AAS element to the first position of the respective list.");

                    _displayContext.RegisterKeyShortcut(
                        "aas-elem-move-end", ModifierKeys.Shift | ModifierKeys.Control, Key.End,
                        "Move current AAS element to the last position of the respective list.");

                    _displayContext.RegisterKeyShortcut(
                        "aas-elem-delete", ModifierKeys.Shift | ModifierKeys.Control, Key.Delete,
                        "Delete current AAS element in the respective list. Shift key skips dialogue.");

                    _displayContext.RegisterKeyShortcut(
                        "aas-elem-cut", ModifierKeys.Shift | ModifierKeys.Control, Key.X,
                        "Transfers current AAS element into paste buffer and deletes in respective list.");

                    _displayContext.RegisterKeyShortcut(
                        "aas-elem-copy", ModifierKeys.Shift | ModifierKeys.Control, Key.C,
                        "Copies current AAS element into paste buffer for later pasting.");

                    _displayContext.RegisterKeyShortcut(
                        "aas-elem-paste-into", ModifierKeys.Shift | ModifierKeys.Control, Key.V,
                        "Copy existing paste buffer into the child list of the current AAS element.");

                    _displayContext.RegisterKeyShortcut(
                        "aas-elem-paste-above", ModifierKeys.Shift | ModifierKeys.Control, Key.W,
                        "Copy existing paste buffer above the current AAS element in the same list.");

                    _displayContext.RegisterKeyShortcut(
                        "aas-elem-paste-below", ModifierKeys.Shift | ModifierKeys.Control, Key.Y,
                        "Copy existing paste buffer below the current AAS element in the same list.");

                }
            }

            // keep the stack
            _lastRenderedRootElement = stack;
#endif

            // return render hints
            return renderHints;
        }

        public Tuple<AnyUiDisplayContextWpf, AnyUiUIElement> GetLastRenderedRoot()
        {
            return new Tuple<AnyUiDisplayContextWpf, AnyUiUIElement>(
                _displayContext, _lastRenderedRootElement);
        }

        public void RedisplayRenderedRoot(
            AnyUiUIElement root,
            AnyUiRenderMode mode,
            bool useInnerGrid = false)
        {
            // safe
            _lastRenderedRootElement = root;

            // redisplay
            theMasterPanel.Children.Clear();
            UIElement spwpf = null;

            var allowReUse = mode == AnyUiRenderMode.StatusToUi;

            if (useInnerGrid
                && root is AnyUiStackPanel stack
                && stack?.Children != null
                && stack.Children.Count == 1
                && stack.Children[0] is AnyUiGrid grid)
            {
                spwpf = _displayContext.GetOrCreateWpfElement(grid, allowReUse: allowReUse, mode: mode);
            }
            else
            {
                spwpf = _displayContext.GetOrCreateWpfElement(root, allowReUse: allowReUse, mode: mode);
                DockPanel.SetDock(spwpf, Dock.Top);
            }

            _helper.ShowLastHighlights();
            theMasterPanel.Children.Add(spwpf);
        }

        #endregion

        public void HandleGlobalKeyDown(KeyEventArgs e, bool preview)
        {
            // access
            if (_displayContext == null)
                return;

            // save keyboad states for AnyUI
            _displayContext.ActualShiftState = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
            _displayContext.ActualControlState = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
            _displayContext.ActualAltState = (Keyboard.Modifiers & ModifierKeys.Alt) > 0;

            // investigate event itself
            if (e == null)
                return;
            var num = _displayContext?.TriggerKeyShortcut(e.Key, Keyboard.Modifiers, preview);
            if (num > 0)
                e.Handled = true;
        }

        public IEnumerable<KeyShortcutRecord> EnumerateShortcuts()
        {
            if (_displayContext?.KeyShortcuts != null)
                foreach (var sc in _displayContext.KeyShortcuts)
                    yield return sc;
        }
    }
}
