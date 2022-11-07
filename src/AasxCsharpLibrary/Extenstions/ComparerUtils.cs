using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
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
