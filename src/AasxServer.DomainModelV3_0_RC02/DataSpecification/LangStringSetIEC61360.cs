using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasCore.Aas3_0_RC02.HasDataSpecification
{
    public class LangStringSetIEC61360 : List<LangString>
    {
        public static LangStringSetIEC61360 CreateFrom(List<LangString> src)
        {
            var res = new LangStringSetIEC61360();
            if (src != null)
                foreach (var ls in src)
                    res.Add(new LangString(ls.Language, ls.Text));
            return res;
        }
    }
}
