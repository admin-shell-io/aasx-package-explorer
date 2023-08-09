/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
    public class PlotBufferFixLen
    {
        public const int BufferSize = 512;

        public ScottPlot.Plottable.IPlottable Plottable;
        public double[] Xdata = new double[BufferSize];
        public double[] Ydata = new double[BufferSize];
        public int BufferLevel = 0;

        public void Push(double y, double? x = null)
        {
            if (!x.HasValue)
                x = DateTime.UtcNow.ToOADate();

            if (BufferLevel < BufferSize)
            {
                // simply add
                Xdata[BufferLevel] = x.Value;
                Ydata[BufferLevel] = y;

                if (Plottable is ScottPlot.Plottable.SignalPlot sigp)
                    sigp.MaxRenderIndex = BufferLevel;
                if (Plottable is ScottPlot.Plottable.ScatterPlot scap)
                    scap.MaxRenderIndex = BufferLevel;

                BufferLevel++;
            }
            else
            {
                // brute shift
                Array.Copy(Xdata, 1, Xdata, 0, BufferSize - 1);
                Array.Copy(Ydata, 1, Ydata, 0, BufferSize - 1);
                Xdata[BufferSize - 1] = x.Value;
                Ydata[BufferSize - 1] = y;

                if (Plottable is ScottPlot.Plottable.SignalPlot sigp)
                    sigp.MaxRenderIndex = BufferSize - 1;
                if (Plottable is ScottPlot.Plottable.ScatterPlot scap)
                    scap.MaxRenderIndex = BufferSize - 1;
            }
        }
    }
}
