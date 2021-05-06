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
using AdminShellNS;

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions for SmdExporter plugin. Mainly to provide semantic id for Submodel. 
    /// Preliminary works.
    /// </summary>
    public class SmdExporter : AasxDefinitionBase
    {
        public static SmdExporter Static = new SmdExporter();

        public AdminShell.SemanticId
            SEM_SmdExporterSubmodel;

        public AdminShell.ConceptDescription
            CD_Dummy;

        public SmdExporter()
        {
            // info
            this.DomainInfo = "Plugin SmdExporter";

            // Referable
            SEM_SmdExporterSubmodel = new AdminShell.SemanticId(
                AdminShell.Key.CreateNew(
                    type: "Submodel",
                    local: false,
                    idType: "IRI",
                    value: "http://admin-shell.io/aasx-package-explorer/plugins/SmdExporter/Submodel/1/0"));

            // dummy .. to be replaced later
            CD_Dummy = CreateSparseConceptDescription("en", "IRI",
                "Dummy",
                "http://admin-shell.io/aasx-package-explorer/plugins/SmdExporter/Dummy/1/0",
                @"TBD.");

            // reflect
            AddEntriesByReflection(this.GetType(), useAttributes: false, useFieldNames: true);
        }
    }
}
