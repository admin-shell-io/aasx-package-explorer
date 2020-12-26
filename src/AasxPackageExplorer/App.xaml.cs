/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxWpfControlLibrary;
using System.Collections.Generic;
using System.IO;
using System.Windows;


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

            AasxPackageExplorer.Log.Singleton.Info(
                "The default options are expected in the JSON file: {0}", pathToDefaultOptions);
            if (File.Exists(pathToDefaultOptions))
            {
                AasxPackageExplorer.Log.Singleton.Info(
                    "Loading the default options from: {0}", pathToDefaultOptions);
                OptionsInformation.ReadJson(pathToDefaultOptions, optionsInformation);
            }
            else
            {
                AasxPackageExplorer.Log.Singleton.Info(
                    "The JSON file with the default options does not exist;" +
                    "no default options were loaded: {0}", pathToDefaultOptions);
            }

            // Cover the special case for having a single positional command-line option

            if (args.Length == 1 && !args[0].StartsWith("-"))
            {
                string directAasx = args[0];
                AasxPackageExplorer.Log.Singleton.Info("Direct request to load AASX {0} ..", directAasx);
                optionsInformation.AasxToLoad = directAasx;
            }

            // Parse options from the command-line and execute the directives on the fly (such as parsing and
            // overruling given in the additional option files, *e.g.*, through "-read-json" and "-options")

            AasxPackageExplorer.Log.Singleton.Info($"Parsing {args.Length} command-line option(s)...");

            for (var i = 0; i < args.Length; i++)
                AasxPackageExplorer.Log.Singleton.Info($"Command-line option: {i}: {args[i]}");

            OptionsInformation.ParseArgs(args, optionsInformation);

            return optionsInformation;
        }

        public static Dictionary<string, Plugins.PluginInstance> LoadAndActivatePlugins(
            IReadOnlyList<OptionsInformation.PluginDllInfo> pluginDllInfos)
        {
            // Plugins to be loaded
            if (pluginDllInfos.Count == 0) return new Dictionary<string, Plugins.PluginInstance>();

            AasxPackageExplorer.Log.Singleton.Info(
                $"Trying to load and activate {pluginDllInfos.Count} plug-in(s)...");
            var loadedPlugins = Plugins.TryActivatePlugins(pluginDllInfos);

            Plugins.TrySetOptionsForPlugins(pluginDllInfos, loadedPlugins);

            return loadedPlugins;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // allow long term logging (for report box)
            AasxPackageExplorer.Log.Singleton.EnableLongTermStore();

            // Build up of options
            AasxPackageExplorer.Log.Singleton.Info("Application startup.");

            var exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;

            Options.ReplaceCurr(InferOptions(exePath, e.Args));

            // search for plugins?
            if (Options.Curr.PluginDir != null)
            {
                var searchDir = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(exePath),
                    Options.Curr.PluginDir);

                AasxPackageExplorer.Log.Singleton.Info(
                    "Searching for the plugins in the plugin directory: {0}", searchDir);

                var pluginDllInfos = Plugins.TrySearchPlugins(searchDir);

                AasxPackageExplorer.Log.Singleton.Info(
                    $"Found {pluginDllInfos.Count} plugin(s) in the plugin directory: {searchDir}");

                Options.Curr.PluginDll.AddRange(pluginDllInfos);
            }

            AasxPackageExplorer.Log.Singleton.Info(
                $"Loading and activating {Options.Curr.PluginDll.Count} plugin(s)...");

            Plugins.LoadedPlugins = LoadAndActivatePlugins(Options.Curr.PluginDll);

            // at end, write all default options to JSON?
            if (Options.Curr.WriteDefaultOptionsFN != null)
            {
                // info
                var fullFilename = System.IO.Path.GetFullPath(Options.Curr.WriteDefaultOptionsFN);
                AasxPackageExplorer.Log.Singleton.Info($"Writing resulting options to a JSON file: {fullFilename}");

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
                    if (x != null &&
                        x is System.Windows.Media.SolidColorBrush && Options.Curr.AccentColors.ContainsKey(i))
                        this.Resources[resNames[i]] = new System.Windows.Media.SolidColorBrush(
                            Options.Curr.AccentColors[i]);
                }
                resNames = new[] { "FocusErrorColor" };
                for (int i = 0; i < resNames.Length; i++)
                {
                    var x = this.FindResource(resNames[i]);
                    if (x != null && x is System.Windows.Media.Color && Options.Curr.AccentColors.ContainsKey(3 + i))
                        this.Resources[resNames[i]] = Options.Curr.AccentColors[3 + i];
                }
            }

            Test();

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

        public void Test()
        {
            var pc = new PackageCentral();
            pc.MainContainer.Close();
            pc.MainContainer.Load("A01.aasx", loadResident: true); ;
            pc.MainContainer.Close();
            pc.MainContainer.Load("A01.aasx", loadResident: true); ;
            pc.MainContainer.SaveAs("A02.aasx");
            pc.MainContainer.Close();
        }
    }
}
