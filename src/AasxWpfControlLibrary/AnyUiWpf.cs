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
using AasxPackageExplorer;
using AdminShellNS;
using AasxWpfControlLibrary;
using AnyUi.AAS;

namespace AnyUi
{
    public class AnyUiDisplayDataWpf : AnyUiDisplayDataBase
    {
        public AnyUiDisplayContextWpf Context;
        public UIElement WpfElement;
        public int actiCnt = 0;

        public AnyUiDisplayDataWpf(AnyUiDisplayContextWpf Context)
        {
            this.Context = Context;
        }
    }

    public class AnyUiDisplayContextWpf : AnyUiContextBase
    {
        public ModifyRepo ModifyRepo;
        public IFlyoutProvider FlyoutProvider;
        public PackageCentral Packages;

        public AnyUiDisplayContextWpf(
            ModifyRepo modifyRepo, 
            IFlyoutProvider flyoutProvider, PackageCentral packages)
        {
            ModifyRepo = modifyRepo;
            FlyoutProvider = flyoutProvider;
            Packages = packages;
            InitRenderRecs();
        }

        public Brush GetWpfBrush(AnyUiBrush br)
        {
            if (br == null)
                return Brushes.Transparent;
            var c = br.Color;
            return new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
        }

        public AnyUiColor GetAnyUiColor(Color c)
        {
            if (c == null)
                return AnyUiColors.Transparent;
            return AnyUiColor.FromArgb(c.A, c.R, c.G, c.B);
        }

        public AnyUiBrush GetAnyUiBrush(SolidColorBrush br)
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

        private class RenderRec
        {
            public Type CntlType;
            public Type WpfType;
            public Action<AnyUiUIElement, UIElement> InitLambda;

            public RenderRec(Type cntlType, Type wpfType, Action<AnyUiUIElement, UIElement> initLambda = null)
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
                new RenderRec(typeof(AnyUiFrameworkElement), typeof(FrameworkElement), (a, b) =>
                {
                    if (a is AnyUiFrameworkElement cntl && b is FrameworkElement wpf)
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
                    }
                }),

                new RenderRec(typeof(AnyUiControl), typeof(Control), (a, b) =>
                {
                   if (a is AnyUiControl cntl && b is Control wpf)
                   {
                       if (cntl.VerticalContentAlignment.HasValue)
                           wpf.VerticalContentAlignment = 
                            (VerticalAlignment)((int) cntl.VerticalContentAlignment.Value);
                       if (cntl.HorizontalContentAlignment.HasValue)
                           wpf.HorizontalContentAlignment = 
                            (HorizontalAlignment)((int) cntl.HorizontalContentAlignment.Value);
                   }
                }),

                new RenderRec(typeof(AnyUiContentControl), typeof(ContentControl), (a, b) =>
                {
                   if (a is AnyUiContentControl cntl && b is ContentControl wpf)
                   {
                   }
                }),

                new RenderRec(typeof(AnyUiDecorator), typeof(Decorator), (a, b) =>
                {
                   if (a is AnyUiDecorator cntl && b is Decorator wpf)
                   {
                   }
                }),

                new RenderRec(typeof(AnyUiPanel), typeof(Panel), (a, b) =>
                {
                   if (a is AnyUiPanel cntl && b is Panel wpf)
                   {
                       // normal members
                       if (cntl.Background != null)
                           wpf.Background = GetWpfBrush(cntl.Background);

                       // children
                       wpf.Children.Clear();
                       if (cntl.Children != null)
                           foreach (var ce in cntl.Children)
                               wpf.Children.Add(GetOrCreateWpfElement(ce));
                   }
                }),

                new RenderRec(typeof(AnyUiGrid), typeof(Grid), (a, b) =>
                {
                   if (a is AnyUiGrid cntl && b is Grid wpf)
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

                new RenderRec(typeof(AnyUiStackPanel), typeof(StackPanel), (a, b) =>
                {
                   if (a is AnyUiStackPanel cntl && b is StackPanel wpf)
                   {
                       if (cntl.Orientation.HasValue)
                           wpf.Orientation = (Orientation)((int) cntl.Orientation.Value);
                   }
                }),

                new RenderRec(typeof(AnyUiWrapPanel), typeof(WrapPanel), (a, b) =>
                {
                   if (a is AnyUiWrapPanel cntl && b is WrapPanel wpf)
                   {
                       if (cntl.Orientation.HasValue)
                           wpf.Orientation = (Orientation)((int) cntl.Orientation.Value);
                   }
                }),

                new RenderRec(typeof(AnyUiBorder), typeof(Border), (a, b) =>
                {
                   if (a is AnyUiBorder cntl && b is Border wpf)
                   {
                       if (cntl.Background != null)
                           wpf.Background = GetWpfBrush(cntl.Background);
                       if (cntl.BorderThickness != null)
                           wpf.BorderThickness = GetWpfTickness(cntl.BorderThickness);
                       if (cntl.BorderBrush != null)
                           wpf.BorderBrush = GetWpfBrush(cntl.BorderBrush);
                       if (cntl.Padding != null)
                           wpf.Padding = GetWpfTickness(cntl.Padding);
                   }
                }),

                new RenderRec(typeof(AnyUiLabel), typeof(Label), (a, b) =>
                {
                   if (a is AnyUiLabel cntl && b is Label wpf)
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

                new RenderRec(typeof(AnyUiTextBlock), typeof(TextBlock), (a, b) =>
                {
                   if (a is AnyUiTextBlock cntl && b is TextBlock wpf)
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
                        if (cntl.FontWeight.HasValue)
                            wpf.FontWeight = GetFontWeight(cntl.FontWeight.Value);
                       wpf.Text = cntl.Text;
                   }
                }),

                new RenderRec(typeof(AnyUiHintBubble), typeof(HintBubble), (a, b) =>
                {
                   if (a is AnyUiHintBubble cntl && b is HintBubble wpf)
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

                new RenderRec(typeof(AnyUiTextBox), typeof(TextBox), (a, b) =>
                {
                   if (a is AnyUiTextBox cntl && b is TextBox wpf)
                   {
                       if (cntl.Background != null)
                           wpf.Background = GetWpfBrush(cntl.Background);
                       if (cntl.Foreground != null)
                           wpf.Foreground = GetWpfBrush(cntl.Foreground);
                       if (cntl.Padding != null)
                           wpf.Padding = GetWpfTickness(cntl.Padding);
                       wpf.VerticalScrollBarVisibility = (ScrollBarVisibility)((int) cntl.VerticalScrollBarVisibility);
                       wpf.AcceptsReturn = cntl.AcceptsReturn;
                       if (cntl.MaxLines != null)
                           wpf.MaxLines = cntl.MaxLines.Value;
                       wpf.Text = cntl.Text;
                   }
                }),

                new RenderRec(typeof(AnyUiComboBox), typeof(ComboBox), (a, b) =>
                {
                   if (a is AnyUiComboBox cntl && b is ComboBox wpf)
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
                           // wpf.Items.Clear();
                           foreach (var i in cntl.Items)
                               wpf.Items.Add(i);
                       }

                       wpf.Text = cntl.Text;
                       if (cntl.SelectedIndex.HasValue)
                           wpf.SelectedIndex = cntl.SelectedIndex.Value;
                   }
                }),

                new RenderRec(typeof(AnyUiCheckBox), typeof(CheckBox), (a, b) =>
                {
                   if (a is AnyUiCheckBox cntl && b is CheckBox wpf)
                   {
                       if (cntl.Background != null)
                           wpf.Background = GetWpfBrush(cntl.Background);
                       if (cntl.Foreground != null)
                           wpf.Foreground = GetWpfBrush(cntl.Foreground);
                       if (cntl.IsChecked.HasValue)
                           wpf.IsChecked = cntl.IsChecked.Value;
                       if (cntl.Padding != null)
                           wpf.Padding = GetWpfTickness(cntl.Padding);
                       wpf.Content = cntl.Content;
                   }
                }),

                new RenderRec(typeof(AnyUiButton), typeof(Button), (a, b) =>
                {
                   if (a is AnyUiButton cntl && b is Button wpf)
                   {
                       if (cntl.Background != null)
                           wpf.Background = GetWpfBrush(cntl.Background);
                       if (cntl.Foreground != null)
                           wpf.Foreground = GetWpfBrush(cntl.Foreground);
                       if (cntl.Padding != null)
                           wpf.Padding = GetWpfTickness(cntl.Padding);
                       wpf.Content = cntl.Content;
                       wpf.ToolTip = cntl.ToolTip;
                   }
                })
            });
        }

        public UIElement GetOrCreateWpfElement(
            AnyUiUIElement AnyUi,
            Type superType = null,
            bool allowCreate = true)
        {
            // access
            if (AnyUi == null)
                return null;

            // have data attached to cntl
            if (AnyUi.DisplayData == null)
                AnyUi.DisplayData = new AnyUiDisplayDataWpf(this);
            var dd = AnyUi?.DisplayData as AnyUiDisplayDataWpf;
            if (dd == null)
                return null;

            // most specialized class or in recursion/ creation of base classes?
            var topClass = superType == null;

            // return, if already created and not (still) in recursion/ creation of base classes
            if (dd.WpfElement != null && topClass)
                return dd.WpfElement;
            if (!allowCreate)
                return null;

            // identify render rec
            var searchType = (superType != null) ? superType : AnyUi.GetType();
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
            if (dd.WpfElement == null && topClass)
                dd.WpfElement = (UIElement) Activator.CreateInstance(foundRR.WpfType);
            if (dd.WpfElement == null)
                return null;

            // recurse (first)
            var bt = searchType.BaseType;
            if (bt != null)
                GetOrCreateWpfElement(AnyUi, superType: bt);

            // perform the render action (for this level of attributes, second)
            foundRR.InitLambda?.Invoke(AnyUi, dd.WpfElement);

            // call action
            if (topClass)
            {
                UIElementWasRendered(AnyUi, dd.WpfElement);
            }

            // result
            return dd.WpfElement;
        }

        public void UIElementWasRendered(AnyUiUIElement AnyUi, UIElement el)
        {
            // ModifyRepo works on fwElems ..
            if (ModifyRepo != null && AnyUi is AnyUiFrameworkElement AnyUiFe && el is FrameworkElement elFe)
            {
                if (AnyUi.DisplayData is AnyUiDisplayDataWpf dd)
                    dd.actiCnt++;

                ModifyRepo.ActivateAnyUi(AnyUiFe, elFe);
            }
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

        /// <summary>
        /// Show selected dialogues hardware-independent. The technology implementation will show the
        /// dialogue based on the type of provided <c>dialogueData</c>. 
        /// </summary>
        /// <param name="dialogueData"></param>
        /// <returns>If the dialogue was end with "OK" or similar success.</returns>
        public override bool StartModalDialogue(AnyUiDialogueDataBase dialogueData)
        {
            // access
            if (dialogueData == null || FlyoutProvider == null)
                return false;

            // make sure to reset
            dialogueData.Result = false;

            // beware of exceptions
            try
            {

                // TODO (MIHO, 2020-12-21): can be realized without tedious central dispatch?
                if (dialogueData is AnyUiDialogueDataTextBox ddtb)
                {
                    var uc = new TextBoxFlyout();
                    uc.DiaData = ddtb;
                    FlyoutProvider.StartFlyoverModal(uc);
                }

                if (dialogueData is AnyUiDialogueDataSelectEclassEntity ddec)
                {
                    var uc = new SelectEclassEntityFlyout();
                    uc.DiaData = ddec;
                    FlyoutProvider.StartFlyoverModal(uc);
                }

                if (dialogueData is AnyUiDialogueDataTextEditor ddte)
                {
                    var uc = new TextEditorFlyout();
                    uc.DiaData = ddte;
                    FlyoutProvider.StartFlyoverModal(uc);
                }

                if (dialogueData is AnyUiDialogueDataSelectAasEntity ddsa)
                {
                    var uc = new SelectAasEntityFlyout(Packages);
                    uc.DiaData = ddsa;
                    FlyoutProvider.StartFlyoverModal(uc);
                }

                if (dialogueData is AnyUiDialogueDataSelectReferableFromPool ddrf)
                {
                    var uc = new SelectFromReferablesPoolFlyout(AasxPredefinedConcepts.DefinitionsPool.Static);
                    uc.DiaData = ddrf;
                    FlyoutProvider.StartFlyoverModal(uc);
                }

                if (dialogueData is AnyUiDialogueDataSelectQualifierPreset ddsq)
                {
                    var fullfn = System.IO.Path.GetFullPath(Options.Curr.QualifiersFile);
                    var uc = new SelectQualifierPresetFlyout(fullfn);
                    uc.DiaData = ddsq;
                    FlyoutProvider.StartFlyoverModal(uc);
                }

            } catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"while performing AnyUI dialogue {dialogueData.GetType().ToString()}");
            }

            // result
            return dialogueData.Result;
        }
    }
}
