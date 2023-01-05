/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// #define TESTMODE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using AasxIntegrationBase;
using AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxPluginBomStructure
{

    /// <summary>
    /// This set of static functions lay-out a graph according to the package information.
    /// Right now, no domain-specific lay-out.
    /// </summary>
    public class GenericBomControl
    {
        private AdminShellPackageEnv _package;
        private Submodel _submodel;

        private Microsoft.Msagl.Drawing.Graph theGraph = null;
        private Microsoft.Msagl.WpfGraphControl.GraphViewer theViewer = null;
        private AasCore.Aas3_0_RC02.IReferable theReferable = null;

        private PluginEventStack eventStack = null;

        private Dictionary<AasCore.Aas3_0_RC02.IReferable, int> preferredPresetIndex =
            new Dictionary<AasCore.Aas3_0_RC02.IReferable, int>();

        private BomStructureOptionsRecordList _bomRecords = new BomStructureOptionsRecordList();

        private GenericBomCreatorOptions _creatorOptions = new GenericBomCreatorOptions();

        private BomStructureOptions _bomOptions = new BomStructureOptions();

        public void SetEventStack(PluginEventStack es)
        {
            this.eventStack = es;
        }

        public object FillWithWpfControls(
            BomStructureOptions bomOptions,
            object opackage, object osm, object masterDockPanel)
        {
            // access
            _package = opackage as AdminShellPackageEnv;
            _submodel = osm as Submodel;
            _bomOptions = bomOptions;
            var master = masterDockPanel as DockPanel;
            if (_bomOptions == null || _package == null || _submodel == null || master == null)
                return null;

            // set of records helping layouting
            _bomRecords = new BomStructureOptionsRecordList(_bomOptions?.MatchingRecords(_submodel.SemanticId));

            // clear some other members (GenericBomControl is not allways created new)
            _creatorOptions = new GenericBomCreatorOptions();

            // apply some global options?
            foreach (var br in _bomRecords)
            {
                if (br.Layout >= 1 && br.Layout <= PresetSettingNames.Length)
                    _creatorOptions.LayoutIndex = br.Layout - 1;
                if (br.Compact.HasValue)
                    _creatorOptions.CompactLabels = br.Compact.Value;
            }

            // the Submodel elements need to have parents
            _submodel.SetAllParents();

            // prepare options for fast access
            _bomOptions.Index();

            // create TOP controls
            var spTop = new StackPanel();
            spTop.Orientation = Orientation.Horizontal;
            DockPanel.SetDock(spTop, Dock.Top);
            master.Children.Add(spTop);

            var lb1 = new Label();
            lb1.Content = "Layout style: ";
            spTop.Children.Add(lb1);

            var cbli = new ComboBox();
            foreach (var psn in this.PresetSettingNames)
                cbli.Items.Add(psn);
            cbli.SelectedIndex = _creatorOptions.LayoutIndex;
            cbli.SelectionChanged += CbLayoutIndex_SelectionChanged;
            spTop.Children.Add(cbli);

            var cbcomp = new CheckBox();
            cbcomp.Content = "Compact labels";
            cbcomp.Margin = new System.Windows.Thickness(10, 0, 10, 0);
            cbcomp.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
            cbcomp.IsChecked = _creatorOptions.CompactLabels;
            cbcomp.Checked += CbCompactLabels_CheckedChanged;
            cbcomp.Unchecked += CbCompactLabels_CheckedChanged;
            spTop.Children.Add(cbcomp);

            // create BOTTOM controls
            var legend = GenericBomCreator.GenerateWrapLegend();
            DockPanel.SetDock(legend, Dock.Bottom);
            master.Children.Add(legend);

            // set default for very small edge label size
            Microsoft.Msagl.Drawing.Label.DefaultFontSize = 6;

            // make a Dock panel
            var dp = new DockPanel();
            dp.ClipToBounds = true;
            dp.MinWidth = 10;
            dp.MinHeight = 10;

            // very important: add first the panel, then add graph
            master.Children.Add(dp);

            // graph
            var graph = CreateGraph(_package, _submodel, _creatorOptions);

            // very important: first bind it, then add graph
            var viewer = new Microsoft.Msagl.WpfGraphControl.GraphViewer();
            viewer.BindToPanel(dp);
            viewer.MouseDown += Viewer_MouseDown;
            viewer.MouseMove += Viewer_MouseMove;
            viewer.MouseUp += Viewer_MouseUp;
            viewer.ObjectUnderMouseCursorChanged += Viewer_ObjectUnderMouseCursorChanged;
            viewer.Graph = graph;

            // make it re-callable
            theGraph = graph;
            theViewer = viewer;
            theReferable = _submodel;

            // return viewer for advanced manilulation
            return viewer;
        }

        private Microsoft.Msagl.Drawing.Graph CreateGraph(
            AdminShellPackageEnv env,
            Submodel sm,
            GenericBomCreatorOptions options)
        {
            // access   
            if (env == null || sm == null || options == null)
                return null;

            //create a graph object
            Microsoft.Msagl.Drawing.Graph graph = new Microsoft.Msagl.Drawing.Graph("BOM-graph");

#if TESTMODE
            //create the graph content
            graph.AddEdge("A", "B");
            var e1 = graph.AddEdge("B", "C");
            e1.Attr.ArrowheadAtSource = Microsoft.Msagl.Drawing.ArrowStyle.None;
            e1.Attr.ArrowheadAtTarget = Microsoft.Msagl.Drawing.ArrowStyle.None;
            e1.Attr.Color = Microsoft.Msagl.Drawing.Color.Magenta;
            e1.GeometryEdge = new Microsoft.Msagl.Core.Layout.Edge();
            // e1.LabelText = "Dumpf!";
            e1.LabelText = "hbhbjhbjhb";
            // e1.Label = new Microsoft.Msagl.Drawing.Label("Dumpf!!");
            graph.AddEdge("A", "C").Attr.Color = Microsoft.Msagl.Drawing.Color.Green;
            graph.FindNode("A").Attr.FillColor = Microsoft.Msagl.Drawing.Color.Magenta;
            graph.FindNode("B").Attr.FillColor = Microsoft.Msagl.Drawing.Color.MistyRose;
            Microsoft.Msagl.Drawing.Node c = graph.FindNode("C");
            graph.FindNode("B").LabelText = "HalliHallo";
            c.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PaleGreen;
            c.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Diamond;
            c.Label.FontSize = 28;

#else

            var creator = new GenericBomCreator(
                env?.AasEnv,
                _bomRecords,
                options);

            using (var tw = new StreamWriter("bomgraph.log"))
            {
                creator.RecurseOnLayout(1, graph, null, sm.SubmodelElements, 1, tw);
                creator.RecurseOnLayout(2, graph, null, sm.SubmodelElements, 1, tw);
                creator.RecurseOnLayout(3, graph, null, sm.SubmodelElements, 1, tw);
            }

            // make default or (already) preferred settings
            var settings = GivePresetSettings(options.LayoutIndex);
            if (this.preferredPresetIndex != null && this.preferredPresetIndex.ContainsKey(sm))
                settings = GivePresetSettings(this.preferredPresetIndex[sm]);
            if (settings != null)
                graph.LayoutAlgorithmSettings = settings;

#endif
            return graph;
        }

        private void Viewer_ObjectUnderMouseCursorChanged(
            object sender, Microsoft.Msagl.Drawing.ObjectUnderMouseCursorChangedEventArgs e)
        {
        }

        private void Viewer_MouseUp(object sender, Microsoft.Msagl.Drawing.MsaglMouseEventArgs e)
        {
        }

        private void Viewer_MouseMove(object sender, Microsoft.Msagl.Drawing.MsaglMouseEventArgs e)
        {
        }

        private void Viewer_MouseDown(object sender, Microsoft.Msagl.Drawing.MsaglMouseEventArgs e)
        {
            if (e != null && e.Clicks > 1 && e.LeftButtonIsPressed && theViewer != null && this.eventStack != null)
            {
                // double-click detected, can access the viewer?
                try
                {
                    var x = theViewer.ObjectUnderMouseCursor;
                    if (x != null && x.DrawingObject != null && x.DrawingObject.UserData != null)
                    {
                        var us = x.DrawingObject.UserData;
                        if (us is AasCore.Aas3_0_RC02.IReferable)
                        {
                            // make event
                            var refs = new List<AasCore.Aas3_0_RC02.Key>();
                            (us as AasCore.Aas3_0_RC02.IReferable).CollectReferencesByParent(refs);

                            // ok?
                            if (refs.Count > 0)
                            {
                                var evt = new AasxPluginResultEventNavigateToReference();
                                evt.targetReference = ExtendReference.CreateNew(refs);
                                this.eventStack.PushEvent(evt);
                            }
                        }

                        if (us is AasCore.Aas3_0_RC02.Reference)
                        {
                            var evt = new AasxPluginResultEventNavigateToReference();
                            evt.targetReference = (us as AasCore.Aas3_0_RC02.Reference);
                            this.eventStack.PushEvent(evt);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }
        }

        private string[] PresetSettingNames =
        {
            "1 | Tree style layout",
            "2 | Round layout (no spacing)",
            "3 | Round layout (middle spacing)",
            "4 | Round layout (large spacing)"
        };

        private Microsoft.Msagl.Core.Layout.LayoutAlgorithmSettings GivePresetSettings(int i)
        {
            if (i == 0)
            {
                var settings = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings();
                return settings;
            }

            if (i == 1)
            {
                var settings = new Microsoft.Msagl.Layout.Incremental.FastIncrementalLayoutSettings();
                settings.RepulsiveForceConstant = 1.0;
                return settings;
            }

            if (i == 2)
            {
                var settings = new Microsoft.Msagl.Layout.Incremental.FastIncrementalLayoutSettings();
                settings.RepulsiveForceConstant = 30.0;
                return settings;
            }

            if (i == 3)
            {
                var settings = new Microsoft.Msagl.Layout.Incremental.FastIncrementalLayoutSettings();
                settings.RepulsiveForceConstant = 100.0;
                return settings;
            }

            return null;
        }

        private void CbCompactLabels_CheckedChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                // re-draw (brutally)
                _creatorOptions.CompactLabels = cb.IsChecked == true;
                theGraph = CreateGraph(_package, _submodel, _creatorOptions);
                theViewer.Graph = null;
                theViewer.Graph = theGraph;
            }
        }

        private void CbLayoutIndex_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null || theGraph == null || theViewer == null)
                return;

            try
            {
                // try to remember preferred setting
                if (this.theReferable != null && preferredPresetIndex != null && cb.SelectedIndex >= 0)
                    this.preferredPresetIndex[this.theReferable] = cb.SelectedIndex;

                // re-draw (brutally)
                _creatorOptions.LayoutIndex = cb.SelectedIndex;
                theGraph = CreateGraph(_package, _submodel, _creatorOptions);
                theViewer.Graph = null;
                theViewer.Graph = theGraph;
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }
        }
    }
}
