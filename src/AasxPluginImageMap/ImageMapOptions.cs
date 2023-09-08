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

namespace AasxPluginImageMap
{
    public class ImageMapOptionsOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
    }

    public class ImageMapOptions : AasxPluginLookupOptionsBase
    {
        public List<ImageMapOptionsOptionsRecord> Records = new List<ImageMapOptionsOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static ImageMapOptions CreateDefault()
        {
            var rec = new ImageMapOptionsOptionsRecord();
            rec.AllowSubmodelSemanticId.Add(
                AasxPredefinedConcepts.ImageMap.Static.SEM_ImageMapSubmodel.GetAsExactlyOneKey());

            var opt = new ImageMapOptions();
            opt.Records.Add(rec);

            return opt;
        }
    }
}
