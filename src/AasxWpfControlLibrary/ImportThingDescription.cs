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
        public static AdminShell.SubmodelElementCollection BuildArraySchema(AdminShell.SubmodelElementCollection dsCollection, JObject arrayJObject)
        {
            AdminShell.QualifierCollection abDSQualifier = new AdminShell.QualifierCollection();
            foreach (var arrayELement in arrayJObject)
            {
                string key = arrayELement.Key.ToString();
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
                            AdminShell.SubmodelElementCollection _item = BuildAbstractDataSchema(_jObject, "item" + (i).ToString());
                            i = i + 1;
                            _item.semanticId = createSemanticID("item");
                            items.Add(_item);
                        }
                    }
                    else
                    {
                        string jItem = (arrayJObject["items"]).ToString();
                        JObject _jObject = JObject.Parse(jItem);
                        AdminShell.SubmodelElementCollection _item = BuildAbstractDataSchema(_jObject, "item1");
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
        public static AdminShell.SubmodelElementCollection BuildNumberSchema(AdminShell.SubmodelElementCollection dsCollection, JObject numberJObject)
        {
            foreach (var numberElement in numberJObject)
            {
                string key = numberElement.Key.ToString();
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
        public static AdminShell.SubmodelElementCollection BuildIntegerSchema(AdminShell.SubmodelElementCollection dsCollection, JObject interJObject)
        {
            foreach (var integerSElement in interJObject)
            {
                string key = integerSElement.Key.ToString();
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
        public static AdminShell.SubmodelElementCollection BuildStringSchema(AdminShell.SubmodelElementCollection dsCollection, JObject stringjObject)
        {
            foreach (var stringElement in stringjObject)
            {
                string key = stringElement.Key.ToString();
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
        public static AdminShell.SubmodelElementCollection BuildObjectSchema(AdminShell.SubmodelElementCollection dsCollection, JObject objectjObject)
        {
            foreach (var objectElement in objectjObject)
            {
                string key = objectElement.Key.ToString();
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
                    JObject _propertiesVariableJobject = (JObject)objectjObject["properties"];
                    foreach (var x in _propertiesVariableJobject)
                    {
                        JObject _propertyJobject = JObject.FromObject(x.Value);
                        AdminShell.SubmodelElementCollection _propertyC = BuildAbstractDataSchema(_propertyJobject, x.Key.ToString());
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
            List<string> formElements = new List<string> { "href", "contentType", "contentCoding", "security", "scopes", "op", "response", "additionalResponses" };
            foreach (var fomELement in formjObject)
            {
                string key = fomELement.Key.ToString();
                if (key == ("href"))
                {
                    tdForm.qualifiers.Add(createAASQualifier("href", formjObject["href"].ToString()));
                }
                if (key == ("contentType"))
                {
                    tdForm.qualifiers.Add(createAASQualifier("contentType", formjObject["contentType"].ToString()));
                }
                if (key == ("contentCoding"))
                {
                    tdForm.qualifiers.Add(createAASQualifier("contentCoding", formjObject["contentCoding"].ToString()));
                }
                if (key == ("security"))
                {
                    if ((formjObject["security"].Type).ToString() == "String")
                    {
                        tdForm.qualifiers.Add(createAASQualifier("security", formjObject["security"].ToString()));
                    }
                    if ((formjObject["security"].Type).ToString() == "Array")
                    {
                        AdminShell.SubmodelElementCollection _security = new AdminShell.SubmodelElementCollection();
                        _security.idShort = "security";
                        _security.category = "PARAMETER";
                        _security.ordered = false;
                        _security.allowDuplicates = false;
                        _security.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                        _security.AddDescription("en", "Set of security definition names, chosen from those defined in securityDefinitions. These must all be satisfied for access to resources.");
                        _security.semanticId = createSemanticID("security");
                        _security.qualifiers = new AdminShell.QualifierCollection();
                        int index = 0;
                        foreach (var x in formjObject["security"])
                        {
                            AdminShell.Qualifier _securityQualifier = createAASQualifier("security", (x).ToString());
                            _securityQualifier.type = "security" + index.ToString();
                            _security.qualifiers.Add(_securityQualifier);

                            index = index + 1;
                        }
                        tdForm.Add(_security);
                    }
                }
                if (key == ("scopes"))
                {
                    if ((formjObject["scopes"].Type).ToString() == "String")
                    {
                        tdForm.qualifiers.Add(createAASQualifier("scopes", formjObject["scopes"].ToString()));
                    }
                    if ((formjObject["scopes"].Type).ToString() == "Array")
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
                        int index = 0;
                        foreach (var x in formjObject["scopes"])
                        {
                            AdminShell.Qualifier _scopeQualifier = createAASQualifier("scope", (x).ToString());
                            _scopeQualifier.type = "scope" + index.ToString();
                            _scopes.qualifiers.Add(_scopeQualifier);
                            index = index + 1;
                        }
                        tdForm.Add(_scopes);
                    }
                }
                if (key == ("response")) // Need to check 
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
                if (key == ("additionalResponses")) // Need to check 
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
                        foreach (JObject arJObject in formjObject["additionalResponses"])
                        {
                            index = index + 1;
                            _response.Add(BuildAdditionalResponse(arJObject, "additionalResponse" + index.ToString()));
                        }
                    }
                    tdForm.Add(_response);
                }
                if (key == ("op"))
                {
                    if ((formjObject["op"].Type).ToString() == "String")
                    {
                        tdForm.qualifiers.Add(createAASQualifier("op", formjObject["op"].ToString()));
                    }
                    if ((formjObject["op"].Type).ToString() == "Array")
                    {
                        AdminShell.SubmodelElementCollection _op = new AdminShell.SubmodelElementCollection();
                        _op.idShort = "op";
                        _op.category = "PARAMETER";
                        _op.ordered = false;
                        _op.allowDuplicates = false;
                        _op.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                        _op.AddDescription("en", "	Indicates the semantic intention of performing the operation(s) described by the form. For example, the Property interaction allows get and set operations. The protocol binding may contain a form for the get operation and a different form for the set operation. The op attribute indicates which form is for which and allows the client to select the correct form for the operation required. op can be assigned one or more interaction verb(s) each representing a semantic intention of an operation.");
                        _op.semanticId = createSemanticID("op");
                        _op.qualifiers = new AdminShell.QualifierCollection();
                        int index = 0;
                        foreach (var x in formjObject["op"])
                        {
                            AdminShell.Qualifier _opQualifier = createAASQualifier("op", (x).ToString());
                            _opQualifier.type = "op" + index.ToString();
                            _op.qualifiers.Add(_opQualifier);
                            index = index + 1;
                        }
                        tdForm.Add(_op);
                    }
                }
                if (key == ("subprotocol"))
                {
                    tdForm.qualifiers.Add(createAASQualifier("subprotocol", formjObject["subprotocol"].ToString()));
                }
                if (!formElements.Contains(key))
                {
                    tdForm.qualifiers.Add(createAASQualifier(key, (fomELement.Value).ToString()));
                }
            }

            return tdForm;
        }

        // TD DataSchema
        public static AdminShell.SubmodelElementCollection BuildAbstractDataSchema(JObject dsjObject, string idShort)
        {
            AdminShell.SubmodelElementCollection abstractDS = new AdminShell.SubmodelElementCollection();
            abstractDS.idShort = idShort;
            abstractDS.category = "PARAMETER";
            abstractDS.ordered = false;
            abstractDS.allowDuplicates = false;
            abstractDS.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            abstractDS.qualifiers = new AdminShell.QualifierCollection();
            foreach (var dsELement in dsjObject)
            {
                string key = dsELement.Key.ToString();
                if (key == ("@type")) // needs to be discussed with Mr. Sebastian. When input is an array 
                                      // requires an example
                {
                    abstractDS.qualifiers.Add(createAASQualifier("@type", dsjObject["@type"].ToString()));
                }
                if (key == ("title"))
                {
                    abstractDS.qualifiers.Add(createAASQualifier("title", dsjObject["title"].ToString()));
                }
                if (key == ("titles"))
                {
                    JObject _titlesJObject = (JObject)dsjObject["titles"];
                    List<AdminShellV20.LangStr> titleList2 = new List<AdminShellV20.LangStr>();
                    foreach (var x in _titlesJObject)
                    {
                        AdminShellV20.LangStr title = new AdminShellV20.LangStr((x.Key).ToString(), (x.Value).ToString());
                        titleList2.Add(title);
                    }
                    AdminShell.MultiLanguageProperty mlp = BuildMultiLanguageProperty("titles", titleList2, "Provides multi-language human-readable titles (e.g., display a text for UI representation in different languages)");
                    abstractDS.Add(mlp);
                }
                if (key == ("description"))
                {
                    abstractDS.AddDescription("en", dsjObject["description"].ToString());
                }
                if (key == ("descriptions"))
                {
                    JObject _descriptionsJObject = (JObject)dsjObject["descriptions"];
                    foreach (var x in _descriptionsJObject)
                    {
                        abstractDS.AddDescription((x.Key).ToString(), (x.Value).ToString());
                    }
                }
                if (key == ("const"))
                {
                    abstractDS.qualifiers.Add(createAASQualifier("const", dsjObject["const"].ToString()));
                }
                if (key == ("default"))
                {
                    abstractDS.qualifiers.Add(createAASQualifier("default", dsjObject["default"].ToString()));
                }
                if (key == ("unit"))
                {
                    abstractDS.qualifiers.Add(createAASQualifier("unit", dsjObject["unit"].ToString()));
                }
                if (key == ("oneOf"))
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
                    foreach (JObject ds in dsjObject["oneOf"])
                    {
                        i = i + 1;
                        AdminShell.SubmodelElementCollection _oneOf = BuildAbstractDataSchema(ds, "oneOf" + i.ToString());
                        _oneOf.semanticId = createSemanticID("oneOf");
                        oneOf.Add(_oneOf);
                    }
                    abstractDS.Add(oneOf);
                }
                if (key == ("enum"))
                {
                    AdminShell.SubmodelElementCollection enums = new AdminShell.SubmodelElementCollection();
                    enums.idShort = "enum";
                    enums.category = "PARAMETER";
                    enums.ordered = false;
                    enums.allowDuplicates = false;
                    enums.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                    enums.AddDescription("en", "Restricted set of values provided as an array.");
                    enums.semanticId = createSemanticID("enum");
                    enums.qualifiers = new AdminShell.QualifierCollection();
                    int i = 0;
                    foreach (string ds in dsjObject["enum"])
                    {
                        i = i + 1;
                        AdminShell.Qualifier _enum = createAASQualifier("enum", ds.ToString());
                        _enum.type = "enum" + i.ToString();
                        enums.qualifiers.Add(_enum);
                    }
                    abstractDS.Add(enums);
                }
                if (key == ("readOnly"))
                {
                    abstractDS.qualifiers.Add(createAASQualifier("readOnly", dsjObject["readOnly"].ToString()));
                }
                if (key == ("writeOnly"))
                {
                    abstractDS.qualifiers.Add(createAASQualifier("writeOnly", dsjObject["writeOnly"].ToString()));
                }
                if (key == ("format"))
                {
                    abstractDS.qualifiers.Add(createAASQualifier("format", dsjObject["format"].ToString()));
                }
                if (key == ("type"))
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
        public static AdminShell.SubmodelElementCollection BuildAbstractInteractionAvoidance(JObject jObject, string idShort)
        {
            AdminShell.SubmodelElementCollection _interactionAffordance = BuildAbstractDataSchema(jObject, idShort);
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
                JObject _uriVariablesJObject = (JObject)jObject["uriVariables"];
                foreach (var x in _uriVariablesJObject)
                {
                    JObject _uriVariable = JObject.FromObject(x.Value);
                    AdminShell.SubmodelElementCollection _uriVariableC = BuildAbstractDataSchema(_uriVariable, x.Key.ToString());
                    _uriVariableC.semanticId = createSemanticID("uriVariable");
                    _uriVariables.Add(_uriVariableC);
                }
                _interactionAffordance.Add(_uriVariables);
            }
            if (jObject.ContainsKey("forms"))
            {
                AdminShell.SubmodelElementCollection forms = new AdminShell.SubmodelElementCollection();
                forms.idShort = "forms";
                forms.category = "PARAMETER";
                forms.ordered = false;
                forms.allowDuplicates = false;
                forms.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                forms.AddDescription("en", "Set of form hypermedia controls that describe how an operation can be performed. Forms are serializations of Protocol Bindings");
                forms.semanticId = createSemanticID("forms");
                forms.qualifiers = new AdminShell.QualifierCollection();
                int i = 0;
                foreach (JObject ds in jObject["forms"])
                {
                    i = i + 1;
                    forms.Add(BuildTDForm(ds, "form" + i.ToString()));
                }
                _interactionAffordance.Add(forms);
            }
            return _interactionAffordance;
        }

        // TD Properties
        public static AdminShell.SubmodelElementCollection BuildTDProperty(JObject _propertyJObject, string propertyName)
        {
            AdminShell.SubmodelElementCollection _tdProperty = BuildAbstractInteractionAvoidance(_propertyJObject, propertyName);
            _tdProperty.semanticId = createSemanticID("property");
            if (_propertyJObject.ContainsKey("observable"))
            {
                _tdProperty.qualifiers.Add(createAASQualifier("observable", (_propertyJObject["observable"]).ToString()));
            }
            if (_propertyJObject.ContainsKey("updateFrequencey"))
            {
                _tdProperty.qualifiers.Add(createAASQualifier("updateFrequencey", (_propertyJObject["updateFrequencey"]).ToString()));
            }
            return _tdProperty;
        }
        public static AdminShell.SubmodelElementCollection BuildTDProperties(JObject jObject)
        {
            AdminShell.SubmodelElementCollection tdProperties = new AdminShell.SubmodelElementCollection();
            tdProperties.idShort = "properties";
            tdProperties.category = "PARAMETER";
            tdProperties.ordered = false;
            tdProperties.allowDuplicates = false;
            tdProperties.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            tdProperties.AddDescription("en", "Properties definion of the thing Description");
            tdProperties.semanticId = createSemanticID("properties");
            string _jProperty = (jObject["properties"]).ToString();
            JObject temp = JObject.Parse(_jProperty);
            tdProperties.qualifiers = new AdminShell.QualifierCollection();
            foreach (var x1 in temp)
            {
                string jProperty = (x1.Value).ToString();
                JObject _jObject = JObject.Parse(jProperty);
                tdProperties.Add(BuildTDProperty(_jObject, (x1.Key).ToString()));
            }
            return tdProperties;
        }

        // TD Events
        public static AdminShell.SubmodelElementCollection BuildTDEvent(JObject _eventJObject, string actionName)
        {
            AdminShell.SubmodelElementCollection _tdEvent = BuildAbstractInteractionAvoidance(_eventJObject, actionName);
            _tdEvent.semanticId = createSemanticID("event");
            foreach (var eventElement in _eventJObject)
            {
                string key = eventElement.Key.ToString();
                if (key == ("subscription"))
                {
                    JObject _subscriptiontDS = JObject.Parse((_eventJObject["subscription"]).ToString());
                    AdminShell.SubmodelElementCollection _subscription = BuildAbstractDataSchema(_subscriptiontDS, "subscription");
                    _subscription.semanticId = createSemanticID("subscription");
                    _tdEvent.Add(_subscription);
                }
                if (key == ("data"))
                {
                    JObject _dataDS = JObject.Parse((_eventJObject["data"]).ToString());
                    AdminShell.SubmodelElementCollection _data = (BuildAbstractDataSchema(_dataDS, "data"));
                    _data.semanticId = createSemanticID("subscription");
                    _tdEvent.Add(_data);
                }
                if (key == ("cancellation"))
                {
                    JObject cancellationDS = JObject.Parse((_eventJObject["cancellation"]).ToString());
                    AdminShell.SubmodelElementCollection _cancellation = (BuildAbstractDataSchema(cancellationDS, "cancellation"));
                    _cancellation.semanticId = createSemanticID("cancellation");
                    _tdEvent.Add(_cancellation);
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
            string _jProperty = (jObject["events"]).ToString();
            JObject temp = JObject.Parse(_jProperty);
            tdEvents.qualifiers = new AdminShell.QualifierCollection();
            foreach (var x1 in temp)
            {
                string jProperty = (x1.Value).ToString();
                JObject _jObject = JObject.Parse(jProperty);
                tdEvents.Add(BuildTDEvent(_jObject, (x1.Key).ToString()));
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
            tdActions.qualifiers = new AdminShell.QualifierCollection();
            JObject temp = JObject.FromObject(jObject["actions"]);
            foreach (var x1 in temp)
            {
                string jProperty = (x1.Value).ToString();
                JObject _jObject = JObject.Parse(jProperty);
                tdActions.Add(BuildTDAction(_jObject, (x1.Key).ToString()));
            }
            return tdActions;
        }
        public static AdminShell.SubmodelElementCollection BuildTDAction(JObject _actionJObject, string actionName)
        {
            AdminShell.SubmodelElementCollection _tdAction = BuildAbstractInteractionAvoidance(_actionJObject, actionName);
            _tdAction.semanticId = createSemanticID("action");
            foreach (var actionELement in _actionJObject)
            {
                string key = actionELement.Key.ToString();
                if (key == ("input"))
                {
                    JObject _inputDS = JObject.Parse((_actionJObject["input"]).ToString());
                    AdminShell.SubmodelElementCollection _input = BuildAbstractDataSchema(_inputDS, "input");
                    _input.semanticId = createSemanticID("input");
                    _tdAction.Add(_input);
                }
                if (key == ("output"))
                {
                    JObject _outputDS = JObject.Parse((_actionJObject["output"]).ToString());
                    AdminShell.SubmodelElementCollection _output = (BuildAbstractDataSchema(_outputDS, "output"));
                    _output.semanticId = createSemanticID("output");
                    _tdAction.Add(_output);
                }
                if (key == ("safe"))
                {
                    _tdAction.qualifiers.Add(createAASQualifier("safe", _actionJObject["safe"].ToString()));
                }
                if (key == ("idempotent"))
                {
                    _tdAction.qualifiers.Add(createAASQualifier("idempotent", _actionJObject["idempotent"].ToString()));
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
            foreach (var linkElement in linkJObject)
            {
                string key = linkElement.Key.ToString();
                if (key == ("href"))
                {
                    _tdLink.qualifiers.Add(createAASQualifier("href", linkJObject["href"].ToString()));
                }
                if (key == ("type"))
                {
                    _tdLink.qualifiers.Add(createAASQualifier("type", linkJObject["type"].ToString()));
                }
                if (key == ("rel"))
                {
                    _tdLink.qualifiers.Add(createAASQualifier("rel", linkJObject["rel"].ToString()));
                }
                if (key == ("anchor"))
                {
                    _tdLink.qualifiers.Add(createAASQualifier("anchor", linkJObject["anchor"].ToString()));
                }
                if (key == ("sizes"))
                {
                    _tdLink.qualifiers.Add(createAASQualifier("sizes", linkJObject["sizes"].ToString()));
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
            tdLinks.AddDescription("en", "	Provides Web links to arbitrary resources that relate to the specified Thing Description.");
            tdLinks.semanticId = createSemanticID("links");
            tdLinks.qualifiers = new AdminShell.QualifierCollection();
            int index = 1;
            foreach (JObject ds in jObject["links"])
            {
                tdLinks.Add(BuildTDLink(ds, "link" + index.ToString()));
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
                JObject _descriptionsJObject = (JObject)jObject["descriptions"];
                foreach (var x in _descriptionsJObject)
                {
                    _securityDefinition.AddDescription((x.Key).ToString(), (x.Value).ToString());
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
            string _jProperty = (jObject["securityDefinitions"]).ToString();
            JObject temp = JObject.Parse(_jProperty);
            foreach (var x1 in temp)
            {
                string jProperty = (x1.Value).ToString();
                JObject _jObject = JObject.Parse(jProperty);
                _securityDefinitions.Add(BuildSecurityDefinition(_jObject, (x1.Key).ToString()));
            }
            return _securityDefinitions;
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
                if (false)
                {
                    //ValidateTDJson(tdJObject)
                    exportData["status"] = "error";
                    exportData["error"] = "The JSON LD document is not valid";
                    return exportData;
                }
                else
                {
                    sm.qualifiers = new AdminShell.QualifierCollection();
                    foreach (var tdkey in tdJObject)
                    {
                        string key = tdkey.Key.ToString();
                        if (key == ("@context"))
                        {
                            if ((tdJObject["@context"].Type).ToString() == "String")
                            {
                                sm.qualifiers.Add(createAASQualifier("@context", tdJObject["@context"].ToString()));
                            }
                            if ((tdJObject["@context"].Type).ToString() == "Array")
                            {
                                AdminShell.SubmodelElementCollection _context = new AdminShell.SubmodelElementCollection();
                                _context.idShort = "@context";
                                _context.category = "PARAMETER";
                                _context.ordered = false;
                                _context.allowDuplicates = false;
                                _context.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                                _context.AddDescription("en", "JSON-LD keyword to label the object with semantic tags ");
                                _context.semanticId = createSemanticID("@context");
                                _context.qualifiers = new AdminShell.QualifierCollection();

                                foreach (var x in tdJObject["@context"])
                                {
                                    if ((x.Type).ToString() == "String")
                                    {
                                        _context.qualifiers.Add(createAASQualifier("@context", x.ToString()));
                                    }
                                    else
                                    {
                                        try
                                        {
                                            JObject _contextJobject = JObject.FromObject(x);
                                            foreach (var y in _contextJobject)
                                            {
                                                _context.qualifiers.Add(createAASQualifier((y.Key).ToString(), (y.Value).ToString()));
                                            }
                                        }
                                        catch
                                        {

                                        }

                                    }
                                }
                                sm.Add(_context);
                            }
                        }
                        if (key == ("@type"))
                        {
                            if ((tdJObject["@type"].Type).ToString() == "String")
                            {
                                sm.qualifiers.Add(createAASQualifier("@type", tdJObject["@type"].ToString()));
                            }
                            if ((tdJObject["@type"].Type).ToString() == "Array")
                            {
                                AdminShell.SubmodelElementCollection _types = new AdminShell.SubmodelElementCollection();
                                _types.idShort = "@type";
                                _types.category = "PARAMETER";
                                _types.ordered = false;
                                _types.allowDuplicates = false;
                                _types.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                                _types.AddDescription("en", "JSON-LD keyword to label the object with semantic tags (or types).");
                                _types.semanticId = createSemanticID("@type");
                                _types.qualifiers = new AdminShell.QualifierCollection();
                                int index = 1;
                                foreach (var x in tdJObject["@type"])
                                {
                                    _types.qualifiers.Add(createAASQualifier("@type" + (index).ToString(), (x).ToString()));
                                    index = index + 1;
                                }
                                sm.Add(_types);
                            }
                        }
                        if (key == ("id"))
                        {
                            string id = tdJObject["id"].ToString();
                            sm.SetIdentification("IRI", id);
                            smref.First.idType = "IRI";
                            smref.First.value = id;
                        }
                        if (key == ("title"))
                        {
                            sm.qualifiers.Add(createAASQualifier("title", tdJObject["title"].ToString()));
                        }
                        if (key == ("titles"))
                        {
                            JObject _titlesJObject = (JObject)tdJObject["titles"];
                            List<AdminShellV20.LangStr> titleList2 = new List<AdminShellV20.LangStr>();
                            foreach (var x in _titlesJObject)
                            {
                                AdminShellV20.LangStr title = new AdminShellV20.LangStr((x.Key).ToString(), (x.Value).ToString());
                                titleList2.Add(title);
                            }
                            AdminShell.MultiLanguageProperty mlp = BuildMultiLanguageProperty("titles", titleList2, "Provides multi-language human-readable titles (e.g., display a text for UI representation in different languages)");
                            sm.Add(mlp);
                        }
                        if (key == ("description"))
                        {
                            sm.AddDescription("en", tdJObject["description"].ToString());
                        }
                        if (key == ("descriptions"))
                        {
                            JObject _descriptionsJObject = (JObject)tdJObject["descriptions"];
                            foreach (var x in _descriptionsJObject)
                            {
                                sm.AddDescription((x.Key).ToString(), (x.Value).ToString());

                            }
                        }
                        if (key == ("version"))
                        {
                            string version = tdJObject["version"].ToString();
                            JObject versionObject = JObject.Parse(version);
                            string intance = "";
                            string model = "";
                            if (versionObject.ContainsKey("instance"))
                            { intance = (versionObject["instance"]).ToString(); }
                            if (versionObject.ContainsKey("model"))
                            { model = (versionObject["model"]).ToString(); }
                            sm.SetAdminstration(intance, model);
                        }
                        if (key == ("created"))
                        {
                            sm.qualifiers.Add(createAASQualifier("created", tdJObject["created"].ToString()));
                        }
                        if (key == ("modified"))
                        {
                            sm.qualifiers.Add(createAASQualifier("modified", tdJObject["modified"].ToString()));
                        }
                        if (key == ("support"))
                        {
                            sm.qualifiers.Add(createAASQualifier("support", tdJObject["support"].ToString()));
                        }
                        if (key == ("base"))
                        {
                            sm.qualifiers.Add(createAASQualifier("base", tdJObject["base"].ToString()));
                        }
                        if (key == ("properties"))
                        {
                            AdminShell.SubmodelElementCollection porperties = BuildTDProperties(tdJObject);
                            sm.Add(porperties);
                        }
                        if (key == ("actions"))
                        {
                            AdminShell.SubmodelElementCollection actions = BuildTDActions(tdJObject);
                            sm.Add(actions);
                        }
                        if (key == ("events"))
                        {
                            AdminShell.SubmodelElementCollection events = BuildTDEvents(tdJObject);
                            sm.Add(events);
                        }
                        if (key == ("links"))
                        {
                            AdminShell.SubmodelElementCollection links = BuildTDLinks(tdJObject);
                            sm.Add(links);
                        }
                        if (key == ("forms"))
                        {
                            AdminShell.SubmodelElementCollection forms = new AdminShell.SubmodelElementCollection();
                            forms.idShort = "forms";
                            forms.category = "PARAMETER";
                            forms.ordered = false;
                            forms.allowDuplicates = false;
                            forms.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                            forms.qualifiers = new AdminShell.QualifierCollection();
                            forms.AddDescription("en", "Set of form hypermedia controls that describe how an operation can be performed. Forms are serializations of Protocol Bindings");
                            int i = 0;
                            foreach (JObject ds in tdJObject["forms"])
                            {
                                forms.Add(BuildTDForm(ds, "Form" + i.ToString()));
                            }
                            sm.Add(forms);
                        }
                        if (key == ("security"))
                        {
                            if ((tdJObject["security"].Type).ToString() == "String")
                            {
                                sm.qualifiers.Add(createAASQualifier("security", tdJObject["security"].ToString()));
                            }
                            if ((tdJObject["security"].Type).ToString() == "Array")
                            {
                                AdminShell.SubmodelElementCollection _security = new AdminShell.SubmodelElementCollection();
                                _security.idShort = "security";
                                _security.category = "PARAMETER";
                                _security.ordered = false;
                                _security.allowDuplicates = false;
                                _security.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                                _security.AddDescription("en", "security definition names");
                                _security.semanticId = createSemanticID("security");
                                _security.qualifiers = new AdminShell.QualifierCollection();
                                int index = 1;
                                foreach (var x in tdJObject["security"])
                                {
                                    AdminShell.Qualifier securityQual = createAASQualifier("security", (x).ToString());
                                    securityQual.type = "security" + index.ToString();
                                    _security.qualifiers.Add(securityQual);
                                    index = index + 1;
                                }
                                sm.Add(_security);
                            }
                        }
                        if (key == ("securityDefinitions"))
                        {
                            AdminShell.SubmodelElementCollection _securityDefintions = BuildTDSecurityDefinitions(tdJObject);
                            sm.Add(_securityDefintions);
                        }
                        if (key == ("profile"))
                        {
                            if ((tdJObject["profile"].Type).ToString() == "String")
                            {
                                AdminShell.Qualifier _profileQualifier = createAASQualifier("profile", tdJObject["profile"].ToString());
                                sm.qualifiers.Add(_profileQualifier);
                            }
                            if ((tdJObject["profile"].Type).ToString() == "Array")
                            {
                                AdminShell.SubmodelElementCollection _profile = new AdminShell.SubmodelElementCollection();
                                _profile.idShort = "profile";
                                _profile.category = "PARAMETER";
                                _profile.ordered = false;
                                _profile.allowDuplicates = false;
                                _profile.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                                _profile.AddDescription("en", "Indicates the WoT Profile mechanisms followed by this Thing Description and the corresponding Thing implementation.");
                                _profile.qualifiers = new AdminShell.QualifierCollection();
                                int index = 1;
                                foreach (var x in tdJObject["profile"])
                                {
                                    AdminShell.Qualifier _profileQual = createAASQualifier("profile", (x).ToString());
                                    _profileQual.type = "profile" + index.ToString();
                                    _profile.qualifiers.Add(_profileQual);
                                    index = index + 1;
                                }
                                sm.Add(_profile);
                            }
                        }
                        if (key == ("schemaDefinitions"))
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
                                _schemaDefinitions.value.Add(BuildAbstractDataSchema(_schemaDefinition, sdKey.ToString()));
                            }
                            sm.Add(_schemaDefinitions);
                        }
                        sm.semanticId = createSemanticID("Thing");
                    }

                    exportData["status"] = "Success";
                    return exportData;
                }
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
