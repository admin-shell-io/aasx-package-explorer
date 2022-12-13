/*
Copyright (c) 2022 PHOENIX CONTACT GmbH & Co. KG <info@phoenixcontact.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS;
using Newtonsoft.Json.Linq;

namespace AasxSchemaExport
{
    internal class SubmodelElementDefinitionContext
    {
        private const string DynamicIdShortIndicator = "{00}";

        public JObject Schema { get; }
        public JArray TargetAllOf { get; }
        public AdminShellV20.SubmodelElement SubmodelElement { get; }

        public string SubmodelElementName { get; }

        public bool IsDynamicIdShort => SubmodelElement.idShort.EndsWith(DynamicIdShortIndicator);

        public JObject ContainsReference { get; }
        public JObject SubmodelElementDefinition { get; }

        public JObject SubmodelElementDefinitionProperties => SubmodelElementDefinition[Tokens.Properties] as JObject;

        public JObject SchemaDefinitions => Schema[Tokens.Definitions] as JObject;

        public SubmodelElementDefinitionContext(JObject schema, JArray targetAllOf, AdminShellV20.SubmodelElement submodelElement)
        {
            Schema = schema;
            TargetAllOf = targetAllOf;
            SubmodelElement = submodelElement;

            SubmodelElementName = SubmodelElement.idShort.Replace(DynamicIdShortIndicator, "");
            ContainsReference = JObject.Parse($@"{{'{Tokens.Contains}': {{'{Tokens.Ref}': '#/{Tokens.Definitions}/{SubmodelElementName}'}}}}");
            SubmodelElementDefinition = JObject.Parse($@"{{'{Tokens.Properties}': {{}}}}");
        }
    }
}