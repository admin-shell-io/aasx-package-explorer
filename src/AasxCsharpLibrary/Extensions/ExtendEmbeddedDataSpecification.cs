using AasCore.Aas3_0_RC02;
using AasCore.Aas3_0_RC02.HasDataSpecification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataSpecificationContent = AasCore.Aas3_0_RC02.HasDataSpecification.DataSpecificationContent;

namespace Extensions
{
    public static class ExtendEmbeddedDataSpecification
    {
        public static EmbeddedDataSpecification ConvertFromV20(this EmbeddedDataSpecification embeddedDataSpecification, AasxCompatibilityModels.AdminShellV20.EmbeddedDataSpecification sourceEmbeddedSpec)
        {
            if(sourceEmbeddedSpec != null)
            {
                embeddedDataSpecification.DataSpecification = ExtensionsUtil.ConvertReferenceFromV20(sourceEmbeddedSpec.dataSpecification, ReferenceTypes.GlobalReference);
            }

            if(sourceEmbeddedSpec.dataSpecificationContent != null)
            {
                var newDataSpecContent = new DataSpecificationContent();
                newDataSpecContent.ConvertFromV20(sourceEmbeddedSpec.dataSpecificationContent);
                embeddedDataSpecification.DataSpecificationContent = newDataSpecContent;
            }
            return embeddedDataSpecification;
        }
    }
}
