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
    public class AasCntlDisplayDataWpf : AasCntlDisplayDataBase
    {
        public AasCntlDisplayContextWpf Context;
        public UIElement WpfElement;
    }

    public class AasCntlDisplayContextWpf : AasCntlDisplayContextBase
    {
        public ModifyRepo ModifyRepo;

        public AasCntlDisplayContextWpf(ModifyRepo modifyRepo)
        {
            ModifyRepo = modifyRepo;
        }

        public void RenderWpfElement<W>(AasCntlUIElement aasCntl)
            where W : UIElement, new()
        {
            // access
            var dd = aasCntl?.DisplayData as AasCntlDisplayDataWpf;
            if (dd == null)
                return;

            // create wpfElement accordingly
            if (dd.WpfElement == null)
                dd.WpfElement = new W();

            //
            // differentiate types
            // (the double-if statement serves for an encapsulation of local names)
            //
            if (typeof(W) == typeof(FrameworkElement))
            {
                if (aasCntl is AasCntlFrameworkElement cntl && dd.WpfElement is FrameworkElement wpf)
                {
                    RenderWpfElement<UIElement>(aasCntl);
                    if (cntl.Margin != null)
                        wpf.Margin = cntl.Margin.GetWpfTickness();
                    if (cntl.VerticalAlignment.HasValue)
                        wpf.VerticalAlignment = cntl.VerticalAlignment.Value;
                    if (cntl.HorizontalAlignment.HasValue)
                        wpf.HorizontalAlignment = cntl.HorizontalAlignment.Value;
                    if (cntl.MinHeight.HasValue)
                        wpf.MinHeight = cntl.MinHeight.Value;
                    if (cntl.MinWidth.HasValue)
                        wpf.MinWidth = cntl.MinWidth.Value;
                    if (cntl.MaxHeight.HasValue)
                        wpf.MaxHeight = cntl.MaxHeight.Value;
                    if (cntl.MaxWidth.HasValue)
                        wpf.MaxWidth = cntl.MaxWidth.Value;
                    wpf.Tag = cntl.Tag;
                }
            }
            else
            if (typeof(W) == typeof(Control))
            {
                // manual base class
                RenderWpfElement<FrameworkElement>(aasCntl);
                // this class
                if (aasCntl is AasCntlControl cntl && dd.WpfElement is Control wpf)
                {
                    if (cntl.VerticalContentAlignment.HasValue)
                        wpf.VerticalContentAlignment = cntl.VerticalContentAlignment.Value;
                    if (cntl.HorizontalContentAlignment.HasValue)
                        wpf.HorizontalContentAlignment = cntl.HorizontalContentAlignment.Value;
                }
            }
            else
            if (typeof(W) == typeof(ContentControl))
            {
                // manual base class
                RenderWpfElement<Control>(aasCntl);
                // this class
                if (aasCntl is AasCntlContentControl cntl && dd.WpfElement is ContentControl wpf)
                {
                }
            }
            else
            if (typeof(W) == typeof(Decorator))
            {
                // manual base class
                RenderWpfElement<FrameworkElement>(aasCntl);
                // this class
                if (aasCntl is AasCntlDecorator cntl && dd.WpfElement is Decorator wpf)
                {
                }
            }
            else
            if (typeof(W) == typeof(Grid) || typeof(W) == typeof(StackPanel) || typeof(W) == typeof(WrapPanel))
            {
                // manual base class
                RenderWpfElement<FrameworkElement>(aasCntl);

                // the Panel super class (is abstract, therefore not able to use recursion via T)
                if (aasCntl is AasCntlPanel cntl && dd.WpfElement is Panel wpf)
                {
                    // normal members
                    if (cntl.Background != null)
                        wpf.Background = cntl.Background.GetWpfBrush();

                    // children
                    wpf.Children.Clear();
                    if (cntl.Children != null)
                        foreach (var ce in cntl.Children)
                            wpf.Children.Add(ce.GetOrCreateWpfElement());
                }


                if (typeof(W) == typeof(Grid))
                {
                    // manual base class
                    RenderWpfElement<Panel>(aasCntl);
                    // this class
                    if (aasCntl is AasCntlGrid cntl && dd.WpfElement is Grid wpf)
                    {
                        if (cntl.RowDefinitions != null)
                            foreach (var rd in cntl.RowDefinitions)
                                wpf.RowDefinitions.Add(rd.GetWpfRowDefinition());

                        if (cntl.ColumnDefinitions != null)
                            foreach (var cd in cntl.ColumnDefinitions)
                                wpf.ColumnDefinitions.Add(cd.GetWpfColumnDefinition());

                        // make sure to target only already realized children
                        foreach (var cel in cntl.Children)
                        {
                            var celwpf = cel.GetOrCreateWpfElement();
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
                }

                if (typeof(W) == typeof(StackPanel))
                {
                    // manual base class
                    RenderWpfElement<Panel>(aasCntl);
                    // this class
                    if (aasCntl is AasCntlStackPanel cntl && dd.WpfElement is StackPanel wpf)
                    {
                        if (cntl.Orientation.HasValue)
                            wpf.Orientation = cntl.Orientation.Value;
                    }
                }

                if (typeof(W) == typeof(WrapPanel))
                {
                    // manual base class
                    RenderWpfElement<Panel>(aasCntl);
                    // this class
                    if (aasCntl is AasCntlWrapPanel cntl && dd.WpfElement is WrapPanel wpf)
                    {
                        if (cntl.Orientation.HasValue)
                            wpf.Orientation = cntl.Orientation.Value;
                    }
                }
            }
            else
            if (typeof(W) == typeof(Border))
            {
                // manual base class
                RenderWpfElement<Decorator>(aasCntl);
                // this class
                if (aasCntl is AasCntlBorder cntl && dd.WpfElement is Border wpf)
                {
                    if (cntl.Background != null)
                        wpf.Background = cntl.Background.GetWpfBrush();
                    if (cntl.BorderThickness != null)
                        wpf.BorderThickness = cntl.BorderThickness.GetWpfTickness();
                    if (cntl.BorderBrush != null)
                        wpf.BorderBrush = cntl.BorderBrush.GetWpfBrush();
                    if (cntl.Padding != null)
                        wpf.Padding = cntl.Padding.GetWpfTickness();
                }
            }
            else
            if (typeof(W) == typeof(Label))
            {
                // manual base class
                RenderWpfElement<ContentControl>(aasCntl);
                // this class
                if (aasCntl is AasCntlLabel cntl && dd.WpfElement is Label wpf)
                {
                    if (cntl.Background != null)
                        wpf.Background = cntl.Background.GetWpfBrush();
                    if (cntl.Foreground != null)
                        wpf.Foreground = cntl.Foreground.GetWpfBrush();
                    if (cntl.FontWeight != null)
                        wpf.FontWeight = cntl.FontWeight.Value;
                    if (cntl.Padding != null)
                        wpf.Padding = cntl.Padding.GetWpfTickness();
                    wpf.Content = cntl.Content;
                }
            }
            else
            if (typeof(W) == typeof(TextBlock))
            {
                // manual base class
                RenderWpfElement<FrameworkElement>(aasCntl);
                // this class
                if (aasCntl is AasCntlTextBlock cntl && dd.WpfElement is TextBlock wpf)
                {
                    if (cntl.Background != null)
                        wpf.Background = cntl.Background;
                    if (cntl.Foreground != null)
                        wpf.Foreground = cntl.Foreground;
                    if (cntl.FontWeight != null)
                        wpf.FontWeight = cntl.FontWeight.Value;
                    if (cntl.Padding != null)
                        wpf.Padding = cntl.Padding.GetWpfTickness();
                    wpf.Text = cntl.Text;
                }
            }
            else
            if (typeof(W) == typeof(HintBubble))
            {
                // manual base class
                RenderWpfElement<TextBox>(aasCntl);
                // this class
                if (aasCntl is AasCntlHintBubble cntl && dd.WpfElement is HintBubble wpf)
                {
                    if (cntl.Background != null)
                        wpf.Background = cntl.Background;
                    if (cntl.Foreground != null)
                        wpf.Foreground = cntl.Foreground;
                    if (cntl.Padding != null)
                        wpf.Padding = cntl.Padding.GetWpfTickness();
                    wpf.Text = cntl.Text;
                }
            }
            else
            if (typeof(W) == typeof(TextBox))
            {
                // manual base class
                RenderWpfElement<Control>(aasCntl);
                // this class
                if (aasCntl is AasCntlTextBox cntl && dd.WpfElement is TextBox wpf)
                {
                    if (cntl.Background != null)
                        wpf.Background = cntl.Background;
                    if (cntl.Foreground != null)
                        wpf.Foreground = cntl.Foreground;
                    if (cntl.Padding != null)
                        wpf.Padding = cntl.Padding.GetWpfTickness();
                    wpf.VerticalScrollBarVisibility = cntl.VerticalScrollBarVisibility;
                    wpf.AcceptsReturn = cntl.AcceptsReturn;
                    if (cntl.MaxLines != null)
                        wpf.MaxLines = cntl.MaxLines.Value;
                    wpf.Text = cntl.Text;
                }
            }
            else
            if (typeof(W) == typeof(ComboBox))
            {
                // manual base class
                RenderWpfElement<Control>(aasCntl);
                // this class
                if (aasCntl is AasCntlComboBox cntl && dd.WpfElement is ComboBox wpf)
                {
                    if (cntl.Background != null)
                        wpf.Background = cntl.Background;
                    if (cntl.Foreground != null)
                        wpf.Foreground = cntl.Foreground;
                    if (cntl.Padding != null)
                        wpf.Padding = cntl.Padding.GetWpfTickness();
                    if (cntl.IsEditable.HasValue)
                        wpf.IsEditable = cntl.IsEditable.Value;

                    if (cntl.Items != null)
                        foreach (var i in cntl.Items)
                            wpf.Items.Add(i);

                    wpf.Text = cntl.Text;
                    if (cntl.SelectedIndex.HasValue)
                        wpf.SelectedIndex = cntl.SelectedIndex.Value;
                }
            }
            else
            if (typeof(W) == typeof(CheckBox))
            {
                // manual base class
                RenderWpfElement<Control>(aasCntl);
                // this class
                if (aasCntl is AasCntlCheckBox cntl && dd.WpfElement is CheckBox wpf)
                {
                    if (cntl.Background != null)
                        wpf.Background = cntl.Background;
                    if (cntl.Foreground != null)
                        wpf.Foreground = cntl.Foreground;
                    if (cntl.IsChecked.HasValue)
                        wpf.IsChecked = cntl.IsChecked.Value;
                    if (cntl.Padding != null)
                        wpf.Padding = cntl.Padding.GetWpfTickness();
                    wpf.Content = cntl.Content;
                }
            }
            else
            if (typeof(W) == typeof(Button))
            {
                // manual base class
                RenderWpfElement<Control>(aasCntl);
                // this class
                if (aasCntl is AasCntlButton cntl && dd.WpfElement is Button wpf)
                {
                    if (cntl.Background != null)
                        wpf.Background = cntl.Background;
                    if (cntl.Foreground != null)
                        wpf.Foreground = cntl.Foreground;
                    if (cntl.Padding != null)
                        wpf.Padding = cntl.Padding.GetWpfTickness();
                    wpf.Content = cntl.Content;
                    wpf.ToolTip = cntl.ToolTip;
                }
            }
            else
            {
                throw new Exception("RenderWpfElement: type " + typeof(W).ToString() + " not implemented!");
            }
        }

        public void UIElementWasRendered(AasCntlUIElement aasCntl, UIElement el)
        {
            // ModifyRepo works on fwElems ..
            if (ModifyRepo != null && aasCntl is AasCntlFrameworkElement aasCntlFe && el is FrameworkElement elFe)
            {
                ModifyRepo.ActivateAasCntl(aasCntlFe, elFe);
            }
        }
    }
}
