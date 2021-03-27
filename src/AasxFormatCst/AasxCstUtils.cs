using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxFormatCst
{
    public class AasxCstUtils
    {
        public static string ToPascalCase(string st)
        {
            // access
            if (st == null)
                return null;

            // blanks at start not allowed
            st = st.TrimStart();

            // c style
            var res = "";
            for (int i=0; i<st.Length; i++)
            {
                if (i == 0 || (i > 0 && Char.IsWhiteSpace(st[i - 1])))
                {
                    if (res.Length > 0)
                        res = res.Substring(0, res.Length - 1);
                    res += Char.ToUpperInvariant(st[i]);
                }
                else
                {
                    res += Char.ToLowerInvariant(st[i]);
                }
            }

            // ok
            return res;
        }
    }
}
