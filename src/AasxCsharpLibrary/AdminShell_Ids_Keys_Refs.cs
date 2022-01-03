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
    /// Version of Details of Administration Shell Part 1 V3.0 RC 02 published Apre 2022
    /// </summary>
    public partial class AdminShellV30
    {
        //
        // Identifier
        //

        /// <summary>
        /// V30
        /// Thic class is the "old" Identification class of meta-model V2.0
        /// It did contain two attributes "idType" and "id"
        /// As string is sealed, this class cannot derive dirctly from string,
        /// so an implicit conversion is tested
        /// </summary>
        public class Identifier
        {

            // members

            [XmlText]
            [CountForHash]
            public string value = "";

            // implicit operators

            public static implicit operator string(Identifier d)
            {
                return d.value;
            }

            public static implicit operator Identifier(string d)
            {
                return new Identifier(d);
            }

            // some constants

            public static string IRDI = "IRDI";
            public static string IRI = "IRI";
            public static string IdShort = "IdShort";

            // constructors

            public Identifier() { }

            public Identifier(Identifier src)
            {
                this.value = src.value;
            }

#if !DoNotUseAasxCompatibilityModels
            public Identifier(AasxCompatibilityModels.AdminShellV10.Identification src)
            {
                this.value = src.id;
            }

            public Identifier(AasxCompatibilityModels.AdminShellV20.Identification src)
            {
                this.value = src.id;
            }
#endif

            public Identifier(string id)
            {
                this.value = id;
            }

            public Identifier(Key key)
            {
                this.value = key.value;
            }

            // Creator with validation

            public static Identifier CreateNew(string id)
            {
                return new Identifier(id);
            }

            // further

            public bool IsEqual(Identifier other)
            {
                return this.value.Trim().ToLower() == other.value.Trim().ToLower();
            }

            public static bool IsIRI(string value)
            {
                if (value == null)
                    return false;
                var m = Regex.Match(value, @"\s*(\w+)://");
                return m.Success;
            }

            public bool IsIRI()
            {
                return IsIRI(value);
            }

            public static bool IsIRDI(string value)
            {
                if (value == null)
                    return false;
                var m = Regex.Match(value, @"\s*(\d{3,4})(:|-|/)");
                return m.Success;
            }

            public bool IsIRDI()
            {
                return IsIRDI(value);
            }

            // Matching

            public bool Matches(string id, Key.MatchMode matchMode = Key.MatchMode.Identification)
            {
                if (id == null || value == null)
                    return false;
                return value.Trim() == id.Trim();
            }

            public bool Matches(Identifier id, Key.MatchMode matchMode = Key.MatchMode.Identification)
            {
                if (id == null || value == null)
                    return false;
                return value.Trim() == id.value.Trim();
            }

            public bool Matches(Key key, Key.MatchMode matchMode = Key.MatchMode.Identification)
            {
                if (key == null)
                    return false;
                return this.Matches(key.value, matchMode);
            }

            // validation

            public static AasValidationAction Validate(AasValidationRecordList results, Identifier id, Referable container)
            {
                // access
                if (results == null || container == null)
                    return AasValidationAction.No;

                var res = AasValidationAction.No;

                // check
                if (id?.value == null)
                {
                    // violation case
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SpecViolation, container,
                        "Value: is null",
                        () =>
                        {
                            res = AasValidationAction.ToBeDeleted;
                        }));
                }

                // may give result "to be deleted"
                return res;
            }

            // Other

            public override string ToString()
            {
                return value;
            }
        }

        public class ListOfIdentifier : List<Identifier>
        {
            // Member operation

            public void AddRange(List<Key> kl)
            {
                if (kl == null)
                    return;
                foreach (var k in kl)
                    Add(k?.value);
            }

            public string ToString(string delimiter = ",")
            {
                return string.Join(delimiter, this.Select((x) => x.ToString()));
            }

            public static ListOfIdentifier Parse(string input)
            {
                // access
                if (input == null)
                    return null;

                // split
                var parts = input.Split(',', ';');
                var loi = new ListOfIdentifier();

                foreach (var p in parts)
                    loi.Add(p);

                return loi;
            }

            // validation

            public static void Validate(AasValidationRecordList results, ListOfIdentifier kl,
                Referable container)
            {
                // access
                if (results == null || kl == null || container == null)
                    return;

                // iterate thru
                var idx = 0;
                while (idx < kl.Count)
                {
                    var act = Identifier.Validate(results, kl[idx], container);
                    if (act == AasValidationAction.ToBeDeleted)
                    {
                        kl.RemoveAt(idx);
                        continue;
                    }
                    idx++;
                }
            }

        }

        //
        // IdentifierKeyValuePair(s)
        //

        public class IdentifierKeyValuePair : IAasElement
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // member
            // from hasSemantics:
            [XmlElement(ElementName = "semanticId")]
            public SemanticId semanticId = null;

            [MetaModelName("IdentifierKeyValuePair.key")]
            [TextSearchable]
            [CountForHash]
            public string key = "";

            [MetaModelName("IdentifierKeyValuePair.value")]
            [TextSearchable]
            [CountForHash]
            public string value = null;

            [CountForHash]
            public GlobalReference externalSubjectId = null;

            // constructors

            public IdentifierKeyValuePair() { }

            public IdentifierKeyValuePair(IdentifierKeyValuePair src)
            {
                if (src.semanticId != null)
                    this.semanticId = new SemanticId(src.semanticId);
                this.key = src.key;
                this.value = src.value;
                if (src.externalSubjectId != null)
                    this.externalSubjectId = new GlobalReference(src.externalSubjectId);
            }

#if !DoNotUseAasxCompatibilityModels
            // not existing in V2.0
#endif

            // self description

            public AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("IdentifierKeyValuePair", "IKV");
            }

            public string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
            }
        }

        public class ListOfIdentifierKeyValuePair : List<IdentifierKeyValuePair>
        {

            // constructors
            public ListOfIdentifierKeyValuePair() : base() { }
            public ListOfIdentifierKeyValuePair(ListOfIdentifierKeyValuePair src) : base()
            {
                if (src != null)
                    foreach (var kvp in src)
                        Add(new IdentifierKeyValuePair(kvp));
            }
        }

        //
        // Keys
        //

        public class Key
        {
            // Constants

            public enum MatchMode { Relaxed, Identification }; // in V3.0RC02: Strict not anymore

            // Members

            [MetaModelName("Key.type")]
            [TextSearchable]
            [XmlAttribute]
            [CountForHash]
            public string type = "";

            //TODO: REMOVE
            //[XmlAttribute]
            //[CountForHash]
            //public bool local = false;

            //[MetaModelName("Key.idType")]
            //[TextSearchable]
            //[XmlAttribute]
            //[JsonIgnore]
            //[CountForHash]
            //public string idType = "";

            //[XmlIgnore]
            //[JsonProperty(PropertyName = "idType")]
            //public string JsonIdType
            //{
            //    // adapt idShort <-> IdShort
            //    get => (idType == "idShort") ? "IdShort" : idType;
            //    set => idType = (value == "idShort") ? "IdShort" : value;
            //}

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
                this.value = src.value;
            }

#if !DoNotUseAasxCompatibilityModels
            public Key(AasxCompatibilityModels.AdminShellV10.Key src)
            {
                this.type = src.type;
                this.value = src.value;
            }

            public Key(AasxCompatibilityModels.AdminShellV20.Key src)
            {
                var stll = src?.type?.Trim().ToLower();
                if (stll == AasxCompatibilityModels.AdminShellV20.Key.GlobalReference.ToLower())
                    this.type = Key.GlobalReference;
                else
                    this.type = src.type;
                this.value = src.value;
            }
#endif

            public Key(string type, string value)
            {
                this.type = type;
                this.value = value;
            }

            public static Key CreateNew(string type, string value)
            {
                var k = new Key()
                {
                    type = type,
                    value = value
                };
                return (k);
            }

            public static Key GetFromRef(ModelReference r)
            {
                if (r == null || r.Count != 1)
                    return null;
                return r[0];
            }

            public Identifier ToId()
            {
                return new Identifier(this);
            }

            public string ToString(int format = 0)
            {
                if (format == 1)
                {
                    return String.Format(
                        "({0}){1}", this.type, this.value);
                }
                if (format == 2)
                {
                    return String.Format("{0}", this.value);
                }

                // (old) default
                return $"[{this.type}, {this.value}]";
            }

            public static Key Parse(string cell, string typeIfNotSet = null,
                bool allowFmtAll = false, bool allowFmt0 = false,
                bool allowFmt1 = false, bool allowFmt2 = false)
            {
                // access and defaults?
                if (cell == null || cell.Trim().Length < 1)
                    return null;
                if (typeIfNotSet == null)
                    typeIfNotSet = Key.GlobalReference;

                // TODO: REWORK OLD & NEW FORMATS!!

                // OLD format == 1
                if (allowFmtAll || allowFmt1)
                {
                    var m = Regex.Match(cell, @"\((\w+)\)\((\S+)\)\[(\w+)\]( ?)(.*)$");
                    if (m.Success)
                    {
                        return new AdminShell.Key(
                                m.Groups[1].ToString(), m.Groups[5].ToString());
                    }
                }

                // OLD format == 2
                if (allowFmtAll || allowFmt2)
                {
                    var m = Regex.Match(cell, @"\[(\w+)\]( ?)(.*)$");
                    if (m.Success)
                    {
                        return new AdminShell.Key(typeIfNotSet, m.Groups[3].ToString());
                    }
                }

                // OLD format == 0
                if (allowFmtAll || allowFmt0)
                {
                    var m = Regex.Match(cell, @"\[(\w+),( ?)([^,]+),( ?)\[(\w+)\],( ?)(.*)\]");
                    if (m.Success)
                    {
                        return new AdminShell.Key(m.Groups[1].ToString(), m.Groups[7].ToString());
                    }
                }

                // no
                return null;
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
                "AnnotatedRelationshipElement",
                "AssetAdministrationShell",
                "BasicEvent",
                "Blob",
                "Capability",
                "ConceptDescription",
                "DataElement",
                "Entity",
                "Event",
                "File",
                "FragmentReference",
                "GlobalElementReference",
                "ModelElementReference",
                "MultiLanguageProperty",
                "Operation",
                "OperationVariable", // not specified, but used by AASX Package Explorer
                "Property",
                "Range",
                "ReferenceElement",
                "RelationshipElement",
                "Submodel",
                "SubmodelElement",
                "SubmodelElementCollection", // not specified, but used by AASX Package Explorer
                "SubmodelElementList",
                "SubmodelElementStructure",
                "SubmodelRef" // not specified, but used by AASX Package Explorer
            };

            public static string[] ReferableElements = new string[] {
                "AnnotatedRelationshipElement",
                "AssetAdministrationShell",
                "BasicEvent",
                "Blob",
                "Capability",
                "ConceptDescription",
                "DataElement",
                "Entity",
                "Event",
                "File",
                "FragmentReference",
                "GlobalElementReference",
                "ModelElementReference",
                "MultiLanguageProperty",
                "Operation",
                "OperationVariable", // not specified, but used by AASX Package Explorer
                "Property",
                "Range",
                "ReferenceElement",
                "RelationshipElement",
                "Submodel",
                "SubmodelElement",
                "SubmodelElementCollection", // not specified, but used by AASX Package Explorer
                "SubmodelElementList",
                "SubmodelElementStructure"
            };

            public static string[] SubmodelElements = new string[] {
                "AnnotatedRelationshipElement",
                "BasicEvent",
                "Blob",
                "Capability",
                "DataElement",
                "Entity",
                "Event",
                "File",
                // "GlobalElementReference", // in spec, but not expected by AASX Package Explorer
                // "ModelElementReference", // in spec, but not expected by AASX Package Explorer
                "MultiLanguageProperty",
                "Operation",
                "Property",
                "Range",
                "ReferenceElement",
                "RelationshipElement",
                "Submodel",
                // "SubmodelElement", // in spec, but not expected by AASX Package Explorer
                "SubmodelElementCollection", // not specified, but used by AASX Package Explorer
                "SubmodelElementList",
                "SubmodelElementStructure"
            };

            // use this in list to designate all of the above elements
            public static string AllElements = "All";

            // use this in list to designate the GlobalReference
            // Resharper disable MemberHidesStaticFromOuterClass
            public static string GlobalReference = "GlobalElementReference";
            public static string ModelReference = "ModelElementReference";
            public static string FragmentReference = "FragmentReference";
            public static string ConceptDescription = "ConceptDescription";
            public static string SubmodelRef = "SubmodelRef";
            public static string Submodel = "Submodel";
            public static string SubmodelElement = "SubmodelElement";
            public static string AssetInformation = "AssetInformation";
            public static string AAS = "AssetAdministrationShell";
            public static string Entity = "Entity";
            // Resharper enable MemberHidesStaticFromOuterClass

            // TODO: REMOVE
            //public static string[] IdentifierTypeNames = new string[] {
            //    Identifier.IdShort, "FragmentId", "Custom", Identifier.IRDI, Identifier.IRI };
            //public enum IdentifierType { IdShort = 0, FragmentId, Custom, IRDI, IRI };

            //public static string GetIdentifierTypeName(IdentifierType t)
            //{
            //    return IdentifierTypeNames[(int)t];
            //}

            //public static string IdShort = "IdShort";
            //public static string FragmentId = "FragmentId";
            //public static string Custom = "Custom";

            // some helpers

            public static bool IsInNamedElementsList(string[] elementsList, string ke)
            {
                if (elementsList == null || ke == null)
                    return false;

                foreach (var s in elementsList)
                    if (s.Trim().ToLower() == ke.Trim().ToLower())
                        return true;

                return false;
            }

            public bool IsInKeyElements()
            {
                return IsInNamedElementsList(KeyElements, this.type);
            }

            public bool IsInReferableElements()
            {
                return IsInNamedElementsList(ReferableElements, this.type);
            }

            public bool IsInSubmodelElements()
            {
                return IsInNamedElementsList(SubmodelElements, this.type);
            }

            public bool IsIRI()
            {
                return Identifier.IsIRI(value);
            }

            public bool IsIRDI()
            {
                return Identifier.IsIRDI(value);
            }

            public bool IsType(string value)
            {
                if (value == null || type == null || type.Trim() == "")
                    return false;
                return value.Trim().ToLower().Equals(type.Trim().ToLower());
            }

            public bool IsAbsolute()
            {
                return IsType(Key.GlobalReference)
                    || IsType(Key.AAS)
                    || IsType(Key.Submodel);
            }

            public bool Matches(
                string type, string id, MatchMode matchMode = MatchMode.Relaxed)
            {
                if (matchMode == MatchMode.Relaxed)
                    return this.type == type && this.value == id;

                if (matchMode == MatchMode.Identification)
                    return this.value == id;

                return false;
            }

            public bool Matches(Identifier id)
            {
                if (id == null)
                    return false;
                return this.Matches(Key.GlobalReference, id.value, MatchMode.Identification);
            }

            public bool Matches(Key key, MatchMode matchMode = MatchMode.Relaxed)
            {
                if (key == null)
                    return false;
                return this.Matches(key.type, key.value, matchMode);
            }

            // validation

            public static AasValidationAction Validate(AasValidationRecordList results, Key k, Referable container)
            {
                // access
                if (results == null || container == null)
                    return AasValidationAction.No;

                var res = AasValidationAction.No;

                // check
                if (k == null)
                {
                    // violation case
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SpecViolation, container,
                        "Key: is null",
                        () =>
                        {
                            res = AasValidationAction.ToBeDeleted;
                        }));
                }
                else
                {
                    // check type
                    var tf = AdminShellUtil.CheckIfInConstantStringArray(KeyElements, k.type);
                    if (tf == AdminShellUtil.ConstantFoundEnum.No)
                        // violation case
                        results.Add(new AasValidationRecord(
                            AasValidationSeverity.SchemaViolation, container,
                            "Key: type is not in allowed enumeration values",
                            () =>
                            {
                                k.type = GlobalReference;
                            }));
                    if (tf == AdminShellUtil.ConstantFoundEnum.AnyCase)
                        // violation case
                        results.Add(new AasValidationRecord(
                            AasValidationSeverity.SchemaViolation, container,
                            "Key: type in wrong casing",
                            () =>
                            {
                                k.type = AdminShellUtil.CorrectCasingForConstantStringArray(
                                    KeyElements, k.type);
                            }));
                }

                // may give result "to be deleted"
                return res;
            }
        }

        public class KeyList : List<Key>
        {
            // getters / setters

            [XmlIgnore]
            public bool IsEmpty { get { return this.Count < 1; } }

            // constructors / creators

            public KeyList() { }

            public KeyList(KeyList src)
            {
                if (src != null)
                    foreach (var k in src)
                        this.Add(new Key(k));
            }

            public static KeyList CreateNew(Key k)
            {
                var kl = new KeyList { k };
                return kl;
            }

            public static KeyList CreateNew(string type, string value)
            {
                var kl = new KeyList() {
                    Key.CreateNew(type, value)
                };
                return kl;
            }

            public static KeyList CreateNew(string type, string[] valueItems)
            {
                // access
                if (valueItems == null)
                    return null;

                // prepare
                var kl = new AdminShell.KeyList();
                foreach (var x in valueItems)
                    kl.Add(new AdminShell.Key(type, "" + x));
                return kl;
            }

            // matches

            public bool Matches(KeyList other, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                if (other == null || other.Count != this.Count)
                    return false;

                var same = true;
                for (int i = 0; i < this.Count; i++)
                    same = same && this[i].Matches(other[i], matchMode);

                return same;
            }

            // other

            public void NumberIndices()
            {
                for (int i = 0; i < this.Count; i++)
                    this[i].index = i;
            }

            public string ToString(int format = 0, string delimiter = ",")
            {
                return string.Join(delimiter, this.Select((x) => x.ToString(format)));
            }

            public static KeyList Parse(string input)
            {
                // access
                if (input == null)
                    return null;

                // split
                var parts = input.Split(',', ';');
                var kl = new KeyList();

                foreach (var p in parts)
                {
                    var k = Key.Parse(p);
                    if (k != null)
                        kl.Add(k);
                }

                return kl;
            }

            public string MostSignificantInfo()
            {
                if (this.Count < 1)
                    return "-";
                var i = this.Count - 1;
                var res = this[i].value;
                if (this[i].IsType(Key.FragmentReference) && i > 0)
                    res += this[i - 1].value;
                return res;
            }

            // validation

            public static void Validate(AasValidationRecordList results, KeyList kl,
                Referable container)
            {
                // access
                if (results == null || kl == null || container == null)
                    return;

                // iterate thru
                var idx = 0;
                while (idx < kl.Count)
                {
                    var act = Key.Validate(results, kl[idx], container);
                    if (act == AasValidationAction.ToBeDeleted)
                    {
                        kl.RemoveAt(idx);
                        continue;
                    }
                    idx++;
                }
            }

            public bool StartsWith(KeyList head, bool emptyIsTrue = false,
                Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                // access
                if (head == null)
                    return false;
                if (head.Count == 0)
                    return emptyIsTrue;

                // simply test element-wise
                for (int i = 0; i < head.Count; i++)
                {
                    // does head have more elements than this list?
                    if (i >= this.Count)
                        return false;

                    if (!head[i].Matches(this[i], matchMode))
                        return false;
                }

                // ok!
                return true;
            }

            // arithmetics

            public static KeyList operator +(KeyList a, Key b)
            {
                var res = new KeyList(a);
                if (b != null)
                    res.Add(b);
                return res;
            }

            public static KeyList operator +(KeyList a, KeyList b)
            {
                var res = new KeyList(a);
                if (b != null)
                    res.AddRange(b);
                return res;
            }

            public KeyList SubList(int startPos, int count = int.MaxValue)
            {
                var res = new KeyList();
                if (startPos >= this.Count)
                    return res;
                int nr = 0;
                for (int i = startPos; i < this.Count && nr < count; i++)
                {
                    nr++;
                    res.Add(this[i]);
                }
                return res;
            }

            public KeyList ReplaceLastKey(KeyList newKeys)
            {
                var res = new KeyList(this);
                if (res.Count < 1 || newKeys == null || newKeys.Count < 1)
                    return res;

                res.Remove(res.Last());
                res = res + newKeys;

                return res;
            }

            // other

            /// <summary>
            /// Take only idShort, ignore all other key-types and create a '/'-separated list
            /// </summary>
            /// <returns>Empty string or list of idShorts</returns>
            public string BuildIdShortPath(int startPos = 0, int count = int.MaxValue)
            {
                if (startPos >= this.Count)
                    return "";
                int nr = 0;
                var res = "";
                for (int i = startPos; i < this.Count && nr < count; i++)
                {
                    nr++;
                    // V3RC02: quite expensive check: if SME -> then treat as idShort
                    if (this[i].IsInSubmodelElements())
                    {
                        if (res != "")
                            res += "/";
                        res += this[i].value;
                    }
                }
                return res;
            }
        }

        //
        // <<abstract>> Reference
        //

        [XmlType(TypeName = "reference")]
        public class Reference : IAasElement
        {
            // self description

            public virtual AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Reference", "Rfc");
            }

            public virtual string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
            }
        }

        /// <summary>
        /// STILL TODO
        /// </summary>
        public class GlobalReference : Reference
        {
            // members

            [XmlIgnore] // anyway, as it is private/ protected
            [JsonIgnore]
            protected ListOfIdentifier value = new ListOfIdentifier();

            // Keys getters / setters

            [XmlArray("values")]
            [XmlArrayItem("value")]
            [JsonIgnore]
            public ListOfIdentifier Value { get { return value; } }
            [XmlIgnore]
            [JsonProperty(PropertyName = "values")]
            public ListOfIdentifier JsonKeys => value;

            // other members

            [XmlIgnore]
            [JsonIgnore]
            public bool IsEmpty { get { return value == null || value.Count < 1; } }
            [XmlIgnore]
            [JsonIgnore]
            public int Count { get { if (value == null) return 0; return value.Count; } }
            [XmlIgnore]
            [JsonIgnore]
            public Identifier this[int index] { get { return value[index]; } }

            [XmlIgnore]
            [JsonIgnore]
            public Identifier First { get { return this.Count < 1 ? null : this.value[0]; } }

            [XmlIgnore]
            [JsonIgnore]
            public Identifier Last { get { return this.Count < 1 ? null : this.value[this.value.Count - 1]; } }

            // constructors / creators

            public GlobalReference() : base() { }
            public GlobalReference(GlobalReference src) : base() 
            {
                if (src == null)
                    return;

                foreach (var id in src.Value)
                    value.Add(new Identifier(id));
            }

            public GlobalReference(Reference r) : base() { }
            
            public GlobalReference(Identifier id) : base() {
                value.Add(id);
            }

#if !DoNotUseAasxCompatibilityModels
            public GlobalReference(List<AasxCompatibilityModels.AdminShellV10.Key> src)
            {
                if (src == null)
                    return;

                foreach (var k in src)
                    value.Add("" + k?.value);
            }

            public GlobalReference(List<AasxCompatibilityModels.AdminShellV20.Key> src)
            {
                if (src == null)
                    return;

                foreach (var k in src)
                    value.Add("" + k?.value);
            }

            public GlobalReference(AasxCompatibilityModels.AdminShellV10.Reference src)
            {
                if (src == null)
                    return;
                
                foreach (var k in src.Keys)
                    value.Add("" + k?.value);
            }

            public GlobalReference(AasxCompatibilityModels.AdminShellV20.Reference src)
            {
                if (src == null)
                    return;

                foreach (var k in src.Keys)
                    value.Add("" + k?.value);
            }
#endif

            public static GlobalReference CreateNew(Identifier id)
            {
                if (id == null)
                    return null;
                var r = new GlobalReference();
                r.value.Add(id);
                return r;
            }

            public static GlobalReference CreateNew(ListOfIdentifier loi)
            {
                if (loi == null)
                    return null;
                var r = new GlobalReference();
                r.value.AddRange(loi);
                return r;
            }

            // Matching

            public bool MatchesExactlyOneId(Identifier id, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                if (value == null || value.Count != 1)
                    return false;
                return value[0].Matches(id, matchMode);
            }

            public bool Matches(Identifier id, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                if (this.Count == 1)
                    return this[0].Matches(id, matchMode);
                return false;
            }

            public bool Matches(GlobalReference other, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                if (this.value == null || other == null || other.Value == null || other.Count != this.Count)
                    return false;

                var same = true;
                for (int i = 0; i < this.Count; i++)
                    same = same && this.Value[i].Matches(other[i], matchMode);

                return same;
            }

            public bool Matches(ModelReference other, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                if (this.value == null || other == null || other.Keys == null || other.Count != this.Count)
                    return false;

                var same = true;
                for (int i = 0; i < this.Count; i++)
                    same = same && this.Value[i].Matches(other[i], matchMode);

                return same;
            }

            public bool Matches(SemanticId other, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                return Matches(new GlobalReference(other), matchMode);
            }


            // other

            public string ToString(int format = 0, string delimiter = ",")
            {
                return string.Join(delimiter, value);
            }

            public static GlobalReference Parse(string input)
            {
                return CreateNew(ListOfIdentifier.Parse(input));
            }

            /// <summary>
            /// Converts the GlobalReference to a simple Identifier
            /// </summary>
            /// <param name="strict">Check, if exact number of information is available</param>
            /// <returns>Identifier</returns>
            public Identifier GetAsIdentifier(bool strict = false)
            {
                if (value == null || value.Count < 1)
                    return null;
                if (strict && value.Count != 1)
                    return null;
                return value.First().value;
            }

            public Key GetAsExactlyOneKey(string type = null)
            {
                if (value == null || value.Count != 1)
                    return null;
                if (type == null)
                    type = Key.GlobalReference;
                var k = value[0];
                return new Key(type, k.value);
            }

            // self description

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("GlobalReference", "GRf");
            }

            public override string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
            }
        }

        /// <summary>
        /// Receive most of the V2.0 Reference handling
        /// </summary>
        public class ModelReference : Reference
        {
            // members

            public GlobalReference referredSemanticId = null;

            [XmlIgnore] // anyway, as it is private/ protected
            [JsonIgnore]
            protected KeyList keys = new KeyList();

            // Keys getters / setters

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

            // other members

            [XmlIgnore]
            [JsonIgnore]
            public bool IsEmpty { get { return keys == null || keys.Count < 1; } }
            [XmlIgnore]
            [JsonIgnore]
            public int Count { get { if (keys == null) return 0; return keys.Count; } }
            [XmlIgnore]
            [JsonIgnore]
            public Key this[int index] { get { return keys[index]; } }

            [XmlIgnore]
            [JsonIgnore]
            public Key First { get { return this.Count < 1 ? null : this.keys[0]; } }

            [XmlIgnore]
            [JsonIgnore]
            public Key Last { get { return this.Count < 1 ? null : this.keys[this.keys.Count - 1]; } }

            // constructors / creators

            public ModelReference()
            {
            }

            public ModelReference(Key k)
            {
                if (k != null)
                    keys.Add(k);
            }

            public ModelReference(ModelReference src)
            {
                if (src == null)
                    return;

                if (src.referredSemanticId != null)
                    referredSemanticId = new GlobalReference(src.referredSemanticId);

                foreach (var k in src.Keys)
                    keys.Add(new Key(k));
            }

            public ModelReference(GlobalReference src)
            {
                if (src == null)
                    return;

                foreach (var id in src.Value)
                    keys.Add(new Key("", id));
            }

            public ModelReference(SemanticId src, string type = null)
            {
                if (type == null)
                    type = Key.GlobalReference;
                if (src != null)
                    foreach (var id in src.Value)
                        keys.Add(new Key(type, id));
            }

#if !DoNotUseAasxCompatibilityModels
            public ModelReference(AasxCompatibilityModels.AdminShellV10.Reference src)
            {
                if (src != null)
                    foreach (var k in src.Keys)
                        keys.Add(new Key(k));
            }

            public ModelReference(AasxCompatibilityModels.AdminShellV10.SemanticId src)
            {
                if (src != null)
                    foreach (var k in src.Keys)
                        keys.Add(new Key(k));
            }

            public ModelReference(AasxCompatibilityModels.AdminShellV20.Reference src)
            {
                if (src != null)
                    foreach (var k in src.Keys)
                        keys.Add(new Key(k));
            }
#endif

            public static ModelReference CreateNew(Key k)
            {
                if (k == null)
                    return null;
                var r = new ModelReference();
                r.keys.Add(k);
                return r;
            }

            public static ModelReference CreateNew(List<Key> k)
            {
                if (k == null)
                    return null;
                var r = new ModelReference();
                r.keys.AddRange(k);
                return r;
            }

            public static ModelReference CreateNew(string type, string value)
            {
                if (type == null || value == null)
                    return null;
                var r = new ModelReference();
                r.keys.Add(Key.CreateNew(type, value));
                return r;
            }

            public static ModelReference Parse(string input)
            {
                return CreateNew(KeyList.Parse(input));
            }

            public static ModelReference CreateIrdiReference(string irdi)
            {
                if (irdi == null)
                    return null;
                var r = new ModelReference();
                r.keys.Add(new Key(Key.GlobalReference, irdi));
                return r;
            }

            // additions

            public static ModelReference operator +(ModelReference a, Key b)
            {
                var res = new ModelReference(a);
                res.Keys?.Add(b);
                return res;
            }

            public static ModelReference operator +(ModelReference a, ModelReference b)
            {
                var res = new ModelReference(a);
                res.Keys?.AddRange(b?.Keys);
                return res;
            }

            // Matching

            public bool MatchesExactlyOneKey(
                string type, string id, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                if (keys == null || keys.Count != 1)
                    return false;
                var k = keys[0];
                return k.Matches(type, id, matchMode);
            }

            public bool MatchesExactlyOneKey(Key key, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                if (key == null)
                    return false;
                return this.MatchesExactlyOneKey(key.type, key.value, matchMode);
            }

            public bool Matches(
                string type, string id, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                if (this.Count == 1)
                {
                    var k = keys[0];
                    return k.Matches(type, id, matchMode);
                }
                return false;
            }

            public bool Matches(Key key, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                if (this.Count == 1)
                {
                    var k = keys[0];
                    return k.Matches(key, matchMode);
                }
                return false;
            }

            public bool Matches(Identifier other)
            {
                if (other == null)
                    return false;
                if (this.Count == 1)
                {
                    var k = keys[0];
                    return k.Matches(Key.GlobalReference, other.value, Key.MatchMode.Identification);
                }
                return false;
            }

            public bool Matches(ModelReference other, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                if (this.keys == null || other == null || other.keys == null || other.Count != this.Count)
                    return false;

                var same = true;
                for (int i = 0; i < this.Count; i++)
                    same = same && this.keys[i].Matches(other.keys[i], matchMode);

                return same;
            }

            public bool Matches(SemanticId other, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                return Matches(new ModelReference(other), matchMode);
            }

            public bool Matches(ConceptDescription cd, Key.MatchMode matchMode = Key.MatchMode.Relaxed)
            {
                return Matches(cd?.GetReference(), matchMode);
            }

            public string ToString(int format = 0, string delimiter = ",")
            {
                return keys?.ToString(format, delimiter);
            }

            // further

            public Key GetAsExactlyOneKey()
            {
                if (keys == null || keys.Count != 1)
                    return null;
                var k = keys[0];
                return new Key(k.type, k.value);
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

            // self description

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("ModelReference", "MRf");
            }

            public override string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
            }
        }

    }
}
