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
    internal class Aspect
    {
        private string name;
        private string preferredName;
        private string description;
        private string language;
        private List<BammProperty> myList = new List<BammProperty>();



        public string Name { get => name; set => name = value; }
        public string PreferredName { get => preferredName; set => preferredName = value; }
        public string Description { get => description; set => description = value; }
        public string Language { get => language; set => language = value; }
        internal List<BammProperty> MyList { get => myList; set => myList = value; }
    }
}
