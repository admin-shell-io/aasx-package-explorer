/*
Microsoft Automatic Graph Layout,MSAGL

Copyright (c) Microsoft Corporation

All rights reserved.

MIT License

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphmapsWithMesh;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using MEdge = Microsoft.Msagl.Drawing.Edge;
using MEllipse = Microsoft.Msagl.Core.Geometry.Curves.Ellipse;
using MLineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using MPoint = Microsoft.Msagl.Core.Geometry.Point;
using MPolyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;
using MRectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Visibility = System.Windows.Visibility;
using WBrush = System.Windows.Media.Brush;
using WBrushes = System.Windows.Media.Brushes;
using WCanvas = System.Windows.Controls.Canvas;
using WColor = System.Windows.Media.Color;
using WDoubleCollection = System.Windows.Media.DoubleCollection;
using WFrameworkElement = System.Windows.FrameworkElement;
using WGeometry = System.Windows.Media.Geometry;
using WPath = System.Windows.Shapes.Path;
using WSize = System.Windows.Size;
using WSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WStreamGeometry = System.Windows.Media.StreamGeometry;
using WStreamGeometryContext = System.Windows.Media.StreamGeometryContext;
using WSweepDirection = System.Windows.Media.SweepDirection;

namespace Microsoft.Msagl.WpfGraphControl
{
    internal class VEdge : IViewerEdge, IInvalidatable
    {

        internal WFrameworkElement LabelFrameworkElement;

        public VEdge(MEdge edge, WFrameworkElement labelFrameworkElement)
        {
            Edge = edge;
            CurvePath = new WPath
            {
                Data = GetICurveWpfGeometry(edge.GeometryEdge.Curve),
                Tag = this
            };

            EdgeAttrClone = edge.Attr.Clone();

            if (edge.Attr.ArrowAtSource)
                SourceArrowHeadPath = new WPath
                {
                    Data = DefiningSourceArrowHead(edge.Attr.ArrowheadAtSource),
                    Tag = this
                };
            if (edge.Attr.ArrowAtTarget)
                TargetArrowHeadPath = new WPath
                {
                    Data = DefiningTargetArrowHead(Edge.GeometryEdge.EdgeGeometry, PathStrokeThickness,
                            edge.Attr.ArrowheadAtTarget),
                    Tag = this
                };

            SetPathStroke(
                ArrowHeadIsEmpty(edge.Attr.ArrowheadAtSource),
                ArrowHeadIsEmpty(edge.Attr.ArrowheadAtTarget));

            if (labelFrameworkElement != null)
            {
                LabelFrameworkElement = labelFrameworkElement;
                Common.PositionFrameworkElement(LabelFrameworkElement, edge.Label.Center, 1);
            }
            edge.Attr.VisualsChanged += (a, b) => Invalidate();

            edge.IsVisibleChanged += obj =>
            {
                foreach (var frameworkElement in FrameworkElements)
                {
                    frameworkElement.Visibility = edge.IsVisible ? Visibility.Visible : Visibility.Hidden;
                }
            };
        }

        internal IEnumerable<WFrameworkElement> FrameworkElements
        {
            get
            {
                if (SourceArrowHeadPath != null)
                    yield return this.SourceArrowHeadPath;
                if (TargetArrowHeadPath != null)
                    yield return TargetArrowHeadPath;

                if (CurvePath != null)
                    yield return CurvePath;

                if (
                    LabelFrameworkElement != null)
                    yield return
                        LabelFrameworkElement;
            }
        }


        internal EdgeAttr EdgeAttrClone { get; set; }

        internal static WGeometry DefiningTargetArrowHead(EdgeGeometry edgeGeometry, double thickness, ArrowStyle? arrStyle = null)
        {
            if (edgeGeometry.TargetArrowhead == null || edgeGeometry.Curve == null)
                return null;
            var streamGeometry = new WStreamGeometry();
            using (WStreamGeometryContext context = streamGeometry.Open())
            {
                AddArrow(context, edgeGeometry.Curve.End,
                         edgeGeometry.TargetArrowhead.TipPosition, thickness, arrStyle);
                return streamGeometry;
            }
        }

        bool ArrowHeadIsEmpty(ArrowStyle? arrStyle = null)
        {
            return arrStyle.HasValue
                && (arrStyle.Value == ArrowStyle.Tee
                    || arrStyle.Value == ArrowStyle.ODiamond
                    || arrStyle.Value == ArrowStyle.Generalization);
        }

        WGeometry DefiningSourceArrowHead(ArrowStyle? arrStyle = null)
        {
            var streamGeometry = new WStreamGeometry();
            using (WStreamGeometryContext context = streamGeometry.Open())
            {
                AddArrow(context,
                    Edge.GeometryEdge.Curve.Start,
                    Edge.GeometryEdge.EdgeGeometry.SourceArrowhead.TipPosition,
                    PathStrokeThickness,
                    arrStyle);
                return streamGeometry;
            }
        }


        double PathStrokeThickness
        {
            get
            {
                return PathStrokeThicknessFunc != null ? PathStrokeThicknessFunc() : this.Edge.Attr.LineWidth;
            }
        }

        internal WPath CurvePath { get; set; }
        internal WPath SourceArrowHeadPath { get; set; }
        internal WPath TargetArrowHeadPath { get; set; }

        internal static WGeometry GetICurveWpfGeometry(ICurve curve)
        {
            var streamGeometry = new WStreamGeometry();
            using (WStreamGeometryContext context = streamGeometry.Open())
            {
                FillStreamGeometryContext(context, curve);
                return streamGeometry;
            }
        }

        static void FillStreamGeometryContext(WStreamGeometryContext context, ICurve curve)
        {
            if (curve == null)
                return;
            FillContextForICurve(context, curve);
        }

        internal static void FillContextForICurve(WStreamGeometryContext context, ICurve iCurve)
        {

            context.BeginFigure(Common.WpfPoint(iCurve.Start), false, false);

            var c = iCurve as Curve;
            if (c != null)
                FillContexForCurve(context, c);
            else
            {
                var cubicBezierSeg = iCurve as CubicBezierSegment;
                if (cubicBezierSeg != null)
                    context.BezierTo(Common.WpfPoint(cubicBezierSeg.B(1)), Common.WpfPoint(cubicBezierSeg.B(2)),
                                     Common.WpfPoint(cubicBezierSeg.B(3)), true, false);
                else
                {
                    var ls = iCurve as MLineSegment;
                    if (ls != null)
                        context.LineTo(Common.WpfPoint(ls.End), true, false);
                    else
                    {
                        var rr = iCurve as RoundedRect;
                        if (rr != null)
                            FillContexForCurve(context, rr.Curve);
                        else
                        {
                            var poly = iCurve as MPolyline;
                            if (poly != null)
                                FillContexForPolyline(context, poly);
                            else
                            {
                                var ellipse = iCurve as MEllipse;
                                if (ellipse != null)
                                {
                                    double sweepAngle = EllipseSweepAngle(ellipse);
                                    bool largeArc = Math.Abs(sweepAngle) >= Math.PI;
                                    MRectangle box = ellipse.FullBox();
                                    context.ArcTo(Common.WpfPoint(ellipse.End),
                                                  new WSize(box.Width / 2, box.Height / 2),
                                                  sweepAngle,
                                                  largeArc,
                                                  sweepAngle < 0
                                                      ? WSweepDirection.Counterclockwise
                                                      : WSweepDirection.Clockwise,
                                                  true, true);
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }
                        }
                    }
                }
            }
        }

        static void FillContexForPolyline(WStreamGeometryContext context, MPolyline poly)
        {
            for (PolylinePoint pp = poly.StartPoint.Next; pp != null; pp = pp.Next)
                context.LineTo(Common.WpfPoint(pp.Point), true, false);
        }

        static void FillContexForCurve(WStreamGeometryContext context, Curve c)
        {
            foreach (ICurve seg in c.Segments)
            {
                var bezSeg = seg as CubicBezierSegment;
                if (bezSeg != null)
                {
                    context.BezierTo(Common.WpfPoint(bezSeg.B(1)),
                                     Common.WpfPoint(bezSeg.B(2)), Common.WpfPoint(bezSeg.B(3)), true, false);
                }
                else
                {
                    var ls = seg as MLineSegment;
                    if (ls != null)
                        context.LineTo(Common.WpfPoint(ls.End), true, false);
                    else
                    {
                        var ellipse = seg as MEllipse;
                        if (ellipse != null)
                        {
                            double sweepAngle = EllipseSweepAngle(ellipse);
                            bool largeArc = Math.Abs(sweepAngle) >= Math.PI;
                            MRectangle box = ellipse.FullBox();
                            context.ArcTo(Common.WpfPoint(ellipse.End),
                                          new WSize(box.Width / 2, box.Height / 2),
                                          sweepAngle,
                                          largeArc,
                                          sweepAngle < 0
                                              ? WSweepDirection.Counterclockwise
                                              : WSweepDirection.Clockwise,
                                          true, true);
                        }
                        else
                            throw new NotImplementedException();
                    }
                }
            }
        }

        public static double EllipseSweepAngle(MEllipse ellipse)
        {
            double sweepAngle = ellipse.ParEnd - ellipse.ParStart;
            return ellipse.OrientedCounterclockwise() ? sweepAngle : -sweepAngle;
        }


        static void AddArrow(WStreamGeometryContext context, MPoint start, MPoint end, double thickness, ArrowStyle? arrStyle = null)
        {
            if (arrStyle.HasValue && arrStyle.Value == ArrowStyle.Diamond
                || arrStyle.Value == ArrowStyle.ODiamond)
            {
                MPoint dir = end - start;
                double dl = dir.Length;
                //take into account the widths
                double delta = Math.Min(dl / 2, thickness + thickness / 2);
                dir *= (dl - delta) / dl;

                MPoint start2 = start + 0.4 * dir;
                MPoint end2 = start + 1.0 * dir;
                MPoint mid = start - 0.2 * dir;

                dir = dir.Rotate(Math.PI / 2);
                MPoint s = 1.2 * dir * HalfArrowAngleTan;

                context.BeginFigure(Common.WpfPoint(start2 + s), true, true);
                context.LineTo(Common.WpfPoint(end2), true, true);
                context.LineTo(Common.WpfPoint(start2 - s), true, true);
                context.LineTo(Common.WpfPoint(mid), true, true);
                context.LineTo(Common.WpfPoint(start2 + s), true, true);
            }
            else
            if (arrStyle.HasValue && arrStyle.Value == ArrowStyle.Tee)
            {
                MPoint dir = end - start;
                double dl = dir.Length;
                //take into account the widths
                double delta = Math.Min(dl / 2, thickness + thickness / 2);
                dir *= (dl - delta) / dl;

                MPoint start2 = start + 0.0 * dir;
                MPoint end2 = start + 1.0 * dir;
                MPoint mid = start - 0.2 * dir;

                dir = dir.Rotate(Math.PI / 2);
                MPoint s = 0.5 * dir * HalfTeeAngleTan;

                context.BeginFigure(Common.WpfPoint(start2 + s), true, true);
                context.LineTo(Common.WpfPoint(start2 - s), true, true);
            }
            else
            if (arrStyle.HasValue && arrStyle.Value == ArrowStyle.Generalization)
            {
                MPoint dir = end - start;
                double dl = dir.Length;
                //take into account the widths
                double delta = Math.Min(dl / 2, thickness + thickness / 2);
                dir *= (dl - delta) / dl;

                MPoint start2 = start + 0.0 * dir;
                MPoint end2 = start + 1.0 * dir;
                MPoint mid = start - 0.2 * dir;

                dir = dir.Rotate(Math.PI / 2);
                MPoint s = 1.2 * dir * HalfGeneralizationAngleTan;

                context.BeginFigure(Common.WpfPoint(start2 + s), true, true);
                context.LineTo(Common.WpfPoint(end2), true, true);
                context.LineTo(Common.WpfPoint(start2 - s), true, true);
            }
            else
            if (thickness > 1)
            {
                MPoint dir = end - start;
                MPoint h = dir;
                double dl = dir.Length;
                if (dl < 0.001)
                    return;
                dir /= dl;

                var s = new MPoint(-dir.Y, dir.X);
                double w = 0.5 * thickness;
                MPoint s0 = w * s;

                s *= h.Length * HalfArrowAngleTan;
                s += s0;

                double rad = w / HalfArrowAngleCos;

                context.BeginFigure(Common.WpfPoint(start + s), true, true);
                context.LineTo(Common.WpfPoint(start - s), true, false);
                context.LineTo(Common.WpfPoint(end - s0), true, false);
                context.ArcTo(Common.WpfPoint(end + s0), new WSize(rad, rad),
                              Math.PI - ArrowAngle, false, WSweepDirection.Clockwise, true, false);
            }
            else
            {
                MPoint dir = end - start;
                double dl = dir.Length;
                //take into account the widths
                double delta = Math.Min(dl / 2, thickness + thickness / 2);
                dir *= (dl - delta) / dl;
                end = start + dir;
                dir = dir.Rotate(Math.PI / 2);
                MPoint s = dir * HalfArrowAngleTan;

                context.BeginFigure(Common.WpfPoint(start + s), true, true);
                context.LineTo(Common.WpfPoint(end), true, true);
                context.LineTo(Common.WpfPoint(start - s), true, true);
            }
        }

        const double ArrowAngle = 30.0; //degrees
        const double GeneralizationAngle = 45.0; //degrees
        const double TeeAngle = 90.0; //degrees

        static readonly double HalfArrowAngleTan = Math.Tan(ArrowAngle * 0.5 * Math.PI / 180.0);
        static readonly double HalfArrowAngleCos = Math.Cos(ArrowAngle * 0.5 * Math.PI / 180.0);

        static readonly double HalfTeeAngleTan = Math.Tan(TeeAngle * 0.5 * Math.PI / 180.0);
        static readonly double HalfGeneralizationAngleTan = Math.Tan(GeneralizationAngle * 0.5 * Math.PI / 180.0);

        #region Implementation of IViewerObject

        public DrawingObject DrawingObject
        {
            get { return Edge; }
        }

        public bool MarkedForDragging { get; set; }

#pragma warning disable 67
        public event EventHandler MarkedForDraggingEvent;
        public event EventHandler UnmarkedForDraggingEvent;
#pragma warning restore 67

        #endregion

        #region Implementation of IViewerEdge

        public MEdge Edge { get; private set; }
        public IViewerNode Source { get; private set; }
        public IViewerNode Target { get; private set; }
        public double RadiusOfPolylineCorner { get; set; }

        public VLabel VLabel { get; set; }

        #endregion

        internal void Invalidate(WFrameworkElement fe, Rail rail, byte edgeTransparency)
        {
            var path = fe as WPath;
            if (path != null)
                SetPathStrokeToRailPath(rail, path, edgeTransparency);
        }
        public void Invalidate()
        {
            var vis = Edge.IsVisible ? Visibility.Visible : Visibility.Hidden;
            foreach (var fe in FrameworkElements) fe.Visibility = vis;
            if (vis == Visibility.Hidden)
                return;
            CurvePath.Data = GetICurveWpfGeometry(Edge.GeometryEdge.Curve);
            if (Edge.Attr.ArrowAtSource)
                SourceArrowHeadPath.Data = DefiningSourceArrowHead(
                    Edge.Attr.ArrowheadAtSource);
            if (Edge.Attr.ArrowAtTarget)
                TargetArrowHeadPath.Data = DefiningTargetArrowHead(
                    Edge.GeometryEdge.EdgeGeometry, PathStrokeThickness,
                    Edge.Attr.ArrowheadAtTarget);
            SetPathStroke(
                ArrowHeadIsEmpty(Edge.Attr.ArrowheadAtSource),
                ArrowHeadIsEmpty(Edge.Attr.ArrowheadAtTarget));
            if (VLabel != null)
                ((IInvalidatable)VLabel).Invalidate();
        }

        void SetPathStroke(bool emptySource, bool emptyTarget)
        {
            SetPathStrokeToPath(CurvePath);
            if (SourceArrowHeadPath != null)
            {
                SourceArrowHeadPath.Stroke = SourceArrowHeadPath.Fill = Common.BrushFromMsaglColor(Edge.Attr.Color);
                SourceArrowHeadPath.StrokeThickness = PathStrokeThickness;
                if (emptySource)
                    SourceArrowHeadPath.Fill = WBrushes.White;
            }
            if (TargetArrowHeadPath != null)
            {
                TargetArrowHeadPath.Stroke = TargetArrowHeadPath.Fill = Common.BrushFromMsaglColor(Edge.Attr.Color);
                TargetArrowHeadPath.StrokeThickness = PathStrokeThickness;
                if (emptyTarget)
                    TargetArrowHeadPath.Fill = WBrushes.White;
            }
        }

        void SetPathStrokeToRailPath(Rail rail, WPath path, byte transparency)
        {

            path.Stroke = SetStrokeColorForRail(transparency, rail);
            path.StrokeThickness = PathStrokeThickness;

            foreach (var style in Edge.Attr.Styles)
            {
                if (style == Drawing.Style.Dotted)
                {
                    path.StrokeDashArray = new WDoubleCollection { 1, 1 };
                }
                else if (style == Drawing.Style.Dashed)
                {
                    var f = DashSize();
                    path.StrokeDashArray = new WDoubleCollection { f, f };
                }
            }
        }

        WBrush SetStrokeColorForRail(byte transparency, Rail rail)
        {
            return rail.IsHighlighted == false
                       ? new WSolidColorBrush(new WColor
                       {
                           A = transparency,
                           R = Edge.Attr.Color.R,
                           G = Edge.Attr.Color.G,
                           B = Edge.Attr.Color.B
                       })
                       : WBrushes.Red;
        }

        void SetPathStrokeToPath(WPath path)
        {
            path.Stroke = Common.BrushFromMsaglColor(Edge.Attr.Color);
            path.StrokeThickness = PathStrokeThickness;

            foreach (var style in Edge.Attr.Styles)
            {
                if (style == Drawing.Style.Dotted)
                {
                    path.StrokeDashArray = new WDoubleCollection { 1, 1 };
                }
                else if (style == Drawing.Style.Dashed)
                {
                    var f = DashSize();
                    path.StrokeDashArray = new WDoubleCollection { f, f };
                }
            }
        }

        public override string ToString()
        {
            return Edge.ToString();
        }

        internal static double _dashSize = 0.05; //inches
        internal Func<double> PathStrokeThicknessFunc;

        public VEdge(MEdge edge, LgLayoutSettings lgSettings)
        {
            Edge = edge;
            EdgeAttrClone = edge.Attr.Clone();
        }

        internal double DashSize()
        {
            var w = PathStrokeThickness;
            var dashSizeInPoints = _dashSize * GraphViewer.DpiXStatic;
            return dashSizeInPoints / w;
        }

        internal void RemoveItselfFromCanvas(WCanvas graphCanvas)
        {
            if (CurvePath != null)
                graphCanvas.Children.Remove(CurvePath);

            if (SourceArrowHeadPath != null)
                graphCanvas.Children.Remove(SourceArrowHeadPath);

            if (TargetArrowHeadPath != null)
                graphCanvas.Children.Remove(TargetArrowHeadPath);

            if (VLabel != null)
                graphCanvas.Children.Remove(VLabel.FrameworkElement);

        }

        public WFrameworkElement CreateFrameworkElementForRail(Rail rail, byte edgeTransparency)
        {
            var iCurve = rail.Geometry as ICurve;
            WPath fe;
            if (iCurve != null)
            {
                fe = (WPath)CreateFrameworkElementForRailCurve(rail, iCurve, edgeTransparency);
            }
            else
            {
                var arrowhead = rail.Geometry as Arrowhead;
                if (arrowhead != null)
                {
                    fe = (WPath)CreateFrameworkElementForRailArrowhead(rail, arrowhead, rail.CurveAttachmentPoint, edgeTransparency);
                }
                else
                    throw new InvalidOperationException();
            }
            fe.Tag = rail;
            return fe;
        }

        WFrameworkElement CreateFrameworkElementForRailArrowhead(Rail rail, Arrowhead arrowhead, MPoint curveAttachmentPoint, byte edgeTransparency)
        {
            var streamGeometry = new WStreamGeometry();

            using (WStreamGeometryContext context = streamGeometry.Open())
            {
                AddArrow(context, curveAttachmentPoint, arrowhead.TipPosition,
                         PathStrokeThickness);

            }

            var path = new WPath
            {
                Data = streamGeometry,
                Tag = this
            };

            SetPathStrokeToRailPath(rail, path, edgeTransparency);
            return path;
        }

        WFrameworkElement CreateFrameworkElementForRailCurve(Rail rail, ICurve iCurve, byte transparency)
        {
            var path = new WPath
            {
                Data = GetICurveWpfGeometry(iCurve),
            };
            SetPathStrokeToRailPath(rail, path, transparency);

            return path;
        }
    }
}
