/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AasxIntegrationBase;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPluginExportTable.Uml
{
    /// <summary>
    /// Some user options for exporting UML
    /// </summary>
    public class ExportUmlRecord
    {
        public enum ExportFormat { Xmi11 = 0, Xmi21, PlantUml }

        public static string[] FormatNames =
        {
            "XMI v1.1 (UML1.3, EA flavour, only limit information, no associations)",
            "XMI v2.1 (UML2.1, EA flavour, associations mis-shaped)",
            "PlantUML (text format, www.plantuml.com)"
        };

        public ExportFormat Format = ExportFormat.PlantUml;

#if __not_promising

        public static string[] PlantFormatNames =
        {
            "(0) PlantUML with classes and attributes",
            "(1) PlantUML with key/values maps",
            "(2) PlantUML with key/values maps (Entity elements only)"
        };

        public enum PlantUmlFormat { Classes = 0, Maps = 1, Entities = 2 }

        [AasxMenuArgument(help: "PlantUMP sub format (0..)")]
        public PlantUmlFormat PlantFormat = 0;

#endif

        [AasxMenuArgument(help: "Strings delimited by spaces will matched against class names and on " +
            "positive match will suppress rendering of such class.")]
        public string Suppress = null;

        [AasxMenuArgument(help: "If greater 0, will determine the depth level of UML generation. " +
            "A value of 1 will process only the top level and stop.")]
        public int Depth = 0;

        [AasxMenuArgument(help: "If greater or equal zero, limits the number of characters for inital values.")]
        public int LimitInitialValue = 15;

        [AasxMenuArgument(help: "If set, no members are rendered inside a class (PlantUML).")]
        public bool Outline = false;

        [AasxMenuArgument(help: "If set, changes the direction of adding graphical elements (PlantUML).")]
        public bool SwapDirection = false;

        [AasxMenuArgument(help: "If set, will use concept idShort as names of classes.")]
        public bool ClassesFromConcepts = false;

        [AasxMenuArgument(help: "If set, will not name the ends of an association, in order to " +
            "minimize graphical clutter.")]
        public bool NoAssociationNames = false;

        [AasxMenuArgument(help: "If set, system clipboard will have generated code, in order e.g. to directly " +
            "paste to PlantUML.")]
        public bool CopyToPasteBuffer = false;
    }
}
