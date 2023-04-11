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
using AasxCompatibilityModels;
using AasxPluginSmdExporter.Model;
using Newtonsoft.Json.Linq;

namespace AasxPluginSmdExporter
{
    public class SMD
    {

        List<RelationshipElement> RelationshipElements { get; set; }

        List<SimulationModel> SimulationModels { get; set; }

        Dictionary<string, AdminShellV20.Entity> SimModelsAsEntities =
            new Dictionary<string, AdminShellV20.Entity>();

        AdminShellV20.Submodel Bom { get; set; }

        private int relCount = 0;

        public SMD(List<RelationshipElement> relationshipElements, List<SimulationModel> simulationModels)
        {
            this.RelationshipElements = relationshipElements;
            this.SimulationModels = simulationModels;
        }


        /// <summary>
        /// Adds the SMD to the currently open aasx
        /// </summary>
        /// <returns></returns>
        public bool PutSMD()
        {
            string name = "SimulationModelDescription";
            string name_sm = "BillOfMaterial";

            if (!AddAas(name))
                return false;

            if (!AddBomSubmodel(name, name_sm))
                return false;

            // 
            if (!AddComponentInfos(name, name_sm) || !AddRelationshipInfo(name, name_sm))
                return false;

            return true;
        }



        /// <summary>
        /// Adds the BillOfMaterial Submodel to the AAS 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="name_sm"></param>
        /// <returns></returns>
        private bool AddBomSubmodel(string name, string name_sm)
        {
            Bom = new AdminShellV20.Submodel();
            Bom.idShort = name_sm;
            Bom.identification.id = "urn:itsowl.tedz.com:sm:instance:9053_7072_4002_2783";
            Bom.identification.idType = "IRI";
            Bom.semanticId = new AdminShellV20.SemanticId();
            Bom.semanticId.Keys.Add(new AdminShellV20.Key());
            Bom.semanticId.Keys[0].value = "http://example.com/id/type/submodel/BOM/1/1";
            Bom.semanticId.Keys[0].type = "Submodel";
            Bom.semanticId.Keys[0].idType = "IRI";

            String bom_json = Newtonsoft.Json.JsonConvert.SerializeObject(Bom);

            if (AASRestClient.PutSubmodel(bom_json, name_sm, name))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Adds the AAS with the given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool AddAas(string name)
        {

            AdminShellV20.AdministrationShell aas = new AdminShellV20.AdministrationShell();

            aas.idShort = name;
            aas.identification.id = "urn:itsowl.tedz.com:demo:aas:1:1:123";
            aas.identification.idType = "IRI";

            String smd = Newtonsoft.Json.JsonConvert.SerializeObject(aas);

            //Put SMD
            if (AASRestClient.PutAAS(smd, name))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Adds the Relationships to the billOfMaterial. The relations are all the ports which are connected.
        /// Thus we need to iterate over all simulationmodels and its input ports to connect get all signalflow ports.
        /// And additionally all physical ports.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="name_sm"></param>
        /// <returns></returns>
        private bool AddRelationshipInfo(string name, string name_sm)
        {

            bool allsuccess = true;
            foreach (var sim in SimulationModels)
            {

                foreach (var input in sim.Inputs)
                {
                    foreach (var output in input.ConnectedTo)
                    {
                        AdminShellV20.RelationshipElement portAsRel = GetPortsAsRelation(input, output);

                        string test_ent_json = Newtonsoft.Json.JsonConvert.SerializeObject(portAsRel);

                        if (!AASRestClient.PutEntity(test_ent_json, name, name_sm, ""))
                        {
                            allsuccess = false;
                        }
                    }
                }



                if (sim.IsNode)
                {
                    foreach (var port in sim.PhysicalPorts)
                    {
                        AdminShellV20.RelationshipElement portAsRel =
                            GetPortsAsRelation(port, port.ConnectedTo[0], 1);

                        string test_ent_json = Newtonsoft.Json.JsonConvert.SerializeObject(portAsRel);

                        if (!AASRestClient.PutEntity(test_ent_json, name, name_sm, ""))
                        {
                            allsuccess = false;
                        }
                    }
                }

            }
            return allsuccess;
        }

        /// <summary>
        /// Adds the component and its ports to the BillOfMaterial
        /// </summary>
        /// <param name="name"></param>
        /// <param name="name_sm"></param>
        /// <returns></returns>
        private bool AddComponentInfos(string name, string name_sm)
        {
            bool allsuccess = true;

            foreach (var sim in SimulationModels)
            {
                if (!((sim.Inputs.Count == 0 && sim.Outputs.Count == 0) && sim.PhysicalPorts.Count == 0))
                {
                    AdminShellV20.Entity simAsEntity = GetSimModelAsEntity(sim, name_sm);
                    foreach (var port in GetPortsAsProp(sim))
                    {
                        simAsEntity.Add(port);
                    }
                    string test_ent_json = Newtonsoft.Json.JsonConvert.SerializeObject(simAsEntity);

                    SimModelsAsEntities.Add(sim.Name, simAsEntity);

                    if (!AASRestClient.PutEntity(test_ent_json, name, name_sm, ""))
                    {
                        allsuccess = false;
                    }
                }

            }

            return allsuccess;
        }

        /// <summary>
        /// Returns the SimulationModel port Information as a Property
        /// </summary>
        /// <param name="simulationModel"></param>
        /// <returns></returns>
        private List<AdminShellV20.Property> GetPortsAsProp(SimulationModel simulationModel)
        {
            List<AdminShellV20.Property> ioputs = new List<AdminShellV20.Property>();
            foreach (var input in simulationModel.Inputs)
            {
                ioputs.Add(GetPortAsProperty(input, "in"));
            }

            foreach (var output in simulationModel.Outputs)
            {
                ioputs.Add(GetPortAsProperty(output, "out"));
            }

            foreach (var output in simulationModel.PhysicalPorts)
            {
                ioputs.Add(GetPortAsProperty(output, "physical"));
            }


            return ioputs;
        }

        /// <summary>
        /// Returns the given port as property
        /// </summary>
        /// <param name="port"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private AdminShellV20.Property GetPortAsProperty(IOput port, string direction)
        {
            AdminShellV20.Property port_prop = new AdminShellV20.Property();
            port_prop.idShort = port.IdShort;
            port_prop.AddQualifier("direction", direction);
            port_prop.AddQualifier("Domain", port.Domain);
            port_prop.valueType = port.Datatype;
            port_prop.semanticId = port.SemanticId;

            return port_prop;
        }



        /// <summary>
        /// Returns the SimulationModel as Entity
        /// </summary>
        /// <param name="simulationModel"></param>
        /// <param name="nameSubmodel"></param>
        /// <returns></returns>
        public AdminShellV20.Entity GetSimModelAsEntity(SimulationModel simulationModel, string nameSubmodel)
        {

            AdminShellV20.Entity entity = new AdminShellV20.Entity();
            entity.idShort = simulationModel.Name;
            entity.entityType = "CoManagedEntity";
            entity.semanticId = simulationModel.SemanticId;

            // make new parent
            var newpar = new AdminShellV20.Referable();
            newpar.idShort = nameSubmodel;
            entity.parent = newpar;

            // to be processed further
            return entity;
        }

        /// <summary>
        /// Returns the given Relationshipelement as RelationshipElement of Adminshell
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public AdminShellV20.RelationshipElement GetPortsAsRelation(IOput input, IOput output, int type = 0)
        {

            AdminShellV20.Submodel submodel = new AdminShellV20.Submodel();

            submodel.identification.id = "urn:itsowl.tedz.com:sm:instance:9053_7072_4002_2783";
            submodel.identification.idType = "IRI";


            // Finden der zugehörigen properties der in und out
            AdminShellV20.Entity entity = new AdminShellV20.Entity();
            entity.idShort = input.Owner;
            entity.parent = submodel;

            AdminShellV20.Property property = new AdminShellV20.Property();
            property.idShort = input.IdShort;
            property.parent = entity;


            AdminShellV20.Entity outentity = new AdminShellV20.Entity();
            outentity.idShort = output.Owner;
            outentity.parent = submodel;

            AdminShellV20.Property outproperty = new AdminShellV20.Property();
            outproperty.idShort = output.IdShort;
            outproperty.parent = outentity;


            // setzen als first bzw second

            AdminShellV20.RelationshipElement relationshipElement =
                new AdminShellV20.RelationshipElement();

            relationshipElement.first = outproperty.GetReference();
            relationshipElement.second = property.GetReference();
            relationshipElement.semanticId = SetSemanticIdRelEle(input, output, type);
            relationshipElement.idShort = $"{relCount++}";


            return relationshipElement;
        }

        /// <summary>
        /// Sets the SemanticID of the relation depending in the semanticPort class.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public AdminShellV20.SemanticId SetSemanticIdRelEle(IOput input, IOput output, int type)
        {
            AdminShellV20.SemanticId semantic = new AdminShellV20.SemanticId();
            AdminShellV20.Key key = new AdminShellV20.Key();
            key.idType = "IRI";
            key.index = 0;
            key.local = true;
            key.type = "ConceptDescription";
            switch (type)
            {
                case 0: key.value = SemanticPort.GetInstance().GetSemanticForPort("SmdComp_SignalFlow"); break;
                case 1: key.value = SemanticPort.GetInstance().GetSemanticForPort("SmdComp_PhysicalElectric"); break;
                case 2: key.value = "mechanic"; break;
            }
            semantic.JsonKeys.Add(key);
            return semantic;
        }
    }
}
