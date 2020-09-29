using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AasxPredefinedConcepts
{
    public class AasxDefinitionBase
    {
        //
        // Inner classes
        //

        public class LibraryEntry
        {
            public string name = "";
            public string contents = "";

            public LibraryEntry() { }
            public LibraryEntry(string name, string contents)
            {
                this.name = name;
                this.contents = contents;
            }
        }

        //
        // Fields
        //

        protected Dictionary<string, LibraryEntry> theLibrary = new Dictionary<string, LibraryEntry>();

        //
        // Constructors
        //

        public AasxDefinitionBase() { }

        public AasxDefinitionBase(Assembly assembly, string resourceName)
        {
            this.theLibrary = BuildLibrary(assembly, resourceName);
        }

        //
        // Rest
        //

        public void ReadLibrary(Assembly assembly, string resourceName)
        {
            this.theLibrary = BuildLibrary(assembly, resourceName);
        }

        protected Dictionary<string, LibraryEntry> BuildLibrary(Assembly assembly, string resourceName)
        {
            // empty result
            var res = new Dictionary<string, LibraryEntry>();

            // access resource
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return res;

            // read text
            TextReader tr = new StreamReader(stream);
            var jsonStr = tr.ReadToEnd();
            stream.Close();

            // Parse into root
            var root = JObject.Parse(jsonStr);

            // decompose
            foreach (var child in root.Children())
            {
                // just look for 1. level properties
                var prop = child as JProperty;
                if (prop == null)
                    continue;

                // ok
                var name = prop.Name;
                var contents = prop.Value.ToString();

                // populate
                res.Add(name, new LibraryEntry(name, contents));
            }

            return res;
        }

        public LibraryEntry RetrieveEntry(string name)
        {
            // simple access
            if (theLibrary == null || name == null || !theLibrary.ContainsKey(name))
                return null;

            // return
            return theLibrary[name];
        }

        public T RetrieveReferable<T>(string name) where T : AdminShell.Referable
        {
            // entry
            var entry = this.RetrieveEntry(name);
            if (entry == null || entry.contents == null)
                return null;

            // try de-serialize
            try
            {
                var r = JsonConvert.DeserializeObject<T>(entry.contents);
                return r;
            }
            catch
            {
                return null;
            }
        }

        public static AdminShell.ConceptDescription CreateSparseConceptDescription(
            string lang,
            string idType,
            string idShort,
            string id,
            string definitionHereString,
            AdminShell.Reference isCaseOf = null)
        {
            // access
            if (idShort == null || idType == null || id == null)
                return null;

            // create CD
            var cd = AdminShell.ConceptDescription.CreateNew(idShort, idType, id);
            var dsiec = cd.CreateDataSpecWithContentIec61360();
            dsiec.preferredName = new AdminShellV20.LangStringSetIEC61360(lang, "" + idShort);
            dsiec.definition = new AdminShellV20.LangStringSetIEC61360(lang,
                "" + AdminShellUtil.CleanHereStringWithNewlines(nl: " ", here: definitionHereString));

            // options
            if (isCaseOf != null)
                cd.IsCaseOf = new List<AdminShell.Reference>(new[] { isCaseOf });

            // ok
            return cd;
        }

        /// <summary>
        /// This attribute indicates, that the attributed member shall be looked up by its name
        /// in the library.
        /// </summary>
        [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
        public class RetrieveReferableForField : System.Attribute
        {
        }

        public void RetrieveEntriesByReflection(Type typeToReflect = null,
            bool useAttributes = false, bool useFieldNames = false)
        {
            // access
            if (this.theLibrary == null || typeToReflect == null)
                return;

            // reflection
            foreach (var fi in typeToReflect.GetFields())
            {
                // access
                if (fi == null)
                    continue;

                // libName
                var libName = "" + fi.Name;

                // test
                var ok = false;
                var isSM = fi.FieldType == typeof(AdminShell.Submodel);
                var isCD = fi.FieldType == typeof(AdminShell.ConceptDescription);

                if (useAttributes && fi.GetCustomAttribute(typeof(RetrieveReferableForField)) != null)
                    ok = true;

                if (useFieldNames && isSM && libName.StartsWith("SM_"))
                    ok = true;

                if (useFieldNames && isCD && libName.StartsWith("CD_"))
                    ok = true;

                if (!ok)
                    continue;

                // access library
                if (isSM)
                { 
                    var sm = this.RetrieveReferable<AdminShell.Submodel>(libName);
                    fi.SetValue(this, sm);
                }
                if (isCD)
                {
                    var cd = this.RetrieveReferable<AdminShell.ConceptDescription>(libName);
                    fi.SetValue(this, cd);
                }
            }
        }
    }
}
