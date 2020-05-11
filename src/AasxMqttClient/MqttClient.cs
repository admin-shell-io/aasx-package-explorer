using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
   Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
   Copyright (c) 2019 Fraunhofer IOSB-INA Lemgo, eine rechtlich nicht selbständige Einrichtung der Fraunhofer-Gesellschaft
    zur Förderung der angewandten Forschung e.V. <florian.pethig@iosb-ina.fraunhofer.de>, author: Florian Pethig

   This software is licensed under the Eclipse Public License 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
*/

/* For Mqtt Content:

MIT License

MQTTnet Copyright (c) 2016-2019 Christian Kratky
*/

namespace AasxMqttClient
{
    public class MqttClient
    {        
        public MqttClient()
        {
             
        }

        public static async Task StartAsync(AdminShellPackageEnv package, GrapevineLoggerSuper logger = null)
        {                      
            // Create TCP based options using the builder.
            var options = new MqttClientOptionsBuilder()
                .WithClientId("AASXPackageXplorer MQTT Client")
                .WithTcpServer("localhost", 1883)
                .Build();

            //create MQTT Client and Connect using options above
            IMqttClient mqttClient = new MqttFactory().CreateMqttClient();
            await mqttClient.ConnectAsync(options);
            if(mqttClient.IsConnected == true)
                logger.Info("### CONNECTED WITH SERVER ###");

            //publish AAS to AAS Topic
            foreach(AdminShell.AdministrationShell aas in package.AasEnv.AdministrationShells)
            {
                logger.Info("Publish AAS");
                var message = new MqttApplicationMessageBuilder()
                               .WithTopic("AAS")
                               .WithPayload(Newtonsoft.Json.JsonConvert.SerializeObject(aas))
                               .WithExactlyOnceQoS()
                               .WithRetainFlag()
                               .Build();

                await mqttClient.PublishAsync(message);

                //publish submodels
                foreach (var sm in package.AasEnv.Submodels)
                {
                    logger.Info("Publish " + "Submodel_" + sm.idShort);

                    var message2 = new MqttApplicationMessageBuilder()
                                   .WithTopic("Submodel_" + sm.idShort)
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
