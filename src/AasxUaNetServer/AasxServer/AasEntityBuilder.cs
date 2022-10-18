/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using Opc.Ua;

namespace AasOpcUaServer
{
    public class AasEntityBuilder
    {
        //// Static singleton for AAS entity builders
        // ugly, but simple: the singleton variables gives access to information
        //
        public static AasNodeManager nodeMgr = null;

        public AdminShellPackageEnv package = null;

        public AasxUaServerOptions theServerOptions = null;

        public IDictionary<NodeId, IList<IReference>> nodeMgrExternalReferences = null;

        /// <summary>
        /// Root of AASes
        /// </summary>
        public NodeState RootAAS = null;

        /// <summary>
        /// Root for CDs
        /// </summary>
        public NodeState RootConceptDescriptions = null;

        /// <summary>
        /// Root for DataSpecifications
        /// </summary>
        public NodeState RootDataSpecifications = null;

        /// <summary>
        /// Provide a root node, if semantic ids shall create missing dictionary entry (targets), 
        /// which can not be found in the AAS environment.
        /// </summary>
        public NodeState RootMissingDictionaryEntries = null;

        public AasEntityBuilder(AasNodeManager nodeMgr, AdminShellPackageEnv package,
            IDictionary<NodeId, IList<IReference>> externalReferences, AasxUaServerOptions options)
        {
            AasEntityBuilder.nodeMgr = nodeMgr;
            this.package = package;
            this.nodeMgrExternalReferences = externalReferences;
            this.aasTypes = new AasTypeEntities();
            this.theServerOptions = options;
            this.aasTypes.BuildEntites(this);
        }

        public class NodeRecord
        {
            public NodeState uanode = null;
            public AdminShell.Referable referable = null;
            public AdminShell.Identification identification = null;

            public NodeRecord() { }
            public NodeRecord(NodeState uanode, AdminShell.Referable referable)
            {
                this.uanode = uanode;
                this.referable = referable;
            }
            public NodeRecord(NodeState uanode, AdminShell.Identification identification)
            {
                this.uanode = uanode;
                this.identification = identification;
            }
        }

        private Dictionary<AdminShell.Referable, NodeRecord> NodeRecordFromReferable
            = new Dictionary<AdminShell.Referable, NodeRecord>();
        private Dictionary<string, NodeRecord> NodeRecordFromIdentificationHash
            = new Dictionary<string, NodeRecord>();

        /// <summary>
        /// Use this function always to remeber new node records.
        /// </summary>
        /// <param name="nr"></param>
        public void AddNodeRecord(NodeRecord nr)
        {
            if (nr.referable != null && !NodeRecordFromReferable.ContainsKey(nr.referable))
                NodeRecordFromReferable.Add(nr.referable, nr);

            if (nr.identification != null && nr.identification.idType != null && nr.identification.id != null)
            {
                var hash = nr.identification.idType.Trim().ToUpper() + "|" + nr.identification.id.Trim().ToUpper();
                if (!NodeRecordFromIdentificationHash.ContainsKey(hash))
                    NodeRecordFromIdentificationHash.Add(hash, nr);
            }
        }

        /// <summary>
        /// Use this always to lookup node records from Referable
        /// </summary>
        /// <param name="referable"></param>
        /// <returns></returns>
        public NodeRecord LookupNodeRecordFromReferable(AdminShell.Referable referable)
        {
            if (NodeRecordFromReferable == null || !NodeRecordFromReferable.ContainsKey(referable))
                return null;
            return NodeRecordFromReferable[referable];
        }

        /// <summary>
        /// Use this always to lookup node records from Indentifiable
        /// </summary>
        /// <param name="identification"></param>
        /// <returns></returns>
        public NodeRecord LookupNodeRecordFromIdentification(AdminShell.Identification identification)
        {
            var hash = identification.idType.Trim().ToUpper() + "|" + identification.id.Trim().ToUpper();
            if (NodeRecordFromReferable == null || !NodeRecordFromIdentificationHash.ContainsKey(hash))
                return null;
            return NodeRecordFromIdentificationHash[hash];
        }

        /// <summary>
        /// Base class for actions, which shall be done on the 2nd pass of the information model building
        /// </summary>
        public class NodeLateAction
        {
            public NodeState uanode = null;
        }

        /// <summary>
        /// Make a late reference to another node identified by a AAS reference information
        /// </summary>
        public class NodeLateActionLinkToReference : NodeLateAction
        {
            public enum ActionType { None, SetAasReference, SetDictionaryEntry }

            public AdminShell.Reference targetReference = null;
            public ActionType actionType = ActionType.None;

            public NodeLateActionLinkToReference(NodeState uanode, AdminShell.Reference targetReference,
                ActionType actionType)
            {
                this.uanode = uanode;
                this.targetReference = targetReference;
                this.actionType = actionType;
            }
        }

        private List<NodeLateAction> noteLateActions = new List<NodeLateAction>();

        /// <summary>
        /// Add a late action, which will be processed as 2nd phase of info model preparation
        /// </summary>
        /// <param name="la"></param>
        public void AddNodeLateAction(NodeLateAction la)
        {
            this.noteLateActions.Add(la);
        }

        /// <summary>
        /// Top level creation functions. Uses the definitions of RootAAS, RootConceptDescriptions, 
        /// RootDataSpecifications to synthesize information model
        /// </summary>
        public void CreateAddInstanceObjects(AdminShell.AdministrationShellEnv env)
        {
            if (RootAAS == null)
                return;

            // CDs (build 1st to be "remembered" as targets for "HasDictionaryEntry")
            if (env.ConceptDescriptions != null && this.RootConceptDescriptions != null)
                foreach (var cd in env.ConceptDescriptions)
                {
                    this.AasTypes.ConceptDescription.CreateAddElements(this.RootConceptDescriptions,
                        AasUaBaseEntity.CreateMode.Instance, cd);
                }

            // AAS
            if (env.AdministrationShells != null)
                foreach (var aas in env.AdministrationShells)
                    this.AasTypes.AAS.CreateAddInstanceObject(RootAAS, env, aas);

            // go through late actions
            foreach (var la in this.noteLateActions)
            {
                // make a Reference ??
                var lax = la as NodeLateActionLinkToReference;

                // more simple case: AasReference between known entities
                if (lax != null && lax.actionType == NodeLateActionLinkToReference.ActionType.SetAasReference
                    && lax.uanode != null
                    && this.package != null && this.package.AasEnv != null)
                {
                    // 1st, take reference and turn it into Referable
                    var targetReferable = this.package.AasEnv.FindReferableByReference(lax.targetReference);
                    if (targetReferable == null)
                        continue;

                    // 2nd, try to lookup the Referable and turn it into a uanode
                    var targetNodeRec = this.LookupNodeRecordFromReferable(targetReferable);
                    if (targetNodeRec == null || targetNodeRec.uanode == null)
                        continue;

                    // now, we have everything to formulate a reference
                    lax.uanode.AddReference(this.AasTypes.HasAasReference.GetTypeNodeId(), false,
                        targetNodeRec.uanode.NodeId);
                }

                // a bit more complicated: could include a "empty reference" to outside concept
                if (lax != null && lax.actionType == NodeLateActionLinkToReference.ActionType.SetDictionaryEntry
                    && lax.uanode != null
                    && this.package != null && this.package.AasEnv != null)
                {
                    // tracking
                    var foundAtAll = false;

                    // 1st, take reference and turn it into Referable
                    var targetReferable = this.package.AasEnv.FindReferableByReference(lax.targetReference);
                    if (targetReferable != null)
                    {
                        // 2nd, try to lookup the Referable and turn it into a uanode
                        var targetNodeRec = this.LookupNodeRecordFromReferable(targetReferable);
                        if (targetNodeRec != null && targetNodeRec.uanode != null)
                        {
                            // simple case: have a target node, just make a link
                            lax.uanode.AddReference(this.AasTypes.HasDictionaryEntry.GetTypeNodeId(), false,
                                targetNodeRec.uanode.NodeId);
                            foundAtAll = true;
                        }
                    }

                    // make "empty reference"??
                    // by definition, this makes only sense if the targetReference has exactly 1 key, as we could 
                    // only have one key in a dictionary entry
                    if (!foundAtAll && lax.targetReference.Keys.Count == 1)
                    {
                        // can turn the targetReference to a simple identification
                        var targetId = new AdminShell.Identification(lax.targetReference.Keys[0].idType,
                            lax.targetReference.Keys[0].value);

                        // we might have such an (empty) target already available as uanode
                        var nr = this.LookupNodeRecordFromIdentification(targetId);
                        if (nr != null)
                        {
                            // just create the missing link
                            lax.uanode.AddReference(this.AasTypes.HasDictionaryEntry.GetTypeNodeId(), false,
                                nr.uanode?.NodeId);
                        }
                        else
                        {
                            // create NEW empty reference?
                            if (this.RootMissingDictionaryEntries != null)
                            {
                                // create missing object
                                var miss = this.CreateAddObject(
                                    this.RootMissingDictionaryEntries,
                                    AasUaBaseEntity.CreateMode.Instance,
                                    targetId.id,
                                    ReferenceTypeIds.HasComponent,
                                    this.AasTypes.ConceptDescription.GetTypeObjectFor(targetId)?.NodeId);

                                // add the reference
                                lax.uanode.AddReference(this.AasTypes.HasDictionaryEntry.GetTypeNodeId(), false,
                                    miss?.NodeId);

                                // put it into the NodeRecords, that it can be re-used?? no!!
                                this.AddNodeRecord(new AasEntityBuilder.NodeRecord(miss, targetId));
                            }
                            else
                            {
                                // just create the missing link
                                // TODO (MIHO, 2020-08-06): check, which namespace shall be used
                                var missingTarget = new ExpandedNodeId("" + targetId.id, 99);
                                lax.uanode.AddReference(this.AasTypes.HasDictionaryEntry.GetTypeNodeId(), false,
                                    missingTarget);
                            }
                        }
                    }
                }

            }
        }

        public ReferenceTypeState CreateAddReferenceType(string browseDisplayName, string inverseName, uint preferredNumId = 0, bool useZeroNS = false, NodeId sourceId = null, ExpandedNodeId extraSubtype = null)
        {
            // create node itself
            var x = new ReferenceTypeState();
            x.BrowseName = browseDisplayName;
            x.DisplayName = browseDisplayName;
            x.InverseName = inverseName;
            x.Symmetric = false;
            x.IsAbstract = false;
            x.NodeId = nodeMgr.NewType(nodeMgr.SystemContext, x, preferredNumId);

            // set Subtype reference
            if (sourceId == null)
                sourceId = new NodeId(32, 0);

            if (extraSubtype != null)
                x.AddReference(ReferenceTypeIds.HasSubtype, isInverse: true, extraSubtype);

            // done
            return x;
        }

        public DataTypeState CreateAddDataType(
            string browseDisplayName, NodeId superTypeId, uint preferredNumId = 0)
        {
            var x = new DataTypeState();
            x.BrowseName = "" + browseDisplayName;
            x.DisplayName = "" + browseDisplayName;
            x.Description = new LocalizedText("en", browseDisplayName);
            x.SuperTypeId = superTypeId;
            x.NodeId = nodeMgr.NewType(nodeMgr.SystemContext, x, preferredNumId);

            return x;
        }

        /// <summary>
        /// Helper to create an ObjectType-Node and it to the information model.
        /// </summary>
        /// <param name="browseDisplayName">Name displayed in the node tree</param>
        /// <param name="superTypeId">Base class or similar</param>
        /// <param name="preferredNumId">Numerical id of the node in the default name space to be set fixed</param>
        /// <param name="descriptionKey">Lookup a Description on AAS literal/ refSemantics</param>
        /// <param name="modellingRule">Modeling Rule, if not None</param>
        public BaseObjectTypeState CreateAddObjectType(
            string browseDisplayName,
            NodeId superTypeId,
            uint preferredNumId = 0,
            string descriptionKey = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            var x = AasUaNodeHelper.CreateObjectType(browseDisplayName, superTypeId, descriptionKey: descriptionKey);

            x.NodeId = nodeMgr.NewType(nodeMgr.SystemContext, x, preferredNumId);

            return x;
        }

        /// <summary>
        /// Helper to create an Object-Node. Note: __NO__ NodeId is created by the default! Must be done by outer 
        /// functionality!!
        /// </summary>
        /// <param name="parent">Parent node</param>
        /// <param name="mode">Type or instance</param>
        /// <param name="browseDisplayName">Name displayed in the node tree</param>
        /// <param name="referenceTypeFromParentId"></param>
        /// <param name="typeDefinitionId">Type of the Object</param>
        /// <param name="modellingRule">Modeling Rule, if not None</param>
        /// <param name="extraName"></param>
        /// <returns>The node</returns>
        public BaseObjectState CreateAddObject(
            NodeState parent,
            AasUaBaseEntity.CreateMode mode,
            string browseDisplayName,
            NodeId referenceTypeFromParentId = null,
            NodeId typeDefinitionId = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None,
            string extraName = null)
        {
            BaseObjectState baseObject = AasUaNodeHelper.CreateObject(parent, browseDisplayName, typeDefinitionId, modellingRule, extraName);
            
            baseObject.NodeId = nodeMgr.New(nodeMgr.SystemContext, mode, baseObject);

            parent?.AddChild(baseObject);

            if (referenceTypeFromParentId != null)
            {
                if (parent != null)
                {
                    parent.AddReference(referenceTypeFromParentId, false, baseObject.NodeId);

                    if (referenceTypeFromParentId == ReferenceTypeIds.HasComponent)
                    {
                        baseObject.AddReference(referenceTypeFromParentId, true, parent.NodeId);
                    }

                    if (referenceTypeFromParentId == ReferenceTypeIds.HasProperty)
                    {
                        baseObject.AddReference(referenceTypeFromParentId, true, parent.NodeId);
                    }
                }
            }

            return baseObject;
        }

        //// Properties
        //

        /// <summary>
        /// Helper to create an PropertyState-Node for a certain type and add it to the information model. 
        /// Note: __NO__ NodeId is created by the default! Must be done by outer functionality!!
        /// </summary>
        /// <typeparam name="T">C# type of the proprty</typeparam>
        /// <param name="parent">Parent node</param>
        /// <param name="mode">Type or instance</param>
        /// <param name="browseDisplayName">Name displayed in the node tree</param>
        /// <param name="dataTypeId">Data type, such as String.. Given by DataTypeIds...</param>
        /// <param name="value">Value of the type T or Null</param>
        /// <param name="referenceTypeFromParentId"></param>
        /// <param name="typeDefinitionId">Type definition; independent from DataType!</param>
        /// <param name="valueRank">-1 or e.g. 1 for array</param>
        /// <param name="defaultSettings">Apply default settings for a normal Property</param>
        /// <param name="modellingRule">Modeling Rule, if not None</param>
        /// <returns>NodeState</returns>
        public PropertyState<T> CreateAddPropertyState<T>(
            NodeState parent, AasUaBaseEntity.CreateMode mode,
            string browseDisplayName,
            NodeId dataTypeId, T value,
            NodeId referenceTypeFromParentId = null,
            NodeId typeDefinitionId = null,
            int valueRank = -2,
            bool defaultSettings = false,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            // apply cumulative settings
            if (defaultSettings)
            {
                referenceTypeFromParentId = ReferenceTypeIds.HasProperty;
                typeDefinitionId = VariableTypeIds.PropertyType;

                if (valueRank == -2)
                {
                    valueRank = -1;
                }
            }

            // make Property
            var x = new PropertyState<T>(parent);
            x.BrowseName = "" + browseDisplayName;
            x.DisplayName = "" + browseDisplayName;
            x.Description = new LocalizedText("en", browseDisplayName);
            x.DataType = dataTypeId;
            if (valueRank > -2)
                x.ValueRank = valueRank;
            // ReSharper disable once RedundantCast
            x.Value = (T)value;
            AasUaNodeHelper.CheckSetModellingRule(modellingRule, x);
            x.NodeId = nodeMgr.New(nodeMgr.SystemContext, mode, x);

            // add Node
            if (parent != null)
                parent.AddChild(x);

            // set relations
            if (referenceTypeFromParentId != null)
            {
                if (parent != null)
                {
                    parent.AddReference(referenceTypeFromParentId, false, x.NodeId);

                    if (referenceTypeFromParentId == ReferenceTypeIds.HasComponent)
                        x.AddReference(referenceTypeFromParentId, true, parent.NodeId);

                    if (referenceTypeFromParentId == ReferenceTypeIds.HasProperty)
                        x.AddReference(referenceTypeFromParentId, true, parent.NodeId);
                }
            }

            if (typeDefinitionId != null)
            {
                x.TypeDefinitionId = typeDefinitionId;
            }

            x.AccessLevel = AccessLevels.CurrentReadOrWrite;
            x.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            return x;
        }

        public MethodState CreateAddMethodState(
            NodeState parent, AasUaBaseEntity.CreateMode mode,
            string browseDisplayName,
            Argument[] inputArgs = null, Argument[] outputArgs = null, NodeId referenceTypeFromParentId = null,
            NodeId methodDeclarationId = null, GenericMethodCalledEventHandler onCalled = null)
        {
            // method node
            var m = new MethodState(parent);
            m.BrowseName = "" + browseDisplayName;
            m.DisplayName = "" + browseDisplayName;
            m.Description = new LocalizedText("en", browseDisplayName);
            m.NodeId = nodeMgr.New(nodeMgr.SystemContext, mode, m);
            if (methodDeclarationId != null)
                m.MethodDeclarationId = methodDeclarationId;

            m.Executable = true;
            m.UserExecutable = true;

            if (parent != null)
                parent.AddChild(m);

            if (referenceTypeFromParentId != null)
            {
                if (parent != null)
                {
                    parent.AddReference(referenceTypeFromParentId, false, m.NodeId);

                    if (referenceTypeFromParentId == ReferenceTypeIds.HasComponent)
                        m.AddReference(referenceTypeFromParentId, true, parent.NodeId);

                    if (referenceTypeFromParentId == ReferenceTypeIds.HasProperty)
                        m.AddReference(referenceTypeFromParentId, true, parent.NodeId);
                }
            }

            // can have inputs, outputs
            for (int i = 0; i < 2; i++)
            {
                // pretty argument list
                var arguments = (i == 0) ? inputArgs : outputArgs;
                if (arguments == null || arguments.Length < 1)
                    continue;

                // make a property for this
                var prop = CreateAddPropertyState<Argument[]>(
                    m, mode,
                    (i == 0) ? "InputArguments" : "OutputArguments",
                    DataTypeIds.Argument,
                    arguments,
                    ReferenceTypeIds.HasProperty,
                    typeDefinitionId: VariableTypeIds.PropertyType,
                    valueRank: 1);

                // explicitely add arguments ass well?
                if (i == 0)
                    m.InputArguments = prop;

                if (i == 1)
                    m.OutputArguments = prop;

            }

            // event handler
            if (onCalled != null)
                m.OnCallMethod = onCalled;


            return m;
        }


        //// Entities
        //

        public class AasTypeEntities
        {
            public AasUaEntityPathType PathType;
            public AasUaEntityMimeType MimeType;

            public AasUaEntityIdentification Identification;
            public AasUaEntityAdministration Administration;
            public AasUaEntityQualifier Qualifier;
            public AasUaEntityAssetKind AssetKind;
            public AasUaEntityModelingKind ModelingKind;
            public AasUaEntityReferable Referable;
            public AasUaEntityReferenceBase ReferenceBase;
            public AasUaEntityReference Reference;
            public AasUaEntitySemanticId SemanticId;
            public AasUaEntitySubmodel Submodel;
            public AasUaEntityProperty Property;
            public AasUaEntityCollection Collection;
            public AasUaEntitySubmodelElement SubmodelElement;
            public AasUaEntitySubmodelWrapper SubmodelWrapper;
            public AasUaEntityFile File;
            public AasUaEntityFileType FileType;
            public AasUaEntityBlob Blob;
            public AasUaEntityReferenceElement ReferenceElement;
            public AasUaEntityRelationshipElement RelationshipElement;
            public AasUaEntityOperationVariable OperationVariable;
            public AasUaEntityOperation Operation;
            public AasUaEntityConceptDictionary ConceptDictionary;
            public AasUaEntityConceptDescription ConceptDescription;
            public AasUaEntityView View;
            public AasUaEntityAsset Asset;
            public AasUaEntityAAS AAS;

            public AasUaEntityDataSpecification DataSpecification;
            public AasUaEntityDataSpecificationIEC61360 DataSpecificationIEC61360;

            public AasUaInterfaceAASIdentifiableType IAASIdentifiableType;
            public AasUaInterfaceAASReferableType IAASReferableType;

            public AasUaNamespaceZeroEntity BaseInterfaceType;

            public AasUaNamespaceZeroReference HasDictionaryEntry;
            public AasUaReferenceHasAasReference HasAasReference;
            public AasUaNamespaceZeroReference HasInterface;
            public AasUaNamespaceZeroReference HasAddIn;
            public AasUaNamespaceZeroEntity DictionaryEntryType;
            public AasUaNamespaceZeroEntity UriDictionaryEntryType;
            public AasUaNamespaceZeroEntity IrdiDictionaryEntryType;
            public AasUaNamespaceZeroEntity DictionaryFolderType;

            public void BuildEntites(AasEntityBuilder builder)
            {
                // build up entities, which are in the UA specs, but not in this Stack
                BaseInterfaceType = new AasUaNamespaceZeroEntity(builder, 17602);
                HasDictionaryEntry = new AasUaNamespaceZeroReference(builder, 17597);
                HasInterface = new AasUaNamespaceZeroReference(builder, 17603);
                HasAddIn = new AasUaNamespaceZeroReference(builder, 17604);
                DictionaryEntryType = new AasUaNamespaceZeroEntity(builder, 17589);
                UriDictionaryEntryType = new AasUaNamespaceZeroEntity(builder, 17600);
                IrdiDictionaryEntryType = new AasUaNamespaceZeroEntity(builder, 17598);
                DictionaryFolderType = new AasUaNamespaceZeroEntity(builder, 17591);

                // AAS DataTypes
                PathType = new AasUaEntityPathType(builder);
                MimeType = new AasUaEntityMimeType(builder);

                // first entities
                Referable = new AasUaEntityReferable(builder, 1004);
                Identification = new AasUaEntityIdentification(builder, 1000);
                Administration = new AasUaEntityAdministration(builder, 1001);

                // interfaces
                IAASReferableType = new AasUaInterfaceAASReferableType(
                    builder, 2001); // dependencies: Referable
                IAASIdentifiableType = new AasUaInterfaceAASIdentifiableType(
                    builder, 2000); // dependencies: IAASReferable

                // AAS References
                ReferenceBase = new AasUaEntityReferenceBase(builder, 0);
                Reference = new AasUaEntityReference(builder, 1005);
                SemanticId = new AasUaEntitySemanticId(builder, 1006); // dependecies: Reference
                HasAasReference = new AasUaReferenceHasAasReference(builder, 4000); // dependencies: Referable

                // Data Specifications
                DataSpecification = new AasUaEntityDataSpecification(builder, 3000);
                DataSpecificationIEC61360 = new AasUaEntityDataSpecificationIEC61360(
                    builder, 3001); // dependencies: Reference, Identification, Administration

                // rest
                Qualifier = new AasUaEntityQualifier(builder, 1002); // dependencies: SemanticId, Reference
                AssetKind = new AasUaEntityAssetKind(builder, 1025);
                ModelingKind = new AasUaEntityModelingKind(builder, 1003);
                SubmodelElement = new AasUaEntitySubmodelElement(builder, 1008);
                SubmodelWrapper = new AasUaEntitySubmodelWrapper(builder, 1012); // dependencies: SubmodelElement
                Submodel = new AasUaEntitySubmodel(builder, 1007); // dependencies: SubmodelWrapper
                Property = new AasUaEntityProperty(builder, 1009);
                Collection = new AasUaEntityCollection(builder, 1010); // needs 2 ids!
                FileType = new AasUaEntityFileType(builder, 1014);
                File = new AasUaEntityFile(builder, 1013); // dependencies: FileType
                Blob = new AasUaEntityBlob(builder, 1015);
                ReferenceElement = new AasUaEntityReferenceElement(builder, 1016);
                RelationshipElement = new AasUaEntityRelationshipElement(builder, 1017);
                OperationVariable = new AasUaEntityOperationVariable(builder, 1018);
                Operation = new AasUaEntityOperation(builder, 1019);
                ConceptDictionary = new AasUaEntityConceptDictionary(builder, 1020);
                ConceptDescription = new AasUaEntityConceptDescription(builder, 1021);
                View = new AasUaEntityView(builder, 1022);
                Asset = new AasUaEntityAsset(builder, 1023);
                AAS = new AasUaEntityAAS(builder, 1024);
            }
        }

        private AasTypeEntities aasTypes = null;
        public AasTypeEntities AasTypes { get { return aasTypes; } }

        //// Annotations
        //

        private Dictionary<NodeState, List<object>> nodeStateAnnotations = new Dictionary<NodeState, List<object>>();

        public void AddNodeStateAnnotation(NodeState nodeState, object businessObject)
        {
            if (!nodeStateAnnotations.ContainsKey(nodeState))
                nodeStateAnnotations[nodeState] = new List<object>();
            nodeStateAnnotations[nodeState].Add(businessObject);
        }

        public void RemoveNodeStateAnnotation(NodeState nodeState, object businessObject)
        {
            if (!nodeStateAnnotations.ContainsKey(nodeState))
                return;
            if (nodeStateAnnotations[nodeState].Contains(businessObject))
                nodeStateAnnotations[nodeState].Remove(businessObject);
        }

        public T FindNoteStateAnnotation<T>(NodeState nodeState) where T : class
        {
            if (nodeState == null)
                return null;
            if (!nodeStateAnnotations.ContainsKey(nodeState))
                return null;
            foreach (var bo in nodeStateAnnotations[nodeState])
                if (bo is T)
                    return bo as T;
            return null;
        }

    }

}
