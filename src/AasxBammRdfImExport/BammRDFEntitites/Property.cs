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
    class Property
    {
        private string name;
        private string preferredName;
        private string description;
        private string Language;
        private Characteristics characteristic = new Characteristics();
        private string exampleValue;
        public string Name { get => name; set => name = value; }
        public string PreferredName { get => preferredName; set => preferredName = value; }

        public string Description { get => description; set => description = value; }
        public string ExampleValue { get => exampleValue; set => exampleValue = value; }
        public string Language1 { get => Language; set => Language = value; }
        internal Characteristics Characteristic { get => characteristic; set => characteristic = value; }
    }
}
