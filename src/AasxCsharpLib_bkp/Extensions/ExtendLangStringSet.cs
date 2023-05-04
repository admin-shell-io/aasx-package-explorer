using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        // TODO (Jui, 2023-01-05): Check why the generic Copy<T> does not apply here?!
        public static List<LangString> Copy(this List<LangString> original)
        {
            var res = new List<LangString>();
            if (original != null)
                foreach (var o in original)
                    res.Add(o.Copy());
            return res;
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

        public static List<LangString> Parse(string cell)
        {
            // access
            if (cell == null)
                return null;

            // iterative approach
            var res = new List<LangString>();
            while (true)
            {
                // trivial case and finite end
                if (!cell.Contains("@"))
                {
                    if (cell.Trim() != "")
                        res.Add(new LangString(ExtendLangString.LANG_DEFAULT, cell));
                    break;
                }

                // OK, pick the next couple
                var m = Regex.Match(cell, @"(.*?)@(\w+)", RegexOptions.Singleline);
                if (!m.Success)
                {
                    // take emergency exit?
                    res.Add(new LangString("??", cell));
                    break;
                }

                // use the match and shorten cell ..
                res.Add(new LangString(m.Groups[2].ToString(), m.Groups[1].ToString().Trim()));
                cell = cell.Substring(m.Index + m.Length);
            }

            return res;
        }

    }
}
