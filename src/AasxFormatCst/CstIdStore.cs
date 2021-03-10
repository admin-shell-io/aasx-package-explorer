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

        public void CreateEmptyItemsFromSMEs(AdminShell.SubmodelElementWrapperCollection smwc)
        {
            if (smwc == null)
                return;

            foreach (var smw in smwc)
            {
                var sme = smw?.submodelElement;
                if (sme == null)
                    continue;

                if (sme is AdminShell.SubmodelElementCollection smc)
                {
                    // SMC ? recurse!
                    CreateEmptyItemsFromSMEs(smc.value);
                }
                else
                {
                    // any other
                    var si = sme.semanticId?.GetAsExactlyOneKey()?.value;
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
        }

        public void WriteToFile(string fn)
        {
            File.WriteAllText(fn, JsonConvert.SerializeObject(this, Formatting.Indented));
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
