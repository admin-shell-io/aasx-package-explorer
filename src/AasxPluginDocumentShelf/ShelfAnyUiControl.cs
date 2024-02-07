/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxIntegrationBaseGdi;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using Newtonsoft.Json;
using System.IO.Packaging;
using System.Threading.Tasks;

// ReSharper disable InconsistentlySynchronizedField
// ReSharper disable AccessToModifiedClosure

namespace AasxPluginDocumentShelf
{
    public class ShelfAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private Aas.Submodel _submodel = null;
        private DocumentShelfOptions _options = null;
        private PluginEventStack _eventStack = null;
        private PluginSessionBase _session = null;
        private AnyUiStackPanel _panel = null;
        private AnyUiContextPlusDialogs _displayContext = null;
        private PluginOperationContextBase _opContext = null;
        private AasxPluginBase _plugin = null;

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        private DocumentEntity.SubmodelVersion _renderedVersion = DocumentEntity.SubmodelVersion.Default;
        private DocumentEntity.SubmodelVersion _selectedVersion = DocumentEntity.SubmodelVersion.Default;

        protected DefinitionsVDI2770.Vdi2770DocClass _selectedDocClass = DefinitionsVDI2770.Vdi2770DocClass.All;
        protected AasxLanguageHelper.LangEnum _selectedLang = AasxLanguageHelper.LangEnum.Any;

        private List<DocumentEntity> _renderedEntities = new List<DocumentEntity>();

        private List<DocumentEntity> theDocEntitiesToPreview = new List<DocumentEntity>();

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

        public ShelfAnyUiControl()
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
            DocumentShelfOptions theOptions,
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
            _options = theOptions;
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

        public static ShelfAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            DocumentShelfOptions options,
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
            var shelfCntl = new ShelfAnyUiControl();
            shelfCntl.Start(log, package, sm, options, eventStack, session, panel, opContext, cdp, plugin);

            // return shelf
            return shelfCntl;
        }

        #endregion

        #region Display Submodel
        //=============

        private void RenderFullShelf(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk)
        {
            // test trivial access
            if (_options == null || _submodel?.SemanticId == null)
                return;

            // make sure for the right Submodel
            DocumentShelfOptionsRecord foundRec = null;
            foreach (var rec in _options.LookupAllIndexKey<DocumentShelfOptionsRecord>(
                _submodel?.SemanticId?.GetAsExactlyOneKey()))
                foundRec = rec;

            if (foundRec == null)
                return;

            // right now: hardcoded check for model version
            _renderedVersion = DocumentEntity.SubmodelVersion.Default;
            var defs11 = AasxPredefinedConcepts.VDI2770v11.Static;
            var defs12 = AasxPredefinedConcepts.IdtaHandoverDocumentationV12.Static;
            if (_submodel.SemanticId.MatchesExactlyOneKey(defs12?.SM_HandoverDocumentation?.GetSemanticKey()))
                _renderedVersion = DocumentEntity.SubmodelVersion.V12;
            if (_submodel.SemanticId.MatchesExactlyOneKey(defs11?.SM_ManufacturerDocumentation?.GetSemanticKey()))
                _renderedVersion = DocumentEntity.SubmodelVersion.V11;
            if (foundRec.ForceVersion == DocumentEntity.SubmodelVersion.V10)
                _renderedVersion = DocumentEntity.SubmodelVersion.V10;
            if (foundRec.ForceVersion == DocumentEntity.SubmodelVersion.V11)
                _renderedVersion = DocumentEntity.SubmodelVersion.V11;
            if (foundRec.ForceVersion == DocumentEntity.SubmodelVersion.V12)
                _renderedVersion = DocumentEntity.SubmodelVersion.V12;
            _selectedVersion = _renderedVersion;

            // set usage info
            var useinf = foundRec.UsageInfo;

            // what defaultLanguage
            string defaultLang = null;

            // make new list box items
            _renderedEntities = new List<DocumentEntity>();
            // ReSharper disable ExpressionIsAlwaysNull
            if (_renderedVersion == DocumentEntity.SubmodelVersion.V12)
                _renderedEntities = ListOfDocumentEntity.ParseSubmodelForV12(
                    _package, _submodel, defs12, defaultLang, (int)_selectedDocClass, _selectedLang);
            else if (_renderedVersion == DocumentEntity.SubmodelVersion.V11)
                _renderedEntities = ListOfDocumentEntity.ParseSubmodelForV11(
                    _package, _submodel, defs11, defaultLang, (int)_selectedDocClass, _selectedLang);
            else
                _renderedEntities = ListOfDocumentEntity.ParseSubmodelForV10(
                    _package, _submodel, _options, defaultLang, (int)_selectedDocClass, _selectedLang);
            // ReSharper enable ExpressionIsAlwaysNull

            // bring it to the panel            
            RenderPanelOutside(view, uitk, _renderedVersion, useinf, defaultLang, _renderedEntities);
        }

        protected double _lastScrollPosition = 0.0;

        protected void RenderPanelOutside(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            DocumentEntity.SubmodelVersion modelVersion,
            string usageInfo,
            string defaultLanguage,
            List<DocumentEntity> its,
            double? initialScrollPos = null)
        {
            // make an outer grid, very simple grid of two rows: header & body
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
                content: $"Document Shelf");

            if (_opContext?.IsDisplayModeEditOrAdd == true)
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(header, 0, 1,
                        margin: new AnyUiThickness(2), setHeight: 21,
                        padding: new AnyUiThickness(2, 0, 2, 0),
                        content: "Add Entity .."),
                    (o) =>
                    {
                        // mode change
                        _formEntity = new AnyUiPanelEntity();
                        _formDoc = null;

                        //redisplay
                        PushUpdateEvent();
                        return new AnyUiLambdaActionNone();
                    });

            if (_opContext?.IsDisplayModeEditOrAdd == true)
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(header, 0, 2,
                        margin: new AnyUiThickness(2), setHeight: 21,
                        padding: new AnyUiThickness(2, 0, 2, 0),
                        content: "Add Document .."),
                    setValueAsync: (o) => ButtonTabPanels_Click("ButtonAddDocument"));

            //
            // Usage info
            //

            if (usageInfo.HasContent())
                uitk.AddSmallBasicLabelTo(header, 1, 0, colSpan: 5,
                    margin: new AnyUiThickness(8, 2, 2, 2),
                    foreground: AnyUiBrushes.DarkBlue,
                    fontSize: 0.8f, textWrapping: AnyUiTextWrapping.Wrap,
                    content: usageInfo);

            //
            // Selectors
            //

            // VDI 2770 classes
            var classes = new List<object>();
            foreach (var dc in (DefinitionsVDI2770.Vdi2770DocClass[])Enum.GetValues(
                                    typeof(DefinitionsVDI2770.Vdi2770DocClass)))
                classes.Add((object)
                    "" + DefinitionsVDI2770.GetDocClass(dc) + " - " + DefinitionsVDI2770.GetDocClassName(dc));

            // languages
            var langs = new List<object>();
            foreach (var dc in (AasxLanguageHelper.LangEnum[])Enum.GetValues(
                                    typeof(AasxLanguageHelper.LangEnum)))
                langs.Add("Lang - " + AasxLanguageHelper.LangEnumToISO639String[(int)dc]);

            // controls
            var controls = uitk.AddSmallWrapPanelTo(outer, 1, 0,
                background: AnyUiBrushes.MiddleGray, margin: new AnyUiThickness(0, 0, 0, 2));

            AnyUiComboBox cbClasses = null, cbLangs = null;

            cbClasses = AnyUiUIElement.RegisterControl(controls.Add(new AnyUiComboBox()
            {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                MinWidth = 140,
                Items = classes,
                SelectedIndex = (int)_selectedDocClass
            }), (o) =>
            {
                // ReSharper disable PossibleInvalidOperationException
                if (cbClasses != null)
                    _selectedDocClass = (DefinitionsVDI2770.Vdi2770DocClass)cbClasses.SelectedIndex;
                // ReSharper enable PossibleInvalidOperationException
                PushUpdateEvent();
                return new AnyUiLambdaActionNone();
            });

            cbLangs = AnyUiUIElement.RegisterControl(controls.Add(new AnyUiComboBox()
            {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                MinWidth = 120,
                Items = langs,
                SelectedIndex = (int)_selectedLang
            }), (o) =>
            {
                // ReSharper disable PossibleInvalidOperationException
                if (cbLangs != null)
                    _selectedLang = (AasxLanguageHelper.LangEnum)cbLangs.SelectedIndex;
                // ReSharper enable PossibleInvalidOperationException
                PushUpdateEvent();
                return new AnyUiLambdaActionNone();
            });

            AnyUiUIElement.RegisterControl(controls.Add(new AnyUiComboBox()
            {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                MinWidth = 60,
                Items = (new[] { "V1.0", "V1.1", "V1.2" }).ToList<object>(),
                SelectedIndex =
                    (_renderedVersion == DocumentEntity.SubmodelVersion.V12 ? 2 :
                    (_renderedVersion == DocumentEntity.SubmodelVersion.V11 ? 1 : 0)),
            }), (o) =>
            {
                if (o is int oi)
                    _selectedVersion = (DocumentEntity.SubmodelVersion)(oi + 1);
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
                        DocumentEntity foundDe = null;
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
            AnyUiSmallWidgetToolkit uitk, DocumentEntity de)
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
            var g = uitk.AddSmallGrid(3, 3,
                colWidths: new[] { "60:", "*", "24:" },
                rowHeights: new[] { "14:", "40:", "24:" },
                margin: new AnyUiThickness(1),
                background: AnyUiBrushes.White);
            border.Child = g;

            // Orga and Country flags flapping in the breeze
            var sp1 = uitk.AddSmallStackPanelTo(g, 0, 1,
                setHorizontal: true);

            var wpCountries = sp1.Add(new AnyUiWrapPanel()
            {
                HorizontalAlignment = AnyUiHorizontalAlignment.Left,
                Orientation = AnyUiOrientation.Horizontal
            });

            foreach (var code in de.CountryCodes)
                wpCountries.Add(new AnyUiCountryFlag()
                {
                    HorizontalAlignment = AnyUiHorizontalAlignment.Left,
                    ISO3166Code = code,
                    Margin = new AnyUiThickness(0, 0, 3, 0),
                    MinHeight = 14,
                    MaxHeight = 14,
                    MaxWidth = 20
                });

            sp1.Add(new AnyUiTextBlock()
            {
                HorizontalAlignment = AnyUiHorizontalAlignment.Left,
                HorizontalContentAlignment = AnyUiHorizontalAlignment.Left,
                Text = $"{de.Organization}",
                FontSize = 0.8f,
                FontWeight = AnyUiFontWeight.Bold
            });

            // Title
            uitk.AddSmallBasicLabelTo(g, 1, 1,
                textIsSelectable: false,
                margin: new AnyUiThickness(2),
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                fontSize: 1.2f,
                content: $"{de.Title}");

            // further info
            uitk.AddSmallBasicLabelTo(g, 2, 1,
                textIsSelectable: false,
                margin: new AnyUiThickness(2),
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                fontSize: 0.8f,
                content: $"{de.FurtherInfo}");

            // Image
            de.ImgContainerAnyUi =
                uitk.Set(
                    uitk.AddSmallImageTo(g, 0, 0,
                        margin: new AnyUiThickness(2),
                        stretch: AnyUiStretch.Uniform),
                    rowSpan: 3,
                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                    verticalAlignment: AnyUiVerticalAlignment.Stretch);

            // image allows drag
            de.ImgContainerAnyUi.EmitEvent = AnyUiEventMask.DragStart;
            de.ImgContainerAnyUi.setValueLambda = (o) =>
            {
                if (o is AnyUiEventData ed
                    && ed.Mask == AnyUiEventMask.DragStart)
                {
                    de.RaiseDragStart();
                }
                return new AnyUiLambdaActionNone();
            };

            var hds = new List<string>();
            if (_opContext?.IsDisplayModeEditOrAdd == true)
            {
                hds.AddRange(new[] { "\u270e", "Edit metadata" });
                hds.AddRange(new[] { "\u2702", "Delete" });
            }
            else
                hds.AddRange(new[] { "\u270e", "View metadata" });

            hds.AddRange(new[] { "\U0001F56E", "View file" });
            hds.AddRange(new[] { "\U0001F4BE", "Save file .." });

            if (_opContext?.IsDisplayModeEditOrAdd == true)
            {
                hds.AddRange(new[] { "\U0001F5BC", "Make preview permanent" });
            }

            // context menu
            uitk.AddSmallContextMenuItemTo(g, 2, 2,
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
                PluginName = "AasxPluginDocumentShelf",
                Session = _session,
                Mode = mode,
                UseInnerGrid = true
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

        private async Task DocumentEntity_MenuClick(DocumentEntity e, string menuItemHeader, object tag)
        {
            // first check
            if (e == null || menuItemHeader == null)
                return;

            // what to do?
            if (tag == null
                && (menuItemHeader == "Edit metadata" || menuItemHeader == "View metadata")
                && e.SourceElementsDocument != null && e.SourceElementsDocumentVersion != null)
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

                // prepare form instance, take over existing data
                if (_renderedVersion == DocumentEntity.SubmodelVersion.V12)
                {
                    // ask the plugin generic forms for information via event stack
                    // and subsequently start editing form
                    await GetFormDescForV12(e.SourceElementsDocument, lambda);
                }
                else
                {
                    var desc = DocuShelfSemanticConfig.CreateVdi2770TemplateDescFor(_renderedVersion, _options);
                    _updateSourceElements = e.SourceElementsDocument;

                    lambda(desc);
                }

                // OK
                return;
            }

            if (tag == null && menuItemHeader == "Delete" && e.SourceElementsDocument != null
                && e.SourceElementsDocumentVersion != null && _submodel?.SubmodelElements != null
                && _options != null
                && _opContext?.IsDisplayModeEditOrAdd == true)
            {
                // the source elements need to match a Document
                var semConf = DocuShelfSemanticConfig.CreateDefaultFor(_renderedVersion);
                var found = false;
                foreach (var smcDoc in
                    _submodel.SubmodelElements.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                        semConf.SemIdDocument, MatchMode.Relaxed))
                    if (smcDoc?.Value == e.SourceElementsDocument)
                    {
                        // identify as well the DocumentVersion
                        // (convert to List() because of Count() below)
                        var allVers =
                            e.SourceElementsDocument.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                                semConf.SemIdDocumentVersion, MatchMode.Relaxed).ToList();
                        foreach (var smcVer in allVers)
                            if (smcVer?.Value == e.SourceElementsDocumentVersion)
                            {
                                // found 
                                found = true;

                                // access
                                if (smcVer == null || smcVer.Value == null || smcDoc == null || smcDoc.Value == null)
                                    continue;

                                // ask back via display context
                                if (AnyUiMessageBoxResult.Cancel == await _displayContext?.MessageBoxFlyoutShowAsync(
                                    "Delete DocumentEntity? This cannot be reverted!",
                                    "DocumentShelf",
                                    AnyUiMessageBoxButton.OKCancel,
                                    AnyUiMessageBoxImage.Question))
                                    return;

                                // do it
                                try
                                {
                                    // confirmed! -> delete
                                    if (allVers.Count < 2)
                                        // remove the whole document!
                                        _submodel.SubmodelElements.Remove(smcDoc);
                                    else
                                        // remove only the document version
                                        e.SourceElementsDocument.Remove(smcVer);

                                    // re-display also in Explorer
                                    _eventStack?.PushEvent(new AasxPluginResultEventRedrawAllElements()
                                    { Session = _session });

                                    // log
                                    _log?.Info("Deleted Document(Version).");
                                }
                                catch (Exception ex)
                                {
                                    _log?.Error(ex, "while saving digital file to user specified location");
                                }

                                // OK
                                return;
                            }

                        // ReSharper enable PossibleMultipleEnumeration
                    }

                if (!found)
                    _log?.Error("Document element was not found properly!");
            }

            // show digital file
            if (tag == null && menuItemHeader == "View file")
                DocumentEntity_DisplaySaveFile(e, true, false);

            // save digital file?
            if (tag == null && menuItemHeader == "Save file .." && e.DigitalFile?.Path.HasContent() == true)
            {
                DocumentEntity_DisplaySaveFile(e, true, true);
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
                    "DocumentShelf",
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

        private void DocumentEntity_DisplaySaveFile(DocumentEntity e, bool display, bool save)
        {
            // first check
            if (e == null || e.DigitalFile?.Path == null || e.DigitalFile.Path.Trim().Length < 1
                || _eventStack == null)
                return;

            try
            {
                // temp input
                var inputFn = e.DigitalFile.Path;
                try
                {
                    if (!inputFn.ToLower().Trim().StartsWith("http://")
                            && !inputFn.ToLower().Trim().StartsWith("https://"))
                        inputFn = _package?.MakePackageFileAvailableAsTempFile(inputFn);
                }
                catch (Exception ex)
                {
                    _log?.Error(ex, "Making local file available");
                }

                // give over to event stack
                _eventStack?.PushEvent(new AasxPluginResultEventDisplayContentFile()
                {
                    SaveInsteadDisplay = save,
                    ProposeFn = System.IO.Path.GetFileName(e.DigitalFile.Path),
                    Session = _session,
                    fn = inputFn,
                    mimeType = e.DigitalFile.MimeType
                });
            }
            catch (Exception ex)
            {
                _log?.Error(ex, "when executing file action");
            }
        }

        private void DocumentEntity_DoubleClick(DocumentEntity e)
        {
            DocumentEntity_DisplaySaveFile(e, true, false);
        }

        protected bool _inDragStart = false;

        private void DocumentEntity_DragStart(DocumentEntity e)
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

        private async Task<AnyUiLambdaActionBase> ButtonTabPanels_Click(string cmd, string arg = null)
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
                    && _options != null
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

            if (cmd == "ButtonFixCDs")
            {
                // check if CDs are present
                var theDefs = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(
                    new AasxPredefinedConcepts.DefinitionsVDI2770());
                var theCds = theDefs.GetAllReferables().Where(
                        (rf) => { return rf is Aas.ConceptDescription; }).ToList();

                // v11
                if (_selectedVersion == DocumentEntity.SubmodelVersion.V11)
                {
                    theCds = AasxPredefinedConcepts.VDI2770v11.Static.GetAllReferables().Where(
                        (rf) => { return rf is Aas.ConceptDescription; }).ToList();
                }

                // v12
                if (_selectedVersion == DocumentEntity.SubmodelVersion.V12)
                {
                    theCds = AasxPredefinedConcepts.IdtaHandoverDocumentationV12.Static.GetAllReferables().Where(
                        (rf) => { return rf is Aas.ConceptDescription; }).ToList();
                }

                if (theCds.Count < 1)
                {
                    _log?.Error(
                        "Not able to find appropriate ConceptDescriptions in pre-definitions. " +
                        "Aborting.");
                    return new AnyUiLambdaActionNone();
                }

                // check for Environment
                var env = _package?.AasEnv;
                if (env == null)
                {
                    _log?.Error(
                        "Not able to access AAS environment for set of Submodel's ConceptDescriptions. Aborting.");
                    return new AnyUiLambdaActionNone();
                }

                // ask back via display context
                if (AnyUiMessageBoxResult.Cancel == await _displayContext?.MessageBoxFlyoutShowAsync(
                    "Add missing ConceptDescriptions to the AAS?",
                    "DocumentShelf",
                    AnyUiMessageBoxButton.OKCancel,
                    AnyUiMessageBoxImage.Question))
                    return new AnyUiLambdaActionNone();

                // do it
                try
                {
                    // ok, check
                    int nr = 0;
                    foreach (var x in theCds)
                    {
                        var cd = x as Aas.ConceptDescription;
                        if (cd == null || cd.Id?.HasContent() != true)
                            continue;
                        var cdFound = env.FindConceptDescriptionById(cd.Id);
                        if (cdFound != null)
                            continue;
                        // ok, add
                        var newCd = cd.Copy();
                        env.ConceptDescriptions.Add(newCd);
                        nr++;
                    }

                    // ok
                    _log?.Info("In total, {0} ConceptDescriptions were added to the AAS environment.", nr);
                }
                catch (Exception ex)
                {
                    _log?.Error(ex, "when adding ConceptDescriptions for Document");
                }

                // ok; event pending, nothing here
                return new AnyUiLambdaActionNone();
            }

            if (cmd == "ButtonAddDocument")
            {
                // lambda
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

                // prepare form instance, take over existing data
                if (_renderedVersion == DocumentEntity.SubmodelVersion.V12)
                {
                    // ask the plugin generic forms for information via event stack
                    // and subsequently start editing form
                    await GetFormDescForV12(sourceElems: null, lambda);
                }
                else
                {
                    var desc = DocuShelfSemanticConfig.CreateVdi2770TemplateDescFor(_renderedVersion, _options);
                    _updateSourceElements = null;
                    lambda(desc);
                }


                // OK
                return new AnyUiLambdaActionNone();
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

            if (cmd == "ButtonAddEntity" && _formEntity.IdShort.HasContent())
            {
                // add entity
                _submodel?.SmeForWrite().CreateSMEForCD<Aas.Entity>(
                    AasxPredefinedConcepts.VDI2770v11.Static.CD_DocumentedEntity,
                    idShort: "" + _formEntity.IdShort.Trim(),
                    addSme: true);

                _log?.Info($"Entity {_formEntity.IdShort} added.");

                // reset view
                _formEntity = null;
                _formDoc = null;

                // redisplay tree and plugin
                _eventStack?.PushEvent(new AasxPluginResultEventRedrawAllElements()
                { Session = _session });
                return new AnyUiLambdaActionNone();
            }

            // no?
            return new AnyUiLambdaActionNone();
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
            if (_renderedEntities == null || theDocEntitiesToPreview == null || _inDispatcherTimer)
                if (_renderedEntities == null || theDocEntitiesToPreview == null || _inDispatcherTimer)
                    return;

            _inDispatcherTimer = true;
            var updateDisplay = false;

            // each tick check for one image, if a preview shall be done
            if (theDocEntitiesToPreview != null && theDocEntitiesToPreview.Count > 0 &&
                numDocEntitiesInPreview < maxDocEntitiesInPreview)
            {
                // pop
                DocumentEntity ent = null;
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

                                DocumentEntity lambdaEntity = ent;
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
                        _log?.Error(ex, "AasxPluginDocumentShelf / converting previews");
                    else if (_dispatcherNumException == 3)
                        _log?.Info("AasxPluginDocumentShelf / stopping logging exceptions.");
                    _dispatcherNumException++;
                }
            }

            // over all items in order to check, if a prepared image shall be displayed
            foreach (var de in _renderedEntities)
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
                            _log?.Error(ex, "AasxPluginDocumentShelf / displaying previews");
                        else if (_dispatcherNumException == 3)
                            _log?.Info("AasxPluginDocumentShelf / stopping logging exceptions.");
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
