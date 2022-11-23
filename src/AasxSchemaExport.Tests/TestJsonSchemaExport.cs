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
            var schema = this.ExportSchema();

            var schemaVersion = schema["$schema"];

            Assert.IsNotNull(schemaVersion);
            Assert.AreEqual(schemaVersion.Value<string>(), "https://json-schema.org/draft/2019-09/schema");
        }

        [Test]
        public void Test_title_corresponds_to_idShort()
        {
            var schema = this.ExportSchema();

            var schemaVersion = schema["title"];

            Assert.IsNotNull(schemaVersion);
            Assert.AreEqual(schemaVersion.Value<string>(), "AssetAdministrationShellSubmodelSubmodelTest");
        }

        [Test]
        public void Test_type_should_be_object()
        {
            var schema = this.ExportSchema();

            var schemaVersion = schema["type"];

            Assert.IsNotNull(schemaVersion);
            Assert.AreEqual(schemaVersion.Value<string>(), "object");
        }

        [Test]
        public void Test_reference_to_root_schema_definition()
        {
            var schema = this.ExportSchema();

            var refDefinition = schema["$ref"];

            Assert.AreEqual(refDefinition.Value<string>(), "#/definitions/Root");
        }


        private JObject ExportSchema()
        {
            var exporter = new SubmodelTemplateJsonSchemaExporterV20();
            var schema = exporter.ExportSchema(_submodel);
            return JObject.Parse(schema);
        }
    }
}
