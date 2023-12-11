/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendIAdministrativeInformation
    {
        public static string ToStringExtended(this IAdministrativeInformation ls, int fmt)
        {
            if (fmt == 2)
                return String.Format("/{0}/{1}", ls.Version, ls.Revision);
            if (fmt == 1)
                return String.Format("V{0}.{1}", ls.Version, ls.Revision);
            return String.Format("[ver={0}, rev={1}, tmpl={2}, crea={3}]",
                ls.Version, ls.Revision, ls.TemplateId, ls.Creator?.ToStringExtended(fmt));
        }
    }
}
