using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AdminShellNS;

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

        public static int CountHeadingSpaces(string line)
        {
            if (line == null)
                return 0;
            int j;
            for (j = 0; j < line.Length; j++)
                if (!Char.IsWhiteSpace(line[j]))
                    break;
            return j;
        }

        /// <summary>
        /// Used to re-reformat a C# here string, which is multiline string introduced by @" ... ";
        /// </summary>
        public static string[] CleanHereStringToArray(string here)
        {
            if (here == null)
                return null;

            // convert all weird breaks to pure new lines
            here = here.Replace("\r\n", "\n");
            here = here.Replace("\n\r", "\n");

            // convert all tabs to spaces
            here = here.Replace("\t", "    ");

            // split these
            var lines = new List<string>(here.Split('\n'));
            if (lines.Count < 1)
                return lines.ToArray();

            // the first line could be special
            string firstLine = null;
            if (lines[0].Trim() != "")
            {
                firstLine = lines[0].Trim();
                lines.RemoveAt(0);
            }

            // detect an constant amount of heading spaces
            var headSpaces = int.MaxValue;
            foreach (var line in lines)
                if (line.Trim() != "")
                    headSpaces = Math.Min(headSpaces, CountHeadingSpaces(line));

            // multi line trim possible?
            if (headSpaces != int.MaxValue && headSpaces > 0)
                for (int i = 0; i < lines.Count; i++)
                    if (lines[i].Length > headSpaces)
                        lines[i] = lines[i].Substring(headSpaces);

            // re-compose again
            if (firstLine != null)
                lines.Insert(0, firstLine);

            // return
            return lines.ToArray();
        }

        /// <summary>
        /// Used to re-reformat a C# here string, which is multiline string introduced by @" ... ";
        /// </summary>
        public static string CleanHereStringWithNewlines(string here, string nl = null)
        {
            if (nl == null)
                nl = Environment.NewLine;
            var lines = CleanHereStringToArray(here);
            if (lines == null)
                return null;
            return String.Join(nl, lines);
        }

        public static BitmapImage LoadBitmapImageFromPackage(AdminShellPackageEnv package, string path)
        {
            if (package == null || path == null)
                return null;

            // ReSharper disable EmptyGeneralCatchClause
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

                // note: no closing required!
                // give this back
                return bi;
            }
            catch { }
            // ReSharper enable EmptyGeneralCatchClause

            return null;
        }

        public class StoredPrintColors
        {
            public SolidColorBrush BrushError, BrushRed, BrushBlue, BrushLink;
        }

        public static StoredPrintColors BrightPrintColors = new StoredPrintColors()
        {
            BrushError = new SolidColorBrush(Color.FromRgb(255, 0, 0)),
            BrushRed = new SolidColorBrush(Color.FromRgb(251, 144, 55)),
            BrushBlue = new SolidColorBrush(Color.FromRgb(143, 170, 220)),
            BrushLink = new SolidColorBrush(Color.FromRgb(46, 117, 182))
        };

        public static StoredPrintColors DarkPrintColors = new StoredPrintColors()
        {
            BrushError = new SolidColorBrush(Color.FromRgb(255, 0, 0)),
            BrushRed = new SolidColorBrush(Color.FromRgb(192, 0, 0)),
            BrushBlue = new SolidColorBrush(Color.FromRgb(0, 109, 165)),
            BrushLink = new SolidColorBrush(Color.FromRgb(5, 14, 187))
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
                if (sp.color == StoredPrint.ColorRed)
                {
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, colors.BrushRed);
                }

                if (sp.color == StoredPrint.ColorBlue)
                {
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, colors.BrushBlue);
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

                // ReSharper disable EmptyGeneralCatchClause
                try
                {
                    link.Inlines.Add("" + sp.linkTxt + Environment.NewLine);
                    link.NavigateUri = new Uri(sp.linkUri);
                    if (linkClickHandler != null)
                        link.Click += linkClickHandler;
                }
                catch { }
                // ReSharper enable EmptyGeneralCatchClause
            }
        }
    }
}
