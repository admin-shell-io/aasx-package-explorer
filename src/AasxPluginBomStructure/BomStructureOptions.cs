using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginBomStructure
{
    public class BomStructureOptionsRecord
    {
        public List<AdminShell.Key> AllowSubmodelSemanticId = new List<AdminShell.Key>();
    }

    public class BomStructureOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        public List<BomStructureOptionsRecord> Records = new List<BomStructureOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static BomStructureOptions CreateDefault()
        {
            var rec = new BomStructureOptionsRecord();
            rec.AllowSubmodelSemanticId.Add(AdminShell.Key.CreateNew(
                type: "Submodel",
                local: false,
                idType: "IRI",
                value: "http://smart.festo.com/id/type/submodel/BOM/1/1"));

            var opt = new BomStructureOptions();
            opt.Records.Add(rec);

            return opt;
        }
    }
}
