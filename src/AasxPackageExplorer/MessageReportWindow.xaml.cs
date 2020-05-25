using AasxGlobalLogging;
using AasxIntegrationBase;
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

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License - v 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für MessageReportWindow.xaml
    /// </summary>
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
            AasxWpfBaseUtils.StoredPrintToRichTextBox(this.RichTextTextReport, sp, AasxWpfBaseUtils.DarkPrintColors, linkClickHandler: link_Click);
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
