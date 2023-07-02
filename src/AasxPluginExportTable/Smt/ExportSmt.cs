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
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using Newtonsoft.Json;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;

namespace AasxPluginExportTable.Smt
{
    /// <summary>
    /// This class allows exporting a Submodel to an AsciiDoc specification.
    /// The general approach is to identify several dedicated SME (mostly BLOBs) and
    /// to chunk together their AsciiDoc contents.
    /// </summary>
    public class ExportSmt
    {
        protected Aas.Environment Env = null;
        protected Aas.ISubmodel SrcSm = null;

        protected void ProcessTextBlock(Aas.IBlob blob)
        {

        }

        public void ExportSmtToFile(
            Aas.Environment env,
            Aas.ISubmodel submodel,
            ExportSmtRecord options,
            string fn)
        {
            // access
            if (options == null || submodel == null || !fn.HasContent())
                return;
            Env = env;
            SrcSm = submodel;

            // predefined semantic ids
            var defs = AasxPredefinedConcepts.AsciiDoc.Static;
            var mm = MatchMode.Relaxed;

            // walk the Submodel
            SrcSm.RecurseOnSubmodelElements(null, (o, parents, sme) =>
            {
                // semantic id
                var semId = sme?.SemanticId;
                if (semId?.IsValid() != true)
                    return true;

                // BLOB elements
                if (sme is Aas.IBlob blob)
                {
                    if (semId.Matches(defs.CD_TextBlock.GetCdReference(), mm))
                        ProcessTextBlock(blob);
                }

                // go further on
                return true;
            });
        }
    }
}
