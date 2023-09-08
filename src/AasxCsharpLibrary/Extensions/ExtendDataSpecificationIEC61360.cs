/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System.Collections.Generic;

namespace Extensions
{
    public static class ExtendDataSpecificationIEC61360
    {
        public static DataSpecificationIec61360 ConvertFromV20(this DataSpecificationIec61360 ds61360, AasxCompatibilityModels.AdminShellV20.DataSpecificationIEC61360 src616360)
        {
            if (src616360.preferredName != null)
                ds61360.PreferredName = new List<ILangStringPreferredNameTypeIec61360>().ConvertFromV20(src616360.preferredName);

            if (src616360.shortName != null)
                ds61360.ShortName = new List<ILangStringShortNameTypeIec61360>().ConvertFromV20(src616360.shortName);

            if (!string.IsNullOrEmpty(src616360.unit))
            {
                ds61360.Unit = src616360.unit;
            }

            if (src616360.unitId != null)
                ds61360.UnitId = ExtensionsUtil.ConvertReferenceFromV20(AasxCompatibilityModels.AdminShellV20.Reference.CreateNew(src616360.unitId.keys), ReferenceTypes.ExternalReference);

            ds61360.ValueFormat = src616360.valueFormat;
            ds61360.SourceOfDefinition = src616360.sourceOfDefinition;
            ds61360.Symbol = src616360.symbol;
            if (!(string.IsNullOrEmpty(src616360.dataType)))
            {
                var dt = src616360.dataType;
                if (!dt.StartsWith("xs:"))
                    dt = "xs:" + dt;
                ds61360.DataType = Stringification.DataTypeIec61360FromString(dt);
            }
            if (src616360.definition != null)
                ds61360.Definition = new List<ILangStringDefinitionTypeIec61360>().ConvertFromV20(src616360.definition);

            //TODO (jtikekar, 0000-00-00): check with Andreas
            ds61360.Value = "";

            return ds61360;
        }
    }
}
