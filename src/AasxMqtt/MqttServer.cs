using MQTTnet;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* For Mqtt Content:

MIT License

MQTTnet Copyright (c) 2016-2019 Christian Kratky
*/

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
            //var mqttServer = new MqttFactory().CreateMqttServer();
            await mqttServer.StartAsync(new MqttServerOptions());
            //Console.WriteLine("Press any key to exit.");
            //Console.ReadLine();
            //await mqttServer.StopAsync();
        }

        public async Task MqttSeverStopAsync()
        {
            await mqttServer.StopAsync();
        }
    }
}
