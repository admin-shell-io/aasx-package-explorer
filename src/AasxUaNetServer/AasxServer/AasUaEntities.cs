using AdminShellNS;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AasOpcUaServer
{
    public class AasUaBaseEntity
    {
        public enum CreateMode { Type, Instance };

        /// <summary>
        /// Reference back to the entity builder
        /// </summary>
        protected AasEntityBuilder entityBuilder = null;

        public AasUaBaseEntity(AasEntityBuilder entityBuilder)
        {
            this.entityBuilder = entityBuilder;
        }

        /// <summary>
        /// Typically the node of the entity in the AAS type object space
        /// </summary>
        protected NodeState typeObject = null;

        /// <summary>
        /// If the entitiy does not have a direct type object, the object id instead (for pre-defined objects)
        /// </summary>
        protected NodeId typeObjectId = null;

        /// <summary>
        /// Getter of the type object
        /// </summary>
        public NodeState GetTypeObject()
        {
            return typeObject;
        }

        /// <summary>
        /// Getter of the type object id, either directly or via the type object (if avilable)
        /// </summary>
        /// <returns></returns>
        public NodeId GetTypeNodeId()
        {
            if (typeObjectId != null)
                return typeObjectId;
            if (typeObject == null)
                return null;
            return typeObject.NodeId;
        }
    }

    public class AasUaEntityPathType : AasUaBaseEntity
    {
        public AasUaEntityPathType(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type elements
            this.typeObject = this.entityBuilder.CreateAddDataType("AASPathType", DataTypeIds.String);
        }
    }

    public class AasUaEntityMimeType : AasUaBaseEntity
    {
        public AasUaEntityMimeType(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type elements
            this.typeObject = this.entityBuilder.CreateAddDataType("AASMimeType", DataTypeIds.String);
        }
    }

    public class AasUaEntityIdentification : AasUaBaseEntity
    {
        public AasUaEntityIdentification(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASIdentifierType", ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:Identifier");
            // add some elements
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "IdType", DataTypeIds.String, null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "Id", DataTypeIds.String, null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.Identification identification = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            var o = this.entityBuilder.CreateAddObject(parent, "Identification", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);

            if (mode == CreateMode.Instance)
            {
                this.entityBuilder.CreateAddPropertyState<string>(o, "IdType", DataTypeIds.String, "" + "" + identification.idType, defaultSettings: true);
                this.entityBuilder.CreateAddPropertyState<string>(o, "Id", DataTypeIds.String, "" + "" + identification.id, defaultSettings: true);
            }

            return o;
        }
    }

    public class AasUaEntityAdministration : AasUaBaseEntity
    {
        public AasUaEntityAdministration(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASAdministrativeInformationType", ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:AdministrativeInformation");
            // add some elements
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "Version", DataTypeIds.String, null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "Revision", DataTypeIds.String, null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.Administration administration = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;
            // Create whole object only if required
            if (mode == CreateMode.Instance && administration == null)
                return null;

            var o = this.entityBuilder.CreateAddObject(parent, "Administration", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);

            if (mode == CreateMode.Instance)
            {
                if (administration == null)
                    return null;
                this.entityBuilder.CreateAddPropertyState<string>(o, "Version", DataTypeIds.String, "" + "" + administration.version, defaultSettings: true);
                this.entityBuilder.CreateAddPropertyState<string>(o, "Revision", DataTypeIds.String, "" + "" + administration.revision, defaultSettings: true);
            }

            return o;
        }
    }

    public class AasUaEntityQualifier : AasUaBaseEntity
    {
        public AasUaEntityQualifier(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASQualifierType", ObjectTypeIds.BaseObjectType, preferredTypeNumId, "AAS:Qualifier");

            // add some elements
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(this.typeObject, CreateMode.Type, null, "SemanticId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "Type", DataTypeIds.String, null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "Value", DataTypeIds.String, null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "ValueId", AasUaNodeHelper.ModellingRule.Optional);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.Qualifier qualifier = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;
            // Create whole object only if required
            if (mode == CreateMode.Instance && qualifier == null)
                return null;

            if (mode == CreateMode.Type)
            {
                // plain
                var o = this.entityBuilder.CreateAddObject(parent, "Qualifier", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);
                return o;
            }
            else
            {
                // need data
                if (qualifier == null)
                    return null;

                // do a little extra?
                string extraName = null;
                if (qualifier.type != null && qualifier.type.Length>0)
                {
                    extraName = "Qualifier:" + qualifier.type;
                    if (qualifier.value != null && qualifier.value.Length > 0)
                        extraName += "=" + qualifier.value;
                }

                var o = this.entityBuilder.CreateAddObject(parent, "Qualifier", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule, extraName: extraName);

                this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(o, CreateMode.Instance, qualifier.semanticId, "SemanticId");
                this.entityBuilder.CreateAddPropertyState<string>(o, "Type", DataTypeIds.String, "" + qualifier.type, defaultSettings: true);
                this.entityBuilder.CreateAddPropertyState<string>(o, "Value", DataTypeIds.String, "" + qualifier.value, defaultSettings: true);
                this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, qualifier.valueId, "ValueId");

                return o;
            }

        }
    }

    public class AasUaEntityAssetKind : AasUaBaseEntity
    {
        public AasUaEntityAssetKind(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            // no special type here (is a string)
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.AssetKind kind = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (mode == CreateMode.Instance && kind == null)
                return null;

            var o = this.entityBuilder.CreateAddPropertyState<string>(parent, "Kind", DataTypeIds.String, (mode == CreateMode.Type) ? null : "" + kind.kind, defaultSettings: true, modellingRule: modellingRule);

            return o;
        }
    }

    public class AasUaEntityModelingKind : AasUaBaseEntity
    {
        public AasUaEntityModelingKind(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            // no special type here (is a string)
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.ModelingKind kind = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (mode == CreateMode.Instance && kind == null)
                return null;

            var o = this.entityBuilder.CreateAddPropertyState<string>(parent, "Kind", DataTypeIds.String, (mode == CreateMode.Type) ? null : "" + kind.kind, defaultSettings: true, modellingRule: modellingRule);

            return o;
        }
    }

    public class AasUaEntityReferable : AasUaBaseEntity
    {
        public AasUaEntityReferable(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // NO type object required
            // see IAASReferable interface
        }

        /// <summary>
        /// This adds all Referable attributes to the parent and re-defines the descriptons 
        /// </summary>
        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.Referable refdata = null)
        {
            if (parent == null)
                return null;
            if (mode == CreateMode.Instance && refdata == null)
                return null;

            if (mode == CreateMode.Type || refdata?.category != null)
                this.entityBuilder.CreateAddPropertyState<string>(parent, "Category", DataTypeIds.String, (mode == CreateMode.Type) ? null : "" + refdata.category, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            // No idShort as typically in the DisplayName of the node
            // this.entityBuilder.CreateAddPropertyState<string>(parent, "IdShort", DataTypeIds.String, "" + refdata.idShort, defaultSettings: true);

            if (mode == CreateMode.Instance)
            {
                // now, re-set the description on the parent
                // ISSUE: only ONE language supported!
                parent.Description = AasUaUtils.GetBestUaDescriptionFromAasDescription(refdata.description);
            }

            return null;
        }
    }

    public class AasUaEntityReferenceBase : AasUaBaseEntity
    {
        public AasUaEntityReferenceBase(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // nothing, only used to share code
        }

        /// <summary>
        /// Sets the "Keys" value information of an AAS Reference. This is especially important for referencing outwards of the AAS (environment).
        /// </summary>
        public void CreateAddKeyElements(NodeState parent, CreateMode mode, List<AdminShell.Key> keys = null)
        {
            if (parent == null)
                return;

            // MIHO: open62541 does not to process Values as string[], therefore change it temporarily

            if (this.entityBuilder != null && this.entityBuilder.theServerOptions != null && this.entityBuilder.theServerOptions.ReferenceKeysAsSingleString)
            {
                // fix for open62541
                var keyo = this.entityBuilder.CreateAddPropertyState<string>(parent, "Keys", DataTypeIds.String, null, defaultSettings: true);

                if (mode == CreateMode.Instance && keyo != null)
                {
                    keyo.Value = AasUaUtils.ToOpcUaReference(AdminShell.Reference.CreateNew(keys));
                }
            }
            else
            {
                // default behaviour
                var keyo = this.entityBuilder.CreateAddPropertyState<string[]>(parent, "Keys", DataTypeIds.Structure, null, defaultSettings: true);

                if (mode == CreateMode.Instance && keyo != null)
                {
                    keyo.Value = AasUaUtils.ToOpcUaReferenceList(AdminShell.Reference.CreateNew(keys))?.ToArray();
                }
            }
        }

        /// <summary>
        /// Sets the UA relation of an AAS Reference. This is especially important for reference within an AAS node structure, to be
        /// in the style of OPC UA
        /// </summary>
        public void CreateAddReferenceElements(NodeState parent, CreateMode mode, List<AdminShell.Key> keys = null)
        {
            if (parent == null)
                return;

            if (mode == CreateMode.Type)
            {
                // makes no sense
            }
            else
            {
                // would make sense, but is replaced by the code in "CreateAddInstanceObjects" directly.
            }
        }
    }

    public class AasUaEntityReference : AasUaEntityReferenceBase
        {
        public AasUaEntityReference(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASReferenceType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
            // with some elements
            this.CreateAddKeyElements(this.typeObject, CreateMode.Type);
            this.CreateAddReferenceElements(this.typeObject, CreateMode.Type);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.Reference reference, string browseDisplayName = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                var o = this.entityBuilder.CreateAddObject(parent, browseDisplayName ?? "Reference", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);
                return o;
            }
            else
            {
                if (reference == null)
                    return null;

                var o = this.entityBuilder.CreateAddObject(parent, browseDisplayName ?? "Reference", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

                // explicit strings?
                this.CreateAddKeyElements(o, mode, reference.Keys);

                // find a matching concept description or other referable?
                // as we do not have all other nodes realized, store a late action
                this.entityBuilder.AddNodeLateAction(
                    new AasEntityBuilder.NodeLateActionLinkToReference(
                        o,
                        reference,
                        AasEntityBuilder.NodeLateActionLinkToReference.ActionType.SetAasReference
                    ));

                // OK
                return o;
            }
        }
    }

    public class AasUaEntitySemanticId : AasUaEntityReferenceBase
    {
        public AasUaEntitySemanticId(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // re-use AASReferenceType for this
            this.typeObject = this.entityBuilder.AasTypes.Reference.GetTypeObject();
            // with some elements
            // this was duplicate: this.CreateAddKeyElements(this.typeObject, CreateMode.Type);
            this.CreateAddReferenceElements(this.typeObject, CreateMode.Type);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, CreateMode mode, AdminShell.SemanticId semid = null, string browseDisplayName = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                var o = this.entityBuilder.CreateAddObject(parent, browseDisplayName ?? "SemanticId", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);
                return o;
            }
            else
            {
                if (semid == null)
                    return null;

                var o = this.entityBuilder.CreateAddObject(parent, browseDisplayName ?? "SemanticId", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

                // explicit strings?
                this.CreateAddKeyElements(o, mode, semid.Keys);

                // find a matching concept description or other referable?
                // as we do not have all other nodes realized, store a late action
                this.entityBuilder.AddNodeLateAction(
                    new AasEntityBuilder.NodeLateActionLinkToReference(
                        parent,
                        new AdminShell.Reference(semid),
                        AasEntityBuilder.NodeLateActionLinkToReference.ActionType.SetDictionaryEntry
                    ));

                // OK
                return o;
            }
        }
    }

    public class AasUaEntityAsset : AasUaBaseEntity
    {
        public AasUaEntityAsset(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASAssetType", ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:Asset");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false, this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            // add necessary type information
            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasKind
            this.entityBuilder.AasTypes.AssetKind.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "AssetIdentificationModel", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.Asset asset = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;
            // Create whole object only if required
            if (mode == CreateMode.Instance && asset     == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "Asset", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);

            if (mode == CreateMode.Instance)
            {
                // access
                if (asset == null)
                    return null;

                // register node record
                this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, asset));

                // Referable
                this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Instance, asset);
                // Identifiable
                this.entityBuilder.AasTypes.Identification.CreateAddElements(o, CreateMode.Instance, asset.identification);
                this.entityBuilder.AasTypes.Administration.CreateAddElements(o, CreateMode.Instance, asset.administration);
                // HasKind
                this.entityBuilder.AasTypes.AssetKind.CreateAddElements(o, CreateMode.Instance, asset.kind);
                // HasDataSpecification
                if (asset.hasDataSpecification != null && asset.hasDataSpecification.reference != null)
                    foreach (var ds in asset.hasDataSpecification.reference)
                        this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, ds, "DataSpecification");
                // own attributes
                this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, asset.assetIdentificationModelRef, "AssetIdentificationModel");
            }

            return o;
        }
    }

    public class AasUaEntityAAS : AasUaBaseEntity
    {
        public AasUaEntityAAS(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASAssetAdministrationShellType", ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:AssetAdministrationShell");

            // interface
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false, this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            // add necessary type information
            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);

            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "DerivedFrom", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // assets
            this.entityBuilder.AasTypes.Asset.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            // associated views
            this.entityBuilder.AasTypes.View.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // associated submodels
            this.entityBuilder.AasTypes.Submodel.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // concept dictionary
            this.entityBuilder.AasTypes.ConceptDictionary.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.AdministrationShellEnv env, AdminShell.AdministrationShell aas)
        {
            // access
            if (env == null || aas == null)
                return null;

            // containing element
            string extraName = null;
            string browseName = "AssetAdministrationShell";
            if (aas.idShort != null && aas.idShort.Trim().Length > 0)
            {
                extraName = "AssetAdministrationShell:" + aas.idShort;
                browseName = aas.idShort;
            }
            var o = this.entityBuilder.CreateAddObject(parent, browseName, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId,
                        extraName: extraName);

            // register node record
            this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, aas));

            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Instance, aas);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddElements(o, CreateMode.Instance, aas.identification);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(o, CreateMode.Instance, aas.administration);
            // HasDataSpecification
            if (aas.hasDataSpecification != null && aas.hasDataSpecification.reference != null)
                foreach (var ds in aas.hasDataSpecification.reference)
                    this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, ds, "DataSpecification");
            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, aas.derivedFrom, "DerivedFrom");

            // associated asset
            if (aas.assetRef != null)
            {
                var asset = env.FindAsset(aas.assetRef);
                if (asset != null)
                    this.entityBuilder.AasTypes.Asset.CreateAddElements(o, CreateMode.Instance, asset);
            }

            // associated views
            if (aas.views != null)
                foreach (var vw in aas.views.views)
                    this.entityBuilder.AasTypes.View.CreateAddElements(o, CreateMode.Instance, vw);

            // associated submodels
            if (aas.submodelRefs != null && aas.submodelRefs.Count > 0)
                foreach (var smr in aas.submodelRefs)
                {
                    var sm = env.FindSubmodel(smr);
                    if (sm != null)
                        this.entityBuilder.AasTypes.Submodel.CreateAddElements(o, CreateMode.Instance, sm);
                }

            // make up CD dictionaries
            if (aas.conceptDictionaries != null && aas.conceptDictionaries.Count > 0)
            {
                foreach (var cdd in aas.conceptDictionaries)
                {
                    // TODO: reference to CDs. They are stored sepaately
                }
            } 

            // results
            return o;
        }
    }

    public class AasUaEntitySubmodel : AasUaBaseEntity
    {
        public AasUaEntitySubmodel(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASSubmodelType", ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:Submodel");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false, this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            // add some elements
            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasSemantics
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(this.typeObject, CreateMode.Type, null, "SemanticId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasKind
            this.entityBuilder.AasTypes.ModelingKind.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // Qualifiable
            this.entityBuilder.AasTypes.Qualifier.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // SubmodelElements
            this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.Submodel sm = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                // create only containing element with generic name
                var o = this.entityBuilder.CreateAddObject(parent, "Submodel", ReferenceTypeIds.HasComponent, this.GetTypeNodeId(), modellingRule: modellingRule);
                return o;
            }
            else
            {
                // access
                if (sm == null)
                    return null;

                // containing element
                var o = this.entityBuilder.CreateAddObject(parent, "" + sm.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, extraName: "Submodel:" + sm.idShort);

                // register node record
                this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, sm));

                // Referable
                this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Instance, sm);
                // Identifiable
                this.entityBuilder.AasTypes.Identification.CreateAddElements(o, CreateMode.Instance, sm.identification);
                this.entityBuilder.AasTypes.Administration.CreateAddElements(o, CreateMode.Instance, sm.administration);
                // HasSemantics
                this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(o, CreateMode.Instance, sm.semanticId, "SemanticId");
                // HasKind
                this.entityBuilder.AasTypes.ModelingKind.CreateAddElements(o, CreateMode.Instance, sm.kind);
                // HasDataSpecification
                if (sm.hasDataSpecification != null && sm.hasDataSpecification.reference != null)
                    foreach (var ds in sm.hasDataSpecification.reference)
                        this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, ds, "DataSpecification");
                // Qualifiable
                if (sm.qualifiers != null)
                    foreach (var q in sm.qualifiers)
                        this.entityBuilder.AasTypes.Qualifier.CreateAddElements(o, CreateMode.Instance, q);

                // SubmodelElements
                if (sm.submodelElements != null)
                    foreach (var smw in sm.submodelElements)
                        if (smw.submodelElement != null)
                            this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(o, CreateMode.Instance, smw);

                // result
                return o;
            }
        }
    }

    /// <summary>
    /// This class is for the representation if SME in UA namespace
    /// </summary>
    public class AasUaEntitySubmodelElement : AasUaBaseEntity
    {
        public AasUaEntitySubmodelElement(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASSubmodelElementType", ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:SubmodelElement");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false, this.entityBuilder.AasTypes.IAASReferableType.GetTypeNodeId());

            // add some elements to the type
            // Note: in this special case, the instance elements are populated by AasUaEntitySubmodelElementBase, while the elements
            // for the type are populated here
            
            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // HasSemantics
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(this.typeObject, CreateMode.Type, null, "SemanticId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasKind
            this.entityBuilder.AasTypes.ModelingKind.CreateAddElements(this.typeObject, CreateMode.Type, null, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // Qualifiable
            this.entityBuilder.AasTypes.Qualifier.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
        }
    }

    /// <summary>
    /// This class is the base class of derived properties
    /// </summary>
    public class AasUaEntitySubmodelElementBase : AasUaBaseEntity
    {
        public AasUaEntitySubmodelElementBase(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // do NOT create type object, as this is done by sub-class
        }

        public NodeState PopulateInstanceObject(NodeState o, AdminShell.SubmodelElement sme)
        {
            // access
            if (o == null || sme == null)
                return null;

            // take this as perfect opportunity to register node record
            this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, sme));

            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Instance, sme);
            // HasSemantics
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(o, CreateMode.Instance, sme.semanticId, "SemanticId");
            // HasKind
            this.entityBuilder.AasTypes.ModelingKind.CreateAddElements(o, CreateMode.Instance, sme.kind);
            // HasDataSpecification
            if (sme.hasDataSpecification != null && sme.hasDataSpecification.reference != null)
                foreach (var ds in sme.hasDataSpecification.reference)
                    this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, ds, "DataSpecification");
            // Qualifiable
            if (sme.qualifiers != null)
                foreach (var q in sme.qualifiers)
                    this.entityBuilder.AasTypes.Qualifier.CreateAddElements(o, CreateMode.Instance, q);

            // result
            return o;
        }
    }

    /// <summary>
    /// This class will automatically instantiate the correct SubmodelElement entity.
    /// </summary>
    public class AasUaEntitySubmodelWrapper : AasUaBaseEntity
    {
        public AasUaEntitySubmodelWrapper(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // do NOT create type object, as this is done by sub-class
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.SubmodelElementWrapper smw = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            // access
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                // a bit extra-reguar, for the type we're constructing the BaseType directly
                // parent.AddReference(ReferenceTypeIds.HasComponent, false, this.entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId());
                // return null;

                // create only containing element (base type) with generic name
                var o = this.entityBuilder.CreateAddObject(parent, "SubmodelElement", ReferenceTypeIds.HasComponent, this.entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), modellingRule: modellingRule);
                return o;
            }
            else
            {
                if (smw == null || smw.submodelElement == null)
                return null;

                if (smw.submodelElement is AdminShell.SubmodelElementCollection)
                {
                    var coll = smw.submodelElement as AdminShell.SubmodelElementCollection;
                    return this.entityBuilder.AasTypes.Collection.CreateAddInstanceObject(parent, coll);
                }
                else if (smw.submodelElement is AdminShell.Property)
                    return this.entityBuilder.AasTypes.Property.CreateAddInstanceObject(parent, smw.submodelElement as AdminShell.Property);
                else if (smw.submodelElement is AdminShell.File)
                    return this.entityBuilder.AasTypes.File.CreateAddInstanceObject(parent, smw.submodelElement as AdminShell.File);
                else if (smw.submodelElement is AdminShell.Blob)
                    return this.entityBuilder.AasTypes.Blob.CreateAddInstanceObject(parent, smw.submodelElement as AdminShell.Blob);
                else if (smw.submodelElement is AdminShell.ReferenceElement)
                    return this.entityBuilder.AasTypes.ReferenceElement.CreateAddInstanceObject(parent, smw.submodelElement as AdminShell.ReferenceElement);
                else if (smw.submodelElement is AdminShell.RelationshipElement)
                    return this.entityBuilder.AasTypes.RelationshipElement.CreateAddInstanceObject(parent, smw.submodelElement as AdminShell.RelationshipElement);
                else if (smw.submodelElement is AdminShell.Operation)
                    return this.entityBuilder.AasTypes.Operation.CreateAddInstanceObject(parent, smw.submodelElement as AdminShell.Operation);

                // nope
                return null;
            }
        }
    }

    public class AasUaEntityProperty : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityProperty(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASPropertyType", entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId, descriptionKey: "AAS:Property");

            // elements not in the base type
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Instance, null, "ValueId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "ValueType", DataTypeIds.String,null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "Value", DataTypeIds.BaseDataType, null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Property prop)
        {
            // access
            if (prop == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + prop.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, prop);

            // TODO: not sure if to add these
            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, prop.valueId, "ValueId");
            this.entityBuilder.CreateAddPropertyState<string>(o, "ValueType", DataTypeIds.String, "" + prop.valueType, defaultSettings: true);

            // TODO: aim is to support many types natively
            var vt = (prop.valueType ?? "").ToLower().Trim();
            if (vt == "boolean")
            {
                var x = (prop.value ?? "").ToLower().Trim();
                this.entityBuilder.CreateAddPropertyState<bool>(o, "Value", DataTypeIds.Boolean, x == "true", defaultSettings: true);
            }
            else if (vt == "datetime" || vt == "datetimestamp" || vt == "time")
            {
                DateTime dt;
                if (DateTime.TryParse(prop.value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dt))
                    this.entityBuilder.CreateAddPropertyState<Int64>(o, "Value", DataTypeIds.DateTime, dt.ToFileTimeUtc(), defaultSettings: true);
            }
            else if (vt == "decimal" || vt == "integer" || vt == "long" || vt == "nonpositiveinteger" || vt == "negativeinteger")
            {
                Int64 v;
                if (Int64.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<Int64>(o, "Value", DataTypeIds.Int64, v, defaultSettings: true);
            }
            else if (vt == "int")
            {
                Int32 v;
                if (Int32.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<Int32>(o, "Value", DataTypeIds.Int32, v, defaultSettings: true);
            }
            else if (vt == "short")
            {
                Int16 v;
                if (Int16.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<Int16>(o, "Value", DataTypeIds.Int16, v, defaultSettings: true);
            }
            else if (vt == "byte")
            {
                SByte v;
                if (SByte.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<SByte>(o, "Value", DataTypeIds.Byte, v, defaultSettings: true);
            }
            else if (vt == "nonnegativeinteger" || vt == "positiveinteger" || vt == "unsignedlong")
            {
                UInt64 v;
                if (UInt64.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<UInt64>(o, "Value", DataTypeIds.UInt64, v, defaultSettings: true);
            }
            else if (vt == "unsignedint")
            {
                UInt32 v;
                if (UInt32.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<UInt32>(o, "Value", DataTypeIds.UInt32, v, defaultSettings: true);
            }
            else if (vt == "unsignedshort")
            {
                UInt16 v;
                if (UInt16.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<UInt16>(o, "Value", DataTypeIds.UInt16, v, defaultSettings: true);
            }
            else if (vt == "unsignedbyte")
            {
                Byte v;
                if (Byte.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<Byte>(o, "Value", DataTypeIds.Byte, v, defaultSettings: true);
            }
            else if (vt == "double")
            {
                double v;
                if (double.TryParse(prop.value, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                    this.entityBuilder.CreateAddPropertyState<double>(o, "Value", DataTypeIds.Double, v, defaultSettings: true);
            }
            else if (vt == "float")
            {
                float v;
                if (float.TryParse(prop.value, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                    this.entityBuilder.CreateAddPropertyState<float>(o, "Value", DataTypeIds.Float, v, defaultSettings: true);
            }
            else
            {
                // leave in string
                this.entityBuilder.CreateAddPropertyState<string>(o, "Value", DataTypeIds.String, prop.value, defaultSettings: true);
            }

            // result
            return o;
        }
    }

    public class AasUaEntityCollection : AasUaEntitySubmodelElementBase
    {
        public NodeState typeObjectOrdered = null;

        public AasUaEntityCollection(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            // TODO: use the collection element of UA
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASSubmodelElementCollectionType", entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId, descriptionKey: "AAS:SubmodelElementCollection");
            this.typeObjectOrdered = this.entityBuilder.CreateAddObjectType("AASSubmodelElementOrderedCollectionType", this.GetTypeNodeId(), preferredTypeNumId+1, descriptionKey: "AAS:SubmodelElementCollection");

            // some elements
            foreach (var o in new NodeState[] {  this.typeObject /* , this.typeObjectOrdered */ })
            {
                this.entityBuilder.CreateAddPropertyState<bool>(o, "AllowDuplicates", DataTypeIds.Boolean, false, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            }
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.SubmodelElementCollection coll)
        {
            // access
            if (coll == null)
                return null;

            // containing element
            var to = GetTypeObject().NodeId;
            if (coll.ordered && this.typeObjectOrdered != null)
                to = this.typeObjectOrdered.NodeId;
            var o = this.entityBuilder.CreateAddObject(parent, "" + coll.idShort, ReferenceTypeIds.HasComponent, to);

            // populate common attributes
            base.PopulateInstanceObject(o, coll);

            // own attributes
            // this.entityBuilder.CreateAddPropertyState<bool>(o, "Ordered", DataTypeIds.Boolean, coll.ordered, defaultSettings: true);
            this.entityBuilder.CreateAddPropertyState<bool>(o, "AllowDuplicates", DataTypeIds.Boolean, coll.allowDuplicates, defaultSettings: true);

            // values
            if (coll.value != null)
                foreach (var smw in coll.value)
                    if (smw.submodelElement != null)
                        this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(o, CreateMode.Instance, smw);

            // result
            return o;
        }
    }

    public class AasUaEntityFile : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityFile(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASFileType", entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId, descriptionKey: "AAS:File");

            // some elements
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "MimeType", this.entityBuilder.AasTypes.MimeType.GetTypeNodeId(), null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "Value", this.entityBuilder.AasTypes.PathType.GetTypeNodeId(), null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.FileType.CreateAddElements(this.typeObject, CreateMode.Type);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.File file)
        {
            // access
            if (file == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + file.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, file);

            // own attributes
            this.entityBuilder.CreateAddPropertyState<string>(o, "MimeType", this.entityBuilder.AasTypes.MimeType.GetTypeNodeId(), file.mimeType, defaultSettings: true);
            this.entityBuilder.CreateAddPropertyState<string>(o, "Value", this.entityBuilder.AasTypes.PathType.GetTypeNodeId(), file.value, defaultSettings: true);

            // wonderful working
            if (this.entityBuilder.AasTypes.FileType.CheckSuitablity(this.entityBuilder.package, file))
                this.entityBuilder.AasTypes.FileType.CreateAddElements(o, CreateMode.Instance, this.entityBuilder.package, file);

            // result
            return o;
        }
    }

    public class AasUaEntityBlob : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityBlob(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASBlobType", entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId, descriptionKey: "AAS:Blob");

            // some elements
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "MimeType", DataTypeIds.String, null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "Value", DataTypeIds.String, null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Blob blob)
        {
            // access
            if (blob == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + blob.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, blob);

            // own attributes
            this.entityBuilder.CreateAddPropertyState<string>(o, "MimeType", DataTypeIds.String, blob.mimeType, defaultSettings: true);
            this.entityBuilder.CreateAddPropertyState<string>(o, "Value", DataTypeIds.String, blob.value, defaultSettings: true);

            // result
            return o;
        }
    }

    public class AasUaEntityReferenceElement : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityReferenceElement(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASReferenceElementType", entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId, descriptionKey: "AAS:ReferenceElement");

            // some elements
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "Value", modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.ReferenceElement refElem)
        {
            // access
            if (refElem == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + refElem.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, refElem);

            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, refElem.value, "Value");

            // result
            return o;
        }
    }

    public class AasUaEntityRelationshipElement : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityRelationshipElement(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASRelationshipElementType", entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId, descriptionKey: "AAS:RelationshipElement");

            // some elements
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "First", modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "Second", modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.RelationshipElement relElem)
        {
            // access
            if (relElem == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + relElem.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, relElem);

            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, relElem.first, "First");
            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, relElem.second, "Second");

            // result
            return o;
        }
    }

    public class AasUaEntityOperationVariable : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityOperationVariable(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("OperationVariableType", entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId, descriptionKey: "AAS:OperationVariable");
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.OperationVariable opvar)
        {
            // access
            if (opvar == null || opvar.value == null || opvar.value.submodelElement == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + opvar.value.submodelElement.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, opvar.value.submodelElement);

            // own attributes
            this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(o, CreateMode.Instance, opvar.value);

            // result
            return o;
        }
    }

    public class AasUaEntityOperation : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityOperation(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASOperationType", entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId, descriptionKey: "AAS:Operation");

            // indicate the Operation
            this.entityBuilder.CreateAddMethodState(this.typeObject, "Operation",
                    inputArgs: null /* new Argument[] { }*/,
                    outputArgs: null /* new Argument[] { }*/,
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent);

            // some elements
            for (int i = 0; i < 2; i++)
            {
                var o2 = this.entityBuilder.CreateAddObject(this.typeObject, (i == 0) ? "in" : "out", ReferenceTypeIds.HasComponent, this.entityBuilder.AasTypes.OperationVariable.GetTypeObject().NodeId);
                this.entityBuilder.AasTypes.OperationVariable.CreateAddInstanceObject(o2, null);
            }
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Operation op)
        {
            // access
            if (op == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + op.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, op);

            // own AAS attributes (in/out op vars)
            for (int i=0; i<2; i++)
            {
                var opvarList = op[i];
                if (opvarList != null && opvarList.Count > 0)
                {
                    var o2 = this.entityBuilder.CreateAddObject(o, (i == 0) ? "OperationInputVariables" : "OperationOutputVariables", 
                        ReferenceTypeIds.HasComponent, /* TODO */ GetTypeObject().NodeId);
                    foreach (var opvar in opvarList)
                        // this.entityBuilder.AasTypes.OperationVariable.CreateAddInstanceObject(o2, opvar);
                        if (opvar != null && opvar.value != null)
                            this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(o2, CreateMode.Instance, opvar.value);
                }
            }

            // create a method?
            if (true)
            {
                var args = new List<Argument>[] { new List<Argument>(), new List<Argument>() };
                for (int i = 0; i < 2; i++)
                    if (op[i] != null)
                        foreach (var opvar in op[i])
                        {
                            // TODO: decide to from where the name comes
                            var name = "noname";
                            
                            // TODO: description: get "en" version is appropriate?
                            LocalizedText desc = new LocalizedText("");

                            // TODO: parse UA data type out .. OK?
                            NodeId dataType = null; 
                            if (opvar.value != null && opvar.value.submodelElement != null)
                            {
                                // better name .. but not best (see below)
                                if (opvar.value.submodelElement.idShort != null && opvar.value.submodelElement.idShort.Trim() != "")
                                    name = "" + opvar.value.submodelElement.idShort;

                                // TODO: description: get "en" version is appropriate?
                                desc = AasUaUtils.GetBestUaDescriptionFromAasDescription(opvar.value.submodelElement.description);

                                // currenty, only accept properties as in/out arguments. Only these have an XSD value type!!
                                var prop = opvar.value.submodelElement as AdminShell.Property;
                                if (prop != null && prop.valueType != null)
                                {
                                    // TODO: this any better?
                                    if (prop.idShort != null && prop.idShort.Trim() != "")
                                        name = "" + prop.idShort;

                                    // TODO: description: get "en" version is appropriate?
                                    if (desc.Text == null || desc.Text == "")
                                        desc = AasUaUtils.GetBestUaDescriptionFromAasDescription(opvar.value.submodelElement.description);

                                    // try convert type
                                    Type sharpType;
                                    if (!AasUaUtils.AasValueTypeToUaDataType(prop.valueType, out sharpType, out dataType))
                                        dataType = null;
                                }
                            }
                            if (dataType == null)
                                continue;

                            var a = new Argument(name, dataType, -1, desc.Text ?? "");
                            args[i].Add(a);
                        }

                var opmeth = this.entityBuilder.CreateAddMethodState(o, "Operation",
                    inputArgs: args[0].ToArray(),
                    outputArgs: args[1].ToArray(),
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent);
            }

            // result
            return o;
        }
    }

    public class AasUaEntityView : AasUaBaseEntity
    {
        public AasUaEntityView(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASViewType", ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:View");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false, this.entityBuilder.AasTypes.IAASReferableType.GetTypeNodeId());

            // add some elements
            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // HasSemantics
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(this.typeObject, CreateMode.Type, null, "SemanticId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // contained elements
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "ContainedElement", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.View view = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                //parent.AddReference(ReferenceTypeIds.HasComponent, false, this.GetTypeNodeId());
                //return null;

                // create only containing element with generic name
                var o = this.entityBuilder.CreateAddObject(parent, "View", ReferenceTypeIds.HasComponent, this.GetTypeNodeId(), modellingRule: modellingRule);
                return o;

            }
            else
            {
                // access
                if (view == null)
                    return null;

                // containing element
                var o = this.entityBuilder.CreateAddObject(parent, "" + view.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, extraName: "View:" + view.idShort);

                // register node record
                this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, view));

                // Referable
                this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Instance, view);
                // HasSemantics
                this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(o, CreateMode.Instance, view.semanticId, "SemanticId");
                // HasDataSpecification
                if (view.hasDataSpecification != null && view.hasDataSpecification.reference != null)
                    foreach (var ds in view.hasDataSpecification.reference)
                        this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, ds, "DataSpecification");

                // contained elements
                for (int i = 0; i < view.Count; i++)
                {
                    var cer = view[i];
                    if (cer != null)
                        this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, cer, "ContainedElement");
                }

                // OK
                return o;
            }
        }
    }

    public class AasUaEntityConceptDictionary : AasUaBaseEntity
    {
        public AasUaEntityConceptDictionary(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASConceptDictionaryType", ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:ConceptDictionary");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false, this.entityBuilder.AasTypes.IAASReferableType.GetTypeNodeId());

            // add necessary type information
            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // Dictionary Entries
            this.entityBuilder.CreateAddObject(this.typeObject, "DictionaryEntry", ReferenceTypeIds.HasComponent, this.entityBuilder.AasTypes.DictionaryEntryType.GetTypeNodeId(), modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.ConceptDictionary cdd = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;
            // Create whole object only if required
            if (mode == CreateMode.Instance && cdd == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "ConceptDictionary", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);

            if (mode == CreateMode.Instance)
            {
                // access
                if (cdd == null)
                    return null;

                // Referable
                this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Instance, cdd);
            }

            return o;
        }
    }

    public class AasUaEntityDataSpecification : AasUaBaseEntity
    {
        public AasUaEntityDataSpecification(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASDataSpecificationType", ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:DataSpecification");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false, this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());
        }

    }

    public class AasUaEntityDataSpecificationIEC61360 : AasUaBaseEntity
    {
        public AasUaEntityDataSpecificationIEC61360(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASDataSpecificationIEC61360Type", this.entityBuilder.AasTypes.DataSpecification.GetTypeNodeId(), preferredTypeNumId, descriptionKey: "AAS:DataSpecificationIEC61360");

            // very special rule here for the Identifiable
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false, this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Instance,
                new AdminShell.Identification("URI", "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360"),
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Instance,
                new AdminShell.Administration("1", "0"),
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            // add some more elements
            this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(this.typeObject, "PreferredName", DataTypeIds.LocalizedText,
                    value: null, defaultSettings: true, valueRank: 1, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "ShortName", DataTypeIds.String, value: null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "Unit", DataTypeIds.String, value: null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "UnitId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(this.typeObject, "SourceOfDefinition", DataTypeIds.LocalizedText,
                value: null, defaultSettings: true, valueRank: 1, modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "Symbol", DataTypeIds.String, value: null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "DataType", DataTypeIds.String, value: null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(this.typeObject, "Definition", DataTypeIds.LocalizedText,
                value: null, defaultSettings: true, valueRank: 1, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, "ValueFormat", DataTypeIds.String, value: null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.DataSpecificationIEC61360 ds = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            // for the sake of clarity, we're directly splitting cases
            if (mode == CreateMode.Type)
            {
                // containing element (only)
                var o = this.entityBuilder.CreateAddObject(parent, "DataSpecificationIEC61360", this.entityBuilder.AasTypes.HasAddIn.GetTypeNodeId(), GetTypeObject().NodeId, modellingRule: modellingRule);
                return o;
            }
            else
            {
                // access
                if (ds == null)
                    return null;

                // we can only provide minimal unique naming 
                var name = "DataSpecificationIEC61360";
                if (ds.shortName != null && this.entityBuilder.RootDataSpecifications != null)
                    name += "_" + ds.shortName;

                // containing element (depending on root folder)
                NodeState o = null;
                if (this.entityBuilder.RootDataSpecifications != null)
                {
                    // under common folder
                    o = this.entityBuilder.CreateAddObject(this.entityBuilder.RootDataSpecifications, name, ReferenceTypeIds.Organizes, GetTypeObject().NodeId);
                    // link to this object
                    parent.AddReference(this.entityBuilder.AasTypes.HasAddIn.GetTypeNodeId(), false, o.NodeId);
                }
                else
                {
                    // under parent
                    o = this.entityBuilder.CreateAddObject(parent, name, this.entityBuilder.AasTypes.HasAddIn.GetTypeNodeId(), GetTypeObject().NodeId);
                }

                // add some elements        
                if (ds.preferredName != null && ds.preferredName.Count > 0)
                    this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(o, "PreferredName", DataTypeIds.LocalizedText,
                        value: AasUaUtils.GetUaLocalizedTexts(ds.preferredName?.langString), defaultSettings: true, valueRank: 1);

                if (ds.shortName != null && ds.shortName.Count > 0)
                    this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(o, "ShortName", DataTypeIds.LocalizedText,
                        value: AasUaUtils.GetUaLocalizedTexts(ds.shortName?.langString), defaultSettings: true, valueRank: 1);

                if (ds.unit != null)
                    this.entityBuilder.CreateAddPropertyState<string>(o, "Unit", DataTypeIds.String, value: ds.unit, defaultSettings: true);

                if (ds.unitId != null)
                    this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, AdminShell.Reference.CreateNew(ds.unitId?.Keys), "UnitId");

                if (ds.sourceOfDefinition != null)
                    this.entityBuilder.CreateAddPropertyState<string>(o, "SourceOfDefinition", DataTypeIds.String, value: ds.sourceOfDefinition, defaultSettings: true);

                if (ds.symbol != null)
                    this.entityBuilder.CreateAddPropertyState<string>(o, "Symbol", DataTypeIds.String, value: ds.symbol, defaultSettings: true);

                if (ds.dataType != null)
                    this.entityBuilder.CreateAddPropertyState<string>(o, "DataType", DataTypeIds.String, value: ds.dataType, defaultSettings: true);

                if (ds.definition != null && ds.definition.Count > 0)
                    this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(o, "Definition", DataTypeIds.LocalizedText,
                        value: AasUaUtils.GetUaLocalizedTexts(ds.definition?.langString), defaultSettings: true, valueRank: 1);

                if (ds.valueFormat != null)
                    this.entityBuilder.CreateAddPropertyState<string>(o, "ValueFormat", DataTypeIds.String, value: ds.valueFormat, defaultSettings: true);

                // return
                return o;
            }
        }
    }

    public class AasUaEntityConceptDescription : AasUaBaseEntity
    {
        public NodeState typeObjectIrdi;
        public NodeState typeObjectUri;
        public NodeState typeObjectCustom;


        public AasUaEntityConceptDescription(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            // TODO: make super classes for UriDictionaryEntryType..
            this.typeObjectIrdi = this.entityBuilder.CreateAddObjectType("AASIrdiConceptDescriptionType", this.entityBuilder.AasTypes.IrdiDictionaryEntryType.GetTypeNodeId(), 0, descriptionKey: "AAS:ConceptDescription");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObjectIrdi, false, this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            this.typeObjectUri = this.entityBuilder.CreateAddObjectType("AASUriConceptDescriptionType", this.entityBuilder.AasTypes.UriDictionaryEntryType.GetTypeNodeId(), 0, descriptionKey: "AAS:ConceptDescription");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObjectUri, false, this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            this.typeObjectCustom = this.entityBuilder.CreateAddObjectType("AASCustomConceptDescriptionType", this.entityBuilder.AasTypes.DictionaryEntryType.GetTypeNodeId(), 0, descriptionKey: "AAS:ConceptDescription");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObjectCustom, false, this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            // for each of them, add some elements
            foreach (var o in new NodeState[] { this.typeObjectIrdi, this.typeObjectUri, this.typeObjectCustom })
            {
                // Referable
                this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Type);
                // Identifiable
                this.entityBuilder.AasTypes.Identification.CreateAddElements(o, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
                this.entityBuilder.AasTypes.Administration.CreateAddElements(o, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Optional);
                // IsCaseOf
                this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Type, null, "IsCaseOf", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
                // HasDataSpecification
                this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Type, null, "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);

                // data specification is a child
                this.entityBuilder.AasTypes.DataSpecificationIEC61360.CreateAddElements(o, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.MandatoryPlaceholder);
            }
        }

        public NodeState GetTypeObjectFor(AdminShell.Identification identification)
        {
            var to = this.typeObject; // shall be NULL
            if (identification != null && identification.idType != null && identification.idType.Trim().ToUpper() == "URI")
                to = this.typeObjectUri;
            if (identification != null && identification.idType != null && identification.idType.Trim().ToUpper() == "IRDI")
                to = this.typeObjectIrdi;
            if (identification != null && identification.idType != null && identification.idType.Trim().ToUpper() == "CUSTOM")
                to = this.typeObjectCustom;
            return to;
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AdminShell.ConceptDescription cd = null, AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            // split directly because of complexity
            if (mode == CreateMode.Type)
            {
                // not sure, if this will required, ever
                return null;
            }
            else
            {
                // access
                if (cd == null)
                    return null;

                // makeup name            
                var name = "ConceptDescription_" + Guid.NewGuid().ToString();

                if (false)
                {
                    // Conventional approach: build up a speaking name
                    // but: shall be target of "HasDictionaryEntry", therefore the __PURE__ identifications need to be the name!
                    if (cd.embeddedDataSpecification != null && cd.embeddedDataSpecification.dataSpecificationContent != null && cd.embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360 != null)
                    {
                        var ds = cd.embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360;
                        if (ds.shortName != null)
                            name = ds.shortName.GetDefaultStr();
                        if (cd.identification != null)
                            name += "_" + cd.identification.ToString();
                    }
                    name = AasUaUtils.ToOpcUaName(name);
                }
                else
                {
                    // only identification (the type object will distinct between the id type)
                    if (cd.identification != null)
                        name = cd.identification.id;
                }

                // containing element
                var o = this.entityBuilder.CreateAddObject(parent, name, ReferenceTypeIds.HasComponent, this.GetTypeObjectFor(cd.identification)?.NodeId, modellingRule: modellingRule);

                // Referable
                this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Instance, cd);
                // Identifiable
                this.entityBuilder.AasTypes.Identification.CreateAddElements(o, CreateMode.Instance, cd.identification);
                this.entityBuilder.AasTypes.Administration.CreateAddElements(o, CreateMode.Instance, cd.administration);
                // IsCaseOf
                if (cd.IsCaseOf != null)
                    foreach (var ico in cd.IsCaseOf)
                        this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, ico, "IsCaseOf");
                // HasDataSpecification
                if (cd.embeddedDataSpecification != null && cd.embeddedDataSpecification.dataSpecification != null)
                    this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, cd.embeddedDataSpecification.dataSpecification, "DataSpecification");

                // data specification is a child
                if (cd.embeddedDataSpecification != null && cd.embeddedDataSpecification.dataSpecificationContent != null && cd.embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360 != null)
                {
                    var dso = this.entityBuilder.AasTypes.DataSpecificationIEC61360.CreateAddElements(o, CreateMode.Instance, cd.embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360);
                }

                // remeber CD as NodeRecord
                this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, cd.identification));

                return o;
            }
        }
    }

    // 
    // Elements from the UA spc
    //

    public class AasUaNamespaceZeroEntity : AasUaBaseEntity
    {
        public AasUaNamespaceZeroEntity(AasEntityBuilder entityBuilder, uint presetNumId = 0)
            : base(entityBuilder)
        {
            // just set node id based on existing knowledge
            this.typeObjectId = new NodeId(presetNumId, 0);
        }
    }

    public class AasUaNamespaceZeroReference : AasUaBaseEntity
    {
        public AasUaNamespaceZeroReference(AasEntityBuilder entityBuilder, uint presetNumId = 0)
            : base(entityBuilder)
        {
            // just set node id based on existing knowledge
            this.typeObjectId = new NodeId(presetNumId, 0);
        }

        public void CreateAddInstanceReference(NodeState source, bool isInverse, ExpandedNodeId target)
        {
            if (source != null && target != null && this.GetTypeNodeId() != null)
                source.AddReference(this.GetTypeNodeId(), isInverse, target);
        }
    }

    //
    // References
    // 

    public class AasUaReferenceHasAasReference : AasUaBaseEntity
    {
        public AasUaReferenceHasAasReference(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddReferenceType("AASReference", "AASReferencedBy", preferredTypeNumId, useZeroNS: false);
        }

        public NodeState CreateAddInstanceReference(NodeState parent)
        {
            return null;
        }
    }
    

    //
    // Interfaces   
    //

    public class AasUaInterfaceAASIdentifiableType : AasUaBaseEntity
    {
        public AasUaInterfaceAASIdentifiableType(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("IAASIdentifiableType", this.entityBuilder.AasTypes.IAASReferableType.GetTypeNodeId() /* ObjectTypeIds.BaseObjectType */, preferredTypeNumId, descriptionKey: "AAS:Identifiable");

            // add some elements
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Optional);
        }
    }

    public class AasUaInterfaceAASReferableType : AasUaBaseEntity
    {
        public AasUaInterfaceAASReferableType(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("IAASReferableType", this.entityBuilder.AasTypes.BaseInterfaceType.GetTypeNodeId() /* ObjectTypeIds.BaseObjectType */, preferredTypeNumId, descriptionKey: "AAS:Referable");

            // some elements
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
        }
    }
}
