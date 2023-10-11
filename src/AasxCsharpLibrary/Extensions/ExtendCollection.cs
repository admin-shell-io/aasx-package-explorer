using System.Collections.Generic;

namespace AdminShellNS.Extensions
{
    public static class ExtendCollection
    {
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            if (list != null && list.Count != 0)
            {
                return false;
            }

            return true;
        }
    }
}
