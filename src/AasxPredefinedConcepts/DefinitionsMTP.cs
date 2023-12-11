/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Collections.Generic;
using Aas = AasCore.Aas3_0;

// reSharper disable UnusedType.Global
// reSharper disable ClassNeverInstantiated.Global

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions for MTP. Somehow preliminary, to be replaced by "full" JSON definitions
    /// </summary>
    public class DefinitionsMTP
    {
        /// <summary>
        /// Definitions for MTP. Somehow preliminary, to be replaced by "full" JSON definitions
        /// </summary>
        public class ModuleTypePackage : AasxDefinitionBase
        {
            public Aas.Reference
                SEM_MtpSubmodel,
                SEM_MtpInstanceSubmodel;

            public Aas.ConceptDescription
                CD_MtpTypeSubmodel,
                CD_SourceList,
                CD_SourceOpcUaServer,
                CD_Endpoint,
                CD_MtpFile,
                CD_IdentifierRenaming,
                CD_NamespaceRenaming,
                CD_RenamingOldText,
                CD_RenamingNewText;

            public ModuleTypePackage()
            {
                // info
                this.DomainInfo = "Module Type Package (MTP)";

                // IReferable
                SEM_MtpSubmodel = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>() { new Aas.Key(Aas.KeyTypes.Submodel, "http://www.admin-shell.io/mtp/v1/submodel") });

                SEM_MtpInstanceSubmodel = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>() { new Aas.Key(Aas.KeyTypes.Submodel, "http://www.admin-shell.io/mtp/v1/mtp-instance-submodel") });

                CD_MtpTypeSubmodel = CreateSparseConceptDescription("en", "IRI",
                    "MtpTypeSubmodel",
                    "http://www.admin-shell.io/mtp/v1/New/MtpTypeSubmodel",
                    @"Direct Reference to MTP Type Submodel.");

                CD_SourceList = CreateSparseConceptDescription("en", "IRI",
                    "SourceList",
                    "http://www.admin-shell.io/mtp/v1/MTPSUCLib/CommunicationSet/SourceList",
                    @"Collects source of process data for MTP.");

                CD_SourceOpcUaServer = CreateSparseConceptDescription("en", "IRI",
                    "SourceOpcUaServer",
                    "http://www.admin-shell.io/mtp/v1/MTPCommunicationSUCLib/ServerAssembly/OPCUAServer",
                    "Holds infomation on a single data source, which is an OPC UA server.");

                CD_Endpoint = CreateSparseConceptDescription("en", "IRI",
                    "Endpoint",
                    "http://www.admin-shell.io/mtp/v1/MTPCommunicationSUCLib/ServerAssembly/OPCUAServer/Endpoint",
                    "URL of OPC UA server for data source.");

                CD_MtpFile = CreateSparseConceptDescription("en", "IRI",
                    "MtpFile",
                    "http://www.admin-shell.io/mtp/v1/MTPSUCLib/ModuleTypePackage",
                    "Specifies a File, which contains MTP information in MTP/ AutmationML format." +
                    "File may be zipped.");

                CD_IdentifierRenaming = CreateSparseConceptDescription("en", "IRI",
                    "IdentifierRenaming",
                    "http://www.admin-shell.io/mtp/v1/New/IdentifierRenaming",
                    "Specifies a renaming of OPC UA identifiers for nodes. Designates a SubmodelElementCollection " +
                    "containing two Properties for OldText and NewText for string replacement.");

                CD_NamespaceRenaming = CreateSparseConceptDescription("en", "IRI",
                    "NamespaceRenaming",
                    "http://www.admin-shell.io/mtp/v1/New/NamespaceRenaming",
                    "Specifies a renaming of OPC UA namespaces for nodes. Designates a SubmodelElementCollection " +
                    "containing two Properties for OldText and NewText for string replacement.");

                CD_RenamingOldText = CreateSparseConceptDescription("en", "IRI",
                    "OldText",
                    "http://www.admin-shell.io/mtp/v1/New/RenamingOldText",
                    "Within a renaming of OPC UA identifiers or namespaces, designates the text which shall be " +
                    "replaced.");

                CD_RenamingNewText = CreateSparseConceptDescription("en", "IRI",
                    "NewText",
                    "http://www.admin-shell.io/mtp/v1/New/RenamingNewText",
                    "Within a renaming of OPC UA identifiers or namespaces, designates the new text, which shall be " +
                    "substituted.");

                // reflect
                AddEntriesByReflection(this.GetType(), useAttributes: false, useFieldNames: true);
            }
        }
    }
}
