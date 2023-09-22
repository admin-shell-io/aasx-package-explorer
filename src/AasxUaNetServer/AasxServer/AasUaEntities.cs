/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using Opc.Ua;
using Aas = AasCore.Aas3_0;
using AasxIntegrationBase;

// TODO (MIHO, 2020-08-29): The UA mapping needs to be overworked in order to comply the joint aligment with I4AAS
// TODO (MIHO, 2020-08-29): The UA mapping needs to be checked for the "new" HasDataSpecification strcuture of V2.0.1

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASIdentifierType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:Identifier");
            // add some elements

            if (true == entityBuilder.theServerOptions?.SimpleDataTypes)
            {
                this.entityBuilder.CreateAddDataType("IdType", DataTypeIds.String);
                this.entityBuilder.CreateAddDataType("Id", DataTypeIds.String);
            }
            else
            {
                // this is the original code, which gets not imported by SiOME
                this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "IdType",
                    DataTypeIds.String, null, defaultSettings: true,
                    modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
                this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Id",
                    DataTypeIds.String, null, defaultSettings: true,
                    modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            }
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode,
            string id = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            var o = this.entityBuilder.CreateAddObject(parent, mode, "Identification",
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);

            if (mode == CreateMode.Instance)
            {
                if (id != null)
                {
                    this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Id",
                        DataTypeIds.String, "" + "" + id, defaultSettings: true);
                }
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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASAdministrativeInformationType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:AdministrativeInformation");
            // add some elements
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Version",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Revision",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode,
            Aas.IAdministrativeInformation administration = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;
            // Create whole object only if required
            if (mode == CreateMode.Instance && administration == null)
                return null;

            var o = this.entityBuilder.CreateAddObject(parent, mode, "Administration",
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);

            if (mode == CreateMode.Instance)
            {
                if (administration == null)
                    return null;
                this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Version",
                    DataTypeIds.String, "" + "" + administration.Version, defaultSettings: true);
                this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Revision",
                    DataTypeIds.String, "" + "" + administration.Revision, defaultSettings: true);
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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASQualifierType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, "AAS:Qualifier");

            // add some elements
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(this.typeObject,
                CreateMode.Type, null, "SemanticId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Type",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Value",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type,
                null, "ValueId", AasUaNodeHelper.ModellingRule.Optional);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, Aas.IQualifier qualifier = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;
            // Create whole object only if required
            if (mode == CreateMode.Instance && qualifier == null)
                return null;

            if (mode == CreateMode.Type)
            {
                // plain
                var o = this.entityBuilder.CreateAddObject(parent, mode, "Qualifier", ReferenceTypeIds.HasComponent,
                    GetTypeObject().NodeId, modellingRule: modellingRule);
                return o;
            }
            else
            {
                // need data
                if (qualifier == null)
                    return null;

                // do a little extra?
                string extraName = null;
                if (qualifier.Type != null && qualifier.Type.Length > 0)
                {
                    extraName = "Qualifier:" + qualifier.Type;
                    if (qualifier.Value != null && qualifier.Value.Length > 0)
                        extraName += "=" + qualifier.Value;
                }

                var o = this.entityBuilder.CreateAddObject(parent, mode, "Qualifier",
                    ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule,
                    extraName: extraName);

                this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(o,
                    CreateMode.Instance, qualifier.SemanticId, "SemanticId");
                this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Type",
                    DataTypeIds.String, "" + qualifier.Type, defaultSettings: true);
                this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Value",
                    DataTypeIds.String, "" + qualifier.Value, defaultSettings: true);
                this.entityBuilder.AasTypes.Reference.CreateAddElements(o,
                    CreateMode.Instance, qualifier.ValueId, "ValueId");

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

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, Aas.AssetKind? kind = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (mode == CreateMode.Instance && kind == null)
                return null;

            var o = this.entityBuilder.CreateAddPropertyState<string>(parent, mode, "Kind",
                DataTypeIds.String, 
                (mode == CreateMode.Type) 
                    ? null 
                    : "" + (kind != null ? Stringification.ToString(kind.Value) : ""), 
                defaultSettings: true,
                modellingRule: modellingRule);

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

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, Aas.ModellingKind? kind = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (mode == CreateMode.Instance && kind == null)
                return null;

            var o = this.entityBuilder.CreateAddPropertyState<string>(parent, mode, "Kind",
                DataTypeIds.String, 
                (mode == CreateMode.Type) 
                    ? null 
                    : "" + ( kind != null ? Stringification.ToString(kind) : ""), 
                defaultSettings: true,
                modellingRule: modellingRule);

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
        public NodeState CreateAddElements(NodeState parent, CreateMode mode, Aas.IReferable refdata = null)
        {
            if (parent == null)
                return null;
            if (mode == CreateMode.Instance && refdata == null)
                return null;

            if (mode == CreateMode.Type || refdata?.Category != null)
                this.entityBuilder.CreateAddPropertyState<string>(parent, mode, "Category",
                    DataTypeIds.String, (mode == CreateMode.Type) ? null : "" + refdata?.Category,
                    defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            // No idShort as typically in the DisplayName of the node

            if (mode == CreateMode.Instance)
            {
                // now, re-set the description on the parent
                // ISSUE: only ONE language supported!
                parent.Description = AasUaUtils.GetBestUaDescriptionFromAasDescription(refdata?.Description);
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
        /// Sets the "Keys" value information of an AAS Reference. This is especially important for referencing 
        /// outwards of the AAS (environment).
        /// </summary>
        public void CreateAddKeyElements(NodeState parent, CreateMode mode, Aas.IReference rf = null)
        {
            if (parent == null)
                return;

            // MIHO: open62541 does not to process Values as string[], therefore change it temporarily

            if (this.entityBuilder != null && this.entityBuilder.theServerOptions != null
                && this.entityBuilder.theServerOptions.ReferenceKeysAsSingleString)
            {
				// fix for open62541

				var typeo = this.entityBuilder.CreateAddPropertyState<string>(parent, mode, "Type",
					DataTypeIds.String, null, defaultSettings: true);

				var keyo = this.entityBuilder.CreateAddPropertyState<string>(parent, mode, "Keys",
                    DataTypeIds.String, null, defaultSettings: true);

                if (mode == CreateMode.Instance && keyo != null)
                {
                    typeo.Value = Stringification.ToString(rf.Type);
                    keyo.Value = AasUaUtils.ToOpcUaReference(rf);
                }
            }
            else
            {
                // default behaviour
                var keyo = this.entityBuilder?.CreateAddPropertyState<string[]>(parent, mode, "Keys",
                    DataTypeIds.Structure, null, defaultSettings: true);

                if (mode == CreateMode.Instance && keyo != null)
                {
                    keyo.Value = AasUaUtils.ToOpcUaReferenceList(rf)?.ToArray();
                }
            }
        }

        /// <summary>
        /// Sets the UA relation of an AAS Reference. This is especially important for reference within an AAS node 
        /// structure, to be
        /// in the style of OPC UA
        /// </summary>
        public void CreateAddReferenceElements(NodeState parent, CreateMode mode, List<Aas.IKey> keys = null)
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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASReferenceType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId);
            // with some elements
            this.CreateAddKeyElements(this.typeObject, CreateMode.Type);
            this.CreateAddReferenceElements(this.typeObject, CreateMode.Type);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode,
            Aas.IReference reference, string browseDisplayName = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                var o = this.entityBuilder.CreateAddObject(parent, mode, browseDisplayName ?? "Reference",
                    ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);
                return o;
            }
            else
            {
                if (reference == null)
                    return null;

                var o = this.entityBuilder.CreateAddObject(parent, mode, browseDisplayName ?? "Reference",
                    ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

                // explicit strings?
                this.CreateAddKeyElements(o, mode, reference);

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
            this.CreateAddReferenceElements(this.typeObject, CreateMode.Type);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, CreateMode mode,
            Aas.IReference semid = null, string browseDisplayName = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                var o = this.entityBuilder.CreateAddObject(parent, mode, browseDisplayName ?? "SemanticId",
                    ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);
                return o;
            }
            else
            {
                if (semid == null)
                    return null;

                var o = this.entityBuilder.CreateAddObject(parent, mode, browseDisplayName ?? "SemanticId",
                    ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

                // explicit strings?
                this.CreateAddKeyElements(o, mode, semid);

                // find a matching concept description or other referable?
                // as we do not have all other nodes realized, store a late action
                this.entityBuilder.AddNodeLateAction(
                    new AasEntityBuilder.NodeLateActionLinkToReference(
                        parent,
                        semid.Copy(),
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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASAssetType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:Asset");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            // add necessary type information
            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasKind
            this.entityBuilder.AasTypes.AssetKind.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type,
                null, "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type,
                null, "AssetIdentificationModel", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, Aas.IAssetInformation asset = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;
            // Create whole object only if required
            if (mode == CreateMode.Instance && asset == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, mode, "Asset", ReferenceTypeIds.HasComponent,
                GetTypeObject().NodeId, modellingRule: modellingRule);

            if (mode == CreateMode.Instance)
            {
                // access
                if (asset == null)
                    return null;

                // register node record
                this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, asset.GlobalAssetId));

                // Referable
                // this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Instance, asset);                
                // Identifiable (V3.0: not anymore)
                // this.entityBuilder.AasTypes.Identification.CreateAddElements(
                //    o, CreateMode.Instance, asset.identification);
                // this.entityBuilder.AasTypes.Administration.CreateAddElements(
                //    o, CreateMode.Instance, asset.administration);
                
                // HasKind
                this.entityBuilder.AasTypes.AssetKind.CreateAddElements(o, CreateMode.Instance, asset.AssetKind);
                
                // own attributes
                //this.entityBuilder.AasTypes.Reference.CreateAddElements(
                //    o, CreateMode.Instance, asset.assetIdentificationModelRef, "AssetIdentificationModel");
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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASAssetAdministrationShellType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:AssetAdministrationShell");

            // interface
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            // add necessary type information
            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);

            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "DerivedFrom", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // assets
            this.entityBuilder.AasTypes.Asset.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            // associated submodels
            this.entityBuilder.AasTypes.Submodel.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
        }

        public NodeState CreateAddInstanceObject(NodeState parent,
            Aas.Environment env, Aas.IAssetAdministrationShell aas)
        {
            // access
            if (env == null || aas == null)
                return null;

            // containing element
            string extraName = null;
            string browseName = "AssetAdministrationShell";
            if (aas.IdShort != null && aas.IdShort.Trim().Length > 0)
            {
                extraName = "AssetAdministrationShell:" + aas.IdShort;
                browseName = aas.IdShort;
            }
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance,
                browseName, ReferenceTypeIds.HasComponent,
                GetTypeObject().NodeId, extraName: extraName);

            // register node record
            this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, aas));

            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Instance, aas);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddElements(
                o, CreateMode.Instance, aas.Id);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(
                o, CreateMode.Instance, aas.Administration);
            // HasDataSpecification
            if (aas.EmbeddedDataSpecifications != null)
                foreach (var ds in aas.EmbeddedDataSpecifications)
                    this.entityBuilder.AasTypes.Reference.CreateAddElements(
                        o, CreateMode.Instance, ds?.DataSpecification, "DataSpecification");
            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(
                o, CreateMode.Instance, aas.DerivedFrom, "DerivedFrom");

            // associated asset
            // TODO (??, 0000-00-00): AssetInformation
            //if (aas.assetRef != null)
            //{
            //    var asset = env.FindAsset(aas.assetRef);
            //    if (asset != null)
            //        this.entityBuilder.AasTypes.Asset.CreateAddElements(
            //            o, CreateMode.Instance, asset);
            //}

            // associated submodels
            if (aas.Submodels != null)
                foreach (var smr in aas.Submodels)
                {
                    var sm = env.FindSubmodel(smr);
                    if (sm != null)
                        this.entityBuilder.AasTypes.Submodel.CreateAddElements(
                            o, CreateMode.Instance, sm);
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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASSubmodelType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:Submodel");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            // add some elements
            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasSemantics
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(this.typeObject, CreateMode.Type, null,
                "SemanticId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasKind
            this.entityBuilder.AasTypes.ModelingKind.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // Qualifiable
            this.entityBuilder.AasTypes.Qualifier.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // SubmodelElements
            this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, Aas.ISubmodel sm = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                // create only containing element with generic name
                var o = this.entityBuilder.CreateAddObject(parent, mode, "Submodel", ReferenceTypeIds.HasComponent,
                    this.GetTypeNodeId(), modellingRule: modellingRule);
                return o;
            }
            else
            {
                // access
                if (sm == null)
                    return null;

                // containing element
                var o = this.entityBuilder.CreateAddObject(parent, mode,
                    "" + sm.IdShort, ReferenceTypeIds.HasComponent,
                    GetTypeObject().NodeId, extraName: "Submodel:" + sm.IdShort);

                // register node record
                this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, sm));

                // Referable
                this.entityBuilder.AasTypes.Referable.CreateAddElements(
                    o, CreateMode.Instance, sm);
                // Identifiable
                this.entityBuilder.AasTypes.Identification.CreateAddElements(
                    o, CreateMode.Instance, sm.Id);
                this.entityBuilder.AasTypes.Administration.CreateAddElements(
                    o, CreateMode.Instance, sm.Administration);
                // HasSemantics
                this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(
                    o, CreateMode.Instance, sm.SemanticId, "SemanticId");
                // HasKind
                this.entityBuilder.AasTypes.ModelingKind.CreateAddElements(
                    o, CreateMode.Instance, sm.Kind);
                // HasDataSpecification
                if (sm.EmbeddedDataSpecifications != null)
                    foreach (var ds in sm.EmbeddedDataSpecifications)
                        this.entityBuilder.AasTypes.Reference.CreateAddElements(
                            o, CreateMode.Instance, ds?.DataSpecification, "DataSpecification");
                // Qualifiable
                if (sm.Qualifiers != null)
                    foreach (var q in sm.Qualifiers)
                        this.entityBuilder.AasTypes.Qualifier.CreateAddElements(
                            o, CreateMode.Instance, q);

                // SubmodelElements
                if (sm.SubmodelElements != null)
                    foreach (var smw in sm.SubmodelElements)
                        this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(
                            o, CreateMode.Instance, smw);

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASSubmodelElementType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:SubmodelElement");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false,
                this.entityBuilder.AasTypes.IAASReferableType.GetTypeNodeId());

            // add some elements to the type
            // Note: in this special case, the instance elements are populated by AasUaEntitySubmodelElementBase, 
            // while the elements
            // for the type are populated here

            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // HasSemantics
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(this.typeObject, CreateMode.Type, null,
                "SemanticId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasKind
            this.entityBuilder.AasTypes.ModelingKind.CreateAddElements(this.typeObject, CreateMode.Type, null,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // Qualifiable
            this.entityBuilder.AasTypes.Qualifier.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
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

        public NodeState PopulateInstanceObject(NodeState o, Aas.ISubmodelElement sme)
        {
            // access
            if (o == null || sme == null)
                return null;

            // take this as perfect opportunity to register node record
            this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, sme));

            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(
                o, CreateMode.Instance, sme);
            // HasSemantics
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(
                o, CreateMode.Instance, sme.SemanticId, "SemanticId");
            // HasDataSpecification
            if (sme.EmbeddedDataSpecifications != null)
                foreach (var ds in sme.EmbeddedDataSpecifications)
                    this.entityBuilder.AasTypes.Reference.CreateAddElements(
                        o, CreateMode.Instance, ds?.DataSpecification, "DataSpecification");
            // Qualifiable
            if (sme.Qualifiers != null)
                foreach (var q in sme.Qualifiers)
                    this.entityBuilder.AasTypes.Qualifier.CreateAddElements(
                        o, CreateMode.Instance, q);

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

        public NodeState CreateAddElements(NodeState parent, CreateMode mode,
            Aas.ISubmodelElement smw = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            // access
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                // create only containing element (base type) with generic name
                var o = this.entityBuilder.CreateAddObject(parent, mode,
                    "SubmodelElement", ReferenceTypeIds.HasComponent,
                    this.entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), modellingRule: modellingRule);
                return o;
            }
            else
            {
                if (smw == null)
                    return null;

                if (smw is Aas.ISubmodelElementCollection coll)
                    return this.entityBuilder.AasTypes.Collection.CreateAddInstanceObject(parent, coll);
				else if (smw is Aas.ISubmodelElementList list)
					return this.entityBuilder.AasTypes.SmeList.CreateAddInstanceObject(parent, list);
				else if (smw is Aas.IProperty)
                    return this.entityBuilder.AasTypes.Property.CreateAddInstanceObject(
                        parent, smw as Aas.IProperty);
                else if (smw is Aas.IFile)
                    return this.entityBuilder.AasTypes.File.CreateAddInstanceObject(
                        parent, smw as Aas.IFile);
                else if (smw is Aas.IBlob)
                    return this.entityBuilder.AasTypes.Blob.CreateAddInstanceObject(
                        parent, smw as Aas.IBlob);
                else if (smw is Aas.IReferenceElement)
                    return this.entityBuilder.AasTypes.ReferenceElement.CreateAddInstanceObject(
                        parent, smw as Aas.IReferenceElement);
                else if (smw is Aas.IRelationshipElement)
                    return this.entityBuilder.AasTypes.RelationshipElement.CreateAddInstanceObject(
                        parent, smw as Aas.IRelationshipElement);
                else if (smw is Aas.IOperation)
                    return this.entityBuilder.AasTypes.Operation.CreateAddInstanceObject(
                        parent, smw as Aas.IOperation);

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
            this.typeObject = this.entityBuilder.CreateAddObjectType(
                "AASPropertyType", entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:Property");

            // elements not in the base type
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Instance, null,
                "ValueId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "ValueType",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Value",
                DataTypeIds.BaseDataType, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, Aas.IProperty prop)
        {
            // access
            if (prop == null)
                return null;

            // for all
            var mode = CreateMode.Instance;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, mode, "" + prop.IdShort, ReferenceTypeIds.HasComponent,
                GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, prop);

            // TODO (MIHO, 2020-08-06): not sure if to add these
            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, prop.ValueId, "ValueId");
            this.entityBuilder.CreateAddPropertyState<string>(o, mode, "ValueType",
                DataTypeIds.String, "" + prop.ValueType, defaultSettings: true);

            // aim is to support many types natively
            
            if (prop.ValueType == DataTypeDefXsd.Boolean)
            {
                var x = (prop.Value ?? "").ToLower().Trim();
                this.entityBuilder.CreateAddPropertyState<bool>(o, mode, "Value",
                    DataTypeIds.Boolean, x == "true", defaultSettings: true);
            }
            else if (prop.ValueType == DataTypeDefXsd.DateTime 
                     || prop.ValueType == DataTypeDefXsd.Date 
                     || prop.ValueType == DataTypeDefXsd.Time)
            {
                if (DateTime.TryParse(prop.Value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var dt))
                    this.entityBuilder.CreateAddPropertyState<Int64>(o, mode, "Value",
                        DataTypeIds.DateTime, dt.ToFileTimeUtc(), defaultSettings: true);
            }
            else if (prop.ValueType == DataTypeDefXsd.Decimal 
                     || prop.ValueType == DataTypeDefXsd.Integer 
                     || prop.ValueType == DataTypeDefXsd.Long
                     || prop.ValueType == DataTypeDefXsd.NegativeInteger)
            {
                if (Int64.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<Int64>(o, mode, "Value",
                        DataTypeIds.Int64, v, defaultSettings: true);
            }
            else if (prop.ValueType == DataTypeDefXsd.Int)
            {
                if (Int32.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<Int32>(o, mode, "Value",
                        DataTypeIds.Int32, v, defaultSettings: true);
            }
            else if (prop.ValueType == DataTypeDefXsd.Short)
            {
                if (Int16.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<Int16>(o, mode, "Value",
                        DataTypeIds.Int16, v, defaultSettings: true);
            }
            else if (prop.ValueType == DataTypeDefXsd.Byte)
            {
                if (SByte.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<SByte>(o, mode, "Value",
                        DataTypeIds.Byte, v, defaultSettings: true);
            }
            else if (prop.ValueType == DataTypeDefXsd.NonNegativeInteger 
                     || prop.ValueType == DataTypeDefXsd.PositiveInteger 
                     || prop.ValueType == DataTypeDefXsd.UnsignedLong)
            {
                if (UInt64.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<UInt64>(o, mode, "Value",
                        DataTypeIds.UInt64, v, defaultSettings: true);
            }
            else if (prop.ValueType == DataTypeDefXsd.UnsignedInt)
            {
                if (UInt32.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<UInt32>(o, mode, "Value",
                        DataTypeIds.UInt32, v, defaultSettings: true);
            }
            else if (prop.ValueType == DataTypeDefXsd.UnsignedShort)
            {
                if (UInt16.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<UInt16>(o, mode, "Value",
                        DataTypeIds.UInt16, v, defaultSettings: true);
            }
            else if (prop.ValueType == DataTypeDefXsd.UnsignedByte)
            {
                if (Byte.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<Byte>(o, mode, "Value",
                        DataTypeIds.Byte, v, defaultSettings: true);
            }
            else if (prop.ValueType == DataTypeDefXsd.Double)
            {
                if (double.TryParse(prop.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    this.entityBuilder.CreateAddPropertyState<double>(o, mode, "Value",
                        DataTypeIds.Double, v, defaultSettings: true);
            }
            else if (prop.ValueType == DataTypeDefXsd.Float)
            {
                if (float.TryParse(prop.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    this.entityBuilder.CreateAddPropertyState<float>(o, mode, "Value",
                        DataTypeIds.Float, v, defaultSettings: true);
            }
            else
            {
                // leave in string
                this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Value",
                    DataTypeIds.String, prop.Value, defaultSettings: true);
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
            // TODO (MIHO, 2020-08-06): use the collection element of UA?
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASSubmodelElementCollectionType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:SubmodelElementCollection");
            this.typeObjectOrdered = this.entityBuilder.CreateAddObjectType("AASSubmodelElementOrderedCollectionType",
                this.GetTypeNodeId(), preferredTypeNumId + 1,
                descriptionKey: "AAS:SubmodelElementCollection");

            // some elements
            // ReSharper disable once RedundantExplicitArrayCreation
            foreach (var o in new NodeState[] { this.typeObject /* , this.typeObjectOrdered */ })
            {
                this.entityBuilder.CreateAddPropertyState<bool>(o, CreateMode.Type, "AllowDuplicates",
                    DataTypeIds.Boolean, false, defaultSettings: true,
                    modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            }
        }

        public NodeState CreateAddInstanceObject(NodeState parent, Aas.ISubmodelElementCollection coll)
        {
            // access
            if (coll == null)
                return null;

            // containing element
            var to = GetTypeObject().NodeId;
            //if (coll.ordered && this.typeObjectOrdered != null)
            //    to = this.typeObjectOrdered.NodeId;
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance,
                "" + coll.IdShort, ReferenceTypeIds.HasComponent, to);

            // populate common attributes
            base.PopulateInstanceObject(o, coll);

            // own attributes
            //this.entityBuilder.CreateAddPropertyState<bool>(o, CreateMode.Instance, "AllowDuplicates",
            //    DataTypeIds.Boolean, coll.AllowDuplicates, defaultSettings: true);

            // values
            if (coll.Value != null)
                foreach (var smw in coll.Value)
                    this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(
                        o, CreateMode.Instance, smw);

            // result
            return o;
        }
    }

	public class AasUaEntitySmeList : AasUaEntitySubmodelElementBase
	{
		public NodeState typeObjectOrdered = null;

		public AasUaEntitySmeList(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
			: base(entityBuilder)
		{
			// create type object
			this.typeObject = this.entityBuilder.CreateAddObjectType("AASSubmodelElementListType",
				entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
				descriptionKey: "AAS:SubmodelElementList");
			this.typeObjectOrdered = this.entityBuilder.CreateAddObjectType("AASSubmodelElementOrderedListType",
				this.GetTypeNodeId(), preferredTypeNumId + 1,
				descriptionKey: "AAS:SubmodelElementList");

			// some elements
			// ReSharper disable once RedundantExplicitArrayCreation
			foreach (var o in new NodeState[] { this.typeObject /* , this.typeObjectOrdered */ })
			{
				this.entityBuilder.CreateAddPropertyState<bool>(o, CreateMode.Type, "AllowDuplicates",
					DataTypeIds.Boolean, false, defaultSettings: true,
					modellingRule: AasUaNodeHelper.ModellingRule.Optional);
			}
		}

		public NodeState CreateAddInstanceObject(NodeState parent, Aas.ISubmodelElementList list)
		{
			// access
			if (list == null)
				return null;

			// containing element
			var to = GetTypeObject().NodeId;
            if (list.OrderRelevant.HasValue && list.OrderRelevant.Value && this.typeObjectOrdered != null)
                to = this.typeObjectOrdered.NodeId;
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance,
				"" + list.IdShort, ReferenceTypeIds.HasComponent, to);

			// populate common attributes
			base.PopulateInstanceObject(o, list);

			// own attributes
			//this.entityBuilder.CreateAddPropertyState<bool>(o, CreateMode.Instance, "AllowDuplicates",
			//    DataTypeIds.Boolean, coll.AllowDuplicates, defaultSettings: true);

			// values
			if (list.Value != null)
				foreach (var smw in list.Value)
					this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(
						o, CreateMode.Instance, smw);

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASFileType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(),
                preferredTypeNumId, descriptionKey: "AAS:File");

            // some elements
            this.entityBuilder.CreateAddPropertyState<string>(
                this.typeObject, CreateMode.Type, "MimeType",
                (this.entityBuilder.theServerOptions?.SimpleDataTypes == true)
                    ? DataTypeIds.String
                    : this.entityBuilder.AasTypes.MimeType.GetTypeNodeId(),
                null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<string>(
                this.typeObject, CreateMode.Type, "Value",
                 (true == this.entityBuilder?.theServerOptions?.SimpleDataTypes)
                    ? DataTypeIds.String
                    : this.entityBuilder.AasTypes.PathType.GetTypeNodeId(),
                null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.AasTypes.FileType.CreateAddElements(this.typeObject, CreateMode.Type);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, Aas.IFile file)
        {
            // access
            if (file == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance, "" + file.IdShort,
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, file);

            // own attributes
            this.entityBuilder.CreateAddPropertyState<string>(
                o, CreateMode.Instance, "MimeType",
                (this.entityBuilder.theServerOptions?.SimpleDataTypes == true)
                    ? DataTypeIds.String
                    : this.entityBuilder.AasTypes.MimeType.GetTypeNodeId(),
                file.ContentType, defaultSettings: true);
            this.entityBuilder.CreateAddPropertyState<string>(
                o, CreateMode.Instance, "Value",
                (true == this.entityBuilder?.theServerOptions?.SimpleDataTypes)
                    ? DataTypeIds.String
                    : this.entityBuilder.AasTypes.PathType.GetTypeNodeId(),
                file.Value, defaultSettings: true);

            // wonderful working
            if (this.entityBuilder.AasTypes.FileType.CheckSuitablity(this.entityBuilder.package, file))
                this.entityBuilder.AasTypes.FileType.CreateAddElements(
                    o, CreateMode.Instance, this.entityBuilder.package, file);

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASBlobType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:Blob");

            // some elements
            this.entityBuilder.CreateAddPropertyState<string>(
                this.typeObject, CreateMode.Type, "MimeType",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Value",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, Aas.IBlob blob)
        {
            // access
            if (blob == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance, "" + blob.IdShort,
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, blob);

            // own attributes
            this.entityBuilder.CreateAddPropertyState<string>(
                o, CreateMode.Instance, "ContentType",
                DataTypeIds.String, blob.ContentType, defaultSettings: true);
            this.entityBuilder.CreateAddPropertyState<string>(o, CreateMode.Instance, "Value",
                DataTypeIds.String, System.Text.Encoding.UTF8.GetString(blob.Value), defaultSettings: true);

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASReferenceElementType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:ReferenceElement");

            // some elements
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "Value",
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, Aas.IReferenceElement refElem)
        {
            // access
            if (refElem == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance, "" + refElem.IdShort,
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, refElem);

            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, refElem.Value, "Value");

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASRelationshipElementType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:RelationshipElement");

            // some elements
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "First", modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "Second", modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, Aas.IRelationshipElement relElem)
        {
            // access
            if (relElem == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance, "" + relElem.IdShort,
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, relElem);

            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, relElem.First, "First");
            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, relElem.Second, "Second");

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("OperationVariableType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:OperationVariable");
        }

        public NodeState CreateAddInstanceObject(NodeState parent, Aas.IOperationVariable opvar)
        {
            // access
            if (opvar == null || opvar.Value == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance,
                "" + opvar.Value.IdShort,
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, opvar.Value);

            // own attributes
            this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(o, CreateMode.Instance, opvar.Value);

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASOperationType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:Operation");

            // indicate the Operation
            this.entityBuilder.CreateAddMethodState(this.typeObject, CreateMode.Type, "Operation",
                    inputArgs: null,
                    outputArgs: null,
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent);

            // some elements
            for (int i = 0; i < 2; i++)
            {
                var o2 = this.entityBuilder.CreateAddObject(this.typeObject, CreateMode.Type, (i == 0) ? "in" : "out",
                    ReferenceTypeIds.HasComponent,
                    this.entityBuilder.AasTypes.OperationVariable.GetTypeObject().NodeId);
                this.entityBuilder.AasTypes.OperationVariable.CreateAddInstanceObject(o2, null);
            }
        }

        public NodeState CreateAddInstanceObject(NodeState parent, Aas.IOperation op)
        {
            // access
            if (op == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance,
                "" + op.IdShort,
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, op);

            // own AAS attributes (in/out op vars)
            for (int i = 0; i < 2; i++)
            {
                var opvarList = op.GetVars((OperationVariableDirection)i);
                if (opvarList != null && opvarList.Count > 0)
                {
                    var o2 = this.entityBuilder.CreateAddObject(o,
                        CreateMode.Instance,
                        (i == 0) ? "OperationInputVariables" : "OperationOutputVariables",
                        ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);
                    foreach (var opvar in opvarList)
                        if (opvar != null && opvar.Value != null)
                            this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(
                                o2, CreateMode.Instance, opvar.Value);
                }
            }

            // create a method?
            if (true)
            {
                // ReSharper disable once RedundantExplicitArrayCreation
                var args = new List<Argument>[] { new List<Argument>(), new List<Argument>() };
                for (int i = 0; i < 2; i++)
                {
                    var opList = op.GetVars((OperationVariableDirection)i);
                    if (opList != null)
                        foreach (var opvar in opList)
                        {
                            // TODO (MIHO, 2020-08-06): decide to from where the name comes
                            var name = "noname";

                            // TODO (MIHO, 2020-08-06): description: get "en" version which is appropriate?
                            LocalizedText desc = new LocalizedText("");

                            // TODO (MIHO, 2020-08-06): parse UA data type out .. OK?
                            NodeId dataType = null;
                            if (opvar.Value != null)
                            {
                                // better name .. but not best (see below)
                                if (opvar.Value.IdShort != null
                                    && opvar.Value.IdShort.Trim() != "")
                                    name = "" + opvar.Value.IdShort;

                                // TODO (MIHO, 2020-08-06): description: get "en" version is appropriate?
                                desc = AasUaUtils.GetBestUaDescriptionFromAasDescription(
                                    opvar.Value.Description);

                                // currenty, only accept properties as in/out arguments. 
                                // Only these have an XSD value type!!
                                var prop = opvar.Value as Aas.IProperty;
                                if (prop != null)
                                {
                                    // TODO (MIHO, 2020-08-06): this any better?
                                    if (prop.IdShort != null && prop.IdShort.Trim() != "")
                                        name = "" + prop.IdShort;

                                    // TODO (MIHO, 2020-08-06): description: get "en" version is appropriate?
                                    if (desc.Text == null || desc.Text == "")
                                        desc = AasUaUtils.GetBestUaDescriptionFromAasDescription(
                                            opvar.Value.Description);

                                    // try convert type
                                    if (!AasUaUtils.AasValueTypeToUaDataType(
                                        prop.ValueType, out var dummy, out dataType))
                                        dataType = null;
                                }
                            }
                            if (dataType == null)
                                continue;

                            var a = new Argument(name, dataType, -1, desc.Text ?? "");
                            args[i].Add(a);
                        }
                }

                var unused = this.entityBuilder.CreateAddMethodState(o, CreateMode.Instance, "Operation",
                    inputArgs: args[0].ToArray(),
                    outputArgs: args[1].ToArray(),
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent);
            }

            // result
            return o;
        }
    }        

    public class AasUaEntityDataSpecification : AasUaBaseEntity
    {
        public AasUaEntityDataSpecification(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASDataSpecificationType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:DataSpecification");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());
        }

    }

    public class AasUaEntityDataSpecificationIEC61360 : AasUaBaseEntity
    {
        public AasUaEntityDataSpecificationIEC61360(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASDataSpecificationIEC61360Type",
                this.entityBuilder.AasTypes.DataSpecification.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:DataSpecificationIEC61360");

            // very special rule here for the Identifiable
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Instance,
                "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360",
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Instance,
                new Aas.AdministrativeInformation(version: "1", revision: "0"),
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            // add some more elements
            this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(this.typeObject, CreateMode.Type,
                "PreferredName",
                DataTypeIds.LocalizedText,
                value: null, defaultSettings: true, valueRank: 1,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type,
                "ShortName",
                DataTypeIds.String, value: null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type,
                "Unit",
                DataTypeIds.String, value: null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "UnitId",
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(this.typeObject, CreateMode.Type,
                "SourceOfDefinition",
                DataTypeIds.LocalizedText,
                value: null, defaultSettings: true, valueRank: 1,
                modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type,
                "Symbol", DataTypeIds.String,
                value: null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type,
                "DataType", DataTypeIds.String,
                value: null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(this.typeObject, CreateMode.Type,
                "Definition",
                DataTypeIds.LocalizedText, value: null, defaultSettings: true, valueRank: 1,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type,
                "ValueFormat",
                DataTypeIds.String, value: null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode,
            Aas.IDataSpecificationIec61360 ds = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            // for the sake of clarity, we're directly splitting cases
            if (mode == CreateMode.Type)
            {
                // containing element (only)
                var o = this.entityBuilder.CreateAddObject(parent, mode, "DataSpecificationIec61360",
                    this.entityBuilder.AasTypes.HasAddIn.GetTypeNodeId(), GetTypeObject().NodeId,
                    modellingRule: modellingRule);
                return o;
            }
            else
            {
                // access
                if (ds == null)
                    return null;

                // we can only provide minimal unique naming 
                var name = "DataSpecificationIEC61360";
                if (ds.PreferredName != null && this.entityBuilder.RootDataSpecifications != null)
                    name += "_" + ds.PreferredName;

                // containing element (depending on root folder)
                NodeState o = null;
                if (this.entityBuilder.RootDataSpecifications != null)
                {
                    // under common folder
                    o = this.entityBuilder.CreateAddObject(this.entityBuilder.RootDataSpecifications, mode, name,
                        ReferenceTypeIds.Organizes, GetTypeObject().NodeId);
                    // link to this object
                    parent.AddReference(this.entityBuilder.AasTypes.HasAddIn.GetTypeNodeId(), false, o.NodeId);
                }
                else
                {
                    // under parent
                    o = this.entityBuilder.CreateAddObject(parent, mode, name,
                        this.entityBuilder.AasTypes.HasAddIn.GetTypeNodeId(), GetTypeObject().NodeId);
                }

                // add some elements        
                if (ds.PreferredName != null && ds.PreferredName.Count > 0)
                    this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(o, mode, "PreferredName",
                        DataTypeIds.LocalizedText,
                        value: AasUaUtils.GetUaLocalizedTexts(ds.PreferredName),
                        defaultSettings: true, valueRank: 1);

                if (ds.ShortName != null && ds.ShortName.Count > 0)
                    this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(o, mode, "ShortName",
                        DataTypeIds.LocalizedText,
                        value: AasUaUtils.GetUaLocalizedTexts(ds.ShortName),
                        defaultSettings: true, valueRank: 1);

                if (ds.Unit != null)
                    this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Unit",
                        DataTypeIds.String, value: ds.Unit, defaultSettings: true);

                if (ds.UnitId != null)
                    this.entityBuilder.AasTypes.Reference.CreateAddElements(o, mode,
                        ds.UnitId, "UnitId");

                if (ds.SourceOfDefinition != null)
                    this.entityBuilder.CreateAddPropertyState<string>(o, mode, "SourceOfDefinition",
                        DataTypeIds.String, value: ds.SourceOfDefinition, defaultSettings: true);

                if (ds.Symbol != null)
                    this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Symbol",
                        DataTypeIds.String, value: ds.Symbol, defaultSettings: true);

                if (ds.DataType != null)
                    this.entityBuilder.CreateAddPropertyState<string>(o, mode, "DataType",
                        DataTypeIds.String, value: Stringification.ToString(ds.DataType.Value), 
                        defaultSettings: true);

                if (ds.Definition != null && ds.Definition.Count > 0)
                    this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(o, mode, "Definition",
                        DataTypeIds.LocalizedText, value: AasUaUtils.GetUaLocalizedTexts(ds.Definition),
                        defaultSettings: true, valueRank: 1);

                if (ds.ValueFormat != null)
                    this.entityBuilder.CreateAddPropertyState<string>(o, mode, "ValueFormat",
                        DataTypeIds.String, value: ds.ValueFormat, defaultSettings: true);

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
            // TODO (MIHO, 2020-08-06): check, if to make super classes for UriDictionaryEntryType?
            this.typeObjectIrdi = this.entityBuilder.CreateAddObjectType("AASIrdiConceptDescriptionType",
                this.entityBuilder.AasTypes.IrdiDictionaryEntryType.GetTypeNodeId(), 0,
                descriptionKey: "AAS:ConceptDescription");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObjectIrdi, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            this.typeObjectUri = this.entityBuilder.CreateAddObjectType("AASUriConceptDescriptionType",
                this.entityBuilder.AasTypes.UriDictionaryEntryType.GetTypeNodeId(), 0,
                descriptionKey: "AAS:ConceptDescription");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObjectUri, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            this.typeObjectCustom = this.entityBuilder.CreateAddObjectType("AASCustomConceptDescriptionType",
                this.entityBuilder.AasTypes.DictionaryEntryType.GetTypeNodeId(), 0,
                descriptionKey: "AAS:ConceptDescription");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObjectCustom, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            // for each of them, add some elements
            // ReSharper disable once RedundantExplicitArrayCreation
            foreach (var o in new NodeState[] { this.typeObjectIrdi, this.typeObjectUri, this.typeObjectCustom })
            {
                // Referable
                this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Type);
                // Identifiable
                this.entityBuilder.AasTypes.Identification.CreateAddElements(o, CreateMode.Type,
                    modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
                this.entityBuilder.AasTypes.Administration.CreateAddElements(o, CreateMode.Type,
                    modellingRule: AasUaNodeHelper.ModellingRule.Optional);
                // IsCaseOf
                this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Type, null, "IsCaseOf",
                    modellingRule: AasUaNodeHelper.ModellingRule.Optional);
                // HasDataSpecification
                this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Type, null, "DataSpecification",
                    modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);

                // data specification is a child
                this.entityBuilder.AasTypes.DataSpecificationIEC61360.CreateAddElements(o, CreateMode.Type,
                    modellingRule: AasUaNodeHelper.ModellingRule.MandatoryPlaceholder);
            }
        }

        public NodeState GetTypeObjectFor(string id)
        {
            var to = this.typeObject; // shall be NULL
            if (id?.HasContent() != true)
                return to;

            var idt = ExtendKey.GuessIdType(id);
            if (idt == ExtendKey.IdType.IRI)
                to = this.typeObjectUri;
            else if (idt == ExtendKey.IdType.IRDI)
                to = this.typeObjectIrdi;
            else
                to = this.typeObjectCustom;
            return to;
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, Aas.IConceptDescription cd = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
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
#pragma warning disable 162
                // ReSharper disable HeuristicUnreachableCode
                {
                    // Conventional approach: build up a speaking name
                    // but: shall be target of "HasDictionaryEntry", therefore the __PURE__ identifications 
                    // need to be the name!
                    if (cd.GetIEC61360() != null)
                    {
                        var ds = cd.GetIEC61360();
                        if (ds.ShortName != null)
                            name = ds.ShortName.GetDefaultString();
                        if (cd.Id != null)
                            name += "_" + cd.Id;
                    }
                    name = AasUaUtils.ToOpcUaName(name);
                }
                // ReSharper enable HeuristicUnreachableCode
#pragma warning restore 162
                else
                {
                    // only identification (the type object will distinct between the id type)
                    if (cd.Id != null)
                        name = cd.Id;
                }

                // containing element
                var o = this.entityBuilder.CreateAddObject(parent, mode, name,
                    ReferenceTypeIds.HasComponent, this.GetTypeObjectFor(cd.Id)?.NodeId,
                    modellingRule: modellingRule);

                // Referable
                this.entityBuilder.AasTypes.Referable.CreateAddElements(
                    o, CreateMode.Instance, cd);
                // Identifiable
                this.entityBuilder.AasTypes.Identification.CreateAddElements(
                    o, CreateMode.Instance, cd.Id);
                this.entityBuilder.AasTypes.Administration.CreateAddElements(
                    o, CreateMode.Instance, cd.Administration);
                // IsCaseOf
                if (cd.IsCaseOf != null)
                    foreach (var ico in cd.IsCaseOf)
                        this.entityBuilder.AasTypes.Reference.CreateAddElements(
                            o, CreateMode.Instance, ico, "IsCaseOf");

                // HasDataSpecification solely under the viewpoint of IEC61360
                var eds = cd.EmbeddedDataSpecifications?.FindFirstIEC61360Spec();
                if (eds != null)
                    this.entityBuilder.AasTypes.Reference.CreateAddElements(
                        o, CreateMode.Instance, eds.DataSpecification, "DataSpecification");

                // data specification is a child
                var ds61360 = cd.EmbeddedDataSpecifications?.GetIEC61360Content();
                if (ds61360 != null)
                {
                    var unused = this.entityBuilder.AasTypes.DataSpecificationIEC61360.CreateAddElements(
                        o, CreateMode.Instance,
                        ds61360);
                }

                // remember CD as NodeRecord
                this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, cd.Id));

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
            this.typeObject = this.entityBuilder.CreateAddReferenceType("AASReference", "AASReferencedBy", preferredTypeNumId, useZeroNS: false, extraSubtype: new NodeId(31, 0) /* request Florian */);
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
            this.typeObject = this.entityBuilder.CreateAddObjectType("IAASIdentifiableType",
                this.entityBuilder.AasTypes.IAASReferableType.GetTypeNodeId() /* ObjectTypeIds.BaseObjectType */,
                preferredTypeNumId, descriptionKey: "AAS:Identifiable");

            // add some elements
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);
        }
    }

    public class AasUaInterfaceAASReferableType : AasUaBaseEntity
    {
        public AasUaInterfaceAASReferableType(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("IAASReferableType",
                this.entityBuilder.AasTypes.BaseInterfaceType.GetTypeNodeId() /* ObjectTypeIds.BaseObjectType */,
                preferredTypeNumId, descriptionKey: "AAS:Referable");

            // some elements
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
        }
    }
}
