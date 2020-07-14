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
    public partial class SelectFromRepositoryFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        public string ResultFilename = null;

        private AasxFileRepository TheAasxRepo = null;

        public SelectFromRepositoryFlyout()
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

        public bool LoadAasxRepoFile(string fn = null)
        {
            try
            {
                // load the data
                if (fn == null)
                {
                    // Static example
                    var init =
                        @"{
  'filemaps': [
    {
      'assetid': 'http://pk.festo.com/3s7plfdrs35',
      'tag': 'F',
      'fn': 'C:\\Users\\miho\\Desktop\\AasxPackageExplorer\\Sample_AAS\\Festo-USB-stick-sample-admin-shell.aasx'
    },
    {
      'assetid': 'http://pk.pf.com/40000039198163',
      'tag': 'PF',
      'fn': 'C:\\Users\\miho\\Desktop\\AasxPackageExplorer\\Sample_AAS\\pf_232769_40000039198163.aasx'
    },
    {
      'assetid': 'www.phoenixcontact.com/asset/product/2404267',
      'tag': 'PC',
      'fn': 'C:\\Users\\miho\\Desktop\\AasxPackageExplorer\\Sample_AAS\\Phoenix Contact AXC F 2152 - 11.aasx'
    }
  ]
}";
                    this.TheAasxRepo = JsonConvert.DeserializeObject<AasxFileRepository>(init);
                }
                else
                {
                    // from file
                    if (!File.Exists(fn))
                        return false;
                    var init = File.ReadAllText(fn);
                    this.TheAasxRepo = JsonConvert.DeserializeObject<AasxFileRepository>(init);
                }

                // rework buttons
                this.StackPanelTags.Children.Clear();
                foreach (var fm in this.TheAasxRepo.filemaps)
                {
                    var tag = fm.tag.Trim();
                    if (tag != "")
                    {
                        var b = new Button();
                        b.Style = (Style)FindResource("TranspRoundCorner");
                        b.Content = "" + tag;
                        b.Height = 40;
                        b.Width = 40;
                        b.Margin = new Thickness(5, 0, 5, 0);
                        b.Foreground = Brushes.White;
                        b.Click += TagButton_Click;
                        this.StackPanelTags.Children.Add(b);
                        fm.link = b;
                    }
                }

            }
            catch
            {
                this.TheAasxRepo = null;
                return false;
            }

            return true;
        }

        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.TheAasxRepo != null && this.TheAasxRepo.filemaps != null)
                foreach (var fm in this.TheAasxRepo.filemaps)
                    if (fm.link == sender)
                    {
                        this.ResultFilename = fm.fn;
                        ControlClosed?.Invoke();
                    }
        }

        //
        // Mechanics
        //

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            ResultFilename = null;
            ControlClosed?.Invoke();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.TextBoxAssetId.Text = "";
            this.TextBoxAssetId.Focus();
            this.TextBoxAssetId.Select(0, 999);
            FocusManager.SetFocusedElement(this, this.TextBoxAssetId);
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            // get text
            var aid = TextBoxAssetId.Text.Trim().ToLower();

            // first compare against tags
            if (this.TheAasxRepo != null && this.TheAasxRepo.filemaps != null)
                foreach (var fm in this.TheAasxRepo.filemaps)
                    if (aid == fm.tag.Trim().ToLower())
                    {
                        this.ResultFilename = fm.fn;
                        ControlClosed?.Invoke();
                        return;
                    }

            // if not, compare assit ids
            if (this.TheAasxRepo != null && this.TheAasxRepo.filemaps != null)
                foreach (var fm in this.TheAasxRepo.filemaps)
                    if (aid == fm.assetId.Trim().ToLower())
                    {
                        this.ResultFilename = fm.fn;
                        ControlClosed?.Invoke();
                        return;
                    }
        }

        private void TextBoxAssetId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                // fake click
                this.ButtonOk_Click(null, null);
            }
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // quit
                ResultFilename = null;
                ControlClosed?.Invoke();
            }
        }
    }
}
