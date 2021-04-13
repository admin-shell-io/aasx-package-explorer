/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AasxPluginSmdExporter
{
    public static class AASRestClient
    {
        static readonly HttpClient httpClient;

        private static bool used;


        static AASRestClient()
        {
            httpClient = new HttpClient();
            used = false;
        }

        /// <summary>
        /// Starts the HttpClient and sets its Baseaadress to the given Adress
        /// </summary>
        /// <param name="baseAddress"></param>
        public static void Start(string baseAddress)
        {
            if (!used)
            {
                httpClient.BaseAddress = new Uri(baseAddress);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                used = true;
            }


        }

        /// <summary>
        /// Returns the Json version of the aas with the given name.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<JObject> GetJson(string path)
        {
            try
            {

                HttpResponseMessage response = httpClient.GetAsync(path).Result;

                if (response.IsSuccessStatusCode)
                {

                    string respstr = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(respstr);

                    return json;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            return null;
        }

        public static async Task<JArray> GetJArray(string path)
        {
            HttpResponseMessage response = httpClient.GetAsync(path).Result;
            if (response.IsSuccessStatusCode)
            {
                string respstr = await response.Content.ReadAsStringAsync();

                JArray jarray = JArray.Parse(respstr);

                return jarray;
            }
            return null;
        }

        /// <summary>
        /// Returns a  BillOfMaterial with the currently necessary information
        /// </summary>
        /// <param name="aasID"></param>
        /// <returns></returns>
        public static BillOfMaterial GetBillofmaterialWithRelationshipElements(string aasID)
        {


            JObject json = GetJson($"aas/{aasID}/submodels/billofmaterial/complete").Result;

            if (json == null || json.SelectToken("hasDataSpecification") == null)
            {
                return null;
            }

            BillOfMaterial billOfMaterial = BillOfMaterial.Parse(json);
            billOfMaterial.BomName = aasID;
            return billOfMaterial;

        }

        /// <summary>
        /// Returns the SimulationModel of the given Submodel
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static SimulationModel GetSimulationModel(string name)
        {

            JObject simulationModelJson
                = AASRestClient.GetJson($"aas/{name}/submodels/SimulationModels/complete").Result;
            SimulationModel ret = SimulationModel.Parse(simulationModelJson, name);
            return ret;
        }

        /// <summary>
        /// Puts an AAS to the server.
        /// </summary>
        /// <param name="aas"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool PutAAS(string aas, string name)
        {
            HttpResponseMessage response = httpClient.PutAsync($"aas",
                new StringContent(aas, Encoding.UTF8, "application/json")).Result;

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Puts an Submodel with Path: aas/{aasId}/submodels to the server.
        /// </summary>
        /// <param name="submodel"></param>
        /// <param name="name"></param>
        /// <param name="aasId"></param>
        /// <returns></returns>
        public static bool PutSubmodel(string submodel, string name, string aasId)
        {
            HttpResponseMessage response = httpClient.PutAsync($"aas/{aasId}/submodels",
                new StringContent(submodel, Encoding.UTF8, "application/json")).Result;

            return response.IsSuccessStatusCode;
        }


        public static bool PutEntity(string submodel_json, string aasId, string nameSub, string nameEntity)
        {
            HttpResponseMessage response = httpClient.PutAsync($"aas/{aasId}/submodels/{nameSub}/elements/{nameEntity}",
                new StringContent(submodel_json, Encoding.UTF8, "application/json")).Result;

            return response.IsSuccessStatusCode;
        }

        public static string GetAASNameForAssetId(string assetId)
        {
            try
            {
                assetId = WebUtility.UrlEncode(assetId);
                var path = $"assets/@qs?qs=IRI,{assetId}";

                JArray jObject = AASRestClient.GetJArray(path).Result;

                return (string)jObject[0]["idShort"];
            }
            catch (Exception)
            {
                // Could not get asset with assetId
                return "";
            }
        }

    }
}
