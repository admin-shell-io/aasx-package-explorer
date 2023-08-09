/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <opensource@phoenixcontact.com>
Author: Andreas Orzelski

Copyright (c) 2019 Fraunhofer IOSB-INA Lemgo,
    eine rechtlich nicht selbständige Einrichtung der Fraunhofer-Gesellschaft
    zur Förderung der angewandten Forschung e.V. <florian.pethig@iosb-ina.fraunhofer.de>
Author: Florian Pethig

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxMqttServer;
using JetBrains.Annotations;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : IAasxPluginInterface
    {
        public LogInstance Log = new LogInstance();

        private MqttServer AASMqttServer = new MqttServer();

        public string GetPluginName()
        {
            return "AasxPluginMqttServer";
        }

        public bool GetPluginCheckForVisualExtension() { return false; }
        public AasxPluginVisualElementExtension CheckForVisualExtension(object referableDisplayed) { return null; }

        public void InitPlugin(string[] args)
        {
            // start ..
            Log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));
        }

        public object CheckForLogMessage()
        {
            return Log.PopLastShortTermPrint();
        }

        public AasxPluginActionDescriptionBase[] ListActions()
        {
            Log.Info("ListActions() called");
            var res = new List<AasxPluginActionDescriptionBase>();
            res.Add(new AasxPluginActionDescriptionBase("MQTTServer-start", "Sample server function doing nothing."));
            res.Add(new AasxPluginActionDescriptionBase("server-stop", "Stops sample server function."));
            return res.ToArray();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            if (action == "server-stop")
                AASMqttServer.MqttSeverStopAsync().Wait();

            if (action == "MQTTServer-start")
            {
                Log.Info("Starting Mqtt Server...");

                try
                {
                    AASMqttServer.MqttSeverStartAsync().Wait();
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

                // return as plain object
                var res = new AasxPluginResultBaseObject();
                res.strType = "MqttServer";
                res.obj = AASMqttServer;
                return res;
            }

            return null;
        }
    }
}
