using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendILangStringTextType
    {
        public static string GetDefaultString(this List<ILangStringTextType> langStringSet, string defaultLang = null)
        {
            return ExtendLangString.GetDefaultStringGen(langStringSet, defaultLang);
        }

        public static string ToStringExtended(this ILangStringTextType ls, int fmt)
        {
            if (fmt == 2)
                return String.Format("{0}@{1}", ls.Text, ls.Language);
            return String.Format("[{0},{1}]", ls.Language, ls.Text);
        }

        public static string ToStringExtended(this List<ILangStringTextType> elems, 
            int format = 1, string delimiter = ",")
        {
            return string.Join(delimiter, elems.Select((k) => k.ToStringExtended(format)));
        }
    }
}
