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
using AasxPluginSmdExporter.Model;
using Newtonsoft.Json.Linq;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using System.IdentityModel.Tokens.Jwt;
using AasCore.Aas3_0;

namespace AasxPluginSmdExporter
{
    public class SMD
    {

        List<RelationshipElement> RelationshipElements { get; set; }

        List<SimulationModel> SimulationModels { get; set; }

        Dictionary<string, Aas.IEntity> SimModelsAsEntities =
            new Dictionary<string, Aas.IEntity>();

        Aas.ISubmodel Bom { get; set; }

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
            Bom = new Aas.Submodel(
                id: "urn:itsowl.tedz.com:sm:instance:9053_7072_4002_2783",
                idShort: name_sm,
                semanticId: new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                    (new[] {
                        new Aas.Key(Aas.KeyTypes.Submodel, "http://example.com/id/type/submodel/BOM/1/1") }
                    ).Cast<Aas.IKey>().ToList()));

            var bom_json = Jsonization.Serialize.ToJsonObject(Bom)
                        .ToJsonString(new System.Text.Json.JsonSerializerOptions());

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
            var aas = new Aas.AssetAdministrationShell(
                id: "urn:itsowl.tedz.com:demo:aas:1:1:123",
                idShort: name,
                assetInformation: new Aas.AssetInformation(Aas.AssetKind.Instance));

            var smd = Jsonization.Serialize.ToJsonObject(aas)
                        .ToJsonString(new System.Text.Json.JsonSerializerOptions());

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
                        var portAsRel = GetPortsAsRelation(input, output);

                        var test_ent_json = Jsonization.Serialize.ToJsonObject(portAsRel)
                                .ToJsonString(new System.Text.Json.JsonSerializerOptions());

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
                        var portAsRel =
                            GetPortsAsRelation(port, port.ConnectedTo[0], 1);

                        var test_ent_json = Jsonization.Serialize.ToJsonObject(portAsRel)
                                .ToJsonString(new System.Text.Json.JsonSerializerOptions());

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
                    var simAsEntity = GetSimModelAsEntity(sim, name_sm);
                    foreach (var port in GetPortsAsProp(sim))
                    {
                        simAsEntity.Add(port);
                    }

                    var test_ent_json = Jsonization.Serialize.ToJsonObject(simAsEntity)
                                .ToJsonString(new System.Text.Json.JsonSerializerOptions());

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
        private List<Aas.IProperty> GetPortsAsProp(SimulationModel simulationModel)
        {
            var ioputs = new List<Aas.IProperty>();
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
        private Aas.IProperty GetPortAsProperty(IOput port, string direction)
        {
            var vt = Aas.DataTypeDefXsd.String;
            try
            {
                vt = Aas.Jsonization.Deserialize.DataTypeDefXsdFrom(port.Datatype);
            }
            catch { }

            var port_prop = new Aas.Property(
                valueType: vt,
                idShort: port.IdShort,
                semanticId: port.SemanticId,
                qualifiers: (new[] {
                    new Aas.Qualifier("direction", Aas.DataTypeDefXsd.String, value: direction),
                    new Aas.Qualifier("Domain", Aas.DataTypeDefXsd.String, value: port.Domain)
                }).Cast<Aas.IQualifier>().ToList());

            return port_prop;
        }

        /// <summary>
        /// Returns the SimulationModel as Entity
        /// </summary>
        /// <param name="simulationModel"></param>
        /// <param name="nameSubmodel"></param>
        /// <returns></returns>
        public Aas.IEntity GetSimModelAsEntity(SimulationModel simulationModel, string nameSubmodel)
        {
            // entity
            var entity = new Aas.Entity(
                entityType: Aas.EntityType.CoManagedEntity,
                idShort: simulationModel.Name,
                semanticId: simulationModel.SemanticId);

            // dead-csharp off
            // make new parent
            //var newpar = new AdminShellV20.Referable();
            //newpar.idShort = nameSubmodel;
            //entity.parent = newpar;
            // dead-csharp off

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
        public Aas.IRelationshipElement GetPortsAsRelation(IOput input, IOput output, int type = 0)
        {
            var submodel = new Aas.Submodel(
                id: "urn:itsowl.tedz.com:sm:instance:9053_7072_4002_2783");

            // Finden der zugehörigen properties der in und out
            var entity = new Aas.Entity(
                entityType: Aas.EntityType.CoManagedEntity,
                idShort: input.Owner);
            entity.Parent = submodel;

            var property = new Aas.Property(
                valueType: Aas.DataTypeDefXsd.String,
                idShort: input.IdShort);
            property.Parent = entity;

            var outentity = new Aas.Entity(
                entityType: Aas.EntityType.CoManagedEntity,
                idShort: output.Owner);
            outentity.Parent = submodel;

            var outproperty = new Aas.Property(
                valueType: Aas.DataTypeDefXsd.String,
                idShort: output.IdShort);
            outproperty.Parent = outentity;

            // setzen als first bzw second

            var relationshipElement = new Aas.RelationshipElement(
                first: outproperty.GetReference(),
                second: property.GetReference(),
                idShort: $"{relCount++}",
                semanticId: SetSemanticIdRelEle(input, output, type));

            return relationshipElement;
        }

        /// <summary>
        /// Sets the SemanticID of the relation depending in the semanticPort class.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Aas.IReference SetSemanticIdRelEle(IOput input, IOput output, int type)
        {
            var semantic = new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                (new[] {
                    new Aas.Key(Aas.KeyTypes.ConceptDescription, "to be filled") }
                ).Cast<Aas.IKey>().ToList());

            switch (type)
            {
                case 0:
                    semantic.Keys[0].Value =
                        SemanticPort.GetInstance().GetSemanticForPort("SmdComp_SignalFlow");
                    break;
                case 1:
                    semantic.Keys[0].Value =
                        SemanticPort.GetInstance().GetSemanticForPort("SmdComp_PhysicalElectric");
                    break;
                case 2:
                    semantic.Keys[0].Value = "mechanic";
                    break;
            }
            return semantic;
        }
    }
}
