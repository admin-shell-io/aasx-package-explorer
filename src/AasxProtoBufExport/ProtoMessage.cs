/*
Copyright (c) 2020 TU Dresden Institute of Applied Computer Science <https://tu-dresden.de/inf/pk>
Author: Nico Braunisch <nico.braunisch@tu-dresden.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System;
using System.Collections.Generic;
using System.Windows;

namespace AasxProtoBufExport
{
    public class ProtoMessage
    {
        private string name;
        private List<ProtField> fields;

        public ProtoMessage(string name, List<ProtField> fields)
        {
            this.name = name;
            this.fields = fields;
        }

        public override string ToString()
        {
            String str = "";
            str += "message " + name + " {\n";
            ProtField pf = new ProtField("", "", "");
            var s = pf.ToString();
            if (!s.Equals(""))
            {
                return "";
            }
            return str;
        }
    }

    public class ProtField
    {
        private string name;
        private string type;
        private string fieldRules;

        public ProtField(string name, string type, string fieldRules)
        {
            this.name = name;
            this.type = type;
            this.fieldRules = fieldRules;
        }

        public override string ToString()
        {
            return name + type + fieldRules;
        }
    }
}