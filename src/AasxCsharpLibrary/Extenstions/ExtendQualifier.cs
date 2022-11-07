using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
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

        #region QualifierCollection

        public static Qualifier FindQualifierOfType(this List<Qualifier> qualifiers, string qualifierType)
        {
            if(qualifierType == null)
            {
                return null;
            }

            foreach(var qualifier in qualifiers)
            {
                if(qualifier != null && qualifierType.Equals(qualifier.Type))
                {
                    return qualifier;
                }
            }

            return null;
        }

        #endregion
    }
}
