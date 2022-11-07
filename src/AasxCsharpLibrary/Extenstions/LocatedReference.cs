using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public class LocatedReference
    {
        public IIdentifiable Identifiable;
        public Reference Reference;

        public LocatedReference() { }
        public LocatedReference(IIdentifiable identifiable, Reference reference)
        {
            Identifiable = identifiable;
            Reference = reference;
        }
    }
}
