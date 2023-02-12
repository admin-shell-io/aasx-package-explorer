/*
Copyright (c) 2018-2022 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnyUi
{
    public class AnyUiSmallWidgetToolkit
    {
        /// <summary>
        /// Automatically replace labes starting with IRI with active links
        /// </summary>
        public bool showIriMode = false;

        public AnyUiGrid AddSmallGrid(int rows, int cols,
            string[] colWidths = null, string[] rowHeights = null,
            AnyUiThickness margin = null, AnyUiBrush background = null,
            AnyUiThickness padding = null)
        {
            var g = new AnyUiGrid();
            g.Margin = margin;
            if (padding != null)
                g.Padding = padding;
            if (background != null)
                g.Background = background;

            var cws = AnyUiListOfGridLength.Parse(colWidths);
            var rhs = AnyUiListOfGridLength.Parse(rowHeights);

            // Cols
            for (int ci = 0; ci < cols; ci++)
            {
                var gc = new AnyUiColumnDefinition();
                // default
                gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
                // width definition?
                if (cws != null && cws.Count > ci && cws[ci] != null)
                    gc.Width = cws[ci];
                // add
                g.ColumnDefinitions.Add(gc);
            }

            // Rows
            for (int ri = 0; ri < rows; ri++)
            {
                var gr = new AnyUiRowDefinition();
                // default
                gr.Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                // height definition?
                if (rhs != null && rhs.Count > ri && rhs[ri] != null)
                    gr.Height = rhs[ri];
                // add
                g.RowDefinitions.Add(gr);
            }

            return g;
        }

        public T AddSmallFrameworkElementTo<T>(
            AnyUiGrid g, int row, int col,
            T fe,
            AnyUiThickness margin = null,
            int? colSpan = null, int? rowSpan = null)
            where T : AnyUiFrameworkElement
        {
            if (margin != null)
                fe.Margin = margin;
            AnyUiGrid.SetRow(fe, row);
            AnyUiGrid.SetColumn(fe, col);
            if (rowSpan.HasValue)
                AnyUiGrid.SetRowSpan(fe, rowSpan.Value);
            if (colSpan.HasValue)
                AnyUiGrid.SetColumnSpan(fe, colSpan.Value);
            g.Children.Add(fe);
            return (fe);
        }


        public AnyUiBorder AddSmallBorderTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiBrush background = null,
            int? colSpan = null, int? rowSpan = null,
            AnyUiThickness borderThickness = null, AnyUiBrush borderBrush = null,
            double? cornerRadius = null)
        {
            var brd = new AnyUiBorder();
            brd.Margin = margin;
            if (background != null)
                brd.Background = background;
            if (borderThickness != null)
                brd.BorderThickness = borderThickness;
            if (borderBrush != null)
                brd.BorderBrush = borderBrush;
            if (cornerRadius != null)
                brd.CornerRadius = cornerRadius;
            AnyUiGrid.SetRow(brd, row);
            AnyUiGrid.SetColumn(brd, col);
            if (rowSpan.HasValue)
                AnyUiGrid.SetRowSpan(brd, rowSpan.Value);
            if (colSpan.HasValue)
                AnyUiGrid.SetColumnSpan(brd, colSpan.Value);
            g.Children.Add(brd);
            return (brd);
        }

        public AnyUiWrapPanel AddSmallWrapPanelTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiBrush background = null,
            int? colSpan = null)
        {
            var wp = new AnyUiWrapPanel();
            wp.Margin = margin;
            if (background != null)
                wp.Background = background;
            AnyUiGrid.SetRow(wp, row);
            AnyUiGrid.SetColumn(wp, col);
            if (colSpan.HasValue)
                AnyUiGrid.SetColumnSpan(wp, colSpan.Value);
            g.Children.Add(wp);
            return (wp);
        }

        public AnyUiStackPanel AddSmallStackPanelTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiBrush background = null,
            bool setVertical = false, bool setHorizontal = false,
            int? colSpan = null)
        {
            var sp = new AnyUiStackPanel();
            sp.Margin = margin;
            if (background != null)
                sp.Background = background;
            if (setVertical)
                sp.Orientation = AnyUiOrientation.Vertical;
            if (setHorizontal)
                sp.Orientation = AnyUiOrientation.Horizontal;
            AnyUiGrid.SetRow(sp, row);
            AnyUiGrid.SetColumn(sp, col);
            if (colSpan.HasValue)
                AnyUiGrid.SetColumnSpan(sp, colSpan.Value);
            g.Children.Add(sp);
            return (sp);
        }

        public AnyUiScrollViewer AddSmallScrollViewerTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiBrush background = null,
            int? colSpan = null,
            AnyUiScrollBarVisibility? horizontalScrollBarVisibility = null,
            AnyUiScrollBarVisibility? verticalScrollBarVisibility = null,
            AnyUiTargetPlatform? flattenForTarget = null,
            double? initialScrollPosition = null)
        {
            var sv = new AnyUiScrollViewer();
            sv.Margin = margin;
            if (background != null)
                sv.Background = background;
            if (horizontalScrollBarVisibility.HasValue)
                sv.HorizontalScrollBarVisibility = horizontalScrollBarVisibility.Value;
            if (verticalScrollBarVisibility.HasValue)
                sv.VerticalScrollBarVisibility = verticalScrollBarVisibility.Value;
            if (flattenForTarget.HasValue)
                sv.FlattenForTarget = flattenForTarget.Value;
            if (initialScrollPosition.HasValue)
                sv.InitialScrollPosition = initialScrollPosition.Value;
            AnyUiGrid.SetRow(sv, row);
            AnyUiGrid.SetColumn(sv, col);
            if (colSpan.HasValue)
                AnyUiGrid.SetColumnSpan(sv, colSpan.Value);
            g.Children.Add(sv);
            return (sv);
        }

        public AnyUiGrid AddSmallGridTo(
            AnyUiGrid g, int row, int col,
            int rows, int cols,
            string[] colWidths = null, string[] rowHeights = null,
            AnyUiThickness margin = null, AnyUiBrush background = null,
            AnyUiThickness padding = null)
        {
            var inner = AddSmallGrid(rows, cols, colWidths, rowHeights, margin, background);
            inner.Margin = margin;
            if (padding != null)
                inner.Padding = padding;
            AnyUiGrid.SetRow(inner, row);
            AnyUiGrid.SetColumn(inner, col);
            g.Children.Add(inner);
            return (inner);
        }

        public AnyUiImage AddSmallImageTo(
            AnyUiGrid g, int row, int col,
            AnyUiThickness margin = null,
            AnyUiStretch? stretch = null,
            AnyUiBitmapInfo bitmap = null)
        {
            var img = new AnyUiImage();
            if (margin != null)
                img.Margin = margin;
            if (stretch != null)
                img.Stretch = stretch.Value;
            if (bitmap != null)
                img.BitmapInfo = bitmap;
            AnyUiGrid.SetRow(img, row);
            AnyUiGrid.SetColumn(img, col);
            g.Children.Add(img);
            return (img);
        }

        public AnyUiTextBox AddSmallTextBoxTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiThickness padding = null,
            string text = "", AnyUiBrush foreground = null, AnyUiBrush background = null,
            int? colSpan = null,
            AnyUiVerticalAlignment? verticalAlignment = null,
            AnyUiVerticalAlignment? verticalContentAlignment = null,
            double? fontSize = null)
        {
            var tb = new AnyUiTextBox();
            tb.Margin = margin;
            tb.Padding = padding;
            if (foreground != null)
                tb.Foreground = foreground;
            if (background != null)
                tb.Background = background;
            if (fontSize != null)
                tb.FontSize = fontSize;
            tb.Text = text;
            if (verticalAlignment != null)
                tb.VerticalAlignment = verticalAlignment;
            if (verticalContentAlignment != null)
                tb.VerticalContentAlignment = verticalContentAlignment.Value;

            // (MIHO, 2020-11-13): constrain to one line
            tb.AcceptsReturn = false;
            tb.MaxLines = 3;
            tb.VerticalScrollBarVisibility = AnyUiScrollBarVisibility.Auto;

            AnyUiGrid.SetRow(tb, row);
            AnyUiGrid.SetColumn(tb, col);
            if (colSpan.HasValue)
                AnyUiGrid.SetColumnSpan(tb, colSpan.Value);
            g.Children.Add(tb);
            return (tb);
        }

        public AnyUiBorder AddSmallDropBoxTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiThickness padding = null,
            string text = "", AnyUiBrush borderBrush = null, AnyUiBrush background = null,
            AnyUiThickness borderThickness = null, int minHeight = 0,
            int? rowSpan = null, int? colSpan = null)
        {
            var brd = new AnyUiBorder();
            brd.Margin = margin;
            brd.Padding = padding;
            brd.IsDropBox = true;

            brd.BorderBrush = AnyUiBrushes.DarkBlue;
            if (borderBrush != null)
                brd.BorderBrush = borderBrush;

            brd.Background = AnyUiBrushes.LightBlue;
            if (background != null)
                brd.Background = background;

            brd.BorderThickness = borderThickness;

            if (minHeight > 0)
                brd.MinHeight = minHeight;

            var tb = new AnyUiTextBlock();
            tb.VerticalAlignment = AnyUiVerticalAlignment.Center;
            tb.HorizontalAlignment = AnyUiHorizontalAlignment.Center;
            tb.TextWrapping = AnyUiTextWrapping.Wrap;
            tb.FontSize = 0.8f;
            tb.Text = text;

            brd.Child = tb;

            AnyUiGrid.SetRow(brd, row);
            AnyUiGrid.SetColumn(brd, col);
            if (rowSpan.HasValue)
                AnyUiGrid.SetRowSpan(brd, rowSpan.Value);
            if (colSpan.HasValue)
                AnyUiGrid.SetColumnSpan(brd, colSpan.Value);
            g.Children.Add(brd);
            return (brd);
        }

        public AnyUiComboBox AddSmallComboBoxTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiThickness padding = null,
            string text = "", AnyUiBrush foreground = null, AnyUiBrush background = null,
            int minWidth = -1, int maxWidth = -1,
            string[] items = null, int? selectedIndex = null,
            bool isEditable = false,
            AnyUiVerticalAlignment? verticalContentAlignment = null,
            AnyUiHorizontalAlignment? horizontalAlignment = null)
        {
            var cb = new AnyUiComboBox();
            cb.Margin = margin;
            cb.Padding = padding;
            if (foreground != null)
                cb.Foreground = foreground;
            if (background != null)
                cb.Background = background;
            if (minWidth >= 0)
                cb.MinWidth = minWidth;
            if (maxWidth >= 0)
                cb.MaxWidth = maxWidth;
            if (items != null)
                foreach (var i in items)
                    cb.Items.Add("" + i);
            if (selectedIndex != null)
                cb.SelectedIndex = selectedIndex;
            cb.Text = text;
            cb.IsEditable = isEditable;
            if (verticalContentAlignment != null)
                cb.VerticalContentAlignment = verticalContentAlignment.Value;
            cb.HorizontalAlignment = AnyUiHorizontalAlignment.Left;
            if (horizontalAlignment.HasValue)
                cb.HorizontalAlignment = horizontalAlignment.Value;
            AnyUiGrid.SetRow(cb, row);
            AnyUiGrid.SetColumn(cb, col);
            g.Children.Add(cb);
            return (cb);
        }

        public void SmallComboBoxSelectNearestItem(AnyUiComboBox cb, string text)
        {
            if (cb == null || text == null)
                return;
            int foundI = -1;
            for (int i = 0; i < cb.Items.Count; i++)
                if (cb.Items[i].ToString().Trim().ToLower() == text.Trim().ToLower())
                    foundI = i;
            if (foundI >= 0)
                cb.SelectedIndex = foundI;
        }

        public AnyUiButton AddSmallButtonTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiThickness padding = null,
            string content = "", AnyUiBrush foreground = null, AnyUiBrush background = null,
            double? setHeight = null, AnyUiVerticalAlignment? verticalAlignment = null,
            bool? directInvoke = null)
        {
            var but = new AnyUiButton();
            but.Margin = margin;
            but.Padding = padding;
            if (foreground != null)
                but.Foreground = foreground;
            if (background != null)
                but.Background = background;
            if (verticalAlignment.HasValue)
                but.VerticalAlignment = verticalAlignment.Value;
            if (setHeight.HasValue)
            {
                but.MinHeight = setHeight.Value;
                but.MaxHeight = setHeight.Value;
            }
            if (directInvoke.HasValue)
                but.DirectInvoke = directInvoke.Value;
            but.Content = content;
            AnyUiGrid.SetRow(but, row);
            AnyUiGrid.SetColumn(but, col);
            g.Children.Add(but);
            return (but);
        }

        public AnyUiButton AddSmallContextMenuItemTo(
            AnyUiGrid g, int row, int col,
            string content,
            string[] menuHeaders,
            Func<object, AnyUiLambdaActionBase> menuItemLambda,
            AnyUiThickness margin = null, AnyUiThickness padding = null,
            AnyUiBrush foreground = null, AnyUiBrush background = null,
            double? fontSize = null, AnyUiFontWeight? fontWeight = null,
            AnyUiVerticalAlignment? verticalAlignment = null)
        {
            // construct button
            var but = new AnyUiButton();
            but.Margin = margin;
            but.Padding = padding;
            if (foreground != null)
                but.Foreground = foreground;
            if (background != null)
                but.Background = background;
            if (fontSize.HasValue)
                but.FontSize = fontSize;
            if (fontWeight.HasValue)
                but.FontWeight = fontWeight.Value;
            if (verticalAlignment != null)
                but.VerticalAlignment = verticalAlignment.Value;
            but.Content = content;
            AnyUiGrid.SetRow(but, row);
            AnyUiGrid.SetColumn(but, col);
            g.Children.Add(but);
            but.SpecialAction = new AnyUiSpecialActionContextMenu(menuHeaders, menuItemLambda);

            // ok
            return (but);
        }

        public AnyUiCheckBox AddSmallCheckBoxTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiThickness padding = null,
            string content = "", bool isChecked = false, AnyUiBrush foreground = null, AnyUiBrush background = null,
            AnyUiVerticalAlignment? verticalAlignment = null,
            AnyUiVerticalAlignment? verticalContentAlignment = null)
        {
            var cb = new AnyUiCheckBox();
            cb.Margin = margin;
            cb.Padding = padding;
            if (foreground != null)
                cb.Foreground = foreground;
            if (background != null)
                cb.Background = background;
            cb.Content = content;
            cb.IsChecked = isChecked;
            if (verticalAlignment != null)
                cb.VerticalAlignment = verticalAlignment.Value;
            if (verticalContentAlignment != null)
                cb.VerticalContentAlignment = verticalContentAlignment.Value;
            AnyUiGrid.SetRow(cb, row);
            AnyUiGrid.SetColumn(cb, col);
            g.Children.Add(cb);
            return (cb);
        }

        public AnyUiTextBlock AddSmallBasicLabelTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiThickness padding = null,
            string content = "", AnyUiBrush foreground = null, AnyUiBrush background = null, bool setBold = false,
            double? fontSize = null, int? colSpan = null,
            AnyUiTextWrapping? textWrapping = null, bool setHyperLink = false,
            AnyUiVerticalAlignment? verticalAlignment = null,
            AnyUiVerticalAlignment? verticalContentAlignment = null,
            AnyUiHorizontalAlignment? horizontalAlignment = null,
            AnyUiHorizontalAlignment? horizontalContentAlignment = null,
            bool textIsSelectable = true)
        {
            AnyUiTextBlock lab = null;
            if (textIsSelectable)
            {
                var stb = new AnyUiSelectableTextBlock();
                if (setHyperLink)
                    stb.TextAsHyperlink = true;
                lab = stb;
            }
            else
                lab = new AnyUiTextBlock();

            lab.Margin = margin;
            lab.Padding = padding;
            if (foreground != null)
                lab.Foreground = foreground;
            if (background != null)
                lab.Background = background;
            if (setBold)
                lab.FontWeight = AnyUiFontWeight.Bold;
            if (fontSize != null)
                lab.FontSize = fontSize;
            if (textWrapping.HasValue)
                lab.TextWrapping = textWrapping.Value;
            if (verticalAlignment.HasValue)
                lab.VerticalAlignment = verticalAlignment.Value;
            if (verticalContentAlignment.HasValue)
                lab.VerticalContentAlignment = verticalContentAlignment.Value;
            if (horizontalAlignment.HasValue)
                lab.HorizontalAlignment = horizontalAlignment.Value;
            if (horizontalContentAlignment.HasValue)
                lab.HorizontalContentAlignment = horizontalContentAlignment.Value;
            lab.Text = content;

            AnyUiGrid.SetRow(lab, row);
            AnyUiGrid.SetColumn(lab, col);
            if (colSpan.HasValue)
                AnyUiGrid.SetColumnSpan(lab, colSpan.Value);
            g.Children.Add(lab);
            return (lab);
        }

        public T Set<T>(T fe,
            int? row = null, int? col = null,
            int? colSpan = null, int? rowSpan = null,
            AnyUiThickness margin = null,
            AnyUiBrush foreground = null,
            AnyUiBrush background = null,
            int? minWidth = null, int? maxWidth = null,
            int? minHeight = null, int? maxHeight = null,
            AnyUiHorizontalAlignment? horizontalAlignment = null,
            AnyUiHorizontalAlignment? horizontalContentAlignment = null,
            AnyUiVerticalAlignment? verticalAlignment = null,
            AnyUiVerticalAlignment? verticalContentAlignment = null,
            AnyUiTargetPlatform? skipForTarget = null,
            AnyUiEventMask? eventMask = null) where T : AnyUiFrameworkElement
        {
            // access
            if (fe == null)
                return null;

            // FrameworkElem
            if (row.HasValue)
                AnyUiGrid.SetRow(fe, row.Value);
            if (col.HasValue)
                AnyUiGrid.SetColumn(fe, col.Value);
            if (rowSpan.HasValue)
                AnyUiGrid.SetRowSpan(fe, rowSpan.Value);
            if (colSpan.HasValue)
                AnyUiGrid.SetColumnSpan(fe, colSpan.Value);

            if (margin != null)
                fe.Margin = margin;
            if (horizontalAlignment != null)
                fe.HorizontalAlignment = horizontalAlignment;
            if (verticalAlignment != null)
                fe.VerticalAlignment = verticalAlignment;

            if (eventMask.HasValue)
                fe.EmitEvent = eventMask.Value;

            if (minWidth.HasValue)
                fe.MinWidth = minWidth;
            if (maxWidth.HasValue)
                fe.MaxWidth = maxWidth;

            if (minHeight.HasValue)
                fe.MinHeight = minHeight;
            if (maxHeight.HasValue)
                fe.MaxHeight = maxHeight;

            if (skipForTarget.HasValue)
                fe.SkipForTarget = skipForTarget.Value;

            // ContentControl
            if (fe is AnyUiContentControl ctl)
            {
                if (foreground != null)
                    ctl.Foreground = foreground;
                if (background != null)
                    ctl.Background = background;

                if (horizontalContentAlignment != null)
                    ctl.HorizontalContentAlignment = horizontalContentAlignment;
                if (verticalContentAlignment != null)
                    ctl.VerticalContentAlignment = verticalContentAlignment;
            }

            // chain
            return fe;
        }

        //
        // small widget handling
        //

        public void AddVerticalSpace(AnyUiStackPanel view, double height = 5)
        {
            var space = new AnyUiBorder()
            {
                BorderBrush = AnyUiBrushes.Transparent,
                BorderThickness = new AnyUiThickness(height),
                Height = height
            };
            view.Add(space);
        }

        public void AddGroup(AnyUiStackPanel view, string name, AnyUiBrushTuple colors,
            bool requestAuxButton = false,
            string auxButtonTitle = null, Func<object, AnyUiLambdaActionBase> auxButtonLambda = null,
            string[] auxContextHeader = null, Func<object, AnyUiLambdaActionBase> auxContextLambda = null)
        {
            AddGroup(view, name, colors?.Bg, colors?.Fg, requestAuxButton,
                auxButtonTitle, auxButtonLambda,
                auxContextHeader, auxContextLambda);
        }

        public void AddGroup(AnyUiStackPanel view, string name, AnyUiBrush background, AnyUiBrush foreground,
            bool requestAuxButton = false,
            string auxButtonTitle = null, Func<object, AnyUiLambdaActionBase> auxButtonLambda = null,
            string[] auxContextHeader = null, Func<object, AnyUiLambdaActionBase> auxContextLambda = null)
        {
            var g = AddSmallGrid(1, 4, new[] { "*", "#", "#", "#" }, margin: new AnyUiThickness(0, 13, 0, 0));

            var auxButton = requestAuxButton && auxButtonTitle != null && auxButtonLambda != null;

            // manually add label (legacy?)
            var l = new AnyUiLabel();
            l.Margin = new AnyUiThickness(0, 0, 0, 0);
            l.Padding = new AnyUiThickness(5, 0, 0, 0);
            l.Background = background;
            l.Foreground = foreground;
            l.Content = "" + name;
            l.FontWeight = AnyUiFontWeight.Bold;
            AnyUiGrid.SetRow(l, 0);
            AnyUiGrid.SetColumn(l, 0);
            g.Children.Add(l);
            view.Children.Add(g);

            // auxButton
            if (auxButton)
            {
                AnyUiUIElement.RegisterControl(
                    AddSmallButtonTo(
                        g, 0, 1,
                        margin: new AnyUiThickness(2, 2, 2, 2),
                        padding: new AnyUiThickness(5, 0, 5, 0),
                        content: auxButtonTitle),
                    auxButtonLambda);
            }

            // context menu
            if (auxContextHeader != null && auxContextLambda != null)
            {
                AddSmallContextMenuItemTo(
                        g, 0, 2,
                        "\u22ee",
                        auxContextHeader.ToArray(),
                        margin: new AnyUiThickness(2, 2, 2, 2),
                        padding: new AnyUiThickness(5, 0, 5, 0),
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        menuItemLambda: auxContextLambda);
            }
        }

        public void AddGroup(AnyUiStackPanel view, string name, AnyUiBrush background, AnyUiBrush foreground,
            bool requestContextMenu,
            string contextMenuText, string[] menuHeaders, Func<object, AnyUiLambdaActionBase> menuItemLambda,
            AnyUiThickness margin = null, AnyUiThickness padding = null)
        {
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 13, 0, 0);

            var gc1 = new AnyUiColumnDefinition();
            gc1.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.ColumnDefinitions.Add(gc1);

            var isContextMenu = requestContextMenu && contextMenuText != null
                && menuHeaders != null && menuItemLambda != null;
            if (isContextMenu)
            {
                var gc3 = new AnyUiColumnDefinition();
                gc3.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                g.ColumnDefinitions.Add(gc3);
            }

            var l = new AnyUiLabel();
            l.Margin = new AnyUiThickness(0, 0, 0, 0);
            l.Padding = new AnyUiThickness(5, 0, 0, 0);
            l.Background = background;
            l.Foreground = foreground;
            l.Content = "" + name;
            l.FontWeight = AnyUiFontWeight.Bold;
            AnyUiGrid.SetRow(l, 0);
            AnyUiGrid.SetColumn(l, 0);
            g.Children.Add(l);
            view.Children.Add(g);

            if (isContextMenu)
            {
                AddSmallContextMenuItemTo(
                    g, 0, 1,
                    contextMenuText, menuHeaders, menuItemLambda,
                    margin: margin, padding: padding);
            }
        }

        public AnyUiSelectableTextBlock AddSmallLabelTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiThickness padding = null,
            string content = "", AnyUiBrush foreground = null, AnyUiBrush background = null,
            bool setBold = false, bool setNoWrap = false,
            AnyUiVerticalAlignment? verticalAlignment = null,
            AnyUiVerticalAlignment? verticalContentAlignment = null)
        {
            var lab = new AnyUiSelectableTextBlock();

            lab.Margin = margin;
            lab.Padding = padding;
            if (verticalAlignment != null)
                lab.VerticalAlignment = verticalAlignment;
            if (verticalContentAlignment != null)
                lab.VerticalContentAlignment = verticalContentAlignment.Value;
            if (foreground != null)
                lab.Foreground = foreground;
            if (background != null)
                lab.Background = background;
            if (setBold)
                lab.FontWeight = AnyUiFontWeight.Bold;
            // if (setNoWrap)
                lab.TextWrapping = AnyUiTextWrapping.NoWrap;
            lab.Text = content;

            // check, which content
            if (this.showIriMode
                && content != null && content != ""
                && (content.Trim().ToLower().StartsWith("http://")
                 || content.Trim().ToLower().StartsWith("https://")))
            {
                // mark as hyperlink
                lab.TextAsHyperlink = true;

                // directly assign lambda
                lab.setValueLambda = (o) =>
                {
                    return new AnyUiLambdaActionDisplayContentFile(content, preferInternalDisplay: true);
                };
            }

            AnyUiGrid.SetRow(lab, row);
            AnyUiGrid.SetColumn(lab, col);
            g.Children.Add(lab);
            return (lab);
        }

        /// <summary>
        /// Adds a subpanel, which has the caption "key". Can be used to visual set apart multiple items
        /// from the sub-panel from the items on the main panel.
        /// </summary>
        /// <param name="view">Panel to be added to</param>
        /// <param name="caption">Caption</param>
        /// <returns>Sub-panel, to which can be added</returns>
        public AnyUiStackPanel AddSubStackPanel(AnyUiStackPanel view, string caption,
            int minWidthFirstCol = -1)
        {
            var g = AddSmallGrid(1, 2, new[] { "#", "*" });

            if (minWidthFirstCol > -1)
                g.ColumnDefinitions[0].MinWidth = minWidthFirstCol;

            AddSmallLabelTo(g, 0, 0, content: caption);
            var sp = AddSmallStackPanelTo(g, 0, 1, setVertical: true);

            // in total
            view.Children.Add(g);

            // done
            return (sp);
        }

        public AnyUiGrid AddSubGrid(AnyUiStackPanel view, string caption,
            int rows, int cols, string[] colWidths = null, AnyUiThickness margin = null,
            int minWidthFirstCol = -1)
        {
            var g = AddSmallGrid(1, 2, new[] { "#", "*" });

            if (minWidthFirstCol > -1)
                g.ColumnDefinitions[0].MinWidth = minWidthFirstCol;

            AddSmallLabelTo(g, 0, 0, content: caption);
            var inner = AddSmallGridTo(g, 0, 1, rows, cols, colWidths, margin: margin);

            // in total
            view.Children.Add(g);

            // done
            return (inner);
        }

    }
}
