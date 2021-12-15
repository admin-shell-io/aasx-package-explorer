/*
 * Copyright (c) 2021 Robert Bosch Manufacturing Solutions GmbH
 *
 * Author: Monisha Macharla Vasu
 * 
 *
 * This Source Code Form is subject to the terms of the Apache License 2.0. 
 * If a copy of the Apache License 2.0 was not distributed with this
 * file, you can obtain one at https://spdx.org/licenses/Apache-2.0.html.
 * 
 *
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace AasxBammRdfImExport.RDFentities
{
    class Characteristics
    {
        private string chracterisname; // field
        private string dataType;
        //  Properties 
        public string DataType { get => dataType; set => dataType = value; }
        public string Chracterisname { get => chracterisname; set => chracterisname = value; }
    }
}
