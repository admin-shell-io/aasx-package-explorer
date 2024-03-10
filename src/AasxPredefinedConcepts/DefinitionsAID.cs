/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Newtonsoft.Json.Linq;
using System.Reflection;
using AdminShellNS;
using AasCore.Aas3_0;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.VisualBasic;
using System.Security.Cryptography;
using AasxIntegrationBase.AasForms;
// ReSharper disable UnassignedField.Global
// (working by reflection)

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions of Submodel VDI2770 according to new alignment with VDI
    /// </summary>
    public class IDTAAid : AasxDefinitionBase
    {
        public static IDTAAid Static = new IDTAAid();
        public Submodel
            SM_AssetInterfaceDescription;

        public JObject EndpointMetadataJObject = AIDResources.EndpointMetadataJObject;
        public List<string> mqttFormElemList = new List<string>() { "mqv_retain",
                                              "mqv_controlPacket","mqv_qos"};
        public List<string> modvFormElemList = new List<string>() { "modv:function",
                                   "modv_entity","modv_zeroBasedAddressing","modv_pollingTime",
                                   "modv_type","modv_mostSignificantByte","modv_mostSignificantWord"
                                   };

        public Reference AID_Submodel = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Submodel") });
        public Reference AID_Interface = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface") });
        
        public Reference ConstructReference(string value)
        {
            Reference _ref = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, value) });
            return _ref;
        }
        public Key ConstructKey(KeyTypes _keyType, string  value) {

            Key _key = new Key(_keyType, value);
            return _key;
        }
        public FormMultiplicity GetFormMultiplicity(string multipli)
        {
            if (multipli == "1")
            {
                return  FormMultiplicity.One;
            }
            else if (multipli == "0..1")
            {
                return FormMultiplicity.ZeroToOne;
            }
            else if (multipli == "0..*")
            {
                return FormMultiplicity.ZeroToMany;
            }
            else if (multipli == "1..*")
            {
                return FormMultiplicity.OneToMany;
            }
            return FormMultiplicity.ZeroToOne;
        }
        public DataTypeDefXsd GetValueType(string valueType)
        {
            return Stringification.DataTypeDefXsdFromString(valueType)
                    ?? DataTypeDefXsd.String;
        }
        public IDTAAid()
        {
            // info
            this.DomainInfo = "IDTA Asset Interface Description";
                
            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "IdtaAssetInterfaceDescription.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(IDTAAid), useFieldNames: true);
        }
    }

}
