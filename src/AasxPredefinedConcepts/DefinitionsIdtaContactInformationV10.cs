/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Stefan Erler

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
    /// Definitions of Submodel ContactInformation (orig. ZVEI, now IDTA still V1.0)
    /// </summary>
    public class IdtaContactInformationV10 : AasxDefinitionBase
    {
        public static IdtaContactInformationV10 Static = new IdtaContactInformationV10();

        public Aas.Submodel
            SM_ContactInformations;

        public
            Aas.ConceptDescription
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
            CD_FurtherDetailsOfContact;

        public IdtaContactInformationV10()
        {
            // info
            this.DomainInfo = "Contact Information (IDTA) V1.0";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(),
                "AasxPredefinedConcepts.Resources." + "IdtaContactInformationV10.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(IdtaContactInformationV10), useFieldNames: true);
        }
    }
}
