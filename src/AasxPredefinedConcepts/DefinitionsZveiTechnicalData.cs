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
using AasCore.Aas3_0_RC02;
using AdminShellNS;

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
            this.theLibrary = BuildLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "ZveiTechnicalData.json");
        }

        public class SetOfDefs
        {
            public Submodel
                SM_TechnicalData;

            public ConceptDescription
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
                this.SM_TechnicalData = bs.RetrieveReferable<Submodel>("SM_TechnicalData");

                this.CD_GeneralInformation = bs.RetrieveReferable<ConceptDescription>(
                    "CD_GeneralInformation");
                this.CD_ManufacturerName = bs.RetrieveReferable<ConceptDescription>("CD_ManufacturerName");
                this.CD_ManufacturerLogo = bs.RetrieveReferable<ConceptDescription>("CD_ManufacturerLogo");
                this.CD_ManufacturerProductDesignation = bs.RetrieveReferable<ConceptDescription>(
                    "CD_ManufacturerProductDesignation");
                this.CD_ManufacturerPartNumber = bs.RetrieveReferable<ConceptDescription>(
                    "CD_ManufacturerPartNumber");
                this.CD_ManufacturerOrderCode = bs.RetrieveReferable<ConceptDescription>(
                    "CD_ManufacturerOrderCode");
                this.CD_ProductImage = bs.RetrieveReferable<ConceptDescription>("CD_ProductImage");
                this.CD_ProductClassifications = bs.RetrieveReferable<ConceptDescription>(
                    "CD_ProductClassifications");
                this.CD_ProductClassificationItem = bs.RetrieveReferable<ConceptDescription>(
                    "CD_ProductClassificationItem");
                this.CD_ClassificationSystem = bs.RetrieveReferable<ConceptDescription>(
                    "CD_ClassificationSystem");
                this.CD_SystemVersion = bs.RetrieveReferable<ConceptDescription>("CD_SystemVersion");
                this.CD_ProductClass = bs.RetrieveReferable<ConceptDescription>("CD_ProductClass");
                this.CD_TechnicalProperties = bs.RetrieveReferable<ConceptDescription>(
                    "CD_TechnicalProperties");
                this.CD_NonstandardizedProperty = bs.RetrieveReferable<ConceptDescription>(
                    "CD_NonstandardizedProperty");
                this.CD_MainSection = bs.RetrieveReferable<ConceptDescription>("CD_MainSection");
                this.CD_SubSection = bs.RetrieveReferable<ConceptDescription>("CD_SubSection");
                this.CD_FurtherInformation = bs.RetrieveReferable<ConceptDescription>(
                    "CD_FurtherInformation");
                this.CD_TextStatement = bs.RetrieveReferable<ConceptDescription>("CD_TextStatement");
                this.CD_ValidDate = bs.RetrieveReferable<ConceptDescription>("CD_ValidDate");
            }

            public IReferable[] GetAllReferables()
            {
                return new IReferable[] {
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
