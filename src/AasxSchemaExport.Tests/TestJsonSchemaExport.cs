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

        [SetUp]
        public void Init()
        {
            var submodelTemplatePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData",
                "IDTA 02006-2-0_Template_Digital Nameplate.aasx");

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

        private JObject ExportSchema()
        {
            var exporter = new SubmodelTemplateJsonSchemaExporterV20();
            var schema = exporter.ExportSchema(_submodel);
            return JObject.Parse(schema);
        }
    }
}
