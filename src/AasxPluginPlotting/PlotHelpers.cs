/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.ConceptModel;
using AdminShellNS;
using ScottPlot;

namespace AasxPluginPlotting
{
    public class PlotHelpers
    {
        public static Brush BrushFrom(System.Drawing.Color col)
        {
            return new SolidColorBrush(Color.FromArgb(col.A, col.R, col.G, col.B));
        }

        public static void SetOverallPlotProperties(
            IWpfPlotViewControl pvc,
            ScottPlot.WpfPlot wpfPlot,
            PlotArguments args,
            double defPlotHeight)
        {
            if (wpfPlot != null)
            {
                var pal = args?.GetScottPalette();
                if (pal != null)
                    wpfPlot.Plot.Palette = pal;
                var stl = args?.GetScottStyle();
                if (stl != null)
                    wpfPlot.Plot.Style(stl);

                var legend = wpfPlot.Plot.Legend(location: Alignment.UpperRight);
                legend.FontSize = 9.0f;
            }

            var cc = pvc?.ContentControl;
            if (cc != null)
            {
                var height = defPlotHeight;
                if (true == args?.height.HasValue)
                    height = args.height.Value;
                cc.MinHeight = height;
                cc.MaxHeight = height;
            }
        }

        public static void SetPlottableProperties(
            ScottPlot.Plottable.BarPlot bars, 
            PlotArguments args)
        {
        }

        public static void SetPlottableProperties(
            ScottPlot.Plottable.ScatterPlot scatter,
            PlotArguments args)
        {
            // access
            if (scatter == null)
                return;

            // set
            if (true == args?.linewidth.HasValue)
                scatter.LineWidth = args.linewidth.Value;

            if (true == args?.markersize.HasValue)
                scatter.MarkerSize = (float)args.markersize.Value;
        }

        public static void SetPlottableProperties(
            ScottPlot.Plottable.SignalPlot signal,
            PlotArguments args)
        {
            // access
            if (signal == null)
                return;

            // set
            if (true == args?.linewidth.HasValue)
                signal.LineWidth = args.linewidth.Value;

            if (true == args?.markersize.HasValue)
                signal.MarkerSize = (float)args.markersize.Value;
        }

        public static string EvalDisplayText(
                string minmalText, AdminShell.SubmodelElement sme,
                AdminShell.ConceptDescription cd = null,
                bool addMinimalTxt = false,
                string defaultLang = null,
                bool useIdShort = true)
        {
            var res = "" + minmalText;
            if (sme != null)
            {
                // best option: description of the SME itself
                string better = sme.description?.GetDefaultStr(defaultLang);

                // if still none, simply use idShort
                // SME specific non-multi-lang found better than CD multi-lang?!
                if (!better.HasContent() && useIdShort)
                    better = sme.idShort;

                // no? then look for CD information
                if (cd != null)
                {
                    if (!better.HasContent())
                        better = cd.GetDefaultPreferredName(defaultLang);
                    if (!better.HasContent())
                        better = cd.idShort;
                    if (better.HasContent() && true == cd.IEC61360Content?.unit.HasContent())
                        better += $" [{cd.IEC61360Content?.unit}]";
                }

                if (better.HasContent())
                {
                    res = better;
                    if (addMinimalTxt)
                        res += $" ({minmalText})";
                }
            }
            return res;
        }
    }
}
