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
    public class ConvertNameplateHsuToZveiV10Provider : ConvertProviderBase
    {
        public class ConvertOfferNameplateHsuToZveiV10 : ConvertOfferBase
        {
            public ConvertOfferNameplateHsuToZveiV10() { }
            public ConvertOfferNameplateHsuToZveiV10(ConvertProviderBase provider, string offerDisp)
                : base(provider, offerDisp) { }
        }

        public override List<ConvertOfferBase> CheckForOffers(IReferable currentReferable)
        {
            // collectResults
            var res = new List<ConvertOfferBase>();

            // use pre-definitions
            var defs = new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate.SetOfNameplate(
                    new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate());

            var sm = currentReferable as Submodel;
            if (sm != null && true == sm.SemanticId.GetAsExactlyOneKey()?.Matches(defs.SM_Nameplate.SemanticId.GetAsExactlyOneKey(),
                MatchMode.Relaxed))
                res.Add(new ConvertOfferNameplateHsuToZveiV10(this,
                            $"Convert Submodel '{"" + sm.IdShort}' for Digital Nameplate HSU to ZVEI V1.0"));

            return res;
        }

        public override bool ExecuteOffer(AdminShellPackageEnv package, IReferable currentReferable,
                ConvertOfferBase offerBase, bool deleteOldCDs, bool addNewCDs)
        {
            // access
            var offer = offerBase as ConvertOfferNameplateHsuToZveiV10;
            if (package == null || package.AasEnv == null || currentReferable == null || offer == null)
                return false;

            // use pre-definitions
            var defsHSU = new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate.SetOfNameplate(
                    new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate());
            var defsV10 = AasxPredefinedConcepts.ZveiNameplateV10.Static;

            // access Submodel (again)
            var sm = currentReferable as Submodel;
            if (sm == null || sm.SubmodelElements == null ||
                    true != sm.SemanticId.GetAsExactlyOneKey()?.Matches(defsHSU.SM_Nameplate.SemanticId.GetAsExactlyOneKey()))
                /* disable line above to allow more models, such as MCAD/ECAD */
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smHSU = sm.SubmodelElements;
            sm.SubmodelElements = new List<ISubmodelElement>();
            sm.SemanticId = new Reference(ReferenceTypes.ModelReference, new List<Key>() { defsV10.SM_Nameplate.SemanticId.GetAsExactlyOneKey() });

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
                foreach (var rf in defsV10.GetAllReferables())
                    if (rf is ConceptDescription conceptDescription)
                        package.AasEnv.ConceptDescriptions.AddConceptDescriptionOrReturnExisting(
                            new ConceptDescription(
                                conceptDescription.Id, conceptDescription.Extensions, 
                                conceptDescription.Category, conceptDescription.IdShort, 
                                conceptDescription.DisplayName, conceptDescription.Description, 
                                conceptDescription.Checksum, conceptDescription.Administration, 
                                conceptDescription.EmbeddedDataSpecifications, 
                                conceptDescription.IsCaseOf));

            // Submodel level

            sm.SubmodelElements.CopyOneSMEbyCopy<Property>(defsV10.CD_ManNam,
                smHSU, defsHSU.CD_ManufacturerName,
                createDefault: true, addSme: true, idShort: "ManufacturerName");

            sm.SubmodelElements.CopyOneSMEbyCopy<Property>(defsV10.CD_ManProDes,
                smHSU, defsHSU.CD_ManufacturerProductDesignation,
                createDefault: true, addSme: true, idShort: "ManufacturerProductDesignation");

            // Address (target cardinality: 1)
            foreach (var smcHSUadd in smHSU.FindAllSemanticIdAs<SubmodelElementCollection>(
                        defsHSU.CD_PhysicalAddress.GetSingleKey()))
            {
                // make a new one
                var smcV10add = sm.SubmodelElements.CreateSMEForCD<SubmodelElementCollection>(
                                defsV10.CD_Add, idShort: "Address", addSme: true);

                // SME
                smcV10add.Value.CopyOneSMEbyCopy<Property>(defsV10.CD_Str,
                    smcHSUadd.Value, new[] {
                        defsHSU.CD_Street.GetSingleKey(),
                    new Key(KeyTypes.ConceptDescription,
                        "https://www.hsu-hh.de/aut/aas/street")},
                    createDefault: true, addSme: true, idShort: "Street");

                smcV10add.Value.CopyOneSMEbyCopy<Property>(defsV10.CD_ZipCod,
                    smcHSUadd.Value, new[] {
                        defsHSU.CD_Zip.GetSingleKey(),
                    new Key(KeyTypes.ConceptDescription,
                        "https://www.hsu-hh.de/aut/aas/postalcode")},
                    createDefault: true, addSme: true, idShort: "Zipcode");

                smcV10add.Value.CopyOneSMEbyCopy<Property>(defsV10.CD_CitTow,
                    smcHSUadd.Value, new[] {
                        defsHSU.CD_CityTown.GetSingleKey(),
                    new Key(KeyTypes.ConceptDescription,
                        "https://www.hsu-hh.de/aut/aas/city")},
                    createDefault: true, addSme: true, idShort: "CityTown");

                smcV10add.Value.CopyOneSMEbyCopy<Property>(defsV10.CD_StaCou,
                    smcHSUadd.Value, new[] {
                        defsHSU.CD_StateCounty.GetSingleKey(),
                    new Key(KeyTypes.ConceptDescription,
                        "https://www.hsu-hh.de/aut/aas/statecounty")},
                    createDefault: true, addSme: true, idShort: "StateCounty");

                smcV10add.Value.CopyOneSMEbyCopy<Property>(defsV10.CD_NatCod,
                    smcHSUadd.Value, defsHSU.CD_CountryCode,
                    createDefault: true, addSme: true, idShort: "NationalCode");
            }

            // Submodel level - continued

            sm.SubmodelElements.CopyOneSMEbyCopy<Property>(defsV10.CD_ManProFam,
                smHSU, defsHSU.CD_ManufacturerProductFamily,
                createDefault: true, addSme: true, idShort: "ManufacturerProductFamily");

            sm.SubmodelElements.CopyOneSMEbyCopy<Property>(defsV10.CD_SerNum,
                smHSU, defsHSU.CD_SerialNumber,
                createDefault: true, addSme: true, idShort: "SerialNumber");

            sm.SubmodelElements.CopyOneSMEbyCopy<Property>(defsV10.CD_YeaOfCon,
                smHSU, defsHSU.CD_YearOfConstruction,
                createDefault: true, addSme: true, idShort: "YearOfConstruction");

            // Markings 
            var smcV10mks = sm.SubmodelElements.CreateSMEForCD<SubmodelElementCollection>(
                            defsV10.CD_Markings, idShort: "Markings", addSme: true);

            // each Marking
            foreach (var smcHSUmk in smHSU.FindAllSemanticIdAs<SubmodelElementCollection>(
                        defsHSU.CD_ProductMarking.GetSingleKey()))
            {
                // make a new one
                var smcV10mk = smcV10mks.Value.CreateSMEForCD<SubmodelElementCollection>(
                                defsV10.CD_Marking, idShort: "" + smcHSUmk.IdShort, addSme: true);

                // take over the name of the old collection in the distinct Property
                var mkName = "" + smcHSUmk.IdShort;
                if (mkName.StartsWith("Marking_"))
                    mkName = mkName.Substring(8);
                var mkNameProp = smcV10mk.Value.CreateSMEForCD<Property>(
                                    defsV10.CD_MarkingName, idShort: "MarkingName", addSme: true);
                mkNameProp.Value = "" + mkName;

                // file
                smcV10mk.Value.CopyOneSMEbyCopy<File>(defsV10.CD_MarkingFile,
                    smcHSUmk.Value, defsHSU.CD_File,
                    createDefault: true, addSme: true, idShort: "ManufacturerName");

                // if there a other Property inside, assume, that their semantic ids shall
                // go into the valueId of the Name

                foreach (var other in smcHSUmk.Value.FindAll((smw) => smw is Property))
                    if (mkNameProp != null
                        && other?.SemanticId != null
                        && !other.SemanticId.IsEmpty())
                    {
                        mkNameProp.ValueId = other.SemanticId;
                    }
            }

            // obviously well
            return true;
        }
    }
}
