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
using AasxPackageLogic;

namespace AasxPackageExplorer
{
    public partial class AboutBox : Window
    {
        private readonly Pref _pref;

        public AboutBox(Pref pref)
        {
            _pref = pref;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // HEADER
            this.HeaderText.Text = "AASX Package Explorer\n" +
                "Copyright (c) 2018-2023 Festo SE & Co. KG and further (see below)\n" +
                "Authors: " + _pref.Authors + " (see below)\n" +
                "This software is licensed under the Apache License 2.0 (see below)" + "\n" +
                "Version: " + _pref.Version + "\n" +
                "Build date: " + _pref.BuildDate;

            this.InfoBox.Text = "[AasxPackageExplorer]" + Environment.NewLine + _pref.LicenseLong;

            // try to include plug-ins as well
            var lic = Plugins.CompileAllLicenses();
            if (lic != null && lic.longLicense != null && lic.longLicense.Length > 0)
                this.InfoBox.Text += "\n\n" + lic.longLicense;
        }
    }
}
