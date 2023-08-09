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
using AasxMqttClient;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    public partial class MqttPublisherFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataMqttPublisher DiaData = new AnyUiDialogueDataMqttPublisher();

        public MqttPublisherFlyout(AnyUiDialogueDataMqttPublisher diaData = null)
        {
            InitializeComponent();

            // set initial data
            if (diaData != null)
                DiaData = diaData;
            else
                DiaData = new AnyUiDialogueDataMqttPublisher();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // texts
            this.LabelCaption.Text = DiaData.Caption;
            this.TextBoxHelp.Text = AnyUiDialogueDataMqttPublisher.HelpString;
            this.TextBoxMqttBroker.Text = DiaData.BrokerUrl;
            this.TextBoxFirstTopicAAS.Text = DiaData.FirstTopicAAS;
            this.TextBoxFirstTopicSubmodel.Text = DiaData.FirstTopicSubmodel;
            this.TextBoxEventPublishTopic.Text = DiaData.EventTopic;
            this.TextBoxSingleValueTopic.Text = DiaData.SingleValueTopic;

            // check box
            this.CheckBoxMqttRetain.IsChecked = DiaData.MqttRetain;
            this.CheckBoxFirstPublish.IsChecked = DiaData.EnableFirstPublish;
            this.CheckBoxEventPublish.IsChecked = DiaData.EnableEventPublish;
            this.CheckBoxSingleValuePublish.IsChecked = DiaData.SingleValuePublish;
            this.CheckBoxSingleValueFirstTime.IsChecked = DiaData.SingleValueFirstTime;

            // focus
            this.TextBoxMqttBroker.Focus();
            this.TextBoxMqttBroker.Select(0, 999);
            FocusManager.SetFocusedElement(this, this.TextBoxMqttBroker);
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

        public bool Result { get { return DiaData.Result; } }

        //
        // Mechanics
        //

        private void ReadDataBack()
        {
            DiaData.BrokerUrl = this.TextBoxMqttBroker.Text;
            DiaData.FirstTopicAAS = this.TextBoxFirstTopicAAS.Text;
            DiaData.FirstTopicSubmodel = this.TextBoxFirstTopicSubmodel.Text;
            DiaData.EventTopic = this.TextBoxEventPublishTopic.Text;

            DiaData.MqttRetain = this.CheckBoxMqttRetain.IsChecked == true;
            DiaData.EnableFirstPublish = this.CheckBoxFirstPublish.IsChecked == true;
            DiaData.EnableEventPublish = this.CheckBoxEventPublish.IsChecked == true;
            DiaData.SingleValuePublish = this.CheckBoxSingleValuePublish.IsChecked == true;
            DiaData.SingleValueFirstTime = this.CheckBoxSingleValueFirstTime.IsChecked == true;
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DiaData.Result = false;
            ControlClosed?.Invoke();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            DiaData.Result = true;
            ReadDataBack();
            ControlClosed?.Invoke();
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
            // Close dialogue?
            if (Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Shift)
                return;

            if (e.Key == Key.Return)
            {
                DiaData.Result = true;
                ReadDataBack();
                ControlClosed?.Invoke();
            }
            if (e.Key == Key.Escape)
            {
                DiaData.Result = false;
                ControlClosed?.Invoke();
            }
        }

    }
}
