using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AnyUi
{
    public class AnyUiSmallWidgetToolkit
    {
        public AnyUiGrid AddSmallGrid(int rows, int cols, string[] colWidths = null, AnyUiThickness margin = null)
        {
            var g = new AnyUiGrid();
            g.Margin = margin;

            // Cols
            for (int ci = 0; ci < cols; ci++)
            {
                var gc = new AnyUiColumnDefinition();
                // default
                gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
                // width definition
                if (colWidths != null && colWidths.Length > ci && colWidths[ci] != null)
                {
                    double scale = 1.0;
                    var kind = colWidths[ci].Trim();
                    var m = Regex.Match(colWidths[ci].Trim(), @"([0-9.+-]+)(.$)");
                    if (m.Success && m.Groups.Count >= 2)
                    {
                        var scaleSt = m.Groups[1].ToString().Trim();
                        if (Double.TryParse(scaleSt, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                            scale = d;
                        kind = m.Groups[2].ToString().Trim();
                    }
                    if (kind == "#")
                        gc.Width = new AnyUiGridLength(scale, AnyUiGridUnitType.Auto);
                    if (kind == "*")
                        gc.Width = new AnyUiGridLength(scale, AnyUiGridUnitType.Star);
                    if (kind == ":")
                        gc.Width = new AnyUiGridLength(scale, AnyUiGridUnitType.Pixel);
                }
                g.ColumnDefinitions.Add(gc);
            }

            // Rows
            for (int ri = 0; ri < rows; ri++)
            {
                var gr = new AnyUiRowDefinition();
                gr.Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                g.RowDefinitions.Add(gr);
            }

            return g;
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
            bool? skipForBrowser = null,
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
            if (skipForBrowser.HasValue)
                sv.SkipForBrowser = skipForBrowser.Value;
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
            int rows, int cols, string[] colWidths = null, AnyUiThickness margin = null)
        {
            var inner = AddSmallGrid(rows, cols, colWidths, margin);
            inner.Margin = margin;
            AnyUiGrid.SetRow(inner, row);
            AnyUiGrid.SetColumn(inner, col);
            g.Children.Add(inner);
            return (inner);
        }

        public AnyUiTextBox AddSmallTextBoxTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiThickness padding = null,
            string text = "", AnyUiBrush foreground = null, AnyUiBrush background = null,
            int? colSpan = null,
            AnyUiVerticalAlignment? verticalContentAlignment = null)
        {
            var tb = new AnyUiTextBox();
            tb.Margin = margin;
            tb.Padding = padding;
            if (foreground != null)
                tb.Foreground = foreground;
            if (background != null)
                tb.Background = background;
            tb.Text = text;
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
            int minWidth = -1, int maxWidth = -1, string[] items = null, bool isEditable = false,
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
            double? setHeight = null, AnyUiVerticalAlignment? verticalAlignment = null)
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
            AnyUiBrush foreground = null, AnyUiBrush background = null)
        {
            // construct button
            var but = new AnyUiButton();
            but.Margin = margin;
            but.Padding = padding;
            if (foreground != null)
                but.Foreground = foreground;
            if (background != null)
                but.Background = background;
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
            if (verticalContentAlignment != null)
                cb.VerticalContentAlignment = verticalContentAlignment.Value;
            AnyUiGrid.SetRow(cb, row);
            AnyUiGrid.SetColumn(cb, col);
            g.Children.Add(cb);
            return (cb);
        }

        public AnyUiSelectableTextBlock AddSmallBasicLabelTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiThickness padding = null,
            string content = "", AnyUiBrush foreground = null, AnyUiBrush background = null, bool setBold = false,
            double? fontSize = null, int? colSpan = null, bool setWrap = false,
            AnyUiVerticalAlignment? verticalAlignment = null,
            AnyUiVerticalAlignment? verticalContentAlignment = null,
            AnyUiHorizontalAlignment? horizontalAlignment = null,
            AnyUiHorizontalAlignment? horizontalContentAlignment = null)
        {
            var lab = new AnyUiSelectableTextBlock();

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
            if (setWrap)
                lab.TextWrapping = AnyUiTextWrapping.Wrap;
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

    }
}
