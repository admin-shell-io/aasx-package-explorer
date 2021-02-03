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
    public class ConvertTechnicalDataToFlatProvider : ConvertProviderBase
    {
        public class ConvertOfferTechnicalDataToFlat : ConvertOfferBase
        {
            public ConvertOfferTechnicalDataToFlat() { }
            public ConvertOfferTechnicalDataToFlat(ConvertProviderBase provider, string offerDisp)
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
                res.Add(new ConvertOfferTechnicalDataToFlat(this,
                        $"Convert Submodel '{"" + sm.idShort}' from Technical Data to flat Submodel"));

            return res;
        }

        public override bool ExecuteOffer(
            AdminShellPackageEnv package, AdminShell.Referable currentReferable,
            ConvertOfferBase offerBase, bool deleteOldCDs, bool addNewCDs)
        {
            // access
            var offer = offerBase as ConvertOfferTechnicalDataToFlat;
            if (package == null || package.AasEnv == null || currentReferable == null || offer == null)
                return false;

            // use pre-definitions
            var defsTD = new AasxPredefinedConcepts.DefinitionsZveiTechnicalData.SetOfDefs(
                                new AasxPredefinedConcepts.DefinitionsZveiTechnicalData());

            // access Submodel (again)
            var sm = currentReferable as AdminShell.Submodel;
            if (sm == null || sm.submodelElements == null
                || true != sm.GetSemanticKey()?.Matches(defsTD.SM_TechnicalData.GetSemanticKey()))
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smcOldTD = sm.submodelElements;
            sm.submodelElements = new AdminShell.SubmodelElementWrapperCollection();
            sm.semanticId = new AdminShell.SemanticId(
                    AdminShell.Key.CreateNew(
                    AdminShell.Key.Submodel, false, AdminShell.Identification.IRI,
                    "http://admin-shell.io/sandbox/technical-data-flat/sm"));

            // find all technical properties
            foreach (var smcTDP in smcOldTD.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                defsTD.CD_TechnicalProperties.GetSingleKey()))
            {
                // access
                if (smcTDP == null || smcTDP.value == null)
                    continue;

                // now, take this as root for a recurse find ..
                foreach (var oldSme in smcTDP.value.FindDeep<AdminShell.SubmodelElement>((o) => true))
                {
                    // no collections!
                    if (oldSme is AdminShell.SubmodelElementCollection)
                        continue;

                    // simply add to new
                    sm.submodelElements.Add(oldSme);
                }
            }

            // obviously well
            return true;
        }
    }
}
