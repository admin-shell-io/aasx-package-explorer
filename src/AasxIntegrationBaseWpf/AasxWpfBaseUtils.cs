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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AdminShellNS;

using ExhaustiveMatch = ExhaustiveMatching.ExhaustiveMatch;

namespace AasxIntegrationBase
{
    public static class AasxWpfBaseUtils
    {
        // see: https://stackoverflow.com/questions/636383/how-can-i-find-wpf-controls-by-name-or-type

        /// <summary>
        /// Finds a Child of a given item in the visual tree.
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter.
        /// If not matching item can be found,
        /// a null parent is being returned.</returns>
        public static T FindChild<T>(DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid.
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child.
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        public static T FindChildLogicalTree<T>(DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid.
            if (parent == null || string.IsNullOrEmpty(childName))
                return null;

            // loop
            foreach (var child in LogicalTreeHelper.GetChildren(parent))
            {
                // directly found?
                var frameworkElement = child as FrameworkElement;
                // If the child's name is set for search
                if (frameworkElement != null && frameworkElement.Name == childName)
                {
                    // if the child's name is of the request name
                    return (T)child;
                }

                // try recursion
                var found = FindChildLogicalTree<T>(child as DependencyObject, childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        public static IEnumerable<T> LogicalTreeFindAllChildsWithRegexTag<T>
            (DependencyObject parent, string childTagPattern)
            where T : DependencyObject
        {
            // Confirm parent and childName are valid.
            if (parent == null)
                yield break;

            // loop
            foreach (var child in LogicalTreeHelper.GetChildren(parent))
            {
                // directly found?
                var fe = child as FrameworkElement;

                // check, if valid
                if (child is T && fe != null && fe.Tag is string tagSt && tagSt.HasContent() &&
                    (childTagPattern == null || Regex.IsMatch(tagSt, childTagPattern)))
                {
                    // if the child's name is of the request name
                    yield return (T)child;
                }

                // try recursion
                foreach (var x in LogicalTreeFindAllChildsWithRegexTag<T>(child as DependencyObject, childTagPattern))
                    yield return x;
            }
        }

        public static BitmapImage LoadBitmapImageFromPackage(AdminShellPackageEnv package, string path)
        {
            if (package == null || path == null)
                return null;

            try
            {
                var thumbStream = package.GetLocalStreamFromPackage(path);
                if (thumbStream == null)
                    return null;

                // load image
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = thumbStream;
                bi.EndInit();

                thumbStream.Close();

                // note: no closing of bi required, as BitmapImage (OnLoad!) will close it!
                // give this back
                return bi;
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            return null;
        }

        public class StoredPrintColors
        {
            public SolidColorBrush BrushError, BrushRed, BrushBlue, BrushYellow, BrushLink, BrushBlack;
        }

        public static StoredPrintColors BrightPrintColors = new StoredPrintColors()
        {
            BrushError = new SolidColorBrush(Color.FromRgb(255, 0, 0)),
            BrushRed = new SolidColorBrush(Color.FromRgb(251, 144, 55)),
            BrushBlue = new SolidColorBrush(Color.FromRgb(143, 170, 220)),
            BrushYellow = new SolidColorBrush(Color.FromRgb(248, 242, 0)),
            BrushLink = new SolidColorBrush(Color.FromRgb(46, 117, 182)),
            BrushBlack = new SolidColorBrush(Color.FromRgb(255, 255, 255))
        };

        public static StoredPrintColors DarkPrintColors = new StoredPrintColors()
        {
            BrushError = new SolidColorBrush(Color.FromRgb(255, 0, 0)),
            BrushRed = new SolidColorBrush(Color.FromRgb(192, 0, 0)),
            BrushBlue = new SolidColorBrush(Color.FromRgb(0, 109, 165)),
            BrushYellow = new SolidColorBrush(Color.FromRgb(255, 153, 0)),
            BrushLink = new SolidColorBrush(Color.FromRgb(5, 14, 187)),
            BrushBlack = new SolidColorBrush(Color.FromRgb(0, 0, 0))
        };

        public static void StoredPrintToRichTextBox(
            RichTextBox rtb, StoredPrint sp, StoredPrintColors colors, bool isExternalError = false,
            RoutedEventHandler linkClickHandler = null)
        {
            // access
            if (rtb == null || sp == null)
                return;

            // append
            TextRange tr = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
            tr.Text = "" + sp.msg;
            tr.Text += Environment.NewLine;

            if (isExternalError || sp.isError)
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, colors.BrushError);
                tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            else
            {
                switch (sp.color)
                {
                    default:
                        throw ExhaustiveMatch.Failed(sp.color);
                    case StoredPrint.Color.Red:
                        tr.ApplyPropertyValue(TextElement.ForegroundProperty, colors.BrushRed);
                        break;
                    case StoredPrint.Color.Blue:
                        tr.ApplyPropertyValue(TextElement.ForegroundProperty, colors.BrushBlue);
                        break;
                    case StoredPrint.Color.Yellow:
                        tr.ApplyPropertyValue(TextElement.ForegroundProperty, colors.BrushYellow);
                        break;
                    case StoredPrint.Color.Black:
                        tr.ApplyPropertyValue(TextElement.ForegroundProperty, colors.BrushBlack);
                        break;
                }
            }

            if (sp.linkTxt != null && sp.linkUri != null)
            {
                // see: https://stackoverflow.com/questions/762271/
                // clicking-hyperlinks-in-a-richtextbox-without-holding-down-ctrl-wpf
                // see: https://stackoverflow.com/questions/9279061/dynamically-adding-hyperlinks-to-a-richtextbox

                // try modify existing
                tr.Text = tr.Text.TrimEnd('\r', '\n') + " ";

                // make another append!
                var link = new Hyperlink(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
                link.IsEnabled = true;

                try
                {
                    link.Inlines.Add("" + sp.linkTxt + Environment.NewLine);
                    link.NavigateUri = new Uri(sp.linkUri);
                    if (linkClickHandler != null)
                        link.Click += linkClickHandler;
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }
        }

        public static void StoredPrintToFloqDoc(
            FlowDocument doc, StoredPrint sp, StoredPrintColors colors, bool isExternalError = false,
            RoutedEventHandler linkClickHandler = null)
        {
            // access
            if (doc == null || sp == null)
                return;

            // simple first
            var pr = new Paragraph();
            pr.Inlines.Add(sp.msg);

            if (isExternalError || sp.isError)
            {
                pr.Foreground = colors.BrushError;
                pr.FontWeight = FontWeights.Bold;
            }
            else
            {
                switch (sp.color)
                {
                    default:
                        throw ExhaustiveMatch.Failed(sp.color);
                    case StoredPrint.Color.Red:
                        pr.Foreground = colors.BrushRed;
                        break;
                    case StoredPrint.Color.Blue:
                        pr.Foreground = colors.BrushBlue;
                        break;
                    case StoredPrint.Color.Yellow:
                        pr.Foreground = colors.BrushYellow;
                        break;
                    case StoredPrint.Color.Black:
                        pr.Foreground = colors.BrushBlack;
                        break;
                }
            }

            if (sp.linkTxt != null && sp.linkUri != null)
            {
                // see: https://stackoverflow.com/questions/762271/
                // clicking-hyperlinks-in-a-richtextbox-without-holding-down-ctrl-wpf
                // see: https://stackoverflow.com/questions/9279061/dynamically-adding-hyperlinks-to-a-richtextbox

                // make another append!
                var link = new Hyperlink();
                link.IsEnabled = true;

                try
                {
                    link.Inlines.Add("" + sp.linkTxt + Environment.NewLine);
                    link.NavigateUri = new Uri(sp.linkUri);
                    if (linkClickHandler != null)
                        link.Click += linkClickHandler;
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

                pr.Inlines.Add(link);
            }

            doc.Blocks.Add(pr);

#if __disabled
            // append
            TextRange tr = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
            tr.Text = "" + sp.msg;
            tr.Text += Environment.NewLine;

            if (isExternalError || sp.isError)
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, colors.BrushError);
                tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            else
            {
                switch (sp.color)
                {
                    default:
                        throw ExhaustiveMatch.Failed(sp.color);
                    case StoredPrint.Color.Red:
                        tr.ApplyPropertyValue(TextElement.ForegroundProperty, colors.BrushRed);
                        break;
                    case StoredPrint.Color.Blue:
                        tr.ApplyPropertyValue(TextElement.ForegroundProperty, colors.BrushBlue);
                        break;
                    case StoredPrint.Color.Yellow:
                        tr.ApplyPropertyValue(TextElement.ForegroundProperty, colors.BrushYellow);
                        break;
                    case StoredPrint.Color.Black:
                        tr.ApplyPropertyValue(TextElement.ForegroundProperty, colors.BrushBlack);
                        break;
                }
            }

            if (sp.linkTxt != null && sp.linkUri != null)
            {
                // see: https://stackoverflow.com/questions/762271/
                // clicking-hyperlinks-in-a-richtextbox-without-holding-down-ctrl-wpf
                // see: https://stackoverflow.com/questions/9279061/dynamically-adding-hyperlinks-to-a-richtextbox

                // try modify existing
                tr.Text = tr.Text.TrimEnd('\r', '\n') + " ";

                // make another append!
                var link = new Hyperlink(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
                link.IsEnabled = true;

                try
                {
                    link.Inlines.Add("" + sp.linkTxt + Environment.NewLine);
                    link.NavigateUri = new Uri(sp.linkUri);
                    if (linkClickHandler != null)
                        link.Click += linkClickHandler;
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }
#endif
        }

    }
}
