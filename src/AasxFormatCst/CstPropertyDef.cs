using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// see: https://json2csharp.com/

namespace AasxFormatCst
{
    public class CstPropertyDef
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class DataType
        {
            public string Type;
            public string BlockReference;
        }

        public class PropertyDefinition
        {
            public string ObjectType = "02";
            public string Namespace;
            public string ID;
            public string Revision = "001";
            public string Name;
            public string Status = "Released";
            public string Definition;
            public DataType DataType;
            public string MinorRevision;
            public string SourceStandard;
            public string Remark;

            public PropertyDefinition() { }

            public PropertyDefinition(CstId id)
            {
                if (id == null)
                    return;

                Namespace = id.Namespace;
                ID = id.ID;
                Revision = id.Revision;
                Name = id.Name;
                MinorRevision = id.MinorRevision;
                Status = id.Status;
            }
        }

        public class Root
        {
            public string SchemaVersion = "1.2.0";
            public string Locale = "en_US";
            public bool SkipExistingIRDI = true;
            public bool AddAsNewRelease = true;
            public List<PropertyDefinition> PropertyDefinitions;
        }

    }
}
