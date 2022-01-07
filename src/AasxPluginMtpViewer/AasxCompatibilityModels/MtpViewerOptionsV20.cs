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
using Newtonsoft.Json;

// ReSharper disable All .. as this is legacy code!

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global

#if !DoNotUseAasxCompatibilityModels

namespace AasxCompatibilityModels.AasxPluginMtpViewer
{
    public class MtpViewerOptionsRecordV20
    {
        public enum MtpRecordType { MtpType, MtpInstance }

        public MtpRecordType RecordType = MtpRecordType.MtpType;
        public List<AdminShellV20.Key> AllowSubmodelSemanticId = new List<AdminShellV20.Key>();
    }

    public class MtpViewerOptionsV20
    {
        public List<MtpViewerOptionsRecordV20> Records = new List<MtpViewerOptionsRecordV20>();

        public WpfMtpControl.MtpSymbolMapRecordListV20 SymbolMappings
            = new WpfMtpControl.MtpSymbolMapRecordListV20();

        public WpfMtpControl.MtpVisuOptionsV20 VisuOptions
            = new WpfMtpControl.MtpVisuOptionsV20();
    }
}

#endif