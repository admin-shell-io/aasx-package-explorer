using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Environment = System.Environment;
using InvalidOperationException = System.InvalidOperationException;
using Path = System.IO.Path;
using Xml = System.Xml;

namespace AdminShellNS.Tests
{
    static class SamplesAasxDir
    {
        public static List<string> ListAasxPaths()
        {
            var variable = "SAMPLE_AASX_DIR";

            var sampleAasxDir = System.Environment.GetEnvironmentVariable(variable);
            if (sampleAasxDir == null)
            {
                throw new InvalidOperationException(
                    $"The environment variable {variable} has not been set. " +
                    "Did you set it manually to the directory containing sample AASXs? " +
                    "Otherwise, run the test through Test.ps1?");
            }

            if (!System.IO.Directory.Exists(sampleAasxDir))
            {
                throw new InvalidOperationException(
                    $"The directory containing the sample AASXs does not exist or is not a directory: " +
                    $"{sampleAasxDir}; did you download the samples with DownloadSamples.ps1?");
            }

            var result = System.IO.Directory.GetFiles(sampleAasxDir)
                .Where(p => System.IO.Path.GetExtension(p) == ".aasx")
                .ToList();

            result.Sort();

            return result;
        }
    }

    public class TestLoadSaveChain
    {
        private static void AssertFilesEqual(string firstPath, string secondPath, string aasxPath)
        {
            string firstContent = System.IO.File.ReadAllText(firstPath);
            string secondContent = System.IO.File.ReadAllText(secondPath);

            string[] firstLines = firstContent.Split(
                new[] { "\r\n", "\r", "\n" },
                System.StringSplitOptions.None
            );

            string[] secondLines = secondContent.Split(
                new[] { "\r\n", "\r", "\n" },
                System.StringSplitOptions.None
            );

            int min = (firstLines.Length < secondLines.Length)
                ? firstLines.Length
                : secondLines.Length;

            for (var i = 0; i < min; i++)
            {
                if (firstLines[i] != secondLines[i])
                {
                    int start = (i < 20) ? 0 : i - 20;
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("The first and the second export in the chain differ:");

                    for (var j = start; j < i; j++)
                    {
                        sb.AppendLine($"[{i,6}:SAME IN BOTH]{firstLines[j]}");
                    }

                    sb.AppendLine($"[{i,6}:IN FIRST    ]{firstLines[i]}");
                    sb.AppendLine($"[{i,6}:IN SECOND   ]{secondLines[i]}");

                    sb.AppendLine($"The AASX sample used was: {aasxPath}");
                    throw new AssertionException(sb.ToString());
                }
            }
        }

        [TestCase(".xml")]
        public void TestLoadSaveLoadAssertEqual(string extension)
        {
            List<string> aasxPaths = SamplesAasxDir.ListAasxPaths();

            using (var tmpDir = new TemporaryDirectory())
            {
                foreach (string aasxPath in aasxPaths)
                {
                    /*
                     * The chain is as follows:
                     * - First load from AASX (package A)
                     * - Convert package 1 to `extension` format and save as path 1
                     * - Load from the path 1 in `extension` format (package B) 
                     * - Save package B in `extension` format to path 2
                     *
                     * We expect the content of the two files (path 1 and path 2, respectively) to be equal.
                     */
                    using (var packageA = new AdminShellPackageEnv(aasxPath))
                    {
                        string path1 = System.IO.Path.Combine(tmpDir.Path, $"first{extension}");
                        string path2 = System.IO.Path.Combine(tmpDir.Path, $"second{extension}");

                        packageA.SaveAs(path1, writeFreshly: true);

                        using (var packageB = new AdminShellPackageEnv(path1))
                        {
                            packageB.SaveAs(path2, writeFreshly: true);
                            AssertFilesEqual(path1, path2, aasxPath);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestLoadSaveXmlValidate()
        {
            // Load the schema

            var xmlSchemaSet = new Xml.Schema.XmlSchemaSet();
            xmlSchemaSet.XmlResolver = new Xml.XmlUrlResolver();

            string schemaPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "Resources\\schemas\\xml\\AAS.xsd");

            xmlSchemaSet.Add(null, schemaPath);

            var schemaMessages = new List<string>();
            xmlSchemaSet.ValidationEventHandler +=
                (object sender, Xml.Schema.ValidationEventArgs e) => { schemaMessages.Add(e.Message); };
            xmlSchemaSet.Compile();

            if (schemaMessages.Count > 0)
            {
                var parts = new List<string> { $"Failed to compile the schema: {schemaPath}" };
                parts.AddRange(schemaMessages);

                throw new InvalidOperationException(string.Join(Environment.NewLine, parts));
            }

            // Load-Save-Validate

            List<string> aasxPaths = SamplesAasxDir.ListAasxPaths();

            using (var tmpDir = new TemporaryDirectory())
            {
                string tmpDirPath = tmpDir.Path;

                foreach (string aasxPath in aasxPaths)
                {
                    using (var package = new AdminShellPackageEnv(aasxPath))
                    {
                        string name = Path.GetFileName(aasxPath);
                        string outPath = System.IO.Path.Combine(tmpDirPath, $"{name}.converted.xml");

                        package.SaveAs(outPath, writeFreshly: true);

                        var settings = new Xml.XmlReaderSettings();
                        settings.ValidationType = Xml.ValidationType.Schema;
                        settings.Schemas = xmlSchemaSet;

                        var messages = new List<string>();
                        settings.ValidationEventHandler +=
                            (object sender, Xml.Schema.ValidationEventArgs e) =>
                            {
                                messages.Add(e.Message);
                            };

                        using (var reader = Xml.XmlReader.Create(outPath, settings))
                        {
                            while (reader.Read())
                            {
                                // Invoke callbacks
                            };

                            if (messages.Count > 0)
                            {
                                var parts = new List<string>
                                {
                                    $"Failed to validate XML file exported from {aasxPath} to {outPath}:"
                                };
                                parts.AddRange(messages);

                                throw new AssertionException(string.Join(Environment.NewLine, parts));
                            }
                        }
                    }
                }
            }
        }
    }
}
