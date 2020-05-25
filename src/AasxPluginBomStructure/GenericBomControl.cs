using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using AasxIntegrationBase;
using AdminShellNS;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The Newtonsoft.JSON serialization is licensed under the MIT License (MIT).
The Microsoft Microsoft Automatic Graph Layout, MSAGL, is licensed under the MIT license (MIT).
*/
namespace AasxPluginBomStructure
{
    public class GenericBomCreator
    {
        public static int WrapMaxColumn = 20;

        public static Microsoft.Msagl.Drawing.Color DefaultBorderColor = new Microsoft.Msagl.Drawing.Color(0, 0, 0);

        public static Microsoft.Msagl.Drawing.Color PropertyFillColor = new Microsoft.Msagl.Drawing.Color(89, 139, 209);
        public static Microsoft.Msagl.Drawing.Color PropertyBorderColor = new Microsoft.Msagl.Drawing.Color(12, 94, 209);

        public static Microsoft.Msagl.Drawing.Color AssetCoManagedColor = new Microsoft.Msagl.Drawing.Color(184, 194, 209);
        public static Microsoft.Msagl.Drawing.Color AssetSelfManagedColor = new Microsoft.Msagl.Drawing.Color(136, 166, 210);

        public static Microsoft.Msagl.Drawing.Color AssetFillColor = new Microsoft.Msagl.Drawing.Color(192, 192, 192);
        public static Microsoft.Msagl.Drawing.Color AssetBorderColor = new Microsoft.Msagl.Drawing.Color(128, 128, 128);

        private Dictionary<AdminShell.Referable, Microsoft.Msagl.Drawing.Node> referableToNode = new Dictionary<AdminShell.Referable, Microsoft.Msagl.Drawing.Node>();
        private Dictionary<AdminShell.Referable, AdminShell.RelationshipElement> referableByRelation = new Dictionary<AdminShell.Referable, AdminShell.RelationshipElement>();
        private AdminShell.AdministrationShellEnv env;
        private int maxNodeId = 1;

        private AasReferenceStore refStore = null;

        public GenericBomCreator(AdminShell.AdministrationShellEnv env)
        {
            this.env = env;
            this.refStore = new AasReferenceStore();
            this.refStore.Index(env);
        }

        public AdminShell.Referable FindReferableByReference(AdminShell.Reference r)
        {
            if (refStore == null)
                return this.env?.FindReferableByReference(r);
            return refStore.FindReferableByReference(r);
        }

        private string GenerateNodeID()
        {
            var res = String.Format("ID{0:00000}", this.maxNodeId++);
            return res;
        }

        public static System.Windows.Media.Brush ColorToBrush(Microsoft.Msagl.Drawing.Color c)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B));
        }

        public static Canvas GenerateLegendPart(int type, int column)
        {
            Canvas c = null;
            // 1 = Symbol
            if (column == 1)
            {
                c = new Canvas();
                c.Width = 30;
                c.Height = 30;
            }

            // 2 = Explanation
            TextBox tb = null;
            if (column == 2)
            {
                c = new Canvas();
                c.Width = 140;
                c.Height = 30;

                tb = new TextBox();
                tb.Width = 138;
                tb.Height = 28;
                tb.TextWrapping = System.Windows.TextWrapping.Wrap;
                tb.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                tb.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                tb.IsReadOnly = true;
                tb.BorderThickness = new System.Windows.Thickness(0);
                Canvas.SetLeft(tb, 0);
                Canvas.SetTop(tb, 0);
                c.Children.Add(tb);
            }

            // Self Managed
            if (column == 1 && c != null && type == 1)
            {
                var r = new System.Windows.Shapes.Rectangle();
                r.Width = 20;
                r.Height = 20;
                r.MinWidth = 20;
                r.MinHeight = 20;
                r.Fill = ColorToBrush(AssetSelfManagedColor);
                r.Stroke = ColorToBrush(DefaultBorderColor);
                r.Width = 2.0;
                r.RadiusX = 5;
                r.RadiusY = 5;
                Canvas.SetLeft(r, 5);
                Canvas.SetTop(r, 3);
                c.Children.Add(r);
            }
            if (column == 2 && tb != null && type == 1)
            {
                tb.Text = "Entity (self-managed)";
            }

            // Co Managed
            if (column == 1 && c != null && type == 2)
            {
                var r = new System.Windows.Shapes.Rectangle();
                r.Width = 20;
                r.Height = 20;
                r.MinWidth = 20;
                r.MinHeight = 20;
                r.Fill = ColorToBrush(AssetCoManagedColor);
                r.Stroke = ColorToBrush(DefaultBorderColor);
                r.Width = 2.0;
                r.RadiusX = 5;
                r.RadiusY = 5;
                Canvas.SetLeft(r, 5);
                Canvas.SetTop(r, 3);
                c.Children.Add(r);
            }
            if (column == 2 && tb != null && type == 2)
            {
                tb.Text = "Entity (co-managed)";
            }

            // Property
            if (column == 1 && c != null && type == 3)
            {
                var r = new System.Windows.Shapes.Rectangle();
                r.Width = 20;
                r.Height = 20;
                r.MinWidth = 20;
                r.MinHeight = 20;
                r.Fill = ColorToBrush(PropertyFillColor);
                r.Stroke = ColorToBrush(PropertyBorderColor);
                r.Width = 2.0;
                r.RadiusX = 0;
                r.RadiusY = 0;
                Canvas.SetLeft(r, 5);
                Canvas.SetTop(r, 3);
                c.Children.Add(r);
            }
            if (column == 2 && tb != null && type == 3)
            {
                tb.Text = "Property";
            }

            // Asset
            if (column == 1 && c != null && type == 4)
            {
                var r = new System.Windows.Shapes.Rectangle();
                r.Width = 20;
                r.Height = 20;
                r.MinWidth = 20;
                r.MinHeight = 20;
                r.Fill = ColorToBrush(AssetFillColor);
                r.Stroke = ColorToBrush(AssetBorderColor);
                r.Width = 2.0;
                r.RadiusX = 0;
                r.RadiusY = 0;
                Canvas.SetLeft(r, 5);
                Canvas.SetTop(r, 3);
                c.Children.Add(r);
            }
            if (column == 2 && tb != null && type == 4)
            {
                tb.Text = "Asset";
            }

            // Prop Rel
            if (column == 1 && c != null && type == 5)
            {
                var line = new System.Windows.Shapes.Line();
                line.X1 = 2;
                line.Y1 = 15;
                line.X2 = 28;
                line.Y2 = 15;
                line.Stroke = ColorToBrush(PropertyBorderColor);
                line.StrokeThickness = 2.0;
                c.Children.Add(line);
            }
            if (column == 2 && tb != null && type == 5)
            {
                tb.Text = "Property \u2b64  Entity";
            }

            // normal Rel
            if (column == 1 && c != null && type == 6)
            {
                var line = new System.Windows.Shapes.Line();
                line.X1 = 2;
                line.Y1 = 15;
                line.X2 = 28;
                line.Y2 = 15;
                line.Stroke = ColorToBrush(DefaultBorderColor);
                line.StrokeThickness = 2.0;
                c.Children.Add(line);
            }
            if (column == 2 && tb != null && type == 6)
            {
                tb.Text = "Relation";
            }

            // ok
            if (c != null)
                c.Margin = new System.Windows.Thickness(2);
            return c;
        }

        public static Border GenerateWrapLegend()
        {
            // create panel
            var wrap = new WrapPanel();
            wrap.Orientation = Orientation.Horizontal;
            wrap.Margin = new System.Windows.Thickness(2);

            // Populate 
            for (int type = 0; type < 6; type++)
            {
                // inside make a stack panel
                var sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;
                for (int c = 0; c < 2; c++)
                {
                    var part = GenerateLegendPart(1 + type, 1 + c);
                    sp.Children.Add(part);
                }
                // stack panel to wrap panel
                wrap.Children.Add(sp);
            }

            // OK
            var border = new Border();
            border.Margin = new System.Windows.Thickness(2);
            border.BorderBrush = System.Windows.Media.Brushes.DarkGray;
            border.BorderThickness = new System.Windows.Thickness(1);
            border.Child = wrap;
            return border;
        }

        private string WrapOnMaxColumn(string text, int maxColumn)
        {
            // we need a reasonable hysteresis
            int hyst = Math.Max(2, maxColumn / 2);

            // chunk it apart
            var sb = new StringBuilder();
            int oldPos = 0;
            int pos = maxColumn;
            while (pos < text.Length)
            {
                // have a possible split point, but try to improve it ..
                int newPos = -1;
                int newDist = int.MaxValue;
                for (int ppos = pos - hyst; ppos < pos + hyst; ppos++)
                {
                    if (ppos < text.Length && !Char.IsLetterOrDigit(text[ppos]))
                    {
                        var dist = Math.Abs(ppos - pos);
                        if (dist < newDist)
                        {
                            newPos = ppos;
                            newDist = dist;
                        }
                    }
                }

                // found better pos
                if (newPos > 0)
                    pos = newPos;

                // now take the chunk
                if (oldPos != 0)
                    sb.Append('\n');
                sb.Append(text.Substring(oldPos, 1 + pos - oldPos));

                // remember this
                oldPos = pos + 1;
                pos += maxColumn;
            }

            // still have a chunk
            sb.Append(text.Substring(oldPos));

            // OK
            return sb.ToString();
        }

        public void RecurseOnLayout(
            int pass,
            Microsoft.Msagl.Drawing.Graph graph,
            AdminShell.Referable parentRef,
            List<AdminShell.SubmodelElementWrapper> wrappers,
            int depth = 1, TextWriter textWriter = null)
        {
            // access
            if (graph == null || wrappers == null)
                return;

            // loop
            foreach (var smw in wrappers)
            {
                if (smw.submodelElement == null)
                    continue;

                if (textWriter != null)
                    textWriter.WriteLine("{0} Recurse pass {1} SME {2}", new String(' ', depth), pass, "" + smw.submodelElement?.idShort);

                if (smw.submodelElement is AdminShell.RelationshipElement)
                {
                    // access
                    var rel = (smw.submodelElement as AdminShell.RelationshipElement);

                    // for adding Nodes to the graph, we need in advance the knowledge, if a property
                    // is connected by a BOM relationship ..
                    if (pass == 1)
                    {
                        var x1 = this.FindReferableByReference(rel.first);
                        var x2 = this.FindReferableByReference(rel.second);
                        if (x1 != null)
                            referableByRelation[x1] = rel;
                        if (x2 != null)
                            referableByRelation[x2] = rel;
                    }

                    // now, try to finally draw relationships
                    if (pass == 3)
                    {
                        // ReSharper disable EmptyGeneralCatchClause
                        try
                        {
                            // build label text
                            var labelText = rel.ToIdShortString();
                            if (rel.semanticId != null && rel.semanticId.Count == 1)
                                labelText += " : " + rel.semanticId[0].value;
                            if (rel.semanticId != null && rel.semanticId.Count > 1)
                                labelText += " : " + rel.semanticId.ToString();

                            // even CD?
                            if (rel.semanticId != null && rel.semanticId.Count > 0)
                            {
                                var cd = this.FindReferableByReference(new AdminShell.Reference(rel.semanticId)) as AdminShell.ConceptDescription;
                                if (cd != null)
                                    labelText += " = " + cd.ToIdShortString();
                            }

                            // format it
                            labelText = WrapOnMaxColumn(labelText, WrapMaxColumn);

                            // now add
                            var x1 = this.FindReferableByReference(rel.first);
                            var x2 = this.FindReferableByReference(rel.second);

                            if (x1 == null || x2 == null)
                                continue;

                            var n1 = referableToNode[x1];
                            var n2 = referableToNode[x2];
                            var e = graph.AddEdge(n1.Id, labelText, n2.Id);
                            e.UserData = rel;
                            e.Attr.ArrowheadAtSource = Microsoft.Msagl.Drawing.ArrowStyle.Normal;
                            e.Attr.ArrowheadAtTarget = Microsoft.Msagl.Drawing.ArrowStyle.Normal;
                            e.Attr.LineWidth = 1;
                        }
                        catch { }
                        // ReSharper enable EmptyGeneralCatchClause
                    }
                }

                if (smw.submodelElement is AdminShell.Property)
                {
                    // access
                    var prop = (smw.submodelElement as AdminShell.Property);

                    // add as a Node to the graph?
                    if (pass == 2 && referableByRelation.ContainsKey(prop))
                    {
                        // this gives nodes!
                        var node = new Microsoft.Msagl.Drawing.Node(GenerateNodeID());
                        node.UserData = prop;
                        node.LabelText = "" + prop.ToIdShortString();
                        node.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Box;
                        node.Attr.FillColor = PropertyFillColor;
                        node.Attr.Color = PropertyBorderColor;
                        node.Attr.XRadius = 0;
                        node.Attr.YRadius = 0;
                        node.Label.FontSize = 8;

                        // add
                        graph.AddNode(node);
                        referableToNode[prop] = node;
                    }

                    // draw a link from the parent (Entity or SMC) to this node
                    if (pass == 3 && parentRef != null)
                    {
                        // get nodes
                        // ReSharper disable EmptyGeneralCatchClause
                        try
                        {
                            if (!referableToNode.ContainsKey(parentRef) || !referableToNode.ContainsKey(prop))
                                continue;

                            var parentNode = referableToNode[parentRef];
                            var propNode = referableToNode[prop];

                            var e = graph.AddEdge(parentNode.Id, propNode.Id);
                            e.Attr.ArrowheadAtSource = Microsoft.Msagl.Drawing.ArrowStyle.None;
                            e.Attr.ArrowheadAtTarget = Microsoft.Msagl.Drawing.ArrowStyle.None;
                            e.Attr.Color = PropertyBorderColor;
                            e.Attr.LineWidth = 2;
                            e.Attr.Weight = 10;
                        }
                        catch { }
                        // ReSharper enable EmptyGeneralCatchClause
                    }
                }

                if (smw.submodelElement is AdminShell.Entity)
                {
                    // access
                    var sme = (smw.submodelElement as AdminShell.Entity);

                    // add Nodes?
                    if (pass == 2)
                    {
                        // this gives nodes!
                        var node1 = new Microsoft.Msagl.Drawing.Node(GenerateNodeID());
                        node1.UserData = sme;
                        node1.LabelText = "" + sme.ToIdShortString();
                        node1.Label.FontSize = 12;

                        // what type?
                        if (sme.GetEntityType() == AdminShellV20.Entity.EntityTypeEnum.SelfManagedEntity)
                        {
                            node1.Attr.FillColor = AssetSelfManagedColor;
                        }
                        if (sme.GetEntityType() == AdminShellV20.Entity.EntityTypeEnum.CoManagedEntity)
                        {
                            node1.Attr.FillColor = AssetCoManagedColor;
                        }

                        // add
                        graph.AddNode(node1);
                        referableToNode[sme] = node1;

                        // add asset label
                        if (sme.assetRef != null && sme.assetRef.Count > 0)
                        {
                            // another node
                            var node2 = new Microsoft.Msagl.Drawing.Node(GenerateNodeID());
                            node2.UserData = sme.assetRef;
                            node2.LabelText = WrapOnMaxColumn("" + sme.assetRef.ToString(), WrapMaxColumn);
                            node2.Label.FontSize = 6;
                            node2.Attr.Color = AssetBorderColor;
                            node2.Attr.FillColor = AssetFillColor;
                            node2.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Box;
                            node2.Attr.XRadius = 0;
                            node2.Attr.YRadius = 0;
                            node2.Attr.LineWidth = 0.5;

                            // add
                            graph.AddNode(node2);

                            // connected
                            var e = graph.AddEdge(node1.Id, node2.Id);
                            e.Attr.ArrowheadAtSource = Microsoft.Msagl.Drawing.ArrowStyle.None;
                            e.Attr.ArrowheadAtTarget = Microsoft.Msagl.Drawing.ArrowStyle.None;
                            e.Attr.Color = AssetBorderColor;
                            e.Attr.LineWidth = 0.5;
                        }
                    }

                    // might have statements
                    // recurse
                    RecurseOnLayout(pass, graph, sme, sme.statements, depth + 1, textWriter);
                }

                if (smw.submodelElement is AdminShell.SubmodelElementCollection)
                {
                    // recurse
                    RecurseOnLayout(pass, graph, smw.submodelElement, (smw.submodelElement as AdminShell.SubmodelElementCollection).value, depth + 1, textWriter);
                }
            }
        }
    }

    /// <summary>
    /// This set of static functions lay-out a graph according to the package information.
    /// Right now, no domain-specific lay-out.
    /// </summary>
    public class GenericBomControl
    {
        private Microsoft.Msagl.Drawing.Graph theGraph = null;
        private Microsoft.Msagl.WpfGraphControl.GraphViewer theViewer = null;
        private AdminShell.Referable theReferable = null;

        private PluginEventStack eventStack = null;

        private Dictionary<AdminShell.Referable, int> preferredPresetIndex = new Dictionary<AdminShellV20.Referable, int>();

        public void SetEventStack(PluginEventStack es)
        {
            this.eventStack = es;
        }

        public object FillWithWpfControls(object opackage, object osm, object masterDockPanel)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as AdminShell.Submodel;
            var master = masterDockPanel as DockPanel;
            if (package == null || sm == null || master == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

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
            cbli.SelectedIndex = 0;
            cbli.SelectionChanged += CbLayoutIndex_SelectionChanged;
            spTop.Children.Add(cbli);

            // create BOTTOM controls
            var legend = GenericBomCreator.GenerateWrapLegend();
            DockPanel.SetDock(legend, Dock.Bottom);
            master.Children.Add(legend);

            // set default for very small edge label size
            Microsoft.Msagl.Drawing.Label.DefaultFontSize = 6;

            //create a graph object 
            Microsoft.Msagl.Drawing.Graph graph = new Microsoft.Msagl.Drawing.Graph("BOM-graph");

#if FALSE
            //create the graph content 
            graph.AddEdge("A", "B");
                var e1 = graph.AddEdge("B", "C");
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

            //graph.AddEdge("A", "B");
            //graph.AddNode("CC");

            var creator = new GenericBomCreator(package.AasEnv);

            using (var tw = new StreamWriter("bomgraph.log"))
            {
                creator.RecurseOnLayout(1, graph, null, sm.submodelElements, 1, tw);
                creator.RecurseOnLayout(2, graph, null, sm.submodelElements, 1, tw);
                creator.RecurseOnLayout(3, graph, null, sm.submodelElements, 1, tw);
            }

            //var settings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings();
            //Microsoft.Msagl.Miscellaneous.LayoutHelpers.CalculateLayout(graph.Geometr yGraph, settings, null);

            // graph.LayoutAlgorithmSettings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings();
            //var setting = new Microsoft.Msagl.Layout.Incremental.FastIncrementalLayoutSettings();
            //setting.RepulsiveForceConstant = 50.0;
            //graph.LayoutAlgorithmSettings = setting;

            // make default or (already) preferred settings
            var settings = GivePresetSettings(cbli.SelectedIndex);
            if (this.preferredPresetIndex != null && this.preferredPresetIndex.ContainsKey(sm))
                settings = GivePresetSettings(this.preferredPresetIndex[sm]);
            if (settings != null)
                graph.LayoutAlgorithmSettings = settings;

#endif

            // make a Dock panel    
            var dp = new DockPanel();
            dp.ClipToBounds = true;
            dp.MinWidth = 10;
            dp.MinHeight = 10;

            // very important: add first the panel, then add graph
            master.Children.Add(dp);

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
            theReferable = sm;

            //var labelPlacer = new Microsoft.Msagl.Core.Layout.EdgeLabelPlacement(graph.GeometryGraph);
            //labelPlacer.Run();

            // return viewer for advanced manilulation
            return viewer;

        }

        private void Viewer_ObjectUnderMouseCursorChanged(object sender, Microsoft.Msagl.Drawing.ObjectUnderMouseCursorChangedEventArgs e)
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
                // ReSharper disable EmptyGeneralCatchClause
                try
                {
                    var x = theViewer.ObjectUnderMouseCursor;
                    if (x != null && x.DrawingObject != null && x.DrawingObject.UserData != null)
                    {
                        var us = x.DrawingObject.UserData;
                        if (us is AdminShell.Referable)
                        {
                            // make event
                            var refs = new List<AdminShell.Key>();
                            (us as AdminShell.Referable).CollectReferencesByParent(refs);

                            // ok?
                            if (refs.Count > 0)
                            {
                                var evt = new AasxPluginResultEventNavigateToReference();
                                evt.targetReference = AdminShell.Reference.CreateNew(refs);
                                this.eventStack.PushEvent(evt);
                            }
                        }

                        if (us is AdminShell.Reference)
                        {
                            var evt = new AasxPluginResultEventNavigateToReference();
                            evt.targetReference = (us as AdminShell.Reference);
                            this.eventStack.PushEvent(evt);
                        }
                    }
                }
                catch { }
                // ReSharper enable EmptyGeneralCatchClause
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

        private void CbLayoutIndex_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null || theGraph == null || theViewer == null)
                return;

            // ReSharper disable EmptyGeneralCatchClause
            try
            {
                // try to remember preferred setting
                if (this.theReferable != null && preferredPresetIndex != null && cb.SelectedIndex >= 0)
                    this.preferredPresetIndex[this.theReferable] = cb.SelectedIndex;

                // generate settings
                var settings = GivePresetSettings(cb.SelectedIndex);
                if (settings == null)
                    return;

                // re-draw (brutally)
                theGraph.LayoutAlgorithmSettings = settings;
                theViewer.Graph = null;
                theViewer.Graph = theGraph;
            }
            catch { }
            // ReSharper enable EmptyGeneralCatchClause
        }
    }
}
