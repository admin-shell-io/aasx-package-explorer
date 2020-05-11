using AasxIntegrationBase.AasForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxIntegrationBase
{
    // see: https://stackoverflow.com/questions/11099466/using-a-custom-type-discriminator-to-tell-json-net-which-type-of-a-class-hierarc

    /// <summary>
    /// This attribute indicates, that it should e.g. serialized in JSON.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class DisplayNameAttribute : System.Attribute
    {
        /// <summary>
        /// Name to show up in JSON.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Setting this parameter prevents the look-up direction from name to type.
        /// By this, two different types can feature the same DisplayName.
        /// </summary>
        public bool NoTypeLookup { get; set; }

        public DisplayNameAttribute(string displayName, bool noTypeLookup = false)
        {
            this.DisplayName = displayName;
            this.NoTypeLookup = noTypeLookup;
        }
    }

    /// <summary>
    /// Serialization Binder for AASX Options. Allows modified $typename via [DisplayNameAttribute]
    /// </summary>
    public class DisplayNameSerializationBinder : DefaultSerializationBinder
    {
        private Dictionary<string, Type> _nameToType;
        private Dictionary<Type, string> _typeToName;

        public DisplayNameSerializationBinder(Type[] startingTypes)
        {
            if (startingTypes == null)
                return;

            _nameToType = new Dictionary<string, Type>();
            _typeToName = new Dictionary<Type, string>();

            foreach (var startingType in startingTypes)
            {
                var customDisplayNameTypes =
                    // this.GetType()
                    startingType
                        .Assembly
                        //concat with references if desired
                        .GetTypes()
                        .Where(x => x
                            .GetCustomAttributes(false)
                            .Any(y => y is DisplayNameAttribute));

                foreach (var t in customDisplayNameTypes)
                {
                    var dn = t.GetCustomAttributes(false).OfType<DisplayNameAttribute>().First().DisplayName;
                    var ntu = t.GetCustomAttributes(false).OfType<DisplayNameAttribute>().First().NoTypeLookup;
                    if (!ntu)
                        _nameToType.Add(dn, t);
                    _typeToName.Add(t, dn);
                }

                /*
                _nameToType = customDisplayNameTypes.ToDictionary(
                    t => t.GetCustomAttributes(false).OfType<DisplayNameAttribute>().First().DisplayName,
                    t => t);

                _typeToName = _nameToType.ToDictionary(
                    t => t.Value,
                    t => t.Key);
                */
            }
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (false == _typeToName.ContainsKey(serializedType))
            {
                base.BindToName(serializedType, out assemblyName, out typeName);
                return;
            }

            var name = _typeToName[serializedType];

            assemblyName = null;
            typeName = name;
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            if (_nameToType.ContainsKey(typeName))
                return _nameToType[typeName];

            return base.BindToType(assemblyName, typeName);
        }
    }

    public class AasxPluginOptionSerialization
    {
        public static JsonSerializerSettings GetDefaultJsonSettings(Type[] startingTypes)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                SerializationBinder = new DisplayNameSerializationBinder(startingTypes),
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.Objects /*,
                Formatting = Formatting.Indented */
            };
            return settings;
        }

        public static void Test123()
        {
            var ttt = new AasForms.FormDescProperty("t", AasForms.FormMultiplicity.One, null, "xdscdsds");
            var descDoc = new FormDescListOfElement();
            descDoc.Add(ttt);

            using (var s = new StreamWriter("test2.json"))
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    SerializationBinder = new DisplayNameSerializationBinder(new Type[] { typeof(FormDescListOfElement), typeof(FormDescProperty) } ),
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    TypeNameHandling = TypeNameHandling.Auto,
                    Formatting = Formatting.Indented
                };
                JsonSerializer serializer = JsonSerializer.Create(settings);
                using (JsonWriter writer = new JsonTextWriter(s))
                {
                    serializer.Serialize(writer, descDoc, typeof(FormDescProperty));
                }
            }

            var settings2 = AasxPluginOptionSerialization.GetDefaultJsonSettings(new Type[] { typeof(FormDescBase) });

            var test2 = new AasForms.FormDescSubmodel("t", null, "xdscdsds");
            var test22 = new AasForms.FormDescProperty("t", AasForms.FormMultiplicity.One, null, "xdscdsds");
            test2.Add(test22);

            var jsonTest = Newtonsoft.Json.JsonConvert.SerializeObject(test2, settings2);
        }

    }
}
