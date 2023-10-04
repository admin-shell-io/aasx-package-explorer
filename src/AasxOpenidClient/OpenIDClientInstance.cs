using AnyUi;
using IdentityModel;
using IdentityModel.Client;
using Jose;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using SSIExtension;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

/*
Copyright (c) 2020 see https://github.com/IdentityServer/IdentityServer4

We adapted the code marginally and removed the parts that we do not use.
*/

// ReSharper disable All .. as this is code from others (adapted from IdentityServer4).

namespace AasxOpenIdClient
{
    /// <summary>
    /// This class is the new version of the OpenIdClient, which is meant to hold the
    /// credentials not globally, but for a specific context, such as the main application
    /// or a specific repository.
    /// </summary>
    public class OpenIdClientInstance
    {
        static public bool AcceptAllCertifications(
            object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public class UiLambdaSet
        {
            public delegate AnyUiMessageBoxResult ShowMessageDelegate(
                                    string content, string text, string caption, AnyUiMessageBoxButton buttons = 0);
            public ShowMessageDelegate MesssageBox;

            public static AnyUiMessageBoxResult MesssageBoxShow(
                UiLambdaSet lambdaSet,
                string content, string text, string caption, AnyUiMessageBoxButton buttons = 0)
            {
                if (lambdaSet?.MesssageBox != null)
                    return lambdaSet.MesssageBox(content, text, caption, buttons);
                return AnyUiMessageBoxResult.Cancel;
            }
        }

        public string authServer = "https://localhost:50001";
        public string dataServer = "http://localhost:51310";
        public string certPfx = "Andreas_Orzelski_Chain.pfx";
        public string certPfxPW = "i40";
        public string outputDir = ".";

        public string token = "";
        public string ssiURL = "";
        public string keycloak = "";
        public string email = "";
        public async Task Run(string tag, string value, UiLambdaSet uiLambda = null)
        {
            ServicePointManager.ServerCertificateValidationCallback =
                new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);

            // Initializes the variables to pass to the MessageBox.Show method.
            string caption = "Connect with " + tag + ".dat";
            string message = "";

            bool withOpenidFile = false;
            if (value != "")
            {
                dataServer = value;
                authServer = "";
                certPfx = "";
                certPfxPW = "";
                value = "";
            }
            else
            {
                // read openx.dat
                try
                {
                    using (StreamReader sr = new StreamReader(tag + ".dat"))
                    {
                        authServer = sr.ReadLine();
                        dataServer = sr.ReadLine();
                        certPfx = sr.ReadLine();
                        certPfxPW = sr.ReadLine();
                        outputDir = sr.ReadLine();
                    }
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.Error(ex, $"The file {tag}.dat can not be read.");
                    return;
                }
                withOpenidFile = true;
            }

            message =
                "authServer: " + authServer + "\n" +
                "dataServer: " + dataServer + "\n" +
                "certPfx: " + certPfx + "\n" +
                "certPfxPW: " + certPfxPW + "\n" +
                "outputDir: " + outputDir + "\n" +
                "\nConinue?";

            // Displays the MessageBox.
            var result = UiLambdaSet.MesssageBoxShow(uiLambda, message, "", caption, AnyUiMessageBoxButton.YesNo);
            if (result != AnyUiMessageBoxResult.Yes)
            {
                // Closes the parent form.
                return;
            }

            UiLambdaSet.MesssageBoxShow(uiLambda, "", "Access Aasx Server at " + dataServer,
                "Data Server", AnyUiMessageBoxButton.OK);

            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            handler.AllowAutoRedirect = false;
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(dataServer)
            };
            if (token != "")
                client.SetBearerToken(token);

            string operation = "";
            string lastOperation = "";
            if (withOpenidFile)
            {
                operation = "authenticate";
                lastOperation = "/server/listaas/";
            }
            else
            {
                operation = "/server/listaas/";
            }

            while (operation != "" && operation != "error")
            {
                UiLambdaSet.MesssageBoxShow(uiLambda, "", "operation: " + operation + value + "\ntoken: " + token,
                    "Operation", AnyUiMessageBoxButton.OK);

                switch (operation)
                {
                    case "/server/listaas/":
                    case "/server/getaasx2/":
                        try
                        {
                            HttpResponseMessage response2 = null;
                            switch (operation)
                            {
                                case "/server/listaas/":
                                    response2 = await client.GetAsync(operation);
                                    break;
                                case "/server/getaasx2/":
                                    response2 = await client.GetAsync(operation + value);
                                    break;
                            }

                            if (response2.StatusCode == System.Net.HttpStatusCode.TemporaryRedirect)
                            {
                                string redirectUrl = response2.Headers.Location.ToString();
                                string[] splitResult = redirectUrl.Split(new string[] { "?" },
                                    StringSplitOptions.RemoveEmptyEntries);
                                Console.WriteLine("Redirect to:" + splitResult[0]);
                                authServer = splitResult[0];
                                UiLambdaSet.MesssageBoxShow(
                                    uiLambda, authServer, "", "Redirect to", AnyUiMessageBoxButton.OK);
                                lastOperation = operation;
                                operation = "authenticate";
                                continue;
                            }
                            if (!response2.IsSuccessStatusCode)
                            {
                                lastOperation = operation;
                                operation = "error";
                                continue;
                            }
                            String urlContents = await response2.Content.ReadAsStringAsync();
                            switch (operation)
                            {
                                case "/server/listaas/":
                                    UiLambdaSet.MesssageBoxShow(uiLambda,
                                        "", "SelectFromListFlyoutItem missing", "SelectFromListFlyoutItem missing",
                                        AnyUiMessageBoxButton.OK);
                                    return;
                                case "/server/getaasx2/":
                                    try
                                    {
                                        var parsed3 = JObject.Parse(urlContents);

                                        string fileName = parsed3.SelectToken("fileName").Value<string>();
                                        string fileData = parsed3.SelectToken("fileData").Value<string>();

                                        var enc = new System.Text.ASCIIEncoding();
                                        var fileString4 = Jose.JWT.Decode(fileData, enc.GetBytes(secretString),
                                            JwsAlgorithm.HS256);
                                        var parsed4 = JObject.Parse(fileString4);

                                        string binaryBase64_4 = parsed4.SelectToken("file").Value<string>();
                                        Byte[] fileBytes4 = Convert.FromBase64String(binaryBase64_4);

                                        Console.WriteLine("Writing file: " + outputDir + "\\" + "download.aasx");
                                        File.WriteAllBytes(outputDir + "\\" + "download.aasx", fileBytes4);
                                    }
                                    catch (Exception ex)
                                    {
                                        AdminShellNS.LogInternally.That.Error(ex, $"Failed at operation: {operation}");
                                        lastOperation = operation;
                                        operation = "error";
                                    }
                                    operation = "";
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            AdminShellNS.LogInternally.That.Error(ex, $"Failed at operation: {operation}");
                            lastOperation = operation;
                            operation = "error";
                        }
                        break;
                    case "authenticate":
                        try
                        {
                            X509SigningCredentials x509Credential = null;
                            if (withOpenidFile)
                            {
                                x509Credential = new X509SigningCredentials(new X509Certificate2(certPfx, certPfxPW));
                            }

                            var response = await RequestTokenAsync(x509Credential, uiLambda);
                            token = response.AccessToken;
                            client.SetBearerToken(token);

                            response.Show();
                            UiLambdaSet.MesssageBoxShow(uiLambda, response.AccessToken, "",
                                "Access Token", AnyUiMessageBoxButton.OK);

                            operation = lastOperation;
                            lastOperation = "";
                        }
                        catch (Exception ex)
                        {
                            AdminShellNS.LogInternally.That.Error(ex, $"Failed at operation: {operation}");
                            lastOperation = operation;
                            operation = "error";
                        }
                        break;
                    case "error":
                        UiLambdaSet.MesssageBoxShow(uiLambda, "", $"Can not perform: {lastOperation}",
                            "Error", AnyUiMessageBoxButton.OK);
                        break;
                }
            }
        }

        public async Task<TokenResponse> RequestTokenAsync(
            SigningCredentials credential,
            UiLambdaSet uiLambda = null)
        {
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            var client = new HttpClient(handler);

            if (keycloak != "")
            {
                TokenResponse tr = null;
                bool error = false;

                string BASE_URL = keycloak;
                string client_id = "devicegrant"; // create a client in your keycloak instance
                string DEVICE_GRANT_ENDPOINT = BASE_URL + "/auth/realms/test/protocol/openid-connect/auth/device";
                string TOKEN_ENDPOINT = BASE_URL + "/auth/realms/test/protocol/openid-connect/token";
                string USERINFO_ENDPOINT = BASE_URL + "/auth/realms/test/protocol/openid-connect/userinfo";
                string verification_uri_complete = "";
                string device_code = "";

                HttpContent httpContent = new StringContent("client_id=" + client_id);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                var r = await client.PostAsync(DEVICE_GRANT_ENDPOINT, httpContent);
                error = !r.IsSuccessStatusCode;

                if (!error)
                {
                    var result = r.Content.ReadAsStringAsync().Result;

                    try
                    {
                        var parsed = JObject.Parse(result);

                        verification_uri_complete = parsed.SelectToken("verification_uri_complete").Value<string>();
                        device_code = parsed.SelectToken("device_code").Value<string>();

                        Process.Start(verification_uri_complete);
                    }
                    catch (Exception)
                    {

                        error = true;
                    }
                }
                if (!error)
                {
                    string parameter = "grant_type=urn:ietf:params:oauth:grant-type:device_code&" +
                        "client_id=" + client_id + "&" +
                        "device_code=" + device_code;
                    string text = "Please login at " + verification_uri_complete + "\n" + parameter;

                    UiLambdaSet.MesssageBoxShow(uiLambda, "Press OK to continue", text,
                        "Keycloak Authentication", AnyUiMessageBoxButton.OK);

                    httpContent = new StringContent(parameter, Encoding.UTF8);
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                    r = await client.PostAsync(TOKEN_ENDPOINT, httpContent);
                    if (r.IsSuccessStatusCode)
                    {
                        var result = r.Content.ReadAsStringAsync().Result;

                        try
                        {
                            var parsed = JObject.Parse(result);

                            token = parsed.SelectToken("access_token").Value<string>();
                            UiLambdaSet.MesssageBoxShow(uiLambda, "Press OK to continue", token,
                                "Access Token", AnyUiMessageBoxButton.OK);
                        }
                        catch (Exception)
                        {
                            error = true;
                        }
                    }
                }
                if (error)
                    keycloak = "";
                return tr;
            }

            var disco = client.GetDiscoveryDocumentAsync(authServer).Result;
            if (disco.IsError) throw new Exception(disco.Error);

            UiLambdaSet.MesssageBoxShow(uiLambda, disco.Raw, "", "Discovery JSON", AnyUiMessageBoxButton.OK);

            List<string> rootCertSubject = new List<string>();
            var discoObject = JsonSerializer.Deserialize<JsonObject>(disco.Raw);
            var rootCertSubjects = JsonSerializer.Deserialize<List<string>>(discoObject["rootCertSubjects"]);
            if (rootCertSubjects != null)
            {
                foreach (var subject in rootCertSubjects)
                {
                    rootCertSubject.Add(subject);
                }
            }

            var clientToken = CreateClientToken(credential, "client.jwt",
                    disco.TokenEndpoint, rootCertSubject, uiLambda);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nClientToken with x5c in header: \n");
            Console.ResetColor();
            Console.WriteLine(clientToken + "\n");

            if (ssiURL == "")
                UiLambdaSet.MesssageBoxShow(uiLambda, clientToken, "", "Client Token", AnyUiMessageBoxButton.OK);

            var response = client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                Scope = "resource1.scope1",

                ClientAssertion =
                {
                    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                    Value = clientToken
                }
            }).Result;

            if (response.IsError)
            {
                throw new Exception(response.Error);
            }

            UiLambdaSet.MesssageBoxShow(uiLambda, response.AccessToken, "",
                "Access Token", AnyUiMessageBoxButton.OK);

            return response;
        }

        public string secretString = "Industrie4.0-Asset-Administration-Shell";
        async Task CallServiceAsync(string token, string value)
        {
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(dataServer)
            };

            client.SetBearerToken(token);

            try
            {
                var response = await client.GetStringAsync("/server/getaasx2/" + value);

                var parsed3 = JObject.Parse(response);

                string fileName = parsed3.SelectToken("fileName").Value<string>();
                string fileData = parsed3.SelectToken("fileData").Value<string>();

                var enc = new System.Text.ASCIIEncoding();
                var fileString4 = Jose.JWT.Decode(fileData, enc.GetBytes(secretString), JwsAlgorithm.HS256);
                var parsed4 = JObject.Parse(fileString4);

                string binaryBase64_4 = parsed4.SelectToken("file").Value<string>();
                Byte[] fileBytes4 = Convert.FromBase64String(binaryBase64_4);

                Console.WriteLine("Writing file: " + outputDir + "\\" + "download.aasx");
                File.WriteAllBytes(outputDir + "\\" + "download.aasx", fileBytes4);
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.Error(ex, $"Can not get .AASX.");
                return;
            }
        }

        private string CreateClientToken(SigningCredentials credential, string clientId, string audience,
            List<string> rootCertSubject,
            UiLambdaSet uiLambda = null)
        {
            string[] x5c = null;
            string certFileName = certPfx;
            string password = certPfxPW;
            X509Certificate2 certificate = null;

            if (credential == null)
            {
                AnyUiMessageBoxResult res = AnyUiMessageBoxResult.No;

                if (ssiURL == "")
                    res = UiLambdaSet.MesssageBoxShow(uiLambda, "",
                        "Select certificate chain from certificate store? \n" +
                        "(otherwise use file Andreas_Orzelski_Chain.pfx)",
                        "Select certificate chain", AnyUiMessageBoxButton.YesNo);

                if (res == AnyUiMessageBoxResult.No)
                    credential = new X509SigningCredentials(new X509Certificate2(certPfx, certPfxPW));
            }

            if (credential == null)
            {
                X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(
                    X509FindType.FindByTimeValid, DateTime.Now, false);

                Boolean rootCertFound = false;
                X509Certificate2Collection fcollection2 = new X509Certificate2Collection();
                foreach (X509Certificate2 fc in fcollection)
                {
                    X509Chain fch = new X509Chain();
                    fch.Build(fc);
                    foreach (X509ChainElement element in fch.ChainElements)
                    {
                        if (rootCertSubject.Contains(element.Certificate.Subject))
                        {
                            rootCertFound = true;
                            fcollection2.Add(fc);
                        }
                    }
                }
                if (rootCertFound)
                    fcollection = fcollection2;

                X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(fcollection,
                    "Test Certificate Select",
                    "Select a certificate from the following list to get information on that certificate",
                    X509SelectionFlag.SingleSelection);
                if (scollection.Count != 0)
                {
                    certificate = scollection[0];
                    X509Chain ch = new X509Chain();
                    ch.Build(certificate);

                    string[] X509Base64 = new string[ch.ChainElements.Count];

                    int j = 0;
                    foreach (X509ChainElement element in ch.ChainElements)
                    {
                        X509Base64[j++] = Convert.ToBase64String(element.Certificate.GetRawCertData());
                    }

                    x5c = X509Base64;
                }
            }
            else
            {
                // use old fixed certificate chain
                X509Certificate2Collection xc = new X509Certificate2Collection();
                xc.Import(certFileName, password, X509KeyStorageFlags.PersistKeySet);

                string[] X509Base64 = new string[xc.Count];

                int j = xc.Count;
                var xce = xc.GetEnumerator();
                for (int i = 0; i < xc.Count; i++)
                {
                    xce.MoveNext();
                    X509Base64[--j] = Convert.ToBase64String(xce.Current.GetRawCertData());
                }
                x5c = X509Base64;

                certificate = new X509Certificate2(certFileName, password);
            }

            string email = "";
            string subject = certificate.Subject;
            var split = subject.Split(new Char[] { ',' });
            if (split[0] != "")
            {
                var split2 = split[0].Split(new Char[] { '=' });
                if (split2[0] == "E")
                {
                    email = split2[1];
                }
            }
            Console.WriteLine("email: " + email);

            //
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(
                Convert.ToBase64String(certificate.RawData, Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE-----");

            if (ssiURL == "")
                UiLambdaSet.MesssageBoxShow(uiLambda, builder.ToString(), "", "Client Certificate",
                    AnyUiMessageBoxButton.OK);

            credential = new X509SigningCredentials(certificate);
            // oz end

            var now = DateTime.UtcNow;

            var token = new JwtSecurityToken(
                    clientId,
                    audience,
                    new List<Claim>()
                    {
                        new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                        new Claim(JwtClaimTypes.Subject, clientId),
                        new Claim(JwtClaimTypes.IssuedAt, now.ToEpochTime().ToString(), ClaimValueTypes.Integer64),
                        // OZ
                        new Claim(JwtClaimTypes.Email, email)
                        // new Claim("x5c", x5c)
                    },
                    now,
                    now.AddMinutes(1),
                    credential)
            ;

            token.Header.Add("x5c", x5c);
            if (ssiURL != "")
            {
                //// Prover prover = new Prover("http://192.168.178.33:5001"); //AASX Package Explorer
                Prover prover = new Prover(ssiURL); //AASX Package Explorer

                string invitation = prover.CreateInvitation();

                token.Header.Add("ssiInvitation", invitation);

                UiLambdaSet.MesssageBoxShow(uiLambda, "ssiURL = " + ssiURL, "", "SSI Info",
                    AnyUiMessageBoxButton.OK);
                UiLambdaSet.MesssageBoxShow(uiLambda,
                    "credentialInfo = " + prover.cred_json_asstring, "", "VC for Presentation",
                    AnyUiMessageBoxButton.OK);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }

        private void Prover_CredentialPresented(object sender, string e)
        {
            throw new NotImplementedException();
        }
    }
}
