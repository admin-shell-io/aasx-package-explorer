/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Aml.Engine.CAEX;
using Mtp.DynamicInstances;
using WpfMtpControl.DataSources;
using static WpfMtpControl.MtpData;

namespace WpfMtpControl
{
    public partial class MtpVisuViewer : UserControl
    {
        //
        // External properties
        //

        public delegate void MtpObjectDoubleClickHandler(MtpData.MtpBaseObject source);
        public event MtpObjectDoubleClickHandler MtpObjectDoubleClick;

        public MtpVisuOptions VisuOptions = new MtpVisuOptions();

        //
        // Internal properties
        //

        public MtpVisuViewer()
        {
            InitializeComponent();
        }

        private void Canvas_Loaded(object sender, RoutedEventArgs e)
        {
            // Timer for loading
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();
        }

        public UIElementHelper.FontSettings LabelFontSettings = new UIElementHelper.FontSettings(
            new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Condensed, 18.0);

        public void SetLineStyle(Shape l, int elementClass)
        {
            if (elementClass == 100)
            {
            }
            if (elementClass == 101)
            {
                DoubleCollection dashes = new DoubleCollection();
                dashes.Add(2);
                dashes.Add(2);
                l.StrokeDashArray = dashes;
            }
            if (elementClass == 102)
            {
                DoubleCollection dashes = new DoubleCollection();
                dashes.Add(4);
                dashes.Add(2);
                dashes.Add(1);
                dashes.Add(2);
                l.StrokeDashArray = dashes;
            }
        }

        public Rectangle ConstructRect(Brush stroke, double strokeThickness, Brush fill = null)
        {
            var res = new Rectangle();
            res.Stroke = stroke;
            res.StrokeThickness = strokeThickness;
            if (fill != null)
                res.Fill = fill;
            return res;
        }

        public Ellipse ConstructEllipse(Nullable<double> strokeThickness = null, Brush stroke = null, Brush fill = null)
        {
            var res = new Ellipse();
            if (stroke != null)
                res.Stroke = stroke;
            if (strokeThickness != null)
                res.StrokeThickness = strokeThickness.Value;
            if (fill != null)
                res.Fill = fill;
            return res;
        }

        public Canvas ConstructContentObject(MtpVisualObjectRecord xaml)
        {
            var contentObject = xaml?.Symbol?.SymbolData as Canvas;
            contentObject = UIElementHelper.cloneElement(contentObject) as Canvas;
            return contentObject;
        }

        public Viewbox ConstructViewboxVO(FrameworkElement contentObject, double rotation)
        {
            // trivial
            if (contentObject == null)
                return null;

            // make such object
            var viso = new ContentControl();
            viso.Content = contentObject;

            // rotation in degree, mathematically positive == anti clock wise
            var rt = new RotateTransform(-rotation, contentObject.Width / 2, contentObject.Height / 2);
            viso.RenderTransform = rt;

            var vb = new Viewbox();
            vb.Child = viso;
            vb.Stretch = Stretch.Fill;

            // ok
            return vb;
        }

        public ContentControl ConstructDirectVO(
            FrameworkElement contentObject, double scale, double rotation, Point center)
        {
            // trivial
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (contentObject == null || center == null)
                return null;

            // rotation in degree, mathematically positive == anti clock wise
            var tg = new TransformGroup();
            tg.Children.Add(new RotateTransform(-rotation, center.X, center.Y));
            tg.Children.Add(new ScaleTransform(scale, scale, center.X, center.Y));
            // Note: the following was disabled by MIHO at same stage:
            //// tg.Children.Add(new ScaleTransform(scale, scale, 0
            //// .0 * center.X + contentObject.Width / 2, 0.0 * center.Y + contentObject.Height / 2));
            contentObject.RenderTransform = tg;

            // make such object
            var viso = new ContentControl();
            viso.Content = contentObject;
            viso.Width = contentObject.Width;
            viso.Height = contentObject.Height;

            // ok
            return viso;
        }

        public void DrawToCanvasAtPositionSize(
            Canvas canvas, double x, double y, double width, double height, FrameworkElement fe)
        {
            if (canvas == null || fe == null)
                return;
            fe.Width = width;
            fe.Height = height;
            canvas.Children.Add(fe);
            Canvas.SetLeft(fe, x);
            Canvas.SetTop(fe, y);
        }

        public void DrawHandlePoint(Canvas canvas, double x, double y, bool draw = true, Brush handleBrush = null)
        {
            if (!draw)
                return;
            var hb = (handleBrush == null) ? Brushes.Blue : handleBrush;
            var p = ConstructEllipse(fill: hb);
            DrawToCanvasAtPositionSize(canvas, x - 4.0, y - 4.0, 8.0, 8.0, p);
        }

        public void DrawMtpPictureIntoCanvas(
            Canvas canvas, MtpData.MtpPicture pic,
            bool drawHandlePoints = true, bool drawBoundingBoxes = true)
        {
            // clear access
            if (canvas == null || pic == null)
                return;
            canvas.Children.Clear();

            // prepare options correctly
            this.VisuOptions?.Prepare();

            // for some tests, we need randomness
#if _only_testing
            var rnd = new Random();
#endif
            // first, set up the canvas
            if (true)
            {
                if (pic.TotalSize.Width < 1 || pic.TotalSize.Height < 1)
                    return;
                canvas.Width = pic.TotalSize.Width;
                canvas.Height = pic.TotalSize.Height;

                if (this.VisuOptions?.BackgroundBrush != null)
                {
                    canvas.Background = this.VisuOptions.BackgroundBrush;
                    scrollViewerVisu.Background = this.VisuOptions.BackgroundBrush;
                }
            }

            // assume, that the elements below are in a list
            foreach (var obj in pic.Objects)
            {
                // the check described in VDI2658 rely on RefBaseSystemUnitPath
                if (obj == null || obj.Name == null)
                    continue;

                //
                // Pipe, FunctionLine, MeasurementLine
                //
                if (obj is MtpData.MtpConnectionObject conn)
                {
                    // make appropriate poly line
                    var l = new Polyline();
                    l.Stroke = Brushes.Black;
                    l.StrokeThickness = 1;
                    l.Points = conn.points;

                    // line style
                    SetLineStyle(l, obj.ObjClass);

                    // draw
                    l.Tag = conn;
                    canvas.Children.Add(l);
                }

                //
                // VisualObject
                //
                if (obj is MtpData.MtpVisualObject vo)
                {
                    // debug?
                    if (obj.Name == "V001")
                    {
                        ;
                    }

                    // search
                    var symbol = vo?.visObj?.Symbol;
                    if (symbol?.SymbolData == null)
                    {
                        // make missing bounding box
                        if (drawBoundingBoxes && vo.x.HasValue && vo.y.HasValue
                            && vo.width.HasValue && vo.height.HasValue)
                        {
                            // box
                            DrawToCanvasAtPositionSize(canvas, vo.x.Value, vo.y.Value, vo.width.Value,
                                vo.height.Value, ConstructRect(Brushes.Red, 2.0));

                            // label?
                            var labeltb = UIElementHelper.CreateStickyLabel(this.LabelFontSettings, "" + vo.Name);
                            UIElementHelper.DrawToCanvasAtPositionAligned(canvas,
                                vo.x.Value + vo.width.Value / 2,
                                vo.y.Value + vo.height.Value / 2,
                                UIElementHelper.DrawToCanvasAlignment.Centered, labeltb);
                        }

                        continue;
                    }

                    // make a NEW content object to display & manipulate
                    var contentObject = symbol.SymbolData as Canvas;
                    if (contentObject == null)
                        continue;

                    contentObject = UIElementHelper.cloneElement(contentObject) as Canvas;

                    // delete not necessary artifacts in the XAML
                    UIElementHelper.FindNozzlesViaTags(contentObject, "Nozzle", extractShapes: true);
                    UIElementHelper.FindNozzlesViaTags(contentObject, "Label", extractShapes: true);

                    // same logic for potential dynamic instance
                    var dynInstanceVO = vo.dynInstance?.CreateVisualObject(vo.width.Value, vo.height.Value);

                    //
                    // how to draw content?
                    //
                    if (dynInstanceVO != null)
                    {
                        // draw an dynamic instance object

                        // make bounding box Rect
                        if (drawBoundingBoxes)
                            DrawToCanvasAtPositionSize(canvas, vo.x.Value, vo.y.Value, vo.width.Value,
                                vo.height.Value, ConstructRect(Brushes.Violet, 1.0));

                        // draw it
                        var vb = ConstructViewboxVO(dynInstanceVO, 0.0 /* vo.rotation.Value */);
                        vb.Stretch = Stretch.Uniform;
                        vb.Tag = vo;
                        DrawToCanvasAtPositionSize(canvas, vo.x.Value, vo.y.Value, vo.width.Value, vo.height.Value, vb);
                    }

                    if (vo.dynInstance == null || vo.dynInstance.DrawSymbolAsWell)
                    {
                        if (vo.visObj != null && contentObject != null)
                        {
                            // how to draw based on valid vis obj information                        
                            if (vo.visObj.Placement == MtpSymbol.SymbolPlaceType.FitNozzles && vo.nozzlePoints.Count > 0
                                && symbol.NozzlePos != null && symbol.NozzlePos.Length > 0)
                            {
                                // magnetically snap in
                                // COG of "surrounding" nozzles, and distance
                                var npArr = vo.nozzlePoints.ToArray();
                                var npCOG = UIElementHelper.ComputeCOG(npArr);
                                var npRadius = UIElementHelper.ComputeRadiusForCenterPointer(npArr, npCOG.Value);

                                // COG of content nozzles, and distance
                                var contentCOG = UIElementHelper.ComputeCOG(symbol.NozzlePos);
                                var contentRadius = UIElementHelper.ComputeRadiusForCenterPointer(
                                                        symbol.NozzlePos, contentCOG.Value);

                                // compute the delta between visual object's mid an its nozzle COG
                                var contentCogToMid = new Point(
                                    contentCOG.Value.X - contentObject.Width / 2.0,
                                    contentCOG.Value.Y - contentObject.Height / 2.0);

                                if (npArr == null || npCOG == null || npRadius == null
                                    || contentCOG == null || contentRadius == null)
                                    continue;

                                // based on the radius and COG information, construct a start vector
                                // FIX: radius could be 0!
                                var start = new UIElementHelper.Transformation2D(
                                    (contentRadius.Value < 0.01) ? 1.0 : npRadius.Value / contentRadius.Value,
                                    vo.rotation.Value,
                                    npCOG.Value.X, npCOG.Value.Y
                                );

                                // disturb it?
#if _only_testing
#pragma warning disable 162
                                // ReSharper disable once HeuristicUnreachableCode
                                {
                                    // ReSharper disable once HeuristicUnreachableCode
                                    start.Rot += -15.0 + 30.0 * rnd.NextDouble();
                                    start.Scale *= 0.8 + 0.4 * rnd.NextDouble();
                                    start.OfsX += -5.0 + 10.0 * rnd.NextDouble();
                                    start.OfsY += -5.0 + 10.0 * rnd.NextDouble();
                                }
#pragma warning restore 162
#endif
                                // improve it
                                var better = UIElementHelper.FindBestFitForFieldOfPoints(
                                                symbol.NozzlePos, npArr, start, 0.3, 30.0, 10.0, 10, 3);
                                if (better != null)
                                    start = better;

                                // draw it
                                UIElementHelper.ApplyMultiLabel(contentObject,
                                    new[] {
                                        new Tuple<string, string>("%TAG%", "" + vo.Name)
                                    });


                                if (obj.Name == "P001")
                                {
                                }

                                var shape = ConstructDirectVO(contentObject, 1.0 * start.Scale, 1.0 * start.Rot,
                                                contentCOG.Value);
                                shape.Width *= start.Scale;
                                shape.Height *= start.Scale;

                                var sr = new Rect(
                                    start.OfsX - shape.Width / 2,
                                    start.OfsY - shape.Height / 2,
                                    shape.Width,
                                    shape.Height);

                                if (drawBoundingBoxes)
                                    DrawToCanvasAtPositionSize(canvas, sr.X, sr.Y, sr.Width, sr.Height,
                                        ConstructRect(Brushes.Blue, 1.0));

                                // Correct position for drawing the shape for some IRRATIONAL offset and the delta 
                                // between shape's mid and the COG of the nozzles

                                sr.Location = new Point(
                                    sr.X - 2.0 - contentCogToMid.X * start.Scale,
                                    sr.Y - 2.0 - contentCogToMid.Y * start.Scale);

                                // for debugging?
                                //// DrawHandlePoint(canvas, sr.X, sr.Y, drawHandlePoints);

                                // draw
                                shape.Tag = vo;
                                shape.BorderThickness = new Thickness(2);
                                shape.BorderBrush = Brushes.Orange;
                                DrawToCanvasAtPositionSize(canvas, sr.X, sr.Y, sr.Width, sr.Height, shape);

                                // register in dynInstance?
                                if (vo.dynInstance != null)
                                {
                                    vo.dynInstance.SymbolElement = shape;
                                    vo.dynInstance.RedrawSymbol();
                                }

                                // draw the label at mid of bounding box
                                var labeltb = UIElementHelper.CreateStickyLabel(this.LabelFontSettings, "" + vo.Name);
                                labeltb.Tag = vo;
                                UIElementHelper.DrawToCanvasAtPositionAligned(canvas,
                                    vo.x.Value + vo.width.Value / 2,
                                    vo.y.Value + vo.height.Value / 2,
                                    UIElementHelper.TranslateRotToAlignemnt(start.Rot), labeltb);

                            }
                            else
                            if (vo.visObj.Placement == MtpSymbol.SymbolPlaceType.StretchToBoundingBox)
                            {
                                // make bounding box Rect
                                if (drawBoundingBoxes)
                                    DrawToCanvasAtPositionSize(canvas,
                                        vo.x.Value, vo.y.Value,
                                        vo.width.Value, vo.height.Value, ConstructRect(Brushes.Blue, 1.0));

                                // draw it
                                UIElementHelper.ApplyMultiLabel(contentObject, new[] {
                                new Tuple<string, string>("%TAG%", "" + vo.Name)
                            });
                                var vb = ConstructViewboxVO(contentObject, vo.rotation.Value);
                                vb.Tag = vo;
                                DrawToCanvasAtPositionSize(canvas,
                                    vo.x.Value, vo.y.Value,
                                    vo.width.Value, vo.height.Value, vb);
                            }
                            else
                            {
                                // right now, impossible!
                            }
                        }
                        else
                        {
                            // make missing part Rect
                            if (drawBoundingBoxes)
                                DrawToCanvasAtPositionSize(canvas,
                                    vo.x.Value, vo.y.Value,
                                    vo.width.Value, vo.height.Value, ConstructRect(Brushes.Red, 2.0));
                        }
                    }

                    // handle in the mid
                    DrawHandlePoint(canvas,
                        vo.x.Value + vo.width.Value / 2, vo.y.Value + vo.height.Value / 2,
                        drawHandlePoints);
                }

                //
                // Topology Object
                //
                if (obj is MtpData.MtpTopologyObject to)
                {
                    // draw source / sink?
                    if (to.ObjClass >= 301 && to.ObjClass <= 302 && to.x != null && to.y != null)
                    {
                        // get visual object
                        var contentObject = ConstructContentObject(to.visObj);
                        UIElementHelper.ApplyMultiLabel(contentObject, new[] {
                                new Tuple<string, string>("%TAG%", "" + to.Name)
                            });

                        if (to.visObj != null && contentObject != null)
                        {
                            // determine XY
                            // Note: still not knowing, if to use nozzle or measurement
                            Nullable<Point> targetPos = null;
                            if (to.nozzlePoints != null && to.nozzlePoints.Count > 0
                                && to.nozzlePoints[0].X > 0.001 && to.nozzlePoints[0].Y > 0.001)
                                targetPos = to.nozzlePoints[0];
                            if (to.measurementPoints != null && to.measurementPoints.Count > 0
                                && to.measurementPoints[0].X > 0.001 && to.measurementPoints[0].Y > 0.001)
                                targetPos = to.measurementPoints[0];
                            if (targetPos == null)
                                targetPos = new Point(to.x.Value, to.y.Value);

                            // draw nozzle based
                            if (to.visObj.Placement == MtpSymbol.SymbolPlaceType.FitNozzles
                                && to.visObj.Symbol?.NozzlePos != null && to.visObj.Symbol?.NozzlePos.Length > 0)
                            {
                                // draw centered to nozzle pos in fixed size
                                var vb = ConstructViewboxVO(contentObject, rotation: 0.0);
                                vb.Height = 40;
                                vb.Width = 40;
                                vb.Stretch = Stretch.UniformToFill;
                                vb.Tag = to;
                                UIElementHelper.DrawToCanvasAtPositionAligned(canvas,
                                    targetPos.Value.X, targetPos.Value.Y,
                                    UIElementHelper.DrawToCanvasAlignment.Centered, vb);

                                // make bounding box Rect
                                if (drawBoundingBoxes)
                                    DrawToCanvasAtPositionSize(canvas,
                                        targetPos.Value.X - vb.Width / 2, targetPos.Value.Y - vb.Height / 2,
                                        vb.Width, vb.Height, ConstructRect(Brushes.Blue, 1.0));

                                // draw a nice label
                                var labelPos = UIElementHelper.RescalePointsByRatioOfFEs(contentObject,
                                                    vb, to.visObj.Symbol?.LabelPos);

                                // draw the label
                                var pos = new Point(targetPos.Value.X, targetPos.Value.Y);
                                var li = (int)to.visObj.LabelAlignment;
                                if (li >= 0 && labelPos != null && li < labelPos.Length)
                                    pos = pos + (Vector)labelPos[li];
                                var labeltb = UIElementHelper.CreateStickyLabel(this.LabelFontSettings, "" + to.Name);
                                labeltb.Tag = to;
                                UIElementHelper.DrawToCanvasAtPositionAligned(canvas,
                                    pos.X, pos.Y,
                                    to.visObj.LabelAlignment, labeltb);
                            }
                            else
                            {
                                // draw square-sized symbol in fixed size over (x/y)
                                var size = 50;

                                // make bounding box Rect
                                if (drawBoundingBoxes)
                                    DrawToCanvasAtPositionSize(canvas,
                                        // ReSharper disable PossibleLossOfFraction
                                        to.x.Value - size / 2, to.y.Value - size / 2,
                                        size, size, ConstructRect(Brushes.DarkOrange, 1.0));

                                // all helpers are NULL-invariant
                                var vb = ConstructViewboxVO(contentObject, rotation: 0.0);
                                vb.Tag = to;
                                DrawToCanvasAtPositionSize(canvas,
                                    to.x.Value - size / 2, to.y.Value - size / 2,
                                    size, size, vb);
                            }

                            // handle
                            DrawHandlePoint(canvas, targetPos.Value.X, targetPos.Value.Y, drawHandlePoints);
                        }
                    }
                }
            }
        }

        private MtpData.MtpPicture activePicture = null;

        public void SetPicture(MtpData.MtpPicture picture)
        {
            this.activePicture = picture;
        }

        public void RedrawMtp()
        {
            // check
            if (this.activePicture == null)
                return;

            // draw & collect click objects
            this.DrawMtpPictureIntoCanvas(canvasVisu, this.activePicture,
                drawHandlePoints: checkboxDrawHP.IsChecked == true,
                drawBoundingBoxes: checkboxDrawBB.IsChecked == true);
        }

        //
        // Callback handling
        //

        private double zoomFactor = 1.0;

        public void ApplyCanvasZoom(double newZoom)
        {
            if (canvasVisu == null)
                return;
            this.zoomFactor = newZoom;
            ScaleTransform scale = new ScaleTransform(this.zoomFactor, this.zoomFactor);
            canvasVisu.LayoutTransform = scale;
        }

        public void ZoomToFitCanvas()
        {
            if (canvasVisu == null || gridOuter == null)
                return;
            try
            {
                var ratioX = canvasVisu.Width / (gridOuter.ActualWidth - 15);
                var ratioY = canvasVisu.Height / (gridOuter.ActualHeight - 2 * 15);
                var ratio = Math.Max(ratioX, ratioY);
                if (ratio > 0.05)
                    ApplyCanvasZoom(1.0f / ratio);
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                ApplyCanvasZoom(1.0f);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == buttonZoomIn)
            {
                ApplyCanvasZoom(this.zoomFactor * 1.15f);
            }
            if (sender == buttonZoomOut)
            {
                ApplyCanvasZoom(this.zoomFactor * 0.9f);
            }
            if (sender == buttonZoomFit)
            {
                ZoomToFitCanvas();
            }
            if (sender == checkboxDrawBB || sender == checkboxDrawHP)
            {
                RedrawMtp();
            }
        }

        // doubts? see: https://stackoverflow.com/questions/5000228/how-can-you-get-the-parent-of-a-uielement

        private object FindTagOfFrameworkElementParents(FrameworkElement fe)
        {
            if (fe.Tag != null)
                return fe.Tag;
            if (fe.Parent is FrameworkElement fep)
                return FindTagOfFrameworkElementParents(fep);
            return null;
        }

        private void CanvasVisu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2 && e.Source is FrameworkElement fe)
            {
                ;

                // fe could be a fine-detail FrameworkElement; check if some father hat a Tag
                var tag = FindTagOfFrameworkElementParents(fe);
                if (tag is MtpData.MtpBaseObject mbo)
                {
                    MtpObjectDoubleClick?.Invoke(mbo);
                }
            }
        }

        //
        // Timer
        //

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (this.activePicture?.Objects != null)
                foreach (var mo in this.activePicture.Objects)
                    if (mo is MtpVisualObject vo && vo.dynInstance != null)
                        vo.dynInstance.Tick(this.VisuOptions);
        }
    }
}
