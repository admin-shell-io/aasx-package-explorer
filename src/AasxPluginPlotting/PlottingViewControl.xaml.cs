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
using AasxIntegrationBase.AdminShellEvents;
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.ConceptModel;
using AdminShellNS;
using ScottPlot;

namespace AasxPluginPlotting
{
    public partial class PlottingViewControl : UserControl
    {
              
        

        public ListOfPlotItem PlotItems = new ListOfPlotItem();

        public PlottingViewControl()
        {
            InitializeComponent();
        }

        private AdminShellPackageEnv thePackage = null;
        private AdminShell.Submodel theSubmodel = null;
        private PlottingOptions theOptions = null;
        private PluginEventStack theEventStack = null;
        private LogInstance theLog = null;
        private PlotArguments _panelArgs = null;

        private string theDefaultLang = null;

        public void Start(
            AdminShellPackageEnv package,
            AdminShell.Submodel sm,
            PlottingOptions options,
            PluginEventStack eventStack,
            LogInstance log)
        {
            // set the context
            this.thePackage = package;
            this.theSubmodel = sm;
            this.theOptions = options;
            this.theEventStack = eventStack;
            this.theLog = log;

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

        private void ReDisplayPlotItemListTiles()
        {
            // reset both
            TabItemDataTiles.Visibility = Visibility.Collapsed;
            TabItemDataList.Visibility = Visibility.Collapsed;
            DataGridPlotItems.DataContext = null;
            PanelGridDataTiles.ItemsSource = null;

            // what to display?
            if (true == _panelArgs?.tiles)
            {
                TabControlDataView.SelectedItem = TabItemDataTiles;

                PanelGridDataTiles.DataContext = PlotItems.ReAssignItemTiles();
                PanelGridDataTiles.ItemsSource = PlotItems;
            }
            else
            {
                TabControlDataView.SelectedItem = TabItemDataList;
                DataGridPlotItems.DataContext = PlotItems;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // panel arguments (needs to be first when loaded)
            _panelArgs = PlotArguments.Parse(theSubmodel.HasQualifierOfType("Plotting.Args")?.value);
            if (true == _panelArgs?.title.HasContent())
                LabelPanelTitle.Content = _panelArgs.title;

            // languages
            foreach (var lng in AasxLanguageHelper.GetLangCodes())
                ComboBoxLang.Items.Add("" + lng);
            ComboBoxLang.Text = "en";

            // try display plot items
            PlotItems.RebuildFromSubmodel(theSubmodel, theDefaultLang);
            ReDisplayPlotItemListTiles();

            // make charts, as well
            PlotItems.RenderedGroups = PlotItems.RenderAllGroups(StackPanelCharts, defPlotHeight: 200);

            // hide these
            if (StackPanelCharts.Children.Count < 1)
            {
                GridContentCharts.Visibility = Visibility.Collapsed;
            }

            // start a timer
            var mainTimerMS = 500;
            if (_panelArgs != null && _panelArgs.timer >= 100)
                mainTimerMS = _panelArgs.timer;
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            // ReSharper disable once RedundantDelegateCreation
            dispatcherTimer.Tick += (se, e2) =>
            {
                if (PlotItems != null)
                {
                    try
                    { 
                        // PlotItems.UpdateValues();
                        // PlotItems.UpdateAllRenderedPlotItems(PlotItems.RenderedGroups);
                        ProcessEvents();
                    }
                    catch (Exception ex)
                    {
                        theLog?.Error(ex, "AasxPluginPlotting:TimerTick() processing updates");
                    }

                }
            };
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, mainTimerMS);
            dispatcherTimer.Start();

            // test
            // var ts1 = new TimeSeriesRecordData();
            // ts1.DataAdd("[0,0.1], [ 1, 0.23334 ] , [3,0.3]");

            try
            {
                TimeSeriesStartFromSubmodel(theSubmodel);
            } catch (Exception ex)
            {
                theLog?.Error(ex, "accessing Submodel elements for creating plot data");
            }

            try
            {
                var pvcs = _timeSeriesData?.RenderTimeSeries(
                    StackPanelTimeSeries, defPlotHeight: 200, theDefaultLang, theLog);

                // do post processing where we superior control
                foreach (var pvc in pvcs)
                {
                    if (pvc is WpfPlotViewControlCumulative pvcc)
                    {
                        pvcc.LatestSamplePositionChanged += (sender2, ndx2) =>
                        {
                            // re-rendering will access updated sample position from the widget
                            _timeSeriesData?.RefreshRenderedTimeSeries(
                                StackPanelTimeSeries, theDefaultLang, theLog);
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                theLog?.Error(ex, "rendering pre-processed plot data");
            }

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

                plt.AddScatter(xs, sin, label: "sin");
                plt.AddScatter(xs, cos, label: "cos");
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
                pv1.Plot.AddSignal(DataGen.RandomWalk(rand, 100));

                var plt = pv1.Plot;
                int pointCount = 51;
                double[] xs = DataGen.Consecutive(pointCount);
                double[] sin = DataGen.Sin(pointCount);
                double[] cos = DataGen.Cos(pointCount);

                plt.AddScatter(xs, sin, label: "sin");
                plt.AddScatter(xs, cos, label: "cos");
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
            PlotItems.RebuildFromSubmodel(theSubmodel, theDefaultLang);
            SafelyRefreshRenderedTimeSeries();
            ReDisplayPlotItemListTiles();
        }

        //
        // Time Series
        // Plan: refactor overall class into pieces afterwards
        //

        public class TimeSeriesDataPoint
        {
            public int Index = 0;
            public string ValStr = "";
            public double? Value;
        }

        public class TimeSeriesMinMaxDouble
        {
            public double Min;
            public double Max;
            public bool IsValid { get { return Min != double.MaxValue && Max != double.MinValue;  } }
            public double Span { get { return Max - Min; } }
            public static TimeSeriesMinMaxDouble Invalid =>
                new TimeSeriesMinMaxDouble() { Min = double.MaxValue, Max = double.MinValue };
        }

        public class TimeSeriesMinMaxInt
        {
            public int Min;
            public int Max;
            public bool IsValid { get { return Min != int.MaxValue && Max != int.MinValue; } }
            public int Span { get { return Max - Min; } }
            public static TimeSeriesMinMaxInt Invalid =>
                new TimeSeriesMinMaxInt() { Min = int.MaxValue, Max = int.MinValue };
        }

        public enum TimeSeriesTimeAxis { None, Utc, Tai, Plain, Duration }

        public class ListOfTimeSeriesDataPoint : List<TimeSeriesDataPoint>
        {
            public ListOfTimeSeriesDataPoint() : base() { }

            public ListOfTimeSeriesDataPoint(string json, TimeSeriesTimeAxis timeAxis) : base()
            {
                Add(json, timeAxis);
            }

            public TimeSeriesMinMaxInt GetMinMaxIndex()
            {
                if (this.Count < 1)
                    return new TimeSeriesMinMaxInt();
                var res = TimeSeriesMinMaxInt.Invalid;
                foreach (var dp in this)
                {
                    if (dp.Index < res.Min)
                        res.Min = dp.Index;
                    if (dp.Index > res.Max)
                        res.Max = dp.Index;
                }
                return res;
            }

            public void Add(string json, TimeSeriesTimeAxis timeAxis)
            {
                // simple state machine approch to pseudo parse-json
                var state = 0;
                int i = 0;
                var err = false;
                var bufIndex = "";
                var bufValue = "";

                while (! err && i < json.Length)
                {

                    switch (state)
                    {
                        // expecting inner '['
                        case 0:                            
                            if (json[i] == '[')
                            {
                                // prepare first buffer
                                state = 1;
                                bufIndex = "";
                                i++;
                            } else
                            if (json[i] == ',' || Char.IsWhiteSpace(json[i]))
                            {
                                // ignore whitespace
                                i++;
                            }
                            else
                            {
                                // break with error
                                err = true;
                            }
                            break;

                        // parsing 1st buffer: index
                        case 1:
                            if (json[i] == ',')
                            {
                                // prepare second buffer
                                state = 2;
                                bufValue = "";
                                i++;
                            }
                            else
                            if("0123456789-+.TZ\"".IndexOf(json[i]) >= 0)
                            {
                                bufIndex += json[i];
                                i++;
                            } else
                            if (Char.IsWhiteSpace(json[i]))
                            {
                                // ignore whitespace
                                i++;
                            }
                            else
                            {
                                // break with error
                                err = true;
                            }
                            break;

                        // parsing 2nd buffer: value
                        case 2:
                            if (json[i] == ']')
                            {
                                // ok, finalize
                                if (int.TryParse(bufIndex, out int iIndex))
                                {
                                    var dp = new TimeSeriesDataPoint()
                                    {
                                        Index = iIndex,
                                        ValStr = bufValue
                                    };

                                    if (timeAxis == TimeSeriesTimeAxis.Utc || timeAxis == TimeSeriesTimeAxis.Tai)
                                    {
                                        // strict time string
                                        if (DateTime.TryParseExact(bufValue,
                                            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture,
                                            DateTimeStyles.AdjustToUniversal, out DateTime dt))
                                        {
                                            dp.Value = dt.ToOADate();
                                        }
                                        else
                                        {
                                            ;
                                        }
                                    }
                                    else
                                    {
                                        // plain time or plain value
                                        if (double.TryParse(bufValue, NumberStyles.Float,
                                                CultureInfo.InvariantCulture, out double fValue))
                                            dp.Value = fValue;
                                    }
                                    this.Add(dp);
                                }

                                // next sample
                                state = 0;
                                i++;
                            }
                            else
                            if ("0123456789-+.:TZ\"".IndexOf(json[i]) >= 0)
                            {
                                bufValue += json[i];
                                i++;
                            }
                            else
                            if (Char.IsWhiteSpace(json[i]))
                            {
                                // ignore whitespace
                                i++;
                            }
                            else
                            {
                                // break with error
                                err = true;
                            }
                            break;
                    }

                }

            }
        }

        public class TimeSeriesDataSet
        {
            public string DataSetId = "";
            public TimeSeriesTimeAxis TimeAxis;
            public AdminShell.Property DataPoint;
            public AdminShell.ConceptDescription DataPointCD;

            public PlotArguments Args = null;

            public TimeSeriesDataSet AssignedTimeDS;

            public TimeSeriesMinMaxInt DsLimits = TimeSeriesMinMaxInt.Invalid;
            public TimeSeriesMinMaxDouble ValueLimits = TimeSeriesMinMaxDouble.Invalid;

            protected TimeSeriesMinMaxInt _dataLimits;         
            protected double[] data = new[] { 0.0 };
            public double[] Data { get { return data; } }

            public ScottPlot.Plottable.IPlottable Plottable;

            public double? RenderedBarWidth, RenderedBarOffet;
            public int? RenderedYaxisIndex;

            public void DataAdd(string json)
            {
                // intermediate format
                var temp = new ListOfTimeSeriesDataPoint(json, TimeAxis);
                if (temp.Count < 1)
                    return;

                // check to adapt current shape of data
                var tempLimits = temp.GetMinMaxIndex();

                // now, if the first time, start with good limits
                if (_dataLimits == null)
                    _dataLimits = new TimeSeriesMinMaxInt() { Min = tempLimits.Min, Max = tempLimits.Min };

                // extend to the left?
                if (tempLimits.Min < _dataLimits.Min)
                {
                    // how much
                    var delta = _dataLimits.Min - tempLimits.Min;

                    // adopt the limits
                    var oldsize = _dataLimits.Max - _dataLimits.Min + 1;
                    _dataLimits.Min -= delta;

                    // resize to these limits
                    Array.Resize<double>(ref data, _dataLimits.Max - _dataLimits.Min + 1);

                    // shift to right
                    Array.Copy(data, 0, data, delta, oldsize);
                }

                // extend to the right (fairly often the case)
                if (tempLimits.Max > _dataLimits.Max)
                {
                    // how much
                    var delta = tempLimits.Max - _dataLimits.Max;
                    _dataLimits.Max += delta;

                    // resize to these limits
                    Array.Resize<double>(ref data, _dataLimits.Max - _dataLimits.Min + 1);
                }

                // now, populate
                foreach (var dp in temp)
                    if (dp.Value.HasValue)
                    {
                        data[dp.Index - _dataLimits.Min] = dp.Value.Value;

                        if (dp.Index < DsLimits.Min)
                            DsLimits.Min = dp.Index;
                        if (dp.Index > DsLimits.Max)
                            DsLimits.Max = dp.Index;

                        if (dp.Value.Value < ValueLimits.Min)
                            ValueLimits.Min = dp.Value.Value;
                        if (dp.Value.Value > ValueLimits.Max)
                            ValueLimits.Max = dp.Value.Value;
                    }
            }

            public void DataAdd(int index, double value, int headroom = 1024)
            {
                // now, if the first time, start with good limits
                if (_dataLimits == null)
                    _dataLimits = new TimeSeriesMinMaxInt() { Min = index, Max = index };

                // extend to the left?
                if (index < _dataLimits.Min)
                {
                    // how much
                    var delta = _dataLimits.Min - index;

                    // adopt the limits
                    var oldsize = _dataLimits.Max - _dataLimits.Min + 1;
                    _dataLimits.Min -= delta;

                    // resize to these limits
                    Array.Resize<double>(ref data, _dataLimits.Max - _dataLimits.Min + 1);

                    // shift to right
                    Array.Copy(data, 0, data, delta, oldsize);
                }

                // extend to the right (fairly often the case)
                if (index > _dataLimits.Max)
                {
                    // how much, but respect headroom
                    var delta = Math.Max(index - _dataLimits.Max, headroom);
                    _dataLimits.Max += delta;

                    // resize to these limits
                    Array.Resize<double>(ref data, _dataLimits.Max - _dataLimits.Min + 1);
                }

                // now, populate
                data[index + _dataLimits.Min] = value;

                if (index < DsLimits.Min)
                    DsLimits.Min = index;
                if (index > DsLimits.Max)
                    DsLimits.Max = index;

                if (value < ValueLimits.Min)
                    ValueLimits.Min = value;
                if (value > ValueLimits.Max)
                    ValueLimits.Max = value;
            }

            /// <summary>
            /// Get the min and max used index values w.r.t. to Data[]
            /// </summary>
            /// <returns>Null in case of array</returns>
            public TimeSeriesMinMaxInt GetRenderLimits()
            {
                // reasonable data?
                if (DsLimits.Min == int.MaxValue || DsLimits.Max == int.MinValue)
                    return null;

                var res = new TimeSeriesMinMaxInt();
                res.Min = DsLimits.Min - _dataLimits.Min;
                res.Max = DsLimits.Max - /* DsLimits.Min - */ _dataLimits.Min;
                return res;
            }

#if __old
            public double[] RenderDataToLimitsOLD()
            {
                var rl = GetRenderLimits();
                if (rl == null)
                    return null;
                
                var res = new double[1 + rl.Max - rl.Min];
                for (int i = rl.Min; i <= rl.Max; i++)
                    res[i - rl.Min] = data[i - _dataLimits.Min];
                return res;
            }
#endif

            public double[] RenderDataToLimits(TimeSeriesMinMaxInt lim = null)
            {
                // defaults?
                if (lim == null)
                    lim = DsLimits;

                // reasonable data?
                if (lim == null || !lim.IsValid)
                    return null;

                // render
                var res = new double[1 + lim.Max - lim.Min];
                for (int i = lim.Min; i <= lim.Max; i++)
                    res[i - lim.Min] = data[i - _dataLimits.Min];
                return res;
            }

            public double? this[int index]
            {
                get
                {
                    if (index < DsLimits.Min || index > DsLimits.Max)
                        return null;
                    return data[index - _dataLimits.Min];
                }
            }
        }

        public class ListOfTimeSeriesDataSet : List<TimeSeriesDataSet>
        {
            /// <summary>
            /// Only get the latest sample from the different data sets.
            /// </summary>
            /// <param name="sampleOffset">Negative value for offset from latest sample</param>
            public List<Tuple<TimeSeriesDataSet, double>> GenerateCumulativeData(int sampleOffset)
            {
                var res = new List<Tuple<TimeSeriesDataSet, double>>();

                foreach (var ds in this)
                {
                    // do not allow empty or time acix data sets
                    if (ds == null && ds.TimeAxis != TimeSeriesTimeAxis.None)
                        continue;

                    // get the last sample
                    var rl = ds.GetRenderLimits();
                    if (rl == null)
                        continue;
                    var i = rl.Max + sampleOffset;
                    if (i < rl.Min)
                        i = rl.Min;
                    var lastVal = ds[i];
                    if (!lastVal.HasValue)
                        continue;

                    // add
                    res.Add(new Tuple<TimeSeriesDataSet, double>(ds, lastVal.Value));
                }

                return res;
            }            
        }

        public class TimeSeriesData
        {
            public AdminShell.SubmodelElementCollection SourceTimeSeries;

            public PlotArguments Args;

            public ListOfTimeSeriesDataSet DataSet = new ListOfTimeSeriesDataSet();

            // the time series might have different time axis for the records (not the variables)
            public Dictionary<TimeSeriesTimeAxis, TimeSeriesDataSet> TimeDsLookup = 
                new Dictionary<TimeSeriesTimeAxis, TimeSeriesDataSet>();

            public IWpfPlotViewControl UsedPlot;
            public bool RederedCumulative;
            public PlotArguments.Type UsedType;

            public TimeSeriesDataSet FindDataSetById(string dsid)
            {
                foreach (var tsd in DataSet)
                    if (tsd?.DataSetId == dsid)
                        return tsd;
                return null;
            }

            public TimeSeriesDataSet FindDataForTimeAxis(bool findUtc = false, bool findTai = false)
            {
                foreach (var tsd in DataSet) 
                {
                    if (tsd == null)
                        continue;
                    if (tsd.TimeAxis == TimeSeriesTimeAxis.Utc && findUtc)
                        return tsd;
                    if (tsd.TimeAxis == TimeSeriesTimeAxis.Tai && findTai)
                        return tsd;
                }
                return null;
            }
        }

        public class ListOfTimeSeriesData : List<TimeSeriesData>
        {
            public TimeSeriesData FindDataSetBySource(AdminShell.SubmodelElementCollection smcts)
            {
                if (smcts == null)
                    return null;

                foreach (var tsd in this)
                    if (tsd?.SourceTimeSeries == smcts)
                        return tsd;
                return null;
            }

            public static string EvalDisplayText(
                string minmalText, AdminShell.SubmodelElement sme, 
                AdminShell.ConceptDescription cd = null,
                bool addMinimalTxt = false,
                string defaultLang = null,
                bool useIdShort = true)
            {
                var res = "" + minmalText;
                if (sme != null)
                {
                    // best option: description of the SME itself
                    string better = sme.description?.GetDefaultStr(defaultLang);

                    // if still none, simply use idShort
                    // SME specific non-multi-lang found better than CD multi-lang?!
                    if (!better.HasContent() && useIdShort)
                        better = sme.idShort;

                    // no? then look for CD information
                    if (cd != null)
                    {
                        if (!better.HasContent())
                            better = cd.GetDefaultPreferredName(defaultLang);
                        if (!better.HasContent())
                            better = cd.idShort;
                        if (better.HasContent() && true == cd.IEC61360Content?.unit.HasContent())
                            better += $" [{cd.IEC61360Content?.unit}]";
                    }

                    if (better.HasContent())
                    {
                        res = better;
                        if (addMinimalTxt)
                            res += $" ({minmalText})";
                    }
                }
                return res;
            }

            public class CumulativeDataItems
            {
                public List<string> Label = new List<string>();
                public List<double> Position = new List<double>();
                public List<double> Value = new List<double>();
            }

            public CumulativeDataItems GenerateCumulativeDataItems(
                List<Tuple<TimeSeriesDataSet, double>> cum, string defaultLang)
            {
                var res = new CumulativeDataItems();
                if (cum == null)
                    return res;
                double pos = 0.0;
                foreach (var cm in cum)
                {
                    var ds = cm.Item1;
                    if (ds == null)
                        continue;

                    var lab = EvalDisplayText("" + ds.DataSetId, ds.DataPoint, ds.DataPointCD,
                        addMinimalTxt: false, defaultLang: defaultLang, useIdShort: true);

                    res.Value.Add(cm.Item2);
                    res.Label.Add(lab);
                    res.Position.Add(pos);
                    pos += 1.0;
                }
                return res;
            }

            public ScottPlot.Plottable.IPlottable GenerateCumulativePlottable(
                ScottPlot.WpfPlot wpfPlot,
                CumulativeDataItems cumdi,
                PlotArguments args)
            {
                // access
                if (wpfPlot == null || cumdi == null || args == null)
                    return null;

                if (args.type == PlotArguments.Type.Pie)
                {
                    var pie = wpfPlot.Plot.AddPie(cumdi.Value.ToArray());
                    pie.SliceLabels = cumdi.Label.ToArray();
                    pie.ShowLabels = true == args.labels;
                    pie.ShowValues = true == args.values;
                    pie.ShowPercentages = true == args.percent;
                    pie.SliceFont.Size = 9.0f;
                    return pie;
                }

                if (args.type == PlotArguments.Type.Bars)
                {
                    var bar = wpfPlot.Plot.AddBar(cumdi.Value.ToArray());
                    wpfPlot.Plot.XTicks(cumdi.Position.ToArray(), cumdi.Label.ToArray());
                    bar.ShowValuesAboveBars = true == args.values;
                    return bar;
                }

                return null;
            }

            public List<IWpfPlotViewControl> RenderTimeSeries(
                StackPanel panel, double defPlotHeight, string defaultLang, LogInstance log)
            {
                // first access
                var res = new List<IWpfPlotViewControl>();
                if (panel == null)
                    return null;
                panel.Children.Clear();

                // go over all groups                
                foreach (var tsd in this)
                {
                    // skip?
                    if (tsd.Args?.skip == true)
                        continue;

                    // which kind of chart?
                    if (tsd.Args != null &&
                        (tsd.Args.type == PlotArguments.Type.Bars
                         || tsd.Args.type == PlotArguments.Type.Pie))
                    {
                        //
                        // Cumulative plots (e.g. the last sample, bars, pie, ..)
                        //

                        tsd.RederedCumulative = true;

                        // start new group
                        var pvc = new WpfPlotViewControlCumulative();
                        tsd.UsedPlot = pvc;
                        pvc.Text = EvalDisplayText("Cumulative plot", tsd.SourceTimeSeries, defaultLang: defaultLang);
                        var wpfPlot = pvc.WpfPlot;
                        if (wpfPlot == null)
                            continue;

                        // generate cumulative data
                        var cum = tsd.DataSet.GenerateCumulativeData(pvc.LatestSamplePosition);
                        var cumdi = GenerateCumulativeDataItems(cum, defaultLang);
                        var plottable = GenerateCumulativePlottable(wpfPlot, cumdi, tsd.Args);
                        if (plottable == null)
                            continue;
                        pvc.ActivePlottable = plottable;

                        // render the plottable into panel
                        PlotHelpers.SetOverallPlotProperties(pvc, wpfPlot, tsd.Args, defPlotHeight);
                        panel.Children.Add(pvc);                        
                        wpfPlot.Render(/* skipIfCurrentlyRendering: true */);
                        res.Add(pvc);
                    }
                    else
                    {
                        //
                        // Time series based plots (scatter, bars)
                        //

                        tsd.RederedCumulative = false;

                        // start new group
                        var pvc = new WpfPlotViewControlHorizontal();
                        tsd.UsedPlot = pvc;
                        pvc.Text = EvalDisplayText("Time Series plot", tsd.SourceTimeSeries, defaultLang: defaultLang);
                        pvc.AutoScaleX = true;
                        pvc.AutoScaleY = true;
                        var wpfPlot = pvc.WpfPlot;
                        if (wpfPlot == null)
                            continue;

                        ScottPlot.WpfPlot lastPlot = null;
                        var xLabels = "Time ( ";
                        int yAxisNum = 0;

                        // some basic attributes
                        lastPlot = wpfPlot;

                        // make a list of plottables in order to sort by order
                        var moveOrder = new List<Tuple<TimeSeriesDataSet, int?>>();

                        // for each signal
                        double? yMin = null, yMax = null;
                        TimeSeriesDataSet lastTimeRecord = null;

                        foreach (var tsds in tsd.DataSet)
                        {
                            // skip?
                            if (tsds.Args?.skip == true)
                                continue;

                            // if its a time axis, skip but remember for following axes
                            if (tsds.TimeAxis != TimeSeriesTimeAxis.None)
                            {
                                lastTimeRecord = tsds;
                                xLabels += "" + tsds.DataSetId + " ";
                                continue;
                            }

                            // add to later sort order
                            moveOrder.Add(new Tuple<TimeSeriesDataSet, int?>(tsds, tsds.Args?.order));

                            // cannot render without time record
                            var timeDStoUse = tsds.AssignedTimeDS;
                            if (timeDStoUse == null)
                                timeDStoUse = lastTimeRecord;
                            if (timeDStoUse == null)
                                continue;
                            tsds.AssignedTimeDS = timeDStoUse;

                            // compare (fix?) render limits
                            var rlt = timeDStoUse.GetRenderLimits();
                            var rld = tsds.GetRenderLimits();
                            if (rlt == null || rld == null || rlt.Min != rld.Min || rlt.Max != rld.Max)
                            {
                                log?.Error($"When rendering data set {tsds.DataSetId} different dimensions for X and Y.");
                                continue;
                            }

                            // integrate args
                            if (tsds.Args != null)
                            {
                                if (tsds.Args.ymin.HasValue)
                                    yMin = Nullable.Compare(tsds.Args.ymin, yMin) > 0 ? tsds.Args.ymin : yMin;

                                if (tsds.Args.ymax.HasValue)
                                    yMax = Nullable.Compare(yMax, tsds.Args.ymax) > 0 ? yMax : tsds.Args.ymax;
                            }

                            // factory new Plottable
                            // pb.Plottable = wpfPlot.plt.PlotSignal(pb.BufferData, label: "" + pi.DisplayPath);

                            ScottPlot.Plottable.BarPlot bars = null;
                            ScottPlot.Plottable.ScatterPlot scatter = null;

                            if (tsds.Args != null && tsds.Args.type == PlotArguments.Type.Bars)
                            {
                                // Bars
                                bars = wpfPlot.Plot.AddBar(
                                    positions: timeDStoUse.RenderDataToLimits(),
                                    values: tsds.RenderDataToLimits());

                                // customize the width of bars (80% of the inter-position distance looks good)
                                if (timeDStoUse.Data.Length >= 2)
                                {
                                    // Note: pretty trivial approach, yet
                                    var bw = (timeDStoUse.Data[1] - timeDStoUse.Data[0]) * .8;

                                    // set
                                    bars.BarWidth = bw;

                                    // remember
                                    tsds.RenderedBarWidth = bw;
                                }

                                bars.Label = EvalDisplayText("" + tsds.DataSetId, tsds.DataPoint, tsds.DataPointCD,
                                    addMinimalTxt: true, defaultLang: defaultLang, useIdShort: false);

                                tsds.Plottable = bars;
                            }
                            else
                            {
                                // Default: Scatter plot
                                scatter = wpfPlot.Plot.AddScatter(
                                    xs: timeDStoUse.Data,
                                    ys: tsds.Data,
                                    label: EvalDisplayText("" + tsds.DataSetId, tsds.DataPoint, tsds.DataPointCD,
                                        addMinimalTxt: true, defaultLang: defaultLang, useIdShort: false));

                                if (true == tsds.Args?.linewidth.HasValue)
                                    scatter.LineWidth = tsds.Args.linewidth.Value;

                                if (true == tsds.Args?.markersize.HasValue)
                                    scatter.MarkerSize = (float) tsds.Args.markersize.Value;

                                tsds.Plottable = scatter;

                                var rl = tsds.GetRenderLimits();
                                scatter.MinRenderIndex = rl.Min;
                                scatter.MaxRenderIndex = rl.Max;
                            }

                            // axis treatment?
                            bool sameAxis = (tsds.Args?.sameaxis == true) && (yAxisNum != 0);
                            int assignAxis = -1;
                            ScottPlot.Renderable.Axis yAxis3 = null;
                            if (!sameAxis)
                            {
                                yAxisNum++;
                                if (yAxisNum >= 2)
                                {
                                    yAxis3 = wpfPlot.Plot.AddAxis(ScottPlot.Renderable.Edge.Right, axisIndex: yAxisNum);
                                    assignAxis = yAxisNum;
                                }
                            }
                            else
                                // take last one
                                assignAxis = yAxisNum - 1;

                            if (assignAxis >= 0)
                            {
                                tsds.RenderedYaxisIndex = assignAxis;

                                if (scatter != null)
                                {
                                    scatter.YAxisIndex = assignAxis;
                                    if (yAxis3 != null)
                                        yAxis3.Color(scatter.Color);
                                }

                                if (bars != null)
                                {
                                    bars.YAxisIndex = assignAxis;
                                    if (yAxis3 != null)
                                        yAxis3.Color(bars.FillColor);
                                }
                            }
                        }

                        // now sort for order
                        moveOrder.Sort((mo1, mo2) => Nullable.Compare(mo1.Item2,mo2.Item2));
                        foreach (var mo in moveOrder)
                            if (mo?.Item1?.Plottable != null)
                                wpfPlot.Plot.MoveLast(mo.Item1.Plottable);

                        // apply some more args to the group
                        if (yMin.HasValue)
                        {
                            wpfPlot.Plot.SetAxisLimits(yMin: yMin.Value);
                            pvc.AutoScaleY = false;
                        }

                        if (yMax.HasValue)
                        {
                            wpfPlot.Plot.SetAxisLimits(yMax: yMax.Value);
                            pvc.AutoScaleY = false;
                        }

                        // time axis
                        if (lastTimeRecord != null &&
                            (lastTimeRecord.TimeAxis == TimeSeriesTimeAxis.Utc
                             || lastTimeRecord.TimeAxis == TimeSeriesTimeAxis.Tai))
                        {
                            wpfPlot.Plot.XAxis.DateTimeFormat(true);
                        }

                        // for the last plot ..
                        if (true /* lastPlot != null */)
                        {
                            xLabels += ")";
                            lastPlot.Plot.XLabel(xLabels);
                        }

                        // render the plot into panel
                        PlotHelpers.SetOverallPlotProperties(pvc, wpfPlot, tsd.Args, defPlotHeight);
                        panel.Children.Add(pvc);
                        pvc.ButtonClick += (sender, ndx) =>
                        {
                            if (ndx == 5)
                            {
                                // perform a customised X/Y axis reset

                                // for X find UTC timescale and reset manually
                                var tsutc = tsd.FindDataForTimeAxis(findUtc: true, findTai: true);
                                if (tsutc?.ValueLimits != null
                                    && tsutc.ValueLimits.IsValid)
                                {
                                    var sp = tsutc.ValueLimits.Span;
                                    wpfPlot.Plot.SetAxisLimitsX(
                                        tsutc.ValueLimits.Min - 0.05 * sp, tsutc.ValueLimits.Max + 0.05 * sp);
                                }

                                // for Y used default
                                wpfPlot.Plot.AxisAutoY();

                                // commit
                                wpfPlot.Render();

                                // no default
                                return;
                            }
                            pvc.DefaultButtonClicked(sender, ndx);
                        };
                        wpfPlot.Render();
                        res.Add(pvc);
                    }
                }

                // return groups for notice
                return res;
            }

            public List<int> RefreshRenderedTimeSeries(StackPanel panel, string defaultLang, LogInstance log)
            {
                // first access
                var res = new List<int>();
                if (panel == null)
                    return null;

                // go over all groups                
                foreach (var tsd in this)
                {
                    // skip?
                    if (tsd.Args?.skip == true)
                        continue;

                    // general plot data
                    if (tsd.UsedPlot is IWpfPlotViewControl ipvc)
                    {
                        ipvc.Text = EvalDisplayText("Time Series plot", tsd.SourceTimeSeries, defaultLang: defaultLang);
                    }

                    // which kind of chart?
                    if (tsd.RederedCumulative)
                    {
                        //
                        // Cumulative plots (e.g. the last sample, bars, pie, ..)
                        //

                        if (tsd.UsedPlot is WpfPlotViewControlCumulative pvc)
                        {
                            // remove old chart
                            if (pvc.ActivePlottable != null)
                                tsd.UsedPlot.WpfPlot.Plot.Remove(pvc.ActivePlottable);

                            // generate cumulative data
                            var cum = tsd.DataSet.GenerateCumulativeData(pvc.LatestSamplePosition);
                            var cumdi = GenerateCumulativeDataItems(cum, defaultLang);
                            var plottable = GenerateCumulativePlottable(tsd.UsedPlot.WpfPlot, cumdi, tsd.Args);
                            if (plottable == null)
                                continue;
                            pvc.ActivePlottable = plottable;

                            // render the plot into panel
                            tsd.UsedPlot.WpfPlot.Render();
                        }
                    }
                    else
                    {
                        //
                        // Time series based plots (scatter, bars)
                        //

                        // Note: the original approach for re-rendering was to leave the individual plottables
                        // in place and to replace/ "update" the data. But:
                        // For Bars: not supported by the API
                        // For Scatter: Update() provided, but same strange errors led to non-rendering charts
                        // Therefore: the plottables are tediously recreated; this is not optimal in terms
                        // of performance!

                        // access
                        if (tsd.UsedPlot?.WpfPlot == null)
                            continue;

                        double maxRenderX = double.MinValue;

                        // find valid data sets
                        foreach (var tsds in tsd.DataSet)
                        {
                            // skip?
                            if (tsds.Args?.skip == true)
                                continue;

                            // required to be no time axis, but to have a time axis
                            if (tsds.TimeAxis != TimeSeriesTimeAxis.None)
                                continue;
                            if (tsds.AssignedTimeDS == null)
                                continue;

                            // compare (fix?) render limits
                            var rlt = tsds.AssignedTimeDS.GetRenderLimits();
                            var rld = tsds.GetRenderLimits();
                            if (rlt == null || rld == null || rlt.Min != rld.Min || rlt.Max != rld.Max)
                            {
                                log?.Error($"When rendering data set {tsds.DataSetId} different dimensions for X and Y.");
                                continue;
                            }

                            if (tsds.Plottable is ScottPlot.Plottable.BarPlot bars)
                            {
                                // need to remove old bars
                                var oldBars = bars;
                                tsd.UsedPlot.WpfPlot.Plot.Remove(bars);

                                // Bars (redefine!!)
                                bars = tsd.UsedPlot.WpfPlot.Plot.AddBar(
                                    positions: tsds.AssignedTimeDS.RenderDataToLimits(),
                                    values: tsds.RenderDataToLimits());
                                tsds.Plottable = bars;

                                // tedious re-assign of style
                                bars.FillColor = oldBars.FillColor;
                                bars.FillColorNegative = oldBars.FillColorNegative;
                                bars.FillColorHatch = oldBars.FillColorHatch;
                                bars.HatchStyle = oldBars.HatchStyle;
                                bars.BorderLineWidth = oldBars.BorderLineWidth;

                                // set Yaxis if available
                                if (tsds.RenderedYaxisIndex.HasValue)
                                    bars.YAxisIndex = tsds.RenderedYaxisIndex.Value;

                                // always in the background
                                // Note: not in sync with sortorder!
                                tsd.UsedPlot.WpfPlot.Plot.MoveFirst(bars);

                                // use the already decided width, offset of bars
                                if (tsds.RenderedBarWidth.HasValue)
                                    bars.BarWidth = tsds.RenderedBarWidth.Value;
                                if (tsds.RenderedBarOffet.HasValue)
                                    bars.PositionOffset = tsds.RenderedBarOffet.Value;

                                bars.Label = EvalDisplayText("" + tsds.DataSetId, tsds.DataPoint, tsds.DataPointCD,
                                    addMinimalTxt: true, defaultLang: defaultLang, useIdShort: false);

                                // eval latest X for later setting
                                var latestX = tsds.AssignedTimeDS.Data[rlt.Max];
                                if (latestX > maxRenderX)
                                    maxRenderX = latestX;

                            }

                            if (tsds.Plottable is ScottPlot.Plottable.ScatterPlot scatter)
                            {
#if __not_working

                                // just set the render limits to new values
                                scatter.Update(tsds.AssignedTimeDS.Data, tsds.Data);
                                    scatter.MinRenderIndex = rld.Min;
                                    scatter.MaxRenderIndex = rld.Max;
#endif

                                // need to remove old bars
                                var oldScatter = scatter;
                                tsd.UsedPlot.WpfPlot.Plot.Remove(scatter);

                                // re-create scatter
                                scatter = tsd.UsedPlot.WpfPlot.Plot.AddScatter(
                                    xs: tsds.AssignedTimeDS.RenderDataToLimits(),
                                    ys: tsds.RenderDataToLimits(),
                                    label: EvalDisplayText("" + tsds.DataSetId, tsds.DataPoint, tsds.DataPointCD,
                                        addMinimalTxt: true, defaultLang: defaultLang, useIdShort: false));

                                // tedious re-assign of style
                                scatter.Color = oldScatter.Color;
                                scatter.LineStyle = oldScatter.LineStyle;
                                scatter.MarkerShape = oldScatter.MarkerShape;
                                scatter.LineWidth = oldScatter.LineWidth;
                                scatter.ErrorLineWidth = oldScatter.ErrorLineWidth;
                                scatter.ErrorCapSize = oldScatter.ErrorCapSize;
                                scatter.MarkerSize = oldScatter.MarkerSize;
                                scatter.StepDisplay = oldScatter.StepDisplay;

                                // set Yaxis and other attributes if available
                                if (tsds.RenderedYaxisIndex.HasValue)
                                    scatter.YAxisIndex = tsds.RenderedYaxisIndex.Value;

                                if (true == tsds.Args?.linewidth.HasValue)
                                    scatter.LineWidth = tsds.Args.linewidth.Value;

                                if (true == tsds.Args?.markersize.HasValue)
                                    scatter.MarkerSize = (float)tsds.Args.markersize.Value;

                                // always in the foreground
                                // Note: not in sync with sortorder!
                                tsd.UsedPlot.WpfPlot.Plot.MoveLast(scatter);

                                tsds.Plottable = scatter;

                                // eval latest X for later setting
                                var latestX = tsds.AssignedTimeDS.Data[rlt.Max];
                                if (latestX > maxRenderX)
                                    maxRenderX = latestX;

                                if (tsd.UsedPlot.AutoScaleY 
                                    && tsds.ValueLimits.Min != double.MaxValue
                                    && tsds.ValueLimits.Max != double.MinValue)
                                {
                                    var ai = scatter.YAxisIndex;
                                    var hy = tsds.ValueLimits.Max - tsds.ValueLimits.Min;
                                    tsd.UsedPlot.WpfPlot.Plot.SetAxisLimits(
                                        yAxisIndex: ai,
                                        yMin: tsds.ValueLimits.Min - hy * 0.1,
                                        yMax: tsds.ValueLimits.Max + hy * 0.1);
                                }

                            }

                        }

                        // remain the zoom level, scroll to lastest x
                        if (maxRenderX != double.MinValue && tsd.UsedPlot.AutoScaleX)
                        {
                            var ax = tsd.UsedPlot.WpfPlot.Plot.GetAxisLimits();
                            var wx = (ax.XMax - ax.XMin);
                            var XMinNew = maxRenderX - 0.9 * wx;
                            var XMaxNew = maxRenderX + 0.1 * wx;
                            if (XMaxNew > ax.XMax)
                                tsd.UsedPlot.WpfPlot.Plot.SetAxisLimitsX(XMinNew, XMaxNew);
                        }
                        
                        // render the plot into panel
                        tsd.UsedPlot.WpfPlot.Render();
                    }
                }

                // return groups for notice
                return res;
            }
        }

        protected ListOfTimeSeriesData _timeSeriesData = new ListOfTimeSeriesData();

        protected double? SpecifiedTimeToDouble(
            TimeSeriesTimeAxis timeAxis, string bufValue)
        {
            if (timeAxis == TimeSeriesTimeAxis.Utc || timeAxis == TimeSeriesTimeAxis.Tai)
            {
                // strict time string
                if (DateTime.TryParseExact(bufValue,
                    "yyyy-MM-dd'T'HH:mm:ssZ", CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal, out DateTime dt))
                {
                    return dt.ToOADate();
                }
            }

            // plain time or plain value
            if (double.TryParse(bufValue, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double fValue))
                return fValue;

            // no?
            return null;
        }

        protected Tuple<TimeSeriesTimeAxis, AdminShell.Property> 
            DetectTimeSpecifier(
                ZveiTimeSeriesDataV10 pcts,
                AdminShell.Key.MatchMode mm,
                AdminShell.SubmodelElementCollection smc)
        {
            // access
            if (smc?.value == null || pcts == null)
                return null;

            // detect
            AdminShell.Property prop = null;
            prop = smc.value.FindFirstSemanticIdAs<AdminShell.Property>(pcts.CD_UtcTime.GetReference(), mm);
            if (prop != null)
                return new Tuple<TimeSeriesTimeAxis, AdminShell.Property>(TimeSeriesTimeAxis.Utc, prop);

            prop = smc.value.FindFirstSemanticIdAs<AdminShell.Property>(pcts.CD_TaiTime.GetReference(), mm);
            if (prop != null)
                return new Tuple<TimeSeriesTimeAxis, AdminShell.Property>(TimeSeriesTimeAxis.Tai, prop);

            prop = smc.value.FindFirstSemanticIdAs<AdminShell.Property>(pcts.CD_Time.GetReference(), mm);
            if (prop != null)
                return new Tuple<TimeSeriesTimeAxis, AdminShell.Property>(TimeSeriesTimeAxis.Plain, prop);

            prop = smc.value.FindFirstSemanticIdAs<AdminShell.Property>(pcts.CD_TimeDuration.GetReference(), mm);
            if (prop != null)
                return new Tuple<TimeSeriesTimeAxis, AdminShell.Property>(TimeSeriesTimeAxis.Plain, prop);

            // no
            return null;
        }

        private void TimeSeriesAddSegmentData(
            ZveiTimeSeriesDataV10 pcts,
            AdminShell.Key.MatchMode mm,
            TimeSeriesData tsd,
            AdminShell.SubmodelElementCollection smcseg)
        {
            // access
            if (pcts == null || smcseg == null)
                return;

            // challenge is to select SMes, which are NOT from a known semantic id!
            var tsvAllowed = new[]
            {
                pcts.CD_RecordId.GetSingleKey(),
                pcts.CD_UtcTime.GetSingleKey(),
                pcts.CD_TaiTime.GetSingleKey(),
                pcts.CD_Time.GetSingleKey(),
                pcts.CD_TimeDuration.GetSingleKey(),
                pcts.CD_ValueArray.GetSingleKey(),
                pcts.CD_ExternalDataFile.GetSingleKey()
            };

            var tsrAllowed = new[]
            {
                pcts.CD_RecordId.GetSingleKey(),
                pcts.CD_UtcTime.GetSingleKey(),
                pcts.CD_TaiTime.GetSingleKey(),
                pcts.CD_Time.GetSingleKey(),
                pcts.CD_TimeDuration.GetSingleKey(),
                pcts.CD_ValueArray.GetSingleKey()
            };

            // find variables?
            foreach (var smcvar in smcseg.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                pcts.CD_TimeSeriesVariable.GetReference(), mm))
            {
                // makes only sense with record id
                var recid = "" + smcvar.value.FindFirstSemanticIdAs<AdminShell.Property>(
                    pcts.CD_RecordId.GetReference(), mm)?.value?.Trim();
                if (recid.Length < 1)
                    continue;

                // add need a value array as well!
                var valarr = "" + smcvar.value.FindFirstSemanticIdAs<AdminShell.Blob>(
                    pcts.CD_ValueArray.GetReference(), mm)?.value?.Trim();
                if (valarr.Length < 1)
                    continue;

                // already have a dataset with that id .. or make new?
                var ds = tsd.FindDataSetById(recid);
                if (ds == null)
                {
                    // add
                    ds = new TimeSeriesDataSet() { DataSetId = recid };
                    tsd.DataSet.Add(ds);

                    // at this very moment, check if this is a time series
                    var timeSpec = DetectTimeSpecifier(pcts, mm, smcvar);
                    if (timeSpec != null)
                        ds.TimeAxis = timeSpec.Item1;

                    // find a DataPoint description?
                    var pdp = smcvar.value.FindFirstAnySemanticId<AdminShell.Property>(tsvAllowed, mm,
                        invertAllowed: true);
                    if (pdp != null && ds.DataPoint == null)
                    {
                        ds.DataPoint = pdp;
                        ds.DataPointCD = thePackage?.AasEnv?.FindConceptDescription(pdp.semanticId);
                    }

                    // plot arguments for record?
                    ds.Args = PlotArguments.Parse(smcvar.HasQualifierOfType("TimeSeries.Args")?.value);
                }

                // now try add the value array
                ds.DataAdd(valarr);
            }

            // find records?
            foreach (var smcrec in smcseg.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                pcts.CD_TimeSeriesRecord.GetReference(), mm))
            {
                // makes only sense with a numerical record id
                var recid = "" + smcrec.value.FindFirstSemanticIdAs<AdminShell.Property>(
                    pcts.CD_RecordId.GetReference(), mm)?.value?.Trim();
                if (recid.Length < 1)
                    continue;
                if (!int.TryParse(recid, out var dataIndex))
                    continue;

                // to prevent attacks, restrict index
                if (dataIndex < 0 || dataIndex > 16 * 1024 * 1024)
                    continue;

                // but, in this case, the dataset id's and data comes from individual
                // data points
                foreach (var pdp in smcrec.value.FindAllSemanticId<AdminShell.Property>(tsrAllowed, mm,
                        invertAllowed: true))
                {
                    // the dataset id is?
                    var dsid = "" + pdp.idShort;
                    if (!dsid.HasContent())
                        continue;

                    // query avilable information on the time
                    var timeSpec = DetectTimeSpecifier(pcts, mm, smcrec);
                    if (timeSpec == null)
                        continue;

                    // already have a dataset with that id .. or make new?
                    var ds = tsd.FindDataSetById(dsid);
                    if (ds == null)
                    {
                        // add
                        ds = new TimeSeriesDataSet() { DataSetId = dsid };
                        tsd.DataSet.Add(ds);

                        // find a DataPoint description? .. store it!
                        if (ds.DataPoint == null)
                        {
                            ds.DataPoint = pdp;
                            ds.DataPointCD = thePackage?.AasEnv?.FindConceptDescription(pdp.semanticId);
                        }

                        // now fix (one time!) the time data set for this data set
                        if (tsd.TimeDsLookup.ContainsKey(timeSpec.Item1))
                            ds.AssignedTimeDS = tsd.TimeDsLookup[timeSpec.Item1];
                        else
                        {
                            // create this
                            ds.AssignedTimeDS = new TimeSeriesDataSet()
                            {
                                DataSetId = "Time_" + timeSpec.Item1.ToString()
                            };
                            tsd.TimeDsLookup[timeSpec.Item1] = ds.AssignedTimeDS;
                        }

                        // plot arguments for datapoint?
                        ds.Args = PlotArguments.Parse(pdp.HasQualifierOfType("TimeSeries.Args")?.value);
                    }

                    // now access the value of the data point as float value
                    if (!double.TryParse(pdp.value, NumberStyles.Float,
                            CultureInfo.InvariantCulture, out var dataValue))
                        continue;

                    // TimeDS and time is required
                    if (ds.AssignedTimeDS == null)
                        continue;

                    var tm = SpecifiedTimeToDouble(timeSpec.Item1, timeSpec.Item2.value);
                    if (!tm.HasValue)
                        continue;

                    // ok, push the data into the dataset
                    ds.AssignedTimeDS.DataAdd(dataIndex, tm.Value);
                    ds.DataAdd(dataIndex, dataValue);
                }
            }
        }

        protected void TimeSeriesStartFromSubmodel(AdminShell.Submodel sm)
        {
            // access
            if (sm?.submodelElements == null)
                return;
            var pcts = AasxPredefinedConcepts.ZveiTimeSeriesDataV10.Static;
            var mm = AdminShell.Key.MatchMode.Relaxed;

            // clear
            _timeSeriesData.Clear();            

            // find SMC for TimeSeries itself -> this will result in a plot
            foreach (var smcts in sm.submodelElements.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                pcts.CD_TimeSeries.GetReference(), mm))
            {
                // make initial data for time series
                var tsd = new TimeSeriesData() { SourceTimeSeries = smcts };
                _timeSeriesData.Add(tsd);

                // plot arguments for time series
                tsd.Args = PlotArguments.Parse(smcts.HasQualifierOfType("TimeSeries.Args")?.value);

                // find segements
                foreach (var smcseg in smcts.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                    pcts.CD_TimeSeriesSegment.GetReference(), mm))
                {
                    TimeSeriesAddSegmentData(pcts, mm, tsd, smcseg);
                }
            }

            ;
        }        

        public void SafelyRefreshRenderedTimeSeries()
        {
            try
            {
                // redisplay
                _timeSeriesData?.RefreshRenderedTimeSeries(StackPanelTimeSeries, theDefaultLang, theLog);
            }
            catch (Exception ex)
            {
                theLog?.Error(ex, "AasxPluginPlotting: refresh display of time series data");
            }
        }

        private AasEventMsgStack _eventStack = new AasEventMsgStack();

        public void PushEvent(AasEventMsgEnvelope ev)
            => _eventStack?.PushEvent(ev);

        private void ProcessEvents()
        {
            // need prefs
            var pcts = AasxPredefinedConcepts.ZveiTimeSeriesDataV10.Static;
            var mm = AdminShell.Key.MatchMode.Relaxed;

            // process all events
            while (true)
            {
                var ev = _eventStack.PopEvent();
                if (ev == null)
                    return;

                // TODO: search for updated values and display them in the 'old' plots

                // search for updated value arrays and re-display the data set

                var changedGroups = new Dictionary<ListOfPlotItem.PlotItemGroup, int>();

                foreach (var pl in ev.PayloadItems)
                    if (pl is AasPayloadUpdateValue uv && uv.Values != null)
                        foreach (var uvi in uv.Values)
                            PlotItems.UpdateMatchingPlotItemsFrom(
                                PlotItems.RenderedGroups, changedGroups, uvi, ev.Timestamp, theDefaultLang);

                foreach (var grp in changedGroups.Keys)
                    PlotItems.UpdateFinalGroup(grp);

                // search for structural creation of portions of segments
                // this is to trigger, if NEW SEGMENTS exist

                if (!ev.IsWellInformed || ev.PayloadItems == null)
                    return;

                var segmentsToInspect = new List<AdminShell.SubmodelElementCollection>();

                foreach (var pl in ev.PayloadItems)
                    if (pl is AasPayloadStructuralChange sc && sc.Changes != null)
                        foreach (var sci in sc.Changes)
                        {
                            if (sci.Reason != StructuralChangeReason.Create
                                || sci.FoundReferable == null)
                                continue;

                            // find the segment

                            var smcseg = ((sci.FoundReferable as AdminShell.SubmodelElement)
                                .FindAllParentsWithSemanticId(new AdminShell.SemanticId(
                                    pcts.CD_TimeSeriesSegment.GetReference()),
                                    includeThis: true)
                                .FirstOrDefault()) as AdminShell.SubmodelElementCollection;
                            if (smcseg == null)
                                continue;

                            // remember

                            if (!segmentsToInspect.Contains(smcseg))
                                segmentsToInspect.Add(smcseg);
                        }

                // now check the newly discovered sigmentsd
                foreach (var smcseg in segmentsToInspect)
                {
                    // access
                    if (smcseg == null)
                        continue;

                    // the segments needs to be situated in a time series
                    var smcts = (smcseg.FindAllParentsWithSemanticId(new AdminShell.SemanticId(
                                    pcts.CD_TimeSeries.GetReference()),
                                    includeThis: false)
                                .FirstOrDefault()) as AdminShell.SubmodelElementCollection;
                    if (smcts == null)
                        continue;

                    // find the data for it
                    var tsd = _timeSeriesData.FindDataSetBySource(smcts);

                    if (tsd == null)
                    {
                        // TODO
                        return;
                        throw new NotImplementedException("AasxPlugPlotting::PushEvent() does not allow new time series");
                    }

                    // ok, add data to time series
                    TimeSeriesAddSegmentData(pcts, mm, tsd, smcseg);

                    // redisplay
                    _timeSeriesData?.RefreshRenderedTimeSeries(StackPanelTimeSeries, theDefaultLang, theLog);
                }
            }

        }

    }
}
