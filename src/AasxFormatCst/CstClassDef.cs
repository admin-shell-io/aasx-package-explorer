using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// see: 1-35 for ApplicationClass (class def), Block Property (prop def), Property Block (class def), Values??

namespace AasxFormatCst
{
    public class CstClassDef
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 

        public class ClassAttribute
        {
            public string Type;
            public string Reference;
        }

        public class ClassDefinition : CstIdObjectBase
        {
            public int UnitSystem;
            public string ClassType;
            public List<ClassAttribute> ClassAttributes = new List<ClassAttribute>();

            public ClassDefinition () 
            {
                ObjectType = "01";
                MinorRevision = "001";
                Status = "Released";
                UnitSystem = 3;
            }

            public ClassDefinition(CstIdObjectBase id)
                : this()
            {
                if (id == null)
                    return;

                Namespace = id.Namespace;
                ID = id.ID;
                Revision = id.Revision;
                Name = id.Name;
                if (id.MinorRevision != null)
                    MinorRevision = id.MinorRevision;
                if (id.Status != null)
                    Status = id.Status;
            }
        }

        public class Root : CstRootBase
        {
            public string SchemaVersion = "1.2.0";
            public string Locale = "en_US";
            public bool SkipExistingIRDI = true;
            public ListOfUnique<ClassDefinition> ClassDefinitions = new ListOfUnique<ClassDefinition>();
        }
    }
}
