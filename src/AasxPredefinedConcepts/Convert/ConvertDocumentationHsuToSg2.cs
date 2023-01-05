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
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0_RC02;
using AasxIntegrationBase;
using AdminShellNS;
using Extensions;

// ReSharper disable MergeIntoPattern

namespace AasxPredefinedConcepts.Convert
{
    public class ConvertDocumentationHsuToSg2Provider : ConvertProviderBase
    {
        public class ConvertOfferDocumentationHsuToSg2 : ConvertOfferBase
        {
            public ConvertOfferDocumentationHsuToSg2() { }
            public ConvertOfferDocumentationHsuToSg2(ConvertProviderBase provider, string offerDisp)
                : base(provider, offerDisp) { }
        }

        public override List<ConvertOfferBase> CheckForOffers(AasCore.Aas3_0_RC02.IReferable currentReferable)
        {
            // collectResults
            var res = new List<ConvertOfferBase>();

            // use pre-definitions
            var defs = new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate.SetOfDocumentation(
                    new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate());

            var sm = currentReferable as AasCore.Aas3_0_RC02.Submodel;
            if (sm != null && true == sm.SemanticId.GetAsExactlyOneKey()?.Matches(defs.SM_Document.SemanticId.GetAsExactlyOneKey()))
                res.Add(new ConvertOfferDocumentationHsuToSg2(this,
                            $"Convert Submodel '{"" + sm.IdShort}' for Documentation HSU to SG2"));

            return res;
        }

        public override bool ExecuteOffer(
            AdminShellPackageEnv package, AasCore.Aas3_0_RC02.IReferable currentReferable, ConvertOfferBase offerBase,
            bool deleteOldCDs, bool addNewCDs)
        {
            // access
            var offer = offerBase as ConvertOfferDocumentationHsuToSg2;
            if (package == null || package.AasEnv == null || currentReferable == null || offer == null)
                return false;

            // use pre-definitions
            var defsHsu = new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate.SetOfDocumentation(
                    new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate());
            var defsSg2 = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(
                    new AasxPredefinedConcepts.DefinitionsVDI2770());

            // access Submodel (again)
            var sm = currentReferable as AasCore.Aas3_0_RC02.Submodel;
            if (sm == null || sm.SubmodelElements == null ||
                    true != sm.SemanticId.GetAsExactlyOneKey()?.Matches(defsHsu.SM_Document.SemanticId.GetAsExactlyOneKey()))
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smcOldHsu = sm.SubmodelElements;
            sm.SubmodelElements = new List<AasCore.Aas3_0_RC02.ISubmodelElement>();
            sm.SemanticId = new AasCore.Aas3_0_RC02.Reference(AasCore.Aas3_0_RC02.ReferenceTypes.ModelReference, new List<AasCore.Aas3_0_RC02.Key>() { defsSg2.SM_VDI2770_Documentation.SemanticId.GetAsExactlyOneKey() });

            // delete (old) CDs
            if (deleteOldCDs)
            {
                sm.RecurseOnSubmodelElements(null, (state, parents, current) =>
                {
                    var sme = current;
                    if (sme != null && sme.SemanticId != null)
                    {
                        var cd = package.AasEnv.FindConceptDescriptionByReference(sme.SemanticId);
                        if (cd != null)
                            if (package.AasEnv.ConceptDescriptions.Contains(cd))
                                package.AasEnv.ConceptDescriptions.Remove(cd);
                    }
                    // recurse
                    return true;
                });
            }

            // add (all) new CDs?
            if (addNewCDs)
                foreach (var rf in defsSg2.GetAllReferables())
                    if (rf is AasCore.Aas3_0_RC02.ConceptDescription conceptDescription)
                    {
                        package.AasEnv.ConceptDescriptions.AddConceptDescriptionOrReturnExisting(
                                new AasCore.Aas3_0_RC02.ConceptDescription(
                                    conceptDescription.Id, conceptDescription.Extensions, 
                                    conceptDescription.Category, conceptDescription.IdShort, 
                                    conceptDescription.DisplayName, conceptDescription.Description, 
                                    conceptDescription.Checksum, conceptDescription.Administration, 
                                    conceptDescription.EmbeddedDataSpecifications, 
                                    conceptDescription.IsCaseOf));
                    }

            // ok, go thru the old == HSU records
            foreach (var smcSource in smcOldHsu.FindAllSemanticIdAs<AasCore.Aas3_0_RC02.SubmodelElementCollection>(
                        defsHsu.CD_DocumentationItem.GetSingleKey(), MatchMode.Relaxed))
            {
                // access
                if (smcSource == null || smcSource.Value == null)
                    continue;

                // make new SG2 Document + DocumentItem
                // Document Item
                // ReSharper disable once ConvertToUsingDeclaration
                //using (var smcDoc = AasCore.Aas3_0_RC02.SubmodelElementCollection.CreateNew("" + smcSource.IdShort,
                //            smcSource.Category,
                //            AasCore.Aas3_0_RC02.Key.GetFromRef(defsSg2.CD_VDI2770_Document.GetCdReference())))
                
                AasCore.Aas3_0_RC02.SubmodelElementCollection smcDoc = new AasCore.Aas3_0_RC02.SubmodelElementCollection(idShort: "" + smcSource.IdShort, category: smcSource.Category, semanticId: defsSg2.CD_VDI2770_Document.GetCdReference());
                //using (var smcDocVersion = AasCore.Aas3_0_RC02.SubmodelElementCollection.CreateNew("DocumentVersion",
                //            smcSource.Category,
                //            AasCore.Aas3_0_RC02.Key.GetFromRef(defsSg2.CD_VDI2770_DocumentVersion.GetCdReference())))
                AasCore.Aas3_0_RC02.SubmodelElementCollection smcDocVersion = new AasCore.Aas3_0_RC02.SubmodelElementCollection(idShort: "DocumentVersion", category: smcSource.Category, semanticId: defsSg2.CD_VDI2770_DocumentVersion.GetCdReference());
                {
                    // Document itself
                    smcDoc.Description = smcSource.Description;

                    // classification
                    var clid = smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                            defsHsu.CD_DocumentClassification_ClassId.GetSingleKey())?.Value;
                    var clname = "" + smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                            defsHsu.CD_VDI2770_ClassName.GetSingleKey())?.Value;
                    var clsys = "" + smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                            defsHsu.CD_VDI2770_ClassificationSystem.GetSingleKey())?.Value;

#if future_structure
                    // as described in the VDI 2770 Submodel template document
                    if (clid.HasContent())
                        using (var smcClass = AasCore.Aas3_0_RC02.SubmodelElementCollection.CreateNew("DocumentClassification",
                                    smcSource.Category, AasCore.Aas3_0_RC02.Key.GetFromRef(defsSg2.CD_XXX.GetReference())))
                        {
                            smcDoc.Add(smcClass);

                            smcClass.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(defsSg2.CD_VDI2770_DocumentClassId,
                                addSme: true)?.Set("string", "" + clid);
                            smcClass.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(defsSg2.CD_VDI2770_DocumentClassName,
                                addSme: true)?.Set("string", "" + clname);
                            smcClass.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                                defsSg2.CD_VDI2770_DocumentClassificationSystem, addSme: true)?
                                .Set("string", "" + clsys);
                        }

#else
                    // current state of code
                    var property = smcDoc.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(defsSg2.CD_VDI2770_DocumentClassId,
                            addSme: true);
                    property.Value = clid;
                    property.ValueType = AasCore.Aas3_0_RC02.DataTypeDefXsd.String;

                    property = smcDoc.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(defsSg2.CD_VDI2770_DocumentClassName,
                            addSme: true);
                    property.Value = clname;

                    property = smcDoc.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(defsSg2.CD_VDI2770_DocumentClassificationSystem,
                            addSme: true);
                    property.Value = clsys;
#endif

                    // items ..
                    property = smcDoc.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(defsSg2.CD_VDI2770_DocumentId, addSme: true);
                    property.Value = "" + smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                    defsHsu.CD_DocumentId.GetSingleKey())?.Value;

                    var idt = "" + smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                            defsHsu.CD_VDI2770_IdType.GetSingleKey())?.IsValueTrue();

                    property = smcDoc.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(defsSg2.CD_VDI2770_IsPrimaryDocumentId,
                            addSme: true);
                    property.ValueType = AasCore.Aas3_0_RC02.DataTypeDefXsd.Boolean;
                    property.Value = idt.Equals("primary", StringComparison.OrdinalIgnoreCase) ? "True" : "False";

                    var referenceElement = smcDoc.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.ReferenceElement>(defsSg2.CD_VDI2770_ReferencedObject,
                            addSme: true);
                    referenceElement.Value = new AasCore.Aas3_0_RC02.Reference(AasCore.Aas3_0_RC02.ReferenceTypes.ModelReference, new List<AasCore.Aas3_0_RC02.Key>());

                    // DocumentVersion

                    // languages
                    var lcs = "" + smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                            defsHsu.CD_DocumentVersion_LanguageCode.GetSingleKey())?.Value;
                    var lcsa = lcs.Trim().Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    //ReSharper disable ConditionIsAlwaysTrueOrFalse
                    if (lcsa != null && lcsa.Length > 0)
                    {
                        int i = 0;
                        foreach (var lc in lcsa)
                        {
                            var lcc = "" + lc;
                            if (lcc.IndexOf('-') > 0)
                                lcc = lc.Substring(0, lcc.IndexOf('-'));
                            property = smcDocVersion.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(defsSg2.CD_VDI2770_Language,
                                    idShort: $"Language{(i++):00}", addSme: true);
                            property.Value = "" + lcc;
                        }
                    }
                    //ReSharper enable ConditionIsAlwaysTrueOrFalse

                    property = smcDocVersion.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(defsSg2.CD_VDI2770_DocumentVersionId,
                        addSme: true);
                    property.Value = "" + smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                    defsHsu.CD_DocumentVersionId.GetSingleKey())?.Value;

                    var cdSrc = new[] { defsHsu.CD_VDI2770_Title, defsHsu.CD_VDI2770_Summary,
                        defsHsu.CD_VDI2770_Keywords };
                    var cdDst = new[] { defsSg2.CD_VDI2770_Title, defsSg2.CD_VDI2770_Summary,
                        defsSg2.CD_VDI2770_Keywords };
                    for (int i = 0; i < 3; i++)
                    {
                        var target = smcDocVersion.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.MultiLanguageProperty>(cdDst[i],
                                addSme: true);
                        if (target == null)
                            continue;

                        var asProp = smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                cdSrc[i].GetSingleKey());
                        if (asProp != null)
                        {
                            target.Value = new List<AasCore.Aas3_0_RC02.LangString>() { new AasCore.Aas3_0_RC02.LangString("en?", "" + asProp.Value) };
                        }

                        var asMLP = smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.MultiLanguageProperty>(
                                cdSrc[i].GetSingleKey());
                        if (asMLP != null)
                            target.Value = asMLP.Value;
                    }

                    property = smcDocVersion.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(defsSg2.CD_VDI2770_Date, addSme: true);
                    property.Value = "" + smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                    defsHsu.CD_VDI2770_SetDate.GetSingleKey())?.Value;

                    property = smcDocVersion.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(defsSg2.CD_VDI2770_Role, addSme: true);
                    property.Value = "" + smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                    defsHsu.CD_VDI2770_Role.GetSingleKey())?.Value;

                    property = smcDocVersion.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(defsSg2.CD_VDI2770_OrganizationName,
                        addSme: true);
                    property.Value = smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                    defsHsu.CD_VDI2770_OrganisationName.GetSingleKey())?.Value;

                    property = smcDocVersion.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                        defsSg2.CD_VDI2770_OrganizationOfficialName, addSme: true);
                    property.Value = "" + smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                defsHsu.CD_VDI2770_OrganisationOfficialName.GetSingleKey())?.Value;

                    // 1 file?
                    var fl = smcSource.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.File>(defsHsu.CD_File.GetSingleKey());
                    var file = smcDocVersion.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.File>(defsSg2.CD_VDI2770_DigitalFile, addSme: true);
                    file.ContentType = fl?.ContentType;
                    file.Value = fl?.Value;

                    // finally, add
                    smcDoc.Add(smcDocVersion);
                    sm.SubmodelElements.Add(smcDoc);

                }
            }

            // obviously well
            return true;
        }
    }
}
