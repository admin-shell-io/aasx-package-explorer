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
    internal class Aspect
    {
        private string name;
        private string preferredName;
        private string description;
        private string language;
        private List<Property> myList = new List<Property>();

       

        public string Name { get => name; set => name = value; }
        public string PreferredName { get => preferredName; set => preferredName = value; }
        public string Description { get => description; set => description = value; }
        public string Language { get => language; set => language = value; }
        internal List<Property> MyList { get => myList; set => myList = value; }
    }
}
