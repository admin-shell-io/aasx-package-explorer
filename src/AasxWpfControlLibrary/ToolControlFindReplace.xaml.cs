/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AasxIntegrationBase;
using AasxIntegrationBaseWpf;
using AasxPackageLogic;
using AdminShellNS;
using AnyUi;

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

        public IFlyoutProvider Flyout = null;

        public delegate void ResultSelectedDelegate(AdminShellUtil.SearchResultItem resultItem);
        public event ResultSelectedDelegate ResultSelected = null;

        public delegate void SetProgressBarDelegate(double? percent, string message = null);
        public event SetProgressBarDelegate SetProgressBar = null;

        private BackgroundWorker worker = null;

        private int CurrentResultIndex = -1;

        private int progressCount = 0;

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
            GridToolsReplace.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
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

            // still working
            if (worker != null)
            {
                Log.Singleton.Info("Search is still working. Aborting!");
                return;
            }

            // execution
            try
            {
#if __simple_static
                AdminShellUtil.EnumerateSearchable(TheSearchResults, TheAasEnv, "", 0, TheSearchOptions);
#else
                worker = new BackgroundWorker();
                worker.DoWork += (sender, args) =>
                {
                    SetProgressBar.Invoke(0.0, "Searching");
                    Log.Singleton.Info("Searching for »{0}«", TheSearchOptions.findText);
                    progressCount = 0;
                    TheSearchOptions.CompileOptions();
                    AdminShellUtil.EnumerateSearchable(TheSearchResults, TheAasEnv, "", 0, TheSearchOptions,
                        progress: (found, num) =>
                        {
                            progressCount++;
                            if ((progressCount % 100) == 0)
                                SetProgressBar.Invoke((progressCount / 100) % 100, "Searching");
                        });
                };
                worker.RunWorkerCompleted += (s2, a2) =>
                {
                    // no worker anymore
                    worker = null;

                    // progress done
                    SetProgressBar.Invoke(0.0, "");

                    // try to go to 1st result
                    CurrentResultIndex = -1;
                    if (TheSearchResults.foundResults != null && TheSearchResults.foundResults.Count > 0 &&
                            ResultSelected != null)
                    {
                        CurrentResultIndex = 0;
                        var sri = TheSearchResults.foundResults[0];
                        SetFindInfo(1 + CurrentResultIndex, TheSearchResults.foundResults.Count, sri);
                        ResultSelected(sri);
                        Log.Singleton.Info(StoredPrint.Color.Blue,
                            "First result of {0} for \u00bb{1}\u00ab displayed.",
                            TheSearchResults.foundResults.Count, TheSearchOptions.findText);
                    }
                    else
                    {
                        Log.Singleton.Info(StoredPrint.Color.Blue, "Search text \u00bb{0}\u00ab not found!",
                            TheSearchOptions.findText);
                    }
                };
                worker.ProgressChanged += (s3, a3) =>
                {
                    SetProgressBar.Invoke(0.01 * a3.ProgressPercentage, "Searching");
                };

                worker.WorkerReportsProgress = true;
                worker.RunWorkerAsync();
#endif
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "When searching for results");
            }

#if __simple_static
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
#endif
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
                Log.Singleton.Error(ex, "When replacing.");
            }
        }

        //
        // Utils
        //

        private void ComboBoxPushNewItemToTop(ComboBox cb, string item)
        {
            // access
            if (cb == null || item == null || item == "")
                return;

            // potentially delete existing
            if (cb.Items.Contains(item))
                cb.Items.Remove(item);

            cb.Items.Insert(0, item);
            cb.Text = item;
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
                ComboBoxPushNewItemToTop(ComboBoxToolsFindText, ComboBoxToolsFindText.Text);
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

            if (sender == ButtonToolsFindStart)
            {
                // kick a new search
                ComboBoxPushNewItemToTop(ComboBoxToolsFindText, ComboBoxToolsFindText.Text);
                UpdateToOptions();
                ClearResults();
                DoSearch();
            }

            if (sender == ButtonToolsFindBackward || sender == ButtonToolsFindForward
                || sender == ButtonToolsReplaceStay || sender == ButtonToolsReplaceForward)
            {
                // 1st check .. renew search?
                if (ComboBoxToolsFindText.Text != TheSearchOptions.findText)
                {
                    // kick a new search
                    ComboBoxPushNewItemToTop(ComboBoxToolsFindText, ComboBoxToolsFindText.Text);
                    UpdateToOptions();
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

            // replace
            if (sender == ButtonToolsReplaceStay || sender == ButtonToolsReplaceForward
                || sender == ButtonToolsReplaceAll)
            {
                ComboBoxPushNewItemToTop(ComboBoxToolsReplaceText, ComboBoxToolsReplaceText.Text);
            }

            if (sender == ButtonToolsReplaceStay || sender == ButtonToolsReplaceForward)
            {
                if (CurrentResultIndex >= 0 &&
                    CurrentResultIndex < TheSearchResults.foundResults.Count)
                {
                    var sri = TheSearchResults.foundResults[CurrentResultIndex];
                    var rt = ComboBoxToolsReplaceText.Text;
                    var fwd = sender == ButtonToolsReplaceForward;
                    DoReplace(sri, rt);
                    Log.Singleton.Info("In {0}, replaced »{1}« with »{2}« and {3}.",
                        sri?.ToString(),
                        TheSearchOptions.findText, rt,
                        (fwd) ? "forwarding" : "staying");

                    if (fwd)
                    {
                        // can forward
                        if (CurrentResultIndex < TheSearchResults.foundResults.Count - 1)
                        {
                            CurrentResultIndex++;
                            var sri2 = TheSearchResults.foundResults[CurrentResultIndex];
                            SetFindInfo(1 + CurrentResultIndex, TheSearchResults.foundResults.Count, sri2);
                            ResultSelected(sri2);
                        }
                        else
                        {
                            Log.Singleton.Info(StoredPrint.Color.Blue, "End of search results reached.");
                        }
                    }
                    else
                    {
                        // stay
                        ResultSelected(sri);
                    }
                }
            }

            if (sender == ButtonToolsReplaceAll)
            {
                if (Flyout == null
                    || AnyUiMessageBoxResult.Yes == Flyout.MessageBoxFlyoutShow(
                        "Perform replace on all found occurences? " +
                        "This operation cannot be reverted!", "Replace ALL",
                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                {
                    // start
                    var rt = ComboBoxToolsReplaceText.Text;
                    int replacements = 0;
                    AdminShellUtil.SearchResultItem foundSri = null;

                    // execute
                    try
                    {
                        foreach (var sri in TheSearchResults.foundResults)
                        {
                            AdminShellUtil.ReplaceInSearchable(TheSearchOptions, sri, rt);
                            Log.Singleton.Info("In {0}, replaced (all) »{1}« with »{2}«.",
                                sri?.ToString(),
                                TheSearchOptions.findText, rt);

                            foundSri = sri;
                            replacements++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "When replacing all occurences of: " + TheSearchOptions.findText);
                    }

                    // finally
                    Log.Singleton.Info("Replaced {0} occurences of »{1}« with »{2}«.",
                        "" + replacements, TheSearchOptions.findText, rt);
                    if (foundSri != null)
                        ResultSelected(foundSri);
                }
            }

            // options
            if (sender == ButtonToolsFindOptions)
            {
                var cm = DynamicContextMenu.CreateNew();
                var op = TheSearchOptions;

                cm.Add(new DynamicContextItem(
                    "IGNR", "", "Ignore case", checkState: op.isIgnoreCase));
                cm.Add(new DynamicContextItem(
                    "WHOL", "", "Whole word only", checkState: op.isWholeWord));
                cm.Add(new DynamicContextItem(
                    "REGX", "", "Use regex", checkState: op.isRegex));

                cm.Add(DynamicContextItem.CreateSeparator());

                cm.Add(new DynamicContextItem(
                    "COLL", "", "Search Collection/ List", checkState: op.searchCollection));
                cm.Add(new DynamicContextItem(
                    "PROP", "", "Search Property", checkState: op.searchProperty));
                cm.Add(new DynamicContextItem(
                    "MLPR", "", "Search Multilang.Prop.", checkState: op.searchMultiLang));
                cm.Add(new DynamicContextItem(
                    "OTHER", "", "Search all other", checkState: op.searchOther));

                cm.Add(DynamicContextItem.CreateSeparator());
                cm.Add(DynamicContextItem.CreateTextBox("LANG", "", "Language:", 70, op.searchLanguage, 70));

                cm.Start(sender as Button, (tag, obj) =>
                {
                    switch (tag)
                    {
                        case "IGNR": TheSearchOptions.isIgnoreCase ^= true; break;
                        case "WHOL": TheSearchOptions.isWholeWord ^= true; break;
                        case "REGX": TheSearchOptions.isRegex ^= true; break;
                        case "COLL": TheSearchOptions.searchCollection ^= true; break;
                        case "PROP": TheSearchOptions.searchProperty ^= true; break;
                        case "MLPR": TheSearchOptions.searchMultiLang ^= true; break;
                        case "OTHER": TheSearchOptions.searchOther ^= true; break;
                        case "LANG":
                            if (obj is string st)
                                TheSearchOptions.searchLanguage = st;
                            break;
                    }
                });
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.ButtonToolsFindInfo.Text = "";
        }
    }
}
