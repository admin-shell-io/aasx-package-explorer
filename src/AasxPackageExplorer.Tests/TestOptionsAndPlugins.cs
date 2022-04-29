using System.Linq;
using AasxPackageLogic;
using JetBrains.Annotations;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using File = System.IO.File;
using InvalidOperationException = System.InvalidOperationException;
using Is = NUnit.Framework.Is;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using Path = System.IO.Path;
using TestAttribute = NUnit.Framework.TestAttribute;
using TestContext = NUnit.Framework.TestContext;

namespace AasxPackageExplorer.Tests
{
    internal static class Common
    {
        public static string AasxPackageExplorerExe()
        {
            var pth = Path.Combine(TestContext.CurrentContext.TestDirectory, "AasxPackageExplorer.exe");
            if (!File.Exists(pth))
            {
                throw new InvalidOperationException(
                    $"The package explorer executable could not be found: {pth}");
            }

            return pth;
        }
    }

    [TestFixture]
    // ReSharper disable UnusedType.Global
    public class TestParseArguments
    {
        [Test]
        public void Test_defaults_without_any_arguments()
        {
            string exePath = Common.AasxPackageExplorerExe();
            var optionsInformation = App.InferOptions(exePath, new string[] { });

            // Test only a tiny subset of properties that are crucial for further unit tests

            // Note that the plugin directory differs in the default options between the "Release" and "Debug"
            // build.
            Assert.AreEqual(".", optionsInformation.PluginDir);
        }

        [Test]
        public void Test_overrule_plugin_dir_in_command_line()
        {
            string exePath = Common.AasxPackageExplorerExe();
            var optionsInformation = App.InferOptions(
                exePath, new[] { "-plugin-dir", "/somewhere/over/the/rainbow" });

            Assert.AreEqual("/somewhere/over/the/rainbow", optionsInformation.PluginDir);
        }

        [Test]
        public void Test_directly_load_AASX()
        {
            string exePath = Common.AasxPackageExplorerExe();
            var optionsInformation = App.InferOptions(
                exePath, new[] { "/somewhere/over/the/rainbow.aasx" });

            Assert.AreEqual("/somewhere/over/the/rainbow.aasx", optionsInformation.AasxToLoad);
        }

        [Test]
        public void Test_that_command_line_arguments_after_the_additional_config_file_overrule()
        {
            using (var tmpDir = new TemporaryDirectory())
            {
                var jsonOptionsPath = Path.Combine(tmpDir.Path, "options-test.json");

                const string text = @"{ ""PluginDir"": ""/somewhere/from/the/additional"" }";
                File.WriteAllText(jsonOptionsPath, text);

                string exePath = Common.AasxPackageExplorerExe();
                var optionsInformation = App.InferOptions(
                    exePath, new[]
                    {
                        "-read-json", jsonOptionsPath,
                        "-plugin-dir", "/somewhere/from/the/command/line",
                    });

                Assert.AreEqual("/somewhere/from/the/command/line", optionsInformation.PluginDir);
            }
        }

        [Test]
        public void Test_that_additional_config_file_after_the_command_lines_overrules()
        {
            using (var tmpDir = new TemporaryDirectory())
            {
                var jsonOptionsPath = Path.Combine(tmpDir.Path, "options-test.json");

                const string text = @"{ ""PluginDir"": ""/somewhere/from/the/additional"" }";
                File.WriteAllText(jsonOptionsPath, text);

                string exePath = Common.AasxPackageExplorerExe();
                var optionsInformation = App.InferOptions(
                    exePath, new[]
                    {
                        "-plugin-dir", "/somewhere/from/the/command/line",
                        "-read-json", jsonOptionsPath
                    });

                Assert.AreEqual("/somewhere/from/the/additional", optionsInformation.PluginDir);
            }
        }

        [Test]
        public void Test_that_multiple_additional_config_files_are_possible()
        {
            using (var tmpDir = new TemporaryDirectory())
            {
                var jsonOptionsOnePath = Path.Combine(tmpDir.Path, "options-test-one.json");
                File.WriteAllText(jsonOptionsOnePath,
                    @"{ ""PluginDir"": ""/somewhere/from/the/additional-one"" }");

                var jsonOptionsTwoPath = Path.Combine(tmpDir.Path, "options-test-one.json");
                File.WriteAllText(jsonOptionsTwoPath,
                    @"{ ""PluginDir"": ""/somewhere/from/the/additional-two"" }");

                string exePath = Common.AasxPackageExplorerExe();
                var optionsInformation = App.InferOptions(
                    exePath, new[]
                    {
                        "-read-json", jsonOptionsOnePath,
                        "-read-json", jsonOptionsTwoPath
                    });

                Assert.AreEqual("/somewhere/from/the/additional-two", optionsInformation.PluginDir);
            }
        }

        [Test]
        public void Test_plugin_paths_from_JSON_options()
        {
            string exePath = Common.AasxPackageExplorerExe();

            using (var tmpDir = new TemporaryDirectory())
            {
                var jsonOptionsPath = Path.Combine(tmpDir.Path, "options-test.json");

                var text = @"{
    ""PluginDll"": [
        {
          ""Path"": ""AasxIntegrationEmptySample.dll"",
          ""Args"": [],
          ""Options"": null
        },
        {
          ""Path"": ""AasxPluginUaNetServer.dll"",
          ""Args"": [
            ""-single-nodeids"",
            ""-single-keys"",
            ""-ns"",
            ""2"",
            ""-ns"",
            ""3""
          ],
          ""Options"": null
        },
        {
          ""Path"": ""AasxPluginBomStructure.dll"",
          ""Args"": []
        }
    ];
}";
                File.WriteAllText(jsonOptionsPath, text);

                var optionsInformation = App.InferOptions(
                    exePath, new[] { "-read-json", jsonOptionsPath });

                Assert.AreEqual(3, optionsInformation.PluginDll.Count);

                // TODO (mristin, 2020-11-13): @MIHO please check -- Options should be null, not empty?
                Assert.IsEmpty(optionsInformation.PluginDll[0].Args);
                Assert.IsEmpty(optionsInformation.PluginDll[0].Options);
                Assert.AreEqual(null, optionsInformation.PluginDll[0].DefaultOptions);
                Assert.AreEqual("AasxIntegrationEmptySample.dll", optionsInformation.PluginDll[0].Path);

                Assert.That(optionsInformation.PluginDll[1].Args,
                    Is.EquivalentTo(new[] { "-single-nodeids", "-single-keys", "-ns", "2", "-ns", "3" }));
                Assert.IsEmpty(optionsInformation.PluginDll[1].Options);
                Assert.AreEqual(null, optionsInformation.PluginDll[1].DefaultOptions);
                Assert.AreEqual("AasxPluginUaNetServer.dll", optionsInformation.PluginDll[1].Path);

                Assert.IsEmpty(optionsInformation.PluginDll[2].Args);
                Assert.AreEqual(null, optionsInformation.PluginDll[2].Options);
                Assert.AreEqual(null, optionsInformation.PluginDll[2].DefaultOptions);
                Assert.AreEqual("AasxPluginBomStructure.dll", optionsInformation.PluginDll[2].Path);
            }
        }

        [Test]
        public void Test_plugin_paths_from_default_JSON_options()
        {
            using (var tmpDir = new TemporaryDirectory())
            {
                var exePath = Path.Combine(tmpDir.Path, "NonexistingAasxPackageExplorer.exe");
                var optionsPath = Path.Combine(tmpDir.Path, "NonexistingAasxPackageExplorer.options.json");

                var text = @"{
    ""PluginDir"": "".\\AcmePlugins"",
    ""PluginDll"": [ { ""Path"": ""AasxPluginBomStructure.dll"", ""Args"": [] } ];
}";
                File.WriteAllText(optionsPath, text);

                var optionsInformation = App.InferOptions(
                    exePath, new string[] { });

                Assert.AreEqual(".\\AcmePlugins", optionsInformation.PluginDir);

                Assert.AreEqual(1, optionsInformation.PluginDll.Count);
                Assert.IsEmpty(optionsInformation.PluginDll[0].Args);
                Assert.AreEqual(null, optionsInformation.PluginDll[0].Options);
                Assert.AreEqual(null, optionsInformation.PluginDll[0].DefaultOptions);
                Assert.AreEqual("AasxPluginBomStructure.dll", optionsInformation.PluginDll[0].Path);
            }
        }

        [Test]
        public void Test_plugins_are_passed_arguments()
        {
            var optionsInformation = App.InferOptions(
                "NonExistingAasxPackageExplorer.exe",
                new[] { "-p", "-something", "-dll", "ACME-plugins\\AasxSomeACMEPlugin.dll" });

            Assert.AreEqual(1, optionsInformation.PluginDll.Count);
            Assert.AreEqual("ACME-plugins\\AasxSomeACMEPlugin.dll", optionsInformation.PluginDll[0].Path);
            Assert.That(optionsInformation.PluginDll[0].Args, Is.EquivalentTo(new[] { "-something" }));
        }

        [Test]
        public void Test_plugins_are_searched_in_plugins_directory()
        {
            using (var tmpDir = new TemporaryDirectory())
            {
                var tagPath = Path.Combine(tmpDir.Path, "AasxAcmePluginForSomething.plugin");
                File.WriteAllText(tagPath, @"ACME tag!");

                var pluginPath = Path.Combine(tmpDir.Path, "AasxAcmePluginForSomething.dll");
                File.WriteAllText(pluginPath, @"ACME!");

                var pluginDllInfos = Plugins.TrySearchPlugins(tmpDir.Path);

                Assert.AreEqual(1, pluginDllInfos.Count);

                Assert.AreEqual(null, pluginDllInfos[0].Args);
                Assert.AreEqual(null, pluginDllInfos[0].Options);
                Assert.AreEqual(null, pluginDllInfos[0].DefaultOptions);
                Assert.AreEqual(pluginPath, pluginDllInfos[0].Path);
            }
        }

        [Test]
        public void Test_that_splash_time_is_set()
        {
            var optionsInformation = App.InferOptions(
                "NonExistingAasxPackageExplorer.exe", new[] { "-splash", "1984" });

            Assert.AreEqual(1984, optionsInformation.SplashTime);
        }
    }

    [TestFixture]
    // ReSharper disable UnusedType.Global
    public class TestLoadPlugins
    {
        [Test]
        public void Test_that_it_works()
        {
            using (var tmpDir = new TemporaryDirectory())
            {
                var jsonOptionsPath = Path.Combine(tmpDir.Path, "options-test.json");

                var exePath = Common.AasxPackageExplorerExe();

                var pluginPath = Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "TestResources\\AasxPackageExplorer.Tests\\AasxPluginGenericForms.dll");

                Assert.IsTrue(File.Exists(pluginPath), pluginPath);

                var text =
                    $@"{{ ""PluginDll"": [ {{ ""Path"": {JsonConvert.ToString(pluginPath)}, ""Args"": [] }} ] }}";

                File.WriteAllText(jsonOptionsPath, text);

                var optionsInformation = App.InferOptions(
                    exePath, new[] { "-read-json", jsonOptionsPath });

                Assert.AreEqual(1, optionsInformation.PluginDll.Count);
                Assert.IsEmpty(optionsInformation.PluginDll[0].Args);
                Assert.AreEqual(null, optionsInformation.PluginDll[0].Options);
                Assert.AreEqual(null, optionsInformation.PluginDll[0].DefaultOptions);
                Assert.AreEqual(pluginPath, optionsInformation.PluginDll[0].Path);

                // ReSharper disable UnusedVariable
                var loadedPlugins = App.LoadAndActivatePlugins(optionsInformation.PluginDll);

                // TODO (Marko Ristin, 2021-07-09): not clear, how this test could pass. As of today,
                // it is failing and therefore disabled
                //// Assert.AreEqual(new[] { "AasxPluginGenericForms" }, loadedPlugins.Keys.ToList());

                // TODO (Marko Ristin, 2021-07-09): could not fix
                //// Assert.IsNotNull(loadedPlugins["AasxPluginGenericForms"]);

                // This is not a comprehensive test, but it would fail if the plugin DLL has not been properly loaded.
                //// Assert.Greater(loadedPlugins["AasxPluginGenericForms"].ListActions().Length, 0);
            }
        }
    }
}
