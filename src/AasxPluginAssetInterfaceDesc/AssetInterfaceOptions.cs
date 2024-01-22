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
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AasxIntegrationBase;

namespace AasxPluginAssetInterfaceDescription
{
    public class AssetInterfaceOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
        public bool IsDescription = false;
        public bool IsMapping = false;

        public bool UseHttp = true;
        public bool UseModbus = true;
        public bool UseMqtt = true;
        public bool UseOpcUa = true;
    }

    public class AssetInterfaceOptions : AasxPluginLookupOptionsBase
    {
        public List<AssetInterfaceOptionsRecord> Records = new List<AssetInterfaceOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static AssetInterfaceOptions CreateDefault()
        {
            var defs = new DefinitionsMTP.ModuleTypePackage();

            var rec1 = new AssetInterfaceOptionsRecord();
            rec1.IsDescription = true;
            rec1.AllowSubmodelSemanticId = new[] { 
                new Aas.Key(Aas.KeyTypes.Submodel, 
                "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Submodel") }.ToList();

            var rec2 = new AssetInterfaceOptionsRecord();
            rec2.IsMapping = true;
            rec1.AllowSubmodelSemanticId = new[] {
                new Aas.Key(Aas.KeyTypes.Submodel,
                "https://admin-shell.io/idta/AssetInterfacesMappingConfiguration/1/0/Submodel") }.ToList();

            var opt = new AssetInterfaceOptions();
            opt.Records.Add(rec1);

            return opt;
        }
    }
}
