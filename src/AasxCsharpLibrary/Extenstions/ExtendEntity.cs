using AasCore.Aas3_0_RC02;
using AdminShellNS.Display;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
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

                entity.Statements.Add((IDataElement)submodelElement);
            }
        }

        public static void Remove(this Entity entity, ISubmodelElement submodelElement)
        {
            if(entity != null)
            {
                if(entity.Statements != null)
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
                entity.Statements = new ();
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
                //TODO:jtikekar whether to convert to Global or specific asset id
                var assetRef = ExtensionsUtil.ConvertReferenceFromV20(sourceEntity.assetRef, ReferenceTypes.GlobalReference);
                entity.GlobalAssetId = assetRef;
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
    }
}
