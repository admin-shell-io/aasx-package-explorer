using System.Xml;
using System.Runtime;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System;
using System.IO.Packaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Linq;

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).
*/

namespace AdminShellNS
{
    /// <summary>
    /// This empty class derives always from the current version of the Administration Shell class hierarchy.
    /// </summary>
    public class AdminShell : AdminShellV20 { }

    #region AdminShell_V2_0

    /// <summary>
    /// Version of Details of Administration Shell Part 1 V1.0 published Nov/Dec/Jan 2018/19
    /// </summary>
    public class AdminShellV20
    {

        public class Identification
        {

            // members

            [XmlAttribute]
            [CountForHash]
            public string idType = "";

            [XmlText]
            [CountForHash]
            public string id = "";

            // some constants

            public static string IRDI = "IRDI";
            public static string IRI = "IRI";
            public static string IdShort = "IdShort";

            // constructors

            public Identification() { }

            public Identification(Identification src)
            {
                this.idType = src.idType;
                this.id = src.id;
            }

#if UseAasxCompatibilityModels
            public Identification(AasxCompatibilityModels.AdminShellV10.Identification src)
            {
                this.idType = src.idType;
                if (this.idType.Trim().ToLower() == "uri")
                    this.idType = Identification.IRI;
                this.id = src.id;
            }
#endif

            public Identification(string idType, string id)
            {
                this.idType = idType;
                this.id = id;
            }

            public Identification(Key key)
            {
                this.idType = key.idType;
                this.id = key.value;
            }

            // Creator with validation

            public static Identification CreateNew(string idType, string id)
            {
                if (idType == null || id == null)
                    return null;
                var found = false;
                foreach (var x in Key.IdentifierTypeNames)
                    found = found || idType.ToLower().Trim() == x.ToLower().Trim();
                if (!found)
                    return null;
                return new Identification(idType, id);
            }

            // further

            public bool IsEqual(Identification other)
            {
                return (this.idType.Trim().ToLower() == other.idType.Trim().ToLower()
                    && this.id.Trim().ToLower() == other.id.Trim().ToLower());
            }

            public override string ToString()
            {
                return $"[{this.idType}] {this.id}";
            }
        }

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

#if UseAasxCompatibilityModels
            public Administration(AasxCompatibilityModels.AdminShellV10.Administration src)
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

        public class Key
        {
            // Constants

            public enum MatchMode { Strict, Relaxed, Identification };

            // Members

            [MetaModelName("Key.type")]
            [TextSearchable]
            [XmlAttribute]
            [CountForHash]
            public string type = "";

            [XmlAttribute]
            [CountForHash]
            public bool local = false;

            [MetaModelName("Key.idType")]
            [TextSearchable]
            [XmlAttribute]
            [JsonIgnore]
            [CountForHash]
            public string idType = "";

            [XmlIgnore]
            [JsonProperty(PropertyName = "idType")]
            public string JsonIdType
            {
                // adapt idShort <-> IdShort
                get => (idType == "idShort") ? "IdShort" : idType;
                set => idType = (value == "idShort") ? "IdShort" : value;
            }

            [MetaModelName("Key.value")]
            [TextSearchable]
            [XmlText]
            [CountForHash]
            public string value = "";

            [XmlIgnore]
            [JsonProperty(PropertyName = "index")]
            [CountForHash]
            public int index = 0;

            public Key()
            {
            }

            public Key(Key src)
            {
                this.type = src.type;
                this.local = src.local;
                this.idType = src.idType;
                this.value = src.value;
            }

#if UseAasxCompatibilityModels
            public Key(AasxCompatibilityModels.AdminShellV10.Key src)
            {
                this.type = src.type;
                this.local = src.local;
                this.idType = src.idType;
                if (this.idType.Trim().ToLower() == "uri")
                    this.idType = Identification.IRI;
                if (this.idType.Trim().ToLower() == "idshort")
                    this.idType = Identification.IdShort;
                this.value = src.value;
            }
#endif

            public Key(string type, bool local, string idType, string value)
            {
                this.type = type;
                this.local = local;
                this.idType = idType;
                this.value = value;
            }

            public static Key CreateNew(string type, bool local, string idType, string value)
            {
                var k = new Key()
                {
                    type = type,
                    local = local,
                    idType = idType,
                    value = value
                };
                return (k);
            }

            public static Key GetFromRef(Reference r)
            {
                if (r == null || r.Count != 1)
                    return null;
                return r[0];
            }

            public string ToString(int format = 0)
            {
                if (format == 1)
                {
                    return String.Format(
                        "({0})({1})[{2}]{3}", this.type, this.local ? "local" : "no-local", this.idType, this.value);
                }
                if (format == 2)
                {
                    return String.Format("[{0}]{1}", this.idType, this.value);
                }

                // (old) default
                var tlc = (this.local) ? "Local" : "not Local";
                return $"[{this.type}, {tlc}, {this.idType}, {this.value}]";
            }

            public static string KeyListToString(List<Key> keys)
            {
                if (keys == null || keys.Count < 1)
                    return "";
                // normally, exactly one key
                if (keys.Count == 1)
                    return keys[0].ToString();
                // multiple!
                var s = "[ ";
                foreach (var k in keys)
                {
                    if (s.Length > 0)
                        s += ", ";
                    s += k.ToString();
                }
                return s + " ]";
            }

            public static string[] KeyElements = new string[] {
            "GlobalReference",
            "AccessPermissionRule",
            "Asset",
            "AssetAdministrationShell",
            "ConceptDescription",
            "Submodel",
            "SubmodelRef", // not completely right, but used by Package Explorer
            "Blob",
            "ConceptDictionary",
            "DataElement",
            "File",
            "Operation",
            "OperationVariable",
            "BasicEvent",
            "Entity",
            "Property",
            "MultiLanguageProperty",
            "Range",
            "ReferenceElement",
            "RelationshipElement",
            "AnnotatedRelationshipElement",
            "Capability",
            "SubmodelElement",
            "SubmodelElementCollection",
            "View" };

            public static string[] ReferableElements = new string[] {
            "AccessPermissionRule",
            "Asset",
            "AssetAdministrationShell",
            "ConceptDescription",
            "Submodel",
            "Blob",
            "ConceptDictionary",
            "DataElement",
            "File",
            "Operation",
            "OperationVariable",
            "Entity",
            "BasicEvent",
            "Property",
            "MultiLanguageProperty",
            "Range",
            "ReferenceElement",
            "RelationshipElement",
            "AnnotatedRelationshipElement",
            "Capability",
            "SubmodelElement",
            "SubmodelElementCollection",
            "View" };

            public static string[] SubmodelElements = new string[] {
            "DataElement",
            "File",
            "Event",
            "Operation",
            "Property",
            "MultiLanguageProperty",
            "Range",
            "ReferenceElement",
            "RelationshipElement",
            "AnnotatedRelationshipElement",
            "Capability",
            "BasicEvent",
            "Entity",
            "SubmodelElementCollection"};

            public static string[] IdentifiableElements = new string[] {
            "Asset",
            "AssetAdministrationShell",
            "ConceptDescription",
            "Submodel" };

            // use this in list to designate all of the above elements
            public static string AllElements = "All";

            // use this in list to designate the GlobalReference
            // Resharper disable MemberHidesStaticFromOuterClass
            public static string GlobalReference = "GlobalReference";
            public static string ConceptDescription = "ConceptDescription";
            public static string SubmodelRef = "SubmodelRef";
            public static string Submodel = "Submodel";
            public static string Asset = "Asset";
            public static string AAS = "AssetAdministrationShell";
            // Resharper enable MemberHidesStaticFromOuterClass

            public static string[] IdentifierTypeNames = new string[] {
                Identification.IdShort, "Custom", Identification.IRDI, Identification.IRI };

            public enum IdentifierType { IdShort = 0, Custom, IRDI, IRI };

            public static string GetIdentifierTypeName(IdentifierType t)
            {
                return IdentifierTypeNames[(int)t];
            }

            // some helpers

            public static bool IsInKeyElements(string ke)
            {
                var res = false;
                foreach (var s in KeyElements)
                    if (s.Trim().ToLower() == ke.Trim().ToLower())
                        res = true;
                return res;
            }

            public bool Matches(
                string type, bool local, string idType, string id, MatchMode matchMode = MatchMode.Strict)
            {
                if (matchMode == MatchMode.Strict)
                    return this.type == type && this.local == local && this.idType == idType && this.value == id;

                if (matchMode == MatchMode.Relaxed)
                    return (this.type == type || this.type == Key.GlobalReference || type == Key.GlobalReference)
                        && this.idType == idType && this.value == id;

                if (matchMode == MatchMode.Identification)
                    return this.idType == idType && this.value == id;

                return false;
            }

            public bool Matches(Key key, MatchMode matchMode = MatchMode.Strict)
            {
                if (key == null)
                    return false;
                return this.Matches(key.type, key.local, key.idType, key.value, matchMode);
            }

        }

        public class KeyList : List<Key>
        {
            // getters / setters

            [XmlIgnore]
            public bool IsEmpty { get { return this.Count < 1; } }

            // constructors / creators

            public static KeyList CreateNew(Key k)
            {
                var kl = new KeyList { k };
                return kl;
            }

            public static KeyList CreateNew(string type, bool local, string idType, string value)
            {
                var kl = new KeyList() {
                    Key.CreateNew(type, local, idType, value)
                };
                return kl;
            }

            // other

            public void NumberIndices()
            {
                for (int i = 0; i < this.Count; i++)
                    this[i].index = i;
            }

            public string ToString(int format = 0, string delimiter = ",")
            {
                var res = "";
                foreach (var k in this)
                    res += k.ToString(format) + delimiter;
                return res.TrimEnd(',');
            }
        }

        [XmlType(TypeName = "reference")]
        public class Reference
        {

            // members

            [XmlIgnore] // anyway, as it is private
            [JsonIgnore]
            private KeyList keys = new KeyList();

            // getters / setters

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
            public bool IsEmpty { get { return keys == null || keys.Count < 1; } }
            [XmlIgnore]
            [JsonIgnore]
            public int Count { get { if (keys == null) return 0; return keys.Count; } }
            [XmlIgnore]
            [JsonIgnore]
            public Key this[int index] { get { return keys[index]; } }

            // constructors / creators

            public Reference()
            {
            }

            public Reference(Key k)
            {
                if (k != null)
                    keys.Add(k);
            }

            public Reference(Reference src)
            {
                if (src != null)
                    foreach (var k in src.Keys)
                        keys.Add(new Key(k));
            }

#if UseAasxCompatibilityModels
            public Reference(AasxCompatibilityModels.AdminShellV10.Reference src)
            {
                if (src != null)
                    foreach (var k in src.Keys)
                        keys.Add(new Key(k));
            }
#endif

            public Reference(SemanticId src)
            {
                if (src != null)
                    foreach (var k in src.Keys)
                        keys.Add(new Key(k));
            }

#if UseAasxCompatibilityModels
            public Reference(AasxCompatibilityModels.AdminShellV10.SemanticId src)
            {
                if (src != null)
                    foreach (var k in src.Keys)
                        keys.Add(new Key(k));
            }
#endif
            public static Reference CreateNew(Key k)
            {
                if (k == null)
                    return null;
                var r = new Reference();
                r.keys.Add(k);
                return r;
            }

            public static Reference CreateNew(List<Key> k)
            {
                if (k == null)
                    return null;
                var r = new Reference();
                r.keys.AddRange(k);
                return r;
            }

            public static Reference CreateNew(string type, bool local, string idType, string value)
            {
                if (type == null || idType == null || value == null)
                    return null;
                var r = new Reference();
                r.keys.Add(Key.CreateNew(type, local, idType, value));
                return r;
            }

            public static Reference CreateIrdiReference(string irdi)
            {
                if (irdi == null)
                    return null;
                var r = new Reference();
                r.keys.Add(new Key(Key.GlobalReference, false, Identification.IRDI, irdi));
                return r;
            }

            // further

            public Key GetAsExactlyOneKey()
            {
                if (keys == null || keys.Count != 1)
                    return null;
                var k = keys[0];
                return new Key(k.type, k.local, k.idType, k.value);
            }

            public bool MatchesExactlyOneKey(
                string type, bool local, string idType, string id, Key.MatchMode matchMode = Key.MatchMode.Strict)
            {
                if (keys == null || keys.Count != 1)
                    return false;
                var k = keys[0];
                return k.Matches(type, local, idType, id, matchMode);
            }

            public bool MatchesExactlyOneKey(Key key, Key.MatchMode matchMode = Key.MatchMode.Strict)
            {
                if (key == null)
                    return false;
                return this.MatchesExactlyOneKey(key.type, key.local, key.idType, key.value, matchMode);
            }

            public bool Matches(
                string type, bool local, string idType, string id, Key.MatchMode matchMode = Key.MatchMode.Strict)
            {
                if (this.Count == 1)
                {
                    var k = keys[0];
                    return k.Matches(type, local, idType, id, matchMode);
                }
                return false;
            }

            public bool Matches(Key key, Key.MatchMode matchMode = Key.MatchMode.Strict)
            {
                if (this.Count == 1)
                {
                    var k = keys[0];
                    return k.Matches(key, matchMode);
                }
                return false;
            }

            public bool Matches(Identification other)
            {
                if (other == null)
                    return false;
                if (this.Count == 1)
                {
                    var k = keys[0];
                    return k.Matches(Key.GlobalReference, false, other.idType, other.id, Key.MatchMode.Identification);
                }
                return false;
            }

            public bool Matches(Reference other, Key.MatchMode matchMode = Key.MatchMode.Strict)
            {
                if (this.keys == null || other == null || other.keys == null || other.Count != this.Count)
                    return false;

                var same = true;
                for (int i = 0; i < this.Count; i++)
                    same = same && this.keys[i].Matches(other.keys[i], matchMode);

                return same;
            }

            public bool Matches(SemanticId other)
            {
                return Matches(new Reference(other));
            }

            public string ToString(int format = 0, string delimiter = ",")
            {
                return keys?.ToString(format, delimiter);
            }

            public string ListOfValues(string delim)
            {
                string res = "";
                if (this.Keys != null)
                    foreach (var x in this.Keys)
                    {
                        if (x == null)
                            continue;
                        if (res != "") res += delim;
                        res += x.value;
                    }
                return res;
            }

            public virtual string GetElementName()
            {
                return "Reference";
            }
        }

        [XmlType(TypeName = "derivedFrom")]
        public class AssetAdministrationShellRef : Reference
        {
            // constructors

            public AssetAdministrationShellRef() : base() { }

            public AssetAdministrationShellRef(Key k) : base(k) { }

            public AssetAdministrationShellRef(Reference src) : base(src) { }

#if UseAasxCompatibilityModels
            public AssetAdministrationShellRef(AasxCompatibilityModels.AdminShellV10.Reference src) : base(src) { }
#endif

            // further methods

            public override string GetElementName()
            {
                return "AssetAdministrationShellRef";
            }
        }

        [XmlType(TypeName = "assetRef")]
        public class AssetRef : Reference
        {
            // constructors

            public AssetRef() : base() { }

            public AssetRef(AssetRef src) : base(src) { }

#if UseAasxCompatibilityModels
            public AssetRef(AasxCompatibilityModels.AdminShellV10.AssetRef src) : base(src) { }
#endif

            public AssetRef(Reference r)
                : base(r)
            {
            }

            // further methods

            public override string GetElementName()
            {
                return "AssetRef";
            }
        }

        [XmlType(TypeName = "submodelRef")]
        public class SubmodelRef : Reference
        {
            // constructors

            public SubmodelRef() : base() { }

            public SubmodelRef(SubmodelRef src) : base(src) { }

            public SubmodelRef(Reference src) : base(src) { }

#if UseAasxCompatibilityModels
            public SubmodelRef(AasxCompatibilityModels.AdminShellV10.SubmodelRef src) : base(src) { }
#endif

            public new static SubmodelRef CreateNew(string type, bool local, string idType, string value)
            {
                var r = new SubmodelRef();
                r.Keys.Add(Key.CreateNew(type, local, idType, value));
                return r;
            }

            public static SubmodelRef CreateNew(Reference src)
            {
                if (src == null || src.Keys == null)
                    return null;
                var r = new SubmodelRef();
                r.Keys.AddRange(src.Keys);
                return r;
            }

            // further methods

            public override string GetElementName()
            {
                return "SubmodelRef";
            }
        }

        [XmlType(TypeName = "conceptDescriptionRef")]
        public class ConceptDescriptionRef : Reference
        {
            // constructors

            public ConceptDescriptionRef() : base() { }

            public ConceptDescriptionRef(ConceptDescriptionRef src) : base(src) { }

#if UseAasxCompatibilityModels
            public ConceptDescriptionRef(
                AasxCompatibilityModels.AdminShellV10.ConceptDescriptionRef src) : base(src) { }
#endif

            // further methods

            public new static ConceptDescriptionRef CreateNew(string type, bool local, string idType, string value)
            {
                var r = new ConceptDescriptionRef();
                r.Keys.Add(Key.CreateNew(type, local, idType, value));
                return r;
            }

            public override string GetElementName()
            {
                return "ConceptDescriptionRef";
            }
        }

        [XmlType(TypeName = "dataSpecificationRef")]
        public class DataSpecificationRef : Reference
        {
            // constructors

            public DataSpecificationRef() : base() { }

            public DataSpecificationRef(DataSpecificationRef src) : base(src) { }

#if UseAasxCompatibilityModels
            public DataSpecificationRef(AasxCompatibilityModels.AdminShellV10.DataSpecificationRef src) : base(src) { }
#endif

            // further methods

            public static DataSpecificationRef CreateNew(Reference src)
            {
                if (src == null || src.Keys == null)
                    return null;
                var res = new DataSpecificationRef();
                foreach (var k in src.Keys)
                    res.Keys.Add(new Key(k));
                return res;
            }

            public override string GetElementName()
            {
                return "DataSpecificationRef";
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

#if UseAasxCompatibilityModels
            public ConceptDescriptionRefs(AasxCompatibilityModels.AdminShellV10.ConceptDescriptionRefs src)
            {
                if (src.conceptDescriptions != null)
                    foreach (var cdr in src.conceptDescriptions)
                        this.conceptDescriptions.Add(new ConceptDescriptionRef(cdr));
            }
#endif
        }

        [XmlType(TypeName = "containedElementRef")]
        public class ContainedElementRef : Reference
        {
            // constructors

            public ContainedElementRef() { }

            public ContainedElementRef(ContainedElementRef src) : base(src) { }

#if UseAasxCompatibilityModels
            public ContainedElementRef(AasxCompatibilityModels.AdminShellV10.ContainedElementRef src) : base(src) { }
#endif

            public static ContainedElementRef CreateNew(Reference src)
            {
                if (src == null || src.Keys == null)
                    return null;
                var r = new ContainedElementRef();
                r.Keys.AddRange(src.Keys);
                return r;
            }

            // further methods

            public override string GetElementName()
            {
                return "ContainedElementRef";
            }
        }

        [XmlType(TypeName = "hasDataSpecification")]
        public class HasDataSpecification
        {
            [XmlElement(ElementName = "reference")] // make "reference" go away by magic?!
            public List<Reference> reference = new List<Reference>();

            public HasDataSpecification() { }

            public HasDataSpecification(HasDataSpecification src)
            {
                foreach (var r in src.reference)
                    reference.Add(new Reference(r));
            }

#if UseAasxCompatibilityModels
            public HasDataSpecification(AasxCompatibilityModels.AdminShellV10.HasDataSpecification src)
            {
                foreach (var r in src.reference)
                    reference.Add(new Reference(r));
            }
#endif
        }

        [XmlType(TypeName = "ContainedElements")]
        public class ContainedElements
        {

            // members

            [XmlElement(ElementName = "containedElementRef")] // make "reference" go away by magic?!
            public List<ContainedElementRef> reference = new List<ContainedElementRef>();

            // getter / setter

            public bool IsEmpty { get { return reference == null || reference.Count < 1; } }
            public int Count { get { if (reference == null) return 0; return reference.Count; } }
            public ContainedElementRef this[int index] { get { return reference[index]; } }

            // Creators

            public ContainedElements() { }

            public ContainedElements(ContainedElements src)
            {
                if (src.reference != null)
                    foreach (var r in src.reference)
                        this.reference.Add(new ContainedElementRef(r));
            }

#if UseAasxCompatibilityModels
            public ContainedElements(AasxCompatibilityModels.AdminShellV10.ContainedElements src)
            {
                if (src.reference != null)
                    foreach (var r in src.reference)
                        this.reference.Add(new ContainedElementRef(r));
            }
#endif

            public static ContainedElements CreateOrSetInner(ContainedElements outer, ContainedElementRef[] inner)
            {
                var res = outer;
                if (res == null)
                    res = new ContainedElements();
                if (inner == null)
                {
                    res.reference = null;
                    return res;
                }
                res.reference = new List<ContainedElementRef>(inner);
                return res;
            }

        }

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

#if UseAasxCompatibilityModels
            public LangStr(AasxCompatibilityModels.AdminShellV10.LangStr src)
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
        }

        public class ListOfLangStr : List<LangStr>
        {
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

#if UseAasxCompatibilityModels
            public Description(AasxCompatibilityModels.AdminShellV10.Description src)
            {
                if (src != null)
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

#if UseAasxCompatibilityModels
            public AssetKind(AasxCompatibilityModels.AdminShellV10.Kind src)
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
                var res = new AssetKind() { kind = "Type" };
                return res;
            }

            public static AssetKind CreateAsInstance()
            {
                var res = new AssetKind() { kind = "Instance" };
                return res;
            }
        }

        public class ModelingKind
        {
            [MetaModelName("ModelingKind.kind")]
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
            public bool IsTemplate { get { return kind != null && kind.Trim().ToLower() == "template"; } }

            // constructors / creators

            public ModelingKind() { }

            public ModelingKind(ModelingKind src)
            {
                kind = src.kind;
            }

#if UseAasxCompatibilityModels
            public ModelingKind(AasxCompatibilityModels.AdminShellV10.Kind src)
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
                var res = new ModelingKind() { kind = "Template" };
                return res;
            }

            public static ModelingKind CreateAsInstance()
            {
                var res = new ModelingKind() { kind = "Instance" };
                return res;
            }
        }

        public class SemanticId : Reference
        {

            // constructors / creators

            public SemanticId()
                : base()
            {
            }

            public SemanticId(SemanticId src)
                : base(src)
            {
            }

            public SemanticId(Reference src) : base(src) { }

#if UseAasxCompatibilityModels
            public SemanticId(AasxCompatibilityModels.AdminShellV10.SemanticId src)
                : base(src)
            {
            }
#endif
            public SemanticId(Key key) : base(key) { }

            public static SemanticId CreateFromKey(Key key)
            {
                if (key == null)
                    return null;
                var res = new SemanticId();
                res.Keys.Add(key);
                return res;
            }

            public static SemanticId CreateFromKeys(List<Key> keys)
            {
                if (keys == null)
                    return null;
                var res = new SemanticId();
                res.Keys.AddRange(keys);
                return res;
            }

        }

        public interface IEnumerateChildren
        {
            IEnumerable<SubmodelElementWrapper> EnumerateChildren();
            void AddChild(SubmodelElementWrapper smw);
        }

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

        public class Referable
        {

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
                    if (description == null)
                        description = new Description();
                    description.langString = value;
                }
            }

            [XmlIgnore]
            [JsonIgnore]
            [SkipForHash] // important to skip, as recursion elsewise will go in cycles!
            [SkipForReflection] // important to skip, as recursion elsewise will go in cycles!
            public Referable parent = null;

            public static string CONSTANT = "CONSTANT";
            public static string Category_PARAMETER = "PARAMETER";
            public static string VARIABLE = "VARIABLE";

            public static string[] ReferableCategoryNames = new string[] { CONSTANT, Category_PARAMETER, VARIABLE };

            // constructors

            public Referable() { }

            public Referable(Referable src)
            {
                if (src == null)
                    return;
                this.idShort = src.idShort;
                this.category = src.category;
                if (src.description != null)
                    this.description = new Description(src.description);
            }

#if UseAasxCompatibilityModels
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
#endif

            public void AddDescription(string lang, string str)
            {
                if (description == null)
                    description = new Description();
                description.langString.Add(new LangStr(lang, str));
            }

            public virtual string GetElementName()
            {
                return "Referable"; // not correct, but this method wasn't overridden correctly
            }

            public string GetFriendlyName()
            {
                return AdminShellUtil.FilterFriendlyName(this.idShort);
            }

            public void CollectReferencesByParent(List<Key> refs)
            {
                // check, if this is identifiable
                if (this is Identifiable)
                {
                    var idf = this as Identifiable;
                    if (idf != null)
                    {
                        var k = Key.CreateNew(
                            idf.GetElementName(), true, idf.identification?.idType, idf.identification?.id);
                        refs.Insert(0, k);
                    }
                }
                else
                {
                    var k = Key.CreateNew(this.GetElementName(), true, "IdShort", this.idShort);
                    refs.Insert(0, k);
                    // recurse upwards!
                    if (parent != null)
                        (this.parent).CollectReferencesByParent(refs);
                }
            }

            public string CollectIdShortByParent()
            {
                // recurse first
                var head = "";
                if (!(this is Identifiable) && this.parent != null)
                    // can go up
                    head = this.parent.CollectIdShortByParent() + "/";
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
        }

        public class Identifiable : Referable
        {

            // members

            public Identification identification = new Identification();
            public Administration administration = null;

            // constructors

            public Identifiable() : base() { }

            public Identifiable(Identifiable src)
                : base(src)
            {
                if (src == null)
                    return;
                if (src.identification != null)
                    this.identification = new Identification(src.identification);
                if (src.administration != null)
                    this.administration = new Administration(src.administration);
            }

#if UseAasxCompatibilityModels
            public Identifiable(AasxCompatibilityModels.AdminShellV10.Identifiable src)
                : base(src)
            {
                if (src.identification != null)
                    this.identification = new Identification(src.identification);
                if (src.administration != null)
                    this.administration = new Administration(src.administration);
            }
#endif

            public void SetIdentification(string idType, string id, string idShort = null)
            {
                identification.idType = idType;
                identification.id = id;
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
                if (identification != null && identification.id != "")
                    return AdminShellUtil.FilterFriendlyName(this.identification.id);
                return AdminShellUtil.FilterFriendlyName(this.idShort);
            }

        }

        public class JsonModelTypeWrapper
        {
            public string name = "";

            public JsonModelTypeWrapper(string name = "") { this.name = name; }
        }

        public class AdministrationShell : Identifiable
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

#if UseAasxCompatibilityModels
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
#endif

            public static AdministrationShell CreateNew(
                string idType, string id, string version = null, string revision = null)
            {
                var s = new AdministrationShell();
                if (version != null)
                    s.SetAdminstration(version, revision);
                s.identification.idType = idType;
                s.identification.id = id;
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
                hasDataSpecification.reference.Add(r);
            }

            public override string GetElementName()
            {
                return "AssetAdministrationShell";
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", idShort, "\"AAS\"");
                if (administration != null)
                    caption += "V" + administration.version + "." + administration.revision;

                var info = "";
                if (identification != null)
                    info = $"[{identification.idType}, {identification.id}]";
                return Tuple.Create(caption, info);
            }

            public override string ToString()
            {
                var ci = ToCaptionInfo();
                return string.Format("{0}{1}", ci.Item1, (ci.Item2 != "") ? " / " + ci.Item2 : "");
            }

            public SubmodelRef FindSubmodelRef(Identification refid)
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

        }

        public class Asset : Identifiable
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // from hasDataSpecification:
            [XmlElement(ElementName = "hasDataSpecification")]
            public HasDataSpecification hasDataSpecification = null;
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
            // from this very class
            [XmlElement(ElementName = "assetIdentificationModelRef")]
            public SubmodelRef assetIdentificationModelRef = null;

            [XmlElement(ElementName = "billOfMaterialRef")]
            public SubmodelRef billOfMaterialRef = null;

            // constructors

            public Asset() { }

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

#if UseAasxCompatibilityModels
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
#endif

            // Getter & setters

            public AssetRef GetReference()
            {
                var r = new AssetRef();
                r.Keys.Add(
                    Key.CreateNew(
                        this.GetElementName(), true, this.identification.idType, this.identification.id));
                return r;
            }

            public override string GetElementName()
            {
                return "Asset";
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
                if (administration != null)
                    caption += "V" + administration.version + "." + administration.revision;

                var info = "";
                if (identification != null)
                    info = $"[{identification.idType}, {identification.id}]";
                return Tuple.Create(caption, info);
            }

            public override string ToString()
            {
                var ci = ToCaptionInfo();
                return string.Format("{0}{1}", ci.Item1, (ci.Item2 != "") ? " / " + ci.Item2 : "");
            }

        }

        public class View : Referable
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // members

            // from hasSemanticId:
            [XmlElement(ElementName = "semanticId")]
            public SemanticId semanticId = null;
            // from hasDataSpecification
            [XmlElement(ElementName = "hasDataSpecification")]
            public HasDataSpecification hasDataSpecification = null;

            // from this very class
            [JsonIgnore]
            [SkipForSearch]
            public ContainedElements containedElements = null;
            [XmlIgnore]
            [SkipForSearch]
            [JsonProperty(PropertyName = "containedElements")]
            public ContainedElementRef[] JsonContainedElements
            {
                get { return containedElements?.reference.ToArray(); }
                set { containedElements = ContainedElements.CreateOrSetInner(containedElements, value); }
            }

            // getter / setter

            [XmlIgnore]
            [JsonIgnore]
            public bool IsEmpty { get { return containedElements == null || containedElements.Count < 1; } }
            [XmlIgnore]
            [JsonIgnore]
            public int Count { get { if (containedElements == null) return 0; return containedElements.Count; } }

            public ContainedElementRef this[int index]
            {
                get { if (containedElements == null) return null; return containedElements[index]; }
            }

            // constructors / creators

            public View() { }

            public View(View src)
                : base(src)
            {
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                if (src.hasDataSpecification != null)
                    this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                if (src.containedElements != null)
                    this.containedElements = new ContainedElements(src.containedElements);
            }

#if UseAasxCompatibilityModels
            public View(AasxCompatibilityModels.AdminShellV10.View src)
                : base(src)
            {
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                if (src.hasDataSpecification != null)
                    this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                if (src.containedElements != null)
                    this.containedElements = new ContainedElements(src.containedElements);
            }
#endif

            public static View CreateNew(string idShort)
            {
                var v = new View() { idShort = idShort };
                return (v);
            }

            public void AddDataSpecification(Key k)
            {
                if (hasDataSpecification == null)
                    hasDataSpecification = new HasDataSpecification();
                var r = new Reference();
                r.Keys.Add(k);
                hasDataSpecification.reference.Add(r);
            }

            public void AddContainedElement(Key k)
            {
                if (containedElements == null)
                    containedElements = new ContainedElements();
                var r = new ContainedElementRef();
                r.Keys.Add(k);
                containedElements.reference.Add(r);
            }

            public void AddContainedElement(List<Key> keys)
            {
                if (containedElements == null)
                    containedElements = new ContainedElements();
                var r = new ContainedElementRef();
                foreach (var k in keys)
                    r.Keys.Add(k);
                containedElements.reference.Add(r);
            }

            public void AddContainedElement(Reference r)
            {
                if (containedElements == null)
                    containedElements = new ContainedElements();
                containedElements.reference.Add(ContainedElementRef.CreateNew(r));
            }

            public void AddContainedElement(List<Reference> rlist)
            {
                if (containedElements == null)
                    containedElements = new ContainedElements();
                foreach (var r in rlist)
                    containedElements.reference.Add(ContainedElementRef.CreateNew(r));
            }

            public override string GetElementName()
            {
                return "View";
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
                var info = "";
                if (this.semanticId != null)
                    info = Key.KeyListToString(this.semanticId.Keys);
                if (this.containedElements != null && this.containedElements.reference != null)
                    info = (info + " ").Trim() +
                        String.Format("({0} elements)", this.containedElements.reference.Count);
                return Tuple.Create(caption, info);
            }

            public override string ToString()
            {
                var ci = ToCaptionInfo();
                return string.Format("{0}{1}", ci.Item1, (ci.Item2 != "") ? " / " + ci.Item2 : "");
            }
        }

        public class Views
        {
            [XmlElement(ElementName = "view")]
            [JsonIgnore]
            public List<View> views = new List<View>();

            // constructors

            public Views() { }

            public Views(Views src)
            {
                if (src != null && src.views != null)
                    foreach (var v in src.views)
                        this.views.Add(new View(v));
            }

#if UseAasxCompatibilityModels
            public Views(AasxCompatibilityModels.AdminShellV10.Views src)
            {
                if (src != null && src.views != null)
                    foreach (var v in src.views)
                        this.views.Add(new View(v));
            }
#endif

            public static Views CreateOrSetInnerViews(Views outer, View[] inner)
            {
                var res = outer;
                if (res == null)
                    res = new Views();
                if (inner == null)
                {
                    res.views = null;
                    return res;
                }
                res.views = new List<View>(inner);
                return res;
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

#if UseAasxCompatibilityModels
            // not available in V1.0
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

        /// <summary>
        /// Multiple lang string for the IEC61360 namespace
        /// </summary>
        public class LangStringSetIEC61360
        {

            // members

            [XmlElement(ElementName = "langString", Namespace = "http://www.admin-shell.io/IEC61360/2/0")]
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

            public LangStringSetIEC61360() { }

            public LangStringSetIEC61360(LangStringSetIEC61360 src)
            {
                if (src.langString != null)
                    foreach (var ls in src.langString)
                        this.langString.Add(new LangStr(ls));
            }

#if UseAasxCompatibilityModels
            public LangStringSetIEC61360(AasxCompatibilityModels.AdminShellV10.LangStringIEC61360 src)
            {
                if (src.langString != null)
                    foreach (var ls in src.langString)
                        this.langString.Add(new LangStr(ls));
            }
#endif
            public LangStringSetIEC61360(string lang, string str)
            {
                if (str == null || str.Trim() == "")
                    return;
                this.langString.Add(new LangStr(lang, str));
            }

            // converter

            public static LangStringSetIEC61360 CreateFrom(List<LangStr> src)
            {
                var res = new LangStringSetIEC61360();
                if (src != null)
                    foreach (var ls in src)
                        res.langString.Add(new LangStr(ls));
                return res;
            }

            // single string representation

            public string GetDefaultStr(string defaultLang = null)
            {
                return this.langString?.GetDefaultStr(defaultLang);
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

#if UseAasxCompatibilityModels
            public UnitId(AasxCompatibilityModels.AdminShellV10.UnitId src)
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
                var res = new UnitId();
                if (src != null && src.Keys != null)
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
            public LangStringSetIEC61360 preferredName = new LangStringSetIEC61360();

            public LangStringSetIEC61360 shortName = new LangStringSetIEC61360();

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
            public LangStringSetIEC61360 definition = new LangStringSetIEC61360();

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

#if UseAasxCompatibilityModels
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
                    d.preferredName.langString = LangStr.CreateManyFromStringArray(preferredName);
                d.shortName = new LangStringSetIEC61360("EN?", shortName);
                d.unit = unit;
                d.unitId = unitId;
                d.valueFormat = valueFormat;
                d.sourceOfDefinition = sourceOfDefinition;
                d.symbol = symbol;
                d.dataType = dataType;
                if (definition != null)
                    d.definition.langString = LangStr.CreateManyFromStringArray(definition);
                return (d);
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

            public DataSpecificationIEC61360 dataSpecificationIEC61360 = new DataSpecificationIEC61360();
            public DataSpecificationISO99999 dataSpecificationISO99999 = null;

            // constructors

            public DataSpecificationContent() { }

            public DataSpecificationContent(DataSpecificationContent src)
            {
                if (src.dataSpecificationIEC61360 != null)
                    this.dataSpecificationIEC61360 = new DataSpecificationIEC61360(src.dataSpecificationIEC61360);
            }

#if UseAasxCompatibilityModels
            public DataSpecificationContent(AasxCompatibilityModels.AdminShellV10.DataSpecificationContent src)
            {
                if (src.dataSpecificationIEC61360 != null)
                    this.dataSpecificationIEC61360 = new DataSpecificationIEC61360(src.dataSpecificationIEC61360);
            }
#endif
        }

        public class EmbeddedDataSpecification
        {
            // members

            public DataSpecificationContent dataSpecificationContent = new DataSpecificationContent();
            public DataSpecificationRef dataSpecification = new DataSpecificationRef();

            // constructors

            public EmbeddedDataSpecification() { }

            public EmbeddedDataSpecification(EmbeddedDataSpecification src)
            {
                if (src.dataSpecification != null)
                    this.dataSpecification = new DataSpecificationRef(src.dataSpecification);
                if (src.dataSpecificationContent != null)
                    this.dataSpecificationContent = new DataSpecificationContent(src.dataSpecificationContent);
            }

#if UseAasxCompatibilityModels
            public EmbeddedDataSpecification(AasxCompatibilityModels.AdminShellV10.EmbeddedDataSpecification src)
            {
                if (src.hasDataSpecification != null)
                    this.dataSpecification = new DataSpecificationRef(src.hasDataSpecification);
                if (src.dataSpecificationContent != null)
                    this.dataSpecificationContent = new DataSpecificationContent(src.dataSpecificationContent);
            }
#endif
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
            // TODO: in V1.0, shall be a list of embeddedDataSpecification
            [XmlElement(ElementName = "embeddedDataSpecification")]
            [JsonIgnore]
            public EmbeddedDataSpecification embeddedDataSpecification = new EmbeddedDataSpecification();
            [XmlIgnore]
            [JsonProperty(PropertyName = "embeddedDataSpecifications")]
            public EmbeddedDataSpecification[] JsonEmbeddedDataSpecifications
            {
                get
                {
                    if (embeddedDataSpecification == null)
                        return null;
                    return new[] { embeddedDataSpecification };
                }
                set
                {
                    embeddedDataSpecification = (value == null) ? null : value[0];
                }
            }

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
                    this.embeddedDataSpecification = new EmbeddedDataSpecification(src.embeddedDataSpecification);
                if (src.isCaseOf != null)
                    foreach (var ico in src.isCaseOf)
                    {
                        if (this.isCaseOf == null)
                            this.isCaseOf = new List<Reference>();
                        this.isCaseOf.Add(new Reference(ico));
                    }
            }

#if UseAasxCompatibilityModels
            public ConceptDescription(AasxCompatibilityModels.AdminShellV10.ConceptDescription src)
                : base(src)
            {
                if (src.embeddedDataSpecification != null)
                    this.embeddedDataSpecification = new EmbeddedDataSpecification(src.embeddedDataSpecification);
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
                string idType, string id, string version = null, string revision = null, string idShort = null)
            {
                var cd = new ConceptDescription();
                if (idShort != null)
                    cd.idShort = idShort;
                cd.identification.idType = idType;
                cd.identification.id = id;
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
                return Key.CreateNew(this.GetElementName(), true, this.identification.idType, this.identification.id);
            }

            public ConceptDescriptionRef GetReference()
            {
                var r = new ConceptDescriptionRef();
                r.Keys.Add(
                    Key.CreateNew(
                        this.GetElementName(), true, this.identification.idType, this.identification.id));
                return r;
            }

            public Key GetGlobalDataSpecRef()
            {
                if (embeddedDataSpecification.dataSpecification.Count != 1)
                    return null;
                return (embeddedDataSpecification.dataSpecification[0]);
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
                this.embeddedDataSpecification = new EmbeddedDataSpecification();
                this.embeddedDataSpecification.dataSpecification.Keys.Add(
                    Key.CreateNew(
                        "GlobalReference", false, "IRI",
                        "www.admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360"));
                this.embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360 =
                    AdminShell.DataSpecificationIEC61360.CreateNew(
                        preferredNames, shortName, unit, unitId, valueFormat, sourceOfDefinition, symbol,
                        dataType, definition);
                this.AddIsCaseOf(
                    Reference.CreateNew(
                        new Key("ConceptDescription", false, this.identification.idType, this.identification.id)));
            }

            public DataSpecificationIEC61360 GetIEC61360()
            {
                if (embeddedDataSpecification != null &&
                    embeddedDataSpecification.dataSpecificationContent != null &&
                    embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360 != null)
                    return embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360;
                return null;
            }

            public string GetDefaultPreferredName(string defaultLang = null)
            {
                return "" +
                    embeddedDataSpecification?.dataSpecificationContent?.dataSpecificationIEC61360?
                        .preferredName?.GetDefaultStr(defaultLang);
            }

            public string GetDefaultShortName(string defaultLang = null)
            {
                return "" +
                    embeddedDataSpecification?.dataSpecificationContent?.dataSpecificationIEC61360?
                        .shortName?.GetDefaultStr(defaultLang);
            }

            public override string GetElementName()
            {
                return "ConceptDescription";
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = "";
                if (this.idShort != null && this.idShort.Trim() != "")
                    caption = $"\"{this.idShort.Trim()}\"";
                if (this.identification != null)
                    caption = (caption + " " + this.identification).Trim();

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
        }

        public class ListOfConceptDescriptions : List<ConceptDescription>
        {

            // finding

            public ConceptDescription Find(ConceptDescriptionRef cdr)
            {
                if (cdr == null)
                    return null;
                return Find(cdr.Keys);
            }

            public ConceptDescription Find(Identification id)
            {
                var cdr = ConceptDescriptionRef.CreateNew("Conceptdescription", true, id.idType, id.id);
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
                if (!key.local || key.type.ToLower().Trim() != "conceptdescription")
                    return null;
                // brute force
                foreach (var cd in this)
                    if (cd.identification.idType.ToLower().Trim() == key.idType.ToLower().Trim()
                        && cd.identification.id.ToLower().Trim() == key.value.ToLower().Trim())
                        return cd;
                // uups
                return null;
            }

            // item management

            public ConceptDescription AddIfNew(ConceptDescription cd)
            {
                if (cd == null)
                    return null;

                var exist = this.Find(cd.identification);
                if (exist != null)
                    return exist;

                this.Add(cd);
                return cd;
            }
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

#if UseAasxCompatibilityModels
            public ConceptDictionary(AasxCompatibilityModels.AdminShellV10.ConceptDictionary src)
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

            public override string GetElementName()
            {
                return "ConceptDictionary";
            }
        }

        [XmlRoot(ElementName = "aasenv", Namespace = "http://www.admin-shell.io/aas/2/0")]
        public class AdministrationShellEnv
        {
            [XmlAttribute(Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
            public string schemaLocation =
                "http://www.admin-shell.io/aas/2/0 AAS.xsd http://www.admin-shell.io/IEC61360/2/0 IEC61360.xsd";

            // [XmlElement(ElementName="assetAdministrationShells")]
            [XmlIgnore] // will be ignored, anyway
            private List<AdministrationShell> administrationShells = new List<AdministrationShell>();
            [XmlIgnore] // will be ignored, anyway
            private List<Asset> assets = new List<Asset>();
            [XmlIgnore] // will be ignored, anyway
            private List<Submodel> submodels = new List<Submodel>();
            [XmlIgnore] // will be ignored, anyway
            private ListOfConceptDescriptions conceptDescriptions = new ListOfConceptDescriptions();

            // getter / setters

            [XmlArray("assetAdministrationShells")]
            [XmlArrayItem("assetAdministrationShell")]
            [JsonProperty(PropertyName = "assetAdministrationShells")]
            public List<AdministrationShell> AdministrationShells
            {
                get { return administrationShells; }
                set { administrationShells = value; }
            }

            [XmlArray("assets")]
            [XmlArrayItem("asset")]
            [JsonProperty(PropertyName = "assets")]
            public List<Asset> Assets
            {
                get { return assets; }
                set { assets = value; }
            }

            [XmlArray("submodels")]
            [XmlArrayItem("submodel")]
            [JsonProperty(PropertyName = "submodels")]
            public List<Submodel> Submodels
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

#if UseAasxCompatibilityModels
            public AdministrationShellEnv(AasxCompatibilityModels.AdminShellV10.AdministrationShellEnv src)
            {
                if (src.AdministrationShells != null)
                    foreach (var aas in src.AdministrationShells)
                        this.administrationShells.Add(new AdministrationShell(aas));

                if (src.Assets != null)
                    foreach (var asset in src.Assets)
                        this.assets.Add(new Asset(asset));

                if (src.Submodels != null)
                    foreach (var sm in src.Submodels)
                        this.submodels.Add(new Submodel(sm));

                if (src.ConceptDescriptions != null)
                    foreach (var cd in src.ConceptDescriptions)
                        this.conceptDescriptions.Add(new ConceptDescription(cd));
            }
#endif

            // finders

            public AdministrationShell FindAAS(Identification id)
            {
                if (id == null)
                    return null;
                foreach (var aas in this.AdministrationShells)
                    if (aas.identification != null && aas.identification.IsEqual(id))
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

            public AdministrationShell FindAASwithSubmodel(Identification smid)
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

            public Asset FindAsset(Identification id)
            {
                if (id == null)
                    return null;
                foreach (var asset in this.Assets)
                    if (asset.identification != null && asset.identification.IsEqual(id))
                        return asset;
                return null;
            }

            public Asset FindAsset(AssetRef aref)
            {
                // trivial
                if (aref == null)
                    return null;
                // can only refs with 1 key
                if (aref.Count != 1)
                    return null;
                // and we're picky
                var key = aref[0];
                if (!key.local || key.type.ToLower().Trim() != "asset")
                    return null;
                // brute force
                foreach (var a in assets)
                    if (a.identification.idType.ToLower().Trim() == key.idType.ToLower().Trim()
                        && a.identification.id.ToLower().Trim() == key.value.ToLower().Trim())
                        return a;
                // uups
                return null;
            }

            public Submodel FindSubmodel(Identification id)
            {
                if (id == null)
                    return null;
                foreach (var sm in this.Submodels)
                    if (sm.identification != null && sm.identification.IsEqual(id))
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
                if (!key.local || key.type.ToLower().Trim() != "submodel")
                    return null;
                // brute force
                foreach (var sm in this.Submodels)
                    if (sm.identification.idType.ToLower().Trim() == key.idType.ToLower().Trim()
                        && sm.identification.id.ToLower().Trim() == key.value.ToLower().Trim())
                        return sm;
                // uups
                return null;
            }

            public Submodel FindFirstSubmodelBySemanticId(Key semId)
            {
                // access
                if (semId == null)
                    return null;

                // brute force
                foreach (var sm in this.Submodels)
                    if (true == sm.semanticId?.MatchesExactlyOneKey(semId))
                        return sm;

                return null;
            }

            public IEnumerable<Submodel> FindAllSubmodelBySemanticId(
                Key semId, Key.MatchMode matchMode = Key.MatchMode.Strict)
            {
                // access
                if (semId == null)
                    yield break;

                // brute force
                foreach (var sm in this.Submodels)
                    if (true == sm.semanticId?.MatchesExactlyOneKey(semId, matchMode))
                        yield return sm;
            }

            public Referable FindReferableByReference(Reference rf, int keyIndex = 0)
            {
                // first index needs to exist ..
                if (rf == null || keyIndex >= rf.Count)
                    return null;

                // which type?
                var firstType = rf[keyIndex].type.Trim().ToLower();
                var firstIdentification = new Identification(rf[keyIndex].idType, rf[keyIndex].value);

                if (firstType == Key.AAS.Trim().ToLower())
                    return this.FindAAS(firstIdentification);

                if (firstType == Key.Asset.Trim().ToLower())
                    return this.FindAsset(firstIdentification);

                if (firstType == Key.ConceptDescription.Trim().ToLower())
                    return this.FindConceptDescription(firstIdentification);

                if (firstType == Key.Submodel.Trim().ToLower())
                {
                    // ok, search Submodel
                    var sm = this.FindSubmodel(new Identification(rf[keyIndex].idType, rf[keyIndex].value));
                    if (sm == null)
                        return null;

                    // at our end?
                    if (keyIndex >= rf.Count - 1)
                        return sm;

                    // go inside
                    return SubmodelElementWrapper.FindReferableByReference(sm.submodelElements, rf, keyIndex + 1);
                }

                // nothing in this Environment
                return null;
            }

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
                return FindConceptDescription(semId.Keys);
            }

            public ConceptDescription FindConceptDescription(Reference rf)
            {
                if (rf == null)
                    return null;
                return FindConceptDescription(rf.Keys);
            }

            public ConceptDescription FindConceptDescription(Identification id)
            {
                var cdr = ConceptDescriptionRef.CreateNew("Conceptdescription", true, id.idType, id.id);
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
                if (!key.local || key.type.ToLower().Trim() != "conceptdescription")
                    return null;
                // brute force
                foreach (var cd in conceptDescriptions)
                    if (cd.identification.idType.ToLower().Trim() == key.idType.ToLower().Trim()
                        && cd.identification.id.ToLower().Trim() == key.value.ToLower().Trim())
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
                            foreach (var x in sm.submodelElements.FindAll<T>(match))
                                yield return x;
                    }
                }
                else
                {
                    if (this.Submodels != null)
                        foreach (var sm in this.Submodels)
                            if (sm?.submodelElements != null)
                                foreach (var x in sm.submodelElements.FindAll<T>(match))
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

            // creators

            private void CopyConceptDescriptionsFrom(
                AdministrationShellEnv srcEnv, SubmodelElement src, bool shallowCopy = false)
            {
                // access
                if (srcEnv == null || src == null || src.semanticId == null)
                    return;
                // check for this SubmodelElement in Source
                var cdSrc = srcEnv.FindConceptDescription(src.semanticId.Keys);
                if (cdSrc == null)
                    return;
                // check for this SubmodelElement in Destnation (this!)
                var cdDest = this.FindConceptDescription(src.semanticId.Keys);
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
            /// Returns, if a successful renaming was performed
            /// </summary>
            public bool RenameIdentifiable<T>(Identification oldId, Identification newId) where T : Identifiable
            {
                // access
                if (oldId == null || newId == null || oldId.IsEqual(newId))
                    return false;

                if (typeof(T) == typeof(ConceptDescription))
                {
                    // check, if exist or not exist
                    var cdOld = FindConceptDescription(oldId);
                    if (cdOld == null || FindConceptDescription(newId) != null)
                        return false;

                    // rename old cd
                    cdOld.identification = newId;

                    // search all SMEs referring to this CD
                    foreach (var sme in this.FindAllSubmodelElements<SubmodelElement>(match: (s) =>
                    {
                        return (s != null && s.semanticId != null && s.semanticId.Matches(oldId));
                    }))
                    {
                        sme.semanticId[0].idType = newId.idType;
                        sme.semanticId[0].value = newId.id;
                    }

                    // seems fine
                    return true;
                }

                // no result is false, as well
                return false;
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
                        var cd = src.FindConceptDescription(w.submodelElement.semanticId.Keys);
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
                List<Asset> filterForAsset = null,
                List<Submodel> filterForSubmodel = null,
                List<ConceptDescription> filterForCD = null)
            {
                // prepare defaults
                if (filterForAas == null)
                    filterForAas = new List<AdministrationShell>();
                if (filterForAsset == null)
                    filterForAsset = new List<Asset>();
                if (filterForSubmodel == null)
                    filterForSubmodel = new List<Submodel>();
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

                        // consequences
                        if (aas.assetRef != null)
                        {
                            var asset = src.FindAsset(aas.assetRef);
                            if (asset != null)
                                filterForAsset.Add(asset);
                        }

                        if (aas.submodelRefs != null)
                            foreach (var smr in aas.submodelRefs)
                            {
                                var sm = src.FindSubmodel(smr);
                                if (sm != null)
                                    filterForSubmodel.Add(sm);
                            }

                        if (aas.conceptDictionaries != null)
                            foreach (var cdd in aas.conceptDictionaries)
                                if (cdd.conceptDescriptionsRefs != null &&
                                    cdd.conceptDescriptionsRefs.conceptDescriptions != null)
                                    foreach (var cdr in cdd.conceptDescriptionsRefs.conceptDescriptions)
                                    {
                                        var cd = src.FindConceptDescription(cdr);
                                        if (cd != null)
                                            filterForCD.Add(cd);
                                    }
                    }

                // take over Assets
                foreach (var asset in src.assets)
                    if (filterForAsset.Contains(asset))
                    {
                        // take over
                        res.assets.Add(new Asset(asset));
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
        }

        //
        // Submodel + Submodel elements
        //

        public interface IGetReference
        {
            Reference GetReference();
        }

        public class Qualifier
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // member
            // from hasSemantics:
            [XmlElement(ElementName = "semanticId")]
            // [JsonIgnore]
            public SemanticId semanticId = null;

            // this class
            // TODO: check, if Json has Qualifiers or not

            // [JsonIgnore]
            [MetaModelName("Qualifier.type")]
            [TextSearchable]
            [CountForHash]
            public string type = "";

            // [JsonIgnore]
            [MetaModelName("Qualifier.valueType")]
            [TextSearchable]
            [CountForHash]
            public string valueType = "";

            // [JsonIgnore]
            [CountForHash]
            public Reference valueId = null;

            // [JsonIgnore]
            [MetaModelName("Qualifier.value")]
            [TextSearchable]
            [CountForHash]
            public string value = null;

            // Remark: due to publication of v2.0.1, the order of elements has changed!!!
            // from hasSemantics:
            /* OZ
            [XmlElement(ElementName = "semanticId")]
            // [JsonIgnore]
            public SemanticId semanticId = null;
            */

            // constructors

            public Qualifier() { }

            public Qualifier(Qualifier src)
            {
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                this.type = src.type;
                this.value = src.value;
                if (src.valueId != null)
                    this.valueId = new Reference(src.valueId);
            }

#if UseAasxCompatibilityModels
            public Qualifier(AasxCompatibilityModels.AdminShellV10.Qualifier src)
            {
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                this.type = src.qualifierType;
                this.value = src.qualifierValue;
                if (src.qualifierValueId != null)
                    this.valueId = new Reference(src.qualifierValueId);
            }
#endif

            public string GetElementName()
            {
                return "Qualifier";
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
        }

        /// <summary>
        ///  This class holds some convenience functions for Qualifiers
        /// </summary>
        public class QualifierCollection : List<Qualifier>
        {
            public QualifierCollection()
            {

            }

#if UseAasxCompatibilityModels
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
        }

        public class SubmodelElement : Referable, System.IDisposable, IGetReference
        {
            // constants
            public static Type[] PROP_MLP = new Type[] {
                typeof(AdminShell.MultiLanguageProperty), typeof(AdminShell.Property) };

            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // members

            // do this in order to be IDisposable, that is: suitable for (using)
            void System.IDisposable.Dispose() { }
            public void GetData() { }
            // from hasDataSpecification:
            [XmlElement(ElementName = "hasDataSpecification")]
            public HasDataSpecification hasDataSpecification = null;
            // from hasSemantics:
            [XmlElement(ElementName = "semanticId")]
            public SemanticId semanticId = null;
            // from hasKind:
            [XmlElement(ElementName = "kind")]
            [JsonIgnore]
            public ModelingKind kind = null;
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
            // from Qualifiable:
            [XmlArray("qualifier")]
            [XmlArrayItem("qualifier")]
            [JsonProperty(PropertyName = "constraints")]
            public QualifierCollection qualifiers = null;

            // getter / setter

            // constructors / creators

            public SubmodelElement()
                : base() { }

            public SubmodelElement(SubmodelElement src)
                : base(src)
            {
                if (src == null)
                    return;
                if (src.hasDataSpecification != null)
                    hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                if (src.semanticId != null)
                    semanticId = new SemanticId(src.semanticId);
                if (src.kind != null)
                    kind = new ModelingKind(src.kind);
                if (src.qualifiers != null)
                {
                    if (qualifiers == null)
                        qualifiers = new QualifierCollection();
                    foreach (var q in src.qualifiers)
                        qualifiers.Add(new Qualifier(q));
                }
            }

#if UseAasxCompatibilityModels
            public SubmodelElement(AasxCompatibilityModels.AdminShellV10.SubmodelElement src)
                : base(src)
            {
                if (src.hasDataSpecification != null)
                    hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                if (src.semanticId != null)
                    semanticId = new SemanticId(src.semanticId);
                if (src.kind != null)
                    kind = new ModelingKind(src.kind);
                if (src.qualifiers != null)
                {
                    if (qualifiers == null)
                        qualifiers = new QualifierCollection(src.qualifiers);
                    foreach (var q in src.qualifiers)
                        qualifiers.Add(new Qualifier(q));
                }
            }
#endif

            public void CreateNewLogic(string idShort = null, string category = null, Key semanticIdKey = null)
            {
                if (idShort != null)
                    this.idShort = idShort;
                if (category != null)
                    this.category = category;
                if (semanticIdKey != null)
                {
                    if (this.semanticId == null)
                        this.semanticId = new SemanticId();
                    this.semanticId.Keys.Add(semanticIdKey);
                }
            }

            public void AddQualifier(
                string qualifierType = null, string qualifierValue = null, KeyList semanticKeys = null,
                Reference qualifierValueId = null)
            {
                if (this.qualifiers == null)
                    this.qualifiers = new QualifierCollection();
                var q = new Qualifier()
                {
                    type = qualifierType,
                    value = qualifierValue,
                    valueId = qualifierValueId,
                };
                if (semanticKeys != null)
                    q.semanticId = SemanticId.CreateFromKeys(semanticKeys);
                /* OZ
                if (valueType != null)
                    q.valueType = valueType;
                */
                this.qualifiers.Add(q);
            }

            public Qualifier HasQualifierOfType(string qualifierType)
            {
                if (this.qualifiers == null || qualifierType == null)
                    return null;
                foreach (var q in this.qualifiers)
                    if (q.type.Trim().ToLower() == qualifierType.Trim().ToLower())
                        return q;
                return null;
            }

            public override string GetElementName()
            {
                return "SubmodelElement";
            }

            public Reference GetReference()
            {
                Reference r = new Reference();
                // this is the tail of our referencing chain ..
                r.Keys.Add(Key.CreateNew(GetElementName(), true, "IdShort", this.idShort));
                // try to climb up ..
                var current = this.parent;
                while (current != null)
                {
                    if (current is Identifiable)
                    {
                        // add big information set
                        r.Keys.Insert(0, Key.CreateNew(
                            current.GetElementName(),
                            true,
                            (current as Identifiable).identification.idType,
                            (current as Identifiable).identification.id));
                    }
                    else
                    {
                        // reference via idShort
                        r.Keys.Insert(0, Key.CreateNew(
                            current.GetElementName(),
                            true,
                            "IdShort", current.idShort));
                    }
                    current = current.parent;
                }
                return r;
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
                var info = "";
                if (semanticId != null)
                    AdminShellUtil.EvalToNonEmptyString("\u21e8 {0}", semanticId.ToString(), "");
                return Tuple.Create(caption, info);
            }

            public override string ToString()
            {
                var ci = ToCaptionInfo();
                return string.Format("{0}{1}", ci.Item1, (ci.Item2 != "") ? " / " + ci.Item2 : "");
            }

            public virtual string ValueAsText(string defaultLang = null)
            {
                return "";
            }
        }

        [XmlType(TypeName = "submodelElement")]
        public class SubmodelElementWrapper
        {

            // members

            [XmlElement(ElementName = "property", Type = typeof(Property))]
            [XmlElement(ElementName = "multiLanguageProperty", Type = typeof(MultiLanguageProperty))]
            [XmlElement(ElementName = "range", Type = typeof(Range))]
            [XmlElement(ElementName = "file", Type = typeof(File))]
            [XmlElement(ElementName = "blob", Type = typeof(Blob))]
            [XmlElement(ElementName = "referenceElement", Type = typeof(ReferenceElement))]
            [XmlElement(ElementName = "relationshipElement", Type = typeof(RelationshipElement))]
            [XmlElement(ElementName = "annotatedRelationshipElement", Type = typeof(AnnotatedRelationshipElement))]
            [XmlElement(ElementName = "capability", Type = typeof(Capability))]
            [XmlElement(ElementName = "submodelElementCollection", Type = typeof(SubmodelElementCollection))]
            [XmlElement(ElementName = "operation", Type = typeof(Operation))]
            [XmlElement(ElementName = "basicEvent", Type = typeof(BasicEvent))]
            [XmlElement(ElementName = "entity", Type = typeof(Entity))]
            public SubmodelElement submodelElement;

            // element names
            public enum AdequateElementEnum
            {
                Unknown = 0, SubmodelElementCollection, Property, MultiLanguageProperty, Range, File, Blob,
                ReferenceElement, RelationshipElement, AnnotatedRelationshipElement, Capability, Operation,
                BasicEvent, Entity
            }

            public static AdequateElementEnum[] AdequateElementsDataElement =
            {
                AdequateElementEnum.SubmodelElementCollection, AdequateElementEnum.RelationshipElement,
                AdequateElementEnum.AnnotatedRelationshipElement, AdequateElementEnum.Capability,
                AdequateElementEnum.Operation, AdequateElementEnum.BasicEvent, AdequateElementEnum.Entity
            };

            public static string[] AdequateElementNames = { "Unknown", "SubmodelElementCollection", "Property",
                "MultiLanguageProperty", "Range", "File", "Blob", "ReferenceElement", "RelationshipElement",
                "AnnotatedRelationshipElement", "Capability", "Operation", "BasicEvent", "Entity" };

            // constructors

            public SubmodelElementWrapper() { }

            // cloning

            public SubmodelElementWrapper(SubmodelElement src, bool shallowCopy = false)
            {
                if (src is SubmodelElementCollection)
                    this.submodelElement = new SubmodelElementCollection(
                        src as SubmodelElementCollection, shallowCopy: shallowCopy);
                if (src is Property)
                    this.submodelElement = new Property(src as Property);
                if (src is MultiLanguageProperty)
                    this.submodelElement = new MultiLanguageProperty(src as MultiLanguageProperty);
                if (src is Range)
                    this.submodelElement = new Range(src as Range);
                if (src is File)
                    this.submodelElement = new File(src as File);
                if (src is Blob)
                    this.submodelElement = new Blob(src as Blob);
                if (src is ReferenceElement)
                    this.submodelElement = new ReferenceElement(src as ReferenceElement);
                if (src is RelationshipElement)
                    this.submodelElement = new RelationshipElement(src as RelationshipElement);
                if (src is AnnotatedRelationshipElement)
                    this.submodelElement = new AnnotatedRelationshipElement(src as AnnotatedRelationshipElement);
                if (src is Capability)
                    this.submodelElement = new Capability(src as Capability);
                if (src is Operation)
                    this.submodelElement = new Operation(src as Operation);
                if (src is BasicEvent)
                    this.submodelElement = new BasicEvent(src as BasicEvent);
                if (src is Entity)
                    this.submodelElement = new Entity(src as Entity);
            }

#if UseAasxCompatibilityModels
            public SubmodelElementWrapper(
                AasxCompatibilityModels.AdminShellV10.SubmodelElement src, bool shallowCopy = false)
            {
                if (src is AasxCompatibilityModels.AdminShellV10.SubmodelElementCollection)
                    this.submodelElement = new SubmodelElementCollection(
                        src as AasxCompatibilityModels.AdminShellV10.SubmodelElementCollection,
                        shallowCopy: shallowCopy);
                if (src is AasxCompatibilityModels.AdminShellV10.Property)
                    this.submodelElement = new Property(src as AasxCompatibilityModels.AdminShellV10.Property);
                if (src is AasxCompatibilityModels.AdminShellV10.File)
                    this.submodelElement = new File(src as AasxCompatibilityModels.AdminShellV10.File);
                if (src is AasxCompatibilityModels.AdminShellV10.Blob)
                    this.submodelElement = new Blob(src as AasxCompatibilityModels.AdminShellV10.Blob);
                if (src is AasxCompatibilityModels.AdminShellV10.ReferenceElement)
                    this.submodelElement = new ReferenceElement(
                        src as AasxCompatibilityModels.AdminShellV10.ReferenceElement);
                if (src is AasxCompatibilityModels.AdminShellV10.RelationshipElement)
                    this.submodelElement = new RelationshipElement(
                        src as AasxCompatibilityModels.AdminShellV10.RelationshipElement);
                if (src is AasxCompatibilityModels.AdminShellV10.Operation)
                    this.submodelElement = new Operation(src as AasxCompatibilityModels.AdminShellV10.Operation);
            }
#endif

            public static string GetAdequateName(AdequateElementEnum ae)
            {
                return AdequateElementNames[(int)ae];
            }

            public static AdequateElementEnum GetAdequateEnum(string adequateName)
            {
                if (adequateName == null)
                    return AdequateElementEnum.Unknown;

                foreach (var en in (AdequateElementEnum[])Enum.GetValues(typeof(AdequateElementEnum)))
                    if (Enum.GetName(typeof(AdequateElementEnum), en)?.Trim().ToLower() ==
                        adequateName.Trim().ToLower())
                        return en;

                return AdequateElementEnum.Unknown;
            }

            public static IEnumerable<AdequateElementEnum> GetAdequateEnums(
                AdequateElementEnum[] excludeValues = null, AdequateElementEnum[] includeValues = null)
            {
                if (includeValues != null)
                {
                    foreach (var en in includeValues)
                        yield return en;
                }
                else
                {
                    foreach (var en in (AdequateElementEnum[])Enum.GetValues(typeof(AdequateElementEnum)))
                    {
                        if (en == AdequateElementEnum.Unknown)
                            continue;
                        if (excludeValues != null && excludeValues.Contains(en))
                            continue;
                        yield return en;
                    }
                }
            }

            /// <summary>
            /// Introduced for JSON serialization, can create SubModelElements based on a string name
            /// </summary>
            public static SubmodelElement CreateAdequateType(AdequateElementEnum ae, SubmodelElement src = null)
            {
                if (ae == AdequateElementEnum.Property)
                    return new Property(src);
                if (ae == AdequateElementEnum.MultiLanguageProperty)
                    return new MultiLanguageProperty(src);
                if (ae == AdequateElementEnum.Range)
                    return new Range(src);
                if (ae == AdequateElementEnum.File)
                    return new File(src);
                if (ae == AdequateElementEnum.Blob)
                    return new Blob(src);
                if (ae == AdequateElementEnum.ReferenceElement)
                    return new ReferenceElement(src);
                if (ae == AdequateElementEnum.RelationshipElement)
                    return new RelationshipElement(src);
                if (ae == AdequateElementEnum.AnnotatedRelationshipElement)
                    return new AnnotatedRelationshipElement(src);
                if (ae == AdequateElementEnum.Capability)
                    return new Capability(src);
                if (ae == AdequateElementEnum.SubmodelElementCollection)
                    return new SubmodelElementCollection(src);
                if (ae == AdequateElementEnum.Operation)
                    return new Operation(src);
                if (ae == AdequateElementEnum.BasicEvent)
                    return new BasicEvent(src);
                if (ae == AdequateElementEnum.Entity)
                    return new Entity(src);
                return null;
            }

            /// <summary>
            /// Introduced for JSON serialization, can create SubModelElements based on a string name
            /// </summary>
            /// <param name="elementName">string name (standard PascalCased)</param>
            public static SubmodelElement CreateAdequateType(string elementName)
            {
                return CreateAdequateType(GetAdequateEnum(elementName));
            }

            /// <summary>
            /// Can create SubmodelElements based on a given type information
            /// </summary>
            /// <param name="t">Type of the SME to be created</param>
            /// <returns>SubmodelElement or null</returns>
            public static SubmodelElement CreateAdequateType(Type t)
            {
                if (t == null || !t.IsSubclassOf(typeof(AdminShell.SubmodelElement)))
                    return null;
                var sme = Activator.CreateInstance(t) as SubmodelElement;
                return sme;
            }

            public string GetFourDigitCode()
            {
                if (submodelElement == null)
                    return ("Null");
                if (submodelElement is AdminShell.Property) return ("Prop");
                if (submodelElement is AdminShell.MultiLanguageProperty) return ("Lang");
                if (submodelElement is AdminShell.Range) return ("Rang");
                if (submodelElement is AdminShell.File) return ("File");
                if (submodelElement is AdminShell.Blob) return ("Blob");
                if (submodelElement is AdminShell.ReferenceElement) return ("Ref");
                if (submodelElement is AdminShell.AnnotatedRelationshipElement) return ("ARel");
                // Note: sequence matters, as AnnotatedRelationshipElement is also RelationshipElement!!
                if (submodelElement is AdminShell.RelationshipElement) return ("Rel");
                if (submodelElement is AdminShell.Capability) return ("Cap");
                if (submodelElement is AdminShell.SubmodelElementCollection) return ("Coll");
                if (submodelElement is AdminShell.Operation) return ("Opr");
                if (submodelElement is AdminShell.Entity) return ("Ent");
                if (submodelElement is AdminShell.BasicEvent) return ("Evt");
                return ("Elem");
            }

            public static List<SubmodelElement> ListOfWrappersToListOfElems(List<SubmodelElementWrapper> wrappers)
            {
                var res = new List<SubmodelElement>();
                if (wrappers == null)
                    return res;
                foreach (var w in wrappers)
                    if (w.submodelElement != null)
                        res.Add(w.submodelElement);
                return res;
            }

            public static SubmodelElementWrapper CreateFor(SubmodelElement sme)
            {
                var res = new SubmodelElementWrapper() { submodelElement = sme };
                return res;
            }

            public static Referable FindReferableByReference(
                List<SubmodelElementWrapper> wrappers, Reference rf, int keyIndex)
            {
                // first index needs to exist ..
                if (wrappers == null || rf == null || keyIndex >= rf.Count)
                    return null;

                // as SubmodelElements are not Identifiables, the actual key shall be IdSHort
                if (rf[keyIndex].idType.Trim().ToLower() != Key.GetIdentifierTypeName(
                                                                Key.IdentifierType.IdShort).Trim().ToLower())
                    return null;

                // over all wrappers
                foreach (var smw in wrappers)
                    if (smw.submodelElement != null &&
                        smw.submodelElement.idShort.Trim().ToLower() == rf[keyIndex].value.Trim().ToLower())
                    {
                        // match on this level. Did we find a leaf element?
                        if ((keyIndex + 1) >= rf.Count)
                            return smw.submodelElement;

                        // dive into SMC?
                        if (smw.submodelElement is SubmodelElementCollection smc)
                        {
                            var found = FindReferableByReference(smc.value, rf, keyIndex + 1);
                            if (found != null)
                                return found;
                        }

                        // dive into Entity statements?
                        if (smw.submodelElement is Entity ent)
                        {
                            var found = FindReferableByReference(ent.statements, rf, keyIndex + 1);
                            if (found != null)
                                return found;
                        }

                        // else:
                        return null;
                    }

                // no?
                return null;
            }

            // typecasting wrapper into specific type
            public T GetAs<T>() where T : SubmodelElement
            {
                var x = (this.submodelElement) as T;
                return x;
            }

        }

        public class SubmodelElementWrapperCollection : BaseSubmodelElementWrapperCollection<SubmodelElement>
        {
            public SubmodelElementWrapperCollection() : base() { }

            public SubmodelElementWrapperCollection(SubmodelElementWrapperCollection other)
                : base(other)
            {
            }
        }

        public class DataElementWrapperCollection : BaseSubmodelElementWrapperCollection<DataElement>
        {
            public DataElementWrapperCollection() : base() { }

            public DataElementWrapperCollection(SubmodelElementWrapperCollection other)
                : base(other)
            {
            }

            public DataElementWrapperCollection(DataElementWrapperCollection other)
                : base()
            {
                foreach (var wo in other)
                    this.Add(wo);
            }
        }

        /// <summary>
        /// Provides some more functionalities for searching specific elements, e.g. in a SMEC
        /// </summary>
        // OZ
        // Resharper disable UnusedTypeParameter
        public class BaseSubmodelElementWrapperCollection<ELEMT> : List<SubmodelElementWrapper>
            where ELEMT : SubmodelElement
        {
            // Resharper enable UnusedTypeParameter
            // no new members, as due to inheritance

            // constructors

            public BaseSubmodelElementWrapperCollection() : base() { }

            public BaseSubmodelElementWrapperCollection(SubmodelElementWrapperCollection other)
                : base()
            {
                if (other == null)
                    return;

                foreach (var smw in other)
                    this.Add(new SubmodelElementWrapper(smw.submodelElement));
            }

            // better find functions

            public IEnumerable<T> FindAll<T>(Predicate<T> match = null) where T : SubmodelElement
            {
                foreach (var smw in this)
                {
                    var current = smw.submodelElement;
                    if (current == null)
                        continue;

                    // call lambda for this element
                    if (current is T)
                        if (match == null || match.Invoke(current as T))
                            yield return current as T;

                    // dive into?
                    if (current is SubmodelElementCollection)
                    {
                        var smc = current as SubmodelElementCollection;
                        foreach (var x in smc.value.FindAll<T>(match))
                            yield return x;
                    }

                    if (current is Operation)
                    {
                        var op = current as Operation;
                        for (int i = 0; i < 2; i++)
                            foreach (var x in Operation.GetWrappers(op[i]).FindAll<T>(match))
                                yield return x;
                    }
                }
            }

            public IEnumerable<SubmodelElementWrapper> FindAllIdShort(string idShort)
            {
                foreach (var smw in this)
                    if (smw.submodelElement != null)
                        if (smw.submodelElement.idShort.Trim().ToLower() == idShort.Trim().ToLower())
                            yield return smw;
            }

            public IEnumerable<T> FindAllIdShortAs<T>(string idShort) where T : SubmodelElement
            {
                foreach (var smw in this)
                    if (smw.submodelElement != null && smw.submodelElement is T)
                        if (smw.submodelElement.idShort.Trim().ToLower() == idShort.Trim().ToLower())
                            yield return smw.submodelElement as T;
            }

            public SubmodelElementWrapper FindFirstIdShort(string idShort)
            {
                return FindAllIdShort(idShort)?.FirstOrDefault<SubmodelElementWrapper>();
            }

            public T FindFirstIdShortAs<T>(string idShort) where T : SubmodelElement
            {
                return FindAllIdShortAs<T>(idShort)?.FirstOrDefault<T>();
            }

            public IEnumerable<SubmodelElementWrapper> FindAllSemanticId(Key semId, Type[] allowedTypes = null)
            {
                foreach (var smw in this)
                    if (smw.submodelElement != null && smw.submodelElement.semanticId != null)
                    {
                        if (smw.submodelElement == null)
                            continue;

                        if (allowedTypes != null)
                        {
                            var smwt = smw.submodelElement.GetType();
                            if (!allowedTypes.Contains(smwt))
                                continue;
                        }

                        if (smw.submodelElement.semanticId.MatchesExactlyOneKey(semId))
                            yield return smw;
                    }
            }

            public IEnumerable<T> FindAllSemanticIdAs<T>(Key semId, Key.MatchMode matchMode = Key.MatchMode.Strict)
                where T : SubmodelElement
            {
                foreach (var smw in this)
                    if (smw.submodelElement != null && smw.submodelElement is T
                        && smw.submodelElement.semanticId != null)
                        if (smw.submodelElement.semanticId.MatchesExactlyOneKey(semId, matchMode))
                            yield return smw.submodelElement as T;
            }

            public SubmodelElementWrapper FindFirstSemanticId(Key semId, Type[] allowedTypes = null)
            {
                return FindAllSemanticId(semId, allowedTypes)?.FirstOrDefault<SubmodelElementWrapper>();
            }

            public T FindFirstSemanticIdAs<T>(Key semId, Key.MatchMode matchMode = Key.MatchMode.Strict)
                where T : SubmodelElement
            {
                return FindAllSemanticIdAs<T>(semId, matchMode)?.FirstOrDefault<T>();
            }

            // recursion

            public void RecurseOnSubmodelElements(
                object state, List<SubmodelElement> parents,
                Action<object, List<SubmodelElement>, SubmodelElement> lambda)
            {
                // trivial
                if (lambda == null)
                    return;
                if (parents == null)
                    parents = new List<SubmodelElement>();

                // over all elements
                foreach (var smw in this)
                {
                    var current = smw.submodelElement;
                    if (current == null)
                        continue;

                    // call lambda for this element
                    lambda(state, parents, current);

                    // add to parents
                    parents.Add(current);

                    // dive into?
                    if (current is SubmodelElementCollection smc)
                        smc.value?.RecurseOnSubmodelElements(state, parents, lambda);

                    if (current is Entity ent)
                        ent.statements?.RecurseOnSubmodelElements(state, parents, lambda);

                    if (current is Operation op)
                        for (int i = 0; i < 2; i++)
                            Operation.GetWrappers(op[i])?.RecurseOnSubmodelElements(state, parents, lambda);

                    if (current is AnnotatedRelationshipElement arel)
                        arel.annotations?.RecurseOnSubmodelElements(state, parents, lambda);

                    // remove from parents
                    parents.RemoveAt(parents.Count - 1);
                }
            }

            // idShort management

            /// <summary>
            /// Checks, if given <c>idShort</c> is already existing in the collection of SubmodelElements.
            /// Trims the string, but does not ignore upper/ lowercase. An empty <c>idShort</c> returns <c>false</c>.
            /// </summary>
            public bool CheckIdShortIsUnique(string idShort)
            {
                idShort = idShort?.Trim();
                if (idShort == null || idShort.Length < 1)
                    return false;

                var res = true;
                foreach (var smw in this)
                    if (smw.submodelElement != null && smw.submodelElement.idShort != null &&
                        smw.submodelElement.idShort == idShort)
                    {
                        res = false;
                        break;
                    }

                return res;
            }

            /// <summary>
            /// The string <c>idShortTemplate</c> shall contain <c>Format.String</c> partt such as <c>{0}</c>.
            /// A <c>int</c>-Parameter is as long incremented, until the resulting <c>idShort</c> proves
            /// to be unique in the collection of SubmodelElements or <c>maxNum</c> is reached.
            /// Returns <c>null</c> in case of any error.
            /// </summary>
            public string IterateIdShortTemplateToBeUnique(string idShortTemplate, int maxNum)
            {
                if (idShortTemplate == null || maxNum < 1 || !idShortTemplate.Contains("{0"))
                    return null;

                int i = 1;
                while (i < maxNum)
                {
                    var ids = String.Format(idShortTemplate, i);
                    if (this.CheckIdShortIsUnique(ids))
                        return ids;
                    i++;
                }

                return null;
            }

            // give more direct access to SMEs

            /// <summary>
            /// Add <c>sme</c> by creating a SubmodelElementWrapper for it and adding to this collection.
            /// </summary>
            public void Add(SubmodelElement sme)
            {
                if (sme == null)
                    return;
                this.Add(SubmodelElementWrapper.CreateFor(sme));
            }

            /// <summary>
            /// Finds the first (shall be only 1!) SubmodelElementWrapper with SubmodelElement <c>sme</c>.
            /// </summary>
            public SubmodelElementWrapper FindSubModelElement(SubmodelElement sme)
            {
                if (sme != null)
                    foreach (var smw in this)
                        if (smw?.submodelElement == sme)
                            return smw;
                return null;
            }

            /// <summary>
            /// Removes the first (shall be only 1!) SubmodelElementWrapper with SubmodelElement <c>sme</c>.
            /// </summary>
            public void Remove(SubmodelElement sme)
            {
                if (sme == null)
                    return;
                var found = FindSubModelElement(sme);
                if (found != null)
                    this.Remove(found);
            }

            // a little more business logic

            public T CreateSMEForCD<T>(ConceptDescription cd, string category = null, string idShort = null,
                string idxTemplate = null, int maxNum = 999, bool addSme = false) where T : SubmodelElement, new()
            {
                // access
                if (cd == null)
                    return null;

                // try to potentially figure out idShort
                var ids = cd.idShort;
                if (ids == null && cd.embeddedDataSpecification != null &&
                    cd.embeddedDataSpecification.dataSpecificationContent != null &&
                    cd.embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360 != null)
                    ids = cd.embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360.shortName?
                        .GetDefaultStr();
                if (idShort != null)
                    ids = idShort;
                if (ids == null)
                    return null;

                // unique?
                if (idxTemplate != null)
                    ids = this.IterateIdShortTemplateToBeUnique(idxTemplate, maxNum);

                // make a new instance
                var sme = new T()
                {
                    idShort = ids,
                    semanticId = new SemanticId(cd.GetReference())
                };
                if (category != null)
                    sme.category = category;

                // instantanously add it?
                if (addSme)
                    this.Add(sme);

                // give back
                return sme;
            }

            public T CreateSMEForIdShort<T>(string idShort, string category = null,
                string idxTemplate = null, int maxNum = 999, bool addSme = false) where T : SubmodelElement, new()
            {
                // access
                if (idShort == null)
                    return null;

                // try to potentially figure out idShort
                var ids = idShort;

                // unique?
                if (idxTemplate != null)
                    ids = this.IterateIdShortTemplateToBeUnique(idxTemplate, maxNum);

                // make a new instance
                var sme = new T() { idShort = ids };
                if (category != null)
                    sme.category = category;

                // instantanously add it?
                if (addSme)
                    this.Add(sme);

                // give back
                return sme;
            }
        }

        public interface IManageSubmodelElements
        {
            void Add(SubmodelElement sme);
            void Remove(SubmodelElement sme);
        }

        public class Submodel : Identifiable, IManageSubmodelElements,
                                    System.IDisposable, IGetReference, IEnumerateChildren
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // members

            // do this in order to be IDisposable, that is: suitable for (using)
            void System.IDisposable.Dispose() { }
            public void GetData() { }
            // from hasDataSpecification:
            [XmlElement(ElementName = "hasDataSpecification")]
            public HasDataSpecification hasDataSpecification = null;
            // from HasKind
            [XmlElement(ElementName = "kind")]
            [JsonIgnore]
            public ModelingKind kind = new ModelingKind();
            // from hasSemanticId:
            [XmlElement(ElementName = "semanticId")]
            public SemanticId semanticId = new SemanticId();
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
            // from Qualifiable:
            [XmlArray("qualifier")]
            [XmlArrayItem("qualifier")]
            public QualifierCollection qualifiers = null;

            // from this very class
            [JsonIgnore]
            public SubmodelElementWrapperCollection submodelElements = null;

            [XmlIgnore]
            [JsonProperty(PropertyName = "submodelElements")]

            public SubmodelElement[] JsonSubmodelElements
            {
                get
                {
                    var res = new List<SubmodelElement>();
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

#if UseAasxCompatibilityModels
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
#endif

            public static Submodel CreateNew(string idType, string id, string version = null, string revision = null)
            {
                var s = new Submodel() { identification = new Identification(idType, id) };
                if (version != null)
                {
                    if (s.administration == null)
                        s.administration = new Administration();
                    s.administration.version = version;
                    s.administration.revision = revision;
                }
                return (s);
            }

            // from IEnumarateChildren
            public IEnumerable<SubmodelElementWrapper> EnumerateChildren()
            {
                if (this.submodelElements != null)
                    foreach (var smw in this.submodelElements)
                        yield return smw;
            }

            public void AddChild(SubmodelElementWrapper smw)
            {
                if (smw == null)
                    return;
                if (this.submodelElements == null)
                    this.submodelElements = new SubmodelElementWrapperCollection();
                this.submodelElements.Add(smw);
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

            public void Remove(SubmodelElement sme)
            {
                if (submodelElements != null)
                    submodelElements.Remove(sme);
            }

            // further

            public override string GetElementName()
            {
                return "Submodel";
            }

            public Reference GetReference()
            {
                SubmodelRef l = new SubmodelRef();
                l.Keys.Add(
                    Key.CreateNew(
                        this.GetElementName(), true, this.identification.idType, this.identification.id));
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
                    return new Key(this.GetElementName(), true, this.identification?.idType, this.identification?.id);
                else
                    return this.semanticId?.GetAsExactlyOneKey();
            }

            public void AddDataSpecification(Key k)
            {
                if (hasDataSpecification == null)
                    hasDataSpecification = new HasDataSpecification();
                var r = new Reference();
                r.Keys.Add(k);
                hasDataSpecification.reference.Add(r);
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

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
                if (administration != null)
                    caption += "V" + administration.version + "." + administration.revision;
                var info = "";
                if (identification != null)
                    info = $"[{identification.idType}, {identification.id}]";
                return Tuple.Create(caption, info);
            }

            public override string ToString()
            {
                var ci = ToCaptionInfo();
                return string.Format("{0}{1}", ci.Item1, (ci.Item2 != "") ? " / " + ci.Item2 : "");
            }

            // Recursing


            public void RecurseOnSubmodelElements(
                object state, Action<object, List<SubmodelElement>, SubmodelElement> lambda)
            {
                this.submodelElements?.RecurseOnSubmodelElements(state, null, lambda);
            }

            // Parents stuff

            private static void SetParentsForSME(Referable parent, SubmodelElement se)
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

        }

        //
        // Derived from SubmodelElements
        //

        public class DataElement : SubmodelElement
        {
            public static string ValueType_STRING = "string";
            public static string ValueType_DATE = "string";
            public static string ValueType_BOOLEAN = "date";

            public static string[] ValueTypeItems = new string[] {
                        "anyType", "complexType", "anySimpleType", "anyAtomicType", "anyURI", "base64Binary",
                        "boolean", "date", "dateTime",
                        "dateTimeStamp", "decimal", "integer", "long", "int", "short", "byte", "nonNegativeInteger",
                        "positiveInteger",
                        "unsignedLong", "unsignedShort", "unsignedByte", "nonPositiveInteger", "negativeInteger",
                        "double", "duration",
                        "dayTimeDuration", "yearMonthDuration", "float", "hexBinary", "string", "langString", "time" };

            public DataElement() { }

            public DataElement(SubmodelElement src) : base(src) { }

            public DataElement(DataElement src) : base(src) { }

#if UseAasxCompatibilityModels
            public DataElement(AasxCompatibilityModels.AdminShellV10.DataElement src)
                : base(src)
            { }
#endif

            public override string GetElementName()
            {
                return "DataElement";
            }
        }

        public class JsonValueTypeCast
        {

            public class JsonDataObjectType
            {
                [JsonProperty(PropertyName = "name")]
                public string name = "";
            }

            [JsonProperty(PropertyName = "dataObjectType")]
            public JsonDataObjectType dataObjectType = new JsonDataObjectType();

            public JsonValueTypeCast(string name)
            {
                this.dataObjectType.name = name;
            }
        }

        public class Property : DataElement
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // members

            [MetaModelName("Property.valueType")]
            [TextSearchable]
            [JsonIgnore]
            public string valueType = "";
            [XmlIgnore]
            [JsonProperty(PropertyName = "valueType")]
            public JsonValueTypeCast JsonValueType
            {
                get { return new JsonValueTypeCast(this.valueType); }
                set { this.valueType = value?.dataObjectType?.name; }
            }


            [MetaModelName("Property.value")]
            [TextSearchable]
            public string value = "";
            public Reference valueId = null;

            // constructors

            public Property() { }

            public Property(SubmodelElement src) : base(src) { }

            public Property(Property src)
                : base(src)
            {
                this.valueType = src.valueType;
                this.value = src.value;
                if (src.valueId != null)
                    src.valueId = new Reference(src.valueId);
            }

#if UseAasxCompatibilityModels
            public Property(AasxCompatibilityModels.AdminShellV10.Property src)
                : base(src)
            {
                this.valueType = src.valueType;
                this.value = src.value;
                if (src.valueId != null)
                    this.valueId = new Reference(src.valueId);
            }
#endif

            public static Property CreateNew(string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new Property();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            public Property Set(string valueType = "", string value = "")
            {
                this.valueType = valueType;
                this.value = value;
                return this;
            }

            public Property Set(string type, bool local, string idType, string value)
            {
                this.valueId = Reference.CreateNew(Key.CreateNew(type, local, idType, value));
                return this;
            }

            public override string GetElementName()
            {
                return "Property";
            }

            public override string ValueAsText(string defaultLang = null)
            {
                return "" + value;
            }

            public bool IsTrue()
            {
                if (this.valueType?.Trim().ToLower() == "boolean")
                {
                    var v = "" + this.value?.Trim().ToLower();
                    if (v == "true" || v == "1")
                        return true;
                }
                return false;
            }
        }

        public class MultiLanguageProperty : DataElement
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // members

            public LangStringSet value = new LangStringSet();
            public Reference valueId = null;

            // constructors

            public MultiLanguageProperty() { }

            public MultiLanguageProperty(SubmodelElement src) : base(src) { }

            public MultiLanguageProperty(MultiLanguageProperty src)
                : base(src)
            {
                this.value = new LangStringSet(src.value);
                if (src.valueId != null)
                    src.valueId = new Reference(src.valueId);
            }

#if UseAasxCompatibilityModels
            // not available in V1.0
#endif

            public static MultiLanguageProperty CreateNew(
                string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new MultiLanguageProperty();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            public override string GetElementName()
            {
                return "MultiLanguageProperty";
            }

            public MultiLanguageProperty Set(ListOfLangStr ls)
            {
                this.value = new LangStringSet(ls);
                return this;
            }

            public MultiLanguageProperty Set(LangStr ls)
            {
                if (this.value == null)
                    this.value = new LangStringSet();
                this.value.Add(ls);
                return this;
            }

            public MultiLanguageProperty Set(string lang, string str)
            {
                return this.Set(new LangStr(lang, str));
            }

            public override string ValueAsText(string defaultLang = null)
            {
                return "" + value?.GetDefaultStr(defaultLang);
            }

        }

        public class Range : DataElement
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // members

            [MetaModelName("Range.valueType")]
            [TextSearchable]
            [JsonIgnore]
            [CountForHash]
            public string valueType = "";

            [XmlIgnore]
            [JsonProperty(PropertyName = "valueType")]
            public JsonValueTypeCast JsonValueType
            {
                get { return new JsonValueTypeCast(this.valueType); }
                set { this.valueType = value?.dataObjectType?.name; }
            }

            [MetaModelName("Range.min")]
            [TextSearchable]
            [CountForHash]
            public string min = "";

            [MetaModelName("Range.max")]
            [TextSearchable]
            [CountForHash]
            public string max = "";

            // constructors

            public Range() { }

            public Range(SubmodelElement src) : base(src) { }

            public Range(Range src)
                : base(src)
            {
                this.valueType = src.valueType;
                this.min = src.min;
                this.max = src.max;
            }

#if UseAasxCompatibilityModels
            // not available in V1.0
#endif

            public static Range CreateNew(string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new Range();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            public override string GetElementName()
            {
                return "Range";
            }

            public override string ValueAsText(string defaultLang = null)
            {
                return "" + min + " .. " + max;
            }

        }

        public class Blob : DataElement
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // members

            [MetaModelName("Blob.mimeType")]
            [TextSearchable]
            [CountForHash]
            public string mimeType = "";

            [MetaModelName("Blob.value")]
            [TextSearchable]
            [CountForHash]
            public string value = "";

            // constructors

            public Blob() { }

            public Blob(SubmodelElement src) : base(src) { }

            public Blob(Blob src)
                : base(src)
            {
                this.mimeType = src.mimeType;
                this.value = src.value;
            }

#if UseAasxCompatibilityModels
            public Blob(AasxCompatibilityModels.AdminShellV10.Blob src)
                : base(src)
            {
                this.mimeType = src.mimeType;
                this.value = src.value;
            }
#endif

            public static Blob CreateNew(string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new Blob();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            public void Set(string mimeType = "", string value = "")
            {
                this.mimeType = mimeType;
                this.value = value;
            }

            public override string GetElementName()
            {
                return "Blob";
            }

        }

        public class File : DataElement
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // members

            [MetaModelName("File.mimeType")]
            [TextSearchable]
            [CountForHash]
            public string mimeType = "";

            [MetaModelName("File.value")]
            [TextSearchable]
            [CountForHash]
            public string value = "";

            // constructors

            public File() { }

            public File(SubmodelElement src) : base(src) { }

            public File(File src)
                : base(src)
            {
                this.mimeType = src.mimeType;
                this.value = src.value;
            }

#if UseAasxCompatibilityModels
            public File(AasxCompatibilityModels.AdminShellV10.File src)
                : base(src)
            {
                this.mimeType = src.mimeType;
                this.value = src.value;
            }
#endif

            public static File CreateNew(string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new File();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            public void Set(string mimeType = "", string value = "")
            {
                this.mimeType = mimeType;
                this.value = value;
            }

            public override string GetElementName()
            {
                return "File";
            }

            public static string[] GetPopularMimeTypes()
            {
                return
                    new[] {
                    System.Net.Mime.MediaTypeNames.Text.Plain,
                    System.Net.Mime.MediaTypeNames.Text.Xml,
                    System.Net.Mime.MediaTypeNames.Text.Html,
                    "application/json",
                    "application/rdf+xml",
                    System.Net.Mime.MediaTypeNames.Application.Pdf,
                    System.Net.Mime.MediaTypeNames.Image.Jpeg,
                    "image/png",
                    System.Net.Mime.MediaTypeNames.Image.Gif,
                    "application/iges",
                    "application/step"
                    };
            }

            public override string ValueAsText(string defaultLang = null)
            {
                return "" + value;
            }
        }

        public class ReferenceElement : DataElement
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // members

            public Reference value = new Reference();

            // constructors

            public ReferenceElement() { }

            public ReferenceElement(SubmodelElement src) : base(src) { }

            public ReferenceElement(ReferenceElement src)
                : base(src)
            {
                if (src.value != null)
                    this.value = new Reference(src.value);
            }

#if UseAasxCompatibilityModels
            public ReferenceElement(AasxCompatibilityModels.AdminShellV10.ReferenceElement src)
                : base(src)
            {
                if (src.value != null)
                    this.value = new Reference(src.value);
            }
#endif

            public static ReferenceElement CreateNew(
                string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new ReferenceElement();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            public void Set(Reference value = null)
            {
                this.value = value;
            }

            public override string GetElementName()
            {
                return "ReferenceElement";
            }

        }

        public class RelationshipElement : DataElement
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // members

            public Reference first = new Reference();
            public Reference second = new Reference();

            // constructors

            public RelationshipElement() { }

            public RelationshipElement(SubmodelElement src) : base(src) { }

            public RelationshipElement(RelationshipElement src)
                : base(src)
            {
                if (src.first != null)
                    this.first = new Reference(src.first);
                if (src.second != null)
                    this.second = new Reference(src.second);
            }

#if UseAasxCompatibilityModels
            public RelationshipElement(AasxCompatibilityModels.AdminShellV10.RelationshipElement src)
                : base(src)
            {
                if (src.first != null)
                    this.first = new Reference(src.first);
                if (src.second != null)
                    this.second = new Reference(src.second);
            }
#endif

            public static RelationshipElement CreateNew(
                string idShort = null, string category = null, Key semanticIdKey = null, Reference first = null,
                Reference second = null)
            {
                var x = new RelationshipElement();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                x.first = first;
                x.second = second;
                return (x);
            }

            public void Set(Reference first = null, Reference second = null)
            {
                this.first = first;
                this.second = second;
            }

            public override string GetElementName()
            {
                return "RelationshipElement";
            }
        }

        public class AnnotatedRelationshipElement : RelationshipElement, IEnumerateChildren
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // members

            // from this very class

            [JsonIgnore]
            [SkipForHash] // do NOT count children!
            [XmlArray("annotations")]
            [XmlArrayItem("dataElement")]
            public DataElementWrapperCollection annotations = null;

            [XmlIgnore]
            [JsonProperty(PropertyName = "annotations")]
            public DataElement[] JsonAnotations
            {
                get
                {
                    var res = new List<DataElement>();
                    if (annotations != null)
                        foreach (var smew in annotations)
                            if (smew.submodelElement is DataElement de)
                                res.Add(de);
                    return res.ToArray();
                }
                set
                {
                    if (value != null)
                    {
                        this.annotations = new DataElementWrapperCollection();
                        foreach (var x in value)
                        {
                            var smew = new SubmodelElementWrapper() { submodelElement = x };
                            this.annotations.Add(smew);
                        }
                    }
                }
            }

            // constructors

            public AnnotatedRelationshipElement() { }

            public AnnotatedRelationshipElement(SubmodelElement src) : base(src) { }

            public AnnotatedRelationshipElement(AnnotatedRelationshipElement src)
                : base(src)
            {
                if (src.first != null)
                    this.first = new Reference(src.first);
                if (src.second != null)
                    this.second = new Reference(src.second);
                if (src.annotations != null)
                    this.annotations = new DataElementWrapperCollection(src.annotations);
            }

            public new static AnnotatedRelationshipElement CreateNew(
                string idShort = null, string category = null, Key semanticIdKey = null,
                Reference first = null, Reference second = null)
            {
                var x = new AnnotatedRelationshipElement();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                x.first = first;
                x.second = second;
                return (x);
            }

            // enumerates its children

            public IEnumerable<SubmodelElementWrapper> EnumerateChildren()
            {
                if (this.annotations != null)
                    foreach (var smw in this.annotations)
                        yield return smw;
            }

            public void AddChild(SubmodelElementWrapper smw)
            {
                if (smw == null || !(smw.submodelElement is DataElement))
                    return;
                if (this.annotations == null)
                    this.annotations = new DataElementWrapperCollection();
                this.annotations.Add(smw);
            }

            // further 

            public new void Set(Reference first = null, Reference second = null)
            {
                this.first = first;
                this.second = second;
            }

            public override string GetElementName()
            {
                return "AnnotatedRelationshipElement";
            }
        }

        public class Capability : SubmodelElement
        {
            public Capability() { }

            public Capability(SubmodelElement src) : base(src) { }

            public Capability(Capability src)
                : base(src)
            { }

            public override string GetElementName()
            {
                return "Capability";
            }
        }


        public class SubmodelElementCollection : SubmodelElement, IManageSubmodelElements, IEnumerateChildren
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // values == SMEs
            [JsonIgnore]
            [SkipForHash] // do NOT count children!
            public SubmodelElementWrapperCollection value = new SubmodelElementWrapperCollection();

            [XmlIgnore]
            [JsonProperty(PropertyName = "value")]
            public SubmodelElement[] JsonValue
            {
                get
                {
                    var res = new List<SubmodelElement>();
                    if (value != null)
                        foreach (var smew in value)
                            res.Add(smew.submodelElement);
                    return res.ToArray();
                }
                set
                {
                    if (value != null)
                    {
                        this.value = new SubmodelElementWrapperCollection();
                        foreach (var x in value)
                        {
                            var smew = new SubmodelElementWrapper() { submodelElement = x };
                            this.value.Add(smew);
                        }
                    }
                }
            }

            // constant members
            public bool ordered = false;
            public bool allowDuplicates = false;

            // enumartes its children

            public IEnumerable<SubmodelElementWrapper> EnumerateChildren()
            {
                if (this.value != null)
                    foreach (var smw in this.value)
                        yield return smw;
            }

            public void AddChild(SubmodelElementWrapper smw)
            {
                if (smw == null)
                    return;
                if (this.value == null)
                    this.value = new SubmodelElementWrapperCollection();
                this.value.Add(smw);
            }

            // constructors

            public SubmodelElementCollection() { }

            public SubmodelElementCollection(SubmodelElement src) : base(src) { }

            public SubmodelElementCollection(SubmodelElementCollection src, bool shallowCopy = false)
                : base(src)
            {
                this.ordered = src.ordered;
                this.allowDuplicates = src.allowDuplicates;
                if (!shallowCopy)
                    foreach (var smw in src.value)
                        value.Add(new SubmodelElementWrapper(smw.submodelElement));
            }

#if UseAasxCompatibilityModels
            public SubmodelElementCollection(
                AasxCompatibilityModels.AdminShellV10.SubmodelElementCollection src, bool shallowCopy = false)
                : base(src)
            {
                this.ordered = src.ordered;
                this.allowDuplicates = src.allowDuplicates;
                if (!shallowCopy)
                    foreach (var smw in src.value)
                        value.Add(new SubmodelElementWrapper(smw.submodelElement));
            }
#endif

            public static SubmodelElementCollection CreateNew(
                string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new SubmodelElementCollection();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            // from IManageSubmodelElements
            public void Add(SubmodelElement sme)
            {
                if (value == null)
                    value = new SubmodelElementWrapperCollection();
                var sew = new SubmodelElementWrapper();
                sme.parent = this; // track parent here!
                sew.submodelElement = sme;
                value.Add(sew);
            }

            public void Remove(SubmodelElement sme)
            {
                if (value != null)
                    value.Remove(sme);
            }

            // further

            public void Set(bool allowDuplicates = false, bool ordered = false)
            {
                this.allowDuplicates = allowDuplicates;
                this.ordered = ordered;
            }

            public SubmodelElementWrapper FindFirstIdShort(string idShort)
            {
                return this.value?.FindFirstIdShort(idShort);
            }

            public T CreateSMEForCD<T>(ConceptDescription cd, string category = null, string idShort = null,
                string idxTemplate = null, int maxNum = 999, bool addSme = false) where T : SubmodelElement, new()
            {
                if (this.value == null)
                    this.value = new SubmodelElementWrapperCollection();
                return this.value.CreateSMEForCD<T>(cd, category, idShort, idxTemplate, maxNum, addSme);
            }

            public T CreateSMEForIdShort<T>(string idShort, string category = null,
                string idxTemplate = null, int maxNum = 999, bool addSme = false) where T : SubmodelElement, new()
            {
                if (this.value == null)
                    this.value = new SubmodelElementWrapperCollection();
                return this.value.CreateSMEForIdShort<T>(idShort, category, idxTemplate, maxNum, addSme);
            }


            public override string GetElementName()
            {
                return "SubmodelElementCollection";
            }
        }

        public class OperationVariable
        {
            public enum Direction { In, Out, InOut };

            // Note: for OperationVariable, the values of the SubmodelElement itself ARE NOT TO BE USED!
            // only the SME attributes of "value" are counting

            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // members
            public SubmodelElementWrapper value = null;

            // constructors

            public OperationVariable()
            {
            }

            public OperationVariable(OperationVariable src, bool shallowCopy = false)
            {
                this.value = new SubmodelElementWrapper(src.value.submodelElement, shallowCopy);
            }

#if UseAasxCompatibilityModels
            public OperationVariable(
                AasxCompatibilityModels.AdminShellV10.OperationVariable src, bool shallowCopy = false)
            {
                this.value = new SubmodelElementWrapper(src.value.submodelElement, shallowCopy);
            }
#endif

            public OperationVariable(SubmodelElement elem)
                : base()
            {
                this.value = new SubmodelElementWrapper(elem);
            }

            public string GetElementName()
            {
                return "OperationVariable";
            }
        }

        public class Operation : SubmodelElement, IEnumerateChildren
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // members
            [JsonIgnore]
            [XmlElement(ElementName = "inputVariable")]
            [SkipForHash] // do NOT count children!
            public List<OperationVariable> inputVariable = new List<OperationVariable>();

            [JsonIgnore]
            [XmlElement(ElementName = "outputVariable")]
            [SkipForHash] // do NOT count children!
            public List<OperationVariable> outputVariable = new List<OperationVariable>();

            [JsonIgnore]
            [XmlElement(ElementName = "inoutputVariable")]
            [SkipForHash] // do NOT count children!
            public List<OperationVariable> inoutputVariable = new List<OperationVariable>();

            [XmlIgnore]
            // MICHA 190504: enabled JSON operation variables!
            [JsonProperty(PropertyName = "inputVariable")]
            // [JsonIgnore]
            public OperationVariable[] JsonInputVariable
            {
                get { return inputVariable?.ToArray(); }
                set { inputVariable = (value != null) ? new List<OperationVariable>(value) : null; }
            }

            [XmlIgnore]
            [JsonProperty(PropertyName = "outputVariable")]
            // MICHA 190504: enabled JSON operation variables!
            // [JsonIgnore]
            public OperationVariable[] JsonOutputVariable
            {
                get { return outputVariable?.ToArray(); }
                set { outputVariable = (value != null) ? new List<OperationVariable>(value) : null; }
            }

            [XmlIgnore]
            [JsonProperty(PropertyName = "inoutputVariable")]
            // MICHA 190504: enabled JSON operation variables!
            // [JsonIgnore]
            public OperationVariable[] JsonInOutputVariable
            {
                get { return inoutputVariable?.ToArray(); }
                set { inoutputVariable = (value != null) ? new List<OperationVariable>(value) : null; }
            }

            public List<OperationVariable> this[OperationVariable.Direction dir]
            {
                get
                {
                    if (dir == OperationVariable.Direction.In)
                        return inputVariable;
                    else
                    if (dir == OperationVariable.Direction.Out)
                        return outputVariable;
                    else
                        return inoutputVariable;
                }
                set
                {
                    if (dir == OperationVariable.Direction.In)
                        inputVariable = value;
                    else
                    if (dir == OperationVariable.Direction.Out)
                        outputVariable = value;
                    else
                        inoutputVariable = value;
                }
            }

            public List<OperationVariable> this[int dir]
            {
                get
                {
                    return (dir == 0) ? inputVariable : outputVariable;
                }
                set
                {
                    if (dir == 0)
                        inputVariable = value;
                    else
                        outputVariable = value;
                }
            }

            public static SubmodelElementWrapperCollection GetWrappers(List<OperationVariable> ovl)
            {
                var res = new SubmodelElementWrapperCollection();
                foreach (var ov in ovl)
                    if (ov.value != null)
                        res.Add(ov.value);
                return res;
            }

            // enumartes its children
            public IEnumerable<SubmodelElementWrapper> EnumerateChildren()
            {
                if (this.inputVariable != null)
                    foreach (var smw in this.inputVariable)
                        yield return smw?.value;

                if (this.outputVariable != null)
                    foreach (var smw in this.outputVariable)
                        yield return smw?.value;

                if (this.inoutputVariable != null)
                    foreach (var smw in this.inoutputVariable)
                        yield return smw?.value;
            }

            public void AddChild(SubmodelElementWrapper smw)
            {
                // not enough information to select list of children
            }

            // constructors

            public Operation() { }

            public Operation(SubmodelElement src) : base(src) { }

            public Operation(Operation src)
                : base(src)
            {
                for (int i = 0; i < 2; i++)
                    if (src[i] != null)
                    {
                        if (this[i] == null)
                            this[i] = new List<OperationVariable>();
                        foreach (var ov in src[i])
                            this[i].Add(new OperationVariable(ov));
                    }
            }

#if UseAasxCompatibilityModels
            public Operation(AasxCompatibilityModels.AdminShellV10.Operation src)
                : base(src)
            {
                for (int i = 0; i < 2; i++)
                    if (src[i] != null)
                    {
                        if (this[i] == null)
                            this[i] = new List<OperationVariable>();
                        foreach (var ov in src[i])
                            this[i].Add(new OperationVariable(ov));
                    }
            }
#endif

            public override string GetElementName()
            {
                return "Operation";
            }
        }

        public class Entity : SubmodelElement, IManageSubmodelElements, IEnumerateChildren
        {
            public enum EntityTypeEnum { CoManagedEntity = 0, SelfManagedEntity = 1, Undef = 3 }
            public static string[] EntityTypeNames = new string[] { "CoManagedEntity", "SelfManagedEntity" };

            // for JSON only

            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // from this very class

            [JsonIgnore]
            [SkipForHash] // do NOT count children!
            public SubmodelElementWrapperCollection statements = new SubmodelElementWrapperCollection();

            [XmlIgnore]
            [JsonProperty(PropertyName = "statements")]
            public SubmodelElement[] JsonStatements
            {
                get
                {
                    var res = new List<SubmodelElement>();
                    if (statements != null)
                        foreach (var smew in statements)
                            res.Add(smew.submodelElement);
                    return res.ToArray();
                }
                set
                {
                    if (value != null)
                    {
                        this.statements = new SubmodelElementWrapperCollection();
                        foreach (var x in value)
                        {
                            var smew = new SubmodelElementWrapper() { submodelElement = x };
                            this.statements.Add(smew);
                        }
                    }
                }
            }

            // further members

            [CountForHash]
            public string entityType = "";

            [JsonProperty(PropertyName = "asset")]
            public AssetRef assetRef = null;

            // enumerates its children

            public IEnumerable<SubmodelElementWrapper> EnumerateChildren()
            {
                if (this.statements != null)
                    foreach (var smw in this.statements)
                        yield return smw;
            }

            public void AddChild(SubmodelElementWrapper smw)
            {
                if (smw == null)
                    return;
                if (this.statements == null)
                    this.statements = new SubmodelElementWrapperCollection();
                this.statements.Add(smw);
            }

            // constructors

            public Entity() { }

            public Entity(SubmodelElement src) : base(src) { }

            public Entity(Entity src)
                : base(src)
            {
                if (src.statements != null)
                {
                    this.statements = new SubmodelElementWrapperCollection();
                    foreach (var smw in src.statements)
                        this.statements.Add(new SubmodelElementWrapper(smw.submodelElement));
                }
                this.entityType = src.entityType;
                if (src.assetRef != null)
                    this.assetRef = new AssetRef(src.assetRef);
            }

            public Entity(EntityTypeEnum entityType, string idShort = null, AssetRef assetRef = null)
            {
                this.entityType = EntityTypeNames[(int)entityType];
                this.idShort = idShort;
                this.assetRef = assetRef;
            }

#if UseAasxCompatibilityModels
            // not available in V1.0
#endif

            public static Entity CreateNew(string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new Entity();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            // from IManageSubmodelElements
            public void Add(SubmodelElement sme)
            {
                if (statements == null)
                    statements = new SubmodelElementWrapperCollection();
                var sew = new SubmodelElementWrapper();
                sme.parent = this; // track parent here!
                sew.submodelElement = sme;
                statements.Add(sew);
            }

            public void Remove(SubmodelElement sme)
            {
                if (statements != null)
                    statements.Remove(sme);
            }

            // management of elememts

            public SubmodelElementWrapper FindSubmodelElementWrapper(string idShort)
            {
                if (this.statements == null)
                    return null;
                foreach (var smw in this.statements)
                    if (smw.submodelElement != null)
                        if (smw.submodelElement.idShort.Trim().ToLower() == idShort.Trim().ToLower())
                            return smw;
                return null;
            }

            // entity type

            public EntityTypeEnum GetEntityType()
            {
                EntityTypeEnum res = EntityTypeEnum.Undef;
                if (this.entityType != null && this.entityType.Trim().ToLower() == EntityTypeNames[0].ToLower())
                    res = EntityTypeEnum.CoManagedEntity;
                if (this.entityType != null && this.entityType.Trim().ToLower() == EntityTypeNames[1].ToLower())
                    res = EntityTypeEnum.SelfManagedEntity;
                return res;
            }

            // name

            public override string GetElementName()
            {
                return "Entity";
            }
        }

        public class BasicEvent : SubmodelElement
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // from this very class
            public Reference observed = new Reference();

            // constructors

            public BasicEvent() { }

            public BasicEvent(SubmodelElement src) : base(src) { }

            public BasicEvent(BasicEvent src)
                : base(src)
            {
                if (src.observed != null)
                    this.observed = new Reference(src.observed);
            }

#if UseAasxCompatibilityModels
            // not available in V1.0
#endif

            public static BasicEvent CreateNew(string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new BasicEvent();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            public override string GetElementName()
            {
                return "BasicEvent";
            }
        }

        //
        // Handling of packages
        //


    }

    #endregion
}
