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
using AasxPredefinedConcepts;
using AdminShellNS;
using WpfMtpControl;

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

        public MtpVisuOptions VisuOptions = new MtpVisuOptions();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static MtpViewerOptions CreateDefault()
        {
            var defs = new DefinitionsMTP.ModuleTypePackage();

            var rec1 = new MtpViewerOptionsRecord();
            rec1.RecordType = MtpViewerOptionsRecord.MtpRecordType.MtpType;
            rec1.AllowSubmodelSemanticId = new List<AdminShell.Key>(defs.SEM_MtpSubmodel.Keys);

            var rec2 = new MtpViewerOptionsRecord();
            rec2.RecordType = MtpViewerOptionsRecord.MtpRecordType.MtpInstance;
            rec2.AllowSubmodelSemanticId = new List<AdminShell.Key>(defs.SEM_MtpInstanceSubmodel.Keys);

            var opt = new MtpViewerOptions();
            opt.Records.Add(rec1);
            opt.Records.Add(rec2);

            return opt;
        }
    }
}
