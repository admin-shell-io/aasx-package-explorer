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
    public class AasEvents : AasxDefinitionBase
    {
        public static AasEvents Static = new AasEvents();

        public Aas.ConceptDescription
            CD_StructureChangeOutwards,
            CD_UpdateValueOutwards;

        public AasEvents()
        {
            // info
            this.DomainInfo = "AASX PackageExplorer - AAS Events";

            // definitons
            CD_StructureChangeOutwards = CreateSparseConceptDescription("en", "IRI",
                "StructureChangeOutwards",
                "https://admin-shell.io/tmp/AAS/Events/StructureChangeOutwards",
                @"Events emitted by the AAS if AAS elements are created, modified or deleted.");

            CD_UpdateValueOutwards = CreateSparseConceptDescription("en", "IRI",
                "UpdateValueOutwards",
                "https://admin-shell.io/tmp/AAS/Events/UpdateValueOutwards",
                @"Events emitted by the AAS if the value of an AAS element is changed.");

            // reflect
            AddEntriesByReflection(this.GetType(), useAttributes: false, useFieldNames: true);
        }
    }
}
