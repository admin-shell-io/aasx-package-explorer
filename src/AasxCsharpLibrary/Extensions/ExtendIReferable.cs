using AasCore.Aas3_0_RC02;
using AdminShellNS;
using AdminShellNS.Display;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendIReferable
    {
        #region AasxPackageExplorer

        public static void RecurseOnReferables(this IReferable referable,
                object state, Func<object, List<IReferable>, IReferable, bool> lambda,
                bool includeThis = false)
        {
            if(referable is Submodel submodel)
            {
                submodel.RecurseOnReferables(state, lambda, includeThis);
            }
            else if(referable is SubmodelElementCollection submodelElementCollection)
            {
                submodelElementCollection.RecurseOnReferables(state, lambda, includeThis);
            }
            else if(referable is SubmodelElementList submodelElementList)
            {
                submodelElementList.RecurseOnReferables(state, lambda, includeThis);
            }
            else if (includeThis)
                lambda(state, null, referable);
        }

        public static void Remove(this IReferable referable, ISubmodelElement submodelElement)
        {
            if(referable is Submodel submodel)
            {
                submodel.Remove(submodelElement);
            }
            else if(referable is AnnotatedRelationshipElement annotatedRelationshipElement)
            {
                annotatedRelationshipElement.Remove(submodelElement);
            }
            else if(referable is SubmodelElementCollection submodelElementCollection)
            {
                submodelElementCollection.Remove(submodelElement);
            }
            else if(referable is SubmodelElementList submodelElementList)
            {
                submodelElementList.Remove(submodelElement);
            }
            else if(referable is Entity entity)
            {
                entity.Remove(submodelElement);
            }
        }
        
        public static void Add(this IReferable referable, ISubmodelElement submodelElement)
        {
            if (referable is Submodel submodel)
            {
                submodel.Add(submodelElement);
            }
            else if (referable is AnnotatedRelationshipElement annotatedRelationshipElement)
            {
                annotatedRelationshipElement.Add(submodelElement);
            }
            else if (referable is SubmodelElementCollection submodelElementCollection)
            {
                submodelElementCollection.Add(submodelElement);
            }
            else if (referable is SubmodelElementList submodelElementList)
            {
                submodelElementList.Add(submodelElement);
            }
            else if (referable is Entity entity)
            {
                entity.Add(submodelElement);
            }
        }

        #region Display

        public static EnumerationPlacmentBase GetChildrenPlacement(this IReferable referable, ISubmodelElement submodelElement)
        {
            if(referable is Operation operation)
            {
                return operation.GetChildrenPlacement(submodelElement);
            }

            return null;
        }

        #endregion

        public static IIdentifiable FindParentFirstIdentifiable(this IReferable referable)
        {
            IReferable curr = referable;
            while (curr != null)
            {
                if (curr is IIdentifiable curri)
                    return curri;
                curr = curr.Parent as IReferable;
            }
            return null;
        }

        #endregion

        #region ListOfReferables
        public static Reference GetReference(this List<IReferable> referables)
        {
            return new Reference(ReferenceTypes.GlobalReference, referables.ToKeyList());
        }

        public static List<Key> ToKeyList(this List<IReferable> referables)
        {
            var res = new List<Key>();
            foreach (var rf in referables)
                res.Add(new Key(KeyTypes.Referable, rf.IdShort));
            return res;
        }
        #endregion

        public static Reference GetReference(this IReferable referable)
        {
            if (referable is IIdentifiable identifiable)
            {
                return identifiable.GetReference();
            }
            else if (referable is ISubmodelElement submodelElement)
            {
                return submodelElement.GetModelReference();
            }
            else
                return null;
        }
        public static void Validate(this IReferable referable,AasValidationRecordList results)
        {
            referable.BaseValidation(results);
            
            if(referable is ConceptDescription conceptDescription)
            {
                conceptDescription.Validate(results);
            }
            else if(referable is Submodel submodel)
            {
                submodel.Validate(results);
            }
            else if(referable is ISubmodelElement submodelElement)
            {
                submodelElement.Validate(results);
            }
        }

        public static void BaseValidation(this IReferable referable, AasValidationRecordList results)
        {
            // access
            if (results == null)
                return;

            // check
            if (string.IsNullOrEmpty(referable.IdShort))
                results.Add(new AasValidationRecord(
                    AasValidationSeverity.SpecViolation, referable,
                    "Referable: missing idShort",
                    () =>
                    {
                        referable.IdShort = "TO_FIX";
                    }));

            if (referable.Description != null && (referable.Description.Count < 1))
                results.Add(new AasValidationRecord(
                    AasValidationSeverity.SchemaViolation, referable,
                    "Referable: existing description with missing langString",
                    () =>
                    {
                        referable.Description = null;
                    }));
        }

        public static AasElementSelfDescription GetSelfDescription(this IReferable referable)
        {
            if (referable is AssetAdministrationShell)
            {
                return new AasElementSelfDescription("AssetAdministrationShell", "AAS");
            }
            else if (referable is ConceptDescription)
            {
                return new AasElementSelfDescription("ConceptDescription", "CD");
            }
            else if (referable is Submodel)
            {
                return new AasElementSelfDescription("Submodel", "SM");
            }
            else if (referable is Property)
            {
                return new AasElementSelfDescription("Property", "Prop");
            }
            else if (referable is MultiLanguageProperty)
            {
                return new AasElementSelfDescription("MultiLanguageProperty", "MLP");
            }
            else if(referable is AasCore.Aas3_0_RC02.Range)
            {
                return new AasElementSelfDescription("Range", "Range");
            }
            else if(referable is Blob)
            {
                return new AasElementSelfDescription("Blob", "Blob");
            }
            else if(referable is AasCore.Aas3_0_RC02.File)
            {
                return new AasElementSelfDescription("File", "File");
            }
            else if(referable is ReferenceElement)
            {
                return new AasElementSelfDescription("ReferenceElement", "Ref");
            }
            else if(referable is RelationshipElement)
            {
                return new AasElementSelfDescription("RelationshipElement", "Rel");
            }
            else if(referable is AnnotatedRelationshipElement)
            {
                return new AasElementSelfDescription("AnnotatedRelationshipElement", "RelA");
            }
            else if(referable is Capability)
            {
                return new AasElementSelfDescription("Capability", "Cap");
            }
            else if(referable is SubmodelElementCollection)
            {
                return new AasElementSelfDescription("SubmodelElementCollection", "SMC");
            }
            else if(referable is SubmodelElementList)
            {
                return new AasElementSelfDescription("SubmodelElementList", "SML");
            }
            else if(referable is Operation)
            {
                return new AasElementSelfDescription("Operation", "Opr");
            }
            else if(referable is Entity)
            {
                return new AasElementSelfDescription("Entity", "Ent");
            }
            else if(referable is BasicEventElement)
            {
                return new AasElementSelfDescription("BasicEventElement", "Evt");
            }
            else if(referable is IDataElement)
            {
                return new AasElementSelfDescription("DataElement", "DE");
            }
            else if(referable is ISubmodelElement)
            {
                return new AasElementSelfDescription("SubmodelElement", "SME");
            }
            else
            {
                return new AasElementSelfDescription("Referable", "Ref");
            }
        }
        public static void CollectReferencesByParent(this IReferable referable, List<Key> refs)
        {
            // access
            if (refs == null)
                return;

            // check, if this is identifiable
            if (referable is IIdentifiable)
            {
                var idf = referable as IIdentifiable;
                if (idf != null)
                {
                    //var k = Key.CreateNew(
                    //    idf.GetElementName(), true, idf.identification?.idType, idf.identification?.id);
                    
                    var key = new Key((KeyTypes)Stringification.KeyTypesFromString(idf.GetType().Name), idf.Id);
                    refs.Insert(0, key);
                }
            }
            else
            {
                //var k = Key.CreateNew(this.GetElementName(), true, "IdShort", referable.IdShort);
                var key = new Key((KeyTypes)Stringification.KeyTypesFromString(referable.GetType().Name), referable.IdShort);
                refs.Insert(0, key);
                // recurse upwards!
                if (referable.Parent is IReferable prf)
                    prf.CollectReferencesByParent(refs);
            }
        }
        public static void SetTimeStamp(this IReferable referable, DateTime timeStamp)
        {
            IReferable newReferable = referable;
            do
            {
                newReferable.TimeStamp = timeStamp;
                if (newReferable != newReferable.Parent)
                {
                    newReferable = (IReferable)newReferable.Parent;
                }
                else
                    newReferable = null;
            }
            while (newReferable != null);
        }

        public static IEnumerable<ISubmodelElement> EnumerateChildren(this IReferable referable)
        {
            if (referable is Submodel submodel && submodel.SubmodelElements != null)
            {
                if (submodel.SubmodelElements != null)
                {
                    foreach (var submodelElement in submodel.SubmodelElements)
                    {
                        yield return submodelElement;
                    }
                }
            }
            else if (referable is SubmodelElementCollection submodelElementCollection)
            {
                if (submodelElementCollection.Value != null)
                {
                    foreach (var submodelElement in submodelElementCollection.Value)
                    {
                        yield return submodelElement;
                    }
                }
            }
            else if (referable is SubmodelElementList submodelElementList)
            {
                if (submodelElementList.Value != null)
                {
                    foreach (var submodelElement in submodelElementList.Value)
                    {
                        yield return submodelElement;
                    }
                }
            }
            else if (referable is AnnotatedRelationshipElement annotatedRelationshipElement)
            {
                if (annotatedRelationshipElement.Annotations != null)
                {
                    foreach (var submodelElement in annotatedRelationshipElement.Annotations)
                    {
                        yield return submodelElement;
                    }
                }
            }
            else if (referable is Entity entity)
            {
                if (entity.Statements != null)
                {
                    foreach (var submodelElement in entity.Statements)
                    {
                        yield return submodelElement;
                    }
                }
            }
            else if (referable is Operation operation)
            {
                foreach (var inputVariable in operation.InputVariables)
                {
                    yield return inputVariable.Value;
                }

                foreach (var outputVariable in operation.OutputVariables)
                {
                    yield return outputVariable.Value;
                }

                foreach (var inOutVariable in operation.InoutputVariables)
                {
                    yield return inOutVariable.Value;
                }
            }
            else
            {
                yield break;
            }
        }


        public static void SetAllParentsAndTimestamps(this IReferable referable, IReferable parent, DateTime timeStamp, DateTime timeStampCreate)
        {
            if (parent == null)
                return;

            referable.Parent = parent;
            referable.TimeStamp = timeStamp;
            referable.TimeStampCreate = timeStampCreate;

            foreach (var submodelElement in referable.EnumerateChildren())
            {
                submodelElement.SetAllParentsAndTimestamps(referable, timeStamp, timeStampCreate);
            }
        }

        public static Submodel GetParentSubmodel(this IReferable referable)
        {
            IReferable parent = referable;
            while (parent is not Submodel && parent != null)
                parent = (IReferable)parent.Parent;
            return parent as Submodel;
        }

        public static string CollectIdShortByParent(this IReferable referable)
        {
            // recurse first
            var head = "";
            if (referable is not IIdentifiable && referable.Parent is IReferable parentReferable)
                // can go up
                head = parentReferable.CollectIdShortByParent() + "/";
            // add own
            var myid = "<no id-Short!>";
            if (string.IsNullOrEmpty(referable.IdShort))
                myid = referable.IdShort.Trim();
            // together
            return head + myid;
        }

        public static void AddDescription(this IReferable referable,string language, string Text)
        {
            if (referable.Description == null)
                referable.Description = new List<LangString>();
            referable.Description.Add(new LangString(language, Text));
        }
    }
}
