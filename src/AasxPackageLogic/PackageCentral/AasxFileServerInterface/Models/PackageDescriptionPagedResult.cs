/*
Copyright (c) 2018-2023 

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System.Collections.Generic;

namespace AasxPackageLogic.PackageCentral.AasxFileServerInterface.Models
{
    public class PackageDescriptionPagedResult
    {
        public List<PackageDescription> result { get; set; }
        public PagedResultPagingMetadata paging_metadata { get; set; }
    }
}
