/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxIntegrationBaseGdi;
using AasxPredefinedConcepts;
using AdminShellNS;
using AnyUi;
using Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;

// ReSharper disable InconsistentlySynchronizedField
// ReSharper disable AccessToModifiedClosure

namespace AasxPluginAID
{
    public class AIDAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private Aas.Submodel _submodel = null;
        private PluginEventStack _eventStack = null;
        private PluginSessionBase _session = null;
        private AnyUiStackPanel _panel = null;
        private AnyUiContextPlusDialogs _displayContext = null;
        private PluginOperationContextBase _opContext = null;
        private AasxPluginBase _plugin = null;

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        private String _selectedProtocolType = "HTTP";

        private List<InterfaceEntity> _renderedInterfaces = new List<InterfaceEntity>();
        private List<InterfaceEntity> theDocEntitiesToPreview = new List<InterfaceEntity>();

        private System.Timers.Timer _dispatcherTimer = null;

        // members for form editing

        protected AnyUiRenderForm _formDoc = null;

        protected static int InstCounter = 1;

        protected string CurrInst = "";

        #endregion

        #region Cache for already generated Images
        //========================================

#if USE_WPF
        private static Dictionary<string, BitmapImage> referableHashToCachedBitmap =
            new Dictionary<string, BitmapImage>();
#else
        private static Dictionary<string, AnyUiBitmapInfo> referableHashToCachedBitmap =
            new Dictionary<string, AnyUiBitmapInfo>();
#endif
        #endregion

        #region Constructors, as for WPF control
        //=============

        public AIDAnyUiControl()
        {
#if JUST_FOR_INFO
            // Timer for loading
            //System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            //dispatcherTimer.Tick += DispatcherTimer_Tick;
            //dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            //dispatcherTimer.Start();

            //var _timer = new System.Threading.Timer((e) =>
            //{
            //    DispatcherTimer_Tick(null, null);
            //}, null, TimeSpan.FromMilliseconds(2000), TimeSpan.FromMilliseconds(1000));

            // see: https://stackoverflow.com/questions/5143599/detecting-whether-on-ui-thread-in-wpf-and-winforms
            //var dispatcher = System.Windows.Threading.Dispatcher.FromThread(System.Threading.Thread.CurrentThread);
            //if (dispatcher != null)
            //{
            //    var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            //    dispatcherTimer.Tick += DispatcherTimer_Tick;
            //    dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            //    dispatcherTimer.Start();
            //}
            //else
            //{
            //    // Note: this timer shall work for all sorts of applications?
            //    // see: https://stackoverflow.com/questions/21041299/c-sharp-dispatchertimer-in-dll-application-never-triggered
            //    var _timer2 = new System.Timers.Timer(1000);
            //    _timer2.Elapsed += DispatcherTimer_Tick;
            //    _timer2.Enabled = true;
            //    _timer2.Start();
            //}
#endif
#if USE_WPF
            AnyUiHelper.CreatePluginTimer(1000, DispatcherTimer_Tick);
#else
            // Note: this timer shall work for all sorts of applications?
            // see: https://stackoverflow.com/questions/21041299/c-sharp-dispatchertimer-in-dll-application-never-triggered
            _dispatcherTimer = new System.Timers.Timer(1000);
            _dispatcherTimer.Elapsed += DispatcherTimer_Tick;
            _dispatcherTimer.Enabled = true;
            _dispatcherTimer.Start();
#endif

            CurrInst = "" + InstCounter;
            InstCounter++;

        }

        public void Dispose()
        {
            if (_dispatcherTimer != null)
            {
                _dispatcherTimer.Stop();
                _dispatcherTimer.Dispose();
            }
        }

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            Aas.Submodel theSubmodel,
            PluginEventStack eventStack,
            PluginSessionBase session,
            AnyUiStackPanel panel,
            PluginOperationContextBase opContext,
            AnyUiContextPlusDialogs cdp,
            AasxPluginBase plugin)
        {
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _eventStack = eventStack;
            _session = session;
            _panel = panel;
            _opContext = opContext;
            _displayContext = cdp;
            _plugin = plugin;

            // no form, yet
            _formDoc = null;

            // fill given panel
            RenderFullShelf(_panel, _uitk);
        }

        public static AIDAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            PluginEventStack eventStack,
            PluginSessionBase session,
            object opanel,
            PluginOperationContextBase opContext,
            AnyUiContextPlusDialogs cdp,
            AasxPluginBase plugin)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as Aas.Submodel;
            var panel = opanel as AnyUiStackPanel;
            if (package == null || sm == null || panel == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // do NOT create WPF controls
            FormInstanceBase.createSubControls = false;

            // factory this object
            var shelfCntl = new AIDAnyUiControl();
            shelfCntl.Start(log, package, sm, eventStack, session, panel, opContext, cdp, plugin);

            // return shelf
            return shelfCntl;
        }

        #endregion

        #region Display Submodel
        //=============

        private void RenderFullShelf(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk)
        {
            // test trivial access
            if ( _submodel?.SemanticId == null)
                return;

            var found = false;
            if (_submodel.SemanticId.GetAsExactlyOneKey().Value == IDTAAid.Static.AID_Submodel.GetAsExactlyOneKey().Value)
                found = true;

            if (!found)
                return;

            // set usage info
            string useinf = null;

            // what defaultLanguage
            string defaultLang = "en";

            // make new list box items
            _renderedInterfaces = new List<InterfaceEntity>();
            // ReSharper disable ExpressionIsAlwaysNull
            _renderedInterfaces = ListOfInterfaceEntity.ParseSubmodelAID(
                    _package, _submodel, "HTTP");
            // bring it to the panel            
            RenderPanelOutside(view, uitk, useinf, defaultLang, _renderedInterfaces);
        }

        protected double _lastScrollPosition = 0.0;

        protected void RenderPanelOutside(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            string usageInfo,
            string defaultLanguage,
            List<InterfaceEntity> its,
            double? initialScrollPos = null)
        {
            var outer = view.Add(uitk.AddSmallGrid(rows: 4, cols: 1, colWidths: new[] { "*" }));
            outer.RowDefinitions[2].Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);

            // at top, make buttons for the general form
            var header = uitk.AddSmallGridTo(outer, 0, 0, rows: 2, cols: 5,
                    colWidths: new[] { "*", "#", "#", "#", "#" });

            header.Margin = new AnyUiThickness(0);
            header.Background = AnyUiBrushes.LightBlue;

            //
            // Blue bar
            //

            uitk.AddSmallBasicLabelTo(header, 0, 0, margin: new AnyUiThickness(8, 6, 0, 6),
                foreground: AnyUiBrushes.DarkBlue,
                fontSize: 1.5f,
                setBold: true,
                content: $"AssetInterfaceDescriptions");

            if (_opContext?.IsDisplayModeEditOrAdd == true)
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(header, 0, 2,
                        margin: new AnyUiThickness(2), setHeight: 21,
                        padding: new AnyUiThickness(2, 0, 2, 0),
                        content: "Add Interface Description .."),
                    setValueAsync: (o) => ButtonTabPanels_Click("ButtonAddDocument"));


            List<object> protocolTypes = new List<object> { "HTTP", "MQTT", "MODBUS" };
            // controls
            var controls = uitk.AddSmallWrapPanelTo(outer, 1, 0,
                background: AnyUiBrushes.MiddleGray, margin: new AnyUiThickness(0, 0, 0, 2));

            AnyUiButton importButton = null;

            importButton = AnyUiUIElement.RegisterControl(controls.Add(new AnyUiButton()
            {
                Margin = new AnyUiThickness(2),
                MinHeight = 21,
                Content = "Import Interface Description .."
            }), (o) =>
            {
                ButtonTabPanels_Click("ButtonImportDocument");
                return new AnyUiLambdaActionNone();
            });

            //
            // Scroll area
            //

            // small spacer
            outer.RowDefinitions[2] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 2, 0,
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // add the body, a scroll viewer
            outer.RowDefinitions[3] = new AnyUiRowDefinition(1.0, AnyUiGridUnitType.Star);
            var scroll = AnyUiUIElement.RegisterControl(
                uitk.AddSmallScrollViewerTo(outer, 3, 0,
                    horizontalScrollBarVisibility: AnyUiScrollBarVisibility.Disabled,
                    verticalScrollBarVisibility: AnyUiScrollBarVisibility.Visible,
                    flattenForTarget: AnyUiTargetPlatform.Browser, initialScrollPosition: initialScrollPos),
                (o) =>
                {
                    if (o is Tuple<double, double> positions)
                    {
                        _lastScrollPosition = positions.Item2;
                    }
                    return new AnyUiLambdaActionNone();
                });

            // need a stack panel to add inside
            var inner = new AnyUiStackPanel() { Orientation = AnyUiOrientation.Vertical };
            scroll.Content = inner;

            // render the innerts of the scroll viewer
            inner.Background = AnyUiBrushes.LightGray;

            if (its != null)
                foreach (var de in its)
                {
                    var rde = RenderAnyUiDocumentEntity(uitk, de);
                    if (rde != null)
                        inner.Add(rde);
                }

            // post process
            if (its != null)
                foreach (var ent in its)
                {
                    // if a preview file exists, try load directly, but not interfere
                    // we delayed load logic, as these images might get more detailed
                    if (ent.PreviewFile?.Path?.HasContent() == true)
                    {
                        var inputFn = ent.PreviewFile.Path;

                        try
                        {
                            // from package?
                            if (CheckIfPackageFile(inputFn))
                                inputFn = _package?.MakePackageFileAvailableAsTempFile(ent.PreviewFile.Path);

                            ent.LoadImageFromPath(inputFn);
                        }
                        catch (Exception ex)
                        {
                            // do not show any error message, as it might appear
                            // frequently
                            LogInternally.That.SilentlyIgnoredError(ex);
                        }
                    }

                    // delayed load logic
                    // can already put a generated image into the viewbox?
                    if (referableHashToCachedBitmap != null &&
                        referableHashToCachedBitmap.ContainsKey(ent.ReferableHash))
                    {
#if USE_WPF
                        ent.ImgContainerAnyUi.BitmapInfo = AnyUiHelper.CreateAnyUiBitmapInfo(
                            referableHashToCachedBitmap[ent.ReferableHash]);
#else
                        ent.ImgContainerAnyUi.BitmapInfo = referableHashToCachedBitmap[ent.ReferableHash];
#endif
                    }
                    else
                    {
                        // trigger generation of image

                        // check if already in list
                        InterfaceEntity foundDe = null;
                        foreach (var de in theDocEntitiesToPreview)
                            if (ent.ReferableHash == de.ReferableHash)
                                foundDe = de;

                        lock (theDocEntitiesToPreview)
                        {
                            if (foundDe != null)
                                theDocEntitiesToPreview.Remove(foundDe);
                            theDocEntitiesToPreview.Add(ent);
                        }
                    }

                    // attach events and add
                    ent.DoubleClick += DocumentEntity_DoubleClick;
                    ent.MenuClick += DocumentEntity_MenuClick;
                    ent.DragStart += DocumentEntity_DragStart;
                }
        }

        public AnyUiFrameworkElement RenderAnyUiDocumentEntity(
            AnyUiSmallWidgetToolkit uitk, InterfaceEntity de)
        {
            // access
            if (de == null)
                return new AnyUiStackPanel();

            // make a outer grid
            var outerG = uitk.AddSmallGrid(1, 1,
                colWidths: new[] { "*" }, rowHeights: new[] { "*" },
                margin: new AnyUiThickness(0));

            // make background border
            for (int i = 2; i > 0; i--)
                uitk.Set(
                    uitk.AddSmallBorderTo(outerG, 0, 0,
                        margin: new AnyUiThickness(3 + 2 * i, 3 + 2 * i, 3 + 4 - 2 * i, 3 + 4 - 2 * i),
                        background: AnyUiBrushes.White,
                        borderBrush: AnyUiBrushes.Black,
                        borderThickness: new AnyUiThickness(1.0)),
                    skipForTarget: AnyUiTargetPlatform.Browser);

            // make the border, which will get content
            var border = uitk.AddSmallBorderTo(outerG, 0, 0,
                margin: new AnyUiThickness(3, 3, 3 + 4, 3 + 4),
                background: AnyUiBrushes.White,
                borderBrush: AnyUiBrushes.Black,
                borderThickness: new AnyUiThickness(1.0));

            // the border emits double clicks
            border.EmitEvent = AnyUiEventMask.LeftDouble;
            border.setValueLambda = (o) =>
            {
                if (o is AnyUiEventData ed
                    && ed.Mask == AnyUiEventMask.LeftDouble
                    && ed.ClickCount == 2)
                {
                    de.RaiseDoubleClick();
                }
                return new AnyUiLambdaActionNone();
            };

            // make a grid
            var g = uitk.AddSmallGrid(4, 4,
                colWidths: new[] { "*", "5:", "*", "22:" },
                rowHeights: new[] { "*", "5:", "*", "25:" },
                margin: new AnyUiThickness(1),
                background: AnyUiBrushes.White);
            border.Child = g;

            uitk.AddSmallBasicLabelTo(g, 0, 0,
                textIsSelectable: false,
                margin: new AnyUiThickness(2),
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                fontSize: 1.0f,
                content: "Title : " + $"{de.Title}");


            uitk.AddSmallBasicLabelTo(g, 0, 2,
                textIsSelectable: false,
                margin: new AnyUiThickness(2),
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                fontSize: 1.0f,
                content: "Created : " + $"{de.Created}");

            uitk.AddSmallBasicLabelTo(g, 2, 0,
                textIsSelectable: false,
                margin: new AnyUiThickness(2),
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                fontSize: 1.0f,
                content: "contentType : " + $"{de.ContentType}");


            uitk.AddSmallBasicLabelTo(g, 2, 2,
                textIsSelectable: false,
                margin: new AnyUiThickness(2),
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                fontSize: 1.0f,
                content: "ProtocolType : " + $"{de.ProtocolType}");

            var hds = new List<string>();
            if (_opContext?.IsDisplayModeEditOrAdd == true)
            {
                hds.AddRange(new[] { "\u270e", "Edit Interface Data" });
                hds.AddRange(new[] { "\u2702", "Delete Interface Data" });
            }
            else
                hds.AddRange(new[] { "\u270e", "View Interface Data" });

            hds.AddRange(new[] { "\U0001F4BE", "Save to thing description file .." });

            // context menu
            uitk.AddSmallContextMenuItemTo(g, 3, 3,
                    "\u22ee",
                    hds.ToArray(),
                    margin: new AnyUiThickness(2, 2, 2, 2),
                    padding: new AnyUiThickness(5, 0, 5, 0),
                    fontWeight: AnyUiFontWeight.Bold,
                    menuItemLambda: null,
                    menuItemLambdaAsync: async (o) =>
                    {
                        if (o is int ti && ti >= 0 && ti < hds.Count)
                            // awkyard, but for compatibility to WPF version
                            await de?.RaiseMenuClick(hds[2 * ti + 1], null);
                        return new AnyUiLambdaActionNone();
                    });

            // ok
            return outerG;
        }

        #endregion

        #region Create entity
        //=====================

        protected AnyUiPanelEntity _formEntity = null;

        protected class AnyUiPanelEntity
        {
            public string IdShort = "";

            public void RenderAnyUi(
                AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
                Func<object, Task<AnyUiLambdaActionBase>> lambdaCancel = null,
                Func<object, Task<AnyUiLambdaActionBase>> lambdaAdd = null)
            {
                //
                // make an outer grid, very simple grid of rows: header, spacer, body
                //

                var outer = view.Add(uitk.AddSmallGrid(rows: 3, cols: 1, colWidths: new[] { "*" }));
                outer.RowDefinitions[2].Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);

                // at top, make buttons for the general form
                var header = uitk.AddSmallGridTo(outer, 0, 0, 1, cols: 5, colWidths: new[] { "*", "#", "#", "#", "#" });

                header.Margin = new AnyUiThickness(0);
                header.Background = AnyUiBrushes.LightBlue;

                //
                // Blue bar
                //

                uitk.AddSmallBasicLabelTo(header, 0, 0, margin: new AnyUiThickness(8, 6, 0, 6),
                    foreground: AnyUiBrushes.DarkBlue,
                    fontSize: 1.5f,
                    setBold: true,
                    content: $"Entity");

                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(header, 0, 1,
                        margin: new AnyUiThickness(2), setHeight: 21,
                        padding: new AnyUiThickness(4, 0, 4, 0),
                        content: "Cancel"),
                    setValueAsync: lambdaCancel);

                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(header, 0, 2,
                        margin: new AnyUiThickness(2), setHeight: 21,
                        padding: new AnyUiThickness(4, 0, 4, 0),
                        content: "Add"),
                    setValueAsync: lambdaAdd);

                // small spacer
                outer.RowDefinitions[1] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
                uitk.AddSmallBasicLabelTo(outer, 1, 0,
                    fontSize: 0.3f,
                    verticalAlignment: AnyUiVerticalAlignment.Top,
                    content: "", background: AnyUiBrushes.White);

                //
                // Grid with entries
                //

                var body = uitk.AddSmallGridTo(outer, 2, 0, rows: 1, cols: 2,
                    colWidths: new[] { "#", "*" },
                    background: AnyUiBrushes.LightGray);

                uitk.AddSmallBasicLabelTo(body, 0, 0,
                    verticalAlignment: AnyUiVerticalAlignment.Center,
                    verticalContentAlignment: AnyUiVerticalAlignment.Center,
                    margin: new AnyUiThickness(0, 0, 4, 0),
                    textWrapping: AnyUiTextWrapping.NoWrap,
                    content: "idShort:");

                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallTextBoxTo(body, 0, 1,
                        margin: new AnyUiThickness(2, 10, 2, 10),
                        text: "" + IdShort),
                    (o) =>
                    {
                        if (o is string os)
                            IdShort = os;
                        return new AnyUiLambdaActionNone();
                    });
            }

        }

        #endregion

        #region Event handling
        //=============

        private Action<AasxPluginEventReturnBase> _menuSubscribeForNextEventReturn = null;

        protected void PushUpdateEvent(AnyUiRenderMode mode = AnyUiRenderMode.All)
        {
            // bring it to the panel by redrawing the plugin
            _eventStack?.PushEvent(new AasxPluginEventReturnUpdateAnyUi()
            {
                // get the always currentplugin name
                PluginName = "AasxPluginAID",
                Session = _session,
                Mode = mode,
                UseInnerGrid = true
            });
        }

        protected void PushUpdatenRedrawEvent(AnyUiRenderMode mode = AnyUiRenderMode.All)
        {
            // bring it to the panel by redrawing the plugin
            _eventStack?.PushEvent(new AasxPluginEventReturnUpdateAnyUi()
            {
                // get the always currentplugin name
                PluginName = "AasxPluginAID",
                Session = _session,
                Mode = mode,
                UseInnerGrid = true
            });
            
            _eventStack?.PushEvent(new AasxPluginResultEventRedrawAllElements()
                {
                    Session = _session,
                });
        }

        public void HandleEventReturn(AasxPluginEventReturnBase evtReturn)
        {
            // demands from shelf
            if (_menuSubscribeForNextEventReturn != null)
            {
                // delete first
                var tempLambda = _menuSubscribeForNextEventReturn;
                _menuSubscribeForNextEventReturn = null;

                // execute
                tempLambda(evtReturn);

                // finish
                return;
            }

            // check, if a form is active
            if (_formDoc != null)
            {
                _formDoc.HandleEventReturn(evtReturn);
            }
        }

        #endregion

        #region Update
        //=============

        public void Update(params object[] args)
        {
            // check args
            if (args == null || args.Length < 2
                || !(args[0] is AnyUiStackPanel newPanel)
                || !(args[1] is AnyUiContextPlusDialogs newCdp))
                return;

            // ok, re-assign panel and re-display
            _displayContext = newCdp;
            _panel = newPanel;
            _panel.Children.Clear();

            // multiple different views can be renders
            if (_formEntity != null)
            {
                _formEntity.RenderAnyUi(_panel, _uitk,
                    lambdaCancel: (o) => ButtonTabPanels_Click("ButtonCancelEntity"),
                    lambdaAdd: (o) => ButtonTabPanels_Click("ButtonAddEntity"));
            }
            else
            if (_formDoc != null)
            {
                if (_opContext?.IsDisplayModeEditOrAdd == true)
                {
                    _formDoc.RenderFormInst(_panel, _uitk, _opContext,
                        setLastScrollPos: true,
                        lambdaFixCds: (o) => ButtonTabPanels_Click("ButtonFixCDs"),
                        lambdaCancel: (o) => ButtonTabPanels_Click("ButtonCancel"),
                        lambdaOK: (o) => ButtonTabPanels_Click("ButtonUpdate"));
                }
                else
                {
                    _formDoc.RenderFormInst(_panel, _uitk, _opContext,
                        setLastScrollPos: true,
                        lambdaCancel: (o) => ButtonTabPanels_Click("ButtonCancel"));
                }
            }
            else
            {
                // the default: the full shelf
                RenderFullShelf(_panel, _uitk);
            }
        }

        #endregion

        #region Callbacks
        //===============

        private List<Aas.ISubmodelElement> _updateSourceElements = null;

        private async Task GetFormDescForV12(
            List<Aas.ISubmodelElement> sourceElems,
            Action<FormDescSubmodelElementCollection> lambda)
        {
            // ask the plugin generic forms for information via event stack
            _eventStack?.PushEvent(new AasxIntegrationBase.AasxPluginResultEventInvokeOtherPlugin()
            {
                Session = _session,
                PluginName = "AasxPluginGenericForms",
                Action = "find-form-desc",
                UseAsync = false,
                Args = new object[] {
                    AasxPredefinedConcepts.IdtaHandoverDocumentationV12.Static
                        .SM_HandoverDocumentation.GetSemanticRef() }
            });

            // .. and receive incoming event ..
            _menuSubscribeForNextEventReturn = (revt) =>
            {
                if (revt is AasxPluginEventReturnInvokeOther rinv
                    && rinv.ResultData is AasxPluginResultBaseObject rbo
                    && rbo.obj is List<FormDescBase> fdb
                    && fdb.Count == 1
                    && fdb[0] is FormDescSubmodel descSm)
                {
                    _updateSourceElements = sourceElems;

                    // need to identify the form for single document BELOW the Submodel
                    FormDescSubmodelElementCollection descSmc = null;
                    if (descSm.SubmodelElements != null)
                        foreach (var desc in descSm.SubmodelElements)
                            if (desc is FormDescSubmodelElementCollection desc2
                                && AasxPredefinedConcepts.IdtaHandoverDocumentationV12.Static
                                    .CD_Document.GetReference()?.MatchesExactlyOneKey(
                                        desc2?.KeySemanticId, matchMode: MatchMode.Relaxed) == true)
                            {
                                descSmc = desc2;
                            }

                    if (descSmc == null)
                        return;

                    lambda?.Invoke(descSmc);
                }
            };
        }

        private async Task DocumentEntity_MenuClick(InterfaceEntity e, string menuItemHeader, object tag)
        {
            // first check
            if (e == null || menuItemHeader == null)
                return;

            // what to do?
            if (tag == null
                && (menuItemHeader == "Edit Interface Data" || menuItemHeader == "View Interface Data"))
            {
                // lambda
                Action<FormDescSubmodelElementCollection> lambda = (desc) =>
                {
                    var fi = new FormInstanceSubmodelElementCollection(null, desc);
                    fi.PresetInstancesBasedOnSource(_updateSourceElements);
                    fi.outerEventStack = _eventStack;
                    fi.OuterPluginName = _plugin?.GetPluginName();
                    fi.OuterPluginSession = _session;

                    // initialize form
                    _formDoc = new AnyUiRenderForm(
                        fi,
                        updateMode: true);
                    PushUpdateEvent();
                };

                var desc = AIDSemanticConfig.CreateAssetInterfaceDescription();
                _updateSourceElements = e.SourceElementsDocument;

                lambda(desc);

                // OK
                return;
            }

            if (tag == null && menuItemHeader == "Delete Interface Data" && _submodel?.SubmodelElements != null
                && _opContext?.IsDisplayModeEditOrAdd == true)
            {
                // the source elements need to match a Document
                var semConf = IDTAAid.Static.AID_Interface;
                var found = false;
                foreach (var smcDoc in
                    _submodel.SubmodelElements.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                        semConf, MatchMode.Relaxed))
                    if (smcDoc?.Value == e.SourceElementsDocument)
                    {
                        // ask back via display context
                        if (AnyUiMessageBoxResult.Cancel == await _displayContext?.MessageBoxFlyoutShowAsync(
                                    "Delete Interface description? This cannot be reverted!",
                                    "AssetInterfaceDescriptions",
                                    AnyUiMessageBoxButton.OKCancel,
                                    AnyUiMessageBoxImage.Question))
                            return;

                        // do it
                        try
                        {
                            _submodel.SubmodelElements.Remove(smcDoc);
                            e.SourceElementsDocument.Remove(smcDoc);

                            // re-display also in Explorer
                            _eventStack?.PushEvent(new AasxPluginResultEventRedrawAllElements()
                            { Session = _session });

                            // log
                            _log?.Info("Deleted Interface description.");
                        }
                        catch (Exception ex)
                        {
                            _log?.Error(ex, "while saveing digital file to user specified loacation");
                        }

                        // OK
                        return;

                        // ReSharper enable PossibleMultipleEnumeration
                    }

                if (!found)
                    _log?.Error("Document element was not found properly!");
            }

            // show digital file
            if (tag == null && menuItemHeader == "View file")
                DocumentEntity_DisplaySaveFileAsync(e, true, false);

            // save digital file?
            if (tag == null && menuItemHeader == "Save to thing description file ..")
            {
                DocumentEntity_DisplaySaveFileAsync(e, true, true);
            }

            // show digital file
            if (tag == null && menuItemHeader == "Make preview permanent"
                && e?.ReferableHash != null
                && referableHashToCachedBitmap.ContainsKey(e?.ReferableHash)
                && e.AddPreviewFile != null)
            {
                // make sure its empty
                if (e.PreviewFile != null)
                {
                    _log?.Error("For this entity, the PreviewFile seems to be already defined. Aborting.");
                    return;
                }

                if (AnyUiMessageBoxResult.Cancel == await _displayContext?.MessageBoxFlyoutShowAsync(
                    "Add a PreviewFile information to the current DocumentEntity?",
                    "AssetInterfaceDescriptions",
                    AnyUiMessageBoxButton.OKCancel,
                    AnyUiMessageBoxImage.Question))
                    return;

                // try turn the bitmap into a physical file
                try
                {
                    var bi = referableHashToCachedBitmap[e.ReferableHash];

                    // go the easy route first
                    if (bi.PngData != null)
                    {
                        // create a writeable temp png file name
                        var tmpfn = System.IO.Path.GetTempFileName();
                        var pngfn = tmpfn.Replace(".tmp", ".png");

                        // write it
                        System.IO.File.WriteAllBytes(pngfn, bi.PngData);

                        // prepare upload data
                        var ptd = "/aasx/";
                        var ptfn = System.IO.Path.GetFileName(pngfn);
                        _package.PrepareSupplementaryFileParameters(ref ptd, ref ptfn);

                        // get content type
                        var mimeType = AdminShellPackageEnv.GuessMimeType(ptfn);

                        // call "add"
                        var targetPath = _package.AddSupplementaryFileToStore(
                            pngfn, ptd, ptfn,
                            embedAsThumb: false, useMimeType: mimeType);

                        if (targetPath == null)
                        {
                            _log?.Error(
                                $"Error adding file {pngfn} to package");
                        }
                        else
                        {
                            // add
                            _log?.Info(StoredPrint.Color.Blue,
                                $"Added {ptfn} to pending package items. A save-operation is required.");

                            var res = e.AddPreviewFile(
                                e,
                                path: System.IO.Path.Combine(ptd, ptfn),
                                contentType: mimeType);

                            // re-display also in Explorer
                            _eventStack?.PushEvent(new AasxPluginResultEventRedrawAllElements()
                            { Session = _session });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log?.Error(ex, "Creating physical file for PreviewFile");
                }
            }
        }

        private async Task DocumentEntity_DisplaySaveFileAsync(InterfaceEntity e, bool display, bool save)
        {
            try
            {
                if (e.SourceElementsDocument.Count == 0)
                {
                    return;
                }
                Aas.ISubmodelElementCollection dmc = e.SourceElementsDocument[0].Parent as Aas.ISubmodelElementCollection;
                var uc = await _displayContext.MenuSelectSaveFilenameAsync(
                                        ticket: null, argName: null,
                                        caption: "Select preset JSON file to save ..",
                                        proposeFn: "new.json",
                                        filter: "Preset JSON file (*.json)|*.json|All files (*.*)|*.*",
                                        msg: "Not found");



                JObject tdJson = AIDTDExport.ExportInterfacetoTDJson(dmc);
                using (var s = new StreamWriter(uc.TargetFileName as string))
                {
                    string output = Newtonsoft.Json.JsonConvert.SerializeObject(tdJson,
                        Newtonsoft.Json.Formatting.Indented);
                    s.WriteLine(output);
                }
            }
            catch (Exception ex)
            {
                _log?.Error(ex, "when executing file action");
                return;
            }
        }

        private void DocumentEntity_DoubleClick(InterfaceEntity e)
        {
            DocumentEntity_DisplaySaveFileAsync(e, true, false);
        }

        protected bool _inDragStart = false;

        private void DocumentEntity_DragStart(InterfaceEntity e)
        {
            // first check
            if (e == null || e.DigitalFile?.Path == null || e.DigitalFile.Path.Trim().Length < 1 || _inDragStart)
            {
                _inDragStart = false;
                return;
            }

            // lock
            _inDragStart = true;

            // hastily prepare data
            try
            {
                // make a file available
                var inputFn = e.DigitalFile.Path;

                // check if it an address in the package only
                if (!inputFn.Trim().StartsWith("/"))
                {
                    _log?.Error("Can only drag package local files!");
                    _inDragStart = false;
                    return;
                }

                // now should make available
                if (CheckIfPackageFile(inputFn))
                    inputFn = _package?.MakePackageFileAvailableAsTempFile(e.DigitalFile.Path, keepFilename: true);

                if (!inputFn.HasContent())
                {
                    _log?.Error("Error making digital file available. Aborting!");
                    return;
                }

                // start the operation
                e.ImgContainerAnyUi?.DisplayData?.DoDragDropFiles(e.ImgContainerAnyUi, new[] { inputFn });
            }
            catch (Exception ex)
            {
                _log?.Error(ex, "when initiate file dragging");
                _inDragStart = false;
                return;
            }

            // unlock
            _inDragStart = false;
        }

        private async Task<AnyUiLambdaActionBase> ButtonTabPanels_Click(string cmd, string args = null)
        {
            if (cmd == "ButtonCancel")
            {
                // re-display (tree & panel)
                return new AnyUiLambdaActionRedrawAllElementsBase() { RedrawCurrentEntity = true };
            }

            if (cmd == "ButtonUpdate")
            {
                // add
                if (this._formDoc != null
                    && _package != null
                    && _submodel != null)
                {
                    // on this level of the hierarchy, shall a new SMEC be created or shall
                    // the existing source of elements be used?
                    List<Aas.ISubmodelElement> currentElements = null;
                    if (_formDoc.InUpdateMode)
                    {
                        currentElements = _updateSourceElements;
                    }
                    else
                    {
                        currentElements = new List<Aas.ISubmodelElement>();
                    }

                    // create a sequence of SMEs
                    try
                    {
                        if (_formDoc.FormInstance is FormInstanceSubmodelElementCollection fismec)
                            fismec.AddOrUpdateDifferentElementsToCollection(
                                currentElements, _package, addFilesToPackage: true);

                        _log?.Info("Document elements updated. Do not forget to save, if necessary!");
                    }
                    catch (Exception ex)
                    {
                        _log?.Error(ex, "when adding Document");
                    }
                    // the InstSubmodel, which started the process, should have a "fresh" SMEC available
                    // make it unique in the Documentens Submodel
                    var newSmc = (_formDoc.FormInstance as FormInstanceSubmodelElementCollection)?.sme
                            as Aas.SubmodelElementCollection;

                    // if not update, put them into the Document's Submodel
                    if (!_formDoc.InUpdateMode && currentElements != null && newSmc != null)
                    {
                        // make newSmc unique in the cotext of the Submodel
                        FormInstanceHelper.MakeIdShortUnique(_submodel.SubmodelElements, newSmc);

                        // add the elements
                        newSmc.Value = currentElements;

                        // add the whole SMC
                        _submodel.Add(newSmc);
                    }
                }
                else
                {
                    _log?.Error("Preconditions for update entities from Document not met.");
                }
                // re-display (tree & panel)
                return new AnyUiLambdaActionRedrawAllElementsBase() { NextFocus = _submodel };
            }

            if (cmd == "ButtonAddDocument")
            {
                Action<FormDescSubmodelElementCollection> lambda = (desc) =>
                {
                    var fi = new FormInstanceSubmodelElementCollection(null, desc);
                    fi.outerEventStack = _eventStack;
                    fi.OuterPluginName = _plugin?.GetPluginName();
                    fi.OuterPluginSession = _session;

                    // initialize form
                    _formDoc = new AnyUiRenderForm(
                        fi,
                        updateMode: false);

                    // bring it to the panel by redrawing the plugin
                    PushUpdateEvent();
                };

                var desc = AIDSemanticConfig.CreateAssetInterfaceDescription();
                _updateSourceElements = null;
                lambda(desc);

                return new AnyUiLambdaActionNone();
            }

            if (cmd == "ButtonImportDocument")
            {
                var uc = await _displayContext.MenuSelectOpenFilenameAsync(
                                        ticket: null, argName: null,
                                        caption: "Select JSON file to import..",
                                        proposeFn: null, filter: "Preset JSON file (*.json)|*.json|All files (*.*)|*.*",
                                        msg: "Not found");

                
                string text = System.IO.File.ReadAllText(uc.TargetFileName);
                JObject tdJObject;
                using (var tdStringReader = new StringReader(text))
                using (var jsonTextReader = new JsonTextReader(tdStringReader)
                { DateParseHandling = DateParseHandling.None })
                {
                    tdJObject = JObject.FromObject(JToken.ReadFrom(jsonTextReader));
                }
                var targetPath = "/aasx/files/";
                var onlyFn = System.IO.Path.GetFileNameWithoutExtension(uc.TargetFileName);
                var onlyExt = System.IO.Path.GetExtension(uc.TargetFileName);
                var salt = Guid.NewGuid().ToString().Substring(0, 8);
                var targetFn = String.Format("{0}_{1}{2}", onlyFn, salt, onlyExt);

                var desc = AIDTDImport.CreateAssetInterfaceDescriptionFromTd(tdJObject, targetPath + "/"+targetFn);
                _submodel.Add(desc);
                PushUpdatenRedrawEvent();
                return new AnyUiLambdaActionRedrawAllElementsBase() { NextFocus = _submodel };
            }

            if (cmd == "ButtonCancelEntity")
            {
                // reset view
                _formEntity = null;
                _formDoc = null;

                // redisplay
                PushUpdateEvent();
                return new AnyUiLambdaActionNone();
            }

            return new AnyUiLambdaActionNone();
        }

        public static InterfaceEntity ParseSCInterfaceDescription(Aas.SubmodelElementCollection smcDoc,
                                                                  string referableHash)
        {
            var defs1 = AasxPredefinedConcepts.IDTAAid.Static;

            var title =
                "" +
                   smcDoc.Value.FindFirstSemanticIdAs<Aas.Property>(defs1.AID_title,
                MatchMode.Relaxed)?.Value;

            var Created =
                "" +
                smcDoc.Value.FindFirstSemanticIdAs<Aas.Property>(
                    defs1.AID_created, MatchMode.Relaxed)?
                    .Value;

            var ContentType =
                "" +
                smcDoc.Value.FindFirstSemanticIdAs<Aas.Property>(
                    defs1.AID_contentType, MatchMode.Relaxed)?
                    .Value;

            var ProtocolType = "HTTP";
            var ent = new InterfaceEntity(title, Created, ContentType, ProtocolType);

            // add
            ent.SourceElementsDocument = smcDoc.Value;
            ent.ReferableHash = referableHash;

            return ent;
        }
        #endregion

        #region Timer
        //===========

        private object mutexDocEntitiesInPreview = new object();
        private int numDocEntitiesInPreview = 0;
        private const int maxDocEntitiesInPreview = 3;

        private bool _inDispatcherTimer = false;

        protected int _dispatcherNumException = 0;

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            // access
            if (_renderedInterfaces == null || theDocEntitiesToPreview == null || _inDispatcherTimer)
                if (_renderedInterfaces == null || theDocEntitiesToPreview == null || _inDispatcherTimer)
                    return;

            _inDispatcherTimer = true;
            var updateDisplay = false;

            // each tick check for one image, if a preview shall be done
            if (theDocEntitiesToPreview != null && theDocEntitiesToPreview.Count > 0 &&
                numDocEntitiesInPreview < maxDocEntitiesInPreview)
            {
                // pop
                InterfaceEntity ent = null;
                lock (theDocEntitiesToPreview)
                {
                    ent = theDocEntitiesToPreview[0];
                    theDocEntitiesToPreview.RemoveAt(0);
                }

                try
                {
                    // temp input
                    var inputFn = ent?.DigitalFile?.Path;
                    if (inputFn != null)
                    {
                        // try check if Magick.NET library is available
                        var thumbBI = AnyUiGdiHelper.MakePreviewFromPackageOrUrl(_package, inputFn);
                        if (thumbBI != null)
                        {
                            // directly add this
                            if (referableHashToCachedBitmap != null
                                && !referableHashToCachedBitmap.ContainsKey(ent.ReferableHash))
                            {
                                if (ent.ImgContainerAnyUi != null)
                                    ent.ImgContainerAnyUi.BitmapInfo = thumbBI;
                                referableHashToCachedBitmap[ent.ReferableHash] = thumbBI;
                                updateDisplay = true;
                            }
                        }
                        else
                        {
                            //
                            // OLD way: use external program to convert
                            //

                            // makes only sense under Windows
                            if (OperatingSystemHelper.IsWindows())
                            {
                                // from package?
                                if (CheckIfPackageFile(inputFn))
                                    inputFn = _package?.MakePackageFileAvailableAsTempFile(ent.DigitalFile.Path);

                                // temp output
                                string outputFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".png");

                                // remember these for later deletion
                                ent.DeleteFilesAfterLoading = new[] { inputFn, outputFn };

                                // start process
                                string arguments = string.Format(
                                    "-flatten -density 75 \"{0}\"[0] \"{1}\"", inputFn, outputFn);
                                string exeFn = System.IO.Path.Combine(
                                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                    "convert.exe");

                                var startInfo = new ProcessStartInfo(exeFn, arguments)
                                {
                                    WindowStyle = ProcessWindowStyle.Hidden
                                };

                                var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

                                InterfaceEntity lambdaEntity = ent;
                                string outputFnBuffer = outputFn;
                                process.Exited += (sender2, args) =>
                                {
                                    // release number of parallel processes
                                    lock (mutexDocEntitiesInPreview)
                                    {
                                        numDocEntitiesInPreview--;
                                    }

                                    // take over data?
                                    if (lambdaEntity.ImgContainerAnyUi != null)
                                    {
                                        // trigger display image
                                        lambdaEntity.ImageReadyToBeLoaded = outputFnBuffer;
                                    }
                                };

                                try
                                {
                                    process.Start();
                                }
                                catch (Exception ex)
                                {
                                    AdminShellNS.LogInternally.That.Error(
                                        ex, $"Failed to start the process: {startInfo.FileName} " +
                                            $"with arguments {string.Join(" ", startInfo.Arguments)}");
                                }

                                // limit the number of parallel executions
                                lock (mutexDocEntitiesInPreview)
                                {
                                    numDocEntitiesInPreview++;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);

                    if (_dispatcherNumException < 3)
                        _log?.Error(ex, "AasxPluginAID / converting previews");
                    else if (_dispatcherNumException == 3)
                        _log?.Info("AasxPluginAID / stopping logging exceptions.");
                    _dispatcherNumException++;
                }
            }

            // over all items in order to check, if a prepared image shall be displayed
            foreach (var de in _renderedInterfaces)
            {
                if (de == null)
                    continue;

                if (de.ImageReadyToBeLoaded != null)
                {
                    // never again
                    var tempFn = de.ImageReadyToBeLoaded;
                    de.ImageReadyToBeLoaded = null;

                    // try load
                    try
                    {
                        // convert here, as the tick-Thread in STA / UI thread
                        var bi = de.LoadImageFromPath(tempFn);
                        updateDisplay = true;

                        // now delete the associated files file!
                        if (de.DeleteFilesAfterLoading != null)
                            foreach (var fn in de.DeleteFilesAfterLoading)
                                // it is quite likely (e.g. http:// files) that the delete fails!
                                try
                                {
                                    System.IO.File.Delete(fn);
                                }
                                catch (Exception ex)
                                {
                                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                                }

                        // remember in the cache
                        if (bi != null
                            && referableHashToCachedBitmap != null
                            && !referableHashToCachedBitmap.ContainsKey(de.ReferableHash))
                            referableHashToCachedBitmap[de.ReferableHash] = bi;
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);

                        if (_dispatcherNumException < 3)
                            _log?.Error(ex, "AasxPluginAID / displaying previews");
                        else if (_dispatcherNumException == 3)
                            _log?.Info("AasxPluginAID / stopping logging exceptions.");
                        _dispatcherNumException++;
                    }
                }
            }

            if (_eventStack != null && updateDisplay)
                _eventStack.PushEvent(new AasxPluginEventReturnUpdateAnyUi()
                {
                    Session = _session,
                    PluginName = null, // do NOT call the plugin before rendering
                    Mode = AnyUiRenderMode.StatusToUi,
                    UseInnerGrid = true
                });

            _inDispatcherTimer = false;
        }

        #endregion

        #region Utilities
        //===============

        private bool CheckIfPackageFile(string fn)
        {
            return fn.StartsWith(@"/");
        }

        #endregion
    }
}
