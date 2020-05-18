﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using QRCoder;
using AdminShellNS;
using System.Xml.Serialization;
using ZXing;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Newtonsoft.Json;
using System.IO;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License - v 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation (QRCoder) is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC, ZXing.Net) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxPackageExplorer
{
    public class AasxPrintFunctions
    {
        private static void Print(Visual v)
        {

            System.Windows.FrameworkElement e = v as System.Windows.FrameworkElement;
            if (e == null)
                return;

            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                //store original scale
                Transform originalScale = e.LayoutTransform;
                //get selected printer capabilities

                System.Printing.PrintCapabilities capabilities = pd.PrintQueue.GetPrintCapabilities(pd.PrintTicket);

                //get scale of the print wrt to screen of WPF visual
                double scale = Math.Min(capabilities.PageImageableArea.ExtentWidth / e.ActualWidth, capabilities.PageImageableArea.ExtentHeight /
                               e.ActualHeight);

                //Transform the Visual to scale
                e.LayoutTransform = new ScaleTransform(scale, scale);

                //get the size of the printer page
                System.Windows.Size sz = new System.Windows.Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);

                //update the layout of the visual to the printer page size.
                e.Measure(sz);
                e.Arrange(new System.Windows.Rect(new System.Windows.Point(capabilities.PageImageableArea.OriginWidth, capabilities.PageImageableArea.OriginHeight), sz));

                //now print the visual to printer to fit on the one page.
                pd.PrintVisual(v, "My Print");

                //apply the original transform.
                e.LayoutTransform = originalScale;
            }
        }

        public class CodeSheetItem
        {
            public string id = "";
            public string description = "";
            public string code = "";
            public double normSize = 1.0;
        }

        public static void PrintCodeSheet(CodeSheetItem[] codeSheetItems, string title = "Asset repository code sheet")
        {
            // grid
            var numrow = 4;
            var numcol = 4;
            var overSize = new System.Windows.Size(12000, 12000);

            var g = new Grid();
            for (int i = 0; i < 1 + 2 * numrow; i++)
            {
                var rd = new RowDefinition();
                rd.Height = GridLength.Auto;
                g.RowDefinitions.Add(rd);
            }
            for (int i = 0; i < numcol; i++)
            {
                var cd = new ColumnDefinition();
                cd.Width = GridLength.Auto;
                g.ColumnDefinitions.Add(cd);
            }
            g.Margin = new Thickness(50);
            g.Children.Clear();

            // title
            var tlb = new Label();
            tlb.Content = title;
            tlb.FontSize = 24;
            tlb.HorizontalContentAlignment = HorizontalAlignment.Center;
            tlb.VerticalContentAlignment = VerticalAlignment.Center;
            Grid.SetRow(tlb, 0);
            Grid.SetColumn(tlb, 0);
            Grid.SetColumnSpan(tlb, numcol);
            g.Children.Add(tlb);

            // all items
            foreach (var csi in codeSheetItems)
            {
                var row = (g.Children.Count / 2) / numcol;
                var col = (g.Children.Count / 2) % numcol;

                if (g.Children.Count / 2 >= numrow * numcol)
                    break;

                Bitmap bmp = null;
                if (csi.code.Trim().ToLower() == "dmc")
                {
                    // DMC
                    var barcodeWriter = new BarcodeWriter();

                    // set the barcode format
                    barcodeWriter.Format = BarcodeFormat.DATA_MATRIX;

                    // write text and generate a 2-D barcode as a bitmap
                    bmp = barcodeWriter.Write(csi.id);
                }
                else
                if (csi.code.Trim().ToLower() == "qr")
                {
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(csi.id, QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    bmp = qrCode.GetGraphic(20);
                }
                else
                    continue;

                var imgsrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        bmp.GetHbitmap(),
                        IntPtr.Zero,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(bmp.Width, bmp.Height));
                var img = new System.Windows.Controls.Image();
                img.Source = imgsrc;
                img.Height = 200 * csi.normSize;
                img.Width = 200 * csi.normSize;
                img.VerticalAlignment = VerticalAlignment.Bottom;
                img.HorizontalAlignment = HorizontalAlignment.Center;
                img.Margin = new Thickness(40, 40, 40, 0);
                Grid.SetRow(img, 1 + 2 * row);
                Grid.SetColumn(img, col);
                g.Children.Add(img);

                var lab = new Label();
                lab.Content = csi.description;

                var labview = new Viewbox();
                labview.Child = lab;
                labview.Stretch = Stretch.Uniform;
                labview.Width = 200;
                labview.Height = 40;
                Grid.SetRow(labview, 1 + 2 * row + 1);
                Grid.SetColumn(labview, col);
                g.Children.Add(labview);
            }


            var cb = new Border();
            cb.BorderBrush = System.Windows.Media.Brushes.Black;
            cb.BorderThickness = new Thickness(1);
            g.Measure(overSize);
            g.Arrange(new Rect(new System.Windows.Point(0, 0), g.DesiredSize)); // this will bring down the overSize to adequate values
            cb.Child = g;
            cb.Measure(overSize);
            cb.Arrange(new Rect(new System.Windows.Point(0, 0), cb.DesiredSize)); // this will bring down the overSize to adequate values

            Print(cb);
        }

        public static bool PrintRepositoryCodeSheet(string repofn, string title = "Asset repository")
        {
            AasxFileRepository TheAasxRepo = null;
            List<CodeSheetItem> codeSheetItems = new List<CodeSheetItem>();
            try
            {
                // load the data
                if (!File.Exists(repofn))
                    return false;
                var init = File.ReadAllText(repofn);
                TheAasxRepo = JsonConvert.DeserializeObject<AasxFileRepository>(init);

                // all assets
                for (int i = 0; i < TheAasxRepo.filemaps.Count; i++)
                {
                    var fmi = TheAasxRepo.filemaps[i];
                    var csi = new CodeSheetItem();
                    csi.id = fmi.assetId;
                    csi.code = fmi.code;
                    csi.description = fmi.description;
                    csi.normSize = 1.0; // do not vary
                    codeSheetItems.Add(csi);
                }

                // print
                PrintCodeSheet(codeSheetItems.ToArray(), title);

            }
            catch (Exception ex)
            {
                MessageBox.Show("" + ex.Message, "Disappointment");
                return false;
            }
            return true;
        }

        public static bool PrintSingleAssetCodeSheet(string assetId, string description, string title = "Single asset code sheet")
        {
            List<CodeSheetItem> codeSheetItems = new List<CodeSheetItem>();
            try
            {

                // all assets
                for (int i = 0; i < 8; i++)
                {
                    var csi = new CodeSheetItem();
                    csi.id = assetId;
                    csi.code = (i % 2) == 0 ? "qr" : "dmc";
                    csi.description = description;
                    csi.normSize = 0.25 * (1 + (i / 2)); // 2 * .25, 2 * .5, ..
                    codeSheetItems.Add(csi);
                }

                // print
                PrintCodeSheet(codeSheetItems.ToArray(), title);

            }
            catch (Exception ex)
            {
                MessageBox.Show("" + ex.Message, "Disappointment");
                return false;
            }
            return true;
        }



    }
}
