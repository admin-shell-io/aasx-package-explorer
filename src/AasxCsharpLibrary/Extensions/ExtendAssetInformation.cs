using System;

namespace Extensions
{
    public static class ExtendAssetInformation
    {
        #region AasxPackageExplorer

        public static Tuple<string, string> ToCaptionInfo(this IAssetInformation assetInformation)
        {
            //TODO:jtikekar support KeyType.AssetInformation
            //var caption = Key.AssetInformation;
            var caption = "AssetInformation";
            var info = "" + assetInformation.GlobalAssetId;
            return Tuple.Create(caption, info);
        }

        #endregion
        public static AssetInformation ConvertFromV10(this AssetInformation assetInformation, AasxCompatibilityModels.AdminShellV10.Asset sourceAsset)
        {
            //Determine AssetKind
            var assetKind = AssetKind.Instance;
            if (sourceAsset.kind.IsType)
            {
                assetKind = AssetKind.Type;
            }

            assetInformation.AssetKind = assetKind;


            //Assign GlobalAssetId
            assetInformation.GlobalAssetId = sourceAsset.identification.id;

            return assetInformation;
        }

        public static AssetInformation ConvertFromV20(this AssetInformation assetInformation, AasxCompatibilityModels.AdminShellV20.Asset sourceAsset)
        {
            //Determine AssetKind
            var assetKind = AssetKind.Instance;
            if (sourceAsset.kind.IsType)
            {
                assetKind = AssetKind.Type;
            }

            assetInformation.AssetKind = assetKind;


            //Assign GlobalAssetId
            assetInformation.GlobalAssetId = sourceAsset.identification.id;

            return assetInformation;
        }
    }
}
