/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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


namespace AasxPluginTechnicalData
{
    public class TechnicalDataOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
    }

    public class TechnicalDataOptions : AasxPluginLookupOptionsBase
    {
        public List<TechnicalDataOptionsRecord> Records = new List<TechnicalDataOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static TechnicalDataOptions CreateDefault()
        {
            // definitions
            var defsV10 = new AasxPredefinedConcepts.DefinitionsZveiTechnicalData.SetOfDefs(
                    new AasxPredefinedConcepts.DefinitionsZveiTechnicalData());
            var defsV11 = AasxPredefinedConcepts.ZveiTechnicalDataV11.Static;
            var defsV12 = AasxPredefinedConcepts.IdtaTechnicalDataV12.Static;

            // records

            var opt = new TechnicalDataOptions();

            var rec10 = new TechnicalDataOptionsRecord();
            rec10.AllowSubmodelSemanticId.Add(defsV10.SM_TechnicalData.GetSemanticKey());
            opt.Records.Add(rec10);

            var rec11 = new TechnicalDataOptionsRecord();
            rec11.AllowSubmodelSemanticId.Add(defsV11.SM_TechnicalData.GetSemanticKey());
            opt.Records.Add(rec11);

            var rec12 = new TechnicalDataOptionsRecord();
            rec11.AllowSubmodelSemanticId.Add(defsV12.SM_TechnicalData.GetSemanticKey());
            opt.Records.Add(rec12);

            return opt;
        }
    }
}
