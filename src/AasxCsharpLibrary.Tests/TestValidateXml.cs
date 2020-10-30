using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Environment = System.Environment;
using InvalidOperationException = System.InvalidOperationException;
using Path = System.IO.Path;

namespace AdminShellNS.Tests
{
    public class TestOnFiles
    {
        private List<string> mustListXmlFiles(string xmlDir)
        {
            if (!System.IO.Directory.Exists(xmlDir))
            {
                throw new InvalidOperationException(
                    $"The directory containing the test XML files does not exist or is not a directory: " +
                    xmlDir);
            }

            var paths = System.IO.Directory.GetFiles(xmlDir)
                .Where(p => System.IO.Path.GetExtension(p) == ".xml")
                .ToList();

            if (paths.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No *.xml files were found in the directory expected to contain the test XML files: " +
                    xmlDir);
            }

            return paths;
        }

        [Test]
        public void TestSuccess()
        {
            var paths = mustListXmlFiles(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "TestResources\\XmlValidation\\expectedOk"));

            var xmlValidator = AasSchemaValidation.NewXmlValidator();

            foreach (string path in paths)
            {
                using (var fileStream = System.IO.File.OpenRead(path))
                {
                    var records = new AasValidationRecordList();
                    xmlValidator.Validate(records, fileStream);
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

        [Test]
        public void TestInvalidAccordingToSchema()
        {
            var paths = mustListXmlFiles(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "TestResources\\XmlValidation\\expectedInvalidAccordingToSchema"));

            var xmlValidator = AasSchemaValidation.NewXmlValidator();

            foreach (string path in paths)
            {
                using (var fileStream = System.IO.File.OpenRead(path))
                {
                    var records = new AasValidationRecordList();
                    xmlValidator.Validate(records, fileStream);

                    string errPath = Path.Combine(
                        Path.GetDirectoryName(path),
                        $"{Path.GetFileNameWithoutExtension(path)}.err");

                    string expectedErr = null;
                    if (File.Exists(errPath))
                    {
                        expectedErr = string.Join(Environment.NewLine, File.ReadAllLines(errPath));
                    }

                    if (records.Count == 0)
                    {
                        if (expectedErr == null)
                        {
                            throw new AssertionException(
                                "Expected the test XML file to be invalid, " +
                                $"but there were no validation records: {path}");
                        }
                        else
                        {
                            throw new AssertionException(
                                "Expected the test XML file to be invalid, " +
                                $"but there were no validation records: {path}; the expected errors were:" +
                                $"{Environment.NewLine}{expectedErr}");
                        }
                    }

                    var gotErr =
                        string.Join(Environment.NewLine,
                            records
                                .Select((r) =>
                                {
                                    string message = r.ToString();
                                    if (message.Contains("\n") || message.Contains("\r"))
                                    {
                                        throw new InvalidOperationException(
                                            "Unexpected newline character in the validation record " +
                                            $"caused by the invalid test XML file {path}: {message}");
                                    }

                                    return r.ToString();
                                })
                        );

                    if (expectedErr == null)
                    {
                        var nl = Environment.NewLine;
                        throw new AssertionException(
                            $"The expected error for the test XML file {path} does not exist: {errPath}.{nl}" +
                            "Did you create it in the test resources? " +
                            $"The XML validation gave us the following error message:{nl}{gotErr}{nl}{nl}" +
                            "Please verify the obtained error message and consider creating the corresponding " +
                            "test resource.");
                    }
                    else if (gotErr != expectedErr)
                    {
                        var nl = Environment.NewLine;
                        throw new AssertionException(
                            $"The validation records caused by the test XML file {path} " +
                            $"do not match the expected ones. Got:{nl}{gotErr}{nl}{nl}Expected:{nl}{expectedErr}");
                    }
                    else
                    {
                        // The errors match; everything is OK.
                    }
                }
            }
        }
    }
}