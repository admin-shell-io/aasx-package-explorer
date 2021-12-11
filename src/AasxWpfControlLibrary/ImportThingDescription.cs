/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
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
    public static class TDJsonImport
    {
        public static bool ValidateTDJson(JObject tdJObject)
        {
            //JsonSchema tdSchema = JsonSchema.Parse(File.ReadAllText(@"td-json-schema-validation.json"));
            //bool idValid = tdJObject.IsValid(tdSchema);
            return false;
        }

        // TD DataSchema Sub classes
        public static AdminShell.SubmodelElementCollection BuildArraySchema(AdminShell.SubmodelElementCollection dsCollection, JToken arrayJObject)
        {
            foreach (var temp in arrayJObject)
            {
                JProperty arrayELement = (JProperty)temp;
                string key = arrayELement.Name.ToString();
                if (key == ("minItems"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("minItems", arrayJObject["minItems"].ToString()));
                }
                if (key == ("maxItems"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("maxItems", arrayJObject["minItems"].ToString()));
                }
                if (key == ("items"))
                {
                    AdminShell.SubmodelElementCollection items = new AdminShell.SubmodelElementCollection();
                    if ((arrayJObject["items"].Type).ToString() == "Array")
                    {
                        int i = 0;
                        foreach (var x in arrayJObject["items"])
                        {
                            string jProperty = x.ToString();
                            JObject _jObject = JObject.Parse(jProperty);
                            AdminShell.SubmodelElementCollection _item = BuildAbstractDataSchema(_jObject, "item" + (i).ToString(),"item");
                            i = i + 1;
                            _item.semanticId = createSemanticID("item");
                            items.Add(_item);
                        }
                    }
                    else
                    {
                        string jItem = (arrayJObject["items"]).ToString();
                        JObject _jObject = JObject.Parse(jItem);
                        AdminShell.SubmodelElementCollection _item = BuildAbstractDataSchema(_jObject, "item1","item");
                        _item.semanticId = createSemanticID("item");
                        items.Add(_item);
                    }
                    items.idShort = "items";
                    items.semanticId = createSemanticID("item");
                    dsCollection.Add(items);
                }
            }


            return dsCollection;
        }
        public static AdminShell.SubmodelElementCollection BuildNumberSchema(AdminShell.SubmodelElementCollection dsCollection, JToken numberJObject)
        {
            foreach (var temp in numberJObject)
            {
                JProperty numberElement = (JProperty)temp;
                string key = numberElement.Name.ToString();
                if (key == ("minimum"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("minimum", numberJObject["minimum"].ToString()));
                }
                if (key == ("exclusiveMinimum"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("exclusiveMinimum", numberJObject["exclusiveMinimum"].ToString()));
                }
                if (key == ("maximum"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("maximum", numberJObject["maximum"].ToString()));
                }
                if (key == ("exclusiveMaximum"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("exclusiveMaximum", numberJObject["exclusiveMaximum"].ToString()));
                }
                if (key == ("multipleOf"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("multipleOf", numberJObject["multipleOf"].ToString()));
                }
            }
            return dsCollection;
        }
        public static AdminShell.SubmodelElementCollection BuildIntegerSchema(AdminShell.SubmodelElementCollection dsCollection, JToken interJObject)
        {
            foreach (var temp  in interJObject)
            {
                JProperty integerSElement = (JProperty)temp;
                string key = integerSElement.Name.ToString();
                if (key == ("minimum"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("minimum", interJObject["minimum"].ToString()));
                }
                if (key == ("exclusiveMinimum"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("exclusiveMinimum", interJObject["exclusiveMinimum"].ToString()));
                }
                if (key == ("maximum"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("maximum", interJObject["maximum"].ToString()));
                }
                if (key == ("exclusiveMaximum"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("exclusiveMaximum", interJObject["exclusiveMaximum"].ToString()));
                }
                if (key == ("multipleOf"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("multipleOf", interJObject["multipleOf"].ToString()));
                }
            }
            return dsCollection;
        }
        public static AdminShell.SubmodelElementCollection BuildStringSchema(AdminShell.SubmodelElementCollection dsCollection, JToken stringjObject)
        {
            foreach (var temp in stringjObject)
            {
                JProperty stringElement = (JProperty)temp;
                string key = stringElement.Name.ToString();
                if (key == ("minLength"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("minLength", stringjObject["minLength"].ToString()));
                }
                if (key == ("maxLength"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("maxLength", stringjObject["maxLength"].ToString()));
                }
                if (key == ("pattern"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("pattern", stringjObject["pattern"].ToString()));
                }
                if (key == ("contentEncoding"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("contentEncoding", stringjObject["contentEncoding"].ToString()));
                }
                if (key == ("contentMediaType"))
                {
                    dsCollection.qualifiers.Add(createAASQualifier("contentMediaType", stringjObject["contentMediaType"].ToString()));
                }
            }

            return dsCollection;
        }
        public static AdminShell.SubmodelElementCollection BuildObjectSchema(AdminShell.SubmodelElementCollection dsCollection, JToken objectjObject)
        {
            foreach (var temp in objectjObject)
            {
                JProperty objectElement = (JProperty)temp;
                string key = objectElement.Name.ToString();
                if (key == ("required"))
                {
                    AdminShell.SubmodelElementCollection requireds = new AdminShell.SubmodelElementCollection();
                    requireds.idShort = "required";
                    requireds.AddDescription("en", "Defines which members of the object type are mandatory.");
                    requireds.qualifiers = new AdminShell.QualifierCollection();
                    int i = 1;
                    foreach (var x in objectjObject["required"])
                    {
                        AdminShell.Qualifier _required = createAASQualifier("required", x.ToString());
                        _required.type = "required" + i.ToString();
                        requireds.qualifiers.Add(_required);
                        i = i + 1;
                    }
                    dsCollection.Add(requireds);
                }
                if (key == ("properties"))
                {
                    AdminShell.SubmodelElementCollection _properties = new AdminShell.SubmodelElementCollection();
                    _properties.idShort = "properties";
                    _properties.category = "PARAMETER";
                    _properties.ordered = false;
                    _properties.allowDuplicates = false;
                    _properties.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                    _properties.semanticId = createSemanticID("properties");
                    _properties.qualifiers = new AdminShell.QualifierCollection();
                    foreach (var temp1 in objectjObject["properties"])
                    {
                        JProperty x = (JProperty)temp1;
                        JObject _propertyJobject = JObject.FromObject(x.Value);
                        AdminShell.SubmodelElementCollection _propertyC = BuildAbstractDataSchema(_propertyJobject, x.Name.ToString(),"property");
                        _propertyC.semanticId = createSemanticID("property");
                        _properties.Add(_propertyC);
                    }
                    dsCollection.Add(_properties);
                }
            }


            return dsCollection;
        }

        // AAS SubmodelMultiLanguage Property
        public static AdminShell.MultiLanguageProperty BuildMultiLanguageProperty(string idShort, List<AdminShellV20.LangStr> texts, string description)
        {
            AdminShell.MultiLanguageProperty _multiLanguageProperty = new AdminShell.MultiLanguageProperty();
            _multiLanguageProperty.idShort = idShort;
            _multiLanguageProperty.category = "PARAMETER";
            _multiLanguageProperty.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            foreach (var text in texts)
            {
                _multiLanguageProperty.value.langString.Add(text);
            }
            _multiLanguageProperty.AddDescription("en", description);
            return _multiLanguageProperty;
        }

        // TD Forms
        public static AdminShell.SubmodelElementCollection BuildAdditionalResponse(JObject jobject, string idshort)
        {
            AdminShell.SubmodelElementCollection arCollection = new AdminShell.SubmodelElementCollection();
            arCollection.idShort = idshort;
            arCollection.category = "PARAMETER";
            arCollection.ordered = false;
            arCollection.allowDuplicates = false;
            arCollection.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            arCollection.AddDescription("en", "Communication metadata describing the expected response message for additional responses.");
            arCollection.qualifiers = new AdminShell.QualifierCollection();
            if (jobject.ContainsKey("success"))
            {
                arCollection.AddQualifier("success", jobject["success"].ToString());
            }
            if (jobject.ContainsKey("contentType"))
            {
                arCollection.AddQualifier("contentTypeschema", jobject["contentType"].ToString());
            }
            if (jobject.ContainsKey("schema"))
            {
                arCollection.AddQualifier("schema", jobject["schema"].ToString());
            }
            return arCollection;
        }

        // ConstructCommon Elements
        public static AdminShell.SubmodelElementCollection BuildCommonELement(AdminShell.SubmodelElementCollection thingCollection, JObject jObject)
        {
            foreach (var tempThing in (JToken)jObject)
            {
                JProperty thingE = (JProperty)tempThing;
                string key = thingE.Name.ToString();
                if (key == "title")
                {
                    thingCollection.qualifiers.Add(createAASQualifier("title", thingE.Value.ToString()));
                }
                if (key == "description")
                {
                    thingCollection.AddDescription("en", thingE.Value.ToString());
                }
                if (key == "descriptions")
                {
                    foreach (var temp in thingE.Value)
                    {
                        JProperty x = (JProperty)temp;
                        thingCollection.AddDescription((x.Name).ToString(), (x.Value).ToString());
                    }
                }
                if (key == "titles")
                {
                    List<AdminShellV20.LangStr> titleList = new List<AdminShellV20.LangStr>();
                    foreach (var temp in thingE.Value)
                    {
                        JProperty x = (JProperty)temp;
                        AdminShellV20.LangStr title = new AdminShellV20.LangStr((x.Name).ToString(), (x.Value).ToString());
                        titleList.Add(title);
                    }
                    AdminShell.MultiLanguageProperty mlp = BuildMultiLanguageProperty(key, titleList, "Provides multi-language human-readable titles (e.g., display a text for UI representation in different languages)");
                    thingCollection.Add(mlp);
                }
            }

            return thingCollection;
        }

        // TD DataSchema
        public static AdminShell.SubmodelElementCollection BuildAbstractDataSchema(JObject dsjObject, string idShort,string type)
        {
            AdminShell.SubmodelElementCollection abstractDS = new AdminShell.SubmodelElementCollection();
            abstractDS.idShort = idShort;
            abstractDS.category = "PARAMETER";
            abstractDS.ordered = false;
            abstractDS.allowDuplicates = false;
            abstractDS.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            abstractDS.qualifiers = new AdminShell.QualifierCollection();
            abstractDS.semanticId = createSemanticID(type);
            string[] qualList = { "const","default",
                                          "unit", "readOnly", "writeOnly","format","@type" };
            string[] dsArrayList = { "enum", "@type" };
            abstractDS = BuildCommonELement(abstractDS, dsjObject);
            foreach (var temp in (JToken)dsjObject)
            {
                JProperty dsELement = (JProperty) temp;
                string key = dsELement.Name.ToString();
                if ((dsELement.Value.Type).ToString() == "Array")
                {

                    if (dsArrayList.Contains(key))
                    {
                        AdminShell.SubmodelElementCollection arrayCollection = new AdminShell.SubmodelElementCollection();
                        arrayCollection.idShort = key;
                        arrayCollection.category = "PARAMETER";
                        arrayCollection.ordered = false;
                        arrayCollection.allowDuplicates = false;
                        arrayCollection.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                        arrayCollection.AddDescription("en", TDSemanticId.getarrayListDesc(key));
                        arrayCollection.qualifiers = new AdminShell.QualifierCollection();
                        int index = 1;
                        foreach (var x in dsELement.Value)
                        {
                            AdminShell.Qualifier _arrayCQual = createAASQualifier(key + index.ToString(), (x).ToString());
                            arrayCollection.qualifiers.Add(_arrayCQual);
                            index = index + 1;
                        }
                        abstractDS.Add(arrayCollection);

                    }
                }
                if ((dsELement.Value.Type).ToString() == "String")
                {
                    if (qualList.Contains(key))
                    {
                        abstractDS.qualifiers.Add(createAASQualifier(key, dsELement.Value.ToString()));
                    }
                }
                if (key == "oneOf")
                {
                    AdminShell.SubmodelElementCollection oneOf = new AdminShell.SubmodelElementCollection();
                    oneOf.idShort = "oneOf";
                    oneOf.category = "PARAMETER";
                    oneOf.ordered = false;
                    oneOf.allowDuplicates = false;
                    oneOf.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                    oneOf.AddDescription("en", "Used to ensure that the data is valid against one of the specified schemas in the array.");
                    oneOf.qualifiers = new AdminShell.QualifierCollection();
                    int i = 0;
                    foreach (var ds in dsjObject["oneOf"])
                    {
                        i = i + 1;
                        oneOf.Add(BuildAbstractDataSchema(JObject.FromObject(ds), "oneOf" + i.ToString(), "oneOf"));
                    }
                    abstractDS.Add(oneOf);
                }
                if (key == "type")
                {
                    abstractDS.qualifiers.Add(createAASQualifier("type", dsjObject["type"].ToString()));
                    string dsType = dsjObject["type"].ToString();
                    if (dsType == "array")
                    {
                        abstractDS = BuildArraySchema(abstractDS, dsjObject);
                    }
                    if (dsType == "number")
                    {
                        abstractDS = BuildNumberSchema(abstractDS, dsjObject);
                    }
                    if (dsType == "integer")
                    {
                        abstractDS = BuildIntegerSchema(abstractDS, dsjObject);
                    }
                    if (dsType == "string")
                    {
                        abstractDS = BuildStringSchema(abstractDS, dsjObject);
                    }
                    if (dsType == "object")
                    {
                        abstractDS = BuildObjectSchema(abstractDS, dsjObject);
                    }
                }
            }
            return abstractDS;
        }

        // TD Interaction Avoidance
        public static AdminShell.SubmodelElementCollection BuildAbstractInteractionAvoidance(JObject jObject, string idShort,string type)
        {
            AdminShell.SubmodelElementCollection _interactionAffordance = BuildAbstractDataSchema(jObject, idShort, type);
            if (jObject.ContainsKey("uriVariables"))
            {
                AdminShell.SubmodelElementCollection _uriVariables = new AdminShell.SubmodelElementCollection();
                _uriVariables.idShort = "uriVariables";
                _uriVariables.category = "PARAMETER";
                _uriVariables.ordered = false;
                _uriVariables.allowDuplicates = false;
                _uriVariables.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                _uriVariables.semanticId = createSemanticID("uriVariables");
                _uriVariables.qualifiers = new AdminShell.QualifierCollection();
                foreach (var temp in jObject["uriVariables"])
                {
                    JProperty uriVarObject = (JProperty)temp;
                    string key = uriVarObject.Name.ToString();
                    _uriVariables.Add(BuildAbstractDataSchema(JObject.FromObject(uriVarObject.Value), key, "uriVariables"));
                }
                _interactionAffordance.Add(_uriVariables);
            }
            if (jObject.ContainsKey("forms"))
            {
                _interactionAffordance.Add(BuildForms(jObject));
            }
            return _interactionAffordance;
        }

        // TD Properties
        public static AdminShell.SubmodelElementCollection BuildTDProperty(JObject _propertyJObject, string propertyName)
        {
            AdminShell.SubmodelElementCollection _tdProperty = BuildAbstractInteractionAvoidance(_propertyJObject, propertyName,"property");
            _tdProperty.semanticId = createSemanticID("property");
            _tdProperty.qualifiers = new AdminShell.QualifierCollection();
            if (_propertyJObject.ContainsKey("observable"))
            {
                _tdProperty.qualifiers.Add(createAASQualifier("observable", (_propertyJObject["observable"]).ToString()));
            }
            if (_propertyJObject.ContainsKey("updateFrequencey"))
            {
                _tdProperty.qualifiers.Add(createAASQualifier("updateFrequencey", (_propertyJObject["updateFrequencey"]).ToString()));
            }
            if (_propertyJObject.ContainsKey("updatable"))
            {
                _tdProperty.qualifiers.Add(createAASQualifier("updatable", (_propertyJObject["updatable"]).ToString()));
            }
            return _tdProperty;
        }
        public static AdminShell.SubmodelElementCollection BuildTDProperties(JObject tdObject)
        {
            AdminShell.SubmodelElementCollection tdProperties = new AdminShell.SubmodelElementCollection();
            tdProperties.idShort = "properties";
            tdProperties.category = "PARAMETER";
            tdProperties.ordered = false;
            tdProperties.allowDuplicates = false;
            tdProperties.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            tdProperties.AddDescription("en", "Properties definion of the thing Description");
            tdProperties.semanticId = createSemanticID("properties");
            foreach (var temp in tdObject["properties"])
            {
                JProperty propertyObject = (JProperty)temp;
                tdProperties.Add(BuildTDProperty(JObject.FromObject(propertyObject.Value), (propertyObject.Name).ToString()));
            }
            return tdProperties;
        }

        // TD Events
        public static AdminShell.SubmodelElementCollection BuildTDEvent(JObject _eventJObject, string actionName)
        {
            AdminShell.SubmodelElementCollection _tdEvent = BuildAbstractInteractionAvoidance(_eventJObject, actionName,"event");
            _tdEvent.semanticId = createSemanticID("event");
            string[] dsList = { "subscription", "data", "cancellation" };
            foreach (var temp in (JToken)_eventJObject)
            {
                JProperty eventElement = (JProperty)temp;
                string key = eventElement.Name.ToString();
                if (dsList.Contains(key))
                {
                    _tdEvent.Add(BuildAbstractDataSchema(JObject.FromObject(_eventJObject[key]),key,key));
                }
            }
            return _tdEvent;
        }
        public static AdminShell.SubmodelElementCollection BuildTDEvents(JObject jObject)
        {
            AdminShell.SubmodelElementCollection tdEvents = new AdminShell.SubmodelElementCollection();
            tdEvents.idShort = "events";
            tdEvents.category = "PARAMETER";
            tdEvents.ordered = false;
            tdEvents.allowDuplicates = false;
            tdEvents.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            tdEvents.AddDescription("en", "All Event-based Interaction Affordances of the Thing.");
            tdEvents.semanticId = createSemanticID("events");
            tdEvents.qualifiers = new AdminShell.QualifierCollection();
            foreach (var temp in jObject["events"])
            {
                JProperty eventObject = (JProperty)temp;
                tdEvents.Add(BuildTDEvent(JObject.FromObject(eventObject.Value), (eventObject.Name).ToString()));
            }
            return tdEvents;
        }

        // TD Actions
        public static AdminShell.SubmodelElementCollection BuildTDActions(JObject jObject)
        {
            AdminShell.SubmodelElementCollection tdActions = new AdminShell.SubmodelElementCollection();
            tdActions.idShort = "actions";
            tdActions.category = "PARAMETER";
            tdActions.ordered = false;
            tdActions.allowDuplicates = false;
            tdActions.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            tdActions.AddDescription("en", "All Action-based Interaction Affordances of the Thing.");
            tdActions.semanticId = createSemanticID("actions");
            foreach (var temp in jObject["actions"])
            {
                JProperty actionObject = (JProperty)temp;
                tdActions.Add(BuildTDAction(JObject.FromObject(actionObject.Value), (actionObject.Name).ToString()));
            }
            return tdActions;
        }
        public static AdminShell.SubmodelElementCollection BuildTDAction(JObject _actionJObject, string actionName)
        {
            AdminShell.SubmodelElementCollection _tdAction = BuildAbstractInteractionAvoidance(_actionJObject, actionName,"action");
            _tdAction.semanticId = createSemanticID("action");
            _tdAction.qualifiers = new AdminShell.QualifierCollection();
            string[] dsList = {"input","output"};
            string[] qualList = { "safe", "idempotent" };
            foreach (var temp in (JToken)_actionJObject)
            {
                JProperty actionElement = (JProperty)temp;
                string key = actionElement.Name.ToString();
                if (dsList.Contains(key))
                {
                    _tdAction.Add(BuildAbstractDataSchema(JObject.FromObject(_actionJObject[key]), key, key));
                }
                if (qualList.Contains(key))
                {
                    _tdAction.qualifiers.Add(createAASQualifier(key, _actionJObject[key].ToString()));
                }
            }
            return _tdAction;
        }

        // TD Links
        public static AdminShell.SubmodelElementCollection BuildTDLink(JObject linkJObject, string idShort)
        {
            AdminShell.SubmodelElementCollection _tdLink = new AdminShell.SubmodelElementCollection();
            _tdLink.idShort = idShort;
            _tdLink.category = "PARAMETER";
            _tdLink.ordered = false;
            _tdLink.allowDuplicates = false;
            _tdLink.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            _tdLink.AddDescription("en", "A link can be viewed as a statement of the form link context has a relation type resource at link target, where the optional target attributes may further describe the resource");
            _tdLink.semanticId = createSemanticID("link");
            _tdLink.qualifiers = new AdminShell.QualifierCollection();
            string[] linkElemList = { "href", "type", "rel", "anchor", "sizes" };
            foreach (var temp in (JToken)linkJObject)
            {
                JProperty linkElement = (JProperty)temp;
                if (linkElemList.Contains(linkElement.Name))
                {
                    _tdLink.qualifiers.Add(createAASQualifier(linkElement.Name.ToString(), linkElement.Value.ToString()));
                }
            }
            return _tdLink;
        }
        public static AdminShell.SubmodelElementCollection BuildTDLinks(JObject jObject)
        {
            AdminShell.SubmodelElementCollection tdLinks = new AdminShell.SubmodelElementCollection();
            tdLinks.idShort = "links";
            tdLinks.category = "PARAMETER";
            tdLinks.ordered = false;
            tdLinks.allowDuplicates = false;
            tdLinks.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            tdLinks.AddDescription("en", "Provides Web links to arbitrary resources that relate to the specified Thing Description.");
            tdLinks.semanticId = createSemanticID("links");
            int index = 1;
            foreach (var linkObject in jObject["links"])
            {
                tdLinks.Add(BuildTDLink(JObject.FromObject(linkObject), "link" + index.ToString()));
                index = index + 1;
            }
            return tdLinks;
        }

        // TD Security Definition
        public static AdminShell.SubmodelElementCollection BuildSecurityDefinition(JObject jObject, string definitionName)
        {
            AdminShell.SubmodelElementCollection _securityDefinition = new AdminShell.SubmodelElementCollection();
            _securityDefinition.idShort = definitionName;
            _securityDefinition.category = "PARAMETER";
            _securityDefinition.ordered = false;
            _securityDefinition.allowDuplicates = false;
            _securityDefinition.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            _securityDefinition.qualifiers = new AdminShell.QualifierCollection();
            if (jObject.ContainsKey("@type")) // needs to be discussed with Mr. Sebastian. When input is an array 
                                              // requires an example
            {
                _securityDefinition.qualifiers.Add(createAASQualifier("@type", jObject["@type"].ToString()));
            }
            if (jObject.ContainsKey("description"))
            {
                _securityDefinition.AddDescription("en", jObject["description"].ToString());
            }
            if (jObject.ContainsKey("descriptions"))
            {
                foreach ( var temp in jObject["descriptions"])
                {
                    JProperty x = (JProperty)temp;
                    _securityDefinition.AddDescription((x.Name).ToString(), (x.Value).ToString());
                }
            }
            if (jObject.ContainsKey("proxy"))
            {
                _securityDefinition.qualifiers.Add(createAASQualifier("proxy", jObject["proxy"].ToString()));
            }
            if (jObject.ContainsKey("scheme"))
            {
                string scheme = jObject["scheme"].ToString();
                _securityDefinition.qualifiers.Add(createAASQualifier("scheme", scheme));
                if (scheme == "combo")
                {
                    if (jObject.ContainsKey("oneOf"))
                    {
                        if ((jObject["oneOf"].Type).ToString() == "String")
                        {
                            _securityDefinition.qualifiers.Add(createAASQualifier("oneOf", (jObject["oneOf"]).ToString()));
                        }
                        if ((jObject["oneOf"].Type).ToString() == "Array")
                        {
                            AdminShell.SubmodelElementCollection _oneOf = new AdminShell.SubmodelElementCollection();
                            _oneOf.idShort = "oneOf";
                            _oneOf.category = "PARAMETER";
                            _oneOf.ordered = false;
                            _oneOf.allowDuplicates = false;
                            _oneOf.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                            _oneOf.AddDescription("en", "	Array of two or more strings identifying other named security scheme definitions, any one of which, when satisfied, will allow access. Only one may be chosen for use.");
                            _oneOf.semanticId = createSemanticID("oneOf");
                            int index = 1;
                            _oneOf.qualifiers = new AdminShell.QualifierCollection();
                            foreach (var x in jObject["oneOf"])
                            {
                                _oneOf.qualifiers.Add(createAASQualifier("oneOf" + (index).ToString(), (x).ToString()));
                                index = index + 1;
                            }
                            _securityDefinition.Add(_oneOf);
                        }
                    }
                    if (jObject.ContainsKey("allOf"))
                    {
                        if ((jObject["allOf"].Type).ToString() == "String")
                        {
                            _securityDefinition.qualifiers.Add(createAASQualifier("allOf", (jObject["allOf"]).ToString()));
                        }
                        if ((jObject["allOf"].Type).ToString() == "Array")
                        {
                            AdminShell.SubmodelElementCollection _allOf = new AdminShell.SubmodelElementCollection();
                            _allOf.idShort = "oneOf";
                            _allOf.category = "PARAMETER";
                            _allOf.ordered = false;
                            _allOf.allowDuplicates = false;
                            _allOf.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                            _allOf.AddDescription("en", "Array of two or more strings identifying other named security scheme definitions, all of which must be satisfied for access.");
                            _allOf.semanticId = createSemanticID("allOf");
                            int index = 1;
                            _allOf.qualifiers = new AdminShell.QualifierCollection();
                            foreach (var x in jObject["allOf"])
                            {
                                _allOf.qualifiers.Add(createAASQualifier("allOf" + (index).ToString(), (x).ToString()));
                                index = index + 1;
                            }
                        }
                    }
                    _securityDefinition.semanticId = createSemanticID("combo");
                }
                if (scheme == "basic" || scheme == "apikey")
                {
                    if (jObject.ContainsKey("name"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("name", jObject["name"].ToString()));
                    }
                    if (jObject.ContainsKey("in"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("in", jObject["in"].ToString()));
                    }
                    if (scheme == "basic")
                    {
                        _securityDefinition.semanticId = createSemanticID("basic");
                    }
                    if (scheme == "apikey")
                    {
                        _securityDefinition.semanticId = createSemanticID("apikey");
                    }

                }
                if (scheme == "digest")
                {
                    if (jObject.ContainsKey("name"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("name", jObject["name"].ToString()));
                    }
                    if (jObject.ContainsKey("in"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("in", jObject["in"].ToString()));
                    }
                    if (jObject.ContainsKey("qop"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("qop", jObject["qop"].ToString()));
                    }
                    _securityDefinition.semanticId = createSemanticID("digest");
                }
                if (scheme == "bearer")
                {
                    if (jObject.ContainsKey("name"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("name", jObject["name"].ToString()));
                    }
                    if (jObject.ContainsKey("in"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("in", jObject["in"].ToString()));
                    }
                    if (jObject.ContainsKey("authorization"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("authorization", jObject["authorization"].ToString()));
                    }
                    if (jObject.ContainsKey("alg"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("alg", jObject["alg"].ToString()));
                    }
                    if (jObject.ContainsKey("format"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("format", jObject["format"].ToString()));
                    }
                    _securityDefinition.semanticId = createSemanticID("bearer");
                }
                if (scheme == "nosec")
                {
                    _securityDefinition.semanticId = createSemanticID("nosec");
                }
                if (scheme == "psk")
                {
                    if (jObject.ContainsKey("identity"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("identity", jObject["identity"].ToString()));
                    }
                    _securityDefinition.semanticId = createSemanticID("psk");
                }
                if (scheme == "oauth2")
                {
                    if (jObject.ContainsKey("authorization"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("authorization", jObject["authorization"].ToString()));
                    }
                    if (jObject.ContainsKey("token"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("token", jObject["token"].ToString()));
                    }
                    if (jObject.ContainsKey("refresh"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("refresh", jObject["refresh"].ToString()));
                    }
                    if (jObject.ContainsKey("flow"))
                    {
                        _securityDefinition.qualifiers.Add(createAASQualifier("flow", jObject["flow"].ToString()));
                    }
                    if (jObject.ContainsKey("scopes"))
                    {
                        if ((jObject["scopes"].Type).ToString() == "String")
                        {
                            _securityDefinition.qualifiers.Add(createAASQualifier("scopes", (jObject["scopes"]).ToString()));
                        }
                        if ((jObject["scopes"].Type).ToString() == "Array")
                        {
                            AdminShell.SubmodelElementCollection _scopes = new AdminShell.SubmodelElementCollection();
                            _scopes.idShort = "scopes";
                            _scopes.category = "PARAMETER";
                            _scopes.ordered = false;
                            _scopes.allowDuplicates = false;
                            _scopes.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                            _scopes.AddDescription("en", "Set of authorization scope identifiers provided as an array. These are provided in tokens returned by an authorization server and associated with forms in order to identify what resources a client may access and how. The values associated with a form should be chosen from those defined in an OAuth2SecurityScheme active on that form.");
                            _scopes.semanticId = createSemanticID("scopes");
                            _scopes.qualifiers = new AdminShell.QualifierCollection();
                            int index = 1;
                            foreach (var x in jObject["scopes"])
                            {
                                _scopes.qualifiers.Add(createAASQualifier("scopes" + (index).ToString(), (x).ToString()));
                                index = index + 1;
                            }
                            _securityDefinition.Add(_scopes);
                        }
                    }
                    _securityDefinition.semanticId = createSemanticID("oauth2");
                }
            }
            return _securityDefinition;
        }
        public static AdminShell.SubmodelElementCollection BuildTDSecurityDefinitions(JObject jObject)
        {
 
            AdminShell.SubmodelElementCollection _securityDefinitions = new AdminShell.SubmodelElementCollection();
            _securityDefinitions.idShort = "securityDefinitions";
            _securityDefinitions.category = "PARAMETER";
            _securityDefinitions.ordered = false;
            _securityDefinitions.allowDuplicates = false;
            _securityDefinitions.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            _securityDefinitions.AddDescription("en", "Set of named security configurations (definitions only). Not actually applied unless names are used in a security name-value pair.");
            _securityDefinitions.semanticId = createSemanticID("securityDefinitions");
            _securityDefinitions.qualifiers = new AdminShell.QualifierCollection();
            foreach (var temp in jObject["securityDefinitions"])
            {
                JProperty x1 = (JProperty)temp;
                JObject _jObject = JObject.Parse((x1.Value).ToString());
                _securityDefinitions.Add(BuildSecurityDefinition(_jObject, (x1.Name).ToString()));
            }
            return _securityDefinitions;
        }

        // TD Forms
        public static AdminShell.SubmodelElementCollection BuildTDForm(JObject formjObject, string idShort)
        {
            AdminShell.SubmodelElementCollection tdForm = new AdminShell.SubmodelElementCollection();
            tdForm.idShort = idShort;
            tdForm.category = "PARAMETER";
            tdForm.ordered = false;
            tdForm.allowDuplicates = false;
            tdForm.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            tdForm.AddDescription("en", "Hypermedia controls that describe how an operation can be performed. Form is a  serializations of Protocol Bindings");
            tdForm.semanticId = createSemanticID("form");
            tdForm.qualifiers = new AdminShell.QualifierCollection();
            string[] qualList = { "href", "contentType", "contentCoding", "subprotocol", "security", "scopes", "op" };
            string[] qualArrayList = { "security", "scopes","op"};

            foreach (var temp in (JToken)formjObject)
            {
                JProperty formElement = (JProperty)temp;
                string key = formElement.Name.ToString();
                if (key == "response") // Need to check 
                {
                    AdminShell.SubmodelElementCollection _response = new AdminShell.SubmodelElementCollection();
                    _response.idShort = "response";
                    _response.category = "PARAMETER";
                    _response.ordered = false;
                    _response.allowDuplicates = false;
                    _response.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                    _response.AddDescription("en", "This optional term can be used if, e.g., the output communication metadata differ from input metadata (e.g., output contentType differ from the input contentType). The response name contains metadata that is only valid for the primary response messages.");
                    JObject contentTypeJObject = JObject.FromObject(formjObject["response"]);
                    _response.Add(BuildTDProperty(contentTypeJObject, "contentType"));
                }
                else if (key == "additionalResponses") // Need to check 
                {
                    AdminShell.SubmodelElementCollection _response = new AdminShell.SubmodelElementCollection();
                    _response.idShort = "additionalResponses";
                    _response.category = "PARAMETER";
                    _response.ordered = false;
                    _response.allowDuplicates = false;
                    _response.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                    _response.AddDescription("en", "This optional term can be used if additional expected responses are possible, e.g. for error reporting. Each additional response needs to be distinguished from others in some way (for example, by specifying a protocol-specific error code), and may also have its own data schema");
                    if ((formjObject["additionalResponses"].Type).ToString() == "String")
                    {
                        JObject aRJObject = JObject.FromObject(formjObject["additionalResponses"]);
                        _response.Add(BuildAdditionalResponse(aRJObject, "additionalResponse1"));
                    }
                    else if ((formjObject["additionalResponses"].Type).ToString() == "Array")
                    {
                        int index = 0;
                        foreach (var arJObject in formjObject["additionalResponses"])
                        {
                            index = index + 1;
                            _response.Add(BuildAdditionalResponse(JObject.FromObject(arJObject), "additionalResponse" + index.ToString()));
                        }
                    }
                    tdForm.Add(_response);
                }

                if ((formElement.Value.Type).ToString() == "String")
                {
                    if (qualList.Contains(key))
                    {
                        tdForm.qualifiers.Add(createAASQualifier(key, formElement.Value.ToString()));
                    }
                }
                if ((formElement.Value.Type).ToString() == "Array")
                {
                    if (qualArrayList.Contains(formElement.Name))
                    {
                        AdminShell.SubmodelElementCollection _arrayElement = new AdminShell.SubmodelElementCollection();
                        _arrayElement.idShort = key;
                        _arrayElement.category = "PARAMETER";
                        _arrayElement.ordered = false;
                        _arrayElement.allowDuplicates = false;
                        _arrayElement.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                        _arrayElement.AddDescription("en", TDSemanticId.getarrayListDescription(key));
                        _arrayElement.semanticId = createSemanticID(key);
                        _arrayElement.qualifiers = new AdminShell.QualifierCollection();
                        int index = 0;
                        foreach (var x in formjObject[key])
                        {
                            AdminShell.Qualifier _formQualifier = createAASQualifier(key, (x).ToString());
                            _formQualifier.type = key + index.ToString();
                            _arrayElement.qualifiers.Add(_formQualifier);

                            index = index + 1;
                        }
                        tdForm.Add(_arrayElement);
                    }
                }

            }
            return tdForm;
        }
        public static AdminShell.SubmodelElementCollection BuildForms(JObject tdJObject)
        {
            AdminShell.SubmodelElementCollection forms = new AdminShell.SubmodelElementCollection();
            forms.idShort = "forms";
            forms.category = "PARAMETER";
            forms.ordered = false;
            forms.allowDuplicates = false;
            forms.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            forms.AddDescription("en", "Set of form hypermedia controls that describe how an operation can be performed. Forms are serializations of Protocol Bindings");
            int i = 0;
            foreach (var formObject in tdJObject["forms"])
            {
                forms.Add(BuildTDForm(JObject.FromObject(formObject), "Form" + i.ToString()));
            }
            return forms;
        }

        // AAS Semantic ID
        public static AdminShell.SemanticId createSemanticID(string tdType)
        {
            AdminShell.Key tdSemanticKey = new AdminShell.Key();
            tdSemanticKey.type = "GlobalReference";
            tdSemanticKey.local = true;
            tdSemanticKey.idType = "IRI";
            tdSemanticKey.value = TDSemanticId.getSemanticID(tdType);
            AdminShell.SemanticId tdSemanticId = new AdminShell.SemanticId(tdSemanticKey);

            return tdSemanticId;
        }

        // AAS Qualifier
        public static AdminShell.Qualifier createAASQualifier(string qualifierType, string qualifierValue)
        {
            AdminShell.Qualifier aasQualifier = new AdminShell.Qualifier();
            aasQualifier.type = qualifierType;
            aasQualifier.value = qualifierValue;
            if (TDSemanticId.getSemanticID(qualifierType) != "empty")
            {
                aasQualifier.semanticId = createSemanticID(qualifierType);
            }
            return aasQualifier;
        }


        public static AdminShell.SubmodelElementCollection BuildschemaDefinitions(JObject tdJObject)
        {
            AdminShell.SubmodelElementCollection _schemaDefinitions = new AdminShell.SubmodelElementCollection();
            _schemaDefinitions.idShort = "schemaDefinitions";
            _schemaDefinitions.category = "PARAMETER";
            _schemaDefinitions.ordered = false;
            _schemaDefinitions.allowDuplicates = false;
            _schemaDefinitions.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            _schemaDefinitions.AddDescription("en", "Set of named data schemas. To be used in a schema name-value pair inside an AdditionalExpectedResponse object.");
            _schemaDefinitions.qualifiers = new AdminShell.QualifierCollection();
            _schemaDefinitions.semanticId = createSemanticID("schemaDefinitions");
            foreach (var sdKey in tdJObject["schemaDefinitions"])
            {
                JObject _schemaDefinition = new JObject(tdJObject["schemaDefinitions"][sdKey]);
                _schemaDefinitions.value.Add(BuildAbstractDataSchema(_schemaDefinition, sdKey.ToString(), "schemaDefinitions"));
            }
            return _schemaDefinitions;
        }
        public static JObject ImportTDJsontoSubModel(
            string inputFn, AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm, AdminShell.SubmodelRef smref)
        {
            JObject exportData = new JObject();
            try
            {
                JObject tdJObject;
                string text = File.ReadAllText(inputFn);
                using (var tdStringReader = new StringReader(text))
                using (var jsonTextReader = new JsonTextReader(tdStringReader) { DateParseHandling = DateParseHandling.None })
                {
                    tdJObject = JObject.FromObject(JToken.ReadFrom(jsonTextReader));
                }                
                sm.qualifiers = new AdminShell.QualifierCollection();
                foreach (var tempThing in (JToken)tdJObject)
                {
                    JProperty thingE = (JProperty)tempThing;
                    string key = thingE.Name.ToString();
                    string[] qualList = { "@context","created", 
                                          "modified", "support", "base","security","@type" ,"title"};
                    string[] tdArrayList = { "profile","security","@type"};
                    if (key == "description")
                    {
                        sm.AddDescription("en", thingE.Value.ToString());
                    }
                    if (key == "descriptions")
                    {
                        foreach (var temp in thingE.Value)
                        {
                            JProperty x = (JProperty)temp;
                            sm.AddDescription((x.Name).ToString(), (x.Value).ToString());
                        }
                    }
                    if (key == "titles")
                    {
                        List<AdminShellV20.LangStr> titleList = new List<AdminShellV20.LangStr>();
                        foreach (var temp in thingE.Value)
                        {
                            JProperty x = (JProperty)temp;
                            AdminShellV20.LangStr title = new AdminShellV20.LangStr((x.Name).ToString(), (x.Value).ToString());
                            titleList.Add(title);
                        }
                        AdminShell.MultiLanguageProperty mlp = BuildMultiLanguageProperty(key, titleList, "Provides multi-language human-readable titles (e.g., display a text for UI representation in different languages)");
                        sm.Add(mlp);
                    }

                    if (key == "id")
                    {
                        string id = thingE.Value.ToString();
                        sm.SetIdentification("IRI", id);
                        smref.First.idType = "IRI";
                        smref.First.value = id;
                    }
                    if (key == "properties")
                    {
                        sm.Add(BuildTDProperties(tdJObject));
                    }
                    if (key == "actions")
                    {
                        sm.Add(BuildTDActions(tdJObject));
                    }
                    if (key == "events")
                    {
                        sm.Add(BuildTDEvents(tdJObject));
                    }
                    if (key == "links")
                    {
                        sm.Add(BuildTDLinks(tdJObject));
                    }
                    if (key == "forms")
                    {
                        sm.Add(BuildForms(tdJObject));
                    }
                    if (key == "securityDefinitions")
                    {
                        sm.Add(BuildTDSecurityDefinitions(tdJObject));
                    }
                    if (key == "titles")
                    {
                        List<AdminShellV20.LangStr> titleList = new List<AdminShellV20.LangStr>();
                        foreach (var temp in thingE.Value)
                        {
                            JProperty x = (JProperty)temp;
                            AdminShellV20.LangStr title = new AdminShellV20.LangStr((x.Name).ToString(), (x.Value).ToString());
                            titleList.Add(title);
                        }
                        AdminShell.MultiLanguageProperty mlp = BuildMultiLanguageProperty(key, titleList, "Provides multi-language human-readable titles (e.g., display a text for UI representation in different languages)");
                        sm.Add(mlp);
                    }
                    if (key == "version")
                    {
                        JObject versionObject = JObject.FromObject(thingE.Value);
                        string intance = "";
                        string model = "";
                        if (versionObject.ContainsKey("instance"))
                        { intance = (versionObject["instance"]).ToString(); }
                        if (versionObject.ContainsKey("model"))
                        { model = (versionObject["model"]).ToString(); }
                        sm.SetAdminstration(intance, model);
                    }
                    if (key == "schemaDefinitions")
                    {
                        sm.Add(BuildschemaDefinitions(tdJObject));
                    }
                    if ((thingE.Value.Type).ToString() == "Array")
                    {
                        JObject secProfile = new JObject
                        {
                            ["profile"] = "Indicates the WoT Profile mechanisms followed by this Thing Description and the corresponding Thing implementation.",
                            ["security"] = "security definition names",
                            ["@type"] = "JSON-LD keyword to label the object with semantic tags (or types)."
                        };
                        if (key == "@context")
                        {
                            AdminShell.SubmodelElementCollection _context = new AdminShell.SubmodelElementCollection();
                            _context.idShort = key;
                            _context.category = "PARAMETER";
                            _context.ordered = false;
                            _context.allowDuplicates = false;
                            _context.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                            _context.AddDescription("en", "JSON-LD keyword to label the object with semantic tags ");
                            _context.semanticId = createSemanticID(key);
                            _context.qualifiers = new AdminShell.QualifierCollection();
                            foreach (var temp in thingE.Value)
                            {
                                JProperty listElement = (JProperty) temp;
                                if ((listElement.Type).ToString() == "String")
                                {
                                    _context.qualifiers.Add(createAASQualifier(key, listElement.ToString()));
                                }
                                else
                                {
                                    JObject _contextJobject = JObject.FromObject(listElement);
                                    foreach (var y in _contextJobject)
                                    {
                                        _context.qualifiers.Add(createAASQualifier((y.Key).ToString(), (y.Value).ToString()));
                                    }
                                }
                            }
                            sm.Add(_context);
                        }
                        if (tdArrayList.Contains(key))
                        {
                            AdminShell.SubmodelElementCollection _profile = new AdminShell.SubmodelElementCollection();
                            _profile.idShort = key;
                            _profile.category = "PARAMETER";
                            _profile.ordered = false;
                            _profile.allowDuplicates = false;
                            _profile.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                            _profile.AddDescription("en", secProfile[key].ToString());
                            _profile.qualifiers = new AdminShell.QualifierCollection();
                            int index = 1;
                            foreach (var x in thingE.Value)
                            {
                                AdminShell.Qualifier _profileQual = createAASQualifier(key, (x).ToString());
                                _profileQual.type = key + index.ToString();
                                _profile.qualifiers.Add(_profileQual);
                                index = index + 1;
                            }
                            sm.Add(_profile);

                        }
                    }
                    if ((thingE.Value.Type).ToString() == "String")
                    {
                        if (qualList.Contains(key))
                        {
                            sm.qualifiers.Add(createAASQualifier(key, thingE.Value.ToString()));
                        }
                    }

                    sm.semanticId = createSemanticID("Thing");
                }
                exportData["status"] = "Success";
                return exportData;
            }
            catch (Exception ex)
            {
                exportData["status"] = "error";
                exportData["error"] = (ex).ToString();
                return exportData;
            }



        }
    }
}
