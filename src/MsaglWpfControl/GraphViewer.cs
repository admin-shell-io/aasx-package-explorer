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
using System.ComponentModel;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Miscellaneous.LayoutEditing;
using MDrawingEdge = Microsoft.Msagl.Drawing.Edge;
using MEdge = Microsoft.Msagl.Core.Layout.Edge;
using MILabeledObject = Microsoft.Msagl.Drawing.ILabeledObject;
using MLabel = Microsoft.Msagl.Drawing.Label;
using MLineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using MModifierKeys = Microsoft.Msagl.Drawing.ModifierKeys;
using MNode = Microsoft.Msagl.Core.Layout.Node;
using MPoint = Microsoft.Msagl.Core.Geometry.Point;
using MRectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using WBitmapFrame = System.Windows.Media.Imaging.BitmapFrame;
using WBrushes = System.Windows.Media.Brushes;
using WCanvas = System.Windows.Controls.Canvas;
using WContextMenu = System.Windows.Controls.ContextMenu;
using WContextMenuService = System.Windows.Controls.ContextMenuService;
using WEllipse = System.Windows.Shapes.Ellipse;
using WFlowDirection = System.Windows.FlowDirection;
using WFontFamily = System.Windows.Media.FontFamily;
using WFontStretches = System.Windows.FontStretches;
using WFontStyle = System.Windows.FontStyle;
using WFontWeights = System.Windows.FontWeights;
using WFormattedText = System.Windows.Media.FormattedText;
using WFrameworkElement = System.Windows.FrameworkElement;
using WGeometryHitTestParameters = System.Windows.Media.GeometryHitTestParameters;
using WHitTestResult = System.Windows.Media.HitTestResult;
using WHitTestResultBehavior = System.Windows.Media.HitTestResultBehavior;
using WHitTestResultCallback = System.Windows.Media.HitTestResultCallback;
using WIInputElement = System.Windows.IInputElement;
using WImage = System.Windows.Controls.Image;
using WKeyboard = System.Windows.Input.Keyboard;
using WMatrixTransform = System.Windows.Media.MatrixTransform;
using WMenuItem = System.Windows.Controls.MenuItem;
using WMessageBox = System.Windows.MessageBox;
using WModifierKeys = System.Windows.Input.ModifierKeys;
using WMouse = System.Windows.Input.Mouse;
using WMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WMouseButtonState = System.Windows.Input.MouseButtonState;
using WMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WMouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using WPanel = System.Windows.Controls.Panel;
using WPath = System.Windows.Shapes.Path;
using WPixelFormats = System.Windows.Media.PixelFormats;
using WPngBitmapEncoder = System.Windows.Media.Imaging.PngBitmapEncoder;
using WPoint = System.Windows.Point;
using WRect = System.Windows.Rect;
using WRectangle = System.Windows.Shapes.Rectangle;
using WRectangleGeometry = System.Windows.Media.RectangleGeometry;
using WRenderTargetBitmap = System.Windows.Media.Imaging.RenderTargetBitmap;
using WRoutedEventArgs = System.Windows.RoutedEventArgs;
using WRoutedEventHandler = System.Windows.RoutedEventHandler;
using WSize = System.Windows.Size;
using WSizeChangedEventArgs = System.Windows.SizeChangedEventArgs;
using WTextBlock = System.Windows.Controls.TextBlock;
using WTransform = System.Windows.Media.Transform;
using WTypeface = System.Windows.Media.Typeface;
using WVisibility = System.Windows.Visibility;
using WVisualTreeHelper = System.Windows.Media.VisualTreeHelper;
namespace Microsoft.Msagl.WpfGraphControl
{
    public class GraphViewer : IViewer
    {
        WPath _targetArrowheadPathForRubberEdge;

        WPath _rubberEdgePath;
        WPath _rubberLinePath;
        MPoint _sourcePortLocationForEdgeRouting;
        CancelToken _cancelToken = new CancelToken();
        BackgroundWorker _backgroundWorker;
        MPoint _mouseDownPositionInGraph;
        bool _mouseDownPositionInGraph_initialized;

        WEllipse _sourcePortCircle;
        protected WEllipse TargetPortCircle { get; set; }

        WPoint _objectUnderMouseDetectionLocation;

        [JetBrains.Annotations.UsedImplicitly]
        public event EventHandler LayoutStarted;

        [JetBrains.Annotations.UsedImplicitly]
        public event EventHandler LayoutComplete;

        /// <summary>
        /// if set to true will layout in a task
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public bool RunLayoutAsync = false;

        readonly WCanvas _graphCanvas = new WCanvas();
        Graph _drawingGraph;

        readonly Dictionary<DrawingObject, WFrameworkElement> drawingObjectsToFrameworkElements =
            new Dictionary<DrawingObject, WFrameworkElement>();

        readonly LayoutEditor layoutEditor;


        GeometryGraph geometryGraphUnderLayout;
        bool needToCalculateLayout = true;


        object _objectUnderMouseCursor;

        static double _dpiX;
        static int _dpiY;

        readonly Dictionary<DrawingObject, IViewerObject> drawingObjectsToIViewerObjects =
            new Dictionary<DrawingObject, IViewerObject>();

        WFrameworkElement _rectToFillGraphBackground;
        WRectangle _rectToFillCanvas;


        GeometryGraph GeomGraph
        {
            get { return _drawingGraph?.GeometryGraph; }
        }

        /// <summary>
        /// the canvas to draw the graph
        /// </summary>
        public WCanvas GraphCanvas
        {
            get { return _graphCanvas; }
        }

        public GraphViewer()
        {
            layoutEditor = new LayoutEditor(this);

            _graphCanvas.SizeChanged += GraphCanvasSizeChanged;
            _graphCanvas.MouseLeftButtonDown += GraphCanvasMouseLeftButtonDown;
            _graphCanvas.MouseRightButtonDown += GraphCanvasRightMouseDown;
            _graphCanvas.MouseMove += GraphCanvasMouseMove;

            _graphCanvas.MouseLeftButtonUp += GraphCanvasMouseLeftButtonUp;
            _graphCanvas.MouseWheel += GraphCanvasMouseWheel;
            _graphCanvas.MouseRightButtonUp += GraphCanvasRightMouseUp;
            ViewChangeEvent += AdjustBtrectRenderTransform;

            LayoutEditingEnabled = true;
            clickCounter = new ClickCounter(() => WMouse.GetPosition((WIInputElement)_graphCanvas.Parent));
            clickCounter.Elapsed += ClickCounterElapsed;
        }


        #region WPF stuff

        /// <summary>
        /// adds the main panel of the viewer to the children of the parent
        /// </summary>
        /// <param name="panel"></param>
        public void BindToPanel(WPanel panel)
        {
            panel.Children.Add(GraphCanvas);
            GraphCanvas.UpdateLayout();
        }


        void ClickCounterElapsed(object sender, EventArgs e)
        {
            var vedge = clickCounter.ClickedObject as VEdge;
            if (vedge != null)
            {
                if (clickCounter.UpCount == clickCounter.DownCount && clickCounter.UpCount == 1)
                    HandleClickForEdge(vedge);
            }
            clickCounter.ClickedObject = null;
        }



        void AdjustBtrectRenderTransform(object sender, EventArgs e)
        {
            if (_rectToFillCanvas == null)
                return;
            _rectToFillCanvas.RenderTransform = (WTransform)_graphCanvas.RenderTransform.Inverse;
            var parent = (WPanel)GraphCanvas.Parent;
            _rectToFillCanvas.Width = parent.ActualWidth;
            _rectToFillCanvas.Height = parent.ActualHeight;

        }

        void GraphCanvasRightMouseUp(object sender, WMouseButtonEventArgs e)
        {
            OnMouseUp(e);
        }

        void HandleClickForEdge(VEdge vEdge)
        {
            //todo : add a hook
            var lgSettings = Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgSettings != null)
            {
                var lgEi = lgSettings.GeometryEdgesToLgEdgeInfos[vEdge.Edge.GeometryEdge];
                lgEi.SlidingZoomLevel = Math.Abs(lgEi.SlidingZoomLevel) > 0.0001 ? 0 : double.PositiveInfinity;

                ViewChangeEvent?.Invoke(null, null);
            }
        }

        void GraphCanvasRightMouseDown(object sender, WMouseButtonEventArgs e)
        {
            if (MouseDown != null)
                MouseDown(this, CreateMouseEventArgs(e));
        }

        void GraphCanvasMouseWheel(object sender, WMouseWheelEventArgs e)
        {
            if (e.Delta != 0)
            {
                const double zoomFractionLocal = 0.9;
                var zoomInc = e.Delta < 0 ? zoomFractionLocal : 1.0 / zoomFractionLocal;
                ZoomAbout(ZoomFactor * zoomInc, e.GetPosition(_graphCanvas));
                e.Handled = true;
            }
        }

        /// <summary>
        /// keeps centerOfZoom pinned to the screen and changes the scale by zoomFactor
        /// </summary>
        /// <param name="zoomFactor"></param>
        /// <param name="centerOfZoom"></param>
        public void ZoomAbout(double zoomFactor, WPoint centerOfZoom)
        {
            var scale = zoomFactor * FitFactor;
            var centerOfZoomOnScreen =
                _graphCanvas.TransformToAncestor((WFrameworkElement)_graphCanvas.Parent).Transform(centerOfZoom);
            SetTransform(scale, centerOfZoomOnScreen.X - centerOfZoom.X * scale,
                         centerOfZoomOnScreen.Y + centerOfZoom.Y * scale);
        }

        public LayoutEditor LayoutEditor
        {
            get { return layoutEditor; }
        }

        void GraphCanvasMouseLeftButtonDown(object sender, WMouseEventArgs e)
        {
            clickCounter.AddMouseDown(_objectUnderMouseCursor);
            if (MouseDown != null)
                MouseDown(this, CreateMouseEventArgs(e));

            if (e.Handled) return;
            _mouseDownPositionInGraph = Common.MsaglPoint(e.GetPosition(_graphCanvas));
            _mouseDownPositionInGraph_initialized = true;
        }


        void GraphCanvasMouseMove(object sender, WMouseEventArgs e)
        {
            if (MouseMove != null)
                MouseMove(this, CreateMouseEventArgs(e));

            if (e.Handled) return;


            if (WMouse.LeftButton == WMouseButtonState.Pressed && (!LayoutEditingEnabled || _objectUnderMouseCursor == null))
            {
                if (!_mouseDownPositionInGraph_initialized)
                {
                    _mouseDownPositionInGraph = Common.MsaglPoint(e.GetPosition(_graphCanvas));
                    _mouseDownPositionInGraph_initialized = true;
                }

                Pan(e);
            }
            else
            {
                // Retrieve the coordinate of the mouse position.
                WPoint mouseLocation = e.GetPosition(_graphCanvas);
                // Clear the contents of the list used for hit test results.
                ObjectUnderMouseCursor = null;
                UpdateWithWpfHitObjectUnderMouseOnLocation(mouseLocation, MyHitTestResultCallback);
            }
        }

        void UpdateWithWpfHitObjectUnderMouseOnLocation(WPoint pt, WHitTestResultCallback hitTestResultCallback)
        {
            _objectUnderMouseDetectionLocation = pt;
            // Expand the hit test area by creating a geometry centered on the hit test point.
            var rect = new WRect(new WPoint(pt.X - MouseHitTolerance, pt.Y - MouseHitTolerance),
                new WPoint(pt.X + MouseHitTolerance, pt.Y + MouseHitTolerance));
            var expandedHitTestArea = new WRectangleGeometry(rect);
            // Set up a callback to receive the hit test result enumeration.
            WVisualTreeHelper.HitTest(_graphCanvas, null,
                hitTestResultCallback,
                new WGeometryHitTestParameters(expandedHitTestArea));
        }


        // Return the result of the hit test to the callback.
        WHitTestResultBehavior MyHitTestResultCallback(WHitTestResult result)
        {
            var frameworkElement = result.VisualHit as WFrameworkElement;

            if (frameworkElement == null)
                return WHitTestResultBehavior.Continue;
            if (frameworkElement.Tag == null)
                return WHitTestResultBehavior.Continue;
            var tag = frameworkElement.Tag;
            var iviewerObj = tag as IViewerObject;
            if (iviewerObj != null && iviewerObj.DrawingObject.IsVisible)
            {
                if (ObjectUnderMouseCursor is IViewerEdge || ObjectUnderMouseCursor == null
                    ||
                    WPanel.GetZIndex(frameworkElement) >
                    WPanel.GetZIndex(GetFrameworkElementFromIViewerObject(ObjectUnderMouseCursor)))
                    //always overwrite an edge or take the one with greater zIndex
                    ObjectUnderMouseCursor = iviewerObj;
            }
            return WHitTestResultBehavior.Continue;
        }


        WFrameworkElement GetFrameworkElementFromIViewerObject(IViewerObject viewerObject)
        {
            WFrameworkElement ret;

            var vNode = viewerObject as VNode;
            if (vNode != null) ret = vNode.FrameworkElementOfNodeForLabel ?? vNode.BoundaryPath;
            else
            {
                var vLabel = viewerObject as VLabel;
                if (vLabel != null) ret = vLabel.FrameworkElement;
                else
                {
                    var vEdge = viewerObject as VEdge;
                    if (vEdge != null) ret = vEdge.CurvePath;
                    else
                    {
                        throw new InvalidOperationException(
#if DEBUG
                            "Unexpected object type in GraphViewer"
#endif
                            );
                    }
                }
            }
            if (ret == null)
                throw new InvalidOperationException(
#if DEBUG
                    "did not find a framework element!"
#endif
                    );

            return ret;
        }

        // Return the result of the hit test to the callback.
        WHitTestResultBehavior MyHitTestResultCallbackWithNoCallbacksToTheUser(WHitTestResult result)
        {
            var frameworkElement = result.VisualHit as WFrameworkElement;

            if (frameworkElement == null)
                return WHitTestResultBehavior.Continue;
            object tag = frameworkElement.Tag;
            if (tag != null)
            {
                //it is a tagged element
                var ivo = tag as IViewerObject;
                if (ivo != null)
                {
                    if (ivo.DrawingObject.IsVisible)
                    {
                        _objectUnderMouseCursor = ivo;
                        if (tag is VNode || tag is MLabel)
                            return WHitTestResultBehavior.Stop;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(tag is Rail);
                    _objectUnderMouseCursor = tag;
                    return WHitTestResultBehavior.Stop;
                }
            }

            return WHitTestResultBehavior.Continue;
        }


        protected double MouseHitTolerance
        {
            get { return (0.05) * DpiX / CurrentScale; }

        }
        /// <summary>
        /// this function pins the sourcePoint to screenPoint
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="sourcePoint"></param>
        void SetTransformFromTwoPoints(WPoint screenPoint, MPoint sourcePoint)
        {
            var scale = CurrentScale;
            SetTransform(scale, screenPoint.X - scale * sourcePoint.X, screenPoint.Y + scale * sourcePoint.Y);
        }
        /// <summary>
        /// Moves the point to the center of the viewport
        /// </summary>
        /// <param name="sourcePoint"></param>
        public void PointToCenter(MPoint sourcePoint)
        {
            WPoint center = new WPoint(_graphCanvas.RenderSize.Width / 2, _graphCanvas.RenderSize.Height / 2);
            SetTransformFromTwoPoints(center, sourcePoint);
        }
        public void NodeToCenterWithScale(Drawing.Node node, double scale)
        {
            if (node.GeometryNode == null) return;
            var screenPoint = new WPoint(_graphCanvas.RenderSize.Width / 2, _graphCanvas.RenderSize.Height / 2);
            var sourcePoint = node.BoundingBox.Center;
            SetTransform(scale, screenPoint.X - scale * sourcePoint.X, screenPoint.Y + scale * sourcePoint.Y);
        }

        public void NodeToCenter(Drawing.Node node)
        {
            if (node.GeometryNode == null) return;
            PointToCenter(node.GeometryNode.Center);
        }

        void Pan(WMouseEventArgs e)
        {
            if (UnderLayout)
                return;

            if (!_graphCanvas.IsMouseCaptured)
                _graphCanvas.CaptureMouse();


            SetTransformFromTwoPoints(e.GetPosition((WFrameworkElement)_graphCanvas.Parent),
                    _mouseDownPositionInGraph);

            if (ViewChangeEvent != null)
                ViewChangeEvent(null, null);
        }

        public double CurrentScale
        {
            get { return ((WMatrixTransform)_graphCanvas.RenderTransform).Matrix.M11; }
        }

        internal MsaglMouseEventArgs CreateMouseEventArgs(WMouseEventArgs e)
        {
            return new GvMouseEventArgs(e, this);
        }

        void GraphCanvasMouseLeftButtonUp(object sender, WMouseEventArgs e)
        {
            OnMouseUp(e);
            clickCounter.AddMouseUp();
            if (_graphCanvas.IsMouseCaptured)
            {
                e.Handled = true;
                _graphCanvas.ReleaseMouseCapture();
            }
        }

        void OnMouseUp(WMouseEventArgs e)
        {
            if (MouseUp != null)
                MouseUp(this, CreateMouseEventArgs(e));
        }

        void GraphCanvasSizeChanged(object sender, WSizeChangedEventArgs e)
        {
            if (_drawingGraph == null) return;
            // keep the same zoom level
            double oldfit = GetFitFactor(e.PreviousSize);
            double fitNow = FitFactor;
            double scaleFraction = fitNow / oldfit;
            SetTransform(CurrentScale * scaleFraction, CurrentXOffset * scaleFraction, CurrentYOffset * scaleFraction);
        }

        protected double CurrentXOffset
        {
            get { return ((WMatrixTransform)_graphCanvas.RenderTransform).Matrix.OffsetX; }
        }

        protected double CurrentYOffset
        {
            get { return ((WMatrixTransform)_graphCanvas.RenderTransform).Matrix.OffsetY; }
        }

        /// <summary>
        ///
        /// </summary>
        public double ZoomFactor
        {
            get { return CurrentScale / FitFactor; }
        }

        #endregion

        #region IViewer stuff

        public event EventHandler<EventArgs> ViewChangeEvent;
        public event EventHandler<MsaglMouseEventArgs> MouseDown;
        public event EventHandler<MsaglMouseEventArgs> MouseMove;
        public event EventHandler<MsaglMouseEventArgs> MouseUp;
        public event EventHandler<ObjectUnderMouseCursorChangedEventArgs> ObjectUnderMouseCursorChanged;

        public IViewerObject ObjectUnderMouseCursor
        {
            get
            {
                // this function can bring a stale object
                var location = WMouse.GetPosition(_graphCanvas);
                if (!(_objectUnderMouseDetectionLocation == location))
                    UpdateWithWpfHitObjectUnderMouseOnLocation(location, MyHitTestResultCallbackWithNoCallbacksToTheUser);
                return GetIViewerObjectFromObjectUnderCursor(_objectUnderMouseCursor);
            }
            private set
            {
                var old = _objectUnderMouseCursor;
                bool callSelectionChanged = _objectUnderMouseCursor != value && ObjectUnderMouseCursorChanged != null;

                _objectUnderMouseCursor = value;

                if (callSelectionChanged)
                    ObjectUnderMouseCursorChanged(this,
                                                  new ObjectUnderMouseCursorChangedEventArgs(
                                                      GetIViewerObjectFromObjectUnderCursor(old),
                                                      GetIViewerObjectFromObjectUnderCursor(_objectUnderMouseCursor)));
            }
        }

        IViewerObject GetIViewerObjectFromObjectUnderCursor(object obj)
        {
            if (obj == null)
                return null;
            return obj as IViewerObject;
        }

        public void Invalidate(IViewerObject objectToInvalidate)
        {
            ((IInvalidatable)objectToInvalidate).Invalidate();
        }

        public void Invalidate()
        {
            //todo: is it right to do nothing
        }

        public event EventHandler GraphChanged;

        public MModifierKeys ModifierKeys
        {
            get
            {
                switch (WKeyboard.Modifiers)
                {
                    case WModifierKeys.Alt:
                        return ModifierKeys.Alt;
                    case WModifierKeys.Control:
                        return ModifierKeys.Control;
                    case WModifierKeys.None:
                        return ModifierKeys.None;
                    case WModifierKeys.Shift:
                        return ModifierKeys.Shift;
                    case WModifierKeys.Windows:
                        return ModifierKeys.Windows;
                    default:
                        return ModifierKeys.None;
                }
            }
        }

        public MPoint ScreenToSource(MsaglMouseEventArgs e)
        {
            var p = new MPoint(e.X, e.Y);
            var m = Transform.Inverse;
            return m * p;
        }


        public IEnumerable<IViewerObject> Entities
        {
            get
            {
                foreach (var viewerObject in drawingObjectsToIViewerObjects.Values)
                {
                    yield return viewerObject;
                    var edge = viewerObject as VEdge;
                    if (edge != null)
                        if (edge.VLabel != null)
                            yield return edge.VLabel;
                }
            }
        }

        internal static double DpiXStatic
        {
            get
            {
                if (Math.Abs(_dpiX) <= 0.0001)
                    GetDpi();
                return _dpiX;
            }
        }

        static void GetDpi()
        {
            int hdcSrc = NativeMethods.GetWindowDC(NativeMethods.GetDesktopWindow());
            //LOGPIXELSX = 88,
            //LOGPIXELSY = 90,
            _dpiX = NativeMethods.GetDeviceCaps(hdcSrc, 88);
            _dpiY = NativeMethods.GetDeviceCaps(hdcSrc, 90);
            NativeMethods.ReleaseDC(NativeMethods.GetDesktopWindow(), hdcSrc);
        }

        public double DpiX
        {
            get { return DpiXStatic; }
        }

        public double DpiY
        {
            get { return DpiYStatic; }
        }

        static double DpiYStatic
        {
            get
            {
                if (Math.Abs(_dpiY) <= 0.0001)
                    GetDpi();
                return _dpiY;
            }
        }

        public void OnDragEnd(IEnumerable<IViewerObject> changedObjects)
        {
            throw new NotImplementedException();
        }

        public double LineThicknessForEditing { get; set; }

        /// <summary>
        /// the layout editing with the mouse is enabled if and only if this field is set to false
        /// </summary>
        public bool LayoutEditingEnabled { get; set; }

        public bool InsertingEdge { get; set; }

        public void PopupMenus(params Tuple<string, VoidDelegate>[] menuItems)
        {
            var contextMenu = new WContextMenu();
            foreach (var pair in menuItems)
                contextMenu.Items.Add(CreateMenuItem(pair.Item1, pair.Item2));
            contextMenu.Closed += ContextMenuClosed;
            WContextMenuService.SetContextMenu(_graphCanvas, contextMenu);

        }

        void ContextMenuClosed(object sender, WRoutedEventArgs e)
        {
            WContextMenuService.SetContextMenu(_graphCanvas, null);
        }

        public static object CreateMenuItem(string title, VoidDelegate voidVoidDelegate)
        {
            var menuItem = new WMenuItem { Header = title };
            // ReSharper disable once RedundantCast
            menuItem.Click += (WRoutedEventHandler)(delegate { voidVoidDelegate(); });
            return menuItem;
        }

        public double UnderlyingPolylineCircleRadius
        {
            get { return 0.1 * DpiX / CurrentScale; }
        }

        public Graph Graph
        {
            get { return _drawingGraph; }
            set
            {
                _drawingGraph = value;
                if (_drawingGraph != null)
                    Console.WriteLine("starting processing a graph with {0} nodes and {1} edges", _drawingGraph.NodeCount,
                        _drawingGraph.EdgeCount);
                ProcessGraph();
            }
        }

        const double DesiredPathThicknessInInches = 0.008;

        readonly Dictionary<DrawingObject, Func<DrawingObject, WFrameworkElement>> registeredCreators =
            new Dictionary<DrawingObject, Func<DrawingObject, WFrameworkElement>>();

        readonly ClickCounter clickCounter;

        [JetBrains.Annotations.UsedImplicitly]
        public string MsaglFileToSave = null;

        double GetBorderPathThickness()
        {
            return DesiredPathThicknessInInches * DpiX;
        }

        readonly Object _processGraphLock = new object();
        void ProcessGraph()
        {
            lock (_processGraphLock)
            {
                ProcessGraphUnderLock();
            }
        }

        void ProcessGraphUnderLock()
        {
            try
            {
                if (LayoutStarted != null)
                    LayoutStarted(null, null);

                CancelToken = new CancelToken();

                if (_drawingGraph == null) return;

                HideCanvas();
                ClearGraphViewer();
                CreateFrameworkElementsForLabelsOnly();
                if (NeedToCalculateLayout)
                {
                    _drawingGraph.CreateGeometryGraph(); //forcing the layout recalculation
                    if (_graphCanvas.Dispatcher.CheckAccess())
                        PopulateGeometryOfGeometryGraph();
                    else
                        _graphCanvas.Dispatcher.Invoke(PopulateGeometryOfGeometryGraph);
                }

                geometryGraphUnderLayout = _drawingGraph.GeometryGraph;
                if (RunLayoutAsync)
                    SetUpBackgrounWorkerAndRunAsync();
                else
                    RunLayoutInUIThread();
            }
            catch (Exception e)
            {
                WMessageBox.Show(e.ToString());
            }
        }

        void RunLayoutInUIThread()
        {
            LayoutGraph();
            PostLayoutStep();
            if (LayoutComplete != null)
                LayoutComplete(null, null);
        }

        void SetUpBackgrounWorkerAndRunAsync()
        {
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += (a, b) => LayoutGraph();
            _backgroundWorker.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Error != null)
                {
                    WMessageBox.Show(args.Error.ToString());
                    ClearGraphViewer();
                }
                else if (CancelToken.Canceled)
                {
                    ClearGraphViewer();
                }
                else
                {
                    if (_graphCanvas.Dispatcher.CheckAccess())
                        PostLayoutStep();
                    else
                        _graphCanvas.Dispatcher.Invoke(PostLayoutStep);
                }
                _backgroundWorker = null; //this will signal that we are not under layout anymore
                if (LayoutComplete != null)
                    LayoutComplete(null, null);
            };
            _backgroundWorker.RunWorkerAsync();
        }

        void HideCanvas()
        {
            if (_graphCanvas.Dispatcher.CheckAccess())
                _graphCanvas.Visibility = WVisibility.Hidden; // hide canvas while we lay it out asynchronously.
            else
                _graphCanvas.Dispatcher.Invoke(() => _graphCanvas.Visibility = WVisibility.Hidden);
        }


        void LayoutGraph()
        {
            if (NeedToCalculateLayout)
            {
                try
                {
                    LayoutHelpers.CalculateLayout(geometryGraphUnderLayout, _drawingGraph.LayoutAlgorithmSettings,
                                                  CancelToken);
                    if (MsaglFileToSave != null)
                    {
                        _drawingGraph.Write(MsaglFileToSave);
                        Console.WriteLine("saved into {0}", MsaglFileToSave);
                        Environment.Exit(0);
                    }
                }
                catch (OperationCanceledException)
                {
                    //swallow this exception
                }
            }
        }

        void PostLayoutStep()
        {
            _graphCanvas.Visibility = WVisibility.Visible;
            PushDataFromLayoutGraphToFrameworkElements();
            _backgroundWorker = null; //this will signal that we are not under layout anymore
            if (GraphChanged != null)
                GraphChanged(this, null);

            SetInitialTransform();
        }

        /// <summary>
        /// creates a viewer node
        /// </summary>
        /// <param name="drawingNode"></param>
        /// <returns></returns>
        public IViewerNode CreateIViewerNode(Drawing.Node drawingNode)
        {
            var frameworkElement = CreateTextBlockForDrawingObj(drawingNode);
            var width = frameworkElement.Width + 2 * drawingNode.Attr.LabelMargin;
            var height = frameworkElement.Height + 2 * drawingNode.Attr.LabelMargin;
            var bc = NodeBoundaryCurves.GetNodeBoundaryCurve(drawingNode, width, height);
            drawingNode.GeometryNode = new MNode(bc, drawingNode);
            var vNode = CreateVNode(drawingNode);
            layoutEditor.AttachLayoutChangeEvent(vNode);
            return vNode;
        }

        void ClearGraphViewer()
        {
            ClearGraphCanvasChildren();

            drawingObjectsToIViewerObjects.Clear();
            drawingObjectsToFrameworkElements.Clear();
        }

        void ClearGraphCanvasChildren()
        {
            if (_graphCanvas.Dispatcher.CheckAccess())
                _graphCanvas.Children.Clear();
            else _graphCanvas.Dispatcher.Invoke(() => _graphCanvas.Children.Clear());
        }

        /// <summary>
        /// zooms to the default view
        /// </summary>
        public void SetInitialTransform()
        {
            if (_drawingGraph == null || GeomGraph == null) return;

            var scale = FitFactor;
            var graphCenter = GeomGraph.BoundingBox.Center;
            var vp = new MRectangle(new MPoint(0, 0),
                                   new MPoint(_graphCanvas.RenderSize.Width, _graphCanvas.RenderSize.Height));

            SetTransformOnViewportWithoutRaisingViewChangeEvent(scale, graphCenter, vp);
        }

        public WImage DrawImage(string fileName)
        {
            var ltrans = _graphCanvas.LayoutTransform;
            var rtrans = _graphCanvas.RenderTransform;
            _graphCanvas.LayoutTransform = null;
            _graphCanvas.RenderTransform = null;
            var renderSize = _graphCanvas.RenderSize;

            double scale = FitFactor;
            int w = (int)(GeomGraph.Width * scale);
            int h = (int)(GeomGraph.Height * scale);

            SetTransformOnViewportWithoutRaisingViewChangeEvent(scale, GeomGraph.BoundingBox.Center, new MRectangle(0, 0, w, h));

            WSize size = new WSize(w, h);
            // Measure and arrange the surface
            // VERY IMPORTANT
            _graphCanvas.Measure(size);
            _graphCanvas.Arrange(new WRect(size));

            foreach (var node in _drawingGraph.Nodes.Concat(_drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf()))
            {
                if (drawingObjectsToIViewerObjects.TryGetValue(node, out IViewerObject o))
                {
                    ((VNode)o).Invalidate();
                }
            }

            WRenderTargetBitmap renderBitmap = new WRenderTargetBitmap(w, h, DpiX, DpiY, WPixelFormats.Pbgra32);
            renderBitmap.Render(_graphCanvas);

            if (fileName != null)
                // Create a file stream for saving image
                using (System.IO.FileStream outStream = new System.IO.FileStream(fileName, System.IO.FileMode.Create))
                {
                    // Use png encoder for our data
                    WPngBitmapEncoder encoder = new WPngBitmapEncoder();
                    // push the rendered bitmap to it
                    encoder.Frames.Add(WBitmapFrame.Create(renderBitmap));
                    // save the data to the stream
                    encoder.Save(outStream);
                }

            _graphCanvas.LayoutTransform = ltrans;
            _graphCanvas.RenderTransform = rtrans;
            _graphCanvas.Measure(renderSize);
            _graphCanvas.Arrange(new WRect(renderSize));

            return new WImage { Source = renderBitmap };
        }

        void SetTransformOnViewportWithoutRaisingViewChangeEvent(double scale, MPoint graphCenter, MRectangle vp)
        {
            var dx = vp.Width / 2 - scale * graphCenter.X;
            var dy = vp.Height / 2 + scale * graphCenter.Y;

            SetTransformWithoutRaisingViewChangeEvent(scale, dx, dy);

        }

        public MRectangle ClientViewportMappedToGraph
        {
            get
            {
                var t = Transform.Inverse;
                var p0 = new MPoint(0, 0);
                var p1 = new MPoint(_graphCanvas.RenderSize.Width, _graphCanvas.RenderSize.Height);
                return new MRectangle(t * p0, t * p1);
            }
        }


        void SetTransform(double scale, double dx, double dy)
        {
            if (ScaleIsOutOfRange(scale)) return;
            _graphCanvas.RenderTransform = new WMatrixTransform(scale, 0, 0, -scale, dx, dy);
            if (ViewChangeEvent != null)
                ViewChangeEvent(null, null);
        }

        void SetTransformWithoutRaisingViewChangeEvent(double scale, double dx, double dy)
        {
            if (ScaleIsOutOfRange(scale)) return;
            _graphCanvas.RenderTransform = new WMatrixTransform(scale, 0, 0, -scale, dx, dy);
        }

        bool ScaleIsOutOfRange(double scale)
        {
            return scale < 0.000001 || scale > 100000.0; //todo: remove hardcoded values
        }


        double FitFactor
        {
            get
            {
                var geomGraph = GeomGraph;
                if (_drawingGraph == null || geomGraph == null ||

                    Math.Abs(geomGraph.Width) < 0.0001 || Math.Abs(geomGraph.Height) < 0.0001)
                    return 1;

                var size = _graphCanvas.RenderSize;

                return GetFitFactor(size);
            }
        }

        double GetFitFactor(WSize rect)
        {
            var geomGraph = GeomGraph;
            return geomGraph == null ? 1 : Math.Min(rect.Width / geomGraph.Width, rect.Height / geomGraph.Height);
        }

        void PushDataFromLayoutGraphToFrameworkElements()
        {
            CreateRectToFillCanvas();
            CreateAndPositionGraphBackgroundRectangle();
            CreateVNodes();
            CreateEdges();
        }


        void CreateRectToFillCanvas()
        {
            var parent = (WPanel)GraphCanvas.Parent;
            _rectToFillCanvas = new WRectangle();
            WCanvas.SetLeft(_rectToFillCanvas, 0);
            WCanvas.SetTop(_rectToFillCanvas, 0);
            _rectToFillCanvas.Width = parent.ActualWidth;
            _rectToFillCanvas.Height = parent.ActualHeight;

            _rectToFillCanvas.Fill = WBrushes.Transparent;
            WPanel.SetZIndex(_rectToFillCanvas, -2);
            _graphCanvas.Children.Add(_rectToFillCanvas);
        }




        void CreateEdges()
        {
            foreach (var edge in _drawingGraph.Edges)
                CreateEdge(edge, null);
        }

        VEdge CreateEdge(MDrawingEdge edge, LgLayoutSettings lgSettings)
        {
            lock (this)
            {
                if (drawingObjectsToIViewerObjects.ContainsKey(edge))
                    return (VEdge)drawingObjectsToIViewerObjects[edge];
                if (lgSettings != null)
                    return CreateEdgeForLgCase(lgSettings, edge);

                drawingObjectsToFrameworkElements.TryGetValue(edge, out WFrameworkElement labelTextBox);
                var vEdge = new VEdge(edge, labelTextBox);

                var zIndex = ZIndexOfEdge(edge);
                drawingObjectsToIViewerObjects[edge] = vEdge;

                if (edge.Label != null)
                    SetVEdgeLabel(edge, vEdge, zIndex);

                WPanel.SetZIndex(vEdge.CurvePath, zIndex);
                _graphCanvas.Children.Add(vEdge.CurvePath);
                SetVEdgeArrowheads(vEdge, zIndex);

                return vEdge;
            }
        }

        int ZIndexOfEdge(MDrawingEdge edge)
        {
            var source = (VNode)drawingObjectsToIViewerObjects[edge.SourceNode];
            var target = (VNode)drawingObjectsToIViewerObjects[edge.TargetNode];

            var zIndex = Math.Max(source.ZIndex, target.ZIndex) + 1;
            return zIndex;
        }

        VEdge CreateEdgeForLgCase(LgLayoutSettings lgSettings, MDrawingEdge edge)
        {
            return (VEdge)(drawingObjectsToIViewerObjects[edge] = new VEdge(edge, lgSettings)
            {
                PathStrokeThicknessFunc = () => GetBorderPathThickness() * edge.Attr.LineWidth
            });
        }

        void SetVEdgeLabel(MDrawingEdge edge, VEdge vEdge, int zIndex)
        {
            if (!drawingObjectsToFrameworkElements.TryGetValue(edge, out WFrameworkElement frameworkElementForEdgeLabel))
            {
                drawingObjectsToFrameworkElements[edge] =
                    frameworkElementForEdgeLabel = CreateTextBlockForDrawingObj(edge);
                frameworkElementForEdgeLabel.Tag = new VLabel(edge, frameworkElementForEdgeLabel);
            }

            vEdge.VLabel = (VLabel)frameworkElementForEdgeLabel.Tag;
            if (frameworkElementForEdgeLabel.Parent == null)
            {
                _graphCanvas.Children.Add(frameworkElementForEdgeLabel);
                WPanel.SetZIndex(frameworkElementForEdgeLabel, zIndex);
            }
        }

        void SetVEdgeArrowheads(VEdge vEdge, int zIndex)
        {
            if (vEdge.SourceArrowHeadPath != null)
            {
                WPanel.SetZIndex(vEdge.SourceArrowHeadPath, zIndex);
                _graphCanvas.Children.Add(vEdge.SourceArrowHeadPath);
            }
            if (vEdge.TargetArrowHeadPath != null)
            {
                WPanel.SetZIndex(vEdge.TargetArrowHeadPath, zIndex);
                _graphCanvas.Children.Add(vEdge.TargetArrowHeadPath);
            }
        }

        void CreateVNodes()
        {
            foreach (var node in _drawingGraph.Nodes.Concat(_drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf()))
            {
                CreateVNode(node);
                Invalidate(drawingObjectsToIViewerObjects[node]);
            }
        }

        IViewerNode CreateVNode(Drawing.Node node)
        {
            lock (this)
            {
                if (drawingObjectsToIViewerObjects.ContainsKey(node))
                    return (IViewerNode)drawingObjectsToIViewerObjects[node];

                if (!drawingObjectsToFrameworkElements.TryGetValue(node, out WFrameworkElement feOfLabel))
                    feOfLabel = CreateAndRegisterFrameworkElementOfDrawingNode(node);

                var vn = new VNode(node, feOfLabel,
                    e => (VEdge)drawingObjectsToIViewerObjects[e], () => GetBorderPathThickness() * node.Attr.LineWidth);

                foreach (var fe in vn.FrameworkElements)
                    _graphCanvas.Children.Add(fe);

                drawingObjectsToIViewerObjects[node] = vn;

                return vn;
            }
        }

        // MIHO added this
        public IEnumerable<IViewerNode> GetViewerNodes()
        {
            if (drawingObjectsToIViewerObjects != null)
                foreach (var x in drawingObjectsToIViewerObjects.Values)
                    if (x is IViewerNode vn)
                        yield return vn;
        }

        public WFrameworkElement CreateAndRegisterFrameworkElementOfDrawingNode(Drawing.Node node)
        {
            lock (this)
                return drawingObjectsToFrameworkElements[node] = CreateTextBlockForDrawingObj(node);
        }

        void CreateAndPositionGraphBackgroundRectangle()
        {
            CreateGraphBackgroundRect();
            SetBackgroundRectanglePositionAndSize();

            var rect = _rectToFillGraphBackground as WRectangle;
            if (rect != null)
            {
                rect.Fill = Common.BrushFromMsaglColor(_drawingGraph.Attr.BackgroundColor);
            }
            WPanel.SetZIndex(_rectToFillGraphBackground, -1);
            _graphCanvas.Children.Add(_rectToFillGraphBackground);
        }

        void CreateGraphBackgroundRect()
        {
            var lgGraphBrowsingSettings = _drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (lgGraphBrowsingSettings == null)
            {
                _rectToFillGraphBackground = new WRectangle();
            }
        }


        void SetBackgroundRectanglePositionAndSize()
        {
            if (GeomGraph == null) return;
            _rectToFillGraphBackground.Width = GeomGraph.Width;
            _rectToFillGraphBackground.Height = GeomGraph.Height;

            var center = GeomGraph.BoundingBox.Center;
            Common.PositionFrameworkElement(_rectToFillGraphBackground, center, 1);
        }


        void PopulateGeometryOfGeometryGraph()
        {
            geometryGraphUnderLayout = _drawingGraph.GeometryGraph;
            foreach (
                MNode msaglNode in
                    geometryGraphUnderLayout.Nodes)
            {
                var node = (Drawing.Node)msaglNode.UserData;
                if (_graphCanvas.Dispatcher.CheckAccess())
                    msaglNode.BoundaryCurve = GetNodeBoundaryCurve(node);
                else
                {
                    var msagNodeInThread = msaglNode;
                    _graphCanvas.Dispatcher.Invoke(() => msagNodeInThread.BoundaryCurve = GetNodeBoundaryCurve(node));
                }
            }

            foreach (
                Cluster cluster in geometryGraphUnderLayout.RootCluster.AllClustersWideFirstExcludingSelf())
            {
                var subgraph = (Subgraph)cluster.UserData;
                if (_graphCanvas.Dispatcher.CheckAccess())
                    cluster.CollapsedBoundary = GetClusterCollapsedBoundary(subgraph);
                else
                {
                    var clusterInThread = cluster;
                    _graphCanvas.Dispatcher.Invoke(
                        () => clusterInThread.BoundaryCurve = GetClusterCollapsedBoundary(subgraph));
                }
                if (cluster.RectangularBoundary == null)
                    cluster.RectangularBoundary = new RectangularClusterBoundary();
                cluster.RectangularBoundary.TopMargin = subgraph.DiameterOfOpenCollapseButton + 0.5 +
                                                        subgraph.Attr.LineWidth / 2;
            }

            foreach (var msaglEdge in geometryGraphUnderLayout.Edges)
            {
                var drawingEdge = (MDrawingEdge)msaglEdge.UserData;
                AssignLabelWidthHeight(msaglEdge, drawingEdge);
            }
        }

        ICurve GetClusterCollapsedBoundary(Subgraph subgraph)
        {
            double width, height;

            if (drawingObjectsToFrameworkElements.TryGetValue(subgraph, out WFrameworkElement fe))
            {

                width = fe.Width + 2 * subgraph.Attr.LabelMargin + subgraph.DiameterOfOpenCollapseButton;
                height = Math.Max(fe.Height + 2 * subgraph.Attr.LabelMargin, subgraph.DiameterOfOpenCollapseButton);
            }
            else
                return GetApproximateCollapsedBoundary(subgraph);

            if (width < _drawingGraph.Attr.MinNodeWidth)
                width = _drawingGraph.Attr.MinNodeWidth;
            if (height < _drawingGraph.Attr.MinNodeHeight)
                height = _drawingGraph.Attr.MinNodeHeight;
            return NodeBoundaryCurves.GetNodeBoundaryCurve(subgraph, width, height);
        }

        ICurve GetApproximateCollapsedBoundary(Subgraph subgraph)
        {
            if (textBoxForApproxNodeBoundaries == null)
                SetUpTextBoxForApproxNodeBoundaries();


            double width, height;
            if (String.IsNullOrEmpty(subgraph.LabelText))
                height = width = subgraph.DiameterOfOpenCollapseButton;
            else
            {
                double a = ((double)subgraph.LabelText.Length) / textBoxForApproxNodeBoundaries.Text.Length *
                           subgraph.Label.FontSize / MLabel.DefaultFontSize;
                width = textBoxForApproxNodeBoundaries.Width * a + subgraph.DiameterOfOpenCollapseButton;
                height =
                    Math.Max(
                        textBoxForApproxNodeBoundaries.Height * subgraph.Label.FontSize / MLabel.DefaultFontSize,
                        subgraph.DiameterOfOpenCollapseButton);
            }

            if (width < _drawingGraph.Attr.MinNodeWidth)
                width = _drawingGraph.Attr.MinNodeWidth;
            if (height < _drawingGraph.Attr.MinNodeHeight)
                height = _drawingGraph.Attr.MinNodeHeight;

            return NodeBoundaryCurves.GetNodeBoundaryCurve(subgraph, width, height);
        }


        void AssignLabelWidthHeight(Core.Layout.ILabeledObject labeledGeomObj,
                                    DrawingObject drawingObj)
        {
            if (drawingObjectsToFrameworkElements.ContainsKey(drawingObj))
            {
                WFrameworkElement fe = drawingObjectsToFrameworkElements[drawingObj];
                labeledGeomObj.Label.Width = fe.Width;
                labeledGeomObj.Label.Height = fe.Height;
            }
        }


        ICurve GetNodeBoundaryCurve(Drawing.Node node)
        {
            double width, height;

            if (drawingObjectsToFrameworkElements.TryGetValue(node, out WFrameworkElement fe))
            {
                width = fe.Width + 2 * node.Attr.LabelMargin;
                height = fe.Height + 2 * node.Attr.LabelMargin;
            }
            else
                return GetNodeBoundaryCurveByMeasuringText(node);

            if (width < _drawingGraph.Attr.MinNodeWidth)
                width = _drawingGraph.Attr.MinNodeWidth;
            if (height < _drawingGraph.Attr.MinNodeHeight)
                height = _drawingGraph.Attr.MinNodeHeight;
            return NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
        }

        WTextBlock textBoxForApproxNodeBoundaries;

        public static WSize MeasureText(string text,
        WFontFamily family, double size)
        {


#pragma warning disable 618
            WFormattedText formattedText = new WFormattedText(
#pragma warning restore 618
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                WFlowDirection.LeftToRight,
                new WTypeface(family, new WFontStyle(), WFontWeights.Regular, WFontStretches.Normal),
                size,
                WBrushes.Black,
                null);

            return new WSize(formattedText.Width, formattedText.Height);
        }

        ICurve GetNodeBoundaryCurveByMeasuringText(Drawing.Node node)
        {
            double width, height;
            if (String.IsNullOrEmpty(node.LabelText))
            {
                width = 10;
                height = 10;
            }
            else
            {
                var size = MeasureText(node.LabelText, new WFontFamily(node.Label.FontName), node.Label.FontSize);
                width = size.Width;
                height = size.Height;
            }

            width += 2 * node.Attr.LabelMargin;
            height += 2 * node.Attr.LabelMargin;

            if (width < _drawingGraph.Attr.MinNodeWidth)
                width = _drawingGraph.Attr.MinNodeWidth;
            if (height < _drawingGraph.Attr.MinNodeHeight)
                height = _drawingGraph.Attr.MinNodeHeight;

            return NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
        }


        void SetUpTextBoxForApproxNodeBoundaries()
        {
            textBoxForApproxNodeBoundaries = new WTextBlock
            {
                Text = "Fox jumping over River",
                FontFamily = new WFontFamily(MLabel.DefaultFontName),
                FontSize = MLabel.DefaultFontSize,
            };

            textBoxForApproxNodeBoundaries.Measure(new WSize(double.PositiveInfinity, double.PositiveInfinity));
            textBoxForApproxNodeBoundaries.Width = textBoxForApproxNodeBoundaries.DesiredSize.Width;
            textBoxForApproxNodeBoundaries.Height = textBoxForApproxNodeBoundaries.DesiredSize.Height;
        }


        void CreateFrameworkElementsForLabelsOnly()
        {
            foreach (var edge in _drawingGraph.Edges)
            {
                var fe = CreateDefaultFrameworkElementForDrawingObject(edge);
                if (fe != null)
                    if (_graphCanvas.Dispatcher.CheckAccess())
                        fe.Tag = new VLabel(edge, fe);
                    else
                    {
                        var localEdge = edge;
                        _graphCanvas.Dispatcher.Invoke(() => fe.Tag = new VLabel(localEdge, fe));
                    }
            }

            foreach (var node in _drawingGraph.Nodes)
                CreateDefaultFrameworkElementForDrawingObject(node);
            if (_drawingGraph.RootSubgraph != null)
                foreach (var subgraph in _drawingGraph.RootSubgraph.AllSubgraphsWidthFirstExcludingSelf())
                    CreateDefaultFrameworkElementForDrawingObject(subgraph);
        }

        public void RegisterLabelCreator(DrawingObject drawingObject, Func<DrawingObject, WFrameworkElement> func)
        {
            registeredCreators[drawingObject] = func;
        }

        public void UnregisterLabelCreator(DrawingObject drawingObject)
        {
            registeredCreators.Remove(drawingObject);
        }

        public Func<DrawingObject, WFrameworkElement> GetLabelCreator(DrawingObject drawingObject)
        {
            return registeredCreators[drawingObject];
        }

        WFrameworkElement CreateTextBlockForDrawingObj(DrawingObject drawingObj)
        {
            if (registeredCreators.TryGetValue(drawingObj, out Func<DrawingObject, WFrameworkElement> registeredCreator))
                return registeredCreator(drawingObj);
            if (drawingObj is Subgraph)
                return null; //todo: add Label support later
            var labeledObj = drawingObj as MILabeledObject;
            if (labeledObj == null)
                return null;

            var drawingLabel = labeledObj.Label;
            if (drawingLabel == null)
                return null;

            WTextBlock textBlock = null;
            if (_graphCanvas.Dispatcher.CheckAccess())
                textBlock = CreateTextBlock(drawingLabel);
            else
                _graphCanvas.Dispatcher.Invoke(() => textBlock = CreateTextBlock(drawingLabel));

            return textBlock;
        }

        static WTextBlock CreateTextBlock(MLabel drawingLabel)
        {
            var textBlock = new WTextBlock
            {
                Tag = drawingLabel,
                Text = drawingLabel.Text,
                FontFamily = new WFontFamily(drawingLabel.FontName),
                FontSize = drawingLabel.FontSize,
                Foreground = Common.BrushFromMsaglColor(drawingLabel.FontColor)
            };

            textBlock.Measure(new WSize(double.PositiveInfinity, double.PositiveInfinity));
            textBlock.Width = textBlock.DesiredSize.Width;
            textBlock.Height = textBlock.DesiredSize.Height;
            return textBlock;
        }


        WFrameworkElement CreateDefaultFrameworkElementForDrawingObject(DrawingObject drawingObject)
        {
            lock (this)
            {
                var textBlock = CreateTextBlockForDrawingObj(drawingObject);
                if (textBlock != null)
                    drawingObjectsToFrameworkElements[drawingObject] = textBlock;
                return textBlock;
            }
        }




        public void DrawRubberLine(MsaglMouseEventArgs args)
        {
            DrawRubberLine(ScreenToSource(args));
        }

        public void StopDrawingRubberLine()
        {
            _graphCanvas.Children.Remove(_rubberLinePath);
            _rubberLinePath = null;
            _graphCanvas.Children.Remove(_targetArrowheadPathForRubberEdge);
            _targetArrowheadPathForRubberEdge = null;
        }

        public void AddEdge(IViewerEdge edge, bool registerForUndo)
        {
            var drawingEdge = edge.Edge;
            MEdge geomEdge = drawingEdge.GeometryEdge;

            _drawingGraph.AddPrecalculatedEdge(drawingEdge);
            _drawingGraph.GeometryGraph.Edges.Add(geomEdge);

        }

        public IViewerEdge CreateEdgeWithGivenGeometry(MDrawingEdge drawingEdge)
        {
            return CreateEdge(drawingEdge, _drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings);
        }

        public void AddNode(IViewerNode node, bool registerForUndo)
        {
            if (_drawingGraph == null)
                throw new InvalidOperationException(); // adding a node when the graph does not exist
            var vNode = (VNode)node;
            _drawingGraph.AddNode(vNode.Node);
            _drawingGraph.GeometryGraph.Nodes.Add(vNode.Node.GeometryNode);
            layoutEditor.AttachLayoutChangeEvent(vNode);
            _graphCanvas.Children.Add(vNode.FrameworkElementOfNodeForLabel);
            layoutEditor.CleanObstacles();
        }

        public IViewerObject AddNode(Drawing.Node drawingNode)
        {
            Graph.AddNode(drawingNode);
            var vNode = CreateVNode(drawingNode);
            LayoutEditor.AttachLayoutChangeEvent(vNode);
            LayoutEditor.CleanObstacles();
            return vNode;
        }

        public void RemoveEdge(IViewerEdge edge, bool registerForUndo)
        {
            lock (this)
            {
                var vedge = (VEdge)edge;
                var dedge = vedge.Edge;
                _drawingGraph.RemoveEdge(dedge);
                _drawingGraph.GeometryGraph.Edges.Remove(dedge.GeometryEdge);
                drawingObjectsToFrameworkElements.Remove(dedge);
                drawingObjectsToIViewerObjects.Remove(dedge);

                vedge.RemoveItselfFromCanvas(_graphCanvas);
            }
        }

        public void RemoveNode(IViewerNode node, bool registerForUndo)
        {
            lock (this)
            {
                RemoveEdges(node.Node.OutEdges);
                RemoveEdges(node.Node.InEdges);
                RemoveEdges(node.Node.SelfEdges);
                drawingObjectsToFrameworkElements.Remove(node.Node);
                drawingObjectsToIViewerObjects.Remove(node.Node);
                var vnode = (VNode)node;
                vnode.DetouchFromCanvas(_graphCanvas);

                _drawingGraph.RemoveNode(node.Node);
                _drawingGraph.GeometryGraph.Nodes.Remove(node.Node.GeometryNode);
                layoutEditor.DetachNode(node);
                layoutEditor.CleanObstacles();
            }
        }

        void RemoveEdges(IEnumerable<MDrawingEdge> drawingEdges)
        {
            foreach (var de in drawingEdges.ToArray())
            {
                var vedge = (VEdge)drawingObjectsToIViewerObjects[de];
                RemoveEdge(vedge, false);
            }
        }


        public IViewerEdge RouteEdge(MDrawingEdge drawingEdge)
        {
            var geomEdge = GeometryGraphCreator.CreateGeometryEdgeFromDrawingEdge(drawingEdge);
            var geomGraph = _drawingGraph.GeometryGraph;
            LayoutHelpers.RouteAndLabelEdges(
                geomGraph,
                _drawingGraph.LayoutAlgorithmSettings,
                new[] { geomEdge },
                10,                             // new in new nuget package
                new CancelToken());             // new in new nuget package
            return CreateEdge(drawingEdge, _drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings);
        }

        public IViewerGraph ViewerGraph { get; set; }

        public double ArrowheadLength
        {
            get { return 0.2 * DpiX / CurrentScale; }
        }

        public void SetSourcePortForEdgeRouting(MPoint portLocation)
        {
            _sourcePortLocationForEdgeRouting = portLocation;
            if (_sourcePortCircle == null)
            {
                _sourcePortCircle = CreatePortPath();
                _graphCanvas.Children.Add(_sourcePortCircle);
            }
            _sourcePortCircle.Width = _sourcePortCircle.Height = UnderlyingPolylineCircleRadius;
            _sourcePortCircle.StrokeThickness = _sourcePortCircle.Width / 10;
            Common.PositionFrameworkElement(_sourcePortCircle, portLocation, 1);
        }

        WEllipse CreatePortPath()
        {
            return new WEllipse
            {
                Stroke = WBrushes.Brown,
                Fill = WBrushes.Brown,
            };
        }



        public void SetTargetPortForEdgeRouting(MPoint portLocation)
        {
            if (TargetPortCircle == null)
            {
                TargetPortCircle = CreatePortPath();
                _graphCanvas.Children.Add(TargetPortCircle);
            }
            TargetPortCircle.Width = TargetPortCircle.Height = UnderlyingPolylineCircleRadius;
            TargetPortCircle.StrokeThickness = TargetPortCircle.Width / 10;
            Common.PositionFrameworkElement(TargetPortCircle, portLocation, 1);
        }

        public void RemoveSourcePortEdgeRouting()
        {
            _graphCanvas.Children.Remove(_sourcePortCircle);
            _sourcePortCircle = null;
        }

        public void RemoveTargetPortEdgeRouting()
        {
            _graphCanvas.Children.Remove(TargetPortCircle);
            TargetPortCircle = null;
        }


        public void DrawRubberEdge(EdgeGeometry edgeGeometry)
        {
            if (_rubberEdgePath == null)
            {
                _rubberEdgePath = new WPath
                {
                    Stroke = WBrushes.Black,
                    StrokeThickness = GetBorderPathThickness() * 3
                };
                _graphCanvas.Children.Add(_rubberEdgePath);
                _targetArrowheadPathForRubberEdge = new WPath
                {
                    Stroke = WBrushes.Black,
                    StrokeThickness = GetBorderPathThickness() * 3
                };
                _graphCanvas.Children.Add(_targetArrowheadPathForRubberEdge);
            }
            _rubberEdgePath.Data = VEdge.GetICurveWpfGeometry(edgeGeometry.Curve);
            _targetArrowheadPathForRubberEdge.Data = VEdge.DefiningTargetArrowHead(edgeGeometry,
                                                                                  edgeGeometry.LineWidth);
        }


        bool UnderLayout
        {
            get { return _backgroundWorker != null; }
        }

        public void StopDrawingRubberEdge()
        {
            _graphCanvas.Children.Remove(_rubberEdgePath);
            _graphCanvas.Children.Remove(_targetArrowheadPathForRubberEdge);
            _rubberEdgePath = null;
            _targetArrowheadPathForRubberEdge = null;
        }


        public PlaneTransformation Transform
        {
            get
            {
                var mt = _graphCanvas.RenderTransform as WMatrixTransform;
                if (mt == null)
                    return PlaneTransformation.UnitTransformation;
                var m = mt.Matrix;
                return new PlaneTransformation(m.M11, m.M12, m.OffsetX, m.M21, m.M22, m.OffsetY);
            }
            set
            {
                SetRenderTransformWithoutRaisingEvents(value);

                if (ViewChangeEvent != null)
                    ViewChangeEvent(null, null);
            }
        }

        void SetRenderTransformWithoutRaisingEvents(PlaneTransformation value)
        {
            _graphCanvas.RenderTransform = new WMatrixTransform(value[0, 0], value[0, 1], value[1, 0], value[1, 1],
                                                              value[0, 2],
                                                              value[1, 2]);
        }


        public bool NeedToCalculateLayout
        {
            get { return needToCalculateLayout; }
            set { needToCalculateLayout = value; }
        }

        /// <summary>
        /// the cancel token used to cancel a long running layout
        /// </summary>
        public CancelToken CancelToken
        {
            get { return _cancelToken; }
            set { _cancelToken = value; }
        }

        /// <summary>
        /// no layout is done, but the overlap is removed for graphs with geometry
        /// </summary>
        public bool NeedToRemoveOverlapOnly { get; set; }


        public void DrawRubberLine(MPoint rubberEnd)
        {
            if (_rubberLinePath == null)
            {
                _rubberLinePath = new WPath
                {
                    Stroke = WBrushes.Black,
                    StrokeThickness = GetBorderPathThickness() * 3
                };
                _graphCanvas.Children.Add(_rubberLinePath);
            }
            _rubberLinePath.Data =
                VEdge.GetICurveWpfGeometry(new MLineSegment(_sourcePortLocationForEdgeRouting, rubberEnd));
        }

        public void StartDrawingRubberLine(MPoint startingPoint)
        {
        }

        #endregion



        public IViewerNode CreateIViewerNode(Drawing.Node drawingNode, MPoint center, object visualElement)
        {
            if (_drawingGraph == null)
                return null;
            var frameworkElement = visualElement as WFrameworkElement ?? CreateTextBlockForDrawingObj(drawingNode);
            var width = frameworkElement.Width + 2 * drawingNode.Attr.LabelMargin;
            var height = frameworkElement.Height + 2 * drawingNode.Attr.LabelMargin;
            var bc = NodeBoundaryCurves.GetNodeBoundaryCurve(drawingNode, width, height);
            drawingNode.GeometryNode = new MNode(bc, drawingNode) { Center = center };
            var vNode = CreateVNode(drawingNode);
            _drawingGraph.AddNode(drawingNode);
            _drawingGraph.GeometryGraph.Nodes.Add(drawingNode.GeometryNode);
            layoutEditor.AttachLayoutChangeEvent(vNode);
            MakeRoomForNewNode(drawingNode);

            return vNode;
        }

        void MakeRoomForNewNode(Drawing.Node drawingNode)
        {
            IncrementalDragger incrementalDragger = new IncrementalDragger(new[] { drawingNode.GeometryNode },
                                                                           Graph.GeometryGraph,
                                                                           Graph.LayoutAlgorithmSettings);
            incrementalDragger.Drag(new MPoint());

            foreach (var n in incrementalDragger.ChangedGraph.Nodes)
            {
                var dn = (Drawing.Node)n.UserData;
                var vn = drawingObjectsToIViewerObjects[dn] as VNode;
                if (vn != null)
                    vn.Invalidate();
            }

            foreach (var n in incrementalDragger.ChangedGraph.Edges)
            {
                var dn = (Drawing.Edge)n.UserData;
                var ve = drawingObjectsToIViewerObjects[dn] as VEdge;
                if (ve != null)
                    ve.Invalidate();
            }
        }
    }
}
