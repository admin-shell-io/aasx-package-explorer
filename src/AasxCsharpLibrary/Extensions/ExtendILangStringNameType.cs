using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendILangStringNameType
    {
        public static string ToStringExtended(this ILangStringNameType ls, int fmt)
        {
            if (fmt == 2)
                return String.Format("{0}@{1}", ls.Text, ls.Language);
            return String.Format("[{0},{1}]", ls.Language, ls.Text);
        }

        public static string ToStringExtended(this List<ILangStringNameType> elems,
            int format = 1, string delimiter = ",")
        {
            return string.Join(delimiter, elems.Select((k) => k.ToStringExtended(format)));
        }
    }
}
