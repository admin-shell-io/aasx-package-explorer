/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AasOpcUaServer;
using AasxUaNetServer;
using AdminShellNS;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    // ReSharper disable once UnusedType.Global
    public class AasxPlugin : AasxPluginBase
    {
        #region // Plug In
        private AasxUaNetServer.UaNetServerOptions _options = new AasxUaNetServer.UaNetServerOptions();
        private bool _stop = false;
        private UaServerWrapper _server = null;
        // dead-csharp off
        /* TODO (MIHO, 2021-11-17): damned, weird dependency reasons between
         * 
         * .net6.0 and .net472 seem NOT TO ALLOW referring to AasxIntegrationBase.
         * Fix */
        //private static T LoadDefaultOptionsFromAssemblyDirXXXX<T>(
        //    string pluginName, Assembly assy = null,
        //    JsonSerializerSettings settings = null) where T : AasxPluginOptionsBase
        //{
        //    // expand assy?
        //    if (assy == null)
        //        assy = Assembly.GetExecutingAssembly();
        //    if (pluginName == null || pluginName == "")
        //        return null;

        //    // build fn
        //    var optfn = System.IO.Path.Combine(
        //                System.IO.Path.GetDirectoryName(assy.Location),
        //                pluginName + ".options.json");

        //    if (File.Exists(optfn))
        //    {
        //        var optText = File.ReadAllText(optfn);

        //        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(optText, settings);
        //    }

        //    // no
        //    return null;
        //}
        // dead-csharp on

        public new void InitPlugin(string[] args)
        {
            PluginName = "AasxPluginUaNetServer";
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = AasxUaNetServer.UaNetServerOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase
                        .LoadDefaultOptionsFromAssemblyDir<AasxUaNetServer.UaNetServerOptions>(
                            this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    this._options = newOpt;
            }
            catch (Exception ex)
            {
                _log?.Error(ex, "Exception when reading default options {1}");
            }

            // index them!
            _options.IndexListOfRecords(_options.Records);
        }

        public new AasxPluginActionDescriptionBase[] ListActions()
        {
            var res = ListActionsBasicHelper(
                enableCheckVisualExt: false,
                enableLicenses: true);
            res.Add(new AasxPluginActionDescriptionBase("server-start", "Start OPC UA Server for AASX."));
            res.Add(new AasxPluginActionDescriptionBase("server-stop", "Stops server function."));
            return res.ToArray();
        }

        public new AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            if (action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                lic.shortLicense =
                    "This application uses the OPC Foundation .NET Standard stack. See: OPC REDISTRIBUTABLES "
                    + "Agreement of Use." + Environment.NewLine +
                    "The OPC UA Example Code of OPC UA Standard is licensed under the MIT license (MIT).";

                lic.isStandardLicense = true;
                lic.longLicense = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                    "LICENSE.txt", Assembly.GetExecutingAssembly());

                return lic;
            }

            if (action == "server-stop")
                this._stop = true;

            if (action == "server-start")
            {
                // init
                this._stop = false;
                _log.Info("Starting OPC UA AASX Server. Based on the OPC Foundation UA Net Standard stack.");
                _log.Info("Copyright (c) 2018-2023 Festo SE & Co. KG " +
                    "<https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister.");
                _log.Info("Portions copyright (c) by OPC Foundation, Inc. and licensed under the Reciprocal "
                    + "Community License (RCL).");
                _log.Info("See https://opcfoundation.org/license/rcl.html.");

                // access AASX
                if (args == null || args.Length < 1)
                {
                    _log.Info("No AASX package environment passed to plug-in. Stopping...");
                    System.Threading.Thread.Sleep(5000);
                    return null;
                }

                var package = args[0] as AdminShellPackageEnv;
                if (package == null)
                {
                    _log.Info("No AASX package environment passed to plug-in. Stopping...");
                    System.Threading.Thread.Sleep(5000);
                    return null;
                }
                _log.Info("AASX package env has filename {0}", package.Filename);

                // configure UA here a little bit
                ApplicationInstance.MessageDlg = new ApplicationMessageDlg(_log);

                // arguments
                var externalOptions = new List<string>();
                if (_options?.Args != null)
                    foreach (var o1 in _options.Args)
                        externalOptions.Add(o1);

                if (args.Length >= 2 && args[1] is string[])
                {
                    var pluginArgs = args[1] as string[];
                    if (pluginArgs != null && pluginArgs.Length > 0)
                    {
                        foreach (var o2 in pluginArgs)
                            externalOptions.Add(o2);
                    }
                }

                // debug
                var lstr = $"Taking over {externalOptions.Count} arguments: ";
                foreach (var ls in externalOptions)
                    lstr += ls + " ";
                _log.Info("{0}", lstr);

                // parse
                var internalOptions = new AasOpcUaServer.AasxUaServerOptions();
                internalOptions.ParseArgs(externalOptions.ToArray());

                // run the server
                try
                {
                    this._server = new UaServerWrapper(_stopTimeout: 0, _aasxEnv: package, logger: _log, _serverOptions: internalOptions);
                    this._server.Run();
                }
                catch (Exception ex)
                {
                    _log.Info("Exception whenn running server: {0}", ex.Message);
                }

                // do as long as user wants
                int i = 0;
                while (true)
                {
                    if (this._stop)
                    {
                        _log.Info("Stopping ...");
                        if (this._server != null)
                            this._server.Stop();
                        break;
                    }

                    //TODO (MIHO, 0000-00-00): Temporary disabled
                    // seems not to work anymore
                    ////if (this.server != null && this.server.IsNotRunningAnymore())
                    ////    break;

                    // new option
                    if (true == this._server?.FinallyStopped)
                        break;

                    System.Threading.Thread.Sleep(50);
                    if (i % 200 == 0)
                        _log.Info("Heartbeat {0} x 50ms ..", i);
                    i++;
                }
                _log.Info("Stopped.");
            }

            var res = new AasxPluginResultBase();
            return res;
        }

    }

    #endregion

    #region // taken form Net46 Console Server
    //
    //
    //

    public class ApplicationMessageDlg : IApplicationMessageDlg
    {
        private LogInstance logger = null;
        private string message = string.Empty;
        private bool ask = false;

        public ApplicationMessageDlg(LogInstance logger)
        {
            this.logger = logger;
        }

        public override void Message(string text, bool ask = false)
        {
            this.message = text;
            this.ask = ask;
        }

        public override async Task<bool> ShowAsync()
        {
            if (ask)
            {
                message += " (y/n, default y): ";
                logger.Info("{0}", message);
                Console.Write(message);
            }
            else
            {
                logger.Info("{0}", message);
                Console.WriteLine(message);
            }
            if (ask)
            {
                // always say yes!
                return await Task.FromResult(true);
            }
            return await Task.FromResult(true);
        }
    }



    #endregion
}
