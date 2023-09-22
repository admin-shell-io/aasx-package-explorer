/*
Copyright (c) 2018-2023 

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxOpenIdClient;
using AasxPackageLogic.PackageCentral.AasxFileServerInterface.Models;
using IdentityModel.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace AasxPackageLogic.PackageCentral.AasxFileServerInterface
{
    internal class AasxServerService
    {
        private HttpClient _httpClient;

        public AasxServerService(string baseAddress)
        {
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            handler.AllowAutoRedirect = false;

            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri(baseAddress);
            _httpClient.DefaultRequestHeaders.Add("IsGetAllPackagesApi", "true");
        }

        internal void DeleteAASXByPackageId(string encodedPackageId, PackageContainerAasxFileRepository fileRepository)
        {
            _httpClient.DefaultRequestHeaders.Remove("IsGetAllPackagesApi");

            //CHeck OpenId
            CheckOpenId(fileRepository);
            bool repeat = true;
            while (repeat)
            {
                // get response?
                var response = _httpClient.DeleteAsync($"packages/{encodedPackageId}").Result;

                if (response.StatusCode == HttpStatusCode.Found)
                {
                    OpenIdRedirect(response, fileRepository);
                    repeat = true;
                    continue;
                }

                repeat = false;
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        Log.Singleton.Error($"Operation Forbidden");
                    }
                    else
                    {
                        Log.Singleton.Error($"Error while deleting the AASX File.");
                    }
                }
            }
        }

        internal HttpResponseMessage GetAASXByPackageId(string encodedPackageId, PackageContainerAasxFileRepository fileRepository)
        {
            _httpClient.DefaultRequestHeaders.Remove("IsGetAllPackagesApi");
            //CHeck OpenId
            CheckOpenId(fileRepository);
            bool repeat = true;
            while (repeat)
            {
                // get response?
                var response = _httpClient.GetAsync($"packages/{encodedPackageId}",
                    HttpCompletionOption.ResponseHeadersRead).Result;

                if (response.StatusCode == HttpStatusCode.Found)
                {
                    OpenIdRedirect(response, fileRepository);
                    repeat = true;
                    continue;
                }

                repeat = false;
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        Log.Singleton.Error($"Operation Forbidden");
                    }
                    else
                    {
                        Log.Singleton.Error($"Error while fetching the AASX File.");
                    }
                }
            }

            return null;
        }

        private void OpenIdRedirect(HttpResponseMessage response, PackageContainerAasxFileRepository fileRepository)
        {
            var runtimeOptions = fileRepository.CentralRuntimeOptions;
            var oidc = fileRepository.OpenIdClient;
            string redirectUrl = response.Headers.Location.ToString();
            // ReSharper disable once RedundantExplicitArrayCreation
            string[] splitResult = redirectUrl.Split(new string[] { "?" },
                StringSplitOptions.RemoveEmptyEntries);
            splitResult[0] = splitResult[0].TrimEnd('/');

            if (splitResult.Length < 1)
            {
                runtimeOptions?.Log?.Error("TemporaryRedirect, but url split to successful");
            }

            runtimeOptions?.Log?.Info("Redirect to: " + splitResult[0]);

            if (oidc == null)
            {
                runtimeOptions?.Log?.Info("Creating new OpenIdClient..");
                oidc = new OpenIdClientInstance();
                fileRepository.OpenIdClient = oidc;
                fileRepository.OpenIdClient.email = OpenIDClient.email;
                fileRepository.OpenIdClient.ssiURL = OpenIDClient.ssiURL;
                fileRepository.OpenIdClient.keycloak = OpenIDClient.keycloak;
            }
            oidc.authServer = splitResult[0];

            runtimeOptions?.Log?.Info($".. authentication at auth server {oidc.authServer} needed");

            var response2 = oidc.RequestTokenAsync(null,
                GenerateUiLambdaSet(runtimeOptions)).Result;
            if (oidc.keycloak == "" && response2 != null)
                oidc.token = response2.AccessToken;
            if (oidc.token != "" && oidc.token != null)
                _httpClient.SetBearerToken(oidc.token);
        }

        private OpenIdClientInstance.UiLambdaSet GenerateUiLambdaSet(PackCntRuntimeOptions runtimeOptions)
        {
            var res = new OpenIdClientInstance.UiLambdaSet();

            if (runtimeOptions?.ShowMesssageBox != null)
                res.MesssageBox = (content, text, title, buttons) =>
                    runtimeOptions.ShowMesssageBox(content, text, title, buttons);

            return res;
        }

        private void CheckOpenId(PackageContainerAasxFileRepository fileRepository)
        {
            var runtimeOptions = fileRepository.CentralRuntimeOptions;
            // Token existing?
            var oidc = fileRepository?.OpenIdClient;
            if (oidc == null)
            {
                runtimeOptions?.Log?.Info("  no ContainerList available. No OpecIdClient possible!");
                if (fileRepository != null && OpenIDClient.email != "")
                {
                    fileRepository.OpenIdClient = new OpenIdClientInstance();
                    fileRepository.OpenIdClient.email = OpenIDClient.email;
                    fileRepository.OpenIdClient.ssiURL = OpenIDClient.ssiURL;
                    fileRepository.OpenIdClient.keycloak = OpenIDClient.keycloak;
                    oidc = fileRepository.OpenIdClient;
                }
            }
            if (oidc != null)
            {
                if (oidc.token != "")
                {
                    runtimeOptions?.Log?.Info($"  using existing bearer token.");
                    _httpClient.SetBearerToken(oidc.token);
                }
                else
                {
                    if (oidc.email != "")
                    {
                        runtimeOptions?.Log?.Info($"  using existing email token.");
                        _httpClient.DefaultRequestHeaders.Add("Email", OpenIDClient.email);
                    }
                }
            }
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

        internal int PostAASXPackage(byte[] fileContent, string fileName, PackageContainerAasxFileRepository fileRepository)
        {
            _httpClient.DefaultRequestHeaders.Remove("IsGetAllPackagesApi");

            //CHeck OpenId
            CheckOpenId(fileRepository);
            bool repeat = true;
            while (repeat)
            {
                var stream = new MemoryStream(fileContent);
                var content = new MultipartFormDataContent
                {
                    { new StreamContent(stream), "file", fileName }
                };
                // get response?
                var response = _httpClient.PostAsync("packages", content).Result;

                if (response.StatusCode == HttpStatusCode.Found)
                {
                    OpenIdRedirect(response, fileRepository);
                    repeat = true;
                    continue;
                }

                repeat = false;
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    return int.Parse(responseContent);
                }

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        Log.Singleton.Error($"Operation Forbidden");
                    }
                    else
                    {
                        Log.Singleton.Error($"Error while uploading the AASX File.");
                    }
                }
            }

            return -1;
        }

        internal void PutAASXPackageById(string encodedPackageId, byte[] fileContent, string fileName)
        {
            _httpClient.DefaultRequestHeaders.Remove("IsGetAllPackagesApi");
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
