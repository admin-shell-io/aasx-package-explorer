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

namespace AasxPredefinedConcepts.Convert
{
    public class ConvertTechnicalDataV10ToV11Provider : ConvertProviderBase
    {
        public class ConvertOfferTechnicalDataV10ToV11 : ConvertOfferBase
        {
            public ConvertOfferTechnicalDataV10ToV11() { }
            public ConvertOfferTechnicalDataV10ToV11(ConvertProviderBase provider, string offerDisp)
                : base(provider, offerDisp) { }
        }

        public override List<ConvertOfferBase> CheckForOffers(AdminShell.Referable currentReferable)
        {
            // collectResults
            var res = new List<ConvertOfferBase>();

            // use pre-definitions
            var defs = new AasxPredefinedConcepts.DefinitionsZveiTechnicalData.SetOfDefs(
                    new AasxPredefinedConcepts.DefinitionsZveiTechnicalData());

            var sm = currentReferable as AdminShell.Submodel;
            if (sm != null && true == sm.GetSemanticKey()?.Matches(defs.SM_TechnicalData.GetSemanticKey()))
                res.Add(new ConvertOfferTechnicalDataV10ToV11(this,
                            $"Convert Submodel '{"" + sm.idShort}' for Technical Data (ZVEI) V1.0 to V1.1"));

            return res;
        }

        private void RecurseToCopyTechnicalProperties(
            AasxPredefinedConcepts.ZveiTechnicalDataV11 defsV11,
            AasxPredefinedConcepts.DefinitionsZveiTechnicalData.SetOfDefs defsV10,
            AdminShell.SubmodelElementCollection smcDest,
            AdminShell.SubmodelElementCollection smcSrc)
        {
            // access
            if (defsV10 == null || defsV11 == null || smcDest?.value == null || smcSrc?.value == null)
                return;

            // for EACH property
            foreach (var sme in smcSrc.value)
            {
                // access
                if (sme?.submodelElement == null)
                    continue;

                var special = false;

                // Submodel Handling
                if (sme.submodelElement is AdminShell.SubmodelElementCollection smcSectSrc)
                {
                    // what to create?
                    AdminShell.SubmodelElementCollection smcSectDst = null;

                    if (smcSectSrc.semanticId?.Matches(defsV10.CD_MainSection.GetSingleKey()) == true)
                        smcSectDst = smcDest.value.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                            defsV11.CD_MainSection, addSme: false);

                    if (smcSectSrc.semanticId?.Matches(defsV10.CD_SubSection.GetSingleKey()) == true)
                        smcSectDst = smcDest.value.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                            defsV11.CD_SubSection, addSme: false);

                    smcSectDst ??= new AdminShell.SubmodelElementCollection(smcSectSrc, shallowCopy: true);

                    // add manually
                    smcSectDst.idShort = smcSectSrc.idShort;
                    smcSectDst.category = smcSectSrc.category;
                    if (smcSectSrc.description != null)
                        smcSectDst.description = new AdminShell.Description(smcSectSrc.description);
                    smcDest.value.Add(smcSectDst);

                    // recurse
                    RecurseToCopyTechnicalProperties(defsV11, defsV10, smcSectDst, smcSectSrc);

                    // was special
                    special = true;
                }

                if (!special)
                {
                    // just move "by hand", as the old SMEs are already detached
                    smcDest.Add(sme.submodelElement);

                    // do some fix for "non-standardized"
                    if (sme.submodelElement.semanticId?
                        .MatchesExactlyOneKey(defsV10.CD_NonstandardizedProperty.GetSingleKey(),
                            AdminShell.Key.MatchMode.Relaxed) == true)
                    {
                        // fix
                        sme.submodelElement.semanticId = new AdminShell.SemanticId(
                            defsV11.CD_SemanticIdNotAvailable.GetReference());
                    }
                }
            }
        }

        public override bool ExecuteOffer(AdminShellPackageEnv package, AdminShell.Referable currentReferable,
                ConvertOfferBase offerBase, bool deleteOldCDs, bool addNewCDs)
        {
            // access
            var offer = offerBase as ConvertOfferTechnicalDataV10ToV11;
            if (package == null || package.AasEnv == null || currentReferable == null || offer == null)
                return false;

            // use pre-definitions
            var defsV10 = new AasxPredefinedConcepts.DefinitionsZveiTechnicalData.SetOfDefs(
                    new AasxPredefinedConcepts.DefinitionsZveiTechnicalData());
            var defsV11 = AasxPredefinedConcepts.ZveiTechnicalDataV11.Static;

            // access Submodel (again)
            var sm = currentReferable as AdminShell.Submodel;
            if (sm == null || sm.submodelElements == null ||
                    true != sm.GetSemanticKey()?.Matches(defsV10.SM_TechnicalData.GetSemanticKey()))
                /* disable line above to allow more models, such as MCAD/ECAD */
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smcV10 = sm.submodelElements;
            sm.submodelElements = new AdminShell.SubmodelElementWrapperCollection();
            sm.semanticId = new AdminShell.SemanticId(defsV11.SM_TechnicalData.GetSemanticKey());

            // delete (old) CDs
            if (deleteOldCDs)
            {
                smcV10.RecurseOnSubmodelElements(null, null, (state, parents, current) =>
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
                foreach (var rf in defsV11.GetAllReferables())
                    if (rf is AdminShell.ConceptDescription)
                        package.AasEnv.ConceptDescriptions.AddIfNew(new AdminShell.ConceptDescription(
                                    rf as AdminShell.ConceptDescription));

            // General Info (target cardinality: 1)
            foreach (var smcV10gi in smcV10.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        defsV10.CD_GeneralInformation.GetSingleKey()))
            {
                // make a new one
                var smcV11gi = sm.submodelElements.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                                defsV11.CD_GeneralInformation, addSme: true);

                // SME
                smcV11gi.value.CopyOneSMEbyCopy<AdminShell.Property>(defsV11.CD_ManufacturerName,
                    smcV10gi.value, defsV10.CD_ManufacturerName,
                    createDefault: true, addSme: true);

                smcV11gi.value.CopyOneSMEbyCopy<AdminShell.File>(defsV11.CD_ManufacturerLogo,
                    smcV10gi.value, defsV10.CD_ManufacturerLogo,
                    createDefault: true, addSme: true);

                smcV11gi.value.CopyOneSMEbyCopy<AdminShell.Property>(defsV11.CD_ManufacturerPartNumber,
                    smcV10gi.value, defsV10.CD_ManufacturerPartNumber,
                    createDefault: true, addSme: true);

                smcV11gi.value.CopyOneSMEbyCopy<AdminShell.Property>(defsV11.CD_ManufacturerOrderCode,
                    smcV10gi.value, defsV10.CD_ManufacturerOrderCode,
                    createDefault: true, addSme: true);

                smcV11gi.value.CopyManySMEbyCopy<AdminShell.File>(defsV11.CD_ProductImage,
                    smcV10gi.value, defsV10.CD_ProductImage,
                    createDefault: true);
            }

            // Product Classifications (target cardinality: 1)
            foreach (var smcV10pcs in smcV10.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        defsV10.CD_ProductClassifications.GetSingleKey()))
            {
                // make a new one
                var smcV11pcs = sm.submodelElements.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                                defsV11.CD_ProductClassifications, addSme: true);

                // Product Classification Items (target cardinality: 1..n)
                foreach (var smcV10pci in smcV10pcs.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                            defsV10.CD_ProductClassificationItem.GetSingleKey()))
                {
                    // make a new one
                    var smcV11pci = smcV11pcs.value.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                                defsV11.CD_ProductClassificationItem, addSme: true);

                    // SME
                    smcV11pci.value.CopyOneSMEbyCopy<AdminShell.Property>(defsV11.CD_ProductClassificationSystem,
                        smcV10pci.value, defsV10.CD_ClassificationSystem,
                        createDefault: true, addSme: true);

                    smcV11pci.value.CopyOneSMEbyCopy<AdminShell.Property>(defsV11.CD_ClassificationSystemVersion,
                        smcV10pci.value, defsV10.CD_SystemVersion,
                        createDefault: true, addSme: true);

                    smcV11pci.value.CopyOneSMEbyCopy<AdminShell.Property>(defsV11.CD_ProductClassId,
                        smcV10pci.value, defsV10.CD_ProductClass,
                        createDefault: true, addSme: true);
                }
            }

            // TechnicalProperties (target cardinality: 1)
            foreach (var smcV10prop in smcV10.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        defsV10.CD_TechnicalProperties.GetSingleKey()))
            {
                // make a new one
                var smcV11prop = sm.submodelElements.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                                defsV11.CD_TechnicalProperties, addSme: true);

                // use recursion
                RecurseToCopyTechnicalProperties(defsV11, defsV10, smcV11prop, smcV10prop);
            }

            // Further Info (target cardinality: 1)
            foreach (var smcV10fi in smcV10.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        defsV10.CD_FurtherInformation.GetSingleKey()))
            {
                // make a new one
                var smcV11fi = sm.submodelElements.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                                defsV11.CD_FurtherInformation, addSme: true);

                // SME
                smcV11fi.value.CopyManySMEbyCopy<AdminShell.MultiLanguageProperty>(defsV11.CD_TextStatement,
                    smcV10fi.value, defsV10.CD_TextStatement,
                    createDefault: true);

                smcV11fi.value.CopyOneSMEbyCopy<AdminShell.Property>(defsV11.CD_ValidDate,
                    smcV10fi.value, defsV10.CD_ValidDate,
                    createDefault: true, addSme: true);
            }

            // obviously well
            return true;
        }
    }
}
