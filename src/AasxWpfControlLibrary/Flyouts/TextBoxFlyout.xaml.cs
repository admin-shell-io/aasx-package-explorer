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
    public partial class TextBoxFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataTextBox DiaData = new AnyUiDialogueDataTextBox();

        public TextBoxFlyout(
            string Caption = null,
            AnyUiMessageBoxImage Symbol = AnyUiMessageBoxImage.None
            )
        {
            InitializeComponent();

            // set initial data
            DiaData = new AnyUiDialogueDataTextBox(Caption, symbol: Symbol);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // texts
            this.LabelCaption.Content = DiaData.Caption;

            // dialogue width
            if (DiaData.MaxWidth.HasValue && DiaData.MaxWidth.Value > 200)
                OuterGrid.MaxWidth = DiaData.MaxWidth.Value;

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
                    new Uri("/AasxIntegrationBaseWpf;component/Resources/msg_question.png",
                            UriKind.RelativeOrAbsolute));
            if (DiaData.Symbol == AnyUiMessageBoxImage.Warning)
                this.ImageIcon.Source = new BitmapImage(
                    new Uri("/AasxIntegrationBaseWpf;component/Resources/msg_warning.png",
                            UriKind.RelativeOrAbsolute));

            // text to edit
            this.TextBoxText.Text = DiaData.Text;

            // focus
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

        public void LambdaActionAvailable(AnyUiLambdaActionBase la)
        {
        }

        public string Text
        {
            get
            {
                return DiaData.Text;
            }
            set
            {
                DiaData.Text = value;
                TextBoxText.Text = DiaData.Text;
            }
        }

        public bool Result { get { return DiaData.Result; } }

        //
        // Mechanics
        //

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DiaData.Result = false;
            ControlClosed?.Invoke();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            DiaData.Result = true;
            DiaData.Text = TextBoxText.Text;
            ControlClosed?.Invoke();
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
            if (this.DiaData.Options == AnyUiDialogueDataTextBox.DialogueOptions.FilterAllControlKeys)
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
                DiaData.Result = true;
                DiaData.Text = TextBoxText.Text;
                ControlClosed?.Invoke();
            }
            if (e.Key == Key.Escape)
            {
                DiaData.Result = false;
                ControlClosed?.Invoke();
            }
        }

        private void TextBoxText_TextChanged(object sender, TextChangedEventArgs e)
        {
            DiaData.Text = TextBoxText.Text;
        }
    }
}
