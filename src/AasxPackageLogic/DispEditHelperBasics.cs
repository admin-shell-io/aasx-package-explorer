/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AasCore.Aas3_0_RC02;
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AdminShellNS.DiaryData;
using AdminShellNS.Extenstions;
using AnyUi;
using Extensions;
using Newtonsoft.Json;

namespace AasxPackageLogic
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
        /// <param name="breakIfTrue">If check was true, abort checking of further hints.
        /// Use: avoid checking of null for every hint.</param>
        /// <param name="severityLevel">Display high/red or normal/blue</param>
        public HintCheck(
            Func<bool> check, string text, bool breakIfTrue = false, Severity severityLevel = Severity.High)
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
    // Color palette
    //    

    public class DispLevelColors
    {
        public AnyUiBrushTuple
            MainSection, SubSection, SubSubSection,
            HintSeverityHigh, HintSeverityNotice;

        public static DispLevelColors GetLevelColorsFromOptions(OptionsInformation opt)
        {
            // access
            if (opt == null)
                return null;

            // ReSharper disable CoVariantArrayConversion            
            var res = new DispLevelColors()
            {
                MainSection = new AnyUiBrushTuple(
                    new AnyUiBrush(opt.GetColor(OptionsInformation.ColorNames.DarkestAccentColor)),
                    AnyUiBrushes.White),
                SubSection = new AnyUiBrushTuple(
                    new AnyUiBrush(opt.GetColor(OptionsInformation.ColorNames.DarkAccentColor)),
                    AnyUiBrushes.Black),
                SubSubSection = new AnyUiBrushTuple(
                    new AnyUiBrush(opt.GetColor(OptionsInformation.ColorNames.DarkAccentColor)),
                    AnyUiBrushes.Black),
                HintSeverityHigh = new AnyUiBrushTuple(
                    new AnyUiBrush(opt.GetColor(OptionsInformation.ColorNames.FocusErrorBrush)),
                    AnyUiBrushes.White),
                HintSeverityNotice = new AnyUiBrushTuple(
                    new AnyUiBrush(opt.GetColor(OptionsInformation.ColorNames.DarkAccentColor)),
                    new AnyUiBrush(opt.GetColor(OptionsInformation.ColorNames.DarkestAccentColor)))
            };
            // ReSharper enable CoVariantArrayConversion
            return res;
        }
    }

    //
    // Helpers
    //

    // ReSharper disable once UnusedType.Global
    public class DispEditHelperBasics : AnyUiSmallWidgetToolkit
    {
        //
        // Members
        //

        private string[] defaultLanguages = new[] { "en", "de", "fr", "es", "it", "zh", "kr", "jp" };

        public PackageCentral.PackageCentral packages = null;
        public IPushApplicationEvent appEventsProvider = null;

        public DispLevelColors levelColors = null;

        public int standardFirstColWidth = 96;
        public int smallFirstColWidth = 96 / 2;

        public bool editMode = false;
        public bool hintMode = false;
        public bool showIriMode = false;

        public ModifyRepo repo = null;

        public DispEditHighlight.HighlightFieldInfo highlightField = null;
        private AnyUiFrameworkElement lastHighlightedField = null;

        public AnyUiContextBase context = null;

        //
        // Highlighting
        //

        public void HighligtStateElement(AnyUiFrameworkElement fe, bool highlighted)
        {
            // access
            if (fe == null)
                return;

            // save
            if (highlighted)
                this.lastHighlightedField = fe;

        }

        /// <summary>
        /// During renderig, the last highlighted field will be identified.
        /// This function will perform the rendering; presuming that the controls
        /// are already displayed by the implementation technology.
        /// </summary>
        public void ShowLastHighlights()
        {
            // any highlighted?
            if (this.lastHighlightedField == null)
                return;

            // execute
            // be a little careful
            try
            {
                this.context?.HighlightElement(this.lastHighlightedField, true);
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

        }

        public void ClearHighlights()
        {
            try
            {
                if (this.lastHighlightedField == null)
                    return;
                HighligtStateElement(this.lastHighlightedField, highlighted: false);
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }
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
            ModifyRepo repo = null,
            string auxButtonTitle = null, Func<object, AnyUiLambdaActionBase> auxButtonLambda = null)
        {
            AddGroup(view, name, colors?.Bg, colors?.Fg, repo, auxButtonTitle, auxButtonLambda);
        }

        public void AddGroup(AnyUiStackPanel view, string name, AnyUiBrush background, AnyUiBrush foreground,
            ModifyRepo repo = null,
            string auxButtonTitle = null, Func<object, AnyUiLambdaActionBase> auxButtonLambda = null)
        {
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 13, 0, 0);

            var gc1 = new AnyUiColumnDefinition();
            gc1.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.ColumnDefinitions.Add(gc1);

            var gr1 = new AnyUiRowDefinition();
            gr1.Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
            g.RowDefinitions.Add(gr1);

            var auxButton = repo != null && auxButtonTitle != null && auxButtonLambda != null;
            if (auxButton)
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
        }

        public void AddGroup(AnyUiStackPanel view, string name, AnyUiBrush background, AnyUiBrush foreground,
            ModifyRepo repo,
            string contextMenuText, string[] menuHeaders, Func<object, AnyUiLambdaActionBase> menuItemLambda,
            AnyUiThickness margin = null, AnyUiThickness padding = null)
        {
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 13, 0, 0);

            var gc1 = new AnyUiColumnDefinition();
            gc1.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.ColumnDefinitions.Add(gc1);

            var isContextMenu = repo != null && contextMenuText != null
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
            if (setNoWrap)
                lab.TextWrapping = AnyUiTextWrapping.NoWrap;
            lab.Text = content;

            // check, which content
            if (this.showIriMode
                && content.HasContent()
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


#if ONLY_INFO
        // This was used in former times and now replaced by using a set lambda in all times

        public void AddKeyValueRef(
            AnyUiStackPanel view, string key, object containingObject, ref string value, string nullValue = null,
            ModifyRepo repo = null, Func<object, AnyUiLambdaActionBase> setValue = null,
            string[] comboBoxItems = null, bool comboBoxIsEditable = false,
            string auxButtonTitle = null, Func<int, AnyUiLambdaActionBase> auxButtonLambda = null,
            string auxButtonToolTip = null,
            string[] auxButtonTitles = null,
            string[] auxButtonToolTips = null,
            AnyUiLambdaActionBase takeOverLambdaAction = null,
            bool limitToOneRowForNoEdit = false)
        {
            AddKeyValue(
                view, key, value, nullValue, repo, setValue, comboBoxItems, comboBoxIsEditable,
                auxButtonTitle, auxButtonLambda, auxButtonToolTip,
                auxButtonTitles, auxButtonToolTips, takeOverLambdaAction,
                (value == null) ? 0 : value.GetHashCode(), containingObject: containingObject,
                limitToOneRowForNoEdit: limitToOneRowForNoEdit);
        }
#endif

        /// <summary>
        /// This is a plain wrapper for <c>AddKeyValue</c> and <c>AddKeyValueRef</c>.
        /// Background is that in former times a reference was required; however, this is now
        /// for yoears done with the <c>setValue</c> lambda.
        /// Both this function and <c>AddKeyValue</c> are functionally equivalent.
        /// </summary>
        /// <param name="view">The <c>AnyUiView</c> the widget shall be added to</param>
        /// <param name="key">Label to be displayed in fron of editing field</param>
        /// <param name="containingObject">Contiaing object (for find/replace function)</param>
        /// <param name="value">Stringified value of the variable</param>
        /// <param name="nullValue">String if the value happens to be null</param>
        /// <param name="repo">Repository link. Used to mark the edit mode.</param>
        /// <param name="setValue">Lambda activiated, if variable is changed</param>
        /// <param name="comboBoxItems">If <c>null</c> displays a combo box</param>
        /// <param name="comboBoxIsEditable">True, if combobox choices can also be editied</param>
        /// <param name="auxButtonTitle">Legacy. If there is a single auxiliary button, name of the button</param>
        /// <param name="auxButtonLambda">Legacy. Lambda for that single button</param>
        /// <param name="auxButtonToolTip">Legacy. Tooltip for that single button.</param>
        /// <param name="auxButtonTitles">Array of button titles to be offered.</param>
        /// <param name="auxButtonToolTips">Array of tool tips for that buttons.</param>
        /// <param name="takeOverLambdaAction">Lambda called at the end of a modification.</param>
        /// <param name="limitToOneRowForNoEdit">Limitation for displaying multiple lines of value</param>
        public void AddKeyValueExRef(
            AnyUiStackPanel view, string key, object containingObject, string value, string nullValue = null,
            ModifyRepo repo = null, Func<object, AnyUiLambdaActionBase> setValue = null,
            string[] comboBoxItems = null, bool comboBoxIsEditable = false,
            string auxButtonTitle = null, Func<int, AnyUiLambdaActionBase> auxButtonLambda = null,
            string auxButtonToolTip = null,
            string[] auxButtonTitles = null,
            string[] auxButtonToolTips = null,
            AnyUiLambdaActionBase takeOverLambdaAction = null,
            bool limitToOneRowForNoEdit = false,
            int comboBoxMinWidth = -1)
        {
            AddKeyValue(
                view, key, value, nullValue, repo, setValue, comboBoxItems, comboBoxIsEditable,
                auxButtonTitle, auxButtonLambda, auxButtonToolTip,
                auxButtonTitles, auxButtonToolTips, takeOverLambdaAction,
                (value == null) ? 0 : value.GetHashCode(), containingObject: containingObject,
                limitToOneRowForNoEdit: limitToOneRowForNoEdit,
                comboBoxMinWidth: comboBoxMinWidth);
        }

        /// <summary>
        /// Allow editing a plain content variable. The variable content is fed into by <c>value</c>.
        /// If the variable is changed, the lambda <c>setValue</c> is activated.
        /// </summary>
        /// <param name="view">The <c>AnyUiView</c> the widget shall be added to</param>
        /// <param name="key">Label to be displayed in fron of editing field</param>
        /// <param name="value">Stringified value of the variable</param>
        /// <param name="nullValue">String if the value happens to be null</param>
        /// <param name="repo">Repository link. Used to mark the edit mode.</param>
        /// <param name="setValue">Lambda activiated, if variable is changed</param>
        /// <param name="comboBoxItems">If <c>null</c> displays a combo box</param>
        /// <param name="comboBoxIsEditable">True, if combobox choices can also be editied</param>
        /// <param name="auxButtonTitle">Legacy. If there is a single auxiliary button, name of the button</param>
        /// <param name="auxButtonLambda">Legacy. Lambda for that single button</param>
        /// <param name="auxButtonToolTip">Legacy. Tooltip for that single button.</param>
        /// <param name="auxButtonTitles">Array of button titles to be offered.</param>
        /// <param name="auxButtonToolTips">Array of tool tips for that buttons.</param>
        /// <param name="takeOverLambdaAction">Lambda called at the end of a modification.</param>
        /// <param name="valueHash">Hash value of the variable (for find/replace function)</param>
        /// <param name="containingObject">Contiaing object (for find/replace function)</param>
        /// <param name="limitToOneRowForNoEdit">Limitation for displaying multiple lines of value</param>
        /// <param name="comboBoxMinWidth">Minimal width if value is edited by combo box</param>
        public void AddKeyValue(
            AnyUiStackPanel view, string key, string value, string nullValue = null,
            ModifyRepo repo = null, Func<object, AnyUiLambdaActionBase> setValue = null,
            string[] comboBoxItems = null, bool comboBoxIsEditable = false,
            string auxButtonTitle = null, Func<int, AnyUiLambdaActionBase> auxButtonLambda = null,
            string auxButtonToolTip = null,
            string[] auxButtonTitles = null, string[] auxButtonToolTips = null,
            AnyUiLambdaActionBase takeOverLambdaAction = null,
            Nullable<int> valueHash = null,
            object containingObject = null,
            bool limitToOneRowForNoEdit = false,
            int comboBoxMinWidth = -1)
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
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 1, 0, 1);
            var gc1 = new AnyUiColumnDefinition();
            gc1.Width = AnyUiGridLength.Auto;
            gc1.MinWidth = this.standardFirstColWidth;
            g.ColumnDefinitions.Add(gc1);
            var gc2 = new AnyUiColumnDefinition();
            gc2.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.ColumnDefinitions.Add(gc2);

            if (auxButton)
                for (int i = 0; i < intButtonTitles.Count; i++)
                {
                    var gc3 = new AnyUiColumnDefinition();
                    gc3.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                    g.ColumnDefinitions.Add(gc3);
                }

            // Label for key
            AddSmallLabelTo(g, 0, 0, padding: new AnyUiThickness(5, 0, 0, 0), content: "" + key + ":");

            // Label / TextBox for value
            if (repo == null)
            {
                if (limitToOneRowForNoEdit)
                    value = AdminShellUtil.RemoveNewLinesAndLimit("" + value, 120, ellipsis: "\u2026");
                AddSmallLabelTo(g, 0, 1, padding: new AnyUiThickness(4, 0, 0, 0), content: "" + value);
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
                var cb = AddSmallComboBoxTo(
                    g, 0, 1,
                    margin: new AnyUiThickness(4, 2, 2, 2),
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    text: "" + value,
                    minWidth: Math.Max(60, comboBoxMinWidth),
                    maxWidth: maxWidth,
                    items: comboBoxItems,
                    isEditable: comboBoxIsEditable);
                AnyUiUIElement.RegisterControl(cb, setValue, takeOverLambda: takeOverLambdaAction);

                // check here, if to hightlight
                if (cb != null && this.highlightField != null && valueHash != null &&
                        this.highlightField.fieldHash == valueHash.Value &&
                        (containingObject == null || containingObject == this.highlightField.containingObject))
                    this.HighligtStateElement(cb, true);
            }
            else
            {
                // use plain text box
                var tb = AddSmallTextBoxTo(g, 0, 1, margin: new AnyUiThickness(4, 2, 2, 2), text: "" + value);
                AnyUiUIElement.RegisterControl(tb,
                    setValue, takeOverLambda: takeOverLambdaAction);

                // check here, if to hightlight
                if (tb != null && this.highlightField != null && valueHash != null &&
                        this.highlightField.fieldHash == valueHash.Value &&
                        (containingObject == null || containingObject == this.highlightField.containingObject))
                    this.HighligtStateElement(tb, true);
            }

            if (auxButton)
                for (int i = 0; i < intButtonTitles.Count; i++)
                {
                    Func<object, AnyUiLambdaActionBase> lmb = null;
                    int closureI = i;
                    if (auxButtonLambda != null)
                        lmb = (o) =>
                        {
                            return auxButtonLambda(closureI); // exchange o with i !!
                        };
                    var b = AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g, 0, 2 + i,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            content: intButtonTitles[i]),
                        lmb) as AnyUiButton;
                    if (i < intButtonToolTips.Count)
                        b.ToolTip = intButtonToolTips[i];
                }

            // in total
            view.Children.Add(g);
        }



        public void AddKeyDropTarget(
            AnyUiStackPanel view, string key, string value, string nullValue = null,
            ModifyRepo repo = null, Func<object, AnyUiLambdaActionBase> setValue = null, int minHeight = 0)
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

            // Grid
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 1, 0, 1);
            var gc1 = new AnyUiColumnDefinition();
            gc1.Width = new AnyUiGridLength(this.standardFirstColWidth);
            g.ColumnDefinitions.Add(gc1);
            var gc2 = new AnyUiColumnDefinition();
            gc2.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.ColumnDefinitions.Add(gc2);

            // Label for key
            AddSmallLabelTo(g, 0, 0, padding: new AnyUiThickness(5, 0, 0, 0), content: "" + key + ":");

            // Label / TextBox for value
            if (repo == null)
            {
                // view only
                AddSmallLabelTo(g, 0, 1, padding: new AnyUiThickness(2, 0, 0, 0), content: "" + value);
            }
            else
            {
                // interactive
                var brd = AddSmallDropBoxTo(g, 0, 1, margin: new AnyUiThickness(2, 2, 2, 2),
                    borderThickness: new AnyUiThickness(1), text: "" + value, minHeight: minHeight);
                AnyUiUIElement.RegisterControl(brd,
                    setValue);
            }

            // in total
            view.Children.Add(g);
        }

        public void AddKeyMultiValue(AnyUiStackPanel view, string key, string[][] value, string[] widths)
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
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 0, 0, 0);

            var gc1 = new AnyUiColumnDefinition();
            gc1.Width = AnyUiGridLength.Auto;
            gc1.MinWidth = this.standardFirstColWidth;
            g.ColumnDefinitions.Add(gc1);

            for (int c = 0; c < cols; c++)
            {
                var gc2 = new AnyUiColumnDefinition();
                if (widths[c] == "*")
                    gc2.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
                else
                if (widths[c] == "#")
                    gc2.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                else
                {
                    if (Int32.TryParse(widths[c], out int i))
                        gc2.Width = new AnyUiGridLength(i);
                }
                g.ColumnDefinitions.Add(gc2);
            }

            for (int r = 0; r < rows; r++)
            {
                var gr = new AnyUiRowDefinition();
                gr.Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                g.RowDefinitions.Add(gr);
            }

            // Label for key
            var l1 = new AnyUiLabel();
            l1.Margin = new AnyUiThickness(0, 0, 0, 0);
            l1.Padding = new AnyUiThickness(5, 0, 0, 0);
            l1.Content = "" + key + ":";
            AnyUiGrid.SetRow(l1, 0);
            AnyUiGrid.SetColumn(l1, 0);
            g.Children.Add(l1);

            // Label for any values
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var l2 = new AnyUiLabel();
                    l2.Margin = new AnyUiThickness(0, 0, 0, 0);
                    l2.Padding = new AnyUiThickness(2, 0, 0, 0);
                    l2.Content = "" + value[r][c];
                    AnyUiGrid.SetRow(l2, 0 + r);
                    AnyUiGrid.SetColumn(l2, 1 + c);
                    g.Children.Add(l2);
                }

            // in total
            view.Children.Add(g);
        }

        public void AddCheckBox(AnyUiStackPanel panel, string key, bool initialValue, string additionalInfo = "",
                Action<bool> valueChanged = null)
        {
            // make grid
            var g = this.AddSmallGrid(1, 2, new[] { "" + this.standardFirstColWidth + ":", "*" },
                    margin: new AnyUiThickness(0, 2, 0, 0));

            // Column 0 = Key
            this.AddSmallLabelTo(g, 0, 0, padding: new AnyUiThickness(5, 0, 0, 0), content: key);

            // Column 1 = Check box or info
            if (repo == null || valueChanged == null)
            {
                this.AddSmallLabelTo(g, 0, 1, padding: new AnyUiThickness(2, 0, 0, 0),
                        content: initialValue ? "True" : "False");
            }
            else
            {
                AnyUiUIElement.RegisterControl(this.AddSmallCheckBoxTo(g, 0, 1, margin: new AnyUiThickness(2, 2, 2, 2),
                    content: additionalInfo, verticalContentAlignment: AnyUiVerticalAlignment.Center,
                    isChecked: initialValue),
                        (o) =>
                        {
                            if (o is bool)
                                valueChanged((bool)o);
                            return new AnyUiLambdaActionNone();
                        });
            }

            // add
            panel.Children.Add(g);
        }

        public void AddAction(
            AnyUiPanel view, string key, string[] actionStr, ModifyRepo repo = null,            
            Func<int, AnyUiLambdaActionBase> action = null,
            string[] actionTags = null,
            bool[] addWoEdit = null,
            string[] actionToolTips = null)
        {
            // access 
            if (action == null || actionStr == null)
                return;
            if (repo == null && addWoEdit == null)
                return;
            var numButton = actionStr.Length;

            // Grid
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 5, 0, 5);

            // 0 key
            var gc = new AnyUiColumnDefinition();
            gc.Width = AnyUiGridLength.Auto;
            gc.MinWidth = this.standardFirstColWidth;
            g.ColumnDefinitions.Add(gc);

            // 1+x button
            for (int i = 0; i < 1 /* numButton*/ ; i++)
            {
                gc = new AnyUiColumnDefinition();
                gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
                g.ColumnDefinitions.Add(gc);
            }

            // 0 row
            var gr = new AnyUiRowDefinition();
            gr.Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.RowDefinitions.Add(gr);

            // key label
            var x = AddSmallLabelTo(g, 0, 0, margin: new AnyUiThickness(5, 0, 0, 0),
                setNoWrap: true,
                content: "" + key);
            x.VerticalAlignment = AnyUiVerticalAlignment.Center;

            // 1 + action button
            var wp = AddSmallWrapPanelTo(g, 0, 1, margin: new AnyUiThickness(4, 0, 4, 0));
            for (int i = 0; i < numButton; i++)
            {
                // render?
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (!((repo != null)
                      || (addWoEdit != null && addWoEdit.Length > i && addWoEdit[i])
                     ))
                    continue;

                // render?
                int currentI = i;
                var b = new AnyUiButton();
                b.Content = "" + actionStr[i];
                if (actionToolTips != null && actionToolTips.Length > i)
                    b.ToolTip = actionToolTips[i];
                b.Margin = new AnyUiThickness(0, 2, 4, 2);
                b.Padding = new AnyUiThickness(5, 0, 5, 0);
                wp.Children.Add(b);
                AnyUiUIElement.RegisterControl(b,
                    (o) =>
                    {
                        return action(currentI); // button # as argument!
                    });

                if (actionTags != null && i < actionTags.Length)
                    AnyUiUIElement.NameControl(b, actionTags[i]);
            }

            // in total
            view.Children.Add(g);
        }

        public void AddAction(
            AnyUiStackPanel view, string key, string actionStr, ModifyRepo repo = null,
            Func<int, AnyUiLambdaActionBase> action = null)
        {
            AddAction(view, key, new[] { actionStr }, repo, action);
        }

        public void AddKeyListLangStr(
            AnyUiStackPanel view, string key, List<LangString> langStr, ModifyRepo repo = null,
            IReferable relatedReferable = null)
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
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 0, 0, 0);

            // 0 key
            var gc = new AnyUiColumnDefinition();
            gc.Width = AnyUiGridLength.Auto;
            gc.MinWidth = this.standardFirstColWidth;
            g.ColumnDefinitions.Add(gc);

            // 1 langs
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 2 values
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.ColumnDefinitions.Add(gc);

            // 3 buttons behind it
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // rows
            for (int r = 0; r < rows + rowOfs; r++)
            {
                var gr = new AnyUiRowDefinition();
                gr.Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                g.RowDefinitions.Add(gr);
            }

            // populate key
            AddSmallLabelTo(g, 0, 0, margin: new AnyUiThickness(5, 0, 0, 0),
                setNoWrap: true,
                content: "" + key + ":");

            // populate [+]
            if (repo != null)
            {
                AnyUiUIElement.RegisterControl(
                    AddSmallButtonTo(
                        g, 0, 3,
                        margin: new AnyUiThickness(2, 2, 2, 2),
                        padding: new AnyUiThickness(5, 0, 5, 0),
                        content: "Add blank"),
                    (o) =>
                    {
                        var ls = new LangString("","");
                        langStr?.Add(ls);
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    });
            }

            // contents?
            if (langStr != null)
                for (int i = 0; i < langStr.Count; i++)
                    if (repo == null)
                    {
                        // lang
                        AddSmallLabelTo(
                            g, 0 + i + rowOfs, 1,
                            margin: new AnyUiThickness(4, 0, 0, 0),
                            padding: new AnyUiThickness(2, 0, 0, 0),
                            setNoWrap: true,
                            content: "[" + langStr[i].Language + "]");

                        // str
                        AddSmallLabelTo(
                            g, 0 + i + rowOfs, 2,
                            padding: new AnyUiThickness(2, 0, 0, 0),
                            content: "" + langStr[i].Text);
                    }
                    else
                    {
                        // save in current context
                        var currentI = 0 + i;

                        // lang
                        var tbLang = AddSmallComboBoxTo(
                            g, 0 + i + rowOfs, 1,
                            margin: new AnyUiThickness(4, 2, 2, 2),
                            padding: new AnyUiThickness(0, -1, 0, -1),
                            text: "" + langStr[currentI].Language,
                            minWidth: 60,
                            items: defaultLanguages,
                            isEditable: true);
                        AnyUiUIElement.RegisterControl(
                            tbLang,
                            (o) =>
                            {
                                langStr[currentI].Language = o as string;
                                this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionNone();
                            });
                        // check here, if to hightlight
                        if (tbLang != null && this.highlightField != null &&
                                this.highlightField.fieldHash == langStr[currentI].Language.GetHashCode() &&
                                (this.highlightField.containingObject == langStr[currentI]))
                            this.HighligtStateElement(tbLang, true);

                        // str
                        var tbStr = AddSmallTextBoxTo(
                            g, 0 + i + rowOfs, 2,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center,
                            text: "" + langStr[currentI].Text);
                        AnyUiUIElement.RegisterControl(
                            tbStr,
                            (o) =>
                            {
                                langStr[currentI].Text = o as string;
                                this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionNone();
                            });
                        // check here, if to hightlight
                        if (tbStr != null && this.highlightField != null &&
                                this.highlightField.fieldHash == langStr[currentI].Text.GetHashCode() &&
                                (this.highlightField.containingObject == langStr[currentI]))
                            this.HighligtStateElement(tbStr, true);

                        // button [-]
                        AnyUiUIElement.RegisterControl(
                            AddSmallButtonTo(
                                g, 0 + i + rowOfs, 3,
                                margin: new AnyUiThickness(2, 2, 2, 2),
                                padding: new AnyUiThickness(5, 0, 5, 0),
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                content: "-"),
                            (o) =>
                            {
                                langStr.RemoveAt(currentI);
                                this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawEntity();
                            });
                    }

            // in total
            view.Children.Add(g);
        }

        public List<Key> SmartSelectAasEntityKeys(
            PackageCentral.PackageCentral packages,
            PackageCentral.PackageCentral.Selector selector, string filter = null)
        {
            var uc = new AnyUiDialogueDataSelectAasEntity(
                caption: "Select entity of AAS ..",
                selector: selector, filter: filter);
            this.context.StartFlyoverModal(uc);
            if (uc.Result && uc.ResultKeys != null)
                return uc.ResultKeys;

            return null;
        }

        public VisualElementGeneric SmartSelectAasEntityVisualElement(
            PackageCentral.PackageCentral packages,
            PackageCentral.PackageCentral.Selector selector,
            string filter = null)
        {
            var uc = new AnyUiDialogueDataSelectAasEntity(
                caption: "Select entity of AAS ..",
                selector: selector, filter: filter);
            this.context.StartFlyoverModal(uc);
            if (uc.Result && uc.ResultVisualElement != null)
                return uc.ResultVisualElement;

            return null;
        }

        public bool SmartSelectEclassEntity(
            AnyUiDialogueDataSelectEclassEntity.SelectMode mode, ref string resIRDI,
            ref ConceptDescription resCD)
        {
            var res = false;

            // TODO (MIHO, 2020-12-21): function & if-clause is obsolete
            var uc = new AnyUiDialogueDataSelectEclassEntity("Select ECLASS entity ..",
                mode: mode);
            this.context.StartFlyoverModal(uc);
            resIRDI = uc.ResultIRDI;
            resCD = uc.ResultCD;
            res = resIRDI != null;

            return res;
        }

        /// <summary>
        /// Asks the user for SME element type, allowing exclusion of types.
        /// </summary>
        public AasSubmodelElements SelectAdequateEnum(
            string caption, AasSubmodelElements[] excludeValues = null,
            AasSubmodelElements[] includeValues = null)
        {
            // prepare a list
            var fol = new List<AnyUiDialogueListItem>();
            foreach (var en in AdminShellUtil.GetAdequateEnums(excludeValues, includeValues))
                fol.Add(new AnyUiDialogueListItem(Enum.GetName(typeof(AasSubmodelElements), en), en));

            // prompt for this list
            var uc = new AnyUiDialogueDataSelectFromList(
                caption: caption);
            uc.ListOfItems = fol;
            this.context.StartFlyoverModal(uc);
            if (uc.Result && uc.ResultItem != null && uc.ResultItem.Tag != null &&
                    uc.ResultItem.Tag is AasSubmodelElements)
            {
                // to which?
                var en = (AasSubmodelElements)uc.ResultItem.Tag;
                return en;
            }

            return AasSubmodelElements.SubmodelElement;
        }

        /// <summary>
        /// Asks the user, to which SME to refactor to, create the new SME and returns it.
        /// </summary>
        public ISubmodelElement SmartRefactorSme(ISubmodelElement oldSme)
        {
            // access
            if (oldSme == null)
                return null;

            // ask
            var en = SelectAdequateEnum(
                $"Refactor {oldSme.GetSelfDescription().AasElementName} '{"" + oldSme.IdShort}' to new element type ..");
            if (en == AasSubmodelElements.SubmodelElement)
                return null;

            if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                "Recfactor selected entity? " +
                    "This operation will change the selected submodel element and " +
                    "delete specific attributes. It can not be reverted!",
                "AASX", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
            {
                try
                {
                    {
                        // which?
                        var refactorSme = AdminShellUtil.CreateSubmodelElementFromEnum(en, oldSme);
                        return refactorSme;
                    }
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "Executing refactoring");
                }
            }

            return null;
        }

        // see below
        public void AddKeyListOfIdentifier(
            AnyUiStackPanel view, string key,
            List<string> keys,
            ModifyRepo repo = null,
            PackageCentral.PackageCentral packages = null,
            PackageCentral.PackageCentral.Selector selector = PackageCentral.PackageCentral.Selector.Main,
            string addExistingEntities = null,
            bool addEclassIrdi = false,
            bool addFromPool = false,
            string[] addPresetNames = null, List<string>[] addPresetKeyLists = null,
            Func<int, AnyUiLambdaActionBase> auxButtonLambda = null,
            string[] auxButtonTitles = null, string[] auxButtonToolTips = null,
            Func<List<string>, AnyUiLambdaActionBase> jumpLambda = null,
            AnyUiLambdaActionBase takeOverLambdaAction = null,
            Func<List<string>, AnyUiLambdaActionBase> noEditJumpLambda = null,
            IReferable relatedReferable = null,
            Action<IReferable> emitCustomEvent = null)
        {
            AddKeyListGeneric<string, List<string>>(
                view, key, keys, repo, packages, selector,
                addExistingEntities: addExistingEntities,
                addEclassIrdi: addEclassIrdi,
                addFromPool: addFromPool,
                addPresetNames: addPresetNames,
                addPresetKeyLists: addPresetKeyLists,
                auxButtonLambda: auxButtonLambda,
                auxButtonTitles: auxButtonTitles, auxButtonToolTips: auxButtonToolTips,
                jumpLambda: jumpLambda,
                takeOverLambdaAction: takeOverLambdaAction,
                noEditJumpLambda: noEditJumpLambda,
                relatedReferable: relatedReferable,
                emitCustomEvent: emitCustomEvent,
                addElemLambda: (o) =>
                {
                    //if (o is IIdentifiable id)
                    //    keys.Add(new Identifier(id.Id));
                    //if (o is Key k)
                    //    keys.Add(k.Value);

                    //TODO: jtikekar Test
                    if (o is IIdentifiable id)
                        keys.Add(id.Id);
                    if (o is Key k)
                        keys.Add(k.Value);
                },
                getItem1Lambda: null,
                getItem2Lambda: (k) => k,
                setItem1Lambda: null,
                setItem2Lambda: (k, o) => { k = (string)o; },
                elemsToStringLambda: (list) => string.Join("\r\n", list.Select((x) => x))
            );
        }

        // GENERIC version

        public void AddKeyListGeneric<T, LIST>(
            AnyUiStackPanel view, string key,
            LIST elems,
            ModifyRepo repo = null,
            PackageCentral.PackageCentral packages = null,
            PackageCentral.PackageCentral.Selector selector = PackageCentral.PackageCentral.Selector.Main,
            string addExistingEntities = null,
            bool addEclassIrdi = false,
            bool addFromPool = false,
            string[] addPresetNames = null, LIST[] addPresetKeyLists = null,
            Func<int, AnyUiLambdaActionBase> auxButtonLambda = null,
            string[] auxButtonTitles = null, string[] auxButtonToolTips = null,
            Func<LIST, AnyUiLambdaActionBase> jumpLambda = null,
            AnyUiLambdaActionBase takeOverLambdaAction = null,
            Func<LIST, AnyUiLambdaActionBase> noEditJumpLambda = null,
            IReferable relatedReferable = null,
            Action<IReferable> emitCustomEvent = null,
            Action<object> addElemLambda = null,
            Func<T, object> getItem1Lambda = null,
            Func<T, object> getItem2Lambda = null,
            Action<T, object> setItem1Lambda = null,
            Action<T, object> setItem2Lambda = null,
            Func<LIST, string> elemsToStringLambda = null)
            where LIST : List<T>
            //where T : new()
        {
            // sometimes needless to show
            if (repo == null && (elems == null || elems.Count < 1))
                return;
            int rows = 1; // default!
            if (elems != null && elems.Count > 1)
                rows = elems.Count;
            int rowOfs = 0;
            if (repo != null)
                rowOfs = 1;
            if (jumpLambda != null)
                rowOfs = 1;

            // default
            if (emitCustomEvent == null)
                emitCustomEvent = (rf) => { this.AddDiaryEntry(rf, new DiaryEntryStructChange()); };

            // Grid
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 0, 0, 0);

            // 0 key
            var gc = new AnyUiColumnDefinition();
            gc.Width = AnyUiGridLength.Auto;
            gc.MinWidth = this.standardFirstColWidth;
            g.ColumnDefinitions.Add(gc);

            // 1 type
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 2 local
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 3 id type
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 4 value
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.ColumnDefinitions.Add(gc);

            // 5 .. buttons behind it
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // rows
            for (int r = 0; r < rows + rowOfs; r++)
            {
                var gr = new AnyUiRowDefinition();
                gr.Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                g.RowDefinitions.Add(gr);
            }

            // populate key
            AddSmallLabelTo(g, 0, 0, margin: new AnyUiThickness(5, 0, 0, 0), content: "" + key + ":");

            // presets?
            var presetNo = 0;
            if (addPresetNames != null && addPresetKeyLists != null
                && addPresetNames.Length == addPresetKeyLists.Length)
                presetNo = addPresetNames.Length;

            if (repo == null)
            {
                // TODO (Michael Hoffmeister, 2020-08-01): possibly [Jump] button??
            }
            else
            if (elems != null)
            {
                // basic scheme: populate [+], [Select], [ECLASS], [Copy] buttons
                var colDescs = new List<string>(new[] { "*", "#", "#", "#", "#", "#", "#" });

                // extend by variable # of items
                var addColNum = presetNo;
                if (auxButtonLambda != null && auxButtonTitles != null)
                    addColNum += auxButtonTitles.Length;
                for (int i = 0; i < addColNum; i++)
                    colDescs.Add("#");

                // create such Grid
                var g2 = AddSmallGrid(1, 7 + addColNum, colDescs.ToArray());
                AnyUiGrid.SetRow(g2, 0);
                AnyUiGrid.SetColumn(g2, 1);
                AnyUiGrid.SetColumnSpan(g2, 7);
                g.Children.Add(g2);

                // add the different widgets

                if (addFromPool)
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 1,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            content: "Add known"),
                        (o) =>
                        {
                            var uc = new AnyUiDialogueDataSelectReferableFromPool(
                                caption: "Select known entity");
                            this.context.StartFlyoverModal(uc);

                            if (uc.Result &&
                                uc.ResultItem is AasxPredefinedConcepts.DefinitionsPoolReferableEntity pe
                                && pe.Ref is IIdentifiable id
                                && id.Id != null)
                                addElemLambda?.Invoke(id);

                            emitCustomEvent?.Invoke(relatedReferable);

                            if (takeOverLambdaAction != null)
                                return takeOverLambdaAction;
                            else
                                return new AnyUiLambdaActionRedrawEntity();
                        });

                if (addEclassIrdi)
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 2,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            content: "Add ECLASS"),
                        (o) =>
                        {
                            string resIRDI = null;
                            ConceptDescription resCD = null;
                            if (this.SmartSelectEclassEntity(
                                    AnyUiDialogueDataSelectEclassEntity.SelectMode.IRDI, ref resIRDI, ref resCD))
                            {
                                addElemLambda?.Invoke(new Key(KeyTypes.GlobalReference, resIRDI));
                            }

                            emitCustomEvent?.Invoke(relatedReferable);

                            if (takeOverLambdaAction != null)
                                return takeOverLambdaAction;
                            else
                                return new AnyUiLambdaActionRedrawEntity();
                        });

                if (addExistingEntities != null && packages.MainAvailable)
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 3,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            content: "Add existing"),
                        (o) =>
                        {
                            var k2 = SmartSelectAasEntityKeys(packages, selector, addExistingEntities);
                            if (k2 != null)
                                foreach (var k in k2)
                                    addElemLambda?.Invoke(k);

                            emitCustomEvent?.Invoke(relatedReferable);

                            if (takeOverLambdaAction != null)
                                return takeOverLambdaAction;
                            else
                                return new AnyUiLambdaActionRedrawEntity();
                        });

                AnyUiUIElement.RegisterControl(
                    AddSmallButtonTo(
                        g2, 0, 4,
                        margin: new AnyUiThickness(2, 2, 2, 2),
                        padding: new AnyUiThickness(5, 0, 5, 0),
                        content: "Add blank"),
                    (o) =>
                    {
                        var k = new Key(KeyTypes.FragmentReference, ""); //Default
                        addElemLambda?.Invoke(k);

                        emitCustomEvent?.Invoke(relatedReferable);

                        if (takeOverLambdaAction != null)
                            return takeOverLambdaAction;
                        else
                            return new AnyUiLambdaActionRedrawEntity();
                    });

                if (jumpLambda != null)
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 5,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            content: "Jump"),
                        (o) =>
                        {
                            return jumpLambda(elems);
                        });

                AnyUiUIElement.RegisterControl(
                    AddSmallButtonTo(
                        g2, 0, 6,
                        margin: new AnyUiThickness(2, 2, 2, 2),
                        padding: new AnyUiThickness(5, 0, 5, 0),
                        content: "Clipboard"),
                    (o) =>
                    {
                        var st = "" + elemsToStringLambda?.Invoke(elems);
                        this.context?.ClipboardSet(new AnyUiClipboardData(st));
                        Log.Singleton.Info("Keys written to clipboard.");
                        return new AnyUiLambdaActionNone();
                    });

                if (addColNum > 0)
                {
                    // variable number of items
                    int currCol = 7;

                    for (int i = 0; i < presetNo; i++)
                    {
                        var closureKey = addPresetKeyLists[i];
                        AnyUiUIElement.RegisterControl(
                            AddSmallButtonTo(
                                g2, 0, currCol++,
                                margin: new AnyUiThickness(2, 2, 2, 2),
                                padding: new AnyUiThickness(5, 0, 5, 0),
                                content: "" + addPresetNames[i]),
                            (o) =>
                            {
                                elems.AddRange(closureKey);
                                emitCustomEvent?.Invoke(relatedReferable);
                                return new AnyUiLambdaActionRedrawEntity();
                            });
                    }

                    if (auxButtonTitles != null)
                        for (int i = 0; i < auxButtonTitles.Length; i++)
                        {
                            Func<object, AnyUiLambdaActionBase> lmb = null;
                            int closureI = i;
                            if (auxButtonLambda != null)
                                lmb = (o) =>
                                {
                                    return auxButtonLambda(closureI); // exchange o with i !!
                                };
                            var b = AnyUiUIElement.RegisterControl(
                                AddSmallButtonTo(
                                    g2, 0, currCol++,
                                    margin: new AnyUiThickness(2, 2, 2, 2),
                                    padding: new AnyUiThickness(5, 0, 5, 0),
                                    content: auxButtonTitles[i]),
                                lmb) as AnyUiButton;
                            if (auxButtonToolTips != null && i < auxButtonToolTips.Length)
                                b.ToolTip = auxButtonToolTips[i];
                        }
                }
            }

            // contents?
            if (elems != null)
                for (int i = 0; i < elems.Count; i++)
                    if (repo == null)
                    {
                        // lang
                        if (getItem1Lambda != null)
                            AddSmallLabelTo(
                                g, 0 + i + rowOfs, 1,
                                padding: new AnyUiThickness(2, 0, 0, 0),
                                content: "" + getItem1Lambda?.Invoke(elems[i]));

                        // value
                        if (getItem2Lambda != null)
                            AddSmallLabelTo(
                                g, 0 + i + rowOfs, 4,
                                padding: new AnyUiThickness(2, 0, 0, 0),
                                content: "" + getItem2Lambda?.Invoke(elems[i]));

                        // jump
                        /* TODO (MIHO, 2021-02-16): this mechanism is ugly and only intended to be temporary!
                           It shall be replaced (after intergrating AnyUI) by a better repo handling */
                        if (noEditJumpLambda != null && i == 0)
                        {
                            AnyUiUIElement.RegisterControl(
                                AddSmallButtonTo(
                                    g, 0 + +rowOfs, 5,
                                    margin: new AnyUiThickness(2, 2, 2, 2),
                                    padding: new AnyUiThickness(5, 0, 5, 0),
                                    content: "Jump"),
                                    (o) =>
                                    {
                                        return noEditJumpLambda(elems);
                                    });
                        }
                    }
                    else
                    {
                        // save in current context
                        var currentI = 0 + i;

                        // TODO (Michael Hoffmeister, 2020-08-01): Needs to be revisited

                        // type
                        if (getItem1Lambda != null)
                        {
                            var item = getItem1Lambda?.Invoke(elems[i]);
                            var cbType = AnyUiUIElement.RegisterControl(
                                AddSmallComboBoxTo(
                                    g, 0 + i + rowOfs, 1,
                                    margin: new AnyUiThickness(2, 2, 2, 2),
                                    text: "" + item,
                                    minWidth: 100,
                                    items: Enum.GetNames(typeof(KeyTypes)),
                                    isEditable: false,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                (o) =>
                                {
                                    setItem1Lambda?.Invoke(elems[currentI], o);
                                    emitCustomEvent?.Invoke(relatedReferable);
                                    return new AnyUiLambdaActionNone();
                                },
                                takeOverLambda: takeOverLambdaAction) as AnyUiComboBox;
                            SmallComboBoxSelectNearestItem(cbType, cbType.Text);

                            // check here, if to hightlight
                            if (cbType != null && this.highlightField != null && item != null &&
                                    this.highlightField.fieldHash == item.GetHashCode() &&
                                    (object)elems[currentI] == this.highlightField.containingObject)
                                this.HighligtStateElement(cbType, true);
                        }

                        // value
                        if (getItem2Lambda != null)
                        {
                            var item = getItem2Lambda?.Invoke(elems[i]);
                            var tbValue = AddSmallTextBoxTo(
                                g, 0 + i + rowOfs, 4,
                                margin: new AnyUiThickness(2, 2, 2, 2),
                                text: "" + item,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center);
                            AnyUiUIElement.RegisterControl(
                                tbValue,
                                (o) =>
                                {
                                    setItem2Lambda?.Invoke(elems[currentI], o);
                                    emitCustomEvent?.Invoke(relatedReferable);
                                    return new AnyUiLambdaActionNone();
                                }, takeOverLambda: takeOverLambdaAction);

                            // check here, if to hightlight
                            if (tbValue != null && this.highlightField != null && item != null &&
                                    this.highlightField.fieldHash == item.GetHashCode() &&
                                    (object)elems[currentI] == this.highlightField.containingObject)
                                this.HighligtStateElement(tbValue, true);
                        }

                        // button [hamburger]
                        AddSmallContextMenuItemTo(
                                g, 0 + i + rowOfs, 5,
                                "\u22ee",
                                repo, new[] {
                                    "\u2702", "Delete",
                                    "\u25b2", "Move Up",
                                    "\u25bc", "Move Down",
                                },
                                margin: new AnyUiThickness(2, 2, 2, 2),
                                padding: new AnyUiThickness(5, 0, 5, 0),
                                menuItemLambda: (o) =>
                                {
                                    var action = false;

                                    if (o is int ti)
                                        switch (ti)
                                        {
                                            case 0:
                                                elems.RemoveAt(currentI);
                                                action = true;
                                                break;
                                            case 1:
                                                MoveElementInListUpwards<T>(elems, elems[currentI]);
                                                action = true;
                                                break;
                                            case 2:
                                                MoveElementInListDownwards<T>(elems, elems[currentI]);
                                                action = true;
                                                break;
                                        }

                                    emitCustomEvent?.Invoke(relatedReferable);

                                    if (action)
                                        if (takeOverLambdaAction != null)
                                            return takeOverLambdaAction;
                                        else
                                            return new AnyUiLambdaActionRedrawEntity();
                                    return new AnyUiLambdaActionNone();
                                });

                    }

            // in total
            view.Children.Add(g);
        }

        public AnyUiButton AddSmallContextMenuItemTo(
            AnyUiGrid g, int row, int col,
            string content,
            ModifyRepo repo,
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

        public void AddKeyListKeys(
            AnyUiStackPanel view, string key,
            List<Key> keys,
            ModifyRepo repo = null,
            PackageCentral.PackageCentral packages = null,
            PackageCentral.PackageCentral.Selector selector = PackageCentral.PackageCentral.Selector.Main,
            string addExistingEntities = null,
            bool addEclassIrdi = false,
            bool addFromKnown = false,
            string[] addPresetNames = null, List<Key>[] addPresetKeyLists = null,
            Func<List<Key>, AnyUiLambdaActionBase> jumpLambda = null,
            AnyUiLambdaActionBase takeOverLambdaAction = null,
            Func<List<Key>, AnyUiLambdaActionBase> noEditJumpLambda = null,
            IReferable relatedReferable = null,
            Action<IReferable> emitCustomEvent = null,
            AnyUiPanel frontPanel = null,
            AnyUiPanel footerPanel = null,
            bool topContextMenu = false,
            string[] auxContextHeader = null, Func<int, AnyUiLambdaActionBase> auxContextLambda = null)
        {
            // sometimes needless to show
            if (repo == null && (keys == null || keys.Count < 1))
                return;
            int rows = 1; // default!
            if (keys != null && keys.Count >= 1)
                rows += keys.Count;
            if (footerPanel != null)
                rows++;
            int rowOfs = 0;
            if (repo != null)
                rowOfs = 1;
            if (repo != null && jumpLambda != null)
                rowOfs = 1;

            // default
            if (emitCustomEvent == null)
                emitCustomEvent = (rf) => { this.AddDiaryEntry(rf, new DiaryEntryStructChange()); };

            // Grid
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 0, 0, 0);

            // 0 key
            var gc = new AnyUiColumnDefinition();
            gc.Width = AnyUiGridLength.Auto;
            gc.MinWidth = this.standardFirstColWidth;
            g.ColumnDefinitions.Add(gc);

            // 1 type
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 2 local
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 3 id type
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 4 value
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.ColumnDefinitions.Add(gc);

            // 5 .. buttons behind it
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // rows
            for (int r = 0; r < rows + rowOfs; r++)
            {
                var gr = new AnyUiRowDefinition();
                gr.Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                g.RowDefinitions.Add(gr);
            }

            // populate key
            AddSmallLabelTo(g, 0, 0, margin: new AnyUiThickness(5, 0, 0, 0), 
                verticalAlignment: AnyUiVerticalAlignment.Center,
                content: "" + key + ":");

            // presets?
            var presetNo = 0;
            if (addPresetNames != null && addPresetKeyLists != null
                && addPresetNames.Length == addPresetKeyLists.Length)
                presetNo = addPresetNames.Length;

            if (repo == null)
            {
                // TODO (Michael Hoffmeister, 2020-08-01): possibly [Jump] button??
            }
            else
            if (keys != null)
            {
                //
                // First row:
                // populate [+], [Select], [ECLASS], [Copy] buttons
                //

                var colDescs = new List<string>(new[] { "*", "#", "#", "#", "#", "#", "#", "#", "#" });
                for (int i = 0; i < presetNo; i++)
                    colDescs.Add("#");

                var g2 = AddSmallGrid(1, 9 + presetNo, colDescs.ToArray());
                g2.HorizontalAlignment = AnyUiHorizontalAlignment.Stretch;
                AnyUiGrid.SetRow(g2, 0);
                AnyUiGrid.SetColumn(g2, 1);
                AnyUiGrid.SetColumnSpan(g2, 7);
                g.Children.Add(g2);

                if (frontPanel != null)
                {
                    AnyUiGrid.SetRow(frontPanel, 0);
                    AnyUiGrid.SetColumn(frontPanel, 0);
                    g2.Children.Add(frontPanel);
                }

                //
                // Define lambdas for double use
                //

                Func<object, AnyUiLambdaActionBase> lambdaEclassIrdi = (o) =>
                {
                    string resIRDI = null;
                    ConceptDescription resCD = null;
                    if (this.SmartSelectEclassEntity(
                            AnyUiDialogueDataSelectEclassEntity.SelectMode.IRDI, ref resIRDI, ref resCD))
                    {
                        keys.Add(
                            new Key(KeyTypes.GlobalReference, resIRDI));
                    }

                    emitCustomEvent?.Invoke(relatedReferable);

                    if (takeOverLambdaAction != null)
                        return takeOverLambdaAction;
                    else
                        return new AnyUiLambdaActionRedrawEntity();
                };

                Func<object, AnyUiLambdaActionBase> lambdaClipboard = (o) =>
                {
                    var st = keys.ToStringExtended(delimiter: "\r\n");
                    this.context?.ClipboardSet(new AnyUiClipboardData(st));
                    Log.Singleton.Info("Keys written to clipboard.");
                    return new AnyUiLambdaActionNone();
                };

                // 
                // populate top row
                //

                if (addFromKnown)
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 2,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            content: "Add known"),
                        (o) =>
                        {
                            var uc = new AnyUiDialogueDataSelectReferableFromPool(
                                caption: "Select known entity");
                            this.context.StartFlyoverModal(uc);

                            if (uc.Result &&
                                uc.ResultItem is AasxPredefinedConcepts.DefinitionsPoolReferableEntity pe
                                && pe.Ref is IIdentifiable id
                                && id.Id != null)
                                // DECISION: references to concepts are always GlobalReferences
                                keys.Add(new Key(KeyTypes.GlobalReference, id.Id));

                            emitCustomEvent?.Invoke(relatedReferable);

                            if (takeOverLambdaAction != null)
                                return takeOverLambdaAction;
                            else
                                return new AnyUiLambdaActionRedrawEntity();
                        });                

                if (!topContextMenu && addEclassIrdi)
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 3,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            content: "Add ECLASS"),
                        lambdaEclassIrdi);

                if (addExistingEntities != null && packages.MainAvailable)
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 4,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            content: "Add existing"),
                        (o) =>
                        {
                            var k2 = SmartSelectAasEntityKeys(packages, selector, addExistingEntities);

                            // some special cases
                            if (!Options.Curr.ModelRefCd && k2 != null && k2.Count == 1
                                && k2[0].Type == KeyTypes.ConceptDescription)
                                k2[0].Type = KeyTypes.GlobalReference;

                            if (k2 != null)
                                keys.AddRange(k2);

                            emitCustomEvent?.Invoke(relatedReferable);

                            if (takeOverLambdaAction != null)
                                return takeOverLambdaAction;
                            else
                                return new AnyUiLambdaActionRedrawEntity();
                        });

                AnyUiUIElement.RegisterControl(
                    AddSmallButtonTo(
                        g2, 0, 5,
                        margin: new AnyUiThickness(2, 2, 2, 2),
                        padding: new AnyUiThickness(5, 0, 5, 0),
                        content: "Add blank"),
                    (o) =>
                    {
                        var k = new Key(KeyTypes.GlobalReference, ""); //TODO:jtikekar default key
                        keys.Add(k);

                        emitCustomEvent?.Invoke(relatedReferable);

                        if (takeOverLambdaAction != null)
                            return takeOverLambdaAction;
                        else
                            return new AnyUiLambdaActionRedrawEntity();
                    });

                if (!topContextMenu && jumpLambda != null)
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 6,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            content: "Jump"),
                        (o) =>
                        {
                            return jumpLambda(keys);
                        });

                if (!topContextMenu)
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 7,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            content: "Clipboard"),
                        lambdaClipboard);

                //
                // Presets
                //

                for (int i = 0; i < presetNo; i++)
                {
                    var closureKey = addPresetKeyLists[i];
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 8 + i,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            content: "" + addPresetNames[i]),
                        (o) =>
                        {
                            keys.AddRange(closureKey);
                            emitCustomEvent?.Invoke(relatedReferable);
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                //
                // Top Row Context Menue
                // (those functions lfess frequent use)
                //

                if (topContextMenu)
                {
                    List<string> contextHeaders = new();
                    if (addEclassIrdi)
                        contextHeaders.AddRange(new[] { "\U0001f517", "Add ECLASS" });
                    if (jumpLambda != null)
                        contextHeaders.AddRange(new[] { "\u21a6", "Jump" });
                    if (true)
                        contextHeaders.AddRange(new[] { "\U0001f4cb", "Copy to clipboard" });

                    var auxContextOfs = contextHeaders.Count / 2;
                    if (auxContextHeader != null && auxContextHeader.Length >= 2)
                        contextHeaders.AddRange(auxContextHeader);

                    AddSmallContextMenuItemTo(
                        g2, 0, 8 + presetNo,
                        "\u22ee",
                        contextHeaders.ToArray(),
                        margin: new AnyUiThickness(2, 2, 2, 2),
                        padding: new AnyUiThickness(5, 0, 5, 0),
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        menuItemLambda: (o) =>
                        {
                            if (o is int oi && oi >= 0 && (2*oi + 1) < contextHeaders.Count)
                            {
                                if (oi >= auxContextOfs && auxContextLambda != null)
                                    return auxContextLambda(oi - auxContextOfs);

                                if (contextHeaders[2 * oi + 1].Contains("ECLASS"))
                                    return lambdaEclassIrdi(o);

                                if (contextHeaders[2 * oi + 1].Contains("Jump"))
                                    return jumpLambda(keys);

                                if (contextHeaders[2 * oi + 1].Contains("clipboard"))
                                    return lambdaClipboard(o);
                            }
                            return new AnyUiLambdaActionNone();
                        });
                }
            }

            // contents?
            if (keys != null)
                for (int i = 0; i < keys.Count; i++)
                    if (repo == null)
                    {
                        // type
                        AddSmallLabelTo(
                            g, 0 + i + rowOfs, 1,
                            padding: new AnyUiThickness(2, 0, 0, 0),
                            setNoWrap: true,
                            content: "(" + keys[i].Type + ")");

                        // value
                        AddSmallLabelTo(
                            g, 0 + i + rowOfs, 4,
                            padding: new AnyUiThickness(2, 0, 0, 0),
                            content: "" + keys[i].Value);

                        // jump
                        /* TODO (MIHO, 2021-02-16): this mechanism is ugly and only intended to be temporary!
                           It shall be replaced (after intergrating AnyUI) by a better repo handling */
                        if (noEditJumpLambda != null && i == 0)
                        {
                            AnyUiUIElement.RegisterControl(
                                AddSmallButtonTo(
                                    g, 0 + +rowOfs, 5,
                                    margin: new AnyUiThickness(2, 2, 2, 2),
                                    padding: new AnyUiThickness(5, 0, 5, 0),
                                    content: "Jump"),
                                    (o) =>
                                    {
                                        return noEditJumpLambda(keys);
                                    });
                        }
                    }
                    else
                    {
                        // save in current context
                        var currentI = 0 + i;

                        // TODO (Michael Hoffmeister, 2020-08-01): Needs to be revisited

                        // type
                        var cbType = AnyUiUIElement.RegisterControl(
                            AddSmallComboBoxTo(
                                g, 0 + i + rowOfs, 1,
                                margin: new AnyUiThickness(4, 2, 2, 2),
                                padding: new AnyUiThickness(2, -1, 0, -1),
                                text: "" + keys[currentI].Type,
                                minWidth: 100,
                                items: Enum.GetNames(typeof(KeyTypes)),
                                isEditable: false,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center),
                            (o) =>
                            {
                                keys[currentI].Type = (KeyTypes)Stringification.KeyTypesFromString((string)o);
                                emitCustomEvent?.Invoke(relatedReferable);
                                return new AnyUiLambdaActionNone();
                            },
                            takeOverLambda: takeOverLambdaAction);
                        SmallComboBoxSelectNearestItem(cbType, cbType.Text);

                        // check here, if to hightlight
                        if (cbType != null && this.highlightField != null &&
                                this.highlightField.fieldHash == keys[currentI].Type.GetHashCode() &&
                                keys[currentI] == this.highlightField.containingObject)
                            this.HighligtStateElement(cbType, true);

                        //// check here, if to hightlight
                        //if (cbIdType != null && this.highlightField != null && keys[currentI].idType != null &&
                        //        this.highlightField.fieldHash == keys[currentI].idType.GetHashCode() &&
                        //        keys[currentI] == this.highlightField.containingObject)
                        //    this.HighligtStateElement(cbIdType, true);

                        // value
                        var tbValue = AddSmallTextBoxTo(
                            g, 0 + i + rowOfs, 4,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            text: "" + keys[currentI].Value,
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);
                        AnyUiUIElement.RegisterControl(
                            tbValue,
                            (o) =>
                            {
                                keys[currentI].Value = o as string;
                                emitCustomEvent?.Invoke(relatedReferable);
                                return new AnyUiLambdaActionNone();
                            }, takeOverLambda: takeOverLambdaAction);

                        // check here, if to hightlight
                        if (tbValue != null && this.highlightField != null && keys[currentI].Value != null &&
                                this.highlightField.fieldHash == keys[currentI].Value.GetHashCode() &&
                                keys[currentI] == this.highlightField.containingObject)
                            this.HighligtStateElement(tbValue, true);

                        // button [hamburger]
                        AddSmallContextMenuItemTo(
                                g, 0 + i + rowOfs, 5,
                                "\u22ee",
                                new[] {
                                    "\u2702", "Delete",
                                    "\u25b2", "Move Up",
                                    "\u25bc", "Move Down",
                                },
                                margin: new AnyUiThickness(2, 2, 2, 2),
                                padding: new AnyUiThickness(5, 0, 5, 0),
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                menuItemLambda: (o) =>
                                {
                                    var action = false;

                                    if (o is int ti)
                                        switch (ti)
                                        {
                                            case 0:
                                                keys.RemoveAt(currentI);
                                                action = true;
                                                break;
                                            case 1:
                                                MoveElementInListUpwards<Key>(keys, keys[currentI]);
                                                action = true;
                                                break;
                                            case 2:
                                                MoveElementInListDownwards<Key>(keys, keys[currentI]);
                                                action = true;
                                                break;
                                        }

                                    emitCustomEvent?.Invoke(relatedReferable);

                                    if (action)
                                        if (takeOverLambdaAction != null)
                                            return takeOverLambdaAction;
                                        else
                                            return new AnyUiLambdaActionRedrawEntity();
                                    return new AnyUiLambdaActionNone();
                                });

                    }

            //
            // Footer
            //

            if (footerPanel != null)
            {
                AnyUiGrid.SetRow(footerPanel, 0 + keys.Count + rowOfs);
                AnyUiGrid.SetColumn(footerPanel, 1);
                AnyUiGrid.SetColumnSpan(footerPanel, 7);
                g.Children.Add(footerPanel);
            }

            //
            // in total
            //
            
            view.Children.Add(g);
        }

        //
        // Safeguarding functions (checking if somethingis null and doing ..)
        //

        public bool SafeguardAccess(
            AnyUiStackPanel view, ModifyRepo repo, object data, string key, string actionStr,
            Func<int, AnyUiLambdaActionBase> action)
        {
            if (repo != null && data == null)
                AddAction(view, key, actionStr, repo, action);
            return (data != null);
        }

        //
        // List manipulations (single entities)
        //

        public int MoveElementInListUpwards<T>(List<T> list, T entity)
        {
            if (list == null || list.Count < 2 || entity == null)
                return -1;
            int ndx = list.IndexOf(entity);
            if (ndx < 1)
                return -1;
            list.RemoveAt(ndx);
            var newndx = Math.Max(ndx - 1, 0);
            list.Insert(newndx, entity);
            return newndx;
        }

        public int MoveElementInListDownwards<T>(List<T> list, T entity)
        {
            if (list == null || list.Count < 2 || entity == null)
                return -1;
            int ndx = list.IndexOf(entity);
            if (ndx < 0 || ndx >= list.Count - 1)
                return -1;
            list.RemoveAt(ndx);
            var newndx = Math.Min(ndx + 1, list.Count);
            list.Insert(newndx, entity);
            return newndx;
        }

        public int MoveElementToTopOfList<T>(List<T> list, T entity)
        {
            if (list == null || list.Count < 2 || entity == null)
                return -1;
            int ndx = list.IndexOf(entity);
            if (ndx < 1)
                return -1;
            list.RemoveAt(ndx);
            var newndx = 0;
            list.Insert(newndx, entity);
            return newndx;
        }

        public int MoveElementToBottomOfList<T>(List<T> list, T entity)
        {
            if (list == null || list.Count < 2 || entity == null)
                return -1;
            int ndx = list.IndexOf(entity);
            if (ndx < 0)
                return -1;
            list.RemoveAt(ndx);
            var newndx = list.Count;
            list.Insert(newndx, entity);
            return newndx;
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

        public int AddElementInListBefore<T>(List<T> list, T entity, T existing)
        {
            if (list == null || list.Count < 1 || entity == null)
                return -1;
            int ndx = list.IndexOf(existing);
            if (ndx < 0 || ndx > list.Count - 1)
                return -1;
            list.Insert(ndx, entity);
            return ndx;
        }

        public int AddElementInListAfter<T>(List<T> list, T entity, T existing)
        {
            if (list == null || list.Count < 1 || entity == null)
                return -1;
            int ndx = list.IndexOf(existing);
            if (ndx < 0 || ndx > list.Count)
                return -1;
            list.Insert(ndx + 1, entity);
            return ndx + 1;
        }

        //
        // List manipulations (multiple entities)
        //

        public int MoveElementsToStartingIndex<T>(List<T> list, List<T> entities, int startingIndex)
        {
            // check
            if (list == null || list.Count < 1 || entities == null)
                return -1;
            if (startingIndex < 0)
                return -1;
            // remove all from list
            foreach (var e in entities)
                if (list.Contains(e))
                    list.Remove(e);
            // now, insert sequentially starting from index
            var si2 = startingIndex;
            if (si2 > list.Count)
                si2 = list.Count;
            var ndx = si2;
            foreach (var e in entities)
                list.Insert(ndx++, e);
            // return something
            return si2;
        }

        public int DeleteElementsInList<T>(List<T> list, List<T> entities)
        {
            // check
            if (list == null || list.Count < 1 || entities == null)
                return -1;
            // remove all from list
            foreach (var e in entities)
                if (list.Contains(e))
                    list.Remove(e);
            // return something
            return 0;
        }

        //
        // manipulations for list of SME wrappers
        //

        public int AddElementInSmeListBefore<T>(
            List<T> list,
            T entity, T existing,
            bool makeUniqueIfNeeded = false)
            where T : ISubmodelElement
        {
            // access
            if (list == null || list.Count < 1 || entity == null)
                return -1;

            // make unqiue
            if (makeUniqueIfNeeded && !(list as List<ISubmodelElement>)
                .CheckIdShortIsUnique(entity.IdShort))
                    this.MakeNewReferableUnique(entity);

            // delegate
            return AddElementInListBefore<T>(list, entity, existing);
        }

        public int AddElementInSmeListAfter<T>(
            List<T> list,
            T entity, T existing,
            bool makeUniqueIfNeeded = false)
            where T : ISubmodelElement
        {
            // access
            if (list == null || list.Count < 1 || entity == null)
                return -1;

            // make unqiue
            if (makeUniqueIfNeeded && !(list as List<ISubmodelElement>)
                .CheckIdShortIsUnique(entity.IdShort))
                    this.MakeNewReferableUnique(entity);

            // delegate
            return AddElementInListAfter<T>(list, entity, existing);
        }

        //
        // Helper
        //

        public void EntityListUpDownDeleteHelper<T>(
            AnyUiPanel stack, ModifyRepo repo, List<T> list, T entity,
            object alternativeFocus, string label = "Entities:",
            object nextFocus = null, PackCntChangeEventData sendUpdateEvent = null, bool preventMove = false,
            IReferable explicitParent = null)
        {
            if (nextFocus == null)
                nextFocus = entity;

            // pick out referable
            IReferable entityRf = null;
            if (entity is ISubmodelElement smw)
                entityRf = smw;
            if (entity is IReferable rf)
                entityRf = rf;

            AddAction(
                stack, label,
                new[] { "Move up", "Move down", "Move top", "Move end", "Delete" },
                actionTags: new[] { "aas-elem-move-up", "aas-elem-move-down",
                    "aas-elem-move-top", "aas-elem-move-end", "aas-elem-delete" },
                repo: repo,
                action: (buttonNdx) =>
                {
                    if (buttonNdx >= 0 && buttonNdx <= 3)
                    {
                        if (preventMove)
                        {
                            this.context.MessageBoxFlyoutShow(
                                "Moving within list is not possible, as list of entities has dynamic " +
                                "sort order.",
                                "Move entities", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);
                            return new AnyUiLambdaActionNone();
                        }

                        var newndx = -1;
                        if (buttonNdx == 0) newndx = MoveElementInListUpwards<T>(list, entity);
                        if (buttonNdx == 1) newndx = MoveElementInListDownwards<T>(list, entity);
                        if (buttonNdx == 2) newndx = MoveElementToTopOfList<T>(list, entity);
                        if (buttonNdx == 3) newndx = MoveElementToBottomOfList<T>(list, entity);
                        if (newndx >= 0)
                        {
                            this.AddDiaryEntry(entityRf,
                                new DiaryEntryStructChange(StructuralChangeReason.Modify, createAtIndex: newndx),
                                explicitParent: explicitParent);

                            if (sendUpdateEvent != null)
                            {
                                sendUpdateEvent.Reason = PackCntChangeEventReason.MoveToIndex;
                                sendUpdateEvent.NewIndex = newndx;
                                sendUpdateEvent.DisableSelectedTreeItemChange = true;
                                return new AnyUiLambdaActionPackCntChange(sendUpdateEvent);
                            }
                            else
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: nextFocus, isExpanded: null);
                        }
                        else
                            return new AnyUiLambdaActionNone();
                    }

                    if (buttonNdx == 4)

                        if (this.context.ActualShiftState
                            || AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                "Delete selected entity? This operation can not be reverted!", "AAS-ENV",
                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                        {
                            var ret = DeleteElementInList<T>(list, entity, alternativeFocus);

                            this.AddDiaryEntry(entityRf,
                                new DiaryEntryStructChange(StructuralChangeReason.Delete),
                                explicitParent: explicitParent);

                            if (sendUpdateEvent != null)
                            {
                                sendUpdateEvent.Reason = PackCntChangeEventReason.Delete;
                                return new AnyUiLambdaActionPackCntChange(sendUpdateEvent);
                            }
                            else
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: ret, isExpanded: null);
                        }

                    return new AnyUiLambdaActionNone();
                });
        }

        //
        // Identify ECLASS properties to be imported
        //

        public void IdentifyTargetsForEclassImportOfCDs(
            AasCore.Aas3_0_RC02.Environment env, List<ISubmodelElement> elems,
            ref List<ISubmodelElement> targets)
        {
            if (env == null || targets == null || elems == null)
                return;
            foreach (var elem in elems)
            {
                // sort all the non-fitting
                if (elem.SemanticId != null && !elem.SemanticId.IsEmpty() && elem.SemanticId.Keys.Count == 1
                    && elem.SemanticId.Keys[0].Type == KeyTypes.ConceptDescription
                    && elem.SemanticId.Keys[0].Value.StartsWith("0173"))
                {
                    // already in CDs?
                    var x = env.FindConceptDescriptionByReference(elem.SemanticId);
                    if (x == null)
                        // this one has the potential to get imported ECLASS CD
                        targets.Add(elem);
                }

                // recursion?
                if (elem is SubmodelElementCollection)
                {
                    var childs = new List<ISubmodelElement>(
                        (elem as SubmodelElementCollection).Value);
                    IdentifyTargetsForEclassImportOfCDs(env, childs, ref targets);
                }
            }
        }

        public bool ImportEclassCDsForTargets(AasCore.Aas3_0_RC02.Environment env, object startMainDataElement,
                List<ISubmodelElement> targets)
        {
            // need dialogue and data
            if (env == null || targets == null)
                return false;

            // use ECLASS utilities
            var fullfn = System.IO.Path.GetFullPath(Options.Curr.EclassDir);
            var jobData = new EclassUtils.SearchJobData(fullfn);
            foreach (var t in targets)
                if (t != null && t.SemanticId != null && t.SemanticId.Keys.Count == 1)
                    jobData.searchIRDIs.Add(t.SemanticId.Keys[0].Value.ToLower().Trim());
            // still valid?
            if (jobData.searchIRDIs.Count < 1)
                return false;

            // make a progress flyout
            var uc = new AnyUiDialogueDataProgress(
                "Import ConceptDescriptions from ECLASS",
                info: "Preparing ...", symbol: AnyUiMessageBoxImage.Information);
            uc.Progress = 0.0;
            // show this
            this.context.StartFlyover(uc);

            // setup worker
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                // job data
                System.Threading.Thread.Sleep(10);

                // longrunnig task for searching IRDIs ..
                uc.Info = "Collecting ECLASS Data ..";
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
                    if (t.SemanticId == null || t.SemanticId.Keys.Count != 1)
                        continue;

                    // CD
                    var newcd = EclassUtils.GenerateConceptDescription(jobData.items, t.SemanticId.Keys[0].Value);
                    if (newcd == null)
                        continue;

                    // add?
                    if (null == env.FindConceptDescriptionByReference(
                            new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.ConceptDescription, newcd.Id)})))
                    {
                        env.ConceptDescriptions.Add(newcd);

                        this.AddDiaryEntry(newcd, new DiaryEntryStructChange(StructuralChangeReason.Create));
                    }
                }
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                // in any case, close flyover
                this.context.CloseFlyover();

                // redraw everything
                this.context.EmitOutsideAction(new AnyUiLambdaActionRedrawAllElements(startMainDataElement));
            };
            worker.RunWorkerAsync();

            // ok
            return true;
        }

        /// <summary>
        /// Creates CDs depending on semanticId of SM or SME.
        /// </summary>
        /// <param name="env">Environment</param>
        /// <param name="root">Submodel or SME</param>
        /// <param name="recurseChilds">Recurse on child elements</param>
        /// <param name="repairSemIds">Will check if the type/ local of the semanticId of
        /// the source SMEs shall be adopted.</param>
        /// <returns>Tuple (#no valid id, #already present, #added) </returns>
        public Tuple<int, int, int> ImportCDsFromSmSme(
            AasCore.Aas3_0_RC02.Environment env,
            IReferable root,
            bool recurseChilds = false,
            bool repairSemIds = false)
        {
            // access
            var noValidId = 0;
            var alreadyPresent = 0;
            var added = 0;

            if (env == null || root == null)
                return new Tuple<int, int, int>(noValidId, alreadyPresent, added);

            //
            // Part 0 : define a lambda
            //

            Action<Reference, IReferable> actionAddCD = (newid, rf) =>
            {
                if (newid == null || newid.Count() < 1)
                {
                    noValidId++;
                }
                else
                {
                    // repair semanticId
                    if (repairSemIds)
                    {
                        if (rf is Submodel rfsm && rfsm.SemanticId != null
                            && rfsm.SemanticId.Count() >= 1)
                        {
                            rfsm.SemanticId.Keys[0].Type = KeyTypes.Submodel;
                        }

                        if (rf is ISubmodelElement rfsme && rfsme.SemanticId != null
                            && rfsme.SemanticId.Count() >= 1)
                        {
                            rfsme.SemanticId.Keys[0].Type = KeyTypes.ConceptDescription;
                        }
                    }

                    // ok?
                    if (newid.Keys.Count != 1)
                        return;

                    // id of new CD
                    var cdid = newid.Keys[0].Value;

                    // check if existing
                    var exCd = env.FindConceptDescriptionById(cdid);
                    if (exCd != null)
                    {
                        alreadyPresent++;
                    }
                    else
                    {
                        // create such CD
                        var cd = new ConceptDescription(cdid);
                        if (rf != null)
                        {
                            cd.IdShort = rf.IdShort;
                            if (rf.Description != null)
                                cd.Description = rf.Description.Copy();
                        }

                        // store in AAS enviroment
                        env.ConceptDescriptions.Add(cd);

                        // count and emit event
                        added++;
                        this.AddDiaryEntry(root, new DiaryEntryStructChange());
                    }
                }
            };

            //
            // Part 1 : semanticId of root
            //


            if (root is IHasSemantics rsmid)
                actionAddCD(rsmid.SemanticId, root as IReferable);


            //
            // Part 2 : semanticId of all children
            //

            if (recurseChilds)
                foreach (var child in root.Descend().OfType<ISubmodelElement>())
                    if (child is IHasSemantics rsmid2)
                        actionAddCD(rsmid2.SemanticId, child);

            // done
            return new Tuple<int, int, int>(noValidId, alreadyPresent, added);
        }

        //
        // Hinting
        //

        public void AddHintBubble(AnyUiStackPanel view, bool hintMode, HintCheck[] hints)
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
                        textsToShow.Add(
                            $"Error while checking hints: {ex.Message} at {AdminShellUtil.ShortLocation(ex)}");
                        highestSev = HintCheck.Severity.High;
                    }
                }

            // some?
            if (textsToShow.Count < 1)
                return;

            // show!
            var bubble = new AnyUiHintBubble();
            bubble.FontSize = 0.8f;
            bubble.Margin = new AnyUiThickness(2, 4, 2, 0);
            bubble.Text = string.Join("\r\n", textsToShow);
            if (highestSev == HintCheck.Severity.High)
            {
                bubble.Background = levelColors?.HintSeverityHigh.Bg;
                bubble.Foreground = levelColors?.HintSeverityHigh.Fg;
            }
            if (highestSev == HintCheck.Severity.Notice)
            {
                bubble.Background = levelColors?.HintSeverityNotice.Bg;
                bubble.Foreground = levelColors?.HintSeverityNotice.Fg;
            }
            view.Children.Add(bubble);
        }

        public void AddHintBubble(AnyUiStackPanel view, bool hintMode, HintCheck hint)
        {
            AddHintBubble(view, hintMode, new[] { hint });
        }

        public T[] ConcatArrays<T>(IEnumerable<T> a, IEnumerable<T> b)
        {
            if (a == null && b == null)
                return null;

            if (a == null)
                return b.ToArray();

            if (b == null)
                return a.ToArray();

            var l = new List<T>();
            l.AddRange(a);
            l.AddRange(b);
            return l.ToArray();
        }

        public HintCheck[] ConcatHintChecks(IEnumerable<HintCheck> a, IEnumerable<HintCheck> b)
        {
            return ConcatArrays<HintCheck>(a, b);
        }

        //
        // Intermediate layer to handle modifications, event generation, marking of time stamps
        //

        /// <summary>
        /// Care for a pseudo-unqiue identification of the IReferable.
        /// Unique identification will be established by adding something such as "---74937434739"
        /// Note: not in <c>cs</c>, as considered part of business logic.
        /// Note: if <c>idShort</c> has content, will add unique content.
        /// </summary>
        /// <param name="rf">given IReferable</param>
        public void MakeNewReferableUnique(IReferable rf)
        {
            // access
            if (rf == null)
                return;

            // random add
            var r = new Random();
            var addStr = "---" + r.Next(0, 0x7fffffff).ToString("X8");

            // completely blank?
            if (rf.IdShort.Trim() == "")
            {
                // empty!
                rf.IdShort = rf.GetSelfDescription().AasElementName + addStr;
                return;
            }

            // already existing?
            var p = rf.IdShort.LastIndexOf("---", StringComparison.Ordinal);
            if (p >= 0)
            {
                rf.IdShort = rf.IdShort.Substring(0, p) + addStr;
                return;
            }

            // simply add
            rf.IdShort += addStr;
        }

        /// <summary>
        /// Care for a pseudo-unqiue identification of the Identifiable.
        /// Unique identification will be established by adding something such as "---74937434739"
        /// Note: not in <c>cs</c>, as considered part of business logic.
        /// Note: if <c>identification == null</c>, will create one.
        /// Note: if <c>identification</c> has content, will add unique content.
        /// </summary>
        /// <param name="idf">given Identifiable</param>
        public void MakeNewIdentifiableUnique(IIdentifiable idf)
        {
            // access
            if (idf == null)
                return;

            //if (idf == null)
            //    idf.Id = new Identification(); //IF cannot be instatiated

            // random add
            var r = new Random();
            var addStr = "---" + r.Next(0, 0x7fffffff).ToString("X8");

            // completely blank?
            if (string.IsNullOrEmpty(idf.Id))
            {
                // empty!
                idf.Id = idf.GetSelfDescription().AasElementName + addStr;
                return;
            }

            // already existing?
            var p = idf.Id.LastIndexOf("---", StringComparison.Ordinal);
            if (p >= 0)
            {
                idf.Id = idf.Id.Substring(0, p) + addStr;
                return;
            }

            // simply add
            idf.Id += addStr;
        }

        /// <summary>
        /// This class tries to acquire element reference information, which is used by 
        /// <c>AddDiaryEntry</c>
        /// </summary>
        public class DiaryReference
        {
            public List<Key> OriginalPath;

            public DiaryReference() { }

            public DiaryReference(IReferable rf)
            {
                //OriginalPath = rf?.GetReference()?.Keys;
                OriginalPath = rf?.GetReference()?.Keys;
            }
        }

        /// <summary>
        /// Base class for diary entries, which are recorded with respect to a AAS element
        /// Diary entries contain a minimal set of information to later produce AAS events or such.
        /// </summary>
        public class DiaryEntryBase
        {
            public DateTime Timestamp;
        }

        /// <summary>
        /// Structural change of that AAS element
        /// </summary>
        public class DiaryEntryStructChange : DiaryEntryBase
        {
            public StructuralChangeReason Reason;
            public int CreateAtIndex = -1;

            public DiaryEntryStructChange(
                StructuralChangeReason reason = StructuralChangeReason.Modify,
                int createAtIndex = -1)
            {
                Reason = reason;
                CreateAtIndex = createAtIndex;
            }
        }

        /// <summary>
        /// Update value of that AAS element
        /// </summary>
        public class DiaryEntryUpdateValue : DiaryEntryBase
        {
        }

        /// <summary>
        /// Takes that diary information and correctly translate this to transaction of the AAS and its elements
        /// </summary>
        public void AddDiaryEntry(IReferable rf, DiaryEntryBase de,
            DiaryReference diaryReference = null,
            bool allChildrenAffected = false,
            IReferable explicitParent = null)
        {
            // trivial
            if (de == null)
                return;

            // structure?
            if (de is DiaryEntryStructChange desc)
            {
                // create
                var evi = new AasPayloadStructuralChangeItem(
                    DateTime.UtcNow, desc.Reason,
                    path: (rf as IReferable)?.GetReference()?.Keys,
                    createAtIndex: desc.CreateAtIndex,
                    // Assumption: models will be serialized correctly
                    data: JsonConvert.SerializeObject(rf));

                if (diaryReference?.OriginalPath != null)
                    evi.Path = diaryReference.OriginalPath;

                // attach where?
                var attachRf = rf;
                if (rf != null && rf.Parent is IReferable parRf)
                    attachRf = parRf;
                if (explicitParent != null)
                    attachRf = explicitParent;

                // add 
                DiaryDataDef.AddAndSetTimestamps(attachRf, evi,
                    isCreate: desc.Reason == StructuralChangeReason.Create);
            }

            // update value?
            if (rf != null && de is DiaryEntryUpdateValue && rf is ISubmodelElement sme)
            {
                // create
                var evi = new AasPayloadUpdateValueItem(
                    path: (rf as IReferable)?.GetReference()?.Keys,
                    value: sme.ValueAsText());

                // TODO (MIHO, 2021-08-17): check if more SME types to serialize

                if (sme is Property p)
                    evi.ValueId = p.ValueId;

                if (sme is MultiLanguageProperty mlp)
                {
                    evi.Value = mlp.Value;
                    evi.ValueId = mlp.ValueId;
                }

                if (sme is AasCore.Aas3_0_RC02.Range rng)
                    evi.Value = new[] { rng.Min, rng.Max };

                // add 
                DiaryDataDef.AddAndSetTimestamps(rf, evi, isCreate: false);
            }

        }
    }
}
