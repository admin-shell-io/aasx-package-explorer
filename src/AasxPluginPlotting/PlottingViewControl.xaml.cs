/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.ConceptModel;
using AdminShellNS;
using ScottPlot;

namespace AasxPluginPlotting
{
    public partial class PlottingViewControl : UserControl
    {
        public class PlotBuffer
        {
            public const int BufferSize = 512;

            public PlottableSignal Plottable;
            public double[] BufferData = new double[BufferSize];
            public int BufferLevel = 0;

            public void Push(double data)
            {
                if (BufferLevel < BufferSize)
                {
                    // simply add
                    BufferData[BufferLevel] = data;
                    if (Plottable != null)
                        Plottable.maxRenderIndex = BufferLevel;
                    BufferLevel++;
                }
                else
                {
                    // brute shift
                    Array.Copy(BufferData, 1, BufferData, 0, BufferSize - 1);
                    BufferData[BufferSize - 1] = data;
                    if (Plottable != null)
                        Plottable.maxRenderIndex = BufferSize - 1;
                }
            }
        }

        public class PlotArguments
        {
            // ReSharper disable UnassignedField.Global
            public string grp;
            public string fmt;
            public double? xmin, ymin, xmax, ymax;
            // ReSharper enable UnassignedField.Global
        }

        // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
        public class PlotItem : /* IEquatable<DetailItem>, */ INotifyPropertyChanged, IComparable<PlotItem>
        {
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(propertyName));
            }

            public AdminShell.SubmodelElement SME = null;
            public string ArgsStr = "";
            public PlotArguments Args = null;

            public PlotBuffer Buffer;

            public int Group
            {
                get
                {
                    // have default group to be the last
                    if (Args?.grp == null)
                        return 9999;
                    if (int.TryParse(Args.grp, out int i))
                        return i;
                    return 9999;
                }
            }

            private string _path = "";
            public string DisplayPath
            {
                get { return _path; }
                set { _path = value; OnPropertyChanged("DisplayPath"); }
            }

            private string _value = null;
            public string DisplayValue
            {
                get { return _value; }
                set { _value = value; OnPropertyChanged("DisplayValue"); }
            }

            private AdminShell.Description _description = new AdminShell.Description();
            public AdminShell.Description Description
            {
                get { return _description; }
                set { _description = value; OnPropertyChanged("DisplayDescription"); }
            }

            private string _displayDescription = "";
            public string DisplayDescription
            {
                get { return _displayDescription; }
                set { _displayDescription = value; OnPropertyChanged("DisplayDescription"); }
            }

            public PlotItem() { }

            public PlotItem(AdminShell.SubmodelElement sme, string args,
                string path, string value, AdminShell.Description description, string lang)
            {
                SME = sme;
                ArgsStr = args;
                _path = path;
                _value = value;
                _description = description;
                _displayDescription = description?.GetDefaultStr(lang);
                TryParseArgs();
            }

            private void TryParseArgs()
            {
                try
                {
                    Args = null;
                    if (!ArgsStr.HasContent())
                        return;
                    Args = Newtonsoft.Json.JsonConvert.DeserializeObject<PlotArguments>(ArgsStr);
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                }
            }

            public int CompareTo(PlotItem other)
            {
                if (other == null)
                    return -1;
                if (this.Group > other.Group)
                    return +1;
                if (this.Group < other.Group)
                    return -1;
                // Resharper disable once StringCompareToIsCultureSpecific
                return (this._path.CompareTo(other._path));
            }

        }

        public class ListOfPlotItem : ObservableCollection<PlotItem>
        {
            public void UpdateLang(string lang)
            {
                foreach (var it in this)
                    it.DisplayDescription = it.Description?.GetDefaultStr(lang);
            }

            public void UpdateValues(string lang = null)
            {
                foreach (var it in this)
                    if (it.SME != null)
                    {
                        var dbl = it.SME.ValueAsDouble();
                        if (dbl.HasValue && it.Args?.fmt != null)
                        {
                            try
                            {
                                it.DisplayValue = "" + dbl.Value.ToString(it.Args.fmt, CultureInfo.InvariantCulture);
                            }
                            catch (Exception ex)
                            {
                                it.DisplayValue = "<fmt error>";
                                LogInternally.That.SilentlyIgnoredError(ex);
                            }
                        }
                        else
                            it.DisplayValue = "" + it.SME.ValueAsText(lang);
                    }
            }

            public void RebuildFromSubmodel(AdminShell.Submodel sm, string lang)
            {
                // clear & access
                this.Clear();
                if (sm == null)
                    return;

                // find all SME with appropriate Qualifer
                var temp = new List<PlotItem>();
                sm.RecurseOnSubmodelElements(this, (state, parents, sme) =>
                {
                    // qualifier
                    var q = sme.HasQualifierOfType("Plotting.Args");
                    if (q == null)
                        return;

                    // select for SME type
                    /* TODO (MIHO, 2021-01-04): consider at least to include MLP, as well */
                    if (!(sme is AdminShell.Property))
                        return;

                    // build path
                    var path = sme.idShort;
                    if (parents != null)
                        foreach (var par in parents)
                            path = "" + par.idShort + " / " + path;

                    // add
                    temp.Add(new PlotItem(sme, "" + q.value, path, "" + sme.ValueAsText(), sme.description, lang));
                });

                // sort them for continous grouping
                temp.Sort();
                foreach (var t in temp)
                    this.Add(t);
            }

            public class PlotItemGroup : List<PlotItem>
            {
                public ScottPlot.WpfPlot WpfPlot;
                public int Group = 0;
            }

            public IEnumerable<PlotItemGroup> GetItemsGrouped()
            {
                // corner case
                if (this.Count < 1)
                    yield break;

                // start 1st chunk
                var temp = new PlotItemGroup();
                temp.Add(this[0]);
                temp.Group = this[0].Group;
                var startI = 0;
                for (int i = 1; i < this.Count; i++)
                {
                    if (this[startI].Group == this[i].Group)
                    {
                        temp.Add(this[i]);
                    }
                    else
                    {
                        yield return temp;
                        temp = new PlotItemGroup();
                        startI = i;
                        temp.Add(this[i]);
                        temp.Group = this[i].Group;
                    }
                }

                // last one
                if (temp.Count > 0)
                    yield return temp;
            }

            private bool _autoScaleX, _autoScaleY;

            public List<PlotItemGroup> RenderedGroups;

            public List<PlotItemGroup> RenderAllGroups(StackPanel panel, double plotHeight)
            {
                // first access
                var res = new List<PlotItemGroup>();
                if (panel == null)
                    return null;
                panel.Children.Clear();

                // before applying arguments
                _autoScaleX = true;
                _autoScaleY = true;

                // go over all groups                
                ScottPlot.WpfPlot lastPlot = null;
                foreach (var groupPI in GetItemsGrouped())
                {
                    // start new group
                    var pvc = new WpfPlotViewControlHorizontal();
                    pvc.Text = "Single value plot";
                    if (groupPI.Group >= 0 && groupPI.Group < 9999)
                        pvc.Text += $"; grp={groupPI.Group}";
                    var wpfPlot = pvc.WpfPlot;

                    // some basic attributes
                    lastPlot = wpfPlot;
                    wpfPlot.plt.AntiAlias(false, false, false);
                    wpfPlot.AxisChanged += (s, e) => WpfPlot_AxisChanged(wpfPlot, e);
                    groupPI.WpfPlot = wpfPlot;
                    res.Add(groupPI);

                    // for all wpf / all signals
                    pvc.Height = plotHeight;
                    pvc.ButtonClick += WpfPlot_ButtonClicked;

                    // for each signal
                    double? yMin = null, yMax = null;

                    foreach (var pi in groupPI)
                    {
                        // value
                        var val = pi.SME?.ValueAsDouble();

                        // integrate args
                        if (pi.Args != null)
                        {
                            if (pi.Args.ymin.HasValue)
                                yMin = Nullable.Compare(pi.Args.ymin, yMin) > 0 ? pi.Args.ymin : yMin;

                            if (pi.Args.ymax.HasValue)
                                yMax = Nullable.Compare(yMax, pi.Args.ymax) > 0 ? yMax : pi.Args.ymax;
                        }

                        // prepare data
                        var pb = new PlotBuffer();
                        pi.Buffer = pb;

                        // factory new Plottable
                        pb.Plottable = wpfPlot.plt.PlotSignal(pb.BufferData, label: "" + pi.DisplayPath);
                        pb.Push(val.HasValue ? val.Value : 0.0);
                    }

                    // apply some more args to the group
                    if (yMin.HasValue)
                    {
                        wpfPlot.plt.Axis(y1: yMin.Value);
                        _autoScaleY = false;
                    }

                    if (yMax.HasValue)
                    {
                        wpfPlot.plt.Axis(y2: yMax.Value);
                        _autoScaleY = false;
                    }

                    // render the plot into panel
                    wpfPlot.plt.Legend(fontSize: 9.0f);
                    panel.Children.Add(pvc);
                    wpfPlot.Render(skipIfCurrentlyRendering: true);
                }

                // for the last plot ..
                if (lastPlot != null)
                {
                    lastPlot.plt.XLabel("Samples");
                }

                // return groups for notice
                return res;
            }

            public void ForAllGroupsAndPlot(
                List<PlotItemGroup> groups,
                Action<PlotItemGroup, PlotItem> lambda = null)
            {
                if (groups == null)
                    return;
                foreach (var grp in groups)
                {
                    if (grp == null)
                        continue;
                    foreach (var pi in grp)
                        lambda?.Invoke(grp, pi);
                }
            }

            private void WpfPlot_ButtonClicked(WpfPlotViewControlHorizontal sender, int ndx)
            {
                // access
                var wpfPlot = sender?.WpfPlot;
                if (wpfPlot == null)
                    return;

                if (ndx == 1 || ndx == 2)
                {
                    // Horizontal scale Plus / Minus
                    var ax = wpfPlot.plt.Axis();
                    var width = Math.Abs(ax[1] - ax[0]);

                    if (ndx == 1)
                        wpfPlot.plt.Axis(x1: ax[0] - width / 2, x2: ax[1] + width / 2);

                    if (ndx == 2)
                        wpfPlot.plt.Axis(x1: ax[0] + width / 4, x2: ax[1] - width / 4);

                    // no autoscale for X
                    _autoScaleX = false;

                    // call for the other
                    WpfPlot_AxisChanged(wpfPlot, null);
                }

                if (ndx == 3 || ndx == 4)
                {
                    // Vertical scale Plus / Minus
                    var ax = wpfPlot.plt.Axis();
                    var height = Math.Abs(ax[3] - ax[2]);

                    if (ndx == 3)
                        wpfPlot.plt.Axis(y1: ax[2] - height / 2, y2: ax[3] + height / 2);

                    if (ndx == 4)
                        wpfPlot.plt.Axis(y1: ax[2] + height / 4, y2: ax[3] - height / 4);

                    // no autoscale for Y
                    _autoScaleY = false;

                    // call for the other
                    WpfPlot_AxisChanged(wpfPlot, null);
                }

                if (ndx == 5)
                {
                    // swithc auto scale ON and hope the best
                    _autoScaleX = true;
                    _autoScaleY = true;
                }

                if (ndx == 6)
                {
                    // plot larger
                    sender.Height += 100;
                }

                if (ndx == 7 && sender.Height >= 299)
                {
                    // plot smaller
                    sender.Height -= 100;
                }
            }

            private void WpfPlot_AxisChanged(object sender, EventArgs e)
            {
                if (sender is ScottPlot.WpfPlot wpfPlot)
                {
                    if (_autoScaleX || _autoScaleY)
                    {
                        _autoScaleX = false;
                        _autoScaleY = false;
                        ForAllGroupsAndPlot(RenderedGroups, (grp, pi) =>
                        {
                            // disable
                            var oldLimits = grp.WpfPlot.plt.Axis();
                            grp.WpfPlot.plt.Axis(x2: oldLimits[1] + 10);
                        });
                    }

                    ForAllGroupsAndPlot(RenderedGroups, (grp, pi) =>
                    {
                        var one = wpfPlot.plt.Axis();
                        if (grp.WpfPlot != wpfPlot && grp.WpfPlot != null)
                        {
                            // move
                            grp.WpfPlot.plt.Axis(x1: one[0], x2: one[1]);
                            grp.WpfPlot.Render(skipIfCurrentlyRendering: true);
                        }
                    });
                }
            }

            public void UpdateAllRenderedPlotItems(List<PlotItemGroup> groups)
            {
                // access
                if (groups == null)
                    return;

                // foreach
                foreach (var grp in groups)
                {
                    // access
                    if (grp == null)
                        continue;

                    // push for each item
                    foreach (var pi in grp)
                    {
                        var val = pi.SME?.ValueAsDouble();
                        pi.Buffer?.Push(val.HasValue ? val.Value : 0.0);
                    }

                    // scale?
                    if (_autoScaleX && _autoScaleY)
                        grp.WpfPlot?.plt.AxisAuto();
                    else
                    if (_autoScaleX)
                        grp.WpfPlot?.plt.AxisAutoX();
                    else
                    if (_autoScaleY)
                        grp.WpfPlot?.plt.AxisAutoY();

                    grp.WpfPlot?.Render(skipIfCurrentlyRendering: true);
                }
            }
        }

        public ListOfPlotItem PlotItems = new ListOfPlotItem();

        public PlottingViewControl()
        {
            InitializeComponent();
        }

        private AdminShellPackageEnv thePackage = null;
        private AdminShell.Submodel theSubmodel = null;
        private PlottingOptions theOptions = null;
        private PluginEventStack theEventStack = null;

        private string theDefaultLang = null;

        public void Start(
            AdminShellPackageEnv package,
            AdminShell.Submodel sm,
            PlottingOptions options,
            PluginEventStack eventStack)
        {
            // set the context
            this.thePackage = package;
            this.theSubmodel = sm;
            this.theOptions = options;
            this.theEventStack = eventStack;

            // ok, directly set contents
            SetContents();
        }

        private void SetContents()
        {
            // already set the contents?
        }

        //
        // Data logic
        //



        //
        // Mechanics
        //

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // languages
            foreach (var lng in AasxLanguageHelper.GetLangCodes())
                ComboBoxLang.Items.Add("" + lng);
            ComboBoxLang.Text = "en";

            // try display plot items
            PlotItems.RebuildFromSubmodel(theSubmodel, theDefaultLang);
            DataGridPlotItems.DataContext = PlotItems;

            // make charts, as well
            PlotItems.RenderedGroups = PlotItems.RenderAllGroups(StackPanelCharts, plotHeight: 200);

            // start a timer
            // Timer for status
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            // ReSharper disable once RedundantDelegateCreation
            dispatcherTimer.Tick += (se, e2) =>
            {
                if (PlotItems != null)
                {
                    PlotItems.UpdateValues();
                    PlotItems.UpdateAllRenderedPlotItems(PlotItems.RenderedGroups);
                }
            };
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            dispatcherTimer.Start();
        }

        private void FillDemoPlots()
        {
            Random rand = new Random();

            if (true)
            {
                var plt = new ScottPlot.Plot(600, 400);

                int pointCount = 51;
                double[] xs = DataGen.Consecutive(pointCount);
                double[] sin = DataGen.Sin(pointCount);
                double[] cos = DataGen.Cos(pointCount);

                plt.PlotScatter(xs, sin, label: "sin");
                plt.PlotScatter(xs, cos, label: "cos");
                plt.Legend();

                plt.Title("Scatter Plot Quickstart");
                plt.YLabel("Vertical Units");
                plt.XLabel("Horizontal Units");

                var pv1 = new ScottPlot.WpfPlot();
                pv1.Height = 200;

                plt.Title("Scatter Plot Quickstart");
                plt.YLabel("Vertical Units");
                plt.XLabel("Horizontal Units");

                StackPanelCharts.Children.Add(pv1);
            }

            if (true)
            {
                var pv1 = new ScottPlot.WpfPlot();
                pv1.Height = 200;
                pv1.plt.PlotSignal(DataGen.RandomWalk(rand, 100));

                var plt = pv1.plt;
                int pointCount = 51;
                double[] xs = DataGen.Consecutive(pointCount);
                double[] sin = DataGen.Sin(pointCount);
                double[] cos = DataGen.Cos(pointCount);

                plt.PlotScatter(xs, sin, label: "sin");
                plt.PlotScatter(xs, cos, label: "cos");
                plt.Legend();

                plt.Title("Scatter Plot Quickstart");
                plt.YLabel("Vertical Units");
                plt.XLabel("Horizontal Units");

                StackPanelCharts.Children.Add(pv1);
            }
        }

        private void ScrollViewerContent_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // simply disable the WHOLE preview -> mosue wheel handling, as the WpfControls get disturbed
            e.Handled = true;

            // scroll ourselves
            ScrollViewerContent.ScrollToVerticalOffset(ScrollViewerContent.VerticalOffset - e.Delta);

#if SAMPLE_CODE_EVENT_RAISE

            // simply disable mouse scroll for ScrollViewer, as it conflicts with the plots
            e.Handled = true;

            // if in inner loop, discard
            if (scrollInEventRaising)
                return;

            if (StackPanelCharts.Children != null)
                foreach (var uc in StackPanelCharts.Children)
                    if (uc is ScottPlot.WpfPlot wpfPlot)
                    {
                        if (wpfPlot.IsMouseOver)
                            continue;

                        // wpfPlot.RaiseEvent(new RoutedEventArgs(UIElement.PreviewMouseWheelEvent, sender));
                        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                        eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                        eventArg.Source = uc;
                        // this seems to raise consecutive events .. somehow??
                        wpfPlot.RaiseEvent(eventArg);
                    }
#endif
        }

        private void ComboBoxLang_TextChanged(object sender, TextChangedEventArgs e)
        {
            theDefaultLang = ComboBoxLang.Text;
            DataGridPlotItems.DataContext = null;
            PlotItems.RebuildFromSubmodel(theSubmodel, theDefaultLang);
            DataGridPlotItems.DataContext = PlotItems;
        }
    }
}
