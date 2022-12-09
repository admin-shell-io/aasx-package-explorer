using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                "IDTA 02006-2-0_Template_Digital Nameplate.aasx");

            var packageEnv = new AdminShellPackageEnv(submodelTemplatePath);
            _submodel = packageEnv.AasEnv.Submodels[0];
        }

        [Test]
        public void Test_meta_information()
        {
            var schema = ExportSchema();

            Assert.AreEqual(schema["$schema"].Value<string>(), "https://json-schema.org/draft/2019-09/schema");
            Assert.AreEqual(schema["title"].Value<string>(), "AssetAdministrationShellSubmodelTest");
        }


        [Test]
        public void Test_type_should_be_object()
        {
            var schema = ExportSchema();

            var type = schema["type"];

            Assert.IsNotNull(type);
            Assert.AreEqual(type.Value<string>(), "object");
        }

        [Test]
        public void Test_reference_to_root_schema_definition()
        {
            var schema = ExportSchema();

            var definitionRef = FindObjectInArrayWithProperty(
                schema["allOf"] as JArray,
                "$ref",
                "#/$defs/Root");

            Assert.NotNull(definitionRef);
        }

        [Test]
        public void Test_unevaluated_properties_should_be_false()
        {
            var schema = ExportSchema();

            var unevaluatedProperties = schema["unevaluatedProperties"];

            Assert.AreEqual(unevaluatedProperties.Value<bool>(), false);
        }

        [Test]
        public void Test_rootDef_should_reference_submodelDef()
        {
            var schema = ExportSchema();

            var definitionRef = FindObjectInArrayWithProperty(
                schema["$defs"]["Root"]["allOf"] as JArray,
                "$ref",
                "aas.json#/$defs/Submodel");

            Assert.NotNull(definitionRef);
        }

        [Test]
        public void Test_identifiable_properties()
        {
            var schema = ExportSchema();

            var definitionRef = FindObjectInArrayWithProperty(
                schema["$defs"]["Root"]["allOf"] as JArray,
                "$ref",
                "#/$defs/Identifiable");

            Assert.NotNull(definitionRef);
            Assert.AreEqual(schema
                ["$defs"]
                ["Identifiable"]
                ["properties"]
                ["modelType"]
                ["name"]
                ["const"].Value<string>(), "Submodel");
        }

        [Test]
        public void Test_level0_prop1()
        {
            var schema = ExportSchema();

            var definition = GetDefinition(schema,"Prop1");
            var properties = GetPropertiesOfContains(definition);

            Assert.AreEqual(properties["idShort"]?["const"].Value<string>(), "Prop1");
            Assert.AreEqual(properties["kind"]?["const"].Value<string>(), "Instance");
            Assert.AreEqual(properties["modelType"]?["properties"]?["name"]?["const"].Value<string>(), "Property");
            Assert.AreEqual(properties["valueType"]?["properties"]?["dataObjectType"]?["properties"]?["name"]?["const"].Value<string>(), "String");
        }

        private JObject GetDefinition(JObject schema, string name)
        {
            var definition = schema["definitions"][name];
            return definition as JObject;
        }

        private JObject GetPropertiesOfContains(JObject jObject)
        {
            var properties = jObject["contains"]["properties"];
            return properties as JObject;
        }


        private object FindObjectInArrayWithProperty(JArray jArray, string propertyName, string propertyValue)
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
