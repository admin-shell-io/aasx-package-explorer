using AdminShellNS;
using Newtonsoft.Json.Linq;

namespace AasxSchemaExport
{
    public class SubmodelTemplateJsonSchemaExporterV20 : ISchemaExporter
    {
        public string ExportSchema(AdminShellV20.Submodel submodel)
        {
            var schema = new JObject();

            schema["$schema"] = "https://json-schema.org/draft/2019-09/schema";
            schema["title"] = $"AssetAdministrationShellSubmodel{submodel.idShort}";
            schema["type"] = "object";
            schema["unevaluatedProperties"] = false;

            schema["$ref"] = "#/definitions/Root";

            schema["definitions"] = JToken.Parse("{\"Root\": {}}");
            schema["definitions"]["Root"] = JToken.Parse("{\"allOf\": []}");
            var rootAllOfDef = schema["definitions"]["Root"]["allOf"] as JArray;
            
            rootAllOfDef.Insert(0, JToken.Parse("{\"$ref\": \"aas.json#/definitions/Submodel\" }"));




            return schema.ToString();

        }
    }
}
