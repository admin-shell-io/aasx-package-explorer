using AasxIntegrationBase;
using AdminShellNS;
using Grapevine.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace AasxRestServerLibrary
{
    public class AasxRestClient : IAasxOnlineConnection
    {
        // Instance management

        private Uri uri = null;
        private RestClient client = null;
        private WebProxy proxy = null;

        public AasxRestClient(string hostpart)
        {
            this.uri = new Uri(hostpart.TrimEnd('/'));
            this.client = new RestClient();
            this.client.Host = this.uri.Host;
            this.client.Port = this.uri.Port;
            if (File.Exists("C:\\dat\\proxy.dat"))
            {
                string proxyAddress = "";
                string username = "";
                string password = "";
                try
                {   // Open the text file using a stream reader.
                    using (StreamReader sr = new StreamReader("C:\\dat\\proxy.dat"))
                    {
                        proxyAddress = sr.ReadLine();
                        username = sr.ReadLine();
                        password = sr.ReadLine();
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("The file C:\\dat\\proxy.dat could not be read:");
                    Console.WriteLine(e.Message);
                }
                this.proxy = new WebProxy();
                Uri newUri = new Uri(proxyAddress);
                this.proxy.Address = newUri;
                this.proxy.Credentials = new NetworkCredential(username, password);
            }
            else
            {
                var pi = WebRequest.GetSystemWebProxy();
                // TODO: Andreas, check this. System.Net.WebProxy.GetDefaultProxy was deprecated
                this.proxy = (WebProxy)pi;
                if (this.proxy != null)
                    this.proxy.UseDefaultCredentials = true;
            }
        }

        // interface

        public bool IsValid() { return this.uri != null; } // assume validity
        public bool IsConnected() { return true; } // always, as there is no open connection by principle
        public string GetInfo() { return uri.ToString(); }

        public Stream GetThumbnailStream()
        {
            var request = new RestRequest("/aas/id/thumbnail");
            if (this.proxy != null)
                request.Proxy = this.proxy;
            var response = client.Execute(request);
            if (response.StatusCode != Grapevine.Shared.HttpStatusCode.Ok)
                throw new Exception($"REST {response.ResponseUri} response {response.StatusCode} with {response.StatusDescription}");

            // Note: the normal response.GetContent() internally reads ContentStream as a string and screws up binary data.
            // Necessary to access the real implementing object
            var rr = response as RestResponse;
            if (rr != null)
            {
                return rr.Advanced.GetResponseStream();
            }
            return null;
        }

        public string ReloadPropertyValue()
        {
            return "";
        }

        // utilities

        string BuildUriQueryPartId(string tag, AdminShell.Identifiable entity)
        {
            if (entity == null || entity.identification == null)
                return "";
            var res = "";
            if (tag != null)
                res += tag.Trim() + "=";
            res += entity.identification.idType.Trim() + "," + entity.identification.id.Trim();
            return res;
        }

        string BuildUriQueryString(params string[] parts)
        {
            if (parts == null)
                return "";
            var res = "?";
            foreach (var p in parts)
            {
                if (res.Length > 1)
                    res += "&";
                res += p;
            }
            return res;
        }

        // individual functions

        public AdminShellPackageEnv OpenPackageByAasEnv()
        {
            var request = new RestRequest("/aas/id/aasenv");
            if (this.proxy != null)
                request.Proxy = this.proxy;
            var respose = client.Execute(request);
            if (respose.StatusCode != Grapevine.Shared.HttpStatusCode.Ok)
                throw new Exception($"REST {respose.ResponseUri} response {respose.StatusCode} with {respose.StatusDescription}");
            var res = new AdminShellPackageEnv();
            res.LoadFromAasEnvString(respose.GetContent());
            return res;
        }

        public string GetSubmodel(string name)
        {
            string fullname = "/aas/id/submodels/" + name + "/complete";
            var request = new RestRequest(fullname);
            if (this.proxy != null)
                request.Proxy = this.proxy;
            var response = client.Execute(request);
            if (response.StatusCode != Grapevine.Shared.HttpStatusCode.Ok)
                throw new Exception($"REST {response.ResponseUri} response {response.StatusCode} with {response.StatusDescription}");
            return response.GetContent();
        }

        public string PutSubmodel(string payload)
        {
            string fullname = "/aas/id/submodels/";
            var request = new RestRequest(fullname);
            request.HttpMethod = Grapevine.Shared.HttpMethod.PUT;
            request.ContentType = Grapevine.Shared.ContentType.JSON;
            request.Payload = payload;
            if (this.proxy != null)
                request.Proxy = this.proxy;
            var response = client.Execute(request);
            if (response.StatusCode != Grapevine.Shared.HttpStatusCode.Ok)
                throw new Exception($"REST {response.ResponseUri} response {response.StatusCode} with {response.StatusDescription}");
            return response.GetContent();
        }

        public string UpdatePropertyValue(AdminShell.AdministrationShellEnv env, AdminShell.Submodel submodel, AdminShell.SubmodelElement sme)
        {
            // trivial fails
            if (env == null || sme == null)
                return null;

            // need AAS, indirect
            var aas = env.FindAASwithSubmodel(submodel.identification);
            if (aas == null)
                return null;

            // build path         
            // var query = BuildUriQueryString(BuildUriQueryPartId("Q1", aas), BuildUriQueryPartId("Q2", submodel));
            // var reqpath = "/aas/@Q1/submodels/@Q2/elements/" + sme.CollectIdShortByParent() + "/property";
            var aasId = aas.idShort;
            var submodelId = submodel.idShort;
            var elementId = sme.CollectIdShortByParent();
            var reqpath = "./aas/" + aasId + "/submodels/" + submodelId + "/elements/" + elementId + "/property";

            // request
            // var uribuilder = new UriBuilder(reqpath);
            // uribuilder.Query = query;
            // var request = new RestRequest(uribuilder.ToString());
            var request = new RestRequest(reqpath);
            if (this.proxy != null)
                request.Proxy = this.proxy;
            var respose = client.Execute(request);
            if (respose.StatusCode != Grapevine.Shared.HttpStatusCode.Ok)
                throw new Exception($"REST {respose.ResponseUri} response {respose.StatusCode} with {respose.StatusDescription}");

            // return respose.GetContent();
            var json = respose.GetContent();
            var parsed = JObject.Parse(json);
            var value = parsed.SelectToken("value").Value<string>();
            return value;
        }
    }
}
