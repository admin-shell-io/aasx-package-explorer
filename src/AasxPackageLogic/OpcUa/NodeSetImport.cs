/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using Aas = AasCore.Aas3_0;

namespace AasxPackageLogic
{
    public static class OpcUaTools
    {
        public class UaField
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
            public List<UaField> fields;

            public UaNode()
            {
                children = new List<UaNode>();
                references = new List<string>();
                fields = new List<UaField>();
            }
        }


        static List<UaNode> roots;
        static List<UaNode> nodes;
        static Dictionary<string, UaNode> parentNodes;
        static Dictionary<string, Int16> semanticIDPool;

        public static void ImportNodeSetToSubModel(
            string inputFn, Aas.IEnvironment env, Aas.ISubmodel sm,
            Aas.IReference smref)
        {
#if OLD_V20_CODE
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
                                UaField f = new UaField();
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

            var outerSme = new Aas.SubmodelElementCollection(idShort: "OuterCollection");
            sm.Add(outerSme);
            var innerSme = new Aas.SubmodelElementCollection(idShort: "InnerCollection");
            sm.Add(innerSme);
            var conceptSme = new Aas.SubmodelElementCollection(idShort: "ConceptDescriptionCollection");
            sm.Add(conceptSme);

            // store models information
            var msemanticID = ExtendReference.CreateFromKey(new Key(Aas.KeyTypes.GlobalReference, ModelUri + "models"));
            var msme = new Aas.SubmodelElementCollection(idShort: "Models", semanticId: msemanticID);
            msme.SemanticId.Keys.Add(new Key(Aas.KeyTypes.GlobalReference, "Models"));
            innerSme.Add(msme);

            // modeluri
            msemanticID = ExtendReference.CreateFromKey(new Key(Aas.KeyTypes.GlobalReference, ModelUri + "models/modeluri"));
            var mp = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "ModelUri", semanticId: msemanticID);
            mp.Value = ModelUri;
            msme.Add(mp);
            addLeaf(conceptSme, mp);
            // modeluriversion
            msemanticID = AdminShell.Aas.Key.CreateNew(
                "GlobalReference", false, "IRI", ModelUri + "models/modeluriversion");
            mp = AdminShell.Aas.Property.CreateNew("ModelUriVersion", null, msemanticID);
            mp.valueType = "string";
            mp.value = ModelUriVersion;
            msme.Add(mp);
            addLeaf(conceptSme, mp);
            // modeluripublicationdate
            msemanticID = AdminShell.Aas.Key.CreateNew(
                "GlobalReference", false, "IRI", ModelUri + "models/modeluripublicationdate");
            mp = AdminShell.Aas.Property.CreateNew("ModelUriPublicationDate", null, msemanticID);
            mp.valueType = "string";
            mp.value = ModelUriPublicationDate;
            msme.Add(mp);
            addLeaf(conceptSme, mp);
            // requiredmodeluri
            msemanticID = AdminShell.Aas.Key.CreateNew(
                "GlobalReference", false, "IRI", ModelUri + "models/requiredmodeluri");
            mp = AdminShell.Aas.Property.CreateNew("RequiredModelUri", null, msemanticID);
            mp.valueType = "string";
            mp.value = RequiredModelUri;
            msme.Add(mp);
            addLeaf(conceptSme, mp);
            // modeluriversion
            msemanticID = AdminShell.Aas.Key.CreateNew(
                "GlobalReference", false, "IRI", ModelUri + "models/requiredmodeluriversion");
            mp = AdminShell.Aas.Property.CreateNew("RequiredModelUriVersion", null, msemanticID);
            mp.valueType = "string";
            mp.value = RequiredModelUriVersion;
            msme.Add(mp);
            addLeaf(conceptSme, mp);
            // modeluripublicationdate
            msemanticID = AdminShell.Aas.Key.CreateNew(
                "GlobalReference", false, "IRI", ModelUri + "models/requiredmodeluripublicationdate");
            mp = AdminShell.Aas.Property.CreateNew("RequiredModelUriPublicationDate", null, msemanticID);
            mp.valueType = "string";
            mp.value = RequiredModelUriPublicationDate;
            msme.Add(mp);
            addLeaf(conceptSme, mp);

            // iterate through independent root trees
            // store UADataType to UADataTypeCollection in the end
            var semanticIDDataTypes = AdminShell.Aas.Key.CreateNew(
                "GlobalReference", false, "IRI", ModelUri + "UADataTypeCollection");
            var smeDataTypes = new Aas.SubmodelElementCollection(idShort: 
                "UADataTypeCollection", null, semanticIDDataTypes);

            foreach (UaNode n in roots)
            {
                String name = n.BrowseName;
                if (n.SymbolicName != null && n.SymbolicName != "")
                {
                    name = n.SymbolicName;
                }
                var semanticID = AdminShell.Aas.Key.CreateNew("GlobalReference", false, "IRI", ModelUri + name);
                if ((n.children != null && n.children.Count != 0) ||
                    (n.fields != null && n.fields.Count != 0))
                {
                    var sme = new Aas.SubmodelElementCollection(idShort: name, null, semanticID);
                    sme.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UATypeName", false, "OPC", n.UAObjectTypeName));
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
                    foreach (UaField f in n.fields)
                    {
                        sme.semanticId.Keys.Add(
                            AdminShell.Aas.Key.CreateNew(
                                "UAField", false, "OPC", f.name + " = " + f.value + " : " + f.description));

                        semanticID = AdminShell.Aas.Key.CreateNew(
                            "GlobalReference", false, "IRI", ModelUri + name + "/" + f.name);

                        var p = AdminShell.Aas.Property.CreateNew(f.name, null, semanticID);
                        p.valueType = "string";
                        p.value = f.value;
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
#endif
        }

#if OLD_V20_CODE
        public static void createSubmodelElements(
            UaNode n, AdminShell.AdministrationShellEnv env, AdminShell.Aas.SubmodelElementCollection smec,
            AdminShell.SubmodelRef smref, string path, AdminShell.Aas.SubmodelElementCollection concepts)
        {
            String name = n.BrowseName;
            if (n.SymbolicName != null && n.SymbolicName != "")
            {
                name = n.SymbolicName;
            }
            var semanticID = AdminShell.Aas.Key.CreateNew("GlobalReference", false, "IRI", path + name);
            if ((n.children != null && n.children.Count != 0) ||
                (n.fields != null && n.fields.Count != 0))
            {
                var sme = new Aas.SubmodelElementCollection(idShort: name, null, semanticID);
                sme.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UATypeName", false, "OPC", n.UAObjectTypeName));
                smec.Add(sme);
                if (n.Value != "")
                {
                    var p = createSE(n, path);
                    sme.Add(p);
                    addLeaf(concepts, p);
                }
                foreach (UaField f in n.fields)
                {
                    sme.semanticId.Keys.Add(
                        AdminShell.Aas.Key.CreateNew(
                            "UAField", false, "OPC", f.name + " = " + f.value + " : " + f.description));
                    semanticID = AdminShell.Aas.Key.CreateNew("GlobalReference", false, "IRI", path + name + "/" + f.name);
                    var p = AdminShell.Aas.Property.CreateNew(f.name, null, semanticID);
                    p.valueType = "string";
                    p.value = f.value;
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

        public static AdminShell.SubmodelElement createSE(UaNode n, string path)
        {
            AdminShell.SubmodelElement se = null;

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
            var semanticID = AdminShell.Aas.Key.CreateNew("GlobalReference", false, "IRI", path + name);

            switch (n.UAObjectTypeName)
            {
                case "UAReferenceType":
                    se = AdminShell.Aas.RelationshipElement.CreateNew(name, null, semanticID);
                    if (se == null) return null;
                    break;
                default:
                    se = AdminShell.Aas.Property.CreateNew(name, null, semanticID);
                    if (se == null) return null;
                    (se as AdminShell.Aas.Property).valueType = "string";
                    (se as AdminShell.Aas.Property).value = n.Value;
                    break;
            }

            if (n.UAObjectTypeName == "UAVariable")
            {
                se.category = "VARIABLE";
            }

            se.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UATypeName", false, "OPC", n.UAObjectTypeName));
            se.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UANodeId", false, "OPC", n.NodeId));
            if (n.ParentNodeId != null && n.ParentNodeId != "")
                se.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UAParentNodeId", false, "OPC", n.ParentNodeId));
            if (n.BrowseName != null && n.BrowseName != "")
                se.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UABrowseName", false, "OPC", n.BrowseName));
            if (n.DisplayName != null && n.DisplayName != "")
                se.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UADisplayName", false, "OPC", n.DisplayName));
            if (n.NameSpace != null && n.NameSpace != "")
                se.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UANameSpace", false, "OPC", n.NameSpace));
            if (n.SymbolicName != null && n.SymbolicName != "")
                se.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UASymbolicName", false, "OPC", n.SymbolicName));
            if (n.DataType != null && n.DataType != "")
                se.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UADataType", false, "OPC", n.DataType));
            if (n.Description != null && n.Description != "")
                se.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UADescription", false, "OPC", n.Description));
            foreach (string s in n.references)
            {
                se.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UAReference", false, "OPC", s));
            }
            if (n.DefinitionName != null && n.DefinitionName != "")
                se.semanticId.Keys.Add(AdminShell.Aas.Key.CreateNew("UADefinitionName", false, "OPC", n.DefinitionName));
            if (n.DefinitionNameSpace != null && n.DefinitionNameSpace != "")
                se.semanticId.Keys.Add(
                    AdminShell.Aas.Key.CreateNew(
                        "UADefinitionNameSpace", false, "OPC", n.DefinitionNameSpace));
            foreach (UaField f in n.fields)
            {
                se.semanticId.Keys.Add(
                    AdminShell.Aas.Key.CreateNew(
                        "UAField", false, "OPC", f.name + " = " + f.value + " : " + f.description));
            }

            return se;
        }

        public static void addLeaf(AdminShell.Aas.SubmodelElementCollection concepts, AdminShell.SubmodelElement sme)
        {
            var se = AdminShell.Aas.Property.CreateNew(sme.idShort, null, sme.semanticId[0]);
            concepts.Add(se);
        }
#endif
    }
}
