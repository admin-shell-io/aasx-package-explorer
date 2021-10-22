/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AasxIntegrationBase;

namespace AasxPluginPlotting
{
    public class PlotArguments
    {
        // ReSharper disable UnassignedField.Global

        /// <summary>
        /// Symbolic name of a group, a plot shall assigned to
        /// </summary>            
        public string grp;

        /// <summary>
        /// C# string format string to format a double value pretty
        /// </summary>
        public string fmt;

        /// <summary>
        /// Min and max values of the axes
        /// </summary>
        public double? xmin, ymin, xmax, ymax;

        /// <summary>
        /// Skip this plot
        /// </summary>
        public bool skip;

        /// <summary>
        /// Keep the plot on the same Y axis as the plot before
        /// </summary>
        public bool sameaxis;

        /// <summary>
        /// Dimensions of the overall plot
        /// </summary>
        public double? height, width;

        /// <summary>
        /// For pie/bar-charts: initially display labels, values or percent values
        /// </summary>
        public bool labels, values, percent;

        /// <summary>
        /// Assign a predefined palette or style
        /// Palette: Aurora, Category10, Category20, ColorblindFriendly, Dark, DarkPastel, Frost, Microcharts, 
        ///          Nord, OneHalf, OneHalfDark, PolarNight, Redness, SnowStorm, Tsitsulin 
        /// Style: Black, Blue1, Blue2, Blue3, Burgundy, Control, Default, Gray1, Gray2, Light1, Light2, 
        ///        Monospace, Pink, Seaborn
        /// </summary>
        public string palette, style;

        public enum Type { None, Bars, Pie }

        /// <summary>
        /// Make a plot to be a bar or pie chart.
        /// Can be associated to TimeSeries or TimeSeriesVariable/ DataPoint
        /// </summary>
        public Type type;

        // ReSharper enable UnassignedField.Global

        public static PlotArguments Parse(string json)
        {
            if (!json.HasContent())
                return null;

            try
            {
                var res = Newtonsoft.Json.JsonConvert.DeserializeObject<PlotArguments>(json);
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
