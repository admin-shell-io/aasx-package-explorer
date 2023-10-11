/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AasxCompatibilityModels;
using AdminShellNS;
using AdminShellNS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendISubmodelElement
    {
        // constants
        public static Type[] PROP_MLP = new Type[] {
            typeof(MultiLanguageProperty), typeof(Property) };

        #region AasxPackageExplorer

        public static List<T> Copy<T>(this List<T> original)
        {
            var res = new List<T>();
            if (original != null)
                foreach (var o in original)
                    res.Add(o.Copy());
            return res;
        }

        public static object AddChild(this ISubmodelElement submodelElement, ISubmodelElement childSubmodelElement, EnumerationPlacmentBase placement = null)
        {
            if (submodelElement is AnnotatedRelationshipElement annotatedRelationshipElement)
            {
                return annotatedRelationshipElement.AddChild(childSubmodelElement, placement);
            }
            else if (submodelElement is SubmodelElementCollection submodelElementCollection)
            {
                return submodelElementCollection.AddChild(childSubmodelElement, placement);
            }
            else if (submodelElement is SubmodelElementList submodelElementList)
            {
                return submodelElementList.AddChild(childSubmodelElement, placement);
            }
            else if (submodelElement is Operation operation)
            {
                return operation.AddChild(childSubmodelElement, placement);
            }
            else if (submodelElement is Entity entity)
            {
                return entity.AddChild(childSubmodelElement, placement);
            }
            else
                return childSubmodelElement;
        }

        public static List<ISubmodelElement> GetChildsAsList(this ISubmodelElement sme)
        {
            return sme.DescendOnce().Where((x) => x is ISubmodelElement).Cast<ISubmodelElement>().ToList();
        }

        public static Tuple<string, string> ToCaptionInfo(this ISubmodelElement submodelElement)
        {
            var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", submodelElement.IdShort, "<no idShort!>");
            var info = "";
            // TODO (MIHO, 2021-07-08): obvious error .. info should receive semanticId .. but would change 
            // display presentation .. therefore to be checked again
            if (submodelElement.SemanticId != null)
                AdminShellUtil.EvalToNonEmptyString("\u21e8 {0}", submodelElement.SemanticId.ToStringExtended(), "");
            return Tuple.Create(caption, info);
        }

        public static void ValueFromText(this ISubmodelElement submodelElement, string text, string defaultLang = null)
        {
            switch (submodelElement)
            {
                case Property property:
                    {
                        property.ValueFromText(text);
                        break;
                    }
                case MultiLanguageProperty multiLanguageProperty:
                    {
                        multiLanguageProperty.ValueFromText(text, defaultLang);
                        break;
                    }
                default:
                    {
                        throw new Exception("Unhandled submodel element type");
                    }
            }
        }

        #endregion
        public static IEnumerable<IReferable> FindAllParents(this ISubmodelElement submodelElement,
                Predicate<IReferable> p,
                bool includeThis = false, bool includeSubmodel = false,
                bool passOverMiss = false)
        {
            // call for this?
            if (includeThis)
            {
                if (p == null || p.Invoke(submodelElement))
                    yield return submodelElement;
                else
                    if (!passOverMiss)
                    yield break;
            }

            // daisy chain all parents ..
            if (submodelElement.Parent != null)
            {
                if (submodelElement.Parent is ISubmodelElement psme)
                {
                    foreach (var q in psme.FindAllParents(p, includeThis: true,
                        passOverMiss: passOverMiss))
                        yield return q;
                }
                else if (includeSubmodel && submodelElement.Parent is Submodel psm)
                {
                    if (p == null || p.Invoke(psm))
                        yield return submodelElement;
                }
            }
        }

        public static IEnumerable<IReferable> FindAllParentsWithSemanticId(
                this ISubmodelElement submodelElement, IReference semId,
                bool includeThis = false, bool includeSubmodel = false, bool passOverMiss = false)
        {
            return (FindAllParents(submodelElement,
                (rf) => (true == (rf as IHasSemantics)?.SemanticId?.Matches(semId,
                    matchMode: MatchMode.Relaxed)),
                includeThis: includeThis, includeSubmodel: includeSubmodel, passOverMiss: passOverMiss));
        }

        public static string ValueAsText(this ISubmodelElement submodelElement, string defaultLang = null)
        {
            //TODO (??, 0000-00-00): Need to check/test this logic again
            if (submodelElement is Property property)
            {
                return property.ValueAsText();
            }

            if (submodelElement is MultiLanguageProperty multiLanguageProperty)
            {
                return multiLanguageProperty.ValueAsText(defaultLang);
            }

            if (submodelElement is AasCore.Aas3_0.Range range)
            {
                return range.ValueAsText();
            }

            if (submodelElement is File file)
            {
                return file.ValueAsText();
            }

            return "";
        }

        public static IQualifier FindQualifierOfType(this ISubmodelElement submodelElement, string qualifierType)
        {
            if (submodelElement.Qualifiers == null || submodelElement.Qualifiers.Count == 0)
            {
                return null;
            }

            foreach (var qualifier in submodelElement.Qualifiers)
            {
                if (qualifier.Type.Equals(qualifierType, StringComparison.OrdinalIgnoreCase))
                {
                    return qualifier;
                }
            }

            return null;

        }

        public static IReference GetModelReference(this ISubmodelElement sme, bool includeParents = true)
        {
            // this will be the tail of our chain
            var keyList = new List<IKey>();
            var keyType = ExtensionsUtil.GetKeyType(sme);
            var key = new Key(keyType, sme.IdShort);
            keyList.Add(key);

            // keys for Parents will be INSERTED in front, iteratively
            var currentParent = sme.Parent;
            while (includeParents && currentParent != null)
            {
                if (currentParent is IIdentifiable identifiable)
                {
                    var currentParentKey = new Key(ExtensionsUtil.GetKeyType(identifiable), identifiable.Id);
                    keyList.Insert(0, currentParentKey);
                    currentParent = null;
                }
                else if (currentParent is IReferable referable)
                {
                    var currentParentKey = new Key(ExtensionsUtil.GetKeyType(referable), referable.IdShort);
                    keyList.Insert(0, currentParentKey);
                    currentParent = referable.Parent;
                }

            }

            var outputReference = new Reference(ReferenceTypes.ModelReference, keyList);
            outputReference.ReferredSemanticId = sme.SemanticId;
            return outputReference;
        }

        public static IEnumerable<T> FindDeep<T>(this ISubmodelElement submodelElement)
        {
            if (submodelElement is T)
            {
                yield return (T)submodelElement;
            }

            foreach (var x in submodelElement.Descend().OfType<T>())
                yield return x;
        }

        public static ISubmodelElement ConvertFromV10(this ISubmodelElement submodelElement, AdminShellV10.SubmodelElement sourceSubmodelElement, bool shallowCopy = false)
        {
            ISubmodelElement outputSubmodelElement = null;
            if (sourceSubmodelElement != null)
            {
                if (sourceSubmodelElement is AdminShellV10.SubmodelElementCollection collection)
                {
                    var newSmeCollection = new SubmodelElementCollection();
                    outputSubmodelElement = newSmeCollection.ConvertFromV10(collection, shallowCopy);
                }
                else if (sourceSubmodelElement is AdminShellV10.Property sourceProperty)
                {
                    var newProperty = new Property(DataTypeDefXsd.String);
                    outputSubmodelElement = newProperty.ConvertFromV10(sourceProperty);
                }
                else if (sourceSubmodelElement is AdminShellV10.File sourceFile)
                {
                    var newFile = new File("");
                    outputSubmodelElement = newFile.ConvertFromV10(sourceFile);
                }
                else if (sourceSubmodelElement is AdminShellV10.Blob blob)
                {
                    var newBlob = new Blob("");
                    outputSubmodelElement = newBlob.ConvertFromV10(blob);
                }
                else if (sourceSubmodelElement is AdminShellV10.ReferenceElement sourceReferenceElement)
                {
                    outputSubmodelElement = new ReferenceElement();
                }
                else if (sourceSubmodelElement is AdminShellV10.RelationshipElement sourceRelationshipElement)
                {
                    var newFirst = ExtensionsUtil.ConvertReferenceFromV10(sourceRelationshipElement.first, ReferenceTypes.ModelReference);
                    var newSecond = ExtensionsUtil.ConvertReferenceFromV10(sourceRelationshipElement.second, ReferenceTypes.ModelReference);
                    outputSubmodelElement = new RelationshipElement(newFirst, newSecond);
                }

                if (sourceSubmodelElement is AdminShellV10.Operation sourceOperation)
                {
                    var newInputVariables = new List<IOperationVariable>();
                    var newOutputVariables = new List<IOperationVariable>();
                    if (!sourceOperation.valueIn.IsNullOrEmpty())
                    {

                        foreach (var inputVariable in sourceOperation.valueIn)
                        {
                            if (inputVariable.value.submodelElement != null)
                            {
                                ISubmodelElement newSubmodelElement = null;
                                newSubmodelElement = newSubmodelElement.ConvertFromV10(inputVariable.value.submodelElement);
                                var newOpVariable = new OperationVariable(newSubmodelElement);
                                newInputVariables.Add(newOpVariable);
                            }
                        }
                    }
                    if (!sourceOperation.valueOut.IsNullOrEmpty())
                    {
                        foreach (var outputVariable in sourceOperation.valueOut)
                        {
                            if (outputVariable.value.submodelElement != null)
                            {
                                ISubmodelElement newSubmodelElement = null;
                                newSubmodelElement = newSubmodelElement.ConvertFromV10(outputVariable.value.submodelElement);
                                var newOpVariable = new OperationVariable(newSubmodelElement);
                                newOutputVariables.Add(newOpVariable);
                            }
                        }
                    }

                    outputSubmodelElement = new Operation(inputVariables: newInputVariables, outputVariables: newOutputVariables);
                }


                outputSubmodelElement.BasicConversionFromV10(sourceSubmodelElement);
            }

            return outputSubmodelElement;
        }

        private static void BasicConversionFromV10(this ISubmodelElement submodelElement, AdminShellV10.SubmodelElement sourceSubmodelElement)
        {
            if (!string.IsNullOrEmpty(sourceSubmodelElement.idShort))
            {
                submodelElement.IdShort = sourceSubmodelElement.idShort;
            }

            if (!string.IsNullOrEmpty(sourceSubmodelElement.category))
            {
                submodelElement.Category = sourceSubmodelElement.category;
            }

            if (sourceSubmodelElement.description != null)
            {
                submodelElement.Description = ExtensionsUtil.ConvertDescriptionFromV10(sourceSubmodelElement.description);
            }

            if (sourceSubmodelElement.semanticId != null && !sourceSubmodelElement.semanticId.IsEmpty)
            {
                var keyList = new List<IKey>();
                foreach (var refKey in sourceSubmodelElement.semanticId.Keys)
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
                submodelElement.SemanticId = new Reference(ReferenceTypes.ExternalReference, keyList);
            }

            if (sourceSubmodelElement.kind != null)
            {
                //SubmodelElement does not have kind anymore
            }

            if (!sourceSubmodelElement.qualifiers.IsNullOrEmpty())
            {
                if (submodelElement.Qualifiers == null && submodelElement.Qualifiers.Count != 0)
                {
                    submodelElement.Qualifiers = new List<IQualifier>();
                }

                foreach (var sourceQualifier in sourceSubmodelElement.qualifiers)
                {
                    var newQualifier = new Qualifier("", DataTypeDefXsd.String);
                    newQualifier = newQualifier.ConvertFromV10(sourceQualifier);
                    submodelElement.Qualifiers.Add(newQualifier);
                }
            }

            if (sourceSubmodelElement.hasDataSpecification != null)
            {
                //TODO (jtikekar, 0000-00-00): EmbeddedDataSpecification?? (as per old implementation)
                if (submodelElement.EmbeddedDataSpecifications == null)
                {
                    submodelElement.EmbeddedDataSpecifications = new List<IEmbeddedDataSpecification>();
                }
                foreach (var dataSpecification in sourceSubmodelElement.hasDataSpecification.reference)
                {
                    if (!dataSpecification.IsEmpty)
                    {
                        submodelElement.EmbeddedDataSpecifications.Add(
                                        new EmbeddedDataSpecification(
                                            ExtensionsUtil.ConvertReferenceFromV10(dataSpecification, ReferenceTypes.ExternalReference),
                                            null));
                    }
                }
            }
        }

        public static ISubmodelElement ConvertFromV20(this ISubmodelElement submodelElement, AdminShellV20.SubmodelElement sourceSubmodelElement, bool shallowCopy = false)
        {
            ISubmodelElement outputSubmodelElement = null;
            if (sourceSubmodelElement != null)
            {
                if (sourceSubmodelElement is AdminShellV20.SubmodelElementCollection collection)
                {
                    var newSmeCollection = new SubmodelElementCollection();
                    outputSubmodelElement = newSmeCollection.ConvertFromV20(collection, shallowCopy);
                }
                else if (sourceSubmodelElement is AdminShellV20.Property sourceProperty)
                {
                    var newProperty = new Property(DataTypeDefXsd.String);
                    outputSubmodelElement = newProperty.ConvertFromV20(sourceProperty);
                }
                else if (sourceSubmodelElement is AdminShellV20.MultiLanguageProperty sourceMultiLangProp)
                {
                    var newMultiLangProperty = new MultiLanguageProperty();
                    outputSubmodelElement = newMultiLangProperty.ConvertFromV20(sourceMultiLangProp);
                }
                else if (sourceSubmodelElement is AdminShellV20.Range sourceRange)
                {
                    var newRange = new AasCore.Aas3_0.Range(DataTypeDefXsd.String);
                    outputSubmodelElement = newRange.ConvertFromV20(sourceRange);
                }
                else if (sourceSubmodelElement is AdminShellV20.File sourceFile)
                {
                    var newFile = new File("");
                    outputSubmodelElement = newFile.ConvertFromV20(sourceFile);
                }
                else if (sourceSubmodelElement is AdminShellV20.Blob blob)
                {
                    var newBlob = new Blob("");
                    outputSubmodelElement = newBlob.ConvertFromV20(blob);
                }
                else if (sourceSubmodelElement is AdminShellV20.ReferenceElement sourceReferenceElement)
                {
                    var newReference = ExtensionsUtil.ConvertReferenceFromV20(sourceReferenceElement.value, ReferenceTypes.ModelReference);
                    outputSubmodelElement = new ReferenceElement(value: newReference);
                }
                else if (sourceSubmodelElement is AdminShellV20.AnnotatedRelationshipElement sourceAnnotedRelationshipElement)
                {
                    var newFirst = ExtensionsUtil.ConvertReferenceFromV20(sourceAnnotedRelationshipElement.first, ReferenceTypes.ModelReference);
                    var newSecond = ExtensionsUtil.ConvertReferenceFromV20(sourceAnnotedRelationshipElement.second, ReferenceTypes.ModelReference);
                    var newAnnotedRelElement = new AnnotatedRelationshipElement(newFirst, newSecond);
                    outputSubmodelElement = newAnnotedRelElement.ConvertAnnotationsFromV20(sourceAnnotedRelationshipElement);
                }
                else if (sourceSubmodelElement is AdminShellV20.RelationshipElement sourceRelationshipElement)
                {
                    var newFirst = ExtensionsUtil.ConvertReferenceFromV20(sourceRelationshipElement.first, ReferenceTypes.ModelReference);
                    var newSecond = ExtensionsUtil.ConvertReferenceFromV20(sourceRelationshipElement.second, ReferenceTypes.ModelReference);
                    outputSubmodelElement = new RelationshipElement(newFirst, newSecond);
                }
                else if (sourceSubmodelElement is AdminShellV20.BasicEvent sourceBasicEvent)
                {
                    var newObserved = ExtensionsUtil.ConvertReferenceFromV20(sourceBasicEvent.observed, ReferenceTypes.ModelReference);

                    outputSubmodelElement = new BasicEventElement(newObserved, Direction.Input, StateOfEvent.Off);
                    //TODO (jtikekar, 0000-00-00): default values of enums
                }
                else if (sourceSubmodelElement is AdminShellV20.Entity sourceEntity)
                {
                    var entityType = Stringification.EntityTypeFromString(sourceEntity.entityType);
                    var newEntity = new Entity(entityType ?? EntityType.CoManagedEntity);
                    outputSubmodelElement = newEntity.ConvertFromV20(sourceEntity);
                }
                else if (sourceSubmodelElement is AdminShellV20.Operation sourceOperation)
                {
                    var newInputVariables = new List<IOperationVariable>();
                    var newOutputVariables = new List<IOperationVariable>();
                    var newInOutVariables = new List<IOperationVariable>();
                    if (!sourceOperation.inputVariable.IsNullOrEmpty())
                    {

                        foreach (var inputVariable in sourceOperation.inputVariable)
                        {
                            if (inputVariable.value.submodelElement != null)
                            {
                                ISubmodelElement newSubmodelElement = null;
                                newSubmodelElement = newSubmodelElement.ConvertFromV20(inputVariable.value.submodelElement);
                                var newOpVariable = new OperationVariable(newSubmodelElement);
                                newInputVariables.Add(newOpVariable);
                            }
                        }
                    }
                    if (!sourceOperation.outputVariable.IsNullOrEmpty())
                    {
                        foreach (var outputVariable in sourceOperation.outputVariable)
                        {
                            if (outputVariable.value.submodelElement != null)
                            {
                                ISubmodelElement newSubmodelElement = null;
                                newSubmodelElement = newSubmodelElement.ConvertFromV20(outputVariable.value.submodelElement);
                                var newOpVariable = new OperationVariable(newSubmodelElement);
                                newOutputVariables.Add(newOpVariable);
                            }
                        }
                    }

                    if (!sourceOperation.inoutputVariable.IsNullOrEmpty())
                    {
                        foreach (var inOutVariable in sourceOperation.inoutputVariable)
                        {
                            if (inOutVariable.value.submodelElement != null)
                            {
                                ISubmodelElement newSubmodelElement = null;
                                newSubmodelElement = newSubmodelElement.ConvertFromV20(inOutVariable.value.submodelElement);
                                var newOpVariable = new OperationVariable(newSubmodelElement);
                                newInOutVariables.Add(newOpVariable);
                            }
                        }
                    }

                    outputSubmodelElement = new Operation(inputVariables: newInputVariables, outputVariables: newOutputVariables, inoutputVariables: newInOutVariables);
                }

                outputSubmodelElement.BasicConversionFromV20(sourceSubmodelElement);
            }

            return outputSubmodelElement;
        }

        private static void BasicConversionFromV20(this ISubmodelElement submodelElement, AdminShellV20.SubmodelElement sourceSubmodelElement)
        {
            if (!string.IsNullOrEmpty(sourceSubmodelElement.idShort))
                submodelElement.IdShort = sourceSubmodelElement.idShort;

            if (!string.IsNullOrEmpty(sourceSubmodelElement.category))
                submodelElement.Category = sourceSubmodelElement.category;

            if (sourceSubmodelElement.description != null)
                submodelElement.Description = ExtensionsUtil.ConvertDescriptionFromV20(sourceSubmodelElement.description);

            if (sourceSubmodelElement.semanticId != null && !sourceSubmodelElement.semanticId.IsEmpty)
            {
                var keyList = new List<IKey>();
                foreach (var refKey in sourceSubmodelElement.semanticId.Keys)
                {
                    var keyType = Stringification.KeyTypesFromString(refKey.type);
                    if (keyType != null)
                    {
                        // DECISION: After phone call with Birgit, set all CD to GlobalReference
                        // assuming it is always a external concept
                        if (keyType == KeyTypes.ConceptDescription)
                            keyType = KeyTypes.GlobalReference;

                        keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {refKey.type} not found.");
                    }
                }
                submodelElement.SemanticId = new Reference(ReferenceTypes.ExternalReference, keyList);
            }


            if (!sourceSubmodelElement.qualifiers.IsNullOrEmpty())
            {
                if (submodelElement.Qualifiers == null || submodelElement.Qualifiers.Count == 0)
                    submodelElement.Qualifiers = new List<IQualifier>();

                foreach (var sourceQualifier in sourceSubmodelElement.qualifiers)
                {
                    var newQualifier = new Qualifier("", DataTypeDefXsd.String);
                    newQualifier = newQualifier.ConvertFromV20(sourceQualifier);
                    submodelElement.Qualifiers.Add(newQualifier);
                }
            }

            if (sourceSubmodelElement.hasDataSpecification != null)
            {
                //TODO (jtikekar, 0000-00-00): EmbeddedDataSpecification?? (as per old implementation)
                if (submodelElement.EmbeddedDataSpecifications == null)
                    submodelElement.EmbeddedDataSpecifications = new List<IEmbeddedDataSpecification>();

                //TODO (jtikekar, 0000-00-00): DataSpecificationContent?? (as per old implementation)
                foreach (var sourceDataSpec in sourceSubmodelElement.hasDataSpecification)
                {
                    submodelElement.EmbeddedDataSpecifications.Add(
                        new EmbeddedDataSpecification(
                            ExtensionsUtil.ConvertReferenceFromV20(sourceDataSpec.dataSpecification, ReferenceTypes.ExternalReference),
                            null));
                }
            }

            // move Qualifiers to Extensions
            submodelElement.MigrateV20QualifiersToExtensions();
        }

        #region List<ISubmodelElement>

        public static IReferable FindReferableByReference(
                this List<ISubmodelElement> submodelElements, Reference rf, int keyIndex)
        {
            return FindReferableByReference(submodelElements, rf?.Keys, keyIndex);
        }

        public static IReferable FindReferableByReference(
            this List<ISubmodelElement> submodelElements, List<IKey> keys, int keyIndex)
        {
            // first index needs to exist ..
            if (submodelElements == null || keys == null || keyIndex >= keys.Count)
                return null;


            // over all wrappers
            foreach (var smw in submodelElements)
                if (smw != null && smw.IdShort.Equals(keys[keyIndex].Value, StringComparison.OrdinalIgnoreCase))
                {
                    // match on this level. Did we find a leaf element?
                    if ((keyIndex + 1) >= keys.Count)
                        return smw;

                    // dive into SMC?
                    if (smw is SubmodelElementCollection smc)
                    {
                        var found = FindReferableByReference(smc.Value, keys, keyIndex + 1);
                        if (found != null)
                            return found;
                    }
                    // dive into SML?
                    if (smw is SubmodelElementList submodelElementList)
                    {
                        var found = FindReferableByReference(submodelElementList.Value, keys, keyIndex + 1);
                        if (found != null)
                            return found;
                    }

                    // dive into AnnotedRelationshipElement?
                    if (smw is AnnotatedRelationshipElement annotatedRelationshipElement)
                    {
                        var annotations = new List<ISubmodelElement>(annotatedRelationshipElement.Annotations);
                        var found = FindReferableByReference(annotations, keys, keyIndex + 1);
                        if (found != null)
                            return found;
                    }

                    // dive into Entity statements?
                    if (smw is Entity ent)
                    {
                        var found = FindReferableByReference(ent.Statements, keys, keyIndex + 1);
                        if (found != null)
                            return found;
                    }

                    // else:
                    return null;
                }

            // no?
            return null;
        }

        public static IEnumerable<T> FindDeep<T>(this IEnumerable<ISubmodelElement> submodelElements, Predicate<T> match = null) where T : ISubmodelElement
        {
            foreach (var smw in submodelElements)
            {
                var current = smw;
                if (current == null)
                    continue;

                // call lambda for this element
                if (current is T)
                    if (match == null || match.Invoke((T)current))
                        yield return (T)current;

                // dive into?
                // TODO (MIHO, 2020-07-31): would be nice to use IEnumerateChildren for this ..
                // TODO (MIHO, 2023-01-01): would be nice to use AasCore.DescendOnce() for this ..
#if __old__
                if (current is SubmodelElementCollection smc && smc.Value != null)
                    foreach (var x in smc.Value.FindDeep<T>(match))
                        yield return x;

                if (current is AnnotatedRelationshipElement are && are.Annotations != null)
                {
                    var annotationsList = new List<ISubmodelElement>(are.Annotations);
                    foreach (var x in annotationsList.FindDeep<T>(match))
                        yield return x;
                }

                if (current is Entity ent && ent.Statements != null)
                    foreach (var x in ent.Statements.FindDeep<T>(match))
                        yield return x;

                if (current is Operation op)
                {
                    var operationVariables = new List<ISubmodelElement>();
                    foreach (var opVariable in op.InputVariables)
                    {
                        operationVariables.Add(opVariable.Value);
                    }

                    foreach (var opVariable in op.InoutputVariables)
                    {
                        operationVariables.Add(opVariable.Value);
                    }

                    foreach (var opVariable in op.OutputVariables)
                    {
                        operationVariables.Add(opVariable.Value);
                    }

                    foreach (var x in operationVariables.FindDeep<T>(match))
                        yield return x;
                }
#else
                var smeChilds = current.DescendOnce().Where((ic) => ic is ISubmodelElement)
                        .Cast<ISubmodelElement>();
                foreach (var x in smeChilds.FindDeep<T>(match))
                    yield return x;
#endif
            }
        }

        public static void CopyManySMEbyCopy<T>(this List<ISubmodelElement> submodelElements, ConceptDescription destCD,
                List<ISubmodelElement> sourceSmc, ConceptDescription sourceCD,
                bool createDefault = false, Action<T> setDefault = null,
                MatchMode matchMode = MatchMode.Relaxed) where T : ISubmodelElement
        {
            submodelElements.CopyManySMEbyCopy(destCD.GetSingleKey(), sourceSmc, sourceCD.GetSingleKey(),
                createDefault ? destCD : null, setDefault, matchMode);
        }

        public static void CopyManySMEbyCopy<T>(this List<ISubmodelElement> submodelElements, Key destSemanticId,
                List<ISubmodelElement> sourceSmc, Key sourceSemanticId,
                ConceptDescription createDefault = null, Action<T> setDefault = null,
                MatchMode matchMode = MatchMode.Relaxed) where T : ISubmodelElement
        {
            // bool find possible sources
            bool foundSrc = false;
            if (sourceSmc == null)
                return;
            foreach (var src in sourceSmc.FindAllSemanticIdAs<T>(sourceSemanticId, matchMode))
            {
                // type of found src?
                AasSubmodelElements aeSrc = (AasSubmodelElements)Enum.Parse(typeof(AasSubmodelElements), src.GetType().Name);

                // ok?
                if (src == null || aeSrc == AasSubmodelElements.SubmodelElement)
                    continue;
                foundSrc = true;

                // ok, create new one
                var dst = AdminShellNS.AdminShellUtil.CreateSubmodelElementFromEnum(aeSrc, src);
                if (dst != null)
                {
                    // make same things sure
                    dst.IdShort = src.IdShort;
                    dst.Category = src.Category;
                    dst.SemanticId = new Reference(ReferenceTypes.ModelReference, new List<IKey>() { destSemanticId });

                    // instantanously add it?
                    submodelElements.Add(dst);
                }
            }

            // default?
            if (createDefault != null && !foundSrc)
            {
                // ok, default
                var dflt = submodelElements.CreateSMEForCD<T>(createDefault, addSme: true);

                // set default?
                setDefault?.Invoke(dflt);
            }
        }

        public static T CopyOneSMEbyCopy<T>(this List<ISubmodelElement> submodelElements, ConceptDescription destCD,
                List<ISubmodelElement> sourceSmc, Key[] sourceKeys,
                bool createDefault = false, Action<T> setDefault = null,
                MatchMode matchMode = MatchMode.Relaxed,
                string idShort = null, bool addSme = false) where T : ISubmodelElement
        {
            return submodelElements.CopyOneSMEbyCopy<T>(destCD?.GetSingleKey(), sourceSmc, sourceKeys,
                createDefault ? destCD : null, setDefault, matchMode, idShort, addSme);
        }

        public static T CopyOneSMEbyCopy<T>(this List<ISubmodelElement> submodelELements, ConceptDescription destCD,
                List<ISubmodelElement> sourceSmc, ConceptDescription sourceCD,
                bool createDefault = false, Action<T> setDefault = null,
                MatchMode matchMode = MatchMode.Relaxed,
                string idShort = null, bool addSme = false) where T : ISubmodelElement
        {
            return submodelELements.CopyOneSMEbyCopy<T>(destCD?.GetSingleKey(), sourceSmc, new[] { sourceCD?.GetSingleKey() },
                createDefault ? destCD : null, setDefault, matchMode, idShort, addSme);
        }

        public static T CopyOneSMEbyCopy<T>(this List<ISubmodelElement> submodelElements, Key destSemanticId,
                List<ISubmodelElement> sourceSmc, Key[] sourceSemanticId,
                ConceptDescription createDefault = null, Action<T> setDefault = null,
                MatchMode matchMode = MatchMode.Relaxed,
                string idShort = null, bool addSme = false) where T : ISubmodelElement
        {
            // get source
            var src = sourceSmc.FindFirstAnySemanticIdAs<T>(sourceSemanticId, matchMode);

            // may be make an adaptive conversion
            if (src == null)
            {
                var anySrc = sourceSmc?.FindFirstAnySemanticId(sourceSemanticId, matchMode: matchMode);
                src = submodelElements.AdaptiveConvertTo<T>(anySrc, createDefault,
                            idShort: idShort, addSme: false);
            }

            // proceed
            AasSubmodelElements aeSrc = (AasSubmodelElements)Enum.Parse(typeof(AasSubmodelElements), src?.GetType().Name);
            if (src == null || aeSrc == AasSubmodelElements.SubmodelElement)
            {
                // create a default?
                if (createDefault == null)
                    return default(T);

                // ok, default
                var dflt = submodelElements.CreateSMEForCD<T>(createDefault, idShort: idShort, addSme: addSme);

                // set default?
                setDefault?.Invoke(dflt);

                // return 
                return dflt;
            }

            // ok, create new one
            var dst = AdminShellNS.AdminShellUtil.CreateSubmodelElementFromEnum(aeSrc, src);
            if (dst == null)
                return default(T);

            // make same things sure
            dst.IdShort = src.IdShort;
            dst.Category = src.Category;
            dst.SemanticId = new Reference(ReferenceTypes.ModelReference, new List<IKey>() { destSemanticId });

            // instantanously add it?
            if (addSme)
                submodelElements.Add(dst);

            // give back
            return (T)dst;
        }

        public static T AdaptiveConvertTo<T>(this List<ISubmodelElement> submodelElements,
                ISubmodelElement anySrc,
                ConceptDescription createDefault = null,
                string idShort = null, bool addSme = false) where T : ISubmodelElement
        {
            if (typeof(T) == typeof(MultiLanguageProperty)
                    && anySrc is Property srcProp)
            {
                var res = submodelElements.CreateSMEForCD<T>(createDefault, idShort: idShort, addSme: addSme);
                if (res is MultiLanguageProperty mlp)
                {
                    mlp.Value = new List<ILangStringTextType>() {
                        new LangStringTextType(AdminShellUtil.GetDefaultLngIso639(), srcProp.Value) };
                    mlp.ValueId = srcProp.ValueId;
                    return res;
                }
            }

            if (typeof(T) == typeof(Property)
                    && anySrc is MultiLanguageProperty srcMlp)
            {
                var res = submodelElements.CreateSMEForCD<T>(createDefault, idShort: idShort, addSme: addSme);
                if (res is Property prp)
                {
                    prp.Value = "" + srcMlp.Value?.GetDefaultString();
                    prp.ValueId = srcMlp.ValueId;
                    return res;
                }
            }

            return default(T);
        }

        public static IEnumerable<ISubmodelElement> FindAllIdShort(this List<ISubmodelElement> submodelElements,
            string idShort)
        {
            foreach (var smw in submodelElements)
                if (smw != null)
                    if (smw.IdShort.Trim().ToLower() == idShort.Trim().ToLower())
                        yield return smw;
        }

        public static IEnumerable<T> FindAllIdShortAs<T>(this List<ISubmodelElement> submodelElements,
            string idShort) where T : class, ISubmodelElement
        {
            foreach (var smw in submodelElements)
                if (smw is T)
                    if (smw.IdShort.Trim().ToLower() == idShort.Trim().ToLower())
                        yield return smw as T;
        }

        public static ISubmodelElement FindFirstIdShort(this List<ISubmodelElement> submodelElements,
            string idShort)
        {
            return submodelElements.FindAllIdShort(idShort)?.FirstOrDefault<ISubmodelElement>();
        }

        public static T FindFirstIdShortAs<T>(this List<ISubmodelElement> submodelElements,
            string idShort) where T : class, ISubmodelElement
        {
            return submodelElements.FindAllIdShortAs<T>(idShort)?.FirstOrDefault<T>();
        }


        public static ISubmodelElement FindFirstAnySemanticId(this List<ISubmodelElement> submodelElements,
                Key[] semId, Type[] allowedTypes = null, MatchMode matchMode = MatchMode.Strict)
        {
            if (semId == null)
                return null;
            foreach (var si in semId)
            {
                var found = submodelElements.FindAllSemanticId(si, allowedTypes, matchMode)?
                            .FirstOrDefault<ISubmodelElement>();
                if (found != null)
                    return found;
            }
            return null;
        }

        public static T FindFirstAnySemanticIdAs<T>(
            this List<ISubmodelElement> submodelElements, IKey[] semId, MatchMode matchMode = MatchMode.Strict)
                where T : ISubmodelElement
        {
            if (semId == null)
                return default(T);
            foreach (var si in semId)
            {
                var found = submodelElements.FindAllSemanticIdAs<T>(si, matchMode).FirstOrDefault<T>();
                if (found != null)
                    return found;
            }
            return default(T);
        }

        public static T CreateNew<T>(
            string idShort = null, string category = null, IReference semanticId = null)
                where T : ISubmodelElement, new()
        {
            var res = new T();
            if (idShort != null)
                res.IdShort = idShort;
            if (category != null)
                res.Category = category;
            if (semanticId != null)
                res.SemanticId = semanticId.Copy();
            return res;
        }

        public static T CreateSMEForCD<T>(this List<ISubmodelElement> submodelELements, IConceptDescription conceptDescription, string category = null, string idShort = null,
                string idxTemplate = null, int maxNum = 999, bool addSme = false, bool isTemplate = false)
                where T : ISubmodelElement
        {
            // access
            if (conceptDescription == null)
                return default(T);

            // fin type enum
            var smeType = AdminShellUtil.AasSubmodelElementsFrom<T>();
            if (!smeType.HasValue)
                return default(T);

            // try to potentially figure out idShort
            var ids = conceptDescription.IdShort;

            //TODO (jtikekar, 0000-00-00): Temporarily removed
            if ((ids == null || ids.Trim() == "") && conceptDescription.GetIEC61360() != null)
                ids = conceptDescription.GetIEC61360().ShortName?
                    .GetDefaultString();

            if (idShort != null)
                ids = idShort;

            if (ids == null)
                return default(T);

            // unique?
            if (idxTemplate != null)
                ids = submodelELements.IterateIdShortTemplateToBeUnique(idxTemplate, maxNum);

            // make a new instance
            var semanticId = conceptDescription.GetCdReference();
            ISubmodelElement sme = AdminShellUtil.CreateSubmodelElementFromEnum(smeType.Value);
            if (sme == null)
                return default(T);
            sme.IdShort = ids;
            sme.SemanticId = semanticId.Copy();
            if (category != null)
                sme.Category = category;

            // if its a SMC, make sure its accessible
            if (sme is SubmodelElementCollection smc)
                smc.Value = new List<ISubmodelElement>();

            // instantanously add it?
            if (addSme)
                submodelELements.Add(sme);

            // give back
            return (T)sme;
        }

        public static IEnumerable<T> FindAllSemanticIdAs<T>(this List<ISubmodelElement> submodelELements,
            IKey semId, MatchMode matchMode = MatchMode.Strict)
                where T : ISubmodelElement
        {
            foreach (var submodelElement in submodelELements)
                if (submodelElement != null && submodelElement is T
                    && submodelElement.SemanticId != null)
                    if (submodelElement.SemanticId.MatchesExactlyOneKey(semId, matchMode))
                        yield return (T)submodelElement;
        }

        public static IEnumerable<T> FindAllSemanticIdAs<T>(this List<ISubmodelElement> submodelELements,
            IReference semId, MatchMode matchMode = MatchMode.Strict)
        where T : ISubmodelElement
        {
            foreach (var submodelElement in submodelELements)
                if (submodelElement != null && submodelElement is T
                    && submodelElement.SemanticId != null)
                    if (submodelElement.SemanticId.Matches(semId, matchMode))
                        yield return (T)submodelElement;
        }

        public static T FindFirstSemanticIdAs<T>(this List<ISubmodelElement> submodelElements,
            IKey semId, MatchMode matchMode = MatchMode.Strict)
            where T : ISubmodelElement
        {
            return submodelElements.FindAllSemanticIdAs<T>(semId, matchMode).FirstOrDefault<T>();
        }

        public static T FindFirstSemanticIdAs<T>(this List<ISubmodelElement> submodelElements,
            IReference semId, MatchMode matchMode = MatchMode.Strict)
            where T : ISubmodelElement
        {
            return submodelElements.FindAllSemanticIdAs<T>(semId, matchMode).FirstOrDefault<T>();
        }

        public static List<ISubmodelElement> GetChildListFromFirstSemanticId(
            this List<ISubmodelElement> submodelElements,
            IKey semKey, MatchMode matchMode = MatchMode.Strict)
        {
            return FindFirstSemanticIdAs<ISubmodelElement>(submodelElements, semKey, matchMode)?.GetChildsAsList();
        }

        public static List<ISubmodelElement> GetChildListFromFirstSemanticId(
            this List<ISubmodelElement> submodelElements,
            IReference semId, MatchMode matchMode = MatchMode.Strict)
        {
            return FindFirstSemanticIdAs<ISubmodelElement>(submodelElements, semId, matchMode)?.GetChildsAsList();
        }

        public static IEnumerable<List<ISubmodelElement>> GetChildListsFromAllSemanticId(
            this List<ISubmodelElement> submodelElements,
            IKey semKey, MatchMode matchMode = MatchMode.Strict)
        {
            foreach (var child in FindAllSemanticIdAs<ISubmodelElement>(submodelElements, semKey, matchMode))
                yield return child.GetChildsAsList()?.ToList();
        }

        public static IEnumerable<List<ISubmodelElement>> GetChildListsFromAllSemanticId(
            this List<ISubmodelElement> submodelElements,
            IReference semId, MatchMode matchMode = MatchMode.Strict)
        {
            foreach (var child in FindAllSemanticIdAs<ISubmodelElement>(submodelElements, semId, matchMode))
                yield return child.GetChildsAsList()?.ToList();
        }

        public static IEnumerable<ISubmodelElement> Join(params IEnumerable<ISubmodelElement>[] lists)
        {
            if (lists == null || lists.Length < 1)
                yield break;
            foreach (var l in lists)
                foreach (var sme in l)
                    yield return sme;
        }

        public static void RecurseOnReferables(
            this List<ISubmodelElement> submodelElements, object state, List<IReferable> parents,
                Func<object, List<IReferable>, IReferable, bool> lambda)
        {
            if (lambda == null)
                return;
            if (parents == null)
                parents = new List<IReferable>();

            // over all elements
            foreach (var submodelElement in submodelElements)
            {
                var current = submodelElement;
                if (current == null)
                    continue;

                // call lambda for this element
                // AND decide, if to recurse!
                var goDeeper = lambda(state, parents, current);

                if (goDeeper)
                {
                    // add to parents
                    parents.Add(current);

                    // dive into?
                    if (current is SubmodelElementCollection smc)
                        smc.Value?.RecurseOnReferables(state, parents, lambda);

                    if (current is Entity ent)
                        ent.Statements?.RecurseOnReferables(state, parents, lambda);

                    if (current is Operation operation)
                    {
                        SubmodelElementCollection opVariableCollection = new SubmodelElementCollection();
                        opVariableCollection.Value = new List<ISubmodelElement>();
                        foreach (var inputVariable in operation.InputVariables)
                        {
                            opVariableCollection.Value.Add(inputVariable.Value);
                        }

                        foreach (var outputVariable in operation.OutputVariables)
                        {
                            opVariableCollection.Value.Add(outputVariable.Value);
                        }

                        foreach (var inOutVariable in operation.InoutputVariables)
                        {
                            opVariableCollection.Value.Add(inOutVariable.Value);
                        }

                        opVariableCollection.Value.RecurseOnReferables(state, parents, lambda);
                    }

                    if (current is AnnotatedRelationshipElement annotatedRelationshipElement)
                    {
                        var annotationElements = new List<ISubmodelElement>();
                        if (annotatedRelationshipElement.Annotations != null)
                            foreach (var annotation in annotatedRelationshipElement.Annotations)
                            {
                                annotationElements.Add(annotation);
                            }
                        annotationElements.RecurseOnReferables(state, parents, lambda);
                    }

                    // remove from parents
                    parents.RemoveAt(parents.Count - 1);
                }
            }
        }

        public static void RecurseOnSubmodelElements(
            this List<ISubmodelElement> submodelElements, object state,
            List<ISubmodelElement> parents, Action<object, List<ISubmodelElement>, ISubmodelElement> lambda)
        {
            // trivial
            if (lambda == null)
                return;
            if (parents == null)
                parents = new List<ISubmodelElement>();

            // over all elements
            foreach (var smw in submodelElements)
            {
                var current = smw;
                if (current == null)
                    continue;

                // call lambda for this element
                lambda(state, parents, current);

                // add to parents
                parents.Add(current);

                // dive into?
                if (current is SubmodelElementCollection smc)
                    smc.Value?.RecurseOnSubmodelElements(state, parents, lambda);

                if (current is Entity ent)
                    ent.Statements?.RecurseOnSubmodelElements(state, parents, lambda);

                if (current is Operation operation)
                {
                    SubmodelElementCollection opVariableCollection = new SubmodelElementCollection();
                    foreach (var inputVariable in operation.InputVariables)
                    {
                        opVariableCollection.Value.Add(inputVariable.Value);
                    }

                    foreach (var outputVariable in operation.OutputVariables)
                    {
                        opVariableCollection.Value.Add(outputVariable.Value);
                    }

                    foreach (var inOutVariable in operation.InoutputVariables)
                    {
                        opVariableCollection.Value.Add(inOutVariable.Value);
                    }

                    opVariableCollection.Value.RecurseOnSubmodelElements(state, parents, lambda);
                }

                if (current is AnnotatedRelationshipElement annotatedRelationshipElement)
                {
                    var annotationElements = new List<ISubmodelElement>();
                    foreach (var annotation in annotatedRelationshipElement.Annotations)
                    {
                        annotationElements.Add(annotation);
                    }
                    annotationElements.RecurseOnSubmodelElements(state, parents, lambda);
                }

                // remove from parents
                parents.RemoveAt(parents.Count - 1);
            }
        }

        public static IEnumerable<T> FindAllSemanticIdAs<T>(
            this List<ISubmodelElement> submodelELements, string semanticId) where T : ISubmodelElement
        {
            foreach (var submodelElement in submodelELements)
            {
                if (submodelElement != null && submodelElement is T && submodelElement.SemanticId != null)
                {
                    if (submodelElement.SemanticId.Matches(semanticId))
                    {
                        yield return (T)submodelElement;
                    }
                }
            }
        }

        public static T FindFirstSemanticIdAs<T>(
            this List<ISubmodelElement> submodelELements, string semanticId) where T : ISubmodelElement
        {
            return submodelELements.FindAllSemanticIdAs<T>(semanticId).FirstOrDefault();
        }

        public static T FindFirstAnySemanticIdAs<T>(
            this List<ISubmodelElement> submodelELements, string[] semanticIds) where T : ISubmodelElement
        {
            if (semanticIds == null)
                return default;
            foreach (var semanticId in semanticIds)
            {
                var found = submodelELements.FindFirstSemanticIdAs<T>(semanticId);
                if (found != null)
                    return found;
            }
            return default;
        }

        public static IEnumerable<T> FindAllSemanticId<T>(
            this List<ISubmodelElement> smes,
            string[] allowedSemanticIds,
            bool invertedAllowed = false) where T : ISubmodelElement
        {
            if (allowedSemanticIds == null || allowedSemanticIds.Length < 1)
                yield break;

            foreach (var sme in smes)
            {
                if (sme == null || !(sme is T))
                    continue;

                if (sme.SemanticId == null || sme.SemanticId.Keys.Count < 1)
                {
                    if (invertedAllowed)
                        yield return (T)sme;
                    continue;
                }

                var found = false;
                foreach (var semanticId in allowedSemanticIds)
                    if (sme.SemanticId.Matches(semanticId))
                    {
                        found = true;
                        break;
                    }

                if (invertedAllowed)
                    found = !found;

                if (found)
                    yield return (T)sme;
            }
        }

        public static T FindFirstAnySemanticId<T>(
            this List<ISubmodelElement> submodelElements, string[] allowedSemanticIds,
            bool invertAllowed = false) where T : ISubmodelElement
        {
            return submodelElements.FindAllSemanticId<T>(allowedSemanticIds, invertAllowed).FirstOrDefault();
        }

        public static IEnumerable<T> FindAllSemanticId<T>(
            this List<ISubmodelElement> smes,
            IKey[] allowedSemanticIds, MatchMode mm = MatchMode.Strict,
            bool invertedAllowed = false) where T : ISubmodelElement
        {
            if (allowedSemanticIds == null || allowedSemanticIds.Length < 1)
                yield break;

            foreach (var sme in smes)
            {
                if (sme == null || !(sme is T))
                    continue;

                if (sme.SemanticId == null || sme.SemanticId.Keys.Count < 1)
                {
                    if (invertedAllowed)
                        yield return (T)sme;
                    continue;
                }

                var found = false;
                foreach (var semanticId in allowedSemanticIds)
                    if (sme.SemanticId.MatchesExactlyOneKey(semanticId, mm))
                    {
                        found = true;
                        break;
                    }

                if (invertedAllowed)
                    found = !found;

                if (found)
                    yield return (T)sme;
            }
        }

        public static T FindFirstAnySemanticId<T>(
            this List<ISubmodelElement> submodelElements,
            IKey[] allowedSemanticIds, MatchMode mm = MatchMode.Strict,
            bool invertAllowed = false) where T : ISubmodelElement
        {
            return submodelElements.FindAllSemanticId<T>(allowedSemanticIds, mm, invertAllowed).FirstOrDefault();
        }

        public static IEnumerable<ISubmodelElement> FindAllSemanticId(
            this List<ISubmodelElement> submodelElements, IKey semId,
            Type[] allowedTypes = null,
            MatchMode matchMode = MatchMode.Strict)
        {
            foreach (var smw in submodelElements)
                if (smw != null && smw.SemanticId != null)
                {
                    if (smw == null)
                        continue;

                    if (allowedTypes != null)
                    {
                        var smwt = smw.GetType();
                        if (!allowedTypes.Contains(smwt))
                            continue;
                    }

                    if (smw.SemanticId.MatchesExactlyOneKey(semId, matchMode))
                        yield return smw;
                }
        }

        public static ISubmodelElement FindFirstSemanticId(
            this List<ISubmodelElement> submodelElements,
            IKey semId, Type[] allowedTypes = null, MatchMode matchMode = MatchMode.Strict)
        {
            return submodelElements.FindAllSemanticId(semId, allowedTypes, matchMode)?.FirstOrDefault<ISubmodelElement>();
        }

        public static IEnumerable<T> FindAllSemanticIdAs<T>(
            this List<ISubmodelElement> smes,
            ConceptDescription cd, MatchMode matchMode = MatchMode.Strict)
                where T : ISubmodelElement
        {
            foreach (var x in FindAllSemanticIdAs<T>(smes, cd.GetReference(), matchMode))
                yield return x;
        }

        public static T FindFirstSemanticIdAs<T>(
            this List<ISubmodelElement> smes,
            ConceptDescription cd, MatchMode matchMode = MatchMode.Strict)
                where T : ISubmodelElement
        {
            return smes.FindAllSemanticIdAs<T>(cd, matchMode).FirstOrDefault<T>();
        }

        public static string IterateIdShortTemplateToBeUnique(this List<ISubmodelElement> submodelElements, string idShortTemplate, int maxNum)
        {
            if (idShortTemplate == null || maxNum < 1 || !idShortTemplate.Contains("{0"))
                return null;

            int i = 1;
            while (i < maxNum)
            {
                var ids = string.Format(idShortTemplate, i);
                if (submodelElements.CheckIdShortIsUnique(ids))
                    return ids;
                i++;
            }

            return null;
        }

        /// <summary>
        /// Returns false, if there is another element with same idShort in the list
        /// </summary>
        public static bool CheckIdShortIsUnique(this List<ISubmodelElement> submodelElements, string idShort)
        {
            idShort = idShort?.Trim();
            if (idShort == null || idShort.Length < 1)
                return false;

            var res = true;
            foreach (var smw in submodelElements)
                if (smw != null && smw.IdShort != null && smw.IdShort == idShort)
                {
                    res = false;
                    break;
                }

            return res;
        }

        #endregion

        public static ISubmodelElement UpdateFrom(this ISubmodelElement elem, ISubmodelElement source)
        {
            if (source == null)
                return elem;

            // IReferable
            elem.Category = source.Category;
            elem.IdShort = source.IdShort;
            elem.DisplayName = source.DisplayName?.Copy();
            elem.Description = source.Description?.Copy();


            // IHasSemantics
            if (source.SemanticId != null)
                elem.SemanticId = source.SemanticId.Copy();
            if (source.SupplementalSemanticIds != null)
                elem.SupplementalSemanticIds = source.SupplementalSemanticIds.Copy();

            // IQualifiable
            if (source.Qualifiers != null)
                elem.Qualifiers = source.Qualifiers.Copy();

            // IHasDataSpecification
            if (source.EmbeddedDataSpecifications != null)
                elem.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications.Copy();

            return elem;
        }

        //
        // Factories
        //

        private static readonly Dictionary<AasSubmodelElements, string> AasSubmodelElementsToAbbrev = (
            new Dictionary<AasSubmodelElements, string>()
            {
                { AasSubmodelElements.AnnotatedRelationshipElement, "RelA" },
                { AasSubmodelElements.BasicEventElement, "BEvt" },
                { AasSubmodelElements.Blob, "Blob" },
                { AasSubmodelElements.Capability, "Cap" },
                { AasSubmodelElements.DataElement, "DE" },
                { AasSubmodelElements.Entity, "Ent" },
                { AasSubmodelElements.EventElement, "Evt" },
                { AasSubmodelElements.File, "File" },
                { AasSubmodelElements.MultiLanguageProperty, "MLP" },
                { AasSubmodelElements.Operation, "Opr" },
                { AasSubmodelElements.Property, "Prop" },
                { AasSubmodelElements.Range, "Range" },
                { AasSubmodelElements.ReferenceElement, "Ref" },
                { AasSubmodelElements.RelationshipElement, "Rel" },
                { AasSubmodelElements.SubmodelElement, "SME" },
                { AasSubmodelElements.SubmodelElementList, "SML" },
                { AasSubmodelElements.SubmodelElementCollection, "SMC" }
            });

        /// <summary>
        /// Retrieve the string abbreviation of <paramref name="that" />.
        /// </summary>
        /// <remarks>
        /// If <paramref name="that" /> is not a valid literal, return <c>null</c>.
        /// </remarks>
        public static string? ToString(AasSubmodelElements? that)
        {
            if (!that.HasValue)
            {
                return null;
            }
            else
            {
                if (AasSubmodelElementsToAbbrev.TryGetValue(that.Value, out string? value))
                {
                    return value;
                }
                else
                {
                    return null;
                }
            }
        }

        private static readonly Dictionary<string, AasSubmodelElements> _aasSubmodelElementsFromAbbrev = (
            new Dictionary<string, AasSubmodelElements>()
            {
                { "RelA", AasSubmodelElements.AnnotatedRelationshipElement },
                { "BEvt", AasSubmodelElements.BasicEventElement },
                { "Blob", AasSubmodelElements.Blob },
                { "Cap", AasSubmodelElements.Capability },
                { "DE", AasSubmodelElements.DataElement },
                { "Ent", AasSubmodelElements.Entity },
                { "Evt", AasSubmodelElements.EventElement },
                { "File", AasSubmodelElements.File },
                { "MLP", AasSubmodelElements.MultiLanguageProperty },
                { "Opr", AasSubmodelElements.Operation },
                { "Prop", AasSubmodelElements.Property },
                { "Range", AasSubmodelElements.Range },
                { "Ref", AasSubmodelElements.ReferenceElement },
                { "Rel", AasSubmodelElements.RelationshipElement },
                { "SME", AasSubmodelElements.SubmodelElement },
                { "SML", AasSubmodelElements.SubmodelElementList },
                { "SMC", AasSubmodelElements.SubmodelElementCollection }
            });

        /// <summary>
        /// Parse the string abbreviation of <see cref="AasSubmodelElements" />.
        /// </summary>
        /// <remarks>
        /// If <paramref name="text" /> is not a valid string representation
        /// of a literal of <see cref="AasSubmodelElements" />,
        /// return <c>null</c>.
        /// </remarks>
        public static AasSubmodelElements? AasSubmodelElementsFromAbbrev(string text)
        {
            if (_aasSubmodelElementsFromAbbrev.TryGetValue(text, out AasSubmodelElements value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Parse the string representation or the abbreviation of <see cref="AasSubmodelElements" />.
        /// </summary>
        /// <remarks>
        /// If <paramref name="text" /> is not a valid string representation
        /// of a literal of <see cref="AasSubmodelElements" />,
        /// return <c>null</c>.
        /// </remarks>
        public static AasSubmodelElements? AasSubmodelElementsFromStringOrAbbrev(string text)
        {
            var res = Stringification.AasSubmodelElementsFromString(text);
            if (res.HasValue)
                return res;

            return AasSubmodelElementsFromAbbrev(text);
        }

    }
}
