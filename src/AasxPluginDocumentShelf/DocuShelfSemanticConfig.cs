/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;

namespace AasxPluginDocumentShelf
{
    /// <summary>
    /// Starting from Dec 2021, move these information into a separate class.
    /// These were for V1.0 only; no case is known, that these were redefined.
    /// Therefore, make then more static
    /// </summary>
    public class DocuShelfSemanticConfig
    {
        public Aas.Key SemIdDocumentation = null;
        public Aas.Key SemIdDocument = null;
        public Aas.Key SemIdDocumentIdValue = null;
        public Aas.Key SemIdDocumentClassId = null;
        public Aas.Key SemIdDocumentClassName = null;
        public Aas.Key SemIdDocumentClassificationSystem = null;
        public Aas.Key SemIdOrganizationName = null;
        public Aas.Key SemIdOrganizationOfficialName = null;
        public Aas.Key SemIdDocumentVersion = null;
        public Aas.Key SemIdLanguage = null;
        public Aas.Key SemIdTitle = null;
        public Aas.Key SemIdDate = null;
        public Aas.Key SemIdDocumentVersionIdValue = null;
        public Aas.Key SemIdDigitalFile = null;

        public Aas.Key SemIdDocumentId = null;
        public Aas.Key SemIdIsPrimaryDocumentId = null;
        public Aas.Key SemIdDocumentVersionId = null;
        public Aas.Key SemIdSummary = null;
        public Aas.Key SemIdKeywords = null;
        public Aas.Key SemIdStatusValue = null;
        public Aas.Key SemIdRole = null;
        public Aas.Key SemIdDomainId = null;
        public Aas.Key SemIdReferencedObject = null;

        public FormDescSubmodelElementCollection FormVdi2770 = null;

        public static DocuShelfSemanticConfig Singleton = CreateDefaultV10();

        /// <summary>
        /// Create a set of minimal options; based on the options approach for V10
        /// </summary>
        public static DocuShelfSemanticConfig CreateDefaultV10()
        {
            var opt = new DocuShelfSemanticConfig();

            // use pre-definitions
            var preDefLib = new AasxPredefinedConcepts.DefinitionsVDI2770();
            var preDefs = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(preDefLib);

            opt.SemIdDocumentation = preDefs.SM_VDI2770_Documentation?.SemanticId?.GetAsExactlyOneKey();

            opt.SemIdDocument = preDefs.CD_VDI2770_Document?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentIdValue = preDefs.CD_VDI2770_DocumentIdValue?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentClassId = preDefs.CD_VDI2770_DocumentClassId?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentClassName = preDefs.CD_VDI2770_DocumentClassName?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentClassificationSystem =
                preDefs.CD_VDI2770_DocumentClassificationSystem?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdOrganizationName = preDefs.CD_VDI2770_OrganizationName?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdOrganizationOfficialName =
                preDefs.CD_VDI2770_OrganizationOfficialName?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentVersion = preDefs.CD_VDI2770_DocumentVersion?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdLanguage = preDefs.CD_VDI2770_Language?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdTitle = preDefs.CD_VDI2770_Title?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdDate = preDefs.CD_VDI2770_Date?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentVersionIdValue =
                preDefs.CD_VDI2770_DocumentVersionIdValue?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdDigitalFile = preDefs.CD_VDI2770_DigitalFile?.GetCdReference()?.GetAsExactlyOneKey();

            /* new, Birgit */
            opt.SemIdDocumentId = preDefs.CD_VDI2770_DocumentId?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdIsPrimaryDocumentId =
                preDefs.CD_VDI2770_IsPrimaryDocumentId?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentVersionId = preDefs.CD_VDI2770_DocumentVersionId?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdSummary = preDefs.CD_VDI2770_Summary?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdKeywords = preDefs.CD_VDI2770_Keywords?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdStatusValue = preDefs.CD_VDI2770_StatusValue?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdRole = preDefs.CD_VDI2770_Role?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdDomainId = preDefs.CD_VDI2770_DomainId?.GetCdReference()?.GetAsExactlyOneKey();
            opt.SemIdReferencedObject = preDefs.CD_VDI2770_ReferencedObject?.GetCdReference()?.GetAsExactlyOneKey();

            return opt;
        }

        /// <summary>
        /// Create a set of minimal options suitable for V11
        /// </summary>
        public static DocuShelfSemanticConfig CreateDefaultV11()
        {
            var opt = new DocuShelfSemanticConfig();

            // use pre-definitions
            var preDefs = AasxPredefinedConcepts.VDI2770v11.Static;

            opt.SemIdDocumentation = preDefs.SM_ManufacturerDocumentation.GetSemanticKey();
            opt.SemIdDocument = preDefs.CD_Document?.GetSingleKey();
            opt.SemIdDocumentIdValue = preDefs.CD_DocumentIdValue?.GetSingleKey();
            opt.SemIdDocumentClassId = preDefs.CD_ClassId?.GetSingleKey();
            opt.SemIdDocumentClassName = preDefs.CD_ClassName?.GetSingleKey();
            opt.SemIdDocumentClassificationSystem = preDefs.CD_ClassificationSystem?.GetSingleKey();
            opt.SemIdOrganizationName = preDefs.CD_OrganizationName?.GetSingleKey();
            opt.SemIdOrganizationOfficialName = preDefs.CD_OrganizationOfficialName?.GetSingleKey();
            opt.SemIdDocumentVersion = preDefs.CD_DocumentVersion?.GetSingleKey();
            opt.SemIdLanguage = preDefs.CD_Language?.GetSingleKey();
            opt.SemIdTitle = preDefs.CD_Title?.GetSingleKey();
            opt.SemIdDate = preDefs.CD_SetDate?.GetSingleKey();
            opt.SemIdDocumentVersionIdValue = preDefs.CD_DocumentVersionId?.GetSingleKey();
            opt.SemIdDigitalFile = preDefs.CD_DigitalFile?.GetSingleKey();
            opt.SemIdDocumentId = preDefs.CD_DocumentId?.GetSingleKey();
            opt.SemIdIsPrimaryDocumentId = preDefs.CD_IsPrimary?.GetSingleKey();
            opt.SemIdDocumentVersionId = preDefs.CD_DocumentVersionId?.GetSingleKey();
            opt.SemIdSummary = preDefs.CD_Summary?.GetSingleKey();
            opt.SemIdKeywords = preDefs.CD_KeyWords?.GetSingleKey();
            opt.SemIdStatusValue = preDefs.CD_StatusValue?.GetSingleKey();
            opt.SemIdDomainId = preDefs.CD_DocumentDomainId?.GetSingleKey();

            return opt;
        }

        /// <summary>
        /// Create a set of minimal options suitable for V12
        /// </summary>
        public static DocuShelfSemanticConfig CreateDefaultV12()
        {
            var opt = new DocuShelfSemanticConfig();

            // use pre-definitions
            var preDefs = AasxPredefinedConcepts.IdtaHandoverDocumentationV12.Static;

            opt.SemIdDocumentation = preDefs.SM_HandoverDocumentation.GetSemanticKey();
            opt.SemIdDocument = preDefs.CD_Document?.GetSingleKey();
            opt.SemIdDocumentIdValue = preDefs.CD_ValueId?.GetSingleKey();
            opt.SemIdDocumentClassId = preDefs.CD_ClassId?.GetSingleKey();
            opt.SemIdDocumentClassName = preDefs.CD_ClassName?.GetSingleKey();
            opt.SemIdDocumentClassificationSystem = preDefs.CD_ClassificationSystem?.GetSingleKey();
            opt.SemIdOrganizationName = preDefs.CD_OrganizationName?.GetSingleKey();
            opt.SemIdOrganizationOfficialName = preDefs.CD_OrganizationOfficialName?.GetSingleKey();
            opt.SemIdDocumentVersion = preDefs.CD_DocumentVersion?.GetSingleKey();
            opt.SemIdLanguage = preDefs.CD_Language?.GetSingleKey();
            opt.SemIdTitle = preDefs.CD_Title?.GetSingleKey();
            opt.SemIdDate = preDefs.CD_StatusSetDate?.GetSingleKey();
            opt.SemIdDocumentVersionIdValue = preDefs.CD_DocumentVersionId?.GetSingleKey();
            opt.SemIdDigitalFile = preDefs.CD_DigitalFile?.GetSingleKey();
            opt.SemIdDocumentId = preDefs.CD_DocumentId?.GetSingleKey();
            opt.SemIdIsPrimaryDocumentId = preDefs.CD_IsPrimary?.GetSingleKey();
            opt.SemIdDocumentVersionId = preDefs.CD_DocumentVersionId?.GetSingleKey();
            opt.SemIdSummary = preDefs.CD_Summary?.GetSingleKey();
            opt.SemIdKeywords = preDefs.CD_KeyWords?.GetSingleKey();
            opt.SemIdStatusValue = preDefs.CD_StatusValue?.GetSingleKey();
            opt.SemIdDomainId = preDefs.CD_DocumentDomainId?.GetSingleKey();

            return opt;
        }

        /// <summary>
        /// Create a set of minimal options suitable for given version
        /// </summary>
        public static DocuShelfSemanticConfig CreateDefaultFor(DocumentEntity.SubmodelVersion ver)
        {
            if (ver == DocumentEntity.SubmodelVersion.V12)
                return CreateDefaultV12();
            if (ver == DocumentEntity.SubmodelVersion.V11)
                return CreateDefaultV11();
            return CreateDefaultV10();
        }

        /// <summary>
        /// Create a default template description for VDI2770 based on the SemanticIds from the <c>options</c>
        /// </summary>
        public static FormDescSubmodelElementCollection CreateVdi2770TemplateDescFor(
            DocumentEntity.SubmodelVersion ver, DocumentShelfOptions opt)
        {
            if (ver == DocumentEntity.SubmodelVersion.V12)
                // needs to be handle on level of calling function
                return null;
            if (ver == DocumentEntity.SubmodelVersion.V11)
                return CreateVdi2770v11TemplateDesc();
            return CreateVdi2770TemplateDesc(opt);
        }

        /// <summary>
        /// Create a default template description for VDI2770 based on the SemanticIds from the <c>options</c>
        /// </summary>
        public static FormDescSubmodelElementCollection CreateVdi2770TemplateDesc(DocumentShelfOptions opt)
        {
            if (opt == null)
                return null;

            var semConfig = DocuShelfSemanticConfig.CreateDefaultV10();

            // DocumentItem

            var descDoc = new FormDescSubmodelElementCollection(
                "Document", FormMultiplicity.ZeroToMany, semConfig.SemIdDocument, "Document{0:00}",
                "Each document item comprises a set of elements describing the information of a VDI 2770 Document " +
                "with directly attached DocumentVersion.");

            // Document

            descDoc.Add(new FormDescProperty(
                "DocumentId", FormMultiplicity.One, semConfig.SemIdDocumentId, "DocumentId",
                "The combination of DocumentId and DocumentVersionId shall be unique."));

            descDoc.Add(new FormDescProperty(
                "IsPrimary", FormMultiplicity.One, semConfig.SemIdIsPrimaryDocumentId, "IsPrimary",
                "True, if primary document id for the document."));

            var descDocClass = new FormDescProperty(
                "ClassId", FormMultiplicity.One, semConfig.SemIdDocumentClassId, "ClassId",
                "VDI2770 ClassId of the document.");

            var cbList = new List<string>();
            var vlList = new List<string>();
            foreach (var dc in (DefinitionsVDI2770.Vdi2770DocClass[])Enum.GetValues(
                                                typeof(DefinitionsVDI2770.Vdi2770DocClass)))
            {
                if ((int)dc == 0)
                    continue;
                cbList.Add("" + DefinitionsVDI2770.GetDocClass(dc) + " - " + DefinitionsVDI2770.GetDocClassName(dc));
                vlList.Add("" + DefinitionsVDI2770.GetDocClass(dc));
            }

            descDocClass.comboBoxChoices = cbList.ToArray();
            descDocClass.valueFromComboBoxIndex = vlList.ToArray();
            descDoc.Add(descDocClass);

            var descDocName = new FormDescProperty(
                "ClassName", FormMultiplicity.One, semConfig.SemIdDocumentClassName, "ClassName",
                "VDI2770 ClassName of the document. This property is automatically computed based on ClassId.",
                isReadOnly: true);

            descDocName.SlaveOfIdShort = "ClassId";

            descDocName.valueFromMasterValue = new Dictionary<string, string>();
            foreach (var dc in (DefinitionsVDI2770.Vdi2770DocClass[])Enum.GetValues(
                                                                    typeof(DefinitionsVDI2770.Vdi2770DocClass)))
                descDocName.valueFromMasterValue.Add(
                    DefinitionsVDI2770.GetDocClass(dc), DefinitionsVDI2770.GetDocClassName(dc));

            descDoc.Add(descDocName);

            descDoc.Add(
                new FormDescProperty(
                    "ClassificationSystem", FormMultiplicity.One, semConfig.SemIdDocumentClassificationSystem,
                    "ClassificationSystem",
                    "This property is always set to VDI2770:2018", isReadOnly: true, presetValue: "VDI2770:2018"));

            descDoc.Add(new FormDescProperty(
                "ReferencedObject", FormMultiplicity.One, semConfig.SemIdReferencedObject, "ReferencedObject",
                "Reference to Asset or Entity, on which the Document is related to."));

            // DocumentVersion

            var descDocVer = new FormDescSubmodelElementCollection(
                "DocumentVersion", FormMultiplicity.OneToMany, semConfig.SemIdDocumentVersion, "DocumentVersion{0:00}",
                "VDI2770 allows for multiple DocumentVersions for a document to be delivered.");
            descDoc.Add(descDocVer);

            descDocVer.Add(new FormDescProperty(
                "DocumentVersionId", FormMultiplicity.One, semConfig.SemIdDocumentVersionIdValue, "DocumentVersionId",
                "The combination of DocumentId and DocumentVersionId shall be unique."));

            descDocVer.Add(new FormDescProperty(
                "Languages", FormMultiplicity.ZeroToMany, semConfig.SemIdLanguage, "Language{0}",
                "List of languages used in the DocumentVersion. For most cases, " +
                "at least one language shall be given."));

            descDocVer.Add(new FormDescMultiLangProp(
                "Title", FormMultiplicity.One, semConfig.SemIdTitle, "Title",
                "Language dependent title of the document."));

            descDocVer.Add(new FormDescMultiLangProp(
                "Summary", FormMultiplicity.One, semConfig.SemIdSummary, "Summary",
                "Language dependent summary of the document."));

            descDocVer.Add(new FormDescMultiLangProp(
                "Keywords", FormMultiplicity.One, semConfig.SemIdKeywords, "Keywords",
                "Language dependent keywords of the document."));

            descDocVer.Add(new FormDescFile(
                "DigitalFile", FormMultiplicity.OneToMany, semConfig.SemIdDigitalFile, "DigitalFile{0:00}",
                "Digital file, which represents the Document and DocumentVersion. " +
                "A PDF/A format is required for textual representations."));

            descDocVer.Add(new FormDescProperty(
                "SetDate", FormMultiplicity.One, semConfig.SemIdDate, "SetDate",
                "Date when the document was introduced into the AAS or set to the status. Format is YYYY-MM-dd."));

            var descStatus = new FormDescProperty(
                "StatusValue", FormMultiplicity.One, semConfig.SemIdStatusValue, "StatusValue",
                "Each document version represents a point in time in the document life cycle. " +
                "This status value refers to the milestones in the document life cycle. " +
                "The following two statuses should be used for the application of this guideline: " +
                "InReview (under review), Released (released).");
            descDocVer.Add(descStatus);
            descStatus.comboBoxChoices = new[] { "InReview", "Released" };

            var descRole = new FormDescProperty(
                "Role", FormMultiplicity.One, semConfig.SemIdRole, "Role",
                "Define a role for the organisation according to the following selection list.");
            descDocVer.Add(descRole);
            descRole.comboBoxChoices = new[] { "Author", "Customer", "Supplier", "Manufacturer", "Responsible" };

            descDocVer.Add(new FormDescProperty(
                "OrganizationName", FormMultiplicity.One, semConfig.SemIdOrganizationName, "OrganizationName",
                "Common name for the organisation."));

            descDocVer.Add(new FormDescProperty(
                "OrganizationOfficialName", FormMultiplicity.One, semConfig.SemIdOrganizationOfficialName,
                "OrganizationOfficialName",
                "Official name for the organisation (which might be longer or include legal information)."));

            return descDoc;
        }

        /// <summary>
        /// Create form descriptions for the new v1.1 template of VDIO2770
        /// </summary>
        public static FormDescSubmodelElementCollection CreateVdi2770v11TemplateDesc()
        {
            // shortcut
            var defs = AasxPredefinedConcepts.VDI2770v11.Static;

            // DocumentItem

            var descDoc = new FormDescSubmodelElementCollection(
                "Document", FormMultiplicity.ZeroToMany, defs.CD_Document?.GetSingleKey(),
                "Document{0:00}",
                "Each document item comprises a set of elements describing the information of a VDI 2770 Document " +
                "with directly attached DocumentVersion.");

            // DocumentIdDomain

            var descDocIdDom = new FormDescSubmodelElementCollection(
                "DocumentIdDomain", FormMultiplicity.OneToMany, defs.CD_DocumentId?.GetSingleKey(),
                "DocumentIdDomain{0:00}",
                "Set of information on the Document within a given domain, e.g. the providing organisation.");
            descDoc.Add(descDocIdDom);

            descDocIdDom.Add(new FormDescProperty(
                "DocumentDomainId", FormMultiplicity.One, defs.CD_DocumentDomainId?.GetSingleKey(),
                "DocumentDomainId",
                "Identification of the Domain, e.g. the providing organisation."));

            descDocIdDom.Add(new FormDescProperty(
                "DocumentId", FormMultiplicity.One, defs.CD_DocumentIdValue?.GetSingleKey(),
                "DocumentId",
                "Identification of the Document within a given domain, e.g. the providing organisation."));

            descDocIdDom.Add(new FormDescProperty(
                "IsPrimary", FormMultiplicity.ZeroToOne, defs.CD_IsPrimary?.GetSingleKey(),
                "IsPrimary",
                "Flag indicating that a DocumentId within a collection of at least two DocumentId`s is the " +
                "‘primary’ identifier for the document. This is the preferred ID of the document (commonly from " +
                "the point of view of the owner of the asset)."));

            // DocumentClassification

            var descDocClass = new FormDescSubmodelElementCollection(
                "DocumentClassification", FormMultiplicity.ZeroToMany, defs.CD_DocumentClassification?.GetSingleKey(),
                "DocumentClassification{0:00}",
                "Set of information on the Document within a given domain, e.g. the providing organisation.");
            descDoc.Add(descDocClass);

            var descDocClassSystem = new FormDescProperty(
                            "ClassificationSystem",
                            FormMultiplicity.One, defs.CD_ClassificationSystem?.GetSingleKey(),
                            "ClassificationSystem",
                            "Identification of the classification system. A classification according to " +
                            "VDI2770:2018 shall be given.");

            descDocClassSystem.comboBoxChoices = new[]
                { "VDI2770:2018", "IEC 61355-1:2008", "IEC/IEEE 82079-1:2019" };

            descDocClass.Add(descDocClassSystem);

            var descDocClassId = new FormDescProperty(
                "ClassId", FormMultiplicity.One, defs.CD_ClassId?.GetSingleKey(),
                "ClassId",
                "ClassId of the document in VDI2770 or other.");

            var cbList = new List<string>(new[] { "(free text)" });
            var vlList = new List<string>(new[] { "" });
            foreach (var dc in (DefinitionsVDI2770.Vdi2770DocClass[])Enum.GetValues(
                                                typeof(DefinitionsVDI2770.Vdi2770DocClass)))
            {
                if ((int)dc == 0)
                    continue;
                cbList.Add("VDI2770 - " + DefinitionsVDI2770.GetDocClass(dc) + " - "
                    + DefinitionsVDI2770.GetDocClassName(dc));
                vlList.Add("" + DefinitionsVDI2770.GetDocClass(dc));
            }

            descDocClassId.comboBoxChoices = cbList.ToArray();
            descDocClassId.valueFromComboBoxIndex = vlList.ToArray();
            descDocClass.Add(descDocClassId);

            var descDocName = new FormDescProperty(
                "ClassName", FormMultiplicity.One, defs.CD_ClassName?.GetSingleKey(),
                "ClassName",
                "ClassName of the document in VDI2770 or other. " +
                "This property is automatically computed based on ClassId.");

            descDocName.SlaveOfIdShort = "ClassId";

            descDocName.valueFromMasterValue = new Dictionary<string, string>();
            descDocName.valueFromMasterValue.Add("", "");
            foreach (var dc in (DefinitionsVDI2770.Vdi2770DocClass[])Enum.GetValues(
                                                                    typeof(DefinitionsVDI2770.Vdi2770DocClass)))
                descDocName.valueFromMasterValue.Add(
                    DefinitionsVDI2770.GetDocClass(dc), DefinitionsVDI2770.GetDocClassName(dc));

            descDocClass.Add(descDocName);

            // DocumentVersion

            var descDocVer = new FormDescSubmodelElementCollection(
                "DocumentVersion", FormMultiplicity.OneToMany, defs.CD_DocumentVersion?.GetSingleKey(),
                "DocumentVersion{0:00}",
                "VDI2770 allows for multiple DocumentVersions for a document to be delivered.");
            descDoc.Add(descDocVer);

            descDocVer.Add(new FormDescProperty(
                "Languages", FormMultiplicity.OneToMany, defs.CD_Language?.GetSingleKey(), "Language{0:00}",
                "List of languages used in the DocumentVersion. For most cases, " +
                "at least one language shall be given."));

            descDocVer.Add(new FormDescProperty(
                "DocumentVersionId", FormMultiplicity.One, defs.CD_DocumentVersionId?.GetSingleKey(),
                "DocumentVersionId",
                "Unambiguous identification number of a DocumentVersion."));

            descDocVer.Add(new FormDescMultiLangProp(
                "Title", FormMultiplicity.One, defs.CD_Title?.GetSingleKey(), "Title",
                "Language dependent title of the document."));

            descDocVer.Add(new FormDescMultiLangProp(
                "SubTitle", FormMultiplicity.ZeroToOne, defs.CD_SubTitle?.GetSingleKey(), "SubTitle",
                "Language dependent sub title of the document."));

            descDocVer.Add(new FormDescMultiLangProp(
                "Summary", FormMultiplicity.One, defs.CD_Summary?.GetSingleKey(), "Summary",
                "Language dependent summary of the document."));

            descDocVer.Add(new FormDescMultiLangProp(
                "Keywords", FormMultiplicity.One, defs.CD_KeyWords?.GetSingleKey(), "Keywords",
                "Language dependent keywords of the document."));

            descDocVer.Add(new FormDescProperty(
                "SetDate", FormMultiplicity.One, defs.CD_SetDate?.GetSingleKey(), "SetDate",
                "Date when the document status was set. Format is YYYY-MM-dd."));

            var descStatus = new FormDescProperty(
                "StatusValue", FormMultiplicity.One, defs.CD_StatusValue?.GetSingleKey(), "StatusValue",
                "Each document version represents a point in time in the document life cycle. " +
                "This status value refers to the milestones in the document life cycle. " +
                "The following two statuses should be used for the application of this guideline: " +
                "InReview (under review), Released (released).");
            descDocVer.Add(descStatus);
            descStatus.comboBoxChoices = new[] { "InReview", "Released" };

            descDocVer.Add(new FormDescProperty(
                "OrganizationName", FormMultiplicity.One, defs.CD_OrganizationName?.GetSingleKey(), "OrganizationName",
                "Organiziation name of the author of the Document."));

            descDocVer.Add(new FormDescProperty(
                "OrganizationOfficialName", FormMultiplicity.One, defs.CD_OrganizationOfficialName?.GetSingleKey(),
                "OrganizationOfficialName",
                "Official name of the organization of author of the Document " +
                "(which might be longer or include legal information)."));

            descDocVer.Add(new FormDescFile(
                "DigitalFile", FormMultiplicity.OneToMany, defs.CD_DigitalFile?.GetSingleKey(), "DigitalFile{0:00}",
                "Digital file, which represents the Document and DocumentVersion. " +
                "A PDF/A format is required for textual representations."));

            descDocVer.Add(new FormDescFile(
                "PreviewFile", FormMultiplicity.ZeroToOne, defs.CD_PreviewFile?.GetSingleKey(), "PreviewFile{0:00}",
                "Provides a preview image of the Document, e.g. first page, in a commonly used " +
                "image format and low resolution."));

            descDocVer.Add(new FormDescReferenceElement(
                "RefersTo", FormMultiplicity.ZeroToMany, defs.CD_RefersTo?.GetSingleKey(),
                "RefersTo{0:00}",
                "Forms a generic RefersTo-relationship to another Document or DocumentVersion. " +
                "They have a loose relationship."));

            descDocVer.Add(new FormDescReferenceElement(
                "BasedOn", FormMultiplicity.ZeroToMany, defs.CD_BasedOn?.GetSingleKey(),
                "BasedOn{0:00}",
                "Forms a BasedOn-relationship to another Document or DocumentVersion. Typically states, that the " +
                "content of the document bases on another document (e.g. specification requirements). Both have a " +
                "strong relationship."));

            descDocVer.Add(new FormDescReferenceElement(
                "TranslationOf", FormMultiplicity.ZeroToMany, defs.CD_TranslationOf?.GetSingleKey(),
                "TranslationOf{0:00}",
                "Forms a TranslationOf-relationship to another Document or DocumentVersion. Both have a " +
                "strong relationship."));

            // back to Document

            descDoc.Add(new FormDescReferenceElement(
                "DocumentedEntity", FormMultiplicity.ZeroToOne, defs.CD_DocumentedEntity?.GetSingleKey(),
                "DocumentedEntity",
                "Identifies the Entity, which is subject to the Documentation."));

            // end

            return descDoc;
        }

    }
}
