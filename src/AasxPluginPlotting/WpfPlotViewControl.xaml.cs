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
    public partial class WpfPlotViewControl : UserControl
    {
        public ScottPlot.WpfPlot WpfPlot { get { return WpfPlotItself; } }

        public event Action<WpfPlotViewControl, int> ButtonClick;

        public WpfPlotViewControl()
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
