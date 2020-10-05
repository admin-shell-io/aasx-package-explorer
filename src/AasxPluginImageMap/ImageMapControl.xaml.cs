using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxPredefinedConcepts;
using AdminShellNS;
using Newtonsoft.Json;

// ReSharper disable InconsistentlySynchronizedField
// checks and everything looks fine .. maybe .Count() is already treated as synchronized action?

namespace AasxPluginImageMap
{
    /// <summary>
    /// Interaktionslogik für ImageMapControl.xaml
    /// </summary>
    public partial class ImageMapControl : UserControl
    {
        #region Members
        //=============

        private LogInstance Log = new LogInstance();
        private AdminShellPackageEnv thePackage = null;
        private AdminShell.Submodel theSubmodel = null;
        private ImageMapOptions theOptions = null;
        private PluginEventStack theEventStack = null;

        public List<double> clickedCoordinates = new List<double>();

        #endregion

        #region View Model
        //================

        private ViewModel theViewModel = new ViewModel();

        public class ViewModel : AasxIntegrationBase.WpfViewModelBase
        {
        }

        #endregion
        #region Init of component
        //=======================

        public ImageMapControl()
        {
            InitializeComponent();

            // Timer for loading
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
        }

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel theSubmodel,
            ImageMapOptions theOptions,
            PluginEventStack eventStack)
        {
            this.Log = log;
            this.thePackage = thePackage;
            this.theSubmodel = theSubmodel;
            this.theOptions = theOptions;
            this.theEventStack = eventStack;
        }

        public static ImageMapControl FillWithWpfControls(
            LogInstance log,
            object opackage, object osm,
            ImageMapOptions options,
            PluginEventStack eventStack,
            object masterDockPanel)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as AdminShell.Submodel;
            var master = masterDockPanel as DockPanel;
            if (package == null || sm == null || master == null)
            {
                return null;
            }

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // create TOP control
            var imgMapCntl = new ImageMapControl();
            imgMapCntl.Start(log, package, sm, options, eventStack);
            master.Children.Add(imgMapCntl);

            // return shelf
            return imgMapCntl;
        }

        #endregion

        #region Business Logic
        //====================

        private void FindFileAndDisplay()
        {
            // access
            if (this.theSubmodel?.submodelElements == null || this.thePackage == null)
                return;

            // file?
            var fe = this.theSubmodel.submodelElements.FindFirstSemanticIdAs<AdminShell.File>(
                AasxPredefinedConcepts.ImageMap.Static.CD_ImageFile.GetReference(),
                AdminShellV20.Key.MatchMode.Relaxed);
            if (fe == null)
                return;

            // bitmap data
            var bitmapdata = thePackage.GetByteArrayFromUriOrLocalPackage(fe.value);
            if (bitmapdata == null)
                return;

            // set?
            var bi = (BitmapSource)new ImageSourceConverter().ConvertFrom(bitmapdata);
            this.ImageContent.Source = bi;
        }

        private string ClickedCoordinatesToString()
        {
            var sum = "";
            foreach (var d in this.clickedCoordinates)
            {
                if (sum != "")
                    sum += ", ";
                sum += String.Format(CultureInfo.InvariantCulture, "{0:F1}", d);
            }
            return sum;
        }

        private void DisplayClickedCoordinates()
        {
            var sum = ClickedCoordinatesToString();
            this.LabelInfo.Content = $"Clicked coordinates = [ {sum} ]";
        }

        private double[] ParseDoubles(string input)
        {
            try
            {
                var x = JsonConvert.DeserializeObject<List<double>>(input);
                return x.ToArray();
            }
            catch {
                return null;
            }
        }

        public string[] RegionColors = new[] { "#ff0000", "#00ff00", "#0000ff", "#ffff00", "#ff00ff", "#00ffff" };

        private Tuple<Brush, Brush, Brush> DetermineColors (int index, bool forceTransparent, byte aFill, byte aStroke)
        {
            var bc = (Color)ColorConverter.ConvertFromString(RegionColors[index % 6]);
            if (bc == null)
                bc = Colors.White;
            var fillColor = Color.FromArgb(aFill, bc.R, bc.G, bc.B);
            var strokeColor = Color.FromArgb(aStroke, bc.R, bc.G, bc.B);
            var textColor = Colors.White;
            if (forceTransparent)
            {
                fillColor = Colors.Transparent;
                strokeColor = Colors.Transparent;
                textColor = strokeColor;
            }
            return new Tuple<Brush, Brush, Brush>(
                new SolidColorBrush(fillColor), new SolidColorBrush(strokeColor),
                new SolidColorBrush(textColor));
        }

        private void AddRectangle(
            Canvas canvas, object tag, double x0, double y0, double w, double h,
            Brush fill, Brush stroke)
        {
            // access
            if (canvas == null)
                return;

            var x = new Rectangle();
            x.Fill = fill;
            x.Stroke = stroke;
            x.StrokeThickness = 1.0;
            x.Width = w;
            x.Height = h;
            x.Tag = tag;
            Canvas.SetLeft(x, x0);
            Canvas.SetTop(x, y0);
            canvas.Children.Add(x);
        }

        private void AddCircle(
            Canvas canvas, object tag, double x0, double y0, double wh,
            Brush fill, Brush stroke)
        {
            // access
            if (canvas == null)
                return;

            var x = new Ellipse();
            x.Fill = fill;
            x.Stroke = stroke;
            x.StrokeThickness = 1.0;
            x.Width = wh;
            x.Height = wh;
            x.Tag = tag;
            Canvas.SetLeft(x, x0);
            Canvas.SetTop(x, y0);
            canvas.Children.Add(x);
        }

        private void AddPolygon(
            Canvas canvas, object tag, double[] points,
            Brush fill, Brush stroke)
        {
            // access
            if (canvas == null || points == null || points.Length < 6)
                return;

            // points
            var pc = new PointCollection();
            for (int i = 0; (2 * i) < points.Length; i++)
                pc.Add(new Point(points[2 * i], points[2 * i + 1]));

            var x = new Polygon();
            x.Fill = fill;
            x.Stroke = stroke;
            x.StrokeThickness = 1.0;
            x.Points = pc;
            x.Tag = tag;
            Canvas.SetLeft(x, 0);
            Canvas.SetTop(x, 0);
            canvas.Children.Add(x);
        }

        private void AddLabel(
            Canvas canvas, object tag, double x0, double y0, double w, double h,
            Brush fg, string text)
        {
            // access
            if (canvas == null)
                return;

            var lb = new Label();
            lb.Width = w;
            lb.Height = h;
            lb.HorizontalContentAlignment = HorizontalAlignment.Center;
            lb.VerticalContentAlignment = VerticalAlignment.Center;
            lb.Content = "" + text;
            lb.FontSize = 8.0;
            lb.Foreground = fg;
            lb.Tag = tag;
            Canvas.SetLeft(lb, x0);
            Canvas.SetTop(lb, y0);
            canvas.Children.Add(lb);
        }

        private void FindRegionsAndDisplay(bool forceTransparent)
        {
            // access
            if (this.theSubmodel?.submodelElements == null || this.thePackage == null)
                return;

            // clear canvas
            this.CanvasContent.Children.Clear();

            // entities
            int index = -1;
            foreach (var ent in this.theSubmodel.submodelElements.FindAllSemanticIdAs<AdminShell.Entity>(
                AasxPredefinedConcepts.ImageMap.Static.CD_EntityOfImageMap.GetReference(),
                AdminShellV20.Key.MatchMode.Relaxed))
            {
                // access
                if (ent?.statements == null)
                    continue;

                // find all regions known
                foreach (var prect in ent.statements.FindAllSemanticIdAs<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_RegionRect.GetReference(),
                    AdminShellV20.Key.MatchMode.Relaxed))
                {
                    // access
                    if (!(prect?.value.HasContent() == true))
                        continue;
                    var pts = ParseDoubles(prect.value);
                    if (pts == null || pts.Length != 4)
                        continue;
                    if (pts[2] < pts[0] || pts[3] < pts[1])
                        continue;
                    index++;

                    // colors
                    var cols = DetermineColors(index, forceTransparent, 0x30, 0xff);

                    // construct widget
                    AddRectangle(this.CanvasContent, prect, 
                        pts[0], pts[1], pts[2] - pts[0], pts[3] - pts[1], 
                        cols.Item1, cols.Item2);

                    // construct text
                    if (!forceTransparent)
                        AddLabel(this.CanvasContent, prect,
                            pts[0], pts[1], pts[2] - pts[0], pts[3] - pts[1],
                            cols.Item3, "" + prect.idShort);
                }

                foreach (var pcirc in ent.statements.FindAllSemanticIdAs<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_RegionCircle.GetReference(),
                    AdminShellV20.Key.MatchMode.Relaxed))
                {
                    // access
                    if (!(pcirc?.value.HasContent() == true))
                        continue;
                    var pts = ParseDoubles(pcirc.value);
                    if (pts == null || pts.Length != 3)
                        continue;
                    if (pts[2] <= 0.0)
                        continue;
                    index++;

                    // colors
                    var cols = DetermineColors(index, forceTransparent, 0x30, 0xff);

                    // construct widget
                    AddCircle(this.CanvasContent, pcirc,
                        pts[0] - pts[2], pts[1] - pts[2], 2 * pts[2],
                        cols.Item1, cols.Item2);

                    // construct text
                    if (!forceTransparent)
                        AddLabel(this.CanvasContent, pcirc,
                            pts[0] - pts[2], pts[1] - pts[2], 2 * pts[2], 2 * pts[2],
                            cols.Item3, "" + pcirc.idShort);
                }

                foreach (var ppoly in ent.statements.FindAllSemanticIdAs<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_RegionPolygon.GetReference(),
                    AdminShellV20.Key.MatchMode.Relaxed))
                {
                    // access
                    if (!(ppoly?.value.HasContent() == true))
                        continue;
                    var pts = ParseDoubles(ppoly.value);
                    if (pts == null || pts.Length < 6 || pts.Length % 2 == 1)
                        continue;
                    index++;

                    // colors
                    var cols = DetermineColors(index, forceTransparent, 0x30, 0xff);

                    // construct widget
                    AddPolygon(this.CanvasContent, ppoly,
                        pts,
                        cols.Item1, cols.Item2);
                }
            }
        }

        #endregion

        #region WPF handling
        //==================

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // change title
            var tit = "" + this.theSubmodel?.idShort;
            if (!tit.HasContent())
                tit = "Image Map";
            this.LabelPanel.Content = tit;

            // display file
            FindFileAndDisplay();

            // display regions
            FindRegionsAndDisplay(!(this.CheckBoxShowRegions.IsChecked == true));
        }

        #endregion

        private void CanvasContent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // re - display regions
            FindRegionsAndDisplay(!(this.CheckBoxShowRegions.IsChecked == true));
        }

        private void CheckBoxShowRegions_Checked(object sender, RoutedEventArgs e)
        {
            // re - display regions
            FindRegionsAndDisplay(!(this.CheckBoxShowRegions.IsChecked == true));
        }

        private void HandleMouseClickOnImage()
        {
            // get actual coordinate relative to image & zoom of image!
            Point p = Mouse.GetPosition(this.ImageContent);

            // replace or add coordinates to the stored ones
            if (!Keyboard.IsKeyDown(Key.LeftShift))
                this.clickedCoordinates.Clear();

            // add
            this.clickedCoordinates.Add(p.X);
            this.clickedCoordinates.Add(p.Y);

            // display
            DisplayClickedCoordinates();
        }

        private void ImageContent_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleMouseClickOnImage();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var sum = ClickedCoordinatesToString();
            sum = "[ " + sum + " ]";
            Clipboard.SetText("" + sum);
            this.LabelInfo.Content = "Clipboard set.";
        }

        private void CanvasContent_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2 && e.Source is FrameworkElement fe)
            {
                // check
                if (fe.Tag is AdminShell.Property prop &&
                    prop.parent is AdminShell.Entity ent)
                {
                    ;
                }

                // handle double click in any case
                return;
            }

            // now, take as event for underlying image
            HandleMouseClickOnImage();
        }
    }
}
