/*
Copyright (c) 2021 KEB Automation KG <https://www.keb.de/>,
Copyright (c) 2021 Lenze SE <https://www.lenze.com/en-de/>,
author: Jonas Grote, Denis Göllner, Sebastian Bischof

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginSmdExporter.Model
{
    public abstract class BomSubmodel
    {

        public List<string> PropertyBoms { get; set; }

        public string name { get; set; }
        protected BomSubmodel()
        {
            PropertyBoms = new List<string>();
        }
    }


}
