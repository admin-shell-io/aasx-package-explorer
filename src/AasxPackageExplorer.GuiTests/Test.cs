using System;
using System.IO;
using System.Linq;
using System.Windows.Interop;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using AssertionException = NUnit.Framework.AssertionException;
using File = System.IO.File;
using Path = System.IO.Path;
using Retry = FlaUI.Core.Tools.Retry;
using TestAttribute = NUnit.Framework.TestAttribute;
using TimeSpan = System.TimeSpan;

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
                Retry.WhileNull(() =>
                        // ReSharper disable once AccessToDisposedClosure
                        application.GetAllTopLevelWindows(automation)
                            .FirstOrDefault((w) => w.Title == "AASX Package Explorer Splash Screen"),
                    throwOnTimeout: true, timeout: TimeSpan.FromSeconds(5),
                    timeoutMessage: "Could not find the splash screen"
                );

                Common.AssertNoErrors(application, mainWindow);
            }, new Run { Args = new[] { "-splash", "5000" } });
        }

        [Test]
        public void Test_that_about_does_not_break_the_app()
        {
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                var helpMenuItem = mainWindow
                    .FindFirstDescendant(
                        cf => cf.ByClassName("MenuItem").And(cf.ByName("Help")))
                    .AsMenuItem();

                helpMenuItem.Click();

                var aboutMenuItem = helpMenuItem
                    .FindFirstChild(cf => cf.ByName("About .."))
                    .AsMenuItem();

                aboutMenuItem.Click();

                Retry.WhileNull(() =>
                        // ReSharper disable once AccessToDisposedClosure
                        application.GetAllTopLevelWindows(automation)
                            .FirstOrDefault((w) => w.Title == "About"),
                    throwOnTimeout: true, timeout: TimeSpan.FromSeconds(5),
                    timeoutMessage: "Could not find the about window"
                );
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

                Retry.WhileNull(() =>
                        // ReSharper disable once AccessToDisposedClosure
                        application.GetAllTopLevelWindows(automation)
                            .FirstOrDefault((w) => w.Title == "Message Report"),
                    throwOnTimeout: true, timeout: TimeSpan.FromSeconds(5),
                    timeoutMessage: "Could not find the 'Message Report' window");
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
                    Path.GetDirectoryName(relExpectedPth),
                    Path.GetFileName(relExpectedPth) + ".got");
                File.WriteAllText(gotPth, got);

                Assert.AreEqual(got, expected,
                    "The expected tree structure does not coincide with the tree structure rendered from the UI. " +
                    "If you made changes to UI, please make sure you update the file containing expected values " +
                    $"accordingly (search for the file {relExpectedPth}).\n\n" +
                    $"The test used the file available in the test context: {expectedPth} " +
                    $"(you probably don't want to change *that* file, but the original one in the source code)\n\n" +
                    $"The tree structure obtained from the application was stored " +
                    $"for your convenience to: {gotPth}\n\n" +
                    $"Use a diff tool to inspect the differences.");
            }, new Run { Args = new[] { "-splash", "0", path } });
        }
    }
}
