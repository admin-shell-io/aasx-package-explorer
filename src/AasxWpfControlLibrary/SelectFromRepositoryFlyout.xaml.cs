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

        public AasxFileRepository.FileItem ResultItem = null;

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

        public bool LoadAasxRepoFile(string fn = null, AasxFileRepository repo = null)
        {
            try
            {
                this.TheAasxRepo = null;

                if (fn != null)
                {
                    // from file
                    this.TheAasxRepo = AasxFileRepository.Load(fn);

                }

                if (repo != null)
                {
                    // from RAM
                    this.TheAasxRepo = repo;
                }

                if (this.TheAasxRepo == null)
                    return false;

                // rework buttons
                this.StackPanelTags.Children.Clear();
                foreach (var fm in this.TheAasxRepo.FileMap)
                {
                    var tag = fm.Tag.Trim();
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
                        b.Tag = fm;
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
            var b = sender as Button;
            if (b?.Tag != null && this.TheAasxRepo?.FileMap != null && this.TheAasxRepo.FileMap.Contains(b.Tag))
            {
                this.ResultItem = b.Tag as AasxFileRepository.FileItem;
                ControlClosed?.Invoke();
            }
        }

        //
        // Mechanics
        //

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            ResultItem = null;
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
            if (this.TheAasxRepo != null && this.TheAasxRepo.FileMap != null)
                foreach (var fm in this.TheAasxRepo.FileMap)
                    if (aid == fm.Tag.Trim().ToLower())
                    {
                        this.ResultItem = fm;
                        ControlClosed?.Invoke();
                        return;
                    }

            // if not, compare assit ids
            if (this.TheAasxRepo != null && this.TheAasxRepo.FileMap != null)
                foreach (var fm in this.TheAasxRepo.FileMap)
                    if (aid == fm.AssetId.Trim().ToLower())
                    {
                        this.ResultItem = fm;
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
                ResultItem = null;
                ControlClosed?.Invoke();
            }
        }
    }
}
