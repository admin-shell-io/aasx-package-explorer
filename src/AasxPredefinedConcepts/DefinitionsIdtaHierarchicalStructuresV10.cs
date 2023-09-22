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
    /// Definitions of Submodel Digital Nameplate 
    /// </summary>
    public class HierarchStructV10 : AasxDefinitionBase
    {
        public static HierarchStructV10 Static = new HierarchStructV10();

        public Aas.Submodel
            SM_HierarchicalStructures;

        public Aas.ConceptDescription
            CD_EntryNode,
            CD_Node,
            CD_SameAs,
            CD_IsPartOf,
            CD_HasPart,
            CD_BulkCount,
            CD_ArcheType;

        public HierarchStructV10()
        {
            // info
            this.DomainInfo = "Hierarchical Structures (IDTA) V1.0";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources."
                + "IdtaHierarchicalStructV10.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(HierarchStructV10), useFieldNames: true);
        }
    }
}
