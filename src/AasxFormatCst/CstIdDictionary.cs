using AdminShellNS;
using System;
using System.Collections.Generic;
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
        public CstId cstId;
    }

    public class CstIdStory : List<CstIdDictionaryItem>
    {
    }
}
