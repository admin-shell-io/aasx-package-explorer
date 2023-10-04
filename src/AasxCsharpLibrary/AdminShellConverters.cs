/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Reflection;

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
                    return new AssetAdministrationShell("", null);
                // dead-csharp off
                //TODO (jtikekar, 0000-00-00): refactor default
                //if (elementName == "Asset")  
                //TODO (jtikekar, 0000-00-00): Change
                //    return new AssetInformation(AssetKind.Instance);
                if (elementName == KeyTypes.ConceptDescription.ToString())
                    return new ConceptDescription("");
                if (elementName == KeyTypes.Submodel.ToString())
                    return new Submodel("");
                //if (elementName == KeyTypes.View)
                //    return new View();
                // dead-csharp on
                return CreateSubmodelElementIstance(elementName);
            }

            private static ISubmodelElement CreateSubmodelElementIstance(string typeName)
            {
                //TODO (jtikekar, 0000-00-00): Need to test
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

            // see: https://stackoverflow.com/questions/4963160/
            // how-to-determine-if-a-type-implements-an-interface-with-c-sharp-reflection

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);
                var icic = StringComparison.InvariantCultureIgnoreCase;

                if (!BlobHasValue && typeof(IBlob).IsAssignableFrom(property.DeclaringType)
                    && property.PropertyName.Equals("value", icic))
                    property.ShouldSerialize = instance => { return false; };

                if (!SubmodelHasElements && typeof(ISubmodel).IsAssignableFrom(property.DeclaringType)
                    && property.PropertyName.Equals("submodelElements", icic))
                    property.ShouldSerialize = instance => { return false; };

                if (!SmcHasValue && typeof(ISubmodelElementCollection).IsAssignableFrom(property.DeclaringType)
                    && property.PropertyName.Equals("value", icic))
                    property.ShouldSerialize = instance => { return false; };

                if (!OpHasVariables && typeof(IOperation).IsAssignableFrom(property.DeclaringType)
                    && (property.PropertyName.Equals("in", icic)
                        || property.PropertyName.Equals("out", icic)))
                    property.ShouldSerialize = instance => { return false; };

                if (!AasHasViews && typeof(IAssetAdministrationShell).IsAssignableFrom(property.DeclaringType)
                    && property.PropertyName.Equals("views", icic))
                    property.ShouldSerialize = instance => { return false; };

                return property;
            }
        }

        public class AdaptiveAasIClassConverter : JsonConverter
        {
            public enum ConversionMode
            {
                /// <summary>
                /// For (known) nodes of the AAS meta model, the converison of Newtonsoft.Json
                /// is used. This is done by invoking creation of the real data type for the
                /// desired interface types.
                /// Assumption: fast, sloppy, fault-tolerant
                /// </summary>

                Typecast,
                /// <summary>
                /// For (known) nodes of the AAS meta model, the sub-node content is converted
                /// to string representation and subsequently converted by the AAS core deserialization.
                /// Assumption: slow, precise but provide maximum compatibility.
                /// </summary>
                AasCore
            };

            public ConversionMode Mode = ConversionMode.Typecast;

            public bool WriteRawAasCore = false;

            /// <summary>
            /// For **SERIALIZATION** and **JsonConverter attribute** a parameterless 
            /// constructor is needed.
            /// </summary>
            public AdaptiveAasIClassConverter() : base()
            {
                Mode = ConversionMode.AasCore;
            }

            public AdaptiveAasIClassConverter(ConversionMode mode) : base()
            {
                Mode = mode;
            }

            public override bool CanConvert(Type objectType)
            {
                if (typeof(IReference).IsAssignableFrom(objectType)
                    || typeof(IKey).IsAssignableFrom(objectType))
                    return true;
                return false;
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override object ReadJson(JsonReader reader,
                                            Type objectType,
                                             object existingValue,
                                             JsonSerializer serializer)
            {
                // check
                if (reader.TokenType == JsonToken.Null)
                    return null;

                // spooky?
                if (Mode == ConversionMode.AasCore)
                {
                    var json = JRaw.Create(reader).ToString();
                    var node = System.Text.Json.Nodes.JsonNode.Parse(json);
                    return ExtendIClass.IClassFrom(objectType, node);
                }

                // Load JObject from stream
                JObject jObject = JObject.Load(reader);

                // Create target object based on JObject
                object target = null;
                if (typeof(IReference).IsAssignableFrom(objectType))
                    target = new Reference(ReferenceTypes.ExternalReference, null);
                if (typeof(IKey).IsAssignableFrom(objectType))
                    target = new Key(KeyTypes.GlobalReference, "");

                // Populate the object properties
                serializer.Populate(jObject.CreateReader(), target);

                return target;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value is IClass ic)
                {
                    // serialize by AAS core
                    var jsonStr = Jsonization.Serialize.ToJsonObject(ic)
                        .ToJsonString(new System.Text.Json.JsonSerializerOptions()
                        {
                            WriteIndented = true
                        });

                    // how to write
                    if (WriteRawAasCore)
                    {
                        // directly write raw string into serializer
                        // drawback: no indentation
                        writer.WriteStartObject();
                        jsonStr = jsonStr.TrimStart('{').TrimEnd('}');
                        writer.WriteRaw(jsonStr);
                        writer.WriteEndObject();
                    }
                    else
                    {
                        // double-digest by a text reader and rewrite token stream
                        // pro: indentation
                        // con: run time performance, chance of de-serialization issues
                        using (var reader = new JsonTextReader(new StringReader(jsonStr))
                        {
                            DateParseHandling = DateParseHandling.None,
                            FloatParseHandling = FloatParseHandling.Decimal
                        })
                        {
                            writer.WriteToken(reader);
                        }
                    }
                }
                else
                {
                    // normal serialization
                    var jo = JObject.FromObject(value);
                    jo.WriteTo(writer);
                }
            }
        }

    }
}
