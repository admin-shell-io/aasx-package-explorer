/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Numerics;
using System.Windows.Forms;
using QRCoder;
using ImageMagick;
using System.DirectoryServices.ActiveDirectory;


namespace AasxPluginDigitalNameplate
{
    public class NameplateAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private Aas.Submodel _submodel = null;
        private DigitalNameplateOptions _options = null;
        private PluginEventStack _eventStack = null;
        private PluginSessionBase _session = null;
        private AnyUiStackPanel _panel = null;
        private AnyUiContextPlusDialogs _displayContext = null;
        private PluginOperationContextBase _opContext = null;
        private AasxPluginBase _plugin = null;

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        private string _renderedVersion = "";

        protected NameplateData _nameplateData = null;

        protected DigitalNameplateOptionsRecord _foundRecord = null;

        protected const int MarkingsPerRow = 5;

        #endregion       

        #region Constructors, as for WPF control
        //=============

        public NameplateAnyUiControl()
        {
        }

        public void Dispose()
        {
        }

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            Aas.Submodel theSubmodel,
            DigitalNameplateOptions theOptions,
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

            // fill given panel
            RenderFullNameplate(_panel, _uitk);
        }

        public static NameplateAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            DigitalNameplateOptions options,
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
            var shelfCntl = new NameplateAnyUiControl();
            shelfCntl.Start(log, package, sm, options, eventStack, session, panel, opContext, cdp, plugin);

            // return shelf
            return shelfCntl;
        }

        #endregion

        #region Display Submodel
        //=============

        private void RenderFullNameplate(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk)
        {
            // test trivial access
            if (_options == null || _submodel?.SemanticId == null)
                return;

            // make sure for the right Submodel            
            foreach (var rec in _options.LookupAllIndexKey<DigitalNameplateOptionsRecord>(
                _submodel?.SemanticId?.GetAsExactlyOneKey()))
                _foundRecord = rec;

            if (_foundRecord == null)
                return;

            // acquire information
            if (_foundRecord.Parser == DigitalNameplateOptionsRecord.ParserEnum.V10)
                _nameplateData = NameplateData.ParseSubmodelForV10(_package, _submodel, _options);

            if (_foundRecord.Parser == DigitalNameplateOptionsRecord.ParserEnum.V20)
                _nameplateData = NameplateData.ParseSubmodelForV20(_package, _submodel, _options);

            // bring it to the panel            
            RenderPanelOutside(view, uitk, _renderedVersion);
        }

        protected double _lastScrollPosition = 0.0;

        protected void RenderPanelOutside(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            string modelVersion,
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
                content: $"Digital Nameplate");

            if (_opContext?.IsDisplayModeEditOrAdd == true)
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(header, 0, 1,
                        margin: new AnyUiThickness(2), setHeight: 21,
                        padding: new AnyUiThickness(2, 0, 2, 0),
                        content: "Add Entity .."),
                    (o) =>
                    {
                        // to do

                        //redisplay
                        PushUpdateEvent();
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

            var stackGrid = uitk.AddSmallGrid(4, 1, colWidths: new[] { "*" });
            scroll.Content = stackGrid;

            // add a small explanation
            var gridExpl = uitk.AddSmallGridTo(stackGrid, 0, 0, 1, 2, colWidths: new[] { "*", "#" }, rowHeights: new[] { "#" },
                margin: new AnyUiThickness(0, 0, 0, 10));

            // required for HTML rendering
            gridExpl.ColumnDefinitions[1].MinWidth = 100;

            uitk.AddSmallBasicLabelTo(gridExpl, 0, 0,
                background: AnyUiBrushes.LightBlue,
                content: "" + _foundRecord?.Explanation,
                padding: new AnyUiThickness(8, 5, 8, 5),
                textWrapping: AnyUiTextWrapping.Wrap,
                verticalAlignment: AnyUiVerticalAlignment.Stretch,
                verticalContentAlignment: AnyUiVerticalAlignment.Top);

            var imgIec = uitk.Set(
                AddNamedImage(uitk, gridExpl, 0, 1, "iec-logo.png", stretch: AnyUiStretch.UniformToFill),
                    margin: new AnyUiThickness(10, 0, 0, 0),
                    verticalAlignment: AnyUiVerticalAlignment.Stretch,
                    maxHeight: 100);

            // add the nameplate itself
            var gridNp = uitk.Set(RenderAnyUiNameplateData(uitk, _nameplateData),
                margin: new AnyUiThickness(18, 10, 18, 10));
            AnyUiGrid.SetRow(gridNp, 1);
            AnyUiGrid.SetColumn(gridNp, 0);
            stackGrid.Add(gridNp);

            // add report area
            if (_statements != null && _statements.Count > 0)
            {
                var gridReport = uitk.AddSmallGridTo(stackGrid, 2, 0, 1 + _statements.Count, 1,
                    margin: new AnyUiThickness(0, 10, 0, 0));

                uitk.AddSmallBasicLabelTo(gridReport, 0, 0,
                    content: "Individual elements of the Digital Nameplate:",
                    fontSize: 1.4f, setBold: true,
                    foreground: AnyUiBrushes.DarkGray);

                for (int i = 0; i < _statements.Count; i++)
                    AddReportLineFromIndex(uitk, gridReport, 1 + i, 0, _statements[i]);
            }

            // render the innerts of the scroll viewer
            stackGrid.Background = AnyUiBrushes.LightGray;
        }

        /// <summary>
        /// Allows flagging information w.r.t. the intention of the Submodel
        /// </summary>
        public enum Quality { Free, Good, Warn, Error };

        public AnyUiBrush BrushfromQuality(Quality qual)
        {
            switch (qual)
            {
                case Quality.Free:
                    return new AnyUiBrush(0xbbfcfcfc);
                case Quality.Good:
                    return new AnyUiBrush(0x7389ff79);
                case Quality.Warn:
                    return new AnyUiBrush(0xb6f2fd91);
                default:
                    return new AnyUiBrush(0x73ff7979);
            }
        }

        /// <summary>
        /// Attaches some more information to an indexed information in the nameplate.
        /// </summary>
        public class IndexStatement
        {
            public string Index = "";
            public Quality Quality = Quality.Free;
            public string Description = null;
            public string Statement = null;

            public IndexStatement() { }

            public IndexStatement(Quality quality, string description = null, string statement = null)
            {
                Quality = quality;
                Description = description;
                Statement = statement;
            }

            public IndexStatement Set(
                string description = null)
            {
                if (description != null)
                    Description = description;
                return this;
            }
        }

        protected List<IndexStatement> _statements = new List<IndexStatement>();

        protected AnyUiGrid AddGridWithIndex(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int row, int col,
            string index,
            int? rowSpan = null, int? colSpan = null,
            IndexStatement statement = null)
        {
            // keep statement
            if (_statements != null && statement != null && index?.HasContent() == true)
            {
                var stm = statement.Copy();
                stm.Index = index;
                _statements.Add(stm);
            }

            // make a outer grid
            var g2 = uitk.AddSmallGridTo(grid, row, col,
                1, 2,
                rowHeights: new[] { "*" }, colWidths: new[] { "24:", "*" },
                margin: new AnyUiThickness(2));

            // very small label
            uitk.AddSmallBasicLabelTo(g2, 0, 0,
                content: index,
                fontSize: 0.9f,
                margin: new AnyUiThickness(0, 0, 6, 0),
                background: (statement != null) ? BrushfromQuality(statement.Quality) : new AnyUiBrush(0x40ffffff),
                verticalAlignment: AnyUiVerticalAlignment.Top,
                verticalContentAlignment: AnyUiVerticalAlignment.Stretch,
                horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                horizontalContentAlignment: AnyUiHorizontalAlignment.Center);

            if (rowSpan.HasValue)
                g2.GridRowSpan = rowSpan.Value;

            if (colSpan.HasValue)
                g2.GridColumnSpan = colSpan.Value;

            return g2;
        }

        protected AnyUiGrid AddIndexTextBlock(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int row, int col,
            string index,
            string text,
            double? fontSize = null,
            int? rowSpan = null, int? colSpan = null,
            IndexStatement statement = null,
            double? lineHeight = null)
        {
            // make a outer grid
            var g2 = AddGridWithIndex(
                uitk, grid, row, col, index,
                rowSpan: rowSpan, colSpan: colSpan,
                statement: statement);

            var tb = uitk.AddSmallBasicLabelTo(g2, 0, 1,
                content: text, /* multiLine: true, */ fontSize: fontSize,
                margin: new AnyUiThickness(0, 2, 0, 0),
                background: AnyUiBrushes.Transparent, // new AnyUiBrush(0x40ffffff),
                textWrapping: AnyUiTextWrapping.Wrap,
                verticalAlignment: AnyUiVerticalAlignment.Stretch,
                verticalContentAlignment: AnyUiVerticalAlignment.Top);

            tb.MaxHeight = 150;
            tb.LineHeightPercent = lineHeight;

            return g2;
        }

        protected AnyUiImage AddNamedImage(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int row, int col,
            string mediaPath,
            AnyUiStretch? stretch = null)
        {
            // access
            if (mediaPath.HasContent() != true)
                return null;

            AnyUiBitmapInfo bitmapInfo = null;
            try
            {
                // figure our relative path
                var basePath = "" + System.IO.Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly()?.Location);

                // path of image to load
                var fn = Path.Combine("AasxPluginDigitalNameplate.media", mediaPath);
                var il = fn.Replace("\\", "/");
                var imagePath = Path.Combine(basePath, il);

                // load
                bitmapInfo = AnyUiGdiHelper.CreateAnyUiBitmapInfo(imagePath);
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            var res = (bitmapInfo == null) ? null :
                uitk.AddSmallImageTo(grid, row, col,
                    stretch: stretch,
                    bitmap: bitmapInfo);

            return res;
        }

        protected AnyUiImage AddQrCode(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int row, int col,
            string payload,
            AnyUiStretch? stretch = null)
        {
            // access
            if (payload.HasContent() != true)
                return null;

            AnyUiBitmapInfo bitmapInfo = null;
            try
            {
                // this is absurldy complex
                // see: https://github.com/codebude/QRCoder/wiki/
                // Advanced-usage---QR-Code-renderers#25-pngbyteqrcode-renderer-in-detail
                using (var qrGenerator = new QRCodeGenerator())
                {
                    using (var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q))
                    {
                        using (var code = new PngByteQRCode(qrCodeData))
                        {
                            byte[] pngBytes = code.GetGraphic(20,
                                darkColorRgba: new byte[] { 0, 0, 0, 0xff },
                                lightColorRgba: new byte[] { 0xff, 0xff, 0xff, 0 });

                            using (MemoryStream ms = new MemoryStream())
                            {
                                // save as memory stream 
                                ms.Write(pngBytes, 0, pngBytes.Length);
                                ms.Position = 0;

                                // rewinded; make ImageMagick
                                MagickFactory f = new MagickFactory();
                                var img = new MagickImage(f.Image.Create(ms));

                                // make intermediate format from it
                                // absurldy, this will AGAIN create an PNG for the HMTL side ..
                                bitmapInfo = AnyUiGdiHelper.CreateAnyUiBitmapInfo(img);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            var res = (bitmapInfo == null) ? null :
                uitk.AddSmallImageTo(grid, row, col,
                    stretch: stretch,
                    bitmap: bitmapInfo);

            return res;
        }

        protected AnyUiImage AddAasxFileImage(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int row, int col,
            Aas.IFile aasFile,
            AnyUiStretch? stretch = null)
        {
            // access
            if (aasFile?.Value.HasContent() != true)
                return null;

            AnyUiBitmapInfo bitmapInfo = null;
            try
            {
                bitmapInfo = AnyUiGdiHelper.LoadBitmapInfoFromPackage(_package, aasFile.Value);
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            var res = (bitmapInfo == null) ? null :
                uitk.AddSmallImageTo(grid, row, col,
                    stretch: stretch,
                    bitmap: bitmapInfo);

            return res;
        }

        protected AnyUiGrid AddReportLineFromIndex(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int row, int col,
            IndexStatement index)
        {
            // access
            if (grid == null || index == null)
                return null;

            // make a outer grid
            var g2 = uitk.AddSmallGridTo(grid, row, col,
                2, 2,
                rowHeights: new[] { "#", "#" }, colWidths: new[] { "24:", "*" },
                margin: new AnyUiThickness(2),
                background: AnyUiBrushes.LightGray);

            // very small label
            uitk.AddSmallBasicLabelTo(g2, 0, 0,
                content: index.Index,
                fontSize: 0.9f,
                margin: new AnyUiThickness(0, 4, 6, 0),
                background: (index != null) ? BrushfromQuality(index.Quality) : new AnyUiBrush(0x40ffffff),
                verticalAlignment: AnyUiVerticalAlignment.Top,
                verticalContentAlignment: AnyUiVerticalAlignment.Stretch,
                horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                horizontalContentAlignment: AnyUiHorizontalAlignment.Center);

            // description
            if (index.Description != null)
                uitk.AddSmallBasicLabelTo(g2, 0, 1,
                    content: index.Description,
                    fontSize: 1.0f,
                    margin: new AnyUiThickness(0, 4, 0, 2),
                    background: AnyUiBrushes.LightGray,
                    textWrapping: AnyUiTextWrapping.Wrap,
                    verticalAlignment: AnyUiVerticalAlignment.Top,
                    verticalContentAlignment: AnyUiVerticalAlignment.Stretch,
                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                    horizontalContentAlignment: AnyUiHorizontalAlignment.Left);

            // statement
            if (index.Statement != null)
                uitk.AddSmallBasicLabelTo(g2, 1, 1,
                    content: index.Statement,
                    fontSize: 1.0f,
                    margin: new AnyUiThickness(0, 4, 0, 2),
                    background: AnyUiBrushes.LightGray,
                    textWrapping: AnyUiTextWrapping.Wrap,
                    verticalAlignment: AnyUiVerticalAlignment.Top,
                    verticalContentAlignment: AnyUiVerticalAlignment.Stretch,
                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                    horizontalContentAlignment: AnyUiHorizontalAlignment.Left);

            return g2;
        }

#if backup

        public AnyUiFrameworkElement RenderAnyUiNameplateData(
            AnyUiSmallWidgetToolkit uitk, NameplateData plate)
        {
            // access
            if (plate == null)
                return new AnyUiStackPanel();

            // make a outer grid
            var grid = uitk.AddSmallGrid(8, 8,
                rowHeights: new[] { "#", "#", "#", "#", "#", "#", "#", "#" },
                colWidths: new[] { "14:", "14:", "*", "*", "*", "*", "14:", "14:" }, 
                margin: new AnyUiThickness(2));

            // Background image

            uitk.Set(
                AddNamedImage(uitk, grid, 0, 0, "metal-plate3.png", stretch: AnyUiStretch.Fill),
                rowSpan: 8,
                colSpan: 8,
                horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                verticalAlignment: AnyUiVerticalAlignment.Stretch,
                skipForTarget: AnyUiTargetPlatform.Browser);

            // Borders

            uitk.Set(
                uitk.AddSmallBorderTo(grid, 0, 1, rowSpan: 8, colSpan: 6,
                    margin: new AnyUiThickness(3, 3, 3, 3),
                    background: AnyUiBrushes.Transparent,
                    borderBrush: AnyUiBrushes.Black,
                    borderThickness: new AnyUiThickness(1.5),
                    cornerRadius: 4.0));

            var screwPos = new[] { 0, 0, 0, 7, 7, 0, 7, 7 };
            for (int i = 0; i < 4; i++)
            {
                if (false)
                {
                    // screw by border
                    var brd = uitk.Set(
                        uitk.AddSmallBorderTo(grid, screwPos[2 * i + 0], screwPos[2 * i + 1],
                            margin: new AnyUiThickness(2),
                            background: AnyUiBrushes.Transparent,
                            borderBrush: AnyUiBrushes.Black,
                            borderThickness: new AnyUiThickness(1.0),
                            cornerRadius: 4.0),
                        skipForTarget: AnyUiTargetPlatform.Browser);
                    brd.Height = 6;
                    brd.MaxHeight = 12;
                }
                else
                {
                    // screw by image
                    uitk.Set(
                        AddNamedImage(
                            uitk, grid, screwPos[2 * i + 0], screwPos[2 * i + 1],
                            "screw-black2.png", stretch: AnyUiStretch.None));
                }
            }

            //
            // By which id is the product identified ("big" id)?
            //

            var prodBigStr = "";
            var prodBigStmt = new IndexStatement()
            {
                Description = "ManufacturerProductFamily or ManufacturerProductType:\n" +
                    "Clear identification for the asset responsible, which asset is represented."
            };

            if (plate.ManufacturerProductType?.HasContent() == true)
            {
                prodBigStr = plate.ManufacturerProductType;
                prodBigStmt.Quality = Quality.Good;
                prodBigStmt.Statement = "ManufacturerProductType (V2.0) is recommended over " +
                    "ManufacturerProductType.";
            }
            else
            if (plate.ManufacturerProductFamily?.HasContent() == true)
            {
                prodBigStr = plate.ManufacturerProductFamily;
                prodBigStmt.Quality = Quality.Warn;
                prodBigStmt.Statement = "If possible, use ManufacturerProductType (V2.0) instead of " +
                        "ManufacturerProductType.";
            }
            else
            {
                prodBigStr = plate.ManufacturerProductFamily;
                prodBigStmt.Quality = Quality.Error;
                prodBigStmt.Statement = "Either the product type or at least the product family shall be given.";
            }

            AddIndexTextBlock(uitk, grid, 1, 2, colSpan: 2,
                index: "(10)", fontSize: 2.0f,
                text: prodBigStr,
                statement: prodBigStmt);

            // Manu Name

            var desc = "ManufacturerName:\n" +
                "Legally valid designation of the natural or judicial person which places " +
                "the asset on the market. Typically, the company name including the legal form";

            AddIndexTextBlock(uitk, grid, 1, 4,
                index: "(1)", fontSize: 2.0f,
                text: "" + plate.ManufacturerName,
                statement: (plate.ManufacturerName?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, desc, "Is given.")
                    : new IndexStatement(Quality.Error, desc, "ManufacturerName needs to be given!"));

            // Logo

            var gridLogo = AddGridWithIndex(uitk, grid, 1, 5,
                index: "(2)",
                statement: ((plate.CompanyLogo?.Value?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Error, statement: "Should be given."))
                    .Set(description: "CompanyLogo:\n" +
                        "A graphic mark used to represent a organisation or product. " +
                        "Helpful to users to assess the validity of representation of the asset."));

            if (plate.CompanyLogo != null)
            {
                AddAasxFileImage(uitk, gridLogo, 0, 1, plate.CompanyLogo, AnyUiStretch.Uniform);
            }

            // Product details

            var pdGrid = uitk.Set(
                uitk.AddSmallGridTo(grid, 2, 2, 10, 1),
                rowSpan: 2);

            AddIndexTextBlock(uitk, pdGrid, 0, 0, index: "(11)", text: "" + plate.ManufacturerProductRoot,
                statement: ((plate.ManufacturerProductRoot?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given."))
                    .Set(description: "ManufacturerProductRoot:\n" +
                        "Top level of the product hierarchy of a organisation. Typically denotes a category " +
                        "of assets, such as: \"flow meter\"."));

            AddIndexTextBlock(uitk, pdGrid, 1, 0, index: "(12)", text: "" + plate.ManufacturerProductFamily,
                statement: ((plate.ManufacturerProductFamily?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given."))
                    .Set(description: "ManufacturerProductFamily:\n" +
                        "2nd level of the product hierarchy of a organisation. Typically denotes a specific " +
                        "type family of asset, such as: \"ABC\", which is within the given " +
                        "top level (category)."));

            AddIndexTextBlock(uitk, pdGrid, 2, 0, index: "(13)", text: "" + plate.ManufacturerProductType,
                statement: ((plate.ManufacturerProductRoot?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given."))
                    .Set(description: "ManufacturerProductType:\n" +
                        "Specific product type of an asset. Typically specific enough to order " +
                        "a spare or replacement part."));

            AddIndexTextBlock(uitk, pdGrid, 3, 0, index: "(14)", text: "" + plate.OrderCodeOfManufacturer,
                statement: ((plate.ManufacturerProductRoot?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given."))
                    .Set(description: "OrderCodeOfManufacturer:\n" +
                        "Unique combination of numbers and letters given by the manufacturer precisely " +
                        "defining the asset type. Full information given to order an exact replacement " +
                        "of the asset."));

            AddIndexTextBlock(uitk, pdGrid, 4, 0, index: "(15)",
                text: "" + plate.ProductArticleNumberOfManufacturer,
                statement: ((plate.ManufacturerProductRoot?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given."))
                    .Set(description: "ProductArticleNumberOfManufacturer:\n" +
                        "Unique product identifier of the manufacturerTop defined by the ordering " +
                        "system of the manufacturer. Typically known as part number."));

            AddIndexTextBlock(uitk, pdGrid, 5, 0, index: "(16)", text: "" + plate.SerialNumber,
                statement: ((plate.ManufacturerProductRoot?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn,
                        statement: "Should be given, if asset is an product instance."))
                    .Set(description: "SerialNumber:\n" +
                        "Unique combination of numbers and letters used to identify the " +
                        "product (asset) instance once it has been manufactured. Does not need to be " +
                        "worldwide unqiue, only for the manufacturer."));

            // Product designation

            AddIndexTextBlock(uitk, grid, 2, 3, rowSpan: 2,
                index: "(17)",
                text: "" + plate.ManufacturerProductDesignation,
                statement: ((plate.ManufacturerProductDesignation?.HasContent() == true
                             && plate.ManufacturerProductDesignation.Length < 50)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given and concise."))
                    .Set(description: "ManufacturerProductDesignation:\n" +
                        "Short description of the product (short text). Ideally not longer than " +
                        "50 characters."));

            // Contact information

            AddIndexTextBlock(uitk, grid, 2, 4, colSpan: 2,
                index: "(3)",
                text: "" + string.Join(" \u2022 ", (plate.ContactInformation ?? (new[] { "-" }).ToList())),
                statement: ((plate.ContactInformation != null && plate.ContactInformation.Count >= 2)
                ? new IndexStatement(Quality.Good, statement: "Is given.")
                : new IndexStatement(Quality.Warn, statement: "Should be given."))
                .Set(description: "ContactInformation:\n" +
                    "At least, the following information needs to be given: Street, Zipcode, CityTown, " +
                    "NationalCode."));

            // Explosion safety

            if (plate.ExplSafetyStr != null)
                AddIndexTextBlock(uitk, grid, 3, 4, colSpan: 2,
                    index: "(30)",
                    text: "" + plate.ExplSafetyStr);

            // Markings

            if (plate.Markings != null && plate.Markings.Count > 0)
            {
                // indexed grid
                var gridMarksOut = AddGridWithIndex(uitk, grid, 4, 2, rowSpan: 3, colSpan: 2,
                        index: "(20)");

                // raster
                var numMarks = plate.Markings.Count;
                var numCol = numMarks;
                var numRow = 1;
                if (numCol > MarkingsPerRow)
                {
                    numRow = 1 + ((numMarks - 1) / MarkingsPerRow);
                    numCol = MarkingsPerRow;
                }
                
                // wrap within raster
                var wrapPanel = new AnyUiWrapPanel();
                wrapPanel.VerticalAlignment = AnyUiVerticalAlignment.Bottom;
                AnyUiGrid.SetRow(wrapPanel, 0);
                AnyUiGrid.SetColumn(wrapPanel, 1);
                gridMarksOut.Add(wrapPanel);

                // fill raster
                for (int i = 0; i < numMarks; i++)
                {
                    // access
                    var mark = plate.Markings[i];
                    if (mark == null)
                        continue;

                    int row = i / MarkingsPerRow;
                    int col = i % MarkingsPerRow;

                    // render EITHER image or text
                    AnyUiBitmapInfo markImg = null;
                    if (_package != null)
                        markImg = AnyUiGdiHelper.LoadBitmapInfoFromPackage(_package, mark.File?.Value);

                    if (markImg != null)
                    {
                        // render IMAGE
                        var img = uitk.Set(
                            new AnyUiImage() { Stretch = AnyUiStretch.Uniform, BitmapInfo = markImg },
                            margin: new AnyUiThickness(0, 0, 4, 4),
                            horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                            verticalAlignment: AnyUiVerticalAlignment.Stretch);
                        wrapPanel.Add(img);

                        img.MaxHeight = 70;
                        img.MaxWidth = 100;
                    }
                    else
                    {
                        // render TEXT
                        var tb = new AnyUiTextBlock()
                        {
                            Text = "" + mark.Name,
                            FontSize = 1.4,
                            Margin = new AnyUiThickness(0, 0, 4, 4),
                            Padding = new AnyUiThickness(4),
                            Background = AnyUiBrushes.White,
                            TextWrapping = AnyUiTextWrapping.Wrap,
                            VerticalAlignment = AnyUiVerticalAlignment.Stretch,
                            VerticalContentAlignment = AnyUiVerticalAlignment.Center
                        };
                        wrapPanel.Add(tb);

                        tb.MaxHeight = 70;
                        tb.MaxWidth = 100;
                    }
                }
            }

            // versions

            var vers = new List<string>();
            if (plate.HardwareVersion?.HasContent() == true)
                vers.Add("H/W: " + plate.HardwareVersion);
            if (plate.FirmwareVersion?.HasContent() == true)
                vers.Add("F/W: " + plate.FirmwareVersion);
            if (plate.SoftwareVersion?.HasContent() == true)
                vers.Add("S/W: " + plate.SoftwareVersion);

            var versStr = string.Join("\n", vers);

            AddIndexTextBlock(uitk, grid, 4, 4,
                index: "(18)",
                text: "" + versStr,
                statement: ((versStr.HasContent())
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Error, statement: "Should be given."))
                    .Set(description: "(Hard|Firm|Soft)wareVersion:\n" +
                        "Hardware versions often lead to a new type or order code of the asset. " +
                        "Firmware is directly supplied by the device (asset), while software is used " +
                        "by the device (asset)."));

            // Year of construction

            AddIndexTextBlock(uitk, grid, 5, 4,
                index: "(4)",
                text: "" + plate.YearOfConstruction,
                statement: ((plate.YearOfConstruction?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Error, statement: "Shall be given."))
                    .Set(description: "YearOfConstruction:\n" +
                        "Year (4 digits) as completion of manufacture of asset."));

            // Date of manufacture

            AddIndexTextBlock(uitk, grid, 6, 4,
                index: "(5)",
                text: "" + plate.DateOfManufacture,
                statement: ((plate.DateOfManufacture?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given."))
                    .Set(description: "SerialNumber:\n" +
                        "Date from which the production and / or development process is completed or " +
                        "from which a service is provided completely."));

            // URI of product (QR)

            if (plate.URIOfTheProduct?.HasContent() == true)
            {
                var gridQr = AddGridWithIndex(uitk, grid, 4, 5, rowSpan: 3,
                    index: "(6)",
                    statement: ((plate.URIOfTheProduct?.HasContent() == true)
                        ? new IndexStatement(Quality.Good, statement: "Is given.")
                        : new IndexStatement(Quality.Error, statement: "Shall be given."))
                        .Set(description: "URIOfTheProduct:\n" +
                            "Unique global identification of the product (asset) using an " +
                            "universal resource identifier (URI) according IEC 61406."));

                // AddQrCode(uitk, gridQr, 0, 1, plate.URIOfTheProduct, stretch: AnyUiStretch.Uniform);
            }

            // ok
            return grid;
        }

#endif

        public AnyUiFrameworkElement RenderAnyUiNameplateData(
            AnyUiSmallWidgetToolkit uitk, NameplateData plate)
        {
            // access
            if (plate == null)
                return new AnyUiStackPanel();

            // make a outer grid
            var plateGrid = uitk.AddSmallGrid(3, 5,
                rowHeights: new[] { "#", "#", "#" },
                colWidths: new[] { "14:", "14:", "*", "14:", "14:" },
                margin: new AnyUiThickness(2));

            // Background image

            var backImg = uitk.Set(
                AddNamedImage(uitk, plateGrid, 0, 0, "metal-plate3.png", stretch: AnyUiStretch.Fill),
                rowSpan: 3,
                colSpan: 5,
                horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                verticalAlignment: AnyUiVerticalAlignment.Stretch,
                skipForTarget: AnyUiTargetPlatform.Browser);

            plateGrid.BackgroundImageHtml = backImg;

            // Borders

            var border = uitk.Set(
                uitk.AddSmallBorderTo(plateGrid, 1, 2, rowSpan: 1, colSpan: 1,
                    margin: new AnyUiThickness(3, 3, 3, 3),
                    background: AnyUiBrushes.Transparent,
                    borderBrush: AnyUiBrushes.Black,
                    borderThickness: new AnyUiThickness(1.5),
                    cornerRadius: 4.0));

            var screwPos = new[] { 0, 0, 0, 4, 2, 0, 2, 4 };
            for (int i = 0; i < 4; i++)
            {
                if (false)
                {
                    // screw by border
                    var brd = uitk.Set(
                        uitk.AddSmallBorderTo(plateGrid, screwPos[2 * i + 0], screwPos[2 * i + 1],
                            margin: new AnyUiThickness(2),
                            background: AnyUiBrushes.Transparent,
                            borderBrush: AnyUiBrushes.Black,
                            borderThickness: new AnyUiThickness(1.0),
                            cornerRadius: 4.0),
                        skipForTarget: AnyUiTargetPlatform.Browser);
                    brd.Height = 6;
                    brd.MaxHeight = 12;
                }
                else
                {
                    // screw by image
                    uitk.Set(
                        AddNamedImage(
                            uitk, plateGrid, screwPos[2 * i + 0], screwPos[2 * i + 1],
                            "screw-black2.png", stretch: AnyUiStretch.None));
                }
            }

            // make a outer grid
            var grid = uitk.AddSmallGrid(6, 4,
                rowHeights: new[] { "#", "#", "#", "#", "#", "#" },
                colWidths: new[] { "*", "*", "*", "*" },
                margin: new AnyUiThickness(2));

            border.Child = grid;

            //
            // By which id is the product identified ("big" id)?
            //

            var prodBigStr = "";
            var prodBigStmt = new IndexStatement()
            {
                Description = "ManufacturerProductFamily or ManufacturerProductType:\n" +
                    "Clear identification for the asset responsible, which asset is represented."
            };

            if (plate.ManufacturerProductType?.HasContent() == true)
            {
                prodBigStr = plate.ManufacturerProductType;
                prodBigStmt.Quality = Quality.Good;
                prodBigStmt.Statement = "ManufacturerProductType (V2.0) is recommended over " +
                    "ManufacturerProductType.";
            }
            else
            if (plate.ManufacturerProductFamily?.HasContent() == true)
            {
                prodBigStr = plate.ManufacturerProductFamily;
                prodBigStmt.Quality = Quality.Warn;
                prodBigStmt.Statement = "If possible, use ManufacturerProductType (V2.0) instead of " +
                        "ManufacturerProductType.";
            }
            else
            {
                prodBigStr = plate.ManufacturerProductFamily;
                prodBigStmt.Quality = Quality.Error;
                prodBigStmt.Statement = "Either the product type or at least the product family shall be given.";
            }

            AddIndexTextBlock(uitk, grid, 0, 0, colSpan: 2,
                index: "(10)", fontSize: 2.0f,
                lineHeight: 150,
                text: prodBigStr,
                statement: prodBigStmt);

            // Manu Name

            var desc = "ManufacturerName:\n" +
                "Legally valid designation of the natural or judicial person which places " +
                "the asset on the market. Typically, the company name including the legal form";

            AddIndexTextBlock(uitk, grid, 0, 2,
                index: "(1)", fontSize: 2.0f,
                lineHeight: 150,
                text: "" + plate.ManufacturerName,
                statement: (plate.ManufacturerName?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, desc, "Is given.")
                    : new IndexStatement(Quality.Error, desc, "ManufacturerName needs to be given!"));

            // Logo

            var gridLogo = AddGridWithIndex(uitk, grid, 0, 3,
                index: "(2)",
                statement: ((plate.CompanyLogo?.Value?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Error, statement: "Should be given."))
                    .Set(description: "CompanyLogo:\n" +
                        "A graphic mark used to represent a organisation or product. " +
                        "Helpful to users to assess the validity of representation of the asset."));

            if (plate.CompanyLogo != null)
            {
                AddAasxFileImage(uitk, gridLogo, 0, 1, plate.CompanyLogo, AnyUiStretch.Uniform);
            }

            // Product details

            var pdGrid = uitk.Set(
                uitk.AddSmallGridTo(grid, 1, 0, 10, 1),
                rowSpan: 2);

            AddIndexTextBlock(uitk, pdGrid, 0, 0, index: "(11)", text: "" + plate.ManufacturerProductRoot,
                lineHeight: 120,
                statement: ((plate.ManufacturerProductRoot?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given."))
                    .Set(description: "ManufacturerProductRoot:\n" +
                        "Top level of the product hierarchy of a organisation. Typically denotes a category " +
                        "of assets, such as: \"flow meter\"."));

            AddIndexTextBlock(uitk, pdGrid, 1, 0, index: "(12)", text: "" + plate.ManufacturerProductFamily,
                lineHeight: 120,
                statement: ((plate.ManufacturerProductFamily?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given."))
                    .Set(description: "ManufacturerProductFamily:\n" +
                        "2nd level of the product hierarchy of a organisation. Typically denotes a specific " +
                        "type family of asset, such as: \"ABC\", which is within the given " +
                        "top level (category)."));

            AddIndexTextBlock(uitk, pdGrid, 2, 0, index: "(13)", text: "" + plate.ManufacturerProductType,
                lineHeight: 120,
                statement: ((plate.ManufacturerProductRoot?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given."))
                    .Set(description: "ManufacturerProductType:\n" +
                        "Specific product type of an asset. Typically specific enough to order " +
                        "a spare or replacement part."));

            AddIndexTextBlock(uitk, pdGrid, 3, 0, index: "(14)", text: "" + plate.OrderCodeOfManufacturer,
                lineHeight: 120,
                statement: ((plate.ManufacturerProductRoot?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given."))
                    .Set(description: "OrderCodeOfManufacturer:\n" +
                        "Unique combination of numbers and letters given by the manufacturer precisely " +
                        "defining the asset type. Full information given to order an exact replacement " +
                        "of the asset."));

            AddIndexTextBlock(uitk, pdGrid, 4, 0, index: "(15)",
                lineHeight: 120,
                text: "" + plate.ProductArticleNumberOfManufacturer,
                statement: ((plate.ManufacturerProductRoot?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given."))
                    .Set(description: "ProductArticleNumberOfManufacturer:\n" +
                        "Unique product identifier of the manufacturerTop defined by the ordering " +
                        "system of the manufacturer. Typically known as part number."));

            AddIndexTextBlock(uitk, pdGrid, 5, 0, index: "(16)", text: "" + plate.SerialNumber,
                lineHeight: 120,
                statement: ((plate.ManufacturerProductRoot?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn,
                        statement: "Should be given, if asset is an product instance."))
                    .Set(description: "SerialNumber:\n" +
                        "Unique combination of numbers and letters used to identify the " +
                        "product (asset) instance once it has been manufactured. Does not need to be " +
                        "worldwide unqiue, only for the manufacturer."));

            // Product designation

            AddIndexTextBlock(uitk, grid, 1, 1, rowSpan: 2,
                index: "(17)",
                text: "" + plate.ManufacturerProductDesignation,
                lineHeight: 120,
                statement: ((plate.ManufacturerProductDesignation?.HasContent() == true
                             && plate.ManufacturerProductDesignation.Length < 50)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given and concise."))
                    .Set(description: "ManufacturerProductDesignation:\n" +
                        "Short description of the product (short text). Ideally not longer than " +
                        "50 characters."));

            // Contact information

            AddIndexTextBlock(uitk, grid, 1, 2, colSpan: 2,
                index: "(3)",
                text: "" + string.Join(" \u2022 ", (plate.ContactInformation ?? (new[] { "-" }).ToList())),
                lineHeight: 120,
                statement: ((plate.ContactInformation != null && plate.ContactInformation.Count >= 2)
                ? new IndexStatement(Quality.Good, statement: "Is given.")
                : new IndexStatement(Quality.Warn, statement: "Should be given."))
                .Set(description: "ContactInformation:\n" +
                    "At least, the following information needs to be given: Street, Zipcode, CityTown, " +
                    "NationalCode."));

            // Explosion safety

            if (plate.ExplSafetyStr != null)
                AddIndexTextBlock(uitk, grid, 2, 2, colSpan: 2,
                    index: "(30)",
                    lineHeight: 120,
                    text: "" + plate.ExplSafetyStr);

            // Markings

            if (plate.Markings != null && plate.Markings.Count > 0)
            {
                // indexed grid
                var gridMarksOut = AddGridWithIndex(uitk, grid, 3, 0, rowSpan: 3, colSpan: 2,
                        index: "(20)");

                // raster
                var numMarks = plate.Markings.Count;
                var numCol = numMarks;
                var numRow = 1;
                if (numCol > MarkingsPerRow)
                {
                    numRow = 1 + ((numMarks - 1) / MarkingsPerRow);
                    numCol = MarkingsPerRow;
                }

                // wrap within raster
                var wrapPanel = new AnyUiWrapPanel();
                wrapPanel.VerticalAlignment = AnyUiVerticalAlignment.Bottom;
                AnyUiGrid.SetRow(wrapPanel, 0);
                AnyUiGrid.SetColumn(wrapPanel, 1);
                gridMarksOut.Add(wrapPanel);

                // fill raster
                for (int i = 0; i < numMarks; i++)
                {
                    // access
                    var mark = plate.Markings[i];
                    if (mark == null)
                        continue;

                    int row = i / MarkingsPerRow;
                    int col = i % MarkingsPerRow;

                    // make a small grid and add this manually
                    var oneMarkGrid = uitk.AddSmallGrid(2, 1, colWidths: new[] { "*" }, rowHeights: new[] { "*", "#" });
                    oneMarkGrid.MaxHeight = 100;
                    oneMarkGrid.MaxWidth = 100;
                    wrapPanel.Add(oneMarkGrid);

                    // very important for HTML rendering of WrapPanel!
                    oneMarkGrid.HorizontalAlignment = AnyUiHorizontalAlignment.Left;

                    // render EITHER image or text
                    AnyUiBitmapInfo markImg = null;
                    if (_package != null)
                        markImg = AnyUiGdiHelper.LoadBitmapInfoFromPackage(_package, mark.File?.Value);

                    if (markImg != null)
                    {
                        // render IMAGE

                        //var img = uitk.Set(
                        //    new AnyUiImage() { Stretch = AnyUiStretch.Uniform, BitmapInfo = markImg },
                        //    margin: new AnyUiThickness(0, 0, 4, 4),
                        //    horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                        //    verticalAlignment: AnyUiVerticalAlignment.Stretch);
                        //wrapPanel.Add(img);

                        //img.MaxHeight = 70;
                        //img.MaxWidth = 100;
                        var img = uitk.Set(
                            uitk.AddSmallImageTo(oneMarkGrid, 0, 0,
                                margin: new AnyUiThickness(0, 0, 4, 4),
                                stretch: AnyUiStretch.Uniform,
                                bitmap: markImg),
                            horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                            verticalAlignment: AnyUiVerticalAlignment.Stretch,
                            maxWidth: 100 /* for HTML it is required to state here! */);
                    }
                    else
                    {
                        // render TEXT

                        //var tb = new AnyUiTextBlock()
                        //{
                        //    Text = "" + mark.Name,
                        //    FontSize = 1.4,
                        //    Margin = new AnyUiThickness(0, 0, 4, 4),
                        //    Padding = new AnyUiThickness(4),
                        //    Background = AnyUiBrushes.White,
                        //    TextWrapping = AnyUiTextWrapping.Wrap,
                        //    VerticalAlignment = AnyUiVerticalAlignment.Stretch,
                        //    VerticalContentAlignment = AnyUiVerticalAlignment.Center
                        //};
                        //wrapPanel.Add(tb);

                        //tb.MaxHeight = 70;
                        //tb.MaxWidth = 100;

                        var tb = uitk.Set(
                            uitk.AddSmallBasicLabelTo(oneMarkGrid, 0, 0,
                                content: "" + mark.Name,
                                fontSize: 1.4,
                                margin: new AnyUiThickness(0, 0, 4, 4),
                                padding: new AnyUiThickness(4),
                                background: AnyUiBrushes.White,
                                textWrapping: AnyUiTextWrapping.Wrap,
                                verticalAlignment: AnyUiVerticalAlignment.Stretch,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center));                            
                    }

                    // render (little) additional text
                    if (mark.AddText != null && mark.AddText.Count > 0)
                    {
                        var markAddTxt = string.Join(" * ", mark.AddText);
                        var at = uitk.Set(
                                uitk.AddSmallBasicLabelTo(oneMarkGrid, 1, 0,
                                    content: "" + markAddTxt,
                                    fontSize: 0.8,
                                    margin: new AnyUiThickness(0, 0, 4, 4),
                                    background: AnyUiBrushes.Transparent,
                                    textWrapping: AnyUiTextWrapping.Wrap,
                                    horizontalAlignment: AnyUiHorizontalAlignment.Center,
                                    horizontalContentAlignment: AnyUiHorizontalAlignment.Center),
                                maxWidth: 100);
                    }
                }
            }

            // versions

            var vers = new List<string>();
            if (plate.HardwareVersion?.HasContent() == true)
                vers.Add("H/W: " + plate.HardwareVersion);
            if (plate.FirmwareVersion?.HasContent() == true)
                vers.Add("F/W: " + plate.FirmwareVersion);
            if (plate.SoftwareVersion?.HasContent() == true)
                vers.Add("S/W: " + plate.SoftwareVersion);

            var versStr = string.Join("\n", vers);

            AddIndexTextBlock(uitk, grid, 3, 2,
                index: "(18)",
                text: "" + versStr,
                lineHeight: 120,
                statement: ((versStr.HasContent())
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Error, statement: "Should be given."))
                    .Set(description: "(Hard|Firm|Soft)wareVersion:\n" +
                        "Hardware versions often lead to a new type or order code of the asset. " +
                        "Firmware is directly supplied by the device (asset), while software is used " +
                        "by the device (asset)."));

            // Year of construction

            AddIndexTextBlock(uitk, grid, 4, 2,
                index: "(4)",
                text: "" + plate.YearOfConstruction,
                lineHeight: 120,
                statement: ((plate.YearOfConstruction?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Error, statement: "Shall be given."))
                    .Set(description: "YearOfConstruction:\n" +
                        "Year (4 digits) as completion of manufacture of asset."));

            // Date of manufacture

            AddIndexTextBlock(uitk, grid, 5, 2,
                index: "(5)",
                text: "" + plate.DateOfManufacture,
                lineHeight: 120,
                statement: ((plate.DateOfManufacture?.HasContent() == true)
                    ? new IndexStatement(Quality.Good, statement: "Is given.")
                    : new IndexStatement(Quality.Warn, statement: "Should be given."))
                    .Set(description: "SerialNumber:\n" +
                        "Date from which the production and / or development process is completed or " +
                        "from which a service is provided completely."));

            // URI of product (QR)

            if (plate.URIOfTheProduct?.HasContent() == true)
            {
                var gridQr = AddGridWithIndex(uitk, grid, 3, 3, rowSpan: 3,
                    index: "(6)",
                    statement: ((plate.URIOfTheProduct?.HasContent() == true)
                        ? new IndexStatement(Quality.Good, statement: "Is given.")
                        : new IndexStatement(Quality.Error, statement: "Shall be given."))
                        .Set(description: "URIOfTheProduct:\n" +
                            "Unique global identification of the product (asset) using an " +
                            "universal resource identifier (URI) according IEC 61406."));

                AddQrCode(uitk, gridQr, 0, 1, plate.URIOfTheProduct, stretch: AnyUiStretch.Uniform);
            }

            // ok
            return plateGrid;
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
                PluginName = "AasxPluginDigitalNameplate",
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

            // the default: the full shelf
            RenderFullNameplate(_panel, _uitk);
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
