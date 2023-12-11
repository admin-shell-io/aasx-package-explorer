using System.Collections.Generic;

namespace AasxPackageLogic.PackageCentral.AasxFileServerInterface.Models
{
    public class PackageDescriptionPagedResult
    {
        public List<PackageDescription> result { get; set; }
        public PagedResultPagingMetadata paging_metadata { get; set; }
    }
}
