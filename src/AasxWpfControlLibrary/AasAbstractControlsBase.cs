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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AasxIntegrationBase;
using AasxWpfControlLibrary;
using AdminShellNS;

namespace AasxPackageExplorer
{
    public enum AasCntlGridUnitType { Auto = 0, Pixel = 1, Star = 2 }

    public enum AasCntlHorizontalAlignment { Left = 0, Center = 1, Right = 2, Stretch = 3 }
    public enum AasCntlVerticalAlignment { Top = 0, Center = 1, Bottom = 2, Stretch = 3 }

    public class AasCntlGridLength
    {
        public double Value = 1.0;
        public AasCntlGridUnitType Type = AasCntlGridUnitType.Auto;

        public static AasCntlGridLength Auto { get { return new AasCntlGridLength(1.0, AasCntlGridUnitType.Auto); } }

        public AasCntlGridLength() { }

        public AasCntlGridLength(double value, AasCntlGridUnitType type = AasCntlGridUnitType.Auto)
        {
            this.Value = value;
            this.Type = type;
        }

        public GridLength GetWpfGridLength()
        {
            return new GridLength(this.Value, (GridUnitType)((int)Type));
        }
    }

    public class AasCntlColumnDefinition
    {
        public AasCntlGridLength Width;
        public double? MinWidth;

        public ColumnDefinition GetWpfColumnDefinition()
        {
            var res = new ColumnDefinition();
            if (this.Width != null)
                res.Width = this.Width.GetWpfGridLength();
            if (this.MinWidth.HasValue)
                res.MinWidth = this.MinWidth.Value;
            return res;
        }
    }

    public class AasCntlRowDefinition
    {
        public AasCntlGridLength Height;
        public double? MinHeight;

        public RowDefinition GetWpfRowDefinition()
        {
            var res = new RowDefinition();
            if (this.Height != null)
                res.Height = this.Height.GetWpfGridLength();
            if (this.MinHeight.HasValue)
                res.MinHeight = this.MinHeight.Value;
            return res;
        }
    }

    public class AasCntlBrush
    {
        private Color solidColorBrush = Colors.Black;

        public AasCntlBrush() { }

        public AasCntlBrush(Color c)
        {
            solidColorBrush = c;
        }

        public AasCntlBrush(SolidColorBrush b)
        {
            solidColorBrush = b.Color;
        }

        public AasCntlBrush(UInt32 c)
        {
            byte[] bytes = BitConverter.GetBytes(c);
            solidColorBrush = Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
        }

        public Brush GetWpfBrush()
        {
            return new SolidColorBrush(solidColorBrush);
        }
    }

    public class AasCntlBrushes
    {
        public static AasCntlBrush Black { get { return new AasCntlBrush(0xff000000u); } }
        public static AasCntlBrush DarkBlue { get { return new AasCntlBrush(0xff00008bu); } }
        public static AasCntlBrush LightBlue { get { return new AasCntlBrush(0xffadd8e6u); } }
        public static AasCntlBrush White { get { return new AasCntlBrush(0xffffffffu); } }
    }

    public class AasCntlThickness
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }

        public AasCntlThickness() { }
        
        public AasCntlThickness(double all) 
        { 
            Left = all; Top = all; Right = all; Bottom = all; 
        }
        
        public AasCntlThickness(double left, double top, double right, double bottom) 
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public Thickness GetWpfTickness()
        {
            return new Thickness(Left, Top, Right, Bottom);
        }
    }

    public class AasCntlDisplayContextBase
    {
    }

    

    public class AasCntlDisplayDataBase
    {
    }    

    public class AasCntlUIElement
    {
        public int? GridRow, GridRowSpan, GridColumn, GridColumnSpan;

        protected UIElement wpfElement;

        public AasCntlDisplayDataBase DisplayData;

        public virtual void RenderUIElement(UIElement el) { }

    }

    public class AasCntlFrameworkElement : AasCntlUIElement
    {
        public AasCntlThickness Margin;
        public AasCntlVerticalAlignment? VerticalAlignment;
        public AasCntlHorizontalAlignment? HorizontalAlignment;

        public double? MinHeight;
        public double? MinWidth;
        public double? MaxHeight;
        public double? MaxWidth;

        public object Tag = null;

    }

    public class AasCntlControl : AasCntlFrameworkElement
    {
        public AasCntlVerticalAlignment? VerticalContentAlignment;
        public AasCntlHorizontalAlignment? HorizontalContentAlignment;
    }

    public class AasCntlContentControl : AasCntlControl
    {
    }

    public class AasCntlDecorator : AasCntlFrameworkElement
    {
        public virtual UIElement Child { get; set; }

    }    

    public class AasCntlPanel : AasCntlFrameworkElement
    {
        public AasCntlBrush Background;
        public List<AasCntlUIElement> Children = new List<AasCntlUIElement>();

    }

    public class AasCntlGrid : AasCntlPanel
    {
        public List<AasCntlRowDefinition> RowDefinitions = new List<AasCntlRowDefinition>();
        public List<AasCntlColumnDefinition> ColumnDefinitions = new List<AasCntlColumnDefinition>();

        public static void SetRow(AasCntlUIElement el, int value) { if (el != null) el.GridRow = value; }        
        public static void SetRowSpan(AasCntlUIElement el, int value) { if (el != null) el.GridRowSpan = value; }
        public static void SetColumn(AasCntlUIElement el, int value) { if (el != null) el.GridColumn = value; }
        public static void SetColumnSpan(AasCntlUIElement el, int value) { if (el != null) el.GridColumnSpan = value; }

    }

    public class AasCntlStackPanel : AasCntlPanel
    {
        public Orientation? Orientation;

    }

    public class AasCntlWrapPanel : AasCntlPanel
    {
        public Orientation? Orientation;

    }    

    public class AasCntlBorder : AasCntlDecorator
    {
        public AasCntlBrush Background = null;
        public AasCntlThickness BorderThickness;
        public AasCntlBrush BorderBrush = null;
        public AasCntlThickness Padding;

    }

    public class AasCntlLabel : AasCntlContentControl
    {
        public AasCntlBrush Background;
        public AasCntlBrush Foreground;
        public AasCntlThickness Padding;

        public Nullable<FontWeight> FontWeight = null;
        public string Content = null;

    }

    public class AasCntlTextBlock : AasCntlFrameworkElement
    {
        public Brush Background;
        public Brush Foreground;
        public AasCntlThickness Padding;

        public Nullable<FontWeight> FontWeight = null;
        public string Text = null;

    }

    public class AasCntlHintBubble : AasCntlTextBox
    {
    }

    public class AasCntlTextBox : AasCntlControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public AasCntlThickness Padding;

        public ScrollBarVisibility VerticalScrollBarVisibility;

        public bool AcceptsReturn;
        public Nullable<int> MaxLines;

        public string Text = null;

    }

    public class AasCntlComboBox : AasCntlControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public AasCntlThickness Padding;

        public bool? IsEditable;

        public List<object> Items = new List<object>();
        public string Text = null;

        public int? SelectedIndex;

    }

    public class AasCntlCheckBox : AasCntlContentControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public AasCntlThickness Padding;

        public string Content = null;

        public bool? IsChecked;

    }

    public class AasCntlButton : AasCntlContentControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public AasCntlThickness Padding;

        public string Content = null;
        public string ToolTip = null;

        public event RoutedEventHandler Click;

    }
}
