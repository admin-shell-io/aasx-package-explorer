/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AasxWpfControlLibrary;
using AdminShellNS;
using Newtonsoft.Json;

namespace AnyUi
{
    public class AnyUiDisplayDataWpf : AnyUiDisplayDataBase
    {
        [JsonIgnore]
        public AnyUiDisplayContextWpf Context;

        [JsonIgnore]
        public UIElement WpfElement;

        [JsonIgnore]
        public bool EventsAdded;

        public AnyUiDisplayDataWpf(AnyUiDisplayContextWpf Context)
        {
            this.Context = Context;
        }

        /// <summary>
        /// Initiates a drop operation with one ore more files given by filenames.
        /// </summary>
        public override void DoDragDropFiles(AnyUiUIElement elem, string[] files)
        {
            // access 
            if (files == null || files.Length < 1)
                return;
            var sc = new System.Collections.Specialized.StringCollection();
            sc.AddRange(files);

            // WPF element
            var dd = elem?.DisplayData as AnyUiDisplayDataWpf;

            // start
            DataObject data = new DataObject();
            data.SetFileDropList(sc);

            // Inititate the drag-and-drop operation.
            DragDrop.DoDragDrop(dd?.WpfElement, data, DragDropEffects.Copy | DragDropEffects.Move);
        }

    }

    public class AnyUiDisplayContextWpf : AnyUiContextBase
    {
        [JsonIgnore]
        public IFlyoutProvider FlyoutProvider;
        [JsonIgnore]
        public PackageCentral Packages;

        public static string SessionSingletonWpf = "session-wpf";

        public AnyUiDisplayContextWpf(
            IFlyoutProvider flyoutProvider, PackageCentral packages)
        {
            FlyoutProvider = flyoutProvider;
            Packages = packages;
            InitRenderRecs();
        }

        public static Color GetWpfColor(AnyUiColor c)
        {
            if (c == null)
                return Colors.Transparent;
            return Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        public static SolidColorBrush GetWpfBrush(AnyUiColor c)
        {
            if (c == null)
                return Brushes.Transparent;
            return new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
        }

        public static SolidColorBrush GetWpfBrush(AnyUiBrush br)
        {
            if (br == null)
                return Brushes.Transparent;
            var c = br.Color;
            return new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
        }

        public static AnyUiColor GetAnyUiColor(Color c)
        {
            return AnyUiColor.FromArgb(c.A, c.R, c.G, c.B);
        }

        public static AnyUiColor GetAnyUiColor(SolidColorBrush br)
        {
            if (br == null)
                return AnyUiColors.Default;
            return GetAnyUiColor(br.Color);
        }

        public static AnyUiBrush GetAnyUiBrush(SolidColorBrush br)
        {
            if (br == null)
                return AnyUiBrushes.Transparent;
            return new AnyUiBrush(GetAnyUiColor(br.Color));
        }

        public GridLength GetWpfGridLength(AnyUiGridLength gl)
        {
            if (gl == null)
                return GridLength.Auto;
            return new GridLength(gl.Value, (GridUnitType)((int)gl.Type));
        }

        public ColumnDefinition GetWpfColumnDefinition(AnyUiColumnDefinition cd)
        {
            var res = new ColumnDefinition();
            if (cd?.Width != null)
                res.Width = GetWpfGridLength(cd.Width);
            if (cd?.MinWidth.HasValue == true)
                res.MinWidth = cd.MinWidth.Value;
            if (cd?.MaxWidth.HasValue == true)
                res.MaxWidth = cd.MaxWidth.Value;
            return res;
        }

        public RowDefinition GetWpfRowDefinition(AnyUiRowDefinition rd)
        {
            var res = new RowDefinition();
            if (rd?.Height != null)
                res.Height = GetWpfGridLength(rd.Height);
            if (rd?.MinHeight.HasValue == true)
                res.MinHeight = rd.MinHeight.Value;
            return res;
        }

        public Thickness GetWpfTickness(AnyUiThickness tn)
        {
            if (tn == null)
                return new Thickness(0);
            return new Thickness(tn.Left, tn.Top, tn.Right, tn.Bottom);
        }

        public FontWeight GetFontWeight(AnyUiFontWeight wt)
        {
            var res = FontWeights.Normal;
            if (wt == AnyUiFontWeight.Bold)
                res = FontWeights.Bold;
            return res;
        }

        public Point GetWpfPoint(AnyUiPoint p)
        {
            return new Point(p.X, p.Y);
        }

        public AnyUiPoint GetAnyUiPoint(Point p)
        {
            return new AnyUiPoint(p.X, p.Y);
        }

        //
        // Handling of outside actions
        //

        [JsonIgnore]
        public List<AnyUiLambdaActionBase> WishForOutsideAction = new List<AnyUiLambdaActionBase>();

        /// <summary>
        /// This function is called from multiple places inside this class to emit an labda action
        /// to the superior logic of the application
        /// </summary>
        /// <param name="action"></param>
        public override void EmitOutsideAction(AnyUiLambdaActionBase action)
        {
            if (action == null)
                return;
            WishForOutsideAction.Add(action);
        }

        //
        // Render records: mapping AnyUi-Widgets to WPF widgets
        //

        private class RenderRec
        {
            public Type CntlType;
            public Type WpfType;
            [JsonIgnore]
            public Action<AnyUiUIElement, UIElement, AnyUiRenderMode> InitLambda;
            [JsonIgnore]
            public Action<AnyUiUIElement, UIElement, bool> HighlightLambda;

            public RenderRec(Type cntlType, Type wpfType,
                Action<AnyUiUIElement, UIElement, AnyUiRenderMode> initLambda = null,
                Action<AnyUiUIElement, UIElement, bool> highlightLambda = null)
            {
                CntlType = cntlType;
                WpfType = wpfType;
                InitLambda = initLambda;
                HighlightLambda = highlightLambda;
            }
        }

        private class ListOfRenderRec : List<RenderRec>
        {
            public RenderRec FindAnyUiCntl(Type searchType)
            {
                foreach (var rr in this)
                    if (rr?.CntlType == searchType)
                        return rr;
                return null;
            }
        }

        [JsonIgnore]
        private ListOfRenderRec RenderRecs = new ListOfRenderRec();

        private string FilterForBadText(string input)
        {
            var res = new StringBuilder();
            foreach (var c in input)
                if (c >= ' ')
                    res.Append(c);
            return res.ToString();

        }

        [JsonIgnore]
        private Point _dragStartPoint = new Point(0, 0);

        private void InitRenderRecs()
        {
            RenderRecs.Clear();
            RenderRecs.AddRange(new[]
            {
                new RenderRec(typeof(AnyUiUIElement), typeof(UIElement), (a, b, mode) =>
                {
                    // ReSharper disable UnusedVariable
                    if (a is AnyUiUIElement cntl && b is UIElement wpf
                        && mode == AnyUiRenderMode.All)
                    {
                    }
                    // ReSharper enable UnusedVariable
                }),

                new RenderRec(typeof(AnyUiFrameworkElement), typeof(FrameworkElement), (a, b, mode) =>
                {
                    if (a is AnyUiFrameworkElement cntl && b is FrameworkElement wpf
                        && mode == AnyUiRenderMode.All)
                    {
                        if (cntl.Margin != null)
                            wpf.Margin = GetWpfTickness(cntl.Margin);
                        if (cntl.VerticalAlignment.HasValue)
                            wpf.VerticalAlignment = (VerticalAlignment)((int)cntl.VerticalAlignment.Value);
                        if (cntl.HorizontalAlignment.HasValue)
                            wpf.HorizontalAlignment = (HorizontalAlignment)((int) cntl.HorizontalAlignment.Value);
                        if (cntl.MinHeight.HasValue)
                            wpf.MinHeight = cntl.MinHeight.Value;
                        if (cntl.MinWidth.HasValue)
                            wpf.MinWidth = cntl.MinWidth.Value;
                        if (cntl.MaxHeight.HasValue)
                            wpf.MaxHeight = cntl.MaxHeight.Value;
                        if (cntl.MaxWidth.HasValue)
                            wpf.MaxWidth = cntl.MaxWidth.Value;
                        wpf.Tag = cntl.Tag;

                        if (cntl.DisplayData is AnyUiDisplayDataWpf ddwpf
                            && ddwpf.EventsAdded == false)
                        {
                            // add events only once!
                            ddwpf.EventsAdded = true;

                            if ( ((cntl.EmitEvent & AnyUiEventMask.LeftDown) > 0)
                                || ((cntl.EmitEvent & AnyUiEventMask.LeftDouble) > 0) )
                            {
                                wpf.MouseLeftButtonDown += (s5, e5) => {
                                    if (e5.LeftButton == MouseButtonState.Pressed)
                                    {
                                        // get the current coordinates relative to the framework element
                                        // (only this could be sensible information to an any ui business logic)
                                        var p = GetAnyUiPoint(Mouse.GetPosition(wpf));

                                        // detect and send appropriate event and emit return
                                        if ((cntl.EmitEvent & AnyUiEventMask.LeftDown) > 0)
                                        {
                                            EmitOutsideAction(cntl.setValueLambda?.Invoke(
                                                new AnyUiEventData(AnyUiEventMask.LeftDown, cntl, e5.ClickCount, p)));
                                        }

                                        if (((cntl.EmitEvent & AnyUiEventMask.LeftDouble) > 0)
                                            && e5.ClickCount == 2)
                                        {
                                            EmitOutsideAction(cntl.setValueLambda?.Invoke(
                                                new AnyUiEventData(AnyUiEventMask.LeftDown, cntl, e5.ClickCount, p)));
                                        }
                                    }
                                };
                            }

                            if ((cntl.EmitEvent & AnyUiEventMask.DragStart) > 0)
                            {
                                wpf.MouseLeftButtonDown +=(s6, e6) =>
                                {
                                    _dragStartPoint = e6.GetPosition(null);
                                };

                                wpf.PreviewMouseMove += (s7, e7) =>
                                {
                                    if (e7.LeftButton == MouseButtonState.Pressed)
                                    {
                                        Point position = e7.GetPosition(null);
                                        if (Math.Abs(position.X - _dragStartPoint.X)
                                                > SystemParameters.MinimumHorizontalDragDistance
                                            || Math.Abs(position.Y - _dragStartPoint.Y)
                                                > SystemParameters.MinimumVerticalDragDistance)
                                        {
                                            cntl.setValueLambda?.Invoke(
                                                new AnyUiEventData(AnyUiEventMask.DragStart, cntl));
                                        }
                                    }
                                };
                            }
                        }
                    }
                }),

                new RenderRec(typeof(AnyUiControl), typeof(Control), (a, b, mode) =>
                {
                   if (a is AnyUiControl cntl && b is Control wpf
                       && mode == AnyUiRenderMode.All)
                   {
                       if (cntl.VerticalContentAlignment.HasValue)
                           wpf.VerticalContentAlignment =
                            (VerticalAlignment)((int) cntl.VerticalContentAlignment.Value);
                       if (cntl.HorizontalContentAlignment.HasValue)
                           wpf.HorizontalContentAlignment =
                            (HorizontalAlignment)((int) cntl.HorizontalContentAlignment.Value);

                       if (cntl.FontSize.HasValue)
                            wpf.FontSize = SystemFonts.MessageFontSize * cntl.FontSize.Value;
                       if (cntl.FontWeight.HasValue)
                            wpf.FontWeight = GetFontWeight(cntl.FontWeight.Value);
                   }
                }),

                new RenderRec(typeof(AnyUiContentControl), typeof(ContentControl), (a, b, mode) =>
                {
                   if (a is AnyUiContentControl && b is ContentControl
                       && mode == AnyUiRenderMode.All)
                   {
                   }
                }),

                new RenderRec(typeof(AnyUiDecorator), typeof(Decorator), (a, b, mode) =>
                {
                    if (a is AnyUiDecorator cntl && b is Decorator wpf
                        && mode == AnyUiRenderMode.All)
                    {
                        // child
                        wpf.Child = GetOrCreateWpfElement(cntl.Child, allowReUse: false);
                    }
                }),

                new RenderRec(typeof(AnyUiViewbox), typeof(Viewbox), (a, b, mode) =>
                {
                   if (a is AnyUiViewbox cntl && b is Viewbox wpf
                       && mode == AnyUiRenderMode.All)
                   {
                        wpf.Stretch = (Stretch)(int) cntl.Stretch;
                   }
                }),

                new RenderRec(typeof(AnyUiPanel), typeof(Panel), (a, b, mode) =>
                {
                   if (a is AnyUiPanel cntl && b is Panel wpf)
                   {
                        // figure out, when to redraw
                        var redraw = (mode == AnyUiRenderMode.All)
                            || (mode == AnyUiRenderMode.StatusToUi && a is AnyUiCanvas);

                        if (redraw)
                        {
                            // normal members
                            if (cntl.Background != null)
                                wpf.Background = GetWpfBrush(cntl.Background);

                            // children
                            wpf.Children.Clear();
                            if (cntl.Children != null)
                                foreach (var ce in cntl.Children)
                                {
                                    wpf.Children.Add(GetOrCreateWpfElement(ce, allowReUse: false));
                                }
                        }
                   }
                }),

                new RenderRec(typeof(AnyUiGrid), typeof(Grid), (a, b, mode) =>
                {
                   if (a is AnyUiGrid cntl && b is Grid wpf
                       && mode == AnyUiRenderMode.All)
                   {
                       if (cntl.RowDefinitions != null)
                           foreach (var rd in cntl.RowDefinitions)
                               wpf.RowDefinitions.Add(GetWpfRowDefinition(rd));

                       if (cntl.ColumnDefinitions != null)
                           foreach (var cd in cntl.ColumnDefinitions)
                               wpf.ColumnDefinitions.Add(GetWpfColumnDefinition(cd));

                       // make sure to target only already realized children
                       foreach (var cel in cntl.Children)
                       {
                           var celwpf = GetOrCreateWpfElement(cel, allowCreate: false);
                           if (wpf.Children.Contains(celwpf))
                           {
                               if (cel.GridRow.HasValue)
                                   Grid.SetRow(celwpf, cel.GridRow.Value);
                               if (cel.GridRowSpan.HasValue)
                                   Grid.SetRowSpan(celwpf, cel.GridRowSpan.Value);
                               if (cel.GridColumn.HasValue)
                                   Grid.SetColumn(celwpf, cel.GridColumn.Value);
                               if (cel.GridColumnSpan.HasValue)
                                   Grid.SetColumnSpan(celwpf, cel.GridColumnSpan.Value);
                           }
                       }
                   }
                }),

                new RenderRec(typeof(AnyUiStackPanel), typeof(StackPanel), (a, b, mode) =>
                {
                   if (a is AnyUiStackPanel cntl && b is StackPanel wpf
                       && mode == AnyUiRenderMode.All)
                   {
                       if (cntl.Orientation.HasValue)
                           wpf.Orientation = (Orientation)((int) cntl.Orientation.Value);
                   }
                }),

                new RenderRec(typeof(AnyUiWrapPanel), typeof(WrapPanel), (a, b, mode) =>
                {
                   if (a is AnyUiWrapPanel cntl && b is WrapPanel wpf
                       && mode == AnyUiRenderMode.All)
                   {
                       if (cntl.Orientation.HasValue)
                           wpf.Orientation = (Orientation)((int) cntl.Orientation.Value);
                   }
                }),

                new RenderRec(typeof(AnyUiShape), typeof(Shape), (a, b, mode) =>
                {
                    if (a is AnyUiShape cntl && b is Shape wpf
                        && mode == AnyUiRenderMode.All)
                    {
                        if (cntl.Fill != null)
                            wpf.Fill = GetWpfBrush(cntl.Fill);
                        if (cntl.Stroke != null)
                            wpf.Stroke = GetWpfBrush(cntl.Stroke);
                        if (cntl.StrokeThickness.HasValue)
                            wpf.StrokeThickness = cntl.StrokeThickness.Value;
                        wpf.Tag = cntl.Tag;
                    }
                }),

                new RenderRec(typeof(AnyUiRectangle), typeof(Rectangle), (a, b, mode) =>
                {
                    // ReSharper disable UnusedVariable
                    if (a is AnyUiRectangle cntl && b is Rectangle wpf
                        && mode == AnyUiRenderMode.All)
                    {
                    }
                    // ReSharper enable UnusedVariable
                }),

                new RenderRec(typeof(AnyUiEllipse), typeof(Ellipse), (a, b, mode) =>
                {
                    // ReSharper disable UnusedVariable
                    if (a is AnyUiEllipse cntl && b is Ellipse wpf
                        && mode == AnyUiRenderMode.All)
                    {
                    }
                    // ReSharper enable UnusedVariable
                }),

                new RenderRec(typeof(AnyUiPolygon), typeof(Polygon), (a, b, mode) =>
                {
                    if (a is AnyUiPolygon cntl && b is Polygon wpf
                        && (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi))
                    {
                        // points
                        if (cntl.Points != null)
                            foreach (var p in cntl.Points)
                                wpf.Points.Add(GetWpfPoint(p));
                    }
                }),

                new RenderRec(typeof(AnyUiCanvas), typeof(Canvas), (a, b, mode) =>
                {
                   if (a is AnyUiCanvas cntl && b is Canvas wpf)
                   {
                        // Children are added but deserve some post processing
                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {
                            if (cntl.Children.Count == wpf.Children.Count)
                                for (int i=0; i < cntl.Children.Count; i++)
                                {
                                    var cc = cntl.Children[i] as AnyUiFrameworkElement;
                                    var cw = wpf.Children[i] as FrameworkElement;
                                    if (cc != null && cw != null)
                                    {
                                        cw.Width = cc.Width;
                                        cw.Height = cc.Height;
                                        Canvas.SetLeft(cw, cc.X);
                                        Canvas.SetTop(cw, cc.Y);
                                    }
                                }
                        }

                        // need to subscribe for events?
                        if (mode == AnyUiRenderMode.All)
                        {
                            if ((cntl.EmitEvent & AnyUiEventMask.MouseAll) > 0)
                            {
                                wpf.MouseDown += (s,e) =>
                                {
                                    // get the current coordinates relative to the framework element
                                    // (onlythis could be sensible information to an any ui business logic)
                                    var p = GetAnyUiPoint(Mouse.GetPosition(wpf));

                                    // try to find AnyUI element emitting the event
                                    object auiSource = null;
                                    foreach (var ch in cntl.Children)
                                        if ((ch?.DisplayData as AnyUiDisplayDataWpf)?.WpfElement == e.Source)
                                            auiSource = ch;
                                
                                    // send event and emit return
                                    if (e.ChangedButton == MouseButton.Left)
                                    {
                                        EmitOutsideAction(
                                            cntl.setValueLambda?.Invoke(new AnyUiEventData(
                                                    AnyUiEventMask.LeftDown, auiSource, e.ClickCount, p)));
                                    }
                                };
                            }
                        }
                   }
                }),

                new RenderRec(typeof(AnyUiScrollViewer), typeof(ScrollViewer), (a, b, mode) =>
                {
                   if (a is AnyUiScrollViewer cntl && b is ScrollViewer wpf
                       && mode == AnyUiRenderMode.All)
                   {
                        // attributes
                        if (cntl.HorizontalScrollBarVisibility.HasValue)
                            wpf.HorizontalScrollBarVisibility =
                                (ScrollBarVisibility)((int) cntl.HorizontalScrollBarVisibility.Value);
                        if (cntl.VerticalScrollBarVisibility.HasValue)
                            wpf.VerticalScrollBarVisibility =
                                (ScrollBarVisibility)((int) cntl.VerticalScrollBarVisibility.Value);

                        // initial position (before attaching callback)
                        if (cntl.InitialScrollPosition.HasValue)
                        {
                            wpf.ScrollToVerticalOffset(cntl.InitialScrollPosition.Value);
                        }

                        // callbacks
                        wpf.ScrollChanged += (object sender, ScrollChangedEventArgs e) =>
                        {
                            cntl.setValueLambda?.Invoke(
                                new Tuple<double, double>(e.HorizontalOffset, e.VerticalOffset));
                        };
                   }
                }),

                new RenderRec(typeof(AnyUiBorder), typeof(Border), (a, b, mode) =>
                {
                    if (a is AnyUiBorder cntl && b is Border wpf
                        && mode == AnyUiRenderMode.All)
                    {
                        // members
                        if (cntl.Background != null)
                            wpf.Background = GetWpfBrush(cntl.Background);
                        if (cntl.BorderThickness != null)
                            wpf.BorderThickness = GetWpfTickness(cntl.BorderThickness);
                        if (cntl.BorderBrush != null)
                            wpf.BorderBrush = GetWpfBrush(cntl.BorderBrush);
                        if (cntl.Padding != null)
                            wpf.Padding = GetWpfTickness(cntl.Padding);
                        if (cntl.CornerRadius != null)
                            wpf.CornerRadius  = new CornerRadius(cntl.CornerRadius.Value);
                        
                        // callbacks
                        if (cntl.IsDropBox)
                        {
                            wpf.AllowDrop = true;
                            wpf.DragEnter += (object sender2, DragEventArgs e2) =>
                            {
                                e2.Effects = DragDropEffects.Copy;
                            };
                            wpf.PreviewDragOver += (object sender3, DragEventArgs e3) =>
                            {
                                e3.Handled = true;
                            };
                            wpf.Drop += (object sender4, DragEventArgs e4) =>
                            {
                                if (e4.Data.GetDataPresent(DataFormats.FileDrop, true))
                                {
                                    // Note that you can have more than one file.
                                    string[] files = (string[])e4.Data.GetData(DataFormats.FileDrop);

                                    // Assuming you have one file that you care about, pass it off to whatever
                                    // handling code you have defined.
                                    if (files != null && files.Length > 0
                                        && sender4 is FrameworkElement)
                                    {
                                        // update UI
                                        if (wpf.Child is TextBlock tb2)
                                            tb2.Text = "" + files[0];

                                        // value changed
                                        var action = cntl.setValueLambda?.Invoke(files[0]);
                                        if (action != null && !(action is AnyUiLambdaActionNone))
                                            EmitOutsideAction(action);

                                        // contents changed
                                        WishForOutsideAction.Add(new AnyUiLambdaActionContentsChanged());
                                    }
                                }

                                e4.Handled = true;
                            };
                        }
                    }
                }),

                new RenderRec(typeof(AnyUiLabel), typeof(Label), (a, b, mode) =>
                {
                   if (a is AnyUiLabel cntl && b is Label wpf
                       && mode == AnyUiRenderMode.All)
                   {
                       if (cntl.Background != null)
                           wpf.Background = GetWpfBrush(cntl.Background);
                       if (cntl.Foreground != null)
                           wpf.Foreground = GetWpfBrush(cntl.Foreground);
                       if (cntl.FontWeight.HasValue)
                           wpf.FontWeight = GetFontWeight(cntl.FontWeight.Value);
                       if (cntl.Padding != null)
                           wpf.Padding = GetWpfTickness(cntl.Padding);
                       wpf.Content = cntl.Content;
                   }
                }),

                new RenderRec(typeof(AnyUiTextBlock), typeof(TextBlock), (a, b, mode) =>
                {
                   if (a is AnyUiTextBlock cntl && b is TextBlock wpf)
                   {
                        if (mode == AnyUiRenderMode.All)
                        {
                            if (cntl.Background != null)
                                wpf.Background = GetWpfBrush(cntl.Background);
                            if (cntl.Foreground != null)
                                wpf.Foreground = GetWpfBrush(cntl.Foreground);
                            if (cntl.FontWeight.HasValue)
                                wpf.FontWeight = GetFontWeight(cntl.FontWeight.Value);
                            if (cntl.Padding != null)
                                wpf.Padding = GetWpfTickness(cntl.Padding);
                            if (cntl.TextWrapping.HasValue)
                                wpf.TextWrapping = (TextWrapping)((int) cntl.TextWrapping.Value);

                            if (cntl.FontSize.HasValue)
                                wpf.FontSize = SystemFonts.MessageFontSize * cntl.FontSize.Value;
                            if (cntl.FontWeight.HasValue)
                                wpf.FontWeight = GetFontWeight(cntl.FontWeight.Value);
                        }

                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {
                            wpf.Text = cntl.Text;
                        }
                   }
                }),

                new RenderRec(typeof(AnyUiSelectableTextBlock), typeof(SelectableTextBlock), (a, b, mode) =>
                {
                   if (a is AnyUiSelectableTextBlock cntl && b is SelectableTextBlock wpf
                       &&
                       (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi))
                   {
                        if (cntl.TextAsHyperlink)
                        {
                            var hl = new System.Windows.Documents.Hyperlink()
                            {
                                NavigateUri = new Uri(cntl.Text),
                            };
                            hl.Inlines.Add(cntl.Text);
                            hl.RequestNavigate += (sender, e) =>
                            {
                                // normal procedure
                                var action = cntl.setValueLambda?.Invoke(cntl);
                                EmitOutsideAction(action);
                            };
                            wpf.Inlines.Clear();
                            wpf.Inlines.Add(hl);
                        }
                        else
                        {
                            wpf.Text = cntl.Text;
                        }
                   }
                }),

                new RenderRec(typeof(AnyUiHintBubble), typeof(HintBubble), (a, b, mode) =>
                {
                   if (a is AnyUiHintBubble cntl && b is HintBubble wpf
                       && mode == AnyUiRenderMode.All)
                   {
                       if (cntl.Background != null)
                           wpf.Background = GetWpfBrush(cntl.Background);
                       if (cntl.Foreground != null)
                           wpf.Foreground = GetWpfBrush(cntl.Foreground);
                       if (cntl.Padding != null)
                           wpf.Padding = GetWpfTickness(cntl.Padding);
                       wpf.Text = cntl.Text;
                   }
                }),

                new RenderRec(typeof(AnyUiImage), typeof(Image), (a, b, mode) =>
                {
                   if (a is AnyUiImage cntl && b is Image wpf)
                   {
                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {
                            BitmapSource sourceBi = null;
                            if (cntl.BitmapInfo?.ImageSource is BitmapSource bs)
                               sourceBi = bs;
                            else if (cntl.BitmapInfo?.PngData != null)
                            {
                                using (MemoryStream memory = new MemoryStream())
                                {
                                    memory.Write(cntl.BitmapInfo.PngData, 0, cntl.BitmapInfo.PngData.Length);
                                    memory.Position = 0;

                                    BitmapImage bi = new BitmapImage();
                                    bi.BeginInit();
                                    bi.StreamSource = memory;
                                    bi.CacheOption = BitmapCacheOption.OnLoad;
                                    bi.EndInit();

                                    sourceBi = bi;
                                }
                            }

                            // found something?
                            if (sourceBi != null)
                            {
                                // additionally convert?
                                if (cntl.BitmapInfo?.ConvertTo96dpi == true)
                                {
                                    // prepare
                                    double dpi = 96;
                                    int width = sourceBi.PixelWidth;
                                    int height = sourceBi.PixelHeight;

                                    // execute
                                    int stride = width * sourceBi.Format.BitsPerPixel;
                                    byte[] pixelData = new byte[stride * height];
                                    sourceBi.CopyPixels(pixelData, stride, 0);
                                    var destBi = BitmapSource.Create(
                                        width, height, dpi, dpi, sourceBi.Format, null, pixelData, stride);
                                    destBi.Freeze();

                                    // remember
                                    sourceBi = destBi;
                                }

                                // finally set
                                wpf.Source = sourceBi;
                            }

                            wpf.Stretch = (Stretch)(int) cntl.Stretch;
                        }
                   }
                }),

                new RenderRec(typeof(AnyUiCountryFlag), typeof(CountryFlag.CountryFlag), (a, b, mode) =>
                {
                   if (a is AnyUiCountryFlag cntl && b is CountryFlag.CountryFlag wpf
                       && mode == AnyUiRenderMode.All)
                   {
                        // need to translate two enums
                        foreach (var ev in (CountryFlag.CountryCode[])Enum.GetValues(typeof(CountryFlag.CountryCode)))
                            if (Enum.GetName(typeof(CountryFlag.CountryCode), ev)?.Trim().ToUpper() == cntl.ISO3166Code)
                                wpf.Code = ev;
                   }
                }),

                new RenderRec(typeof(AnyUiTextBox), typeof(TextBox), (a, b, mode) =>
                {
                    if (a is AnyUiTextBox cntl && b is TextBox wpf)
                    {
                        if (mode == AnyUiRenderMode.All)
                        {
                            // members  
                            if (cntl.Background != null)
                                wpf.Background = GetWpfBrush(cntl.Background);
                            if (cntl.Foreground != null)
                                wpf.Foreground = GetWpfBrush(cntl.Foreground);
                            if (cntl.Padding != null)
                                wpf.Padding = GetWpfTickness(cntl.Padding);

                            wpf.VerticalScrollBarVisibility = (ScrollBarVisibility)
                                ((int) cntl.VerticalScrollBarVisibility);
                            wpf.AcceptsReturn = cntl.AcceptsReturn;
                            if (cntl.MaxLines != null)
                                wpf.MaxLines = cntl.MaxLines.Value;
                            wpf.Text = cntl.Text;
                        
                            // callbacks
                            cntl.originalValue = "" + cntl.Text;
                            wpf.TextChanged += (sender, e) => {
                                cntl.setValueLambda?.Invoke(wpf.Text);
                                WishForOutsideAction.Add(new AnyUiLambdaActionContentsChanged());
                            };
                            wpf.KeyUp += (sender, e) =>
                            {
                                if (e.Key == Key.Enter)
                                {
                                    e.Handled = true;
                                    EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                                    EmitOutsideAction(cntl.takeOverLambda);
                                }
                            };
                        }

                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {
                            wpf.Text = cntl.Text;
                        }
                    }
                }, highlightLambda: (a,b,highlighted) => {
                    if (a is AnyUiTextBox && b is TextBox tb)
                    {
                        if (highlighted)
                        {
                            tb.BorderBrush = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                            tb.BorderThickness = new Thickness(3);
                            tb.Focus();
                            tb.SelectAll();
                        }
                        else
                        {
                            tb.BorderBrush = SystemColors.ControlDarkBrush;
                            tb.BorderThickness = new Thickness(1);
                        }
                    }
                }),

                new RenderRec(typeof(AnyUiComboBox), typeof(ComboBox), (a, b, mode) =>
                {
                    // members
                    if (a is AnyUiComboBox cntl && b is ComboBox wpf
                        && mode == AnyUiRenderMode.All)
                    {
                        if (cntl.Background != null)
                            wpf.Background = GetWpfBrush(cntl.Background);
                        if (cntl.Foreground != null)
                            wpf.Foreground = GetWpfBrush(cntl.Foreground);
                        if (cntl.Padding != null)
                            wpf.Padding = GetWpfTickness(cntl.Padding);
                        if (cntl.IsEditable.HasValue)
                            wpf.IsEditable = cntl.IsEditable.Value;

                        if (cntl.Items != null)
                        {
                            foreach (var i in cntl.Items)
                                wpf.Items.Add(i);
                        }

                        wpf.Text = cntl.Text;
                        if (cntl.SelectedIndex.HasValue)
                            wpf.SelectedIndex = cntl.SelectedIndex.Value;

                        // callbacks
                        cntl.originalValue = "" + cntl.Text;
                        System.Windows.Controls.TextChangedEventHandler tceh = (sender, e) => {
                            // for AAS events: only invoke, if required
                            if (cntl.Text != wpf.Text)
                                cntl.setValueLambda?.Invoke(wpf.Text);
                            cntl.Text = wpf.Text;
                        };
                        wpf.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, tceh);
                        if (!wpf.IsEditable)
                        {
                            // we need this event
                            wpf.SelectionChanged += (sender, e) => {
                                cntl.SelectedIndex = wpf.SelectedIndex;
                                cntl.Text = wpf.Text;
                                EmitOutsideAction(cntl.setValueLambda?.Invoke((string) wpf.SelectedItem));
                                EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                                // Note for MIHO: this was the dangerous outside event loop!
                                EmitOutsideAction(cntl.takeOverLambda);
                            };
                        }
                        else
                        { 
                            // if editabe, add this for comfort
                            wpf.KeyUp += (sender, e) =>
                            {
                                if (e.Key == Key.Enter)
                                {
                                    e.Handled = true;
                                    EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                                    EmitOutsideAction(cntl.takeOverLambda);
                                }
                            };
                        }
                    }
                }, highlightLambda: (a,b,highlighted) => {
                    if (a is AnyUiComboBox && b is ComboBox cb)
                    {
                        if (highlighted)
                        {
                            cb.BorderBrush = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                            cb.BorderThickness = new Thickness(3);

                            try
                            {
                                // see: https://stackoverflow.com/questions/37006596/borderbrush-to-combobox
                                // see also: https://stackoverflow.com/questions/2285491/
                                // wpf-findname-returns-null-when-it-should-not
                                cb.ApplyTemplate();
                                var cbTemp = cb.Template;
                                if (cbTemp != null)
                                {
                                    var toggleButton = cbTemp.FindName(
                                        "toggleButton", cb) as System.Windows.Controls.Primitives.ToggleButton;
                                    toggleButton?.ApplyTemplate();
                                    var tgbTemp = toggleButton?.Template;
                                    if (tgbTemp != null)
                                    {
                                        var border = tgbTemp.FindName("templateRoot", toggleButton) as Border;
                                        if (border != null)
                                            border.BorderBrush = new SolidColorBrush(
                                                Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                            }

                            cb.Focus();
                        }
                        else
                        {
                            cb.BorderBrush = SystemColors.ControlDarkBrush;
                            cb.BorderThickness = new Thickness(1);
                        }
                    }
                }),

                new RenderRec(typeof(AnyUiCheckBox), typeof(CheckBox), (a, b, mode) =>
                {
                    if (a is AnyUiCheckBox cntl && b is CheckBox wpf
                        && mode == AnyUiRenderMode.All)
                    {
                        // members
                        if (cntl.Background != null)
                            wpf.Background = GetWpfBrush(cntl.Background);
                        if (cntl.Foreground != null)
                            wpf.Foreground = GetWpfBrush(cntl.Foreground);
                        if (cntl.IsChecked.HasValue)
                            wpf.IsChecked = cntl.IsChecked.Value;
                        if (cntl.Padding != null)
                            wpf.Padding = GetWpfTickness(cntl.Padding);
                        wpf.Content = cntl.Content;
                        // callbacks
                        cntl.originalValue = cntl.IsChecked;
                        RoutedEventHandler ceh = (sender, e) =>
                        {
                            EmitOutsideAction(cntl.setValueLambda?.Invoke(wpf.IsChecked == true));
                            EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                            EmitOutsideAction(cntl.takeOverLambda);
                        };
                        wpf.Checked += ceh;
                        wpf.Unchecked += ceh;
                    }
                }),

                new RenderRec(typeof(AnyUiButton), typeof(Button), (a, b, mode) =>
                {
                    if (a is AnyUiButton cntl && b is Button wpf
                        && mode == AnyUiRenderMode.All)
                    {
                        // members
                        if (cntl.Background != null)
                            wpf.Background = GetWpfBrush(cntl.Background);
                        if (cntl.Foreground != null)
                            wpf.Foreground = GetWpfBrush(cntl.Foreground);
                        if (cntl.Padding != null)
                            wpf.Padding = GetWpfTickness(cntl.Padding);

                        wpf.Content = cntl.Content;
                        wpf.ToolTip = cntl.ToolTip;
                        // callbacks
                        wpf.Click += (sender, e) =>
                        {
                            // normal procedure
                            var action = cntl.setValueLambda?.Invoke(cntl);
                            EmitOutsideAction(action);

                            // special case
                            if (cntl.SpecialAction is AnyUiSpecialActionContextMenu cntlcm
                                && cntlcm.MenuItemHeaders != null)
                            {
                                var nmi = cntlcm.MenuItemHeaders.Length / 2;
                                var cm = new ContextMenu();
                                for (int i = 0; i < nmi; i++)
                                {
                                    // menu item itself
                                    var mi = new MenuItem();
                                    mi.Icon = "" + cntlcm.MenuItemHeaders[2 * i + 0];
                                    mi.Header = "" + cntlcm.MenuItemHeaders[2 * i + 1];
                                    mi.Tag = i;
                                    cm.Items.Add(mi);

                                    // directly attached
                                    var bufferedI = i;
                                    mi.Click += (sender2, e2) =>
                                    {
                                        var action2 = cntlcm.MenuItemLambda?.Invoke(bufferedI);
                                        EmitOutsideAction(action2);
                                    };
                                }
                                cm.PlacementTarget = wpf;
                                cm.IsOpen = true;
                            }
                        };
                    }
                })
            });
        }

        public UIElement GetOrCreateWpfElement(
            AnyUiUIElement el,
            Type superType = null,
            bool allowCreate = true,
            bool allowReUse = true,
            AnyUiRenderMode mode = AnyUiRenderMode.All)
        {
            // access
            if (el == null)
                return null;

            // have data attached to cntl
            if (el.DisplayData == null)
                el.DisplayData = new AnyUiDisplayDataWpf(this);
            var dd = el?.DisplayData as AnyUiDisplayDataWpf;
            if (dd == null)
                return null;

            // most specialized class or in recursion/ creation of base classes?
            var topClass = superType == null;

            // identify render rec
            var searchType = (superType != null) ? superType : el.GetType();
            var foundRR = RenderRecs.FindAnyUiCntl(searchType);
            if (foundRR == null || foundRR.WpfType == null)
                return null;

            // special case: update status only
            if (mode == AnyUiRenderMode.StatusToUi
                && dd.WpfElement != null && allowReUse && topClass)
            {
                // itself
                if (el.Touched)
                {
                    // perform a "minimal" render action
                    // recurse (first) in the base types ..
                    var bt2 = searchType.BaseType;
                    if (bt2 != null)
                        GetOrCreateWpfElement(el, superType: bt2, allowReUse: true,
                            mode: AnyUiRenderMode.StatusToUi);

                    foundRR.InitLambda?.Invoke(el, dd.WpfElement, AnyUiRenderMode.StatusToUi);

                }
                el.Touched = false;

                // recurse into
                if (el is AnyUi.IEnumerateChildren ien)
                    foreach (var elch in ien.GetChildren())
                        GetOrCreateWpfElement(elch, allowCreate: false, allowReUse: true,
                            mode: AnyUiRenderMode.StatusToUi);

                // return (effectively TOP element)
                return dd.WpfElement;
            }

            // special case: return, if already created and not (still) in recursion/ creation of base classes
            if (dd.WpfElement != null && allowReUse && topClass)
                return dd.WpfElement;
            if (!allowCreate)
                return null;

            // create wpfElement accordingly?
            //// Note: excluded from condition: dd.WpfElement == null
            if (topClass)
                dd.WpfElement = (UIElement)Activator.CreateInstance(foundRR.WpfType);
            if (dd.WpfElement == null)
                return null;

            // recurse (first) in the base types ..
            var bt = searchType.BaseType;
            if (bt != null)
                GetOrCreateWpfElement(el, superType: bt, allowReUse: allowReUse);

            // perform the render action (for this level of attributes, second)
            foundRR.InitLambda?.Invoke(el, dd.WpfElement, AnyUiRenderMode.All);

            // does the element need child elements?
            // do a special case handling here, unless a more generic handling is required

            {
                if (el is AnyUiScrollViewer cntl && dd.WpfElement is ScrollViewer wpf
                    && cntl.Content != null)
                {
                    wpf.Content = GetOrCreateWpfElement(cntl.Content, allowReUse: allowReUse);
                }
            }

            // does the element need child elements?
            // do a special case handling here, unless a more generic handling is required

            {
                if (el is AnyUiBorder cntl && dd.WpfElement is Border wpf
                    && cntl.Child != null)
                {
                    wpf.Child = GetOrCreateWpfElement(cntl.Child, allowReUse: allowReUse);
                }
            }

            // call action
            if (topClass)
            {
                UIElementWasRendered(el, dd.WpfElement);
            }

            // result
            return dd.WpfElement;
        }

        //
        // Tag information
        //

        protected List<AnyUiUIElement> _namedElements = new List<AnyUiUIElement>();

        public int PrepareNameList(AnyUiUIElement root)
        {
            _namedElements = new List<AnyUiUIElement>();
            if (root == null)
                return 0;
            _namedElements = root.FindAllNamed().ToList();
            return _namedElements.Count;
        }

        public AnyUiUIElement FindFirstNamedElement(string name)
        {
            if (_namedElements == null || name == null)
                return null;
            foreach (var el in _namedElements)
                if (el.Name?.Trim()?.ToLower() == name.Trim().ToLower())
                    return el;
            return null;
        }

        //
        // Shortcut handling
        //

        public class KeyShortcutRecord
        {
            public AnyUiUIElement Element;
            public System.Windows.Input.ModifierKeys Modifiers;
            public System.Windows.Input.Key Key;
            public bool Preview = true;
            public string Info;
        }

        private List<KeyShortcutRecord> _keyShortcuts = new List<KeyShortcutRecord>();

        public List<KeyShortcutRecord> KeyShortcuts { get { return _keyShortcuts; } }

        public bool RegisterKeyShortcut(
            string name,
            System.Windows.Input.ModifierKeys modifiers,
            System.Windows.Input.Key key,
            string info)
        {
            var el = FindFirstNamedElement(name);
            if (el == null)
                return false;
            _keyShortcuts.Add(new KeyShortcutRecord()
            {
                Element = el,
                Modifiers = modifiers,
                Key = key,
                Info = info
            });
            return true;
        }

        public int TriggerKeyShortcut(
            System.Windows.Input.Key key,
            System.Windows.Input.ModifierKeys modifiers,
            bool preview)
        {
            var res = 0;
            if (_keyShortcuts == null)
                return res;
            foreach (var sc in _keyShortcuts)
                if (key == sc.Key && modifiers == sc.Modifiers && preview == sc.Preview)
                {
                    // found, any lambdas appicable?
                    if (sc.Element is AnyUiButton btn)
                    {
                        var action = btn.setValueLambda?.Invoke(btn);
                        EmitOutsideAction(action);
                        res++;
                    }
                }
            return res;
        }

        //
        // Utilities
        //

        /// <summary>
        /// Graphically highlights/ marks an element to be "selected", e.g for seacg/ replace
        /// operations.
        /// </summary>
        /// <param name="el">AnyUiElement</param>
        /// <param name="highlighted">True for highlighted, set for clear state</param>
        public override void HighlightElement(AnyUiFrameworkElement el, bool highlighted)
        {
            // access 
            if (el == null)
                return;
            var dd = el?.DisplayData as AnyUiDisplayDataWpf;
            if (dd?.WpfElement == null)
                return;

            // renderRec?
            var foundRR = RenderRecs.FindAnyUiCntl(el.GetType());
            if (foundRR?.HighlightLambda == null)
                return;

            // perform the render action (for this level of attributes, second)
            foundRR.HighlightLambda.Invoke(el, dd.WpfElement, highlighted);
        }

        public void UIElementWasRendered(AnyUiUIElement AnyUi, UIElement el)
        {
        }

        /// <summary>
        /// Tries to revert changes in some controls.
        /// </summary>
        /// <returns>True, if changes were applied</returns>
        public override bool CallUndoChanges(AnyUiUIElement root)
        {
            var res = false;

            // recurse?
            if (root is AnyUiPanel panel)
                if (panel.Children != null)
                    foreach (var ch in panel.Children)
                        res = res || CallUndoChanges(ch);

            // can do something
            if (root is AnyUiTextBox cntl && cntl.DisplayData is AnyUiDisplayDataWpf dd
                && dd?.WpfElement is TextBox tb && cntl.originalValue != null)
            {
                tb.Text = cntl.originalValue as string;
                res = true;
            }

            // some changes
            return res;
        }

        /// <summary>
        /// If supported by implementation technology, will set Clipboard (copy/ paste buffer)
        /// of the main application computer.
        /// </summary>
        public override void ClipboardSet(AnyUiClipboardData cb)
        {
            if (cb == null)
                return;

            if (cb.Watermark != null)
                Clipboard.SetData("AASXPE", cb.Watermark);

            if (cb.Text != null)
                Clipboard.SetText(cb.Text);
        }

        /// <summary>
        /// If supported by implementation technology, will get Clipboard (copy/ paste buffer)
        /// of the main application computer.
        /// </summary>
        public override AnyUiClipboardData ClipboardGet()
        {
            var res = new AnyUiClipboardData();

            // get watermark?
            res.Watermark = (string)Clipboard.GetData("AASXPE");

            // get text?
            res.Text = Clipboard.GetText();

            // ok
            return res;
        }

        /// <summary>
        /// Returns the selected items in the tree, which are provided by the implementation technology
        /// (derived class of this).
        /// Note: these would be of type <c>VisualElementGeneric</c>, but is in other assembly.
        /// </summary>
        /// <returns></returns>
        public override List<IAnyUiSelectedItem> GetSelectedItems()
        {
            return null;
        }

        /// <summary>
        /// Show MessageBoxFlyout with contents
        /// </summary>
        /// <param name="message">Message on the main screen</param>
        /// <param name="caption">Caption string (title)</param>
        /// <param name="buttons">Buttons according to WPF standard messagebox</param>
        /// <param name="image">Image according to WPF standard messagebox</param>
        /// <returns></returns>
        public override AnyUiMessageBoxResult MessageBoxFlyoutShow(
            string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image)
        {
            if (FlyoutProvider == null)
                return AnyUiMessageBoxResult.Cancel;
            return FlyoutProvider.MessageBoxFlyoutShow(message, caption, buttons, image);
        }

        private UserControl DispatchFlyout(AnyUiDialogueDataBase dialogueData)
        {
            // access
            if (dialogueData == null)
                return null;
            UserControl res = null;

            // dispatch
            // TODO (MIHO, 2020-12-21): can be realized without tedious central dispatch?
            if (dialogueData is AnyUiDialogueDataEmpty ddem)
            {
                var uc = new EmptyFlyout();
                uc.DiaData = ddem;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataTextBox ddtb)
            {
                var uc = new TextBoxFlyout();
                uc.DiaData = ddtb;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataChangeElementAttributes ddcea)
            {
                var uc = new ChangeElementAttributesFlyout();
                uc.DiaData = ddcea;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataSelectEclassEntity ddec)
            {
                var uc = new SelectEclassEntityFlyout();
                uc.DiaData = ddec;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataTextEditor ddte)
            {
                var uc = new TextEditorFlyout();
                uc.DiaData = ddte;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataSelectFromList ddsl)
            {
                var uc = new SelectFromListFlyout();
                uc.DiaData = ddsl;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataSelectAasEntity ddsa)
            {
                var uc = new SelectAasEntityFlyout(Packages);
                uc.DiaData = ddsa;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataSelectReferableFromPool ddrf)
            {
                var uc = new SelectFromReferablesPoolFlyout(AasxPredefinedConcepts.DefinitionsPool.Static);
                uc.DiaData = ddrf;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataSelectQualifierPreset ddsq)
            {
                var fullfn = System.IO.Path.GetFullPath(Options.Curr.QualifiersFile);
                var uc = new SelectQualifierPresetFlyout(fullfn);
                uc.DiaData = ddsq;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataProgress ddpr)
            {
                var uc = new ProgressBarFlyout();
                uc.DiaData = ddpr;
                res = uc;
            }

            return res;
        }

        private void PerformSpecialOps(bool modal, AnyUiDialogueDataBase dialogueData)
        {
            if (modal && dialogueData is AnyUiDialogueDataOpenFile ddof)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                var res = dlg.ShowDialog();
                if (res == true)
                {
                    ddof.Result = true;
                    ddof.FileName = dlg.FileName;
                }
            }
        }

        /// <summary>
        /// Shows specified dialogue hardware-independent. The technology implementation will show the
        /// dialogue based on the type of provided <c>dialogueData</c>. 
        /// Modal dialogue: this function will block, until user ends dialogue.
        /// </summary>
        /// <param name="dialogueData"></param>
        /// <returns>If the dialogue was end with "OK" or similar success.</returns>
        public override bool StartFlyoverModal(AnyUiDialogueDataBase dialogueData)
        {
            // access
            if (dialogueData == null || FlyoutProvider == null)
                return false;

            // make sure to reset
            dialogueData.Result = false;

            // beware of exceptions
            try
            {
                var uc = DispatchFlyout(dialogueData);
                if (uc != null)
                {
                    if (dialogueData.HasModalSpecialOperation)
                        // start WITHOUT modal
                        FlyoutProvider?.StartFlyover(uc);
                    else
                        FlyoutProvider?.StartFlyoverModal(uc);
                }

                // now, in case
                PerformSpecialOps(modal: true, dialogueData: dialogueData);

                // may be close?
                if (dialogueData.HasModalSpecialOperation)
                    // start WITHOUT modal
                    FlyoutProvider?.CloseFlyover();
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"while showing modal AnyUI dialogue {dialogueData.GetType().ToString()}");
            }

            // result
            return dialogueData.Result;
        }

        /// <summary>
        /// Shows specified dialogue hardware-independent. The technology implementation will show the
        /// dialogue based on the type of provided <c>dialogueData</c>. 
        /// Non-modal: This function wil return immideately after initially displaying the dialogue.
        /// </summary>
        /// <param name="dialogueData"></param>
        /// <returns>If the dialogue was end with "OK" or similar success.</returns>
        public override void StartFlyover(AnyUiDialogueDataBase dialogueData)
        {
            // access
            if (dialogueData == null || FlyoutProvider == null)
                return;

            // make sure to reset
            dialogueData.Result = false;

            // beware of exceptions
            try
            {
                var uc = DispatchFlyout(dialogueData);
                if (uc != null)
                    FlyoutProvider?.StartFlyover(uc);

            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"while starting AnyUI dialogue {dialogueData.GetType().ToString()}");
            }
        }

        /// <summary>
        /// Closes started flyover dialogue-
        /// </summary>
        public override void CloseFlyover()
        {
            FlyoutProvider.CloseFlyover();
        }

        //
        // Special functions
        //

        /// <summary>
        /// Print a page with QR codes
        /// </summary>
        public override void PrintSingleAssetCodeSheet(
            string assetId, string description, string title = "Single asset code sheet")
        {
            AasxPrintFunctions.PrintSingleAssetCodeSheet(assetId, description, title);
        }
    }

    public class AnyUiColorToWpfBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is AnyUiColor col)
                return AnyUiDisplayContextWpf.GetWpfBrush(col);
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is SolidColorBrush br)
            {
                return AnyUiDisplayContextWpf.GetAnyUiColor(br);
            }
            return AnyUiColors.Default;
        }
    }

    public class AnyUiBrushToWpfBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is AnyUiBrush br)
                return AnyUiDisplayContextWpf.GetWpfBrush(br);
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is SolidColorBrush br)
            {
                return AnyUiDisplayContextWpf.GetAnyUiBrush(br);
            }
            return AnyUiBrushes.Default;
        }
    }

    public class AnyUiVisibilityToWpfVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is AnyUiVisibility vis)
            {
                if (vis == AnyUiVisibility.Visible)
                    return System.Windows.Visibility.Visible;
                if (vis == AnyUiVisibility.Collapsed)
                    return System.Windows.Visibility.Collapsed;
            }
            return System.Windows.Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is System.Windows.Visibility vis)
            {
                if (vis == Visibility.Visible)
                    return AnyUiVisibility.Visible;
                if (vis == Visibility.Collapsed)
                    return AnyUiVisibility.Collapsed;
            }
            return AnyUiVisibility.Hidden;
        }
    }

#if _not_needed
    public class AlwaysTrueValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return true;
        }
    }
#endif
}
