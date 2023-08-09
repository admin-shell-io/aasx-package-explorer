/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
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
