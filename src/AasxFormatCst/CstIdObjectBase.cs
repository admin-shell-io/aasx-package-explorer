using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AasxFormatCst
{
    public class CstIdObjectBase
    {
        public string ObjectType;
        public string Namespace;
        public string ID;
        public string Revision;
        public string Name;
        public string MinorRevision;
        public string Status;

        public string ToRef()
        {
            var res = String.Format("{0}#{1}-{2}#{3}", Namespace, ObjectType, ID, Revision);
            return res;
        }

        public static CstIdObjectBase Parse(string input)
        {
            var m = Regex.Match(input, @"^\s*(\w+)#(\w+)-(\w+)#(\w+)");
            if (!m.Success)
                return null;
            var res = new CstIdObjectBase()
            {
                Namespace = m.Groups[1].ToString(),
                ObjectType = m.Groups[2].ToString(),
                ID = m.Groups[2].ToString(),
                Revision = m.Groups[4].ToString()
            };
            return res;
        }
    }
}
