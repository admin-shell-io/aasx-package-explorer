using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxFormatCst
{
    public class CstListOfValues
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class LOVStringItem
        {
            public string StringValue;
            public string DisplayValue;
            public string BlockReference;
        }

        public class LOVItems
        {
            public string DataType;
            public List<LOVStringItem> LOVStringItems;
        }

        public class KeyLOVDefinition
        {
            public string ObjectType = "09";
            public string Namespace;
            public string ID;
            public string Revision;
            public string Name;
            public string Status;
            public LOVItems LOVItems;
        }

        public class Root
        {
            public string SchemaVersion = "1.0.0";
            public string Locale = "en_US";
            public List<KeyLOVDefinition> KeyLOVDefinitions;
        }
    }
}
