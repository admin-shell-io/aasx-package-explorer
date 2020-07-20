using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using AasxOpenIdClient;
using IdentityModel;
using IdentityModel.Client;
using Jose;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public static async Task Run(string tag, string value)
        {
            ServicePointManager.ServerCertificateValidationCallback =
                new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);

            // Initializes the variables to pass to the MessageBox.Show method.
            string caption = "Connect with " + tag + ".dat";
            string message = "";

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

                    message =
                        "authServer: " + authServer + "\n" +
                        "dataServer: " + dataServer + "\n" +
                        "certPfx: " + certPfx + "\n" +
                        "certPfxPW: " + certPfxPW + "\n" +
                        "outputDir: " + outputDir + "\n" +
                        "\nConinue?";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(tag + ".dat " + " can not be read!");
                return;
            }

            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result;

            // Displays the MessageBox.
            result = System.Windows.Forms.MessageBox.Show(message, caption, buttons);
            if (result != System.Windows.Forms.DialogResult.Yes)
            {
                // Closes the parent form.
                return;
            }

            // X.509 cert
            var certificate = new X509Certificate2(certPfx, certPfxPW);
            X509SigningCredentials x509Credential = null;

            var response = await RequestTokenAsync(x509Credential);
            response.Show();

            System.Windows.Forms.MessageBox.Show(response.AccessToken, "Access Token", MessageBoxButtons.OK);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nPress ENTER to access Aasx Server at " + dataServer + "\n");
            Console.ResetColor();
            Console.ReadLine();
            await CallServiceAsync(response.AccessToken, value);
        }

        static async Task<TokenResponse> RequestTokenAsync(SigningCredentials credential)
        {
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            var client = new HttpClient(handler);

            var disco = await client.GetDiscoveryDocumentAsync(authServer);
            if (disco.IsError) throw new Exception(disco.Error);

            System.Windows.Forms.MessageBox.Show(disco.Raw, "Discovery JSON", MessageBoxButtons.OK);

            var clientToken = CreateClientToken(credential, "client.jwt", disco.TokenEndpoint);
            // oz
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nClientToken with x5c in header: \n");
            Console.ResetColor();
            Console.WriteLine(clientToken + "\n");

            System.Windows.Forms.MessageBox.Show(clientToken, "Client Token", MessageBoxButtons.OK);

            var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                // Scope = "feature1",
                Scope = "scope1",

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

            "\n\nService claims:".ConsoleGreen();
        }

        private static string CreateClientToken(SigningCredentials credential, string clientId, string audience)
        {
            // oz
            string x5c = "";
            string certFileName = certPfx;
            string password = certPfxPW;

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

            x5c = JsonConvert.SerializeObject(X509Base64);

            string email = "";
            X509Certificate2 x509 = new X509Certificate2(certFileName, password);
            string subject = x509.Subject;
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
                Convert.ToBase64String(x509.RawData, Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE-----");

            System.Windows.Forms.MessageBox.Show(builder.ToString(), "Client Certificate", MessageBoxButtons.OK);
            //

            credential = new X509SigningCredentials(x509);
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
