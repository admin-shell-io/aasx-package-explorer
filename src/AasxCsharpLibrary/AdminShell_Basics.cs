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

        /// <summary>
        /// This class allows to describe further data (in derived classes) when enumerating Children.
        /// </summary>
        public class EnumerationPlacmentBase
        {
        }

        /// <summary>
        /// This interfaces designates enitities, whích can enumerate their children.
        /// An optional placement can be provided (in/ out/ inout, index, ..)
        /// </summary>
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

        //
        // Self description (not in the meta model!)
        //

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

        //
        // Data Specification
        //
        
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

        //
        // Lang Str
        //

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

        //
        // Description
        //

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

        //
        // Kinds
        //

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

        //
        // Extensions attached to each Referable
        //

        /// <summary>
        /// Single extension of an element. 
        /// </summary>
        public class Extension : IGetSemanticId
        {
            // members

            // from hasSemantics:
            [XmlElement(ElementName = "semanticId")]
            public SemanticId semanticId = null;
            public SemanticId GetSemanticId() => semanticId;

            // this class

            /// <summary>
            /// Name of the extension.  The  name of an extension within <c>ListOfExtension</c> 
            /// needs to be unique.
            /// </summary>
            public string name = "";

            /// <summary>
            /// Type of the value of the extension.
            /// </summary>
            public string valueType = "xsd:string";

            /// <summary>
            /// Value of the extension. In meta model this is ValueDataType, but in this SDK
            /// its just a plain string. Appropriate serialization needs to happen.
            /// Note: this string can VERY HUGE, i.e. an XML document on its own.
            /// </summary>
            public string value = "";

            /// <summary>
            /// Reference  to (multiple)  elements  the extension refers to.
            /// </summary>
            public List<ModelReference> refersTo = null;
        }

        /// <summary>
        /// Allows each Referable to hold multiple Extensions.
        /// </summary>
        public class ListOfExtension : List<Extension>
        {
        }

        //
        // hierarchical organized time stamping and transaction approach, coined "DiaryData"
        //

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

            // from HasExtension
            public ListOfExtension extension = null;

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

            public void AddExtension(Extension ext)
            {
                if (ext == null)
                    return;
                if (extension == null)
                    extension = new ListOfExtension();
                extension.Add(ext);
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
       
        //
        // Qualifier
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