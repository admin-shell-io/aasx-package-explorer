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
using AdminShellNS.Extenstions;
using Extensions;

// ReSharper disable MergeIntoPattern

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

        public override List<ConvertOfferBase> CheckForOffers(IReferable currentReferable)
        {
            // collectResults
            var res = new List<ConvertOfferBase>();

            // use pre-definitions
            var defs = new AasxPredefinedConcepts.DefinitionsZveiTechnicalData.SetOfDefs(
                    new AasxPredefinedConcepts.DefinitionsZveiTechnicalData());

            var sm = currentReferable as Submodel;
            if (sm != null && true == sm.SemanticId.GetAsExactlyOneKey()?.Matches(defs.SM_TechnicalData.SemanticId.GetAsExactlyOneKey()))
                res.Add(new ConvertOfferTechnicalDataV10ToV11(this,
                            $"Convert Submodel '{"" + sm.IdShort}' for Technical Data (ZVEI) V1.0 to V1.1"));

            return res;
        }

        private void RecurseToCopyTechnicalProperties(
            AasxPredefinedConcepts.ZveiTechnicalDataV11 defsV11,
            AasxPredefinedConcepts.DefinitionsZveiTechnicalData.SetOfDefs defsV10,
            SubmodelElementCollection smcDest,
            SubmodelElementCollection smcSrc)
        {
            // access
            if (defsV10 == null || defsV11 == null || smcDest?.Value == null || smcSrc?.Value == null)
                return;

            // for EACH property
            foreach (var sme in smcSrc.Value)
            {
                // access
                if (sme == null)
                    continue;

                var special = false;

                // Submodel Handling
                if (sme is SubmodelElementCollection smcSectSrc)
                {
                    // what to create?
                    SubmodelElementCollection smcSectDst = null;

                    if (smcSectSrc.SemanticId?.MatchesExactlyOneKey(defsV10.CD_MainSection.GetSingleKey()) == true)
                        smcSectDst = smcDest.Value.CreateSMEForCD<SubmodelElementCollection>(
                            defsV11.CD_MainSection, addSme: false);

                    if (smcSectSrc.SemanticId?.MatchesExactlyOneKey(defsV10.CD_SubSection.GetSingleKey()) == true)
                        smcSectDst = smcDest.Value.CreateSMEForCD<SubmodelElementCollection>(
                            defsV11.CD_SubSection, addSme: false);

                    //smcSectDst ??= new SubmodelElementCollection(smcSectSrc, shallowCopy: true);
                    smcSectDst ??= smcSectSrc.Copy();

                    //jtikekar: no need to add manually, should be taken care by cloning above.
                    // add manually
                    //smcSectDst.IdShort = smcSectSrc.IdShort;
                    //smcSectDst.Category = smcSectSrc.Category;
                    //if (smcSectSrc.Description != null)
                    //    smcSectDst.Description = smcSectSrc.Description;

                    smcDest.Value.Add(smcSectDst);

                    // recurse
                    RecurseToCopyTechnicalProperties(defsV11, defsV10, smcSectDst, smcSectSrc);

                    // was special
                    special = true;
                }

                if (!special)
                {
                    // just move "by hand", as the old SMEs are already detached
                    smcDest.Add(sme);

                    // do some fix for "non-standardized"
                    if (sme.SemanticId?
                        .MatchesExactlyOneKey(defsV10.CD_NonstandardizedProperty.GetSingleKey(),
                            MatchMode.Relaxed) == true)
                    {
                        // fix
                        sme.SemanticId = defsV11.CD_SemanticIdNotAvailable.GetReference();
                    }
                }
            }
        }

        public override bool ExecuteOffer(AdminShellPackageEnv package, IReferable currentReferable,
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
            var sm = currentReferable as Submodel;
            if (sm == null || sm.SubmodelElements == null ||
                    true != sm.SemanticId.GetAsExactlyOneKey()?.Matches(defsV10.SM_TechnicalData.SemanticId.GetAsExactlyOneKey()))
                /* disable line above to allow more models, such as MCAD/ECAD */
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smcV10 = sm.SubmodelElements;
            sm.SubmodelElements = new List<ISubmodelElement>();
            sm.SemanticId = new Reference(ReferenceTypes.ModelReference, new List<Key>() { defsV11.SM_TechnicalData.SemanticId.GetAsExactlyOneKey() });

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
                        package.AasEnv.ConceptDescriptions.AddConceptDescription(
                                new ConceptDescription(conceptDescription.Id, conceptDescription.Extensions, conceptDescription.Category, conceptDescription.IdShort, conceptDescription.DisplayName, conceptDescription.Description, conceptDescription.Checksum, conceptDescription.Administration, conceptDescription.DataSpecifications, conceptDescription.IsCaseOf));

            // General Info (target cardinality: 1)
            foreach (var smcV10gi in smcV10.FindAllSemanticIdAs<SubmodelElementCollection>(
                        defsV10.CD_GeneralInformation.GetSingleKey()))
            {
                // make a new one
                var smcV11gi = sm.SubmodelElements.CreateSMEForCD<SubmodelElementCollection>(
                                defsV11.CD_GeneralInformation, addSme: true);

                // SME
                smcV11gi.Value.CopyOneSMEbyCopy<Property>(defsV11.CD_ManufacturerName,
                    smcV10gi.Value, defsV10.CD_ManufacturerName,
                    createDefault: true, addSme: true);

                smcV11gi.Value.CopyOneSMEbyCopy<File>(defsV11.CD_ManufacturerLogo,
                    smcV10gi.Value, defsV10.CD_ManufacturerLogo,
                    createDefault: true, addSme: true);

                smcV11gi.Value.CopyOneSMEbyCopy<Property>(defsV11.CD_ManufacturerPartNumber,
                    smcV10gi.Value, defsV10.CD_ManufacturerPartNumber,
                    createDefault: true, addSme: true);

                smcV11gi.Value.CopyOneSMEbyCopy<Property>(defsV11.CD_ManufacturerOrderCode,
                    smcV10gi.Value, defsV10.CD_ManufacturerOrderCode,
                    createDefault: true, addSme: true);

                smcV11gi.Value.CopyManySMEbyCopy<File>(defsV11.CD_ProductImage,
                    smcV10gi.Value, defsV10.CD_ProductImage,
                    createDefault: true);
            }

            // Product Classifications (target cardinality: 1)
            foreach (var smcV10pcs in smcV10.FindAllSemanticIdAs<SubmodelElementCollection>(
                        defsV10.CD_ProductClassifications.GetSingleKey()))
            {
                // make a new one
                var smcV11pcs = sm.SubmodelElements.CreateSMEForCD<SubmodelElementCollection>(
                                defsV11.CD_ProductClassifications, addSme: true);

                // Product Classification Items (target cardinality: 1..n)
                foreach (var smcV10pci in smcV10pcs.Value.FindAllSemanticIdAs<SubmodelElementCollection>(
                            defsV10.CD_ProductClassificationItem.GetSingleKey()))
                {
                    // make a new one
                    var smcV11pci = smcV11pcs.Value.CreateSMEForCD<SubmodelElementCollection>(
                                defsV11.CD_ProductClassificationItem, addSme: true);

                    // SME
                    smcV11pci.Value.CopyOneSMEbyCopy<Property>(defsV11.CD_ProductClassificationSystem,
                        smcV10pci.Value, defsV10.CD_ClassificationSystem,
                        createDefault: true, addSme: true);

                    smcV11pci.Value.CopyOneSMEbyCopy<Property>(defsV11.CD_ClassificationSystemVersion,
                        smcV10pci.Value, defsV10.CD_SystemVersion,
                        createDefault: true, addSme: true);

                    smcV11pci.Value.CopyOneSMEbyCopy<Property>(defsV11.CD_ProductClassId,
                        smcV10pci.Value, defsV10.CD_ProductClass,
                        createDefault: true, addSme: true);
                }
            }

            // TechnicalProperties (target cardinality: 1)
            foreach (var smcV10prop in smcV10.FindAllSemanticIdAs<SubmodelElementCollection>(
                        defsV10.CD_TechnicalProperties.GetSingleKey()))
            {
                // make a new one
                var smcV11prop = sm.SubmodelElements.CreateSMEForCD<SubmodelElementCollection>(
                                defsV11.CD_TechnicalProperties, addSme: true);

                // use recursion
                RecurseToCopyTechnicalProperties(defsV11, defsV10, smcV11prop, smcV10prop);
            }

            // Further Info (target cardinality: 1)
            foreach (var smcV10fi in smcV10.FindAllSemanticIdAs<SubmodelElementCollection>(
                        defsV10.CD_FurtherInformation.GetSingleKey()))
            {
                // make a new one
                var smcV11fi = sm.SubmodelElements.CreateSMEForCD<SubmodelElementCollection>(
                                defsV11.CD_FurtherInformation, addSme: true);

                // SME
                smcV11fi.Value.CopyManySMEbyCopy<MultiLanguageProperty>(defsV11.CD_TextStatement,
                    smcV10fi.Value, defsV10.CD_TextStatement,
                    createDefault: true);

                smcV11fi.Value.CopyOneSMEbyCopy<Property>(defsV11.CD_ValidDate,
                    smcV10fi.Value, defsV10.CD_ValidDate,
                    createDefault: true, addSme: true);
            }

            // obviously well
            return true;
        }
    }
}
