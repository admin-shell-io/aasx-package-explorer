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

        /*
        public UIElement GetOrCreateWpfElementTemplated<T>(bool allowCreate = true) where T : UIElement, new()
        {
            if (wpfElement is T || !allowCreate)
                return wpfElement;
            wpfElement = new T();
            this.RenderUIElement(wpfElement);
            //if (display is AasCntlDisplayContextWpf displayWpf)
            //    displayWpf.UIElementWasRendered(this, wpfElement);
            return wpfElement;
        }

        public virtual UIElement GetOrCreateWpfElement(bool allowCreate = true)
        {
            return GetOrCreateWpfElementTemplated<UIElement>(allowCreate);
        }
        */
    }

    public class AasCntlFrameworkElement : AasCntlUIElement
    {
        public AasCntlThickness Margin;
        public VerticalAlignment? VerticalAlignment;
        public HorizontalAlignment? HorizontalAlignment;

        public double? MinHeight;
        public double? MinWidth;
        public double? MaxHeight;
        public double? MaxWidth;

        public object Tag = null;

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is FrameworkElement fe)
            {
                if (this.Margin != null)
                    fe.Margin = this.Margin.GetWpfTickness();
                if (this.VerticalAlignment.HasValue)
                    fe.VerticalAlignment = this.VerticalAlignment.Value;
                if (this.HorizontalAlignment.HasValue)
                    fe.HorizontalAlignment = this.HorizontalAlignment.Value;
                if (this.MinHeight.HasValue)
                    fe.MinHeight = this.MinHeight.Value;
                if (this.MinWidth.HasValue)
                    fe.MinWidth = this.MinWidth.Value;
                if (this.MaxHeight.HasValue)
                    fe.MaxHeight = this.MaxHeight.Value;
                if (this.MaxWidth.HasValue)
                    fe.MaxWidth = this.MaxWidth.Value;
                fe.Tag = this.Tag;
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<FrameworkElement>();
        }
        */

    }

    public class AasCntlControl : AasCntlFrameworkElement
    {
        public VerticalAlignment? VerticalContentAlignment;
        public HorizontalAlignment? HorizontalContentAlignment;

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is Control co)
            {
                if (this.VerticalContentAlignment.HasValue)
                    co.VerticalContentAlignment = this.VerticalContentAlignment.Value;
                if (this.HorizontalContentAlignment.HasValue)
                    co.HorizontalContentAlignment = this.HorizontalContentAlignment.Value;
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<Control>();
        }
        */
    }

    public class AasCntlContentControl : AasCntlControl
    {
        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is ContentControl cc)
            {
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<ContentControl>();
        }
        */
    }

    public class AasCntlDecorator : AasCntlFrameworkElement
    {
        public virtual UIElement Child { get; set; }

        /*
        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<Decorator>();
        }
        */
    }    

    public class AasCntlPanel : AasCntlFrameworkElement
    {
        public AasCntlBrush Background;
        public List<AasCntlUIElement> Children = new List<AasCntlUIElement>();

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is Panel pan)
            {
                // normal members
                if (this.Background != null)
                    pan.Background = this.Background.GetWpfBrush();

                // children
                pan.Children.Clear();
                if (this.Children != null)
                    foreach (var ce in this.Children)
                        pan.Children.Add(ce.GetOrCreateWpfElement());
            }
        } 
        */
    }

    public class AasCntlGrid : AasCntlPanel
    {
        public List<AasCntlRowDefinition> RowDefinitions = new List<AasCntlRowDefinition>();
        public List<AasCntlColumnDefinition> ColumnDefinitions = new List<AasCntlColumnDefinition>();

        public static void SetRow(AasCntlUIElement el, int value) { if (el != null) el.GridRow = value; }        
        public static void SetRowSpan(AasCntlUIElement el, int value) { if (el != null) el.GridRowSpan = value; }
        public static void SetColumn(AasCntlUIElement el, int value) { if (el != null) el.GridColumn = value; }
        public static void SetColumnSpan(AasCntlUIElement el, int value) { if (el != null) el.GridColumnSpan = value; }

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is Grid sp)
            {
                if (this.RowDefinitions != null)
                    foreach (var rd in this.RowDefinitions)
                        sp.RowDefinitions.Add(rd.GetWpfRowDefinition());

                if (this.ColumnDefinitions != null)
                    foreach (var cd in this.ColumnDefinitions)
                        sp.ColumnDefinitions.Add(cd.GetWpfColumnDefinition());

                // make sure to target only already realized children
                foreach (var cel in this.Children)
                {
                    var celwpf = cel.GetOrCreateWpfElement();
                    if (sp.Children.Contains(celwpf))
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
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<Grid>();
        }
        */
    }

    public class AasCntlStackPanel : AasCntlPanel
    {
        public Orientation? Orientation;

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is StackPanel sp)
            {
                if (this.Orientation.HasValue)
                    sp.Orientation = this.Orientation.Value;
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<StackPanel>();
        }
        */
    }

    public class AasCntlWrapPanel : AasCntlPanel
    {
        public Orientation? Orientation;

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is WrapPanel sp)
            {
                if (this.Orientation.HasValue)
                    sp.Orientation = this.Orientation.Value;
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<WrapPanel>();
        }
        */
    }    

    public class AasCntlBorder : AasCntlDecorator
    {
        public AasCntlBrush Background = null;
        public AasCntlThickness BorderThickness;
        public AasCntlBrush BorderBrush = null;
        public AasCntlThickness Padding;

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is Border brd)
            {
                if (this.Background != null)
                    brd.Background = this.Background.GetWpfBrush();
                if (this.BorderThickness != null)
                    brd.BorderThickness = this.BorderThickness.GetWpfTickness();
                if (this.BorderBrush != null)
                    brd.BorderBrush = this.BorderBrush.GetWpfBrush();
                if (this.Padding != null)
                    brd.Padding = this.Padding.GetWpfTickness();
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<Border>();
        }
        */
    }

    public class AasCntlLabel : AasCntlContentControl
    {
        public AasCntlBrush Background;
        public AasCntlBrush Foreground;
        public AasCntlThickness Padding;

        public Nullable<FontWeight> FontWeight = null;
        public string Content = null;

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is Label lb)
            {
                if (this.Background != null)
                    lb.Background = this.Background.GetWpfBrush();
                if (this.Foreground != null)
                    lb.Foreground = this.Foreground.GetWpfBrush();
                if (this.FontWeight != null)
                    lb.FontWeight = this.FontWeight.Value;
                if (this.Padding != null)
                    lb.Padding = this.Padding.GetWpfTickness();
                lb.Content = this.Content;
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<Label>();
        }
        */
    }

    public class AasCntlTextBlock : AasCntlFrameworkElement
    {
        public Brush Background;
        public Brush Foreground;
        public AasCntlThickness Padding;

        public Nullable<FontWeight> FontWeight = null;
        public string Text = null;

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is TextBlock tb)
            {
                if (this.Background != null)
                    tb.Background = this.Background;
                if (this.Foreground != null)
                    tb.Foreground = this.Foreground;
                if (this.FontWeight != null)
                    tb.FontWeight = this.FontWeight.Value;
                if (this.Padding != null)
                    tb.Padding = this.Padding.GetWpfTickness();
                tb.Text = this.Text;
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<TextBlock>();
        }
        */
    }

    public class AasCntlHintBubble : AasCntlTextBox
    {
        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is HintBubble hb)
            {
                if (this.Background != null)
                    hb.Background = this.Background;
                if (this.Foreground != null)
                    hb.Foreground = this.Foreground;
                if (this.Padding != null)
                    hb.Padding = this.Padding.GetWpfTickness();
                hb.Text = this.Text;
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<HintBubble>();
        }
        */
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

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is TextBox tb)
            {
                if (this.Background != null)
                    tb.Background = this.Background;
                if (this.Foreground != null)
                    tb.Foreground = this.Foreground;
                if (this.Padding != null)
                    tb.Padding = this.Padding.GetWpfTickness();
                tb.VerticalScrollBarVisibility = this.VerticalScrollBarVisibility;
                tb.AcceptsReturn = this.AcceptsReturn;
                if (this.MaxLines != null)
                    tb.MaxLines = this.MaxLines.Value;
                tb.Text = this.Text;
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<TextBox>();
        }
        */
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

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is ComboBox cb)
            {
                if (this.Background != null)
                    cb.Background = this.Background;
                if (this.Foreground != null)
                    cb.Foreground = this.Foreground;
                if (this.Padding != null)
                    cb.Padding = this.Padding.GetWpfTickness();
                if (this.IsEditable.HasValue)
                    cb.IsEditable = this.IsEditable.Value;

                if (this.Items != null)
                    foreach (var i in this.Items)
                        cb.Items.Add(i);
                
                cb.Text = this.Text;
                if (this.SelectedIndex.HasValue)
                    cb.SelectedIndex = this.SelectedIndex.Value;
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<ComboBox>();
        }
        */
    }

    public class AasCntlCheckBox : AasCntlContentControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public AasCntlThickness Padding;

        public string Content = null;

        public bool? IsChecked;

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is CheckBox cb)
            {
                if (this.Background != null)
                    cb.Background = this.Background;
                if (this.Foreground != null)
                    cb.Foreground = this.Foreground;
                if (this.IsChecked.HasValue)
                    cb.IsChecked = this.IsChecked.Value;
                if (this.Padding != null)
                    cb.Padding = this.Padding.GetWpfTickness();
                cb.Content = this.Content;
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<CheckBox>();
        }
        */
    }

    public class AasCntlButton : AasCntlContentControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public AasCntlThickness Padding;

        public string Content = null;
        public string ToolTip = null;

        public event RoutedEventHandler Click;

        /*
        public override void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is Button btn)
            {
                if (this.Background != null)
                    btn.Background = this.Background;
                if (this.Foreground != null)
                    btn.Foreground = this.Foreground;
                if (this.Padding != null)
                    btn.Padding = this.Padding.GetWpfTickness();
                btn.Content = this.Content;
                btn.ToolTip = this.ToolTip;
            }
        }

        public override UIElement GetOrCreateWpfElement()
        {
            return GetOrCreateWpfElementTemplated<Button>();
        }
        */
    }
}
