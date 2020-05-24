using AasxIntegrationBase;
using AdminShellNS;
using AasxGlobalLogging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). 
The Grapevine REST server framework is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

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
        /// Instantanously replaces the Options singleton instance with the data provided.
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
        public string PrefLicenseShort = "This software is licensed under the Eclipse Public License 2.0 (EPL-2.0)." + Environment.NewLine +
                "The browser functionality is licensed under the cefSharp license." + Environment.NewLine +
                "The Newtonsoft.JSON serialization is licensed under the MIT License (MIT)." + Environment.NewLine +
                "The QR code generation is licensed under the MIT license (MIT)." + Environment.NewLine +
                "The Zxing.Net Dot Matrix Code (DMC) generation is licensed under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
                "The Grapevine REST server framework is licensed under Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
                "The AutomationML.Engine is licensed under the MIT license (MIT).";

        /// <summary>
        /// The last build date of the application. Based on a resource file. Use of Options as singleton.
        /// </summary>
        [JsonIgnore]
        public string PrefBuildDate
        {
            get
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AasxWpfControlLibrary.Resources.BuildDate.txt"))
                {
                    if (stream != null)
                    {
                        TextReader tr = new StreamReader(stream);
                        string fileContents = tr.ReadToEnd();
                        if (fileContents.Length > 20)
                            fileContents = fileContents.Substring(0, 20) + "..";
                        return (fileContents);
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
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AasxWpfControlLibrary.Resources.LICENSE.txt"))
                {
                    if (stream != null)
                    {
                        TextReader tr = new StreamReader(stream);
                        string fileContents = tr.ReadToEnd();
                        return (fileContents);
                    }
                }
                return "";
            }
        }

        /// <summary>
        /// The current version string of the application. Use of Options as singleton.
        /// </summary>
        [JsonIgnore]
        public string PrefVersion = "1.9.8.2";

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
        public string TemplateIdAas = "www.company.com/ids/aas/DDDD_DDDD_DDDD_DDDD";

        /// <summary>
        /// Template string for the id of an aaset. Could contain up to 16 placeholders of:
        /// D = decimal digit, X = hex digit, A = alphanumerical digit
        /// </summary>
        public string TemplateIdAsset = "www.company.com/ids/asset/DDDD_DDDD_DDDD_DDDD";

        /// <summary>
        /// Template string for the id of an submodel of kind instance. Could contain up to 16 placeholders of:
        /// D = decimal digit, X = hex digit, A = alphanumerical digit
        /// </summary>
        public string TemplateIdSubmodelInstance = "www.company.com/ids/sm/DDDD_DDDD_DDDD_DDDD";

        /// <summary>
        /// Template string for the id of an submodel of kind type. Could contain up to 16 placeholders of:
        /// D = decimal digit, X = hex digit, A = alphanumerical digit
        /// </summary>
        public string TemplateIdSubmodelTemplate = "www.company.com/ids/sm/DDDD_DDDD_DDDD_DDDD";

        /// <summary>
        /// Template string for the id of a concept description. Could contain up to 16 placeholders of:
        /// D = decimal digit, X = hex digit, A = alphanumerical digit
        /// </summary>
        public string TemplateIdConceptDescription = "www.company.com/ids/cd/DDDD_DDDD_DDDD_DDDD";

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
        /// If not null, enables backing up XML files of the AAS-ENV in some files under BackupDir, which could be relative
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
        /// If not null, will retrieved the options of all instantiated plugins and will write these into JSON option file
        /// </summary>
        [JsonIgnore]
        public string WriteDefaultOptionsFN = null;

        /// <summary>
        /// Dictionary of override colors 
        /// </summary>
        [SettableOption]
        public Dictionary<int, System.Windows.Media.Color> AccentColors = new Dictionary<int, System.Windows.Media.Color>();

        /// <summary>
        /// Contains a list of remarks. Intended use: disabling lines of preferences.
        /// </summary>
        public List<string> Remarks = new List<string>();

        /// <summary>
        /// If not null points to the dir, where plugins are (recursively) searched
        /// </summary>
        public string PluginDir = null;

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
        public void WriteJson(string fn)
        {
            // execute in-line, in order to represent to correct order to the human operator
            try
            {
                var jsonStr = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(fn, jsonStr);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"When writing options JSON file {fn}");
            }
        }

        /// <summary>
        /// Will read options from a file into this instance.
        /// </summary>
        public void ReadJson(string fn)
        {
            try
            {
                var jsonStr = File.ReadAllText(fn);
                JsonConvert.PopulateObject(jsonStr, this);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When reading options JSON file");
            }
        }

        /// <summary>
        /// Parse given commandline arguments.
        /// </summary>
        /// <param name="args"></param>
        public void ParseArgs(string[] args)
        {
            for (int index = 0; index < args.Length; index++)
            {
                var arg = args[index].Trim().ToLower();
                var morearg = (args.Length - 1) - index;

                // flags
                if (arg == "-maximized")
                {
                    WindowMaximized = true;
                    continue;
                }
                if (arg == "-noflyouts")
                {
                    UseFlyovers = false;
                    continue;
                }
                if (arg == "-intbrowse")
                {
                    InternalBrowser = true;
                    continue;
                }
                if (arg == "-twopass")
                {
                    EclassTwoPass = true;
                    continue;
                }
                if (arg == "-indirect-load-save")
                {
                    IndirectLoadSave = true;
                    continue;
                }

                // commands, which are executed on the fly ..
                if (arg == "-read-json" && morearg > 0)
                {
                    // parse
                    var fn = System.IO.Path.GetFullPath(args[index + 1]);
                    index++;

                    // execute in-line, in order to represent to correct order to the human operator
                    this.ReadJson(fn);

                    // next arg
                    continue;
                }
                if (arg == "-write-json" && morearg > 0)
                {
                    // parse
                    var fn = System.IO.Path.GetFullPath(args[index + 1]);
                    index++;

                    // do
                    WriteJson(fn);

                    // next arg
                    continue;
                }

                // options
                if (arg == "-left" && morearg > 0)
                {
                    if (Int32.TryParse(args[index + 1], out int i))
                        WindowLeft = i;
                    index++;
                    continue;
                }
                if (arg == "-top" && morearg > 0)
                {
                    if (Int32.TryParse(args[index + 1], out int i))
                        WindowTop = i;
                    index++;
                    continue;
                }
                if (arg == "-width" && morearg > 0)
                {
                    if (Int32.TryParse(args[index + 1], out int i))
                        WindowWidth = i;
                    index++;
                    continue;
                }
                if (arg == "-height" && morearg > 0)
                {
                    if (Int32.TryParse(args[index + 1], out int i))
                        WindowHeight = i;
                    index++;
                    continue;
                }

                if (arg == "-id-aas" && morearg > 0)
                {
                    TemplateIdAas = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-id-asset" && morearg > 0)
                {
                    TemplateIdAsset = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-id-sm-template" && morearg > 0)
                {
                    TemplateIdSubmodelTemplate = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-id-sm-instance" && morearg > 0)
                {
                    TemplateIdSubmodelInstance = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-id-cd" && morearg > 0)
                {
                    TemplateIdConceptDescription = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-eclass" && morearg > 0)
                {
                    EclassDir = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-qualifiers" && morearg > 0)
                {
                    QualifiersFile = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-logo" && morearg > 0)
                {
                    LogoFile = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-aasxrepo" && morearg > 0)
                {
                    AasxRepositoryFn = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-contenthome" && morearg > 0)
                {
                    ContentHome = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-splash" && morearg > 0)
                {
                    if (Int32.TryParse(args[index + 1], out int i))
                        SplashTime = i;
                    index++;
                    continue;
                }
                if (arg == "-options" && morearg > 0)
                {
                    OptionsTextFn = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-backupdir" && morearg > 0)
                {
                    BackupDir = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-resthost" && morearg > 0)
                {
                    RestServerHost = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-restport" && morearg > 0)
                {
                    RestServerPort = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-rem" && morearg > 0)
                {
                    // Add one argument to the plugin list
                    Remarks.Add(args[index + 1]);
                    index++;
                    continue;
                }
                if (arg == "-write-all-json" && morearg > 0)
                {
                    // will be executed very late!
                    WriteDefaultOptionsFN = args[index + 1];
                    index++;
                    continue;
                }
                if (arg == "-plugin-dir" && morearg > 0)
                {
                    PluginDir = args[index + 1];
                    index++;
                    continue;
                }

                // (temporary) options for plugins and DLL path
                if (arg == "-p" && morearg > 0)
                {
                    // Add exactly one following argument to the plugin list
                    PluginArgs.Add(args[index + 1]);
                    index += 1;
                    continue;
                }
                if (arg == "-dll" && morearg > 0)
                {
                    PluginDll.Add(new PluginDllInfo(args[index + 1], PluginArgs.ToArray()));
                    PluginArgs.Clear();
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
                                AccentColors.Add(i, c);
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
                        AasxToLoad = args[index];
                }
            }
        }

        public void TryReadOptionsFile(string fn)
        {
            try
            {
                var optionsTxt = File.ReadAllText(fn);
                var options = optionsTxt.Split(new [] { '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                ParseArgs(options);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Reading options file: " + fn);
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

        #region // Reflection

        //public ExpandoObject ToExpandoObject()
        //{
        //    System.Attribute[] attrs = System.Attribute.GetCustomAttributes(typeof(Options));

        //    var expando = new ExpandoObject();
        //    var expandoDict = expando as IDictionary<string, object>;

        //    var t = typeof(Options);
        //    var l = t.GetFields(BindingFlags.Static | BindingFlags.Public);
        //    foreach (var f in l)
        //        foreach (var a in f.GetCustomAttributes<SettableOption>())
        //        {
        //            if (f.FieldType.IsPrimitive || f.FieldType == typeof(string))
        //            {
        //                // simply clone
        //                expandoDict[f.Name] = f.GetValue(null);
        //            }
        //            else
        //            {
        //                // do a JSON clone
        //                var json = JsonConvert.SerializeObject(f.GetValue(null));
        //                expandoDict[f.Name] = JsonConvert.DeserializeObject(json, f.FieldType);
        //            }

        //        }

        //    string outputJson = JsonConvert.SerializeObject(expando, Formatting.Indented);

        //    var x = JsonConvert.DeserializeObject(outputJson);

        //    return null;
        //}

        #endregion
    }
}
