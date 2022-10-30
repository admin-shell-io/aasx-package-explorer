/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections;
using System.Collections.Generic;
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

            public bool isWholeWord = false;
            public bool isIgnoreCase = false;
            public bool isRegex = false;

            public bool searchCollection = true;
            public bool searchProperty = true;
            public bool searchMultiLang = true;
            public bool searchOther = true;
            public string searchLanguage = "";

            public RegexOptions CompiledRegOpt = RegexOptions.None;
            public Regex CompiledRegex = null;

            public void CompileOptions()
            {
                CompiledRegex = null;
                CompiledRegOpt = RegexOptions.None;
                if (isRegex)
                {
                    CompiledRegOpt = RegexOptions.Compiled | RegexOptions.CultureInvariant;
                    if (isIgnoreCase)
                        CompiledRegOpt |= RegexOptions.IgnoreCase;
                    CompiledRegex = new Regex(findText, CompiledRegOpt);
                }
            }
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
            public Match foundMatch = null;

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

            public override string ToString()
            {
                var idn = "";
                if (businessObject is AdminShell.Referable rf)
                    idn = "." + rf.idShort;
                return "" + qualifiedNameHead + idn + "." + metaModelName;
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

        /// <summary>
        /// Internal function used by <c>EnumerateSearchable</c>
        /// </summary>
        private static void CheckSearchable(
            SearchResults results, SearchOptions options, string qualifiedNameHead, object businessObject,
            MemberInfo mi, object memberValue, object containingObject, Func<int> getMemberHash,
            Action<bool, int> progress = null)
        {
            // try get a speaking name
            var metaModelName = "<unknown>";
            var x1 = mi.GetCustomAttribute<AdminShell.MetaModelName>();
            if (x1 != null && x1.name != null)
                metaModelName = x1.name;

            // check if this object is searchable
            var x2 = mi.GetCustomAttribute<AdminShell.TextSearchable>();
            if (x2 != null)
            {
                // what to check?
                string foundText = "" + memberValue?.ToString();

                // quite late: investigate, if we accepted findings from the 
                // type of business element
                var isColl = (businessObject is AdminShell.AdministrationShell
                        || businessObject is AdminShell.Submodel
                        || businessObject is AdminShell.SubmodelElementCollection);

                var isProp = (businessObject is AdminShell.Property);

                var isMLP = (businessObject is AdminShell.MultiLanguageProperty);


                if (!options.searchCollection && isColl) 
                    return;

                if (!options.searchProperty && isProp)
                    return;

                if (!options.searchMultiLang && isMLP)
                    return;

                if (!options.searchOther && !(isColl || isProp || isMLP))
                    return;

                // find options
                var found = true;
                Match foundMatch = null;
                if (options.findText != null)
                {
                    if (options.isRegex && options.CompiledRegex != null)
                    {
                        foundMatch = options.CompiledRegex.Match(foundText);
                        found = foundMatch != null && foundMatch.Success;
                    }
                    else
                    if (options.isWholeWord)
                    {
                        found = string.Equals(foundText, options.findText,
                            options.isIgnoreCase ? StringComparison.CurrentCultureIgnoreCase : 0);
                    }
                    else
                    {
                        found = foundText.IndexOf(options.findText, 
                            options.isIgnoreCase ? StringComparison.CurrentCultureIgnoreCase : 0) >= 0;
                    }
                }

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
                    
                    sri.foundMatch = foundMatch;

                    // avoid duplicates
                    if (!results.foundResults.Contains(sri))
                        results.foundResults.Add(sri);

                    // progress
                    progress?.Invoke(true, results.foundResults.Count % 50);
                }
            }
        }

        /// <summary>
        /// Uses reflection to investigate the members of a data structure, which is supposely a
        /// structure of the AAS meta model. Inspected classes need to be in <c>options.allowedAssemblies</c>
        /// and classes shall be annotated with attributes <c>AdminShell.MetaModelName</c> and
        /// <c>AdminShell.TextSearchable</c>. Inspection recursion is controlled via attributes
        /// <c>AdminShell.SkipForReflection</c> and <c>AdminShell.SkipForSearch</c>
        /// </summary>
        /// <param name="results">Found result items</param>
        /// <param name="obj">Root of the data structures to be inspected</param>
        /// <param name="qualifiedNameHead">used for recursion</param>
        /// <param name="depth">used for recursion</param>
        /// <param name="options">Search options</param>
        /// <param name="businessObject">used for recursion</param>
        public static void EnumerateSearchable(
            SearchResults results, object obj, string qualifiedNameHead, int depth, SearchOptions options,
            object businessObject = null,
            Action<bool, int> progress = null)
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
            if (obj is AdminShell.Referable)
            {
                elName = (obj as AdminShell.Referable).GetElementName();
                businessObject = obj;
            }

            // enrich qualified name, accordingly
            var qualifiedName = qualifiedNameHead;
            if (elName != null)
                qualifiedName = qualifiedName + (qualifiedName.Length > 0 ? "." : "") + elName;

            // do NOT dive into objects, which are not in the right assembly
            if (options.allowedAssemblies == null || !options.allowedAssemblies.Contains(objType.Assembly))
                return;

            // do not dive into enums
            if (objType.IsEnum)
                return;

            // report a "false" progress
            progress?.Invoke(false, 0);

            // look at fields, first
            var fields = objType.GetFields();
            foreach (var fi in fields)
            {
                // is the object marked to be skipped?
                var x3 = fi.GetCustomAttribute<AdminShell.SkipForReflection>();
                if (x3 != null)
                    continue;

                var x4 = fi.GetCustomAttribute<AdminShell.SkipForSearch>();
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
                        EnumerateSearchable(results, el, qualifiedName, depth + 1, options, businessObject, progress);
                }
                else
                {
                    // field is a single entity .. check it
                    CheckSearchable(
                        results, options, qualifiedName, businessObject, fi, fieldValue, obj,
                        () => { return fieldValue.GetHashCode(); }, progress);

                    // dive deeper ..
                    EnumerateSearchable(results, fieldValue, qualifiedName, depth + 1, options, businessObject, progress);
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
                var x3 = pi.GetCustomAttribute<AdminShell.SkipForReflection>();
                if (x3 != null)
                    continue;

                var x4 = pi.GetCustomAttribute<AdminShell.SkipForSearch>();
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
                        EnumerateSearchable(results, el, qualifiedName, depth + 1, options, businessObject, progress);
                }
                else
                {
                    // field is a single entity .. check it
                    CheckSearchable(
                        results, options, qualifiedName, businessObject, pi, propValue, obj,
                        () => { return propValue.GetHashCode(); }, progress);

                    // dive deeper ..
                    EnumerateSearchable(results, propValue, qualifiedName, depth + 1, options, businessObject, progress);
                }
            }
        }

        /// <summary>
        /// Internal function for <c>ReplaceInSearchable</c>
        /// </summary>
        /// <returns>New member object, if to set.</returns>
        private static object ReplaceInSearchableMember(
            SearchOptions options,
            SearchResultItem item,
            string replaceText,
            object member)
        {
            // access
            if (options == null || item == null || replaceText == null || member == null)
                return null;

            // member type
            if (!(member is string memstr))
            {
                throw new NotImplementedException("ReplaceInSearchableMember::No string member");
            }    

            // regex?
            if (options.isRegex)
            {
                memstr = Regex.Replace(memstr, options.findText, replaceText, options.CompiledRegOpt);
                return memstr;
            }
            else
            {
                // plain text replacement
                memstr = memstr.Replace(options.findText, replaceText);
                return memstr;
            }
        }

        /// <summary>
        /// Do a replacement according to the <c>options</c> in one search result item.
        /// </summary>
        public static void ReplaceInSearchable(
            SearchOptions options,
            SearchResultItem item,
            string replaceText)
        {
            // access
            if (options == null || item == null || replaceText == null)
                return;

            // access in item
            var obj = item.containingObject;
            if (obj == null)
                return;

            // access the object
            Type objType = obj.GetType();
            if (objType == null)
                return;

            // reflect thru this object
            // look at fields, first
            var fields = objType.GetFields();
            foreach (var fi in fields)
            {
                // get value(s)
                var fieldValue = fi.GetValue(obj);
                if (fieldValue == null)
                    continue;

                // hash check on the fieldValue
                if (fieldValue.GetHashCode() == item.foundHash)
                {
                    var newval = ReplaceInSearchableMember(options, item, replaceText, fieldValue);
                    if (newval != null)
                        fi.SetValue(obj, newval);
                }
            }

            // properties & objects behind
            var properties = objType.GetProperties();
            foreach (var pi in properties)
            {
                // get value(s)
                var propValue = pi.GetValue(obj, null);
                if (propValue == null)
                    continue;

                // hash check on the propValue
                if (propValue.GetHashCode() == item.foundHash)
                {
                    ReplaceInSearchableMember(options, item, replaceText, propValue);
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

    }
}
