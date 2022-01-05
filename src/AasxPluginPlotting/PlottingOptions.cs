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
// As of Dec 2021, nobody was known using some handcrafted "AasxPluginPlotting.options.json".
// This is, because plotting plugin is only initally published in Dec 2021.
// A migration by hand was done.
// Therefore it seems to be fair enough not to implement version upgrades, yet.
// However, AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir() is already used and can
// easily engaged for this.

namespace AasxPluginPlotting
{
    public class PlottingOptionsRecord : AasxIntegrationBase.AasxPluginOptionsLookupRecordBase
    {
    }

    public class PlottingOptions : AasxIntegrationBase.AasxPluginLookupOptionsBase
    {
        public List<PlottingOptionsRecord> Records = new List<PlottingOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static PlottingOptions CreateDefault()
        {
            var rec = new PlottingOptionsRecord();
            rec.AllowSubmodelSemanticId.Add(
                AasxPredefinedConcepts.Plotting.Static.SEM_PlottingSubmodel.GetAsIdentifier());

            var opt = new PlottingOptions();
            opt.Records.Add(rec);

            return opt;
        }
    }
}
