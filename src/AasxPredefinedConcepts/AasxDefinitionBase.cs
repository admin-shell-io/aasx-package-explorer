/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
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

        protected List<IReferable> theReflectedReferables = new List<IReferable>();

        public string DomainInfo = "";

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

        public T RetrieveReferable<T>(string name) where T : IReferable
        {
            // entry
            var entry = this.RetrieveEntry(name);
            if (entry == null || entry.contents == null)
                return default(T);

            // try de-serialize
            try
            {
                var r = JsonConvert.DeserializeObject<T>(entry.contents);
                return r;
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                return default(T);
            }
        }

        public static ConceptDescription CreateSparseConceptDescription(
            string lang,
            string idType,
            string idShort,
            string id,
            string definitionHereString,
            Reference isCaseOf = null)
        {
            // access
            if (idShort == null || idType == null || id == null)
                return null;

            // create CD
            var cd = new ConceptDescription(id, idShort:idShort);
            var dsiec = ExtendEmbeddedDataSpecification.CreateIec61360WithContent();
            var dsc = dsiec.DataSpecificationContent as DataSpecificationIec61360;
            dsc.PreferredName = new List<LangString>();
            dsc.PreferredName.Add(new LangString(lang, "" + idShort));
            dsc.Definition = new List<LangString>();
            dsc.Definition.Add(new LangString(lang, "" + AdminShellUtil.CleanHereStringWithNewlines(nl: " ", here: definitionHereString)));

            // options
            if (isCaseOf != null)
                cd.IsCaseOf = new List<Reference>(new[] { isCaseOf });

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

        public virtual IReferable[] GetAllReferables()
        {
            return this.theReflectedReferables?.ToArray();
        }

        public void RetrieveEntriesFromLibraryByReflection(Type typeToReflect = null,
            bool useAttributes = false, bool useFieldNames = false)
        {
            // access
            if (this.theLibrary == null || typeToReflect == null)
                return;

            // remember found Referables
            this.theReflectedReferables = new List<IReferable>();

            // reflection
            foreach (var fi in typeToReflect.GetFields())
            {
                // libName
                var libName = "" + fi.Name;

                // test
                var ok = false;
                var isSM = fi.FieldType == typeof(Submodel);
                var isCD = fi.FieldType == typeof(ConceptDescription);

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
                    var sm = this.RetrieveReferable<Submodel>(libName);
                    fi.SetValue(this, sm);
                    this.theReflectedReferables.Add(sm);
                }
                if (isCD)
                {
                    var cd = this.RetrieveReferable<ConceptDescription>(libName);
                    fi.SetValue(this, cd);
                    this.theReflectedReferables.Add(cd);
                }
            }
        }

        public void AddEntriesByReflection(Type typeToReflect = null,
            bool useAttributes = false, bool useFieldNames = false)
        {
            // access
            if (typeToReflect == null)
                return;

            // reflection
            foreach (var fi in typeToReflect.GetFields())
            {
                // libName
                var fiName = "" + fi.Name;

                // test
                var ok = false;
                var isSM = fi.FieldType == typeof(Submodel);
                var isCD = fi.FieldType == typeof(ConceptDescription);

                if (useAttributes && fi.GetCustomAttribute(typeof(RetrieveReferableForField)) != null)
                    ok = true;

                if (useFieldNames && isSM && fiName.StartsWith("SM_"))
                    ok = true;

                if (useFieldNames && isCD && fiName.StartsWith("CD_"))
                    ok = true;

                if (!ok)
                    continue;

                // add
                var rf = fi.GetValue(this) as IReferable;
                if (rf != null)
                    this.theReflectedReferables.Add(rf);
            }
        }
    }
}
