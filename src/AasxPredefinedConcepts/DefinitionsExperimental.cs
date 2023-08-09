/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using Aas = AasCore.Aas3_0;

// reSharper disable UnusedType.Global
// reSharper disable ClassNeverInstantiated.Global

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// This class holds definitions, which are preliminary, experimental, partial, not stabilized.
    /// Aim is to provide a single source of definitions, which can be used by implementations such as
    /// plugins, to develop some functionalities.
    /// Later, these definitions shall be replaced by substantial definitions, e.g. imported from JSON.
    /// </summary>
    public class DefinitionsExperimental
    {
        /// <summary>
        /// This class holds definitions from the Submodel Template Specification: 
        /// Identifiers for Interoperable relationships  
        /// for the use of Composite Components of Manufacturing Equipment
        /// </summary>
        public class InteropRelations : AasxDefinitionBase
        {
            public Aas.ConceptDescription
                CD_FileToNavigateElement,
                CD_FileToEntity,
                CD_IsPartOfForBOM,
                CD_EntityOfElectricalEngineering,
                CD_ElectricSinglePoleConnection,
                CD_EntityOfFluidicEngineering,
                CD_ConnectorTubePipe,
                CD_TubePipeConnection,
                CD_TubePipeConnectionPneumatic,
                CD_TubePipeConnectionHydraulic;

            public InteropRelations()
            {
                // info
                this.DomainInfo = "Composite assets, interoperable Relations (experimental)";

                // IReferable
                CD_FileToNavigateElement = CreateSparseConceptDescription("en", "IRI",
                    "FileToNavigateElement",
                    "http://admin-shell.io/sandbox/CompositeComponent/General/FileToNavigateElement/1/0",
                    @"Links fragments of a File to an arbitrary element in an AAS. A environment for maintaining 
                    AAS information shall be commanded to navigate to this element.");

                CD_FileToEntity = CreateSparseConceptDescription("en", "IRI",
                    "FileToEntity",
                    "http://admin-shell.io/sandbox/CompositeComponent/General/FileToEntity/1/0",
                    "Links fragments of a File to an Entity element in the AAS.");

                CD_IsPartOfForBOM = CreateSparseConceptDescription("en", "IRI",
                    "IsPartOfForBOM",
                    "http://admin-shell.io/sandbox/CompositeComponent/General/IsPartOfForBOM/1/0",
                    "States a general is-part-of-relationship, which is not further constrained, but used for " +
                    "organizing the BOM in multiple manageable parts.");

                CD_EntityOfElectricalEngineering = CreateSparseConceptDescription("en", "IRI",
                    "EntityOfElectricalEngineering",
                    "http://admin-shell.io/sandbox/CompositeComponent/Electrical/EntityOfElectricalEngineering/1/0",
                    @"States, that the Entity is part of the model for Electrical engineering.
                    Note: Any Aas.Entity with a certain meaning to the Electrical engineering might be 
                    declared as EntityOfElectricalEngineering.");

                CD_ElectricSinglePoleConnection = CreateSparseConceptDescription("en", "IRI",
                    "SinglePoleConnection",
                    "http://admin-shell.io/sandbox/CompositeComponent/Electrical/SinglePoleConnection/1/0",
                    "States, that there is a single pole connection between two Electrical Entities. Without loss " +
                    "of generality, the connection points first/ second will share the same electrical properties.");

                CD_EntityOfFluidicEngineering = CreateSparseConceptDescription("en", "IRI",
                    "EntityOfFluidicEngineering",
                    "http://admin-shell.io/sandbox/CompositeComponent/Fluidic/EntityOfFluidicEngineering/1/0",
                    "States, that the Entity is part of the model for Fluidic engineering");

                CD_ConnectorTubePipe = CreateSparseConceptDescription("en", "IRI",
                    "ConnectorTubePipe",
                    "http://admin-shell.io/sandbox/CompositeComponent/Fluidic/ConnectorTubePipe/1/0",
                    "Establishes a Property, which represents an connector (port, position for fitting) as a " +
                    "specific feature of an EntityOfFluidicEngineering.");

                CD_TubePipeConnection = CreateSparseConceptDescription("en", "IRI",
                    "TubePipeConnection",
                    "http://admin-shell.io/sandbox/CompositeComponent/Fluidic/TubePipeConnection/1/0",
                    "States, that there is a fludic connection between two Electrical Entities. Without loss " +
                    "of generality, the connection points first/ second will share the same material flow.");

                CD_TubePipeConnectionPneumatic = CreateSparseConceptDescription("en", "IRI",
                    "TubePipeConnectionPneumatic",
                    "http://admin-shell.io/sandbox/CompositeComponent/Fluidic/TubePipeConnectionPneumatic/1/0",
                    "States, that there is a pneumatic connection between two Electrical Entities.",
                    isCaseOf: CD_TubePipeConnection.GetCdReference());

                CD_TubePipeConnectionHydraulic = CreateSparseConceptDescription("en", "IRI",
                    "TubePipeConnectionHydraulic",
                    "http://admin-shell.io/sandbox/CompositeComponent/Fluidic/TubePipeConnectionHydraulic/1/0",
                    "States, that there is a hydraulic connection between two Electrical Entities.",
                    isCaseOf: CD_TubePipeConnection.GetReference());

                // reflect
                AddEntriesByReflection(this.GetType(), useAttributes: false, useFieldNames: true);
            }
        }
    }
}
