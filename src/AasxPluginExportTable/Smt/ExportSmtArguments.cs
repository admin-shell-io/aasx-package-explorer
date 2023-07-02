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
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;

namespace AasxPluginExportTable
{
    public class ExportSmtArguments
    {
        // ReSharper disable UnassignedField.Global

        /// <summary>
        /// For image link elements: If set to true, no link text will be created.
        /// </summary>
        public bool noLink = false;

        /// <summary>
        /// For image link elements: If set, the filename of the target file which is to be created.
        /// </summary>
        public string fileName = null;

        // ReSharper enable UnassignedField.Global

        public static ExportSmtArguments Parse(string json)
        {
            if (!json.HasContent())
                return null;

            try
            {
                var res = Newtonsoft.Json.JsonConvert.DeserializeObject<ExportSmtArguments>(json);
                return res;
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            return null;
        }
    }
}
