/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasCore.Aas3_0_RC02;
using AdminShellNS.Extenstions;
using Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AasxCompatibilityModels.AdminShellV20.SubmodelElementWrapper;
using static Extensions.ExtendIDataSpecificationContent;

namespace AdminShellNS
{
    public static class AdminShellUtil
    {

        #region Various utilities
        // ------------------------------------------------------------------------------------

        public static T[] GetEnumValues<T>() where T : Enum
            => (T[])Enum.GetValues(typeof(T));

        public static IEnumerable<T> GetEnumValues<T>(T[] excludes) where T : Enum
        {
            foreach (var v in (T[])Enum.GetValues(typeof(T)))
                if (!excludes.Contains(v))
                    yield return v;
        }

        #endregion

        #region V3 Methods

        public static string[] GetPopularMimeTypes()
        {
            return
                new[] {
                    System.Net.Mime.MediaTypeNames.Text.Plain,
                    System.Net.Mime.MediaTypeNames.Text.Xml,
                    System.Net.Mime.MediaTypeNames.Text.Html,
                    "application/json",
                    "application/rdf+xml",
                    System.Net.Mime.MediaTypeNames.Application.Pdf,
                    System.Net.Mime.MediaTypeNames.Image.Jpeg,
                    "image/png",
                    System.Net.Mime.MediaTypeNames.Image.Gif,
                    "application/iges",
                    "application/step"
                };
        }

        public static IEnumerable<AasSubmodelElements> GetAdequateEnums(AasSubmodelElements[] excludeValues = null, AasSubmodelElements[] includeValues = null)
        {
            if (includeValues != null)
            {
                foreach (var en in includeValues)
                    yield return en;
            }
            else
            {
                foreach (var en in (AasSubmodelElements[])Enum.GetValues(typeof(AasSubmodelElements)))
                {
                    if (en == AasSubmodelElements.SubmodelElement)
                        continue;
                    if (excludeValues != null && excludeValues.Contains(en))
                        continue;
                    yield return en;
                }
            }
        }

        public static ISubmodelElement CreateSubmodelElementFromEnum(AasSubmodelElements smeEnum, ISubmodelElement sourceSme = null)
        {
            switch(smeEnum)
            {
                case AasSubmodelElements.Property:
                    {
                        return new Property(DataTypeDefXsd.String).UpdateFrom(sourceSme);
                    }
                case AasSubmodelElements.MultiLanguageProperty:
                    {
                        return new MultiLanguageProperty().UpdateFrom(sourceSme);
                    }
                case AasSubmodelElements.Range:
                    {
                        return new AasCore.Aas3_0_RC02.Range(DataTypeDefXsd.String).UpdateFrom(sourceSme);
                    }
                case AasSubmodelElements.File:
                    {
                        return new AasCore.Aas3_0_RC02.File("").UpdateFrom(sourceSme);
                    }
                case AasSubmodelElements.Blob:
                    {
                        return new Blob("").UpdateFrom(sourceSme);
                    }
                case AasSubmodelElements.ReferenceElement:
                    {
                        return new ReferenceElement().UpdateFrom(sourceSme);
                    }
                case AasSubmodelElements.RelationshipElement:
                    {
                        return new RelationshipElement(null, null).UpdateFrom(sourceSme);
                    }
                case AasSubmodelElements.AnnotatedRelationshipElement:
                    {
                        return new AnnotatedRelationshipElement(null, null).UpdateFrom(sourceSme);
                    }
                case AasSubmodelElements.Capability:
                    {
                        return new Capability();
                    }
                case AasSubmodelElements.SubmodelElementCollection:
                    {
                        return new SubmodelElementCollection().UpdateFrom(sourceSme);
                    }
                case AasSubmodelElements.SubmodelElementList:
                    {
                        return new SubmodelElementList(AasSubmodelElements.SubmodelElement).UpdateFrom(sourceSme);
                    }
                case AasSubmodelElements.Operation:
                    {
                        return new Operation().UpdateFrom(sourceSme);
                    }
                case AasSubmodelElements.BasicEventElement:
                    {
                        return new BasicEventElement(null, Direction.Input, StateOfEvent.Off);
                    }
                case AasSubmodelElements.Entity:
                    {
                        return new Entity(EntityType.SelfManagedEntity);
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        #endregion
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

        /// <summary>
        /// If len of <paramref name="str"/> exceeds <paramref name="maxLen"/> then
        /// string is shortened and returned with an ellipsis(…) at the end.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="maxLen"></param>
        /// <returns>Shortened string</returns>
        public static string ShortenWithEllipses(string str, int maxLen)
        {
            if (str == null)
                return null;
            if (maxLen >= 0 && str.Length > maxLen)
                str = str.Substring(0, maxLen) + "\u2026";
            return str;
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
                nl = System.Environment.NewLine;
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

        public static void PrintSearchableProperties(object obj, int indent)
        {
            if (obj == null) return;
            string indentString = new string(' ', indent);
            Type objType = obj.GetType();
            PropertyInfo[] properties = objType.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                object propValue = property.GetValue(obj, null);
                var elems = propValue as IList;
                if (elems != null)
                {
                    foreach (var item in elems)
                    {
                        PrintSearchableProperties(item, indent + 3);
                    }
                }
                else
                {
                    // This will not cut-off System.Collections because of the first check
                    if (property.PropertyType.Assembly == objType.Assembly)
                    {
                        Console.WriteLine("{0}{1}:", indentString, property.Name);

                        PrintSearchableProperties(propValue, indent + 2);
                    }
                    else
                    {
                        Console.WriteLine("{0}{1}: {2}", indentString, property.Name, propValue);
                    }
                }
            }
        }

        public class SearchOptions
        {
            public Assembly[] allowedAssemblies = null;
            public int maxDepth = int.MaxValue;
            public bool findFirst = false;
            public int skipFirstResults = 0;
            public string findText = null;
            public bool isIgnoreCase = false;
            public bool isRegex = false;
        }

        public class SearchResultItem : IEquatable<SearchResultItem>
        {
            public SearchOptions searchOptions;
            public string qualifiedNameHead;
            public string metaModelName;
            public object businessObject;
            public string foundText;
            public object foundObject;
            public object containingObject;
            public int foundHash;

            public bool Equals(SearchResultItem other)
            {
                if (other == null)
                    return false;

                return this.qualifiedNameHead == other.qualifiedNameHead &&
                       this.metaModelName == other.metaModelName &&
                       this.businessObject == other.businessObject &&
                       this.containingObject == other.containingObject &&
                       this.foundText == other.foundText &&
                       this.foundHash == other.foundHash;
            }
        }

        public class SearchResults
        {
            public int foundIndex = 0;
            public List<SearchResultItem> foundResults = new List<SearchResultItem>();

            public void Clear()
            {
                foundIndex = -1;
                foundResults.Clear();
            }
        }

        public static void CheckSearchable(
            SearchResults results, SearchOptions options, string qualifiedNameHead, object businessObject,
            MemberInfo mi, object memberValue, object containingObject, Func<int> getMemberHash)
        {
            // try get a speaking name
            var metaModelName = "<unknown>";
            var x1 = mi.GetCustomAttribute<AasxCompatibilityModels.AdminShell.MetaModelName>(); //TODO: jtikekar need to test
            if (x1 != null && x1.name != null)
                metaModelName = x1.name;

            // check if this object is searchable
            var x2 = mi.GetCustomAttribute<AasxCompatibilityModels.AdminShell.TextSearchable>();
            if (x2 != null)
            {
                // what to check?
                string foundText = "" + memberValue?.ToString();

                // find options
                var found = true;
                if (options.findText != null)
                    found = foundText.IndexOf(
                        options.findText, options.isIgnoreCase ? StringComparison.CurrentCultureIgnoreCase : 0) >= 0;

                // add?
                if (found)
                {
                    var sri = new SearchResultItem();
                    sri.searchOptions = options;
                    sri.qualifiedNameHead = qualifiedNameHead;
                    sri.metaModelName = metaModelName;
                    sri.businessObject = businessObject;
                    sri.foundText = foundText;
                    sri.foundObject = memberValue;
                    sri.containingObject = containingObject;
                    if (getMemberHash != null)
                        sri.foundHash = getMemberHash();

                    // avoid duplicates
                    if (!results.foundResults.Contains(sri))
                        results.foundResults.Add(sri);
                }
            }
        }

        public static void EnumerateSearchable(
            SearchResults results, object obj, string qualifiedNameHead, int depth, SearchOptions options,
            object businessObject = null)
        {
            // access
            if (results == null || obj == null || options == null)
                return;
            Type objType = obj.GetType();

            // depth
            if (depth > options.maxDepth)
                return;

            // try to get element name of an AAS entity
            string elName = null;
            if (obj is IReferable referable)
            {
                //elName = (obj as IReferable).GetElementName();
                elName = obj.GetType().Name;
                businessObject = obj;
            }

            // enrich qualified name, accordingly
            var qualifiedName = qualifiedNameHead;
            if (elName != null)
                qualifiedName = qualifiedName + (qualifiedName.Length > 0 ? "." : "") + elName;

            // do NOT dive into objects, which are not in the reight assembly
            if (options.allowedAssemblies == null || !options.allowedAssemblies.Contains(objType.Assembly))
                return;

            // do not dive into enums
            if (objType.IsEnum)
                return;

            // look at fields, first
            var fields = objType.GetFields();
            foreach (var fi in fields)
            {
                // is the object marked to be skipped?
                var x3 = fi.GetCustomAttribute<AasxCompatibilityModels.AdminShell.SkipForReflection>();
                if (x3 != null)
                    continue;

                var x4 = fi.GetCustomAttribute<AasxCompatibilityModels.AdminShell.SkipForSearch>();
                if (x4 != null)
                    continue;

                // get value(s)
                var fieldValue = fi.GetValue(obj);
                if (fieldValue == null)
                    continue;
                var valueElems = fieldValue as IList;
                if (valueElems != null)
                {
                    // field is a collection .. dive deeper, if allowed
                    foreach (var el in valueElems)
                        EnumerateSearchable(results, el, qualifiedName, depth + 1, options, businessObject);
                }
                else
                {
                    // field is a single entity .. check it
                    CheckSearchable(
                        results, options, qualifiedName, businessObject, fi, fieldValue, obj,
                        () => { return fieldValue.GetHashCode(); });

                    // dive deeper ..
                    EnumerateSearchable(results, fieldValue, qualifiedName, depth + 1, options, businessObject);
                }
            }

            // properties & objects behind
            var properties = objType.GetProperties();
            foreach (var pi in properties)
            {
                var gip = pi.GetIndexParameters();
                if (gip.Length > 0)
                    // no indexed properties, yet
                    continue;

                // is the object marked to be skipped?
                var x3 = pi.GetCustomAttribute<AasxCompatibilityModels.AdminShell.SkipForReflection>();
                if (x3 != null)
                    continue;

                var x4 = pi.GetCustomAttribute<AasxCompatibilityModels.AdminShell.SkipForSearch>();
                if (x4 != null)
                    continue;

                // get value(s)
                var propValue = pi.GetValue(obj, null);
                if (propValue == null)
                    continue;
                var valueElems = propValue as IList;
                if (valueElems != null)
                {
                    // property is a collection .. dive deeper, if allowed
                    foreach (var el in valueElems)
                        EnumerateSearchable(results, el, qualifiedName, depth + 1, options, businessObject);
                }
                else
                {
                    // field is a single entity .. check it
                    CheckSearchable(
                        results, options, qualifiedName, businessObject, pi, propValue, obj,
                        () => { return propValue.GetHashCode(); });

                    // dive deeper ..
                    EnumerateSearchable(results, propValue, qualifiedName, depth + 1, options, businessObject);
                }
            }
        }

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

        public static string RemoveNewLinesAndLimit(string input, int maxLength = -1, string ellipsis = "..")
        {
            // access
            if (input == null)
                return null;

            // maybe do a generouse limit first
            if (maxLength >= 1 && input.Length > 2 * maxLength)
                input = input.Substring(0, 2 * maxLength);

            // now do expensive operations
            input = input.Replace('\r', ' ');
            input = input.Replace('\n', ' ');
            input = Regex.Replace(input, @"\s+", " ", RegexOptions.Compiled);

            // now apply exact limit
            if (maxLength >= 1 && input.Length > maxLength)
                input = input.Substring(0, maxLength) + ellipsis;

            // ok
            return input;
        }


    }
}
