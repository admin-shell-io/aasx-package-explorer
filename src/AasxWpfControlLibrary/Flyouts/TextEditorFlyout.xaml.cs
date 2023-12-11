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
using AasxIntegrationBaseWpf;
using AasxPackageLogic;
using AnyUi;
using Newtonsoft.Json;
using static AasxPackageLogic.Plugins;

namespace AasxPackageExplorer
{
    public partial class TextEditorFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataTextEditor DiaData = new AnyUiDialogueDataTextEditor();

        public Func<DynamicContextMenu> ContextMenuCreate = null;
        public AasxMenuActionDelegate ContextMenuAction = null;

        private PluginInstance pluginInstance = null;
        private Control textControl = null;

        public TextEditorFlyout(
            string caption = null)
        {
            InitializeComponent();

            // set initial data
            DiaData = new AnyUiDialogueDataTextEditor(caption);

            // find plugin?
            var res = TrySetTextControl();
            if (res)
            {
                // place text control from plugin
                var tb = this.textControl;
                Grid.SetRow(tb, 1);
                Grid.SetColumn(tb, 1);
                OuterGrid.Children.Add(tb);
            }
            else
            {
                // make text Control
                var tb = new TextBox();
                tb.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                tb.Foreground = Brushes.White;
                tb.FontSize = 14;
                tb.TextWrapping = TextWrapping.NoWrap;
                tb.AcceptsReturn = true;
                tb.AcceptsTab = true;
                tb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                tb.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                this.textControl = tb;

                Grid.SetRow(tb, 1);
                Grid.SetColumn(tb, 1);
                OuterGrid.Children.Add(tb);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // texts
            this.TextBlockCaption.Text = DiaData.Caption;

            // dialogue width
            if (DiaData.MaxWidth.HasValue && DiaData.MaxWidth.Value > 200)
                OuterGrid.MaxWidth = DiaData.MaxWidth.Value;

            // populate preset combo
            ComboBoxPreset.Items.Clear();
            ComboBoxPreset.ItemsSource = DiaData.Presets?.Select((o) => o.Name);

            // text to edit
            SetMimeTypeAndText();

            // focus
            if (this.textControl != null)
            {
                this.textControl.Focus();
                if (this.pluginInstance == null && textControl is TextBox tb)
                    tb.Select(0, 0);
                FocusManager.SetFocusedElement(this, this.textControl);
            }
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

        //
        // Business logic
        //

        private bool TrySetTextControl()
        {
            var pluginName = "AasxPluginAdvancedTextEditor";
            var actionName = "get-textedit-control";
            this.pluginInstance = Plugins.FindPluginInstance(pluginName);

            if (this.pluginInstance == null || !this.pluginInstance.HasAction(actionName))
            {
                // ok, fallback
                return false;
            }

            // setup
            var res = this.pluginInstance.InvokeAction(actionName, "");
            if (res != null && res is AasxPluginResultBaseObject)
            {
                this.textControl = (res as AasxPluginResultBaseObject).obj as Control;
                return true;
            }

            return false;
        }

        //
        // Mechanics
        //

        private void SetMimeTypeAndText()
        {
            if (this.pluginInstance == null && textControl is TextBox tb)
                tb.Text = DiaData.Text;
            if (this.pluginInstance != null && this.pluginInstance.HasAction("set-content"))
                this.pluginInstance.InvokeAction("set-content", DiaData.MimeType, DiaData.Text);
        }

        public string Text
        {
            get
            {
                return DiaData.Text;
            }
        }

        private void PrepareResult()
        {
            // move text back to buffered text
            if (this.pluginInstance == null && textControl is TextBox tb)
                DiaData.Text = tb.Text;
            if (this.pluginInstance != null && this.pluginInstance.HasAction("get-content"))
            {
                var res = this.pluginInstance.InvokeAction("get-content");
                if (res is AasxPluginResultBaseObject rbo)
                    DiaData.Text = rbo.obj as string;
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            // prepare result, as client application migt want to save modified but cancelled text
            PrepareResult();
            DiaData.Result = false;
            ControlClosed?.Invoke();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonOk)
            {
                PrepareResult();
                DiaData.Result = true;
                ControlClosed?.Invoke();
            }

            if (sender == ButtonContextMenu)
            {
                // first attempt: directly attached to the flyout
                var cm = ContextMenuCreate?.Invoke();
                var cma = ContextMenuAction;

                // 2nd attempt: within the dia data?
                if (DiaData is AnyUiDialogueDataTextEditorWithContextMenu ddcm)
                {
                    var am = ddcm.ContextMenuCreate?.Invoke();
                    if (am != null)
                    {
                        cm = DynamicContextMenu.CreateNew(am);
                        cma = ddcm.ContextMenuAction;
                    }
                }

                // still not?
                if (cm == null)
                    return;

                // update data
                PrepareResult();

                // show
                cm.Start(sender as Button, cma);
            }
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
        }

        private void ComboBoxPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == ComboBoxPreset
                && DiaData.Presets != null
                && ComboBoxPreset.SelectedIndex >= 0
                && ComboBoxPreset.SelectedIndex < DiaData.Presets.Count)
            {
                DiaData.Text = DiaData.Presets[ComboBoxPreset.SelectedIndex].Text;
                SetMimeTypeAndText();
            }
        }

        private void UserControl_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e?.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Button_Click(ButtonOk, null);
            }
        }
    }
}
