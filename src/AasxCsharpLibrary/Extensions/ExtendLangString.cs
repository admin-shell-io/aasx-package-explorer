namespace Extensions
{
    //TODO:jtikekar remove
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

        public static IAbstractLangString Create<T>(string language, string text) where T : IAbstractLangString
        {
            if (typeof(T).IsAssignableFrom(typeof(ILangStringTextType)))
            {
                return new LangStringTextType(language, text);
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringNameType)))
            {
                return new LangStringNameType(language, text);
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringPreferredNameTypeIec61360)))
            {
                return new LangStringPreferredNameTypeIec61360(language, text);
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringShortNameTypeIec61360)))
            {
                return new LangStringShortNameTypeIec61360(language, text);
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringDefinitionTypeIec61360)))
            {
                return new LangStringDefinitionTypeIec61360(language, text);
            }
            else
                return null;
        }
    }
}
