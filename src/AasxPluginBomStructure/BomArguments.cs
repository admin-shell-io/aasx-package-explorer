/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AasxIntegrationBase;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;

namespace AasxPluginBomStructure
{
    /// <summary>
    /// This class holds argument which could be used by the user to refine the graphical
    /// display of the BOM. It will be attached to the Qualifier "BOM.Args" in a JSON syntax.
    /// </summary>
    public class BomArguments : IAasReferenceStoreItem
    {
        // ReSharper disable UnassignedField.Global

        /// <summary>
        /// For options: Match a certain semanticId
        /// </summary>
        public Aas.Key Match;

        /// <summary>
        /// Required by <c>IAasReferenceStoreItem</c>
        /// </summary>
        public Aas.IReference GetReference() => ExtendReference.CreateFromKey(Match);

        /// <summary>
        /// Should the drawing of this element skipped?
        /// </summary>
        public bool? Skip;

        /// <summary>
        /// Display title of the respective entity to be shown in the panel
        /// </summary>
        public string Title;

        /// <summary>
        /// Fontsize in points for drawing labels of nodes and relations
        /// </summary>
        public double? FontSize;

        /// <summary>
        /// Switch font to bold?
        /// </summary>
        public bool? FontBold;

        /// <summary>
        /// Line style with dashes?
        /// </summary>
        public bool? Dashed;

        /// <summary>
        /// Line style with dots?
        /// </summary>
        public bool? Dotted;

        /// <summary>
        /// Styles of arrow heads. Uses exactly the same names as in
        /// NuGet package "AutomaticGraphLayout.Drawing".
        /// </summary>
        public enum ArrowStyle
        {
            NonSpecified,
            None,
            Normal,
            Tee,
            Diamond,
            ODiamond,
            Generalization
        }

        /// <summary>
        /// Arrow style of the starting point (head) of a relation.
        /// </summary>
        public ArrowStyle? Start;

        /// <summary>
        /// Arrow style of the ending point (tail) of a relation.
        /// </summary>
        public ArrowStyle? End;

        /// <summary>
        /// Shape of a node.
        /// </summary>
        public enum NodeShape
        {
            Diamond,
            Ellipse,
            Box,
            Circle,
            Record,
            Plaintext,
            Point,
            Mdiamond,
            Msquare,
            Polygon,
            DoubleCircle,
            House,
            InvHouse,
            Parallelogram,
            Octagon,
            TripleOctagon,
            Triangle,
            Trapezium,
            DrawFromGeometry,
            Hexagon
        }

        /// <summary>
        /// Shape of a node.
        /// </summary>
        public NodeShape? Shape;

        /// <summary>
        /// Line width in pixels of a relation.
        /// </summary>
        public double? Width;

        /// <summary>
        /// Possible corner radius of a node
        /// </summary>
        public double? Radius;

        /// <summary>
        /// Stroke color of a relation in web RGBA notation (e.g. #a02030ff).
        /// </summary>
        public string Stroke;

        /// <summary>
        /// Background color of a relation in web RGBA notation (e.g. #a02030ff).
        /// </summary>
        public string Background;

        // ReSharper enable UnassignedField.Global

        public static BomArguments Parse(string json)
        {
            if (json?.HasContent() != true)
                return null;

            try
            {
                var res = Newtonsoft.Json.JsonConvert.DeserializeObject<BomArguments>(json);
                return res;
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            return null;
        }

        public static Microsoft.Msagl.Drawing.Color MsaglColorFrom(string st)
        {
            // default is visible
            if (st?.HasContent() != true)
                return Microsoft.Msagl.Drawing.Color.Black;

            // web style
            if (st.StartsWith("#") && st.Length >= 7)
                try
                {
                    var r = Convert.ToByte(st.Substring(1, 2), 16);
                    var g = Convert.ToByte(st.Substring(3, 2), 16);
                    var b = Convert.ToByte(st.Substring(5, 2), 16);
                    var a = (st.Length >= 9) ? Convert.ToByte(st.Substring(7, 2), 16) : (byte)0xff;
                    return new Microsoft.Msagl.Drawing.Color(a, r, g, b);
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                }

            // do the less efficient stuff

            var prop = typeof(Microsoft.Msagl.Drawing.Color)
                            .GetProperties(BindingFlags.Public | BindingFlags.Static)
                            .Where(f => f.Name.Contains(st, StringComparison.InvariantCultureIgnoreCase))
                            .FirstOrDefault();
            if (prop != null)
                return (Microsoft.Msagl.Drawing.Color)prop.GetValue(null);

            // default, again
            return Microsoft.Msagl.Drawing.Color.Black;
        }

        public static Microsoft.Msagl.Drawing.ArrowStyle MsaglArrowStyleFrom(ArrowStyle style)
        {
            var st = style.ToString();
            foreach (var x in AdminShellUtil.GetEnumValues<Microsoft.Msagl.Drawing.ArrowStyle>())
                if (x.ToString().Equals(st, StringComparison.InvariantCultureIgnoreCase))
                    return x;
            return Microsoft.Msagl.Drawing.ArrowStyle.NonSpecified;
        }

        public static Microsoft.Msagl.Drawing.Shape MsaglShapeFrom(NodeShape style)
        {
            var st = style.ToString();
            foreach (var x in AdminShellUtil.GetEnumValues<Microsoft.Msagl.Drawing.Shape>())
                if (x.ToString().Equals(st, StringComparison.InvariantCultureIgnoreCase))
                    return x;
            return Microsoft.Msagl.Drawing.Shape.Box;
        }

    }

    public class ListOfBomArguments : List<BomArguments>
    {
        [JsonIgnore]
        public AasReferenceStore<BomArguments> Store = new AasReferenceStore<BomArguments>();

        public void Index()
        {
            foreach (var ls in this)
                Store.Index(ExtendReference.CreateFromKey(ls.Match), ls);
        }
    }
}
