/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AasxIntegrationBase;
using AasxIntegrationBaseWpf;
using AasxPackageLogic;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using AnyUi;

namespace AasxPackageExplorer
{
    public partial class ToolControlFindReplace : UserControl
    {
        //
        // Members
        //

        public AasxSearchUtil.SearchOptions TheSearchOptions = new AasxSearchUtil.SearchOptions();
        public AasxSearchUtil.SearchResults TheSearchResults = new AasxSearchUtil.SearchResults();

        public Aas.Environment TheAasEnv = null;

        public IFlyoutProvider Flyout = null;
        public delegate void ResultSelectedDelegate(AasxSearchUtil.SearchResultItem resultItem);

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

            TheSearchOptions.AllowedAssemblies = new[] { typeof(Aas.Environment).Assembly };

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

        public void FindStart(AasxMenuActionTicket ticket)
        {
            // simulate
            ButtonToolsFind_Click(ButtonToolsFindStart, null, ticket);
        }

        public void FindForward(AasxMenuActionTicket ticket)
        {
            // simulate
            ButtonToolsFind_Click(ButtonToolsFindForward, null, ticket);
        }

        public void FindBackward(AasxMenuActionTicket ticket)
        {
            // simulate
            ButtonToolsFind_Click(ButtonToolsFindBackward, null, ticket);
        }

        public void ReplaceStay(AasxMenuActionTicket ticket)
        {
            // simulate
            ButtonToolsFind_Click(ButtonToolsReplaceStay, null, ticket);
        }

        public void ReplaceForward(AasxMenuActionTicket ticket)
        {
            // simulate
            ButtonToolsFind_Click(ButtonToolsReplaceForward, null, ticket);
        }

        public void ReplaceAll(AasxMenuActionTicket ticket)
        {
            // simulate
            ButtonToolsFind_Click(ButtonToolsReplaceAll, null, ticket);
        }

        public void ClearResults()
        {
            TheSearchResults.Clear();
            CurrentResultIndex = -1;
        }

        public void UpdateToOptions()
        {
            TheSearchOptions.FindText = ComboBoxToolsFindText.Text;
            TheSearchOptions.ReplaceText = ComboBoxToolsReplaceText.Text;
        }

        public void SetFindInfo(int index, int count, AasxSearchUtil.SearchResultItem sri)
        {
            if (this.ButtonToolsFindInfo == null)
                return;

            var baseTxt = $"{index} of {count}";

            this.ButtonToolsFindInfo.Text = baseTxt;
        }

        public void DoSearch(AasxMenuActionTicket ticket)
        {
            // access
            if (TheSearchOptions == null || TheSearchResults == null || TheAasEnv == null)
                return;

            // do not accept empty field
            if (TheSearchOptions.FindText == null || TheSearchOptions.FindText.Length < 1)
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
                var manualResetEvent = new ManualResetEvent(false);
                worker.DoWork += (sender, args) =>
                {
                    SetProgressBar.Invoke(0.0, "Searching");
                    Log.Singleton.Info("Searching for »{0}«", TheSearchOptions.FindText);
                    progressCount = 0;
                    TheSearchOptions.CompileOptions();
                    AasxSearchUtil.EnumerateSearchable(TheSearchResults, TheAasEnv, "", 0, TheSearchOptions,
                        progress: (found, num) =>
                        {
                            if (ticket?.ScriptMode == true)
                                return;
                            progressCount++;
                            if ((progressCount % 1000) == 0)
                                SetProgressBar.Invoke((progressCount / 100) % 100, "Searching");
                        });

                    // For the synchronous case, indicate we're done.
                    // This needs to be in the DoWork() portion of the worker!
                    manualResetEvent.Set();
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
                            TheSearchResults.foundResults.Count, TheSearchOptions.FindText);
                    }
                    else
                    {
                        if (ticket != null)
                            ticket.Success = false;
                        Log.Singleton.Info(StoredPrint.Color.Blue, "Search text \u00bb{0}\u00ab not found!",
                            TheSearchOptions.FindText);
                    }
                };
                worker.ProgressChanged += (s3, a3) =>
                {
                    SetProgressBar.Invoke(0.01 * a3.ProgressPercentage, "Searching");
                };

                if (ticket?.ScriptMode != true)
                {
                    // normal operation
                    worker.WorkerReportsProgress = true;

                    // wait for the DoWork() to be done
                    worker.RunWorkerAsync();
                }
                else
                {
                    // in script mode, we want to have the results of the searching available
                    // for subsequent script commands. Therefore 'emulate' the background worker.
                    // see: https://stackoverflow.com/questions/12213650/calling-backgroundworker-synchronously
                    worker.RunWorkerAsync();
                    manualResetEvent.WaitOne();
                    // wait for the whole completion
                    Thread.Sleep(100);
                }
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

        public void DoReplace(AasxSearchUtil.SearchResultItem sri, string replaceText)
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
                AasxSearchUtil.ReplaceInSearchable(TheSearchOptions, sri, replaceText);
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

            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true;
                // kick a new search
                ComboBoxPushNewItemToTop(ComboBoxToolsFindText, ComboBoxToolsFindText.Text);
                UpdateToOptions();
                ClearResults();
                DoSearch(ticket: null);
            }
        }

        private void ButtonToolsFind_Click(object sender, RoutedEventArgs e)
        {
            ButtonToolsFind_Click(sender, e, null);
        }

        private void ButtonToolsFind_Click(
            object sender, RoutedEventArgs e,
            AasxMenuActionTicket ticket)
        {
            if (ComboBoxToolsFindText == null || TheSearchResults == null ||
                    TheSearchResults.foundResults == null || ResultSelected == null)
                return;

            if (sender == ButtonToolsFindStart)
            {
                // kick a new search
                UpdateToOptions();
                ticket?.ArgValue?.PopulateObjectFromArgs(TheSearchOptions);
                ComboBoxPushNewItemToTop(ComboBoxToolsFindText, TheSearchOptions.FindText);
                ClearResults();

                if (ticket?.ScriptMode == true && !TheSearchOptions.FindText.HasContent())
                {
                    ticket.Success = false;
                    Log.Singleton.Error("Find in script mode: no find text is specified. Stopping.");
                    return;
                }
                DoSearch(ticket);
            }

            if (sender == ButtonToolsFindBackward || sender == ButtonToolsFindForward
                || sender == ButtonToolsReplaceStay || sender == ButtonToolsReplaceForward)
            {
                // 1st check .. renew search?
                if (ComboBoxToolsFindText.Text != TheSearchOptions.FindText)
                {
                    // kick a new search
                    UpdateToOptions();
                    ticket?.ArgValue?.PopulateObjectFromArgs(TheSearchOptions);
                    ComboBoxPushNewItemToTop(ComboBoxToolsFindText, TheSearchOptions.FindText);
                    ClearResults();
                    DoSearch(ticket);

                    if (sender == ButtonToolsReplaceStay)
                        Log.Singleton.Info(StoredPrint.Color.Blue,
                            "New search of results initiated. Select replace operation again!");

                    return;
                }
            }

            // continue search?
            if (sender == ButtonToolsFindBackward)
            {
                if (CurrentResultIndex > 0)
                {
                    CurrentResultIndex--;
                    var sri = TheSearchResults.foundResults[CurrentResultIndex];
                    SetFindInfo(1 + CurrentResultIndex, TheSearchResults.foundResults.Count, sri);
                    ResultSelected(sri);
                }
                else
                {
                    if (ticket?.ScriptMode == true)
                    {
                        ticket.Success = false;
                        if (Options.Curr.ScriptLoglevel > 0)
                            Log.Singleton.Info("Script: FindBackward: No success. End of results reached.");
                    }
                }
            }

            if (sender == ButtonToolsFindForward)
            {
                if (CurrentResultIndex >= 0 &&
                    CurrentResultIndex < TheSearchResults.foundResults.Count - 1)
                {
                    CurrentResultIndex++;
                    var sri = TheSearchResults.foundResults[CurrentResultIndex];
                    SetFindInfo(1 + CurrentResultIndex, TheSearchResults.foundResults.Count, sri);
                    ResultSelected(sri);
                }
                else
                {
                    if (ticket?.ScriptMode == true)
                    {
                        ticket.Success = false;
                        if (Options.Curr.ScriptLoglevel > 0)
                            Log.Singleton.Info("Script: FindForward: No success. End of results reached.");
                    }
                }
            }

            // replace
            if (sender == ButtonToolsReplaceStay || sender == ButtonToolsReplaceForward
                || sender == ButtonToolsReplaceAll)
            {
                UpdateToOptions();
                ticket?.ArgValue?.PopulateObjectFromArgs(TheSearchOptions);
                ComboBoxPushNewItemToTop(ComboBoxToolsReplaceText, TheSearchOptions.ReplaceText);
            }

            if (sender == ButtonToolsReplaceStay || sender == ButtonToolsReplaceForward)
            {
                if (CurrentResultIndex >= 0 &&
                    CurrentResultIndex < TheSearchResults.foundResults.Count)
                {
                    var sri = TheSearchResults.foundResults[CurrentResultIndex];
                    var fwd = sender == ButtonToolsReplaceForward;

                    DoReplace(sri, TheSearchOptions.ReplaceText);
                    Log.Singleton.Info("In {0}, replaced »{1}« with »{2}« and {3}.",
                        sri?.ToString(),
                        TheSearchOptions.FindText, TheSearchOptions.ReplaceText,
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
                    || ticket?.ScriptMode == true
                    || AnyUiMessageBoxResult.Yes == Flyout.MessageBoxFlyoutShow(
                        "Perform replace on all found occurences? " +
                        "This operation cannot be reverted!", "Replace ALL",
                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                {
                    // start
                    var rt = ComboBoxToolsReplaceText.Text;
                    int replacements = 0;
                    AasxSearchUtil.SearchResultItem foundSri = null;

                    // execute
                    try
                    {
                        foreach (var sri in TheSearchResults.foundResults)
                        {
                            AasxSearchUtil.ReplaceInSearchable(TheSearchOptions, sri, rt);
                            Log.Singleton.Info("In {0}, replaced (all) »{1}« with »{2}«.",
                                sri?.ToString(),
                                TheSearchOptions.FindText, rt);

                            foundSri = sri;
                            replacements++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "When replacing all occurences of: " + TheSearchOptions.FindText);
                    }

                    // finally
                    Log.Singleton.Info("Replaced {0} occurences of »{1}« with »{2}«.",
                        "" + replacements, TheSearchOptions.FindText, rt);
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
                    "IGNR", "", "Ignore case", checkState: op.IsIgnoreCase));
                cm.Add(new DynamicContextItem(
                    "WHOL", "", "Whole word only", checkState: op.IsWholeWord));
                cm.Add(new DynamicContextItem(
                    "REGX", "", "Use regex", checkState: op.IsRegex));

                cm.Add(DynamicContextItem.CreateSeparator());

                cm.Add(new DynamicContextItem(
                    "COLL", "", "Search Collection/ List", checkState: op.SearchCollection));
                cm.Add(new DynamicContextItem(
                    "PROP", "", "Search Aas.Property", checkState: op.SearchProperty));
                cm.Add(new DynamicContextItem(
                    "MLPR", "", "Search Multilang.Prop.", checkState: op.SearchMultiLang));
                cm.Add(new DynamicContextItem(
                    "OTHER", "", "Search all other", checkState: op.SearchOther));

                cm.Add(DynamicContextItem.CreateSeparator());
                cm.Add(DynamicContextItem.CreateTextBox("LANG", "", "Language:", 70, op.SearchLanguage, 70));

                cm.Start(sender as Button, (tag, obj) =>
                {
                    switch (tag)
                    {
                        case "IGNR": TheSearchOptions.IsIgnoreCase ^= true; break;
                        case "WHOL": TheSearchOptions.IsWholeWord ^= true; break;
                        case "REGX": TheSearchOptions.IsRegex ^= true; break;
                        case "COLL": TheSearchOptions.SearchCollection ^= true; break;
                        case "PROP": TheSearchOptions.SearchProperty ^= true; break;
                        case "MLPR": TheSearchOptions.SearchMultiLang ^= true; break;
                        case "OTHER": TheSearchOptions.SearchOther ^= true; break;
                        case "LANG":
                            if (obj is string st)
                                TheSearchOptions.SearchLanguage = st;
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
