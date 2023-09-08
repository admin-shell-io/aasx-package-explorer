/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Reflection;
using Aas = AasCore.Aas3_0;

// ReSharper disable UnassignedField.Global
// (working by reflection)

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions of Submodel Digital Nameplate 
    /// </summary>
    public class DigitalNameplateV20 : AasxDefinitionBase
    {
        public static DigitalNameplateV20 Static = new DigitalNameplateV20();

        public Aas.Submodel
            SM_Nameplate;

        public Aas.ConceptDescription
            CD_URIOfTheProduct,
            CD_ManufacturerName,
            CD_ManufacturerProductDesignation,
            CD_ContactInformation,
            CD_RoleOfContactPerson,
            CD_NationalCode,
            CD_Language,
            CD_TimeZone,
            CD_CityTown,
            CD_Company,
            CD_Department,
            CD_Phone,
            CD_TelephoneNumber,
            CD_TypeOfTelephone,
            CD_AvailableTime,
            CD_Fax,
            CD_FaxNumber,
            CD_TypeOfFaxNumber,
            CD_Email,
            CD_EmailAddress,
            CD_PublicKey,
            CD_TypeOfEmailAddress,
            CD_TypeOfPublicKey,
            CD_IPCommunication,
            CD_AddressOfAdditionalLink,
            CD_TypeOfCommunication,
            CD_Street,
            CD_Zipcode,
            CD_POBox,
            CD_ZipCodeOfPOBox,
            CD_StateCounty,
            CD_NameOfContact,
            CD_FirstName,
            CD_MiddleNames,
            CD_Title,
            CD_AcademicTitle,
            CD_FurtherDetailsOfContact,
            CD_ManufacturerProductRoot,
            CD_ManufacturerProductFamily,
            CD_ManufacturerProductType,
            CD_OrderCodeOfManufacturer,
            CD_ProductArticleNumberOfManufacturer,
            CD_SerialNumber,
            CD_YearOfConstruction,
            CD_DateOfManufacture,
            CD_HardwareVersion,
            CD_FirmwareVersion,
            CD_SoftwareVersion,
            CD_CountryOfOrigin,
            CD_CompanyLogo,
            CD_Markings,
            CD_Marking,
            CD_MarkingName,
            CD_DesignationOfCertificateOrApproval,
            CD_IssueDate,
            CD_ExpiryDate,
            CD_MarkingFile,
            CD_MarkingAdditionalText,
            CD_ExplosionSafeties,
            CD_ExplosionSafety,
            CD_TypeOfApproval,
            CD_ApprovalAgencyTestingAgency,
            CD_TypeOfProtection,
            CD_RatedInsulationVoltage,
            CD_InstructionsControlDrawing,
            CD_SpecificConditionsForUse,
            CD_IncompleteDevice,
            CD_AmbientConditions,
            CD_DeviceCategory,
            CD_EquipmentProtectionLevel,
            CD_RegionalSpecificMarking,
            CD_ExplosionGroup,
            CD_MinimumAmbientTemperature,
            CD_MaxAmbientTemperature,
            CD_MaxSurfaceTemperatureForDustProof,
            CD_TemperatureClass,
            CD_ProcessConditions,
            CD_LowerLimitingValueOfProcessTemperature,
            CD_UpperLimitingValueOfProcessTemperature,
            CD_ExternalElectricalCircuit,
            CD_DesignationOfElectricalTerminal,
            CD_Characteristics,
            CD_Fisco,
            CD_TwoWISE,
            CD_SafetyRelatedPropertiesForPassiveBehaviour,
            CD_MaxInputPower,
            CD_MaxInputVoltage,
            CD_MaxInputCurrent,
            CD_MaxInternalCapacitance,
            CD_MaxInternalInductance,
            CD_SafetyRelatedPropertiesForActiveBehaviour,
            CD_MaxOutputPower,
            CD_MaxOutputVoltage,
            CD_MaxOutputCurrent,
            CD_MaxExternalCapacitance,
            CD_MaxExternalInductance,
            CD_MaxExternalInductanceResistanceRatio,
            CD_AssetSpecificProperties,
            CD_GuidelineSpecificProperties,
            CD_GuidelineForConformityDeclaration;

        public DigitalNameplateV20()
        {
            // info
            this.DomainInfo = "Digital Nameplate (IDTA) V2.0";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "DigitalNameplateV20.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(DigitalNameplateV20), useFieldNames: true);
        }
    }
}
