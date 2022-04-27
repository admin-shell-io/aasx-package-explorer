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
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

// Quite declarative approach for the future
// resharper disable UnassignedField.Global
// resharper disable ClassNeverInstantiated.Global

// This namespace implements an UI approach which can be implemented by any UI system, hence the name.
// For better PascalCasing, the 'i' is lowercase by intention. In written text, it shall be: "Any UI"
namespace AnyUi
{
    //
    // required Enums and helper classes
    //

    /// <summary>
    /// AnyUI can be rendered to different platforms
    /// </summary>
    public enum AnyUiTargetPlatform { None = 0, Wpf = 1, Browser = 2 }
        
    public enum AnyUiGridUnitType { Auto = 0, Pixel = 1, Star = 2 }

    public enum AnyUiHorizontalAlignment { Left = 0, Center = 1, Right = 2, Stretch = 3 }
    public enum AnyUiVerticalAlignment { Top = 0, Center = 1, Bottom = 2, Stretch = 3 }

    public enum AnyUiOrientation { Horizontal = 0, Vertical = 1 }

    public enum AnyUiScrollBarVisibility { Disabled = 0, Auto = 1, Hidden = 2, Visible = 3 }

    public enum AnyUiTextWrapping { WrapWithOverflow = 0, NoWrap = 1, Wrap = 2 }

    public enum AnyUiFontWeight { Normal = 0, Bold = 1 }

    public class AnyUiGridLength
    {
        public double Value = 1.0;
        public AnyUiGridUnitType Type = AnyUiGridUnitType.Auto;

        public static AnyUiGridLength Auto { get { return new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto); } }

        public AnyUiGridLength() { }

        public AnyUiGridLength(double value, AnyUiGridUnitType type = AnyUiGridUnitType.Auto)
        {
            this.Value = value;
            this.Type = type;
        }
    }

    public class AnyUiListOfGridLength : List<AnyUiGridLength>
    {
        public static AnyUiListOfGridLength Parse(string[] input)
        {
            // access
            if (input == null)
                return null;

            var res = new AnyUiListOfGridLength();
            foreach (var part in input)
            {
                // default
                var gl = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);

                // work on part of input
                double scale = 1.0;
                var kind = part.Trim();
                var m = Regex.Match(kind, @"([0-9.+-]+)(.$)");
                if (m.Success && m.Groups.Count >= 2)
                {
                    var scaleSt = m.Groups[1].ToString().Trim();
                    if (Double.TryParse(scaleSt, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                        scale = d;
                    kind = m.Groups[2].ToString().Trim();
                }
                if (kind == "#")
                    gl = new AnyUiGridLength(scale, AnyUiGridUnitType.Auto);
                if (kind == "*")
                    gl = new AnyUiGridLength(scale, AnyUiGridUnitType.Star);
                if (kind == ":")
                    gl = new AnyUiGridLength(scale, AnyUiGridUnitType.Pixel);

                // add
                res.Add(gl);
            }

            return res;
        }
    }

    public class AnyUiColumnDefinition
    {
        public AnyUiGridLength Width;
        public double? MinWidth, MaxWidth;
    }

    public class AnyUiRowDefinition
    {
        public AnyUiGridLength Height;
        public double? MinHeight;

        public AnyUiRowDefinition() { }

        public AnyUiRowDefinition(double value, AnyUiGridUnitType type = AnyUiGridUnitType.Auto, double? minHeight = null)
        {
            Height = new AnyUiGridLength(value, type);
            MinHeight = minHeight;
        }
    }

    public class AnyUiColor
    {
        public byte A { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public Single ScA { get { return A / 255.0f; } set { A = (byte)(255.0f * value); } }
        public Single ScR { get { return R / 255.0f; } set { R = (byte)(255.0f * value); } }
        public Single ScG { get { return G / 255.0f; } set { G = (byte)(255.0f * value); } }
        public Single ScB { get { return B / 255.0f; } set { B = (byte)(255.0f * value); } }

        public AnyUiColor()
        {
            A = 0xff;
        }

        public AnyUiColor(UInt32 c)
        {
            byte[] bytes = BitConverter.GetBytes(c);
            A = bytes[3];
            R = bytes[2];
            G = bytes[1];
            B = bytes[0];
        }

        public static AnyUiColor FromArgb(byte a, byte r, byte g, byte b)
        {
            var res = new AnyUiColor();
            res.A = a;
            res.R = r;
            res.G = g;
            res.B = b;
            return res;
        }

        public static AnyUiColor FromRgb(byte r, byte g, byte b)
        {
            var res = new AnyUiColor();
            res.A = 0xff;
            res.R = r;
            res.G = g;
            res.B = b;
            return res;
        }

        public static AnyUiColor FromString(string st)
        {
            if (st == null || !st.StartsWith("#") || (st.Length != 7 && st.Length != 9))
                return AnyUiColors.Default;
            UInt32 ui = 0;
            if (st.Length == 9)
                ui = Convert.ToUInt32(st.Substring(1), 16);
            if (st.Length == 7)
                ui = 0xff000000u | Convert.ToUInt32(st.Substring(1), 16);
            return new AnyUiColor(ui);
        }

        public string ToHtmlString(int format)
        {
            if (format == 1)
                // ARGB
                return $"#{A:X2}{R:X2}{G:X2}{B:X2}";

            if (format == 2)
                // ARGB
                return FormattableString.Invariant($"rgba({R},{G},{B},{(A / 255.0):0.###})");

            // default just RGB
            return $"#{R:X2}{G:X2}{B:X2}";
        }
    }

    public class AnyUiColors
    {
        public static AnyUiColor Default { get { return new AnyUiColor(0xff000000u); } }
        public static AnyUiColor Transparent { get { return new AnyUiColor(0x00000000u); } }
        public static AnyUiColor Black { get { return new AnyUiColor(0xff000000u); } }
        public static AnyUiColor DarkBlue { get { return new AnyUiColor(0xff00008bu); } }
        public static AnyUiColor LightBlue { get { return new AnyUiColor(0xffadd8e6u); } }
        public static AnyUiColor Blue { get { return new AnyUiColor(0xff0000ffu); } }
        public static AnyUiColor Green { get { return new AnyUiColor(0xff00ff00u); } }
        public static AnyUiColor Orange { get { return new AnyUiColor(0xffffa500u); } }
        public static AnyUiColor White { get { return new AnyUiColor(0xffffffffu); } }
    }

    public class AnyUiBrush
    {
        private AnyUiColor solidColorBrush = AnyUiColors.Black;

        public AnyUiColor Color { get { return solidColorBrush; } }

        public AnyUiBrush() { }

        public AnyUiBrush(AnyUiColor c)
        {
            solidColorBrush = c;
        }

        public AnyUiBrush(UInt32 c)
        {
            solidColorBrush = new AnyUiColor(c);
        }

        public string HtmlRgb()
        {
            return "rgb(" +
                solidColorBrush.R + ", " +
                solidColorBrush.G + ", " +
                solidColorBrush.B + ")";
        }
    }

    public class AnyUiBrushes
    {
        public static AnyUiBrush Default { get { return new AnyUiBrush(0xff000000u); } }
        public static AnyUiBrush Transparent { get { return new AnyUiBrush(0x00000000u); } }
        public static AnyUiBrush Black { get { return new AnyUiBrush(0xff000000u); } }
        public static AnyUiBrush DarkBlue { get { return new AnyUiBrush(0xff00008bu); } }
        public static AnyUiBrush LightBlue { get { return new AnyUiBrush(0xffadd8e6u); } }
        public static AnyUiBrush White { get { return new AnyUiBrush(0xffffffffu); } }
        public static AnyUiBrush LightGray { get { return new AnyUiBrush(0xffe8e8e8u); } }
        public static AnyUiBrush MiddleGray { get { return new AnyUiBrush(0xffc8c8c8u); } }
        public static AnyUiBrush DarkGray { get { return new AnyUiBrush(0xff808080u); } }
    }

    public class AnyUiBrushTuple
    {
        public AnyUiBrush Bg, Fg;

        public AnyUiBrushTuple() { }

        public AnyUiBrushTuple(AnyUiBrush bg, AnyUiBrush fg)
        {
            this.Bg = bg;
            this.Fg = fg;
        }
    }

    public class AnyUiThickness
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }

        public AnyUiThickness() { }

        public AnyUiThickness(double all)
        {
            Left = all; Top = all; Right = all; Bottom = all;
        }

        public AnyUiThickness(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public bool AllEqual =>
            Left == Top
            && Left == Right
            && Left == Bottom;

        public bool AllZero => AllEqual && Left == 0.0;

        public double Width => Left + Right;
    }

    public enum AnyUiVisibility : byte
    {
        Visible = 0,
        Hidden = 1,
        Collapsed = 2
    }

    public enum AnyUiStretch
    {
        None,
        Fill,
        Uniform,
        UniformToFill
    }

    public struct AnyUiPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public AnyUiPoint(double x, double y) { X = x; Y = y; }
    }

    public struct AnyUiRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public double X2 => X + Width;
        public double Y2 => Y + Height;

        public AnyUiRect(AnyUiRect other)
        {
            X = other.X; 
            Y = other.Y; 
            Width = other.Width; 
            Height = other.Height;
        }

        public AnyUiRect(double x, double y, double w, double h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
        }

        public static AnyUiRect Max(AnyUiRect r1, AnyUiRect r2)
        {
            var x0 = Math.Min(r1.X, r2.X);
            var y0 = Math.Min(r1.Y, r2.Y);

            var x2 = Math.Max(r1.X2, r2.X2);
            var y2 = Math.Max(r1.Y2, r2.Y2);

            return new AnyUiRect(x0, y0, x2 - x0, y2 - y0);
        }
    }

    public class AnyUiPointCollection : List<AnyUiPoint>
    {
        public AnyUiPoint FindCG()
        {
            if (this.Count < 1)
                return new AnyUiPoint(0, 0);

            AnyUiPoint sum = new AnyUiPoint(0, 0);
            foreach (var p in this)
            {
                sum.X += p.X;
                sum.Y += p.Y;
            }

            sum.X /= this.Count;
            sum.Y /= this.Count;

            return sum;
        }

        public AnyUiRect FindBoundingBox()
        {
            var res = new AnyUiRect()
            {
                X = double.MaxValue,
                Y = double.MaxValue
            };
            
            foreach (var p in this)
            {
                if (p.X < res.X)
                    res.X = p.Y;
                if (p.Y < res.Y)
                    res.Y = p.Y;

                if (p.X > res.X + res.Width)
                    res.Width = p.X - res.X;
                if (p.Y > res.Y + res.Height)
                    res.Height = p.Y - res.Y;
            }

            return res;
        }
    }

    //
    // bridge objects between AnyUI base classes and implementations
    //

    public class AnyUiDisplayDataBase
    {
        /// <summary>
        /// Initiates a drop operation with one ore more files given by filenames.
        /// </summary>
        public virtual void DoDragDropFiles(AnyUiUIElement elem, string[] files) { }
    }

    //
    // Handling of events, callbacks and such
    //

    /// <summary>
    /// Any UI defines a lot of lambda functions for handling events.
    /// These lambdas can return results (commands) to the calling application,
    /// which might be given back to higher levels of the applications to trigger
    /// events, such as re-displaying pages or such.
    /// </summary>
    public class AnyUiLambdaActionBase
    {
    }

    /// <summary>
    /// An lambda action explicitely stating that nothing has to be done.
    /// </summary>
    public class AnyUiLambdaActionNone : AnyUiLambdaActionBase
    {
    }

    /// <summary>
    /// Value of the AnyUI control were changed
    /// </summary>
    public class AnyUiLambdaActionContentsChanged : AnyUiLambdaActionBase
    {
    }

    /// <summary>
    /// Changed values shall be taken over to main application
    /// </summary>
    public class AnyUiLambdaActionContentsTakeOver : AnyUiLambdaActionBase
    {
    }

    public enum AnyUiRenderMode { All, StatusToUi }

    /// <summary>
    /// This event causes a call to the specified plugin to update its
    /// already preseted AnyUi representation and will then re-render this
    /// to the users interface.
    /// The plugin action is supposed to be: "update-anyui-visual-extension"
    /// </summary>
    public class AnyUiLambdaActionPluginUpdateAnyUi : AnyUiLambdaActionBase
    {
        public string PluginName = "";
        public object[] ActionArgs = null;
        public AnyUiRenderMode UpdateMode = AnyUiRenderMode.All;
        public bool UseInnerGrid = false;
    }

    /// <summary>
    /// Requests the main application to display a content file or external link
    /// </summary>
    public class AnyUiLambdaActionDisplayContentFile : AnyUiLambdaActionBase
    {
        public AnyUiLambdaActionDisplayContentFile() { }
        public AnyUiLambdaActionDisplayContentFile(
            string fn, string mimeType = null, bool preferInternalDisplay = false)
        {
            this.fn = fn;
            this.mimeType = mimeType;
            this.preferInternalDisplay = preferInternalDisplay;
        }

        public string fn = null;
        public string mimeType = null;
        public bool preferInternalDisplay = false;
    }

    /// <summary>
    /// Request to redraw the current element/ entity.
    /// </summary>
    public class AnyUiLambdaActionRedrawEntity : AnyUiLambdaActionBase { }

    /// <summary>
    /// Reqeust to redraw the full tree of elements, may set new focus or
    /// expand state.
    /// </summary>
    public class AnyUiLambdaActionRedrawAllElementsBase : AnyUiLambdaActionBase
    {
        public object NextFocus = null;
        public bool? IsExpanded = null;
        public bool OnlyReFocus = false;
        public bool RedrawCurrentEntity = false;
    }

    /// <summary>
    /// This class is the base class for event handlers, which can attached to special
    /// events of Any UI controls
    /// </summary>
    public class AnyUiSpecialActionBase
    {
    }

    public class AnyUiSpecialActionContextMenu : AnyUiSpecialActionBase
    {
        public string[] MenuItemHeaders;
        [JsonIgnore]
        public Func<object, AnyUiLambdaActionBase> MenuItemLambda;

        public AnyUiSpecialActionContextMenu() { }

        public AnyUiSpecialActionContextMenu(
            string[] menuItemHeaders,
            Func<object, AnyUiLambdaActionBase> menuItemLambda)
        {
            MenuItemHeaders = menuItemHeaders;
            MenuItemLambda = menuItemLambda;
        }
    }

    //
    // Hierarchy of AnyUI graphical elements (including controls).
    // This hierarchy stems from the WPF hierarchy but should be sufficiently 
    // abstracted in order to be implemented an many UI systems
    //

    /// <summary>
    /// Absolute base class of all AnyUI graphical elements
    /// </summary>
    public class AnyUiUIElement
    {
        // these attributes are typically managed by the (automatic) layout
        // exception: shapes
        public double X, Y, Width, Height;

        // these attributes are managed by the Grid.SetRow.. functions
        public int? GridRow, GridRowSpan, GridColumn, GridColumnSpan;

        /// <summary>
        /// Serves as alpha-numeric name to later bind specific implementations to it
        /// </summary>
        public string Name = null;        

        /// <summary>
        /// If true, can be skipped when rendered into a browser
        /// </summary>
        public AnyUiTargetPlatform SkipForTarget = AnyUiTargetPlatform.None;

        /// <summary>
        /// This onjects builds the bridge to the specific implementation, e.g., WPF.
        /// The specific implementation overloads AnyUiDisplayDataBase to be able to store specific data
        /// per UI element, such as the WPF UIElement.
        /// </summary>
        public AnyUiDisplayDataBase DisplayData;

        /// <summary>
        /// Stores the original (initial) value in string representation.
        /// </summary>
        public object originalValue = null;

        /// <summary>
        /// This callback (lambda) is activated by the control frequently, if a value update
        /// occurs. The calling application needs to set this lambda in order to receive
        /// value updates.
        /// Note: currently, the function result (the lambda) is being ignored except for
        /// Buttons
        /// </summary>
        [JsonIgnore]
        public Func<object, AnyUiLambdaActionBase> setValueLambda = null;

        /// <summary>
        /// Arbitrary object/ tag exclusively used for ad-hoc debug. Do not use for long-term
        /// purposes.
        /// </summary>
        [JsonIgnore]
        public object DebugTag = null;

        /// <summary>
        /// If not null, this lambda result is automatically emitted as outside action,
        /// when the control "feels" to have a "final" selection (Enter, oder ComboBox selected)
        /// </summary>
        [JsonIgnore]
        public AnyUiLambdaActionBase takeOverLambda = null;

        /// <summary>
        /// Indicates, that the status of the element was updated
        /// </summary>
        public bool Touched;

        /// <summary>
        /// Touches the element
        /// </summary>
        public virtual void Touch() { Touched = true; }

        //public class TouchProperty<T>
        //{
        //    protected T _value;
        //    protected Action _touched;
        //    public T Value { 
        //        get { 
        //            return _value; 
        //        } 
        //        set
        //        {
        //            _value = value;
        //            _touched?.Invoke();
        //        }
        //    }

        //    public TouchProperty(Action touched)
        //    {
        //        _touched = touched;
        //    }

        //    public TouchProperty(Action touched, T inital)
        //    {
        //        _value = inital;
        //        _touched = touched;
        //    }
        //}

        /// <summary>
        /// Can be set by the rendering of the element to perform status updates, if touched.
        /// </summary>
        // public Action<AnyUiRenderMode> TouchLambda = null;

        /// <summary>
        /// This function attaches the above lambdas accordingly to a given user control.
        /// It is to be used, when an abstract AnyUi... is being created and the according WPF element
        /// will be activated later.
        /// Note: use of this is for legacy reasons; basically the class members can be used directly
        /// </summary>
        /// <param name="cntl">User control (passed thru)</param>
        /// <param name="setValue">Lambda called, whenever the value is changed</param>
        /// <param name="takeOverLambda">Lamnda called at the end of a modification</param>
        /// <returns>Passes thru the user control</returns>
        public static T RegisterControl<T>(
            T cntl, Func<object, AnyUiLambdaActionBase> setValue,
            AnyUiLambdaActionBase takeOverLambda = null)
            where T : AnyUiUIElement
        {
            // access
            if (cntl == null)
                return null;

            // crude test
            cntl.setValueLambda = setValue;
            cntl.takeOverLambda = takeOverLambda;

            return cntl;
        }

        /// <summary>
        /// Allows setting the name for a control.
        /// </summary>
        /// <param name="cntl"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AnyUiUIElement NameControl(
            AnyUiUIElement cntl, string name)
        {
            // access
            if (cntl == null)
                return null;

            // set
            cntl.Name = name;
            return cntl;
        }

        /// <summary>
        /// Find all children in the (deep) hierarchy, which feature a Name != null.
        /// </summary>
        public IEnumerable<AnyUiUIElement> FindAllNamed()
        {
            if (this.Name != null)
                yield return this;

            if (this is IEnumerateChildren en)
                foreach (var child in en.GetChildren())
                    foreach (var x in child.FindAllNamed())
                        yield return x;
        }

        /// <summary>
        /// Find all children in the (deep) hierarchy, and invoke predicate.
        /// </summary>
        public IEnumerable<AnyUiUIElement> FindAll(Func<AnyUiUIElement, bool> predicate = null)
        {
            if (predicate == null || predicate.Invoke(this))
                yield return this;

            if (this is IEnumerateChildren en)
                foreach (var child in en.GetChildren())
                    foreach (var x in child.FindAll(predicate))
                        yield return x;
        }
    }

    public enum AnyUiEventMask { None = 0, LeftDown = 1, LeftDouble = 2, DragStart = 4,
        MouseAll = LeftDown + LeftDouble }

    public class AnyUiEventData
    {
        public AnyUiEventMask Mask;
        public int ClickCount;
        public object Source;
        public AnyUiPoint RelOrigin;

        public AnyUiEventData() { }

        public AnyUiEventData(AnyUiEventMask mask, object source, int clickCount = 1, 
            AnyUiPoint? relOrigin = null)
        {
            Mask = mask;
            Source = source;
            ClickCount = clickCount;
            if (relOrigin.HasValue)
                RelOrigin = relOrigin.Value;
        }
    }

    public class AnyUiFrameworkElement : AnyUiUIElement
    {
        public AnyUiThickness Margin;
        public AnyUiVerticalAlignment? VerticalAlignment;
        public AnyUiHorizontalAlignment? HorizontalAlignment;

        public double? MinHeight;
        public double? MinWidth;
        public double? MaxHeight;
        public double? MaxWidth;

        public object Tag = null;

        public AnyUiEventMask EmitEvent;        
    }

    /// <summary>
    /// This is the base class for all primitive shapes in AnyUI.
    /// In WPF this is a subclass of FrameworkElement.
    /// </summary>
    public class AnyUiShape : AnyUiFrameworkElement
    {
        public AnyUiBrush Fill, Stroke;
        public double? StrokeThickness;

        public virtual AnyUiRect FindBoundingBox() => new AnyUiRect();

        public virtual bool IsHit(AnyUiPoint pt) => false;
    }

    public class AnyUiRectangle : AnyUiShape
    {
        public override AnyUiRect FindBoundingBox() => 
            new AnyUiRect(X, Y, Width, Height );

        public override bool IsHit(AnyUiPoint pt) =>
            (pt.X >= this.X) && (pt.X <= this.X + this.Width)
            && (pt.Y >= this.Y) && (pt.Y <= this.Y + this.Height);
    }

    public class AnyUiEllipse : AnyUiShape
    {
        public override AnyUiRect FindBoundingBox() =>
            new AnyUiRect(X, Y, Width, Height);

        public override bool IsHit(AnyUiPoint pt)
        {
            // see: https://www.geeksforgeeks.org/check-if-a-point-is-inside-outside-or-on-the-ellipse/
            var h = this.X + this.Width / 2.0;
            var k = this.Y + this.Height / 2.0;
            var a = this.Width / 2.0;
            var b = this.Height / 2.0;

            int p = ((int)Math.Pow((pt.X - h), 2) /
                    (int)Math.Pow(a, 2)) +
                    ((int)Math.Pow((pt.Y - k), 2) /
                    (int)Math.Pow(b, 2));

            return p <= 1;
        }
    }

    public class AnyUiPolygon : AnyUiShape
    {
        public AnyUiPointCollection Points = new AnyUiPointCollection();

        public override AnyUiRect FindBoundingBox()
        {
            if (Points == null || Points.Count < 1)
                return new AnyUiRect();
            return Points.FindBoundingBox();
        }

        // see: https://stackoverflow.com/questions/4243042/c-sharp-point-in-polygon
        /// <summary>
        /// Determines if the given point is inside the polygon
        /// </summary>
        /// <param name="polygon">the vertices of polygon</param>
        /// <param name="testPoint">the given point</param>
        /// <returns>true if the point is inside the polygon; otherwise, false</returns>
        private static bool IsPointInPolygon4(AnyUiPoint[] polygon, AnyUiPoint testPoint)
        {
            bool result = false;
            int j = polygon.Count() - 1;
            for (int i = 0; i < polygon.Count(); i++)
            {
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y || polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        public override bool IsHit(AnyUiPoint pt)
        {
            if (Points == null || Points.Count < 1)
                return false;

            return IsPointInPolygon4(Points.ToArray(), pt);
        }
    }

    public class AnyUiControl : AnyUiFrameworkElement, IGetBackground
    {
        public AnyUiBrush Background = null;
        public AnyUiBrush Foreground = null;
        public AnyUiVerticalAlignment? VerticalContentAlignment;
        public AnyUiHorizontalAlignment? HorizontalContentAlignment;
        public double? FontSize;
        public AnyUiFontWeight? FontWeight;

        public AnyUiBrush GetBackground() => Background;
    }

    public class AnyUiContentControl : AnyUiControl, IEnumerateChildren
    {
        public virtual AnyUiUIElement Content { get; set; }

        public IEnumerable<AnyUiUIElement> GetChildren()
        {
            if (Content != null)
                yield return Content;
        }
    }

    public class AnyUiDecorator : AnyUiFrameworkElement, IEnumerateChildren
    {
        public virtual AnyUiUIElement Child { get; set; }

        public IEnumerable<AnyUiUIElement> GetChildren()
        {
            if (Child != null)
                yield return Child;
        }

        public T SetChild<T>(T child)
            where T : AnyUiUIElement
        {
            Child = child;
            return child;
        }
    }

    public class AnyUiViewbox : AnyUiDecorator
    {
        public AnyUiStretch Stretch = AnyUiStretch.None;
    }

    public interface IEnumerateChildren
    {
        IEnumerable<AnyUiUIElement> GetChildren();
    }

    public interface IGetBackground
    {
        AnyUiBrush GetBackground();
    }

    public class AnyUiPanel : AnyUiFrameworkElement, IEnumerateChildren, IGetBackground
    {
        public AnyUiBrush Background;
        
        private List<AnyUiUIElement> _children = new List<AnyUiUIElement>();
        public List<AnyUiUIElement> Children { 
            get { return _children; } 
            set { _children = value; Touch(); } 
        } 

        public T Add<T>(T elem) where T : AnyUiUIElement
        {
            Children.Add(elem);
            Touch();
            return elem;
        }

        public IEnumerable<AnyUiUIElement> GetChildren()
        {
            if (Children != null)
                foreach (var child in Children)
                    yield return child;
        }

        public AnyUiBrush GetBackground() => Background;
    }

    public class AnyUiGrid : AnyUiPanel
    {
        public List<AnyUiRowDefinition> RowDefinitions = new List<AnyUiRowDefinition>();
        public List<AnyUiColumnDefinition> ColumnDefinitions = new List<AnyUiColumnDefinition>();

        public static void SetRow(AnyUiUIElement el, int value) { if (el != null) el.GridRow = value; }
        public static void SetRowSpan(AnyUiUIElement el, int value) { if (el != null) el.GridRowSpan = value; }
        public static void SetColumn(AnyUiUIElement el, int value) { if (el != null) el.GridColumn = value; }
        public static void SetColumnSpan(AnyUiUIElement el, int value) { if (el != null) el.GridColumnSpan = value; }

        public IEnumerable<AnyUiUIElement> GetChildsAt(int row, int col)
        {
            if (Children == null || RowDefinitions == null || ColumnDefinitions == null
                || row < 0 || row >= RowDefinitions.Count
                || col < 0 || col >= ColumnDefinitions.Count)
                yield break;

            foreach (var ch in Children)
                if (ch.GridRow.HasValue && ch.GridRow.Value == row
                    && ch.GridColumn.HasValue && ch.GridColumn.Value == col)
                    yield return ch;         
        }

        public AnyUiUIElement IsCoveredBySpanCell(
            int row, int col,
            bool returnOnRootCell = false,
            bool returnOnSpanCell = false)
        {
            if (Children == null || RowDefinitions == null || ColumnDefinitions == null
                || row < 0 || row >= RowDefinitions.Count
                || col < 0 || col >= ColumnDefinitions.Count)
                return null;

            foreach (var ch in Children)
            {
                // valid at all?
                if (ch.GridRow == null || ch.GridColumn == null)
                    continue;

                // first check, if in intervals

                var rowSpan = 1;
                if (ch.GridRowSpan.HasValue && ch.GridRowSpan.Value > 1)
                    rowSpan = ch.GridRowSpan.Value;

                var colSpan = 1;
                if (ch.GridColumnSpan.HasValue && ch.GridColumnSpan.Value > 1)
                    colSpan = ch.GridColumnSpan.Value;

                if (row >= ch.GridRow.Value && (row <= ch.GridRow.Value + rowSpan - 1)
                    && col >= ch.GridColumn.Value && (col <= ch.GridColumn.Value + colSpan - 1))
                {
                    // at least in ..
                    // .. but first check for root ..
                    if (returnOnRootCell 
                        && ch.GridRow.Value == row && ch.GridColumn.Value == col)
                        return ch;

                    // .. check for spans
                    if (returnOnSpanCell)
                        return ch;
                }
            }

            return null;
        }

        public (int, int) GetMaxRowCol()
        {
            var maxRow = 0;
            var maxCol = 0;
            if (Children != null)
                foreach (var ch in Children)
                {
                    if (ch.GridRow.HasValue)
                    {
                        var r = ch.GridRow.Value;
                        if (ch.GridRowSpan.HasValue)
                            r += -1 + ch.GridRowSpan.Value;
                        if (r > maxRow)
                            maxRow = r;
                    }

                    if (ch.GridColumn.HasValue)
                    {
                        var c = ch.GridColumn.Value;
                        if (ch.GridColumnSpan.HasValue)
                            c += -1 + ch.GridColumnSpan.Value;
                        if (c > maxCol)
                            maxCol = c;
                    }
                }
            return (maxRow, maxCol);
        }

        public void FixRowColDefs()
        {
            var (maxRow, maxCol) = GetMaxRowCol();

            if (RowDefinitions == null)
                RowDefinitions = new List<AnyUiRowDefinition>();
            while (RowDefinitions.Count < (1 + maxRow))
                RowDefinitions.Add(
                    new AnyUiRowDefinition() { Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto) });

            if (ColumnDefinitions == null)
                ColumnDefinitions = new List<AnyUiColumnDefinition>();
            while (ColumnDefinitions.Count < (1 + maxCol))
                ColumnDefinitions.Add(
                    new AnyUiColumnDefinition() { Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto) });
        }
    }

    public class AnyUiStackPanel : AnyUiPanel
    {
        public AnyUiOrientation? Orientation;
    }

    public class AnyUiWrapPanel : AnyUiPanel
    {
        public AnyUiOrientation? Orientation;
    }

    public class AnyUiCanvas : AnyUiPanel
    {
    }

    public class AnyUiScrollViewer : AnyUiContentControl
    {
        public AnyUiScrollBarVisibility? HorizontalScrollBarVisibility;
        public AnyUiScrollBarVisibility? VerticalScrollBarVisibility;

        /// <summary>
        /// In case of (re-) display, scroll immediately to a desireded vertical position
        /// </summary>
        public double? InitialScrollPosition = null;

        /// <summary>
        /// If true, create not a scrollable area, but simply add childs below
        /// (that is: use system provided scrolling)
        /// </summary>
        public AnyUiTargetPlatform FlattenForTarget = AnyUiTargetPlatform.None;
    }

    public class AnyUiBorder : AnyUiDecorator, IGetBackground
    {
        public AnyUiBrush Background = null;
        public AnyUiThickness BorderThickness;
        public AnyUiBrush BorderBrush = null;
        public AnyUiThickness Padding;
        public double? CornerRadius = null;

        public bool IsDropBox = false;

        public AnyUiBrush GetBackground() => Background;
    }

    public class AnyUiLabel : AnyUiContentControl
    {
        public AnyUiThickness Padding;
        public AnyUiFontWeight? FontWeight;
        public string Content = null;
    }

    public class AnyUiTextBlock : AnyUiControl
    {
        //public AnyUiBrush Background;
        //public AnyUiBrush Foreground;
        public AnyUiThickness Padding;
        public AnyUiTextWrapping? TextWrapping;
        // public AnyUiFontWeight? FontWeight;
        // public double? FontSize;

        public string Text { get { return _text; } set { _text = value; Touch(); } }
        private string _text = null;
    }

    public class AnyUiSelectableTextBlock : AnyUiTextBlock
    {
        public bool TextAsHyperlink = false;
    }

    public class AnyUiHintBubble : AnyUiTextBlock
    {
    }

    public class AnyUiTextBox : AnyUiControl
    {
        public AnyUiThickness Padding;

        public AnyUiScrollBarVisibility VerticalScrollBarVisibility;

        public bool AcceptsReturn;
        public Nullable<int> MaxLines;

        public string Text = null;
    }

    public class AnyUiComboBox : AnyUiControl
    {
        public AnyUiThickness Padding;

        public bool? IsEditable;

        public List<object> Items = new List<object>();
        public string Text = null;

        public int? SelectedIndex;

        public void EvalSelectedIndex(string value)
        {
            if (value == null)
                return;

            if (Items != null)
                for (int i = 0; i < Items.Count; i++)
                    if (Items[i] as string == value)
                        SelectedIndex = i;
        }
    }

    public class AnyUiCheckBox : AnyUiContentControl
    {
        public AnyUiThickness Padding;

        public string Content = null;

        public bool? IsChecked;
    }

    public class AnyUiButton : AnyUiContentControl
    {
        public AnyUiThickness Padding;

        public string Content = null;
        public string ToolTip = null;

        public AnyUiSpecialActionBase SpecialAction;
    }

    public class AnyUiCountryFlag : AnyUiFrameworkElement
    {
        public string ISO3166Code = "";
    }

    public class AnyUiBitmapInfo
    {
        /// <summary>
        /// The bitmap data; as anonymous object (because of dependencies). 
        /// Expected is a BitmapSource/ ImageSource.
        /// </summary>
        public object ImageSource;

        /// <summary>
        /// Pixel-dimensions of the bitmap given by providing functionality.
        /// </summary>
        public double PixelWidth, PixelHeight;

        /// <summary>
        /// Bitmap as data bytes in PNG-format.
        /// </summary>
        public byte[] PngData;
    }

    public class AnyUiImage : AnyUiFrameworkElement
    {

        /// <summary>
        /// Guid of the image. Created by the constructor.
        /// </summary>
        public string ImageGuid = "";

        /// <summary>
        /// The bitmap data; as anonymous object (because of dependencies).
        /// Probably a ImageSource
        /// Setting touches to update
        /// </summary>
        public AnyUiBitmapInfo BitmapInfo { get { return _bitmapInfo; } set { _bitmapInfo = value; ReGuid(); Touch(); } }
        private AnyUiBitmapInfo _bitmapInfo = null;

        /// <summary>
        /// Stretch mode
        /// </summary>
        public AnyUiStretch Stretch = AnyUiStretch.None;

        //
        // Constructors
        //

        public AnyUiImage() { ReGuid(); }

        /// <summary>
        /// Initialize upon constructor, e.g. GUID
        /// </summary>
        protected void ReGuid()
        {
            ImageGuid = "IMG" + Guid.NewGuid().ToString("N");
            if (_imageDictionary.ContainsKey(ImageGuid))
                _imageDictionary.Remove(ImageGuid);
            _imageDictionary.Add(ImageGuid, this);
        }

        //
        // Singleton: Dictionary to find images
        //

        protected static Dictionary<string, AnyUiImage> _imageDictionary = new Dictionary<string, AnyUiImage>();

        public static AnyUiImage FindImage(string guid)
        {
            if (guid == null || !_imageDictionary.ContainsKey(guid))
                return null;

            return _imageDictionary[guid];
        }
    }
}
