/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AasxPackageExplorer;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Threading;
using AasxIntegrationBase;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using AdminShellEvents;
using AasxWpfControlLibrary.AasxFileRepo;

namespace AasxWpfControlLibrary.Toolkit
{
    /// <summary>
    /// Some tools around the HttpClient for HTTP and REST connections
    /// </summary>
    public class ClientToolkitHttpRest : HttpClient
    {
        /// <summary>
        /// Is the left part of an URL, which forms an Endpoint; routes will generated to the right of
        /// it
        /// </summary>
        public Uri Endpoint;

        /// <summary>
        /// Contains that portion of the Endpoint, which is not base address and is not query.
        /// All (constrcuted) routes shall add to base address + endPointSegments.
        /// </summary>
        public string EndPointSegments;

        //
        // Constructors
        //

        public ClientToolkitHttpRest() { }

        public ClientToolkitHttpRest(HttpClientHandler handler)
            : base (handler)
        {
        }

        public static ClientToolkitHttpRest CreateNew(Uri endpoint, bool defaultRequestHandlerJson = true)
        {
            // make HTTP Client
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;

            var res = new ClientToolkitHttpRest(handler);
            if (defaultRequestHandlerJson)
                res.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

            // split into base address and end part
            res.Endpoint = endpoint;
            res.BaseAddress = new Uri(endpoint.GetLeftPart(UriPartial.Authority));
            res.EndPointSegments = endpoint.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);

            // ok 
            return res;
        }

        //
        // Helper
        //

        public override string ToString()
        {
            return $"HTTP/REST client {"" + BaseAddress?.ToString()} / {"" + EndPointSegments}";
        }

        /// <summary>
        /// Combines some routes, maintaining a '/' between them.
        /// Note: empty segments will be skipped
        /// Note: design choice to make '/' not a constant or so ..
        /// Note: it makes the logic explicit. Using <c>new Uri(Uri baseUri, string relativeUri)</c> was considered
        ///       as a pattern, but not adopted.
        /// </summary>
        /// <param name="segments">Segements of the route.</param>
        /// <returns>A route without trailing slash.</returns>
        public string CombineQuery(string first, params string[] segments)
        {
            // sum
            string res = ("" + first).Trim().TrimEnd('/');

            // rights
            if (segments != null)
                foreach (var r in segments)
                {
                    if (!r.HasContent())
                        continue;
                    var rr = r.Trim().Trim('/');
                    res += "/" + rr;
                }

            // trailing slash
            res.TrimEnd('/');
            return res;
        }

        /// <summary>
        /// This will execute <c>CombineQuery</c> headed by the appropriate endpoint segements.
        /// </summary>
        /// <param name="segments">Segements of the route.</param>
        /// <returns>A route without trailing slash.</returns>
        public string PrepareQuery(params string[] segments)
        {
            return CombineQuery(EndPointSegments, segments);
        }

    }
}
