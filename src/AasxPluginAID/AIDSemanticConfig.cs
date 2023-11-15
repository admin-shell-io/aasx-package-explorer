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
using AasxPluginAID;
using Microsoft.VisualBasic;
using AnyUi;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using AasCore.Aas3_0;

namespace AasxPluginAID
{
    /// <summary>
    /// Starting from Dec 2021, move these information into a separate class.
    /// These were for V1.0 only; no case is known, that these were redefined.
    /// Therefore, make then more static
    /// </summary>
    public class AIDSemanticConfig

    {
        public Aas.Key SemIdReferencedObject = null;
        public static AasxPredefinedConcepts.IDTAAid idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
        public FormDescSubmodelElementCollection FormVdi2770 = null;

        public static FormDescSubmodelElementCollection AddCommonSecurityProperty(FormDescSubmodelElementCollection secDef)
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;

            secDef.Add(new FormDescProperty(
                "proxy", FormMultiplicity.One, idtaDef.AID_proxy.GetAsExactlyOneKey(),
                "proxy",
                "URI of the proxy server this security configuration provides access to. If not given, " +
                "the corresponding security configuration is for the endpoint."
                ));

            secDef.Add(new FormDescProperty(
                "scheme", FormMultiplicity.One, idtaDef.AID_scheme.GetAsExactlyOneKey(),
                "scheme",
                "Identification of the security mechanism being configured."
                ));

            return secDef;
        }

        public static FormDescSubmodelElementCollection AddNoSecScheme(string[] elements = null)
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
            var nosecDefinition = new FormDescSubmodelElementCollection(
                "nosec", FormMultiplicity.ZeroToMany, idtaDef.AID_nosec_sc.GetAsExactlyOneKey(),
                "securityDefinition{0:00}",
                "");
            return AddCommonSecurityProperty(nosecDefinition);
        }
        public static FormDescSubmodelElementCollection AddBearerScheme()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;

            var bearerDefinition = new FormDescSubmodelElementCollection(
                "bearer", FormMultiplicity.ZeroToMany, idtaDef.AID_bearer_sc.GetAsExactlyOneKey(),
                "securityDefinition{0:00}",
                "");

            bearerDefinition.Add(new FormDescProperty(
                "authorization", FormMultiplicity.One, idtaDef.AID_authorization.GetAsExactlyOneKey(),
                "authorization",
                "URI of the authorization server."));

            bearerDefinition.Add(new FormDescProperty(
                "name", FormMultiplicity.One, idtaDef.AID_name.GetAsExactlyOneKey(),
                "name",
                "Name for query, header, cookie, or uri parameters."));

            bearerDefinition.Add(new FormDescProperty(
                "alg", FormMultiplicity.One, idtaDef.AID_alg.GetAsExactlyOneKey(),
                "alg",
                "Encoding, encryption, or digest algorithm."));

            bearerDefinition.Add(new FormDescProperty(
                "format", FormMultiplicity.One, idtaDef.AID_format.GetAsExactlyOneKey(),
                "format",
                "Specifies format of security authentication information."));

            bearerDefinition.Add(new FormDescProperty(
                "in", FormMultiplicity.One, idtaDef.AID_in.GetAsExactlyOneKey(),
                "in",
                "Specifies the location of security authentication information."));

            return AddCommonSecurityProperty(bearerDefinition);
        }
        public static FormDescSubmodelElementCollection AddBasicScheme()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
            var basicDefinition = new FormDescSubmodelElementCollection(
                "basic", FormMultiplicity.ZeroToMany, idtaDef.AID_basic_sc.GetAsExactlyOneKey(),
                "securityDefinition{0:00}",
                "");

            basicDefinition.Add(new FormDescProperty(
                "name", FormMultiplicity.One, idtaDef.AID_name.GetAsExactlyOneKey(),
                "name",
                "Name for query, header, cookie, or uri parameters."));

            basicDefinition.Add(new FormDescProperty(
                "in", FormMultiplicity.One, idtaDef.AID_in.GetAsExactlyOneKey(),
                "in",
                "Specifies the location of security authentication information."));

            return AddCommonSecurityProperty(basicDefinition);

        }
        public static FormDescSubmodelElementCollection AddDigestScheme()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
            var digestDefinition = new FormDescSubmodelElementCollection(
                "digest", FormMultiplicity.ZeroToMany, idtaDef.AID_digest_sc.GetAsExactlyOneKey(),
                "securityDefinition{0:00}",
                "");

            digestDefinition.Add(new FormDescProperty(
                "name", FormMultiplicity.One, idtaDef.AID_name.GetAsExactlyOneKey(),
                "name",
                "Name for query, header, cookie, or uri parameters."));

            digestDefinition.Add(new FormDescProperty(
                "in", FormMultiplicity.One, idtaDef.AID_in.GetAsExactlyOneKey(),
                "in",
                "Specifies the location of security authentication information."));

            digestDefinition.Add(new FormDescProperty(
                "qop", FormMultiplicity.One, idtaDef.AID_qop.GetAsExactlyOneKey(),
                "qop",
                "Quality of protection."));

            return AddCommonSecurityProperty(digestDefinition);

        }
        public static FormDescSubmodelElementCollection AddPskScheme()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
            var pskDefinition = new FormDescSubmodelElementCollection(
                "psk", FormMultiplicity.ZeroToMany, idtaDef.AID_psk_sc.GetAsExactlyOneKey(),
                "securityDefinition{0:00}",
                "");

            pskDefinition.Add(new FormDescProperty(
                "identity", FormMultiplicity.One, idtaDef.AID_qop.GetAsExactlyOneKey(),
                "identity",
                "Identifier providing information which can be used for selection or confirmation"));

            return AddCommonSecurityProperty(pskDefinition);
        }
        public static FormDescSubmodelElementCollection AddOauth2Scheme()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;

            var oauth2Definition = new FormDescSubmodelElementCollection(
                "oauth2", FormMultiplicity.ZeroToMany, idtaDef.AID_oauth2_sc.GetAsExactlyOneKey(),
                "securityDefinition{0:00}",
                "");

            oauth2Definition.Add(new FormDescProperty(
                "authorization", FormMultiplicity.One, idtaDef.AID_authorization.GetAsExactlyOneKey(),
                "authorization",
                "URI of the authorization server."));

            oauth2Definition.Add(new FormDescProperty(
                "token", FormMultiplicity.One, idtaDef.AID_token.GetAsExactlyOneKey(),
                "token",
                "URI of the token server."));

            oauth2Definition.Add(new FormDescProperty(
                "refresh", FormMultiplicity.One, idtaDef.AID_alg.GetAsExactlyOneKey(),
                "refresh",
                "URI of the refresh server."));

            oauth2Definition.Add(new FormDescProperty(
                "scopes", FormMultiplicity.One, idtaDef.AID_format.GetAsExactlyOneKey(),
                "scopes",
                "Set of authorization scope identifiers provided as an array. " +
                "These are provided in tokens returned by an authorization server and " +
                "associated with forms in order to identify what resources a client may " +
                "access and how. The values associated with a form SHOULD be chosen from " +
                "those defined in an OAuth2SecurityScheme active on that form."));

            oauth2Definition.Add(new FormDescProperty(
                "flow", FormMultiplicity.One, idtaDef.AID_alg.GetAsExactlyOneKey(),
                "flow",
                "Authorization flow."));

            return AddCommonSecurityProperty(oauth2Definition);
        }
        public static FormDescSubmodelElementCollection AddApiKeyScheme()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
            var apikeyDefinition = new FormDescSubmodelElementCollection(
                "apikey", FormMultiplicity.ZeroToMany, idtaDef.AID_apikey_sc.GetAsExactlyOneKey(),
                "securityDefinition{0:00}",
                "");

            apikeyDefinition.Add(new FormDescProperty(
                "name", FormMultiplicity.One, idtaDef.AID_name.GetAsExactlyOneKey(),
                "name",
                "Name for query, header, cookie, or uri parameters."));

            apikeyDefinition.Add(new FormDescProperty(
                "in", FormMultiplicity.One, idtaDef.AID_in.GetAsExactlyOneKey(),
                "in",
                "Specifies the location of security authentication information."));

            return AddCommonSecurityProperty(apikeyDefinition);

        }
        public static FormDescSubmodelElementCollection AddautoScheme()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
            var autoDefinition = new FormDescSubmodelElementCollection(
                "auto", FormMultiplicity.ZeroToMany, null,
                "securityDefinition{0:00}",
                "");

            return AddCommonSecurityProperty(autoDefinition);
        }
        public static FormDescSubmodelElementCollection CreateSecurityDefinitions()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;

            var securityDefinitions = new FormDescSubmodelElementCollection(
                "securityDefinitions", FormMultiplicity.ZeroToOne, idtaDef.AID_securityDefinitions.GetAsExactlyOneKey(),
                "securityDefinitions",
                "");
            securityDefinitions.Add(AddNoSecScheme());
            securityDefinitions.Add(AddBearerScheme());
            securityDefinitions.Add(AddBasicScheme());
            securityDefinitions.Add(AddDigestScheme());
            securityDefinitions.Add(AddPskScheme());
            securityDefinitions.Add(AddOauth2Scheme());
            securityDefinitions.Add(AddApiKeyScheme());
            securityDefinitions.Add(AddautoScheme());

            return securityDefinitions;
        }
        public static FormDescSubmodelElementCollection CreateFormCommomElements(string title, string presetIdShort, string formInfo)
        {
            var form = new FormDescSubmodelElementCollection(
                            title, FormMultiplicity.ZeroToOne, null,
                            presetIdShort,formInfo);

            return form;
        }
        public static FormDescSubmodelElementCollection CreateHTTPForm()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
            FormDescSubmodelElementCollection form = CreateFormCommomElements("HTTP form","forms","HTTP form");

            form.Add(new FormDescProperty(
                "htv:methodName", FormMultiplicity.One, idtaDef.AID_htv_methodName.GetAsExactlyOneKey(),
                "htv:methodName",
                ""));

            var htvHeaders = new FormDescSubmodelElementCollection(
                "htv:headers", FormMultiplicity.One, idtaDef.AID_htv_headers.GetAsExactlyOneKey(),
                "htv:headers",
                "");

            htvHeaders.Add(new FormDescProperty(
                "htv:fieldName", FormMultiplicity.One, idtaDef.AID_htv_fieldName.GetAsExactlyOneKey(),
                "htv:fieldName",
                ""));

            htvHeaders.Add(new FormDescProperty(
                "htv:fieldValue", FormMultiplicity.One, idtaDef.AID_htv_fieldValue.GetAsExactlyOneKey(),
                "htv:fieldValue",
                ""));

            form.Add(htvHeaders);
            form.KeySemanticId = idtaDef.AID_httpforms.GetAsExactlyOneKey();

            return form;
        }
        public static FormDescSubmodelElementCollection CreateModbusForm()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
            FormDescSubmodelElementCollection form = CreateFormCommomElements("Modbus form", "forms", "Modbus form");


            form.Add(new FormDescProperty(
                "modbus:function", FormMultiplicity.One, idtaDef.AID_modbus_function.GetAsExactlyOneKey(),
                "modbus:function",
                ""));

            form.Add(new FormDescProperty(
                "modbus:entity", FormMultiplicity.One, idtaDef.AID_modbus_entity.GetAsExactlyOneKey(),
                "modbus:entity",
                ""));

            form.Add(new FormDescProperty(
                "modbus:zeroBasedAddressing", FormMultiplicity.One, idtaDef.AID_modbus_zeroBasedAddressing.GetAsExactlyOneKey(),
                "modbus:zeroBasedAddressing",
                ""));

            form.Add(new FormDescProperty(
                "modbus:timeout", FormMultiplicity.One, idtaDef.AID_modbus_timeout.GetAsExactlyOneKey(),
                "modbus:timeout",
                ""));

            form.Add(new FormDescProperty(
                "modbus:pollingTime", FormMultiplicity.One, idtaDef.AID_modbus_pollingTime.GetAsExactlyOneKey(),
                "modbus:pollingTime",
                ""));
            form.KeySemanticId = idtaDef.AID_modbusforms.GetAsExactlyOneKey();
            return form;
        }
        public static FormDescSubmodelElementCollection CreateMQTTForm()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
            FormDescSubmodelElementCollection form = CreateFormCommomElements("MQTT form", "forms", "MQTT form");


            form.Add(new FormDescProperty(
                "mqv:retain", FormMultiplicity.One, idtaDef.AID_mqv_retain.GetAsExactlyOneKey(),
                "mqv:retain",
                ""));

            form.Add(new FormDescProperty(
                "mqv:controlPacket", FormMultiplicity.One, idtaDef.AID_mqv_controlPacket.GetAsExactlyOneKey(),
                "mqv:controlPacket",
                ""));

            form.Add(new FormDescProperty(
                "mqv:qos", FormMultiplicity.One, idtaDef.AID_mqv_qos.GetAsExactlyOneKey(),
                "mqv: qos",
                ""));
            form.KeySemanticId = idtaDef.AID_mqttforms.GetAsExactlyOneKey();
            return form;
        }
        public static FormDescSubmodelElementCollection CreateProperty()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;

            var property = new FormDescSubmodelElementCollection(
                "property", FormMultiplicity.ZeroToMany, idtaDef.AID_propertyName.GetAsExactlyOneKey(),
                "property{0:00}",
                "");

            property.Add(new FormDescProperty(
                "title", FormMultiplicity.One, idtaDef.AID_title.GetAsExactlyOneKey(),
                "title",
                "Provides a human-readable title " +
                "(e.g., display a text for UI representation) based on a default language."));


            property.Add(new FormDescProperty(
                "observable", FormMultiplicity.One, idtaDef.AID_observable.GetAsExactlyOneKey(),
                "observable",
                "A hint that indicates whether Servients hosting the Thing and Intermediaries " +
                "should provide a Protocol Binding that supports the observeproperty and unobserveproperty operations for this Property."));

            property.Add(new FormDescProperty(
                "description", FormMultiplicity.One, idtaDef.AID_description.GetAsExactlyOneKey(),
                "description",
                "Provides additional (human-readable) information based on a default language."));

            property.Add(new FormDescProperty(
                "type", FormMultiplicity.One, idtaDef.AID_type.GetAsExactlyOneKey(),
                "type",
                "Assignment of JSON-based data types compatible with JSON Schema" +
                " (one of boolean, integer, number, string, object, array, or null)."));

            property.Add(new FormDescProperty(
                "min_max", FormMultiplicity.One, idtaDef.AID_min_max.GetAsExactlyOneKey(),
                "min_max",
                ""));

            property.Add(new FormDescProperty(
                "itemsRange", FormMultiplicity.One, idtaDef.AID_itemsRange.GetAsExactlyOneKey(),
                "itemsRange",
                ""));
            
            property.Add(new FormDescProperty(
                "lengthRange", FormMultiplicity.One, idtaDef.AID_lengthRange.GetAsExactlyOneKey(),
                "lengthRange",
                ""));

            property.Add(new FormDescProperty(
                "contentMediaType", FormMultiplicity.One, idtaDef.AID_contentType.GetAsExactlyOneKey(),
                "contentMediaType",
                "Specifies the MIME type of the contents of a string value," +
                " as described in [RFC2046]."));

            property.Add(new FormDescProperty(
                "const", FormMultiplicity.One, idtaDef.AID_const.GetAsExactlyOneKey(),
                "const",
                "Provides a constant value."));

            property.Add(new FormDescProperty(
                "default", FormMultiplicity.One, idtaDef.AID_default.GetAsExactlyOneKey(),
                "default",
                "Supply a default value. The value SHOULD validate against the data schema" +
                " in which it resides."));

            property.Add(new FormDescProperty(
                "unit", FormMultiplicity.One, null,
                "unit",
                "Provides unit information that is used, e.g., in international science," +
                " engineering, and business. To preserve uniqueness, it is recommended that the value of " +
                "the unit points to a semantic definition"));

            property.Add(CreateHTTPForm());
            property.Add(CreateMQTTForm());
            property.Add(CreateModbusForm());

            return property;
        }


        public static FormDescSubmodelElementCollection CreateAssetInterfaceDescription()
        {
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
            // DocumentItem

            var interfaceDescription = new FormDescSubmodelElementCollection(
                "Interface", FormMultiplicity.ZeroToMany,idtaDef.AID_Interface.GetAsExactlyOneKey(),
                "Interface{0:00}",
                "An abstraction of a physical or a virtual entity whose metadata and interfaces are described by a WoT Thing Description," +
                "whereas a virtual entity is the composition of one or more Things.");


            interfaceDescription.Add(new FormDescProperty(
                "title", FormMultiplicity.One, idtaDef.AID_title.GetAsExactlyOneKey(),
                "title",
                "Provides a human-readable title"));

            interfaceDescription.Add(new FormDescProperty(
                "description", FormMultiplicity.One, idtaDef.AID_description.GetAsExactlyOneKey(),
                "description",
                "Provides additional (human-readable) information based on a default language."));

            interfaceDescription.Add(new FormDescProperty(
                "created", FormMultiplicity.One, idtaDef.AID_created.GetAsExactlyOneKey(),
                "created",
                "Provides information when the TD instance was created."));

            interfaceDescription.Add(new FormDescProperty(
                "modified", FormMultiplicity.One, idtaDef.AID_modified.GetAsExactlyOneKey(),
                "modified",
                "Provides information when the TD instance was last modified."));

            interfaceDescription.Add(new FormDescProperty(
                "support", FormMultiplicity.One, idtaDef.AID_support.GetAsExactlyOneKey(),
                "support",
                "Provides information about the TD maintainer as URI scheme (e.g., mailto [RFC6068], tel [RFC3966], https [RFC9112])."));

            var endPointMetaData = new FormDescSubmodelElementCollection(
                "EndpointMetadata", FormMultiplicity.One, idtaDef.AID_EndpointMetadata.GetAsExactlyOneKey(),
                "EndpointMetadata",
                "");

            endPointMetaData.Add(new FormDescProperty(
                "base", FormMultiplicity.One, idtaDef.AID_base.GetAsExactlyOneKey(),
                "base",
                "Define the base URI that is used for all relative URI references throughout a TD document. " +
                "In TD instances, all relative URIs are resolved relative to the base URI using the algorithm defined in [RFC3986]."));

            endPointMetaData.Add(new FormDescProperty(
                "contentType", FormMultiplicity.One, idtaDef.AID_contentType.GetAsExactlyOneKey(),
                "contentType",
                "Assign a content type based on a media type (e.g., text/plain) " +
                "and potential parameters (e.g., charset=utf-8) for the media type [RFC2046]."));

            endPointMetaData.Add(CreateSecurityDefinitions());

            interfaceDescription.Add(endPointMetaData);

            var interfaceMetaData = new FormDescSubmodelElementCollection(
                "InterfaceMetaData", FormMultiplicity.One, idtaDef.AID_InterfaceMetadata.GetAsExactlyOneKey(),
                "InterfaceMetaData",
                "");

            var properties = new FormDescSubmodelElementCollection(
                "properties", FormMultiplicity.ZeroToOne, idtaDef.AID_properties.GetAsExactlyOneKey(),
                "properties",
                "");

            var externalDescriptor = new FormDescSubmodelElementCollection(
                "ExternalDescriptor", FormMultiplicity.ZeroToOne, idtaDef.AID_ExternalDescriptor.GetAsExactlyOneKey(),
                "ExternalDescriptor",
                "");

            externalDescriptor.Add(new FormDescFile(
                "descriptorName", FormMultiplicity.One, idtaDef.AID_fileName.GetAsExactlyOneKey(),
                "descriptorName"));

            properties.Add(CreateProperty());

            interfaceMetaData.Add(properties);

            interfaceDescription.Add(interfaceMetaData);
            interfaceDescription.Add(externalDescriptor);

            return interfaceDescription;
        }

        public static InterfaceEntity ParseSCInterfaceDescription(Aas.SubmodelElementCollection smcDoc,
                                                                  string referableHash)
        {
            var defs1 = AasxPredefinedConcepts.IDTAAid.Static;

            var title =
                "" +
                   smcDoc.Value.FindFirstSemanticIdAs<Aas.Property>(defs1.AID_title,
                MatchMode.Relaxed)?.Value;

            var Created =
                "" +
                smcDoc.Value.FindFirstSemanticIdAs<Aas.Property>(
                    defs1.AID_created, MatchMode.Relaxed)?
                    .Value;

            var ContentType =
                "" +
                smcDoc.Value.FindFirstSemanticIdAs<Aas.Property>(
                    defs1.AID_contentType, MatchMode.Relaxed)?
                    .Value;

            var ProtocolType = "HTTP";
            var ent = new InterfaceEntity(title, Created, ContentType, ProtocolType);

            // add
            ent.SourceElementsDocument = smcDoc.Value;
            ent.ReferableHash = referableHash;

            return ent;
        }

    }
}