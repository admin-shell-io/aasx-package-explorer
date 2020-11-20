using System.Linq;
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

    public class TestParseArguments
    {
        [Test]
        public void TestDefaultsWithoutAnyArguments()
        {
            string exePath = Common.AasxPackageExplorerExe();
            var optionsInformation = App.InferOptions(exePath, new string[] { });

            // Test only a tiny subset of properties that are crucial for further unit tests

            // Note that the plugin directory differs in the default options between the "Release" and "Debug"
            // build.
            Assert.AreEqual(".", optionsInformation.PluginDir);
        }


        [Test]
        public void TestPluginPathsFromJsonOptions()
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
        public void TestPluginPathsFromDefaultJsonOptions()
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
        public void TestPluginsAreSearchedInPluginsDirectory()
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
    }

    public class TestLoadPlugins
    {
        [Test]
        public void TestThatItWorks()
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

                var loadedPlugins = App.LoadAndActivatePlugins(optionsInformation.PluginDll);

                Assert.AreEqual(new[] { "AasxPluginGenericForms" }, loadedPlugins.Keys.ToList());
                Assert.IsNotNull(loadedPlugins["AasxPluginGenericForms"]);

                // This is not a comprehensive test, but it would fail if the plugin DLL has not been properly loaded.
                Assert.Greater(loadedPlugins["AasxPluginGenericForms"].ListActions().Length, 0);
            }
        }
    }
}
