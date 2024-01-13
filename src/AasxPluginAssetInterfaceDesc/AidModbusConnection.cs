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

namespace AasxPluginAssetInterfaceDescription
{
    public class AidModbusConnection : AidBaseConnection
    {
        public ModbusTcpClient Client;

        override public bool Open()
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

        override public bool IsConnected()
        {
            return Client != null && Client.IsConnected;
        }

        override public void Close()
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

        // Note: the async version of ReadHoldingRegisters seems not to work properly?
        override public int UpdateItemValue(AidIfxItemStatus item)
        {
            // access
            if (item?.FormData?.Href?.HasContent() != true
                || item.FormData.Modbus_function?.HasContent() != true)
                return 0;
            int res = 0;

            // decode address + quantity
            // (assumption: 1 quantity = 2 bytes)
            var match = Regex.Match(item.FormData.Href, @"^(\d{1,5})(\?quantity=(\d+))?$");
            if (!match.Success)
                return 0;

            if (!int.TryParse(match.Groups[1].ToString(), out var address))
                return 0;
            if (!int.TryParse(match.Groups[3].ToString(), out var quantity))
                quantity = 1;
            quantity = Math.Max(0, Math.Min(0xffff, quantity));

            // perform function (id = in data)
            byte[] id = null;
            if (item.FormData.Modbus_function.Trim().ToLower() == "readholdingregisters")
            {
                // readHoldingRegisters
                id = (Client.ReadHoldingRegisters<byte>(99, address, 2 * quantity)).ToArray();
                // time
                LastActive = DateTime.Now;
            }

            // success with reading?
            if (id == null || id.Length < 1)
                return 0;

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

            // ok
            return 1;
        }
    }
}
