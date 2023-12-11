/*
Copyright (c) 2022 PHOENIX CONTACT GmbH & Co. KG <info@phoenixcontact.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.IO;
using System.Linq;
using AdminShellNS;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AasxSchemaExport.Tests
{
    [TestFixture]
    public class TestJsonSchemaExport
    {
        private AdminShellV20.Submodel _submodel;

        [OneTimeSetUp]
        public void Init()
        {
            var submodelTemplatePath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "TestData",
                "SubmodelTest.aasx");

            var packageEnv = new AdminShellPackageEnv(submodelTemplatePath);
            _submodel = packageEnv.AasEnv.Submodels[0];
        }

        [Test]
        public void Test_meta_information()
        {
            var schema = ExportSchema();

            Assert.AreEqual(
                schema.GetValue<string>("$schema"),
                "https://json-schema.org/draft/2019-09/schema",
                "Exported schema must support the draft 2019-09 draft.");

            Assert.AreEqual(
                schema.GetValue<string>("title"),
                "AssetAdministrationShellSubmodelTest",
                "The title of the schema must contain the prefix AssetAdministrationShell plus the idShort of the submodel.");
        }

        [Test]
        public void Test_root_type_should_be_object()
        {
            var schema = ExportSchema();

            Assert.AreEqual(
                schema.GetValue<string>("type"),
                "object",
                "The type of the root element must be object.");
        }

        [Test]
        public void Test_submodel_reference()
        {
            var schema = ExportSchema();

            var definitionRef = FindObjectInArrayWithProperty(
                schema.SelectToken($"{Tokens.AllOf}"),
                $"{Tokens.Ref}",
                $"{Constants.MetaModelSchemaUrl}{Constants.MetaModelSubmodelDefinitionPath}");

            Assert.NotNull(
                definitionRef,
                "There must be a reference to the submodel definition.");
        }

        [Test]
        public void Test_identifiable_properties()
        {
            var schema = ExportSchema();

            var definitionRef = FindObjectInArrayWithProperty(
                schema.SelectToken($"{Tokens.AllOf}"),
                $"{Tokens.Ref}",
                $"#/{Tokens.Definitions}/{Tokens.Identifiable}");

            var definition = GetDefinition(schema, Tokens.Identifiable);

            Assert.NotNull(definitionRef, "Must contain the reference to identifiable.");
            Assert.AreEqual(definition.GetValue<string>("" +
                                                        $"{Tokens.Properties}." +
                                                        $"{Tokens.ModelType}." +
                                                        $"{Tokens.Properties}." +
                                                        $"{Tokens.Name}." +
                                                        $"{Tokens.Const}")
              , "Submodel");
        }

        [Test]
        public void Test_multiplicity_zero_to_one()
        {
            var schema = ExportSchema();

            var prop1Reference = GetSubmodelElementsAllOfItem(schema, "Prop1");

            Assert.AreEqual(prop1Reference.GetValue<int>(Tokens.MinContains), 0);
            Assert.AreEqual(prop1Reference.GetValue<int>(Tokens.MaxContains), 1);
        }

        [Test]
        public void Test_multiplicity_one()
        {
            var schema = ExportSchema();

            var prop1Reference = GetSubmodelElementsAllOfItem(schema, "Prop2");

            Assert.AreEqual(prop1Reference.GetValue<int>(Tokens.MinContains), 1);
            Assert.AreEqual(prop1Reference.GetValue<int>(Tokens.MaxContains), 1);
        }

        [Test]
        public void Test_multiplicity_zero_to_many()
        {
            var schema = ExportSchema();

            var prop1Reference = GetSubmodelElementsAllOfItem(schema, "Prop3");

            Assert.AreEqual(prop1Reference.GetValue<int>(Tokens.MinContains), 0);
            Assert.IsNull(prop1Reference[Tokens.MaxContains]);
        }

        [Test]
        public void Test_multiplicity_one_to_many()
        {
            var schema = ExportSchema();

            var prop1Reference = GetSubmodelElementsAllOfItem(schema, "Prop4");

            Assert.AreEqual(prop1Reference.GetValue<int>(Tokens.MinContains), 1);
            Assert.IsNull(prop1Reference[Tokens.MaxContains]);
        }

        [Test]
        public void Test_submodel_element_definition_idShort()
        {
            var schema = ExportSchema();

            var definition = GetDefinition(schema, "Prop1");
            var path = $"{Tokens.Properties}.{Tokens.IdShort}.{Tokens.Const}";

            Assert.AreEqual(definition.GetValue<string>(path), "Prop1");
        }

        [Test]
        public void Test_submodel_element_definition_kind()
        {
            var schema = ExportSchema();

            var definition = GetDefinition(schema, "Prop1");
            var path = $"{Tokens.Properties}.{Tokens.Kind}.{Tokens.Const}";

            Assert.AreEqual(definition.GetValue<string>(path), "Instance");
        }

        [Test]
        public void Test_submodel_element_definition_modelType()
        {
            var schema = ExportSchema();

            var definition = GetDefinition(schema, "Prop1");
            var path = $"{Tokens.Properties}.{Tokens.ModelType}.{Tokens.Properties}.{Tokens.Name}.{Tokens.Const}";

            Assert.AreEqual(definition.GetValue<string>(path), "Property");
        }

        [Test]
        public void Test_submodel_element_definition_valueType()
        {
            var schema = ExportSchema();

            var definition = GetDefinition(schema, "Prop6");
            var path = $"" +
                       $"{Tokens.Properties}." +
                       $"{Tokens.ValueType}." +
                       $"{Tokens.Properties}." +
                       $"{Tokens.DataObjectType}." +
                       $"{Tokens.Properties}." +
                       $"{Tokens.Name}." +
                       $"{Tokens.Const}";

            Assert.AreEqual(definition.GetValue<string>(path), "short");
        }


        [Test]
        public void Test_submodel_element_definition_semanticId()
        {
            var schema = ExportSchema();

            var definition = GetDefinition(schema, "Prop5");
            var semanticIdAllOf = definition.SelectToken($"" +
                                                         $"{Tokens.Properties}." +
                                                         $"{Tokens.SemanticId}." +
                                                         $"{Tokens.Properties}." +
                                                         $"{Tokens.Keys}." +
                                                         $"{Tokens.AllOf}") as JArray;

            if (semanticIdAllOf == null || semanticIdAllOf.Count == 0)
            {
                Assert.Fail("SemanticId(s) were not found.");
            }

            var keyItem = semanticIdAllOf[0][Tokens.Contains];

            Assert.AreEqual(keyItem.GetValue<string>($"{Tokens.Properties}.{Tokens.Type}.{Tokens.Const}"), "ConceptDescription");
            Assert.AreEqual(keyItem.GetValue<bool>($"{Tokens.Properties}.{Tokens.Local}.{Tokens.Const}"), true);
            Assert.AreEqual(keyItem.GetValue<string>($"{Tokens.Properties}.{Tokens.Value}.{Tokens.Const}"), "https://www.example.com/1");
            Assert.AreEqual(keyItem.GetValue<string>($"{Tokens.Properties}.{Tokens.IdType}.{Tokens.Const}"), "IRI");
        }

        private JObject GetDefinition(JObject schema, string name)
        {
            var definition = schema.SelectToken($"{Tokens.Definitions}.{name}");
            return definition as JObject;
        }


        private JObject GetSubmodelElementsAllOfItem(JObject schema, string definitionName)
        {
            var allOf = schema.SelectToken($"" +
                                           $"{Tokens.Definitions}." +
                                           $"{Tokens.Elements}." +
                                           $"{Tokens.Properties}." +
                                           $"{Tokens.SubmodelElements}." +
                                           $"{Tokens.AllOf}") as JArray;

            var result = allOf.FirstOrDefault(item =>
                item.GetValue<string>($"{Tokens.Contains}.{Tokens.Ref}") == $"#/{Tokens.Definitions}/{definitionName}"
            ) as JObject;

            return result;
        }

        private object FindObjectInArrayWithProperty(JToken jArray, string propertyName, string propertyValue)
        {
            var result = jArray.FirstOrDefault(item =>
                item[propertyName] != null &&
                item[propertyName].Value<string>() == propertyValue);

            return result;
        }

        private JObject ExportSchema()
        {
            var exporter = new SubmodelTemplateJsonSchemaExporterV20();
            var schema = exporter.ExportSchema(_submodel);
            return JObject.Parse(schema);
        }
    }
}
