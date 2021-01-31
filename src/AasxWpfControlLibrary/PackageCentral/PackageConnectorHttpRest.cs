/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AdminShellEvents;
using AdminShellNS;
using Newtonsoft.Json;

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
        /// By design, a PackageConnector is based on a PackageContainer. For some funcionality, it will require
        /// a link to the container. For some other, not.
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
        /// <param name="first">First segment</param>
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
            res = res.TrimEnd('/');
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
        public string GetInfo() { return "" + _client?.ToString(); }

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

        /// <summary>
        /// Tries to get an HTTP REST update value event and transforms this into an AAS event.
        /// </summary>
        /// <returns>True, if an event was emitted</returns>
        public async Task<bool> SimulateUpdateValuesEventByGetAsync(
            AdminShell.Submodel rootSubmodel,
            AdminShell.BasicEvent sourceEvent,
            AdminShell.Referable requestedElement,
            DateTime timestamp,
            string topic = null,
            string subject = null)
        {
            // access
            if (!IsValid())
                throw new PackageConnectorException("PackageConnector::SimulateUpdateValuesEventByGetAsync() " +
                    "connection not valid!");

            // first check (only allow two types of elements!)
            var reqSm = requestedElement as AdminShell.Submodel;
            var reqSme = requestedElement as AdminShell.SubmodelElement;
            if (rootSubmodel == null || sourceEvent == null
                || requestedElement == null || timestamp == null
                || (reqSm == null && reqSme == null))
                throw new PackageConnectorException("PackageConnector::SimulateUpdateValuesEventByGetAsync() " +
                    "input arguments not valid!");

            // need the parent info (even, if the calling app might have used this)
            rootSubmodel.SetAllParents();

            // get the reference of the sourceEvent and requestedElement
            var sourceReference = sourceEvent.GetReference();
            var requestedReference = (reqSm != null) ? reqSm.GetReference() : reqSme.GetReference();

            // 2nd check
            if (sourceReference == null || requestedReference == null)
                throw new PackageConnectorException("PackageConnector::SimulateUpdateValuesEventByGetAsync() " +
                    "element references cannot be determined!");

            //// try identify the original Observable
            //// var origObservable = AdminShell.SubmodelElementWrapper.FindReferableByReference(
            ////    rootSubmodel.submodelElements, sourceEvent.observed, keyIndex: 0);

            // basically, can query updates of Submodel or SubmodelElements
            string qst = null;
            if (reqSm != null && reqSm.idShort.HasContent())
            {
                // easy query
                qst = StartQuery("submodels", reqSm.idShort, "values");
            }
            else if (reqSme != null && reqSme.idShort.HasContent()
                && rootSubmodel.idShort.HasContent())
            {
                // TODO (all, 2021-01-30): check periodically for supported element types

                // the query prepared here will fail deterministically, if the reqSme element type is not supported
                // be the AAS server. Therefore, filter for element types, which are not expected to return a valid
                // response
                var reqSmeTypeSupported = reqSme is AdminShell.SubmodelElementCollection
                    || reqSme is AdminShell.Property;
                if (!reqSmeTypeSupported)
                    return false;

                // build path
                var path = "" + reqSme.idShort;

                // Resharper disable once IteratorMethodResultIsIgnored
                reqSme.FindAllParents((x) =>
                {
                    path = x.idShort + "/" + path;
                    return true;
                }, includeThis: false, includeSubmodel: false);

                // full query
                qst = StartQuery("submodels", rootSubmodel.idShort, "elements", path, "values");
            }

            // valid
            if (qst == null)
                throw new PackageConnectorException("PackageConnector::SimulateUpdateValuesEventByGetAsync() " +
                    "not enough data to build query path!");

            // do the actual query
            var response = await _client.GetAsync(qst);
            if (!response.IsSuccessStatusCode)
                throw new PackageConnectorException($"PackageConnector::SimulateUpdateValuesEventByGetAsync() " +
                    $"server did not respond correctly on query {qst} !");

            // prepare basic event message (without sending it)
            var pluv = new AasPayloadUpdateValue();
            var ev = new AasEventMsgEnvelope(
                timestamp,
                source: sourceReference,
                sourceSemanticId: sourceEvent.semanticId,
                observableReference: requestedReference,
                observableSemanticId: (requestedElement as AdminShell.IGetSemanticId).GetSemanticId(),
                topic: topic, subject: subject,
                payload: pluv);

            // goals (1) form the event content from response
            //       (2) directyl update the associated AAS

            // in order to serve goal (2), a wrapper-root is required from the observableReference,
            // that is: requestedElement!
            var wrappers = ((requestedElement as AdminShell.IEnumerateChildren)?.EnumerateChildren())?.ToList();

            // parse dynamic response object
            // Note: currently only updating Properties
            // TODO (MIHO, 2021-01-03): check to handle more SMEs for AasEventMsgUpdateValue
            // TODO (MIHO, 2021-01-04): ValueIds still missing ..
            var frame = Newtonsoft.Json.Linq.JObject.Parse(await response.Content.ReadAsStringAsync());
            if (frame.ContainsKey("values"))
            {
                // populate
                // Resharper disable once PossibleNullReferenceException
                dynamic vallist = JsonConvert.DeserializeObject(frame["values"].ToString());
                if (vallist != null)
                    foreach (var tuple in vallist)
                        if (tuple.path != null)
                        {
                            // KeyList from path
                            var kl = AdminShell.KeyList.CreateNew(AdminShell.Key.SubmodelElement, false,
                                        AdminShell.Key.IdShort, tuple.path.ToObject<string[]>());
                            // goal (1)
                            pluv.Values.Add(
                                new AasPayloadUpdateValueItem(kl, "" + tuple.value));

                            // goal (2)
                            if (wrappers != null)
                            {
                                var x = AdminShell.SubmodelElementWrapper.FindReferableByReference(
                                    wrappers, AdminShell.Reference.CreateNew(kl), keyIndex: 0);
                                if (x is AdminShell.Property prop)
                                {
                                    if (tuple.value != null)
                                        prop.value = tuple.value;
                                }
                                pluv.IsAlreadyUpdatedToAAS = true;
                            }
                        }
            }
            else if (frame.ContainsKey("value"))
            {
                // access response
                var val = frame["value"]?.ToString();

                // goal (1)
                pluv.Values.Add(
                    new AasPayloadUpdateValueItem(path: null, value: "" + val));

                // goal (2)
                if (reqSme is AdminShell.Property prop)
                {
                    if (val != null)
                        prop.value = val;
                    pluv.IsAlreadyUpdatedToAAS = true;
                }
            }
            else
                throw new PackageConnectorException($"PackageConnector::SimulateUpdateValuesEventByGetAsync() " +
                    $"cannot parse response!");

            // try send (only, if there were updates)
            var res = false;
            if (pluv.Values.Count >= 1)
            {
                Container?.PackageCentral?.PushEvent(ev);
                res = true;
            }

            // ok
            return res;
        }

        private class ListAasItem
        {
            public string Index, AasIdShort, AasId, Fn;
        }

        public async Task<List<PackageContainerRepoItem>> GenerateRepositoryFromEndpointAsync()
        {
            // access
            if (!IsValid())
                throw new PackageConnectorException("PackageConnector::GenerateRepositoryFromEndpoint() " +
                    "connection not valid!");

            // results
            var res = new List<PackageContainerRepoItem>();

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
                    if (arr.Length == 4)
                        aasItems.Add(new ListAasItem()
                        {
                            Index = arr[0].Trim(),
                            AasIdShort = arr[1].Trim(),
                            AasId = arr[2].Trim(),
                            Fn = arr[3].Trim()
                        });
                }
            }
            catch (Exception ex)
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
                    var fi = new PackageContainerRepoItem()
                    {
                        Location = CombineQuery(_client.BaseAddress.ToString(), _endPointSegments,
                                    "server", "getaasx", aasi.Index),
                        Description = $"\"{"" + x.Item1?.idShort}\",\"{"" + x.Item2?.idShort}\"",
                        Tag = "" + AdminShellUtil.ExtractPascalCasingLetters(x.Item1?.idShort).SubstringMax(0, 3)
                    };
                    fi.AasIds.Add("" + x.Item1?.identification?.id);
                    fi.AssetIds.Add("" + x.Item2?.identification?.id);
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
