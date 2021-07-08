/*
Copyright (c) 2020 SICK AG <info@sick.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

#nullable enable

// ReSharper disable MergeIntoPattern

using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace AasxDictionaryImport
{
    /// <summary>
    /// Display detail information for an element in the import dialog.  This dialog supports simple string values
    /// (AddString) as well as multi-language strings (AddMultiString).  But as the current data model only returns
    /// simple string values (IElement.GetDetails), AddMultiString is currently unused.
    /// </summary>
    internal partial class ElementDetailsDialog : Window
    {
        public Model.IElement Element { get; }

        public ElementDetailsDialog(Model.IElement element)
        {
            DataContext = this;
            Element = element;

            InitializeComponent();

            var details = element.GetDetails();
            foreach (var field in details.Keys)
                AddString(field, details[field]);

            Title = $"{Element.DisplayName} [{Element.DataSource}]";
        }

        private void AddString(string label, string value)
        {
            AddRow();
            CreateLabel(label);
            CreateTextBox(value);
        }

        private void AddMultiString(string label, MultiString value)
        {
            AddRow();
            CreateLabel(label);
            CreateMultiStringTextBox(value);
        }

        private void AddRow()
        {
            Grid.RowDefinitions.Add(new RowDefinition());
        }

        private void AddElement(FrameworkElement element, int column)
        {
            Grid.SetRow(element, Grid.RowDefinitions.Count - 1);
            Grid.SetColumn(element, column);
            Grid.Children.Add(element);
        }

        private void CreateLabel(string content)
        {
            var label = new Label { Content = content };
            AddElement(label, 0);
        }

        private TextBox CreateTextBox(string text)
        {
            var textBox = new TextBox { Text = text };
            AddElement(textBox, 2);
            return textBox;
        }

        private void CreateLanguageComboBox(MultiString ms, TextBox textBox)
        {
            var comboBox = new ComboBox();
            AddElement(comboBox, 1);

            foreach (var lang in ms.Languages)
                comboBox.Items.Add(lang);
            comboBox.Tag = textBox;
            textBox.Tag = ms;

            if (comboBox.Items.Count > 0)
                comboBox.SelectedItem = ms.DefaultLanguage;
        }

        private void CreateMultiStringTextBox(MultiString ms)
        {
            var textBox = CreateTextBox(ms.GetDefault());
            CreateLanguageComboBox(ms, textBox);

            var toolTip = new StringBuilder();
            foreach (var lang in ms.AvailableLanguages)
            {
                if (toolTip.Length > 0)
                    toolTip.AppendLine();
                toolTip.Append($"[{lang}] {ms.Get(lang)}");
            }
            textBox.ToolTip = new ToolTip { Content = toolTip.ToString() };
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                if (comboBox.Tag is TextBox textBox)
                {
                    if (textBox.Tag is MultiString ms)
                    {
                        textBox.Text = ms.Get(comboBox.SelectedItem.ToString());
                    }
                }
            }
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonViewInBrowser_Click(object sender, RoutedEventArgs e)
        {
            var url = Element.GetDetailsUrl();
            if (url != null)
                System.Diagnostics.Process.Start(url.AbsoluteUri);
        }
    }
}
