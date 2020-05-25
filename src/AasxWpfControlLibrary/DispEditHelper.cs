﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AasxIntegrationBase;
using AdminShellNS;
using AasxGlobalLogging;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License - v 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxPackageExplorer
{
    //
    // Hinting (will be used below)
    //

    public class HintCheck
    {
        public enum Severity { High, Notice };

        public Func<bool> CheckPred = null;
        public string TextToShow = null;
        public bool BreakIfTrue = false;
        public Severity SeverityLevel = Severity.High;

        /// <summary>
        /// Formulate a check, which can cause a hint.
        /// </summary>
        /// <param name="check">Lambda to check. If returns true, trigger the hint.</param>
        /// <param name="text">Hint in plain text form.</param>
        /// <param name="breakIfTrue">If check was true, abort checking of further hints. Use: avoid checking of null for every hint.</param>
        /// <param name="severityLevel">Display high/red or normal/blue</param>
        public HintCheck(Func<bool> check, string text, bool breakIfTrue = false, Severity severityLevel = Severity.High)
        {
            this.CheckPred = check;
            this.TextToShow = text;
            this.BreakIfTrue = breakIfTrue;
            this.SeverityLevel = severityLevel;
        }
    }

    //
    // Highlighting
    //

    public static class DispEditHighlight
    {
        public class HighlightFieldInfo
        {
            public object containingObject;
            public object fieldObject;
            public int fieldHash;

            public HighlightFieldInfo() { }

            public HighlightFieldInfo(object containingObject, object fieldObject, int fieldHash)
            {
                this.containingObject = containingObject;
                this.fieldObject = fieldObject;
                this.fieldHash = fieldHash;
            }
        }
    }

    //
    // Helpers
    //

    public class DispEditHelper
    {
        // 
        // Members
        //

        private string[] defaultLanguages = new[] { "en", "de", "fr", "es", "it", "cn", "kr", "jp" };

        public AdminShellPackageEnv package = null;
        public AdminShellPackageEnv[] auxPackages = null;

        public IFlyoutProvider flyoutProvider = null;

        public Brush[][] levelColors = null;

        public bool editMode = false;
        public bool hintMode = false;

        public ModifyRepo repo = null;

        public DispEditHighlight.HighlightFieldInfo highlightField = null;
        private FrameworkElement lastHighlightedField = null;

        //
        // Highlighting
        //        

        public void HightligtStateElement(FrameworkElement fe, bool highlighted)
        {
            // access
            if (fe == null)
                return;

            // save
            if (highlighted)
                this.lastHighlightedField = fe;

            // be a little careful
            // ReSharper disable EmptyGeneralCatchClause
            try
            {
                // Textbox
                if (fe is TextBox)
                {
                    var tb = fe as TextBox;
                    if (highlighted)
                    {
                        tb.BorderBrush = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                        tb.BorderThickness = new Thickness(3);
                        tb.Focus();
                        tb.SelectAll();
                    }
                    else
                    {
                        tb.BorderBrush = SystemColors.ControlDarkBrush;
                        tb.BorderThickness = new Thickness(1);
                    }
                }

                // Combobox
                if (fe is ComboBox)
                {
                    var cb = fe as ComboBox;
                    if (highlighted)
                    {
                        cb.BorderBrush = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                        cb.BorderThickness = new Thickness(3);

                        try
                        {
                            // see: https://stackoverflow.com/questions/37006596/borderbrush-to-combobox
                            // see also: https://stackoverflow.com/questions/2285491/wpf-findname-returns-null-when-it-should-not
                            cb.ApplyTemplate();
                            var cbTemp = cb.Template;
                            if (cbTemp != null)
                            {
                                // var rootGrid = cbTemp.FindName("templateRoot", cb) as Grid;

                                var toggleButton = cbTemp.FindName("toggleButton", cb) as System.Windows.Controls.Primitives.ToggleButton;
                                toggleButton?.ApplyTemplate();
                                var tgbTemp = toggleButton?.Template;
                                if (tgbTemp != null)
                                {
                                    var border = tgbTemp.FindName("templateRoot", toggleButton) as Border;
                                    if (border != null)
                                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                                }
                            }
                        }
                        catch { }

                        cb.Focus();
                    }
                    else
                    {
                        cb.BorderBrush = SystemColors.ControlDarkBrush;
                        cb.BorderThickness = new Thickness(1);
                    }
                }
            }
            catch { }
            // ReSharper enable EmptyGeneralCatchClause
        }

        public void ClearHighlights()
        {
            // ReSharper disable EmptyGeneralCatchClause
            try
            {
                if (this.lastHighlightedField == null)
                    return;
                HightligtStateElement(this.lastHighlightedField, highlighted: false);
            }
            catch { }
            // ReSharper enable EmptyGeneralCatchClause
        }

        //
        // small widget hanlding
        //

        public Grid AddSmallGrid(int rows, int cols, string[] colWidths = null, Thickness margin = new Thickness())
        {
            var g = new Grid();
            g.Margin = margin;

            // Cols
            for (int ci = 0; ci < cols; ci++)
            {
                var gc = new ColumnDefinition();
                // default
                gc.Width = new GridLength(1.0, GridUnitType.Star);
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
                        gc.Width = new GridLength(scale, GridUnitType.Auto);
                    if (kind == "*")
                        gc.Width = new GridLength(scale, GridUnitType.Star);
                    if (kind == ":")
                        gc.Width = new GridLength(scale, GridUnitType.Pixel);
                }
                g.ColumnDefinitions.Add(gc);
            }

            // Rows
            for (int ri = 0; ri < rows; ri++)
            {
                var gr = new RowDefinition();
                g.RowDefinitions.Add(gr);
            }

            return g;
        }

        public WrapPanel AddSmallWrapPanelTo(Grid g, int row, int col, Thickness margin = new Thickness(), Brush background = null)
        {
            var wp = new WrapPanel();
            wp.Margin = margin;
            if (background != null)
                wp.Background = background;
            Grid.SetRow(wp, row);
            Grid.SetColumn(wp, col);
            g.Children.Add(wp);
            return (wp);
        }

        public StackPanel AddSmallStackPanelTo(Grid g, int row, int col, Thickness margin = new Thickness(), Brush background = null, bool setVertical = false, bool setHorizontal = false)
        {
            var sp = new StackPanel();
            sp.Margin = margin;
            if (background != null)
                sp.Background = background;
            if (setVertical)
                sp.Orientation = Orientation.Vertical;
            if (setHorizontal)
                sp.Orientation = Orientation.Horizontal;
            Grid.SetRow(sp, row);
            Grid.SetColumn(sp, col);
            g.Children.Add(sp);
            return (sp);
        }

        public TextBox AddSmallTextBoxTo(Grid g, int row, int col, Thickness margin = new Thickness(), Thickness padding = new Thickness(), string text = "",
            Brush foreground = null, Brush background = null, Nullable<VerticalAlignment> verticalContentAlignment = null)
        {
            var tb = new TextBox();
            tb.Margin = margin;
            tb.Padding = padding;
            if (foreground != null)
                tb.Foreground = foreground;
            if (background != null)
                tb.Background = background;
            tb.Text = text;
            if (verticalContentAlignment != null)
                tb.VerticalContentAlignment = verticalContentAlignment.Value;
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, col);
            g.Children.Add(tb);
            return (tb);
        }

        public ComboBox AddSmallComboBoxTo(Grid g, int row, int col, Thickness margin = new Thickness(), Thickness padding = new Thickness(), string text = "", Brush foreground = null, Brush background = null,
            int minWidth = -1, int maxWidth = -1, string[] items = null, bool isEditable = false,
            Nullable<VerticalAlignment> verticalContentAlignment = null)
        {
            var cb = new ComboBox();
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
            cb.HorizontalAlignment = HorizontalAlignment.Left;
            Grid.SetRow(cb, row);
            Grid.SetColumn(cb, col);
            g.Children.Add(cb);
            return (cb);
        }

        public void SmallComboBoxSelectNearestItem(ComboBox cb, string text)
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

        public Button AddSmallButtonTo(Grid g, int row, int col, Thickness margin = new Thickness(), Thickness padding = new Thickness(), string content = "", Brush foreground = null, Brush background = null)
        {
            var but = new Button();
            but.Margin = margin;
            but.Padding = padding;
            if (foreground != null)
                but.Foreground = foreground;
            if (background != null)
                but.Background = background;
            but.Content = content;
            Grid.SetRow(but, row);
            Grid.SetColumn(but, col);
            g.Children.Add(but);
            return (but);
        }

        public CheckBox AddSmallCheckBoxTo(Grid g, int row, int col, Thickness margin = new Thickness(), Thickness padding = new Thickness(),
            string content = "", bool isChecked = false, Brush foreground = null, Brush background = null,
            Nullable<VerticalAlignment> verticalContentAlignment = null)
        {
            var cb = new CheckBox();
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
            Grid.SetRow(cb, row);
            Grid.SetColumn(cb, col);
            g.Children.Add(cb);
            return (cb);
        }

        public void AddGroup(StackPanel view, string name, Brush background, Brush foreground,
            ModifyRepo repo = null,
            string auxButtonTitle = null, Func<object, ModifyRepo.LambdaAction> auxButtonLambda = null)
        {
            var g = new Grid();
            g.Margin = new Thickness(0, 13, 0, 0);

            var gc1 = new ColumnDefinition();
            gc1.Width = new GridLength(1.0, GridUnitType.Star);
            g.ColumnDefinitions.Add(gc1);

            var auxButton = repo != null && auxButtonTitle != null && auxButtonLambda != null;
            if (auxButton)
            {
                var gc3 = new ColumnDefinition();
                gc3.Width = new GridLength(1.0, GridUnitType.Auto);
                g.ColumnDefinitions.Add(gc3);
            }

            var l = new Label();
            l.Margin = new Thickness(0, 0, 0, 0);
            l.Padding = new Thickness(5, 0, 0, 0);
            l.Background = background;
            l.Foreground = foreground;
            l.Content = "" + name;
            l.FontWeight = FontWeights.Bold;
            Grid.SetRow(l, 0);
            Grid.SetColumn(l, 0);
            g.Children.Add(l);
            view.Children.Add(g);

            if (auxButton)
            {
                repo.RegisterControl(AddSmallButtonTo(g, 0, 1, margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0), content: auxButtonTitle),
                    auxButtonLambda);
            }
        }

        public TextBlock AddSmallLabelTo(Grid g, int row, int col, Thickness margin = new Thickness(), Thickness padding = new Thickness(), string content = "", Brush foreground = null, Brush background = null, bool setBold = false)
        {
            var lab = new SelectableTextBlock();

            lab.Margin = margin;
            lab.Padding = padding;
            if (foreground != null)
                lab.Foreground = foreground;
            if (background != null)
                lab.Background = background;
            if (setBold)
                lab.FontWeight = FontWeights.Bold;
            lab.Text = content;
            Grid.SetRow(lab, row);
            Grid.SetColumn(lab, col);
            g.Children.Add(lab);
            return (lab);
        }

        /// <summary>
        /// Adds a subpanel, which has the caption "key". Can be used to visual set apart multiple items from the sub-panel
        /// from the items on the main panel.
        /// </summary>
        /// <param name="view">Panel to be added to</param>
        /// <param name="caption">Caption</param>
        /// <returns>Sub-panel, to which can be added</returns>
        public StackPanel AddSubStackPanel(StackPanel view, string caption)
        {
            var g = AddSmallGrid(1, 2, new[] { "#", "*" });
            // var l = AddSmallLabelTo(g, 0, 0, content: caption);
            var sp = AddSmallStackPanelTo(g, 0, 1, setVertical: true);

            // in total
            view.Children.Add(g);

            // done
            return (sp);
        }

        public void AddKeyValueRef(
            StackPanel view, string key, object containingObject, ref string value, string nullValue = null,
            ModifyRepo repo = null, Func<object, ModifyRepo.LambdaAction> setValue = null,
            string[] comboBoxItems = null, bool comboBoxIsEditable = false,
            string auxButtonTitle = null, Func<object, ModifyRepo.LambdaAction> auxButtonLambda = null, string auxButtonToolTip = null,
            string[] auxButtonTitles = null, string[] auxButtonToolTips = null,
            ModifyRepo.LambdaAction takeOverLambdaAction = null)
        {
            AddKeyValue(view, key, value, nullValue, repo, setValue, comboBoxItems, comboBoxIsEditable, auxButtonTitle, auxButtonLambda, auxButtonToolTip,
                auxButtonTitles, auxButtonToolTips, takeOverLambdaAction, (value == null) ? 0 : value.GetHashCode(), containingObject: containingObject);
        }


        public void AddKeyValue(
            StackPanel view, string key, string value, string nullValue = null,
            ModifyRepo repo = null, Func<object, ModifyRepo.LambdaAction> setValue = null,
            string[] comboBoxItems = null, bool comboBoxIsEditable = false,
            string auxButtonTitle = null, Func<object, ModifyRepo.LambdaAction> auxButtonLambda = null, string auxButtonToolTip = null,
            string[] auxButtonTitles = null, string[] auxButtonToolTips = null,
            ModifyRepo.LambdaAction takeOverLambdaAction = null,
            Nullable<int> valueHash = null,
            object containingObject = null)
        {
            // draw anyway?
            if (repo != null && value == null)
            {
                // generate default value
                value = "";
            }
            else
            {
                // normal handling
                if (value == null && nullValue == null)
                    return;
                if (value == null)
                    value = nullValue;
            }

            // aux buttons
            List<string> intButtonTitles = new List<string>();
            List<string> intButtonToolTips = new List<string>();
            if (auxButtonTitle != null)
                intButtonTitles.Add(auxButtonTitle);
            if (auxButtonToolTip != null)
                intButtonToolTips.Add(auxButtonToolTip);
            if (auxButtonTitles != null)
                intButtonTitles.AddRange(auxButtonTitles);
            if (auxButtonToolTips != null)
                intButtonToolTips.AddRange(auxButtonToolTips);

            var auxButton = repo != null && intButtonTitles.Count > 0 && auxButtonLambda != null;

            // Grid
            var g = new Grid();
            g.Margin = new Thickness(0, 1, 0, 1);
            var gc1 = new ColumnDefinition();
            gc1.Width = new GridLength(100);
            g.ColumnDefinitions.Add(gc1);
            var gc2 = new ColumnDefinition();
            gc2.Width = new GridLength(1.0, GridUnitType.Star);
            g.ColumnDefinitions.Add(gc2);

            if (auxButton)
                for (int i = 0; i < intButtonTitles.Count; i++)
                {
                    var gc3 = new ColumnDefinition();
                    gc3.Width = new GridLength(1.0, GridUnitType.Auto);
                    g.ColumnDefinitions.Add(gc3);
                }

            // Label for key
            AddSmallLabelTo(g, 0, 0, padding: new Thickness(5, 0, 0, 0), content: "" + key + ":");

            // Label / TextBox for value
            if (repo == null)
            {
                AddSmallLabelTo(g, 0, 1, padding: new Thickness(2, 0, 0, 0), content: "" + value);
            }
            else if (comboBoxItems != null)
            {
                // guess some max width, in order 
                var maxc = 5;
                foreach (var c in comboBoxItems)
                    if (c.Length > maxc)
                        maxc = c.Length;
                var maxWidth = 10 * maxc; // about one em

                // use combo box
                var cb = AddSmallComboBoxTo(g, 0, 1, margin: new Thickness(0, 2, 2, 2), padding: new Thickness(2, 0, 2, 0), text: "" + value,
                        minWidth: 60, maxWidth: maxWidth, items: comboBoxItems, isEditable: comboBoxIsEditable);
                repo.RegisterControl(cb, setValue, takeOverLambda: takeOverLambdaAction);

                // check here, if to hightlight
                if (cb != null && this.highlightField != null && valueHash != null && this.highlightField.fieldHash == valueHash.Value
                    && (containingObject == null || containingObject == this.highlightField.containingObject))
                    this.HightligtStateElement(cb, true);
            }
            else
            {
                // use plain text box
                var tb = AddSmallTextBoxTo(g, 0, 1, margin: new Thickness(0, 2, 2, 2), text: "" + value);
                repo.RegisterControl(tb,
                    setValue, takeOverLambda: takeOverLambdaAction);

                // check here, if to hightlight
                if (tb != null && this.highlightField != null && valueHash != null && this.highlightField.fieldHash == valueHash.Value
                    && (containingObject == null || containingObject == this.highlightField.containingObject))
                    this.HightligtStateElement(tb, true);
            }

            if (auxButton)
                for (int i = 0; i < intButtonTitles.Count; i++)
                {
                    Func<object, ModifyRepo.LambdaAction> lmb = null;
                    int closureI = i;
                    if (auxButtonLambda != null)
                        lmb = (o) =>
                        {
                            return auxButtonLambda(closureI); // exchange o with i !!
                        };
                    var b = repo.RegisterControl(AddSmallButtonTo(g, 0, 2 + i, margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0), content: intButtonTitles[i]),
                        lmb) as Button;
                    if (i < intButtonToolTips.Count)
                        b.ToolTip = intButtonToolTips[i];
                }

            // in total
            view.Children.Add(g);
        }

        public void AddKeyMultiValue(StackPanel view, string key, string[][] value, string[] widths)
        {
            // draw anyway?
            if (value == null)
                return;

            // get some dimensions
            var rows = value.Length;
            var cols = 1;
            foreach (var r in value)
                if (r.Length > cols)
                    cols = r.Length;

            // Grid
            var g = new Grid();
            g.Margin = new Thickness(0, 0, 0, 0);

            var gc1 = new ColumnDefinition();
            gc1.Width = new GridLength(100);
            g.ColumnDefinitions.Add(gc1);

            for (int c = 0; c < cols; c++)
            {
                var gc2 = new ColumnDefinition();
                if (widths[c] == "*")
                    gc2.Width = new GridLength(1.0, GridUnitType.Star);
                else
                if (widths[c] == "#")
                    gc2.Width = new GridLength(1.0, GridUnitType.Auto);
                else
                {
                    if (Int32.TryParse(widths[c], out int i))
                        gc2.Width = new GridLength(i);
                }
                g.ColumnDefinitions.Add(gc2);
            }

            for (int r = 0; r < rows; r++)
            {
                var gr = new RowDefinition();
                gr.Height = new GridLength(1.0, GridUnitType.Auto);
                g.RowDefinitions.Add(gr);
            }

            // Label for key
            var l1 = new Label();
            l1.Margin = new Thickness(0, 0, 0, 0);
            l1.Padding = new Thickness(5, 0, 0, 0);
            l1.Content = "" + key + ":";
            Grid.SetRow(l1, 0);
            Grid.SetColumn(l1, 0);
            g.Children.Add(l1);

            // Label for any values
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var l2 = new Label();
                    l2.Margin = new Thickness(0, 0, 0, 0);
                    l2.Padding = new Thickness(2, 0, 0, 0);
                    l2.Content = "" + value[r][c];
                    Grid.SetRow(l2, 0 + r);
                    Grid.SetColumn(l2, 1 + c);
                    g.Children.Add(l2);
                }

            // in total
            view.Children.Add(g);
        }

        public void AddAction(Panel view, string key, string[] actionStr, ModifyRepo repo = null, Func<object, ModifyRepo.LambdaAction> action = null)
        {
            // access 
            if (repo == null || action == null || actionStr == null)
                return;
            var numButton = actionStr.Length;

            // Grid
            var g = new Grid();
            g.Margin = new Thickness(0, 5, 0, 5);

            // 0 key
            var gc = new ColumnDefinition();
            gc.Width = new GridLength(1.0, GridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 1+x button
            for (int i = 0; i < 1 /* numButton*/ ; i++)
            {
                gc = new ColumnDefinition();
                gc.Width = new GridLength(1.0, GridUnitType.Star);
                g.ColumnDefinitions.Add(gc);
            }

            // 0 row
            var gr = new RowDefinition();
            gr.Height = new GridLength(1.0, GridUnitType.Star);
            g.RowDefinitions.Add(gr);

            // key label
            var x = AddSmallLabelTo(g, 0, 0, margin: new Thickness(5, 0, 0, 0), content: "" + key);
            // x.VerticalContentAlignment = VerticalAlignment.Center;
            x.VerticalAlignment = VerticalAlignment.Center;

            // 1 + action button
#if never
            for (int i = 0; i < numButton; i++)
            {
                int currentI = i;
                repo.RegisterControl(ElemViewAddSmallButtonTo(g, 0, 1+i, margin: new Thickness(0, 0, 5, 0), padding: new Thickness(5,0,5,0), content: "" + actionStr[i]),
                    (o) => {
                        return action(currentI); // button # as argument!
                    });
            }
#else
            var wp = AddSmallWrapPanelTo(g, 0, 1, margin: new Thickness(5, 0, 5, 0));
            for (int i = 0; i < numButton; i++)
            {
                int currentI = i;
                var b = new Button();
                b.Content = "" + actionStr[i];
                b.Margin = new Thickness(2, 2, 2, 2);
                b.Padding = new Thickness(5, 0, 5, 0);
                wp.Children.Add(b);
                repo.RegisterControl(b,
                    (o) =>
                    {
                        return action(currentI); // button # as argument!
                    });
            }
#endif
            // in total
            view.Children.Add(g);
        }

        public void AddAction(StackPanel view, string key, string actionStr, ModifyRepo repo = null, Func<object, ModifyRepo.LambdaAction> action = null)
        {
            AddAction(view, key, new[] { actionStr }, repo, action);
        }

        public void AddKeyListLangStr(StackPanel view, string key, List<AdminShell.LangStr> langStr, ModifyRepo repo = null)
        {
            // sometimes needless to show
            if (repo == null && (langStr == null || langStr.Count < 1))
                return;
            int rows = 1; // default!
            if (langStr != null && langStr.Count > 1)
                rows = langStr.Count;
            int rowOfs = 0;
            if (repo != null)
                rowOfs = 1;

            // Grid
            var g = new Grid();
            g.Margin = new Thickness(0, 0, 0, 0);

            // 0 key
            var gc = new ColumnDefinition();
            gc.Width = new GridLength(100);
            g.ColumnDefinitions.Add(gc);

            // 1 langs
            gc = new ColumnDefinition();
            gc.Width = new GridLength(1.0, GridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 2 values
            gc = new ColumnDefinition();
            gc.Width = new GridLength(1.0, GridUnitType.Star);
            g.ColumnDefinitions.Add(gc);

            // 3 buttons behind it
            gc = new ColumnDefinition();
            gc.Width = new GridLength(1.0, GridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // rows
            for (int r = 0; r < rows + rowOfs; r++)
            {
                var gr = new RowDefinition();
                gr.Height = new GridLength(1.0, GridUnitType.Auto);
                g.RowDefinitions.Add(gr);
            }

            // populate key
            AddSmallLabelTo(g, 0, 0, margin: new Thickness(5, 0, 0, 0), content: "" + key + ":");

            // populate [+]
            if (repo != null)
            {
                repo.RegisterControl(AddSmallButtonTo(g, 0, 3, margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0), content: "Add blank"),
                    (o) =>
                    {
                        var ls = new AdminShell.LangStr();
                        langStr?.Add(ls);
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    });
            }

            // contents?
            if (langStr != null)
                for (int i = 0; i < langStr.Count; i++)
                    if (repo == null)
                    {
                        // lang
                        AddSmallLabelTo(g, 0 + i + rowOfs, 1, padding: new Thickness(2, 0, 0, 0), content: "[" + langStr[i].lang + "]");

                        // str
                        AddSmallLabelTo(g, 0 + i + rowOfs, 2, padding: new Thickness(2, 0, 0, 0), content: "" + langStr[i].str);
                    }

                    else
                    {
                        // save in current context
                        var currentI = 0 + i;

                        // lang   
                        var tbLang = AddSmallComboBoxTo(g, 0 + i + rowOfs, 1, margin: new Thickness(0, 2, 2, 2), text: "" + langStr[currentI].lang,
                                minWidth: 50, items: defaultLanguages, isEditable: true);
                        repo.RegisterControl(tbLang, (o) =>
                            {
                                langStr[currentI].lang = o as string;
                                return new ModifyRepo.LambdaActionNone();
                            });
                        // check here, if to hightlight
                        if (tbLang != null && this.highlightField != null && this.highlightField.fieldHash == langStr[currentI].lang.GetHashCode()
                            && (this.highlightField.containingObject == langStr[currentI]))
                            this.HightligtStateElement(tbLang, true);

                        // str
                        var tbStr = AddSmallTextBoxTo(g, 0 + i + rowOfs, 2, margin: new Thickness(2, 2, 2, 2), text: "" + langStr[currentI].str);
                        repo.RegisterControl(tbStr, (o) =>
                            {
                                langStr[currentI].str = o as string;
                                return new ModifyRepo.LambdaActionNone();
                            });
                        // check here, if to hightlight
                        if (tbStr != null && this.highlightField != null && this.highlightField.fieldHash == langStr[currentI].str.GetHashCode()
                            && (this.highlightField.containingObject == langStr[currentI]))
                            this.HightligtStateElement(tbStr, true);

                        // button [-]
                        repo.RegisterControl(AddSmallButtonTo(g, 0 + i + rowOfs, 3, margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0), content: "-"),
                            (o) =>
                            {
                                langStr.RemoveAt(currentI);
                                return new ModifyRepo.LambdaActionRedrawEntity();
                            });
                    }

            // in total
            view.Children.Add(g);
        }

        public List<AdminShell.Key> SmartSelectAasEntityKeys(AdminShell.AdministrationShellEnv env, string filter = null, AdminShellPackageEnv package = null, AdminShellPackageEnv[] auxPackages = null)
        {
            if (this.flyoutProvider == null)
            {
                var dlg = new SelectAasEntityDialogueByTree(env, filter, package, auxPackages);
                if (dlg.ShowDialog() == true)
                    return dlg.ResultKeys;
            }
            else
            {
                var uc = new SelectAasEntityFlyout(env, filter, package, auxPackages);
                this.flyoutProvider.StartFlyoverModal(uc);
                if (uc.ResultKeys != null)
                    return uc.ResultKeys;
            }
            return null;
        }

        public VisualElementGeneric SmartSelectAasEntityVisualElement(AdminShell.AdministrationShellEnv env, string filter = null, AdminShellPackageEnv package = null, AdminShellPackageEnv[] auxPackages = null)
        {
            if (this.flyoutProvider == null)
            {
                var dlg = new SelectAasEntityDialogueByTree(env, filter, package, auxPackages);
                if (dlg.ShowDialog() == true)
                    return dlg.ResultVisualElement;
            }
            else
            {
                var uc = new SelectAasEntityFlyout(env, filter, package, auxPackages);
                this.flyoutProvider.StartFlyoverModal(uc);
                if (uc.ResultVisualElement != null)
                    return uc.ResultVisualElement;
            }
            return null;
        }

        public bool SmartSelectEclassEntity(SelectEclassEntityFlyout.SelectMode selectMode, ref string resIRDI, ref AdminShell.ConceptDescription resCD)
        {
            var res = false;
            var fullfn = System.IO.Path.GetFullPath(Options.Curr.EclassDir);
            if (this.flyoutProvider != null)
            {
                var uc = new SelectEclassEntityFlyout(fullfn, selectMode);
                this.flyoutProvider.StartFlyoverModal(uc);
                resIRDI = uc.ResultIRDI;
                resCD = uc.ResultCD;
                res = resIRDI != null;
            }
            else
            {
                var dlg = new SelectEclassEntity(fullfn);
                if (dlg.ShowDialog() == true)
                {
                    resIRDI = dlg.ResultIRDI;
                    resCD = dlg.ResultCD;
                    res = true;
                }
            }
            return res;
        }

        /// <summary>
        /// Asks the user for SME element type, allowing exclusion of types.
        /// </summary>
        public AdminShell.SubmodelElementWrapper.AdequateElementEnum SelectAdequateEnum(string caption, AdminShell.SubmodelElementWrapper.AdequateElementEnum[] excludeValues = null)
        {
            // prepare a list
            var fol = new List<SelectFromListFlyoutItem>();
            foreach (var en in AdminShell.SubmodelElementWrapper.GetAdequateEnums(excludeValues))
                fol.Add(new SelectFromListFlyoutItem(AdminShell.SubmodelElementWrapper.GetAdequateName(en), en));

            // prompt for this list
            var uc = new SelectFromListFlyout();
            uc.Caption = caption;
            uc.ListOfItems = fol;
            this.flyoutProvider.StartFlyoverModal(uc);
            if (uc.ResultItem != null && uc.ResultItem.Tag != null && uc.ResultItem.Tag is AdminShell.SubmodelElementWrapper.AdequateElementEnum)
            {
                // to which?
                var en = (AdminShell.SubmodelElementWrapper.AdequateElementEnum)uc.ResultItem.Tag;
                return en;
            }

            return AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown;
        }

        /// <summary>
        /// Asks the user, to which SME to refactor to, create the new SME and returns it.
        /// </summary>
        public AdminShell.SubmodelElement SmartRefactorSme(AdminShell.SubmodelElement oldSme)
        {
            // access
            if (oldSme == null)
                return null;

            // ask
            var en = SelectAdequateEnum($"Refactor {oldSme.GetElementName()} '{"" + oldSme.idShort}' to new element type ..");
            if (en == AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown)
                return null;

            if (this.flyoutProvider != null && MessageBoxResult.Yes == this.flyoutProvider.MessageBoxFlyoutShow("Recfactor selected entity? This operation will change the selected submodel element and delete specific attributes. It can not be reverted!", "AASX", MessageBoxButton.YesNo, MessageBoxImage.Warning))
            {
                try
                {
                    {
                        // which?
                        var refactorSme = AdminShell.SubmodelElementWrapper.CreateAdequateType(en, oldSme);
                        return refactorSme;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Executing refactoring");
                }
            }

            return null;
        }

        public void AddKeyListKeys(
            StackPanel view, string key,
            AdminShell.KeyList keys,
            ModifyRepo repo = null,
            AdminShellPackageEnv package = null,
            string addExistingEntities = null,
            bool addEclassIrdi = false,
            string[] addPresetNames = null, AdminShell.Key[] addPresetKeys = null,
            Func<AdminShell.KeyList, ModifyRepo.LambdaAction> jumpLambda = null,
            ModifyRepo.LambdaAction takeOverLambdaAction = null)
        {
            // sometimes needless to show
            if (repo == null && (keys == null || keys.Count < 1))
                return;
            int rows = 1; // default!
            if (keys != null && keys.Count > 1)
                rows = keys.Count;
            int rowOfs = 0;
            if (repo != null)
                rowOfs = 1;
            if (jumpLambda != null)
                rowOfs = 1;

            // Grid
            var g = new Grid();
            g.Margin = new Thickness(0, 0, 0, 0);

            // 0 key
            var gc = new ColumnDefinition();
            gc.Width = new GridLength(100);
            g.ColumnDefinitions.Add(gc);

            // 1 type
            gc = new ColumnDefinition();
            gc.Width = new GridLength(1.0, GridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 2 local
            gc = new ColumnDefinition();
            gc.Width = new GridLength(1.0, GridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 3 id type
            gc = new ColumnDefinition();
            gc.Width = new GridLength(1.0, GridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 4 value
            gc = new ColumnDefinition();
            gc.Width = new GridLength(1.0, GridUnitType.Star);
            g.ColumnDefinitions.Add(gc);

            // 5 buttons behind it
            gc = new ColumnDefinition();
            gc.Width = new GridLength(1.0, GridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // rows
            for (int r = 0; r < rows + rowOfs; r++)
            {
                var gr = new RowDefinition();
                gr.Height = new GridLength(1.0, GridUnitType.Auto);
                g.RowDefinitions.Add(gr);
            }

            // populate key
            AddSmallLabelTo(g, 0, 0, margin: new Thickness(5, 0, 0, 0), content: "" + key + ":");

            // presets?
            var presetNo = 0;
            if (addPresetNames != null && addPresetKeys != null && addPresetNames.Length == addPresetKeys.Length)
                presetNo = addPresetNames.Length;


            if (repo == null)
            {
                // possibly [Jump] button??
                // TODO
            }
            else
            if (keys != null)
            {
                // populate [+], [Select], [eCl@ss], [Copy] buttons
                var colDescs = new List<string>(new[] { "*", "#", "#", "#", "#" });
                for (int i = 0; i < presetNo; i++)
                    colDescs.Add("#");

                var g2 = AddSmallGrid(1, 5 + presetNo, colDescs.ToArray());
                Grid.SetRow(g2, 0);
                Grid.SetColumn(g2, 1);
                Grid.SetColumnSpan(g2, 5);
                g.Children.Add(g2);

                if (addEclassIrdi)
                    repo.RegisterControl(AddSmallButtonTo(g2, 0, 1, margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0), content: "Add eCl@ss IRDI"),
                        (o) =>
                        {
                            string resIRDI = null;
                            AdminShell.ConceptDescription resCD = null;
                            if (this.SmartSelectEclassEntity(SelectEclassEntityFlyout.SelectMode.IRDI, ref resIRDI, ref resCD))
                            {
                                keys.Add(AdminShell.Key.CreateNew(AdminShell.Key.GlobalReference, false, AdminShell.Identification.IRDI, resIRDI));
                            }
                            if (takeOverLambdaAction != null)
                                return takeOverLambdaAction;
                            else
                                return new ModifyRepo.LambdaActionRedrawEntity();
                        });

                if (addExistingEntities != null && package != null)
                    repo.RegisterControl(AddSmallButtonTo(g2, 0, 2, margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0), content: "Add existing"),
                        (o) =>
                        {
                            var k2 = SmartSelectAasEntityKeys(package.AasEnv, addExistingEntities);
                            if (k2 != null)
                            {
                                keys.AddRange(k2);
                            }
                            if (takeOverLambdaAction != null)
                                return takeOverLambdaAction;
                            else
                                return new ModifyRepo.LambdaActionRedrawEntity();
                        });

                repo.RegisterControl(AddSmallButtonTo(g2, 0, 3, margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0), content: "Add blank"),
                    (o) =>
                    {
                        var k = new AdminShell.Key();
                        keys.Add(k);
                        if (takeOverLambdaAction != null)
                            return takeOverLambdaAction;
                        else
                            return new ModifyRepo.LambdaActionRedrawEntity();
                    });

                repo.RegisterControl(AddSmallButtonTo(g2, 0, 4, margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0), content: "Clipboard"),
                    (o) =>
                    {
                        var st = keys.ToString(format: 1, delimiter: "\r\n");
                        Clipboard.SetText(st);
                        Log.Info("Keys written to clipboard.");
                        return new ModifyRepo.LambdaActionNone();
                    });

                for (int i = 0; i < presetNo; i++)
                {
                    var closureKey = addPresetKeys[i];
                    repo.RegisterControl(AddSmallButtonTo(g2, 0, 5 + i, margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0), content: "" + addPresetNames[i]),
                        (o) =>
                        {
                            keys.Add(closureKey);
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        });
                }
            }

            // contents?
            if (keys != null)
                for (int i = 0; i < keys.Count; i++)
                    if (repo == null)
                    {
                        // lang
                        AddSmallLabelTo(g, 0 + i + rowOfs, 1, padding: new Thickness(2, 0, 0, 0), content: "(" + keys[i].type + ")");

                        // local
                        AddSmallLabelTo(g, 0 + i + rowOfs, 2, padding: new Thickness(2, 0, 0, 0), content: "" + ((keys[i].local) ? "(local)" : "(no-local)"));

                        // id type
                        AddSmallLabelTo(g, 0 + i + rowOfs, 3, padding: new Thickness(2, 0, 0, 0), content: "[" + keys[i].idType + "]");

                        // value
                        AddSmallLabelTo(g, 0 + i + rowOfs, 4, padding: new Thickness(2, 0, 0, 0), content: "" + keys[i].value);
                    }

                    else
                    {
                        // save in current context
                        var currentI = 0 + i;

                        // TODO MIHO

                        // type
                        var cbType = repo.RegisterControl(
                            AddSmallComboBoxTo(g, 0 + i + rowOfs, 1, margin: new Thickness(2, 2, 2, 2), text: "" + keys[currentI].type,
                                minWidth: 100, items: AdminShell.Key.KeyElements, isEditable: false, verticalContentAlignment: VerticalAlignment.Center),
                            (o) =>
                            {
                                keys[currentI].type = o as string;
                                return new ModifyRepo.LambdaActionNone();
                            }, takeOverLambda: takeOverLambdaAction) as ComboBox;
                        SmallComboBoxSelectNearestItem(cbType, cbType.Text);

                        // check here, if to hightlight
                        if (cbType != null && this.highlightField != null && keys[currentI].type != null && this.highlightField.fieldHash == keys[currentI].type.GetHashCode()
                            && keys[currentI] == this.highlightField.containingObject)
                            this.HightligtStateElement(cbType, true);

                        // local
                        repo.RegisterControl(
                            AddSmallCheckBoxTo(g, 0 + i + rowOfs, 2, margin: new Thickness(2, 2, 2, 2), content: "local", isChecked: keys[currentI].local,
                            verticalContentAlignment: VerticalAlignment.Center),
                            (o) =>
                            {
                                keys[currentI].local = (bool)o;
                                return new ModifyRepo.LambdaActionNone();
                            }, takeOverLambda: takeOverLambdaAction);

                        // id type
                        var cbIdType = AddSmallComboBoxTo(g, 0 + i + rowOfs, 3, margin: new Thickness(2, 2, 2, 2), text: "" + keys[currentI].idType,
                                minWidth: 100, items: AdminShell.Key.IdentifierTypeNames, isEditable: false, verticalContentAlignment: VerticalAlignment.Center);
                        repo.RegisterControl(cbIdType, (o) =>
                            {
                                keys[currentI].idType = o as string;
                                return new ModifyRepo.LambdaActionNone();
                            }, takeOverLambda: takeOverLambdaAction);

                        // check here, if to hightlight
                        if (cbIdType != null && this.highlightField != null && keys[currentI].idType != null && this.highlightField.fieldHash == keys[currentI].idType.GetHashCode()
                            && keys[currentI] == this.highlightField.containingObject)
                            this.HightligtStateElement(cbIdType, true);

                        // value
                        var tbValue = AddSmallTextBoxTo(g, 0 + i + rowOfs, 4, margin: new Thickness(2, 2, 2, 2), text: "" + keys[currentI].value,
                            verticalContentAlignment: VerticalAlignment.Center);
                        repo.RegisterControl(tbValue, (o) =>
                            {
                                keys[currentI].value = o as string;
                                return new ModifyRepo.LambdaActionNone();
                            }, takeOverLambda: takeOverLambdaAction);

                        // check here, if to hightlight
                        if (tbValue != null && this.highlightField != null && keys[currentI].value != null && this.highlightField.fieldHash == keys[currentI].value.GetHashCode()
                            && keys[currentI] == this.highlightField.containingObject)
                            this.HightligtStateElement(tbValue, true);

                        // button [-]
                        repo.RegisterControl(AddSmallButtonTo(g, 0 + i + rowOfs, 5, margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0), content: "-"),
                            (o) =>
                            {
                                keys.RemoveAt(currentI);
                                if (takeOverLambdaAction != null)
                                    return takeOverLambdaAction;
                                else
                                    return new ModifyRepo.LambdaActionRedrawEntity();
                            });
                    }

            // in total
            view.Children.Add(g);
        }

        //
        // Safeguarding functions (checking if somethingis null and doing ..)
        //

        public bool SafeguardAccess(StackPanel view, ModifyRepo repo, object data, string key, string actionStr, Func<object, ModifyRepo.LambdaAction> action)
        {
            if (repo != null && data == null)
                AddAction(view, key, actionStr, repo, action);
            return (data != null);
        }

        //
        // List manipulations
        //

        public void MoveElementInListUpwards<T>(List<T> list, T entity)
        {
            if (list == null || list.Count < 2 || entity == null)
                return;
            int ndx = list.IndexOf(entity);
            if (ndx < 1)
                return;
            list.RemoveAt(ndx);
            list.Insert(Math.Max(ndx - 1, 0), entity);
        }

        public void MoveElementInListDownwards<T>(List<T> list, T entity)
        {
            if (list == null || list.Count < 2 || entity == null)
                return;
            int ndx = list.IndexOf(entity);
            if (ndx < 0 || ndx >= list.Count - 1)
                return;
            list.RemoveAt(ndx);
            list.Insert(Math.Min(ndx + 1, list.Count), entity);
        }

        public object DeleteElementInList<T>(List<T> list, T entity, object alternativeReturn)
        {
            if (list == null || entity == null)
                return alternativeReturn;
            int ndx = list.IndexOf(entity);
            if (ndx < 0)
                return alternativeReturn;
            list.RemoveAt(ndx);
            if (ndx > 0)
                return list.ElementAt(ndx - 1);
            return alternativeReturn;
        }

        public void AddElementInListBefore<T>(List<T> list, T entity, T existing)
        {
            if (list == null || list.Count < 1 || entity == null)
                return;
            int ndx = list.IndexOf(existing);
            if (ndx < 0 || ndx >= list.Count - 1)
                return;
            list.Insert(ndx, entity);
        }

        public void AddElementInListAfter<T>(List<T> list, T entity, T existing)
        {
            if (list == null || list.Count < 1 || entity == null)
                return;
            int ndx = list.IndexOf(existing);
            if (ndx < 0 || ndx > list.Count)
                return;
            list.Insert(ndx + 1, entity);
        }

        public void EntityListUpDownDeleteHelper<T>(Panel stack, ModifyRepo repo, List<T> list, T entity, object alternativeFocus, string label = "Entities:", object nextFocus = null)
        {
            if (nextFocus == null)
                nextFocus = entity;
            AddAction(stack, label, new[] { "Move up", "Move down", "Delete" }, repo, (buttonNdx) =>
           {
               if (buttonNdx is int)
               {
                   if ((int)buttonNdx == 0)
                   {
                       MoveElementInListUpwards<T>(list, entity);
                       return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: nextFocus, isExpanded: null);
                   }

                   if ((int)buttonNdx == 1)
                   {
                       MoveElementInListDownwards<T>(list, entity);
                       return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: nextFocus, isExpanded: null);
                   }

                   if ((int)buttonNdx == 2)
                       if (this.flyoutProvider != null && MessageBoxResult.Yes == this.flyoutProvider.MessageBoxFlyoutShow("Delete selected entity? This operation can not be reverted!", "AASX", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                       {
                           var ret = DeleteElementInList<T>(list, entity, alternativeFocus);
                           return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: ret, isExpanded: null);
                       }
               }
               return new ModifyRepo.LambdaActionNone();
           });
        }

        public void QualifierHelper(StackPanel stack, ModifyRepo repo, List<AdminShell.Qualifier> qualifiers)
        {
            if (editMode)
            {
                // let the user control the number of references
                AddAction(stack, "Qualifier entities:", new[] { "Add blank", "Add preset", "Delete last" }, repo, (buttonNdx) =>
               {
                   if (buttonNdx is int)
                   {
                       if ((int)buttonNdx == 0)
                           qualifiers.Add(new AdminShell.Qualifier());

                       if ((int)buttonNdx == 1)
                       {
                           if (Options.Curr.QualifiersFile == null || flyoutProvider == null)
                               return new ModifyRepo.LambdaActionNone();
                           try
                           {
                               var fullfn = System.IO.Path.GetFullPath(Options.Curr.QualifiersFile);
                               var uc = new SelectQualifierPresetFlyout(fullfn);
                               flyoutProvider.StartFlyoverModal(uc);
                               if (uc.ResultQualifier != null)
                                   qualifiers.Add(uc.ResultQualifier);
                           }
                           catch (Exception ex)
                           {
                               Log.Error(ex, $"While show qualifier presets ({Options.Curr.QualifiersFile})");
                           }
                       }

                       if ((int)buttonNdx == 2 && qualifiers.Count > 0)
                           qualifiers.RemoveAt(qualifiers.Count - 1);
                   }
                   return new ModifyRepo.LambdaActionRedrawEntity();
               });
            }

            for (int i = 0; i < qualifiers.Count; i++)
            {
                var qual = qualifiers[i];
                var substack = AddSubStackPanel(stack, "     "); // just a bit spacing to the left

                AddGroup(substack, $"Qualifier {1 + i}", levelColors[2][0], levelColors[2][1], repo, auxButtonTitle: "Delete", auxButtonLambda: (o) =>
                {
                    qualifiers.Remove(qual);
                    return new ModifyRepo.LambdaActionRedrawEntity();
                });

                AddHintBubble(substack, hintMode, new[] {
                        new HintCheck( () => { return (qual.semanticId == null || qual.semanticId.IsEmpty) && (qual.type == null || qual.type.Trim() == ""); },
                            "Either a semanticId or a type string specification shall be given!")
                    });
                if (SafeguardAccess(substack, repo, qual.semanticId, "semanticId:", "Create data element!", v =>
                {
                    qual.semanticId = new AdminShell.SemanticId();
                    return new ModifyRepo.LambdaActionRedrawEntity();
                }))
                {
                    AddKeyListKeys(substack, "semanticId", qual.semanticId.Keys, repo, package, addExistingEntities: AdminShell.Key.AllElements, addEclassIrdi: true);
                }

                AddKeyValueRef(substack, "type", qual, ref qual.type, null, repo, v => { qual.type = v as string; return new ModifyRepo.LambdaActionNone(); });

                AddKeyValueRef(substack, "value", qual, ref qual.value, null, repo, v => { qual.value = v as string; return new ModifyRepo.LambdaActionNone(); });

                if (SafeguardAccess(substack, repo, qual.valueId, "valueId:", "Create data element!", v =>
                {
                    qual.valueId = new AdminShell.Reference();
                    return new ModifyRepo.LambdaActionRedrawEntity();
                }))
                {
                    AddKeyListKeys(substack, "valueId", qual.valueId.Keys, repo, package, AdminShell.Key.AllElements);
                }

            }

        }

        //
        // Identify eCl@ss properties to be imported
        //

        public void IdentifyTargetsForEclassImportOfCDs(AdminShell.AdministrationShellEnv env, List<AdminShell.SubmodelElement> elems, ref List<AdminShell.SubmodelElement> targets)
        {
            if (env == null || targets == null || elems == null)
                return;
            foreach (var elem in elems)
            {
                // sort all the non-fitting
                if (elem.semanticId != null && !elem.semanticId.IsEmpty && elem.semanticId.Count == 1
                    && elem.semanticId[0].type == AdminShell.Key.ConceptDescription
                    && elem.semanticId[0].idType == "IRDI"
                    && elem.semanticId[0].value.StartsWith("0173"))
                {
                    // already in CDs?
                    var x = env.FindConceptDescription(elem.semanticId[0]);
                    if (x == null)
                        // this one has the potential to get imported eCl@ss CD
                        targets.Add(elem);
                }

                // recursion?
                if (elem is AdminShell.SubmodelElementCollection)
                {
                    var childs = AdminShell.SubmodelElementWrapper.ListOfWrappersToListOfElems((elem as AdminShell.SubmodelElementCollection).value);
                    IdentifyTargetsForEclassImportOfCDs(env, childs, ref targets);
                }
            }
        }

        /*
        public class  ImportEclassArgs
        {
            public EclassUtils.SearchJobData jobData;
            public ProgressBar progress;
        }
        */

        public bool ImportEclassCDsForTargets(AdminShell.AdministrationShellEnv env, object startMainDataElement, List<AdminShell.SubmodelElement> targets)
        {
            // need dialogue and data
            if (this.flyoutProvider == null || env == null || targets == null)
                return false;

            // use eCl@ss utilities 
            var fullfn = System.IO.Path.GetFullPath(Options.Curr.EclassDir);
            var jobData = new EclassUtils.SearchJobData(fullfn);
            foreach (var t in targets)
                if (t != null && t.semanticId != null && t.semanticId.Count == 1 && t.semanticId[0].idType == "IRDI")
                    jobData.searchIRDIs.Add(t.semanticId[0].value.ToLower().Trim());
            // still valid?
            if (jobData.searchIRDIs.Count < 1)
                return false;

            // make a progress flyout
            var uc = new ProgressBarFlyout("Import ConceptDescriptions from eCl@ss", "Preparing ...", MessageBoxImage.Information);
            uc.Progress = 0.0;
            // show this
            this.flyoutProvider.StartFlyover(uc);

            // setup worker
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                // job data
                /*
                var args2 = e.Argument as ImportEclassArgs;
                if (args2 == null)
                    return;
                */

                // longrunnig task for searching IRDIs ..
                uc.Info = "Collecting eCl@ss Data ..";
                EclassUtils.SearchForIRDIinEclassFiles(jobData, (frac) =>
                {
                    uc.Progress = frac;
                });

                // apply to targets
                uc.Info = "Adding missing ConceptDescriptions ..";
                uc.Progress = 0.0;
                for (int i = 0; i < targets.Count; i++)
                {
                    // progress
                    uc.Progress = (1.0 / targets.Count) * i;

                    // access
                    var t = targets[i];
                    if (t.semanticId == null || t.semanticId.Count != 1)
                        continue;

                    // CD
                    var newcd = EclassUtils.GenerateConceptDescription(jobData.items, t.semanticId[0].value);
                    if (newcd == null)
                        continue;

                    // add?
                    if (null == env.FindConceptDescription(AdminShell.Key.CreateNew(AdminShell.Key.ConceptDescription, true, newcd.identification.idType, newcd.identification.id)))
                        env.ConceptDescriptions.Add(newcd);
                }
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                // in any case, close flyover
                if (flyoutProvider != null)
                    flyoutProvider.CloseFlyover();

                // redraw everything
                repo.AddWishForAction(new ModifyRepo.LambdaActionRedrawAllElements(startMainDataElement));
            };
            worker.RunWorkerAsync();

            // ok
            return true;
        }

        /*
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // in any case, close flyover
            if (flyoutProvider != null)
                flyoutProvider.CloseFlyover();
        }

        private void ImportEclassWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // job data
            var args = e.Argument as ImportEclassArgs;
            if (args == null)
                return;

            // longrunnig task for searching IRDIs ..
            EclassUtils.SearchForIRDIinEclassFiles(args.jobData, (frac) => {
                args.progress.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => args.progress.Value = frac));
            });
        }
        */

        //
        // Hinting
        //

        public void AddHintBubble(StackPanel view, bool hintMode, HintCheck[] hints)
        {
            // access
            if (!hintMode || view == null || hints == null)
                return;

            // check, if something to do. Execute all predicates
            List<string> textsToShow = new List<string>();
            HintCheck.Severity highestSev = HintCheck.Severity.Notice;
            foreach (var hc in hints)
                if (hc.CheckPred != null && hc.TextToShow != null)
                {
                    try
                    {
                        if (hc.CheckPred())
                        {
                            textsToShow.Add(hc.TextToShow);
                            if (hc.SeverityLevel == HintCheck.Severity.High)
                                highestSev = HintCheck.Severity.High;
                            if (hc.BreakIfTrue)
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        textsToShow.Add($"Error while checking hints: {ex.Message} at {AdminShellUtil.ShortLocation(ex)}");
                        highestSev = HintCheck.Severity.High;
                    }
                }

            // some?
            if (textsToShow.Count < 1)
                return;

            // show!
            var bubble = new HintBubble();
            bubble.Margin = new Thickness(2, 4, 2, 0);
            bubble.Text = string.Join("\r\n", textsToShow);
            if (highestSev == HintCheck.Severity.High)
            {
                bubble.Background = (SolidColorBrush)System.Windows.Application.Current.Resources["FocusErrorBrush"];
                bubble.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ffffff"));
            }
            if (highestSev == HintCheck.Severity.Notice)
            {
                bubble.Background = (SolidColorBrush)System.Windows.Application.Current.Resources["LightAccentColor"];
                bubble.Foreground = (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"];
            }
            view.Children.Add(bubble);
        }

        public void AddHintBubble(StackPanel view, bool hintMode, HintCheck hint)
        {
            AddHintBubble(view, hintMode, new[] { hint });
        }

    }
}
