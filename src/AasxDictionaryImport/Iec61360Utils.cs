/*
Copyright (c) 2020 SICK AG <info@sick.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AdminShellNS;

namespace AasxDictionaryImport
{
    /// <summary>
    /// Utility functions for the conversion of IEC 61360 data objects into AAS elements.
    /// </summary>
    public static class Iec61360Utils
    {
        /// <summary>
        /// Generates an idShort from the given string.  This means that illegal characters are removed and the string
        /// is converted to UpperCamelCase (using white space as word boundaries).
        /// </summary>
        /// <param name="s">The input string</param>
        /// <returns>A valid idShort based on the given input string</returns>
        public static string CreateIdShort(string s)
        {
            return new string(FixIdShort(s).ToArray());
        }

        /// <summary>
        /// Creates a new submodel with the given IEC 61360 data within the given AAS environment and admin shell,
        /// generating a new IRI based on the current settings and setting the kind to Instance.
        /// </summary>
        /// <param name="env">The AAS environment to add the submodel to</param>
        /// <param name="adminShell">The admin shell to add the submodel to</param>
        /// <param name="data">The IEC 61360 data to create the submodel from</param>
        /// <returns>A new submodel with the given data</returns>
        public static AdminShellV20.Submodel CreateSubmodel(
            AdminShellV20.AdministrationShellEnv env,
            AdminShellV20.AdministrationShell adminShell, Iec61360Data data)
        {
            // We need this to ensure that we don't use the same AAS ID twice when importing multiple submodels (as
            // GenerateIdAccordingTemplate uses the timestamp as part of the ID).
            Thread.Sleep(1000);
            var submodel = new AdminShellV20.Submodel()
            {
                identification = new AdminShellV20.Identification(
                    AdminShellV20.Identification.IRI,
                    AasxPackageExplorer.Options.Curr.GenerateIdAccordingTemplate(
                        AasxPackageExplorer.Options.Curr.TemplateIdSubmodelInstance)),
                idShort = data.IdShort,
                kind = AdminShellV20.ModelingKind.CreateAsInstance(),
            };

            AddDescriptions(submodel, data);
            AddDataSpecification(env, submodel, data);

            adminShell.AddSubmodelRef(submodel.GetReference() as AdminShellV20.SubmodelRef);
            env.Submodels.Add(submodel);

            return submodel;
        }

        /// <summary>
        /// Creates a new submodel element collection with the given IEC 61360 data, setting the kind to Instance.
        /// </summary>
        /// <param name="env">The AAS environment to add the collection to</param>
        /// <param name="data">The IEC 61360 data to create the collection from</param>
        /// <returns>A new submodel element collection with the given data</returns>
        public static AdminShellV20.SubmodelElementCollection CreateCollection(
            AdminShellV20.AdministrationShellEnv env, Iec61360Data data)
        {
            var collection = new AdminShellV20.SubmodelElementCollection()
            {
                idShort = data.IdShort,
                kind = AdminShellV20.ModelingKind.CreateAsInstance(),
            };
            InitSubmodelElement(env, collection, data);
            return collection;
        }

        /// <summary>
        /// Creates a new property with the given IEC 61360 data and value type, setting the kind to Instance.
        /// </summary>
        /// <param name="env">The AAS environment to add the property to</param>
        /// <param name="data">The IEC 61360 data to create the property from</param>
        /// <param name="valueType">The value type of the property</param>
        /// <returns>A new property with the given data</returns>
        public static AdminShellV20.Property CreateProperty(
            AdminShellV20.AdministrationShellEnv env, Iec61360Data data, string valueType)
        {
            var property = new AdminShellV20.Property()
            {
                idShort = data.IdShort,
                kind = AdminShellV20.ModelingKind.CreateAsInstance(),
                valueType = valueType,
            };
            InitSubmodelElement(env, property, data);
            return property;
        }

        private static void InitSubmodelElement(AdminShellV20.AdministrationShellEnv env,
            AdminShellV20.SubmodelElement submodelElement, Iec61360Data data)
        {
            AddDescriptions(submodelElement, data);
            AddDataSpecification(env, submodelElement, data);
        }

        private static void AddDescriptions(AdminShellV20.Referable r, Iec61360Data data)
        {
            foreach (var lang in data.PreferredName.AvailableLanguages)
                r.AddDescription(lang, data.PreferredName.Get(lang));
        }

        private static void AddDataSpecification(AdminShellV20.AdministrationShellEnv env,
            AdminShellV20.Submodel submodel, Iec61360Data data)
        {
            var cd = CreateConceptDescription(env, data);

            // cd should already contain IEC61360Spec; add data spec
            // TODO (Robin, 2020-09-03): MIHO is not sure, if the data spec reference is correct; please check
            var eds = cd.IEC61360DataSpec;
            if (eds != null)
            {
                eds.dataSpecification = new AdminShellV20.DataSpecificationRef(cd.GetReference());
            }

            submodel.semanticId = new AdminShellV20.SemanticId(cd.GetReference());
        }

        private static void AddDataSpecification(AdminShellV20.AdministrationShellEnv env,
            AdminShellV20.SubmodelElement submodelElement, Iec61360Data data)
        {
            var cd = CreateConceptDescription(env, data);

            // cd should already contain IEC61360Spec; add data spec
            // TODO (Robin, 2020-09-03): MIHO is not sure, if the data spec reference is correct; please check
            var eds = cd.IEC61360DataSpec;
            if (eds != null)
            {
                eds.dataSpecification = new AdminShellV20.DataSpecificationRef(cd.GetReference());
            }

            submodelElement.semanticId = new AdminShellV20.SemanticId(cd.GetReference());
        }

        private static AdminShellV20.ConceptDescription CreateConceptDescription(
            AdminShellV20.AdministrationShellEnv env, Iec61360Data data)
        {
            var cd = AdminShellV20.ConceptDescription.CreateNew(
                data.IdShort, AdminShellV20.Identification.IRDI, data.Irdi);

            // TODO (Robin, 2020-09-03): check this code
            cd.IEC61360Content = data.ToDataSpecification();
            // dead-csharp off
            //cd.embeddedDataSpecification = new AdminShellV20.EmbeddedDataSpecification()
            //{
            //    dataSpecificationContent = new AdminShellV20.DataSpecificationContent()
            //    {
            //        dataSpecificationIEC61360 = data.ToDataSpecification(),
            //    },
            //};
            // dead-csharp on

            cd.AddIsCaseOf(AdminShellV20.Reference.CreateIrdiReference(data.Irdi));
            env.ConceptDescriptions.Add(cd);
            return cd;
        }

        private static IEnumerable<char> FixIdShort(string s)
        {
            bool start = true;
            bool newWord = true;

            foreach (var c in s)
            {
                if (start)
                    if (!Char.IsLetter(c))
                        continue;

                start = false;

                if (Char.IsWhiteSpace(c))
                {
                    newWord = true;
                }
                else if (Char.IsLetter(c) || Char.IsDigit(c) || c == '_')
                {
                    yield return newWord ? Char.ToUpper(c) : c;
                    newWord = false;
                }
            }
        }
    }

    /// <summary>
    /// String that is available in multiple languages.
    /// </summary>
    public sealed class MultiString
    {
        private const string DefaultLanguageCode = "en";
        private readonly Dictionary<string, string> _data;

        /// <summary>
        /// All languages that are supported by the data model.
        /// </summary>
        public IEnumerable<string> Languages => _data.Keys;

        /// <summary>
        /// All languages that have a non-empty value for this attribute.
        /// </summary>
        public IEnumerable<string> AvailableLanguages
            => Languages.Where(lang => _data[lang].Length > 0);

        /// <summary>
        /// All values for this attribute.
        /// </summary>
        public IEnumerable<string> Values => _data.Values;

        /// <summary>
        /// The default language for this attribute – English if available, otherwise the first available language.
        /// </summary>
        public string DefaultLanguage
        {
            get
            {
                if (AvailableLanguages.Contains(DefaultLanguageCode))
                    return DefaultLanguageCode;
                return AvailableLanguages.DefaultIfEmpty("").First();
            }
        }

        /// <summary>
        /// Creates a new MultiString object without any values.
        /// </summary>
        public MultiString() : this(new Dictionary<string, string>())
        {
        }

        /// <summary>
        /// Creates a new MultiString object with the given values.
        /// </summary>
        /// <param name="data">The data to store in the MultiString object, as a mapping from languages to
        /// values</param>
        public MultiString(Dictionary<string, string> data)
        {
            _data = data;
        }

        /// <summary>
        /// Adds the given value for the given language to this multi string.
        /// </summary>
        /// <param name="lang">The language code for the value</param>
        /// <param name="value">The value to add to the multi string</param>
        public void Add(string lang, string value)
        {
            if (!_data.ContainsKey(lang))
                _data.Add(lang, value);
        }

        /// <summary>
        /// Returns the value for the given language, or an empty string if no value is set.
        /// </summary>
        /// <param name="lang">The language to check</param>
        /// <returns>The value for the given language, or an empty string</returns>
        public string Get(string lang)
        {
            return _data.TryGetValue(lang, out string? value) ? value : string.Empty;
        }

        /// <summary>
        /// Returns the value of this attribute in the default language – English if available, otherwise the first
        /// available language.  If this multi string does not contain any values, this method returns an empty string.
        /// </summary>
        /// <returns>The value of this attribute in the default language</returns>
        public string GetDefault()
        {
            return Get(DefaultLanguage);
        }

        /// <summary>
        /// Converts this multi string to a LangStringSet used by the AdminShell data model.  Only the non-empty values
        /// are added to the set.
        /// </summary>
        /// <returns>A LangStringSet with the values form this multi string</returns>
        public AdminShellV20.LangStringSetIEC61360 ToLangStringSet()
        {
            var set = new AdminShellV20.LangStringSetIEC61360();
            foreach (var lang in Languages)
            {
                var value = Get(lang);
                if (value.Length > 0)
                    set.Add(new AdminShellV20.LangStr(lang, value));
            }
            return set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return GetDefault();
        }
    }

    /// <summary>
    /// The data for an element according to the IEC 61360 standard.  This data can be used to generate AAS elements,
    /// especially submodels, properties and collections.  The idShort for the generated AAS element is derived from
    /// the short name or the preferred name.
    /// <para>
    /// It is not necessary to set all fields -- some are only applicable for some element types.  But every data
    /// object must have a unique IRDI.
    /// </para>
    /// </summary>
    public sealed class Iec61360Data
    {
        private string? _idShort = null;

        /// <summary>
        /// The IRDI of this element.
        /// </summary>
        public string Irdi { get; set; }

        /// <summary>
        /// The preferred name for this element in multiple languages.
        /// </summary>
        public MultiString PreferredName { get; set; } = new MultiString();

        /// <summary>
        /// The short name for this element in multiple languages.
        /// </summary>
        public MultiString ShortName { get; set; } = new MultiString();

        /// <summary>
        /// The definition for this element in multiple languages.
        /// </summary>
        public MultiString Definition { get; set; } = new MultiString();

        /// <summary>
        /// The definition source for this element.
        /// </summary>
        public string DefinitionSource { get; set; } = string.Empty;

        /// <summary>
        /// The symbol for this element (properties only).
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// The unit for this element (properties only).
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// The IRDI of the unit element for this element (properties only).
        /// </summary>
        public string UnitIrdi { get; set; } = string.Empty;

        /// <summary>
        /// The data type for this element (properties only).
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// The data format for this element (properties only).
        /// </summary>
        public string DataFormat { get; set; } = string.Empty;

        /// <summary>
        /// The AAS shortId for this element.  The short ID is generated from the preferred name.
        /// </summary>
        public string IdShort
        {
            get
            {
                _idShort ??= GenerateIdShort();
                return _idShort;
            }
        }

        /// <summary>
        /// Creates a new Iec61360Data object with the given IRDI.
        /// </summary>
        /// <param name="irdi">The IRDI for this object</param>
        public Iec61360Data(string irdi)
        {
            Irdi = irdi;
        }

        /// <summary>
        /// Converts this data to a DataSpecification object used by the AAS data model.  Empty fields are ignored.
        /// </summary>
        /// <returns>The AAS DataSpecification with the data stored in this element</returns>
        public AdminShellV20.DataSpecificationIEC61360 ToDataSpecification()
        {
            var ds = new AdminShellV20.DataSpecificationIEC61360()
            {
                definition = Definition.ToLangStringSet(),
                preferredName = PreferredName.ToLangStringSet(),
                shortName = ShortName.ToLangStringSet(),
            };

            if (DefinitionSource.Length > 0)
                ds.sourceOfDefinition = DefinitionSource;
            if (Symbol.Length > 0)
                ds.symbol = Symbol;
            if (Unit.Length > 0)
                ds.unit = Unit;
            if (UnitIrdi.Length > 0)
                ds.unitId = AdminShellV20.UnitId.CreateNew(
                    AdminShellV20.Key.GlobalReference, false,
                    AdminShellV20.Identification.IRDI, UnitIrdi);
            if (DataType.Length > 0)
                ds.dataType = DataType;
            if (DataFormat.Length > 0)
                ds.valueFormat = DataFormat;

            return ds;
        }

        private string GenerateIdShort()
        {
            var shortName = ShortName.GetDefault();
            var name = shortName.Length > 0 ? shortName : PreferredName.GetDefault();
            return Iec61360Utils.CreateIdShort(name);
        }
    }
}
