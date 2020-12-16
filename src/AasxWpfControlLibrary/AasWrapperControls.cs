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
    public class AasCntlColumnDefinition : ColumnDefinition
    {
    }

    public class AasCntlRowDefinition : RowDefinition
    {
    }

    /*
    public class AasCntlGridLength : GridLength
    {
    }

    public class AasCntlThickness : Thickness
    {
    }
    */

    public class AasCntlUIElement
    {
        protected UIElement wpfElement = null;

        public virtual void RenderUIElement(UIElement el) { }

        public virtual UIElement GetWpfElement()
        {
            if (wpfElement is UIElement)
                return wpfElement;
            wpfElement = new UIElement();
            this.RenderUIElement(wpfElement);
            return wpfElement;
        }
    }

    public class AasCntlFrameworkElement : AasCntlUIElement
    {
        public Thickness Margin = new Thickness();
        public VerticalAlignment VerticalAlignment;
        public HorizontalAlignment HorizontalAlignment;

        public double? MinHeight = 0.0;
        public double? MinWidth = 0.0;
        public double? MaxHeight = 0.0;
        public double? MaxWidth = 0.0;

        public object Tag = null;

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is FrameworkElement fe)
            {
                fe.Margin = this.Margin;
                fe.VerticalAlignment = this.VerticalAlignment;   
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
       
        public virtual new UIElement GetWpfElement()
        {
            if (wpfElement is FrameworkElement)
                return wpfElement;
            wpfElement = new FrameworkElement();
            this.RenderUIElement(wpfElement);
            return wpfElement;
        }
    }

    public class AasCntlControl : AasCntlFrameworkElement
    {
        public VerticalAlignment VerticalContentAlignment;

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is Control co)
            {
                co.VerticalContentAlignment = this.VerticalContentAlignment;
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            if (wpfElement is Control)
                return wpfElement;
            wpfElement = new Control();
            this.RenderUIElement(wpfElement);
            return wpfElement;
        }
    }

    public class AasCntlContentControl : AasCntlControl
    {
        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is ContentControl cc)
            {
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            if (wpfElement is ContentControl)
                return wpfElement;
            wpfElement = new ContentControl();
            this.RenderUIElement(wpfElement);
            return wpfElement;
        }
    }

    public class AasCntlDecorator : AasCntlFrameworkElement
    {
        public virtual UIElement Child { get; set; }

        public virtual new UIElement GetWpfElement()
        {
            if (wpfElement is Decorator)
                return wpfElement;
            wpfElement = new Decorator();
            this.RenderUIElement(wpfElement);
            return wpfElement;
        }
    }    

    public class AasCntlPanel : AasCntlFrameworkElement
    {
        public Brush Background = null;
        public List<AasCntlUIElement> Children = new List<AasCntlUIElement>();

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is Panel pan)
            {
                // normal members
                pan.Background = this.Background;

                // children
                pan.Children.Clear();
                if (this.Children != null)
                    foreach (var ce in this.Children)
                        pan.Children.Add(ce.GetWpfElement());
            }
        }        
    }

    public class AasCntlGrid : AasCntlPanel
    {
        public List<RowDefinition> RowDefinitions = new List<RowDefinition>();
        public List<ColumnDefinition> ColumnDefinitions = new List<ColumnDefinition>();

        public static void SetRow(AasCntlUIElement element, int value) { }
        public static void SetRowSpan(AasCntlUIElement element, int value) { }
        public static void SetColumn(AasCntlUIElement element, int value) { }
        public static void SetColumnSpan(AasCntlUIElement element, int value) { }

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is Grid sp)
            {
                if (this.RowDefinitions != null)
                    foreach (var rd in this.RowDefinitions)
                        sp.RowDefinitions.Add(rd);

                if (this.ColumnDefinitions != null)
                    foreach (var cd in this.ColumnDefinitions)
                        sp.ColumnDefinitions.Add(cd);
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            if (wpfElement is Grid)
                return wpfElement;
            wpfElement = new Grid();
            this.RenderUIElement(wpfElement);
            return wpfElement;
        }
    }

    public class AasCntlStackPanel : AasCntlPanel
    {
        public Orientation Orientation = Orientation.Horizontal;

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is StackPanel sp)
            {
                sp.Orientation = this.Orientation;
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            if (wpfElement is StackPanel)
                return wpfElement;
            wpfElement = new StackPanel();
            this.RenderUIElement(wpfElement);
            return wpfElement;
        }
    }

    public class AasCntlWrapPanel : AasCntlPanel
    {
        public Orientation Orientation = Orientation.Horizontal;

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is WrapPanel sp)
            {
                sp.Orientation = this.Orientation;
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            var el = new WrapPanel();
            this.RenderUIElement(el);
            return el;
        }
    }    

    public class AasCntlBorder : AasCntlDecorator
    {
        public Brush Background = null;
        public Thickness BorderThickness = new Thickness();
        public Brush BorderBrush = null;
        public Thickness Padding = new Thickness();

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is Border brd)
            {
                brd.Background = this.Background;
                brd.BorderThickness = this.BorderThickness;
                brd.BorderBrush = this.BorderBrush;
                brd.Padding = this.Padding;
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            var el = new Border();
            this.RenderUIElement(el);
            return el;
        }
    }

    public class AasCntlLabel : AasCntlContentControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public Thickness Padding = new Thickness();

        public Nullable<FontWeight> FontWeight = null;
        public string Content = null;

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is Label lb)
            {
                lb.Background = this.Background;
                lb.Foreground = this.Foreground;
                if (this.FontWeight != null)
                    lb.FontWeight = this.FontWeight.Value;
                lb.Padding = this.Padding;
                lb.Content = this.Content;
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            var el = new Label();
            this.RenderUIElement(el);
            return el;
        }
    }

    public class AasCntlTextBlock : AasCntlFrameworkElement
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public Thickness Padding = new Thickness();

        public Nullable<FontWeight> FontWeight = null;
        public string Text = null;

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is TextBlock tb)
            {
                tb.Background = this.Background;
                tb.Foreground = this.Foreground;
                if (this.FontWeight != null)
                    tb.FontWeight = this.FontWeight.Value;
                tb.Padding = this.Padding;
                tb.Text = this.Text;
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            var el = new TextBlock();
            this.RenderUIElement(el);
            return el;
        }
    }

    public class AasCntlHintBubble : AasCntlTextBox
    {
        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is HintBubble hb)
            {
                hb.Background = this.Background;
                hb.Foreground = this.Foreground;
                hb.Padding = this.Padding;
                hb.Text = this.Text;
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            var el = new HintBubble();
            this.RenderUIElement(el);
            return el;
        }
    }

    public class AasCntlTextBox : AasCntlControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public Thickness Padding = new Thickness();

        public ScrollBarVisibility VerticalScrollBarVisibility;

        public bool AcceptsReturn;
        public Nullable<int> MaxLines;

        public string Text = null;

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is TextBox tb)
            {
                tb.Background = this.Background;
                tb.Foreground = this.Foreground;
                tb.Padding = this.Padding;
                tb.VerticalScrollBarVisibility = this.VerticalScrollBarVisibility;
                tb.AcceptsReturn = this.AcceptsReturn;
                if (this.MaxLines != null)
                    tb.MaxLines = this.MaxLines.Value;
                tb.Text = this.Text;
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            var el = new TextBox();
            this.RenderUIElement(el);
            return el;
        }
    }

    public class AasCntlComboBox : AasCntlControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public Thickness Padding = new Thickness();

        public bool? IsEditable;

        public List<object> Items = new List<object>();
        public string Text = null;

        public int? SelectedIndex;

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is ComboBox cb)
            {
                cb.Background = this.Background;
                cb.Foreground = this.Foreground;
                cb.Padding = this.Padding;
                if (this.IsEditable.HasValue)
                    cb.IsEditable = this.IsEditable.Value;
                cb.Text = this.Text;
                if (this.SelectedIndex.HasValue)
                    cb.SelectedIndex = this.SelectedIndex.Value;
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            var el = new ComboBox();
            this.RenderUIElement(el);
            return el;
        }
    }

    public class AasCntlCheckBox : AasCntlContentControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public Thickness Padding = new Thickness();

        public string Content = null;

        public bool? IsChecked;

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is CheckBox cb)
            {
                cb.Background = this.Background;
                cb.Foreground = this.Foreground;
                if (this.IsChecked.HasValue)
                    cb.IsChecked = this.IsChecked.Value;
                cb.Padding = this.Padding;
                cb.Content = this.Content;
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            var el = new CheckBox();
            this.RenderUIElement(el);
            return el;
        }
    }

    public class AasCntlButton : AasCntlContentControl
    {
        public Brush Background = null;
        public Brush Foreground = null;
        public Thickness Padding = new Thickness();

        public string Content = null;
        public string ToolTip = null;

        public event RoutedEventHandler Click;

        public virtual new void RenderUIElement(UIElement el)
        {
            base.RenderUIElement(el);
            if (el is Button btn)
            {
                btn.Background = this.Background;
                btn.Foreground = this.Foreground;
                btn.Padding = this.Padding;
                btn.Content = this.Content;
                btn.ToolTip = this.ToolTip;
            }
        }

        public virtual new UIElement GetWpfElement()
        {
            var el = new Button();
            this.RenderUIElement(el);
            return el;
        }
    }
}
