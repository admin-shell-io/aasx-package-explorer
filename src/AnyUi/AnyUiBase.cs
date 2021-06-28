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

    public class AnyUiColumnDefinition
    {
        public AnyUiGridLength Width;
        public double? MinWidth;
    }

    public class AnyUiRowDefinition
    {
        public AnyUiGridLength Height;
        public double? MinHeight;
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
                ui = Convert.ToUInt32(st.Substring(1));
            if (st.Length == 7)
                ui = 0xff000000u | Convert.ToUInt32(st.Substring(1));
            return new AnyUiColor(ui);
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
    }

    public class AnyUiBrushes
    {
        public static AnyUiBrush Default { get { return new AnyUiBrush(0xff000000u); } }
        public static AnyUiBrush Transparent { get { return new AnyUiBrush(0x00000000u); } }
        public static AnyUiBrush Black { get { return new AnyUiBrush(0xff000000u); } }
        public static AnyUiBrush DarkBlue { get { return new AnyUiBrush(0xff00008bu); } }
        public static AnyUiBrush LightBlue { get { return new AnyUiBrush(0xffadd8e6u); } }
        public static AnyUiBrush White { get { return new AnyUiBrush(0xffffffffu); } }
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
    }

    public enum AnyUiVisibility : byte
    {
        Visible = 0,
        Hidden = 1,
        Collapsed = 2
    }

    //
    // bridge objects between AnyUI base classes and implementations
    //

    public class AnyUiDisplayDataBase
    {
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
        // these attributes are managed by the Grid.SetRow.. functions
        public int? GridRow, GridRowSpan, GridColumn, GridColumnSpan;

        /// <summary>
        /// Serves as alpha-numeric name to later bind specific implementations to it
        /// </summary>
        public string Name = null;

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
        /// If not null, this lambda result is automatically emitted as outside action,
        /// when the control "feels" to have a "final" selection (Enter, oder ComboBox selected)
        /// </summary>
        [JsonIgnore]
        public AnyUiLambdaActionBase takeOverLambda = null;

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
        public static AnyUiUIElement RegisterControl(
            AnyUiUIElement cntl, Func<object, AnyUiLambdaActionBase> setValue,
            AnyUiLambdaActionBase takeOverLambda = null)
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
    }

    public class AnyUiControl : AnyUiFrameworkElement
    {
        public AnyUiBrush Background = null;
        public AnyUiBrush Foreground = null;
        public AnyUiVerticalAlignment? VerticalContentAlignment;
        public AnyUiHorizontalAlignment? HorizontalContentAlignment;
    }

    public class AnyUiContentControl : AnyUiControl
    {
    }

    public class AnyUiDecorator : AnyUiFrameworkElement
    {
        public virtual AnyUiUIElement Child { get; set; }
    }

    public interface IEnumerateChildren
    {
        IEnumerable<AnyUiUIElement> GetChildren();
    }

    public class AnyUiPanel : AnyUiFrameworkElement, IEnumerateChildren
    {
        public AnyUiBrush Background;
        public List<AnyUiUIElement> Children = new List<AnyUiUIElement>();

        public IEnumerable<AnyUiUIElement> GetChildren()
        {
            if (Children != null)
                foreach (var child in Children)
                    yield return child;
        }
    }

    public class AnyUiGrid : AnyUiPanel
    {
        public List<AnyUiRowDefinition> RowDefinitions = new List<AnyUiRowDefinition>();
        public List<AnyUiColumnDefinition> ColumnDefinitions = new List<AnyUiColumnDefinition>();

        public static void SetRow(AnyUiUIElement el, int value) { if (el != null) el.GridRow = value; }
        public static void SetRowSpan(AnyUiUIElement el, int value) { if (el != null) el.GridRowSpan = value; }
        public static void SetColumn(AnyUiUIElement el, int value) { if (el != null) el.GridColumn = value; }
        public static void SetColumnSpan(AnyUiUIElement el, int value) { if (el != null) el.GridColumnSpan = value; }
    }

    public class AnyUiStackPanel : AnyUiPanel
    {
        public AnyUiOrientation? Orientation;
    }

    public class AnyUiWrapPanel : AnyUiPanel
    {
        public AnyUiOrientation? Orientation;
    }

    public class AnyUiBorder : AnyUiDecorator
    {
        public AnyUiBrush Background = null;
        public AnyUiThickness BorderThickness;
        public AnyUiBrush BorderBrush = null;
        public AnyUiThickness Padding;

        public bool IsDropBox = false;
    }

    public class AnyUiLabel : AnyUiContentControl
    {
        public AnyUiThickness Padding;
        public AnyUiFontWeight? FontWeight;
        public string Content = null;
    }

    public class AnyUiTextBlock : AnyUiFrameworkElement
    {
        public AnyUiBrush Background;
        public AnyUiBrush Foreground;
        public AnyUiThickness Padding;
        public AnyUiTextWrapping? TextWrapping;
        public AnyUiFontWeight? FontWeight;
        public double? FontSize;
        public string Text = null;
    }

    public class AnyUiSelectableTextBlock : AnyUiFrameworkElement
    {
        public AnyUiBrush Background;
        public AnyUiBrush Foreground;
        public AnyUiThickness Padding;
        public AnyUiTextWrapping? TextWrapping;
        public AnyUiFontWeight? FontWeight;
        public double? FontSize;
        public string Text = null;
        public bool TextAsHyperlink = false;
    }

    public class AnyUiHintBubble : AnyUiTextBox
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
}
