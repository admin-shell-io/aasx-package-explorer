/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AasxCompatibilityModels;
using System;
using System.Collections.Generic;

namespace Extensions
{
    public static class ExtensionsUtil
    {

        public static Reference ConvertReferenceFromV10(AdminShellV10.Reference sourceReference, ReferenceTypes referenceTypes)
        {
            Reference outputReference = null;
            if (sourceReference != null && !sourceReference.IsEmpty)
            {
                var keyList = new List<IKey>();
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
            if (sourceReference != null && !sourceReference.IsEmpty)
            {
                var keyList = new List<IKey>();
                foreach (var refKey in sourceReference.Keys)
                {
                    // Fix, as Asset does not exist anymore
                    if (refKey.type?.Trim().Equals("Asset", StringComparison.InvariantCultureIgnoreCase) == true)
                        refKey.type = "GlobalReference";

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

        internal static List<ILangStringTextType> ConvertDescriptionFromV10(AdminShellV10.Description sourceDescription)
        {
            var newLangStrList = new List<ILangStringTextType>();
            foreach (var ls in sourceDescription.langString)
            {
                newLangStrList.Add(new LangStringTextType(ls.lang, ls.str));
            }

            return new List<ILangStringTextType>(newLangStrList);
        }

        internal static List<ILangStringTextType> ConvertDescriptionFromV20(AdminShellV20.Description sourceDescription)
        {
            var newLangStrList = new List<ILangStringTextType>();
            foreach (var ls in sourceDescription.langString)
            {
                newLangStrList.Add(new LangStringTextType(ls.lang, ls.str));
            }

            return new List<ILangStringTextType>(newLangStrList);
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
                AasCore.Aas3_0.Range => KeyTypes.Range,
                ReferenceElement => KeyTypes.ReferenceElement,
                RelationshipElement => KeyTypes.RelationshipElement,
                AnnotatedRelationshipElement => KeyTypes.AnnotatedRelationshipElement,
                IIdentifiable => KeyTypes.Identifiable,
                IReferable => KeyTypes.Referable,
                Reference => KeyTypes.GlobalReference,
                //TODO (jtikekar, 0000-00-00): what about model reference
                _ => KeyTypes.SubmodelElement,  // default case
            };
        }
    }
}
