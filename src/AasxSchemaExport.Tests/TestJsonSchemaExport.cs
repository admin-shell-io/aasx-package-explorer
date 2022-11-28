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
                "SubmodelTest.aasx");

            var packageEnv = new AdminShellPackageEnv(submodelTemplatePath);
            _submodel = packageEnv.AasEnv.Submodels[0];
        }

        [Test]
        public void Test_schema_version()
        {
            var schema = ExportSchema();

            var schemaVersion = schema["$schema"];

            Assert.IsNotNull(schemaVersion);
            Assert.AreEqual(schemaVersion.Value<string>(), "https://json-schema.org/draft/2019-09/schema");
        }

        [Test]
        public void Test_title_corresponds_to_idShort()
        {
            var schema = ExportSchema();

            var title = schema["title"];

            Assert.IsNotNull(title);
            Assert.AreEqual(title.Value<string>(), "AssetAdministrationShellSubmodelSubmodelTest");
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

            var refDefinition = schema["$ref"];

            Assert.AreEqual(refDefinition.Value<string>(), "#/definitions/Root");
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
                schema["definitions"]["Root"]["allOf"] as JArray,
                "$ref",
                "aas.json#/definitions/Submodel");

            Assert.NotNull(definitionRef);
        }

        [Test]
        public void Test_identifiable_properties()
        {
            var schema = ExportSchema();

            var definitionRef = FindObjectInArrayWithProperty(
                schema["definitions"]["Root"]["allOf"] as JArray,
                "$ref",
                "#/definitions/Identifiable");

            Assert.NotNull(definitionRef);
            Assert.AreEqual(schema
                ["definitions"]
                ["Identifiable"]
                ["properties"]
                ["modelType"]
                ["name"]
                ["const"].Value<string>(), "Submodel");
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
