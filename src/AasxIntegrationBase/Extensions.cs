using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxIntegrationBase
{
    public static class Extensions
    {
        public static bool HasContent(this string str)
        {
            return str != null && str.Trim() != "";
        }

        public static void SetIfNoContent(ref string s, string input)
        {
            if (!input.HasContent())
                return;
            if (!s.HasContent())
                s = input;
        }

    }
}
