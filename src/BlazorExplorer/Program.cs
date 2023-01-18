/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using BlazorUI.Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BlazorExplorer;

namespace BlazorExplorer
{
    public class Program
    {

        public static event EventHandler NewDataAvailable;

        public static PackageContainerListBase Repo;

        public static bool DisableEdit = false;

        public class NewDataAvailableArgs : EventArgs
        {
            public int signalNewDataMode;
            public int signalSessionNumber;
            public AnyUiLambdaActionBase signalNewLambdaAction;
            public AasxPluginResultEventBase signalNewPluginResultEvent;
            public bool onlyUpdateAasxPanel;

            public NewDataAvailableArgs(int mode = 2, int sessionNumber = 0,
                AnyUiLambdaActionBase newLambdaAction = null,
                AasxPluginResultEventBase newPluginResultEvent = null,
                bool onlyUpdatePanel = false)
            {
            }
        }

        public static void signalNewData(int mode, int sessionNumber = 0,
            AnyUiLambdaActionBase newLambdaAction = null,
            AasxPluginResultEventBase newPluginResultEvent = null,
            bool onlyUpdateAasxPanel = false)
        {
            ;
        }

        public static void EvalSetValueLambdaAndHandleReturn(
            int sessionNumber, AnyUiUIElement elem, object value = null)
        {
            ;
        }

        public static void loadAasx(BlazorSession bi, string value)
        {

        }

        public static void loadAasxFiles(BlazorSession bi, bool load = true)
        {

        }

        public static async Task getAasxAsync(BlazorSession bi, string input)
        {

        }


        // Copy of PackageExplorer
        // TODO: Refactor
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

        // Copy of PackageExplorer
        // TODO: Refactor
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

        // Almost copy of PackageExplorer
        // TODO: Refactor
        public static void Main(string[] args)
        {

            // allow long term logging (for report box)
            Log.Singleton.EnableLongTermStore();

            // Build up of options
            Log.Singleton.Info("Application startup.");
            var exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            Options.ReplaceCurr(InferOptions(exePath, args));

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

            // TODO: Prefs required? Because of OSS?

            // "Start" window
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<BlazorUI.Startup>();
                });
    }
}
