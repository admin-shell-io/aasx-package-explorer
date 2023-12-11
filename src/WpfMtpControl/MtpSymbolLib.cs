/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfMtpControl
{
    public class MtpSymbol
    {
        /// <summary>
        /// Different strategies of dealing with a geometric object
        /// </summary>
        public enum SymbolPlaceType { StretchToBoundingBox, FitNozzles }

        /// <summary>
        /// Symbolic name / ID of the library, as given by static initialization of the symbol lib
        /// </summary>
        public string LibName = "";

        /// <summary>
        /// Symbolic name of the symbol, as derived from the Xaml.Key property
        /// </summary>
        public string SymbolName = "";

        /// <summary>
        /// Getter for the full name, composed from LibName and "." and SymbolName
        /// </summary>
        public string FullName { get { return LibName + "." + SymbolName; } }

        /// <summary>
        /// Preffered placement type
        /// </summary>
        public SymbolPlaceType PreferredPlacement = SymbolPlaceType.StretchToBoundingBox;

        /// <summary>
        /// Preferred label alignment
        /// </summary>
        public UIElementHelper.DrawToCanvasAlignment PreferredLabelAlignment
            = UIElementHelper.DrawToCanvasAlignment.Centered;

        /// <summary>
        /// Object data of the symbol, expected to be XAML Canvas element.
        /// </summary>
        /// 
        private object symbolData = null;
        public object SymbolData
        {
            get { return this.symbolData; }
            set { this.SetSymbol(value); }
        }

        //
        // Constructors
        //

        public MtpSymbol() { }

        public MtpSymbol(string LibName, string SymbolName, object Symbol)
        {
            this.LibName = LibName;
            this.SymbolName = SymbolName;
            this.SymbolData = Symbol;
        }

        public MtpSymbol(MtpSymbol other)
        {
            this.LibName = "" + other?.LibName;
            this.SymbolName = "" + other?.SymbolName;
            this.SymbolData = other?.SymbolData;
        }

        public MtpSymbol(string LibName, ResourceDictionary rd, string SymbolName)
        {
            this.LibName = LibName;
            this.SymbolName = SymbolName;
            this.SymbolData = rd[SymbolName];
        }

        //
        // Evaluation of derived properties
        //

        /// <summary>
        /// Nozzle positions found in the XAML shape
        /// </summary>
        public Point[] NozzlePos = null;

        /// <summary>
        /// Found label positions for the shape. Top=0, Right=1, Bottom=2, Left=3
        /// </summary>
        public Point[] LabelPos = null;

        public void SetSymbol(object XamlContent)
        {
            // assume to be a canvas
            var canvasContent = XamlContent as Canvas;
            if (canvasContent == null)
                return;

            // clone the canvas, as later accessing functionality shall not see Nozzles, Labels artifiacts in XAML
            var clonedCanvas = UIElementHelper.cloneElement(canvasContent) as Canvas;
            if (clonedCanvas == null)
                return;

            // remember this as Symbol!
            this.symbolData = clonedCanvas;

            // find named nozzles and remove artifacts!
            this.NozzlePos = UIElementHelper.FindNozzlesViaTags(clonedCanvas, "Nozzle", extractShapes: true);

            // find label positions and remove artifacts!
            this.LabelPos = UIElementHelper.FindNozzlesViaTags(clonedCanvas, "Label", extractShapes: true);
        }
    }

    public class MtpSymbolLib : Dictionary<string, MtpSymbol>
    {
        public object GetSymbol(string Fullname)
        {
            if (this.ContainsKey(Fullname))
                return this[Fullname].SymbolData;
            return null;
        }

        public void ImportResourceDicrectory(string LibName, ResourceDictionary rd)
        {
            // access
            if (rd == null)
                return;

            // over all
            foreach (var key in rd.Keys)
            {
                // only take Canvas as symbol root element
                if (rd[key] is Canvas)
                {
                    // symbol
                    var sym = new MtpSymbol(LibName, rd, "" + key);

                    // use name to figure out some options
                    if (("" + key).ToLower().Contains("nozzled"))
                        sym.PreferredPlacement = MtpSymbol.SymbolPlaceType.FitNozzles;

                    // add
                    this.Add(sym.FullName, sym);
                }
            }
        }
    }
}
