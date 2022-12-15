using AasCore.Aas3_0_RC02.HasDataSpecification;
using AasxCompatibilityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendHasDataSpecification
    {
        public static HasDataSpecification ConvertFromV20(this HasDataSpecification embeddedDataSpecifications, AasxCompatibilityModels.AdminShellV20.HasDataSpecification sourceSpecification)
        {
            foreach(var sourceSpec in sourceSpecification)
            {
                var newEmbeddedSpec = new EmbeddedDataSpecification();
                newEmbeddedSpec.ConvertFromV20(sourceSpec);
                embeddedDataSpecifications.Add(newEmbeddedSpec);
            }

            return embeddedDataSpecifications;
        }
    }
}
