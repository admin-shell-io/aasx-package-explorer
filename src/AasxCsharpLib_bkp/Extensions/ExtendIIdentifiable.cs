using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendIIdentifiable
    {
        #region List of Identifiers

        public static string ToStringExtended(this List<IIdentifiable> identifiables,string delimiter = ",")
        {
            return string.Join(delimiter, identifiables.Select((x) => x.Id));
        }

        #endregion
        public static Reference GetReference(this IIdentifiable identifiable)
        {
            var key = new Key(ExtensionsUtil.GetKeyType(identifiable), identifiable.Id);
            var outputReference = new Reference(ReferenceTypes.ModelReference, new List<Key>() { key });

            return outputReference;
        }
    }
}
