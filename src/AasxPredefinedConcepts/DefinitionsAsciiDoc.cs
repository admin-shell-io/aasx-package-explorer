/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Collections.Generic;
using Aas = AasCore.Aas3_0;

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions for ImageMap plugin. Somehow preliminary, to be replaced by "full" JSON definitions
    /// </summary>
    public class AsciiDoc : AasxDefinitionBase
    {
        public static AsciiDoc Static = new AsciiDoc();

        public Aas.Reference
            SEM_AsciiDocSubmodel;

        public Aas.ConceptDescription
            CD_TextBlock,
            CD_CoverPage,
            CD_Heading1,
            CD_Heading2,
            CD_Heading3,
            CD_ImageFile,
            CD_GenerateUml,
            CD_GenerateTables;

        public AsciiDoc()
        {
            // info
            this.DomainInfo = "AASX PackageExplorer - Plugin exporting of AsciiDoc formats";

            // IReferable
            SEM_AsciiDocSubmodel = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>() {
                new Aas.Key(Aas.KeyTypes.Submodel,
                "http://admin-shell.io/aasx-package-explorer/functions/asciidoc/1/0") });

            CD_TextBlock = CreateSparseConceptDescription("en", "IRI",
                "TextBlock",
                "http://admin-shell.io/aasx-package-explorer/functions/asciidoc/textblock/1/0",
                @"Generic text block to be added to the document.");

            CD_CoverPage = CreateSparseConceptDescription("en", "IRI",
                "CoverPage",
                "http://admin-shell.io/aasx-package-explorer/functions/asciidoc/coverpage/1/0",
                @"Contents and definitions for the start of the document.");

            CD_Heading1 = CreateSparseConceptDescription("en", "IRI",
                "Heading1",
                "http://admin-shell.io/aasx-package-explorer/functions/asciidoc/heading1/1/0",
                @"Heading with level 1 in AsciiDoc. That is: main chapter.");

            CD_Heading2 = CreateSparseConceptDescription("en", "IRI",
                "Heading2",
                "http://admin-shell.io/aasx-package-explorer/functions/asciidoc/heading2/1/0",
                @"Heading with level 2 in AsciiDoc. That is: sub section.");

            CD_Heading3 = CreateSparseConceptDescription("en", "IRI",
                "Heading3",
                "http://admin-shell.io/aasx-package-explorer/functions/asciidoc/heading3/1/0",
                @"Heading with level 3 in AsciiDoc. That is: sub sub section.");

            CD_ImageFile = CreateSparseConceptDescription("en", "IRI",
                "ImageFile",
                "http://admin-shell.io/aasx-package-explorer/functions/asciidoc/imagefile/1/0",
                @"File element linking to a (supplemental) file and embedding link.");

            CD_GenerateUml = CreateSparseConceptDescription("en", "IRI",
                "GenerateUml",
                "http://admin-shell.io/aasx-package-explorer/functions/asciidoc/generate-uml/1/0",
                @"Reference element linking to set of AAS elements to create PlantUML for.");

            CD_GenerateTables = CreateSparseConceptDescription("en", "IRI",
                "GenerateTables",
                "http://admin-shell.io/aasx-package-explorer/functions/asciidoc/generate-tables/1/0",
                @"Reference element linking to set of AAS elements to create export tabels for.");

            // reflect
            AddEntriesByReflection(this.GetType(), useAttributes: false, useFieldNames: true);
        }
    }
}
