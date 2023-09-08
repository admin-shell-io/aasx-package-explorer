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

// ReSharper disable EventNeverSubscribedTo.Global

namespace AasxPluginPlotting
{
    /// <summary>
    /// Interaktionslogik für WpfPlotViewControl.xaml
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public partial class WpfPlotViewControlVertical : UserControl
    {
        public ScottPlot.WpfPlot WpfPlot { get { return WpfPlotItself; } }

        public event Action<WpfPlotViewControlVertical, int> ButtonClick;

        public WpfPlotViewControlVertical()
        {
            InitializeComponent();
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
    }
}
