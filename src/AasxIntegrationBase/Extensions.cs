/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
    public static class Extensions
    {
        public static bool HasContent(this string str)
        {
            return str != null && str.Trim() != "";
        }

        public static string SubstringMax(this string str, int pos, int len)
        {
            if (!str.HasContent() || str.Length <= pos)
                return "";
            return str.Substring(pos, Math.Min(len, str.Length));
        }

        public static void SetIfNoContent(ref string s, string input)
        {
            if (!input.HasContent())
                return;
            if (!s.HasContent())
                s = input;
        }

        public static string AddWithDelimiter(this string str, string content, string delimter = "")
        {
            var res = str;
            if (res == null)
                return null;

            if (res.HasContent())
                res += "" + delimter;

            res += content;

            return res;
        }

    }
}
