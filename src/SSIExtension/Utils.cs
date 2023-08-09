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
using System.IO;
using System.Net.Http;
using System.Text;

// ReSharper disable All .. as this is test code

namespace SSIExtension
{

    public static class Utils
    {
        public static string CRED_DEF_ID = "TAy1Ac5LKJ2WnWixNHitYf:3:CL:16:default";

        public static string CreateProofRequest(string connection_id)
        {
            return "{\"comment\":\"This is a comment about the reason for the proof\",\"connection_id\":\"" +
                connection_id +
                "\",\"presentation_request\":{\"indy\":{\"name\":\"Proof of Something\"," +
                "\"version\":\"1.0\",\"requested_attributes\":{\"0_email_uuid\":{\"name\":\"email\"," +
                "\"restrictions\":[{\"cred_def_id\":\"" +
                CRED_DEF_ID +
                "\"}]}},\"requested_predicates\":{}}}}";
        }

        public static string CreateProofPresentation(string cred_id)
        {
            return "{\"indy\":{\"requested_attributes\":{\"0_email_uuid\":{\"cred_id\":\"" +
                cred_id +
                "\",\"revealed\":true}},\"requested_predicates\":{},\"self_attested_attributes\":{}}}";
        }
    }
}
