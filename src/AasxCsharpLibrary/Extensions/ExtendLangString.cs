using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendLangString
    {
        // constants
        public static string LANG_DEFAULT = "en";

        // MIHO: not required, see ExtendLangStringSte
        //public static string GetDefaultString(this List<LangString> langStrings, string defaultLang = null)
        //{
        //    // start
        //    if (defaultLang == null)
        //        defaultLang = "en";
        //    defaultLang = defaultLang.Trim().ToLower();
        //    string res = null;

        //    // search
        //    foreach (var ls in langStrings)
        //        if (ls.Language.Trim().ToLower() == defaultLang)
        //            res = ls.Text;
        //    if (res == null && langStrings.Count > 0)
        //        res = langStrings[0].Text;

        //    // found?
        //    return res;
        //}
    }
}
