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

namespace AasxPluginBomStructure
{
    public class BomStructureOptionsRecord
    {
        public List<AdminShell.Key> AllowSubmodelSemanticId = new List<AdminShell.Key>();
    }

    public class BomStructureOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        public List<BomStructureOptionsRecord> Records = new List<BomStructureOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static BomStructureOptions CreateDefault()
        {
            var rec = new BomStructureOptionsRecord();
            rec.AllowSubmodelSemanticId.Add(AdminShell.Key.CreateNew(
                type: "Submodel",
                local: false,
                idType: "IRI",
                value: "http://smart.festo.com/id/type/submodel/BOM/1/1"));

            var opt = new BomStructureOptions();
            opt.Records.Add(rec);

            return opt;
        }
    }
}
