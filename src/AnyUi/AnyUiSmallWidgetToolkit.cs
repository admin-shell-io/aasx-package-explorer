using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AnyUi
{
    public class AnyUiSmallWidgetToolkit
    {       
        public AnyUiGrid AddSmallGrid(int rows, int cols, 
            string[] colWidths = null, string[] rowHeights = null,
            AnyUiThickness margin = null, AnyUiBrush background = null)
        {
            var g = new AnyUiGrid();
            g.Margin = margin;
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
            AnyUiThickness margin = null, AnyUiBrush background = null)
        {
            var inner = AddSmallGrid(rows, cols, colWidths, rowHeights, margin, background);
            inner.Margin = margin;
            AnyUiGrid.SetRow(inner, row);
            AnyUiGrid.SetColumn(inner, col);
            g.Children.Add(inner);
            return (inner);
        }

        public AnyUiImage AddSmallImageTo(
            AnyUiGrid g, int row, int col,
            AnyUiThickness margin = null,
            AnyUiStretch? stretch = null,
            object bitmap = null)
        {
            var img = new AnyUiImage();
            if (margin != null)
                img.Margin = margin;
            if (stretch != null)
                img.Stretch = stretch.Value;
            if (bitmap != null)
                img.Bitmap = bitmap;
            AnyUiGrid.SetRow(img, row);
            AnyUiGrid.SetColumn(img, col);
            g.Children.Add(img);
            return (img);
        }

        public AnyUiTextBox AddSmallTextBoxTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiThickness padding = null,
            string text = "", AnyUiBrush foreground = null, AnyUiBrush background = null,
            int? colSpan = null,
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
            AnyUiBrush foreground = null, AnyUiBrush background = null,
            double? fontSize = null, AnyUiFontWeight? fontWeight = null)
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

        public AnyUiTextBlock AddSmallBasicLabelTo(
            AnyUiGrid g, int row, int col, AnyUiThickness margin = null, AnyUiThickness padding = null,
            string content = "", AnyUiBrush foreground = null, AnyUiBrush background = null, bool setBold = false,
            double? fontSize = null, int? colSpan = null, bool setWrap = false,
            AnyUiVerticalAlignment? verticalAlignment = null,
            AnyUiVerticalAlignment? verticalContentAlignment = null,
            AnyUiHorizontalAlignment? horizontalAlignment = null,
            AnyUiHorizontalAlignment? horizontalContentAlignment = null,
            bool textIsSelectable = true)
        {
            AnyUiTextBlock lab = null;
            if (textIsSelectable) 
                lab = new AnyUiSelectableTextBlock();
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
            AnyUiTargetPlatform? skipForTarget = null) where T : AnyUiFrameworkElement
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

    }
}
