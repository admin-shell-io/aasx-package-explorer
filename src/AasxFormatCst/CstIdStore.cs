/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

// ReSharper disable UnassignedField.Global

namespace AasxFormatCst
{
    public class CstIdDictionaryItem
    {
        /// <summary>
        /// For direct matching a single string based semanticId, value only
        /// </summary>
        public string semId;

        /// <summary>
        /// Full fledged Reference, presumably not used at all
        /// </summary>
        public Reference semanticId;

        /// <summary>
        /// String based CST ID info in one string ("Reference" format)
        /// </summary>
        public string cstRef;

        /// <summary>
        /// Preferred name for the reference.
        /// </summary>
        public string preferredName;

        /// <summary>
        /// Full fledged object with single attributes for CST Id.
        /// </summary>
        public CstIdObjectBase cstId;
    }

    public class CstIdStore : List<CstIdDictionaryItem>
    {
        public CstIdDictionaryItem FindStringSemId(string semId)
        {
            if (semId == null)
                return null;

            foreach (var it in this)
                if (it?.semId != null)
                {
                    if (it.semId.Trim().ToLower() == semId.Trim().ToLower())
                        return it;
                }

            return null;
        }


        public void CreateEmptyItemsFromCDs(IEnumerable<ConceptDescription> cds)
        {
            if (cds == null)
                return;

            foreach (var cd in cds)
            {
                var si = cd?.Id;
                if (si != null)
                {
                    var item = new CstIdDictionaryItem()
                    {
                        semId = si,
                        cstRef = ""
                    };
                    this.Add(item);
                }
            }
        }

        public void CreateEmptyItemsFromSMEs(
            List<ISubmodelElement> smwc,
            bool omitIecEclass = false)
        {
            if (smwc == null)
                return;

            foreach (var smw in smwc)
            {
                var sme = smw;
                if (sme == null)
                    continue;

                // any
                var si = sme.SemanticId?.GetAsExactlyOneKey()?.Value;
                if (si != null)
                {
                    if (omitIecEclass && (si.StartsWith("0173") || si.StartsWith("0112")))
                        continue;

                    var item = new CstIdDictionaryItem()
                    {
                        semId = si,
                        cstRef = ""
                    };
                    this.Add(item);
                }

                if (sme is SubmodelElementCollection smc)
                {
                    // SMC ? recurse!
                    CreateEmptyItemsFromSMEs(smc.Value, omitIecEclass);
                }
            }
        }

        public void WriteToFile(string fn)
        {
            var srl = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
            using (var sw = new StreamWriter(fn))
            {
                srl.Serialize(sw, this);
            }
        }

        public void AddFromFile(string fn)
        {
            if (fn == null)
                return;

            if (System.IO.File.Exists(fn))
            {
                var tmpst = System.IO.File.ReadAllText(fn);
                var tmp = JsonConvert.DeserializeObject<CstIdStore>(tmpst);
                if (tmp != null)
                    foreach (var ti in tmp)
                        this.Add(ti);
            }
        }
    }
}
