using AdminShellNS.Display;
using System;
using System.Collections.Generic;
using System.Linq;

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

        // advanced checks

        public class ConstraintStat
        {
            /// <summary>
            /// Constraint AASd-107: If a first level child element in a SubmodelElementList has a semanticId 
            /// it shall be identical to SubmodelElementList/semanticIdListElement. 
            /// </summary>
            public bool AllChildSemIdMatch = true;

            /// <summary>
            /// Constraint AASd-108: All first level child elements in a SubmodelElementList shall have the 
            /// same submodel element type as specified in SubmodelElementList/typeValueListElement.
            /// </summary>
            public bool AllChildSmeTypeMatch = true;

            /// <summary>
            /// Constraint AASd-109: If SubmodelElementList/typeValueListElement equal to Property or Range, 
            /// SubmodelElementList/valueTypeListElement shall be set and all first level child elements in 
            /// the SubmodelElementList shall have the the value type as specified in 
            /// SubmodelElementList/valueTypeListElement
            /// </summary>
            public bool AllChildValueTypeMatch = true;
        }

        public static ConstraintStat EvalConstraintStat(this SubmodelElementList list)
        {
            // access
            var res = new ConstraintStat();
            if (list.Value == null)
                return res;

            // prepare SME type
            var smeTypeToCheck = list.TypeValueListElement;

            // prepare value type
            var valueTypeToCheck = list.ValueTypeListElement;

            // eval
            foreach (var sme in list.Value)
            {
                // need self description
                var smesd = sme.GetSelfDescription();
                if (smesd == null)
                    continue;

                // sem id?
                if (res.AllChildSemIdMatch
                    && list.SemanticIdListElement?.IsValid() == true
                    && sme.SemanticId?.IsValid() == true
                    && !list.SemanticIdListElement.Matches(sme.SemanticId))
                    res.AllChildSemIdMatch = false;

                // type of SME?
                if (res.AllChildSmeTypeMatch
                    && smesd.SmeType != smeTypeToCheck)
                    res.AllChildSmeTypeMatch = false;

                // value type to check
                if (valueTypeToCheck.HasValue
                    && res.AllChildValueTypeMatch
                    && sme is Property prop
                    && prop.ValueType != valueTypeToCheck.Value)
                    res.AllChildValueTypeMatch = false;

                if (valueTypeToCheck.HasValue
                    && res.AllChildValueTypeMatch
                    && sme is AasCore.Aas3_0.Range range
                    && range.ValueType != valueTypeToCheck.Value)
                    res.AllChildValueTypeMatch = false;
            }

            // ok 
            return res;
        }
    }
}
