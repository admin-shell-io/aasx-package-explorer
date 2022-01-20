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
// As of Dec 2021, nobody was known using some handcrafted "AasxPluginImageMap.options.json".
// It is even not existing,yet.
// Therefore it seems to be fair enough not to implement version upgrades, yet.
// However, AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir() is already used and can
// easily engaged for this.

namespace AasxPluginImageMap
{
    public class ImageMapOptionsOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
    }

    public class ImageMapOptions : AasxIntegrationBase.AasxPluginLookupOptionsBase
    {
        public List<ImageMapOptionsOptionsRecord> Records = new List<ImageMapOptionsOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static ImageMapOptions CreateDefault()
        {
            var rec = new ImageMapOptionsOptionsRecord();
            rec.AllowSubmodelSemanticId.Add(
                AasxPredefinedConcepts.ImageMap.Static.SEM_ImageMapSubmodel.GetAsIdentifier());

            var opt = new ImageMapOptions();
            opt.Records.Add(rec);

            return opt;
        }
    }
}
