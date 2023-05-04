using System;
using System.Collections.Generic;

namespace Extensions
{
    //TODO:jtikekar remove or seperate
    public static class ExtendLangStringSet
    {
        #region AasxPackageExplorer

        public static bool IsValid(this List<ILangStringNameType> langStringSet)
        {
            if (langStringSet != null && langStringSet.Count >= 1)
            {
                return true;
            }

            return false;
        }

        #endregion
        public static bool IsEmpty(this List<ILangStringNameType> langStringSet)
        {
            if (langStringSet == null || langStringSet.Count == 0)
            {
                return true;
            }

            return false;
        }
        public static string GetDefaultString(this List<ILangStringTextType> langStringSet, string defaultLang = null)
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

        public static List<T> Create<T>(string language, string text) where T : IAbstractLangString, new()
        {
            return new List<T> { new T { Language = language, Text = text } };
        }

        public static List<ILangStringNameType> CreateLangStringNameType(string language, string text)
        {
            return new List<ILangStringNameType> { new LangStringNameType(language, text) };
        }

        public static List<ILangStringTextType> CreateLangStringTextType(string language, string text)
        {
            return new List<ILangStringTextType> { new LangStringTextType(language, text) };
        }

        public static List<ILangStringPreferredNameTypeIec61360> CreateManyPreferredNamesFromStringArray(string[] s)
        {
            if (s == null)
                return null;
            var r = new List<ILangStringPreferredNameTypeIec61360>();
            var i = 0;
            while ((i + 1) < s.Length)
            {
                r.Add(new LangStringPreferredNameTypeIec61360(s[i], s[i + 1]));
                i += 2;
            }
            return r;
        }
        public static List<ILangStringDefinitionTypeIec61360> CreateManyDefinitionFromStringArray(string[] s)
        {
            if (s == null)
                return null;
            var r = new List<ILangStringDefinitionTypeIec61360>();
            var i = 0;
            while ((i + 1) < s.Length)
            {
                r.Add(new LangStringDefinitionTypeIec61360(s[i], s[i + 1]));
                i += 2;
            }
            return r;
        }

        public static List<ILangStringTextType> Set(this List<ILangStringTextType> lss, string lang, string text)
        {
            foreach (var ls in lss)
                if (ls.Language.Trim().ToLower() == lang?.Trim().ToLower())
                {
                    ls.Text = text;
                    return lss;
                }
            lss.Add(new LangStringTextType(lang, text));
            return lss;
        }

        public static List<ILangStringTextType> ConvertFromV20(
            this List<ILangStringTextType> langStringSet,
            AasxCompatibilityModels.AdminShellV20.LangStringSet sourceLangStrings)
        {

            //if (!sourceLangStrings.langString.IsNullOrEmpty())
            if (sourceLangStrings.langString != null && sourceLangStrings.langString.Count != 0)
            {
                langStringSet = new List<ILangStringTextType>();
                foreach (var sourceLangString in sourceLangStrings.langString)
                {
                    var langString = new LangStringTextType(sourceLangString.lang, sourceLangString.str);
                    langStringSet.Add(langString);
                }
            }
            return langStringSet;
        }


    }
}
