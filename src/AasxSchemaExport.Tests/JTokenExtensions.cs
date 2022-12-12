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
