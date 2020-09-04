using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public static class AmlExport
    {
        private class AmlInternalLinkEntity
        {
            public string Name = "InternalLink";
            public ExternalInterfaceType extIf = null;

            public AdminShell.Referable sideA = null;
            public AdminShell.Referable sideB = null;

            public string ifClassA = null;
            public string ifClassB = null;

            public Action<InternalElementType> lambdaForSideB = null;

            public AmlInternalLinkEntity(
                ExternalInterfaceType extIf, AdminShell.Referable sideA, AdminShell.Referable sideB,
                string ifClassA, string ifClassB, Action<InternalElementType> lambdaForSideB = null)
            {
                this.extIf = extIf;
                this.sideA = sideA;
                this.sideB = sideB;
                this.ifClassA = ifClassA;
                this.ifClassB = ifClassB;
                this.lambdaForSideB = lambdaForSideB;
            }
        }

        private static InternalElementType AppendIeNameAndRole(
            InternalElementSequence ieseq, string name, string altName = "Unknown", string role = null)
        {
            if (ieseq == null)
                return null;
            var ie = ieseq.Append(
                name != null && name.Trim() != "" ? name : altName + " " + Guid.NewGuid().ToString());
            if (role != null)
            {
                var rr = ie.RoleRequirements.Append();
                rr.RefBaseRoleClassPath = role;
            }
            return ie;
        }

        private static AttributeType AppendAttributeNameAndRole(
            AttributeSequence aseq, string name, string role = null, string val = null,
            string attributeDataType = null)
        {
            if (aseq == null)
                return null;
            var a = aseq.Append(name);
            if (role != null)
            {
                var rf = a.RefSemantic.Append();
                rf.CorrespondingAttributePath = role;
            }
            if (val != null)
            {
                a.Value = val;
            }
            if (attributeDataType != null)
            {
                a.AttributeDataType = attributeDataType;
            }

            return a;
        }

        private static void SetIdentification(AttributeSequence aseq, AdminShell.Identification id)
        {
            if (id == null)
                return;
            var a0 = AppendAttributeNameAndRole(aseq, "identification", AmlConst.Attributes.Identification);
            AppendAttributeNameAndRole(a0.Attribute, "idType", AmlConst.Attributes.Identification_idType, id.idType);
            AppendAttributeNameAndRole(a0.Attribute, "id", AmlConst.Attributes.Identification_id, id.id);
        }

        private static void SetAdministration(AttributeSequence aseq, AdminShell.Administration adm)
        {
            if (adm == null)
                return;
            var a0 = AppendAttributeNameAndRole(aseq, "administration", AmlConst.Attributes.Administration);
            AppendAttributeNameAndRole(
                a0.Attribute, "version", AmlConst.Attributes.Administration_Version, adm.version);
            AppendAttributeNameAndRole(
                a0.Attribute, "revision", AmlConst.Attributes.Administration_Revision, adm.revision);
        }

        private static void SetLangStr(
            AttributeSequence aseq, List<AdminShell.LangStr> langString, string aname, string role)
        {
            if (aseq == null || langString == null || langString.Count < 1)
                return;


            var a0 = AppendAttributeNameAndRole(aseq, aname, role, langString[0].str);
            foreach (var ls in langString)
            {
                if (ls.lang.Trim().ToLower() == "default")
                    continue;
                AppendAttributeNameAndRole(
                    a0.Attribute, AmlConst.Names.AmlLanguageHeader + ls.lang, role: null, val: ls.str);
            }

        }

        private static void SetReferable(AttributeSequence aseq, AdminShell.Referable rf)
        {
            if (aseq == null || rf == null)
                return;
            if (rf.idShort != null)
                AppendAttributeNameAndRole(aseq, "idShort", AmlConst.Attributes.Referable_IdShort, rf.idShort);
            if (rf.category != null)
                AppendAttributeNameAndRole(aseq, "category", AmlConst.Attributes.Referable_Category, rf.category);
            SetLangStr(aseq, rf.description?.langString, "description", AmlConst.Attributes.Referable_Description);
        }

        private static void SetAssetKind(
            AttributeSequence aseq, AdminShell.AssetKind kind, string attributeRole = null)
        {
            if (aseq == null || kind == null || kind.kind == null)
                return;
            if (attributeRole == null)
                attributeRole = AmlConst.Attributes.HasKind_Kind;
            AppendAttributeNameAndRole(aseq, "kind", attributeRole, kind.kind);
        }

        private static void SetModelingKind(
            AttributeSequence aseq, AdminShell.ModelingKind kind, string attributeRole = null)
        {
            if (aseq == null || kind == null || kind.kind == null)
                return;
            if (attributeRole == null)
                attributeRole = AmlConst.Attributes.HasKind_Kind;
            AppendAttributeNameAndRole(aseq, "kind", attributeRole, kind.kind);
        }

        private static string ToAmlName(string input)
        {
            var clean = Regex.Replace(input, @"[^a-zA-Z0-9\-_]", "_");
            while (true)
            {
                var len0 = clean.Length;
                clean = clean.Replace("__", "_");
                if (len0 == clean.Length)
                    break;
            }
            return clean;
        }

        private static string ToAmlSemanticId(AdminShell.SemanticId semid)
        {
            if (semid == null || semid.IsEmpty)
                return null;

            var semstr = "";
            foreach (var k in semid.Keys)
                semstr += String.Format(
                    "({0})({1})[{2}]{3}", k.type, k.local ? "local" : "no-local", k.idType, k.value);

            return semstr;
        }

        private static string ToAmlReference(AdminShell.Reference refid)
        {
            if (refid == null || refid.IsEmpty)
                return null;

            var semstr = "";
            foreach (var k in refid.Keys)
            {
                if (semstr != "")
                    semstr += ",";
                semstr += String.Format(
                    "({0})({1})[{2}]{3}", k.type, k.local ? "local" : "no-local", k.idType, k.value);
            }

            return semstr;
        }

        private static void SetSemanticId(AttributeSequence aseq, AdminShell.SemanticId semid)
        {
            if (aseq == null || semid == null || semid.IsEmpty)
                return;


            AppendAttributeNameAndRole(aseq, "semanticId", AmlConst.Attributes.SemanticId, ToAmlSemanticId(semid));
        }

        private static void SetHasDataSpecification(AttributeSequence aseq, AdminShell.HasDataSpecification ds)
        {
            if (aseq == null || ds == null || ds.Count < 1)
                return;
            foreach (var r in ds)
                AppendAttributeNameAndRole(
                    aseq, "dataSpecification", AmlConst.Attributes.DataSpecificationRef,
                    ToAmlReference(r?.dataSpecification));
        }

        private static void SetQualifiers(
            InternalElementSequence parentIeSeq, AttributeSequence parentAttrSeq,
            List<AdminShell.Qualifier> qualifiers, bool parentAsInternalElements = false)
        {
            if ((parentIeSeq == null && parentAttrSeq == null) || qualifiers == null || qualifiers.Count < 1)
                return;

            foreach (var q in qualifiers)
            {
                // aml-stlye name
                var qid = AmlConst.Names.AmlQualifierHeader + (q.type?.Trim() ?? "qualifier");
                if (q.value != null)
                    qid += "=" + q.value.Trim();
                else if (q.valueId != null)
                    qid += "=" + ToAmlReference(q.valueId);

                AttributeSequence qas = null;

                if (parentAsInternalElements && parentIeSeq != null)
                {
                    // choose IE as well
                    var qie = AppendIeNameAndRole(
                        parentIeSeq, name: q.type, altName: "Qualifier", role: AmlConst.Roles.Qualifer);
                    qas = qie.Attribute;
                }
                else
                {
                    var a = AppendAttributeNameAndRole(parentAttrSeq, qid, AmlConst.Attributes.Qualifer);
                    qas = a.Attribute;
                }

                if (q.semanticId != null)
                    AppendAttributeNameAndRole(
                        qas, "semanticId", AmlConst.Attributes.SemanticId, ToAmlSemanticId(q.semanticId));
                if (q.type != null)
                    AppendAttributeNameAndRole(qas, "type", AmlConst.Attributes.Qualifer_Type, q.type);
                if (q.value != null)
                    AppendAttributeNameAndRole(qas, "value", AmlConst.Attributes.Qualifer_Value, q.value);
                if (q.valueId != null)
                    AppendAttributeNameAndRole(
                        qas, "valueId", AmlConst.Attributes.Qualifer_ValueId, ToAmlReference(q.valueId));
            }
        }

        private static void ExportListOfSme(
            AasAmlMatcher matcher, List<AmlInternalLinkEntity> internalLinksToCreate,
            SystemUnitClassType parent, AdminShell.AdministrationShellEnv env,
            List<AdminShell.SubmodelElementWrapper> wrappers, bool tryUseCompactProperties = false,
            bool aasStyleAttributes = false, bool amlStyleAttributes = true)
        {
            if (parent == null || env == null || wrappers == null)
                return;

            foreach (var smw in wrappers)
            {
                // access
                var sme = smw.submodelElement;
                var smep = sme as AdminShell.Property;
                if (sme == null)
                    continue;

                // device if compact or not ..
                if (tryUseCompactProperties && smep != null && smep.value != null && smep.valueId == null)
                {
                    //
                    // Property as compact attribute
                    //

                    // value itself as Property with idShort
                    var a = AppendAttributeNameAndRole(
                        parent.Attribute, smep.idShort, AmlConst.Attributes.SME_Property, smep.value);

                    // here is no equivalence to set a match!! (MIHO deleted the **to**do** here)

                    // add some data underneath
                    SetReferable(a.Attribute, sme);
                    SetModelingKind(a.Attribute, sme.kind);
                    SetSemanticId(a.Attribute, sme.semanticId);
                    SetHasDataSpecification(a.Attribute, sme.hasDataSpecification);

                    // Property specific
                    if (smep.valueType != null)
                        a.AttributeDataType = "xs:" + smep.valueType.Trim();

                    // Qualifiers
                    SetQualifiers(null, a.Attribute, sme.qualifiers, parentAsInternalElements: false);
                }
                else
                {
                    //
                    // SubmodelElement as self-standing internal element
                    //

                    // make an InternalElement
                    var ie = AppendIeNameAndRole(
                        parent.InternalElement, name: sme.idShort, altName: sme.GetElementName(),
                        role: AmlConst.Roles.SubmodelElement_Header + sme.GetElementName());
                    matcher.AddMatch(sme, ie);

                    // set some data
                    SetReferable(ie.Attribute, sme);
                    SetModelingKind(ie.Attribute, sme.kind);
                    SetSemanticId(ie.Attribute, sme.semanticId);
                    SetHasDataSpecification(ie.Attribute, sme.hasDataSpecification);

                    // depends on type
                    if (smep != null)
                    {
                        if (smep.value != null)
                        {
                            var a = AppendAttributeNameAndRole(
                                ie.Attribute, "value", AmlConst.Attributes.Property_Value, smep.value);
                            if (smep.valueType != null)
                                a.AttributeDataType = "xs:" + smep.valueType.Trim();
                        }
                        if (smep.valueId != null)
                            AppendAttributeNameAndRole(
                                ie.Attribute, "valueId", AmlConst.Attributes.Property_ValueId,
                                ToAmlReference(smep.valueId));
                    }

                    {
                        if (sme is AdminShell.Blob smeb)
                        {
                            if (smeb.mimeType != null)
                                AppendAttributeNameAndRole(
                                    ie.Attribute, "mimeType", AmlConst.Attributes.Blob_MimeType, smeb.mimeType);
                            if (smeb.value != null)
                            {
                                var a = AppendAttributeNameAndRole(
                                    ie.Attribute, "value", AmlConst.Attributes.Blob_Value, smeb.value);
                                a.AttributeDataType = "xs:string";
                            }
                        }
                    }
                    {
                        if (sme is AdminShell.File smef)
                        {
                            if (aasStyleAttributes)
                            {
                                if (smef.mimeType != null)
                                    AppendAttributeNameAndRole(
                                        ie.Attribute, "mimeType", AmlConst.Attributes.File_MimeType, smef.mimeType);
                                if (smef.value != null)
                                {
                                    AppendAttributeNameAndRole(
                                        ie.Attribute, "value", AmlConst.Attributes.File_Value, smef.value);
                                }
                            }

                            if (amlStyleAttributes)
                            {
                                var extIf = ie.ExternalInterface.Append("FileDataReference");
                                if (extIf != null)
                                {
                                    extIf.RefBaseClassPath = AmlConst.Interfaces.FileDataReference;
                                    if (smef.mimeType != null)
                                        AppendAttributeNameAndRole(
                                            extIf.Attribute, "MIMEType", role: null, val: smef.mimeType,
                                            attributeDataType: "xs:string");
                                    if (smef.value != null)
                                    {
                                        AppendAttributeNameAndRole(
                                            extIf.Attribute, "refURI", role: null, val: smef.value,
                                            attributeDataType: "xs:anyURI");
                                    }
                                }
                            }
                        }
                    }
                    {
                        if (sme is AdminShell.ReferenceElement smer)
                        {
                            if (aasStyleAttributes)
                            {
                                if (smer.value != null)
                                {
                                    AppendAttributeNameAndRole(
                                        ie.Attribute, "value", AmlConst.Attributes.ReferenceElement_Value,
                                        ToAmlReference(smer.value));
                                }
                            }

                            if (amlStyleAttributes)
                            {
                                var extIf = ie.ExternalInterface.Append("ReferableReference");
                                if (extIf != null)
                                {
                                    // set the internal interface by class
                                    extIf.RefBaseClassPath = AmlConst.Interfaces.ReferableReference;

                                    // serialize Reference as string in AAS style
                                    if (smer.value != null)
                                    {
                                        AppendAttributeNameAndRole(
                                            extIf.Attribute, "value", AmlConst.Attributes.ReferenceElement_Value,
                                            ToAmlReference(smer.value));
                                    }

                                    // try find the referenced element as Referable in the AAS environment
                                    var targetReferable = env.FindReferableByReference(smer.value);
                                    if (targetReferable != null && internalLinksToCreate != null)
                                    {
                                        internalLinksToCreate.Add(
                                            new AmlInternalLinkEntity(extIf, smer, targetReferable,
                                            "ReferableReference", "ReferableReference",
                                            (x) =>
                                            {
                                                if (x != null)
                                                {
                                                    var x2 = x.ExternalInterface.Append("ReferableReference");
                                                    if (x2 != null)
                                                    {
                                                        x2.RefBaseClassPath = AmlConst.Interfaces.ReferableReference;
                                                    }
                                                }
                                            }));
                                    }
                                }
                            }
                        }
                    }
                    {
                        if (sme is AdminShell.RelationshipElement smer)
                        {
                            if (aasStyleAttributes)
                            {
                                if (smer.first != null && smer.second != null)
                                {
                                    AppendAttributeNameAndRole(
                                        ie.Attribute, "first", AmlConst.Attributes.RelationshipElement_First,
                                        ToAmlReference(smer.first));
                                    AppendAttributeNameAndRole(
                                        ie.Attribute, "second", AmlConst.Attributes.RelationshipElement_Second,
                                        ToAmlReference(smer.second));
                                }
                            }

                            if (amlStyleAttributes)
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    // get first, second
                                    var theRef = (new[] { smer.first, smer.second })[i];
                                    var theName = (new[] { "first", "second" })[i];

                                    if (theRef != null)
                                    {
                                        var extIf = ie.ExternalInterface.Append(theName);
                                        if (extIf != null)
                                        {
                                            // set the internal interface by class
                                            extIf.RefBaseClassPath = AmlConst.Interfaces.ReferableReference;

                                            // serialize Reference as string in AAS style
                                            AppendAttributeNameAndRole(
                                                extIf.Attribute, "value", AmlConst.Attributes.ReferenceElement_Value,
                                                ToAmlReference(theRef));

                                            // try find the referenced element as Referable in the AAS environment
                                            var targetReferable = env.FindReferableByReference(theRef);
                                            if (targetReferable != null && internalLinksToCreate != null)
                                            {
                                                internalLinksToCreate.Add(
                                                    new AmlInternalLinkEntity(extIf, smer, targetReferable, theName,
                                                    "ReferableReference",
                                                    (x) =>
                                                    {
                                                        if (x != null)
                                                        {
                                                            var x2 = x.ExternalInterface.Append("ReferableReference");
                                                            if (x2 != null)
                                                            {
                                                                x2.RefBaseClassPath =
                                                                    AmlConst.Interfaces.ReferableReference;
                                                            }
                                                        }
                                                    }));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
                        if (sme is AdminShell.SubmodelElementCollection smec)
                        {
                            // recurse
                            AppendAttributeNameAndRole(
                                ie.Attribute, "ordered", AmlConst.Attributes.SMEC_ordered,
                                smec.ordered ? "true" : "false", attributeDataType: "xs:boolean");
                            AppendAttributeNameAndRole(
                                ie.Attribute, "allowDuplicates", AmlConst.Attributes.SMEC_allowDuplicates,
                                smec.allowDuplicates ? "true" : "false", attributeDataType: "xs:boolean");

                            ExportListOfSme(matcher, internalLinksToCreate, ie, env, smec.value);
                        }
                    }
                    {
                        if (sme is AdminShell.Operation op)
                        {
                            // over in, out
                            for (int i = 0; i < 2; i++)
                            {
                                // internal element
                                var oie = AppendIeNameAndRole(
                                    ie.InternalElement,
                                    (new[] { "InputVariables", "OutputVariables" })[i],
                                    "",
                                    role: (
                                        new[] {
                                            AmlConst.Roles.OperationVariableIn,
                                            AmlConst.Roles.OperationVariableOut
                                        })[i]);

                                // just a list of SMEs
                                var lop = new List<AdminShell.SubmodelElementWrapper>();
                                foreach (var oe in op[i])
                                    lop.Add(oe.value);
                                ExportListOfSme(matcher, internalLinksToCreate, oie, env, lop);
                            }
                        }
                    }

                    // Qualifiers
                    SetQualifiers(ie.InternalElement, ie.Attribute, sme.qualifiers, parentAsInternalElements: false);
                }
            }
        }

        private static void ExportSubmodelIntoElement(
            AasAmlMatcher matcher, List<AmlInternalLinkEntity> internalLinksToCreate,
            SystemUnitClassType parent,
            AdminShell.AdministrationShellEnv env,
            AdminShell.Submodel sm,
            bool tryUseCompactProperties = false,
            bool exportShallow = false)
        {
            if (parent == null || env == null || sm == null)
                return;

            // set some data
            SetIdentification(parent.Attribute, sm.identification);
            SetAdministration(parent.Attribute, sm.administration);
            SetReferable(parent.Attribute, sm);
            SetModelingKind(parent.Attribute, sm.kind);
            SetSemanticId(parent.Attribute, sm.semanticId);
            SetHasDataSpecification(parent.Attribute, sm.hasDataSpecification);
            SetQualifiers(null, parent.Attribute, sm.qualifiers, parentAsInternalElements: false);

            // properties
            if (!exportShallow)
                ExportListOfSme(
                    matcher, internalLinksToCreate, parent, env, sm.submodelElements, tryUseCompactProperties);
            else
            {
                // add a small information
                AppendIeNameAndRole(parent.InternalElement, name: "Remark: duplicate AAS:Submodel");
            }
        }

        private static InternalElementType ExportSubmodel(
            AasAmlMatcher matcher,
            List<AmlInternalLinkEntity> internalLinksToCreate, InternalElementSequence ieseq,
            AdminShell.AdministrationShellEnv env,
            AdminShell.Submodel sm,
            bool tryUseCompactProperties = false,
            bool exportShallow = false)
        {
            if (ieseq == null || env == null || sm == null)
                return null;

            // directly add internal element
            var ie = AppendIeNameAndRole(ieseq, name: sm.idShort, altName: "Submodel", role: AmlConst.Roles.Submodel);


            ExportSubmodelIntoElement(
                matcher, internalLinksToCreate, ie, env, sm, tryUseCompactProperties, exportShallow);

            // return IE
            return ie;
        }

        private static void ExportAsset(
            InternalElementSequence ieseq, AdminShell.AdministrationShellEnv env, AdminShell.Asset asset)
        {
            if (ieseq == null || env == null || asset == null)
                return;

            // directly add internal element
            var ie = AppendIeNameAndRole(ieseq, name: asset.idShort, altName: "Asset", role: AmlConst.Roles.Asset);

            // set some data
            SetIdentification(ie.Attribute, asset.identification);
            SetAdministration(ie.Attribute, asset.administration);
            SetReferable(ie.Attribute, asset);
            SetAssetKind(ie.Attribute, asset.kind, attributeRole: AmlConst.Attributes.Asset_Kind);
            SetHasDataSpecification(ie.Attribute, asset.hasDataSpecification);

            // do some data directly

            if (asset.assetIdentificationModelRef != null)
                AppendAttributeNameAndRole(
                    ie.Attribute, "assetIdentificationModelRef", AmlConst.Attributes.Asset_IdentificationModelRef,
                    ToAmlReference(asset.assetIdentificationModelRef));

            if (asset.billOfMaterialRef != null)
                AppendAttributeNameAndRole(ie.Attribute, "billOfMaterialRef",
                AmlConst.Attributes.Asset_BillOfMaterialRef, ToAmlReference(asset.billOfMaterialRef));
        }

        private static void ExportView(
            AasAmlMatcher matcher, InternalElementSequence ieseq, AdminShell.AdministrationShellEnv env,
            AdminShell.View view)
        {
            if (ieseq == null || env == null || view == null)
                return;

            // directly add internal element
            var ie = AppendIeNameAndRole(ieseq, name: view.idShort, altName: "View", role: AmlConst.Roles.View);

            // set some data
            SetReferable(ie.Attribute, view);
            SetSemanticId(ie.Attribute, view.semanticId);
            SetHasDataSpecification(ie.Attribute, view.hasDataSpecification);

            // view references
            // from the Meeting: Views sind Listen von "Mirror-Elementen",
            // die auf die Properties.. (IEs) der AAS verweisen.
            // Views hängen unter der jeweiligen AAS (Referable)
            for (int i = 0; i < view.Count; i++)
            {
                // access contained element
                var ce = view[i];
                if (ce == null)
                    continue;

                // find the referenced element
                var targetReferable = env.FindReferableByReference(ce);
                if (targetReferable == null)
                    continue;

                // rely on the "hope", that there is a match
                var targetAml = matcher.GetAmlObject(targetReferable);
                if (targetAml == null)
                    continue;

                // for the time being, make an IE
                // Note: it is "forbidden" to set Roles for mirror elements
                var iece = AppendIeNameAndRole(
                    ie.InternalElement, name: ce.ListOfValues("/"), altName: "Reference",
                    role: null);

                // just convert it to an mirror element
                iece.RefBaseSystemUnitPath = "" + targetAml.ID;
            }
        }

        private static void ExportAAS(
            AasAmlMatcher matcher, InstanceHierarchyType insthier, SystemUnitClassLibType suchier,
            AdminShell.AdministrationShellEnv env, AdminShell.AdministrationShell aas,
            bool tryUseCompactProperties = false)
        {
            // access
            if (insthier == null || suchier == null || env == null || aas == null || matcher == null)
                return;

            // for attaching internal links, we need an IE. Therefore, we do the linking for each AAS separately
            // and ingnore intra-AAS linking
            var internalLinksToCreate = new List<AmlInternalLinkEntity>();

            //
            // always create an AAS under InstanceHierarchy as being the conveyor of AAS information
            //

            // directly add internal element
            var aasIE = AppendIeNameAndRole(
                insthier.InternalElement, name: aas.idShort, altName: "AAS", role: AmlConst.Roles.AAS);

            // set some data
            SetIdentification(aasIE.Attribute, aas.identification);
            SetAdministration(aasIE.Attribute, aas.administration);
            SetReferable(aasIE.Attribute, aas);
            SetHasDataSpecification(aasIE.Attribute, aas.hasDataSpecification);

            if (aas.derivedFrom != null)
                AppendAttributeNameAndRole(
                    aasIE.Attribute, "derivedFrom", AmlConst.Attributes.AAS_DerivedFrom,
                    ToAmlReference(aas.derivedFrom));

            // asset
            var asset = env.FindAsset(aas.assetRef);
            ExportAsset(aasIE.InternalElement, env, asset);

            // the AAS for Submodels of kind = Type willbe created on demand
            SystemUnitFamilyType aasSUC = null;

            //
            // Submodels can be of kind Type/ Instance
            //
            foreach (var smref in aas.submodelRefs)
            {
                // ref -> Submodel
                var sm = env.FindSubmodel(smref);
                if (sm == null)
                    continue;

                // SM types go to system unit classes, instances goe to instance hierarchy
                if (sm.kind != null && sm.kind.IsTemplate)
                {
                    // create AAS for SUCs on demand
                    if (aasSUC == null)
                    {
                        // create parent
                        aasSUC = suchier.SystemUnitClass.Append(aas.idShort ?? "AAS_" + Guid.NewGuid().ToString());

                        // role
                        var rr = aasSUC.SupportedRoleClass.Append();
                        rr.RefRoleClassPath = AmlConst.Roles.AAS;
                    }

                    // create a dedicated SUC for this
                    var smSUC = aasSUC.SystemUnitClass.Append(sm.idShort ?? "Submodel_" + Guid.NewGuid().ToString());

                    // role
                    var rq = smSUC.SupportedRoleClass.Append();
                    rq.RefRoleClassPath = AmlConst.Roles.Submodel;

                    // set same data data, in order to correlate, but not asset
                    SetIdentification(aasSUC.Attribute, aas.identification);
                    SetAdministration(aasSUC.Attribute, aas.administration);
                    SetReferable(aasSUC.Attribute, aas);
                    SetHasDataSpecification(aasSUC.Attribute, aas.hasDataSpecification);

                    // use normal function to export Submodel data into the SUC element
                    ExportSubmodelIntoElement(matcher, internalLinksToCreate, smSUC, env, sm, tryUseCompactProperties);

                    // there could be the chance, that the semanticId of a Submodel of kind = Instance links
                    // to a local Submodel of kind = Type
                    // therefore: store in either way
                    matcher.AddMatch(sm, smSUC);
                }
                else
                {
                    // check, if the Submodel is already exported
                    var smAlreadyExported = matcher.ContainsAasObject(sm);

                    if (!smAlreadyExported)
                    {
                        // export the submodel
                        var smIE = ExportSubmodel(
                            matcher, internalLinksToCreate, aasIE.InternalElement, env, sm,
                            tryUseCompactProperties: tryUseCompactProperties, exportShallow: false);

                        // there could be the chance, that the semanticId of a Submodel of kind = Instance links
                        // to a local Submodel of kind = Type
                        // therefore: store in either way
                        matcher.AddMatch(sm, smIE);
                    }
                    else
                    {
                        // try to utilze "Mirror Elements" for duplicate Submodel
                        // for the time being, make an IE
                        // Note: it is "forbidden" to set Roles for mirror elements
                        var ieSubDup = AppendIeNameAndRole(
                            aasIE.InternalElement, name: sm.idShort, altName: "Submodel", role: null);

                        // get AML entity of existing Submodel
                        var targetAml = matcher.GetAmlObject(sm);

                        if (targetAml != null)
                        {
                            // just convert it to an mirror element
                            ieSubDup.RefBaseSystemUnitPath = "" + targetAml.ID;
                        }
                    }
                }

            }

            //
            // Views
            //

            if (aas.views != null && aas.views.views != null)
                foreach (var view in aas.views.views)
                    ExportView(matcher, aasIE.InternalElement, env, view);

            //
            // Internal Links
            //
            SetInternalLinks(aasIE, matcher, internalLinksToCreate);
        }

        /// <summary>
        /// Sets attributes to outer and inner element (optional) of an ConceptDescription.
        /// If aseqInnner == null, "embeddedDataSpec"-approach will be used
        /// </summary>
        private static void SetAttributesForConceptDescription(
            AttributeSequence aseqOuter, AttributeSequence aseqInner, AdminShell.ConceptDescription cd,
            ref string name)
        {
            // check
            if (aseqOuter == null || cd == null)
                return;

            // start to figure out an speaking name for the AML entity (idSHort, shortName .. will be codified
            // as original attributes, as well)
            // the name will be set at the end
            name = "CD_" + Guid.NewGuid().ToString();
            if (cd.idShort.HasContent())
                name = cd.idShort;

            // set data for identifiable
            SetIdentification(aseqOuter, cd.identification);
            SetAdministration(aseqOuter, cd.administration);
            SetReferable(aseqOuter, cd);

            // isCaseOf
            foreach (var r in cd.IsCaseOf)
                AppendAttributeNameAndRole(aseqOuter, "isCaseOf", AmlConst.Attributes.CD_IsCaseOf, ToAmlReference(r));

            // which data spec as reference
            if (cd.embeddedDataSpecification != null)
                foreach (var eds in cd.embeddedDataSpecification)
                    if (eds.dataSpecification != null)
                        AppendAttributeNameAndRole(
                            aseqOuter, "dataSpecification", AmlConst.Attributes.CD_DataSpecificationRef,
                            ToAmlReference(eds.dataSpecification));

            // which data spec to take as source?
            var source61360 = cd.embeddedDataSpecification?.IEC61360Content;
            // TODO (Michael Hoffmeister, 2020-08-01): If further data specifications exist (in future), add here

            // decide which approach to take (1 or 2 IE)
            AttributeSequence dest61360 = null;
            if (aseqInner == null)
            {
                // we will pack the attribute under an embedded data spec attribute branch
                // now, to the embedded data spec
                if (cd.embeddedDataSpecification != null)
                {
                    var eds = AppendAttributeNameAndRole(
                        aseqOuter, "dataSpecification", AmlConst.Attributes.CD_EmbeddedDataSpecification);
                    if (source61360 != null)
                    {
                        var dsc = AppendAttributeNameAndRole(
                            eds.Attribute, "dataSpecificationContent",
                            AmlConst.Attributes.CD_DataSpecificationContent);
                        if (source61360 != null)
                        {
                            // create attribute branch for CD contents
                            var dsc61360 = AppendAttributeNameAndRole(
                                dsc.Attribute, "dataSpecificationIEC61360",
                                AmlConst.Attributes.CD_DataSpecification61360);
                            dest61360 = dsc61360.Attribute;
                        }
                    }
                }
            }
            else
            {
                // we will use an 2nd IE to have the attributes of the data spcec content
                dest61360 = aseqInner;
            }

            // set attributes of 61360
            if (source61360 != null && dest61360 != null)
            {
                // better name?
                if (source61360.shortName != null && source61360.shortName.Count > 0)
                    name = source61360.shortName.GetDefaultStr();

                // specific data
                SetLangStr(
                    dest61360, source61360.preferredName?.langString, "preferredName",
                    AmlConst.Attributes.CD_DSC61360_PreferredName);
                SetLangStr(
                    dest61360, source61360.shortName?.langString, "shortName",
                    AmlConst.Attributes.CD_DSC61360_ShortName);
                if (source61360.unit != null)
                    AppendAttributeNameAndRole(
                        dest61360, "unit", AmlConst.Attributes.CD_DSC61360_Unit, source61360.unit);
                if (source61360.unitId != null)
                    AppendAttributeNameAndRole(
                        dest61360, "unitId", AmlConst.Attributes.CD_DSC61360_UnitId,
                        ToAmlReference(AdminShell.Reference.CreateNew(source61360.unitId.Keys)));
                if (source61360.valueFormat != null)
                    AppendAttributeNameAndRole(
                        dest61360, "valueFormat", AmlConst.Attributes.CD_DSC61360_ValueFormat,
                        source61360.valueFormat);
                if (source61360.sourceOfDefinition != null)
                    AppendAttributeNameAndRole(
                        dest61360, "sourceOfDefinition", AmlConst.Attributes.CD_DSC61360_SourceOfDefinition,
                        source61360.sourceOfDefinition);
                if (source61360.symbol != null)
                    AppendAttributeNameAndRole(
                        dest61360, "symbol", AmlConst.Attributes.CD_DSC61360_Symbol, source61360.symbol);
                if (source61360.dataType != null)
                    AppendAttributeNameAndRole(
                        dest61360, "dataType", AmlConst.Attributes.CD_DSC61360_DataType, source61360.dataType);
                SetLangStr(
                    dest61360, source61360.definition?.langString, "definition",
                    AmlConst.Attributes.CD_DSC61360_Definition);
            }
        }


        private static void ExportConceptDescriptionsWithExtraContentToIHT(
            InstanceHierarchyType lib, AdminShell.AdministrationShellEnv env)
        {
            // acceess
            if (lib == null || env == null)
                return;

            // over CDs
            foreach (var cd in env.ConceptDescriptions)
            {
                // make IE for CD itself (outer IE)
                string name = "TODO-CD";
                var ieCD = AppendIeNameAndRole(
                    lib.InternalElement, name: name, altName: "CD", role: AmlConst.Roles.ConceptDescription);

                // make IE for the data specification content
                var ieDSC = AppendIeNameAndRole(
                    ieCD.InternalElement, name: "EmbeddedDataSpecification", altName: "DSC",
                    role: AmlConst.Roles.DataSpecificationContent);
                ieDSC.RefBaseSystemUnitPath = AmlConst.Classes.DataSpecificationContent61360;

                // use inner function
                SetAttributesForConceptDescription(ieCD.Attribute, ieDSC.Attribute, cd, ref name);

                // set the final name
                name += "_" + ToAmlName(cd.identification.ToString());
                ieCD.Name = name;
            }
        }


        private static void SetMatcherLinks(AasAmlMatcher matcher)
        {
            // access
            if (matcher == null)
                return;

            // look out for Submodels of kind = Instance, which have a semanticId referring
            // to a Submodel kind = Type with that ID
            foreach (var x in matcher.GetAllAasReferables())
            {
                if (x is AdminShell.Submodel smki)
                {
                    if (smki.kind != null && smki.kind.IsInstance && smki.semanticId != null)
                    {
                        foreach (var y in matcher.GetAllAasReferables())
                            if (y is AdminShell.Submodel smkt)
                            {
                                if (smkt.kind != null && smkt.kind.IsTemplate &&
                                    smki.semanticId.Matches(
                                        AdminShell.Key.Submodel, true, smkt.identification.idType,
                                        smkt.identification.id))
                                {
                                    // we have a match: Submodel kind = Instance -> Submodel kind = Type
                                    var smki_aml = matcher.GetAmlObject(smki) as InternalElementType;
                                    var smkt_aml = matcher.GetAmlObject(smkt) as SystemUnitFamilyType;
                                    if (smki_aml != null && smkt_aml != null)
                                    {
                                        smki_aml.SystemUnitClass = smkt_aml;
                                    }
                                }
                            }
                    }
                }
            }
        }

        private static void SetInternalLinks(
            InternalElementType commonIE, AasAmlMatcher matcher, List<AmlInternalLinkEntity> links)
        {
            // access
            if (matcher == null || links == null)
                return;

            // each link individual
            foreach (var link in links)
            {
                // try identify elements in AML
                var amlSideA = matcher.GetAmlObject(link.sideA);
                var amlSideB = matcher.GetAmlObject(link.sideB);

                // link makes only sense, if both sides are present
                if (amlSideA == null || amlSideB == null || amlSideA.ID == null || amlSideB.ID == null)
                    continue;

                // further checks on link
                if (link.extIf == null || link.ifClassA == null || link.ifClassB == null)
                    continue;

                // may be create stuff (external interfaces) on sideB
                if (link.lambdaForSideB != null)
                    link.lambdaForSideB(amlSideB as InternalElementType);

                // try make an internal link
                var il = commonIE.InternalLink.Append(link.Name);
                if (il != null)
                {
                    il.RefPartnerSideA = amlSideA.ID + ":" + link.ifClassA;
                    il.RefPartnerSideB = amlSideB.ID + ":" + link.ifClassB;
                }
            }
        }

        public static bool ExportTo(AdminShellPackageEnv package, string amlfn, bool tryUseCompactProperties = false)
        {
            // start
            if (package == null || amlfn == null || package.AasEnv == null)
                return false;

            // create main doc
            var doc = CAEXDocument.New_CAEXDocument(CAEXDocument.CAEXSchema.CAEX2_15);

            // merge, if availabe, the role classes
            var xstream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "AasxAmlImExport.Resources.AssetAdministrationShellLib.aml");
            if (xstream != null)
            {
                var xdoc = CAEXDocument.LoadFromStream(xstream);

                foreach (var rcl in xdoc.CAEXFile.RoleClassLib)
                {
                    doc.CAEXFile.RoleClassLib.Insert(rcl);
                }

                foreach (var icl in xdoc.CAEXFile.InterfaceClassLib)
                    doc.CAEXFile.InterfaceClassLib.Insert(icl);

                foreach (var suc in xdoc.CAEXFile.SystemUnitClassLib)
                    doc.CAEXFile.SystemUnitClassLib.Insert(suc);
            }

            // create hierarchies
            var insthier = doc.CAEXFile.InstanceHierarchy.Append(AmlConst.Names.RootInstHierarchy);
            var suchier = doc.CAEXFile.SystemUnitClassLib.Append(AmlConst.Names.RootSystemUnitClasses);

            // after establishing the entities, there will be linking
            var matcher = new AasAmlMatcher();

            // over all AAS
            foreach (var aas in package.AasEnv.AdministrationShells)
            {
                ExportAAS(matcher, insthier, suchier, package.AasEnv, aas, tryUseCompactProperties);
            }

            // ConceptDescriptions
            var cdhier = doc.CAEXFile.InstanceHierarchy.Append(AmlConst.Names.RootConceptDescriptions);
            ExportConceptDescriptionsWithExtraContentToIHT(cdhier, package.AasEnv);

            // now, do links
            SetMatcherLinks(matcher);

            // save and return
            doc.SaveToFile(amlfn, prettyPrint: true);
            return true;
        }
    }
}
