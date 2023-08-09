/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// This interface allows to find some <c>PackageContainerRepoItem</c> by asking for AAS or AssetId.
    /// It does not intend to be a full fledged query interface, but allow to retrieve what is usful for
    /// automatic Reference link following etc.
    /// </summary>
    public interface IPackageContainerFind
    {
        PackageContainerRepoItem FindByAssetId(string aid);
        PackageContainerRepoItem FindByAasId(string aid);
        IEnumerable<PackageContainerRepoItem> EnumerateItems();
        bool Contains(PackageContainerRepoItem fi);
    }
}
