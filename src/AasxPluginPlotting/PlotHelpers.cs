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
    }
}
