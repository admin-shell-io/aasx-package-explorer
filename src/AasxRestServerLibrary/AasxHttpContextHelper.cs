/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxCompatibilityModels;
using AdminShellNS;
using Grapevine.Interfaces.Server;
using Grapevine.Server;
using Grapevine.Server.Attributes;
using Grapevine.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

/*
Please notice:
The API and REST routes implemented in this version of the source code are not specified and standardised by the
specification Details of the Administration Shell. The hereby stated approach is solely the opinion of its author(s).
*/

namespace AasxRestServerLibrary
{
    public class AasxHttpContextHelper
    {

        public AdminShellPackageEnv Package = null;

        public AasxHttpHandleStore IdRefHandleStore = new AasxHttpHandleStore();

        #region // Path helpers

        public bool PathEndsWith(string path, string tag)
        {
            return path.Trim().ToLower().TrimEnd('/').EndsWith(tag);
        }

        public bool PathEndsWith(IHttpContext context, string tag)
        {
            return PathEndsWith(context.Request.PathInfo, tag);
        }

        // see also: https://stackoverflow.com/questions/33619469/
        // how-do-i-write-a-regular-expression-to-route-traffic-with-grapevine-when-my-requ

        public Match PathInfoRegexMatch(MethodBase methodWithRestRoute, string input)
        {
            if (methodWithRestRoute == null)
                return null;
            string piRegex = null;
            foreach (var attr in methodWithRestRoute.GetCustomAttributes<RestRoute>())
                if (attr.PathInfo != null)
                    piRegex = attr.PathInfo;
            if (piRegex == null)
                return null;
            var m = Regex.Match(input, piRegex);
            return m;
        }

        public List<AasxHttpHandleIdentification> CreateHandlesFromQueryString(
            System.Collections.Specialized.NameValueCollection queryStrings)
        {
            // start
            var res = new List<AasxHttpHandleIdentification>();
            if (queryStrings == null)
                return res;

            // over all query strings
            foreach (var kr in queryStrings.AllKeys)
            {
                try
                {
                    var k = kr.Trim().ToLower();
                    var v = queryStrings[k];
                    if (k.StartsWith("q") && k.Length > 1 && v.Contains(','))
                    {
                        var vl = v.Split(',');
                        if (vl.Length == 2)
                        {
                            var id = new AdminShell.Identification(vl[0], vl[1]);
                            var h = new AasxHttpHandleIdentification(id, "@" + k);
                            res.Add(h);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }

            // done
            return res;
        }

        public List<AasxHttpHandleIdentification> CreateHandlesFromRawUrl(string rawUrl)
        {
            // start
            var res = new List<AasxHttpHandleIdentification>();
            if (rawUrl == null)
                return res;

            // un-escape
            var url = System.Uri.UnescapeDataString(rawUrl);

            // split for query string traditional
            var i = url.IndexOf('?');
            if (i < 0 || i == url.Length - 1)
                return res;
            var query = url.Substring(i + 1);

            // try make a Regex wonder, again
            var m = Regex.Match(query, @"(\s*([^&]+)(&|))+");
            if (m.Success && m.Groups.Count >= 3)
                foreach (var cp in m.Groups[2].Captures)
                {
                    var m2 = Regex.Match(cp.ToString(), @"\s*(\w+)\s*=\s*([^,]+),(.+)$");
                    if (m2.Success && m2.Groups.Count >= 4)
                    {
                        var k = m2.Groups[1].ToString();
                        var idt = m2.Groups[2].ToString();
                        var ids = m2.Groups[3].ToString();

                        var id = new AdminShell.Identification(idt, ids);
                        var h = new AasxHttpHandleIdentification(id, "@" + k);
                        res.Add(h);
                    }
                }

            // done
            return res;
        }

        #endregion

        #region // Access package structures

        public AdminShell.AdministrationShell FindAAS(
            string aasid, System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            // trivial
            if (Package == null || Package.AasEnv == null || Package.AasEnv.AdministrationShells == null ||
                Package.AasEnv.AdministrationShells.Count < 1)
                return null;

            // default aas?
            if (aasid == null || aasid.Trim() == "" || aasid.Trim().ToLower() == "id")
                return Package.AasEnv.AdministrationShells[0];

            // resolve an ID?
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(aasid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Package.AasEnv.FindAAS(handleId.identification);

            // no, iterate over idShort
            return Package.AasEnv.FindAAS(aasid);
        }

        public AdminShell.SubmodelRef FindSubmodelRefWithinAas(
            AdminShell.AdministrationShell aas, string smid,
            System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            // trivial
            if (Package == null || Package.AasEnv == null || aas == null || smid == null || smid.Trim() == "")
                return null;

            // via handle
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(smid, specialHandles);

            // no, iterate & find
            foreach (var smref in aas.submodelRefs)
            {
                if (handleId != null && handleId.identification != null)
                {
                    if (smref.Matches(handleId.identification))
                        return smref;
                }
                else
                {
                    var sm = this.Package.AasEnv.FindSubmodel(smref);
                    if (sm != null && sm.idShort != null && sm.idShort.Trim().ToLower() == smid.Trim().ToLower())
                        return smref;
                }
            }

            // no
            return null;
        }

        public AdminShell.Submodel FindSubmodelWithinAas(
            AdminShell.AdministrationShell aas, string smid,
            System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            // trivial
            if (Package == null || Package.AasEnv == null || aas == null || smid == null || smid.Trim() == "")
                return null;

            // via handle
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(smid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Package.AasEnv.FindSubmodel(handleId.identification);

            // no, iterate & find
            foreach (var smref in aas.submodelRefs)
            {
                var sm = this.Package.AasEnv.FindSubmodel(smref);
                if (sm != null && sm.idShort != null && sm.idShort.Trim().ToLower() == smid.Trim().ToLower())
                    return sm;
            }

            // no
            return null;
        }

        public AdminShell.Submodel FindSubmodelWithoutAas(
            string smid, System.Collections.Specialized.NameValueCollection queryStrings = null,
            string rawUrl = null)
        {
            // trivial
            if (Package == null || Package.AasEnv == null || smid == null || smid.Trim() == "")
                return null;

            // via handle
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(smid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Package.AasEnv.FindSubmodel(handleId.identification);

            // no, iterate & find
            foreach (var sm in this.Package.AasEnv.Submodels)
            {
                if (sm != null && sm.idShort != null && sm.idShort.Trim().ToLower() == smid.Trim().ToLower())
                    return sm;
            }

            // no
            return null;
        }

        public AdminShell.ConceptDescription FindCdWithoutAas(
            AdminShell.AdministrationShell aas, string cdid,
            System.Collections.Specialized.NameValueCollection queryStrings = null, string rawUrl = null)
        {
            // trivial
            if (Package == null || Package.AasEnv == null || aas == null || cdid == null || cdid.Trim() == "")
                return null;

            // via handle
            var specialHandles = this.CreateHandlesFromRawUrl(rawUrl);
            var handleId = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(cdid, specialHandles);
            if (handleId != null && handleId.identification != null)
                return Package.AasEnv.FindConceptDescription(handleId.identification);

            // no, iterate & find
            foreach (var cd in Package.AasEnv.ConceptDescriptions)
            {
                if (cd.idShort != null && cd.idShort.Trim().ToLower() == cdid.Trim().ToLower())
                    return cd;
            }

            // no
            return null;
        }


        public class FindSubmodelElementResult
        {
            public AdminShell.Referable elem = null;
            public AdminShell.SubmodelElementWrapper wrapper = null;
            public AdminShell.Referable parent = null;

            public FindSubmodelElementResult(
                AdminShell.Referable elem = null, AdminShell.SubmodelElementWrapper wrapper = null,
                AdminShell.Referable parent = null)
            {
                this.elem = elem;
                this.wrapper = wrapper;
                this.parent = parent;
            }
        }

        public FindSubmodelElementResult FindSubmodelElement(
            AdminShell.Referable parent, List<AdminShell.SubmodelElementWrapper> wrappers,
            string[] elemids, int elemNdx = 0)
        {
            // trivial
            if (wrappers == null || elemids == null || elemNdx >= elemids.Length)
                return null;

            // dive into each
            foreach (var smw in wrappers)
                if (smw.submodelElement != null)
                {
                    // idShort need to match
                    if (smw.submodelElement.idShort.Trim().ToLower() != elemids[elemNdx].Trim().ToLower())
                        continue;

                    // leaf
                    if (elemNdx == elemids.Length - 1)
                    {
                        return new FindSubmodelElementResult(elem: smw.submodelElement, wrapper: smw, parent: parent);
                    }
                    else
                    {
                        // recurse into?
                        var xsmc = smw.submodelElement as AdminShell.SubmodelElementCollection;
                        if (xsmc != null)
                        {
                            var r = FindSubmodelElement(xsmc, xsmc.value, elemids, elemNdx + 1);
                            if (r != null)
                                return r;
                        }

                        var xop = smw.submodelElement as AdminShell.Operation;
                        if (xop != null)
                        {
                            var w2 = new List<AdminShell.SubmodelElementWrapper>();
                            for (int i = 0; i < 2; i++)
                                foreach (var opv in xop[i])
                                    if (opv.value != null)
                                        w2.Add(opv.value);

                            var r = FindSubmodelElement(xop, w2, elemids, elemNdx + 1);
                            if (r != null)
                                return r;
                        }
                    }
                }

            // nothing
            return null;
        }

        #endregion

        #region // Generate responses


        protected static void SendJsonResponse(
            IHttpContext context, object obj, IContractResolver contractResolver = null)
        {
            var settings = new JsonSerializerSettings();
            if (contractResolver != null)
                settings.ContractResolver = contractResolver;
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
            var buffer = context.Request.ContentEncoding.GetBytes(json);
            var length = buffer.Length;

            context.Response.ContentType = ContentType.JSON;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = length;
            context.Response.SendResponse(buffer);
        }

        protected static void SendTextResponse(IHttpContext context, string txt, string mimeType = null)
        {
            context.Response.ContentType = ContentType.TEXT;
            if (mimeType != null)
                context.Response.Advanced.ContentType = mimeType;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = txt.Length;
            context.Response.SendResponse(txt);
        }

        protected static void SendStreamResponse(
            IHttpContext context, Stream stream, string headerAttachmentFileName = null)
        {
            context.Response.ContentType = ContentType.APPLICATION;
            context.Response.ContentLength64 = stream.Length;
            context.Response.SendChunked = true;

            if (headerAttachmentFileName != null)
                context.Response.AddHeader("Content-Disposition", $"attachment; filename={headerAttachmentFileName}");

            stream.CopyTo(context.Response.Advanced.OutputStream);
            context.Response.Advanced.Close();
        }

        #endregion

        #region AAS and Asset

        public void EvalGetAasAndAsset(IHttpContext context, string aasid, bool deep = false, bool complete = false)
        {
            // access the first AAS
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No AAS with id '{aasid}' found.");
                return;
            }

            // try to get the asset as well
            var asset = this.Package.AasEnv.FindAsset(aas.assetRef);

            // result
            dynamic res = new ExpandoObject();
            res.AAS = aas;
            res.Asset = asset;

            // return as JSON
            var cr = new AdminShellConverters.AdaptiveFilterContractResolver(deep: deep, complete: complete);
            SendJsonResponse(context, res, cr);
        }

        public void EvalGetAasEnv(IHttpContext context, string aasid)
        {
            if (this.Package == null || this.Package.AasEnv == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // access the first AAS
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No AAS with id '{aasid}' found.");
                return;
            }

            // create a new, filtered AasEnv
            AdminShell.AdministrationShellEnv copyenv = null;
            try
            {
                copyenv = AdminShell.AdministrationShellEnv.CreateFromExistingEnv(
                    this.Package.AasEnv, filterForAas: new List<AdminShell.AdministrationShell>(new[] { aas }));
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"Cannot filter aas envioronment: {ex.Message}.");
                return;
            }

            // return as FILE
            try
            {
                using (var ms = new MemoryStream())
                {
                    // build a file name
                    var fn = "aasenv.json";
                    if (aas.idShort != null)
                        fn = aas.idShort + "." + fn;
                    // serialize via helper
                    var jsonwriter = copyenv.SerialiazeJsonToStream(new StreamWriter(ms), leaveJsonWriterOpen: true);
                    // write out again
                    ms.Position = 0;
                    SendStreamResponse(context, ms, Path.GetFileName(fn));
                    // bit ugly
                    jsonwriter.Close();
                }
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest,
                    $"Cannot serialize and send aas envioronment: {ex.Message}.");
            }
        }


        public void EvalGetAasThumbnail(IHttpContext context, string aasid)
        {
            if (this.Package == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // access the first AAS
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No AAS with id '{aasid}' found.");
                return;
            }

            // access the thumbnail
            // Note: in this version, the thumbnail is not specific to the AAS, but maybe in later versions ;-)
            Uri thumbUri = null;
            var thumbStream = this.Package.GetLocalThumbnailStream(ref thumbUri);
            if (thumbStream == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No thumbnail available in package.");
                return;
            }

            // return as FILE
            SendStreamResponse(context, thumbStream, Path.GetFileName(thumbUri?.ToString() ?? ""));
            thumbStream.Close();
        }

        public void EvalPutAas(IHttpContext context)
        {
            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // list of Identification
            AdminShell.AdministrationShell aas = null;
            try
            {
                aas = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.AdministrationShell>(
                    context.Request.Payload);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            // need id for idempotent behaviour
            if (aas.identification == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest,
                    $"Identification of entity is (null); PUT cannot be performed.");
                return;
            }

            // datastructure update
            if (this.Package == null || this.Package.AasEnv == null ||
                this.Package.AasEnv.AdministrationShells == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }
            context.Server.Logger.Debug(
                $"Putting AdministrationShell with idShort {aas.idShort ?? "--"} and " +
                $"id {aas.identification.ToString()}");
            var existingAas = this.Package.AasEnv.FindAAS(aas.identification);
            if (existingAas != null)
                this.Package.AasEnv.AdministrationShells.Remove(existingAas);
            this.Package.AasEnv.AdministrationShells.Add(aas);

            // simple OK
            SendTextResponse(context, "OK" + ((existingAas != null) ? " (updated)" : " (new)"));
        }

        public void EvalDeleteAasAndAsset(IHttpContext context, string aasid, bool deleteAsset = false)
        {
            // datastructure update
            if (this.Package == null || this.Package.AasEnv == null ||
                this.Package.AasEnv.AdministrationShells == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // access the AAS
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No AAS with idShort '{aasid}' found.");
                return;
            }

            // find the asset
            var asset = this.Package.AasEnv.FindAsset(aas.assetRef);

            // delete
            context.Server.Logger.Debug(
                $"Deleting AdministrationShell with idShort {aas.idShort ?? "--"} and " +
                $"id {aas.identification?.ToString() ?? "--"}");
            this.Package.AasEnv.AdministrationShells.Remove(aas);

            if (deleteAsset && asset != null)
            {
                context.Server.Logger.Debug(
                    $"Deleting Asset with idShort {asset.idShort ?? "--"} and " +
                    $"id {asset.identification?.ToString() ?? "--"}");
                this.Package.AasEnv.Assets.Remove(asset);
            }

            // simple OK
            SendTextResponse(context, "OK");
        }

        #endregion

        #region // Asset links

        public void EvalGetAssetLinks(IHttpContext context, string assetid)
        {
            // trivial
            if (assetid == null)
                return;

            // do a manual search
            var res = new List<ExpandoObject>();
            var specialHandles = this.CreateHandlesFromQueryString(context.Request.QueryString);
            var handle = IdRefHandleStore.ResolveSpecific<AasxHttpHandleIdentification>(assetid, specialHandles);
            if (handle != null && handle.identification != null)
            {
                foreach (var aas in this.Package.AasEnv.AdministrationShells)
                    if (aas.assetRef != null && aas.assetRef.Matches(handle.identification))
                    {
                        dynamic o = new ExpandoObject();
                        o.identification = aas.identification;
                        o.idShort = aas.idShort;
                        res.Add(o);
                    }
            }
            else
            {
                foreach (var aas in this.Package.AasEnv.AdministrationShells)
                    if (aas.idShort != null && aas.idShort.Trim() != "" &&
                        aas.idShort.Trim().ToLower() == assetid.Trim().ToLower())
                    {
                        dynamic o = new ExpandoObject();
                        o.identification = aas.identification;
                        o.idShort = aas.idShort;
                        res.Add(o);
                    }
            }

            // return as JSON
            SendJsonResponse(context, res);
        }

        public void EvalPutAsset(IHttpContext context)
        {
            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // de-serialize asset
            AdminShell.Asset asset = null;
            try
            {
                asset = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.Asset>(context.Request.Payload);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            // need id for idempotent behaviour
            if (asset.identification == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest,
                    $"Identification of entity is (null); PUT cannot be performed.");
                return;
            }

            // datastructure update
            if (this.Package == null || this.Package.AasEnv == null || this.Package.AasEnv.Assets == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.InternalServerError,
                    $"Error accessing internal data structures.");
                return;
            }
            context.Server.Logger.Debug($"Adding Asset with idShort {asset.idShort ?? "--"}");
            var existingAsset = this.Package.AasEnv.FindAsset(asset.identification);
            if (existingAsset != null)
                this.Package.AasEnv.Assets.Remove(existingAsset);
            this.Package.AasEnv.Assets.Add(asset);

            // simple OK
            SendTextResponse(context, "OK" + ((existingAsset != null) ? " (updated)" : " (new)"));
        }
        #endregion

        #region // List of Submodels

        public class GetSubmodelsItem
        {
            public AdminShell.Identification id = new AdminShell.Identification();
            public string idShort = "";
            public string kind = "";

            public GetSubmodelsItem() { }

            public GetSubmodelsItem(AdminShell.Identification id, string idShort, string kind)
            {
                this.id = id;
                this.idShort = idShort;
                this.kind = kind;
            }

            public GetSubmodelsItem(AdminShell.Identifiable idi, string kind)
            {
                this.id = idi.identification;
                this.idShort = idi.idShort;
                this.kind = kind;
            }
        }

        public void EvalGetSubmodels(IHttpContext context, string aasid)
        {
            // access the AAS
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No AAS with idShort '{aasid}' found.");
                return;
            }

            // build a list of results
            var res = new List<GetSubmodelsItem>();

            // get all submodels
            foreach (var smref in aas.submodelRefs)
            {
                var sm = this.Package.AasEnv.FindSubmodel(smref);
                if (sm != null)
                    res.Add(new GetSubmodelsItem(sm, sm.kind.kind));
            }

            // return as JSON
            SendJsonResponse(context, res);
        }

        public void EvalPutSubmodel(IHttpContext context, string aasid)
        {
            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // access the AAS
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No AAS with idShort '{aasid}' found.");
                return;
            }

            // de-serialize Submodel
            AdminShell.Submodel submodel = null;
            try
            {
                submodel = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.Submodel>(context.Request.Payload);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            // need id for idempotent behaviour
            if (submodel.identification == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest,
                    $"Identification of entity is (null); PUT cannot be performed.");
                return;
            }

            // datastructure update
            if (this.Package == null || this.Package.AasEnv == null || this.Package.AasEnv.Assets == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // add Submodel
            context.Server.Logger.Debug(
                $"Adding Submodel with idShort {submodel.idShort ?? "--"} and " +
                $"id {submodel.identification?.ToString()}");
            var existingSm = this.Package.AasEnv.FindSubmodel(submodel.identification);
            if (existingSm != null)
                this.Package.AasEnv.Submodels.Remove(existingSm);
            this.Package.AasEnv.Submodels.Add(submodel);

            // add SubmodelRef to AAS
            var newsmr = AdminShell.SubmodelRef.CreateNew(
                "Submodel", true, submodel.identification.idType, submodel.identification.id);
            var existsmr = aas.HasSubmodelRef(newsmr);
            if (!existsmr)
            {
                context.Server.Logger.Debug(
                    $"Adding SubmodelRef to AAS with idShort {aas.idShort ?? "--"} and " +
                    $"id {aas.identification?.ToString() ?? "--"}");
                aas.AddSubmodelRef(newsmr);
            }

            // simple OK
            SendTextResponse(context, "OK" + ((existingSm != null) ? " (updated)" : " (new)"));
        }

        public void EvalDeleteSubmodel(IHttpContext context, string aasid, string smid)
        {
            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // access the AAS (absolutely mandatory)
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No AAS with idShort '{aasid}' found.");
                return;
            }

            // delete SubmodelRef 1st
            var smref = this.FindSubmodelRefWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            if (smref != null)
            {
                context.Server.Logger.Debug(
                    $"Removing SubmodelRef {smid} from AAS with idShort {aas.idShort ?? "--"} and " +
                    $"id {aas.identification?.ToString() ?? "--"}");
                aas.submodelRefs.Remove(smref);
            }

            // delete Submodel 2nd
            var sm = this.FindSubmodelWithoutAas(smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm != null)
            {
                context.Server.Logger.Debug($"Removing Submodel {smid} from data structures.");
                this.Package.AasEnv.Submodels.Remove(sm);
            }

            // simple OK
            var cmt = "";
            if (smref == null && sm == null)
                cmt += " (nothing deleted)";
            cmt += ((smref != null) ? " (SubmodelRef deleted)" : "") + ((sm != null) ? " (Submodel deleted)" : "");
            SendTextResponse(context, "OK" + cmt);
        }

        #endregion

        #region // Submodel Complete

        public void EvalGetSubmodelContents(
            IHttpContext context, string aasid, string smid, bool deep = false, bool complete = false)
        {
            // access AAS and Submodel
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // return as JSON
            var cr = new AdminShellConverters.AdaptiveFilterContractResolver(deep: deep, complete: complete);
            SendJsonResponse(context, sm, cr);
        }

        public void EvalGetSubmodelContentsAsTable(IHttpContext context, string aasid, string smid)
        {
            // access AAS and Submodel
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // AAS ENV
            if (this.Package == null || this.Package.AasEnv == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.InternalServerError,
                    $"Error accessing internal data structures.");
                return;
            }

            // make a table
            var table = new List<ExpandoObject>();
            sm.RecurseOnSubmodelElements(null, (o, parents, sme) =>
            {
                // start a row
                dynamic row = new ExpandoObject();

                // defaults
                row.idShorts = "";
                row.typeName = "";
                row.semIdType = "";
                row.semId = "";
                row.shortName = "";
                row.unit = "";
                row.value = "";

                // idShort is a concatenation
                var path = "";
                foreach (var p in parents)
                    path += p.idShort + "/";

                // SubnmodelElement general
                row.idShorts = path + (sme.idShort ?? "(-)");
                row.typeName = sme.GetElementName();
                if (sme.semanticId == null || sme.semanticId.Keys == null)
                { }
                else if (sme.semanticId.Keys.Count > 1)
                {
                    row.semId = "(complex)";
                }
                else
                {
                    row.semIdType = sme.semanticId.Keys[0].idType;
                    row.semId = sme.semanticId.Keys[0].value;
                }

                // try find a concept description
                if (sme.semanticId != null)
                {
                    var cd = this.Package.AasEnv.FindConceptDescription(sme.semanticId.Keys);
                    if (cd != null)
                    {
                        var ds = cd.GetIEC61360();
                        if (ds != null)
                        {
                            row.shortName = (ds.shortName == null ? "" : ds.shortName.GetDefaultStr());
                            row.unit = ds.unit ?? "";
                        }
                    }
                }

                // try add a value
                if (sme is AdminShell.Property)
                {
                    var p = sme as AdminShell.Property;
                    row.value = "" + (p.value ?? "") + ((p.valueId != null) ? p.valueId.ToString() : "");
                }

                if (sme is AdminShell.File)
                {
                    var p = sme as AdminShell.File;
                    row.value = "" + p.value;
                }

                if (sme is AdminShell.Blob)
                {
                    var p = sme as AdminShell.Blob;
                    if (p.value.Length < 128)
                        row.value = "" + p.value;
                    else
                        row.value = "(" + p.value.Length + " bytes)";
                }

                if (sme is AdminShell.ReferenceElement)
                {
                    var p = sme as AdminShell.ReferenceElement;
                    row.value = "" + p.value.ToString();
                }

                if (sme is AdminShell.RelationshipElement)
                {
                    var p = sme as AdminShell.RelationshipElement;
                    row.value = "" + (p.first?.ToString() ?? "(-)") + " <-> " + (p.second?.ToString() ?? "(-)");
                }

                // now, add the row
                table.Add(row);

                // recurse
                return true;
            });

            // return as JSON
            SendJsonResponse(context, table);
        }

        #endregion

        #region // Submodel Elements

        public void EvalGetSubmodelElementContents(
            IHttpContext context, string aasid, string smid, string[] elemids, bool deep = false,
            bool complete = false)
        {
            // access AAS and Submodel
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var sme = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
            if (sme == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No matching element in Submodel found.");
                return;
            }

            // return as JSON
            var cr = new AdminShellConverters.AdaptiveFilterContractResolver(deep: deep, complete: complete);
            SendJsonResponse(context, sme, cr);
        }

        public void EvalGetSubmodelElementsBlob(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            // access AAS and Submodel
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var fse = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
            var smeb = fse?.elem as AdminShell.Blob;
            if (smeb == null || smeb.value == null || smeb.value == "")
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No matching Blob element in Submodel found.");
                return;
            }

            // return as TEXT
            SendTextResponse(context, smeb.value, mimeType: smeb.mimeType);
        }

        public void EvalGetSubmodelElementsProperty(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            // access AAS and Submodel
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var fse = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
            var smep = fse?.elem as AdminShell.Property;
            if (smep == null || smep.value == null || smep.value == "")
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No matching Property element in Submodel found.");
                return;
            }

            // a little bit of demo
            string strval = smep.value;
            if (smep.HasQualifierOfType("DEMO") != null && smep.value != null && smep.valueType != null &&
                smep.valueType.Trim().ToLower() == "double" &&
                double.TryParse(smep.value, NumberStyles.Any, CultureInfo.InvariantCulture, out double dblval))
            {
                dblval += Math.Sin((0.001 * DateTime.UtcNow.Millisecond) * 6.28);
                strval = dblval.ToString(CultureInfo.InvariantCulture);
            }

            // return as little dynamic object
            dynamic res = new ExpandoObject();
            res.value = strval;
            if (smep.valueId != null)
                res.valueId = smep.valueId;

            // send
            SendJsonResponse(context, res);
        }

        public void EvalGetSubmodelElementsFile(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            // access AAS and Submodel
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var fse = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
            var smef = fse?.elem as AdminShell.File;
            if (smef == null || smef.value == null || smef.value == "")
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No matching File element in Submodel found.");
                return;
            }

            // access
            var packageStream = this.Package.GetLocalStreamFromPackage(smef.value);
            if (packageStream == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No file contents available in package.");
                return;
            }

            // return as FILE
            SendStreamResponse(context, packageStream, Path.GetFileName(smef.value));
            packageStream.Close();
        }

        public void EvalPutSubmodelElementContents(IHttpContext context, string aasid, string smid, string[] elemids)
        {
            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // de-serialize SubmodelElement
            AdminShell.SubmodelElement sme = null;
            try
            {
                sme = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.SubmodelElement>(
                    context.Request.Payload, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            // need id for idempotent behaviour
            if (sme?.idShort == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest,
                    $"entity or idShort of entity is (null); PUT cannot be performed.");
                return;
            }

            // access AAS and Submodel
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // special case: parent is Submodel itself
            var updated = false;
            if (elemids == null || elemids.Length < 1)
            {
                var existsmw = sm.FindSubmodelElementWrapper(sme.idShort);
                if (existsmw != null)
                {
                    updated = true;
                    context.Server.Logger.Debug($"Removing old SubmodelElement {sme.idShort} from Submodel {smid}.");
                    sm.submodelElements.Remove(existsmw);
                }

                context.Server.Logger.Debug($"Adding new SubmodelElement {sme.idShort} to Submodel {smid}.");
                sm.Add(sme);
            }
            else
            {
                // find the right SubmodelElement
                var parent = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
                if (parent == null)
                {
                    context.Response.SendResponse(
                        Grapevine.Shared.HttpStatusCode.NotFound, $"No matching element in Submodel found.");
                    return;
                }

                if (parent.elem != null && parent.elem is AdminShell.SubmodelElementCollection parentsmc)
                {
                    var existsmw = parentsmc.FindFirstIdShort(sme.idShort);
                    if (existsmw != null)
                    {
                        updated = true;
                        context.Server.Logger.Debug(
                            $"Removing old SubmodelElement {sme.idShort} from SubmodelCollection.");
                        parentsmc.value.Remove(existsmw);
                    }

                    context.Server.Logger.Debug($"Adding new SubmodelElement {sme.idShort} to SubmodelCollection.");
                    parentsmc.Add(sme);
                }
                else
                {
                    context.Response.SendResponse(
                        Grapevine.Shared.HttpStatusCode.BadRequest,
                        $"Matching SubmodelElement in Submodel {smid} is not suitable to add childs.");
                    return;
                }

            }

            // simple OK
            SendTextResponse(context, "OK" + (updated ? " (with updates)" : ""));
        }

        public void EvalDeleteSubmodelElementContents(
            IHttpContext context, string aasid, string smid, string[] elemids)
        {
            // access AAS and Submodel
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm == null || elemids == null || elemids.Length < 1)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found or " +
                    $"no elements to delete specified.");
                return;
            }

            // OK, Submodel and Element existing
            var fse = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
            if (fse == null || fse.elem == null || fse.parent == null || fse.wrapper == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No matching element in Submodel found.");
                return;
            }

            // where to delete?
            var deleted = false;
            var elinfo = string.Join(".", elemids);
            if (fse.parent == sm)
            {
                context.Server.Logger.Debug($"Deleting specified SubmodelElement {elinfo} from Submodel {smid}.");
                sm.submodelElements.Remove(fse.wrapper);
                deleted = true;
            }

            if (fse.parent is AdminShell.SubmodelElementCollection smc)
            {
                context.Server.Logger.Debug(
                    $"Deleting specified SubmodelElement {elinfo} from SubmodelElementCollection {smc.idShort}.");
                smc.value.Remove(fse.wrapper);
                deleted = true;
            }

            // simple OK
            SendTextResponse(context, "OK" + (!deleted ? " (but nothing deleted)" : ""));
        }

        public void EvalInvokeSubmodelElementOperation(
            IHttpContext context, string aasid, string smid, string[] elemids)
        {
            // access AAS and Submodel
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var sm = this.FindSubmodelWithinAas(aas, smid, context.Request.QueryString, context.Request.RawUrl);
            if (sm == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No AAS '{aasid}' or no Submodel with idShort '{smid}' found.");
                return;
            }

            // find the right SubmodelElement
            var fse = this.FindSubmodelElement(sm, sm.submodelElements, elemids);
            var smep = fse?.elem as AdminShell.Operation;
            if (smep == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No matching Operation element in Submodel found.");
                return;
            }

            // make 1st expectation
            int numExpectedInputArgs = smep.inputVariable?.Count ?? 0;
            int numGivenInputArgs = 0;
            int numExpectedOutputArgs = smep.outputVariable?.Count ?? 0;
            var inputArguments = (new int[numExpectedInputArgs]).Select(x => "").ToList();
            var outputArguments = (new int[numExpectedOutputArgs]).Select(x => "my value").ToList();

            // is a payload required? Always, if at least one input argument required

            if (smep.inputVariable != null && smep.inputVariable.Count > 0)
            {
                // payload present
                if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
                {
                    context.Response.SendResponse(
                        Grapevine.Shared.HttpStatusCode.BadRequest,
                        $"No payload for Operation input argument or content type is not JSON.");
                    return;
                }

                // de-serialize SubmodelElement
                try
                {
                    // serialize
                    var input = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(context.Request.Payload);

                    // set inputs
                    if (input != null && input.Count > 0)
                    {
                        numGivenInputArgs = input.Count;
                        for (int i = 0; i < numGivenInputArgs; i++)
                            inputArguments[i] = input[i];
                    }
                }
                catch (Exception ex)
                {
                    context.Response.SendResponse(
                        Grapevine.Shared.HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                    return;
                }
            }

            // do a check
            if (numExpectedInputArgs != numGivenInputArgs)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest,
                    $"Number of input arguments in payload does not fit expected input arguments of Operation.");
                return;
            }

            // just a test
            if (smep.HasQualifierOfType("DEMO") != null)
            {
                for (int i = 0; i < Math.Min(numExpectedInputArgs, numExpectedOutputArgs); i++)
                    outputArguments[i] = "CALC on " + inputArguments[i];
            }

            // return as little dynamic object
            SendJsonResponse(context, outputArguments);
        }

        public void EvalGetAllCds(IHttpContext context, string aasid)
        {
            // access the AAS
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No AAS with idShort '{aasid}' found.");
                return;
            }

            // build a list of results
            var res = new List<ExpandoObject>();

            // create a new, filtered AasEnv
            // (this is expensive, but delivers us with a list of CDs which are in relation to the respective AAS)
            var copyenv = AdminShell.AdministrationShellEnv.CreateFromExistingEnv(
                this.Package.AasEnv, filterForAas: new List<AdminShell.AdministrationShell>(new[] { aas }));

            // get all CDs and describe them
            foreach (var cd in copyenv.ConceptDescriptions)
            {
                // describe
                dynamic o = new ExpandoObject();
                o.idShort = cd.idShort;
                o.shortName = cd.GetDefaultShortName();
                o.identification = cd.identification;
                o.isCaseOf = cd.IsCaseOf;

                // add
                res.Add(o);
            }

            // return as JSON
            SendJsonResponse(context, res);
        }

        public void EvalGetCdContents(IHttpContext context, string aasid, string cdid)
        {
            // access AAS and CD
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var cd = this.FindCdWithoutAas(aas, cdid, context.Request.QueryString, context.Request.RawUrl);
            if (cd == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No AAS '{aasid}' or no ConceptDescription with id '{cdid}' found.");
                return;
            }

            // return as JSON
            SendJsonResponse(context, cd);
        }

        public void EvalDeleteSpecificCd(IHttpContext context, string aasid, string cdid)
        {
            // access AAS and CD
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            var cd = this.FindCdWithoutAas(aas, cdid, context.Request.QueryString, context.Request.RawUrl);
            if (cd == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    $"No AAS '{aasid}' or no ConceptDescription with id '{cdid}' found.");
                return;
            }

            // delete ?!
            var deleted = false;
            if (this.Package != null && this.Package.AasEnv != null &&
                this.Package.AasEnv.ConceptDescriptions.Contains(cd))
            {
                this.Package.AasEnv.ConceptDescriptions.Remove(cd);
                deleted = true;
            }

            // return as JSON
            SendTextResponse(context, "OK" + (!deleted ? " (but nothing deleted)" : ""));
        }

        #endregion

        #region // GET + POST handles/identification

        public void EvalGetHandlesIdentification(IHttpContext context)
        {
            // get the list
            var res = IdRefHandleStore.FindAll<AasxHttpHandleIdentification>();

            // return this list
            SendJsonResponse(context, res);
        }

        public void EvalPostHandlesIdentification(IHttpContext context)
        {
            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // list of Identification
            List<AdminShell.Identification> ids = null;
            try
            {
                ids = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AdminShell.Identification>>(
                    context.Request.Payload);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }
            if (ids == null || ids.Count < 1)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"No Identification entities in payload.");
                return;
            }

            // turn these list into a list of Handles
            var res = new List<AasxHttpHandleIdentification>();
            foreach (var id in ids)
            {
                var h = new AasxHttpHandleIdentification(id);
                IdRefHandleStore.Add(h);
                res.Add(h);
            }

            // return this list
            SendJsonResponse(context, res);
        }

        #endregion

        #region // Server profile ..

        public void EvalGetServerProfile(IHttpContext context)
        {
            // get the list
            dynamic res = new ExpandoObject();
            var capabilities = new List<ulong>(new ulong[]{
                80,81,82,10,11,12,13,15,16,20,21,30,31,40,41,42,43,50,51,52,53,54,55,56,57,58,59,60,61,70,71,72,73
            });
            res.apiversion = 1;
            res.capabilities = capabilities;

            // return this list
            SendJsonResponse(context, res);
        }

        #endregion

        #region // Concept Descriptions

        public void EvalPutCd(IHttpContext context, string aasid)
        {
            // first check
            if (context.Request.Payload == null || context.Request.ContentType != ContentType.JSON)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"No payload or content type is not JSON.");
                return;
            }

            // access the AAS
            var aas = this.FindAAS(aasid, context.Request.QueryString, context.Request.RawUrl);
            if (aas == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound, $"No AAS with idShort '{aasid}' found.");
                return;
            }

            // de-serialize CD
            AdminShell.ConceptDescription cd = null;
            try
            {
                cd = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.ConceptDescription>(
                    context.Request.Payload);
            }
            catch (Exception ex)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest, $"Cannot deserialize payload: {ex.Message}.");
                return;
            }

            // need id for idempotent behaviour
            if (cd.identification == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.BadRequest,
                    $"Identification of entity is (null); PUT cannot be performed.");
                return;
            }

            // datastructure update
            if (this.Package == null || this.Package.AasEnv == null || this.Package.AasEnv.Assets == null)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.InternalServerError, $"Error accessing internal data structures.");
                return;
            }

            // add Submodel
            context.Server.Logger.Debug(
                $"Adding ConceptDescription with idShort {cd.idShort ?? "--"} and " +
                $"id {cd.identification.ToString()}");
            var existingCd = this.Package.AasEnv.FindConceptDescription(cd.identification);
            if (existingCd != null)
                this.Package.AasEnv.ConceptDescriptions.Remove(existingCd);
            this.Package.AasEnv.ConceptDescriptions.Add(cd);

            // simple OK
            SendTextResponse(context, "OK" + ((existingCd != null) ? " (updated)" : " (new)"));
        }

        #endregion
    }
}
