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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;

namespace WpfMtpControl
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class UIElementHelper
    {
        public static UIElement cloneElement(UIElement orig)
        {
            if (orig == null)
                return (null);

            string s = XamlWriter.Save(orig);
            StringReader stringReader = new StringReader(s);
            XmlReader xmlReader = XmlTextReader.Create(stringReader, new XmlReaderSettings());
            return (UIElement)XamlReader.Load(xmlReader);

        }

        public static Label FindLabelWithText(System.Windows.DependencyObject parent, string textToFind)
        {
            // trivial
            if (parent == null)
                return null;

            // recurse visual tree
            int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                // deep inspect?
                var childLabel = child as Label;
                if (childLabel != null && childLabel.Content != null)
                {
                    var childLabelText = childLabel.Content as string;
                    if (childLabelText != null && childLabelText.Trim() == textToFind.Trim())
                        return childLabel;
                }

                // recurse?
                var childFound = FindLabelWithText(child, textToFind);
                if (childFound != null)
                    return childFound;
            }

            // no
            return null;
        }

        public static void ApplyMultiLabel(FrameworkElement contentObject, Tuple<string, string>[] labelTexts = null)
        {
            // trivial
            if (contentObject == null)
                return;

            // can name labels?
            if (labelTexts != null)
                foreach (var lt in labelTexts)
                {
                    var tagLebel = UIElementHelper.FindLabelWithText(contentObject, lt.Item1);
                    if (tagLebel != null)
                        tagLebel.Content = "" + lt.Item2;
                }
        }

        private static void FindNozzlesViaTagsIntern(
            System.Windows.DependencyObject parent, Dictionary<int, Point> namedNozzles,
            string matchHead, bool extractShapes = false)
        {
            // trivial
            if (parent == null || namedNozzles == null)
                return;

            var toExtract = new List<UIElement>();

            // recurse visual tree
            int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                // deep inspect?
                var childEllipse = child as Ellipse;
                if (childEllipse != null)
                {
                    var childEllipseTagText = childEllipse.Tag as string;
                    if (childEllipseTagText != null && childEllipseTagText.Trim() != "")
                    {
                        var m = Regex.Match(childEllipseTagText, matchHead + @"#(\d+)");
                        if (m.Success)
                        {
                            var nid = Convert.ToInt32(m.Groups[1].ToString());

                            var x = Canvas.GetLeft(childEllipse) + childEllipse.Width / 2;
                            var y = Canvas.GetTop(childEllipse) + childEllipse.Height / 2;
                            if (nid > 0 && nid < 99)
                                namedNozzles[nid] = new Point(x, y);

                            // extract?
                            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                            if (extractShapes && child is UIElement)
                                toExtract.Add(child as UIElement);
                        }
                    }
                }

                // recurse?
                FindNozzlesViaTagsIntern(child, namedNozzles, matchHead, extractShapes);
            }

            if (extractShapes && parent is Canvas)
                foreach (var te in toExtract)
                    (parent as Canvas).Children.Remove(te);

        }

        public static Point[] FindNozzlesViaTags(
            System.Windows.DependencyObject parent, string matchHead, bool extractShapes = false)
        {
            // find named nozzles
            var namedNozzles = new Dictionary<int, Point>();
            UIElementHelper.FindNozzlesViaTagsIntern(parent, namedNozzles, matchHead, extractShapes: extractShapes);

            // integrity check
            for (int i = 0; i < namedNozzles.Count; i++)
                if (!namedNozzles.ContainsKey(1 + i))
                {
                    namedNozzles = null;
                    break;
                }

            // still there
            Point[] res = null;
            if (namedNozzles != null)
            {
                res = new Point[namedNozzles.Count];
                for (int i = 0; i < namedNozzles.Count; i++)
                    res[i] = namedNozzles[1 + i];
            }

            return res;
        }

        /// <summary>
        /// Computes center of gravity, returns null in case of any error.
        /// </summary>
        public static Nullable<Point> ComputeCOG(Point[] pts)
        {
            if (pts == null || pts.Length < 1)
                return null;

            var sum = pts[0];
            for (int i = 1; i < pts.Length; i++)
                sum.Offset(pts[i].X, pts[i].Y);
            sum.X /= 1.0 * pts.Length;
            sum.Y /= 1.0 * pts.Length;

            return sum;
        }

        /// <summary>
        /// Computes the maximum distance of each point to a given center point. Returns null in case of any error.
        /// </summary>
        public static Nullable<double> ComputeRadiusForCenterPointer(Point[] pts, Point cog)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (pts == null || pts.Length < 1 || cog == null)
                return null;

            var r = 0.0;
            for (int i = pts.Length - 1; i >= 0; i--)
            {
                var xd = pts[i].X - cog.X;
                var yd = pts[i].Y - cog.Y;
                var d = Math.Sqrt(xd * xd + yd * yd);
                if (d > r)
                    r = d;
            }

            return r;
        }

        public static Point[] RescalePointsByRatioOfFEs(
            FrameworkElement original, FrameworkElement scaled, Point[] ptsOriginal)
        {
            if (original == null || scaled == null || ptsOriginal == null || ptsOriginal.Length < 1)
                return null;
            var scaleFac = Math.Min(scaled.Width / original.Width, scaled.Height / original.Height);
            var ptsScaled = new Point[ptsOriginal.Length];
            for (int i = 0; i < ptsOriginal.Length; i++)
            {
                var x = (ptsOriginal[i].X - original.Width / 2) * scaleFac;
                var y = (ptsOriginal[i].Y - original.Height / 2) * scaleFac;
                ptsScaled[i] = new Point(x, y);
            }
            return ptsScaled;
        }

        public class Transformation2D
        {
            public double Scale, Rot, OfsX, OfsY;
            public Transformation2D() { }
            public Transformation2D(double Scale, double Rot, double OfsX, double OfsY)
            {
                this.Scale = Scale;
                this.Rot = Rot;
                this.OfsX = OfsX;
                this.OfsY = OfsY;
            }
        }

        public static Point[] ApplyTransformation(Transformation2D trans, Point center, Point[] pts)
        {
            // setup
            if (pts == null || pts.Length < 1)
                return null;
            var res = new Point[pts.Length];

            // 1 for 1
            for (int i = 0; i < pts.Length; i++)
            {
                // around center ..
                var x = pts[i].X - center.X;
                var y = pts[i].Y - center.Y;

                // scale
                x = x * trans.Scale;
                y = y * trans.Scale;

                // rotate (mathematically positive!)
                var radian = -trans.Rot * (Math.PI / 180);
                double cosTheta = Math.Cos(radian);
                double sinTheta = Math.Sin(radian);
                var nx = cosTheta * x - sinTheta * y;
                var ny = sinTheta * x + cosTheta * y;

                // move
                nx += trans.OfsX;
                ny += trans.OfsY;

                // store
                res[i] = new Point(nx, ny);
            }

            // ok
            return res;
        }

        public static Nullable<double> CumulatedErrorToFieldOfPoints(Point[] pts, Point[] field)
        {
            // setup
            if (pts == null || field == null || pts.Length < 1 || field.Length < 1)
                return null;
            double res = 0;

            // we're striking through the field of points
            var strike = new bool[field.Length];

            // pts are leading
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int pi = 0; pi < pts.Length; pi++)
            {
                // find neareast neighbour
                var nd = Double.MaxValue;
                var nfi = -1;
                for (int fi = 0; fi < field.Length; fi++)
                    if (!strike[fi])
                    {
                        var dx = field[fi].X - pts[pi].X;
                        var dy = field[fi].Y - pts[pi].Y;
                        var d = Math.Sqrt(dx * dx + dy * dy);
                        if (d < nd)
                        {
                            nd = d;
                            nfi = fi;
                        }
                    }

                // there is no room for uups!
                if (nfi < 0)
                    return null;

                // compute
                res += nd;
                strike[nfi] = true;
            }

            // ok
            return res;
        }

        public static Transformation2D FindBestFitForFieldOfPoints(
                    Point[] pts, Point[] field,
                    Transformation2D start,
                    double rangeScale, double rangeRot, double rangeXY,
                    int steps, int iterations)
        {
            // setup
            if (pts == null || field == null || pts.Length < 1 || field.Length < 1 || start == null || iterations < 0)
                return null;

            // systematicall apply disturbances
            // on the hierarchy: scale, rot (in degrees), ofsX, ofsY

            var bestTrans = start;
            double bestError = Double.MaxValue;
            var be = CumulatedErrorToFieldOfPoints(pts, field);
            if (be != null)
                bestError = be.Value;

            var center = ComputeCOG(pts);
            if (center == null)
                return null;

            for (int iScale = 0; iScale <= steps; iScale++)
                for (int iRot = 0; iRot <= steps; iRot++)
                    for (int iY = 0; iY <= steps; iY++)
                        for (int iX = 0; iX <= steps; iX++)
                        {
                            // current point
                            var currTrans = new Transformation2D(
                                start.Scale - rangeScale + (2 * rangeScale * iScale / steps),
                                start.Rot - rangeRot + (2 * rangeRot * iRot / steps),
                                start.OfsX - rangeXY + (2 * rangeXY * iX / steps),
                                start.OfsY - rangeXY + (2 * rangeXY * iY / steps));

                            // some parts of the vectorroom are "taboo"
                            if (currTrans.Scale <= 0.000)
                                continue;

                            // transform
                            var currPts = ApplyTransformation(currTrans, center.Value, pts);

                            // evaluate
                            var error = CumulatedErrorToFieldOfPoints(currPts, field);
                            if (error != null && error.Value < bestError)
                            {
                                bestError = error.Value;
                                bestTrans = currTrans;
                            }
                        }

            // go into "recursion"
            var rm = 1.0 / steps;
            // ReSharper disable once UnusedVariable
            var betterTrans = FindBestFitForFieldOfPoints(pts, field, bestTrans,
                                rm * rangeScale, rm * rangeRot, rm * rangeXY, steps, iterations - 1);

            // result
            return bestTrans;
        }

        public class FontSettings
        {
            public FontFamily FontFamily;
            public FontStyle Style;
            public FontWeight Weight;
            public FontStretch Stretch;
            public double EmSize;

            public FontSettings() { }

            public FontSettings(
                FontFamily fontFamily, FontStyle style, FontWeight weight, FontStretch stretch, double EmSize)
            {
                this.FontFamily = fontFamily;
                this.Style = style;
                this.Weight = weight;
                this.Stretch = stretch;
                this.EmSize = EmSize;
            }

            public Size MeasureString(string candidate)
            {
                var formattedText = new FormattedText(
                    candidate,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(this.FontFamily, this.Style, this.Weight, this.Stretch),
                    this.EmSize,
                    Brushes.Black,
                    new NumberSubstitution(),
                    TextFormattingMode.Display,
                    /*
                     * (mristin, 2020-12-04)
                     * The following argument, pixelsPerDip, is a complicated one. We set it here to value 1.0
                     * since we are simply upgrading NET Framework from 4.6.1 to 4.7.2.
                     *
                     * See https://github.com/Microsoft/WPF-Samples/tree/master/PerMonitorDPI,
                     * https://stackoverflow.com/questions/40277388 and
                     * https://social.msdn.microsoft.com/Forums/vstudio/en-US/ef99bb56-df57-411a-a158-cad1eaa63850
                     */
                    1.0
                    );

                return new Size(formattedText.Width, formattedText.Height);
            }
        }

        public static TextBlock CreateStickyLabel(FontSettings fontSettings, string text, double padding = 2.0)
        {
            var size = fontSettings.MeasureString(text);
            // TODO (MICHA, 2020-10-04): check if font is set correctly ..
            // TODO (MICHA, 2020-10-04): seems, that for Textblock the alignement DOES NOT WORK!
            var tb = new TextBlock();
            tb.Height = size.Height + 2 * padding;
            tb.Width = size.Width + 2 * padding;
            tb.HorizontalAlignment = HorizontalAlignment.Center;
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.Text = text;
            return tb;
        }

        public enum DrawToCanvasAlignment { North = 0, East = 1, South = 2, West = 3, Centered = 4 };

        public static DrawToCanvasAlignment TranslateRotToAlignemnt(double rot)
        {
            // modulo
            var pi = Math.PI;
            rot = rot % (2 * pi);

            // 4 segments for now
            if (rot > -0.25 * pi && rot <= 0.25 * pi)
                return DrawToCanvasAlignment.North;
            if (rot > 0.25 * pi && rot <= 0.75 * pi)
                return DrawToCanvasAlignment.East;
            if (rot > 0.75 * pi && rot <= 1.25 * pi)
                return DrawToCanvasAlignment.South;
            if (rot > 1.25 * pi && rot <= 1.75 * pi)
                return DrawToCanvasAlignment.North;
            // ups
            return DrawToCanvasAlignment.Centered;
        }

        public static void DrawToCanvasAtPositionAligned(
            Canvas canvas, double x, double y, DrawToCanvasAlignment alignment, FrameworkElement fe)
        {
            if (canvas == null || fe == null)
                return;
            canvas.Children.Add(fe);

            if (alignment == DrawToCanvasAlignment.North)
            {
                Canvas.SetLeft(fe, x - fe.Width / 2);
                Canvas.SetTop(fe, y - fe.Height);
            }

            if (alignment == DrawToCanvasAlignment.South)
            {
                Canvas.SetLeft(fe, x - fe.Width / 2);
                Canvas.SetTop(fe, y);
            }

            if (alignment == DrawToCanvasAlignment.West)
            {
                Canvas.SetLeft(fe, x - fe.Width);
                Canvas.SetTop(fe, y - fe.Height / 2);
            }

            if (alignment == DrawToCanvasAlignment.East)
            {
                Canvas.SetLeft(fe, x);
                Canvas.SetTop(fe, y - fe.Height / 2);
            }

            if (alignment == DrawToCanvasAlignment.Centered)
            {
                Canvas.SetLeft(fe, x - fe.Width / 2);
                Canvas.SetTop(fe, y - fe.Height / 2);
            }
        }

    }
}
