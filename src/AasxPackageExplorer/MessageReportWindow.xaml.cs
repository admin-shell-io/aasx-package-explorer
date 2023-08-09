/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AasxIntegrationBase;
using AasxPackageLogic;

namespace AasxPackageExplorer
{
    public partial class MessageReportWindow : Window
    {
        public MessageReportWindow(IEnumerable<StoredPrint> storedPrints, string windowTitle = null)
        {
            InitializeComponent();
            if (windowTitle != null)
                this.Title = windowTitle;

            FlowDocViewer.Document = new FlowDocument();

            ButtonToggleWrap.IsChecked = true;
            SetWordWrapping(true);

            foreach (var sp in storedPrints)
            {
#if __disabled
                // Add to rich text box
                AasxWpfBaseUtils.StoredPrintToRichTextBox(
                    this.RichTextTextReport, sp, AasxWpfBaseUtils.DarkPrintColors, linkClickHandler: link_Click);
#endif
                // Add to flow document
                AasxWpfBaseUtils.StoredPrintToFloqDoc(
                    FlowDocViewer.Document, sp, AasxWpfBaseUtils.DarkPrintColors,
                    linkClickHandler: link_Click);
            }
        }

        protected ScrollViewer FindScrollViewer(FlowDocumentScrollViewer flowDocumentScrollViewer)
        {
            if (VisualTreeHelper.GetChildrenCount(flowDocumentScrollViewer) == 0)
            {
                return null;
            }

            // Border is the first child of first child of a ScrolldocumentViewer
            DependencyObject firstChild = VisualTreeHelper.GetChild(flowDocumentScrollViewer, 0);

            Decorator border = VisualTreeHelper.GetChild(firstChild, 0) as Decorator;

            if (border == null)
            {
                return null;
            }

            return border.Child as ScrollViewer;
        }

        public MessageReportWindow(string fullText, string windowTitle = null)
        {
            InitializeComponent();
            if (windowTitle != null)
                this.Title = windowTitle;

#if __disabled
            this.RichTextTextReport.Document.Blocks.Clear();
            this.RichTextTextReport.Document.Blocks.Add(new Paragraph(new Run(fullText)));
#endif
            this.FlowDocViewer.Document.Blocks.Clear();
            this.FlowDocViewer.Document.Blocks.Add(new Paragraph(new Run(fullText)));
        }

        protected void link_Click(object sender, RoutedEventArgs e)
        {
            // access
            var link = sender as Hyperlink;
            if (link == null || link.NavigateUri == null)
                return;

            // get url
            var uri = link.NavigateUri.ToString();
            Log.Singleton.Info($"Displaying {uri} remotely in external viewer ..");
            System.Diagnostics.Process.Start(uri);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // scroll
            FindScrollViewer(FlowDocViewer)?.ScrollToEnd();
        }

#if __disabled
        private void ButtonCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            var tr = new TextRange(
                this.RichTextTextReport.Document.ContentStart,
                this.RichTextTextReport.Document.ContentEnd
            );
            Clipboard.SetText(tr.Text);
            this.DialogResult = true;
        }
#endif

        public void AddStoredPrint(StoredPrint sp)
        {
            // access
            if (FlowDocViewer?.Document == null || sp == null)
                return;

            // Add to flow document
            AasxWpfBaseUtils.StoredPrintToFloqDoc(
                FlowDocViewer.Document, sp, AasxWpfBaseUtils.DarkPrintColors,
                linkClickHandler: link_Click);

            // scroll
            FindScrollViewer(FlowDocViewer)?.ScrollToEnd();
        }

        private void SetWordWrapping(bool state)
        {
            if (state)
            {
                FlowDocViewer.Document.PageWidth = double.NaN;
            }
            else
            {
                FlowDocViewer.Document.PageWidth = 9999;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonGoTop)
            {
                FindScrollViewer(FlowDocViewer)?.ScrollToTop();
            }

            if (sender == ButtonGoBottom)
            {
                FindScrollViewer(FlowDocViewer)?.ScrollToBottom();
            }

            if (sender == ButtonToggleWrap)
            {
                SetWordWrapping(ButtonToggleWrap.IsChecked == true);
            }

            if (sender == ButtonClear)
            {
                FlowDocViewer.Document.Blocks.Clear();
            }
        }
    }
}
