/*
Copyright (c) 2020 SICK AG <info@sick.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

#nullable enable

// ReSharper disable MergeIntoPattern

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace AasxDictionaryImport
{
    /// <summary>
    /// Query a data provider and a query string from the user that will be used to retrieve data from the network
    /// (see <see cref="Model.IDataProvider.Fetch"/>).
    /// </summary>
    internal partial class FetchOnlineDialog : Window
    {
        public Model.IDataProvider? DataProvider => ComboBoxProvider.SelectedItem as Model.IDataProvider;
        public string Query { get; set; } = string.Empty;

        public FetchOnlineDialog(Window owner, ICollection<Model.IDataProvider> providers)
        {
            Owner = owner;
            DataContext = this;

            InitializeComponent();

            foreach (var provider in providers)
                ComboBoxProvider.Items.Add(provider);
            if (providers.Count > 0)
                ComboBoxProvider.SelectedItem = providers.First();
        }

        private void ComboBoxProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (!(ComboBoxProvider.SelectedItem is Model.IDataProvider provider))
                return;
            if (!provider.IsFetchSupported)
                return;

            LabelQuery.Content = provider.FetchPrompt;
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
