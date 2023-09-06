/*
Copyright (c) 2021 KEB Automation KG <https://www.keb.de/>,
Copyright (c) 2021 Lenze SE <https://www.lenze.com/en-de/>,
author: Jonas Grote, Denis Göllner, Sebastian Bischof

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxCompatibilityModels;
using AdminShellNS;
using Extensions;
using JetBrains.Annotations;

namespace AasxPluginSmdExporter
{
    public class SmdExporterOptionsRecord
    {
        public List<AdminShellV20.Key> AllowSubmodelSemanticId = new List<AdminShell.Key>();
    }

    [UsedImplicitlyAttribute]
    public class SmdExporterOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        public List<SmdExporterOptionsRecord> Records = new List<SmdExporterOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static SmdExporterOptions CreateDefault()
        {
            var rec = new SmdExporterOptionsRecord();
            var key = AasxPredefinedConcepts.SmdExporter.Static.SEM_SmdExporterSubmodel.GetAsExactlyOneKey();
            rec.AllowSubmodelSemanticId.Add(new AdminShellV20.Key(key.Type.ToString(), true, "", key.Value));

            var opt = new SmdExporterOptions();
            opt.Records.Add(rec);

            return opt;
        }
    }
}
