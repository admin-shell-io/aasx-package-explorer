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

            schema["allOf"] = JArray.Parse(@"[{'$ref': '#/$defs/Root'}]");

            schema["$defs"] = JObject.Parse(@"{'Root': {'allOf': []}}");

            AddDefinitionForSubmodel(schema);
            AddDefinitionForIdentifiable(schema);
            AddDefinitionsForSubmodelElements(schema);


            return schema.ToString();
        }

        private void AddDefinitionsForSubmodelElements(JToken schema)
        {
            schema.
        }

        private void AddDefinitionForSubmodel(JToken schema)
        {
            var rootAllOf = GetDefinitionRootAllOf(schema);
            rootAllOf.Add(JObject.Parse(@"{'$ref': 'aas.json#/$defs/Submodel'}"));
        }

        private void AddDefinitionForIdentifiable(JToken schema)
        {
            var rootAllOf = GetDefinitionRootAllOf(schema);
            rootAllOf.Add(JObject.Parse(@"{'$ref': '#/$defs/Identifiable'}"));

            schema["$defs"]["Identifiable"] = JObject.Parse(@"
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
            var rootAllOf = schema["$defs"]?["Root"]?["allOf"] as JArray;
            return rootAllOf;
        }
    }
}
