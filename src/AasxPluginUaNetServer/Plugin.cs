/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
    public class AasxPlugin : IAasxPluginInterface
    {
        #region // Plug In
        private LogInstance logger = new LogInstance();
        private AasxUaNetServer.UaNetServerOptions options = new AasxUaNetServer.UaNetServerOptions();
        private bool stop = false;

        private UaServerWrapper server = null;

        public string GetPluginName()
        {
            logger.Info("GetPluginName() = {0}", "Net46AasxServerPlugin");
            return "AasxPluginUaNetServer";
        }

        /* TODO (MIHO, 2021-11-17): damned, weird dependency reasons between
         * .net6.0 and .net472 seem NOT TO ALLOW referring to AasxIntegrationBase.
         * Fix */
        private static T LoadDefaultOptionsFromAssemblyDirXXXX<T>(
            string pluginName, Assembly assy = null,
            JsonSerializerSettings settings = null) where T : AasxPluginOptionsBase
        {
            // expand assy?
            if (assy == null)
                assy = Assembly.GetExecutingAssembly();
            if (pluginName == null || pluginName == "")
                return null;

            // build fn
            var optfn = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(assy.Location),
                        pluginName + ".options.json");

            if (File.Exists(optfn))
            {
                var optText = File.ReadAllText(optfn);

                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(optText, settings);
            }

            // no
            return null;
        }


        public void InitPlugin(string[] args)
        {
            logger.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            options = AasxUaNetServer.UaNetServerOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    /* AasxPluginOptionsBase */ LoadDefaultOptionsFromAssemblyDirXXXX<
                         AasxUaNetServer.UaNetServerOptions>(
                            this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    this.options = newOpt;
            }
            catch (Exception ex)
            {
                logger?.Error(ex, "Exception when reading default options {1}");
            }
        }

        public object CheckForLogMessage()
        {
            return logger.PopLastShortTermPrint();
        }

        public AasxPluginActionDescriptionBase[] ListActions()
        {
            logger.Info("ListActions() called");
            var res = new List<AasxPluginActionDescriptionBase>();
            res.Add(new AasxPluginActionDescriptionBase("get-licenses", "Reports about used licenses."));
            res.Add(new AasxPluginActionDescriptionBase("server-start", "Start OPC UA Server for AASX."));
            res.Add(new AasxPluginActionDescriptionBase("server-stop", "Stops server function."));
            return res.ToArray();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
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
                this.stop = true;

            if (action == "server-start")
            {
                // init
                this.stop = false;
                logger.Info("Starting OPC UA AASX Server. Based on the OPC Foundation UA Net Standard stack.");
                logger.Info("Copyright (c) 2018-2021 Festo AG & Co. KG " +
                    "<https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister.");
                logger.Info("Portions copyright (c) by OPC Foundation, Inc. and licensed under the Reciprocal "
                    + "Community License (RCL).");
                logger.Info("See https://opcfoundation.org/license/rcl.html.");

                // access AASX
                if (args == null || args.Length < 1)
                {
                    logger.Info("No AASX package environment passed to plug-in. Stopping...");
                    System.Threading.Thread.Sleep(5000);
                    return null;
                }

                var package = args[0] as AdminShellPackageEnv;
                if (package == null)
                {
                    logger.Info("No AASX package environment passed to plug-in. Stopping...");
                    System.Threading.Thread.Sleep(5000);
                    return null;
                }
                logger.Info("AASX package env has filename {0}", package.Filename);

                // configure UA here a little bit
                ApplicationInstance.MessageDlg = new ApplicationMessageDlg(logger);

                // arguments
                var externalOptions = new List<string>();
                if (options?.Args != null)
                    foreach (var o1 in options.Args)
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
                logger.Info("{0}", lstr);

                // parse
                var internalOptions = new AasOpcUaServer.AasxUaServerOptions();
                internalOptions.ParseArgs(externalOptions.ToArray());

                // run the server
                try
                {
                    this.server = new UaServerWrapper(_autoAccept: true, _stopTimeout: 0, _aasxEnv: package,
                        logger: logger, _serverOptions: internalOptions);
                    this.server.Run();
                }
                catch (Exception ex)
                {
                    logger.Info("Exception whenn running server: {0}", ex.Message);
                }

                // do as long as user wants
                int i = 0;
                while (true)
                {
                    if (this.stop)
                    {
                        logger.Info("Stopping ...");
                        if (this.server != null)
                            this.server.Stop();
                        break;
                    }

                    // MICHA TODO : Temporary disabled
                    // seems not to work anymore
                    ////if (this.server != null && this.server.IsNotRunningAnymore())
                    ////    break;

                    // new option
                    if (true == this.server?.FinallyStopped)
                        break;

                    System.Threading.Thread.Sleep(50);
                    if (i % 200 == 0)
                        logger.Info("Heartbeat {0} x 50ms ..", i);
                    i++;
                }
                logger.Info("Stopped.");
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
