using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
            this.theLibrary = BuildLibrary(Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "ZveiTechnicalData.json");
        }

        public class SetOfDefs
        {
            public AdminShell.Submodel
                SM_TechnicalData;

            public AdminShell.ConceptDescription
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
                this.SM_TechnicalData = bs.RetrieveReferable<AdminShell.Submodel>("SM_TechnicalData");

                this.CD_GeneralInformation = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_GeneralInformation");
                this.CD_ManufacturerName = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_ManufacturerName");
                this.CD_ManufacturerLogo = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_ManufacturerLogo");
                this.CD_ManufacturerProductDesignation = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_ManufacturerProductDesignation");
                this.CD_ManufacturerPartNumber = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_ManufacturerPartNumber");
                this.CD_ManufacturerOrderCode = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_ManufacturerOrderCode");
                this.CD_ProductImage = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_ProductImage");
                this.CD_ProductClassifications = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_ProductClassifications");
                this.CD_ProductClassificationItem = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_ProductClassificationItem");
                this.CD_ClassificationSystem = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_ClassificationSystem");
                this.CD_SystemVersion = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_SystemVersion");
                this.CD_ProductClass = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_ProductClass");
                this.CD_TechnicalProperties = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_TechnicalProperties");
                this.CD_NonstandardizedProperty = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_NonstandardizedProperty");
                this.CD_MainSection = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_MainSection");
                this.CD_SubSection = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_SubSection");
                this.CD_FurtherInformation = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_FurtherInformation");
                this.CD_TextStatement = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_TextStatement");
                this.CD_ValidDate = bs.RetrieveReferable<AdminShell.ConceptDescription>("CD_ValidDate");
            }

            public AdminShell.Referable[] GetAllReferables()
            {
                return new AdminShell.Referable[] {
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
