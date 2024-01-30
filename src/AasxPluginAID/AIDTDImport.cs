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

namespace AasxPluginAID
{
    class AIDTDImport
    {
        public static string submodelId = null;
        public static string interfaceId = null;
        public static AasxPredefinedConcepts.IDTAAid idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
        public static Aas.ISubmodelElement BuildAasElement(JObject tdJObject, JObject tdSchemaObject)
        {
            Aas.ISubmodelElement submodelElement = null;   
            Aas.Reference semanticReference = idtaDef.ConstructReference(tdSchemaObject["semanticReference"].ToString());
            FormMultiplicity multiplicity = idtaDef.GetFormMultiplicity(tdSchemaObject["multiplcity"].ToString());
            string presetIdShort = tdSchemaObject["presetIdShort"].ToString();
            string elemType = tdSchemaObject["AasElementType"].ToString();
            string formText = tdSchemaObject["formtext"].ToString();
            LangStringTextType description = new Aas.LangStringTextType("en", tdSchemaObject["description"].ToString());
            
            if (elemType == "Property")
            {
                DataTypeDefXsd valueType = idtaDef.GetValueType( tdSchemaObject["valueType"].ToString());
                submodelElement = new Property(valueType, idShort:presetIdShort,
                                                     semanticId : semanticReference, description : new List<ILangStringTextType> { description },
                                                     value: tdJObject[formText].ToString());
                return submodelElement;
            }
            else if (elemType == "SubmodelElementCollection")
            {
                submodelElement = new SubmodelElementCollection(idShort: presetIdShort,
                    semanticId: semanticReference, description: new List<ILangStringTextType> { description });
                foreach (var childSchemaElem in tdSchemaObject["childs"])
                {
                    JObject childSchemaElemObject = JObject.FromObject(childSchemaElem);
                    string celemType = childSchemaElemObject["AasElementType"].ToString();
                    string cFormtext = childSchemaElemObject["formtext"].ToString();
                    string cpresetIdShort = childSchemaElemObject["presetIdShort"].ToString();
                    Aas.Reference csemanticReference = idtaDef.ConstructReference(childSchemaElemObject["semanticReference"].ToString());
                    List<ILangStringTextType> cdescription = new List<ILangStringTextType> { new Aas.LangStringTextType("en", childSchemaElemObject["description"].ToString()) };
                    if (tdJObject.ContainsKey(cFormtext))
                    {
                        if (celemType == "SubmodelElementCollection")
                        {
                            if (cFormtext == "scopes" || cFormtext == "enum")
                            {
                                Aas.SubmodelElementCollection _elems = new SubmodelElementCollection(idShort: cpresetIdShort,
                                                                                semanticId: csemanticReference,
                                                                                description: cdescription);
                                JObject _cSchemaObject = JObject.FromObject(childSchemaElemObject["childs"][0]);
                                int j = 0;
                                foreach (var _scope in tdJObject[cFormtext])
                                {
                                    _elems.Add(new Property(Aas.DataTypeDefXsd.String, idShort: _cSchemaObject["presetIdShort"].ToString(),
                                                     semanticId: csemanticReference, description: new List<ILangStringTextType> { new Aas.LangStringTextType("en", "Authorization scope identifier") },
                                                     value: _scope.ToString()));
                                    j++;
                                }
                                submodelElement.Add(_elems);
                            }
                            else if (cFormtext == "security")
                            {
                                Aas.SubmodelElementCollection security = new SubmodelElementCollection(idShort: cpresetIdShort,
                                                                                semanticId: csemanticReference,
                                                                                description: cdescription);
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
                                                     semanticId: csemanticReference, description: new List<ILangStringTextType> { new Aas.LangStringTextType("en", "Reference element to security scheme definition") },
                                                     value: new Reference(ReferenceTypes.ModelReference, _keys));

                                    security.Add(_secRef);
                                    j++;
                                }
                                submodelElement.Add(security);
                            }
                            else if (cFormtext == "properties")
                            {
                                Aas.SubmodelElementCollection propoerties = new SubmodelElementCollection(idShort: cpresetIdShort,
                                                                                semanticId: csemanticReference,
                                                                                description: cdescription);
                                foreach(JProperty _property in tdJObject["properties"])
                                {
                                    string _key = _property.Name;
                                    JObject propertyJObject = JObject.FromObject(_property.Value);
                                    JObject propertySchemaJObject = JObject.FromObject(childSchemaElemObject["childs"][0]);
                                    Aas.SubmodelElementCollection _propertySMC = BuildAasElement(propertyJObject, propertySchemaJObject) as Aas.SubmodelElementCollection;
                                    _propertySMC.Add(new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "key",
                                                     semanticId: semanticReference, description: new List<ILangStringTextType> { new Aas.LangStringTextType("en", "Optional element when the idShort of {property_name} cannot be used to reflect the desired property name due to the idShort restrictions ") },
                                                     value : _key));
                                    propoerties.Add(_propertySMC);
                                }
                                submodelElement.Add(propoerties);
                            }
                            else if (cFormtext == "forms")
                            {
                                Aas.SubmodelElementCollection forms = new SubmodelElementCollection(idShort: cpresetIdShort,
                                                                                semanticId: csemanticReference,
                                                                                description: cdescription);
                                foreach (var _form in tdJObject["forms"])
                                {
                                    JObject formJObject = JObject.FromObject(_form);
                                    List<string> keys = formJObject.Properties().Select(p => p.Name).ToList();
                                    if (keys.Contains("htv_methodName"))
                                    {
                                        JObject formSchemaJObject = JObject.FromObject(childSchemaElemObject["childs"][0]);
                                        forms.Add(BuildAasElement(formJObject, formSchemaJObject));
                                    }
                                    else if (isMQTTForm(keys))
                                    {
                                        JObject formSchemaJObject = JObject.FromObject(childSchemaElemObject["childs"][1]);
                                        forms.Add(BuildAasElement(formJObject, formSchemaJObject));
                                    }
                                    else if (isModBusForm(keys))
                                    {
                                        JObject formSchemaJObject = JObject.FromObject(childSchemaElemObject["childs"][2]);
                                        forms.Add(BuildAasElement(formJObject, formSchemaJObject));
                                    }
                                    break;
                                }
                                submodelElement.Add(forms);
                            }
                            else
                            {
                                JObject childTDJobject = JObject.FromObject(tdJObject[cFormtext]);
                                submodelElement.Add(BuildAasElement(childTDJobject, childSchemaElemObject));
                            }
                        }
                        else if (celemType == "Property")
                        {
                            submodelElement.Add(BuildAasElement(tdJObject, childSchemaElemObject));
                        }
                    }
                    else if (celemType == "Range")
                    {
                        DataTypeDefXsd valueType = idtaDef.GetValueType(childSchemaElemObject["valueType"].ToString());

                        Aas.Range _range = new Aas.Range(Aas.DataTypeDefXsd.String, idShort: cpresetIdShort,
                                                             semanticId: csemanticReference, description: cdescription);
                        if (cFormtext == "min_max")
                        {
                            if (tdJObject.ContainsKey("minimum"))
                            {
                                _range.Min = tdJObject["minimum"].ToString();
                            }
                            if (tdJObject.ContainsKey("maximum"))
                            {
                                _range.Max = tdJObject["maximum"].ToString();
                            }
                        }
                        else if (cFormtext == "itemsRange")
                        {
                            if (tdJObject.ContainsKey("minItems"))
                            {
                                _range.Min = tdJObject["minItems"].ToString();
                            }
                            if (tdJObject.ContainsKey("maxItems"))
                            {
                                _range.Max = tdJObject["maxItems"].ToString();
                            }
                        }
                        else if (cFormtext == "lengthRange")
                        {
                            if (tdJObject.ContainsKey("minLength"))
                            {
                                _range.Min = tdJObject["minLength"].ToString();
                            }
                            if (tdJObject.ContainsKey("maxLength"))
                            {
                                _range.Max = tdJObject["maxLength"].ToString();
                            }
                        }
                        submodelElement.Add(_range);
                    }
                }
                return submodelElement;
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
        public static SubmodelElementCollection BuildExternalDescriptor(string filename,JToken externalDescriptionTdObject, string contentType)
        {
            Aas.SubmodelElementCollection externalDescriptor = new Aas.SubmodelElementCollection();
            externalDescriptor.IdShort = "ExternalDescriptor";
            externalDescriptor.AddDescription("en", externalDescriptionTdObject["description"].ToString());
            externalDescriptor.SemanticId = idtaDef.ConstructReference(externalDescriptionTdObject["semanticReference"].ToString());

            Aas.File descriptorName = new Aas.File(contentType: contentType, idShort: "fileName",
                                                    semanticId: idtaDef.ConstructReference("https://admin-shell.io/idta/AssetInterfacesDescription/1/0/externalDescriptorName")
                                                    ); 
            descriptorName.Value = filename;

            externalDescriptor.Add(descriptorName);

            return externalDescriptor;
        }
        public static SubmodelElementCollection CreateAssetInterfaceDescriptionFromTd(
            JObject tdJObject, string filename, string _submodelId, int interfaceCount, string _contentType)
        {
            interfaceCount++;
            submodelId = _submodelId;
            interfaceId = "interface" + string.Format("{00:00}", interfaceCount);
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
                    if (childElem["formtext"].ToString() == "ExternalDescriptor")
                    {
                        interfaceDescription.Add(BuildExternalDescriptor(filename, childSchemaObject, _contentType));
                    }
                    else
                    {
                        interfaceDescription.Add(BuildAasElement(tdJObject, childSchemaObject));
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            
            /*

            interfaceDescription.Add(BuildEndPointMetaData(tdJObject, submodelId, interfaceDescription.IdShort));
            interfaceDescription.Add(BuildInterfaceMetaData(tdJObject));
            interfaceDescription.Add(BuildExternalDescriptor(filename));

            interfaceDescription.SemanticId = idtaDef.AID_Interface;

            */
            return interfaceDescription;

        }

    }
}
