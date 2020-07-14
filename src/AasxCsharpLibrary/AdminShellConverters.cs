using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace AdminShellNS
{
    public static class AdminShellConverters
    {
        /// <summary>
        /// This converter is used for reading JSON files; it claims to be responsible for
        /// "SubmodelElements" (the base class)
        /// and decides, which sub-class of the base class shall be populated.
        /// The decision, shich special sub-class to create is done in a factory
        /// AdminShell.SubmodelElementWrapper.CreateAdequateType(),
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
                if (typeof(AdminShell.SubmodelElement).IsAssignableFrom(objectType))
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
                object target = new AdminShell.SubmodelElement();

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
                                var o = AdminShell.SubmodelElementWrapper.CreateAdequateType(cpval);
                                if (o != null)
                                    target = o;
                            }
                        }
                }

                // Populate the object properties
                serializer.Populate(jObject.CreateReader(), target);

                return target;
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

                if (!BlobHasValue && property.DeclaringType == typeof(AdminShell.Blob) &&
                    property.PropertyName == "value")
                    property.ShouldSerialize = instance => { return false; };

                if (!SubmodelHasElements && property.DeclaringType == typeof(AdminShell.Submodel) &&
                    property.PropertyName == "submodelElements")
                    property.ShouldSerialize = instance => { return false; };

                if (!SmcHasValue && property.DeclaringType == typeof(AdminShell.SubmodelElementCollection) &&
                    property.PropertyName == "value")
                    property.ShouldSerialize = instance => { return false; };

                if (!OpHasVariables && property.DeclaringType == typeof(AdminShell.Operation) &&
                    (property.PropertyName == "in" || property.PropertyName == "out"))
                    property.ShouldSerialize = instance => { return false; };

                if (!AasHasViews && property.DeclaringType == typeof(AdminShell.AdministrationShell) &&
                    property.PropertyName == "views")
                    property.ShouldSerialize = instance => { return false; };

                return property;
            }
        }

    }
}
