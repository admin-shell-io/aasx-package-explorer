/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPackageLogic;
using AdminShellNS;
using AnyUi;

namespace AasxPackageExplorer
{
    public partial class LogMessageFlyout : UserControl, IFlyoutAgent
    {
        // constants


        // Members

        public event IFlyoutControlAction ControlClosed;

        public AnyUiDialogueDataLogMessage DiaData = new AnyUiDialogueDataLogMessage();

        public int ControlCloseWarnTime = -1;
        private int _timeToCloseControl = -1;
        public event IFlyoutControlAction ControlWillBeClosed;

        public bool Result = false;

        private List<Regex> _patternError = new List<Regex>();
        private int _counterError = 0, _counterMessage = 0;

        private System.Windows.Threading.DispatcherTimer _timer = null;

        private string _statusCollected = "";

        public LogMessageFlyout(string caption, string initialMessage, Func<StoredPrint> checkForStoredPrint = null)
        {
            InitializeComponent();

            // texts
            this.TextBoxCaption.Text = caption;
            this.TextBoxMessages.AppendText(initialMessage + Environment.NewLine);
            this.CheckBoxAutoScroll.IsChecked = true;

            // timer
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _timer.Start();
            _timer.Tick += (object sender, EventArgs e) =>
            {
                if (checkForStoredPrint != null)
                {
                    var msg = checkForStoredPrint();
                    while (msg != null)
                    {
                        // log
                        this.LogMessage(msg);

                        // again
                        msg = checkForStoredPrint();
                    }
                }

                if (DiaData?.CheckForLogAndEnd != null)
                {
                    var res = DiaData.CheckForLogAndEnd();

                    if (res.Item1 is string[] lmsgs && lmsgs.Length > 0)
                        foreach (var lmsg in lmsgs)
                            this.LogMessage(lmsg);

                    if (res.Item1 is StoredPrint[] sps)
                        foreach (var sp in sps)
                            this.LogMessage(sp);

                    if (res.Item2 && _timeToCloseControl <= 0)
                    {
                        // trigger the time to close (below), in order
                        // to have some visual effect
                        _timeToCloseControl = 1000;
                    }
                }

                if (this._timeToCloseControl >= 0)
                {
                    this._timeToCloseControl -= 100;
                    if (this._timeToCloseControl <= 0)
                    {
                        if (this._timer != null)
                            this._timer.Stop();
                        ControlClosed?.Invoke();
                    }
                }
            };
        }

        public void LogMessage(string msg)
        {
            LogMessage(new StoredPrint(msg));
        }

        public void LogMessage(StoredPrint sp)
        {
            // access
            if (sp == null || sp.msg == null)
                return;

            // Log?
            if (sp.MessageType == StoredPrint.MessageTypeEnum.Error
                || sp.MessageType == StoredPrint.MessageTypeEnum.Log)
            {
                // count
                this._counterMessage++;

                // check for error
                var isError = false;
                foreach (var pattern in _patternError)
                    if (pattern.IsMatch(sp.msg))
                        isError = true;

                var sumError = false;
                if (isError || sp.isError || sp.MessageType == StoredPrint.MessageTypeEnum.Error)
                {
                    _counterError++;
                    sumError = true;
                }

                // add to rich text box
                AasxWpfBaseUtils.StoredPrintToRichTextBox(
                    this.TextBoxMessages, sp, AasxWpfBaseUtils.BrightPrintColors, sumError, link_Click);

                // move scroll
                if (this.CheckBoxAutoScroll.IsChecked == true)
                    this.TextBoxMessages.ScrollToEnd();
            }

            // update status (remembered)
            if (sp.MessageType == StoredPrint.MessageTypeEnum.Status)
            {
                _statusCollected = "";
                if (sp.StatusItems != null)
                    foreach (var si in sp.StatusItems)
                    {
                        if (_statusCollected.HasContent())
                            _statusCollected += "; ";
                        _statusCollected += $"{"" + si.Name} = {"" + si.Value}";
                    }
            }

            // display status
            var status = $"Messages: {this._counterMessage}";
            if (this._counterError > 0)
                status += $" and Errors: {this._counterError}";
            this.TextBoxSummary.Text = status;
            if (_statusCollected.HasContent())
                this.TextBoxSummary.Text += "; " + _statusCollected;
        }

        protected void link_Click(object sender, RoutedEventArgs e)
        {
            // access
            var link = sender as Hyperlink;
            if (link == null || link.NavigateUri == null)
                return;

            // get url
            var uri = link.NavigateUri.ToString();
            Log.Singleton.Info($"Displaying {uri} remotely in external viewer ..");
            System.Diagnostics.Process.Start(uri);
        }

        /// <summary>
        /// Registers an error pattern, based on a regex, which is applied to each incoming log message
        /// </summary>
        public void AddPatternError(Regex pattern)
        {
            if (pattern == null)
                return;
            this._patternError.Add(pattern);
        }

        public void EnableLargeScreen()
        {
            this.MaxWidth = double.PositiveInfinity;
            this.MaxHeight = double.PositiveInfinity;

            this.OuterGrid.MaxWidth = double.PositiveInfinity;
            this.OuterGrid.MaxHeight = double.PositiveInfinity;

            if (this.OuterGrid.RowDefinitions.Count > 2)
            {
                this.OuterGrid.RowDefinitions[0].Height = new GridLength(30);
                this.OuterGrid.RowDefinitions[this.OuterGrid.RowDefinitions.Count - 1].Height = new GridLength(10);
            }

            if (this.OuterGrid.ColumnDefinitions.Count > 3)
            {
                this.OuterGrid.ColumnDefinitions[0].Width = new GridLength(30);
                this.OuterGrid.ColumnDefinitions[this.OuterGrid.ColumnDefinitions.Count - 2].Width = new GridLength(30);
                this.OuterGrid.ColumnDefinitions[this.OuterGrid.ColumnDefinitions.Count - 1].Width = new GridLength(30);
            }
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
        }

        public void LambdaActionAvailable(AnyUiLambdaActionBase la)
        {
        }

        public void CloseControlExplicit()
        {
            if (this._timer != null)
                this._timer.Stop();
            this.ControlClosed?.Invoke();
        }

        public event IFlyoutControlAction ControlMinimize;

        public FlyoutAgentBase Agent = null;

        public FlyoutAgentBase GetAgent() { return Agent; }

        //
        // Mechanics
        //

        private void ButtonCloseMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonClose)
            {
                if (ControlCloseWarnTime < 0)
                {
                    // simply close
                    this.Result = false;
                    if (this._timer != null)
                        this._timer.Stop();
                    ControlClosed?.Invoke();
                }
                else
                {
                    if (ControlWillBeClosed != null)
                        ControlWillBeClosed();
                    _timeToCloseControl = ControlCloseWarnTime;
                }
            }

            if (sender == ButtonMinimize)
            {
                // minimize will immediately stop log popping!
                if (this._timer != null)
                    this._timer.Stop();
                ControlMinimize?.Invoke();
            }
        }

        private void TextBoxMessages_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            this.CheckBoxAutoScroll.IsChecked = false;
        }

        private void TextBoxMessages_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.CheckBoxAutoScroll.IsChecked = false;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

    }
}
