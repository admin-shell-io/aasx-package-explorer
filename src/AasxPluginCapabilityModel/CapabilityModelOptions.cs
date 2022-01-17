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
using AdminShellNS;

namespace AasxPluginCapabilityModel
{
    public class CapabilityModelOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
        public string Placeholder = "";
    }

    public class CapabilityModelOptions : AasxPluginLookupOptionsBase
    {
        public List<CapabilityModelOptionsRecord> Records = new List<CapabilityModelOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static CapabilityModelOptions CreateDefault()
        {
            var rec = new CapabilityModelOptionsRecord()
            {
                Placeholder = "For demonstration purpose, only."
            };
            rec.AllowSubmodelSemanticId.Add(
                new AdminShell.Key(AdminShell.Key.GlobalReference, false, AdminShell.Identification.IRI,
                "https://admin-shell.io/sandbox/FhG/CapabilityModel/Submodel/1/0"));

            var opt = new CapabilityModelOptions();
            opt.Records.Add(rec);

            return opt;
        }
    }
}
