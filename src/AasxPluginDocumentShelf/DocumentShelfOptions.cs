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

namespace AasxPluginDocumentShelf
{
    /// <summary>
    /// Starting from Dec 2021, multiple records of options are foreseen.
    /// Background: use VDI2770 as base model and define multiple
    /// extensions.
    /// Idea: the record define minimal extension, most of the options
    /// hold true for the base.
    /// </summary>
    public class DocumentShelfOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
        /// <summary>
        /// As for "foreign" records the semanticId matching of V12 will fail,
        /// it can be controlled.
        /// Use case: "Documentations" used for PLC files, CAD, ..
        /// </summary>
        public DocumentEntity.SubmodelVersion ForceVersion = DocumentEntity.SubmodelVersion.V12;

        /// <summary>
        /// Give more usage information for this record / use of the Submodel.
        /// </summary>
        public string UsageInfo = null;
    }

    public class DocumentShelfOptions : AasxPluginLookupOptionsBase
    {
        public List<DocumentShelfOptionsRecord> Records = new List<DocumentShelfOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static DocumentShelfOptions CreateDefault()
        {
            var opt = new DocumentShelfOptions();

            //
            //  basic record
            //

            var rec = new DocumentShelfOptionsRecord();
            rec.ForceVersion = DocumentEntity.SubmodelVersion.Default;
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

			rec = new DocumentShelfOptionsRecord()
            {
                UsageInfo = "Some manufacturers use manufacturer documentation to provide models for " +
                "Computer Aided Design (CAD) and further engineering tools.",
                ForceVersion = DocumentEntity.SubmodelVersion.V11
            };
            opt.Records.Add(rec);

            rec.AllowSubmodelSemanticId.Add(new Aas.Key(
                Aas.KeyTypes.Submodel, "smart.festo.com/AAS/Submodel/ComputerAidedDesign/1/0"));

            rec.AllowSubmodelSemanticId.Add(new Aas.Key(
                Aas.KeyTypes.Submodel, "https://admin-shell.io/sandbox/idta/handover/MCAD/0/1/"));

            rec.AllowSubmodelSemanticId.Add(new Aas.Key(
                Aas.KeyTypes.Submodel, "https://admin-shell.io/sandbox/idta/handover/EFCAD/0/1/"));

            rec.AllowSubmodelSemanticId.Add(new Aas.Key(
                Aas.KeyTypes.Submodel, "https://admin-shell.io/sandbox/idta/handover/PLC/0/1/"));

            return opt;
        }
    }

}
