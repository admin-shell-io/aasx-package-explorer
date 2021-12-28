/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AdminShellNS;
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
        /// As for "foreign" records the semanticId matching of V11 will fail,
        /// it can be controlled.
        /// </summary>
        public DocumentEntity.SubmodelVersion ForceVersion = DocumentEntity.SubmodelVersion.V11;

        /// <summary>
        /// Give more usage information for this record / use of the Submodel.
        /// </summary>
        public string UsageInfo = null;
    }

    public class DocumentShelfOptions : AasxIntegrationBase.AasxPluginLookupOptionsBase
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
            var semIdDocumentation = preDefs.SM_VDI2770_Documentation?.semanticId?.GetAsExactlyOneKey();
            if (semIdDocumentation != null)
                rec.AllowSubmodelSemanticId.Add(semIdDocumentation);
                
            // V1.1
            rec.AllowSubmodelSemanticId.Add(
                AasxPredefinedConcepts.VDI2770v11.Static.SM_ManufacturerDocumentation.GetSemanticKey());

            //
            // further models for CAD
            //

            rec = new DocumentShelfOptionsRecord()
            {
                UsageInfo = "Some manufacturers use manufacturer documentation to provide models for " +
                "Computer Aided Design (CAD) and further engineering tools."
            };
            opt.Records.Add(rec);

            rec.AllowSubmodelSemanticId.Add(new AdminShellV20.Key(
                AdminShell.Key.Submodel, false, AdminShell.Identification.IRI,
                "smart.festo.com/AAS/Submodel/ComputerAidedDesign/1/0"));

            return opt;
        }
    }

}
