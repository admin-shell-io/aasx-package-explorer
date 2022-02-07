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
            public AdminShell.SemanticId
                SEM_MtpSubmodel,
                SEM_MtpInstanceSubmodel;

            public AdminShell.ConceptDescription
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

                // Referable
                SEM_MtpSubmodel = new AdminShell.SemanticId(
                    AdminShell.Key.CreateNew(
                        type: "Submodel",
                        local: false,
                        idType: "IRI",
                        value: "https://admin-shell.io/vdi/2658/1/0/MTPSubmodel"));

                SEM_MtpInstanceSubmodel = new AdminShell.SemanticId(
                    AdminShell.Key.CreateNew(
                        type: "Submodel",
                        local: false,
                        idType: "IRI",
                        value: "https://admin-shell.io/vdi/2658/1/0/PEASubmodel"));

                CD_MtpTypeSubmodel = CreateSparseConceptDescription("en", "IRI",
                    "MtpTypeSubmodel",
                    "https://admin-shell.io/vdi/2658/1/0/New/MtpTypeSubmodel",
                    @"Direct Reference to MTP Type Submodel.");

                CD_SourceList = CreateSparseConceptDescription("en", "IRI",
                    "SourceList",
                    "https://admin-shell.io/vdi/2658/1/0/MTPSUCLib/CommunicationSet/SourceList",
                    @"Collects source of process data for MTP.");

                CD_SourceOpcUaServer = CreateSparseConceptDescription("en", "IRI",
                    "SourceOpcUaServer",
                    "https://admin-shell.io/vdi/2658/1/0/MTPCommunicationSUCLib/ServerAssembly/OPCUAServer",
                    "Holds infomation on a single data source, which is an OPC UA server.");

                CD_Endpoint = CreateSparseConceptDescription("en", "IRI",
                    "Endpoint",
                    "https://admin-shell.io/vdi/2658/1/0/MTPCommunicationSUCLib/ServerAssembly/OPCUAServer/Endpoint",
                    "URL of OPC UA server for data source.");

                CD_MtpFile = CreateSparseConceptDescription("en", "IRI",
                    "MtpFile",
                    "https://admin-shell.io/vdi/2658/1/0/MTPSUCLib/ModuleTypePackage",
                    "Specifies a File, which contains MTP information in MTP/ AutmationML format." +
                    "File may be zipped.");

                CD_IdentifierRenaming = CreateSparseConceptDescription("en", "IRI",
                    "IdentifierRenaming",
                    "https://admin-shell.io/vdi/2658/1/0/New/IdentifierRenaming",
                    "Specifies a renaming of OPC UA identifiers for nodes. Designates a SubmodelElementCollection " +
                    "containing two Properties for OldText and NewText for string replacement.");

                CD_NamespaceRenaming = CreateSparseConceptDescription("en", "IRI",
                    "NamespaceRenaming",
                    "https://admin-shell.io/vdi/2658/1/0/New/NamespaceRenaming",
                    "Specifies a renaming of OPC UA namespaces for nodes. Designates a SubmodelElementCollection " +
                    "containing two Properties for OldText and NewText for string replacement.");

                CD_RenamingOldText = CreateSparseConceptDescription("en", "IRI",
                    "OldText",
                    "https://admin-shell.io/vdi/2658/1/0/New/RenamingOldText",
                    "Within a renaming of OPC UA identifiers or namespaces, designates the text which shall be " +
                    "replaced.");

                CD_RenamingNewText = CreateSparseConceptDescription("en", "IRI",
                    "NewText",
                    "https://admin-shell.io/vdi/2658/1/0/New/RenamingNewText",
                    "Within a renaming of OPC UA identifiers or namespaces, designates the new text, which shall be " +
                    "substituted.");

                // reflect
                AddEntriesByReflection(this.GetType(), useAttributes: false, useFieldNames: true);
            }
        }
    }
}
