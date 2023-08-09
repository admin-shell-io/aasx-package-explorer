/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AasxIntegrationBase;
using AasxIntegrationBaseGdi;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPluginKnownSubmodels
{
    public class KnownSubmodelAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private Aas.Submodel _submodel = null;
        private KnownSubmodelsOptions _options = null;
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

        // ReSharper disable EmptyConstructor
        public KnownSubmodelAnyUiControl()
        {
        }
        // ReSharper enable EmptyConstructor

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            Aas.Submodel theSubmodel,
            KnownSubmodelsOptions theOptions,
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
            RenderFullView(_panel, _uitk, _package, _submodel);
        }

        public static KnownSubmodelAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            KnownSubmodelsOptions options,
            PluginEventStack eventStack,
            object opanel)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as Aas.Submodel;
            var panel = opanel as AnyUiStackPanel;
            if (package == null || sm == null || panel == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // factory this object
            var techCntl = new KnownSubmodelAnyUiControl();
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
            Aas.Submodel sm)
        {
            // test trivial access
            if (_options == null || _submodel?.SemanticId == null)
                return;

            // make sure for the right Submodel
            var foundRecs = new List<KnownSubmodelsOptionsRecord>();
            foreach (var rec in _options.LookupAllIndexKey<KnownSubmodelsOptionsRecord>(
                _submodel?.SemanticId?.GetAsExactlyOneKey()))
                foundRecs.Add(rec);

            // render
            RenderPanelOutside(view, uitk, foundRecs, package, sm);
        }

        protected void RenderPanelOutside(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            IEnumerable<KnownSubmodelsOptionsRecord> foundRecs,
            AdminShellPackageEnv package,
            Aas.Submodel sm)
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
                content: $"Associated Submodel Templates");

            //
            // Scroll area
            //

            // small spacer
            outer.RowDefinitions[1] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 1, 0,
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // add the body, a scroll viewer
            outer.RowDefinitions[2] = new AnyUiRowDefinition(1.0, AnyUiGridUnitType.Star);
            var scroll = AnyUiUIElement.RegisterControl(
                uitk.AddSmallScrollViewerTo(outer, 2, 0,
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
                });

            // content of the scroll viewer
            // need a stack panel to add inside
            var inner = new AnyUiStackPanel()
            {
                Orientation = AnyUiOrientation.Vertical,
                Margin = new AnyUiThickness(2)
            };
            scroll.Content = inner;

            if (foundRecs != null)
                foreach (var rec in foundRecs)
                    RenderPanelInner(inner, uitk, rec, package, sm);
        }

        #endregion

        #region Inner
        //=============

        protected void RenderPanelInner(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            KnownSubmodelsOptionsRecord rec,
            AdminShellPackageEnv package,
            Aas.Submodel sm)
        {
            // access
            if (view == null || uitk == null || sm == null || rec == null)
                return;

            // border around the whole item
            var brd = view.Add(new AnyUiBorder()
            {
                BorderBrush = AnyUiBrushes.DarkBlue,
                BorderThickness = new AnyUiThickness(1.0),
                Margin = new AnyUiThickness(2.0),
                Padding = new AnyUiThickness(2.0),
            });

            // make an grid with two columns, first a bit wider 
            var outer = brd.SetChild(uitk.AddSmallGrid(rows: 4, cols: 2,
                            colWidths: new[] { "#", "*" },
                            rowHeights: new[] { "#", "#", "#", "#" },
                            background: AnyUiBrushes.White));

            // fill bitmap
            AnyUiBitmapInfo bitmapInfo = null;
            try
            {
                // figure our relative path
                var basePath = "" + System.IO.Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly()?.Location);

                // path of image to load
                var il = rec.ImageLink.Replace("\\", "/");
                var imagePath = Path.Combine(basePath, il);

                // load
                bitmapInfo = AnyUiGdiHelper.CreateAnyUiBitmapInfo(imagePath);
            }
            catch {; }

            if (bitmapInfo != null)
                uitk.Set(
                    uitk.AddSmallImageTo(outer, 0, 0,
                        margin: new AnyUiThickness(2),
                        stretch: AnyUiStretch.Uniform,
                        bitmap: bitmapInfo),
                    rowSpan: 4,
                    minWidth: 150,
                    maxHeight: 150, maxWidth: 150,
                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                    verticalAlignment: AnyUiVerticalAlignment.Stretch);

            // labels
            uitk.AddSmallBasicLabelTo(outer, 0, 1,
                margin: new AnyUiThickness(8, 2, 0, 0),
                fontSize: 1.2f,
                setBold: true,
                content: "" + rec.Header);

            uitk.AddSmallBasicLabelTo(outer, 1, 1,
                margin: new AnyUiThickness(8, 2, 0, 0),
                fontSize: 1.0f,
                textWrapping: AnyUiTextWrapping.Wrap,
                content: "" + rec.Content);

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallBasicLabelTo(outer, 2, 1,
                    margin: new AnyUiThickness(8, 2, 0, 0),
                    fontSize: 1.0f,
                    textWrapping: AnyUiTextWrapping.Wrap,
                    setHyperLink: true,
                    content: "" + rec.FurtherUrl),
                (o) =>
                {
                    if (o is AnyUiSelectableTextBlock stb && stb.Text.HasContent())
                        return new AnyUiLambdaActionDisplayContentFile()
                        {
                            fn = stb.Text,
                            preferInternalDisplay = true
                        };
                    return new AnyUiLambdaActionNone();
                });

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
            RenderFullView(_panel, _uitk, _package, _submodel);
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
