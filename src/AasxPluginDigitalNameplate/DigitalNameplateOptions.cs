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

namespace AasxPluginDigitalNameplate
{
    /// <summary>
    /// Starting from Dec 2021, multiple records of options are foreseen.
    /// </summary>
    public class DigitalNameplateOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
        public enum ParserEnum { V10, V20 };

        public ParserEnum Parser = DigitalNameplateOptionsRecord.ParserEnum.V10;
        public string Explanation = "";
    }

    public class DigitalNameplateOptions : AasxPluginLookupOptionsBase
    {
        public List<DigitalNameplateOptionsRecord> Records = new List<DigitalNameplateOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static DigitalNameplateOptions CreateDefault()
        {
            var opt = new DigitalNameplateOptions();

            // re-used

            var dpp = "This Digital Nameplate stands for a standardized Submodel of the " +
                "Asset Administration Shell (AAS). It is based on IEC 61406 series for identification of the " +
                "asset and IEC 63278 series for interoperable access of information. " +
                "Submodel, AAS and IEC standards are, among others, also important building blocks of the " +
                "Digital Product Passwort ( DPP4.0 ) initiative. \n\n";

            // V1.0

            var rec = new DigitalNameplateOptionsRecord();
            opt.Records.Add(rec);

            rec.AllowSubmodelSemanticId.Add(
                AasxPredefinedConcepts.ZveiNameplateV10.Static.SM_Nameplate.GetSemanticKey());

            rec.Parser = DigitalNameplateOptionsRecord.ParserEnum.V10;

            rec.Explanation = dpp +
               "This is version V1.0 of the Submodel for digital nameplate. It was originally created by the " +
               "ZVEI association in 2021.";

            // V2.0

            rec = new DigitalNameplateOptionsRecord();
            opt.Records.Add(rec);

            rec.AllowSubmodelSemanticId.Add(
                AasxPredefinedConcepts.DigitalNameplateV20.Static.SM_Nameplate.GetSemanticKey());

            rec.Parser = DigitalNameplateOptionsRecord.ParserEnum.V20;

            rec.Explanation = dpp +
               "This is version V2.0 of the Submodel for digital nameplate. It is maintained by " +
               "the Industrial Digital Twin Association (IDTA). It currently features a mix of URI and " +
               "ECLASS properties and is already prepared to be updated with IEC CDD properties.";

            return opt;
        }
    }

}
