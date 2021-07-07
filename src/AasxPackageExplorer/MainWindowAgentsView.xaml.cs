using AasxIntegrationBase;
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
    /// Interaktionslogik für MainWindowAgentsView.xaml
    /// </summary>
    public partial class MainWindowAgentsView : UserControl
    {
        public MainWindowAgentsView()
        {
            InitializeComponent();
        }

        public IEnumerable<IFlyoutMini> Children
        {
            get
            {
                if (GridContent.Children == null)
                    yield break;
                foreach (var ch in GridContent.Children)
                    if (ch is IFlyoutMini mini)
                        yield return mini;
            }
        }

        public bool Contains(IFlyoutMini mini)
        {
            foreach (var ch in Children)
                if (ch == mini)
                    return true;
            return false;
        }

        public bool Add(UserControl mini)
        {
            // trivial
            if (mini == null)
                return false;

            var gc = GridContent;
            gc.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });
            gc.Children.Add(mini);

            return true;
        }

        public bool Remove(UserControl mini)
        {
            // trivial
            if (mini == null || !Contains(mini as IFlyoutMini))
                return false;

            var gc = GridContent;
            gc.Children.Remove(mini);
            gc.ColumnDefinitions.RemoveAt(gc.ColumnDefinitions.Count - 1);

            return true;
        }
    }
}
