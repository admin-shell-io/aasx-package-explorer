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
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPackageLogic;
using AdminShellNS;
using AnyUi;

namespace AasxPackageExplorer
{
    public partial class LogMessageMiniFlyout : UserControl, IFlyoutMini
    {
        private System.Windows.Threading.DispatcherTimer _timer = null;

        public LogMessageMiniFlyout()
        {
            InitializeComponent();
        }

        public LogMessageMiniFlyout(
            string caption, string initialMessage,
            Func<StoredPrint> checkForStoredPrint = null)
        {
            InitializeComponent();

            // texts
            this.TextBoxCaption.Content = caption;
            this.TextBoxContent.Text = initialMessage;

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
            };
        }

        public FlyoutAgentBase Agent = null;

        public FlyoutAgentBase GetAgent() { return Agent; }

        public void LogMessage(string msg)
        {
            LogMessage(new StoredPrint(msg));
        }

        public void LogMessage(StoredPrint sp)
        {
            // only display update status (remembered)
            if (sp.MessageType == StoredPrint.MessageTypeEnum.Status)
            {
                var _statusCollected = "";
                if (sp.StatusItems != null)
                    foreach (var si in sp.StatusItems)
                    {
                        if (_statusCollected.HasContent())
                            _statusCollected += "; ";
                        _statusCollected +=
                            $"{"" + ((si.NameShort != null) ? si.NameShort : si.Name)} = {"" + si.Value}";
                    }
                TextBoxContent.Text = _statusCollected;
            }
        }

        //
        // Mechanics
        //

        private void ButtonCloseMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonClose)
            {
                // simply close
                if (this._timer != null)
                    this._timer.Stop();
                GetAgent()?.ClosingAction?.Invoke();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

    }
}
