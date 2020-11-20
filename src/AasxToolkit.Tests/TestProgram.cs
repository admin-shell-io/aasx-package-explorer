﻿using System;
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

                        Assert.AreEqual(0, code);
                        Assert.AreEqual("", consoleCap.Error());
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
                    using (var consoleCap = new ConsoleCapture())
                    {
                        int code = AasxToolkit.Program.MainWithExitCode(
                            new[]
                            {
                                "load", pth,
                                "export-template", Path.Combine(tmpDir.Path, "exported.template")
                            });

                        Assert.AreEqual(0, code);
                        Assert.AreEqual("", consoleCap.Error());
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
                        int code = AasxToolkit.Program.MainWithExitCode(
                            new[]
                            {
                                "load", pth,
                                "check",
                                "save", Path.Combine(tmpDir.Path, "saved.xml")
                            });

                        Assert.AreEqual(0, code);
                        Assert.AreEqual("", consoleCap.Error());
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

                            Assert.AreEqual(0, code);
                            Assert.AreEqual("", consoleCap.Error());
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

                    Assert.AreEqual(0, code);
                    Assert.AreEqual("", consoleCap.Error());
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

                    Assert.AreEqual(0, code);
                    Assert.AreEqual("", consoleCap.Error());
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

                    Assert.AreEqual(0, code);
                    Assert.AreEqual("", consoleCap.Error());
                    Assert.IsTrue(consoleCap.Output().StartsWith("AasxToolkit:"));  // Start of the help message
                }
            }
        }
    }
}
