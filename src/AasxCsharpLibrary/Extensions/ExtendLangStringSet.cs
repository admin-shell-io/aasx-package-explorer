using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;

namespace Extensions
{
    public static class ExtendLangStringSet
    {
        #region AasxPackageExplorer

        public static bool IsValid(this List<LangString> langStringSet)
        {
            if(langStringSet != null && langStringSet.Count >=1)
            {
                return true;
            }

            return false;
        }

        #endregion
        public static bool IsEmpty(this List<LangString> langStringSet)
        {
            if (langStringSet == null || langStringSet.Count == 0)
            {
                return true;
            }

            return false;
        }
        public static string GetDefaultString(this List<LangString> langStringSet, string defaultLang = null)
        {
            // start
            if (defaultLang == null)
                defaultLang = "en"; //Default Lang in old implementation is en

            string res = null;

            // search
            foreach (var langString in langStringSet)
                if (langString.Language.Equals(defaultLang, StringComparison.OrdinalIgnoreCase))
                    res = langString.Text;

            if (res == null && langStringSet.Count > 0)
                res = langStringSet[0].Text;

            // found?
            return res;
        }

        public static List<LangString> Create(string language, string text)
        {
            return new List<LangString> { new LangString(language, text) };
        }

        public static List<LangString> CreateManyFromStringArray(string[] s)
        {
            if (s == null)
                return null;
            var r = new List<LangString>();
            var i = 0;
            while ((i + 1) < s.Length)
            {
                r.Add(new LangString(s[i], s[i + 1]));
                i += 2;
            }
            return r;
        }

        public static List<LangString> Set(this List<LangString> lss, string lang, string text)
        {
            foreach (var ls in lss)
                if (ls.Language.Trim().ToLower() == lang?.Trim().ToLower())
                {
                    ls.Text = text;
                    return lss;
                }
            lss.Add(new LangString(lang, text));
            return lss;
        }

        public static List<LangString> ConvertFromV20(
            this List<LangString> langStringSet, 
            AasxCompatibilityModels.AdminShellV20.LangStringSet sourceLangStrings)
        {

            //if (!sourceLangStrings.langString.IsNullOrEmpty())
            if (sourceLangStrings.langString!= null && sourceLangStrings.langString.Count != 0)
            {
                langStringSet = new List<LangString>();
                foreach (var sourceLangString in sourceLangStrings.langString)
                {
                    var langString = new LangString(sourceLangString.lang, sourceLangString.str);
                    langStringSet.Add(langString);
                }
            }
            return langStringSet;
        }

        public static List<LangString> ConvertFromV20(
            this List<LangString> lss, 
            AasxCompatibilityModels.AdminShellV20.LangStringSetIEC61360 src)
        {

            //if (!sourceLangStrings.langString.IsNullOrEmpty())
            if (src != null && src.Count != 0)
            {
                lss = new List<LangString>();
                foreach (var sourceLangString in src)
                {
                    var langString = new LangString(sourceLangString.lang, sourceLangString.str);
                    lss.Add(langString);
                }
            }
            return lss;
        }
    }
}
