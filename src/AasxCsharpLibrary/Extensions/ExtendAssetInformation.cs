/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System;

namespace Extensions
{
    public static class ExtendAssetInformation
    {
        #region AasxPackageExplorer

        public static Tuple<string, string> ToCaptionInfo(this IAssetInformation assetInformation)
        {
            // dead-csharp off
            //TODO (jtikekar, 0000-00-00): support KeyType.AssetInformation
            //var caption = Key.AssetInformation;
            // dead-csharp on
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
