/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AdminShellNS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Extensions
{
    public static class ExtendEnvironment
    {
        #region Environment

        #region AasxPackageExplorer

        public static void RecurseOnReferables(this AasCore.Aas3_0.Environment environment,
                object state, Func<object, List<IReferable>, IReferable, bool> lambda, bool includeThis = false)
        {
            // includeThis does not make sense, as no Referable
            // just use the others
            foreach (var idf in environment.FindAllReferable(onlyIdentifiables: true))
                idf?.RecurseOnReferables(state, lambda, includeThis);
        }

        #endregion

        /// <summary>
        /// Deprecated? Not compatible with AAS core?
        /// </summary>
        public static AasValidationRecordList ValidateAll(this AasCore.Aas3_0.Environment environment)
        {
            // collect results
            var results = new AasValidationRecordList();

            // all entities
            foreach (var rf in environment.FindAllReferable())
                rf.Validate(results);

            // give back
            return results;
        }

        /// <summary>
        /// Deprecated? Not compatible with AAS core?
        /// </summary>
        public static int AutoFix(this AasCore.Aas3_0.Environment environment, IEnumerable<AasValidationRecord> records)
        {
            // access
            if (records == null)
                return -1;

            // collect Referables (expensive safety measure)
            var allowedReferables = environment.FindAllReferable().ToList();

            // go thru records
            int res = 0;
            foreach (var rec in records)
            {
                // access 
                if (rec == null || rec.Fix == null || rec.Source == null)
                    continue;

                // minimal safety measure
                if (!allowedReferables.Contains(rec.Source))
                    continue;

                // apply fix
                res++;
                try
                {
                    rec.Fix.Invoke();
                }
                catch
                {
                    res--;
                }
            }

            // return number of applied fixes
            return res;
        }

        /// <summary>
        /// This function tries to silently fix some issues preventing the environment
        /// are parts of it to be properly serilaized.
        /// </summary>
        /// <returns>Number of fixes taken</returns>
        public static int SilentFix30(this AasCore.Aas3_0.Environment env)
        {
            // access
            int res = 0;
            if (env == null)
                return res;

            // AAS core crashes without AssetInformation
            if (env.AssetAdministrationShells != null)
                foreach (var aas in env.AssetAdministrationShells)
                    if (aas.AssetInformation == null)
                    {
                        aas.AssetInformation = new AssetInformation(assetKind: AssetKind.NotApplicable);
                        res++;
                    }

            // AAS core crashes without EmbeddedDataSpecification.DataSpecificationContent
            // AAS core crashes without EmbeddedDataSpecification.DataSpecificationContent.PreferredName
            foreach (var rf in env.FindAllReferable())
                if (rf is IHasDataSpecification hds)
                    if (hds.EmbeddedDataSpecifications != null)
                        foreach (var eds in hds.EmbeddedDataSpecifications)
                        {
                            if (eds.DataSpecificationContent == null)
                                eds.DataSpecificationContent =
                                    new DataSpecificationIec61360(
                                        new List<ILangStringPreferredNameTypeIec61360>());
                        }

            // ok
            return res;
        }

        public static IEnumerable<IReferable> FindAllReferable(this AasCore.Aas3_0.Environment environment, bool onlyIdentifiables = false)
        {
            if (environment.AssetAdministrationShells != null)
                foreach (var aas in environment.AssetAdministrationShells)
                    if (aas != null)
                    {
                        // AAS itself
                        yield return aas;
                    }

            if (environment.Submodels != null)
                foreach (var sm in environment.Submodels)
                    if (sm != null)
                    {
                        yield return sm;

                        if (!onlyIdentifiables)
                        {
                            // TODO (MIHO, 2020-08-26): not very elegant, yet. Avoid temporary collection
                            var allsme = new List<ISubmodelElement>();
                            sm.RecurseOnSubmodelElements(null, (state, parents, sme) =>
                            {
                                allsme.Add(sme); return true;
                            });
                            foreach (var sme in allsme)
                                yield return sme;
                        }
                    }

            if (environment.ConceptDescriptions != null)
                foreach (var cd in environment.ConceptDescriptions)
                    if (cd != null)
                        yield return cd;
        }

#if !DoNotUseAasxCompatibilityModels

        public static AasCore.Aas3_0.Environment ConvertFromV10(this AasCore.Aas3_0.Environment environment, AasxCompatibilityModels.AdminShellV10.AdministrationShellEnv sourceEnvironement)
        {
            //Convert Administration Shells
            if (sourceEnvironement.AdministrationShells != null)
            {
                if (environment.AssetAdministrationShells == null)
                {
                    environment.AssetAdministrationShells = new List<IAssetAdministrationShell>();
                }
                foreach (var sourceAas in sourceEnvironement.AdministrationShells)
                {
                    var sourceAsset = sourceEnvironement?.FindAsset(sourceAas.assetRef);
                    if (sourceAsset != null)
                    {
                        var newAssetInformation = new AssetInformation(AssetKind.Instance);
                        newAssetInformation = newAssetInformation.ConvertFromV10(sourceAsset);

                        var newAas = new AssetAdministrationShell(id: sourceAas.identification.id, newAssetInformation);
                        newAas = newAas.ConvertFromV10(sourceAas);

                        environment.AssetAdministrationShells.Add(newAas);
                    }

                }
            }

            //Convert Submodels
            if (sourceEnvironement.Submodels != null)
            {
                if (environment.Submodels == null)
                {
                    environment.Submodels = new List<ISubmodel>();
                }
                foreach (var sourceSubmodel in sourceEnvironement.Submodels)
                {
                    var newSubmodel = new Submodel(sourceSubmodel.identification.id);
                    newSubmodel = newSubmodel.ConvertFromV10(sourceSubmodel);
                    environment.Submodels.Add(newSubmodel);
                }
            }

            if (sourceEnvironement.ConceptDescriptions != null)
            {
                if (environment.ConceptDescriptions == null)
                {
                    environment.ConceptDescriptions = new List<IConceptDescription>();
                }
                foreach (var sourceConceptDescription in sourceEnvironement.ConceptDescriptions)
                {
                    var newConceptDescription = new ConceptDescription(sourceConceptDescription.identification.id);
                    newConceptDescription = newConceptDescription.ConvertFromV10(sourceConceptDescription);
                    environment.ConceptDescriptions.Add(newConceptDescription);
                }
            }

            return environment;
        }


        public static AasCore.Aas3_0.Environment ConvertFromV20(this AasCore.Aas3_0.Environment environment, AasxCompatibilityModels.AdminShellV20.AdministrationShellEnv sourceEnvironement)
        {
            //Convert Administration Shells
            if (sourceEnvironement.AdministrationShells != null)
            {
                if (environment.AssetAdministrationShells == null)
                {
                    environment.AssetAdministrationShells = new List<IAssetAdministrationShell>();
                }
                foreach (var sourceAas in sourceEnvironement.AdministrationShells)
                {
                    // first make the AAS
                    var newAas = new AssetAdministrationShell(id: sourceAas.identification.id, null);
                    newAas = newAas.ConvertFromV20(sourceAas);
                    environment.AssetAdministrationShells.Add(newAas);

                    var sourceAsset = sourceEnvironement?.FindAsset(sourceAas.assetRef);
                    if (sourceAsset != null)
                    {
                        var newAssetInformation = new AssetInformation(AssetKind.Instance);
                        newAssetInformation = newAssetInformation.ConvertFromV20(sourceAsset);
                        newAas.AssetInformation = newAssetInformation;
                    }

                }
            }

            //Convert Submodels
            if (sourceEnvironement.Submodels != null)
            {
                if (environment.Submodels == null)
                {
                    environment.Submodels = new List<ISubmodel>();
                }
                foreach (var sourceSubmodel in sourceEnvironement.Submodels)
                {
                    var newSubmodel = new Submodel(sourceSubmodel.identification.id);
                    newSubmodel = newSubmodel.ConvertFromV20(sourceSubmodel);
                    environment.Submodels.Add(newSubmodel);
                }
            }

            if (sourceEnvironement.ConceptDescriptions != null)
            {
                if (environment.ConceptDescriptions == null)
                {
                    environment.ConceptDescriptions = new List<IConceptDescription>();
                }
                foreach (var sourceConceptDescription in sourceEnvironement.ConceptDescriptions)
                {
                    var newConceptDescription = new ConceptDescription(sourceConceptDescription.identification.id);
                    newConceptDescription = newConceptDescription.ConvertFromV20(sourceConceptDescription);
                    environment.ConceptDescriptions.Add(newConceptDescription);
                }
            }

            return environment;
        }

#endif

        //TODO (jtikekar, 0000-00-00): to test
        public static AasCore.Aas3_0.Environment CreateFromExistingEnvironment(this AasCore.Aas3_0.Environment environment,
            AasCore.Aas3_0.Environment sourceEnvironment, List<IAssetAdministrationShell> filterForAas = null, List<AssetInformation> filterForAssets = null, List<ISubmodel> filterForSubmodel = null,
            List<IConceptDescription> filterForConceptDescriptions = null)
        {
            if (filterForAas == null)
            {
                filterForAas = new List<IAssetAdministrationShell>();
            }

            if (filterForAssets == null)
            {
                filterForAssets = new List<AssetInformation>();
            }

            if (filterForSubmodel == null)
            {
                filterForSubmodel = new List<ISubmodel>();
            }

            if (filterForConceptDescriptions == null)
            {
                filterForConceptDescriptions = new List<IConceptDescription>();
            }

            //Copy AssetAdministrationShells
            foreach (var aas in sourceEnvironment.AssetAdministrationShells)
            {
                if (filterForAas.Contains(aas))
                {
                    environment.AssetAdministrationShells.Add(aas);

                    if (aas.Submodels != null && aas.Submodels.Count > 0)
                    {
                        foreach (var submodelReference in aas.Submodels)
                        {
                            var submodel = sourceEnvironment.FindSubmodel(submodelReference);
                            if (submodel != null)
                            {
                                filterForSubmodel.Add(submodel);
                            }
                        }
                    }
                }
            }

            //Copy Submodel
            foreach (var submodel in sourceEnvironment.Submodels)
            {
                if (filterForSubmodel.Contains(submodel))
                {
                    environment.Submodels.Add(submodel);

                    //Find Used CDs
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, submodel.SubmodelElements, ref filterForConceptDescriptions);
                }
            }

            //Copy ConceptDescription
            foreach (var conceptDescription in sourceEnvironment.ConceptDescriptions)
            {
                if (filterForConceptDescriptions.Contains(conceptDescription))
                {
                    environment.ConceptDescriptions.Add(conceptDescription);
                }
            }

            return environment;

        }

        public static void CreateFromExistingEnvRecurseForCDs(this AasCore.Aas3_0.Environment environment, AasCore.Aas3_0.Environment sourceEnvironment,
            List<ISubmodelElement> submodelElements, ref List<IConceptDescription> filterForConceptDescription)
        {
            if (submodelElements == null || submodelElements.Count == 0 || filterForConceptDescription == null || filterForConceptDescription.Count == 0)
            {
                return;
            }

            foreach (var submodelElement in submodelElements)
            {
                if (submodelElement == null)
                {
                    return;
                }

                if (submodelElement.SemanticId != null)
                {
                    var conceptDescription = sourceEnvironment.FindConceptDescriptionByReference(submodelElement.SemanticId);
                    if (conceptDescription != null)
                    {
                        filterForConceptDescription.Add(conceptDescription);
                    }
                }

                if (submodelElement is SubmodelElementCollection smeColl)
                {
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, smeColl.Value, ref filterForConceptDescription);
                }

                if (submodelElement is SubmodelElementList smeList)
                {
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, smeList.Value, ref filterForConceptDescription);
                }

                if (submodelElement is Entity entity)
                {
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, entity.Statements, ref filterForConceptDescription);
                }

                if (submodelElement is AnnotatedRelationshipElement annotatedRelationshipElement)
                {
                    var annotedELements = new List<ISubmodelElement>();
                    foreach (var annotation in annotatedRelationshipElement.Annotations)
                    {
                        annotedELements.Add(annotation);
                    }
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, annotedELements, ref filterForConceptDescription);
                }

                if (submodelElement is Operation operation)
                {
                    var operationELements = new List<ISubmodelElement>();
                    foreach (var inputVariable in operation.InputVariables)
                    {
                        operationELements.Add(inputVariable.Value);
                    }

                    foreach (var outputVariable in operation.OutputVariables)
                    {
                        operationELements.Add(outputVariable.Value);
                    }

                    foreach (var inOutVariable in operation.InoutputVariables)
                    {
                        operationELements.Add(inOutVariable.Value);
                    }

                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, operationELements, ref filterForConceptDescription);

                }
            }
        }

        public static ConceptDescription Add(this AasCore.Aas3_0.Environment env, ConceptDescription cd)
        {
            if (cd == null)
                return null;
            if (env.ConceptDescriptions == null)
                env.ConceptDescriptions = new();
            env.ConceptDescriptions.Add(cd);
            return cd;
        }

        public static Submodel Add(this AasCore.Aas3_0.Environment env, Submodel sm)
        {
            if (sm == null)
                return null;
            if (env.Submodels == null)
                env.Submodels = new();
            env.Submodels.Add(sm);
            return sm;
        }

        public static AssetAdministrationShell Add(this AasCore.Aas3_0.Environment env, AssetAdministrationShell aas)
        {
            if (aas == null)
                return null;
            if (env.AssetAdministrationShells == null)
                env.AssetAdministrationShells = new();
            env.AssetAdministrationShells.Add(aas);
            return aas;
        }

        public static JsonWriter SerialiazeJsonToStream(this AasCore.Aas3_0.Environment environment, StreamWriter streamWriter, bool leaveJsonWriterOpen = false)
        {
            streamWriter.AutoFlush = true;

            JsonSerializer serializer = new JsonSerializer()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Formatting = Newtonsoft.Json.Formatting.Indented
            };

            JsonWriter writer = new JsonTextWriter(streamWriter);
            serializer.Serialize(writer, environment);
            if (leaveJsonWriterOpen)
                return writer;
            writer.Close();
            return null;
        }

        #endregion

        #region Submodel Queries

        public static IEnumerable<ISubmodel> FindAllSubmodelGroupedByAAS(this AasCore.Aas3_0.Environment environment, Func<IAssetAdministrationShell, ISubmodel, bool> p = null)
        {
            if (environment.AssetAdministrationShells == null || environment.Submodels == null)
                yield break;
            foreach (var aas in environment.AssetAdministrationShells)
            {
                if (aas?.Submodels == null)
                    continue;
                foreach (var smref in aas.Submodels)
                {
                    var sm = environment.FindSubmodel(smref);
                    if (sm != null && (p == null || p(aas, sm)))
                        yield return sm;
                }
            }
        }
        public static ISubmodel FindSubmodel(this AasCore.Aas3_0.Environment environment, IReference submodelReference)
        {
            if (environment == null || submodelReference == null)
            {
                return null;
            }

            if (submodelReference.Keys.Count != 1) // Can have only one reference key
            {
                return null;
            }

            var key = submodelReference.Keys[0];
            if (key.Type != KeyTypes.Submodel)
            {
                return null;
            }

            var submodels = environment.Submodels.Where(s => s.Id.Equals(key.Value, StringComparison.OrdinalIgnoreCase));
            if (submodels.Any())
            {
                return submodels.First();
            }

            return null;
        }

        public static ISubmodel FindSubmodelById(this AasCore.Aas3_0.Environment environment, string submodelId)
        {
            if (string.IsNullOrEmpty(submodelId))
            {
                return null;
            }

            var submodels = environment.Submodels.Where(s => s.Id.Equals(submodelId));
            if (submodels.Any())
            {
                return submodels.First();
            }

            return null;
        }

        public static IEnumerable<ISubmodel> FindAllSubmodelsGroupedByAAS(this AasCore.Aas3_0.Environment environment, Func<IAssetAdministrationShell, ISubmodel, bool> p = null)
        {
            if (environment.AssetAdministrationShells == null || environment.Submodels == null)
                yield break;
            foreach (var aas in environment.AssetAdministrationShells)
            {
                if (aas?.Submodels == null)
                    continue;
                foreach (var submodelReference in aas.Submodels)
                {
                    var submodel = environment.FindSubmodel(submodelReference);
                    if (submodel != null && (p == null || p(aas, submodel)))
                        yield return submodel;
                }
            }
        }

        public static IEnumerable<ISubmodel> FindAllSubmodelBySemanticId(this AasCore.Aas3_0.Environment environment, string semanticId)
        {
            if (semanticId == null)
                yield break;

            foreach (var submodel in environment.Submodels)
                if (true == submodel.SemanticId?.Matches(semanticId))
                    yield return submodel;
        }

        #endregion

        #region AssetAdministrationShell Queries
        public static IAssetAdministrationShell FindAasWithSubmodelId(this AasCore.Aas3_0.Environment environment, string submodelId)
        {
            if (submodelId == null)
            {
                return null;
            }

            var aas = environment.AssetAdministrationShells.Where(a => (a.Submodels?.Where(s => s.Matches(submodelId)).FirstOrDefault()) != null).FirstOrDefault();

            return aas;
        }

        public static IAssetAdministrationShell FindAasById(this AasCore.Aas3_0.Environment environment, string aasId)
        {
            if (string.IsNullOrEmpty(aasId))
            {
                return null;
            }

            var aas = environment.AssetAdministrationShells.Where(a => a.Id.Equals(aasId)).First();

            return aas;
        }

        #endregion

        #region ConceptDescription Queries

        public static IConceptDescription FindConceptDescriptionById(
            this AasCore.Aas3_0.Environment env, string cdId)
        {
            if (string.IsNullOrEmpty(cdId))
                return null;

            var conceptDescription = env.ConceptDescriptions.Where(c => c.Id.Equals(cdId)).FirstOrDefault();
            return conceptDescription;
        }

        public static IConceptDescription FindConceptDescriptionByReference(
            this AasCore.Aas3_0.Environment env, IReference rf)
        {
            if (rf == null)
                return null;

            return env.FindConceptDescriptionById(rf.GetAsIdentifier());
        }

        #endregion

        #region Referable Queries

        /// <summary>
        /// Result of FindReferable in Environment
        /// </summary>
        public class ReferableRootInfo
        {
            public AssetAdministrationShell AAS = null;
            public AssetInformation Asset = null;
            public Submodel Submodel = null;
            public ConceptDescription CD = null;

            public int NrOfRootKeys = 0;

            public bool IsValid
            {
                get
                {
                    return NrOfRootKeys > 0 && (AAS != null || Submodel != null || Asset != null);
                }
            }
        }

        //TODO (jtikekar, 0000-00-00): Need to test
        public static IReferable FindReferableByReference(
            this AasCore.Aas3_0.Environment environment,
            IReference reference,
            int keyIndex = 0,
            IEnumerable<ISubmodelElement> submodelElems = null,
            ReferableRootInfo rootInfo = null)
        {
            // access
            var keyList = reference?.Keys;
            if (keyList == null || keyList.Count == 0 || keyIndex >= keyList.Count)
                return null;

            // shortcuts
            var firstKeyType = keyList[keyIndex].Type;
            var firstKeyId = keyList[keyIndex].Value;

            // different pathes
            switch (firstKeyType)
            {
                case KeyTypes.AssetAdministrationShell:
                    {
                        var aas = environment.FindAasById(firstKeyId);

                        // side info?
                        if (rootInfo != null)
                        {
                            rootInfo.AAS = aas as AssetAdministrationShell;
                            rootInfo.NrOfRootKeys = 1 + keyIndex;
                        }

                        //Not found or already at the end of our search
                        if (aas == null || keyIndex >= keyList.Count - 1)
                        {
                            return aas;
                        }

                        return environment.FindReferableByReference(reference, ++keyIndex);
                    }
                // dead-csharp off
                // TODO (MIHO, 2023-01-01): stupid generalization :-(
                case KeyTypes.GlobalReference:
                case KeyTypes.ConceptDescription:
                    {
                        // In meta model V3, multiple important things might by identified
                        // by a flat GlobalReference :-(

                        // find an Asset by that id?

                        var keyedAas = environment.FindAasWithAssetInformation(firstKeyId);
                        if (keyedAas?.AssetInformation != null)
                        {
                            // found an Asset

                            // side info?
                            if (rootInfo != null)
                            {
                                rootInfo.AAS = keyedAas as AssetAdministrationShell;
                                rootInfo.Asset = (AssetInformation)(keyedAas?.AssetInformation);
                                rootInfo.NrOfRootKeys = 1 + keyIndex;
                            }

                            // give back the AAS
                            return keyedAas;
                        }

                        // Concept?Description
                        var keyedCd = environment.FindConceptDescriptionById(firstKeyId);
                        if (keyedCd != null)
                        {
                            // side info?
                            if (rootInfo != null)
                            {
                                rootInfo.CD = keyedCd as ConceptDescription;
                                rootInfo.NrOfRootKeys = 1 + keyIndex;
                            }

                            // give back the CD
                            return keyedCd;
                        }

                        // Nope
                        return null;
                    }
                // dead-csharp on
                case KeyTypes.Submodel:
                    {
                        var submodel = environment.FindSubmodelById(firstKeyId);
                        // No?
                        if (submodel == null)
                            return null;

                        // notice in side info
                        if (rootInfo != null)
                        {
                            rootInfo.Submodel = submodel as Submodel;
                            rootInfo.NrOfRootKeys = 1 + keyIndex;

                            // add even more info
                            if (rootInfo.AAS == null)
                            {
                                foreach (var aas2 in environment.AssetAdministrationShells)
                                {
                                    var smref2 = environment.FindSubmodelById(submodel.Id);
                                    if (smref2 != null)
                                    {
                                        rootInfo.AAS = (AssetAdministrationShell)aas2;
                                        break;
                                    }
                                }
                            }
                        }

                        // at the end of the journey?
                        if (keyIndex >= keyList.Count - 1)
                            return submodel;

                        return environment.FindReferableByReference(reference, ++keyIndex, submodel.SubmodelElements);
                    }
            }

            if (firstKeyType.IsSME() && submodelElems != null)
            {
                var submodelElement = submodelElems.Where(
                    sme => sme.IdShort.Equals(keyList[keyIndex].Value,
                        StringComparison.OrdinalIgnoreCase)).First();

                //This is required element
                if (keyIndex + 1 >= keyList.Count)
                {
                    return submodelElement;
                }

                //Recurse again
                if (submodelElement?.EnumeratesChildren() == true)
                    return environment.FindReferableByReference(reference, ++keyIndex, submodelElement.EnumerateChildren());
            }

            //Nothing in this environment
            return null;
        }

        #endregion

        #region AasxPackageExplorer

        public static IEnumerable<T> FindAllSubmodelElements<T>(this AasCore.Aas3_0.Environment environment,
                Predicate<T> match = null, AssetAdministrationShell onlyForAAS = null) where T : ISubmodelElement
        {
            // more or less two different schemes
            if (onlyForAAS != null)
            {
                if (onlyForAAS.Submodels == null)
                    yield break;
                foreach (var smr in onlyForAAS.Submodels)
                {
                    var sm = environment.FindSubmodel(smr);
                    if (sm?.SubmodelElements != null)
                        foreach (var x in sm.SubmodelElements.FindDeep<T>(match))
                            yield return x;
                }
            }
            else
            {
                if (environment.Submodels != null)
                    foreach (var sm in environment.Submodels)
                        if (sm?.SubmodelElements != null)
                            foreach (var x in sm.SubmodelElements.FindDeep<T>(match))
                                yield return x;
            }
        }

        public static IEnumerable<LocatedReference> FindAllReferences(this AasCore.Aas3_0.Environment environment)
        {
            if (environment.AssetAdministrationShells != null)
                foreach (var aas in environment.AssetAdministrationShells)
                    if (aas != null)
                        foreach (var r in aas.FindAllReferences())
                            yield return r;

            if (environment.Submodels != null)
                foreach (var sm in environment.Submodels)
                    if (sm != null)
                        foreach (var r in sm.FindAllReferences())
                            yield return r;

            if (environment.ConceptDescriptions != null)
                foreach (var cd in environment.ConceptDescriptions)
                    if (cd != null)
                        foreach (var r in cd.FindAllReferences())
                            yield return new LocatedReference(cd, r);
        }

        /// <summary>
        /// Tries renaming an Identifiable, specifically: the identification of an Identifiable and
        /// all references to it.
        /// Currently supported: ConceptDescriptions
        /// Returns a list of Referables, which were changed or <c>null</c> in case of error
        /// </summary>
        public static List<IReferable> RenameIdentifiable<T>(this AasCore.Aas3_0.Environment environment, string oldId, string newId)
            where T : IClass
        {
            // access
            if (oldId == null || newId == null || oldId.Equals(newId))
                return null;

            var res = new List<IReferable>();

            if (typeof(T) == typeof(ConceptDescription))
            {
                // check, if exist or not exist
                var cdOld = environment.FindConceptDescriptionById(oldId);
                if (cdOld == null || environment.FindConceptDescriptionById(newId) != null)
                    return null;

                // rename old cd
                cdOld.Id = newId;
                res.Add(cdOld);

                // search all SMEs referring to this CD
                foreach (var sme in environment.FindAllSubmodelElements<ISubmodelElement>(match: (s) =>
                {
                    return (s != null && s.SemanticId != null && s.SemanticId.Matches(oldId));
                }))
                {
                    sme.SemanticId.Keys[0].Value = newId;
                    res.Add(sme);
                }

                // seems fine
                return res;
            }
            else
            if (typeof(T) == typeof(Submodel))
            {
                // check, if exist or not exist
                var smOld = environment.FindSubmodelById(oldId);
                if (smOld == null || environment.FindSubmodelById(newId) != null)
                    return null;

                // recurse all possible Referenes in the aas env
                foreach (var lr in environment.FindAllReferences())
                {
                    var r = lr?.Reference;
                    if (r != null)
                        for (int i = 0; i < r.Keys.Count; i++)
                            if (r.Keys[i].Matches(KeyTypes.Submodel, oldId, MatchMode.Relaxed))
                            {
                                // directly replace
                                r.Keys[i].Value = newId;
                                if (res.Contains(lr.Identifiable))
                                    res.Add(lr.Identifiable);
                            }
                }

                // rename old Submodel
                smOld.Id = newId;

                // seems fine
                return res;
            }
            else
            if (typeof(T) == typeof(AssetAdministrationShell))
            {
                // check, if exist or not exist
                var aasOld = environment.FindAasById(oldId);
                if (aasOld == null || environment.FindAasById(newId) != null)
                    return null;

                // recurse? -> no?

                // rename old Asset
                aasOld.Id = newId;

                // seems fine
                return res;
            }
            else
            //TODO (jtikekar, 0000-00-00): support asset
            if (typeof(T) == typeof(AssetInformation))
            {
                // check, if exist or not exist
                var assetOld = environment.FindAasWithAssetInformation(oldId);
                if (assetOld == null || environment.FindAasWithAssetInformation(newId) != null)
                    return null;

                // recurse all possible Referenes in the aas env
                foreach (var lr in environment.FindAllReferences())
                {
                    var r = lr?.Reference;
                    if (r != null)
                        for (int i = 0; i < r.Keys.Count; i++)
                            if (r.Keys[i].Matches(KeyTypes.GlobalReference, oldId))
                            {
                                // directly replace
                                r.Keys[i].Value = newId;
                                if (res.Contains(lr.Identifiable))
                                    res.Add(lr.Identifiable);
                            }
                }

                // rename old Asset
                assetOld.AssetInformation.GlobalAssetId = newId;

                // seems fine
                return res;
            }

            // no result is false, as well
            return null;
        }

        public static IAssetAdministrationShell FindAasWithAssetInformation(this AasCore.Aas3_0.Environment environment, string globalAssetId)
        {
            if (string.IsNullOrEmpty(globalAssetId))
            {
                return null;
            }

            foreach (var aas in environment.AssetAdministrationShells)
            {
                if (aas.AssetInformation.GlobalAssetId.Equals(globalAssetId))
                {
                    return aas;
                }
            }

            return null;
        }

        public static ComparerIndexed CreateIndexedComparerCdsForSmUsage(this AasCore.Aas3_0.Environment environment)
        {
            var cmp = new ComparerIndexed();
            int nr = 0;
            foreach (var sm in environment.FindAllSubmodelGroupedByAAS())
                foreach (var sme in sm.FindDeep<ISubmodelElement>())
                {
                    if (sme.SemanticId == null)
                        continue;
                    var cd = environment.FindConceptDescriptionByReference(sme.SemanticId);
                    if (cd == null)
                        continue;
                    if (cmp.Index.ContainsKey(cd))
                        continue;
                    cmp.Index[cd] = nr++;
                }
            return cmp;
        }

        public static ISubmodelElement CopySubmodelElementAndCD(this AasCore.Aas3_0.Environment environment,
                AasCore.Aas3_0.Environment srcEnv, ISubmodelElement srcElem, bool copyCD = false, bool shallowCopy = false)
        {
            // access
            if (srcEnv == null || srcElem == null)
                return null;

            // 1st result pretty easy (calling function will add this to the appropriate Submodel)
            var res = srcElem.Copy();

            // copy the CDs..
            if (copyCD)
                environment.CopyConceptDescriptionsFrom(srcEnv, srcElem, shallowCopy);

            // give back
            return res;
        }

        public static IReference CopySubmodelRefAndCD(this AasCore.Aas3_0.Environment environment,
                AasCore.Aas3_0.Environment srcEnv, IReference srcSubRef, bool copySubmodel = false, bool copyCD = false,
                bool shallowCopy = false)
        {
            // access
            if (srcEnv == null || srcSubRef == null)
                return null;

            // need to have the source Submodel
            var srcSub = srcEnv.FindSubmodel(srcSubRef);
            if (srcSub == null)
                return null;

            // 1st result pretty easy (calling function will add this to the appropriate AAS)
            var dstSubRef = srcSubRef.Copy();

            // get the destination and shall src != dst
            var dstSub = environment.FindSubmodel(dstSubRef);
            if (srcSub == dstSub)
                return null;

            // maybe we need the Submodel in our environment, as well
            if (dstSub == null && copySubmodel)
            {
                dstSub = srcSub.Copy();
                environment.Submodels.Add(dstSub);
            }
            else
            if (dstSub != null)
            {
                // there is already an submodel, just add members
                if (!shallowCopy && srcSub.SubmodelElements != null)
                {
                    if (dstSub.SubmodelElements == null)
                        dstSub.SubmodelElements = new List<ISubmodelElement>();
                    foreach (var smw in srcSub.SubmodelElements)
                        dstSub.SubmodelElements.Add(
                            smw.Copy());
                }
            }

            // copy the CDs..
            if (copyCD && srcSub.SubmodelElements != null)
                foreach (var smw in srcSub.SubmodelElements)
                    environment.CopyConceptDescriptionsFrom(srcEnv, smw, shallowCopy);

            // give back
            return dstSubRef;
        }

        private static void CopyConceptDescriptionsFrom(this AasCore.Aas3_0.Environment environment,
                AasCore.Aas3_0.Environment srcEnv, ISubmodelElement src, bool shallowCopy = false)
        {
            // access
            if (srcEnv == null || src == null || src.SemanticId == null)
                return;
            
            // check for this SubmodelElement in Source
            var cdSrc = srcEnv.FindConceptDescriptionByReference(src.SemanticId);
            if (cdSrc == null)
                return;
            
            // check for this SubmodelElement in Destnation (this!)
            var cdDest = environment.FindConceptDescriptionByReference(src.SemanticId);
            if (cdDest == null)
            {
                // copy new
                environment.ConceptDescriptions.Add(cdSrc.Copy());
            }
            
            // recurse?
            if (!shallowCopy)
                foreach (var m in src.EnumerateChildren())
                    environment.CopyConceptDescriptionsFrom(srcEnv, m, shallowCopy: false);

        }
        #endregion

    }



}
