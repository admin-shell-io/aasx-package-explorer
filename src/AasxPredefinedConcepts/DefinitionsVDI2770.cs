/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Reflection;
using Aas = AasCore.Aas3_0;

namespace AasxPredefinedConcepts
{
    public class DefinitionsVDI2770 : AasxDefinitionBase
    {
        //
        // Constants
        //

        public const string Vdi2770Sys = "VDI2770:2018";

        public enum Vdi2770DocClass
        {
            All = 0, Identification, TechnicalSpec, DrawingPlans, Assemblies, Certificates,
            Commissioning, Operation, GeneralSafety, Maintenance, Repair, SpareParts, ContractDocs
        };

        public static string[] Vdi2770ClassIdMapping =
        {
            "All",      "* - All document classes",
            "01-01",    "Identification",
            "02-01",    "Technical specifiction",
            "02-02",    "Drawings, plans",
            "02-03",    "Assemblies",
            "02-04",    "Certificates, declarations",
            "03-01",    "Commissioning, de-commissioning",
            "03-02",    "Operation",
            "03-03",    "General safety",
            "03-04",    "Inspection, maintenance, testing",
            "03-05",    "Repair",
            "03-06",    "spare parts",
            "04-01",    "Contract documents"
        };

        public static string GetDocClass(Vdi2770DocClass dc)
        {
            return Vdi2770ClassIdMapping[2 * (int)dc + 0];
        }

        public static string GetDocClassName(Vdi2770DocClass dc)
        {
            return Vdi2770ClassIdMapping[2 * (int)dc + 1];
        }

        public static Vdi2770DocClass GetEnumFromDocClassId(string dcs)
        {
            if (dcs == null)
                return Vdi2770DocClass.All;
            foreach (var en in (Vdi2770DocClass[])Enum.GetValues(typeof(Vdi2770DocClass)))
                if (GetDocClass(en).Trim().ToLower() == dcs.Trim().ToLower())
                    return en;
            return Vdi2770DocClass.All;
        }

        public static Vdi2770DocClass GetEnumFromDocClassName(string dcn)
        {
            if (dcn == null)
                return Vdi2770DocClass.All;
            foreach (var en in (Vdi2770DocClass[])Enum.GetValues(typeof(Vdi2770DocClass)))
                if (GetDocClassName(en).Trim().ToLower() == dcn.Trim().ToLower())
                    return en;
            return Vdi2770DocClass.All;
        }

        public static Vdi2770DocClass GetEnumFromDocClasses(string docClassId, string docClassName)
        {
            var en = GetEnumFromDocClassId(docClassId);
            if (en != Vdi2770DocClass.All)
                return en;
            return GetEnumFromDocClassName(docClassName);
        }

        //
        // Concepts..
        //

        public DefinitionsVDI2770()
        {
            this._library = BuildLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "VDI2770.json");
        }

        public class SetOfDefsVDI2770
        {
            public Aas.Submodel
                SM_VDI2770_Documentation;

            public Aas.ConceptDescription
                CD_VDI2770_Document,
                CD_VDI2770_DocumentIdValue,
                CD_VDI2770_DocumentClassId,
                CD_VDI2770_DocumentClassName,
                CD_VDI2770_DocumentClassificationSystem,
                CD_VDI2770_OrganizationName,
                CD_VDI2770_OrganizationOfficialName,
                CD_VDI2770_DocumentVersion,
                CD_VDI2770_Language,
                CD_VDI2770_Title,
                CD_VDI2770_Date,
                CD_VDI2770_DocumentVersionIdValue,
                CD_VDI2770_DigitalFile,

                /* new, Birgit */
                CD_VDI2770_DocumentId,
                CD_VDI2770_IsPrimaryDocumentId,
                CD_VDI2770_DocumentVersionId,
                CD_VDI2770_Summary,
                CD_VDI2770_Keywords,
                CD_VDI2770_StatusValue,
                CD_VDI2770_Role,
                CD_VDI2770_DomainId,
                CD_VDI2770_ReferencedObject;

            public SetOfDefsVDI2770(AasxDefinitionBase bs)
            {
                this.SM_VDI2770_Documentation = bs.RetrieveReferable<Aas.Submodel>("SM_VDI2770_Documentation");

                this.CD_VDI2770_Document = bs.RetrieveReferable<Aas.ConceptDescription>("CD_VDI2770_Document");
                this.CD_VDI2770_DocumentIdValue = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_DocumentIdValue");
                this.CD_VDI2770_DocumentClassId = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_DocumentClassId");
                this.CD_VDI2770_DocumentClassName = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_DocumentClassName");
                this.CD_VDI2770_DocumentClassificationSystem = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_DocumentClassificationSystem");
                this.CD_VDI2770_OrganizationName = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_OrganizationName");
                this.CD_VDI2770_OrganizationOfficialName = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_OrganizationOfficialName");
                this.CD_VDI2770_DocumentVersion = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_DocumentVersion");
                this.CD_VDI2770_Language = bs.RetrieveReferable<Aas.ConceptDescription>("CD_VDI2770_Language");
                this.CD_VDI2770_Title = bs.RetrieveReferable<Aas.ConceptDescription>("CD_VDI2770_Title");
                this.CD_VDI2770_Date = bs.RetrieveReferable<Aas.ConceptDescription>("CD_VDI2770_Date");
                this.CD_VDI2770_DocumentVersionIdValue = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_DocumentVersionIdValue");
                this.CD_VDI2770_DigitalFile = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_DigitalFile");

                /* new, Birgit */
                this.CD_VDI2770_DocumentId = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_DocumentId");
                this.CD_VDI2770_IsPrimaryDocumentId = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_IsPrimaryDocumentId");
                this.CD_VDI2770_DocumentVersionId = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_DocumentVersionId");
                this.CD_VDI2770_Summary = bs.RetrieveReferable<Aas.ConceptDescription>("CD_VDI2770_Summary");
                this.CD_VDI2770_Keywords = bs.RetrieveReferable<Aas.ConceptDescription>("CD_VDI2770_Keywords");
                this.CD_VDI2770_StatusValue = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_StatusValue");
                this.CD_VDI2770_Role = bs.RetrieveReferable<Aas.ConceptDescription>("CD_VDI2770_Role");
                this.CD_VDI2770_DomainId = bs.RetrieveReferable<Aas.ConceptDescription>("CD_VDI2770_DomainId");
                this.CD_VDI2770_ReferencedObject = bs.RetrieveReferable<Aas.ConceptDescription>(
                    "CD_VDI2770_ReferencedObject");
            }

            public Aas.IReferable[] GetAllReferables()
            {
                return new Aas.IReferable[] {
                    SM_VDI2770_Documentation,
                    CD_VDI2770_Document,
                    CD_VDI2770_DocumentIdValue,
                    CD_VDI2770_DocumentClassId,
                    CD_VDI2770_DocumentClassName,
                    CD_VDI2770_DocumentClassificationSystem,
                    CD_VDI2770_OrganizationName,
                    CD_VDI2770_OrganizationOfficialName,
                    CD_VDI2770_DocumentVersion,
                    CD_VDI2770_Language,
                    CD_VDI2770_Title,
                    CD_VDI2770_Date,
                    CD_VDI2770_DocumentVersionIdValue,
                    CD_VDI2770_DigitalFile,

                    /* new, Birgit */
                    CD_VDI2770_DocumentId,
                    CD_VDI2770_IsPrimaryDocumentId,
                    CD_VDI2770_DocumentVersionId,
                    CD_VDI2770_Summary,
                    CD_VDI2770_Keywords,
                    CD_VDI2770_StatusValue,
                    CD_VDI2770_Role,
                    CD_VDI2770_DomainId,
                    CD_VDI2770_ReferencedObject
                };
            }
        }

    }
}
