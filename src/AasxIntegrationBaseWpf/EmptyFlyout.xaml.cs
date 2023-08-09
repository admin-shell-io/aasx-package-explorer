/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
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
using AasxIntegrationBase;
using AnyUi;
using Newtonsoft.Json;

namespace AasxIntegrationBase
{
    public partial class EmptyFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataEmpty DiaData = new AnyUiDialogueDataEmpty();

        public EmptyFlyout(string message = null)
        {
            InitializeComponent();

            // init
            if (message != null)
                DiaData.Message = message;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // infos
            if (DiaData.Message != null)
                LabelMessage.Content = DiaData.Message;
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

        //
        // Mechanics
        //

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            ControlClosed?.Invoke();
        }
    }
}
