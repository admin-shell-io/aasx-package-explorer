/*
Copyright (c) 2021 KEB Automation KG <https://www.keb.de/>,
Copyright (c) 2021 Lenze SE <https://www.lenze.com/en-de/>,
author: Jonas Grote, Denis Göllner, Sebastian Bischof

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using AasxPluginSmdExporter.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AdminShellNS;
using Aas = AasCore.Aas3_0;

namespace AasxPluginSmdExporter
{
    public class BillOfMaterial
    {
        #region properties

        public string idShort { get; set; }

        public List<RelationshipElement> RelationshipElements { get; set; }

        public JObject AsJson { get; set; }

        public Dictionary<string, string> ReferenceToAssets { get; set; } = new Dictionary<string, string>();

        public static Dictionary<string, Aas.IReference> SemanticIdDict { get; set; }

        public Dictionary<string, Aas.IEntity> SubmodelElementsAsEntity { get; set; }

        public Dictionary<string, string> PropEntity { get; set; }

        List<BomSubmodel> BomSubmodels { get; set; } = new List<BomSubmodel>();

        public static Dictionary<string, BillOfMaterial> BillOfMaterials { get; set; } =
            new Dictionary<string, BillOfMaterial>();

        public string BomName { get; set; }

        public List<BomSubmodel> SubmodelsWithProps { get; set; } = new List<BomSubmodel>();
        #endregion


        public BillOfMaterial()
        {
            RelationshipElements = new List<RelationshipElement>();
            SubmodelElementsAsEntity = new Dictionary<string, Aas.IEntity>();

            if (SemanticIdDict == null)
                SemanticIdDict = new Dictionary<string, Aas.IReference>();
        }

        public BillOfMaterial(JObject jObject)
        {

        }

        public void SetSubmodelElementsAsEntity(JObject jObject)
        {
            foreach (var subEle in jObject["submodelElements"])
            {
                var jsonStr = subEle.ToString();

                Aas.IEntity entity = Aas.Jsonization.Deserialize.EntityFrom(
                    System.Text.Json.Nodes.JsonNode.Parse(jsonStr));

                this.SubmodelElementsAsEntity.Add(entity?.IdShort, entity);
            }
        }

        /// <summary>
        /// Sets the asset references
        /// </summary>
        /// <param name="jObject"></param>
        public void SetReferenceToAsset(JObject jObject)
        {
            if (jObject.SelectToken("submodelElements") != null)
            {
                ReferenceToAssets = new Dictionary<string, string>();
                foreach (var subEle in jObject["submodelElements"])
                {
                    JToken locToken = subEle.SelectToken("asset.keys(0).value");
                    if (locToken != null)
                    {
                        string assetId = locToken.ToString();
                        string idShortLoc = (string)subEle["idShort"];
                        if (!ReferenceToAssets.ContainsKey(idShortLoc))
                            ReferenceToAssets.Add(idShortLoc, assetId);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the semantich ID
        /// </summary>
        /// <param name="jObject"></param>
        public void SetSemanticIdDict(JObject jObject)
        {
            if (jObject.SelectToken("submodelElements") == null)
            {
                return;
            }
            foreach (var subEle in jObject["submodelElements"])
            {
                if (subEle["idShort"] != null)
                {
                    var idshort = subEle["idShort"].ToString();

                    if (!SemanticIdDict.ContainsKey(idshort))
                    {
                        if (subEle["semanticId"] != null)
                        {
                            var jsonStr = subEle["semanticId"].ToString();

                            Aas.IReference semantic = Aas.Jsonization.Deserialize.ReferenceFrom(
                                System.Text.Json.Nodes.JsonNode.Parse(jsonStr));

                            SemanticIdDict.Add(idshort, semantic);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Reads the relationships from the Jarray and sets them for the bom
        /// </summary>
        /// <param name="jArray"></param>
        public void SetRelationshipElements(JArray jArray)
        {

            List<RelationshipElement> relationshipElements = new List<RelationshipElement>();

            HashSet<string> componentNames = new HashSet<string>();


            foreach (var submodel in jArray)
            {

                if (submodel.SelectToken("asset.keys(0).value") != null)
                {
                    addEntityName(componentNames, submodel);

                    if (submodel.SelectToken("statements") != null)
                    {

                        JArray statements = (JArray)submodel.SelectToken("statements");
                        if (statements != null)
                        {
                            foreach (var statement in statements)
                            {

                                if (statement.SelectToken("first.keys(0).value") != null &&
                                    statement.SelectToken("second.keys(0).value") != null)
                                {
                                    createRelationshipElement(relationshipElements, statement);

                                }
                            }
                        }
                    }
                }

            }

            setMissingBoms(componentNames);

            this.RelationshipElements = relationshipElements;


        }

        /// <summary>
        /// Gets and adds the missing Boms to the BillOfMaterials list
        /// </summary>
        /// <param name="componentNames"></param>
        private static void setMissingBoms(HashSet<string> componentNames)
        {
            foreach (var component in componentNames)
            {
                if (!BillOfMaterials.ContainsKey(component))
                {
                    BillOfMaterial bom = AASRestClient.GetBillofmaterialWithRelationshipElements(component);
                    BillOfMaterials.Add(component, bom);
                }
            }
        }

        /// <summary>
        /// Adds the name of the aas with the asset id from submodel to the given set.
        /// </summary>
        /// <param name="componentNames"></param>
        /// <param name="submodel"></param>
        private static void addEntityName(HashSet<string> componentNames, JToken submodel)
        {
            JToken locToken = submodel.SelectToken("asset.keys(0).value");
            if (locToken != null)
            {
                string assetId = locToken.ToString();
                string bom_entity = AASRestClient.GetAASNameForAssetId(assetId);
                componentNames.Add(bom_entity);
            }
        }

        /// <summary>
        /// Creates a new RelationshipElement by proccessing the given JToken and adds it to the given list.
        /// </summary>
        /// <param name="relationshipElements"></param>
        /// <param name="statement"></param>
        private static void createRelationshipElement(List<RelationshipElement> relationshipElements, JToken statement)
        {

            RelationshipElement relationshipElement = RelationshipElement.Parse(statement);


            relationshipElements.Add(relationshipElement);
        }




        /// <summary>
        /// Parses the given JObject to a BillOfMaterial Object
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns></returns>
        public static BillOfMaterial Parse(JObject jObject)
        {

            if (jObject.SelectToken("idShort") != null || jObject.SelectToken("submodelElements") != null)
            {

                BillOfMaterial billOfMaterial = new BillOfMaterial();
                billOfMaterial.AsJson = jObject;

                if (jObject.ToString().Contains("statement"))
                {
                    billOfMaterial.SetReferenceToAsset(jObject);
                    billOfMaterial.SetSemanticIdDict(jObject);



                    billOfMaterial.SetSubmodelElementsAsEntity(jObject);

                    billOfMaterial.SetRelationshipElements((JArray)jObject["submodelElements"]);
                }

                billOfMaterial.idShort = (String)jObject["idShort"];


                return billOfMaterial;
            }
            else
            {
                return null;
            }
        }


    }
}
