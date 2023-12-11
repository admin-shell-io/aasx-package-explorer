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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Shapes;
using System.Xml;

namespace WpfMtpControl
{
    public class MtpSymbolMapRecord
    {
        /// <summary>
        /// Applicable eClass Versions, delimited by ";". If empty, then any!
        /// </summary>
        public string EClassVersions = "";

        /// <summary>
        /// Applicable eClass Classes, delimited by ";". Only digits allow ("01024455"), 
        /// If empty, then eClassIRDI shall be set.
        /// </summary>
        public string EClassClasses = null;

        /// <summary>
        /// Applicable eClass IRIDs, delimited by ";". If empty, then eClassClass shall be set.
        /// </summary>
        public string EClassIRDIs = null;

        /// <summary>
        /// For the default (off) state. Full name of the symbol, that is "LibName"."SymbolName",
        /// </summary>
        public string SymbolDefault = null;

        /// <summary>
        /// For the active (on) state. Full name of the symbol, that is "LibName"."SymbolName",
        /// </summary>
        public string SymbolActive = null;

        /// <summary>
        /// For the intermediate (transitioning) state. Full name of the symbol, that is "LibName"."SymbolName",
        /// </summary>
        public string SymbolIntermediate = null;

        /// <summary>
        /// Free use.
        /// </summary>
        public string Comment = null;

        /// <summary>
        /// Priority for finding best match. Increasing is higher prio.
        /// </summary>
        public int Priority = 1;

        //
        // Constructors
        //

        public MtpSymbolMapRecord() { }

        public MtpSymbolMapRecord(string EClassVersions, string EClassClasses, string EClassIRDIs,
            string SymbolDefault, string SymbolActive = null, string SymbolIntermediate = null,
            string Comment = null, int Priority = 1)
        {
            this.EClassVersions = EClassVersions;
            this.EClassClasses = EClassClasses;
            this.EClassIRDIs = EClassIRDIs;
            this.SymbolDefault = SymbolDefault;
            this.SymbolActive = SymbolActive;
            this.SymbolIntermediate = SymbolIntermediate;
            this.Comment = Comment;
            this.Priority = Priority;
        }
    }

    public class MtpSymbolMapRecordList : List<MtpSymbolMapRecord>
    {
    }

    public class MtpVisualObjectRecord
    {
        /// <summary>
        /// Helpful name
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Applicable eClass Versions. If empty, then any!
        /// </summary>
        public Dictionary<string, string> eClassVersions = null;

        /// <summary>
        /// Applicable eClass Classes. Only digits allow ("01024455"), If empty, then eClassIRDI shall be set.
        /// </summary>
        public Dictionary<string, string> eClassClasses = null;

        /// <summary>
        /// Applicable eClass IRIDs. If empty, then eClassClass shall be set.
        /// </summary>
        public Dictionary<string, string> eClassIRDIs = null;

        /// <summary>
        /// Reference to symbol, which is used
        /// </summary>
        public MtpSymbol Symbol = null;

        /// <summary>
        /// Preffered placement type
        /// </summary>
        public MtpSymbol.SymbolPlaceType Placement = MtpSymbol.SymbolPlaceType.StretchToBoundingBox;

        /// <summary>
        /// Preferred label alignment
        /// </summary>
        public UIElementHelper.DrawToCanvasAlignment LabelAlignment = UIElementHelper.DrawToCanvasAlignment.Centered;

        /// <summary>
        /// Priority for finding best match. Increasing is higher prio.
        /// </summary>
        public int Priority = 1;

        public MtpVisualObjectRecord() { }

        /// <summary>
        /// Initialize a record.
        /// </summary>
        /// <param name="Name">Name. Not used for matching.</param>
        /// <param name="Symbol"></param>
        /// <param name="eClassVersions">Applicable eClass Versions, delimited by ";". If empty, then any!</param>
        /// <param name="eClassClasses">Applicable eClass Classes, delimited by ";". Only digits allow ("01024455"), 
        /// If empty, then eClassIRDI shall be set.</param>
        /// <param name="eClassIRDIs">Applicable eClass IRIDs, delimited by ";". If empty, then eClassClass shall 
        /// be set.</param>
        /// <param name="placement"></param>
        /// <param name="labelAlignment"></param>
        /// <param name="prio"></param>
        public MtpVisualObjectRecord(
            string Name,
            MtpSymbol Symbol,
            string eClassVersions = null,
            string eClassClasses = null,
            string eClassIRDIs = null,
            Nullable<MtpSymbol.SymbolPlaceType> placement = null,
            UIElementHelper.DrawToCanvasAlignment labelAlignment = UIElementHelper.DrawToCanvasAlignment.Centered,
            int prio = 1)
        {
            this.Name = Name;
            this.Symbol = Symbol;
            this.eClassVersions = PrepDict(eClassVersions);
            this.eClassClasses = PrepDict(eClassClasses);
            this.eClassIRDIs = PrepDict(eClassIRDIs);
            if (placement != null)
                this.Placement = placement.Value;
            this.LabelAlignment = labelAlignment;
            this.Priority = prio;
        }

        public MtpVisualObjectRecord(MtpSymbolLib symbolLib, MtpSymbolMapRecord config)
        {
            if (config != null)
            {
                this.Name = config.SymbolDefault;
                this.eClassVersions = PrepDict(config.EClassVersions);
                this.eClassClasses = PrepDict(config.EClassClasses);
                this.eClassIRDIs = PrepDict(config.EClassIRDIs);

                if (symbolLib != null && symbolLib.ContainsKey(config.SymbolDefault))
                {
                    var symbol = symbolLib[config.SymbolDefault];
                    this.Symbol = symbol;
                    this.Placement = symbol.PreferredPlacement;
                    this.LabelAlignment = symbol.PreferredLabelAlignment;
                }

                this.Priority = config.Priority;
            }

        }

        public static Dictionary<string, string> PrepDict(string str)
        {
            if (str == null)
                return null;
            var arr = str.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (arr == null || arr.Length < 1)
                return null;
            var res = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var a in arr)
                res.Add(a, a);
            return res;
        }

        public static string FilterEclassClass(string input)
        {
            if (input == null)
                return null;
            var justNumbers = new String(input.Where(Char.IsDigit).ToArray());
            return justNumbers;
        }

    }

    public class MtpVisualObjectLib
    {
        private List<MtpVisualObjectRecord> records = new List<MtpVisualObjectRecord>();

        /// <summary>
        /// Call once to load the static definitions, which are contained in the resources.
        /// </summary>
        public void LoadStatic(MtpSymbolLib syms)
        {

            // add elements
            records.Add(new MtpVisualObjectRecord("Pressurized_vessel_horizontal",
                syms["PNID_ISO10628.Pressurized_vessel_horizontal"],
                eClassVersions: "10.1", eClassClasses: "99999999", prio: 0));

            records.Add(new MtpVisualObjectRecord("Pressurized_vessel_vertical",
                syms["PNID_ISO10628.Pressurized_vessel_vertical"],
                eClassClasses: "36020190;36030101", prio: 0));

            records.Add(new MtpVisualObjectRecord("Shutoff_Valve",
                syms["PNID_ISO10628.Shutoff_Valve_nozzled"],
                eClassVersions: "10.1", eClassClasses: "37010201",
                placement: MtpSymbol.SymbolPlaceType.FitNozzles, prio: 0));

            //// records.Add(new MtpVisualObjectRecord("Pump_general",
            ////    syms["PNID_ISO10628.Pump_general_nozzled"],
            ////    eClassClasses: "36419090;3641", placement: MtpSymbol.SymbolPlaceType.FitNozzles, prio: 0));

            records.Add(new MtpVisualObjectRecord("Sensor_general",
                syms["PNID_ISO10628.Sensor_general"],
                eClassClasses: "27209090;27143121;27200492;27200600;27200200;27200589", prio: 0));

            records.Add(new MtpVisualObjectRecord("Controller_general",
                syms["PNID_ISO10628.Sensor_general"],
                eClassClasses: "27210790;27210101", prio: 0));

            records.Add(new MtpVisualObjectRecord("Source_general",
                syms["PNID_ISO10628.Source_tagged"],
                placement: MtpSymbol.SymbolPlaceType.FitNozzles,
                labelAlignment: UIElementHelper.DrawToCanvasAlignment.North, prio: 0));

            records.Add(new MtpVisualObjectRecord("Sink_general",
                syms["PNID_ISO10628.Sink_tagged"],
                placement: MtpSymbol.SymbolPlaceType.FitNozzles,
                labelAlignment: UIElementHelper.DrawToCanvasAlignment.South, prio: 0));

            // access resource dictionaries FESTO

            records.Add(new MtpVisualObjectRecord("Control_Valve",
                syms["PNID_Festo.control_valve_tagged-u-nozzled"],
                eClassClasses: "37010203", placement: MtpSymbol.SymbolPlaceType.FitNozzles, prio: 0));

            records.Add(new MtpVisualObjectRecord("Manual_Valve",
                syms["PNID_Festo.manual_valve_default-u-nozzled"],
                eClassClasses: "37010201", placement: MtpSymbol.SymbolPlaceType.FitNozzles, prio: 0));

            records.Add(new MtpVisualObjectRecord("Stirrer",
                syms["PNID_Festo.stirrer_active-u"],
                eClassClasses: "36090590", placement: MtpSymbol.SymbolPlaceType.StretchToBoundingBox, prio: 0));

            records.Add(new MtpVisualObjectRecord("Pump_general",
                syms["PNID_Festo.pump_active-r-nozzled"],
                eClassClasses: "36419090;3641", placement: MtpSymbol.SymbolPlaceType.FitNozzles, prio: 0));
        }

        public void LoadFromSymbolMappings(MtpSymbolLib symbolLib, IEnumerable<MtpSymbolMapRecord> mappings)
        {
            // access
            if (symbolLib == null | mappings == null)
                return;

            // each ..
            foreach (var map in mappings)
                records.Add(new MtpVisualObjectRecord(symbolLib, map));
        }

        public MtpVisualObjectRecord FindVisualObjectByName(string name)
        {
            // try find
            foreach (var rec in records)
            {
                // try to find negative events!
                if (name != null && name.Length > 0 && rec.Name.ToLower() != name.Trim().ToLower())
                    continue;
                // ok
                return rec;
            }

            // end of search
            return null;
        }

        public static bool MatchEclassClass(Dictionary<string, string> recClasses, string thisClass)
        {
            // specified in record? .. if not, default: True
            if (recClasses == null || recClasses.Count < 1)
                return true;
            if (thisClass == null || thisClass.Length < 1)
                return false;

            // split rec classes
            foreach (var rc in recClasses.Values)
            {
                // require to be formally valid
                var rcc = rc.Trim();

                // ok, if partially match
                if (thisClass.StartsWith(rcc))
                    return true;
            }

            // ok, not
            return false;
        }

        public MtpVisualObjectRecord FindVisualObjectByClass(
            string eClassVersion = null, string eClassClass = null, string eClassIRDI = null)
        {
            // prepare input
            eClassClass = MtpVisualObjectRecord.FilterEclassClass(eClassClass);

            // minimal match
            if (eClassClass != null)
                eClassClass = eClassClass.Trim();
            if (eClassIRDI != null)
                eClassIRDI = eClassIRDI.Trim();
            if ((eClassClass == null || eClassClass.Length < 1)
                 && (eClassIRDI == null || eClassIRDI.Length < 1))
                return null;

            // try find
            MtpVisualObjectRecord foundRec = null;
            int foundPrio = -1;

            foreach (var rec in records)
            {
                // record either needs to have class or irdi
                if ((rec.eClassClasses == null || rec.eClassClasses.Count < 1)
                    && (rec.eClassIRDIs == null || rec.eClassIRDIs.Count < 1))
                    continue;

                // try to find negative events!
                if (eClassVersion != null && eClassVersion.Length > 0 && rec.eClassVersions != null
                    && !rec.eClassVersions.ContainsKey(eClassVersion))
                    continue;
                //// if (eClassClass != null && eClassClass.Length > 0 && rec.eClassClasses != null 
                ////    && !rec.eClassClasses.ContainsKey(eClassClass))
                ////    continue;
                if (!MatchEclassClass(rec.eClassClasses, eClassClass))
                    continue;
                if (eClassIRDI != null && eClassIRDI.Length > 0 && rec.eClassIRDIs != null
                    && !rec.eClassIRDIs.ContainsKey(eClassIRDI))
                    continue;

                // check if better?
                if (rec.Priority >= foundPrio)
                {
                    foundRec = rec;
                    foundPrio = rec.Priority;
                }
            }

            // end of search
            return foundRec;
        }
    }
}
