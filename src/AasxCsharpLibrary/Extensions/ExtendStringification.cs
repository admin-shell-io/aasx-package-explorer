using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendStringification
    {
        public static IEnumerable<string> DataTypeXsdToStringArray() =>
            Enum.GetValues(typeof(DataTypeDefXsd)).OfType<DataTypeDefXsd>().Select((dt) => Stringification.ToString(dt));
    }
}
