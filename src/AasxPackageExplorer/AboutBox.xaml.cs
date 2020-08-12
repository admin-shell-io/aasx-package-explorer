using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).
*/

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für AboutBox.xaml
    /// </summary>
    public partial class AboutBox : Window
    {
        public AboutBox()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // HEADER
            this.HeaderText.Text = "AASX Package Explorer\n" +
                "Copyright (c) 2018-2020 Festo AG & Co. KG and further (see below)\n" +
                "Authors: " + Options.Curr.PrefAuthors + " (see below)\n" +
                "This software is licensed under the Apache License 2.0 (see below)" + "\n" +
                "Version: " + Options.Curr.PrefVersion + "\n" +
                "Build date: " + Options.Curr.PrefBuildDate;

            this.InfoBox.Text = "[AasxPackageExplorer]" + Environment.NewLine + Options.Curr.PrefLicenseLong;

            // try to include plug-ins as well
            var lic = Plugins.CompileAllLicenses();
            if (lic != null && lic.longLicense != null && lic.longLicense.Length > 0)
                this.InfoBox.Text += "\n\n" + lic.longLicense;
        }
    }
}
