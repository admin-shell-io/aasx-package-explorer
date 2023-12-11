/*
Copyright (c) 2022 PHOENIX CONTACT GmbH & Co. KG <info@phoenixcontact.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

namespace AasxSchemaExport
{
    public static class Tokens
    {
        public static string Schema = "$schema";
        public static string Title = "title";
        public static string Type = "type";
        public static string UnevaluatedProperties = "unevaluatedProperties";
        public static string AllOf = "allOf";
        public static string Ref = "$ref";
        public static string Const = "const";
        public static string Pattern = "pattern";
        public static string Properties = "properties";
        public static string Contains = "contains";
        public static string MinContains = "minContains";
        public static string MaxContains = "maxContains";

        public static string Definitions = "definitions";
        public static string Identifiable = "identifiable";
        public static string Elements = "elements";

        public static string Name = "name";
        public static string IdShort = "idShort";
        public static string Kind = "kind";
        public static string ModelType = "modelType";
        public static string SubmodelElements = "submodelElements";
        public static string SemanticId = "semanticId";
        public static string Keys = "keys";
        public static string MType = "type";
        public static string Local = "local";
        public static string Value = "value";
        public static string IdType = "idType";
        public static string ValueType = "valueType";
        public static string DataObjectType = "dataObjectType";
    }
}
