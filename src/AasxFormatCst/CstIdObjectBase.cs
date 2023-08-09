/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AasxFormatCst
{
    public class CstIdObjectBase : IUniqueness<CstIdObjectBase>
    {

        [JsonProperty(Order = -2)]
        public string ObjectType;

        [JsonProperty(Order = -2)]
        public string Namespace;

        [JsonProperty(Order = -2)]
        public string ID;

        [JsonProperty(Order = -2)]
        public string Revision;

        [JsonProperty(Order = -2)]
        public string Name;

        [JsonProperty(Order = -2)]
        public string MinorRevision;

        [JsonProperty(Order = -2)]
        public string Status;

        public string ToRef()
        {
            var res = String.Format("{0}#{1}-{2}#{3}", Namespace, ObjectType, ID, Revision);
            return res;
        }

        public static CstIdObjectBase Parse(string input)
        {
            if (input == null)
                return null;
            input = input.Trim();

            // ECLASS 
            var m = Regex.Match(input, @"^0173(|-)(\w*)#(\w+)-(\w+)#(\w+)");
            if (m.Success)
            {
                var res = new CstIdObjectBase()
                {
                    Namespace = "0173" + m.Groups[1].ToString() + m.Groups[2].ToString(),
                    ObjectType = m.Groups[3].ToString(),
                    ID = m.Groups[4].ToString(),
                    Revision = m.Groups[5].ToString()
                };
                return res;
            }

            // IEC CDD 
            m = Regex.Match(input, @"^0112/(\w*)/(\w*)/(\w*)/(\w*)#(\w+)#(\w+)");
            if (m.Success)
            {
                var res = new CstIdObjectBase()
                {
                    Namespace = "0112" + "_" + m.Groups[4].ToString(),
                    ObjectType = m.Groups[2].ToString(),
                    ID = m.Groups[5].ToString(),
                    Revision = m.Groups[6].ToString()
                };
                return res;
            }

            // CST style
            m = Regex.Match(input, @"^\s*(\w+)#(\w+)-(\w+)#(\w+)");
            if (m.Success)
            {
                var res = new CstIdObjectBase()
                {
                    Namespace = m.Groups[1].ToString(),
                    ObjectType = m.Groups[2].ToString(),
                    ID = m.Groups[3].ToString(),
                    Revision = m.Groups[4].ToString()
                };
                return res;
            }

            // no
            return null;
        }

        public bool EqualsForUniqueness(CstIdObjectBase other)
        {
            if (other == null)
                return false;

            var res = other.ObjectType.Equals(ObjectType, StringComparison.InvariantCultureIgnoreCase)
                && other.Namespace.Equals(Namespace, StringComparison.InvariantCultureIgnoreCase)
                && other.ID.Equals(ID, StringComparison.InvariantCultureIgnoreCase)
                && other.Revision.Equals(Revision, StringComparison.InvariantCultureIgnoreCase)
#if __SWITCH_OFF
                && other.Name.Equals(Name, StringComparison.InvariantCultureIgnoreCase)
                && other.MinorRevision.Equals(MinorRevision, StringComparison.InvariantCultureIgnoreCase)
#endif
                && other.Status.Equals(Status, StringComparison.InvariantCultureIgnoreCase);

            return res;
        }
    }
}
