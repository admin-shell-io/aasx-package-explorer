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

// Note on V3.0:
// As of Dec 2021, nobody was known using some handcrafted "AasxPluginTechnicalData.options.json".
// This is, because plotting plugin is only initally published in Dec 2021.
// A migration by hand was done.
// Therefore it seems to be fair enough not to implement version upgrades, yet.
// However, AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir() is already used and can
// easily engaged for this.

namespace AasxPluginTechnicalData
{
    public class TechnicalDataOptionsRecord : AasxIntegrationBase.AasxPluginOptionsLookupRecordBase
    {
    }

    public class TechnicalDataOptions : AasxIntegrationBase.AasxPluginLookupOptionsBase
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

            // records

            var opt = new TechnicalDataOptions();

            var rec10 = new TechnicalDataOptionsRecord();
            rec10.AllowSubmodelSemanticId.Add(defsV10.SM_TechnicalData.GetAutoSingleId());
            opt.Records.Add(rec10);

            var rec11 = new TechnicalDataOptionsRecord();
            rec11.AllowSubmodelSemanticId.Add(defsV11.SM_TechnicalData.GetAutoSingleId());
            opt.Records.Add(rec11);

            return opt;
        }
    }
}
