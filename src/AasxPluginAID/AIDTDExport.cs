using AasCore.Aas3_0;
using Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Aas = AasCore.Aas3_0;

namespace AasxPluginAID
{
    class AIDTDExport
    {
        public static AasxPredefinedConcepts.IDTAAid idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
        public static JObject ReplaceformProperties(JObject form)
        {
            JObject temp = new JObject();
            foreach (var elem in form)
            {
                string name = (elem.Key).Replace("_", ":");
                if (name == "htv:headers")
                {
                    int i = 0;
                    List<JObject> headers = new List<JObject>();
                    foreach (var _header in JObject.FromObject(elem.Value))
                    {
                        JObject htvHeader = JObject.FromObject(_header.Value);
                        JObject headerElem = new JObject();
                        if (htvHeader.ContainsKey("htv_fieldName"))
                        {
                            headerElem["htv:fieldName"] = htvHeader["htv_fieldName"].ToString();
                        }
                        if (htvHeader.ContainsKey("htv_fieldName"))
                        {
                            headerElem["htv:fieldValue"] = htvHeader["htv_fieldValue"].ToString();
                        }
                        headers.Add(headerElem);
                        i = i + 1;
                    }
                    temp["htv:headers"] = JToken.FromObject(headers);
                }
                else
                {
                    string value = elem.Value.ToString();
                    temp[name] = value;
                }
            }
            return temp;
        }
        public static JToken TDelemDefinition(ISubmodelElement se, JObject tdSchemaObject, string addParams = null)
        {
            string semanticReference = se.SemanticId.GetAsExactlyOneKey().Value;

            foreach (var child in tdSchemaObject["childs"])
            {
                JObject childElemJObject = JObject.FromObject(child);
                if (childElemJObject["semanticReference"].ToString() == semanticReference)
                {
                    return child;
                }
            }
            return null;
        }
        public static JObject serialize_aid_elem(JToken tdJToken, Aas.ISubmodelElement sme)
        {
            JObject TDJson = new JObject();
            JObject tdSchemaObject = JObject.FromObject(tdJToken);
            string presetIdShort = tdSchemaObject["presetIdShort"].ToString();
            string elemType = tdSchemaObject["AasElementType"].ToString();
            string semanticReference = tdSchemaObject["semanticReference"].ToString();
            string multiplcity = tdSchemaObject["multiplcity"].ToString();

            if (elemType == "Property")
            {
                Aas.Property _property = sme as Aas.Property;
                TDJson[sme.IdShort] = _property.Value.ToString();
            }
            else if (elemType == "Range")
            {
                Aas.Range _range = sme as Aas.Range;
                if (_range.IdShort == "min_max")
                {
                    TDJson["minimum"] = _range.Min;
                    TDJson["maximum"] = _range.Max;
                }
                if (_range.IdShort == "lengthRange")
                {
                    TDJson["minLength"] = _range.Min;
                    TDJson["maxLength"] = _range.Max;
                }
                if (_range.IdShort == "itemsRange")
                {
                    TDJson["minItems"] = _range.Min;
                    TDJson["maxItems"] = _range.Max;
                }
            }
            else if (elemType == "SubmodelElementCollection")
            {
                Aas.ISubmodelElementCollection smc = sme as Aas.SubmodelElementCollection;
                if (presetIdShort == "scopes" || presetIdShort == "enum" || presetIdShort == "security")
                {
                    List<string> listElem = new List<string>();
                    foreach (var se in smc.Value)
                    {
                        if (presetIdShort == "security")
                        {
                            Aas.ReferenceElement referenceElem = se as Aas.ReferenceElement;
                            listElem.Add(referenceElem.Value.Keys.Last().Value.ToString());
                        }
                        else
                        {
                            Aas.Property _property = se as Aas.Property;
                            listElem.Add(_property.Value.ToString());
                        }
                    }
                    TDJson[presetIdShort] = JToken.FromObject(listElem);
                }
                else
                {
                    JObject smcJObject = new JObject();
                    foreach (var se in smc.Value)
                    {
                        JToken childSchemaDefinition = TDelemDefinition(se, tdSchemaObject);
                        JObject serializeJObject = serialize_aid_elem(childSchemaDefinition, se);
                        smcJObject.Merge(JToken.FromObject(serializeJObject));
                    }
                    if (semanticReference == "https://admin-shell.io/idta/AssetInterfaceDescription/1/0/PropertyDefinition"
                        || semanticReference == "https://www.w3.org/2019/wot/json-schema#propertyName")
                    {
                        string _key = smcJObject["key"].ToString();
                        smcJObject.Remove("key");
                        TDJson[_key] = JToken.FromObject(smcJObject);
                    }
                    else if(presetIdShort == "forms")
                    {
                        List<JObject> forms = new List<JObject>();
                        forms.Add(ReplaceformProperties(smcJObject));
                        TDJson[sme.IdShort] = JToken.FromObject(forms);
                    }
                    else
                    {
                        TDJson[sme.IdShort] = JToken.FromObject(smcJObject);
                    }
                }
            }

            return TDJson;
        }
        public static JObject ExportInterfacetoTDJson(Aas.ISubmodelElementCollection smc)
        {
            JObject TDJson = new JObject();
            TDJson["@context"] = "https://www.w3.org/2019/wot/td/v1";
            TDJson["@type"] = "Thing";
            try
            {
                foreach (var se in smc.Value)
                {
                    if (se.IdShort == "EndpointMetadata")
                    {
                        JToken endpointmetaData = serialize_aid_elem(idtaDef.EndpointMetadataJObject["interface"]["childs"][4], se)["EndpointMetadata"];
                        TDJson.Merge(JObject.FromObject(endpointmetaData));
                    }
                    else if (se.IdShort == "ExternalDescriptor")
                    {

                    }
                    else if (se.IdShort == "InteractionMetadata")
                    {
                        JToken InterfaceMetaData = serialize_aid_elem(idtaDef.EndpointMetadataJObject["interface"]["childs"][6], se)["InteractionMetadata"];
                        TDJson.Merge(JObject.FromObject(InterfaceMetaData));
                    }
                    else
                    {
                        Aas.Property _property = se as Aas.Property;
                        TDJson[se.IdShort] = _property.Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return TDJson;

        }
    }
}
