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
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, 
 * author: Michael Hoffmeister.
*/

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : IAasxPluginInterface
    {
        #region // Plug In
        private LogInstance logger = new LogInstance();
        private bool stop = false;

        private UaServerWrapper server = null;

        public string GetPluginName()
        {
            logger.Info("GetPluginName() = {0}", "Net46AasxServerPlugin");
            return "Net46AasxServerPlugin";
        }

        public void InitPlugin(string[] args)
        {
            logger.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));
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
                    + "Agreement of Use.";

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
                logger.Info("Copyright (c) 2018-2019 Festo AG & Co. KG " +
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
                var options = new AasOpcUaServer.AasxUaServerOptions();
                if (args.Length >= 2 && args[1] is string[])
                {
                    var pluginArgs = args[1] as string[];
                    if (pluginArgs != null && pluginArgs.Length > 0)
                    {
                        // debug
                        var lstr = $"Taking over {pluginArgs.Length} arguments: ";
                        foreach (var ls in pluginArgs)
                            lstr += ls + " ";
                        logger.Info("{0}", lstr);

                        // parse
                        options.ParseArgs(pluginArgs);
                    }
                }

                // run the server
                try
                {
                    this.server = new UaServerWrapper(_autoAccept: true, _stopTimeout: 0, _aasxEnv: package,
                        logger: logger, _serverOptions: options);
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
                    if (false && this.server != null && this.server.IsNotRunningAnymore())
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

        public override void Message(string text, bool ask)
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