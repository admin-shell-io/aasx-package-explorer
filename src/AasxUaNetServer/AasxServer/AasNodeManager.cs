/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using AdminShellNS;
using Opc.Ua;
using Opc.Ua.Sample;

namespace AasOpcUaServer
{
    /// <summary>
    /// A node manager the diagnostic information exposed by the server.
    /// </summary>
    public class AasModeManager : SampleNodeManager
    {
        private AdminShellPackageEnv thePackageEnv = null;
        private AasxUaServerOptions theServerOptions = null;

        #region Constructors
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public AasModeManager(
            Opc.Ua.Server.IServerInternal server,
            ApplicationConfiguration configuration,
            AdminShellPackageEnv env,
            AasxUaServerOptions serverOptions = null)
        :
            base(server)
        {
            thePackageEnv = env;
            theServerOptions = serverOptions;

            List<string> namespaceUris = new List<string>();
            namespaceUris.Add("http://opcfoundation.org/UA/i4aas/");
            namespaceUris.Add("http://admin-shell.io/samples/i4aas/instance/");
            // ReSharper disable once VirtualMemberCallInConstructor
            NamespaceUris = namespaceUris;

            m_typeNamespaceIndex = Server.NamespaceUris.GetIndexOrAppend(namespaceUris[0]);
            m_namespaceIndex = Server.NamespaceUris.GetIndexOrAppend(namespaceUris[1]);

            m_lastUsedId = 0;
        }
        #endregion

        #region INodeIdFactory Members
        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="mode">Type or instance</param>
        /// <param name="node">The node.</param>
        /// <returns>The new NodeId.</returns>
        public NodeId New(ISystemContext context, AasUaBaseEntity.CreateMode mode, NodeState node)
        {
            if (mode == AasUaBaseEntity.CreateMode.Type)
            {
                uint id = Utils.IncrementIdentifier(ref m_lastUsedId);
                return new NodeId(id, m_typeNamespaceIndex);
            }
            else
            {
                uint id = Utils.IncrementIdentifier(ref m_lastUsedId);
                return new NodeId(id, m_namespaceIndex);
            }
        }
        #endregion

        public NodeId NewType(ISystemContext context, AasUaBaseEntity.CreateMode mode,
            NodeState node, uint preferredNumId = 0)
        {
            uint id = preferredNumId;
            if (id == 0)
                id = Utils.IncrementIdentifier(ref m_lastUsedTypeId);
            // this is thought to be a BUG in the OPCF code
            //// return new NodeId(preferredNumId, m_typeNamespaceIndex);
            if (mode == AasUaBaseEntity.CreateMode.Type)
                return new NodeId(id, m_typeNamespaceIndex);
            else
                return new NodeId(id, m_namespaceIndex);
        }

        public void SaveNodestateCollectionAsNodeSet2(ISystemContext context, NodeStateCollection nsc, Stream stream,
            bool filterSingleNodeIds, bool addRootItem = false, NodeState rootItem = null)
        {
            Opc.Ua.Export.UANodeSet nodeSet = new Opc.Ua.Export.UANodeSet();
            nodeSet.LastModified = DateTime.UtcNow;
            nodeSet.LastModifiedSpecified = true;

            // 
            // Because the pain of so many wasted hours is so great:
            // This function realizes a "write Nodeset2.xml" functionality, hwich
            // seems to not work out-of-the-box by the existing OPC UA .dll
            // (see "SaveNodestateCollectionAsNodeSet2tryout", .dll is an old version
            //  because auf .net Framework 4.7.2)
            //
            // This function "fakes" this export.
            // Remark: the call of nodeSet.Export() shift the namespace-index.
            // For multiple hours I tried to initialize the namespace-index better,
            // or to figure out, which is the correct exported node for "AASROOT".
            // It was not possible. So, a very bad hack is used.
            // 

            //// nodeSet.NamespaceUris = new[] { "http://opcfoundation.org/UA/" };
            //// var l = new List<string>();
            //// l.Add("http://opcfoundation.org/UA/");
            //// l.AddRange(NamespaceUris);
            //// nodeSet.NamespaceUris = l.ToArray();

            Utils.Trace(Utils.TraceMasks.Operation, "Exporting {0} nodes ..", nsc.Count);
            int i = 0;
            foreach (var n in nsc)
            {
                nodeSet.Export(context, n);
                if ((i++) % 500 == 0)
                    Utils.Trace(Utils.TraceMasks.Operation, "  .. exported already {0} nodes ..", nodeSet.Items.Length);
            }

            if (filterSingleNodeIds)
            {
                Utils.Trace(Utils.TraceMasks.Operation, "Filtering single node ids..");

                // MIHO: There might be DOUBLE nodeIds in the the set!!!!!!!!!! WTF!!!!!!!!!!!!!
                // Brutally eliminate them
                var nodup = new List<Opc.Ua.Export.UANode>();

#if __old_implementation

                foreach (var it in nodeSet.Items)
                {
                    var found = false;
                    foreach (var it2 in nodup)
                        if (it.NodeId == it2.NodeId)
                            found = true;
                    if (found)
                        continue;
                    nodup.Add(it);
                }
#else
                var visitedNodeIds = new Dictionary<string, int>();

                foreach (var it in nodeSet.Items)
                {
                    if (visitedNodeIds.ContainsKey(it.NodeId))
                        continue;
                    visitedNodeIds.Add(it.NodeId, 1);

                    // try to remove double references
                    if (it.References != null)
                    {
                        var newrefs = new List<Opc.Ua.Export.Reference>();
                        foreach (var oldref in it.References)
                        {
                            // trivial
                            if (oldref == null)
                                continue;

                            // brute force check if already there
                            var found = false;
                            foreach (var nr in newrefs)
                                if (oldref.ReferenceType == nr.ReferenceType
                                    && oldref.IsForward == nr.IsForward
                                    && oldref.Value == nr.Value)
                                {
                                    found = true;
                                    break;
                                }

                            // if not, add
                            if (!found)
                                newrefs.Add(oldref);
                        }
                        // only change when necessary (reduce the impact)
                        if (it.References.Length != newrefs.Count)
                            it.References = newrefs.ToArray();
                    }

                    nodup.Add(it);
                }
#endif

                if (addRootItem)
                {
                    Utils.Trace(Utils.TraceMasks.Operation, "Adding root item..");

                    var rootItemSt = "ns=2;i=95"; // weird default
                    if (rootItem != null)
                    {
                        // Bad hack, apoligizes
                        var ni = new NodeId(
                            value: rootItem.NodeId.Identifier,
                            namespaceIndex: (ushort)((rootItem.NodeId.NamespaceIndex) - 1));
                        rootItemSt = ni.Format();
                    }

                    var ri = new Opc.Ua.Export.UAObject()
                    {
                        BrowseName = "Objects",
                        DisplayName = new[] {
                            new Opc.Ua.Export.LocalizedText() { Locale = "en", Value = "Objects" }
                        },
                        NodeId = "ns=0;i=85",
                        References = new[]
                        {
                            new Opc.Ua.Export.Reference() {
                                ReferenceType = /* "" + Opc.Ua.ReferenceTypeIds.HierarchicalReferences.Format() */
                                    "i=33", Value = rootItemSt /* "ns=2;i=95" */ /* rootItemSt */ },
                            new Opc.Ua.Export.Reference() {
                                ReferenceType = /* "" + Opc.Ua.ReferenceTypeIds.HasTypeDefinition.Format() */
                                    "i=40",
                                Value = "i=61" /* FoldersType!! */ /* "i=68" */ }
                        },
                    };

                    nodup.Add(ri);
                }

                nodeSet.Items = nodup.ToArray();
            }

            Utils.Trace(Utils.TraceMasks.Operation, "Writing stream ..");
            nodeSet.Write(stream);
        }

        public void SaveNodestateCollectionAsNodeSet2tryout(
            ISystemContext context, NodeStateCollection nsc, Stream stream,
            bool filterSingleNodeIds)
        {
            while (nsc.Count > 2)
                nsc.RemoveAt(1);

            // MICHA TODO TEST
            using (var sw = new StreamWriter("export-nodeset.xml"))
            {
                nsc.SaveAsNodeSet(context, sw.BaseStream);
            }

            using (var sw2 = new StreamWriter("export-nodeset2.xml"))
            {
                nsc.SaveAsNodeSet2(context, sw2.BaseStream);
            }
        }

        #region INodeManager Members
        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.  
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                base.CreateAddressSpace(externalReferences);

                // Note: might be helpful for debugging
                //// var env = new AdminShell.PackageEnv("Festo-USB-stick-sample-admin-shell.aasx");

                if (true)
                {
                    var builder = new AasEntityBuilder(this, thePackageEnv, null, this.theServerOptions);

                    // Overall root node is "Objects"
                    // Note: it would be better to already have the "Objects" NodeState, but not
                    // clear how to find ..
                    var fakeObjects = new BaseObjectState(null) { NodeId = new NodeId(85, 0) };

                    // Root of whole structure is special, needs to link to external reference

                    builder.RootAAS = builder.CreateAddFolder(AasUaBaseEntity.CreateMode.Instance,
                        fakeObjects, "AASROOT");
                    builder.RootAAS.AddReference(ReferenceTypeIds.Organizes, isInverse: true, fakeObjects.NodeId);

                    // Note: this is TOTALLY WEIRD, but it establishes an inverse reference .. somehow
                    this.AddExternalReferencePublic(new NodeId(85, 0), ReferenceTypeIds.Organizes, false,
                        builder.RootAAS.NodeId, externalReferences);
                    this.AddExternalReferencePublic(builder.RootAAS.NodeId, ReferenceTypeIds.Organizes, true,
                        new NodeId(85, 0), externalReferences);

                    // Folders for DataSpecs
                    // DO NOT USE THIS FEATURE -> Data Spec are "under" the CDs
                    //// builder.RootDataSpecifications = builder.CreateAddFolder(
                    //// builder.RootAAS, "DataSpecifications");
                    //// builder.RootDataSpecifications = builder.CreateAddObject(
                    //// builder.RootAAS, "DataSpecifications");

#if _not_used
#pragma warning disable 162
                    {
                        // Folders for Concept Descriptions
                        // ReSharper disable once HeuristicUnreachableCode
                        builder.RootConceptDescriptions = builder.CreateAddFolder(
                            AasUaBaseEntity.CreateMode.Instance,
                            builder.RootAAS, "ConceptDescriptions");

                        // create missing dictionary entries
                        builder.RootMissingDictionaryEntries = builder.CreateAddFolder(
                            AasUaBaseEntity.CreateMode.Instance,
                            builder.RootAAS, "DictionaryEntries");
                    }
#pragma warning restore 162
#else
                    {
                        // create folder(s) under "Objects"
                        var topOfDict = builder.CreateAddObject(fakeObjects,
                            AasOpcUaServer.AasUaBaseEntity.CreateMode.Instance, "Dictionaries",
                            referenceTypeFromParentId: null,
                            typeDefinitionId: builder.AasTypes.DictionaryFolderType.GetTypeNodeId());
                        topOfDict.AddReference(ReferenceTypeIds.Organizes, isInverse: true, fakeObjects.NodeId);

                        // Note: this is TOTALLY WEIRD, but it establishes an inverse reference .. somehow
                        // 2253 = Objects?
                        this.AddExternalReferencePublic(new NodeId(2253, 0),
                            ReferenceTypeIds.HasComponent, false, topOfDict.NodeId, externalReferences);
                        this.AddExternalReferencePublic(topOfDict.NodeId,
                            ReferenceTypeIds.HasComponent, true, new NodeId(2253, 0), externalReferences);

                        // now, create a dictionary under ..
                        // Folders for Concept Descriptions
                        builder.RootConceptDescriptions = builder.CreateAddObject(topOfDict,
                            AasOpcUaServer.AasUaBaseEntity.CreateMode.Instance, "ConceptDescriptions",
                            referenceTypeFromParentId: ReferenceTypeIds.HasComponent,
                            typeDefinitionId: builder.AasTypes.DictionaryFolderType.GetTypeNodeId());

                        // create missing dictionary entries
                        builder.RootMissingDictionaryEntries = builder.CreateAddObject(topOfDict,
                            AasOpcUaServer.AasUaBaseEntity.CreateMode.Instance, "DictionaryEntries",
                            referenceTypeFromParentId: ReferenceTypeIds.HasComponent,
                            typeDefinitionId: builder.AasTypes.DictionaryFolderType.GetTypeNodeId());
                    }

                    // start process
                    builder.CreateAddInstanceObjects(thePackageEnv.AasEnv);
#endif

                    // Try: ensure the reverse refernces exist.
                    //// AddReverseReferences(externalReferences);

                    if (theServerOptions != null
                        && theServerOptions.SpecialJob == AasxUaServerOptions.JobType.ExportNodesetXml)
                    {
                        try
                        {
                            // empty list
                            var nodesToExport = new NodeStateCollection();

                            // apply filter criteria
                            foreach (var y in this.PredefinedNodes)
                            {
                                var node = y.Value;

                                if (theServerOptions.ExportFilterNamespaceIndex != null
                                    && !theServerOptions.ExportFilterNamespaceIndex.Contains(
                                        node.NodeId.NamespaceIndex))
                                    continue;

                                nodesToExport.Add(node);
                            }

                            // export
                            Utils.Trace(Utils.TraceMasks.Operation,
                                "Writing export file: " + theServerOptions.ExportFilename);
                            var stream = new StreamWriter(theServerOptions.ExportFilename);

                            //// nodesToExport.SaveAsNodeSet2(this.SystemContext, stream.BaseStream, null, 
                            //// theServerOptions != null && theServerOptions.FilterForSingleNodeIds);

                            //// nodesToExport.SaveAsNodeSet2(this.SystemContext, stream.BaseStream);
                            SaveNodestateCollectionAsNodeSet2(this.SystemContext, nodesToExport, stream.BaseStream,
                                filterSingleNodeIds: theServerOptions != null
                                    && theServerOptions.FilterForSingleNodeIds,
                                addRootItem: theServerOptions != null && theServerOptions.AddRootItem,
                                builder.RootAAS);

                            try
                            {
                                stream.Close();
                            }
                            catch (Exception ex)
                            {
                                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                            }

                            Utils.Trace(Utils.TraceMasks.Operation,
                                "Export file *** completely written! ***");

                            // stop afterwards
                            if (theServerOptions.FinalizeAction != null)
                            {
                                Utils.Trace(Utils.TraceMasks.Operation,
                                    "Requesting to shut down application..");
                                theServerOptions.FinalizeAction();
                            }

                        }
                        catch (Exception ex)
                        {
                            Utils.Trace(ex, "When exporting to {0}", "" + theServerOptions.ExportFilename);
                        }

                        // shutdown ..

                    }
                }

                Debug.WriteLine("Done creating custom address space!");
                Utils.Trace(Utils.TraceMasks.Operation,
                    "Done creating custom address space!");
            }
        }

        public NodeStateCollection GenerateInjectNodeStates()
        {
            // new list
            var res = new NodeStateCollection();

            // Missing Object Types
            res.Add(AasUaNodeHelper.CreateObjectType("BaseInterfaceType",
                ObjectTypeIds.BaseObjectType, new NodeId(17602, 0)));
            res.Add(AasUaNodeHelper.CreateObjectType("DictionaryFolderType",
                ObjectTypeIds.FolderType, new NodeId(17591, 0)));
            res.Add(AasUaNodeHelper.CreateObjectType("DictionaryEntryType",
                ObjectTypeIds.BaseObjectType, new NodeId(17589, 0)));
            res.Add(AasUaNodeHelper.CreateObjectType("UriDictionaryEntryType",
                new NodeId(17589, 0), new NodeId(17600, 0)));
            res.Add(AasUaNodeHelper.CreateObjectType("IrdiDictionaryEntryType",
                new NodeId(17589, 0), new NodeId(17598, 0)));

            // Missing Reference Types
            res.Add(AasUaNodeHelper.CreateReferenceType("HasDictionaryEntry", "DictionaryEntryOf",
                ReferenceTypeIds.NonHierarchicalReferences, new NodeId(17597, 0)));
            res.Add(AasUaNodeHelper.CreateReferenceType("HasInterface", "InterfaceOf",
                ReferenceTypeIds.NonHierarchicalReferences, new NodeId(17603, 0)));
            res.Add(AasUaNodeHelper.CreateReferenceType("HasAddIn", "AddInOf",
                ReferenceTypeIds.HasComponent, new NodeId(17604, 0)));

            // deliver list
            return res;
        }

        public void AddReference(NodeId node, IReference reference)
        {
            var dict = new Dictionary<NodeId, IList<IReference>>();
            // ReSharper disable once RedundantExplicitArrayCreation
            dict.Add(node, new List<IReference>(new IReference[] { reference }));
            this.AddReferences(dict);
        }

        /// <summary>
        /// Loads a node set from a file or resource and addes them to the set of predefined nodes.
        /// </summary>
        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();
            return predefinedNodes;
        }

        /// <summary>
        /// Replaces the generic node with a node specific to the model.
        /// </summary>
        protected override NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
        {
            return predefinedNode;
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        protected override void OnCreateMonitoredItem(
            ISystemContext systemContext,
            MonitoredItemCreateRequest itemToCreate,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem)
        {
            // TBD
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        protected override void OnModifyMonitoredItem(
            ISystemContext systemContext,
            MonitoredItemModifyRequest itemToModify,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem,
            double previousSamplingInterval)
        {
            // TBD
        }

        /// <summary>
        /// Does any processing after a monitored item is deleted.
        /// </summary>
        protected override void OnDeleteMonitoredItem(
            ISystemContext systemContext,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem)
        {
            // TBD
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        protected override void OnSetMonitoringMode(
            ISystemContext systemContext,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem,
            MonitoringMode previousMode,
            MonitoringMode currentMode)
        {
            // TBD
        }
        #endregion

        #region Private Fields
        private ushort m_namespaceIndex;
        private ushort m_typeNamespaceIndex;
        private long m_lastUsedId;
        private long m_lastUsedTypeId;
        #endregion
    }
}
