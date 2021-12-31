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
    /// Partial main class of AdminShell: SubmodelElements
    /// </summary>
    public partial class AdminShellV30
    {
        //
        // AAS
        //

        public class AdministrationShell : Identifiable, IFindAllReferences
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // from hasDataSpecification:
            [XmlElement(ElementName = "hasDataSpecification")]
            public HasDataSpecification hasDataSpecification = null;

            // from this very class
            public AssetAdministrationShellRef derivedFrom = null;

            [JsonProperty(PropertyName = "asset")]
            public AssetRef assetRef = new AssetRef();

            [JsonProperty(PropertyName = "submodels")]
            [SkipForSearch]
            public List<SubmodelRef> submodelRefs = new List<SubmodelRef>();

            [JsonIgnore]
            public Views views = null;
            [XmlIgnore]
            [JsonProperty(PropertyName = "views")]
            public View[] JsonViews
            {
                get { return views?.views.ToArray(); }
                set { views = Views.CreateOrSetInnerViews(views, value); }
            }

            [JsonProperty(PropertyName = "conceptDictionaries")]
            public List<ConceptDictionary> conceptDictionaries = null;

            // constructors

            public AdministrationShell() { }

            public AdministrationShell(string idShort) : base(idShort) { }

            public AdministrationShell(AdministrationShell src)
                : base(src)
            {
                if (src != null)
                {
                    if (src.hasDataSpecification != null)
                        this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);

                    if (src.derivedFrom != null)
                        this.derivedFrom = new AssetAdministrationShellRef(src.derivedFrom);

                    if (src.assetRef != null)
                        this.assetRef = new AssetRef(src.assetRef);

                    if (src.submodelRefs != null)
                        foreach (var smr in src.submodelRefs)
                            this.submodelRefs.Add(new SubmodelRef(smr));

                    if (src.views != null)
                        this.views = new Views(src.views);

                    if (src.conceptDictionaries != null)
                    {
                        this.conceptDictionaries = new List<ConceptDictionary>();
                        foreach (var cdd in src.conceptDictionaries)
                            this.conceptDictionaries.Add(new ConceptDictionary(cdd));
                    }
                }
            }

#if !DoNotUseAasxCompatibilityModels
            public AdministrationShell(AasxCompatibilityModels.AdminShellV10.AdministrationShell src)
                : base(src)
            {
                if (src.hasDataSpecification != null)
                    this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);

                if (src.derivedFrom != null)
                    this.derivedFrom = new AssetAdministrationShellRef(src.derivedFrom);

                if (src.assetRef != null)
                    this.assetRef = new AssetRef(src.assetRef);

                if (src.submodelRefs != null)
                    foreach (var smr in src.submodelRefs)
                        this.submodelRefs.Add(new SubmodelRef(smr));

                if (src.views != null)
                    this.views = new Views(src.views);

                if (src.conceptDictionaries != null)
                {
                    this.conceptDictionaries = new List<ConceptDictionary>();
                    foreach (var cdd in src.conceptDictionaries)
                        this.conceptDictionaries.Add(new ConceptDictionary(cdd));
                }
            }

            public AdministrationShell(AasxCompatibilityModels.AdminShellV20.AdministrationShell src)
                : base(src)
            {
                if (src != null)
                {
                    if (src.hasDataSpecification != null)
                        this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);

                    if (src.derivedFrom != null)
                        this.derivedFrom = new AssetAdministrationShellRef(src.derivedFrom);

                    if (src.assetRef != null)
                        this.assetRef = new AssetRef(src.assetRef);

                    if (src.submodelRefs != null)
                        foreach (var smr in src.submodelRefs)
                            this.submodelRefs.Add(new SubmodelRef(smr));

                    if (src.views != null)
                        this.views = new Views(src.views);

                    if (src.conceptDictionaries != null)
                    {
                        this.conceptDictionaries = new List<ConceptDictionary>();
                        foreach (var cdd in src.conceptDictionaries)
                            this.conceptDictionaries.Add(new ConceptDictionary(cdd));
                    }
                }
            }
#endif

            public static AdministrationShell CreateNew(
                string idShort, string idType, string id, string version = null, string revision = null)
            {
                var s = new AdministrationShell();
                s.idShort = idShort;
                if (version != null)
                    s.SetAdminstration(version, revision);
                s.id.value = id;
                return (s);
            }

            // add

            public void AddView(View v)
            {
                if (views == null)
                    views = new Views();
                views.views.Add(v);
            }

            public void AddConceptDictionary(ConceptDictionary d)
            {
                if (conceptDictionaries == null)
                    conceptDictionaries = new List<ConceptDictionary>();
                conceptDictionaries.Add(d);
            }

            public void AddDataSpecification(Key k)
            {
                if (hasDataSpecification == null)
                    hasDataSpecification = new HasDataSpecification();
                var r = new Reference();
                r.Keys.Add(k);
                hasDataSpecification.Add(new EmbeddedDataSpecification(r));
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("AssetAdministrationShell", "AAS");
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", idShort, "\"AAS\"");
                if (administration != null)
                    caption += "V" + administration.version + "." + administration.revision;

                var info = "";
                if (id != null)
                    info = $"[{id.value}]";
                return Tuple.Create(caption, info);
            }

            public override string ToString()
            {
                var ci = ToCaptionInfo();
                return string.Format("{0}{1}", ci.Item1, (ci.Item2 != "") ? " / " + ci.Item2 : "");
            }

            public SubmodelRef FindSubmodelRef(Identifier refid)
            {
                if (this.submodelRefs == null)
                    return null;
                foreach (var r in this.submodelRefs)
                    if (r.Matches(refid))
                        return r;
                return null;
            }

            public bool HasSubmodelRef(SubmodelRef newref)
            {
                // check, if existing
                if (this.submodelRefs == null)
                    return false;
                var found = false;
                foreach (var r in this.submodelRefs)
                    if (r.Matches(newref))
                        found = true;

                return found;
            }

            public void AddSubmodelRef(SubmodelRef newref)
            {
                if (this.submodelRefs == null)
                    this.submodelRefs = new List<SubmodelRef>();
                this.submodelRefs.Add(newref);
            }

            public IEnumerable<LocatedReference> FindAllReferences()
            {
                // Asset
                if (this.assetRef != null)
                    yield return new LocatedReference(this, this.assetRef);

                // Submodel references
                if (this.submodelRefs != null)
                    foreach (var r in this.submodelRefs)
                        yield return new LocatedReference(this, r);

                // Views
                if (this.views?.views != null)
                    foreach (var vw in this.views.views)
                        if (vw?.containedElements?.reference != null)
                            foreach (var r in vw.containedElements.reference)
                                yield return new LocatedReference(this, r);
            }
        }

        public class ListOfAdministrationShells : List<AdministrationShell>, IAasElement
        {
            // self decscription

            public AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("AssetAdministrationShells", "AASs");
            }

            public string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
            }
        }

        //
        // Asset
        //

        public class Asset : Identifiable
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // from hasDataSpecification:
            [XmlElement(ElementName = "hasDataSpecification")]
            public HasDataSpecification hasDataSpecification = null;

            // from this very class
            [XmlElement(ElementName = "assetIdentificationModelRef")]
            public SubmodelRef assetIdentificationModelRef = null;

            [XmlElement(ElementName = "billOfMaterialRef")]
            public SubmodelRef billOfMaterialRef = null;

            // from HasKind
            [XmlElement(ElementName = "kind")]
            [JsonIgnore]
            public AssetKind kind = new AssetKind();
            [XmlIgnore]
            [JsonProperty(PropertyName = "kind")]
            public string JsonKind
            {
                get
                {
                    if (kind == null)
                        return null;
                    return kind.kind;
                }
                set
                {
                    if (kind == null)
                        kind = new AssetKind();
                    kind.kind = value;
                }
            }

            // constructors

            public Asset() { }

            public Asset(string idShort) : base(idShort) { }

            public Asset(Asset src)
                : base(src)
            {
                if (src != null)
                {
                    if (src.hasDataSpecification != null)
                        this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                    if (src.kind != null)
                        this.kind = new AssetKind(src.kind);
                    if (src.assetIdentificationModelRef != null)
                        this.assetIdentificationModelRef = new SubmodelRef(src.assetIdentificationModelRef);
                }
            }

#if !DoNotUseAasxCompatibilityModels
            public Asset(AasxCompatibilityModels.AdminShellV10.Asset src)
                : base(src)
            {
                if (src != null)
                {
                    if (src.hasDataSpecification != null)
                        this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                    if (src.kind != null)
                        this.kind = new AssetKind(src.kind);
                    if (src.assetIdentificationModelRef != null)
                        this.assetIdentificationModelRef = new SubmodelRef(src.assetIdentificationModelRef);
                }
            }

            public Asset(AasxCompatibilityModels.AdminShellV20.Asset src)
                : base(src)
            {
                if (src != null)
                {
                    if (src.hasDataSpecification != null)
                        this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                    if (src.kind != null)
                        this.kind = new AssetKind(src.kind);
                    if (src.assetIdentificationModelRef != null)
                        this.assetIdentificationModelRef = new SubmodelRef(src.assetIdentificationModelRef);
                }
            }
#endif

            // Getter & setters

            public AssetRef GetAssetReference()
            {
                var r = new AssetRef();
                r.Keys.Add(
                    Key.CreateNew(
                        this.GetElementName(), true, "", this.id.value));
                return r;
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Asset", "Asset");
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
                if (administration != null)
                    caption += "V" + administration.version + "." + administration.revision;

                var info = "";
                if (id != null)
                    info = $"[{id.value}]";
                return Tuple.Create(caption, info);
            }

            public override string ToString()
            {
                var ci = ToCaptionInfo();
                return string.Format("{0}{1}", ci.Item1, (ci.Item2 != "") ? " / " + ci.Item2 : "");
            }

            public IEnumerable<Reference> FindAllReferences()
            {
                if (this.assetIdentificationModelRef != null)
                    yield return this.assetIdentificationModelRef;
                if (this.billOfMaterialRef != null)
                    yield return this.billOfMaterialRef;
            }
        }

        public class ListOfAssets : List<Asset>, IAasElement
        {
            // self decscription

            public AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Assets", "Assets");
            }

            public string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
            }

        }

        //
        // ConceptDescriptions
        //

        /// <summary>
        /// Multiple lang string for the IEC61360 namespace
        /// </summary>
        public class LangStringSetIEC61360 : ListOfLangStr
        {

            // getters / setters

            [XmlIgnore]
            [JsonIgnore]
            public bool IsEmpty { get { return this.Count < 1; } }

            // constructors

            public LangStringSetIEC61360() { }

            public LangStringSetIEC61360(ListOfLangStr lol) : base(lol) { }

            public LangStringSetIEC61360(LangStringSetIEC61360 src)
            {
                foreach (var ls in src)
                    this.Add(new LangStr(ls));
            }

#if !DoNotUseAasxCompatibilityModels
            public LangStringSetIEC61360(AasxCompatibilityModels.AdminShellV10.LangStringIEC61360 src)
            {
                if (src.langString != null)
                    foreach (var ls in src.langString)
                        this.Add(new LangStr(ls));
            }

            public LangStringSetIEC61360(AasxCompatibilityModels.AdminShellV20.LangStringSetIEC61360 src)
            {
                foreach (var ls in src)
                    this.Add(new LangStr(ls));
            }
#endif
            public LangStringSetIEC61360(string lang, string str)
            {
                if (str == null || str.Trim() == "")
                    return;
                this.Add(new LangStr(lang, str));
            }

            // converter

            public static LangStringSetIEC61360 CreateFrom(List<LangStr> src)
            {
                var res = new LangStringSetIEC61360();
                if (src != null)
                    foreach (var ls in src)
                        res.Add(new LangStr(ls));
                return res;
            }

        }

        public class UnitId
        {

            // members

            [XmlIgnore]
            [JsonIgnore]
            public KeyList keys = new KeyList();

            // getter / setters

            [XmlArray("keys")]
            [XmlArrayItem("key")]
            [JsonIgnore]
            public KeyList Keys { get { return keys; } }
            [XmlIgnore]
            [JsonProperty(PropertyName = "keys")]
            public KeyList JsonKeys
            {
                get
                {
                    keys?.NumberIndices();
                    return keys;
                }
            }

            [XmlIgnore]
            [JsonIgnore]
            public bool IsEmpty { get { return keys == null || keys.IsEmpty; } }
            [XmlIgnore]
            [JsonIgnore]
            public int Count { get { if (keys == null) return 0; return keys.Count; } }
            [XmlIgnore]
            [JsonIgnore]
            public Key this[int index] { get { return keys[index]; } }

            // constructors / creators

            public UnitId() { }

            public UnitId(UnitId src)
            {
                if (src.keys != null)
                    foreach (var k in src.Keys)
                        this.keys.Add(new Key(k));
            }

#if !DoNotUseAasxCompatibilityModels
            public UnitId(AasxCompatibilityModels.AdminShellV10.UnitId src)
            {
                if (src.keys != null)
                    foreach (var k in src.Keys)
                        this.keys.Add(new Key(k));
            }

            public UnitId(AasxCompatibilityModels.AdminShellV20.UnitId src)
            {
                if (src.keys != null)
                    foreach (var k in src.Keys)
                        this.keys.Add(new Key(k));
            }
#endif

            public static UnitId CreateNew(string type, bool local, string idType, string value)
            {
                var u = new UnitId();
                u.keys.Add(Key.CreateNew(type, local, idType, value));
                return u;
            }

            public static UnitId CreateNew(Reference src)
            {
                if (src == null)
                    return null;
                var res = new UnitId();
                if (src.Keys != null)
                    foreach (var k in src.Keys)
                        res.keys.Add(k);
                return res;
            }
        }

        [XmlRoot(Namespace = "http://www.admin-shell.io/IEC61360/2/0")]
        public class DataSpecificationIEC61360
        {
            // static member
            [XmlIgnore]
            [JsonIgnore]
            public static string[] DataTypeNames = {
                "STRING",
                "STRING_TRANSLATABLE",
                "REAL_MEASURE",
                "REAL_COUNT",
                "REAL_CURRENCY",
                "INTEGER_MEASURE",
                "INTEGER_COUNT",
                "INTEGER_CURRENCY",
                "BOOLEAN",
                "URL",
                "RATIONAL",
                "RATIONAL_MEASURE",
                "TIME",
                "TIMESTAMP",
                "DATE" };

            // members
            // TODO (MIHO, 2020-08-27): According to spec, cardinality is [1..1][1..n]
            // these cardinalities are NOT MAINTAINED in ANY WAY by the system
            public LangStringSetIEC61360 preferredName = new LangStringSetIEC61360();

            // TODO (MIHO, 2020-08-27): According to spec, cardinality is [0..1][1..n]
            // these cardinalities are NOT MAINTAINED in ANY WAY by the system
            public LangStringSetIEC61360 shortName = null;

            [MetaModelName("DataSpecificationIEC61360.unit")]
            [TextSearchable]
            [CountForHash]
            public string unit = "";

            public UnitId unitId = null;

            [MetaModelName("DataSpecificationIEC61360.valueFormat")]
            [TextSearchable]
            [CountForHash]
            public string valueFormat = null;

            [MetaModelName("DataSpecificationIEC61360.sourceOfDefinition")]
            [TextSearchable]
            [CountForHash]
            public string sourceOfDefinition = null;

            [MetaModelName("DataSpecificationIEC61360.symbol")]
            [TextSearchable]
            [CountForHash]
            public string symbol = null;

            [MetaModelName("DataSpecificationIEC61360.dataType")]
            [TextSearchable]
            [CountForHash]
            public string dataType = "";

            // TODO (MIHO, 2020-08-27): According to spec, cardinality is [0..1][1..n]
            // these cardinalities are NOT MAINTAINED in ANY WAY by the system
            public LangStringSetIEC61360 definition = null;

            // getter / setters

            // constructors

            public DataSpecificationIEC61360() { }

            public DataSpecificationIEC61360(DataSpecificationIEC61360 src)
            {
                if (src.preferredName != null)
                    this.preferredName = new LangStringSetIEC61360(src.preferredName);
                this.shortName = src.shortName;
                this.unit = src.unit;
                if (src.unitId != null)
                    this.unitId = new UnitId(src.unitId);
                this.valueFormat = src.valueFormat;
                this.sourceOfDefinition = src.sourceOfDefinition;
                this.symbol = src.symbol;
                this.dataType = src.dataType;
                if (src.definition != null)
                    this.definition = new LangStringSetIEC61360(src.definition);
            }

#if !DoNotUseAasxCompatibilityModels
            public DataSpecificationIEC61360(AasxCompatibilityModels.AdminShellV10.DataSpecificationIEC61360 src)
            {
                if (src.preferredName != null)
                    this.preferredName = new LangStringSetIEC61360(src.preferredName);
                this.shortName = new LangStringSetIEC61360("EN?", src.shortName);
                this.unit = src.unit;
                if (src.unitId != null)
                    this.unitId = new UnitId(src.unitId);
                this.valueFormat = src.valueFormat;
                if (src.sourceOfDefinition != null && src.sourceOfDefinition.Count > 0)
                    this.sourceOfDefinition = src.sourceOfDefinition[0].str;
                this.symbol = src.symbol;
                this.dataType = src.dataType;
                if (src.definition != null)
                    this.definition = new LangStringSetIEC61360(src.definition);
            }

            public DataSpecificationIEC61360(AasxCompatibilityModels.AdminShellV20.DataSpecificationIEC61360 src)
            {
                if (src.preferredName != null)
                    this.preferredName = new LangStringSetIEC61360(src.preferredName);
                this.shortName = new LangStringSetIEC61360(src.shortName);
                this.unit = src.unit;
                if (src.unitId != null)
                    this.unitId = new UnitId(src.unitId);
                this.valueFormat = src.valueFormat;
                this.sourceOfDefinition = src.sourceOfDefinition;
                this.symbol = src.symbol;
                this.dataType = src.dataType;
                if (src.definition != null)
                    this.definition = new LangStringSetIEC61360(src.definition);
            }
#endif

            public static DataSpecificationIEC61360 CreateNew(
                string[] preferredName = null,
                string shortName = "",
                string unit = "",
                UnitId unitId = null,
                string valueFormat = null,
                string sourceOfDefinition = null,
                string symbol = null,
                string dataType = "",
                string[] definition = null
            )
            {
                var d = new DataSpecificationIEC61360();
                if (preferredName != null)
                {
                    d.preferredName = new LangStringSetIEC61360(LangStr.CreateManyFromStringArray(preferredName));
                }
                d.shortName = new LangStringSetIEC61360("EN?", shortName);
                d.unit = unit;
                d.unitId = unitId;
                d.valueFormat = valueFormat;
                d.sourceOfDefinition = sourceOfDefinition;
                d.symbol = symbol;
                d.dataType = dataType;
                if (definition != null)
                {
                    if (d.definition == null)
                        d.definition = new LangStringSetIEC61360();
                    d.definition = new LangStringSetIEC61360(LangStr.CreateManyFromStringArray(definition));
                }
                return (d);
            }

            // "constants"

            public static Key GetKey()
            {
                return Key.CreateNew(
                            "GlobalReference", false, "IRI",
                            "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/2/0");
            }

            // validation

            public void Validate(AasValidationRecordList results, ConceptDescription cd)
            {
                // access
                if (results == null || cd == null)
                    return;

                // check IEC61360 spec
                if (this.preferredName == null || this.preferredName.Count < 1)
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SchemaViolation, cd,
                        "ConceptDescription: missing preferredName",
                        () =>
                        {
                            this.preferredName = new AdminShell.LangStringSetIEC61360("EN?",
                                AdminShellUtil.EvalToNonEmptyString("{0}", cd.idShort, "UNKNOWN"));
                        }));

                if (this.shortName != null && this.shortName.Count < 1)
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SchemaViolation, cd,
                        "ConceptDescription: existing shortName with missing langString",
                        () =>
                        {
                            this.shortName = null;
                        }));

                if (this.definition != null && this.definition.Count < 1)
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SchemaViolation, cd,
                        "ConceptDescription: existing definition with missing langString",
                        () =>
                        {
                            this.definition = null;
                        }));

                // check data type
                string foundDataType = null;
                if (this.dataType != null)
                    foreach (var dtn in DataTypeNames)
                        if (this.dataType.Trim() == dtn.Trim())
                            foundDataType = this.dataType;
                if (foundDataType == null)
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SchemaViolation, cd,
                        "ConceptDescription: dataType does not match allowed enumeration values",
                        () =>
                        {
                            this.dataType = "STRING";
                        }));
            }
        }

        // ReSharper disable ClassNeverInstantiated.Global .. class is important to show potential for ISO!

        public class DataSpecificationISO99999
        {
        }

        // ReSharper enable ClassNeverInstantiated.Global

        public class DataSpecificationContent
        {

            // members

            public DataSpecificationIEC61360 dataSpecificationIEC61360 = null;
            public DataSpecificationISO99999 dataSpecificationISO99999 = null;

            // constructors

            public DataSpecificationContent() { }

            public DataSpecificationContent(DataSpecificationContent src)
            {
                if (src.dataSpecificationIEC61360 != null)
                    this.dataSpecificationIEC61360 = new DataSpecificationIEC61360(src.dataSpecificationIEC61360);
            }

#if !DoNotUseAasxCompatibilityModels
            public DataSpecificationContent(AasxCompatibilityModels.AdminShellV10.DataSpecificationContent src)
            {
                if (src.dataSpecificationIEC61360 != null)
                    this.dataSpecificationIEC61360 = new DataSpecificationIEC61360(src.dataSpecificationIEC61360);
            }

            public DataSpecificationContent(AasxCompatibilityModels.AdminShellV20.DataSpecificationContent src)
            {
                if (src.dataSpecificationIEC61360 != null)
                    this.dataSpecificationIEC61360 = new DataSpecificationIEC61360(src.dataSpecificationIEC61360);
            }
#endif
        }

        public class EmbeddedDataSpecification
        {
            // members

            [JsonIgnore]
            public DataSpecificationContent dataSpecificationContent = null;

            [XmlIgnore]
            [JsonProperty("dataSpecificationContent")]
            public DataSpecificationIEC61360 JsonWrongDataSpec61360
            {
                get { return dataSpecificationContent?.dataSpecificationIEC61360; }
                set
                {
                    if (dataSpecificationContent == null)
                        dataSpecificationContent = new DataSpecificationContent();
                    dataSpecificationContent.dataSpecificationIEC61360 = value;
                }
            }

            public DataSpecificationRef dataSpecification = null;

            // constructors

            public EmbeddedDataSpecification() { }

            public EmbeddedDataSpecification(
                DataSpecificationRef dataSpecification,
                DataSpecificationContent dataSpecificationContent)
            {
                this.dataSpecification = dataSpecification;
                this.dataSpecificationContent = dataSpecificationContent;
            }

            public EmbeddedDataSpecification(EmbeddedDataSpecification src)
            {
                if (src.dataSpecification != null)
                    this.dataSpecification = new DataSpecificationRef(src.dataSpecification);
                if (src.dataSpecificationContent != null)
                    this.dataSpecificationContent = new DataSpecificationContent(src.dataSpecificationContent);
            }

            public EmbeddedDataSpecification(Reference src)
            {
                if (src != null)
                    this.dataSpecification = new DataSpecificationRef(src);
            }

#if !DoNotUseAasxCompatibilityModels
            public EmbeddedDataSpecification(AasxCompatibilityModels.AdminShellV10.EmbeddedDataSpecification src)
            {
                if (src.hasDataSpecification != null)
                    this.dataSpecification = new DataSpecificationRef(src.hasDataSpecification);
                if (src.dataSpecificationContent != null)
                    this.dataSpecificationContent = new DataSpecificationContent(src.dataSpecificationContent);
            }

            public EmbeddedDataSpecification(AasxCompatibilityModels.AdminShellV10.Reference src)
            {
                if (src != null)
                    this.dataSpecification = new DataSpecificationRef(src);
            }

            public EmbeddedDataSpecification(AasxCompatibilityModels.AdminShellV20.EmbeddedDataSpecification src)
            {
                if (src.dataSpecification != null)
                    this.dataSpecification = new DataSpecificationRef(src.dataSpecification);
                if (src.dataSpecificationContent != null)
                    this.dataSpecificationContent = new DataSpecificationContent(src.dataSpecificationContent);
            }
#endif

            public static EmbeddedDataSpecification CreateIEC61360WithContent()
            {
                var eds = new EmbeddedDataSpecification(new DataSpecificationRef(), new DataSpecificationContent());

                eds.dataSpecification.Keys.Add(DataSpecificationIEC61360.GetKey());

                eds.dataSpecificationContent.dataSpecificationIEC61360 =
                    AdminShell.DataSpecificationIEC61360.CreateNew();

                return eds;
            }

            public DataSpecificationIEC61360 GetIEC61360()
            {
                return this.dataSpecificationContent?.dataSpecificationIEC61360;
            }
        }

        public class ConceptDescription : Identifiable, System.IDisposable
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // members

            // do this in order to be IDisposable, that is: suitable for (using)
            void System.IDisposable.Dispose() { }
            public void GetData() { }
            // from HasDataSpecification

#if __not_anymore

        [XmlElement(ElementName = "embeddedDataSpecification")]
        [JsonIgnore]
        public EmbeddedDataSpecification embeddedDataSpecification = new EmbeddedDataSpecification();
#else
            // According to Spec V2.0.1, a ConceptDescription might feature alos multiple data specifications
            /* TODO (MIHO, 2020-08-30): align wording of the member ("embeddedDataSpecification") with the 
                * wording of the other entities ("hasDataSpecification") */
            [XmlElement(ElementName = "embeddedDataSpecification")]
            [JsonIgnore]
            public HasDataSpecification embeddedDataSpecification = null;
#endif

            [XmlIgnore]
            [JsonProperty(PropertyName = "embeddedDataSpecifications")]
            public EmbeddedDataSpecification[] JsonEmbeddedDataSpecifications
            {
                get
                {
                    return this.embeddedDataSpecification?.ToArray();
                }
                set
                {
                    embeddedDataSpecification = new HasDataSpecification(value);
                }
            }

            // old

            // this class
            [XmlIgnore]
            private List<Reference> isCaseOf = null;

            // getter / setter

            [XmlElement(ElementName = "isCaseOf")]
            [JsonProperty(PropertyName = "isCaseOf")]
            public List<Reference> IsCaseOf
            {
                get { return isCaseOf; }
                set { isCaseOf = value; }
            }

            // constructors / creators

            public ConceptDescription() : base() { }

            public ConceptDescription(ConceptDescription src)
                : base(src)
            {
                if (src.embeddedDataSpecification != null)
                    this.embeddedDataSpecification = new HasDataSpecification(src.embeddedDataSpecification);
                if (src.isCaseOf != null)
                    foreach (var ico in src.isCaseOf)
                    {
                        if (this.isCaseOf == null)
                            this.isCaseOf = new List<Reference>();
                        this.isCaseOf.Add(new Reference(ico));
                    }
            }

#if !DoNotUseAasxCompatibilityModels
            public ConceptDescription(AasxCompatibilityModels.AdminShellV10.ConceptDescription src)
                : base(src)
            {
                if (src.embeddedDataSpecification != null)
                {
                    this.embeddedDataSpecification = new HasDataSpecification();
                    this.embeddedDataSpecification.Add(new EmbeddedDataSpecification(src.embeddedDataSpecification));
                }
                if (src.IsCaseOf != null)
                    foreach (var ico in src.IsCaseOf)
                    {
                        if (this.isCaseOf == null)
                            this.isCaseOf = new List<Reference>();
                        this.isCaseOf.Add(new Reference(ico));
                    }
            }

            public ConceptDescription(AasxCompatibilityModels.AdminShellV20.ConceptDescription src)
                : base(src)
            {
                if (src.embeddedDataSpecification != null)
                    this.embeddedDataSpecification = new HasDataSpecification(src.embeddedDataSpecification);
                if (src.IsCaseOf != null)
                    foreach (var ico in src.IsCaseOf)
                    {
                        if (this.isCaseOf == null)
                            this.isCaseOf = new List<Reference>();
                        this.isCaseOf.Add(new Reference(ico));
                    }
            }
#endif

            public static ConceptDescription CreateNew(
                string idShort, string idType, string id, string version = null, string revision = null)
            {
                var cd = new ConceptDescription();
                cd.idShort = idShort;
                cd.id.value = id;
                if (version != null)
                {
                    if (cd.administration == null)
                        cd.administration = new Administration();
                    cd.administration.version = version;
                    cd.administration.revision = revision;
                }
                return (cd);
            }

            public Key GetSingleKey()
            {
                return Key.CreateNew(this.GetElementName(), true, "", this.id.value);
            }

            public ConceptDescriptionRef GetCdReference()
            {
                var r = new ConceptDescriptionRef();
                r.Keys.Add(
                    Key.CreateNew(
                        this.GetElementName(), true, "", this.id.value));
                return r;
            }

            public void SetIEC61360Spec(
                string[] preferredNames = null,
                string shortName = "",
                string unit = "",
                UnitId unitId = null,
                string valueFormat = null,
                string sourceOfDefinition = null,
                string symbol = null,
                string dataType = "",
                string[] definition = null
            )
            {
                var eds = new EmbeddedDataSpecification(new DataSpecificationRef(), new DataSpecificationContent());
                eds.dataSpecification.Keys.Add(
                    DataSpecificationIEC61360.GetKey());
                eds.dataSpecificationContent.dataSpecificationIEC61360 =
                    AdminShell.DataSpecificationIEC61360.CreateNew(
                        preferredNames, shortName, unit, unitId, valueFormat, sourceOfDefinition, symbol,
                        dataType, definition);

                this.embeddedDataSpecification = new HasDataSpecification();
                this.embeddedDataSpecification.Add(eds);

                this.AddIsCaseOf(
                    Reference.CreateNew(new Key("ConceptDescription", this.id.value)));
            }

            public DataSpecificationIEC61360 GetIEC61360()
            {
                return this.embeddedDataSpecification?.IEC61360Content;
            }

            // as experimental approach, forward the IEC getter/sett of hasDataSpec directly

            [XmlIgnore]
            [JsonIgnore]
            public EmbeddedDataSpecification IEC61360DataSpec
            {
                get
                {
                    return this.embeddedDataSpecification?.IEC61360;
                }
                set
                {
                    // add embeddedDataSpecification first?
                    if (this.embeddedDataSpecification == null)
                        this.embeddedDataSpecification = new HasDataSpecification();
                    this.embeddedDataSpecification.IEC61360 = value;
                }
            }

            [XmlIgnore]
            [JsonIgnore]
            public DataSpecificationIEC61360 IEC61360Content
            {
                get
                {
                    return this.embeddedDataSpecification?.IEC61360Content;
                }
                set
                {
                    // add embeddedDataSpecification first?
                    if (this.embeddedDataSpecification == null)
                        this.embeddedDataSpecification = new HasDataSpecification();

                    // check, if e IEC61360 can be found
                    var eds = this.embeddedDataSpecification.IEC61360;

                    // if already there, update
                    if (eds != null)
                    {
                        eds.dataSpecificationContent = new DataSpecificationContent();
                        eds.dataSpecificationContent.dataSpecificationIEC61360 = value;
                        return;
                    }

                    // no: add a full record
                    eds = EmbeddedDataSpecification.CreateIEC61360WithContent();
                    eds.dataSpecificationContent.dataSpecificationIEC61360 = value;
                    this.embeddedDataSpecification.Add(eds);
                }
            }

            public DataSpecificationIEC61360 CreateDataSpecWithContentIec61360()
            {
                var eds = AdminShell.EmbeddedDataSpecification.CreateIEC61360WithContent();
                if (this.embeddedDataSpecification == null)
                    this.embeddedDataSpecification = new HasDataSpecification();
                this.embeddedDataSpecification.Add(eds);
                return eds.dataSpecificationContent?.dataSpecificationIEC61360;
            }

            public string GetDefaultPreferredName(string defaultLang = null)
            {
                return "" +
                    GetIEC61360()?
                        .preferredName?.GetDefaultStr(defaultLang);
            }

            public string GetDefaultShortName(string defaultLang = null)
            {
                return "" +
                    GetIEC61360()?
                        .shortName?.GetDefaultStr(defaultLang);
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("ConceptDescription", "CD");
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = "";
                if (this.idShort != null && this.idShort.Trim() != "")
                    caption = $"\"{this.idShort.Trim()}\"";
                if (this.id != null)
                    caption = (caption + " " + this.id).Trim();

                var info = "" + GetDefaultShortName();

                return Tuple.Create(caption, info);
            }

            public override string ToString()
            {
                var ci = ToCaptionInfo();
                return string.Format("{0}{1}", ci.Item1, (ci.Item2 != "") ? " / " + ci.Item2 : "");
            }

            public void AddIsCaseOf(Reference ico)
            {
                if (isCaseOf == null)
                    isCaseOf = new List<Reference>();
                isCaseOf.Add(ico);
            }

            public static IDisposable CreateNew()
            {
                throw new NotImplementedException();
            }

            // validation

            public override void Validate(AasValidationRecordList results)
            {
                // access
                if (results == null)
                    return;

                // check CD itself
                base.Validate(results);

                // check IEC61360 spec
                var eds61360 = this.IEC61360DataSpec;
                if (eds61360 != null)
                {
                    // check data spec
                    if (eds61360.dataSpecification == null ||
                        !(eds61360.dataSpecification.MatchesExactlyOneKey(DataSpecificationIEC61360.GetKey())))
                        results.Add(new AasValidationRecord(
                            AasValidationSeverity.SpecViolation, this,
                            "HasDataSpecification: data specification content set to IEC61360, but no " +
                            "data specification reference set!",
                            () =>
                            {
                                eds61360.dataSpecification = new DataSpecificationRef(
                                    new Reference(
                                        DataSpecificationIEC61360.GetKey()));
                            }));

                    // validate content
                    if (eds61360.dataSpecificationContent?.dataSpecificationIEC61360 == null)
                    {
                        results.Add(new AasValidationRecord(
                            AasValidationSeverity.SpecViolation, this,
                            "HasDataSpecification: data specification reference set to IEC61360, but no " +
                            "data specification content set!",
                            () =>
                            {
                                eds61360.dataSpecificationContent = new DataSpecificationContent();
                                eds61360.dataSpecificationContent.dataSpecificationIEC61360 =
                                new DataSpecificationIEC61360();
                            }));
                    }
                    else
                    {
                        // validate
                        eds61360.dataSpecificationContent.dataSpecificationIEC61360.Validate(results, this);
                    }
                }
            }

            // more find

            public IEnumerable<Reference> FindAllReferences()
            {
                yield break;
            }
        }

        public class ListOfConceptDescriptions : List<ConceptDescription>, IAasElement
        {
            // finding

            public ConceptDescription Find(ConceptDescriptionRef cdr)
            {
                if (cdr == null)
                    return null;
                return Find(cdr.Keys);
            }

            public ConceptDescription Find(Identifier id)
            {
                var cdr = ConceptDescriptionRef.CreateNew("Conceptdescription", true, "", id.value);
                return Find(cdr);
            }

            public ConceptDescription Find(List<Key> keys)
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
                foreach (var cd in this)
                    if (cd.id.value.ToLower().Trim() == key.value.ToLower().Trim())
                        return cd;
                // uups
                return null;
            }

            // item management

            public ConceptDescription AddIfNew(ConceptDescription cd)
            {
                if (cd == null)
                    return null;

                var exist = this.Find(cd.id);
                if (exist != null)
                    return exist;

                this.Add(cd);
                return cd;
            }

            // self decscription

            public AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("ConceptDescriptions", "CDS");
            }

            public string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
            }

            // sorting


        }

        public class ConceptDictionary : Referable
        {
            [XmlElement(ElementName = "conceptDescriptions")]
            public ConceptDescriptionRefs conceptDescriptionsRefs = null;

            // constructors

            public ConceptDictionary() { }

            public ConceptDictionary(ConceptDictionary src)
            {
                if (src.conceptDescriptionsRefs != null)
                    this.conceptDescriptionsRefs = new ConceptDescriptionRefs(src.conceptDescriptionsRefs);
            }

#if !DoNotUseAasxCompatibilityModels
            public ConceptDictionary(AasxCompatibilityModels.AdminShellV10.ConceptDictionary src)
            {
                if (src.conceptDescriptionsRefs != null)
                    this.conceptDescriptionsRefs = new ConceptDescriptionRefs(src.conceptDescriptionsRefs);
            }

            public ConceptDictionary(AasxCompatibilityModels.AdminShellV20.ConceptDictionary src)
            {
                if (src.conceptDescriptionsRefs != null)
                    this.conceptDescriptionsRefs = new ConceptDescriptionRefs(src.conceptDescriptionsRefs);
            }
#endif

            public static ConceptDictionary CreateNew(string idShort = null)
            {
                var d = new ConceptDictionary();
                if (idShort != null)
                    d.idShort = idShort;
                return (d);
            }

            // add

            public void AddReference(Reference r)
            {
                var cdr = (ConceptDescriptionRef)(ConceptDescriptionRef.CreateNew(r.Keys));
                if (conceptDescriptionsRefs == null)
                    conceptDescriptionsRefs = new ConceptDescriptionRefs();
                conceptDescriptionsRefs.conceptDescriptions.Add(cdr);
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("ConceptDictionary", "CDic");
            }
        }

        //
        // Submodel
        //

        public class Submodel : Identifiable, IManageSubmodelElements,
                            System.IDisposable, IEnumerateChildren, IFindAllReferences,
                            IGetSemanticId, IGetQualifiers
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // members

            // do this in order to be IDisposable, that is: suitable for (using)
            void System.IDisposable.Dispose() { }
            public void GetData() { }

            // from HasKind
            [XmlElement(ElementName = "kind")]
            [JsonIgnore]
            public ModelingKind kind = new ModelingKind();
            [XmlIgnore]
            [JsonProperty(PropertyName = "kind")]
            public string JsonKind
            {
                get
                {
                    if (kind == null)
                        return null;
                    return kind.kind;
                }
                set
                {
                    if (kind == null)
                        kind = new ModelingKind();
                    kind.kind = value;
                }
            }

            // from hasSemanticId:
            [XmlElement(ElementName = "semanticId")]
            public SemanticId semanticId = new SemanticId();
            public SemanticId GetSemanticId() { return semanticId; }

            // from Qualifiable:
            [XmlArray("qualifier")]
            [XmlArrayItem("qualifier")]
            public QualifierCollection qualifiers = null;
            public QualifierCollection GetQualifiers() => qualifiers;

            // from hasDataSpecification:
            [XmlElement(ElementName = "embeddedDataSpecification")]
            public HasDataSpecification hasDataSpecification = null;

            // from this very class
            [XmlIgnore]
            [JsonIgnore]
            private SubmodelElementWrapperCollection _submodelElements = null;

            [JsonIgnore]
            public SubmodelElementWrapperCollection submodelElements
            {
                get { return _submodelElements; }
                set { _submodelElements = value; _submodelElements.Parent = this; }
            }

            [XmlIgnore]
            [JsonProperty(PropertyName = "submodelElements")]
            public SubmodelElement[] JsonSubmodelElements
            {
                get
                {
                    var res = new ListOfSubmodelElement();
                    if (submodelElements != null)
                        foreach (var smew in submodelElements)
                            res.Add(smew.submodelElement);
                    return res.ToArray();
                }
                set
                {
                    if (value != null)
                    {
                        this.submodelElements = new SubmodelElementWrapperCollection();
                        foreach (var x in value)
                        {
                            var smew = new SubmodelElementWrapper() { submodelElement = x };
                            this.submodelElements.Add(smew);
                        }
                    }
                }
            }

            // getter / setter

            // constructors / creators

            public Submodel() : base() { }

            public Submodel(Submodel src, bool shallowCopy = false)
                : base(src)
            {
                if (src == null)
                    return;
                if (src.hasDataSpecification != null)
                    this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                if (src.kind != null)
                    this.kind = new ModelingKind(src.kind);
                if (!shallowCopy && src.submodelElements != null)
                {
                    if (this.submodelElements == null)
                        this.submodelElements = new SubmodelElementWrapperCollection();
                    foreach (var smw in src.submodelElements)
                        this.submodelElements.Add(new SubmodelElementWrapper(smw.submodelElement, shallowCopy: false));
                }
            }

#if !DoNotUseAasxCompatibilityModels
            public Submodel(AasxCompatibilityModels.AdminShellV10.Submodel src, bool shallowCopy = false)
                : base(src)
            {
                if (src.hasDataSpecification != null)
                    this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                if (src.kind != null)
                    this.kind = new ModelingKind(src.kind);
                if (src.qualifiers != null)
                    this.qualifiers = new QualifierCollection(src.qualifiers);
                if (!shallowCopy && src.submodelElements != null)
                {
                    if (this.submodelElements == null)
                        this.submodelElements = new SubmodelElementWrapperCollection();
                    foreach (var smw in src.submodelElements)
                        this.submodelElements.Add(new SubmodelElementWrapper(smw.submodelElement, shallowCopy: false));
                }
            }

            public Submodel(AasxCompatibilityModels.AdminShellV20.Submodel src, bool shallowCopy = false)
                : base(src)
            {
                if (src == null)
                    return;
                if (src.hasDataSpecification != null)
                    this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                if (src.kind != null)
                    this.kind = new ModelingKind(src.kind);
                if (!shallowCopy && src.submodelElements != null)
                {
                    if (this.submodelElements == null)
                        this.submodelElements = new SubmodelElementWrapperCollection();
                    foreach (var smw in src.submodelElements)
                        this.submodelElements.Add(new SubmodelElementWrapper(smw.submodelElement, shallowCopy: false));
                }
            }
#endif

            public static Submodel CreateNew(string idType, string id, string version = null, string revision = null)
            {
                var s = new Submodel() { id = new Identifier(id) };
                if (version != null)
                {
                    if (s.administration == null)
                        s.administration = new Administration();
                    s.administration.version = version;
                    s.administration.revision = revision;
                }
                return (s);
            }

            [JsonIgnore]
            [XmlIgnore]
            public SubmodelElementWrapperCollection SmeForWrite
            {
                get
                {
                    if (this.submodelElements == null)
                        this.submodelElements = new SubmodelElementWrapperCollection();
                    return this.submodelElements;
                }
            }

            // from IEnumarateChildren
            public IEnumerable<SubmodelElementWrapper> EnumerateChildren()
            {
                if (this.submodelElements != null)
                    foreach (var smw in this.submodelElements)
                        yield return smw;
            }

            public EnumerationPlacmentBase GetChildrenPlacement(SubmodelElement child)
            {
                return null;
            }

            public object AddChild(SubmodelElementWrapper smw, EnumerationPlacmentBase placement = null)
            {
                if (smw == null)
                    return null;
                if (this.submodelElements == null)
                    this.submodelElements = new SubmodelElementWrapperCollection();
                if (smw.submodelElement != null)
                    smw.submodelElement.parent = this;
                this.submodelElements.Add(smw);
                return smw;
            }

            // from IManageSubmodelElements
            public void Add(SubmodelElement sme)
            {
                if (submodelElements == null)
                    submodelElements = new SubmodelElementWrapperCollection();
                var sew = new SubmodelElementWrapper();
                sme.parent = this; // track parent here!
                sew.submodelElement = sme;
                submodelElements.Add(sew);
            }

            public void Insert(int index, SubmodelElement sme)
            {
                if (submodelElements == null)
                    submodelElements = new SubmodelElementWrapperCollection();
                var sew = new SubmodelElementWrapper();
                sme.parent = this; // track parent here!
                sew.submodelElement = sme;
                if (index < 0 || index >= submodelElements.Count)
                    return;
                submodelElements.Insert(index, sew);
            }

            public void Remove(SubmodelElement sme)
            {
                if (submodelElements != null)
                    submodelElements.Remove(sme);
            }

            // further

            public void AddQualifier(
                string qualifierType = null, string qualifierValue = null, KeyList semanticKeys = null,
                Reference qualifierValueId = null)
            {
                QualifierCollection.AddQualifier(
                    ref this.qualifiers, qualifierType, qualifierValue, semanticKeys, qualifierValueId);
            }

            public Qualifier HasQualifierOfType(string qualifierType)
            {
                return QualifierCollection.HasQualifierOfType(this.qualifiers, qualifierType);
            }


            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Submodel", "SM");
            }

            public SubmodelRef GetSubmodelRef()
            {
                SubmodelRef l = new SubmodelRef();
                l.Keys.Add(
                    Key.CreateNew(
                        this.GetElementName(), true, "", this.id.value));
                return l;
            }

            /// <summary>
            ///  If instance, return semanticId as on key.
            ///  If template, return identification as key.
            /// </summary>
            /// <returns></returns>
            public Key GetSemanticKey()
            {
                if (true == this.kind?.IsTemplate)
                    return new Key(this.GetElementName(), this.id?.value);
                else
                    return this.semanticId?.GetAsExactlyOneKey();
            }

            public void AddDataSpecification(Key k)
            {
                if (hasDataSpecification == null)
                    hasDataSpecification = new HasDataSpecification();
                var r = new Reference();
                r.Keys.Add(k);
                hasDataSpecification.Add(new EmbeddedDataSpecification(r));
            }

            public SubmodelElementWrapper FindSubmodelElementWrapper(string idShort)
            {
                if (this.submodelElements == null)
                    return null;
                foreach (var smw in this.submodelElements)
                    if (smw.submodelElement != null)
                        if (smw.submodelElement.idShort.Trim().ToLower() == idShort.Trim().ToLower())
                            return smw;
                return null;
            }

            public IEnumerable<T> FindDeep<T>(Predicate<T> match = null) where T : SubmodelElement
            {
                if (this.submodelElements == null)
                    yield break;
                foreach (var x in this.submodelElements.FindDeep<T>(match))
                    yield return x;
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
                if (administration != null)
                    caption += "V" + administration.version + "." + administration.revision;
                var info = "";
                if (id != null)
                    info = $"[{id.value}]";
                return Tuple.Create(caption, info);
            }

            public override string ToString()
            {
                var ci = ToCaptionInfo();
                return string.Format("{0}{1}", ci.Item1, (ci.Item2 != "") ? " / " + ci.Item2 : "");
            }

            // Recursing

            /// <summary>
            /// Recurses on all Submodel elements of a Submodel or SME, which allows children.
            /// The <c>state</c> object will be passed to the lambda function in order to provide
            /// stateful approaches. 
            /// </summary>
            /// <param name="state">State object to be provided to lambda. Could be <c>null.</c></param>
            /// <param name="lambda">The lambda function as <c>(state, parents, SME)</c>
            /// The lambda shall return <c>TRUE</c> in order to deep into recursion.
            /// </param>
            public void RecurseOnSubmodelElements(
                object state, Func<object, ListOfReferable, SubmodelElement, bool> lambda)
            {
                this.submodelElements?.RecurseOnReferables(state, null, (o, par, rf) =>
                {
                    if (rf is SubmodelElement sme)
                        return lambda(o, par, sme);
                    else
                        return true;
                });
            }

            /// <summary>
            /// Recurses on all Submodel elements of a Submodel or SME, which allows children.
            /// The <c>state</c> object will be passed to the lambda function in order to provide
            /// stateful approaches. Include this element, as well. 
            /// </summary>
            /// <param name="state">State object to be provided to lambda. Could be <c>null.</c></param>
            /// <param name="lambda">The lambda function as <c>(state, parents, SME)</c>
            /// The lambda shall return <c>TRUE</c> in order to deep into recursion.</param>
            /// <param name="includeThis">Include this element as well. <c>parents</c> will then 
            /// include this element as well!</param>
            public override void RecurseOnReferables(
                object state, Func<object, ListOfReferable, Referable, bool> lambda,
                bool includeThis = false)
            {
                var parents = new ListOfReferable();
                if (includeThis)
                {
                    lambda(state, null, this);
                    parents.Add(this);
                }
                this.submodelElements?.RecurseOnReferables(state, parents, lambda);
            }

            // Parents stuff

            public static void SetParentsForSME(Referable parent, SubmodelElement se)
            {
                if (se == null)
                    return;

                se.parent = parent;

                // via interface enumaration
                if (se is IEnumerateChildren)
                {
                    var childs = (se as IEnumerateChildren).EnumerateChildren();
                    if (childs != null)
                        foreach (var c in childs)
                            SetParentsForSME(se, c.submodelElement);
                }
            }

            public void SetAllParents()
            {
                if (this.submodelElements != null)
                    foreach (var sme in this.submodelElements)
                        SetParentsForSME(this, sme.submodelElement);
            }

            public T CreateSMEForCD<T>(ConceptDescription cd, string category = null, string idShort = null,
                string idxTemplate = null, int maxNum = 999, bool addSme = false) where T : SubmodelElement, new()
            {
                if (this.submodelElements == null)
                    this.submodelElements = new SubmodelElementWrapperCollection();
                return this.submodelElements.CreateSMEForCD<T>(cd, category, idShort, idxTemplate, maxNum, addSme);
            }

            public T CreateSMEForIdShort<T>(string idShort, string category = null,
                string idxTemplate = null, int maxNum = 999, bool addSme = false) where T : SubmodelElement, new()
            {
                if (this.submodelElements == null)
                    this.submodelElements = new SubmodelElementWrapperCollection();
                return this.submodelElements.CreateSMEForIdShort<T>(idShort, category, idxTemplate, maxNum, addSme);
            }

            // validation

            public override void Validate(AasValidationRecordList results)
            {
                // access
                if (results == null)
                    return;

                // check
                base.Validate(results);
                ModelingKind.Validate(results, kind, this);
                KeyList.Validate(results, semanticId?.Keys, this);
            }

            // find

            public IEnumerable<LocatedReference> FindAllReferences()
            {
                // not nice: use temp list
                var temp = new List<Reference>();

                // recurse
                this.RecurseOnSubmodelElements(null, (state, parents, sme) =>
                {
                    if (sme is ReferenceElement re)
                        if (re.value != null)
                            temp.Add(re.value);
                    if (sme is RelationshipElement rl)
                    {
                        if (rl.first != null)
                            temp.Add(rl.first);
                        if (rl.second != null)
                            temp.Add(rl.second);
                    }
                    // recurse
                    return true;
                });

                // now, give back
                foreach (var r in temp)
                    yield return new LocatedReference(this, r);
            }
        }

        public class ListOfSubmodels : List<Submodel>, IAasElement
        {
            // self decscription

            public AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Submodels", "SMS");
            }

            public string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
            }
        }

    }
}
