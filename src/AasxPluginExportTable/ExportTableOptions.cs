using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxPluginExportTable
{
    public class ExportTableOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        public List<ExportTableRecord> Presets = new List<ExportTableRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static ExportTableOptions CreateDefault()
        {
            var opt = new ExportTableOptions();
            return opt;
        }
    }
}
