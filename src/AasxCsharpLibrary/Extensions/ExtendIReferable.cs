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

        public static string ToIdShortString(this IReferable rf)
        {
            if (rf.IdShort == null || rf.IdShort.Trim().Length < 1)
                return ("<no idShort!>");
            return rf.IdShort.Trim();
        }

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
                return new AasElementSelfDescription("AssetAdministrationShell", "AAS", 
                    KeyTypes.AssetAdministrationShell, null);
            }
            else if (referable is ConceptDescription)
            {
                return new AasElementSelfDescription("ConceptDescription", "CD", 
                    KeyTypes.ConceptDescription, null);
            }
            else if (referable is Submodel)
            {
                return new AasElementSelfDescription("Submodel", "SM", 
                    KeyTypes.Submodel, null);
            }
            else if (referable is Property)
            {
                return new AasElementSelfDescription("Property", "Prop", 
                    KeyTypes.Property, AasSubmodelElements.Property);
            }
            else if (referable is MultiLanguageProperty)
            {
                return new AasElementSelfDescription("MultiLanguageProperty", "MLP", 
                    KeyTypes.MultiLanguageProperty, AasSubmodelElements.MultiLanguageProperty);
            }
            else if(referable is AasCore.Aas3_0_RC02.Range)
            {
                return new AasElementSelfDescription("Range", "Range", 
                    KeyTypes.Range, AasSubmodelElements.Range);
            }
            else if(referable is Blob)
            {
                return new AasElementSelfDescription("Blob", "Blob", 
                    KeyTypes.Blob, AasSubmodelElements.Blob);
            }
            else if(referable is AasCore.Aas3_0_RC02.File)
            {
                return new AasElementSelfDescription("File", "File", 
                    KeyTypes.File, AasSubmodelElements.File);
            }
            else if(referable is ReferenceElement)
            {
                return new AasElementSelfDescription("ReferenceElement", "Ref", 
                    KeyTypes.ReferenceElement, AasSubmodelElements.ReferenceElement);
            }
            else if(referable is RelationshipElement)
            {
                return new AasElementSelfDescription("RelationshipElement", "Rel", 
                    KeyTypes.RelationshipElement, AasSubmodelElements.RelationshipElement);
            }
            else if(referable is AnnotatedRelationshipElement)
            {
                return new AasElementSelfDescription("AnnotatedRelationshipElement", "RelA", 
                    KeyTypes.AnnotatedRelationshipElement, AasSubmodelElements.AnnotatedRelationshipElement);
            }
            else if(referable is Capability)
            {
                return new AasElementSelfDescription("Capability", "Cap", 
                    KeyTypes.Capability, AasSubmodelElements.Capability);
            }
            else if(referable is SubmodelElementCollection)
            {
                return new AasElementSelfDescription("SubmodelElementCollection", "SMC", 
                    KeyTypes.SubmodelElementCollection, AasSubmodelElements.SubmodelElementCollection);
            }
            else if(referable is SubmodelElementList)
            {
                return new AasElementSelfDescription("SubmodelElementList", "SML", 
                    KeyTypes.SubmodelElementList, AasSubmodelElements.SubmodelElementList);
            }
            else if(referable is Operation)
            {
                return new AasElementSelfDescription("Operation", "Opr", 
                    KeyTypes.Operation, AasSubmodelElements.Operation);
            }
            else if(referable is Entity)
            {
                return new AasElementSelfDescription("Entity", "Ent", 
                    KeyTypes.Entity, AasSubmodelElements.Entity);
            }
            else if(referable is BasicEventElement)
            {
                return new AasElementSelfDescription("BasicEventElement", "Evt", 
                    
                    KeyTypes.BasicEventElement, AasSubmodelElements.BasicEventElement);
            }
            else if(referable is IDataElement)
            {
                return new AasElementSelfDescription("DataElement", "DE", 
                    KeyTypes.DataElement, AasSubmodelElements.DataElement);
            }
            else if(referable is ISubmodelElement)
            {
                return new AasElementSelfDescription("SubmodelElement", "SME", 
                    KeyTypes.SubmodelElement, AasSubmodelElements.SubmodelElement);
            }
            else
            {
                return new AasElementSelfDescription("Referable", "Ref", 
                    KeyTypes.Referable, null);
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

        public static bool EnumeratesChildren(this ISubmodelElement elem)
        {
            var num = elem.EnumerateChildren().Count();
            return (num > 0);
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
                if (operation.InputVariables != null)
                    foreach (var inputVariable in operation.InputVariables)
                    {
                        yield return inputVariable.Value;
                    }

                if (operation.OutputVariables != null)
                    foreach (var outputVariable in operation.OutputVariables)
                    {
                        yield return outputVariable.Value;
                    }

                if (operation.InoutputVariables != null)
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

        public static List<IReferable> ListOfIReferableFrom(
                System.Text.Json.Nodes.JsonNode node)
        {
            var res = new List<IReferable>();
            if (node == null)
                return res;
            var array = node.AsArray();
            foreach (var it in array)
            {
                var ir = Jsonization.Deserialize.IReferableFrom(it);
                res.Add(ir);
            }
            return res;
        }

        public static System.Text.Json.Nodes.JsonNode ToJsonObject(List<IClass> classes)
        {
            var jar = new System.Text.Json.Nodes.JsonArray();
            if (classes != null)
                foreach (var c in classes)
                    jar.Add(Jsonization.Serialize.ToJsonObject(c));
            return jar;
        }

        public static IEnumerable<Qualifier> FindAllQualifierType(this IReferable rf, string qualifierType)
        {
            if (!(rf is IQualifiable rfq) || rfq.Qualifiers == null || qualifierType == null)
                yield break;
            foreach (var q in rfq.Qualifiers)
                if (q.Type.Trim().ToLower() == qualifierType.Trim().ToLower())
                    yield return q;
        }

        public static Qualifier HasQualifierOfType(this IReferable rf, string qualifierType)
        {
            if (!(rf is IQualifiable rfq) || rfq.Qualifiers == null)
                return null;
            foreach (var q in rfq.Qualifiers)
                if (q.Type?.Trim().ToLower() == qualifierType?.Trim().ToLower())
                    return q;
            return null;
        }

        public static Qualifier Add(this IReferable rf, Qualifier q)
        {
            if (!(rf is IQualifiable rfq))
                return null;
            if (rfq.Qualifiers == null)
                rfq.Qualifiers = new List<Qualifier>();
            rfq.Qualifiers.Add(q);
            return q;
        }

        public static IEnumerable<Extension> FindAllExtensionName(this IReferable rf, string extensionName)
        {
            if (!(rf is IHasExtensions rfe) || rfe.Extensions == null)
                yield break;
            foreach (var e in rfe.Extensions)
                if (e.Name?.Trim().ToLower() == extensionName?.Trim().ToLower())
                    yield return e;
        }


        public static Extension HasExtensionOfName(this IReferable rf, string extensionName)
        {
            if (!(rf is IHasExtensions rfe) || rfe.Extensions == null)
                return null;
            foreach (var e in rfe.Extensions)
                if (e.Name?.Trim().ToLower() == extensionName?.Trim().ToLower())
                    return e;
            return null;
        }

        public static Extension Add(this IReferable rf, Extension ext)
        {
            if (rf.Extensions == null)
                rf.Extensions = new List<Extension>();
            rf.Extensions.Add(ext); 
            return ext;
        }

        public static void MigrateV20QualifiersToExtensions(this IReferable rf)
        {
            // access
            if (!(rf is IQualifiable iq) || iq.Qualifiers == null || !(rf is IHasExtensions ihe))
                return;

            // Qualifiers to migrate
            var toMigrate = new[] { "Animate.Args", "Plotting.Args", "TimeSeries.Args", "BOM.Args" };

            List<Qualifier> toMove = new List<Qualifier>();
            foreach (var q in iq.Qualifiers)
                foreach (var tm in toMigrate)
                    if (q?.Type?.Equals(tm, StringComparison.InvariantCultureIgnoreCase) == true)
                        toMove.Add(q);

            // now move these 
            for (int i=0; i<toMove.Count; i++)
            {
                var q = toMove[i];
                var ext = new Extension(
                    name: q.Type, semanticId: q.SemanticId, 
                    valueType: q.ValueType, value: q.Value);
                rf.Add(ext);
                iq.Qualifiers.Remove(q);
            }
        }
    }
}
