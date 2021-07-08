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

namespace AasxPackageExplorer
{
    public partial class ChangeElementAttributesFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataChangeElementAttributes DiaData = new AnyUiDialogueDataChangeElementAttributes();

        public ChangeElementAttributesFlyout(AnyUiDialogueDataChangeElementAttributes diaData = null)
        {
            InitializeComponent();

            // set initial data
            if (diaData != null)
                DiaData = diaData;
            else
                DiaData = new AnyUiDialogueDataChangeElementAttributes();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // texts
            this.LabelCaption.Text = DiaData.Caption;

            // attribute name combo
            this.ComboBoxAttrName.Items.Clear();
            foreach (var x in Enum.GetValues(typeof(AnyUiDialogueDataChangeElementAttributes.AttributeEnum)))
                this.ComboBoxAttrName.Items.Add("" + x.ToString());
            this.ComboBoxAttrName.SelectedIndex = (int)DiaData.AttributeToChange;

            // attribute lang combo
            this.ComboBoxAttrLang.Items.Clear();
            foreach (var lng in AasxLanguageHelper.GetLangCodes())
                ComboBoxAttrLang.Items.Add("" + lng);
            ComboBoxAttrLang.Text = "en";

            // Pattern (combo)
            this.ComboBoxPattern.Items.Clear();
            foreach (var x in AnyUiDialogueDataChangeElementAttributes.PatternPresets)
                this.ComboBoxPattern.Items.Add("" + x.ToString());
            this.ComboBoxPattern.Text = DiaData.Pattern;

            // focus
            this.ComboBoxPattern.Focus();
            FocusManager.SetFocusedElement(this, this.ComboBoxPattern);
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        public bool Result { get { return DiaData.Result; } }

        //
        // Mechanics
        //

        private void ReadDataBack()
        {
            DiaData.AttributeToChange = (AnyUiDialogueDataChangeElementAttributes.AttributeEnum)
                                            ComboBoxAttrName.SelectedIndex;
            DiaData.AttributeLang = ComboBoxAttrLang.Text;
            DiaData.Pattern = ComboBoxPattern.Text;
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
