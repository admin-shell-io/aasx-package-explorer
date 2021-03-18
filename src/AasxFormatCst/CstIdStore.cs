using AdminShellNS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public AdminShell.SemanticId semanticId;

        /// <summary>
        /// String based CST ID info in one string ("Reference" format)
        /// </summary>
        public string cstRef;

        /// <summary>
        /// Full fledge object with tingle files for CST Id.
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


        public void CreateEmptyItemsFromCDs(IEnumerable<AdminShell.ConceptDescription> cds)
        {
            if (cds == null)
                return;

            foreach (var cd in cds)
            {
                var si = cd?.identification?.id;
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
            AdminShell.SubmodelElementWrapperCollection smwc,
            bool omitIecEclass = false)
        {
            if (smwc == null)
                return;

            foreach (var smw in smwc)
            {
                var sme = smw?.submodelElement;
                if (sme == null)
                    continue;

                // any
                var si = sme.semanticId?.GetAsExactlyOneKey()?.value;
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

                if (sme is AdminShell.SubmodelElementCollection smc)
                {
                    // SMC ? recurse!
                    CreateEmptyItemsFromSMEs(smc.value, omitIecEclass);
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

            if (File.Exists(fn))
            {
                var tmpst = File.ReadAllText(fn);
                var tmp = JsonConvert.DeserializeObject<CstIdStore>(tmpst);
                if (tmp != null)
                    foreach (var ti in tmp)
                        this.Add(ti);
            }
        }
    }
}
