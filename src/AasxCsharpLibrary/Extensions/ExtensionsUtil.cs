using AasCore.Aas3_0_RC02;
using AasxCompatibilityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtensionsUtil
    {

        public static Reference ConvertReferenceFromV10(AdminShellV10.Reference sourceReference, ReferenceTypes referenceTypes)
        {
            Reference outputReference = null;
            if (sourceReference != null)
            {
                var keyList = new List<Key>();
                foreach (var refKey in sourceReference.Keys)
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
                outputReference = new Reference(referenceTypes, keyList);
            }

            return outputReference;
        }

        public static Reference ConvertReferenceFromV20(AdminShellV20.Reference sourceReference, ReferenceTypes referenceTypes)
        {
            Reference outputReference = null;
            if (sourceReference != null)
            {
                var keyList = new List<Key>();
                foreach (var refKey in sourceReference.Keys)
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
                outputReference = new Reference(referenceTypes, keyList);
            }

            return outputReference;
        }

        internal static LangStringSet ConvertDescriptionFromV10(AdminShellV10.Description sourceDescription)
        {
            var newLangStrList = new List<LangString>();
            foreach (var ls in sourceDescription.langString)
            {
                newLangStrList.Add(new LangString(ls.lang, ls.str));
            }

            return new LangStringSet(newLangStrList);
        }

        internal static LangStringSet ConvertDescriptionFromV20(AdminShellV20.Description sourceDescription)
        {
            var newLangStrList = new List<LangString>();
            foreach (var ls in sourceDescription.langString)
            {
                newLangStrList.Add(new LangString(ls.lang, ls.str));
            }

            return new LangStringSet(newLangStrList);
        }

        internal static KeyTypes GetKeyType(IClass aasElement)
        {
            return aasElement switch
            {
                AssetAdministrationShell => KeyTypes.AssetAdministrationShell,
                Submodel => KeyTypes.Submodel,
                ConceptDescription => KeyTypes.ConceptDescription,
                SubmodelElementCollection => KeyTypes.SubmodelElementCollection,
                SubmodelElementList => KeyTypes.SubmodelElementList,
                BasicEventElement => KeyTypes.BasicEventElement,
                Blob => KeyTypes.Blob,
                Entity => KeyTypes.Entity,
                File => KeyTypes.File,
                MultiLanguageProperty => KeyTypes.MultiLanguageProperty,
                Property => KeyTypes.Property,
                Operation => KeyTypes.Operation,
                AasCore.Aas3_0_RC02.Range => KeyTypes.Range,
                ReferenceElement => KeyTypes.ReferenceElement,
                RelationshipElement => KeyTypes.RelationshipElement,
                AnnotatedRelationshipElement => KeyTypes.AnnotatedRelationshipElement,
                IIdentifiable => KeyTypes.Identifiable,
                IReferable => KeyTypes.Referable,
                Reference => KeyTypes.GlobalReference,//TODO:jtikekar what about model reference
                _ => KeyTypes.SubmodelElement,  // default case
            };
        }
    }
}
