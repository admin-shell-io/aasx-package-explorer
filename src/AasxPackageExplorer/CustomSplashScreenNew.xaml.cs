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
using System.Windows.Threading;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License - v 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für CustomSplahsScreen.xaml
    /// </summary>
    public partial class CustomSplashScreenNew : Window
    {
        public CustomSplashScreenNew()
        {
            InitializeComponent();

            // set new values here
            this.TextBlockAuthors.Text = Options.Curr.PrefAuthors;
            this.TextBlockLicenses.Text = Options.Curr.PrefLicenseShort;
            this.TextBlockVersion.Text = "V" + Options.Curr.PrefVersion;
            this.TextBlockBuildDate.Text = Options.Curr.PrefBuildDate;

            // try to include plug-ins as well
            var lic = Plugins.CompileAllLicenses();
            if (lic != null && lic.shortLicense != null && lic.shortLicense.Length > 0)
                this.TextBlockLicenses.Text += "\n" + lic.shortLicense;

            // Timer
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(Options.Curr.SplashTime < 0 ? 8000 : Options.Curr.SplashTime);
            timer.IsEnabled = true;
            timer.Tick += (object sender, EventArgs e) =>
            {
                // this.DialogResult = true;
                this.Close();
            };
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
