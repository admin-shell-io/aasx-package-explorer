/* 
 * DotAAS Part 2 | HTTP/REST | Entire Interface Collection
 *
 * The entire interface collection as part of Details of the Asset Administration Shell Part 2
 *
 * OpenAPI spec version: Final-Draft
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SwaggerDateConverter = IO.Swagger.Client.SwaggerDateConverter;

namespace IO.Swagger.Model
{
    /// <summary>
    /// Defines LevelType
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LevelType
    {
        /// <summary>
        /// Enum Min for value: Min
        /// </summary>
        [EnumMember(Value = "Min")]
        Min = 1,
        /// <summary>
        /// Enum Max for value: Max
        /// </summary>
        [EnumMember(Value = "Max")]
        Max = 2,
        /// <summary>
        /// Enum Nom for value: Nom
        /// </summary>
        [EnumMember(Value = "Nom")]
        Nom = 3,
        /// <summary>
        /// Enum Typ for value: Typ
        /// </summary>
        [EnumMember(Value = "Typ")]
        Typ = 4
    }
}