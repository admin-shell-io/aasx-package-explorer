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

namespace AasxPackageExplorer
{
    public partial class ProgressBarFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataProgress DiaData = new AnyUiDialogueDataProgress();

        public ProgressBarFlyout(string caption = null, string info = null, AnyUiMessageBoxImage? symbol = null)
        {
            InitializeComponent();

            // inits
            if (caption != null)
                DiaData.Caption = caption;
            if (info != null)
                DiaData.Info = info;
            if (symbol.HasValue)
                DiaData.Symbol = symbol.Value;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // texts
            this.LabelCaption.Content = DiaData.Caption;
            this.LabelInfo.Content = DiaData.Info;

            // image
            this.ImageIcon.Source = null;
            if (DiaData.Symbol == AnyUiMessageBoxImage.Error)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxIntegrationBaseWpf;component/Resources/msg_error.png", UriKind.RelativeOrAbsolute));
            if (DiaData.Symbol == AnyUiMessageBoxImage.Hand)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxIntegrationBaseWpf;component/Resources/msg_hand.png", UriKind.RelativeOrAbsolute));
            if (DiaData.Symbol == AnyUiMessageBoxImage.Information)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxIntegrationBaseWpf;component/Resources/msg_info.png", UriKind.RelativeOrAbsolute));
            if (DiaData.Symbol == AnyUiMessageBoxImage.Question)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri(
                        "/AasxIntegrationBaseWpf;component/Resources/msg_question.png",
                        UriKind.RelativeOrAbsolute));
            if (DiaData.Symbol == AnyUiMessageBoxImage.Warning)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxIntegrationBaseWpf;component/Resources/msg_warning.png", UriKind.RelativeOrAbsolute));

            // wire event
            DiaData.DataChanged += (progress, info) =>
            {
                Progress = progress;
                Info = info;
            };
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

        public string Info
        {
            get
            {
                return LabelInfo.Content as string;
            }
            set
            {
                LabelInfo.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => LabelInfo.Content = value));
            }
        }

        public double Progress
        {
            get
            {
                return TheProgressBar.Value;
            }
            set
            {
                TheProgressBar.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => TheProgressBar.Value = value));
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DiaData.Result = false;
            ControlClosed?.Invoke();
        }

    }
}
