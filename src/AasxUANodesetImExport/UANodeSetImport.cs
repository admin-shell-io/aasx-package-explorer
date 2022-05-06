﻿/*
Copyright (c) 2018-2020 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2020 Phoenix Contact GmbH & Co. KG <opensource@phoenixcontact.com>
Author: Andreas Orzelski

Copyright (c) 2020 Fraunhofer IOSB-INA Lemgo, 
    eine rechtlich nicht selbständige Einrichtung der Fraunhofer-Gesellschaft
    zur Förderung der angewandten Forschung e.V. <jan.nicolas.weskamp@iosb-ina.fraunhofer.de>
Author: Jan Nicolas Weskamp

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AdminShellNS;
using static AdminShellNS.AdminShellV30;

// TODO (Michael Hoffmeister, 2020-08-01): Fraunhofer IOSB: Check ReSharper settings to be OK

// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantCast
// ReSharper disable RedundantCast
// ReSharper disable PossibleIntendedRethrow
// ReSharper disable SimplifyConditionalTernaryExpression
// ReSharper disable UnusedVariable
// ReSharper disable PossibleNullReferenceException
// ReSharper disable CSharpWarnings::CS0162

namespace AasxUANodesetImExport
{
    public static class UANodeSetImport
    {
        public static AdminShellNS.AdminShellPackageEnv thePackageEnv;
        private static UANodeSet InformationModel;

        //Annotations
        //  Procedure:
        //  1. find submodel node
        //  2. create submodel node as submodel
        //  3. find children of submodel node
        //  4. create children of submodel node -> a (SubmodelElement)
        //      4.1. find children of a
        //      4.2. create children of a -> b (SubmodelElement)
        //          4.2.1. find children of b
        //          ...
        //      4.3. set a as SubmodelElement of submodel
        //  5. add Submodel
        //
        //
        //  most Methods are built the same way:
        //  1. iterate through all References of the node
        //  2. check that the ReferenceType is not HasTypeDefinition
        //  3. check for the Type or BrowseName

        public static AdminShellNS.AdminShellPackageEnv Import(UANodeSet model)
        {
            thePackageEnv = new AdminShellNS.AdminShellPackageEnv();
            InformationModel = model;

            //Initialize everything needed
            AdminShell.AdministrationShellEnv env = thePackageEnv.AasEnv;
            var aas = new AdminShell.AdministrationShell();
            env.AdministrationShells.Add(aas);

            //search for the root Node
            var root = getRoot();
            //see Annotations
            if (root != null)
            {
                foreach (Reference _ref in root.References)
                {
                    if (_ref.ReferenceType == "HasComponent")
                    {

                        var node = findNode(_ref.Value);
                        //create Submodel
                        if (getTypeDefinition(node) == "1:AASSubmodelType")
                        {

                            var submodel = createSubmodel((UAObject)node);

                            if (submodel != null)
                            {
                                //add Submodel and its SubmodelRef
                                env.Submodels.Add(submodel);
                                var smr = new AdminShell.SubmodelRef();
                                smr.Keys.Add(
                                    new AdminShell.Key("Submodel", submodel.id.value));
                                aas.submodelRefs.Add(smr);
                            }

                        }
                        //create ConceptDictionary
                        else if (getTypeDefinition(node) == "1:AASConceptDictionaryType")
                        {
                            createConceptDictionary((UAObject)node);
                        }
                        //create Asset
                        ////else if (getTypeDefinition(node) == "1:AASAssetType")
                        ////{
                        ////    AssetInformation ass = createAsset(node);
                        ////    thePackageEnv.AasEnv.Assets.Add(ass);
                        ////}
                        //set DerivedFrom
                        else if (node.BrowseName == "1:DerivedFrom")
                        {
                            List<Key> keys = addSemanticID(node);
                            if (keys.Count > 0) aas.derivedFrom = new AssetAdministrationShellRef(keys[0]);
                        }
                        //create HasDataSpecification
                        else if (node.BrowseName == "1:DataSpecification")
                        {
                            aas.hasDataSpecification = CreateHasDataSpecification(node);
                        }
                        else if (node.BrowseName == "1:Identification" &&
                                getTypeDefinition(node) == "1:AASIdentifierType")
                        {
                            aas.id = createIdentification(node);
                        }

                    }
                    else if (_ref.ReferenceType == "HasProperty")
                    {
                        var node = findNode(_ref.Value);
                        if (node.BrowseName == "1:idShort")
                        {
                            var vari = (UAVariable)node;
                            aas.idShort = vari.Value.InnerText;
                        }
                    }

                }
            }

            return thePackageEnv;
        }

        private static UANode getRoot()
        {
            //the root node will always have the BrowseName AASAssetAdministrationShell
            try
            {
                return InformationModel.Items.First(x => x.BrowseName == "1:AASAssetAdministrationShell");
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }
            return null;
        }

        private static Identifier GetIdentification(UANode submodel)
        {
            //AASIdentifiable
            //  -> AASIdentifierType
            //      -> Id
            //      -> IdType

            //get AASIdentifiable node
            UANode iden = null;
            foreach (Reference _ref in submodel.References)
            {
                if (_ref.ReferenceType == "HasInterface")
                    iden = findNode(_ref.Value);
            }

            Identifier identification = new Identifier();
            foreach (Reference _ref in iden.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    if (getTypeDefinition(findNode(_ref.Value)) == "1:AASIdentifierType")
                    {
                        var val = (UAVariable)findNode(_ref.Value);
                        foreach (Reference _refref in val.References)
                        {
                            if (_refref.ReferenceType == "HasProperty")
                            {
                                UAVariable node = (UAVariable)findNode(_refref.Value);
                                if (node.BrowseName == "1:Id") identification.value = node.Value.InnerText;
                            }
                        }
                    }
                }
            }
            return identification;
        }

        private static Submodel createSubmodel(UAObject submodel)
        {
            //Submodel
            //  -> AASSubmodelElement (multiple)
            //  -> AASSemanticId
            //  -> Property (Category)
            //  -> ModellingKind


            if (isSubmodel(submodel))
            {
                //set Submodelparameters
                Identifier iden = GetIdentification(submodel);
                Submodel sub = Submodel.CreateNew("", iden.value);
                sub.idShort = makePretty(submodel.BrowseName);
                sub.kind = getKind(submodel);


                sub.submodelElements = new SubmodelElementWrapperCollection();

                foreach (Reference _ref in submodel.References)
                {
                    if (_ref.ReferenceType == "HasComponent")
                    {
                        var node = findNode(_ref.Value);
                        string type = getTypeDefinition(node);

                        //Set SemanticId
                        if (type == "1:AASSemanticIdType")
                        {
                            addSemanticID(sub, (UAVariable)node);
                        }

                        //Set SubmodelElements
                        if (type == "1:AASSubmodelElementType")
                        {
                            sub.submodelElements.Add(createSubmodelElement(node));
                        }
                    }

                    //set Kind and Category
                    if (_ref.ReferenceType == "HasProperty")
                    {
                        var var = findNode(_ref.Value);
                        if (var.BrowseName == "1:ModellingKind") sub.kind = getKind(var);
                        if (var.BrowseName == "1:Category") sub.category = getCategory(var);
                    }

                }

                return sub;
            }
            return null;
        }

        private static UANode findNode(string nodeId)
        {
            try
            {
                return (UANode)InformationModel.Items.First(x => x.NodeId == nodeId);
            }
            catch (Exception ex)
            {
                MessageBoxResult result = MessageBox.Show(
                    "NodeId " + nodeId + " could not be found.\nPlease check your NodeSet file.",
                    "Error",
                    MessageBoxButton.OK);
                throw ex;
            }
        }

        private static string getTypeDefinition(UANode node)
        {
            //search for HasTypeDefinitin Reference -> has NodeId of  the type as value
            Reference _ref = node.References.First(x => x.ReferenceType == "HasTypeDefinition");

            //if ns=0, the ObjectType will not be in the list, because it was not created
            if (!_ref.Value.Contains("ns=0;"))
            {
                var n = findNode(_ref.Value);
                return n.BrowseName;
            }
            return null;
        }

        private static bool isSubmodel(UAObject sub)
        {
            foreach (Reference _ref in sub.References)
            {
                if (_ref.ReferenceType == "HasTypeDefinition" && _ref.Value == "ns=1;i=1006")
                    return true;
            }
            return false;
        }

        //Create Parameters

        private static string getCategory(UANode node)
        {
            //Parent (node)
            //  -> Category (Property, String)

            string cat = null;
            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    UANode val = findNode(_ref.Value);
                    if (val.BrowseName == "1:Category")
                    {
                        UAVariable var = (UAVariable)val;
                        cat = var.Value.InnerText;
                    }

                }
            }
            return cat;
        }

        private static AdminShell.ModelingKind getKind(UANode node)
        {
            //Parent (node)
            // -> Kind (Property)

            ModelingKind kind = new ModelingKind();
            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    UANode val = findNode(_ref.Value);
                    if (getTypeDefinition(val) == "1:AASModelingKindDataType")
                    {
                        UAVariable var = (UAVariable)val;
                        kind.kind = var.Value.InnerText;
                    }

                }
            }
            return kind;
        }

        private static Qualifier getQualifier(UANode node)
        {
            //Qualifier
            // -> QualifierType (Property)
            // -> QualifierValue (Property)
            // -> Key (multiple)

            Qualifier qual = new Qualifier();
            List<Key> keys = new List<Key>();

            //create Keys
            keys = addSemanticID(node);
            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    if (_ref.ReferenceType == "HasComponent")
                    {

                    }
                    else
                    {
                        UAVariable var = (UAVariable)findNode(_ref.Value);
                        if (var.BrowseName == "1:QualifierType") qual.type = var.Value.InnerText;
                        if (var.BrowseName == "1:QualifierValue") qual.value = var.Value.InnerText;
                    }

                }
            }
            qual.semanticId = SemanticId.CreateFromKeys(keys);
            return qual;
        }

        private static List<Key> addSemanticID(UANode sem)
        {
            //SemanticId
            //  -> Key (multiple)
            //      -> idType
            //      -> Local
            //      -> Type
            //      -> Value

            List<Key> keys = new List<Key>();
            foreach (Reference _ref in sem.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition" &&
                    getTypeDefinition(findNode(_ref.Value)) == "1:AASKeyType")
                {
                    UAVariable key = (UAVariable)findNode(_ref.Value);
                    Key _key = new Key();
                    foreach (Reference _InnerRef in key.References)
                    {
                        if (_InnerRef.ReferenceType != "HasTypeDefinition")
                        {
                            // TODO: REMOVE IDTYPE && LOCAL

                            UAVariable value = (UAVariable)findNode(_InnerRef.Value);
                            switch (value.BrowseName)
                            {
                                case "1:IdType":
                                    break;
                                case "1:Local":
                                    break;
                                case "1:Type":
                                    _key.type = value.Value.InnerText;
                                    break;
                                case "1:Value":
                                    _key.value = value.Value.InnerText;
                                    break;
                            }
                        }
                    }
                    keys.Add(_key);
                }
            }
            return keys;


        }

        private static void addSemanticID(Submodel sub, UAVariable sem)
        {
            foreach (Reference _ref in sem.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    UAVariable key = (UAVariable)findNode(_ref.Value);
                    Identifier newid = new Identifier();
                    foreach (Reference _InnerRef in key.References)
                    {
                        if (_InnerRef.ReferenceType != "HasTypeDefinition")
                        {
                            // TODO: REMOVE IDTYPE && LOCAL

                            UAVariable value = (UAVariable)findNode(_InnerRef.Value);
                            switch (value.BrowseName)
                            {
                                case "1:IdType":
                                    break;
                                case "1:Local":
                                    break;
                                case "1:Type":
                                    break;
                                case "1:Value":
                                    newid.value = value.Value.InnerText;
                                    break;
                            }
                        }
                    }
                    sub.semanticId.Value.Add(newid);
                }
            }
        }

        private static ModelingKind createKind(string nodeId)
        {
            ModelingKind kind = new ModelingKind();
            var k = (UAVariable)findNode(nodeId);

            kind.kind = k.Value.InnerText;

            return kind;
        }

        private static AdminShell.ModelReference createReference(string val)
        {
            // Refereces are saved as Strings: [type, value]
            AdminShell.ModelReference reference = new AdminShell.ModelReference();
            //convert String to an actual Reference
            var mep = val.Split(',');
            if (mep.Length == 2)
            {
                string type = mep[0].Trim().TrimStart('[');
                string value = mep[1].Trim().TrimEnd(']');
                reference = AdminShell.ModelReference.CreateNew(type, value);
            }
            return reference;
        }

        //Create Submodel Elements

        private static AdminShell.SubmodelElementWrapper createSubmodelElement(UANode node)
        {
            //Parent (node)
            //  -> SemanticId
            //  -> Category (Property)
            //  -> Qualifier
            //  -> Kind (Property)
            //  -> DataNode (Same name as Type)

            AdminShell.SubmodelElementWrapper wrapper = new AdminShell.SubmodelElementWrapper();
            wrapper.submodelElement = new SubmodelElement();
            List<Key> keys = new List<Key>();
            QualifierCollection quals = new QualifierCollection();

            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    var val = findNode(_ref.Value);
                    string type = getTypeDefinition(val);

                    if (type == "1:AASSemanticIdType")
                    {
                        keys = addSemanticID((UAVariable)val);
                    }
                    else if (getTypeDefinition(val) == "1:AASQualifierType")
                    {
                        quals.Add(getQualifier(val));
                    }
                    else
                    {
                        setElementData(findNode(_ref.Value), wrapper);
                    }
                }
            }

            wrapper.submodelElement.idShort = makePretty(node.BrowseName);
            wrapper.submodelElement.semanticId = SemanticId.CreateFromKeys(keys);
            wrapper.submodelElement.kind = getKind(node);
            wrapper.submodelElement.category = getCategory(node);
            wrapper.submodelElement.qualifiers = quals;

            return wrapper;
        }

        private static void setElementData(UANode node, SubmodelElementWrapper wrapper)
        {
            //check which type the Element is, set Data accordingly
            string type = getTypeDefinition(node);
            switch (type)
            {
                case "1:AASSubmodelElementCollectionType":
                    wrapper.submodelElement = setCollection(node);
                    break;
                case "1:AASFileType":
                    wrapper.submodelElement = setFile(node);
                    break;
                case "1:AASReferenceElementType":
                    wrapper.submodelElement = setReferenceElement(node);
                    break;
                case "1:AASPropertyType":
                    wrapper.submodelElement = setPropertyType(node);
                    break;
                case "1:AASBlobType":
                    wrapper.submodelElement = setBlob(node);
                    break;
                case "1:AASRelationshipElementType":
                    wrapper.submodelElement = setRealtionshipElement(node);
                    break;

            }
        }

        private static AdminShell.Property setPropertyType(UANode node)
        {
            //Property
            //  -> Value
            //  -> ValueId

            Property prop = new Property();

            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType == "HasProperty")
                {
                    var var = (UAVariable)findNode(_ref.Value);
                    if (var.BrowseName == "1:Value") prop.value = var.Value.InnerText; prop.valueType = var.DataType;
                    if (var.BrowseName == "1:ValueId") prop.valueId =
                        AdminShell.GlobalReference.CreateNew(createReference(var.Value.InnerText));
                }
            }
            return prop;
        }

        private static ReferenceElement setReferenceElement(UANode node)
        {
            //ReferenceElement
            //  -> Value

            ReferenceElement refEle = new ReferenceElement();
            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    UAVariable var = (UAVariable)findNode(_ref.Value);
                    string val = var.Value.InnerText;
                    //convert String into Reference
                    refEle.value = createReference(val);

                }
            }

            return refEle;
        }

        private static SubmodelElementCollection setCollection(UANode node)
        {
            //Collection
            //  -> SubmodelElement (multiple)

            SubmodelElementCollection coll = new SubmodelElementCollection();
            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType == "HasComponent")
                {
                    coll.value.Add(createSubmodelElement(findNode(_ref.Value)));
                }
            }
            return coll;
        }

        private static File setFile(UANode node)
        {
            //File
            //  -> MimeType 
            //  -> Value 

            File file = new File();
            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    var val = (UAVariable)findNode(_ref.Value);
                    if (val.BrowseName == "1:MimeType") file.mimeType = val.Value.InnerText;
                    if (val.BrowseName == "1:Value") file.value = val.Value.InnerText;
                }

            }
            return file;
        }

        private static Blob setBlob(UANode node)
        {
            //Blob
            //  -> File
            //  -> MimeType


            Blob blob = new Blob();
            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    var val = (UAVariable)findNode(_ref.Value);
                    if (val.BrowseName == "1:File") blob.value = val.Value.InnerText;
                    if (val.BrowseName == "1:MimeType") blob.mimeType = val.Value.InnerText;
                }
            }
            return blob;
        }

        private static RelationshipElement setRealtionshipElement(UANode node)
        {
            //RelationshipElement
            //  -> First (Reference)
            //  -> Second (Reference)

            RelationshipElement elem = new RelationshipElement();

            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    UAVariable var = (UAVariable)findNode(_ref.Value);
                    if (var.BrowseName == "1:First") elem.first = createReference(var.Value.InnerText);
                    if (var.BrowseName == "1:Second") elem.second = createReference(var.Value.InnerText);
                }
            }

            return elem;
        }

        //Create ConceptDictionary

        private static void createConceptDictionary(UAObject dic)
        {
            //ConceptDictionary
            //  -> DictionaryEntry (multiple)
            //      -> AASIrdiConceptDescription 
            //                  (or)
            //      -> AASUriConceptDescription 

            var descs = thePackageEnv.AasEnv.ConceptDescriptions;
            foreach (Reference _ref in dic.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    UANode val = findNode(_ref.Value);
                    foreach (Reference _refref in val.References)
                    {
                        if (_refref.ReferenceType != "HasTypeDefinition")
                        {
                            var mep = createConceptDescription(findNode(_refref.Value), val.BrowseName);
                            descs.Add(mep);
                        }
                    }
                }
            }
        }

        private static ConceptDescription createConceptDescription(UANode node, string name)
        {
            //ConceptDescription
            //  -> AASIdentifiable
            //      -> Identification
            //      -> Administration
            //  -> DataSpecification
            //      -> DataSpecificationIEC61360

            ConceptDescription desc = new ConceptDescription();
            Administration admin = new Administration();
            Identifier iden = new Identifier();

            var esc = EmbeddedDataSpecification.CreateIEC61360WithContent();
            esc.dataSpecificationContent.dataSpecificationIEC61360.shortName =
                new LangStringSetIEC61360("EN?", makePretty(name));

            desc.embeddedDataSpecification = new HasDataSpecification();
            desc.embeddedDataSpecification.Add(esc);


            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    UANode val = findNode(_ref.Value);
                    if (getTypeDefinition(val) == "1:IAASIdentifiableType")
                    {
                        foreach (Reference _refref in val.References)
                        {
                            if (_refref.ReferenceType != "HasTypeDefinition")
                            {
                                UANode var = findNode(_refref.Value);
                                if (getTypeDefinition(var) == "1:AASIdentifierType") iden = createIdentification(var);
                                if (getTypeDefinition(var) == "1:AASAdministrativeInformationType")
                                    admin = createAdmninistration(var);
                            }
                        }
                    }
                    if (getTypeDefinition(val) == "1:AASDataSpecificationType")
                    {
                        foreach (Reference _refref in val.References)
                        {
                            if (_refref.ReferenceType == "HasComponent")
                            {
                                setIECSpec(findNode(_ref.Value), desc);
                            }
                        }

                    }
                }
            }

            desc.id = iden;
            desc.administration = admin;
            return desc;
        }

        private static void setIECSpec(UANode node, ConceptDescription desc)
        {
            //DataSpecificationIEC61360
            //  -> many, many parameters

            var dsc = desc.GetIEC61360();

            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    foreach (Reference _refref in findNode(_ref.Value).References)
                    {
                        if (_refref.ReferenceType != "HasTypeDefinition")
                        {
                            if (_refref.ReferenceType == "HasProperty")
                            {
                                UAVariable var = (UAVariable)findNode(_refref.Value);
                                if (var.BrowseName == "1:DataType" && dsc != null)
                                    dsc.dataType = var.Value.InnerText;

                                if (var.BrowseName == "1:Symbol" && dsc != null)
                                    dsc.symbol = var.Value.InnerText;

                                if (var.BrowseName == "1:Unit" && dsc != null)
                                    dsc.unit = var.Value.InnerText;

                                if (var.BrowseName == "1:ValueFormat" && dsc != null)
                                    dsc.valueFormat = var.Value.InnerText;

                                if (var.BrowseName == "1:IdShort ") desc.idShort = var.Value.InnerText;
                                if (var.BrowseName == "1:Category ") desc.category = var.Value.InnerText;
                            }
                            else if (_refref.ReferenceType == "HasComponent")
                            {
                                UANode obj = findNode(_refref.Value);
                                if (obj.BrowseName == "1:Definition" && dsc != null)
                                    dsc.definition = new LangStringSetIEC61360(getDescription(obj));

                                if (obj.BrowseName == "1:PreferredName" && dsc != null)
                                    dsc.preferredName = new LangStringSetIEC61360(getDescription(obj));
                            }
                        }
                    }
                }
            }
            desc.SetAdminstration("2.0", "1");
            desc.SetIdentification(
                "URI", "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/2/0");
        }

        private static Identifier createIdentification(UANode node)
        {
            //Identification
            //  -> Id
            //  -> IdType

            Identifier iden = new Identifier();
            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    var val = (UAVariable)findNode(_ref.Value);
                    if (val.BrowseName == "1:Id") iden.value = val.Value.InnerText;
                }

            }
            return iden;
        }

        private static Administration createAdmninistration(UANode node)
        {
            //Administration (node)
            //  -> Version (val)
            //  -> Revision (val)

            Administration admin = new Administration();
            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    var val = (UAVariable)findNode(_ref.Value);
                    if (val.BrowseName == "1:Version") admin.version = val.Value.InnerText;
                    if (val.BrowseName == "1:Revision") admin.revision = val.Value.InnerText;
                }
            }
            return admin;
        }

        //Create Asset

        private static AssetInformation createAsset(UANode node)
        {
            //Asset (node)
            //  -> AASIdentifiable (var)
            //  -> ModellingKind (var)
            //  -> AASReferable (var)

            AssetInformation asset = new AssetInformation();
            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    UANode var = findNode(_ref.Value);
                    if (getTypeDefinition(var) == "1:IAASIdentifiableType") setIdentifiable(asset, var);
                    if (getTypeDefinition(var) == "1:AASModelingKindDataType")
                    {
                        asset.assetKind = new AssetKind(); asset.assetKind.kind = createKind(_ref.Value).kind;
                    }
                    if (getTypeDefinition(var) == "1:IAASReferableType") setReferable(asset, var);
                }
            }
            return asset;
        }

        private static void setIdentifiable(AssetInformation asset, UANode node)
        {
            //AASIdentifiable (node)
            //  -> AASAdministrativeInformationType (var)
            //  -> AASIdentifieryType (var)

            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    UANode var = findNode(_ref.Value);

                    if (getTypeDefinition(var) == "1:AASIdentifierType")
                        asset.SetIdentification(createIdentification(var));
                }
            }
        }

        private static void setReferable(AssetInformation asset, UANode node)
        {
            //AASReferable (node)
            //  -> Category (var)
            //  -> Description (var)

            ////asset.description = new Description();
            ////asset.description.langString = new ListOfLangStr();

            ////foreach (Reference _ref in node.References)
            ////{
            ////    if (_ref.ReferenceType != "HasTypeDefinition")
            ////    {
            ////        UANode var = findNode(_ref.Value);
            ////        if (var.BrowseName == "1:Category")
            ////        {
            ////            var temp = (UAVariable)var;
            ////            asset.category = temp.Value.InnerText;
            ////        }
            ////        else if (var.BrowseName == "1:Description")
            ////        {
            ////            asset.description.langString = getDescription(var);
            ////        }
            ////    }
            ////}
        }

        private static ListOfLangStr getDescription(UANode node)
        {
            //Parent (node)
            //  -> AASLangStrSet (val)  (multiple)
            //      -> Language (var)
            //      -> String   (var)

            ListOfLangStr str = new ListOfLangStr();

            foreach (Reference _ref in node.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    UANode val = findNode(_ref.Value);
                    LangStr v = new LangStr();
                    foreach (Reference _refref in val.References)
                    {
                        if (_refref.ReferenceType != "HasTypeDefinition")
                        {
                            UAVariable var = (UAVariable)findNode(_refref.Value);
                            if (var.BrowseName == "1:Language") v.lang = var.Value.InnerText;
                            if (var.BrowseName == "1:String") v.str = var.Value.InnerText;
                        }
                    }
                    str.Add(v);
                }
            }

            return str;
        }

        private static HasDataSpecification CreateHasDataSpecification(UANode spec)
        {
            //HasDataSpecification (spec)
            //  -> Reference (mupltiple)

            HasDataSpecification data = new HasDataSpecification();
            foreach (Reference _ref in spec.References)
            {
                if (_ref.ReferenceType != "HasTypeDefinition")
                {
                    data.Add(
                        new EmbeddedDataSpecification(
                                AdminShell.GlobalReference.CreateNew(
                                    createReference(findNode(_ref.Value)))));
                }
            }
            return data;
        }

        private static AdminShell.ModelReference createReference(UANode node)
        {
            //Reference (node)
            //  -> Key (multiple)

            List<Key> keys = new List<Key>();
            keys = addSemanticID(node);
            AdminShell.ModelReference refe = AdminShell.ModelReference.CreateNew(keys);
            return refe;
        }

        private static AssetRef createAssetRef(UANode node)
        {
            //AssetRef
            //  -> Key (multiple)


            AssetRef ass = new AssetRef();
            var keys = addSemanticID(node);
            foreach (Key key in keys)
            {
                ass.Value.Add(key?.value);
            }
            return ass;
        }

        private static string makePretty(string str)
        {
            //removes the namespace from the string
            //used for every name because:
            //  numbers are not allowed as the first character in idShort
            //  when exporting the namespace gets added again, resulting in mutiple 1: (1:1:...)
            if (str != null)
            {
                string[] strings = str.Split(':');
                if (strings.Length >= 2)
                {
                    str = strings[1];
                }
            }
            return str;
        }
    }
}
