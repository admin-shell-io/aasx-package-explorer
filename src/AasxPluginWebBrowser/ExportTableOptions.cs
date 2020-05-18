using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginWebBrowser
{
    public class WebBrowserOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        // right now, nothing!

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static WebBrowserOptions CreateDefault()
        {
            var opt = new WebBrowserOptions();
            return opt;
        }
    }
}
