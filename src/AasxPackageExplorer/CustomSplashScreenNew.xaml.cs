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
            this.TextBlockVersion.Text = Options.Curr.PrefVersion;
            this.TextBlockBuildDate.Text = "";

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
                this.Close();
            };
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
