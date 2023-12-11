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
    /// Definitions of Submodel VDI2770 according to new alignment with VDI
    /// </summary>
    public class ZveiTechnicalDataV11 : AasxDefinitionBase
    {
        public static ZveiTechnicalDataV11 Static = new ZveiTechnicalDataV11();

        public Aas.Submodel
            SM_TechnicalData;

        public Aas.ConceptDescription
            CD_GeneralInformation,
            CD_ManufacturerName,
            CD_ManufacturerLogo,
            CD_ManufacturerProductDesignation,
            CD_ManufacturerPartNumber,
            CD_ManufacturerOrderCode,
            CD_ProductImage,
            CD_ProductClassifications,
            CD_ProductClassificationItem,
            CD_ProductClassificationSystem,
            CD_ClassificationSystemVersion,
            CD_ProductClassId,
            CD_TechnicalProperties,
            CD_SemanticIdNotAvailable,
            CD_MainSection,
            CD_SubSection,
            CD_FurtherInformation,
            CD_TextStatement,
            CD_ValidDate;

        public ZveiTechnicalDataV11()
        {
            // info
            this.DomainInfo = "Generic Frame for Technical Data for Industrial Equipment (ZVEI) V1.1";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "ZveiTechnicalDataV11.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(ZveiTechnicalDataV11), useFieldNames: true);
        }
    }
}
