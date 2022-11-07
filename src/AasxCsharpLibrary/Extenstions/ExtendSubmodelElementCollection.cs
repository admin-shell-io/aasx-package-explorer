using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendSubmodelElementCollection
    {
        public static T FindFirstIdShortAs<T>(this SubmodelElementCollection submodelElementCollection, string idShort) where T : ISubmodelElement
        {

            var submodelElement = submodelElementCollection.Value.Where(sme => (sme != null) && (sme is T) && sme.IdShort.Equals(idShort, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            return (T)submodelElement;
        }

        public static SubmodelElementCollection ConvertFromV10(this SubmodelElementCollection submodelElementCollection, AasxCompatibilityModels.AdminShellV10.SubmodelElementCollection sourceSmeCollection, bool shallowCopy = false)
        {
            if (sourceSmeCollection == null)
                return null;

            if (submodelElementCollection.Value == null)
            {
                submodelElementCollection.Value = new List<ISubmodelElement>();
            }

            if (!shallowCopy)
            {
                foreach (var submodelElementWrapper in sourceSmeCollection.value)
                {
                    var sourceSubmodelElement = submodelElementWrapper.submodelElement;
                    ISubmodelElement outputSubmodelElement = null;
                    if (sourceSubmodelElement != null)
                    {
                        outputSubmodelElement = outputSubmodelElement.ConvertFromV10(sourceSubmodelElement, shallowCopy);
                    }

                    submodelElementCollection.Value.Add(outputSubmodelElement);
                }
            }

            return submodelElementCollection;
        }

        public static SubmodelElementCollection ConvertFromV20(this SubmodelElementCollection submodelElementCollection, AasxCompatibilityModels.AdminShellV20.SubmodelElementCollection sourceSmeCollection, bool shallowCopy = false)
        {
            if (sourceSmeCollection == null)
                return null;

            if (submodelElementCollection.Value == null)
            {
                submodelElementCollection.Value = new List<ISubmodelElement>();
            }

            if (!shallowCopy)
            {
                foreach (var submodelElementWrapper in sourceSmeCollection.value)
                {
                    var sourceSubmodelElement = submodelElementWrapper.submodelElement;
                    ISubmodelElement outputSubmodelElement = null;
                    if (sourceSubmodelElement != null)
                    {
                        outputSubmodelElement = outputSubmodelElement.ConvertFromV20(sourceSubmodelElement, shallowCopy);
                    }

                    submodelElementCollection.Value.Add(outputSubmodelElement);
                }
            }

            return submodelElementCollection;
        }

        public static void Add(this SubmodelElementCollection submodelElementCollection, ISubmodelElement submodelElement)
        {
            if (submodelElementCollection.Value == null)
            {
                submodelElementCollection.Value = new List<ISubmodelElement>();
            }

            submodelElement.Parent = submodelElementCollection;
            submodelElementCollection.Value.Add(submodelElement);
        }

        public static void Insert(this SubmodelElementCollection submodelElementCollection, int index, ISubmodelElement submodelElement)
        {
            if (submodelElementCollection.Value == null)
            {
                submodelElementCollection.Value = new List<ISubmodelElement>();
            }

            submodelElement.Parent = submodelElementCollection;
            submodelElementCollection.Value.Insert(index, submodelElement);
        }

    }
}
