/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AdminShellNS
{
    /// <summary>
    /// This empty class derives always from the current version of the Administration Shell class hierarchy.
    /// </summary>
    public class AdminShell : AdminShellV30 { }

    /// <summary>
    /// Version of Details of Administration Shell Part 1 V3.0 RC 02 published Apre 2022
    /// </summary>
    public partial class AdminShellV30
    {
        //
        // Version
        //

        /// <summary>
        /// Major version of the meta-model
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public static string MetaModelVersionCoarse = "AAS3.0";

        /// <summary>
        /// Minor version (extension) of the meta-model.
        /// Should be added to <c>MetaModelVersionCoarse</c>
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public static string MetaModelVersionFine = "RC02";

        //
        // Interfaces
        //

        /// <summary>
        /// Extends understanding of Referable to further elements, which can be related to
        /// </summary>
        public interface IAasElement
        {
            AasElementSelfDescription GetSelfDescription();
            string GetElementName();
        }

        public interface IEnumerateChildren
        {
            IEnumerable<SubmodelElementWrapper> EnumerateChildren();
            EnumerationPlacmentBase GetChildrenPlacement(SubmodelElement child);
            object AddChild(SubmodelElementWrapper smw, EnumerationPlacmentBase placement = null);
        }

        public interface IValidateEntity
        {
            void Validate(AasValidationRecordList results);
        }

        /// <summary>
        /// Marks an object, preferaby a payload item, which might be featured by the diary collection
        /// </summary>
        public interface IAasDiaryEntry
        {
        }

        /// <summary>
        /// Marks every entity which features DiaryData, for derivation of AAS event flow
        /// </summary>
        public interface IDiaryData
        {
            DiaryDataDef DiaryData { get; }
        }


        public interface IRecurseOnReferables
        {
            void RecurseOnReferables(
                object state, Func<object, ListOfReferable, Referable, bool> lambda,
                bool includeThis = false);
        }

        /// <summary>
        /// Use by <c>FindAllReference</c> to provide a enumeration of referenced with location
        /// info, where they are contained
        /// </summary>
        public class LocatedReference
        {
            public Identifiable Identifiable;
            public ModelReference Reference;

            public LocatedReference() { }
            public LocatedReference(Identifiable identifiable, ModelReference reference)
            {
                Identifiable = identifiable;
                Reference = reference;
            }
        }

        public interface IFindAllReferences
        {
            IEnumerable<LocatedReference> FindAllReferences();
        }

        public interface IGetSemanticId
        {
            SemanticId GetSemanticId();
        }

        /// <summary>
        /// This interface marks an entity, which can provide a ModelReference of itself.
        /// Typically, these are Referables or Identifiables.
        /// </summary>
        public interface IGetModelReference
        {
            ModelReference GetModelReference(bool includeParents = true);
        }

        /// <summary>
        /// This interface marks an entity, which can provide a GlobaleReference of itself.
        /// These entities are much more rare.
        /// </summary>
        public interface IGetGlobalReference
        {
            GlobalReference GetGlobalReference();
        }

        public interface IGetQualifiers
        {
            QualifierCollection GetQualifiers();
        }

        public interface IManageSubmodelElements
        {
            void Add(SubmodelElement sme);
            void Insert(int index, SubmodelElement sme);
            void Remove(SubmodelElement sme);
        }

        //
        // Attributes
        //

        /// <summary>
        /// This attribute indicates, that it should e.g. serialized in JSON.
        /// </summary>
        [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
        public class CountForHash : System.Attribute
        {
        }

        /// <summary>
        /// This attribute indicates, that evaluation shall not count following field or not dive into references.
        /// </summary>
        [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
        public class SkipForHash : System.Attribute
        {
        }

        /// <summary>
        /// This attribute indicates, that the field / property is searchable
        /// </summary>
        [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
        public class MetaModelName : System.Attribute
        {
            public string name;
            public MetaModelName(string name)
            {
                this.name = name;
            }
        }

        /// <summary>
        /// This attribute indicates, that the field / property shall be skipped for reflection
        /// in order to avoid cycles
        /// </summary>
        [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
        public class SkipForReflection : System.Attribute
        {
        }

        /// <summary>
        /// This attribute indicates, that the field / property shall be skipped for searching, because it is not
        /// directly displayed in Package Explorer
        /// </summary>
        [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
        public class SkipForSearch : System.Attribute
        {
        }

        /// <summary>
        /// This attribute indicates, that the field / property is searchable
        /// </summary>
        [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
        public class TextSearchable : System.Attribute
        {
        }

        //
        // Serialization helpers
        //

        public class JsonModelTypeWrapper
        {
            public string name = "";

            public JsonModelTypeWrapper(string name = "") { this.name = name; }
        }

        //
        // Administration
        //

        public class Administration
        {

            // members

            [MetaModelName("Administration.version")]
            [TextSearchable]
            [CountForHash]
            public string version = "";

            [MetaModelName("Administration.revision")]
            [TextSearchable]
            [CountForHash]
            public string revision = "";

            // constructors

            public Administration() { }

            public Administration(Administration src)
            {
                this.version = src.version;
                this.revision = src.revision;
            }

#if !DoNotUseAasxCompatibilityModels
            public Administration(AasxCompatibilityModels.AdminShellV10.Administration src)
            {
                this.version = src.version;
                this.revision = src.revision;
            }

            public Administration(AasxCompatibilityModels.AdminShellV20.Administration src)
            {
                this.version = src.version;
                this.revision = src.revision;
            }
#endif

            public Administration(string version, string revision)
            {
                this.version = version;
                this.revision = revision;
            }

            public override string ToString()
            {
                return $"R={this.version}, V={this.revision}";
            }
        }

        public class AasElementSelfDescription
        {
            public string ElementName = "";
            public string ElementAbbreviation = "";
            public SubmodelElementWrapper.AdequateElementEnum ElementEnum =
                SubmodelElementWrapper.AdequateElementEnum.Unknown;

            public AasElementSelfDescription() { }

            public AasElementSelfDescription(
                string ElementName, string ElementAbbreviation,
                SubmodelElementWrapper.AdequateElementEnum elementEnum
                    = SubmodelElementWrapper.AdequateElementEnum.Unknown)
            {
                this.ElementName = ElementName;
                this.ElementAbbreviation = ElementAbbreviation;
                this.ElementEnum = elementEnum;
            }
        }


        [XmlType(TypeName = "derivedFrom")]
        public class AssetAdministrationShellRef : ModelReference
        {
            // constructors

            public AssetAdministrationShellRef() : base() { }

            public AssetAdministrationShellRef(Key k) : base(k) { }

            public AssetAdministrationShellRef(ModelReference src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
            public AssetAdministrationShellRef(AasxCompatibilityModels.AdminShellV10.Reference src) : base(src) { }

            public AssetAdministrationShellRef(AasxCompatibilityModels.AdminShellV20.Reference src) : base(src) { }
#endif

            // further methods

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("AssetAdministrationShellRef", "AasRef");
            }
        }

        [XmlType(TypeName = "assetRef")]
        public class AssetRef : GlobalReference
        {
            // constructors

            public AssetRef() : base() { }
            public AssetRef(AssetRef src) : base(src) { }
            public AssetRef(GlobalReference r) : base(r) { }
            public AssetRef(Identifier id) : base(id) { }

#if !DoNotUseAasxCompatibilityModels
            public AssetRef(AasxCompatibilityModels.AdminShellV10.AssetRef src) : base(src) { }
            public AssetRef(AasxCompatibilityModels.AdminShellV20.AssetRef src) : base(src) { }
#endif


            // further methods

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("AssetRef", "AssetRef");
            }
        }

        [XmlType(TypeName = "submodelRef")]
        public class SubmodelRef : ModelReference
        {
            // constructors

            public SubmodelRef() : base() { }

            public SubmodelRef(SubmodelRef src) : base(src) { }

            public SubmodelRef(ModelReference src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
            public SubmodelRef(AasxCompatibilityModels.AdminShellV10.SubmodelRef src) : base(src) { }

            public SubmodelRef(AasxCompatibilityModels.AdminShellV20.SubmodelRef src) : base(src) { }
#endif

            public new static SubmodelRef CreateNew(string type, string value)
            {
                var r = new SubmodelRef();
                r.Keys.Add(Key.CreateNew(type, value));
                return r;
            }

            public static SubmodelRef CreateNew(ModelReference src)
            {
                if (src == null || src.Keys == null)
                    return null;
                var r = new SubmodelRef();
                r.Keys.AddRange(src.Keys);
                return r;
            }

            // further methods

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("SubmodelRef", "SMRef");
            }
        }

        [XmlType(TypeName = "conceptDescriptionRef")]
        public class ConceptDescriptionRef : ModelReference
        {
            // constructors

            public ConceptDescriptionRef() : base() { }

            public ConceptDescriptionRef(ConceptDescriptionRef src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
            public ConceptDescriptionRef(
                AasxCompatibilityModels.AdminShellV10.ConceptDescriptionRef src) : base(src) { }

            public ConceptDescriptionRef(
                AasxCompatibilityModels.AdminShellV20.ConceptDescriptionRef src) : base(src) { }
#endif

            // further methods

            public new static ConceptDescriptionRef CreateNew(string type, string value)
            {
                var r = new ConceptDescriptionRef();
                r.Keys.Add(Key.CreateNew(type, value));
                return r;
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("ConceptDescriptionRef", "CDRef");
            }

        }

        [XmlType(TypeName = "dataSpecificationRef")]
        public class DataSpecificationRef : GlobalReference
        {
            // constructors

            public DataSpecificationRef() : base() { }

            public DataSpecificationRef(DataSpecificationRef src) : base(src) { }
            public DataSpecificationRef(GlobalReference src) : base(src) { }
            public DataSpecificationRef(Identifier src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
            public DataSpecificationRef(AasxCompatibilityModels.AdminShellV10.DataSpecificationRef src) : base(src) { }

            public DataSpecificationRef(AasxCompatibilityModels.AdminShellV10.Reference src) : base(src) { }

            public DataSpecificationRef(AasxCompatibilityModels.AdminShellV20.DataSpecificationRef src) : base(src) { }
#endif

            // further methods

            public static DataSpecificationRef CreateNew(GlobalReference src)
            {
                return new DataSpecificationRef(src); 
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("DataSpecificationRef", "DSRef");
            }

        }

        [XmlType(TypeName = "conceptDescriptions")]
        public class ConceptDescriptionRefs
        {
            [XmlElement(ElementName = "conceptDescriptionRef")]
            public List<ConceptDescriptionRef> conceptDescriptions = new List<ConceptDescriptionRef>();

            // constructors

            public ConceptDescriptionRefs() { }

            public ConceptDescriptionRefs(ConceptDescriptionRefs src)
            {
                if (src.conceptDescriptions != null)
                    foreach (var cdr in src.conceptDescriptions)
                        this.conceptDescriptions.Add(new ConceptDescriptionRef(cdr));
            }

#if !DoNotUseAasxCompatibilityModels
            public ConceptDescriptionRefs(AasxCompatibilityModels.AdminShellV10.ConceptDescriptionRefs src)
            {
                if (src.conceptDescriptions != null)
                    foreach (var cdr in src.conceptDescriptions)
                        this.conceptDescriptions.Add(new ConceptDescriptionRef(cdr));
            }

            public ConceptDescriptionRefs(AasxCompatibilityModels.AdminShellV20.ConceptDescriptionRefs src)
            {
                if (src.conceptDescriptions != null)
                    foreach (var cdr in src.conceptDescriptions)
                        this.conceptDescriptions.Add(new ConceptDescriptionRef(cdr));
            }
#endif
        }

        // Note: In versions prior to V2.0.1, the SDK has "HasDataSpecification" containing only a Reference.
        // Iv 2.0.1, theoretically each entity with HasDataSpecification could also conatin a 
        // EmbeddedDataSpecification. 

        [XmlType(TypeName = "hasDataSpecification")]
        public class HasDataSpecification : List<EmbeddedDataSpecification>
        {
            public HasDataSpecification() { }

            public HasDataSpecification(HasDataSpecification src)
            {
                foreach (var r in src)
                    this.Add(new EmbeddedDataSpecification(r));
            }

            public HasDataSpecification(IEnumerable<EmbeddedDataSpecification> src)
            {
                foreach (var r in src)
                    this.Add(new EmbeddedDataSpecification(r));
            }

#if !DoNotUseAasxCompatibilityModels
            public HasDataSpecification(AasxCompatibilityModels.AdminShellV10.HasDataSpecification src)
            {
                foreach (var r in src.reference)
                    this.Add(new EmbeddedDataSpecification(r));
            }

            public HasDataSpecification(AasxCompatibilityModels.AdminShellV20.HasDataSpecification src)
            {
                foreach (var r in src)
                    this.Add(new EmbeddedDataSpecification(r));
            }

            // make some explicit and easy to use getter, setters            

            [XmlIgnore]
            [JsonIgnore]
            public EmbeddedDataSpecification IEC61360
            {
                get
                {
                    foreach (var eds in this)
                        if (eds?.dataSpecificationContent?.dataSpecificationIEC61360 != null
                            || eds?.dataSpecification?.Matches(
                                DataSpecificationIEC61360.GetIdentifier(), Key.MatchMode.Identification) == true)
                            return eds;
                    return null;
                }
                set
                {
                    // search existing first?
                    var eds = this.IEC61360;
                    if (eds != null)
                    {
                        // replace this
                        /* TODO (MIHO, 2020-08-30): this does not prevent the corner case, that we could have
                            * multiple dataSpecificationIEC61360 in this list, which would be an error */
                        this.Remove(eds);
                        this.Add(value);
                        return;
                    }

                    // no? .. add!
                    this.Add(value);
                }
            }

            [XmlIgnore]
            [JsonIgnore]
            public DataSpecificationIEC61360 IEC61360Content
            {
                get
                {
                    return this.IEC61360?.dataSpecificationContent?.dataSpecificationIEC61360;
                }
                set
                {
                    // search existing first?
                    var eds = this.IEC61360;
                    if (eds != null)
                    {
                        // replace this
                        eds.dataSpecificationContent.dataSpecificationIEC61360 = value;
                        return;
                    }
                    // no? .. add!
                    var edsnew = new EmbeddedDataSpecification();
                    edsnew.dataSpecificationContent.dataSpecificationIEC61360 = value;
                    this.Add(edsnew);
                }
            }

        }
#endif        

        [XmlType(TypeName = "langString", Namespace = "http://www.admin-shell.io/2/0")]
        public class LangStr
        {
            // constants
            public static string LANG_DEFAULT = "en";

            // members

            [MetaModelName("LangStr.lang")]
            [TextSearchable]
            [XmlAttribute(Namespace = "http://www.admin-shell.io/2/0")]
            [JsonProperty(PropertyName = "language")]
            [CountForHash]
            public string lang = "";

            [MetaModelName("LangStr.str")]
            [TextSearchable]
            [XmlText]
            [JsonProperty(PropertyName = "text")]
            [CountForHash]
            public string str = "";

            // constructors

            public LangStr() { }

            public LangStr(LangStr src)
            {
                this.lang = src.lang;
                this.str = src.str;
            }

#if !DoNotUseAasxCompatibilityModels
            public LangStr(AasxCompatibilityModels.AdminShellV10.LangStr src)
            {
                this.lang = src.lang;
                this.str = src.str;
            }

            public LangStr(AasxCompatibilityModels.AdminShellV20.LangStr src)
            {
                this.lang = src.lang;
                this.str = src.str;
            }
#endif

            public LangStr(string lang, string str)
            {
                this.lang = lang;
                this.str = str;
            }

            public static ListOfLangStr CreateManyFromStringArray(string[] s)
            {
                var r = new ListOfLangStr();
                var i = 0;
                while ((i + 1) < s.Length)
                {
                    r.Add(new LangStr(s[i], s[i + 1]));
                    i += 2;
                }
                return r;
            }

            public override string ToString()
            {
                return $"{str}@{lang}";
            }
        }

        public class ListOfLangStr : List<LangStr>
        {
            public ListOfLangStr() { }

            public ListOfLangStr(LangStr ls)
            {
                if (ls != null)
                    this.Add(ls);
            }

            public ListOfLangStr(ListOfLangStr src)
            {
                if (src != null)
                    foreach (var ls in src)
                        this.Add(ls);
            }

            public string this[string lang]
            {
                get
                {
                    return GetDefaultStr(lang);
                }
                set
                {
                    foreach (var ls in this)
                        if (ls.lang.Trim().ToLower() == lang?.Trim().ToLower())
                        {
                            ls.str = value;
                            return;
                        }
                    this.Add(new LangStr(lang, value));
                }
            }

            public string GetDefaultStr(string defaultLang = null)
            {
                // start
                if (defaultLang == null)
                    defaultLang = LangStr.LANG_DEFAULT;
                defaultLang = defaultLang.Trim().ToLower();
                string res = null;

                // search
                foreach (var ls in this)
                    if (ls.lang.Trim().ToLower() == defaultLang)
                        res = ls.str;
                if (res == null && this.Count > 0)
                    res = this[0].str;

                // found?
                return res;
            }

            public bool AllLangSameString()
            {
                if (this.Count < 2)
                    return true;

                for (int i = 1; i < this.Count; i++)
                    if (this[0]?.str != null && this[0]?.str?.Trim() != this[i]?.str?.Trim())
                        return false;

                return true;
            }

            public override string ToString()
            {
                return string.Join(", ", this.Select((ls) => ls.ToString()));
            }

            public static ListOfLangStr Parse(string cell)
            {
                // access
                if (cell == null)
                    return null;

                // iterative approach
                var res = new ListOfLangStr();
                while (true)
                {
                    // trivial case and finite end
                    if (!cell.Contains("@"))
                    {
                        if (cell.Trim() != "")
                            res.Add(new LangStr(LangStr.LANG_DEFAULT, cell));
                        break;
                    }

                    // OK, pick the next couple
                    var m = Regex.Match(cell, @"(.*?)@(\w+)", RegexOptions.Singleline);
                    if (!m.Success)
                    {
                        // take emergency exit?
                        res.Add(new LangStr("??", cell));
                        break;
                    }

                    // use the match and shorten cell ..
                    res.Add(new LangStr(m.Groups[2].ToString(), m.Groups[1].ToString().Trim()));
                    cell = cell.Substring(m.Index + m.Length);
                }

                return res;
            }
        }

        public class Description
        {

            // members

            [XmlElement(ElementName = "langString")]
            public ListOfLangStr langString = new ListOfLangStr();

            // constructors

            public Description() { }

            public Description(Description src)
            {
                if (src != null && src.langString != null)
                    foreach (var ls in src.langString)
                        langString.Add(new LangStr(ls));
            }

            public Description(LangStringSet src)
            {
                if (src != null && src.langString != null)
                    foreach (var ls in src.langString)
                        langString.Add(new LangStr(ls));
            }

#if !DoNotUseAasxCompatibilityModels
            public Description(AasxCompatibilityModels.AdminShellV10.Description src)
            {
                if (src != null)
                    foreach (var ls in src.langString)
                        langString.Add(new LangStr(ls));
            }

            public Description(AasxCompatibilityModels.AdminShellV20.Description src)
            {
                if (src != null && src.langString != null)
                    foreach (var ls in src.langString)
                        langString.Add(new LangStr(ls));
            }
#endif

            // single string representation
            public string GetDefaultStr(string defaultLang = null)
            {
                return this.langString?.GetDefaultStr(defaultLang);
            }

        }

        public class AssetKind
        {
            // constants
            public static string Type = "Type";
            public static string Instance = "Instance";

            [MetaModelName("AssetKind.kind")]
            [TextSearchable]
            [XmlText]
            [CountForHash]
            public string kind = "Instance";

            // getters / setters

            [XmlIgnore]
            [JsonIgnore]
            public bool IsInstance { get { return kind == null || kind.Trim().ToLower() == "instance"; } }

            [XmlIgnore]
            [JsonIgnore]
            public bool IsType { get { return kind != null && kind.Trim().ToLower() == "type"; } }

            // constructors / creators

            public AssetKind() { }

            public AssetKind(AssetKind src)
            {
                kind = src.kind;
            }

#if !DoNotUseAasxCompatibilityModels
            public AssetKind(AasxCompatibilityModels.AdminShellV10.Kind src)
            {
                kind = src.kind;
            }

            public AssetKind(AasxCompatibilityModels.AdminShellV20.AssetKind src)
            {
                kind = src.kind;
            }
#endif

            public AssetKind(string kind)
            {
                this.kind = kind;
            }

            public static AssetKind CreateAsType()
            {
                var res = new AssetKind() { kind = AssetKind.Type };
                return res;
            }

            public static AssetKind CreateAsInstance()
            {
                var res = new AssetKind() { kind = AssetKind.Instance };
                return res;
            }
        }

        public class ModelingKind
        {
            // constants
            public static string Template = "Template";
            public static string Instance = "Instance";

            [MetaModelName("ModelingKind.kind")]
            [TextSearchable]
            [XmlText]
            [CountForHash]
            public string kind = Instance;

            // getters / setters

            [XmlIgnore]
            [JsonIgnore]
            public bool IsInstance { get { return kind == null || kind.Trim().ToLower() == Instance.ToLower(); } }

            [XmlIgnore]
            [JsonIgnore]
            public bool IsTemplate { get { return kind != null && kind.Trim().ToLower() == Template.ToLower(); } }

            // constructors / creators

            public ModelingKind() { }

            public ModelingKind(ModelingKind src)
            {
                kind = src.kind;
            }

#if !DoNotUseAasxCompatibilityModels
            public ModelingKind(AasxCompatibilityModels.AdminShellV10.Kind src)
            {
                kind = src.kind;
            }

            public ModelingKind(AasxCompatibilityModels.AdminShellV20.ModelingKind src)
            {
                kind = src.kind;
            }
#endif

            public ModelingKind(string kind)
            {
                this.kind = kind;
            }

            public static ModelingKind CreateAsTemplate()
            {
                var res = new ModelingKind() { kind = Template };
                return res;
            }

            public static ModelingKind CreateAsInstance()
            {
                var res = new ModelingKind() { kind = Instance };
                return res;
            }

            // validation

            public static void Validate(AasValidationRecordList results, ModelingKind mk, Referable container)
            {
                // access
                if (results == null || container == null)
                    return;

                // check
                if (mk == null || mk.kind == null)
                {
                    // warning
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.Warning, container,
                        "ModelingKind: is null",
                        () =>
                        {
                        }));
                }
                else
                {
                    var k = mk.kind.Trim();
                    var kl = k.ToLower();
                    if (kl != Template.ToLower() && kl != Instance.ToLower())
                    {
                        // violation case
                        results.Add(new AasValidationRecord(
                            AasValidationSeverity.SchemaViolation, container,
                            $"ModelingKind: enumeration value neither {Template} nor {Instance}",
                            () =>
                            {
                                mk.kind = Instance;
                            }));
                    }
                    else if (k != Template && k != Instance)
                    {
                        // warning
                        results.Add(new AasValidationRecord(
                            AasValidationSeverity.Warning, container,
                            "ModelingKind: enumeration value in wrong casing",
                            () =>
                            {
                                if (kl == Template.ToLower())
                                    mk.kind = Template;
                                else
                                    mk.kind = Instance;
                            }));
                    }
                }
            }
        }

        public class SemanticId : GlobalReference
        {
            // constructors / creators

            public SemanticId() : base() { }
            public SemanticId(Identifier id) : base(id) { }
            public SemanticId(SemanticId src) : base(src) { }
            public SemanticId(Reference src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
            public SemanticId(AasxCompatibilityModels.AdminShellV10.SemanticId src) : base(src?.Keys) { }
            public SemanticId(AasxCompatibilityModels.AdminShellV20.SemanticId src) : base(src) { }
#endif

            public static SemanticId CreateFromKey(Key key)
            {
                if (key == null)
                    return null;
                var res = new SemanticId();
                res.Value.Add(key?.value);
                return res;
            }

            public static SemanticId CreateFromKeys(List<Key> keys)
            {
                if (keys == null)
                    return null;
                var res = new SemanticId();
                foreach (var k in keys)
                    res.Value.Add(k?.value);
                return res;
            }

            public new static SemanticId Parse(string input)
            {
                return (SemanticId)CreateNew(ListOfIdentifier.Parse(input));
            }
        }

        /// <summary>
        /// This class allows to describe further data (in derived classes) when enumerating Children.
        /// </summary>
        public class EnumerationPlacmentBase
        {
        }


        /// <summary>
        /// Result of FindReferable in Environment
        /// </summary>
        public class ReferableRootInfo
        {
            public AdministrationShell AAS = null;
            public AssetInformation Asset = null;
            public Submodel Submodel = null;

            public int NrOfRootKeys = 0;

            public bool IsValid
            {
                get
                {
                    return NrOfRootKeys > 0 && (AAS != null || Submodel != null || Asset != null);
                }
            }
        }

        public class DiaryDataDef
        {
            public enum TimeStampKind { Create, Update }

            [XmlIgnore]
            [JsonIgnore]
            private DateTime[] _timeStamp = new DateTime[2];

            [XmlIgnore]
            [JsonIgnore]
            public DateTime[] TimeStamp { get { return _timeStamp; } }

            /// <summary>
            /// List of entries, timewise one after each other (entries are timestamped).
            /// Note: Default is <c>Entries = null</c>, as handling of many many AAS elements does not
            /// create additional overhead of creating empty lists. An empty list shall be avoided.
            /// </summary>
            public List<IAasDiaryEntry> Entries = null;

            public static void AddAndSetTimestamps(Referable element, IAasDiaryEntry de, bool isCreate = false)
            {
                // trivial
                if (element == null || de == null || element.DiaryData == null)
                    return;

                // add entry
                if (element.DiaryData.Entries == null)
                    element.DiaryData.Entries = new List<IAasDiaryEntry>();
                element.DiaryData.Entries.Add(de);

                // figure out which timestamp
                var tsk = TimeStampKind.Update;
                if (isCreate)
                {
                    tsk = TimeStampKind.Create;
                }

                // set this timestamp (and for the parents, as well)
                IDiaryData el = element;
                while (el?.DiaryData != null)
                {
                    // itself
                    el.DiaryData.TimeStamp[(int)tsk] = DateTime.UtcNow;

                    // go up
                    el = (el as Referable)?.parent as IDiaryData;
                }
            }
        }

        public class ListOfReferable : List<Referable>
        {
            // conversion to other list

            public KeyList ToKeyList()
            {
                var res = new KeyList();
                foreach (var rf in this)
                    res.Add(rf.ToKey());
                return res;
            }

            public ModelReference GetReference()
            {
                return ModelReference.CreateNew(ToKeyList());
            }
        }

        public class Referable : IValidateEntity, IAasElement, IDiaryData, IGetModelReference, IRecurseOnReferables
        {
            // diary

            [XmlIgnore]
            [JsonIgnore]
            [SkipForHash]
            [SkipForReflection]
            private DiaryDataDef _diaryData = new DiaryDataDef();

            [XmlIgnore]
            [JsonIgnore]
            [SkipForReflection]
            public DiaryDataDef DiaryData { get { return _diaryData; } }

            // members

            [MetaModelName("Referable.IdShort")]
            [TextSearchable]
            [CountForHash]
            public string idShort = "";

            [MetaModelName("Referable.category")]
            [TextSearchable]
            [CountForHash]
            public string category = null;

            [XmlElement(ElementName = "description")]
            [JsonIgnore]
            [CountForHash]
            public Description description = null;

            [XmlIgnore]
            [JsonProperty(PropertyName = "descriptions")]
            public ListOfLangStr JsonDescription
            {
                get
                {
                    return description?.langString;
                }
                set
                {
                    if (value == null)
                    {
                        description = null;
                        return;
                    }

                    if (description == null)
                        description = new Description();
                    description.langString = value;
                }
            }

            [XmlIgnore]
            [JsonIgnore]
            [SkipForHash] // important to skip, as recursion elsewise will go in cycles!
            [SkipForReflection] // important to skip, as recursion elsewise will go in cycles!
            public IAasElement parent = null;

            public static string CONSTANT = "CONSTANT";
            public static string Category_PARAMETER = "PARAMETER";
            public static string VARIABLE = "VARIABLE";

            public static string[] ReferableCategoryNames = new string[] { CONSTANT, Category_PARAMETER, VARIABLE };

            // constructors

            public Referable() { }

            public Referable(string idShort)
            {
                this.idShort = idShort;
            }

            public Referable(Referable src)
            {
                if (src == null)
                    return;
                this.idShort = src.idShort;
                this.category = src.category;
                if (src.description != null)
                    this.description = new Description(src.description);
            }

#if !DoNotUseAasxCompatibilityModels
            public Referable(AasxCompatibilityModels.AdminShellV10.Referable src)
            {
                if (src == null)
                    return;
                this.idShort = src.idShort;
                if (this.idShort == null)
                    // change in V2.0 -> mandatory
                    this.idShort = "";
                this.category = src.category;
                if (src.description != null)
                    this.description = new Description(src.description);
            }

            public Referable(AasxCompatibilityModels.AdminShellV20.Referable src)
            {
                if (src == null)
                    return;
                this.idShort = src.idShort;
                this.category = src.category;
                if (src.description != null)
                    this.description = new Description(src.description);
            }
#endif

            /// <summary>
            /// Introduced for JSON serialization, can create Referables based on a string name
            /// </summary>
            /// <param name="elementName">string name (standard PascalCased)</param>
            public static Referable CreateAdequateType(string elementName)
            {
                if (elementName == Key.AAS)
                    return new AdministrationShell();
                if (elementName == Key.ConceptDescription)
                    return new ConceptDescription();
                if (elementName == Key.Submodel)
                    return new Submodel();
                return SubmodelElementWrapper.CreateAdequateType(elementName);
            }

            public void AddDescription(string lang, string str)
            {
                if (description == null)
                    description = new Description();
                description.langString.Add(new LangStr(lang, str));
            }

            public virtual AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Referable", "Ref");
            }

            public virtual string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
            }

            public string GetFriendlyName()
            {
                return AdminShellUtil.FilterFriendlyName(this.idShort);
            }

            public virtual ModelReference GetModelReference(bool includeParents = true)
            {
                var r = new ModelReference(new AdminShell.Key(
                    this.GetElementName(), "" + this.idShort));

                if (this is IGetSemanticId igs)
                    r.referredSemanticId = igs.GetSemanticId();

                return r;
            }

            public void CollectReferencesByParent(List<Key> refs)
            {
                // access
                if (refs == null)
                    return;

                // check, if this is identifiable
                if (this is Identifiable)
                {
                    var idf = this as Identifiable;
                    if (idf != null)
                    {
                        var k = Key.CreateNew(idf.GetElementName(), idf.id?.value);
                        refs.Insert(0, k);
                    }
                }
                else
                {
                    var k = Key.CreateNew(this.GetElementName(), this.idShort);
                    refs.Insert(0, k);
                    // recurse upwards!
                    if (this.parent is Referable prf)
                        prf.CollectReferencesByParent(refs);
                }
            }

            public string CollectIdShortByParent()
            {
                // recurse first
                var head = "";
                if (!(this is Identifiable) && this.parent is Referable prf)
                    // can go up
                    head = prf.CollectIdShortByParent() + "/";
                // add own
                var myid = "<no id-Short!>";
                if (this.idShort != null && this.idShort.Trim() != "")
                    myid = this.idShort.Trim();
                // together
                return head + myid;
            }

            // string functions

            public string ToIdShortString()
            {
                if (this.idShort == null || this.idShort.Trim().Length < 1)
                    return ("<no idShort!>");
                return this.idShort.Trim();
            }

            public override string ToString()
            {
                return "" + this.idShort;
            }

            public virtual Key ToKey()
            {
                return new Key(GetElementName(), idShort);
            }

            // hash functionality

            public class ObjectFieldInfo
            {
                public object o;
                public FieldInfo fi;
                public ObjectFieldInfo() { }
                public ObjectFieldInfo(object o, FieldInfo fi)
                {
                    this.o = o;
                    this.fi = fi;
                }
            }

            public List<ObjectFieldInfo> RecursivelyFindFields(object o, Type countAttribute, Type skipAttribute)
            {
                // access
                var res = new List<ObjectFieldInfo>();
                if (o == null || countAttribute == null)
                    return res;

                // find fields for this object
                var t = o.GetType();
                var l = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
                foreach (var f in l)
                {
                    // Skip this field??
                    if (skipAttribute != null && f.GetCustomAttribute(skipAttribute) != null)
                        continue;

                    // add directly?
                    if (f.GetCustomAttribute(countAttribute) != null)
                        res.Add(new ObjectFieldInfo(o, f));

                    // object
                    if (f.FieldType.IsClass)
                    {
                        var oo = f.GetValue(o);
                        var r = RecursivelyFindFields(oo, countAttribute, skipAttribute);
                        res.AddRange(r);
                    }

                    // try cast in IList to check further
                    var elems = f.GetValue(o) as IList;
                    if (elems != null)
                        foreach (var e in elems)
                        {
                            var r = RecursivelyFindFields(e, countAttribute, skipAttribute);
                            res.AddRange(r);
                        }

                }
                // OK
                return res;
            }

            public byte[] ComputeByteArray()
            {
                // use memory stream for effcient behaviour
                var mems = new MemoryStream();

                // TEST
                var xxx = RecursivelyFindFields(this, typeof(CountForHash), typeof(SkipForHash));

                foreach (var ofi in xxx)
                {
                    var a = ofi.fi.GetCustomAttribute<CountForHash>();
                    if (a != null)
                    {
                        // found an accountable field, get bytes
                        var o = ofi.fi.GetValue(ofi.o);
                        byte[] bs = null;

                        // optimize for probabilities

                        if (o is string)
                            bs = System.Text.Encoding.UTF8.GetBytes((string)o);
                        else if (o is char[])
                            bs = System.Text.Encoding.UTF8.GetBytes((char[])o);
                        else if (o is double)
                            bs = BitConverter.GetBytes((double)o);
                        else if (o is float)
                            bs = BitConverter.GetBytes((float)o);
                        else if (o is char)
                            bs = BitConverter.GetBytes((char)o);
                        else if (o is byte)
                            bs = BitConverter.GetBytes((byte)o);
                        else if (o is int)
                            bs = BitConverter.GetBytes((int)o);
                        else if (o is long)
                            bs = BitConverter.GetBytes((long)o);
                        else if (o is short)
                            bs = BitConverter.GetBytes((short)o);
                        else if (o is uint)
                            bs = BitConverter.GetBytes((uint)o);
                        else if (o is ulong)
                            bs = BitConverter.GetBytes((ulong)o);
                        else if (o is ushort)
                            bs = BitConverter.GetBytes((ushort)o);

                        if (bs != null)
                            mems.Write(bs, 0, bs.Length);
                    }
                }

                return mems.ToArray();
            }

            private static System.Security.Cryptography.SHA256 HashProvider =
                System.Security.Cryptography.SHA256.Create();

            public string ComputeHashcode()
            {
                var dataBytes = this.ComputeByteArray();
                var hashBytes = Referable.HashProvider.ComputeHash(dataBytes);

                StringBuilder sb = new StringBuilder();
                foreach (var hb in hashBytes)
                    sb.Append(hb.ToString("X2"));
                return sb.ToString();
            }

            // sorting

            public class ComparerIdShort : IComparer<Referable>
            {
                public int Compare(Referable a, Referable b)
                {
                    return String.Compare(a?.idShort, b?.idShort,
                        CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
                }
            }

            public class ComparerIndexed : IComparer<Referable>
            {
                public int NullIndex = int.MaxValue;
                public Dictionary<Referable, int> Index = new Dictionary<Referable, int>();

                public int Compare(Referable a, Referable b)
                {
                    var ca = Index.ContainsKey(a);
                    var cb = Index.ContainsKey(b);

                    if (!ca && !cb)
                        return 0;
                    // make CDs without usage to appear at end of list
                    if (!ca)
                        return +1;
                    if (!cb)
                        return -1;

                    var ia = Index[a];
                    var ib = Index[b];

                    if (ia == ib)
                        return 0;
                    if (ia < ib)
                        return -1;
                    return +1;
                }
            }

            // validation

            public virtual void Validate(AasValidationRecordList results)
            {
                // access
                if (results == null)
                    return;

                // check
                if (this.idShort == null || this.idShort.Trim() == "")
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SpecViolation, this,
                        "Referable: missing idShort",
                        () =>
                        {
                            this.idShort = "TO_FIX";
                        }));

                if (this.description != null && (this.description.langString == null
                    || this.description.langString.Count < 1))
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SchemaViolation, this,
                        "Referable: existing description with missing langString",
                        () =>
                        {
                            this.description = null;
                        }));
            }

            // hierarchy & recursion (by derived elements)

            public virtual void RecurseOnReferables(
                object state, Func<object, ListOfReferable, Referable, bool> lambda,
                bool includeThis = false)
            {
                if (includeThis)
                    lambda(state, null, this);
            }

            public Identifiable FindParentFirstIdentifiable()
            {
                Referable curr = this;
                while (curr != null)
                {
                    if (curr is Identifiable curri)
                        return curri;
                    curr = curr.parent as Referable;
                }
                return null;
            }
        }

        public class Identifiable : Referable, IGetModelReference
        {

            // members

            // this is complex, because V3.0 made id a simple string
            // and this should be translated to the outside.
            // TODO (MIHO, 2021-12-30): consider a converter for this
            // https://stackoverflow.com/questions/24472404/json-net-how-to-serialize-object-as-value

            [JsonIgnore]
            public Identifier id = new Identifier();
            [XmlIgnore]
            [JsonProperty(PropertyName = "id")]
            public string JsonId { 
                get { return id?.value; } 
                set {
                    if (id == null)
                        id = new Identifier(value);
                    else
                        id.value = value;
                } 
            }

            // rest of members

            public Administration administration = null;

            // constructors

            public Identifiable() : base() { }

            public Identifiable(string idShort) : base(idShort) { }

            public Identifiable(Identifiable src)
                : base(src)
            {
                if (src == null)
                    return;
                if (src.id != null)
                    this.id = new Identifier(src.id);
                if (src.administration != null)
                    this.administration = new Administration(src.administration);
            }

#if !DoNotUseAasxCompatibilityModels
            public Identifiable(AasxCompatibilityModels.AdminShellV10.Identifiable src)
                : base(src)
            {
                if (src.identification != null)
                    this.id = new Identifier(src.identification);
                if (src.administration != null)
                    this.administration = new Administration(src.administration);
            }

            public Identifiable(AasxCompatibilityModels.AdminShellV20.Identifiable src)
                : base(src)
            {
                if (src.identification != null)
                    this.id = new Identifier(src.identification);
                if (src.administration != null)
                    this.administration = new Administration(src.administration);
            }
#endif

            public void SetIdentification(string id, string idShort = null)
            {
                this.id.value = id;
                if (idShort != null)
                    this.idShort = idShort;
            }

            public void SetAdminstration(string version, string revision)
            {
                if (administration == null)
                    administration = new Administration();
                administration.version = version;
                administration.revision = revision;
            }

            public new string GetFriendlyName()
            {
                if (id != null && id.value != "")
                    return AdminShellUtil.FilterFriendlyName(this.id.value);
                return AdminShellUtil.FilterFriendlyName(this.idShort);
            }

            public override string ToString()
            {
                return ("" + id?.ToString() + " " + administration?.ToString()).Trim();
            }

            public override Key ToKey()
            {
                return new Key(GetElementName(), "" + id?.value);
            }

            // self description

            public override ModelReference GetModelReference(bool includeParents = true)
            {
                var r = new ModelReference();

                // TODO: SEM ID
                if (this is IGetSemanticId igs)
                    r.referredSemanticId = igs.GetSemanticId();

                r.Keys.Add(
                    Key.CreateNew(this.GetElementName(), this.id.value));
                return r;
            }

            // sorting

            public class ComparerIdentification : IComparer<Identifiable>
            {
                public int Compare(Identifiable a, Identifiable b)
                {
                    if (a?.id == null && b?.id == null)
                        return 0;
                    if (a?.id == null)
                        return +1;
                    if (b?.id == null)
                        return -1;

                    return String.Compare(a.id.value, b.id.value,
                        CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
                }
            }

        }

        /// <summary>
        /// Multiple lang string for the AAS namespace
        /// </summary>
        public class LangStringSet
        {

            // members

            [XmlElement(ElementName = "langString", Namespace = "http://www.admin-shell.io/aas/2/0")]
            public ListOfLangStr langString = new ListOfLangStr();

            // getters / setters

            [XmlIgnore]
            [JsonIgnore]
            public bool IsEmpty { get { return langString == null || langString.Count < 1; } }
            [XmlIgnore]
            [JsonIgnore]
            public int Count { get { if (langString == null) return 0; return langString.Count; } }
            [XmlIgnore]
            [JsonIgnore]
            public LangStr this[int index] { get { return langString[index]; } }

            // constructors

            public LangStringSet() { }

            public LangStringSet(LangStringSet src)
            {
                if (src.langString != null)
                    foreach (var ls in src.langString)
                        this.langString.Add(new LangStr(ls));
            }

            public LangStringSet(ListOfLangStr src)
            {
                if (src != null)
                    foreach (var ls in src)
                        this.langString.Add(new LangStr(ls));
            }

#if !DoNotUseAasxCompatibilityModels
            public LangStringSet(AasxCompatibilityModels.AdminShellV20.LangStringSet src)
            {
                if (src.langString != null)
                    foreach (var ls in src.langString)
                        this.langString.Add(new LangStr(ls));
            }
#endif
            public LangStringSet(string lang, string str)
            {
                if (str == null || str.Trim() == "")
                    return;
                this.langString.Add(new LangStr(lang, str));
            }

            // converter

            public static LangStringSet CreateFrom(List<LangStr> src)
            {
                var res = new LangStringSet();
                if (src != null)
                    foreach (var ls in src)
                        res.langString.Add(new LangStr(ls));
                return res;
            }

            // add

            public LangStr Add(LangStr ls)
            {
                this.langString.Add(ls);
                return ls;
            }

            public LangStr Add(string lang, string str)
            {
                var ls = new LangStr(lang, str);
                this.langString.Add(ls);
                return ls;
            }

            // single string representation
            public string GetDefaultStr(string defaultLang = null)
            {
                return this.langString?.GetDefaultStr(defaultLang);
            }
        }

        [XmlRoot(ElementName = "aasenv", Namespace = "http://www.admin-shell.io/aas/2/0")]
        public class AdministrationShellEnv : IFindAllReferences, IAasElement, IDiaryData, IRecurseOnReferables
        {
            // diary (as e.g. deleted AASes need to be listed somewhere)

            [XmlIgnore]
            [JsonIgnore]
            [SkipForHash]
            [SkipForReflection]
            private DiaryDataDef _diaryData = new DiaryDataDef();

            [XmlIgnore]
            [JsonIgnore]
            [SkipForReflection]
            public DiaryDataDef DiaryData { get { return _diaryData; } }

            // members

            [XmlAttribute(Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
            [JsonIgnore]
            public string schemaLocation =
                "http://www.admin-shell.io/aas/2/0 AAS.xsd http://www.admin-shell.io/IEC61360/2/0 IEC61360.xsd";

            [XmlIgnore] // will be ignored, anyway
            private ListOfAdministrationShells administrationShells = new ListOfAdministrationShells();
            [XmlIgnore] // will be ignored, anyway
            private ListOfSubmodels submodels = new ListOfSubmodels();
            [XmlIgnore] // will be ignored, anyway
            private ListOfConceptDescriptions conceptDescriptions = new ListOfConceptDescriptions();

            // getter / setters

            [XmlArray("assetAdministrationShells")]
            [XmlArrayItem("assetAdministrationShell")]
            [JsonProperty(PropertyName = "assetAdministrationShells")]
            public ListOfAdministrationShells AdministrationShells
            {
                get { return administrationShells; }
                set { administrationShells = value; }
            }

            [XmlArray("submodels")]
            [XmlArrayItem("submodel")]
            [JsonProperty(PropertyName = "submodels")]
            public ListOfSubmodels Submodels
            {
                get { return submodels; }
                set { submodels = value; }
            }

            [XmlArray("conceptDescriptions")]
            [XmlArrayItem("conceptDescription")]
            [JsonProperty(PropertyName = "conceptDescriptions")]
            public ListOfConceptDescriptions ConceptDescriptions
            {
                get { return conceptDescriptions; }
                set { conceptDescriptions = value; }
            }

            // constructors

            public AdministrationShellEnv() { }

#if !DoNotUseAasxCompatibilityModels
            public AdministrationShellEnv(AasxCompatibilityModels.AdminShellV10.AdministrationShellEnv src)
            {
                if (src.AdministrationShells != null)
                    foreach (var aas in src.AdministrationShells)
                        this.administrationShells.Add(new AdministrationShell(aas, src));

                // AssetInformation is aleady migrated in the constrcutor above

                if (src.Submodels != null)
                    foreach (var sm in src.Submodels)
                        this.submodels.Add(new Submodel(sm));

                if (src.ConceptDescriptions != null)
                    foreach (var cd in src.ConceptDescriptions)
                        this.conceptDescriptions.Add(new ConceptDescription(cd));
            }

            public AdministrationShellEnv(AasxCompatibilityModels.AdminShellV20.AdministrationShellEnv src)
            {
                if (src.AdministrationShells != null)
                    foreach (var aas in src.AdministrationShells)
                        this.administrationShells.Add(new AdministrationShell(aas, src));

                // AssetInformation is aleady migrated in the constrcutor above

                if (src.Submodels != null)
                    foreach (var sm in src.Submodels)
                        this.submodels.Add(new Submodel(sm));

                if (src.ConceptDescriptions != null)
                    foreach (var cd in src.ConceptDescriptions)
                        this.conceptDescriptions.Add(new ConceptDescription(cd));
            }
#endif

            // to String

            public override string ToString()
            {
                var res = "AAS-ENV";
                if (AdministrationShells != null)
                    res += $" {AdministrationShells.Count} AAS";
                if (Submodels != null)
                    res += $" {Submodels.Count} Submodels";
                if (ConceptDescriptions != null)
                    res += $" {ConceptDescriptions.Count} CDs";
                return res;
            }

            // self decscription

            public AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("AdministrationShellEnv", "Env");
            }

            public string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
            }

            // finders

            public AdministrationShell FindAAS(Identifier id)
            {
                if (id == null)
                    return null;
                foreach (var aas in this.AdministrationShells)
                    if (aas.id != null && aas.id.IsEqual(id))
                        return aas;
                return null;
            }

            public AdministrationShell FindAAS(string idShort)
            {
                if (idShort == null)
                    return null;
                foreach (var aas in this.AdministrationShells)
                    if (aas.idShort != null && aas.idShort.Trim().ToLower() == idShort.Trim().ToLower())
                        return aas;
                return null;
            }

            public AdministrationShell FindAASwithSubmodel(Identifier smid)
            {
                if (smid == null)
                    return null;
                foreach (var aas in this.AdministrationShells)
                    if (aas.submodelRefs != null)
                        foreach (var smref in aas.submodelRefs)
                            if (smref.Matches(smid))
                                return aas;
                return null;
            }

            public IEnumerable<AdministrationShell> FindAllAAS(
                Predicate<AdministrationShell> p = null)
            {
                if (this.administrationShells == null)
                    yield break;
                foreach (var x in this.administrationShells)
                    if (p == null || p(x))
                        yield return x;
            }

            public IEnumerable<Submodel> FindAllSubmodelGroupedByAAS(
                Func<AdministrationShell, Submodel, bool> p = null)
            {
                if (this.administrationShells == null || this.submodels == null)
                    yield break;
                foreach (var aas in this.administrationShells)
                {
                    if (aas?.submodelRefs == null)
                        continue;
                    foreach (var smref in aas.submodelRefs)
                    {
                        var sm = this.FindSubmodel(smref);
                        if (sm != null && (p == null || p(aas, sm)))
                            yield return sm;
                    }
                }
            }

            public AdministrationShell FindAasWithAssetInfo(Identifier id)
            {
                if (id == null)
                    return null;
                foreach (var aas in this.AdministrationShells)
                    if (aas?.assetInformation?.globalAssetId?.Matches(id) == true)
                        return aas;
                return null;
            }

            public AdministrationShell FindAasWithAsset(AssetRef aref)
            {
                // trivial
                if (aref == null)
                    return null;
                // can only refs with 1 key
                if (aref.Count != 1)
                    return null;
                // brute force
                foreach (var aas in this.AdministrationShells)
                    if (aas?.assetInformation?.globalAssetId?.Matches(aref) == true)
                        return aas;
                // uups
                return null;
            }

            public Submodel FindSubmodel(Identifier id)
            {
                if (id == null)
                    return null;
                foreach (var sm in this.Submodels)
                    if (sm.id != null && sm.id.IsEqual(id))
                        return sm;
                return null;
            }

            public Submodel FindSubmodel(SubmodelRef smref)
            {
                // trivial
                if (smref == null)
                    return null;
                // can only refs with 1 key
                if (smref.Count != 1)
                    return null;
                // and we're picky
                var key = smref.Keys[0];
                if (key.type.ToLower().Trim() != "submodel")
                    return null;
                // brute force
                foreach (var sm in this.Submodels)
                    if (sm.id.value.ToLower().Trim() == key.value.ToLower().Trim())
                        return sm;
                // uups
                return null;
            }

            public Submodel FindFirstSubmodelBySemanticId(Identifier semId)
            {
                // access
                if (semId == null)
                    return null;

                // brute force
                foreach (var sm in this.Submodels)
                    if (true == sm.semanticId?.MatchesExactlyOneId(semId))
                        return sm;

                return null;
            }

            public IEnumerable<Submodel> FindAllSubmodelBySemanticId(
                Identifier semId, Key.MatchMode matchMode = Key.MatchMode.Identification)
            {
                // access
                if (semId == null)
                    yield break;

                // brute force
                foreach (var sm in this.Submodels)
                    if (true == sm.semanticId?.MatchesExactlyOneId(semId, matchMode))
                        yield return sm;
            }

            public IEnumerable<Referable> FindAllReferable(Predicate<Referable> p)
            {
                if (p == null)
                    yield break;

                foreach (var r in this.FindAllReferable())
                    if (r != null && p(r))
                        yield return r;
            }

            public IEnumerable<Referable> FindAllReferable(bool onlyIdentifiables = false)
            {
                if (this.AdministrationShells != null)
                    foreach (var aas in this.AdministrationShells)
                        if (aas != null)
                        {
                            // AAS itself
                            yield return aas;
                        }

                if (this.Submodels != null)
                    foreach (var sm in this.Submodels)
                        if (sm != null)
                        {
                            yield return sm;

                            if (!onlyIdentifiables)
                            {
                                // TODO (MIHO, 2020-08-26): not very elegant, yet. Avoid temporary collection
                                var allsme = new ListOfSubmodelElement();
                                sm.RecurseOnSubmodelElements(null, (state, parents, sme) =>
                                {
                                    allsme.Add(sme); return true;
                                });
                                foreach (var sme in allsme)
                                    yield return sme;
                            }
                        }

                if (this.ConceptDescriptions != null)
                    foreach (var cd in this.ConceptDescriptions)
                        if (cd != null)
                            yield return cd;
            }

            //
            // Reference handling
            //

            public Referable FindReferableByReference(ModelReference rf, int keyIndex = 0, bool exactMatch = false)
            {
                return FindReferableByReference(rf?.Keys);
            }

            public Referable FindReferableByReference(KeyList kl, int keyIndex = 0, bool exactMatch = false,
                ReferableRootInfo rootInfo = null)
            {
                // first index needs to exist ..
                if (kl == null || keyIndex >= kl.Count)
                    return null;

                // which type?
                var firstType = kl[keyIndex].type.Trim().ToLower();
                var firstIdentification = new Identifier(kl[keyIndex].value);
                AdministrationShell aasToFollow = null;

                if (firstType == Key.AAS.Trim().ToLower())
                {
                    // search aas
                    var aas = this.FindAAS(firstIdentification);

                    // not found or already at end with our search?
                    if (aas == null || keyIndex >= kl.Count - 1)
                        return aas;

                    // side info?
                    if (rootInfo != null)
                    {
                        rootInfo.AAS = aas;
                        rootInfo.NrOfRootKeys = 1 + keyIndex;
                    }

                    // follow up
                    aasToFollow = aas;
                }

                if (firstType == Key.AssetInformation.Trim().ToLower())
                {
                    // search asset
                    var aas4asset = this.FindAasWithAssetInfo(firstIdentification);

                    // not found or already at end with our search?
                    if (aas4asset == null || keyIndex >= kl.Count - 1)
                        return exactMatch ? null : aas4asset;

                    // side info?
                    if (rootInfo != null)
                    {
                        rootInfo.Asset = aas4asset?.assetInformation;
                        rootInfo.NrOfRootKeys = 1 + keyIndex;
                    }

                    // follow up
                    aasToFollow = aas4asset;
                }

                // try
                if (aasToFollow != null)
                {
                    // search different entities
                    if (kl[keyIndex + 1].type.Trim().ToLower() == Key.Submodel.ToLower()
                        || kl[keyIndex + 1].type.Trim().ToLower() == Key.SubmodelRef.ToLower())
                    {
                        // ok, search SubmodelRef
                        var smref = aasToFollow.FindSubmodelRef(kl[keyIndex + 1].ToId());
                        if (smref == null)
                            return exactMatch ? null : aasToFollow;

                        // validate matching submodel
                        var sm = this.FindSubmodel(smref);
                        if (sm == null)
                            return exactMatch ? null : aasToFollow;

                        // side info
                        // side info?
                        if (rootInfo != null)
                        {
                            rootInfo.Submodel = sm;
                            rootInfo.NrOfRootKeys = 2 + keyIndex;
                        }

                        // at our end?
                        if (keyIndex >= kl.Count - 2)
                            return sm;

                        // go inside
                        return SubmodelElementWrapper.FindReferableByReference(sm.submodelElements, kl, keyIndex + 2);
                    }
                }

                if (firstType == Key.ConceptDescription.Trim().ToLower())
                    return this.FindConceptDescription(firstIdentification);

                if (firstType == Key.Submodel.Trim().ToLower())
                {
                    // ok, search Submodel
                    var sm = this.FindSubmodel(new Identifier(kl[keyIndex].value));
                    if (sm == null)
                        return null;

                    // notice in side info
                    if (rootInfo != null)
                    {
                        rootInfo.Submodel = sm;
                        rootInfo.NrOfRootKeys = 1 + keyIndex;

                        // add even more info
                        if (rootInfo.AAS == null)
                        {
                            foreach (var aas2 in this.AdministrationShells)
                            {
                                var smref2 = aas2.FindSubmodelRef(sm.id);
                                if (smref2 != null)
                                {
                                    rootInfo.AAS = aas2;
                                    break;
                                }
                            }
                        }
                    }

                    // at our end?
                    if (keyIndex >= kl.Count - 1)
                        return sm;

                    // go inside
                    return SubmodelElementWrapper.FindReferableByReference(sm.submodelElements, kl, keyIndex + 1);
                }

                // nothing in this Environment
                return null;
            }

            //
            // Handling of CDs
            //

            public ConceptDescription FindConceptDescription(ConceptDescriptionRef cdr)
            {
                if (cdr == null)
                    return null;
                return FindConceptDescription(cdr.Keys);
            }

            public ConceptDescription FindConceptDescription(SemanticId semId)
            {
                if (semId == null)
                    return null;
                return FindConceptDescription(semId.Value);
            }

            public ConceptDescription FindConceptDescription(ModelReference rf)
            {
                if (rf == null)
                    return null;
                return FindConceptDescription(rf.Keys);
            }

            public ConceptDescription FindConceptDescription(Identifier id)
            {
                var cdr = ConceptDescriptionRef.CreateNew(Key.ConceptDescription, id.value);
                return FindConceptDescription(cdr);
            }

            public ConceptDescription FindConceptDescription(List<Key> keys)
            {
                // trivial
                if (keys == null)
                    return null;
                // can only refs with 1 key
                if (keys.Count != 1)
                    return null;
                // and we're picky
                var key = keys[0];
                if (key.type.ToLower().Trim() != "conceptdescription")
                    return null;
                // brute force
                foreach (var cd in conceptDescriptions)
                    if (cd.id.value.ToLower().Trim() == key.value.ToLower().Trim())
                        return cd;
                // uups
                return null;
            }

            public ConceptDescription FindConceptDescription(ListOfIdentifier loi)
            {
                // trivial
                if (loi == null)
                    return null;
                // can only refs with 1 key
                if (loi.Count != 1)
                    return null;
                // and we're picky
                var id = loi[0];
                // brute force
                foreach (var cd in conceptDescriptions)
                    if (cd.id.value.ToLower().Trim() == id.value.ToLower().Trim())
                        return cd;
                // uups
                return null;
            }

            public IEnumerable<T> FindAllSubmodelElements<T>(
                Predicate<T> match = null, AdministrationShell onlyForAAS = null) where T : SubmodelElement
            {
                // more or less two different schemes
                if (onlyForAAS != null)
                {
                    if (onlyForAAS.submodelRefs == null)
                        yield break;
                    foreach (var smr in onlyForAAS.submodelRefs)
                    {
                        var sm = this.FindSubmodel(smr);
                        if (sm?.submodelElements != null)
                            foreach (var x in sm.submodelElements.FindDeep<T>(match))
                                yield return x;
                    }
                }
                else
                {
                    if (this.Submodels != null)
                        foreach (var sm in this.Submodels)
                            if (sm?.submodelElements != null)
                                foreach (var x in sm.submodelElements.FindDeep<T>(match))
                                    yield return x;
                }
            }

            public ConceptDescription FindConceptDescription(Key key)
            {
                if (key == null)
                    return null;
                var l = new List<Key> { key };
                return (FindConceptDescription(l));
            }

            public IEnumerable<LocatedReference> FindAllReferences()
            {
                if (this.AdministrationShells != null)
                    foreach (var aas in this.AdministrationShells)
                        if (aas != null)
                            foreach (var r in aas.FindAllReferences())
                                yield return r;

                if (this.Submodels != null)
                    foreach (var sm in this.Submodels)
                        if (sm != null)
                            foreach (var r in sm.FindAllReferences())
                                yield return r;

                if (this.ConceptDescriptions != null)
                    foreach (var cd in this.ConceptDescriptions)
                        if (cd != null)
                            foreach (var r in cd.FindAllReferences())
                                yield return new LocatedReference(cd, r);
            }

            // creators

            private void CopyConceptDescriptionsFrom(
                AdministrationShellEnv srcEnv, SubmodelElement src, bool shallowCopy = false)
            {
                // access
                if (srcEnv == null || src == null || src.semanticId == null)
                    return;
                // check for this SubmodelElement in Source
                var cdSrc = srcEnv.FindConceptDescription(src.semanticId);
                if (cdSrc == null)
                    return;
                // check for this SubmodelElement in Destnation (this!)
                var cdDest = this.FindConceptDescription(src.semanticId);
                if (cdDest != null)
                    return;
                // copy new
                this.ConceptDescriptions.Add(new ConceptDescription(cdSrc));
                // recurse?
                if (!shallowCopy && src is SubmodelElementCollection)
                    foreach (var m in (src as SubmodelElementCollection).value)
                        CopyConceptDescriptionsFrom(srcEnv, m.submodelElement, shallowCopy: false);

            }

            public SubmodelElementWrapper CopySubmodelElementAndCD(
                AdministrationShellEnv srcEnv, SubmodelElement srcElem, bool copyCD = false, bool shallowCopy = false)
            {
                // access
                if (srcEnv == null || srcElem == null)
                    return null;

                // 1st result pretty easy (calling function will add this to the appropriate Submodel)
                var res = new SubmodelElementWrapper(srcElem);

                // copy the CDs..
                if (copyCD)
                    CopyConceptDescriptionsFrom(srcEnv, srcElem, shallowCopy);

                // give back
                return res;
            }

            public SubmodelRef CopySubmodelRefAndCD(
                AdministrationShellEnv srcEnv, SubmodelRef srcSubRef, bool copySubmodel = false, bool copyCD = false,
                bool shallowCopy = false)
            {
                // access
                if (srcEnv == null || srcSubRef == null)
                    return null;

                // need to have the source Submodel
                var srcSub = srcEnv.FindSubmodel(srcSubRef);
                if (srcSub == null)
                    return null;

                // 1st result pretty easy (calling function will add this to the appropriate AAS)
                var dstSubRef = new SubmodelRef(srcSubRef);

                // get the destination and shall src != dst
                var dstSub = this.FindSubmodel(dstSubRef);
                if (srcSub == dstSub)
                    return null;

                // maybe we need the Submodel in our environment, as well
                if (dstSub == null && copySubmodel)
                {
                    dstSub = new Submodel(srcSub, shallowCopy);
                    this.Submodels.Add(dstSub);
                }
                else
                if (dstSub != null)
                {
                    // there is already an submodel, just add members
                    if (!shallowCopy && srcSub.submodelElements != null)
                    {
                        if (dstSub.submodelElements == null)
                            dstSub.submodelElements = new SubmodelElementWrapperCollection();
                        foreach (var smw in srcSub.submodelElements)
                            dstSub.submodelElements.Add(
                                new SubmodelElementWrapper(
                                    smw.submodelElement, shallowCopy: false));
                    }
                }

                // copy the CDs..
                if (copyCD && srcSub.submodelElements != null)
                    foreach (var smw in srcSub.submodelElements)
                        CopyConceptDescriptionsFrom(srcEnv, smw.submodelElement, shallowCopy);

                // give back
                return dstSubRef;
            }

            /// <summary>
            /// Tries renaming an Identifiable, specifically: the identification of an Identifiable and
            /// all references to it.
            /// Currently supported: ConceptDescriptions
            /// Returns a list of Referables, which were changed or <c>null</c> in case of error
            /// </summary>
            public List<Referable> RenameIdentifiable<T>(Identifier oldId, Identifier newId)
                where T : IAasElement
            {
                // access
                if (oldId == null || newId == null || oldId.IsEqual(newId))
                    return null;

                var res = new List<Referable>();

                if (typeof(T) == typeof(ConceptDescription))
                {
                    // check, if exist or not exist
                    var cdOld = FindConceptDescription(oldId);
                    if (cdOld == null || FindConceptDescription(newId) != null)
                        return null;

                    // rename old cd
                    cdOld.id = newId;
                    res.Add(cdOld);

                    // search all SMEs referring to this CD
                    foreach (var sme in this.FindAllSubmodelElements<SubmodelElement>(match: (s) =>
                    {
                        return (s != null && s.semanticId != null && s.semanticId.Matches(oldId));
                    }))
                    {
                        sme.semanticId[0].value = newId.value;
                        res.Add(sme);
                    }

                    // seems fine
                    return res;
                }

                if (typeof(T) == typeof(Submodel))
                {
                    // check, if exist or not exist
                    var smOld = FindSubmodel(oldId);
                    if (smOld == null || FindSubmodel(newId) != null)
                        return null;

                    // recurse all possible Referenes in the aas env
                    foreach (var lr in this.FindAllReferences())
                    {
                        var r = lr?.Reference;
                        if (r != null)
                            for (int i = 0; i < r.Count; i++)
                                if (r[i].Matches(Key.Submodel, oldId.value, Key.MatchMode.Relaxed))
                                {
                                    // directly replace
                                    r[i].value = newId.value;
                                    if (res.Contains(lr.Identifiable))
                                        res.Add(lr.Identifiable);
                                }
                    }

                    // rename old Submodel
                    smOld.id = newId;

                    // seems fine
                    return res;
                }

                if (typeof(T) == typeof(AssetInformation))
                {
                    // check, if exist or not exist
                    var assetOld = FindAasWithAssetInfo(oldId);
                    if (assetOld == null || FindAasWithAssetInfo(newId) != null)
                        return null;

                    // recurse all possible Referenes in the aas env
                    foreach (var lr in this.FindAllReferences())
                    {
                        var r = lr?.Reference;
                        if (r != null)
                            for (int i = 0; i < r.Count; i++)
                                if (r[i].Matches(Key.AssetInformation, oldId.value))
                                {
                                    // directly replace
                                    r[i].value = newId.value;
                                    if (res.Contains(lr.Identifiable))
                                        res.Add(lr.Identifiable);
                                }
                    }

                    // rename old Asset
                    assetOld.assetInformation.SetIdentification(newId);

                    // seems fine
                    return res;
                }

                // no result is false, as well
                return null;
            }

            // serializations

            public void SerializeXmlToStream(StreamWriter s)
            {
                var serializer = new XmlSerializer(typeof(AdminShell.AdministrationShellEnv));
                var nss = new XmlSerializerNamespaces();
                nss.Add("xsi", System.Xml.Schema.XmlSchema.InstanceNamespace);
                nss.Add("aas", "http://www.admin-shell.io/aas/2/0");
                nss.Add("IEC61360", "http://www.admin-shell.io/IEC61360/2/0");
                serializer.Serialize(s, this, nss);
            }

            public JsonWriter SerialiazeJsonToStream(StreamWriter sw, bool leaveJsonWriterOpen = false)
            {
                sw.AutoFlush = true;

                JsonSerializer serializer = new JsonSerializer()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    Formatting = Newtonsoft.Json.Formatting.Indented
                };

                JsonWriter writer = new JsonTextWriter(sw);
                serializer.Serialize(writer, this);
                if (leaveJsonWriterOpen)
                    return writer;
                writer.Close();
                return null;
            }

            public AdministrationShellEnv DeserializeFromXmlStream(TextReader reader)
            {
                XmlSerializer serializer = new XmlSerializer(
                    typeof(AdminShell.AdministrationShellEnv), "http://www.admin-shell.io/aas/2/0");
                var res = serializer.Deserialize(reader) as AdminShell.AdministrationShellEnv;
                return res;
            }

            public AdministrationShellEnv DeserializeFromJsonStream(TextReader reader)
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                var res = (AdministrationShellEnv)serializer.Deserialize(reader, typeof(AdministrationShellEnv));
                return res;
            }

            // special functions

            private static void CreateFromExistingEnvRecurseForCDs(
                AdministrationShellEnv src, List<SubmodelElementWrapper> wrappers,
                ref List<ConceptDescription> filterForCD)
            {
                if (wrappers == null || filterForCD == null)
                    return;

                foreach (var w in wrappers)
                {
                    // access
                    if (w == null)
                        continue;

                    // include in filter ..
                    if (w.submodelElement.semanticId != null)
                    {
                        var cd = src.FindConceptDescription(w.submodelElement.semanticId);
                        if (cd != null)
                            filterForCD.Add(cd);
                    }

                    // recurse?
                    if (w.submodelElement is SubmodelElementCollection smec)
                        CreateFromExistingEnvRecurseForCDs(src, smec.value, ref filterForCD);

                    if (w.submodelElement is Operation op)
                        for (int i = 0; i < 2; i++)
                        {
                            var w2s = Operation.GetWrappers(op[i]);
                            CreateFromExistingEnvRecurseForCDs(src, w2s, ref filterForCD);
                        }

                    if (w.submodelElement is Entity smee)
                        CreateFromExistingEnvRecurseForCDs(src, smee.statements, ref filterForCD);

                    if (w.submodelElement is AnnotatedRelationshipElement smea)
                        CreateFromExistingEnvRecurseForCDs(src, smea.annotations, ref filterForCD);
                }
            }

            public static AdministrationShellEnv CreateFromExistingEnv(AdministrationShellEnv src,
                List<AdministrationShell> filterForAas = null,
                List<AssetInformation> filterForAsset = null,
                ListOfSubmodels filterForSubmodel = null,
                List<ConceptDescription> filterForCD = null)
            {
                // prepare defaults
                if (filterForAas == null)
                    filterForAas = new List<AdministrationShell>();
                if (filterForSubmodel == null)
                    filterForSubmodel = new ListOfSubmodels();
                if (filterForCD == null)
                    filterForCD = new List<ConceptDescription>();

                // make new
                var res = new AdministrationShellEnv();

                // take over AAS
                foreach (var aas in src.administrationShells)
                    if (filterForAas.Contains(aas))
                    {
                        // take over
                        res.administrationShells.Add(new AdministrationShell(aas));

                        if (aas.submodelRefs != null)
                            foreach (var smr in aas.submodelRefs)
                            {
                                var sm = src.FindSubmodel(smr);
                                if (sm != null)
                                    filterForSubmodel.Add(sm);
                            }
                    }

                // take over Submodels
                foreach (var sm in src.Submodels)
                    if (filterForSubmodel.Contains(sm))
                    {
                        // take over
                        res.submodels.Add(new Submodel(sm));

                        // recursion in order to find used CDs
                        CreateFromExistingEnvRecurseForCDs(src, sm.submodelElements, ref filterForCD);
                    }

                // ConceptDescriptions
                foreach (var cd in src.ConceptDescriptions)
                    if (filterForCD.Contains(cd))
                    {
                        // take over
                        res.conceptDescriptions.Add(new ConceptDescription(cd));
                    }

                // ok
                return res;
            }

            // Sorting

            public Referable.ComparerIndexed CreateIndexedComparerCdsForSmUsage()
            {
                var cmp = new Referable.ComparerIndexed();
                int nr = 0;
                foreach (var sm in FindAllSubmodelGroupedByAAS())
                    foreach (var sme in sm.FindDeep<SubmodelElement>())
                    {
                        if (sme.semanticId == null)
                            continue;
                        var cd = this.FindConceptDescription(sme.semanticId);
                        if (cd == null)
                            continue;
                        if (cmp.Index.ContainsKey(cd))
                            continue;
                        cmp.Index[cd] = nr++;
                    }
                return cmp;
            }

            // Validation

            public AasValidationRecordList ValidateAll()
            {
                // collect results
                var results = new AasValidationRecordList();

                // all entities
                foreach (var rf in this.FindAllReferable())
                    rf.Validate(results);

                // give back
                return results;
            }

            public int AutoFix(IEnumerable<AasValidationRecord> records)
            {
                // access
                if (records == null)
                    return -1;

                // collect Referables (expensive safety measure)
                var allowedReferables = this.FindAllReferable().ToList();

                // go thru records
                int res = 0;
                foreach (var rec in records)
                {
                    // access 
                    if (rec == null || rec.Fix == null || rec.Source == null)
                        continue;

                    // minimal safety measure
                    if (!allowedReferables.Contains(rec.Source))
                        continue;

                    // apply fix
                    res++;
                    try
                    {
                        rec.Fix.Invoke();
                    }
                    catch
                    {
                        res--;
                    }
                }

                // return number of applied fixes
                return res;
            }

            public void RecurseOnReferables(
                object state, Func<object, ListOfReferable, Referable, bool> lambda, bool includeThis = false)
            {
                // includeThis does not make sense, as no Referable
                // just use the others
                foreach (var idf in this.FindAllReferable(onlyIdentifiables: true))
                    idf?.RecurseOnReferables(state, lambda, includeThis);
            }
        }

        //
        // Submodel + Submodel elements
        //

        public class Qualifier : IAasElement, IGetSemanticId
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // member
            // from hasSemantics:
            [XmlElement(ElementName = "semanticId")]
            public SemanticId semanticId = null;
            public SemanticId GetSemanticId() { return semanticId; }

            // this class
            // TODO (Michael Hoffmeister, 2020-08-01): check, if Json has Qualifiers or not

            [MetaModelName("Qualifier.type")]
            [TextSearchable]
            [CountForHash]
            public string type = "";

            [MetaModelName("Qualifier.valueType")]
            [TextSearchable]
            [CountForHash]
            public string valueType = "";

            [CountForHash]
            public GlobalReference valueId = null;

            [MetaModelName("Qualifier.value")]
            [TextSearchable]
            [CountForHash]
            public string value = null;

            // dead-csharp off
            // Remark: due to publication of v2.0.1, the order of elements has changed!!!
            // from hasSemantics:
            // [XmlElement(ElementName = "semanticId")]
            // [JsonIgnore]
            // public SemanticId semanticId = null;
            // dead-csharp on

            // constructors

            public Qualifier() { }

            public Qualifier(Qualifier src)
            {
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                this.type = src.type;
                this.value = src.value;
                if (src.valueId != null)
                    this.valueId = new GlobalReference(src.valueId);
            }

#if !DoNotUseAasxCompatibilityModels
            public Qualifier(AasxCompatibilityModels.AdminShellV10.Qualifier src)
            {
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                this.type = src.qualifierType;
                this.value = src.qualifierValue;
                if (src.qualifierValueId != null)
                    this.valueId = new GlobalReference(src.qualifierValueId);
            }

            public Qualifier(AasxCompatibilityModels.AdminShellV20.Qualifier src)
            {
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                this.type = src.type;
                this.value = src.value;
                if (src.valueId != null)
                    this.valueId = new GlobalReference(src.valueId);
            }
#endif

            public AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Qualifier", "Qfr");
            }

            public string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
            }

            // ReSharper disable MethodOverloadWithOptionalParameter .. this seems to work, anyhow
            // ReSharper disable RedundantArgumentDefaultValue
            public string ToString(int format = 0, string delimiter = ",")
            {
                var res = "" + type;
                if (res == "")
                    res += "" + semanticId?.ToString(format, delimiter);

                if (value != null)
                    res += " = " + value;
                else if (valueId != null)
                    res += " = " + valueId?.ToString(format, delimiter);

                return res;
            }

            public override string ToString()
            {
                return this.ToString(0);
            }
            // ReSharper enable MethodOverloadWithOptionalParameter
            // ReSharper enable RedundantArgumentDefaultValue

            public static Qualifier Parse(string input)
            {
                var m = Regex.Match(input, @"\s*([^,]*)(,[^=]+){0,1}\s*=\s*([^,]*)(,.+){0,1}\s*");
                if (!m.Success)
                    return null;

                return new Qualifier()
                {
                    type = m.Groups[1].ToString().Trim(),
                    semanticId = SemanticId.Parse(m.Groups[1].ToString().Trim()),
                    value = m.Groups[3].ToString().Trim(),
                    valueId = GlobalReference.Parse(m.Groups[1].ToString().Trim())
                };
            }
        }

        /// <summary>
        ///  This class holds some convenience functions for Qualifiers
        /// </summary>
        public class QualifierCollection : List<Qualifier>
        {
            public QualifierCollection()
            {

            }

#if !DoNotUseAasxCompatibilityModels
            public QualifierCollection(
                List<AasxCompatibilityModels.AdminShellV10.Qualifier> src, bool shallowCopy = false)

            {
                if (src != null && src.Count != 0)
                {
                    foreach (var q in src)
                    {
                        this.Add(new Qualifier(q));
                    }
                }
            }
#endif

            /// <summary>
            /// Add qualifier. If null, do nothing
            /// </summary>
            public new void Add(Qualifier q)
            {
                if (q == null)
                    return;
                base.Add(q);
            }

            public Qualifier FindType(string type)
            {
                if (type == null)
                    return null;
                foreach (var q in this)
                    if (q != null && q.type != null && q.type.Trim() == type.Trim())
                        return q;
                return null;
            }

            public Qualifier FindSemanticId(SemanticId semId)
            {
                if (semId == null)
                    return null;
                foreach (var q in this)
                    if (q != null && q.semanticId != null && q.semanticId.Matches(semId))
                        return q;
                return null;
            }

            // ReSharper disable MethodOverloadWithOptionalParameter .. this seems to work, anyhow
            // ReSharper disable RedundantArgumentDefaultValue
            public string ToString(int format = 0, string delimiter = ";", string referencesDelimiter = ",")
            {
                var res = "";
                foreach (var q in this)
                {
                    if (res != "")
                        res += delimiter;
                    res += q.ToString(format, referencesDelimiter);
                }
                return res;
            }

            public override string ToString()
            {
                return this.ToString(0);
            }
            // ReSharper enable MethodOverloadWithOptionalParameter
            // ReSharper enable RedundantArgumentDefaultValue

            // for convenience methods of Submodel, SubmodelElement

            public static void AddQualifier(
                ref QualifierCollection qualifiers,
                string qualifierType = null, string qualifierValue = null, KeyList semanticKeys = null,
                GlobalReference qualifierValueId = null)
            {
                if (qualifiers == null)
                    qualifiers = new QualifierCollection();
                var q = new Qualifier()
                {
                    type = qualifierType,
                    value = qualifierValue,
                    valueId = qualifierValueId,
                };
                if (semanticKeys != null)
                    q.semanticId = SemanticId.CreateFromKeys(semanticKeys);
                qualifiers.Add(q);
            }

            public static Qualifier HasQualifierOfType(
                QualifierCollection qualifiers,
                string qualifierType)
            {
                if (qualifiers == null || qualifierType == null)
                    return null;
                foreach (var q in qualifiers)
                    if (q.type.Trim().ToLower() == qualifierType.Trim().ToLower())
                        return q;
                return null;
            }

            public IEnumerable<Qualifier> FindAllQualifierType(string qualifierType)
            {
                if (qualifierType == null)
                    yield break;
                foreach (var q in this)
                    if (q.type.Trim().ToLower() == qualifierType.Trim().ToLower())
                        yield return q;
            }
        }

    }
}