/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using AasxGlobalLogging;
using AasxIntegrationBase;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPackageExplorer
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
        /// The authors of the application. Use of Options as singleton.
        /// </summary>
        [JsonIgnore]
        public string PrefAuthors = "Michael Hoffmeister, Andreas Orzelski and further";

        /// <summary>
        /// The current (used) licenses of the application. Use of Options as singleton.
        /// </summary>
        [JsonIgnore]
        public string PrefLicenseShort =
            "This software is licensed under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
            "The Newtonsoft.JSON serialization is licensed under the MIT License (MIT)." + Environment.NewLine +
            "The QR code generation is licensed under the MIT license (MIT)." + Environment.NewLine +
            "The Zxing.Net Dot Matrix Code (DMC) generation is licensed " +
            "under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
            "The Grapevine REST server framework is licensed " +
            "under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
            "The AutomationML.Engine is licensed under the MIT license (MIT)." +
            "The MQTT server and client is licensed " +
            "under the MIT license (MIT)." + Environment.NewLine +
            "The IdentityModel OpenID client is licensed " +
            "under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
            "The jose-jwt object signing and encryption is licensed " +
            "under the MIT license (MIT).";

        /// <summary>
        /// The last build date of the application. Based on a resource file. Use of Options as singleton.
        /// </summary>
        [JsonIgnore]
        public string PrefBuildDate
        {
            get
            {
                using (var stream =
                    Assembly
                        .GetExecutingAssembly()
                        .GetManifestResourceStream("AasxWpfControlLibrary.Resources.BuildDate.txt"))
                {
                    if (stream != null)
                    {
                        TextReader tr = new StreamReader(stream);
                        string fileContents = tr.ReadToEnd();
                        if (fileContents.Length > 20)
                            fileContents = fileContents.Substring(0, 20) + "..";
                        return (fileContents.Trim());
                    }
                }
                return "";
            }
        }

        /// <summary>
        /// The license texts of the application. Based on a resource file. Use of Options as singleton.
        /// </summary>
        [JsonIgnore]
        public string PrefLicenseLong
        {
            get
            {
                return AasxPluginHelper.LoadLicenseTxtFromAssemblyDir("LICENSE.txt", Assembly.GetEntryAssembly());
            }
        }

        /// <summary>
        /// The current version string of the application. Use of Options as singleton.
        /// Note: in the past, there was a semantic version such as "1.9.8.3", but
        /// this was not maintained properly. Now, a version is derived from the
        /// build data with the intention, that the according tag in Github-Releases
        /// will be identical.
        /// </summary>
        [JsonIgnore]
        public string PrefVersion
        {
            get
            {
                var bdate = "" + PrefBuildDate;
                var version = "(not available)";

                // %date% in European format (e.g. during development)
                var m = Regex.Match(bdate, @"(\d+)\.(\d+)\.(\d+)");
                if (m.Success && m.Groups.Count >= 4)
                    version = "v" + ((m.Groups[3].Value.Length == 2) ? "20" : "")
                        + m.Groups[3].Value + "-"
                        + m.Groups[2].Value + "-"
                        + m.Groups[1].Value;

                // %date% in US local (e.g. from continous integration from Github)
                m = Regex.Match(bdate, @"(\d+)\/(\d+)\/(\d+)");
                if (m.Success && m.Groups.Count >= 4)
                    version = "v" + ((m.Groups[3].Value.Length == 2) ? "20" : "")
                        + m.Groups[3].Value + "-"
                        + m.Groups[1].Value + "-"
                        + m.Groups[2].Value;

                return version;
            }
        }

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
        /// If not null, read a text file containing the options
        /// </summary>
        [JsonIgnore]
        public string OptionsTextFn = null;

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

        /// <summary>
        /// Dictionary of override colors
        /// </summary>
        [SettableOption]
        public Dictionary<int, System.Windows.Media.Color> AccentColors =
            new Dictionary<int, System.Windows.Media.Color>();

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
        /// prmpting the user.
        /// </summary>
        public bool LoadWithoutPrompt = false;

        /// <summary>
        /// Point to a list of SecureConnectPresets for the respective dialogie
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public Newtonsoft.Json.Linq.JToken SecureConnectPresets;

        /// <summary>
        /// Contains a list of strings which shall be handled over to plugins.
        /// New: only for internal use, will be found in the DllInfos
        /// </summary>
        [JsonIgnore]
        private List<string> PluginArgs = new List<string>();

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
                Log.Error(ex, $"When writing options to a JSON file: {filename}");
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
                Log.Error(ex, "When reading options JSON file");
            }
        }

        public static void ParseArgs(string[] args, OptionsInformation optionsInformation)
        {
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
                    optionsInformation.OptionsTextFn = args[index + 1];
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

                // (temporary) options for plugins and DLL path
                if (arg == "-p" && morearg > 0)
                {
                    // Add exactly one following argument to the plugin list
                    optionsInformation.PluginArgs.Add(args[index + 1]);
                    index += 1;
                    continue;
                }
                if (arg == "-dll" && morearg > 0)
                {
                    optionsInformation.PluginDll.Add(
                        new PluginDllInfo(args[index + 1], optionsInformation.PluginArgs.ToArray()));
                    optionsInformation.PluginArgs.Clear();
                    index++;
                    continue;
                }

                // Colors
                {
                    var found = false;
                    for (int i = 0; i < 10; i++)
                        if (arg == $"-c{i:0}" && morearg > 0)
                        {
                            // ReSharper disable EmptyGeneralCatchClause
                            // ReSharper disable PossibleNullReferenceException
                            try
                            {
                                var c = (Color)ColorConverter.ConvertFromString(args[index + 1]);
                                optionsInformation.AccentColors.Add(i, c);
                            }
                            catch { }
                            // ReSharper enable EmptyGeneralCatchClause
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
                Log.Error(ex, "Reading options file: " + filename);
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
