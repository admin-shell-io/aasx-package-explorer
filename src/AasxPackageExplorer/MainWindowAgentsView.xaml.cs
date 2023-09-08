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
using AasxIntegrationBase;

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
            var ndx = gc.ColumnDefinitions.Count;

            gc.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(1.0, GridUnitType.Star),
                MaxWidth = 300
            });
            Grid.SetColumn(mini, ndx);
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

        private bool _alreadyLoaded = false;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // UserControl.Loaded could be fired multiple time, when rendered below a TabPanel!
            if (_alreadyLoaded)
                return;
            _alreadyLoaded = true;

            // on loading, clear contest
            var gc = GridContent;
            gc.Children.Clear();
            gc.ColumnDefinitions.Clear();
        }
    }
}
