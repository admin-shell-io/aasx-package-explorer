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

namespace AasxPluginAssetInterfaceDescription
{
    public enum AidIfxItemKind { Unknown, Property, Action, Event };

    public class AidIfxItemStatus
    {
        /// <summary>
        /// Which kind of information: Property, Action, Event.
        /// </summary>
        public AidIfxItemKind Kind = AidIfxItemKind.Unknown;

        /// <summary>
        /// Contains the hierarchy information, where the item is stored in hierarchy
        /// of the given interface (end-point).
        /// </summary>
        public string Location = "";

        /// <summary>
        /// Display name of the item. Could be from IdShort, key, title.
        /// </summary>
        public string DisplayName = "";

        /// <summary>
        /// Contains the forms information with all detailed information for the 
        /// technology.
        /// </summary>
        public CD_Forms FormData = null;

        /// <summary>
        /// String data for value incl. unit information.
        /// </summary>
        public string Value = "";

        /// <summary>
        /// Link to entity (property, action, event).
        /// </summary>
        public object Tag = null;
    }

    public enum AidInterfaceTechnology { HTTP, Modbus, MQTT }

    public class AidInterfaceStatus
    {
        /// <summary>
        /// Technology being used ..
        /// </summary>
        public AidInterfaceTechnology Technology = AidInterfaceTechnology.HTTP;

        /// <summary>
        /// Display name. Could be from SMC IdShort or title.
        /// Will be printed in bold.
        /// </summary>
        public string DisplayName = "";

        /// <summary>
        /// Further infornation. Printed in light appearence.
        /// </summary>
        public string Info = "";

        /// <summary>
        /// The information items (properties, actions, events)
        /// </summary>
        public List<AidIfxItemStatus> Items = new List<AidIfxItemStatus>();

        /// <summary>
        /// Base connect information.
        /// </summary>
        public string EndpointBase = "";

        /// <summary>
        /// Link to entity (interface).
        /// </summary>
        public object Tag = null;

        /// <summary>
        /// Holds the technology connection currently used.
        /// </summary>
        public AidModbusConnection Connection = null;
    }

    public class AidModbusConnection
    {
        public Uri TargetUri;

        public ModbusTcpClient Client;

        public DateTime LastActive = default(DateTime);

        public bool Open()
        {
            try
            {
                Client = new ModbusTcpClient();
                Client.Connect(new IPEndPoint(IPAddress.Parse(TargetUri.Host), TargetUri.Port));
                LastActive = DateTime.Now;
                return true;
            } catch (Exception ex)
            {
                Client = null;
                return false;
            }
        }

        public bool IsConnected()
        {
            return Client != null && Client.IsConnected;
        }

        public void Close()
        {
            if (IsConnected())
            {
                Client.Disconnect();
                Client = null;
            }
            else
            {
                Client = null;
            }
        }

        public void UpdateItemValue(AidModbusConnection conn, AidIfxItemStatus item)
        {
            // access
            if (item?.FormData?.Href?.HasContent() != true
                || item.FormData.Modbus_function?.HasContent() != true)
                return;

            // decode address + quantity
            // (assumption: 1 quantity = 2 bytes)
            var match = Regex.Match(item.FormData.Href, @"^(\d{1,5})(\?quantity=(\d+))?$");
            if (!match.Success)
                return;

            if (!int.TryParse(match.Groups[1].ToString(), out var address))
                return;
            if (!int.TryParse(match.Groups[3].ToString(), out var quantity))
                quantity = 1;
            quantity = Math.Max(0, Math.Min(0xffff, quantity));

            // perform function (id = in data)
            byte[] id = null;
            if (item.FormData.Modbus_function.Trim().ToLower() == "readholdingregisters")
            {
                // readHoldingRegisters
                id = conn.Client.ReadHoldingRegisters<byte>(99, address, quantity).ToArray();
                // time
                conn.LastActive = DateTime.Now;
            }

            // success with reading?
            if (id == null || id.Length < 1)
                return;

            // swapping (od = out data)
            // https://doc.iobroker.net/#de/adapters/adapterref/iobroker.modbus/README.md?wp
            var mbtp = item.FormData.Modbus_type?.ToLower().Trim();
            byte[] od = id.ToArray();
            if (quantity == 2)
            {
                // 32bit operation on AABBCCDD
                if (mbtp.EndsWith("be"))
                {
                    // big endian AABBCCDD => AABBCCDD
                    od[3] = id[3]; od[2] = id[2]; od[1] = id[1]; od[0] = id[0];
                }
                else
                if (mbtp.EndsWith("le"))
                {
                    // little endian AABBCCDD => DDCCBBAA
                    od[3] = id[0]; od[2] = id[1]; od[1] = id[2]; od[0] = id[3];
                }
                else
                if (mbtp.EndsWith("sw"))
                {
                    // Big Endian Word Swap AABBCCDD => CCDDAABB
                    od[3] = id[2]; od[2] = id[3]; od[1] = id[0]; od[0] = id[1];
                }
                else
                if (mbtp.EndsWith("sb"))
                {
                    // Big Endian Byte Swap AABBCCDD => DDCCBBAA
                    od[3] = id[0]; od[2] = id[1]; od[1] = id[2]; od[0] = id[3];
                }
            }
            else
            if (quantity == 1)
            {
                // 16bit operation on AABB
                if (mbtp.EndsWith("le"))
                {
                    // little endian AABB => BBAA
                    od[1] = id[0]; od[0] = id[1];
                }
            }

            // conversion to value
            // idea: (1) convert to binary type, (2) convert to adequate string representation
            var strval = "";
            if (mbtp.StartsWith("uint32") && quantity >= 2)
            {
                strval = BitConverter.ToUInt32(od).ToString();
            }
            else
            if (mbtp.StartsWith("int32") && quantity >= 2)
            {
                strval = BitConverter.ToInt32(od).ToString();
            }
            else
            if (mbtp.StartsWith("uint16") && quantity >= 1)
            {
                strval = BitConverter.ToUInt16(od).ToString();
            }
            else
            if (mbtp.StartsWith("int16") && quantity >= 1)
            {
                strval = BitConverter.ToInt16(od).ToString();
            }
            else
            if (mbtp.StartsWith("uint8") && quantity >= 1)
            {
                strval = Convert.ToByte(od[0]).ToString();
            }
            else
            if (mbtp.StartsWith("int8") && quantity >= 1)
            {
                strval = Convert.ToSByte(od[0]).ToString();
            }
            else
            if (mbtp.StartsWith("float") && quantity >= 2)
            {
                strval = BitConverter.ToSingle(od).ToString("R", CultureInfo.InvariantCulture);
            }
            else
            if (mbtp.StartsWith("double") && quantity >= 4)
            {
                strval = BitConverter.ToDouble(od).ToString("R", CultureInfo.InvariantCulture);
            }
            else
            if (mbtp.StartsWith("string") && quantity >= 1)
            {
                strval = BitConverter.ToString(od);
            }

            // save in item
            item.Value = strval;
        }
    }

    public class AidModbusConnections : Dictionary<Uri, AidModbusConnection>
    {
        public AidModbusConnection GetOrCreate(string target)
        {
            if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
                return null;
            if (this.ContainsKey(uri))
                return this[uri];

            var conn = new AidModbusConnection() { TargetUri = uri };
            return conn;
        }
    }

    /// <summary>
    /// Holds track of all current Aid interface status information.
    /// Idea: well be preset and updated by plug-in events.
    /// Will then allow technical connect to asset interfaces.
    /// Will exist **longer** than the invocation of just the plugin UI.
    /// </summary>
    public class AidAllInterfaceStatus
    {
        public List<AidInterfaceStatus> InterfaceStatus = new List<AidInterfaceStatus>();

        public AidModbusConnections ModbusConnections = new AidModbusConnections();

        /// <summary>
        /// Will connect to each target once, get values and will disconnect again.
        /// </summary>
        public void UpdateValuesSingleShot()
        {
            // Modbus
            // Open, connect and read all connections
            foreach (var ifc in InterfaceStatus.Where((i) => i.Technology == AidInterfaceTechnology.Modbus))
            {
                // get a connection
                if (ifc.EndpointBase?.HasContent() != true)
                    continue;
                var conn = ModbusConnections.GetOrCreate(ifc.EndpointBase);
                if (conn == null)
                    continue;

                // open it
                if (!conn.Open())
                    continue;
                ifc.Connection = conn;

                // go thru all items
                foreach (var item in ifc.Items)
                    conn.UpdateItemValue(conn, item);
            }

            // close all connections
            foreach (var ifc in InterfaceStatus.Where((i) => i.Technology == AidInterfaceTechnology.Modbus))
            {
                if (ifc.Connection?.IsConnected() == true)
                    ifc.Connection.Close();
            }
        }
    }

}
