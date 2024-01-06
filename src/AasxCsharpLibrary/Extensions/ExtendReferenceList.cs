/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AdminShellNS.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendReferenceList
    {
        /// <summary>
        /// Useful, if the searched reference will have only on key (e.g. ECLASS properties)
        /// </summary>
        public static bool MatchesAnyWithExactlyOneKey(this List<IReference> reflist, IKey key, MatchMode matchMode = MatchMode.Strict)
        {
            if (key == null || reflist == null || reflist.Count < 1)
            {
                return false;
            }

            var found = false;
            foreach (var r in reflist)
                found = found || r.MatchesExactlyOneKey(key, matchMode);

            return found;
        }
    }

}
