using Assert = NUnit.Framework.Assert;
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
        public void Test_to_load_a_sample_aasx()
        {
            var path = Common.PathTo01FestoAasx();
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.AssertLoadAasx(application, mainWindow, path);
            });
        }

        [Test]
        public void Test_that_the_asset_image_is_displayed()
        {
            var path = Common.PathTo01FestoAasx();
            Common.RunWithMainWindow((application, automation, mainWindow) =>
            {
                Common.AssertLoadAasx(application, mainWindow, path);

                const string automationId = "AssetPic";

                var assetPic = Retry.Find(
                    () => mainWindow.FindFirstChild(cf => cf.ByAutomationId(automationId)),
                    new RetrySettings { ThrowOnTimeout = true, Timeout = TimeSpan.FromSeconds(5) });

                Assert.IsNotNull(assetPic, $"Could not find the element: {automationId}");

                // The dimensions of the image will not be set properly if the image could not be loaded.
                Assert.AreEqual(
                    (106, 79), (assetPic.BoundingRectangle.Height, assetPic.BoundingRectangle.Width));
            });
        }
    }
}