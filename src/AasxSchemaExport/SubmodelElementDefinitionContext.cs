/*
Copyright (c) 2022 PHOENIX CONTACT GmbH & Co. KG <info@phoenixcontact.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Newtonsoft.Json.Linq;
using Aas = AasCore.Aas3_0;

namespace AasxSchemaExport
{
    internal class SubmodelElementDefinitionContext
    {
        private const string DynamicIdShortIndicator = "{00}";

        public JObject Schema { get; }
        public JArray TargetAllOf { get; }
        public Aas.ISubmodelElement SubmodelElement { get; }

        public string SubmodelElementName { get; }

        public bool IsDynamicIdShort => SubmodelElement.IdShort.EndsWith(DynamicIdShortIndicator);

        public JObject ContainsReference { get; }
        public JObject SubmodelElementDefinition { get; }

        public JObject SubmodelElementDefinitionProperties => SubmodelElementDefinition[Tokens.Properties] as JObject;

        public JObject SchemaDefinitions => Schema[Tokens.Definitions] as JObject;

        public SubmodelElementDefinitionContext(JObject schema, JArray targetAllOf, Aas.ISubmodelElement submodelElement)
        {
            Schema = schema;
            TargetAllOf = targetAllOf;
            SubmodelElement = submodelElement;

            SubmodelElementName = SubmodelElement.IdShort.Replace(DynamicIdShortIndicator, "");
            ContainsReference = JObject.Parse($@"{{'{Tokens.Contains}': {{'{Tokens.Ref}': '#/{Tokens.Definitions}/{SubmodelElementName}'}}}}");
            SubmodelElementDefinition = JObject.Parse($@"{{'{Tokens.Properties}': {{}}}}");
        }
    }
}