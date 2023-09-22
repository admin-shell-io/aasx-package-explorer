/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using AasxIntegrationBase;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global

namespace AasxUaNetServer
{
    public class UaNetServerOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
    }

    public class UaNetServerOptions : AasxPluginLookupOptionsBase
    {
        public string[] Args;

        public List<UaNetServerOptionsRecord> Records = new List<UaNetServerOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static UaNetServerOptions CreateDefault()
        {
            var opt = new UaNetServerOptions();
            return opt;
        }
    }
}
