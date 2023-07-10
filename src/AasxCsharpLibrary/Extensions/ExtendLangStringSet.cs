using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Extensions
{
    //TODO (jtikekar, 0000-00-00): remove or seperate
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

        //public static string GetDefaultString(this List<ILangStringTextType> langStringSet, string defaultLang = null)
        //{
        //    return ExtendLangString.GetDefaultStringGen(langStringSet, defaultLang);
        //// start
        //if (defaultLang == null)
        //    defaultLang = "en"; //Default Lang in old implementation is en

        //string res = null;

        //// search
        //foreach (var langString in langStringSet)
        //    if (langString.Language.Equals(defaultLang, StringComparison.OrdinalIgnoreCase))
        //        res = langString.Text;

        //if (res == null && langStringSet.Count > 0)
        //    res = langStringSet[0].Text;

        //// found?
        //return res;
        //}

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

        public static List<T> Parse<T>(string cell,
            Func<string, string, T> createLs) where T : class
        {
            // access
            if (cell == null || createLs == null)
                return null;

            // iterative approach
            var res = new List<T>();
            while (true)
            {
                // trivial case and finite end
                if (!cell.Contains("@"))
                {
                    if (cell.Trim() != "")
                    {
                        res.Add(createLs(ExtendLangString.LANG_DEFAULT, cell));
                    }
                    break;
                }

                // OK, pick the next couple
                var m = Regex.Match(cell, @"(.*?)@(\w+)", RegexOptions.Singleline);
                if (!m.Success)
                {
                    // take emergency exit?
                    res.Add(createLs("??", cell));
                }

                // use the match and shorten cell ..
                res.Add(createLs(m.Groups[2].ToString(), m.Groups[1].ToString().Trim()));
                cell = cell.Substring(m.Index + m.Length);
            }

            return res;
        }

        public static void Add<T>(this List<T> list, string language, string text) where T : IAbstractLangString
        {
            if (typeof(T).IsAssignableFrom(typeof(ILangStringTextType)))
            {
                (list as List<ILangStringTextType>)
                    .Add(new LangStringTextType(language, text));
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringNameType)))
            {
                (list as List<ILangStringNameType>)
                    .Add(new LangStringNameType(language, text));
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringPreferredNameTypeIec61360)))
            {
                (list as List<ILangStringPreferredNameTypeIec61360>)
                    .Add(new LangStringPreferredNameTypeIec61360(language, text));
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringShortNameTypeIec61360)))
            {
                (list as List<ILangStringShortNameTypeIec61360>)
                    .Add(new LangStringShortNameTypeIec61360(language, text));
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringDefinitionTypeIec61360)))
            {
                (list as List<ILangStringDefinitionTypeIec61360>)
                    .Add(new LangStringDefinitionTypeIec61360(language, text));
            }
        }
    }
}
