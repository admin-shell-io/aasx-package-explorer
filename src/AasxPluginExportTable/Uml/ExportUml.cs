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
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxPluginExportTable.Uml
{
    /// <summary>
    /// This class allows exporting a Submodel to various UML formats.
    /// Note: it is a little misplaced in the "export table" plugin, however the
    /// domain is quite the same and maybe special file format dependencies will 
    /// be re equired in the future.
    /// </summary>
    public static class ExportUml
    {
        public static void ExportUmlToFile(
            AasCore.Aas3_0_RC02.Environment env,
            AasCore.Aas3_0_RC02.Submodel submodel,
            ExportUmlRecord options,
            string fn)
        {
            // access
            if (options == null | !fn.HasContent())
                return;

            // which writer?
            IBaseWriter writer = null;
            if (options.Format == ExportUmlRecord.ExportFormat.Xmi11)
                writer = new Xmi11Writer();
            if (options.Format == ExportUmlRecord.ExportFormat.Xmi21)
                writer = new Xmi21Writer();
            if (options.Format == ExportUmlRecord.ExportFormat.PlantUml)
                writer = new PlantUmlWriter();

            if (writer != null)
            {
                writer.StartDoc(options);
                writer.ProcessSubmodel(submodel);
                writer.ProcessPost();
                writer.SaveDoc(fn);
            }
        }
    }
}
