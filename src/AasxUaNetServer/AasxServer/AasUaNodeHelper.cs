/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Opc.Ua;

namespace AasOpcUaServer
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AasUaNodeHelper
    {
        /// <summary>
        /// 4.	The cardinality of an association or aggregation is specified via OPC Modelling rules. 
        /// The OPC modelling rule “Optional” is used if the cardinality is Zero or 1. 
        /// The OPC modelling rule “Mandatory” is used if the cardinality is One. 
        /// The OPC Modelling rule “OptionalPlaceholder” is used if the cardinality is zero, 
        /// one or more than one element. 
        /// The OPC Modelling rule “MandatoryPlaceholder” ” is used if the cardinality is one or more than one element.
        /// </summary>
        public enum ModellingRule { None, Optional, OptionalPlaceholder, Mandatory, MandatoryPlaceholder }

        /// <summary>
        /// Try find out the Description for a certain AAS literal/ refSemantics
        /// </summary>
        public static LocalizedText SetLocalizedTextWithDescription(LocalizedText l, string key)
        {
            var dk = AasEntityDescriptions.LookupDescription(key);

            if (key != null)
            {
                l = new LocalizedText("en", dk);
            }

            return l;
        }

        /// <summary>
        /// Appply modelling rule to an arbitrary node
        /// </summary>
        public static NodeState CheckSetModellingRule(ModellingRule modellingRule, NodeState o)
        {
            if (o == null || modellingRule == ModellingRule.None)
            {
                return o;
            }

            if (modellingRule == ModellingRule.Optional)
            {
                o.AddReference(ReferenceTypeIds.HasModellingRule, false, ObjectIds.ModellingRule_Optional);
            }

            if (modellingRule == ModellingRule.OptionalPlaceholder)
            {
                o.AddReference(ReferenceTypeIds.HasModellingRule, false, ObjectIds.ModellingRule_OptionalPlaceholder);
            }

            if (modellingRule == ModellingRule.Mandatory)
            {
                o.AddReference(ReferenceTypeIds.HasModellingRule, false, ObjectIds.ModellingRule_Mandatory);
            }

            if (modellingRule == ModellingRule.MandatoryPlaceholder)
            {
                o.AddReference(ReferenceTypeIds.HasModellingRule, false, ObjectIds.ModellingRule_MandatoryPlaceholder);
            }

            return o;
        }

        /// <summary>
        /// Helper to create an ObjectType-Node. Note: __NO__ NodeId is created by the default! 
        /// Must be done by outer functionality!!
        /// </summary>
        /// <param name="browseDisplayName">Name displayed in the node tree</param>
        /// <param name="superTypeId">Base class or similar</param>
        /// <param name="presetNodeId">Preset the NodeId</param>
        /// <param name="descriptionKey">Lookup a Description on AAS literal/ refSemantics</param>
        /// <param name="modellingRule">Modeling Rule, if not None</param>
        /// <returns>THe node</returns>
        public static BaseObjectTypeState CreateObjectType(
            string browseDisplayName,
            NodeId superTypeId,
            NodeId presetNodeId = null,
            string descriptionKey = null,
            ModellingRule modellingRule = ModellingRule.None)
        {
            BaseObjectTypeState baseObjectType = new()
            {
                BrowseName = "" + browseDisplayName,
                DisplayName = "" + browseDisplayName,
                Description = SetLocalizedTextWithDescription(new LocalizedText("en", browseDisplayName), descriptionKey),
                SuperTypeId = superTypeId
            };

            if (presetNodeId != null)
            {
                baseObjectType.NodeId = presetNodeId;
            }

            CheckSetModellingRule(modellingRule, baseObjectType);

            return baseObjectType;
        }

        /// <summary>
        /// Helper to create an ReferenceType-Node. Note: __NO__ NodeId is created by the default! 
        /// Must be done by outer functionality!!
        /// </summary>
        /// <param name="browseDisplayName">Name displayed in the node tree</param>
        /// <param name="inverseName"></param>
        /// <param name="superTypeId">Preset the NodeId</param>
        /// <param name="presetNodeId">Preset the NodeId</param>
        /// <returns></returns>
        public static ReferenceTypeState CreateReferenceType(
            string browseDisplayName,
            string inverseName,
            NodeId superTypeId,
            NodeId presetNodeId = null)
        {
            ReferenceTypeState referenceType = new()
            {
                BrowseName = browseDisplayName,
                DisplayName = browseDisplayName,
                InverseName = inverseName,
                Symmetric = false,
                IsAbstract = false,
                SuperTypeId = superTypeId
            };

            if (presetNodeId != null)
            {
                referenceType.NodeId = presetNodeId;
            }

            return referenceType;
        }

        /// <summary>
        /// Helper to create an Object-Node. Note: __NO__ NodeId is created by the default! 
        /// Must be done by outer functionality!!
        /// </summary>
        /// <param name="parent">Parent node</param>
        /// <param name="browseDisplayName">Name displayed in the node tree</param>
        /// <param name="typeDefinitionId">Type of the Object</param>
        /// <param name="modellingRule">Modeling Rule, if not None</param>
        /// <param name="extraName"></param>
        /// <returns>The node</returns>
        public static BaseObjectState CreateObject(
            NodeState parent,
            string browseDisplayName,
            NodeId typeDefinitionId = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None,
            string extraName = null)
        {
            BaseObjectState baseObject = new(parent)
            {
                BrowseName = string.Empty + browseDisplayName,
                DisplayName = string.Empty + browseDisplayName,
                Description = new LocalizedText("en", string.Empty + browseDisplayName),
                TypeDefinitionId = typeDefinitionId
            };

            if (extraName != null)
            {
                baseObject.DisplayName = extraName;
            }

            CheckSetModellingRule(modellingRule, baseObject);

            return baseObject;
        }

    }
}
