/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using AasxIntegrationBase;
using AdminShellNS;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.Win32;

namespace AasxPluginAdvancedTextEditor
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControlAdvancedTextEditor : UserControl
    {
        //
        // Init
        //

        public UserControlAdvancedTextEditor()
        {
            InitializeComponent();

            this.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);

            propertyGridComboBox.SelectedIndex = 2;

            textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
            textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
            SearchPanel.Install(textEditor);
        }

        //
        // Interface
        //

        string currentFileName;

        public string Text
        {
            get { return textEditor.Text; }
            set { textEditor.Text = value; }
        }

        private string mimeType = "";
        public string MimeType
        {
            set
            {
                this.mimeType = value;
                TrySetMimeType(this.mimeType);
            }
        }

        //
        // Commands
        //

        private void TrySetMimeType(string mimeType)
        {
            // access
            if (this.highlightingComboBox == null)
                return;
            mimeType = ("" + mimeType).Trim().ToLower();

            // transfer to appropriate tag
            var tag = "";
            var m = Regex.Match(mimeType, @"(text|application)\/(\w+)");
            if (m.Success && m.Groups.Count >= 3)
                tag = m.Groups[2].ToString();
            if (!tag.HasContent())
                return;

            // activate mime type via setting the selection of the combo box
            foreach (var item in highlightingComboBox.Items)
                if (("" + item).Trim().ToLower() == tag)
                {
                    highlightingComboBox.SelectedItem = item;
                    break;
                }

            // set word wrap accordingly
            var mt = mimeType.ToLower().Trim();
            var isWordWrap = mt.Contains("markdown") || mt.Contains("asciidoc") 
                || mt.Contains("xml") || mt.Contains("json");
            textEditor.WordWrap = isWordWrap;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // try apply stored mime type
            TrySetMimeType(this.mimeType);
        }

        void openFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            if (dlg.ShowDialog() ?? false)
            {
                currentFileName = dlg.FileName;
                textEditor.Load(currentFileName);
                textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(
                    Path.GetExtension(currentFileName));
            }
        }

        void saveFileClick(object sender, EventArgs e)
        {
            if (currentFileName == null)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.DefaultExt = ".txt";
                if (dlg.ShowDialog() ?? false)
                {
                    currentFileName = dlg.FileName;
                }
                else
                {
                    return;
                }
            }
            textEditor.Save(currentFileName);
        }

        void propertyGridComboBoxSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (propertyGrid == null)
                return;
            switch (propertyGridComboBox.SelectedIndex)
            {
                case 0:
                    propertyGrid.SelectedObject = textEditor;
                    break;
                case 1:
                    propertyGrid.SelectedObject = textEditor.TextArea;
                    break;
                case 2:
                    propertyGrid.SelectedObject = textEditor.Options;
                    break;
            }
        }

        void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
        }

        void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
        }

        #region Folding
        FoldingManager foldingManager;
        object foldingStrategy;

        void HighlightingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (textEditor.SyntaxHighlighting == null)
            {
                foldingStrategy = null;
            }
            else
            {
                switch (textEditor.SyntaxHighlighting.Name)
                {
                    case "XML":
                        foldingStrategy = new XmlFoldingStrategy();
                        textEditor.TextArea.IndentationStrategy =
                            new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
                        break;
                    case "C#":
                    case "C++":
                    case "PHP":
                    case "Java":
                        textEditor.TextArea.IndentationStrategy =
                            new ICSharpCode.AvalonEdit.Indentation.CSharp.CSharpIndentationStrategy(
                                textEditor.Options);
                        break;
                    default:
                        textEditor.TextArea.IndentationStrategy =
                            new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
                        foldingStrategy = null;
                        break;
                }
            }
            if (foldingStrategy != null)
            {
                if (foldingManager == null)
                    foldingManager = FoldingManager.Install(textEditor.TextArea);
                UpdateFoldings();
            }
            else
            {
                if (foldingManager != null)
                {
                    FoldingManager.Uninstall(foldingManager);
                    foldingManager = null;
                }
            }
        }

        void UpdateFoldings()
        {
        }
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonFontPlus)
            {
                textEditor.FontSize += 4;
            }

            if (sender == ButtonFontMinus)
            {
                textEditor.FontSize = Math.Max(10, textEditor.FontSize);
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)
                && e.Key == Key.OemPlus)
            {
                textEditor.FontSize += 4;
                e.Handled = true;
            }

            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)
                && e.Key == Key.OemPlus)
            {
                textEditor.FontSize = Math.Max(10, textEditor.FontSize);
                e.Handled = true;
            }
        }
    }
}
