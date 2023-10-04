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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AasxPluginSmdExporter.Model;
using Newtonsoft.Json.Linq;
using Aas = AasCore.Aas3_0;
using AdminShellNS;

namespace AasxPluginSmdExporter
{
    public class Exporter
    {

        public BillOfMaterial BillOfMaterial { get; set; }

        public Dictionary<string, SimulationModel> SimulationsModels { get; set; }

        public List<RelationshipElement> RelationshipElements { get; set; }

        public List<SummingPoint> SummingPoints { get; set; }

        public static bool forcePhysicalConnections = false;

        public Exporter()
        {

        }

        public Exporter(bool forcePhysicalConnections)
        {
            Exporter.forcePhysicalConnections = forcePhysicalConnections;
        }

        /// <summary>
        /// Creates a SMD by reading the BillOfMaterial and getting the RelationshipElements and 
        /// collecting the SimulationModels for the Relations in a list which is a property of the class
        /// </summary>
        /// <param name="host"></param>
        /// <param name="maschine"></param>
        public bool CreateSMD(String host, String maschine)
        {
            AASRestClient.Start(host);

            // Einlesen der billofmaterial der maschine und der zugehörigen RelElement
            BillOfMaterial = AASRestClient.GetBillofmaterialWithRelationshipElements(maschine);

            if (BillOfMaterial == null)
            {
                return false;
            }

            RelationshipElements = BillOfMaterial.RelationshipElements;
            foreach (var bom in BillOfMaterial.BillOfMaterials.Values)
            {
                RelationshipElements = RelationshipElements.Concat(bom.RelationshipElements).ToList();
            }



            foreach (var rel in RelationshipElements)
            {
                if (rel == null) return false;
            }

            DeleteDuplicateRelationshipsElements();

            // Einlesen der beiden simModelle für die einzelnen reletionships elemente in ein Dictionary
            SimulationsModels = GetSimulationModelsFromRelationshipElements();

            return true;

        }

        /// <summary>
        /// Deletes Relationshipelements from the RelationshipElements list which are duplicates.
        /// </summary>
        private void DeleteDuplicateRelationshipsElements()
        {
            // durchgehen a

            List<RelationshipElement> relList = new List<RelationshipElement>();

            foreach (var rel in RelationshipElements)
            {
                if (!ContainsRelationBetween(relList, rel.ValueSecond, rel.ValueFirst))
                {
                    relList.Add(rel);
                }
            }

            RelationshipElements = relList;

        }

        /// <summary>
        /// Checks whether or not there is a relation between the element first and the element second in the given list
        /// </summary>
        /// <param name="list"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public bool ContainsRelationBetween(List<RelationshipElement> list, string first, string second)
        {
            if (first == null || second == null) return false;
            foreach (var rel in list)
            {
                if (rel.ValueFirst.Equals(first) && rel.ValueSecond.Equals(second) ||
                    rel.ValueFirst.Equals(second) && rel.ValueSecond.Equals(first))
                {
                    return true;
                }
            }
            return false;
        }



        /// <summary>
        /// Returns a list of all SimulationModels for which no fmu was found
        /// </summary>
        /// <returns></returns>
        public List<SimulationModel> GetSimulationModelsWithoutFMU()
        {
            List<SimulationModel> simulationModelsNoFmu = new List<SimulationModel>();
            foreach (var key in SimulationsModels.Keys)
            {
                SimulationModel simulationModel = SimulationsModels[key];
                if (simulationModel.Fmu == null)
                {
                    simulationModelsNoFmu.Add(simulationModel);
                }
            }
            return simulationModelsNoFmu;
        }

        /// <summary>
        /// Gets the Simulationmodels for the Relations in RelationshipElements and returns them.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, SimulationModel> GetSimulationModelsFromRelationshipElements()
        {
            Dictionary<string, SimulationModel> simulationsModels = new Dictionary<string, SimulationModel>();

            foreach (var relationshipElement in RelationshipElements)
            {

                if (!simulationsModels.ContainsKey(relationshipElement.ValueFirst))
                {

                    addSimulationModelToList(simulationsModels, relationshipElement.ValueFirst);

                }
                if (!simulationsModels.ContainsKey(relationshipElement.ValueSecond))
                {

                    addSimulationModelToList(simulationsModels, relationshipElement.ValueSecond);

                }

            }

            return simulationsModels;
        }

        /// <summary>
        /// Gets the simulationmodel with the given name and adds it to the given list
        /// </summary>
        /// <param name="simulationsModels"></param>
        /// <param name="name"></param>
        private void addSimulationModelToList(Dictionary<string, SimulationModel> simulationsModels, string name)
        {
            SimulationModel simModel = AASRestClient.GetSimulationModel(name);



            if (simModel.AsJson != null)
            {
                simulationsModels.Add(name, simModel);
            }
            else
            {

            }
        }

        /// <summary>
        /// Compares the domains of the ports of the simulationmodels belonging to the given relationshiElement
        /// and connects if the domains are the same.
        /// If atleast one connetion is foudn true is returned.
        /// </summary>
        /// <param name="relationshipElement"></param>
        /// <param name="firstEle"></param>
        /// <param name="secondEle"></param>
        /// <param name="interfaceFirst"></param>
        /// <param name="interFaceSecond"></param>
        /// <returns></returns>
        public bool CheckCompatibleDomainsAndConnect(RelationshipElement relationshipElement,
            string firstEle,
            string secondEle,
            string interfaceFirst,
            string interFaceSecond)
        {

            if (SimulationsModels.ContainsKey(firstEle) && SimulationsModels.ContainsKey(secondEle))
            {
                bool foundConnection = false;
                // Holen der Liste der Input Ports von ele1
                List<IOput> inputsFirst = SimulationsModels[firstEle].GetInputsWithInterfaceName(interfaceFirst);
                // Hohlen der Liste der Input Ports von ele2
                List<IOput> inputsSecond = SimulationsModels[secondEle].GetInputsWithInterfaceName(interFaceSecond);
                // Holen der Outputs von ele1
                List<IOput> outputsFirst = SimulationsModels[firstEle].GetOutputsWithInterfaceName(interfaceFirst);
                // Holen der Outputs von ele2
                List<IOput> outputsSecond = SimulationsModels[secondEle].GetOutputsWithInterfaceName(interFaceSecond);

                List<IOput> physicalFirst
                    = SimulationsModels[firstEle].GetPhysicalPortsWithInterfaceName(interfaceFirst);

                List<IOput> physicalSecond
                    = SimulationsModels[secondEle].GetPhysicalPortsWithInterfaceName(interFaceSecond);

                foundConnection = connectInputsAndOutputsByDomain(physicalFirst, physicalSecond);
                foundConnection = connectInputsAndOutputsByDomain(inputsFirst, outputsSecond) || foundConnection;
                foundConnection = connectInputsAndOutputsByDomain(inputsSecond, outputsFirst) || foundConnection;
                // Erstellen einer Liste, welche die Verbindungen enthält
                return foundConnection;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Connects the ports of the inputs list with the ports of the outputs list in case the domains are the same.
        /// Returns true in case at least one connection is found.
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        /// <returns></returns>
        private bool connectInputsAndOutputsByDomain(List<IOput> inputs, List<IOput> outputs)
        {
            bool foundConnection = false;
            foreach (var input in inputs)
            {


                foreach (var output in outputs)
                {

                    if (input.Domain.Equals(output.Domain))
                    {

                        if (output.IsPhysical)
                        {
                            if (EClass.CheckEClassConnection(output.EclassID, input.EclassID) ||
                                EClass.CheckEClassConnection(input.EclassID, output.EclassID))
                            {
                                output.AddPhysicalConnection(input);
                                foundConnection = true;
                            }

                        }
                        else
                        {

                            input.AddConnection(output);

                            foundConnection = true;
                        }

                    }
                }


            }
            return foundConnection;
        }

        /// <summary>
        /// Checks the Datatypes of the connected In and Outputs and 
        /// returns a list of strings with the names and 
        /// the datatype of the in and output
        /// which are connected but do not share the same datatype.
        /// </summary>
        /// <param name="relationshipElement"></param>
        /// <param name="firstEle"></param>
        /// <param name="secondEle"></param>
        /// <returns></returns>
        public List<string> ReturnListWithMessageForDifferentDatatypes(RelationshipElement relationshipElement,
            string firstEle,
            string secondEle)
        {
            List<String> sameDatatype = new List<string>();

            if (SimulationsModels.ContainsKey(firstEle) && SimulationsModels.ContainsKey(secondEle))
            {
                foreach (var input in SimulationsModels[firstEle].Inputs.Concat(SimulationsModels[secondEle].Inputs))
                {

                    foreach (var connection in input.ConnectedTo)
                    {
                        if (!input.Datatype.Equals(connection.Datatype))
                        {
                            sameDatatype.Add($"{input.IdShort}({input.Datatype})" +
                                $"and {connection.IdShort}({connection.Datatype})" +
                                $"are connected but do not share the same datatype");

                        }
                    }

                }

                return sameDatatype;
            }
            else
            {
                return sameDatatype;
            }


        }

        /// <summary>
        /// Checks the EClass ids of all connected in and output ports
        /// If they are not compatible the connection gets removed
        /// Returns a list of strings with the name, eclass id and 
        /// unit of the in and output ports of all removed connections
        /// </summary>
        /// <param name="relationshipElement"></param>
        /// <param name="firstEle"></param>
        /// <param name="secondEle"></param>
        /// <returns></returns>
        public List<string> CheckeClassConnectionAndRemoveIncompatible(RelationshipElement relationshipElement,
            string firstEle,
            string secondEle)
        {
            EClass eClass = new EClass();

            List<string> changed = new List<string>();

            if (SimulationsModels.ContainsKey(firstEle) && SimulationsModels.ContainsKey(secondEle))
            {
                foreach (var input in SimulationsModels[firstEle].Inputs.Concat(SimulationsModels[secondEle].Inputs))
                {
                    for (int i = input.ConnectedTo.Count - 1; i >= 0; i--)
                    {
                        var connection = input.ConnectedTo[i];

                        if (SimulationsModels[input.Owner].Mappings.Count != 0)
                        {
                            Console.WriteLine("---Mappings of " + input.Owner);
                            foreach (var map in SimulationsModels[input.Owner].Mappings.Values)
                            {
                                Console.WriteLine(map.BasicId + " " + map.UnknownId);
                            }
                        }

                        // Search for compatible Eclass IDs
                        if (EClass.CheckEClassConnection(input.EclassID, connection.EclassID) ||
                            checkForMapping(input, connection, eClass))
                        {


                        }
                        // Not Compatible Eclass ids and connection gets removed.
                        else
                        {
                            input.RemoveConnection(connection);
                            changed.Add(input.IdShort + $"({input.EclassID})({input.Unit}) and " +
                                connection.IdShort +
                                $"({connection.EclassID})({connection.Unit})" +
                                $"are connected but do not have compatible Eclass ID");
                        }
                    }
                }


            }

            return changed;

        }

        /// <summary>
        /// Checks for mappings for the in or output port and returns true in case there is a mapping.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="eClass"></param>
        /// <returns></returns>
        private bool checkForMapping(IOput input, IOput output, EClass eClass)
        {

            SimulationModel simulationModel = SimulationsModels[input.Owner];
            bool result = simulationModel.Mappings.ContainsKey(input.EclassID) &&
            EClass.CheckEClassConnection(simulationModel.Mappings[input.EclassID].BasicId, output.EclassID);
            // Check output ports only if mapping was not already found in input ports
            if (result == false)
            {
                simulationModel = SimulationsModels[output.Owner];
                result = simulationModel.Mappings.ContainsKey(output.EclassID) &&
                EClass.CheckEClassConnection(input.EclassID, simulationModel.Mappings[output.EclassID].BasicId);
            }
            return result;
        }





        /// <summary>
        /// Tries to Put a new SMD and returns the success result.
        /// </summary>
        /// <returns></returns>
        public bool PutSMD()
        {
            SMD smd = new SMD(RelationshipElements, SimulationsModels.Values.ToList<SimulationModel>());
            return smd.PutSMD();
        }


        /// <summary>
        /// Returns true when there is a port of relationship elment one which is connected to a port
        /// of relationship element two
        /// </summary>
        /// <param name="rel"></param>
        /// <returns></returns>
        public bool RelationHasConnection(RelationshipElement rel)
        {
            // There is no simulationmodel
            if (!SimulationsModels.ContainsKey(rel.ValueFirst) || !SimulationsModels.ContainsKey(rel.ValueSecond))
            {
                return true;
            }

            SimulationModel firstSim = SimulationsModels[rel.ValueFirst];
            SimulationModel secondSim = SimulationsModels[rel.ValueSecond];

            // There are Relations which are not necassary 
            if (!rel.IsCertainConnection || firstSim.Inputs.Count == 0 || secondSim.Inputs.Count == 0)
            {
                return true;
            }

            // Checking if there is at least one connection
            if (hasConnectionToOtherValue(firstSim.Inputs, rel.ValueSecond) ||
                hasConnectionToOtherValue(secondSim.Inputs, rel.ValueFirst) ||
                hasConnectionToOtherValue(firstSim.PhysicalPorts, rel.ValueSecond))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given ports list contains a port which has a owner with a name which equals value.
        /// </summary>
        /// <param name="ports"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool hasConnectionToOtherValue(List<IOput> ports, string value)
        {
            foreach (var input in ports)
            {
                foreach (var output in input.ConnectedTo)
                {
                    if (output.Owner.Equals(value))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Creates summing points for all current inputs which are connected to n > 1 output ports and are through.
        /// The summing point has n inputs and one output
        /// The SummingPoint is represanted as a simulationmodel
        /// </summary>
        /// <returns></returns>
        public bool CreateSummingPoints()
        {
            // Counter for summing points
            int spCount = 0;

            Dictionary<string, SimulationModel> summingPointsAsSim = new Dictionary<string, SimulationModel>();

            foreach (var simulationModel in SimulationsModels.Values)
            {
                foreach (var input in simulationModel.Inputs)
                {
                    // Summing Point needed when input is connected to more than one Port and input is current.
                    if (IsThroughVariable(input) && input.ConnectedTo.Count > 1)
                    {

                        SummingPoint summingPoint = createNewSummingPoint("Sum_" + spCount++,
                            summingPointsAsSim,
                            input);
                        summingPointsAsSim.Add(summingPoint.Name, summingPoint.GetAsSimulationModel());

                    }
                }
            }

            // Adding the summing points as simulationmodel
            foreach (var key in summingPointsAsSim.Keys)
            {
                SimulationsModels.Add(key, summingPointsAsSim[key]);
            }

            return true;
        }

        /// <summary>
        /// Creates a new SummingPoint between the given input and the outputs connected to it.
        /// </summary>
        /// <param name="spName"></param>
        /// <param name="summingPointsAsSim"></param>
        /// <param name="input"></param>
        private SummingPoint createNewSummingPoint(string spName,
            Dictionary<string, SimulationModel> summingPointsAsSim,
            IOput input)
        {

            IOput newOutput = new IOput("out", spName, input.SemanticId);

            SummingPoint summingPoint = new SummingPoint(newOutput, spName, createSemanticIdSummingPoint());

            createConnectionBetweenOutputsConnectedToInputAndSummingPoint(summingPoint, input, spName);

            input.AddConnection(summingPoint.output);
            summingPoint.output.Domain = "SimToolSubset";


            return summingPoint;
        }

        /// <summary>
        /// Creates a new semantic id for the summing points
        /// </summary>
        /// <returns></returns>
        private Aas.IReference createSemanticIdSummingPoint()
        {
            var semanticId = new Aas.Reference(
                type: Aas.ReferenceTypes.ModelReference,
                keys: (new[] {
                    new Aas.Key(Aas.KeyTypes.ConceptDescription,
                        SemanticPort.GetInstance().GetSemanticForPort("BoM_SmdComp_Sum")) }
                ).Cast<Aas.IKey>().ToList());
            return semanticId;
        }

        /// <summary>
        /// Creates the connection between the output ports which are connected to input and the summingpoint.
        /// And removes the existing connections.
        /// </summary>
        /// <param name="summingPoint"></param>
        /// <param name="input"></param>
        /// <param name="name"></param>
        private void createConnectionBetweenOutputsConnectedToInputAndSummingPoint(SummingPoint summingPoint,
            IOput input,
            string name)
        {
            for (int i = input.ConnectedTo.Count - 1; i >= 0; i--)
            {
                var output = input.ConnectedTo[i];
                output.Domain = "SimToolSubset";
                IOput newSPPort = new IOput("in_" + (i + 1), name);
                newSPPort.SemanticId = output.SemanticId;
                newSPPort.Domain = "SimToolSubset";
                //Einfügen eines neuen input in den summingpoint
                summingPoint.inputs.Add(newSPPort);

                // An stelle dessen einfügen einer Verbindung zwischen dem neuen input und dem output
                newSPPort.AddConnection(output);
                // Finden der alten Verbindung und entfernen dieser
                output.RemoveConnection(input);
            }
        }


        /// <summary>
        /// Returns whether or not the given port is an through variable
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool IsThroughVariable(IOput port)
        {
            if (port.Domain.Equals("elecDC"))
            {
                return port.Unit.Contains("Ampere");
            }
            else if (port.Domain.Equals("mechRot"))
            {
                return port.Unit.Contains("Newtonmeter");
            }
            else if (port.Domain.Equals("mechTrans"))
            {
                return port.Unit.Equals("Newton");
            }


            return port.Unit.Contains("Ampere");
        }

        /// <summary>
        /// Returns whether or not the given port is an across variable
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool IsAcrossVariable(IOput port)
        {
            if (port.Domain.Equals("elecDC"))
            {
                return port.Unit.Contains("Volt");
            }
            else if (port.Domain.Equals("mechRot"))
            {
                return port.Unit.Contains("1/min");
            }
            else if (port.Domain.Equals("mechTrans"))
            {
                return port.Unit.Equals("m/s");
            }


            return false;
        }

        /// <summary>
        /// Creates multiplication points in case there is a connection where the ports to not share the same unit 
        /// but were connected because of a mapping. The mapping conatains a value which is used here as the factor
        /// for the multiplication. The existing output port of the existing connection gets connected to the multports
        /// input port and the input port of the connection gets connected to the multports output port.
        /// The old connection is removed.
        /// </summary>
        /// <returns></returns>
        public bool CreateMultPoints()
        {
            int multCount = 0;
            List<SimulationModel> newSimMod = new List<SimulationModel>();

            var semanticId = new Aas.Reference(
                type: Aas.ReferenceTypes.ModelReference,
                keys: (new[] {
                    new Aas.Key(Aas.KeyTypes.ConceptDescription,
                        SemanticPort.GetInstance().GetSemanticForPort("BoM_SmdComp_Mult")) }
                ).Cast<Aas.IKey>().ToList());

            foreach (var simmod in SimulationsModels.Values)
            {
                foreach (var input in simmod.Inputs)
                {
                    if (input.EclassID != null && simmod.Mappings.ContainsKey(input.EclassID))
                    {
                        foreach (var connected in input.ConnectedTo.ToList())
                        {
                            string name = "mult_" + multCount++;
                            SimulationModel multPort = new SimulationModel();
                            multPort.SemanticId = semanticId;
                            multPort.Multfactor = simmod.Mappings[input.EclassID].Value;

                            IOput iput = new IOput("in", name);
                            iput.SemanticId = input.SemanticId;

                            IOput oput = new IOput("out", name);
                            oput.SemanticId = input.SemanticId;

                            input.RemoveConnection(connected);

                            oput.AddConnection(input);
                            iput.AddConnection(connected);

                            multPort.Outputs.Add(oput);
                            multPort.Inputs.Add(iput);
                            multPort.Name = name;
                            newSimMod.Add(multPort);
                        }



                    }
                }


                foreach (var output in simmod.Outputs)
                {
                    if (output.EclassID != null && simmod.Mappings.ContainsKey(output.EclassID))
                    {
                        foreach (var connected in output.ConnectedTo.ToList())
                        {
                            string name = "mult_" + multCount++;
                            SimulationModel multPort = new SimulationModel();
                            multPort.SemanticId = semanticId;
                            multPort.Multfactor = simmod.Mappings[output.EclassID].Value;

                            IOput iput = new IOput("in", name);
                            iput.SemanticId = output.SemanticId;

                            IOput oput = new IOput("out", name);
                            oput.SemanticId = output.SemanticId;

                            output.RemoveConnection(connected);

                            iput.AddConnection(output);
                            oput.AddConnection(connected);

                            multPort.Outputs.Add(oput);
                            multPort.Inputs.Add(iput);
                            multPort.Name = name;
                            newSimMod.Add(multPort);
                        }
                    }
                }


            }
            foreach (var simMod in newSimMod)
            {
                SimulationsModels.Add(simMod.Name, simMod);
            }

            return true;
        }

        /// <summary>
        /// In case there are more than one output ports connected to one input port which is an across variable this
        /// function adds an warning string to the return list.
        /// 
        /// </summary>
        /// <returns></returns>
        public List<string> ToManyOutputsAcross()
        {
            List<string> warnings = new List<string>();
            foreach (var simMod in SimulationsModels.Values)
            {
                foreach (var input in simMod.Inputs)
                {
                    if (input.ConnectedTo.Count <= 1)
                    {

                    }
                    else if (IsAcrossVariable(input))
                    {
                        warnings.Add($"-WARNING: Input: {input.IdShort}({input.Unit})" +
                            $"of {input.Owner} is connected to more than one output.");
                    }
                    else
                    {
                        foreach (var output in input.ConnectedTo)
                        {
                            if (IsAcrossVariable(output))
                            {
                                warnings.Add($"-WARNING: Input: {input.IdShort}({output.Unit})" +
                                    $"of {input.Owner} is connected to more than one output.");
                                break;
                            }
                        }
                    }
                }
            }
            return warnings;
        }
        /// <summary>
        /// In case there are more than one input ports connected to one output port which is an through variable this
        /// function adds an warning string to the return list.
        /// 
        /// </summary>
        /// <returns></returns>
        public List<string> ToManyOutputThrough()
        {
            List<string> warnings = new List<string>();
            foreach (var simMod in SimulationsModels.Values)
            {
                foreach (var output in simMod.Outputs)
                {
                    if (output.ConnectedTo.Count <= 1)
                    {

                    }
                    else if (IsThroughVariable(output))
                    {
                        warnings.Add($"-WARNING: Output: {output.IdShort}({output.Unit})" +
                            $"of {output.Owner} is connected to more than one input.");
                    }
                    else
                    {
                        foreach (var input in output.ConnectedTo)
                        {
                            if (IsThroughVariable(input))
                            {
                                warnings.Add($"-WARNING: Output: {output.IdShort}({input.Unit})" +
                                    $"of {output.Owner} is connected to more than one input.");
                                break;
                            }
                        }
                    }
                }
            }
            return warnings;
        }

        /// <summary>
        /// Creates a node, represented as a Simulationmodel, for all physical ports which are connected. 
        /// </summary>
        public void CreateNodesForPhysicalPorts()
        {

            int nodeCount = 0;
            List<SimulationModel> newSimMod = new List<SimulationModel>();

            var semanticId = new Aas.Reference(
                type: Aas.ReferenceTypes.ModelReference,
                keys: (new[] {
                    new Aas.Key(Aas.KeyTypes.ConceptDescription,
                        "www.tedz.itsowl.com/ids/cd/1132_9030_2102_4033") }
                ).Cast<Aas.IKey>().ToList());

            List<List<IOput>> nodeLists = new List<List<IOput>>();

            foreach (var simmod in SimulationsModels.Values)
            {
                foreach (var port in simmod.PhysicalPorts)
                {

                    if (!nodeLists.Contains(port.ConnectedTo) && port.ConnectedTo.Count > 1)
                    {
                        nodeLists.Add(port.ConnectedTo);
                    }

                }
            }

            foreach (var list in nodeLists)
            {
                SimulationModel nodeModel = createNode(nodeCount, semanticId, list);

                if (nodeModel.PhysicalPorts.Count > 0)
                {
                    newSimMod.Add(nodeModel);
                    nodeCount++;
                }
            }

            foreach (var simmod in newSimMod)
            {
                SimulationsModels.Add(simmod.Name, simmod);
            }


        }

        /// <summary>
        /// Creates a node between the given ports
        /// </summary>
        /// <param name="nodeCount"></param>
        /// <param name="semanticId"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        private static SimulationModel createNode(int nodeCount,
            Aas.IReference semanticId,
            List<IOput> list)
        {
            SimulationModel nodeModel = new SimulationModel();
            nodeModel.IsNode = true;
            string name = "node_" + nodeCount;
            nodeModel.Name = name;
            nodeModel.SemanticId = semanticId;
            int portnumber = 0;
            foreach (var port in list)
            {


                IOput newport = new IOput(port.IdShort + "_" + portnumber++, name);
                newport.IsPhysical = true;
                newport.ConnectedTo.Add(port);
                nodeModel.PhysicalPorts.Add(newport);
            }

            return nodeModel;
        }
    }
}
