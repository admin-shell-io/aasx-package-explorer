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
using AasxIntegrationBase;
using AdminShellNS;

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

        public override List<ConvertOfferBase> CheckForOffers(AdminShell.Referable currentReferable)
        {
            // collectResults
            var res = new List<ConvertOfferBase>();

            // use pre-definitions
            var defs = new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate.SetOfDocumentation(
                    new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate());

            var sm = currentReferable as AdminShell.Submodel;
            if (sm != null && true == sm.GetSemanticKey()?.Matches(defs.SM_Document.GetSemanticKey()))
                res.Add(new ConvertOfferDocumentationHsuToSg2(this,
                            $"Convert Submodel '{"" + sm.idShort}' for Documentation HSU to SG2"));

            return res;
        }

        public override bool ExecuteOffer(
            AdminShellPackageEnv package, AdminShell.Referable currentReferable, ConvertOfferBase offerBase,
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
            var sm = currentReferable as AdminShell.Submodel;
            if (sm == null || sm.submodelElements == null ||
                    true != sm.GetSemanticKey()?.Matches(defsHsu.SM_Document.GetSemanticKey()))
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smcOldHsu = sm.submodelElements;
            sm.submodelElements = new AdminShell.SubmodelElementWrapperCollection();
            sm.semanticId = new AdminShell.SemanticId(defsSg2.SM_VDI2770_Documentation.GetSemanticKey());

            // delete (old) CDs
            if (deleteOldCDs)
            {
                smcOldHsu.RecurseOnSubmodelElements(null, null, (state, parents, current) =>
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
                foreach (var rf in defsSg2.GetAllReferables())
                    if (rf is AdminShell.ConceptDescription)
                        package.AasEnv.ConceptDescriptions.AddIfNew(
                                new AdminShell.ConceptDescription(rf as AdminShell.ConceptDescription));

            // ok, go thru the old == HSU records
            foreach (var smcSource in smcOldHsu.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        defsHsu.CD_DocumentationItem.GetSingleKey(), AdminShell.Key.MatchMode.Relaxed))
            {
                // access
                if (smcSource == null || smcSource.value == null)
                    continue;

                // make new SG2 Document + DocumentItem
                // Document Item
                // ReSharper disable once ConvertToUsingDeclaration
                using (var smcDoc = AdminShell.SubmodelElementCollection.CreateNew("" + smcSource.idShort,
                            smcSource.category,
                            AdminShell.Key.GetFromRef(defsSg2.CD_VDI2770_Document.GetCdReference())))
                using (var smcDocVersion = AdminShell.SubmodelElementCollection.CreateNew("DocumentVersion",
                            smcSource.category,
                            AdminShell.Key.GetFromRef(defsSg2.CD_VDI2770_DocumentVersion.GetCdReference())))
                {
                    // Document itself
                    smcDoc.description = smcSource.description;

                    // classification
                    var clid = smcSource.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            defsHsu.CD_DocumentClassification_ClassId.GetSingleKey())?.value;
                    var clname = "" + smcSource.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            defsHsu.CD_VDI2770_ClassName.GetSingleKey())?.value;
                    var clsys = "" + smcSource.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            defsHsu.CD_VDI2770_ClassificationSystem.GetSingleKey())?.value;

#if future_structure
                    // as described in the VDI 2770 Submodel template document
                    if (clid.HasContent())
                        using (var smcClass = AdminShell.SubmodelElementCollection.CreateNew("DocumentClassification",
                                    smcSource.category, AdminShell.Key.GetFromRef(defsSg2.CD_XXX.GetReference())))
                        {
                            smcDoc.Add(smcClass);

                            smcClass.value.CreateSMEForCD<AdminShell.Property>(defsSg2.CD_VDI2770_DocumentClassId,
                                addSme: true)?.Set("string", "" + clid);
                            smcClass.value.CreateSMEForCD<AdminShell.Property>(defsSg2.CD_VDI2770_DocumentClassName,
                                addSme: true)?.Set("string", "" + clname);
                            smcClass.value.CreateSMEForCD<AdminShell.Property>(
                                defsSg2.CD_VDI2770_DocumentClassificationSystem, addSme: true)?
                                .Set("string", "" + clsys);
                        }

#else
                    // current state of code
                    smcDoc.value.CreateSMEForCD<AdminShell.Property>(defsSg2.CD_VDI2770_DocumentClassId,
                            addSme: true)?.Set("string", "" + clid);
                    smcDoc.value.CreateSMEForCD<AdminShell.Property>(defsSg2.CD_VDI2770_DocumentClassName,
                            addSme: true)?.Set("string", "" + clname);
                    smcDoc.value.CreateSMEForCD<AdminShell.Property>(defsSg2.CD_VDI2770_DocumentClassificationSystem,
                            addSme: true)?.Set("string", "" + clsys);
#endif

                    // items ..
                    smcDoc.value.CreateSMEForCD<AdminShell.Property>(defsSg2.CD_VDI2770_DocumentId, addSme: true)?.
                        Set("string", "" + smcSource.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsHsu.CD_DocumentId.GetSingleKey())?.value);

                    var idt = "" + smcSource.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            defsHsu.CD_VDI2770_IdType.GetSingleKey())?.IsTrue();
                    smcDoc.value.CreateSMEForCD<AdminShell.Property>(defsSg2.CD_VDI2770_IsPrimaryDocumentId,
                            addSme: true)?.Set("boolean", (idt.Trim().ToLower() == "primary") ? "True" : "False");

                    smcDoc.value.CreateSMEForCD<AdminShell.ReferenceElement>(defsSg2.CD_VDI2770_ReferencedObject,
                            addSme: true)?.
                        Set(new AdminShell.Reference());

                    // DocumentVersion

                    // languages
                    var lcs = "" + smcSource.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            defsHsu.CD_DocumentVersion_LanguageCode.GetSingleKey())?.value;
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
                            smcDocVersion.value.CreateSMEForCD<AdminShell.Property>(defsSg2.CD_VDI2770_Language,
                                    idShort: $"Language{(i++):00}", addSme: true)?.
                                Set("string", "" + lcc);
                        }
                    }
                    //ReSharper enable ConditionIsAlwaysTrueOrFalse

                    smcDocVersion.value.CreateSMEForCD<AdminShell.Property>(defsSg2.CD_VDI2770_DocumentVersionId,
                        addSme: true)?.
                        Set("string", "" + smcSource.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsHsu.CD_DocumentVersionId.GetSingleKey())?.value);

                    var cdSrc = new[] { defsHsu.CD_VDI2770_Title, defsHsu.CD_VDI2770_Summary,
                        defsHsu.CD_VDI2770_Keywords };
                    var cdDst = new[] { defsSg2.CD_VDI2770_Title, defsSg2.CD_VDI2770_Summary,
                        defsSg2.CD_VDI2770_Keywords };
                    for (int i = 0; i < 3; i++)
                    {
                        var target = smcDocVersion.value.CreateSMEForCD<AdminShell.MultiLanguageProperty>(cdDst[i],
                                addSme: true);
                        if (target == null)
                            continue;

                        var asProp = smcSource.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                cdSrc[i].GetSingleKey());
                        if (asProp != null)
                            target.Set("en?", "" + asProp.value);

                        var asMLP = smcSource.value.FindFirstSemanticIdAs<AdminShell.MultiLanguageProperty>(
                                cdSrc[i].GetSingleKey());
                        if (asMLP != null)
                            target.value = asMLP.value;
                    }

                    smcDocVersion.value.CreateSMEForCD<AdminShell.Property>(defsSg2.CD_VDI2770_Date, addSme: true)?.
                        Set("string", "" + smcSource.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsHsu.CD_VDI2770_SetDate.GetSingleKey())?.value);

                    smcDocVersion.value.CreateSMEForCD<AdminShell.Property>(defsSg2.CD_VDI2770_Role, addSme: true)?.
                        Set("string", "" + smcSource.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsHsu.CD_VDI2770_Role.GetSingleKey())?.value);

                    smcDocVersion.value.CreateSMEForCD<AdminShell.Property>(defsSg2.CD_VDI2770_OrganizationName,
                        addSme: true)?.Set("string", "" + smcSource.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defsHsu.CD_VDI2770_OrganisationName.GetSingleKey())?.value);

                    smcDocVersion.value.CreateSMEForCD<AdminShell.Property>(
                        defsSg2.CD_VDI2770_OrganizationOfficialName, addSme: true)?.Set("string",
                            "" + smcSource.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                defsHsu.CD_VDI2770_OrganisationOfficialName.GetSingleKey())?.value);

                    // 1 file?
                    var fl = smcSource.value.FindFirstSemanticIdAs<AdminShell.File>(defsHsu.CD_File.GetSingleKey());
                    smcDocVersion.value.CreateSMEForCD<AdminShell.File>(defsSg2.CD_VDI2770_DigitalFile, addSme: true)?.
                            Set("" + fl?.mimeType, "" + fl?.value);

                    // finally, add
                    smcDoc.Add(smcDocVersion);
                    sm.submodelElements.Add(smcDoc);

                }
            }

            // obviously well
            return true;
        }
    }
}
