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

// Note on V3.0:
// As of Dec 2021, nobody was known using some handcrafted "KnownSubmodelsOptions.options.json".
// It is even not existing,yet.
// Therefore it seems to be fair enough not to implement version upgrades, yet.
// However, AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir() is already used and can
// easily engaged for this.

namespace AasxPluginKnownSubmodels
{
    public class KnownSubmodelsOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
        public string Header;
        public string Content;
        public string ImageLink;
        public string FurtherUrl;
    }

    public class KnownSubmodelsOptions : AasxPluginLookupOptionsBase
    {
        public List<KnownSubmodelsOptionsRecord> Records = new List<KnownSubmodelsOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static KnownSubmodelsOptions CreateDefault()
        {
            var rec = new KnownSubmodelsOptionsRecord()
            {
                Header = "ZVEI Contact Information (Version 1.0)",
                Content = "This submodel template aims at interoperable provision of contact information in regard " +
                    "to the asset of the respective Asset Administration Shell. " +
                    "The intended use-case is the provision of a standardized property structure for contact " +
                    "information, which can effectively accelerate the preperation for asset maintenance.",
                ImageLink = "AasxPluginKnownSubmodels.media\\SMT_ZveiContactInformation10.png",
                FurtherUrl = "https://github.com/admin-shell-io/id"
            };
            rec.AllowSubmodelSemanticId.Add(
                AasxPredefinedConcepts.ZveiContactInformationV10.Static.SM_ContactInformation.GetAutoSingleId());

            var opt = new KnownSubmodelsOptions();
            opt.Records.Add(rec);

            return opt;
        }
    }
}
