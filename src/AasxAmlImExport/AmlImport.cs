/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasCore.Aas3_0_RC02;
using AasxIntegrationBase;
using AdminShellNS;
using Extensions;
using Aml.Engine.CAEX;
using Newtonsoft.Json.Linq;

namespace AasxAmlImExport
{
    public static class AmlImport
    {
        public static string PrintSemantic(CAEXSequence<RefSemanticType> sem)
        {
            var res = "";
            if (sem != null)
                foreach (var rf in sem)
                    if (rf.CorrespondingAttributePath != null && rf.CorrespondingAttributePath.Trim() != "")
                    {
                        if (res != "")
                            res += ", ";
                        res += rf.CorrespondingAttributePath.Trim();
                    }
            return res;
        }



        public class AmlParser
        {
            public AdminShellPackageEnv package = null;

            private class TargetIdAction
            {
                public int TargetId;
                public Action<InternalLinkType, int> Action;

                public TargetIdAction(int targetId, Action<InternalLinkType, int> action)
                {
                    TargetId = targetId;
                    Action = action;
                }
            }

            /// <summary>
            /// During parsing of internal elements, AAS entities can register themselves to be source or target
            /// of an AML internal link.
            /// Key is the value of il.RefPartnerSide(A|B).
            /// Lambda will be called, checking if link is meaningful needs to be done inside.
            /// </summary>
            private MultiValueDictionary<string, TargetIdAction> registerForInternalLinks =
                    new MultiValueDictionary<string, TargetIdAction>();

            private class IeViewAmlTarget
            {
                public InternalElementType Ie;
                //public View View;
                public CAEXObject AmlTarget;

                public IeViewAmlTarget(InternalElementType ie, CAEXObject amlTarget)
                {
                    Ie = ie;
                    AmlTarget = amlTarget;
                }
            }

            /// <summary>
            /// Remember contained element refs for Views, to be assiciated later with AAS entities
            /// </summary>
            private List<IeViewAmlTarget> latePopoulationViews = new List<IeViewAmlTarget>();

            /// <summary>
            /// Hold available all IDs of input AML
            /// </summary>
            private Dictionary<string, InternalElementType> idDict = new Dictionary<string, InternalElementType>();

            private AasAmlMatcher matcher = new AasAmlMatcher();

            public AmlParser() { }

            public AmlParser(AdminShellPackageEnv package)
            {
                this.package = package;
            }

            public void Debug(int indentation, string msg, params object[] args)
            {
                var st = String.Format(msg, args);
                Console.WriteLine("{0}{1}", new String(' ', 2 * indentation), st);
            }

            public Reference ParseAmlReference(string refstr)
            {
                // trivial
                if (refstr == null)
                    return null;

                // a reference could carry multiple Keys, delimited by ","
                var refstrs = refstr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (refstr.Length < 1)
                    return null;

                // build a Reference
                //var res = new Reference();
                var keyList = new List<Key>();

                // over all entries
                foreach (var rs in refstrs)
                {
                    var m = Regex.Match(rs.Trim(), @"^\(([^)]+)\)(.*)$");
                    if (!m.Success)
                        // immediate fail or next try?
                        return null;

                    // get string data
                    var ke = m.Groups[1].ToString();
                    var id = m.Groups[2].ToString();

                    // verify: ke has to be in allowed range
                    var keyType = Stringification.KeyTypesFromString(ke);
                    if (keyType.HasValue)
                    {
                        // create key and make on refece
                        var k = new Key(keyType.Value, id);
                        keyList.Add(k);
                    }
                    else
                        return null;
                }
                var res = new Reference(ReferenceTypes.ModelReference, keyList);
                return res;
            }

            public bool CheckForRole(CAEXSequence<SupportedRoleClassType> seq, string refRoleClassPath)
            {
                if (seq == null)
                    return false;
                foreach (var src in seq)
                    if (src.RefRoleClassPath != null && src.RefRoleClassPath.Trim() != "")
                        if (src.RefRoleClassPath.Trim().ToLower() == refRoleClassPath.Trim().ToLower())
                            return true;
                return false;
            }

            public bool CheckForRole(CAEXSequence<RoleRequirementsType> seq, string refBaseRoleClassPath)
            {
                if (seq == null)
                    return false;
                foreach (var src in seq)
                    if (src.RefBaseRoleClassPath != null && src.RefBaseRoleClassPath.Trim() != "")
                        if (src.RefBaseRoleClassPath.Trim().ToLower() == refBaseRoleClassPath.Trim().ToLower())
                            return true;
                return false;
            }

            public bool CheckForRoleClassOrRoleRequirements(SystemUnitClassType ie, string classPath)
            {
                /*
                 HACK (MIHO, 2020-08-01): The check for role class or requirements is still questionable
                 but seems to be correct (see below)

                 Question MIHO: I dont understand the determinism behind that!
                 WIEGAND: me, neither ;-)
                 Wiegand:  ich hab mir von Prof.Drath nochmal erklären lassen, wie SupportedRoleClass und
                 RoleRequirement verwendet werden:
                 In CAEX2.15(aktuelle AML Version und unsere AAS Mapping Version):
                   1.Eine SystemUnitClass hat eine oder mehrere SupportedRoleClasses, die ihre „mögliche Rolle
                     beschreiben(Drucker / Fax / kopierer)
                   2.Wird die SystemUnitClass als InternalElement instanziiert entscheidet man sich für eine
                     Hauptrolle, die dann zum RoleRequirement wird und evtl. Nebenklassen die dann
                     SupportedRoleClasses sind(ist ein Workaround weil CAEX2.15 in der Norm nur
                     ein RoleReuqirement erlaubt)
                 InCAEX3.0(nächste AMl Version):
                   1.Wie bei CAEX2.15
                   2.Wird die SystemUnitClass als Internal Elementinstanziiert werden die verwendeten Rollen
                     jeweils als RoleRequirement zugewiesen (in CAEX3 sind mehrere RoleReuqirements nun erlaubt)
                */

                // Remark: SystemUnitClassType is suitable for SysUnitClasses and InternalElements

                if (ie is InternalElementType iet)
                    if (CheckForRole(iet.RoleRequirements, classPath))
                        return true;

                return
                    CheckForRole(ie.SupportedRoleClass, classPath);
            }

            public bool CheckAttributeFoRefSemantic(AttributeType a, string correspondingAttributePath)
            {
                if (a.RefSemantic != null)
                    foreach (var rf in a.RefSemantic)
                        if (rf.CorrespondingAttributePath != null &&
                        rf.CorrespondingAttributePath.Trim() != "" &&
                        rf.CorrespondingAttributePath.Trim().ToLower() == correspondingAttributePath.Trim().ToLower())
                            // found!
                            return true;
                return false;
            }

            public AttributeType FindAttributeByRefSemantic(AttributeSequence aseq, string correspondingAttributePath)
            {
                foreach (var a in aseq)
                {
                    // check attribute itself
                    if (CheckAttributeFoRefSemantic(a, correspondingAttributePath))
                        // found!
                        return a;

                    // could be childs
                    var x = FindAttributeByRefSemantic(a.Attribute, correspondingAttributePath);
                    if (x != null)
                        return x;
                }
                return null;
            }

            public string FindAttributeValueByRefSemantic(AttributeSequence aseq, string correspondingAttributePath)
            {
                var a = FindAttributeByRefSemantic(aseq, correspondingAttributePath);
                return a?.Value;
            }

            public ExternalInterfaceType FindExternalInterfaceByNameAndBaseClassPath(
                ExternalInterfaceSequence eiseq, string name, string classpath)
            {
                ExternalInterfaceType res = null;
                if (eiseq != null)
                    foreach (var ei in eiseq)
                        if (
                            (name == null || (ei.Name != null &&
                                ei.Name.Trim().ToLower() == name.Trim().ToLower())) &&
                            (classpath == null ||
                                (ei.RefBaseClassPath != null &&
                                 ei.RefBaseClassPath.Trim().ToLower() == classpath.Trim().ToLower())))
                            res = ei;
                return res;
            }

            public List<LangString> TryParseListOfLangStrFromAttributes(
                AttributeSequence aseq, string correspondingAttributePath)
            {
                if (aseq == null || correspondingAttributePath == null)
                    return null;
                var aroot = FindAttributeByRefSemantic(aseq, correspondingAttributePath);
                if (aroot == null)
                    return null;

                // primary stuff
                var res = new List<LangString> { new LangString("Default", aroot.Value) };

                // assume the language-specific attributes being directly sub-ordinated
                if (aroot.Attribute != null)
                    foreach (var a in aroot.Attribute)
                    {
                        var m = Regex.Match(a.Name.Trim(), @"([^=]+)\w*=(.*)$");
                        if (m.Success && m.Groups[1].ToString().ToLower() == "aml-lang")
                            res.Add(new LangString(m.Groups[2].ToString(), a.Value));
                    }

                // end
                return res;
            }

            public List<LangString> TryParseDescriptionFromAttributes(
                AttributeSequence aseq, string correspondingAttributePath)
            {
                var ls = TryParseListOfLangStrFromAttributes(aseq, correspondingAttributePath);
                if (ls == null)
                    return null;

                var res = new List<LangString>(ls);
                return res;
            }

            public List<Qualifier> TryParseQualifiersFromAttributes(AttributeSequence aseq)
            {
                if (aseq == null)
                    return null;

                List<Qualifier> res = null;
                foreach (var a in aseq)
                    if (CheckAttributeFoRefSemantic(a, AmlConst.Attributes.Qualifer))
                    {
                        // gather
                        var qt = FindAttributeValueByRefSemantic(a.Attribute, AmlConst.Attributes.Qualifer_Type);
                        var qv = FindAttributeValueByRefSemantic(a.Attribute, AmlConst.Attributes.Qualifer_Value);
                        var sid = FindAttributeValueByRefSemantic(a.Attribute, AmlConst.Attributes.SemanticId);
                        var qvid = FindAttributeValueByRefSemantic(a.Attribute, AmlConst.Attributes.Qualifer_ValueId);

                        // check
                        if ((qt != null || sid != null) && (qv != null || qvid != null))
                        {

                            // create
                            var q = new Qualifier(qt, DataTypeDefXsd.String)
                            {
                                Value = qv,
                                SemanticId = new Reference(ReferenceTypes.ModelReference, ParseAmlReference(sid)?.Keys),
                                ValueId = ParseAmlReference(qvid)
                            };

                            // add
                            if (res == null)
                                res = new List<Qualifier>();
                            res.Add(q);
                        }
                    }

                return res;
            }

            public List<Reference> TryParseDataSpecificationFromAttributes(AttributeSequence aseq)
            {
                if (aseq == null)
                    return null;

                List<Reference> res = null;
                foreach (var a in aseq)
                    if (CheckAttributeFoRefSemantic(a, AmlConst.Attributes.DataSpecificationRef))
                    {
                        var r = ParseAmlReference(a.Value);
                        if (r != null)
                        {
                            if (res == null)
                                res = new List<Reference>(); //default initilization
                            //TODO: jtikekar Temporarily removed, cannot be added, as it may reflect in the other places, like AssetAdministrationShell does not contain EmbeddedDS
                            //res.Add(new EmbeddedDataSpecification(r));
                            res.Add(r);
                        }
                    }

                return res;
            }

            public List<T> TryParseListItemsFromAttributes<T>(
                AttributeSequence aseq, string correspondingAttributePath, Func<string, T> lambda)
            {
                var list = new List<T>();
                foreach (var a in aseq)
                    if (CheckAttributeFoRefSemantic(a, correspondingAttributePath))
                    {
                        var item = lambda(a.Value);
                        list.Add(item);
                    }
                return list;
            }

            private void AddToSubmodelOrSmec(IReferable parent, ISubmodelElement se)
            {
                //if (parent is IManageSubmodelElements imse)
                //    imse.Add(se);
                if (parent is Submodel submodel)
                {
                    submodel.Add(se);
                }
                else if (parent is SubmodelElementCollection collection)
                {
                    collection.Add(se);
                }
                else
                {
                    Console.WriteLine("Unsupported parent");
                }
            }

            private AssetAdministrationShell TryParseAasFromIe(SystemUnitClassType ie)
            {
                // begin new (temporary) object
                var aas = new AssetAdministrationShell("", new AssetInformation(AssetKind.Instance));

                // gather important attributes
                var idShort = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_IdShort);
                var id = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Identification_id);
                var version = FindAttributeValueByRefSemantic(
                    ie.Attribute, AmlConst.Attributes.Administration_Version);
                var revision = FindAttributeValueByRefSemantic(
                    ie.Attribute, AmlConst.Attributes.Administration_Revision);
                var cat = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_Category);
                var desc = TryParseDescriptionFromAttributes(ie.Attribute, AmlConst.Attributes.Referable_Description);
                var ds = TryParseDataSpecificationFromAttributes(ie.Attribute);
                var derivedfrom = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.AAS_DerivedFrom);

                // we need to have some important information
                if (id != null)
                {
                    // set data
                    aas.IdShort = ie.Name;
                    if (idShort != null)
                        aas.IdShort = idShort;
                    aas.Id = id;
                    if (version != null && revision != null)
                        aas.Administration = new AdministrativeInformation(version: version, revision: revision);
                    aas.Category = cat;
                    if (desc != null)
                        aas.Description = desc;
                    if (ds != null)
                        aas.EmbeddedDataSpecifications = ds.Select((dsi) => new EmbeddedDataSpecification(dsi, null)).ToList();
                    if (derivedfrom != null)
                    {
                        var derivedFromRef = ParseAmlReference(derivedfrom);
                        aas.DerivedFrom = new Reference(derivedFromRef.Type, derivedFromRef.Keys);
                    }

                    // result
                    return aas;
                }
                else
                    // uups!
                    return null;
            }

            private AssetInformation TryParseAssetFromIe(InternalElementType ie)
            {
                // begin new (temporary) object
                var asset = new AssetInformation(AssetKind.Instance);

                // gather important attributes
                var idShort = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_IdShort);
                var id = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Identification_id);
                var version = FindAttributeValueByRefSemantic(
                    ie.Attribute, AmlConst.Attributes.Administration_Version);
                var revision = FindAttributeValueByRefSemantic(
                    ie.Attribute, AmlConst.Attributes.Administration_Revision);
                var cat = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_Category);
                var desc = TryParseDescriptionFromAttributes(ie.Attribute, AmlConst.Attributes.Referable_Description);
                var kind = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Asset_Kind);
                var ds = TryParseDataSpecificationFromAttributes(ie.Attribute);

                // we need to have some important information
                if (id != null)
                {
                    // set data
                    //TODO: jtikekar Uncomment and Support
                    //asset.identification = new Identification(idType, id);

                    //NO administrativeInformation, catagory or description in V3 AssetInformation
                    //if (version != null && revision != null)
                    //    asset.administration = new Administration(version, revision);
                    //asset.Category = cat;
                    //if (desc != null)
                    //    asset.Description = desc;

                    asset.GlobalAssetId = ExtendReference.CreateFromKey(KeyTypes.GlobalReference, id);

                    if (kind != null)
                        asset.AssetKind = (AssetKind)Stringification.AssetKindFromString(kind);
                    //No DataSpecification asset
                    //if (ds != null)
                    //    asset.hasDataSpecification = ds;

                    // result
                    return asset;
                }
                else
                    // uups!
                    return null;
            }

            private void FillDictWithInternalElementsIds(
                Dictionary<string, InternalElementType> dict, InternalElementSequence ieseq)
            {
                if (dict == null || ieseq == null)
                    return;
                foreach (var ie in ieseq)
                {
                    if (ie.ID != null)
                        dict.Add(ie.ID, ie);
                    FillDictWithInternalElementsIds(dict, ie.InternalElement);
                }
            }

            private InternalElementType FindInternalElementByID(string ID)
            {
                if (ID == null)
                    return null;
                if (!idDict.ContainsKey(ID))
                    return null;
                return idDict[ID];
            }

            //private View TryParseViewFromIe(InstanceHierarchyType insthier, InternalElementType ie)
            //{
            //    // access
            //    if (insthier == null || ie == null)
            //        return null;

            //    //
            //    // make up local data management
            //    //

            //    // begin new (temporary) objects
            //    var view = new View();

            //    // gather important attributes
            //    var idShort = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_IdShort);
            //    var cat = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_Category);
            //    var desc = TryParseDescriptionFromAttributes(ie.Attribute, AmlConst.Attributes.Referable_Description);
            //    var ds = TryParseDataSpecificationFromAttributes(ie.Attribute);
            //    var semid = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.SemanticId);

            //    // we need to have some important information
            //    if (ie.Name != null)
            //    {
            //        // set data
            //        view.IdShort = ie.Name;
            //        if (idShort != null)
            //            view.IdShort = idShort;
            //        view.Category = cat;
            //        if (desc != null)
            //            view.Description = desc;
            //        if (semid != null)
            //            view.SemanticId = SemanticId.CreateFromKeys(ParseAmlReference(semid)?.Keys);
            //        if (ds != null)
            //            view.hasDataSpecification = ds;

            //        // check for direct descendents to be "Mirror-Elements"
            //        if (ie.InternalElement != null)
            //            foreach (var mie in ie.InternalElement)
            //                if (mie.RefBaseSystemUnitPath.HasContent())
            //                {
            //                    // candidate .. try identify target
            //                    var el = FindInternalElementByID(mie.RefBaseSystemUnitPath);
            //                    if (el != null)
            //                    {
            //                        // for the View's contain element references, all targets of the references
            //                        // shall exists.
            //                        // This is not already the case, therefore store the AML IE / View Information
            //                        // for later parsing
            //                        this.latePopoulationViews.Add(new IeViewAmlTarget(ie, view, el));
            //                    }
            //                }

            //        // result
            //        return view;
            //    }
            //    else
            //        // uups!
            //        return null;
            //}

            private Submodel TryParseSubmodelFromIe(SystemUnitClassType ie)
            {
                // begin new (temporary) object
                var sm = new Submodel("");

                // gather important attributes
                var idShort = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_IdShort);
                var id = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Identification_id);
                var version = FindAttributeValueByRefSemantic(
                    ie.Attribute, AmlConst.Attributes.Administration_Version);
                var revision = FindAttributeValueByRefSemantic(
                    ie.Attribute, AmlConst.Attributes.Administration_Revision);
                var cat = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_Category);
                var desc = TryParseDescriptionFromAttributes(ie.Attribute, AmlConst.Attributes.Referable_Description);
                var semid = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.SemanticId);
                var kind = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.HasKind_Kind);
                var qualifiers = TryParseQualifiersFromAttributes(ie.Attribute);
                var ds = TryParseDataSpecificationFromAttributes(ie.Attribute);

                // we need to have some important information
                if (id != null)
                {
                    // set data
                    sm.IdShort = ie.Name;
                    if (idShort != null)
                        sm.IdShort = idShort;
                    sm.Id = id;
                    if (version != null && revision != null)
                        sm.Administration = new AdministrativeInformation(version: version, revision: revision);
                    sm.Category = cat;
                    if (desc != null)
                        sm.Description = desc;
                    if (semid != null)
                        sm.SemanticId = new Reference(ReferenceTypes.ModelReference, ParseAmlReference(semid)?.Keys);
                    if (kind != null)
                        sm.Kind = Stringification.ModelingKindFromString(kind);
                    if (qualifiers != null)
                        sm.Qualifiers = qualifiers;
                    if (ds != null)
                        sm.EmbeddedDataSpecifications = ds.Select((dsi) => new EmbeddedDataSpecification(dsi, null)).ToList();

                    // result
                    return sm;
                }
                else
                    // uups!
                    return null;
            }

            private SubmodelElementCollection TryParseSubmodelElementCollection(SystemUnitClassType ie)
            {
                // begin new (temporary) object
                var smec = new SubmodelElementCollection();

                // gather important attributes
                var idShort = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_IdShort);
                var semid = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.SemanticId);
                var kind = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.HasKind_Kind);
                var cat = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_Category);
                var desc = TryParseDescriptionFromAttributes(ie.Attribute, AmlConst.Attributes.Referable_Description);
                var qualifiers = TryParseQualifiersFromAttributes(ie.Attribute);
                var ds = TryParseDataSpecificationFromAttributes(ie.Attribute);

                // we need to have some important information (only IReferable name, shoud be always there..)
                if (ie.Name != null)
                {
                    // set data
                    smec.IdShort = ie.Name;
                    if (idShort != null)
                        smec.IdShort = idShort;
                    if (semid != null)
                        smec.SemanticId = new Reference(ReferenceTypes.ModelReference, ParseAmlReference(semid)?.Keys);
                    if (kind != null)
                        smec.Kind = Stringification.ModelingKindFromString(kind);
                    if (desc != null)
                        smec.Description = desc;
                    if (cat != null)
                        smec.Category = cat;
                    if (qualifiers != null)
                        smec.Qualifiers = qualifiers;
                    if (ds != null)
                        smec.EmbeddedDataSpecifications = ds.Select((dsi) => new EmbeddedDataSpecification(dsi, null)).ToList();

                    // result
                    return smec;
                }
                else
                    // uups!
                    return null;
            }

            private void TryPopulateReferenceAttribute(
                SystemUnitClassType ie, string ifName, string ifClassPath, ISubmodelElement target,
                int targetId = 0)
            {
                // now used
                var ei = FindExternalInterfaceByNameAndBaseClassPath(ie.ExternalInterface, ifName, ifClassPath);
                if (ei != null)
                {
                    // 1st, try parse internal AML relationship
                    // by this, AML can easily setup a reference
                    // to do so, register a link and attach the appropriate lambda
                    this.registerForInternalLinks.Add(
                        "" + ie.ID + ":" + ifName,
                        new TargetIdAction(
                            targetId,
                            (il, ti) =>
                            {
                                // trivial
                                if (il == null || ti != targetId)
                                    return;
                                // assume to be side A
                                if (il.RelatedObjects.ASystemUnitClass == null ||
                                    il.RelatedObjects.ASystemUnitClass != ie)
                                    return;
                                // extract side B
                                if (il.RelatedObjects.BSystemUnitClass == null)
                                    return;
                                // need to find the AASX entity of it!
                                // in a good world, the match can this do for us!
                                var aasref = matcher.GetAasObject(il.RelatedObjects.BSystemUnitClass);
                                if (aasref == null)
                                    return;
                                // get a "real" reference of this
                                var theref = new Reference(ReferenceTypes.ModelReference, new List<Key>());
                                aasref.CollectReferencesByParent(theref.Keys);
                                // nooooooooooow, set this
                                if (targetId == 1 && target is ReferenceElement tre)
                                    tre.Value = theref;
                                if (targetId == 2 && target is RelationshipElement trse)
                                    trse.First = theref;
                                if (targetId == 3 && target is RelationshipElement tre2)
                                    tre2.Second = theref;
                            })
                        );

                    // 2nd (but earlier in evaluation sequence), we can try to access the AAS Reference
                    // via value directly
                    var value = FindAttributeValueByRefSemantic(
                        ei.Attribute, AmlConst.Attributes.ReferenceElement_Value);
                    if (value != null)
                    {
                        if (targetId == 1 && target is ReferenceElement tre)
                            tre.Value = ParseAmlReference(value);
                        if (targetId == 2 && target is RelationshipElement trse)
                            trse.First = ParseAmlReference(value);
                        if (targetId == 3 && target is RelationshipElement tre2)
                            tre2.Second = ParseAmlReference(value);
                    }
                }
            }

            private ISubmodelElement TryPopulateSubmodelElement(
                SystemUnitClassType ie, ISubmodelElement sme, bool aasStyleAttributes = false,
                bool amlStyleAttributes = true)
            {
                // access?
                if (sme == null)
                    return null;

                // gather important attributes
                var idShort = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_IdShort);
                var semid = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.SemanticId);
                var kind = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.HasKind_Kind);
                var cat = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_Category);
                var desc = TryParseDescriptionFromAttributes(ie.Attribute, AmlConst.Attributes.Referable_Description);
                var qualifiers = TryParseQualifiersFromAttributes(ie.Attribute);
                var ds = TryParseDataSpecificationFromAttributes(ie.Attribute);

                if (ie.Name != null)
                {
                    // set information
                    sme.IdShort = ie.Name;
                    if (idShort != null)
                        sme.IdShort = idShort;
                    if (semid != null)
                        sme.SemanticId = new Reference(ReferenceTypes.ModelReference, ParseAmlReference(semid)?.Keys);
                    if (kind != null)
                        sme.Kind = Stringification.ModelingKindFromString(kind);
                    if (desc != null)
                        sme.Description = desc;
                    if (cat != null)
                        sme.Category = cat;
                    if (qualifiers != null)
                        sme.Qualifiers = qualifiers;
                    if (ds != null)
                        sme.EmbeddedDataSpecifications = ds.Select((dsi) => new EmbeddedDataSpecification(dsi, null)).ToList();

                    // and also special attributes for each adequate type
                    if (sme is Property p)
                    {
                        var value = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Property_Value);
                        var valueAttr = FindAttributeByRefSemantic(ie.Attribute, AmlConst.Attributes.Property_Value);
                        var valueId = FindAttributeValueByRefSemantic(
                            ie.Attribute, AmlConst.Attributes.Property_ValueId);

                        p.Value = value;
                        if (valueId != null)
                            p.ValueId = ParseAmlReference(valueId);
                        if (valueAttr != null)
                            p.ValueType = Stringification.DataTypeDefXsdFromString(ParseAmlDataType(
                                valueAttr.AttributeDataType)) ?? DataTypeDefXsd.String;
                    }

                    if (sme is AasCore.Aas3_0_RC02.Range rng)
                    {
                        var min = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Range_Min);
                        var minAttr = FindAttributeByRefSemantic(ie.Attribute, AmlConst.Attributes.Range_Min);

                        var max = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Range_Max);
                        var maxAttr = FindAttributeByRefSemantic(ie.Attribute, AmlConst.Attributes.Range_Max);

                        if (min != null)
                        {
                            rng.Min = min;
                            if (minAttr != null)
                                rng.ValueType = Stringification.DataTypeDefXsdFromString(ParseAmlDataType(minAttr.AttributeDataType))
                                    ?? DataTypeDefXsd.String;
                        }

                        if (max != null)
                        {
                            rng.Max = max;
                            if (maxAttr != null)
                                rng.ValueType = Stringification.DataTypeDefXsdFromString(ParseAmlDataType(maxAttr.AttributeDataType))
                                    ?? DataTypeDefXsd.String;
                        }
                    }

                    if (sme is MultiLanguageProperty mlp)
                    {
                        var value = TryParseDescriptionFromAttributes(
                            ie.Attribute, AmlConst.Attributes.MultiLanguageProperty_Value);
                        var valueId = FindAttributeValueByRefSemantic(
                            ie.Attribute, AmlConst.Attributes.MultiLanguageProperty_ValueId);

                        if (value != null)
                            mlp.Value = value.Copy();
                        if (valueId != null)
                            mlp.ValueId = ParseAmlReference(valueId);
                    }

                    if (sme is Blob smeb)
                    {
                        var mimeType = FindAttributeValueByRefSemantic(
                            ie.Attribute, AmlConst.Attributes.Blob_MimeType);
                        var value = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Blob_Value);
                        if (mimeType != null)
                            smeb.ContentType = mimeType;
                        if (value != null)
                            smeb.Value = Encoding.Default.GetBytes(value);
                    }

                    if (sme is File smef)
                    {
                        var mimeType = FindAttributeValueByRefSemantic(
                            ie.Attribute, AmlConst.Attributes.File_MimeType);
                        var value = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.File_Value);
                        if (mimeType != null)
                            smef.ContentType = mimeType;
                        if (value != null)
                            smef.Value = value;
                    }

                    if (sme is ReferenceElement smer)
                    {
                        if (aasStyleAttributes)
                        {
                            // not used anymore!
                            var value = FindAttributeValueByRefSemantic(
                                ie.Attribute, AmlConst.Attributes.ReferenceElement_Value);
                            if (value != null)
                                smer.Value = ParseAmlReference(value);
                        }

                        if (amlStyleAttributes)
                        {
                            // now the default
                            TryPopulateReferenceAttribute(
                                ie, "ReferableReference", AmlConst.Interfaces.ReferableReference, smer, 1);
                        }
                    }

                    // will also include AnnotatedRelationship !!
                    if (sme is RelationshipElement smere)
                    {
                        if (aasStyleAttributes)
                        {
                            // not used anymore!
                            var first = FindAttributeValueByRefSemantic(
                                ie.Attribute, AmlConst.Attributes.RelationshipElement_First);
                            var second = FindAttributeValueByRefSemantic(
                                ie.Attribute, AmlConst.Attributes.RelationshipElement_Second);
                            if (first != null && second != null)
                            {
                                smere.First = ParseAmlReference(first);
                                smere.Second = ParseAmlReference(second);
                            }
                        }

                        if (amlStyleAttributes)
                        {
                            // now the default
                            TryPopulateReferenceAttribute(
                                ie, "first", AmlConst.Interfaces.ReferableReference, smere, 2);
                            TryPopulateReferenceAttribute(
                                ie, "second", AmlConst.Interfaces.ReferableReference, smere, 3);
                        }
                    }

                    if (sme is Entity ent)
                    {
                        var entityType = FindAttributeValueByRefSemantic(
                            ie.Attribute, AmlConst.Attributes.Entity_entityType);
                        if (entityType != null)
                            ent.EntityType = (EntityType)Stringification.EntityTypeFromString(entityType);

                        var assetRef = FindAttributeValueByRefSemantic(
                                ie.Attribute, AmlConst.Attributes.Entity_asset);
                        if (assetRef != null)
                        {
                            var reference = ParseAmlReference(assetRef);
                            ent.GlobalAssetId = new Reference(reference.Type, reference.Keys);
                        }
                    }

                    // ok
                    return sme;
                }
                else
                    // uups!
                    return null;
            }

            private ConceptDescription TryParseConceptDescription(AttributeSequence aseq)
            {
                // begin new (temporary) object
                var cd = new ConceptDescription("");

                // gather important attributes
                var idShort = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.Referable_IdShort);
                var id = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.Identification_id);
                var version = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.Administration_Version);
                var revision = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.Administration_Revision);
                var cat = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.Referable_Category);
                var desc = TryParseDescriptionFromAttributes(aseq, AmlConst.Attributes.Referable_Description);

                // we need to have some important information (only IReferable name, shoud be always there..)
                if (id != null)
                {
                    // set normal data
                    cd.IdShort = idShort;
                    cd.Id = id;
                    if (version != null && revision != null)
                        cd.Administration = new AdministrativeInformation(version: version, revision: revision);
                    if (desc != null)
                        cd.Description = desc;
                    if (cat != null)
                        cd.Category = cat;

                    // special data
                    cd.IsCaseOf = TryParseListItemsFromAttributes<Reference>(
                        aseq, AmlConst.Attributes.CD_IsCaseOf, (s) => { return ParseAmlReference(s); });

                    // result
                    return cd;
                }
                else
                    // uups!
                    return null;
            }

            private DataSpecificationIec61360 TryParseDataSpecificationContentIEC61360(
                AttributeSequence aseq)
            {
                // finally, create the entity
                var ds = new DataSpecificationIec61360(null);

                // populate
                var pn = TryParseListOfLangStrFromAttributes(aseq, AmlConst.Attributes.CD_DSC61360_PreferredName);
                if (pn != null)
                    ds.PreferredName = new List<LangString>(pn);

                var sn = TryParseListOfLangStrFromAttributes(aseq, AmlConst.Attributes.CD_DSC61360_ShortName);
                if (sn != null)
                    ds.ShortName = new List<LangString>(sn);

                ds.Unit = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.CD_DSC61360_Unit);

                ds.UnitId = ParseAmlReference(FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.CD_DSC61360_UnitId))?.Copy();

                ds.ValueFormat = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.CD_DSC61360_ValueFormat);

                ds.SourceOfDefinition = FindAttributeValueByRefSemantic(
                    aseq, AmlConst.Attributes.CD_DSC61360_SourceOfDefinition);

                ds.Symbol = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.CD_DSC61360_Symbol);
                ds.DataType = Stringification.DataTypeIec61360FromString(
                    FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.CD_DSC61360_DataType) ?? "string");

                var def = TryParseListOfLangStrFromAttributes(aseq, AmlConst.Attributes.CD_DSC61360_Definition);
                if (def != null)
                    ds.Definition = new List<LangString>(def);

                // done, without further checks
                return ds;
            }

            /// <summary>
            /// Returns a valid AdminShell value type for an AML data type
            /// </summary>
            public string ParseAmlDataType(string dt)
            {
                if (dt == null)
                    return "";
                dt = dt.Trim().ToLower();
                dt = dt.Replace("xs:", "");
                return dt;
            }

            /// <summary>
            /// Tries to build up one or multiple AAS with respective entities based on the hierarchy of internal
            /// elements, found.
            /// Utilizes recursion!
            /// </summary>
            public void ParseInternalElementsForAasEntities(
                InstanceHierarchyType insthier,
                InternalElementSequence ieseq,
                AssetAdministrationShell currentAas = null,
                Submodel currentSubmodel = null,
                IReferable currentSmeCollection = null,
                Operation currentOperation = null,
                int currentOperationDir = -1,
                int indentation = 0)
            {
                if (ieseq == null)
                    return;
                foreach (var ie in ieseq)
                {
                    // start
                    Debug(indentation, "Consulting IE name {0}", ie.Name);

                    //
                    // find mirror elements
                    //
                    InternalElementType mirrorTarget = null;
                    if (ie.RefBaseSystemUnitPath != null)
                    {
                        var x = FindInternalElementByID(ie.RefBaseSystemUnitPath);
                        if (x != null)
                            mirrorTarget = x;
                    }

                    //
                    // AAS
                    //
                    if (CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.AAS))
                    {
                        // begin new (temporary) object
                        var aas = TryParseAasFromIe(ie);
                        if (aas != null)
                        {
                            Debug(indentation, "  AAS with required attributes recognised. Starting new AAS..");

                            // make temporary object official
                            this.package.AasEnv.AssetAdministrationShells.Add(aas);
                            currentAas = aas;
                            matcher.AddMatch(aas, ie);
                        }
                        else
                            Debug(indentation, "  AAS with insufficient attributes. Skipping");
                    }

                    //
                    // AssetInformation
                    //
                    if (currentAas != null && CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.AssetInformation))
                    {
                        // begin new (temporary) object
                        var asset = TryParseAssetFromIe(ie);
                        if (asset != null)
                        {
                            Debug(indentation, "  ASSET with required attributes recognised. Starting new AssetInformation..");

                            // make temporary object official
                            currentAas.AssetInformation = asset;
                            //matcher.AddMatch(asset, ie); //TODO jtikekar AssetInformation is not Referable
                        }
                        else
                            Debug(indentation, "  ASSET with insufficient attributes. Skipping");
                    }

                    //
                    // View
                    //
                    //View removed from V3
                    //if (currentAas != null && CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.View))
                    //{
                    //    // begin new (temporary) object
                    //var view = TryParseViewFromIe(insthier, ie);
                    //    if (view != null)
                    //    {
                    //        Debug(indentation, "  VIEW with required attributes recognised. Collecting references..");

                    //        // make temporary object official
                    //        currentAas.AddView(view);
                    //        matcher.AddMatch(view, ie);
                    //    }
                    //    else
                    //        Debug(indentation, "  VIEW with insufficient attributes. Skipping");
                    //}

                    //
                    // Submodel
                    //
                    if (currentAas != null && CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.Submodel))
                    {
                        // begin new (temporary) object
                        var sm = TryParseSubmodelFromIe(ie);
                        if (sm != null && (sm.Kind == null || sm.Kind == ModelingKind.Instance))
                        {
                            Debug(
                                indentation,
                                "  SUBMODEL with required attributes recognised. Starting new Submodel..");

                            // there might be the case, that a submodel with the same identification is already
                            // existing.
                            // If so, that switch to it and ignore the newly parsed set of information
                            // (TODO: check, if to merge information?)
                            var existSm = this.package.AasEnv.FindSubmodelById(sm.Id);
                            if (existSm != null)
                                sm = existSm;

                            // make temporary object official
                            currentSubmodel = sm;
                            matcher.AddMatch(sm, ie);

                            // this will be the parent for child elements
                            // Remark: add only, if not a SM with the same ID is existing. This could have the
                            // consequences that additional properties in the 2nd SM with the same SM get lost!
                            if (null == this.package.AasEnv.FindSubmodelById(sm.Id))
                                this.package.AasEnv.Submodels.Add(sm);
                            if (currentAas.Submodels == null)
                            {
                                currentAas.Submodels = new List<Reference>();
                            }
                            currentAas.Submodels.Add(sm.GetReference());
                            currentSmeCollection = sm;
                        }
                        else
                            Debug(indentation, "  SUBMODEL with insufficient attributes. Skipping");
                    }

                    // Mirror of Submodel?
                    if (currentAas != null && mirrorTarget != null &&
                        CheckForRoleClassOrRoleRequirements(mirrorTarget, AmlConst.Roles.Submodel))
                    {
                        // try parse EXISTING (target) Submodel -> to get Identification
                        var targetSm = TryParseSubmodelFromIe(mirrorTarget);
                        if (targetSm != null && this.package != null && this.package.AasEnv != null)
                        {
                            // try use Identification to find existing Submodel
                            var existSm = package.AasEnv.FindSubmodelById(targetSm.Id);
                            if (currentAas.Submodels == null)
                            {
                                currentAas.Submodels = new List<Reference>();
                            }

                            // if so, add a SubmodelRef
                            currentAas.Submodels.Add(existSm.GetReference());
                        }
                    }

                    //
                    // Submodel Element Collection
                    //
                    var parentSmeCollection = currentSmeCollection;
                    if (currentAas != null && currentSubmodel != null &&
                        CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.SubmodelElement_SMEC))
                    {
                        // begin new (temporary) object
                        var smec = TryParseSubmodelElementCollection(ie);
                        if (smec != null)
                        {
                            Debug(
                                indentation,
                                "  SUBMODEL-ELEMENT-COLLECTION with required attributes recognised. " +
                                "Starting new SME..");

                            // make collection official
                            AddToSubmodelOrSmec(currentSmeCollection, smec);
                            matcher.AddMatch(smec, ie);

                            // will be the ne parent for child elements
                            parentSmeCollection = smec;
                        }
                        else
                            Debug(indentation, "  SUBMODEL-ELEMENT-COLLECTION with insufficient attributes. Skipping");
                    }

                    //
                    // Entity
                    //
                    if (currentAas != null && currentSubmodel != null &&
                        CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.SubmodelElement_Entity))
                    {
                        // begin new (temporary) object
                        var ent = new Entity(EntityType.SelfManagedEntity);
                        ent = TryPopulateSubmodelElement(ie, ent) as Entity;
                        if (ent != null)
                        {
                            Debug(
                                indentation,
                                "  ENTITY with required attributes recognised. " +
                                "Starting new ENTITY..");

                            // make collection official
                            AddToSubmodelOrSmec(currentSmeCollection, ent);
                            matcher.AddMatch(ent, ie);

                            // will be the ne parent for child elements
                            parentSmeCollection = ent;
                        }
                        else
                            Debug(indentation, "  ENTITY with insufficient attributes. Skipping");
                    }

                    //
                    // Annotated Relationship
                    //
                    if (currentAas != null && currentSubmodel != null &&
                        CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.SubmodelElement_AnnotatedRelationship))
                    {
                        // begin new (temporary) object
                        var ent = new AnnotatedRelationshipElement(null, null);
                        ent = TryPopulateSubmodelElement(ie, ent) as AnnotatedRelationshipElement;
                        if (ent != null)
                        {
                            Debug(
                                indentation,
                                "  ANNOTATED-RELATIONSHIP with required attributes recognised. " +
                                "Starting new ANNOTATED-RELATIONSHIP..");

                            // make collection official
                            AddToSubmodelOrSmec(currentSmeCollection, ent);
                            matcher.AddMatch(ent, ie);

                            // will be the ne parent for child elements
                            parentSmeCollection = ent;
                        }
                        else
                            Debug(indentation, "  ANNOTATED-RELATIONSHIP with insufficient attributes. Skipping");
                    }

                    //
                    // in Submodel oder SMEC, look out for attributes
                    //
                    #region
                    if (ie.Attribute != null && currentAas != null && currentSubmodel != null &&
                        currentSmeCollection != null)
                        foreach (var a in ie.Attribute)
                            if (CheckAttributeFoRefSemantic(a, "AAS_Property"))
                            {
                                // create a Property
                                Debug(indentation, "  found ATTR {0}. Adding as property.", a.Name);

                                var p = new Property(DataTypeDefXsd.String)
                                {
                                    IdShort = a.Name,
                                    Value = a.Value,
                                    ValueType = (DataTypeDefXsd)Stringification.DataTypeDefXsdFromString(ParseAmlDataType(a.AttributeDataType)),
                                    Qualifiers = TryParseQualifiersFromAttributes(a.Attribute),
                                    EmbeddedDataSpecifications = TryParseDataSpecificationFromAttributes(ie.Attribute)?
                                        .Select((dsi) => new EmbeddedDataSpecification(dsi, null)).ToList()
                                };

                                // gather information
                                var semid = FindAttributeValueByRefSemantic(
                                    a.Attribute, AmlConst.Attributes.SemanticId);

                                if (semid != null)
                                {
                                    p.SemanticId = new Reference(ReferenceTypes.ModelReference, ParseAmlReference(semid)?.Keys);
                                }

                                // add
                                AddToSubmodelOrSmec(currentSmeCollection, p);
                            }
                    #endregion

                    //
                    // Operation Variable List In / Out
                    //
                    for (int i = 0; i < 2; i++)
                        if (currentAas != null && currentSubmodel != null && currentOperation != null &&
                            CheckForRoleClassOrRoleRequirements(
                                ie, (new[] {
                                    AmlConst.Roles.OperationVariableIn, AmlConst.Roles.OperationVariableOut })[i]))
                        {
                            Debug(
                                indentation, "  List of OPERATION VARIABLE detected. Switching direction to {0}..", i);

                            // switch direction
                            currentOperationDir = i;
                        }

                    //
                    // in Submodel, SMEC, also Internal Elements can have a property role
                    //
                    #region                    
                    // Note MIHO, 2020-10-18): I presume, that SMC shall be excluded from th search, hence
                    // do another kind of comparison
                    // reSharper disable once ForCanBeConvertedToForeach
                    foreach (AasSubmodelElements smeEnum in Enum.GetValues(typeof(AasSubmodelElements)))
                    {
                        if (smeEnum == AasSubmodelElements.SubmodelElement || smeEnum == AasSubmodelElements.SubmodelElementList || smeEnum == AasSubmodelElements.SubmodelElementCollection || smeEnum == AasSubmodelElements.AnnotatedRelationshipElement || smeEnum == AasSubmodelElements.Entity)
                        {
                            continue;
                        }

                        if (CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.SubmodelElement_Header + smeEnum.ToString()))
                        {
                            // begin new (temporary) object
                            //var sme = SubmodelElementWrapper.CreateAdequateType(ae);
                            var sme = AdminShellUtil.CreateSubmodelElementFromEnum(smeEnum);
                            if (sme == null)
                                continue;

                            // populate
                            sme = TryPopulateSubmodelElement(ie, sme);
                            matcher.AddMatch(sme, ie);
                            if (sme != null)
                            {
                                if (currentOperation == null || currentOperationDir < 0 || currentOperationDir >= 2)
                                {
                                    Debug(
                                        indentation,
                                        "  SUBMODEL-ELEMENT {0} with required attributes recognised. " +
                                        "Adding to a SMEC..", sme.GetType().ToString());

                                    // add
                                    AddToSubmodelOrSmec(currentSmeCollection, sme);

                                    // need keep track of state
                                    if (sme is Operation)
                                    {
                                        currentOperation = sme as Operation;
                                        currentOperationDir = -1;
                                    }
                                }
                                else
                                {
                                    Debug(
                                        indentation,
                                        "  SUBMODEL-ELEMENT {0} with required attributes recognised. " +
                                        "Adding to a pending OPERATION..", sme.GetType().ToString());

                                    // add
                                    ISubmodelElement wrapper = sme;
                                    var opv = new OperationVariable(wrapper);
                                    currentOperation.InputVariables.Add(opv);
                                    //currentOperation[currentOperationDir].Add(opv);
                                }
                            }
                            else
                                Debug(indentation, "  SUBMODEL-ELEMENT with insufficient attributes. Skipping");
                        }
                        #endregion

                    }


                    // recurse into childs
                    ParseInternalElementsForAasEntities(
                        insthier, ie.InternalElement, currentAas, currentSubmodel, parentSmeCollection,
                        currentOperation, currentOperationDir, indentation + 1);
                }

            }

            public void ParseSystemUnits(
                CAEXSequenceOfCAEXObjects<SystemUnitFamilyType> sucseq,
                AssetAdministrationShell currentAas = null,
                Submodel currentSubmodel = null,
                IReferable currentSmeCollection = null,
                Operation currentOperation = null,
                int currentOperationDir = -1,
                int indentation = 0)
            {
                if (sucseq == null)
                    return;
                foreach (var suc in sucseq)
                {
                    // start
                    Debug(indentation, "Consulting SUC name {0}", suc.Name);

                    //
                    // AAS
                    //
                    if (CheckForRoleClassOrRoleRequirements(suc, AmlConst.Roles.AAS))
                    {
                        // begin new (temporary) object
                        var aas = TryParseAasFromIe(suc);
                        if (aas != null)
                        {
                            Debug(indentation, "  AAS with required attributes recognised. Starting new AAS..");

                            // AAS might already exist by parsing instance, therefore check for existance
                            // If so, then switch to it and ignore the newly parsed set of information
                            // (TODO: check, if to merge information?)
                            var existAas = this.package.AasEnv.FindAasById(aas.Id);
                            if (existAas != null)
                                aas = existAas;
                            else
                                // add
                                this.package.AasEnv.AssetAdministrationShells.Add(aas);

                            // remember
                            currentAas = aas;
                        }
                        else
                            Debug(indentation, "  AAS with insufficient attributes. Skipping");
                    }

                    //
                    // Submodel
                    //
                    if (currentAas != null && CheckForRoleClassOrRoleRequirements(suc, AmlConst.Roles.Submodel))
                    {
                        // begin new (temporary) object
                        var sm = TryParseSubmodelFromIe(suc);
                        if (sm != null && sm.Kind != null && sm.Kind == ModelingKind.Template)
                        {
                            Debug(
                                indentation,
                                "  SUBMODEL with required attributes recognised. Starting new Submodel..");

                            // there might be the case, that a submodel with the same identification already exists.
                            // If so, that switch to it and ignore the newly parsed set of information
                            // (TODO: check, if to merge information?)
                            var existSm = this.package.AasEnv.FindSubmodelById(sm.Id);
                            if (existSm != null)
                                sm = existSm;

                            // make temporary object official
                            currentSubmodel = sm;

                            // this will be the parent for child elements
                            this.package.AasEnv.Submodels.Add(sm);
                            if (currentAas.Submodels == null)
                            {
                                currentAas.Submodels = new List<Reference>();
                            }
                            currentAas.Submodels.Add(sm.GetReference());
                            currentSmeCollection = sm;
                        }
                        else
                            Debug(indentation, "  SUBMODEL with insufficient attributes. Skipping");
                    }

                    // recurse into childs, which are SUC
                    ParseSystemUnits(
                        suc.SystemUnitClass, currentAas, currentSubmodel, currentSmeCollection, currentOperation,
                        currentOperationDir, indentation + 1);
                    // switch recursion into InternalElements
                    if (suc is SystemUnitClassType suct)
                        ParseInternalElementsForAasEntities(
                            null, suct.InternalElement, currentAas, currentSubmodel, currentSmeCollection,
                            currentOperation, currentOperationDir, indentation + 1);
                }

            }

            /// <summary>
            /// Go recursively through internal elements to identify concept descriptions, and below,
            /// data specifications.
            /// </summary>
            public void ParseIEsForConceptDescriptions(
                InstanceHierarchyType insthier,
                InternalElementSequence ieseq,
                int indentation = 0)
            {
                if (ieseq == null)
                    return;
                foreach (var ie in ieseq)
                {
                    // start
                    Debug(indentation, "Consulting IE name {0} for CDs", ie.Name);

                    // (outer) Concept Description
                    if (CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.ConceptDescription))
                    {
                        var cd = TryParseConceptDescription(ie.Attribute);
                        if (cd != null)
                        {
                            // add
                            Debug(indentation, " .. added as {0}", cd.Id);
                            this.package.AasEnv.ConceptDescriptions.Add(cd);

                            // look for direct descendants = Data Specifcations
                            if (ie.InternalElement != null)
                                foreach (var ie2 in ie.InternalElement)
                                    if (ie2.RefBaseSystemUnitPath != null &&
                                        ie2.RefBaseSystemUnitPath.Trim() ==
                                            AmlConst.Classes.DataSpecificationContent61360)
                                    {
                                        // (inner) Data Spec
                                        var ds61360 = TryParseDataSpecificationContentIEC61360(ie2.Attribute);
                                        if (ds61360 != null)
                                        {
                                            //embedded data spec for the SDK
                                            /*
                                             TODO (Michael Hoffmeister, 2020-08-01): fill out 
                                             eds.hasDataSpecification by using outer attributes
                                            */

                                            var eds = ExtendEmbeddedDataSpecification.CreateIec61360WithContent(ds61360);

                                            var hds = FindAttributeValueByRefSemantic(
                                                ie.Attribute, AmlConst.Attributes.CD_DataSpecificationRef);
                                            if (hds != null)
                                                eds.DataSpecification = ParseAmlReference(hds).Copy();

                                            cd.EmbeddedDataSpecifications = new List<EmbeddedDataSpecification>();
                                            cd.EmbeddedDataSpecifications.Add(eds);
                                        }
                                    }
                        }
                    }

                    // recurse into childs
                    ParseIEsForInternalLinks(ie.InternalElement, indentation + 1);
                }

            }

            /// <summary>
            /// Go recursively through internal elements to consult internal links
            /// </summary>
            public void ParseIEsForInternalLinks(
                InternalElementSequence ieseq,
                int indentation = 0)
            {
                if (ieseq == null)
                    return;
                foreach (var ie in ieseq)
                {
                    // start
                    Debug(indentation, "Consulting IE name {0} for internal links", ie.Name);

                    // find some links?
                    if (ie.InternalLink != null)
                        foreach (var il in ie.InternalLink)
                        {
                            // trivial check
                            if (il == null)
                                continue;
                            // try find registered sides
                            foreach (var side in new[] { il.RefPartnerSideA, il.RefPartnerSideB })
                            {
                                if (!this.registerForInternalLinks.ContainsKey(side))
                                    continue;
                                var items = this.registerForInternalLinks[side];
                                if (items != null)
                                    foreach (var it in items)
                                        it.Action?.Invoke(il, it.TargetId);
                            }
                        }

                    // recurse into childs
                    ParseIEsForInternalLinks(ie.InternalElement, indentation + 1);
                }

            }

            /// <summary>
            /// Populate views with contained elements refs.
            /// Precondition: AAS entities exist, matcher up to data, parents up to date
            /// </summary>
            public void LatePopulateViews()
            {
                foreach (var ieViewAmlTarget in this.latePopoulationViews)
                {
                    // access
                    if (ieViewAmlTarget.Ie == null /*|| ieViewAmlTarget.View == null*/ || ieViewAmlTarget.AmlTarget == null)
                        continue;

                    // we need to identify the target with respect to the AAS
                    var aasTarget = matcher.GetAasObject(ieViewAmlTarget.AmlTarget);
                    if (aasTarget == null)
                        continue;

                    // get a "real" reference of this
                    var theref = new Reference(ReferenceTypes.ModelReference, new List<Key>());
                    aasTarget.CollectReferencesByParent(theref.Keys);

                    // add
                    //ieViewAmlTarget.View.AddContainedElement(theref);
                }
            }

            /// <summary>
            /// Build up internal data structures
            /// </summary>
            /// <param name="caex">Input AML / CAEX file</param>
            public void Start(CAEXFileType caex)
            {
                foreach (var x in caex.InstanceHierarchy)
                    FillDictWithInternalElementsIds(idDict, x.InternalElement);
            }
        }

        public static void ImportInto(AdminShellPackageEnv package, string amlfn)
        {
            // try open file
            var doc = CAEXDocument.LoadFromFile(amlfn);

            // new parser
            var parser = new AmlParser(package);

            // start, to build up internal data structures
            parser.Start(doc.CAEXFile);

            // try find ConceptDescriptions
            foreach (var x in doc.CAEXFile.InstanceHierarchy)
                parser.ParseIEsForConceptDescriptions(x, x.InternalElement);

            // over all instances (containing AAS information)
            foreach (var x in doc.CAEXFile.InstanceHierarchy)
                parser.ParseInternalElementsForAasEntities(x, x.InternalElement);

            // after instances, try find kind=Type information for Submodels
            // assumption: the AAS information is already conveyed by the instance hierarchy
            foreach (var x in doc.CAEXFile.SystemUnitClassLib)
                parser.ParseSystemUnits(x.SystemUnitClass);

            // the following steps will require valid parent information
            foreach (var sm in package.AasEnv.Submodels)
                sm.SetAllParents();

            // do the late population of views
            parser.LatePopulateViews();

            // the different parse stages might have reegistered actions to be source or
            // destination of AML internal links
            // find all internal links and assess them ..
            foreach (var x in doc.CAEXFile.InstanceHierarchy)
                parser.ParseIEsForInternalLinks(x.InternalElement);
        }
    }
}
