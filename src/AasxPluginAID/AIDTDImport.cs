using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;
using Extensions;
using AasCore.Aas3_0;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Security.Policy;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace AasxPluginAID
{
    class AIDTDImport
    {
        public static AasxPredefinedConcepts.IDTAAid idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
        public static Aas.Property BuildAASProperty(string idShort, string value, string description = "", Aas.Reference _semanticId = null)
        {
            Aas.Property _property = new Aas.Property(valueType: Aas.DataTypeDefXsd.String, idShort: idShort, value: value);
            _property.IdShort = idShort;
            _property.Category = "PARAMETER";
            _property.AddDescription("en", description);
            _property.SemanticId = _semanticId;
           
            return _property;
        }
        public static Aas.Range BuildAASRange(string idShort, string description = "", Aas.Reference _semanticId = null)
        {
            Aas.Range _range = new Aas.Range(valueType: Aas.DataTypeDefXsd.String, idShort: idShort);
            _range.IdShort = idShort;
            _range.Category = "PARAMETER";
            _range.AddDescription("en", description);
            _range.SemanticId = _semanticId;

            return _range;
        }
        public static Aas.SubmodelElementCollection BuildCommonSecurityProperties(JObject secJObject, string _name)
        {
            Aas.SubmodelElementCollection securityScheme = new Aas.SubmodelElementCollection();
            securityScheme.IdShort = _name;
            if (secJObject.ContainsKey("proxy"))
            {
                securityScheme.Add(BuildAASProperty(idShort: "proxy", value: secJObject["proxy"].ToString(),
                        description: "", idtaDef.AID_proxy));
            }
            if (secJObject.ContainsKey("scheme"))
            {
                securityScheme.Add(BuildAASProperty(idShort: "scheme", value: secJObject["scheme"].ToString(),
                        description: "", idtaDef.AID_scheme));
            }

            return securityScheme;
        }
        public static Aas.SubmodelElementCollection BuildNosecScheme(JObject nosecJObject, string _name)
        {
            Aas.SubmodelElementCollection nosec = BuildCommonSecurityProperties(nosecJObject, _name);
            nosec.SemanticId = idtaDef.AID_nosec_sc;
            return nosec;
        }
        public static Aas.SubmodelElementCollection BuildBearerScheme(JObject bearerJObject, string _name)
        {
            Aas.SubmodelElementCollection bearersec = BuildCommonSecurityProperties(bearerJObject, _name);
            bearersec.SemanticId = idtaDef.AID_bearer_sc;
            if (bearerJObject.ContainsKey("name"))
            {
                bearersec.Add(BuildAASProperty(idShort: "name", value: bearerJObject["name"].ToString(),
                        description: "", idtaDef.AID_name));
            }
            if (bearerJObject.ContainsKey("in"))
            {
                bearersec.Add(BuildAASProperty(idShort: "in", value: bearerJObject["in"].ToString(),
                        description: "", idtaDef.AID_in));
            }
            if (bearerJObject.ContainsKey("qop"))
            {
                bearersec.Add(BuildAASProperty(idShort: "qop", value: bearerJObject["qop"].ToString(),
                        description: "", idtaDef.AID_qop));
            }
            if (bearerJObject.ContainsKey("authorization"))
            {
                bearersec.Add(BuildAASProperty(idShort: "authorization", value: bearerJObject["authorization"].ToString(),
                        description: "", idtaDef.AID_scheme));
            }
            if (bearerJObject.ContainsKey("alg"))
            {
                bearersec.Add(BuildAASProperty(idShort: "alg", value: bearerJObject["alg"].ToString(),
                        description: "", idtaDef.AID_alg));
            }
            if (bearerJObject.ContainsKey("format"))
            {
                bearersec.Add(BuildAASProperty(idShort: "format", value: bearerJObject["format"].ToString(),
                        description: "", idtaDef.AID_format));
            }
            return bearersec;
        }
        public static Aas.SubmodelElementCollection BuildBasicScheme(JObject basicJObject, string _name)
        {
            Aas.SubmodelElementCollection basicsec = BuildCommonSecurityProperties(basicJObject, _name);
            basicsec.SemanticId = idtaDef.AID_basic_sc;
            if (basicJObject.ContainsKey("name"))
            {
                basicsec.Add(BuildAASProperty(idShort: "name", value: basicJObject["name"].ToString(),
                        description: "", idtaDef.AID_name));
            }
            if (basicJObject.ContainsKey("in"))
            {
                basicsec.Add(BuildAASProperty(idShort: "in", value: basicJObject["in"].ToString(),
                        description: "", idtaDef.AID_in));
            }
            return basicsec;
        }
        public static Aas.SubmodelElementCollection BuildDigestScheme(JObject digestJObject, string _name)
        {
            Aas.SubmodelElementCollection digestsec = BuildCommonSecurityProperties(digestJObject, _name);
            digestsec.SemanticId = idtaDef.AID_digest_sc;
            if (digestJObject.ContainsKey("name"))
            {
                digestsec.Add(BuildAASProperty(idShort: "name", value: digestJObject["name"].ToString(),
                        description: "", idtaDef.AID_name));
            }
            if (digestJObject.ContainsKey("in"))
            {
                digestsec.Add(BuildAASProperty(idShort: "in", value: digestJObject["in"].ToString(),
                        description: "", idtaDef.AID_in));
            }
            if (digestJObject.ContainsKey("qop"))
            {
                digestsec.Add(BuildAASProperty(idShort: "qop", value: digestJObject["qop"].ToString(),
                        description: "", idtaDef.AID_qop));
            }
            return digestsec;
        }
        public static Aas.SubmodelElementCollection BuildPskScheme(JObject pskJObject, string _name)
        {
            Aas.SubmodelElementCollection psksec = BuildCommonSecurityProperties(pskJObject, _name);
            psksec.SemanticId = idtaDef.AID_psk_sc;
            if (pskJObject.ContainsKey("identity"))
            {
                psksec.Add(BuildAASProperty(idShort: "name", value: pskJObject["identity"].ToString(),
                        description: "", idtaDef.AID_name));
            }
            return psksec;
        }
        public static Aas.SubmodelElementCollection BuildOauth2Scheme(JObject oauth2JObject, string _name)
        {
            Aas.SubmodelElementCollection oauth2sec = BuildCommonSecurityProperties(oauth2JObject, _name);
            oauth2sec.SemanticId = idtaDef.AID_oauth2_sc;
            if (oauth2JObject.ContainsKey("authorization"))
            {
                oauth2sec.Add(BuildAASProperty(idShort: "authorization", value: oauth2JObject["authorization"].ToString(),
                        description: "", idtaDef.AID_authorization));
            }
            if (oauth2JObject.ContainsKey("token"))
            {
                oauth2sec.Add(BuildAASProperty(idShort: "token", value: oauth2JObject["token"].ToString(),
                        description: "", idtaDef.AID_token));
            }
            if (oauth2JObject.ContainsKey("refresh"))
            {
                oauth2sec.Add(BuildAASProperty(idShort: "refresh", value: oauth2JObject["refresh"].ToString(),
                        description: "", idtaDef.AID_refresh));
            }
            if (oauth2JObject.ContainsKey("scopes"))
            {
                oauth2sec.Add(BuildAASProperty(idShort: "scopes", value: oauth2JObject["scopes"].ToString(),
                        description: "", idtaDef.AID_scopes));
            }
            if (oauth2JObject.ContainsKey("alg"))
            {
                oauth2sec.Add(BuildAASProperty(idShort: "flow", value: oauth2JObject["flow"].ToString(),
                        description: "", idtaDef.AID_flow));
            }
            return oauth2sec;
        }
        public static Aas.SubmodelElementCollection BuildApiKeyScheme(JObject apiKeyJObject, string _name)
        {
            Aas.SubmodelElementCollection apikeysec = BuildCommonSecurityProperties(apiKeyJObject, _name);
            apikeysec.SemanticId = idtaDef.AID_digest_sc;
            if (apiKeyJObject.ContainsKey("name"))
            {
                apikeysec.Add(BuildAASProperty(idShort: "name", value: apiKeyJObject["name"].ToString(),
                        description: "", idtaDef.AID_name));
            }
            if (apiKeyJObject.ContainsKey("in"))
            {
                apikeysec.Add(BuildAASProperty(idShort: "in", value: apiKeyJObject["in"].ToString(),
                        description: "", idtaDef.AID_in));
            }
            return apikeysec;
        }
        public static Aas.SubmodelElementCollection BuildSecurityDefinitions(JObject sdJObject)
        {
            Aas.SubmodelElementCollection sds = new Aas.SubmodelElementCollection();
            sds.IdShort = "securityDefinitions";
            sds.SemanticId = idtaDef.AID_securityDefinitions;
            foreach (var temp in sdJObject["securityDefinitions"])
            {
                JProperty x1 = (JProperty)temp;
                JObject _jObject = JObject.Parse((x1.Value).ToString());
                string _name = x1.Name.ToString();
                string scheme = _jObject["scheme"].ToString();

                if (scheme == "nosec")
                {
                    Aas.SubmodelElementCollection nosec = BuildCommonSecurityProperties(_jObject, _name);
                    nosec.SemanticId = idtaDef.AID_nosec_sc;
                    sds.Add(nosec);
                }
                else if (scheme == "bearer")
                {
                    sds.Add(BuildBearerScheme(_jObject, _name));
                }
                else if (scheme == "basic")
                {
                    sds.Add(BuildBasicScheme(_jObject, _name));
                }
                else if (scheme == "digest")
                {
                    sds.Add(BuildDigestScheme(_jObject, _name));
                }
                else if (scheme == "psk")
                {
                    sds.Add(BuildPskScheme(_jObject, _name));
                }
                else if (scheme == "oauth2")
                {
                    sds.Add(BuildOauth2Scheme(_jObject, _name));
                }
                else if (scheme == "apikey")
                {
                    sds.Add(BuildApiKeyScheme(_jObject, _name));
                }
                else if (scheme == "auto")
                {
                    Aas.SubmodelElementCollection autosec = BuildCommonSecurityProperties(_jObject, _name);
                    autosec.SemanticId = idtaDef.AID_auto_sc;
                    sds.Add(autosec);
                }
            }
            return sds;
        }
        public static Aas.SubmodelElementCollection BuildEndPointMetaData(JObject tdJObject)
        {
            Aas.SubmodelElementCollection EndpointMetadata = new Aas.SubmodelElementCollection();
            EndpointMetadata.IdShort = "EndpointMetadata";
            EndpointMetadata.AddDescription("en", "");
            EndpointMetadata.SemanticId = idtaDef.AID_EndpointMetadata;
            if (tdJObject.ContainsKey("base"))
            {
                EndpointMetadata.Add(BuildAASProperty(idShort: "base", value: tdJObject["base"].ToString(),
                            description: "", idtaDef.AID_base));
            }
            if (tdJObject.ContainsKey("contentType"))
            {
                EndpointMetadata.Add(BuildAASProperty(idShort: "contentType", value: tdJObject["contentType"].ToString(),
                            description: "", idtaDef.AID_contentType));
            }
            if (tdJObject.ContainsKey("securityDefinitions"))
            {
                EndpointMetadata.Add(BuildSecurityDefinitions(tdJObject));
            }
            return EndpointMetadata;
        }
        public static Aas.SubmodelElementCollection BuildDataSchema(JObject dsJObject)
        {
            Aas.SubmodelElementCollection _dataschema = new Aas.SubmodelElementCollection();
            if (dsJObject.ContainsKey("const"))
            {
                _dataschema.Add(BuildAASProperty("const", dsJObject["const"].ToString(), "", idtaDef.AID_const));
            }
            if (dsJObject.ContainsKey("default"))
            {
                _dataschema.Add(BuildAASProperty("default", dsJObject["default"].ToString(), "", idtaDef.AID_default));
            }
            if (dsJObject.ContainsKey("unit"))
            {
                _dataschema.Add(BuildAASProperty("unit", dsJObject["unit"].ToString(), "", idtaDef.AID_unit));
            }
            if (dsJObject.ContainsKey("title"))
            {
                _dataschema.Add(BuildAASProperty("title", dsJObject["title"].ToString(), "", idtaDef.AID_title));
            }
            if (dsJObject.ContainsKey("description"))
            {
                _dataschema.Add(BuildAASProperty("description", dsJObject["description"].ToString(), "", idtaDef.AID_description));
            }
            if (dsJObject.ContainsKey("type"))
            {
                _dataschema.Add(BuildAASProperty("type", dsJObject["type"].ToString(), "", idtaDef.AID_type));
            }
            if (dsJObject.ContainsKey("minimum"))
            {
                Aas.Range min_max = BuildAASRange("min_max", "", idtaDef.AID_min_max);
                min_max.Min = dsJObject["minimum"].ToString();
                if (dsJObject.ContainsKey("maximum"))
                {
                    min_max.Max = dsJObject["maximum"].ToString();
                }
                _dataschema.Add(min_max);
            }
            if (dsJObject.ContainsKey("maximum") && !dsJObject.ContainsKey("minimum"))
            {
                Aas.Range min_max = BuildAASRange("min_max", "", idtaDef.AID_min_max);
                min_max.Max = dsJObject["maximum"].ToString();
                _dataschema.Add(min_max);
            }
            if (dsJObject.ContainsKey("minItems"))
            {
                Aas.Range itemsRange = BuildAASRange("itemsRange", "", idtaDef.AID_itemsRange);
                itemsRange.Min = dsJObject["minItems"].ToString();
                if (dsJObject.ContainsKey("maxItems"))
                {
                    itemsRange.Max = dsJObject["maxItems"].ToString();
                }
                _dataschema.Add(itemsRange);
            }
            if (dsJObject.ContainsKey("maxItems") && !dsJObject.ContainsKey("minItems"))
            {
                Aas.Range itemsRange = BuildAASRange("itemsRange", "", idtaDef.AID_itemsRange);
                itemsRange.Max = dsJObject["maxItems"].ToString();
                _dataschema.Add(itemsRange);
            }
            if (dsJObject.ContainsKey("minLength"))
            {
                Aas.Range itemsRange = BuildAASRange("itemsRange", "", idtaDef.AID_itemsRange);
                itemsRange.Min = dsJObject["minLength"].ToString();
                if (dsJObject.ContainsKey("maxLength"))
                {
                    itemsRange.Max = dsJObject["maxLength"].ToString();
                }
                _dataschema.Add(itemsRange);
            }
            if (dsJObject.ContainsKey("maxLength") && !dsJObject.ContainsKey("minLength"))
            {
                Aas.Range itemsRange = BuildAASRange("itemsRange", "", idtaDef.AID_itemsRange);
                itemsRange.Max = dsJObject["maxLength"].ToString();
                _dataschema.Add(itemsRange);
            }
            if (dsJObject.ContainsKey("contentMediaType"))
            {
                _dataschema.Add(BuildAASProperty("contentMediaType", dsJObject["contentMediaType"].ToString(), "", idtaDef.AID_contentType));
            }
            return _dataschema;
        }
        public static Aas.SubmodelElementCollection BuildHTTPBindings(JObject httpJObject, Aas.SubmodelElementCollection _form)
        {
            bool formfound = false;
            if (httpJObject.ContainsKey("htv:methodName"))
            {
                _form.Add(BuildAASProperty("htv:methodName", httpJObject["htv:methodName"].ToString(), "", idtaDef.AID_htv_methodName));
                formfound = true;
            }
            if (httpJObject.ContainsKey("htv:headers"))
            {
                Aas.SubmodelElementCollection httpHeaders = new Aas.SubmodelElementCollection();
                httpHeaders.IdShort = "htv:headers";
                httpHeaders.SemanticId = idtaDef.AID_htv_headers;
                foreach (var temp in httpJObject["htv:headers"])
                {
                    JProperty x1 = (JProperty)temp;
                    JObject _jObject = JObject.Parse((x1.Value).ToString());
                    
                    if (_jObject.ContainsKey("htv:fieldName"))
                    {
                        httpHeaders.Add(BuildAASProperty("htv_fieldName", httpJObject["htv:fieldName"].ToString(), "", idtaDef.AID_htv_fieldName));
                    }
                    if (_jObject.ContainsKey("htv:fieldValue"))
                    {
                        httpHeaders.Add(BuildAASProperty("htv_fieldValue", httpJObject["htv:fieldValue"].ToString(), "", idtaDef.AID_htv_fieldValue));
                    }
                }
                _form.Add(httpHeaders);
                formfound = true;
            }
            if(formfound)
            {
                _form.SemanticId = idtaDef.AID_httpforms;
            }
            return _form;
        }
        public static Aas.SubmodelElementCollection BuildMQTTBindings(JObject mqttJObject, Aas.SubmodelElementCollection _form)
        {
            bool formfound = false;
            if (mqttJObject.ContainsKey("mqv:retain"))
            {
                _form.Add(BuildAASProperty("mqv_retain", mqttJObject["mqv:retain"].ToString(), "", idtaDef.AID_mqv_retain));
                formfound = true;
            }
            if (mqttJObject.ContainsKey("mqv:controlPacket"))
            {
                _form.Add(BuildAASProperty("mqv_controlPacket", mqttJObject["mqv:controlPacket"].ToString(), "", idtaDef.AID_mqv_controlPacket));
                formfound = true;
            }
            if (mqttJObject.ContainsKey("mqv:qos"))
            {
                _form.Add(BuildAASProperty("mqv_qos", mqttJObject["mqv:qos"].ToString(), "", idtaDef.AID_mqv_qos));
                formfound = true;
            }
            if (formfound)
            {
                _form.SemanticId = idtaDef.AID_mqttforms;
            }
            return _form;
        }
        public static Aas.SubmodelElementCollection BuildModBusBindings(JObject mqttJObject, Aas.SubmodelElementCollection _form)
        {
            bool formfound = false;
            if (mqttJObject.ContainsKey("modbus:function"))
            {
                _form.Add(BuildAASProperty("modbus_function", mqttJObject["modbus:function"].ToString(), "", idtaDef.AID_modbus_function));
                formfound = true;
            }
            if (mqttJObject.ContainsKey("modbus:entity"))
            {
                _form.Add(BuildAASProperty("modbus_entity", mqttJObject["modbus:entity"].ToString(), "", idtaDef.AID_modbus_entity));
                formfound = true;
            }
            if (mqttJObject.ContainsKey("modbus:zeroBasedAddressing"))
            {
                _form.Add(BuildAASProperty("modbus_zeroBasedAddressing", mqttJObject["modbus:zeroBasedAddressing"].ToString(), "", idtaDef.AID_modbus_zeroBasedAddressing));
                formfound = true;
            }
            if (mqttJObject.ContainsKey("modbus:timeout"))
            {
                _form.Add(BuildAASProperty("modbus:timeout", mqttJObject["modbus:timeout"].ToString(), "", idtaDef.AID_modbus_timeout));
                formfound = true;
            }
            if (mqttJObject.ContainsKey("modbus_pollingTime"))
            {
                _form.Add(BuildAASProperty("modbus_pollingTime", mqttJObject["modbus:pollingTime"].ToString(), "", idtaDef.AID_modbus_pollingTime));
                formfound = true;
            }
            if (mqttJObject.ContainsKey("modbus:endian"))
            {
                _form.Add(BuildAASProperty("modbus_type", mqttJObject["modbus:type"].ToString(), "", idtaDef.AID_modbus_type));
                formfound = true;
            }
            if (formfound)
            {
                _form.SemanticId = idtaDef.AID_modbusforms;
            }
            return _form;
        }
        public static Aas.SubmodelElementCollection BuildForm(JObject formJObject)
        {
            Aas.SubmodelElementCollection _form = new Aas.SubmodelElementCollection();
            _form.IdShort = "forms";
            _form.SemanticId = idtaDef.AID_forms;

            if (formJObject.ContainsKey("href"))
            {
                _form.Add(BuildAASProperty("href", formJObject["href"].ToString(), "", idtaDef.AID_href));
            }
            if (formJObject.ContainsKey("contentType"))
            {
                _form.Add(BuildAASProperty("contentType", formJObject["contentType"].ToString(), "", idtaDef.AID_contentType));
            }
            if (formJObject.ContainsKey("subprotocol"))
            {
                _form.Add(BuildAASProperty("subprotocol", formJObject["subprotocol"].ToString(), "", idtaDef.AID_subprotocol));
            }
            _form = BuildHTTPBindings(formJObject, _form);
            _form = BuildMQTTBindings(formJObject, _form);
            _form = BuildModBusBindings(formJObject, _form);
            return _form;
        }
        public static Aas.SubmodelElementCollection BuildTDProperty(JObject pJObject, string name)
        {
            Aas.SubmodelElementCollection _property = BuildDataSchema(pJObject);
            _property.IdShort = name;
            _property.AddDescription("en", "");
            _property.SemanticId = idtaDef.AID_propertyName;

            if (pJObject.ContainsKey("observable"))
            {
                _property.Add(BuildAASProperty("observable", pJObject["observable"].ToString(),"" ,idtaDef.AID_observable));
            }
            if (pJObject.ContainsKey("forms"))
            {
                _property.Add(BuildForm((JObject)pJObject["forms"][0]));      
            }
            return _property;
        }
        public static Aas.SubmodelElementCollection BuildInterfaceMetaData(JObject tdJObject)
        {
            Aas.SubmodelElementCollection InterfaceMetaData = new Aas.SubmodelElementCollection();
            InterfaceMetaData.IdShort = "InterfaceMetaData";
            InterfaceMetaData.AddDescription("en", "");
            InterfaceMetaData.SemanticId = idtaDef.AID_InterfaceMetadata;

            if (tdJObject.ContainsKey("properties")) 
            {
                Aas.SubmodelElementCollection properties = new Aas.SubmodelElementCollection();
                properties.IdShort = "properties";
                properties.AddDescription("en", "");
                properties.SemanticId = idtaDef.AID_properties;
                int i = 0;
                foreach (var temp in tdJObject["properties"])
                {
                    try
                    {
                        JProperty x1 = (JProperty)temp;
                        JObject _jObject = JObject.Parse((x1.Value).ToString());
                        properties.Add(BuildTDProperty(_jObject, x1.Name.ToString()));
                        i++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }

                }
                InterfaceMetaData.Add(properties);
            }

            return InterfaceMetaData;
        }
        public static SubmodelElementCollection BuildExternalDescriptor(string filename)
        {
            Aas.SubmodelElementCollection externalDescriptor = new Aas.SubmodelElementCollection();
            externalDescriptor.IdShort = "ExternalDescriptor";
            externalDescriptor.AddDescription("en","");
            externalDescriptor .SemanticId = idtaDef.AID_ExternalDescriptor;

            Aas.File descriptorName = new Aas.File(contentType : "json", idShort : "fileName", 
                                                    semanticId: idtaDef.AID_fileName);
            descriptorName.Value = filename;
            
            externalDescriptor.Add(descriptorName);

            return externalDescriptor;
        }
        public static SubmodelElementCollection CreateAssetInterfaceDescriptionFromTd(
            JObject tdJObject, string filename)
        {                    
            // shortcut
            var idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
            // DocumentItem

            Aas.SubmodelElementCollection interfaceDescription = new Aas.SubmodelElementCollection();
            interfaceDescription.IdShort = "interface01";
            interfaceDescription.AddDescription("en", "An abstraction of a physical or a virtual entity whose metadata and interfaces are described by a WoT Thing Description," +
                "whereas a virtual entity is the composition of one or more Things.");
            interfaceDescription.SemanticId = idtaDef.AID_Interface;

            if (tdJObject.ContainsKey("title"))
            {
                interfaceDescription.Add(BuildAASProperty(idShort: "title", value: tdJObject["title"].ToString(),
                    description: "Provides a human-readable title", idtaDef.AID_title));
            }
            if (tdJObject.ContainsKey("description"))
            {
                interfaceDescription.Add(BuildAASProperty(idShort: "description", value: tdJObject["description"].ToString(),
                    description: "Provides additional (human-readable) information based on a default language.", idtaDef.AID_title));
            }
            if (tdJObject.ContainsKey("created"))
            {
                interfaceDescription.Add(BuildAASProperty(idShort: "created", value: tdJObject["created"].ToString(),
                    description: "Provides information when the TD instance was created.", idtaDef.AID_created));
            }
            if (tdJObject.ContainsKey("modified"))
            {
                interfaceDescription.Add(BuildAASProperty(idShort: "modified", value: tdJObject["modified"].ToString(),
                    description: "Provides information when the TD instance was last modified.", idtaDef.AID_modified));
            }
            if (tdJObject.ContainsKey("support"))
            {
                interfaceDescription.Add(BuildAASProperty(idShort: "support", value: tdJObject["support"].ToString(),
                    description: "Provides information about the TD maintainer as URI scheme", idtaDef.AID_support));
            }

            interfaceDescription.Add(BuildEndPointMetaData(tdJObject));
            interfaceDescription.Add(BuildInterfaceMetaData(tdJObject));
            interfaceDescription.Add(BuildExternalDescriptor(filename));

            interfaceDescription.SemanticId = idtaDef.AID_Interface;


            return interfaceDescription;

        }

    }
}
