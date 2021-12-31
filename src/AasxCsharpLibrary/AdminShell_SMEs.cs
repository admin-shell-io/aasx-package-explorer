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

            public SubmodelElement(AasxCompatibilityModels.AdminShellV20.SubmodelElement src)
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
                            "",
                            cid.id.value));
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
                var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", idShort, "<no idShort!>");
                var info = "";
                // TODO (MIHO, 2021-07-08): obvious error .. info should receive semanticId .. but would change 
                // display presentation .. therefore to be checked again
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

            public SubmodelElementWrapper(AasxCompatibilityModels.AdminShellV20.SubmodelElement src, bool shallowCopy = false)
            {
                /* TODO (MIHO, 2021-08-12): consider using:
                   Activator.CreateInstance(pl.GetType(), new object[] { pl }) */

                if (src is AasxCompatibilityModels.AdminShellV20.SubmodelElementCollection)
                    this.submodelElement = new SubmodelElementCollection(
                        src as AasxCompatibilityModels.AdminShellV20.SubmodelElementCollection, shallowCopy: shallowCopy);
                if (src is AasxCompatibilityModels.AdminShellV20.Property)
                    this.submodelElement = new Property(src as AasxCompatibilityModels.AdminShellV20.Property);
                if (src is AasxCompatibilityModels.AdminShellV20.MultiLanguageProperty)
                    this.submodelElement = new MultiLanguageProperty(src as AasxCompatibilityModels.AdminShellV20.MultiLanguageProperty);
                if (src is AasxCompatibilityModels.AdminShellV20.Range)
                    this.submodelElement = new Range(src as AasxCompatibilityModels.AdminShellV20.Range);
                if (src is AasxCompatibilityModels.AdminShellV20.File)
                    this.submodelElement = new File(src as AasxCompatibilityModels.AdminShellV20.File);
                if (src is AasxCompatibilityModels.AdminShellV20.Blob)
                    this.submodelElement = new Blob(src as AasxCompatibilityModels.AdminShellV20.Blob);
                if (src is AasxCompatibilityModels.AdminShellV20.ReferenceElement)
                    this.submodelElement = new ReferenceElement(src as AasxCompatibilityModels.AdminShellV20.ReferenceElement);
                if (src is AasxCompatibilityModels.AdminShellV20.RelationshipElement)
                    this.submodelElement = new RelationshipElement(src as AasxCompatibilityModels.AdminShellV20.RelationshipElement);
                if (src is AasxCompatibilityModels.AdminShellV20.AnnotatedRelationshipElement)
                    this.submodelElement = new AnnotatedRelationshipElement(src as AasxCompatibilityModels.AdminShellV20.AnnotatedRelationshipElement);
                if (src is AasxCompatibilityModels.AdminShellV20.Capability)
                    this.submodelElement = new Capability(src as AasxCompatibilityModels.AdminShellV20.Capability);
                if (src is AasxCompatibilityModels.AdminShellV20.Operation)
                    this.submodelElement = new Operation(src as AasxCompatibilityModels.AdminShellV20.Operation);
                if (src is AasxCompatibilityModels.AdminShellV20.BasicEvent)
                    this.submodelElement = new BasicEvent(src as AasxCompatibilityModels.AdminShellV20.BasicEvent);
                if (src is AasxCompatibilityModels.AdminShellV20.Entity)
                    this.submodelElement = new Entity(src as AasxCompatibilityModels.AdminShellV20.Entity);
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
                if (!rf[keyIndex].IsInSubmodelElements())
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

#if !DoNotUseAasxCompatibilityModels
            public DataElementWrapperCollection(AasxCompatibilityModels.AdminShellV20.DataElementWrapperCollection src)
                : base()
            {
                foreach (var wo in src)
                    this.Add(new SubmodelElementWrapper(wo?.submodelElement));
            }
#endif

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
                string idxTemplate = null, int maxNum = 999, bool addSme = false) where T : SubmodelElement, new()
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
                    "unsignedLong", "unsignedShort", "unsignedByte", "nonPositiveInteger", "negativeInteger",
                    "double", "duration",
                    "dayTimeDuration", "yearMonthDuration", "float", "hexBinary", "string", "langString", "time" };

            public static string[] ValueTypes_Number = new[] {
                    "decimal", "integer", "long", "int", "short", "byte", "nonNegativeInteger",
                    "positiveInteger",
                    "unsignedLong", "unsignedShort", "unsignedByte", "nonPositiveInteger", "negativeInteger",
                    "double", "float" };

            public DataElement() { }

            public DataElement(SubmodelElement src) : base(src) { }

            public DataElement(DataElement src) : base(src) { }

#if !DoNotUseAasxCompatibilityModels
            public DataElement(AasxCompatibilityModels.AdminShellV10.DataElement src)
                : base(src)
            { }

            public DataElement(AasxCompatibilityModels.AdminShellV20.DataElement src) : base(src) { }
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

            public Property(AasxCompatibilityModels.AdminShellV20.Property src)
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
            public MultiLanguageProperty(AasxCompatibilityModels.AdminShellV20.MultiLanguageProperty src)
                : base(src)
            {
                this.value = new LangStringSet(src.value);
                if (src.valueId != null)
                    valueId = new Reference(src.valueId);
            }
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

            public Range(AasxCompatibilityModels.AdminShellV20.Range src)
                : base(src)
            {
                this.valueType = src.valueType;
                this.min = src.min;
                this.max = src.max;
            }
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

            public Blob(AasxCompatibilityModels.AdminShellV20.Blob src)
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

            public File(AasxCompatibilityModels.AdminShellV20.File src)
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

            public ReferenceElement(AasxCompatibilityModels.AdminShellV20.ReferenceElement src)
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

            public RelationshipElement(AasxCompatibilityModels.AdminShellV20.RelationshipElement src)
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

#if !DoNotUseAasxCompatibilityModels
            public AnnotatedRelationshipElement(AasxCompatibilityModels.AdminShellV20.AnnotatedRelationshipElement src)
                : base(src)
            {
                if (src.first != null)
                    this.first = new Reference(src.first);
                if (src.second != null)
                    this.second = new Reference(src.second);
                if (src.annotations != null)
                    this.annotations = new DataElementWrapperCollection(src.annotations);
            }
#endif

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

#if !DoNotUseAasxCompatibilityModels
            public Capability(AasxCompatibilityModels.AdminShellV20.Capability src)
                : base(src)
            { }
#endif

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
                if (!shallowCopy)
                    foreach (var smw in src.value)
                        value.Add(new SubmodelElementWrapper(smw.submodelElement));
            }

            public SubmodelElementCollection(AasxCompatibilityModels.AdminShellV20.SubmodelElement src, bool shallowCopy = false)
                : base(src)
            {
                if (!(src is AasxCompatibilityModels.AdminShellV20.SubmodelElementCollection smc))
                    return;

                this.ordered = smc.ordered;
                this.allowDuplicates = smc.allowDuplicates;
                this.value = new SubmodelElementWrapperCollection();
                if (!shallowCopy)
                    foreach (var smw in smc.value)
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

            public OperationVariable(
                AasxCompatibilityModels.AdminShellV20.OperationVariable src, bool shallowCopy = false)
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

            public Operation(AasxCompatibilityModels.AdminShellV20.Operation src)
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

            public Entity(AasxCompatibilityModels.AdminShellV20.Entity src)
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

            public BasicEvent(AasxCompatibilityModels.AdminShellV20.BasicEvent src)
                : base(src)
            {
                if (src.observed != null)
                    this.observed = new Reference(src.observed);
            }
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

    }
}
