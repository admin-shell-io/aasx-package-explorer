/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
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
using AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxIntegrationBase
{
    /// <summary>
    /// This call implements some functions to search entities in the AdminShell data structures.
    /// </summary>
    public static class AasxSearchUtil
    {
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
            /// <summary>
            /// Search might restrict assemblies to be searched in
            /// </summary>
            public Assembly[] AllowedAssemblies = null;

            /// <summary>
            /// Search might restrict recursive depth to not go into loops
            /// </summary>
            public int MaxDepth = int.MaxValue;

            [AasxMenuArgument(help: "Specifies the text to be searched.")]
            public string FindText = null;

            [AasxMenuArgument(help: "Specifies the text to be replaced with.")]
            public string ReplaceText = null;

            [AasxMenuArgument(help: "'True' if only whole words are to be searched.")]
            public bool IsWholeWord = false;

            [AasxMenuArgument(help: "'True' if to ignore upper/ lower case when searching.")]
            public bool IsIgnoreCase = false;

            [AasxMenuArgument(help: "'True' if to use regular expressions for searching.")]
            public bool IsRegex = false;

            [AasxMenuArgument(help: "'True' if texts in SubmodelElementCollections shall be searched.")]
            public bool SearchCollection = true;

            [AasxMenuArgument(help: "'True' if texts in Properties shall be searched.")]
            public bool SearchProperty = true;

            [AasxMenuArgument(help: "'True' if texts in MultiLanguageProperties shall be searched.")]
            public bool SearchMultiLang = true;

            [AasxMenuArgument(help: "'True' if texts in other elements shall be searched.")]
            public bool SearchOther = true;

            [AasxMenuArgument(help: "If string present, restricts the search to a certain language " +
                "given by that string in multi language entities.")]
            public string SearchLanguage = "";

            /// <summary>
            /// For sake of speed, regex'es will be compiled
            /// </summary>
            public RegexOptions CompiledRegOpt = RegexOptions.None;

            /// <summary>
            /// For sake of speed, regex'es will be compiled
            /// </summary>
            public Regex CompiledRegex = null;

            /// <summary>
            /// Perform compilation for regex'es.
            /// </summary>
            public void CompileOptions()
            {
                CompiledRegex = null;
                CompiledRegOpt = RegexOptions.None;
                if (IsRegex)
                {
                    CompiledRegOpt = RegexOptions.Compiled | RegexOptions.CultureInvariant;
                    if (IsIgnoreCase)
                        CompiledRegOpt |= RegexOptions.IgnoreCase;
                    CompiledRegex = new Regex(FindText, CompiledRegOpt);
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
                if (businessObject is IReferable rf)
                    idn = "." + rf.IdShort;
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
        public static void CheckSearchable(
            SearchResults results, SearchOptions options, string qualifiedNameHead, object businessObject,
            MemberInfo mi, object memberValue, object containingObject, Func<int> getMemberHash,
            Action<bool, int> progress = null)
        {
            // try get a speaking name
            var metaModelName = "<unknown>";
            var x1 = mi.GetCustomAttribute<AasCore.Aas3_0_RC02.Attributes.MetaModelName>();
            if (x1 != null && x1.name != null)
                metaModelName = x1.name;

            // check if this object is searchable
            var x2 = mi.GetCustomAttribute<AasCore.Aas3_0_RC02.Attributes.TextSearchable>();
            if (x2 != null)
            {
                // what to check?
                string foundText = "" + memberValue?.ToString();

                // quite late: investigate, if we accepted findings from the 
                // type of business element
                var isColl = (businessObject is AssetAdministrationShell
                        || businessObject is Submodel
                        || businessObject is SubmodelElementCollection);

                var isProp = (businessObject is Property);

                var isMLP = (businessObject is MultiLanguageProperty);


                if (!options.SearchCollection && isColl)
                    return;

                if (!options.SearchProperty && isProp)
                    return;

                if (!options.SearchMultiLang && isMLP)
                    return;

                if (!options.SearchOther && !(isColl || isProp || isMLP))
                    return;

                // find options
                var found = true;
                Match foundMatch = null;
                if (options.FindText != null)
                {
                    if (options.IsRegex && options.CompiledRegex != null)
                    {
                        foundMatch = options.CompiledRegex.Match(foundText);
                        found = foundMatch.Success;
                    }
                    else
                    if (options.IsWholeWord)
                    {
                        found = string.Equals(foundText, options.FindText,
                            options.IsIgnoreCase ? StringComparison.CurrentCultureIgnoreCase : 0);
                    }
                    else
                    {
                        found = foundText.IndexOf(options.FindText,
                            options.IsIgnoreCase ? StringComparison.CurrentCultureIgnoreCase : 0) >= 0;
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
        /// <param name="progress">Progress is reported for any check of field/ property and for any addition</param>
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
            if (depth > options.MaxDepth)
                return;

            // try to get element name of an AAS entity
            string elName = null;
            if (obj is IReferable)
            {
                elName = (obj as IReferable).GetSelfDescription()?.AasElementName;
                businessObject = obj;
            }

            // enrich qualified name, accordingly
            var qualifiedName = qualifiedNameHead;
            if (elName != null)
                qualifiedName = qualifiedName + (qualifiedName.Length > 0 ? "." : "") + elName;

            // do NOT dive into objects, which are not in the right assembly
            if (options.AllowedAssemblies == null || !options.AllowedAssemblies.Contains(objType.Assembly))
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
                var x3 = fi.GetCustomAttribute<AasCore.Aas3_0_RC02.Attributes.SkipForReflection>();
                if (x3 != null)
                    continue;

                var x4 = fi.GetCustomAttribute<AasCore.Aas3_0_RC02.Attributes.SkipForSearch>();
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
                var x3 = pi.GetCustomAttribute<AasCore.Aas3_0_RC02.Attributes.SkipForReflection>();
                if (x3 != null)
                    continue;

                var x4 = pi.GetCustomAttribute<AasCore.Aas3_0_RC02.Attributes.SkipForSearch>();
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
            if (options.IsRegex)
            {
                memstr = Regex.Replace(memstr, options.FindText, replaceText, options.CompiledRegOpt);
                return memstr;
            }
            else
            {
                // plain text replacement
                memstr = memstr.Replace(options.FindText, replaceText);
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

    }
}
