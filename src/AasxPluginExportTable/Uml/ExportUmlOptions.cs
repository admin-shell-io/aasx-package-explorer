/*
Copyright (c) 2018-2022 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPluginExportTable.Uml
{
    /// <summary>
    /// Some user options for exporting UML
    /// </summary>
    public class ExportUmlOptions
    {
        public enum ExportFormat { Xmi11 = 0, Xmi21, PlantUml }

        public static string[] FormatNames =
        {
            "XMI v1.1 (UML1.3, EA flavour, only limit information, no associations)",
            "XMI v2.1 (UML2.1, EA flavour, associations mis-shaped)",
            "PlantUML (text format, www.plantuml.com)"
        };

        public ExportFormat Format = ExportFormat.Xmi21;

        public int LimitInitialValue = 15;

        public bool CopyToPasteBuffer = false;
    }
}
