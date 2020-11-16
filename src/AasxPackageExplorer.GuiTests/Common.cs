using System.Linq;
using FlaUI.Core.AutomationElements; // necessary extension for AsLabel() and other methods
using Application = FlaUI.Core.Application;
using Assert = NUnit.Framework.Assert;
using AssertionException = NUnit.Framework.AssertionException;
using Directory = System.IO.Directory;
using File = System.IO.File;
using FileNotFoundException = System.IO.FileNotFoundException;
using InvalidOperationException = System.InvalidOperationException;
using Path = System.IO.Path;
using Retry = FlaUI.Core.Tools.Retry;
using TimeSpan = System.TimeSpan;
using UIA3Automation = FlaUI.UIA3.UIA3Automation;
using Window = FlaUI.Core.AutomationElements.Window;

namespace AasxPackageExplorer.GuiTests
{
    static class Common
    {
        public static string PathTo01FestoAasx()
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

            if (!Directory.Exists(sampleAasxDir))
            {
                throw new InvalidOperationException(
                    $"The directory containing the sample AASXs does not exist or is not a directory: " +
                    $"{sampleAasxDir}; did you download the samples with DownloadSamples.ps1?");
            }

            var pth = Path.Combine(sampleAasxDir, "01_Festo.aasx");

            if (!File.Exists(pth))
            {
                throw new FileNotFoundException($"The Article-ovel sample AASX could not be found: {pth}");
            }

            return pth;
        }


        public delegate void Implementation(Application application, UIA3Automation automation, Window mainWindow);

        /// <summary>
        /// Finds the main AASX Package Explorer window and executes the code dependent on it.
        /// </summary>
        /// <remarks>This method is necessary since splash screen confuses FlaUI and prevents us from
        /// easily determining the main window.</remarks>
        /// <param name="implementation">Code to be executed</param>
        public static void RunWithMainWindow(Implementation implementation)
        {
            string environmentVariable = "AASX_PACKAGE_EXPLORER_RELEASE_DIR";
            string releaseDir = System.Environment.GetEnvironmentVariable(environmentVariable);
            if (releaseDir == null)
            {
                throw new InvalidOperationException(
                    $"Expected the environment variable to be set: {environmentVariable}; " +
                    "otherwise we can not find binaries to be tested through functional tests.");
            }

            string pathToExe = Path.Combine(releaseDir, "AasxPackageExplorer.exe");
            if (!File.Exists(pathToExe))
            {
                throw new FileNotFoundException(
                    "The executable of the AASX Package Explorer " +
                    $"could not be found in the release directory: {pathToExe}; did you compile it properly before?");
            }

            var app = Application.Launch(pathToExe);
            try
            {
                using (var automation = new UIA3Automation())
                {
                    // ReSharper disable once AccessToDisposedClosure
                    Retry.WhileEmpty(() => app.GetAllTopLevelWindows(automation));

                    var mainWindow = app
                        .GetAllTopLevelWindows(automation)
                        .First((w) => w.Title == "AASX Package Explorer");

                    implementation(app, automation, mainWindow);
                }
            }
            finally
            {
                app.Kill();
            }
        }

        /// <summary>
        /// Retry until the label element with number of errors is found and then check that there are no errors.
        /// 
        /// If the search times out, an exception will be thrown.
        /// </summary>
        /// <param name="application">AASX Package Explorer application under test</param>
        /// <param name="mainWindow">Main window of <paramref name="application"/></param>
        /// <remarks>Both <paramref name="application"/> and <paramref name="mainWindow"/> should be obtained
        /// with <see cref="RunWithMainWindow"/></remarks>
        public static void AssertNoErrors(Application application, Window mainWindow)
        {
            const string automationId = "LabelNumberErrors";

            var numberErrors = Retry.Find(
                () => (application.HasExited)
                    ? null
                    : mainWindow.FindFirstChild(cf => cf.ByAutomationId(automationId)),
                new RetrySettings { ThrowOnTimeout = true, Timeout = TimeSpan.FromSeconds(5) });

            Assert.IsFalse(application.HasExited,
                "Application unexpectedly exited while searching for number of errors label");

            Assert.IsNotNull(numberErrors, $"Element {automationId} could not be found.");

            Assert.AreEqual("Text", numberErrors.ClassName, $"Expected {automationId} to be a label");
            Assert.AreEqual("No errors", numberErrors.AsLabel().Text, "Expected no errors on startup");
        }

        public static void AssertLoadAasx(Application application, Window mainWindow, string path)
        {
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"The AASX file to be loaded does not exist: {path}");
            }

            var fileMenuItem = mainWindow
                .FindFirstDescendant(
                    cf => cf.ByClassName("MenuItem").And(cf.ByName("File")))
                .AsMenuItem();

            fileMenuItem.Click();

            var openMenuItem = fileMenuItem
                .FindFirstChild(cf => cf.ByName("Open .."))
                .AsMenuItem();

            openMenuItem.Click();

            Retry.WhileEmpty(
                () => mainWindow.ModalWindows,
                throwOnTimeout: true, timeout: TimeSpan.FromSeconds(10));

            Assert.AreEqual(1, mainWindow.ModalWindows.Length);

            var modal = mainWindow.ModalWindows[0];
            var pathCombo = modal.FindFirstChild(cf => cf.ByAutomationId("1148")).AsComboBox();

            pathCombo.EditableText = path;

            var openButton = modal.FindFirstChild(cf => cf.ByAutomationId("1")).AsButton();
            openButton.Click();

            Assert.IsEmpty(modal.ModalWindows,
                $"Unexpected modal window (probably an error) while opening the AASX: {path}");

            Retry.WhileTrue(() => mainWindow.ModalWindows.Length > 0,
                throwOnTimeout: true, timeout: TimeSpan.FromSeconds(10));

            if (application.HasExited)
                throw new AssertionException(
                    "The application unexpectedly exited. " +
                    $"Check manually why the file could not be opened: {path}");

            AssertNoErrors(application, mainWindow);
        }
    }
}