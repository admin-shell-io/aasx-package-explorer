using System;
using System.IO;
using System.Linq;
using System.Windows.Interop;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using AssertionException = NUnit.Framework.AssertionException;
using File = System.IO.File;
using Path = System.IO.Path;
using Retry = FlaUI.Core.Tools.Retry;
using TestAttribute = NUnit.Framework.TestAttribute;
using TimeSpan = System.TimeSpan;

// ReSharper disable MergeIntoPattern

namespace AasxPackageExplorer.GuiTests
{
    public class TestBasic
    {
        [Test]
        public void Test_application_start()
        {
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.AssertNoErrors(application, mainWindow);
            });
        }

        [Test]
        public void Test_that_splash_screen_does_not_break_the_app()
        {
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.RequireTopLevelWindow(application, automation, (w) => w.AutomationId == "splashScreen",
                        "Could not find the splash screen window");
                Common.AssertNoErrors(application, mainWindow);
            }, new Run { Args = new[] { "-splash", "5000" } });
        }

        [Test]
        public void Test_that_about_does_not_break_the_app()
        {
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.RequireMenuItem(mainWindow, "Help", "About ..").Click();
                Common.RequireTopLevelWindowByTitle(application, automation, "About");
            });
        }

        [Test]
        public void Test_to_load_a_sample_aasx()
        {
            var path = Common.PathTo01FestoAasx();
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.AssertLoadAasx(application, mainWindow, path);
                Common.AssertNoErrors(application, mainWindow);
            });
        }

        [Test]
        public void Test_to_load_and_reload_sample_aasxes()
        {
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.AssertLoadAasx(application, mainWindow, Common.PathTo01FestoAasx());
                Common.AssertNoErrors(application, mainWindow);

                Common.AssertLoadAasx(application, mainWindow, Common.PathTo34FestoAasx());
                Common.AssertNoErrors(application, mainWindow);
            });
        }

        [Test]
        public void Test_that_the_asset_image_is_displayed()
        {
            var path = Common.PathTo01FestoAasx();
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.AssertLoadAasx(application, mainWindow, path);
                Common.AssertNoErrors(application, mainWindow);

                const string automationId = "AssetPic";

                var assetPic = Retry.Find(
                    () => mainWindow.FindFirstChild(cf => cf.ByAutomationId(automationId)),
                    new RetrySettings { ThrowOnTimeout = true, Timeout = TimeSpan.FromSeconds(5) });

                Assert.IsNotNull(assetPic, $"Could not find the element: {automationId}");

                // The dimensions of the image will not be set properly if the image could not be loaded.
                if (assetPic.BoundingRectangle.Height <= 1 ||
                    assetPic.BoundingRectangle.Width <= 1)
                {
                    throw new AssertionException(
                        "The asset picture has unexpected dimensions: " +
                        $"width is {assetPic.BoundingRectangle.Width} and " +
                        $"height is {assetPic.BoundingRectangle.Height}");
                }
            });
        }

        [Test]
        public void Test_that_opening_an_invalid_AASX_does_not_break_the_app()
        {
            using var tmpDir = new TemporaryDirectory();
            var path = Path.Combine(tmpDir.Path, "invalid.aasx");
            File.WriteAllText(path, "totally invalid");

            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.AssertLoadAasx(application, mainWindow, path);
                var numberErrors = Retry.Find(
                    () => (application.HasExited)
                        ? null
                        : mainWindow.FindFirstChild(cf => cf.ByAutomationId("LabelNumberErrors")),
                    new RetrySettings { ThrowOnTimeout = true, Timeout = TimeSpan.FromSeconds(5) });

                Assert.AreEqual("Errors: 1", numberErrors.AsLabel().Text);
            });
        }

        [Test]
        public void Test_that_error_report_doesnt_break_the_app()
        {
            using var tmpDir = new TemporaryDirectory();
            var path = Path.Combine(tmpDir.Path, "invalid.aasx");
            File.WriteAllText(path, "totally invalid");

            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.AssertLoadAasx(application, mainWindow, path);

                var numberErrors = Retry.Find(
                    () => (application.HasExited)
                        ? null
                        : mainWindow.FindFirstChild(cf => cf.ByAutomationId("LabelNumberErrors")),
                    new RetrySettings { ThrowOnTimeout = true, Timeout = TimeSpan.FromSeconds(5) });

                Assert.AreEqual("Errors: 1", numberErrors.AsLabel().Text);

                var buttonReport = Retry.Find(
                    () => mainWindow.FindFirstChild(cf => cf.ByAutomationId("ButtonReport")),
                    new RetrySettings
                    {
                        ThrowOnTimeout = true,
                        Timeout = TimeSpan.FromSeconds(5),
                        TimeoutMessage = "Could not find the report button"
                    }).AsButton();

                buttonReport.Click();

                Common.RequireTopLevelWindowByTitle(application, automation, "Message Report");
            });
        }

        [Test]
        public void Test_that_tree_doesnt_change()
        {
            var path = Common.PathTo01FestoAasx();
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.AssertNoErrors(application, mainWindow);

                var tree = Retry.Find(
                    () => mainWindow.FindFirstDescendant(
                        cf => cf.ByAutomationId("treeViewInner")),
                    new RetrySettings
                    {
                        ThrowOnTimeout = true,
                        Timeout = TimeSpan.FromSeconds(5),
                        TimeoutMessage = "Could not find the treeViewInner tree"
                    }).AsTree();

                if (tree == null)
                {
                    throw new AssertionException("tree unexpectedly null");
                }

                static string RenderTree(Tree aTree)
                {
                    using var sw = new StringWriter();

                    void RenderItem(TreeItem item, int level)
                    {
                        item.Patterns.ScrollItem.Pattern.ScrollIntoView();

                        // Collect all text children to create a label
                        var children = item.FindAllChildren(
                            cf => cf.ByClassName("TextBlock"));

                        var label = "[" + string.Join(", ",
                            children.Select(c => Common.Quote(c.AsLabel().Text))) + "]";

                        sw.WriteLine($"{new string('*', level)}{label}");

                        // Expand
                        var expander = item
                            .FindFirstChild(cf => cf.ByAutomationId("Expander"))
                            .AsToggleButton();

                        if (expander != null && !expander.IsOffscreen && expander.ToggleState == ToggleState.Off)
                        {
                            expander.Click(false);
                        }

                        foreach (var subitem in item.Items)
                        {
                            RenderItem(subitem, level + 1);
                        }
                    }

                    foreach (var item in aTree.Items)
                    {
                        RenderItem(item, 1);
                    }

                    return sw.ToString();
                }

                string got = RenderTree(tree);

                string relExpectedPth = Path.Combine(
                    "TestResources", "AasxPackageExplorer.GuiTests",
                    "ExpectedTrees", $"{Path.GetFileName(path)}.tree.txt");

                string expectedPth = Path.Combine(TestContext.CurrentContext.TestDirectory, relExpectedPth);
                string expected = File.ReadAllText(expectedPth);

                string gotPth = Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    Path.GetDirectoryName(relExpectedPth) ??
                        throw new InvalidOperationException(
                            $"Unexpected null directory from: {relExpectedPth}"),
                    Path.GetFileName(relExpectedPth) + ".got");
                File.WriteAllText(gotPth, got);

                Assert.AreEqual(got, expected,
                    "The expected tree structure does not coincide with the tree structure rendered from the UI. " +
                    "If you made changes to UI, please make sure you update the file containing expected values " +
                    $"accordingly (search for the file {relExpectedPth}).\n\n" +
                    $"The test used the file available in the test context: {expectedPth} " +
                    "(you probably don't want to change *that* file, but the original one in the source code)\n\n" +
                    "The tree structure obtained from the application was stored " +
                    $"for your convenience to: {gotPth}\n\n" +
                    "Use a diff tool to inspect the differences.");
            }, new Run { Args = new[] { "-splash", "0", path } });
        }

        [Test]
        public void Test_that_document_shelf_doesnt_break_the_app()
        {
            var path = Common.PathTo34FestoAasx();
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.AssertNoErrors(application, mainWindow);

                var tree = Retry.Find(
                    () => mainWindow.FindFirstDescendant(
                        cf => cf.ByAutomationId("treeViewInner")),
                    new RetrySettings
                    {
                        ThrowOnTimeout = true,
                        Timeout = TimeSpan.FromSeconds(5),
                        TimeoutMessage = "Could not find the treeViewInner tree"
                    }).AsTree();

                Assert.AreEqual(1, tree.Items.Length,
                    $"Expected only one node at the root, but got: {tree.Items.Length}");

                var root = tree.Items[0];

                // Find documentation

                const string documentationLabel = "\"Documentation\" ";

                TreeItem? documentationItem = root.Items.FirstOrDefault(
                    item =>
                        item.FindFirstChild(
                            cf =>
                                cf.ByClassName("TextBlock").And(
                                    cf.ByName(documentationLabel))) != null);

                Assert.IsNotNull(documentationItem,
                    $"Could not find the item in the tree containing the text block '{documentationLabel}'");

                var expander = documentationItem
                    .FindFirstChild(cf => cf.ByAutomationId("Expander"))
                    .AsToggleButton();

                if (expander != null && !expander.IsOffscreen && expander.ToggleState == ToggleState.Off)
                {
                    expander.Click(false);
                }

                // Find Document shelf

                const string documentShelfLabel = "Document Shelf";

                var documentShelfTextBlock = documentationItem.FindFirstDescendant(
                    cf => cf.ByClassName("TextBlock").And(cf.ByName(documentShelfLabel)));

                Assert.IsNotNull(documentShelfTextBlock,
                    $"Could not find the text block in the tree '{documentShelfLabel}'");

                documentShelfTextBlock.Click();

                Common.AssertNoErrors(application, mainWindow);

                var shelfControl = mainWindow.FindFirstDescendant(
                    cf => cf.ByAutomationId("shelfControl"));
                Assert.IsNotNull(shelfControl, "Could not find 'shelfControl' by automation ID");
            }, new Run { Args = new[] { "-splash", "0", path } });
        }

        [Test]
        public void Test_that_technical_viewer_doesnt_break_the_app()
        {
            var path = Common.PathTo34FestoAasx();
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.AssertNoErrors(application, mainWindow);

                var tree = Retry.Find(
                    () => mainWindow.FindFirstDescendant(
                        cf => cf.ByAutomationId("treeViewInner")),
                    new RetrySettings
                    {
                        ThrowOnTimeout = true,
                        Timeout = TimeSpan.FromSeconds(5),
                        TimeoutMessage = "Could not find the treeViewInner tree"
                    }).AsTree();

                Assert.AreEqual(1, tree.Items.Length,
                    $"Expected only one node at the root, but got: {tree.Items.Length}");

                var root = tree.Items[0];

                // Find the ZVEI tree item

                const string technicalDataZveiLabel = "\"TechnicalData ZVEI\" ";

                TreeItem? technicalDataZvei = root.Items.FirstOrDefault(
                    item =>
                        item.FindFirstChild(
                            cf =>
                                cf.ByClassName("TextBlock").And(
                                    cf.ByName(technicalDataZveiLabel))) != null);

                Assert.IsNotNull(technicalDataZvei,
                    $"Could not find the item in the tree containing the text block '{technicalDataZveiLabel}'");


                var expander = technicalDataZvei
                    .FindFirstChild(cf => cf.ByAutomationId("Expander"))
                    .AsToggleButton();

                if (expander != null && !expander.IsOffscreen && expander.ToggleState == ToggleState.Off)
                {
                    expander.Click();
                }

                const string technicalDataViewerLabel = "Technical Data Viewer";

                var technicalDataViewer = technicalDataZvei.FindFirstDescendant(
                    cf => cf.ByClassName("TextBlock").And(
                        cf.ByName(technicalDataViewerLabel)));

                Assert.IsNotNull(technicalDataViewer,
                    $"Could not find the text block '{technicalDataViewerLabel}'");

                var technicalDataViewControl =
                    mainWindow.FindFirstDescendant(cf => cf.ByClassName("TechnicalDataViewControl"));
                Assert.IsNull(
                    technicalDataViewControl,
                    "Unexpectedly found the control with class name 'TechnicalDataViewControl'");

                technicalDataViewer.Click();

                technicalDataViewControl =
                    mainWindow.FindFirstDescendant(cf => cf.ByClassName("TechnicalDataViewControl"));
                Assert.IsNotNull(
                    technicalDataViewControl, "Could not find the control with class name 'TechnicalDataViewControl'");

            }, new Run { Args = new[] { "-splash", "0", path } });
        }
    }

    public class TestDictionaryImport
    {
        [Test]
        public void Test_import_dialog()
        {
            var aasxPath = Common.PathTo01FestoAasx();
            var dictImportDir = Path.Combine(Common.TestResourcesDir(), "IecCdd", "empty");
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.RequireMenuItem(mainWindow, "File", "Import ..", "Import Submodel from Dictionary ..").Click();

                var window = Common.RequireTopLevelWindowByTitle(application, automation, "Dictionary Import");

                var dataProviders = window.FindFirstChild("DataSourceLabel").AsLabel().Text;
                Assert.That(dataProviders.Contains("IEC CDD"), "IEC CDD is not listed as a data provider");

                var dataSources = window.FindFirstChild("ComboBoxSource").AsComboBox().Items;
                Assert.That(dataSources.Length == 0, "Expected no default sources for an empty dictionary import dir");

                Common.AssertNoErrors(application, mainWindow);
            }, new Run { Args = new[] { "-dict-import-dir", dictImportDir, aasxPath } });
        }

        [Test]
        public void Test_simple_iec_cdd_data_source()
        {
            var aasxPath = Common.PathTo01FestoAasx();
            var dictImportDir = Path.Combine(Common.TestResourcesDir(), "IecCdd", "simple");
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.RequireMenuItem(mainWindow, "File", "Import ..", "Import Submodel from Dictionary ..").Click();

                var window = Common.RequireTopLevelWindowByTitle(application, automation, "Dictionary Import");

                var comboBoxDataSources = window.FindFirstChild("ComboBoxSource").AsComboBox();
                var dataSources = comboBoxDataSources.Items;
                Assert.That(dataSources.Length == 1,
                        "Expected one default sources for the dictionary import dir 'simple'");
                var dataSource = dataSources[0].Text;
                Assert.That(dataSource == "IEC CDD: simple",
                        $"Unexpected label for the simple IEC CDD data source: '{dataSource}'");

                comboBoxDataSources.Select(0);

                var topLevelView = window.FindFirstChild("ClassViewControl").AsListBox();
                var topLevelElements = Retry.WhileEmpty(() => topLevelView.Items,
                        timeout: TimeSpan.FromSeconds(5),
                        throwOnTimeout: true,
                        timeoutMessage: "Could not find top-level elements from simple IEC CDD source"
                ).Result;

                Assert.That(topLevelElements.Select(e => e.Text), Is.EqualTo(new[] { "C1", "C2", "C3" }),
                        "Unexpected top-level elements for the simple IEC CDD source");

                Common.AssertNoErrors(application, mainWindow);
            }, new Run { Args = new[] { "-dict-import-dir", dictImportDir, aasxPath } });
        }
    }
}
