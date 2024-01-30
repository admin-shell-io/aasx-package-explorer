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

namespace AasxPluginAID
{
    public class IntefaceShelfOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
        public string UsageInfo = null;
    }

    public class InterfaceShelfOptions : AasxPluginLookupOptionsBase
    {
        public List<IntefaceShelfOptionsRecord> Records = new List<IntefaceShelfOptionsRecord>();
        public static AasxPredefinedConcepts.IDTAAid idtaDef = AasxPredefinedConcepts.IDTAAid.Static;

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static InterfaceShelfOptions CreateDefault()
        {
            var opt = new InterfaceShelfOptions();

            //
            //  basic record
            //

            var rec = new IntefaceShelfOptionsRecord();
            opt.Records.Add(rec);
            
            rec.AllowSubmodelSemanticId.Add(idtaDef.SM_AssetInterfaceDescription.GetSemanticKey());

            return opt;
        }
    }

}
