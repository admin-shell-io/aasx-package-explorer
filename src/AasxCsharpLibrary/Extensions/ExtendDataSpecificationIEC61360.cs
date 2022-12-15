using AasCore.Aas3_0_RC02;
using AasCore.Aas3_0_RC02.HasDataSpecification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendDataSpecificationIEC61360
    {
        public static DataSpecificationIEC61360 ConvertFromV20(this DataSpecificationIEC61360 dataSpecificationIEC61360, AasxCompatibilityModels.AdminShellV20.DataSpecificationIEC61360 sourceDataSpecIEC61360)
        {
            if (sourceDataSpecIEC61360.preferredName != null)
            {
                //TODO:jtikekar performance impact
                List<LangString> preferredNames = new List<LangString>();
                foreach(var srcPrefName in sourceDataSpecIEC61360.preferredName)
                {
                    preferredNames.Add(new LangString(srcPrefName.lang, srcPrefName.str));
                }
                dataSpecificationIEC61360.preferredName = LangStringSetIEC61360.CreateFrom(preferredNames);
            }
            
            if (sourceDataSpecIEC61360.preferredName != null)
            {
                //TODO:jtikekar performance impact
                List<LangString> shortNames = new List<LangString>();
                foreach(var srcShortName in sourceDataSpecIEC61360.shortName)
                {
                    shortNames.Add(new LangString(srcShortName.lang, srcShortName.str));
                }
                dataSpecificationIEC61360.shortName = LangStringSetIEC61360.CreateFrom(shortNames);
            }
                
            dataSpecificationIEC61360.unit = sourceDataSpecIEC61360.unit;
            if (sourceDataSpecIEC61360.unitId != null)
            {
                dataSpecificationIEC61360.unitId =  ExtensionsUtil.ConvertReferenceFromV20(AasxCompatibilityModels.AdminShellV20.Reference.CreateNew(sourceDataSpecIEC61360.unitId.keys), ReferenceTypes.GlobalReference);
            }
            dataSpecificationIEC61360.valueFormat = sourceDataSpecIEC61360.valueFormat;
            dataSpecificationIEC61360.sourceOfDefinition = sourceDataSpecIEC61360.sourceOfDefinition;
            dataSpecificationIEC61360.symbol = sourceDataSpecIEC61360.symbol;
            dataSpecificationIEC61360.dataType = sourceDataSpecIEC61360.dataType;
            if (sourceDataSpecIEC61360.definition != null)
            {
                //TODO:jtikekar performance impact
                List<LangString> definitions = new List<LangString>();
                foreach (var srcDefinition in sourceDataSpecIEC61360.definition)
                {
                    definitions.Add(new LangString(srcDefinition.lang, srcDefinition.str));
                }
                dataSpecificationIEC61360.definition = LangStringSetIEC61360.CreateFrom(definitions);
            }
            return dataSpecificationIEC61360;
        }
    }
}
