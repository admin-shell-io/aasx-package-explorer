/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2021 Phoenix Contact GmbH & Co. KG <opensource@phoenixcontact.com>
Author: Andreas Orzelski

Copyright (c) 2021 Fraunhofer IOSB-INA Lemgo, 
    eine rechtlich nicht selbständige Einrichtung der Fraunhofer-Gesellschaft
    zur Förderung der angewandten Forschung e.V.

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// ReSharper disable All .. as this is test code

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace SSIExtension
{
    public class test
    {
        public static bool withAgents = false;
    }
    public class Prover
    {
        public string APIEndpoint { get; }
        public string cred_id { get; private set; }
        public string cred_json_asstring { get; private set; }

        string verifier_connection_id;

        public event EventHandler<string> CredentialPresented;

        public Prover(string APIEndpoint)
        {
            this.APIEndpoint = APIEndpoint;
        }

        public string CreateInvitation()
        {
            if (!test.withAgents)
                return "aorzelski@phoenixcontact.com";

            HttpResponseMessage result = new HttpClient().PostAsync(
                APIEndpoint + $"/connections/create-invitation?auto_accept=true", new StringContent("{}")).Result;
            var resultJson = result.Content.ReadAsStringAsync().Result;
            var invitationForVerifier = JsonDocument.Parse(resultJson).RootElement.GetProperty("invitation").ToString();
            verifier_connection_id = JsonDocument.Parse(resultJson).RootElement.GetProperty("connection_id").ToString();
            Console.WriteLine(
                $"Invitation for Verifier created. The Connection for Verifier has ID '{verifier_connection_id}'");

            Console.WriteLine("Waiting for the Connection to be accepted and VC to be requested by Verifier.");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            retryVCPresentationTimer = new Timer(CheckPresentVCCallback, stopwatch, 1000, 1000);

            var httpClient = new HttpClient();
            string allCredentialsFromAPI
                = httpClient.GetAsync(APIEndpoint + "/credentials").Result.Content.ReadAsStringAsync().Result;
            JsonElement cred_json = JsonDocument.Parse(allCredentialsFromAPI).RootElement
                .GetProperty("results").EnumerateArray().Where(
                r => r.GetProperty("cred_def_id").ToString() == Utils.CRED_DEF_ID).First();
            cred_json_asstring = cred_json.ToString();
            cred_id = cred_json.GetProperty("referent").GetString();

            return invitationForVerifier;
        }

        private Timer retryVCPresentationTimer;

        private void CheckPresentVCCallback(object stopwatchstate)
        {
            try
            {
                //holder finds out about presentation request and starts presenting
                Console.WriteLine("Try checking for VC Proof Request.");
                var httpClient = new HttpClient();
                string records = httpClient.GetAsync(APIEndpoint +
                    $"/present-proof-2.0/records?connection_id=" +
                    $"{verifier_connection_id}&role=prover&state=request-received")
                    .Result.Content.ReadAsStringAsync().Result;
                string pres_ex_id = JsonDocument.Parse(records).RootElement
                    .GetProperty("results").EnumerateArray().First().GetProperty("pres_ex_id").GetString();

                Console.WriteLine("VC Proof Request reveived. Try sending the VC Presentation.");


                Console.WriteLine($"Using VC with ID '{cred_id}' for VC Presentation");
                string proofPresentation = Utils.CreateProofPresentation(cred_id);
                JsonDocument.Parse(proofPresentation);

                HttpResponseMessage proofPresHttpResponse = httpClient.
                    PostAsync(APIEndpoint + $"/present-proof-2.0/records/{pres_ex_id}/send-presentation",
                    new StringContent(proofPresentation, Encoding.UTF8, "application/json")).Result;
                if (proofPresHttpResponse.IsSuccessStatusCode)
                {
                    CredentialPresented?.Invoke(this, cred_json_asstring);
                    Console.WriteLine("VC Presentation send!");
                    retryVCPresentationTimer.Dispose();
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("VC Presentation not yet send: " + e.Message);
            }

            Stopwatch stopwatch = (Stopwatch)stopwatchstate;

            if (stopwatch.ElapsedMilliseconds > 120000)
            {
                Console.WriteLine("No more waiting, time exceeded without a successful termination.");
                retryVCPresentationTimer.Dispose();
                return;
            }
            else
            {
                Console.WriteLine("Will try again checking for VC Proof Request later...");
            }
        }
    }

}