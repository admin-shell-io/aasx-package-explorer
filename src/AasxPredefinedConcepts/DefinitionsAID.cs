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
        
        public Reference AID_Submodel = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Submodel") });
        public Reference AID_Interface = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface") });
        public Reference AID_title = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#title") });
        public Reference AID_description = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.dublincore.org/specifications/dublin-core/dcmi-terms/#description") });
        public Reference AID_created = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#created") });
        public Reference AID_modified = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#modified") });
        public Reference AID_support = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#support") });
        public Reference AID_EndpointMetadata = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/EndpointMetadata") });
        public Reference AID_base = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#base") });
        public Reference AID_contentType = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/hypermedia#forContentType") });
        public Reference AID_security = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#hasSecurityConfiguration") });
        public Reference AID_securityDefinitions = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#definesSecurityScheme") });
        public Reference AID_nosec_sc = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#NoSecurityScheme") });
        public Reference AID_scheme = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#definesSecurityScheme") });
        public Reference AID_auto_sc = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#AutoSecurityScheme") });
        public Reference AID_proxy = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#proxy") });
        public Reference AID_basic_sc = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#BasicSecurityScheme") });
        public Reference AID_name = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#name") });
        public Reference AID_in = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#in") });
        public Reference AID_combo_sc = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#ComboSecurityScheme") });
        public Reference AID_oneOf = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/json-schema#oneOf") });
        public Reference AID_allOf = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/json-schema#allOf") });
        public Reference AID_apikey_sc = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#APIKeySecurityScheme") });
        public Reference AID_psk_sc = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#PSKSecurityScheme") });
        public Reference AID_identity = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#identity") });
        public Reference AID_digest_sc = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#DigestSecurityScheme") });
        public Reference AID_qop = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#qop") });
        public Reference AID_bearer_sc = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#BearerSecurityScheme") });
        public Reference AID_authorization = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#authorization") });
        public Reference AID_alg = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#alg") });
        public Reference AID_format = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#format") });
        public Reference AID_oauth2_sc = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#OAuth2SecurityScheme") });
        public Reference AID_token = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#token") });
        public Reference AID_refresh = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#refresh") });
        public Reference AID_scopes = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#scopes") });
        public Reference AID_flow = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/security#flow") });
        public Reference AID_InterfaceMetadata = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/InterfaceMetadata") });
        public Reference AID_properties = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#PropertyAffordance") });
        public Reference AID_propertyName = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfaceDescription/1/0/PropertyDefinition") });
        public Reference AID_key = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/key") });
        public Reference AID_type = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/1999/02/22-rdf-syntax-ns#type") });
        public Reference AID_observable = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#isObservable") });
        public Reference AID_const = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/json-schema#const") });
        public Reference AID_default = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/json-schema#default") });
        public Reference AID_unit = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://schema.org/unitCode") });
        public Reference AID_min_max = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/minMaxRange") });
        public Reference AID_lengthRange = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/lengthRange") });
        public Reference AID_items = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/json-schema#items") });
        public Reference AID_valueSemantics = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/valueSemantics") });
        public Reference AID_itemsRange = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/itemsRange") });
        
        public Reference AID_forms = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#hasForm") });
        public Reference AID_httpforms = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#hasHTTPForm") });
        public Reference AID_mqttforms = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#hasMQTTForm") });
        public Reference AID_modbusforms = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#hasMODBUSForm") });

        public Reference AID_href = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/hypermedia#hasTarget") });
        public Reference AID_htv_methodName = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2011/http#methodName") });
        public Reference AID_htv_headers = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2011/http#headers") });
        public Reference AID_htv_fieldName = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2011/http#fieldName") });
        public Reference AID_htv_fieldValue = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2011/http#fieldValue") });

        public Reference AID_actions = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#ActionAffordance") });
        public Reference AID_events = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/td#EventAffordance") });
        public Reference AID_ExternalDescriptor = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/ExternalDescriptor") });
        public Reference AID_fileName = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/externalDescriptorName") });
        public Reference AID_InterfaceMODBUS = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface") });
        public Reference AID_modbus_function = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/modbus#Function") });
        public Reference AID_modbus_entity = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/modbus#Entity") });
        public Reference AID_modbus_zeroBasedAddressing = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/modbus#hasZeroBasedAddressingFlag") });
        public Reference AID_modbus_pollingTime = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/modbus#pollingTime") });
        public Reference AID_modbus_timeout = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/mqtt#hasQoSFlag") });
        public Reference AID_modbus_type = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/modbus#type") });
        public Reference AID_InterfaceMQTT = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface") });
        public Reference AID_mqv_retain = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/mqtt#hasRetainFlag") });
        public Reference AID_mqv_controlPacket = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/mqtt#ControlPacket") });
        public Reference AID_mqv_qos = new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/mqtt#hasQoSFlag") });
        public Reference AID_subprotocol = new Reference(ReferenceTypes.ExternalReference,new List<IKey> { new Key(KeyTypes.GlobalReference, "https://www.w3.org/2019/wot/hypermedia#forSubProtocol") });


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
