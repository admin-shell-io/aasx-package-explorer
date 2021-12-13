/*
Copyright (c) 2021-2022 Otto-von-Guericke-Universität Magdeburg, Lehrstuhl Integrierte Automation
harish.pakala@ovgu.de, Author: Harish Kumar Pakala

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using AdminShellNS;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace AasxPackageExplorer
{
    public static class TDJsonExport
    {
        public static JObject createForms(AdminShell.SubmodelElement formsSem)
        {
            List<JObject> forms = new List<JObject>();
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(formsSem);
            foreach (AdminShell.SubmodelElementWrapper _tempChild in _tempCollection.EnumerateChildren())
            {
                JObject formJObject = new JObject();
                AdminShell.SubmodelElement form = _tempChild.submodelElement;
                foreach (AdminShell.Qualifier smQualifier in form.qualifiers)
                {
                    formJObject[smQualifier.type] = smQualifier.value;
                }
                AdminShell.SubmodelElementCollection _formElementCollection = new AdminShell.SubmodelElementCollection(form);
                foreach (AdminShell.SubmodelElementWrapper _tempformElement in _formElementCollection.EnumerateChildren())
                {
                    AdminShell.SubmodelElement _formElement = _tempformElement.submodelElement;
                    if (_formElement.idShort == "security")
                    {
                        List<string> securityList = new List<string>();
                        foreach (AdminShell.Qualifier _secQual in _formElement.qualifiers)
                        {
                            securityList.Add(_secQual.value);
                        }
                        formJObject["security"] = JToken.FromObject(securityList);
                    }
                    else if (_formElement.idShort == "scopes")
                    {
                        AdminShell.SubmodelElementCollection _scopesCollection = new AdminShell.SubmodelElementCollection(_formElement, false);
                        List<string> scopesList = new List<string>();
                        foreach (AdminShell.Qualifier _scopeQual in _scopesCollection.qualifiers)
                        {
                            scopesList.Add(_scopeQual.value);
                        }
                        formJObject["scopes"] = JToken.FromObject(scopesList);
                    }
                    else if (_formElement.idShort == "response")
                    {
                        AdminShell.SubmodelElementCollection _response = new AdminShell.SubmodelElementCollection(_formElement, false);
                        foreach (AdminShell.SubmodelElementWrapper _tempResponse in _response.EnumerateChildren())
                        {
                            JObject contentTypeObject = new JObject();
                            contentTypeObject["contentType"] = (_tempResponse.submodelElement).ValueAsText();
                            formJObject["response"] = contentTypeObject;
                        }
                    }
                    else if (_formElement.idShort == "additionalResponses")
                    {
                        AdminShell.SubmodelElementCollection _response = new AdminShell.SubmodelElementCollection(_formElement, false);
                        JObject arJObject = new JObject();
                        foreach (AdminShell.SubmodelElementWrapper _tempResponse in _response.EnumerateChildren())
                        {
                            if (_tempResponse.submodelElement.idShort == "success")
                            {
                                arJObject["success"] = Convert.ToBoolean((_tempResponse.submodelElement).ValueAsText());

                            }
                            else
                            {
                                arJObject[_tempResponse.submodelElement.idShort] = (_tempResponse.submodelElement).ValueAsText();

                            }
                        }
                    }
                    else if (_formElement.idShort == "op")
                    {
                        AdminShell.SubmodelElementCollection _opCollection = new AdminShell.SubmodelElementCollection(_formElement, false);
                        List<string> opList = new List<string>();
                        foreach (AdminShell.Qualifier _opQual in _opCollection.qualifiers)
                        {
                            opList.Add(_opQual.value);
                        }
                        formJObject["op"] = JToken.FromObject(opList);
                    }
                    else
                    {
                        formJObject[_formElement.idShort] = _tempformElement.GetAs<AdminShell.Property>().value.ToString();
                    }
                }
                forms.Add(formJObject);
            }
            JObject formsjObject = new JObject();
            formsjObject["forms"] = JToken.FromObject(forms);
            return formsjObject;
        }
        public static JObject createuriVariables(AdminShell.SubmodelElement uriSem)
        {
            JObject uriVarJObject = new JObject();
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(uriSem);
            foreach (AdminShell.SubmodelElementWrapper _tempuriVarElement in _tempCollection.EnumerateChildren())
            {
                AdminShell.SubmodelElement _uriVariable = _tempuriVarElement.submodelElement;
                uriVarJObject[_uriVariable.idShort] = JToken.FromObject(createDataSchema(_uriVariable));
            }
            return uriVarJObject;
        }
        public static JObject createArraySchema(AdminShell.SubmodelElement sem)
        {
            JObject semJObject = new JObject();
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(sem);
            foreach (AdminShell.SubmodelElementWrapper _tempChild in _tempCollection.EnumerateChildren())
            {
                AdminShell.SubmodelElement dsElement = _tempChild.submodelElement;
                if (dsElement.idShort == "items")
                {
                    AdminShell.SubmodelElementCollection _items = new AdminShell.SubmodelElementCollection(dsElement);
                    List<JObject> itemsJObject = new List<JObject>();
                    foreach (AdminShell.SubmodelElementWrapper _itemTemp in _items.EnumerateChildren())
                    {
                        AdminShell.SubmodelElement item = _itemTemp.submodelElement;
                        JObject dsJObject = createDataSchema(item);
                        itemsJObject.Add(dsJObject);
                    }
                    if (itemsJObject.Count == 1)
                    {
                        semJObject["items"] = JToken.FromObject(itemsJObject[0]);
                    }
                    else
                    {
                        semJObject["items"] = JToken.FromObject(itemsJObject);
                    }

                }
            }
            return semJObject;
        }
        public static JObject createObjectSchema(AdminShell.SubmodelElement sem)
        {
            JObject semJObject = new JObject();
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(sem);
            foreach (AdminShell.SubmodelElementWrapper _tempChild in _tempCollection.EnumerateChildren())
            {
                AdminShell.SubmodelElement dsElement = _tempChild.submodelElement;
                if (dsElement.idShort == "properties")
                {
                    AdminShell.SubmodelElementCollection _properties = new AdminShell.SubmodelElementCollection(dsElement);
                    JObject propertiesJObject = new JObject();
                    foreach (AdminShell.SubmodelElementWrapper _itemTemp in _properties.EnumerateChildren())
                    {
                        AdminShell.SubmodelElement item = _itemTemp.submodelElement;
                        JObject dsJObject = createDataSchema(item);
                        propertiesJObject[item.idShort] = JToken.FromObject(dsJObject);
                    }
                    semJObject["properties"] = JToken.FromObject(propertiesJObject);
                }
                if (dsElement.idShort == "required")
                {
                    List<string> requiredList = new List<string>();
                    foreach (AdminShell.Qualifier _requiredQual in dsElement.qualifiers)
                    {
                        requiredList.Add(_requiredQual.value);
                    }
                    semJObject["required"] = JToken.FromObject(requiredList);
                }
            }
            return semJObject;
        }

        public static List<JToken> enumELement(AdminShell.QualifierCollection qualCollection)
        {

            List<JToken> enums = new List<JToken>();
            foreach (AdminShell.Qualifier _enumQual in qualCollection)
            {
                if (int.TryParse(_enumQual.value, out int numericValue))
                {
                    enums.Add(numericValue);
                }
                else if (float.TryParse(_enumQual.value, out float floatValue))
                {
                    enums.Add(floatValue);
                }
                else if (double.TryParse(_enumQual.value, out double doubleValue))
                {
                    enums.Add(doubleValue);
                }
                else if (bool.TryParse(_enumQual.value, out bool boolValue))
                {
                    enums.Add(boolValue);
                }
                else
                {
                    enums.Add(_enumQual.value);
                }
            }
            return enums;
        }
        public static JObject createDataSchema(AdminShell.SubmodelElement sem)
        {
            JObject semJObject = new JObject();
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(sem);
            foreach (AdminShell.SubmodelElementWrapper _tempChild in _tempCollection.EnumerateChildren())
            {
                AdminShell.SubmodelElement dsElement = _tempChild.submodelElement;
                if (dsElement.idShort == "titles")
                {
                    JObject _titlesJObject = new JObject();
                    AdminShell.MultiLanguageProperty mlp = new AdminShell.MultiLanguageProperty(dsElement);
                    AdminShell.LangStringSet _titles = new AdminShell.LangStringSet(mlp.value);
                    foreach (AdminShell.LangStr _title in _titles.langString)
                    {
                        _titlesJObject[_title.lang] = _title.str;
                    }
                    semJObject["titles"] = _titlesJObject;
                }
                if (dsElement.idShort == "oneOf")
                {
                    List<JObject> oneOfJObjects = new List<JObject>();
                    AdminShell.SubmodelElementCollection _enumCOllection = new AdminShell.SubmodelElementCollection(dsElement);
                    foreach (AdminShell.SubmodelElementWrapper _temponeOf in _enumCOllection.EnumerateChildren())
                    {
                        AdminShell.SubmodelElement _oneOf = _temponeOf.submodelElement;
                        oneOfJObjects.Add(createDataSchema(_oneOf));
                    }
                    semJObject["oneOf"] = JToken.FromObject(oneOfJObjects);
                }
                if (dsElement.idShort == "enum")
                {
                    semJObject["enum"] = JToken.FromObject(enumELement(dsElement.qualifiers));
                }
            }
            if (sem.description != null)
            {
                AdminShell.ListOfLangStr tdDescription = sem.description.langString;
                if (tdDescription.Count != 1)
                {
                    semJObject["description"] = tdDescription[0].str;
                    int index = 1;
                    JObject descriptions = new JObject();
                    for (index = 1; index < tdDescription.Count; index++)
                    {
                        AdminShell.LangStr desc = tdDescription[index];
                        descriptions[desc.lang] = desc.str;
                    }
                    semJObject["descriptions"] = JToken.FromObject(descriptions);
                }
                else
                {
                    semJObject["description"] = tdDescription[0].str;
                }
            }
            foreach (AdminShell.Qualifier smQualifier in sem.qualifiers)
            {
                if (smQualifier.type == "readOnly" || smQualifier.type == "writeOnly")
                {
                    semJObject[smQualifier.type] = Convert.ToBoolean(smQualifier.value);
                }
                else if (smQualifier.type == "minItems" || smQualifier.type == "maxItems" || smQualifier.type == "minLength" || smQualifier.type == "maxLength")
                {
                    semJObject[smQualifier.type] = Convert.ToUInt32(smQualifier.value);
                }
                else
                {
                    semJObject[smQualifier.type] = smQualifier.value;
                }
            }
            foreach (var temp in (JToken)semJObject)
            {
                JProperty typeObject = (JProperty)temp;
                string key = typeObject.Name.ToString();
                if (key == "type")
                {
                    string dsType = typeObject.Value.ToString();
                    if (dsType == "array")
                    {
                        JObject arrayObject = createArraySchema(sem);
                        if (arrayObject.ContainsKey("items"))
                        {
                            semJObject["items"] = arrayObject["items"];
                        }
                    }
                    if (dsType == "object")
                    {
                        JObject objectSchemaJObject = createObjectSchema(sem);
                        if (objectSchemaJObject.ContainsKey("properties"))
                        {
                            semJObject["properties"] = JToken.FromObject( objectSchemaJObject["properties"]);
                        }
                        if (objectSchemaJObject.ContainsKey("required"))
                        {
                            semJObject["required"] = JToken.FromObject(objectSchemaJObject["required"]);
                        }
                    }
                    if (dsType == "integer")
                    {
                        List<string> integerSchema = new List<string> { "minimum", "exclusiveMinimum", "maximum", "exclusiveMaximum", "multipleOf" };
                        foreach (string elem in integerSchema)
                        {
                            foreach (AdminShell.Qualifier semQual in sem.qualifiers)
                            {
                                if (elem == semQual.type)
                                {
                                    semJObject[semQual.type] = (int)Convert.ToDouble(semQual.value);
                                }
                            }
                        }
                    }
                    if (dsType == "number")
                    {
                        List<string> numberSchema = new List<string> { "minimum", "exclusiveMinimum", "maximum", "exclusiveMaximum", "multipleOf" };
                        foreach (string elem in numberSchema)
                        {
                            foreach (AdminShell.Qualifier semQual in sem.qualifiers)
                            {
                                if (elem == semQual.type) 
                                {
                                    semJObject[semQual.type] = Convert.ToDecimal(semQual.value.ToString());
                                }
                            }
                        }
                    }
                    break;
                }
            }
            
            return semJObject;
        }
        public static JObject createInteractionAvoidance(AdminShell.SubmodelElement sem)
        {
            JObject semJObject = createDataSchema(sem);
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(sem);
            foreach (AdminShell.SubmodelElementWrapper _tempChild in _tempCollection.EnumerateChildren())
            {
                AdminShell.SubmodelElement dsElement = _tempChild.submodelElement;
                if (dsElement.idShort == "forms")
                {
                    semJObject["forms"] = createForms(dsElement)["forms"];
                }
                if (dsElement.idShort == "uriVariables")
                {
                    createuriVariables(dsElement);
                    semJObject["uriVariables"] = createuriVariables(dsElement);
                }
            }

            return semJObject;
        }
        public static JObject createTDProperties(AdminShell.SubmodelElement propertiesSem)
        {
            JObject propertiesJObject = new JObject();
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(propertiesSem);
            foreach (AdminShell.SubmodelElementWrapper _tempProperty in _tempCollection.EnumerateChildren())
            {
                AdminShell.SubmodelElement _propoerty = _tempProperty.submodelElement;
                JObject propetyJObject = createInteractionAvoidance(_propoerty);
                if (propetyJObject.ContainsKey("observable"))
                {
                    propetyJObject["observable"] = Convert.ToBoolean(propetyJObject["observable"]);
                }
                propertiesJObject[_propoerty.idShort] = propetyJObject;
            }
            return propertiesJObject;
        }
        public static JObject createTDActions(AdminShell.SubmodelElement actionsSem)
        {
            JObject actionsJObject = new JObject();
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(actionsSem);
            foreach (AdminShell.SubmodelElementWrapper _tempAction in _tempCollection.EnumerateChildren())
            {
                AdminShell.SubmodelElement _action = _tempAction.submodelElement;
                JObject actionJObject = createInteractionAvoidance(_action);
                AdminShell.SubmodelElementCollection _actionItems = new AdminShell.SubmodelElementCollection(_action);
                foreach (AdminShell.SubmodelElementWrapper _tempActionItem in _actionItems.EnumerateChildren())
                {
                    AdminShell.SubmodelElement _actionItem = _tempActionItem.submodelElement;
                    if (_actionItem.idShort == "input")
                    {
                        actionJObject["input"] = JToken.FromObject(createDataSchema(_actionItem));
                    }
                    if (_actionItem.idShort == "output")
                    {
                        actionJObject["output"] = JToken.FromObject(createDataSchema(_actionItem));
                    }
                }
                foreach (AdminShell.Qualifier actionQual in _action.qualifiers)
                {
                    if (actionQual.type == "safe" || actionQual.type == "idempotent")
                    {
                        actionJObject[actionQual.type] = Convert.ToBoolean(actionQual.value);
                    }
                }
                actionsJObject[_action.idShort] = JToken.FromObject(actionJObject);
            }
            return actionsJObject;
        }
        public static JObject createTDEvents(AdminShell.SubmodelElement eventsSem)
        {
            JObject eventsJObject = new JObject();
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(eventsSem);
            foreach (AdminShell.SubmodelElementWrapper _tempEvent in _tempCollection.EnumerateChildren())
            {
                AdminShell.SubmodelElement _event = _tempEvent.submodelElement;
                JObject actionJObject = createInteractionAvoidance(_event);
                AdminShell.SubmodelElementCollection _eventItems = new AdminShell.SubmodelElementCollection(_event);
                foreach (AdminShell.SubmodelElementWrapper _tempEventItem in _eventItems.EnumerateChildren())
                {
                    AdminShell.SubmodelElement _eventItem = _tempEventItem.submodelElement;
                    if (_eventItem.idShort == "subscription")
                    {
                        actionJObject["subscription"] = JToken.FromObject(createDataSchema(_eventItem));
                    }
                    if (_eventItem.idShort == "data")
                    {
                        actionJObject["data"] = JToken.FromObject(createDataSchema(_eventItem));
                    }
                    if (_eventItem.idShort == "cancellation")
                    {
                        actionJObject["cancellation"] = JToken.FromObject(createDataSchema(_eventItem));
                    }
                }
                eventsJObject[_event.idShort] = JToken.FromObject(actionJObject);
            }
            return eventsJObject;
        }
        public static JObject createTDLinks(AdminShell.SubmodelElement linksSem)
        {
            List<JObject> links = new List<JObject>();
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(linksSem);
            foreach (AdminShell.SubmodelElementWrapper _tempLink in _tempCollection.EnumerateChildren())
            {
                AdminShell.SubmodelElement link = _tempLink.submodelElement;
                JObject jObject = new JObject();
                foreach (AdminShell.Qualifier linkItem in link.qualifiers)
                {
                    jObject[linkItem.type] = linkItem.value;
                }
                links.Add(jObject);
            }
            JObject linksJObject = new JObject();
            linksJObject["links"] = JToken.FromObject(links);
            return linksJObject;
        }
        public static JObject createTDSecurity(AdminShell.SubmodelElement securitySem)
        {
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(securitySem);
            List<string> securityList = new List<string>();
            foreach (AdminShell.Qualifier _security in _tempCollection.qualifiers)
            {
                securityList.Add(_security.value);
            }
            JObject securityJObject = new JObject();
            securityJObject["security"] = JToken.FromObject(securityList);
            return securityJObject;
        }

        public static JObject createSecurityScheme(AdminShell.SubmodelElement sschemaSem)
        {
            JObject sschemaJOBject = new JObject();
            foreach (AdminShell.Qualifier smQualifier in sschemaSem.qualifiers)
            {
                sschemaJOBject[smQualifier.type] = smQualifier.value;
            }
            if (sschemaSem.description != null)
            {
                AdminShell.ListOfLangStr tdDescription = sschemaSem.description.langString;
                if (tdDescription.Count != 1)
                {
                    sschemaJOBject["description"] = tdDescription[0].str;
                    int index = 1;
                    JObject descriptions = new JObject();
                    for (index = 1; index < tdDescription.Count; index++)
                    {
                        AdminShell.LangStr desc = tdDescription[index];
                        descriptions[desc.lang] = desc.str;
                    }
                    sschemaJOBject["descriptions"] = descriptions;
                }
                else
                {
                    sschemaJOBject["description"] = tdDescription[0].str;
                }

            }

            return sschemaJOBject;
        }
        public static JObject createTDSecurityDefinitions(AdminShell.SubmodelElement sdSem)
        {
            JObject securityDefinitionsJObject = new JObject();
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(sdSem);
            foreach (AdminShell.SubmodelElementWrapper _tempSD in _tempCollection.EnumerateChildren())
            {
                AdminShell.SubmodelElement _securityDefinition = _tempSD.submodelElement;
                JObject securityJObject = createSecurityScheme(_securityDefinition);
                AdminShell.SubmodelElementCollection _securityDItems = new AdminShell.SubmodelElementCollection(_securityDefinition);
                foreach (var temp in (JToken)securityJObject)
                {
                    JProperty secObject = (JProperty) temp;
                    string key = secObject.Name.ToString();
                    if (key == "scheme")
                    {
                        string securityScheme = (secObject.Value).ToString();
                        if (securityScheme == "combo")
                        {
                            foreach (AdminShell.SubmodelElementWrapper _temp_combosecurityDItems in _securityDItems.EnumerateChildren())
                            {
                                AdminShell.SubmodelElementCollection csdItem = new AdminShell.SubmodelElementCollection(_temp_combosecurityDItems.submodelElement);
                                List<string> csdItemList = new List<string>();
                                foreach (AdminShell.Qualifier _csdQual in csdItem.qualifiers)
                                {
                                    csdItemList.Add(_csdQual.value);
                                }
                                securityJObject[csdItem.idShort] = JToken.FromObject(csdItemList);

                            }
                            securityDefinitionsJObject[_securityDefinition.idShort] = JToken.FromObject(securityJObject);
                        }
                        if (securityScheme == "oauth2")
                        {
                            foreach (AdminShell.SubmodelElementWrapper _temp_combosecurityDItems in _securityDItems.EnumerateChildren())
                            {
                                AdminShell.SubmodelElementCollection oauth2SDItem = new AdminShell.SubmodelElementCollection(_temp_combosecurityDItems.submodelElement);
                                List<string> csdItemList = new List<string>();
                                foreach (AdminShell.Qualifier _csdQual in oauth2SDItem.qualifiers)
                                {
                                    csdItemList.Add(_csdQual.value);
                                }
                                securityJObject[oauth2SDItem.idShort] = JToken.FromObject(csdItemList);
                            }
                            securityDefinitionsJObject[_securityDefinition.idShort] = JToken.FromObject(securityJObject);
                        }
                    }
                }
                securityDefinitionsJObject[_securityDefinition.idShort] = securityJObject;
            }
            return securityDefinitionsJObject;
        }
        public static JObject createTDProfile(AdminShell.SubmodelElement profileSem)
        {
            JObject profileJObject = new JObject();
            AdminShell.SubmodelElementCollection _tempCollection = new AdminShell.SubmodelElementCollection(profileSem);
            List<string> profileList = new List<string>();
            foreach (AdminShell.Qualifier _profileQual in _tempCollection.qualifiers)
            {
                profileList.Add(_profileQual.value);
            }
            profileJObject["profile"] = JToken.FromObject(profileList);
            return profileJObject;
        }
        public static JObject createTDSchemaDefinitions(AdminShell.SubmodelElement sem)
        {
            JObject semJObject = new JObject();
            return semJObject;
        }

        public static JObject ExportSMtoJson(AdminShell.Submodel sm)
        {
            JObject exportData = new JObject();
            try
            {
                JObject TDJson = new JObject();
                if (sm.qualifiers != null)
                {
                    foreach (AdminShell.Qualifier smQualifier in sm.qualifiers)
                    {
                        TDJson[smQualifier.type] = smQualifier.value.ToString();
                    }
                }

                // description
                if (sm.description != null)
                {
                    AdminShell.ListOfLangStr tdDescription = sm.description.langString;
                    if (tdDescription.Count != 1)
                    {
                        TDJson["description"] = tdDescription[0].str;
                        int index = 1;
                        JObject descriptions = new JObject();
                        for (index = 1; index < tdDescription.Count; index++)
                        {
                            AdminShell.LangStr desc = tdDescription[index];
                            descriptions[desc.lang] = desc.str;
                        }
                        TDJson["descriptions"] = descriptions;
                    }
                    else
                    {
                        TDJson["description"] = tdDescription[0].str;
                    }
                }
                //version
                if (sm.administration != null)
                {
                    JObject versionInfo = new JObject();
                    AdminShell.Administration adm = sm.administration;
                    if (adm.version != "")
                    {
                        versionInfo["instance"] = adm.version;
                    }
                    if (adm.revision != "")
                    {
                        versionInfo["model"] = adm.version;
                    }
                    if (versionInfo.Count != 0)
                    {
                        TDJson["version"] = versionInfo;
                    }
                }
                // id
                TDJson["id"] = sm.identification.id;
                if (sm.submodelElements != null)
                {
                    foreach (AdminShell.SubmodelElementWrapper tdElementWrapper in sm.submodelElements)
                    {
                        AdminShell.SubmodelElement tdElement = tdElementWrapper.submodelElement;
                        if (tdElement.idShort == "@type")
                        {
                            List<object> typeList = new List<object>();
                            foreach (AdminShell.Qualifier _typeQual in tdElement.qualifiers)
                            {
                                typeList.Add((_typeQual.value));

                            }
                            TDJson["@type"] = JToken.FromObject(typeList);
                        }
                        if (tdElement.idShort == "titles")
                        {
                            JObject _titlesJObject = new JObject();
                            AdminShell.MultiLanguageProperty mlp = new AdminShell.MultiLanguageProperty(tdElement);
                            AdminShell.LangStringSet _titles = new AdminShell.LangStringSet(mlp.value);
                            foreach (AdminShell.LangStr _title in _titles.langString)
                            {
                                _titlesJObject[_title.lang] = _title.str;
                            }
                            TDJson["titles"] = _titlesJObject;
                        }
                        if (tdElement.idShort == "@context")
                        {
                            List<object> contextList = new List<object>();
                            JObject _conSemantic = new JObject();
                            foreach (AdminShell.Qualifier _con in tdElement.qualifiers)
                            {
                                if (_con.type == "@context")
                                {
                                    contextList.Add((_con.value));
                                }
                                else
                                {   _conSemantic[_con.type] = _con.value;
                                   
                                }  
                            }
                            contextList.Add(_conSemantic);
                            TDJson["@context"] = JToken.FromObject(contextList);
                        }
                        if (tdElement.idShort == "properties")
                        {
                            TDJson["properties"] = createTDProperties(tdElement);
                        }
                        else if (tdElement.idShort == "actions")
                        {
                            TDJson["actions"] = createTDActions(tdElement);
                        }
                        else if (tdElement.idShort == "events")
                        {
                            TDJson["events"] = createTDEvents(tdElement);
                        }
                        else if (tdElement.idShort == "links")
                        {
                            TDJson["links"] = createTDLinks(tdElement)["links"];
                        }
                        else if (tdElement.idShort == "forms")
                        {
                            TDJson["forms"] = createForms(tdElement)["forms"];
                        }
                        else if (tdElement.idShort == "security")
                        {
                            TDJson["security"] = createTDSecurity(tdElement)["security"];
                        }
                        else if (tdElement.idShort == "securityDefinitions")
                        {
                            TDJson["securityDefinitions"] = createTDSecurityDefinitions(tdElement);
                        }
                        else if (tdElement.idShort == "profile")
                        {
                            TDJson["profile"] = createTDProfile(tdElement);
                        }
                        else if (tdElement.idShort == "schemaDefinitions")
                        {
                            TDJson["schemaDefinitions"] = createTDSchemaDefinitions(tdElement);
                        }
                    }
                }
                exportData["status"] = "success";
                exportData["data"] = TDJson;

            }
            catch (Exception ex)
            {
                exportData["status"] = "error";
                exportData["data"] = ex.ToString();
            }
            return exportData;

        }
    }
}