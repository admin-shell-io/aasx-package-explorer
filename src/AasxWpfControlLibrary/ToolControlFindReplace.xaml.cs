/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AasxIntegrationBase;
using AasxPackageLogic;
using AdminShellNS;

namespace AasxPackageExplorer
{
    public partial class ToolControlFindReplace : UserControl
    {
        //
        // Members
        //

        public AdminShellUtil.SearchOptions TheSearchOptions = new AdminShellUtil.SearchOptions();
        public AdminShellUtil.SearchResults TheSearchResults = new AdminShellUtil.SearchResults();

        public AdminShell.AdministrationShellEnv TheAasEnv = null;

        public delegate void ResultSelectedDelegate(AdminShellUtil.SearchResultItem resultItem);

        public event ResultSelectedDelegate ResultSelected = null;

        private int CurrentResultIndex = -1;

        //
        // Initialize
        //

        public ToolControlFindReplace()
        {
            InitializeComponent();

            TheSearchOptions.allowedAssemblies = new[] { typeof(AdminShell).Assembly };

            // the combo box needs a special treatment in order to have it focussed ..
            ComboBoxToolsFindText.Loaded += (object sender, RoutedEventArgs e) =>
            {
                // try focus again after loading ..
                ComboBoxToolsFindText.Focus();
            };

            ComboBoxToolsFindText.GotFocus += (object sender, RoutedEventArgs e) =>
            {
                var textBox = ComboBoxToolsFindText.Template.FindName(
                    "PART_EditableTextBox", ComboBoxToolsFindText) as TextBox;
                if (textBox != null)
                    textBox.Select(0, textBox.Text.Length);
            };

        }

        //
        // Public functionality
        //

        public void ShowReplace(bool visible)
        {
            WrapPanelToolsReplace.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public void FocusFirstField()
        {
            if (ComboBoxToolsFindText == null)
                return;

            ComboBoxToolsFindText.Focus();
        }

        public void FindForward()
        {
            // simulate
            ButtonToolsFind_Click(ButtonToolsFindForward, null);
        }

        public void FindBackward()
        {
            // simulate
            ButtonToolsFind_Click(ButtonToolsFindBackward, null);
        }

        public void ClearResults()
        {
            TheSearchResults.Clear();
            CurrentResultIndex = -1;
        }

        public void UpdateToOptions()
        {
            TheSearchOptions.findText = ComboBoxToolsFindText.Text;
            TheSearchOptions.isIgnoreCase = CheckBoxToolsFindIgnoreCase.IsChecked == true;
            TheSearchOptions.isRegex = CheckBoxToolsFindRegex.IsChecked == true;
        }

        public void SetFindInfo(int index, int count, AdminShellUtil.SearchResultItem sri)
        {
            if (this.ButtonToolsFindInfo == null)
                return;

            var baseTxt = $"{index} of {count}";

            this.ButtonToolsFindInfo.Text = baseTxt;
        }

        public void DoSearch()
        {
            // access
            if (TheSearchOptions == null || TheSearchResults == null || TheAasEnv == null)
                return;

            // do not accept empty field
            if (TheSearchOptions.findText == null || TheSearchOptions.findText.Length < 1)
            {
                this.ButtonToolsFindInfo.Text = "no search!";
                return;
            }

            // execution
            try
            {
                AdminShellUtil.EnumerateSearchable(TheSearchResults, TheAasEnv, "", 0, TheSearchOptions);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "When searching for results");
            }

            // try to go to 1st result
            CurrentResultIndex = -1;
            if (TheSearchResults.foundResults != null && TheSearchResults.foundResults.Count > 0 &&
                    ResultSelected != null)
            {
                CurrentResultIndex = 0;
                var sri = TheSearchResults.foundResults[0];
                SetFindInfo(1 + CurrentResultIndex, TheSearchResults.foundResults.Count, sri);
                ResultSelected(sri);
            }
            else
            {
                this.ButtonToolsFindInfo.Text = "not found!";
            }
        }

        public void DoReplace(AdminShellUtil.SearchResultItem sri, string replaceText)
        {
            // access
            if (TheSearchOptions == null || TheSearchResults == null || TheAasEnv == null
                || replaceText == null || sri == null)
            {
                Log.Singleton.Error("Invalid result data. Cannot use for replace.");
                return;
            }

            // execution
            try
            {
                AdminShellUtil.ReplaceInSearchable(TheSearchOptions, sri, replaceText);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "When searching for results");
            }
        }

        //
        // Callbacks
        //

        private void ComboBoxToolsFindText_KeyUp(object sender, KeyEventArgs e)
        {
            if (ComboBoxToolsFindText == null || TheSearchOptions == null)
                return;

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                // kick a new search
                UpdateToOptions();
                ClearResults();
                DoSearch();
            }
        }

        private void ButtonToolsFind_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxToolsFindText == null || TheSearchResults == null ||
                    TheSearchResults.foundResults == null || ResultSelected == null)
                return;

            if (sender == ButtonToolsFindBackward || sender == ButtonToolsFindForward
                || sender == ButtonToolsReplaceStay)
            {
                // 1st check .. renew search?
                if (ComboBoxToolsFindText.Text != TheSearchOptions.findText)
                {
                    // kick a new search
                    TheSearchOptions.findText = ComboBoxToolsFindText.Text;
                    ClearResults();
                    DoSearch();

                    if (sender == ButtonToolsReplaceStay)
                        Log.Singleton.Info(StoredPrint.Color.Blue, 
                            "New search of results initiated. Select replace operation again!");

                    return;
                }
            }

            // continue search?
            if (sender == ButtonToolsFindBackward && CurrentResultIndex > 0)
            {
                CurrentResultIndex--;
                var sri = TheSearchResults.foundResults[CurrentResultIndex];
                SetFindInfo(1 + CurrentResultIndex, TheSearchResults.foundResults.Count, sri);
                ResultSelected(sri);
            }

            if (sender == ButtonToolsFindForward && CurrentResultIndex >= 0 &&
                    CurrentResultIndex < TheSearchResults.foundResults.Count - 1)
            {
                CurrentResultIndex++;
                var sri = TheSearchResults.foundResults[CurrentResultIndex];
                SetFindInfo(1 + CurrentResultIndex, TheSearchResults.foundResults.Count, sri);
                ResultSelected(sri);
            }

            if (sender == ButtonToolsReplaceStay)
            {
                if (CurrentResultIndex >= 0 &&
                    CurrentResultIndex < TheSearchResults.foundResults.Count)
                {
                    var sri = TheSearchResults.foundResults[CurrentResultIndex];
                    var rt = ComboBoxToolsReplaceText.Text;
                    DoReplace(sri, rt);
                    Log.Singleton.Info("Replaced {0} with {1} and staying.", TheSearchOptions.findText, rt);
                    ResultSelected(sri);
                }
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.ButtonToolsFindInfo.Text = "";
        }
    }
}
