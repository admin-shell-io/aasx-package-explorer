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

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions for Plotting plugin. Mainly to provide semantic id for Submodel. 
    /// Preliminary works.
    /// </summary>
    public class Plotting : AasxDefinitionBase
    {
        public static Plotting Static = new Plotting();

        public AasCore.Aas3_0_RC02.Reference
            SEM_PlottingSubmodel;

        public ConceptDescription
            CD_Dummy;

        public Plotting()
        {
            // info
            this.DomainInfo = "Plugin Plotting";

            // AasCore.Aas3_0_RC02.IReferable
            SEM_PlottingSubmodel = new AasCore.Aas3_0_RC02.Reference(ReferenceTypes.GlobalReference, new List<AasCore.Aas3_0_RC02.Key>() { new AasCore.Aas3_0_RC02.Key(AasCore.Aas3_0_RC02.KeyTypes.Submodel, "http://admin-shell.io/aasx-package-explorer/plugins/Plotting/Submodel/1/0") });


            // dummy .. to be replaced later
            CD_Dummy = CreateSparseConceptDescription("en", "IRI",
                "Dummy",
                "http://admin-shell.io/aasx-package-explorer/plugins/ImageMap/Dummy/1/0",
                @"TBD.");

            // reflect
            AddEntriesByReflection(this.GetType(), useAttributes: false, useFieldNames: true);
        }
    }
}
