﻿/*
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
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AasxPredefinedConcepts
{
    public class AasxDefinitionBase
    {
        //
        // Constants & members
        //

        public const string V20Tag = "AAS2.0";
        public string ReadVersion = "";

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

        public class Library : Dictionary<string, LibraryEntry>
        {
        }

        //
        // Fields
        //

        protected Library _library = new Library();

        protected List<Aas.IReferable> theReflectedReferables = new List<Aas.IReferable>();

        public string DomainInfo = "";

        //
        // Constructors
        //

        public AasxDefinitionBase() { }

        public AasxDefinitionBase(Assembly assembly, string resourceName)
        {
            this._library = BuildLibrary(assembly, resourceName);
        }

        //
        // Rest
        //

        public void ReadLibrary(Assembly assembly, string resourceName)
        {
            this._library = BuildLibrary(assembly, resourceName);
        }

        protected Library BuildLibrary(Assembly assembly, string resourceName)
        {
            // empty result
            var res = new Library();

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

                // some special cases
                if (prop.Name == "Version")
                {
                    // note this once for the whole class
                    ReadVersion = prop.Value.ToString();
                    continue;
                }

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
            if (_library == null || name == null || !_library.ContainsKey(name))
                return null;

            // return
            return _library[name];
        }

        public T RetrieveReferable<T>(string name) where T : class, Aas.IReferable
        {
            // entry
            var entry = this.RetrieveEntry(name);
            if (entry == null || entry.contents == null)
                return default(T);

            // try de-serialize
            T res = null;
            try
            {
                // do some on-the-fly conversion?
#if !DoNotUseAasxCompatibilityModels
                if (ReadVersion == V20Tag)
                {
                    if (typeof(T) == typeof(Aas.Submodel))
                    {
                        var old = JsonConvert.DeserializeObject
                            <AasxCompatibilityModels.AdminShellV20.Submodel>(entry.contents);
                        if (old != null)
                            res = new Aas.Submodel("").ConvertFromV20(old) as T;
                    }

                    if (typeof(T) == typeof(Aas.ConceptDescription))
                    {
                        var old = JsonConvert.DeserializeObject
                            <AasxCompatibilityModels.AdminShellV20.ConceptDescription>(entry.contents);
                        if (old != null)
                            res = new Aas.ConceptDescription("").ConvertFromV20(old) as T;
                    }
                }
#endif
                // TODO (MIHO, 2022-12-31): for V3.0, another method of deserialization is required!!

                res ??= JsonConvert.DeserializeObject<T>(entry.contents);
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                return default(T);
            }

            // OK
            return res;
        }

        public static Aas.ConceptDescription CreateSparseConceptDescription(
            string lang,
            string idType,
            string idShort,
            string id,
            string definitionHereString,
            Aas.Reference isCaseOf = null)
        {
            // access
            if (idShort == null || idType == null || id == null)
                return null;

            // create CD
            var cd = new Aas.ConceptDescription(id, idShort: idShort);
            var dsiec = ExtendEmbeddedDataSpecification.CreateIec61360WithContent();
            var dsc = dsiec.DataSpecificationContent as Aas.DataSpecificationIec61360;
            dsc.PreferredName = new List<Aas.LangString>();
            dsc.PreferredName.Add(new Aas.LangString(lang, "" + idShort));
            dsc.Definition = new List<Aas.LangString>();
            dsc.Definition.Add(new Aas.LangString(lang, "" + AdminShellUtil.CleanHereStringWithNewlines(nl: " ", here: definitionHereString)));

            // options
            if (isCaseOf != null)
                cd.IsCaseOf = new List<Aas.Reference>(new[] { isCaseOf });

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

        public virtual Aas.IReferable[] GetAllReferables()
        {
            return this.theReflectedReferables?.ToArray();
        }

        public void RetrieveEntriesFromLibraryByReflection(Type typeToReflect = null,
            bool useAttributes = false, bool useFieldNames = false)
        {
            // access
            if (this._library == null || typeToReflect == null)
                return;

            // remember found Referables
            this.theReflectedReferables = new List<Aas.IReferable>();

            // reflection
            foreach (var fi in typeToReflect.GetFields())
            {
                // libName
                var libName = "" + fi.Name;

                // test
                var ok = false;
                var isSM = fi.FieldType == typeof(Aas.Submodel);
                var isCD = fi.FieldType == typeof(Aas.ConceptDescription);

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
                    var sm = this.RetrieveReferable<Aas.Submodel>(libName);
                    fi.SetValue(this, sm);
                    this.theReflectedReferables.Add(sm);
                }
                if (isCD)
                {
                    var cd = this.RetrieveReferable<Aas.ConceptDescription>(libName);
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
                var isSM = fi.FieldType == typeof(Aas.Submodel);
                var isCD = fi.FieldType == typeof(Aas.ConceptDescription);

                if (useAttributes && fi.GetCustomAttribute(typeof(RetrieveReferableForField)) != null)
                    ok = true;

                if (useFieldNames && isSM && fiName.StartsWith("SM_"))
                    ok = true;

                if (useFieldNames && isCD && fiName.StartsWith("CD_"))
                    ok = true;

                if (!ok)
                    continue;

                // add
                var rf = fi.GetValue(this) as Aas.IReferable;
                if (rf != null)
                    this.theReflectedReferables.Add(rf);
            }
        }
    }
}
