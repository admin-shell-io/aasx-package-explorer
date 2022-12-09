using System;
using System.Collections.Generic;
using System.Linq;
using AdminShellNS;
using Newtonsoft.Json.Linq;

namespace AasxSchemaExport
{
    public class SubmodelTemplateJsonSchemaExporterV20 : ISchemaExporter
    {
        private const string MetaModelSchemaUrl = "aas.json";
        private const string MetaModelSubmodelDefinitionPath = "#/definitions/Submodel";

        private List<Func<Context, bool>> _submodelElementDefinitionSuppliers;

        public string ExportSchema(AdminShellV20.Submodel submodel)
        {
            var schema = new JObject();

            InitSubmodelElementDefinitionSuppliers();
            AddRootData(schema, submodel);
            AddSubmodelReference(schema);
            AddDefinitionForIdentifiable(schema);
            AddDefinitionForSubmodelElements(schema, submodel);

            var result = schema.ToString();
            return result;
        }

        private void InitSubmodelElementDefinitionSuppliers()
        {
            _submodelElementDefinitionSuppliers = new List<Func<Context, bool>>
            {
                SupplyGateArbitrary, 
                SupplyIdShort,
                SupplyKind,
                SupplyModelType,
                SupplySemanticId,
                SupplyValueType,
                SupplyMultiplicity,
                SupplyCollectionSubmodelElements,
                SupplyCreatedDefinition
            };
        }

        private void AddRootData(JObject schema, AdminShellV20.Submodel submodel)
        {
            schema[Tokens.Schema] = "https://json-schema.org/draft/2019-09/schema";
            schema[Tokens.Title] = $"AssetAdministrationShell{submodel.idShort}";
            schema[Tokens.Type] = "object";
            schema[Tokens.UnevaluatedProperties] = false;
            schema[Tokens.AllOf] = new JArray();
            schema[Tokens.Definitions] = new JObject();
        }

        private void AddSubmodelReference(JObject schema)
        {
            var reference = $"{MetaModelSchemaUrl}{MetaModelSubmodelDefinitionPath}";
            AddReferenceToArray(schema, GetRootAllOf, reference);
        }

        private void AddDefinitionForIdentifiable(JObject schema)
        {
            AddDefinitionReferenceToRootAllOf(schema, Tokens.Identifiable);

            schema[Tokens.Definitions][Tokens.Identifiable] = JObject.Parse($@"
            {{
                '{Tokens.Type}': 'object',
                '{Tokens.Properties}': {{
                    '{Tokens.ModelType}': {{
                        '{Tokens.Type}': 'object',
                        '{Tokens.Properties}': {{
                            '{Tokens.Name}': {{
                                '{Tokens.Const}': 'Submodel'
                            }}
                        }}
                    }}
                }}
            }}");
        }

        private void AddDefinitionForSubmodelElements(JObject schema, AdminShellV20.Submodel submodel)
        {
            AddDefinitionReferenceToRootAllOf(schema, Tokens.Elements);

            schema[Tokens.Definitions][Tokens.Elements] = JObject.Parse($@"
            {{
                '{Tokens.Properties}': {{
                    '{Tokens.SubmodelElements}': {{
                        '{Tokens.Type}': 'array', 
                        '{Tokens.AllOf}': []
                    }}
                }}
            }}");

            var targetAllOf = SelectToken<JArray>(
                schema, 
                $"$.{Tokens.Definitions}.{Tokens.Elements}.properties.submodelElements.allOf");
            var submodelElements = submodel.submodelElements.Select(item => item.submodelElement);

            AddDefinitionsForSubmodelElements(schema, targetAllOf, submodelElements);
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
            var context = new Context(schema, targetAllOf, submodelElement);

            foreach (var submodelElementHandler in _submodelElementDefinitionSuppliers)
            {
                var result = submodelElementHandler(context);
                if (!result)
                    break;
            }
        }

        private bool SupplyGateArbitrary(Context context)
        {
            if (context.SubmodelElement.idShort == "{arbitrary}")
                return false;

            return true;
        }

        private bool SupplyIdShort(Context context)
        {
            var submodelElement = context.SubmodelElement;
            var definitionProperties = context.SubmodelElementDefinitionProperties;

            if (context.IsDynamicIdShort)
            {
                var idShortPattern = $@"{context.SubmodelElementName}\\d{{2}}";
                definitionProperties[Tokens.IdShort] = JObject.Parse($@"
                {{
                    '{Tokens.Pattern}': '^{idShortPattern}$'
                }}");
            }
            else
            {
                definitionProperties[Tokens.IdShort] = JObject.Parse($@"
                {{
                    '{Tokens.Const}': '{submodelElement.idShort}'
                }}");
            }

            return true;
        }

        private bool SupplyKind(Context context)
        {
            var definitionProperties = context.SubmodelElementDefinitionProperties;
            definitionProperties[Tokens.Kind] = JObject.Parse($@"
            {{
                '{Tokens.Const}': 'Instance'
            }}");

            return true;
        }

        private bool SupplyModelType(Context context)
        {
            var submodelElement = context.SubmodelElement;
            var definitionProperties = context.SubmodelElementDefinitionProperties;

            var modelType = submodelElement.JsonModelType.name;
            definitionProperties[Tokens.ModelType] = JObject.Parse($@"
            {{
                '{Tokens.Properties}': {{
                    '{Tokens.Name}': {{
                        '{Tokens.Const}': '{modelType}'
                    }}
                }}
            }}");

            return true;
        }

        private bool SupplySemanticId(Context context)
        {
            var submodelElement = context.SubmodelElement;
            var definitionProperties = context.SubmodelElementDefinitionProperties;

            if (!submodelElement.semanticId.IsEmpty)
            {
                definitionProperties[Tokens.SemanticId] = JObject.Parse($@"
                {{
                    '{Tokens.Properties}': {{
                        '{Tokens.Keys}': {{
                            '{Tokens.Type}': 'array', 
                            '{Tokens.AllOf}': []
                        }}
                     }}
                }}");

                var allOf = SelectToken<JArray>(
                    definitionProperties, 
                    $"{Tokens.SemanticId}.{Tokens.Properties}.{Tokens.Keys}.{Tokens.AllOf}");

                submodelElement.semanticId.Keys.ForEach(key =>
                {
                    var containsDef = JObject.Parse($@"
                    {{
                        '{Tokens.Contains}': {{
                            '{Tokens.Properties}': {{
                                '{Tokens.MType}': {{
                                    '{Tokens.Const}': '{key.type}'
                                 }},
                                '{Tokens.Local}': {{
                                    '{Tokens.Const}': {key.local.ToString().ToLower()}
                                 }},
                                '{Tokens.Value}': {{
                                    '{Tokens.Const}': '{key.value}'
                                 }},
                                '{Tokens.IdType}': {{
                                    '{Tokens.Const}': '{key.idType}'
                                }},
                            }}
                        }}
                    }}");

                    allOf.Add(containsDef);
                });
            }

            return true;
        }

        private bool SupplyValueType(Context context)
        {
            var submodelElement = context.SubmodelElement;
            var definitionProperties = context.SubmodelElementDefinitionProperties;

            if (submodelElement is AdminShellV20.Property property)
            {
                var valueType = property.JsonValueType.dataObjectType.name;
                if (!string.IsNullOrEmpty(valueType))
                    definitionProperties[Tokens.ValueType] = JObject.Parse($@"
                    {{
                        '{Tokens.Properties}': {{
                            '{Tokens.DataObjectType}': {{
                                '{Tokens.Properties}': {{
                                    '{Tokens.Name}': {{
                                        '{Tokens.Const}': '{valueType}'
                                    }}
                                }}
                            }}
                        }}
                    }}");
            }

            return true;
        }

        private bool SupplyMultiplicity(Context context)
        {
            var submodelElement = context.SubmodelElement;
            var containsReference = context.ContainsReference;

            var multiplicityQualifier = submodelElement.qualifiers.FindType("Multiplicity");
            if (multiplicityQualifier != null)
            {
                switch (multiplicityQualifier.value)
                {
                    case "ZeroToOne":
                        containsReference[Tokens.MinContains] = 0;
                        containsReference[Tokens.MaxContains] = 1;
                        break;
                    case "One":
                        containsReference[Tokens.MinContains] = 1;
                        containsReference[Tokens.MaxContains] = 1;
                        break;
                    case "ZeroToMany":
                        containsReference[Tokens.MinContains] = 0;
                        break;
                    case "OneToMany":
                        containsReference[Tokens.MinContains] = 1;
                        break;
                }
            }

            return true;
        }

        private bool SupplyCollectionSubmodelElements(Context context)
        {
            var schema = context.Schema;
            var submodelElement = context.SubmodelElement;
            var definitionProperties = context.SubmodelElementDefinitionProperties;

            if (submodelElement is AdminShellV20.SubmodelElementCollection submodelElementCollection)
            {
                var submodelElements = submodelElementCollection.value
                    .Select(item => item.submodelElement)
                    .ToArray();


                definitionProperties[Tokens.Value] = JToken.Parse($@"
                {{
                    '{Tokens.Type}': 'array', 
                    '{Tokens.AllOf}': []
                }}");

                var targetAllOf = SelectToken<JArray>(definitionProperties, $"{Tokens.Value}.{Tokens.AllOf}");

                AddDefinitionsForSubmodelElements(schema, targetAllOf, submodelElements);
            }

            return true;
        }

        private bool SupplyCreatedDefinition(Context context)
        {
            var schemaDefinitions = context.SchemaDefinitions;
            var targetAllOf = context.TargetAllOf;
            var containsReference = context.ContainsReference;
            var submodelElementName = context.SubmodelElementName;

            schemaDefinitions[submodelElementName] = context.SubmodelElementDefinition;
            targetAllOf.Add(containsReference);

            return true;
        }


        private void AddDefinitionReferenceToRootAllOf(JObject schema, string token)
        {
            var reference = $"#/{Tokens.Definitions}/{token}";
            AddReferenceToArray(schema, GetRootAllOf, reference);
        }

        private T SelectToken<T>(JToken source, string path) where T: JToken
        {
            var result = source.SelectToken(path) as T;
            if (result == null)
                throw new Exception($"Token was not found. {path}");

            return result;
        }

        private void AddReferenceToArray(JObject schema, Func<JObject,JArray> targetArrayProvider, string reference)
        {
            var targetArray = targetArrayProvider(schema);
            targetArray.Add(JObject.Parse($@"{{'$ref': '{reference}' }}"));
        }

        private JArray GetRootAllOf(JToken schema)
        {
            var result = SelectToken<JArray>(schema, $"{Tokens.AllOf}");
            return result;
        }
    }
}
