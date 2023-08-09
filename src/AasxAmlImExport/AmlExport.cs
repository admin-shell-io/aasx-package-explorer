/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AdminShellNS;
using Aml.Engine.CAEX;
using Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AasxAmlImExport
{
    public static class AmlExport
    {
        private class AmlInternalLinkEntity
        {
            public string Name = "InternalLink";
            public ExternalInterfaceType extIf = null;

            public IReferable sideA = null;
            public IReferable sideB = null;

            public string ifClassA = null;
            public string ifClassB = null;

            public Action<InternalElementType> lambdaForSideB = null;

            public AmlInternalLinkEntity(
                ExternalInterfaceType extIf, IReferable sideA, IReferable sideB,
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

        private static void SetIdentification(AttributeSequence aseq, string id)
        {
            if (id == null)
                return;
            var a0 = AppendAttributeNameAndRole(aseq, "identification", AmlConst.Attributes.Identification);
            AppendAttributeNameAndRole(a0.Attribute, "id", AmlConst.Attributes.Identification_id, id);
        }

        private static void SetAdministration(AttributeSequence aseq, IAdministrativeInformation adm)
        {
            if (adm == null)
                return;
            var a0 = AppendAttributeNameAndRole(aseq, "administration", AmlConst.Attributes.Administration);
            AppendAttributeNameAndRole(
                a0.Attribute, "version", AmlConst.Attributes.Administration_Version, adm.Version);
            AppendAttributeNameAndRole(
                a0.Attribute, "revision", AmlConst.Attributes.Administration_Revision, adm.Revision);
        }

        private static void SetLangStr(
            AttributeSequence aseq, List<ILangStringTextType> langString, string aname, string role)
        {
            if (aseq == null || langString == null || langString.Count < 1)
                return;


            var a0 = AppendAttributeNameAndRole(aseq, aname, role, langString[0].Text);
            foreach (var ls in langString)
            {
                if (ls.Language.Trim().ToLower() == "default")
                    continue;
                AppendAttributeNameAndRole(
                    a0.Attribute, AmlConst.Names.AmlLanguageHeader + ls.Language, role: null, val: ls.Text);
            }

        }

        private static void SetLangStr(
            AttributeSequence aseq, List<ILangStringDefinitionTypeIec61360> langString, string aname, string role)
        {
            if (aseq == null || langString == null || langString.Count < 1)
                return;


            var a0 = AppendAttributeNameAndRole(aseq, aname, role, langString[0].Text);
            foreach (var ls in langString)
            {
                if (ls.Language.Trim().ToLower() == "default")
                    continue;
                AppendAttributeNameAndRole(
                    a0.Attribute, AmlConst.Names.AmlLanguageHeader + ls.Language, role: null, val: ls.Text);
            }

        }

        private static void SetLangStr(
            AttributeSequence aseq, List<ILangStringShortNameTypeIec61360> langString, string aname, string role)
        {
            if (aseq == null || langString == null || langString.Count < 1)
                return;


            var a0 = AppendAttributeNameAndRole(aseq, aname, role, langString[0].Text);
            foreach (var ls in langString)
            {
                if (ls.Language.Trim().ToLower() == "default")
                    continue;
                AppendAttributeNameAndRole(
                    a0.Attribute, AmlConst.Names.AmlLanguageHeader + ls.Language, role: null, val: ls.Text);
            }

        }
        private static void SetLangStr(
            AttributeSequence aseq, List<ILangStringPreferredNameTypeIec61360> langString, string aname, string role)
        {
            if (aseq == null || langString == null || langString.Count < 1)
                return;


            var a0 = AppendAttributeNameAndRole(aseq, aname, role, langString[0].Text);
            foreach (var ls in langString)
            {
                if (ls.Language.Trim().ToLower() == "default")
                    continue;
                AppendAttributeNameAndRole(
                    a0.Attribute, AmlConst.Names.AmlLanguageHeader + ls.Language, role: null, val: ls.Text);
            }

        }

        private static void SetReferable(AttributeSequence aseq, IReferable rf)
        {
            if (aseq == null || rf == null)
                return;
            if (rf.IdShort != null)
                AppendAttributeNameAndRole(aseq, "idShort", AmlConst.Attributes.Referable_IdShort, rf.IdShort);
            if (rf.Category != null)
                AppendAttributeNameAndRole(aseq, "category", AmlConst.Attributes.Referable_Category, rf.Category);
            SetLangStr(aseq, rf.Description, "description", AmlConst.Attributes.Referable_Description);
        }

        private static void SetAssetKind(
            AttributeSequence aseq, AssetKind kind, string attributeRole = null)
        {
            if (aseq == null)
                return;
            if (attributeRole == null)
                attributeRole = AmlConst.Attributes.HasKind_Kind;
            AppendAttributeNameAndRole(aseq, "kind", attributeRole, Stringification.ToString(kind));
        }

        private static void SetModelingKind(
            AttributeSequence aseq, ModellingKind? kind, string attributeRole = null)
        {
            if (aseq == null || !kind.HasValue)
                return;
            if (attributeRole == null)
                attributeRole = AmlConst.Attributes.HasKind_Kind;
            AppendAttributeNameAndRole(aseq, "kind", attributeRole, Stringification.ToString(kind));
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

        private static string ToAmlSemanticId(IReference semid)
        {
            if (semid == null || semid.IsEmpty())
                return null;

            var semstr = "";
            foreach (var k in semid.Keys)
                semstr += String.Format(
                    "({0})({1})", k.Type, k.Value);

            return semstr;
        }

        private static string ToAmlReference(IReference refid)
        {
            if (refid == null || refid.IsEmpty())
                return null;

            var semstr = "";
            foreach (var k in refid.Keys)
            {
                if (semstr != "")
                    semstr += ",";
                semstr += String.Format(
                    "({0})({1})", k.Type, k.Value);
            }

            return semstr;
        }

        private static void SetSemanticId(AttributeSequence aseq, IReference semid)
        {
            if (aseq == null || semid == null || semid.IsEmpty())
                return;


            AppendAttributeNameAndRole(aseq, "semanticId", AmlConst.Attributes.SemanticId, ToAmlSemanticId(semid));
        }

        private static void SetHasDataSpecification(AttributeSequence aseq, List<IEmbeddedDataSpecification> ds)
        {
            if (aseq == null || ds == null || ds.Count < 1)
                return;
            foreach (var r in ds)
                AppendAttributeNameAndRole(
                    aseq, "dataSpecification", AmlConst.Attributes.DataSpecificationRef,
                    ToAmlReference(r.DataSpecification));
        }

        private static void SetQualifiers(
            InternalElementSequence parentIeSeq, AttributeSequence parentAttrSeq,
            List<IQualifier> qualifiers, bool parentAsInternalElements = false)
        {
            if ((parentIeSeq == null && parentAttrSeq == null) || qualifiers == null || qualifiers.Count < 1)
                return;

            foreach (var q in qualifiers)
            {
                // aml-stlye name
                var qid = AmlConst.Names.AmlQualifierHeader + (q.Type?.Trim() ?? "qualifier");
                if (q.Value != null)
                    qid += "=" + q.Value.Trim();
                else if (q.ValueId != null)
                    qid += "=" + ToAmlReference(q.ValueId);

                AttributeSequence qas = null;

                if (parentAsInternalElements && parentIeSeq != null)
                {
                    // choose IE as well
                    var qie = AppendIeNameAndRole(
                        parentIeSeq, name: q.Type, altName: "Qualifier", role: AmlConst.Roles.Qualifer);
                    qas = qie.Attribute;
                }
                else
                {
                    var a = AppendAttributeNameAndRole(parentAttrSeq, qid, AmlConst.Attributes.Qualifer);
                    qas = a.Attribute;
                }

                if (q.SemanticId != null)
                    AppendAttributeNameAndRole(
                        qas, "semanticId", AmlConst.Attributes.SemanticId, ToAmlSemanticId(q.SemanticId));
                if (q.Type != null)
                    AppendAttributeNameAndRole(qas, "type", AmlConst.Attributes.Qualifer_Type, q.Type);
                if (q.Value != null)
                    AppendAttributeNameAndRole(qas, "value", AmlConst.Attributes.Qualifer_Value, q.Value);
                if (q.ValueId != null)
                    AppendAttributeNameAndRole(
                        qas, "valueId", AmlConst.Attributes.Qualifer_ValueId, ToAmlReference(q.ValueId));
            }
        }

        private static void ExportReferenceWithSme(
            AasCore.Aas3_0.Environment env,
            List<AmlInternalLinkEntity> internalLinksToCreate,
            InternalElementType ie,
            IReferable referable,
            IReference refInReferable,
            string attrName,
            string roleName,
            string outgoingLinkName,
            bool aasStyleAttributes = false, bool amlStyleAttributes = true)
        {
            // access
            if (env == null || ie == null || !attrName.HasContent() || !roleName.HasContent()
                || !outgoingLinkName.HasContent())
                return;

            // working mode(s)
            if (aasStyleAttributes)
            {
                if (refInReferable != null)
                {
                    AppendAttributeNameAndRole(ie.Attribute, attrName, roleName, ToAmlReference(refInReferable));
                }
            }

            if (amlStyleAttributes)
            {
                var extIf = ie.ExternalInterface.Append(outgoingLinkName);
                if (extIf != null)
                {
                    // set the internal interface by class
                    extIf.RefBaseClassPath = AmlConst.Interfaces.ReferableReference;

                    // serialize Reference as string in AAS style
                    if (refInReferable != null)
                    {
                        AppendAttributeNameAndRole(ie.Attribute, attrName, roleName, ToAmlReference(refInReferable));
                    }

                    // try find the referenced element as IReferable in the AAS environment
                    var targetReferable = env.FindReferableByReference(refInReferable);
                    if (targetReferable != null && internalLinksToCreate != null)
                    {
                        internalLinksToCreate.Add(
                            new AmlInternalLinkEntity(extIf, referable, targetReferable,
                            outgoingLinkName, "ReferableReference",
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

        private static void ExportListOfSme(
            AasAmlMatcher matcher, List<AmlInternalLinkEntity> internalLinksToCreate,
            SystemUnitClassType parent, AasCore.Aas3_0.Environment env,
            List<ISubmodelElement> wrappers, bool tryUseCompactProperties = false,
            bool aasStyleAttributes = false, bool amlStyleAttributes = true)
        {
            if (parent == null || env == null || wrappers == null)
                return;

            foreach (var smw in wrappers)
            {
                // access
                var sme = smw;
                var smep = sme as Property;
                if (sme == null)
                    continue;

                // device if compact or not ..
                if (tryUseCompactProperties && smep != null && smep.Value != null && smep.ValueId == null)
                {
                    //
                    // Property as compact attribute
                    //

                    // value itself as Property with idShort
                    var a = AppendAttributeNameAndRole(
                        parent.Attribute, smep.IdShort, AmlConst.Attributes.SME_Property, smep.Value);

                    // here is no equivalence to set a match!! (MIHO deleted the **to**do** here)

                    // add some data underneath
                    SetReferable(a.Attribute, sme);
                    SetSemanticId(a.Attribute, sme.SemanticId);
                    SetHasDataSpecification(a.Attribute, sme.EmbeddedDataSpecifications);

                    // Property specific
                    a.AttributeDataType = "xs:" + Stringification.ToString(smep.ValueType).Trim();

                    // Qualifiers
                    SetQualifiers(null, a.Attribute, sme.Qualifiers, parentAsInternalElements: false);
                }
                else
                {
                    //
                    // SubmodelElement as self-standing internal element
                    //

                    // make an InternalElement
                    var ie = AppendIeNameAndRole(
                        parent.InternalElement, name: sme.IdShort, altName: sme.GetType().Name,
                        role: AmlConst.Roles.SubmodelElement_Header + sme.GetType().Name);
                    matcher.AddMatch(sme, ie);

                    // set some data
                    SetReferable(ie.Attribute, sme);
                    SetSemanticId(ie.Attribute, sme.SemanticId);
                    SetHasDataSpecification(ie.Attribute, sme.EmbeddedDataSpecifications);

                    // depends on type
                    if (smep != null)
                    {
                        if (smep.Value != null)
                        {
                            var a = AppendAttributeNameAndRole(
                                ie.Attribute, "value", AmlConst.Attributes.Property_Value, smep.Value);
                            a.AttributeDataType = "xs:" + Stringification.ToString(smep.ValueType).Trim();
                        }
                        if (smep.ValueId != null)
                            AppendAttributeNameAndRole(
                                ie.Attribute, "valueId", AmlConst.Attributes.Property_ValueId,
                                ToAmlReference(smep.ValueId));
                    }

                    switch (sme)
                    {
                        case MultiLanguageProperty mlp:
                            // value
                            if (mlp.Value != null)
                            {
                                SetLangStr(ie.Attribute, mlp.Value, "value",
                                    AmlConst.Attributes.MultiLanguageProperty_Value);
                            }

                            // value id
                            if (mlp.ValueId != null)
                                AppendAttributeNameAndRole(
                                    ie.Attribute, "valueId", AmlConst.Attributes.MultiLanguageProperty_ValueId,
                                    ToAmlReference(mlp.ValueId));
                            break;

                        case Blob smeb:
                            // mime type
                            if (smeb.ContentType != null)
                                AppendAttributeNameAndRole(
                                    ie.Attribute, "mimeType", AmlConst.Attributes.Blob_MimeType, smeb.ContentType);

                            // value
                            if (smeb.Value != null)
                            {
                                var a = AppendAttributeNameAndRole(
                                    ie.Attribute, "value", AmlConst.Attributes.Blob_Value, System.Text.Encoding.Default.GetString(smeb.Value));
                                a.AttributeDataType = "xs:string";
                            }
                            break;

                        case File smef:
                            if (aasStyleAttributes)
                            {
                                if (smef.ContentType != null)
                                    AppendAttributeNameAndRole(
                                        ie.Attribute, "mimeType", AmlConst.Attributes.File_MimeType, smef.ContentType);
                                if (smef.Value != null)
                                {
                                    AppendAttributeNameAndRole(
                                        ie.Attribute, "value", AmlConst.Attributes.File_Value, smef.Value);
                                }
                            }

                            if (amlStyleAttributes)
                            {
                                var extIf = ie.ExternalInterface.Append("FileDataReference");
                                if (extIf != null)
                                {
                                    extIf.RefBaseClassPath = AmlConst.Interfaces.FileDataReference;
                                    if (smef.ContentType != null)
                                        AppendAttributeNameAndRole(
                                            extIf.Attribute, "MIMEType", role: null, val: smef.ContentType,
                                            attributeDataType: "xs:string");
                                    if (smef.Value != null)
                                    {
                                        AppendAttributeNameAndRole(
                                            extIf.Attribute, "refURI", role: null, val: smef.Value,
                                            attributeDataType: "xs:anyURI");
                                    }
                                }
                            }
                            break;

                        case ReferenceElement smer:
                            // value == a Reference
                            ExportReferenceWithSme(env, internalLinksToCreate, ie, smer,
                                smer.Value, "value", AmlConst.Attributes.ReferenceElement_Value, "value",
                                aasStyleAttributes, amlStyleAttributes);
                            break;

                        case RelationshipElement smer:
                            // first & second
                            ExportReferenceWithSme(env, internalLinksToCreate, ie, smer,
                                smer.First, "first", AmlConst.Attributes.RelationshipElement_First, "first",
                                aasStyleAttributes, amlStyleAttributes);

                            ExportReferenceWithSme(env, internalLinksToCreate, ie, smer,
                                smer.Second, "second", AmlConst.Attributes.RelationshipElement_Second, "second",
                                aasStyleAttributes, amlStyleAttributes);

                            // Recurse
                            if (sme is AnnotatedRelationshipElement anno)
                            {
                                var annotations = new List<ISubmodelElement>(anno.Annotations);
                                ExportListOfSme(matcher, internalLinksToCreate, ie, env, annotations);
                            }

                            break;

                        case SubmodelElementCollection smec:
                            // dead-csharp off
                            // recurse
                            //ordered and allowDuplicates removed from SMEColl in V3
                            //AppendAttributeNameAndRole(
                            //    ie.Attribute, "ordered", AmlConst.Attributes.SMEC_ordered,
                            //    smec.Ordered ? "true" : "false", attributeDataType: "xs:boolean");
                            //AppendAttributeNameAndRole(
                            //    ie.Attribute, "allowDuplicates", AmlConst.Attributes.SMEC_allowDuplicates,
                            //    smec.allowDuplicates ? "true" : "false", attributeDataType: "xs:boolean");
                            // dead-csharp on

                            ExportListOfSme(matcher, internalLinksToCreate, ie, env, smec.Value);
                            break;

                        case SubmodelElementList smel:
                            // recurse
                            AppendAttributeNameAndRole(
                                ie.Attribute, "ordered", AmlConst.Attributes.SMEC_ordered,
                                (bool)smel.OrderRelevant ? "true" : "false", attributeDataType: "xs:boolean");

                            ExportListOfSme(matcher, internalLinksToCreate, ie, env, smel.Value);
                            break;

                        case Operation op:
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
                                var lop = new List<ISubmodelElement>();
                                foreach (var opVar in op.InputVariables)
                                {
                                    lop.Add(opVar.Value);
                                }

                                foreach (var opVar in op.OutputVariables)
                                {
                                    lop.Add(opVar.Value);
                                }

                                foreach (var opVar in op.InoutputVariables)
                                {
                                    lop.Add(opVar.Value);
                                }
                                ExportListOfSme(matcher, internalLinksToCreate, oie, env, lop);
                            }
                            break;

                        case Entity ent:
                            // entityType
                            AppendAttributeNameAndRole(
                                ie.Attribute, "entityType", AmlConst.Attributes.Entity_entityType,
                                Stringification.ToString(ent.EntityType), attributeDataType: "xs:string");

                            // assetRef
                            //TODO (jtikekar, 0000-00-00):  SpecficAssetId
                            if (!string.IsNullOrEmpty(ent.GlobalAssetId))
                            {
                                // dead-csharp off
                                //TODO (jtikekar, 0000-00-00): uncomment and support
                                //ExportReferenceWithSme(env, internalLinksToCreate, ie, ent,
                                //    ent.GlobalAssetId, "asset", AmlConst.Attributes.Entity_asset, "asset",
                                //    aasStyleAttributes, amlStyleAttributes);
                                // dead-csharp on
                            }

                            // Recurse
                            ExportListOfSme(matcher, internalLinksToCreate, ie, env, ent.Statements);
                            break;

                        case AasCore.Aas3_0.Range rng:
                            // min
                            if (rng.Min != null)
                            {
                                var a = AppendAttributeNameAndRole(
                                    ie.Attribute, "min", AmlConst.Attributes.Range_Min, rng.Min);
                                a.AttributeDataType = "xs:" + Stringification.ToString(rng.ValueType).Trim();
                            }

                            // max
                            if (rng.Max != null)
                            {
                                var a = AppendAttributeNameAndRole(
                                    ie.Attribute, "max", AmlConst.Attributes.Range_Max, rng.Max);
                                a.AttributeDataType = "xs:" + Stringification.ToString(rng.ValueType).Trim();
                            }
                            break;
                    }

                    // Qualifiers
                    SetQualifiers(ie.InternalElement, ie.Attribute, sme.Qualifiers, parentAsInternalElements: false);
                }
            }
        }

        private static void ExportSubmodelIntoElement(
            AasAmlMatcher matcher, List<AmlInternalLinkEntity> internalLinksToCreate,
            SystemUnitClassType parent,
            AasCore.Aas3_0.Environment env,
            ISubmodel sm,
            bool tryUseCompactProperties = false,
            bool exportShallow = false)
        {
            if (parent == null || env == null || sm == null)
                return;

            // set some data
            SetIdentification(parent.Attribute, sm.Id);
            SetAdministration(parent.Attribute, sm.Administration);
            SetReferable(parent.Attribute, sm);
            SetModelingKind(parent.Attribute, sm.Kind);
            SetSemanticId(parent.Attribute, sm.SemanticId);
            SetHasDataSpecification(parent.Attribute, sm.EmbeddedDataSpecifications);
            SetQualifiers(null, parent.Attribute, sm.Qualifiers, parentAsInternalElements: false);

            // properties
            if (!exportShallow)
                ExportListOfSme(
                    matcher, internalLinksToCreate, parent, env, sm.SubmodelElements, tryUseCompactProperties);
            else
            {
                // add a small information
                AppendIeNameAndRole(parent.InternalElement, name: "Remark: duplicate AAS:Submodel");
            }
        }

        private static InternalElementType ExportSubmodel(
            AasAmlMatcher matcher,
            List<AmlInternalLinkEntity> internalLinksToCreate, InternalElementSequence ieseq,
            AasCore.Aas3_0.Environment env,
            ISubmodel sm,
            bool tryUseCompactProperties = false,
            bool exportShallow = false)
        {
            if (ieseq == null || env == null || sm == null)
                return null;

            // directly add internal element
            var ie = AppendIeNameAndRole(ieseq, name: sm.IdShort, altName: "Submodel", role: AmlConst.Roles.Submodel);


            ExportSubmodelIntoElement(
                matcher, internalLinksToCreate, ie, env, sm, tryUseCompactProperties, exportShallow);

            // return IE
            return ie;
        }

        private static void ExportAsset(
            InternalElementSequence ieseq, AasCore.Aas3_0.Environment env, IAssetInformation asset)
        {
            if (ieseq == null || env == null || asset == null)
                return;

            // directly add internal element
            var ie = AppendIeNameAndRole(ieseq, name: asset.GlobalAssetId, altName: "AssetInformation", role: AmlConst.Roles.AssetInformation);

            // set some data
            //TODO (jtikekar, 0000-00-00): what about specific asset Ids
            if (!string.IsNullOrEmpty(asset.GlobalAssetId))
            {
                SetIdentification(ie.Attribute, asset.GlobalAssetId);
            }

            SetAssetKind(ie.Attribute, asset.AssetKind, attributeRole: AmlConst.Attributes.Asset_Kind);

            // dead-csharp off
            // do some data directly
            //No More assetIdentificationModelRef and BOM a part of Asset
            //if (asset.assetIdentificationModelRef != null)
            //    AppendAttributeNameAndRole(
            //        ie.Attribute, "assetIdentificationModelRef", AmlConst.Attributes.Asset_IdentificationModelRef,
            //        ToAmlReference(asset.assetIdentificationModelRef));

            //if (asset.billOfMaterialRef != null)
            //    AppendAttributeNameAndRole(ie.Attribute, "billOfMaterialRef",
            //    AmlConst.Attributes.Asset_BillOfMaterialRef, ToAmlReference(asset.billOfMaterialRef));
        }

        //private static void ExportView(
        //    AasAmlMatcher matcher, InternalElementSequence ieseq, AasCore.Aas3_0_RC02.Environment env, View view)
        //{
        //    if (ieseq == null || env == null || view == null)
        //        return;

        //    // directly add internal element
        //    var ie = AppendIeNameAndRole(ieseq, name: view.IdShort, altName: "View", role: AmlConst.Roles.View);

        //    // set some data
        //    SetReferable(ie.Attribute, view);
        //    SetSemanticId(ie.Attribute, view.SemanticId);
        //    SetHasDataSpecification(ie.Attribute, view.hasDataSpecification);

        //    // view references
        //    // from the Meeting: Views sind Listen von "Mirror-Elementen",
        //    // die auf die Properties.. (IEs) der AAS verweisen.
        //    // Views hängen unter der jeweiligen AAS (IReferable)
        //    for (int i = 0; i < view.Count; i++)
        //    {
        //        // access contained element
        //        var ce = view[i];
        //        if (ce == null)
        //            continue;

        //        // find the referenced element
        //        var targetReferable = env.FindReferableByReference(ce);
        //        if (targetReferable == null)
        //            continue;

        //        // rely on the "hope", that there is a match
        //        var targetAml = matcher.GetAmlObject(targetReferable);
        //        if (targetAml == null)
        //            continue;

        //        // for the time being, make an IE
        //        // Note: it is "forbidden" to set Roles for mirror elements
        //        var iece = AppendIeNameAndRole(
        //            ie.InternalElement, name: ce.ListOfValues("/"), altName: "Reference",
        //            role: null);

        //        // just convert it to an mirror element
        //        iece.RefBaseSystemUnitPath = "" + targetAml.ID;
        //    }
        //}
        // dead-csharp on
        private static void ExportAAS(
            AasAmlMatcher matcher, InstanceHierarchyType insthier, SystemUnitClassLibType suchier,
            AasCore.Aas3_0.Environment env, IAssetAdministrationShell aas,
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
                insthier.InternalElement, name: aas.IdShort, altName: "AAS", role: AmlConst.Roles.AAS);

            // set some data
            SetIdentification(aasIE.Attribute, aas.Id);
            SetAdministration(aasIE.Attribute, aas.Administration);
            SetReferable(aasIE.Attribute, aas);
            SetHasDataSpecification(aasIE.Attribute, aas.EmbeddedDataSpecifications);

            if (aas.DerivedFrom != null)
                AppendAttributeNameAndRole(
                    aasIE.Attribute, "derivedFrom", AmlConst.Attributes.AAS_DerivedFrom,
                    ToAmlReference(aas.DerivedFrom));

            // asset
            var asset = aas.AssetInformation;
            ExportAsset(aasIE.InternalElement, env, asset);

            // the AAS for Submodels of kind = Type willbe created on demand
            SystemUnitFamilyType aasSUC = null;

            //
            // Submodels can be of kind Type/ Instance
            //
            foreach (var smref in aas.Submodels)
            {
                // ref -> Submodel
                var sm = env.FindSubmodel(smref);
                if (sm == null)
                    continue;

                // SM types go to system unit classes, instances goe to instance hierarchy
                if (sm.Kind != null && sm.Kind == ModellingKind.Template)
                {
                    // create AAS for SUCs on demand
                    if (aasSUC == null)
                    {
                        // create parent
                        aasSUC = suchier.SystemUnitClass.Append(aas.IdShort ?? "AAS_" + Guid.NewGuid().ToString());

                        // role
                        var rr = aasSUC.SupportedRoleClass.Append();
                        rr.RefRoleClassPath = AmlConst.Roles.AAS;
                    }

                    // create a dedicated SUC for this
                    var smSUC = aasSUC.SystemUnitClass.Append(sm.IdShort ?? "Submodel_" + Guid.NewGuid().ToString());

                    // role
                    var rq = smSUC.SupportedRoleClass.Append();
                    rq.RefRoleClassPath = AmlConst.Roles.Submodel;

                    // set same data data, in order to correlate, but not asset
                    SetIdentification(aasSUC.Attribute, aas.Id);
                    SetAdministration(aasSUC.Attribute, aas.Administration);
                    SetReferable(aasSUC.Attribute, aas);
                    SetHasDataSpecification(aasSUC.Attribute, aas.EmbeddedDataSpecifications);

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
                            aasIE.InternalElement, name: sm.IdShort, altName: "Submodel", role: null);

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
            // Internal Links
            //
            SetInternalLinks(aasIE, matcher, internalLinksToCreate);
        }

        /// <summary>
        /// Sets attributes to outer and inner element (optional) of an ConceptDescription.
        /// If aseqInnner == null, "embeddedDataSpec"-approach will be used
        /// </summary>
        private static void SetAttributesForConceptDescription(
            AttributeSequence aseqOuter, AttributeSequence aseqInner, IConceptDescription cd,
            ref string name)
        {
            // check
            if (aseqOuter == null || cd == null)
                return;

            // start to figure out an speaking name for the AML entity (idSHort, shortName .. will be codified
            // as original attributes, as well)
            // the name will be set at the end
            name = "CD_" + Guid.NewGuid().ToString();
            if (cd.IdShort.HasContent())
                name = cd.IdShort;

            // set data for identifiable
            SetIdentification(aseqOuter, cd.Id);
            SetAdministration(aseqOuter, cd.Administration);
            SetReferable(aseqOuter, cd);

            // isCaseOf
            if (cd.IsCaseOf != null)
                foreach (var r in cd.IsCaseOf)
                    AppendAttributeNameAndRole(aseqOuter, "isCaseOf", AmlConst.Attributes.CD_IsCaseOf, ToAmlReference(r));

            // which data spec as reference
            if (cd.EmbeddedDataSpecifications != null)
                foreach (var eds in cd.EmbeddedDataSpecifications)
                    if (eds.DataSpecification != null)
                        AppendAttributeNameAndRole(
                            aseqOuter, "dataSpecification", AmlConst.Attributes.CD_DataSpecificationRef,
                            ToAmlReference(eds.DataSpecification));

            //jtikekar:Added as üet DotAAS-1
            // TODO (MIHO, 2022-12-21): do not understand this duplication?!
            if (cd.EmbeddedDataSpecifications != null)
                foreach (var ds in cd.EmbeddedDataSpecifications)
                    if (ds != null)
                        AppendAttributeNameAndRole(
                            aseqOuter, "dataSpecification", AmlConst.Attributes.CD_DataSpecificationRef,
                            ToAmlReference(ds.DataSpecification));

            // which data spec to take as source?
            var source61360 = cd.EmbeddedDataSpecifications?.GetIEC61360Content();
            // TODO (Michael Hoffmeister, 2020-08-01): If further data specifications exist (in future), add here

            // decide which approach to take (1 or 2 IE)
            AttributeSequence dest61360 = null;
            if (aseqInner == null)
            {
                // we will pack the attribute under an embedded data spec attribute branch
                // now, to the embedded data spec
                if (cd.EmbeddedDataSpecifications != null)
                {
                    var eds = AppendAttributeNameAndRole(
                        aseqOuter, "dataSpecification", AmlConst.Attributes.CD_EmbeddedDataSpecification);
                    if (source61360 != null)
                    {
                        var dsc = AppendAttributeNameAndRole(
                            eds.Attribute, "dataSpecificationContent",
                            AmlConst.Attributes.CD_DataSpecificationContent);

                        // create attribute branch for CD contents
                        var dsc61360 = AppendAttributeNameAndRole(
                            dsc.Attribute, "dataSpecificationIEC61360",
                            AmlConst.Attributes.CD_DataSpecification61360);
                        dest61360 = dsc61360.Attribute;
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
                if (source61360.ShortName != null && source61360.ShortName.Count > 0)
                    name = source61360.ShortName.GetDefaultString();

                // specific data
                SetLangStr(
                    dest61360, source61360.PreferredName, "preferredName",
                    AmlConst.Attributes.CD_DSC61360_PreferredName);
                SetLangStr(
                    dest61360, source61360.ShortName, "shortName",
                    AmlConst.Attributes.CD_DSC61360_ShortName);
                if (source61360.Unit != null)
                    AppendAttributeNameAndRole(
                        dest61360, "unit", AmlConst.Attributes.CD_DSC61360_Unit, source61360.Unit);
                if (source61360.UnitId != null)
                    AppendAttributeNameAndRole(
                        dest61360, "unitId", AmlConst.Attributes.CD_DSC61360_UnitId,
                        ToAmlReference(new Reference(ReferenceTypes.ExternalReference, source61360.UnitId.Keys)));
                if (source61360.ValueFormat != null)
                    AppendAttributeNameAndRole(
                        dest61360, "valueFormat", AmlConst.Attributes.CD_DSC61360_ValueFormat,
                        source61360.ValueFormat);
                if (source61360.SourceOfDefinition != null)
                    AppendAttributeNameAndRole(
                        dest61360, "sourceOfDefinition", AmlConst.Attributes.CD_DSC61360_SourceOfDefinition,
                        source61360.SourceOfDefinition);
                if (source61360.Symbol != null)
                    AppendAttributeNameAndRole(
                        dest61360, "symbol", AmlConst.Attributes.CD_DSC61360_Symbol, source61360.Symbol);
                if (source61360.DataType != null)
                    AppendAttributeNameAndRole(
                        dest61360, "dataType", AmlConst.Attributes.CD_DSC61360_DataType,
                        Stringification.ToString(source61360.DataType));
                SetLangStr(
                    dest61360, source61360.Definition, "definition",
                    AmlConst.Attributes.CD_DSC61360_Definition);
            }
        }


        private static void ExportConceptDescriptionsWithExtraContentToIHT(
            InstanceHierarchyType lib, AasCore.Aas3_0.Environment env)
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
                name += "_" + ToAmlName(cd.Id);
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
                if (x is Submodel smki)
                {
                    if (smki.Kind != null && (smki.Kind == ModellingKind.Instance) && smki.SemanticId != null)
                    {
                        foreach (var y in matcher.GetAllAasReferables())
                            if (y is Submodel smkt)
                            {
                                if (smkt.Kind != null && smkt.Kind == ModellingKind.Template &&
                                    smki.SemanticId.Matches(
                                        KeyTypes.Submodel,
                                        smkt.Id))
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
            foreach (var aas in package.AasEnv.AssetAdministrationShells)
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
