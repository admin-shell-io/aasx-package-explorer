using AasxPackageLogic.PackageCentral.AasxFileServerInterface.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace AasxPackageLogic.PackageCentral.AasxFileServerInterface
{
    internal class AasxServerService
    {
        private HttpClient _httpClient;

        public AasxServerService(string baseAddress)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseAddress);
        }

        internal void DeleteAASXByPackageId(string encodedPackageId)
        {
            var response = _httpClient.DeleteAsync($"packages/{encodedPackageId}").Result;
            response.EnsureSuccessStatusCode();
        }

        internal HttpResponseMessage GetAASXByPackageId(string encodedPackageId)
        {
            var response = _httpClient.GetAsync($"packages/{encodedPackageId}", HttpCompletionOption.ResponseHeadersRead).Result;
            response.EnsureSuccessStatusCode();

            return response;
        }

        internal List<PackageDescription> GetAllAASXPackageIds()
        {
            var response = _httpClient.GetAsync("packages").Result;
            response.EnsureSuccessStatusCode();

            var content = response.Content.ReadAsStringAsync().Result;
            var pagedPackages = JsonConvert.DeserializeObject<PackageDescriptionPagedResult>(content);
            return pagedPackages.result;
        }

        internal AssetAdministrationShell GetAssetAdministrationShellById(string encodedAasId)
        {
            var response = _httpClient.GetAsync($"shells/{encodedAasId}").Result;
            response.EnsureSuccessStatusCode();

            var content = response.Content.ReadAsStringAsync().Result;
            var contentNode = System.Text.Json.JsonSerializer.Deserialize<JsonNode>(content);
            var output = Jsonization.Deserialize.AssetAdministrationShellFrom(contentNode);
            return output;
        }

        internal int PostAASXPackage(byte[] fileContent, string fileName)
        {
            var stream = new MemoryStream(fileContent);
            var content = new MultipartFormDataContent
            {
                { new StreamContent(stream), "file", fileName }
            };
            var response = _httpClient.PostAsync("packages", content).Result;
            response.EnsureSuccessStatusCode();
            var responseContent = response.Content.ReadAsStringAsync().Result;
            return int.Parse(responseContent);
        }

        internal void PutAASXPackageById(string encodedPackageId, byte[] fileContent, string fileName)
        {
            var stream = new MemoryStream(fileContent);
            var content = new MultipartFormDataContent
            {
                { new StreamContent(stream), "file", fileName }
            };
            var response = _httpClient.PutAsync($"packages/{encodedPackageId}", content).Result;
            response.EnsureSuccessStatusCode();
        }
    }
}
