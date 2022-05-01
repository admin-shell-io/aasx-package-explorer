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
using AasxIntegrationBaseWpf;
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.ConceptModel;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPluginTechnicalData
{
    public class TechnicalDataAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private AdminShell.Submodel _submodel = null;
        private TechnicalDataOptions _options = null;
        private PluginEventStack _eventStack = null;
        private AnyUiStackPanel _panel = null;

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        #endregion

        #region Members to be kept for state/ update
        //=============

        protected double _lastScrollPosition = 0.0;

        protected int _selectedLangIndex = 0;
        protected string _selectedLangStr = null;

        #endregion

        #region Constructors, as for WPF control
        //=============

        public TechnicalDataAnyUiControl()
        {
        }

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel theSubmodel,
            TechnicalDataOptions theOptions,
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
            RenderFullView(_panel, _uitk, _package, _submodel, defaultLang: null);
        }

        public static TechnicalDataAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            TechnicalDataOptions options,
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

            // factory this object
            var techCntl = new TechnicalDataAnyUiControl();
            techCntl.Start(log, package, sm, options, eventStack, panel);

            // return shelf
            return techCntl;
        }

        #endregion

        #region Display Submodel
        //=============

        private void RenderFullView(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            AdminShellPackageEnv package,
            AdminShell.Submodel sm, string defaultLang = null)
        {
            // test trivial access
            if (_options == null || _submodel?.semanticId == null)
                return;

            // make sure for the right Submodel
            TechnicalDataOptionsRecord foundRec = null;
            foreach (var rec in _options.LookupAllIndexKey<TechnicalDataOptionsRecord>(
                _submodel?.semanticId?.GetAsExactlyOneKey()))
                foundRec = rec;

            if (foundRec == null)
                return;

            // retrieve the Definitions
            var theDefs = new ConceptModelZveiTechnicalData(sm);

            // render
            RenderPanelOutside(view, uitk, theDefs, package, sm, defaultLang);
        }

        protected void RenderPanelOutside (
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            ConceptModelZveiTechnicalData theDefs,
            AdminShellPackageEnv package,
            AdminShell.Submodel sm, string defaultLang = null)
        {
            // make an outer grid, very simple grid of two rows: header & body
            var outer = view.Add(uitk.AddSmallGrid(rows: 7, cols: 1, colWidths: new[] { "*" }));

            //
            // Bluebar
            //

            var bluebar = uitk.AddSmallGridTo(outer, 0, 0, 1, cols: 5, colWidths: new[] { "*", "#", "#", "#", "#" });

            bluebar.Margin = new AnyUiThickness(0);
            bluebar.Background = AnyUiBrushes.LightBlue;

            uitk.AddSmallBasicLabelTo(bluebar, 0, 0, margin: new AnyUiThickness(8, 6, 0, 6),
                foreground: AnyUiBrushes.DarkBlue,
                fontSize: 1.5f,
                setBold: true,
                content: $"Technical Data");

            // languages
            var langs = new List<string>();
            foreach (var dc in (AasxLanguageHelper.LangEnum[])Enum.GetValues(
                                    typeof(AasxLanguageHelper.LangEnum)))
                langs.Add("Lang - " + AasxLanguageHelper.LangEnumToISO639String[(int)dc]);

            if (_selectedLangStr == null)
                _selectedLangStr = defaultLang;

            AnyUiComboBox cbLang = null;
            cbLang = AnyUiUIElement.RegisterControl(
                uitk.Set(
                    uitk.AddSmallComboBoxTo(bluebar, 0, 1,
                        margin: new AnyUiThickness(2), minWidth: 120,
                        padding: new AnyUiThickness(2, 0, 2, 0),
                        items: langs.ToArray(),
                        selectedIndex : _selectedLangIndex),
                    maxHeight: 21),
                (o) =>
                {
                    if (!cbLang.SelectedIndex.HasValue)
                        return new AnyUiLambdaActionNone();
                    _selectedLangIndex = cbLang.SelectedIndex.Value;
                    _selectedLangStr = AasxLanguageHelper.LangEnumToISO639String[_selectedLangIndex];
                    return new AnyUiLambdaActionPluginUpdateAnyUi()
                    {
                        PluginName = AasxIntegrationBase.AasxPlugin.PluginName,
                        UseInnerGrid = true
                    };
                }) as AnyUiComboBox;

            //
            // Header area
            //

            // small spacer
            outer.RowDefinitions[1] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 1, 0,
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // header
            var header = uitk.AddSmallStackPanelTo(outer, 2, 0, setVertical: true);
            RenderPanelHeader(header, uitk, theDefs, package, sm, _selectedLangStr);

            //
            // Scroll area
            //

            // small spacer
            outer.RowDefinitions[3] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 3, 0, 
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // add the body, a scroll viewer
            outer.RowDefinitions[4] = new AnyUiRowDefinition(1.0, AnyUiGridUnitType.Star);
            var scroll = AnyUiUIElement.RegisterControl(
                uitk.AddSmallScrollViewerTo(outer, 4, 0,
                    horizontalScrollBarVisibility: AnyUiScrollBarVisibility.Disabled,
                    verticalScrollBarVisibility: AnyUiScrollBarVisibility.Visible,
                    flattenForTarget: AnyUiTargetPlatform.Browser, initialScrollPosition: _lastScrollPosition),
                (o) =>
                {
                    if (o is Tuple<double, double> positions)
                    {
                        _lastScrollPosition = positions.Item2;
                    }
                    return new AnyUiLambdaActionNone();
                }) as AnyUiScrollViewer;

            // content of the scroll viewer
            // need a stack panel to add inside
            var inner = new AnyUiStackPanel() { 
                Orientation = AnyUiOrientation.Vertical,
                Margin = new AnyUiThickness(2)
            };
            scroll.Content = inner;
            RenderPanelInner(inner, uitk, theDefs, package, sm, _selectedLangStr);

            //
            // Footer area
            //

            // small spacer
            outer.RowDefinitions[5] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 5, 0,
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // header
            var footer = uitk.AddSmallStackPanelTo(outer, 6, 0, setVertical: true);
            RenderPanelFooter(footer, uitk, theDefs, package, sm, _selectedLangStr);
        }

        #endregion

        #region Header
        //=============

        protected class ClassificationRecord
        {
            public string System { get; set; }
            public string Version { get; set; }
            public string ClassTxt { get; set; }
        }

        protected void RenderPanelHeader(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            ConceptModelZveiTechnicalData theDefs,
            AdminShellPackageEnv package,
            AdminShell.Submodel sm, string defaultLang = null)
        {
            // access
            if (view == null || uitk == null || sm == null)
                return;

            // make an grid with two columns, first a bit wider 
            var outer = view.Add(uitk.AddSmallGrid(rows: 4, cols: 2, colWidths: new[] { "7*", "3*" }));
            outer.ColumnDefinitions[1].MaxWidth = 200;

            //
            // General header
            //           

            // section General
            var smcGeneral = sm.submodelElements.FindFirstSemanticIdAs<AdminShell.SubmodelElementCollection>(
                theDefs.CD_GeneralInformation.GetSingleKey());
            if (smcGeneral != null)
            {
                // gather information

                var prodDesig = "" +
                    smcGeneral.value.FindFirstSemanticId(
                        theDefs.CD_ManufacturerProductDesignation.GetSingleKey(),
                        allowedTypes: AdminShell.SubmodelElement.PROP_MLP)?
                            .submodelElement?.ValueAsText(defaultLang);

                var prodCode = "" +
                    smcGeneral.value.FindFirstSemanticIdAs<AdminShell.Property>(
                        theDefs.CD_ManufacturerOrderCode.GetSingleKey())?.value;
                
                var partNumber = "" +
                    smcGeneral.value.FindFirstSemanticIdAs<AdminShell.Property>(
                        theDefs.CD_ManufacturerPartNumber.GetSingleKey())?.value;

                var manuName = "" +
                    smcGeneral.value.FindFirstSemanticIdAs<AdminShell.Property>(
                        theDefs.CD_ManufacturerName.GetSingleKey())?.value;

                AnyUiBitmapInfo imageManuLogo = null;
                if (package != null)
                {
                    var bi = AasxWpfBaseUtils.LoadBitmapImageFromPackage(
                        package,
                        smcGeneral.value.FindFirstSemanticIdAs<AdminShell.File>(
                            theDefs.CD_ManufacturerLogo.GetSingleKey())?.value
                        );
                    imageManuLogo = AnyUiHelper.CreateAnyUiBitmapInfo(bi);
                }

                // render information

                uitk.AddSmallBasicLabelTo(outer, 0, 0, margin: new AnyUiThickness(1),
                    fontSize: 1.2f,
                    setBold: true,
                    content: prodDesig);

                uitk.AddSmallBasicLabelTo(outer, 1, 0, margin: new AnyUiThickness(1),
                    fontSize: 1.6f,
                    setBold: true,
                    content: prodCode);

                uitk.AddSmallBasicLabelTo(outer, 2, 0, margin: new AnyUiThickness(1),
                    fontSize: 1.0f,
                    setBold: false,
                    content: partNumber);

                uitk.AddSmallBasicLabelTo(outer, 0, 1, margin: new AnyUiThickness(1),
                    horizontalAlignment: AnyUiHorizontalAlignment.Right,
                    horizontalContentAlignment: AnyUiHorizontalAlignment.Right,
                    fontSize: 1.0f,
                    setBold: false,
                    content: manuName);

                uitk.Set(
                    uitk.AddSmallImageTo(outer, 1, 1,
                        margin: new AnyUiThickness(2),
                        stretch: AnyUiStretch.Uniform,
                        bitmap: imageManuLogo),
                    rowSpan: 2,
                    maxHeight: 100, maxWidth: 200,
                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                    verticalAlignment: AnyUiVerticalAlignment.Stretch);

                //
                // Product Images
                //

                // gather

                var pil = new List<AnyUiBitmapInfo>();
                foreach (var pi in
                            smcGeneral.value.FindAllSemanticIdAs<AdminShell.File>(
                                theDefs.CD_ProductImage.GetSingleKey()))
                {
                    var bi = AasxWpfBaseUtils.LoadBitmapImageFromPackage(package, pi.value);
                    var imgInfo = AnyUiHelper.CreateAnyUiBitmapInfo(bi);
                    if (imgInfo != null)
                        pil.Add(imgInfo);
                }

                // ok, render?

                if (pil.Count > 0)
                {
                    // make an outer grid, very simple grid of two rows: header & body
                    var pilGrid = uitk.AddSmallGridTo(outer, 3, 0, rows: 1, cols: pil.Count /* , colWidths: new[] { "*" } */);

                    for (int i=0; i<pil.Count; i++)
                    {
                        uitk.Set(
                            uitk.AddSmallImageTo(pilGrid, 0, 0 + i,
                                margin: new AnyUiThickness(2),
                                stretch: AnyUiStretch.Uniform,
                                bitmap: pil[i]),
                            maxHeight: 100, maxWidth: 200,
                            horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                            verticalAlignment: AnyUiVerticalAlignment.Stretch);
                    }
                }
            }

            //
            // also Section: Product Classifications
            //

            var smcClassifications =
                sm.submodelElements.FindFirstSemanticIdAs<AdminShell.SubmodelElementCollection>(
                    theDefs.CD_ProductClassifications.GetSingleKey());
            if (smcClassifications != null)
            {
                // gather

                var clr = new List<ClassificationRecord>();
                foreach (var smc in
                        smcClassifications.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                            theDefs.CD_ProductClassificationItem.GetSingleKey()))
                {
                    var sys = (
                        "" +
                        smc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            theDefs.CD_ProductClassificationSystem.GetSingleKey())?.value).Trim();
                    var ver = (
                        "" +
                        smc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            theDefs.CD_ClassificationSystemVersion.GetSingleKey())?.value).Trim();
                    var cls = (
                        "" +
                        smc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            theDefs.CD_ProductClassId.GetSingleKey())?.value).Trim();

                    if (sys != "" && cls != "")
                        clr.Add(new ClassificationRecord() { System = sys, Version = ver, ClassTxt = cls });
                }

                // render
                if (clr.Count > 0)
                {
                    // make an outer grid, very simple grid of two rows: header & body
                    var clrGrid = uitk.AddSmallGridTo(outer, 3, 1, rows: clr.Count, cols: 1, colWidths: new[] { "*" } );

                    for (int i = 0; i < clr.Count; i++)
                    {
                        // instances are again a grid inside a border
                        var clri = clr[i];
                        var clrbrd = uitk.AddSmallBorderTo(clrGrid, 0 + i, 0,
                                        borderThickness: new AnyUiThickness(1.5), borderBrush: AnyUiBrushes.DarkBlue,
                                        margin: new AnyUiThickness(2),
                                        cornerRadius: 2.0);
                        var clrgi = uitk.AddSmallGrid(rows: 2, cols: 2, colWidths: new[] { "*", "#" });
                        clrbrd.Child = clrgi;

                        // labels
                        uitk.AddSmallBasicLabelTo(clrgi, 0, 0, margin: new AnyUiThickness(1),
                            fontSize: 1.1f,
                            setBold: false,
                            content: clr[i].System);

                        uitk.AddSmallBasicLabelTo(clrgi, 0, 1, margin: new AnyUiThickness(1),
                            horizontalAlignment: AnyUiHorizontalAlignment.Right,
                            horizontalContentAlignment: AnyUiHorizontalAlignment.Right,
                            fontSize: 1.1f,
                            setBold: false,
                            content: clr[i].Version);

                        uitk.AddSmallBasicLabelTo(clrgi, 1, 0, margin: new AnyUiThickness(1),
                            horizontalAlignment: AnyUiHorizontalAlignment.Center,
                            horizontalContentAlignment: AnyUiHorizontalAlignment.Center,
                            fontSize: 1.4f,
                            setBold: true, 
                            colSpan: 2,
                            content: clr[i].ClassTxt);
                    }
                }

            }
        }

        #endregion

        #region Inner
        //=============

        protected class TripleRowData
        {
            public string Heading = null;
            public AnyUiThickness HeadMargin = null;
            public double? FontSize;
            public AnyUiFontWeight FontWeight;
            public string Name = "", Semantics = "", Value = "";
        }

        protected void RenderTripleRowData(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            TripleRowData[] rows)
        {
            // access
            if (rows == null)
                return;

            // ok
            var grid = view.Add(uitk.AddSmallGrid(rows: rows.Length, cols: 3, colWidths: new[] { "*", "*", "*" }));
            for (int ri=0; ri < rows.Length; ri++)
            {
                // access
                var row = rows[ri];
                if (row == null)
                    continue;

                if (row.Value.Contains("IP67"))
                    ;

                if (row.Heading.HasContent())
                {
                    // heading
                    var hlb = uitk.AddSmallBasicLabelTo(grid, ri, 0, margin: row.HeadMargin,
                        colSpan: 3,
                        content: row.Heading);
                    hlb.FontSize = row.FontSize;
                    hlb.FontWeight = row.FontWeight;
                }
                else
                {
                    // normal row, 3 bordered cells
                    var cols = new[] { row.Name, row.Semantics, row.Value };
                    for (int ci=0; ci<3; ci++)
                    {
                        var brd = uitk.AddSmallBorderTo(grid, ri, ci,
                            margin: (ci == 0) ? new AnyUiThickness(0, -1, 0, 0) : new AnyUiThickness(-1, -1, 0, 0),
                            borderThickness: new AnyUiThickness(1.0), borderBrush: AnyUiBrushes.DarkGray);
                        brd.Child = new AnyUiSelectableTextBlock()
                        {
                            Text = cols[ci],
                            Padding = new AnyUiThickness(1),
                            FontSize = row.FontSize,
                            FontWeight = row.FontWeight,
                        };
                    }
                }
            }
        }

        protected void TableAddPropertyRows_Recurse(
            ConceptModelZveiTechnicalData theDefs, string defaultLang, AdminShellPackageEnv package,
            List<TripleRowData> rows, AdminShell.SubmodelElementWrapperCollection smwc, int depth = 0)
        {
            // access
            if (rows == null || smwc == null)
                return;

            // go element by element
            foreach (var smw in smwc)
            {
                // access
                if (smw?.submodelElement == null)
                    continue;
                var sme = smw.submodelElement;

                // prepare information about displayName, semantics unit
                var semantics = "-";
                var unit = "";
                // make up property name (1)
                var dispName = "" + sme.idShort;
                var dispNameWithCD = dispName;

                // make up semantics
                if (sme.semanticId != null)
                {
                    if (sme.semanticId.Matches(theDefs.CD_SemanticIdNotAvailable.GetSingleKey()))
                        semantics = "(not available)";
                    else
                    {
                        // the semantics display
                        semantics = "" + sme.semanticId.ToString(2);

                        // find better property name (2)
                        var cd = package?.AasEnv?.FindConceptDescription(sme.semanticId);
                        if (cd != null)
                        {
                            // unit?
                            unit = "" + cd.GetIEC61360()?.unit;

                            // names
                            var dsn = cd.GetDefaultShortName(defaultLang);
                            if (dsn != "")
                                dispNameWithCD = dsn;

                            var dpn = cd.GetDefaultPreferredName(defaultLang);
                            if (dpn != "")
                                dispNameWithCD = dpn;
                        }
                    }
                }

                // make up even better better property name (3)
                var descDef = "" + sme.description?.langString?.GetDefaultStr(defaultLang);
                if (descDef.HasContent())
                {
                    dispName = descDef;
                    dispNameWithCD = dispName;
                }

                // special function?
                if (sme is AdminShell.SubmodelElementCollection &&
                        true == sme.semanticId?.Matches(theDefs.CD_MainSection.GetSingleKey()))
                {
                    // Main Section
                    rows.Add(new TripleRowData()
                    {
                        Heading = "" + dispName,
                        HeadMargin= new AnyUiThickness(-2 + 4*depth, 6, 0, 4),
                        FontSize = 1.4f,
                        FontWeight = AnyUiFontWeight.Bold
                    }); 

                    // recurse into that (again, new group)
                    TableAddPropertyRows_Recurse(
                        theDefs, defaultLang, package, rows,
                        (sme as AdminShell.SubmodelElementCollection).value, depth + 1);
                }
                else
                if (sme is AdminShell.SubmodelElementCollection &&
                    true == sme.semanticId?.Matches(theDefs.CD_SubSection.GetSingleKey()))
                {
                    // Sub Section
                    rows.Add(new TripleRowData()
                    {
                        Heading = "" + dispName,
                        HeadMargin = new AnyUiThickness(-2 + 4*depth, 4, 0, 2),
                        FontSize = 1.2f,
                        FontWeight = AnyUiFontWeight.Bold
                    });

                    // recurse into that
                    TableAddPropertyRows_Recurse(
                        theDefs, defaultLang, package, rows,
                        (sme as AdminShell.SubmodelElementCollection).value, depth + 1);
                }
                else
                if (sme is AdminShell.Property || sme is AdminShell.MultiLanguageProperty || sme is AdminShell.Range)
                {
                    rows.Add(new TripleRowData()
                    {
                        Name = dispNameWithCD,
                        Semantics = semantics,
                        Value = "" + sme.ValueAsText(defaultLang) + " " + unit
                    });
                }
            }
        }

        protected void RenderPanelInner(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            ConceptModelZveiTechnicalData theDefs,
            AdminShellPackageEnv package,
            AdminShell.Submodel sm, string defaultLang = null)
        {
            // access
            if (view == null || uitk == null || sm == null)
                return;

            // section Properties
            var smcProps =
                sm.submodelElements.FindFirstSemanticIdAs<AdminShell.SubmodelElementCollection>(
                    theDefs.CD_TechnicalProperties.GetSingleKey());
            if (smcProps == null)
                return;

            // rows and header
            var rows = new List<TripleRowData>();
            rows.Add(new TripleRowData()
            {
                Name = "Property",
                Semantics = "Semantics",
                Value = "Value",
                FontWeight = AnyUiFontWeight.Bold
            });

            // recurse
            TableAddPropertyRows_Recurse(theDefs, defaultLang, package, rows, smcProps.value);

            // render
            RenderTripleRowData(view, uitk, rows.ToArray());
        }

        #endregion

        #region Footer
        //=============

        protected void RenderPanelFooter(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            ConceptModelZveiTechnicalData theDefs,
            AdminShellPackageEnv package,
            AdminShell.Submodel sm, string defaultLang = null)
        {
            // access
            if (view == null || uitk == null || sm == null)
                return;

            // gather data for footer
            var validDate = "";
            var tsl = new List<string>();

            var smcFurther = sm.submodelElements.FindFirstSemanticIdAs<AdminShell.SubmodelElementCollection>(
                theDefs.CD_FurtherInformation.GetSingleKey());
            if (smcFurther != null)
            {
                // single items
                validDate = "" + smcFurther.value.FindFirstSemanticIdAs<AdminShell.Property>(
                    theDefs.CD_ValidDate.GetSingleKey())?.value;

                // Lines
                foreach (var smw in
                    smcFurther.value.FindAllSemanticId(
                        theDefs.CD_TextStatement.GetSingleKey(), allowedTypes: AdminShell.SubmodelElement.PROP_MLP))
                    tsl.Add("" + smw?.submodelElement?.ValueAsText(defaultLang));
            }

            // make an grid with two columns, first a bit wider 
            var outer = view.Add(uitk.AddSmallGrid(rows: 4, cols: 2, 
                            colWidths: new[] { "*", "#" }, background: AnyUiBrushes.White));
            outer.ColumnDefinitions[1].MaxWidth = 200;

            // fill
            uitk.AddSmallBasicLabelTo(outer, 0, 1,
                fontSize: 1.0f,
                content: validDate);

            for (int i=0; i<tsl.Count; i++)
                uitk.AddSmallBasicLabelTo(outer, 0 + i, 0,
                fontSize: 1.0f,
                content: tsl[i]);
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

            // the default: the full shelf
            RenderFullView(_panel, _uitk, _package, _submodel, defaultLang: null);
        }

        #endregion

        #region Callbacks
        //===============


        #endregion

        #region Utilities
        //===============


        #endregion
    }
}
