using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions of Submodel VDI2770 according to new alignment with VDI
    /// </summary>
    public class VDI2770v11 : AasxDefinitionBase
    {
        public AdminShell.Submodel
            SM_ManufacturerDocumentation;

        public AdminShell.ConceptDescription
            CD_Document,
            CD_DocumentIdDomain,
            CD_DocumentDomainId,
            CD_DocumentId,
            CD_DocumentClassification,
            CD_DocumentClassId,
            CD_DocumentClassName,
            CD_DocumentClassificationSystem,
            CD_DocumentVersion,
            CD_Language,
            CD_DocumentVersionIdValue,
            CD_Title,
            CD_Summary,
            CD_KeyWords,
            CD_Date,
            CD_LifeCycleStatusValue,
            CD_Role,
            CD_OrganizationName,
            CD_OrganizationOfficialName,
            CD_DigitalFile,
            CD_PreviewFile,
            CD_Comment,
            CD_DocumentedEntity,
            CD_RefersTo,
            CD_BasedOn,
            CD_Affecting,
            CD_TranslationOf,
            CD_Entity;

        public static VDI2770v11 Static = null;

        static VDI2770v11()
        {
            Static = new VDI2770v11();
            Static.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "VDI2770v11.json");
            Static.RetrieveEntriesByReflection(typeof(VDI2770v11), useFieldNames: true);
        }
    }
}
