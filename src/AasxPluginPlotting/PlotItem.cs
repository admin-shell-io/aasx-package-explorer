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

        public PlotBufferFixLen Buffer;

        public DateTime? LastUpdate;

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

        private string _unit = "";
        public string DisplayUnit
        {
            get { return _unit; }
            set { _unit = value; OnPropertyChanged("DisplayUnit"); }
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

        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public int RowSpan { get; set; }
        public int ColumnSpan { get; set; }

        private Brush _valueForeground = Brushes.Black;
        public Brush ValueForeground
        {
            get { return _valueForeground; }
            set { _valueForeground = value; OnPropertyChanged("DisplayValue"); }
        }

        public PlotItem() { }

        public PlotItem(AdminShell.SubmodelElement sme, string args,
            string path, AdminShell.Description description, string lang)
        {
            SME = sme;
            ArgsStr = args;
            _path = path;
            _description = description;
            _displayDescription = description?.GetDefaultStr(lang);
            Args = PlotArguments.Parse(ArgsStr);
        }

        public void SetValue(double? dbl, string lang)
        {
            if (dbl.HasValue && Args?.fmt != null)
            {
                //try
                //{
                    DisplayValue = "" + dbl.Value.ToString(Args.fmt, CultureInfo.InvariantCulture);
                //}
                //catch (Exception ex)
                //{
                //    DisplayValue = "<fmt error>";
                //    LogInternally.That.SilentlyIgnoredError(ex);
                //}
            }
            else
                DisplayValue = "" + SME.ValueAsText(lang);
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
            return _path.CompareTo(other._path);
        }

        public void ResetRowCol(PlotArguments args = null)
        {
            RowIndex = -1;
            ColumnIndex = -1;
            RowSpan = 1;
            ColumnSpan = 1;

            if (args?.row != null)
                RowIndex = Math.Max(0, args.row.Value);

            if (args?.col != null)
                ColumnIndex = Math.Max(0, args.col.Value);

            if (args?.rowspan != null)
                RowSpan = Math.Max(1, args.rowspan.Value);

            if (args?.colspan != null)
                ColumnSpan = Math.Max(1, args.colspan.Value);
        }

        public bool RowColValid()
        {
            return RowIndex >= 0 && ColumnIndex >= 0
                && RowSpan >= 1 && ColumnSpan >= 1;
        }
    }

    public class RowColTuple
    {
        public int Rows { get; set; }
        public int Cols { get; set; }
    }

    public class ListOfPlotItem : ObservableCollection<PlotItem>
    {
        public RowColTuple GetMaxRowCol()
        {
            var res = new RowColTuple() { Rows = -1, Cols = -1 };
            foreach (var it in this)
            {
                if (!it.RowColValid())
                    continue;
                res.Rows = Math.Max(res.Rows, it.RowIndex + it.RowSpan - 1);
                res.Cols = Math.Max(res.Cols, it.ColumnIndex + it.ColumnSpan - 1);
            }
            return res;
        }

        public int GetMaxColForRow(int row)
        {
            var res = -1;
            foreach (var it in this)
                if (it.RowIndex <= row && it.RowIndex + it.RowSpan > row)
                    res = Math.Max(res, it.ColumnIndex + it.ColumnSpan - 1);
            return res;
        }

        public RowColTuple ReAssignItemTiles(int defaultCols = 4)
        {
            // first clear all items
            foreach (var it in this)
                it.ResetRowCol(it.Args);

            // get the fix assigned maximums
            var fixmax = GetMaxRowCol();
            if (fixmax.Rows < 1 || fixmax.Cols < 1)
            {
                // default
                fixmax.Rows = 1;
                fixmax.Cols = defaultCols;
            }
            fixmax.Rows = Math.Max(1, fixmax.Rows);
            fixmax.Cols = Math.Max(1, fixmax.Cols);

            // try reassign un-assigned items
            var lastRow = fixmax.Rows - 1;
            foreach (var it in this)
            {
                // already done?
                if (it.RowColValid())
                    continue;

                // find a column in the currently last row
                var lastCol = GetMaxColForRow(lastRow);

                // already to wrap?
                if (lastCol >= fixmax.Cols - 1)
                {
                    lastRow++;
                    lastCol = 0;
                }
                else
                {
                    lastCol = lastCol + 1;
                }

                // assign
                it.RowIndex = lastRow;
                it.ColumnIndex = lastCol;
            }

            // ok, eval in total
            var res = GetMaxRowCol();
            res.Rows = Math.Max(1, res.Rows + 1);
            res.Cols = Math.Max(1, res.Cols + 1);
            return res;
        }

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
                    it.SetValue(dbl, lang);
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
                    return true;

                // select for SME type
                /* TODO (MIHO, 2021-01-04): consider at least to include MLP, as well */
                if (!(sme is AdminShell.Property))
                    return true;

                // build path
                var path = sme.idShort;
                if (parents != null)
                    foreach (var par in parents)
                        path = "" + par.idShort + " / " + path;

                // add
                var npi = new PlotItem(sme, "" + q.value, path, sme.description, lang);
                npi.SetValue(sme.ValueAsDouble(), lang);
                temp.Add(npi);

                // re-adjust
                if (npi.Args?.unit != null && !npi.DisplayUnit.HasContent())
                    npi.DisplayUnit = npi.Args.unit;

                // recurse
                return true;
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
            public bool UseOAaxis = false;
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

        public List<PlotItemGroup> RenderedGroups;               

        public List<PlotItemGroup> RenderAllGroups(StackPanel panel, double defPlotHeight)
        {
            // first access
            var res = new List<PlotItemGroup>();
            if (panel == null)
                return null;
            panel.Children.Clear();

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
                wpfPlot.Tag = pvc;

                // initial state
                PlotHelpers.SetOverallPlotProperties(pvc, wpfPlot, null, defPlotHeight);

                // before applying arguments
                pvc.AutoScaleX = true;
                pvc.AutoScaleY = true;

                // figure out type of x axis
                foreach (var pi in groupPI)
                    if (pi.Args != null && pi.Args.src == PlotArguments.Source.Event)
                        groupPI.UseOAaxis = true;

                // some basic attributes
                lastPlot = wpfPlot;
                // wpfPlot.Plot.AntiAlias(false, false, false);
                wpfPlot.AxesChanged += (s, e) => WpfPlot_AxisChanged(wpfPlot, e);
                wpfPlot.MouseDown += (s2, e2) =>
                {
                    _userAxisChangeEnabled = true;
                };
                wpfPlot.MouseMove += WpfPlot_MouseMove;
                groupPI.WpfPlot = wpfPlot;
                res.Add(groupPI);

                // for all wpf / all signals
                pvc.Height = defPlotHeight;
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
                    var pb = new PlotBufferFixLen();
                    pi.Buffer = pb;

                    // factory new Plottable
                    if (!groupPI.UseOAaxis)
                    {
                        // sample based
                        pb.Push(val.HasValue ? val.Value : 0.0);
                        var signal = wpfPlot.Plot.AddSignal(pb.Ydata, label: "" + pi.DisplayPath);
                        PlotHelpers.SetPlottableProperties(signal, pi.Args);
                        pb.Plottable = signal;                        
                        if (signal.FillColor1.HasValue)
                            pi.ValueForeground = PlotHelpers.BrushFrom(signal.FillColor1.Value);
                    }
                    else
                    {
                        // time based
                        var scatter = wpfPlot.Plot.AddScatter(pb.Xdata, pb.Ydata, label: "" + pi.DisplayPath);
                        PlotHelpers.SetPlottableProperties(scatter, pi.Args);
                        pb.Plottable = scatter;
                        pi.ValueForeground = PlotHelpers.BrushFrom(scatter.Color);
                    }

                }

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

                // enable time axis
                if (groupPI.UseOAaxis)
                    wpfPlot.Plot.XAxis.DateTimeFormat(true);

                // render the plot into panel
                panel.Children.Add(pvc);
                wpfPlot.Render();
            }

            // for the last plot ..
            if (lastPlot != null)
            {
                lastPlot.Plot.XLabel("Samples / Time");
            }

            // return groups for notice
            return res;
        }

        public void ForAllGroupsAndPlot(
            List<PlotItemGroup> groups,
            Action<PlotItemGroup, IWpfPlotViewControl, PlotItem> lambda = null)
        {
            if (groups == null)
                return;
            foreach (var grp in groups)
            {
                if (grp == null)
                    continue;
                var pvc = grp?.WpfPlot?.Tag as IWpfPlotViewControl;
                foreach (var pi in grp)
                    lambda?.Invoke(grp, pvc, pi);
            }
        }

        private void WpfPlot_ButtonClicked(IWpfPlotViewControl sender, int ndx)
        {
            // access
            var wpfPlot = sender?.WpfPlot;
            if (wpfPlot == null)
                return;

            if (!(sender is WpfPlotViewControlHorizontal pvc))
                return;

            if (ndx == 1 || ndx == 2)
            {
                // Horizontal scale Plus / Minus
                var ax = wpfPlot.Plot.GetAxisLimits();
                var width = Math.Abs(ax.XMax - ax.XMin);

                if (ndx == 1)
                    wpfPlot.Plot.SetAxisLimits(xMin: ax.XMin - width / 2, xMax: ax.XMax + width / 2);

                if (ndx == 2)
                    wpfPlot.Plot.SetAxisLimits(xMin: ax.XMin + width / 4, xMax: ax.XMax - width / 4);

                // no autoscale for X
                ForAllGroupsAndPlot(RenderedGroups, (grp, opvc, pi) => 
                { 
                    if (opvc != null) 
                        opvc.AutoScaleX = false; 
                });

                // call for the other
                ApplyLimitsToAllOtherLimitsOf(wpfPlot);
            }

            if (ndx == 3 || ndx == 4)
            {
                // Vertical scale Plus / Minus
                var ax = wpfPlot.Plot.GetAxisLimits();
                var height = Math.Abs(ax.YMax - ax.YMin);

                if (ndx == 3)
                    wpfPlot.Plot.SetAxisLimits(yMin: ax.YMin - height / 2, yMax: ax.YMax + height / 2);

                if (ndx == 4)
                    wpfPlot.Plot.SetAxisLimits(yMin: ax.YMin + height / 4, yMax: ax.YMax - height / 4);

                // no autoscale for Y
                ForAllGroupsAndPlot(RenderedGroups, (grp, opvc, pi) =>
                {
                    if (opvc != null)
                        opvc.AutoScaleX = false;
                });

                // call for the other
                ApplyLimitsToAllOtherLimitsOf(wpfPlot);
            }

            if (ndx == 5)
            {
                // switch auto scale ON and hope the best
                sender.AutoScaleX = true;
                sender.AutoScaleY = true;
            }

            if (ndx == 6)
            {
                // plot larger
                var h = pvc.ActualHeight + 100;
                pvc.Height = h;
                pvc.MinHeight = h;
                pvc.MaxHeight = h;
            }

            if (ndx == 7 && pvc.Height >= 299)
            {
                // plot smaller
                var h = pvc.ActualHeight - 100;
                pvc.Height = h;
                pvc.MinHeight = h;
                pvc.MaxHeight = h;
            }
        }

        private bool _userAxisChangeEnabled = false;

        private void WpfPlot_AxisChanged(object sender, EventArgs e)
        {
        }

        private void ApplyLimitsToAllOtherLimitsOf(
            ScottPlot.WpfPlot wpfPlot)
        {
            ForAllGroupsAndPlot(RenderedGroups, (grp, pvc, pi) =>
            {
                var one = wpfPlot.Plot.GetAxisLimits();
                if (grp.WpfPlot != wpfPlot && grp.WpfPlot != null)
                {
                    // move
                    grp.WpfPlot.Plot.SetAxisLimits(xMin: one.XMin, xMax: one.XMax);
                    grp.WpfPlot.Render();
                }
            });
        }

        private void WpfPlot_MouseMove(object sender, MouseEventArgs e)
        {
            // Note: this function originally was the WpfPlot_AxisChanged handler; however
            // this handler was frequently triggered by the widget system or axis change and
            // not by the user. So, the functionality was moved to the mouse handler
            if (!(e.LeftButton == MouseButtonState.Pressed))
                return;

            if (!(sender is ScottPlot.WpfPlot wpfPlot))
                return;

            if (!(wpfPlot.Tag is WpfPlotViewControlHorizontal pvc))
                return;

            // is the USER performed a axis move action
            if (_userAxisChangeEnabled)
            {
                if (pvc.AutoScaleX || pvc.AutoScaleY)
                {
                    pvc.AutoScaleX = false;
                    pvc.AutoScaleY = false;
                    ForAllGroupsAndPlot(RenderedGroups, (grp, opvc, pi) =>
                    {
                        // disable also there
                        if (opvc == null)
                            return;
                        opvc.AutoScaleX = false;
                        opvc.AutoScaleY = false;
                        var oldLimits = grp.WpfPlot.Plot.GetAxisLimits();
                        grp.WpfPlot.Plot.SetAxisLimits(xMax: oldLimits.XMax + 10);
                    });
                }

                ApplyLimitsToAllOtherLimitsOf(wpfPlot);
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
                    // do this NOT for event sources items
                    if (pi.Args != null && pi.Args.src == PlotArguments.Source.Event)
                        continue;

                    // add
                    var val = pi.SME?.ValueAsDouble();
                    if (grp.UseOAaxis)
                        pi.Buffer?.Push(
                            x: DateTime.UtcNow.ToOADate(),
                            y: val.HasValue ? val.Value : 0.0);
                    else
                        pi.Buffer?.Push(val.HasValue ? val.Value : 0.0);
                }

                if (!(grp.WpfPlot?.Tag is IWpfPlotViewControl pvc))
                    continue;

                // scale?
                if (pvc.AutoScaleX && pvc.AutoScaleY)
                    grp.WpfPlot?.Plot.AxisAuto();
                else
                if (pvc.AutoScaleX)
                    grp.WpfPlot?.Plot.AxisAutoX();
                else
                if (pvc.AutoScaleY)
                    grp.WpfPlot?.Plot.AxisAutoY();

                grp.WpfPlot?.Render(/* skipIfCurrentlyRendering: true */);
            }
        }

        public void UpdateMatchingPlotItemsFrom(
            List<PlotItemGroup> groups,
            Dictionary<PlotItemGroup,int> groupsToUpdate,
            AasPayloadUpdateValueItem uvi,
            DateTime timestamp,
            string lang)
        {
            // access
            if (groups == null || uvi == null)
                return;

            // foreach
            foreach (var grp in groups)
            {
                // access
                if (grp == null)
                    continue;

                // push for each item
                bool found = false;
                foreach (var pi in grp)
                {
                    // must be event source
                    if (pi.Args == null || pi.Args.src != PlotArguments.Source.Event)
                        continue;

                    // found a hit?
                    if (pi.SME != uvi.FoundReferable)
                        continue;                    

                    // use the value which was already set to the SME
                    // then try newer one
                    var val = pi.SME?.ValueAsDouble();
                    if (!val.HasValue)
                        continue;
                    if (uvi.Value is double vdbl)
                        val = vdbl;
                    if (uvi.Value is string vstr)
                        if (double.TryParse(vstr, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                        val = f;

                    // commit
                    found = true;

                    pi.SetValue(val, lang);

                    if (grp.UseOAaxis)
                        pi.Buffer?.Push(
                            x: timestamp.ToOADate(),
                            y: val.HasValue ? val.Value : 0.0);
                    else
                        pi.Buffer?.Push(val.HasValue ? val.Value : 0.0);
                }

                if (found && groupsToUpdate != null && !groupsToUpdate.ContainsKey(grp))
                    groupsToUpdate.Add(grp, 1);
            }
        }

        public void UpdateFinalGroup(
            PlotItemGroup grp)
        {
            // acess
            if (grp == null || !(grp.WpfPlot?.Tag is IWpfPlotViewControl pvc))
                return;

            // update
            // scale?
            if (pvc.AutoScaleX && pvc.AutoScaleY)
                grp.WpfPlot?.Plot.AxisAuto();
            else
            if (pvc.AutoScaleX)
                grp.WpfPlot?.Plot.AxisAutoX();
            else
            if (pvc.AutoScaleY)
                grp.WpfPlot?.Plot.AxisAutoY();

            grp.WpfPlot?.Render();
        }
    }
}
