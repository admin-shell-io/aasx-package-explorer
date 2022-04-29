/*
Copyright (c) 2021 Robert Bosch Manufacturing Solutions GmbH

Author: Monisha Macharla Vasu

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
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
