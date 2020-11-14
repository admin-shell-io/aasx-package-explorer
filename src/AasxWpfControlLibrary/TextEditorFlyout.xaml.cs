using System;
using System.Collections.Generic;
using System.IO;
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
using AasxIntegrationBase;
using Newtonsoft.Json;

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
    /// Interaktionslogik für SelectFromRepository.xaml
    /// </summary>
    public partial class TextEditorFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        public enum DialogueOptions { None, FilterAllControlKeys };

        public bool Result = false;

        private bool textControlFromPlugin = false;

        private Control textControl = null;
                    
        public TextEditorFlyout(
            string caption)
        {
            InitializeComponent();

            // texts
            this.TextBlockCaption.Text = caption;

            // make text Control
            var tb = new TextBox();
            tb.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            tb.Foreground = Brushes.White;
            tb.FontSize = 14;
            tb.TextWrapping = TextWrapping.NoWrap;
            tb.AcceptsReturn = true;
            tb.AcceptsTab = true;
            tb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            tb.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            this.textControl = tb;

            Grid.SetRow(tb, 1);
            Grid.SetColumn(tb, 1);
            OuterGrid.Children.Add(tb);
            
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        //
        // Mechanics
        //

        public string Text
        {
            get
            {
                if (!textControlFromPlugin && textControl is TextBox tb)
                    return tb.Text;
                return "";
            }
            set
            {
                if (!textControlFromPlugin && textControl is TextBox tb)
                    tb.Text = value;
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Result = false;
            ControlClosed?.Invoke();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            this.Result = true;
            ControlClosed?.Invoke();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // focus
            if (this.textControl != null)
            {
                this.textControl.Focus();
                if (!textControlFromPlugin && textControl is TextBox tb)
                    tb.Select(0, 0);
                FocusManager.SetFocusedElement(this, this.textControl);
            }
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
        }
    }
}
