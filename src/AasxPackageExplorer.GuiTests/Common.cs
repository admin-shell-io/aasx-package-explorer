using System.Linq;
using FlaUI.Core.AutomationElements;
using NUnit.Framework; // necessary extension for AsLabel() and other methods
using Application = FlaUI.Core.Application;
using Assert = NUnit.Framework.Assert;
using AssertionException = NUnit.Framework.AssertionException;
using Directory = System.IO.Directory;
using Exception = System.Exception;
using File = System.IO.File;
using FileNotFoundException = System.IO.FileNotFoundException;
using InvalidOperationException = System.InvalidOperationException;
using Path = System.IO.Path;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using Regex = System.Text.RegularExpressions.Regex;
using Retry = FlaUI.Core.Tools.Retry;
using TimeSpan = System.TimeSpan;
using UIA3Automation = FlaUI.UIA3.UIA3Automation;
using Win32Exception = System.ComponentModel.Win32Exception;
using Window = FlaUI.Core.AutomationElements.Window;

namespace AasxPackageExplorer.GuiTests
{
    public class Run
    {
        public string[] Args = { "-splash", "0" };
        public bool DontKill = false;
    }

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
                throw new FileNotFoundException($"The sample AASX could not be found: {pth}");
            }

            return pth;
        }


        public delegate void Implementation(Application application, UIA3Automation automation, Window mainWindow);

        public static readonly string[] DefaultArgs = { "-splash", "0" };

        /// <summary>
        /// Finds the main AASX Package Explorer window and executes the code dependent on it.
        /// </summary>
        /// <remarks>This method is necessary since splash screen confuses FlaUI and prevents us from
        /// easily determining the main window.</remarks>
        /// <param name="implementation">Code to be executed</param>
        /// <param name="run">Run options. If null, a new run with default values is used</param>
        public static void RunWithMainWindow(Implementation implementation, Run? run = null)
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

            var resolvedRun = run ?? new Run();

            // See https://stackoverflow.com/questions/5510343/escape-command-line-arguments-in-c-sharp
            string joinedArgs = string.Join(
                " ",
                resolvedRun.Args
                    .Select(arg => Regex.Replace(arg, @"(\\*)" + "\"", @"$1$1\" + "\"")));

            var psi = new ProcessStartInfo
            {
                FileName = pathToExe,
                Arguments = joinedArgs,
                RedirectStandardError = true,
                WorkingDirectory = ".",
                UseShellExecute = false
            };

            bool gotStderr = false;

            var process = new Process { StartInfo = psi };
            try
            {
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        gotStderr = true;
                        TestContext.Error.WriteLine(e.Data);
                    }
                };

                process.Start();
                process.BeginErrorReadLine();
            }
            catch (Exception)
            {
                TestContext.Error.WriteLine(
                    $"Failed to launch the process: FileName: {psi.FileName}, " +
                    $"Arguments: {psi.Arguments}, Working directory: {psi.WorkingDirectory}");
                throw;
            }

            var app = new Application(process, false);

            try
            {
                using var automation = new UIA3Automation();

                var mainWindow = Retry.Find(() =>
                        // ReSharper disable once AccessToDisposedClosure
                        app.GetAllTopLevelWindows(automation)
                            .FirstOrDefault((w) => w.Title == "AASX Package Explorer"),
                    new RetrySettings
                    {
                        ThrowOnTimeout = true,
                        Timeout = TimeSpan.FromSeconds(5),
                        TimeoutMessage = "Could not find the main window"
                    }).AsWindow();

                implementation(app, automation, mainWindow);
            }
            finally
            {
                if (!resolvedRun.DontKill)
                {
                    app.Kill();
                }
            }

            if (gotStderr)
            {
                throw new AssertionException(
                    "Unexpected writes to standard error. Please see the test context for more detail.");
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
                throwOnTimeout: true, timeout: TimeSpan.FromSeconds(10),
                timeoutMessage: "Could not find the modal windows of the main window");

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
