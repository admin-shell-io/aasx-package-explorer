using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

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

        public override List<ConvertOfferBase> CheckForOffers(AdminShell.Referable currentReferable)
        {
            // collectResults
            var res = new List<ConvertOfferBase>();

            // use pre-definitions
            var defs = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(
                    new AasxPredefinedConcepts.DefinitionsVDI2770());

            var sm = currentReferable as AdminShell.Submodel;
            if (sm != null && true == sm.GetSemanticKey()?.Matches(defs.SM_VDI2770_Documentation.GetSemanticKey()))
                res.Add(new ConvertOfferDocumentationSg2ToHsu(this,
                            $"Convert Submodel '{"" + sm.idShort}' for Documentation SG2 to HSU"));

            // MIHO, 2020-07-31: temporary have code to allow conversion of Festo MCAD / ECAD models as well
            //// if (sm != null && true == sm.GetSemanticKey()?.Matches("Submodel", false, "IRI", 
            ////       "http://smart.festo.com/AAS/Submodel/ComputerAidedDesign/1/0", AdminShell.Key.MatchMode.Relaxed))
            ////     res.Add(new ConvertOfferDocumentationSg2ToHsu(this, 
            ////       $"Convert Submodel '{"" + sm.idShort}' for Documentation SG2 to HSU"));

            return res;
        }

        public override bool ExecuteOffer(AdminShellPackageEnv package, AdminShell.Referable currentReferable,
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
            var sm = currentReferable as AdminShell.Submodel;
            if (sm == null || sm.submodelElements == null ||
                    true != sm.GetSemanticKey()?.Matches(defsSg2.SM_VDI2770_Documentation.GetSemanticKey()))
                /* disable line above to allow more models, such as MCAD/ECAD */
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smcOldSg2 = sm.submodelElements;
            sm.submodelElements = new AdminShell.SubmodelElementWrapperCollection();
            sm.semanticId = new AdminShell.SemanticId(defsHsu.SM_Document.GetSemanticKey());

            // delete (old) CDs
            if (deleteOldCDs)
            {
                smcOldSg2.RecurseOnSubmodelElements(null, null, (state, parents, current) =>
                {
                    var sme = current;
                    if (sme != null && sme.semanticId != null)
                    {
                        var cd = package.AasEnv.FindConceptDescription(sme.semanticId);
                        if (cd != null)
                            if (package.AasEnv.ConceptDescriptions.Contains(cd))
                                package.AasEnv.ConceptDescriptions.Remove(cd);
                    }
                });
            }

            // add (all) new CDs?
            if (addNewCDs)
                foreach (var rf in defsHsu.GetAllReferables())
                    if (rf is AdminShell.ConceptDescription)
                        package.AasEnv.ConceptDescriptions.AddIfNew(new AdminShell.ConceptDescription(
                                    rf as AdminShell.ConceptDescription));

            // ok, go thru the old == SG2 records
            foreach (var smcDoc in smcOldSg2.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        defsSg2.CD_VDI2770_Document.GetSingleKey()))
            {
                // access
                if (smcDoc == null || smcDoc.value == null)
                    continue;

                // look immediately for DocumentVersion, as only with this there is a valid List item
                foreach (var smcVer in smcDoc.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                            defsSg2.CD_VDI2770_DocumentVersion.GetSingleKey()))
                {
                    // access
                    if (smcVer == null || smcVer.value == null)
                        continue;

                    // make new HSU Document
                    // ReSharper disable once ConvertToUsingDeclaration
                    // Document Item
                    using (var smcHsuDoc = AdminShell.SubmodelElementCollection.CreateNew("" + smcDoc.idShort,
                                smcDoc.category,
                                AdminShell.Key.GetFromRef(defsHsu.CD_DocumentationItem.GetCdReference())))
                    {
                        // Document itself
                        smcHsuDoc.description = smcDoc.description;
                        sm.submodelElements.Add(smcHsuDoc);

                        // items ..
                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_DocumentType, addSme: true)?.
                            Set("string", "Single");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_DomainId,
                            addSme: true)?.Set("string", "");

                        var b = true == smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                defsSg2.CD_VDI2770_IsPrimaryDocumentId.GetSingleKey())?.IsTrue();
                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_IdType, addSme: true)?.
                            Set("string", b ? "Primary" : "Secondary");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_DocumentId, addSme: true)?.
                            Set("string", "");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_DocumentDomainId,
                            addSme: true)?.Set("string", "");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_Role, addSme: true)?.
                            Set("string", "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                        defsSg2.CD_VDI2770_Role.GetSingleKey())?.value);

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_OrganisationId,
                            addSme: true)?.Set("string", "");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_OrganisationName,
                            addSme: true)?.Set("string", "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                        defsSg2.CD_VDI2770_OrganizationName.GetSingleKey())?.value);

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(
                                defsHsu.CD_VDI2770_OrganisationOfficialName, addSme: true)?.Set("string",
                                    "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                        defsSg2.CD_VDI2770_OrganizationOfficialName.GetSingleKey())?.value);

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_Description,
                                addSme: true)?.Set("string", "");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_DocumentPartId, addSme: true)?.
                            Set("string", "");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_DocumentClassification_ClassId,
                            addSme: true)?.Set("string", "" + smcDoc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsSg2.CD_VDI2770_DocumentClassId.GetSingleKey())?.value);

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_ClassName,
                            addSme: true)?.Set("string", "" + smcDoc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsSg2.CD_VDI2770_DocumentClassName.GetSingleKey())?.value);

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_ClassificationSystem,
                            addSme: true)?.Set("string", "" + smcDoc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsSg2.CD_VDI2770_DocumentClassificationSystem.GetSingleKey())?.value);

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_DocumentVersionId,
                            addSme: true)?.Set("string", "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsSg2.CD_VDI2770_DocumentVersionId.GetSingleKey())?.value);

                        var lcs = "";
                        foreach (var lcp in smcVer.value.FindAllSemanticIdAs<AdminShell.Property>(
                                    defsSg2.CD_VDI2770_Language.GetSingleKey()))
                            lcs += "" + lcp?.value + ",";
                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_DocumentVersion_LanguageCode,
                                addSme: true)?.Set("string", lcs.TrimEnd(','));

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_Title, addSme: true)?.
                            Set("string", "" + smcVer.value.FindFirstSemanticId(
                                        defsSg2.CD_VDI2770_Title.GetSingleKey(),
                                        new[] {
                                            typeof(AdminShell.Property),
                                            typeof(AdminShell.MultiLanguageProperty)
                                        })?.submodelElement?.ValueAsText());

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_Summary, addSme: true)?.
                            Set("string", "" + smcVer.value.FindFirstSemanticId(
                                        defsSg2.CD_VDI2770_Summary.GetSingleKey(),
                                        new[] {
                                            typeof(AdminShell.Property),
                                            typeof(AdminShell.MultiLanguageProperty)
                                        })?.submodelElement?.ValueAsText());

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_Keywords,
                            addSme: true)?.Set("string", "" + smcVer.value.FindFirstSemanticId(
                                    defsSg2.CD_VDI2770_Keywords.GetSingleKey(),
                                    new[] {
                                        typeof(AdminShell.Property),
                                        typeof(AdminShell.MultiLanguageProperty)
                                    })?.submodelElement?.ValueAsText());

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_StatusValue,
                            addSme: true)?.Set("string", "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsSg2.CD_VDI2770_StatusValue.GetSingleKey())?.value);

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_SetDate, addSme: true)?.
                            Set("string", "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                        defsSg2.CD_VDI2770_Date.GetSingleKey())?.value);

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_Purpose, addSme: true)?.
                            Set("string", "");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_BasedOnProcedure,
                            addSme: true)?.Set("string", "");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_Comments,
                            addSme: true)?.Set("string", "");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_ReferencedObject_Type,
                            addSme: true)?.Set("string", "");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(
                            defsHsu.CD_VDI2770_ReferencedObject_RefType, addSme: true)?.Set("string", "");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(
                            defsHsu.CD_VDI2770_ReferencedObject_ObjectId, addSme: true)?.Set("string", "");

                        smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_FileId, addSme: true)?.
                            Set("string", "");

                        var fl = smcVer.value.FindFirstSemanticIdAs<AdminShell.File>(
                                defsSg2.CD_VDI2770_DigitalFile.GetSingleKey());
                        if (fl != null)
                        {
                            smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_FileName,
                                addSme: true)?.Set("string", System.IO.Path.GetFileName("" + fl.value));

                            smcHsuDoc.value.CreateSMEForCD<AdminShell.Property>(defsHsu.CD_VDI2770_FileFormat,
                                addSme: true)?.Set("string", "" + fl.mimeType);

                            smcHsuDoc.value.CreateSMEForCD<AdminShell.File>(defsHsu.CD_File, addSme: true)?.
                                Set("" + fl.mimeType, "" + fl.value);

                        }
                    }
                }
            }

            // obviously well
            return true;
        }
    }
}
