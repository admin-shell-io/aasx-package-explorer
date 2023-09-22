/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using Microsoft.Msagl.Drawing;
using System.Runtime.Intrinsics.X86;

namespace AasxPluginBomStructure
{
    public class GenericBomCreatorOptions
    {
        public bool CompactLabels = true;
        public bool ShowAssetIds = true;
        public int LayoutIndex = 0;
        public double LayoutSpacing = 10;
    }

    public class GenericBomEntityStyle
    {
        public Microsoft.Msagl.Drawing.Color
            FillColor, BorderColor, FontColor;
    }

    public class GenericBomCreator
    {
        public static int WrapMaxColumn = 20;

        public static Microsoft.Msagl.Drawing.Color DefaultBorderColor = new Microsoft.Msagl.Drawing.Color(0, 0, 0);

        public static GenericBomEntityStyle EntityStyleAAS = new GenericBomEntityStyle()
        {
            FillColor = new Microsoft.Msagl.Drawing.Color(1, 40, 203),
            BorderColor = new Microsoft.Msagl.Drawing.Color(192, 204, 255),
            FontColor = new Microsoft.Msagl.Drawing.Color(0, 0, 0)
        };

        public static GenericBomEntityStyle EntityStyleSubmodel = new GenericBomEntityStyle()
        {
            FillColor = new Microsoft.Msagl.Drawing.Color(1, 40, 203),
            BorderColor = new Microsoft.Msagl.Drawing.Color(192, 204, 255),
            FontColor = new Microsoft.Msagl.Drawing.Color(255, 255, 255)
        };

        public static GenericBomEntityStyle EntityStyleDataElement = new GenericBomEntityStyle()
        {
            FillColor = new Microsoft.Msagl.Drawing.Color(219, 226, 255),
            BorderColor = new Microsoft.Msagl.Drawing.Color(1, 40, 203),
            FontColor = new Microsoft.Msagl.Drawing.Color(0, 0, 0)
        };

        public static Microsoft.Msagl.Drawing.Color AssetCoManagedColor =
            new Microsoft.Msagl.Drawing.Color(158, 179, 255);
        public static Microsoft.Msagl.Drawing.Color AssetSelfManagedColor =
            new Microsoft.Msagl.Drawing.Color(104, 137, 255);

        public static Microsoft.Msagl.Drawing.Color AssetFillColor =
            new Microsoft.Msagl.Drawing.Color(192, 192, 192);
        public static Microsoft.Msagl.Drawing.Color AssetBorderColor =
            new Microsoft.Msagl.Drawing.Color(128, 128, 128);

        private ConvenientDictionary<Aas.IReferable, Microsoft.Msagl.Drawing.Node> referableToNode =
            new ConvenientDictionary<Aas.IReferable, Microsoft.Msagl.Drawing.Node>();
        private ConvenientDictionary<Aas.IReferable, Aas.ISubmodelElement> referableByRelation =
            new ConvenientDictionary<Aas.IReferable, Aas.ISubmodelElement>();

        private Aas.Environment _env;
        private BomStructureOptionsRecordList _bomRecords;
        private GenericBomCreatorOptions _options;

        private int maxNodeId = 1;

        private AasReferableStore _refStore = null;

        public GenericBomCreator(
            Aas.Environment env,
            BomStructureOptionsRecordList bomRecords,
            GenericBomCreatorOptions options)
        {
            _env = env;
            _bomRecords = bomRecords;
            _options = options;
            _refStore = new AasReferableStore();
            _refStore.Index(env);
        }

        public void SetRecods(BomStructureOptionsRecordList bomRecords)
        {
            _bomRecords = bomRecords;
        }

        public Aas.IReferable FindReferableByReference(Aas.IReference r)
        {
            if (_refStore == null)
                return this._env?.FindReferableByReference(r);
            return _refStore.FindElementByReference(r, MatchMode.Relaxed);
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
                r.Fill = ColorToBrush(EntityStyleDataElement.FillColor);
                r.Stroke = ColorToBrush(EntityStyleDataElement.BorderColor);
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
                line.Stroke = ColorToBrush(EntityStyleDataElement.BorderColor);
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
            wrap.Orientation = System.Windows.Controls.Orientation.Horizontal;
            wrap.Margin = new System.Windows.Thickness(2);

            // Populate
            for (int type = 0; type < 6; type++)
            {
                // inside make a stack panel
                var sp = new StackPanel();
                sp.Orientation = System.Windows.Controls.Orientation.Horizontal;
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

        public void ApplyNodeStyle(
            Microsoft.Msagl.Drawing.Node node,
            BomArguments ns)
        {
            // any 
            if (node == null || ns == null)
                return;

            // try find a shape
            if (ns.Shape.HasValue)
                node.Attr.Shape = BomArguments.MsaglShapeFrom(ns.Shape.Value);

            if (ns.Background?.HasContent() == true)
                node.Attr.FillColor = BomArguments.MsaglColorFrom(ns.Background);

            if (ns.Stroke?.HasContent() == true)
                node.Attr.Color = BomArguments.MsaglColorFrom(ns.Stroke);

            if (ns.Radius.HasValue)
            {
                node.Attr.XRadius = ns.Radius.Value;
                node.Attr.YRadius = ns.Radius.Value;
            }

            if (ns.FontSize.HasValue)
                node.Label.FontSize = ns.FontSize.Value;

            if (ns.Dashed.HasValue && ns.Dashed.Value == true)
                node.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dashed);
            if (ns.Dotted.HasValue && ns.Dotted.Value == true)
                node.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dotted);
            if (ns.FontBold.HasValue && ns.FontBold.Value == true)
                node.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Bold);

            if (ns.Width.HasValue)
                node.Attr.LineWidth = ns.Width.Value;

            if (ns.Title.HasContent())
                node.LabelText = ns.Title;
        }

        public void ApplyLinkStyle(
            Microsoft.Msagl.Drawing.Edge e,
            BomArguments ls)
        {
            // any 
            if (e == null || ls == null)
                return;

            if (ls.Start.HasValue)
                e.Attr.ArrowheadAtSource = BomArguments.MsaglArrowStyleFrom(ls.Start.Value);
            if (ls.End.HasValue)
                e.Attr.ArrowheadAtTarget = BomArguments.MsaglArrowStyleFrom(ls.End.Value);

            if (ls.Stroke?.HasContent() == true)
                e.Attr.Color = BomArguments.MsaglColorFrom(ls.Stroke);

            if (ls.Dashed.HasValue && ls.Dashed.Value == true)
                e.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dashed);
            if (ls.Dotted.HasValue && ls.Dotted.Value == true)
                e.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dotted);
            if (ls.FontBold.HasValue && ls.FontBold.Value == true)
                e.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Bold);

            if (ls.Width.HasValue)
                e.Attr.LineWidth = ls.Width.Value;

            if (ls.Title.HasContent())
                e.LabelText = ls.Title;
        }

        public void CreateAasAndSubmodelNodes(
            Microsoft.Msagl.Drawing.Graph graph,
            Aas.ISubmodel sm)
        {
            // access
            if (sm == null)
                return;

            // Submodel node?
            var smNode = referableToNode.GetValueOrDefault(sm);
            if (smNode == null)
            {
                smNode = CreateNode(graph,
                    sm,
                    sm.ToIdShortString(),
                    Microsoft.Msagl.Drawing.Shape.Hexagon,
                    EntityStyleSubmodel.FillColor, EntityStyleSubmodel.BorderColor,
                    EntityStyleSubmodel.FontColor,
                    xRadius: 0, yRadius: 0,
                    fontSize: 12,
                    ns1: _bomRecords?.FindFirstNodeStyle(sm.SemanticId),
                    ns2: BomArguments.Parse(sm.HasExtensionOfName("BOM.Args")?.Value));

                // link will be formed later be the recursion, starting with the individual
                // Submodel
            }

            // AAS?
            var aas = _env?.FindAasWithSubmodelId(sm?.Id);
            var aasNode = referableToNode.GetValueOrDefault(aas);
            if (aas != null && aasNode == null)
            {
                aasNode = CreateNode(graph,
                    aas,
                    aas.ToIdShortString(),
                    Microsoft.Msagl.Drawing.Shape.DoubleCircle,
                    EntityStyleAAS.FillColor, EntityStyleAAS.BorderColor,
                    EntityStyleAAS.FontColor,
                    xRadius: 0, yRadius: 0,
                    fontSize: 12,
                    ns1: BomArguments.Parse(aas.HasExtensionOfName("BOM.Args")?.Value));
            }

            // make a link?
            if (smNode != null && aasNode != null)
            {
                CreateLink(
                    graph, smNode, aasNode,
                    "(listed in)", 1.0,
                    ArrowStyle.None, ArrowStyle.Normal);
            }
        }

        protected Node CreateNode(
            Microsoft.Msagl.Drawing.Graph graph,
            Aas.IReferable rf,
            string nodeText,
            Microsoft.Msagl.Drawing.Shape shape,
            Microsoft.Msagl.Drawing.Color fillColor,
            Microsoft.Msagl.Drawing.Color borderColor,
            Microsoft.Msagl.Drawing.Color fontColor,
            double xRadius, double yRadius,
            double fontSize,
            BomArguments ns1 = null,
            BomArguments ns2 = null)
        {
            // skip?
            if (ns1?.Skip == true || ns2?.Skip == true)
                return null;

            // this gives nodes!
            var node = new Microsoft.Msagl.Drawing.Node(GenerateNodeID());
            node.UserData = rf;
            node.LabelText = "" + nodeText;
            node.Attr.Shape = shape;
            node.Attr.FillColor = fillColor;
            node.Attr.Color = borderColor;
            node.Attr.XRadius = xRadius;
            node.Attr.YRadius = yRadius;
            node.Label.FontSize = fontSize;
            node.Label.FontColor = fontColor;

            // more style based?
            ApplyNodeStyle(node, ns1);
            ApplyNodeStyle(node, ns2);

            // add
            graph.AddNode(node);
            referableToNode[rf] = node;

            // okey
            return node;
        }

        protected Edge CreateLink(
            Microsoft.Msagl.Drawing.Graph graph,
            Node n1,
            Node n2,
            string linkText,
            double lineWidth,
            ArrowStyle arrowheadAtSource,
            ArrowStyle arrowheadAtTarget,
            bool isDashed = false, bool isDotted = false,
            object userData = null,
            BomArguments ls1 = null,
            BomArguments ls2 = null,
            int? weight = null)
        {
            // format text it
            var labelText = WrapOnMaxColumn(linkText, WrapMaxColumn);

            // skip?
            if (ls1?.Skip == true || ls2?.Skip == true)
                return null;

            // enough nodes?
            if (n1 == null || n2 == null)
                return null;

            var edge = graph.AddEdge(n1.Id, labelText, n2.Id);
            edge.UserData = userData;
            edge.Attr.ArrowheadAtSource = arrowheadAtSource;
            edge.Attr.ArrowheadAtTarget = arrowheadAtTarget;
            edge.Attr.LineWidth = lineWidth;

            if (isDashed)
                edge.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dashed);
            if (isDotted)
                edge.Attr.AddStyle(Microsoft.Msagl.Drawing.Style.Dotted);

            if (weight.HasValue)
                edge.Attr.Weight = weight.Value;

            // more style based on semantic id?
            ApplyLinkStyle(edge, ls1);
            ApplyLinkStyle(edge, ls2);

            // okay
            return edge;
        }

        public void RecurseOnLayout(
            int pass,
            Microsoft.Msagl.Drawing.Graph graph,
            Aas.IReferable parentRef,
            List<Aas.ISubmodelElement> smec,
            int depth = 1, TextWriter textWriter = null,
            Aas.IReferable entityParentRef = null)
        {
            // access
            if (graph == null || smec == null)
                return;

            // loop
            foreach (var sme in smec)
            {
                if (sme == null)
                    continue;

                if (textWriter != null)
                    textWriter.WriteLine(
                        "{0} Recurse pass {1} SME {2}",
                        new String(' ', depth), pass, "" + sme.IdShort);

                if (sme is Aas.IReferenceElement
                    || sme is Aas.IRelationshipElement)
                {
                    Aas.IReferable x1 = null, x2 = null;

                    if (sme is Aas.IRelationshipElement rel)
                    {
                        // includes AnnotatedRelationshipElement
                        x1 = this.FindReferableByReference(rel.First);
                        x2 = this.FindReferableByReference(rel.Second);
                    }

                    if (sme is Aas.IReferenceElement rfe)
                    {
                        x1 = sme.Parent as Aas.IReferable;
                        x2 = this.FindReferableByReference(rfe.Value);
                    }

                    // for adding Nodes to the graph, we need in advance the knowledge, if a property
                    // is connected by a BOM relationship ..
                    if (pass == 1)
                    {
                        if (x1 != null)
                            referableByRelation[x1] = sme;
                        if (x2 != null)
                            referableByRelation[x2] = sme;
                    }

                    // now, try to finally draw relationships
                    if (pass == 3)
                    {
                        try
                        {
                            // build label text
                            var labelText = sme.ToIdShortString();
                            if (_options?.CompactLabels != true)
                            {
                                if (sme.SemanticId != null && sme.SemanticId.Count() == 1)
                                    labelText += " : " + sme.SemanticId.Keys[0].Value;
                                if (sme.SemanticId != null && sme.SemanticId.Count() > 1)
                                    labelText += " : " + sme.SemanticId.ToString();
                            }

                            // find BOM display arguments?
                            var args = BomArguments.Parse(sme.HasExtensionOfName("BOM.Args")?.Value);

                            // even CD?
                            if (sme.SemanticId != null && sme.SemanticId.Count() > 0)
                            {
                                var cd = this.FindReferableByReference(sme.SemanticId.Copy()) as Aas.ConceptDescription;

                                if (cd != null)
                                {
                                    labelText += " = " + cd.ToIdShortString();

                                    // option
                                    if (_options?.CompactLabels == true
                                        && cd.IdShort.HasContent())
                                        labelText = cd.IdShort;

                                    // (less important) args?
                                    args = args ?? BomArguments.Parse(cd.HasExtensionOfName("BOM.Args")?.Value);
                                }
                            }

                            CreateLink(graph,
                                referableToNode.GetValueOrDefault(x1),
                                referableToNode.GetValueOrDefault(x2),
                                labelText,
                                1.0, ArrowStyle.Normal, ArrowStyle.Normal,
                                userData: sme,
                                ls1: _bomRecords?.FindFirstLinkStyle(sme.SemanticId, sme.SupplementalSemanticIds),
                                ls2: args);
                        }
                        catch (Exception ex)
                        {
                            AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                        }
                    }
                }

                if (sme is Aas.IDataElement)
                {
                    // add as a Node to the graph?
                    if (pass == 2 && referableByRelation.ContainsKey(sme))
                    {
                        var node = CreateNode(graph, sme,
                            "" + sme.ToIdShortString(),
                            Microsoft.Msagl.Drawing.Shape.Box,
                            EntityStyleDataElement.FillColor,
                            EntityStyleDataElement.BorderColor,
                            EntityStyleDataElement.FontColor,
                            0, 0, fontSize: 8,
                            ns1: _bomRecords?.FindFirstNodeStyle(sme.SemanticId, sme.SupplementalSemanticIds),
                            ns2: BomArguments.Parse(sme.HasExtensionOfName("BOM.Args")?.Value));
                    }

                    // draw a link from the parent (Entity or SMC) to this node
                    if (pass == 3 && parentRef != null)
                    {
                        CreateLink(graph,
                            referableToNode.GetValueOrDefault(parentRef),
                            referableToNode.GetValueOrDefault(sme),
                            "has",
                            lineWidth: 1,
                            arrowheadAtSource: ArrowStyle.None,
                            arrowheadAtTarget: ArrowStyle.Normal,
                            weight: 10000);
                    }
                }

                if (sme is Aas.Entity ent)
                {
                    // add Nodes?
                    if (pass == 2)
                    {
                        // can get an link style?
                        var ns = _bomRecords?.FindFirstNodeStyle(ent.SemanticId, ent.SupplementalSemanticIds);
                        if (ns?.Skip == true)
                            continue;

                        // this gives nodes!
                        var node1 = new Microsoft.Msagl.Drawing.Node(GenerateNodeID());
                        node1.UserData = ent;
                        node1.LabelText = "" + ent.ToIdShortString();
                        node1.Label.FontSize = 12;

                        // what type?
                        if (ent.EntityType == Aas.EntityType.SelfManagedEntity)
                        {
                            node1.Attr.FillColor = AssetSelfManagedColor;
                        }
                        if (ent.EntityType == Aas.EntityType.CoManagedEntity)
                        {
                            node1.Attr.FillColor = AssetCoManagedColor;
                        }

                        // apply style
                        if (ns != null)
                            ApplyNodeStyle(node1, ns);

                        // add
                        graph.AddNode(node1);
                        referableToNode[sme] = node1;

                        // add asset label
                        if (_options?.ShowAssetIds == true
                            && ent.GlobalAssetId != null && ent.GlobalAssetId.Count() > 0)
                        {
                            // another node
                            var node2 = new Microsoft.Msagl.Drawing.Node(GenerateNodeID());
                            node2.UserData = ent.GlobalAssetId;
                            node2.LabelText = WrapOnMaxColumn("" + ent.GlobalAssetId, WrapMaxColumn);
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

                    // draw a link from the parent (Entity or SMC) to this node
                    if (pass == 3 && entityParentRef != null)
                    {
                        CreateLink(graph,
                            referableToNode.GetValueOrDefault(entityParentRef),
                            referableToNode.GetValueOrDefault(ent),
                            "(in)",
                            lineWidth: 0.75,
                            arrowheadAtSource: ArrowStyle.Normal,
                            arrowheadAtTarget: ArrowStyle.None,
                            isDotted: true,
                            weight: 10000);
                    }

                    // might have statements
                    // recurse
                    RecurseOnLayout(pass, graph, sme, ent.Statements, depth + 1, textWriter);
                }

                if (sme is Aas.SubmodelElementCollection innerSmc)
                {
                    // recurse
                    RecurseOnLayout(
                        pass, graph, sme,
                        innerSmc.Value,
                        depth + 1, textWriter);
                }
            }
        }
    }

}
