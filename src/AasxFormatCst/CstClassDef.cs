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

        public class ClassDefinition
        {
            public string ObjectType = "01";
            public string Namespace;
            public string ID;
            public string Revision;
            public string Name;
            public string MinorRevision = "001";
            public string Status = "Released";
            public int UnitSystem = 3;
            public string ClassType;
            public List<ClassAttribute> ClassAttributes = new List<ClassAttribute>();

            public ClassDefinition () { }

            public ClassDefinition(CstId id)
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

            public string ToRef()
            {
                var res = String.Format("{0}{1}-{2}#{3}", Namespace, ObjectType, ID, Revision);
                return res;
            }
        }

        public class Root
        {
            public string SchemaVersion = "1.2.0";
            public string Locale = "en_US";
            public bool SkipExistingIRDI = true;
            public List<ClassDefinition> ClassDefinitions = new List<ClassDefinition>();
        }
    }
}
