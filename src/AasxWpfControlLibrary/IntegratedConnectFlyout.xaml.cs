/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
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
using AasxWpfControlLibrary.PackageCentral;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    public partial class IntegratedConnectFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        private string _caption;
        private double? _maxWidth;
        private string _location;
        private string _initialDirectory;
        // private Func<StoredPrint> _checkForStoredPrint;
        private LogInstance _logger;

        public bool Result;
        public PackageContainerBase ResultContainer;

        public IntegratedConnectFlyout(
            string caption = null, 
            double? maxWidth = null,
            string initialLocation = null,
            string initialDirectory = null,
            // Func<StoredPrint> checkForStoredPrint = null
            LogInstance logger = null)
        {
            InitializeComponent();

            _caption = caption;
            _maxWidth = maxWidth;
            _location = initialLocation;
            _initialDirectory = initialDirectory;
            // _checkForStoredPrint = checkForStoredPrint;
            _logger = logger;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // texts
            if (_caption != null)
                this.LabelCaption.Content = _caption;

            // dialogue width
            if (_maxWidth.HasValue && _maxWidth.Value > 200)
                OuterGrid.MaxWidth = _maxWidth.Value;

            // start page
            if (_location != null)
                TextBoxStartLocation.Text = _location;

            // focus
            this.TextBoxStartLocation.Focus();
            this.TextBoxStartLocation.Select(0, 999);
            FocusManager.SetFocusedElement(this, this.TextBoxStartLocation);

            // timer
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Start();
            timer.Tick += (object sender2, EventArgs e2) =>
            {
                //var msg = _checkForStoredPrint?.Invoke();
                //while (msg != null)
                //{
                //    this.LogMessage(msg);
                //    msg = _checkForStoredPrint?.Invoke();
                //}                
                var msg = _logger?.PopLastShortTermPrint();
                while (msg != null)
                {
                    Log.Singleton.Append(msg);
                    this.LogMessage(msg);
                    msg = _logger?.PopLastShortTermPrint();
                }
            };
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        public void LogMessage(StoredPrint sp)
        {
            // access
            if (sp == null || sp.msg == null)
                return;

            // add to rich text box
            AasxWpfBaseUtils.StoredPrintToRichTextBox(
                this.TextBoxMessages, sp, AasxWpfBaseUtils.BrightPrintColors);

            // move scroll
            if (true /* this.CheckBoxAutoScroll.IsChecked == true */)
                this.TextBoxMessages.ScrollToEnd();
        }

        //
        // Mechanics
        //

        // see: https://stackoverflow.com/questions/52706251/c-sharp-async-await-a-button-click

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonMsgSmaller && TextBoxMessages.FontSize >= 6.0)
                TextBoxMessages.FontSize -= 2.0;
            if (sender == ButtonMsgLarger && TextBoxMessages.FontSize < 99.0)
                TextBoxMessages.FontSize += 2.0;

            if (sender == ButtonStartSelect)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                if (_initialDirectory != null)
                {
                    dlg.InitialDirectory = _initialDirectory;
                    _initialDirectory = null;
                }
                dlg.Filter =
                    "AASX package files (*.aasx)|*.aasx|AAS XML file (*.xml)|*.xml|" +
                    "AAS JSON file (*.json)|*.json|All files (*.*)|*.*";

                var res = dlg.ShowDialog();
                if (res == true)
                {
                    TextBoxStartLocation.Text = dlg.FileName;
                }
            }

            if (sender == ButtonStartProceed)
            {
                // make runtime options to link to this dialogue
                var ro = new PackageContainerRuntimeOptions()
                {
                    Log = _logger,
                    ProgressChanged = (tfs, tbd) =>
                    {
                        // determine
                        if (tfs == null)
                            tfs = 5 * 1024 * 1024;
                        var frac = Math.Min(100.0, 100.0 * tbd / tfs.Value);

                        // thread safe
                        TheProgressBar.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() => TheProgressBar.Value = frac));

                        LabelProgressText.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() => LabelProgressText.Content = $"{tbd} bytes transferred"));
                    }
                };

                // Log
                var location = TextBoxStartLocation.Text;
                _logger?.Info($"Connect (integrated): Trying to connect to {location} ..");

                // try do the magic
                try
                {
                    var x = PackageContainerFactory.GuessAndCreateFor(
                        location,
                        loadResident: true,
                        runtimeOptions: ro);
                } catch (Exception ex)
                {
                    _logger?.Error(ex, "when guessing for packager container!");
                }
            }
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
            /*
            if (this.Options == DialogueOptions.FilterAllControlKeys)
            {
                if (e.Key >= Key.F1 && e.Key <= Key.F24 || e.Key == Key.Escape || e.Key == Key.Enter ||
                        e.Key == Key.Delete || e.Key == Key.Insert)
                {
                    e.Handled = true;
                    return;
                }
            }
            */

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


        private void ListBoxSelectAAS_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void TextBoxMessages_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                return;
            }

            e.Handled = true;
            if (e.Delta > 0)
            {
                ++TextBoxMessages.FontSize;
            }
            else
            {
                if (TextBoxMessages.FontSize >= 6.0)
                --TextBoxMessages.FontSize;
            }
        }
    }
}
