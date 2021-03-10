using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxFormatCst
{
    public class CstId
    {
        public string Namespace;
        public string ID;
        public string Revision;
        public string Name;
        public string MinorRevision;
        public string Status;

        public string ToRef()
        {
            var res = String.Format("{0}{1}-{2}#{3}", Namespace, ObjectType, ID, Revision);
            return res;
        }

        public static CstId Parse(string st)
        {

        }
    }
}
