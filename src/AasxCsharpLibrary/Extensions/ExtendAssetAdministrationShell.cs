using AasCore.Aas3_0_RC02;
using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendAssetAdministrationShell
    {
        #region AasxPackageExplorer

        public static Tuple<string, string> ToCaptionInfo(this AssetAdministrationShell assetAdministrationShell)
        {
            var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", assetAdministrationShell.IdShort, "\"AAS\"");
            if (assetAdministrationShell.Administration != null)
                caption += "V" + assetAdministrationShell.Administration.Version + "." + assetAdministrationShell.Administration.Revision;

            var info = "";
            if (assetAdministrationShell.Id != null)
                info = $"[{assetAdministrationShell.Id}]";
            return Tuple.Create(caption, info);
        }

        public static IEnumerable<LocatedReference> FindAllReferences(this AssetAdministrationShell assetAdministrationShell)
        {
            // Asset
            //TODO:jtikekar support asset
            //if (assetAdministrationShell.AssetInformation != null)
            //    yield return new LocatedReference(assetAdministrationShell, assetAdministrationShell.AssetInformation);

            // Submodel references
            if (assetAdministrationShell.Submodels != null)
                foreach (var r in assetAdministrationShell.Submodels)
                    yield return new LocatedReference(assetAdministrationShell, r);

        }

        #endregion

        public static bool HasSubmodelReference(this AssetAdministrationShell assetAdministrationShell, Reference submodelReference)
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

        public static void AddSubmodelReference(this AssetAdministrationShell assetAdministrationShell, Reference newSubmodelReference)
        {
            if (assetAdministrationShell.Submodels == null)
            {
                assetAdministrationShell.Submodels = new List<Reference>();
            }

            assetAdministrationShell.Submodels.Add(newSubmodelReference);
        }

        //TODO:jtikekar: Change the name, currently based on older implementation
        public static string GetFriendlyName(this AssetAdministrationShell assetAdministrationShell)
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
                assetAdministrationShell.DerivedFrom = new Reference(ReferenceTypes.ModelReference, new List<Key>() { key });
            }

            if (sourceAas.submodelRefs != null || sourceAas.submodelRefs.Count != 0)
            {
                foreach (var submodelRef in sourceAas.submodelRefs)
                {
                    var keyList = new List<Key>();
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
                        assetAdministrationShell.Submodels = new List<Reference>();
                    }
                    assetAdministrationShell.Submodels.Add(new Reference(ReferenceTypes.ModelReference, keyList));
                }
            }

            if (sourceAas.hasDataSpecification != null)
            {
                //TODO: jtikekar : EmbeddedDataSpecification?? (as per old implementation)
                if (assetAdministrationShell.DataSpecifications == null)
                {
                    assetAdministrationShell.DataSpecifications = new List<Reference>();
                }
                foreach (var dataSpecification in sourceAas.hasDataSpecification.reference)
                {
                    assetAdministrationShell.DataSpecifications.Add(ExtensionsUtil.ConvertReferenceFromV10(dataSpecification, ReferenceTypes.GlobalReference));
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
                assetAdministrationShell.DerivedFrom = new Reference(ReferenceTypes.ModelReference, new List<Key>() { key });
            }

            if (sourceAas.submodelRefs != null || sourceAas.submodelRefs.Count != 0)
            {
                foreach (var submodelRef in sourceAas.submodelRefs)
                {
                    var keyList = new List<Key>();
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
                        assetAdministrationShell.Submodels = new List<Reference>();
                    }
                    assetAdministrationShell.Submodels.Add(new Reference(ReferenceTypes.ModelReference, keyList));
                }
            }

            if (sourceAas.hasDataSpecification != null)
            {
                //TODO: jtikekar : EmbeddedDataSpecification?? (as per old implementation)
                if (assetAdministrationShell.DataSpecifications == null)
                {
                    assetAdministrationShell.DataSpecifications = new List<Reference>();
                }

                //TODO: jtikekar: DataSpecificationContent?? (as per old implementation)
                foreach (var sourceDataSpec in sourceAas.hasDataSpecification)
                {
                    if (sourceDataSpec.dataSpecification != null)
                    {
                        assetAdministrationShell.DataSpecifications.Add(ExtensionsUtil.ConvertReferenceFromV20(sourceDataSpec.dataSpecification, ReferenceTypes.GlobalReference));
                    }
                }
            }

            return assetAdministrationShell;
        }
    }
}
