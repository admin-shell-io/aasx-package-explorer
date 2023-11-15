/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;

namespace AasxPluginAID
{

    public class IntefaceShelfOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
        public string UsageInfo = null;
    }

    public class InterfaceShelfOptions : AasxPluginLookupOptionsBase
    {
        public List<IntefaceShelfOptionsRecord> Records = new List<IntefaceShelfOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static InterfaceShelfOptions CreateDefault()
        {
            var opt = new InterfaceShelfOptions();

            //
            //  basic record
            //

            var rec = new IntefaceShelfOptionsRecord();
            opt.Records.Add(rec);

            // V1.0
            var preDefs = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(
                    new AasxPredefinedConcepts.DefinitionsVDI2770());
            var semIdDocumentation = preDefs.SM_VDI2770_Documentation?.SemanticId?.GetAsExactlyOneKey();
            if (semIdDocumentation != null)
                rec.AllowSubmodelSemanticId.Add(semIdDocumentation);

            // V1.1
            rec.AllowSubmodelSemanticId.Add(
                AasxPredefinedConcepts.VDI2770v11.Static
                    .SM_ManufacturerDocumentation.GetSemanticKey());

            // V1.2
            rec.AllowSubmodelSemanticId.Add(
                AasxPredefinedConcepts.IdtaHandoverDocumentationV12.Static
                    .SM_HandoverDocumentation.GetSemanticKey());

            //
            // further models for CAD
            // (for the time being still V11!)
            //

            rec = new IntefaceShelfOptionsRecord()
            {
                UsageInfo = "Some manufacturers use manufacturer documentation to provide models for " +
                "Computer Aided Design (CAD) and further engineering tools.",
            };
            opt.Records.Add(rec);

            return opt;
        }
    }

}
