/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxIntegrationBase
{
    public static class AasxLanguageHelper
    {
        public enum LangEnum { Any = 0, EN, DE, CN, JP, KR, FR, ES };

        public static string[] LangEnumToISO639String = {
                "All", "en", "de", "cn", "jp", "kr", "fr", "es" }; // ISO 639 -> List of languages

        public static string[] LangEnumToISO3166String = {
                "All", "GB", "DE", "CN", "JP", "KR", "FR", "ES" }; // ISO 3166 -> List of countries

        public static string GetLangCodeFromEnum(LangEnum le)
        {
            return "" + LangEnumToISO639String[(int)le];
        }

        public static string GetCountryCodeFromEnum(LangEnum le)
        {
            return "" + LangEnumToISO3166String[(int)le];
        }

        public static LangEnum FindLangEnumFromLangCode(string candidate)
        {
            if (candidate == null)
                return LangEnum.Any;
            candidate = candidate.ToLower().Trim();
            foreach (var ev in (LangEnum[])Enum.GetValues(typeof(LangEnum)))
                if (candidate == LangEnumToISO639String[(int)ev]?.ToLower())
                    return ev;
            return LangEnum.Any;
        }

        public static LangEnum FindLangEnumFromCountryCode(string candidate)
        {
            if (candidate == null)
                return LangEnum.Any;
            candidate = candidate.ToUpper().Trim();
            foreach (var ev in (LangEnum[])Enum.GetValues(typeof(LangEnum)))
                if (candidate == LangEnumToISO3166String[(int)ev]?.ToUpper())
                    return ev;
            return LangEnum.Any;
        }

        public static IEnumerable<string> GetLangCodes()
        {
            for (int i = 1; i < LangEnumToISO639String.Length; i++)
                yield return LangEnumToISO639String[i];
        }
    }
}
