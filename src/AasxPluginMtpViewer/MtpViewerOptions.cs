/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPredefinedConcepts;
using AdminShellNS;
using WpfMtpControl;

namespace AasxPluginMtpViewer
{
    public class MtpViewerOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
        public enum MtpRecordType { MtpType, MtpInstance }

        public MtpRecordType RecordType = MtpRecordType.MtpType;

        public MtpViewerOptionsRecord() : base() { }

#if !DoNotUseAasxCompatibilityModels
        public MtpViewerOptionsRecord(AasxCompatibilityModels.AasxPluginMtpViewer.MtpViewerOptionsRecordV20 src)
            : base()
        {
            if (src == null)
                return;
            
            RecordType = (MtpRecordType)((int)src.RecordType);

            if (src.AllowSubmodelSemanticId != null)
                foreach (var k in src.AllowSubmodelSemanticId)
                    AllowSubmodelSemanticId.Add(new AdminShell.Identifier(k?.value));
        }
#endif
    }

    public class MtpViewerOptions : AasxPluginLookupOptionsBase
    {
        public List<MtpViewerOptionsRecord> Records = new List<MtpViewerOptionsRecord>();

        public WpfMtpControl.MtpSymbolMapRecordList SymbolMappings = new WpfMtpControl.MtpSymbolMapRecordList();

        public MtpVisuOptions VisuOptions = new MtpVisuOptions();

        public MtpViewerOptions() : base() { }

#if !DoNotUseAasxCompatibilityModels
        public MtpViewerOptions(AasxCompatibilityModels.AasxPluginMtpViewer.MtpViewerOptionsV20 src)
            : base()
        {
            if (src == null)
                return;

            if (src.Records != null)
                foreach (var rec in src.Records)
                    Records.Add(new MtpViewerOptionsRecord(rec));

            var xx = new AasxCompatibilityModels.WpfMtpControl.MtpSymbolMapRecordV20();
            var yy = new MtpSymbolMapRecord(xx);

            if (src.SymbolMappings != null)
                foreach (var sym in src.SymbolMappings)
                    SymbolMappings.Add(new MtpSymbolMapRecord(sym));

            if (src.VisuOptions != null)
                VisuOptions = new WpfMtpControl.MtpVisuOptions(src.VisuOptions);
        }
#endif

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static MtpViewerOptions CreateDefault()
        {
            var defs = new DefinitionsMTP.ModuleTypePackage();

            var rec1 = new MtpViewerOptionsRecord();
            rec1.RecordType = MtpViewerOptionsRecord.MtpRecordType.MtpType;
            rec1.AllowSubmodelSemanticId = new List<AdminShell.Identifier>(
                new[] { defs.SEM_MtpSubmodel.GetAsIdentifier(strict: true) });

            var rec2 = new MtpViewerOptionsRecord();
            rec2.RecordType = MtpViewerOptionsRecord.MtpRecordType.MtpInstance;
            rec2.AllowSubmodelSemanticId = new List<AdminShell.Identifier>(
                new[] { defs.SEM_MtpInstanceSubmodel.GetAsIdentifier(strict: true) });

            var opt = new MtpViewerOptions();
            opt.Records.Add(rec1);
            opt.Records.Add(rec2);

            return opt;
        }
    }
}
