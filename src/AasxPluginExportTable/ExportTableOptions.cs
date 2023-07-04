/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPluginExportTable.Uml;
using AasxPluginExportTable.TimeSeries;
using AasxPluginExportTable.Table;
using AdminShellNS;

namespace AasxPluginExportTable
{
    public class ExportTableOptions : AasxPluginOptionsBase
    {
        public string TemplateIdConceptDescription = "www.example.com/ids/cd/DDDD_DDDD_DDDD_DDDD";

        public string SmtExportHtmlCmd = "";
        public string SmtExportHtmlArgs = "";
        public string SmtExportPdfCmd = "";
        public string SmtExportPdfArgs = "";

        public ExportUmlRecord UmlExport = null;

        public ImportTimeSeriesRecord TimeSeriesImport = null;

        public List<ImportExportTableRecord> Presets = new List<ImportExportTableRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static ExportTableOptions CreateDefault()
        {
            var opt = new ExportTableOptions();
            return opt;
        }
    }
}
