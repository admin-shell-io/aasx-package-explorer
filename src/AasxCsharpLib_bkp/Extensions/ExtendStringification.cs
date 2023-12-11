using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendStringification
    {
        public static IEnumerable<string> DataTypeXsdToStringArray() =>
            Enum.GetValues(typeof(DataTypeDefXsd)).OfType<DataTypeDefXsd>().Select((dt) => Stringification.ToString(dt));
    }
}
