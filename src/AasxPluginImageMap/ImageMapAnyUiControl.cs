/*
Copyright (c) 2018-2022 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxIntegrationBaseGdi;
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.ConceptModel;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using Newtonsoft.Json;

// ReSharper disable NegativeEqualityExpression
// ReSharper disable AccessToModifiedClosure

namespace AasxPluginImageMap
{
    public class ImageMapAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private Aas.Submodel _submodel = null;
        private ImageMapOptions _options = null;
        private PluginEventStack _eventStack = null;
        private AnyUiStackPanel _panel = null;
        private AnyUiContextBase _context = null;
        private AasxPluginBase _plugin = null;

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        public List<AnyUiPoint> _clickedCoordinates = new List<AnyUiPoint>();

        #endregion

        #region Members to be kept for state/ update
        //=============

        protected double _lastScrollPosition = 0.0;

        protected int _showRegions = 0;

        protected AnyUiImage _backgroundImage = null;

        protected double? _backgroundSize = null;

        protected AnyUiTextBlock _labelInfo = null;

        protected AnyUiCanvas _canvas = null;

        #endregion

        #region Constructors, as for WPF control
        //=============

        public ImageMapAnyUiControl()
        {
            // start a timer
            AnyUiTimerHelper.CreatePluginTimer(1000, DispatcherTimer_Tick);
        }

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            Aas.Submodel theSubmodel,
            ImageMapOptions theOptions,
            PluginEventStack eventStack,
            AnyUiStackPanel panel,
            AnyUiContextBase context,
            AasxPluginBase plugin)
        {
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _options = theOptions;
            _eventStack = eventStack;
            _panel = panel;
            _context = context;
            _plugin = plugin;

            // fill given panel
            RenderFullView(_panel, _uitk, _package, _submodel);
        }

        public static ImageMapAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            ImageMapOptions options,
            PluginEventStack eventStack,
            object opanel,
            object ocontext,
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

            // factory this object
            var techCntl = new ImageMapAnyUiControl();
            techCntl.Start(log, package, sm, options, eventStack, panel, ocontext as AnyUiContextBase, plugin);

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
            var foundRecs = new List<ImageMapOptionsOptionsRecord>();
            foreach (var rec in _options.LookupAllIndexKey<ImageMapOptionsOptionsRecord>(
                _submodel?.SemanticId?.GetAsExactlyOneKey()))
                foundRecs.Add(rec);

            // render
            RenderPanelOutside(view, uitk, foundRecs, package, sm);
        }

        protected void RenderPanelOutside(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            IEnumerable<ImageMapOptionsOptionsRecord> foundRecs,
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
                content: $"Image Map");

            AnyUiComboBox cbRegs = null;
            cbRegs = AnyUiUIElement.RegisterControl(
                uitk.AddSmallComboBoxTo(bluebar, 0, 1,
                    minWidth: 110,
                    verticalContentAlignment: AnyUiVerticalAlignment.Center,
                    margin: new AnyUiThickness(2, 4, 8, 4),
                    items: new[] { "Operational", "Show clicks", "Show regions" },
                    selectedIndex: _showRegions),
                (o) =>
                {
                    if (cbRegs?.SelectedIndex != null)
                        _showRegions = cbRegs.SelectedIndex.Value;

                    // trigger a complete redraw, as the regions might emit 
                    // events or not, depending on this flag
                    return new AnyUiLambdaActionPluginUpdateAnyUi()
                    {
                        PluginName = _plugin?.GetPluginName(),
                        UpdateMode = AnyUiRenderMode.All,
                        UseInnerGrid = true
                    };
                });

            //
            // Main area
            //

            // small spacer
            outer.RowDefinitions[1] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 1, 0,
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // viewbox
            outer.RowDefinitions[2].Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            var _viewbox = uitk.AddSmallFrameworkElementTo(outer, 2, 0,
                new AnyUiViewbox() { Stretch = AnyUiStretch.Uniform });

            // inside viewbox: a grid
            var innerGrid = uitk.Set(
                    uitk.AddSmallGrid(rows: 1, cols: 1, colWidths: new[] { "*" }),
                    colSpan: 5);
            _viewbox.Child = innerGrid;

            // add an image
            innerGrid.RowDefinitions[0].Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            _backgroundImage = AnyUiUIElement.RegisterControl(
                uitk.Set(
                    uitk.AddSmallImageTo(innerGrid, 0, 0,
                        stretch: AnyUiStretch.Uniform),
                    eventMask: AnyUiEventMask.LeftDouble),
                (o) =>
                {
                    if (o is AnyUiEventData ev && _showRegions == 1)
                    {
                        _clickedCoordinates.Add(ev.RelOrigin);
                        DisplayClickedCoordinates();
                    }

                    return new AnyUiLambdaActionPluginUpdateAnyUi()
                    {
                        PluginName = null, // do NOT call plugin!
                        UpdateMode = AnyUiRenderMode.StatusToUi,
                        UseInnerGrid = true
                    };
                });

            // add an Canvas on the same place as the image
            _canvas = AnyUiUIElement.RegisterControl(
                uitk.AddSmallFrameworkElementTo(innerGrid, 0, 0,
                    new AnyUiCanvas()
                    {
                        // These events could block the underlying image from sending events.
                        // Therefore only send events, if not the design mode ("show region")
                        // is being activated.
                        EmitEvent = (_showRegions == 0) ? AnyUiEventMask.LeftDouble : 0
                    }),
                setValue: RenderedElement_Clicked);

            //
            // Footer area
            //

            // small spacer
            outer.RowDefinitions[3] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 3, 0,
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // a grid
            var footer = uitk.AddSmallGridTo(outer, 4, 0, 1, cols: 5, colWidths: new[] { "*", "#", "#", "#", "#" });

            footer.Margin = new AnyUiThickness(0);
            footer.Background = AnyUiBrushes.LightGray;

            _labelInfo = uitk.AddSmallBasicLabelTo(footer, 0, 0, margin: new AnyUiThickness(8, 6, 0, 6),
                foreground: AnyUiBrushes.DarkGray,
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                fontSize: 1.0,
                setBold: false,
                content: $"Ready (waiting for double clicks) ..");

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(footer, 0, 1,
                    margin: new AnyUiThickness(2, 2, 2, 4),
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Clear"),
                (o) =>
                {
                    _clickedCoordinates.Clear();
                    DisplayClickedCoordinates();
                    return new AnyUiLambdaActionPluginUpdateAnyUi()
                    {
                        PluginName = null, // do NOT call plugin!
                        UpdateMode = AnyUiRenderMode.StatusToUi,
                        UseInnerGrid = true
                    };
                });

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(footer, 0, 2,
                    margin: new AnyUiThickness(2, 2, 2, 4),
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Copy to clipboard"),
                (o) =>
                {
                    if (_context != null)
                    {
                        var sum = ClickedCoordinatesToString();
                        sum = "[ " + sum + " ]";
                        _context.ClipboardSet(new AnyUiClipboardData(sum));
                        _log?.Info("Coordinates copied to clipboard.");
                    }
                    return new AnyUiLambdaActionNone();
                });

            //
            // Business logic
            //

            SetBasicInfos();
            if (_showRegions == 0 || _showRegions == 2)
                RenderRegions(forceTransparent: (_showRegions == 0));
        }

        /// <summary>
        /// This callback received the clicks from canvas / rendered elements withing
        /// </summary>
        protected AnyUiLambdaActionBase RenderedElement_Clicked(object o)
        {
            // rendered element clicked?
            if (o is AnyUiEventData ev
                        && ev.ClickCount >= 2
                        && ev.Source is AnyUiFrameworkElement fe
                        && fe.Tag is Aas.Property prop
                        && prop.Parent is Aas.Entity ent)
            {
                // need a targetReference
                // first check, if a navigate to reference element can be found
                var navTo = ent.Statements?.FindFirstSemanticIdAs<Aas.ReferenceElement>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_NavigateTo?.GetReference(),
                    MatchMode.Relaxed);
                var targetRf = navTo?.Value;

                // if not, have a look to the Entity itself
                if ((targetRf == null || targetRf.Count() < 1)
                    && ent.EntityType == Aas.EntityType.SelfManagedEntity
                    && ent.GlobalAssetId != null && ent.GlobalAssetId.Count() > 0)
                    targetRf = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new Aas.IKey[]
                    { new Aas.Key(Aas.KeyTypes.GlobalReference, ent.GlobalAssetId) }.ToList());

                // if found, hand over to main program
                if (targetRf != null && targetRf.Count() > 0)
                    _eventStack?.PushEvent(new AasxPluginResultEventNavigateToReference()
                    {
                        targetReference = targetRf
                    });
            }

            return new AnyUiLambdaActionNone();
        }

        #endregion

        #region Business logic
        //=============

        protected void SetBasicInfos()
        {
            // access
            if (_submodel == null || _package == null)
                return;

            // image
            // file?
            var fe = _submodel.SubmodelElements.FindFirstSemanticIdAs<Aas.File>(
                AasxPredefinedConcepts.ImageMap.Static.CD_ImageFile,
                MatchMode.Relaxed);
            if (fe?.Value == null)
                return;

            var bi = AnyUiGdiHelper.LoadBitmapInfoFromPackage(_package, fe.Value);
            if (_backgroundImage != null)
            {
                if (bi != null)
                {
                    bi.ConvertTo96dpi = true;
                    _backgroundImage.BitmapInfo = bi;
                    _backgroundSize = bi.PixelWidth + bi.PixelHeight;
                }
                else
                {
                    // no image available
                    _backgroundImage.Width = 100;
                    _backgroundImage.Height = 100;
                    _backgroundSize = 100;
                    _backgroundImage.Touch();
                }
            }
        }

        private string ClickedCoordinatesToString()
        {
            if (_clickedCoordinates == null)
                return "";
            return String.Join(", ",
                _clickedCoordinates.Select((cc) =>
               {
                   return FormattableString.Invariant($"{cc.X:F1}, {cc.Y:F1}");
               }));
        }

        private void DisplayClickedCoordinates()
        {
            var sum = ClickedCoordinatesToString();
            if (_labelInfo != null)
                _labelInfo.Text = $"Clicked coordinates = [ {sum} ]";
        }

        private double[] ParseDoubles(string input)
        {
            try
            {
                var x = JsonConvert.DeserializeObject<List<double>>(input);
                return x.ToArray();
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                return null;
            }
        }

        public string[] RegionColors = new[] { "#ff0000", "#00ff00", "#0000ff", "#ffff00", "#ff00ff", "#00ffff" };

        private Tuple<AnyUiBrush, AnyUiBrush, AnyUiBrush> DetermineColors(
            int index, bool forceTransparent, byte aFill, byte aStroke)
        {
            // ReSharper disable once PossibleNullReferenceException
            var bc = AnyUiColor.FromString("" + RegionColors[index % 6]);
            var fillColor = AnyUiColor.FromArgb(aFill, bc.R, bc.G, bc.B);
            var strokeColor = AnyUiColor.FromArgb(aStroke, bc.R, bc.G, bc.B);
            var textColor = AnyUiColors.White;
            if (forceTransparent)
            {
                fillColor = AnyUiColors.Transparent;
                strokeColor = AnyUiColors.Transparent;
                textColor = strokeColor;
            }
            return new Tuple<AnyUiBrush, AnyUiBrush, AnyUiBrush>(
                new AnyUiBrush(fillColor), new AnyUiBrush(strokeColor),
                new AnyUiBrush(textColor));
        }

        private void RenderRegions(bool forceTransparent)
        {
            // access
            if (_submodel?.SubmodelElements == null || _canvas == null)
                return;
            var defs = AasxPredefinedConcepts.ImageMap.Static;
            var mm = MatchMode.Relaxed;

            // result
            var res = new List<AnyUiUIElement>();

            // font scale
            double fontSize = 1.0;
            if (_backgroundSize.HasValue && _backgroundSize.Value > 100.0)
            {
                fontSize += _backgroundSize.Value / 2500.0;
            }

            // entities
            int index = -1;
            var activeFe = new List<AnyUiFrameworkElement>();
            foreach (var ent in _submodel.SubmodelElements.FindAllSemanticIdAs<Aas.Entity>(
                defs.CD_EntityOfImageMap, mm))
            {
                // access
                if (ent?.Statements == null)
                    continue;

                // find all regions known
                foreach (var prect in ent.Statements.FindAllSemanticIdAs<Aas.Property>(
                    defs.CD_RegionRect, mm))
                {
                    // access
                    if (!(prect?.Value.HasContent() == true))
                        continue;
                    var pts = ParseDoubles(prect.Value);
                    if (pts == null || pts.Length != 4)
                        continue;
                    if (pts[2] < pts[0] || pts[3] < pts[1])
                        continue;
                    index++;

                    // colors
                    var cols = DetermineColors(index, forceTransparent, 0x30, 0xff);

                    // construct widget
                    var el = new AnyUiRectangle()
                    {
                        Tag = prect,
                        X = pts[0],
                        Y = pts[1],
                        Width = pts[2] - pts[0],
                        Height = pts[3] - pts[1],
                        Fill = cols.Item1,
                        Stroke = cols.Item2,
                        StrokeThickness = 1.0f
                    };
                    activeFe.Add(el);
                    res.Add(el);

                    // construct text
                    if (!forceTransparent)
                        res.Add(new AnyUiLabel()
                        {
                            Tag = prect,
                            X = pts[0],
                            Y = pts[1],
                            Width = pts[2] - pts[0],
                            Height = pts[3] - pts[1],
                            HorizontalAlignment = AnyUiHorizontalAlignment.Center,
                            HorizontalContentAlignment = AnyUiHorizontalAlignment.Center,
                            VerticalAlignment = AnyUiVerticalAlignment.Center,
                            VerticalContentAlignment = AnyUiVerticalAlignment.Center,
                            Foreground = cols.Item3,
                            FontSize = fontSize,
                            Content = "" + prect.IdShort
                        });
                }

                foreach (var pcirc in ent.Statements.FindAllSemanticIdAs<Aas.Property>(
                    defs.CD_RegionCircle, mm))
                {
                    // access
                    if (!(pcirc?.Value.HasContent() == true))
                        continue;
                    var pts = ParseDoubles(pcirc.Value);
                    if (pts == null || pts.Length != 3)
                        continue;
                    if (pts[2] <= 0.0)
                        continue;
                    index++;

                    // colors
                    var cols = DetermineColors(index, forceTransparent, 0x30, 0xff);

                    // construct widget
                    var el = new AnyUiEllipse()
                    {
                        Tag = pcirc,
                        X = pts[0] - pts[2],
                        Y = pts[1] - pts[2],
                        Width = 2 * pts[2],
                        Height = 2 * pts[2],
                        Fill = cols.Item1,
                        Stroke = cols.Item2,
                        StrokeThickness = 1.0f
                    };
                    activeFe.Add(el);
                    res.Add(el);

                    // construct text
                    if (!forceTransparent)
                        res.Add(new AnyUiLabel()
                        {
                            Tag = pcirc,
                            X = pts[0] - pts[2],
                            Y = pts[1] - pts[2],
                            Width = 2 * pts[2],
                            Height = 2 * pts[2],
                            HorizontalAlignment = AnyUiHorizontalAlignment.Center,
                            HorizontalContentAlignment = AnyUiHorizontalAlignment.Center,
                            VerticalAlignment = AnyUiVerticalAlignment.Center,
                            VerticalContentAlignment = AnyUiVerticalAlignment.Center,
                            Foreground = cols.Item3,
                            FontSize = fontSize,
                            Content = "" + pcirc.IdShort
                        });
                }

                foreach (var ppoly in ent.Statements.FindAllSemanticIdAs<Aas.Property>(
                    defs.CD_RegionPolygon, mm))
                {
                    // access
                    if (!(ppoly?.Value.HasContent() == true))
                        continue;
                    var pts = ParseDoubles(ppoly.Value);
                    if (pts == null || pts.Length < 6 || pts.Length % 2 == 1)
                        continue;
                    index++;

                    // points
                    var pc = new AnyUiPointCollection();
                    for (int i = 0; (2 * i) < pts.Length; i++)
                        pc.Add(new AnyUiPoint(pts[2 * i], pts[2 * i + 1]));

                    // colors
                    var cols = DetermineColors(index, forceTransparent, 0x30, 0xff);

                    // construct widget
                    var el = new AnyUiPolygon()
                    {
                        X = 0,
                        Y = 0,
                        Height = 2000,
                        Width = 2000,
                        Tag = ppoly,
                        Points = pc,
                        Fill = cols.Item1,
                        Stroke = cols.Item2,
                        StrokeThickness = 1.0f
                    };
                    activeFe.Add(el);
                    res.Add(el);

                    // construct text
                    if (!forceTransparent)
                    {
                        var bb = pc.FindBoundingBox();
                        res.Add(new AnyUiLabel()
                        {
                            Tag = ppoly,
                            X = bb.X,
                            Y = bb.Y,
                            Width = bb.Width,
                            Height = bb.Height,
                            HorizontalAlignment = AnyUiHorizontalAlignment.Center,
                            HorizontalContentAlignment = AnyUiHorizontalAlignment.Center,
                            VerticalAlignment = AnyUiVerticalAlignment.Center,
                            VerticalContentAlignment = AnyUiVerticalAlignment.Center,
                            Foreground = cols.Item3,
                            FontSize = fontSize,
                            Content = "" + ppoly.IdShort
                        });
                    }
                }
            }

            // "activate" some of the framework elements
            foreach (var afe in activeFe)
            {
                // ok?
                if (afe.Tag == null)
                    return;

                // send events
                afe.EmitEvent = AnyUiEventMask.LeftDouble;

                // hereto
                afe.setValueLambda = RenderedElement_Clicked;
            }

            // try sort in order not to click on the (large, rectangular) labels, but on the 
            // 'real' shapes
            res = res.OrderBy((o) => (o is AnyUiLabel) ? 0 : 1).ToList();

            // set
            _canvas.Children = res;
        }

        protected class DataPointInfo
        {
            public string Value;
            public Aas.DataTypeDefXsd ValueType;
            public ImageMapArguments Args;

            public static DataPointInfo CreateFrom(Aas.ISubmodelElement sme)
            {
                if (sme is Aas.Property prop)
                    return new DataPointInfo()
                    {
                        Value = prop.Value,
                        ValueType = prop.ValueType
                    };

                if (sme is Aas.MultiLanguageProperty mlp)
                    return new DataPointInfo()
                    {
                        Value = mlp.Value?.GetDefaultString(),
                        ValueType = Aas.DataTypeDefXsd.String
                    };

                return null;
            }

            public double? EvalAsDouble(bool forceDouble = false)
            {
                // try to convert to double
                if ((forceDouble
                    || ValueType == Aas.DataTypeDefXsd.Float
                    || ValueType == Aas.DataTypeDefXsd.Double))
                {
                    if (double.TryParse("" + Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double dbl))
                    {
                        try
                        {
                            return dbl;
                        }
                        catch
                        {
                            ;
                        }
                    }
                }

                // else: pass thru
                return null;
            }

            public string EvalAsDisplayText()
            {
                // try to convert to double
                var dbl = EvalAsDouble();
                if (dbl.HasValue && Args?.fmt.HasContent() == true)
                {
                    try
                    {
                        return dbl.Value.ToString(Args.fmt, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        ;
                    }
                }

                // else: take as text
                var res = Value;

                // some manipulations?
                res = AdminShellUtil.ReplacePercentPlaceholder(res, "%utc%",
                    () => DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
                    StringComparison.InvariantCultureIgnoreCase);

                // ok
                return res;
            }

            public AnyUiBrush EvalAsBrush()
            {
                // access
                if (!Value.HasContent())
                    return AnyUiBrushes.Transparent;

                // work on and conditionally substitute value
                var work = "" + Value;

                if (Args?.colorset != null)
                {
                    if (Args.colorset == ImageMapArguments.ColorSetType.Bool
                        && Args.colors != null && Args.colors.Length >= 2)
                    {
                        // try eval boolean state
                        var state = true;
                        var tw = work.Trim().ToLower();
                        if (tw == "" || tw == "0" || tw == "false")
                            state = false;

                        // simply pick color
                        work = state ? Args.colors[1] : Args.colors[0];
                    }

                    if (Args.colorset == ImageMapArguments.ColorSetType.Switch
                        && Args.colors != null && Args.colors.Length >= 2)
                    {
                        // try eval double value
                        var dbl = EvalAsDouble();
                        if (dbl.HasValue)
                        {
                            // eval boolean state
                            var state = dbl.Value >= Args.level0;

                            // simply pick color
                            work = state ? Args.colors[1] : Args.colors[0];
                        }
                    }

                    if (Args.colorset == ImageMapArguments.ColorSetType.Blend
                        && Args.colors != null && Args.colors.Length >= 2
                        && (Args.level1 - Args.level0) > 0.0001)
                    {
                        // try eval double value
                        var dbl = EvalAsDouble();
                        if (dbl.HasValue)
                        {
                            // eval actual w.r.t levels
                            var level = (dbl.Value - Args.level0) / (Args.level1 - Args.level0);

                            // convert 2 colors
                            var c0 = AnyUiColor.FromString(Args.colors[0]);
                            var c1 = AnyUiColor.FromString(Args.colors[1]);

                            // blend color
                            var bc = AnyUiColor.Blend(c0, c1, level);
                            work = bc.ToHtmlString(1);
                        }
                    }

                    if (Args.colorset == ImageMapArguments.ColorSetType.Index
                        && Args.colors != null && Args.colors.Length >= 1)
                    {
                        // try eval double value
                        var dbl = EvalAsDouble();
                        if (dbl.HasValue)
                        {
                            // eval index restricted to array limits
                            var index = Math.Max(0, Math.Min(Args.colors.Length - 1,
                                    Convert.ToInt32(dbl.Value)));

                            // simply pick color
                            work = Args.colors[index];
                        }
                    }
                }

                // finally try convert
                var c = AnyUiColor.FromString(work);
                return new AnyUiBrush(c);
            }
        }

        private DataPointInfo EvalDataPoint(
            List<Aas.ISubmodelElement> smec,
            Aas.IReference semId)
        {
            // access
            if (smec == null || semId == null)
                return null;
            var mm = MatchMode.Relaxed;

            DataPointInfo res = null;

            // find a property?
            Aas.ISubmodelElement specSme = null;
            foreach (var prop in smec.FindAllSemanticIdAs<Aas.Property>(semId, mm))
            {
                res = DataPointInfo.CreateFrom(prop);
                if (res != null)
                {
                    specSme = prop;
                    break;
                }
            }

            foreach (var mlp in smec.FindAllSemanticIdAs<Aas.MultiLanguageProperty>(semId, mm))
            {
                res = DataPointInfo.CreateFrom(mlp);
                if (res != null)
                {
                    specSme = mlp;
                    break;
                }
            }

            foreach (var refe in smec.FindAllSemanticIdAs<Aas.ReferenceElement>(semId, mm))
            {
                // find reference target?
                var targetSme = _package?.AasEnv?.FindReferableByReference(refe?.Value) as Aas.ISubmodelElement;
                if (targetSme == null)
                    continue;

                // read
                res = DataPointInfo.CreateFrom(targetSme);
                if (res != null)
                {
                    specSme = refe;
                    break;
                }
            }

            // found?
            if (res == null || specSme == null)
                return null;

            // find args?
            var q = specSme.HasExtensionOfName("ImageMap.Args");
            if (q != null)
                res.Args = ImageMapArguments.Parse(q.Value);

            // OK
            return res;
        }

        /// <summary>
        /// Rendering of visual elements (e.g. text fields)
        /// </summary>
        /// <returns>If visual update on screen if required</returns>
        private bool RenderVisuElems()
        {
            // access
            if (_submodel?.SubmodelElements == null || _canvas == null)
                return false;
            var defs = AasxPredefinedConcepts.ImageMap.Static;
            var mm = MatchMode.Relaxed;

            // check, if required anyway
            var anyElem = _submodel.SubmodelElements
                .FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                    defs.CD_VisualElement, mm).FirstOrDefault();
            if (anyElem == null)
                return false;

            // result
            var res = new List<AnyUiUIElement>();

            foreach (var smcVE in _submodel.SubmodelElements
                .FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                    defs.CD_VisualElement, mm))
            {
                // access
                if (smcVE?.Value == null)
                    continue;

                // need a background shape (at least for the dimensions)
                AnyUiShape backShape = null;

                foreach (var prect in smcVE.Value.FindAllSemanticIdAs<Aas.Property>(
                    defs.CD_RegionRect, mm))
                {
                    // access
                    if (!(prect?.Value.HasContent() == true))
                        continue;
                    var pts = ParseDoubles(prect.Value);
                    if (pts == null || pts.Length != 4)
                        continue;
                    if (pts[2] < pts[0] || pts[3] < pts[1])
                        continue;

                    // construct widget
                    backShape = new AnyUiRectangle()
                    {
                        Tag = prect,
                        X = pts[0],
                        Y = pts[1],
                        Width = pts[2] - pts[0],
                        Height = pts[3] - pts[1],
                    };

                    // enough
                    break;
                }

                foreach (var pcirc in smcVE.Value.FindAllSemanticIdAs<Aas.Property>(
                    defs.CD_RegionCircle, mm))
                {
                    // access
                    if (!(pcirc?.Value.HasContent() == true))
                        continue;
                    var pts = ParseDoubles(pcirc.Value);
                    if (pts == null || pts.Length != 3)
                        continue;
                    if (pts[2] <= 0.0)
                        continue;

                    // construct widget
                    backShape = new AnyUiEllipse()
                    {
                        Tag = pcirc,
                        X = pts[0] - pts[2],
                        Y = pts[1] - pts[2],
                        Width = 2 * pts[2],
                        Height = 2 * pts[2],
                    };

                    // enough
                    break;
                }

                foreach (var ppoly in smcVE.Value.FindAllSemanticIdAs<Aas.Property>(
                    defs.CD_RegionPolygon, mm))
                {
                    // access
                    if (!(ppoly?.Value.HasContent() == true))
                        continue;
                    var pts = ParseDoubles(ppoly.Value);
                    if (pts == null || pts.Length < 6 || pts.Length % 2 == 1)
                        continue;

                    // points
                    var pc = new AnyUiPointCollection();
                    for (int i = 0; (2 * i) < pts.Length; i++)
                        pc.Add(new AnyUiPoint(pts[2 * i], pts[2 * i + 1]));

                    // construct widget
                    backShape = new AnyUiPolygon()
                    {
                        X = 0,
                        Y = 0,
                        Height = 2000,
                        Width = 2000,
                        Tag = ppoly,
                        Points = pc
                    };

                    // enough
                    break;
                }

                // need aback shape
                if (backShape == null)
                    continue;

                // find important information
                var dpiText = EvalDataPoint(smcVE.Value, defs.CD_TextDisplay.GetReference());
                var dpiFg = EvalDataPoint(smcVE.Value, defs.CD_Foreground.GetReference());
                var dpiBg = EvalDataPoint(smcVE.Value, defs.CD_Background.GetReference());

                // apply minimum requirements
                if (dpiText == null)
                    continue;
                var dispText = dpiText.EvalAsDisplayText();

                var dispFg = (dpiFg == null) ? AnyUiBrushes.White : dpiFg.EvalAsBrush();
                var dispBg = (dpiBg == null) ? AnyUiBrushes.Black : dpiBg.EvalAsBrush();

                // apply to backShape, add
                backShape.Fill = dispBg;
                res.Add(backShape);

                // construct label for foreground (with transparent fill)
                var bb = backShape.FindBoundingBox();

                var lab = new AnyUiLabel()
                {
                    Tag = smcVE,
                    X = bb.X,
                    Y = bb.Y,
                    Width = bb.Width,
                    Height = bb.Height,
                    HorizontalAlignment = AnyUiHorizontalAlignment.Center,
                    HorizontalContentAlignment = AnyUiHorizontalAlignment.Center,
                    VerticalAlignment = AnyUiVerticalAlignment.Center,
                    VerticalContentAlignment = AnyUiVerticalAlignment.Center,
                    Foreground = dispFg,
                    Background = null,
                    FontSize = 1.0,
                    Content = dispText
                };

                // some modifications to the label
                if (dpiText.Args != null)
                {
                    if (dpiText.Args.horalign.HasValue)
                    {
                        if (dpiText.Args.horalign == ImageMapArguments.HorizontalAlign.Left)
                        {
                            lab.HorizontalAlignment = AnyUiHorizontalAlignment.Left;
                            lab.HorizontalContentAlignment = AnyUiHorizontalAlignment.Left;
                        }
                        if (dpiText.Args.horalign == ImageMapArguments.HorizontalAlign.Right)
                        {
                            lab.HorizontalAlignment = AnyUiHorizontalAlignment.Right;
                            lab.HorizontalContentAlignment = AnyUiHorizontalAlignment.Right;
                        }
                    }

                    if (dpiText.Args.padding.HasValue)
                        lab.Padding = new AnyUiThickness(dpiText.Args.padding.Value);

                    if (dpiText.Args.fontsize.HasValue)
                        lab.FontSize *= dpiText.Args.fontsize.Value;
                }

                // add
                res.Add(lab);
            }

            // just add the result
            _canvas.Children.AddRange(res);
            return true;
        }

        #endregion

        #region Update
        //=============

        public void Update(params object[] args)
        {
            // check args
            if (args == null || args.Length < 2
                || !(args[0] is AnyUiStackPanel newPanel))
                return;

            // ok, re-assign panel and re-display
            _panel = newPanel;
            _panel.Children.Clear();

            _context = args[1] as AnyUiContextBase;

            // the default: the full shelf
            RenderFullView(_panel, _uitk, _package, _submodel);
        }

        #endregion

        #region Callbacks
        //===============

        private bool _dispatcherTimerActive = false;

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            // access
            if (_dispatcherTimerActive)
                return;
            _dispatcherTimerActive = true;

            // render
            if (_showRegions == 0 || _showRegions == 2)
                RenderRegions(forceTransparent: (_showRegions == 0));
            var updateRequired = RenderVisuElems();

            // let update
            if (updateRequired)
            {
                _eventStack?.PushEvent(new AasxPluginEventReturnUpdateAnyUi()
                {
                    PluginName = null,
                    Mode = AnyUiRenderMode.StatusToUi,
                    UseInnerGrid = true
                });
            }

            // release
            _dispatcherTimerActive = false;
        }

        #endregion

        #region Utilities
        //===============


        #endregion
    }
}
