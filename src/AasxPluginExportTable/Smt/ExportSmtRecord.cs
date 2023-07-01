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
using AasxIntegrationBase;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPluginExportTable.Smt
{
    /// <summary>
    /// Some user options for exporting package to a (AsciiDoc) SMT specification
    /// </summary>
    public class ExportSmtRecord
    {
        public enum ExportFormat { Adoc = 0, Zip }

        public static string[] FormatNames =
        {
            "Adoc - single file",
            "ZIP - archive with included media and export files"
        };

        public ExportFormat Format = ExportFormat.Adoc;

        public string PresetTables = "";

        public bool ExportHtml = false;
        public bool ExportPdf = false;
    }
}
