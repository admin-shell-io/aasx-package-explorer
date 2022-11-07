using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxServerStandardBib.Extenstions
{
    public static class ExtendSubmodelElementList
    {
        public static T FindFirstIdShortAs<T>(this SubmodelElementList submodelElementList, string idShort) where T : ISubmodelElement
        {

            var submodelElements = submodelElementList.Value.Where(sme => (sme != null) && (sme is T) && sme.IdShort.Equals(idShort, StringComparison.OrdinalIgnoreCase));

            if (submodelElements.Any())
            {
                return (T)submodelElements.First();
            }

            return default;
        }
    }
}
