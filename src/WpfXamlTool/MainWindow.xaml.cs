/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xaml;
using ICSharpCode.AvalonEdit.Document;

namespace WpfXamlTool
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayInfo("This program uses AvalonEdit(NuGet), which is under MIT license.");

            // Timer for below
            System.Windows.Threading.DispatcherTimer MainTimer = new System.Windows.Threading.DispatcherTimer();
            MainTimer.Tick += MainTimer_Tick;
            MainTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            MainTimer.Start();
        }

        private void MainTimer_Tick(object sender, EventArgs e)
        {
            if (CheckBoxLiveDisplay.IsChecked == true)
                TryDisplayXaml();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonPreset0)
            {
                TryPresetFromResource("preset0.xaml");
            }

            if (sender == ButtonPreset1)
            {
                TryPresetFromResource("preset1.xaml");
            }

            if (sender == ButtonFixNames)
            {
                ApplyRegexToXaml(
                    new Regex("Name=\"(\\w+)\"", RegexOptions.Multiline),
                    (Match m) =>
                    {
                        return "";
                    });
            }

            if (sender == ButtonFixRotTrans)
            {
                ApplyRegexToXaml(
                    new Regex("<RotateTransform\\s+Angle=\"([+-01234567890.]+)\\s+([+-01234567890.]+)\\s+" +
                              "([+-01234567890.]+)\"/>", RegexOptions.Multiline),
                    (Match m) =>
                    {
                        var x = $"<RotateTransform Angle=\"{m.Groups[1].ToString()}\" " +
                            $"CenterX=\"{m.Groups[2].ToString()}\" " +
                            $"CenterY=\"{m.Groups[3].ToString()}\"/>";
                        return x;
                    });
            }

            if (sender == ButtonDisplay)
            {
                TryDisplayXaml();
            }

            if (sender == ButtonSave)
            {
                // choose filename
                var dlg = new Microsoft.Win32.SaveFileDialog();
                var fn = "" + TextBlockFilename.Text;
                if (fn == "")
                    fn = "new.xaml";
                dlg.FileName = fn;
                dlg.DefaultExt = "*.xaml";
                dlg.Filter = "XAML file (*.xaml)|*.xaml|All files (*.*)|*.*";

                // save
                try
                {
                    if (true == dlg.ShowDialog())
                    {
                        // get text
                        var txt = "" + textEdit.Text;

                        // write
                        File.WriteAllText(dlg.FileName, txt);
                    }
                }
                catch
                {
                    DisplayInfo("Error writing file.");
                }
            }

            if (sender == ButtonTagFill)
            {
                InsertText(" Tag=\"StateToFill\"");
            }

            if (sender == ButtonTagStroke)
            {
                InsertText(" Tag=\"StateToStroke\"");
            }

            if (sender == ButtonTagTwoNozzleHoriz)
            {
                InsertText("<Ellipse xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" Canvas.Left=\"-2.9\" " +
                    "Width=\"5.5\" Canvas.Top=\"10.1\" Height=\"5.5\" Name=\"circle4145\" " +
                    "StrokeThickness=\"0.37247378\" Stroke=\"#FFFF0000\" StrokeMiterLimit=\"4\" Tag=\"Nozzle#1\"/>" +
                    Environment.NewLine +
                    "<Ellipse xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" Canvas.Left=\"22.3\" " +
                    "Width=\"5.5\" Canvas.Top=\"10.2\" Height=\"5.5\" Name=\"circle41459\" " +
                    "StrokeThickness=\"0.37247378\" Stroke=\"#FFFF0000\" StrokeMiterLimit=\"4\" Tag=\"Nozzle#2\"/>");
            }
        }

        private void TryPresetFromResource(string resName)
        {
            try
            {
                using (var stream =
                    Assembly
                        .GetExecutingAssembly()
                        .GetManifestResourceStream("WpfXamlTool.Resources." + resName))
                {
                    if (stream != null)
                        using (var tr = new StreamReader(stream))
                        {
                            var fileContents = tr.ReadToEnd();
                            textEdit.Text = fileContents;
                        }
                }
            }
            catch
            {
                DisplayInfo("Error accessing resource.");
            }
        }

        private void TryDisplayXaml()
        {
            try
            {
                var x = System.Windows.Markup.XamlReader.Parse("" + textEdit.Text) as UIElement;
                BorderContent.Child = x;
            }
            catch (Exception ex)
            {
                DisplayInfo("Error displaying XAML:" + ex.ToString());
            }
        }

        private void DisplayInfo(string info)
        {
            TextBoxInfo.Text = info;
        }

        private void ApplyRegexToXaml(Regex rg, MatchEvaluator mev)
        {
            try
            {
                // access
                if (rg == null || mev == null)
                    return;

                // get text
                var txt = textEdit.Text;

                // test
                txt = rg.Replace(txt, mev);

                // redisplay
                textEdit.Text = txt;
                textEdit.Focus();
            }
            catch (Exception ex)
            {
                DisplayInfo("Error applying regex XAML:" + ex.ToString());
            }
        }

        private void InsertText(string txtToInsert)
        {
            // access
            if (txtToInsert == null)
                return;

            try
            {
                int offset = textEdit.CaretOffset;
                textEdit.Document.Insert(offset, txtToInsert);
                textEdit.Focus();
            }
            catch (Exception ex)
            {
                DisplayInfo("Error inserting text:" + ex.ToString());
            }
        }

        private void ButtonValue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // try to figure out button increment
                var btn = sender as Button;
                if (btn == null)
                    return;
                var cntstr = btn.Content as string;
                if (cntstr == null)
                    return;

                double sign = 1;
                if (cntstr.StartsWith("+"))
                {
                    sign = 1;
                    cntstr = cntstr.Substring(1);
                }
                if (cntstr.StartsWith("-"))
                {
                    sign = -1;
                    cntstr = cntstr.Substring(1);
                }

                if (!double.TryParse(cntstr, NumberStyles.Any, CultureInfo.InvariantCulture, out double step))
                    return;

                var incdec = sign * step;

                // access line data
                int offset = textEdit.CaretOffset;
                DocumentLine line = textEdit.Document.GetLineByOffset(offset);
                int caretCol = offset - line.Offset;
                var lineTxt = textEdit.Document.GetText(line.Offset, line.Length);

                // at any digit at all?
                string allowedChars = "0123456789+-.";
                if (caretCol < 0 || caretCol >= line.Length ||
                    allowedChars.IndexOf(lineTxt[caretCol]) < 0)
                    return;

                // try find start of digit sequence
                var startCol = caretCol;
                while (startCol > 0 && allowedChars.IndexOf(lineTxt[startCol - 1]) >= 0)
                    startCol--;

                // find end col
                var endCol = caretCol;
                while (endCol < line.Length - 1 && allowedChars.IndexOf(lineTxt[endCol + 1]) >= 0)
                    endCol++;

                // get string
                var origNumber = lineTxt.Substring(startCol, 1 + endCol - startCol);
                if (origNumber.Length < 1)
                    return;

                // try to figure out decimal points
                var dp = 0;
                var p = origNumber.IndexOf('.');
                if (p >= 0)
                    dp = Math.Max(0, origNumber.Length - 1 - p);

                // modify decimal places
                if (Math.Abs(incdec) < 1.0)
                    dp = Math.Max(1, dp);

                // try get actual number
                if (!double.TryParse(origNumber, NumberStyles.Any, CultureInfo.InvariantCulture, out double actual))
                    return;

                // modify actual number
                var newval = actual + incdec;

                // convert to string
                var newstr = string.Format(new NumberFormatInfo() { NumberDecimalDigits = dp }, "{0:F}", newval);

                // modify
                textEdit.Document.Replace(line.Offset + startCol, origNumber.Length, newstr);

                // restore original caret position
                textEdit.CaretOffset = offset;

                // focus again
                textEdit.Focus();

            }
            catch (Exception ex)
            {
                DisplayInfo("Error displaying XAML:" + ex.ToString());
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("myFormat") || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            // Appearantly you need to figure out if OriginalSource would have handled the Drop?
            if (!e.Handled && e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                if (files != null && files.Length > 0)
                {
                    string fn = files[0];
                    try
                    {
                        // text 
                        var txt = File.ReadAllText(fn);
                        textEdit.Text = txt;
                        textEdit.Focus();

                        // fn
                        TextBlockFilename.Text = "" + fn;
                    }
                    catch (Exception ex)
                    {
                        DisplayInfo("Error while receiving file drop to window " + ex.ToString());
                    }
                }
            }
        }
    }
}
