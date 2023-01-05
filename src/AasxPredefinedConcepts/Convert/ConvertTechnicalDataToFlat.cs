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
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

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

        public override List<ConvertOfferBase> CheckForOffers(AasCore.Aas3_0_RC02.IReferable currentReferable)
        {
            // collectResults
            var res = new List<ConvertOfferBase>();

            // use pre-definitions
            var defs = new AasxPredefinedConcepts.DefinitionsZveiTechnicalData.SetOfDefs(
                            new AasxPredefinedConcepts.DefinitionsZveiTechnicalData());

            var sm = currentReferable as AasCore.Aas3_0_RC02.Submodel;
            if (sm != null && true == sm.SemanticId.GetAsExactlyOneKey()?.Matches(defs.SM_TechnicalData.SemanticId.GetAsExactlyOneKey()))
                res.Add(new ConvertOfferTechnicalDataToFlat(this,
                        $"Convert Submodel '{"" + sm.IdShort}' from Technical Data to flat Submodel"));

            return res;
        }

        public override bool ExecuteOffer(
            AdminShellPackageEnv package, AasCore.Aas3_0_RC02.IReferable currentReferable,
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
            var sm = currentReferable as AasCore.Aas3_0_RC02.Submodel;
            if (sm == null || sm.SubmodelElements == null
                || true != sm.SemanticId.GetAsExactlyOneKey()?.Matches(defsTD.SM_TechnicalData.SemanticId.GetAsExactlyOneKey()))
                return false;

            // convert in place: detach old SMEs, change semanticId
            var smcOldTD = sm.SubmodelElements;
            sm.SubmodelElements = new List<AasCore.Aas3_0_RC02.ISubmodelElement>();
            sm.SemanticId = new AasCore.Aas3_0_RC02.Reference(AasCore.Aas3_0_RC02.ReferenceTypes.ModelReference, new List<AasCore.Aas3_0_RC02.Key>() { new AasCore.Aas3_0_RC02.Key(AasCore.Aas3_0_RC02.KeyTypes.Submodel, "http://admin-shell.io/sandbox/technical-data-flat/sm") });

            // find all technical properties
            foreach (var smcTDP in smcOldTD.FindAllSemanticIdAs<AasCore.Aas3_0_RC02.SubmodelElementCollection>(
                defsTD.CD_TechnicalProperties.GetSingleKey()))
            {
                // access
                if (smcTDP == null || smcTDP.Value == null)
                    continue;

                // now, take this as root for a recurse find ..
                foreach (var oldSme in smcTDP.Value.FindDeep<AasCore.Aas3_0_RC02.ISubmodelElement>((o) => true))
                {
                    // no collections!
                    if (oldSme is AasCore.Aas3_0_RC02.SubmodelElementCollection)
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
