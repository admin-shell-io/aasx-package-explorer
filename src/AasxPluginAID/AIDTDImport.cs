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
using AasxIntegrationBase.AasForms;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Web.Services.Description;
using System.Net;

namespace AasxPluginAID
{
    class AIDTDImport
    {
        public static string submodelId = null;
        public static string interfaceId = null;
        public static string contentType = "";
        public static string fileName = "";
        public static AasxPredefinedConcepts.IDTAAid idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
        
        public static Property BuildAasProperty(string idShort, Reference semanticReference,
                                                    LangStringTextType description, string value, DataTypeDefXsd valueType)
        {
            Property _property = new Property(valueType, idShort: idShort,
                                                         semanticId: semanticReference, description: new List<ILangStringTextType> { description },
                                                         value: value);
            return _property;
        }
        public static File BuildAasFile(string idShort, Reference semanticReference, string contentType)
        {
            File descriptorName = new Aas.File(contentType: contentType, idShort: idShort,
                                                 semanticId: semanticReference);
            return descriptorName;
        }
        
        public static SubmodelElementCollection BuildSEC(string idShort, Reference semanticReference, LangStringTextType description)
        {
            SubmodelElementCollection sec = new SubmodelElementCollection(idShort: idShort,
                                                                semanticId: semanticReference,
                                                                description: new List<ILangStringTextType> { description });
            return sec;
        }
        public static ISubmodelElement BuildAasElement(JObject tdJObject, JObject tdSchemaObject)
        {
            string formText = tdSchemaObject["formtext"].ToString();
            ISubmodelElement submodelElement = null;
            Reference semanticReference = idtaDef.ConstructReference(tdSchemaObject["semanticReference"].ToString());
            FormMultiplicity multiplicity = idtaDef.GetFormMultiplicity(tdSchemaObject["multiplcity"].ToString());
            string presetIdShort = tdSchemaObject["presetIdShort"].ToString();
            string elemType = tdSchemaObject["AasElementType"].ToString();
            LangStringTextType description = new Aas.LangStringTextType("en", tdSchemaObject["description"].ToString());
            if (elemType == "Property")
            {
                DataTypeDefXsd valueType = idtaDef.GetValueType(tdSchemaObject["valueType"].ToString());
                submodelElement = BuildAasProperty(presetIdShort, semanticReference, description, tdJObject[formText].ToString(),
                                    valueType);
                return submodelElement;
            }
            else if (elemType == "SubmodelElementCollection")
            {
                submodelElement = new SubmodelElementCollection(idShort: presetIdShort,
                                               semanticId: semanticReference, description: new List<ILangStringTextType> { description });                
                if (formText == "scopes" || formText == "enum")
                {
                    if (!tdJObject.ContainsKey(formText))
                    {
                        return null;
                    }
                    JObject _cSchemaObject = JObject.FromObject(tdSchemaObject["childs"][0]);
                    int j = 0;
                    foreach (var cElem in tdJObject[formText])
                    {
                        submodelElement.Add(BuildAasProperty(formText + string.Format("{00:00}", j), semanticReference,
                            description, cElem.ToString(), DataTypeDefXsd.String));
                        j++;
                    }
                    return submodelElement;
                }
                else if (formText == "security")
                {
                    if (!tdJObject.ContainsKey(formText))
                    {
                        return null;
                    }
                    int j = 0;
                    foreach (var _security in tdJObject["security"])
                    {
                        List<Aas.IKey> _keys = new List<Aas.IKey>
                                    {
                                        idtaDef.ConstructKey(KeyTypes.Submodel,submodelId),
                                        idtaDef.ConstructKey(KeyTypes.SubmodelElementCollection,interfaceId),
                                        idtaDef.ConstructKey(KeyTypes.SubmodelElementCollection,"EndpointMetadata"),
                                        idtaDef.ConstructKey(KeyTypes.SubmodelElementCollection,"securityDefinitions"),
                                        idtaDef.ConstructKey(KeyTypes.SubmodelElementCollection,_security.ToString())
                                    };

                        Aas.ReferenceElement _secRef = new ReferenceElement(idShort: "security" + string.Format("{00:00}", j),
                                         semanticId: semanticReference, description: new List<ILangStringTextType> { new Aas.LangStringTextType("en", "Reference element to security scheme definition") },
                                         value: new Reference(ReferenceTypes.ModelReference, _keys));

                        submodelElement.Add(_secRef);
                        j++;
                    }
                    return submodelElement;
                }
                else if (formText == "properties")
                {
                    foreach (JProperty _property in tdJObject["properties"])
                    {
                        string _key = _property.Name;
                        JObject propertyJObject = new JObject();
                        propertyJObject["property"] = JToken.FromObject(_property.Value);
                        propertyJObject["property"]["key"] = _key;
                        JObject propertySchemaJObject = JObject.FromObject(tdSchemaObject["childs"][0]);
                        SubmodelElementCollection _pSEC =  BuildAasElement(propertyJObject, propertySchemaJObject) as
                                                            SubmodelElementCollection;
                        _pSEC.IdShort = _key;
                        submodelElement.Add(_pSEC);
                    }
                    return submodelElement;
                }
                else if (formText == "forms")
                {
                    if (!tdJObject.ContainsKey(formText))
                    {
                        return null;
                    }
                    int i = 1;
                    foreach (var _form in tdJObject["forms"])
                    {
                        JObject formJObject = JObject.FromObject(_form);
                        List<string> keys = formJObject.Properties().Select(p => p.Name).ToList();
                        if (keys.Contains("htv:methodName"))
                        {
                            JObject formSchemaJObject = JObject.FromObject(tdSchemaObject["childs"][0]);
                            string methodName = formJObject["htv:methodName"].ToString();
                            formJObject.Remove("htv:methodName");
                            formJObject["htv_methodName"] = methodName;
                            JObject httpFormJobject = new JObject();
                            httpFormJobject["HTTP Form"] = JToken.FromObject(formJObject);
                            ISubmodelElement fsmc = BuildAasElement(httpFormJobject, formSchemaJObject);
                            fsmc.IdShort = "form" + string.Format("{00:00}", i);
                            submodelElement.Add(fsmc);
                        }
                        else if (isMQTTForm(keys))
                        {
                            JObject mttFormJobject = new JObject();
                            mttFormJobject["MQTT Form"] = JToken.FromObject(formJObject);
                            JObject formSchemaJObject = JObject.FromObject(tdSchemaObject["childs"][1]);
                            ISubmodelElement fsmc = BuildAasElement(mttFormJobject, formSchemaJObject);
                            fsmc.IdShort = "form" + string.Format("{00:00}", i);
                            submodelElement.Add(fsmc);
                        }
                        else if (isModBusForm(keys))
                        {
                            JObject modbusFormJobject = new JObject();
                            modbusFormJobject["MODBUS Form"] = JToken.FromObject(formJObject);
                            JObject formSchemaJObject = JObject.FromObject(tdSchemaObject["childs"][2]);
                            ISubmodelElement fsmc = BuildAasElement(modbusFormJobject, formSchemaJObject);
                            fsmc.IdShort = "form" + string.Format("{00:00}", i);
                            submodelElement.Add(fsmc);
                        }
                        break;
                    }
                }
                else
                {
                    JObject jobject = JObject.FromObject(tdJObject[formText]);
                    foreach (var childSchemaElem in tdSchemaObject["childs"])
                    {
                        string cformText = childSchemaElem["formtext"].ToString();
                        string cElementType = childSchemaElem["AasElementType"].ToString();

                        if (cElementType == "Range")
                        {
                            string cpresetIdShort = childSchemaElem["presetIdShort"].ToString();
                            Reference csemanticReference = idtaDef.ConstructReference(childSchemaElem["semanticReference"].ToString());
                            LangStringTextType cdescription = new Aas.LangStringTextType("en", childSchemaElem["description"].ToString());

                            bool present = false;
                            Aas.Range _range = new Aas.Range(Aas.DataTypeDefXsd.String, idShort: cpresetIdShort,
                                                                 semanticId: csemanticReference, description: new List<ILangStringTextType> { cdescription });
                            if (cformText == "min_max")
                            {
                                if (jobject.ContainsKey("minimum"))
                                {
                                    present = true;
                                    _range.Min = jobject["minimum"].ToString();
                                }
                                if (jobject.ContainsKey("maximum"))
                                {
                                    present = true;
                                    _range.Max = jobject["maximum"].ToString();
                                }
                            }
                            else if (cformText == "itemsRange")
                            {
                                if (jobject.ContainsKey("minItems"))
                                {
                                    present = true;
                                    _range.Min = jobject["minItems"].ToString();
                                }
                                if (jobject.ContainsKey("maxItems"))
                                {
                                    present = true;
                                    _range.Max = jobject["maxItems"].ToString();
                                }
                            }
                            else if (cformText == "lengthRange")
                            {
                                if (jobject.ContainsKey("minLength"))
                                {
                                    present = true;
                                    _range.Min = jobject["minLength"].ToString();
                                }
                                if (jobject.ContainsKey("maxLength"))
                                {
                                    present = true;
                                    _range.Max = jobject["maxLength"].ToString();
                                }
                            }
                            if (present)
                            {
                                submodelElement.Add(_range);
                            }
                        }
                        else if (jobject.ContainsKey(cformText))
                        {
                            JObject childSchemaElemObject = JObject.FromObject(childSchemaElem);
                            submodelElement.Add(BuildAasElement(jobject, childSchemaElemObject));
                        }
                    }
                }
                return submodelElement;
            }
            else if (elemType == "File")
            {
                File dName =  BuildAasFile("fileName", idtaDef.ConstructReference("https://admin-shell.io/idta/AssetInterfacesDescription/1/0/externalDescriptorName"), contentType);
                dName.Value = fileName;
                return dName;
            }
            return submodelElement;

        }
        public static Boolean isMQTTForm(List<string> keysList)
        {
            foreach (string key in idtaDef.mqttFormElemList)
            {
                if (keysList.Contains(key))
                    return true;
            }
            return false;
        }
        public static Boolean isModBusForm(List<string> keysList)
        {
            foreach (string key in idtaDef.modvFormElemList)
            {
                if (keysList.Contains(key))
                    return true;
            }
            return false;
        }
        public static SubmodelElementCollection CreateAssetInterfaceDescriptionFromTd(
            JObject tdJObject, string filename, string _submodelId, int interfaceCount, string _contentType)
        {   
            interfaceCount++;
            submodelId = _submodelId;
            contentType = _contentType;
            interfaceId = "interface" + string.Format("{00:00}", interfaceCount);
            fileName = filename;
            JObject interfaceJObject = JObject.FromObject(idtaDef.EndpointMetadataJObject["interface"]);
            Aas.Reference semanticReference = idtaDef.ConstructReference(interfaceJObject["semanticReference"].ToString());
            LangStringTextType description = new Aas.LangStringTextType("en", interfaceJObject["description"].ToString());

            Aas.SubmodelElementCollection interfaceDescription = new SubmodelElementCollection(idShort: interfaceId,
                semanticId: semanticReference, description: new List<ILangStringTextType> { description });
            try
            {
                foreach (var childElem in interfaceJObject["childs"])
                {
                    JObject childSchemaObject = JObject.FromObject(childElem);
                    string formText = childSchemaObject["formtext"].ToString();
                    if (formText  == "EndpointMetadata" || formText == "InteractionMetadata" || 
                                formText == "ExternalDescriptor")
                    {
                        Reference smcsemanticReference = idtaDef.ConstructReference(childSchemaObject["semanticReference"].ToString());
                        LangStringTextType smcdescription = new Aas.LangStringTextType("en", childSchemaObject["description"].ToString());
                        SubmodelElementCollection smc = BuildSEC(formText, smcsemanticReference, smcdescription);
                        foreach (var childSchemaElem in childSchemaObject["childs"])
                        {
                            JObject childSchemaElemObject = JObject.FromObject(childSchemaElem);
                            string cElemformText = childSchemaElem["formtext"].ToString();
                            if (cElemformText != "descriptorName")
                            {
                                if (tdJObject.ContainsKey(cElemformText))
                                {
                                    smc.Add(BuildAasElement(tdJObject, childSchemaElemObject));
                                }
                            }
                            else
                            {
                                smc.Add(BuildAasElement(tdJObject, childSchemaElemObject));
                            }
                            
                            
                        }
                        interfaceDescription.Add(smc); 

                    }
                    else
                    {
                        Aas.ISubmodelElement tdElement = BuildAasElement(tdJObject, childSchemaObject);
                        if (tdElement != null)
                        {
                            interfaceDescription.Add(tdElement);
                        }
                    }               
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return interfaceDescription;

        }

    }
}
