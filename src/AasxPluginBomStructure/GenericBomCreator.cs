/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using AasxIntegrationBase;
using AdminShellNS;

namespace AasxPluginBomStructure
{
    public class GenericBomCreatorOptions
    {
        public bool CompactLabels = false;
        public int LayoutIndex = 0;
    }

    public class GenericBomCreator
    {
        public static int WrapMaxColumn = 20;

        public static Microsoft.Msagl.Drawing.Color DefaultBorderColor = new Microsoft.Msagl.Drawing.Color(0, 0, 0);

        public static Microsoft.Msagl.Drawing.Color PropertyFillColor =
            new Microsoft.Msagl.Drawing.Color(89, 139, 209);
        public static Microsoft.Msagl.Drawing.Color PropertyBorderColor =
            new Microsoft.Msagl.Drawing.Color(12, 94, 209);

        public static Microsoft.Msagl.Drawing.Color AssetCoManagedColor =
            new Microsoft.Msagl.Drawing.Color(184, 194, 209);
        public static Microsoft.Msagl.Drawing.Color AssetSelfManagedColor =
            new Microsoft.Msagl.Drawing.Color(136, 166, 210);

        public static Microsoft.Msagl.Drawing.Color AssetFillColor =
            new Microsoft.Msagl.Drawing.Color(192, 192, 192);
        public static Microsoft.Msagl.Drawing.Color AssetBorderColor =
            new Microsoft.Msagl.Drawing.Color(128, 128, 128);

        private Dictionary<AdminShell.Referable, Microsoft.Msagl.Drawing.Node> referableToNode =
            new Dictionary<AdminShell.Referable, Microsoft.Msagl.Drawing.Node>();
        private Dictionary<AdminShell.Referable, AdminShell.RelationshipElement> referableByRelation =
            new Dictionary<AdminShell.Referable, AdminShell.RelationshipElement>();

        private AdminShell.AdministrationShellEnv _env;
        private BomStructureOptionsRecordList _bomRecords;
        private GenericBomCreatorOptions _options;

        private int maxNodeId = 1;

        private AasReferableStore _refStore = null;

        public GenericBomCreator(
            AdminShell.AdministrationShellEnv env,
            BomStructureOptionsRecordList bomRecords,
            GenericBomCreatorOptions options)
        {
            _env = env;
            _bomRecords = bomRecords;
            _options = options;
            _refStore = new AasReferableStore();
            _refStore.Index(env);
        }

        public AdminShell.Referable FindReferableByReference(AdminShell.Reference r)
        {
            if (_refStore == null)
                return this._env?.FindReferableByReference(r);
            return _refStore.FindElementByReference(r, AdminShell.Key.MatchMode.Relaxed);
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

        public Microsoft.Msagl.Drawing.Color GetMsaglColor(string color)
        {
            // ReSharper disable PossibleNullReferenceException
            if (color != null)
                try
                {
                    var col = (System.Windows.Media.Color)ColorConverter.ConvertFromString(color);
                    return new Microsoft.Msagl.Drawing.Color(col.R, col.G, col.B);
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            // ReSharper enable PossibleNullReferenceException

            return new Microsoft.Msagl.Drawing.Color(0, 0, 0);
        }

        public void ApplyNodeStyle(
            Microsoft.Msagl.Drawing.Node node,
            BomNodeStyle ns)
        {
            // any 
            if (node == null || ns == null)
                return;

            // try find a shape
            foreach (var x in (Microsoft.Msagl.Drawing.Shape[])Enum.GetValues(typeof(Microsoft.Msagl.Drawing.Shape)))
                if (Enum.GetName(typeof(Microsoft.Msagl.Drawing.Shape), x).Trim().ToLower()
                    == ("" + ns.Shape).Trim().ToLower())
                    node.Attr.Shape = x;

            if (ns.Background.HasContent())
                node.Attr.FillColor = GetMsaglColor(ns.Background);

            if (ns.Foreground.HasContent())
                node.Attr.Color = GetMsaglColor(ns.Foreground);

            if (ns.Radius > 0.0)
            {
                node.Attr.XRadius = ns.Radius;
                node.Attr.YRadius = ns.Radius;
            }

            if (ns.FontSize > 0.0)
                node.Label.FontSize = ns.FontSize;

            if (ns.Dashed)
                node.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dashed);
            if (ns.Dotted)
                node.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dotted);
            if (ns.Bold)
                node.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Bold);

            if (ns.LineWidth > 0.0)
                node.Attr.LineWidth = ns.LineWidth;

            if (ns.Text.HasContent())
                node.LabelText = ns.Text;
        }

        public void ApplyLinkStyle(
            Microsoft.Msagl.Drawing.Edge e,
            BomLinkStyle ls)
        {
            // any 
            if (e == null || ls == null)
                return;

            if (ls.Direction == BomLinkDirection.None)
            {
                e.Attr.ArrowheadAtSource = Microsoft.Msagl.Drawing.ArrowStyle.None;
                e.Attr.ArrowheadAtTarget = Microsoft.Msagl.Drawing.ArrowStyle.None;
            }
            if (ls.Direction == BomLinkDirection.Forward)
                e.Attr.ArrowheadAtSource = Microsoft.Msagl.Drawing.ArrowStyle.None;
            if (ls.Direction == BomLinkDirection.Backward)
                e.Attr.ArrowheadAtTarget = Microsoft.Msagl.Drawing.ArrowStyle.None;

            if (ls.Color.HasContent())
                e.Attr.Color = GetMsaglColor(ls.Color);

            if (ls.Dashed)
                e.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dashed);
            if (ls.Dotted)
                e.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dotted);
            if (ls.Bold)
                e.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Bold);

            e.Attr.LineWidth = ls.Width;

            if (ls.Text.HasContent())
                e.LabelText = ls.Text;
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
                    textWriter.WriteLine(
                        "{0} Recurse pass {1} SME {2}",
                        new String(' ', depth), pass, "" + smw.submodelElement?.idShort);

                if (smw.submodelElement is AdminShell.RelationshipElement rel)
                {
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
                                var cd = this.FindReferableByReference(
                                    new AdminShell.Reference(
                                        rel.semanticId)) as AdminShell.ConceptDescription;

                                if (cd != null)
                                {
                                    labelText += " = " + cd.ToIdShortString();

                                    // option
                                    if (_options?.CompactLabels == true
                                        && cd.idShort.HasContent())
                                        labelText = cd.idShort;
                                }
                            }

                            // format text it
                            labelText = WrapOnMaxColumn(labelText, WrapMaxColumn);

                            // can get an link style?
                            var ls = _bomRecords?.FindFirstLinkStyle(rel.semanticId);

                            // skip?
                            if (ls?.Skip == true)
                                continue;

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

                            // more style based on semantic id?
                            ApplyLinkStyle(e, ls);
                        }
                        catch (Exception ex)
                        {
                            AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                        }
                    }
                }

                if (smw.submodelElement is AdminShell.Property)
                {
                    // access
                    var prop = (smw.submodelElement as AdminShell.Property);

                    // add as a Node to the graph?
                    if (pass == 2 && referableByRelation.ContainsKey(prop))
                    {
                        // can get an link style?
                        var ns = _bomRecords?.FindFirstNodeStyle(prop.semanticId);

                        // skip?
                        if (ns?.Skip == true)
                            continue;

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

                        // more style based on semantic id?
                        ApplyNodeStyle(node, ns);

                        // add
                        graph.AddNode(node);
                        referableToNode[prop] = node;
                    }

                    // draw a link from the parent (Entity or SMC) to this node
                    if (pass == 3 && parentRef != null)
                    {
                        // get nodes
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
                            e.Attr.Weight = 10000;
                        }
                        catch (Exception ex)
                        {
                            AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                        }
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
                        if (sme.GetEntityType() == AdminShell.Entity.EntityTypeEnum.SelfManagedEntity)
                        {
                            node1.Attr.FillColor = AssetSelfManagedColor;
                        }
                        if (sme.GetEntityType() == AdminShell.Entity.EntityTypeEnum.CoManagedEntity)
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
                    RecurseOnLayout(
                        pass, graph, smw.submodelElement,
                        (smw.submodelElement as AdminShell.SubmodelElementCollection).value,
                        depth + 1, textWriter);
                }
            }
        }
    }

}
