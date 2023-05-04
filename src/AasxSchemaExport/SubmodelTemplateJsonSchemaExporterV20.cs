/*
Copyright (c) 2022 PHOENIX CONTACT GmbH & Co. KG <info@phoenixcontact.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Aas = AasCore.Aas3_0;

namespace AasxSchemaExport
{
    public class SubmodelTemplateJsonSchemaExporterV20 : ISchemaExporter
    {
        private List<Func<SubmodelElementDefinitionContext, bool>> _submodelElementDefinitionSuppliers;

        public string ExportSchema(Aas.ISubmodel submodel)
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
            _submodelElementDefinitionSuppliers = new List<Func<SubmodelElementDefinitionContext, bool>>
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

        private void AddRootData(JObject schema, Aas.ISubmodel submodel)
        {
            schema[Tokens.Schema] = Constants.JsonSchemaDraftVersion;
            schema[Tokens.Title] = $"AssetAdministrationShell{submodel.IdShort}";
            schema[Tokens.Type] = "object";
            schema[Tokens.AllOf] = new JArray();
            schema[Tokens.Definitions] = new JObject();
        }

        private void AddSubmodelReference(JObject schema)
        {
            var reference = $"{Constants.MetaModelSchemaUrl}{Constants.MetaModelSubmodelDefinitionPath}";
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

        private void AddDefinitionForSubmodelElements(JObject schema, Aas.ISubmodel submodel)
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
                $"$.{Tokens.Definitions}.{Tokens.Elements}.{Tokens.Properties}.{Tokens.SubmodelElements}.{Tokens.AllOf}");
            var submodelElements = submodel.SubmodelElements.Select(item => item);

            AddDefinitionsForSubmodelElements(schema, targetAllOf, submodelElements);
        }

        private void AddDefinitionsForSubmodelElements(
            JObject schema,
            JArray targetAllOf,
            IEnumerable<Aas.ISubmodelElement> submodelElements)
        {
            foreach (var submodelElement in submodelElements)
            {
                AddDefinitionForSubmodelElement(schema, targetAllOf, submodelElement);
            }
        }

        private void AddDefinitionForSubmodelElement(JObject schema, JArray targetAllOf, Aas.ISubmodelElement submodelElement)
        {
            var context = new SubmodelElementDefinitionContext(schema, targetAllOf, submodelElement);

            foreach (var submodelElementHandler in _submodelElementDefinitionSuppliers)
            {
                var result = submodelElementHandler(context);
                if (!result)
                    break;
            }
        }

        private bool SupplyGateArbitrary(SubmodelElementDefinitionContext context)
        {
            if (context.SubmodelElement.IdShort == "{arbitrary}")
                return false;

            return true;
        }

        private bool SupplyIdShort(SubmodelElementDefinitionContext context)
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
                    '{Tokens.Const}': '{submodelElement.IdShort}'
                }}");
            }

            return true;
        }

        private bool SupplyKind(SubmodelElementDefinitionContext context)
        {
            var definitionProperties = context.SubmodelElementDefinitionProperties;
            definitionProperties[Tokens.Kind] = JObject.Parse($@"
            {{
                '{Tokens.Const}': 'Instance'
            }}");

            return true;
        }

        private bool SupplyModelType(SubmodelElementDefinitionContext context)
        {
            var submodelElement = context.SubmodelElement;
            var definitionProperties = context.SubmodelElementDefinitionProperties;

            var modelType = submodelElement.GetSelfDescription()?.AasElementName;
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

        private bool SupplySemanticId(SubmodelElementDefinitionContext context)
        {
            var submodelElement = context.SubmodelElement;
            var definitionProperties = context.SubmodelElementDefinitionProperties;

            if (!submodelElement.SemanticId.IsEmpty())
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

                submodelElement.SemanticId.Keys.ForEach(key =>
                {
                    var containsDef = JObject.Parse($@"
                    {{
                        '{Tokens.Contains}': {{
                            '{Tokens.Properties}': {{
                                '{Tokens.MType}': {{
                                    '{Tokens.Const}': '{key.Type}'
                                 }},
                                '{Tokens.Value}': {{
                                    '{Tokens.Const}': '{key.Value}'
                                 }},
                            }}
                        }}
                    }}");

                    allOf.Add(containsDef);
                });
            }

            return true;
        }

        private bool SupplyValueType(SubmodelElementDefinitionContext context)
        {
            var submodelElement = context.SubmodelElement;
            var definitionProperties = context.SubmodelElementDefinitionProperties;

            if (submodelElement is Aas.Property property)
            {
                var valueType = Aas.Stringification.ToString(property.ValueType);
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

        private bool SupplyMultiplicity(SubmodelElementDefinitionContext context)
        {
            var submodelElement = context.SubmodelElement;
            var containsReference = context.ContainsReference;

            var multiplicityQualifier = submodelElement.Qualifiers.FindType("Multiplicity");
            if (multiplicityQualifier != null)
            {
                switch (multiplicityQualifier.Value)
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

        private bool SupplyCollectionSubmodelElements(SubmodelElementDefinitionContext context)
        {
            var schema = context.Schema;
            var submodelElement = context.SubmodelElement;
            var definitionProperties = context.SubmodelElementDefinitionProperties;

            if (submodelElement is Aas.SubmodelElementCollection submodelElementCollection)
            {
                var submodelElements = submodelElementCollection.Value
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

        private bool SupplyCreatedDefinition(SubmodelElementDefinitionContext context)
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

        private T SelectToken<T>(JToken source, string path) where T : JToken
        {
            var result = source.SelectToken(path) as T;
            if (result == null)
                throw new Exception($"Token was not found. {path}");

            return result;
        }

        private void AddReferenceToArray(JObject schema, Func<JObject, JArray> targetArrayProvider, string reference)
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
