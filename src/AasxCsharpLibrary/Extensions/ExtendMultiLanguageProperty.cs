using AasCore.Aas3_0_RC02;
using AdminShellNS.Extenstions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendMultiLanguageProperty
    {
        #region AasxPackageExplorer

        public static void ValueFromText(this MultiLanguageProperty multiLanguageProperty, string text, string defaultLang)
        {
            multiLanguageProperty.Value ??= new List<LangString>();

            multiLanguageProperty.Value.Add(new LangString(defaultLang == null? "en" : defaultLang, text));
        }

        #endregion

        public static string ValueAsText(this MultiLanguageProperty multiLanguageProperty, string defaultLang = null)
        {
            //TODO: need to check/test again
            //return "" + multiLanguageProperty.Value?.LangStrings.FirstOrDefault().Text;
            return "" + multiLanguageProperty.Value?.GetDefaultString(defaultLang);
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

            List<LangString> newLangStringSet = new(newLangStrings);

            property.Value = newLangStringSet.ConvertFromV20(sourceProperty.value);

            return property;

        }

        public static MultiLanguageProperty UpdateFrom(
            this MultiLanguageProperty elem, ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((ISubmodelElement)elem).UpdateFrom(source);

            if (source is Property srcProp)
            {
                elem.Value = new List<LangString> { new LangString("EN?", srcProp.Value) };
                if (srcProp.ValueId != null)
                    elem.ValueId = srcProp.ValueId.Copy();
            }

            if (source is MultiLanguageProperty srcMlp)
            {
                if (srcMlp.Value != null)
                    elem.Value = srcMlp.Value.Copy();
                if (srcMlp.ValueId != null)
                    elem.ValueId = srcMlp.ValueId.Copy();
            }

            if (source is AasCore.Aas3_0_RC02.Range srcRng)
            {
                if (srcRng.Min != null)
                    elem.Value = new List<LangString> { new LangString("EN?", srcRng.Min) };
            }

            if (source is File srcFile)
            {
                elem.Value = new List<LangString> { new LangString("EN?", srcFile.Value) };
            }

            return elem;
        }
    }
}
