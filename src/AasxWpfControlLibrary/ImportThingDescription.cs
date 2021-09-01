/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

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
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;

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
        public static Tuple<AdminShell.SubmodelElementCollection, AdminShell.QualifierCollection> BuildArraySchema( AdminShell.SubmodelElementCollection dsCollection, JObject jObject)
        {
            AdminShell.QualifierCollection abDSQualifier = new AdminShell.QualifierCollection();
            if (jObject.ContainsKey("minItems"))
            {
                AdminShell.Qualifier _minItems = new AdminShell.Qualifier();
                _minItems.type = "minItems";
                _minItems.value = jObject["minItems"].ToString();
                abDSQualifier.Add(_minItems);
            }
            if (jObject.ContainsKey("maxItems"))
            {
                AdminShell.Qualifier _maxItems = new AdminShell.Qualifier();
                _maxItems.type = "maxItems";
                _maxItems.value = jObject["maxItems"].ToString();
                abDSQualifier.Add(_maxItems);
            }
            if (jObject.ContainsKey("items"))
            {
                AdminShell.SubmodelElementCollection items = new AdminShell.SubmodelElementCollection();
                if ((jObject["items"].Type).ToString() == "Array")
                {
                    int i = 0;
                    foreach (var x in jObject["items"])
                    {
                        string jProperty = x.ToString();
                        JObject _jObject = JObject.Parse(jProperty);
                        AdminShell.SubmodelElementCollection item = BuildAbstractDataSchema(_jObject, "item" + (i).ToString(), "Used to define the characteristics of an array.");
                        i = i + 1;
                        items.Add(item);
                    }
                }
                else
                {
                    string jItem = (jObject["items"]).ToString();
                    JObject _jObject = JObject.Parse(jItem);
                    AdminShell.SubmodelElementCollection item = BuildAbstractDataSchema(_jObject, "item1", "Used to define the characteristics of an array.");
                    items.Add(item);
                }
                items.idShort = "items";
                dsCollection.Add(items);
            }

            return Tuple.Create(dsCollection, abDSQualifier);
        }
        public static AdminShell.QualifierCollection BuildNumberSchema(AdminShell.QualifierCollection adsQualifierCollection, JObject jObject)
        {
            if (jObject.ContainsKey("minimum"))
            {
                AdminShell.Qualifier _minimum = new AdminShell.Qualifier();
                _minimum.type = "minimum";
                _minimum.value = jObject["minimum"].ToString();
                adsQualifierCollection.Add(_minimum);
            }
            if (jObject.ContainsKey("exclusiveMinimum"))
            {
                AdminShell.Qualifier _exclusiveMinimum = new AdminShell.Qualifier();
                _exclusiveMinimum.type = "maxItems";
                _exclusiveMinimum.value = jObject["exclusiveMinimum"].ToString();
                adsQualifierCollection.Add(_exclusiveMinimum);
            }
            if (jObject.ContainsKey("maximum"))
            {
                AdminShell.Qualifier _maximum = new AdminShell.Qualifier();
                _maximum.type = "maximum";
                _maximum.value = jObject["maximum"].ToString();
                adsQualifierCollection.Add(_maximum);
            }
            if (jObject.ContainsKey("exclusiveMaximum"))
            {
                AdminShell.Qualifier _exclusiveMaximum = new AdminShell.Qualifier();
                _exclusiveMaximum.type = "exclusiveMaximum";
                _exclusiveMaximum.value = jObject["exclusiveMaximum"].ToString();
                adsQualifierCollection.Add(_exclusiveMaximum);
            }
            if (jObject.ContainsKey("multipleOf"))
            {
                AdminShell.Qualifier _multipleOf = new AdminShell.Qualifier();
                _multipleOf.type = "multipleOf";
                _multipleOf.value = jObject["multipleOf"].ToString();
                adsQualifierCollection.Add(_multipleOf);
            }
            return adsQualifierCollection;
        }
        public static AdminShell.QualifierCollection BuildIntegerSchema(AdminShell.QualifierCollection adsQualifierCollection, JObject jObject)
        {
            if (jObject.ContainsKey("minimum"))
            {
                AdminShell.Qualifier _minimum = new AdminShell.Qualifier();
                _minimum.type = "minimum";
                _minimum.value = jObject["minimum"].ToString();
                adsQualifierCollection.Add(_minimum);
            }
            if (jObject.ContainsKey("exclusiveMinimum"))
            {
                AdminShell.Qualifier _exclusiveMinimum = new AdminShell.Qualifier();
                _exclusiveMinimum.type = "maxItems";
                _exclusiveMinimum.value = jObject["exclusiveMinimum"].ToString();
                adsQualifierCollection.Add(_exclusiveMinimum);
            }
            if (jObject.ContainsKey("maximum"))
            {
                AdminShell.Qualifier _maximum = new AdminShell.Qualifier();
                _maximum.type = "maximum";
                _maximum.value = jObject["maximum"].ToString();
                adsQualifierCollection.Add(_maximum);
            }
            if (jObject.ContainsKey("exclusiveMaximum"))
            {
                AdminShell.Qualifier _exclusiveMaximum = new AdminShell.Qualifier();
                _exclusiveMaximum.type = "exclusiveMaximum";
                _exclusiveMaximum.value = jObject["exclusiveMaximum"].ToString();
                adsQualifierCollection.Add(_exclusiveMaximum);
            }
            if (jObject.ContainsKey("multipleOf"))
            {
                AdminShell.Qualifier _multipleOf = new AdminShell.Qualifier();
                _multipleOf.type = "multipleOf";
                _multipleOf.value = jObject["multipleOf"].ToString();
                adsQualifierCollection.Add(_multipleOf);
            }
            return adsQualifierCollection;
        }
        public static AdminShell.QualifierCollection BuildStringSchema(AdminShell.QualifierCollection adsQualifierCollection, JObject jObject)
        {
            if (jObject.ContainsKey("minLength"))
            {
                AdminShell.Qualifier _minLength = new AdminShell.Qualifier();
                _minLength.type = "minLength";
                _minLength.value = jObject["minLength"].ToString();
                adsQualifierCollection.Add(_minLength);
            }
            if (jObject.ContainsKey("maxLength"))
            {
                AdminShell.Qualifier _maxLength = new AdminShell.Qualifier();
                _maxLength.type = "maxLength";
                _maxLength.value = jObject["maxLength"].ToString();
                adsQualifierCollection.Add(_maxLength);
            }
            if (jObject.ContainsKey("pattern"))
            {
                AdminShell.Qualifier _pattern = new AdminShell.Qualifier();
                _pattern.type = "pattern";
                _pattern.value = jObject["pattern"].ToString();
                adsQualifierCollection.Add(_pattern);
            }
            if (jObject.ContainsKey("contentEncoding"))
            {
                AdminShell.Qualifier _contentEncoding = new AdminShell.Qualifier();
                _contentEncoding.type = "contentEncoding";
                _contentEncoding.value = jObject["contentEncoding"].ToString();
                adsQualifierCollection.Add(_contentEncoding);
            }
            if (jObject.ContainsKey("contentMediaType"))
            {
                AdminShell.Qualifier _contentMediaType = new AdminShell.Qualifier();
                _contentMediaType.type = "contentMediaType";
                _contentMediaType.value = jObject["contentMediaType"].ToString();
                adsQualifierCollection.Add(_contentMediaType);
            }
            return adsQualifierCollection;
        }
        public static AdminShell.SubmodelElementCollection BuildObjectSchema(AdminShell.SubmodelElementCollection dsCollection, JObject jObject)
        {
            if (jObject.ContainsKey("properties"))
            {
                dsCollection.Add(BuildTDProperties(jObject));
            }
            if (jObject.ContainsKey("required"))
            {
                AdminShell.SubmodelElementCollection requireds = new AdminShell.SubmodelElementCollection();
                requireds.idShort = "required";
                requireds.AddDescription("en", "Defines which members of the object type are mandatory.");
                int i = 1;
                foreach (var x in jObject["required"])
                {
                    string jrequired = x.ToString();
                    AdminShell.Property _required = buildaasProperty("required"+(i).ToString(), jrequired,"optionasl / mandatory");
                    i = i + 1;
                    requireds.Add(_required);
                }
                dsCollection.Add(requireds);
            }
            return dsCollection;
        }

        // TD DataSchema
        public static AdminShell.SubmodelElementCollection BuildAbstractDataSchema(JObject jObject, string idShort, string description)
        {
            AdminShell.SubmodelElementCollection abstractDS = new AdminShell.SubmodelElementCollection();
            abstractDS.idShort = idShort;
            abstractDS.category = "PARAMETER";
            abstractDS.ordered = false;
            abstractDS.allowDuplicates = false;
            abstractDS.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            abstractDS.AddDescription("en", description);
            AdminShell.QualifierCollection abDSQualifier = new AdminShell.QualifierCollection();
            if (jObject.ContainsKey("@type")) // needs to be discussed with Mr. Sebastian. When input is an array 
                                              // requires an example
            {
                AdminShell.Qualifier _type = new AdminShell.Qualifier();
                _type.type = "@type";
                _type.value = jObject["@type"].ToString();
                abDSQualifier.Add(_type);
            }
            if (jObject.ContainsKey("title"))
            {
                AdminShell.Qualifier _type = new AdminShell.Qualifier();
                _type.type = "title";
                _type.value = jObject["title"].ToString();
                abDSQualifier.Add(_type);
            }
            if (jObject.ContainsKey("titles"))
            {
                JObject _titlesJObject = (JObject)jObject["titles"];
                List<AdminShellV20.LangStr> titleList2 = new List<AdminShellV20.LangStr>();
                foreach (var x in _titlesJObject)
                {
                    AdminShellV20.LangStr title = new AdminShellV20.LangStr((x.Key).ToString(), (x.Value).ToString());
                    titleList2.Add(title);
                }
                AdminShell.MultiLanguageProperty mlp = BuildMultiLanguageProperty("titles", titleList2, "Provides multi-language human-readable titles (e.g., display a text for UI representation in different languages)");
                abstractDS.Add(mlp);
            }
            if (jObject.ContainsKey("description"))
            {
                abstractDS.AddDescription("en", jObject["description"].ToString());
            }
            if (jObject.ContainsKey("descriptions"))
            {
                JObject _descriptionsJObject = (JObject)jObject["descriptions"];
                foreach (var x in _descriptionsJObject)
                {
                    abstractDS.AddDescription((x.Key).ToString(), (x.Value).ToString());
                }
            }
            if (jObject.ContainsKey("const"))
            {

            }
            if (jObject.ContainsKey("default"))
            {

            }
            if (jObject.ContainsKey("unit"))
            {
                AdminShell.Qualifier _unit = new AdminShell.Qualifier();
                _unit.type = "unit";
                _unit.value = jObject["unit"].ToString();
                abDSQualifier.Add(_unit);
            }
            if (jObject.ContainsKey("oneOf"))
            {
                AdminShell.SubmodelElementCollection oneOf = new AdminShell.SubmodelElementCollection();
                oneOf.idShort = "oneOf";
                oneOf.category = "PARAMETER";
                oneOf.ordered = false;
                oneOf.allowDuplicates = false;
                oneOf.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                oneOf.AddDescription("en", "Used to ensure that the data is valid against one of the specified schemas in the array.");
                int i = 0;
                foreach (JObject ds in jObject["oneOf"])
                {
                    i = i + 1;
                    oneOf.Add(BuildAbstractDataSchema(ds,"oneOf" + i.ToString(), "Data Schemas"));
                }
                abstractDS.Add(oneOf);
            }
            if (jObject.ContainsKey("enum"))
            {
                AdminShell.SubmodelElementCollection enums = new AdminShell.SubmodelElementCollection();
                enums.idShort = "enum";
                enums.category = "PARAMETER";
                enums.ordered = false;
                enums.allowDuplicates = false;
                enums.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                enums.AddDescription("en", "Restricted set of values provided as an array.");
                int i = 0;
                foreach (string ds in jObject["enum"])
                {
                    i = i + 1;
                    enums.Add(buildaasProperty("enum" + i.ToString(), ds.ToString(),""));
                }
                abstractDS.Add(enums);
            }
            if (jObject.ContainsKey("readOnly"))
            {
                AdminShell.Qualifier _readOnly = new AdminShell.Qualifier();
                _readOnly.type = "readOnly";
                _readOnly.value = jObject["readOnly"].ToString();
                abDSQualifier.Add(_readOnly);
            }
            if (jObject.ContainsKey("writeOnly"))
            {
                AdminShell.Qualifier _writeOnly = new AdminShell.Qualifier();
                _writeOnly.type = "writeOnly";
                _writeOnly.value = jObject["writeOnly"].ToString();
                abDSQualifier.Add(_writeOnly);
            }
            if (jObject.ContainsKey("format"))
            {
                AdminShell.Qualifier _format = new AdminShell.Qualifier();
                _format.type = "format";
                _format.value = jObject["format"].ToString();
                abDSQualifier.Add(_format);
            }
            if (jObject.ContainsKey("type"))
            {
                AdminShell.Qualifier _type = new AdminShell.Qualifier();
                _type.type = "type";
                string dsType = jObject["type"].ToString();
                _type.value = dsType;
                abDSQualifier.Add(_type);
                if (dsType == "array")
                {
                    AdminShell.QualifierCollection arraySchemaQualifier = new AdminShell.QualifierCollection();
                    (abstractDS, arraySchemaQualifier) = BuildArraySchema(abstractDS, jObject);
                    foreach (AdminShell.Qualifier asQ in arraySchemaQualifier)
                    {
                        abDSQualifier.Add(asQ);
                    }
                }
                if (dsType == "number")
                {
                    abDSQualifier = BuildNumberSchema(abDSQualifier, jObject);
                }
                if (dsType == "integer")
                {
                    abDSQualifier = BuildIntegerSchema(abDSQualifier, jObject);
                }
                if (dsType == "string")
                {
                    abDSQualifier = BuildStringSchema(abDSQualifier, jObject);
                }
                if (dsType == "object")
                {
                    (abstractDS) = BuildObjectSchema(abstractDS, jObject);
                }
            }
            abstractDS.qualifiers = abDSQualifier;
            return abstractDS;
        }
        
        // AAS Submodel Property
        public static AdminShell.Property buildaasProperty(string idShort, string value, string description)
        {
            AdminShell.Property submodeProperty = new AdminShell.Property();
            submodeProperty.idShort = idShort;
            submodeProperty.value = value;
            submodeProperty.category = "PARAMETER";
            submodeProperty.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            submodeProperty.AddDescription("en", description);
            return submodeProperty;
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

        public static AdminShell.SubmodelElementCollection BuildAdditionalResponse(JObject jobject,string idshort)
        {
            AdminShell.SubmodelElementCollection arCollection = new AdminShell.SubmodelElementCollection();
            arCollection.idShort = idshort;
            arCollection.category = "PARAMETER";
            arCollection.ordered = false;
            arCollection.allowDuplicates = false;
            arCollection.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            arCollection.AddDescription("en", "Communication metadata describing the expected response message for additional responses.");
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
        public static AdminShell.SubmodelElementCollection BuildTDForm(JObject jObject,string idShort)
        {
            AdminShell.SubmodelElementCollection tdForm = new AdminShell.SubmodelElementCollection();
            tdForm.idShort = idShort;
            tdForm.category = "PARAMETER";
            tdForm.ordered = false;
            tdForm.allowDuplicates = false;
            tdForm.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            tdForm.AddDescription("en", "Hypermedia controls that describe how an operation can be performed. Form is a  serializations of Protocol Bindings");
            AdminShell.QualifierCollection formQualifier = new AdminShell.QualifierCollection();
            List<string> formElements = new List<string> { "href", "contentType", "contentCoding", "security", "scopes", "op", "response", "additionalResponses" };
            if (jObject.ContainsKey("href"))
            {
                AdminShell.Qualifier _type = new AdminShell.Qualifier();
                _type.type = "href";
                _type.value = jObject["href"].ToString();
                formQualifier.Add(_type);
            }
            if (jObject.ContainsKey("contentType"))
            {
                AdminShell.Qualifier _type = new AdminShell.Qualifier();
                _type.type = "contentType";
                _type.value = jObject["contentType"].ToString();
                formQualifier.Add(_type);
            }

            if (jObject.ContainsKey("contentCoding"))
            {
                AdminShell.Qualifier _type = new AdminShell.Qualifier();
                _type.type = "contentType";
                _type.value = jObject["contentType"].ToString();
                formQualifier.Add(_type);
            }
            if (jObject.ContainsKey("security"))
            {
                AdminShell.SubmodelElementCollection _security = new AdminShell.SubmodelElementCollection();
                _security.idShort = "security";
                _security.category = "PARAMETER";
                _security.ordered = false;
                _security.allowDuplicates = false;
                _security.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                _security.AddDescription("en", "Set of security definition names, chosen from those defined in securityDefinitions. These must all be satisfied for access to resources.");
                if ((jObject["security"].Type).ToString() == "String")
                {
                    AdminShell.Qualifier _securityName = new AdminShell.Qualifier();
                    _securityName.type = "security";
                    _securityName.value = jObject["security"].ToString();
                    formQualifier.Add(_securityName);
                }
                if ((jObject["security"].Type).ToString() == "Array")
                {
                    int index = 0;
                    foreach (var x in jObject["security"])
                    {
                        _security.Add(buildaasProperty("security" + index.ToString(), (x).ToString(), "security definition name"));
                        index = index + 1;
                    }
                }
                tdForm.Add(_security);
            }
            if (jObject.ContainsKey("scopes"))
            {
                if ((jObject["scopes"].Type).ToString() == "String")
                {
                    AdminShell.Qualifier _securityName = new AdminShell.Qualifier();
                    _securityName.type = "scopes";
                    _securityName.value = jObject["scopes"].ToString();
                    formQualifier.Add(_securityName);
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

                    int index = 0;
                    foreach (var x in jObject["scopes"])
                    {
                        _scopes.Add(buildaasProperty("scopes" + index.ToString(), (x).ToString(), "Authorization scope identifier"));
                        index = index + 1;
                    }
                    tdForm.Add(_scopes);
                }
            }
            if (jObject.ContainsKey("response"))
            {
                AdminShell.SubmodelElementCollection _response = new AdminShell.SubmodelElementCollection();
                _response.idShort = "response";
                _response.category = "PARAMETER";
                _response.ordered = false;
                _response.allowDuplicates = false;
                _response.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                _response.AddDescription("en", "This optional term can be used if, e.g., the output communication metadata differ from input metadata (e.g., output contentType differ from the input contentType). The response name contains metadata that is only valid for the primary response messages.");
                JObject contentTypeJObject = JObject.FromObject(jObject["response"]);
                _response.Add(BuildTDProperty(contentTypeJObject, "contentType"));
            }
            if (jObject.ContainsKey("additionalResponses"))
            {
                AdminShell.SubmodelElementCollection _response = new AdminShell.SubmodelElementCollection();
                _response.idShort = "additionalResponses";
                _response.category = "PARAMETER";
                _response.ordered = false;
                _response.allowDuplicates = false;
                _response.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                _response.AddDescription("en", "This optional term can be used if additional expected responses are possible, e.g. for error reporting. Each additional response needs to be distinguished from others in some way (for example, by specifying a protocol-specific error code), and may also have its own data schema");
                if ((jObject["additionalResponses"].Type).ToString() == "String")
                {
                    JObject aRJObject = JObject.FromObject(jObject["additionalResponses"]);
                    _response.Add(BuildAdditionalResponse(aRJObject, "additionalResponse1"));
                }
                else if ((jObject["additionalResponses"].Type).ToString() == "Array")
                {
                    int index = 0;
                    foreach (JObject arJObject in jObject["additionalResponses"])
                    {
                        index = index + 1;
                        _response.Add(BuildAdditionalResponse(arJObject, "additionalResponse" + index.ToString()));
                    }
                }
                tdForm.Add(_response);
            }
            if (jObject.ContainsKey("op"))
            {
                if ((jObject["op"].Type).ToString() == "String")
                {
                    AdminShell.Qualifier _type = new AdminShell.Qualifier();
                    _type.type = "op";
                    _type.value = jObject["op"].ToString();
                    formQualifier.Add(_type);
                }
                if ((jObject["op"].Type).ToString() == "Array")
                {
                    AdminShell.SubmodelElementCollection _op = new AdminShell.SubmodelElementCollection();
                    _op.idShort = "op";
                    _op.category = "PARAMETER";
                    _op.ordered = false;
                    _op.allowDuplicates = false;
                    _op.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                    _op.AddDescription("en", "	Indicates the semantic intention of performing the operation(s) described by the form. For example, the Property interaction allows get and set operations. The protocol binding may contain a form for the get operation and a different form for the set operation. The op attribute indicates which form is for which and allows the client to select the correct form for the operation required. op can be assigned one or more interaction verb(s) each representing a semantic intention of an operation.");

                    int index = 0;
                    foreach (var x in jObject["op"])
                    {
                        _op.Add(buildaasProperty("op" + index.ToString(), (x).ToString(), "Semantic intention of performing the operation"));
                        index = index + 1;
                    }
                    tdForm.Add(_op);
                }
            }
            if (jObject.ContainsKey("subprotocol"))
            {
                AdminShell.Qualifier _type = new AdminShell.Qualifier();
                _type.type = "subprotocol";
                _type.value = jObject["subprotocol"].ToString();
                formQualifier.Add(_type);
            }

            foreach (var x in jObject)
            {
                string key = x.Key.ToString();
                if (!formElements.Contains(key))
                {
                    tdForm.Add(buildaasProperty(key, (x.Value).ToString(), ""));
                }
            }
            tdForm.qualifiers = formQualifier;
            return tdForm;
        }
        
        // TD Interaction Avoidance
        public static AdminShell.SubmodelElementCollection BuildAbstractInteractionAvoidance(JObject jObject, string idShort, string description)
        {
            AdminShell.SubmodelElementCollection _interactionAffordance = BuildAbstractDataSchema( jObject, idShort, description);
            if (jObject.ContainsKey("uriVariables"))
            {
                      AdminShell.SubmodelElementCollection _uriVariables = new AdminShell.SubmodelElementCollection();
                      _uriVariables.idShort = "uriVariables";
                      _uriVariables.category = "PARAMETER";
                      _uriVariables.ordered = false;
                      _uriVariables.allowDuplicates = false;
                      _uriVariables.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                      _uriVariables.AddDescription("en", "Used to ensure that the data is valid against one of the specified schemas in the array.");
                      JObject _uriVariablesJObject = (JObject)jObject["uriVariables"];
                      foreach (var x in _uriVariablesJObject)
                      {
                          JObject _uriVariable = JObject.FromObject(x.Value);
                          _uriVariables.value.Add(BuildAbstractDataSchema(_uriVariable, x.Key.ToString(), "	Define URI query template variables as collection based on DataSchema declarations. The individual variables DataSchema cannot be an ObjectSchema or an ArraySchema."));
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
                int i = 0;
                foreach (JObject ds in jObject["forms"])
                {
                    i = i + 1;
                    forms.Add(BuildTDForm(ds,"Form"+i.ToString()));
                }
                _interactionAffordance.Add(forms);
            }
            return _interactionAffordance;
        }
        
        // TD Properties
        public static AdminShell.SubmodelElementCollection BuildTDProperty(JObject _propertyJObject, string propertyName)
        {
            AdminShell.SubmodelElementCollection _tdProperty = BuildAbstractInteractionAvoidance(_propertyJObject, propertyName, "An Interaction Affordance that exposes state of the Thing");
            if (_propertyJObject.ContainsKey("observable"))
            {
                _tdProperty.Add(buildaasProperty("observable", (_propertyJObject["observable"]).ToString(), "A hint that indicates whether Servients hosting the Thing and Intermediaries should provide a Protocol Binding that supports the observeproperty and unobserveproperty operations for this Property."));
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
            string _jProperty = (jObject["properties"]).ToString();
            JObject temp = JObject.Parse(_jProperty);
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
            AdminShell.SubmodelElementCollection _tdEvent = BuildAbstractInteractionAvoidance(_eventJObject, actionName, "An Interaction Affordance that exposes state of the Thing");
            if (_eventJObject.ContainsKey("subscription"))
            {
                JObject _subscriptiontDS = JObject.Parse((_eventJObject["subscription"]).ToString());
                _tdEvent.Add(BuildAbstractDataSchema(_subscriptiontDS, "subscription", "Defines data that needs to be passed upon subscription, e.g., filters or message format for setting up Webhooks."));
            }
            if (_eventJObject.ContainsKey("data"))
            {
                JObject _dataDS = JObject.Parse((_eventJObject["data"]).ToString());
                _tdEvent.Add(BuildAbstractDataSchema(_dataDS, "data", "Defines the data schema of the Event instance messages pushed by the Thing."));
            }
            if (_eventJObject.ContainsKey("cancellation"))
            {
                JObject cancellationDS = JObject.Parse((_eventJObject["cancellation"]).ToString());
                _tdEvent.Add(BuildAbstractDataSchema(cancellationDS, "cancellation", "Defines any data that needs to be passed to cancel a subscription, e.g., a specific message to remove a Webhook."));
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
            string _jProperty = (jObject["events"]).ToString();
            JObject temp = JObject.Parse(_jProperty);
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
            AdminShell.SubmodelElementCollection _tdAction = BuildAbstractInteractionAvoidance(_actionJObject, actionName, "An Interaction Affordance that exposes state of the Thing");
            if (_actionJObject.ContainsKey("input"))
            {
                JObject _inputDS = JObject.Parse((_actionJObject["input"]).ToString());
                _tdAction.Add(BuildAbstractDataSchema(_inputDS, "input", "Used to define the input data schema of the Action."));
            }
            if (_actionJObject.ContainsKey("output"))
            {
                JObject _outputDS = JObject.Parse((_actionJObject["output"]).ToString());
                _tdAction.Add(BuildAbstractDataSchema(_outputDS, "output", "Used to define the output data schema of the Action."));
            }
            if (_actionJObject.ContainsKey("safe"))
            {
                _tdAction.AddQualifier("safe", _actionJObject["safe"].ToString());
            }
            if (_actionJObject.ContainsKey("idempotent"))
            {
                _tdAction.AddQualifier("idempotent", _actionJObject["idempotent"].ToString());
            }
            return _tdAction;
        }

        // TD Links
        public static AdminShell.Property BuildTDLink(JObject linkJObject,string index)
        {
            AdminShell.Property _tdLink = buildaasProperty("link" + index, linkJObject["href"].ToString(), "A link can be viewed as a statement of the form link context has a relation type resource at link target, where the optional target attributes may further describe the resource");
            AdminShell.QualifierCollection tdQualifier = new AdminShell.QualifierCollection();
            if (linkJObject.ContainsKey("type"))
            {
                _tdLink.AddQualifier("type", linkJObject["type"].ToString());
            }
            if (linkJObject.ContainsKey("rel"))
            {
                _tdLink.AddQualifier("rel", linkJObject["rel"].ToString());
            }
            if (linkJObject.ContainsKey("anchor"))
            {
                _tdLink.AddQualifier("anchor", linkJObject["anchor"].ToString());
            }
            if (linkJObject.ContainsKey("sizes"))
            {
                _tdLink.AddQualifier("sizes", linkJObject["sizes"].ToString());
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
            int index = 1;
            foreach (JObject ds in jObject["link"])
            {
                tdLinks.Add(BuildTDLink(ds,index.ToString()));
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
            if (jObject.ContainsKey("@type")) // needs to be discussed with Mr. Sebastian. When input is an array 
                                              // requires an example
            {
                _securityDefinition.AddQualifier("@type", jObject["@type"].ToString());
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
                _securityDefinition.AddQualifier("proxy", jObject["proxy"].ToString());
            }
            if (jObject.ContainsKey("scheme"))
            {
                string scheme = jObject["scheme"].ToString();
                _securityDefinition.AddQualifier("scheme", scheme);
                if (scheme == "combo")
                {
                    if (jObject.ContainsKey("oneOf"))
                    {
                        AdminShell.SubmodelElementCollection _oneOf = new AdminShell.SubmodelElementCollection();
                        _oneOf.idShort = "oneOf";
                        _oneOf.category = "PARAMETER";
                        _oneOf.ordered = false;
                        _oneOf.allowDuplicates = false;
                        _oneOf.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                        _oneOf.AddDescription("en", "	Array of two or more strings identifying other named security scheme definitions, any one of which, when satisfied, will allow access. Only one may be chosen for use.");
                        if ((jObject["oneOf"].Type).ToString() == "String")
                        {
                            _oneOf.Add(buildaasProperty("oneOf", (jObject["oneOf"]).ToString(), "Named security scheme"));
                        }
                        if ((jObject["oneOf"].Type).ToString() == "Array")
                        {
                            int index = 1;
                            foreach (var x in jObject["oneOf"])
                            {
                                _oneOf.Add(buildaasProperty("oneOf" + (index).ToString(), (x).ToString(), "Named security scheme"));
                                index = index + 1;
                            }
                        }
                    }
                    if (jObject.ContainsKey("allOf"))
                    {
                        AdminShell.SubmodelElementCollection _allOf = new AdminShell.SubmodelElementCollection();
                        _allOf.idShort = "oneOf";
                        _allOf.category = "PARAMETER";
                        _allOf.ordered = false;
                        _allOf.allowDuplicates = false;
                        _allOf.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                        _allOf.AddDescription("en", "	Array of two or more strings identifying other named security scheme definitions, all of which must be satisfied for access.");
                        if ((jObject["allOf"].Type).ToString() == "String")
                        {
                            _allOf.Add(buildaasProperty("allOf", (jObject["allOf"]).ToString(), "Named security scheme"));
                        }
                        if ((jObject["oneOf"].Type).ToString() == "Array")
                        {
                            int index = 1;
                            foreach (var x in jObject["allOf"])
                            {
                                _allOf.Add(buildaasProperty("allOf" + (index).ToString(), (x).ToString(), "Named security scheme"));
                                index = index + 1;
                            }
                        }
                    }
                }
                if (scheme =="basic" || scheme == "apikey")
                {
                    if (jObject.ContainsKey("name"))
                    {
                        _securityDefinition.AddQualifier("name", jObject["name"].ToString());
                    }
                    if (jObject.ContainsKey("in"))
                    {
                        _securityDefinition.AddQualifier("in", jObject["in"].ToString());
                    }   
                }
                if (scheme == "digest")
                {
                    if (jObject.ContainsKey("name"))
                    {
                        _securityDefinition.AddQualifier("name", jObject["name"].ToString());
                    }
                    if (jObject.ContainsKey("in"))
                    {
                        _securityDefinition.AddQualifier("in", jObject["in"].ToString());
                    }
                    if (jObject.ContainsKey("qop"))
                    {
                        _securityDefinition.AddQualifier("in", jObject["qop"].ToString());
                    }
                }
                if (scheme == "bearer")
                {
                    if (jObject.ContainsKey("name"))
                    {
                        _securityDefinition.AddQualifier("name", jObject["name"].ToString());
                    }
                    if (jObject.ContainsKey("in"))
                    {
                        _securityDefinition.AddQualifier("in", jObject["in"].ToString());
                    }
                    if (jObject.ContainsKey("authorization"))
                    {
                        _securityDefinition.AddQualifier("authorization", jObject["authorization"].ToString());
                    }
                    if (jObject.ContainsKey("alg"))
                    {
                        _securityDefinition.AddQualifier("alg", jObject["alg"].ToString());
                    }
                    if (jObject.ContainsKey("format"))
                    {
                        _securityDefinition.AddQualifier("format", jObject["format"].ToString());
                    }
                }
                if (scheme == "psk")
                {
                    if (jObject.ContainsKey("identity"))
                    {
                        _securityDefinition.AddQualifier("identity", jObject["identity"].ToString());
                    }
                }
                if (scheme == "oauth2")
                {
                    if (jObject.ContainsKey("authorization"))
                    {
                        _securityDefinition.AddQualifier("authorization", jObject["authorization"].ToString());
                    }
                    if (jObject.ContainsKey("token"))
                    {
                        _securityDefinition.AddQualifier("token", jObject["token"].ToString());
                    }
                    if (jObject.ContainsKey("refresh"))
                    {
                        _securityDefinition.AddQualifier("in", jObject["refresh"].ToString());
                    }
                    if (jObject.ContainsKey("flow"))
                    {
                        _securityDefinition.AddQualifier("in", jObject["flow"].ToString());
                    }
                    if (jObject.ContainsKey("scopes"))
                    {
                        AdminShell.SubmodelElementCollection _oneOf = new AdminShell.SubmodelElementCollection();
                        _oneOf.idShort = "scopes";
                        _oneOf.category = "PARAMETER";
                        _oneOf.ordered = false;
                        _oneOf.allowDuplicates = false;
                        _oneOf.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                        _oneOf.AddDescription("en", "Set of authorization scope identifiers provided as an array. These are provided in tokens returned by an authorization server and associated with forms in order to identify what resources a client may access and how. The values associated with a form should be chosen from those defined in an OAuth2SecurityScheme active on that form.");
                        if ((jObject["scopes"].Type).ToString() == "String")
                        {
                            _oneOf.Add(buildaasProperty("scopes", (jObject["scopes"]).ToString(), "Named security scheme"));
                        }
                        if ((jObject["scopes"].Type).ToString() == "Array")
                        {
                            int index = 1;
                            foreach (var x in jObject["scopes"])
                            {
                                _oneOf.Add(buildaasProperty("scopes" + (index).ToString(), (x).ToString(), "OAuth Scope Indetifiers"));
                                index = index + 1;
                            }
                        }
                    }
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

        public static JObject ImportTDJsontoSubModel(
            string inputFn, AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm, AdminShell.SubmodelRef smref)
        {
            JObject exportData = new JObject();
            try
            {
                string text = File.ReadAllText(inputFn);
                JObject tdJObject = JObject.Parse(text);
                if (false) //ValidateTDJson(tdJObject)
                {
                    //AasxPackageExplorer.Log.Singleton.Error("The TD is not a valid JSON file: ");
                    exportData["status"] = "error";
                    return exportData;
                }
                else
                {
                    
                    AdminShell.QualifierCollection tdQualifier = new AdminShell.QualifierCollection();
                    if (tdJObject.ContainsKey("@context"))
                    {
                        // Need to check with @context
                    }
                    if (tdJObject.ContainsKey("@type"))
                    {
                        AdminShell.Qualifier _type = new AdminShell.Qualifier();
                        _type.type = "@type";
                        _type.value = tdJObject["@type"].ToString();
                        tdQualifier.Add(_type);
                    }
                    if (tdJObject.ContainsKey("id"))
                    {
                        string id = tdJObject["id"].ToString();
                        sm.SetIdentification("IRI", id);
                        smref.First.idType = "IRI";
                        smref.First.value = id;
                    }
                    if (tdJObject.ContainsKey("title"))
                    {
                        AdminShell.Qualifier _type = new AdminShell.Qualifier();
                        _type.type = "title";
                        _type.value = tdJObject["title"].ToString();
                        tdQualifier.Add(_type);
                    }
                    if (tdJObject.ContainsKey("titles"))
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
                    if (tdJObject.ContainsKey("description"))
                    {
                        sm.AddDescription("en", tdJObject["description"].ToString());
                    }
                    if (tdJObject.ContainsKey("descriptions"))
                    {
                        JObject _descriptionsJObject = (JObject)tdJObject["descriptions"];
                        foreach (var x in _descriptionsJObject)
                        {
                            sm.AddDescription((x.Key).ToString(), (x.Value).ToString());

                        }
                    }
                    if (tdJObject.ContainsKey("version"))
                    {
                        string version = tdJObject["version"].ToString();
                        JObject versionObject = JObject.Parse(version);
                        string intance = "";
                        string model = "";
                        if (versionObject.ContainsKey("instance"))
                        {intance = (versionObject["instance"]).ToString(); }
                        if (versionObject.ContainsKey("model"))
                        { model = (versionObject["model"]).ToString(); }
                        sm.SetAdminstration(intance, model);
                    }
                    if (tdJObject.ContainsKey("created"))
                    {
                        AdminShell.Qualifier _created = new AdminShell.Qualifier();
                        _created.type = "created";
                        _created.value = tdJObject["created"].ToString();
                        tdQualifier.Add(_created);
                    }
                    if (tdJObject.ContainsKey("modified"))
                    {
                        AdminShell.Qualifier _modified = new AdminShell.Qualifier();
                        _modified.type = "modified";
                        _modified.value = tdJObject["modified"].ToString();
                        tdQualifier.Add(_modified);
                    }
                    if (tdJObject.ContainsKey("support"))
                    {
                        AdminShell.Qualifier _support = new AdminShell.Qualifier();
                        _support.type = "support";
                        _support.value = tdJObject["support"].ToString();
                        tdQualifier.Add(_support);
                    }
                    if (tdJObject.ContainsKey("base"))
                    {
                        AdminShell.Qualifier _base = new AdminShell.Qualifier();
                        _base.type = "base";
                        _base.value = tdJObject["base"].ToString();
                        tdQualifier.Add(_base);
                    }
                    if (tdJObject.ContainsKey("properties"))
                    {
                        AdminShell.SubmodelElementCollection porperties = BuildTDProperties(tdJObject);
                        sm.Add(porperties);
                    }
                    if (tdJObject.ContainsKey("actions"))
                    {
                        AdminShell.SubmodelElementCollection actions = BuildTDActions(tdJObject);
                        sm.Add(actions);
                    }
                    if (tdJObject.ContainsKey("events"))
                    {
                        AdminShell.SubmodelElementCollection events = BuildTDEvents(tdJObject);
                        sm.Add(events);
                    }
                    if (tdJObject.ContainsKey("links"))
                    {
                        AdminShell.SubmodelElementCollection links = BuildTDLinks(tdJObject);
                        sm.Add(links);
                    }
                    if (tdJObject.ContainsKey("forms"))
                    {
                        AdminShell.SubmodelElementCollection forms = new AdminShell.SubmodelElementCollection();
                        forms.idShort = "forms";
                        forms.category = "PARAMETER";
                        forms.ordered = false;
                        forms.allowDuplicates = false;
                        forms.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                        forms.AddDescription("en", "Set of form hypermedia controls that describe how an operation can be performed. Forms are serializations of Protocol Bindings");
                        int i = 0;
                        foreach (JObject ds in tdJObject["forms"])
                        {
                            forms.Add(BuildTDForm(ds,"Form"+i.ToString()));
                        }
                        sm.Add(forms);
                    }
                    if (tdJObject.ContainsKey("security"))
                    {
                        AdminShell.SubmodelElementCollection _security = new AdminShell.SubmodelElementCollection();
                        _security.idShort = "security";
                        _security.category = "PARAMETER";
                        _security.ordered = false;
                        _security.allowDuplicates = false;
                        _security.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                        _security.AddDescription("en", "security definition names");
                        if ((tdJObject["security"].Type).ToString() == "String")
                        {
                            _security.Add(buildaasProperty("security", (tdJObject["security"]).ToString(), "security definition name"));
                        }
                        if ((tdJObject["security"].Type).ToString() == "Array")
                        {
                            int index = 1;
                            foreach (var x in tdJObject["security"])
                            {
                                _security.Add(buildaasProperty("security"+(index).ToString(), (x).ToString(), "security definition name"));
                                index = index + 1;
                            }
                        }
                        sm.Add(_security);
                    }
                    if (tdJObject.ContainsKey("securityDefinitions"))
                    {
                        AdminShell.SubmodelElementCollection _securityDefintions = BuildTDSecurityDefinitions(tdJObject);
                        sm.Add(_securityDefintions);
                    }
                    if (tdJObject.ContainsKey("profile"))
                    {
                        AdminShell.SubmodelElementCollection _profile = new AdminShell.SubmodelElementCollection();
                        _profile.idShort = "profile";
                        _profile.category = "PARAMETER";
                        _profile.ordered = false;
                        _profile.allowDuplicates = false;
                        _profile.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                        _profile.AddDescription("en", "Indicates the WoT Profile mechanisms followed by this Thing Description and the corresponding Thing implementation.");
                        if ((tdJObject["profile"].Type).ToString() == "String")
                        {
                            _profile.Add(buildaasProperty("security", (tdJObject["security"]).ToString(), "WoT Profile mechanism."));
                        }
                        if ((tdJObject["profile"].Type).ToString() == "Array")
                        {
                            int index = 1;
                            foreach (var x in tdJObject["profile"])
                            {
                                _profile.Add(buildaasProperty("profile" + (index).ToString(), (x).ToString(), "WoT Profile mechanism."));
                                index = index + 1;
                            }
                        }
                        sm.Add(_profile);
                    }
                    if (tdJObject.ContainsKey("schemaDefinitions"))
                    {
                        AdminShell.SubmodelElementCollection _schemaDefinitions = new AdminShell.SubmodelElementCollection();
                        _schemaDefinitions.idShort = "schemaDefinitions";
                        _schemaDefinitions.category = "PARAMETER";
                        _schemaDefinitions.ordered = false;
                        _schemaDefinitions.allowDuplicates = false;
                        _schemaDefinitions.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                        _schemaDefinitions.AddDescription("en", "Set of named data schemas. To be used in a schema name-value pair inside an AdditionalExpectedResponse object.");
                        foreach (var key in tdJObject["uriVariables"])
                        {
                            JObject _uriVariable = new JObject(tdJObject["uriVariables"][key]);
                            _schemaDefinitions.value.Add(BuildAbstractDataSchema(_uriVariable, key.ToString(), "Named schema definition."));
                        }
                        sm.Add(_schemaDefinitions);
                    }
                    sm.qualifiers = tdQualifier;
                    sm.idShort = "AssetTD";
                    exportData["status"] = "Success";
                    return tdJObject;
                }
            }
            catch (Exception ex)
            {
                //AasxPackageExplorer.Log.Singleton.Error(ex, "When importing TD Json, an error occurred");
                exportData["status"] = "error";
                return exportData;
            }   



        }
    }
}
