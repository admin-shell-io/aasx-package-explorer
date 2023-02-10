/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using Newtonsoft.Json;

namespace AasxPackageLogic
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

            public PluginInstance(
                int sourceIndex, Assembly asm, Type plugType, IAasxPluginInterface plugObj, string[] args)
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

            public async Task<object> BasicInvokeMethodAsync(string mname, params object[] args)
            {
                var mi = plugType.GetMethod(mname);
                if (mi == null)
                    return null;
                // see: https://stackoverflow.com/questions/16153047/net-invoke-async-method-and-await
                var promise = (Task<object>)mi.Invoke(plugObj, args);
                return await promise;
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

            public static PluginInstance CreateNew(
                int sourceIndex, Assembly asm, Type plugType, IAasxPluginInterface plugObj, string[] args)
            {
                var pi = new PluginInstance(sourceIndex, asm, plugType, plugObj, args);
                pi.name = pi.GetName();
                pi.actions = pi.ListActions();
                if (pi.name == null || pi.actions == null || pi.actions.Length < 1)
                    return null;
                return pi;
            }

            public AasxPluginActionDescriptionBase FindAction(string name, bool useAsync = false)
            {
                if (actions == null || actions.Length < 1)
                    return null;
                foreach (var a in this.actions)
                    if (a.name.Trim().ToLower() == name.Trim().ToLower()
                        && a.UseAsync == useAsync)
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

            public Task<object> InvokeActionAsync(string name, params object[] args)
            {
                var a = this.FindAction(name, useAsync: true);
                if (a == null)
                    return null;
                return this.BasicInvokeMethodAsync("ActivateActionAsync", name, args);
            }
        }

        public static Dictionary<string, PluginInstance> LoadedPlugins = new Dictionary<string, PluginInstance>();

        public static PluginInstance FindPluginInstance(string pname)
        {
            if (LoadedPlugins == null || !pname.HasContent() || !LoadedPlugins.ContainsKey(pname))
                return null;
            return LoadedPlugins[pname];
        }

        public static List<OptionsInformation.PluginDllInfo> TrySearchPlugins(string searchDir)
        {
            // access
            if (!Directory.Exists(searchDir))
                return new List<OptionsInformation.PluginDllInfo>();

            var infos = new List<OptionsInformation.PluginDllInfo>();

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

            return infos;
        }

        public static Dictionary<string, PluginInstance> TryActivatePlugins(
            IReadOnlyList<OptionsInformation.PluginDllInfo> pluginDll)
        {
            var loadedPlugins = new Dictionary<string, PluginInstance>();

            for (int index = 0; index < pluginDll.Count; index++)
            {
                try
                {
                    Log.Singleton.Info("Trying to load a DLL: {0}", pluginDll[index].Path);
                    Console.WriteLine("Trying to load a DLL: {0}", pluginDll[index].Path);

                    // make full path
                    var fullfn = System.IO.Path.GetFullPath(pluginDll[index].Path);

                    // Note: use LoadFrom instead of LoadFile, insane:
                    // https://stackoverflow.com/questions/36075829/
                    // assembly-loadfile-look-dependencies-in-location-of-executeable
                    var asm = Assembly.LoadFrom(fullfn);

                    var tp = asm.GetType("AasxIntegrationBase.AasxPlugin");
                    if (tp == null)
                    {
                        Log.Singleton.Error(
                            "Cannot find class AasxIntegrationBase.AasxPlugin within .dll.");
                        continue;
                    }

                    // create instance using late binding
                    IAasxPluginInterface ob = (IAasxPluginInterface)Activator.CreateInstance(tp);
                    if (ob == null)
                    {
                        Log.Singleton.Error(
                            "Cannot create instance from class AasxIntegrationBase.AasxPlugin within .dll.");
                        continue;
                    }

                    // create plugin
                    var pi = PluginInstance.CreateNew(index, asm, tp, ob, pluginDll[index].Args);
                    if (pi == null)
                    {
                        Log.Singleton.Error(
                            "Cannot invoke methods within instance from " +
                                "class AasxIntegrationBase.AasxPlugin within .dll.");
                        continue;
                    }

                    // init plug-in
                    var singleArg = new object[] { pluginDll[index].Args };
                    pi.BasicInvokeMethod("InitPlugin", singleArg);

                    // adding
                    Log.Singleton.Info(".. adding plugin {0}", pi.name);
                    loadedPlugins.Add(pi.name, pi);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"Trying to activate the plugin at index {index}");
                    Console.WriteLine($"Trying to activate the plugin at index {index} gave {ex.Message}");
                }
            }

            return loadedPlugins;
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
                try
                {
                    var x = pi.InvokeAction("get-licenses") as AasxPluginResultLicense;
                    if (x != null)
                    {
                        if (x.shortLicense.HasContent())
                            res.shortLicense += x.shortLicense + Environment.NewLine;

                        if (!x.isStandardLicense && x.longLicense.HasContent())
                        {
                            res.longLicense += $"[{pi.name}]" + Environment.NewLine;
                            res.longLicense += x.longLicense + Environment.NewLine + Environment.NewLine;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.Error(
                        ex, $"Failed to load the license from the plugin: {pi.name} from {pi.asm.Location}");
                }
            }

            // OK
            return res;
        }

        /// <summary>
        /// Execute lambda for all loaded plugins and correlate with source plugin-dll-information.
        /// </summary>
        public static void TryForAllLoadedPlugins(
            IReadOnlyList<OptionsInformation.PluginDllInfo> pluginDllInfos,
            Dictionary<string, PluginInstance> loadedPlugins,
            string exceptionWhere,
            Action<OptionsInformation.PluginDllInfo, PluginInstance> lambda)
        {
            // access
            if (pluginDllInfos == null || lambda == null)
                return;

            // try to find matching plugins according to options
            for (int sourceIndex = 0; sourceIndex < pluginDllInfos.Count; sourceIndex++)
            {
                // options
                var dllInfo = pluginDllInfos[sourceIndex];

                // loaded plug in?
                PluginInstance piFound = null;
                foreach (var lpi in loadedPlugins.Values)
                    if (lpi.SourceIndex == sourceIndex)
                        piFound = lpi;

                // yes?
                if (piFound == null)
                    continue;

                // yes!
                try
                {
                    lambda(dllInfo, piFound);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, exceptionWhere);
                }
            }
        }

        public static void TryGetDefaultOptionsForPlugins(
            IReadOnlyList<OptionsInformation.PluginDllInfo> pluginDllInfos,
            Dictionary<string, PluginInstance> loadedPlugins)
        {
            TryForAllLoadedPlugins(
                pluginDllInfos,
                loadedPlugins,
                "Trying to get json options from Plugins",
                (dllInfo, lpi) =>
            {
                var popt = lpi.InvokeAction("get-json-options") as AasxPluginResultBaseObject;
                if (popt != null && popt.obj != null && popt.obj is string)
                    dllInfo.DefaultOptions = Newtonsoft.Json.Linq.JValue.Parse(popt.obj as string);
            });
        }

        public static void TrySetOptionsForPlugins(
            IReadOnlyList<OptionsInformation.PluginDllInfo> pluginDllInfos,
            Dictionary<string, PluginInstance> loadedPlugins)
        {
            TryForAllLoadedPlugins(
                pluginDllInfos,
                loadedPlugins,
                "Trying to set json options to plugins",
                (dllInfo, pluginInstance) =>
            {
                if (dllInfo.Options == null) return;

                var jsonStr = dllInfo.Options.ToString(Formatting.None);
                pluginInstance.InvokeAction("set-json-options", jsonStr);
            });
        }

        public static void PumpPluginLogsIntoLog(Action<StoredPrint> duplicateLog = null)
        {
            if (LoadedPlugins == null)
                return;

            // over all loaded plugins
            foreach (var pluginInstance in LoadedPlugins.Values)
            {
                try
                {
                    for (int i = 0; i < 999; i++)
                    {
                        var x = pluginInstance.CheckForLogMessage();
                        if (x == null)
                            break;

                        var xs = x as string;
                        if (xs != null)
                        {
                            if (duplicateLog != null)
                                duplicateLog(new StoredPrint(xs));
                            Log.Singleton.Info("[{0}] {1}", "" + pluginInstance.name, x);
                        }

                        var xsp = x as StoredPrint;
                        if (xsp != null)
                        {
                            xsp.msg = $"[{"" + pluginInstance.name}] " + xsp.msg;
                            if (duplicateLog != null)
                                duplicateLog(xsp);
                            Log.Singleton.Append(xsp);
                            if (xsp.isError)
                                Log.Singleton.NumberErrors++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }
        }

        public static void AllPluginsInvoke(string name, params object[] args)
        {
            // over all loaded plugins
            foreach (var lpi in LoadedPlugins.Values)
            {
                try
                {
                    lpi.InvokeAction(name, args);
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }
        }

        public static void PushEventIntoPlugins(AasEventMsgEnvelope ev)
        {
            // over all loaded plugins
            AllPluginsInvoke("push-aas-event", ev);
        }

    }
}
