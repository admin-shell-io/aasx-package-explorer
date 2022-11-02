/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

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
    /// Definitions for Capability plugin.
    /// </summary>
    public class CapabilitySet : AasxDefinitionBase
    {
        public static CapabilitySet Static = new CapabilitySet();

        public AdminShell.SemanticId
            SM_Capabilities;

        public AdminShell.ConceptDescription
            CD_CapabilitySet,
            CD_CapabilityContainer,
            CD_Capability,
            CD_Comment,
            CD_CapabilityRelationships,
            CD_realizedBy,
            CD_ComposedOfContainer,
            CD_ComposedOfRelationship,
            CD_ConditionContainer,
            CD_CapabilityCondition,
            CD_PropertySet,
            CD_PropertyContainer,
            CD_Property,
            CD_Comment_2,
            CD_PropertyRelationships,
            CD_realizedBy_2;

        public CapabilitySet()
        {
            // info
            this.DomainInfo = "Tools for CapabilitySet";

            // Referable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxCapabilitySearch.Resources." + "CapabilitySet.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(CapabilitySet), useFieldNames: true);
        }
    }
}
