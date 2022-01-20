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

            public AssetInformation assetInformation = null;

            [JsonProperty(PropertyName = "submodels")]
            [SkipForSearch]
            public List<SubmodelRef> submodelRefs = new List<SubmodelRef>();

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

                    if (src.submodelRefs != null)
                        foreach (var smr in src.submodelRefs)
                            this.submodelRefs.Add(new SubmodelRef(smr));
                }
            }

#if !DoNotUseAasxCompatibilityModels
            public AdministrationShell(
                AasxCompatibilityModels.AdminShellV10.AdministrationShell src,
                AasxCompatibilityModels.AdminShellV10.AdministrationShellEnv srcenv)
                : base(src)
            {
                if (src.hasDataSpecification != null)
                    this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);

                if (src.derivedFrom != null)
                    this.derivedFrom = new AssetAdministrationShellRef(src.derivedFrom);

                if (src.submodelRefs != null)
                    foreach (var smr in src.submodelRefs)
                        this.submodelRefs.Add(new SubmodelRef(smr));

                // now locate the Asset in the old environment and set
                var srcasset = srcenv?.FindAsset(src.assetRef);
                if (srcasset != null)
                    assetInformation = new AssetInformation(srcasset);
            }

            public AdministrationShell(AasxCompatibilityModels.AdminShellV20.AdministrationShell src,
                AasxCompatibilityModels.AdminShellV20.AdministrationShellEnv srcenv)
                : base(src)
            {
                if (src != null)
                {
                    if (src.hasDataSpecification != null)
                        this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);

                    if (src.derivedFrom != null)
                        this.derivedFrom = new AssetAdministrationShellRef(src.derivedFrom);

                    if (src.submodelRefs != null)
                        foreach (var smr in src.submodelRefs)
                            this.submodelRefs.Add(new SubmodelRef(smr));

                    // now locate the Asset in the old environment and set
                    var srcasset = srcenv?.FindAsset(src.assetRef);
                    if (srcasset != null)
                    {
                        assetInformation = new AssetInformation(srcasset);

                        this.AddExtension(new Extension()
                        {
                            name = "AAS2.0/MIGRATION",
                            valueType = "application/json",
                            value = JsonConvert.SerializeObject(srcasset, Newtonsoft.Json.Formatting.Indented)
                        });
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

            public void AddDataSpecification(Identifier id)
            {
                if (hasDataSpecification == null)
                    hasDataSpecification = new HasDataSpecification();
                hasDataSpecification.Add(new EmbeddedDataSpecification(new GlobalReference(id)));
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
                // Submodel references
                if (this.submodelRefs != null)
                    foreach (var r in this.submodelRefs)
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

        public class AssetInformation : IAasElement
        {
            // as for V3RC02, Asset in no Referable anymore
            [XmlIgnore]
            [JsonIgnore]
            [SkipForHash] // important to skip, as recursion elsewise will go in cycles!
            [SkipForReflection] // important to skip, as recursion elsewise will go in cycles!
            public IAasElement parent = null;

            // V3RC02: instead of Identification
            public GlobalReference globalAssetId;

            // new in V3RC02
            public ListOfIdentifierKeyValuePair specificAssetId = null;

            // new in V3RC02
            public File defaultThumbnail = null;

            // some fake information
            [XmlIgnore]
            [JsonIgnore]
            public string fakeIdShort => Key.AssetInformation;

            [XmlIgnore]
            [JsonIgnore]
            public Description fakeDescription => null;

            // from HasKind
            [XmlElement(ElementName = "assetKind")]
            [JsonIgnore]
            public AssetKind assetKind = new AssetKind();
            [XmlIgnore]
            [JsonProperty(PropertyName = "assetKind")]
            public string JsonKind
            {
                get
                {
                    if (assetKind == null)
                        return null;
                    return assetKind.kind;
                }
                set
                {
                    if (assetKind == null)
                        assetKind = new AssetKind();
                    assetKind.kind = value;
                }
            }

            // constructors

            public AssetInformation() { }

            public AssetInformation(string fakeIdShort)
            {
                // empty, because V3RC02 does not foresee storage anymore
            }

            public AssetInformation(AssetInformation src)
            {
                if (src == null)
                    return;

                if (src.assetKind != null)
                    assetKind = new AssetKind(src.assetKind);

                if (src.globalAssetId != null)
                    globalAssetId = new GlobalReference();

                if (src.specificAssetId != null)
                    specificAssetId = new ListOfIdentifierKeyValuePair(src.specificAssetId);

                if (src.defaultThumbnail != null)
                    defaultThumbnail = new File(src.defaultThumbnail);
            }

#if !DoNotUseAasxCompatibilityModels
            public AssetInformation(AasxCompatibilityModels.AdminShellV10.Asset src)
            {
                if (src == null)
                    return;

                if (src.kind != null)
                    this.assetKind = new AssetKind(src.kind);

                if (src.identification != null)
                    SetIdentification(src.identification.id);
            }

            public AssetInformation(AasxCompatibilityModels.AdminShellV20.Asset src)
            {
                if (src == null)
                    return;

                if (src.kind != null)
                    this.assetKind = new AssetKind(src.kind);

                if (src.identification != null)
                    SetIdentification(src.identification.id);
            }
#endif

            // Getter & setters

            public AssetRef GetAssetReference() => new AssetRef(globalAssetId);

            public string GetElementName() => Key.AssetInformation;

            public AasElementSelfDescription GetSelfDescription()
                => new AasElementSelfDescription(Key.AssetInformation, "Asset");

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = Key.AssetInformation;
                var info = "" + globalAssetId.ToString(1);
                return Tuple.Create(caption, info);
            }

            public override string ToString()
            {
                var ci = ToCaptionInfo();
                return string.Format("{0}{1}", ci.Item1, (ci.Item2 != "") ? " / " + ci.Item2 : "");
            }

            public IEnumerable<Reference> FindAllReferences()
            {
                yield break;
            }

            public void AddDescription(string lang, string str)
            {
                // empty, because V3RC02 does not foresee storage anymore
            }

            public void SetIdentification(Identifier id)
            {
                if (id == null)
                    return;
                globalAssetId = new GlobalReference(new Identifier(id));
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
                if (src == null)
                    return;
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
                if (src == null)
                    return;
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

        public class UnitId : GlobalReference
        {
            // constructors / creators

            public UnitId() : base() { }
            public UnitId(Identifier id) : base(id) { }
            public UnitId(UnitId src) : base(src) { }

            public UnitId(GlobalReference src) : base(src) { }
            public UnitId(Reference src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
            public UnitId(AasxCompatibilityModels.AdminShellV10.UnitId src) : base(src?.Keys) { }
            public UnitId(AasxCompatibilityModels.AdminShellV20.UnitId src) : base(src?.Keys) { }
#endif
        }

        [XmlRoot(Namespace = "http://www.admin-shell.io/IEC61360/3/0")]
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
                            "GlobalReference",
                            "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/3/0");
            }

            public static Identifier GetIdentifier()
            {
                return new Identifier(
                            "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/3/0");
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

            public EmbeddedDataSpecification(GlobalReference src)
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

                eds.dataSpecification.Value.Add(DataSpecificationIEC61360.GetIdentifier());

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
            private List<ModelReference> isCaseOf = null;

            // getter / setter

            [XmlElement(ElementName = "isCaseOf")]
            [JsonProperty(PropertyName = "isCaseOf")]
            public List<ModelReference> IsCaseOf
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
                            this.isCaseOf = new List<ModelReference>();
                        this.isCaseOf.Add(new ModelReference(ico));
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
                            this.isCaseOf = new List<ModelReference>();
                        this.isCaseOf.Add(new ModelReference(ico));
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
                            this.isCaseOf = new List<ModelReference>();
                        this.isCaseOf.Add(new ModelReference(ico));
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
                return Key.CreateNew(this.GetElementName(), this.id.value);
            }

            public Identifier GetSingleId()
            {
                return new Identifier(this.id.value);
            }

            /// <summary>
            /// In order to be semantically precise, use this id to figure out
            /// the single id zo be put in a semantic id.
            /// </summary>
            public Identifier GetSemanticId()
            {
                return new Identifier(this.id.value);
            }

            public ConceptDescriptionRef GetCdReference()
            {
                var r = new ConceptDescriptionRef();
                r.Keys.Add(
                    Key.CreateNew(this.GetElementName(), this.id.value));
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
                eds.dataSpecification.Value.Add(
                    DataSpecificationIEC61360.GetIdentifier());
                eds.dataSpecificationContent.dataSpecificationIEC61360 =
                    AdminShell.DataSpecificationIEC61360.CreateNew(
                        preferredNames, shortName, unit, unitId, valueFormat, sourceOfDefinition, symbol,
                        dataType, definition);

                this.embeddedDataSpecification = new HasDataSpecification();
                this.embeddedDataSpecification.Add(eds);

                this.AddIsCaseOf(
                    ModelReference.CreateNew(new Key("ConceptDescription", this.id.value)));
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

            public void AddIsCaseOf(ModelReference ico)
            {
                if (isCaseOf == null)
                    isCaseOf = new List<ModelReference>();
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
                        !(eds61360.dataSpecification.MatchesExactlyOneId(DataSpecificationIEC61360.GetIdentifier())))
                        results.Add(new AasValidationRecord(
                            AasValidationSeverity.SpecViolation, this,
                            "HasDataSpecification: data specification content set to IEC61360, but no " +
                            "data specification reference set!",
                            () =>
                            {
                                eds61360.dataSpecification = new DataSpecificationRef(
                                    new GlobalReference(
                                        DataSpecificationIEC61360.GetIdentifier()));
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

            public IEnumerable<ModelReference> FindAllReferences()
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
                var cdr = ConceptDescriptionRef.CreateNew("Conceptdescription", id.value);
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
                GlobalReference qualifierValueId = null)
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
                => new SubmodelRef(GetModelReference());

            /// <summary>
            ///  If instance, return semanticId as one key.
            ///  If template, return identification as key.
            /// </summary>
            public Key GetAutoSingleKey()
            {
                if (true == this.kind?.IsTemplate)
                    return new Key(this.GetElementName(), this.id?.value);
                else
                    return this.semanticId?.GetAsExactlyOneKey();
            }

            /// <summary>
            ///  If instance, return semanticId as one key.
            ///  If template, return identification as key.
            /// </summary>
            public Identifier GetAutoSingleId()
            {
                if (true == this.kind?.IsTemplate)
                    return new Identifier(this.id);
                else
                    return this.semanticId?.GetAsIdentifier(strict: true);
            }

            public void AddDataSpecification(Identifier id)
            {
                if (hasDataSpecification == null)
                    hasDataSpecification = new HasDataSpecification();
                hasDataSpecification.Add(new EmbeddedDataSpecification(new GlobalReference(id)));
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
                ListOfIdentifier.Validate(results, semanticId?.Value, this);
            }

            // find

            public IEnumerable<LocatedReference> FindAllReferences()
            {
                // not nice: use temp list
                var temp = new List<ModelReference>();

                // recurse
                this.RecurseOnSubmodelElements(null, (state, parents, sme) =>
                {
                    if (sme is ModelReferenceElement mre)
                        if (mre.value != null)
                            temp.Add(mre.value);
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
