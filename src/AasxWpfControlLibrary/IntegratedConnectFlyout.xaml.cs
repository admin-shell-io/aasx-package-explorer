/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
using System.Windows.Threading;
using AasxIntegrationBase;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    public partial class IntegratedConnectFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        private PackageCentral _packageCentral;
        private string _caption;
        private double? _maxWidth;
        private string _location;
        private string _initialDirectory;
        private LogInstance _logger;

        private List<AnyUiDialogueListItem> _selectFromListItems;
        private Action<AnyUiDialogueListItem> _selectFromListAction;

        private Action<PackageContainerCredentials> _askedUserCredentials;

        public bool Result;
        public PackageContainerBase ResultContainer;

        //
        // Preset
        //

        // Resharper disable once ClassNeverInstantiated.Global
        // Resharper disable once UnassignedField.Global
        protected class PresetItem
        {
            public string Name = "";
            public string Location = "";
            public string Username = "";
            public string Password;
            public bool AutoClose = true;
            public bool StayConnected = false;
        }

        protected class PresetList : List<PresetItem>
        {
        }

        protected PresetList _presets = new PresetList();

        //
        // Constructor
        //

        public IntegratedConnectFlyout(
            PackageCentral packageCentral,
            string caption = null,
            double? maxWidth = null,
            string initialLocation = null,
            string initialDirectory = null,
            LogInstance logger = null)
        {
            InitializeComponent();

            _packageCentral = packageCentral;
            _caption = caption;
            _maxWidth = maxWidth;
            _location = initialLocation;
            _initialDirectory = initialDirectory;
            _logger = logger;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // texts
            if (_caption != null)
                this.LabelCaption.Content = _caption;

            // dialogue width
            if (_maxWidth.HasValue && _maxWidth.Value > 200)
                OuterGrid.MaxWidth = _maxWidth.Value;

            // start page
            if (_location != null)
                TextBoxStartLocation.Text = _location;
            if (_presets != null)
            {
                ComboBoxPresets.Items.Clear();
                foreach (var pi in _presets)
                    ComboBoxPresets.Items.Add("" + pi?.Name);
            }

            // focus
            this.TextBoxStartLocation.Focus();
            this.TextBoxStartLocation.Select(0, 999);
            FocusManager.SetFocusedElement(this, this.TextBoxStartLocation);

            // timer
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Start();
            timer.Tick += (object sender2, EventArgs e2) =>
            {
                var msg = _logger?.PopLastShortTermPrint();
                while (msg != null)
                {
                    Log.Singleton.Append(msg);
                    this.LogMessage(msg);
                    msg = _logger?.PopLastShortTermPrint();
                }
            };
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        public void LambdaActionAvailable(AnyUiLambdaActionBase la)
        {
        }

        public void LoadPresets(Newtonsoft.Json.Linq.JToken jtoken)
        {
            // access
            if (jtoken == null)
                return;

            try
            {
                var pl = jtoken.ToObject<PresetList>();
                if (pl != null)
                    _presets = pl;
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "When loading integrated connect presets from options");
            }
        }

        private string _lastTextMessage = "";

        public void LogMessage(StoredPrint sp)
        {
            // access
            if (sp == null || sp.msg == null)
                return;

            // remember
            _lastTextMessage = "" + sp.msg;

            // add to rich text box
            AasxWpfBaseUtils.StoredPrintToRichTextBox(
                this.TextBoxMessages, sp, AasxWpfBaseUtils.BrightPrintColors);

            // move scroll
            if (true /* this.CheckBoxAutoScroll.IsChecked == true */)
                this.TextBoxMessages.ScrollToEnd();
        }

        //
        // Mechanics
        //

        // see: https://stackoverflow.com/questions/52706251/c-sharp-async-await-a-button-click

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            // execute in any case (will lead to properly close the the Flyout)
            this.Result = false;
            this.ResultContainer = null;
            ControlClosed?.Invoke();

            // special case
            if (_dispatcherFrame != null)
            {
                _dispatcherFrame.Continue = false;
                _dialogResult = AnyUiMessageBoxResult.Cancel;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonMsgSmaller && TextBoxMessages.FontSize >= 6.0)
                TextBoxMessages.FontSize -= 2.0;
            if (sender == ButtonMsgLarger && TextBoxMessages.FontSize < 99.0)
                TextBoxMessages.FontSize += 2.0;

            if (sender == ButtonStartSelect)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                if (_initialDirectory != null)
                {
                    dlg.InitialDirectory = _initialDirectory;
                    _initialDirectory = null;
                }
                dlg.Filter =
                    "AASX package files (*.aasx)|*.aasx|AAS XML file (*.xml)|*.xml|" +
                    "AAS JSON file (*.json)|*.json|All files (*.*)|*.*";

                var res = dlg.ShowDialog();
                if (res == true)
                {
                    TextBoxStartLocation.Text = dlg.FileName;
                }
            }

            if (sender == ButtonStartProceed)
            {
                ProceedOnPageStart();
            }

            if (sender == ButtonPageSelectFromListProceed)
            {
                ProceedOnPageSelectFromList();
            }

            if (sender == ButtonPageCredentialsProceed)
            {
                ProceedOnPageCredentials();
            }

            if (sender == ButtonPageSummaryDone)
            {
                DoneOnPageSummary();
            }

            AnyUiMessageBoxResult tmpRes = AnyUiMessageBoxResult.None;
            if (sender == ButtonMessageBoxOK)
                tmpRes = AnyUiMessageBoxResult.OK;
            if (sender == ButtonMessageBoxCancel)
                tmpRes = AnyUiMessageBoxResult.Cancel;
            if (sender == ButtonMessageBoxYes)
                tmpRes = AnyUiMessageBoxResult.Yes;
            if (sender == ButtonMessageBoxNo)
                tmpRes = AnyUiMessageBoxResult.No;
            if (tmpRes != AnyUiMessageBoxResult.None)
            {
                if (_dispatcherFrame != null)
                    _dispatcherFrame.Continue = false;
                _dialogResult = tmpRes;
            }

            if (sender == ButtonMsgCopyLast)
            {
                System.Windows.Clipboard.SetText("" + _lastTextMessage);
            }

            if (sender == ButtonMsgCopyAll)
            {
                var tr = new TextRange(TextBoxMessages.Document.ContentStart, TextBoxMessages.Document.ContentEnd);
                System.Windows.Clipboard.SetText("" + tr.Text);
            }
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Escape)
            {
                if (_dispatcherFrame != null)
                {
                    // message box
                    _dialogResult = _presetNoCancel;
                    _dispatcherFrame.Continue = false;
                }
                else
                {
                    // normal mode
                    this.Result = false;
                    ControlClosed?.Invoke();
                }
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Return)
            {
                if (TabControlMain.SelectedItem == TabItemStart)
                    ProceedOnPageStart();

                if (TabControlMain.SelectedItem == TabItemSelectFromList)
                    ProceedOnPageSelectFromList();

                if (TabControlMain.SelectedItem == TabItemCredentials)
                    ProceedOnPageCredentials();

                if (TabControlMain.SelectedItem == TabItemSummary)
                    DoneOnPageSummary();

                if (TabControlMain.SelectedItem == TabItemMessageBox && _dispatcherFrame != null)
                {
                    // message box
                    _dialogResult = _presetOkYes;
                    _dispatcherFrame.Continue = false;
                }
            }

            if (TabControlMain.SelectedItem == TabItemMessageBox && _dispatcherFrame != null
                && _presetOkYes == AnyUiMessageBoxResult.Yes)
            {
                // has a Yes/No message box
                if (e.Key == Key.Y)
                {
                    _dialogResult = _presetOkYes;
                    _dispatcherFrame.Continue = false;
                }

                if (e.Key == Key.N)
                {
                    _dialogResult = AnyUiMessageBoxResult.No;
                    _dispatcherFrame.Continue = false;
                }
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key >= Key.D1 && e.Key <= Key.D9)
            {
                var i = e.Key - Key.D1;
                if (_presets != null && i >= 0 && i < _presets.Count)
                    ApplyPreset(_presets[i]);
            }
        }


        private void TextBoxMessages_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                return;
            }

            e.Handled = true;
            if (e.Delta > 0)
            {
                ++TextBoxMessages.FontSize;
            }
            else
            {
                if (TextBoxMessages.FontSize >= 6.0)
                    --TextBoxMessages.FontSize;
            }
        }

        private void SetProgressBar(double? percent, string message = null)
        {
            // thread safe
            if (percent.HasValue)
                TheProgressBar.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => TheProgressBar.Value = percent.Value));

            if (message != null)
                LabelProgressText.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => LabelProgressText.Content = message));
        }

        //
        // Start page
        //

        private async void ProceedOnPageStart()
        {
            // make runtime options to link to this dialogue
            var ro = new PackCntRuntimeOptions()
            {
                Log = _logger,
                ProgressChanged = (state, tfs, tbd) =>
                {
                    if (state == PackCntRuntimeOptions.Progress.Ongoing)
                    {
                        // determine
                        if (tfs == null)
                            tfs = 5 * 1024 * 1024;
                        var frac = Math.Min(100.0, 100.0 * tbd / tfs.Value);
                        var bshr = AdminShellUtil.ByteSizeHumanReadable(tbd);

                        SetProgressBar(frac, $"{bshr} transferred");
                    }

                    if (state == PackCntRuntimeOptions.Progress.Final)
                        SetProgressBar(0.0, "");
                },
                AskForSelectFromList = (caption, items, propRes) =>
                {
                    TabItemSelectFromList.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() =>
                        {
                            StartPageSelectFromList(caption, items, (li) =>
                            {
                                // never again
                                _selectFromListAction = null;
                                // call
                                propRes?.TrySetResult(li);
                            });
                        }));
                },
                AskForCredentials = (caption, propRes) =>
                {
                    TabItemCredentials.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() =>
                        {
                            StartPageAskCredentials(caption, (pcc) =>
                            {
                                // never again
                                _askedUserCredentials = null;
                                // call
                                propRes?.TrySetResult(pcc);
                            });
                        }));
                }
            };

            // Log
            var location = TextBoxStartLocation.Text;
            _logger?.Info($"Connect (integrated): Trying to connect to {location} ..");

            // try do the magic
            try
            {
                // quickly parse out container options
                var copts = PackageContainerOptionsBase.CreateDefault(Options.Curr, loadResident: true);
                copts.StayConnected = true == CheckBoxStayConnected.IsChecked;
                if (Int32.TryParse("" + TextBoxUpdatePeriod.Text, out int i))
                    copts.UpdatePeriod = Math.Max(OptionsInformation.MinimumUpdatePeriod, i);

                // create container
                var x = await PackageContainerFactory.GuessAndCreateForAsync(
                    _packageCentral,
                    location,
                    location,
                    overrideLoadResident: true,
                    containerOptions: copts,
                    runtimeOptions: ro);

                // returning "x" is the only way to end the dialogue successfuly
                if (x != null)
                {
                    // prepare result
                    _logger?.Info($"Connect (integrated): guessing and creating container package " +
                        $"succeeded with {x.ToString()} !");
                    this.Result = true;
                    this.ResultContainer = x;

                    // close now?
                    if (true == CheckBoxStayAutoClose.IsChecked)
                    {
                        // trigger close
                        ControlClosed?.Invoke();
                    }
                    else
                    {
                        // proceed to summary page
                        StartPageSummary();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "when guessing for packager container!");
            }
        }

        private void ComboBoxPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var i = ComboBoxPresets.SelectedIndex;
            if (_presets != null && i >= 0 && i < _presets.Count)
                ApplyPreset(_presets[i]);
        }

        private void ApplyPreset(PresetItem pi)
        {
            // access
            if (pi == null)
                return;

            // apply
            if (pi.Location != null)
                TextBoxStartLocation.Text = pi.Location;
            if (pi.Username != null)
                TextBoxUsername.Text = pi.Username;
            if (pi.Password != null)
                TextBoxPassword.Text = pi.Password;
            CheckBoxStayAutoClose.IsChecked = pi.AutoClose;
            CheckBoxStayConnected.IsChecked = pi.StayConnected;
        }

        //
        // Select from List
        //

        private void StartPageSelectFromList(string caption,
            List<AnyUiDialogueListItem> selectFromListItems,
            Action<AnyUiDialogueListItem> selectFromListAction)
        {
            // show tab page
            TabControlMain.SelectedItem = TabItemSelectFromList;

            // set
            LabelSelectFromListCaption.Content = "" + caption;
            ListBoxSelectFromList.Items.Clear();
            if (selectFromListItems != null)
                foreach (var loi in selectFromListItems)
                    ListBoxSelectFromList.Items.Add("" + loi.Text);

            // remember
            _selectFromListItems = selectFromListItems;
            _selectFromListAction = selectFromListAction;
        }

        private AnyUiDialogueListItem GetSelectedItemFromList()
        {
            var i = ListBoxSelectFromList.SelectedIndex;
            if (_selectFromListItems != null && i >= 0 && i < _selectFromListItems.Count)
                return _selectFromListItems[i];
            return null;
        }

        private void ListBoxSelectFromList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ProceedOnPageSelectFromList();
        }

        private void ProceedOnPageSelectFromList()
        {
            // read back selected item
            var li = GetSelectedItemFromList();

            // call back
            if (li != null)
                _selectFromListAction?.Invoke(li);
        }

        //
        // User credentials
        //

        private void StartPageAskCredentials(string caption,
            Action<PackageContainerCredentials> askedUserCredentials)
        {
            // show tab page
            TabControlMain.SelectedItem = TabItemCredentials;

            // set
            LabelCredentialsCaption.Content = "" + caption;

            // have a callback
            _askedUserCredentials = askedUserCredentials;
        }

        private void ProceedOnPageCredentials()
        {
            // read back
            var pcc = new PackageContainerCredentials();
            pcc.Username = TextBoxUsername.Text;
            pcc.Password = TextBoxPassword.Text;

            // call back
            _askedUserCredentials?.Invoke(pcc);
        }

        //
        // Summary
        //

        private void StartPageSummary(string message = null)
        {
            // show tab page
            TabControlMain.SelectedItem = TabItemSummary;

            // set
            if (message != null)
                TextBlockSummaryMessage.Text = "" + message;
        }

        private void DoneOnPageSummary()
        {
            if (Result && ResultContainer != null)
                ControlClosed?.Invoke();
        }

        //
        // MessageBox
        //

        private DispatcherFrame _dispatcherFrame = null;
        private AnyUiMessageBoxResult _dialogResult = AnyUiMessageBoxResult.None;

        private AnyUiMessageBoxResult _presetOkYes = AnyUiMessageBoxResult.None;
        private AnyUiMessageBoxResult _presetNoCancel = AnyUiMessageBoxResult.None;

        /// <summary>
        /// Assumes, that the flyout is open and BLOCKS the message loop until a result button
        /// is being pressed!
        /// </summary>
        public AnyUiMessageBoxResult MessageBoxShow(
            string content, string text, string caption,
            AnyUiMessageBoxButton buttons)
        {
            // show tab page
            TabControlMain.SelectedItem = TabItemMessageBox;

            // set text portions
            TextBlockMessageBoxCaption.Text = "" + caption;
            TextBlockMessageBoxText.Text = "" + text;
            if (caption.HasContent())
                LogMessage(new StoredPrint(StoredPrint.Color.Yellow, "" + caption));
            if (text.HasContent())
                LogMessage(new StoredPrint("" + text));
            if (content.HasContent())
                LogMessage(new StoredPrint("" + content));

            // set respective buttons
            _presetOkYes = AnyUiMessageBoxResult.None;
            _presetNoCancel = AnyUiMessageBoxResult.None;

            ButtonMessageBoxOK.Visibility = Visibility.Collapsed;
            ButtonMessageBoxCancel.Visibility = Visibility.Collapsed;
            ButtonMessageBoxYes.Visibility = Visibility.Collapsed;
            ButtonMessageBoxNo.Visibility = Visibility.Collapsed;

            if (buttons == AnyUiMessageBoxButton.OK)
            {
                ButtonMessageBoxOK.Visibility = Visibility.Visible;
                _presetOkYes = AnyUiMessageBoxResult.OK;
            }

            if (buttons == AnyUiMessageBoxButton.OKCancel)
            {
                ButtonMessageBoxOK.Visibility = Visibility.Visible;
                ButtonMessageBoxCancel.Visibility = Visibility.Visible;
                _presetOkYes = AnyUiMessageBoxResult.OK;
                _presetNoCancel = AnyUiMessageBoxResult.Cancel;
            }

            if (buttons == AnyUiMessageBoxButton.YesNo)
            {
                ButtonMessageBoxYes.Visibility = Visibility.Visible;
                ButtonMessageBoxNo.Visibility = Visibility.Visible;
                _presetOkYes = AnyUiMessageBoxResult.Yes;
                _presetNoCancel = AnyUiMessageBoxResult.No;
            }

            if (buttons == AnyUiMessageBoxButton.YesNoCancel)
            {
                ButtonMessageBoxCancel.Visibility = Visibility.Visible;
                ButtonMessageBoxYes.Visibility = Visibility.Visible;
                ButtonMessageBoxNo.Visibility = Visibility.Visible;
                _presetOkYes = AnyUiMessageBoxResult.Yes;
                _presetNoCancel = AnyUiMessageBoxResult.Cancel;
            }

            // modified from StartFlyoverModal()
            // This will "block" execution of the current dispatcher frame
            // and run our frame until the dialog is closed.
            _dialogResult = AnyUiMessageBoxResult.None;
            _dispatcherFrame = new DispatcherFrame();
            Dispatcher.PushFrame(_dispatcherFrame);

            return _dialogResult;
        }
    }
}