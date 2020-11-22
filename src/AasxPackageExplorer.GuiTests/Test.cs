using System.Linq;
using FlaUI.Core.AutomationElements;
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
    }
}
