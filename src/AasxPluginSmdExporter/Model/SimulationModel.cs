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
using Newtonsoft.Json.Linq;
using Aas = AasCore.Aas3_0;
using AdminShellNS;

namespace AasxPluginSmdExporter
{
    public class SimulationModel
    {

        public string Name { get; set; }

        public JObject AsJson { get; set; }

        public String Fmu { get; set; }

        public List<IOput> Outputs { get; set; } = new List<IOput>();

        public List<IOput> Inputs { get; set; } = new List<IOput>();

        public List<IOput> PhysicalPorts { get; set; } = new List<IOput>();

        public Aas.IReference SemanticId { get; set; }

        public Dictionary<string, Mapping> Mappings { get; set; }

        public double Multfactor { get; set; }

        public bool IsNode { get; set; } = false;

        public static List<string> Warnings { get; set; } = new List<string>();

        public SimulationModel()
        {
            Mappings = new Dictionary<string, Mapping>();
            Multfactor = 1;
        }

        /// <summary>
        /// Sets the outputs for the SimulationModel
        /// </summary>
        /// <param name="token"></param>
        private void setOutputs(JToken token)
        {
            if (token.SelectToken("value") != null)
            {
                JArray outs = (JArray)token["value"];

                foreach (var singleout in outs)
                {
                    Outputs.Add(IOput.Parse(singleout, Name));
                }
            }
        }

        /// <summary>
        /// Sets the inputs for the SimulationModel
        /// </summary>
        /// <param name="token"></param>
        private void setInputs(JToken token)
        {
            if (token.SelectToken("value") != null)
            {
                JArray ins = (JArray)token["value"];

                foreach (var singlein in ins)
                {
                    Inputs.Add(IOput.Parse(singlein, Name));
                }
            }
        }

        /// <summary>
        /// Sets the physical ports
        /// </summary>
        /// <param name="token"></param>
        private void setPhysicalPorts(JToken token)
        {
            if (token.SelectToken("value") != null)
            {
                JArray ports = (JArray)token["value"];

                foreach (var port in ports)
                {
                    PhysicalPorts.Add(IOput.Parse(port, Name, true));
                }
            }
        }

        /// <summary>
        /// Sets the mappings of the SimulationModel if there are any.
        /// </summary>
        /// <param name="mappings"></param>
        private void setMappings(JArray mappings)
        {
            foreach (var mappelement in mappings)
            {
                JToken locunknownid = mappelement.SelectToken("constraints[0].value");
                JToken locbasicid = mappelement.SelectToken("constraints[1].value");
                if (locunknownid != null &&
                    mappelement.SelectToken("idShort") != null &&
                    locbasicid != null &&
                    mappelement.SelectToken("value") != null)
                {
                    string unknownId = locunknownid.ToString();
                    string idShort = (string)mappelement["idShort"];
                    string basicId = locbasicid.ToString();
                    double value = Convert.ToDouble((string)mappelement["value"]);
                    Mapping mapping = new Mapping(idShort, unknownId, basicId, value);
                    Mappings.Add(unknownId, mapping);
                }

            }
        }

        /// <summary>
        /// For a given Jtoken wich represants a simulationmodel this method returns the type
        /// </summary>
        /// <param name="jToken"></param>
        /// <returns></returns>
        private String getType(JToken jToken)
        {
            if (jToken.SelectToken("value") != null)
            {
                foreach (var submodels in jToken["value"])
                {
                    JToken idShort = submodels["idShort"];
                    if (idShort != null)
                    {
                        if (idShort.ToString().Contains("ports"))
                        {
                            JToken submodelvalue = submodels["value"];
                            if (submodelvalue != null)
                            {
                                foreach (var values in submodelvalue)
                                {
                                    JToken valueidshort = values.SelectToken("idShort");
                                    JToken valuevalue = values["value"];
                                    if (valueidshort != null && valuevalue != null)
                                    {
                                        if (valueidshort.ToString().Equals("simType"))
                                        {
                                            return valuevalue.ToString();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }



        /// <summary>
        /// Sets the FMU path and returns true or if no FMU is found sets the FMU to null and returns false
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private void setFmu(JObject json)
        {
            JArray submodels = (JArray)json["submodelElements"];
            JToken tokenPhysical = null;
            JToken tokenSignal = null;
            bool foundNoPhysical = true;
            bool foundNoSignal = true;

            foreach (var submodel in submodels)
            {
                string simModelType = getType(submodel);
                if (simModelType != null)
                {

                    if (Exporter.forcePhysicalConnections)
                    {
                        if (simModelType.Equals("physical"))
                        {
                            setPortsAndFmu(submodel);
                            foundNoPhysical = false;
                        }
                        else if (simModelType.Equals("signalflow") && foundNoPhysical)
                        {
                            tokenPhysical = submodel;
                        }
                    }
                    else
                    {
                        if (simModelType.Equals("signalflow"))
                        {
                            setPortsAndFmu(submodel);
                            foundNoSignal = false;
                        }
                        else if (simModelType.Equals("physical") && foundNoPhysical)
                        {
                            tokenSignal = submodel;
                        }
                    }
                }
            }



            if (foundNoPhysical && Exporter.forcePhysicalConnections && tokenPhysical != null)
            {
                setPortsAndFmu(tokenPhysical);
                Warnings.Add("There is no physical simulationmodel for " + this.Name);
            }

            if (foundNoSignal && !Exporter.forcePhysicalConnections && tokenSignal != null)
            {
                setPortsAndFmu(tokenSignal);
                Warnings.Add("There is no signalflow simulationmodel for " + this.Name);
            }

        }

        /// <summary>
        /// Sets the ports and the fmu by using the jtoken
        /// </summary>
        /// <param name="jToken"></param>
        private void setPortsAndFmu(JToken jToken)
        {
            if (jToken.SelectToken("value") != null)
            {
                foreach (var submdodels_ports in jToken["value"])
                {
                    JToken locTokenPorts = submdodels_ports["value"];
                    if (locTokenPorts != null)
                    {
                        foreach (var submodel_ports_submodels in locTokenPorts)
                        {
                            JToken locToken = submodel_ports_submodels["idShort"];
                            if (locToken != null)
                            {
                                if (locToken.ToString().Contains("FMU"))
                                {
                                    this.Fmu = (string)submodel_ports_submodels["value"];
                                }
                                else if (locToken.ToString().Equals("in"))
                                {
                                    JToken inputs = submodel_ports_submodels;
                                    this.setInputs(inputs);
                                }
                                else if (locToken.ToString().Equals("out"))
                                {
                                    JToken outputs = submodel_ports_submodels;
                                    this.setOutputs(outputs);
                                }
                                else if (locToken.ToString().Equals("physical"))
                                {
                                    JToken outputs = submodel_ports_submodels;
                                    this.setPhysicalPorts(outputs);
                                }
                                else if (locToken.ToString().Equals("mappings"))
                                {
                                    JArray mappings = (JArray)submodel_ports_submodels["value"];
                                    setMappings(mappings);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the sematic id
        /// </summary>
        private void setSemanticId()
        {
            if (BillOfMaterial.SemanticIdDict.ContainsKey(this.Name))
            {
                SemanticId = BillOfMaterial.SemanticIdDict[this.Name];
            }
            else
            {
            }
        }

        /// <summary>
        /// Returns a list of inputs with the given interface
        /// </summary>
        /// <param name="interfaceName"></param>
        /// <returns></returns>
        public List<IOput> GetInputsWithInterfaceName(string interfaceName)
        {
            List<IOput> list = new List<IOput>();
            foreach (var input in this.Inputs)
            {
                if (input.InterfaceName.Equals(interfaceName))
                    list.Add(input);
            }

            return list;
        }

        /// <summary>
        /// Returns a list of outputs with the given interface
        /// </summary>
        /// <param name="interfaceName"></param>
        /// <returns></returns>
        public List<IOput> GetOutputsWithInterfaceName(string interfaceName)
        {
            List<IOput> list = new List<IOput>();
            foreach (var output in this.Outputs)
            {
                if (output.InterfaceName.Equals(interfaceName))
                    list.Add(output);
            }

            return list;
        }

        /// <summary>
        /// Returns all physical ports of this simulationmodel with the given interface name.
        /// </summary>
        /// <param name="interfaceName"></param>
        /// <returns></returns>
        public List<IOput> GetPhysicalPortsWithInterfaceName(string interfaceName)
        {
            List<IOput> list = new List<IOput>();
            foreach (var physicalPort in this.PhysicalPorts)
            {
                if (physicalPort.InterfaceName.Equals(interfaceName))
                    list.Add(physicalPort);
            }

            return list;
        }

        /// <summary>
        /// Parses the JObject into an SimulationModel and returns the new created SimulationModel
        /// </summary>
        /// <param name="json"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static SimulationModel Parse(JObject json, String name)
        {
            if (json == null)
            {
                return new SimulationModel();
            }

            SimulationModel simulation = new SimulationModel();
            simulation.Name = name;
            simulation.AsJson = json;
            simulation.setFmu(json);
            simulation.setSemanticId();

            return simulation;
        }

    }
}
