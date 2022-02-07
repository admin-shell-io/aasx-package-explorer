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
    public class ConvertOfferBase
    {
        public ConvertProviderBase Provider = null;
        public string OfferDisplay = "";

        public ConvertOfferBase() { }
        public ConvertOfferBase(
            ConvertProviderBase provider, string offerDisp)
        {
            this.Provider = provider;
            this.OfferDisplay = offerDisp;
        }
    }

    public class ConvertProviderBase
    {
        public virtual List<ConvertOfferBase> CheckForOffers(AdminShell.Referable currentReferable)
        {
            return null;
        }

        public virtual bool ExecuteOffer(
            AdminShellPackageEnv package, AdminShell.Referable currentReferable, ConvertOfferBase offer,
            bool deleteOldCDs, bool addNewCDs)
        {
            return true;
        }
    }

    public static class ConvertPredefinedConcepts
    {
        public static IEnumerable<ConvertProviderBase> GetAllProviders()
        {
            yield return new ConvertDocumentationSg2ToHsuProvider();
            yield return new ConvertDocumentationSg2ToV11Provider();
            yield return new ConvertDocumentationHsuToSg2Provider();
            yield return new ConvertTechnicalDataToFlatProvider();
            yield return new ConvertTechnicalDataV10ToV11Provider();
            yield return new ConvertNameplateHsuToZveiV10Provider();
        }

        public static List<ConvertOfferBase> CheckForOffers(AdminShell.Referable currentReferable)
        {
            var res = new List<ConvertOfferBase>();
            var providers = GetAllProviders();
            foreach (var prov in providers)
            {
                var offers = prov.CheckForOffers(currentReferable);
                if (offers != null)
                    foreach (var o in offers)
                        res.Add(o);
            }
            return res;
        }
    }
}
