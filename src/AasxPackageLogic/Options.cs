/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPackageLogic
{
    /// <summary>
    /// This attribute indicates, that it should e.g. serialized in JSON.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class SettableOption : System.Attribute
    {
        public string JsonName = null;
    }

    /// <summary>
    /// The Singleton for providing options.
    /// </summary>
    public static class Options
    {
        private static OptionsInformation instance = null;
        private static readonly object padlock = new object();

        /// <summary>
        /// The Singleton for Options
        /// </summary>
        public static OptionsInformation Curr
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new OptionsInformation();
                    }
                    return instance;
                }
            }
        }

        /// <summary>
        /// Instantaneously replaces the Options singleton instance with the data provided.
        /// </summary>
        /// <param name="io"></param>
        public static void ReplaceCurr(OptionsInformation io)
        {
            lock (padlock)
            {
                instance = io;
            }
        }
    }


    /// <summary>
    /// This class holds the command line options. An "Options" Singleton will provide an instance of it.
    /// </summary>
    public class OptionsInformation
    {
        /// <summary>
        /// This file shall be loaded at start of application
        /// </summary>
        [SettableOption]
        public string AasxToLoad = null;

        /// <summary>
        /// if not -1, the left of window
        /// </summary>
        public int WindowLeft = -1;

        /// <summary>
        /// if not -1, the top of window
        /// </summary>
        public int WindowTop = -1;

        /// <summary>
        /// if not -1, the width of window
        /// </summary>
        public int WindowWidth = -1;

        /// <summary>
        /// if not -1, the height of window
        /// </summary>
        public int WindowHeight = -1;

        /// <summary>
        /// if True, then maximize window on application startup
        /// </summary>
        public bool WindowMaximized = false;

        /// <summary>
        /// Template string for the id of an AAS. Could contain up to 16 placeholders of:
        /// D = decimal digit, X = hex digit, A = alphanumerical digit
        /// </summary>
        public string TemplateIdAas = "https://example.com/ids/aas/DDDD_DDDD_DDDD_DDDD";

        /// <summary>
        /// Template string for the id of an aaset. Could contain up to 16 placeholders of:
        /// D = decimal digit, X = hex digit, A = alphanumerical digit
        /// </summary>
        public string TemplateIdAsset = "https://example.com/ids/asset/DDDD_DDDD_DDDD_DDDD";

        /// <summary>
        /// Template string for the id of an submodel of kind instance. Could contain up to 16 placeholders of:
        /// D = decimal digit, X = hex digit, A = alphanumerical digit
        /// </summary>
        public string TemplateIdSubmodelInstance = "https://example.com/ids/sm/DDDD_DDDD_DDDD_DDDD";

        /// <summary>
        /// Template string for the id of an submodel of kind type. Could contain up to 16 placeholders of:
        /// D = decimal digit, X = hex digit, A = alphanumerical digit
        /// </summary>
        public string TemplateIdSubmodelTemplate = "https://example.com/ids/sm/DDDD_DDDD_DDDD_DDDD";

        /// <summary>
        /// Template string for the id of a concept description. Could contain up to 16 placeholders of:
        /// D = decimal digit, X = hex digit, A = alphanumerical digit
        /// </summary>
        public string TemplateIdConceptDescription = "https://example.com/ids/cd/DDDD_DDDD_DDDD_DDDD";

        /// <summary>
        /// Path to eCl@ss files
        /// </summary>
        public string EclassDir = null;

        /// <summary>
        /// Path to an image to be displayed as logo
        /// </summary>
        public string LogoFile = null;

        /// <summary>
        /// Path to JSON file defining qualifier presets.
        /// </summary>
        public string QualifiersFile = null;

        /// <summary>
        /// Path to a JSON, defining a set of AasxPackage-Files, which serve as repository
        /// </summary>
        public string AasxRepositoryFn = null;

        /// <summary>
        /// Home address of the content browser on startup, on change of AASX
        /// </summary>
        public string ContentHome = @"https://github.com/admin-shell/io/blob/master/README.md";

        /// <summary>
        /// If true, use transparent flyover dialogs, where possible
        /// </summary>
        public bool UseFlyovers = true;

        /// <summary>
        /// If other then -1, then time in ms for the splash window to stay on the screen.
        /// </summary>
        public int SplashTime = -1;

        /// <summary>
        /// If true, use always internal browser
        /// </summary>
        public bool InternalBrowser = false;

        /// <summary>
        /// If true, apply second search operation to join multi-language information.
        /// </summary>
        public bool EclassTwoPass = false;

        /// <summary>
        /// If not null, enables backing up XML files of the AAS-ENV in some files under BackupDir,
        /// which could be relative
        /// </summary>
        public string BackupDir = null;

        /// <summary>
        /// At max such much different files are used for backing up
        /// </summary>
        public int BackupFiles = 10;

        /// <summary>
        /// Load and store AASX files via temporary package to avoid corruptions.
        /// EXPERIMENTAL!
        /// </summary>
        public bool IndirectLoadSave = false;

        /// <summary>
        /// Hostname for the REST server. If other than "localhost", use of admin rights might be required.
        /// </summary>
        public string RestServerHost = "localhost";

        /// <summary>
        /// Port for the REST server. Port numbers below 1023 may not work.
        /// </summary>
        public string RestServerPort = "1111";

        /// <summary>
        /// If not null, will retrieved the options of all instantiated plugins and
        /// will write these into JSON option file
        /// </summary>
        public string WriteDefaultOptionsFN = null;

        public enum ColorNames
        {
            LightAccentColor = 0, DarkAccentColor, DarkestAccentColor, FocusErrorBrush, FocusErrorColor
        };

        /// <summary>
        /// Dictionary of override colors
        /// </summary>
        [SettableOption]
        public Dictionary<ColorNames, AnyUiColor> AccentColors =
            new Dictionary<ColorNames, AnyUiColor>();

        public AnyUiColor GetColor(ColorNames c)
        {
            if (AccentColors != null && AccentColors.ContainsKey(c))
                return AccentColors[c];
            return AnyUiColors.Black;
        }

        /// <summary>
        /// Contains a list of remarks. Intended use: disabling lines of preferences.
        /// </summary>
        public List<string> Remarks = new List<string>();

        /// <summary>
        /// If not null points to the dir, where plugins are (recursively) searched
        /// </summary>
        public string PluginDir = null;

        /// <summary>
        /// For such operations as query repository, do load a new AASX file without
        /// prompting the user.
        /// </summary>
        public bool LoadWithoutPrompt = false;

        /// <summary>
        /// Point to a list of SecureConnectPresets for the respective dialogue
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public Newtonsoft.Json.Linq.JToken SecureConnectPresets;

        public class PluginDllInfo
        {
            public string Path;
            public string[] Args;

            [JetBrains.Annotations.UsedImplicitly]
            public Newtonsoft.Json.Linq.JToken Options;

            [JetBrains.Annotations.UsedImplicitly]
            public Newtonsoft.Json.Linq.JToken DefaultOptions;

            public PluginDllInfo() { }

            public PluginDllInfo(string path, string[] args = null)
            {
                this.Path = path;
                if (args != null)
                    this.Args = args;
            }
        }

        /// <summary>
        /// Contains a list of tuples (filenames, args) of plugins to be loaded.
        /// </summary>
        [SettableOption]
        public List<PluginDllInfo> PluginDll = new List<PluginDllInfo>();

        public OptionsInformation()
        {
            AccentColors[ColorNames.LightAccentColor] = new AnyUiColor(0xFFCBD8EBu);
            AccentColors[ColorNames.DarkAccentColor] = new AnyUiColor(0xFF88A6D2u);
            AccentColors[ColorNames.DarkestAccentColor] = new AnyUiColor(0xFF4370B3u);
            AccentColors[ColorNames.FocusErrorBrush] = new AnyUiColor(0xFFD42044u);
            AccentColors[ColorNames.FocusErrorColor] = new AnyUiColor(0xFFD42044u);
        }

        /// <summary>
        /// Will save options to a file. Catches exceptions.
        /// </summary>
        public static void WriteJson(OptionsInformation optionsInformation, string filename)
        {
            // execute in-line, in order to represent to correct order to the human operator
            try
            {
                var jsonStr = JsonConvert.SerializeObject(optionsInformation, Formatting.Indented);
                File.WriteAllText(filename, jsonStr);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"When writing options to a JSON file: {filename}");
            }
        }

        /// <summary>
        /// Will read options from a file into the given instance.
        /// </summary>
        public static void ReadJson(string fn, OptionsInformation optionsInformation)
        {
            try
            {
                var jsonStr = File.ReadAllText(fn);
                JsonConvert.PopulateObject(jsonStr, optionsInformation);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "When reading options JSON file");
            }
        }

        public static void ParseArgs(string[] args, OptionsInformation optionsInformation)
        {
            // This is a sweep line for plugin arguments.
            var pluginArgs = new List<string>();

            for (int index = 0; index < args.Length; index++)
            {
                var arg = args[index].Trim().ToLower();
                var morearg = (args.Length - 1) - index;

                // flags
                if (arg == "-maximized")
                {
                    optionsInformation.WindowMaximized = true;
                    continue;
                }
                if (arg == "-noflyouts")
                {
                    optionsInformation.UseFlyovers = false;
                    continue;
                }
                if (arg == "-intbrowse")
                {
                    optionsInformation.InternalBrowser = true;
                    continue;
                }
                if (arg == "-twopass")
                {
                    optionsInformation.EclassTwoPass = true;
                    continue;
                }
                if (arg == "-indirect-load-save")
                {
                    optionsInformation.IndirectLoadSave = true;
                    continue;
                }
                if (arg == "-load-without-prompt")
                {
                    optionsInformation.LoadWithoutPrompt = true;
                    continue;
                }

                // commands, which are executed on the fly ..
                if (arg == "-read-json" && morearg > 0)
                {
                    // parse
                    var fn = System.IO.Path.GetFullPath(args[index + 1]);
                    index++;

                    // execute in-line, in order to represent to correct order to the human operator
                    OptionsInformation.ReadJson(fn, optionsInformation);

                    // next arg
                    continue;
                }
                if (arg == "-write-json" && morearg > 0)
                {
                    // parse
                    var filename = System.IO.Path.GetFullPath(args[index + 1]);
                    index++;

                    // do
                    OptionsInformation.WriteJson(optionsInformation, filename);

                    // next arg
                    continue;
                }

                // options
                if (arg == "-left" && morearg > 0)
                {
                    if (Int32.TryParse(args[index + 1], out int i))
                        optionsInformation.WindowLeft = i;
                    index++;
                    continue;
                }
                if (arg == "-top" && morearg > 0)
                {
                    if (Int32.TryParse(args[index + 1], out int i))
                        optionsInformation.WindowTop = i;
                    index++;
                    continue;
                }
                if (arg == "-width" && morearg > 0)
                {
                    if (Int32.TryParse(args[index + 1], out int i))
                        optionsInformation.WindowWidth = i;
                    index++;
                    continue;
                }
                if (arg == "-height" && morearg > 0)
                {
                    if (Int32.TryParse(args[index + 1], out int i))
                        optionsInformation.WindowHeight = i;
                    index++;
                    continue;
                }

                if (arg == "-id-aas" && morearg > 0)
                {
                    optionsInformation.TemplateIdAas = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-id-asset" && morearg > 0)
                {
                    optionsInformation.TemplateIdAsset = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-id-sm-template" && morearg > 0)
                {
                    optionsInformation.TemplateIdSubmodelTemplate = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-id-sm-instance" && morearg > 0)
                {
                    optionsInformation.TemplateIdSubmodelInstance = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-id-cd" && morearg > 0)
                {
                    optionsInformation.TemplateIdConceptDescription = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-eclass" && morearg > 0)
                {
                    optionsInformation.EclassDir = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-qualifiers" && morearg > 0)
                {
                    optionsInformation.QualifiersFile = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-logo" && morearg > 0)
                {
                    optionsInformation.LogoFile = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-aasxrepo" && morearg > 0)
                {
                    optionsInformation.AasxRepositoryFn = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-contenthome" && morearg > 0)
                {
                    optionsInformation.ContentHome = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-splash" && morearg > 0)
                {
                    if (Int32.TryParse(args[index + 1], out int i))
                        optionsInformation.SplashTime = i;
                    index++;
                    continue;
                }
                if (arg == "-options" && morearg > 0)
                {
                    string pathToOptions = args[index + 1];
                    Log.Singleton.Info(
                        $"Parsing options from a non-default options file: {pathToOptions}");
                    var fullFilename = System.IO.Path.GetFullPath(pathToOptions);
                    OptionsInformation.TryReadOptionsFile(fullFilename, optionsInformation);

                    index++;
                    continue;
                }
                if (arg == "-backupdir" && morearg > 0)
                {
                    optionsInformation.BackupDir = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-resthost" && morearg > 0)
                {
                    optionsInformation.RestServerHost = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-restport" && morearg > 0)
                {
                    optionsInformation.RestServerPort = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-rem" && morearg > 0)
                {
                    // Add one argument to the plugin list
                    optionsInformation.Remarks.Add(args[index + 1]);
                    index++;
                    continue;
                }
                if (arg == "-write-all-json" && morearg > 0)
                {
                    // will be executed very late!
                    optionsInformation.WriteDefaultOptionsFN = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-plugin-dir" && morearg > 0)
                {
                    optionsInformation.PluginDir = args[index + 1];
                    index++;
                    continue;
                }

                // Sweep-line options for plugins and DLL path
                if (arg == "-p" && morearg > 0)
                {
                    // Add exactly one following argument to the sweep line of plugin arguments
                    pluginArgs.Add(args[index + 1]);
                    index += 1;
                    continue;
                }
                if (arg == "-dll" && morearg > 0)
                {
                    // Process and reset the sweep line
                    optionsInformation.PluginDll.Add(
                        new PluginDllInfo(args[index + 1], pluginArgs.ToArray()));
                    pluginArgs.Clear();
                    index++;
                    continue;
                }

                // Colors
                {
                    var found = false;
                    for (int i = 0; i < 10; i++)
                        if (arg == $"-c{i:0}" && morearg > 0)
                        {
                            // ReSharper disable PossibleNullReferenceException
                            try
                            {
                                var c =AnyUiColor.FromString(args[index + 1].Trim());
                                optionsInformation.AccentColors.Add((ColorNames)i, c);
                            }
                            catch (Exception ex)
                            {
                                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                            }
                            // ReSharper enable PossibleNullReferenceException

                            index++;
                            found = true;
                        }
                    if (found)
                        continue;
                }

                // if come to this point and obviously not an option, take this as load argument
                // allow for more options to come (motivation: allow "-write-json options.json" to be the last argument)
                if (!arg.StartsWith("-"))
                {
                    if (System.IO.File.Exists(args[index]))
                        optionsInformation.AasxToLoad = args[index];
                }
            }
        }

        public static void TryReadOptionsFile(string filename, OptionsInformation optionsInformation)
        {
            try
            {
                var optionsTxt = File.ReadAllText(filename);
                var argsFromFile = optionsTxt.Split(
                    new[] { '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                OptionsInformation.ParseArgs(argsFromFile, optionsInformation);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "Reading options file: " + filename);
            }
        }

        #region // IdTemplates

        private Random MyRnd = new Random();

        public string GenerateIdAccordingTemplate(string tpl)
        {
            // generate a deterministic decimal digit string
            var decimals = String.Format("{0:ffffyyMMddHHmmss}", DateTime.UtcNow);
            decimals = new string(decimals.Reverse().ToArray());
            // convert this to an int
            if (!Int64.TryParse(decimals, out Int64 decii))
                decii = MyRnd.Next(Int32.MaxValue);
            // make an hex out of this
            string hexamals = decii.ToString("X");
            // make an alphanumeric string out of this
            string alphamals = "";
            var dii = decii;
            while (dii >= 1)
            {
                var m = dii % 26;
                alphamals += Convert.ToChar(65 + m);
                dii = dii / 26;
            }

            // now, "salt" the strings
            for (int i = 0; i < 32; i++)
            {
                var c = Convert.ToChar(48 + MyRnd.Next(10));
                decimals += c;
                hexamals += c;
                alphamals += c;
            }

            // now, can just use the template
            var id = "";
            foreach (var tpli in tpl)
            {
                if (tpli == 'D' && decimals.Length > 0)
                {
                    id += decimals[0];
                    decimals = decimals.Remove(0, 1);
                }
                else
                if (tpli == 'X' && hexamals.Length > 0)
                {
                    id += hexamals[0];
                    hexamals = hexamals.Remove(0, 1);
                }
                else
                if (tpli == 'A' && alphamals.Length > 0)
                {
                    id += alphamals[0];
                    alphamals = alphamals.Remove(0, 1);
                }
                else
                    id += tpli;
            }

            // ok
            return id;
        }

        #endregion
    }
}
