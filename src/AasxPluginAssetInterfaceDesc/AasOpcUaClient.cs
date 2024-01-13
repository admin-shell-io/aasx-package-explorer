/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AasxIntegrationBase;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

// Note: this is a DUPLICATE from WpfMtpControl

namespace AasxPluginAssetInterfaceDescription
{
    public enum AasOpcUaClientStatus
    {
        Starting = 0,
        ErrorCreateApplication = 0x11,
        ErrorDiscoverEndpoints = 0x12,
        ErrorCreateSession = 0x13,
        ErrorBrowseNamespace = 0x14,
        ErrorCreateSubscription = 0x15,
        ErrorMonitoredItem = 0x16,
        ErrorAddSubscription = 0x17,
        ErrorRunning = 0x18,
        ErrorReadConfigFile = 0x19,
        ErrorNoKeepAlive = 0x30,
        ErrorInvalidCommandLine = 0x100,
        Running = 0x1000,
        Quitting = 0x8000,
        Quitted = 0x8001
    };

    public class AasOpcUaClient
    {
        const int ReconnectPeriod = 10;
        
        /// <summary>
        /// Good condition: starting or running
        /// </summary>
        public AasOpcUaClientStatus ClientStatus;
                
        protected LogInstance _log = null;

        protected string _endpointURL;
        protected static bool _autoAccept = true;
        protected string _userName;
        protected string _password;
        protected uint _timeOutMs = 2000;

        protected ISession _session;
        protected SessionReconnectHandler _reconnectHandler;
        
        public AasOpcUaClient(string endpointURL, bool autoAccept, 
            string userName, string password,
            uint timeOutMs = 2000,
            LogInstance log = null)
        {
            _endpointURL = endpointURL;
            _autoAccept = autoAccept;
            _userName = userName;
            _password = password;
            _timeOutMs = timeOutMs;
            _log = log;
        }

        public async Task DirectConnect()
        {
            await StartClientAsync();
        }

        public void Close()
        {
            if (_session == null)
                return;
            _session.Close(1);
            _session = null;
        }

        public AasOpcUaClientStatus StatusCode { get => ClientStatus; }

        public async Task StartClientAsync()
        {
            _log?.Info("1 - Create an Application Configuration.");
            ClientStatus = AasOpcUaClientStatus.ErrorCreateApplication;

            ApplicationInstance application = new ApplicationInstance
            {
                ApplicationName = "UA Core Sample Client",
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = Utils.IsRunningOnMono() ? "Opc.Ua.MonoSampleClient" : "Opc.Ua.SampleClient"
            };

            // load the application configuration.
            ApplicationConfiguration config = null;
            try
            {
                config = await application.LoadApplicationConfiguration(false);
            }
            catch (Exception ex)
            {
                _log?.Error(ex, "Error reading the config file");
                ClientStatus = AasOpcUaClientStatus.ErrorReadConfigFile;
                return;
            }

            // check the application certificate.
            bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0);
            if (!haveAppCertificate)
            {
                throw new Exception("Application instance certificate invalid!");
            }

            // ReSharper disable HeuristicUnreachableCode
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (haveAppCertificate)
            {
                config.ApplicationUri = X509Utils.GetApplicationUriFromCertificate(
                    config.SecurityConfiguration.ApplicationCertificate.Certificate);

                if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    _autoAccept = true;
                }
                // ReSharper disable once RedundantDelegateCreation
                config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(
                    CertificateValidator_CertificateValidation);
            }
            else
            {
                _log?.Info(
                    "WARN: missing application certificate, using unsecure connection.");
            }
            // ReSharper enable HeuristicUnreachableCode

            _log?.Info("2 - Discover endpoints of {0}.", _endpointURL);
            ClientStatus = AasOpcUaClientStatus.ErrorDiscoverEndpoints;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(_endpointURL, haveAppCertificate, 
                (int) _timeOutMs);
            _log?.Info("    Selected endpoint uses: {0}",
                selectedEndpoint.SecurityPolicyUri.Substring(selectedEndpoint.SecurityPolicyUri.LastIndexOf('#') + 1));

            _log?.Info("3 - Create a session with OPC UA server.");
            ClientStatus = AasOpcUaClientStatus.ErrorCreateSession;
            var endpointConfiguration = EndpointConfiguration.Create(config);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

            _session = await Session.Create(
                config, endpoint, false, "AasxPluginAssetInterfaceDesc", _timeOutMs,
                new UserIdentity(_userName, _password), null);

            // register keep alive handler
            _session.KeepAlive += Client_KeepAlive;

            // ok
            ClientStatus = AasOpcUaClientStatus.Running;

            // final
            _log?.Info("9 - Connection established.");
        }

        // very helpful when debugging and breaking

        private void Client_KeepAlive(Opc.Ua.Client.ISession sender, KeepAliveEventArgs e)
        {
            if (e.Status != null && ServiceResult.IsNotGood(e.Status))
            {
                _log?.Info("Keep alive {0} {1}/{2}", e.Status, sender.OutstandingRequestCount, 
                    sender.DefunctRequestCount);

                if (_reconnectHandler == null)
                {
                    _log?.Info("--- RECONNECTING ---");
                    _reconnectHandler = new SessionReconnectHandler();
                    _reconnectHandler.BeginReconnect(
                        sender, ReconnectPeriod * (int) _timeOutMs, Client_ReconnectComplete);
                }
            }
        }

        private void Client_ReconnectComplete(object sender, EventArgs e)
        {
            // ignore callbacks from discarded objects.
            if (!Object.ReferenceEquals(sender, _reconnectHandler))
            {
                return;
            }

            if (_reconnectHandler != null)
            {
                _session = _reconnectHandler.Session;
                _reconnectHandler.Dispose();
            }

            _reconnectHandler = null;

            _log?.Info("--- RECONNECTED ---");
        }

        private void CertificateValidator_CertificateValidation(
            CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = _autoAccept;
                if (_autoAccept)
                {
                    _log?.Info("Accepted Certificate: {0}", e.Certificate.Subject);
                }
                else
                {
                    _log?.Info("Rejected Certificate: {0}", e.Certificate.Subject);
                }
            }
        }

        public NodeId CreateNodeId(uint value, int nsIndex)
        {
            return new NodeId(value, (ushort)nsIndex);
        }

        public NodeId CreateNodeId(string nodeName, int index)
        {
            return new NodeId(nodeName, (ushort)index);
        }

        private Dictionary<string, ushort> nsDict = null;

        public NodeId CreateNodeId(string nodeName, string ns)
        {
            if (_session == null || _session.NamespaceUris == null)
                return null;

            // build up?
            if (nsDict == null)
            {
                nsDict = new Dictionary<string, ushort>();
                for (ushort i = 0; i < _session.NamespaceUris.Count; i++)
                    nsDict.Add(_session.NamespaceUris.GetString(i), i);
            }

            // find?
            if (nsDict == null || !nsDict.ContainsKey(ns))
                return null;

            return new NodeId(nodeName, nsDict[ns]);
        }

        public NodeId ParseAndCreateNodeId(string input)
        {
            if (input?.HasContent() != true)
                return null;

            {
                var match = Regex.Match(input, @"^\s*ns\s*=\s*(\d+)\s*;\s*i\s*=\s*(\d+)\s*$");
                if (match.Success
                    && ushort.TryParse(match.Groups[1].ToString(), out var ns)
                    && uint.TryParse(match.Groups[2].ToString(), out var i))
                    return new NodeId(i, ns);
            }
            {
                var match = Regex.Match(input, @"^\s*NS\s*(\d+)\s*\|\s*Numeric\s*\|\s*(\d+)\s*$");
                if (match.Success
                    && ushort.TryParse(match.Groups[1].ToString(), out var ns)
                    && uint.TryParse(match.Groups[2].ToString(), out var i))
                    return new NodeId(i, ns);
            }
            {
                var match = Regex.Match(input, @"^\s*ns\s*=\s*(\d+)\s*;\s*s\s*=\s*(.*)$");
                if (match.Success
                    && ushort.TryParse(match.Groups[1].ToString(), out var ns))
                    return new NodeId("" + match.Groups[2].ToString(), ns);
            }
            {
                var match = Regex.Match(input, @"^\s*NS\s*(\d+)\s*\|\s*Alphanumeric\s*\|\s*(.+)$");
                if (match.Success
                    && ushort.TryParse(match.Groups[1].ToString(), out var ns))
                    return new NodeId("" + match.Groups[2].ToString(), ns);
            }

            // no 
            return null;
        }

        public string ReadSubmodelElementValueAsString(string nodeName, int index)
        {
            if (_session == null)
                return "";

            NodeId node = new NodeId(nodeName, (ushort)index);
            return (_session.ReadValue(node).ToString());
        }

        public DataValue ReadNodeId(NodeId nid)
        {
            if (_session == null || nid == null || !_session.Connected)
                return null;
            return (_session.ReadValue(nid));
        }

        public Opc.Ua.Client.Subscription SubscribeNodeIds(NodeId[] nids, MonitoredItemNotificationEventHandler handler,
            int publishingInterval = 1000)
        {
            if (_session == null || nids == null || !_session.Connected || handler == null)
                return null;

            var subscription = new Subscription(_session.DefaultSubscription)
            { PublishingInterval = publishingInterval };

            foreach (var nid in nids)
            {
                var mi = new MonitoredItem(subscription.DefaultItem);
                mi.StartNodeId = nid;
                mi.Notification += handler;
                subscription.AddItem(mi);
            }

            _session.AddSubscription(subscription);
            subscription.Create();
            return subscription;
        }        
    }
}
