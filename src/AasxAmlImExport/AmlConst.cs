/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxAmlImExport
{
    public static class AmlConst
    {
        public static class Names
        {
            public static string AmlLanguageHeader = "aml-lang=";
            public static string AmlQualifierHeader = "qualifier:";

            public static string RootInstHierarchy = "AssetAdministrationShellInstanceHierarchy";
            public static string RootSystemUnitClasses = "AssetAdministrationShellSystemUnitClasses";
            public static string RootConceptDescriptions = "AssetAdministrationShellConceptDescriptions";
        }


        public static class Attributes
        {
            public static string Referable_IdShort = "AAS:IReferable/idShort";
            public static string Referable_Category = "AAS:IReferable/category";
            public static string Referable_Description = "AAS:IReferable/description";

            public static string Asset_Kind = "AAS:AssetInformation/kind";
            public static string HasKind_Kind = "AAS:HasKind/kind";

            public static string SemanticId = "AAS:HasSemantics/semanticId";

            public static string Identification = "AAS:Identifiable/identification";
            public static string Identification_idType = "AAS:Identifier/idType";
            public static string Identification_id = "AAS:Identifier/id";

            public static string Administration = "AAS:Identifiable/administration";
            public static string Administration_Version = "AAS:AdministrativeInformation/version";
            public static string Administration_Revision = "AAS:AdministrativeInformation/revision";

            public static string DataSpecificationRef = "AAS:HasDataSpecification/dataSpecification";

            public static string Qualifer = "AAS:Qualifiable/qualifier";
            public static string Qualifer_Type = "AAS:Qualifier/type";
            public static string Qualifer_Value = "AAS:Qualifier/value";
            public static string Qualifer_ValueId = "AAS:Qualifier/valueId";

            public static string AAS_DerivedFrom = "AAS:AssetAdministrationShell/derivedFrom";

            public static string Asset_IdentificationModelRef = "AAS:AssetInformation/assetIdentificationModel";
            public static string Asset_BillOfMaterialRef = "AAS:AssetInformation/billOfMaterial";

            public static string SME_Property = "AAS:Property";

            public static string Property_Value = "AAS:Property/value";
            public static string File_Value = "AAS:File/value";
            public static string Blob_Value = "AAS:Blob/value";
            public static string ReferenceElement_Value = "AAS:ReferenceElement/value";
            public static string Range_Min = "AAS:Range/min";
            public static string Range_Max = "AAS:Range/max";

            public static string SMEC_ordered = "AAS:SubmodelElementCollection/ordered";
            public static string SMEC_allowDuplicates = "AAS:SubmodelElementCollection/allowDuplicates";

            public static string Property_ValueId = "AAS:Property/valueId";

            public static string MultiLanguageProperty_Value = "AAS:MultiLanguageProperty/value";
            public static string MultiLanguageProperty_ValueId = "AAS:MultiLanguageProperty/valueId";

            public static string Blob_MimeType = "AAS:Blob/mimeType";
            public static string File_MimeType = "AAS:File/mimeType";

            public static string RelationshipElement_First = "AAS:RelationshipElement/first";
            public static string RelationshipElement_Second = "AAS:RelationshipElement/second";

            public static string Entity_entityType = "AAS:Entity/entityType";
            public static string Entity_asset = "AAS:Entity/asset";

            public static string CD_IsCaseOf = "AAS:ConceptDescription/isCaseOf";
            public static string CD_EmbeddedDataSpecification = "AAS:ConceptDescription/EmbeddedDataSpecification";
            public static string CD_DataSpecificationRef = "AAS:ConceptDescription/dataSpecification";
            public static string CD_DataSpecificationContent = "AAS:DataSpecification/DataSpecificationContent";
            public static string CD_DataSpecification61360 = "AAS:DataSpecification/DataSpecificationIEC61360";

            public static string CD_DSC61360_PreferredName = "IEC:DataSpecificationIEC61360/preferredName";
            public static string CD_DSC61360_ShortName = "IEC:DataSpecificationIEC61360/shortName";
            public static string CD_DSC61360_Unit = "IEC:DataSpecificationIEC61360/unit";
            public static string CD_DSC61360_UnitId = "IEC:DataSpecificationIEC61360/unitId";
            public static string CD_DSC61360_ValueFormat = "IEC:DataSpecificationIEC61360/valueFormat";
            public static string CD_DSC61360_SourceOfDefinition = "IEC:DataSpecificationIEC61360/sourceOfDefinition";
            public static string CD_DSC61360_Symbol = "IEC:DataSpecificationIEC61360/symbol";
            public static string CD_DSC61360_DataType = "IEC:DataSpecificationIEC61360/dataType";
            public static string CD_DSC61360_Definition = "IEC:DataSpecificationIEC61360/definition";
        }

        public static class Roles
        {
            public static string Qualifer = "AssetAdministrationShellRoleClassLib/Qualifier";

            public static string AssetInformation = "AssetAdministrationShellRoleClassLib/AssetInformation";

            public static string View = "AssetAdministrationShellRoleClassLib/View";
            public static string ContainedElementRef = "AssetAdministrationShellRoleClassLib/ContainedElementRef";

            public static string AAS = "AssetAdministrationShellRoleClassLib/AssetAdministrationShell";

            public static string Submodel = "AssetAdministrationShellRoleClassLib/Submodel";

            public static string SubmodelElement_Header = "AssetAdministrationShellRoleClassLib/";
            public static string SubmodelElement_SMEC =
                "AssetAdministrationShellRoleClassLib/SubmodelElementCollection";

            public static string OperationVariableIn = "AssetAdministrationShellRoleClassLib/OperationInputVariables";
            public static string OperationVariableOut =
                "AssetAdministrationShellRoleClassLib/OperationOutputVariables";
            public static string OperationVariableInOut =
                "AssetAdministrationShellRoleClassLib/OperationInoutputVariables";

            public static string SubmodelElement_Entity =
                "AssetAdministrationShellRoleClassLib/Entity";

            public static string SubmodelElement_AnnotatedRelationship =
                "AssetAdministrationShellRoleClassLib/AnnotatedRelationshipElement";

            public static string ConceptDescription = "AssetAdministrationShellRoleClassLib/ConceptDescription";
            public static string DataSpecificationContent =
                "AssetAdministrationShellRoleClassLib/DataSpecificationContent";
        }

        public static class Classes
        {
            public static string DataSpecificationContent61360 =
                "AssetAdministrationShellDataSpecificationTemplates/DataSpecificationIEC61360Template/" +
                "DataSpecificationIEC61360";
        }

        public static class Interfaces
        {
            public static string FileDataReference = "AssetAdministrationShellInterfaceClassLib/FileDataReference";
            public static string ReferableReference = "AssetAdministrationShellInterfaceClassLib/ReferableReference";
        }
    }
}
