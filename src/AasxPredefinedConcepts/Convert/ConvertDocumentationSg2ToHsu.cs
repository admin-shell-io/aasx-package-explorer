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
using AdminShellNS;
using Extensions;

// ReSharper disable MergeIntoPattern

namespace AasxPredefinedConcepts.Convert
{
    public class ConvertDocumentationSg2ToHsuProvider : ConvertProviderBase
    {
        public class ConvertOfferDocumentationSg2ToHsu : ConvertOfferBase
        {
            public ConvertOfferDocumentationSg2ToHsu() { }
            public ConvertOfferDocumentationSg2ToHsu(ConvertProviderBase provider, string offerDisp)
                : base(provider, offerDisp) { }
        }

        public override List<ConvertOfferBase> CheckForOffers(Aas.IReferable currentReferable)
        {
            // collectResults
            var res = new List<ConvertOfferBase>();

            // use pre-definitions
            var defs = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(
                    new AasxPredefinedConcepts.DefinitionsVDI2770());

            var sm = currentReferable as Aas.Submodel;
            if (sm != null && true == sm.SemanticId.GetAsExactlyOneKey()?.Matches(defs.SM_VDI2770_Documentation.SemanticId.GetAsExactlyOneKey()))
                res.Add(new ConvertOfferDocumentationSg2ToHsu(this,
                            $"Convert Submodel '{"" + sm.IdShort}' for Documentation SG2 to HSU"));

            // MIHO, 2020-07-31: temporary have code to allow conversion of Festo MCAD / ECAD models as well
            //// if (sm != null && true == sm.SemanticId.GetAsExactlyOneKey()?.Matches("Submodel", false, "IRI", 
            ////       "http://smart.festo.com/AAS/Submodel/ComputerAidedDesign/1/0", MatchMode.Relaxed))
            ////     res.Add(new ConvertOfferDocumentationSg2ToHsu(this, 
            ////       $"Convert Submodel '{"" + sm.IdShort}' for Documentation SG2 to HSU"));

            return res;
        }

        public override bool ExecuteOffer(AdminShellPackageEnv package, Aas.IReferable currentReferable,
                ConvertOfferBase offerBase, bool deleteOldCDs, bool addNewCDs)
        {
            // access
            var offer = offerBase as ConvertOfferDocumentationSg2ToHsu;
            if (package == null || package.AasEnv == null || currentReferable == null || offer == null)
                return false;

            // use pre-definitions
            var defsSg2 = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(
                    new AasxPredefinedConcepts.DefinitionsVDI2770());
            var defsHsu = new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate.SetOfDocumentation(
                    new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate());

            // access Submodel (again)
            var sm = currentReferable as Aas.Submodel;
            if (sm == null || sm.SubmodelElements == null ||
                    true != sm.SemanticId.GetAsExactlyOneKey()?.Matches(defsSg2.SM_VDI2770_Documentation.SemanticId.GetAsExactlyOneKey()))
                /* disable line above to allow more models, such as MCAD/ECAD */
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smcOldSg2 = sm.SubmodelElements;
            sm.SubmodelElements = new List<Aas.ISubmodelElement>();
            sm.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.Key>() { defsHsu.SM_Document.SemanticId.GetAsExactlyOneKey() });

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
                foreach (var rf in defsHsu.GetAllReferables())
                    if (rf is Aas.ConceptDescription conceptDescription)
                        package.AasEnv.ConceptDescriptions.AddConceptDescriptionOrReturnExisting(
                            new Aas.ConceptDescription(
                                conceptDescription.Id, conceptDescription.Extensions,
                                conceptDescription.Category, conceptDescription.IdShort,
                                conceptDescription.DisplayName, conceptDescription.Description,
                                conceptDescription.Checksum, conceptDescription.Administration,
                                conceptDescription.EmbeddedDataSpecifications,
                                conceptDescription.IsCaseOf));

            // ok, go thru the old == SG2 records
            foreach (var smcDoc in smcOldSg2.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                        defsSg2.CD_VDI2770_Document.GetSingleKey()))
            {
                // access
                if (smcDoc == null || smcDoc.Value == null)
                    continue;

                // look immediately for DocumentVersion, as only with this there is a valid List item
                foreach (var smcVer in smcDoc.Value.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                            defsSg2.CD_VDI2770_DocumentVersion.GetSingleKey()))
                {
                    // access
                    if (smcVer == null || smcVer.Value == null)
                        continue;

                    // make new HSU Document
                    // ReSharper disable once ConvertToUsingDeclaration
                    // Document Item
                    //using (var smcHsuDoc = Aas.SubmodelElementCollection.CreateNew("" + smcDoc.IdShort,
                    //            smcDoc.Category,
                    //            Aas.Key.GetFromRef(defsHsu.CD_DocumentationItem.GetCdReference())))
                    var smcHsuDoc = new Aas.SubmodelElementCollection(idShort: "" + smcDoc.IdShort, category: smcDoc.Category, semanticId: defsHsu.CD_DocumentationItem.GetCdReference());
                    {
                        // Document itself
                        smcHsuDoc.Description = smcDoc.Description;
                        sm.SubmodelElements.Add(smcHsuDoc);

                        // items ..
                        var property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_DocumentType, addSme: true);
                        property.Value = "Single";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_DomainId,
                            addSme: true);
                        property.Value = "";

                        var b = true == smcVer.Value.FindFirstSemanticIdAs<Aas.Property>(
                                defsSg2.CD_VDI2770_IsPrimaryDocumentId.GetSingleKey())?.IsValueTrue();
                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_IdType, addSme: true);
                        property.Value = b ? "Primary" : "Secondary";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_DocumentId, addSme: true);
                        property.Value = "";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_DocumentDomainId,
                            addSme: true);
                        property.Value = "";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_Role, addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticIdAs<Aas.Property>(
                                        defsSg2.CD_VDI2770_Role.GetSingleKey())?.Value;

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_OrganisationId,
                            addSme: true);
                        property.Value = "";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_OrganisationName,
                            addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticIdAs<Aas.Property>(
                                        defsSg2.CD_VDI2770_OrganizationName.GetSingleKey())?.Value;

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(
                                defsHsu.CD_VDI2770_OrganisationOfficialName, addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticIdAs<Aas.Property>(
                                        defsSg2.CD_VDI2770_OrganizationOfficialName.GetSingleKey())?.Value;

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_Description,
                                addSme: true);
                        property.Value = "";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_DocumentPartId, addSme: true);
                        property.Value = "";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_DocumentClassification_ClassId,
                            addSme: true);
                        property.Value = "" + smcDoc.Value.FindFirstSemanticIdAs<Aas.Property>(
                                    defsSg2.CD_VDI2770_DocumentClassId.GetSingleKey())?.Value;

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_ClassName,
                            addSme: true);
                        property.Value = "" + smcDoc.Value.FindFirstSemanticIdAs<Aas.Property>(
                                    defsSg2.CD_VDI2770_DocumentClassName.GetSingleKey())?.Value;

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_ClassificationSystem,
                            addSme: true);
                        property.Value = "" + smcDoc.Value.FindFirstSemanticIdAs<Aas.Property>(
                                    defsSg2.CD_VDI2770_DocumentClassificationSystem.GetSingleKey())?.Value;

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_DocumentVersionId,
                            addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticIdAs<Aas.Property>(
                                    defsSg2.CD_VDI2770_DocumentVersionId.GetSingleKey())?.Value;

                        var lcs = "";
                        foreach (var lcp in smcVer.Value.FindAllSemanticIdAs<Aas.Property>(
                                    defsSg2.CD_VDI2770_Language.GetSingleKey()))
                            lcs += "" + lcp?.Value + ",";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_DocumentVersion_LanguageCode,
                                addSme: true);
                        property.Value = lcs.TrimEnd(',');

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_Title, addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticId(
                                        defsSg2.CD_VDI2770_Title.GetSingleKey(),
                                        new[] {
                                            typeof(Aas.Property),
                                            typeof(Aas.MultiLanguageProperty)
                                        })?.ValueAsText();

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_Summary, addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticId(
                                        defsSg2.CD_VDI2770_Summary.GetSingleKey(),
                                        new[] {
                                            typeof(Aas.Property),
                                            typeof(Aas.MultiLanguageProperty)
                                        })?.ValueAsText();

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_Keywords,
                            addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticId(
                                    defsSg2.CD_VDI2770_Keywords.GetSingleKey(),
                                    new[] {
                                        typeof(Aas.Property),
                                        typeof(Aas.MultiLanguageProperty)
                                    })?.ValueAsText();

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_StatusValue,
                            addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticIdAs<Aas.Property>(
                                    defsSg2.CD_VDI2770_StatusValue.GetSingleKey())?.Value;

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_SetDate, addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticIdAs<Aas.Property>(
                                        defsSg2.CD_VDI2770_Date.GetSingleKey())?.Value;

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_Purpose, addSme: true);
                        property.Value = "";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_BasedOnProcedure,
                            addSme: true);
                        property.Value = "";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_Comments,
                            addSme: true);
                        property.Value = "";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_ReferencedObject_Type,
                            addSme: true);
                        property.Value = "";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(
                            defsHsu.CD_VDI2770_ReferencedObject_RefType, addSme: true);
                        property.Value = "";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(
                            defsHsu.CD_VDI2770_ReferencedObject_ObjectId, addSme: true);
                        property.Value = "";

                        property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_FileId, addSme: true);
                        property.Value = "";

                        var fl = smcVer.Value.FindFirstSemanticIdAs<Aas.File>(
                                defsSg2.CD_VDI2770_DigitalFile.GetSingleKey());
                        if (fl != null)
                        {
                            property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_FileName,
                                addSme: true);
                            property.Value = System.IO.Path.GetFileName("" + fl.Value);

                            property = smcHsuDoc.Value.CreateSMEForCD<Aas.Property>(defsHsu.CD_VDI2770_FileFormat,
                                addSme: true);
                            property.Value = "" + fl.ContentType;

                            var file = smcHsuDoc.Value.CreateSMEForCD<Aas.File>(defsHsu.CD_File, addSme: true);
                            file.ContentType = fl.ContentType;
                            file.Value = fl.Value;

                        }
                    }
                }
            }

            // obviously well
            return true;
        }
    }
}
