/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0_RC02;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace AdminShellNS
{
    public static class AdminShellConverters
    {
        /// <summary>
        /// This converter is used for reading JSON files; it claims to be responsible for
        /// "Referable" (the base class)
        /// and decides, which sub-class of the base class shall be populated.
        /// If the object is SubmodelElement, the decision, shich special sub-class to create is done in a factory
        /// SubmodelElementWrapper.CreateAdequateType(),
        /// in order to have all sub-class specific decisions in one place (SubmodelElementWrapper)
        /// Remark: There is a NuGet package JsonSubTypes, which could have done the job, except the fact of having
        /// "modelType" being a class property with a contained property "name".
        /// </summary>
        public class JsonAasxConverter : JsonConverter
        {
            private string UpperClassProperty = "modelType";
            private string LowerClassProperty = "name";

            public JsonAasxConverter() : base()
            {
            }

            public JsonAasxConverter(string UpperClassProperty, string LowerClassProperty) : base()
            {
                this.UpperClassProperty = UpperClassProperty;
                this.LowerClassProperty = LowerClassProperty;
            }

            public override bool CanConvert(Type objectType)
            {
                // Info MIHO 21 APR 2020: changed this from SubmodelElement to Referable
                if (typeof(IReferable).IsAssignableFrom(objectType))
                    return true;
                return false;
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override object ReadJson(JsonReader reader,
                                            Type objectType,
                                             object existingValue,
                                             JsonSerializer serializer)
            {
                // Load JObject from stream
                JObject jObject = JObject.Load(reader);

                // Create target object based on JObject
                IReferable target = null;

                if (jObject.ContainsKey(UpperClassProperty))
                {
                    var j2 = jObject[UpperClassProperty];
                    if (j2 != null)
                        foreach (var c in j2.Children())
                        {
                            var cprop = c as Newtonsoft.Json.Linq.JProperty;
                            if (cprop == null)
                                continue;
                            if (cprop.Name == LowerClassProperty && cprop.Value.Type.ToString() == "String")
                            {
                                var cpval = cprop.Value.ToObject<string>();
                                if (cpval == null)
                                    continue;
                                // Info MIHO 21 APR 2020: use Referable.CreateAdequateType instead of SMW...
                                var o = CreateAdequateType(cpval);
                                if (o != null)
                                    target = o;
                            }
                        }
                }

                // Populate the object properties
                serializer.Populate(jObject.CreateReader(), target);

                return target;
            }

            public static IReferable CreateAdequateType(string elementName)
            {
                if (elementName == KeyTypes.AssetAdministrationShell.ToString())
                    return new AssetAdministrationShell("", null);   //TODO: jtikekar: refactor default
                //if (elementName == "Asset")  //TODO: jtikekar Change
                //    return new AssetInformation(AssetKind.Instance);
                if (elementName == KeyTypes.ConceptDescription.ToString())
                    return new ConceptDescription("");
                if (elementName == KeyTypes.Submodel.ToString())
                    return new Submodel("");
                //if (elementName == KeyTypes.View)
                //    return new View();
                return CreateSubmodelElementIstance(elementName);
            }

            private static ISubmodelElement CreateSubmodelElementIstance(string typeName)
            {
                //TODO: jtikekar Need to test
                Type type = Type.GetType(typeName);
                if (type == null || !type.IsSubclassOf(typeof(ISubmodelElement)))
                    return null;
                var sme = Activator.CreateInstance(type) as ISubmodelElement;
                return sme;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// This converter / contract resolver for Json.NET adaptively filters different levels of depth
        /// of nested AASX structures.
        /// </summary>
        public class AdaptiveFilterContractResolver : DefaultContractResolver
        {
            public bool AasHasViews = true;
            public bool BlobHasValue = true;
            public bool SubmodelHasElements = true;
            public bool SmcHasValue = true;
            public bool OpHasVariables = true;

            public AdaptiveFilterContractResolver() { }

            public AdaptiveFilterContractResolver(bool deep = true, bool complete = true)
            {
                if (!deep)
                {
                    this.SubmodelHasElements = false;
                    this.SmcHasValue = false;
                    this.OpHasVariables = false;
                }
                if (!complete)
                {
                    this.AasHasViews = false;
                    this.BlobHasValue = false;
                }

            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                if (!BlobHasValue && property.DeclaringType == typeof(Blob) &&
                    property.PropertyName == "value")
                    property.ShouldSerialize = instance => { return false; };

                if (!SubmodelHasElements && property.DeclaringType == typeof(Submodel) &&
                    property.PropertyName == "submodelElements")
                    property.ShouldSerialize = instance => { return false; };

                if (!SmcHasValue && property.DeclaringType == typeof(SubmodelElementCollection) &&
                    property.PropertyName == "value")
                    property.ShouldSerialize = instance => { return false; };

                if (!OpHasVariables && property.DeclaringType == typeof(Operation) &&
                    (property.PropertyName == "in" || property.PropertyName == "out"))
                    property.ShouldSerialize = instance => { return false; };

                if (!AasHasViews && property.DeclaringType == typeof(AssetAdministrationShell) &&
                    property.PropertyName == "views")
                    property.ShouldSerialize = instance => { return false; };

                return property;
            }
        }

    }
}
