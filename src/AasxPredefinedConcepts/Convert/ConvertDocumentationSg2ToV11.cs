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
using AdminShellNS;

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

        public override List<ConvertOfferBase> CheckForOffers(AdminShell.Referable currentReferable)
        {
            // collectResults
            var res = new List<ConvertOfferBase>();

            // use pre-definitions
            var defs = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(
                    new AasxPredefinedConcepts.DefinitionsVDI2770());

            var sm = currentReferable as AdminShell.Submodel;
            if (sm != null && true == sm.GetSemanticKey()?.Matches(defs.SM_VDI2770_Documentation.GetSemanticKey()))
                res.Add(new ConvertOfferDocumentationSg2ToV11(this,
                            $"Convert Submodel '{"" + sm.idShort}' for Documentation SG2 (V1.0) to V1.1"));

            return res;
        }

        public override bool ExecuteOffer(AdminShellPackageEnv package, AdminShell.Referable currentReferable,
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
            var sm = currentReferable as AdminShell.Submodel;
            if (sm == null || sm.submodelElements == null ||
                    true != sm.GetSemanticKey()?.Matches(defsSg2.SM_VDI2770_Documentation.GetSemanticKey()))
                /* disable line above to allow more models, such as MCAD/ECAD */
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smcOldSg2 = sm.submodelElements;
            sm.submodelElements = new AdminShell.SubmodelElementWrapperCollection();
            sm.semanticId = new AdminShell.SemanticId(defsV11.SM_ManufacturerDocumentation.GetSemanticKey());

            // delete (old) CDs
            if (deleteOldCDs)
            {
                sm.RecurseOnSubmodelElements(null, (state, parents, current) =>
                {
                    var sme = current;
                    if (sme != null && sme.semanticId != null)
                    {
                        var cd = package.AasEnv.FindConceptDescription(sme.semanticId);
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

                    // make new V11 Document
                    // ReSharper disable once ConvertToUsingDeclaration
                    // Document Item
                    using (var smcV11Doc = AdminShell.SubmodelElementCollection.CreateNew("" + smcDoc.idShort,
                                smcDoc.category,
                                AdminShell.Key.GetFromRef(defsV11.CD_Document.GetCdReference())))
                    {
                        // Document itself
                        smcV11Doc.description = smcDoc.description;
                        sm.submodelElements.Add(smcV11Doc);

                        // Domain?
                        var s1 = smcDoc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsSg2.CD_VDI2770_DomainId.GetSingleKey(),
                                    AdminShell.Key.MatchMode.Relaxed)?.value;
                        var s2 = smcDoc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsSg2.CD_VDI2770_DocumentId.GetSingleKey(),
                                    AdminShell.Key.MatchMode.Relaxed)?.value;
                        if (s1 != null || s2 != null)
                        {
                            var smcV11Dom = smcV11Doc.value.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                                defsV11.CD_DocumentDomainId, addSme: true);

                            smcV11Dom.value.CreateSMEForCD<AdminShell.Property>(
                                defsV11.CD_DocumentDomainId, addSme: true)?.Set("string", "" + s1);
                            smcV11Dom.value.CreateSMEForCD<AdminShell.Property>(
                                defsV11.CD_DocumentId, addSme: true)?.Set("string", "" + s2);
                        }

                        // Classification (3 properties)
                        s1 = smcDoc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsSg2.CD_VDI2770_DocumentClassificationSystem.GetSingleKey(),
                                    AdminShell.Key.MatchMode.Relaxed)?.value;
                        s2 = smcDoc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsSg2.CD_VDI2770_DocumentClassId.GetSingleKey(),
                                    AdminShell.Key.MatchMode.Relaxed)?.value;
                        var s3 = smcDoc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsSg2.CD_VDI2770_DocumentClassName.GetSingleKey(),
                                    AdminShell.Key.MatchMode.Relaxed)?.value;
                        if (s2 != null || s3 != null)
                        {
                            var smcV11Cls = smcV11Doc.value.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                                defsV11.CD_DocumentClassification, addSme: true);

                            smcV11Cls.value.CreateSMEForCD<AdminShell.Property>(
                                defsV11.CD_ClassificationSystem, addSme: true)?.Set("string", "" + s1);
                            smcV11Cls.value.CreateSMEForCD<AdminShell.Property>(
                                defsV11.CD_ClassId, addSme: true)?.Set("string", "" + s2);
                            smcV11Cls.value.CreateSMEForCD<AdminShell.Property>(
                                defsV11.CD_ClassName, addSme: true)?.Set("string", "" + s3);
                        }

                        // Document Version
                        var smcV11Ver = smcV11Doc.value.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                                defsV11.CD_DocumentVersion, addSme: true);

                        foreach (var o in smcVer.value.FindAllSemanticIdAs<AdminShell.Property>(
                                defsSg2.CD_VDI2770_Language.GetSingleKey(),
                                AdminShell.Key.MatchMode.Relaxed))
                            smcV11Ver.value.CreateSMEForCD<AdminShell.Property>(
                                defsV11.CD_Language, addSme: true)?.Set("string", "" + o);

                        smcV11Ver.value.CreateSMEForCD<AdminShell.Property>(
                            defsV11.CD_DocumentVersionId, addSme: true)?.
                            Set("string", "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                defsSg2.CD_VDI2770_DocumentVersionIdValue.GetSingleKey(),
                                AdminShell.Key.MatchMode.Relaxed)?.value);

                        var mlp1 = smcVer.value.FindFirstSemanticIdAs<AdminShell.MultiLanguageProperty>(
                                    defsSg2.CD_VDI2770_Title.GetSingleKey(),
                                    AdminShell.Key.MatchMode.Relaxed);
                        if (mlp1 != null)
                            smcV11Ver.value.CreateSMEForCD<AdminShell.MultiLanguageProperty>(
                                defsV11.CD_Title, addSme: true).value = mlp1.value;

                        mlp1 = smcVer.value.FindFirstSemanticIdAs<AdminShell.MultiLanguageProperty>(
                                    defsSg2.CD_VDI2770_Summary.GetSingleKey(),
                                    AdminShell.Key.MatchMode.Relaxed);
                        if (mlp1 != null)
                            smcV11Ver.value.CreateSMEForCD<AdminShell.MultiLanguageProperty>(
                                defsV11.CD_Summary, addSme: true).value = mlp1.value;

                        mlp1 = smcVer.value.FindFirstSemanticIdAs<AdminShell.MultiLanguageProperty>(
                                    defsSg2.CD_VDI2770_Keywords.GetSingleKey(),
                                    AdminShell.Key.MatchMode.Relaxed);
                        if (mlp1 != null)
                            smcV11Ver.value.CreateSMEForCD<AdminShell.MultiLanguageProperty>(
                                defsV11.CD_KeyWords, addSme: true).value = mlp1.value;

                        smcV11Ver.value.CreateSMEForCD<AdminShell.Property>(
                            defsV11.CD_SetDate, addSme: true)?.
                            Set("string", "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                defsSg2.CD_VDI2770_Date.GetSingleKey(),
                                AdminShell.Key.MatchMode.Relaxed)?.value);

                        smcV11Ver.value.CreateSMEForCD<AdminShell.Property>(
                            defsV11.CD_StatusValue, addSme: true)?.
                            Set("string", "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                defsSg2.CD_VDI2770_StatusValue.GetSingleKey(),
                                AdminShell.Key.MatchMode.Relaxed)?.value);

                        smcV11Ver.value.CreateSMEForCD<AdminShell.Property>(
                            defsV11.CD_OrganizationName, addSme: true)?.
                            Set("string", "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                defsSg2.CD_VDI2770_OrganizationName.GetSingleKey(),
                                AdminShell.Key.MatchMode.Relaxed)?.value);

                        smcV11Ver.value.CreateSMEForCD<AdminShell.Property>(
                            defsV11.CD_OrganizationOfficialName, addSme: true)?.
                            Set("string", "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                defsSg2.CD_VDI2770_OrganizationOfficialName.GetSingleKey(),
                                AdminShell.Key.MatchMode.Relaxed)?.value);

                        foreach (var o in smcVer.value.FindAllSemanticIdAs<AdminShell.File>(
                                defsSg2.CD_VDI2770_DigitalFile.GetSingleKey(),
                                AdminShell.Key.MatchMode.Relaxed))
                            smcV11Ver.value.CreateSMEForCD<AdminShell.File>(
                                defsV11.CD_DigitalFile, addSme: true)?.Set(o.mimeType, o.value);
                    }
                }
            }

            // obviously well
            return true;
        }
    }
}
