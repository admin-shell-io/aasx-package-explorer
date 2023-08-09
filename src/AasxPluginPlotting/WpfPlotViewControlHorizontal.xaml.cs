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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AasxPluginPlotting
{
    /// <summary>
    /// Interaktionslogik für WpfPlotViewControl.xaml
    /// </summary>
    public partial class WpfPlotViewControlHorizontal : UserControl, IWpfPlotViewControl
    {
        public ScottPlot.WpfPlot WpfPlot { get { return WpfPlotItself; } }
        public ContentControl ContentControl => this;

        public event Action<WpfPlotViewControlHorizontal, int> ButtonClick;

        public string Text { get { return TextboxInfo.Text; } set { TextboxInfo.Text = value; } }

        public bool AutoScaleX
        {
            get => true == ButtonScaleX.IsChecked;
            set => ButtonScaleX.IsChecked = value;
        }

        public bool AutoScaleY
        {
            get => true == ButtonScaleY.IsChecked;
            set => ButtonScaleY.IsChecked = value;
        }

        public WpfPlotViewControlHorizontal()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonHPlus)
                ButtonClick?.Invoke(this, 1);
            if (sender == ButtonHMinus)
                ButtonClick?.Invoke(this, 2);
            if (sender == ButtonVPlus)
                ButtonClick?.Invoke(this, 3);
            if (sender == ButtonVMinus)
                ButtonClick?.Invoke(this, 4);
            if (sender == ButtonAuto)
                ButtonClick?.Invoke(this, 5);
            if (sender == ButtonLarger)
                ButtonClick?.Invoke(this, 6);
            if (sender == ButtonSmaller)
                ButtonClick?.Invoke(this, 7);
        }

        public void DefaultButtonClicked(WpfPlotViewControlHorizontal sender, int ndx)
        {
            // access
            var wpfPlot = sender?.WpfPlot;
            if (wpfPlot == null)
                return;

            if (ndx == 1 || ndx == 2)
            {
                // Horizontal scale Plus / Minus
                var ax = wpfPlot.Plot.GetAxisLimits();
                var width = Math.Abs(ax.XMax - ax.XMin);

                if (ndx == 1)
                    wpfPlot.Plot.SetAxisLimits(xMin: ax.XMin - width / 2, xMax: ax.XMax + width / 2);

                if (ndx == 2)
                    wpfPlot.Plot.SetAxisLimits(xMin: ax.XMin + width / 4, xMax: ax.XMax - width / 4);

                // no autoscale for X
                AutoScaleX = false;

                // show
                wpfPlot.Render();
            }

            if (ndx == 3 || ndx == 4)
            {
                // Vertical scale Plus / Minus
                var ax = wpfPlot.Plot.GetAxisLimits();
                var height = Math.Abs(ax.YMax - ax.YMin);

                if (ndx == 3)
                    wpfPlot.Plot.SetAxisLimits(yMin: ax.YMin - height / 2, yMax: ax.YMax + height / 2);

                if (ndx == 4)
                    wpfPlot.Plot.SetAxisLimits(yMin: ax.YMin + height / 4, yMax: ax.YMax - height / 4);

                // no autoscale for Y
                AutoScaleY = false;

                // show
                wpfPlot.Render();
            }

            if (ndx == 5)
            {
                // switch auto scale ON and hope the best
                AutoScaleX = true;
                AutoScaleY = true;
            }

            if (ndx == 6)
            {
                // plot larger
                var h = sender.ActualHeight + 100;
                sender.Height = h;
                sender.MinHeight = h;
                sender.MaxHeight = h;

                // show
                wpfPlot.Render();
            }

            if (ndx == 7 && sender.Height >= 299)
            {
                // plot smaller
                var h = sender.ActualHeight - 100;
                sender.Height = h;
                sender.MinHeight = h;
                sender.MaxHeight = h;

                // show
                wpfPlot.Render();
            }
        }

    }
}
