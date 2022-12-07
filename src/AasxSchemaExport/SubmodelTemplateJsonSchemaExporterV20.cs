using System;
using System.Linq;
using AasxCompatibilityModels;
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

            AddReferenceForSubmodel(schema);
            AddDefinitionForIdentifiable(schema);
            AddArrayDefinitionForSubmodelElements(schema);
            
            foreach (var submodelElement in submodel.submodelElements.Select(item => item.submodelElement))
            {
                AddDefinitionForSubmodelElement(schema, submodelElement);
            }

            return schema.ToString();
        }

        private void AddArrayDefinitionForSubmodelElements(JObject schema)
        {
            schema["$defs"]["Elements"] =
                JObject.Parse(
                    @"{'type': 'array', 'additionalItems': 'false', 'properties': {'submodelElements': {'allOf': []}}}");

            AddReferenceToArray(GetRootAllOf, schema, "#/$defs/Elements");
        }


        private void AddDefinitionForSubmodelElement(JObject schema, AdminShellV20.SubmodelElement submodelElement)
        {
            var elementName = submodelElement.idShort;
            schema["$defs"][elementName] = JObject.Parse(@"{'contains': {'properties': {}}}");

            var propertiesObject = SelectToken<JObject>(schema, $"$.$defs.{elementName}.contains.properties");

            // idShort
            propertiesObject["idShort"] = JObject.Parse($@"{{'const': '{elementName}'}}");

            // kind
            propertiesObject["kind"] = JObject.Parse(@"{'const': 'Instance'}");

            // modelType
            var modelType = submodelElement.JsonModelType.name;
            propertiesObject["modelType"] = JObject.Parse($@"{{'properties': {{'name': {{'const': '{modelType}'}}}}}}");

            // semanticId
            if (!submodelElement.semanticId.IsEmpty)
            {
                propertiesObject["semanticId"] = JObject.Parse(@"{'properties': {'keys': {'allOf': []}}}");
                var allOf = SelectToken<JArray>(propertiesObject, "semanticId.properties.keys.allOf");
                submodelElement.semanticId.Keys.ForEach(key =>
                {
                    var containsDef = JObject.Parse($@"
                            {{'contains': 
                                {{'properties': 
                                    {{
                                        'type': {{'const': '{key.type}'}},
                                        'local': {{'const': '{key.local.ToString().ToLower()}'}},
                                        'referenceValue': {{'const': '{key.value}'}},
                                        'idType': {{'const': '{key.idType}'}},
                                    }}
                                }}
                            }}");
                    allOf.Add(containsDef);
                });
            }


            if (submodelElement is AdminShellV20.Property property)
            {
                // valueType
                var valueType = property.JsonValueType.dataObjectType.name;
                propertiesObject["valueType"] = JObject.Parse($@"{{'properties': {{'dataObjectType': {{'properties': {{'name': {{'const': '{valueType}'}}}}}}}}}}");
            }

            if (submodelElement is AdminShellV20.SubmodelElementCollection submodelElementCollection)
            {
                propertiesObject["value"] = new JArray();
                var submodelElements = submodelElementCollection.value.Select(item => item.submodelElement);

            }

            // Multiplicity
            var multiplicityQualifier = submodelElement.qualifiers.FindType("Multiplicity");
            if (multiplicityQualifier != null)
            {
                switch (multiplicityQualifier.value)
                {
                    case "ZeroToOne":
                        schema["$defs"][elementName]["minContains"] = 0;
                        schema["$defs"][elementName]["maxContains"] = 1;
                        break;
                    case "One":
                        schema["$defs"][elementName]["minContains"] = 1;
                        schema["$defs"][elementName]["maxContains"] = 1;
                        break;
                    case "ZeroToMany":
                        schema["$defs"][elementName]["minContains"] = 0;
                        break;
                    case "OneToMany":
                        schema["$defs"][elementName]["minContains"] = 1;
                        break;
                }
            }

            AddReferenceToArray(GetSubmodelElementsAllOf, schema, $"#/$defs/{elementName}");
        }

        private void AddReferenceForSubmodel(JObject schema)
        {
            AddReferenceToArray(GetRootAllOf, schema, "aas.json#/$defs/Submodel");
        }

        private void AddDefinitionForIdentifiable(JToken schema)
        {
            var rootAllOf = GetRootAllOf(schema);
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

        private T SelectToken<T>(JToken source, string path) where T: JToken
        {
            var result = source.SelectToken(path) as T;
            if (result == null)
                throw new Exception($"Token was not found. {path}");

            return result;
        }

        private void AddReferenceToArray(Func<JObject,JArray> targetArrayProvider, JObject schema, string referenceValue)
        {
            var target = targetArrayProvider(schema);
            target.Add(JObject.Parse($@"{{'$ref': '{referenceValue}' }}"));
        }

        private JArray GetRootAllOf(JToken schema)
        {
            var result = schema["$defs"]?["Root"]?["allOf"] as JArray;
            return result;
        }

        private JArray GetSubmodelElementsAllOf(JObject schema)
        {
            var result = schema["$defs"]?["Elements"]?["properties"]?["submodelElements"]?["allOf"] as JArray;
            return result;
        }
    }
}
