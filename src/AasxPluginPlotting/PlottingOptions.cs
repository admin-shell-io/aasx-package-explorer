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
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AasxIntegrationBase;

namespace AasxPluginPlotting
{
    public class PlottingOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
    }

    public class PlottingOptions : AasxPluginLookupOptionsBase
    {
        public List<PlottingOptionsRecord> Records = new List<PlottingOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static PlottingOptions CreateDefault()
        {
            var rec = new PlottingOptionsRecord();
            rec.AllowSubmodelSemanticId.Add(
                AasxPredefinedConcepts.Plotting.Static.SEM_PlottingSubmodel.GetAsExactlyOneKey());

            var opt = new PlottingOptions();
            opt.Records.Add(rec);

            return opt;
        }
    }
}
