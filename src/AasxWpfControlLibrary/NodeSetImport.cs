using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using AdminShellNS;

namespace AasxPackageExplorer
{


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

        public UaNode()
        {
            children = new List<UaNode> { };
            references = new List<string> { };
        }
    }

    public static class OpcUaTools
    {
        static List<UaNode> roots;
        static List<UaNode> nodes;
        static Dictionary<string,UaNode> parentNodes;
        static UaNode[] stack;
        static int iStack = 0;

        public static void ImportNodeSetToSubModel(string inputFn, AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm, AdminShell.SubmodelRef smref)
        {
            XmlTextReader reader = new XmlTextReader(inputFn);
            StreamWriter sw = File.CreateText(inputFn + ".log.txt");

            string elementName = "";
            string referenceType = "";

            roots = new List<UaNode> { };
            nodes = new List<UaNode> { };
            parentNodes = new Dictionary<string, UaNode>();
            UaNode currentNode = null;
            iStack = 0;

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
                                currentNode.Value = reader.Value;
                                break;
                            case "Description":
                                currentNode.Description = reader.Value;
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
                UaNode p = null;
                if (parentNodes.TryGetValue(n.NodeId, out p))
                {
                    parentNodes[n.NodeId] = n;
                }
            }

            // scan nodes and set parent and children for node
            foreach (UaNode n in nodes)
            {
                if (n.ParentNodeId != null && n.ParentNodeId != "")
                {
                    UaNode p = null;
                    if (parentNodes.TryGetValue(n.ParentNodeId, out p))
                    {
                        n.parent = p;
                        p.children.Add(n);
                    }
                }
            }

            // store models information
            /*
            ModelUri = reader.GetAttribute("ModelUri");
            ModelUriVersion = reader.GetAttribute("Version");
            ModelUriPublicationDate = reader.GetAttribute("PublicationDate");
            RequiredModelUri = reader.GetAttribute("ModelUri");
            RequiredModelUriVersion = reader.GetAttribute("Version");
            RequiredModelUriPublicationDate = reader.GetAttribute("PublicationDate");
            */
            var msemanticID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", ModelUri + "models");
            var msme = AdminShell.SubmodelElementCollection.CreateNew("Models", null, msemanticID);
            msme.semanticId.Keys.Add(AdminShell.Key.CreateNew("UATypeName", false, "OPC", "Models"));
            sm.Add(msme);
            // modeluri
            msemanticID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", ModelUri + "models/modeluri");
            var mp = AdminShell.Property.CreateNew("ModelUri", null, msemanticID);
            mp.valueType = "string";
            mp.value = ModelUri;
            msme.Add(mp);
            // modeluriversion
            msemanticID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", ModelUri + "models/modeluriversion");
            mp = AdminShell.Property.CreateNew("ModelUriVersion", null, msemanticID);
            mp.valueType = "string";
            mp.value = ModelUriVersion;
            msme.Add(mp);
            // modeluripublicationdate
            msemanticID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", ModelUri + "models/modeluripublicationdate");
            mp = AdminShell.Property.CreateNew("ModelUriPublicationDate", null, msemanticID);
            mp.valueType = "string";
            mp.value = ModelUriPublicationDate;
            msme.Add(mp);
            // requiredmodeluri
            msemanticID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", ModelUri + "models/requiredmodeluri");
            mp = AdminShell.Property.CreateNew("RequiredModelUri", null, msemanticID);
            mp.valueType = "string";
            mp.value = RequiredModelUri;
            msme.Add(mp);
            // modeluriversion
            msemanticID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", ModelUri + "models/requiredmodeluriversion");
            mp = AdminShell.Property.CreateNew("RequiredModelUriVersion", null, msemanticID);
            mp.valueType = "string";
            mp.value = RequiredModelUriVersion;
            msme.Add(mp);
            // modeluripublicationdate
            msemanticID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", ModelUri + "models/requiredmodeluripublicationdate");
            mp = AdminShell.Property.CreateNew("RequiredModelUriPublicationDate", null, msemanticID);
            mp.valueType = "string";
            mp.value = RequiredModelUriPublicationDate;
            msme.Add(mp);

            // iterate through independent root trees
            foreach (UaNode n in roots)
            {
                String name = n.BrowseName;
                if (n.SymbolicName != null && n.SymbolicName != "")
                {
                    name = n.SymbolicName;
                }
                var semanticID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", ModelUri + name);
                if (n.children != null && n.children.Count != 0)
                {
                    var sme = AdminShell.SubmodelElementCollection.CreateNew(name, null, semanticID);
                    sme.semanticId.Keys.Add(AdminShell.Key.CreateNew("UATypeName", false, "OPC", n.UAObjectTypeName));
                    sm.Add(sme);
                    if (n.Value != "")
                    {
                        var p = AdminShell.Property.CreateNew(name, null, semanticID);
                        storeProperty(n, ref p);
                        sme.Add(p);
                    }
                    foreach (UaNode c in n.children)
                    {
                        createSubmodelElements(c, env, sme, smref, ModelUri + name + "/");
                    }
                }
                else
                {
                    var p = AdminShell.Property.CreateNew(name, null, semanticID);
                    storeProperty(n, ref p);
                    sm.Add(p);
                }
            }
        }

        public static void createSubmodelElements(UaNode n, AdminShell.AdministrationShellEnv env, AdminShell.SubmodelElementCollection smec, AdminShell.SubmodelRef smref, string path)
        {
            String name = n.BrowseName;
            if (n.SymbolicName != null && n.SymbolicName != "")
            {
                name = n.SymbolicName;
            }
            var semanticID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", path + name);
            if (n.children != null && n.children.Count != 0)
            {
                var sme = AdminShell.SubmodelElementCollection.CreateNew(name, null, semanticID);
                sme.semanticId.Keys.Add(AdminShell.Key.CreateNew("UATypeName", false, "OPC", n.UAObjectTypeName));
                smec.Add(sme);
                if (n.Value != "")
                {
                    var p = AdminShell.Property.CreateNew(name, null, semanticID);
                    storeProperty(n, ref p);
                    sme.Add(p);
                }
                foreach (UaNode c in n.children)
                {
                    createSubmodelElements(c, env, sme, smref, path + name + "/");
                }
            }
            else
            {
                var p = AdminShell.Property.CreateNew(name, null, semanticID);
                storeProperty(n, ref p);
                smec.Add(p);
            }
        }

        public static void storeProperty(UaNode n, ref AdminShell.Property p)
        {
            p.valueType = "string";
            p.value = n.Value;
            p.semanticId.Keys.Add(AdminShell.Key.CreateNew("UATypeName", false, "OPC", n.UAObjectTypeName));
            p.semanticId.Keys.Add(AdminShell.Key.CreateNew("UANodeId", false, "OPC", n.NodeId));
            if (n.ParentNodeId != null && n.ParentNodeId != "")
                p.semanticId.Keys.Add(AdminShell.Key.CreateNew("UAParentNodeId", false, "OPC", n.ParentNodeId));
            if (n.BrowseName != null && n.BrowseName != "")
                p.semanticId.Keys.Add(AdminShell.Key.CreateNew("UABrowseName", false, "OPC", n.BrowseName));
            if (n.DisplayName != null && n.DisplayName != "")
                p.semanticId.Keys.Add(AdminShell.Key.CreateNew("UADisplayName", false, "OPC", n.DisplayName));
            if (n.NameSpace != null && n.NameSpace != "")
                p.semanticId.Keys.Add(AdminShell.Key.CreateNew("UANameSpace", false, "OPC", n.NameSpace));
            if (n.SymbolicName != null && n.SymbolicName != "")
                p.semanticId.Keys.Add(AdminShell.Key.CreateNew("UASymbolicName", false, "OPC", n.SymbolicName));
            if (n.DataType != null && n.DataType != "")
                p.semanticId.Keys.Add(AdminShell.Key.CreateNew("UADataType", false, "OPC", n.DataType));
            if (n.Description != null && n.Description != "")
                p.semanticId.Keys.Add(AdminShell.Key.CreateNew("UADescription", false, "OPC", n.Description));
            foreach (string s in n.references)
            {
                p.semanticId.Keys.Add(AdminShell.Key.CreateNew("UAReference", false, "OPC", s));
            }
        }

        public static void ImportNodeSetToSubModel_OLD(string inputFn, AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm, AdminShell.SubmodelRef smref)
        {
            // OPC UA NodeSet
            // Input
            Boolean is_Iri = false;
            String Uri = "";
            Boolean is_UADataType = false;
            Boolean is_UAVariable = false;
            Boolean is_UAObject = false;
            Boolean is_UAMethod = false;
            Boolean is_DisplayName = false;
            Boolean is_Value = false;
            String DisplayName = "";
            // String Field_Name = "";
            // String Field_Value = "";
            // Output
            String Name = "";
            String Value = "";

            AdminShell.SubmodelElementCollection[] propGroup = new AdminShell.SubmodelElementCollection[10];
            int i_propGroup = 0;

            XmlTextReader reader = new XmlTextReader(inputFn);
            StreamWriter sw = File.CreateText(inputFn + ".log.txt");

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.
                        if (reader.Name == AdminShell.Identification.IRI)
                            is_Iri = true;
                        if (reader.Name == "UADataType")
                            is_UADataType = true;
                        if (reader.Name == "UAVariable")
                            is_UAVariable = true;
                        if (reader.Name == "UAObject")
                            is_UAObject = true;
                        if (reader.Name == "UAMethod")
                            is_UAMethod = true;
                        if (reader.Name == "DisplayName")
                            is_DisplayName = true;
                        if (reader.Name == "Value")
                            is_Value = true;
                        if (reader.Name == "Field")
                        {
                            Name = reader.GetAttribute("Name");
                            Value = reader.GetAttribute("Value");
                        }
                        break;
                    case XmlNodeType.Text: //Display the text in each element.
                        if (is_Iri)
                        {
                            Uri = reader.Value;
                        }
                        if (is_DisplayName)
                        {
                            DisplayName = reader.Value;
                        }
                        if (is_UAVariable && is_Value)
                        {
                            if (reader.Value.Length < 100)
                            {
                                Value = reader.Value;
                            }
                            else
                            {
                                Value = reader.Value.Substring(0, 100) + " ..";
                            }
                        }
                        break;
                    case XmlNodeType.EndElement: //Display the end of the element.
                        if (is_Iri)
                        {
                            Name = "NamespaceURI";
                            Value = Uri;
                            is_Iri = false;
                            Uri = "";
                        }
                        if (is_UADataType && is_DisplayName)
                        {
                            propGroup[i_propGroup] = AdminShell.SubmodelElementCollection.CreateNew("UADataType " + DisplayName);
                            sm.Add(propGroup[i_propGroup]);
                            i_propGroup++;
                            is_UADataType = false;
                            is_DisplayName = false;
                            DisplayName = "";
                        }
                        if (reader.Name == "UADataType")
                            i_propGroup--;
                        if (reader.Name == "UAVariable")
                        {
                            Name = "UAVariable " + DisplayName;
                            DisplayName = "";
                            is_DisplayName = false;
                            is_UAVariable = false;
                        }
                        if (is_UAObject)
                        {
                            Name = "UAObject " + DisplayName;
                            DisplayName = "";
                            is_DisplayName = false;
                            is_UAObject = false;
                        }
                        if (is_UAMethod)
                        {
                            Name = "UAMethod " + DisplayName;
                            DisplayName = "";
                            is_DisplayName = false;
                            is_UAMethod = false;
                        }
                        if (is_Value)
                        {
                            is_Value = false;
                        }
                        if (is_DisplayName)
                        {
                            is_DisplayName = false;
                        }
                        break;
                }
                if (Name != "")
                {
                    using (var cd = AdminShell.ConceptDescription.CreateNew(AdminShell.Identification.IRDI, ""))
                    {
                        env.ConceptDescriptions.Add(cd);
                        cd.SetIEC61360Spec(
                            preferredNames: new[] { "EN", Name },
                            shortName: Name,
                            unit: "string",
                            valueFormat: "STRING",
                            definition: new[] { "EN", Name }
                        );

                        var p = AdminShell.Property.CreateNew(cd.GetDefaultShortName(), "PARAMETER", AdminShell.Key.GetFromRef(cd.GetReference()));
                        p.valueType = "string";
                        p.value = Value;

                        if (i_propGroup > 0)
                        {
                            propGroup[i_propGroup - 1].Add(p);
                        }
                        else
                        {
                            sm.Add(p);
                        }
                    }
                    Name = "";
                    Value = "";
                }
            }
            sw.Close();
        }

    }
}
