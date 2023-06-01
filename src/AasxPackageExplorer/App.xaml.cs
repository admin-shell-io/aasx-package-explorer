/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using AasxIntegrationBase;
using AasxPackageLogic;
using AdminShellNS;
using AnyUi;

// [assembly: System.Windows.Media.DisableDpiAwareness]

namespace AasxPackageExplorer
{
    public partial class App : Application
    {
        /// <summary>
        /// Infers application options based on the command-line arguments.
        /// </summary>
        /// <param name="exePath">path to AasxPackageExplorer.exe</param>
        /// <param name="args">command-line arguments</param>
        /// <returns>inferred options</returns>
        public static OptionsInformation InferOptions(string exePath, string[] args)
        {
            var optionsInformation = new OptionsInformation();

            // Load the default command-line options from a file with a conventional file name

            var pathToDefaultOptions = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(exePath),
                System.IO.Path.GetFileNameWithoutExtension(exePath) + ".options.json");

            Log.Singleton.Info(
                "The default options are expected in the JSON file: {0}", pathToDefaultOptions);
            if (File.Exists(pathToDefaultOptions))
            {
                Log.Singleton.Info(
                    "Loading the default options from: {0}", pathToDefaultOptions);
                OptionsInformation.ReadJson(pathToDefaultOptions, optionsInformation);
            }
            else
            {
                Log.Singleton.Info(
                    "The JSON file with the default options does not exist;" +
                    "no default options were loaded: {0}", pathToDefaultOptions);
            }

            // Cover the special case for having a single positional command-line option

            if (args.Length == 1 && !args[0].StartsWith("-"))
            {
                string directAasx = args[0];
                Log.Singleton.Info("Direct request to load AASX {0} ..", directAasx);
                optionsInformation.AasxToLoad = directAasx;
            }

            // Parse options from the command-line and execute the directives on the fly (such as parsing and
            // overruling given in the additional option files, *e.g.*, through "-read-json" and "-options")

            Log.Singleton.Info($"Parsing {args.Length} command-line option(s)...");

            for (var i = 0; i < args.Length; i++)
                Log.Singleton.Info($"Command-line option: {i}: {args[i]}");

            OptionsInformation.ParseArgs(args, optionsInformation);

            return optionsInformation;
        }

        public static Dictionary<string, Plugins.PluginInstance> LoadAndActivatePlugins(
            IReadOnlyList<OptionsInformation.PluginDllInfo> pluginDllInfos)
        {
            // Plugins to be loaded
            if (pluginDllInfos.Count == 0) return new Dictionary<string, Plugins.PluginInstance>();

            Log.Singleton.Info(
                $"Trying to load and activate {pluginDllInfos.Count} plug-in(s)...");
            var loadedPlugins = Plugins.TryActivatePlugins(pluginDllInfos);

            Plugins.TrySetOptionsForPlugins(pluginDllInfos, loadedPlugins);

            return loadedPlugins;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // MIHO: This does not work
            // WinPInvokeHelpers.SetProcessDPIAware(WinPInvokeHelpers.PROCESS_DPI_AWARENESS.Process_DPI_Unaware);

            // allow long term logging (for report box)
            Log.Singleton.EnableLongTermStore();

            // catch unhandled exceptions
            SetupExceptionHandling();

            // Build up of options
            Log.Singleton.Info("Application startup.");
            var exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            Options.ReplaceCurr(InferOptions(exePath, e.Args));

            // commit some options to other global locations
            AdminShellUtil.DefaultLngIso639 = AasxLanguageHelper.GetFirstLangCode(Options.Curr.DefaultLang) ?? "en?";

            // search for plugins?
            if (Options.Curr.PluginDir != null)
            {
                var searchDir = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(exePath),
                    Options.Curr.PluginDir);

                Log.Singleton.Info(
                    "Searching for the plugins in the plugin directory: {0}", searchDir);

                var pluginDllInfos = Plugins.TrySearchPlugins(searchDir);

                Log.Singleton.Info(
                    $"Found {pluginDllInfos.Count} plugin(s) in the plugin directory: {searchDir}");

                Options.Curr.PluginDll.AddRange(pluginDllInfos);
            }

            Log.Singleton.Info(
                $"Loading and activating {Options.Curr.PluginDll.Count} plugin(s)...");

            Plugins.LoadedPlugins = LoadAndActivatePlugins(Options.Curr.PluginDll);

            // at end, write all default options to JSON?
            if (Options.Curr.WriteDefaultOptionsFN != null)
            {
                // info
                var fullFilename = System.IO.Path.GetFullPath(Options.Curr.WriteDefaultOptionsFN);
                Log.Singleton.Info($"Writing resulting options to a JSON file: {fullFilename}");

                // retrieve
                Plugins.TryGetDefaultOptionsForPlugins(Options.Curr.PluginDll, Plugins.LoadedPlugins);
                OptionsInformation.WriteJson(Options.Curr, fullFilename);
            }

            // colors
            if (true)
            {
                var resNames = new[] {
                    "LightAccentColor", "DarkAccentColor", "DarkestAccentColor", "FocusErrorBrush" };
                for (int i = 0; i < resNames.Length; i++)
                {
                    var x = this.FindResource(resNames[i]);
                    if (x != null
                        && x is System.Windows.Media.SolidColorBrush
                        && Options.Curr.AccentColors.ContainsKey((OptionsInformation.ColorNames)i))
                        this.Resources[resNames[i]] = AnyUiDisplayContextWpf.GetWpfBrush(
                            Options.Curr.GetColor((OptionsInformation.ColorNames)i));
                }
                resNames = new[] { "FocusErrorColor" };
                for (int i = 0; i < resNames.Length; i++)
                {
                    var x = this.FindResource(resNames[i]);
                    if (x != null
                        && x is System.Windows.Media.Color
                        && Options.Curr.AccentColors.ContainsKey((OptionsInformation.ColorNames)(3 + i)))
                        this.Resources[resNames[i]] = AnyUiDisplayContextWpf.GetWpfColor(
                            Options.Curr.GetColor((OptionsInformation.ColorNames)(3 + i)));
                }
            }

            // preferences
            Pref pref = Pref.Read();

            // show splash (required for licenses of open source)
            if (Options.Curr.SplashTime != 0)
            {
                var splash = new CustomSplashScreenNew(pref);
                splash.Show();
            }

            // show main window
            MainWindow wnd = new MainWindow(pref);
            wnd.Show();
        }

        // see: https://stackoverflow.com/questions/793100/globally-catch-exceptions-in-a-wpf-application

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName =
                    System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                Log.Singleton.Error(exception, message);
            }
        }
    }
}
