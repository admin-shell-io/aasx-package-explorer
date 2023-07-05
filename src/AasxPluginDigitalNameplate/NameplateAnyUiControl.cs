/*
Copyright (c) 2018-2022 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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

// ReSharper disable InconsistentlySynchronizedField
// ReSharper disable AccessToModifiedClosure

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
            _nameplateData = NameplateData.ParseSubmodelForV10(_package, _submodel, _options);

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

            // need panel to add inside
            // var inner = new AnyUiStackPanel() { Orientation = AnyUiOrientation.Vertical };

            var stackGrid = uitk.AddSmallGrid(4, 1, colWidths: new[] { "*" });
            scroll.Content = stackGrid;

            // add a small explanation
            var gridExpl = uitk.AddSmallGridTo(stackGrid, 0, 0, 1, 2, colWidths: new[] { "*", "#" }, rowHeights: new[] { "#"},
                margin: new AnyUiThickness(0, 0, 0, 10));

            uitk.AddSmallBasicLabelTo(gridExpl, 0, 0,
                background: AnyUiBrushes.LightBlue,
                content: "" + _foundRecord?.Explanation,
                padding: new AnyUiThickness(0, 4, 0, 0),
                textWrapping: AnyUiTextWrapping.Wrap,
                verticalAlignment: AnyUiVerticalAlignment.Stretch,
                verticalContentAlignment: AnyUiVerticalAlignment.Top);

            uitk.Set(
                AddNamedImage(uitk, gridExpl, 0, 1, "iec-logo.png", stretch: AnyUiStretch.UniformToFill),
                    margin: new AnyUiThickness(10, 0, 0, 0),
                    verticalAlignment: AnyUiVerticalAlignment.Stretch, 
                    maxHeight: 100);

            // add the nameplate itself
            var gridNp = RenderAnyUiNameplateData(uitk, _nameplateData);
            AnyUiGrid.SetRow(gridNp, 1);
            stackGrid.Add(gridNp);

            // render the innerts of the scroll viewer
            stackGrid.Background = AnyUiBrushes.LightGray;
        }

        protected AnyUiGrid AddIndexAndUiElement(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int row, int col,
            string index,
            AnyUiUIElement uiElem,
            int? rowSpan = null, int? colSpan = null)
        {
            // make a outer grid
            var g2 = uitk.AddSmallGridTo(grid, row, col,
                1, 2,
                rowHeights: new[] { "*" }, colWidths: new[] { "24:", "*" },
                margin: new AnyUiThickness(2));

            // very small label
            uitk.AddSmallBasicLabelTo(g2, 0, 0,
                content: index,
                fontSize: 0.9f,
                margin: new AnyUiThickness(0, 0, 0, 2),
                background: new AnyUiBrush(0x40ffffff),
                verticalAlignment: AnyUiVerticalAlignment.Bottom,
                verticalContentAlignment: AnyUiVerticalAlignment.Stretch,
                horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                horizontalContentAlignment: AnyUiHorizontalAlignment.Center);

            AnyUiGrid.SetRow(uiElem, row);
            AnyUiGrid.SetColumn(uiElem, col);
            g2.Add(uiElem);

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
            int? rowSpan = null, int? colSpan = null)
        {
            // make a outer grid
            var g2 = uitk.AddSmallGridTo(grid, row, col,
                1, 2,
                rowHeights: new[] { "*" }, colWidths: new[] { "24:", "*" },
                margin: new AnyUiThickness(2));

            // very small label
            uitk.AddSmallBasicLabelTo(g2, 0, 0,
                content: index,
                fontSize: 0.9f,
                margin: new AnyUiThickness(0, 0, 0, 2),
                background: new AnyUiBrush(0x40ffffff),
                verticalAlignment: AnyUiVerticalAlignment.Bottom,
                verticalContentAlignment: AnyUiVerticalAlignment.Stretch,
                horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                horizontalContentAlignment: AnyUiHorizontalAlignment.Center);

            var tb = uitk.AddSmallBasicLabelTo(g2, 0, 1,
                content: text, /* multiLine: true, */ fontSize: fontSize,
                margin: new AnyUiThickness(6, 2, 2, 2),
                background: new AnyUiBrush(0x40ffffff),
                textWrapping: AnyUiTextWrapping.Wrap,
                verticalAlignment: AnyUiVerticalAlignment.Stretch,
                verticalContentAlignment: AnyUiVerticalAlignment.Top);

            // tb.IsReadOnly = true;
            tb.MaxHeight = 150;

            if (rowSpan.HasValue)
                g2.GridRowSpan = rowSpan.Value;

            if (colSpan.HasValue)
                g2.GridColumnSpan = colSpan.Value;

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
                uitk.Set(
                    uitk.AddSmallImageTo(grid, row, col,
                        stretch: stretch,
                        bitmap: bitmapInfo),
                    rowSpan: 7,
                    colSpan: 8,
                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                    verticalAlignment: AnyUiVerticalAlignment.Stretch);

            return res;
        }

        public AnyUiFrameworkElement RenderAnyUiNameplateData(
            AnyUiSmallWidgetToolkit uitk, NameplateData plate)
        {
            // access
            if (plate == null)
                return new AnyUiStackPanel();

            // make a outer grid
            var grid = uitk.AddSmallGrid(7, 8,
                rowHeights: new[] { "#", "#", "#", "#", "#", "#", "#" },
                colWidths: new[] { "14:", "14:", "*", "*", "*", "*", "14:", "14:" }, 
                margin: new AnyUiThickness(2));

            uitk.Set(uitk.AddSmallLabelTo(grid, 0, 0, content: ""), maxHeight: 4, minHeight: 4);

            // Background image

            uitk.Set(AddNamedImage(uitk, grid, 0, 0, "metal-plate3.png", stretch: AnyUiStretch.Fill),
                rowSpan: 7,
                    colSpan: 8,
                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                    verticalAlignment: AnyUiVerticalAlignment.Stretch);

            // Borders

            uitk.Set(
                uitk.AddSmallBorderTo(grid, 0, 1, rowSpan: 7, colSpan: 6,
                    margin: new AnyUiThickness(3, 3, 3, 3),
                    background: AnyUiBrushes.Transparent,
                    borderBrush: AnyUiBrushes.Black,
                    borderThickness: new AnyUiThickness(1.0),
                    cornerRadius: 4.0),
                skipForTarget: AnyUiTargetPlatform.Browser);

            var screwPos = new[] { 0, 0, 0, 7, 6, 0, 6, 7 };
            for (int i = 0; i < 4; i++)
            {
                var brd = uitk.Set(
                    uitk.AddSmallBorderTo(grid, screwPos[2*i+0], screwPos[2*i+1],
                        margin: new AnyUiThickness(2),
                        background: AnyUiBrushes.Transparent,
                        borderBrush: AnyUiBrushes.Black,
                        borderThickness: new AnyUiThickness(1.0),
                        cornerRadius: 4.0),
                    skipForTarget: AnyUiTargetPlatform.Browser);
                brd.Height = 6;
                brd.MaxHeight = 12;
            }

            // Product BIG

            AddIndexTextBlock(uitk, grid, 1, 2, colSpan: 2,
                index: "(10)", fontSize: 2.0f,
                text: "" + ((plate.ManufacturerProductType?.HasContent() == true)
                    ? plate.ManufacturerProductType : plate.ManufacturerProductFamily));

            // Manu Name

            AddIndexTextBlock(uitk, grid, 1, 4,
                index: "(1)", fontSize: 2.0f,
                text: "" + plate.ManufacturerName);

            // Logo

            AddIndexTextBlock(uitk, grid, 1, 5,
                index: "(2)",
                text: "LOGO");

            // Product details

            var pdGrid = uitk.Set(
                uitk.AddSmallGridTo(grid, 2, 2, 10, 1),
                rowSpan: 2);

            AddIndexTextBlock(uitk, pdGrid, 0, 0, index: "(11)", text: "" + plate.ManufacturerProductRoot);
            AddIndexTextBlock(uitk, pdGrid, 1, 0, index: "(12)", text: "" + plate.ManufacturerProductFamily);
            AddIndexTextBlock(uitk, pdGrid, 2, 0, index: "(13)", text: "" + plate.ManufacturerProductType);
            AddIndexTextBlock(uitk, pdGrid, 3, 0, index: "(14)", text: "" + plate.OrderCodeOfManufacturer);
            AddIndexTextBlock(uitk, pdGrid, 4, 0, index: "(15)", 
                text: "" + plate.ProductArticleNumberOfManufacturer);
            AddIndexTextBlock(uitk, pdGrid, 5, 0, index: "(16)", text: "" + plate.SerialNumber);

            // Product designation

            AddIndexTextBlock(uitk, grid, 2, 3, rowSpan: 2,
                index: "(17)",
                text: "" + plate.ManufacturerProductDesignation);

            // Contact information

            if (plate.ContactInformation != null)
                AddIndexTextBlock(uitk, grid, 2, 4, colSpan: 2,
                    index: "(3)",
                    text: "" + string.Join(" * ", plate.ContactInformation));

            // Explosion safety

            if (plate.ContactInformation != null)
                AddIndexTextBlock(uitk, grid, 3, 4, colSpan: 2,
                    index: "(30)",
                    text: "" + plate.ExplSafetyStr);

            // Markings

            if (plate.ContactInformation != null)
                AddIndexTextBlock(uitk, grid, 4, 2, rowSpan: 2, colSpan: 2,
                    index: "(20)",
                    text: "" + "MARKINGS");

            // Year of construction

            AddIndexTextBlock(uitk, grid, 4, 4,
                index: "(4)",
                text: "" + plate.YearOfConstruction);

            // Date of manufacture

            AddIndexTextBlock(uitk, grid, 5, 4,
                index: "(5)",
                text: "" + plate.DateOfManufacture);

            // URI of product (QR)

            AddIndexTextBlock(uitk, grid, 4, 5, rowSpan: 2,
                index: "(6)",
                text: "" + plate.CompanyLogo);

            // ok
            return grid;
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
