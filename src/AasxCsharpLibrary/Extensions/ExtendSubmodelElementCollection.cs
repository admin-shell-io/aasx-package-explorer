using AdminShellNS.Display;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendSubmodelElementCollection
    {
        #region AasxPackageExplorer

        /// <summary>
        /// Recurses on all Submodel elements of a Submodel or SME, which allows children.
        /// The <c>state</c> object will be passed to the lambda function in order to provide
        /// stateful approaches. Include this element, as well. 
        /// </summary>
        /// <param name="state">State object to be provided to lambda. Could be <c>null.</c></param>
        /// <param name="lambda">The lambda function as <c>(state, parents, SME)</c>
        /// The lambda shall return <c>TRUE</c> in order to deep into recursion.</param>
        /// <param name="includeThis">Include this element as well. <c>parents</c> will then 
        /// include this element as well!</param>
        public static void RecurseOnReferables(this SubmodelElementCollection submodelElementCollection,
            object state, Func<object, List<IReferable>, IReferable, bool> lambda,
            bool includeThis = false)
        {
            var parents = new List<IReferable>();
            if (includeThis)
            {
                lambda(state, null, submodelElementCollection);
                parents.Add(submodelElementCollection);
            }
            submodelElementCollection.Value?.RecurseOnReferables(state, parents, lambda);
        }

        public static void Remove(this SubmodelElementCollection submodelElementCollection, ISubmodelElement submodelElement)
        {
            if (submodelElementCollection != null)
            {
                if (submodelElementCollection.Value != null)
                {
                    submodelElementCollection.Value.Remove(submodelElement);
                }
            }
        }

        public static object AddChild(this SubmodelElementCollection submodelElementCollection, ISubmodelElement childSubmodelElement, EnumerationPlacmentBase placement = null)
        {
            if (childSubmodelElement == null)
                return null;
            if (submodelElementCollection.Value == null)
                submodelElementCollection.Value = new();
            if (childSubmodelElement != null)
                childSubmodelElement.Parent = submodelElementCollection;
            submodelElementCollection.Value.Add(childSubmodelElement);
            return childSubmodelElement;
        }

        #endregion
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
            submodelElementCollection.Value ??= new List<ISubmodelElement>();

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

        public static T CreateSMEForCD<T>(
            this SubmodelElementCollection smc,
            ConceptDescription conceptDescription, string category = null, string idShort = null,
            string idxTemplate = null, int maxNum = 999, bool addSme = false, bool isTemplate = false)
                where T : ISubmodelElement
        {
            if (smc.Value == null)
                smc.Value = new List<ISubmodelElement>();
            return smc.Value.CreateSMEForCD<T>(
                conceptDescription, category, idShort, idxTemplate, maxNum, addSme, isTemplate);
        }

        public static SubmodelElementCollection UpdateFrom(
            this SubmodelElementCollection elem, ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((ISubmodelElement)elem).UpdateFrom(source);

            if (source is SubmodelElementList srcList)
            {
                if (srcList.Value != null)
                    elem.Value = srcList.Value.Copy();
            }

            if (source is Operation srcOp)
            {
                Action<List<ISubmodelElement>, List<IOperationVariable>> appov = (dst, src) =>
                {
                    if (src == null)
                        return;
                    foreach (var ov in src)
                        if (ov.Value != null)
                            dst.Append(ov.Value.Copy());
                };

                elem.Value = new();
                appov(elem.Value, srcOp.InputVariables);
                appov(elem.Value, srcOp.InoutputVariables);
                appov(elem.Value, srcOp.OutputVariables);
                if (elem.Value.Count < 1)
                    elem.Value = null;
            }

            return elem;
        }

    }
}
