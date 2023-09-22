/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using System.Windows;
using System.Diagnostics.Metrics;
using System.Drawing.Drawing2D;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace AasxPluginBomStructure
{

    /// <summary>
    /// This set of static functions lay-out a graph according to the package information.
    /// Right now, no domain-specific lay-out.
    /// </summary>
    public class GenericBomControl
    {
        private AdminShellPackageEnv _package;
        private Aas.Submodel _submodel;
        private bool _createOnPackage = false;

        private Microsoft.Msagl.Drawing.Graph theGraph = null;
        private Microsoft.Msagl.WpfGraphControl.GraphViewer theViewer = null;
        private Aas.IReferable theReferable = null;

        private PluginEventStack eventStack = null;

        private BomStructureOptionsRecordList _bomRecords = new BomStructureOptionsRecordList();

        private GenericBomCreatorOptions _creatorOptions = new GenericBomCreatorOptions();

        private Dictionary<Aas.IReferable, GenericBomCreatorOptions> preferredPreset =
            new Dictionary<Aas.IReferable, GenericBomCreatorOptions>();

        private BomStructureOptions _bomOptions = new BomStructureOptions();

        public void SetEventStack(PluginEventStack es)
        {
            this.eventStack = es;
        }

        protected WrapPanel CreateTopPanel()
        {
            // create TOP controls
            var wpTop = new WrapPanel();
            wpTop.Orientation = Orientation.Horizontal;

            // style

            wpTop.Children.Add(new Label() { Content = "Layout style: " });

            var cbli = new ComboBox()
            {
                Margin = new Thickness(0, 0, 0, 5)
            };
            foreach (var psn in this.PresetSettingNames)
                cbli.Items.Add(psn);
            cbli.SelectedIndex = _creatorOptions.LayoutIndex;
            cbli.SelectionChanged += (s3, e3) =>
            {
                _creatorOptions.LayoutIndex = cbli.SelectedIndex;
                RememberSettings();
                RedrawGraph();
            };
            wpTop.Children.Add(cbli);

            // spacing

            wpTop.Children.Add(new Label() { Content = "Spacing: " });

            var sli = new Slider()
            {
                Orientation = Orientation.Horizontal,
                Width = 150,
                Minimum = 1,
                Maximum = 300,
                TickFrequency = 10,
                IsSnapToTickEnabled = true,
                Value = _creatorOptions.LayoutSpacing,
                Margin = new System.Windows.Thickness(10, 0, 10, 5),
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            sli.ValueChanged += (s, e) =>
            {
                _creatorOptions.LayoutSpacing = e.NewValue;
                RememberSettings();
                RedrawGraph();
            };
            wpTop.Children.Add(sli);

            // Compact labels

            var cbcomp = new CheckBox()
            {
                Content = "Compact labels",
                Margin = new System.Windows.Thickness(10, 0, 10, 5),
                VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                IsChecked = _creatorOptions.CompactLabels,
            };
            RoutedEventHandler cbcomb_changed = (s2, e2) =>
            {
                _creatorOptions.CompactLabels = cbcomp.IsChecked == true;
                RememberSettings();
                RedrawGraph();
            };
            cbcomp.Checked += cbcomb_changed;
            cbcomp.Unchecked += cbcomb_changed;
            wpTop.Children.Add(cbcomp);

            // show asset ids

            var cbaid = new CheckBox()
            {
                Content = "Show Asset ids",
                Margin = new System.Windows.Thickness(10, 0, 10, 5),
                VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                IsChecked = _creatorOptions.CompactLabels,
            };
            RoutedEventHandler cbaid_changed = (s2, e2) =>
            {
                _creatorOptions.ShowAssetIds = cbaid.IsChecked == true;
                RememberSettings();
                RedrawGraph();
            };
            cbaid.Checked += cbaid_changed;
            cbaid.Unchecked += cbaid_changed;
            wpTop.Children.Add(cbaid);

            // "select" button

            var btnSelect = new Button()
            {
                Content = "Selection \U0001f846 tree",
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(4, 0, 4, 0)
            };
            btnSelect.Click += (s3, e3) =>
            {
                // check for marked entities
                var markedRf = new List<Aas.IReferable>();

                if (theViewer != null)
                    foreach (var vn in theViewer.GetViewerNodes())
                        if (vn.MarkedForDragging && vn.Node?.UserData is Aas.IReferable rf)
                            markedRf.Add(rf);

                if (markedRf.Count < 1)
                    return;

                // send event to main application
                var evt = new AasxPluginResultEventVisualSelectEntities()
                {
                    Referables = markedRf
                };
                this.eventStack.PushEvent(evt);
            };
            wpTop.Children.Add(btnSelect);

            // return

            return wpTop;
        }

        public object FillWithWpfControls(
            BomStructureOptions bomOptions,
            object opackage, object osm, object masterDockPanel)
        {
            // access
            _package = opackage as AdminShellPackageEnv;
            _submodel = osm as Aas.Submodel;
            _createOnPackage = false;
            _bomOptions = bomOptions;
            var master = masterDockPanel as DockPanel;
            if (_bomOptions == null || _package == null || _submodel == null || master == null)
                return null;

            // set of records helping layouting
            _bomRecords = new BomStructureOptionsRecordList(
                _bomOptions.LookupAllIndexKey<BomStructureOptionsRecord>(
                    _submodel.SemanticId?.GetAsExactlyOneKey()));

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

            // already user defined?
            if (preferredPreset != null && preferredPreset.ContainsKey(_submodel))
                _creatorOptions = preferredPreset[_submodel].Copy();

            // the Submodel elements need to have parents
            _submodel.SetAllParents();

            // create controls
            var spTop = CreateTopPanel();
            DockPanel.SetDock(spTop, Dock.Top);
            master.Children.Add(spTop);

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

            // return viewer for advanced manipulation
            return viewer;
        }

        public object CreateViewPackageReleations(
            BomStructureOptions bomOptions,
            object opackage,
            DockPanel master)
        {
            // access
            _package = opackage as AdminShellPackageEnv;
            _submodel = null;
            _createOnPackage = true;
            _bomOptions = bomOptions;
            if (_bomOptions == null || _package?.AasEnv == null)
                return null;

            // new master panel
            // dead-csharp off
            // var master = new DockPanel();
            // dead-csharp on

            // clear some other members (GenericBomControl is not allways created new)
            _creatorOptions = new GenericBomCreatorOptions();

            // index all submodels
            foreach (var sm in _package.AasEnv.OverSubmodelsOrEmpty())
                sm.SetAllParents();

            // create controls
            var spTop = CreateTopPanel();
            DockPanel.SetDock(spTop, Dock.Top);
            master.Children.Add(spTop);

            // create BOTTOM controls
            var legend = GenericBomCreator.GenerateWrapLegend();
            DockPanel.SetDock(legend, Dock.Bottom);
            master.Children.Add(legend);

            // set default for very small edge label size
            Microsoft.Msagl.Drawing.Label.DefaultFontSize = 6;

            // make a Dock panel (within)
            var dp = new DockPanel();
            dp.ClipToBounds = true;
            dp.MinWidth = 10;
            dp.MinHeight = 10;

            // very important: add first the panel, then add graph
            master.Children.Add(dp);

            // graph
            var graph = CreateGraph(_package, null, _creatorOptions, createOnPackage: _createOnPackage);

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

            // return viewer for advanced manipulation
            // dead-csharp off
            // return viewer;
            // dead-csharp on

            // return master
            return master;
        }

        private Microsoft.Msagl.Drawing.Graph CreateGraph(
            AdminShellPackageEnv env,
            Aas.Submodel sm,
            GenericBomCreatorOptions options,
            bool createOnPackage = false)
        {
            // access   
            if (env?.AasEnv == null || (sm == null && !createOnPackage) || options == null)
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

            // Turn on logging if required
            //// using (var tw = new StreamWriter("bomgraph.log"))
            {
                if (!createOnPackage)
                {
                    // just one Submodel
                    creator.RecurseOnLayout(1, graph, null, sm.SubmodelElements, 1, null);
                    creator.RecurseOnLayout(2, graph, null, sm.SubmodelElements, 1, null);
                    creator.RecurseOnLayout(3, graph, null, sm.SubmodelElements, 1, null);
                }
                else
                {
                    for (int pass = 1; pass <= 3; pass++)
                        foreach (var sm2 in env.AasEnv.OverSubmodelsOrEmpty())
                        {
                            // create AAS and SM
                            if (pass == 1)
                                creator.CreateAasAndSubmodelNodes(graph, sm2);

                            // modify creator's bomRecords on the fly
                            var recs = new BomStructureOptionsRecordList(
                                _bomOptions.LookupAllIndexKey<BomStructureOptionsRecord>(
                                    sm2.SemanticId?.GetAsExactlyOneKey()));
                            creator.SetRecods(recs);

                            // graph itself
                            creator.RecurseOnLayout(pass, graph, null, sm2.SubmodelElements, 1, null,
                                entityParentRef: sm2);
                        }
                }
            }

            // make default or (already) preferred settings
            var settings = GivePresetSettings(options, graph.NodeCount);
            if (this.preferredPreset != null && sm != null
                && this.preferredPreset.ContainsKey(sm))
                settings = GivePresetSettings(this.preferredPreset[sm], graph.NodeCount);
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
                        if (us is Aas.IReferable)
                        {
                            // make event
                            var refs = new List<Aas.IKey>();
                            (us as Aas.IReferable).CollectReferencesByParent(refs);

                            // ok?
                            if (refs.Count > 0)
                            {
                                var evt = new AasxPluginResultEventNavigateToReference();
                                evt.targetReference = ExtendReference.CreateNew(refs);
                                this.eventStack.PushEvent(evt);
                            }
                        }

                        if (us is Aas.Reference)
                        {
                            var evt = new AasxPluginResultEventNavigateToReference();
                            evt.targetReference = (us as Aas.Reference);
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
            "2 | Round layout (variable)",
        };

        private Microsoft.Msagl.Core.Layout.LayoutAlgorithmSettings GivePresetSettings(
            GenericBomCreatorOptions opt, int nodeCount)
        {
            if (opt == null || opt.LayoutIndex == 0)
            {
                // Tree
                var settings = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings();
                return settings;
            }
            else
            {
                // Round
                var settings = new Microsoft.Msagl.Layout.Incremental.FastIncrementalLayoutSettings();
                settings.RepulsiveForceConstant = 8.0 / (1.0 + nodeCount) * (1.0 + opt.LayoutSpacing);
                return settings;
            }
        }

        protected void RememberSettings()
        {
            // try to remember preferred setting
            if (this.theReferable != null && preferredPreset != null && _creatorOptions != null)
                this.preferredPreset[this.theReferable] = _creatorOptions.Copy();
        }

        protected void RedrawGraph()
        {
            try
            {
                // re-draw (brutally)
                theGraph = CreateGraph(_package, _submodel, _creatorOptions, createOnPackage: _createOnPackage);

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
