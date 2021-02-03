/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AasxIntegrationBase;

namespace AasxPackageExplorer
{
    public partial class LogMessageFlyout : UserControl, IFlyoutControl
    {
        // constants


        // Members

        public event IFlyoutControlClosed ControlClosed;

        public int ControlCloseWarnTime = -1;
        private int timeToCloseControl = -1;
        public event IFlyoutControlClosed ControlWillBeClosed;

        public bool Result = false;

        private List<Regex> patternError = new List<Regex>();
        private int counterError = 0, counterMessage = 0;

        private System.Windows.Threading.DispatcherTimer timer = null;

        public LogMessageFlyout(string caption, string initialMessage, Func<StoredPrint> checkForStoredPrint = null)
        {
            InitializeComponent();

            // texts
            this.TextBoxCaption.Text = caption;
            this.TextBoxMessages.AppendText(initialMessage + Environment.NewLine);
            this.CheckBoxAutoScroll.IsChecked = true;

            // timer
            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Start();
            timer.Tick += (object sender, EventArgs e) =>
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

                if (this.timeToCloseControl >= 0)
                {
                    this.timeToCloseControl -= 100;
                    if (this.timeToCloseControl < 0 && this.ControlClosed != null)
                    {
                        if (this.timer != null)
                            this.timer.Stop();
                        this.ControlClosed();
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

            // count
            this.counterMessage++;

            // check for error
            var isError = false;
            foreach (var pattern in patternError)
                if (pattern.IsMatch(sp.msg))
                    isError = true;

            if (isError || sp.isError)
                counterError++;

            // add to rich text box
            AasxWpfBaseUtils.StoredPrintToRichTextBox(
                this.TextBoxMessages, sp, AasxWpfBaseUtils.BrightPrintColors, isError, link_Click);

            // move scroll
            if (this.CheckBoxAutoScroll.IsChecked == true)
                this.TextBoxMessages.ScrollToEnd();

            // update status
            var status = $"Messages: {this.counterMessage}";
            if (this.counterError > 0)
                status += $" and Errors: {this.counterError}";
            this.TextBoxSummary.Text = status;
        }

        protected void link_Click(object sender, RoutedEventArgs e)
        {
            // access
            var link = sender as Hyperlink;
            if (link == null || link.NavigateUri == null)
                return;

            // get url
            var uri = link.NavigateUri.ToString();
            AasxPackageExplorer.Log.Singleton.Info($"Displaying {uri} remotely in external viewer ..");
            System.Diagnostics.Process.Start(uri);
        }

        /// <summary>
        /// Registers an error pattern, based on a regex, which is applied to each incoming log message
        /// </summary>
        public void AddPatternError(Regex pattern)
        {
            if (pattern == null)
                return;
            this.patternError.Add(pattern);
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

        public void CloseControlExplicit()
        {
            if (this.timer != null)
                this.timer.Stop();
            if (this.ControlClosed != null)
                this.ControlClosed();
        }

        //
        // Mechanics
        //

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            if (ControlCloseWarnTime < 0)
            {
                // simply close
                this.Result = false;
                if (this.timer != null)
                    this.timer.Stop();
                ControlClosed?.Invoke();
            }
            else
            {
                if (ControlWillBeClosed != null)
                    ControlWillBeClosed();
                timeToCloseControl = ControlCloseWarnTime;
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
