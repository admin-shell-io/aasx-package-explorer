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

        public MtpSymbolMapRecord(MtpSymbolMapRecord src)
        {
            if (src == null)
                return;
            
            EClassVersions = src.EClassVersions;
            EClassClasses = src.EClassClasses;
            EClassIRDIs = src.EClassIRDIs;
            SymbolDefault = src.SymbolDefault;
            SymbolActive = src.SymbolActive;
            SymbolIntermediate = src.SymbolIntermediate;
            Comment = src.Comment;
            Priority = src.Priority;
        }

#if !DoNotUseAasxCompatibilityModels

        public MtpSymbolMapRecord(AasxCompatibilityModels.WpfMtpControl.MtpSymbolMapRecordV20 src)
        {
            if (src == null)
                return;

            EClassVersions = src.EClassVersions;
            EClassClasses = src.EClassClasses;
            EClassIRDIs = src.EClassIRDIs;
            SymbolDefault = src.SymbolDefault;
            SymbolActive = src.SymbolActive;
            SymbolIntermediate = src.SymbolIntermediate;
            Comment = src.Comment;
            Priority = src.Priority;
        }

#endif
    }

    public class MtpSymbolMapRecordList : List<MtpSymbolMapRecord>
    {
    }
}
