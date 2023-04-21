using AasCore.Aas3_0_RC02;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendQualifier
    {
        public static Qualifier ConvertFromV10(this Qualifier qualifier, AasxCompatibilityModels.AdminShellV10.Qualifier sourceQualifier)
        {
            if (sourceQualifier.semanticId != null)
            {
                var keyList = new List<Key>();
                foreach (var refKey in sourceQualifier.semanticId.Keys)
                {
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {refKey.type} not found.");
                    }
                }
                qualifier.SemanticId = new Reference(ReferenceTypes.GlobalReference, keyList);
            }

            qualifier.Type = sourceQualifier.qualifierType;
            qualifier.Value = sourceQualifier.qualifierValue;

            if (sourceQualifier.qualifierValueId != null)
            {
                var keyList = new List<Key>();
                foreach (var refKey in sourceQualifier.qualifierValueId.Keys)
                {
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {refKey.type} not found.");
                    }
                }
                qualifier.ValueId = new Reference(ReferenceTypes.GlobalReference, keyList);
            }

            return qualifier;
        }

        public static Qualifier ConvertFromV20(this Qualifier qualifier, AasxCompatibilityModels.AdminShellV20.Qualifier sourceQualifier)
        {
            if (sourceQualifier.semanticId != null)
            {
                var keyList = new List<Key>();
                foreach (var refKey in sourceQualifier.semanticId.Keys)
                {
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {refKey.type} not found.");
                    }

                }
                qualifier.SemanticId = new Reference(ReferenceTypes.GlobalReference, keyList);
            }

            qualifier.Type = sourceQualifier.type;
            qualifier.Value = sourceQualifier.value;

            if (sourceQualifier.valueId != null)
            {
                var keyList = new List<Key>();
                foreach (var refKey in sourceQualifier.valueId.Keys)
                {
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {refKey.type} not found.");
                    }
                }
                qualifier.ValueId = new Reference(ReferenceTypes.GlobalReference, keyList);
            }

            return qualifier;
        }

        // ReSharper disable MethodOverloadWithOptionalParameter .. this seems to work, anyhow
        // ReSharper disable RedundantArgumentDefaultValue
        public static string ToStringExtended(this Qualifier q,
            int format = 0, string delimiter = ",")
        {
            var res = "" + q.Type;
            if (res == "")
                res += "" + q.SemanticId?.ToStringExtended(format, delimiter);

            if (q.Value != null)
                res += " = " + q.Value;
            else if (q.ValueId != null)
                res += " = " + q.ValueId?.ToStringExtended(format, delimiter);

            return res;
        }
        // ReSharper enable MethodOverloadWithOptionalParameter
        // ReSharper enable RedundantArgumentDefaultValue

        //
        //
        // List<Qualifier>
        //
        //

        #region QualifierCollection

        public static Qualifier FindQualifierOfType(this List<Qualifier> qualifiers, string qualifierType)
        {
            if (qualifierType == null)
            {
                return null;
            }

            foreach (var qualifier in qualifiers)
            {
                if (qualifier != null && qualifierType.Equals(qualifier.Type))
                {
                    return qualifier;
                }
            }

            return null;
        }

        // ReSharper disable MethodOverloadWithOptionalParameter .. this seems to work, anyhow
        // ReSharper disable RedundantArgumentDefaultValue
        public static string ToStringExtended(this List<Qualifier> qualifiers,
            int format = 0, string delimiter = ";", string referencesDelimiter = ",")
        {
            var res = "";
            foreach (var q in qualifiers)
            {
                if (res != "")
                    res += delimiter;
                res += q.ToStringExtended(format, referencesDelimiter);
            }
            return res;
        }
        // ReSharper enable MethodOverloadWithOptionalParameter
        // ReSharper enable RedundantArgumentDefaultValue

        public static Qualifier FindType(this List<Qualifier> qualifiers, string type)
        {
            if (type == null || qualifiers == null)
                return null;
            foreach (var q in qualifiers)
                if (q != null && q.Type != null && q.Type.Trim() == type.Trim())
                    return q;
            return null;
        }

        public static Qualifier Parse(string input)
        {
            var m = Regex.Match(input, @"\s*([^,]*)(,[^=]+){0,1}\s*=\s*([^,]*)(,.+){0,1}\s*");
            if (!m.Success)
                return null;

            return new Qualifier(
                valueType: DataTypeDefXsd.String,
                type: m.Groups[1].ToString().Trim(),
                semanticId: ExtendReference.Parse(m.Groups[1].ToString().Trim()),
                value: m.Groups[3].ToString().Trim(),
                valueId: ExtendReference.Parse(m.Groups[1].ToString().Trim())
            );
        }

        #endregion
    }
}
