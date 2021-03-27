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

        public class PropertyDefinition : CstIdObjectBase, IUniqueness<PropertyDefinition>
        {
            public string Definition;
            public DataType DataType;
            public string SourceStandard;
            public string Remark;

            public PropertyDefinition() 
            {
                ObjectType = "02";
                Revision = "001";
                Status = "Released";
            }

            public PropertyDefinition(CstIdObjectBase id)
                : this()
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

            public bool EqualsForUniqueness(PropertyDefinition other)
            {
                return base.EqualsForUniqueness(other);
            }
        }

        public class Root : CstRootBase
        {
            public string SchemaVersion = "1.2.0";
            public string Locale = "en_US";
            public bool SkipExistingIRDI = true;
            public bool AddAsNewRelease = true;
            public ListOfUnique<PropertyDefinition> PropertyDefinitions;
        }

    }
}
