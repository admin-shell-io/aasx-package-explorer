/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0_RC02;
using AdminShellNS;

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

        public ConceptDescription
            CD_StructureChangeOutwards,
            CD_UpdateValueOutwards;

        public AasEvents()
        {
            // info
            this.DomainInfo = "AAS Events";

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
