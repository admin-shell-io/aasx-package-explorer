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
using AasxIntegrationBase.AdminShellEvents;
using AasxPackageLogic;
using AnyUi;

namespace AasxPackageExplorer
{
    public partial class LogMessageMiniFlyout : UserControl, IFlyoutMini
    {
        // constants

        public LogMessageMiniFlyout()
        {
            InitializeComponent();
        }

        public FlyoutAgentBase Agent = null;

        public FlyoutAgentBase GetAgent() { return Agent; }

        public void LogMessage(string msg)
        {
            LogMessage(new StoredPrint(msg));
        }

        public void LogMessage(StoredPrint sp)
        {
        }

        //
        // Mechanics
        //

        private void ButtonCloseMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonClose)
            {
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

    }
}
