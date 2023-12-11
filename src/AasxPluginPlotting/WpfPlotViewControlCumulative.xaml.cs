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
    public partial class WpfPlotViewControlCumulative : UserControl, IWpfPlotViewControl
    {
        public ScottPlot.WpfPlot WpfPlot { get { return WpfPlotItself; } }
        public ContentControl ContentControl => this;

        public event Action<WpfPlotViewControlCumulative, int> LatestSamplePositionChanged;

        public string Text { get { return TextboxInfo.Text; } set { TextboxInfo.Text = value; } }

        protected ScottPlot.Plottable.IPlottable _plottable;
        public ScottPlot.Plottable.IPlottable ActivePlottable
        {
            set { _plottable = value; AdjustButtonVisibility(); }
            get { return _plottable; }
        }

        public bool AutoScaleX { get => false; set {; } }
        public bool AutoScaleY { get => false; set {; } }

        protected int _latestSamplePosition = 0;
        public int LatestSamplePosition
        {
            get { return _latestSamplePosition; }
            set
            {
                _latestSamplePosition = value;
                TextBoxSamplePos.Text = "" + _latestSamplePosition;
            }
        }

        public WpfPlotViewControlCumulative()
        {
            InitializeComponent();
        }

        protected void AdjustButtonVisibility()
        {
            var isPie = _plottable is ScottPlot.Plottable.PiePlot;
            var isBar = _plottable is ScottPlot.Plottable.BarPlot;

            ButtonLabels.Visibility = isPie ? Visibility.Visible : Visibility.Collapsed;
            ButtonValues.Visibility = (isPie || isBar) ? Visibility.Visible : Visibility.Collapsed;
            ButtonPercentage.Visibility = isPie ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_plottable is ScottPlot.Plottable.PiePlot pie)
            {
                if (sender == ButtonLabels) pie.ShowLabels = !pie.ShowLabels;
                if (sender == ButtonValues) pie.ShowValues = !pie.ShowValues;
                if (sender == ButtonPercentage) pie.ShowPercentages = !pie.ShowPercentages;
            }

            if (_plottable is ScottPlot.Plottable.BarPlot bar)
            {
                if (sender == ButtonValues) bar.ShowValuesAboveBars = !bar.ShowValuesAboveBars;
            }

            if (sender == ButtonPrev)
            {
                _latestSamplePosition--;
                TextBoxSamplePos.Text = "" + _latestSamplePosition;
                LatestSamplePositionChanged?.Invoke(this, _latestSamplePosition);
            }

            if (sender == ButtonNext && _latestSamplePosition < 0)
            {
                _latestSamplePosition++;
                TextBoxSamplePos.Text = "" + _latestSamplePosition;
                LatestSamplePositionChanged?.Invoke(this, _latestSamplePosition);
            }

            WpfPlotItself.Render();
        }

        private void TextBoxSamplePos_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (int.TryParse(TextBoxSamplePos.Text, out var i))
                {
                    _latestSamplePosition = Math.Min(0, i);
                    TextBoxSamplePos.Text = "" + _latestSamplePosition;
                    LatestSamplePositionChanged?.Invoke(this, _latestSamplePosition);
                }
            }
        }
    }
}
