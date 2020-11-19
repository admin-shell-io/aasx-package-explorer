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
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für SelectFromRepository.xaml
    /// </summary>
    public partial class TextBoxFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        public enum DialogueOptions { None, FilterAllControlKeys };

        private DialogueOptions Options = DialogueOptions.None;

        public bool Result = false;

        private Dictionary<Button, MessageBoxResult> buttonToResult = new Dictionary<Button, MessageBoxResult>();

        public TextBoxFlyout(
            string caption, MessageBoxImage image, DialogueOptions options = DialogueOptions.None,
            double? maxWidth = null)
        {
            InitializeComponent();

            // texts
            this.LabelCaption.Content = caption;

            // dialogue width
            if (maxWidth.HasValue && maxWidth.Value > 200)
                OuterGrid.MaxWidth = maxWidth.Value;

            // options
            this.Options = options;

            // image
            this.ImageIcon.Source = null;
            if (image == MessageBoxImage.Error)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxWpfControlLibrary;component/Resources/msg_error.png", UriKind.RelativeOrAbsolute));
            if (image == MessageBoxImage.Hand)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxWpfControlLibrary;component/Resources/msg_hand.png", UriKind.RelativeOrAbsolute));
            if (image == MessageBoxImage.Information)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxWpfControlLibrary;component/Resources/msg_info.png", UriKind.RelativeOrAbsolute));
            if (image == MessageBoxImage.Question)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxWpfControlLibrary;component/Resources/msg_question.png", UriKind.RelativeOrAbsolute));
            if (image == MessageBoxImage.Warning)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxWpfControlLibrary;component/Resources/msg_warning.png", UriKind.RelativeOrAbsolute));

            // focus
            this.TextBoxText.Text = "";
            this.TextBoxText.Focus();
            this.TextBoxText.Select(0, 999);
            FocusManager.SetFocusedElement(this, this.TextBoxText);
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        //
        // Mechanics
        //

        public string Text
        {
            get
            {
                return TextBoxText.Text;
            }
            set
            {
                TextBoxText.Text = value;
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Result = false;
            ControlClosed?.Invoke();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            this.Result = true;
            ControlClosed?.Invoke();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
            if (this.Options == DialogueOptions.FilterAllControlKeys)
            {
                if (e.Key >= Key.F1 && e.Key <= Key.F24 || e.Key == Key.Escape || e.Key == Key.Enter ||
                        e.Key == Key.Delete || e.Key == Key.Insert)
                {
                    e.Handled = true;
                    return;
                }
            }

            // Close dialogue?
            if (Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Shift)
                return;

            if (e.Key == Key.Return)
            {
                this.Result = true;
                ControlClosed?.Invoke();
            }
            if (e.Key == Key.Escape)
            {
                this.Result = false;
                ControlClosed?.Invoke();
            }
        }
    }
}
