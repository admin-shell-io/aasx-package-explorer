using AdminShellNS.Display;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendAnnotatedRelationshipElement
    {
        #region AasxPackageExplorer

        public static void Add(this AnnotatedRelationshipElement annotatedRelationshipElement, ISubmodelElement submodelElement)
        {
            if (annotatedRelationshipElement != null)
            {
                annotatedRelationshipElement.Annotations ??= new();

                submodelElement.Parent = annotatedRelationshipElement;

                annotatedRelationshipElement.Annotations.Add((IDataElement)submodelElement);
            }
        }

        public static void Remove(this AnnotatedRelationshipElement annotatedRelationshipElement, ISubmodelElement submodelElement)
        {
            if (annotatedRelationshipElement != null)
            {
                if (annotatedRelationshipElement.Annotations != null)
                {
                    annotatedRelationshipElement.Annotations.Remove((IDataElement)submodelElement);
                }
            }
        }

        public static object AddChild(this AnnotatedRelationshipElement annotatedRelationshipElement, ISubmodelElement childSubmodelElement, EnumerationPlacmentBase placement = null)
        {
            if (childSubmodelElement == null || childSubmodelElement is not IDataElement)
                return null;

            annotatedRelationshipElement.Annotations ??= new();

            if (childSubmodelElement != null)
                childSubmodelElement.Parent = annotatedRelationshipElement;

            annotatedRelationshipElement.Annotations.Add((IDataElement)childSubmodelElement);
            return childSubmodelElement;
        }

        #endregion
        public static AnnotatedRelationshipElement ConvertAnnotationsFromV20(this AnnotatedRelationshipElement annotatedRelationshipElement, AasxCompatibilityModels.AdminShellV20.AnnotatedRelationshipElement sourceAnnotedRelElement)
        {
            if (sourceAnnotedRelElement == null)
            {
                return null;
            }

            if (sourceAnnotedRelElement.annotations != null)
            {
                annotatedRelationshipElement.Annotations ??= new List<IDataElement>();
                foreach (var submodelElementWrapper in sourceAnnotedRelElement.annotations)
                {
                    var sourceSubmodelElement = submodelElementWrapper.submodelElement;
                    ISubmodelElement outputSubmodelElement = null;
                    if (sourceSubmodelElement != null)
                    {
                        outputSubmodelElement = outputSubmodelElement.ConvertFromV20(sourceSubmodelElement);
                    }
                    annotatedRelationshipElement.Annotations.Add((IDataElement)outputSubmodelElement);
                }
            }

            return annotatedRelationshipElement;
        }

        public static T FindFirstIdShortAs<T>(this AnnotatedRelationshipElement annotedRelationshipElement, string idShort) where T : ISubmodelElement
        {

            var submodelElements = annotedRelationshipElement.Annotations.Where(sme => sme != null && sme is T && sme.IdShort.Equals(idShort, StringComparison.OrdinalIgnoreCase));

            if (submodelElements.Any())
            {
                return (T)submodelElements.First();
            }

            return default;
        }

        public static AnnotatedRelationshipElement Set(this AnnotatedRelationshipElement elem,
            Reference first, Reference second)
        {
            elem.First = first;
            elem.Second = second;
            return elem;
        }

        public static AnnotatedRelationshipElement UpdateFrom(
            this AnnotatedRelationshipElement elem, ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((ISubmodelElement)elem).UpdateFrom(source);

            if (source is ReferenceElement srcRef)
            {
                if (srcRef.Value != null)
                    elem.First = srcRef.Value.Copy();
            }

            if (source is RelationshipElement srcRel)
            {
                if (srcRel.First != null)
                    elem.First = srcRel.First.Copy();
            }

            return elem;
        }

    }
}
