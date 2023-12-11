using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0_RC02;
using AdminShellNS;

namespace Extensions
{
    public static class ExtendIDataSpecificationContent
    {
        public enum ContentTypes { NoInfo, Iec61360, PhysicalUnit}

        public static Key GetKeyForIec61360()
        {
            return new Key(KeyTypes.GlobalReference,
                "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/3/0");
        }

        public static Reference GetReferencForIec61360()
        {
            return new Reference(ReferenceTypes.GlobalReference, new List<Key> { GetKeyForIec61360() });
        }

        public static Key GetKeyForPhysicalUnit()
        {
            return new Key(KeyTypes.GlobalReference,
                "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationPhysicalUnit/3/0");
        }

        public static Key GetKeyFor(ContentTypes ct)
        {
            if (ct == ContentTypes.Iec61360)
                return GetKeyForIec61360();
            if (ct == ContentTypes.PhysicalUnit) 
                return GetKeyForPhysicalUnit();
            return null;
        }

        public static IDataSpecificationContent ContentFactoryFor(ContentTypes ct)
        {
            if (ct == ContentTypes.Iec61360)
                return new DataSpecificationIec61360(null);
            if (ct == ContentTypes.PhysicalUnit)
                return new DataSpecificationPhysicalUnit("", "", null);
            return null;
        }

        public static ContentTypes GuessContentTypeFor(Reference rf)
        {
            foreach (var v in AdminShellUtil.GetEnumValues<ContentTypes>(new[] { ContentTypes.NoInfo }))
                if (rf?.MatchesExactlyOneKey(GetKeyFor(v)) == true)
                    return v;
            return ContentTypes.NoInfo;
        }

        public static ContentTypes GuessContentTypeFor(IDataSpecificationContent content)
        {
            if (content is DataSpecificationIec61360)
                return ContentTypes.Iec61360;
            if (content is DataSpecificationPhysicalUnit)
                return ContentTypes.PhysicalUnit;
            return ContentTypes.NoInfo;
        }
    }
}
