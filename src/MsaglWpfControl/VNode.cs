using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using MEdge = Microsoft.Msagl.Drawing.Edge;
using MEllipse = Microsoft.Msagl.Core.Geometry.Curves.Ellipse;
using MLineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using MNode = Microsoft.Msagl.Drawing.Node;
using MPoint = Microsoft.Msagl.Core.Geometry.Point;
using MPolyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;
using MShape = Microsoft.Msagl.Drawing.Shape;
using WArcSegment = System.Windows.Media.ArcSegment;
using WBorder = System.Windows.Controls.Border;
using WBrush = System.Windows.Media.Brush;
using WBrushes = System.Windows.Media.Brushes;
using WCanvas = System.Windows.Controls.Canvas;
using WCornerRadius = System.Windows.CornerRadius;
using WEllipseGeometry = System.Windows.Media.EllipseGeometry;
using WFrameworkElement = System.Windows.FrameworkElement;
using WGeometry = System.Windows.Media.Geometry;
using WLineSegment = System.Windows.Media.LineSegment;
using WMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WPanel = System.Windows.Controls.Panel;
using WPath = System.Windows.Shapes.Path;
using WPathFigure = System.Windows.Media.PathFigure;
using WPathGeometry = System.Windows.Media.PathGeometry;
using WRect = System.Windows.Rect;
using WRectangle = System.Windows.Shapes.Rectangle;
using WRotateTransform = System.Windows.Media.RotateTransform;
using WSize = System.Windows.Size;
using WSweepDirection = System.Windows.Media.SweepDirection;
using WTextBlock = System.Windows.Controls.TextBlock;
using WVisibility = System.Windows.Visibility;

namespace Microsoft.Msagl.WpfGraphControl
{
    public class VNode : IViewerNode, IInvalidatable
    {
        internal WPath BoundaryPath;
        internal WFrameworkElement FrameworkElementOfNodeForLabel;
        readonly Func<MEdge, VEdge> _funcFromDrawingEdgeToVEdge;
        Subgraph _subgraph;
        MNode _node;
        WBorder _collapseButtonBorder;
        WRectangle _topMarginRect;
        WPath _collapseSymbolPath;
        readonly WBrush _collapseSymbolPathInactive = WBrushes.Silver;

        internal int ZIndex
        {
            get
            {
                var geomNode = Node.GeometryNode;
                if (geomNode == null)
                    return 0;
                return geomNode.AllClusterAncestors.Count();
            }
        }

        public MNode Node
        {
            get { return _node; }
            private set
            {
                _node = value;
                _subgraph = _node as Subgraph;
            }
        }


        internal VNode(MNode node, WFrameworkElement frameworkElementOfNodeForLabelOfLabel,
            Func<MEdge, VEdge> funcFromDrawingEdgeToVEdge, Func<double> pathStrokeThicknessFunc)
        {
            PathStrokeThicknessFunc = pathStrokeThicknessFunc;
            Node = node;
            FrameworkElementOfNodeForLabel = frameworkElementOfNodeForLabelOfLabel;

            _funcFromDrawingEdgeToVEdge = funcFromDrawingEdgeToVEdge;

            CreateNodeBoundaryPath();
            if (FrameworkElementOfNodeForLabel != null)
            {
                FrameworkElementOfNodeForLabel.Tag = this; //get a backpointer to the VNode
                Common.PositionFrameworkElement(FrameworkElementOfNodeForLabel, node.GeometryNode.Center, 1);
                WPanel.SetZIndex(FrameworkElementOfNodeForLabel, WPanel.GetZIndex(BoundaryPath) + 1);
            }
            SetupSubgraphDrawing();
            Node.Attr.VisualsChanged += (a, b) => Invalidate();
            Node.IsVisibleChanged += obj =>
            {
                foreach (var frameworkElement in FrameworkElements)
                {
                    frameworkElement.Visibility = Node.IsVisible ? WVisibility.Visible : WVisibility.Hidden;
                }
            };
        }

        internal IEnumerable<WFrameworkElement> FrameworkElements
        {
            get
            {
                if (FrameworkElementOfNodeForLabel != null) yield return FrameworkElementOfNodeForLabel;
                if (BoundaryPath != null) yield return BoundaryPath;
                if (_collapseButtonBorder != null)
                {
                    yield return _collapseButtonBorder;
                    yield return _topMarginRect;
                    yield return _collapseSymbolPath;
                }
            }
        }

        void SetupSubgraphDrawing()
        {
            if (_subgraph == null) return;

            SetupTopMarginBorder();
            SetupCollapseSymbol();
        }

        void SetupTopMarginBorder()
        {
            var cluster = (Cluster)_subgraph.GeometryObject;
            _topMarginRect = new WRectangle
            {
                Fill = WBrushes.Transparent,
                Width = Node.Width,
                Height = cluster.RectangularBoundary.TopMargin
            };
            PositionTopMarginBorder(cluster);
            SetZIndexAndMouseInteractionsForTopMarginRect();
        }

        void PositionTopMarginBorder(Cluster cluster)
        {
            var box = cluster.BoundaryCurve.BoundingBox;

            Common.PositionFrameworkElement(_topMarginRect,
                box.LeftTop + new MPoint(_topMarginRect.Width / 2, -_topMarginRect.Height / 2), 1);
        }

        void SetZIndexAndMouseInteractionsForTopMarginRect()
        {
            _topMarginRect.MouseEnter +=
                (
                    (a, b) =>
                    {
                        _collapseButtonBorder.Background =
                            Common.BrushFromMsaglColor(_subgraph.CollapseButtonColorActive);
                        _collapseSymbolPath.Stroke = WBrushes.Black;
                    }
                    );

            _topMarginRect.MouseLeave +=
                (a, b) =>
                {
                    _collapseButtonBorder.Background = Common.BrushFromMsaglColor(_subgraph.CollapseButtonColorInactive);
                    _collapseSymbolPath.Stroke = WBrushes.Silver;
                };
            WPanel.SetZIndex(_topMarginRect, int.MaxValue);
        }

        void SetupCollapseSymbol()
        {
            var collapseBorderSize = GetCollapseBorderSymbolSize();
            Debug.Assert(collapseBorderSize > 0);
            _collapseButtonBorder = new WBorder
            {
                Background = Common.BrushFromMsaglColor(_subgraph.CollapseButtonColorInactive),
                Width = collapseBorderSize,
                Height = collapseBorderSize,
                CornerRadius = new WCornerRadius(collapseBorderSize / 2)
            };

            WPanel.SetZIndex(_collapseButtonBorder, WPanel.GetZIndex(BoundaryPath) + 1);


            var collapseButtonCenter = GetCollapseButtonCenter(collapseBorderSize);
            Common.PositionFrameworkElement(_collapseButtonBorder, collapseButtonCenter, 1);

            double w = collapseBorderSize * 0.4;
            _collapseSymbolPath = new WPath
            {
                Data = CreateCollapseSymbolPath(collapseButtonCenter + new MPoint(0, -w / 2), w),
                Stroke = _collapseSymbolPathInactive,
                StrokeThickness = 1
            };

            WPanel.SetZIndex(_collapseSymbolPath, WPanel.GetZIndex(_collapseButtonBorder) + 1);
            _topMarginRect.MouseLeftButtonDown += TopMarginRectMouseLeftButtonDown;
        }


        /// <summary>
        /// </summary>
        public event Action<IViewerNode> IsCollapsedChanged;

        void InvokeIsCollapsedChanged()
        {
            if (IsCollapsedChanged != null)
                IsCollapsedChanged(this);
        }



        void TopMarginRectMouseLeftButtonDown(object sender, WMouseButtonEventArgs e)
        {
            var pos = e.GetPosition(_collapseButtonBorder);
            if (pos.X <= _collapseButtonBorder.Width && pos.Y <= _collapseButtonBorder.Height && pos.X >= 0 &&
                pos.Y >= 0)
            {
                e.Handled = true;
                var cluster = (Cluster)_subgraph.GeometryNode;
                cluster.IsCollapsed = !cluster.IsCollapsed;
                InvokeIsCollapsedChanged();
            }
        }

        double GetCollapseBorderSymbolSize()
        {
            return ((Cluster)_subgraph.GeometryNode).RectangularBoundary.TopMargin -
                   PathStrokeThickness / 2 - 0.5;
        }

        MPoint GetCollapseButtonCenter(double collapseBorderSize)
        {
            var box = _subgraph.GeometryNode.BoundaryCurve.BoundingBox;
            //cannot trust subgraph.GeometryNode.BoundingBox for a cluster
            double offsetFromBoundaryPath = PathStrokeThickness / 2 + 0.5;
            var collapseButtonCenter = box.LeftTop + new MPoint(collapseBorderSize / 2 + offsetFromBoundaryPath,
                -collapseBorderSize / 2 - offsetFromBoundaryPath);
            return collapseButtonCenter;
        }

        WGeometry CreateCollapseSymbolPath(MPoint center, double width)
        {
            var pathGeometry = new WPathGeometry();
            var pathFigure = new WPathFigure { StartPoint = Common.WpfPoint(center + new MPoint(-width, width)) };

            pathFigure.Segments.Add(new WLineSegment(Common.WpfPoint(center), true));
            pathFigure.Segments.Add(
                new WLineSegment(Common.WpfPoint(center + new MPoint(width, width)), true));

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        internal void CreateNodeBoundaryPath()
        {
            if (FrameworkElementOfNodeForLabel != null)
            {
                var center = Node.GeometryNode.Center;
                var margin = 2 * Node.Attr.LabelMargin;
                var bc = NodeBoundaryCurves.GetNodeBoundaryCurve(Node,
                    FrameworkElementOfNodeForLabel
                        .Width + margin,
                    FrameworkElementOfNodeForLabel
                        .Height + margin);
                bc.Translate(center);
            }
            BoundaryPath = new WPath { Data = CreatePathFromNodeBoundary(), Tag = this };
            WPanel.SetZIndex(BoundaryPath, ZIndex);
            SetFillAndStroke();
            if (Node.Label != null)
            {
                BoundaryPath.ToolTip = Node.LabelText;
                if (FrameworkElementOfNodeForLabel != null)
                    FrameworkElementOfNodeForLabel.ToolTip = Node.LabelText;
            }
        }

        internal Func<double> PathStrokeThicknessFunc;

        double PathStrokeThickness
        {
            get { return PathStrokeThicknessFunc != null ? PathStrokeThicknessFunc() : Node.Attr.LineWidth; }
        }

        byte GetTransparency(byte t)
        {
            return t;
        }

        void SetFillAndStroke()
        {
            byte trasparency = GetTransparency(Node.Attr.Color.A);
            BoundaryPath.Stroke =
                Common.BrushFromMsaglColor(new Drawing.Color(trasparency, Node.Attr.Color.R, Node.Attr.Color.G,
                    Node.Attr.Color.B));
            SetBoundaryFill();
            BoundaryPath.StrokeThickness = PathStrokeThickness;

            var textBlock = FrameworkElementOfNodeForLabel as WTextBlock;
            if (textBlock != null)
            {
                var col = Node.Label.FontColor;
                textBlock.Foreground =
                    Common.BrushFromMsaglColor(new Drawing.Color(GetTransparency(col.A), col.R, col.G, col.B));
            }
        }

        void SetBoundaryFill()
        {
            BoundaryPath.Fill = Common.BrushFromMsaglColor(Node.Attr.FillColor);
        }

        WGeometry DoubleCircle()
        {
            var box = Node.BoundingBox;
            double w = box.Width;
            double h = box.Height;
            var pathGeometry = new WPathGeometry();
            var r = new WRect(box.Left, box.Bottom, w, h);
            pathGeometry.AddGeometry(new WEllipseGeometry(r));
            var inflation = Math.Min(5.0, Math.Min(w / 3, h / 3));
            r.Inflate(-inflation, -inflation);
            pathGeometry.AddGeometry(new WEllipseGeometry(r));
            return pathGeometry;
        }

        WGeometry CreatePathFromNodeBoundary()
        {
            WGeometry geometry;
            switch (Node.Attr.Shape)
            {
                case MShape.Box:
                case MShape.House:
                case MShape.InvHouse:
                case MShape.Diamond:
                case MShape.Octagon:
                case MShape.Hexagon:

                    geometry = CreateGeometryFromMsaglCurve(Node.GeometryNode.BoundaryCurve);
                    break;

                case MShape.DoubleCircle:
                    geometry = DoubleCircle();
                    break;


                default:
                    geometry = GetEllipseGeometry();
                    break;
            }

            return geometry;
        }

        WGeometry CreateGeometryFromMsaglCurve(ICurve iCurve)
        {
            var pathGeometry = new WPathGeometry();
            var pathFigure = new WPathFigure
            {
                IsClosed = true,
                IsFilled = true,
                StartPoint = Common.WpfPoint(iCurve.Start)
            };

            var curve = iCurve as Curve;
            if (curve != null)
            {
                AddCurve(pathFigure, curve);
            }
            else
            {
                var rect = iCurve as RoundedRect;
                if (rect != null)
                    AddCurve(pathFigure, rect.Curve);
                else
                {
                    var ellipse = iCurve as MEllipse;
                    if (ellipse != null)
                    {
                        return new WEllipseGeometry(Common.WpfPoint(ellipse.Center), ellipse.AxisA.Length,
                            ellipse.AxisB.Length);
                    }
                    var poly = iCurve as MPolyline;
                    if (poly != null)
                    {
                        var p = poly.StartPoint.Next;
                        do
                        {
                            pathFigure.Segments.Add(new WLineSegment(Common.WpfPoint(p.Point),
                                true));

                            p = p.NextOnPolyline;
                        } while (p != poly.StartPoint);
                    }
                }
            }


            pathGeometry.Figures.Add(pathFigure);

            return pathGeometry;
        }


        static void AddCurve(WPathFigure pathFigure, Curve curve)
        {
            foreach (ICurve seg in curve.Segments)
            {
                var ls = seg as MLineSegment;
                if (ls != null)
                    pathFigure.Segments.Add(new WLineSegment(Common.WpfPoint(ls.End), true));
                else
                {
                    var ellipse = seg as MEllipse;
                    if (ellipse != null)
                        pathFigure.Segments.Add(new WArcSegment(Common.WpfPoint(ellipse.End),
                            new WSize(ellipse.AxisA.Length, ellipse.AxisB.Length),
                            MPoint.Angle(new MPoint(1, 0), ellipse.AxisA),
                            ellipse.ParEnd - ellipse.ParEnd >= Math.PI,
                            !ellipse.OrientedCounterclockwise()
                                ? WSweepDirection.Counterclockwise
                                : WSweepDirection.Clockwise, true));
                }
            }
        }

        WGeometry GetEllipseGeometry()
        {
            return new WEllipseGeometry(Common.WpfPoint(Node.BoundingBox.Center), Node.BoundingBox.Width / 2,
                Node.BoundingBox.Height / 2);
        }

        #region Implementation of IViewerObject

        public DrawingObject DrawingObject
        {
            get { return Node; }
        }

        bool markedForDragging;

        /// <summary>
        /// Implements a property of an interface IEditViewer
        /// </summary>
        public bool MarkedForDragging
        {
            get
            {
                return markedForDragging;
            }
            set
            {
                markedForDragging = value;
                if (value)
                {
                    MarkedForDraggingEvent?.Invoke(this, null);
                }
                else
                {
                    UnmarkedForDraggingEvent?.Invoke(this, null);
                }
            }
        }

        public event EventHandler MarkedForDraggingEvent;
        public event EventHandler UnmarkedForDraggingEvent;

        #endregion

        public IEnumerable<IViewerEdge> InEdges
        {
            get { return Node.InEdges.Select(e => _funcFromDrawingEdgeToVEdge(e)); }
        }

        public IEnumerable<IViewerEdge> OutEdges
        {
            get { return Node.OutEdges.Select(e => _funcFromDrawingEdgeToVEdge(e)); }
        }

        public IEnumerable<IViewerEdge> SelfEdges
        {
            get { return Node.SelfEdges.Select(e => _funcFromDrawingEdgeToVEdge(e)); }
        }
        public void Invalidate()
        {
            if (!Node.IsVisible)
            {
                foreach (var fe in FrameworkElements)
                    fe.Visibility = WVisibility.Hidden;
                return;
            }

            BoundaryPath.Data = CreatePathFromNodeBoundary();

            Common.PositionFrameworkElement(FrameworkElementOfNodeForLabel, Node.BoundingBox.Center, 1);


            SetFillAndStroke();
            if (_subgraph == null) return;
            PositionTopMarginBorder((Cluster)_subgraph.GeometryNode);
            double collapseBorderSize = GetCollapseBorderSymbolSize();
            var collapseButtonCenter = GetCollapseButtonCenter(collapseBorderSize);
            Common.PositionFrameworkElement(_collapseButtonBorder, collapseButtonCenter, 1);
            double w = collapseBorderSize * 0.4;
            _collapseSymbolPath.Data = CreateCollapseSymbolPath(collapseButtonCenter + new MPoint(0, -w / 2), w);
            _collapseSymbolPath.RenderTransform = ((Cluster)_subgraph.GeometryNode).IsCollapsed
                ? new WRotateTransform(180, collapseButtonCenter.X,
                    collapseButtonCenter.Y)
                : null;

            _topMarginRect.Visibility =
                _collapseSymbolPath.Visibility =
                    _collapseButtonBorder.Visibility = WVisibility.Visible;

        }

        public override string ToString()
        {
            return Node.Id;
        }

        internal void DetouchFromCanvas(WCanvas graphCanvas)
        {
            if (BoundaryPath != null)
                graphCanvas.Children.Remove(BoundaryPath);
            if (FrameworkElementOfNodeForLabel != null)
                graphCanvas.Children.Remove(FrameworkElementOfNodeForLabel);
        }
    }
}
