using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// see: https://json2csharp.com/

namespace AasxFormatCst
{
    public class CstPropertyRecord
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class ID
        {
            public string PropertyName;
            public string PropertyValue;
        }

        public class Property : IUniqueness<Property>
        {
            public string PropertyName;
            public string PropertyValue;
            public string ID;
            public string Name;

            // Value shall be either String or ListOfProperty

            [JsonIgnore]
            public string ValueStr;

            [JsonIgnore]
            public PropertyRecord ValueProps;

            public object Value
            {
                get
                {
                    if (ValueProps != null)
                        return ValueProps;
                    return ValueStr;
                }
            }

            public bool EqualsForUniqueness(Property other)
            {
                if (other == null)
                    return false;

                return ID.Equals(other.ID, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public class ListOfProperty : ListOfUnique<Property>
        {
            public ListOfProperty() : base() { }
            public ListOfProperty(Property[] arr) : base(arr) { }
        }

        public class ClassifiedObject
        {
            public string ObjectType;
            public bool ClassifyRevision;
            public List<ID> ID;
            public ListOfProperty Properties;
        }

        public class PropertyRecord
        {
            public string ID;
            public string ObjectType;
            public string ClassDefinition;
            public ClassifiedObject ClassifiedObject;
            public int? UnitSystem;
            public ListOfProperty Properties;
        }

        public class Root : CstRootBase
        {
            public string SchemaVersion = "1.1.0";
            public string Locale = "en_US";
            public List<PropertyRecord> PropertyRecords;
        }
    }
}
