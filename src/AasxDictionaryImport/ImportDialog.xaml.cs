/*
Copyright (c) 2020 SICK AG <info@sick.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;
using AasxDictionaryImport.Model;

namespace AasxDictionaryImport
{
    /// <summary>
    /// The main import dialog.  The main elements of the dialog are a list view (the left half of the main panel) that
    /// lists all available elements depending on the import mode and a tree view (the right half of the main panel)
    /// that displays the structure of one or more elements that have been selected in the list view.  Elements of the
    /// tree view can be checked or unchecked to determine whether they should be imported.  The selection can be
    /// accessed using the GetResult method.
    /// <para>
    /// The data source can be selected using a combo box.  If the user wants to use other data sources than those
    /// shipped with the AASX Package Explorer, they can select a custom directory.  This dialog could be extended by
    /// adding another button that fetches a data source from the network.
    /// </para>
    /// </summary>
    internal partial class ImportDialog : Window
    {
        public ISet<Model.IDataProvider> DataProviders = new HashSet<Model.IDataProvider> {
            new Cdd.DataProvider(),
            new Eclass.DataProvider(),
        };
        public Model.IDataContext? Context;
        private readonly ObservableCollection<Model.IElement> _topLevelElements
            = new ObservableCollection<Model.IElement>();
        private readonly ObservableCollection<ElementWrapper> _detailsElements
            = new ObservableCollection<ElementWrapper>();
        private string _filter = string.Empty;

        public ImportMode ImportMode { get; }

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

        public ImportDialog(Window owner, ImportMode importMode, string defaultSourceDir)
        {
            Owner = owner;
            DataContext = this;

            ImportMode = importMode;
            TopLevelView = new ListCollectionView(_topLevelElements);
            DetailsView = new ListCollectionView(_detailsElements);

            InitializeComponent();

            foreach (var provider in DataProviders)
                foreach (var source in provider.FindDefaultDataSources(defaultSourceDir))
                    ComboBoxSource.Items.Add(source);

            DataSourceLabel.Content = String.Join(", ", DataProviders.Select(p => p.Name));
            ButtonFetchOnline.IsEnabled = DataProviders.Any(p => p.IsFetchSupported);

            LoadCachedImports();
        }

        private void LoadCachedImports()
        {
            var indexFile = Path.Combine(Path.Combine(Path.GetTempPath(), $"aasx.import"), $"cache.index.xml");
            if (File.Exists(indexFile))
            {
                XDocument doc = XDocument.Load(indexFile);
                foreach (var cacheEl in doc.Root.Elements("CachedElement"))
                {
                    var fileName = cacheEl.Attribute("FileName").Value;
                    if (File.Exists(fileName) && cacheEl.Attribute("Source").Value.Equals("Online"))
                    {
                        ICollection<Model.IDataProvider> providers =
                            DataProviders.Where(p => p.IsFetchSupported).ToList();
                        foreach (var provider in providers)
                        {
                            if (cacheEl.Attribute("Provider").Value.Equals(provider.Name))
                            {
                                var source = provider.OpenPath(fileName, Model.DataSourceType.Online);
                                ComboBoxSource.Items.Add(source);
                                break;
                            }
                        }
                    }
                }

            }
        }
        private void SaveCachedIndex()
        {
            var indexFile = Path.Combine(Path.Combine(Path.GetTempPath(), $"aasx.import"), $"cache.index.xml");
            XDocument doc = new XDocument(
                new XElement("Cache")
                );
            var rootEl = doc.Root;
            foreach (var listItem in ComboBoxSource.Items)
            {
                if (listItem is Model.FileSystemDataSource source)
                {
                    var el = new XElement("CachedElement");
                    el.SetAttributeValue("FileName", source.Path);
                    el.SetAttributeValue("Source", source.Type.ToString());
                    el.SetAttributeValue("Provider", source.DataProvider.ToString());
                    rootEl.Add(el);
                }
            }
            doc.Save(indexFile);
        }

        public IEnumerable<Model.IElement> GetResult()
        {
            return _detailsElements.Select(e => e.Element);
        }

        private void UpdateImportButton()
        {
            ButtonImport.IsEnabled = _detailsElements.Any(w => w.IsChecked != false);
        }

        private string? GetImportPath()
        {
            using var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Select a local file for the Dictionary Import"
            };

            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                ? dialog.FileName : null;
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

        private IEnumerable<Model.IElement> GetElements(Model.IDataContext context)
        {
            switch (ImportMode)
            {
                case ImportMode.SubmodelElements:
                    return context.LoadSubmodelElements();
                case ImportMode.Submodels:
                    return context.LoadSubmodels();
                default:
                    return new List<Model.IElement>();
            }
        }

        private void ButtonImport_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            SaveCachedIndex();
            Close();
        }
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            SaveCachedIndex();
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
            Title = "Dictionary Import";
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

                    foreach (var element in GetElements(Context))
                    {
                        _topLevelElements.Add(element);
                    }
                    Title = $"Dictionary Import [{source}]";

                    if (ClassViewControl.Items.Count > 0)
                        ClassViewControl.SelectedItem = ClassViewControl.Items[0];
                    CheckBoxAllIecCddAttributes.Visibility = (source.DataProvider is Cdd.DataProvider) ?
                        Visibility.Visible : Visibility.Hidden;
                }
                catch (Model.ImportException ex)
                {
                    AasxPackageExplorer.Log.Singleton.Error(ex, "Could not load the selected data source.");
                    MessageBox.Show(
                     "Could not load the selected data source.\n" +
                     "Details: " + ex.Message,
                     "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ButtonOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var path = GetImportPath();
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

        private void ButtonFetchOnline_Click(object sender, RoutedEventArgs e)
        {
            var fetchProviders = DataProviders.Where(p => p.IsFetchSupported).ToList();
            if (fetchProviders.Count == 0)
                return;

            var dialog = new FetchOnlineDialog(this, fetchProviders);
            if (dialog.ShowDialog() != true || dialog.DataProvider == null)
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var source = dialog.DataProvider.Fetch(dialog.Query);
                ComboBoxSource.Items.Add(source);
                ComboBoxSource.SelectedItem = source;
            }
            catch (Model.ImportException ex)
            {
                AasxPackageExplorer.Log.Singleton.Error(ex,
                        $"Could not fetch data from {dialog.DataProvider} using the query {dialog.Query}.");
                MessageBox.Show(
                        "Could not fetch the requested data.\n" +
                        "Details: " + ex.Message,
                        "Fetch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Model.IElement? element = sender switch
            {
                TreeViewItem { IsSelected: true } treeViewItem =>
                    (treeViewItem.DataContext as ElementWrapper)?.Element,
                ListViewItem { IsSelected: true } listViewItem =>
                    listViewItem.DataContext as Model.IElement,
                _ => null
            };

            if (element != null)
            {
                e.Handled = true;

                var dialog = new ElementDetailsDialog(element);
                dialog.Show();
                dialog.Activate();
            }
        }

        private void CheckBoxAllIecCddAttributes_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxSource.SelectedItem is Cdd.DataSource source)
                source.ImportAllAttributes = CheckBoxAllIecCddAttributes.IsChecked == true;

            if (CheckBoxAllIecCddAttributes.IsChecked == true)
            {
                if (MessageBox.Show(
                    "Only the free IEC CDD attributes may be used without restrictions. Please read the End User " +
                    "License Agreement for IEC Common Data Dictionary (CDD) for more information. Do you want to " +
                    "open the EULA now?",
                    "IEC CDD Import", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("https://cdd.iec.ch/CDD/iec61360/iec61360.nsf/License?openPage");
                }
            }
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            ComboBoxSource.Items.Clear();
            _topLevelElements.Clear();
            _detailsElements.Clear();
        }
    }

    /// <summary>
    /// The import mode that determines which elements are shown in the element
    /// list on the left side of the import dialog.
    /// </summary>
    internal enum ImportMode
    {
        /// <summary>
        /// Show all elements that correspond to AAS submodels.
        /// </summary>
        Submodels,
        /// <summary>
        /// Show all elements that correspond to AAS submodel elements, i. e.
        /// collections and properties.
        /// </summary>
        SubmodelElements,
    }

    internal class ElementWrapper : INotifyPropertyChanged
    {
        private bool? _isChecked = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Model.IElement Element { get; }

        public ElementWrapper? Parent { get; }

        public string Id => Element.Id;

        public string Name => Element.DisplayName;

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

            _isChecked = Element.IsSelected;
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
