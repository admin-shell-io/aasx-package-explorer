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
                    @"{'properties': {'submodelElements': {'type': 'array', 'allOf': []}}}");

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
            // Ignore arbitrary submodel elements
            if (submodelElement.idShort == "{arbitrary}")
                return;

            var isDynamicIdShort = false;
            var elementName = submodelElement.idShort;

            if (submodelElement.idShort.EndsWith("{00}"))
            {
                isDynamicIdShort = true;
                elementName = submodelElement.idShort.Replace("{00}", "");
            }

            var allOfEntry = JObject.Parse($@"{{'contains': {{'$ref': '#/$defs/{elementName}'}}}}");
            var submodelElementDefinition = JObject.Parse(@"{'properties': {}}");
            var propertiesObject = SelectToken<JObject>(submodelElementDefinition, "properties");


            // idShort
            if (isDynamicIdShort)
            {
                var idShortPattern = $@"{elementName}\\d{{2}}";
                propertiesObject["idShort"] = JObject.Parse($@"{{'pattern': '^{idShortPattern}$'}}");
            } else
            {
                propertiesObject["idShort"] = JObject.Parse($@"{{'const': '{submodelElement.idShort}'}}");
            }
            
            // kind
            propertiesObject["kind"] = JObject.Parse(@"{'const': 'Instance'}");

            // modelType
            var modelType = submodelElement.JsonModelType.name;
            propertiesObject["modelType"] = JObject.Parse($@"{{'properties': {{'name': {{'const': '{modelType}'}}}}}}");

            // semanticId
            if (!submodelElement.semanticId.IsEmpty)
            {
                propertiesObject["semanticId"] = JObject.Parse(@"{'properties': {'keys': {'type': 'array', 'allOf': []}}}");
                var allOf = SelectToken<JArray>(propertiesObject, "semanticId.properties.keys.allOf");
                submodelElement.semanticId.Keys.ForEach(key =>
                {
                    var containsDef = JObject.Parse($@"
                            {{'contains': 
                                {{'properties': 
                                    {{
                                        'type': {{'const': '{key.type}'}},
                                        'local': {{'const': {key.local.ToString().ToLower()}}},
                                        'value': {{'const': '{key.value}'}},
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
                if (!string.IsNullOrEmpty(valueType))
                    propertiesObject["valueType"] = JObject.Parse($@"{{'properties': {{'dataObjectType': {{'properties': {{'name': {{'const': '{valueType}'}}}}}}}}}}");
            }

            if (submodelElement is AdminShellV20.SubmodelElementCollection submodelElementCollection)
            {
                var submodelElements = submodelElementCollection.value
                    .Select(item => item.submodelElement)
                    .ToArray();


                propertiesObject["value"] = JToken.Parse($@"{{'type': 'array', 'allOf': []}}");
                var collectionAllOf = SelectToken<JArray>(propertiesObject, "value.allOf");
                
                AddDefinitionsForSubmodelElements(schema, collectionAllOf, submodelElements);
            }

            // Multiplicity
            var multiplicityQualifier = submodelElement.qualifiers.FindType("Multiplicity");
            if (multiplicityQualifier != null)
            {
                switch (multiplicityQualifier.value)
                {
                    case "ZeroToOne":
                        allOfEntry["minContains"] = 0;
                        allOfEntry["maxContains"] = 1;
                        break;
                    case "One":
                        allOfEntry["minContains"] = 1;
                        allOfEntry["maxContains"] = 1;
                        break;
                    case "ZeroToMany":
                        allOfEntry["minContains"] = 0;
                        break;
                    case "OneToMany":
                        allOfEntry["minContains"] = 1;
                        break;
                }
            }

            schema["$defs"][elementName] = submodelElementDefinition;
            targetAllOf.Add(allOfEntry);
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
