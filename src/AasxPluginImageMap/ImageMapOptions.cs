using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxPluginImageMap
{
    public class ImageMapOptionsOptionsRecord
    {
        public List<AdminShell.Key> AllowSubmodelSemanticId = new List<AdminShell.Key>();
    }

    public class ImageMapOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        public List<ImageMapOptionsOptionsRecord> Records = new List<ImageMapOptionsOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static ImageMapOptions CreateDefault()
        {
            var rec = new ImageMapOptionsOptionsRecord();
            rec.AllowSubmodelSemanticId.Add(
                AasxPredefinedConcepts.ImageMap.Static.SEM_ImageMapSubmodel.GetAsExactlyOneKey());

            var opt = new ImageMapOptions();
            opt.Records.Add(rec);

            return opt;
        }
    }
}
