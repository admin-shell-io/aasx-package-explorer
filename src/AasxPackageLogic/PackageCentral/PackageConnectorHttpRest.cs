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
using AasxIntegrationBase.AdminShellEvents;
using AasxOpenIdClient;
using AdminShellNS;
using IdentityModel.Client;
using IO.Swagger.Api;
using IO.Swagger.Client;
using Newtonsoft.Json;
using SSIExtension;

namespace AasxPackageLogic.PackageCentral
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

            OpenIDClient.auth = endpoint.ToString().Contains("?auth");
            if (endpoint.ToString().Contains("?ssi"))
            {
                string[] s = endpoint.ToString().Split('=');
                OpenIDClient.ssiURL = s[1];
            }
            if (endpoint.ToString().Contains("?keycloak"))
            {
                string[] s = endpoint.ToString().Split('=');
                OpenIDClient.keycloak = s[1];
            }
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
            if (OpenIDClient.email != "")
                _client.DefaultRequestHeaders.Add("Email", OpenIDClient.email);

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
                || requestedElement == null || (reqSm == null && reqSme == null))
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
            if (OpenIDClient.email != "")
                _client.DefaultRequestHeaders.Add("Email", OpenIDClient.email);

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

                if (OpenIDClient.auth)
                {
                    var responseAuth = _client.GetAsync("/authserver").Result;
                    if (responseAuth.IsSuccessStatusCode)
                    {
                        var content = responseAuth.Content.ReadAsStringAsync().Result;
                        if (content != null && content != "")
                        {
                            OpenIDClient.authServer = content;
                            var response2 = await OpenIDClient.RequestTokenAsync(null);
                            OpenIDClient.token = response2.AccessToken;
                            OpenIDClient.auth = false;
                        }
                    }
                }

                if (OpenIDClient.token != "")
                {
                    _client.SetBearerToken(OpenIDClient.token);
                }
                else
                {
                    if (OpenIDClient.email != "")
                        _client.DefaultRequestHeaders.Add("Email", OpenIDClient.email);
                }

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
                        ContainerOptions = PackageContainerOptionsBase.CreateDefault(Options.Curr),
                        Location = CombineQuery(_client.BaseAddress.ToString(), _endPointSegments,
                                    "server", "getaasx", aasi.Index),
                        Description = $"\"{"" + x.Item1?.idShort}\",\"{"" + x.Item2?.idShort}\"",
                        Tag = "" + AdminShellUtil.ExtractPascalCasingLetters(x.Item1?.idShort).SubstringMax(0, 3)
                    };
                    fi.AasIds.Add("" + x.Item1?.id?.value);
                    fi.AssetIds.Add("" + x.Item2?.id?.value);
                    res.Add(fi);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"when parsing index {aasi.Index}");
                }

            // return results
            return res;
        }

#if OLD
        public async Task<bool> PullEvents()
        {
            // access
            if (!IsValid())
                throw new PackageConnectorException("PackageConnector::PullEvents() " +
                    "connection not valid!");

            // do the query
            var qst = "/geteventmessages";
            var response = await _client.GetAsync(qst);
            if (!response.IsSuccessStatusCode)
                throw new PackageConnectorException($"PackageConnector::PullEvents() " +
                    $"Server did not respond correctly on query {qst} !");

            // ok
            // parse dynamic response object
            var frame = Newtonsoft.Json.Linq.JObject.Parse(await response.Content.ReadAsStringAsync());

            // change handler, start?
            var handler = Container?.ChangeEventHandler;
            handler?.Invoke(Container, PackCntChangeEventReason.StartOfChanges);

            // which events?
            if (frame != null 
                && frame.ContainsKey("Changes") 
                && (frame["Changes"] is Newtonsoft.Json.Linq.JArray changes)
                && changes.Count > 0)
                foreach (var changeJO in changes)
                {
                    // access
                    if (changeJO == null)
                        continue;

                    // try deserialize
                    var change = changeJO.ToObject<AasPayloadStructuralChangeItem>();
                    if (change == null)
                        throw new PackageConnectorException($"PackageConnector::PullEvents() " +
                            "Cannot deserize payload StructuralChangeItem!");

                    // try determine tarket of {Observavle}/{path}
                    AdminShell.Referable target = null;
                    if (change.Path?.IsEmpty == false)
                    {
                        AdminShell.KeyList kl = null;
                        if (change.Path.First().IsAbsolute())
                            kl = change.Path;
                        else
                        {
                            // { Observavle}/{ path}
                            // need outer event!
                            throw new NotImplementedException("Outer Event Message!");
                        }
                        target = Env?.AasEnv?.FindReferableByReference(kl);
                    }

                    // create
                    if (change.Reason == AasPayloadStructuralChangeItem.ChangeReason.Create)
                    {
                        // need data object
                        var dataRef = change.GetDataAsReferable();
                        if (dataRef == null)
                            throw new PackageConnectorException($"PackageConnector::PullEvents() " +
                                "Cannot deserize StructuralChangeItem Referable data!");

                        // go through some cases
                        // all SM, SME with dependent elements
                        if (target is AdminShell.IManageSubmodelElements targetMgr
                            && dataRef is AdminShell.SubmodelElement sme
                            && change.CreateAtIndex < 0)
                        {
                            targetMgr.Add(sme);
                        }
                        else
                        // at least for SMC, handle CreateAtIndex
                        if (target is AdminShell.SubmodelElementCollection targetSmc
                            && dataRef is AdminShell.SubmodelElement sme2
                            && change.CreateAtIndex >= 0
                            && targetSmc.value != null
                            && change.CreateAtIndex < targetSmc.value.Count)
                        {
                            targetSmc.value.Insert(change.CreateAtIndex, sme2);
                        }
                        else
                        // add to AAS
                        if (target is AdminShell.AdministrationShell targetAas
                            && dataRef is AdminShell.Submodel sm
                            && Env?.AasEnv != null)
                        {
                            Env.AasEnv.Submodels.Add(sm);
                            targetAas.AddSubmodelRef(sm?.GetSubmodelRef());
                        }
                        else
                            throw new PackageConnectorException($"PackageConnector::PullEvents() " +
                                "Error creating data within Observable/path!");
                    }
                }

            // change handler, start?
            handler?.Invoke(Container, PackCntChangeEventReason.EndOfChanges);

            return true;
        }
#endif

        private void ExecuteEventAction(
            AasPayloadStructuralChangeItem change,
            AasEventMsgEnvelope env,
            PackCntChangeEventHandler handler = null)
        {
            // trivial
            if (change?.Path == null)
                return;

            // try determine tarket of "Observable"/"path"
            var targetKl = new AdminShell.KeyList();
            AdminShell.Referable target = null;
            if (change.Path?.IsEmpty == false)
            {
                if (env.ObservableReference?.Keys != null)
                    targetKl = new AdminShell.KeyList(env.ObservableReference.Keys);

                targetKl.AddRange(change.Path);

                if (targetKl.First().IsAbsolute())
                    target = Env?.AasEnv?.FindReferableByReference(targetKl);
            }

            // try evaluate parent of target?
            var parentKl = new AdminShell.KeyList(targetKl);
            AdminShell.Referable parent = null;
            if (parentKl.Count > 1)
            {
                parentKl.RemoveAt(parentKl.Count - 1);
                parent = Env?.AasEnv?.FindReferableByReference(parentKl);
            }

            // create
            if (change.Reason == StructuralChangeReason.Create)
            {
                // need parent (target will not exist)
                if (parent == null)
                {
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                        info: "PackageConnector::PullEvents() Create " +
                        "Cannot find parent Referable! " + change.Path.ToString(1)));
                    return;
                }

                // target existing??
                if (target != null)
                {
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                        info: "PackageConnector::PullEvents() Create " +
                        "Target Referable already existing .. Aborting! " + change.Path.ToString(1)));
                    return;
                }

                // need data object
                var dataRef = change.GetDataAsReferable();
                if (dataRef == null)
                {
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                        info: "PackageConnector::PullEvents() Create " +
                        "Cannot deserize StructuralChangeItem Referable data!"));
                    return;
                }

                // need to care for parents inside
                // TODO (MIHO, 2021-11-07): refactor use of SetParentsForSME to be generic
                if (dataRef is AdminShell.Submodel drsm)
                    drsm.SetAllParents();
                if (dataRef is AdminShell.SubmodelElement drsme)
                    AdminShell.Submodel.SetParentsForSME(parent, drsme);

                // paranoiac: make sure, that dataRef.idShort matches last key of target (in case of SME)
                if (dataRef is AdminShell.SubmodelElement sme0
                    && true != targetKl?.Last()?.Matches(
                        "", false, AdminShell.Key.IdShort, sme0.idShort,
                        AdminShell.Key.MatchMode.Identification))
                {
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                        info: "PackageConnector::PullEvents() Create " +
                        "Target SME idShort does not match provided data section! " + change.Path.ToString(1)));
                    return;
                }

                // go through some cases
                // all SM, SME with dependent elements
                if (parent is AdminShell.IManageSubmodelElements parentMgr
                    && dataRef is AdminShell.SubmodelElement sme
                    && change.CreateAtIndex < 0)
                {
                    parentMgr.Add(sme);
                    change.FoundReferable = dataRef;
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Create,
                        thisRef: sme, parentRef: parent));
                }
                else
                // at least for SMC, handle CreateAtIndex
                if (parent is AdminShell.SubmodelElementCollection parentSmc
                    && dataRef is AdminShell.SubmodelElement sme2
                    && change.CreateAtIndex >= 0
                    && parentSmc.value != null
                    && change.CreateAtIndex < parentSmc.value.Count)
                {
                    parentSmc.value.Insert(change.CreateAtIndex, sme2);
                    change.FoundReferable = dataRef;
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Create,
                        thisRef: sme2, parentRef: parent, createAtIndex: change.CreateAtIndex));
                }
                else
                // add to AAS
                if (parent is AdminShell.AdministrationShell parentAas
                    && dataRef is AdminShell.Submodel sm
                    && Env?.AasEnv != null)
                {
                    Env.AasEnv.Submodels.Add(sm);
                    parentAas.AddSubmodelRef(sm?.GetSubmodelRef());
                    change.FoundReferable = dataRef;
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Create,
                        thisRef: sm, parentRef: parent));
                }
                else
                {
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                        info: "PackageConnector::PullEvents() Create " +
                        "Exception creating data within Observable/path! " + change.Path.ToString(1)));
                    return;
                }
            }

            // delete
            if (change.Reason == StructuralChangeReason.Delete)
            {
                // need target
                if (target == null)
                {
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                        info: "PackageConnector::PullEvents() Delete " +
                        "Cannot find target Referable! " + change.Path.ToString(1)));
                    return;
                }

                // need target
                if (parent == null)
                {
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                        info: "PackageConnector::PullEvents() Delete " +
                        "Cannot find parent Referable for target! " + change.Path.ToString(1)));
                    return;
                }

                // go through some cases
                // all SM, SME with dependent elements
                if (parent is AdminShell.IManageSubmodelElements parentMgr
                    && target is AdminShell.SubmodelElement sme)
                {
                    // Note: assumption is, that Remove() will not throw exception,
                    // if sme does not exist. Sadly, there is also no exception to 
                    // handler in this case
                    change.FoundReferable = target;
                    parentMgr.Remove(sme);
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Delete,
                        thisRef: sme, parentRef: parent));
                }
                else
                // delete SM from AAS
                // Note: this implementation requires, that path consists of:
                //       <AAS> , <SM>
                // TODO (MIHO, 2021-05-21): make sure, this is required by the specification!
                if (parent is AdminShell.AdministrationShell parentAas
                    && parentAas.submodelRefs != null
                    && targetKl.Count >= 1
                    && target is AdminShell.Submodel sm
                    && Env?.AasEnv != null)
                {
                    AdminShell.SubmodelRef smrefFound = null;
                    foreach (var smref in parentAas.submodelRefs)
                        if (smref.Matches(targetKl.Last(), AdminShell.Key.MatchMode.Relaxed))
                            smrefFound = smref;

                    if (smrefFound == null)
                    {
                        handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                            info: "PackageConnector::PullEvents() Delete " +
                            "Cannot find SubmodelRef in target AAS!"));
                        return;
                    }

                    change.FoundReferable = target;

                    parentAas.submodelRefs.Remove(smrefFound);
                    Env.AasEnv.Submodels.Remove(sm);
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Delete,
                        thisRef: target, parentRef: parent));
                }
                else
                {
                    handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                        info: "PackageConnector::PullEvents() Create " +
                        "Exception deleting data within Observable/path! " + change.Path.ToString(1)));
                }
            }

            // modify
            // TODO (MIHO, 2021-10-09): Modify missing!!
            if (change.Reason == StructuralChangeReason.Modify)
            {
                throw new NotImplementedException("ExecuteEventAction() for Modify!!");
            }
        }

        private void ExecuteEventAction(
            AasPayloadUpdateValueItem value,
            AasEventMsgEnvelope env,
            PackCntChangeEventHandler handler = null)
        {
            // trivial
            if (value?.Path == null)
                return;

            // try determine tarket of "Observable"/"path"
            var targetKl = new AdminShell.KeyList();
            AdminShell.Referable target = null;
            if (value.Path?.IsEmpty == false)
            {
                if (env.ObservableReference?.Keys != null)
                    targetKl = new AdminShell.KeyList(env.ObservableReference.Keys);

                targetKl.AddRange(value.Path);

                if (targetKl.First().IsAbsolute())
                    target = Env?.AasEnv?.FindReferableByReference(targetKl);
            }

            // no target?
            if (target == null)
            {
                handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                    info: "PackageConnector::PullEvents() Update " +
                    "Cannot find target Referable!"));
                return;
            }

            // remember (e.g. to be processed further by lugins or similar)
            value.FoundReferable = target;

            // try to update
            if (target is AdminShell.AdministrationShell)
            {
                handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                    info: "PackageConnector::PullEvents() Update " +
                    "Update of AAS not implemented!"));
                // TODO (MIHO, 2021-05-28): to be implemented
                return;
            }

            if (target is AdminShell.Submodel)
            {
                handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                    info: "PackageConnector::PullEvents() Update " +
                    "Update of Submodel not implemented!"));
                // TODO (MIHO, 2021-05-28): to be implemented
                return;
            }

            if (target is AdminShell.SubmodelElementCollection)
            {
                handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                    info: "PackageConnector::PullEvents() Update " +
                    "Update of SubmodelElementCollection not implemented!"));
                // TODO (MIHO, 2021-05-28): to be implemented
                return;
            }

            if (target is AdminShell.SubmodelElement sme)
            {
                // use differentiated functionality
                PackageContainerBase.UpdateSmeFromEventPayloadItem(sme, value);

                handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.ValueUpdateSingle,
                        thisRef: sme));
                return;
            }

            // else
            handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.Exception,
                info: "PackageConnector::PullEvents() Update " +
                "Target for path not recognised: " + value.Path));
        }

        public async Task<DateTime> PullEvents(string qst)
        {
            // access
            if (!IsValid() || !qst.HasContent())
                throw new PackageConnectorException("PackageConnector::PullEvents() " +
                    "connection not valid!");

            // will return maximum timestamp
            DateTime lastTS = DateTime.MinValue;

            // do the query
            if (OpenIDClient.email != "")
                _client.DefaultRequestHeaders.Add("Email", OpenIDClient.email);

            var response = await _client.GetAsync(_endPointSegments + qst);
            if (!response.IsSuccessStatusCode)
                throw new PackageConnectorException($"PackageConnector::PullEvents() " +
                    $"Server did not respond correctly on query {qst} !");

            // ok
            // parse dynamic response object
            var settings = AasxIntegrationBase.AasxPluginOptionSerialization.GetDefaultJsonSettings(
                    new[] { typeof(AasEventMsgEnvelope) });
            AasEventMsgEnvelope[] envelopes;
            try
            {
                envelopes = Newtonsoft.Json.JsonConvert.DeserializeObject<AasEventMsgEnvelope[]>(
                    await response.Content.ReadAsStringAsync(), settings);
            }
            catch (Exception ex)
            {
                throw new PackageConnectorException($"PackageConnector::PullEvents() " +
                                    "Error parsing event message payload(s)", ex);
            }

            // change handler, start?
            var handler = Container?.PackageCentral?.ChangeEventHandler;
            handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.StartOfChanges));

            if (envelopes != null)
                foreach (var env in envelopes)
                {
                    // trivial
                    if (env?.PayloadItems == null)
                        continue;

                    // header parsing
                    if (env.Timestamp > lastTS)
                        lastTS = env.Timestamp;

                    // payloads
                    foreach (var pl in env.PayloadItems)
                    {
                        // Structural
                        if (pl is AasPayloadStructuralChange chgStruct)
                        {
                            // trivial
                            if (chgStruct?.Changes == null)
                                continue;

                            foreach (var change in chgStruct.Changes)
                                ExecuteEventAction(change, env, handler);
                        }

                        // Update Value
                        if (pl is AasPayloadUpdateValue chgUpdate)
                        {
                            // trivial
                            if (chgUpdate?.Values == null)
                                continue;

                            foreach (var value in chgUpdate.Values)
                                ExecuteEventAction(value, env, handler);
                        }
                    }

                    // send to upper structure?
                    if (true)
                        Container?.PackageCentral?.PushEvent(env);
                }
#if _not_now

            // which events?
            if (frame != null
                && frame.ContainsKey("Changes")
                && (frame["Changes"] is Newtonsoft.Json.Linq.JArray changes)
                && changes.Count > 0)
                foreach (var changeJO in changes)
                {
                    // access
                    if (changeJO == null)
                        continue;

                    // try deserialize
                    var change = changeJO.ToObject<AasPayloadStructuralChangeItem>();
                    if (change == null)
                        throw new PackageConnectorException($"PackageConnector::PullEvents() " +
                            "Cannot deserize payload StructuralChangeItem!");

                    // try determine tarket of {Observavle}/{path}
                    AdminShell.Referable target = null;
                    if (change.Path?.IsEmpty == false)
                    {
                        AdminShell.KeyList kl = null;
                        if (change.Path.First().IsAbsolute())
                            kl = change.Path;
                        else
                        {
                            // { Observavle}/{ path}
                            // need outer event!
                            throw new NotImplementedException("Outer Event Message!");
                        }
                        target = Env?.AasEnv?.FindReferableByReference(kl);
                    }

                    // create
                    if (change.Reason == AasPayloadStructuralChangeItem.ChangeReason.Create)
                    {
                        // need data object
                        var dataRef = change.GetDataAsReferable();
                        if (dataRef == null)
                            throw new PackageConnectorException($"PackageConnector::PullEvents() " +
                                "Cannot deserize StructuralChangeItem Referable data!");

                        // go through some cases
                        // all SM, SME with dependent elements
                        if (target is AdminShell.IManageSubmodelElements targetMgr
                            && dataRef is AdminShell.SubmodelElement sme
                            && change.CreateAtIndex < 0)
                        {
                            targetMgr.Add(sme);
                        }
                        else
                        // at least for SMC, handle CreateAtIndex
                        if (target is AdminShell.SubmodelElementCollection targetSmc
                            && dataRef is AdminShell.SubmodelElement sme2
                            && change.CreateAtIndex >= 0
                            && targetSmc.value != null
                            && change.CreateAtIndex < targetSmc.value.Count)
                        {
                            targetSmc.value.Insert(change.CreateAtIndex, sme2);
                        }
                        else
                        // add to AAS
                        if (target is AdminShell.AdministrationShell targetAas
                            && dataRef is AdminShell.Submodel sm
                            && Env?.AasEnv != null)
                        {
                            Env.AasEnv.Submodels.Add(sm);
                            targetAas.AddSubmodelRef(sm?.GetSubmodelRef());
                        }
                        else
                            throw new PackageConnectorException($"PackageConnector::PullEvents() " +
                                "Error creating data within Observable/path!");
                    }
                }

#endif

            // change handler, start?
            handler?.Invoke(new PackCntChangeEventData(Container, PackCntChangeEventReason.EndOfChanges));

            // ok
            return lastTS;
        }

    }
}
