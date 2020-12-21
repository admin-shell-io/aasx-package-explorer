/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
    public partial class MessageBoxFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        public AnyUiMessageBoxResult Result = AnyUiMessageBoxResult.None;

        private Dictionary<Button, AnyUiMessageBoxResult> buttonToResult = 
            new Dictionary<Button, AnyUiMessageBoxResult>();

        public MessageBoxFlyout(string message, string caption, 
            AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image)
        {
            InitializeComponent();

            // texts
            this.TextBlockTitle.Text = caption;
            this.TextBlockMessage.Text = message;

            // image
            this.ImageIcon.Source = null;
            if (image == AnyUiMessageBoxImage.Error)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxIntegrationBaseWpf;component/Resources/msg_error.png", UriKind.RelativeOrAbsolute));
            if (image == AnyUiMessageBoxImage.Hand)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxIntegrationBaseWpf;component/Resources/msg_hand.png", UriKind.RelativeOrAbsolute));
            if (image == AnyUiMessageBoxImage.Information)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxIntegrationBaseWpf;component/Resources/msg_info.png", UriKind.RelativeOrAbsolute));
            if (image == AnyUiMessageBoxImage.Question)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri(
                        "/AasxIntegrationBaseWpf;component/Resources/msg_question.png", UriKind.RelativeOrAbsolute));
            if (image == AnyUiMessageBoxImage.Warning)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri(
                        "/AasxIntegrationBaseWpf;component/Resources/msg_warning.png", UriKind.RelativeOrAbsolute));

            // buttons
            List<string> buttonDefs = new List<string>();
            List<AnyUiMessageBoxResult> buttonResults = new List<AnyUiMessageBoxResult>();
            if (buttons == AnyUiMessageBoxButton.OK)
            {
                buttonDefs.Add("OK");
                buttonResults.Add(AnyUiMessageBoxResult.OK);
            }
            if (buttons == AnyUiMessageBoxButton.OKCancel)
            {
                buttonDefs.Add("OK");
                buttonResults.Add(AnyUiMessageBoxResult.OK);
                buttonDefs.Add("Cancel");
                buttonResults.Add(AnyUiMessageBoxResult.Cancel);
            }
            if (buttons == AnyUiMessageBoxButton.YesNo)
            {
                buttonDefs.Add("Yes");
                buttonResults.Add(AnyUiMessageBoxResult.Yes);
                buttonDefs.Add("No");
                buttonResults.Add(AnyUiMessageBoxResult.No);
            }
            if (buttons == AnyUiMessageBoxButton.YesNoCancel)
            {
                buttonDefs.Add("Yes");
                buttonResults.Add(AnyUiMessageBoxResult.Yes);
                buttonDefs.Add("No");
                buttonResults.Add(AnyUiMessageBoxResult.No);
                buttonDefs.Add("Cancel");
                buttonResults.Add(AnyUiMessageBoxResult.Cancel);
            }
            this.StackPanelButtons.Children.Clear();
            buttonToResult = new Dictionary<Button, AnyUiMessageBoxResult>();
            foreach (var bd in buttonDefs)
            {
                var b = new Button();
                b.Style = (Style)FindResource("TranspRoundCorner");
                b.Content = "" + bd;
                b.Height = 40;
                b.Width = 40;
                b.Margin = new Thickness(5, 0, 5, 0);
                b.Foreground = Brushes.White;
                b.Click += StackPanelButton_Click;
                this.StackPanelButtons.Children.Add(b);
                if (buttonResults.Count > 0)
                {
                    buttonToResult[b] = buttonResults[0];
                    buttonResults.RemoveAt(0);
                }
            }
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        private void StackPanelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button && buttonToResult.ContainsKey(sender as Button))
            {
                this.Result = buttonToResult[sender as Button];
                ControlClosed?.Invoke();
            }
        }

        //
        // Mechanics
        //

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Result = AnyUiMessageBoxResult.None;
            ControlClosed?.Invoke();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Shift)
                return;

            if (e.Key == Key.Y && buttonToResult.ContainsValue(AnyUiMessageBoxResult.Yes))
            {
                this.Result = AnyUiMessageBoxResult.Yes;
                ControlClosed?.Invoke();
            }
            if ((e.Key == Key.N || e.Key == Key.Escape) && buttonToResult.ContainsValue(AnyUiMessageBoxResult.No))
            {
                this.Result = AnyUiMessageBoxResult.No;
                ControlClosed?.Invoke();
            }
            if ((e.Key == Key.O || e.Key == Key.Return) && buttonToResult.ContainsValue(AnyUiMessageBoxResult.OK))
            {
                this.Result = AnyUiMessageBoxResult.OK;
                ControlClosed?.Invoke();
            }
            if ((e.Key == Key.C || e.Key == Key.Escape) && buttonToResult.ContainsValue(AnyUiMessageBoxResult.Cancel))
            {
                this.Result = AnyUiMessageBoxResult.Cancel;
                ControlClosed?.Invoke();
            }
        }
    }
}
