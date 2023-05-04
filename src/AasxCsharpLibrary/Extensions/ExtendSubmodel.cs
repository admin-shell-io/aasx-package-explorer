using AdminShellNS;
using AdminShellNS.Display;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendSubmodel
    {
        #region AasxPackageExplorer

        /// <summary>
        /// Recurses on all Submodel elements of a Submodel or SME, which allows children.
        /// The <c>state</c> object will be passed to the lambda function in order to provide
        /// stateful approaches. Include this element, as well. 
        /// </summary>
        /// <param name="state">State object to be provided to lambda. Could be <c>null.</c></param>
        /// <param name="lambda">The lambda function as <c>(state, parents, SME)</c>
        /// The lambda shall return <c>TRUE</c> in order to deep into recursion.</param>
        /// <param name="includeThis">Include this element as well. <c>parents</c> will then 
        /// include this element as well!</param>
        public static void RecurseOnReferables(this Submodel submodel,
            object state, Func<object, List<IReferable>, IReferable, bool> lambda,
            bool includeThis = false)
        {
            var parents = new List<IReferable>();
            if (includeThis)
            {
                lambda(state, null, submodel);
                parents.Add(submodel);
            }
            submodel.SubmodelElements?.RecurseOnReferables(state, parents, lambda);
        }

        public static void Remove(this Submodel submodel, ISubmodelElement submodelElement)
        {
            if (submodel != null)
            {
                if (submodel.SubmodelElements != null)
                {
                    submodel.SubmodelElements.Remove(submodelElement);
                }
            }
        }

        public static object AddChild(this ISubmodel submodel, ISubmodelElement childSubmodelElement, EnumerationPlacmentBase placement = null)
        {
            if (childSubmodelElement == null)
                return null;
            submodel.SubmodelElements ??= new();
            if (childSubmodelElement != null)
                childSubmodelElement.Parent = submodel;
            submodel.SubmodelElements.Add(childSubmodelElement);
            return childSubmodelElement;
        }

        public static Tuple<string, string> ToCaptionInfo(this ISubmodel submodel)
        {
            var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", submodel.IdShort, "<no idShort!>");
            if (submodel.Administration != null)
                caption += "V" + submodel.Administration.Version + "." + submodel.Administration.Revision;
            var info = "";
            if (submodel.Id != null)
                info = $"[{submodel.Id}]";
            return Tuple.Create(caption, info);
        }

        public static IEnumerable<LocatedReference> FindAllReferences(this ISubmodel submodel)
        {
            // not nice: use temp list
            var temp = new List<IReference>();

            // recurse
            submodel.RecurseOnSubmodelElements(null, (state, parents, sme) =>
            {
                if (sme is ReferenceElement re)
                    if (re.Value != null)
                        temp.Add(re.Value);
                if (sme is RelationshipElement rl)
                {
                    if (rl.First != null)
                        temp.Add(rl.First);
                    if (rl.Second != null)
                        temp.Add(rl.Second);
                }
                // recurse
                return true;
            });

            // now, give back
            foreach (var r in temp)
                yield return new LocatedReference(submodel, r);
        }

        #endregion
        public static void Validate(this Submodel submodel, AasValidationRecordList results)
        {
            // access
            if (results == null)
                return;

            // check
            submodel.BaseValidation(results);
            submodel.Kind.Value.Validate(results, submodel);
            submodel.SemanticId.Keys.Validate(results, submodel);
        }
        public static Submodel ConvertFromV10(this Submodel submodel, AasxCompatibilityModels.AdminShellV10.Submodel sourceSubmodel, bool shallowCopy = false)
        {
            if (sourceSubmodel == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourceSubmodel.idShort))
            {
                submodel.IdShort = "";
            }
            else
            {
                submodel.IdShort = sourceSubmodel.idShort;
            }

            if (sourceSubmodel.description != null)
            {
                submodel.Description = ExtensionsUtil.ConvertDescriptionFromV10(sourceSubmodel.description);
            }

            if (sourceSubmodel.administration != null)
            {
                submodel.Administration = new AdministrativeInformation(version: sourceSubmodel.administration.version, revision: sourceSubmodel.administration.revision);
            }

            if (sourceSubmodel.semanticId != null)
            {
                var keyList = new List<IKey>();
                foreach (var refKey in sourceSubmodel.semanticId.Keys)
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
                submodel.SemanticId = new Reference(ReferenceTypes.ExternalReference, keyList);
            }

            if (sourceSubmodel.kind != null)
            {
                if (sourceSubmodel.kind.IsInstance)
                {
                    submodel.Kind = ModellingKind.Instance;
                }
                else
                {
                    submodel.Kind = ModellingKind.Template;
                }
            }

            if (sourceSubmodel.qualifiers != null && sourceSubmodel.qualifiers.Count != 0)
            {
                if (submodel.Qualifiers == null && submodel.Qualifiers.Count != 0)
                {
                    submodel.Qualifiers = new List<IQualifier>();
                }

                foreach (var sourceQualifier in sourceSubmodel.qualifiers)
                {
                    var newQualifier = new Qualifier("", DataTypeDefXsd.String);
                    newQualifier = newQualifier.ConvertFromV10(sourceQualifier);
                    submodel.Qualifiers.Add(newQualifier);
                }
            }

            if (!shallowCopy && sourceSubmodel.submodelElements != null)
            {
                if (submodel.SubmodelElements == null)
                {
                    submodel.SubmodelElements = new List<ISubmodelElement>();
                }

                foreach (var submodelElementWrapper in sourceSubmodel.submodelElements)
                {
                    var sourceSubmodelELement = submodelElementWrapper.submodelElement;
                    ISubmodelElement outputSubmodelElement = null;
                    if (sourceSubmodelELement != null)
                    {
                        outputSubmodelElement = outputSubmodelElement.ConvertFromV10(sourceSubmodelELement, shallowCopy);
                        submodel.SubmodelElements.Add(outputSubmodelElement);
                    }

                }
            }

            return submodel;

        }

        public static Submodel ConvertFromV20(this Submodel sm, AasxCompatibilityModels.AdminShellV20.Submodel srcSM, bool shallowCopy = false)
        {
            if (srcSM == null)
                return null;

            if (string.IsNullOrEmpty(srcSM.idShort))
                sm.IdShort = "";
            else
                sm.IdShort = srcSM.idShort;

            if (srcSM.identification?.id != null)
                sm.Id = srcSM.identification.id;

            if (srcSM.description != null)
                sm.Description = ExtensionsUtil.ConvertDescriptionFromV20(srcSM.description);

            if (srcSM.administration != null)
                sm.Administration = new AdministrativeInformation(
                    version: srcSM.administration.version, revision: srcSM.administration.revision);

            if (srcSM.semanticId != null)
            {
                var keyList = new List<IKey>();
                foreach (var refKey in srcSM.semanticId.Keys)
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
                sm.SemanticId = new Reference(ReferenceTypes.ExternalReference, keyList);
            }

            if (srcSM.kind != null)
            {
                if (srcSM.kind.IsInstance)
                {
                    sm.Kind = ModellingKind.Instance;
                }
                else
                {
                    sm.Kind = ModellingKind.Template;
                }
            }

            if (srcSM.qualifiers != null && srcSM.qualifiers.Count != 0)
            {
                if (sm.Qualifiers == null)
                {
                    sm.Qualifiers = new List<IQualifier>();
                }

                foreach (var sourceQualifier in srcSM.qualifiers)
                {
                    var newQualifier = new Qualifier("", DataTypeDefXsd.String);
                    newQualifier = newQualifier.ConvertFromV20(sourceQualifier);
                    sm.Qualifiers.Add(newQualifier);
                }
            }

            if (!shallowCopy && srcSM.submodelElements != null)
            {
                if (sm.SubmodelElements == null)
                {
                    sm.SubmodelElements = new List<ISubmodelElement>();
                }

                foreach (var submodelElementWrapper in srcSM.submodelElements)
                {
                    var sourceSubmodelELement = submodelElementWrapper.submodelElement;
                    ISubmodelElement outputSubmodelElement = null;
                    if (sourceSubmodelELement != null)
                    {
                        outputSubmodelElement = outputSubmodelElement.ConvertFromV20(sourceSubmodelELement, shallowCopy);
                        sm.SubmodelElements.Add(outputSubmodelElement);
                    }

                }
            }

            // move Qualifiers to Extensions
            sm.MigrateV20QualifiersToExtensions();

            return sm;
        }

        public static T FindFirstIdShortAs<T>(this ISubmodel submodel, string idShort) where T : ISubmodelElement
        {

            var submodelElement = submodel.SubmodelElements.Where(sme => (sme != null) && (sme is T) && sme.IdShort.Equals(idShort, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            return (T)submodelElement;
        }

        public static IEnumerable<T> FindDeep<T>(this ISubmodel submodel)
        {
            if (submodel.SubmodelElements == null || submodel.SubmodelElements.Count == 0)
            {
                yield break;
            }

            foreach (var submodelElement in submodel.SubmodelElements)
            {
                foreach (var x in submodelElement.FindDeep<T>())
                    yield return x;
            }
        }

        public static Reference GetModelReference(this ISubmodel submodel)
        {
            var key = new Key(KeyTypes.Submodel, submodel.Id);
            var outputReference = new Reference(ReferenceTypes.ModelReference, new List<IKey>() { key })
            {
                ReferredSemanticId = submodel.SemanticId
            };

            return outputReference;
        }

        /// <summary>
        ///  If instance, return semanticId as one key.
        ///  If template, return identification as key.
        /// </summary>
        public static Key GetSemanticKey(this Submodel submodel)
        {
            if (submodel.Kind == ModellingKind.Instance)
                return submodel.SemanticId.GetAsExactlyOneKey();
            else
                return new Key(KeyTypes.Submodel, submodel.Id);
        }

        public static List<ISubmodelElement> SmeForWrite(this Submodel submodel)
        {
            if (submodel.SubmodelElements == null)
                submodel.SubmodelElements = new();
            return submodel.SubmodelElements;
        }

        public static void RecurseOnSubmodelElements(this ISubmodel submodel, object state, Func<object, List<IReferable>, ISubmodelElement, bool> lambda)
        {
            submodel.SubmodelElements?.RecurseOnReferables(state, null, (o, par, rf) =>
            {
                if (rf is ISubmodelElement sme)
                    return lambda(o, par, sme);
                else
                    return true;
            });
        }

        public static ISubmodelElement FindSubmodelElementByIdShort(this ISubmodel submodel, string smeIdShort)
        {
            if (submodel.SubmodelElements == null || submodel.SubmodelElements.Count == 0)
            {
                return null;
            }

            var submodelElements = submodel.SubmodelElements.Where(sme => (sme != null) && sme.IdShort.Equals(smeIdShort, StringComparison.OrdinalIgnoreCase));
            if (submodelElements.Any())
            {
                return submodelElements.First();
            }
            else
            {
                return null;
            }
        }

        public static void SetAllParents(this ISubmodel submodel, DateTime timestamp)
        {
            if (submodel.SubmodelElements != null)
                foreach (var sme in submodel.SubmodelElements)
                    SetParentsForSME(submodel, sme, timestamp);
        }

        public static void SetParentsForSME(IReferable parent, ISubmodelElement submodelElement, DateTime timestamp)
        {
            if (submodelElement == null)
                return;

            submodelElement.Parent = parent;
            submodelElement.TimeStamp = timestamp;
            submodelElement.TimeStampCreate = timestamp;

            foreach (var childElement in submodelElement.EnumerateChildren())
            {
                SetParentsForSME(submodelElement, childElement, timestamp);
            }
        }

        public static void SetParentsForSME(IReferable parent, ISubmodelElement submodelElement)
        {
            if (submodelElement == null)
                return;

            submodelElement.Parent = parent;

            foreach (var childElement in submodelElement.EnumerateChildren())
            {
                SetParentsForSME(submodelElement, childElement);
            }
        }

        public static void SetAllParents(this ISubmodel submodel)
        {
            if (submodel.SubmodelElements != null)
                foreach (var sme in submodel.SubmodelElements)
                    SetParentsForSME(submodel, sme);
        }

        public static void Add(this Submodel submodel, ISubmodelElement submodelElement)
        {
            if (submodel.SubmodelElements == null)
            {
                submodel.SubmodelElements = new List<ISubmodelElement>();
            }

            submodelElement.Parent = submodel;
            submodel.SubmodelElements.Add(submodelElement);
        }

        public static void Insert(this ISubmodel submodel, int index, ISubmodelElement submodelElement)
        {
            if (submodel.SubmodelElements == null)
            {
                submodel.SubmodelElements = new List<ISubmodelElement>();
            }

            submodelElement.Parent = submodel;
            submodel.SubmodelElements.Insert(index, submodelElement);
        }

        public static T CreateSMEForCD<T>(
            this Submodel sm,
            ConceptDescription conceptDescription, string category = null, string idShort = null,
            string idxTemplate = null, int maxNum = 999, bool addSme = false, bool isTemplate = false)
                where T : ISubmodelElement
        {
            if (sm.SubmodelElements == null)
                sm.SubmodelElements = new List<ISubmodelElement>();
            return sm.SubmodelElements.CreateSMEForCD<T>(
                conceptDescription, category, idShort, idxTemplate, maxNum, addSme, isTemplate);
        }

    }
}
