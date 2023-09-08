/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AasxWpfControlLibrary.PackageCentral;
using AdminShellNS;
using Newtonsoft.Json;
using QRCoder;
using ZXing;

namespace AasxPackageExplorer
{
    public static class AasxPrintFunctions
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
                if (capabilities.PageImageableArea == null)
                    return;
                double scale = Math.Min(
                    capabilities.PageImageableArea.ExtentWidth / e.ActualWidth,
                    capabilities.PageImageableArea.ExtentHeight / e.ActualHeight);

                //Transform the Visual to scale
                e.LayoutTransform = new ScaleTransform(scale, scale);

                //get the size of the printer page
                System.Windows.Size sz = new System.Windows.Size(
                    capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);

                //update the layout of the visual to the printer page size.
                e.Measure(sz);
                e.Arrange(
                    new System.Windows.Rect(
                        new System.Windows.Point(
                            capabilities.PageImageableArea.OriginWidth, capabilities.PageImageableArea.OriginHeight),
                        sz));

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
            // access
            if (codeSheetItems == null || codeSheetItems.Length < 1)
                return;

            // grid
            var numcol = 4;
            var numrow = 1 + ((codeSheetItems.Length - 1) / 4);
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
                    var barcodeWriter = new BarcodeWriter<Bitmap>()
                    {
                        Format = BarcodeFormat.DATA_MATRIX,
                        Renderer = new ZXing.Windows.Compatibility.BitmapRenderer()
                    };

                    //// write text and generate a 2-D barcode as a bitmap
                    bmp = barcodeWriter.Write(csi.id);
                }
                else
                {
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(csi.id, QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    bmp = qrCode.GetGraphic(20);
                }

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
            // this will bring down the overSize to adequate values
            g.Arrange(
                new Rect(new System.Windows.Point(0, 0), g.DesiredSize));
            cb.Child = g;
            cb.Measure(overSize);
            // this will bring down the overSize to adequate values
            cb.Arrange(new Rect(new System.Windows.Point(0, 0), cb.DesiredSize));

            Print(cb);
        }

        public static bool PrintRepositoryCodeSheet(
            string repoFn = null, PackageContainerListBase repoDirect = null, string title = "Asset repository")
        {
            List<CodeSheetItem> codeSheetItems = new List<CodeSheetItem>();
            try
            {
                PackageContainerListBase repo = null;

                // load the data
                if (repoFn != null)
                {
                    // from file
                    repo = PackageContainerListLocal.Load<PackageContainerListLocal>(repoFn);
                }

                if (repoDirect != null)
                {
                    // from RAM
                    repo = repoDirect;
                }

                // got something
                if (repo == null)
                    return false;

                // all assets
                foreach (var fmi in repo.FileMap)
                    if (fmi.AssetIds != null)
                        foreach (var id in fmi.AssetIds)
                        {
                            var csi = new CodeSheetItem();
                            csi.id = id;
                            csi.code = fmi.CodeType2D;
                            csi.description = fmi.Description;
                            csi.normSize = 1.0; // do not vary
                            codeSheetItems.Add(csi);
                        }

                // print
                PrintCodeSheet(codeSheetItems.ToArray(), title);

            }
            catch (Exception ex)
            {
                MessageBox.Show("" + ex.Message, "Print AASX file repository");
                return false;
            }
            return true;
        }

        public static bool PrintSingleAssetCodeSheet(
            string assetId, string description, string title = "Single asset code sheet")
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
                Log.Singleton.Error(ex, "when printing Code Sheet");
                return false;
            }
            return true;
        }



    }
}
