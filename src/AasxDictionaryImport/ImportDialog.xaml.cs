/*
 * Copyright (c) 2020 SICK AG <info@sick.de>
 *
 * This software is licensed under the Apache License 2.0 (Apache-2.0).
 * The ExcelDataReder dependency is licensed under the MIT license
 * (https://github.com/ExcelDataReader/ExcelDataReader/blob/develop/LICENSE).
 */

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AasxGlobalLogging;

namespace AasxDictionaryImport
{
    /// <summary>
    /// The main import dialog.  The main elements of the dialog are a list view (the left half of the main panel) that
    /// lists all available top-level elements and a tree view (the right half of the main panel) that displays the
    /// structure of one or more top-level elements that have been selected in the list view.  Elements of the tree view
    /// can be checked or unchecked to determine whether they should be imported.  The selection can be accessed using
    /// the GetResult method.
    /// <para>
    /// The data source can be selected using a combo box.  If the user wants to use other data sources than those
    /// shipped with the AASX Package Exlporer, they can select a custom directory.  This dialog could be extended by
    /// adding another button that fetches a data source from the network.
    /// </para>
    /// </summary>
    internal partial class ImportDialog : Window
    {
        public ISet<Model.IDataProvider> DataProviders = new HashSet<Model.IDataProvider> {
            new Cdd.DataProvider(),
        };
        public Model.IDataContext? Context;
        private readonly ObservableCollection<Model.IElement> _topLevelElements
            = new ObservableCollection<Model.IElement>();
        private readonly ObservableCollection<ElementWrapper> _detailsElements
            = new ObservableCollection<ElementWrapper>();
        private string _filter = string.Empty;

        public ListCollectionView TopLevelView { get; }

        public ListCollectionView DetailsView { get; }

        public string Filter
        {
            get { return _filter; }
            set
            {
                var lowerValue = value.ToLower();
                if (_filter != lowerValue)
                {
                    _filter = lowerValue;
                    ApplyFilter();
                }
            }
        }

        public ImportDialog()
        {
            DataContext = this;

            TopLevelView = new ListCollectionView(_topLevelElements);
            DetailsView = new ListCollectionView(_detailsElements);

            InitializeComponent();

            foreach (var provider in DataProviders)
                foreach (var source in provider.FindDefaultDataSources())
                    ComboBoxSource.Items.Add(source);

            DataSourceLabel.Content = String.Join(", ", DataProviders.Select(p => p.Name));
        }

        public IEnumerable<Model.IElement> GetResult()
        {
            return _detailsElements.Select(e => e.Element);
        }

        private void UpdateImportButton()
        {
            ButtonImport.IsEnabled = _detailsElements.Any(w => w.IsChecked != false);
        }

        private string? GetImportDirectory()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select the import directory."
            };

            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                ? dialog.SelectedPath : null;
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrEmpty(_filter))
            {
                TopLevelView.Filter = null;
            }
            else
            {
                var parts = _filter.Split(' ').Where(s => s.Length > 0);
                TopLevelView.Filter = o =>
                {
                    if (!(o is Model.IElement element))
                        return false;

                    return element.Match(parts);
                };
            }
        }

        private void ButtonImport_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ClassViewControl_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Context == null)
                return;

            _detailsElements.Clear();

            foreach (var item in ClassViewControl.SelectedItems)
            {
                if (item is Model.IElement element)
                {
                    var wrapper = new ElementWrapper(element);
                    wrapper.PropertyChanged += ClassWrapper_PropertyChanged;
                    _detailsElements.Add(wrapper);
                }
            }

            UpdateImportButton();
        }

        private void ClassWrapper_PropertyChanged(object o, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked")
                UpdateImportButton();
        }

        private void ComboBoxSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Title = "IEC CDD Import";
            _topLevelElements.Clear();
            _detailsElements.Clear();

            if (ComboBoxSource.SelectedItem is Model.IDataSource source)
            {
                try
                {
                    try
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        Context = source.Load();
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }

                    foreach (var cls in Context.LoadSubmodels())
                    {
                        _topLevelElements.Add(cls);
                    }
                    Title = $"IEC CDD Import [{source}]";

                    if (ClassViewControl.Items.Count > 0)
                        ClassViewControl.SelectedItem = ClassViewControl.Items[0];
                }
                catch (Model.ImportException ex)
                {
                    Log.Error(ex, "Could not load the selected data source.");
                    MessageBox.Show(
                     "Could not load the selected data source.\n" +
                     "Details: " + ex.Message,
                     "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ButtonOpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            var path = GetImportDirectory();
            if (path == null)
                return;

            var validProviders = DataProviders.Where(p => p.IsValidPath(path)).ToList();
            if (validProviders.Count != 1)
                return;

            var source = validProviders.First().OpenPath(path);

            foreach (var item in ComboBoxSource.Items)
            {
                if (source.Equals(item))
                {
                    ComboBoxSource.SelectedItem = item;
                    return;
                }
            }

            ComboBoxSource.Items.Add(source);
            ComboBoxSource.SelectedItem = source;
        }

        private void ViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Model.IElement? element = null;

            if (sender is TreeViewItem treeViewItem && treeViewItem.IsSelected)
                element = (treeViewItem.DataContext as ElementWrapper)?.Element;
            else if (sender is ListViewItem listViewItem && listViewItem.IsSelected)
                element = listViewItem.DataContext as Model.IElement;

            if (element != null)
            {
                e.Handled = true;

                var dialog = new ElementDetailsDialog(element);
                dialog.Show();
                dialog.Activate();
            }
        }
    }

    internal class ElementWrapper : INotifyPropertyChanged
    {
        private bool? _isChecked = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Model.IElement Element { get; }

        public ElementWrapper? Parent { get; }

        public string Id => Element.Id;

        public string Name => Element.Name;

        public bool IsExpanded => Parent == null;

        public bool? IsChecked
        {
            get { return _isChecked; }
            set { SetIsChecked(value == true, true, true); }
        }

        public List<ElementWrapper> Children { get; }

        public ElementWrapper(Model.IElement element, ElementWrapper? parent = null)
        {
            Parent = parent;
            Element = element;
            Children = element.Children.Select(e => new ElementWrapper(e, this)).ToList();

            Element.IsSelected = _isChecked != false;
        }

        protected void PropagateIsChecked(bool up, bool down)
        {
            if (up)
                Parent?.UpdateIsChecked();
            if (down)
            {
                foreach (var child in Children)
                    child.SetIsChecked(IsChecked, false, true);
            }
        }

        public void UpdateIsChecked()
        {
            // tri-state checkbox:  true = checked, null = partial, false = unchecked
            var any = false;
            var all = true;
            foreach (var child in Children)
            {
                if (child.IsChecked == true || child.IsChecked == null)
                    any = true;
                if (child.IsChecked != true)
                    all = false;
            }

            bool? isChecked;
            if (all)
                isChecked = true;
            else if (any)
                isChecked = null;
            else
                isChecked = false;
            SetIsChecked(isChecked, true, false);
        }

        public void SetIsChecked(bool? value, bool propagateUp, bool propagateDown)
        {
            if (_isChecked == value)
                return;

            _isChecked = value;
            Element.IsSelected = value != false;
            PropagateIsChecked(propagateUp, propagateDown);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsChecked"));
        }
    }
}
