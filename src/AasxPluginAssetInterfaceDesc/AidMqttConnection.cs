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

namespace AasxPluginAssetInterfaceDescription
{
    public class AidMqttConnection : AidBaseConnection
    {
        protected static MqttFactory _factory = new MqttFactory();

        public IMqttClient Client;

        protected Dictionary<string, string> _subscribedTopics = new Dictionary<string, string>();

        override public bool Open()
        {
            try
            {
                // see: https://www.emqx.com/en/blog/connecting-to-serverless-mqtt-broker-with-mqttnet-in-csharp
                Client = _factory.CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(TargetUri.Host, TargetUri.Port) // MQTT broker address and port
                    // .WithCredentials(username, password) // Set username and password
                    .WithClientId("AasxPackageExplorer")
                    .WithCleanSession()
                    .Build();

                // need to switch to async
                var task = Task.Run(() => Client.ConnectAsync(options));
                task.Wait();
                var res = task.Result;

                // no subscriptions, yet
                _subscribedTopics.Clear();

                // get messages
                Client.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;

                // ok
                return Client.IsConnected;
            }
            catch (Exception ex)
            {
                Client = null;
                _subscribedTopics.Clear();
                return false;
            }
        }

        private async Task Client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            // access
            if (arg == null)
                return;

            // topic?
            var topic = arg.ApplicationMessage?.Topic;
            if (topic?.HasContent() != true)
                return;

            // payload?
            // var payload = Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment);
            var payload = arg.ApplicationMessage.ConvertPayloadToString();
            if (payload?.HasContent() != true)
                return;

            // refer further..
            MessageReceived?.Invoke(topic, payload);

            // ok
            await Task.Yield();
        }

        override public bool IsConnected()
        {
            // simple
            return Client != null && Client.IsConnected;
        }

        override public void Close()
        {
            if (IsConnected())
            {
                var task = Task.Run(() => Client.DisconnectAsync());
                task.Wait();
                _subscribedTopics.Clear();
            }
        }

        override public int UpdateItemValue(AidIfxItemStatus item)
        {
            // Cannot do anything. MQTT is pure publish/ subscribe.
            // Unable to ask for a status value.
            return 0;
        }

        override public void PrepareContinousRun(IEnumerable<AidIfxItemStatus> items)
        {
            // access
            if (!IsConnected() || items == null)
                return;

            foreach (var item in items)
            {
                // valid topic?
                var topic = "" + item.FormData?.Href;
                if (topic.StartsWith("/"))
                    topic = topic.Remove(0, 1);
                if (!topic.HasContent())
                    continue;

                // need only "subscribe"
                if (item.FormData?.Mqv_controlPacket?.HasContent() != true)
                    continue;
                if (item.FormData.Mqv_controlPacket.Trim().ToLower() != "subscribe")
                    continue;

                // is topic already subscribed?
                if (_subscribedTopics.ContainsKey(topic))
                    continue;

                // ok, subscribe
                var task = Task.Run(() => Client.SubscribeAsync(topic));
                task.Wait();
                _subscribedTopics.Add(topic, topic);
            }
        }

    }
}
