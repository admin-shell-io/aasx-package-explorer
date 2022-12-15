using AasCore.Aas3_0_RC02.HasDataSpecification;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendDataSpecificationContent
    {
        public static DataSpecificationContent ConvertFromV20(this DataSpecificationContent dataSpecificationContent, AasxCompatibilityModels.AdminShellV20.DataSpecificationContent sourceDataSpecContent)
        {
            if (sourceDataSpecContent.dataSpecificationIEC61360 != null)
            {
                var newDataSpecIEC61360 = new DataSpecificationIEC61360();
                newDataSpecIEC61360.ConvertFromV20(sourceDataSpecContent.dataSpecificationIEC61360);
                dataSpecificationContent.DataSpecificationIEC61360 = newDataSpecIEC61360;
            }
            return dataSpecificationContent;
        }
    }
}
