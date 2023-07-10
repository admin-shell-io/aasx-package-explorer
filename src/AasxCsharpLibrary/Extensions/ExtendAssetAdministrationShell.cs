using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Extensions
{
    public static class ExtendAssetAdministrationShell
    {
        #region AasxPackageExplorer

        public static Tuple<string, string> ToCaptionInfo(this IAssetAdministrationShell assetAdministrationShell)
        {
            var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", assetAdministrationShell.IdShort, "\"AAS\"");
            if (assetAdministrationShell.Administration != null)
                caption += "V" + assetAdministrationShell.Administration.Version + "." + assetAdministrationShell.Administration.Revision;

            var info = "";
            if (assetAdministrationShell.Id != null)
                info = $"[{assetAdministrationShell.Id}]";
            return Tuple.Create(caption, info);
        }

        public static IEnumerable<LocatedReference> FindAllReferences(this IAssetAdministrationShell assetAdministrationShell)
        {
            // Asset
            //TODO (jtikekar, 0000-00-00): support asset
            //if (assetAdministrationShell.AssetInformation != null)
            //    yield return new LocatedReference(assetAdministrationShell, assetAdministrationShell.AssetInformation);

            // Submodel references
            if (assetAdministrationShell.Submodels != null)
                foreach (var r in assetAdministrationShell.Submodels)
                    yield return new LocatedReference(assetAdministrationShell, r);

        }

        #endregion

        public static bool HasSubmodelReference(this IAssetAdministrationShell assetAdministrationShell, Reference submodelReference)
        {
            if (submodelReference == null)
            {
                return false;
            }

            foreach (var aasSubmodelReference in assetAdministrationShell.Submodels)
            {
                if (aasSubmodelReference.Matches(submodelReference))
                {
                    return true;
                }
            }

            return false;
        }

        public static void AddSubmodelReference(this IAssetAdministrationShell assetAdministrationShell, IReference newSubmodelReference)
        {
            if (assetAdministrationShell.Submodels == null)
            {
                assetAdministrationShell.Submodels = new List<IReference>();
            }

            assetAdministrationShell.Submodels.Add(newSubmodelReference);
        }

        //TODO (jtikekar, 0000-00-00): Change the name, currently based on older implementation
        public static string GetFriendlyName(this IAssetAdministrationShell assetAdministrationShell)
        {
            if (string.IsNullOrEmpty(assetAdministrationShell.IdShort))
            {
                return null;
            }

            return Regex.Replace(assetAdministrationShell.IdShort, @"[^a-zA-Z0-9\-_]", "_");
        }

        public static AssetAdministrationShell ConvertFromV10(this AssetAdministrationShell assetAdministrationShell, AasxCompatibilityModels.AdminShellV10.AdministrationShell sourceAas)
        {
            if (sourceAas == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourceAas.idShort))
            {
                assetAdministrationShell.IdShort = "";
            }
            else
            {
                assetAdministrationShell.IdShort = sourceAas.idShort;
            }

            if (sourceAas.description != null)
            {
                assetAdministrationShell.Description = ExtensionsUtil.ConvertDescriptionFromV10(sourceAas.description);
            }

            if (sourceAas.administration != null)
            {
                assetAdministrationShell.Administration = new AdministrativeInformation(version: sourceAas.administration.version, revision: sourceAas.administration.revision);
            }

            if (sourceAas.derivedFrom != null)
            {
                var key = new Key(KeyTypes.AssetAdministrationShell, sourceAas.identification.id);
                assetAdministrationShell.DerivedFrom = new Reference(ReferenceTypes.ModelReference, new List<IKey>() { key });
            }

            if (sourceAas.submodelRefs != null || sourceAas.submodelRefs.Count != 0)
            {
                foreach (var submodelRef in sourceAas.submodelRefs)
                {
                    var keyList = new List<IKey>();
                    foreach (var refKey in submodelRef.Keys)
                    {
                        //keyList.Add(new Key(ExtensionsUtil.GetKeyTypeFromString(refKey.type), refKey.value));
                        var keyType = Stringification.KeyTypesFromString(refKey.type);
                        if (keyType != null)
                        {
                            keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                        }
                        else
                        {
                            Console.WriteLine($"KeyType value {refKey.type} not found.");
                        }
                    }
                    if (assetAdministrationShell.Submodels == null)
                    {
                        assetAdministrationShell.Submodels = new List<IReference>();
                    }
                    assetAdministrationShell.Submodels.Add(new Reference(ReferenceTypes.ModelReference, keyList));
                }
            }

            if (sourceAas.hasDataSpecification != null)
            {
                //TODO (jtikekar, 0000-00-00): EmbeddedDataSpecification?? (as per old implementation)
                if (assetAdministrationShell.EmbeddedDataSpecifications == null)
                {
                    assetAdministrationShell.EmbeddedDataSpecifications = new List<IEmbeddedDataSpecification>();
                }
                foreach (var dataSpecification in sourceAas.hasDataSpecification.reference)
                {
                    assetAdministrationShell.EmbeddedDataSpecifications.Add(new EmbeddedDataSpecification(
                        ExtensionsUtil.ConvertReferenceFromV10(dataSpecification, ReferenceTypes.ExternalReference),
                        null));
                }
            }

            return assetAdministrationShell;
        }

        public static AssetAdministrationShell ConvertFromV20(this AssetAdministrationShell assetAdministrationShell, AasxCompatibilityModels.AdminShellV20.AdministrationShell sourceAas)
        {
            if (sourceAas == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourceAas.idShort))
            {
                assetAdministrationShell.IdShort = "";
            }
            else
            {
                assetAdministrationShell.IdShort = sourceAas.idShort;
            }

            if (sourceAas.description != null)
            {
                assetAdministrationShell.Description = ExtensionsUtil.ConvertDescriptionFromV20(sourceAas.description);
            }

            if (sourceAas.administration != null)
            {
                assetAdministrationShell.Administration = new AdministrativeInformation(version: sourceAas.administration.version, revision: sourceAas.administration.revision);
            }

            if (sourceAas.derivedFrom != null)
            {
                var key = new Key(KeyTypes.AssetAdministrationShell, sourceAas.identification.id);
                assetAdministrationShell.DerivedFrom = new Reference(ReferenceTypes.ModelReference, new List<IKey>() { key });
            }

            if (sourceAas.submodelRefs != null || sourceAas.submodelRefs.Count != 0)
            {
                foreach (var submodelRef in sourceAas.submodelRefs)
                {
                    var keyList = new List<IKey>();
                    foreach (var refKey in submodelRef.Keys)
                    {
                        //keyList.Add(new Key(ExtensionsUtil.GetKeyTypeFromString(refKey.type), refKey.value));
                        var keyType = Stringification.KeyTypesFromString(refKey.type);
                        if (keyType != null)
                        {
                            keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                        }
                        else
                        {
                            Console.WriteLine($"KeyType value {refKey.type} not found.");
                        }
                    }
                    if (assetAdministrationShell.Submodels == null)
                    {
                        assetAdministrationShell.Submodels = new List<IReference>();
                    }
                    assetAdministrationShell.Submodels.Add(new Reference(ReferenceTypes.ModelReference, keyList));
                }
            }

            if (sourceAas.hasDataSpecification != null && sourceAas.hasDataSpecification.Count > 0)
            {
                //TODO (jtikekar, 0000-00-00): EmbeddedDataSpecification?? (as per old implementation)
                if (assetAdministrationShell.EmbeddedDataSpecifications == null)
                {
                    assetAdministrationShell.EmbeddedDataSpecifications = new List<IEmbeddedDataSpecification>();
                }

                //TODO (jtikekar, 0000-00-00): DataSpecificationContent?? (as per old implementation)
                foreach (var sourceDataSpec in sourceAas.hasDataSpecification)
                {
                    if (sourceDataSpec.dataSpecification != null)
                    {
                        assetAdministrationShell.EmbeddedDataSpecifications.Add(
                            new EmbeddedDataSpecification(
                                ExtensionsUtil.ConvertReferenceFromV20(sourceDataSpec.dataSpecification, ReferenceTypes.ExternalReference),
                                null));
                    }
                }
            }

            return assetAdministrationShell;
        }
    }
}
