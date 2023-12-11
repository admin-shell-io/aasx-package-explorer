/*
Copyright (c) 2021-2022 Otto-von-Guericke-Universität Magdeburg, Lehrstuhl Integrierte Automation
harish.pakala@ovgu.de, Author: Harish Kumar Pakala

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Aas = AasCore.Aas3_0;

namespace AasxPackageExplorer
{
    public static class TDJsonExport
    {
        public static JObject createForms(Aas.ISubmodelElement formsSem)
        {
            List<JObject> forms = new List<JObject>();
            Aas.SubmodelElementCollection _tempCollection = (Aas.SubmodelElementCollection)formsSem.Copy();
            foreach (Aas.ISubmodelElement _tempChild in _tempCollection.EnumerateChildren())
            {
                JObject formJObject = new JObject();
                Aas.ISubmodelElement form = _tempChild;
                foreach (Aas.Qualifier smQualifier in form.Qualifiers)
                {
                    formJObject[smQualifier.Type] = smQualifier.Value;
                }
                Aas.SubmodelElementCollection _formElementCollection = (Aas.SubmodelElementCollection)form.Copy();
                foreach (Aas.ISubmodelElement _tempformElement in
                        _formElementCollection.EnumerateChildren())
                {
                    Aas.ISubmodelElement _formElement = _tempformElement;
                    if (_formElement.IdShort == "security")
                    {
                        List<string> securityList = new List<string>();
                        foreach (Aas.Qualifier _secQual in _formElement.Qualifiers)
                        {
                            securityList.Add(_secQual.Value);
                        }
                        formJObject["security"] = JToken.FromObject(securityList);
                    }
                    else if (_formElement.IdShort == "scopes")
                    {
                        Aas.SubmodelElementCollection _scopesCollection =
                                    (Aas.SubmodelElementCollection)_formElement.Copy();
                        List<string> scopesList = new List<string>();
                        foreach (Aas.Qualifier _scopeQual in _scopesCollection.Qualifiers)
                        {
                            scopesList.Add(_scopeQual.Value);
                        }
                        formJObject["scopes"] = JToken.FromObject(scopesList);
                    }
                    else if (_formElement.IdShort == "response")
                    {
                        Aas.SubmodelElementCollection _response =
                            (Aas.SubmodelElementCollection)_formElement.Copy();
                        foreach (Aas.ISubmodelElement _tempResponse in _response.EnumerateChildren())
                        {
                            JObject contentTypeObject = new JObject();
                            contentTypeObject["contentType"] = (_tempResponse).ValueAsText();
                            formJObject["response"] = contentTypeObject;
                        }
                    }
                    else if (_formElement.IdShort == "additionalResponses")
                    {
                        Aas.SubmodelElementCollection _response =
                            (Aas.SubmodelElementCollection)_formElement.Copy();
                        JObject arJObject = new JObject();
                        foreach (Aas.ISubmodelElement _tempResponse in _response.EnumerateChildren())
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
                        Aas.SubmodelElementCollection _opCollection =
                            (Aas.SubmodelElementCollection)_formElement.Copy();
                        List<string> opList = new List<string>();
                        foreach (Aas.Qualifier _opQual in _opCollection.Qualifiers)
                        {
                            opList.Add(_opQual.Value);
                        }
                        formJObject["op"] = JToken.FromObject(opList);
                    }
                    else
                    {
                        formJObject[_formElement.IdShort] = (_tempformElement as Aas.Property).Value.ToString();
                    }
                }
                forms.Add(formJObject);
            }
            JObject formsjObject = new JObject();
            formsjObject["forms"] = JToken.FromObject(forms);
            return formsjObject;
        }
        public static JObject createuriVariables(Aas.ISubmodelElement uriSem)
        {
            JObject uriVarJObject = new JObject();
            Aas.SubmodelElementCollection _tempCollection = (Aas.SubmodelElementCollection)uriSem.Copy();
            foreach (Aas.ISubmodelElement _tempuriVarElement in _tempCollection.EnumerateChildren())
            {
                Aas.ISubmodelElement _uriVariable = _tempuriVarElement;
                uriVarJObject[_uriVariable.IdShort] = JToken.FromObject(createDataSchema(_uriVariable));
            }
            return uriVarJObject;
        }
        public static JObject createArraySchema(Aas.ISubmodelElement sem)
        {
            JObject semJObject = new JObject();
            Aas.SubmodelElementCollection _tempCollection = (Aas.SubmodelElementCollection)sem.Copy();
            foreach (Aas.ISubmodelElement _tempChild in _tempCollection.EnumerateChildren())
            {
                Aas.ISubmodelElement dsElement = _tempChild;
                if (dsElement.IdShort == "items")
                {
                    Aas.SubmodelElementCollection _items = (Aas.SubmodelElementCollection)dsElement.Copy();
                    List<JObject> itemsJObject = new List<JObject>();
                    foreach (Aas.ISubmodelElement _itemTemp in _items.EnumerateChildren())
                    {
                        Aas.ISubmodelElement item = _itemTemp;
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
        public static JObject createObjectSchema(Aas.ISubmodelElement sem)
        {
            JObject semJObject = new JObject();
            Aas.SubmodelElementCollection _tempCollection = (Aas.SubmodelElementCollection)sem.Copy();
            foreach (Aas.ISubmodelElement _tempChild in _tempCollection.EnumerateChildren())
            {
                Aas.ISubmodelElement dsElement = _tempChild;
                if (dsElement.IdShort == "properties")
                {
                    Aas.SubmodelElementCollection _properties =
                        (Aas.SubmodelElementCollection)dsElement.Copy();
                    JObject propertiesJObject = new JObject();
                    foreach (Aas.ISubmodelElement _itemTemp in _properties.EnumerateChildren())
                    {
                        Aas.ISubmodelElement item = _itemTemp;
                        JObject dsJObject = createDataSchema(item);
                        propertiesJObject[item.IdShort] = JToken.FromObject(dsJObject);
                    }
                    semJObject["properties"] = JToken.FromObject(propertiesJObject);
                }
                if (dsElement.IdShort == "required")
                {
                    List<string> requiredList = new List<string>();
                    foreach (Aas.Qualifier _requiredQual in dsElement.Qualifiers)
                    {
                        requiredList.Add(_requiredQual.Value);
                    }
                    semJObject["required"] = JToken.FromObject(requiredList);
                }
            }
            return semJObject;
        }

        public static List<JToken> enumELement(List<Aas.IQualifier> qualCollection)
        {

            List<JToken> enums = new List<JToken>();
            foreach (Aas.Qualifier _enumQual in qualCollection)
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
        public static JObject createDataSchema(Aas.ISubmodelElement sem)
        {
            JObject semJObject = new JObject();
            Aas.SubmodelElementCollection _tempCollection = (Aas.SubmodelElementCollection)sem.Copy();
            string dschemaType = "";
            foreach (Aas.ISubmodelElement _tempChild in _tempCollection.EnumerateChildren())
            {
                Aas.ISubmodelElement dsElement = _tempChild;
                if (dsElement.IdShort == "titles")
                {
                    JObject _titlesJObject = new JObject();
                    Aas.MultiLanguageProperty mlp = (Aas.MultiLanguageProperty)dsElement.Copy();
                    var _titles = mlp.Value.Copy();
                    foreach (Aas.ILangStringTextType _title in _titles)
                    {
                        _titlesJObject[_title.Language] = _title.Text;
                    }
                    semJObject["titles"] = _titlesJObject;
                }
                if (dsElement.IdShort == "oneOf")
                {
                    List<JObject> oneOfJObjects = new List<JObject>();
                    Aas.SubmodelElementCollection _enumCOllection =
                        (Aas.SubmodelElementCollection)dsElement.Copy();
                    foreach (Aas.ISubmodelElement _temponeOf in _enumCOllection.EnumerateChildren())
                    {
                        Aas.ISubmodelElement _oneOf = _temponeOf;
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
                List<Aas.ILangStringTextType> tdDescription = sem.Description;
                if (tdDescription.Count != 1)
                {
                    semJObject["description"] = tdDescription[0].Text;
                    int index = 1;
                    JObject descriptions = new JObject();
                    for (index = 1; index < tdDescription.Count; index++)
                    {
                        Aas.ILangStringTextType desc = tdDescription[index];
                        descriptions[desc.Language] = desc.Text;
                    }
                    semJObject["descriptions"] = JToken.FromObject(descriptions);
                }
                else
                {
                    semJObject["description"] = tdDescription[0].Text;
                }
            }
            foreach (Aas.Qualifier smQualifier in sem.Qualifiers)
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
                    foreach (Aas.Qualifier semQual in sem.Qualifiers)
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
                    foreach (Aas.Qualifier semQual in sem.Qualifiers)
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
        public static JObject createInteractionAvoidance(Aas.ISubmodelElement sem)
        {
            JObject semJObject = createDataSchema(sem);
            Aas.SubmodelElementCollection _tempCollection = (Aas.SubmodelElementCollection)sem.Copy();
            foreach (Aas.ISubmodelElement _tempChild in _tempCollection.EnumerateChildren())
            {
                Aas.ISubmodelElement dsElement = _tempChild;
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
        public static JObject createTDProperties(Aas.ISubmodelElement propertiesSem)
        {
            JObject propertiesJObject = new JObject();
            Aas.SubmodelElementCollection _tempCollection =
                (Aas.SubmodelElementCollection)propertiesSem.Copy();
            foreach (Aas.ISubmodelElement _tempProperty in _tempCollection.EnumerateChildren())
            {
                Aas.ISubmodelElement _propoerty = _tempProperty;
                JObject propetyJObject = createInteractionAvoidance(_propoerty);
                if (propetyJObject.ContainsKey("observable"))
                {
                    propetyJObject["observable"] = Convert.ToBoolean(propetyJObject["observable"]);
                }
                propertiesJObject[_propoerty.IdShort] = propetyJObject;
            }
            return propertiesJObject;
        }
        public static JObject createTDActions(Aas.ISubmodelElement actionsSem)
        {
            JObject actionsJObject = new JObject();
            Aas.SubmodelElementCollection _tempCollection = (Aas.SubmodelElementCollection)actionsSem.Copy();
            foreach (Aas.ISubmodelElement _tempAction in _tempCollection.EnumerateChildren())
            {
                Aas.ISubmodelElement _action = _tempAction;
                JObject actionJObject = createInteractionAvoidance(_action);
                Aas.SubmodelElementCollection _actionItems = (Aas.SubmodelElementCollection)_action.Copy();
                foreach (Aas.ISubmodelElement _tempActionItem in _actionItems.EnumerateChildren())
                {
                    Aas.ISubmodelElement _actionItem = _tempActionItem;
                    if (_actionItem.IdShort == "input")
                    {
                        actionJObject["input"] = JToken.FromObject(createDataSchema(_actionItem));
                    }
                    if (_actionItem.IdShort == "output")
                    {
                        actionJObject["output"] = JToken.FromObject(createDataSchema(_actionItem));
                    }
                }
                foreach (Aas.Qualifier actionQual in _action.Qualifiers)
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
        public static JObject createTDEvents(Aas.ISubmodelElement eventsSem)
        {
            JObject eventsJObject = new JObject();
            Aas.SubmodelElementCollection _tempCollection =
                (Aas.SubmodelElementCollection)eventsSem.Copy();
            foreach (Aas.ISubmodelElement _tempEvent in _tempCollection.EnumerateChildren())
            {
                Aas.ISubmodelElement _event = _tempEvent;
                JObject actionJObject = createInteractionAvoidance(_event);
                Aas.SubmodelElementCollection _eventItems =
                    (Aas.SubmodelElementCollection)_event.Copy();
                foreach (Aas.ISubmodelElement _tempEventItem in _eventItems.EnumerateChildren())
                {
                    Aas.ISubmodelElement _eventItem = _tempEventItem;
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
        public static JObject createTDLinks(Aas.ISubmodelElement linksSem)
        {
            List<JObject> links = new List<JObject>();
            Aas.SubmodelElementCollection _tempCollection = (Aas.SubmodelElementCollection)linksSem.Copy();
            foreach (Aas.ISubmodelElement _tempLink in _tempCollection.EnumerateChildren())
            {
                Aas.ISubmodelElement link = _tempLink;
                JObject jObject = new JObject();
                foreach (Aas.Qualifier linkItem in link.Qualifiers)
                {
                    jObject[linkItem.Type] = linkItem.Value;
                }
                links.Add(jObject);
            }
            JObject linksJObject = new JObject();
            linksJObject["links"] = JToken.FromObject(links);
            return linksJObject;
        }
        public static JObject createTDSecurity(Aas.ISubmodelElement securitySem)
        {
            Aas.SubmodelElementCollection _tempCollection =
                (Aas.SubmodelElementCollection)securitySem.Copy();
            List<string> securityList = new List<string>();
            foreach (Aas.Qualifier _security in _tempCollection.Qualifiers)
            {
                securityList.Add(_security.Value);
            }
            JObject securityJObject = new JObject();
            securityJObject["security"] = JToken.FromObject(securityList);
            return securityJObject;
        }

        public static JObject createSecurityScheme(Aas.ISubmodelElement sschemaSem)
        {
            JObject sschemaJOBject = new JObject();
            foreach (Aas.Qualifier smQualifier in sschemaSem.Qualifiers)
            {
                sschemaJOBject[smQualifier.Type] = smQualifier.Value;
            }
            if (sschemaSem.Description != null)
            {
                List<Aas.ILangStringTextType> tdDescription = sschemaSem.Description;
                if (tdDescription.Count != 1)
                {
                    sschemaJOBject["description"] = tdDescription[0].Text;
                    int index = 1;
                    JObject descriptions = new JObject();
                    for (index = 1; index < tdDescription.Count; index++)
                    {
                        Aas.ILangStringTextType desc = tdDescription[index];
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
        public static JObject createTDSecurityDefinitions(Aas.ISubmodelElement sdSem)
        {
            JObject securityDefinitionsJObject = new JObject();
            Aas.SubmodelElementCollection _tempCollection = (Aas.SubmodelElementCollection)sdSem.Copy();
            foreach (Aas.ISubmodelElement _tempSD in _tempCollection.EnumerateChildren())
            {
                Aas.ISubmodelElement _securityDefinition = _tempSD;
                JObject securityJObject = createSecurityScheme(_securityDefinition);
                Aas.SubmodelElementCollection _securityDItems =
                    (Aas.SubmodelElementCollection)_securityDefinition.Copy();
                foreach (var temp in (JToken)securityJObject)
                {
                    JProperty secObject = (JProperty)temp;
                    string key = secObject.Name.ToString();
                    if (key == "scheme")
                    {
                        string securityScheme = (secObject.Value).ToString();
                        if (securityScheme == "combo")
                        {
                            foreach (Aas.ISubmodelElement _temp_combosecurityDItems in
                                _securityDItems.EnumerateChildren())
                            {
                                Aas.SubmodelElementCollection csdItem =
                                    (Aas.SubmodelElementCollection)_temp_combosecurityDItems.Copy();
                                List<string> csdItemList = new List<string>();
                                foreach (Aas.Qualifier _csdQual in csdItem.Qualifiers)
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
                            foreach (Aas.ISubmodelElement _temp_combosecurityDItems in
                                _securityDItems.EnumerateChildren())
                            {
                                Aas.SubmodelElementCollection oauth2SDItem =
                                    (Aas.SubmodelElementCollection)_temp_combosecurityDItems.Copy();
                                List<string> csdItemList = new List<string>();
                                foreach (Aas.Qualifier _csdQual in oauth2SDItem.Qualifiers)
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
        public static JObject createTDProfile(Aas.ISubmodelElement profileSem)
        {
            JObject profileJObject = new JObject();
            Aas.SubmodelElementCollection _tempCollection =
                (Aas.SubmodelElementCollection)profileSem.Copy();
            List<string> profileList = new List<string>();
            foreach (Aas.Qualifier _profileQual in _tempCollection.Qualifiers)
            {
                profileList.Add(_profileQual.Value);
            }
            profileJObject["profile"] = JToken.FromObject(profileList);
            return profileJObject;
        }
        public static JObject createTDSchemaDefinitions(Aas.ISubmodelElement sem)
        {
            JObject semJObject = new JObject();
            return semJObject;
        }

        public static JObject ExportSMtoJson(Aas.ISubmodel sm)
        {
            JObject exportData = new JObject();
            try
            {
                JObject TDJson = new JObject();
                if (sm.Qualifiers != null)
                {
                    foreach (Aas.Qualifier smQualifier in sm.Qualifiers)
                    {
                        TDJson[smQualifier.Type] = smQualifier.Value.ToString();
                    }
                }

                // description
                if (sm.Description != null)
                {
                    List<Aas.ILangStringTextType> tdDescription = sm.Description;
                    if (tdDescription.Count != 1)
                    {
                        TDJson["description"] = tdDescription[0].Text;
                        int index = 1;
                        JObject descriptions = new JObject();
                        for (index = 1; index < tdDescription.Count; index++)
                        {
                            Aas.ILangStringTextType desc = tdDescription[index];
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
                    Aas.IAdministrativeInformation adm = sm.Administration;
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
                    foreach (Aas.ISubmodelElement tdElementWrapper in sm.SubmodelElements)
                    {
                        Aas.ISubmodelElement tdElement = tdElementWrapper;
                        if (tdElement.IdShort == "@type")
                        {
                            List<object> typeList = new List<object>();
                            foreach (Aas.Qualifier _typeQual in tdElement.Qualifiers)
                            {
                                typeList.Add((_typeQual.Value));

                            }
                            TDJson["@type"] = JToken.FromObject(typeList);
                        }
                        if (tdElement.IdShort == "titles")
                        {
                            JObject _titlesJObject = new JObject();
                            Aas.MultiLanguageProperty mlp = (Aas.MultiLanguageProperty)tdElement.Copy();
                            var _titles = mlp.Value.Copy();
                            foreach (Aas.ILangStringTextType _title in _titles)
                            {
                                _titlesJObject[_title.Language] = _title.Text;
                            }
                            TDJson["titles"] = _titlesJObject;
                        }
                        if (tdElement.IdShort == "@context")
                        {
                            List<object> contextList = new List<object>();
                            JObject _conSemantic = new JObject();
                            foreach (Aas.Qualifier _con in tdElement.Qualifiers)
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