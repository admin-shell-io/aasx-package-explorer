/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using AasxIntegrationBase;

namespace AasxPackageExplorer
{
    public partial class MessageReportWindow : Window
    {
        public MessageReportWindow(IEnumerable<StoredPrint> storedPrints)
        {
            InitializeComponent();

            foreach (var sp in storedPrints)
            {
                // Add to rich text box
                AasxWpfBaseUtils.StoredPrintToRichTextBox(
                    this.RichTextTextReport, sp, AasxWpfBaseUtils.DarkPrintColors, linkClickHandler: link_Click);
            }
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

        private void ButtonEmailToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("michael.hoffmeister@festo.com");
        }

        private void ButtonCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            var tr = new TextRange(
                this.RichTextTextReport.Document.ContentStart,
                this.RichTextTextReport.Document.ContentEnd
            );
            Clipboard.SetText(tr.Text);
            this.DialogResult = true;
        }

    }
}
