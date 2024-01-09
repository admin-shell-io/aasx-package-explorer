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
using WpfMtpControl;
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

        // protected Dictionary<string, string> _subscribedTopics = new Dictionary<string, string>();

        override public bool Open()
        {
            try
            {
                // make client
                // use the full target uri as endpoint (first)
                Client = new AasOpcUaClient(
                    TargetUri.ToString(), 
                    _autoAccept: true, 
                    _userName: this.User,
                    _password: this.Password);
                Client.Run();

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
                    Client.Cancel();
                    Client.Close();
                } catch (Exception ex)
                {
                    ;
                }
                // _subscribedTopics.Clear();
            }
        }

        protected NodeId ParseAndCreateNodeId (string input)
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

        override public int UpdateItemValue(AidIfxItemStatus item)
        {
            // access
            if (!IsConnected())
                return 0;

            // careful
            try
            {
                // get an node id?
                var nid = ParseAndCreateNodeId(item?.FormData?.Href);

                // direct read possible?
                var dv = Client.ReadNodeId(nid);
                item.Value = "" + dv?.Value;
                LastActive = DateTime.Now;
            }
            catch (Exception ex)
            {
                ;
            }


            return 0;
        }

        //override public void PrepareContinousRun(IEnumerable<AidIfxItemStatus> items)
        //{
        //    // access
        //    if (!IsConnected() || items == null)
        //        return;

        //    foreach (var item in items)
        //    {
        //        // valid topic?
        //        var topic = "" + item.FormData?.Href;
        //        if (topic.StartsWith("/"))
        //            topic = topic.Remove(0, 1);
        //        if (!topic.HasContent())
        //            continue;

        //        // need only "subscribe"
        //        if (item.FormData?.Mqv_controlPacket?.HasContent() != true)
        //            continue;
        //        if (item.FormData.Mqv_controlPacket.Trim().ToLower() != "subscribe")
        //            continue;

        //        // is topic already subscribed?
        //        if (_subscribedTopics.ContainsKey(topic))
        //            continue;

        //        // ok, subscribe
        //        var task = Task.Run(() => Client.SubscribeAsync(topic));
        //        task.Wait();
        //        _subscribedTopics.Add(topic, topic);
        //    }
        //}

    }
}
