using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendDataSpecificationIEC61360
    {
        public static DataSpecificationIec61360 ConvertFromV20(this DataSpecificationIec61360 dataSpecificationIEC61360, AasxCompatibilityModels.AdminShellV20.DataSpecificationIEC61360 sourceDataSpecIEC61360)
        {
            if (sourceDataSpecIEC61360.preferredName != null)
            {
                //TODO:jtikekar performance impact
                List<LangString> preferredNames = new List<LangString>();
                foreach(var srcPrefName in sourceDataSpecIEC61360.preferredName)
                {
                    preferredNames.Add(new LangString(srcPrefName.lang, srcPrefName.str));
                }
                dataSpecificationIEC61360.PreferredName = preferredNames;
            }
            
            if (sourceDataSpecIEC61360.preferredName != null)
            {
                //TODO:jtikekar performance impact
                List<LangString> shortNames = new List<LangString>();
                foreach(var srcShortName in sourceDataSpecIEC61360.shortName)
                {
                    shortNames.Add(new LangString(srcShortName.lang, srcShortName.str));
                }
                dataSpecificationIEC61360.ShortName = shortNames;
            }
                
            dataSpecificationIEC61360.Unit = sourceDataSpecIEC61360.unit;
            if (sourceDataSpecIEC61360.unitId != null)
            {
                dataSpecificationIEC61360.UnitId =  ExtensionsUtil.ConvertReferenceFromV20(AasxCompatibilityModels.AdminShellV20.Reference.CreateNew(sourceDataSpecIEC61360.unitId.keys), ReferenceTypes.GlobalReference);
            }
            dataSpecificationIEC61360.ValueFormat = sourceDataSpecIEC61360.valueFormat;
            dataSpecificationIEC61360.SourceOfDefinition = sourceDataSpecIEC61360.sourceOfDefinition;
            dataSpecificationIEC61360.Symbol = sourceDataSpecIEC61360.symbol;
            dataSpecificationIEC61360.DataType = Stringification.DataTypeIec61360FromString(sourceDataSpecIEC61360.dataType);
            if (sourceDataSpecIEC61360.definition != null)
            {
                //TODO:jtikekar performance impact
                List<LangString> definitions = new List<LangString>();
                foreach (var srcDefinition in sourceDataSpecIEC61360.definition)
                {
                    definitions.Add(new LangString(srcDefinition.lang, srcDefinition.str));
                }
                dataSpecificationIEC61360.Definition = definitions;
            }
            return dataSpecificationIEC61360;
        }
    }
}
