/*
Copyright (c) 2021 KEB Automation KG <https://www.keb.de/>,
Copyright (c) 2021 Lenze SE <https://www.lenze.com/en-de/>,
author: Jonas Grote, Denis Göllner, Sebastian Bischof

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AasxPluginSmdExporter
{
    public class Mapping
    {
        public string IdShort { get; set; }

        public string UnknownId { get; set; }

        public string BasicId { get; set; }

        public double Value { get; set; }

        public Mapping(string idShort, string unknownId, string basicId, double value)
        {
            this.IdShort = idShort;
            this.UnknownId = unknownId;
            this.BasicId = basicId;
            this.Value = value;
        }

    }
}