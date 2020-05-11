using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
*/

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    public class AasxPlugin : IAasxPluginInterface // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    {
        public LogInstance Log = new LogInstance();
        private bool stop = false;

        public string GetPluginName()
        {
            Log.Info("GetPluginName() = {0}", "EmptySample");
            return "EmptySample";
        }

        public bool GetPluginCheckForVisualExtension() { return false; }
        public AasxPluginVisualElementExtension CheckForVisualExtension(object referableDisplayed) { return null; }

        public void InitPlugin(string[] args)
        {
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
            res.Add(new AasxPluginActionDescriptionBase("server-start", "Sample server function doing nothing."));
            res.Add(new AasxPluginActionDescriptionBase("server-stop", "Stops sample server function."));
            return res.ToArray();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            // logger.Log("ActivatePlugin() called with action = {0}", action);

            if (action == "server-stop")
                this.stop = true;

            if (action == "server-start")
            {
                this.stop = false;
                Log.Info("This is a (empty) sample server demonstrating the plugin capabilities, only.");
                int i = 0;
                while (true)
                {
                    if (this.stop)
                    {
                        Log.Info("Stopping ...");
                        break;
                    }
                    System.Threading.Thread.Sleep(50);
                    if (i % 20 == 0)
                        Log.Info("Heartbeat {0} ..", i);
                    i++;
                }
                Log.Info("Stopped.");
            }

            var res = new AasxPluginResultBase();
            return res;
        }
    }
}
