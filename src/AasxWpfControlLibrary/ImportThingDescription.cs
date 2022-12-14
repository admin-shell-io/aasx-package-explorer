/*
Copyright (c) 2021-2022 Otto-von-Guericke-Universität Magdeburg, Lehrstuhl Integrierte Automation
harish.pakala@ovgu.de, Author: Harish Kumar Pakala

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extenstions;
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
            return false;
        }

        // TD DataSchema Sub classes
        public static SubmodelElementCollection BuildArraySchema(
                SubmodelElementCollection dsCollection, JToken arrayJObject)
        {
            foreach (var temp in arrayJObject)
            {
                JProperty arrayELement = (JProperty)temp;
                string key = arrayELement.Name.ToString();
                if (key == ("minItems"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("minItems", arrayJObject["minItems"].ToString()));
                }
                if (key == ("maxItems"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("maxItems", arrayJObject["minItems"].ToString()));
                }
                if (key == ("items"))
                {
                    SubmodelElementCollection items = new SubmodelElementCollection();
                    if ((arrayJObject["items"].Type).ToString() == "Array")
                    {
                        int i = 0;
                        foreach (var x in arrayJObject["items"])
                        {
                            string jProperty = x.ToString();
                            JObject _jObject = JObject.Parse(jProperty);
                            SubmodelElementCollection _item = BuildAbstractDataSchema
                                                                        (_jObject, "item" + (i).ToString(), "item");
                            i = i + 1;
                            _item.SemanticId = createSemanticID("item");
                            items.Add(_item);
                        }
                    }
                    else
                    {
                        string jItem = (arrayJObject["items"]).ToString();
                        JObject _jObject = JObject.Parse(jItem);
                        SubmodelElementCollection _item = BuildAbstractDataSchema(_jObject, "item1", "item");
                        _item.SemanticId = createSemanticID("item");
                        items.Add(_item);
                    }
                    items.IdShort = "items";
                    items.SemanticId = createSemanticID("item");
                    dsCollection.Add(items);
                }
            }


            return dsCollection;
        }
        public static SubmodelElementCollection BuildNumberSchema(
            SubmodelElementCollection dsCollection, JToken numberJObject)
        {
            foreach (var temp in numberJObject)
            {
                JProperty numberElement = (JProperty)temp;
                string key = numberElement.Name.ToString();
                if (key == ("minimum"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("minimum", numberJObject["minimum"].ToString()));
                }
                if (key == ("exclusiveMinimum"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("exclusiveMinimum",
                                                                    numberJObject["exclusiveMinimum"].ToString()));
                }
                if (key == ("maximum"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("maximum", numberJObject["maximum"].ToString()));
                }
                if (key == ("exclusiveMaximum"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("exclusiveMaximum",
                                                                   numberJObject["exclusiveMaximum"].ToString()));
                }
                if (key == ("multipleOf"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("multipleOf",
                                                                   numberJObject["multipleOf"].ToString()));
                }
            }
            return dsCollection;
        }
        public static SubmodelElementCollection BuildIntegerSchema(
            SubmodelElementCollection dsCollection, JToken interJObject)
        {
            foreach (var temp in interJObject)
            {
                JProperty integerSElement = (JProperty)temp;
                string key = integerSElement.Name.ToString();
                if (key == ("minimum"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("minimum", interJObject["minimum"].ToString()));
                }
                if (key == ("exclusiveMinimum"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("exclusiveMinimum",
                                                                    interJObject["exclusiveMinimum"].ToString()));
                }
                if (key == ("maximum"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("maximum", interJObject["maximum"].ToString()));
                }
                if (key == ("exclusiveMaximum"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("exclusiveMaximum",
                                                                    interJObject["exclusiveMaximum"].ToString()));
                }
                if (key == ("multipleOf"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("multipleOf",
                                                                    interJObject["multipleOf"].ToString()));
                }
            }
            return dsCollection;
        }
        public static SubmodelElementCollection BuildStringSchema(
            SubmodelElementCollection dsCollection, JToken stringjObject)
        {
            foreach (var temp in stringjObject)
            {
                JProperty stringElement = (JProperty)temp;
                string key = stringElement.Name.ToString();
                if (key == ("minLength"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("minLength",
                                                                    stringjObject["minLength"].ToString()));
                }
                if (key == ("maxLength"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("maxLength",
                                                                   stringjObject["maxLength"].ToString()));
                }
                if (key == ("pattern"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("pattern",
                                                                    stringjObject["pattern"].ToString()));
                }
                if (key == ("contentEncoding"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("contentEncoding",
                                                                    stringjObject["contentEncoding"].ToString()));
                }
                if (key == ("contentMediaType"))
                {
                    dsCollection.Qualifiers.Add(createAASQualifier("contentMediaType",
                                                                    stringjObject["contentMediaType"].ToString()));
                }
            }

            return dsCollection;
        }
        public static SubmodelElementCollection BuildObjectSchema(
            SubmodelElementCollection dsCollection, JToken objectjObject)
        {
            foreach (var temp in objectjObject)
            {
                JProperty objectElement = (JProperty)temp;
                string key = objectElement.Name.ToString();
                if (key == ("required"))
                {
                    SubmodelElementCollection requireds = new SubmodelElementCollection();
                    requireds.IdShort = "required";
                    requireds.AddDescription("en", "Defines which members of the object type are mandatory.");
                    requireds.Qualifiers = new List<Qualifier>();
                    int i = 1;
                    foreach (var x in objectjObject["required"])
                    {
                        Qualifier _required = createAASQualifier("required", x.ToString());
                        _required.Type = "required" + i.ToString();
                        requireds.Qualifiers.Add(_required);
                        i = i + 1;
                    }
                    dsCollection.Add(requireds);
                }
                if (key == ("properties"))
                {
                    SubmodelElementCollection _properties = new SubmodelElementCollection();
                    _properties.IdShort = "properties";
                    _properties.Category = "PARAMETER";
                    //_properties.ordered = false;
                    //_properties.allowDuplicates = false;
                    _properties.Kind = ModelingKind.Instance;
                    _properties.SemanticId = createSemanticID("properties");
                    _properties.Qualifiers = new List<Qualifier>();
                    foreach (var temp1 in objectjObject["properties"])
                    {
                        JProperty x = (JProperty)temp1;
                        JObject _propertyJobject = JObject.FromObject(x.Value);
                        SubmodelElementCollection _propertyC = BuildAbstractDataSchema(
                                                                    _propertyJobject, x.Name.ToString(), "property");
                        _propertyC.SemanticId = createSemanticID("property");
                        _properties.Add(_propertyC);
                    }
                    dsCollection.Add(_properties);
                }
            }


            return dsCollection;
        }

        // AAS SubmodelMultiLanguage Property
        public static MultiLanguageProperty BuildMultiLanguageProperty(
            string idShort, List<LangString> texts, string description)
        {
            MultiLanguageProperty _multiLanguageProperty = new MultiLanguageProperty();
            _multiLanguageProperty.IdShort = idShort;
            _multiLanguageProperty.Category = "PARAMETER";
            _multiLanguageProperty.Kind = ModelingKind.Instance;
            foreach (var text in texts)
            {
                _multiLanguageProperty.Value.LangStrings.Add(text);
            }
            _multiLanguageProperty.AddDescription("en", description);
            return _multiLanguageProperty;
        }

        // TD Forms
        public static SubmodelElementCollection BuildAdditionalResponse(JObject jobject, string idshort)
        {
            SubmodelElementCollection arCollection = new()
            {
                IdShort = idshort,
                Category = "PARAMETER",
                //arCollection.ordered = false;
                //arCollection.allowDuplicates = false;
                Kind = ModelingKind.Instance
            };
            arCollection.AddDescription("en", "Communication metadata describing the expected response message " +
                "                              for additional responses.");
            arCollection.Qualifiers = new List<Qualifier>();
            if (jobject.ContainsKey("success"))
            {
                arCollection.Qualifiers.Add(new Qualifier("success", DataTypeDefXsd.String, value: jobject["success"].ToString()));
            }
            if (jobject.ContainsKey("contentType"))
            {
                arCollection.Qualifiers.Add(new Qualifier("contentTypeschema", DataTypeDefXsd.String, value: jobject["contentType"].ToString()));
            }
            if (jobject.ContainsKey("schema"))
            {
                arCollection.Qualifiers.Add(new Qualifier("schema", DataTypeDefXsd.String, value: jobject["schema"].ToString()));
            }
            return arCollection;
        }

        // ConstructCommon Elements

        // TD DataSchema
        public static SubmodelElementCollection BuildAbstractDataSchema(
            JObject dsjObject, string idShort, string type)
        {
            SubmodelElementCollection abstractDS = new SubmodelElementCollection();
            abstractDS.IdShort = idShort;
            abstractDS.Category = "PARAMETER";
            //abstractDS.ordered = false;
            //abstractDS.allowDuplicates = false;
            abstractDS.Kind = ModelingKind.Instance;
            abstractDS.Qualifiers = new List<Qualifier>();
            abstractDS.SemanticId = createSemanticID(type);
            string[] qualList = { "const","default",
                                          "unit", "readOnly", "writeOnly","format","@type" };
            string[] dsArrayList = { "enum", "@type" };
            foreach (var temp in (JToken)dsjObject)
            {
                JProperty dsELement = (JProperty)temp;
                string key = dsELement.Name.ToString();
                if (key == "title")
                {
                    abstractDS.Qualifiers.Add(createAASQualifier("title", dsELement.Value.ToString()));
                }
                if (key == "description")
                {
                    abstractDS.AddDescription("en", dsELement.Value.ToString());
                }
                if (key == "descriptions")
                {
                    foreach (var temp1 in dsELement.Value)
                    {
                        JProperty x = (JProperty)temp1;
                        abstractDS.AddDescription((x.Name).ToString(), (x.Value).ToString());
                    }
                }
                if (key == "titles")
                {
                    List<LangString> titleList = new List<LangString>();
                    foreach (var temp2 in dsELement.Value)
                    {
                        JProperty x = (JProperty)temp2;
                        LangString title = new LangString(
                                                            (x.Name).ToString(), (x.Value).ToString());
                        titleList.Add(title);
                    }
                    MultiLanguageProperty mlp = BuildMultiLanguageProperty(
                                                          key, titleList, "Provides multi-language " +
                                                          "human-readable titles (e.g., display a text for " +
                                                          "UI representation in different languages)");
                    abstractDS.Add(mlp);
                }
                if (key == "oneOf")
                {
                    SubmodelElementCollection oneOf = new SubmodelElementCollection();
                    oneOf.IdShort = "oneOf";
                    oneOf.Category = "PARAMETER";
                    //oneOf.ordered = false;
                    //oneOf.allowDuplicates = false;
                    oneOf.Kind = ModelingKind.Instance;
                    oneOf.AddDescription("en", "Used to ensure that the data is valid " +
                                                "against one of the specified schemas in the array.");
                    oneOf.Qualifiers = new List<Qualifier>();
                    int i = 0;
                    foreach (var ds in dsjObject["oneOf"])
                    {
                        i = i + 1;
                        oneOf.Add(BuildAbstractDataSchema(JObject.FromObject(ds), "oneOf" + i.ToString(), "oneOf"));
                    }
                    abstractDS.Add(oneOf);
                }
                if (key == "data1" || key == "type")
                {
                    string dsType = "";
                    if (key == "data1")
                    {
                        dsType = dsjObject["data1"]["type"].ToString();
                        abstractDS.Qualifiers.Add(createAASQualifier("data1.Type", dsType));
                    }
                    else
                    {
                        dsType = dsjObject["type"].ToString();
                        abstractDS.Qualifiers.Add(createAASQualifier("type", dsType));
                    }

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
                if ((dsELement.Value.Type).ToString() == "Array")
                {
                    if (dsArrayList.Contains(key))
                    {
                        SubmodelElementCollection arrayCollection = new
                                                        SubmodelElementCollection();
                        arrayCollection.IdShort = key;
                        arrayCollection.Category = "PARAMETER";
                        //arrayCollection.ordered = false;
                        //arrayCollection.allowDuplicates = false;
                        arrayCollection.Kind = ModelingKind.Instance;
                        arrayCollection.AddDescription("en", TDSemanticId.getarrayListDesc(key));
                        arrayCollection.Qualifiers = new List<Qualifier>();
                        int index = 1;
                        foreach (var x in dsELement.Value)
                        {
                            Qualifier _arrayCQual = createAASQualifier(key + index.ToString(),
                                (x).ToString());
                            arrayCollection.Qualifiers.Add(_arrayCQual);
                            index = index + 1;
                        }
                        abstractDS.Add(arrayCollection);

                    }
                }
                else
                {
                    if (qualList.Contains(key))
                    {
                        abstractDS.Qualifiers.Add(createAASQualifier(key, dsELement.Value.ToString()));
                    }
                }
            }
            return abstractDS;
        }

        // TD Interaction Avoidance
        public static SubmodelElementCollection BuildAbstractInteractionAvoidance(
            JObject jObject, string idShort, string type)
        {
            SubmodelElementCollection _interactionAffordance = BuildAbstractDataSchema(
                                                        jObject, idShort, type);
            if (jObject.ContainsKey("uriVariables"))
            {
                SubmodelElementCollection _uriVariables = new SubmodelElementCollection();
                _uriVariables.IdShort = "uriVariables";
                _uriVariables.Category = "PARAMETER";
                //_uriVariables.ordered = false;
                //_uriVariables.allowDuplicates = false;
                _uriVariables.Kind = ModelingKind.Instance;
                _uriVariables.SemanticId = createSemanticID("uriVariables");
                foreach (var temp in jObject["uriVariables"])
                {
                    JProperty uriVarObject = (JProperty)temp;
                    string key = uriVarObject.Name.ToString();
                    _uriVariables.Add(BuildAbstractDataSchema(
                        JObject.FromObject(uriVarObject.Value), key, "uriVariables"));
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
        public static SubmodelElementCollection BuildTDProperty(
                                                JObject _propertyJObject, string propertyName)
        {
            SubmodelElementCollection _tdProperty = BuildAbstractInteractionAvoidance(
                                                                _propertyJObject, propertyName, "property");
            _tdProperty.SemanticId = createSemanticID("property");
            if (_propertyJObject.ContainsKey("observable"))
            {
                _tdProperty.Qualifiers.Add(createAASQualifier("observable",
                                                        (_propertyJObject["observable"]).ToString()));
            }
            if (_propertyJObject.ContainsKey("updateFrequencey"))
            {
                _tdProperty.Qualifiers.Add(createAASQualifier("updateFrequencey",
                                                        (_propertyJObject["updateFrequencey"]).ToString()));
            }
            if (_propertyJObject.ContainsKey("updatable"))
            {
                _tdProperty.Qualifiers.Add(createAASQualifier("updatable",
                                                         (_propertyJObject["updatable"]).ToString()));
            }
            return _tdProperty;
        }
        public static SubmodelElementCollection BuildTDProperties(JObject tdObject)
        {
            SubmodelElementCollection tdProperties = new SubmodelElementCollection();
            tdProperties.IdShort = "properties";
            tdProperties.Category = "PARAMETER";
            //tdProperties.ordered = false;
            //tdProperties.allowDuplicates = false;
            tdProperties.Kind = ModelingKind.Instance;
            tdProperties.AddDescription("en", "Properties definion of the thing Description");
            tdProperties.SemanticId = createSemanticID("properties");
            foreach (var temp in tdObject["properties"])
            {
                JProperty propertyObject = (JProperty)temp;
                tdProperties.Add(BuildTDProperty(JObject.FromObject(propertyObject.Value),
                                                    (propertyObject.Name).ToString()));
            }
            return tdProperties;
        }

        // TD Events
        public static SubmodelElementCollection BuildTDEvent(JObject _eventJObject, string actionName)
        {
            SubmodelElementCollection _tdEvent = BuildAbstractInteractionAvoidance(
                _eventJObject, actionName, "event");
            _tdEvent.SemanticId = createSemanticID("event");
            string[] dsList = { "subscription", "data", "cancellation" };
            foreach (var temp in (JToken)_eventJObject)
            {
                JProperty eventElement = (JProperty)temp;
                string key = eventElement.Name.ToString();
                if (dsList.Contains(key))
                {
                    _tdEvent.Add(BuildAbstractDataSchema(JObject.FromObject(_eventJObject[key]), key, key));
                }
            }
            return _tdEvent;
        }
        public static SubmodelElementCollection BuildTDEvents(JObject jObject)
        {
            SubmodelElementCollection tdEvents = new SubmodelElementCollection();
            tdEvents.IdShort = "events";
            tdEvents.Category = "PARAMETER";
            //tdEvents.ordered = false;
            //tdEvents.allowDuplicates = false;
            tdEvents.Kind = ModelingKind.Instance;
            tdEvents.AddDescription("en", "All Event-based Interaction Affordances of the Thing.");
            tdEvents.SemanticId = createSemanticID("events");
            foreach (var temp in jObject["events"])
            {
                JProperty eventObject = (JProperty)temp;
                tdEvents.Add(BuildTDEvent(JObject.FromObject(eventObject.Value), (eventObject.Name).ToString()));
            }
            return tdEvents;
        }

        // TD Actions
        public static SubmodelElementCollection BuildTDActions(JObject jObject)
        {
            SubmodelElementCollection tdActions = new SubmodelElementCollection();
            tdActions.IdShort = "actions";
            tdActions.Category = "PARAMETER";
            //tdActions.ordered = false;
            //tdActions.allowDuplicates = false;
            tdActions.Kind = ModelingKind.Instance;
            tdActions.AddDescription("en", "All Action-based Interaction Affordances of the Thing.");
            tdActions.SemanticId = createSemanticID("actions");
            foreach (var temp in jObject["actions"])
            {
                JProperty actionObject = (JProperty)temp;
                tdActions.Add(BuildTDAction(JObject.FromObject(actionObject.Value),
                                                (actionObject.Name).ToString()));
            }
            return tdActions;
        }
        public static SubmodelElementCollection BuildTDAction(JObject _actionJObject, string actionName)
        {
            SubmodelElementCollection _tdAction = BuildAbstractInteractionAvoidance(
                _actionJObject, actionName, "action");
            _tdAction.SemanticId = createSemanticID("action");
            string[] dsList = { "input", "output" };
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
                    _tdAction.Qualifiers.Add(createAASQualifier(key, _actionJObject[key].ToString()));
                }
            }
            return _tdAction;
        }

        // TD Links
        public static SubmodelElementCollection BuildTDLink(JObject linkJObject, string idShort)
        {
            SubmodelElementCollection _tdLink = new SubmodelElementCollection();
            _tdLink.IdShort = idShort;
            _tdLink.Category = "PARAMETER";
            //_tdLink.ordered = false;
            //_tdLink.allowDuplicates = false;
            _tdLink.Kind = ModelingKind.Instance;
            _tdLink.AddDescription("en", "A link can be viewed as a statement of the form link" +
                "context has a relation type resource at link target, " +
                "where the optional target attributes may further describe the resource");
            _tdLink.SemanticId = createSemanticID("link");
            _tdLink.Qualifiers = new List<Qualifier>();
            string[] linkElemList = { "href", "type", "rel", "anchor", "sizes" };
            foreach (var temp in (JToken)linkJObject)
            {
                JProperty linkElement = (JProperty)temp;
                if (linkElemList.Contains(linkElement.Name))
                {
                    _tdLink.Qualifiers.Add(createAASQualifier(
                            linkElement.Name.ToString(), linkElement.Value.ToString()));
                }
            }
            return _tdLink;
        }
        public static SubmodelElementCollection BuildTDLinks(JObject jObject)
        {
            SubmodelElementCollection tdLinks = new SubmodelElementCollection();
            tdLinks.IdShort = "links";
            tdLinks.Category = "PARAMETER";
            //tdLinks.ordered = false;
            //tdLinks.allowDuplicates = false;
            tdLinks.Kind = ModelingKind.Instance;
            tdLinks.AddDescription("en", "Provides Web links to arbitrary resources that relate to" +
                                    "the specified Thing Description.");
            tdLinks.SemanticId = createSemanticID("links");
            int index = 1;
            foreach (var linkObject in jObject["links"])
            {
                tdLinks.Add(BuildTDLink(JObject.FromObject(linkObject), "link" + index.ToString()));
                index = index + 1;
            }
            return tdLinks;
        }

        // TD Security Definition
        public static SubmodelElementCollection BuildSecurityDefinition(
                    JObject jObject, string definitionName)
        {
            SubmodelElementCollection _securityDefinition = new SubmodelElementCollection();
            _securityDefinition.IdShort = definitionName;
            _securityDefinition.Category = "PARAMETER";
            //_securityDefinition.ordered = false;
            //_securityDefinition.allowDuplicates = false;
            _securityDefinition.Kind = ModelingKind.Instance;
            _securityDefinition.Qualifiers = new List<Qualifier>();
            if (jObject.ContainsKey("@type")) // needs to be discussed with Mr. Sebastian. When input is an array 
                                              // requires an example
            {
                _securityDefinition.Qualifiers.Add(createAASQualifier("@type", jObject["@type"].ToString()));
            }
            if (jObject.ContainsKey("description"))
            {
                _securityDefinition.AddDescription("en", jObject["description"].ToString());
            }
            if (jObject.ContainsKey("descriptions"))
            {
                foreach (var temp in jObject["descriptions"])
                {
                    JProperty x = (JProperty)temp;
                    _securityDefinition.AddDescription((x.Name).ToString(), (x.Value).ToString());
                }
            }
            if (jObject.ContainsKey("proxy"))
            {
                _securityDefinition.Qualifiers.Add(createAASQualifier("proxy", jObject["proxy"].ToString()));
            }
            if (jObject.ContainsKey("scheme"))
            {
                string scheme = jObject["scheme"].ToString();
                _securityDefinition.Qualifiers.Add(createAASQualifier("scheme", scheme));
                if (scheme == "combo")
                {
                    if (jObject.ContainsKey("oneOf"))
                    {
                        if ((jObject["oneOf"].Type).ToString() == "Array")
                        {
                            SubmodelElementCollection _oneOf = new SubmodelElementCollection();
                            _oneOf.IdShort = "oneOf";
                            _oneOf.Category = "PARAMETER";
                            //_oneOf.ordered = false;
                            //_oneOf.allowDuplicates = false;
                            _oneOf.Kind = ModelingKind.Instance;
                            _oneOf.AddDescription("en", "	Array of two or more strings identifying other" +
                                "named security scheme definitions, any one of which, when satisfied, " +
                                "will allow access. Only one may be chosen for use.");
                            _oneOf.SemanticId = createSemanticID("oneOf");
                            int index = 1;
                            _oneOf.Qualifiers = new List<Qualifier>();
                            foreach (var x in jObject["oneOf"])
                            {
                                _oneOf.Qualifiers.Add(createAASQualifier("oneOf" + (index).ToString(), (x).ToString()));
                                index = index + 1;
                            }
                            _securityDefinition.Add(_oneOf);
                        }
                        else
                        {
                            _securityDefinition.Qualifiers.Add(createAASQualifier("oneOf",
                                                                    (jObject["oneOf"]).ToString()));
                        }
                    }
                    if (jObject.ContainsKey("allOf"))
                    {
                        if ((jObject["allOf"].Type).ToString() == "Array")
                        {
                            SubmodelElementCollection _allOf = new SubmodelElementCollection();
                            _allOf.IdShort = "oneOf";
                            _allOf.Category = "PARAMETER";
                            //_allOf.ordered = false;
                            //_allOf.allowDuplicates = false;
                            _allOf.Kind = ModelingKind.Instance;
                            _allOf.AddDescription("en", "Array of two or more strings identifying other" +
                                "named security scheme definitions, all of which must be satisfied for access.");
                            _allOf.SemanticId = createSemanticID("allOf");
                            int index = 1;
                            _allOf.Qualifiers = new List<Qualifier>();
                            foreach (var x in jObject["allOf"])
                            {
                                _allOf.Qualifiers.Add(createAASQualifier("allOf" + (index).ToString(),
                                    (x).ToString()));
                                index = index + 1;
                            }
                        }
                        else
                        {
                            _securityDefinition.Qualifiers.Add(createAASQualifier("allOf",
                                (jObject["allOf"]).ToString()));
                        }
                    }
                    _securityDefinition.SemanticId = createSemanticID("combo");
                }
                if (scheme == "basic" || scheme == "apikey")
                {
                    if (jObject.ContainsKey("name"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("name", jObject["name"].ToString()));
                    }
                    if (jObject.ContainsKey("in"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("in", jObject["in"].ToString()));
                    }
                    if (scheme == "basic")
                    {
                        _securityDefinition.SemanticId = createSemanticID("basic");
                    }
                    if (scheme == "apikey")
                    {
                        _securityDefinition.SemanticId = createSemanticID("apikey");
                    }

                }
                if (scheme == "digest")
                {
                    if (jObject.ContainsKey("name"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("name", jObject["name"].ToString()));
                    }
                    if (jObject.ContainsKey("in"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("in", jObject["in"].ToString()));
                    }
                    if (jObject.ContainsKey("qop"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("qop", jObject["qop"].ToString()));
                    }
                    _securityDefinition.SemanticId = createSemanticID("digest");
                }
                if (scheme == "bearer")
                {
                    if (jObject.ContainsKey("name"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("name", jObject["name"].ToString()));
                    }
                    if (jObject.ContainsKey("in"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("in", jObject["in"].ToString()));
                    }
                    if (jObject.ContainsKey("authorization"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("authorization",
                                                                    jObject["authorization"].ToString()));
                    }
                    if (jObject.ContainsKey("alg"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("alg", jObject["alg"].ToString()));
                    }
                    if (jObject.ContainsKey("format"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("format", jObject["format"].ToString()));
                    }
                    _securityDefinition.SemanticId = createSemanticID("bearer");
                }
                if (scheme == "nosec")
                {
                    _securityDefinition.SemanticId = createSemanticID("nosec");
                }
                if (scheme == "psk")
                {
                    if (jObject.ContainsKey("identity"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("identity",
                                            jObject["identity"].ToString()));
                    }
                    _securityDefinition.SemanticId = createSemanticID("psk");
                }
                if (scheme == "oauth2")
                {
                    if (jObject.ContainsKey("authorization"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("authorization",
                                                jObject["authorization"].ToString()));
                    }
                    if (jObject.ContainsKey("token"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("token",
                                                jObject["token"].ToString()));
                    }
                    if (jObject.ContainsKey("refresh"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("refresh",
                                                jObject["refresh"].ToString()));
                    }
                    if (jObject.ContainsKey("flow"))
                    {
                        _securityDefinition.Qualifiers.Add(createAASQualifier("flow",
                                                jObject["flow"].ToString()));
                    }
                    if (jObject.ContainsKey("scopes"))
                    {
                        if ((jObject["scopes"].Type).ToString() == "String")
                        {
                            _securityDefinition.Qualifiers.Add(createAASQualifier("scopes",
                                                (jObject["scopes"]).ToString()));
                        }
                        if ((jObject["scopes"].Type).ToString() == "Array")
                        {
                            SubmodelElementCollection _scopes = new SubmodelElementCollection();
                            _scopes.IdShort = "scopes";
                            _scopes.Category = "PARAMETER";
                            //_scopes.ordered = false;
                            //_scopes.allowDuplicates = false;
                            _scopes.Kind = ModelingKind.Instance;
                            _scopes.AddDescription("en", "Set of authorization scope identifiers " +
                                "provided as an array. These are provided in tokens returned " +
                                "by an authorization server and associated with forms in order to " +
                                "identify what resources a client may access and how. The values " +
                                "associated with a form should be chosen from those defined in an " +
                                "OAuth2SecurityScheme active on that form.");
                            _scopes.SemanticId = createSemanticID("scopes");
                            _scopes.Qualifiers = new List<Qualifier>();
                            int index = 1;
                            foreach (var x in jObject["scopes"])
                            {
                                _scopes.Qualifiers.Add(createAASQualifier("scopes" + (index).ToString(),
                                                            (x).ToString()));
                                index = index + 1;
                            }
                            _securityDefinition.Add(_scopes);
                        }
                    }
                    _securityDefinition.SemanticId = createSemanticID("oauth2");
                }
            }
            return _securityDefinition;
        }
        public static SubmodelElementCollection BuildTDSecurityDefinitions(JObject jObject)
        {

            SubmodelElementCollection _securityDefinitions = new SubmodelElementCollection();
            _securityDefinitions.IdShort = "securityDefinitions";
            _securityDefinitions.Category = "PARAMETER";
            //_securityDefinitions.ordered = false;
            //_securityDefinitions.allowDuplicates = false;
            _securityDefinitions.Kind = ModelingKind.Instance;
            _securityDefinitions.AddDescription("en", "Set of named security configurations" +
                "(definitions only). Not actually applied unless names are used in a security name-value pair.");
            _securityDefinitions.SemanticId = createSemanticID("securityDefinitions");
            _securityDefinitions.Qualifiers = new List<Qualifier>();
            foreach (var temp in jObject["securityDefinitions"])
            {
                JProperty x1 = (JProperty)temp;
                JObject _jObject = JObject.Parse((x1.Value).ToString());
                _securityDefinitions.Add(BuildSecurityDefinition(_jObject, (x1.Name).ToString()));
            }
            return _securityDefinitions;
        }

        // TD Forms
        public static SubmodelElementCollection BuildTDForm(JObject formjObject, string idShort)
        {
            SubmodelElementCollection tdForm = new SubmodelElementCollection();
            tdForm.IdShort = idShort;
            tdForm.Category = "PARAMETER";
            //tdForm.ordered = false;
            //tdForm.allowDuplicates = false;
            tdForm.Kind = ModelingKind.Instance;
            tdForm.AddDescription("en", "Hypermedia controls that describe how an operation can be performed." +
                                        " Form is a  serializations of Protocol Bindings");
            tdForm.SemanticId = createSemanticID("form");
            tdForm.Qualifiers = new List<Qualifier>();
            string[] qualList = { "href", "contentType", "contentCoding", "subprotocol", "security", "scopes", "op" };
            string[] qualArrayList = { "security", "scopes", "op" };

            foreach (var temp in (JToken)formjObject)
            {
                JProperty formElement = (JProperty)temp;
                string key = formElement.Name.ToString();
                if (key == "response") // Need to check 
                {
                    SubmodelElementCollection _response = new SubmodelElementCollection();
                    _response.IdShort = "response";
                    _response.Category = "PARAMETER";
                    //_response.ordered = false;
                    //_response.allowDuplicates = false;
                    _response.Kind = ModelingKind.Instance;
                    _response.AddDescription("en", "This optional term can be used if, e.g., the output" +
                        "communication metadata differ from input metadata (e.g., output contentType differ" +
                        "from the input contentType). The response name contains metadata that is only valid for" +
                        "the primary response messages.");
                    JObject contentTypeJObject = JObject.FromObject(formjObject["response"]);
                    _response.Add(BuildTDProperty(contentTypeJObject, "contentType"));
                }
                else if (key == "additionalResponses") // Need to check 
                {
                    SubmodelElementCollection _response = new SubmodelElementCollection();
                    _response.IdShort = "additionalResponses";
                    _response.Category = "PARAMETER";
                    //_response.ordered = false;
                    //_response.allowDuplicates = false;
                    _response.Kind = ModelingKind.Instance;
                    _response.AddDescription("en", "This optional term can be used if additional" +
                        "expected responses are possible, e.g. for error reporting. Each additional" +
                        "response needs to be distinguished from others in some way (for example, by" +
                        "specifying a protocol-specific error code), and may also have its own data schema");
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
                            _response.Add(BuildAdditionalResponse(JObject.FromObject(arJObject),
                                "additionalResponse" + index.ToString()));
                        }
                    }
                    tdForm.Add(_response);
                }
                if ((formElement.Value.Type).ToString() == "Array")
                {
                    if (qualArrayList.Contains(formElement.Name))
                    {
                        SubmodelElementCollection _arrayElement = new SubmodelElementCollection();
                        _arrayElement.IdShort = key;
                        _arrayElement.Category = "PARAMETER";
                        //_arrayElement.ordered = false;
                        //_arrayElement.allowDuplicates = false;
                        _arrayElement.Kind = ModelingKind.Instance;
                        _arrayElement.AddDescription("en", TDSemanticId.getarrayListDescription(key));
                        _arrayElement.SemanticId = createSemanticID(key);
                        _arrayElement.Qualifiers = new List<Qualifier>();
                        int index = 0;
                        foreach (var x in formjObject[key])
                        {
                            Qualifier _formQualifier = createAASQualifier(key, (x).ToString());
                            _formQualifier.Type = key + index.ToString();
                            _arrayElement.Qualifiers.Add(_formQualifier);

                            index = index + 1;
                        }
                        tdForm.Add(_arrayElement);
                    }
                }
                else
                {
                    if (qualList.Contains(key))
                    {
                        tdForm.Qualifiers.Add(createAASQualifier(key, formElement.Value.ToString()));
                    }
                    else
                    {
                        if ((formElement.Value.Type).ToString() == "String")
                        {
                            tdForm.Qualifiers.Add(createAASQualifier(key, formElement.Value.ToString()));
                        }
                    }
                }

            }
            return tdForm;
        }
        public static SubmodelElementCollection BuildForms(JObject tdJObject)
        {
            SubmodelElementCollection forms = new SubmodelElementCollection();
            forms.IdShort = "forms";
            forms.Category = "PARAMETER";
            //forms.ordered = false;
            //forms.allowDuplicates = false;
            forms.Kind = ModelingKind.Instance;
            forms.AddDescription("en", "Set of form hypermedia controls that describe how an operation" +
                                           "can be performed." +
                                        "Forms are serializations of Protocol Bindings");
            int i = 0;
            foreach (var formObject in tdJObject["forms"])
            {
                forms.Add(BuildTDForm(JObject.FromObject(formObject), "Form" + i.ToString()));
            }
            return forms;
        }

        // AAS Semantic ID
        public static Reference createSemanticID(string tdType)
        {
            //Key tdSemanticKey = new Key();
            //tdSemanticKey.Type = "GlobalReference";
            //tdSemanticKey.local = true;
            //tdSemanticKey.idType = "IRI";
            //tdSemanticKey.Value = TDSemanticId.getSemanticID(tdType);
            Reference tdSemanticId = new(ReferenceTypes.GlobalReference, new List<Key>() { new Key((KeyTypes)Stringification.KeyTypesFromString(tdType), TDSemanticId.getSemanticID(tdType)) });

            return tdSemanticId;
        }

        // AAS Qualifier
        public static Qualifier createAASQualifier(string qualifierType, string qualifierValue)
        {
            Qualifier aasQualifier = new Qualifier(qualifierType, DataTypeDefXsd.String, value:qualifierValue);
            if (TDSemanticId.getSemanticID(qualifierType) != "empty")
            {
                aasQualifier.SemanticId = createSemanticID(qualifierType);
            }
            return aasQualifier;
        }


        public static SubmodelElementCollection BuildschemaDefinitions(JObject tdJObject)
        {
            SubmodelElementCollection _schemaDefinitions = new SubmodelElementCollection();
            _schemaDefinitions.IdShort = "schemaDefinitions";
            _schemaDefinitions.Category = "PARAMETER";
            _schemaDefinitions.Kind = ModelingKind.Instance;
            _schemaDefinitions.AddDescription("en", "Set of named data schemas." +
                                "To be used in a schema name-value pair inside an AdditionalExpectedResponse object.");
            _schemaDefinitions.Qualifiers = new List<Qualifier>();
            //_schemaDefinitions.SemanticId = createSemanticID("schemaDefinitions");
            _schemaDefinitions.SemanticId = createSemanticID("schemaDefinitions");
            foreach (var sdKey in tdJObject["schemaDefinitions"])
            {
                JObject _schemaDefinition = new JObject(tdJObject["schemaDefinitions"][sdKey]);
                _schemaDefinitions.Value.Add(BuildAbstractDataSchema(_schemaDefinition, sdKey.ToString(),
                    "schemaDefinitions"));
            }
            return _schemaDefinitions;
        }
        public static JObject ImportTDJsontoSubModel(
            string inputFn, AasCore.Aas3_0_RC02.Environment env, Submodel sm,
            Reference smref)
        {
            JObject exportData = new JObject();
            try
            {
                JObject tdJObject;
                string text = System.IO.File.ReadAllText(inputFn);
                using (var tdStringReader = new StringReader(text))
                using (var jsonTextReader = new JsonTextReader(tdStringReader)
                { DateParseHandling = DateParseHandling.None })
                {
                    tdJObject = JObject.FromObject(JToken.ReadFrom(jsonTextReader));
                }
                sm.Qualifiers = new List<Qualifier>();
                foreach (var tempThing in (JToken)tdJObject)
                {
                    JProperty thingE = (JProperty)tempThing;
                    string key = thingE.Name.ToString();
                    string[] qualList = { "@context","created",
                                          "modified", "support", "base","security","@type" ,"title"};
                    string[] tdArrayList = { "profile", "security", "@type" };
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
                        List<LangString> titleList = new List<LangString>();
                        foreach (var temp in thingE.Value)
                        {
                            JProperty x = (JProperty)temp;
                            LangString title = new LangString((x.Name).ToString(),
                                (x.Value).ToString());
                            titleList.Add(title);
                        }
                        MultiLanguageProperty mlp = BuildMultiLanguageProperty(key, titleList,
                            "Provides multi-language human-readable titles (e.g., display a text for UI" +
                            "representation in different languages)");
                        sm.Add(mlp);
                    }

                    if (key == "id")
                    {
                        string id = thingE.Value.ToString();
                        sm.Id = id;
                        smref.Keys[0] = new Key(KeyTypes.Submodel, id);
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
                        List<LangString> titleList = new List<LangString>();
                        foreach (var temp in thingE.Value)
                        {
                            JProperty x = (JProperty)temp;
                            LangString title = new LangString((x.Name).ToString(),
                                (x.Value).ToString());
                            titleList.Add(title);
                        }
                        MultiLanguageProperty mlp = BuildMultiLanguageProperty(key, titleList,
                            "Provides multi-language human-readable titles (e.g., display a text for UI " +
                            "representation in different languages)");
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
                        sm.Administration = new AdministrativeInformation(version: intance, revision: model);
                    }
                    if (key == "schemaDefinitions")
                    {
                        sm.Add(BuildschemaDefinitions(tdJObject));
                    }
                    if ((thingE.Value.Type).ToString() == "Array")
                    {
                        JObject secProfile = new JObject
                        {
                            ["profile"] = "Indicates the WoT Profile mechanisms followed by this" +
                            "Thing Description and the corresponding Thing implementation.",
                            ["security"] = "security definition names",
                            ["@type"] = "JSON-LD keyword to label the object with semantic " +
                            "tags (or types)."
                        };
                        if (key == "@context")
                        {
                            SubmodelElementCollection _context = new SubmodelElementCollection();
                            _context.IdShort = key;
                            _context.Category = "PARAMETER";
                            //_context.ordered = false;
                            //_context.allowDuplicates = false;
                            _context.Kind = ModelingKind.Instance;
                            _context.AddDescription("en", "JSON-LD keyword to label the object with semantic tags ");
                            _context.SemanticId = createSemanticID(key);
                            _context.Qualifiers = new List<Qualifier>();
                            foreach (var temp in thingE.Value)
                            {
                                if ((temp.Type).ToString() == "String")
                                {
                                    _context.Qualifiers.Add(createAASQualifier(key, temp.ToString()));
                                }
                                else
                                {
                                    JObject _contextJobject = JObject.FromObject(temp);
                                    foreach (var y in _contextJobject)
                                    {
                                        _context.Qualifiers.Add(createAASQualifier((y.Key).ToString(),
                                            (y.Value).ToString()));
                                    }
                                }
                            }
                            sm.Add(_context);
                        }
                        if (tdArrayList.Contains(key))
                        {
                            SubmodelElementCollection _profile = new SubmodelElementCollection();
                            _profile.IdShort = key;
                            _profile.Category = "PARAMETER";
                            //_profile.ordered = false;
                            //_profile.allowDuplicates = false;
                            _profile.Kind = ModelingKind.Instance;
                            _profile.AddDescription("en", secProfile[key].ToString());
                            _profile.Qualifiers = new List<Qualifier>();
                            int index = 1;
                            foreach (var x in thingE.Value)
                            {
                                Qualifier _profileQual = createAASQualifier(key, (x).ToString());
                                _profileQual.Type = key + index.ToString();
                                _profile.Qualifiers.Add(_profileQual);
                                index = index + 1;
                            }
                            sm.Add(_profile);

                        }
                    }
                    if ((thingE.Value.Type).ToString() == "String")
                    {
                        if (qualList.Contains(key))
                        {
                            sm.Qualifiers.Add(createAASQualifier(key, thingE.Value.ToString()));
                        }
                    }

                    sm.SemanticId = createSemanticID("Thing");
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
