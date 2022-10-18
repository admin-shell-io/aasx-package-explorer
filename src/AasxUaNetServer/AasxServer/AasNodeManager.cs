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

using AdminShellNS;
using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AasOpcUaServer
{
    /// <summary>
    /// A node manager the diagnostic information exposed by the server.
    /// </summary>
    public class AasModeManager : CustomNodeManager2
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
            uint id = Utils.IncrementIdentifier(ref m_lastUsedId);

            if (mode == AasUaBaseEntity.CreateMode.Type)
            {
                return new NodeId(id, m_typeNamespaceIndex);
            }
            else
            {
                return new NodeId(id, m_namespaceIndex);
            }
        }
        #endregion

        public NodeId NewType(ISystemContext context, NodeState node, uint preferredNumId)
        {
            uint id = preferredNumId;

            if (id == 0)
            {
                id = Utils.IncrementIdentifier(ref m_lastUsedTypeId);
            }
            
            return new NodeId(id, m_typeNamespaceIndex);
        }

        public void SaveNodestateCollectionAsNodeSet2(ISystemContext context, NodeStateCollection nsc, Stream stream, bool filterSingleNodeIds, NodeState rootItem = null)
        {
            Opc.Ua.Export.UANodeSet nodeSet = new Opc.Ua.Export.UANodeSet();
            nodeSet.LastModified = DateTime.UtcNow;
            nodeSet.LastModifiedSpecified = true;

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

                var nodup = new List<Opc.Ua.Export.UANode>();

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

                nodeSet.Items = nodup.ToArray();
            }

            //
            // write
            //

            Utils.Trace(Utils.TraceMasks.Operation, "Writing stream ..");
            nodeSet.Write(stream);
        }

        #region INodeManager Members

        private FolderState CreateAASFolder(IList<IReference> objectsFolder)
        {
            string name = "AssetAdminShell";
            FolderState folder = new FolderState(null)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(name, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };

            folder.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
            objectsFolder.Add(new NodeStateReference(ReferenceTypes.Organizes, false, folder.NodeId));
            AddPredefinedNode(SystemContext, folder);

            return folder;
        }
        
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
                var builder = new AasEntityBuilder(this, thePackageEnv, null, theServerOptions);

                // get a reference to the objects folder
                IList<IReference> objectsFolder = null;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out objectsFolder))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = objectsFolder = new List<IReference>();
                }

                // create AAS folder under objects folder
                FolderState root = CreateAASFolder(objectsFolder);
   
                // ceate dictionaries folder
                var topOfDict = new BaseObjectState(root) 
                {
                    NodeId = new NodeId("Dictionaries", NamespaceIndex)
                };

                if (theServerOptions.CeateDictionariesFolder)
                {
                    topOfDict = builder.CreateAddObject(
                        root,
                        AasUaBaseEntity.CreateMode.Instance,
                        "Dictionaries",
                        referenceTypeFromParentId: null,
                        typeDefinitionId: builder.AasTypes.DictionaryFolderType.GetTypeNodeId());
                }

                // now, create a folder for Concept Descriptions
                builder.RootConceptDescriptions = builder.CreateAddObject(
                    topOfDict,
                    AasUaBaseEntity.CreateMode.Instance,
                    "ConceptDescriptions",
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent,
                    typeDefinitionId: builder.AasTypes.DictionaryFolderType.GetTypeNodeId());

                // create missing dictionary entries
                builder.RootMissingDictionaryEntries = builder.CreateAddObject(
                    topOfDict,
                    AasUaBaseEntity.CreateMode.Instance, 
                    "DictionaryEntries",
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent,
                    typeDefinitionId: builder.AasTypes.DictionaryFolderType.GetTypeNodeId());
                
                builder.CreateAddInstanceObjects(thePackageEnv.AasEnv);

                AddReverseReferences(externalReferences);

                // write nodeset2.xml file, if required
                if (theServerOptions != null && theServerOptions.SpecialJob == AasxUaServerOptions.JobType.ExportNodesetXml)
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

                        SaveNodestateCollectionAsNodeSet2(this.SystemContext, nodesToExport, stream.BaseStream,
                            filterSingleNodeIds: theServerOptions != null
                                && theServerOptions.FilterForSingleNodeIds,
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
                }
            
                Debug.WriteLine("Done creating custom address space!");
                Utils.Trace(Utils.TraceMasks.Operation, "Done creating custom address space!");
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

        #endregion

        #region Private Fields
        private ushort m_namespaceIndex;
        private ushort m_typeNamespaceIndex;
        private long m_lastUsedId;
        private long m_lastUsedTypeId;
        #endregion
    }
}
