
using TestAttribute = NUnit.Framework.TestAttribute;

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
        public void Test_to_load_sample_aasxs()
        {
            foreach (string path in Common.ListAasxPaths())
            {
                Common.RunWithMainWindow((application, automation, mainWindow) =>
                {
                    Common.AssertLoad(application, mainWindow, path);
                });
            }
        }
    }
}