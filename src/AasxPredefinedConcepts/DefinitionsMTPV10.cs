/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Reflection;
using AdminShellNS;

// ReSharper disable UnassignedField.Global
// (working by reflection)

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions of Submodel VDI2770 according to new alignment with VDI
    /// </summary>
    public class MTPV10 : AasxDefinitionBase
    {
        public static MTPV10 Static = new MTPV10();

        public AdminShell.Submodel
            SM_ModuleTypePackage,
            SM_ProcessEquipmentAssembly;

        public AdminShell.ConceptDescription
            CD_MTPFile,
            CD_MTPReferences,
            CD_MTPReference,
            CD_SourceList,
            CD_OPCUAServer,
            CD_DisplayName,
            CD_Description,
            CD_DiscoveryUrl,
            CD_ApplicationUri;

        public MTPV10()
        {
            // info
            this.DomainInfo = "Inclusion of Module Type Package (MTP) Data into Asset Administration Shell v1.0";

            // Referable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "MTPV10.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(MTPV10), useFieldNames: true);
        }
    }
}
