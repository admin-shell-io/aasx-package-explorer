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

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für VisualElementHistoryControl.xaml
    /// </summary>
    public partial class VisualElementHistoryControl : UserControl
    {
        // members

        public event EventHandler<VisualElementGeneric> VisualElementRequested = null;

        private List<VisualElementGeneric> history = new List<VisualElementGeneric>();

        // init

        public VisualElementHistoryControl()
        {
            InitializeComponent();

            // initial state: without content
            buttonBack.IsEnabled = false;
        }

        // functions

        /// <summary>
        /// Clears the history
        /// </summary>
        public void Clear()
        {
            // clear
            history.Clear();

            // not enabled
            buttonBack.IsEnabled = false;
        }

        public void Push(VisualElementGeneric ve)
        {
            // add, only if not already there
            if (history.Count < 1 || history[history.Count - 1] != ve)
                history.Add(ve);

            // is enabled
            buttonBack.IsEnabled = true;
        }

        private void buttonBack_Click(object sender, RoutedEventArgs e)
        {
            // anything
            if (history == null || history.Count < 1)
                return;

            // pop last (as this the already displayed one)
            history.RemoveAt(history.Count - 1);

            // may be disable ..
            buttonBack.IsEnabled = history.Count > 0;

            // give back the one prior to it
            if (history.Count < 1)
                return;
            var ve = history[history.Count - 1];

            // trigger event
            this.VisualElementRequested?.Invoke(this, ve);
        }
    }
}
