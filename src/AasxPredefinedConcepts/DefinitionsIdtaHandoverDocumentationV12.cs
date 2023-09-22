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
    public class IdtaHandoverDocumentationV12 : AasxDefinitionBase
    {
        public static IdtaHandoverDocumentationV12 Static = new IdtaHandoverDocumentationV12();

        public Aas.Submodel
            SM_HandoverDocumentation;

        public Aas.ConceptDescription
            CD_numberOfDocuments,
            CD_Document,
            CD_DocumentId,
            CD_DocumentDomainId,
            CD_ValueId,
            CD_IsPrimary,
            CD_DocumentClassification,
            CD_ClassId,
            CD_ClassName,
            CD_ClassificationSystem,
            CD_DocumentVersion,
            CD_Language,
            CD_DocumentVersionId,
            CD_Title,
            CD_SubTitle,
            CD_Summary,
            CD_KeyWords,
            CD_StatusSetDate,
            CD_StatusValue,
            CD_OrganizationName,
            CD_OrganizationOfficialName,
            CD_RefersTo,
            CD_BasedOn,
            CD_TranslationOf,
            CD_DigitalFile,
            CD_PreviewFile,
            CD_DocumentedEntity;

        public IdtaHandoverDocumentationV12()
        {
            // info
            this.DomainInfo = "Handover Documentation (IDTA) V1.2";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(),
                "AasxPredefinedConcepts.Resources." + "IdtaHandoverDocumentationV12.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(IdtaHandoverDocumentationV12), useFieldNames: true);
        }
    }
}
