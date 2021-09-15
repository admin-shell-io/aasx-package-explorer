using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AasxToolkit.Test;
using NUnit.Framework;
using InvalidOperationException = System.InvalidOperationException;

namespace AasxToolkit.Tests
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

    public class TestLoadSave
    {
        [Test]
        public void TestNoErrorOnSamples()
        {
            foreach (string pth in SamplesAasxDir.ListAasxPaths())
            {
                using (var tmpDir = new TemporaryDirectory())
                {
                    using (var consoleCap = new ConsoleCapture())
                    {
                        int code = AasxToolkit.Program.MainWithExitCode(
                            new[]
                            {
                                "load", pth,
                                "save", Path.Combine(tmpDir.Path, "saved.xml")
                            });

                        if (consoleCap.Error() != "")
                        {
                            throw new AssertionException(
                                $"Expected no stderr, but got:{System.Environment.NewLine} " +
                                consoleCap.Error());
                        }

                        Assert.AreEqual(0, code);
                    }
                }
            }
        }
    }

    public class TestExportTemplate
    {
        [Test]
        public void TestNoErrorOnSamples()
        {
            foreach (string pth in SamplesAasxDir.ListAasxPaths())
            {
                using (var tmpDir = new TemporaryDirectory())
                {
                    string targetPth = Path.Combine(tmpDir.Path, "exported.template");

                    using (var consoleCap = new ConsoleCapture())
                    {
                        int code = AasxToolkit.Program.MainWithExitCode(
                            new[]
                            {
                                "load", pth,
                                "export-template", targetPth
                            });

                        if (consoleCap.Error() != "")
                        {
                            throw new AssertionException(
                                $"Expected no stderr, but got:{System.Environment.NewLine}" +
                                consoleCap.Error() +
                                System.Environment.NewLine +
                                System.Environment.NewLine +
                                "The original command was:" + System.Environment.NewLine +
                                $"AasxToolkit load {pth} export-template {targetPth}");
                        }

                        Assert.AreEqual(0, code);
                    }
                }
            }
        }
    }

    public class TestLoadCheckSave
    {
        [Test]
        public void TestNoErrorOnSamples()
        {
            foreach (string pth in SamplesAasxDir.ListAasxPaths())
            {
                using (var tmpDir = new TemporaryDirectory())
                {
                    using (var consoleCap = new ConsoleCapture())
                    {
                        string targetPth = Path.Combine(tmpDir.Path, "saved.xml");

                        int code = AasxToolkit.Program.MainWithExitCode(
                            new[]
                            {
                                "load", pth,
                                "check",
                                "save", targetPth
                            });

                        if (consoleCap.Error() != "")
                        {
                            throw new AssertionException(
                                $"Expected no stderr, but got:{System.Environment.NewLine}" +
                                consoleCap.Error() +
                                System.Environment.NewLine +
                                System.Environment.NewLine +
                                $"The executed command was: " +
                                $"AasxToolkit load {pth} check save {targetPth}");
                        }

                        Assert.AreEqual(0, code);
                    }
                }
            }
        }

        public class TestLoadCheckAndFixSave
        {
            [Test]
            public void TestNoErrorOnSamples()
            {
                foreach (string pth in SamplesAasxDir.ListAasxPaths())
                {
                    using (var tmpDir = new TemporaryDirectory())
                    {
                        using (var consoleCap = new ConsoleCapture())
                        {
                            int code = AasxToolkit.Program.MainWithExitCode(
                                new[]
                                {
                                    "load", pth,
                                    "check+fix",
                                    "save", Path.Combine(tmpDir.Path, "saved.xml")
                                });

                            if (consoleCap.Error() != "")
                            {
                                throw new AssertionException(
                                    $"Expected no stderr, but got:{System.Environment.NewLine}" +
                                    consoleCap.Error());
                            }

                            Assert.AreEqual(0, code);
                        }
                    }
                }
            }
        }

        public class TestHelp
        {
            [Test]
            public void TestDisplayedWhenNoArguments()
            {
                using (var consoleCap = new ConsoleCapture())
                {
                    int code = AasxToolkit.Program.MainWithExitCode(new string[] { });

                    if (consoleCap.Error() != "")
                    {
                        throw new AssertionException(
                            $"Expected no stderr, but got:{System.Environment.NewLine}" +
                            consoleCap.Error());
                    }

                    Assert.AreEqual(0, code);

                    Assert.IsTrue(consoleCap.Output().StartsWith("AasxToolkit:"));  // Start of the help message
                }
            }

            [TestCase("help")]
            [TestCase("-help")]
            [TestCase("--help")]
            [TestCase("/help")]
            [TestCase("-h")]
            [TestCase("/h")]
            public void TestDisplayedWhenHelpArgument(string helpArg)
            {
                using (var consoleCap = new ConsoleCapture())
                {
                    int code = AasxToolkit.Program.MainWithExitCode(new[] { helpArg });

                    if (consoleCap.Error() != "")
                    {
                        throw new AssertionException(
                            $"Expected no stderr, but got:{System.Environment.NewLine}" +
                            consoleCap.Error());
                    }

                    Assert.AreEqual(0, code);

                    Assert.IsTrue(consoleCap.Output().StartsWith("AasxToolkit:"));  // Start of the help message
                }
            }

            [Test]
            public void TestHelpTrumpsOtherArguments()
            {
                using (var consoleCap = new ConsoleCapture())
                {
                    int code = AasxToolkit.Program.MainWithExitCode(
                        new[] { "load", "doesnt-exist.aasx", "help" });

                    if (consoleCap.Error() != "")
                    {
                        throw new AssertionException(
                            $"Expected no stderr, but got:{System.Environment.NewLine}" +
                            consoleCap.Error());
                    }

                    Assert.AreEqual(0, code);

                    Assert.IsTrue(consoleCap.Output().StartsWith("AasxToolkit:"));  // Start of the help message
                }
            }
        }

        public class TestValidation
        {
            [Test]
            public void TestAgainstSampleData()
            {
                var samplePaths = new List<string>
                {
                    Path.Combine(
                        TestContext.CurrentContext.TestDirectory,
                        "TestResources\\AasxToolkit.Tests\\sample.xml")
                    /*
                     TODO (mristin, 2020-10-30): add json once the validation is in place.
                     Michael Hoffmeister had it almost done today.
                     
                    Path.Combine(
                        TestContext.CurrentContext.TestDirectory,
                        "TestResources\\AasxToolkit.Tests\\sample.json")
                        
                        dead-csharp ignore this comment
                        */
                };

                foreach (string samplePath in samplePaths)
                {
                    if (!File.Exists(samplePath))
                    {
                        throw new FileNotFoundException($"The sample file could not be found: {samplePath}");
                    }

                    using (var tmpDir = new TemporaryDirectory())
                    {
                        using (var consoleCap = new ConsoleCapture())
                        {
                            string extension = Path.GetExtension(samplePath);
                            string tmpPath = Path.Combine(tmpDir.Path, $"to-be-validated{extension}");

                            int code = AasxToolkit.Program.MainWithExitCode(
                                new[] { "load", samplePath, "check+fix", "save", tmpPath, "validate", tmpPath });

                            if (consoleCap.Error() != "")
                            {
                                var nl = System.Environment.NewLine;
                                throw new AssertionException(
                                    $"Expected no stderr for the sample file {samplePath}, but got:{nl}" +
                                    $"{consoleCap.Error()}");
                            }

                            Assert.AreEqual(0, code);
                        }
                    }
                }
            }
        }
    }
}
