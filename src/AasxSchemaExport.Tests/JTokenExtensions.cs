/*
Copyright (c) 2022 PHOENIX CONTACT GmbH & Co. KG <info@phoenixcontact.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using Newtonsoft.Json.Linq;

namespace AasxSchemaExport.Tests
{
    public static class JTokenExtensions
    {
        public static T GetValue<T>(this JToken token, string path)
        {
            var targetToken = token.SelectToken(path);
            if (targetToken == null)
                throw new Exception($"Token with the path: {path} was not found.");

            return targetToken.Value<T>();
        }
    }
}
