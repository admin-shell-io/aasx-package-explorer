using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace WpfMtpControl
{
    public enum AasOpcUaClientStatus : int
    {
        Ok = 0,
        ErrorCreateApplication = 0x11,
        ErrorDiscoverEndpoints = 0x12,
        ErrorCreateSession = 0x13,
        ErrorBrowseNamespace = 0x14,
        ErrorCreateSubscription = 0x15,
        ErrorMonitoredItem = 0x16,
        ErrorAddSubscription = 0x17,
        ErrorRunning = 0x18,
        ErrorNoKeepAlive = 0x30,
        ErrorInvalidCommandLine = 0x100,
        Running = 0x1000,
        Quitting = 0x8000,
        Quitted = 0x8001
    };

    public class AasOpcUaClient
    {
        const int ReconnectPeriod = 10;
        Session session;
        SessionReconnectHandler reconnectHandler;
        string endpointURL;
        int clientRunTime = Timeout.Infinite;
        static bool autoAccept = true;
        static AasOpcUaClientStatus exitCode;
        string userName;
        string password;

        public AasOpcUaClient(string _endpointURL, bool _autoAccept,
            int _stopTimeout, string _userName, string _password)
        {
            endpointURL = _endpointURL;
            autoAccept = _autoAccept;
            clientRunTime = _stopTimeout <= 0 ? Timeout.Infinite : _stopTimeout * 1000;
            userName = _userName;
            password = _password;
        }

        private BackgroundWorker worker = null;

        public void Run()
        {
            // start server as a worker (will start in the background)
            var worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (s1, e1) =>
            {
                try
                {
                    while (true)
                    {
                        StartClientAsync().Wait();

                        // keep running
                        if (exitCode == AasOpcUaClientStatus.Running)
                            while (true)
                                Thread.Sleep(200);

                        // restart
                        Thread.Sleep(200);
                    }
                }
                catch
                {
                }
            };
            worker.RunWorkerCompleted += (s1, e1) =>
            {
                ;
            };
            worker.RunWorkerAsync();
        }

        public void Cancel()
        {
            if (worker != null && worker.IsBusy)
                try
                {
                    worker.CancelAsync();
                    worker.Dispose();
                }
                catch { }

        }

        public void Close()
        {
            if (session == null)
                return;
            session.Close(1);
            session = null;
        }

        public AasOpcUaClientStatus StatusCode { get => exitCode; }

        public async Task StartClientAsync()
        {
            Console.WriteLine("1 - Create an Application Configuration.");
            exitCode = AasOpcUaClientStatus.ErrorCreateApplication;

            ApplicationInstance application = new ApplicationInstance
            {
                ApplicationName = "UA Core Sample Client",
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = Utils.IsRunningOnMono() ? "Opc.Ua.MonoSampleClient" : "Opc.Ua.SampleClient"
            };

            // load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false);

            // check the application certificate.
            bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0);
            if (!haveAppCertificate)
            {
                throw new Exception("Application instance certificate invalid!");
            }

            if (haveAppCertificate)
            {
                config.ApplicationUri = Utils.GetApplicationUriFromCertificate(
                    config.SecurityConfiguration.ApplicationCertificate.Certificate);

                if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    autoAccept = true;
                }
                config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(
                    CertificateValidator_CertificateValidation);
            }
            else
            {
                Console.WriteLine("    WARN: missing application certificate, using unsecure connection.");
            }

            Console.WriteLine("2 - Discover endpoints of {0}.", endpointURL);
            exitCode = AasOpcUaClientStatus.ErrorDiscoverEndpoints;
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointURL, haveAppCertificate, 15000);
            Console.WriteLine("    Selected endpoint uses: {0}",
                selectedEndpoint.SecurityPolicyUri.Substring(selectedEndpoint.SecurityPolicyUri.LastIndexOf('#') + 1));

            Console.WriteLine("3 - Create a session with OPC UA server.");
            exitCode = AasOpcUaClientStatus.ErrorCreateSession;
            var endpointConfiguration = EndpointConfiguration.Create(config);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

            session = await Session.Create(config, endpoint, false, "OPC UA Console Client", 60000,
                new UserIdentity(userName, password), null);

            // register keep alive handler
            session.KeepAlive += Client_KeepAlive;

            // ok
            exitCode = AasOpcUaClientStatus.Running;
        }

        private void Client_KeepAlive(Session sender, KeepAliveEventArgs e)
        {
            if (e.Status != null && ServiceResult.IsNotGood(e.Status))
            {
                Console.WriteLine("{0} {1}/{2}", e.Status, sender.OutstandingRequestCount, sender.DefunctRequestCount);

                if (reconnectHandler == null)
                {
                    Console.WriteLine("--- RECONNECTING ---");
                    reconnectHandler = new SessionReconnectHandler();
                    reconnectHandler.BeginReconnect(sender, ReconnectPeriod * 1000, Client_ReconnectComplete);
                }
            }
        }

        private void Client_ReconnectComplete(object sender, EventArgs e)
        {
            // ignore callbacks from discarded objects.
            if (!Object.ReferenceEquals(sender, reconnectHandler))
            {
                return;
            }

            session = reconnectHandler.Session;
            reconnectHandler.Dispose();
            reconnectHandler = null;

            Console.WriteLine("--- RECONNECTED ---");
        }

        private static void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            foreach (var value in item.DequeueValues())
            {
                /// Console.WriteLine("{0}: {1}, {2}, {3}", item.DisplayName, value.Value, 
                /// value.SourceTimestamp, value.StatusCode);
            }
        }

        private static void CertificateValidator_CertificateValidation(
            CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = autoAccept;
                if (autoAccept)
                {
                    Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
                }
                else
                {
                    Console.WriteLine("Rejected Certificate: {0}", e.Certificate.Subject);
                }
            }
        }

        public NodeId CreateNodeId(string nodeName, int index)
        {
            return new NodeId(nodeName, (ushort)index);
        }

        private Dictionary<string, ushort> nsDict = null;

        public NodeId CreateNodeId(string nodeName, string ns)
        {
            if (session == null || session.NamespaceUris == null)
                return null;

            // build up?
            if (nsDict == null)
            {
                nsDict = new Dictionary<string, ushort>();
                for (ushort i = 0; i < session.NamespaceUris.Count; i++)
                    nsDict.Add(session.NamespaceUris.GetString(i), i);
            }

            // find?
            if (nsDict == null || !nsDict.ContainsKey(ns))
                return null;

            return new NodeId(nodeName, nsDict[ns]);
        }

        public string ReadSubmodelElementValueAsString(string nodeName, int index)
        {
            if (session == null)
                return "";

            NodeId node = new NodeId(nodeName, (ushort)index);
            return (session.ReadValue(node).ToString());
        }

        public DataValue ReadNodeId(NodeId nid)
        {
            if (session == null || nid == null || !session.Connected)
                return null;
            return (session.ReadValue(nid));
        }

        public void SubscribeNodeIds(NodeId[] nids, MonitoredItemNotificationEventHandler handler,
            int publishingInteral = 1000)
        {
            if (session == null || nids == null || !session.Connected || handler == null)
                return;

            var subscription = new Subscription(session.DefaultSubscription)
            { PublishingInterval = publishingInteral };

            foreach (var nid in nids)
            {
                var mi = new MonitoredItem(subscription.DefaultItem);
                mi.StartNodeId = nid;
                mi.Notification += handler;
                subscription.AddItem(mi);
            }

            session.AddSubscription(subscription);
            subscription.Create();
        }
    }
}
