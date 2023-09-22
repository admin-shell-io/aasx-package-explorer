/*
Copyright (c) 2021 KEB Automation KG <https://www.keb.de/>,
Copyright (c) 2021 Lenze SE <https://www.lenze.com/en-de/>,
author: Jonas Grote, Denis Göllner, Sebastian Bischof

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Aas = AasCore.Aas3_0;
using AdminShellNS;

namespace AasxPluginSmdExporter
{
    public class IOput
    {

        public string Owner { get; set; }

        public string EclassID { get; set; }

        public string IdShort { get; set; }

        public string Unit { get; set; }

        public string Domain { get; set; }

        public string Datatype { get; set; }

        public List<IOput> ConnectedTo { get; set; } = new List<IOput>();

        public Aas.IReference SemanticId { get; set; }

        public string ConceptDescription { get; set; }

        public string InterfaceName { get; set; }

        public bool IsPhysical { get; set; } = false;

        public IOput()
        {

        }

        public IOput(string idShort, string owner)
        {
            this.IdShort = idShort;
            this.Owner = owner;
            this.Unit = "";
            this.Domain = "";
        }

        public IOput(string idShort, string owner, Aas.IReference semanticId)
        {
            this.IdShort = idShort;
            this.Owner = owner;
            this.SemanticId = semanticId;
            this.Unit = "";
            this.Domain = "";
        }
        /// <summary>
        /// Sets the eclass id 
        /// </summary>
        /// <param name="value_IO"></param>
        public void SetEClass(JToken value_IO)
        {
            JToken locToken = value_IO.SelectToken("semanticId.keys[0].value");
            if (locToken != null)
            {
                this.EclassID = locToken.ToString();
                this.Unit = EClass.GetUnitForEclassId(this.EclassID);
            }
            else
            {
                EclassID = "";
            }
        }

        /// <summary>
        /// Sets the id short
        /// </summary>
        /// <param name="token"></param>
        public void SetIdShort(JToken token)
        {
            if (token.SelectToken("idShort") != null)
            {
                this.IdShort = (string)token["idShort"];
            }
            else
            {
                IdShort = "";
            }
        }

        public void SetUnit(JToken token)
        {


        }

        /// <summary>
        /// Sets the domain
        /// </summary>
        /// <param name="token"></param>
        public void SetDomain(JToken token)
        {
            JToken locToken = token.SelectToken("constraints[0].value");
            if (locToken != null)
            {
                Domain = locToken.ToString();
                Domain = Domain.ToLower();
            }
            else
            {
                Domain = "";
            }


        }

        public void SetInterfaceName(JToken token)
        {
            if (token.SelectToken("value.keys[1].value") != null && token.SelectToken("value.keys[0].value") != null)
            {
                InterfaceName = this.Owner + ((string)token.SelectToken("value.keys[1].value"));
            }
            else
            {
                InterfaceName = "";
            }
        }

        /// <summary>
        /// Sets the datatype
        /// </summary>
        /// <param name="token"></param>
        public void SetDatatype(JToken token)
        {
            JToken locToken = token.SelectToken("constraints[1].value");
            if (locToken != null)
            {
                Datatype = locToken.ToString();
            }
            else
            {
                Datatype = "";
            }
        }

        public void SetSemanticId(JToken token)
        {
            if (token.SelectToken("semanticId") != null)
            {
                var jsonStr = token["semanticId"].ToString();

                this.SemanticId = Aas.Jsonization.Deserialize.ReferenceFrom(
                    System.Text.Json.Nodes.JsonNode.Parse(jsonStr));

                this.ConceptDescription = this.SemanticId?.Keys[0].Value;
            }
            else
            {
                EclassID = "";
            }
        }

        /// <summary>
        /// Removes the connection to the given port, in case there is one.
        /// </summary>
        /// <param name="connectedPort"></param>
        public void RemoveConnection(IOput connectedPort)
        {
            if (connectedPort != null && !this.IsPhysical)
            {
                this.ConnectedTo.Remove(connectedPort);
                connectedPort.ConnectedTo.Remove(this);
            }
        }

        /// <summary>
        /// Adds a connetion to the given port.
        /// </summary>
        /// <param name="portToConnect"></param>
        public void AddConnection(IOput portToConnect)
        {
            if (portToConnect != null)
            {
                this.ConnectedTo.Add(portToConnect);
                portToConnect.ConnectedTo.Add(this);

            }
        }

        /// <summary>
        /// Creates connections between physical ports. All ports which are connected share the same ConnectedTo
        /// Port list.
        /// </summary>
        /// <param name="portToConnect"></param>
        public void AddPhysicalConnection(IOput portToConnect)
        {


            if (this.ConnectedTo.Count == 0 && portToConnect.ConnectedTo.Count == 0)
            {
                this.ConnectedTo.Add(portToConnect);
                this.ConnectedTo.Add(this);
                portToConnect.ConnectedTo = this.ConnectedTo;
            }
            else if (this.ConnectedTo.Count != 0 && portToConnect.ConnectedTo.Count == 0)
            {
                this.ConnectedTo.Add(portToConnect);
                portToConnect.ConnectedTo = this.ConnectedTo;
            }
            else if (this.ConnectedTo.Count == 0 && portToConnect.ConnectedTo.Count != 0)
            {
                portToConnect.ConnectedTo.Add(this);
                this.ConnectedTo = portToConnect.ConnectedTo;
            }
            else if (this.ConnectedTo.Count != 0 && portToConnect.ConnectedTo.Count != 0)
            {
                List<IOput> ports = this.ConnectedTo.Concat(portToConnect.ConnectedTo).ToList();

                foreach (var port in ports)
                {
                    port.ConnectedTo = ports;
                }

            }


        }

        /// <summary>
        /// Parses the given JToken into an IOput object
        /// </summary>
        /// <param name="token"></param>
        /// <param name="ownerName"></param>
        /// <param name="isPhysical"></param>
        /// <returns></returns>
        public static IOput Parse(JToken token, string ownerName, bool isPhysical = false)
        {
            IOput ioput = new IOput();
            ioput.Owner = ownerName;
            ioput.IsPhysical = isPhysical;
            ioput.SetEClass(token);

            ioput.SetIdShort(token);
            ioput.SetUnit(token);
            ioput.SetDomain(token);
            ioput.SetDatatype(token);
            ioput.SetSemanticId(token);
            ioput.SetInterfaceName(token);


            return ioput;
        }

    }
}
