/*
Copyright (c) 2021 KEB Automation KG <https://www.keb.de/>,
Copyright (c) 2021 Lenze SE <https://www.lenze.com/en-de/>,
author: Jonas Grote, Denis Göllner, Sebastian Bischof

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;

namespace AasxPluginSmdExporter
{
    public static class IdTables
    {
        /// <summary>
        /// To define simulation model ports, a domain is added to every port 
        /// Only ports with matching domain will be connected from the SMD exporter
        /// </summary>
        public static Dictionary<string, string> SemanticPortsDomain = new Dictionary<string, string>()
        {
            { "Domain", "SemanticId" },
            { "elecDC", "Dies ist elecDC" },
            { "SmdComp_SignalFlow", "www.tedz.itsowl.com/ids/cd/7565_0191_6002_1240" },
            { "SmdComp_PhysicalElectric", "www.tedz.itsowl.com/ids/cd/7565_0191_6002_2222" },
            { "SmdComp_PhysicalMechanic", "placeholder" },
            { "SmdComp_SimModelType", "www.tedz.itsowl.com/ids/cd/3191_3132_1102_6456" },
            { "BoM_SmdComp_Fmu", "www.tedz.itsowl.com/ids/cd/5593_7040_8002_2422" },
            { "BoM_SmdComp_Sum", "www.tedz.itsowl.com/ids/cd/8364_7040_8002_5528" },
            { "BoM_SmdComp_Mult", "www.tedz.itsowl.com/ids/cd/9301_8040_8002_5424" },
            { "BoM_SmdComp_Div", "www.tedz.itsowl.com/ids/cd/6321_8040_8002_3964" },
            { "BoM_Comp_Interface", "www.tedz.itsowl.com/ids/cd/7221_0191_0102_0108" },
            { "BoM_SmdComp_PhyNode", "placeholder" }
        };
        /// <summary>
        /// The SMD Exporters draws connections between simulations models based on EClass IDs.
        /// This dictionary includes all supported connections between simulation model ports
        /// </summary>
        public static Dictionary<string, string> CompatibleEclassIds = new Dictionary<string, string>()
        {
            { "eclassIn", "eclassOut" },
            { "0173-1#02-AAU445#002", "0173-1#02-BAB975#006" },
            { "http://tedz.itsowl.com/demo/cd/1/1/1213_2122_7002_4649",
                "http://tedz.itsowl.com/demo/cd/1/1/1213_2122_7002_4649" },
            { "http://tedz.itsowl.com/demo/cd/1/1/1053_2122_7002_4869",
                "http://tedz.itsowl.com/demo/cd/1/1/1053_2122_7002_4869" },
            { "0173-1#02-BAC200#007", "0173-1#02-AAB427#007" },
            { "http://tedz.itsowl.com/demo/cd/1/1/6522_2122_7002_4607",
                "http://tedz.itsowl.com/demo/cd/1/1/6522_2122_7002_4607" },
            { "http://tedz.itsowl.com/demo/cd/1/1/0082_2122_7002_4718",
                "http://tedz.itsowl.com/demo/cd/1/1/0082_2122_7002_4718" },
            { "0173-1#02-AAG870#003", "0173-1#02-AAG870#003" },
            { "http://tedz.itsowl.com/demo/cd/1/1/6130_2122_7002_4355",
                "http://tedz.itsowl.com/demo/cd/1/1/6130_2122_7002_4355" },
            { "0173-1#02-BAA977#005", "0173-1#02-BAA977#005" },
            { "0173-1#02-AAJ701#002", "0173-1#02-AAC967#006" },
            { "0173-1#02-BAI019#003", "0173-1#02-BAI019#003" },
            { "0173-1#02-AAI902#001", "0173-1#02-AAI902#001" },
            { "http://tedz.itsowl.com/demo/cd/1/1/6484_9013_7002_8349",
                "http://tedz.itsowl.com/demo/cd/1/1/6484_9013_7002_8349" },
            { "http://tedz.itsowl.com/demo/cd/1/1/1102_2171_1102_8721",
                "http://tedz.itsowl.com/demo/cd/1/1/1102_2171_1102_8721" }
        };
        /// <summary>
        /// Supported simulation model input ports with affiliated units
        /// </summary>
        public static Dictionary<string, string> InputEclassUnits = new Dictionary<string, string>()
        {
            { "eclassInId", "eclassInUnit" },
            { "0173-1#02-AAU445#002", "A (Ampere)" },
            { "http://tedz.itsowl.com/demo/cd/1/1/1213_2122_7002_4649", "A (Ampere)" },
            { "http://tedz.itsowl.com/demo/cd/1/1/1053_2122_7002_4869", "A (Ampere)" },
            { "0173-1#02-BAC200#007", "V (Volt)" },
            { "http://tedz.itsowl.com/demo/cd/1/1/6522_2122_7002_4607", "V (Volt)" },
            { "http://tedz.itsowl.com/demo/cd/1/1/0082_2122_7002_4718", "V (Volt)" },
            { "0173-1#02-AAG870#003", "1/min (Umdrehungen pro Minute)" },
            { "http://tedz.itsowl.com/demo/cd/1/1/6130_2122_7002_4355", "1/min (Umdrehungen pro Minute)" },
            { "0173-1#02-BAA977#005", "N*m (Newtonmeter)" },
            { "0173-1#02-AAJ701#002", "V*A (Voltampere)" },
            { "0173-1#02-BAI019#003", "m/s (Meter pro Sekunde)" },
            { "0173-1#02-AAI902#001", "N (Newton)" },
            { "http://tedz.itsowl.com/demo/cd/1/1/6484_9013_7002_8349", "-" },
            { "http://tedz.itsowl.com/demo/cd/1/1/1102_2171_1102_8721", "-" }
        };
        /// <summary>
        /// Supported simulation model output ports with affiliated units
        /// </summary>
        public static Dictionary<string, string> OutputEclassUnits = new Dictionary<string, string>()
        {
            { "eclassOutId", "eclassOutUnit" },
            { "0173-1#02-BAB975#006", "A (Ampere)" },
            { "http://tedz.itsowl.com/demo/cd/1/1/1213_2122_7002_4649", "A (Ampere)" },
            { "http://tedz.itsowl.com/demo/cd/1/1/1053_2122_7002_4869", "A (Ampere)" },
            { "0173-1#02-AAB427#007", "V (Volt)" },
            { "http://tedz.itsowl.com/demo/cd/1/1/6522_2122_7002_4607", "V (Volt)" },
            { "http://tedz.itsowl.com/demo/cd/1/1/0082_2122_7002_4718", "V (Volt)" },
            { "0173-1#02-AAG870#003", "1/min (Umdrehungen pro Minute)" },
            { "http://tedz.itsowl.com/demo/cd/1/1/6130_2122_7002_4355", "1/min (Umdrehungen pro Minute)" },
            { "0173-1#02-BAA977#005", "N*m (Newtonmeter)" },
            { "0173-1#02-AAC967#006", "V*A (Voltampere)" },
            { "0173-1#02-BAI019#003", "m/s (Meter pro Sekunde)" },
            { "0173-1#02-AAI902#001", "Schubkraft (In)" },
            { "http://tedz.itsowl.com/demo/cd/1/1/6484_9013_7002_8349", "-" },
            { "http://tedz.itsowl.com/demo/cd/1/1/1102_2171_1102_8721", "-" }
        };

    }
}
