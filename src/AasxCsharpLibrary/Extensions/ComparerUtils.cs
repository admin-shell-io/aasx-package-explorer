/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System.Collections.Generic;
using System.Globalization;

namespace Extensions
{
    public static class CompareUtils
    {
        public static bool Compare<T>(T x, T y)
        {
            return EqualityComparer<T>.Default.Equals(x, y);
        }
    }
    public class ComparerIdShort : IComparer<IReferable>
    {
        public int Compare(IReferable a, IReferable b)
        {
            return string.Compare(a?.IdShort, b?.IdShort,
                CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
        }
    }

    public class ComparerIdentification : IComparer<IIdentifiable>
    {
        public int Compare(IIdentifiable a, IIdentifiable b)
        {
            return string.Compare(a.Id, b.Id,
                CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
        }
    }



    public class ComparerIndexed : IComparer<IReferable>
    {
        public int NullIndex = int.MaxValue;
        public Dictionary<IReferable, int> Index = new();

        public int Compare(IReferable a, IReferable b)
        {
            var ca = Index.ContainsKey(a);
            var cb = Index.ContainsKey(b);

            if (!ca && !cb)
                return 0;
            // make CDs without usage to appear at end of list
            if (!ca)
                return +1;
            if (!cb)
                return -1;

            var ia = Index[a];
            var ib = Index[b];

            if (ia == ib)
                return 0;
            if (ia < ib)
                return -1;
            return +1;
        }
    }
}
