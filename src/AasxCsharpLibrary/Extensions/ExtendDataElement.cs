using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendDataElement
    {
        public static DataTypeDefXsd[] ValueTypes_Number =
            new[] { DataTypeDefXsd.Decimal, DataTypeDefXsd.Double, DataTypeDefXsd.Float,
                DataTypeDefXsd.Integer, DataTypeDefXsd.Long, DataTypeDefXsd.Int, DataTypeDefXsd.Short,
                DataTypeDefXsd.Byte, DataTypeDefXsd.NonNegativeInteger, DataTypeDefXsd.NonPositiveInteger,
                DataTypeDefXsd.UnsignedInt, DataTypeDefXsd.Integer, DataTypeDefXsd.UnsignedByte, 
                DataTypeDefXsd.UnsignedLong, DataTypeDefXsd.UnsignedShort, DataTypeDefXsd.NegativeInteger }; 
    }
}
