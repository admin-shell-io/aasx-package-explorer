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


namespace AasxPluginPlotting
{
    public class PlotArguments
    {
        // ReSharper disable UnassignedField.Global

        /// <summary>
        /// Display title of the respective entity to be shown in the panel
        /// </summary>
        public string title;

        /// <summary>
        /// Symbolic name of a group, a plot shall assigned to
        /// </summary>            
        public string grp;

        /// <summary>
        /// C# string format string to format a double value pretty.
        /// Note: e.g. F4
        /// </summary>
        public string fmt;

        /// <summary>
        /// Unit to display.
        /// </summary>
        public string unit;

        /// <summary>
        /// Min and max values of the axes
        /// </summary>
        public double? xmin, ymin, xmax, ymax;

        /// <summary>
        /// Skip this plot in charts display
        /// </summary>
        public bool skip;

        /// <summary>
        /// Keep the plot on the same Y axis as the plot before
        /// </summary>
        public bool sameaxis;

        /// <summary>
        /// Plottables will be shown with ascending order
        /// </summary>
        public int order = -1;

        /// <summary>
        /// Width of plot line, size of its markers
        /// </summary>
        public double? linewidth, markersize;

        /// <summary>
        /// In order to display more than one bar plottable, set the bar-width to 0.5 or 0.33
        /// and the bar-offset to -0.5 .. +0.5
        /// </summary>
        public double? barwidth, barofs;

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

        public enum Source { Timer, Event }

        /// <summary>
        /// Specify source for value updates.
        /// </summary>
        public Source src;

        /// <summary>
        /// Specifies the timer interval in milli-seconds. Minimum value 100ms.
        /// Applicable on: Submodel
        /// </summary>
        public int timer;

        /// <summary>
        /// Instead of displaying a list of plot items, display a set of tiles.
        /// Rows and columns can be assigned to the individual tiles.
        /// Applicable on: Submodel
        /// </summary>
        public bool tiles;

        /// <summary>
        /// Defines the zero-based row- and column position for tile based display.
        /// The span-settings allow stretching over multiple (>1) tiles.
        /// Applicable on: Properties
        /// </summary>
        public int? row, col, rowspan, colspan;

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

        public ScottPlot.IPalette GetScottPalette()
        {
            if (palette?.HasContent() == true)
                foreach (var pl in ScottPlot.Palette.GetPalettes())
                    if (pl.Name.ToLower().Trim() == palette.ToLower().Trim())
                        return pl;
            return null;
        }

        public ScottPlot.Styles.IStyle GetScottStyle()
        {
            if (style?.HasContent() == true)
                foreach (var st in ScottPlot.Style.GetStyles())
                    if (st.GetType().Name.ToLower().Trim() == style.ToLower().Trim())
                        return st;
            return null;
        }
    }
}
