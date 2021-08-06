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
            if (jObject.ContainsKey("title"))
            {
                List<AdminShellV20.LangStr> titleList1 = new List<AdminShellV20.LangStr>();
                AdminShellV20.LangStr title1 = new AdminShellV20.LangStr("en", jObject["title"].ToString());
                titleList1.Add(title1);
                AdminShell.MultiLanguageProperty mlp = BuildMultiLanguageProperty("title", titleList1, "Provides multi-language human-readable titles (e.g., display a text for UI representation in different languages)");
                abstractDS.Add(mlp);
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
            if (jObject.ContainsKey("unit"))
            {
                AdminShell.Qualifier _type = new AdminShell.Qualifier();
                _type.type = "unit";
                _type.value = jObject["unit"].ToString();
                abDSQualifier.Add(_type);
            }
            if (jObject.ContainsKey("readOnly"))
            {
                AdminShell.Qualifier _type = new AdminShell.Qualifier();
                _type.type = "readOnly";
                _type.value = jObject["readOnly"].ToString();
                abDSQualifier.Add(_type);
            }
            if (jObject.ContainsKey("writeOnly"))
            {
                AdminShell.Qualifier _type = new AdminShell.Qualifier();
                _type.type = "writeOnly";
                _type.value = jObject["writeOnly"].ToString();
                abDSQualifier.Add(_type);
            }
            if (jObject.ContainsKey("format"))
            {
                AdminShell.Qualifier _type = new AdminShell.Qualifier();
                _type.type = "format";
                _type.value = jObject["format"].ToString();
                abDSQualifier.Add(_type);
            }
            // ObjectSchema
            if (jObject.ContainsKey("enum"))
            {

            }
            if (jObject.ContainsKey("const"))
            {

            }
            if (jObject.ContainsKey("default"))
            {

            }
            if (jObject.ContainsKey("type"))
            {

            }
            if (jObject.ContainsKey("oneOf"))
            {

            }
            abstractDS.qualifiers = abDSQualifier;
            return abstractDS;
        }
        public static AdminShell.SubmodelElementCollection BuildTDForm(JObject jObject)
        {
            AdminShell.SubmodelElementCollection tdForm = new AdminShell.SubmodelElementCollection();
            tdForm.idShort = "Form";
            tdForm.category = "PARAMETER";
            tdForm.ordered = false;
            tdForm.allowDuplicates = false;
            tdForm.kind = AdminShellV20.ModelingKind.CreateAsInstance();
            tdForm.AddDescription("en", "Hypermedia controls that describe how an operation can be performed. Form is a  serializations of Protocol Bindings");
            AdminShell.QualifierCollection formQualifier = new AdminShell.QualifierCollection();
            List<string> formElements = new List<string>(new string[] { "href", "contentType", "op" });
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
            if (jObject.ContainsKey("op"))
            {
                AdminShell.SubmodelElementCollection _op = new AdminShell.SubmodelElementCollection();
                _op.idShort = "op";
                _op.category = "PARAMETER";
                _op.ordered = false;
                _op.allowDuplicates = false;
                _op.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                _op.AddDescription("en", "	Indicates the semantic intention of performing the operation(s) described by the form. For example, the Property interaction allows get and set operations. The protocol binding may contain a form for the get operation and a different form for the set operation. The op attribute indicates which form is for which and allows the client to select the correct form for the operation required. op can be assigned one or more interaction verb(s) each representing a semantic intention of an operation.");
                if ((jObject["op"].Type).ToString() == "String")
                {
                    _op.Add(buildaasProperty("op", (jObject["op"]).ToString(), "Semantic intention of performing the operation"));
                }
                if ((jObject["op"].Type).ToString() == "Array")
                {
                    foreach (var x in jObject["op"])
                    {
                        _op.Add(buildaasProperty("op", (x).ToString(), "Semantic intention of performing the operation"));
                    }
                }
                tdForm.Add(_op);
            }
            foreach (var x in jObject)
            {
                string key = x.Key.ToString();
                if (!formElements.Contains(key))
                {
                    tdForm.Add(buildaasProperty(key, (x).ToString(), ""));
                }
            }
            tdForm.qualifiers = formQualifier;
            return tdForm;
        }
        public static AdminShell.SubmodelElementCollection BuildAbstractInteractionAvoidance(JObject jObject, string idShort, string description)
        {
            AdminShell.SubmodelElementCollection _interactionAffordance = BuildAbstractDataSchema(jObject, idShort, description);
            if (jObject.ContainsKey("uriVariables"))
                  {
                      AdminShell.SubmodelElementCollection _uriVariables = new AdminShell.SubmodelElementCollection();
                      _uriVariables.idShort = "uriVariables";
                      _uriVariables.category = "PARAMETER";
                      _uriVariables.ordered = false;
                      _uriVariables.allowDuplicates = false;
                      _uriVariables.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                      _uriVariables.AddDescription("en", "Used to ensure that the data is valid against one of the specified schemas in the array.");
                      foreach (var key in jObject["uriVariables"])
                      {
                          JObject _uriVariable = new JObject(jObject["uriVariables"][key]);
                          _uriVariables.value.Add(BuildAbstractDataSchema(_uriVariable, key.ToString(), "	Define URI query template variables as collection based on DataSchema declarations. The individual variables DataSchema cannot be an ObjectSchema or an ArraySchema."));
                      } 
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
                foreach (JObject ds in jObject["forms"])
                {
                    forms.Add(BuildTDForm(ds));
                }
                _interactionAffordance.Add(forms);
            }
            return _interactionAffordance;
        }
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

        public static void ImportTDJsontoSubModel(
            string inputFn, AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm, AdminShell.SubmodelRef smref)
        {

            try
            {
                string text = File.ReadAllText(inputFn);
                JObject tdJObject = JObject.Parse(text);

                if (ValidateTDJson(tdJObject))
                {
                    //AasxPackageExplorer.Log.Singleton.Error("The TD is not a valid JSON file: ");
                    throw new InvalidOperationException(
                                $"The TD is not a valid JSON file: ");
                    return;
                }
                else
                {

                    AdminShell.SubmodelElementCollection tdSubmodelCollection = new AdminShell.SubmodelElementCollection();
                    AdminShell.QualifierCollection tdQualifier = new AdminShell.QualifierCollection();
                    if (tdJObject.ContainsKey("@type"))
                    {
                        AdminShell.Qualifier _type = new AdminShell.Qualifier();
                        _type.type = "@type";
                        _type.value = tdJObject["@type"].ToString();
                        tdQualifier.Add(_type);
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
                    if (tdJObject.ContainsKey("description"))
                    {
                        tdSubmodelCollection.AddDescription("en", tdJObject["description"].ToString());
                    }
                    if (tdJObject.ContainsKey("descriptions"))
                    {
                        JObject _descriptionsJObject = (JObject)tdJObject["descriptions"];
                        foreach (var x in _descriptionsJObject)
                        {
                            tdSubmodelCollection.AddDescription((x.Key).ToString(), (x.Value).ToString());

                        }
                    }
                    if (tdJObject.ContainsKey("title"))
                    {
                        List<AdminShellV20.LangStr> titleList1 = new List<AdminShellV20.LangStr>();
                        AdminShellV20.LangStr title1 = new AdminShellV20.LangStr("en", tdJObject["title"].ToString());
                        titleList1.Add(title1);
                        AdminShell.MultiLanguageProperty mlp = BuildMultiLanguageProperty("title", titleList1, "Provides multi-language human-readable titles (e.g., display a text for UI representation in different languages)");
                        tdSubmodelCollection.Add(mlp);
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
                        tdSubmodelCollection.Add(mlp);
                    }
                    if (tdJObject.ContainsKey("properties"))
                    {
                        AdminShell.SubmodelElementCollection porperties = BuildTDProperties(tdJObject);
                        tdSubmodelCollection.Add(porperties);
                    }
                    tdSubmodelCollection.qualifiers = tdQualifier;
                    tdSubmodelCollection.idShort = "AssetTD";
                    sm.idShort = "ThingDescription";
                    sm.Add(tdSubmodelCollection);
                }
            }
            catch (Exception ex)
            {
                //AasxPackageExplorer.Log.Singleton.Error(ex, "When importing TD Json, an error occurred");
                return;
            }



        }
    }
}
