﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace SSIExtension
{
    public class Prover
    {
        public string APIEndpoint { get; }
        string verifier_connection_id;

        public Prover(string APIEndpoint)
        {
            this.APIEndpoint = APIEndpoint;
        }

        public string CreateInvitation()
        {
            // oz
            return "aorzelski@phoenixcontact.com";
            // oz end

            HttpResponseMessage result = new HttpClient().PostAsync(APIEndpoint + $"/connections/create-invitation?auto_accept=true", new StringContent("{}")).Result;
            var resultJson = result.Content.ReadAsStringAsync().Result;
            var invitationForVerifier = JsonDocument.Parse(resultJson).RootElement.GetProperty("invitation").ToString();
            verifier_connection_id = JsonDocument.Parse(resultJson).RootElement.GetProperty("connection_id").ToString();
            Console.WriteLine($"Invitation for Verifier created. The Connection for Verifier has ID '{verifier_connection_id}'");

            Console.WriteLine("Waiting for the Connection to be accepted and VC to be requested by Verifier.");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            retryVCPresentationTimer = new Timer(CheckPresentVCCallback, stopwatch, 1000, 1000);

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
                string records = httpClient.GetAsync(APIEndpoint + $"/present-proof-2.0/records?connection_id={verifier_connection_id}&role=prover&state=request-received").Result.Content.ReadAsStringAsync().Result;
                var pres_ex_id = JsonDocument.Parse(records).RootElement.GetProperty("results").EnumerateArray().First().GetProperty("pres_ex_id").GetString();

                Console.WriteLine("VC Proof Request reveived. Try sending the VC Presentation.");
                string allCredentialsFromAPI = httpClient.GetAsync(APIEndpoint + "/credentials").Result.Content.ReadAsStringAsync().Result;
                string cred_id = JsonDocument.Parse(allCredentialsFromAPI).RootElement.GetProperty("results").EnumerateArray().Where(r => r.GetProperty("cred_def_id").ToString() == Utils.CRED_DEF_ID).First().GetProperty("referent").GetString();
                Console.WriteLine($"Using VC with ID '{cred_id}' for VC Presentation");
                string proofPresentation = Utils.CreateProofPresentation(cred_id);
                JsonDocument.Parse(proofPresentation);

                HttpResponseMessage proofPresHttpResponse = httpClient.
                    PostAsync(APIEndpoint + $"/present-proof-2.0/records/{pres_ex_id}/send-presentation", new StringContent(proofPresentation, Encoding.UTF8, "application/json")).Result;
                if (proofPresHttpResponse.IsSuccessStatusCode)
                {
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

            if (stopwatch.ElapsedMilliseconds > 30000)
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
