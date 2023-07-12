/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxPackageLogic;
using AdminShellNS;
using AnyUi;
using BlazorUI.Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable MergeIntoPattern

namespace BlazorUI
{
    /// <summary>
    /// Single element for style pile, consisting of key and value
    /// </summary>
	public class StyleElem
    {
        public string Key = "";
        public string Value = "";

        public StyleElem() { }

        public StyleElem(StyleElem other)
        {
            if (other == null)
                return;
            Key = other.Key;
            Value = other.Value;
        }

        public StyleElem(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public StyleElem(string init)
        {
            var p = init.IndexOf(':');
            if (p < 0)
            {
                Key = init;
            }
            else
            {
                Key = init.Substring(0, p);
                Value = init.Substring(p + 1);
            }
        }
    }

    /// <summary>
    /// List of key value pair for the synthesis of style strings for HTML elements.
    /// Idea is to provide operators such as Add or Remove very easily.
    /// </summary>
    public class StylePile : List<StyleElem>
    {
        public StylePile() { }

        public StylePile(StylePile other)
        {
            Add(other);
        }

        public StylePile(string elemInit)
        {
            Add(elemInit);
        }

        public void Add(StylePile other)
        {
            if (other != null)
                foreach (var se in other)
                    Add(new StyleElem(se));
        }

        public void Add(string elemInit)
        {
            if (elemInit == null)
                return;
            var elems = elemInit.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var elem in elems)
                this.Add(new StyleElem(elem));
        }

        public void Add(string key, string value, bool doNotSetIfNull = false)
        {
            if (doNotSetIfNull && value == null)
                return;

            this.Add(new StyleElem(key, value));
        }

        public void Set(string key, string value,
            bool add = false,
            bool doNotSetIfNull = false)
        {
            if (doNotSetIfNull && value == null)
                return;

            var found = false;
            foreach (var elem in this)
                if (elem.Key.Trim() == key.Trim())
                {
                    elem.Value = value;
                    found = true;
                }

            if (!found && add)
                Add(key, value);
        }

        public void Set(StyleElem elem,
            bool add = false,
            bool doNotSetIfNull = false)
        {
            if (elem == null)
                return;
            Set(elem.Key, elem.Value, add, doNotSetIfNull);
        }

        public void Remove(string key)
        {
            this.RemoveAll((el) => el.Key.Trim() == key.Trim());
        }

        public override string ToString()
        {
            return string.Join("; ", this.Select((se) => $"{se.Key}:{se.Value}"));
        }

        public static StylePile operator +(StylePile sp, StyleElem elem)
        {
            sp.Add(elem);
            return sp;
        }

        public static StylePile operator +(StylePile sp, string init)
        {
            sp.Set(new StyleElem(init), add: true);
            return sp;
        }

        public static StylePile operator -(StylePile sp, StyleElem elem)
        {
            if (sp.Contains(elem))
                sp.Remove(elem);
            return sp;
        }

        public static StylePile operator -(StylePile sp, string key)
        {
            sp.Remove(key);
            return sp;
        }

        public void SetSpecifics(
            AnyUiBrush foreground = null,
            AnyUiBrush background = null,
            AnyUiBrush borderBrush = null,
            AnyUiThickness margin = null,
            AnyUiThickness padding = null,
            AnyUiThickness borderThickness = null,
            double? cornerRadius = null,
            AnyUiTextWrapping? textWrapping = null,
            double? fontSizeRel = null,
            AnyUiFontWeight? fontWeight = null,
            bool fillWidth = false,
            bool forceNoWrap = false
            )
        {
            // defaults, if not otherwise stated
            if (forceNoWrap || (textWrapping.HasValue && textWrapping.Value == AnyUiTextWrapping.NoWrap))
                Set("white-space", "nowrap", add: true);
            else
            {
                // https://stackoverflow.com/questions/36418352/css-word-wrap-break-word-wont-work
                Set("text-wrap", "break-word", add: true);
                Set("overflow-wrap", "break-word", add: true);
                Set("word-break", "break-all", add: true);
                Set("word-break", "break-all", add: true);
                Set("hyphens", "auto", add: true);
            }

            // colors
            Set("color", foreground?.HtmlRgb(), doNotSetIfNull: true, add: true);
            Set("background-color", background?.HtmlRgb(), doNotSetIfNull: true, add: true);
            Set("border-color", borderBrush?.HtmlRgb(), doNotSetIfNull: true, add: true);

            if (margin != null)
            {
                // https://developer.mozilla.org/de/docs/Web/CSS/margin
                if (margin.AllEqual)
                    Set("margin", $"{margin.Left}px", add: true);
                else
                    Set("margin", $"{margin.Top}px {margin.Right}px {margin.Bottom}px {margin.Left}px ",
                        add: true);
            }

            if (padding != null)
            {
                // https://developer.mozilla.org/de/docs/Web/CSS/padding
                if (padding.AllEqual)
                    Set("padding", $"{padding.Left}px", add: true);
                else
                    Set("padding", $"{padding.Top}px {padding.Right}px {padding.Bottom}px {padding.Left}px ",
                        add: true);
            }

            if (borderThickness != null)
            {
                // https://www.w3schools.com/cssref/pr_border-color.asp
                // https://developer.mozilla.org/de/docs/Web/CSS/border-width
                if (borderThickness.AllEqual)
                    Set("border-width", $"{borderThickness.Left}px", add: true);
                else
                    Set("border-width",
                        FormattableString.Invariant($"{borderThickness.Top}px {borderThickness.Right}px ") +
                        FormattableString.Invariant($"{borderThickness.Bottom}px {borderThickness.Left}px "),
                        add: true);
            }

            if (borderBrush != null || borderThickness != null)
                Set("border-style", $"solid", add: true);

            if (cornerRadius.HasValue)
                // in order to work properly, the table requieres: "border-collapse: separate;"
                // note: radius itself needs to be much larger compared to WPF
                Set("border-radius", FormattableString.Invariant($"{4.0 * cornerRadius}px"), add: true);

            if (textWrapping.HasValue && textWrapping.Value != AnyUiTextWrapping.NoWrap)
            {
                // https://developer.mozilla.org/de/docs/Web/CSS/white-space
                // https://developer.mozilla.org/de/docs/Web/CSS/overflow-wrap
                Set("white-space", "normal", add: true);
                Set("word-wrap", "break-word", add: true);
            }

            if (fontSizeRel.HasValue)
                Set("font-size", FormattableString.Invariant($"{fontSizeRel.Value}rem"), add: true);

            if (fontWeight.HasValue)
            {
                if (fontWeight == AnyUiFontWeight.Bold)
                    Set("font-weight", "bold", add: true);
            }
        }

        public void SetMinMaxWidthHeight(
            AnyUiUIElement elem,
            bool setWidth = true,
            bool setHeight = false)
        {
            if (!(elem is AnyUiFrameworkElement fe))
                return;

            var scale = (elem.DisplayData as AnyUiDisplayDataHtml)?.GetScale() ?? 1.0;

            if (setWidth)
            {
                if (fe.MinWidth.HasValue)
                    Set("min-width", FormattableString.Invariant($"{scale * fe.MinWidth.Value}px"), add: true);
                if (fe.MaxWidth.HasValue)
                    Set("max-width", FormattableString.Invariant($"{scale * fe.MaxWidth.Value}px"), add: true);
            }

            if (setHeight)
            {
                if (fe.MinHeight.HasValue)
                {
                    Set("min-height", FormattableString.Invariant($"{scale * fe.MinHeight.Value}px"), add: true);
                    Set("object-fit", "scale-down", add: true);
                }
                if (fe.MaxHeight.HasValue)
                {
                    Set("max-height", FormattableString.Invariant($"{scale * fe.MaxHeight.Value}px"), add: true);
                    Set("object-fit", "scale-down", add: true);
                }
            }
        }

        public void SetFillWidth(
            AnyUiUIElement element,
            AnyUiHtmlFillMode fillmode,
            AnyUiThickness margin = null,
            bool setMinMaxWidth = false,
            bool setMinMaxHeight = false,
            bool setInlineBlock = false
            )
        {
            if (fillmode == AnyUiHtmlFillMode.FillWidth)
            {
                Set("width", FormattableString.Invariant(
                    $"calc(100% - {margin?.Width ?? 0.0}px)"), add: true);
                Set("box-sizing", "border-box", add: true);
            }

            if (element != null && (setMinMaxWidth || setMinMaxHeight))
                SetMinMaxWidthHeight(element, setWidth: setMinMaxWidth, setHeight: setMinMaxHeight);

            if (setInlineBlock)
                Set("display", "inline-block", add: true);
        }


        public void SetAlignments(AnyUiUIElement elem,
            bool allowStretch = true,
            bool setVertical = false)
        {
            if (!(elem is AnyUiFrameworkElement fe))
                return;

            if (fe.HorizontalAlignment.HasValue && fe.HorizontalAlignment.Value == AnyUiHorizontalAlignment.Left)
            {
            }
            else
            if (fe.HorizontalAlignment.HasValue && fe.HorizontalAlignment.Value == AnyUiHorizontalAlignment.Right)
            {
                Set("float", "right", add: true);
            }
            else
            if (fe.HorizontalAlignment.HasValue && fe.HorizontalAlignment.Value == AnyUiHorizontalAlignment.Center)
            {
                // center needs to be handled in the grid cell!
            }
            else
            {
                Set("width", "100%", add: true);
                Set("box-sizing", "border-box", add: true);
            }

            // vertical as well (in general, should be set by the table)?
            // ReSharper disable PossibleInvalidOperationException
            if (setVertical)
            {
                if (fe.HorizontalAlignment.HasValue && fe.VerticalAlignment.Value == AnyUiVerticalAlignment.Center)
                {
                    Set("vertical-align", "center", add: true);
                }
            }
            // ReSharper enable PossibleInvalidOperationException
        }
    }
}