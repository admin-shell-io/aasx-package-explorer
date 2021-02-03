using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Windows;
using System.Windows.Forms;
using AasxOpenIdClient;
using IdentityModel;
using IdentityModel.Client;
using Jose;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
Copyright (c) 2020 see https://github.com/IdentityServer/IdentityServer4

We adapted the code marginally and removed the parts that we do not use.
*/

// ReSharper disable All .. as this is code from others (adapted from IdentityServer4).

namespace AasxOpenIdClient
{
    public class OpenIDClient
    {
        static public bool AcceptAllCertifications(
            object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public static string authServer = "https://localhost:50001";
        public static string dataServer = "http://localhost:51310";
        public static string certPfx = "Andreas_Orzelski_Chain.pfx";
        public static string certPfxPW = "i40";
        public static string outputDir = ".";

        public static string token = "";
        public static async Task Run(string tag, string value/*, AasxIntegrationBase.IFlyoutProvider flyoutProvider*/)
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

            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result;

            // Displays the MessageBox.
            result = System.Windows.Forms.MessageBox.Show(message, caption, buttons);
            if (result != System.Windows.Forms.DialogResult.Yes)
            {
                // Closes the parent form.
                return;
            }

            System.Windows.Forms.MessageBox.Show("Access Aasx Server at " + dataServer,
                "Data Server", MessageBoxButtons.OK);

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
                System.Windows.Forms.MessageBox.Show("operation: " + operation + value + "\ntoken: " + token,
                    "Operation", MessageBoxButtons.OK);

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
                                System.Windows.Forms.MessageBox.Show(authServer, "Redirect to", MessageBoxButtons.OK);
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
                                    System.Windows.Forms.MessageBox.Show(
                                    "SelectFromListFlyoutItem missing", "SelectFromListFlyoutItem missing",
                                    MessageBoxButtons.OK);
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

                            var response = await RequestTokenAsync(x509Credential);
                            token = response.AccessToken;
                            client.SetBearerToken(token);

                            response.Show();
                            System.Windows.Forms.MessageBox.Show(response.AccessToken,
                                "Access Token", MessageBoxButtons.OK);

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
                        System.Windows.Forms.MessageBox.Show($"Can not perform: {lastOperation}",
                            "Error", MessageBoxButtons.OK);
                        break;
                }
            }
        }

        public static async Task<TokenResponse> RequestTokenAsync(SigningCredentials credential)
        {
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            var client = new HttpClient(handler);

            var disco = await client.GetDiscoveryDocumentAsync(authServer);
            if (disco.IsError) throw new Exception(disco.Error);

            System.Windows.Forms.MessageBox.Show(disco.Raw, "Discovery JSON", MessageBoxButtons.OK);

            List<string> rootCertSubject = new List<string>();
            dynamic discoObject = Json.Decode(disco.Raw);
            if (discoObject.rootCertSubjects != null)
            {
                int i = 0;
                while (i < discoObject.rootCertSubjects.Length)
                {
                    rootCertSubject.Add(discoObject.rootCertSubjects[i++]);
                }
            }

            var clientToken = CreateClientToken(credential, "client.jwt", disco.TokenEndpoint, rootCertSubject);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nClientToken with x5c in header: \n");
            Console.ResetColor();
            Console.WriteLine(clientToken + "\n");

            System.Windows.Forms.MessageBox.Show(clientToken, "Client Token", MessageBoxButtons.OK);

            var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                // Scope = "feature1",
                // Scope = "scope1",
                Scope = "resource1.scope1",

                ClientAssertion =
                {
                    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                    Value = clientToken
                }
            });

            if (response.IsError)
            {
                throw new Exception(response.Error);
            }

            System.Windows.Forms.MessageBox.Show(response.AccessToken,
                "Access Token", System.Windows.Forms.MessageBoxButtons.OK);

            return response;
        }

        public static string secretString = "Industrie4.0-Asset-Administration-Shell";
        static async Task CallServiceAsync(string token, string value)
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

        private static string CreateClientToken(SigningCredentials credential, string clientId, string audience,
            List<string> rootCertSubject)
        {
            // oz
            //// string x5c = "";
            string[] x5c = null;
            string certFileName = certPfx;
            string password = certPfxPW;
            X509Certificate2 certificate = null;

            if (credential == null)
            {
                var res = System.Windows.Forms.MessageBox.Show(
                    "Select certificate chain from certificate store? \n" +
                    "(otherwise use file Andreas_Orzelski_Chain.pfx)",
                    "Select certificate chain", MessageBoxButtons.YesNo);
                if (res == DialogResult.No)
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

            System.Windows.Forms.MessageBox.Show(builder.ToString(), "Client Certificate", MessageBoxButtons.OK);
            //

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
            // oz

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }
    }
}
