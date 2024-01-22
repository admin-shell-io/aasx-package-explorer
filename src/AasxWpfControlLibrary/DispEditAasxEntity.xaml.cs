/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static AnyUi.AnyUiDisplayContextWpf;

namespace AasxPackageExplorer
{
    public partial class DispEditAasxEntity : UserControl
    {
        private PackageCentral _packages = null;
        private ListOfVisualElementBasic _theEntities = null;
        private DispEditHelperMultiElement _helper = new DispEditHelperMultiElement();
        private AnyUiUIElement _lastRenderedRootElement = null;


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
                                _packages, dcwpf, _theEntities, _helper.editMode, _helper.hintMode,
                                flyoutProvider: dcwpf?.FlyoutProvider,
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
                var changes = true == _helper?.context?.CallUndoChanges(_lastRenderedRootElement);
                if (changes)
                    _helper.context.EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());

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

        public AnyUiStackPanel ClearDisplayDefaultStack()
        {
            theMasterPanel.Children.Clear();
            var sp = new AnyUiStackPanel();
            var spwpf = new Label();
            DockPanel.SetDock(spwpf, Dock.Top);
            theMasterPanel.Children.Add(spwpf);
            _lastRenderedRootElement = null;
            return sp;
        }

        public Panel GetMasterPanel()
        {
            return theMasterPanel;
        }

        public void SetDisplayExternalControl(System.Windows.FrameworkElement fe)
        {
            theMasterPanel.Children.Clear();
            if (fe != null)
            {
                theMasterPanel.Children.Add(fe);
                theMasterPanel.InvalidateVisual();
            }
            _lastRenderedRootElement = null;
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

        //
        // Management of loaded plugin
        //

        protected VisualElementGeneric LoadedPluginNode = null;
        protected Plugins.PluginInstance LoadedPluginInstance = null;
        protected object LoadedPluginSessionId = null;
        protected int LoadedPluginApproach = 0;

        /// <summary>
        /// Sends a dispose signal to the loaded plugin in order to properly
        /// release its resources before session might be disposed or plugin might
        /// be changed.
        /// </summary>
        public void DisposeLoadedPlugin()
        {
            // access
            if (LoadedPluginInstance == null || LoadedPluginSessionId == null || LoadedPluginApproach < 1)
            {
                LoadedPluginNode = null;
                LoadedPluginInstance = null;
                LoadedPluginSessionId = null;
                return;
            }

            // try release
            try
            {
                if (LoadedPluginApproach == 1)
                    LoadedPluginInstance.InvokeAction("clear-panel-visual-extension",
                        LoadedPluginSessionId);

                if (LoadedPluginApproach == 2)
                    LoadedPluginInstance.InvokeAction("dispose-anyui-visual-extension",
                        LoadedPluginSessionId);

                LoadedPluginNode = null;
                LoadedPluginApproach = 0;
                LoadedPluginInstance = null;
                LoadedPluginSessionId = null;
            }
            catch (Exception ex)
            {
                LogInternally.That.CompletelyIgnoredError(ex);
            }
        }

        //
        // Main function
        //

        public DisplayRenderHints DisplayOrEditVisualAasxElement(
            PackageCentral packages,
            AnyUiDisplayContextWpf displayContext,
            ListOfVisualElementBasic entities,
            bool editMode, bool hintMode = false, bool showIriMode = false, bool checkSmt = false,
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
            _helper.context = displayContext;

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

                // try to delegate to common routine
                var common = _helper.DisplayOrEditCommonEntity(
                    packages, stack, superMenu, editMode, hintMode, checkSmt, cdSortOrder, entity);

                if (common)
                {
                    // can reset plugin
                    DisposeLoadedPlugin();
                }
                else
                {
                    // some special cases
                    if (entity is VisualElementPluginExtension vepe)
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

                        // may dispose old (other plugin)
                        var pluginOnlyUpdate = true;
                        if (LoadedPluginInstance == null
                            || LoadedPluginNode != entity
                            || LoadedPluginInstance != vepe.thePlugin)
                        {
                            // invalidate, fill new
                            DisposeLoadedPlugin();
                            pluginOnlyUpdate = false;
                        }

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
                                    stack, _helper.context, AnyUiDisplayContextWpf.SessionSingletonWpf,
                                    opContext);

                                // remember
                                LoadedPluginNode = entity;
                                LoadedPluginApproach = 2;
                                LoadedPluginInstance = vepe.thePlugin;
                                LoadedPluginSessionId = AnyUiDisplayContextWpf.SessionSingletonWpf;
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

                                    // remember
                                    LoadedPluginNode = entity;
                                    LoadedPluginApproach = 1;
                                    LoadedPluginInstance = vepe.thePlugin;
                                    LoadedPluginSessionId = AnyUiDisplayContextWpf.SessionSingletonWpf;
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
            }
            else
            if (entities.Count > 1)
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
                    // accessing the WPF version of display context!
                    spwpf = displayContext.GetOrCreateWpfElement(grid);
                }
                else
                {
                    spwpf = displayContext.GetOrCreateWpfElement(stack);
                    DockPanel.SetDock(spwpf, Dock.Top);
                }
                _helper.ShowLastHighlights();

                theMasterPanel.Children.Add(spwpf);

            }

            // keep the stack
            _lastRenderedRootElement = stack;
#endif

            // return render hints
            return renderHints;
        }

        public Tuple<AnyUiDisplayContextWpf, AnyUiUIElement> GetLastRenderedRoot()
        {
            if (!(_helper.context is AnyUiDisplayContextWpf dcwpf))
                return null;

            return new Tuple<AnyUiDisplayContextWpf, AnyUiUIElement>(
                dcwpf, _lastRenderedRootElement);
        }

        public void RedisplayRenderedRoot(
            AnyUiUIElement root,
            AnyUiRenderMode mode,
            bool useInnerGrid = false,
			Dictionary<AnyUiUIElement, bool> updateElemsOnly = null)
        {
            // safe
            _lastRenderedRootElement = root;
            if (!(_helper?.context is AnyUiDisplayContextWpf dcwpf))
                return;

            // no plugin
            // DisposeLoadedPlugin();

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
                spwpf = dcwpf.GetOrCreateWpfElement(grid, allowReUse: allowReUse, mode: mode,
                    updateElemsOnly: updateElemsOnly);
            }
            else
            {
                spwpf = dcwpf.GetOrCreateWpfElement(root, allowReUse: allowReUse, mode: mode,
                    updateElemsOnly: updateElemsOnly);
                DockPanel.SetDock(spwpf, Dock.Top);
            }

            _helper.ShowLastHighlights();
            theMasterPanel.Children.Add(spwpf);
        }

        #endregion

        public void HandleGlobalKeyDown(KeyEventArgs e, bool preview)
        {
            // access
            if (!(_helper?.context is AnyUiDisplayContextWpf dcwpf))
                return;

            // save keyboad states for AnyUI
            _helper.context.ActualShiftState = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
            _helper.context.ActualControlState = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
            _helper.context.ActualAltState = (Keyboard.Modifiers & ModifierKeys.Alt) > 0;

            // investigate event itself
            if (e == null)
                return;
            var num = dcwpf.TriggerKeyShortcut(e.Key, Keyboard.Modifiers, preview);
            if (num > 0)
                e.Handled = true;
        }

        public IEnumerable<KeyShortcutRecord> EnumerateShortcuts()
        {
            // access
            if (!(_helper?.context is AnyUiDisplayContextWpf dcwpf))
                yield break;

            if (dcwpf.KeyShortcuts != null)
                foreach (var sc in dcwpf.KeyShortcuts)
                    yield return sc;
        }
    }
}
