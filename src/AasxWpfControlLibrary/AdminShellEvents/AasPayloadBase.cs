/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AasxPackageExplorer;
using System.Windows.Documents;

namespace AdminShellEvents
{
    /// <summary>
    /// Base class for any AAS event payload. 
    /// Payloads are wrapped in AAS event envelopes or transactions.
    /// </summary>
    public class AasPayloadBase
    {

        public virtual string ToMarkup()
        {
            return "";
        }

    }
}
