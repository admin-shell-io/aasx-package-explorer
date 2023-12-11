/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable All .. as this is legacy code!

#if !DoNotUseAasxCompatibilityModels

namespace AasxCompatibilityModels
{
    #region Utils
    //
    // Utils
    //

    public class AdminShellUtilV10
    {
        public static string EvalToNonNullString(string fmt, object o, string elseString = "")
        {
            if (o == null)
                return elseString;
            return string.Format(fmt, o);
        }

        public static string EvalToNonEmptyString(string fmt, string o, string elseString = "")
        {
            if (o == "")
                return elseString;
            return string.Format(fmt, o);
        }

        public static string FilterFriendlyName(string src)
        {
            if (src == null)
                return null;
            return Regex.Replace(src, @"[^a-zA-Z0-9\-_]", "_");
        }

        public static bool HasWhitespace(string src)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));
            for (var i = 0; i < src.Length; i++)
                foreach (var c in src)
                    if (char.IsWhiteSpace(c))
                        return true;
            return false;
        }

        public static bool ComplyIdShort(string src)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));
            var res = true;
            foreach (var c in src)
                if (!Char.IsLetterOrDigit(c) && c != '_')
                    res = false;
            if (src.Length > 0 && !Char.IsLetter(src[0]))
                res = false;
            return res;
        }

        public static string ShortLocation(Exception ex)
        {
            if (ex == null || ex.StackTrace == null)
                return "";
            string[] lines = ex.StackTrace.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 1)
                return "";
            // search for " in "
            // as the most actual stacktrace might be a built-in function, this might not work and therefore
            // go down in the stack
            int currLine = 0;
            while (true)
            {
                // nothing found at all
                if (currLine >= lines.Length)
                    return "";
                // access current line
                var p = lines[currLine].IndexOf(" in ", StringComparison.Ordinal);
                if (p < 0)
                {
                    // advance to next oldest line
                    currLine++;
                    continue;
                }
                // search last "\" or "/", to get only filename portion and position
                p = lines[currLine].LastIndexOfAny(new[] { '\\', '/' });
                if (p < 0)
                {
                    // advance to next oldest line
                    currLine++;
                    continue;
                }
                // return this
                return lines[currLine].Substring(p);
            }
        }

    }

    #endregion


    #region AdminShell_V1_0

    /// <summary>
    /// Version of Details of Administration Shell Part 1 V1.0 published Nov/Dec/Jan 2018/19
    /// </summary>
    public class AdminShellV10
    {

        public class Identification
        {

            // members

            [XmlAttribute]
            public string idType = "";
            [XmlText]
            public string id = "";

            // constructors

            public Identification() { }

            public Identification(string idType, string id)
            {
                this.idType = idType;
                this.id = id;
            }

            public Identification(Identification src)
            {
                this.idType = src.idType;
                this.id = src.id;
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

            public string version = "";
            public string revision = "";

            // constructors

            public Administration() { }

            public Administration(Administration src)
            {
                this.version = src.version;
                this.revision = src.revision;
            }

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
            [XmlAttribute]
            public string type = "";
            [XmlAttribute]
            public bool local = false;

            [XmlAttribute]
            [JsonIgnore]
            public string idType = "";
            [XmlIgnore]
            [JsonProperty(PropertyName = "idType")]
            public string JsonIdType
            {
                get { return (idType == "idShort") ? "IdShort" : idType; }
                set { if (value == "IdShort") idType = "idShort"; else idType = value; }
            }

            [XmlText]
            public string value = "";

            [XmlIgnore]
            [JsonProperty(PropertyName = "index")]
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

            public Key(string type, bool local, string idType, string value)
            {
                this.type = type;
                this.local = local;
                this.idType = idType;
                this.value = value;
            }

            public static Key CreateNew(string type, bool local, string idType, string value)
            {
                var k = new Key();
                k.type = type;
                k.local = local;
                k.idType = idType;
                k.value = value;
                return (k);
            }

            public static Key GetFromRef(Reference r)
            {
                if (r == null || r.Count != 1)
                    return null;
                return r[0];
            }

            public override string ToString()
            {
                var local = (this.local) ? "Local" : "not Local";
                return $"[{this.type}, {local}, {this.idType}, {this.value}]";
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
            "Event",
            "Operation",
            "OperationVariable",
            "Property",
            "ReferenceElement",
            "RelationshipElement",
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
            "Event",
            "Operation",
            "OperationVariable",
            "Property",
            "ReferenceElement",
            "RelationshipElement",
            "SubmodelElement",
            "SubmodelElementCollection",
            "View"
        };

            public static string[] SubmodelElements = new string[] {
            "DataElement",
            "File",
            "Event",
            "Operation",
            "Property",
            "ReferenceElement",
            "RelationshipElement",
            "SubmodelElementCollection"};

            public static string[] IdentifiableElements = new string[] {
            "Asset",
            "AssetAdministrationShell",
            "ConceptDescription",
            "Submodel" };

            // use this in list to designate all of the above elements
            public static string AllElements = "All";

            // use this in list to designate the GlobalReference
            public static string GlobalReference = "GlobalReference";
            public static string ConceptDescription = "ConceptDescription";
            public static string SubmodelRef = "SubmodelRef";
            public static string Submodel = "Submodel";
            public static string Asset = "Asset";
            public static string AAS = "AssetAdministrationShell";

            public static string[] IdentifierTypeNames = new string[] { "IdShort", "Custom", "IRDI", "URI" };

            public enum IdentifierType { IdShort = 0, Custom, IRDI, URI };

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

        }

        // the whole class shall not be serialized by having it private
        public class KeyList
        {
            // members

            [XmlIgnore] // anyway, as it is privat
            private List<Key> key = new List<Key>();

            // getters / setters

            [XmlIgnore]
            public List<Key> Keys { get { return key; } }
            [XmlIgnore]
            public bool IsEmpty { get { return key == null || key.Count < 1; } }
            [XmlIgnore]
            public int Count { get { if (key == null) return 0; return key.Count; } }
            [XmlIgnore]
            public Key this[int index] { get { return key[index]; } }

            // constructors / creators

            public void Add(Key k)
            {
                key.Add(k);
            }

            public static KeyList CreateNew(Key k)
            {
                var kl = new KeyList();
                kl.Add(k);
                return kl;
            }

            public static KeyList CreateNew(string type, bool local, string idType, string value)
            {
                var kl = new KeyList();
                kl.Add(Key.CreateNew(type, local, idType, value));
                return kl;
            }

            // other

            public void NumberIndices()
            {
                if (this.Keys == null)
                    return;
                for (int i = 0; i < this.Keys.Count; i++)
                    this.Keys[i].index = i;
            }
        }

        [XmlType(TypeName = "reference")]
        public class Reference
        {

            // members

            [XmlIgnore] // anyway, as it is privat
            [JsonIgnore]
            private KeyList keys = new KeyList();

            // getters / setters

            [XmlArray("keys")]
            [XmlArrayItem("key")]
            [JsonIgnore]
            public List<Key> Keys { get { return keys?.Keys; } }
            [XmlIgnore]
            [JsonProperty(PropertyName = "keys")]
            public List<Key> JsonKeys
            {
                get
                {
                    keys?.NumberIndices();
                    return keys.Keys;
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
                    keys.Keys.Add(k);
            }

            public Reference(Reference src)
            {
                if (src != null)
                    foreach (var k in src.Keys)
                        keys.Add(new Key(k));
            }

            public Reference(SemanticId src)
            {
                if (src != null)
                    foreach (var k in src.Keys)
                        keys.Add(new Key(k));
            }

            public static Reference CreateNew(Key k)
            {
                if (k == null)
                    return null;
                var r = new Reference();
                r.keys.Keys.Add(k);
                return r;
            }

            public static Reference CreateNew(List<Key> k)
            {
                if (k == null)
                    return null;
                var r = new Reference();
                r.keys.Keys.AddRange(k);
                return r;
            }

            public static Reference CreateNew(string type, bool local, string idType, string value)
            {
                if (type == null || idType == null || value == null)
                    return null;
                var r = new Reference();
                r.keys.Keys.Add(Key.CreateNew(type, local, idType, value));
                return r;
            }

            public static Reference CreateIrdiReference(string irdi)
            {
                if (irdi == null)
                    return null;
                var r = new Reference();
                r.keys.Keys.Add(new Key(Key.GlobalReference, false, "IRDI", irdi));
                return r;
            }

            // further

            public bool IsExactlyOneKey(string type, bool local, string idType, string id)
            {
                if (keys == null || keys.Keys == null || keys.Count != 1)
                    return false;
                var k = keys.Keys[0];
                return k.type == type && k.local == local && k.idType == idType && k.value == id;
            }

            public bool MatchesTo(Identification other)
            {
                return (this.keys != null && this.keys.Count == 1
                    && this.keys[0].idType.Trim().ToLower() == other.idType.Trim().ToLower()
                    && this.keys[0].value.Trim().ToLower() == other.id.Trim().ToLower());
            }

            public bool MatchesTo(Reference other)
            {
                if (this.keys == null || other == null || other.keys == null || other.Count != this.Count)
                    return false;

                var same = true;
                for (int i = 0; i < this.Count; i++)
                    same = same
                        && this.keys[i].type.Trim().ToLower() == other.keys[i].type.Trim().ToLower()
                        && this.keys[i].local == other.keys[i].local
                        && this.keys[i].idType.Trim().ToLower() == other.keys[i].idType.Trim().ToLower()
                        && this.keys[i].value.Trim().ToLower() == other.keys[i].value.Trim().ToLower();

                return same;
            }

            public override string ToString()
            {
                var res = "";
                if (keys != null && keys.Keys != null)
                    foreach (var k in keys.Keys)
                        res += k.ToString() + ",";
                return res.TrimEnd(',');
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

            // translation

            public static AssetRef CreateNew(Reference r)
            {
                return (AssetRef)new Reference(r);
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

            public static new SubmodelRef CreateNew(string type, bool local, string idType, string value)
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
        }

        [XmlType(TypeName = "containedElementRef")]
        public class ContainedElementRef : Reference
        {
            // constructors

            public ContainedElementRef() { }
            public ContainedElementRef(ContainedElementRef src) : base(src) { }

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

        [XmlType(TypeName = "langString", Namespace = "http://www.admin-shell.io/1/0")]
        public class LangStr
        {

            // members

            [XmlAttribute(Namespace = "http://www.admin-shell.io/1/0")]
            [JsonProperty(PropertyName = "language")]
            public string lang = "";
            [XmlText]
            [JsonProperty(PropertyName = "text")]
            public string str = "";

            // constructors

            public LangStr() { }

            public LangStr(LangStr src)
            {
                this.lang = src.lang;
                this.str = src.str;
            }

            public static LangStr CreateNew(string lang, string str)
            {
                var l = new LangStr();
                l.lang = lang;
                l.str = str;
                return (l);
            }

            public static List<LangStr> CreateManyFromStringArray(string[] s)
            {
                var r = new List<LangStr>();
                var i = 0;
                while ((i + 1) < s.Length)
                {
                    r.Add(LangStr.CreateNew(s[i], s[i + 1]));
                    i += 2;
                }
                return r;
            }
        }

        public class Description
        {

            // members

            [XmlElement(ElementName = "langString")]
            public List<LangStr> langString = new List<LangStr>();

            // constructors

            public Description() { }

            public Description(Description src)
            {
                if (src != null)
                    foreach (var ls in src.langString)
                        langString.Add(new LangStr(ls));
            }
        }

        public class Kind
        {
            [XmlText]
            public string kind = "Instance";

            // getters / setters

            [XmlIgnore]
            [JsonIgnore]
            public bool IsInstance { get { return kind == null || kind.Trim().ToLower() == "instance"; } }

            [XmlIgnore]
            [JsonIgnore]
            public bool IsType { get { return kind != null && kind.Trim().ToLower() == "type"; } }

            // constructors / creators

            public Kind() { }

            public Kind(Kind src)
            {
                kind = src.kind;
            }

            public Kind(string kind)
            {
                this.kind = kind;
            }

            public static Kind CreateFrom(Kind k)
            {
                var res = new Kind();
                res.kind = k.kind;
                return res;
            }

            public static Kind CreateAsType()
            {
                var res = new Kind();
                res.kind = "Type";
                return res;
            }

            public static Kind CreateAsInstance()
            {
                var res = new Kind();
                res.kind = "Instance";
                return res;
            }
        }

        public class SemanticId
        {

            // members

            [XmlIgnore]
            [JsonIgnore]
            private KeyList keys = new KeyList();

            // getters / setters

            [XmlArray("keys")]
            [XmlArrayItem("key")]
            [JsonIgnore]
            public List<Key> Keys { get { return keys?.Keys; } }
            [XmlIgnore]
            [JsonProperty(PropertyName = "keys")]
            public List<Key> JsonKeys
            {
                get
                {
                    keys?.NumberIndices();
                    return keys.Keys;
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

            public override string ToString()
            {
                return Key.KeyListToString(keys.Keys);
            }

            // constructors / creators

            public SemanticId()
            {
            }

            public SemanticId(SemanticId src)
            {
                if (src != null)
                    foreach (var k in src.Keys)
                        keys.Add(k);
            }

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

            // matching

            public bool Matches(string type, bool local, string idType, string value)
            {
                if (this.Count == 1
                    && this.keys[0].type.ToLower().Trim() == type.ToLower().Trim()
                    && this.keys[0].local == local
                    && this.keys[0].idType.ToLower().Trim() == idType.ToLower().Trim()
                    && this.keys[0].value.ToLower().Trim() == value.ToLower().Trim())
                    return true;
                return false;
            }
        }

        public class Referable
        {

            // members

            public string idShort = null;
            public string category = null;

            [XmlElement(ElementName = "description")]
            [JsonIgnore]
            public Description description = null;
            [XmlIgnore]
            [JsonProperty(PropertyName = "descriptions")]
            public List<LangStr> JsonDescription
            {
                get
                {
                    if (description == null)
                        return null;
                    return description.langString;
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
            public Referable parent = null;

            public static string[] ReferableCategoryNames = new string[] { "CONSTANT", "PARAMETER", "VARIABLE" };

            // constructors

            public Referable() { }

            public Referable(Referable src)
            {
                this.idShort = src.idShort;
                this.category = src.category;
                if (src.description != null)
                    this.description = new Description(src.description);
            }

            public void AddDescription(string lang, string str)
            {
                if (description == null)
                    description = new Description();
                description.langString.Add(LangStr.CreateNew(lang, str));
            }

            public virtual string GetElementName()
            {
                return "GlobalReference"; // not correct, but this method wasn't overridden correctly
            }

            public string GetFriendlyName()
            {
                return AdminShellUtilV10.FilterFriendlyName(this.idShort);
            }

            public void CollectReferencesByParent(List<Key> refs)
            {
                // check, if this is identifiable
                if (this is Identifiable)
                {
                    var idf = this as Identifiable;
                    var k = Key.CreateNew(
                        idf.GetElementName(), true, idf.identification.idType, idf.identification.id);
                    refs.Insert(0, k);
                }
                else
                {
                    var k = Key.CreateNew(this.GetElementName(), true, "idShort", this.idShort);
                    refs.Insert(0, k);
                    // recurse upwards!
                    if (parent != null && parent is Referable)
                        (this.parent).CollectReferencesByParent(refs);
                }
            }

            public string CollectIdShortByParent()
            {
                // recurse first
                var head = "";
                if (!(this is Identifiable) && this.parent != null && this.parent is Referable)
                    // can go up
                    head = this.parent.CollectIdShortByParent() + "/";
                // add own
                var myid = "<no id-Short!>";
                if (this.idShort != null && this.idShort.Trim() != "")
                    myid = this.idShort.Trim();
                // together
                return head + myid;
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
                if (src.identification != null)
                    this.identification = new Identification(src.identification);
                if (src.administration != null)
                    this.administration = new Administration(src.administration);
            }

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
                    return AdminShellUtilV10.FilterFriendlyName(this.identification.id);
                return AdminShellUtilV10.FilterFriendlyName(this.idShort);
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

            // constructurs

            public AdministrationShell() { }

            public AdministrationShell(AdministrationShell src)
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

            public static AdministrationShell CreateNew(
                string idType, string id, string version = null, string revision = null)
            {
                var s = new AdministrationShell();
                s.identification.idType = idType;
                s.identification.id = id;
                if (version != null)
                    s.SetAdminstration(version, revision);
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
                var caption = AdminShellUtilV10.EvalToNonNullString("\"{0}\" ", idShort, "\"AAS\"");
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
                    if (r.MatchesTo(refid))
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
                    if (r.MatchesTo(newref))
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
            public Kind kind = new Kind();
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
                        kind = new Kind();
                    kind.kind = value;
                }
            }
            // from this very class
            [XmlElement(ElementName = "assetIdentificationModelRef")]
            public SubmodelRef assetIdentificationModelRef = null;

            // constructors

            public Asset() { }

            public Asset(Asset src)
                : base(src)
            {
                if (src.hasDataSpecification != null)
                    this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                if (src.kind != null)
                    this.kind = new Kind(src.kind);
                if (src.assetIdentificationModelRef != null)
                    this.assetIdentificationModelRef = new SubmodelRef(src.assetIdentificationModelRef);
            }

            // Getter & setters

            public AssetRef GetReference()
            {
                var r = new AssetRef();
                r.Keys.Add(
                    Key.CreateNew(this.GetElementName(), true, this.identification.idType, this.identification.id));
                return r;
            }

            public override string GetElementName()
            {
                return "Asset";
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtilV10.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
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
            public ContainedElements containedElements = null;
            [XmlIgnore]
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
                get
                {
                    if (
containedElements == null) return null; return containedElements[index];
                }
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

            public static View CreateNew(string idShort)
            {
                var v = new View();
                v.idShort = idShort;
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
                var caption = AdminShellUtilV10.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
                var info = "";
                if (this.semanticId != null)
                    info = Key.KeyListToString(this.semanticId.Keys);
                if (this.containedElements != null && this.containedElements.reference != null)
                    info =
                        (info + " ").Trim() + String.Format("({0} elements)", this.containedElements.reference.Count);
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

        public class LangStringIEC61360
        {

            // members

            [XmlElement(ElementName = "langString", Namespace = "http://www.admin-shell.io/aas/1/0")]
            public List<LangStr> langString = new List<LangStr>();

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

            public LangStringIEC61360() { }

            public LangStringIEC61360(LangStringIEC61360 src)
            {
                if (src.langString != null)
                    foreach (var ls in src.langString)
                        this.langString.Add(new LangStr(ls));
            }

            // converter

            public static LangStringIEC61360 CreateFrom(List<LangStr> src)
            {
                var res = new LangStringIEC61360();
                if (src != null)
                    foreach (var ls in src)
                        res.langString.Add(new LangStr(ls));
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
            public List<Key> Keys { get { return keys?.Keys; } }
            [XmlIgnore]
            [JsonProperty(PropertyName = "keys")]
            public List<Key> JsonKeys
            {
                get
                {
                    keys?.NumberIndices();
                    return keys.Keys;
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
            public Key this[int index] { get { return keys.Keys[index]; } }

            // constructors / creators

            public UnitId() { }

            public UnitId(UnitId src)
            {
                if (src.keys != null)
                    foreach (var k in src.Keys)
                        this.keys.Add(new Key(k));
            }

            public static UnitId CreateNew(string type, bool local, string idType, string value)
            {
                var u = new UnitId();
                u.keys.Keys.Add(Key.CreateNew(type, local, idType, value));
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

        [XmlRoot(Namespace = "http://www.admin-shell.io/IEC61360/1/0")]
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
            public LangStringIEC61360 preferredName = new LangStringIEC61360();
            public string shortName = "";
            public string unit = "";
            public UnitId unitId = null;
            public string valueFormat = null;
            public List<LangStr> sourceOfDefinition = new List<LangStr>();
            public string symbol = null;
            public string dataType = "";
            public LangStringIEC61360 definition = new LangStringIEC61360();

            // getter / setters

            // constructors

            public DataSpecificationIEC61360() { }

            public DataSpecificationIEC61360(DataSpecificationIEC61360 src)
            {
                if (src.preferredName != null)
                    this.preferredName = new LangStringIEC61360(src.preferredName);
                this.shortName = src.shortName;
                this.unit = src.unit;
                if (src.unitId != null)
                    this.unitId = new UnitId(src.unitId);
                this.valueFormat = src.valueFormat;
                if (src.sourceOfDefinition != null)
                    foreach (var sod in src.sourceOfDefinition)
                        this.sourceOfDefinition.Add(sod);
                this.symbol = src.symbol;
                this.dataType = src.dataType;
                if (src.definition != null)
                    this.definition = new LangStringIEC61360(src.definition);
            }

            public static DataSpecificationIEC61360 CreateNew(
                string[] preferredName = null,
                string shortName = "",
                string unit = "",
                UnitId unitId = null,
                string valueFormat = null,
                string[] sourceOfDefinition = null,
                string symbol = null,
                string dataType = "",
                string[] definition = null
            )
            {
                var d = new DataSpecificationIEC61360();
                if (preferredName != null)
                    d.preferredName.langString = LangStr.CreateManyFromStringArray(preferredName);
                d.shortName = shortName;
                d.unit = unit;
                d.unitId = unitId;
                d.valueFormat = valueFormat;
                if (sourceOfDefinition != null)
                    d.sourceOfDefinition = LangStr.CreateManyFromStringArray(sourceOfDefinition);
                d.symbol = symbol;
                d.dataType = dataType;
                if (definition != null)
                    d.definition.langString = LangStr.CreateManyFromStringArray(definition);
                return (d);
            }
        }

        public class DataSpecificationISO99999
        {
        }

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
        }

        public class EmbeddedDataSpecification
        {
            // members

            public DataSpecificationRef hasDataSpecification = new DataSpecificationRef();
            public DataSpecificationContent dataSpecificationContent = new DataSpecificationContent();

            // constructors

            public EmbeddedDataSpecification() { }

            public EmbeddedDataSpecification(EmbeddedDataSpecification src)
            {
                if (src.hasDataSpecification != null)
                    this.hasDataSpecification = new DataSpecificationRef(src.hasDataSpecification);
                if (src.dataSpecificationContent != null)
                    this.dataSpecificationContent = new DataSpecificationContent(src.dataSpecificationContent);
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
            // TODO (Michael Hoffmeister, 1970-01-01): in V1.0, shall be a list of embeddedDataSpecification
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
                    return new EmbeddedDataSpecification[] { embeddedDataSpecification };
                }
                set
                {
                    if (value == null)
                        embeddedDataSpecification = null;
                    else
                        embeddedDataSpecification = value[0];
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

            public static ConceptDescription CreateNew(
                string idType, string id, string version = null, string revision = null)
            {
                var cd = new ConceptDescription();
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

            public ConceptDescriptionRef GetReference()
            {
                var r = new ConceptDescriptionRef();
                r.Keys.Add(
                    Key.CreateNew(this.GetElementName(), true, this.identification.idType, this.identification.id));
                return r;
            }

            public Key GetGlobalDataSpecRef()
            {
                if (embeddedDataSpecification.hasDataSpecification.Count != 1)
                    return null;
                return (embeddedDataSpecification.hasDataSpecification[0]);
            }

            public void SetIEC61360Spec(
                string[] preferredNames = null,
                string shortName = "",
                string unit = "",
                UnitId unitId = null,
                string valueFormat = null,
                string[] sourceOfDefinition = null,
                string symbol = null,
                string dataType = "",
                string[] definition = null
            )
            {
                this.embeddedDataSpecification = new EmbeddedDataSpecification();
                this.embeddedDataSpecification.hasDataSpecification.Keys.Add(
                    Key.CreateNew("GlobalReference", false, "URI",
                        "www.admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360"));
                this.embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360 =
                    AdminShellV10.DataSpecificationIEC61360.CreateNew(
                        preferredNames, shortName, unit, unitId, valueFormat, sourceOfDefinition, symbol, dataType,
                        definition);
                this.AddIsCaseOf(
                    Reference.CreateNew(new Key(
                        "ConceptDescription", false, this.identification.idType, this.identification.id)));
            }

            public DataSpecificationIEC61360 GetIEC61360()
            {
                if (embeddedDataSpecification != null &&
                    embeddedDataSpecification.dataSpecificationContent != null &&
                    embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360 != null)
                    return embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360;
                return null;
            }

            public string GetShortName()
            {
                if (embeddedDataSpecification != null &&
                    embeddedDataSpecification.dataSpecificationContent != null &&
                    embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360 != null)
                    return embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360.shortName;
                return "";
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

                var info = "";
                if (embeddedDataSpecification != null &&
                    embeddedDataSpecification.dataSpecificationContent != null &&
                    embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360 != null)
                    info += embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360.shortName;

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

        [XmlRoot(ElementName = "aasenv", Namespace = "http://www.admin-shell.io/aas/1/0")]
        public class AdministrationShellEnv
        {
            [XmlAttribute(Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
            public string schemaLocation =
                "http://www.admin-shell.io/aas/1/0 AAS.xsd http://www.admin-shell.io/IEC61360/1/0 IEC61360.xsd";

            /// [XmlElement(ElementName="assetAdministrationShells")]
            [XmlIgnore] // will be ignored, anyway
            private List<AdministrationShell> administrationShells = new List<AdministrationShell>();
            [XmlIgnore] // will be ignored, anyway
            private List<Asset> assets = new List<Asset>();
            [XmlIgnore] // will be ignored, anyway
            private List<Submodel> submodels = new List<Submodel>();
            [XmlIgnore] // will be ignored, anyway
            private List<ConceptDescription> conceptDescriptions = new List<ConceptDescription>();

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
            public List<ConceptDescription> ConceptDescriptions
            {
                get { return conceptDescriptions; }
                set { conceptDescriptions = value; }
            }

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
                            if (smref.MatchesTo(smid))
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

            public ConceptDescription FindConceptDescription(Key key)
            {
                if (key == null)
                    return null;
                var l = new List<Key>();
                l.Add(key);
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
                        CopyConceptDescriptionsFrom(srcEnv, m.submodelElement, shallowCopy);

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
                {
                    // there is already an submodel, just add members
                    if (!shallowCopy && srcSub.submodelElements != null)
                    {
                        if (dstSub.submodelElements == null)
                            dstSub.submodelElements = new List<SubmodelElementWrapper>();
                        foreach (var smw in srcSub.submodelElements)
                            dstSub.submodelElements.Add(new SubmodelElementWrapper(smw.submodelElement, shallowCopy));
                    }
                }

                // copy the CDs..
                if (copyCD && srcSub.submodelElements != null)
                    foreach (var smw in srcSub.submodelElements)
                        CopyConceptDescriptionsFrom(srcEnv, smw.submodelElement, shallowCopy);

                // give back
                return dstSubRef;
            }

            // serializations

            public void SerializeXmlToStream(StreamWriter s)
            {
                var serializer = new XmlSerializer(typeof(AdminShellV10.AdministrationShellEnv));
                var nss = new XmlSerializerNamespaces();
                nss.Add("xsi", System.Xml.Schema.XmlSchema.InstanceNamespace);
                nss.Add("aas", "http://www.admin-shell.io/aas/1/0");
                nss.Add("IEC61360", "http://www.admin-shell.io/IEC61360/1/0");
                serializer.Serialize(s, this, nss);
            }

            public JsonWriter SerialiazeJsonToStream(StreamWriter sw, bool leaveJsonWriterOpen = false)
            {
                sw.AutoFlush = true;

                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                serializer.Formatting = Newtonsoft.Json.Formatting.Indented;

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
                    typeof(AdminShellV10.AdministrationShellEnv), "http://www.admin-shell.io/aas/1/0");
                var res = serializer.Deserialize(reader) as AdminShellV10.AdministrationShellEnv;
                return res;
            }

            public AdministrationShellEnv DeserializeFromJsonStream(TextReader reader)
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new AdminShellV10.JsonAasxConverter("modelType", "name"));
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
                    // include in filter ..
                    if (w.submodelElement.semanticId != null)
                    {
                        var cd = src.FindConceptDescription(w.submodelElement.semanticId.Keys);
                        if (cd != null)
                            filterForCD.Add(cd);
                    }

                    // recurse?
                    if (w.submodelElement is SubmodelElementCollection)
                        CreateFromExistingEnvRecurseForCDs(
                            src, (w.submodelElement as SubmodelElementCollection).value, ref filterForCD);

                    if (w.submodelElement is Operation)
                        for (int i = 0; i < 2; i++)
                        {
                            var w2s = Operation.GetWrappers((w.submodelElement as Operation)[i]);
                            CreateFromExistingEnvRecurseForCDs(src, w2s, ref filterForCD);
                        }

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
            // TODO (Michael Hoffmeister, 1970-01-01): Qualifiers not working!
            // 190410: test-wise enable them again, everyhing works fine ..
            public SemanticId semanticId = null;

            // this class
            public string qualifierType = null;
            public string qualifierValue = null;
            public Reference qualifierValueId = null;

            // constructors

            public Qualifier() { }

            public Qualifier(Qualifier src)
            {
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                this.qualifierType = src.qualifierType;
                this.qualifierValue = src.qualifierValue;
                if (src.qualifierValueId != null)
                    this.qualifierValueId = new Reference(src.qualifierValueId);
            }

            public string GetElementName()
            {
                return "Qualifier";
            }
        }

        public class SubmodelElement : Referable, System.IDisposable, IGetReference
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
            // from hasSemantics:
            [XmlElement(ElementName = "semanticId")]
            public SemanticId semanticId = null;
            // from hasKind:
            [XmlElement(ElementName = "kind")]
            [JsonIgnore]
            public Kind kind = null;
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
                        kind = new Kind();
                    kind.kind = value;
                }
            }
            // from Qualifiable:
            [XmlArray("qualifier")]
            [XmlArrayItem("qualifier")]
            [JsonProperty(PropertyName = "constraints")]
            public List<Qualifier> qualifiers = null;

            // getter / setter

            // constructors / creators

            public SubmodelElement()
                : base() { }

            public SubmodelElement(SubmodelElement src)
                : base(src)
            {
                if (src.hasDataSpecification != null)
                    hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                if (src.semanticId != null)
                    semanticId = new SemanticId(src.semanticId);
                if (src.kind != null)
                    kind = new Kind(src.kind);
                if (src.qualifiers != null)
                {
                    if (qualifiers == null)
                        qualifiers = new List<Qualifier>();
                    foreach (var q in src.qualifiers)
                        qualifiers.Add(new Qualifier(q));
                }
            }

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
                    this.qualifiers = new List<Qualifier>();
                var q = new Qualifier();
                q.qualifierType = qualifierType;
                q.qualifierValue = qualifierValue;
                q.qualifierValueId = qualifierValueId;
                if (semanticKeys != null)
                    q.semanticId = SemanticId.CreateFromKeys(semanticKeys.Keys);
                this.qualifiers.Add(q);
            }

            public Qualifier HasQualifierOfType(string qualifierType)
            {
                if (this.qualifiers == null || qualifierType == null)
                    return null;
                foreach (var q in this.qualifiers)
                    if (q.qualifierType.Trim().ToLower() == qualifierType.Trim().ToLower())
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
                r.Keys.Add(Key.CreateNew(GetElementName(), true, "idShort", this.idShort));
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
                            "idShort", this.idShort));
                    }
                    current = current.parent;
                }
                return r;
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtilV10.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
                var info = "";
                if (semanticId != null)
                    info = AdminShellUtilV10.EvalToNonEmptyString("\u21e8 {0}", semanticId.ToString(), "");
                return Tuple.Create(caption, info);
            }

            public override string ToString()
            {
                var ci = ToCaptionInfo();
                return string.Format("{0}{1}", ci.Item1, (ci.Item2 != "") ? " / " + ci.Item2 : "");
            }


        }

        [XmlType(TypeName = "submodelElement")]
        public class SubmodelElementWrapper
        {

            // members

            [XmlElement(ElementName = "property", Type = typeof(Property))]
            [XmlElement(ElementName = "file", Type = typeof(File))]
            [XmlElement(ElementName = "blob", Type = typeof(Blob))]
            [XmlElement(ElementName = "referenceElement", Type = typeof(ReferenceElement))]
            [XmlElement(ElementName = "relationshipElement", Type = typeof(RelationshipElement))]
            [XmlElement(ElementName = "submodelElementCollection", Type = typeof(SubmodelElementCollection))]
            [XmlElement(ElementName = "operation", Type = typeof(Operation))]
            public SubmodelElement submodelElement;

            // element names
            public static string[] AdequateElementNames = {
                "SubmodelElementCollection", "Property", "File", "Blob", "ReferenceElement",
                "RelationshipElement", "Operation", "OperationVariable" };

            // constructors

            public SubmodelElementWrapper() { }

            // for cloning
            public SubmodelElementWrapper(SubmodelElement src, bool shallowCopy = false)
            {
                if (src is Property)
                    this.submodelElement = new Property(src as Property);
                if (src is File)
                    this.submodelElement = new File(src as File);
                if (src is Blob)
                    this.submodelElement = new Blob(src as Blob);
                if (src is ReferenceElement)
                    this.submodelElement = new ReferenceElement(src as ReferenceElement);
                if (src is RelationshipElement)
                    this.submodelElement = new RelationshipElement(src as RelationshipElement);
                if (src is SubmodelElementCollection)
                    this.submodelElement = new SubmodelElementCollection(
                        src as SubmodelElementCollection, shallowCopy: shallowCopy);
                if (src is Operation)
                    this.submodelElement = new Operation(src as Operation);
            }

            /// <summary>
            /// Introduced for JSON serialization, can create SubModelElements based on a string name
            /// </summary>
            /// <param name="elementName">string name (standard PascalCased)</param>
            /// <returns>SubmodelElement</returns>
            public static SubmodelElement CreateAdequateType(string elementName)
            {
                if (elementName == "Property")
                    return new Property();
                if (elementName == "File")
                    return new File();
                if (elementName == "Blob")
                    return new Blob();
                if (elementName == "ReferenceElement")
                    return new ReferenceElement();
                if (elementName == "RelationshipElement")
                    return new RelationshipElement();
                if (elementName == "SubmodelElementCollection")
                    return new SubmodelElementCollection();
                if (elementName == "Operation")
                    return new Operation();
                if (elementName == "OperationVariable")
                    return new OperationVariable();
                return null;
            }

            /// <summary>
            /// Can create SubmodelElements based on a numerical index
            /// </summary>
            /// <param name="index">Index 0..7 (6+7 are Operation..!)</param>
            /// <returns>SubmodelElement</returns>
            public static SubmodelElement CreateAdequateType(int index)
            {
                AdminShellV10.SubmodelElement sme = null;
                switch (index)
                {
                    case 0:
                        sme = new AdminShellV10.Property();
                        break;
                    case 1:
                        sme = new AdminShellV10.File();
                        break;
                    case 2:
                        sme = new AdminShellV10.Blob();
                        break;
                    case 3:
                        sme = new AdminShellV10.ReferenceElement();
                        break;
                    case 4:
                        sme = new AdminShellV10.SubmodelElementCollection();
                        break;
                    case 5:
                        sme = new AdminShellV10.RelationshipElement();
                        break;
                    case 6:
                        sme = new AdminShellV10.Operation();
                        break;
                    case 7:
                        sme = new AdminShellV10.OperationVariable();
                        break;
                }
                return sme;
            }

            public string GetFourDigitCode()
            {
                if (submodelElement == null)
                    return ("Null");
                if (submodelElement is AdminShellV10.Property) return ("Prop");
                if (submodelElement is AdminShellV10.File) return ("File");
                if (submodelElement is AdminShellV10.Blob) return ("Blob");
                if (submodelElement is AdminShellV10.ReferenceElement) return ("Ref");
                if (submodelElement is AdminShellV10.RelationshipElement) return ("Rel");
                if (submodelElement is AdminShellV10.SubmodelElementCollection) return ("Coll");
                if (submodelElement is AdminShellV10.Operation) return ("Opr");
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
                var res = new SubmodelElementWrapper();
                res.submodelElement = sme;
                return res;
            }

            public static Referable FindReferableByReference(
                List<SubmodelElementWrapper> wrappers, Reference rf, int keyIndex)
            {
                // first index needs to exist ..
                if (wrappers == null || rf == null || keyIndex >= rf.Count)
                    return null;

                // as SubmodelElements are not Identifiables, the actual key shall be IdSHort
                if (rf[keyIndex].idType.Trim().ToLower() !=
                        Key.GetIdentifierTypeName(Key.IdentifierType.IdShort).Trim().ToLower())
                    return null;

                // over all wrappers
                if (wrappers != null)
                    foreach (var smw in wrappers)
                        if (smw.submodelElement != null &&
                            smw.submodelElement.idShort.Trim().ToLower() == rf[keyIndex].value.Trim().ToLower())
                        {
                            // match on this level. Did we find a leaf element?
                            if ((keyIndex + 1) >= rf.Count)
                                return smw.submodelElement;

                            // ok, not a leaf, must be a recursion
                            // int SMEC
                            if (smw.submodelElement is SubmodelElementCollection)
                                return FindReferableByReference(
                                    (smw.submodelElement as SubmodelElementCollection).value, rf, keyIndex + 1);

                            // TODO (Michael Hoffmeister, 1970-01-01): Operation

                            // else:
                            return null;
                        }

                // no?
                return null;
            }
        }

        public class Submodel : Identifiable, System.IDisposable, IGetReference
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
            // from hasSemanticId:
            [XmlElement(ElementName = "semanticId")]
            public SemanticId semanticId = new SemanticId();
            // from Kindable
            [XmlElement(ElementName = "kind")]
            [JsonIgnore]
            public Kind kind = new Kind();
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
                        kind = new Kind();
                    kind.kind = value;
                }
            }
            // from Qualifiable:
            [XmlArray("qualifier")]
            [XmlArrayItem("qualifier")]
            public List<Qualifier> qualifiers = null;

            // from this very class
            [JsonIgnore]
            public List<SubmodelElementWrapper> submodelElements = null;
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
                        this.submodelElements = new List<SubmodelElementWrapper>();
                        foreach (var x in value)
                        {
                            var smew = new SubmodelElementWrapper();
                            smew.submodelElement = x;
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
                if (src.hasDataSpecification != null)
                    this.hasDataSpecification = new HasDataSpecification(src.hasDataSpecification);
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                if (src.kind != null)
                    this.kind = new Kind(src.kind);
                if (!shallowCopy && src.submodelElements != null)
                {
                    if (this.submodelElements == null)
                        this.submodelElements = new List<SubmodelElementWrapper>();
                    foreach (var smw in src.submodelElements)
                        this.submodelElements.Add(new SubmodelElementWrapper(smw.submodelElement, shallowCopy));
                }
            }

            public static Submodel CreateNew(string idType, string id, string version = null, string revision = null)
            {
                var s = new Submodel();
                s.identification.idType = idType;
                s.identification.id = id;
                if (version != null)
                {
                    if (s.administration == null)
                        s.administration = new Administration();
                    s.administration.version = version;
                    s.administration.revision = revision;
                }
                return (s);
            }

            public override string GetElementName()
            {
                return "Submodel";
            }

            public Reference GetReference()
            {
                SubmodelRef l = new SubmodelRef();
                l.Keys.Add(
                    Key.CreateNew(this.GetElementName(), true, this.identification.idType, this.identification.id));
                return l;
            }

            public void Add(SubmodelElement se)
            {
                if (submodelElements == null)
                    submodelElements = new List<SubmodelElementWrapper>();
                var sew = new SubmodelElementWrapper();
                se.parent = this; // track parent here!
                sew.submodelElement = se;
                submodelElements.Add(sew);
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
                var caption = AdminShellUtilV10.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
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

            private void RecurseOnSubmodelElementsRecurse(
                List<SubmodelElementWrapper> wrappers, object state, List<SubmodelElement> parents,
                Action<object, List<SubmodelElement>, SubmodelElement> lambda)
            {
                // trivial
                if (wrappers == null || parents == null || lambda == null)
                    return;

                // over all elements
                foreach (var smw in wrappers)
                {
                    var current = smw.submodelElement;
                    if (current == null)
                        continue;

                    // call lambda for this element
                    lambda(state, parents, current);

                    // add to parents
                    parents.Add(current);

                    // dive into?
                    if (current is SubmodelElementCollection)
                    {
                        var smc = current as SubmodelElementCollection;
                        RecurseOnSubmodelElementsRecurse(smc.value, state, parents, lambda);
                    }

                    if (current is Operation)
                    {
                        var op = current as Operation;
                        for (int i = 0; i < 2; i++)
                            RecurseOnSubmodelElementsRecurse(Operation.GetWrappers(op[i]), state, parents, lambda);
                    }

                    // remove from parents
                    parents.RemoveAt(parents.Count - 1);
                }
            }

            public void RecurseOnSubmodelElements(
                object state, Action<object, List<SubmodelElement>, SubmodelElement> lambda)
            {
                RecurseOnSubmodelElementsRecurse(this.submodelElements, state, new List<SubmodelElement>(), lambda);
            }

            // Parents stuff

            private static void SetParentsForSME(Referable parent, SubmodelElement se)
            {
                se.parent = parent;
                var smc = se as SubmodelElementCollection;
                if (smc != null)
                    foreach (var sme in smc.value)
                        SetParentsForSME(se, sme.submodelElement);
            }

            public void SetAllParents()
            {
                if (this.submodelElements != null)
                    foreach (var sme in this.submodelElements)
                        SetParentsForSME(this, sme.submodelElement);
            }

        }

        //
        // Derived from SubmodelElements
        //

        public class DataElement : SubmodelElement
        {

            public DataElement() { }

            public DataElement(DataElement src)
                : base(src)
            { }

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

            [JsonIgnore]
            public string valueType = "";
            [XmlIgnore]
            [JsonProperty(PropertyName = "valueType")]
            public JsonValueTypeCast JsonValueType
            {
                get { return new JsonValueTypeCast(this.valueType); }
                set { this.valueType = value?.dataObjectType?.name; }
            }


            public string value = "";
            public Reference valueId = null;

            // constructors

            public Property() { }

            public Property(Property src)
                : base(src)
            {
                this.valueType = src.valueType;
                this.value = src.value;
                if (src.valueId != null)
                    src.valueId = new Reference(src.valueId);
            }

            public static Property CreateNew(string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new Property();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            public void Set(string valueType = "", string value = "")
            {
                this.valueType = valueType;
                this.value = value;
            }

            public void Set(string type, bool local, string idType, string value)
            {
                this.valueId = Reference.CreateNew(Key.CreateNew(type, local, idType, value));
            }

            public override string GetElementName()
            {
                return "Property";
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

            public string mimeType = "";
            public string value = "";

            // constructors

            public Blob() { }

            public Blob(Blob src)
                : base(src)
            {
                this.mimeType = src.mimeType;
                this.value = src.value;
            }

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

            public string mimeType = "";
            public string value = "";

            // constructors

            public File() { }

            public File(File src)
                : base(src)
            {
                this.mimeType = src.mimeType;
                this.value = src.value;
            }

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
                    new string[] {
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

            public ReferenceElement(ReferenceElement src)
                : base(src)
            {
                if (src.value != null)
                    this.value = new Reference(src.value);
            }

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

            public RelationshipElement(RelationshipElement src)
                : base(src)
            {
                if (src.first != null)
                    this.first = new Reference(src.first);
                if (src.second != null)
                    this.second = new Reference(src.second);
            }

            public static RelationshipElement CreateNew(
                string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new RelationshipElement();
                x.CreateNewLogic(idShort, category, semanticIdKey);
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

        public class SubmodelElementCollection : SubmodelElement
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
            public List<SubmodelElementWrapper> value = new List<SubmodelElementWrapper>();

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
                        this.value = new List<SubmodelElementWrapper>();
                        foreach (var x in value)
                        {
                            var smew = new SubmodelElementWrapper();
                            smew.submodelElement = x;
                            this.value.Add(smew);
                        }
                    }
                }
            }

            // further members
            public bool ordered = false;
            public bool allowDuplicates = false;

            // constructors

            public SubmodelElementCollection() { }

            public SubmodelElementCollection(SubmodelElementCollection src, bool shallowCopy = false)
                : base(src)
            {
                this.ordered = src.ordered;
                this.allowDuplicates = src.allowDuplicates;
                if (!shallowCopy)
                    foreach (var smw in src.value)
                        value.Add(new SubmodelElementWrapper(smw.submodelElement));
            }

            public static SubmodelElementCollection CreateNew(
                string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new SubmodelElementCollection();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            public void Set(bool allowDuplicates = false, bool ordered = false)
            {
                this.allowDuplicates = allowDuplicates;
                this.ordered = ordered;
            }

            public void Add(SubmodelElement se)
            {
                if (value == null)
                    value = new List<SubmodelElementWrapper>();
                var sew = new SubmodelElementWrapper();
                se.parent = this; // track parent here!
                sew.submodelElement = se;
                value.Add(sew);
            }

            public SubmodelElementWrapper FindSubmodelElementWrapper(string idShort)
            {
                if (this.value == null)
                    return null;
                foreach (var smw in this.value)
                    if (smw.submodelElement != null)
                        if (smw.submodelElement.idShort.Trim().ToLower() == idShort.Trim().ToLower())
                            return smw;
                return null;
            }

            public override string GetElementName()
            {
                return "SubmodelElementCollection";
            }
        }

        public class OperationVariable : SubmodelElement
        {
            public enum Direction { In, Out };

            // Note: for OperationVariable, the values of the SubmodelElement itself ARE NOT TO BE USED!
            // only the SME attributes of "value" are counting

            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public new JsonModelTypeWrapper JsonModelType
            {
                get { return new JsonModelTypeWrapper(GetElementName()); }
            }

            // members
            public SubmodelElementWrapper value = null;

            // constructors

            public OperationVariable()
            {
                this.kind = new Kind("Type");
            }

            public OperationVariable(OperationVariable src, bool shallowCopy = false)
                : base(src)
            {
                this.value = new SubmodelElementWrapper(src.value.submodelElement, shallowCopy);
            }

            public OperationVariable(SubmodelElement elem)
                : base()
            {
                this.value = new SubmodelElementWrapper(elem);
            }

            public override string GetElementName()
            {
                return "OperationVariable";
            }
        }

        public class Operation : SubmodelElement
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
            [XmlElement(ElementName = "in")]
            public List<OperationVariable> valueIn = new List<OperationVariable>();

            [JsonIgnore]
            [XmlElement(ElementName = "out")]
            public List<OperationVariable> valueOut = new List<OperationVariable>();

            [XmlIgnore]
            // MICHA 190504: enabled JSON operation variables!
            [JsonProperty(PropertyName = "in")]
            public OperationVariable[] JsonValueIn
            {
                get { return valueIn?.ToArray(); }
                set { valueIn = (value != null) ? new List<OperationVariable>(value) : null; }
            }

            [XmlIgnore]
            [JsonProperty(PropertyName = "out")]
            // MICHA 190504: enabled JSON operation variables!
            public OperationVariable[] JsonValueOut
            {
                get { return valueOut?.ToArray(); }
                set { valueOut = (value != null) ? new List<OperationVariable>(value) : null; }
            }

            public List<OperationVariable> this[OperationVariable.Direction dir]
            {
                get
                {
                    return (dir == OperationVariable.Direction.In) ? valueIn : valueOut;
                }
                set
                {
                    if (dir == OperationVariable.Direction.In)
                        valueIn = value;
                    else
                        valueOut = value;
                }
            }

            public List<OperationVariable> this[int dir]
            {
                get
                {
                    return (dir == 0) ? valueIn : valueOut;
                }
                set
                {
                    if (dir == 0)
                        valueIn = value;
                    else
                        valueOut = value;
                }
            }

            public static List<SubmodelElementWrapper> GetWrappers(List<OperationVariable> ovl)
            {
                var res = new List<SubmodelElementWrapper>();
                foreach (var ov in ovl)
                    if (ov.value != null)
                        res.Add(ov.value);
                return res;
            }

            // constructors

            public Operation() { }

            public Operation(Operation src)
                : base(src)
            {
                for (int i = 0; i < 2; i++)
                    if (src[i] != null)
                    {
                        if (this[i] == null)
                            this[i] = new List<OperationVariable>();
                        foreach (var ov in src[i])
                            this[i].Add(ov);
                    }
            }


            public override string GetElementName()
            {
                return "Operation";
            }
        }

        //
        // Handling of packages
        //

        /// <summary>
        /// This converter is used for reading JSON files; it claims to be responsible for
        /// "SubmodelElements" (the base class)
        /// and decides, which sub-class of the base class shall be populated.
        /// The decision, shich special sub-class to create is done in a factory
        /// AdminShell.SubmodelElementWrapper.CreateAdequateType(),
        /// in order to have all sub-class specific decisions in one place (SubmodelElementWrapper)
        /// Remark: There is a NuGet package JsonSubTypes, which could have done the job, except the fact of having
        /// "modelType" being a class property with a contained property "name".
        /// </summary>
        public class JsonAasxConverter : JsonConverter
        {
            private string UpperClassProperty = "modelType";
            private string LowerClassProperty = "name";

            public JsonAasxConverter() : base()
            {
            }

            public JsonAasxConverter(string UpperClassProperty, string LowerClassProperty) : base()
            {
                this.UpperClassProperty = UpperClassProperty;
                this.LowerClassProperty = LowerClassProperty;
            }

            public override bool CanConvert(Type objectType)
            {
                if (typeof(AdminShellV10.SubmodelElement).IsAssignableFrom(objectType))
                    return true;
                return false;
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override object ReadJson(JsonReader reader,
                                            Type objectType,
                                             object existingValue,
                                             JsonSerializer serializer)
            {
                // Load JObject from stream
                JObject jObject = JObject.Load(reader);

                // Create target object based on JObject
                object target = new AdminShellV10.SubmodelElement();

                if (jObject.ContainsKey(UpperClassProperty))
                {
                    var j2 = jObject[UpperClassProperty];
                    foreach (var c in j2.Children())
                    {
                        var cprop = c as Newtonsoft.Json.Linq.JProperty;
                        if (cprop == null)
                            continue;
                        if (cprop.Name == LowerClassProperty &&
                            cprop.Value != null &&
                            cprop.Value.Type.ToString() == "String")
                        {
                            var cpval = cprop.Value.ToObject<string>();
                            if (cpval == null)
                                continue;
                            var o = AdminShellV10.SubmodelElementWrapper.CreateAdequateType(cpval);
                            if (o != null)
                                target = o;
                        }
                    }
                }

                // Populate the object properties
                serializer.Populate(jObject.CreateReader(), target);

                return target;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// This class lets an outer functionality keep track on the supplementary files, which are in or
        /// are pending to be added or deleted to an Package.
        /// </summary>
        public class PackageSupplementaryFile : Referable
        {
            public enum LocationType { InPackage, AddPending, DeletePending }

            public enum SpecialHandlingType { None, EmbedAsThumbnail }

            public Uri uri = null;
            public string sourcePath = null;
            public LocationType location = LocationType.InPackage;
            public SpecialHandlingType specialHandling = SpecialHandlingType.None;

            public PackageSupplementaryFile(
                Uri uri, string sourcePath = null, LocationType location = LocationType.InPackage,
                SpecialHandlingType specialHandling = SpecialHandlingType.None)
            {
                this.uri = uri;
                this.sourcePath = sourcePath;
                this.location = location;
                this.specialHandling = specialHandling;
            }

            // class derives from Referable in order to provide GetElementName
            public override string GetElementName()
            {
                return "File";
            }

        }

        /// <summary>
        /// This class encapsulates an AdminShellEnvironment and supplementary files into an AASX Package.
        /// Specifically has the capability to load, update and store .XML, .JSON and .AASX packages.
        /// </summary>
        public class PackageEnv
        {
            private string fn = "New Package";
            private AdministrationShellEnv aasenv = new AdministrationShellEnv();
            private Package openPackage = null;
            private List<PackageSupplementaryFile> pendingFilesToAdd = new List<PackageSupplementaryFile>();
            private List<PackageSupplementaryFile> pendingFilesToDelete = new List<PackageSupplementaryFile>();

            public PackageEnv()
            {
            }

            public PackageEnv(AdministrationShellEnv env)
            {
                if (env != null)
                    this.aasenv = env;
            }

            public PackageEnv(string fn)
            {
                Load(fn);
            }

            public bool IsOpen
            {
                get
                {
                    return openPackage != null;
                }
            }

            public string Filename
            {
                get
                {
                    return fn;
                }
            }

            public AdminShellV10.AdministrationShellEnv AasEnv
            {
                get
                {
                    return aasenv;
                }
            }

            public bool Load(string fn)
            {
                this.fn = fn;
                if (this.openPackage != null)
                    this.openPackage.Close();
                this.openPackage = null;

                if (fn.ToLower().EndsWith(".xml"))
                {
                    // load only XML
                    try
                    {
                        // TODO (Michael Hoffmeister, 1970-01-01): use aasenv serialzers here!
                        XmlSerializer serializer = new XmlSerializer(
                            typeof(AdminShellV10.AdministrationShellEnv), "http://www.admin-shell.io/aas/1/0");
                        TextReader reader = new StreamReader(fn);
                        this.aasenv = serializer.Deserialize(reader) as AdminShellV10.AdministrationShellEnv;
                        if (this.aasenv == null)
                            throw (new Exception("Type error for XML file!"));
                        reader.Close();
                    }
                    catch (Exception ex)
                    {
                        throw (new Exception(
                            string.Format("While reading AAS {0} at {1} gave: {2}", fn,
                                AdminShellUtilV10.ShortLocation(ex), ex.Message)));
                    }
                    return true;
                }

                if (fn.ToLower().EndsWith(".json"))
                {
                    // load only JSON
                    try
                    {
                        using (StreamReader file = System.IO.File.OpenText(fn))
                        {
                            // TODO (Michael Hoffmeister, 1970-01-01): use aasenv serialzers here!
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Converters.Add(new AdminShellV10.JsonAasxConverter("modelType", "name"));
                            this.aasenv = (AdministrationShellEnv)serializer.Deserialize(
                                file, typeof(AdministrationShellEnv));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw (new Exception(
                            string.Format("While reading AAS {0} at {1} gave: {2}",
                                fn, AdminShellUtilV10.ShortLocation(ex), ex.Message)));
                    }
                    return true;
                }

                if (fn.ToLower().EndsWith(".aasx"))
                {
                    // load package AASX
                    try
                    {
                        var package = Package.Open(fn, FileMode.Open);

                        // get the origin from the package
                        PackagePart originPart = null;
                        var xs = package.GetRelationshipsByType(
                            "http://www.admin-shell.io/aasx/relationships/aasx-origin");
                        foreach (var x in xs)
                            if (x.SourceUri.ToString() == "/")
                            {
                                originPart = package.GetPart(x.TargetUri);
                                break;
                            }
                        if (originPart == null)
                            throw (new Exception(string.Format("Unable to find AASX origin. Aborting!")));

                        // get the specs from the package
                        PackagePart specPart = null;
                        xs = originPart.GetRelationshipsByType(
                            "http://www.admin-shell.io/aasx/relationships/aas-spec");
                        foreach (var x in xs)
                        {
                            specPart = package.GetPart(x.TargetUri);
                            break;
                        }
                        if (specPart == null)
                            throw (new Exception(string.Format("Unable to find AASX spec(s). Aborting!")));

                        // open spec part to read
                        try
                        {
                            if (specPart.Uri.ToString().ToLower().Trim().EndsWith("json"))
                            {
                                using (var s = specPart.GetStream(FileMode.Open))
                                {
                                    using (StreamReader file = new StreamReader(s))
                                    {
                                        JsonSerializer serializer = new JsonSerializer();
                                        serializer.Converters.Add(
                                            new AdminShellV10.JsonAasxConverter("modelType", "name"));
                                        this.aasenv = (AdministrationShellEnv)serializer.Deserialize(
                                            file, typeof(AdministrationShellEnv));
                                    }
                                }
                            }
                            else
                            {
                                using (var s = specPart.GetStream(FileMode.Open))
                                {
                                    // own catch loop to be more specific
                                    XmlSerializer serializer = new XmlSerializer(
                                        typeof(AdminShellV10.AdministrationShellEnv),
                                        "http://www.admin-shell.io/aas/1/0");
                                    this.aasenv = serializer.Deserialize(s) as AdminShellV10.AdministrationShellEnv;
                                    this.openPackage = package;
                                    if (this.aasenv == null)
                                        throw (new Exception("Type error for XML file!"));
                                    s.Close();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw (new Exception(
                                string.Format("While reading AAS {0} spec at {1} gave: {2}",
                                    fn, AdminShellUtilV10.ShortLocation(ex), ex.Message)));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw (new Exception(
                            string.Format("While reading AASX {0} at {1} gave: {2}", fn,
                                AdminShellUtilV10.ShortLocation(ex), ex.Message)));
                    }
                    return true;
                }

                // Don't know to handle
                throw (new Exception(string.Format($"Not able to handle {fn}.")));
            }

            public bool LoadFromAasEnvString(string content)
            {
                try
                {
                    using (var file = new StringReader(content))
                    {
                        // TODO (Michael Hoffmeister, 1970-01-01): use aasenv serialzers here!
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Converters.Add(new AdminShellV10.JsonAasxConverter("modelType", "name"));
                        this.aasenv = (AdministrationShellEnv)serializer.Deserialize(
                            file, typeof(AdministrationShellEnv));
                    }
                }
                catch (Exception ex)
                {
                    throw (new Exception(
                        string.Format("While reading AASENV string {0} gave: {1}",
                            AdminShellUtilV10.ShortLocation(ex), ex.Message)));
                }
                return true;
            }

            public enum PreferredFormat { None, Xml, Json };

            public bool SaveAs(string fn, bool writeFreshly = false, PreferredFormat prefFmt = PreferredFormat.None)
            {

                if (fn.ToLower().EndsWith(".xml"))
                {
                    // save only XML
                    this.fn = fn;
                    try
                    {
                        using (var s = new StreamWriter(this.fn))
                        {
                            // TODO (Michael Hoffmeister, 1970-01-01): use aasenv serialzers here!
                            var serializer = new XmlSerializer(typeof(AdminShellV10.AdministrationShellEnv));
                            var nss = new XmlSerializerNamespaces();
                            nss.Add("xsi", System.Xml.Schema.XmlSchema.InstanceNamespace);
                            nss.Add("aas", "http://www.admin-shell.io/aas/1/0");
                            nss.Add("IEC61360", "http://www.admin-shell.io/IEC61360/1/0");
                            serializer.Serialize(s, this.aasenv, nss);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw (new Exception(
                            string.Format("While writing AAS {0} at {1} gave: {2}",
                                fn, AdminShellUtilV10.ShortLocation(ex), ex.Message)));
                    }
                    return true;
                }

                if (fn.ToLower().EndsWith(".json"))
                {
                    // save only JSON
                    // this funcitonality is a initial test
                    this.fn = fn;
                    try
                    {
                        using (var sw = new StreamWriter(fn))
                        {
                            // TODO (Michael Hoffmeister, 1970-01-01): use aasenv serialzers here!

                            sw.AutoFlush = true;

                            JsonSerializer serializer = new JsonSerializer();
                            serializer.NullValueHandling = NullValueHandling.Ignore;
                            serializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                            serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                            using (JsonWriter writer = new JsonTextWriter(sw))
                            {
                                serializer.Serialize(writer, this.aasenv);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw (new Exception(
                            string.Format("While writing AAS {0} at {1} gave: {2}",
                                fn, AdminShellUtilV10.ShortLocation(ex), ex.Message)));
                    }
                    return true;
                }

                if (fn.ToLower().EndsWith(".aasx"))
                {
                    // save package AASX
                    try
                    {
                        // we want existing contents to be preserved, but no possiblity to change file name
                        // therefore: copy file to new name, re-open!
                        // fn could be changed, therefore close "old" package first
                        if (this.openPackage != null)
                        {
                            try
                            {
                                this.openPackage.Close();
                                if (!writeFreshly)
                                    System.IO.File.Copy(this.fn, fn);
                            }
                            catch { }
                            this.openPackage = null;
                        }

                        // approach is to utilize the existing package, if possible. If not, create from scratch
                        var package = Package.Open(fn, (writeFreshly) ? FileMode.Create : FileMode.OpenOrCreate);
                        this.fn = fn;

                        // get the origin from the package
                        PackagePart originPart = null;
                        var xs = package.GetRelationshipsByType(
                            "http://www.admin-shell.io/aasx/relationships/aasx-origin");
                        foreach (var x in xs)
                            if (x.SourceUri.ToString() == "/")
                            {
                                originPart = package.GetPart(x.TargetUri);
                                break;
                            }
                        if (originPart == null)
                        {
                            // create, as not existing
                            originPart = package.CreatePart(
                                new Uri(
                                    "/aasx/aasx-origin", UriKind.RelativeOrAbsolute),
                                    System.Net.Mime.MediaTypeNames.Text.Plain, CompressionOption.Maximum);
                            using (var s = originPart.GetStream(FileMode.Create))
                            {
                                var bytes = System.Text.Encoding.ASCII.GetBytes("Intentionally empty.");
                                s.Write(bytes, 0, bytes.Length);
                            }
                            package.CreateRelationship(
                                originPart.Uri, TargetMode.Internal,
                                "http://www.admin-shell.io/aasx/relationships/aasx-origin");
                        }

                        // get the specs from the package
                        PackagePart specPart = null;
                        PackageRelationship specRel = null;
                        xs = originPart.GetRelationshipsByType(
                            "http://www.admin-shell.io/aasx/relationships/aas-spec");
                        foreach (var x in xs)
                        {
                            specRel = x;
                            specPart = package.GetPart(x.TargetUri);
                            break;
                        }

                        // check, if we have to change the spec part
                        if (specPart != null && specRel != null)
                        {
                            var name = System.IO.Path.GetFileNameWithoutExtension(
                                specPart.Uri.ToString()).ToLower().Trim();
                            var ext = System.IO.Path.GetExtension(specPart.Uri.ToString()).ToLower().Trim();
                            if ((ext == ".json" && prefFmt == PreferredFormat.Xml)
                                 || (ext == ".xml" && prefFmt == PreferredFormat.Json)
                                 || (name.StartsWith("aasenv-with-no-id")))
                            {
                                // try kill specpart
                                try
                                {
                                    originPart.DeleteRelationship(specRel.Id);
                                    package.DeletePart(specPart.Uri);
                                }
                                catch { }
                                finally { specPart = null; specRel = null; }
                            }
                        }

                        if (specPart == null)
                        {
                            // create, as not existing
                            var frn = "aasenv-with-no-id";
                            if (this.aasenv.AdministrationShells.Count > 0)
                                frn = this.aasenv.AdministrationShells[0].GetFriendlyName() ?? frn;
                            var aas_spec_fn = "/aasx/#/#.aas";
                            if (prefFmt == PreferredFormat.Json)
                                aas_spec_fn += ".json";
                            else
                                aas_spec_fn += ".xml";
                            aas_spec_fn = aas_spec_fn.Replace("#", "" + frn);
                            specPart = package.CreatePart(
                                new Uri(
                                    aas_spec_fn, UriKind.RelativeOrAbsolute), System.Net.Mime.MediaTypeNames.Text.Xml,
                                    CompressionOption.Maximum);
                            originPart.CreateRelationship(
                                specPart.Uri, TargetMode.Internal,
                                "http://www.admin-shell.io/aasx/relationships/aas-spec");
                        }

                        // now, specPart shall be != null!
                        if (specPart.Uri.ToString().ToLower().Trim().EndsWith("json"))
                        {
                            using (var s = specPart.GetStream(FileMode.Create))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                serializer.NullValueHandling = NullValueHandling.Ignore;
                                serializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                                serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                                using (var sw = new StreamWriter(s))
                                {
                                    using (JsonWriter writer = new JsonTextWriter(sw))
                                    {
                                        serializer.Serialize(writer, this.aasenv);
                                    }
                                }
                            }
                        }
                        else
                        {
                            using (var s = specPart.GetStream(FileMode.Create))
                            {
                                var serializer = new XmlSerializer(typeof(AdminShellV10.AdministrationShellEnv));
                                var nss = new XmlSerializerNamespaces();
                                nss.Add("xsi", System.Xml.Schema.XmlSchema.InstanceNamespace);
                                nss.Add("aas", "http://www.admin-shell.io/aas/1/0");
                                nss.Add("IEC61360", "http://www.admin-shell.io/IEC61360/1/0");
                                serializer.Serialize(s, this.aasenv, nss);
                            }
                        }

                        // there might be pending files to be deleted (first delete, then add, in case of identical
                        // files in both categories)
                        foreach (var psfDel in pendingFilesToDelete)
                        {
                            // try find an existing part for that file ..
                            var found = false;

                            // normal files
                            xs = specPart.GetRelationshipsByType(
                                "http://www.admin-shell.io/aasx/relationships/aas-suppl");
                            foreach (var x in xs)
                                if (x.TargetUri == psfDel.uri)
                                {
                                    // try to delete
                                    specPart.DeleteRelationship(x.Id);
                                    package.DeletePart(psfDel.uri);
                                    found = true;
                                    break;
                                }

                            // thumbnails
                            xs = package.GetRelationshipsByType(
                                "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail");
                            foreach (var x in xs)
                                if (x.TargetUri == psfDel.uri)
                                {
                                    // try to delete
                                    package.DeleteRelationship(x.Id);
                                    package.DeletePart(psfDel.uri);
                                    found = true;
                                    break;
                                }

                            if (!found)
                                throw (new Exception(
                                    $"Not able to delete pending file {psfDel.uri} in saving package {fn}"));
                        }

                        // after this, there are no more pending for delete files
                        pendingFilesToDelete.Clear();

                        // write pending supplementary files
                        foreach (var psfAdd in pendingFilesToAdd)
                        {
                            // make sure ..
                            if (psfAdd.sourcePath == null ||
                                psfAdd.location != PackageSupplementaryFile.LocationType.AddPending)
                                continue;

                            // normal file?
                            if (psfAdd.specialHandling == PackageSupplementaryFile.SpecialHandlingType.None)
                            {

                                // try find an existing part for that file ..
                                PackagePart filePart = null;
                                xs = specPart.GetRelationshipsByType(
                                    "http://www.admin-shell.io/aasx/relationships/aas-suppl");
                                foreach (var x in xs)
                                    if (x.TargetUri == psfAdd.uri)
                                    {
                                        filePart = package.GetPart(x.TargetUri);
                                        break;
                                    }

                                if (filePart == null)
                                {
                                    // create new part and link
                                    filePart = package.CreatePart(
                                        psfAdd.uri, AdminShellV10.PackageEnv.GuessMimeType(psfAdd.sourcePath),
                                        CompressionOption.Maximum);
                                    specPart.CreateRelationship(
                                        filePart.Uri, TargetMode.Internal,
                                        "http://www.admin-shell.io/aasx/relationships/aas-suppl");
                                }

                                // now should be able to write
                                using (var s = filePart.GetStream(FileMode.Create))
                                {
                                    var bytes = System.IO.File.ReadAllBytes(psfAdd.sourcePath);
                                    s.Write(bytes, 0, bytes.Length);
                                }
                            }

                            // thumbnail file?
                            if (psfAdd.specialHandling ==
                                PackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail)
                            {
                                // try find an existing part for that file ..
                                PackagePart filePart = null;
                                xs = package.GetRelationshipsByType(
                                    "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail");
                                foreach (var x in xs)
                                    if (x.SourceUri.ToString() == "/" && x.TargetUri == psfAdd.uri)
                                    {
                                        filePart = package.GetPart(x.TargetUri);
                                        break;
                                    }

                                if (filePart == null)
                                {
                                    // create new part and link
                                    filePart = package.CreatePart(
                                        psfAdd.uri, AdminShellV10.PackageEnv.GuessMimeType(psfAdd.sourcePath),
                                        CompressionOption.Maximum);
                                    package.CreateRelationship(
                                        filePart.Uri, TargetMode.Internal,
                                        "http://schemas.openxmlformats.org/package/2006/" +
                                        "relationships/metadata/thumbnail");
                                }

                                // now should be able to write
                                using (var s = filePart.GetStream(FileMode.Create))
                                {
                                    var bytes = System.IO.File.ReadAllBytes(psfAdd.sourcePath);
                                    s.Write(bytes, 0, bytes.Length);
                                }
                            }
                        }

                        // after this, there are no more pending for add files
                        pendingFilesToAdd.Clear();

                        // flush, but leave open
                        package.Flush();
                        this.openPackage = package;
                    }
                    catch (Exception ex)
                    {
                        throw (new Exception(
                            string.Format("While write AASX {0} at {1} gave: {2}",
                            fn, AdminShellUtilV10.ShortLocation(ex), ex.Message)));
                    }
                    return true;
                }

                // Don't know to handle
                throw (new Exception(string.Format($"Not able to handle {fn}.")));
            }

            private int BackupIndex = 0;

            public void BackupInDir(string backupDir, int maxFiles)
            {
                // access
                if (backupDir == null || maxFiles < 1)
                    return;

                // we do it not caring on any errors
                try
                {
                    // get index in form
                    if (BackupIndex == 0)
                    {
                        // do not always start at 0!!
                        var rnd = new Random();
                        BackupIndex = rnd.Next(maxFiles);
                    }
                    var ndx = BackupIndex % maxFiles;
                    BackupIndex += 1;

                    // build a filename
                    var fn = Path.Combine(backupDir, $"backup{ndx:000}.xml");

                    // raw save
                    using (var s = new StreamWriter(fn))
                    {
                        var serializer = new XmlSerializer(typeof(AdminShellV10.AdministrationShellEnv));
                        var nss = new XmlSerializerNamespaces();
                        nss.Add("xsi", System.Xml.Schema.XmlSchema.InstanceNamespace);
                        nss.Add("aas", "http://www.admin-shell.io/aas/1/0");
                        nss.Add("IEC61360", "http://www.admin-shell.io/IEC61360/1/0");
                        serializer.Serialize(s, this.aasenv, nss);
                    }
                }
                catch { }
            }

            public Stream GetLocalStreamFromPackage(string uriString)
            {
                // access
                if (this.openPackage == null)
                    throw (new Exception(string.Format($"AASX Package {this.fn} not opened. Aborting!")));
                var part = this.openPackage.GetPart(new Uri(uriString, UriKind.RelativeOrAbsolute));
                if (part == null)
                    throw (new Exception(
                        string.Format(
                            $"Cannot access URI {uriString} in {this.fn} not opened. Aborting!")));
                return part.GetStream(FileMode.Open);
            }

            public long GetStreamSizeFromPackage(string uriString)
            {
                long res = 0;
                try
                {
                    if (this.openPackage == null)
                        return 0;
                    var part = this.openPackage.GetPart(new Uri(uriString, UriKind.RelativeOrAbsolute));
                    if (part == null)
                        return 0;
                    using (var s = part.GetStream(FileMode.Open))
                    {
                        res = s.Length;
                    }
                }
                catch { return 0; }
                return res;
            }

            public Stream GetLocalThumbnailStream(ref Uri thumbUri)
            {
                // access
                if (this.openPackage == null)
                    throw (new Exception(string.Format($"AASX Package {this.fn} not opened. Aborting!")));
                // get the thumbnail over the relationship
                PackagePart thumbPart = null;
                var xs = this.openPackage.GetRelationshipsByType(
                    "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail");
                foreach (var x in xs)
                    if (x.SourceUri.ToString() == "/")
                    {
                        thumbPart = this.openPackage.GetPart(x.TargetUri);
                        thumbUri = x.TargetUri;
                        break;
                    }
                if (thumbPart == null)
                    throw (new Exception(string.Format("Unable to find AASX thumbnail. Aborting!")));
                return thumbPart.GetStream(FileMode.Open);
            }

            public Stream GetLocalThumbnailStream()
            {
                Uri dummy = null;
                return GetLocalThumbnailStream(ref dummy);
            }

            public List<PackageSupplementaryFile> GetListOfSupplementaryFiles()
            {
                // new result
                var result = new List<PackageSupplementaryFile>();

                // access
                if (this.openPackage != null)
                {

                    // get the thumbnail(s) from the package
                    var xs = this.openPackage.GetRelationshipsByType(
                        "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail");
                    foreach (var x in xs)
                        if (x.SourceUri.ToString() == "/")
                        {
                            result.Add(new PackageSupplementaryFile(
                                x.TargetUri,
                                location: PackageSupplementaryFile.LocationType.InPackage,
                                specialHandling: PackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail));
                        }

                    // get the origin from the package
                    PackagePart originPart = null;
                    xs = this.openPackage.GetRelationshipsByType(
                        "http://www.admin-shell.io/aasx/relationships/aasx-origin");
                    foreach (var x in xs)
                        if (x.SourceUri.ToString() == "/")
                        {
                            originPart = this.openPackage.GetPart(x.TargetUri);
                            break;
                        }

                    if (originPart != null)
                    {
                        // get the specs from the origin
                        PackagePart specPart = null;
                        xs = originPart.GetRelationshipsByType(
                            "http://www.admin-shell.io/aasx/relationships/aas-spec");
                        foreach (var x in xs)
                        {
                            specPart = this.openPackage.GetPart(x.TargetUri);
                            break;
                        }

                        if (specPart != null)
                        {
                            // get the supplementaries from the package, derived from spec
                            xs = specPart.GetRelationshipsByType(
                                "http://www.admin-shell.io/aasx/relationships/aas-suppl");
                            foreach (var x in xs)
                            {
                                result.Add(
                                    new PackageSupplementaryFile(
                                        x.TargetUri, location: PackageSupplementaryFile.LocationType.InPackage));
                            }
                        }
                    }
                }

                // add or modify the files to delete
                foreach (var psfDel in pendingFilesToDelete)
                {
                    // already in
                    var found = result.Find(x => { return x.uri == psfDel.uri; });
                    if (found != null)
                        found.location = PackageSupplementaryFile.LocationType.DeletePending;
                    else
                    {
                        psfDel.location = PackageSupplementaryFile.LocationType.DeletePending;
                        result.Add(psfDel);
                    }
                }

                // add the files to store as well
                foreach (var psfAdd in pendingFilesToAdd)
                {
                    // already in (should not happen ?!)
                    var found = result.Find(x => { return x.uri == psfAdd.uri; });
                    if (found != null)
                        found.location = PackageSupplementaryFile.LocationType.AddPending;
                    else
                    {
                        psfAdd.location = PackageSupplementaryFile.LocationType.AddPending;
                        result.Add(psfAdd);
                    }
                }

                // done
                return result;
            }

            public static string GuessMimeType(string fn)
            {
                var file_ext = System.IO.Path.GetExtension(fn).ToLower().Trim();
                var content_type = System.Net.Mime.MediaTypeNames.Text.Plain;
                if (file_ext == ".pdf") content_type = System.Net.Mime.MediaTypeNames.Application.Pdf;
                if (file_ext == ".xml") content_type = System.Net.Mime.MediaTypeNames.Text.Xml;
                if (file_ext == ".txt") content_type = System.Net.Mime.MediaTypeNames.Text.Plain;
                if (file_ext == ".igs") content_type = "application/iges";
                if (file_ext == ".iges") content_type = "application/iges";
                if (file_ext == ".stp") content_type = "application/step";
                if (file_ext == ".step") content_type = "application/step";
                if (file_ext == ".jpg") content_type = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                if (file_ext == ".jpeg") content_type = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                if (file_ext == ".png") content_type = "image/png";
                if (file_ext == ".gif") content_type = System.Net.Mime.MediaTypeNames.Image.Gif;
                return content_type;
            }

            public void AddSupplementaryFileToStore(
                string sourcePath, string targetDir, string targetFn, bool embedAsThumb)
            {
                // beautify parameters
                sourcePath = sourcePath.Trim();
                targetDir = targetDir.Trim();
                if (!targetDir.EndsWith("/"))
                    targetDir += "/";
                targetDir = targetDir.Replace(@"\", "/");
                targetFn = targetFn.Trim();
                if (sourcePath == "" || targetDir == "" || targetFn == "")
                    throw (new Exception(string.Format("Trying add supplementary file with empty name or path!")));

                var file_fn = "" + targetDir.Trim() + targetFn.Trim();

                // add record
                pendingFilesToAdd.Add(
                    new PackageSupplementaryFile(
                        new Uri(file_fn, UriKind.RelativeOrAbsolute),
                        sourcePath,
                        location: PackageSupplementaryFile.LocationType.AddPending,
                        specialHandling: (
                            embedAsThumb
                                ? PackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail
                                : PackageSupplementaryFile.SpecialHandlingType.None)
                    ));
            }

            public void DeleteSupplementaryFile(PackageSupplementaryFile psf)
            {
                if (psf == null)
                    throw (new Exception(string.Format("No supplementary file given!")));

                if (psf.location == PackageSupplementaryFile.LocationType.AddPending)
                {
                    // is still pending in add list -> remove
                    pendingFilesToAdd.RemoveAll((x) => { return x.uri == psf.uri; });
                }

                if (psf.location == PackageSupplementaryFile.LocationType.InPackage)
                {
                    // add to pending delete list
                    pendingFilesToDelete.Add(psf);
                }
            }

            public void Close()
            {
                if (this.openPackage != null)
                    this.openPackage.Close();
                this.openPackage = null;
                this.fn = "";
                this.aasenv = null;
            }
        }

    }

    #endregion
}

#endif
