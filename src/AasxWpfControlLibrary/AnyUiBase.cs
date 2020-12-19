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

namespace AnyUi
{
    //
    // required Enums and helper classes
    //

    public enum AnyUiGridUnitType { Auto = 0, Pixel = 1, Star = 2 }

    public enum AnyUiHorizontalAlignment { Left = 0, Center = 1, Right = 2, Stretch = 3 }
    public enum AnyUiVerticalAlignment { Top = 0, Center = 1, Bottom = 2, Stretch = 3 }

    public enum AnyUiOrientation { Horizontal = 0, Vertical = 1}

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

        public GridLength GetWpfGridLength()
        {
            return new GridLength(this.Value, (GridUnitType)((int)Type));
        }
    }

    public class AnyUiColumnDefinition
    {
        public AnyUiGridLength Width;
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

    public class AnyUiRowDefinition
    {
        public AnyUiGridLength Height;
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

    public class AnyUiBrush
    {
        private Color solidColorBrush = Colors.Black;

        public AnyUiBrush() { }

        public AnyUiBrush(Color c)
        {
            solidColorBrush = c;
        }

        public AnyUiBrush(SolidColorBrush b)
        {
            solidColorBrush = b.Color;
        }

        public AnyUiBrush(UInt32 c)
        {
            byte[] bytes = BitConverter.GetBytes(c);
            solidColorBrush = Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
        }

        public Brush GetWpfBrush()
        {
            return new SolidColorBrush(solidColorBrush);
        }
    }

    public class AnyUiBrushes
    {
        public static AnyUiBrush Black { get { return new AnyUiBrush(0xff000000u); } }
        public static AnyUiBrush DarkBlue { get { return new AnyUiBrush(0xff00008bu); } }
        public static AnyUiBrush LightBlue { get { return new AnyUiBrush(0xffadd8e6u); } }
        public static AnyUiBrush White { get { return new AnyUiBrush(0xffffffffu); } }
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

        public Thickness GetWpfTickness()
        {
            return new Thickness(Left, Top, Right, Bottom);
        }
    }

    //
    // bridge objects between AnyUI base classes and implementations
    //

    public class AnyUiContextBase
    {
    }    

    public class AnyUiDisplayDataBase
    {
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
        /// This onjects builds the bridge to the specific implementation, e.g., WPF.
        /// The specific implementation overloads AnyUiDisplayDataBase to be able to store specific data
        /// per UI element, such as the WPF UIElement.
        /// </summary>
        public AnyUiDisplayDataBase DisplayData;
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
        public AnyUiVerticalAlignment? VerticalContentAlignment;
        public AnyUiHorizontalAlignment? HorizontalContentAlignment;
    }

    public class AnyUiContentControl : AnyUiControl
    {
    }

    public class AnyUiDecorator : AnyUiFrameworkElement
    {
        public virtual UIElement Child { get; set; }
    }    

    public class AnyUiPanel : AnyUiFrameworkElement
    {
        public AnyUiBrush Background;
        public List<AnyUiUIElement> Children = new List<AnyUiUIElement>();
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

    }

    public class AnyUiLabel : AnyUiContentControl
    {
        public AnyUiBrush Background;
        public AnyUiBrush Foreground;
        public AnyUiThickness Padding;

        public Nullable<FontWeight> FontWeight = null;
        public string Content = null;

    }

    public class AnyUiTextBlock : AnyUiFrameworkElement
    {
        public Brush Background;
        public Brush Foreground;
        public AnyUiThickness Padding;

        public Nullable<FontWeight> FontWeight = null;
        public string Text = null;

    }

    public class AnyUiHintBubble : AnyUiTextBox
    {
    }

    public class AnyUiTextBox : AnyUiControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public AnyUiThickness Padding;

        public ScrollBarVisibility VerticalScrollBarVisibility;

        public bool AcceptsReturn;
        public Nullable<int> MaxLines;

        public string Text = null;

    }

    public class AnyUiComboBox : AnyUiControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public AnyUiThickness Padding;

        public bool? IsEditable;

        public List<object> Items = new List<object>();
        public string Text = null;

        public int? SelectedIndex;

    }

    public class AnyUiCheckBox : AnyUiContentControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public AnyUiThickness Padding;

        public string Content = null;

        public bool? IsChecked;

    }

    public class AnyUiButton : AnyUiContentControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public AnyUiThickness Padding;

        public string Content = null;
        public string ToolTip = null;

        public event RoutedEventHandler Click;

    }
}
