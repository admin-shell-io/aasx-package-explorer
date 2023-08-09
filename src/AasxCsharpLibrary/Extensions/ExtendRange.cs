/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System;
using AAS = AasCore.Aas3_0;

namespace Extensions
{
    public static class ExtendRange
    {
        public static string ValueAsText(this AasCore.Aas3_0.Range range)
        {
            return "" + range.Min + " .. " + range.Max;
        }

        public static AasCore.Aas3_0.Range ConvertFromV20(this AasCore.Aas3_0.Range range, AasxCompatibilityModels.AdminShellV20.Range sourceRange)
        {
            if (sourceRange == null)
            {
                return null;
            }

            var propertyType = AAS.Stringification.DataTypeDefXsdFromString("xs:" + sourceRange.valueType);
            if (propertyType != null)
            {
                range.ValueType = (AAS.DataTypeDefXsd)propertyType;
            }
            else
            {
                Console.WriteLine($"ValueType {sourceRange.valueType} not found for property {range.IdShort}");
            }

            range.Max = sourceRange.max;
            range.Min = sourceRange.min;

            return range;
        }

        public static AAS.Range UpdateFrom(this AAS.Range elem, AAS.ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((AAS.ISubmodelElement)elem).UpdateFrom(source);

            if (source is AAS.Property srcProp)
            {
                elem.ValueType = srcProp.ValueType;
                elem.Min = srcProp.Value;
                elem.Max = elem.Min;
            }

            if (source is AAS.MultiLanguageProperty srcMlp)
            {
                elem.ValueType = AAS.DataTypeDefXsd.String;
                elem.Min = "" + srcMlp.Value?.GetDefaultString();
                elem.Max = elem.Min;
            }

            if (source is AAS.File srcFile)
            {
                elem.ValueType = AAS.DataTypeDefXsd.String;
                elem.Min = "" + srcFile.Value;
                elem.Max = elem.Min;
            }

            return elem;
        }
    }
}
