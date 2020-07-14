using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxPluginTechnicalData
{
    public class TechnicalDataOptionsRecord
    {
        public List<AdminShell.Key> AllowSubmodelSemanticId = new List<AdminShell.Key>();
    }

    public class TechnicalDataOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        public List<TechnicalDataOptionsRecord> Records = new List<TechnicalDataOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static TechnicalDataOptions CreateDefault()
        {
            var rec = new TechnicalDataOptionsRecord();
            rec.AllowSubmodelSemanticId.Add(AdminShell.Key.CreateNew(
                type: "Submodel",
                local: false,
                idType: "IRI",
                value: "http://smart.festo.com/id/type/submodel/TechnicalDatat/1/1"));

            var opt = new TechnicalDataOptions();
            opt.Records.Add(rec);

            return opt;
        }
    }
}
