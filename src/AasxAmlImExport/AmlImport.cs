using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AdminShellNS;
using Aml.Engine.CAEX;

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).

The AutomationML.Engine is under the MIT license.
(see https://raw.githubusercontent.com/AutomationML/AMLEngine2.1/master/license.txt)
*/

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
                public AdminShell.View View;
                public CAEXObject AmlTarget;

                public IeViewAmlTarget(InternalElementType ie, AdminShellV20.View view, CAEXObject amlTarget)
                {
                    Ie = ie;
                    View = view;
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

            public AdminShell.Reference ParseAmlReference(string refstr)
            {
                // trivial
                if (refstr == null)
                    return null;

                // a reference could carry multiple Keys, delimited by ","
                var refstrs = refstr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (refstr.Length < 1)
                    return null;

                // build a Reference
                var res = new AdminShell.Reference();

                // over all entries
                foreach (var rs in refstrs)
                {
                    var m = Regex.Match(rs.Trim(), @"^\(([^)]+)\)\s*\(([^)]+)\)\s*\[(\w+)\](.*)$");
                    if (!m.Success)
                        // immediate fail or next try?
                        return null;

                    // get string data
                    var ke = m.Groups[1].ToString();
                    var local = m.Groups[2].ToString().Trim().ToLower();
                    var idtype = m.Groups[3].ToString();
                    var id = m.Groups[4].ToString();

                    // verify: ke has to be in allowed range
                    if (!AdminShell.Key.IsInKeyElements(ke))
                        return null;
                    var islocal = local == "local";

                    // create key and make on refece
                    var k = new AdminShell.Key(ke, islocal, idtype, id);
                    res.Keys.Add(k);
                }
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

            public AdminShell.ListOfLangStr TryParseListOfLangStrFromAttributes(
                AttributeSequence aseq, string correspondingAttributePath)
            {
                if (aseq == null || correspondingAttributePath == null)
                    return null;
                var aroot = FindAttributeByRefSemantic(aseq, correspondingAttributePath);
                if (aroot == null)
                    return null;

                // primary stuff
                var res = new AdminShell.ListOfLangStr { new AdminShell.LangStr("Default", aroot.Value) };

                // assume the language-specific attributes being directly sub-ordinated
                if (aroot.Attribute != null)
                    foreach (var a in aroot.Attribute)
                    {
                        var m = Regex.Match(a.Name.Trim(), @"([^=]+)\w*=(.*)$");
                        if (m.Success && m.Groups[1].ToString().ToLower() == "aml-lang")
                            res.Add(new AdminShell.LangStr(m.Groups[2].ToString(), a.Value));
                    }

                // end
                return res;
            }

            public AdminShell.Description TryParseDescriptionFromAttributes(
                AttributeSequence aseq, string correspondingAttributePath)
            {
                var ls = TryParseListOfLangStrFromAttributes(aseq, correspondingAttributePath);
                if (ls == null)
                    return null;

                var res = new AdminShell.Description() { langString = ls };
                return res;
            }

            public AdminShell.QualifierCollection TryParseQualifiersFromAttributes(AttributeSequence aseq)
            {
                if (aseq == null)
                    return null;

                AdminShell.QualifierCollection res = null;
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
                            var q = new AdminShell.Qualifier()
                            {
                                type = qt,
                                value = qv,
                                semanticId = AdminShell.SemanticId.CreateFromKeys(ParseAmlReference(sid)?.Keys),
                                valueId = ParseAmlReference(qvid)
                            };

                            // add
                            if (res == null)
                                res = new AdminShellV20.QualifierCollection();
                            res.Add(q);
                        }
                    }

                return res;
            }

            public AdminShell.HasDataSpecification TryParseDataSpecificationFromAttributes(AttributeSequence aseq)
            {
                if (aseq == null)
                    return null;

                AdminShell.HasDataSpecification res = null;
                foreach (var a in aseq)
                    if (CheckAttributeFoRefSemantic(a, AmlConst.Attributes.DataSpecificationRef))
                    {
                        var r = ParseAmlReference(a.Value);
                        if (r != null)
                        {
                            if (res == null)
                                res = new AdminShell.HasDataSpecification();
                            res.Add(new AdminShell.EmbeddedDataSpecification(r));
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

            private void AddToSubmodelOrSmec(AdminShell.Referable parent, AdminShell.SubmodelElement se)
            {
                // TODO (MIHO, 2020-10-19): it should exist a generic Interface IManageSubmodelElements
                // in AdminShell.cs which allows arbitrary elements to add children
                if (parent is AdminShell.Submodel psm)
                    psm.Add(se);

                if (parent is AdminShell.SubmodelElementCollection psmc)
                    psmc.Add(se);

                if (parent is AdminShell.Entity ent)
                    ent.Add(se);

                if (parent is AdminShell.AnnotatedRelationshipElement anno)
                    anno.Add(se);
            }

            private AdminShell.AdministrationShell TryParseAasFromIe(SystemUnitClassType ie)
            {
                // begin new (temporary) object
                var aas = new AdminShell.AdministrationShell();

                // gather important attributes
                var idShort = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_IdShort);
                var idType = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Identification_idType);
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
                if (idType != null && id != null)
                {
                    // set data
                    aas.idShort = ie.Name;
                    if (idShort != null)
                        aas.idShort = idShort;
                    aas.identification = new AdminShell.Identification(idType, id);
                    if (version != null && revision != null)
                        aas.administration = new AdminShell.Administration(version, revision);
                    aas.category = cat;
                    if (desc != null)
                        aas.description = desc;
                    if (ds != null)
                        aas.hasDataSpecification = ds;
                    if (derivedfrom != null)
                        aas.derivedFrom = new AdminShell.AssetAdministrationShellRef(ParseAmlReference(derivedfrom));

                    // result
                    return aas;
                }
                else
                    // uups!
                    return null;
            }

            private AdminShell.Asset TryParseAssetFromIe(InternalElementType ie)
            {
                // begin new (temporary) object
                var asset = new AdminShell.Asset();

                // gather important attributes
                var idShort = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_IdShort);
                var idType = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Identification_idType);
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
                if (idType != null && id != null)
                {
                    // set data
                    asset.idShort = ie.Name;
                    if (idShort != null)
                        asset.idShort = idShort;
                    asset.identification = new AdminShell.Identification(idType, id);
                    if (version != null && revision != null)
                        asset.administration = new AdminShell.Administration(version, revision);
                    asset.category = cat;
                    if (desc != null)
                        asset.description = desc;
                    if (kind != null)
                        asset.kind = new AdminShell.AssetKind(kind);
                    if (ds != null)
                        asset.hasDataSpecification = ds;

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

            private AdminShell.View TryParseViewFromIe(InstanceHierarchyType insthier, InternalElementType ie)
            {
                // access
                if (insthier == null || ie == null)
                    return null;

                //
                // make up local data management
                //

                // begin new (temporary) objects
                var view = new AdminShell.View();

                // gather important attributes
                var idShort = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_IdShort);
                var cat = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_Category);
                var desc = TryParseDescriptionFromAttributes(ie.Attribute, AmlConst.Attributes.Referable_Description);
                var ds = TryParseDataSpecificationFromAttributes(ie.Attribute);
                var semid = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.SemanticId);

                // we need to have some important information
                if (ie.Name != null)
                {
                    // set data
                    view.idShort = ie.Name;
                    if (idShort != null)
                        view.idShort = idShort;
                    view.category = cat;
                    if (desc != null)
                        view.description = desc;
                    if (semid != null)
                        view.semanticId = AdminShell.SemanticId.CreateFromKeys(ParseAmlReference(semid)?.Keys);
                    if (ds != null)
                        view.hasDataSpecification = ds;

                    // check for direct descendents to be "Mirror-Elements"
                    if (ie.InternalElement != null)
                        foreach (var mie in ie.InternalElement)
                            if (mie.RefBaseSystemUnitPath.HasContent())
                            {
                                // candidate .. try identify target
                                var el = FindInternalElementByID(mie.RefBaseSystemUnitPath);
                                if (el != null)
                                {
                                    // for the View's contain element references, all targets of the references
                                    // shall exists.
                                    // This is not already the case, therefore store the AML IE / View Information
                                    // for later parsing
                                    this.latePopoulationViews.Add(new IeViewAmlTarget(ie, view, el));
                                }
                            }

                    // result
                    return view;
                }
                else
                    // uups!
                    return null;
            }

            private AdminShell.Submodel TryParseSubmodelFromIe(SystemUnitClassType ie)
            {
                // begin new (temporary) object
                var sm = new AdminShell.Submodel();

                // gather important attributes
                var idShort = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_IdShort);
                var idType = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Identification_idType);
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
                if (idType != null && id != null)
                {
                    // set data
                    sm.idShort = ie.Name;
                    if (idShort != null)
                        sm.idShort = idShort;
                    sm.identification = new AdminShell.Identification(idType, id);
                    if (version != null && revision != null)
                        sm.administration = new AdminShell.Administration(version, revision);
                    sm.category = cat;
                    if (desc != null)
                        sm.description = desc;
                    if (semid != null)
                        sm.semanticId = AdminShell.SemanticId.CreateFromKeys(ParseAmlReference(semid)?.Keys);
                    if (kind != null)
                        sm.kind = new AdminShell.ModelingKind(kind);
                    if (qualifiers != null)
                        sm.qualifiers = qualifiers;
                    if (ds != null)
                        sm.hasDataSpecification = ds;

                    // result
                    return sm;
                }
                else
                    // uups!
                    return null;
            }

            private AdminShell.SubmodelElementCollection TryParseSubmodelElementCollection(SystemUnitClassType ie)
            {
                // begin new (temporary) object
                var smec = new AdminShell.SubmodelElementCollection();

                // gather important attributes
                var idShort = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_IdShort);
                var semid = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.SemanticId);
                var kind = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.HasKind_Kind);
                var cat = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Referable_Category);
                var desc = TryParseDescriptionFromAttributes(ie.Attribute, AmlConst.Attributes.Referable_Description);
                var qualifiers = TryParseQualifiersFromAttributes(ie.Attribute);
                var ds = TryParseDataSpecificationFromAttributes(ie.Attribute);

                // we need to have some important information (only Referable name, shoud be always there..)
                if (ie.Name != null)
                {
                    // set data
                    smec.idShort = ie.Name;
                    if (idShort != null)
                        smec.idShort = idShort;
                    if (semid != null)
                        smec.semanticId = AdminShell.SemanticId.CreateFromKeys(ParseAmlReference(semid)?.Keys);
                    if (kind != null)
                        smec.kind = new AdminShell.ModelingKind(kind);
                    if (desc != null)
                        smec.description = desc;
                    if (cat != null)
                        smec.category = cat;
                    if (qualifiers != null)
                        smec.qualifiers = qualifiers;
                    if (ds != null)
                        smec.hasDataSpecification = ds;

                    // result
                    return smec;
                }
                else
                    // uups!
                    return null;
            }            

            private void TryPopulateReferenceAttribute(
                SystemUnitClassType ie, string ifName, string ifClassPath, AdminShell.SubmodelElement target,
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
                        "" + ie.ID + ":" + "ReferableReference",
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
                                var theref = new AdminShell.Reference();
                                aasref.CollectReferencesByParent(theref.Keys);
                                // nooooooooooow, set this
                                if (targetId == 1 && target is AdminShell.ReferenceElement tre)
                                    tre.value = theref;
                                if (targetId == 2 && target is AdminShell.RelationshipElement trse)
                                    trse.first = theref;
                                if (targetId == 3 && target is AdminShell.RelationshipElement tre2)
                                    tre2.second = theref;
                            })
                        );

                    // 2nd (but earlier in evaluation sequence), we can try to access the AAS Reference
                    // via value directly
                    var value = FindAttributeValueByRefSemantic(
                        ei.Attribute, AmlConst.Attributes.ReferenceElement_Value);
                    if (value != null)
                    {
                        if (targetId == 1 && target is AdminShell.ReferenceElement tre)
                            tre.value = ParseAmlReference(value);
                        if (targetId == 2 && target is AdminShell.RelationshipElement trse)
                            trse.first = ParseAmlReference(value);
                        if (targetId == 3 && target is AdminShell.RelationshipElement tre2)
                            tre2.second = ParseAmlReference(value);
                    }
                }
            }

            private AdminShell.SubmodelElement TryPopulateSubmodelElement(
                SystemUnitClassType ie, AdminShell.SubmodelElement sme, bool aasStyleAttributes = false,
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
                    sme.idShort = ie.Name;
                    if (idShort != null)
                        sme.idShort = idShort;
                    if (semid != null)
                        sme.semanticId = AdminShell.SemanticId.CreateFromKeys(ParseAmlReference(semid)?.Keys);
                    if (kind != null)
                        sme.kind = new AdminShell.ModelingKind(kind);
                    if (desc != null)
                        sme.description = desc;
                    if (cat != null)
                        sme.category = cat;
                    if (qualifiers != null)
                        sme.qualifiers = qualifiers;
                    if (ds != null)
                        sme.hasDataSpecification = ds;

                    // and also special attributes for each adequate type
                    if (sme is AdminShell.Property p)
                    {
                        var value = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Property_Value);
                        var valueAttr = FindAttributeByRefSemantic(ie.Attribute, AmlConst.Attributes.Property_Value);
                        var valueId = FindAttributeValueByRefSemantic(
                            ie.Attribute, AmlConst.Attributes.Property_ValueId);

                        p.value = value;
                        if (valueId != null)
                            p.valueId = ParseAmlReference(valueId);
                        if (valueAttr != null)
                            p.valueType = ParseAmlDataType(valueAttr.AttributeDataType);
                    }

                    if (sme is AdminShell.MultiLanguageProperty mlp)
                    {
                        var value = TryParseDescriptionFromAttributes(
                            ie.Attribute, AmlConst.Attributes.MultiLanguageProperty_Value);
                        var valueId = FindAttributeValueByRefSemantic(
                            ie.Attribute, AmlConst.Attributes.MultiLanguageProperty_ValueId);

                        if (value.langString != null)
                            mlp.value = new AdminShell.LangStringSet(value.langString);
                        if (valueId != null)
                            mlp.valueId = ParseAmlReference(valueId);
                    }

                    if (sme is AdminShell.Blob smeb)
                    {
                        var mimeType = FindAttributeValueByRefSemantic(
                            ie.Attribute, AmlConst.Attributes.Blob_MimeType);
                        var value = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.Blob_Value);
                        if (mimeType != null)
                            smeb.mimeType = mimeType;
                        if (value != null)
                            smeb.value = value;
                    }

                    if (sme is AdminShell.File smef)
                    {
                        var mimeType = FindAttributeValueByRefSemantic(
                            ie.Attribute, AmlConst.Attributes.File_MimeType);
                        var value = FindAttributeValueByRefSemantic(ie.Attribute, AmlConst.Attributes.File_Value);
                        if (mimeType != null)
                            smef.mimeType = mimeType;
                        if (value != null)
                            smef.value = value;
                    }

                    if (sme is AdminShell.ReferenceElement smer)
                    {
                        if (aasStyleAttributes)
                        {
                            // not used anymore!
                            var value = FindAttributeValueByRefSemantic(
                                ie.Attribute, AmlConst.Attributes.ReferenceElement_Value);
                            if (value != null)
                                smer.value = ParseAmlReference(value);
                        }

                        if (amlStyleAttributes)
                        {
                            // now the default
                            TryPopulateReferenceAttribute(
                                ie, "ReferableReference", AmlConst.Interfaces.ReferableReference, smer, 1);
                        }
                    }

                    // will also include AnnotatedRelationship !!
                    if (sme is AdminShell.RelationshipElement smere)
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
                                smere.first = ParseAmlReference(first);
                                smere.second = ParseAmlReference(second);
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

                    if (sme is AdminShell.Entity ent)
                    {
                        var entityType = FindAttributeValueByRefSemantic(
                            ie.Attribute, AmlConst.Attributes.Entity_entityType);
                        if (entityType != null)
                            ent.entityType = entityType;

                        var assetRef = FindAttributeValueByRefSemantic(
                                ie.Attribute, AmlConst.Attributes.Entity_asset);
                        if (assetRef != null)
                            ent.assetRef = new AdminShell.AssetRef(ParseAmlReference(assetRef));
                    }

                    // ok
                    return sme;
                }
                else
                    // uups!
                    return null;
            }

            private AdminShell.ConceptDescription TryParseConceptDescription(AttributeSequence aseq)
            {
                // begin new (temporary) object
                var cd = new AdminShell.ConceptDescription();

                // gather important attributes
                var idShort = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.Referable_IdShort);
                var idType = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.Identification_idType);
                var id = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.Identification_id);
                var version = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.Administration_Version);
                var revision = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.Administration_Revision);
                var cat = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.Referable_Category);
                var desc = TryParseDescriptionFromAttributes(aseq, AmlConst.Attributes.Referable_Description);

                // we need to have some important information (only Referable name, shoud be always there..)
                if (idType != null && id != null)
                {
                    // set normal data
                    cd.idShort = idShort;
                    cd.identification = new AdminShell.Identification(idType, id);
                    if (version != null && revision != null)
                        cd.administration = new AdminShell.Administration(version, revision);
                    if (desc != null)
                        cd.description = desc;
                    if (cat != null)
                        cd.category = cat;

                    // special data
                    cd.IsCaseOf = TryParseListItemsFromAttributes<AdminShell.Reference>(
                        aseq, AmlConst.Attributes.CD_IsCaseOf, (s) => { return ParseAmlReference(s); });

                    // result
                    return cd;
                }
                else
                    // uups!
                    return null;
            }

            private AdminShell.DataSpecificationIEC61360 TryParseDataSpecificationContentIEC61360(
                AttributeSequence aseq)
            {
                // finally, create the entity
                var ds = new AdminShell.DataSpecificationIEC61360();

                // populate
                var pn = TryParseListOfLangStrFromAttributes(aseq, AmlConst.Attributes.CD_DSC61360_PreferredName);
                if (pn != null)
                    ds.preferredName = AdminShell.LangStringSetIEC61360.CreateFrom(pn);

                var sn = TryParseListOfLangStrFromAttributes(aseq, AmlConst.Attributes.CD_DSC61360_ShortName);
                if (sn != null)
                    ds.shortName = AdminShell.LangStringSetIEC61360.CreateFrom(sn);

                ds.unit = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.CD_DSC61360_Unit);

                ds.unitId = AdminShell.UnitId.CreateNew(
                    ParseAmlReference(FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.CD_DSC61360_UnitId)));

                ds.valueFormat = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.CD_DSC61360_ValueFormat);

                ds.sourceOfDefinition = FindAttributeValueByRefSemantic(
                    aseq, AmlConst.Attributes.CD_DSC61360_SourceOfDefinition);

                ds.symbol = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.CD_DSC61360_Symbol);
                ds.dataType = FindAttributeValueByRefSemantic(aseq, AmlConst.Attributes.CD_DSC61360_DataType);

                var def = TryParseListOfLangStrFromAttributes(aseq, AmlConst.Attributes.CD_DSC61360_Definition);
                if (def != null)
                    ds.definition = AdminShell.LangStringSetIEC61360.CreateFrom(def);

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
                AdminShell.AdministrationShell currentAas = null,
                AdminShell.Submodel currentSubmodel = null,
                AdminShell.Referable currentSmeCollection = null,
                AdminShell.Operation currentOperation = null,
                int currentOperationDir = -1,
                int indentation = 0)
            {
                if (ieseq == null)
                    return;
                foreach (var ie in ieseq)
                {
                    // start
                    Debug(indentation, "Consulting IE name {0}", ie.Name);

                    if (ie.Name == "OperatingManual")
                        ;

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
                            this.package.AasEnv.AdministrationShells.Add(aas);
                            currentAas = aas;
                            matcher.AddMatch(aas, ie);
                        }
                        else
                            Debug(indentation, "  AAS with insufficient attributes. Skipping");
                    }

                    //
                    // Asset
                    //
                    if (currentAas != null && CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.Asset))
                    {
                        // begin new (temporary) object
                        var asset = TryParseAssetFromIe(ie);
                        if (asset != null)
                        {
                            Debug(indentation, "  ASSET with required attributes recognised. Starting new Asset..");

                            // make temporary object official
                            this.package.AasEnv.Assets.Add(asset);
                            currentAas.assetRef = asset.GetAssetReference();
                            matcher.AddMatch(asset, ie);
                        }
                        else
                            Debug(indentation, "  ASSET with insufficient attributes. Skipping");
                    }

                    //
                    // View
                    //
                    if (currentAas != null && CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.View))
                    {
                        // begin new (temporary) object
                        var view = TryParseViewFromIe(insthier, ie);
                        if (view != null)
                        {
                            Debug(indentation, "  VIEW with required attributes recognised. Collecting references..");

                            // make temporary object official
                            currentAas.AddView(view);
                            matcher.AddMatch(view, ie);
                        }
                        else
                            Debug(indentation, "  VIEW with insufficient attributes. Skipping");
                    }

                    //
                    // Submodel
                    //
                    if (currentAas != null && CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.Submodel))
                    {
                        // begin new (temporary) object
                        var sm = TryParseSubmodelFromIe(ie);
                        if (sm != null && (sm.kind == null || sm.kind.IsInstance))
                        {
                            Debug(
                                indentation,
                                "  SUBMODEL with required attributes recognised. Starting new Submodel..");

                            // there might be the case, that a submodel with the same identification is already
                            // existing.
                            // If so, that switch to it and ignore the newly parsed set of information
                            // (TODO: check, if to merge information?)
                            var existSm = this.package.AasEnv.FindSubmodel(sm.identification);
                            if (existSm != null)
                                sm = existSm;

                            // make temporary object official
                            currentSubmodel = sm;
                            matcher.AddMatch(sm, ie);

                            // this will be the parent for child elements
                            // Remark: add only, if not a SM with the same ID is existing. This could have the
                            // consequences that additional properties in the 2nd SM with the same SM get lost!
                            if (null == this.package.AasEnv.FindSubmodel(sm.identification))
                                this.package.AasEnv.Submodels.Add(sm);
                            currentAas.AddSubmodelRef(sm.GetReference() as AdminShell.SubmodelRef);
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
                            var existSm = package.AasEnv.FindSubmodel(targetSm.identification);

                            // if so, add a SubmodelRef
                            currentAas.AddSubmodelRef(existSm.GetReference() as AdminShell.SubmodelRef);
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
                        var ent = new AdminShell.Entity();
                        ent = TryPopulateSubmodelElement(ie, ent) as AdminShell.Entity;
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
                        var ent = new AdminShell.AnnotatedRelationshipElement();
                        ent = TryPopulateSubmodelElement(ie, ent) as AdminShell.AnnotatedRelationshipElement;
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

                                var p = new AdminShell.Property()
                                {
                                    idShort = a.Name,
                                    value = a.Value,
                                    valueType = ParseAmlDataType(a.AttributeDataType),
                                    qualifiers = TryParseQualifiersFromAttributes(a.Attribute),
                                    hasDataSpecification = TryParseDataSpecificationFromAttributes(ie.Attribute)
                                };

                                // gather information
                                var semid = FindAttributeValueByRefSemantic(
                                    a.Attribute, AmlConst.Attributes.SemanticId);
                                if (semid != null)
                                    p.semanticId = AdminShell.SemanticId.CreateFromKeys(
                                        ParseAmlReference(semid)?.Keys);

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
                    // Note 18 OCT 2020 (MIHO): I presume, that SMC shall be excluded from th search, hence
                    // do another kind of comparison
                    for (int i = 0; i < AdminShell.SubmodelElementWrapper.AdequateElementNames.Length; i++)
                    {
                        // access
                        var aen = AdminShell.SubmodelElementWrapper.AdequateElementNames[i];
                        var ae = AdminShell.SubmodelElementWrapper.GetAdequateEnum(aen);
                        if (ae == AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown
                            || ae == AdminShell.SubmodelElementWrapper.AdequateElementEnum.SubmodelElementCollection
                            || ae == AdminShell.SubmodelElementWrapper.AdequateElementEnum.Entity
                            || ae == 
                               AdminShell.SubmodelElementWrapper.AdequateElementEnum.AnnotatedRelationshipElement)
                            continue;

                        if (CheckForRoleClassOrRoleRequirements(ie, AmlConst.Roles.SubmodelElement_Header + aen))
                        {
                            // begin new (temporary) object
                            var sme = AdminShell.SubmodelElementWrapper.CreateAdequateType(ae);
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
                                    if (sme is AdminShell.Operation)
                                    {
                                        currentOperation = sme as AdminShell.Operation;
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
                                    var wrapper = new AdminShell.SubmodelElementWrapper();
                                    wrapper.submodelElement = sme;
                                    var opv = new AdminShell.OperationVariable()
                                    {
                                        value = wrapper
                                    };
                                    currentOperation[currentOperationDir].Add(opv);
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
                AdminShell.AdministrationShell currentAas = null,
                AdminShell.Submodel currentSubmodel = null,
                AdminShell.Referable currentSmeCollection = null,
                AdminShell.Operation currentOperation = null,
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
                            var existAas = this.package.AasEnv.FindAAS(aas.identification);
                            if (existAas != null)
                                aas = existAas;
                            else
                                // add
                                this.package.AasEnv.AdministrationShells.Add(aas);

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
                        if (sm != null && sm.kind != null && sm.kind.IsTemplate)
                        {
                            Debug(
                                indentation,
                                "  SUBMODEL with required attributes recognised. Starting new Submodel..");

                            // there might be the case, that a submodel with the same identification already exists.
                            // If so, that switch to it and ignore the newly parsed set of information
                            // (TODO: check, if to merge information?)
                            var existSm = this.package.AasEnv.FindSubmodel(sm.identification);
                            if (existSm != null)
                                sm = existSm;

                            // make temporary object official
                            currentSubmodel = sm;

                            // this will be the parent for child elements
                            this.package.AasEnv.Submodels.Add(sm);
                            currentAas.AddSubmodelRef(sm.GetReference() as AdminShell.SubmodelRef);
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
                            Debug(indentation, " .. added as {0}", cd.identification.ToString());
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
                                            // embedded data spec for the SDK
                                            var eds = new AdminShell.EmbeddedDataSpecification();
                                            cd.IEC61360DataSpec = eds;

                                            /*
                                             TODO (Michael Hoffmeister, 2020-08-01): fill out 
                                             eds.hasDataSpecification by using outer attributes
                                            */
                                            var hds = FindAttributeValueByRefSemantic(
                                                ie.Attribute, AmlConst.Attributes.CD_DataSpecificationRef);
                                            if (hds != null)
                                                eds.dataSpecification = AdminShell.DataSpecificationRef.CreateNew(
                                                    ParseAmlReference(hds));

                                            // make 61360 data
                                            eds.dataSpecificationContent = new AdminShell.DataSpecificationContent()
                                            {
                                                dataSpecificationIEC61360 = ds61360
                                            };
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
                    if (ieViewAmlTarget.Ie == null || ieViewAmlTarget.View == null || ieViewAmlTarget.AmlTarget == null)
                        continue;

                    // we need to identify the target with respect to the AAS
                    var aasTarget = matcher.GetAasObject(ieViewAmlTarget.AmlTarget);
                    if (aasTarget == null)
                        continue;

                    // get a "real" reference of this
                    var theref = new AdminShell.Reference();
                    aasTarget.CollectReferencesByParent(theref.Keys);

                    // add
                    ieViewAmlTarget.View.AddContainedElement(theref);
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
