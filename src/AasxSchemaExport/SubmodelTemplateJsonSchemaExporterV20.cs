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

            schema["definitions"] = JObject.Parse(@"{'Root': {'allOf': []}}");

            AddDefinitionForSubmodel(schema);
            AddDefinitionForIdentifiable(schema);

            return schema.ToString();
        }

        private void AddDefinitionForSubmodel(JToken schema)
        {
            var rootAllOf = GetDefinitionRootAllOf(schema);
            rootAllOf.Add(JObject.Parse(@"{'$ref': 'aas.json#/definitions/Submodel'}"));
        }

        private void AddDefinitionForIdentifiable(JToken schema)
        {
            var rootAllOf = GetDefinitionRootAllOf(schema);
            rootAllOf.Add(JObject.Parse(@"{'$ref': '#/definitions/Identifiable'}"));

            schema["definitions"]["Identifiable"] = JObject.Parse(@"
                {
                    'type': 'object',
                    'properties': {
                        'modelType': {
                            'type': 'object',
                            'name': {
                                'const': 'Submodel'
                            }
                        }
                    }
                }");
        }

        private JArray GetDefinitionRootAllOf(JToken schema)
        {
            var rootAllOf = schema["definitions"]?["Root"]?["allOf"] as JArray;
            return rootAllOf;
        }
    }
}
