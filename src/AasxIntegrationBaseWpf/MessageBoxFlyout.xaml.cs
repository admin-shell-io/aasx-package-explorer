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
        public event IFlyoutControlAction ControlClosed;

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

            var layout = LayoutButtons(buttons);
            RenderButtonLayout(
                this, this.StackPanelButtons, layout, 
                StackPanelButton_Click, out buttonToResult);
        }

        public class MessageButtonLayout
        {
            /// <summary>
            /// Title of the buttons to display
            /// </summary>
            public List<string> buttonDefs = new List<string>();

            /// <summary>
            /// Result this very button shall trigger
            /// </summary>
            public List<AnyUiMessageBoxResult> buttonResults = new List<AnyUiMessageBoxResult>();
        }

        public static MessageButtonLayout LayoutButtons(
            AnyUiMessageBoxButton dialogButtons,
            string[] extraButtons = null)
        {
            var res = new MessageButtonLayout();
            if (dialogButtons == AnyUiMessageBoxButton.OK)
            {
                res.buttonDefs.Add("OK");
                res.buttonResults.Add(AnyUiMessageBoxResult.OK);
            }
            if (dialogButtons == AnyUiMessageBoxButton.OKCancel)
            {
                res.buttonDefs.Add("OK");
                res.buttonResults.Add(AnyUiMessageBoxResult.OK);
                res.buttonDefs.Add("Cancel");
                res.buttonResults.Add(AnyUiMessageBoxResult.Cancel);
            }
            if (dialogButtons == AnyUiMessageBoxButton.YesNo)
            {
                res.buttonDefs.Add("Yes");
                res.buttonResults.Add(AnyUiMessageBoxResult.Yes);
                res.buttonDefs.Add("No");
                res.buttonResults.Add(AnyUiMessageBoxResult.No);
            }
            if (dialogButtons == AnyUiMessageBoxButton.YesNoCancel)
            {
                res.buttonDefs.Add("Yes");
                res.buttonResults.Add(AnyUiMessageBoxResult.Yes);
                res.buttonDefs.Add("No");
                res.buttonResults.Add(AnyUiMessageBoxResult.No);
                res.buttonDefs.Add("Cancel");
                res.buttonResults.Add(AnyUiMessageBoxResult.Cancel);
            }
            if (extraButtons != null)
                for (int i=0; i<extraButtons.Length; i++)
                {
                    res.buttonDefs.Add(extraButtons[i]);
                    res.buttonResults.Add(AnyUiMessageBoxResult.Extra0 + i);
                }
            return res;
        }

        public static void RenderButtonLayout(
            UserControl control,
            StackPanel stack,
            MessageButtonLayout layout,
            RoutedEventHandler click,
            out Dictionary<Button, AnyUiMessageBoxResult> buttonToResult,
            double fontSize = 14,
            double buttonWidth = 40,
            double buttonHeight = 40)
        {
            // access
            buttonToResult = new Dictionary<Button, AnyUiMessageBoxResult>();
            if (control == null || stack == null || layout == null)
                return;

            // clear state
            stack.Children.Clear();
            stack.Visibility = (layout.buttonDefs.Count > 0) ? Visibility.Visible : Visibility.Collapsed;

            // render
            foreach (var bd in layout.buttonDefs)
            {
                var b = new Button();
                b.Style = (Style) control.FindResource("TranspRoundCorner");
                b.Content = "" + bd;
                b.Height = buttonHeight;
                b.MinWidth = buttonWidth;
                b.Padding = new Thickness(8, 0, 8, 10);
                b.FontSize = fontSize;
                b.Margin = new Thickness(5, 0, 5, 0);
                b.Foreground = Brushes.White;
                b.Click += click;
                stack.Children.Add(b);
                if (layout.buttonResults.Count > 0)
                {
                    buttonToResult[b] = layout.buttonResults[0];
                    layout.buttonResults.RemoveAt(0);
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

        public void LambdaActionAvailable(AnyUiLambdaActionBase la)
        {
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
