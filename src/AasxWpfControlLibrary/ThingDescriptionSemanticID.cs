/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using AdminShellNS;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;

namespace AasxPackageExplorer
{
    public static class TDSemanticId
    {
        public static JObject semanticIDJObject = new JObject {
            // td Schema
            ["Thing"] = "https://www.w3.org/2019/wot/td#Thing",
            ["@type"] = "tbd",
            ["@id"] = "tbd",
            ["id"] = "tbd",
            ["title"] = "https://dublincore.org/specifications/dublin-core/dcmi-terms/#title",
            ["titles"] = "https://dublincore.org/specifications/dublin-core/dcmi-terms/#title",
            ["description"] = "https://www.dublincore.org/specifications/dublin-core/dcmi-terms/#description",
            ["descriptions"] = "https://www.dublincore.org/specifications/dublin-core/dcmi-terms/#description",
            
            ["created"] = "https://dublincore.org/specifications/dublin-core/dcmi-terms/#created",
            ["modified"] = "https://dublincore.org/specifications/dublin-core/dcmi-terms/#modified",
            ["support"] = "tbd",
            ["base"] = "tbd",

            //Property
            ["properties"] = "https://www.w3.org/2019/wot/json-schema#properties",
            ["property"] = "https://www.w3.org/2019/wot/td#PropertyAffordance",// Property Affordance
            ["observable"] = "https://www.w3.org/2019/wot/td#isObservable",

            //action
            ["actions"] = "tbd",
            ["action"] = "https://www.w3.org/2019/wot/td#ActionAffordance",
            ["input"] = "https://www.w3.org/2019/wot/td#hasInputSchema",
            ["output"] = "https://www.w3.org/2019/wot/td#hasOutputSchema",
            ["safe"] = "https://www.w3.org/2019/wot/td#isSafe",
            ["idempotent"] = "https://www.w3.org/2019/wot/td#isIdempotent",

            //event
            ["events"] = "tbd",
            ["event"] = "https://www.w3.org/2019/wot/td#EventAffordance",
            ["subscription"] = "https://www.w3.org/2019/wot/td#hasSubscriptionSchema",
            ["data"] = "https://www.w3.org/2019/wot/td#hasNotificationSchema",
            ["cancellation"] = "https://www.w3.org/2019/wot/td#hasCancellationSchema",

            //links
            ["links"] = "tbd",
            ["link"] = "https://www.w3.org/2019/wot/hypermedia#Link",

            //forms
            ["forms"] = "tbd",
            ["form"] = "https://www.w3.org/2019/wot/hypermedia#Form",

            ["security"] = "tbd",

            //securityDefinitions
            ["securityDefinitions"] = "tbd",
            ["apikey"] = "https://www.w3.org/2019/wot/security#APIKeySecurityScheme",
            ["basic"] = "https://www.w3.org/2019/wot/security#BasicSecurityScheme",
            ["bearer"] = "https://www.w3.org/2019/wot/security#BearerSecurityScheme",
            ["combo"] = "https://www.w3.org/2019/wot/security#ComboSecurityScheme",
            ["digest"] = "https://www.w3.org/2019/wot/security#DigestSecurityScheme",
            ["nosec"] = "https://www.w3.org/2019/wot/security#NoSecurityScheme",
            ["oauth2"] = "https://www.w3.org/2019/wot/security#OAuth2SecurityScheme",
            ["psk"] = "https://www.w3.org/2019/wot/security#PSKSecurityScheme",


            ["allOf"] = "https://www.w3.org/2019/wot/security#allOf",
            ["authorization"] = "https://www.w3.org/2019/wot/security#authorization",
            ["oneOf"] = "https://www.w3.org/2019/wot/security#oneOf",
            ["proxy"] = "https://www.w3.org/2019/wot/security#proxy",
            ["refresh"] = "https://www.w3.org/2019/wot/security#refresh",
            ["token"] = "https://www.w3.org/2019/wot/security#token",
            ["alg"] = "https://www.w3.org/2019/wot/security#alg",
            ["flow"] = "https://www.w3.org/2019/wot/security#flow",
            ["format"] = "https://www.w3.org/2019/wot/security#format",
            ["identity"] = "https://www.w3.org/2019/wot/security#identity",
            ["in"] = "https://www.w3.org/2019/wot/security#in",
            ["name"] = "https://www.w3.org/2019/wot/security#name",
            ["qop"] = "https://www.w3.org/2019/wot/security#qop",
            ["scopes"] = "https://www.w3.org/2019/wot/security#scopes",

            //DataSchema
            ["const"] = "https://www.w3.org/2019/wot/json-schema#const",
            ["default"] = "https://www.w3.org/2019/wot/json-schema#default",
            ["oneOf"] = "https://www.w3.org/2019/wot/json-schema#oneOf",
            ["enum"] = "https://www.w3.org/2019/wot/json-schema#enum",
            ["readOnly"] = "https://www.w3.org/2019/wot/json-schema#readOnly",
            ["writeOnly"] = "https://www.w3.org/2019/wot/json-schema#writeOnly",
            ["format"] = "https://www.w3.org/2019/wot/json-schema#format",
            ["type"] = "tbd",

            //ArraySchema
            ["items"] = "https://www.w3.org/2019/wot/json-schema#items",
            ["item"] = "",
            ["minItems"] = "https://www.w3.org/2019/wot/json-schema#minItems",
            ["maxItems"] = "https://www.w3.org/2019/wot/json-schema#maxItems",

            //NumberSchema and Integer
            ["minimum"] = "https://www.w3.org/2019/wot/json-schema#minimum",
            ["exclusiveMinimum"] = "https://www.w3.org/2019/wot/json-schema#exclusiveMinimum",
            ["maximum"] = "https://www.w3.org/2019/wot/json-schema#maximum",
            ["exclusiveMaximum"] = "https://www.w3.org/2019/wot/json-schema#exclusiveMaximum",
            ["multipleOf"] = "https://www.w3.org/2019/wot/json-schema#multipleOf",

            //ObjectSchema 
            //["properties"] 
            ["required"] = "https://www.w3.org/2019/wot/json-schema#required",

            //StringSchema
            ["minLength"] = "https://www.w3.org/2019/wot/json-schema#minLength",
            ["maxLength"] = "https://www.w3.org/2019/wot/json-schema#maxLength",
            ["pattern"] = "https://www.w3.org/2019/wot/json-schema#pattern",
            ["contentEncoding"] = "https://www.w3.org/2019/wot/json-schema#contentEncoding",
            ["contentMediaType"] = "https://www.w3.org/2019/wot/json-schema#contentMediaType",

            ["profile"] = "tbd",

            
            ["op"] = "tbd",

            ["success"] = "https://www.w3.org/2019/wot/hypermedia#isSuccess",
            ["ContentType"] = "https://www.w3.org/2019/wot/hypermedia#forContentType",

            ["version"] = "tbd",
            ["VersionInfo"] = "tbd",

            ["invokeAction"] = "https://www.w3.org/2019/wot/td#invokeAction",
            ["observeAllProperties"] = "https://www.w3.org/2019/wot/td#observeAllProperties",
            ["observeProperty"] = "https://www.w3.org/2019/wot/td#observeProperty",
            ["readAllProperties"] = "https://www.w3.org/2019/wot/td#readAllProperties",
            ["readMultipleProperties"] = "https://www.w3.org/2019/wot/td#readMultipleProperties",
            ["readProperty"] = "https://www.w3.org/2019/wot/td#readProperty",
            ["subscribeAllEvents"] = "https://www.w3.org/2019/wot/td#subscribeAllEvents",
            ["subscribeEvent"] = "https://www.w3.org/2019/wot/td#subscribeEvent",
            ["unobserveAllProperties"] = "https://www.w3.org/2019/wot/td#unobserveAllProperties",
            ["unobserveProperty"] = "https://www.w3.org/2019/wot/td#unobserveProperty",
            ["unsubscribeAllEvents"] = "https://www.w3.org/2019/wot/td#unsubscribeAllEvents",
            ["unsubscribeEvent"] = "https://www.w3.org/2019/wot/td#unsubscribeEvent",
            ["writeAllProperties"] = "https://www.w3.org/2019/wot/td#writeAllProperties",
            ["writeMultipleProperties"] = "https://www.w3.org/2019/wot/td#writeMultipleProperties",
            ["writeProperty"] = "https://www.w3.org/2019/wot/td#writeProperty",

            // json-schema
            ["allOf"] = "https://www.w3.org/2019/wot/json-schema#allOf",
            ["anyOf"] = "https://www.w3.org/2019/wot/json-schema#anyOf",
            ["contentType"] = "https://www.w3.org/2019/wot/hypermedia#forContentType",

            ["href"] = "tbd",

            ["actions"] = "tbd",
            ["scope"] = "tbd",
            ["uriVariables"] = "tbd",
            
            ["scheme"] = "tbd",



            ["schemaDefinitions"] = "tbd",
            ["propertyName"] = "https://www.w3.org/2019/wot/json-schema#propertyName",


            //security


            //hypermedia

            ["AdditionalExpectedResponse"] = "https://www.w3.org/2019/wot/hypermedia#AdditionalExpectedResponse",
            ["ExpectedResponse"] = "https://www.w3.org/2019/wot/hypermedia#ExpectedResponse",

        };
        public static string getSemanticID(string tdType)
        {
            if(semanticIDJObject.ContainsKey(tdType))
            {
                return semanticIDJObject[tdType].ToString();
            }
            else
            {
                return "empty";
            }

        }
    }
}
/*
            { "" , ""},
            { "" , ""},
            { "" , ""},
            { "" , ""},
            { "" , ""},
            { "" , ""},
            { "" , ""},
            { "" , ""},
            { "" , ""},

 */
