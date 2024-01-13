/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AasxIntegrationBase;
using AasxPredefinedConcepts.AssetInterfacesDescription;
using FluentModbus;
using System.Net;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Net.Http;
using MQTTnet;
using MQTTnet.Client;
using System.Web.Services.Description;
using Opc.Ua;

namespace AasxPluginAssetInterfaceDescription
{
    public class AidOpcUaConnection : AidBaseConnection
    {       
        public AasOpcUaClient Client;

        public class SubscribedItem
        {
            public string NodePath;
            public Opc.Ua.Client.Subscription Subscription;
            public AidIfxItemStatus Item;
        }

        protected Dictionary<Opc.Ua.NodeId, SubscribedItem> _subscriptions
            = new Dictionary<NodeId, SubscribedItem>();

        override public bool Open()
        {
            try
            {
                // make client
                // use the full target uri as endpoint (first)
                Client = new AasOpcUaClient(
                    TargetUri.ToString(), 
                    autoAccept: true, 
                    userName: this.User,
                    password: this.Password,
                    log: Log);
                // Client.Run();

                var task = Task.Run(async () => await Client.DirectConnect());
                task.Wait();

                // ok
                return IsConnected();
            }
            catch (Exception ex)
            {
                Client = null;
                // _subscribedTopics.Clear();
                return false;
            }
        }

        override public bool IsConnected()
        {
            // simple
            return Client != null && Client.StatusCode == AasOpcUaClientStatus.Running;
        }

        override public void Close()
        {
            if (IsConnected())
            {
                try
                {
                    // Client.Cancel();
                    Client.Close();
                } catch (Exception ex)
                {
                    ;
                }
                // _subscribedTopics.Clear();
            }
        }        

        override public int UpdateItemValue(AidIfxItemStatus item)
        {
            // access
            if (!IsConnected())
                return 0;

            // careful
            try
            {
                // get an node id?
                var nid = Client.ParseAndCreateNodeId(item?.FormData?.Href);

                // direct read possible?
                var dv = Client.ReadNodeId(nid);
                item.Value = AdminShellUtil.ToStringInvariant(dv?.Value);
                LastActive = DateTime.Now;
            }
            catch (Exception ex)
            {
                ;
            }

            return 0;
        }

        override public void PrepareContinousRun(IEnumerable<AidIfxItemStatus> items)
        {
            // access
            if (!IsConnected() || items == null)
                return;

            // over the items
            // go the easy way: put each item into one subscription
            foreach (var item in items)
            {
                // valid href?
                var nodePath = "" + item.FormData?.Href;
                nodePath = nodePath.Trim();
                if (!nodePath.HasContent())
                    continue;

                // get an node id?
                var nid = Client.ParseAndCreateNodeId(nodePath);
                if (nid == null)
                    continue;

                // is topic already subscribed?
                if (_subscriptions.ContainsKey(nodePath))
                    continue;

                // ok, make subscription
                var sub = Client.SubscribeNodeIds(
                    new[] { nid },
                    handler: SubscriptionHandler,
                    publishingInteral: 500);
                _subscriptions.Add(nodePath,
                    new SubscribedItem() {
                        NodePath = nodePath,
                        Subscription = sub,
                        Item = item,
                    });
            }
        }

        protected void SubscriptionHandler(
            Opc.Ua.Client.MonitoredItem monitoredItem,
            Opc.Ua.Client.MonitoredItemNotificationEventArgs e)
        {
            // key is the "start node"
            if (_subscriptions == null || monitoredItem?.StartNodeId == null
                || !_subscriptions.ContainsKey(monitoredItem.StartNodeId))
                return;

            // okay
            var subi = _subscriptions[monitoredItem.StartNodeId];
            if (subi?.Item != null && subi.NodePath?.HasContent() == true)
            {
                // take over most actual value
                var valueObj = monitoredItem.DequeueValues().LastOrDefault();
                MessageReceived?.Invoke(subi.NodePath, AdminShellUtil.ToStringInvariant(valueObj));
            }
        }
    }
}
