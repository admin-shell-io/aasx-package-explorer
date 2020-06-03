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

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für HintBubble.xaml
    /// </summary>
    public partial class HintBubble : UserControl
    {
        public HintBubble()
        {
            InitializeComponent();
        }

        public string Text
        {
            get
            {
                return innerbubble.Text;
            }
            set
            {
                innerbubble.Text = value;
            }
        }

        public new Brush Foreground
        {
            get { return innerbubble.Foreground; }
            set { innerbubble.Foreground = value; }
        }

        public new Brush Background
        {
            get { return innerbubble.Background; }
            set
            {
                innerbubble.Background = value;
                outerbubble.Fill = value;
            }
        }
    }
}
