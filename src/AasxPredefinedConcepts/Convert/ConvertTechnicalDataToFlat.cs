/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS;
using Extensions;
using System.Collections.Generic;
using Aas = AasCore.Aas3_0;

// ReSharper disable MergeIntoPattern

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

        public override List<ConvertOfferBase> CheckForOffers(Aas.IReferable currentReferable)
        {
            // collectResults
            var res = new List<ConvertOfferBase>();

            // use pre-definitions
            var defs = new AasxPredefinedConcepts.DefinitionsZveiTechnicalData.SetOfDefs(
                            new AasxPredefinedConcepts.DefinitionsZveiTechnicalData());

            var sm = currentReferable as Aas.Submodel;
            if (sm != null && true == sm.SemanticId.GetAsExactlyOneKey()?.Matches(defs.SM_TechnicalData.SemanticId.GetAsExactlyOneKey()))
                res.Add(new ConvertOfferTechnicalDataToFlat(this,
                        $"Convert Submodel '{"" + sm.IdShort}' from Technical Data to flat Submodel"));

            return res;
        }

        public override bool ExecuteOffer(
            AdminShellPackageEnv package, Aas.IReferable currentReferable,
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
            var sm = currentReferable as Aas.Submodel;
            if (sm == null || sm.SubmodelElements == null
                || true != sm.SemanticId.GetAsExactlyOneKey()?.Matches(defsTD.SM_TechnicalData.SemanticId.GetAsExactlyOneKey()))
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smcOldTD = sm.SubmodelElements;
            sm.SubmodelElements = new List<Aas.ISubmodelElement>();
            sm.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>() { new Aas.Key(Aas.KeyTypes.Submodel, "http://admin-shell.io/sandbox/technical-data-flat/sm") });

            // find all technical properties
            foreach (var smcTDP in smcOldTD.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                defsTD.CD_TechnicalProperties.GetSingleKey()))
            {
                // access
                if (smcTDP == null || smcTDP.Value == null)
                    continue;

                // now, take this as root for a recurse find ..
                foreach (var oldSme in smcTDP.Value.FindDeep<Aas.ISubmodelElement>((o) => true))
                {
                    // no collections!
                    if (oldSme is Aas.SubmodelElementCollection)
                        continue;

                    // simply add to new
                    sm.SubmodelElements.Add(oldSme);
                }
            }

            // obviously well
            return true;
        }
    }
}
