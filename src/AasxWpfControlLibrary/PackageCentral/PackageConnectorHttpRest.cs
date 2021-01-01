/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AasxPackageExplorer;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Threading;
using AasxIntegrationBase;
using System.Net.Http.Headers;

namespace AasxWpfControlLibrary.PackageCentral
{
    /// <summary>
    /// Holds a live-link to AAS exposing the (standadized) HTTP REST interface.
    /// Note: as of Jan 2021, the REST routes of contemporary aasx-server (prototype AO, MIHO) are being used.
    ///       In a later stage, this shall switch to the "official" HTPP REST routes of AASiD.
    /// </summary>
    public class PackageConnectorHttpRest : PackageConnectorBase
    {
        /// <summary>
        /// Is the left part of an URL, which forms an Endpoint; routes will generated to the right of
        /// it
        /// </summary>
        public Uri Endpoint { get { return _endpoint; } }
        private Uri _endpoint;

        /// <summary>
        /// Contains the base address of the HTTP client. Is host, port (authority).
        /// </summary>
        private Uri _baseAddress;

        /// <summary>
        /// Contains that portion of the Endpoint, which is not base address and is not query.
        /// All (constrcuted) routes shall add to base address + endPointSegments.
        /// </summary>
        private string _endPointSegments;

        /// <summary>
        /// HttpClient right instantiated in the constructor. Rest of the REST calls shall call against
        /// it.
        /// Note: this is the difference to the code in aasx-server. By demand of AO, we will base
        ///       this on HttpClient and might port back it to aasx-server/AasxRestClient.
        /// </summary>
        private HttpClient _client;

        //
        // Constructors
        //

        /// <summary>
        /// By design, a PackageConnector is based on a PackageContainer.
        /// The basepoint is the left part of an URL, which forms an Endpoint; routes will generated to the right of
        /// it.
        /// </summary>
        public PackageConnectorHttpRest(PackageContainerBase container, Uri endpoint)
            : base(container)
        {
            _endpoint = endpoint;

            // split into base address and end part
            _baseAddress = new Uri(endpoint.GetLeftPart(UriPartial.Authority));
            _endPointSegments = endpoint.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);

            // make HTTP Client
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;

            _client = new HttpClient(handler);
            _client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            _client.BaseAddress = _baseAddress;
        }

        //
        // Helper
        //

        public override string ToString()
        {
            return $"HTTP/REST connector {"" + _baseAddress?.ToString()} / {"" + _endPointSegments}";
        }

        /// <summary>
        /// Combines some routes, maintaining a '/' between them.
        /// Note: empty segments will be skipped
        /// Note: design choice to make '/' not a constant or so ..
        /// Note: it makes the logic explicit. Using <c>new Uri(Uri baseUri, string relativeUri)</c> was considered
        ///       as a pattern, but not adopted.
        /// </summary>
        /// <param name="segments">Segements of the route.</param>
        /// <returns>A route without trailing slash.</returns>
        public string CombineQuery(string first, params string[] segments)
        {
            // sum
            string res = ("" + first).Trim().TrimEnd('/');

            // rights
            if (segments != null)
                foreach (var r in segments)
                {
                    if (!r.HasContent())
                        continue;
                    var rr = r.Trim().Trim('/');
                    res += "/" + rr;
                }

            // trailing slash
            res.TrimEnd('/');
            return res;
        }

        /// <summary>
        /// This will execute <c>CombineQuery</c> headed by the appropriate endpoint segements.
        /// </summary>
        /// <param name="segments">Segements of the route.</param>
        /// <returns>A route without trailing slash.</returns>
        public string StartQuery(params string[] segments)
        {
            return CombineQuery(_endPointSegments, segments);
        }

        //
        // Interface, as it complies to aasx-server/IAasxOnlineConnection
        //

        public bool IsValid() { return this._client != null; } // assume validity
        public bool IsConnected() { return true; } // always, as there is no open connection by principle
        public string GetInfo() { return _baseAddress.ToString() + "/" + _endPointSegments; }

        //
        // Functions required by the connector
        //

        public async Task<Tuple<AdminShell.AdministrationShell, AdminShell.Asset>> GetAasAssetCore(string index)
        {
            // access
            if (!IsValid())
                throw new PackageConnectorException("PackageConnector connection not valid!");
            if (!index.HasContent())
                throw new PackageConnectorException("PackageConnector::GetAasAssetCore() requires to have " +
                    "valid index data!");

            // do the actual query
            var response = await _client.GetAsync(StartQuery("aas", index, "core"));
            response.EnsureSuccessStatusCode();
            var frame = Newtonsoft.Json.Linq.JObject.Parse(await response.Content.ReadAsStringAsync());

            // proudly to the parsing
            AdminShell.AdministrationShell aas = null;
            AdminShell.Asset asset = null;

            if (frame.ContainsKey("AAS"))
                aas = AdminShellSerializationHelper.DeserializeFromJSON<AdminShell.AdministrationShell>(frame["AAS"]);
            if (frame.ContainsKey("Asset"))
                asset = AdminShellSerializationHelper.DeserializeFromJSON<AdminShell.Asset>(frame["Asset"]);

            // result
            return new Tuple<AdminShell.AdministrationShell, AdminShell.Asset>(aas, asset);
        }

        public async Task<bool> UpdateValuesFromCompleteAsync(AdminShell.Submodel sm)
        {
            // access
            if (!IsValid())
                throw new PackageConnectorException("PackageConnector::GetSubmodel() connection not valid!");

            /* TODO (AO/MIHO, 2020-12-31): right now for the connector, the AAS is addressed by _endpointSegments 
               and an idShort is required. That is the worst possible implementation and needs to be improved. */
            if (!sm.idShort.HasContent())
                throw new PackageConnectorException("PackageConnector::GetSubmodel() requires to have submodel " +
                    "idShort!");

            // do the actual query
            string query = StartQuery("submodels", sm.idShort, "complete");
            var response = await _client.GetAsync(query);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();

            // convert to updated Referable
            using (var newSM = AdminShellSerializationHelper
                .DeserializeFromJSON<AdminShell.Submodel>(new StreamReader(stream)))
            {
                ;
            }

            // ok
            return true;
        }

        private class ListAasItem
        {
            public string Index, AasIdShort, AasId, Fn;
        }

        public async Task<List<AasxFileRepository.FileItem>> GenerateRepositoryFromEndpointAsync()
        {
            // access
            if (!IsValid())
                throw new PackageConnectorException("PackageConnector::GenerateRepositoryFromEndpoint() " +
                    "connection not valid!");

            // results
            var res = new List<AasxFileRepository.FileItem>();

            // Log
            Log.Singleton.Info($"Building repository items for aas-list from {this.ToString()} ..");

            // sync-query for the list
            var aasItems = new List<ListAasItem>();
            try
            {
                // query
                var listAasResponse = await _client.GetAsync(
                    StartQuery("server", "listaas"));
                listAasResponse.EnsureSuccessStatusCode();
                var listAasString = await listAasResponse.Content.ReadAsStringAsync();

                // get some structures
                dynamic listAas = Newtonsoft.Json.Linq.JObject.Parse(listAasString);
                foreach (var li in listAas.aaslist)
                {
                    string line = "" + li;
                    var arr = line.Trim().Split(new[] { " : " }, StringSplitOptions.RemoveEmptyEntries);
                    if (arr != null && arr.Length == 4)
                    aasItems.Add(new ListAasItem() { Index = arr[0].Trim(), AasIdShort = arr[1].Trim(), 
                        AasId = arr[2].Trim(), Fn = arr[3].Trim() });
                }
            } catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"when parsing /server/listaas/ for {this.ToString()}");
            }

            // go thru the list
            foreach (var aasi in aasItems)
                try
                {
                    // query
                    var x = await GetAasAssetCore(aasi.Index);
                    if (x.Item1 == null || x.Item2 == null)
                    {
                        Log.Singleton.Error($"when retrieving /aas/{aasi.Index}/, some null contents for AAS or" +
                            $"Asset were found.");
                    }

                    // file item
                    var fi = new AasxFileRepository.FileItem()
                    {
                        Filename = CombineQuery(_baseAddress.ToString(), _endPointSegments, 
                                    "server", "getaasx", aasi.Index),
                        AasId = "" + x.Item1.identification?.id,
                        AssetId = "" + x.Item2.identification?.id,
                        Description = $"\"{"" + x.Item1.idShort}\",\"{"" + x.Item2.idShort}\"",
                        Tag = "" + AdminShellUtil.ExtractPascalCasingLetters(x.Item1.idShort).SubstringMax(0, 3)
                    };
                    res.Add(fi);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"when parsing index {aasi.Index}");
                }

            // return results
            return res;
        }

    }
}
