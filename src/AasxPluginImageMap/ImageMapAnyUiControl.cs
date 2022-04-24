using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.ConceptModel;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPluginImageMap
{
    public class ImageMapAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private AdminShell.Submodel _submodel = null;
        private ImageMapOptions _options = null;
        private PluginEventStack _eventStack = null;
        private AnyUiStackPanel _panel = null;
        private AnyUiContextBase _context = null;

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        public List<AnyUiPoint> _clickedCoordinates = new List<AnyUiPoint>();

        #endregion

        #region Members to be kept for state/ update
        //=============

        protected double _lastScrollPosition = 0.0;

        protected bool _showRegions = false;

        protected AnyUiImage _backgroundImage = null;

        protected double? _backgroundSize = null;

        protected AnyUiTextBlock _labelInfo = null;

        protected AnyUiCanvas _canvas = null;

        #endregion

        #region Constructors, as for WPF control
        //=============

        public ImageMapAnyUiControl()
        {
        }

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel theSubmodel,
            ImageMapOptions theOptions,
            PluginEventStack eventStack,
            AnyUiStackPanel panel,
            AnyUiContextBase context)
        {
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _options = theOptions;
            _eventStack = eventStack;
            _panel = panel;
            _context = context;

            // fill given panel
            RenderFullView(_panel, _uitk, _package, _submodel);
        }

        public static ImageMapAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            ImageMapOptions options,
            PluginEventStack eventStack,
            object opanel,
            object ocontext)
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
            var techCntl = new ImageMapAnyUiControl();
            techCntl.Start(log, package, sm, options, eventStack, panel, ocontext as AnyUiContextBase);

            // return shelf
            return techCntl;
        }

        #endregion

        #region Display Submodel
        //=============

        private void RenderFullView(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            AdminShellPackageEnv package,
            AdminShell.Submodel sm)
        {
            // test trivial access
            if (_options == null || _submodel?.semanticId == null)
                return;

            // make sure for the right Submodel
            var foundRecs = new List<ImageMapOptionsOptionsRecord>();
            foreach (var rec in _options.LookupAllIndexKey<ImageMapOptionsOptionsRecord>(
                _submodel?.semanticId?.GetAsExactlyOneKey()))
                foundRecs.Add(rec);

            // render
            RenderPanelOutside(view, uitk, foundRecs, package, sm);
        }

        protected void RenderPanelOutside (
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            IEnumerable<ImageMapOptionsOptionsRecord> foundRecs,
            AdminShellPackageEnv package,
            AdminShell.Submodel sm)
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

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallCheckBoxTo(bluebar, 0, 1,
                    verticalAlignment: AnyUiVerticalAlignment.Center,
                    verticalContentAlignment: AnyUiVerticalAlignment.Center,
                    margin: new AnyUiThickness(0, 0, 8, 0),
                    content: "Show regions",
                    isChecked: _showRegions),
                (o)=>
                {
                    _showRegions = (bool) o;
                    return new AnyUiLambdaActionNone();
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
                    uitk.AddSmallGrid(rows: 1, cols: 1, colWidths: new [] {"*"}),
                    colSpan: 5);
            _viewbox.Child = innerGrid;

            // add an image
            innerGrid.RowDefinitions[0].Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            _backgroundImage = (AnyUiImage) AnyUiUIElement.RegisterControl(
                uitk.Set(
                    uitk.AddSmallImageTo(innerGrid, 0, 0,
                        stretch: AnyUiStretch.Uniform), 
                    eventMask: AnyUiEventMask.LeftDouble),
                (o) =>
                {
                    if (o is AnyUiEventData ev)
                    {
                        _clickedCoordinates.Add(ev.RelOrigin);
                        DisplayClickedCoordinates();
                    }
                    return new AnyUiLambdaActionPluginUpdateAnyUi()
                    {
                        PluginName = null, // do NOT call plugin!
                        UpdateMode = AnyUiPluginUpdateMode.StatusToUi,
                        UseInnerGrid = true
                    };
                });

            // add an Canvas on the same place as the image
            _canvas = AnyUiUIElement.RegisterControl(
                uitk.AddSmallFrameworkElementTo(innerGrid, 0, 0,
                    new AnyUiCanvas()
                    {
                        EmitEvent = AnyUiEventMask.LeftDouble
                    }),
                (o) =>
                {
                    if (o is AnyUiEventData ev
                        && ev.ClickCount >= 2
                        && ev.Source is AnyUiFrameworkElement fe
                        && fe.Tag is AdminShell.Property prop
                        && prop.parent is AdminShell.Entity ent)
                    {
                        // need a targetReference
                        // first check, if a navigate to reference element can be found
                        var navTo = ent.statements?.FindFirstSemanticIdAs<AdminShell.ReferenceElement>(
                            AasxPredefinedConcepts.ImageMap.Static.CD_NavigateTo?.GetReference(),
                            AdminShell.Key.MatchMode.Relaxed);
                        var targetRf = navTo?.value;

                        // if not, have a look to the Entity itself
                        if ((targetRf == null || targetRf.Count < 1)
                            && ent.GetEntityType() == AdminShell.Entity.EntityTypeEnum.SelfManagedEntity
                            && ent.assetRef != null && ent.assetRef.Count > 0)
                            targetRf = ent.assetRef;

                        // if found, hand over to main program
                        if (targetRf != null && targetRf.Count > 0)
                            _eventStack?.PushEvent(new AasxPluginResultEventNavigateToReference()
                            {
                                targetReference = targetRf
                            });
                    }
                    return new AnyUiLambdaActionNone();
                });

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
                fontSize: 1.0,
                setBold: false,
                content: $"Ready ..");

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
                        UpdateMode = AnyUiPluginUpdateMode.StatusToUi,
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

            SetInfos();
            SetRegions(forceTransparent: false);
        }

#endregion

#region Business logic
        //=============

        protected void SetInfos()
        {
            // access
            if (_submodel == null || _package == null)
                return;

            // image
            // file?
            var fe = _submodel.submodelElements.FindFirstSemanticIdAs<AdminShell.File>(
                AasxPredefinedConcepts.ImageMap.Static.CD_ImageFile.GetReference(),
                AdminShellV20.Key.MatchMode.Relaxed);
            if (fe == null)
                return;

            // bitmap data
            var bitmapdata = _package.GetByteArrayFromUriOrLocalPackage(fe.value);
            if (bitmapdata == null)
                return;

            // set?
            var bi = (BitmapSource)new ImageSourceConverter().ConvertFrom(bitmapdata);
            if (_backgroundImage != null && bi != null)
            {
                _backgroundImage.Bitmap = bi;
                _backgroundSize = bi.Width + bi.Height;
            }
        }

        private string ClickedCoordinatesToString()
        {
            if (_clickedCoordinates == null)
                return "";
            return String.Join(", ",
                _clickedCoordinates.Select((cc) =>
               {
                   return FormattableString.Invariant($"({cc.X:F1}, {cc.Y:F1})");
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

        private void SetRegions(bool forceTransparent)
        {
            // access
            if (_submodel?.submodelElements == null || _canvas == null)
                return;

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
            foreach (var ent in _submodel.submodelElements.FindAllSemanticIdAs<AdminShell.Entity>(
                AasxPredefinedConcepts.ImageMap.Static.CD_EntityOfImageMap.GetReference(),
                AdminShellV20.Key.MatchMode.Relaxed))
            {
                // access
                if (ent?.statements == null)
                    continue;

                // find all regions known
                foreach (var prect in ent.statements.FindAllSemanticIdAs<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_RegionRect.GetReference(),
                    AdminShellV20.Key.MatchMode.Relaxed))
                {
                    // access
                    if (!(prect?.value.HasContent() == true))
                        continue;
                    var pts = ParseDoubles(prect.value);
                    if (pts == null || pts.Length != 4)
                        continue;
                    if (pts[2] < pts[0] || pts[3] < pts[1])
                        continue;
                    index++;

                    // colors
                    var cols = DetermineColors(index, forceTransparent, 0x30, 0xff);

                    // construct widget
                    res.Add(new AnyUiRectangle()
                    {
                        Tag = prect,
                        X = pts[0], Y = pts[1],
                        Width = pts[2] - pts[0],
                        Height = pts[3] - pts[1],
                        Fill = cols.Item1,
                        Stroke = cols.Item2,
                        StrokeThickness = 1.0f
                    });

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
                            Content = "" + prect.idShort
                        });
                }

                foreach (var pcirc in ent.statements.FindAllSemanticIdAs<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_RegionCircle.GetReference(),
                    AdminShellV20.Key.MatchMode.Relaxed))
                {
                    // access
                    if (!(pcirc?.value.HasContent() == true))
                        continue;
                    var pts = ParseDoubles(pcirc.value);
                    if (pts == null || pts.Length != 3)
                        continue;
                    if (pts[2] <= 0.0)
                        continue;
                    index++;

                    // colors
                    var cols = DetermineColors(index, forceTransparent, 0x30, 0xff);

                    // construct widget
                    res.Add(new AnyUiEllipse()
                    {
                        Tag = pcirc,
                        X = pts[0] - pts[2],
                        Y = pts[1] - pts[2],
                        Width = 2 * pts[2],
                        Height = 2 * pts[2],
                        Fill = cols.Item1,
                        Stroke = cols.Item2,
                        StrokeThickness = 1.0f
                    });

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
                            Content = "" + pcirc.idShort
                        });
                }

                foreach (var ppoly in ent.statements.FindAllSemanticIdAs<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_RegionPolygon.GetReference(),
                    AdminShellV20.Key.MatchMode.Relaxed))
                {
                    // access
                    if (!(ppoly?.value.HasContent() == true))
                        continue;
                    var pts = ParseDoubles(ppoly.value);
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
                    res.Add(new AnyUiPolygon()
                    {
                        X = 0, Y = 0,
                        Height = 2000, Width = 2000,
                        Tag = ppoly,
                        Points = pc,
                        Fill = cols.Item1,
                        Stroke = cols.Item2,
                        StrokeThickness = 4.0f
                    });
                    //AddPolygon(this.CanvasContent, ppoly,
                    //    pts,
                    //    cols.Item1, cols.Item2);

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
                            Content = "" + ppoly.idShort
                        });
                    }
                }
            }

            ;

            // try sort in order not to click on the (large, rectangular) labels, but on the 
            // 'real' shapes
            res  = res.OrderBy((o) => (o is AnyUiLabel) ? 0 : 1).ToList();

            // set
            _canvas.Children = res;
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


#endregion

#region Utilities
        //===============


#endregion
    }
}
