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
            "{sm} = Submodel.idShort, {sm-id} = Submodel.identification" + Environment.NewLine;

        public string BrokerUrl = "localhost:1883";

        public bool EnableFirstPublish = true;
        public string FirstPublishTopic = "/aas/{aas}/";

        public bool EnableEventPublish = false;
        public string EventPublishTopic = "/aas/{aas}/";

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
            string smIdShort = null, AdminShell.Identification smId = null)
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

            return res;
        }

        public async Task StartAsync(
            AdminShellPackageEnv package,
            AnyUiDialogueDataMqttPublisher diaData,
            GrapevineLoggerSuper logger = null)
        {
            // first options
            _diaData = diaData;
            if (_diaData == null)
            {
                logger?.Error("Error: no options available.");
                return;
            }

            // broker?
            var hp = SplitBrokerUrl(_diaData.BrokerUrl);
            if (hp == null)
            {
                logger?.Error("Error: no broker URL available.");
                return;
            }
            logger?.Info($"Conneting broker {hp.Item1}:{hp.Item2} ..");

            // Create TCP based options using the builder.
            
            var options = new MqttClientOptionsBuilder()
                .WithClientId("AASXPackageXplorer MQTT Client")
                .WithTcpServer(hp.Item1, hp.Item2)
                .Build();

            //create MQTT Client and Connect using options above
            
            IMqttClient mqttClient = new MqttFactory().CreateMqttClient();
            await mqttClient.ConnectAsync(options);
            if (mqttClient.IsConnected)
                logger?.Info("### CONNECTED WITH SERVER ###");

            //publish AAS to AAS Topic

            if (_diaData.EnableFirstPublish)
            {
                foreach (AdminShell.AdministrationShell aas in package.AasEnv.AdministrationShells)
                {
                    logger?.Info("Publish AAS");
                    var message = new MqttApplicationMessageBuilder()
                                   .WithTopic(GenerateTopic(
                                        _diaData.FirstPublishTopic, defaultIfNull: "AAS",
                                        aasIdShort: aas.idShort, aasId: aas.identification))
                                   .WithPayload(Newtonsoft.Json.JsonConvert.SerializeObject(aas))
                                   .WithExactlyOnceQoS()
                                   .WithRetainFlag()
                                   .Build();

                    await mqttClient.PublishAsync(message);

                    //publish submodels
                    foreach (var sm in package.AasEnv.Submodels)
                    {
                        logger?.Info("Publish " + "Submodel_" + sm.idShort);

                        var message2 = new MqttApplicationMessageBuilder()
                                        .WithTopic(GenerateTopic(
                                            _diaData.FirstPublishTopic, defaultIfNull: "Submodel_" + sm.idShort,
                                            aasIdShort: aas.idShort, aasId: aas.identification,
                                            smIdShort: sm.idShort, smId: sm.identification))
                                       .WithPayload(Newtonsoft.Json.JsonConvert.SerializeObject(sm))
                                       .WithExactlyOnceQoS()
                                       .WithRetainFlag()
                                       .Build();

                        await mqttClient.PublishAsync(message2);
                    }
                }
            }
        }
    }
}
