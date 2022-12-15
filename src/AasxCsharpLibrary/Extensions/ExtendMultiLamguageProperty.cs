using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendMultiLamguageProperty
    {
        #region AasxPackageExplorer

        public static void ValueFromText(this MultiLanguageProperty multiLanguageProperty, string text, string defaultLang)
        {
            multiLanguageProperty.Value ??= new LangStringSet(new List<LangString>());

            multiLanguageProperty.Value.LangStrings.Add(new LangString(defaultLang == null? "en" : defaultLang, text));
        }

        #endregion

        public static string ValueAsText(this MultiLanguageProperty multiLanguageProperty, string defaultLang = null)
        {
            //TODO: need to check/test again
            //return "" + multiLanguageProperty.Value?.LangStrings.FirstOrDefault().Text;
            return "" + multiLanguageProperty.Value?.GetDefaultString();
        }

        public static MultiLanguageProperty ConvertFromV20(this MultiLanguageProperty property, AasxCompatibilityModels.AdminShellV20.MultiLanguageProperty sourceProperty)
        {
            if (sourceProperty == null)
            {
                return null;
            }

            if (sourceProperty.valueId != null)
            {
                var keyList = new List<Key>();
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
                        Console.WriteLine($"KeyType value not found for property {property.IdShort}");
                    }
                }
                property.ValueId = new Reference(ReferenceTypes.GlobalReference, keyList);
            }

            var newLangStrings = new List<LangString>();

            LangStringSet newLangStringSet = new(newLangStrings);

            property.Value = newLangStringSet.ConvertFromV20(sourceProperty.value);

            return property;

        }
    }
}
