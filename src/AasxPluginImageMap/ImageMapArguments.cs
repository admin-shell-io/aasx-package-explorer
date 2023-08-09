/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;

namespace AasxPluginImageMap
{
    public class ImageMapArguments
    {
        // ReSharper disable UnassignedField.Global

        public enum HorizontalAlign { Center, Left, Right };

        /// <summary>
        /// Bool evaluates value as bool and switches between colors[0] and colors[1].
        /// Switch evaluates value as double and switches between colors[0] and colors[1] depending on level0.
        /// Blend evaluates value as double and blends between colors[0] and colors[1] depending on level0 and level1.
        /// Index evaluates value as int and uses this value as index to colors-array.
        /// </summary>
        public enum ColorSetType { Bool, Switch, Blend, Index };

        /// <summary>
        /// C# string format string to format a double value pretty.
        /// Note: e.g. F4
        /// </summary>
        public string fmt;

        /// <summary>
        /// If given, specifies the factor of the font size with respect to regular size
        /// </summary>
        public double? fontsize;

        /// <summary>
        /// If given, specifies the distance between text display and outer background shape
        /// </summary>
        public double? padding;

        /// <summary>
        /// If given, specifies the horizontal alignment of the text display
        /// </summary>
        public HorizontalAlign? horalign;

        /// <summary>
        /// Use value of element to set a color value.
        /// </summary>
        public ColorSetType? colorset;

        /// <summary>
        /// Lower value compare level.
        /// </summary>
        public double level0 = 0.0;

        /// <summary>
        /// Uppe value compare level.
        /// </summary>
        public double level1 = 1.0;

        /// <summary>
        /// Array of colors. 
        /// First array item (color[0]) is often used as off/ false/ lower/ start color.
        /// Second array item (color[1]) is often used as on/ true/ upper/ end color.
        /// Third and more array items are used for indexed colors.
        /// </summary>
        public string[] colors;

        // ReSharper enable UnassignedField.Global

        public static ImageMapArguments Parse(string json)
        {
            if (!json.HasContent())
                return null;

            try
            {
                var res = Newtonsoft.Json.JsonConvert.DeserializeObject<ImageMapArguments>(json);
                return res;
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            return null;
        }
    }
}
