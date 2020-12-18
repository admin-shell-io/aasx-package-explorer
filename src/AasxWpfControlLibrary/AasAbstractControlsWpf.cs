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

        public AasCntlDisplayDataWpf(AasCntlDisplayContextWpf Context)
        {
            this.Context = Context;
        }
    }

    public class AasCntlDisplayContextWpf : AasCntlDisplayContextBase
    {
        public ModifyRepo ModifyRepo;

        public AasCntlDisplayContextWpf(ModifyRepo modifyRepo)
        {
            ModifyRepo = modifyRepo;
            InitRenderRecs();
        }

        private class RenderRec
        {
            public Type CntlType;
            public Type WpfType;
            public Action<AasCntlUIElement, UIElement> InitLambda;

            public RenderRec(Type cntlType, Type wpfType, Action<AasCntlUIElement, UIElement> initLambda = null)
            {
                CntlType = cntlType;
                WpfType = wpfType;
                InitLambda = initLambda;
            }
        }

        private List<RenderRec> RenderRecs = new List<RenderRec>();

        private void InitRenderRecs()
        {
            RenderRecs.Clear();
            RenderRecs.AddRange(new[]
            {
                new RenderRec(typeof(AasCntlFrameworkElement), typeof(FrameworkElement), (a, b) =>
                {
                    if (a is AasCntlFrameworkElement cntl && b is FrameworkElement wpf)
                    {
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
                }),

                new RenderRec(typeof(AasCntlControl), typeof(Control), (a, b) =>
                {
                   if (a is AasCntlControl cntl && b is Control wpf)
                   {
                       if (cntl.VerticalContentAlignment.HasValue)
                           wpf.VerticalContentAlignment = cntl.VerticalContentAlignment.Value;
                       if (cntl.HorizontalContentAlignment.HasValue)
                           wpf.HorizontalContentAlignment = cntl.HorizontalContentAlignment.Value;
                   }
                }),

                new RenderRec(typeof(AasCntlContentControl), typeof(ContentControl), (a, b) =>
                {
                   if (a is AasCntlContentControl cntl && b is ContentControl wpf)
                   {
                   }
                }),

                new RenderRec(typeof(AasCntlDecorator), typeof(Decorator), (a, b) =>
                {
                   if (a is AasCntlDecorator cntl && b is Decorator wpf)
                   {
                   }
                }),

                new RenderRec(typeof(AasCntlPanel), typeof(Panel), (a, b) =>
                {
                   if (a is AasCntlPanel cntl && b is Panel wpf)
                   {
                       // normal members
                       if (cntl.Background != null)
                           wpf.Background = cntl.Background.GetWpfBrush();

                       // children
                       wpf.Children.Clear();
                       if (cntl.Children != null)
                           foreach (var ce in cntl.Children)
                               wpf.Children.Add(GetOrCreateWpfElement(ce));
                   }
                }),

                new RenderRec(typeof(AasCntlGrid), typeof(Grid), (a, b) =>
                {
                   if (a is AasCntlGrid cntl && b is Grid wpf)
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

                new RenderRec(typeof(AasCntlStackPanel), typeof(StackPanel), (a, b) =>
                {
                   if (a is AasCntlStackPanel cntl && b is StackPanel wpf)
                   {
                       if (cntl.Orientation.HasValue)
                           wpf.Orientation = cntl.Orientation.Value;
                   }
                }),

                new RenderRec(typeof(AasCntlWrapPanel), typeof(WrapPanel), (a, b) =>
                {
                   if (a is AasCntlWrapPanel cntl && b is WrapPanel wpf)
                   {
                       if (cntl.Orientation.HasValue)
                           wpf.Orientation = cntl.Orientation.Value;
                   }
                }),

                new RenderRec(typeof(AasCntlBorder), typeof(Border), (a, b) =>
                {
                   if (a is AasCntlBorder cntl && b is Border wpf)
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
                }),

                new RenderRec(typeof(AasCntlLabel), typeof(Label), (a, b) =>
                {
                   if (a is AasCntlLabel cntl && b is Label wpf)
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
                }),

                new RenderRec(typeof(AasCntlTextBlock), typeof(TextBlock), (a, b) =>
                {
                   if (a is AasCntlTextBlock cntl && b is TextBlock wpf)
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
                }),

                new RenderRec(typeof(AasCntlHintBubble), typeof(HintBubble), (a, b) =>
                {
                   if (a is AasCntlHintBubble cntl && b is HintBubble wpf)
                   {
                       if (cntl.Background != null)
                           wpf.Background = cntl.Background;
                       if (cntl.Foreground != null)
                           wpf.Foreground = cntl.Foreground;
                       if (cntl.Padding != null)
                           wpf.Padding = cntl.Padding.GetWpfTickness();
                       wpf.Text = cntl.Text;
                   }
                }),

                new RenderRec(typeof(AasCntlTextBox), typeof(TextBox), (a, b) =>
                {
                   if (a is AasCntlTextBox cntl && b is TextBox wpf)
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
                }),

                new RenderRec(typeof(AasCntlComboBox), typeof(ComboBox), (a, b) =>
                {
                   if (a is AasCntlComboBox cntl && b is ComboBox wpf)
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
                       {
                           // wpf.Items.Clear();
                           foreach (var i in cntl.Items)
                               wpf.Items.Add(i);
                       }

                       wpf.Text = cntl.Text;
                       if (cntl.SelectedIndex.HasValue)
                           wpf.SelectedIndex = cntl.SelectedIndex.Value;
                   }
                }),

                new RenderRec(typeof(AasCntlCheckBox), typeof(CheckBox), (a, b) =>
                {
                   if (a is AasCntlCheckBox cntl && b is CheckBox wpf)
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
                }),

                new RenderRec(typeof(AasCntlButton), typeof(Button), (a, b) =>
                {
                   if (a is AasCntlButton cntl && b is Button wpf)
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
                })
            });
        }

        public UIElement GetOrCreateWpfElement(
            AasCntlUIElement aasCntl,
            Type superType = null,
            bool allowCreate = true)
        {
            // access
            if (aasCntl == null)
                return null;

            // have data attached to cntl
            if (aasCntl.DisplayData == null)
                aasCntl.DisplayData = new AasCntlDisplayDataWpf(this);
            var dd = aasCntl?.DisplayData as AasCntlDisplayDataWpf;
            if (dd == null)
                return null;

            // only return
            if (!allowCreate)
                return dd.WpfElement;

            // identify render rec
            var searchType = (superType != null) ? superType : aasCntl.GetType();
            RenderRec foundRR = null;
            foreach (var rr in RenderRecs)
                if (rr?.CntlType == searchType)
                {
                    foundRR = rr;
                    break;
                }
            if (foundRR == null || foundRR.WpfType == null)
                return null;

            // create wpfElement accordingly?
            if (dd.WpfElement == null)
                dd.WpfElement = (UIElement) Activator.CreateInstance(foundRR.WpfType);
            if (dd.WpfElement == null)
                return null;

            // recurse (first)
            var bt = searchType.BaseType;
            if (bt != null)
                GetOrCreateWpfElement(aasCntl, superType: bt);

            // perform the render action (for this level of attributes, second)
            if (aasCntl is AasCntlComboBox cb && cb.Items.Count == 3)
                // TODO MIHO
                ;
            foundRR.InitLambda?.Invoke(aasCntl, dd.WpfElement);            

            // call action
            UIElementWasRendered(aasCntl, dd.WpfElement);

            // result
            return dd.WpfElement;
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
