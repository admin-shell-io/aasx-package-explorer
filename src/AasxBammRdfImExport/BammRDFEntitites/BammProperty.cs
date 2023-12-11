/*
Copyright (c) 2021 Robert Bosch Manufacturing Solutions GmbH

Author: Monisha Macharla Vasu

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// ReSharper disable ClassNeverInstantiated.Global

using System;
using System.Collections.Generic;
using System.Text;

namespace AasxBammRdfImExport.RDFentities
{
    public class BammProperty
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
