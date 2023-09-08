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
    /// Definitions of Submodel Generic Frame for Technical Data for Industrial Equipment
    /// in Manufacturing (IDTA 02003-1-2) from Aug 2022
    /// </summary>
    public class IdtaTechnicalDataV12 : AasxDefinitionBase
    {
        public static IdtaTechnicalDataV12 Static = new IdtaTechnicalDataV12();

        public Aas.Submodel
            SM_TechnicalData;

        public Aas.ConceptDescription
            CD_GeneralInformation,
            CD_ManufacturerName,
            CD_ManufacturerLogo,
            CD_ManufacturerProductDesignation,
            CD_ManufacturerArticleNumber,
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

        public IdtaTechnicalDataV12()
        {
            // info
            this.DomainInfo = "Generic Frame for Technical Data for Industrial Equipment (IDTA) V1.2";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "IdtaTechnicalDataV12.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(IdtaTechnicalDataV12), useFieldNames: true);
        }
    }
}
