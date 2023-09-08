/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendIIdentifiable
    {
        #region List of Identifiers

        public static string ToStringExtended(this List<IIdentifiable> identifiables, string delimiter = ",")
        {
            return string.Join(delimiter, identifiables.Select((x) => x.Id));
        }

        #endregion
        public static IReference GetReference(this IIdentifiable identifiable)
        {
            var key = new Key(ExtensionsUtil.GetKeyType(identifiable), identifiable.Id);
            //TODO (jtikekar, 0000-00-00): if model or Global reference?
            var outputReference = new Reference(ReferenceTypes.ModelReference, new List<IKey>() { key });

            return outputReference;
        }
    }
}
