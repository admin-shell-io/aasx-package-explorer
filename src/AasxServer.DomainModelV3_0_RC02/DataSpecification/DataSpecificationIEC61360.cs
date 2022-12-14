using AasCore.Aas3_0_RC02.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AasCore.Aas3_0_RC02.HasDataSpecification
{
    public class DataSpecificationIEC61360
    {
        // static member
        [XmlIgnore]
        //[JsonIgnore]  //TODO:jtikekar uncomment and support, NewtonSoft vs System.Json
        public static string[] DataTypeNames = {
                "STRING",
                "STRING_TRANSLATABLE",
                "REAL_MEASURE",
                "REAL_COUNT",
                "REAL_CURRENCY",
                "INTEGER_MEASURE",
                "INTEGER_COUNT",
                "INTEGER_CURRENCY",
                "BOOLEAN",
                "URL",
                "RATIONAL",
                "RATIONAL_MEASURE",
                "TIME",
                "TIMESTAMP",
                "DATE" };

        // members
        // TODO (MIHO, 2020-08-27): According to spec, cardinality is [1..1][1..n]
        // these cardinalities are NOT MAINTAINED in ANY WAY by the system
        public LangStringSetIEC61360 preferredName = new LangStringSetIEC61360();

        // TODO (MIHO, 2020-08-27): According to spec, cardinality is [0..1][1..n]
        // these cardinalities are NOT MAINTAINED in ANY WAY by the system
        public LangStringSetIEC61360 shortName = null;

        [MetaModelName("DataSpecificationIEC61360.unit")]
        [TextSearchable]
        [CountForHash]
        public string unit = "";

        //UnitId is a Global Reference
        public Reference unitId = null;

        [MetaModelName("DataSpecificationIEC61360.valueFormat")]
        [TextSearchable]
        [CountForHash]
        public string valueFormat = null;

        [MetaModelName("DataSpecificationIEC61360.sourceOfDefinition")]
        [TextSearchable]
        [CountForHash]
        public string sourceOfDefinition = null;

        [MetaModelName("DataSpecificationIEC61360.symbol")]
        [TextSearchable]
        [CountForHash]
        public string symbol = null;

        [MetaModelName("DataSpecificationIEC61360.dataType")]
        [TextSearchable]
        [CountForHash]
        public string dataType = "";

        // TODO (MIHO, 2020-08-27): According to spec, cardinality is [0..1][1..n]
        // these cardinalities are NOT MAINTAINED in ANY WAY by the system
        public LangStringSetIEC61360 definition = null;

        public static string GetIdentifier()
        {
            return "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/3/0";
        }

        public static DataSpecificationIEC61360 CreateNew(
                string[] preferredName = null,
                string shortName = "",
                string unit = "",
                Reference unitId = null,
                string valueFormat = null,
                string sourceOfDefinition = null,
                string symbol = null,
                string dataType = "",
                string[] definition = null
            )
        {
            var d = new DataSpecificationIEC61360();
            if (preferredName != null)
            {
                d.preferredName = new LangStringSetIEC61360();
                d.preferredName.AddRange(CreateManyFromStringArray(preferredName));
            }
            d.shortName = new LangStringSetIEC61360
            {
                new LangString("EN?", shortName)
            };
            d.unit = unit;
            d.unitId = unitId;
            d.valueFormat = valueFormat;
            d.sourceOfDefinition = sourceOfDefinition;
            d.symbol = symbol;
            d.dataType = dataType;
            if (definition != null)
            {
                if (d.definition == null)
                    d.definition = new LangStringSetIEC61360();
                d.definition = new LangStringSetIEC61360();
                d.definition.AddRange(CreateManyFromStringArray(definition));
            }
            return (d);
        }

        private static List<LangString> CreateManyFromStringArray(string[] s)
        {
            var r = new List<LangString>();
            var i = 0;
            while ((i + 1) < s.Length)
            {
                r.Add(new LangString(s[i], s[i + 1]));
                i += 2;
            }
            return r;
        }
    }
}
