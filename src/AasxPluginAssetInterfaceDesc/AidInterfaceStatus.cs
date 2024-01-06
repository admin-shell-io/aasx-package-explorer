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
        public AidBaseConnection Connection = null;
    }

    public class AidBaseConnection
    {
        public Uri TargetUri;

        public DateTime LastActive = default(DateTime);

        virtual public bool Open()
        {
            return false;
        }

        virtual public bool IsConnected()
        {
            return false;
        }

        virtual public void Close()
        {
        }

        virtual public void UpdateItemValue(AidIfxItemStatus item)
        {
        }
    }

    public class AidGenericConnections<T> : Dictionary<Uri, T> where T : AidBaseConnection, new()
    {
        public T GetOrCreate(string target)
        {
            if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
                return null;
            if (this.ContainsKey(uri))
                return this[uri];

            var conn = new T() { TargetUri = uri };
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
        public bool[] UseTech = { true, false, false };

        public List<AidInterfaceStatus> InterfaceStatus = new List<AidInterfaceStatus>();

        public AidGenericConnections<AidHttpConnection> HttpConnections =
            new AidGenericConnections<AidHttpConnection>();

        public AidGenericConnections<AidModbusConnection> ModbusConnections = 
            new AidGenericConnections<AidModbusConnection>();

        /// <summary>
        /// Will connect to each target once, get values and will disconnect again.
        /// </summary>
        public void UpdateValuesSingleShot()
        {
            foreach (var tech in AdminShellUtil.GetEnumValues<AidInterfaceTechnology>())
            {
                // use?
                if (!UseTech[(int)tech])
                    continue;

                // find all interfaces with that technology
                foreach (var ifc in InterfaceStatus.Where((i) => i.Technology == tech))
                {
                    // get a connection
                    if (ifc.EndpointBase?.HasContent() != true)
                        continue;

                    // find connection by factory
                    AidBaseConnection conn = null;
                    switch (tech)
                    {
                        case AidInterfaceTechnology.HTTP:
                            conn = HttpConnections.GetOrCreate(ifc.EndpointBase);
                            break;

                        case AidInterfaceTechnology.Modbus:
                            conn = ModbusConnections.GetOrCreate(ifc.EndpointBase);
                            break;
                    }
                    if (conn == null)
                        continue;

                    // open it
                    if (!conn.Open())
                        continue;
                    ifc.Connection = conn;

                    // go thru all items
                    foreach (var item in ifc.Items)
                        conn.UpdateItemValue(item);
                }
            }

            // close all connections
            foreach (var ifc in InterfaceStatus)
            {
                if (ifc.Connection?.IsConnected() == true)
                    ifc.Connection.Close();
            }
        }
    }

}
