using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMtpControl
{
    public class AasOpcUaClientOld
    {
        public void Log (string msg, params object[] args)
        {
            ;
        }

        public static AasOpcUaClientOld CreateClient(string url)
        {
            // try to directly initialize 
            ApplicationInstance application = new ApplicationInstance
            {
                ApplicationName = "a",
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = Utils.IsRunningOnMono() ? "Opc.Ua.MonoSampleClient" : "Opc.Ua.SampleClient"
            };            

            var config = application.LoadApplicationConfiguration(@"C:\Users\miho\Desktop\AasxPackageExplorer\AasxMtpPlugin\AasxPluginMtpViewer\bin\Debug\Opc.Ua.SampleClient.Config.xml", false).ConfigureAwait(false).GetAwaiter().GetResult();
            /*
            var config = new ApplicationConfiguration();
            config.ApplicationName = "AASX Package Explorer Client";
            config.ApplicationType = ApplicationType.Client;
            config.ApplicationUri = "www.opc.com";
            */
            if (config == null)
                return null;

            var epconfig = EndpointConfiguration.Create(config);
            var epdesc = CoreClientUtils.SelectEndpoint(url, useSecurity: false, operationTimeout: 2000);
            var ep = new ConfiguredEndpoint(null, epdesc, epconfig);

            var sessionTask = Session.Create(config, ep, false, "AAS client", 30000, new UserIdentity("anonymous", "anonymous"), null);
            var session = sessionTask.ConfigureAwait(false).GetAwaiter().GetResult();

            var c = new AasOpcUaClientOld();
            return c;
        }
    }
}
