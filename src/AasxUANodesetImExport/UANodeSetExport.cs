using AdminShellNS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static AdminShellNS.AdminShellV20;

namespace AasxUANodesetImExport
{
    public class UANodeSetExport
    {
        //consists of every single node that will be created
        public static List<UANode> root = new List<UANode>();

        private static int masterID = 1500;

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
                    MessageBoxResult result = MessageBox.Show("Error in XML formatting\n\n" + ex.ToString(),
                                          "Error",
                                          MessageBoxButton.OK);
                    throw ex;
                }

            }
            return InformationModel;
        }

        //Annotations
        //Almost every Method is build the same way:
        //1. Create UANode object and set its parameters
        //   Create a List of References and add a Reference to the Type (HasTypeDefinition)
        //   Increment masterId++, so the InformationModel will stay consistent and so there be no duplicate IDs
        //2. Set specific parameters of the mapped Object with properties
        //   createReference only needs the ReferenceType and a nodeId, that is why every method only returns a string
        //3. Create an array from the List
        //   Add the created Node to the root List

        public static string CreateAAS(string name, AdminShellV20.AdministrationShellEnv env)
        {

            UAObject sub = new UAObject();
            sub.NodeId = "ns=1;i=" + masterID.ToString();
            sub.BrowseName = "1:AASAssetAdministrationShell";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(new Reference
            {
                Value = "i=58",
                ReferenceType = "HasTypeDefinition"
            });
            refs.Add(new Reference
            {
                IsForward = false,
                Value = "i=85",
                ReferenceType = "Organizes"
            });

            //add ConceptDictionary
            refs.Add(CreateReference("HasComponent", CreateConceptDictionaryFolder(env.ConceptDescriptions)));

            //add Assets
            foreach (AdminShellV20.Asset asset in env.Assets)
            {
                refs.Add(CreateReference("HasComponent", CreateAASAsset(asset)));
            }

            //add Submodels
            foreach (AdminShellV20.Submodel submodel in env.Submodels)
            {
                string id = CreateAASSubmodel(submodel);
                refs.Add(CreateReference("HasComponent", id));
            }

            //map AAS Information
            foreach (AdminShellV20.AdministrationShell shell in env.AdministrationShells)
            {
                if (shell.views != null)
                {
                    foreach (AdminShellV20.View view in shell.views.views)
                    {
                        refs.Add(CreateReference("HasComponent", CreateView(view)));
                    }
                }

                if (shell.derivedFrom != null)
                {
                    refs.Add(CreateReference("HasComponent", CreateDerivedFrom(shell.derivedFrom.Keys)));
                }

                if (shell.idShort != null)
                {
                    refs.Add(CreateReference("HasProperty", CreateProperty(shell.idShort, "PropertyType", "idShort", "String")));
                }

                if (shell.identification != null)
                {
                    refs.Add(CreateReference("HasComponent", CreateIdentifiableIdentification(shell.identification.id, shell.identification.idType)));
                }

                if (shell.hasDataSpecification != null)
                {
                    refs.Add(CreateReference("HasComponent", CreateHasDataSpecification(shell.hasDataSpecification)));
                }

                if (shell.assetRef != null)
                {
                    refs.Add(CreateReference("HasComponent", CreateAssetRef(shell.assetRef)));
                }
            }

            sub.References = refs.ToArray();
            root.Add((UANode)sub);
            return sub.NodeId;
        }


        private static string CreateAASSubmodel(AdminShellV20.Submodel submodel)
        {
            UAObject sub = new UAObject();
            sub.NodeId = "ns=1;i=" + masterID.ToString();
            sub.BrowseName = "1:" + submodel.idShort;
            masterID++;
            List<Reference> refs = new List<Reference>();
            if (submodel.kind != null)
                refs.Add(CreateReference("HasProperty", CreateProperty(submodel.kind.kind, "1:AASModelingKindDataType", "ModellingKind", "String")));
            refs.Add(CreateHasTypeDefinition("1:AASSubmodelType"));
            refs.Add(CreateReference("HasProperty", CreateProperty(submodel.category, "PropertyType", "Category", "String")));
            refs.Add(CreateReference("HasComponent", CreateSemanticId(submodel.semanticId)));

            //set Identifiable
            if (submodel.administration == null)
            {
                refs.Add(CreateReference("HasInterface", CreateIdentifiable(submodel.identification.id, submodel.identification.idType, null, null)));
            }
            else if (submodel.identification == null)
            {
                refs.Add(CreateReference("HasInterface", CreateIdentifiable(null, null, submodel.administration.version, submodel.administration.revision)));

            }
            else
            {
                refs.Add(CreateReference("HasInterface", CreateIdentifiable(submodel.identification.id, submodel.identification.idType, submodel.administration.version, submodel.administration.revision)));
            }

            //set Qualifier if it exists
            if (submodel.qualifiers != null)
            {
                foreach (AdminShellV20.Qualifier qualifier in submodel.qualifiers)
                {
                    refs.Add(CreateReference("HasComponent", CreateAASQualifier(qualifier.type, qualifier.value, qualifier.valueId)));
                }
            }

            //add Elements
            foreach (AdminShellV20.SubmodelElementWrapper element in submodel.submodelElements)
            {
                string id = CreateSubmodelElement(element.submodelElement);
                refs.Add(CreateReference("HasComponent", id));
            }

            sub.References = refs.ToArray();
            root.Add((UANode)sub);
            return sub.NodeId;
        }

        private static string CreateProperty(string value, string type, string BrowseName, string datatype)
        {
            //Creates a Property with a single Value

            UAVariable prop = new UAVariable();
            prop.NodeId = "ns=1;i=" + masterID.ToString();
            prop.BrowseName = "1:" + BrowseName;
            prop.DataType = datatype;
            masterID++;
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

            //Remove ':', because it's not allowed in XML
            if (datatype.Contains(':'))
            {
                var strings = datatype.Split(':');
                datatype = strings[1];
            }

            //Create XMLElement to store Value in
            System.Xml.XmlElement element = doc.CreateElement("uax", datatype, "http://opcfoundation.org/UA/2008/02/Types.xsd");
            element.InnerText = value;
            prop.Value = element;

            prop.References = new Reference[1];

            prop.References[0] = CreateHasTypeDefinition(type);
            root.Add((UANode)prop);

            return prop.NodeId;
        }

        private static Reference CreateReference(string type, string value)
        {
            if (value != null)
            {
                //check if Type is "Base"Type (not seperately created)
                if (!value.Contains("ns=0;i="))
                {
                    value = value.Replace("ns=1;i=", "");
                    Reference _ref = new Reference
                    {
                        Value = "ns=1;i=" + value,
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

        private static string CreateAASQualifier(string type, string value, AdminShellV20.Reference valueId)
        {
            UAObject qual = new UAObject();
            qual.NodeId = "ns=1;i=" + masterID.ToString();
            qual.BrowseName = "1:Qualifier";
            masterID++;
            List<Reference> refs = new List<Reference>();

            //map Qualifier Data
            refs.Add(CreateHasTypeDefinition("1:AASQualifierType"));
            refs.Add(CreateReference("HasProperty", CreateProperty(type, "1:AASPropertyType", "QualifierType", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(value, "1:AASPropertyType", "QualifierValue", "String")));
            if (valueId != null)
            {
                foreach (AdminShellV20.Key key in valueId.Keys)
                {
                    refs.Add(CreateReference("HasComponent", CreateKey(key.idType, key.local.ToString(), key.type, key.value)));
                }
            }

            qual.References = refs.ToArray();
            root.Add((UANode)qual);
            return qual.NodeId;
        }

        private static string CreateSubmodelElement(AdminShellV20.SubmodelElement element)
        {
            UAObject elem = new UAObject();
            elem.BrowseName = "1:" + element.idShort;
            elem.NodeId = "ns=1;i=" + masterID.ToString();
            masterID++;

            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASSubmodelElementType"));

            refs.Add(CreateReference("HasProperty", CreateProperty(element.category, "PropertyType", "Category", "String")));
            refs.Add(CreateReference("HasComponent", CreateSemanticId(element.semanticId)));

            //add Referable &
            //add Description if it exists
            if (element.description == null)
            {
                refs.Add(CreateReference("HasInterface", CreateReferable(element.category, null)));
            }
            else
            {
                refs.Add(CreateReference("HasInterface", CreateReferable(element.category, element.description.langString)));
            }

            //add Kind if it exists
            if (element.kind != null)
                refs.Add(CreateReference("HasProperty", CreateProperty(element.kind.kind, "1:AASModelingKindDataType", "ModellingKind", "String")));


            //add Qualifier if it exists
            if (element.qualifiers != null)
            {
                foreach (AdminShellV20.Qualifier qualifier in element.qualifiers)
                {
                    refs.Add(CreateReference("HasComponent", CreateAASQualifier(qualifier.type, qualifier.value, qualifier.valueId)));
                }
            }

            //Set Elementspecific Data
            string type = element.GetElementName();
            switch (type)
            {

                case "SubmodelElementCollection":
                    AdminShellV20.SubmodelElementCollection coll = (AdminShellV20.SubmodelElementCollection)element;
                    refs.Add(CreateReference("HasComponent", CreateSubmodelElementCollection(coll)));
                    break;

                case "Property":
                    AdminShellV20.Property prop = (AdminShellV20.Property)element;
                    refs.Add(CreateReference("HasComponent", CreatePropertyType(prop.value, prop.valueId)));
                    break;

                case "Operation":
                    AdminShellV20.Operation op = (AdminShellV20.Operation)element;
                    refs.Add(CreateReference("HasComponent", CreateAASOperation(op.inputVariable, op.outputVariable)));
                    break;

                case "Blob":
                    AdminShellV20.Blob blob = (AdminShellV20.Blob)element;
                    refs.Add(CreateReference("HasComponent", CreateAASBlob(blob.value, blob.mimeType)));
                    break;

                case "File":
                    AdminShellV20.File file = (AdminShellV20.File)element;
                    refs.Add(CreateReference("HasComponent", CreateAASFile(file.value, file.mimeType)));
                    break;

                case "RelationshipElement":
                    AdminShellV20.RelationshipElement rela = (AdminShellV20.RelationshipElement)element;

                    refs.Add(CreateReference("HasComponent", CreateAASRelationshipElement(rela.first.ToString(), rela.second.ToString())));
                    break;

                case "ReferenceElement":
                    AdminShellV20.ReferenceElement refe = (AdminShellV20.ReferenceElement)element;
                    refs.Add(CreateReference("HasComponent", CreateReferenceElement(refe.value.ToString())));
                    break;
            }

            elem.References = refs.ToArray();
            root.Add((UANode)elem);
            return elem.NodeId;
        }


        //SubmodelElementData Creation
        //always the same pattern -> mapping Data
        //

        private static string CreateSubmodelElementCollection(AdminShellV20.SubmodelElementCollection collection)
        {
            UAObject coll = new UAObject();
            coll.NodeId = "ns=1;i=" + masterID.ToString();
            coll.BrowseName = "1:Collection";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASSubmodelElementCollectionType"));
            foreach (AdminShellV20.SubmodelElementWrapper elem in collection.value)
            {
                refs.Add(CreateReference("HasComponent", CreateSubmodelElement(elem.submodelElement)));
            }
            refs.Add(CreateReference("HasProperty", CreateProperty(collection.allowDuplicates.ToString().ToLower(), "BaseVariableType", "AllowDublication", "Boolean")));
            coll.References = refs.ToArray();
            root.Add((UANode)coll);
            return coll.NodeId;
        }

        private static string CreatePropertyType(string value, AdminShellV20.Reference valueId)
        {
            UAObject prop = new UAObject();
            prop.NodeId = "ns=1;i=" + masterID.ToString();
            prop.BrowseName = "1:Property";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASPropertyType"));
            refs.Add(CreateReference("HasProperty", CreateProperty(value, "1:AASPropertyType", "Value", "String"))); //DataType: Man weiss es nicht
            if (valueId != null) refs.Add(CreateReference("HasProperty", CreateProperty(valueId.ToString(), "1:AASReferenceType", "ValueId", "String")));
            prop.References = refs.ToArray();
            root.Add((UANode)prop);
            return prop.NodeId;
        }

        private static string CreateMultiLanguageProperty(string value)
        {
            UAObject prop = new UAObject();
            prop.NodeId = "ns=1;i=" + masterID.ToString();
            prop.BrowseName = "1:MultiLanguageProperty";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASMultiLanguagePropertyType"));
            refs.Add(CreateReference("HasProperty", CreateProperty(value, "1:AASPropertyType", "Value", "String"))); //DataType: Man weiss es nicht
            prop.References = refs.ToArray();
            root.Add((UANode)prop);
            return prop.NodeId;
        }

        private static string CreateAASCapability(string value)
        {
            UAObject prop = new UAObject();
            prop.NodeId = "ns=1;i=" + masterID.ToString();
            prop.BrowseName = "1:Capability";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASCapabilityType"));
            refs.Add(CreateReference("HasProperty", CreateProperty(value, "BaseVariableType", "Capability", "String")));
            prop.References = refs.ToArray();
            root.Add((UANode)prop);
            return prop.NodeId;
        }

        private static string CreateAASOperation(List<AdminShellV20.OperationVariable> vin, List<AdminShellV20.OperationVariable> vout)
        {
            UAObject prop = new UAObject();
            prop.NodeId = "ns=1;i=" + masterID.ToString();
            prop.BrowseName = "1:Operation";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASOperationType"));



            prop.References = refs.ToArray();
            root.Add((UANode)prop);
            return prop.NodeId;
        }

        //private static string createOpArgs(string name, List<AdminShellV20.OperationVariable> args)
        //{
        //    UAObject prop = new UAObject();
        //    prop.NodeId = "ns=1;i=" + masterID.ToString();
        //    prop.BrowseName = name;
        //    masterID++;
        //    List<Reference> refs = new List<Reference>();
        //    refs.Add(CreateHasTypeDefinition("BaseObjectType"));

        //    foreach(AdminShellV20.OperationVariable var in args)
        //    {
        //        refs.Add(CreateProperty(var.))
        //    }


        //    prop.References = refs.ToArray();
        //    root.Add((UANode)prop);
        //    return prop.NodeId;
        //}

        private static string CreateAASBlob(string value, string mimeType)
        {
            UAObject prop = new UAObject();
            prop.NodeId = "ns=1;i=" + masterID.ToString();
            prop.BrowseName = "1:Blob";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASBlobType"));
            refs.Add(CreateReference("HasComponent", CreateProperty(value, "FileType", "File", "String"))); //DataType: Man weiss es nicht
            refs.Add(CreateReference("HasComponent", CreateProperty(mimeType, "PropertyType", "MimeType", "String"))); //DataType: Man weiss es nicht
            prop.References = refs.ToArray();
            root.Add((UANode)prop);
            return prop.NodeId;
        }

        private static string CreateAASFile(string value, string mimeType, string file = null)
        {
            UAObject prop = new UAObject();
            prop.NodeId = "ns=1;i=" + masterID.ToString();
            prop.BrowseName = "1:File";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASFileType"));
            refs.Add(CreateReference("HasProperty", CreateProperty(value, "PropertyType", "Value", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(mimeType, "PropertyType", "MimeType", "String")));
            if (file != null)
                refs.Add(CreateReference("HasComponent", CreateProperty(file, "FileType", "File", "String")));
            prop.References = refs.ToArray();
            root.Add((UANode)prop);
            return prop.NodeId;
        }

        private static string CreateAASRelationshipElement(string first, string second)
        {
            UAObject prop = new UAObject();
            prop.NodeId = "ns=1;i=" + masterID.ToString();
            prop.BrowseName = "1:RelationshipElement";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASRelationshipElementType"));
            refs.Add(CreateReference("HasComponent", CreateProperty(first, "1:AASReferenceType", "First", "String")));
            refs.Add(CreateReference("HasComponent", CreateProperty(second, "1:AASReferenceType", "Second", "String")));
            prop.References = refs.ToArray();
            root.Add((UANode)prop);
            return prop.NodeId;
        }

        private static string CreateRangeElement(string type, string min, string max)
        {
            UAObject prop = new UAObject();
            prop.NodeId = "ns=1;i=" + masterID.ToString();
            prop.BrowseName = "1:RangeElement";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASRangeElement"));

            refs.Add(CreateReference("HasProperty", CreateProperty(type, "PropertyType", "ValueType", "1:AASValueTypeDataType")));
            refs.Add(CreateReference("HasProperty", CreateProperty(type, "PropertyType", "Min", "BaseDataType")));
            refs.Add(CreateReference("HasProperty", CreateProperty(type, "PropertyType", "Max", "BaseDataType")));

            prop.References = refs.ToArray();
            root.Add((UANode)prop);
            return prop.NodeId;
        }

        private static string CreateReferenceElement(string value)
        {
            UAObject prop = new UAObject();
            prop.NodeId = "ns=1;i=" + masterID.ToString();
            prop.BrowseName = "1:ReferenceElement";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASReferenceElementType"));
            refs.Add(CreateReference("HasComponent", CreateProperty(value, "1:AASReferenceType", "Value", "String")));
            prop.References = refs.ToArray();
            root.Add((UANode)prop);
            return prop.NodeId;
        }

        //public static string CreateAnnotatedRelationshipElement(AdminShellV20.RelationshipElement value)
        //{
        //    UAObject prop = new UAObject();
        //    prop.NodeId = "ns=1;i=" + masterID.ToString();
        //    prop.BrowseName = "1:AnnoatedRelationshipElement";
        //    masterID++;
        //    List<Reference> refs = new List<Reference>();
        //    refs.Add(CreateHasTypeDefinition("1:AASAnnotatedRelationshipElement"));

        //    value.

        //    prop.References = refs.ToArray();
        //    root.Add((UANode)prop);
        //    return prop.NodeId;
        //}

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

                //case "CodeType":
                //    value = "ns=0;i=17589";
                //    break;

                case "BaseDataVariableType":
                    value = "ns=0;i=63";
                    break;

                case "String":
                    value = "ns=0;i=17589";
                    break;
            }
            return value;
        }

        //Interface Creation

        private static string CreateIdentifiable(string id, string idtype, string version, string revision)
        {
            UAObject ident = new UAObject();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:AASIdentifiable";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:IAASIdentifiableType"));

            //add Identification and Administration
            if (id != null || idtype != null)
                refs.Add(CreateReference("HasProperty", CreateIdentifiableIdentification(id, idtype)));
            if (version != null || revision != null)
                refs.Add(CreateReference("HasProperty", CreateIdentifiableAdministration(version, revision)));

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateIdentifiableIdentification(string id, string idtype)
        {
            UAVariable ident = new UAVariable();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:Identification";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASIdentifierType"));

            refs.Add(CreateReference("HasProperty", CreateProperty(id, "PropertyType", "Id", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(idtype, "1:AASIdentifierTypeDataType", "IdType", "String")));

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateIdentifiableAdministration(string version, string revision)
        {
            UAVariable ident = new UAVariable();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:Administration";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASAdministrativeInformationType"));

            refs.Add(CreateReference("HasProperty", CreateProperty(version, "1:AASPropertyType", "Version", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(revision, "1:AASPropertyType", "Revision", "String")));

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateReferable(string category, List<AdminShellV20.LangStr> langstr)
        {
            UAVariable ident = new UAVariable();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:AASReferable";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:IAASReferableType"));

            if (langstr != null)
            {
                refs.Add(CreateReference("HasProperty", CreateLangStrContainer(langstr, "Description")));
            }
            refs.Add(CreateReference("HasProperty", CreateProperty(category, "PropertyType", "Category", "String")));

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateSemanticId(AdminShellV20.SemanticId sem)
        {
            if (sem == null)
                return null;

            UAVariable ident = new UAVariable();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:AASSemanticId";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASSemanticIdType"));

            foreach (AdminShellV20.Key key in sem.Keys)
            {
                refs.Add(CreateReference("HasComponent", CreateKey(key.idType, key.local.ToString(), key.type, key.value)));
            }

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateKey(string idtype, string local, string type, string value)
        {
            UAVariable ident = new UAVariable();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:AASKey";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASKeyType"));

            refs.Add(CreateReference("HasProperty", CreateProperty(idtype, "1:AASKeyType", "IdType", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(local, "BaseDataVariableType", "Local", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(type, "1:AASPropertyType", "Type", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(value, "BaseDataVariableType", "Value", "String")));

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateLangStrSet(string lang, string str)
        {
            UAVariable ident = new UAVariable();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:AASLangStrSet";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASLangStrSetType"));

            refs.Add(CreateReference("HasProperty", CreateProperty(lang, "PropertyType", "Language", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(str, "PropertyType", "String", "String")));

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateAASAsset(AdminShellV20.Asset asset)
        {
            UAVariable ident = new UAVariable();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:" + asset.idShort;
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASAssetType"));

            if (asset.kind != null)
                refs.Add(CreateReference("HasProperty", CreateProperty(asset.kind.kind, "1:AASModelingKindDataType", "Kind", "String")));

            //check if either administration or identification is null, -> (because if identification were null, you could not access id or idType)
            //then set accordingly
            if (asset.administration == null)
            {
                refs.Add(CreateReference("HasComponent", CreateIdentifiable(asset.identification.id, asset.identification.idType, null, null)));
            }
            else if (asset.identification == null)
            {
                refs.Add(CreateReference("HasComponent", CreateIdentifiable(null, null, asset.administration.version, asset.administration.revision)));
            }
            else
            {
                refs.Add(CreateReference("HasComponent", CreateIdentifiable(asset.identification.id, asset.identification.idType, asset.administration.version, asset.administration.revision)));
            }

            if (asset.description == null)
            {
                refs.Add(CreateReference("HasComponent", CreateReferable(asset.category, null)));
            }
            else
            {
                refs.Add(CreateReference("HasComponent", CreateReferable(asset.category, asset.description.langString)));
            }

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        //ConceptDictionary Creation

        private static string CreateConceptDictionaryFolder(List<AdminShellV20.ConceptDescription> concepts)
        {
            UAObject ident = new UAObject();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:ConceptDictionary";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASConceptDictionaryType"));

            foreach (AdminShellV20.ConceptDescription con in concepts)
            {
                refs.Add(CreateReference("HasComponent", CreateDictionaryEntry(con)));
            }

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateDictionaryEntry(AdminShellV20.ConceptDescription concept)
        {
            UAObject entry = new UAObject();
            entry.NodeId = "ns=1;i=" + masterID.ToString();

            entry.BrowseName = "1:" + concept.ToCaptionInfo().Item2;


            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("DictionaryEntryType"));

            if (concept.identification.idType == "IRI")
            {
                refs.Add(CreateReference("HasComponent", CreateUriConceptDescription(concept)));
            }
            else
            {
                refs.Add(CreateReference("HasComponent", CreateIrdConceptDescription(concept)));
            }

            entry.References = refs.ToArray();
            root.Add((UANode)entry);
            return entry.NodeId;
        }

        private static string CreateUriConceptDescription(AdminShellV20.ConceptDescription concept)
        {
            UAObject ident = new UAObject();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:AASUriConceptDescription";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASUriConceptDescriptionType"));

            refs.Add(CreateReference("HasComponent", CreateDataSpecification(concept)));
            refs.Add(CreateReference("HasProperty", CreateIdentifiable(concept.identification.id, concept.identification.idType, null, null)));

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateIrdConceptDescription(AdminShellV20.ConceptDescription concept)
        {
            UAObject ident = new UAObject();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:AASIrdiConceptDescription";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASUriConceptDescriptionType"));

            refs.Add(CreateReference("HasComponent", CreateDataSpecification(concept)));
            refs.Add(CreateReference("HasProperty", CreateIdentifiable(concept.identification.id, concept.identification.idType, null, null)));

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateDataSpecification(AdminShellV20.ConceptDescription concept)
        {
            UAObject ident = new UAObject();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:DataSpecification";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASDataSpecificationType"));

            refs.Add(CreateReference("HasComponent", CreateDataSpecificationIEC61360(concept)));

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateDataSpecificationIEC61360(AdminShellV20.ConceptDescription concept)
        {
            UAObject ident = new UAObject();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:DataSpecificationIEC61360";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASDataSpecificationIEC61360Type"));

            refs.Add(CreateReference("HasProperty", CreateProperty("DataSpecificationIEC61360", "PropertyType", "DefaultInstanceBrowseName", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty("DataSpecificationIEC61360", "PropertyType", "IdShort", "String")));
            //refs.Add(CreateReference("HasProperty", CreateProperty(concept.GetIEC61360().sourceOfDefinition, "BaseVariableType", "SourceOfDestination", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(concept.category, "PropertyType", "Category", "String")));

            refs.Add(CreateReference("HasProperty", CreateProperty(concept.GetIEC61360().GetHashCode().ToString(), "BaseVariableType", "Code", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(concept.GetIEC61360().dataType, "BaseVariableType", "DataType", "String")));

            refs.Add(CreateReference("HasComponent", CreateLangStrContainer(concept.GetIEC61360().definition.langString, "Definition")));
            refs.Add(CreateReference("HasComponent", CreateLangStrContainer(concept.GetIEC61360().preferredName.langString, "PreferredName")));

            refs.Add(CreateReference("HasProperty", CreateProperty(concept.GetIEC61360().shortName.ToString(), "BaseVariableType", "ShortName", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(concept.GetIEC61360().symbol, "BaseVariableType", "Symbol", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(concept.GetIEC61360().unit, "BaseVariableType", "Unit", "String")));
            //refs.Add(CreateReference("HasComponent", CreateProperty(concept.GetIEC61360().unitId., "1:AASReferenceType", "UnitId", "String")));
            refs.Add(CreateReference("HasProperty", CreateProperty(concept.GetIEC61360().valueFormat, "BaseVariableType", "ValueFormat", "String")));

            //refs.Add(CreateReference("HasProperty", CreateProperty(concept.GetIEC61360().sourceOfDefinition, "1:BaseVariableType", "ShortName", "String")));

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        private static string CreateLangStrContainer(List<AdminShellV20.LangStr> list, string name)
        {
            UAObject ident = new UAObject();
            ident.NodeId = "ns=1;i=" + masterID.ToString();
            ident.BrowseName = "1:" + name;
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("BaseObjectType"));

            foreach (AdminShellV20.LangStr str in list)
            {
                refs.Add(CreateReference("HasProperty", CreateLangStrSet(str.lang, str.str)));
            }

            ident.References = refs.ToArray();
            root.Add((UANode)ident);
            return ident.NodeId;
        }

        //Asset Creation

        private static string CreateView(AdminShellV20.View view)
        {
            UAVariable var = new UAVariable();
            var.NodeId = "ns=1;i=" + masterID.ToString();
            var.BrowseName = "1:" + view.idShort;
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASViewType"));

            if (view.description != null)
            {
                refs.Add(CreateReference("HasComponent", CreateReferable(view.category, view.description.langString)));

            }

            foreach (AdminShellV20.ContainedElementRef con in view.containedElements.reference)
            {
                refs.Add(CreateReference("HasComponent", createContainedElement(con)));
            }


            var.References = refs.ToArray();
            root.Add((UANode)var);
            return var.NodeId;
        }

        private static string CreateDerivedFrom(List<AdminShellV20.Key> keys)
        {
            UAObject obj = new UAObject();
            obj.NodeId = "ns=1;i=" + masterID.ToString();
            obj.BrowseName = "1:DerivedFrom";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASReferenceType"));

            foreach (AdminShellV20.Key key in keys)
            {
                refs.Add(CreateReference("HasComponent", CreateKey(key.idType, key.local.ToString(), key.type, key.value)));
            }

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        private static string CreateHasDataSpecification(AdminShellV20.HasDataSpecification data)
        {
            UAObject obj = new UAObject();
            obj.NodeId = "ns=1;i=" + masterID.ToString();
            obj.BrowseName = "1:DataSpecification";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASReferenceType"));

            foreach (AdminShellV20.Reference _ref in data.reference)
            {
                refs.Add(CreateReference("HasComponent", CreateReference(_ref)));
            }

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        private static string CreateReference(AdminShellV20.Reference _ref)
        {
            UAObject obj = new UAObject();
            obj.NodeId = "ns=1;i=" + masterID.ToString();
            obj.BrowseName = "1:Reference";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASReferenceType"));

            foreach (AdminShellV20.Key key in _ref.Keys)
            {
                refs.Add(CreateReference("HasComponent", CreateKey(key.idType, key.local.ToString(), key.type, key.value)));
            }

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        private static string CreateAssetRef(AdminShellV20.AssetRef ass)
        {
            UAObject obj = new UAObject();
            obj.NodeId = "ns=1;i=" + masterID.ToString();
            obj.BrowseName = "1:AssetRef";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASReferenceType"));

            foreach (AdminShellV20.Key key in ass.Keys)
            {
                refs.Add(CreateReference("HasComponent", CreateKey(key.idType, key.local.ToString(), key.type, key.value)));
            }

            obj.References = refs.ToArray();
            root.Add((UANode)obj);
            return obj.NodeId;
        }

        private static string createContainedElement(AdminShellV20.ContainedElementRef ele)
        {
            UAVariable var = new UAVariable();
            var.NodeId = "ns=1;i=" + masterID.ToString();
            var.BrowseName = "1:ContainedElementRef";
            masterID++;
            List<Reference> refs = new List<Reference>();
            refs.Add(CreateHasTypeDefinition("1:AASReferenceType"));

            foreach (AdminShellV20.Key key in ele.Keys)
            {
                refs.Add(CreateReference("HasComponent", CreateKey(key.idType, key.local.ToString(), key.type, key.value)));
            }

            var.References = refs.ToArray();
            root.Add((UANode)var);
            return var.NodeId;
        }

    }
}
