using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxPluginMtpViewer
{
    public class MtpViewerOptionsRecord
    {
        public enum MtpRecordType { MtpType, MtpInstance }

        public MtpRecordType RecordType = MtpRecordType.MtpType;
        public List<AdminShell.Key> AllowSubmodelSemanticId = new List<AdminShell.Key>();
    }

    public class MtpViewerOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        public List<MtpViewerOptionsRecord> Records = new List<MtpViewerOptionsRecord>();

        public WpfMtpControl.MtpSymbolMapRecordList SymbolMappings = new WpfMtpControl.MtpSymbolMapRecordList();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static MtpViewerOptions CreateDefault()
        {
            var rec1 = new MtpViewerOptionsRecord();
            rec1.RecordType = MtpViewerOptionsRecord.MtpRecordType.MtpType;
            rec1.AllowSubmodelSemanticId.Add(AdminShell.Key.CreateNew(
                type: "Submodel",
                local: false,
                idType: "IRI",
                value: "http://www.admin-shell.io/mtp/v1/submodel"));

            var rec2 = new MtpViewerOptionsRecord();
            rec2.RecordType = MtpViewerOptionsRecord.MtpRecordType.MtpInstance;
            rec2.AllowSubmodelSemanticId.Add(AdminShell.Key.CreateNew(
                type: "Submodel",
                local: false,
                idType: "IRI",
                value: "http://www.admin-shell.io/mtp/v1/mtp-instance-submodel"));

            var opt = new MtpViewerOptions();
            opt.Records.Add(rec1);
            opt.Records.Add(rec2);

            return opt;
        }
    }
}
