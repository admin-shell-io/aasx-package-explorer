using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Environment = System.Environment;
using InvalidOperationException = System.InvalidOperationException;
using Path = System.IO.Path;

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

    /*
    TODO (mristin, 2020-10-05): The class is unused since all its tests were disabled temporarily and
    will be fixed in the near future.

    Once the tests are enabled, please remove this Resharper directive.
    */
    // ReSharper disable once UnusedType.Global
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

        /*
        TODO (mristin, 2020-10-05): This test has been temporary disabled so that we can merge in the branch
        MIHO/EnhanceDocumentShelf. The test should be fixed in a future pull request and we will then re-enable it
        again.

        Please do not forget to remove the Resharper directive at the top of this class.

        [TestCase(".xml")]

        dead-csharp ignore this comment
        */
        public void TestLoadSaveLoadAssertEqual(string extension)
        {
            List<string> aasxPaths = SamplesAasxDir.ListAasxPaths();

            using var tmpDir = new TemporaryDirectory();
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
                using var packageA = new AdminShellPackageEnv(aasxPath);
                string path1 = System.IO.Path.Combine(tmpDir.Path, $"first{extension}");
                string path2 = System.IO.Path.Combine(tmpDir.Path, $"second{extension}");

                packageA.SaveAs(path1, writeFreshly: true);

                using var packageB = new AdminShellPackageEnv(path1);
                packageB.SaveAs(path2, writeFreshly: true);
                AssertFilesEqual(path1, path2, aasxPath);
            }
        }


        /*
        TODO (mristin, 2020-10-05): This test has been temporary disabled so that we can merge in the branch
        MIHO/EnhanceDocumentShelf. The test should be fixed in a future pull request and we will then re-enable it
        again.

        Please do not forget to remove the Resharper directive at the top of this class.

        [Test]

        dead-csharp ignore this comment
        */
        public void TestLoadSaveXmlValidate()
        {
            var validator = AasSchemaValidation.NewXmlValidator();

            List<string> aasxPaths = SamplesAasxDir.ListAasxPaths();

            using var tmpDir = new TemporaryDirectory();
            string tmpDirPath = tmpDir.Path;

            foreach (string aasxPath in aasxPaths)
            {
                using var package = new AdminShellPackageEnv(aasxPath);
                /*
                      TODO (mristin, 2020-09-17): Remove autofix once XSD and Aasx library in sync

                      Package has been loaded, now we need to do an automatic check & fix.

                      This is necessary as Aasx library is still not conform with the XSD AASX schema and breaks
                      certain constraints (*e.g.*, the cardinality of langString = 1..*).
                    */
                var recs = package.AasEnv.ValidateAll();
                if (recs != null)
                {
                    package.AasEnv.AutoFix(recs);
                }

                // Save as XML
                string name = Path.GetFileName(aasxPath);
                string outPath = System.IO.Path.Combine(tmpDirPath, $"{name}.converted.xml");
                package.SaveAs(outPath, writeFreshly: true);

                using var fileStream = System.IO.File.OpenRead(outPath);
                var records = new AasValidationRecordList();
                validator.Validate(records, fileStream);
                if (records.Count != 0)
                {
                    var parts = new List<string>
                    {
                        $"Failed to validate XML file exported from {aasxPath} to {outPath}:"
                    };
                    parts.AddRange(records.Select((r) => r.Message));
                    throw new AssertionException(string.Join(Environment.NewLine, parts));
                }
            }
        }
    }
}
