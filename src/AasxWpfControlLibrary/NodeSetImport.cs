/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extenstions;

namespace AasxPackageExplorer
{

    public class field
    {
        public string name;
        public string value;
        public string description;
    }

    public class UaNode
    {
        public string UAObjectTypeName;
        public string NodeId;
        public string ParentNodeId;
        public string BrowseName;
        public string NameSpace;
        public string SymbolicName;
        public string DataType;
        public string Description;
        public string Value;
        public string DisplayName;

        public object parent;
        public List<UaNode> children;
        public List<string> references;

        public string DefinitionName;
        public string DefinitionNameSpace;
        public List<field> fields;

        public UaNode()
        {
            children = new List<UaNode>();
            references = new List<string>();
            fields = new List<field>();
        }
    }

    public static class OpcUaTools
    {
        static List<UaNode> roots;
        static List<UaNode> nodes;
        static Dictionary<string, UaNode> parentNodes;
        static Dictionary<string, Int16> semanticIDPool;

        public static void ImportNodeSetToSubModel(
            string inputFn, AasCore.Aas3_0_RC02.Environment env, Submodel sm,
            Reference smref)
        {
            XmlTextReader reader = new XmlTextReader(inputFn);
            StreamWriter sw = System.IO.File.CreateText(inputFn + ".log.txt");

            string elementName = "";
            bool tagDefinition = false;
            string referenceType = "";

            roots = new List<UaNode>();
            nodes = new List<UaNode>();
            parentNodes = new Dictionary<string, UaNode>();
            semanticIDPool = new Dictionary<string, Int16>();
            UaNode currentNode = null;

            // global model data
            string ModelUri = "";
            string ModelUriVersion = "";
            string ModelUriPublicationDate = "";
            string RequiredModelUri = "";
            string RequiredModelUriVersion = "";
            string RequiredModelUriPublicationDate = "";


            // scan nodeset and store node data in nodes
            // store also roots, i.e. no parent in node
            // store also new ParentNodeIds in parentNodes with value null
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        elementName = reader.Name;
                        switch (elementName)
                        {
                            case "Model":
                                ModelUri = reader.GetAttribute("ModelUri");
                                ModelUriVersion = reader.GetAttribute("Version");
                                ModelUriPublicationDate = reader.GetAttribute("PublicationDate");
                                break;
                            case "RequiredModel":
                                RequiredModelUri = reader.GetAttribute("ModelUri");
                                RequiredModelUriVersion = reader.GetAttribute("Version");
                                RequiredModelUriPublicationDate = reader.GetAttribute("PublicationDate");
                                break;
                            case "UADataType":
                            case "UAVariable":
                            case "UAObject":
                            case "UAMethod":
                            case "UAReferenceType":
                            case "UAObjectType":
                            case "UAVariableType":
                                string parentNodeId = reader.GetAttribute("ParentNodeId");
                                currentNode = new UaNode();
                                currentNode.UAObjectTypeName = elementName;
                                currentNode.NodeId = reader.GetAttribute("NodeId");
                                currentNode.ParentNodeId = parentNodeId;
                                currentNode.BrowseName = reader.GetAttribute("BrowseName");
                                var split = currentNode.BrowseName.Split(':');
                                if (split.Length > 1)
                                {
                                    currentNode.NameSpace = split[0];
                                    if (split.Length == 2)
                                        currentNode.BrowseName = split[1];
                                }
                                currentNode.SymbolicName = reader.GetAttribute("SymbolicName");
                                currentNode.DataType = reader.GetAttribute("DataType");
                                break;
                            case "Reference":
                                referenceType = reader.GetAttribute("ReferenceType");
                                break;
                            case "Definition":
                                tagDefinition = true;
                                currentNode.DefinitionName = reader.GetAttribute("Name");
                                var splitd = currentNode.DefinitionName.Split(':');
                                if (splitd.Length > 1)
                                {
                                    currentNode.DefinitionNameSpace = splitd[0];
                                    if (splitd.Length == 2)
                                        currentNode.DefinitionName = splitd[1];
                                }
                                break;
                            case "Field":
                                field f = new field();
                                f.name = reader.GetAttribute("Name");
                                f.value = reader.GetAttribute("Value");
                                currentNode.fields.Add(f);
                                break;
                            case "Description":
                                break;
                        }
                        break;
                    case XmlNodeType.Text:
                        switch (elementName)
                        {
                            case "String":
                            case "DateTime":
                            case "Boolean":
                            case "Int32":
                            case "ByteString":
                            case "uax:String":
                            case "uax:DateTime":
                            case "uax:Boolean":
                            case "uax:Int32":
                            case "uax:Int16":
                            case "uax:ByteString":
                            case "uax:Float":
                                currentNode.Value = reader.Value;
                                break;
                            case "Description":
                                if (tagDefinition)
                                {
                                    int count = currentNode.fields.Count;
                                    if (count > 0)
                                    {
                                        currentNode.fields[count - 1].description = reader.Value;
                                    }
                                }
                                else
                                {
                                    currentNode.Description = reader.Value;
                                }
                                break;
                            case "Reference":
                                string reference = referenceType + " " + reader.Value;
                                currentNode.references.Add(reference);
                                break;
                            case "DisplayName":
                                currentNode.DisplayName = reader.Value;
                                break;
                        }
                        break;
                    case XmlNodeType.EndElement: //Display the end of the element.
                        switch (reader.Name)
                        {
                            case "Definition":
                                tagDefinition = false;
                                break;
                        }
                        if (currentNode == null || currentNode.UAObjectTypeName == null)
                        {
                            break;
                        }
                        if (reader.Name == currentNode.UAObjectTypeName)
                        {
                            switch (currentNode.UAObjectTypeName)
                            {
                                case "UADataType":
                                case "UAVariable":
                                case "UAObject":
                                case "UAMethod":
                                case "UAReferenceType":
                                case "UAObjectType":
                                case "UAVariableType":
                                    nodes.Add(currentNode);
                                    if (currentNode.ParentNodeId == null || currentNode.ParentNodeId == "")
                                    {
                                        roots.Add(currentNode);
                                    }
                                    else
                                    {
                                        // collect different parentNodeIDs to set corresponding node in dictionary later
                                        if (!parentNodes.ContainsKey(currentNode.ParentNodeId))
                                            parentNodes.Add(currentNode.ParentNodeId, null);
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }

            sw.Close();

            // scan nodes and store parent node in parentNodes value
            foreach (UaNode n in nodes)
            {
                if (parentNodes.TryGetValue(n.NodeId, out UaNode unused))
                {
                    parentNodes[n.NodeId] = n;
                }
            }

            // scan nodes and set parent and children for node
            foreach (UaNode n in nodes)
            {
                if (n.ParentNodeId != null && n.ParentNodeId != "")
                {
                    if (parentNodes.TryGetValue(n.ParentNodeId, out UaNode p))
                    {
                        if (p != null)
                        {
                            n.parent = p;
                            p.children.Add(n);
                        }
                        else
                        {
                            roots.Add(n);
                        }
                    }
                }
            }

            var outerSme = new SubmodelElementCollection(idShort:"OuterCollection");
            sm.Add(outerSme);
            var innerSme = new SubmodelElementCollection(idShort:"InnerCollection");
            sm.Add(innerSme);
            var conceptSme = new SubmodelElementCollection(idShort: "ConceptDescriptionCollection");
            sm.Add(conceptSme);

            // store models information
            var msemanticID = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, ModelUri+"models")});
            var msme = new SubmodelElementCollection(idShort:"Models",semanticId: msemanticID);
            msme.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UATypeName:Models"));
            innerSme.Add(msme);

            // modeluri
            msemanticID = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, ModelUri + "models/modeluri") });
            var mp = new Property(DataTypeDefXsd.String,idShort:"ModelUri",semanticId: msemanticID);
            mp.Value = ModelUri;
            msme.Add(mp);
            addLeaf(conceptSme, mp);
            // modeluriversion
            msemanticID = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, ModelUri + "models/modeluriversion") });
            mp = new Property(DataTypeDefXsd.String,idShort:"ModelUriVersion",semanticId: msemanticID);
            mp.Value = ModelUriVersion;
            msme.Add(mp);
            addLeaf(conceptSme, mp);
            // modeluripublicationdate
            msemanticID = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, ModelUri + "models/modeluripublicationdate") });
            mp = new Property(DataTypeDefXsd.String, idShort: "ModelUriPublicationDate", semanticId: msemanticID);
            mp.Value = ModelUriPublicationDate;
            msme.Add(mp);
            addLeaf(conceptSme, mp);
            // requiredmodeluri
            msemanticID = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, ModelUri + "models/requiredmodeluri") });
            mp = new Property(DataTypeDefXsd.String, idShort: "RequiredModelUri", semanticId: msemanticID);
            mp.Value = RequiredModelUri;
            msme.Add(mp);
            addLeaf(conceptSme, mp);
            // modeluriversion
            msemanticID = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, ModelUri + "models/requiredmodeluriversion") });
            mp = new Property(DataTypeDefXsd.String, idShort: "RequiredModelUriVersion", semanticId: msemanticID);
            mp.Value = RequiredModelUriVersion;
            msme.Add(mp);
            addLeaf(conceptSme, mp);
            // modeluripublicationdate
            msemanticID = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, ModelUri + "models/requiredmodeluripublicationdate") });
            mp = new Property(DataTypeDefXsd.String, idShort: "RequiredModelUriPublicationDate", semanticId: msemanticID);
            mp.Value = RequiredModelUriPublicationDate;
            msme.Add(mp);
            addLeaf(conceptSme, mp);

            // iterate through independent root trees
            // store UADataType to UADataTypeCollection in the end
            var semanticIDDataTypes = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, ModelUri + "UADataTypeCollection") });
            var smeDataTypes = new SubmodelElementCollection(
                idShort:"UADataTypeCollection",semanticId: semanticIDDataTypes);

            foreach (UaNode n in roots)
            {
                String name = n.BrowseName;
                if (n.SymbolicName != null && n.SymbolicName != "")
                {
                    name = n.SymbolicName;
                }
                var semanticID = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, ModelUri + name) });
                if ((n.children != null && n.children.Count != 0) ||
                    (n.fields != null && n.fields.Count != 0))
                {
                    var sme = new SubmodelElementCollection(idShort:name, semanticId: semanticID);
                    sme.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UATypeName:" + n.UAObjectTypeName));
                    switch (n.UAObjectTypeName)
                    {
                        case "UADataType":
                        case "UAObjectType":
                            smeDataTypes.Add(sme);
                            break;
                        default:
                            innerSme.Add(sme);
                            break;
                    }
                    if (n.Value != null && n.Value != "")
                    {
                        var p = createSE(n, ModelUri);
                        sme.Add(p);
                        addLeaf(conceptSme, p);
                    }
                    foreach (field f in n.fields)
                    {
                        sme.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UAField:" + f.name + " = " + f.value + " : " + f.description));


                        semanticID = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, ModelUri + name + "/" + f.name) });

                        var p = new Property(DataTypeDefXsd.String,idShort:f.name, semanticId: semanticID);
                        p.Value = f.value;
                        sme.Add(p);
                        addLeaf(conceptSme, p);
                    }
                    foreach (UaNode c in n.children)
                    {
                        createSubmodelElements(c, env, sme, smref, ModelUri + name + "/", conceptSme);
                    }
                }
                else
                {
                    var se = createSE(n, ModelUri);
                    switch (n.UAObjectTypeName)
                    {
                        case "UADataType":
                        case "UAObjectType":
                            smeDataTypes.Add(se);
                            addLeaf(conceptSme, se);
                            break;
                        default:
                            innerSme.Add(se);
                            addLeaf(conceptSme, se);
                            break;
                    }
                }
            }

            // Add datatypes in the end
            innerSme.Add(smeDataTypes);
        }

        public static void createSubmodelElements(
            UaNode n, AasCore.Aas3_0_RC02.Environment env, SubmodelElementCollection smec,
            Reference smref, string path, SubmodelElementCollection concepts)
        {
            String name = n.BrowseName;
            if (n.SymbolicName != null && n.SymbolicName != "")
            {
                name = n.SymbolicName;
            }
            var semanticID = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, path + name) });
            if ((n.children != null && n.children.Count != 0) ||
                (n.fields != null && n.fields.Count != 0))
            {
                var sme = new SubmodelElementCollection(idShort:name,semanticId: semanticID);
                sme.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UATypeName:" + n.UAObjectTypeName));

                smec.Add(sme);
                if (n.Value != "")
                {
                    var p = createSE(n, path);
                    sme.Add(p);
                    addLeaf(concepts, p);
                }
                foreach (field f in n.fields)
                {
                    sme.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UAField:" + f.name + " = " + f.value + " : " + f.description));

                    semanticID = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, path + name + "/" + f.name) });
                    var p = new Property(DataTypeDefXsd.String,idShort:f.name ,semanticId: semanticID);
                    p.Value = f.value;
                    sme.Add(p);
                    addLeaf(concepts, p);
                }
                if (n.children != null)
                {
                    foreach (UaNode c in n.children)
                    {
                        createSubmodelElements(c, env, sme, smref, path + name + "/", concepts);
                    }
                }
            }
            else
            {
                var se = createSE(n, path);
                smec.Add(se);
                addLeaf(concepts, se);
            }
        }

        public static ISubmodelElement createSE(UaNode n, string path)
        {
            ISubmodelElement se = null;

            String name = n.BrowseName;
            if (n.SymbolicName != null && n.SymbolicName != "")
            {
                name = n.SymbolicName;
            }

            // Check that semanticID only exists once and no overlapping names
            if (!semanticIDPool.ContainsKey(path + name))
            {
                semanticIDPool.Add(path + name, 0);
            }
            else
            {
                // Names are not unique
                string[] split = n.NodeId.Split('=');
                name += split[split.Length - 1];
                semanticIDPool.Add(path + name, 0);
            }
            var semanticID = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, path+name)});

            switch (n.UAObjectTypeName)
            {
                case "UAReferenceType":
                    se = new RelationshipElement(null, null, idShort:name,semanticId: semanticID);
                    if (se == null) return null;
                    break;
                default:
                    se = new Property(DataTypeDefXsd.String,idShort:name,semanticId: semanticID);
                    if (se == null) return null;
                    (se as Property).Value = n.Value;
                    break;
            }

            if (n.UAObjectTypeName == "UAVariable")
            {
                se.Category = "VARIABLE";
            }

            // TODO (MIHO/AO, 2022-01-07): change to Extensions?
            se.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UATypeName" + ":" + n.UAObjectTypeName));
            se.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UANodeId" + ":" + n.NodeId));
            if (n.ParentNodeId != null && n.ParentNodeId != "")
                se.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UAParentNodeId" + ":" + n.ParentNodeId));
            if (n.BrowseName != null && n.BrowseName != "")
                se.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UABrowseName" + ":" + n.BrowseName));
            if (n.DisplayName != null && n.DisplayName != "")
                se.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UADisplayName" + ":" + n.DisplayName));
            if (n.NameSpace != null && n.NameSpace != "")
                se.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UANameSpace" + ":" + n.NameSpace));
            if (n.SymbolicName != null && n.SymbolicName != "")
                se.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UASymbolicName" + ":" + n.SymbolicName));
            if (n.DataType != null && n.DataType != "")
                se.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UADataType" + ":" + n.DataType));
            if (n.Description != null && n.Description != "")
                se.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UADescription" + ":" + n.Description));
            foreach (string s in n.references)
            {
                se.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UAReference" + ":" + s));
            }
            if (n.DefinitionName != null && n.DefinitionName != "")
                se.SemanticId.Keys.Add(new Key(KeyTypes.GlobalReference, "UADefinitionName" + ":" + n.DefinitionName));
            if (n.DefinitionNameSpace != null && n.DefinitionNameSpace != "")
                se.SemanticId.Keys.Add(
                    new Key(KeyTypes.GlobalReference, "UADefinitionNameSpace" + ":" + n.DefinitionNameSpace));
            foreach (field f in n.fields)
            {
                se.SemanticId.Keys.Add(
                    new Key(KeyTypes.GlobalReference, "UAField" + ":" + f.name + " = " + f.value + " : " + f.description));
            }

            return se;
        }

        public static void addLeaf(SubmodelElementCollection concepts, ISubmodelElement sme)
        {
            var se = new Property(DataTypeDefXsd.String,idShort:sme.IdShort,semanticId: sme.SemanticId);
            concepts.Add(se);
        }
    }
}
