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
            return String.Format("[ver={0}, rev={1}, tmpl={2}, crea={3}]", 
                ls.Version, ls.Revision, ls.TemplateId, ls.Creator?.ToStringExtended(fmt));
        }
    }
}
