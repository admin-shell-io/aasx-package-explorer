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
    /// Interaktionslogik für FakeBrowser.xaml
    /// </summary>
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
