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

                // TODO (MIHO, 2022-19-12): check again, see questions
                var oldid = "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/2/0";
                var newid = "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/3/0";
                if (sourceEmbeddedSpec.dataSpecification?.Matches("", false, "IRI", oldid, 
                    AasxCompatibilityModels.AdminShellV20.Key.MatchMode.Identification) == true)
                {
                    embeddedDataSpecification.DataSpecification.Keys[0].Value = newid;
                }
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
