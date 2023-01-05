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
using AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

// ReSharper disable MergeIntoPattern

namespace AasxPredefinedConcepts.Convert
{
    public class ConvertDocumentationSg2ToV11Provider : ConvertProviderBase
    {
        public class ConvertOfferDocumentationSg2ToV11 : ConvertOfferBase
        {
            public ConvertOfferDocumentationSg2ToV11() { }
            public ConvertOfferDocumentationSg2ToV11(ConvertProviderBase provider, string offerDisp)
                : base(provider, offerDisp) { }
        }

        public override List<ConvertOfferBase> CheckForOffers(AasCore.Aas3_0_RC02.IReferable currentReferable)
        {
            // collectResults
            var res = new List<ConvertOfferBase>();

            // use pre-definitions
            var defs = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(
                    new AasxPredefinedConcepts.DefinitionsVDI2770());

            var sm = currentReferable as Submodel;
            if (sm != null && true == sm.SemanticId.GetAsExactlyOneKey()?.Matches(defs.SM_VDI2770_Documentation.SemanticId.GetAsExactlyOneKey()))
                res.Add(new ConvertOfferDocumentationSg2ToV11(this,
                            $"Convert Submodel '{"" + sm.IdShort}' for Documentation SG2 (V1.0) to V1.1"));

            return res;
        }

        public override bool ExecuteOffer(AdminShellPackageEnv package, AasCore.Aas3_0_RC02.IReferable currentReferable,
                ConvertOfferBase offerBase, bool deleteOldCDs, bool addNewCDs)
        {
            // access
            var offer = offerBase as ConvertOfferDocumentationSg2ToV11;
            if (package == null || package.AasEnv == null || currentReferable == null || offer == null)
                return false;

            // use pre-definitions
            var defsSg2 = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(
                    new AasxPredefinedConcepts.DefinitionsVDI2770());
            var defsV11 = AasxPredefinedConcepts.VDI2770v11.Static;

            // access Submodel (again)
            var sm = currentReferable as Submodel;
            if (sm == null || sm.SubmodelElements == null ||
                    true != sm.SemanticId.GetAsExactlyOneKey()?.Matches(defsSg2.SM_VDI2770_Documentation.SemanticId.GetAsExactlyOneKey()))
                /* disable line above to allow more models, such as MCAD/ECAD */
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smcOldSg2 = sm.SubmodelElements;
            sm.SubmodelElements = new List<AasCore.Aas3_0_RC02.ISubmodelElement>();
            sm.SemanticId = new AasCore.Aas3_0_RC02.Reference(ReferenceTypes.ModelReference, new List<AasCore.Aas3_0_RC02.Key>() { defsV11.SM_ManufacturerDocumentation.SemanticId.GetAsExactlyOneKey() });

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
                foreach (var rf in defsV11.GetAllReferables())
                    if (rf is ConceptDescription conceptDescription)
                        package.AasEnv.ConceptDescriptions.AddConceptDescriptionOrReturnExisting(
                            new ConceptDescription(
                                conceptDescription.Id, conceptDescription.Extensions, 
                                conceptDescription.Category, conceptDescription.IdShort, 
                                conceptDescription.DisplayName, conceptDescription.Description, 
                                conceptDescription.Checksum, conceptDescription.Administration, 
                                conceptDescription.EmbeddedDataSpecifications, 
                                conceptDescription.IsCaseOf));

            // ok, go thru the old == SG2 records
            foreach (var smcDoc in smcOldSg2.FindAllSemanticIdAs<AasCore.Aas3_0_RC02.SubmodelElementCollection>(
                        defsSg2.CD_VDI2770_Document.GetSingleKey()))
            {
                // access
                if (smcDoc == null || smcDoc.Value == null)
                    continue;

                // look immediately for DocumentVersion, as only with this there is a valid List item
                foreach (var smcVer in smcDoc.Value.FindAllSemanticIdAs<AasCore.Aas3_0_RC02.SubmodelElementCollection>(
                            defsSg2.CD_VDI2770_DocumentVersion.GetSingleKey()))
                {
                    // access
                    if (smcVer == null || smcVer.Value == null)
                        continue;

                    // make new V11 Document
                    // ReSharper disable once ConvertToUsingDeclaration
                    // Document Item
                    //using (var smcV11Doc = AasCore.Aas3_0_RC02.SubmodelElementCollection.CreateNew("" + smcDoc.IdShort,
                    //            smcDoc.Category,
                    //            AasCore.Aas3_0_RC02.Key.GetFromRef(defsV11.CD_Document.GetCdReference())))
                    var smcV11Doc = new AasCore.Aas3_0_RC02.SubmodelElementCollection(idShort: "" + smcDoc.IdShort, category: smcDoc.Category, semanticId: defsV11.CD_Document.GetCdReference());
                    {
                        // Document itself
                        smcV11Doc.Description = smcDoc.Description;
                        sm.SubmodelElements.Add(smcV11Doc);

                        // Domain?
                        var s1 = smcDoc.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                    defsSg2.CD_VDI2770_DomainId.GetSingleKey(),
                                    MatchMode.Relaxed)?.Value;
                        var s2 = smcDoc.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                    defsSg2.CD_VDI2770_DocumentId.GetSingleKey(),
                                    MatchMode.Relaxed)?.Value;
                        if (s1 != null || s2 != null)
                        {
                            var smcV11Dom = smcV11Doc.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.SubmodelElementCollection>(
                                defsV11.CD_DocumentDomainId, addSme: true);

                            var prop = smcV11Dom.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                                defsV11.CD_DocumentDomainId, addSme: true);
                            prop.Value = "" + s1;

                            prop = smcV11Dom.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                                defsV11.CD_DocumentId, addSme: true);
                            prop.Value = "" + s2;
                        }

                        // Classification (3 properties)
                        s1 = smcDoc.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                    defsSg2.CD_VDI2770_DocumentClassificationSystem.GetSingleKey(),
                                    MatchMode.Relaxed)?.Value;
                        s2 = smcDoc.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                    defsSg2.CD_VDI2770_DocumentClassId.GetSingleKey(),
                                    MatchMode.Relaxed)?.Value;
                        var s3 = smcDoc.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                    defsSg2.CD_VDI2770_DocumentClassName.GetSingleKey(),
                                    MatchMode.Relaxed)?.Value;
                        if (s2 != null || s3 != null)
                        {
                            var smcV11Cls = smcV11Doc.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.SubmodelElementCollection>(
                                defsV11.CD_DocumentClassification, addSme: true);

                            var prop = smcV11Cls.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                                defsV11.CD_ClassificationSystem, addSme: true);
                            prop.Value = "" + s1;

                            prop = smcV11Cls.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                                defsV11.CD_ClassId, addSme: true);
                            prop.Value = "" + s2;

                            prop = smcV11Cls.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                                defsV11.CD_ClassName, addSme: true);
                            prop.Value = "" + s3;
                        }

                        // Document Version
                        var smcV11Ver = smcV11Doc.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.SubmodelElementCollection>(
                                defsV11.CD_DocumentVersion, addSme: true);

                        foreach (var o in smcVer.Value.FindAllSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                defsSg2.CD_VDI2770_Language.GetSingleKey(),
                                MatchMode.Relaxed))
                        {
                            var prop = smcV11Ver.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                                defsV11.CD_Language, addSme: true);
                            prop.Value = "" + o;
                        }

                        var property = smcV11Ver.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                            defsV11.CD_DocumentVersionId, addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                defsSg2.CD_VDI2770_DocumentVersionIdValue.GetSingleKey(),
                                MatchMode.Relaxed)?.Value;

                        var mlp1 = smcVer.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.MultiLanguageProperty>(
                                    defsSg2.CD_VDI2770_Title.GetSingleKey(),
                                    MatchMode.Relaxed);
                        if (mlp1 != null)
                            smcV11Ver.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.MultiLanguageProperty>(
                                defsV11.CD_Title, addSme: true).Value = mlp1.Value;

                        mlp1 = smcVer.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.MultiLanguageProperty>(
                                    defsSg2.CD_VDI2770_Summary.GetSingleKey(),
                                    MatchMode.Relaxed);
                        if (mlp1 != null)
                            smcV11Ver.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.MultiLanguageProperty>(
                                defsV11.CD_Summary, addSme: true).Value = mlp1.Value;

                        mlp1 = smcVer.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.MultiLanguageProperty>(
                                    defsSg2.CD_VDI2770_Keywords.GetSingleKey(),
                                    MatchMode.Relaxed);
                        if (mlp1 != null)
                            smcV11Ver.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.MultiLanguageProperty>(
                                defsV11.CD_KeyWords, addSme: true).Value = mlp1.Value;

                        property = smcV11Ver.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                            defsV11.CD_SetDate, addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                defsSg2.CD_VDI2770_Date.GetSingleKey(),
                                MatchMode.Relaxed)?.Value;

                        property = smcV11Ver.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                            defsV11.CD_StatusValue, addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                defsSg2.CD_VDI2770_StatusValue.GetSingleKey(),
                                MatchMode.Relaxed)?.Value;

                        property = smcV11Ver.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                            defsV11.CD_OrganizationName, addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                defsSg2.CD_VDI2770_OrganizationName.GetSingleKey(),
                                MatchMode.Relaxed)?.Value;

                        property = smcV11Ver.Value.CreateSMEForCD<AasCore.Aas3_0_RC02.Property>(
                            defsV11.CD_OrganizationOfficialName, addSme: true);
                        property.Value = "" + smcVer.Value.FindFirstSemanticIdAs<AasCore.Aas3_0_RC02.Property>(
                                defsSg2.CD_VDI2770_OrganizationOfficialName.GetSingleKey(),
                                MatchMode.Relaxed)?.Value;

                        foreach (var o in smcVer.Value.FindAllSemanticIdAs<File>(
                                defsSg2.CD_VDI2770_DigitalFile.GetSingleKey(),
                                MatchMode.Relaxed))
                        {
                            var file = smcV11Ver.Value.CreateSMEForCD<File>(
                                defsV11.CD_DigitalFile, addSme: true);
                            file.ContentType = o.ContentType;
                            file.Value = o.Value;
                        }
                    }
                }
            }

            // obviously well
            return true;
        }
    }
}
