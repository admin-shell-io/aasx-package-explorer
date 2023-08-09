/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// see: 1-35 for ApplicationClass (class def), Block Property (prop def), Property Block (class def), Values??

// ReSharper disable ClassNeverInstantiated.Global

namespace AasxFormatCst
{
    public class CstClassDef
    {
        //// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 

        public class ClassAttribute : IUniqueness<ClassAttribute>
        {
            public string Type;
            public string Reference;

            public bool EqualsForUniqueness(ClassAttribute other)
            {
                if (other == null)
                    return false;

                var res = Type == other.Type
                    && Reference == other.Reference;

                return res;
            }
        }

        public class ClassDefinition : CstIdObjectBase, IUniqueness<ClassDefinition>
        {
            public int UnitSystem;
            public string ClassType;
            public ListOfUnique<ClassAttribute> ClassAttributes = new ListOfUnique<ClassAttribute>();

            public ClassDefinition()
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

            public bool EqualsForUniqueness(ClassDefinition other)
            {
                return base.EqualsForUniqueness(other);
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
