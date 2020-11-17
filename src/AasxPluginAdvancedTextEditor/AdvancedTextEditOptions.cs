using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxPluginAdvancedTextEditor
{
    public class AdvancedTextEditOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        // right now, nothing!

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static AdvancedTextEditOptions CreateDefault()
        {
            var opt = new AdvancedTextEditOptions();
            return opt;
        }
    }
}
