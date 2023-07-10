using AdminShellNS.Display;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendEntity
    {
        #region AasxPackageExplorer

        public static void Add(this Entity entity, ISubmodelElement submodelElement)
        {
            if (entity != null)
            {
                entity.Statements ??= new();

                submodelElement.Parent = entity;

                entity.Statements.Add(submodelElement);
            }
        }

        public static void Remove(this Entity entity, ISubmodelElement submodelElement)
        {
            if (entity != null)
            {
                if (entity.Statements != null)
                {
                    entity.Statements.Remove(submodelElement);
                }
            }
        }

        public static object AddChild(this Entity entity, ISubmodelElement childSubmodelElement, EnumerationPlacmentBase placement = null)
        {
            if (childSubmodelElement == null)
                return null;
            if (entity.Statements == null)
                entity.Statements = new();
            if (childSubmodelElement != null)
                childSubmodelElement.Parent = entity;
            entity.Statements.Add(childSubmodelElement);
            return childSubmodelElement;
        }

        #endregion
        public static Entity ConvertFromV20(this Entity entity, AasxCompatibilityModels.AdminShellV20.Entity sourceEntity)
        {
            if (sourceEntity == null)
            {
                return null;
            }

            if (sourceEntity.statements != null)
            {
                entity.Statements ??= new List<ISubmodelElement>();
                foreach (var submodelElementWrapper in sourceEntity.statements)
                {
                    var sourceSubmodelElement = submodelElementWrapper.submodelElement;
                    ISubmodelElement outputSubmodelElement = null;
                    if (sourceSubmodelElement != null)
                    {
                        outputSubmodelElement = outputSubmodelElement.ConvertFromV20(sourceSubmodelElement);
                    }
                    entity.Statements.Add(outputSubmodelElement);
                }
            }

            if (sourceEntity.assetRef != null)
            {
                //TODO (jtikekar, 0000-00-00): whether to convert to Global or specific asset id
                var assetRef = ExtensionsUtil.ConvertReferenceFromV20(sourceEntity.assetRef, ReferenceTypes.ExternalReference);
                entity.GlobalAssetId = assetRef.GetAsIdentifier();
            }

            return entity;
        }

        public static T FindFirstIdShortAs<T>(this Entity entity, string idShort) where T : ISubmodelElement
        {

            var submodelElements = entity.Statements.Where(sme => (sme != null) && (sme is T) && sme.IdShort.Equals(idShort, StringComparison.OrdinalIgnoreCase));

            if (submodelElements.Any())
            {
                return (T)submodelElements.First();
            }

            return default;
        }

        public static T CreateSMEForCD<T>(
            this Entity ent,
            ConceptDescription conceptDescription, string category = null, string idShort = null,
            string idxTemplate = null, int maxNum = 999, bool addSme = false, bool isTemplate = false)
                where T : ISubmodelElement
        {
            if (ent.Statements == null)
                ent.Statements = new List<ISubmodelElement>();
            return ent.Statements.CreateSMEForCD<T>(
                conceptDescription, category, idShort, idxTemplate, maxNum, addSme, isTemplate);
        }
    }
}
