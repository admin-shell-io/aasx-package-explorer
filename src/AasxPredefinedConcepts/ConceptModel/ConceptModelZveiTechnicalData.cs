/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using Aas = AasCore.Aas3_0;

namespace AasxPredefinedConcepts.ConceptModel
{
    /// <summary>
    /// This class integrates/ abstracts the concept definitions of multiple versions
    /// of ZVEI TechnicalData
    /// </summary>
    public class ConceptModelZveiTechnicalData
    {
        public enum Version { Unknown, V1_0, V1_1, V1_2 }

        public Version ActiveVersion = Version.Unknown;

        public Aas.Submodel
            SM_TechnicalData;

        public Aas.ConceptDescription
            CD_GeneralInformation,
            CD_ManufacturerName,
            CD_ManufacturerLogo,
            CD_ManufacturerProductDesignation,
            CD_ManufacturerPartNumber,
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

        public ConceptModelZveiTechnicalData(Aas.Submodel sm)
        {
            InitFromSubmodel(sm);
        }

        public void InitFromVersion(Version ver)
        {
            //
            // V1.0
            //

            if (ver == Version.V1_0)
            {
                var defsV10 = new AasxPredefinedConcepts.DefinitionsZveiTechnicalData.SetOfDefs(
                        new AasxPredefinedConcepts.DefinitionsZveiTechnicalData());

                ActiveVersion = Version.V1_0;

                SM_TechnicalData = defsV10.SM_TechnicalData;

                CD_GeneralInformation = defsV10.CD_GeneralInformation;
                CD_ManufacturerName = defsV10.CD_ManufacturerName;
                CD_ManufacturerLogo = defsV10.CD_ManufacturerLogo;
                CD_ManufacturerProductDesignation = defsV10.CD_ManufacturerProductDesignation;
                CD_ManufacturerPartNumber = defsV10.CD_ManufacturerPartNumber;
                CD_ManufacturerOrderCode = defsV10.CD_ManufacturerOrderCode;
                CD_ProductImage = defsV10.CD_ProductImage;
                CD_ProductClassifications = defsV10.CD_ProductClassifications;
                CD_ProductClassificationItem = defsV10.CD_ProductClassificationItem;
                CD_ProductClassificationSystem = defsV10.CD_ClassificationSystem;
                CD_ClassificationSystemVersion = defsV10.CD_SystemVersion;
                CD_ProductClassId = defsV10.CD_ProductClass;
                CD_TechnicalProperties = defsV10.CD_TechnicalProperties;
                CD_SemanticIdNotAvailable = defsV10.CD_NonstandardizedProperty;
                CD_MainSection = defsV10.CD_MainSection;
                CD_SubSection = defsV10.CD_SubSection;
                CD_FurtherInformation = defsV10.CD_FurtherInformation;
                CD_TextStatement = defsV10.CD_TextStatement;
                CD_ValidDate = defsV10.CD_ValidDate;
            }

            //
            // V1.1
            //

            if (ver == Version.V1_1)
            {
                var defsV11 = AasxPredefinedConcepts.ZveiTechnicalDataV11.Static;

                ActiveVersion = Version.V1_1;

                SM_TechnicalData = defsV11.SM_TechnicalData;

                CD_GeneralInformation = defsV11.CD_GeneralInformation;
                CD_ManufacturerName = defsV11.CD_ManufacturerName;
                CD_ManufacturerLogo = defsV11.CD_ManufacturerLogo;
                CD_ManufacturerProductDesignation = defsV11.CD_ManufacturerProductDesignation;
                CD_ManufacturerPartNumber = defsV11.CD_ManufacturerPartNumber;
                CD_ManufacturerOrderCode = defsV11.CD_ManufacturerOrderCode;
                CD_ProductImage = defsV11.CD_ProductImage;
                CD_ProductClassifications = defsV11.CD_ProductClassifications;
                CD_ProductClassificationItem = defsV11.CD_ProductClassificationItem;
                CD_ProductClassificationSystem = defsV11.CD_ProductClassificationSystem;
                CD_ClassificationSystemVersion = defsV11.CD_ClassificationSystemVersion;
                CD_ProductClassId = defsV11.CD_ProductClassId;
                CD_TechnicalProperties = defsV11.CD_TechnicalProperties;
                CD_SemanticIdNotAvailable = defsV11.CD_SemanticIdNotAvailable;
                CD_MainSection = defsV11.CD_MainSection;
                CD_SubSection = defsV11.CD_SubSection;
                CD_FurtherInformation = defsV11.CD_FurtherInformation;
                CD_TextStatement = defsV11.CD_TextStatement;
                CD_ValidDate = defsV11.CD_ValidDate;
            }

            //
            // V1.2
            //

            if (ver == Version.V1_2)
            {
                var defsV12 = AasxPredefinedConcepts.IdtaTechnicalDataV12.Static;

                ActiveVersion = Version.V1_2;

                SM_TechnicalData = defsV12.SM_TechnicalData;

                CD_GeneralInformation = defsV12.CD_GeneralInformation;
                CD_ManufacturerName = defsV12.CD_ManufacturerName;
                CD_ManufacturerLogo = defsV12.CD_ManufacturerLogo;
                CD_ManufacturerProductDesignation = defsV12.CD_ManufacturerProductDesignation;
                CD_ManufacturerArticleNumber = defsV12.CD_ManufacturerArticleNumber;
                CD_ManufacturerOrderCode = defsV12.CD_ManufacturerOrderCode;
                CD_ProductImage = defsV12.CD_ProductImage;
                CD_ProductClassifications = defsV12.CD_ProductClassifications;
                CD_ProductClassificationItem = defsV12.CD_ProductClassificationItem;
                CD_ProductClassificationSystem = defsV12.CD_ProductClassificationSystem;
                CD_ClassificationSystemVersion = defsV12.CD_ClassificationSystemVersion;
                CD_ProductClassId = defsV12.CD_ProductClassId;
                CD_TechnicalProperties = defsV12.CD_TechnicalProperties;
                CD_SemanticIdNotAvailable = defsV12.CD_SemanticIdNotAvailable;
                CD_MainSection = defsV12.CD_MainSection;
                CD_SubSection = defsV12.CD_SubSection;
                CD_FurtherInformation = defsV12.CD_FurtherInformation;
                CD_TextStatement = defsV12.CD_TextStatement;
                CD_ValidDate = defsV12.CD_ValidDate;
            }
        }

        public void InitFromSubmodel(Aas.Submodel sm)
        {
            var defsV10 = new AasxPredefinedConcepts.DefinitionsZveiTechnicalData.SetOfDefs(
                    new AasxPredefinedConcepts.DefinitionsZveiTechnicalData());
            if (sm.SemanticId.MatchesExactlyOneKey(
                    defsV10.SM_TechnicalData.SemanticId.GetAsExactlyOneKey(), MatchMode.Relaxed))
                InitFromVersion(Version.V1_0);

            var defsV11 = AasxPredefinedConcepts.ZveiTechnicalDataV11.Static;
            if (sm.SemanticId.MatchesExactlyOneKey(
                    defsV11.SM_TechnicalData.SemanticId.GetAsExactlyOneKey(), MatchMode.Relaxed))
                InitFromVersion(Version.V1_1);

            var defsV12 = AasxPredefinedConcepts.IdtaTechnicalDataV12.Static;
            if (sm.SemanticId.MatchesExactlyOneKey(
                    defsV12.SM_TechnicalData.SemanticId.GetAsExactlyOneKey(), MatchMode.Relaxed))
                InitFromVersion(Version.V1_2);
        }
    }
}
