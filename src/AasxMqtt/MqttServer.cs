/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using MQTTnet;
using MQTTnet.Server;

namespace AasxMqttServer
{
    class MqttServer
    {
        IMqttServer mqttServer;

        public MqttServer()
        {
            mqttServer = new MqttFactory().CreateMqttServer();
        }

        public async Task MqttSeverStartAsync()
        {
            //Start a MQTT server.
            await mqttServer.StartAsync(new MqttServerOptions());
        }

        public async Task MqttSeverStopAsync()
        {
            await mqttServer.StopAsync();
        }
    }
}
