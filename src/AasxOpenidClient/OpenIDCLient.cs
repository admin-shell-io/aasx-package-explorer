using System;
using Microsoft.IdentityModel.Tokens;
using IdentityModel;
using IdentityModel.Client;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Clients;
using System.IO;
using System.Windows;
using Jose;
// using AasxPackageExplorer;
using System.Net;
using System.Windows.Forms;
using System.Text;

namespace AasxPrivateKeyJwtClient
{
    public class OpenIDClient
    {
        static public bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        // private static string rsaKey = "{'d':'GmiaucNIzdvsEzGjZjd43SDToy1pz-Ph-shsOUXXh-dsYNGftITGerp8bO1iryXh_zUEo8oDK3r1y4klTonQ6bLsWw4ogjLPmL3yiqsoSjJa1G2Ymh_RY_sFZLLXAcrmpbzdWIAkgkHSZTaliL6g57vA7gxvd8L4s82wgGer_JmURI0ECbaCg98JVS0Srtf9GeTRHoX4foLWKc1Vq6NHthzqRMLZe-aRBNU9IMvXNd7kCcIbHCM3GTD_8cFj135nBPP2HOgC_ZXI1txsEf-djqJj8W5vaM7ViKU28IDv1gZGH3CatoysYx6jv1XJVvb2PH8RbFKbJmeyUm3Wvo-rgQ','dp':'YNjVBTCIwZD65WCht5ve06vnBLP_Po1NtL_4lkholmPzJ5jbLYBU8f5foNp8DVJBdFQW7wcLmx85-NC5Pl1ZeyA-Ecbw4fDraa5Z4wUKlF0LT6VV79rfOF19y8kwf6MigyrDqMLcH_CRnRGg5NfDsijlZXffINGuxg6wWzhiqqE','dq':'LfMDQbvTFNngkZjKkN2CBh5_MBG6Yrmfy4kWA8IC2HQqID5FtreiY2MTAwoDcoINfh3S5CItpuq94tlB2t-VUv8wunhbngHiB5xUprwGAAnwJ3DL39D2m43i_3YP-UO1TgZQUAOh7Jrd4foatpatTvBtY3F1DrCrUKE5Kkn770M','e':'AQAB','kid':'ZzAjSnraU3bkWGnnAqLapYGpTyNfLbjbzgAPbbW2GEA','kty':'RSA','n':'wWwQFtSzeRjjerpEM5Rmqz_DsNaZ9S1Bw6UbZkDLowuuTCjBWUax0vBMMxdy6XjEEK4Oq9lKMvx9JzjmeJf1knoqSNrox3Ka0rnxXpNAz6sATvme8p9mTXyp0cX4lF4U2J54xa2_S9NF5QWvpXvBeC4GAJx7QaSw4zrUkrc6XyaAiFnLhQEwKJCwUw4NOqIuYvYp_IXhw-5Ti_icDlZS-282PcccnBeOcX7vc21pozibIdmZJKqXNsL1Ibx5Nkx1F1jLnekJAmdaACDjYRLL_6n3W4wUp19UvzB1lGtXcJKLLkqB6YDiZNu16OSiSprfmrRXvYmvD8m6Fnl5aetgKw','p':'7enorp9Pm9XSHaCvQyENcvdU99WCPbnp8vc0KnY_0g9UdX4ZDH07JwKu6DQEwfmUA1qspC-e_KFWTl3x0-I2eJRnHjLOoLrTjrVSBRhBMGEH5PvtZTTThnIY2LReH-6EhceGvcsJ_MhNDUEZLykiH1OnKhmRuvSdhi8oiETqtPE','q':'0CBLGi_kRPLqI8yfVkpBbA9zkCAshgrWWn9hsq6a7Zl2LcLaLBRUxH0q1jWnXgeJh9o5v8sYGXwhbrmuypw7kJ0uA3OgEzSsNvX5Ay3R9sNel-3Mqm8Me5OfWWvmTEBOci8RwHstdR-7b9ZT13jk-dsZI7OlV_uBja1ny9Nz9ts','qi':'pG6J4dcUDrDndMxa-ee1yG4KjZqqyCQcmPAfqklI2LmnpRIjcK78scclvpboI3JQyg6RCEKVMwAhVtQM6cBcIO3JrHgqeYDblp5wXHjto70HVW6Z8kBruNx1AH9E8LzNvSRL-JVTFzBkJuNgzKQfD0G77tQRgJ-Ri7qu3_9o1M4'}";

        public static string authServer = "https://localhost:50001";
        public static string dataServer = "http://localhost:51310";
        public static string certPfx = "Andreas_Orzelski_Chain.pfx";
        public static string certPfxPW = "i40";
        public static string outputDir = ".";

        public static async Task Run(string tag, string value)
        {
            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);

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

            /*
            string message = authServer + "\n" + dataServer;
            var uc = new MessageBoxFlyout(message, "openid",
                MessageBoxButton.OkCancel, MessageBoxImage.Information);
            var res = uc.Show();
            if (res != MessageBoxResult.OK)
            {
            }
            */
            // Console.Title = "Console Client Credentials Flow with JWT Assertion";

            // X.509 cert
            // var certificate = new X509Certificate2("client.p12", "changeit");
            var certificate = new X509Certificate2(certPfx, certPfxPW);
            // var x509Credential = new X509SigningCredentials(certificate);
            X509SigningCredentials x509Credential = null;

            var response = await RequestTokenAsync(x509Credential);
            response.Show();

            System.Windows.Forms.MessageBox.Show(response.AccessToken, "Access Token", MessageBoxButtons.OK);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nPress ENTER to access Aasx Server at " + dataServer + "\n");
            Console.ResetColor();
            Console.ReadLine();
            await CallServiceAsync(response.AccessToken, value);

            /*
            // RSA JsonWebkey
            var jwk = new JsonWebKey(rsaKey);
            response = await RequestTokenAsync(new SigningCredentials(jwk, "RS256"));
            response.Show();
            
            Console.ReadLine();
            await CallServiceAsync(response.AccessToken);
            */
        }

        static async Task<TokenResponse> RequestTokenAsync(SigningCredentials credential)
        {
            var client = new HttpClient();

            // var disco = await client.GetDiscoveryDocumentAsync(Constants.Authority);
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

        // static string baseAddress = "http://localhost:51310";

        public static string secretString = "Industrie4.0-Asset-Administration-Shell";
        static async Task CallServiceAsync(string token, string value)
        {
            // var baseAddress = Constants.SampleApi;

            var client = new HttpClient
            {
                // BaseAddress = new Uri(baseAddress)
                BaseAddress = new Uri(dataServer)
            };

            client.SetBearerToken(token);
            // var response = await client.GetStringAsync("identity");
            var response = await client.GetStringAsync("/server/getaasx2/" + value);

            var parsed3 = JObject.Parse(response);

            string fileName = parsed3.SelectToken("fileName").Value<string>();
            string fileData = parsed3.SelectToken("fileData").Value<string>();

            var enc = new System.Text.ASCIIEncoding();
            var fileString4 = Jose.JWT.Decode(fileData, enc.GetBytes(secretString), JwsAlgorithm.HS256);
            var parsed4 = JObject.Parse(fileString4);

            string binaryBase64_4 = parsed4.SelectToken("file").Value<string>();
            Byte[] fileBytes4 = Convert.FromBase64String(binaryBase64_4);

            // Console.WriteLine("Writing file: " + "\\output" + "\\" + fileName);
            // File.WriteAllBytes("\\output" + "/" + fileName, fileBytes4);
            Console.WriteLine("Writing file: " + outputDir + "\\" + "download.aasx");
            File.WriteAllBytes(outputDir + "\\" + "download.aasx", fileBytes4);

            "\n\nService claims:".ConsoleGreen();
            // Console.WriteLine(JArray.Parse(response));
            // Console.WriteLine(response);
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

            // Byte[] certFileBytes = Convert.FromBase64String(X509Base64[0]);
            // credential = new X509SigningCredentials(new X509Certificate2(certFileBytes));

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
