/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AasxIntegrationBase.AdminShellEvents;
using AdminShellNS;
using AnyUi;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using Newtonsoft.Json;

namespace AasxMqttClient
{
    public class AnyUiDialogueDataMqttPublisher : AnyUiDialogueDataBase
    {
        [JsonIgnore]
        public static int MqttDefaultPort = 1883;

        [JsonIgnore]
        public static string HelpString =
            "{aas} = AAS.idShort, {aas-id} = AAS.identification" + Environment.NewLine +
            "{sm} = Submodel.idShort, {sm-id} = Submodel.identification" + Environment.NewLine +
            "{path} = Path of idShorts";

        public string BrokerUrl = "localhost:1883";
        public bool MqttRetain = false;

        public bool EnableFirstPublish = true;
        public string FirstTopicAAS = "AAS";
        public string FirstTopicSubmodel = "Submodel_{sm}";

        public bool EnableEventPublish = false;
        public string EventTopic = "Events";

        public bool SingleValuePublish = false;
        public bool SingleValueFirstTime = false;
        public string SingleValueTopic = "Values";

        public bool LogDebug = false;

        public AnyUiDialogueDataMqttPublisher(
            string caption = "",
            double? maxWidth = null)
            : base(caption, maxWidth)
        {
        }

        public static AnyUiDialogueDataMqttPublisher CreateWithOptions(
            string caption = "",
            double? maxWidth = null,
            Newtonsoft.Json.Linq.JToken jtoken = null)
        {
            // access
            if (jtoken == null)
                return new AnyUiDialogueDataMqttPublisher(caption, maxWidth);

            try
            {
                var res = jtoken.ToObject<AnyUiDialogueDataMqttPublisher>();
                if (res != null)
                    // found something
                    return res;
            }
            catch { }

            // .. no, default!
            return new AnyUiDialogueDataMqttPublisher(caption, maxWidth);
        }
    }    

    public class MqttClient
    {
        private AnyUiDialogueDataMqttPublisher _diaData = null;
        private GrapevineLoggerSuper _logger = null;
        private IMqttClient _mqttClient = null;

        /// <summary>
        /// Splits into host part and numerical port number. Format e.g. "192.168.0.27:1883" or "localhost:1884".
        /// Note: special function realized, as side effects of <c>Uri()</c> not clear.
        /// </summary>
        private Tuple<string, int> SplitBrokerUrl(string url)
        {
            // TODO (MIHO, 2021-06-30): check use of Url()
            if (url == null)
                return null;

            // trivial
            int p = url.IndexOf(':');
            if (p < 0 || p >= url.Length)
                return new Tuple<string, int>(url, AnyUiDialogueDataMqttPublisher.MqttDefaultPort);

            // split
            var host = url.Substring(0, p);
            var pstr = url.Substring(p + 1);
            if (int.TryParse(pstr, out int pnr))
                return new Tuple<string, int>(host, pnr);

            // default
            return new Tuple<string, int>(host, AnyUiDialogueDataMqttPublisher.MqttDefaultPort);
        }

        private string GenerateTopic(string template, 
            string defaultIfNull = null,
            string aasIdShort = null, AdminShell.Identification aasId = null,
            string smIdShort = null, AdminShell.Identification smId = null,
            string path = null)
        {
            var res = template;

            if (defaultIfNull != null && res == null)
                res = defaultIfNull;

            if (aasIdShort != null)
                res = res.Replace("{aas}", "" + aasIdShort);

            if (aasId?.id != null)
                res = res.Replace("{aas-id}", "" + System.Net.WebUtility.UrlEncode(aasId.id));

            if (smIdShort != null)
                res = res.Replace("{sm}", "" + smIdShort);

            if (smId?.id != null)
                res = res.Replace("{sm-id}", "" + System.Net.WebUtility.UrlEncode(smId.id));

            if (path != null)
                res = res.Replace("{path}", path);

            // make sure that topic are not starting / ending with '/'
            res = res.Trim(' ', '/');

            return res;
        }

        public async Task StartAsync(
            AdminShellPackageEnv package,
            AnyUiDialogueDataMqttPublisher diaData,
            GrapevineLoggerSuper logger = null)
        {
            // first options
            _diaData = diaData;
            _logger = logger;
            if (_diaData == null)
            {
                logger?.Error("Error: no options available.");
                return;
            }

            // broker?
            var hp = SplitBrokerUrl(_diaData.BrokerUrl);
            if (hp == null)
            {
                _logger?.Error("Error: no broker URL available.");
                return;
            }
            _logger?.Info($"Conneting broker {hp.Item1}:{hp.Item2} ..");

            // Create TCP based options using the builder.
            
            var options = new MqttClientOptionsBuilder()
                .WithClientId("AASXPackageXplorer MQTT Client")
                .WithTcpServer(hp.Item1, hp.Item2)
                .Build();

            //create MQTT Client and Connect using options above
            
            _mqttClient = new MqttFactory().CreateMqttClient();
            await _mqttClient.ConnectAsync(options);
            if (_mqttClient.IsConnected)
                _logger?.Info("### CONNECTED WITH SERVER ###");

            //publish AAS to AAS Topic

            if (_diaData.EnableFirstPublish)
            {
                foreach (AdminShell.AdministrationShell aas in package.AasEnv.AdministrationShells)
                {
                    _logger?.Info("Publish first AAS");
                    var message = new MqttApplicationMessageBuilder()
                                   .WithTopic(GenerateTopic(
                                        _diaData.FirstTopicAAS, defaultIfNull: "AAS",
                                        aasIdShort: aas.idShort, aasId: aas.identification))
                                   .WithPayload(Newtonsoft.Json.JsonConvert.SerializeObject(aas))
                                   .WithExactlyOnceQoS()
                                   .WithRetainFlag(_diaData.MqttRetain)
                                   .Build();

                    await _mqttClient.PublishAsync(message);

                    //publish submodels
                    foreach (var sm in package.AasEnv.Submodels)
                    {
                        // whole structure
                        _logger?.Info("Publish first " + "Submodel_" + sm.idShort);

                        var message2 = new MqttApplicationMessageBuilder()
                                        .WithTopic(GenerateTopic(
                                            _diaData.FirstTopicSubmodel, defaultIfNull: "Submodel_" + sm.idShort,
                                            aasIdShort: aas.idShort, aasId: aas.identification,
                                            smIdShort: sm.idShort, smId: sm.identification))
                                       .WithPayload(Newtonsoft.Json.JsonConvert.SerializeObject(sm))
                                       .WithExactlyOnceQoS()
                                       .WithRetainFlag(_diaData.MqttRetain)
                                       .Build();

                        await _mqttClient.PublishAsync(message2);

                        // single values as well? 
                        if (_diaData.SingleValueFirstTime)
                            PublishSingleValues_FirstTimeSubmodel(aas, sm, sm.GetReference()?.Keys);
                    }
                }
            }

            _logger?.Info("Publish full events: " + _diaData.EnableEventPublish);
            _logger?.Info("Publish single values: " + _diaData.SingleValuePublish);
        }

        private void PublishSingleValues_FirstTimeSubmodel(
            AdminShell.AdministrationShell aas,
            AdminShell.Submodel sm,
            AdminShell.KeyList startPath)
        {
            // trivial
            if (aas == null || sm == null)
                return;

            // give this to (recursive) function
            sm.submodelElements?.RecurseOnSubmodelElements(null, null, (o, parents, sme) =>
            {
                // assumption is, the sme is now "leaf" of a SME-hierarchy
                if (sme is AdminShell.IEnumerateChildren)
                    return;

                // value of the leaf
                var valStr = sme.ValueAsText();

                // build a complete path of keys
                var path = startPath + parents.ToKeyList() + sme?.ToKey();
                var pathStr = path.BuildIdShortPath();

                // publish
                if (_diaData.LogDebug)
                    _logger?.Info("Publish single value (first time)");
                
                var msg = new MqttApplicationMessageBuilder()
                            .WithTopic(GenerateTopic(
                                _diaData.EventTopic, defaultIfNull: "SingleValue",
                                aasIdShort: aas.idShort, aasId: aas.identification,
                                smIdShort: sm.idShort, smId: sm.identification,
                                path: pathStr))
                            .WithPayload(valStr)
                            .WithExactlyOnceQoS()
                            .WithRetainFlag(_diaData.MqttRetain)
                            .Build();
                _mqttClient.PublishAsync(msg).GetAwaiter().GetResult();
            });                
        }

        private void PublishSingleValues_ChangeItem(
            AasEventMsgEnvelope ev, 
            AdminShell.ReferableRootInfo ri,            
            AdminShell.KeyList startPath,
            AasPayloadStructuralChangeItem ci)
        {
            // trivial
            if (ev == null || ci == null || startPath == null)
                return;

            // only specific reasons
            if (!(ci.Reason == AasPayloadStructuralChangeItem.ChangeReason.Create
                  || ci.Reason == AasPayloadStructuralChangeItem.ChangeReason.Modify))
                return;

            // need a payload
            if (ci.Path == null || ci.Data == null)
                return;

            var dataRef = ci.GetDataAsReferable();

            // give this to (recursive) function
            var messages = new List<MqttApplicationMessage>();
            if (dataRef is AdminShell.SubmodelElement dataSme)
            {
                var smwc = new AdminShell.SubmodelElementWrapperCollection(dataSme);
                smwc.RecurseOnSubmodelElements(null, null, (o, parents, sme) =>
                {
                    // assumption is, the sme is now "leaf" of a SME-hierarchy
                    if (sme is AdminShell.IEnumerateChildren)
                        return;

                    // value of the leaf
                    var valStr = sme.ValueAsText();

                    // build a complete path of keys
                    var path = startPath + ci.Path + parents.ToKeyList() + sme?.ToKey();
                    var pathStr = path.BuildIdShortPath();

                    // publish
                    if (_diaData.LogDebug)
                        _logger?.Info("Publish single value (create/ update)");
                    messages.Add(
                        new MqttApplicationMessageBuilder()
                            .WithTopic(GenerateTopic(
                                _diaData.EventTopic, defaultIfNull: "SingleValue",
                                aasIdShort: ri?.AAS?.idShort, aasId: ri?.AAS?.identification,
                                smIdShort: ri?.Submodel?.idShort, smId: ri?.Submodel?.identification,
                                path: pathStr))
                            .WithPayload(valStr)
                            .WithExactlyOnceQoS()
                            .WithRetainFlag(_diaData.MqttRetain)
                            .Build());                  
                });
            }

            // publish these
            // convert to synchronous behaviour
            foreach (var msg in messages)
                _mqttClient.PublishAsync(msg).GetAwaiter().GetResult();
        }

        private void PublishSingleValues_UpdateItem(
            AasEventMsgEnvelope ev,
            AdminShell.ReferableRootInfo ri,
            AdminShell.KeyList startPath,
            AasPayloadUpdateValueItem ui)
        {
            // trivial
            if (ev == null || ui == null || startPath == null || ui.Path == null)
                return;

            // value of the leaf
            var valStr = "" + ui.Value;

            // build a complete path of keys
            var path = startPath + ui.Path;
            var pathStr = path.BuildIdShortPath();

            // publish
            if (_diaData.LogDebug)
                _logger?.Info("Publish single value (update value)");
            var message = new MqttApplicationMessageBuilder()
                    .WithTopic(GenerateTopic(
                        _diaData.EventTopic, defaultIfNull: "SingleValue",
                        aasIdShort: ri?.AAS?.idShort, aasId: ri?.AAS?.identification,
                        smIdShort: ri?.Submodel?.idShort, smId: ri?.Submodel?.identification,
                        path: pathStr))
                    .WithPayload(valStr)
                    .WithExactlyOnceQoS()
                    .WithRetainFlag(_diaData.MqttRetain)
                    .Build();

            // publish
            _mqttClient.PublishAsync(message).GetAwaiter().GetResult();
        }

        public void PublishEventAsync(AasEventMsgEnvelope ev,
            AdminShell.ReferableRootInfo ri = null)
        {
            // access
            if (ev == null || _mqttClient == null || !_mqttClient.IsConnected)
                return;

            // serialize the event
            var settings = AasxIntegrationBase.AasxPluginOptionSerialization.GetDefaultJsonSettings(
                    new[] { typeof(AasEventMsgEnvelope) });
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.Formatting = Formatting.Indented;
            var json = JsonConvert.SerializeObject(ev, settings);

            // aas / sm already available in rootInfo, prepare idShortPath
            var sourcePathStr = "";
            var sourcePath = new AdminShell.KeyList();
            if (ev.Source?.Keys != null && ri != null && ev.Source.Keys.Count > ri.NrOfRootKeys)
            {
                sourcePath = ev.Source.Keys.SubList(ri.NrOfRootKeys);
                sourcePathStr = sourcePath.BuildIdShortPath();
            }

            var observablePathStr = "";
            var observablePath = new AdminShell.KeyList();
            if (ev.ObservableReference?.Keys != null && ri != null 
                && ev.ObservableReference.Keys.Count > ri.NrOfRootKeys)
            {
                observablePath = ev.ObservableReference.Keys.SubList(ri.NrOfRootKeys);
                observablePathStr = observablePath.BuildIdShortPath();
            }

            // publish the full event?
            if (_diaData.EnableEventPublish)
            {
                if (_diaData.LogDebug)
                    _logger?.Info("Publish Event");
                var message = new MqttApplicationMessageBuilder()
                               .WithTopic(GenerateTopic(
                                    _diaData.EventTopic, defaultIfNull: "Event",
                                    aasIdShort: ri?.AAS?.idShort, aasId: ri?.AAS?.identification,
                                    smIdShort: ri?.Submodel?.idShort, smId: ri?.Submodel?.identification,
                                    path: sourcePathStr))
                               .WithPayload(json)
                               .WithExactlyOnceQoS()
                               .WithRetainFlag(_diaData.MqttRetain)
                               .Build();

                // convert to synchronous behaviour
                _mqttClient.PublishAsync(message).GetAwaiter().GetResult();
            }

            // deconstruct the event into single units?
            if (_diaData.SingleValuePublish)
            {
                if (_diaData.LogDebug)
                    _logger?.Info("Publish single values ..");

                if (ev.Payloads != null)
                    foreach (var epl in ev.Payloads)
                    {
                        if (epl is AasPayloadStructuralChange apsc && apsc.Changes != null)
                            foreach (var ci in apsc.Changes)
                                PublishSingleValues_ChangeItem(ev, ri, observablePath, ci);

                        if (epl is AasPayloadUpdateValue apuv && apuv.Values != null)
                            foreach (var ui in apuv.Values)
                                PublishSingleValues_UpdateItem(ev, ri, observablePath, ui);
                    }
            }
        }

        public void PublishEvent(AasEventMsgEnvelope ev,
            AdminShell.ReferableRootInfo ri = null)
        {
            PublishEventAsync(ev, ri);
        }
    }
}
