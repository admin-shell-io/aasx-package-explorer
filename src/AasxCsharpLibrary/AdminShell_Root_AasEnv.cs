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
        // AAS ENV
        //

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

        [XmlRoot(ElementName = "aasenv", Namespace = "http://www.admin-shell.io/aas/3/0")]
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
                "http://www.admin-shell.io/aas/3/0 AAS.xsd http://www.admin-shell.io/IEC61360/3/0 IEC61360.xsd";

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
                nss.Add("aas", "http://www.admin-shell.io/aas/3/0");
                nss.Add("IEC61360", "http://www.admin-shell.io/IEC61360/3/0");
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
                    typeof(AdminShell.AdministrationShellEnv), "http://www.admin-shell.io/aas/3/0");
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

    }
}
