/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Aas = AasCore.Aas3_0;

// reSharper disable UnusedType.Global
// reSharper disable ClassNeverInstantiated.Global

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// This class holds definitions, which are preliminary, experimental, partial, not stabilized.
    /// The definitions aim as the definition and handling of Events.
    /// The should end up finally in a AASiD specification.
    /// </summary>
    public class SmtAdditions : AasxDefinitionBase
    {
        public static SmtAdditions Static = new SmtAdditions();

        public Aas.ConceptDescription
			CD_ContactInfoPreviewFile;

        public Aas.IKey
            Key_SmtDropinDefinition,
            Key_SmtDropinUse;

        public SmtAdditions()
        {
            // info
            this.DomainInfo = "AASX PackageExplorer - Submodel templates - Additional (non standard) information";

            // definitons
            CD_ContactInfoPreviewFile = CreateSparseConceptDescription("en", "IRI",
				"ContactInfoPreviewFile",
				"https://admin-shell.io/tmp/SMT/Additions/ContactInformation/PreviewFile",
				@"Provides a preview image of the contact, e.g. an image of a person or a symbolic pictuture, in a commonly used image format and low resolution.");

            // SMT dropins
            Key_SmtDropinDefinition = new Aas.Key(Aas.KeyTypes.GlobalReference,
                "https://admin-shell.io/smt-dropin/smt-dropin-definition/1/0");

            Key_SmtDropinUse = new Aas.Key(Aas.KeyTypes.GlobalReference,
                "https://admin-shell.io/smt-dropin/smt-dropin-use/1/0");


            // reflect
            AddEntriesByReflection(this.GetType(), useAttributes: false, useFieldNames: true);
        }
    }
}
