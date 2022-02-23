using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        #endregion

        #region Constructors, as for WPF control
        //=============

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

            // fill given panel
            DisplaySubmodel(_panel, _uitk);
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

        private void DisplaySubmodel(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk)
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
            var modelVersion = DocumentEntity.SubmodelVersion.Default;
            var defs11 = AasxPredefinedConcepts.VDI2770v11.Static;
            if (_submodel.semanticId.Matches(defs11?.SM_ManufacturerDocumentation?.GetSemanticKey()))
                modelVersion = DocumentEntity.SubmodelVersion.V11;
            if (foundRec.ForceVersion == DocumentEntity.SubmodelVersion.V10)
                modelVersion = DocumentEntity.SubmodelVersion.V10;
            if (foundRec.ForceVersion == DocumentEntity.SubmodelVersion.V11)
                modelVersion = DocumentEntity.SubmodelVersion.V11;

            // set usage info
            var useinf = foundRec.UsageInfo;

            // what defaultLanguage
            string defaultLang = null;
            //if (theViewModel != null && theViewModel.TheSelectedLanguage > AasxLanguageHelper.LangEnum.Any)
            //    defaultLang = AasxLanguageHelper.LangEnumToISO639String[(int)theViewModel.TheSelectedLanguage];

            // make new list box items
            var its = new List<DocumentEntity>();
            if (modelVersion != DocumentEntity.SubmodelVersion.V11)
                its = ListOfDocumentEntity.ParseSubmodelForV10(
                    _package, _submodel, _options, defaultLang, 0, AasxLanguageHelper.LangEnum.Any); // selectedDocClass, selectedLanguage);
            else
                its = ListOfDocumentEntity.ParseSubmodelForV11(
                    _package, _submodel, defs11, defaultLang, 0, AasxLanguageHelper.LangEnum.Any); // selectedDocClass, selectedLanguage);

            // bring it to the panel            
            RenderPanelOutside(view, uitk, modelVersion, useinf, defaultLang, its);
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
                content: $"Document Shelf");

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(header, 0, 1,
                    margin: new AnyUiThickness(2), setHeight: 21,
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Add Entity .."),
                (o) =>
                {
                    return new AnyUiLambdaActionNone();
                });

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(header, 0, 2,
                    margin: new AnyUiThickness(2), setHeight: 21,
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Add Document .."),
                (o) =>
                {
                    return new AnyUiLambdaActionNone();
                });

            //
            // Usage info
            //

            if (usageInfo.HasContent())
                uitk.AddSmallBasicLabelTo(header, 1, 0, colSpan: 5,
                    margin: new AnyUiThickness(8, 2, 2, 2),
                    foreground: AnyUiBrushes.DarkBlue,
                    fontSize: 0.6f, setWrap: true,
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

            var cbClasses = AnyUiUIElement.RegisterControl(controls.Add(new AnyUiComboBox() {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                MinWidth = 120,
                Items = classes,
                SelectedIndex = 0
            }), (o) =>
            {
                return new AnyUiLambdaActionNone();
            });

            var cbLangs = AnyUiUIElement.RegisterControl(controls.Add(new AnyUiComboBox()
            {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                MinWidth = 100,
                Items = langs,
                SelectedIndex = 0
            }), (o) =>
            {
                return new AnyUiLambdaActionNone();
            });

            var cbVersion = AnyUiUIElement.RegisterControl(controls.Add(new AnyUiCheckBox()
            {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                Content = "latest SMT",
                IsChecked = modelVersion == DocumentEntity.SubmodelVersion.V11,
                VerticalAlignment = AnyUiVerticalAlignment.Center
            }), (o) =>
            {
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
                    skipForBrowser: true, initialScrollPosition: initialScrollPos),
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
                    var rde = RenderDocumentEntity(uitk, de);
                    if (rde != null)
                        inner.Add(rde);
                }
        }

        public AnyUiFrameworkElement RenderAnyUiDocumentEntity (
            AnyUiSmallWidgetToolkit uitk, DocumentEntity de)
        {
            // access
            if (de == null)
                return new AnyUiStackPanel();

            // make a border
            var border = new AnyUiBorder()
            {
                BorderBrush = AnyUiBrushes.DarkGray,
                BorderThickness = new AnyUiThickness(1),
                Margin = new AnyUiThickness(3)
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

            var wpCountries = sp1.Add(new AnyUiWrapPanel() { Orientation = AnyUiOrientation.Horizontal });

            foreach (var code in de.CountryCodes)
                wpCountries.Add(new AnyUiCountryFlag()
                {
                    ISO3166Code = code,
                    MinHeight = 14, MaxHeight = 14
                }); 

            var orga = sp1.Add(new AnyUiTextBlock() { 
                Text = $"{de.Organization}",
                FontSize = 0.8f,
                FontWeight = AnyUiFontWeight.Bold
            });

            // Title

            uitk.AddSmallBasicLabelTo(g, 1, 1,
                margin: new AnyUiThickness(2),
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                fontSize: 1.2f,
                content: $"{de.Title}");

            // further info

            uitk.AddSmallBasicLabelTo(g, 2, 1,
                margin: new AnyUiThickness(2),
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                fontSize: 0.8f,
                content: $"FURTHER INFO");

            // Image

            uitk.AddSmallImageTo(g, 1, 0,
                margin: new AnyUiThickness(2));

            // context menu

            //AnyUiUIElement.RegisterControl(
            //    uitk.AddSmallButtonTo(g, 2, 2, margin: new AnyUiThickness(2.0),
            //        verticalAlignment: AnyUiVerticalAlignment.Top,
            //        content: "\u22ee"),
            //        (o) =>
            //        {
            //            return new AnyUiLambdaActionNone();
            //        });


            // button [hamburger]
            uitk.AddSmallContextMenuItemTo(g, 2, 2, 
                    "\u22ee",
                    new[] {
                                    "\u270e", "Edit",
                                    "\u2702", "Delete",
                                    "\U0001F4BE", "Save as ..",
                    },
                    margin: new AnyUiThickness(2, 2, 2, 2),
                    padding: new AnyUiThickness(5, 0, 5, 0),
                    menuItemLambda: (o) =>
                    {
                        if (o is int ti)
                            switch (ti)
                            {
                                case 0:
                                    break;
                            }
                        return new AnyUiLambdaActionNone();
                    });

            // ok
            return border;
        }

        #endregion

        #region Event handling
        //=============

        public void HandleEventReturn(AasxPluginEventReturnBase evtReturn)
        {            
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
            // RenderFormInst(_panel, _uitk, _currentFormInst, initialScrollPos: _lastScrollPosition);
        }

        #endregion

        #region Button clicks
        //=============

        private AnyUiLambdaActionBase ButtonTabPanels_Click(string cmd)
        {
            if (cmd == "ButtonCancel")
            {
                // re-display (tree & panel)
                return new AnyUiLambdaActionRedrawAllElementsBase() { NextFocus = _submodel };
            }

            // no?
            return new AnyUiLambdaActionNone();
        }

#endregion
    }
}
