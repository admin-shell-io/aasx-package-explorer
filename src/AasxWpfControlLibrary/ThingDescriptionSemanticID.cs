/*
Copyright (c) 2021-2022 Otto-von-Guericke-Universität Magdeburg, Lehrstuhl Integrierte Automation
harish.pakala@ovgu.de, Author: Harish Kumar Pakala

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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace AasxPackageExplorer
{
    public static class TDSemanticId
    {
        public static JObject semanticIDJObject = new JObject
        {
            // td Schema
            ["Thing"] = "https://www.w3.org/2019/wot/td#Thing",
            ["@type"] = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type",
            ["@id"] = "tbd",
            ["id"] = "tbd",
            ["@context"] = "https://www.w3.org/2019/wot/td/v1",
            ["title"] = "https://dublincore.org/specifications/dublin-core/dcmi-terms/#title",
            ["titles"] = "https://dublincore.org/specifications/dublin-core/dcmi-terms/#title",
            ["description"] = "https://www.dublincore.org/specifications/dublin-core/dcmi-terms/#description",
            ["descriptions"] = "https://www.dublincore.org/specifications/dublin-core/dcmi-terms/#description",

            ["created"] = "https://dublincore.org/specifications/dublin-core/dcmi-terms/#created",
            ["modified"] = "https://dublincore.org/specifications/dublin-core/dcmi-terms/#modified",
            ["support"] = "https://www.w3.org/2019/wot/td#supportContact",
            ["base"] = "https://www.w3.org/2019/wot/td#baseURI",

            //Property
            ["properties"] = "https://www.w3.org/2019/wot/json-schema#properties",
            ["property"] = "https://www.w3.org/2019/wot/td#PropertyAffordance",// Property Affordance
            ["observable"] = "https://www.w3.org/2019/wot/td#isObservable",

            //action
            ["actions"] = "https://www.w3.org/2019/wot/td#hasActionAffordance",
            ["action"] = "https://www.w3.org/2019/wot/td#ActionAffordance",
            ["input"] = "https://www.w3.org/2019/wot/td#hasInputSchema",
            ["output"] = "https://www.w3.org/2019/wot/td#hasOutputSchema",
            ["safe"] = "https://www.w3.org/2019/wot/td#isSafe",
            ["idempotent"] = "https://www.w3.org/2019/wot/td#isIdempotent",

            //event
            ["events"] = "https://www.w3.org/2019/wot/td#hasEventAffordance",
            ["event"] = "https://www.w3.org/2019/wot/td#EventAffordance",
            ["subscription"] = "https://www.w3.org/2019/wot/td#hasSubscriptionSchema",
            ["data"] = "https://www.w3.org/2019/wot/td#hasNotificationSchema",
            ["cancellation"] = "https://www.w3.org/2019/wot/td#hasCancellationSchema",

            //links
            ["links"] = "https://www.w3.org/2019/wot/td#hasLink",
            ["link"] = "https://www.w3.org/2019/wot/hypermedia#Link",

            //forms
            ["forms"] = "https://www.w3.org/2019/wot/td#hasForm",
            ["form"] = "https://www.w3.org/2019/wot/hypermedia#Form",

            ["security"] = "https://www.w3.org/2019/wot/td#hasSecurityConfiguration",

            //securityDefinitions
            ["securityDefinitions"] = "https://www.w3.org/2019/wot/td#definesSecurityScheme",
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
            ["item"] = "https://www.w3.org/2019/wot/json-schema#items",
            ["minItems"] = "https://www.w3.org/2019/wot/json-schema#minItems",
            ["maxItems"] = "https://www.w3.org/2019/wot/json-schema#maxItems",

            //NumberSchema and Integer
            ["minimum"] = "https://www.w3.org/2019/wot/json-schema#minimum",
            ["exclusiveMinimum"] = "https://www.w3.org/2019/wot/json-schema#exclusiveMinimum",
            ["maximum"] = "https://www.w3.org/2019/wot/json-schema#maximum",
            ["exclusiveMaximum"] = "https://www.w3.org/2019/wot/json-schema#exclusiveMaximum",
            ["multipleOf"] = "https://www.w3.org/2019/wot/json-schema#multipleOf",

            //ObjectSchema 
            ["required"] = "https://www.w3.org/2019/wot/json-schema#required",

            //StringSchema
            ["minLength"] = "https://www.w3.org/2019/wot/json-schema#minLength",
            ["maxLength"] = "https://www.w3.org/2019/wot/json-schema#maxLength",
            ["pattern"] = "https://www.w3.org/2019/wot/json-schema#pattern",
            ["contentEncoding"] = "https://www.w3.org/2019/wot/json-schema#contentEncoding",
            ["contentMediaType"] = "https://www.w3.org/2019/wot/json-schema#contentMediaType",

            ["profile"] = "https://www.w3.org/2019/wot/td#hasProfile",


            ["op"] = "https://www.w3.org/2019/wot/td#OperationType",

            ["success"] = "https://www.w3.org/2019/wot/hypermedia#isSuccess",
            ["ContentType"] = "https://www.w3.org/2019/wot/hypermedia#forContentType",

            ["version"] = "https://www.w3.org/2019/wot/td#versionInfo",
            ["VersionInfo"] = "https://www.w3.org/2019/wot/td#versionInfo",

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

            ["href"] = "https://www.w3.org/2019/wot/hypermedia#hasTarget",

            ["scope"] = "https://www.w3.org/2019/wot/td#scope",

            ["uriVariables"] = "",
            ["scheme"] = "",



            ["schemaDefinitions"] = "",
            ["propertyName"] = "https://www.w3.org/2019/wot/json-schema#propertyName",

            //hypermedia

            ["AdditionalExpectedResponse"] = "https://www.w3.org/2019/wot/hypermedia#AdditionalExpectedResponse",
            ["ExpectedResponse"] = "https://www.w3.org/2019/wot/hypermedia#ExpectedResponse",

        };

        public static JObject arrayListDescription = new JObject
        {
            ["security"] = "Set of security definition names, chosen from those defined in securityDefinitions." +
            "These must all be satisfied for access to resources.",
            ["scopes"] = "	Set of authorization scope identifiers provided as an array." +
            "These are provided in tokens returned by an authorization server and associated with forms in" +
            "order to identify what resources a client may access and how. The values associated with a" +
            "form should be chosen from those defined in an OAuth2SecurityScheme active on that form.",
            ["op"] = "Indicates the semantic intention of performing the operation(s) described by" +
            "the form. For example, the Property interaction allows get and set operations." +
            "The protocol binding may contain a form for the get operation and a different form for the" +
            "set operation. The op attribute indicates which form is for which and allows the client to select" +
            "the correct form for the operation required. op can be assigned one or more interaction verb(s) " +
            "representing a semantic intention of an operation."
        };
        public static JObject arrayListDesc = new JObject
        {
            ["enum"] = "Restricted set of values provided as an array.",
            ["@type"] = "JSON-LD keyword to label the object with semantic tags (or types)."
        };
        public static string getarrayListDescription(string tdType)
        {
            try
            {
                foreach (var temp in (JToken)arrayListDescription)
                {
                    JProperty x = (JProperty)temp;
                    if (x.Name.ToString() == tdType)
                    {
                        return x.Value.ToString();
                    }
                }
                return "";
            }
            catch
            {
                return "";
            }

        }
        public static string getarrayListDesc(string tdType)
        {
            try
            {
                foreach (var temp in (JToken)arrayListDesc)
                {
                    JProperty x = (JProperty)temp;
                    if (x.Name.ToString() == tdType)
                    {
                        return x.Value.ToString();
                    }
                }
                return "";
            }
            catch
            {
                return "";
            }

        }
        public static string getSemanticID(string tdType)
        {
            try
            {
                foreach (var temp in (JToken)semanticIDJObject)
                {
                    JProperty x = (JProperty)temp;
                    if (x.Name.ToString() == tdType)
                    {
                        return x.Value.ToString();
                    }
                }
                return "";
            }
            catch
            {
                return "";
            }

        }
    }
}