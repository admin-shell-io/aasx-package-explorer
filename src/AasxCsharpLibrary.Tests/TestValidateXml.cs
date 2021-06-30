using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Environment = System.Environment;
using InvalidOperationException = System.InvalidOperationException;
using Path = System.IO.Path;

namespace AdminShellNS.Tests
{
    public class TestOnFiles
    {
        [Test]
        public void TestSuccess()
        {
            string successDir = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "TestResources\\AasxCsharpLibrary.Tests\\XmlValidation\\expectedOk");

            if (!System.IO.Directory.Exists(successDir))
            {
                throw new InvalidOperationException(
                    $"The directory containing the valid AAS XML files does not exist or is not a directory: " +
                    successDir);
            }

            var paths = System.IO.Directory.GetFiles(successDir)
                .Where(p => System.IO.Path.GetExtension(p) == ".xml")
                .ToList();

            if (paths.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No *.xml files were found in the directory expected to contain the valid XML files: " +
                    successDir);
            }

            var validator = AasSchemaValidation.NewXmlValidator();

            foreach (string path in paths)
            {
                using var fileStream = System.IO.File.OpenRead(path);
                var records = new AasValidationRecordList();
                validator.Validate(records, fileStream);
                if (records.Count != 0)
                {
                    var parts = new List<string>
                    {
                        $"Failed to validate XML file {path}:"
                    };
                    parts.AddRange(records.Select((r) => r.Message));
                    throw new AssertionException(string.Join(Environment.NewLine, parts));
                }
            }
        }
    }
}
