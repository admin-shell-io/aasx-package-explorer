using System;
using System.Collections.Generic;
using System.Linq;
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

            var submodelElements = submodel.submodelElements.Select(item => item.submodelElement);
            var submodelElementsAllOf = GetSubmodelElementsAllOf(schema);
            AddDefinitionsForSubmodelElements(schema, submodelElementsAllOf, submodelElements);

            var result = schema.ToString();

            return result;
        }

        private void AddArrayDefinitionForSubmodelElements(JObject schema)
        {
            schema["$defs"]["Elements"] =
                JObject.Parse(
                    @"{'properties': {'submodelElements': {'type': 'array', 'additionalItems': false, 'allOf': []}}}");

            AddReferenceToArray(GetRootAllOf, schema, "#/$defs/Elements");
        }

        private void AddDefinitionsForSubmodelElements(
            JObject schema,
            JArray targetAllOf,
            IEnumerable<AdminShellV20.SubmodelElement> submodelElements)
        {
            foreach (var submodelElement in submodelElements)
            {
                AddDefinitionForSubmodelElement(schema, targetAllOf, submodelElement);
            }
        }

        private void AddDefinitionForSubmodelElement(JObject schema, JArray targetAllOf, AdminShellV20.SubmodelElement submodelElement)
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
                propertiesObject["semanticId"] = JObject.Parse(@"{'properties': {'keys': {'type': 'array', 'additionalItems': false, 'allOf': []}}}");
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
                propertiesObject["value"] = JToken.Parse(@"{'type': 'array', 'additionalItems': false, 'allOf': []}");
                var collectionAllOf = SelectToken<JArray>(propertiesObject, "value.allOf");
                var submodelElements = submodelElementCollection.value.Select(item => item.submodelElement);
                AddDefinitionsForSubmodelElements(schema, collectionAllOf, submodelElements);
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
            AddReferenceToArray(targetAllOf, $"#/$defs/{elementName}");
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

        private void AddReferenceToArray(JArray targetArray, string referenceValue)
        {
            targetArray.Add(JObject.Parse($@"{{'$ref': '{referenceValue}' }}"));
        }

        private void AddReferenceToArray(Func<JObject,JArray> targetArrayProvider, JObject schema, string referenceValue)
        {
            var target = targetArrayProvider(schema);
            target.Add(JObject.Parse($@"{{'$ref': '{referenceValue}' }}"));
        }

        private JArray GetRootAllOf(JToken schema)
        {
            var result = SelectToken<JArray>(schema, "$.$defs.Root.allOf");
            return result;
        }

        private JArray GetSubmodelElementsAllOf(JObject schema)
        {
            var result = SelectToken<JArray>(schema, "$.$defs.Elements.properties.submodelElements.allOf");
            return result;
        }
    }
}
