/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AdminShellNS
{
    public static class AdminShellUtil
    {
        public static string EvalToNonNullString(string fmt, object o, string elseString = "")
        {
            if (o == null)
                return elseString;
            return string.Format(fmt, o);
        }

        public static string EvalToNonEmptyString(string fmt, string o, string elseString = "")
        {
            if (o == null || o == "")
                return elseString;
            return string.Format(fmt, o);
        }

        /// <summary>Creates a filter-friendly name from the source.</summary>
        /// <example>
        /// <code>Assert.AreEqual("", AdminShellUtil.FilterFriendlyName(""));</code>
        /// <code doctest="true">Assert.AreEqual("someName", AdminShellUtil.FilterFriendlyName("someName"));</code>
        /// <code doctest="true">Assert.AreEqual("some__name", AdminShellUtil.FilterFriendlyName("some!;name"));</code>
        /// </example>
        public static string FilterFriendlyName(string src)
        {
            if (src == null)
                return null;
            return Regex.Replace(src, @"[^a-zA-Z0-9\-_]", "_");
        }

        /// <example>
        /// <code doctest="true">Assert.IsFalse(AdminShellUtil.HasWhitespace(""));</code>
        /// <code doctest="true">Assert.IsTrue(AdminShellUtil.HasWhitespace(" "));</code>
        /// <code doctest="true">Assert.IsTrue(AdminShellUtil.HasWhitespace("aa bb"));</code>
        /// <code doctest="true">Assert.IsTrue(AdminShellUtil.HasWhitespace(" aabb"));</code>
        /// <code doctest="true">Assert.IsTrue(AdminShellUtil.HasWhitespace("aabb "));</code>
        /// <code doctest="true">Assert.IsFalse(AdminShellUtil.HasWhitespace("aabb"));</code>
        /// </example>
        public static bool HasWhitespace(string src)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));
            foreach (var s in src)
                if (char.IsWhiteSpace(s))
                    return true;
            return false;
        }

        /// <code doctest="true">Assert.IsTrue(AdminShellUtil.ComplyIdShort(""));</code>
        public static bool ComplyIdShort(string src)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));
            var res = true;
            foreach (var s in src)
                if (!Char.IsLetterOrDigit(s) && s != '_')
                    res = false;
            if (src.Length > 0 && !Char.IsLetter(src[0]))
                res = false;
            return res;
        }

        public static string ByteSizeHumanReadable(long len)
        {
            // see: https://stackoverflow.com/questions/281640/
            // how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            string res = String.Format("{0:0.##} {1}", len, sizes[order]);
            return res;
        }

        public static string ExtractPascalCasingLetters(string src)
        {
            // access
            src = src?.Trim();
            if (src == null || src.Length < 1)
                return null;

            // walk through
            var res = "";
            var arm = true;
            foreach (var c in src)
            {
                // take?
                if (arm && Char.IsUpper(c))
                    res += c;
                // state for next iteration
                arm = !Char.IsUpper(c);
            }

            // result
            return res;
        }

        public static int CountHeadingSpaces(string line)
        {
            if (line == null)
                return 0;
            int j;
            for (j = 0; j < line.Length; j++)
                if (!Char.IsWhiteSpace(line[j]))
                    break;
            return j;
        }

        /// <summary>
        /// Used to re-reformat a C# here string, which is multiline string introduced by @" ... ";
        /// </summary>
        public static string[] CleanHereStringToArray(string here)
        {
            if (here == null)
                return null;

            // convert all weird breaks to pure new lines
            here = here.Replace("\r\n", "\n");
            here = here.Replace("\n\r", "\n");

            // convert all tabs to spaces
            here = here.Replace("\t", "    ");

            // split these
            var lines = new List<string>(here.Split('\n'));
            if (lines.Count < 1)
                return lines.ToArray();

            // the first line could be special
            string firstLine = null;
            if (lines[0].Trim() != "")
            {
                firstLine = lines[0].Trim();
                lines.RemoveAt(0);
            }

            // detect an constant amount of heading spaces
            var headSpaces = int.MaxValue;
            foreach (var line in lines)
                if (line.Trim() != "")
                    headSpaces = Math.Min(headSpaces, CountHeadingSpaces(line));

            // multi line trim possible?
            if (headSpaces != int.MaxValue && headSpaces > 0)
                for (int i = 0; i < lines.Count; i++)
                    if (lines[i].Length > headSpaces)
                        lines[i] = lines[i].Substring(headSpaces);

            // re-compose again
            if (firstLine != null)
                lines.Insert(0, firstLine);

            // return
            return lines.ToArray();
        }

        /// <summary>
        /// Used to re-reformat a C# here string, which is multiline string introduced by @" ... ";
        /// </summary>
        public static string CleanHereStringWithNewlines(string here, string nl = null)
        {
            if (nl == null)
                nl = Environment.NewLine;
            var lines = CleanHereStringToArray(here);
            if (lines == null)
                return null;
            return String.Join(nl, lines);
        }

        public static string ShortLocation(Exception ex)
        {
            if (ex == null || ex.StackTrace == null)
                return "";
            string[] lines = ex.StackTrace.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 1)
                return "";
            // search for " in "
            // as the most actual stacktrace might be a built-in function, this might not work and therefore
            // go down in the stack
            int currLine = 0;
            while (true)
            {
                // nothing found at all
                if (currLine >= lines.Length)
                    return "";
                // access current line
                /* TODO (MIHO, 2020-11-12): replace with Regex for multi language. Ideally have Exception messages
                   always as English. */
                var p = lines[currLine].IndexOf(" in ", StringComparison.Ordinal);
                if (p < 0)
                    p = lines[currLine].IndexOf(" bei ", StringComparison.Ordinal);
                if (p < 0)
                {
                    // advance to next oldest line
                    currLine++;
                    continue;
                }
                // search last "\" or "/", to get only filename portion and position
                p = lines[currLine].LastIndexOfAny(new[] { '\\', '/' });
                if (p < 0)
                {
                    // advance to next oldest line
                    currLine++;
                    continue;
                }
                // return this
                return lines[currLine].Substring(p);
            }
        }

        public enum ConstantFoundEnum { No, AnyCase, ExactCase }

        public static ConstantFoundEnum CheckIfInConstantStringArray(string[] arr, string str)
        {
            if (arr == null || str == null)
                return ConstantFoundEnum.No;

            bool anyCaseFound = false;
            bool exactCaseFound = false;
            foreach (var a in arr)
            {
                anyCaseFound = anyCaseFound || str.ToLower() == a.ToLower();
                exactCaseFound = exactCaseFound || str == a;
            }
            if (exactCaseFound)
                return ConstantFoundEnum.ExactCase;
            if (anyCaseFound)
                return ConstantFoundEnum.AnyCase;
            return ConstantFoundEnum.No;
        }

        public static string CorrectCasingForConstantStringArray(string[] arr, string str)
        {
            if (arr == null || str == null)
                return str;

            foreach (var a in arr)
                if (str.ToLower() == a.ToLower())
                    return a;

            return str;
        }

        //
        //
        //
        //
        //


        //
        // String manipulations
        //

        public static string ReplacePercentPlaceholder(
            string input,
            string searchFor,
            Func<string> substLamda,
            StringComparison comparisonType = StringComparison.InvariantCulture)
        {
            // access
            if (input == null || searchFor == null || searchFor == "")
                return input;

            // find
            while (true)
            {
                // any occurence
                var p = input.IndexOf(searchFor, comparisonType);
                if (p < 0)
                    break;

                // split
                var left = input.Substring(0, p);
                var right = "";
                var rp = p + searchFor.Length;
                if (rp < input.Length)
                    right = input.Substring(rp);

                // lambda
                var repl = "" + substLamda?.Invoke();

                // build new
                input = left + repl + right;
            }

            // ok
            return input;
        }

        //
        // Reflection
        //

        public static void SetFieldLazyValue(FieldInfo f, object obj, object value)
        {
            // access
            if (f == null || obj == null)
                return;

            switch (Type.GetTypeCode(f.FieldType))
            {
                case TypeCode.String:
                    f.SetValue(obj, "" + value);
                    break;

                case TypeCode.Byte:
                    if (Byte.TryParse("" + value, out var ui8))
                        f.SetValue(obj, ui8);
                    break;

                case TypeCode.SByte:
                    if (SByte.TryParse("" + value, out var i8))
                        f.SetValue(obj, i8);
                    break;

                case TypeCode.Int16:
                    if (Int16.TryParse("" + value, out var i16))
                        f.SetValue(obj, i16);
                    break;

                case TypeCode.Int32:
                    if (Int32.TryParse("" + value, out var i32))
                        f.SetValue(obj, i32);
                    break;

                case TypeCode.Int64:
                    if (Int64.TryParse("" + value, out var i64))
                        f.SetValue(obj, i64);
                    break;

                case TypeCode.UInt16:
                    if (UInt16.TryParse("" + value, out var ui16))
                        f.SetValue(obj, ui16);
                    break;

                case TypeCode.UInt32:
                    if (UInt32.TryParse("" + value, out var ui32))
                        f.SetValue(obj, ui32);
                    break;

                case TypeCode.UInt64:
                    if (UInt64.TryParse("" + value, out var ui64))
                        f.SetValue(obj, ui64);
                    break;

                case TypeCode.Single:
                    if (Single.TryParse("" + value, out var sgl))
                        f.SetValue(obj, sgl);
                    break;

                case TypeCode.Double:
                    if (Double.TryParse("" + value, out var dbl))
                        f.SetValue(obj, dbl);
                    break;

                case TypeCode.Boolean:
                    var isFalse = value == null
                        || (value is int vi && vi == 0)
                        || (value is string vs && vs == "")
                        || (value is bool vb && !vb);
                    f.SetValue(obj, isFalse);
                    break;
            }
        }

        //
        // Base 64
        //

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        //
        // Generation of Ids
        //

        private static Random MyRnd = new Random();

        public static string GenerateIdAccordingTemplate(string tpl)
        {
            // generate a deterministic decimal digit string
            var decimals = String.Format("{0:ffffyyMMddHHmmss}", DateTime.UtcNow);
            decimals = new string(decimals.Reverse().ToArray());
            // convert this to an int
            if (!Int64.TryParse(decimals, out Int64 decii))
                decii = MyRnd.Next(Int32.MaxValue);
            // make an hex out of this
            string hexamals = decii.ToString("X");
            // make an alphanumeric string out of this
            string alphamals = "";
            var dii = decii;
            while (dii >= 1)
            {
                var m = dii % 26;
                alphamals += Convert.ToChar(65 + m);
                dii = dii / 26;
            }

            // now, "salt" the strings
            for (int i = 0; i < 32; i++)
            {
                var c = Convert.ToChar(48 + MyRnd.Next(10));
                decimals += c;
                hexamals += c;
                alphamals += c;
            }

            // now, can just use the template
            var id = "";
            foreach (var tpli in tpl)
            {
                if (tpli == 'D' && decimals.Length > 0)
                {
                    id += decimals[0];
                    decimals = decimals.Remove(0, 1);
                }
                else
                if (tpli == 'X' && hexamals.Length > 0)
                {
                    id += hexamals[0];
                    hexamals = hexamals.Remove(0, 1);
                }
                else
                if (tpli == 'A' && alphamals.Length > 0)
                {
                    id += alphamals[0];
                    alphamals = alphamals.Remove(0, 1);
                }
                else
                    id += tpli;
            }

            // ok
            return id;
        }

    }
}
