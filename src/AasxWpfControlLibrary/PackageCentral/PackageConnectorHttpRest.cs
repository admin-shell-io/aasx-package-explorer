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
using Newtonsoft.Json;
using AdminShellEvents;
using AasxWpfControlLibrary.AasxFileRepo;
using AasxWpfControlLibrary.Toolkit;

namespace AasxWpfControlLibrary.PackageCentral
{
    /// <summary>
    /// Holds a live-link to AAS exposing the (standadized) HTTP REST interface.
    /// Note: as of Jan 2021, the REST routes of contemporary aasx-server (prototype AO, MIHO) are being used.
    ///       In a later stage, this shall switch to the "official" HTPP REST routes of AASiD.
    /// </summary>
    public class PackageConnectorHttpRest : PackageConnectorBase
    {
        //
        // Members
        //

        private ClientToolkitHttpRest _client;

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
            _client = ClientToolkitHttpRest.CreateNew(endpoint);
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
            var response = await _client.GetAsync(_client.PrepareQuery("aas", index, "core"));
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
                || (reqSm == null && reqSme == null) )
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

            // try identify the original Observable
            //var origObservable = AdminShell.SubmodelElementWrapper.FindReferableByReference(
            //    rootSubmodel.submodelElements, sourceEvent.observed, keyIndex: 0);

            // basically, can query updates of Submodel or SubmodelElements
            string qst = null;
            if (reqSm != null && reqSm.idShort.HasContent())
            {
                // easy query
                qst = _client.PrepareQuery("submodels", reqSm.idShort, "values");
            }
            else if (reqSme != null && reqSme.idShort.HasContent()
                && rootSubmodel.idShort.HasContent())
            {
                // build path
                var path = "" + reqSme.idShort;
                reqSme.FindAllParents((x) =>
                {
                    path = x.idShort + "/" + path;
                    return true;
                }, includeThis: false, includeSubmodel: false);

                // full query
                qst = _client.PrepareQuery("submodels", rootSubmodel.idShort, "elements", path, "values");
            }

            // valid
            if (qst == null)
                throw new PackageConnectorException("PackageConnector::SimulateUpdateValuesEventByGetAsync() " +
                    "not enough data to build query path!");

            // do the actual query
            string query = _client.PrepareQuery(qst);
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
            // TODO (MIHO, 20201-01-03): check to handle more SMEs for AasEventMsgUpdateValue
            // TODO (MIHO, 2021-01-04): ValueIds still missing ..
            var frame = Newtonsoft.Json.Linq.JObject.Parse(await response.Content.ReadAsStringAsync());
            if (frame.ContainsKey("values"))
            {                
                // populate
                dynamic vallist = JsonConvert.DeserializeObject(frame["values"].ToString());
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
                    new AasPayloadUpdateValueItem(path: null, value: "" + val)) ;

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

        public async Task<List<AasxFileRepositoryItem>> GenerateRepositoryFromEndpointAsync()
        {
            // access
            if (!IsValid())
                throw new PackageConnectorException("PackageConnector::GenerateRepositoryFromEndpoint() " +
                    "connection not valid!");

            // results
            var res = new List<AasxFileRepositoryItem>();

            // Log
            Log.Singleton.Info($"Building repository items for aas-list from {this.ToString()} ..");

            // sync-query for the list
            var aasItems = new List<ListAasItem>();
            try
            {
                // query
                var listAasResponse = await _client.GetAsync(
                    _client.PrepareQuery("server", "listaas"));
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
                    var fi = new AasxFileRepositoryItem()
                    {
                        Filename = _client.CombineQuery(_client.BaseAddress.ToString(), _client.EndPointSegments, 
                                    "server", "getaasx", aasi.Index),
                        Description = $"\"{"" + x.Item1.idShort}\",\"{"" + x.Item2.idShort}\"",
                        Tag = "" + AdminShellUtil.ExtractPascalCasingLetters(x.Item1.idShort).SubstringMax(0, 3)
                    };
                    fi.AasIds.Add("" + x.Item1.identification?.id);
                    fi.AssetIds.Add("" + x.Item2.identification?.id);
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
