using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendRange
    {
        public static string ValueAsText(this AasCore.Aas3_0_RC02.Range range)
        {
            return "" + range.Min + " .. " + range.Max;
        }

        public static AasCore.Aas3_0_RC02.Range ConvertFromV20(this AasCore.Aas3_0_RC02.Range range, AasxCompatibilityModels.AdminShellV20.Range sourceRange)
        {
            if (sourceRange == null)
            {
                return null;
            }

            var propertyType = Stringification.DataTypeDefXsdFromString("xs:" + sourceRange.valueType);
            if (propertyType != null)
            {
                range.ValueType = (DataTypeDefXsd)propertyType;
            }
            else
            {
                Console.WriteLine($"ValueType {sourceRange.valueType} not found for property {range.IdShort}");
            }

            range.Max = sourceRange.max;
            range.Min = sourceRange.min;

            return range;
        }
    }
}
