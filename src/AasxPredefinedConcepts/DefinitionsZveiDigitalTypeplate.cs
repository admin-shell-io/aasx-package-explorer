/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using JetBrains.Annotations;

namespace AasxPredefinedConcepts
{
    public class DefinitionsZveiDigitalTypeplate : AasxDefinitionBase
    {

        //
        // Concepts..
        //

        public DefinitionsZveiDigitalTypeplate()
        {
            this._library = BuildLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "ZveiDigitalTypeplate.json");
        }

        [JetBrains.Annotations.UsedImplicitly]
        public class SetOfNameplate
        {
            public AasCore.Aas3_0_RC02.Submodel
                SM_Nameplate;

            public AasCore.Aas3_0_RC02.ConceptDescription
                CD_ManufacturerName,
                CD_ManufacturerProductDesignation,
                CD_PhysicalAddress,
                CD_CountryCode,
                CD_Street,
                CD_Zip,
                CD_CityTown,
                CD_StateCounty,
                CD_ManufacturerProductFamily,
                CD_SerialNumber,
                CD_BatchNumber,
                CD_ProductCountryOfOrigin,
                CD_YearOfConstruction,
                CD_ProductMarking,
                CD_CEQualificationPresent,
                CD_File,
                CD_CRUUSLabelingPresent;

            public SetOfNameplate(AasxDefinitionBase bs)
            {
                this.SM_Nameplate = bs.RetrieveReferable<AasCore.Aas3_0_RC02.Submodel>("SM_Nameplate");

                this.CD_ManufacturerName = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_ManufacturerName");
                this.CD_ManufacturerProductDesignation = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_ManufacturerProductDesignation");
                this.CD_PhysicalAddress = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_PhysicalAddress");
                this.CD_CountryCode = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_CountryCode");
                this.CD_Street = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_Street");
                this.CD_Zip = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_Zip");
                this.CD_CityTown = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_CityTown");
                this.CD_StateCounty = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_StateCounty");
                this.CD_ManufacturerProductFamily = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_ManufacturerProductFamily");
                this.CD_SerialNumber = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_SerialNumber");
                this.CD_BatchNumber = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_BatchNumber");
                this.CD_ProductCountryOfOrigin = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_ProductCountryOfOrigin");
                this.CD_YearOfConstruction = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_YearOfConstruction");
                this.CD_ProductMarking = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_ProductMarking");
                this.CD_CEQualificationPresent = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_CEQualificationPresent");
                this.CD_File = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_File");
                this.CD_CRUUSLabelingPresent = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_CRUUSLabelingPresent");
            }

            public AasCore.Aas3_0_RC02.IReferable[] GetAllReferables()
            {
                return new AasCore.Aas3_0_RC02.IReferable[] {
                    SM_Nameplate,
                    CD_ManufacturerName,
                    CD_ManufacturerProductDesignation,
                    CD_PhysicalAddress,
                    CD_CountryCode,
                    CD_Street,
                    CD_Zip,
                    CD_CityTown,
                    CD_StateCounty,
                    CD_ManufacturerProductFamily,
                    CD_SerialNumber,
                    CD_BatchNumber,
                    CD_ProductCountryOfOrigin,
                    CD_YearOfConstruction,
                    CD_ProductMarking,
                    CD_CEQualificationPresent,
                    CD_File,
                    CD_CRUUSLabelingPresent
                };
            }
        }

        [JetBrains.Annotations.UsedImplicitly]
        public class SetOfIdentification
        {
            public AasCore.Aas3_0_RC02.Submodel
                SM_Identification;

            public AasCore.Aas3_0_RC02.ConceptDescription
                CD_ManufacturerName,
                CD_GLNOfManufacturer,
                CD_SupplierOfTheIdentifier,
                CD_MAN_PROD_NUM,
                CD_ManufacturerProductDesignation,
                CD_ManufacturerProductDescription,
                CD_NameOfSupplier,
                CD_GLNOfSupplier,
                CD_SupplierIdProvider,
                CD_SUP_PROD_NUM,
                CD_SupplierProductDesignation,
                CD_SupplierProductDescription,
                CD_ManufacturerProductFamily,
                CD_ClassificationSystem,
                CD_SecondaryKeyTyp,
                CD_TypThumbnail,
                CD_AssetId,
                CD_SerialNumber,
                CD_BatchNumber,
                CD_SecondaryKeyInstance,
                CD_DateOfManufacture,
                CD_DeviceRevision,
                CD_SoftwareRevision,
                CD_HardwareRevision,
                CD_QrCode,
                CD_OrganisationContactInfo,
                CD_ContactInfo_Role,
                CD_PhysicalAddress,
                CD_CountryCode,
                CD_Street,
                CD_Zip,
                CD_CityTown,
                CD_StateCounty,
                CD_Email,
                CD_URL,
                CD_PhoneNumber,
                CD_Fax,
                CD_CompanyLogo;

            public SetOfIdentification(AasxDefinitionBase bs)
            {
                this.SM_Identification = bs.RetrieveReferable<AasCore.Aas3_0_RC02.Submodel>("SM_Identification");

                this.CD_ManufacturerName = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_ManufacturerName");
                this.CD_GLNOfManufacturer = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_GLNOfManufacturer");
                this.CD_SupplierOfTheIdentifier = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_SupplierOfTheIdentifier");
                this.CD_MAN_PROD_NUM = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_MAN_PROD_NUM");
                this.CD_ManufacturerProductDesignation = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_ManufacturerProductDesignation");
                this.CD_ManufacturerProductDescription = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_ManufacturerProductDescription");
                this.CD_NameOfSupplier = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_NameOfSupplier");
                this.CD_GLNOfSupplier = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_GLNOfSupplier");
                this.CD_SupplierIdProvider = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_SupplierIdProvider");
                this.CD_SUP_PROD_NUM = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_SUP_PROD_NUM");
                this.CD_SupplierProductDesignation = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_SupplierProductDesignation");
                this.CD_SupplierProductDescription = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_SupplierProductDescription");
                this.CD_ManufacturerProductFamily = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_ManufacturerProductFamily");
                this.CD_ClassificationSystem = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_ClassificationSystem");
                this.CD_SecondaryKeyTyp = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_SecondaryKeyTyp");
                this.CD_TypThumbnail = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_TypThumbnail");
                this.CD_AssetId = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_AssetId");
                this.CD_SerialNumber = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_SerialNumber");
                this.CD_BatchNumber = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_BatchNumber");
                this.CD_SecondaryKeyInstance = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_SecondaryKeyInstance");
                this.CD_DateOfManufacture = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_DateOfManufacture");
                this.CD_DeviceRevision = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_DeviceRevision");
                this.CD_SoftwareRevision = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_SoftwareRevision");
                this.CD_HardwareRevision = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_HardwareRevision");
                this.CD_QrCode = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_QrCode");
                this.CD_OrganisationContactInfo = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_OrganisationContactInfo");
                this.CD_ContactInfo_Role = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_ContactInfo_Role");
                this.CD_PhysicalAddress = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_PhysicalAddress");
                this.CD_CountryCode = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_CountryCode");
                this.CD_Street = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_Street");
                this.CD_Zip = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_Zip");
                this.CD_CityTown = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_CityTown");
                this.CD_StateCounty = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_StateCounty");
                this.CD_Email = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_Email");
                this.CD_URL = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_URL");
                this.CD_PhoneNumber = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_PhoneNumber");
                this.CD_Fax = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_Fax");
                this.CD_CompanyLogo = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_CompanyLogo");

            }

            public AasCore.Aas3_0_RC02.IReferable[] GetAllReferables()
            {
                return new AasCore.Aas3_0_RC02.IReferable[] {
                    SM_Identification,
                    CD_ManufacturerName,
                    CD_GLNOfManufacturer,
                    CD_SupplierOfTheIdentifier,
                    CD_MAN_PROD_NUM,
                    CD_ManufacturerProductDesignation,
                    CD_ManufacturerProductDescription,
                    CD_NameOfSupplier,
                    CD_GLNOfSupplier,
                    CD_SupplierIdProvider,
                    CD_SUP_PROD_NUM,
                    CD_SupplierProductDesignation,
                    CD_SupplierProductDescription,
                    CD_ManufacturerProductFamily,
                    CD_ClassificationSystem,
                    CD_SecondaryKeyTyp,
                    CD_TypThumbnail,
                    CD_AssetId,
                    CD_SerialNumber,
                    CD_BatchNumber,
                    CD_SecondaryKeyInstance,
                    CD_DateOfManufacture,
                    CD_DeviceRevision,
                    CD_SoftwareRevision,
                    CD_HardwareRevision,
                    CD_QrCode,
                    CD_OrganisationContactInfo,
                    CD_ContactInfo_Role,
                    CD_PhysicalAddress,
                    CD_CountryCode,
                    CD_Street,
                    CD_Zip,
                    CD_CityTown,
                    CD_StateCounty,
                    CD_Email,
                    CD_URL,
                    CD_PhoneNumber,
                    CD_Fax,
                    CD_CompanyLogo
                };
            }
        }

        public class SetOfDocumentation
        {
            public AasCore.Aas3_0_RC02.Submodel
                SM_Document;

            public AasCore.Aas3_0_RC02.ConceptDescription
                CD_DocumentationItem,
                CD_DocumentType,
                CD_VDI2770_DomainId,
                CD_VDI2770_IdType,
                CD_DocumentId,
                CD_DocumentDomainId,
                CD_VDI2770_Role,
                CD_VDI2770_OrganisationId,
                CD_VDI2770_OrganisationName,
                CD_VDI2770_OrganisationOfficialName,
                CD_VDI2770_Description,
                CD_DocumentPartId,
                CD_DocumentClassification_ClassId,
                CD_VDI2770_ClassName,
                CD_VDI2770_ClassificationSystem,
                CD_DocumentVersionId,
                CD_DocumentVersion_LanguageCode,
                CD_VDI2770_Title,
                CD_VDI2770_Summary,
                CD_VDI2770_Keywords,
                CD_VDI2770_StatusValue,
                CD_VDI2770_SetDate,
                CD_VDI2770_Purpose,
                CD_VDI2770_BasedOnProcedure,
                CD_VDI2770_Comments,
                CD_VDI2770_ReferencedObject_Type,
                CD_VDI2770_ReferencedObject_RefType,
                CD_VDI2770_ReferencedObject_ObjectId,
                CD_VDI2770_FileId,
                CD_VDI2770_FileName,
                CD_VDI2770_FileFormat,
                CD_File;

            public SetOfDocumentation(AasxDefinitionBase bs)
            {
                this.SM_Document = bs.RetrieveReferable<AasCore.Aas3_0_RC02.Submodel>("SM_Document");

                this.CD_DocumentationItem = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_DocumentationItem");
                this.CD_DocumentType = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_DocumentType");
                this.CD_VDI2770_DomainId = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_VDI2770_DomainId");
                this.CD_VDI2770_IdType = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_VDI2770_IdType");
                this.CD_DocumentId = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_DocumentId");
                this.CD_DocumentDomainId = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_DocumentDomainId");
                this.CD_VDI2770_Role = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_VDI2770_Role");
                this.CD_VDI2770_OrganisationId = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_VDI2770_OrganisationId");
                this.CD_VDI2770_OrganisationName = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_VDI2770_OrganisationName");
                this.CD_VDI2770_OrganisationOfficialName = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_VDI2770_OrganisationOfficialName");
                this.CD_VDI2770_Description = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_VDI2770_Description");
                this.CD_DocumentPartId = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_DocumentPartId");
                this.CD_DocumentClassification_ClassId = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_DocumentClassification_ClassId");
                this.CD_VDI2770_ClassName = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_VDI2770_ClassName");
                this.CD_VDI2770_ClassificationSystem = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_VDI2770_ClassificationSystem");
                this.CD_DocumentVersionId = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_DocumentVersionId");
                this.CD_DocumentVersion_LanguageCode = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_DocumentVersion_LanguageCode");
                this.CD_VDI2770_Title = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_VDI2770_Title");
                this.CD_VDI2770_Summary = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_VDI2770_Summary");
                this.CD_VDI2770_Keywords = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_VDI2770_Keywords");
                this.CD_VDI2770_StatusValue = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_VDI2770_StatusValue");
                this.CD_VDI2770_SetDate = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_VDI2770_SetDate");
                this.CD_VDI2770_Purpose = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_VDI2770_Purpose");
                this.CD_VDI2770_BasedOnProcedure = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_VDI2770_BasedOnProcedure");
                this.CD_VDI2770_Comments = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_VDI2770_Comments");
                this.CD_VDI2770_ReferencedObject_Type = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_VDI2770_ReferencedObject_Type");
                this.CD_VDI2770_ReferencedObject_RefType = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_VDI2770_ReferencedObject_RefType");
                this.CD_VDI2770_ReferencedObject_ObjectId = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_VDI2770_ReferencedObject_ObjectId");
                this.CD_VDI2770_FileId = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_VDI2770_FileId");
                this.CD_VDI2770_FileName = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_VDI2770_FileName");
                this.CD_VDI2770_FileFormat = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>(
                    "CD_VDI2770_FileFormat");
                this.CD_File = bs.RetrieveReferable<AasCore.Aas3_0_RC02.ConceptDescription>("CD_DigitalFile");

            }

            public AasCore.Aas3_0_RC02.IReferable[] GetAllReferables()
            {
                return new AasCore.Aas3_0_RC02.IReferable[] {
                    SM_Document,
                    CD_DocumentationItem,
                    CD_DocumentType,
                    CD_VDI2770_DomainId,
                    CD_VDI2770_IdType,
                    CD_DocumentId,
                    CD_DocumentDomainId,
                    CD_VDI2770_Role,
                    CD_VDI2770_OrganisationId,
                    CD_VDI2770_OrganisationName,
                    CD_VDI2770_OrganisationOfficialName,
                    CD_VDI2770_Description,
                    CD_DocumentPartId,
                    CD_DocumentClassification_ClassId,
                    CD_VDI2770_ClassName,
                    CD_VDI2770_ClassificationSystem,
                    CD_DocumentVersionId,
                    CD_DocumentVersion_LanguageCode,
                    CD_VDI2770_Title,
                    CD_VDI2770_Summary,
                    CD_VDI2770_Keywords,
                    CD_VDI2770_StatusValue,
                    CD_VDI2770_SetDate,
                    CD_VDI2770_Purpose,
                    CD_VDI2770_BasedOnProcedure,
                    CD_VDI2770_Comments,
                    CD_VDI2770_ReferencedObject_Type,
                    CD_VDI2770_ReferencedObject_RefType,
                    CD_VDI2770_ReferencedObject_ObjectId,
                    CD_VDI2770_FileId,
                    CD_VDI2770_FileName,
                    CD_VDI2770_FileFormat,
                    CD_File
                };
            }
        }
    }
}
