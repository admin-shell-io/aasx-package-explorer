using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Extensions
{
    public static class ExtendProperty
    {
        #region AasxPackageExplorer

        public static void ValueFromText(this Property property, string text)
        {
            property.Value = text;
        }

        #endregion
        public static bool IsValueTrue(this Property property)
        {
            if (property.ValueType == DataTypeDefXsd.Boolean)
            {
                if (property.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string ValueAsText(this Property property)
        {
            return "" + property.Value;
        }

        public static double? ValueAsDouble(this Property prop)
        {
            // pointless
            if (prop.Value == null || prop.Value.Trim() == "")
                return null;

            // type?
            if (!ExtendDataElement.ValueTypes_Number.Contains(prop.ValueType))
                return null;

            // try convert
            if (double.TryParse(prop.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double dbl))
                return dbl;

            // no
            return null;
        }
        public static Property ConvertFromV10(this Property property, AasxCompatibilityModels.AdminShellV10.Property sourceProperty)
        {
            if (sourceProperty == null)
            {
                return null;
            }
            var propertyType = Stringification.DataTypeDefXsdFromString("xs:" + sourceProperty.valueType);
            if (propertyType != null)
            {
                property.ValueType = (DataTypeDefXsd)propertyType;
            }
            else
            {
                Console.WriteLine($"ValueType {sourceProperty.valueType} not found for property {sourceProperty.idShort}");
            }
            property.Value = sourceProperty.value;
            if (sourceProperty.valueId != null)
            {
                var keyList = new List<IKey>();
                foreach (var refKey in sourceProperty.valueId.Keys)
                {
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {sourceProperty.valueType} not found for property {property.IdShort}");
                    }
                }
                property.ValueId = new Reference(ReferenceTypes.ExternalReference, keyList);
            }

            return property;
        }

        public static Property ConvertFromV20(this Property property, AasxCompatibilityModels.AdminShellV20.Property sourceProperty)
        {
            if (sourceProperty == null)
            {
                return null;
            }

            var propertyType = Stringification.DataTypeDefXsdFromString("xs:" + sourceProperty.valueType);
            if (propertyType != null)
            {
                property.ValueType = (DataTypeDefXsd)propertyType;
            }
            else
            {
                Console.WriteLine($"ValueType {sourceProperty.valueType} not found for property {sourceProperty.idShort}");
            }
            property.Value = sourceProperty.value;
            if (sourceProperty.valueId != null)
            {
                var keyList = new List<IKey>();
                foreach (var refKey in sourceProperty.valueId.Keys)
                {
                    //keyList.Add(new Key(ExtensionsUtil.GetKeyTypeFromString(refKey.type), refKey.value));
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {sourceProperty.valueType} not found for property {property.IdShort}");
                    }
                }
                property.ValueId = new Reference(ReferenceTypes.ExternalReference, keyList);
            }

            return property;
        }

        public static Property UpdateFrom(this Property elem, ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((ISubmodelElement)elem).UpdateFrom(source);

            if (source is Property srcProp)
            {
                elem.ValueType = srcProp.ValueType;
                elem.Value = srcProp.Value;
                if (srcProp.ValueId != null)
                    elem.ValueId = srcProp.ValueId.Copy();
            }

            if (source is AasCore.Aas3_0.Range srcRng)
            {
                elem.ValueType = srcRng.ValueType;
                elem.Value = srcRng.Min;
            }

            if (source is MultiLanguageProperty srcMlp)
            {
                elem.ValueType = DataTypeDefXsd.String;
                elem.Value = "" + srcMlp.Value?.GetDefaultString();
                if (srcMlp.ValueId != null)
                    elem.ValueId = srcMlp.ValueId.Copy();
            }

            if (source is File srcFile)
            {
                elem.ValueType = DataTypeDefXsd.String;
                elem.Value = "" + srcFile.Value;
            }

            return elem;
        }

        // MIHO: Jui, why was this required?
#if OLD

        public static void UpdatePropertyFrom(this Property property, Property sourceProperty)
        {
            if (sourceProperty.Extensions != null)
            {
                property.Extensions = sourceProperty.Extensions;
            }
            if (sourceProperty.Category != null)
            {
                property.Category = sourceProperty.Category;
            }
            if (sourceProperty.IdShort != null)
            {
                property.IdShort = sourceProperty.IdShort;
            }
            if (sourceProperty.DisplayName != null)
            {
                property.DisplayName = sourceProperty.DisplayName;
            }
            if (sourceProperty.Description != null)
            {
                property.Description = sourceProperty.Description;
            }
            if (sourceProperty.Checksum != null)
            {
                property.Checksum = sourceProperty.Checksum;
            }
            if (sourceProperty.Kind != null)
            {
                property.Kind = sourceProperty.Kind;
            }
            if (sourceProperty.SemanticId != null)
            {
                property.SemanticId = sourceProperty.SemanticId;
            }
            if (sourceProperty.SupplementalSemanticIds != null)
            {
                property.SupplementalSemanticIds = sourceProperty.SupplementalSemanticIds;
            }
            if (sourceProperty.Qualifiers != null)
            {
                property.Qualifiers = sourceProperty.Qualifiers;
            }
            if (sourceProperty.EmbeddedDataSpecifications != null)
            {
                property.EmbeddedDataSpecifications = sourceProperty.EmbeddedDataSpecifications;
            }
            if (true)
            {
                property.ValueType = sourceProperty.ValueType;
            }
            if (sourceProperty.ValueId != null)
            {
                property.ValueId = sourceProperty.ValueId;
            }
            if (sourceProperty.Value != null)
            {
                property.Value = sourceProperty.Value;
            }
        }
#endif

        public static Property Set(this Property prop,
            DataTypeDefXsd valueType = DataTypeDefXsd.String, string value = "")
        {
            prop.ValueType = valueType;
            prop.Value = value;
            return prop;
        }

        public static Property Set(this Property prop,
            KeyTypes type, string value)
        {
            prop.ValueId = ExtendReference.CreateFromKey(new Key(type, value));
            return prop;
        }

        public static Property Set(this Property prop,
            Qualifier q)
        {
            if (q != null)
                prop.Add(q);
            return prop;
        }

        public static Property Set(this Property prop,
            Extension ext)
        {
            if (ext != null)
                prop.Add(ext);
            return prop;
        }
    }
}
