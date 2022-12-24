using AasCore.Aas3_0_RC02;
using AdminShellNS.Display;
using AdminShellNS.Extenstions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendSubmodelElementList
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
        public static void RecurseOnReferables(this SubmodelElementList submodelElementList,
            object state, Func<object, List<IReferable>, IReferable, bool> lambda,
            bool includeThis = false)
        {
            var parents = new List<IReferable>();
            if (includeThis)
            {
                lambda(state, null, submodelElementList);
                parents.Add(submodelElementList);
            }
            submodelElementList.Value?.RecurseOnReferables(state, parents, lambda);
        }

        public static void Add(this SubmodelElementList submodelElementList, ISubmodelElement submodelElement)
        {
            if (submodelElementList != null)
            {
                submodelElementList.Value ??= new();

                submodelElement.Parent = submodelElementList;

                submodelElementList.Value.Add(submodelElement);
            }
        }

        public static void Remove(this SubmodelElementList submodelElementList, ISubmodelElement submodelElement)
        {
            if (submodelElementList != null)
            {
                if (submodelElementList.Value != null)
                {
                    submodelElementList.Value.Remove(submodelElement);
                }
            }
        }

        public static object AddChild(this SubmodelElementList submodelElementList, ISubmodelElement childSubmodelElement, EnumerationPlacmentBase placement = null)
        {
            if (childSubmodelElement == null)
                return null;
            if (submodelElementList.Value == null)
                submodelElementList.Value = new();
            if (childSubmodelElement != null)
                childSubmodelElement.Parent = submodelElementList;
            submodelElementList.Value.Add(childSubmodelElement);
            return childSubmodelElement;
        }

        #endregion
        public static T FindFirstIdShortAs<T>(this SubmodelElementList submodelElementList, string idShort) where T : ISubmodelElement
        {

            var submodelElements = submodelElementList.Value.Where(sme => sme != null && sme is T && sme.IdShort.Equals(idShort, StringComparison.OrdinalIgnoreCase));

            if (submodelElements.Any())
            {
                return (T)submodelElements.First();
            }

            return default;
        }

        public static SubmodelElementList UpdateFrom(
            this SubmodelElementList elem, ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((ISubmodelElement)elem).UpdateFrom(source);

            if (source is SubmodelElementCollection srcColl)
            {
                if (srcColl.Value != null)
                    elem.Value = srcColl.Value.Copy();
            }

            if (source is Operation srcOp)
            {
                Action<List<ISubmodelElement>, List<OperationVariable>> appov = (dst, src) =>
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
