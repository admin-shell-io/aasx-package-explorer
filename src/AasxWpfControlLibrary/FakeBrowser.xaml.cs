/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AasxPackageExplorer
{
    public partial class FakeBrowser : UserControl
    {
        public FakeBrowser()
        {
            InitializeComponent();
        }

        private string _address = "";
        public string Address
        {
            get
            {
                return _address;
            }
            set
            {
                _address = value;

                // distinct between local and global
                Uri uri = null;
                if (_address.Trim().ToLower().StartsWith("http://")
                    || _address.Trim().ToLower().StartsWith("https://"))
                {
                    uri = new Uri(_address, UriKind.RelativeOrAbsolute);
                }
                else
                {
                    var path = System.IO.Path.Combine("", _address);
                    uri = new Uri(path);
                }

                // set it
                theImage.Source = new BitmapImage(uri);
            }
        }

        public double ZoomLevel = 1.0;
    }
}
