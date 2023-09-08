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

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using static System.Text.Json.JsonElement;

// ReSharper disable All .. as this is test code

namespace SSIExtension
{

    public class Verifier
    {
        public string APIEndpoint { get; }

        public Verifier(string APIEndpoint)
        {
            this.APIEndpoint = APIEndpoint;
        }

        public Dictionary<string, string> GetVerifiedAttributes(string invitation)
        {
            var result = new Dictionary<string, string>();

            if (!test.withAgents)
            {
                var _key = "email";
                var _value = invitation;
                result.Add(_key, _value);
                return result;
            }

            //receive invitation
            var clientInvitee = new HttpClient();
            string resultInvitee = clientInvitee.PostAsync(APIEndpoint +
                $"/connections/receive-invitation?auto_accept=true&alias=Prover",
                new StringContent(invitation)).Result.Content.ReadAsStringAsync().Result;
            var prover_connection_id = JsonDocument.Parse(resultInvitee).
                RootElement.GetProperty("connection_id").GetString();

            //trust ping for completion
            var clientTrustPing = new HttpClient();
            string resultTrustPing = clientTrustPing.PostAsync(APIEndpoint +
                $"/connections/{prover_connection_id}/send-ping",
                new StringContent("{}")).Result.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"Invitation accepted, Provers Connection ID is '{resultInvitee}'");
            //wait a little bit so that the connection is ready
            Thread.Sleep(1000);

            //verifier requests proof
            string proofReq = Utils.CreateProofRequest(prover_connection_id);
            JsonDocument.Parse(proofReq);

            var clientRequester = new HttpClient();
            HttpResponseMessage httpresult = clientRequester.
                PostAsync(APIEndpoint + $"/present-proof-2.0/send-request",
                new StringContent(proofReq, Encoding.UTF8, "application/json")).Result;
            var resultJson = httpresult.Content.ReadAsStringAsync().Result;
            var requester_pres_ex_id = JsonDocument.Parse(resultJson).
                RootElement.GetProperty("pres_ex_id").ToString();
            Console.WriteLine($"Proof requested [{requester_pres_ex_id}]");

            //give prove time to respond
            Thread.Sleep(3000);

            //verifier verifies presentation
            HttpResponseMessage verifyPresentationResult = clientRequester.
                PostAsync(APIEndpoint +
                    $"/present-proof-2.0/records/{requester_pres_ex_id}/verify-presentation", null).Result;
            if (verifyPresentationResult.IsSuccessStatusCode)
            {
                var jsonDoc = JsonDocument.Parse(verifyPresentationResult.Content.ReadAsStringAsync().Result);
                var verified = jsonDoc.RootElement.GetProperty("verified").GetString();
                if (verified == "true")
                {
                    ObjectEnumerator objEnum = jsonDoc.RootElement.GetProperty("by_format")
                        .GetProperty("pres")
                        .GetProperty("indy")
                        .GetProperty("requested_proof")
                        .GetProperty("revealed_attrs").EnumerateObject();
                    foreach (var item in objEnum)
                    {
                        var key = item.Name.Split('_')[1];
                        var value = item.Value.GetProperty("raw").ToString();
                        result.Add(key, value);
                    }
                }
            }
            return result;
        }

    }
}
