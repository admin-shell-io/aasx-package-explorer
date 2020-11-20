/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using AasxGlobalLogging;
using AasxIntegrationBase;

namespace AasxPackageExplorer
{
    public partial class MessageReportWindow : Window
    {
        public MessageReportWindow()
        {
            InitializeComponent();
        }

        public void Append(StoredPrint sp)
        {
            // access
            if (sp == null || sp.msg == null)
                return;

            // add to rich text box
            AasxWpfBaseUtils.StoredPrintToRichTextBox(
                this.RichTextTextReport, sp, AasxWpfBaseUtils.DarkPrintColors, linkClickHandler: link_Click);
        }

        protected void link_Click(object sender, RoutedEventArgs e)
        {
            // access
            var link = sender as Hyperlink;
            if (link == null || link.NavigateUri == null)
                return;

            // get url
            var uri = link.NavigateUri.ToString();
            Log.Info($"Displaying {uri} remotely in external viewer ..");
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
