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
    public class VDI2770v11 : AasxDefinitionBase
    {
        public static VDI2770v11 Static = new VDI2770v11();

        public const string Vdi2770Sys = "VDI2770:2020";

        public Aas.Submodel
            SM_ManufacturerDocumentation;

        public Aas.ConceptDescription
            CD_Document,
            CD_DocumentId,
            CD_DocumentDomainId,
            CD_DocumentIdValue,
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
            CD_SetDate,
            CD_StatusValue,
            CD_OrganizationName,
            CD_OrganizationOfficialName,
            CD_DigitalFile,
            CD_PreviewFile,
            CD_RefersTo,
            CD_BasedOn,
            CD_TranslationOf,
            CD_DocumentedEntity;

        public VDI2770v11()
        {
            // info
            this.DomainInfo = "Manufacturer Documentation (VDI2770) v1.1";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "VDI2770v11.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(VDI2770v11), useFieldNames: true);
        }
    }
}
