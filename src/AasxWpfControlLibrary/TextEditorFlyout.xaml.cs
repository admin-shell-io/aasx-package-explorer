/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using static AasxPackageExplorer.Plugins;

namespace AasxPackageExplorer
{
    public partial class TextEditorFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        public bool Result = false;

        private PluginInstance pluginInstance = null;

        private Control textControl = null;

        private string bufferedText = "";

        public TextEditorFlyout(
            string caption)
        {
            InitializeComponent();

            // texts
            this.TextBlockCaption.Text = caption;

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

        //
        // Outer
        //

        public void ControlStart()
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

        public void SetMimeTypeAndText(string mimeType, string text)
        {
            this.bufferedText = text;

            if (this.pluginInstance == null && textControl is TextBox tb)
                tb.Text = text;
            if (this.pluginInstance != null && this.pluginInstance.HasAction("set-content"))
                this.pluginInstance.InvokeAction("set-content", mimeType, text);
        }

        public string Text
        {
            get
            {
                return this.bufferedText;
            }
        }

        private void PrepareResult()
        {
            // move text back to buffered text
            if (this.pluginInstance == null && textControl is TextBox tb)
                this.bufferedText = tb.Text;
            if (this.pluginInstance != null && this.pluginInstance.HasAction("get-content"))
            {
                var res = this.pluginInstance.InvokeAction("get-content");
                if (res is AasxPluginResultBaseObject rbo)
                    this.bufferedText = rbo.obj as string;
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Result = false;
            ControlClosed?.Invoke();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            PrepareResult();
            this.Result = true;
            ControlClosed?.Invoke();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // focus
            if (this.textControl != null)
            {
                this.textControl.Focus();
                if (this.pluginInstance == null && textControl is TextBox tb)
                    tb.Select(0, 0);
                FocusManager.SetFocusedElement(this, this.textControl);
            }
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
        }
    }
}
