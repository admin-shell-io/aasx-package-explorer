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
using AdminShellNS;
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
    public class SelectFromListFlyoutItem
    {
        public string Text = "";
        public object Tag = null;

        public SelectFromListFlyoutItem() { }

        public SelectFromListFlyoutItem(string text, object tag)
        {
            this.Text = text;
            this.Tag = tag;
        }
    }

    /// <summary>
    /// Creates a flyout in order to select items from a list
    /// </summary>
    public partial class SelectFromListFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        public string Caption = "Select item ..";

        public List<SelectFromListFlyoutItem> ListOfItems = null;

        public string[] AlternativeSelectButtons = null;

        public int ResultIndex = -1;
        public SelectFromListFlyoutItem ResultItem = null;

        public SelectFromListFlyout()
        {
            InitializeComponent();
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
        }

        //
        // Mechanics
        //

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // fill caption
            if (this.Caption != null)
                TextBlockCaption.Text = "" + this.Caption;

            // fill listbox
            ListBoxPresets.Items.Clear();
            foreach (var loi in this.ListOfItems)
                ListBoxPresets.Items.Add("" + loi.Text);

            // alternative buttons
            if (this.AlternativeSelectButtons != null)
            {
                this.ButtonsPanel.Children.Clear();
                foreach (var txt in this.AlternativeSelectButtons)
                {
                    var b = new Button();
                    b.Foreground = Brushes.White;
                    b.FontSize = 18;
                    b.Padding = new Thickness(4);
                    b.Margin = new Thickness(4);
                    this.ButtonsPanel.Children.Add(b);
                }
            }
        }

        private bool PrepareResult()
        {
            var i = ListBoxPresets.SelectedIndex;
            if (this.ListOfItems != null && i >= 0 && i < this.ListOfItems.Count)
            {
                this.ResultIndex = i;
                this.ResultItem = this.ListOfItems[i];
                return true;
            }

            // uups
            return false;
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (PrepareResult())
                ControlClosed?.Invoke();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.ResultIndex = -1;
            this.ResultItem = null;
            ControlClosed?.Invoke();
        }

        private void ListBoxPresets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PrepareResult())
                ControlClosed?.Invoke();
        }
    }
}
