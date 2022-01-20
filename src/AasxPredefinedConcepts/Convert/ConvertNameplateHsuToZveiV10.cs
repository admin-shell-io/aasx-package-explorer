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
    public class ConvertNameplateHsuToZveiV10Provider : ConvertProviderBase
    {
        public class ConvertOfferNameplateHsuToZveiV10 : ConvertOfferBase
        {
            public ConvertOfferNameplateHsuToZveiV10() { }
            public ConvertOfferNameplateHsuToZveiV10(ConvertProviderBase provider, string offerDisp)
                : base(provider, offerDisp) { }
        }

        public override List<ConvertOfferBase> CheckForOffers(AdminShell.Referable currentReferable)
        {
            // collectResults
            var res = new List<ConvertOfferBase>();

            // use pre-definitions
            var defs = new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate.SetOfNameplate(
                    new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate());

            var sm = currentReferable as AdminShell.Submodel;
            if (sm != null && true == sm.GetAutoSingleKey()?.Matches(defs.SM_Nameplate.GetAutoSingleKey()))
                res.Add(new ConvertOfferNameplateHsuToZveiV10(this,
                            $"Convert Submodel '{"" + sm.idShort}' for Digital Nameplate HSU to ZVEI V1.0"));

            return res;
        }

        public override bool ExecuteOffer(AdminShellPackageEnv package, AdminShell.Referable currentReferable,
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
            var sm = currentReferable as AdminShell.Submodel;
            if (sm == null || sm.submodelElements == null ||
                    true != sm.GetAutoSingleKey()?.Matches(defsHSU.SM_Nameplate.GetAutoSingleKey()))
                /* disable line above to allow more models, such as MCAD/ECAD */
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smHSU = sm.submodelElements;
            sm.submodelElements = new AdminShell.SubmodelElementWrapperCollection();
            sm.semanticId = new AdminShell.SemanticId(defsV10.SM_Nameplate.GetAutoSingleId());

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
                foreach (var rf in defsV10.GetAllReferables())
                    if (rf is AdminShell.ConceptDescription)
                        package.AasEnv.ConceptDescriptions.AddIfNew(new AdminShell.ConceptDescription(
                                    rf as AdminShell.ConceptDescription));

            // Submodel level

            sm.submodelElements.CopyOneSMEbyCopy<AdminShell.Property>(defsV10.CD_ManNam,
                smHSU, defsHSU.CD_ManufacturerName,
                createDefault: true, addSme: true, idShort: "ManufacturerName");

            sm.submodelElements.CopyOneSMEbyCopy<AdminShell.Property>(defsV10.CD_ManProDes,
                smHSU, defsHSU.CD_ManufacturerProductDesignation,
                createDefault: true, addSme: true, idShort: "ManufacturerProductDesignation");

            // Address (target cardinality: 1)
            foreach (var smcHSUadd in smHSU.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        defsHSU.CD_PhysicalAddress.GetSingleId()))
            {
                // make a new one
                var smcV10add = sm.submodelElements.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                                defsV10.CD_Add, idShort: "Address", addSme: true);

                // SME
                smcV10add.value.CopyOneSMEbyCopy<AdminShell.Property>(defsV10.CD_Str,
                    smcHSUadd.value, new[] {
                        defsHSU.CD_Street.GetSingleId(),
                    new AdminShell.Identifier(
                        "https://www.hsu-hh.de/aut/aas/street")},
                    createDefault: true, addSme: true, idShort: "Street");

                smcV10add.value.CopyOneSMEbyCopy<AdminShell.Property>(defsV10.CD_ZipCod,
                    smcHSUadd.value, new[] {
                        defsHSU.CD_Zip.GetSingleId(),
                        new AdminShell.Identifier("https://www.hsu-hh.de/aut/aas/postalcode")},
                    createDefault: true, addSme: true, idShort: "Zipcode");

                smcV10add.value.CopyOneSMEbyCopy<AdminShell.Property>(defsV10.CD_CitTow,
                    smcHSUadd.value, new[] {
                        defsHSU.CD_CityTown.GetSingleId(),
                        new AdminShell.Identifier("https://www.hsu-hh.de/aut/aas/city")},
                    createDefault: true, addSme: true, idShort: "CityTown");

                smcV10add.value.CopyOneSMEbyCopy<AdminShell.Property>(defsV10.CD_StaCou,
                    smcHSUadd.value, new[] {
                        defsHSU.CD_StateCounty.GetSingleId(),
                        new AdminShell.Identifier("https://www.hsu-hh.de/aut/aas/statecounty")},
                    createDefault: true, addSme: true, idShort: "StateCounty");

                smcV10add.value.CopyOneSMEbyCopy<AdminShell.Property>(defsV10.CD_NatCod,
                    smcHSUadd.value, defsHSU.CD_CountryCode,
                    createDefault: true, addSme: true, idShort: "NationalCode");
            }

            // Submodel level - continued

            sm.submodelElements.CopyOneSMEbyCopy<AdminShell.Property>(defsV10.CD_ManProFam,
                smHSU, defsHSU.CD_ManufacturerProductFamily,
                createDefault: true, addSme: true, idShort: "ManufacturerProductFamily");

            sm.submodelElements.CopyOneSMEbyCopy<AdminShell.Property>(defsV10.CD_SerNum,
                smHSU, defsHSU.CD_SerialNumber,
                createDefault: true, addSme: true, idShort: "SerialNumber");

            sm.submodelElements.CopyOneSMEbyCopy<AdminShell.Property>(defsV10.CD_YeaOfCon,
                smHSU, defsHSU.CD_YearOfConstruction,
                createDefault: true, addSme: true, idShort: "YearOfConstruction");

            // Markings 
            var smcV10mks = sm.submodelElements.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                            defsV10.CD_Markings, idShort: "Markings", addSme: true);

            // each Marking
            foreach (var smcHSUmk in smHSU.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        defsHSU.CD_ProductMarking.GetSingleId()))
            {
                // make a new one
                var smcV10mk = smcV10mks.value.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                                defsV10.CD_Marking, idShort: "" + smcHSUmk.idShort, addSme: true);

                // take over the name of the old collection in the distinct Property
                var mkName = "" + smcHSUmk.idShort;
                if (mkName.StartsWith("Marking_"))
                    mkName = mkName.Substring(8);
                var mkNameProp = smcV10mk.value.CreateSMEForCD<AdminShell.Property>(
                                    defsV10.CD_MarkingName, idShort: "MarkingName", addSme: true)?
                                    .Set(AdminShell.DataElement.ValueType_STRING, "" + mkName);

                // file
                smcV10mk.value.CopyOneSMEbyCopy<AdminShell.File>(defsV10.CD_MarkingFile,
                    smcHSUmk.value, defsHSU.CD_File,
                    createDefault: true, addSme: true, idShort: "ManufacturerName");

                // if there a other Property inside, assume, that their semantic ids shall
                // go into the valueId of the Name

                foreach (var other in smcHSUmk.value.FindAll((smw) => smw?.submodelElement is AdminShell.Property))
                    if (mkNameProp != null
                        && other?.submodelElement?.semanticId != null
                        && !other.submodelElement.semanticId.IsEmpty
                        && other.submodelElement.semanticId[0].IsIRDI())
                    {
                        mkNameProp.valueId = other.submodelElement.semanticId;
                    }
            }

            // obviously well
            return true;
        }
    }
}
