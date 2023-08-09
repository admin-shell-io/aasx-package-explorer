/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using AdminShell_V20;
using Newtonsoft.Json;

//namespace AdminShellNS
//namespace AdminShell_V20
namespace AasxCompatibilityModels
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

#if !DoNotUseAasxCompatibilityModels
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

            public bool IsIRI()
            {
                return idType?.Trim().ToUpper() == "URI"
                    || idType?.Trim().ToUpper() == IRI;
            }

            public bool IsIRDI()
            {
                return idType?.Trim().ToUpper() == IRDI;
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

#if !DoNotUseAasxCompatibilityModels
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

#if !DoNotUseAasxCompatibilityModels
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

            public Identification ToId()
            {
                return new Identification(this);
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

            public static Key Parse(string cell, string typeIfNotSet = null,
                bool allowFmtAll = false, bool allowFmt0 = false,
                bool allowFmt1 = false, bool allowFmt2 = false)
            {
                // access and defaults?
                if (cell == null || cell.Trim().Length < 1)
                    return null;
                if (typeIfNotSet == null)
                    typeIfNotSet = Key.GlobalReference;

                // format == 1
                if (allowFmtAll || allowFmt1)
                {
                    var m = Regex.Match(cell, @"\((\w+)\)\((\S+)\)\[(\w+)\]( ?)(.*)$");
                    if (m.Success)
                    {
                        return new AdminShell.Key(
                                m.Groups[1].ToString(), m.Groups[2].ToString() == "local",
                                m.Groups[3].ToString(), m.Groups[5].ToString());
                    }
                }

                // format == 2
                if (allowFmtAll || allowFmt2)
                {
                    var m = Regex.Match(cell, @"\[(\w+)\]( ?)(.*)$");
                    if (m.Success)
                    {
                        return new AdminShell.Key(
                                typeIfNotSet, true,
                                m.Groups[1].ToString(), m.Groups[3].ToString());
                    }
                }

                // format == 0
                if (allowFmtAll || allowFmt0)
                {
                    var m = Regex.Match(cell, @"\[(\w+),( ?)([^,]+),( ?)\[(\w+)\],( ?)(.*)\]");
                    if (m.Success)
                    {
                        return new AdminShell.Key(
                                m.Groups[1].ToString(), !m.Groups[3].ToString().Contains("not"),
                                m.Groups[5].ToString(), m.Groups[7].ToString());
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
            "GlobalReference",
            "FragmentReference",
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
            public static string FragmentReference = "FragmentReference";
            public static string ConceptDescription = "ConceptDescription";
            public static string SubmodelRef = "SubmodelRef";
            public static string Submodel = "Submodel";
            public static string SubmodelElement = "SubmodelElement";
            public static string Asset = "Asset";
            public static string AAS = "AssetAdministrationShell";
            public static string Entity = "Entity";
            public static string View = "View";
            // Resharper enable MemberHidesStaticFromOuterClass

            public static string[] IdentifierTypeNames = new string[] {
                Identification.IdShort, "FragmentId", "Custom", Identification.IRDI, Identification.IRI };

            public enum IdentifierType { IdShort = 0, FragmentId, Custom, IRDI, IRI };

            public static string GetIdentifierTypeName(IdentifierType t)
            {
                return IdentifierTypeNames[(int)t];
            }

            public static string IdShort = "IdShort";
            public static string FragmentId = "FragmentId";
            public static string Custom = "Custom";

            // some helpers

            public static bool IsInKeyElements(string ke)
            {
                var res = false;
                foreach (var s in KeyElements)
                    if (s.Trim().ToLower() == ke.Trim().ToLower())
                        res = true;
                return res;
            }

            public bool IsIdType(string[] value)
            {
                if (value == null || idType == null || idType.Trim() == "")
                    return false;
                return value.Contains(idType.Trim());
            }

            public bool IsIdType(string value)
            {
                if (value == null || idType == null || idType.Trim() == "")
                    return false;
                return value.Trim().Equals(idType.Trim());
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
                    || IsType(Key.Asset)
                    || IsType(Key.Submodel);
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

            public bool Matches(Identification id)
            {
                if (id == null)
                    return false;
                return this.Matches(Key.GlobalReference, false, id.idType, id.id, MatchMode.Identification);
            }

            public bool Matches(Key key, MatchMode matchMode = MatchMode.Strict)
            {
                if (key == null)
                    return false;
                return this.Matches(key.type, key.local, key.idType, key.value, matchMode);
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
                    // check IdType
                    var idf = AdminShellUtilV20.CheckIfInConstantStringArray(IdentifierTypeNames, k.idType);
                    if (idf == AdminShellUtilV20.ConstantFoundEnum.No)
                        // violation case
                        results.Add(new AasValidationRecord(
                            AasValidationSeverity.SchemaViolation, container,
                            "Key: idType is not in allowed enumeration values",
                            () =>
                            {
                                k.idType = Custom;
                            }));
                    if (idf == AdminShellUtilV20.ConstantFoundEnum.AnyCase)
                        // violation case
                        results.Add(new AasValidationRecord(
                            AasValidationSeverity.SchemaViolation, container,
                            "Key: idType in wrong casing",
                            () =>
                            {
                                k.idType = AdminShellUtilV20.CorrectCasingForConstantStringArray(
                                    IdentifierTypeNames, k.idType);
                            }));

                    // check type
                    var tf = AdminShellUtilV20.CheckIfInConstantStringArray(KeyElements, k.type);
                    if (tf == AdminShellUtilV20.ConstantFoundEnum.No)
                        // violation case
                        results.Add(new AasValidationRecord(
                            AasValidationSeverity.SchemaViolation, container,
                            "Key: type is not in allowed enumeration values",
                            () =>
                            {
                                k.type = GlobalReference;
                            }));
                    if (tf == AdminShellUtilV20.ConstantFoundEnum.AnyCase)
                        // violation case
                        results.Add(new AasValidationRecord(
                            AasValidationSeverity.SchemaViolation, container,
                            "Key: type in wrong casing",
                            () =>
                            {
                                k.idType = AdminShellUtilV20.CorrectCasingForConstantStringArray(
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

            public static KeyList CreateNew(string type, bool local, string idType, string value)
            {
                var kl = new KeyList() {
                    Key.CreateNew(type, local, idType, value)
                };
                return kl;
            }

            public static KeyList CreateNew(string type, bool local, string idType, string[] valueItems)
            {
                // access
                if (valueItems == null)
                    return null;

                // prepare
                var kl = new AdminShell.KeyList();
                foreach (var x in valueItems)
                    kl.Add(new AdminShell.Key(type, local, idType, "" + x));
                return kl;
            }

            // matches

            public bool Matches(KeyList other, Key.MatchMode matchMode = Key.MatchMode.Strict)
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
                var res = string.Join(delimiter, this.Select((k) => k.ToString(format)));
                return res;
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
                if (this[i].IsIdType(new[] { Key.FragmentId }) && i > 0)
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
                Key.MatchMode matchMode = Key.MatchMode.Strict)
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
                    if (this[i].idType.Trim().ToLower() == Key.IdShort.Trim().ToLower())
                    {
                        if (res != "")
                            res += "/";
                        res += this[i].value;
                    }
                }
                return res;
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

        /// <summary>
        /// Extends understanding of Referable to further elements, which can be related to
        /// </summary>
        public interface IAasElement
        {
            AasElementSelfDescription GetSelfDescription();
            string GetElementName();
        }

        [XmlType(TypeName = "reference")]
        public class Reference : IAasElement
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

            [XmlIgnore]
            [JsonIgnore]
            public Key First { get { return this.Count < 1 ? null : this.keys[0]; } }

            [XmlIgnore]
            [JsonIgnore]
            public Key Last { get { return this.Count < 1 ? null : this.keys[this.keys.Count - 1]; } }

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

#if !DoNotUseAasxCompatibilityModels
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

#if !DoNotUseAasxCompatibilityModels
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

            // additions

            public static Reference operator +(Reference a, Key b)
            {
                var res = new Reference(a);
                res.Keys?.Add(b);
                return res;
            }

            public static Reference operator +(Reference a, Reference b)
            {
                var res = new Reference(a);
                res.Keys?.AddRange(b?.Keys);
                return res;
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

            public bool Matches(SemanticId other, Key.MatchMode matchMode = Key.MatchMode.Strict)
            {
                return Matches(new Reference(other), matchMode);
            }

            public bool Matches(ConceptDescription cd, Key.MatchMode matchMode = Key.MatchMode.Strict)
            {
                return Matches(cd?.GetReference(), matchMode);
            }

            public string ToString(int format = 0, string delimiter = ",")
            {
                return keys?.ToString(format, delimiter);
            }

            public static Reference Parse(string input)
            {
                return CreateNew(KeyList.Parse(input));
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

            public virtual AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Reference", "Rfc");
            }

            public virtual string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
            }
        }

        [XmlType(TypeName = "derivedFrom")]
        public class AssetAdministrationShellRef : Reference
        {
            // constructors

            public AssetAdministrationShellRef() : base() { }

            public AssetAdministrationShellRef(Key k) : base(k) { }

            public AssetAdministrationShellRef(Reference src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
            public AssetAdministrationShellRef(AasxCompatibilityModels.AdminShellV10.Reference src) : base(src) { }
#endif

            // further methods

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("AssetAdministrationShellRef", "AasRef");
            }
        }

        [XmlType(TypeName = "assetRef")]
        public class AssetRef : Reference
        {
            // constructors

            public AssetRef() : base() { }

            public AssetRef(AssetRef src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
            public AssetRef(AasxCompatibilityModels.AdminShellV10.AssetRef src) : base(src) { }
#endif

            public AssetRef(Reference r)
                : base(r)
            {
            }

            // further methods

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("AssetRef", "AssetRef");
            }
        }

        [XmlType(TypeName = "submodelRef")]
        public class SubmodelRef : Reference
        {
            // constructors

            public SubmodelRef() : base() { }

            public SubmodelRef(SubmodelRef src) : base(src) { }

            public SubmodelRef(Reference src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
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

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("SubmodelRef", "SMRef");
            }
        }

        [XmlType(TypeName = "conceptDescriptionRef")]
        public class ConceptDescriptionRef : Reference
        {
            // constructors

            public ConceptDescriptionRef() : base() { }

            public ConceptDescriptionRef(ConceptDescriptionRef src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
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

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("ConceptDescriptionRef", "CDRef");
            }

        }

        [XmlType(TypeName = "dataSpecificationRef")]
        public class DataSpecificationRef : Reference
        {
            // constructors

            public DataSpecificationRef() : base() { }

            public DataSpecificationRef(DataSpecificationRef src) : base(src) { }

            public DataSpecificationRef(Reference src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
            public DataSpecificationRef(AasxCompatibilityModels.AdminShellV10.DataSpecificationRef src) : base(src) { }

            public DataSpecificationRef(AasxCompatibilityModels.AdminShellV10.Reference src) : base(src) { }
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
#endif
        }

        [XmlType(TypeName = "containedElementRef")]
        public class ContainedElementRef : Reference
        {
            // constructors

            public ContainedElementRef() { }

            public ContainedElementRef(ContainedElementRef src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
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

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("ContainedElementRef", "CERef");
            }

        }

#if __not_valid_anymore
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

#if !DoNotUseAasxCompatibilityModels
            public HasDataSpecification(AasxCompatibilityModels.AdminShellV10.HasDataSpecification src)
            {
                foreach (var r in src.reference)
                    reference.Add(new Reference(r));
            }
#endif
        }
#else
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
#endif

            // make some explicit and easy to use getter, setters            

            [XmlIgnore]
            [JsonIgnore]
            public EmbeddedDataSpecification IEC61360
            {
                get
                {
                    foreach (var eds in this)
                        if (eds?.dataSpecificationContent?.dataSpecificationIEC61360 != null
                            || eds?.dataSpecification?.MatchesExactlyOneKey(
                                DataSpecificationIEC61360.GetKey(), Key.MatchMode.Identification) == true)
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

#if !DoNotUseAasxCompatibilityModels
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

#if !DoNotUseAasxCompatibilityModels
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

            public string GetExactStrForLang(string lang)
            {
                // start
                if (lang == null)
                    return null;
                string res = null;

                // exact search
                foreach (var ls in this)
                    if (ls.lang.Trim().ToLower() == lang)
                        res = ls.str;

                // found?
                return res;
            }

            public bool ContainsLang(string lang)
            {
                return GetExactStrForLang(lang) != null;
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

#if !DoNotUseAasxCompatibilityModels
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

            public new static SemanticId Parse(string input)
            {
                return (SemanticId)CreateNew(KeyList.Parse(input));
            }
        }

        /// <summary>
        /// This class allows to describe further data (in derived classes) when enumerating Children.
        /// </summary>
        public class EnumerationPlacmentBase
        {
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

        /// <summary>
        /// Result of FindReferable in Environment
        /// </summary>
        public class ReferableRootInfo
        {
            public AdministrationShell AAS = null;
            public Asset Asset = null;
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

        /// <summary>
        /// Marks an object, preferaby a payload item, which might be featured by the diary collection
        /// </summary>
        public interface IAasDiaryEntry
        {
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

        public interface IDiaryData
        {
            DiaryDataDef DiaryData { get; }
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

            public Reference GetReference()
            {
                return Reference.CreateNew(ToKeyList());
            }
        }

        public interface IRecurseOnReferables
        {
            void RecurseOnReferables(
                object state, Func<object, ListOfReferable, Referable, bool> lambda,
                bool includeThis = false);
        }

        public class Referable : IValidateEntity, IAasElement, IDiaryData, IGetReference, IRecurseOnReferables
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
#endif

            /// <summary>
            /// Introduced for JSON serialization, can create Referables based on a string name
            /// </summary>
            /// <param name="elementName">string name (standard PascalCased)</param>
            public static Referable CreateAdequateType(string elementName)
            {
                if (elementName == Key.AAS)
                    return new AdministrationShell();
                if (elementName == Key.Asset)
                    return new Asset();
                if (elementName == Key.ConceptDescription)
                    return new ConceptDescription();
                if (elementName == Key.Submodel)
                    return new Submodel();
                if (elementName == Key.View)
                    return new View();
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
                return AdminShellUtilV20.FilterFriendlyName(this.idShort);
            }

            public virtual Reference GetReference(bool includeParents = true)
            {
                return new Reference(
                    new AdminShell.Key(
                        this.GetElementName(), false, Key.IdShort, "" + this.idShort));
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
                return new Key(GetElementName(), true, Key.IdShort, idShort);
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

        public class Identifiable : Referable, IGetReference
        {

            // members

            public Identification identification = new Identification();
            public Administration administration = null;

            // constructors

            public Identifiable() : base() { }

            public Identifiable(string idShort) : base(idShort) { }

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

#if !DoNotUseAasxCompatibilityModels
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
                    return AdminShellUtilV20.FilterFriendlyName(this.identification.id);
                return AdminShellUtilV20.FilterFriendlyName(this.idShort);
            }

            public override string ToString()
            {
                return ("" + identification?.ToString() + " " + administration?.ToString()).Trim();
            }

            public override Key ToKey()
            {
                return new Key(GetElementName(), true, "" + identification?.idType, "" + identification?.id);
            }

            // self description

            public override Reference GetReference(bool includeParents = true)
            {
                var r = new Reference();
                r.Keys.Add(
                    Key.CreateNew(
                        this.GetElementName(), true, this.identification.idType, this.identification.id));
                return r;
            }

            // sorting

            public class ComparerIdentification : IComparer<Identifiable>
            {
                public int Compare(Identifiable a, Identifiable b)
                {
                    if (a?.identification == null && b?.identification == null)
                        return 0;
                    if (a?.identification == null)
                        return +1;
                    if (b?.identification == null)
                        return -1;

                    var vc = String.Compare(a.identification.idType, b.identification.idType,
                        CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
                    if (vc != 0)
                        return vc;

                    return String.Compare(a.identification.id, b.identification.id,
                        CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
                }
            }

        }

        public class JsonModelTypeWrapper
        {
            public string name = "";

            public JsonModelTypeWrapper(string name = "") { this.name = name; }
        }

        public interface IFindAllReferences
        {
            IEnumerable<LocatedReference> FindAllReferences();
        }

        public interface IGetSemanticId
        {
            SemanticId GetSemanticId();
        }

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
#endif

            public static AdministrationShell CreateNew(
                string idShort, string idType, string id, string version = null, string revision = null)
            {
                var s = new AdministrationShell();
                s.idShort = idShort;
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
                hasDataSpecification.Add(new EmbeddedDataSpecification(r));
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("AssetAdministrationShell", "AAS");
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtilV20.EvalToNonNullString("\"{0}\" ", idShort, "\"AAS\"");
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
#endif

            // Getter & setters

            public AssetRef GetAssetReference()
            {
                var r = new AssetRef();
                r.Keys.Add(
                    Key.CreateNew(
                        this.GetElementName(), true, this.identification.idType, this.identification.id));
                return r;
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Asset", "Asset");
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtilV20.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
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

#if !DoNotUseAasxCompatibilityModels
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
                hasDataSpecification.Add(new EmbeddedDataSpecification(r));
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

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("View", "View");
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtilV20.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
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

            // validation

            public override void Validate(AasValidationRecordList results)
            {
                // access
                if (results == null)
                    return;

                // check
                base.Validate(results);
                KeyList.Validate(results, semanticId?.Keys, this);
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

#if !DoNotUseAasxCompatibilityModels
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

#if !DoNotUseAasxCompatibilityModels
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
                                AdminShellUtilV20.EvalToNonEmptyString("{0}", cd.idShort, "UNKNOWN"));
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
#endif

            public static ConceptDescription CreateNew(
                string idShort, string idType, string id, string version = null, string revision = null)
            {
                var cd = new ConceptDescription();
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

            public ConceptDescriptionRef GetCdReference()
            {
                var r = new ConceptDescriptionRef();
                r.Keys.Add(
                    Key.CreateNew(
                        this.GetElementName(), true, this.identification.idType, this.identification.id));
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
                    Reference.CreateNew(
                        new Key("ConceptDescription", false, this.identification.idType, this.identification.id)));
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

        /// <summary>
        /// Use by <c>FindAllReference</c> to provide a enumeration of referenced with location
        /// info, where they are contained
        /// </summary>
        public class LocatedReference
        {
            public Identifiable Identifiable;
            public Reference Reference;

            public LocatedReference() { }
            public LocatedReference(Identifiable identifiable, Reference reference)
            {
                Identifiable = identifiable;
                Reference = reference;
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
            private ListOfAssets assets = new ListOfAssets();
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

            [XmlArray("assets")]
            [XmlArrayItem("asset")]
            [JsonProperty(PropertyName = "assets")]
            public ListOfAssets Assets
            {
                get { return assets; }
                set { assets = value; }
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

            // to String

            public override string ToString()
            {
                var res = "AAS-ENV";
                if (AdministrationShells != null)
                    res += $" {AdministrationShells.Count} AAS";
                if (Assets != null)
                    res += $" {Assets.Count} Assets";
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

                            if (!onlyIdentifiables)
                            {
                                // Views
                                if (aas.views?.views != null)
                                    foreach (var view in aas.views.views)
                                        yield return view;
                            }
                        }

                if (this.Assets != null)
                    foreach (var asset in this.Assets)
                        if (asset != null)
                            yield return asset;

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

            public Referable FindReferableByReference(Reference rf, int keyIndex = 0, bool exactMatch = false)
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
                var firstIdentification = new Identification(kl[keyIndex].idType, kl[keyIndex].value);
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

                if (firstType == Key.Asset.Trim().ToLower())
                {
                    // search asset
                    var asset = this.FindAsset(firstIdentification);

                    // not found or already at end with our search?
                    if (asset == null || keyIndex >= kl.Count - 1)
                        return exactMatch ? null : asset;

                    // try find aas for it
                    var aas = this.FindAllAAS((a) =>
                    {
                        return a?.assetRef?.Matches(asset.identification) == true;
                    }).FirstOrDefault();
                    if (aas == null)
                        return exactMatch ? null : asset;

                    // side info?
                    if (rootInfo != null)
                    {
                        rootInfo.Asset = asset;
                        rootInfo.NrOfRootKeys = 1 + keyIndex;
                    }

                    // follow up
                    aasToFollow = aas;
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
                    var sm = this.FindSubmodel(new Identification(kl[keyIndex].idType, kl[keyIndex].value));
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
                                var smref2 = aas2.FindSubmodelRef(sm.identification);
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

                if (this.Assets != null)
                    foreach (var asset in this.Assets)
                        if (asset != null)
                            foreach (var r in asset.FindAllReferences())
                                yield return new LocatedReference(asset, r);

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
            /// Returns a list of Referables, which were changed or <c>null</c> in case of error
            /// </summary>
            public List<Referable> RenameIdentifiable<T>(Identification oldId, Identification newId)
                where T : Identifiable
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
                    cdOld.identification = newId;
                    res.Add(cdOld);

                    // search all SMEs referring to this CD
                    foreach (var sme in this.FindAllSubmodelElements<SubmodelElement>(match: (s) =>
                    {
                        return (s != null && s.semanticId != null && s.semanticId.Matches(oldId));
                    }))
                    {
                        sme.semanticId[0].idType = newId.idType;
                        sme.semanticId[0].value = newId.id;
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
                                if (r[i].Matches(Key.Submodel, false, oldId.idType, oldId.id, Key.MatchMode.Relaxed))
                                {
                                    // directly replace
                                    r[i].idType = newId.idType;
                                    r[i].value = newId.id;
                                    if (res.Contains(lr.Identifiable))
                                        res.Add(lr.Identifiable);
                                }
                    }

                    // rename old Submodel
                    smOld.identification = newId;

                    // seems fine
                    return res;
                }

                if (typeof(T) == typeof(Asset))
                {
                    // check, if exist or not exist
                    var assetOld = FindAsset(oldId);
                    if (assetOld == null || FindAsset(newId) != null)
                        return null;

                    // recurse all possible Referenes in the aas env
                    foreach (var lr in this.FindAllReferences())
                    {
                        var r = lr?.Reference;
                        if (r != null)
                            for (int i = 0; i < r.Count; i++)
                                if (r[i].Matches(Key.Asset, false, oldId.idType, oldId.id, Key.MatchMode.Relaxed))
                                {
                                    // directly replace
                                    r[i].idType = newId.idType;
                                    r[i].value = newId.id;
                                    if (res.Contains(lr.Identifiable))
                                        res.Add(lr.Identifiable);
                                }
                    }

                    // rename old Submodel
                    assetOld.identification = newId;

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
                ListOfSubmodels filterForSubmodel = null,
                List<ConceptDescription> filterForCD = null)
            {
                // prepare defaults
                if (filterForAas == null)
                    filterForAas = new List<AdministrationShell>();
                if (filterForAsset == null)
                    filterForAsset = new List<Asset>();
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

        public interface IGetReference
        {
            Reference GetReference(bool includeParents = true);
        }

        public interface IGetQualifiers
        {
            QualifierCollection GetQualifiers();
        }

        public class Qualifier : IAasElement
        {
            // for JSON only
            [XmlIgnore]
            [JsonProperty(PropertyName = "modelType")]
            public JsonModelTypeWrapper JsonModelType { get { return new JsonModelTypeWrapper(GetElementName()); } }

            // member
            // from hasSemantics:
            [XmlElement(ElementName = "semanticId")]
            public SemanticId semanticId = null;

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
            public Reference valueId = null;

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
                    this.valueId = new Reference(src.valueId);
            }

            public Qualifier(string type, string value)
            {
                this.type = type;
                this.value = value;
            }

#if !DoNotUseAasxCompatibilityModels
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
                    valueId = Reference.Parse(m.Groups[1].ToString().Trim())
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
                Qualifier q)
            {
                if (q == null)
                    return;
                if (qualifiers == null)
                    qualifiers = new QualifierCollection();
                qualifiers.Add(q);
            }

            public static void AddQualifier(
                ref QualifierCollection qualifiers,
                string qualifierType = null, string qualifierValue = null, KeyList semanticKeys = null,
                Reference qualifierValueId = null)
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

        public class ListOfSubmodelElement : List<SubmodelElement>
        {
            // conversion to other list

            public KeyList ToKeyList()
            {
                var res = new KeyList();
                foreach (var sme in this)
                    res.Add(sme.ToKey());
                return res;
            }

            public Reference GetReference()
            {
                return Reference.CreateNew(ToKeyList());
            }
        }

        public class SubmodelElement : Referable, System.IDisposable, IGetReference, IGetSemanticId, IGetQualifiers
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
            [JsonProperty(PropertyName = "constraints")]
            public QualifierCollection qualifiers = null;
            public QualifierCollection GetQualifiers() => qualifiers;

            // from hasDataSpecification:
            [XmlElement(ElementName = "embeddedDataSpecification")]
            public HasDataSpecification hasDataSpecification = null;

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

#if !DoNotUseAasxCompatibilityModels
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

            public static T CreateNew<T>(string idShort = null, string category = null, Reference semanticId = null)
                where T : SubmodelElement, new()
            {
                var res = new T();
                if (idShort != null)
                    res.idShort = idShort;
                if (category != null)
                    res.category = category;
                if (semanticId != null)
                    res.semanticId = new SemanticId(semanticId);
                return res;
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
                Qualifier q)
            {
                QualifierCollection.AddQualifier(
                    ref this.qualifiers, q);
            }

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
                return new AasElementSelfDescription("SubmodelElement", "SME");
            }

            public override Reference GetReference(bool includeParents = true)
            {
                Reference r = new Reference();
                // this is the tail of our referencing chain ..
                r.Keys.Add(Key.CreateNew(GetElementName(), true, "IdShort", this.idShort));
                // try to climb up ..
                var current = this.parent;
                while (includeParents && current != null)
                {
                    if (current is Identifiable cid)
                    {
                        // add big information set
                        r.Keys.Insert(0, Key.CreateNew(
                            current.GetElementName(),
                            true,
                            cid.identification.idType,
                            cid.identification.id));
                    }
                    else
                    if (current is Referable crf)
                    {
                        // reference via idShort
                        r.Keys.Insert(0, Key.CreateNew(
                            current.GetElementName(),
                            true,
                            "IdShort", crf.idShort));
                    }

                    if (current is Referable crf2)
                        current = crf2.parent;
                    else
                        current = null;
                }
                return r;
            }

            public IEnumerable<Referable> FindAllParents(
                Predicate<Referable> p,
                bool includeThis = false, bool includeSubmodel = false,
                bool passOverMiss = false)
            {
                // call for this?
                if (includeThis)
                {
                    if (p == null || p.Invoke(this))
                        yield return this;
                    else
                        if (!passOverMiss)
                        yield break;
                }

                // daisy chain all parents ..
                if (this.parent != null)
                {
                    if (this.parent is SubmodelElement psme)
                    {
                        foreach (var q in psme.FindAllParents(p, includeThis: true,
                            passOverMiss: passOverMiss))
                            yield return q;
                    }
                    else if (includeSubmodel && this.parent is Submodel psm)
                    {
                        if (p == null || p.Invoke(psm))
                            yield return this;
                    }
                }
            }

            public IEnumerable<Referable> FindAllParentsWithSemanticId(
                SemanticId semId,
                bool includeThis = false, bool includeSubmodel = false, bool passOverMiss = false)
            {
                return (FindAllParents(
                    (rf) => (true == (rf as IGetSemanticId)?.GetSemanticId()?.Matches(semId,
                        matchMode: Key.MatchMode.Relaxed)),
                    includeThis: includeThis, includeSubmodel: includeSubmodel, passOverMiss: passOverMiss));
            }

            public Tuple<string, string> ToCaptionInfo()
            {
                var caption = AdminShellUtilV20.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
                var info = "";
                // TODO (MIHO, 2021-07-08): obvious error .. info should receive semanticId .. but would change 
                // display presentation .. therefore to be checked again
                if (semanticId != null)
                    AdminShellUtilV20.EvalToNonEmptyString("\u21e8 {0}", semanticId.ToString(), "");
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

            public virtual double? ValueAsDouble()
            {
                return null;
            }

            public virtual void ValueFromText(string text, string defaultLang = null)
            {
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

            public static string[] AdequateElementShortName = { null, "SMC", null,
                "MLP", null, null, null, "Ref", "Rel",
                "ARel", null, null, "Event", "Entity" };

            // constructors

            public SubmodelElementWrapper() { }

            // cloning

            public SubmodelElementWrapper(SubmodelElement src, bool shallowCopy = false)
            {
                /* TODO (MIHO, 2021-08-12): consider using:
                   Activator.CreateInstance(pl.GetType(), new object[] { pl }) */

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

#if !DoNotUseAasxCompatibilityModels
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

            /// <summary>
            /// Deprecated. See below.
            /// </summary>
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

            /// <summary>
            /// This version uses the element name array and allows using ShortName
            /// </summary>
            public static AdequateElementEnum GetAdequateEnum2(string adequateName, bool useShortName = false)
            {
                if (adequateName == null)
                    return AdequateElementEnum.Unknown;

                foreach (var en in (AdequateElementEnum[])Enum.GetValues(typeof(AdequateElementEnum)))
                    if (((int)en < AdequateElementNames.Length
                          && AdequateElementNames[(int)en].Trim().ToLower() == adequateName.Trim().ToLower())
                        || (useShortName
                          && (int)en < AdequateElementShortName.Length
                          && AdequateElementShortName[(int)en] != null
                          && AdequateElementShortName[(int)en].Trim().ToLower() == adequateName.Trim().ToLower()))
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

            public string GetElementAbbreviation()
            {
                if (submodelElement == null)
                    return ("Null");
                var dsc = submodelElement.GetSelfDescription();
                if (dsc?.ElementAbbreviation == null)
                    return ("Null");
                return dsc.ElementAbbreviation;
            }

            public static string GetElementNameByAdequateType(SubmodelElement sme)
            {
                // access
                var sd = sme.GetSelfDescription();
                if (sd == null || sd.ElementEnum == AdequateElementEnum.Unknown)
                    return null;
                var en = sd.ElementEnum;

                // get the names
                string res = null;
                if ((int)en < AdequateElementNames.Length)
                    res = AdequateElementNames[(int)en].Trim();
                if ((int)en < AdequateElementShortName.Length && AdequateElementShortName[(int)en] != null)
                    res = AdequateElementShortName[(int)en].Trim();
                return res;
            }

            public static ListOfSubmodelElement ListOfWrappersToListOfElems(List<SubmodelElementWrapper> wrappers)
            {
                var res = new ListOfSubmodelElement();
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
                return FindReferableByReference(wrappers, rf?.Keys, keyIndex);
            }

            public static Referable FindReferableByReference(
                List<SubmodelElementWrapper> wrappers, KeyList rf, int keyIndex)
            {
                // first index needs to exist ..
                if (wrappers == null || rf == null || keyIndex >= rf.Count)
                    return null;

                // as SubmodelElements are not Identifiables, the actual key shall be IdShort
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

            public SubmodelElementWrapperCollection(SubmodelElementWrapper smw) : base(smw) { }

            public SubmodelElementWrapperCollection(SubmodelElement sme) : base(sme) { }

            public SubmodelElementWrapperCollection(SubmodelElementWrapperCollection other) : base(other) { }
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

            // member: Parent
            // will be held correctly by the containing class
            public Referable Parent = null;

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

            public BaseSubmodelElementWrapperCollection(SubmodelElementWrapper smw)
                : base()
            {
                if (smw != null)
                    this.Add(smw);
            }

            public BaseSubmodelElementWrapperCollection(SubmodelElement sme)
                : base()
            {
                if (sme != null)
                    this.Add(new SubmodelElementWrapper(sme));
            }

            // better find functions

            public IEnumerable<T> FindDeep<T>(Predicate<T> match = null) where T : SubmodelElement
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
                    // TODO (MIHO, 2020-07-31): would be nice to use IEnumerateChildren for this ..
                    if (current is SubmodelElementCollection smc && smc.value != null)
                        foreach (var x in smc.value.FindDeep<T>(match))
                            yield return x;

                    if (current is AnnotatedRelationshipElement are && are.annotations != null)
                        foreach (var x in are.annotations.FindDeep<T>(match))
                            yield return x;

                    if (current is Entity ent && ent.statements != null)
                        foreach (var x in ent.statements.FindDeep<T>(match))
                            yield return x;

                    if (current is Operation op)
                        for (int i = 0; i < 2; i++)
                            if (Operation.GetWrappers(op[i]) != null)
                                foreach (var x in Operation.GetWrappers(op[i]).FindDeep<T>(match))
                                    yield return x;
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

            public IEnumerable<SubmodelElementWrapper> FindAllSemanticId(
                Key semId, Type[] allowedTypes = null, Key.MatchMode matchMode = Key.MatchMode.Strict)
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

                        if (smw.submodelElement.semanticId.MatchesExactlyOneKey(semId, matchMode))
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

            public IEnumerable<T> FindAllSemanticIdAs<T>(Reference semId,
                Key.MatchMode matchMode = Key.MatchMode.Strict)
                where T : SubmodelElement
            {
                foreach (var smw in this)
                    if (smw.submodelElement != null && smw.submodelElement is T
                        && smw.submodelElement.semanticId != null)
                        if (smw.submodelElement.semanticId.Matches(semId, matchMode))
                            yield return smw.submodelElement as T;
            }

            public IEnumerable<T> FindAllSemanticIdAs<T>(ConceptDescription cd,
                Key.MatchMode matchMode = Key.MatchMode.Strict)
                where T : SubmodelElement
            {
                foreach (var x in FindAllSemanticIdAs<T>(cd.GetReference(), matchMode))
                    yield return x;
            }

            public SubmodelElementWrapper FindFirstSemanticId(
                Key semId, Type[] allowedTypes = null, Key.MatchMode matchMode = Key.MatchMode.Strict)
            {
                return FindAllSemanticId(semId, allowedTypes, matchMode)?.FirstOrDefault<SubmodelElementWrapper>();
            }

            public SubmodelElementWrapper FindFirstAnySemanticId(
                Key[] semId, Type[] allowedTypes = null, Key.MatchMode matchMode = Key.MatchMode.Strict)
            {
                if (semId == null)
                    return null;
                foreach (var si in semId)
                {
                    var found = FindAllSemanticId(si, allowedTypes, matchMode)?
                                .FirstOrDefault<SubmodelElementWrapper>();
                    if (found != null)
                        return found;
                }
                return null;
            }

            public T FindFirstSemanticIdAs<T>(Key semId, Key.MatchMode matchMode = Key.MatchMode.Strict)
                where T : SubmodelElement
            {
                return FindAllSemanticIdAs<T>(semId, matchMode)?.FirstOrDefault<T>();
            }

            public T FindFirstAnySemanticIdAs<T>(Key[] semId, Key.MatchMode matchMode = Key.MatchMode.Strict)
                where T : SubmodelElement
            {
                if (semId == null)
                    return null;
                foreach (var si in semId)
                {
                    var found = FindAllSemanticIdAs<T>(si, matchMode)?.FirstOrDefault<T>();
                    if (found != null)
                        return found;
                }
                return null;
            }

            public T FindFirstSemanticIdAs<T>(Reference semId, Key.MatchMode matchMode = Key.MatchMode.Strict)
                where T : SubmodelElement
            {
                return FindAllSemanticIdAs<T>(semId, matchMode)?.FirstOrDefault<T>();
            }

            /* TODO (MIHO, 2021-10-18): there are overlaps of this new function with
             * this old function: FindFirstAnySemanticId(Key[] semId ..
             * clarify/ refactor */
            public IEnumerable<T> FindAllSemanticId<T>(
                Key[] allowedSemId, Key.MatchMode matchMode = Key.MatchMode.Strict,
                bool invertAllowed = false)
                where T : SubmodelElement
            {
                if (allowedSemId == null || allowedSemId.Length < 1)
                    yield break;

                foreach (var smw in this)
                {
                    if (smw.submodelElement == null || !(smw.submodelElement is T))
                        continue;

                    if (smw.submodelElement.semanticId == null || smw.submodelElement.semanticId.Count < 1)
                    {
                        if (invertAllowed)
                            yield return smw.submodelElement as T;
                        continue;
                    }

                    var found = false;
                    foreach (var semId in allowedSemId)
                        if (smw.submodelElement.semanticId.MatchesExactlyOneKey(semId, matchMode))
                        {
                            found = true;
                            break;
                        }

                    if (invertAllowed)
                        found = !found;

                    if (found)
                        yield return smw.submodelElement as T;
                }
            }

            public T FindFirstAnySemanticId<T>(
                Key[] allowedSemId, Key.MatchMode matchMode = Key.MatchMode.Strict,
                bool invertAllowed = false)
                where T : SubmodelElement
            {
                return FindAllSemanticId<T>(allowedSemId, matchMode, invertAllowed)?.FirstOrDefault<T>();
            }

            // recursion

            /// <summary>
            /// Recurses on all Submodel elements of a Submodel or SME, which allows children.
            /// The <c>state</c> object will be passed to the lambda function in order to provide
            /// stateful approaches. Also a list of <c>parents</c> will be provided to
            /// the lambda. This list of parents can be initialized or simply set to <c>null</c>
            /// in order to be created automatically.
            /// </summary>
            /// <param name="state">State object to be provided to lambda. Could be <c>null.</c></param>
            /// <param name="parents">List of already existing parents to be provided to lambda. 
            /// Could be <c>null.</c></param>
            /// <param name="lambda">The lambda function as <c>(state, parents, SME)</c>
            /// The lambda shall return <c>TRUE</c> in order to deep into recursion.
            /// </param>
            public void RecurseOnReferables(
                object state, ListOfReferable parents,
                Func<object, ListOfReferable, Referable, bool> lambda)
            {
                // trivial
                if (lambda == null)
                    return;
                if (parents == null)
                    parents = new ListOfReferable();

                // over all elements
                foreach (var smw in this)
                {
                    var current = smw.submodelElement;
                    if (current == null)
                        continue;

                    // call lambda for this element
                    // AND decide, if to recurse!
                    var goDeeper = lambda(state, parents, current);

                    if (goDeeper)
                    {
                        // add to parents
                        parents.Add(current);

                        // dive into?
                        if (current is SubmodelElementCollection smc)
                            smc.value?.RecurseOnReferables(state, parents, lambda);

                        if (current is Entity ent)
                            ent.statements?.RecurseOnReferables(state, parents, lambda);

                        if (current is Operation op)
                            for (int i = 0; i < 2; i++)
                                Operation.GetWrappers(op[i])?.RecurseOnReferables(state, parents, lambda);

                        if (current is AnnotatedRelationshipElement arel)
                            arel.annotations?.RecurseOnReferables(state, parents, lambda);

                        // remove from parents
                        parents.RemoveAt(parents.Count - 1);
                    }
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
                sme.parent = this.Parent;
                this.Add(SubmodelElementWrapper.CreateFor(sme));
            }

            /// <summary>
            /// Add <c>sme</c> by creating a SubmodelElementWrapper for it and adding to this collection.
            /// </summary>
            public void Insert(int index, SubmodelElement sme)
            {
                if (sme == null || index < 0 || index >= this.Count)
                    return;
                sme.parent = this.Parent;
                this.Insert(index, SubmodelElementWrapper.CreateFor(sme));
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
                string idxTemplate = null, int maxNum = 999, bool addSme = false, bool isTemplate = false)
                where T : SubmodelElement, new()
            {
                // access
                if (cd == null)
                    return null;

                // try to potentially figure out idShort
                var ids = cd.idShort;
                if ((ids == null || ids.Trim() == "") && cd.GetIEC61360() != null)
                    ids = cd.GetIEC61360().shortName?
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
                    semanticId = new SemanticId(cd.GetCdReference())
                };
                if (category != null)
                    sme.category = category;
                if (isTemplate)
                    sme.kind = ModelingKind.CreateAsTemplate();

                // if its a SMC, make sure its accessible
                if (sme is SubmodelElementCollection smc)
                    smc.value = new SubmodelElementWrapperCollection();

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

            // for conversion

            public T AdaptiveConvertTo<T>(
                SubmodelElement anySrc,
                ConceptDescription createDefault = null,
                string idShort = null, bool addSme = false) where T : SubmodelElement, new()
            {
                if (typeof(T) == typeof(MultiLanguageProperty)
                        && anySrc is Property srcProp)
                {
                    var res = this.CreateSMEForCD<T>(createDefault, idShort: idShort, addSme: addSme);
                    if (res is MultiLanguageProperty mlp)
                    {
                        mlp.value = new LangStringSet("EN?", srcProp.value);
                        mlp.valueId = srcProp.valueId;
                        return res;
                    }
                }

                if (typeof(T) == typeof(Property)
                        && anySrc is MultiLanguageProperty srcMlp)
                {
                    var res = this.CreateSMEForCD<T>(createDefault, idShort: idShort, addSme: addSme);
                    if (res is Property prp)
                    {
                        prp.value = "" + srcMlp.value?.GetDefaultStr();
                        prp.valueId = srcMlp.valueId;
                        return res;
                    }
                }

                return null;
            }

            public T CopyOneSMEbyCopy<T>(Key destSemanticId,
                SubmodelElementWrapperCollection sourceSmc, Key[] sourceSemanticId,
                ConceptDescription createDefault = null, Action<T> setDefault = null,
                Key.MatchMode matchMode = Key.MatchMode.Relaxed,
                string idShort = null, bool addSme = false) where T : SubmodelElement, new()
            {
                // get source
                var src = sourceSmc?.FindFirstAnySemanticIdAs<T>(sourceSemanticId, matchMode);

                // may be make an adaptive conversion
                if (src == null)
                {
                    var anySrc = sourceSmc?.FindFirstAnySemanticId(sourceSemanticId, matchMode: matchMode);
                    src = AdaptiveConvertTo<T>(anySrc?.submodelElement, createDefault,
                                idShort: idShort, addSme: false);
                }

                // proceed
                var aeSrc = SubmodelElementWrapper.GetAdequateEnum(src?.GetElementName());
                if (src == null || aeSrc == SubmodelElementWrapper.AdequateElementEnum.Unknown)
                {
                    // create a default?
                    if (createDefault == null)
                        return null;

                    // ok, default
                    var dflt = this.CreateSMEForCD<T>(createDefault, idShort: idShort, addSme: addSme);

                    // set default?
                    setDefault?.Invoke(dflt);

                    // return 
                    return dflt;
                }

                // ok, create new one
                var dst = SubmodelElementWrapper.CreateAdequateType(aeSrc, src) as T;
                if (dst == null)
                    return null;

                // make same things sure
                dst.idShort = src.idShort;
                dst.category = src.category;
                dst.semanticId = new SemanticId(destSemanticId);

                // instantanously add it?
                if (addSme)
                    this.Add(dst);

                // give back
                return dst;
            }

            public T CopyOneSMEbyCopy<T>(ConceptDescription destCD,
                SubmodelElementWrapperCollection sourceSmc, ConceptDescription sourceCD,
                bool createDefault = false, Action<T> setDefault = null,
                Key.MatchMode matchMode = Key.MatchMode.Relaxed,
                string idShort = null, bool addSme = false) where T : SubmodelElement, new()
            {
                return this.CopyOneSMEbyCopy<T>(destCD?.GetSingleKey(), sourceSmc, new[] { sourceCD?.GetSingleKey() },
                    createDefault ? destCD : null, setDefault, matchMode, idShort, addSme);
            }

            public T CopyOneSMEbyCopy<T>(ConceptDescription destCD,
                SubmodelElementWrapperCollection sourceSmc, Key[] sourceKeys,
                bool createDefault = false, Action<T> setDefault = null,
                Key.MatchMode matchMode = Key.MatchMode.Relaxed,
                string idShort = null, bool addSme = false) where T : SubmodelElement, new()
            {
                return this.CopyOneSMEbyCopy<T>(destCD?.GetSingleKey(), sourceSmc, sourceKeys,
                    createDefault ? destCD : null, setDefault, matchMode, idShort, addSme);
            }

            public void CopyManySMEbyCopy<T>(Key destSemanticId,
                SubmodelElementWrapperCollection sourceSmc, Key sourceSemanticId,
                ConceptDescription createDefault = null, Action<T> setDefault = null,
                Key.MatchMode matchMode = Key.MatchMode.Relaxed) where T : SubmodelElement, new()
            {
                // bool find possible sources
                bool foundSrc = false;
                if (sourceSmc == null)
                    return;
                foreach (var src in sourceSmc.FindAllSemanticIdAs<T>(sourceSemanticId, matchMode))
                {
                    // type of found src?
                    var aeSrc = SubmodelElementWrapper.GetAdequateEnum(src?.GetElementName());

                    // ok?
                    if (src == null || aeSrc == SubmodelElementWrapper.AdequateElementEnum.Unknown)
                        continue;
                    foundSrc = true;

                    // ok, create new one
                    var dst = SubmodelElementWrapper.CreateAdequateType(aeSrc, src) as T;
                    if (dst != null)
                    {
                        // make same things sure
                        dst.idShort = src.idShort;
                        dst.category = src.category;
                        dst.semanticId = new SemanticId(destSemanticId);

                        // instantanously add it?
                        this.Add(dst);
                    }
                }

                // default?
                if (createDefault != null && !foundSrc)
                {
                    // ok, default
                    var dflt = this.CreateSMEForCD<T>(createDefault, addSme: true);

                    // set default?
                    setDefault?.Invoke(dflt);
                }
            }

            public void CopyManySMEbyCopy<T>(ConceptDescription destCD,
                SubmodelElementWrapperCollection sourceSmc, ConceptDescription sourceCD,
                bool createDefault = false, Action<T> setDefault = null,
                Key.MatchMode matchMode = Key.MatchMode.Relaxed) where T : SubmodelElement, new()
            {
                CopyManySMEbyCopy(destCD.GetSingleKey(), sourceSmc, sourceCD.GetSingleKey(),
                    createDefault ? destCD : null, setDefault, matchMode);
            }
        }

        public interface IManageSubmodelElements
        {
            void Add(SubmodelElement sme);
            void Insert(int index, SubmodelElement sme);
            void Remove(SubmodelElement sme);
        }

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
                var caption = AdminShellUtilV20.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
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

        //
        // Derived from SubmodelElements
        //

        public class DataElement : SubmodelElement
        {
            public static string ValueType_STRING = "string";
            public static string ValueType_DATE = "date";
            public static string ValueType_BOOLEAN = "boolean";

            public static string[] ValueTypeItems = new string[] {
                    "anyType", "complexType", "anySimpleType", "anyAtomicType", "anyURI", "base64Binary",
                    "boolean", "date", "dateTime",
                    "dateTimeStamp", "decimal", "integer", "long", "int", "short", "byte", "nonNegativeInteger",
                    "positiveInteger",
                    "unsignedLong", "unsignedInt", "unsignedShort", "unsignedByte", "nonPositiveInteger",
                    "negativeInteger", "double", "duration",
                    "dayTimeDuration", "yearMonthDuration", "float", "hexBinary", "string", "langString", "time" };

            public static string[] ValueTypes_Number = new[] {
                    "decimal", "integer", "long", "int", "short", "byte", "nonNegativeInteger",
                    "positiveInteger",
                    "unsignedLong", "unsignedInt", "unsignedShort", "unsignedByte", "nonPositiveInteger",
                    "negativeInteger", "double", "float" };

            public DataElement() { }

            public DataElement(SubmodelElement src) : base(src) { }

            public DataElement(DataElement src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
            public DataElement(AasxCompatibilityModels.AdminShellV10.DataElement src)
                : base(src)
            { }
#endif

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("DataElement", "DE");
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

            public Property(SubmodelElement src)
                : base(src)
            {
                if (!(src is Property p))
                    return;
                this.valueType = p.valueType;
                this.value = p.value;
                if (p.valueId != null)
                    valueId = new Reference(p.valueId);
            }

#if !DoNotUseAasxCompatibilityModels
            public Property(AasxCompatibilityModels.AdminShellV10.Property src)
                : base(src)
            {
                if (src == null)
                    return;

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

            public Property Set(Qualifier q)
            {
                if (q != null)
                    this.AddQualifier(q);
                return this;
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Property", "Prop",
                    SubmodelElementWrapper.AdequateElementEnum.Property);
            }

            public override string ValueAsText(string defaultLang = null)
            {
                return "" + value;
            }

            public override void ValueFromText(string text, string defaultLang = null)
            {
                value = "" + text;
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

            public override double? ValueAsDouble()
            {
                // pointless
                if (this.value == null || this.value.Trim() == "" || this.valueType == null)
                    return null;

                // type?
                var vt = this.valueType.Trim().ToLower();
                if (!DataElement.ValueTypes_Number.Contains(vt))
                    return null;

                // try convert
                if (double.TryParse(this.value, NumberStyles.Any, CultureInfo.InvariantCulture, out double dbl))
                    return dbl;

                // no
                return null;
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

            public MultiLanguageProperty(SubmodelElement src)
                : base(src)
            {
                if (!(src is MultiLanguageProperty mlp))
                    return;

                this.value = new LangStringSet(mlp.value);
                if (mlp.valueId != null)
                    valueId = new Reference(mlp.valueId);
            }

#if !DoNotUseAasxCompatibilityModels
            // not available in V1.0
#endif

            public static MultiLanguageProperty CreateNew(
                string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new MultiLanguageProperty();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("MultiLanguageProperty", "MLP",
                    SubmodelElementWrapper.AdequateElementEnum.MultiLanguageProperty);
            }

            public MultiLanguageProperty Set(LangStringSet ls)
            {
                this.value = ls;
                return this;
            }

            public MultiLanguageProperty Set(ListOfLangStr ls)
            {
                this.value = new LangStringSet(ls);
                return this;
            }

            public MultiLanguageProperty Set(LangStr ls)
            {
                if (ls == null)
                    return this;
                if (this.value?.langString == null)
                    this.value = new LangStringSet();
                this.value.langString[ls.lang] = ls.str;
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

            public override void ValueFromText(string text, string defaultLang = null)
            {
                Set(defaultLang, text);
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

            public Range(SubmodelElement src)
                : base(src)
            {
                if (!(src is Range rng))
                    return;

                this.valueType = rng.valueType;
                this.min = rng.min;
                this.max = rng.max;
            }

#if !DoNotUseAasxCompatibilityModels
            // not available in V1.0
#endif

            public static Range CreateNew(string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new Range();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Range", "Range",
                    SubmodelElementWrapper.AdequateElementEnum.Range);
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

            public Blob(SubmodelElement src)
                : base(src)
            {
                if (!(src is Blob blb))
                    return;

                this.mimeType = blb.mimeType;
                this.value = blb.value;
            }

#if !DoNotUseAasxCompatibilityModels
            public Blob(AasxCompatibilityModels.AdminShellV10.Blob src)
                : base(src)
            {
                if (src == null)
                    return;

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

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Blob", "Blob",
                    SubmodelElementWrapper.AdequateElementEnum.Blob);
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

            public File(SubmodelElement src)
                : base(src)
            {
                if (!(src is File fil))
                    return;

                this.mimeType = fil.mimeType;
                this.value = fil.value;
            }

#if !DoNotUseAasxCompatibilityModels
            public File(AasxCompatibilityModels.AdminShellV10.File src)
                : base(src)
            {
                if (src == null)
                    return;

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

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("File", "File",
                    SubmodelElementWrapper.AdequateElementEnum.File);
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

            public ReferenceElement(SubmodelElement src)
                : base(src)
            {
                if (!(src is ReferenceElement re))
                    return;

                if (re.value != null)
                    this.value = new Reference(re.value);
            }

#if !DoNotUseAasxCompatibilityModels
            public ReferenceElement(AasxCompatibilityModels.AdminShellV10.ReferenceElement src)
                : base(src)
            {
                if (src == null)
                    return;

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

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("ReferenceElement", "Ref",
                    SubmodelElementWrapper.AdequateElementEnum.ReferenceElement);
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

            public RelationshipElement(SubmodelElement src)
                : base(src)
            {
                if (!(src is RelationshipElement rel))
                    return;

                if (rel.first != null)
                    this.first = new Reference(rel.first);
                if (rel.second != null)
                    this.second = new Reference(rel.second);
            }

#if !DoNotUseAasxCompatibilityModels
            public RelationshipElement(AasxCompatibilityModels.AdminShellV10.RelationshipElement src)
                : base(src)
            {
                if (src == null)
                    return;

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

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("RelationshipElement", "Rel",
                    SubmodelElementWrapper.AdequateElementEnum.RelationshipElement);
            }
        }

        public class AnnotatedRelationshipElement : RelationshipElement, IManageSubmodelElements, IEnumerateChildren
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

            public AnnotatedRelationshipElement(SubmodelElement src)
                : base(src)
            {
                if (!(src is AnnotatedRelationshipElement arel))
                    return;
                if (arel.first != null)
                    this.first = new Reference(arel.first);
                if (arel.second != null)
                    this.second = new Reference(arel.second);
                if (arel.annotations != null)
                    this.annotations = new DataElementWrapperCollection(arel.annotations);
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

            public EnumerationPlacmentBase GetChildrenPlacement(SubmodelElement child)
            {
                return null;
            }

            public object AddChild(SubmodelElementWrapper smw, EnumerationPlacmentBase placement = null)
            {
                if (smw == null || !(smw.submodelElement is DataElement))
                    return null;
                if (this.annotations == null)
                    this.annotations = new DataElementWrapperCollection();
                if (smw.submodelElement != null)
                    smw.submodelElement.parent = this;
                this.annotations.Add(smw);
                return smw;
            }

            // from IManageSubmodelElements
            public void Add(SubmodelElement sme)
            {
                if (annotations == null)
                    annotations = new DataElementWrapperCollection();
                var sew = new SubmodelElementWrapper();
                sme.parent = this; // track parent here!
                sew.submodelElement = sme;
                annotations.Add(sew);
            }

            public void Insert(int index, SubmodelElement sme)
            {
                if (annotations == null)
                    annotations = new DataElementWrapperCollection();
                var sew = new SubmodelElementWrapper();
                sme.parent = this; // track parent here!
                if (index < 0 || index >= annotations.Count)
                    return;
                annotations.Insert(index, sew);
            }

            public void Remove(SubmodelElement sme)
            {
                if (annotations != null)
                    annotations.Remove(sme);
            }

            // further 

            public new void Set(Reference first = null, Reference second = null)
            {
                this.first = first;
                this.second = second;
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("AnnotatedRelationshipElement", "RelA",
                    SubmodelElementWrapper.AdequateElementEnum.AnnotatedRelationshipElement);
            }


        }

        public class Capability : SubmodelElement
        {
            public Capability() { }

            public Capability(SubmodelElement src)
                : base(src)
            { }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Capability", "Cap",
                    SubmodelElementWrapper.AdequateElementEnum.Capability);
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
            [XmlIgnore]
            [JsonIgnore]
            [SkipForHash] // do NOT count children!
            private SubmodelElementWrapperCollection _value = null;

            [JsonIgnore]
            public SubmodelElementWrapperCollection value
            {
                get { return _value; }
                set { _value = value; _value.Parent = this; }
            }

            [XmlIgnore]
            [JsonProperty(PropertyName = "value")]
            public SubmodelElement[] JsonValue
            {
                get
                {
                    var res = new ListOfSubmodelElement();
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

            public EnumerationPlacmentBase GetChildrenPlacement(SubmodelElement child)
            {
                return null;
            }

            public object AddChild(SubmodelElementWrapper smw, EnumerationPlacmentBase placement = null)
            {
                if (smw == null)
                    return null;
                if (this.value == null)
                    this.value = new SubmodelElementWrapperCollection();
                if (smw.submodelElement != null)
                    smw.submodelElement.parent = this;
                this.value.Add(smw);
                return smw;
            }

            // constructors

            public SubmodelElementCollection() { }

            public SubmodelElementCollection(SubmodelElement src, bool shallowCopy = false)
                : base(src)
            {
                if (!(src is SubmodelElementCollection smc))
                    return;

                this.ordered = smc.ordered;
                this.allowDuplicates = smc.allowDuplicates;
                this.value = new SubmodelElementWrapperCollection();
                if (!shallowCopy)
                    foreach (var smw in smc.value)
                        value.Add(new SubmodelElementWrapper(smw.submodelElement));
            }

#if !DoNotUseAasxCompatibilityModels
            public SubmodelElementCollection(
                AasxCompatibilityModels.AdminShellV10.SubmodelElementCollection src, bool shallowCopy = false)
                : base(src)
            {
                if (src == null)
                    return;

                this.ordered = src.ordered;
                this.allowDuplicates = src.allowDuplicates;
                this.value = new SubmodelElementWrapperCollection();
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

            public void Insert(int index, SubmodelElement sme)
            {
                if (value == null)
                    value = new SubmodelElementWrapperCollection();
                var sew = new SubmodelElementWrapper();
                sme.parent = this; // track parent here!
                sew.submodelElement = sme;
                if (index < 0 || index >= value.Count)
                    return;
                value.Insert(index, sew);
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


            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("SubmodelElementCollection", "SMC",
                    SubmodelElementWrapper.AdequateElementEnum.SubmodelElementCollection);
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
                this.value?.RecurseOnReferables(state, null, (o, par, rf) =>
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
                this.value?.RecurseOnReferables(state, parents, lambda);
            }
        }

        public class OperationVariable : IAasElement
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
                this.value = new SubmodelElementWrapper(src?.value?.submodelElement, shallowCopy);
            }

#if !DoNotUseAasxCompatibilityModels
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

            public AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("OperationVariable", "OprVar");
            }

            public string GetElementName()
            {
                return this.GetSelfDescription()?.ElementName;
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
            public OperationVariable[] JsonInputVariable
            {
                get { return inputVariable?.ToArray(); }
                set { inputVariable = (value != null) ? new List<OperationVariable>(value) : null; }
            }

            [XmlIgnore]
            [JsonProperty(PropertyName = "outputVariable")]
            // MICHA 190504: enabled JSON operation variables!
            public OperationVariable[] JsonOutputVariable
            {
                get { return outputVariable?.ToArray(); }
                set { outputVariable = (value != null) ? new List<OperationVariable>(value) : null; }
            }

            [XmlIgnore]
            [JsonProperty(PropertyName = "inoutputVariable")]
            // MICHA 190504: enabled JSON operation variables!
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
                    if (dir == 0)
                        return inputVariable;
                    else
                    if (dir == 1)
                        return outputVariable;
                    else
                        return inoutputVariable;
                }
                set
                {
                    if (dir == 0)
                        inputVariable = value;
                    else
                    if (dir == 1)
                        outputVariable = value;
                    else
                        inoutputVariable = value;
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

            public class EnumerationPlacmentOperationVariable : EnumerationPlacmentBase
            {
                public OperationVariable.Direction Direction;
                public OperationVariable OperationVariable;
            }

            public EnumerationPlacmentBase GetChildrenPlacement(SubmodelElement child)
            {
                // trivial
                if (child == null)
                    return null;

                // search
                OperationVariable.Direction? dir = null;
                OperationVariable opvar = null;
                if (this.inputVariable != null)
                    foreach (var ov in this.inputVariable)
                        if (ov?.value?.submodelElement == child)
                        {
                            dir = OperationVariable.Direction.In;
                            opvar = ov;
                        }

                if (this.outputVariable != null)
                    foreach (var ov in this.outputVariable)
                        if (ov?.value?.submodelElement == child)
                        {
                            dir = OperationVariable.Direction.Out;
                            opvar = ov;
                        }

                if (this.inoutputVariable != null)
                    foreach (var ov in this.inoutputVariable)
                        if (ov?.value?.submodelElement == child)
                        {
                            dir = OperationVariable.Direction.InOut;
                            opvar = ov;
                        }

                // found
                if (!dir.HasValue)
                    return null;
                return new EnumerationPlacmentOperationVariable()
                {
                    Direction = dir.Value,
                    OperationVariable = opvar
                };
            }

            public object AddChild(SubmodelElementWrapper smw, EnumerationPlacmentBase placement = null)
            {
                // not enough information to select list of children?
                var pl = placement as EnumerationPlacmentOperationVariable;
                if (smw == null || pl == null)
                    return null;

                // ok, use information
                var ov = new OperationVariable();
                ov.value = smw;

                if (smw.submodelElement != null)
                    smw.submodelElement.parent = this;

                if (pl.Direction == OperationVariable.Direction.In)
                {
                    if (inputVariable == null)
                        inputVariable = new List<OperationVariable>();
                    inputVariable.Add(ov);
                }

                if (pl.Direction == OperationVariable.Direction.Out)
                {
                    if (outputVariable == null)
                        outputVariable = new List<OperationVariable>();
                    outputVariable.Add(ov);
                }

                if (pl.Direction == OperationVariable.Direction.InOut)
                {
                    if (inoutputVariable == null)
                        inoutputVariable = new List<OperationVariable>();
                    inoutputVariable.Add(ov);
                }

                return ov;
            }

            // constructors

            public Operation() { }

            public Operation(SubmodelElement src)
                : base(src)
            {
                if (!(src is Operation op))
                    return;

                for (int i = 0; i < 2; i++)
                    if (op[i] != null)
                    {
                        if (this[i] == null)
                            this[i] = new List<OperationVariable>();
                        foreach (var ov in op[i])
                            this[i].Add(new OperationVariable(ov));
                    }
            }

#if !DoNotUseAasxCompatibilityModels
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

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Operation", "Opr",
                    SubmodelElementWrapper.AdequateElementEnum.Operation);
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
            [XmlIgnore]
            [JsonIgnore]
            [SkipForHash] // do NOT count children!
            private SubmodelElementWrapperCollection _statements = null;

            [JsonIgnore]
            public SubmodelElementWrapperCollection statements
            {
                get { return _statements; }
                set { _statements = value; _statements.Parent = this; }
            }

            [XmlIgnore]
            [JsonProperty(PropertyName = "statements")]
            public SubmodelElement[] JsonStatements
            {
                get
                {
                    var res = new ListOfSubmodelElement();
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

            public EnumerationPlacmentBase GetChildrenPlacement(SubmodelElement child)
            {
                return null;
            }

            public object AddChild(SubmodelElementWrapper smw, EnumerationPlacmentBase placement = null)
            {
                if (smw == null)
                    return null;
                if (this.statements == null)
                    this.statements = new SubmodelElementWrapperCollection();
                if (smw.submodelElement != null)
                    smw.submodelElement.parent = this;
                this.statements.Add(smw);
                return smw;
            }

            // constructors

            public Entity() { }

            public Entity(SubmodelElement src)
                : base(src)
            {
                if (!(src is Entity ent))
                    return;

                if (ent.statements != null)
                {
                    this.statements = new SubmodelElementWrapperCollection();
                    foreach (var smw in ent.statements)
                        this.statements.Add(new SubmodelElementWrapper(smw.submodelElement));
                }
                this.entityType = ent.entityType;
                if (ent.assetRef != null)
                    this.assetRef = new AssetRef(ent.assetRef);
            }

            public Entity(EntityTypeEnum entityType, string idShort = null, AssetRef assetRef = null,
                string category = null, Key semanticIdKey = null)
            {
                CreateNewLogic(idShort, null, semanticIdKey);

                this.entityType = EntityTypeNames[(int)entityType];
                this.assetRef = assetRef;
            }

#if !DoNotUseAasxCompatibilityModels
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

            public void Insert(int index, SubmodelElement sme)
            {
                if (statements == null)
                    statements = new SubmodelElementWrapperCollection();
                var sew = new SubmodelElementWrapper();
                sme.parent = this; // track parent here!
                sew.submodelElement = sme;
                if (index < 0 || index >= statements.Count)
                    return;
                statements.Insert(index, sew);
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

            public T CreateSMEForCD<T>(ConceptDescription cd, string category = null, string idShort = null,
                string idxTemplate = null, int maxNum = 999, bool addSme = false) where T : SubmodelElement, new()
            {
                if (this.statements == null)
                    this.statements = new SubmodelElementWrapperCollection();
                return this.statements.CreateSMEForCD<T>(cd, category, idShort, idxTemplate, maxNum, addSme);
            }

            public T CreateSMEForIdShort<T>(string idShort, string category = null,
                string idxTemplate = null, int maxNum = 999, bool addSme = false) where T : SubmodelElement, new()
            {
                if (this.statements == null)
                    this.statements = new SubmodelElementWrapperCollection();
                return this.statements.CreateSMEForIdShort<T>(idShort, category, idxTemplate, maxNum, addSme);
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

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("Entity", "Ent",
                    SubmodelElementWrapper.AdequateElementEnum.Entity);
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

            public BasicEvent(SubmodelElement src)
                : base(src)
            {
                if (!(src is BasicEvent be))
                    return;

                if (be.observed != null)
                    this.observed = new Reference(be.observed);
            }

#if !DoNotUseAasxCompatibilityModels
            // not available in V1.0
#endif

            public static BasicEvent CreateNew(string idShort = null, string category = null, Key semanticIdKey = null)
            {
                var x = new BasicEvent();
                x.CreateNewLogic(idShort, category, semanticIdKey);
                return (x);
            }

            public override AasElementSelfDescription GetSelfDescription()
            {
                return new AasElementSelfDescription("BasicEvent", "Evt",
                    SubmodelElementWrapper.AdequateElementEnum.BasicEvent);
            }
        }

        //
        // Handling of packages
        //
    }

    #endregion
}

