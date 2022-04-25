using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxPredefinedConcepts;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPluginDocumentShelf
{
    public class ShelfAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private AdminShell.Submodel _submodel = null;
        private DocumentShelfOptions _options = null;
        private PluginEventStack _eventStack = null;
        private AnyUiStackPanel _panel = null;

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        private string convertableFiles = ".pdf .jpeg .jpg .png .bmp .pdf .xml .txt *";

        private DocumentEntity.SubmodelVersion _renderedVersion = DocumentEntity.SubmodelVersion.Default;
        private DocumentEntity.SubmodelVersion _selectedVersion = DocumentEntity.SubmodelVersion.Default;

        protected DefinitionsVDI2770.Vdi2770DocClass _selectedDocClass = DefinitionsVDI2770.Vdi2770DocClass.All;
        protected AasxLanguageHelper.LangEnum _selectedLang = AasxLanguageHelper.LangEnum.Any;

        private List<DocumentEntity> _renderedEntities = new List<DocumentEntity>();

        private List<DocumentEntity> theDocEntitiesToPreview = new List<DocumentEntity>();

        // members for form editing

        protected AnyUiRenderForm _formDoc = null;

        #endregion

        #region Cache for already generated Images
        //========================================

        private static Dictionary<string, BitmapImage> referableHashToCachedBitmap =
            new Dictionary<string, BitmapImage>();

        #endregion

        #region Constructors, as for WPF control
        //=============

        public ShelfAnyUiControl()
        {
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
            var dispatcher = System.Windows.Threading.Dispatcher.FromThread(System.Threading.Thread.CurrentThread);
            if (dispatcher != null)
            {
                var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += DispatcherTimer_Tick;
                dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
                dispatcherTimer.Start();
            }
            else
            {
                // Note: this timer shall work for all sorts of applications?
                // see: https://stackoverflow.com/questions/21041299/c-sharp-dispatchertimer-in-dll-application-never-triggered
                var _timer2 = new System.Timers.Timer(1000);
                _timer2.Elapsed += DispatcherTimer_Tick;
                _timer2.Enabled = true;
                _timer2.Start();
            }
        }

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel theSubmodel,
            DocumentShelfOptions theOptions,
            PluginEventStack eventStack,
            AnyUiStackPanel panel)
        {
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _options = theOptions;
            _eventStack = eventStack;
            _panel = panel;

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
            object opanel)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as AdminShell.Submodel;
            var panel = opanel as AnyUiStackPanel;
            if (package == null || sm == null || panel == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // do NOT create WPF controls
            FormInstanceBase.createSubControls = false;

            // factory this object
            var shelfCntl = new ShelfAnyUiControl();
            shelfCntl.Start(log, package, sm, options, eventStack, panel);

            // return shelf
            return shelfCntl;
        }

        #endregion

        #region Display Submodel
        //=============

        private void RenderFullShelf(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk)
        {
            // test trivial access
            if (_options == null || _submodel?.semanticId == null)
                return;

            // make sure for the right Submodel
            DocumentShelfOptionsRecord foundRec = null;
            foreach (var rec in _options.LookupAllIndexKey<DocumentShelfOptionsRecord>(
                _submodel?.semanticId?.GetAsExactlyOneKey()))
                foundRec = rec;

            if (foundRec == null)
                return;

            // right now: hardcoded check for mdoel version
            _renderedVersion = DocumentEntity.SubmodelVersion.Default;
            var defs11 = AasxPredefinedConcepts.VDI2770v11.Static;
            if (_submodel.semanticId.Matches(defs11?.SM_ManufacturerDocumentation?.GetSemanticKey()))
                _renderedVersion = DocumentEntity.SubmodelVersion.V11;
            if (foundRec.ForceVersion == DocumentEntity.SubmodelVersion.V10)
                _renderedVersion = DocumentEntity.SubmodelVersion.V10;
            if (foundRec.ForceVersion == DocumentEntity.SubmodelVersion.V11)
                _renderedVersion = DocumentEntity.SubmodelVersion.V11;
            _selectedVersion =  _renderedVersion;

            // set usage info
            var useinf = foundRec.UsageInfo;

            // what defaultLanguage
            string defaultLang = null;
            //if (theViewModel != null && theViewModel.TheSelectedLanguage > AasxLanguageHelper.LangEnum.Any)
            //    defaultLang = AasxLanguageHelper.LangEnumToISO639String[(int)theViewModel.TheSelectedLanguage];

            // make new list box items
            _renderedEntities = new List<DocumentEntity>();
            if (_renderedVersion != DocumentEntity.SubmodelVersion.V11)
                _renderedEntities = ListOfDocumentEntity.ParseSubmodelForV10(
                    _package, _submodel, _options, defaultLang, (int) _selectedDocClass, _selectedLang); 
            else
                _renderedEntities = ListOfDocumentEntity.ParseSubmodelForV11(
                    _package, _submodel, defs11, defaultLang, (int) _selectedDocClass, _selectedLang); 

            // bring it to the panel            
            RenderPanelOutside(view, uitk, _renderedVersion, useinf, defaultLang, _renderedEntities);
        }

        protected double _lastScrollPosition = 0.0;

        protected void RenderPanelOutside (
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            DocumentEntity.SubmodelVersion modelVersion,
            string usageInfo,
            string defaultLanguage,
            List<DocumentEntity> its,
            double ?initialScrollPos = null)
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

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(header, 0, 2,
                    margin: new AnyUiThickness(2), setHeight: 21,
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Add Document .."),
                (o) => ButtonTabPanels_Click("ButtonAddDocument"));

            //
            // Usage info
            //

            if (usageInfo.HasContent())
                uitk.AddSmallBasicLabelTo(header, 1, 0, colSpan: 5,
                    margin: new AnyUiThickness(8, 2, 2, 2),
                    foreground: AnyUiBrushes.DarkBlue,
                    fontSize: 0.8f, setWrap: true,
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
                langs.Add((object)
                    ("Lang - " + AasxLanguageHelper.LangEnumToISO639String[(int) dc]));

            // controls
            var controls = uitk.AddSmallWrapPanelTo(outer, 1, 0, 
                background: AnyUiBrushes.MiddleGray, margin: new AnyUiThickness(0, 0, 0, 2));

            AnyUiComboBox cbClasses= null, cbLangs = null;

            cbClasses = (AnyUiComboBox)AnyUiUIElement.RegisterControl(controls.Add(new AnyUiComboBox() {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                MinWidth = 140,
                Items = classes,
                SelectedIndex = (int) _selectedDocClass
            }), (o) =>
            {
                _selectedDocClass = (DefinitionsVDI2770.Vdi2770DocClass) cbClasses.SelectedIndex;
                PushUpdateEvent();
                return new AnyUiLambdaActionNone();
            });

            cbLangs = (AnyUiComboBox)AnyUiUIElement.RegisterControl(controls.Add(new AnyUiComboBox()
            {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                MinWidth = 120,
                Items = langs,
                SelectedIndex = (int) _selectedLang
            }), (o) =>
            {
                _selectedLang = (AasxLanguageHelper.LangEnum)cbLangs.SelectedIndex;
                PushUpdateEvent();
                return new AnyUiLambdaActionNone();
            });

            var cbVersion = AnyUiUIElement.RegisterControl(controls.Add(new AnyUiComboBox()
            {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                MinWidth = 60,
                Items = (new string[] { "V1.0", "V1.1" }).ToList<object>(),
                SelectedIndex = _renderedVersion == DocumentEntity.SubmodelVersion.V11 ? 1 : 0,
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
            var space = uitk.AddSmallBasicLabelTo(outer, 2, 0, 
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
                }) as AnyUiScrollViewer;

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

                        // inputFn = @"C:\MIHO\Develop\Aasx\repo\sample.png";

                        ent.LoadImageFromPath(inputFn);
                    } catch (Exception ex)
                    {
                        _log?.Error(ex, $"when loading preview image {inputFn}");
                    }
                }

                // delayed load logic
                // can already put a generated image into the viewbox?
                if (referableHashToCachedBitmap != null &&
                    referableHashToCachedBitmap.ContainsKey(ent.ReferableHash))
                {
                    ent.ImgContainerAnyUi.Bitmap = referableHashToCachedBitmap[ent.ReferableHash];
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

        public AnyUiFrameworkElement RenderAnyUiDocumentEntity (
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
                rowHeights: new[] { "14:", "40:", "24:"},
                margin: new AnyUiThickness(1),
                background: AnyUiBrushes.White);
            border.Child = g;

            // Orga and Country flags flapping in the breez
            var sp1 = uitk.AddSmallStackPanelTo(g, 0, 1,
                setHorizontal: true);

            var wpCountries = sp1.Add(new AnyUiWrapPanel() { 
                HorizontalAlignment = AnyUiHorizontalAlignment.Left,
                Orientation = AnyUiOrientation.Horizontal });

            foreach (var code in de.CountryCodes)
                wpCountries.Add(new AnyUiCountryFlag()
                {
                    HorizontalAlignment = AnyUiHorizontalAlignment.Left,
                    ISO3166Code = code,
                    Margin = new AnyUiThickness(0,0, 3, 0),
                    MinHeight = 14, MaxHeight = 14, MaxWidth = 20
                }); 

            var orga = sp1.Add(new AnyUiTextBlock() { 
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

            // context menu
            uitk.AddSmallContextMenuItemTo(g, 2, 2, 
                    "\u22ee",
                    new[] {
                                    "\u270e", "Edit",
                                    "\u2702", "Delete",
                                    "\U0001F4BE", "Save as ..",
                    },
                    margin: new AnyUiThickness(2, 2, 2, 2),
                    padding: new AnyUiThickness(5, 0, 5, 0),
                    fontWeight: AnyUiFontWeight.Bold,
                    menuItemLambda: (o) =>
                    {
                        if (o is int ti)
                            // awkyard, but for compatibility to WPF version
                            de?.RaiseMenuClick((new string[] { "Edit", "Delete", "Save file .." })[ti], null);
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
                Func<object, AnyUiLambdaActionBase> lambdaCancel = null,
                Func<object, AnyUiLambdaActionBase> lambdaAdd = null)
            {
                // will have (in future) a text box

                AnyUiTextBox tbIdShort = null;

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
                    lambdaCancel);

                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(header, 0, 2,
                        margin: new AnyUiThickness(2), setHeight: 21,
                        padding: new AnyUiThickness(4, 0, 4, 0),
                        content: "Add"),
                    lambdaAdd);

                // small spacer
                outer.RowDefinitions[1] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
                var space = uitk.AddSmallBasicLabelTo(outer, 1, 0,
                    fontSize: 0.3f,
                    verticalAlignment: AnyUiVerticalAlignment.Top,
                    content: "", background: AnyUiBrushes.White);

                //
                // Grid with entries
                //

                var body = uitk.AddSmallGridTo(outer, 2, 0, rows: 1, cols: 2, 
                    colWidths: new[] { "#", "*" },
                    background:AnyUiBrushes.LightGray);

                uitk.AddSmallBasicLabelTo(body, 0, 0,
                    verticalAlignment: AnyUiVerticalAlignment.Center,
                    verticalContentAlignment: AnyUiVerticalAlignment.Center,
                    margin: new AnyUiThickness(0,0,4,0),
                    content: "idShort:");

                tbIdShort = (AnyUiTextBox) AnyUiUIElement.RegisterControl(
                    uitk.AddSmallTextBoxTo(body, 0, 1,
                        margin: new AnyUiThickness(2,10,2,10),
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
                PluginName = AasxIntegrationBase.AasxPlugin.PluginName,
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
                return;
            }

            //if (this.currentFormInst?.subscribeForNextEventReturn != null)
            //{
            //    // delete first
            //    var tempLambda = this.currentFormInst.subscribeForNextEventReturn;
            //    this.currentFormInst.subscribeForNextEventReturn = null;

            //    // execute
            //    tempLambda(evtReturn);
            //}
        }

        #endregion

        #region Update
        //=============

        public void Update(params object[] args)
        {
            // check args
            if (args == null || args.Length < 1
                || !(args[0] is AnyUiStackPanel newPanel)) 
                return;

            // ok, re-assign panel and re-display
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
                // RenderFormInst(_panel, _uitk, _currentFormInst, initialScrollPos: _lastScrollPosition);

                _formDoc.RenderFormInst(_panel, _uitk,
                    setLastScrollPos: true,
                    lambdaFixCds: (o) => ButtonTabPanels_Click("ButtonFixCDs"),
                    lambdaCancel: (o) => ButtonTabPanels_Click("ButtonCancel"),
                    lambdaOK: (o) => ButtonTabPanels_Click("ButtonUpdate"));
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

        private AdminShell.SubmodelElementWrapperCollection _updateSourceElements = null;

        private void DocumentEntity_MenuClick(DocumentEntity e, string menuItemHeader, object tag)
        {
            // first check
            if (e == null || menuItemHeader == null)
                return;

            // what to do?
            if (tag == null && menuItemHeader == "Edit" && e.SourceElementsDocument != null &&
                e.SourceElementsDocumentVersion != null)
            {
                // prepare form instance, take over existing data
                var desc = DocuShelfSemanticConfig.CreateVdi2770TemplateDescFor(_renderedVersion, _options);
                _updateSourceElements = e.SourceElementsDocument;

                var fi = new FormInstanceSubmodelElementCollection(null, desc);
                fi.PresetInstancesBasedOnSource(_updateSourceElements);
                fi.outerEventStack = _eventStack;
                fi.OuterPluginName = AasxIntegrationBase.AasxPlugin.PluginName;

                // initialize form
                _formDoc = new AnyUiRenderForm(
                    fi,
                    updateMode: true);


                //// make a template description for the content (remeber it)
                //formInUpdateMode = true;
                //updateSourceElements = e.SourceElementsDocument;

                //var desc = DocuShelfSemanticConfig.CreateVdi2770TemplateDescFor(_renderedVersion, _options);
                //var defs = DocuShelfSemanticConfig.CreateDefaultFor(_renderedVersion);
                //this.currentFormDescription = desc;

                //// take over existing data
                //this.currentFormInst = new FormInstanceSubmodelElementCollection(null, currentFormDescription);
                //this.currentFormInst.PresetInstancesBasedOnSource(updateSourceElements);
                //this.currentFormInst.outerEventStack = _eventStack;

                // bring it to the panel by redrawing the plugin
                PushUpdateEvent();

                // OK
                return;
            }

            if (tag == null && menuItemHeader == "Delete" && e.SourceElementsDocument != null 
                && e.SourceElementsDocumentVersion != null && _submodel?.submodelElements != null 
                && _options != null)
            {
                // the source elements need to match a Document
                var semConf = DocuShelfSemanticConfig.CreateDefaultFor(_renderedVersion);
                var found = false;
                foreach (var smcDoc in
                    _submodel.submodelElements.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        semConf.SemIdDocument))
                    if (smcDoc?.value == e.SourceElementsDocument)
                    {
                        // identify as well the DocumentVersion
                        // (convert to List() because of Count() below)
                        var allVers =
                            e.SourceElementsDocument.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                                semConf.SemIdDocumentVersion).ToList();
                        foreach (var smcVer in allVers)
                            if (smcVer?.value == e.SourceElementsDocumentVersion)
                            {
                                // found 
                                found = true;

                                // access
                                if (smcVer == null || smcVer.value == null || smcDoc == null || smcDoc.value == null)
                                    continue;

                                // ask back via event stack
                                _eventStack?.PushEvent(new AasxIntegrationBase.AasxPluginResultEventMessageBox()
                                {
                                    Caption = "Question",
                                    Message = "Delete Document?",
                                    Buttons = AnyUiMessageBoxButton.YesNo,
                                    Image = AnyUiMessageBoxImage.Warning
                                });

                                // .. and receive incoming event
                                _menuSubscribeForNextEventReturn = (revt) =>
                                {
                                    if (revt is AasxPluginEventReturnMessageBox rmb
                                        && rmb.Result == AnyUiMessageBoxResult.Yes)
                                    {
                                        try
                                        {
                                            // confirmed! -> delete
                                            if (allVers.Count < 2)
                                                // remove the whole document!
                                                _submodel.submodelElements.Remove(smcDoc);
                                            else
                                                // remove only the document version
                                                e.SourceElementsDocument.Remove(smcVer);

                                            // re-display also in Explorer
                                            _eventStack?.PushEvent(new AasxPluginResultEventRedrawAllElements());

                                            // log
                                            _log?.Info("Deleted Document(Version).");
                                        }
                                        catch (Exception ex)
                                        {
                                            _log?.Error(ex, "while saveing digital file to user specified loacation");
                                        }
                                    }
                                };

                                // OK
                                return;
                            }

                        // ReSharper enable PossibleMultipleEnumeration
                    }

                if (!found)
                    _log?.Error("Document element was not found properly!");
            }

            // save digital file?
            if (tag == null && menuItemHeader == "Save file .." && e.DigitalFile?.Path.HasContent() == true)
            {
                // make a file available
                var inputFn = e.DigitalFile.Path;

                if (CheckIfPackageFile(inputFn))
                    inputFn = _package?.MakePackageFileAvailableAsTempFile(e.DigitalFile.Path);

                if (!inputFn.HasContent())
                {
                    _log.Error("Error making digital file available. Aborting!");
                    return;
                }

                // ask for a file name via event stack
                _eventStack?.PushEvent(new AasxIntegrationBase.AasxPluginResultEventSelectFile()
                {
                    SaveDialogue = true,
                    Title = "Save digital file as ..",
                    FileName = System.IO.Path.GetFileName(e.DigitalFile.Path),
                    DefaultExt = "*" + System.IO.Path.GetExtension(e.DigitalFile.Path)
                });

                // .. and receive incoming event
                _menuSubscribeForNextEventReturn = (revt) =>
                {
                    if (revt is AasxPluginEventReturnSelectFile rsel
                        && rsel.FileNames != null && rsel.FileNames.Length > 0)
                    {
                        try
                        {
                            // do it
                            File.Copy(inputFn, rsel.FileNames[0], overwrite: true);
                            _log.Info("Successfully saved {0}", rsel.FileNames[0]);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "while saving digital file to user specified loacation");
                        }
                    }
                };
            }

            //// check for a document reference
            //if (tag != null && tag is Tuple<DocumentEntity.DocRelationType, AdminShell.Reference> reltup
            //    && reltup.Item2 != null && reltup.Item2.Count > 0)
            //{
            //    var evt = new AasxPluginResultEventNavigateToReference();
            //    evt.targetReference = new AdminShell.Reference(reltup.Item2);
            //    this.theEventStack.PushEvent(evt);
            //}
        }

        private void DocumentEntity_DoubleClick(DocumentEntity e)
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
                    fn = inputFn,
                    mimeType = e.DigitalFile.MimeType
                });
            }
            catch (Exception ex)
            {
                _log?.Error(ex, "when double-click");
            }
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

        private AnyUiLambdaActionBase ButtonTabPanels_Click(string cmd, string arg = null)
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
                    AdminShell.SubmodelElementWrapperCollection currentElements = null;
                    if (_formDoc.InUpdateMode)
                    {
                        currentElements = _updateSourceElements;
                    }
                    else
                    {
                        currentElements = new AdminShell.SubmodelElementWrapperCollection();
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
                            as AdminShell.SubmodelElementCollection;

                    // if not update, put them into the Document's Submodel
                    if (!_formDoc.InUpdateMode && currentElements != null && newSmc != null)
                    {
                        // make newSmc unique in the cotext of the Submodel
                        FormInstanceHelper.MakeIdShortUnique(_submodel.submodelElements, newSmc);

                        // add the elements
                        newSmc.value = currentElements;

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
                    (rf) => { return rf is AdminShell.ConceptDescription; }).ToList();

                // v11
                if (_selectedVersion == DocumentEntity.SubmodelVersion.V11)
                {
                    theCds = AasxPredefinedConcepts.VDI2770v11.Static.GetAllReferables().Where(
                    (rf) => { return rf is AdminShell.ConceptDescription; }).ToList();
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

                // ask back via event stack
                _eventStack?.PushEvent(new AasxIntegrationBase.AasxPluginResultEventMessageBox()
                {
                    Caption = "Question",
                    Message = "Add missing ConceptDescriptions to the AAS?",
                    Buttons = AnyUiMessageBoxButton.YesNo,
                    Image = AnyUiMessageBoxImage.Warning
                });

                // .. and receive incoming event
                _menuSubscribeForNextEventReturn = (revt) =>
                {
                    if (revt is AasxPluginEventReturnMessageBox rmb
                        && rmb.Result == AnyUiMessageBoxResult.Yes)
                    {
                        try
                        {
                            // ok, check
                            int nr = 0;
                            foreach (var x in theCds)
                            {
                                var cd = x as AdminShell.ConceptDescription;
                                if (cd == null || cd.identification == null)
                                    continue;
                                var cdFound = env.FindConceptDescription(cd.identification);
                                if (cdFound != null)
                                    continue;
                                // ok, add
                                var newCd = new AdminShell.ConceptDescription(cd);
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
                    }
                };

                // ok; event pending, nothing here
                return new AnyUiLambdaActionNone();
            }

            if (cmd == "ButtonAddDocument")
            {
                // prepare form instance, take over existing data
                var desc = DocuShelfSemanticConfig.CreateVdi2770TemplateDescFor(_renderedVersion, _options);
                _updateSourceElements = null;

                var fi = new FormInstanceSubmodelElementCollection(null, desc);
                fi.outerEventStack = _eventStack;
                fi.OuterPluginName = AasxIntegrationBase.AasxPlugin.PluginName;

                // initialize form
                _formDoc = new AnyUiRenderForm(
                    fi,
                    updateMode: false);

                // bring it to the panel by redrawing the plugin
                PushUpdateEvent();

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
                _submodel?.SmeForWrite?.CreateSMEForCD<AdminShell.Entity>(
                    AasxPredefinedConcepts.VDI2770v11.Static.CD_DocumentedEntity,
                    idShort: "" + _formEntity.IdShort.Trim(),
                    addSme: true);

                _log?.Info($"Entity {_formEntity.IdShort} added.");

                // reset view
                _formEntity = null;
                _formDoc = null;

                // redisplay tree and plugin
                _eventStack?.PushEvent(new AasxPluginResultEventRedrawAllElements());
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

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            // access
            if (_renderedEntities == null || theDocEntitiesToPreview == null)
                return;

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
                        // from package?
                        if (CheckIfPackageFile(inputFn))
                            inputFn = _package?.MakePackageFileAvailableAsTempFile(ent.DigitalFile.Path);

                        // temp output
                        string outputFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".png");

                        // remember these for later deletion
                        ent.DeleteFilesAfterLoading = new[] { inputFn, outputFn };

                        // start process
                        string arguments = string.Format("-flatten -density 75 \"{0}\"[0] \"{1}\"", inputFn, outputFn);
                        string exeFn = System.IO.Path.Combine(
                            System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "convert.exe");

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
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }

            // over all items in order to check, if a prepared image shall be displayed
            var updateDisplay = false;
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
                                    File.Delete(fn);
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
                    }
                }
            }

            if (_eventStack != null && updateDisplay)
                _eventStack.PushEvent(new AasxPluginEventReturnUpdateAnyUi() { 
                    PluginName = null, // do NOT call the plugin before rendering
                    Mode = AnyUiRenderMode.StatusToUi,
                    UseInnerGrid = true
                });
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
