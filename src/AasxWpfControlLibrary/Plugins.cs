using AasxIntegrationBase;
using AdminShellNS;
using AasxGlobalLogging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The Newtonsoft.JSON serialization is licensed under the MIT License (MIT).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxPackageExplorer
{
    /// <summary>
    /// This class holds all loaded plug-ins. It implements a singleton.
    /// </summary>
    public static class Plugins
    {
        public class PluginInstance
        {
            public int SourceIndex = -1;
            public Assembly asm = null;
            public Type plugType = null;
            public IAasxPluginInterface plugObj = null;
            public string[] args = null;

            public string name = "";
            public AasxPluginActionDescriptionBase[] actions = new AasxPluginActionDescriptionBase[] { };

            public PluginInstance()
            { }

            public PluginInstance(int sourceIndex, Assembly asm, Type plugType, IAasxPluginInterface plugObj, string[] args)
            {
                this.SourceIndex = sourceIndex;
                this.asm = asm;
                this.plugType = plugType;
                this.plugObj = plugObj;
                this.args = args;
            }

            public object BasicInvokeMethod(string mname, params object[] args)
            {
                var mi = plugType.GetMethod(mname);
                if (mi == null)
                    return null;
                return mi.Invoke(plugObj, args);
            }

            public AasxPluginActionDescriptionBase[] ListActions()
            {
                // ReSharper disable RedundantExplicitParamsArrayCreation
                var res = this.BasicInvokeMethod("ListActions", new object[] { }) as AasxPluginActionDescriptionBase[];
                // ReSharper enable RedundantExplicitParamsArrayCreation
                return res;
            }

            public string GetName()
            {
                // ReSharper disable RedundantExplicitParamsArrayCreation
                return (string)this.BasicInvokeMethod("GetPluginName", new object[] { });
                // ReSharper enable RedundantExplicitParamsArrayCreation
            }

            public static PluginInstance CreateNew(int sourceIndex, Assembly asm, Type plugType, IAasxPluginInterface plugObj, string[] args)
            {
                var pi = new PluginInstance(sourceIndex, asm, plugType, plugObj, args);
                pi.name = pi.GetName();
                pi.actions = pi.ListActions();
                if (pi.name == null || pi.actions == null || pi.actions.Length < 1)
                    return null;
                return pi;
            }

            public AasxPluginActionDescriptionBase FindAction(string name)
            {
                if (actions == null || actions.Length < 1)
                    return null;
                foreach (var a in this.actions)
                    if (a.name.Trim().ToLower() == name.Trim().ToLower())
                        return a;
                return null;
            }

            public bool HasAction(string name)
            {
                return this.FindAction(name) != null;
            }

            public object CheckForLogMessage()
            {
                // ReSharper disable RedundantExplicitParamsArrayCreation
                return this.BasicInvokeMethod("CheckForLogMessage", new object[] { });
                // ReSharper enable RedundantExplicitParamsArrayCreation
            }

            public object InvokeAction(string name, params object[] args)
            {
                var a = this.FindAction(name);
                if (a == null)
                    return null;
                return this.BasicInvokeMethod("ActivateAction", name, args);
            }
        }

        public static Dictionary<string, PluginInstance> LoadedPlugins = new Dictionary<string, PluginInstance>();

        public static PluginInstance FindPluginInstance(string pname)
        {
            if (LoadedPlugins == null || !LoadedPlugins.ContainsKey(pname))
                return null;
            return LoadedPlugins[pname];
        }

        public static void TrySearchPlugins(string searchDir, List<OptionsInformation.PluginDllInfo> infos)
        {
            // access
            if (!Directory.Exists(searchDir) || infos == null)
                return;

            // try get files
            // see: https://stackoverflow.com/questions/9830069/searching-for-file-in-directories-recursively/9830116
            foreach (string tagFn in Directory.EnumerateFiles(searchDir, "*.plugin", SearchOption.AllDirectories))
            {
                // deduce .dll name
                var dllPath = Path.Combine(
                        Path.GetDirectoryName(tagFn),
                        Path.GetFileNameWithoutExtension(tagFn) + ".dll");

                // present?
                if (File.Exists(dllPath))
                {
                    var pi = new OptionsInformation.PluginDllInfo(dllPath);
                    infos.Add(pi);
                }
            }
        }

        public static void TryActivatePlugins(List<OptionsInformation.PluginDllInfo> pluginDll)
        {
            for (int index = 0; index < pluginDll.Count; index++)
                try
                {
                    Log.Info("Trying load .dll at {0}", pluginDll[index].Path);

                    // make full path
                    var fullfn = System.IO.Path.GetFullPath(pluginDll[index].Path);

                    // Note: use LoadFrom instead of LoadFile, insane:
                    // https://stackoverflow.com/questions/36075829/assembly-loadfile-look-dependencies-in-location-of-executeable
                    var asm = Assembly.LoadFrom(fullfn);

                    var tp = asm.GetType("AasxIntegrationBase.AasxPlugin");
                    if (tp == null)
                    {
                        Log.Error("Cannot find class AasxIntegrationBase.AasxPlugin within .dll.");
                        continue;
                    }

                    // create instance using late binding
                    IAasxPluginInterface ob = (IAasxPluginInterface)Activator.CreateInstance(tp);
                    if (ob == null)
                    {
                        Log.Error("Cannot create instance from class AasxIntegrationBase.AasxPlugin within .dll.");
                        continue;
                    }

                    // create plugin
                    var pi = PluginInstance.CreateNew(index, asm, tp, ob, pluginDll[index].Args);
                    if (pi == null)
                    {
                        Log.Error("Cannot invoke methods within instance from class AasxIntegrationBase.AasxPlugin within .dll.");
                        continue;
                    }

                    // init plug-in
                    var singleArg = new object[] { pluginDll[index].Args };
                    pi.BasicInvokeMethod("InitPlugin", singleArg);

                    // adding
                    Log.Info(".. adding plugin {0}", pi.name);
                    LoadedPlugins.Add(pi.name, pi);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Trying activate plugin index {index}");
                }
        }

        public static AasxPluginResultLicense CompileAllLicenses()
        {
            // make an empty one
            var res = new AasxPluginResultLicense();
            res.shortLicense = "";
            res.longLicense = "";

            // over all loaded plugins
            foreach (var pi in LoadedPlugins.Values)
            {
                // ReSharper disable EmptyGeneralCatchClause
                try
                {
                    var x = pi.InvokeAction("get-licenses") as AasxPluginResultLicense;
                    if (x != null)
                    {
                        res.shortLicense += x.shortLicense + "\n";
                        res.longLicense += x.longLicense + "\n\n";
                    }
                }
                catch { }
                // ReSharper enable EmptyGeneralCatchClause
            }

            // OK
            return res;
        }

        /// <summary>
        /// Execute lambda for all loaded plugins and correlate with source plugin-dll-information. Returns a list of results of the lambda.
        /// </summary>
        public static List<object> TryForAllLoadedPlugins(OptionsInformation opt, string exceptionWhere, Func<OptionsInformation.PluginDllInfo, PluginInstance, object> lambda)
        {
            // access
            var res = new List<object>();
            if (opt == null || lambda == null)
                return res;

            // try to find matching plugins according to options
            for (int sourceIndex = 0; sourceIndex < opt.PluginDll.Count; sourceIndex++)
            {
                // options
                var dllinfo = opt.PluginDll[sourceIndex];

                // loaded plug in?
                PluginInstance piFound = null;
                foreach (var lpi in LoadedPlugins.Values)
                    if (lpi.SourceIndex == sourceIndex)
                        piFound = lpi;

                // yes?
                if (piFound == null)
                    continue;

                // yes!
                try
                {
                    var res2 = lambda(dllinfo, piFound);
                    res.Add(res2);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, exceptionWhere);
                }
            }

            // OK
            return res;
        }

        /// <summary>
        /// Takes the <c>opt</c> and tries to write them to the associated plugin.
        /// </summary>
        public static void TrGetDefaultOptionsForPlugins(OptionsInformation opt)
        {
            TryForAllLoadedPlugins(opt, "Trying get json options from Plugins", (dllinfo, lpi) =>
            {
                var popt = lpi.InvokeAction("get-json-options") as AasxPluginResultBaseObject;
                if (popt != null && popt.obj != null && popt.obj is string)
                    dllinfo.DefaultOptions = Newtonsoft.Json.Linq.JValue.Parse((popt.obj as string)); ;

                return true;
            });
        }

        /// <summary>
        /// Takes the <c>opt</c> and tries to write them to the associated plugin.
        /// </summary>
        public static void TrySetOptionsForPlugins(OptionsInformation opt)
        {
            TryForAllLoadedPlugins(opt, "Trying set json options to plugins", (dllinfo, lpi) =>
            {
                if (dllinfo.Options != null)
                {
                    var jsonStr = dllinfo.Options.ToString(Formatting.None);
                    lpi.InvokeAction("set-json-options", jsonStr);
                }

                return true;
            });
        }

        public static void PumpPluginLogsIntoLog(Action<StoredPrint> duplicateLog = null)
        {
            if (LoadedPlugins == null)
                return;

            // over all loaded plugins
            foreach (var pi in LoadedPlugins.Values)
            {
                // ReSharper disable EmptyGeneralCatchClause
                try
                {
                    for (int i = 0; i < 999; i++)
                    {
                        var x = pi.CheckForLogMessage();
                        if (x == null)
                            break;

                        var xs = x as string;
                        if (xs != null)
                        {
                            if (duplicateLog != null)
                                duplicateLog(new StoredPrint(xs));
                            Log.Info("[{0}] {1}", "" + pi.name, x);
                        }

                        var xsp = x as StoredPrint;
                        if (xsp != null)
                        {
                            xsp.msg = $"[{"" + pi.name}] " + xsp.msg;
                            if (duplicateLog != null)
                                duplicateLog(xsp);
                            Log.LogInstance.Append(xsp);
                        }
                    }
                }
                catch { }
                // ReSharper enable EmptyGeneralCatchClause
            }
        }

    }
}
