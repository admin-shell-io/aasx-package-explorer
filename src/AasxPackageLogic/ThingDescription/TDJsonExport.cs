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
using AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace AasxPackageExplorer
{
    public static class TDJsonExport
    {
        public static JObject createForms(AasCore.Aas3_0_RC02.ISubmodelElement formsSem)
        {
            List<JObject> forms = new List<JObject>();
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection = (AasCore.Aas3_0_RC02.SubmodelElementCollection)formsSem.Copy();
            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempChild in _tempCollection.EnumerateChildren())
            {
                JObject formJObject = new JObject();
                AasCore.Aas3_0_RC02.ISubmodelElement form = _tempChild;
                foreach (Qualifier smQualifier in form.Qualifiers)
                {
                    formJObject[smQualifier.Type] = smQualifier.Value;
                }
                AasCore.Aas3_0_RC02.SubmodelElementCollection _formElementCollection = (AasCore.Aas3_0_RC02.SubmodelElementCollection)form.Copy();
                foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempformElement in
                        _formElementCollection.EnumerateChildren())
                {
                    AasCore.Aas3_0_RC02.ISubmodelElement _formElement = _tempformElement;
                    if (_formElement.IdShort == "security")
                    {
                        List<string> securityList = new List<string>();
                        foreach (Qualifier _secQual in _formElement.Qualifiers)
                        {
                            securityList.Add(_secQual.Value);
                        }
                        formJObject["security"] = JToken.FromObject(securityList);
                    }
                    else if (_formElement.IdShort == "scopes")
                    {
                        AasCore.Aas3_0_RC02.SubmodelElementCollection _scopesCollection =
                                    (AasCore.Aas3_0_RC02.SubmodelElementCollection)_formElement.Copy();
                        List<string> scopesList = new List<string>();
                        foreach (Qualifier _scopeQual in _scopesCollection.Qualifiers)
                        {
                            scopesList.Add(_scopeQual.Value);
                        }
                        formJObject["scopes"] = JToken.FromObject(scopesList);
                    }
                    else if (_formElement.IdShort == "response")
                    {
                        AasCore.Aas3_0_RC02.SubmodelElementCollection _response =
                            (AasCore.Aas3_0_RC02.SubmodelElementCollection)_formElement.Copy();
                        foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempResponse in _response.EnumerateChildren())
                        {
                            JObject contentTypeObject = new JObject();
                            contentTypeObject["contentType"] = (_tempResponse).ValueAsText();
                            formJObject["response"] = contentTypeObject;
                        }
                    }
                    else if (_formElement.IdShort == "additionalResponses")
                    {
                        AasCore.Aas3_0_RC02.SubmodelElementCollection _response =
                            (AasCore.Aas3_0_RC02.SubmodelElementCollection)_formElement.Copy();
                        JObject arJObject = new JObject();
                        foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempResponse in _response.EnumerateChildren())
                        {
                            if (_tempResponse.IdShort == "success")
                            {
                                arJObject["success"] = Convert.ToBoolean((_tempResponse).ValueAsText());

                            }
                            else
                            {
                                arJObject[_tempResponse.IdShort] =
                                    (_tempResponse).ValueAsText();

                            }
                        }
                    }
                    else if (_formElement.IdShort == "op")
                    {
                        AasCore.Aas3_0_RC02.SubmodelElementCollection _opCollection =
                            (AasCore.Aas3_0_RC02.SubmodelElementCollection)_formElement.Copy();
                        List<string> opList = new List<string>();
                        foreach (Qualifier _opQual in _opCollection.Qualifiers)
                        {
                            opList.Add(_opQual.Value);
                        }
                        formJObject["op"] = JToken.FromObject(opList);
                    }
                    else
                    {
                        //formJObject[_formElement.IdShort] =
                        //    _tempformElement.GetAs<AasCore.Aas3_0_RC02.Property>().Value.ToString();
                        formJObject[_formElement.IdShort] = (_tempformElement as AasCore.Aas3_0_RC02.Property).Value.ToString();
                    }
                }
                forms.Add(formJObject);
            }
            JObject formsjObject = new JObject();
            formsjObject["forms"] = JToken.FromObject(forms);
            return formsjObject;
        }
        public static JObject createuriVariables(AasCore.Aas3_0_RC02.ISubmodelElement uriSem)
        {
            JObject uriVarJObject = new JObject();
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection = (AasCore.Aas3_0_RC02.SubmodelElementCollection)uriSem.Copy();
            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempuriVarElement in _tempCollection.EnumerateChildren())
            {
                AasCore.Aas3_0_RC02.ISubmodelElement _uriVariable = _tempuriVarElement;
                uriVarJObject[_uriVariable.IdShort] = JToken.FromObject(createDataSchema(_uriVariable));
            }
            return uriVarJObject;
        }
        public static JObject createArraySchema(AasCore.Aas3_0_RC02.ISubmodelElement sem)
        {
            JObject semJObject = new JObject();
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection = (AasCore.Aas3_0_RC02.SubmodelElementCollection)sem.Copy();
            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempChild in _tempCollection.EnumerateChildren())
            {
                AasCore.Aas3_0_RC02.ISubmodelElement dsElement = _tempChild;
                if (dsElement.IdShort == "items")
                {
                    AasCore.Aas3_0_RC02.SubmodelElementCollection _items = (AasCore.Aas3_0_RC02.SubmodelElementCollection)dsElement.Copy();
                    List<JObject> itemsJObject = new List<JObject>();
                    foreach (AasCore.Aas3_0_RC02.ISubmodelElement _itemTemp in _items.EnumerateChildren())
                    {
                        AasCore.Aas3_0_RC02.ISubmodelElement item = _itemTemp;
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
        public static JObject createObjectSchema(AasCore.Aas3_0_RC02.ISubmodelElement sem)
        {
            JObject semJObject = new JObject();
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection = (AasCore.Aas3_0_RC02.SubmodelElementCollection)sem.Copy();
            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempChild in _tempCollection.EnumerateChildren())
            {
                AasCore.Aas3_0_RC02.ISubmodelElement dsElement = _tempChild;
                if (dsElement.IdShort == "properties")
                {
                    AasCore.Aas3_0_RC02.SubmodelElementCollection _properties =
                        (AasCore.Aas3_0_RC02.SubmodelElementCollection)dsElement.Copy();
                    JObject propertiesJObject = new JObject();
                    foreach (AasCore.Aas3_0_RC02.ISubmodelElement _itemTemp in _properties.EnumerateChildren())
                    {
                        AasCore.Aas3_0_RC02.ISubmodelElement item = _itemTemp;
                        JObject dsJObject = createDataSchema(item);
                        propertiesJObject[item.IdShort] = JToken.FromObject(dsJObject);
                    }
                    semJObject["properties"] = JToken.FromObject(propertiesJObject);
                }
                if (dsElement.IdShort == "required")
                {
                    List<string> requiredList = new List<string>();
                    foreach (Qualifier _requiredQual in dsElement.Qualifiers)
                    {
                        requiredList.Add(_requiredQual.Value);
                    }
                    semJObject["required"] = JToken.FromObject(requiredList);
                }
            }
            return semJObject;
        }

        public static List<JToken> enumELement(List<Qualifier> qualCollection)
        {

            List<JToken> enums = new List<JToken>();
            foreach (Qualifier _enumQual in qualCollection)
            {
                if (int.TryParse(_enumQual.Value, out int numericValue))
                {
                    enums.Add(numericValue);
                }
                else if (float.TryParse(_enumQual.Value, out float floatValue))
                {
                    enums.Add(floatValue);
                }
                else if (double.TryParse(_enumQual.Value, out double doubleValue))
                {
                    enums.Add(doubleValue);
                }
                else if (bool.TryParse(_enumQual.Value, out bool boolValue))
                {
                    enums.Add(boolValue);
                }
                else
                {
                    enums.Add(_enumQual.Value);
                }
            }
            return enums;
        }
        public static JObject createDataSchema(AasCore.Aas3_0_RC02.ISubmodelElement sem)
        {
            JObject semJObject = new JObject();
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection = (AasCore.Aas3_0_RC02.SubmodelElementCollection)sem.Copy();
            string dschemaType = "";
            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempChild in _tempCollection.EnumerateChildren())
            {
                AasCore.Aas3_0_RC02.ISubmodelElement dsElement = _tempChild;
                if (dsElement.IdShort == "titles")
                {
                    JObject _titlesJObject = new JObject();
                    AasCore.Aas3_0_RC02.MultiLanguageProperty mlp = (AasCore.Aas3_0_RC02.MultiLanguageProperty)dsElement.Copy();
                    var _titles = mlp.Value.Copy();
                    foreach (LangString _title in _titles)
                    {
                        _titlesJObject[_title.Language] = _title.Text;
                    }
                    semJObject["titles"] = _titlesJObject;
                }
                if (dsElement.IdShort == "oneOf")
                {
                    List<JObject> oneOfJObjects = new List<JObject>();
                    AasCore.Aas3_0_RC02.SubmodelElementCollection _enumCOllection =
                        (AasCore.Aas3_0_RC02.SubmodelElementCollection)dsElement.Copy();
                    foreach (AasCore.Aas3_0_RC02.ISubmodelElement _temponeOf in _enumCOllection.EnumerateChildren())
                    {
                        AasCore.Aas3_0_RC02.ISubmodelElement _oneOf = _temponeOf;
                        oneOfJObjects.Add(createDataSchema(_oneOf));
                    }
                    semJObject["oneOf"] = JToken.FromObject(oneOfJObjects);
                }
                if (dsElement.IdShort == "enum")
                {
                    semJObject["enum"] = JToken.FromObject(enumELement(dsElement.Qualifiers));
                }
            }
            if (sem.Description != null)
            {
                List<LangString> tdDescription = sem.Description;
                if (tdDescription.Count != 1)
                {
                    semJObject["description"] = tdDescription[0].Text;
                    int index = 1;
                    JObject descriptions = new JObject();
                    for (index = 1; index < tdDescription.Count; index++)
                    {
                        LangString desc = tdDescription[index];
                        descriptions[desc.Language] = desc.Text;
                    }
                    semJObject["descriptions"] = JToken.FromObject(descriptions);
                }
                else
                {
                    semJObject["description"] = tdDescription[0].Text;
                }
            }
            foreach (Qualifier smQualifier in sem.Qualifiers)
            {
                if (smQualifier.Type == "readOnly" || smQualifier.Type == "writeOnly")
                {
                    semJObject[smQualifier.Type] = Convert.ToBoolean(smQualifier.Value);
                }
                else if (smQualifier.Type == "minItems" || smQualifier.Type == "maxItems" ||
                    smQualifier.Type == "minLength" || smQualifier.Type == "maxLength")
                {
                    semJObject[smQualifier.Type] = Convert.ToUInt32(smQualifier.Value);
                }
                else if (smQualifier.Type == "data1.Type" || smQualifier.Type == "type")
                {
                    if (smQualifier.Type == "type")
                    {
                        semJObject[smQualifier.Type] = smQualifier.Value;
                    }
                    if (smQualifier.Type == "data1.Type")
                    {
                        semJObject["data1"] = JToken.FromObject(new JObject { ["type"] = smQualifier.Value });
                    }
                    dschemaType = smQualifier.Value;
                }
                else
                {
                    semJObject[smQualifier.Type] = smQualifier.Value;
                }
            }
            if (dschemaType == "array")
            {
                JObject arrayObject = createArraySchema(sem);
                if (arrayObject.ContainsKey("items"))
                {
                    semJObject["items"] = arrayObject["items"];
                }
            }
            else if (dschemaType == "object")
            {
                JObject objectSchemaJObject = createObjectSchema(sem);
                if (objectSchemaJObject.ContainsKey("properties"))
                {
                    semJObject["properties"] = JToken.FromObject(objectSchemaJObject["properties"]);
                }
                if (objectSchemaJObject.ContainsKey("required"))
                {
                    semJObject["required"] = JToken.FromObject(objectSchemaJObject["required"]);
                }
            }
            else if (dschemaType == "integer")
            {
                List<string> integerSchema = new List<string> { "minimum", "exclusiveMinimum", "maximum",
                    "exclusiveMaximum", "multipleOf" };
                foreach (string elem in integerSchema)
                {
                    foreach (Qualifier semQual in sem.Qualifiers)
                    {
                        if (elem == semQual.Type)
                        {
                            semJObject[semQual.Type] = (int)Convert.ToDouble(semQual.Value);
                        }
                    }
                }
            }
            else if (dschemaType == "number")
            {
                List<string> numberSchema = new List<string> { "minimum", "exclusiveMinimum", "maximum",
                    "exclusiveMaximum", "multipleOf" };
                foreach (string elem in numberSchema)
                {
                    foreach (Qualifier semQual in sem.Qualifiers)
                    {
                        if (elem == semQual.Type)
                        {
                            semJObject[semQual.Type] = Convert.ToDecimal(semQual.Value.ToString());
                        }
                    }
                }
            }



            return semJObject;
        }
        public static JObject createInteractionAvoidance(AasCore.Aas3_0_RC02.ISubmodelElement sem)
        {
            JObject semJObject = createDataSchema(sem);
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection = (AasCore.Aas3_0_RC02.SubmodelElementCollection)sem.Copy();
            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempChild in _tempCollection.EnumerateChildren())
            {
                AasCore.Aas3_0_RC02.ISubmodelElement dsElement = _tempChild;
                if (dsElement.IdShort == "forms")
                {
                    semJObject["forms"] = createForms(dsElement)["forms"];
                }
                if (dsElement.IdShort == "uriVariables")
                {
                    createuriVariables(dsElement);
                    semJObject["uriVariables"] = createuriVariables(dsElement);
                }
            }

            return semJObject;
        }
        public static JObject createTDProperties(AasCore.Aas3_0_RC02.ISubmodelElement propertiesSem)
        {
            JObject propertiesJObject = new JObject();
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection =
                (AasCore.Aas3_0_RC02.SubmodelElementCollection)propertiesSem.Copy();
            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempProperty in _tempCollection.EnumerateChildren())
            {
                AasCore.Aas3_0_RC02.ISubmodelElement _propoerty = _tempProperty;
                JObject propetyJObject = createInteractionAvoidance(_propoerty);
                if (propetyJObject.ContainsKey("observable"))
                {
                    propetyJObject["observable"] = Convert.ToBoolean(propetyJObject["observable"]);
                }
                propertiesJObject[_propoerty.IdShort] = propetyJObject;
            }
            return propertiesJObject;
        }
        public static JObject createTDActions(AasCore.Aas3_0_RC02.ISubmodelElement actionsSem)
        {
            JObject actionsJObject = new JObject();
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection = (AasCore.Aas3_0_RC02.SubmodelElementCollection)actionsSem.Copy();
            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempAction in _tempCollection.EnumerateChildren())
            {
                AasCore.Aas3_0_RC02.ISubmodelElement _action = _tempAction;
                JObject actionJObject = createInteractionAvoidance(_action);
                AasCore.Aas3_0_RC02.SubmodelElementCollection _actionItems = (AasCore.Aas3_0_RC02.SubmodelElementCollection)_action.Copy();
                foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempActionItem in _actionItems.EnumerateChildren())
                {
                    AasCore.Aas3_0_RC02.ISubmodelElement _actionItem = _tempActionItem;
                    if (_actionItem.IdShort == "input")
                    {
                        actionJObject["input"] = JToken.FromObject(createDataSchema(_actionItem));
                    }
                    if (_actionItem.IdShort == "output")
                    {
                        actionJObject["output"] = JToken.FromObject(createDataSchema(_actionItem));
                    }
                }
                foreach (Qualifier actionQual in _action.Qualifiers)
                {
                    if (actionQual.Type == "safe" || actionQual.Type == "idempotent")
                    {
                        actionJObject[actionQual.Type] = Convert.ToBoolean(actionQual.Value);
                    }
                }
                actionsJObject[_action.IdShort] = JToken.FromObject(actionJObject);
            }
            return actionsJObject;
        }
        public static JObject createTDEvents(AasCore.Aas3_0_RC02.ISubmodelElement eventsSem)
        {
            JObject eventsJObject = new JObject();
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection =
                (AasCore.Aas3_0_RC02.SubmodelElementCollection)eventsSem.Copy();
            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempEvent in _tempCollection.EnumerateChildren())
            {
                AasCore.Aas3_0_RC02.ISubmodelElement _event = _tempEvent;
                JObject actionJObject = createInteractionAvoidance(_event);
                AasCore.Aas3_0_RC02.SubmodelElementCollection _eventItems =
                    (AasCore.Aas3_0_RC02.SubmodelElementCollection)_event.Copy();
                foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempEventItem in _eventItems.EnumerateChildren())
                {
                    AasCore.Aas3_0_RC02.ISubmodelElement _eventItem = _tempEventItem;
                    if (_eventItem.IdShort == "subscription")
                    {
                        actionJObject["subscription"] = JToken.FromObject(createDataSchema(_eventItem));
                    }
                    if (_eventItem.IdShort == "data")
                    {
                        actionJObject["data"] = JToken.FromObject(createDataSchema(_eventItem));
                    }
                    if (_eventItem.IdShort == "cancellation")
                    {
                        actionJObject["cancellation"] = JToken.FromObject(createDataSchema(_eventItem));
                    }
                }
                eventsJObject[_event.IdShort] = JToken.FromObject(actionJObject);
            }
            return eventsJObject;
        }
        public static JObject createTDLinks(AasCore.Aas3_0_RC02.ISubmodelElement linksSem)
        {
            List<JObject> links = new List<JObject>();
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection = (AasCore.Aas3_0_RC02.SubmodelElementCollection)linksSem.Copy();
            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempLink in _tempCollection.EnumerateChildren())
            {
                AasCore.Aas3_0_RC02.ISubmodelElement link = _tempLink;
                JObject jObject = new JObject();
                foreach (Qualifier linkItem in link.Qualifiers)
                {
                    jObject[linkItem.Type] = linkItem.Value;
                }
                links.Add(jObject);
            }
            JObject linksJObject = new JObject();
            linksJObject["links"] = JToken.FromObject(links);
            return linksJObject;
        }
        public static JObject createTDSecurity(AasCore.Aas3_0_RC02.ISubmodelElement securitySem)
        {
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection =
                (AasCore.Aas3_0_RC02.SubmodelElementCollection)securitySem.Copy();
            List<string> securityList = new List<string>();
            foreach (Qualifier _security in _tempCollection.Qualifiers)
            {
                securityList.Add(_security.Value);
            }
            JObject securityJObject = new JObject();
            securityJObject["security"] = JToken.FromObject(securityList);
            return securityJObject;
        }

        public static JObject createSecurityScheme(AasCore.Aas3_0_RC02.ISubmodelElement sschemaSem)
        {
            JObject sschemaJOBject = new JObject();
            foreach (Qualifier smQualifier in sschemaSem.Qualifiers)
            {
                sschemaJOBject[smQualifier.Type] = smQualifier.Value;
            }
            if (sschemaSem.Description != null)
            {
                List<LangString> tdDescription = sschemaSem.Description;
                if (tdDescription.Count != 1)
                {
                    sschemaJOBject["description"] = tdDescription[0].Text;
                    int index = 1;
                    JObject descriptions = new JObject();
                    for (index = 1; index < tdDescription.Count; index++)
                    {
                        LangString desc = tdDescription[index];
                        descriptions[desc.Language] = desc.Text;
                    }
                    sschemaJOBject["descriptions"] = descriptions;
                }
                else
                {
                    sschemaJOBject["description"] = tdDescription[0].Text;
                }

            }

            return sschemaJOBject;
        }
        public static JObject createTDSecurityDefinitions(AasCore.Aas3_0_RC02.ISubmodelElement sdSem)
        {
            JObject securityDefinitionsJObject = new JObject();
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection = (AasCore.Aas3_0_RC02.SubmodelElementCollection)sdSem.Copy();
            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _tempSD in _tempCollection.EnumerateChildren())
            {
                AasCore.Aas3_0_RC02.ISubmodelElement _securityDefinition = _tempSD;
                JObject securityJObject = createSecurityScheme(_securityDefinition);
                AasCore.Aas3_0_RC02.SubmodelElementCollection _securityDItems =
                    (AasCore.Aas3_0_RC02.SubmodelElementCollection)_securityDefinition.Copy();
                foreach (var temp in (JToken)securityJObject)
                {
                    JProperty secObject = (JProperty)temp;
                    string key = secObject.Name.ToString();
                    if (key == "scheme")
                    {
                        string securityScheme = (secObject.Value).ToString();
                        if (securityScheme == "combo")
                        {
                            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _temp_combosecurityDItems in
                                _securityDItems.EnumerateChildren())
                            {
                                AasCore.Aas3_0_RC02.SubmodelElementCollection csdItem =
                                    (AasCore.Aas3_0_RC02.SubmodelElementCollection)_temp_combosecurityDItems.Copy();
                                List<string> csdItemList = new List<string>();
                                foreach (Qualifier _csdQual in csdItem.Qualifiers)
                                {
                                    csdItemList.Add(_csdQual.Value);
                                }
                                securityJObject[csdItem.IdShort] = JToken.FromObject(csdItemList);

                            }
                            securityDefinitionsJObject[_securityDefinition.IdShort] =
                                JToken.FromObject(securityJObject);
                        }
                        if (securityScheme == "oauth2")
                        {
                            foreach (AasCore.Aas3_0_RC02.ISubmodelElement _temp_combosecurityDItems in
                                _securityDItems.EnumerateChildren())
                            {
                                AasCore.Aas3_0_RC02.SubmodelElementCollection oauth2SDItem =
                                    (AasCore.Aas3_0_RC02.SubmodelElementCollection)_temp_combosecurityDItems.Copy();
                                List<string> csdItemList = new List<string>();
                                foreach (Qualifier _csdQual in oauth2SDItem.Qualifiers)
                                {
                                    csdItemList.Add(_csdQual.Value);
                                }
                                securityJObject[oauth2SDItem.IdShort] = JToken.FromObject(csdItemList);
                            }
                            securityDefinitionsJObject[_securityDefinition.IdShort] =
                                JToken.FromObject(securityJObject);
                        }
                    }
                }
                securityDefinitionsJObject[_securityDefinition.IdShort] = securityJObject;
            }
            return securityDefinitionsJObject;
        }
        public static JObject createTDProfile(AasCore.Aas3_0_RC02.ISubmodelElement profileSem)
        {
            JObject profileJObject = new JObject();
            AasCore.Aas3_0_RC02.SubmodelElementCollection _tempCollection =
                (AasCore.Aas3_0_RC02.SubmodelElementCollection)profileSem.Copy();
            List<string> profileList = new List<string>();
            foreach (Qualifier _profileQual in _tempCollection.Qualifiers)
            {
                profileList.Add(_profileQual.Value);
            }
            profileJObject["profile"] = JToken.FromObject(profileList);
            return profileJObject;
        }
        public static JObject createTDSchemaDefinitions(AasCore.Aas3_0_RC02.ISubmodelElement sem)
        {
            JObject semJObject = new JObject();
            return semJObject;
        }

        public static JObject ExportSMtoJson(Submodel sm)
        {
            JObject exportData = new JObject();
            try
            {
                JObject TDJson = new JObject();
                if (sm.Qualifiers != null)
                {
                    foreach (Qualifier smQualifier in sm.Qualifiers)
                    {
                        TDJson[smQualifier.Type] = smQualifier.Value.ToString();
                    }
                }

                // description
                if (sm.Description != null)
                {
                    List<LangString> tdDescription = sm.Description;
                    if (tdDescription.Count != 1)
                    {
                        TDJson["description"] = tdDescription[0].Text;
                        int index = 1;
                        JObject descriptions = new JObject();
                        for (index = 1; index < tdDescription.Count; index++)
                        {
                            LangString desc = tdDescription[index];
                            descriptions[desc.Language] = desc.Text;
                        }
                        TDJson["descriptions"] = descriptions;
                    }
                    else
                    {
                        TDJson["description"] = tdDescription[0].Text;
                    }
                }
                //version
                if (sm.Administration != null)
                {
                    JObject versionInfo = new JObject();
                    AdministrativeInformation adm = sm.Administration;
                    if (adm.Version != "")
                    {
                        versionInfo["instance"] = adm.Version;
                    }
                    if (adm.Revision != "")
                    {
                        versionInfo["model"] = adm.Version;
                    }
                    if (versionInfo.Count != 0)
                    {
                        TDJson["version"] = versionInfo;
                    }
                }
                // id
                TDJson["id"] = sm.Id;
                if (sm.SubmodelElements != null)
                {
                    foreach (AasCore.Aas3_0_RC02.ISubmodelElement tdElementWrapper in sm.SubmodelElements)
                    {
                        AasCore.Aas3_0_RC02.ISubmodelElement tdElement = tdElementWrapper;
                        if (tdElement.IdShort == "@type")
                        {
                            List<object> typeList = new List<object>();
                            foreach (Qualifier _typeQual in tdElement.Qualifiers)
                            {
                                typeList.Add((_typeQual.Value));

                            }
                            TDJson["@type"] = JToken.FromObject(typeList);
                        }
                        if (tdElement.IdShort == "titles")
                        {
                            JObject _titlesJObject = new JObject();
                            AasCore.Aas3_0_RC02.MultiLanguageProperty mlp = (AasCore.Aas3_0_RC02.MultiLanguageProperty)tdElement.Copy();
                            var _titles = mlp.Value.Copy();
                            foreach (LangString _title in _titles)
                            {
                                _titlesJObject[_title.Language] = _title.Text;
                            }
                            TDJson["titles"] = _titlesJObject;
                        }
                        if (tdElement.IdShort == "@context")
                        {
                            List<object> contextList = new List<object>();
                            JObject _conSemantic = new JObject();
                            foreach (Qualifier _con in tdElement.Qualifiers)
                            {
                                if (_con.Type == "@context")
                                {
                                    contextList.Add((_con.Value));
                                }
                                else
                                {
                                    _conSemantic[_con.Type] = _con.Value;

                                }
                            }
                            if (_conSemantic.Count != 0)
                            {
                                contextList.Add(_conSemantic);
                            }
                            TDJson["@context"] = JToken.FromObject(contextList);
                        }
                        if (tdElement.IdShort == "properties")
                        {
                            TDJson["properties"] = createTDProperties(tdElement);
                        }
                        else if (tdElement.IdShort == "actions")
                        {
                            TDJson["actions"] = createTDActions(tdElement);
                        }
                        else if (tdElement.IdShort == "events")
                        {
                            TDJson["events"] = createTDEvents(tdElement);
                        }
                        else if (tdElement.IdShort == "links")
                        {
                            TDJson["links"] = createTDLinks(tdElement)["links"];
                        }
                        else if (tdElement.IdShort == "forms")
                        {
                            TDJson["forms"] = createForms(tdElement)["forms"];
                        }
                        else if (tdElement.IdShort == "security")
                        {
                            TDJson["security"] = createTDSecurity(tdElement)["security"];
                        }
                        else if (tdElement.IdShort == "securityDefinitions")
                        {
                            TDJson["securityDefinitions"] = createTDSecurityDefinitions(tdElement);
                        }
                        else if (tdElement.IdShort == "profile")
                        {
                            TDJson["profile"] = createTDProfile(tdElement);
                        }
                        else if (tdElement.IdShort == "schemaDefinitions")
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