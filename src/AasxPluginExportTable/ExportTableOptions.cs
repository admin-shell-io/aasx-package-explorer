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
using AdminShellNS;

// Note on V3.0:
// As of today, the options neither use Records nor use AdminShell meta model entities, yet.
// Therefore it seems to be fair enough not to implement version upgrades, yet.
// However, AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir() is already used and can
// easily engaged for this.

namespace AasxPluginExportTable
{
    public class ExportTableOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        public string TemplateIdConceptDescription = "www.example.com/ids/cd/DDDD_DDDD_DDDD_DDDD";

        public Uml.ExportUmlOptions UmlExport = null;

        public List<ExportTableRecord> Presets = new List<ExportTableRecord>();

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
