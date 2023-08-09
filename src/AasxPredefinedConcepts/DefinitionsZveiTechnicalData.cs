/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Reflection;
using Aas = AasCore.Aas3_0;

namespace AasxPredefinedConcepts
{
    public class DefinitionsZveiTechnicalData : AasxDefinitionBase
    {
        //
        // Constants
        //


        //
        // Concepts..
        //

        public DefinitionsZveiTechnicalData()
        {
            this._library = BuildLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "ZveiTechnicalData.json");
        }

        public class SetOfDefs
        {
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
                CD_ClassificationSystem,
                CD_SystemVersion,
                CD_ProductClass,
                CD_TechnicalProperties,
                CD_NonstandardizedProperty,
                CD_MainSection,
                CD_SubSection,
                CD_FurtherInformation,
                CD_TextStatement,
                CD_ValidDate;

            public SetOfDefs(AasxDefinitionBase bs)
            {
                this.SM_TechnicalData = bs.RetrieveReferable<Aas.Submodel>("SM_TechnicalData");

                this.CD_GeneralInformation = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_GeneralInformation");
                this.CD_ManufacturerName = bs.RetrieveReferable<Aas.ConceptDescription>("CD_ManufacturerName");
                this.CD_ManufacturerLogo = bs.RetrieveReferable<Aas.ConceptDescription>("CD_ManufacturerLogo");
                this.CD_ManufacturerProductDesignation = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_ManufacturerProductDesignation");
                this.CD_ManufacturerPartNumber = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_ManufacturerPartNumber");
                this.CD_ManufacturerOrderCode = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_ManufacturerOrderCode");
                this.CD_ProductImage = bs.RetrieveReferable<Aas.ConceptDescription>("CD_ProductImage");
                this.CD_ProductClassifications = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_ProductClassifications");
                this.CD_ProductClassificationItem = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_ProductClassificationItem");
                this.CD_ClassificationSystem = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_ClassificationSystem");
                this.CD_SystemVersion = bs.RetrieveReferable<Aas.ConceptDescription>("CD_SystemVersion");
                this.CD_ProductClass = bs.RetrieveReferable<Aas.ConceptDescription>("CD_ProductClass");
                this.CD_TechnicalProperties = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_TechnicalProperties");
                this.CD_NonstandardizedProperty = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_NonstandardizedProperty");
                this.CD_MainSection = bs.RetrieveReferable<Aas.ConceptDescription>("CD_MainSection");
                this.CD_SubSection = bs.RetrieveReferable<Aas.ConceptDescription>("CD_SubSection");
                this.CD_FurtherInformation = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_FurtherInformation");
                this.CD_TextStatement = bs.RetrieveReferable<Aas.ConceptDescription>("CD_TextStatement");
                this.CD_ValidDate = bs.RetrieveReferable<Aas.ConceptDescription>("CD_ValidDate");
            }

            public Aas.IReferable[] GetAllReferables()
            {
                return new Aas.IReferable[] {
                    SM_TechnicalData,
                    CD_GeneralInformation,
                    CD_ManufacturerName,
                    CD_ManufacturerLogo,
                    CD_ManufacturerProductDesignation,
                    CD_ManufacturerPartNumber,
                    CD_ManufacturerOrderCode,
                    CD_ProductImage,
                    CD_ProductClassifications,
                    CD_ProductClassificationItem,
                    CD_ClassificationSystem,
                    CD_SystemVersion,
                    CD_ProductClass,
                    CD_TechnicalProperties,
                    CD_NonstandardizedProperty,
                    CD_MainSection,
                    CD_SubSection,
                    CD_FurtherInformation,
                    CD_TextStatement,
                    CD_ValidDate
                };
            }
        }
    }
}
