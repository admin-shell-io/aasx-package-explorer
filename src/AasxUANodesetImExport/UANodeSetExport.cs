using AdminShellNS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using static AdminShellNS.AdminShellV20;
using File = AdminShellNS.AdminShellV20.File;

namespace AasxUANodesetImExport
{
    public class UANodeSetExport
    {

        //consists of every single node that will be created
        public static List<UANode> root = new List<UANode>();

        private static int masterID = 7500;
        private static string typesNS = "http://admin-shell.io/OPC_UA_CS/Types.xsd";
        private static int currentNamespace = 1;

        public static UANodeSet getInformationModel(string filename)
        {
            string path = filename;
            UANodeSet InformationModel = new UANodeSet();
            string executebaleLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string location = Path.Combine(executebaleLocation, path);
            string xml = System.IO.File.ReadAllText(path);

            XmlSerializer serializer = new XmlSerializer(typeof(UANodeSet));
            using (TextReader reader = new StringReader(xml))
            {
                try
                {
                    InformationModel = (UANodeSet)serializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    //    MessageBoxResult result = MessageBox.Show("Error in XML formatting\n\n" + ex.ToString(),
                    //                          "Error",
                    //                          MessageBoxButton.OK);
                    throw ex;
                }

            }
            return InformationModel;
        }

        public static UANodeSet getDefaultI4AAS()
        {
            var thisAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            UANodeSet InformationModel = new UANodeSet();
            string fullname = "AasxUANodesetImExport.i4AASCS.xml";
            try
            {
                using (Stream stream = thisAssembly.GetManifestResourceStream(fullname))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(UANodeSet));
                    InformationModel = (UANodeSet)serializer.Deserialize(stream);
                }

            }
            catch (Exception ex)
            {
                //MessageBoxResult result = MessageBox.Show("Error loading I4AAS CS: \n\n" + ex.ToString(),
                //                      "Error",
                //                      MessageBoxButton.OK);
            }
            return InformationModel;
        }

        public static string createRootObject(string name, AdministrationShellEnv env)
        {
            UAObject rootObj = new UAObject();
            rootObj.NodeId = "ns=" + currentNamespace + ";i=" + masterID.ToString();
            rootObj.BrowseName = currentNamespace + ":AASAssetAdministrationShell";
            masterID++;
            List<Reference> refs = new List<Reference>();

            refs.Add(new Reference
            {
                IsForward = false,
                Value = "i=85",
                ReferenceType = "Organizes"
            });
            refs.Add(CreateHasTypeDefinition("1:AASAssetAdministrationShellType"));


            foreach(AdministrationShell shell in env.AdministrationShells)
            {
                //IAASIdentifiableType
                refs.Add(CreateReference("HasInterface", createIAASIdentifiable(shell)));

                //SubmodelReference
                foreach (SubmodelRef subRef in shell.submodelRefs)
                {
                    if (subRef != null) refs.Add(CreateReference("HasComponent", createAASReference("SubmodelReference", subRef, "3:http://admin-shell.io/aas/2/0/AssetAdministrationShell/submodels")));
                }

                //DerivedFrom -> AASReferenceType
                refs.Add(CreateReference("HasComponent", createAASReference("DerivedFrom", shell.derivedFrom, "3:http://admin-shell.io/aas/2/0/AssetAdministrationShell/derivedFrom")));

                //DataSpecification -> ReferenceType
                refs.Add(createDataSpecification("DataSpecification:" + shell.idShort, shell.hasDataSpecification, "3:http://admin-shell.io/aas/2/0/AssetAdministrationShell/dataSpecifications"));

                //View Liste
                if(shell.views != null && shell.views.views != null)
                {
                    foreach (View view in shell.views.views)
                    {
                        refs.Add(CreateReference("HasComponent", createAASView(view, "3:http://admin-shell.io/aas/2/0/AssetAdministrationShell/asset")));
                    }
                }
            }

            //Asset -> AASAssetType
            foreach (Asset asset in env.Assets)
            {
                refs.Add(CreateReference("HasComponent", createAASAsset(asset.idShort, asset, "3:http://admin-shell.io/aas/2/0/AssetAdministrationShell/asset")));
            }

            //Submodel Liste
            foreach(Submodel sub in env.Submodels)
            {
                refs.Add(CreateReference("HasComponent", createSubmodel(sub, "3:http://admin-shell.io/aas/2/0/AssetAdministrationShell/submodel")));
            }

            //UriDictionaryEntry
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/AssetAdministrationShell")));


            rootObj.References = refs.ToArray();
            root.Add((UANode)rootObj);
            return rootObj.NodeId;
        }

        //Helper

        private static UAObject createObject(string name)
        {
            UAObject obj = new UAObject();
            obj.NodeId = "ns=" + currentNamespace + ";i=" + masterID.ToString();
            obj.BrowseName = currentNamespace + ":" + name;
            masterID++;
            return obj;
        }

        private static List<Reference> createReferenceList(String typeDef, String addRef)
        {
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition(typeDef));
            if(!String.IsNullOrEmpty(addRef)) refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry(addRef)));
            return refs;
        }

        private static Reference CreateReference(string type, string value)
        {
            if (value != null)
            {
                //check if Type is "Base"Type (not seperately created)
                if (value.Contains("ns=1;i="))
                {
                    value = value.Replace("ns=1;i=", "");
                    Reference _ref = new Reference
                    {
                        Value = "ns=1;i=" + value,
                        ReferenceType = type
                    };
                    return _ref;
                }
                else if(value.Contains("ns=" + currentNamespace + ";i="))
                {
                    value = value.Replace("ns=" + currentNamespace +";i=", "");
                    Reference _ref = new Reference
                    {
                        Value = "ns=" + currentNamespace + ";i=" + value,
                        ReferenceType = type
                    };
                    return _ref;
                }
                else
                {
                    Reference _ref = new Reference
                    {
                        Value = value,
                        ReferenceType = type
                    };
                    return _ref;
                }

            }
            return null;
        }

        private static Reference CreateHasTypeDefinition(string type)
        {
            string _value = findBaseType(type);

            if (_value == null)
            {
                try
                {
                    _value = root.Find(x => x.BrowseName == type).NodeId;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return null;
                }
            }

            return CreateReference("HasTypeDefinition", _value);
        }

        private static string findBaseType(string type)
        {
            //because the BaseTypes are not in the mapping XML
            //they need to be somewhere, hard coded
            string value = null;
            switch (type)
            {
                case "BaseVariableType":
                    value = "ns=0;i=62";
                    break;

                case "BaseObjectType":
                    value = "ns=0;i=58";
                    break;

                case "PropertyType":
                    value = "ns=0;i=68";
                    break;

                case "DictionaryEntryType":
                    value = "ns=0;i=17589";
                    break;

                case "FileType":
                    value = "ns=0;i=11575";
                    break;

                case "BaseDataVariableType":
                    value = "ns=0;i=63";
                    break;

                case "String":
                    value = "ns=0;i=17589";
                    break;

                case "UriDictionaryEntryType":
                    value = "ns=0;i=17600";
                    break;
            }
            return value;
        }

        private static string createProperty(string name, string value, string datatype, string addRefs)
        {
            UAVariable prop = new UAVariable();
            prop.BrowseName = currentNamespace + ":" + name;
            prop.NodeId = "ns=" +currentNamespace + ";i=" + masterID.ToString();
            prop.DataType = datatype;
            masterID++;
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

            //Remove ':', because it's not allowed in XML
            if (datatype.Contains(':'.ToString()))
            {
                var strings = datatype.Split(':');
                datatype = strings[1];
            }

            var isNumeric = int.TryParse(value, out int n);
            if (isNumeric)
            {
                //Create XMLElement to store Value in
                System.Xml.XmlElement element = doc.CreateElement("uax", "Int32", "");
                element.InnerText = value;
                prop.Value = element;
            }
            else
            {
                //Create XMLElement to store Value in
                System.Xml.XmlElement element = doc.CreateElement("uax", datatype, "http://opcfoundation.org/UA/2008/02/Types.xsd");
                element.InnerText = value;
                prop.Value = element;
            }



            prop.References = new Reference[2];

            prop.References[0] = CreateHasTypeDefinition("PropertyType");
            prop.References[1] = CreateHasDictionaryEntry(addRefs);
            root.Add((UANode)prop);

            return prop.NodeId;


        }

        //...

        private static string createUriDictionaryEntry(string name)
        {
            UAObject obj = new UAObject();
            obj.NodeId = "ns=" + currentNamespace + ";i=" + masterID.ToString();
            obj.BrowseName = name;
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("UriDictionaryEntryType"));
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        private static string createAASReference(string name, AdminShellV20.Reference aasref, string addRef)
        {
            if(aasref != null)
            {
                UAObject obj = createObject(name);
                var refs = createReferenceList("1:AASReferenceType", addRef);

                //Keys -> AASKeyDataType[]
                refs.Add(CreateReference("HasProperty", CreateKeyProperty(aasref.Keys, "Keys", "3:http://admin-shell.io/aas/2/0/Reference/keys")));

                obj.References = refs.ToArray();
                root.Add((UANode)obj);
                return obj.NodeId;
            }
            return null;
        }

        private static Reference createDataSpecification(string name, HasDataSpecification spec, string addRefs)
        {
            if (spec == null) return null;

            var obj = createObject(name);
            var refs = createReferenceList("1:AASDataSpecificationType", addRefs);

            if (spec.IEC61360 != null) refs.Add(CreateReference("HasProperty", CreateKeyProperty(spec.IEC61360.dataSpecification.Keys, "DataSpecification", "3:http://admin-shell.io/aas/2/0/Identifiable")));
            
            foreach(EmbeddedDataSpecification emSpec in spec)
            {
                refs.Add(CreateReference("HasProperty", CreateKeyProperty(emSpec.dataSpecification.Keys, "DataSpecification", "3:http://admin-shell.io/aas/2/0/Identifiable")));
            }
            
            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return CreateReference("HasComponent", obj.NodeId);
        }

        //Submodel Stuff

        private static string createSubmodel(Submodel sub, string addRefs)
        {
            var obj = createObject("Submodel:" + sub.idShort);
            var refs = createReferenceList("1:AASSubmodelType", addRefs);
            refs.Add(CreateHasDictionaryEntry("3:http://admin-shell.io/aas/2/0/Submodel"));

            //SemanticId
            refs.Add(CreateReference("HasDictionaryEntry", createAASReference("SemanticId", sub.semanticId, "")));

            //IAASIdentifiable
            refs.Add(CreateReference("HasInterface", createIAASIdentifiable(sub.administration, sub.identification)));

            //DataSpecification
            refs.Add(createDataSpecification("DataSpecification", sub.hasDataSpecification, "3:http://admin-shell.io/aas/2/0/Asset/dataSpecifications"));

            //ModellingKind -> Property
            refs.Add(CreateReference("HasProperty", createProperty("ModelingKind", GetAASModelingKindDataType(sub.kind.kind).ToString(), "AASModelingKindDataType", "3:http://admin-shell.io/aas/2/0/HasKind/kind")));

            //Qualifiert -> QualifierType
            foreach(Qualifier qual in sub.qualifiers)
            {
                refs.Add(CreateReference("HasCpomponent", createQualifier(qual, "3:http://admin-shell.io/aas/2/0/Submodel/qualifiers")));
            }

            //SubmodelElement -> Liste
            foreach(SubmodelElementWrapper element in sub.submodelElements)
            {
                refs.Add(CreateReference("HasComponent", createSubmodelElement(element.submodelElement, "3:http://admin-shell.io/aas/2/0/Submodel/submodelElements")));
            }

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        private static string createQualifier(Qualifier qual, string addRefs)
        {
            var obj = createObject("Qualifier:" + qual.type + ":" + qual.value);
            var refs = createReferenceList("1:AASQualifierType", addRefs);
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/Qualifier")));

            //Type -> Property
            refs.Add(CreateReference("HasProperty", createProperty("Type", qual.type, "String", "3:http://admin-shell.io/aas/2/0/Qualifier/type")));

            //ValueType -> Property
            refs.Add(CreateReference("HasProperty", createProperty("ValueType", GetAASValueTypeDataType(qual.valueType).ToString(), "AASValueTypeDataType", "3:http://admin-shell.io/aas/2/0/Qualifier/valueType")));

            //Value -> Property
            refs.Add(CreateReference("HasProperty", createProperty("Value", qual.value, "BaseDataType", "3:http://admin-shell.io/aas/2/0/Qualifier/value")));

            //ValueId -> AASReference
            refs.Add(CreateReference("HasProperty", CreateKeyProperty(qual.valueId.Keys, "ValueId", "")));

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        //A lot of SubmodelElementTypes

        private static string createSubmodelElement(SubmodelElement element, string addRefs)
        {
            var obj = createObject(element.GetElementName() + ": " + element.idShort);
            var refs = new List<Reference>();
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry(addRefs)));

            //IdShort -> Property
            refs.Add(CreateReference("HasProperty", createProperty("IdShort", element.idShort, "String", "3:http://admin-shell.io/aas/2/0/SubmodelElement/idShort")));

            //Category -> Property
            refs.Add(CreateReference("HasProperty", createProperty("Category", element.category, "String", "3:http://admin-shell.io/aas/2/0/Referable/category")));

            //ModellingKind -> Property
            refs.Add(CreateReference("HasProperty", createProperty("ModelingKind", GetAASModelingKindDataType(element.kind.kind).ToString(), "AASModelingKindDataType", "3:http://admin-shell.io/aas/2/0/SubmodelElement/kind")));

            //Qualifier -> QualifierType
            foreach(Qualifier qual in element.qualifiers)
            {
                refs.Add(CreateReference("HasComponent", createQualifier(qual, "3:http://admin-shell.io/aas/2/0/ SubmodelElement/qualifiers")));
            }

            //DataSpecification
            refs.Add(createDataSpecification("DataSpecification", element.hasDataSpecification, "3:http://admin-shell.io/aas/2/0/SubmodelElement/dataSpecifications"));

            //SubmodelElement-specific Data
            string type = element.GetElementName();
            switch (type)
            {

                case "SubmodelElementCollection":
                    AdminShellV20.SubmodelElementCollection coll = (AdminShellV20.SubmodelElementCollection)element;
                    refs.Add(CreateHasTypeDefinition("1:AASSubmodelElementCollectionType"));
                    createSubmodelCollection(coll, refs);
                    break;

                case "Property":
                    AdminShellV20.Property prop = (AdminShellV20.Property)element;
                    refs.Add(CreateHasTypeDefinition("1:AASPropertyType"));
                    createPropertyElement(prop, refs);
                    break;

                case "Operation":
                    AdminShellV20.Operation op = (AdminShellV20.Operation)element;
                    refs.Add(CreateHasTypeDefinition("1:AASOperationType"));
                    createOperation(op, refs);
                    break;

                case "Blob":
                    AdminShellV20.Blob blob = (AdminShellV20.Blob)element;
                    refs.Add(CreateHasTypeDefinition("1:AASBlobType"));
                    createBlob(blob, refs);
                    break;

                case "File":
                    AdminShellV20.File file = (AdminShellV20.File)element;
                    refs.Add(CreateHasTypeDefinition("1:AASFileType"));
                    createFile(file, refs);
                    break;

                case "RelationshipElement":
                    AdminShellV20.RelationshipElement rela = (AdminShellV20.RelationshipElement)element;
                    refs.Add(CreateHasTypeDefinition("1:AASRelationshipElementType"));
                    createRelationshipElement(rela, refs);
                    break;

                case "ReferenceElement":
                    AdminShellV20.ReferenceElement refe = (AdminShellV20.ReferenceElement)element;
                    refs.Add(CreateHasTypeDefinition("1:AASReferenceType"));
                    createReferenceElement(refe, refs);
                    break;

                case "MultiLanguageProperty":
                    MultiLanguageProperty multi = (MultiLanguageProperty)element;
                    refs.Add(CreateHasTypeDefinition("1:AASMultiLanguagePropertyType"));
                    createMultiLanguageProperty(multi, refs);
                    break;

                case "Range":
                    Range range = (Range)element;
                    refs.Add(CreateHasTypeDefinition("1:AASRangeType"));
                    createRange(range, refs);
                    break;

                case "Entity":
                    Entity ent = (Entity)element;
                    refs.Add(CreateHasTypeDefinition("1:AASEntityType"));
                    createEntity(ent, refs);
                    break;
            }

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;

        }
     
        private static void createSubmodelCollection(SubmodelElementCollection collection, List<Reference> refs)
        {
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/SubmodelElementCollection")));

            //AllowDuplicates -> Property
            refs.Add(CreateReference("HasProperty", createProperty("AllowDuplicates", collection.allowDuplicates.ToString().ToLower(), "Boolean", "3:http://admin-shell.io/aas/2/0/ SubmodelElementCollection/allowDuplicates")));

            //Submodels
            foreach(SubmodelElementWrapper element in collection.value)
            {
                refs.Add(CreateReference("HasComponent", createSubmodelElement(element.submodelElement, "3:http://admin-shell.io/aas/2/0/SubmodelElementCollection/values")));
            }
        }

        private static void createMultiLanguageProperty(MultiLanguageProperty prop, List<Reference> refs)
        {
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/MultiLanguageProperty")));

            //ValueId -> Reference
            refs.Add(CreateReference("HasComponent", createAASReference("ValueId", prop.valueId, "3:http://admin-shell.io/aas/2/0/MultiLanguageProperty/valueId")));

            //Value -> LocalizedText
            refs.Add(CreateReference("HasProperty", CreateLocalizedTextProperty("Value", prop.value.langString, "3:http://admin-shell.io/aas/2/0/ MultiLanguageProperty/value")));
        }

        private static void createPropertyElement(Property prop, List<Reference> refs)
        {
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/Property")));

            //Value -> Property
            //BaseDataType zu String geändert!!
            refs.Add(CreateReference("HasProperty", createProperty("Value", prop.value, "String", "3:http://admin-shell.io/aas/2/0/Property/value")));

            //ValueType -> Property
            refs.Add(CreateReference("HasProperty", createProperty("ValueType", GetAASValueTypeDataType(prop.valueType).ToString(), "AASValueTypeDataType", "3:http://admin-shell.io/aas/2/0/Property/valueType")));

            //ValueId -> Reference
            refs.Add(CreateReference("HasComponent", createAASReference("ValueId", prop.valueId, "3:http://admin-shell.io/aas/2/0/Property/valueId")));
        }

        private static void createCapability(Capability cap, List<Reference> refs)
        {
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/Property")));
        }

        private static void createOperation(Operation op, List<Reference> refs)
        {
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/Operation")));

            //Operation -> Operation
        }

        private static void createBlob(Blob blob, List<Reference> refs)
        {
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/Blob")));

            //File -> File
            refs.Add(CreateReference("HasComponent", createFileFile("File", "")));
        }

        private static void createFile(File file, List<Reference> refs)
        {
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/File")));

            //FileReference -> Property
            refs.Add(CreateReference("HasProperty", createProperty("FileReference", file.value, "String", "3:http://admin-shell.io/aas/2/0/File/value")));

            //MimeType -> Property
            refs.Add(CreateReference("HasProperty", createProperty("MimeType", file.mimeType, "AASMimeDataType", "3:http://admin-shell.io/aas/2/0/File/mimeType")));

            //File -> File
            refs.Add(CreateReference("HasComponent", createFileFile("File", "")));
        }

        private static string createFileFile(string name, string addRefs)
        {
            var obj = createObject(name);
            var refs = createReferenceList("FileType", addRefs);

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        private static void createRelationshipElement(RelationshipElement rel, List<Reference> refs)
        {
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/RelationshipElement")));

            //First -> Reference
            refs.Add(CreateReference("HasComponent", createAASReference("First", rel.first, "3:http://admin-shell.io/aas/2/0/RelationshipElement/first")));

            //Second -> Reference
            refs.Add(CreateReference("HasComponent", createAASReference("Second", rel.second, "3:http://admin-shell.io/aas/2/0/RelationshipElement/second")));
        }

        private static void createAnnotatedRelationshipElement(AnnotatedRelationshipElement rel, List<Reference> refs)
        {
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/AnnotatedRelationshipElement")));

            //DataElement -> SubmodelElement
            
        }

        private static void createReferenceElement(ReferenceElement refel, List<Reference> refs)
        {
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/ReferenceElement")));

            //Value -> Reference
            refs.Add(CreateReference("HasComponent", createAASReference("Value", refel.value, "3:http://admin-shell.io/aas/2/0/ReferenceElement/value")));
        }

            //Insert Event here

        private static void createEntity(Entity ent, List<Reference> refs)
        {
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/Entity")));

            //SubmodelElement -> Element
            foreach(SubmodelElementWrapper element in ent.statements)
            {
                refs.Add(CreateReference("HasComponent", createSubmodelElement(element.submodelElement, "3:http://admin-shell.io/aas/2/0/Entity/statements")));
            }

            //EntityType -> Property
            refs.Add(CreateReference("HasProperty", createProperty("EntityType", GetAASEntityTypeDataType(ent.entityType).ToString(), "AASEntityTypeDataType", "3:http://admin-shell.io/aas/2/0/Entity/entityType")));

            //Asset -> Reference
            refs.Add(CreateReference("HasComponent", createAASReference("Asset", ent.assetRef, "3:http://admin-shell.io/aas/2/0/Entity/asset")));
        }

        private static void createRange(Range range, List<Reference> refs)
        {
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/Range")));

            //ValueType -> Property
            refs.Add(CreateReference("HasProperty", createProperty("ValueType", GetAASValueTypeDataType(range.valueType).ToString(), "AASValueTypeDataType", "3:http://admin-shell.io/aas/2/0/Range/valueType")));

            //Min -> Propery
            refs.Add(CreateReference("HasProperty", createProperty("Min", range.min, "BaseDataType", "3:http://admin-shell.io/aas/2/0/Range/min")));

            //Max -> Property
            refs.Add(CreateReference("HasProperty", createProperty("Max", range.max, "BaseDataType", "3:http://admin-shell.io/aas/2/0/Range/max")));
        }

        //IAASIdentifiableType Stuff 

        private static string createIAASIdentifiable(AdministrationShell shell)
        {
            var obj = createObject("IAASIdentifiable");
            var refs = createReferenceList("1:IAASIdentifiableType", null);
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/AdministrativeInformation")));

            //Identification -> AASIdentifierType
            if(shell.identification != null) refs.Add(CreateReference("HasComponent", createAASIdentifier("Identification", shell.identification, "3:http://admin-shell.io/aas/2/0/Identifiable/identification")));
            //Administration -> AASAdministrativeInformationType
            if(shell.administration != null) refs.Add(CreateReference("HasComponent", createAASAdministrativeInformation("Administration", shell.administration, "3:http://admin-shell.io/aas/2/0/Identifiable/administration")));

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        private static string createIAASIdentifiable(Administration admin, Identification iden)
        {
            var obj = createObject("IAASIdentifiable");
            var refs = createReferenceList("1:IAASIdentifiableType", null);
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/AdministrativeInformation")));

            //Identification -> AASIdentifierType
            if(iden != null) refs.Add(CreateReference("HasComponent", createAASIdentifier("Identification", iden, "3:http://admin-shell.io/aas/2/0/Identifiable/identification")));
            //Administration -> AASAdministrativeInformationType
            if(admin != null) refs.Add(CreateReference("HasComponent", createAASAdministrativeInformation("Administration", admin, "3:http://admin-shell.io/aas/2/0/Identifiable/administration")));

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        private static string createAASIdentifier(string name, Identification iden, string addRefs)
        {
            var obj = createObject(name);
            var refs = createReferenceList("1:AASIdentifierType", addRefs);

            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/Identifier")));
            refs.Add(CreateReference("HasProperty", createProperty("Id", iden.id, "String", "3:http://admin-shell.io/aas/2/0/Identifier/id")));
            refs.Add(CreateReference("HasProperty", createProperty("IdType", GetAASIdentifierTypeDataType(iden.idType).ToString(), "AASIdentifierTypeDataType", "3:http://admin-shell.io/aas/2/0/Identifier/idType")));

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        private static string createAASAdministrativeInformation(string name, Administration admin, string addRefs)
        {
            var obj = createObject(name);
            var refs = createReferenceList("1:AASAdministrativeInformationType", addRefs);

            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/AdministrativeInformation")));
            refs.Add(CreateReference("HasProperty", createProperty("Version", admin.version, "String", "3:http://admin-shell.io/aas/2/0/AdministrativeInformation/version")));
            refs.Add(CreateReference("HasProperty", createProperty("Revision", admin.revision, "String", "3:http://admin-shell.io/aas/2/0/AdministrativeInformation/revision")));

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        //Asset Creation

        private static string createAASAsset(string name, Asset asset, string addRefs)
        {
            var obj = createObject("Asset:"+name);
            var refs = createReferenceList("1:AASAssetType", addRefs);
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/Asset")));

            //IAASIdentifiable
            refs.Add(CreateReference("HasInterface", createIAASIdentifiable(asset.administration, asset.identification)));

            //AssetKind
            refs.Add(CreateReference("HasProperty", createProperty("AssetKind", GetAASAssetKindDataType(asset.kind.kind).ToString(), "AASAssetKindDataType", "3:http://admin-shell.io/aas/2/0/Asset/assetKind")));

            //AssetIdentificationModel
            if(asset.assetIdentificationModelRef != null) refs.Add(CreateReference("HasComponent", CreateKeyProperty(asset.assetIdentificationModelRef.Keys, "AssetIdentificationModel", "3:http://admin-shell.io/aas/2/0/Asset/assetIdentificationModel")));

            //BillOfData
            if(asset.billOfMaterialRef != null) refs.Add(CreateReference("HasProperty", CreateKeyProperty(asset.billOfMaterialRef.Keys, "BillOfMaterial", "3:http://admin-shell.io/aas/2/0/Asset/billOfMaterial")));

            //DataSpecification
            refs.Add(createDataSpecification("DataSpecification", asset.hasDataSpecification, "3:http://admin-shell.io/aas/2/0/Asset/dataSpecifications"));

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        //View Creation

        private static string createAASView(View view, string addRefs)
        {
            var obj = createObject("View:" + view.idShort);
            var refs = createReferenceList("1:AASViewType", addRefs);
            refs.Add(CreateReference("HasDictionaryEntry", createUriDictionaryEntry("3:http://admin-shell.io/aas/2/0/View")));

            //Referable -> AASReferenceType
            foreach(ContainedElementRef _ref in view.containedElements.reference)
            {
                refs.Add(CreateReference("HasComponent", createAASReference("ContainedElement", _ref, "3:http://admin-shell.io/aas/2/0/View/containedElements")));
            }

            //DataSpecification -> AASReferenceType
            refs.Add(createDataSpecification("DataSpecification", view.hasDataSpecification, "3:http://admin-shell.io/aas/2/0/View/dataSpecifications"));

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        //UANodeSet Array Stuff

        private static XmlElement CreateLocalizedTextValue(List<LangStr> strs)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("uax", "ListOfLocalizedText", "http://opcfoundation.org/UA/2008/02/Types.xsd");

            foreach (LangStr str in strs)
            {
                XmlNode node = doc.CreateNode(XmlNodeType.Element, "uax", "LocalizedText", "http://opcfoundation.org/UA/2008/02/Types.xsd");
                XmlNode locale = doc.CreateNode(XmlNodeType.Element, "uax", "Locale", "http://opcfoundation.org/UA/2008/02/Types.xsd");
                locale.InnerText = str.lang;
                XmlNode text = doc.CreateNode(XmlNodeType.Element, "uax", "Text", "http://opcfoundation.org/UA/2008/02/Types.xsd");
                text.InnerText = str.str;

                node.AppendChild(locale);
                node.AppendChild(text);
                element.AppendChild(node);
            }

            return element;
        }

        private static string CreateLocalizedTextProperty(string name, List<LangStr> list, string dic)
        {
            UAVariable prop = new UAVariable();
            prop.NodeId = "ns="+currentNamespace+";i=" + masterID.ToString();
            prop.BrowseName = currentNamespace + ":" + name;
            prop.DataType = "LocalizedText";
            masterID++;

            prop.Value = CreateLocalizedTextValue(list);

            prop.References = new Reference[2];
            prop.References[0] = CreateHasTypeDefinition("PropertyType");
            prop.References[1] = CreateHasDictionaryEntry(dic);

            prop.ValueRank = 1;
            prop.ArrayDimensions = list.Count.ToString();

            root.Add((UANode)prop);
            return prop.NodeId;
        }



        private static XmlNode CreateKeyValue(Key key, XmlDocument doc)
        {
            XmlNode root = doc.CreateNode(XmlNodeType.Element, "uax", "ExtensionObject", "http://opcfoundation.org/UA/2008/02/Types.xsd");

            XmlNode typeId = doc.CreateNode(XmlNodeType.Element, "uax", "TypeId", "http://opcfoundation.org/UA/2008/02/Types.xsd");

            XmlNode identifier = doc.CreateNode(XmlNodeType.Element, "uax", "Identifier", "http://opcfoundation.org/UA/2008/02/Types.xsd");
            identifier.InnerText = "ns=1;i=5039";
            typeId.AppendChild(identifier);
            root.AppendChild(typeId);

            XmlNode body = doc.CreateNode(XmlNodeType.Element, "uax", "Body", "http://opcfoundation.org/UA/2008/02/Types.xsd");
            XmlNode xmlKey = doc.CreateNode(XmlNodeType.Element, "", "AASKeyDataType", typesNS);

            XmlNode type = doc.CreateNode(XmlNodeType.Element, "", "Type", "");
            type.InnerText = GetAASKeyElementsDataType(key.type).ToString();
            xmlKey.AppendChild(type);

            XmlNode local = doc.CreateNode(XmlNodeType.Element, "", "Local", "");
            local.InnerText = key.local.ToString();
            xmlKey.AppendChild(local);

            XmlNode value = doc.CreateNode(XmlNodeType.Element, "", "Value", "");
            value.InnerText = key.value.ToString();
            xmlKey.AppendChild(value);

            XmlNode idtype = doc.CreateNode(XmlNodeType.Element, "", "IdType", "");
            idtype.InnerText = GetAASKeyTypeDataType(key.idType).ToString();
            xmlKey.AppendChild(idtype);

            body.AppendChild(xmlKey);
            root.AppendChild(body);

            return root;
        }

        private static XmlElement CreateKeysValue(List<Key> keys)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("uax", "ListOfExtensionObject", "http://opcfoundation.org/UA/2008/02/Types.xsd");

            foreach (Key key in keys)
            {
                element.AppendChild(CreateKeyValue(key, doc));
            }

            return element;
        }

        private static string CreateKeyProperty(List<Key> keys, string name, string dic)
        {
            UAVariable ident = new UAVariable();
            ident.NodeId = "ns="+currentNamespace+";i=" + masterID.ToString();
            ident.BrowseName = currentNamespace + ":" + name;
            ident.DataType = "AASKeyDataType";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("PropertyType"));
            refs.Add(CreateHasDictionaryEntry(dic));

            ident.Value = CreateKeysValue(keys);

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateKeyProperty(Key key, string name, string dic)
        {
            UAVariable ident = new UAVariable();
            ident.NodeId = "ns="+currentNamespace+";i=" + masterID.ToString();
            ident.BrowseName = currentNamespace + ":" + name;
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASKeyTypeDataType"));
            refs.Add(CreateHasDictionaryEntry(dic));

            ident.Value = (XmlElement)CreateKeyValue(key, new XmlDocument());

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static Reference CreateHasDictionaryEntry(string name)
        {
            UAObject ident = new UAObject();
            ident.NodeId = "ns="+currentNamespace+";i=" + masterID.ToString();
            ident.BrowseName = currentNamespace + ":" + name;
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("UriDictionaryEntryType"));

            ident.References = refs.ToArray();
            root.Add((UANode)ident);

            return CreateReference("HasDictionaryEntry", ident.NodeId);

        }

        //i4AAS Enumerations

        private static int GetAASKeyTypeDataType(string str)
        {
            int i = -1;
            switch (str)
            {
                case "IdShort":
                    i = 0;
                    break;
                case "FragmentId":
                    i = 1;
                    break;
                case "Custom":
                    i = 2;
                    break;
                case "IRDI":
                    i = 3;
                    break;
                case "IRI":
                    i = 4;
                    break;
            }
            return i;
        }

        private static int GetAASKeyElementsDataType(string str)
        {
            int i = -1;
            switch (str)
            {
                case "AccessPermissionRule":
                    i = 0;
                    break;
                case "AnnotatedRelationshipElement":
                    i = 1;
                    break;
                case "Asset":
                    i = 2;
                    break;
                case "AssetAdministrationShell":
                    i = 3;
                    break;
                case "Blob":
                    i = 4;
                    break;
                case "Capability":
                    i = 5;
                    break;
                case "ConceptDescription":
                    i = 6;
                    break;
                case "ConceptDictionary":
                    i = 7;
                    break;
                case "DataElement":
                    i = 8;
                    break;
                case "Entity":
                    i = 9;
                    break;
                case "Event":
                    i = 10;
                    break;
                case "File":
                    i = 11;
                    break;
                case "FragmentReference":
                    i = 12;
                    break;
                case "GlobalReference":
                    i = 13;
                    break;
                case "MultiLanguageProperty":
                    i = 14;
                    break;
                case "Property":
                    i = 15;
                    break;
                case "Operation":
                    i = 16;
                    break;
                case "Range":
                    i = 17;
                    break;
                case "ReferenceElement":
                    i = 18;
                    break;
                case "RelationshipElement":
                    i = 19;
                    break;
                case "Submodel":
                    i = 20;
                    break;
                case "SubmodelElement":
                    i = 21;
                    break;
                case "SubmodelElementCollection":
                    i = 22;
                    break;
                case "View":
                    i = 23;
                    break;
            }
            return i;
        }

        private static int GetAASIdentifierTypeDataType(string str)
        {
            int i = -1;
            switch (str)
            {
                case "IRDI":
                    i = 0;
                    break;
                case "IRI":
                    i = 1;
                    break;
                case "Custom":
                    i = 2;
                    break;
            }
            return i;
        }

        private static int GetAASAssetKindDataType(string str)
        {
            int i = -1;
            switch (str)
            {
                case "Type":
                    i = 0;
                    break;
                case "Instance":
                    i = 1;
                    break;
            }
            return i;
        }

        private static int GetAASModelingKindDataType(string str)
        {
            int i = -1;
            switch (str)
            {
                case "Template":
                    i = 0;
                    break;
                case "Instance":
                    i = 1;
                    break;
            }
            return i;
        }

        private static int GetAASValueTypeDataType(string str)
        {
            int i = -1;
            switch (str)
            {
                case "Boolean":
                    i = 0;
                    break;
                case "SByte":
                    i = 1;
                    break;
                case "Byte":
                    i = 2;
                    break;
                case "Int16":
                    i = 3;
                    break;
                case "UInt16":
                    i = 4;
                    break;
                case "Int32":
                    i = 5;
                    break;
                case "UInt32":
                    i = 6;
                    break;
                case "Int64":
                    i = 7;
                    break;
                case "UInt64":
                    i = 8;
                    break;
                case "Float":
                    i = 9;
                    break;
                case "Double":
                    i = 10;
                    break;
                case "String":
                    i = 11;
                    break;
                case "DateTime":
                    i = 12;
                    break;
                case "ByteString":
                    i = 14;
                    break;
                case "LocalizedText":
                    i = 20;
                    break;
                case "UtcTime":
                    i = 37;
                    break;
            }
            return i;
        }

        private static int GetAASEntityTypeDataType(string str)
        {
            int i = -1;
            switch (str)
            {
                case "CoManagedEntity":
                    i = 1;
                    break;
                case "SelfManagedEntity":
                    i = 2;
                    break;
            }
            return i;
        }

        private static int GetAASCategoryDataType(string str)
        {
            if (str != null) str = str.ToUpper();
            int i = -1;
            switch (str)
            {
                case "CONSTANT":
                    i = 0;
                    break;
                case "PARAMETER":
                    i = 1;
                    break;
                case "VARIABLE":
                    i = 2;
                    break;
                case "RELATIONSHIP":
                    i = 3;
                    break;
            }
            return i + 1;
        }

    }
}
