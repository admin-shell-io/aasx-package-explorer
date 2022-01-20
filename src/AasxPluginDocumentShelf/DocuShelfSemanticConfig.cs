/*
Copyright (c) 2018-2022 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AdminShellNS;
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
        public AdminShell.Identifier SemIdDocumentation = null;
        public AdminShell.Identifier SemIdDocument = null;
        public AdminShell.Identifier SemIdDocumentIdValue = null;
        public AdminShell.Identifier SemIdDocumentClassId = null;
        public AdminShell.Identifier SemIdDocumentClassName = null;
        public AdminShell.Identifier SemIdDocumentClassificationSystem = null;
        public AdminShell.Identifier SemIdOrganizationName = null;
        public AdminShell.Identifier SemIdOrganizationOfficialName = null;
        public AdminShell.Identifier SemIdDocumentVersion = null;
        public AdminShell.Identifier SemIdLanguage = null;
        public AdminShell.Identifier SemIdTitle = null;
        public AdminShell.Identifier SemIdDate = null;
        public AdminShell.Identifier SemIdDocumentVersionIdValue = null;
        public AdminShell.Identifier SemIdDigitalFile = null;

        public AdminShell.Identifier SemIdDocumentId = null;
        public AdminShell.Identifier SemIdIsPrimaryDocumentId = null;
        public AdminShell.Identifier SemIdDocumentVersionId = null;
        public AdminShell.Identifier SemIdSummary = null;
        public AdminShell.Identifier SemIdKeywords = null;
        public AdminShell.Identifier SemIdStatusValue = null;
        public AdminShell.Identifier SemIdRole = null;
        public AdminShell.Identifier SemIdDomainId = null;
        public AdminShell.Identifier SemIdReferencedObject = null;

        public FormDescSubmodelElementCollection FormVdi2770 = null;

        public static DocuShelfSemanticConfig Singleton = CreateDefault();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static DocuShelfSemanticConfig CreateDefault()
        {
            var opt = new DocuShelfSemanticConfig();

            // use pre-definitions
            var preDefLib = new AasxPredefinedConcepts.DefinitionsVDI2770();
            var preDefs = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(preDefLib);

            opt.SemIdDocumentation = preDefs.SM_VDI2770_Documentation?.GetAutoSingleId();

            opt.SemIdDocument = preDefs.CD_VDI2770_Document?.GetSingleId();
            opt.SemIdDocumentIdValue = preDefs.CD_VDI2770_DocumentIdValue?.GetSingleId();
            opt.SemIdDocumentClassId = preDefs.CD_VDI2770_DocumentClassId?.GetSingleId();
            opt.SemIdDocumentClassName = preDefs.CD_VDI2770_DocumentClassName?.GetSingleId();
            opt.SemIdDocumentClassificationSystem =
                preDefs.CD_VDI2770_DocumentClassificationSystem?.GetSingleId();
            opt.SemIdOrganizationName = preDefs.CD_VDI2770_OrganizationName?.GetSingleId();
            opt.SemIdOrganizationOfficialName =
                preDefs.CD_VDI2770_OrganizationOfficialName?.GetSingleId();
            opt.SemIdDocumentVersion = preDefs.CD_VDI2770_DocumentVersion?.GetSingleId();
            opt.SemIdLanguage = preDefs.CD_VDI2770_Language?.GetSingleId();
            opt.SemIdTitle = preDefs.CD_VDI2770_Title?.GetSingleId();
            opt.SemIdDate = preDefs.CD_VDI2770_Date?.GetSingleId();
            opt.SemIdDocumentVersionIdValue =
                preDefs.CD_VDI2770_DocumentVersionIdValue?.GetSingleId();
            opt.SemIdDigitalFile = preDefs.CD_VDI2770_DigitalFile?.GetSingleId();

            /* new, Birgit */
            opt.SemIdDocumentId = preDefs.CD_VDI2770_DocumentId?.GetSingleId();
            opt.SemIdIsPrimaryDocumentId =
                preDefs.CD_VDI2770_IsPrimaryDocumentId?.GetSingleId();
            opt.SemIdDocumentVersionId = preDefs.CD_VDI2770_DocumentVersionId?.GetSingleId();
            opt.SemIdSummary = preDefs.CD_VDI2770_Summary?.GetSingleId();
            opt.SemIdKeywords = preDefs.CD_VDI2770_Keywords?.GetSingleId();
            opt.SemIdStatusValue = preDefs.CD_VDI2770_StatusValue?.GetSingleId();
            opt.SemIdRole = preDefs.CD_VDI2770_Role?.GetSingleId();
            opt.SemIdDomainId = preDefs.CD_VDI2770_DomainId?.GetSingleId();
            opt.SemIdReferencedObject = preDefs.CD_VDI2770_ReferencedObject?.GetSingleId();

            return opt;
        }

        /// <summary>
        /// Create a default template description for VDI2770 based on the SemanticIds from the <c>options</c>
        /// </summary>
        /// <param name="opt"></param>
        /// <returns></returns>
        public static FormDescSubmodelElementCollection CreateVdi2770TemplateDesc(DocumentShelfOptions opt)
        {
            if (opt == null)
                return null;

            var semConfig = DocuShelfSemanticConfig.CreateDefault();

            // DocumentItem

            var descDoc = new FormDescSubmodelElementCollection(
                "Document", FormMultiplicity.ZeroToMany, semConfig.SemIdDocument, "Document{0:00}",
                "Each document item comprises a set of elements describing the information of a VDI 2770 Document " +
                "with directly attached DocumentVersion.");

            // Document

            descDoc.Add(new FormDescProperty(
                "DocumentId", FormMultiplicity.One, semConfig.SemIdDocumentId, "DocumentId",
                "The combination of DocumentId and DocumentVersionId shall be unqiue."));

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
                "VDI2770 ClassName of the document. This property is automaticall computed based on ClassId.",
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
                "DocumentVersion", FormMultiplicity.OneToMany, semConfig.SemIdDocumentVersion, "DocumentVersion",
                "VDI2770 allows for multiple DocumentVersions for a document to be delivered.");
            descDoc.Add(descDocVer);

            descDocVer.Add(new FormDescProperty(
                "DocumentVersionId", FormMultiplicity.One, semConfig.SemIdDocumentVersionIdValue, "DocumentVersionId",
                "The combination of DocumentId and DocumentVersionId shall be unqiue."));

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
                "Document", FormMultiplicity.ZeroToMany, defs.CD_Document?.GetSingleId(),
                "Document{0:00}",
                "Each document item comprises a set of elements describing the information of a VDI 2770 Document " +
                "with directly attached DocumentVersion.");

            // DocumentIdDomain

            var descDocIdDom = new FormDescSubmodelElementCollection(
                "DocumentIdDomain", FormMultiplicity.OneToMany, defs.CD_DocumentId?.GetSingleId(),
                "DocumentIdDomain{0:00}",
                "Set of information on the Document within a given domain, e.g. the providing organisation.");
            descDoc.Add(descDocIdDom);

            descDocIdDom.Add(new FormDescProperty(
                "DocumentDomainId", FormMultiplicity.One, defs.CD_DocumentDomainId?.GetSingleId(),
                "DocumentDomainId",
                "Identification of the Domain, e.g. the providing organisation."));

            descDocIdDom.Add(new FormDescProperty(
                "DocumentId", FormMultiplicity.One, defs.CD_DocumentId?.GetSingleId(),
                "DocumentId",
                "Identification of the Document within a given domain, e.g. the providing organisation."));

            descDocIdDom.Add(new FormDescProperty(
                "IsPrimary", FormMultiplicity.ZeroToOne, defs.CD_IsPrimary?.GetSingleId(),
                "IsPrimary",
                "Flag indicating that a DocumentId within a collection of at least two DocumentId`s is the " +
                "‘primary’ identifier for the document. This is the preferred ID of the document (commonly from " +
                "the point of view of the owner of the asset)."));

            // DocumentClassification

            var descDocClass = new FormDescSubmodelElementCollection(
                "DocumentClassification", FormMultiplicity.ZeroToMany, defs.CD_DocumentClassification?.GetSingleId(),
                "DocumentClassification{0:00}",
                "Set of information on the Document within a given domain, e.g. the providing organisation.");
            descDoc.Add(descDocClass);

            var descDocClassSystem = new FormDescProperty(
                            "ClassificationSystem",
                            FormMultiplicity.One, defs.CD_ClassificationSystem?.GetSingleId(),
                            "ClassificationSystem",
                            "Identification of the classification system. A classification according to " +
                            "VDI2770:2018 shall be given.");

            descDocClassSystem.comboBoxChoices = new[]
                { "VDI2770:2018", "IEC 61355-1:2008", "IEC/IEEE 82079-1:2019" };

            descDocClass.Add(descDocClassSystem);

            var descDocClassId = new FormDescProperty(
                "ClassId", FormMultiplicity.One, defs.CD_ClassId?.GetSingleId(),
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
                "ClassName", FormMultiplicity.One, defs.CD_ClassName?.GetSingleId(),
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
                "DocumentVersion", FormMultiplicity.OneToMany, defs.CD_DocumentVersion?.GetSingleId(),
                "DocumentVersion",
                "VDI2770 allows for multiple DocumentVersions for a document to be delivered.");
            descDoc.Add(descDocVer);

            descDocVer.Add(new FormDescProperty(
                "Languages", FormMultiplicity.OneToMany, defs.CD_Language?.GetSingleId(), "Language{0:00}",
                "List of languages used in the DocumentVersion. For most cases, " +
                "at least one language shall be given."));

            descDocVer.Add(new FormDescProperty(
                "DocumentVersionId", FormMultiplicity.One, defs.CD_DocumentVersionId?.GetSingleId(),
                "DocumentVersionId",
                "Unambigous identification number of a DocumentVersion."));

            descDocVer.Add(new FormDescMultiLangProp(
                "Title", FormMultiplicity.One, defs.CD_Title?.GetSingleId(), "Title",
                "Language dependent title of the document."));

            descDocVer.Add(new FormDescMultiLangProp(
                "SubTitle", FormMultiplicity.ZeroToOne, defs.CD_SubTitle?.GetSingleId(), "SubTitle",
                "Language dependent sub title of the document."));

            descDocVer.Add(new FormDescMultiLangProp(
                "Summary", FormMultiplicity.One, defs.CD_Summary?.GetSingleId(), "Summary",
                "Language dependent summary of the document."));

            descDocVer.Add(new FormDescMultiLangProp(
                "Keywords", FormMultiplicity.One, defs.CD_KeyWords?.GetSingleId(), "Keywords",
                "Language dependent keywords of the document."));

            descDocVer.Add(new FormDescProperty(
                "SetDate", FormMultiplicity.One, defs.CD_SetDate?.GetSingleId(), "SetDate",
                "Date when the document status was set. Format is YYYY-MM-dd."));

            var descStatus = new FormDescProperty(
                "StatusValue", FormMultiplicity.One, defs.CD_StatusValue?.GetSingleId(), "StatusValue",
                "Each document version represents a point in time in the document life cycle. " +
                "This status value refers to the milestones in the document life cycle. " +
                "The following two statuses should be used for the application of this guideline: " +
                "InReview (under review), Released (released).");
            descDocVer.Add(descStatus);
            descStatus.comboBoxChoices = new[] { "InReview", "Released" };

            descDocVer.Add(new FormDescProperty(
                "OrganizationName", FormMultiplicity.One, defs.CD_OrganizationName?.GetSingleId(), "OrganizationName",
                "Organiziation name of the author of the Document."));

            descDocVer.Add(new FormDescProperty(
                "OrganizationOfficialName", FormMultiplicity.One, defs.CD_OrganizationOfficialName?.GetSingleId(),
                "OrganizationOfficialName",
                "Official name of the organization of author of the Document " +
                "(which might be longer or include legal information)."));

            descDocVer.Add(new FormDescFile(
                "DigitalFile", FormMultiplicity.OneToMany, defs.CD_DigitalFile?.GetSingleId(), "DigitalFile{0:00}",
                "Digital file, which represents the Document and DocumentVersion. " +
                "A PDF/A format is required for textual representations."));

            descDocVer.Add(new FormDescFile(
                "PreviewFile", FormMultiplicity.ZeroToOne, defs.CD_PreviewFile?.GetSingleId(), "PreviewFile{0:00}",
                "Provides a preview image of the Document, e.g. first page, in a commonly used " +
                "image format and low resolution."));

            descDocVer.Add(new FormDescReferenceElement(
                "RefersTo", FormMultiplicity.ZeroToMany, defs.CD_RefersTo?.GetSingleId(),
                "RefersTo{0:00}",
                "Forms a generic RefersTo-relationship to another Document or DocumentVersion. " +
                "They have a loose relationship."));

            descDocVer.Add(new FormDescReferenceElement(
                "BasedOn", FormMultiplicity.ZeroToMany, defs.CD_BasedOn?.GetSingleId(),
                "BasedOn{0:00}",
                "Forms a BasedOn-relationship to another Document or DocumentVersion. Typically states, that the " +
                "content of the document bases on another document (e.g. specification requirements). Both have a " +
                "strong relationship."));

            descDocVer.Add(new FormDescReferenceElement(
                "TranslationOf", FormMultiplicity.ZeroToMany, defs.CD_TranslationOf?.GetSingleId(),
                "TranslationOf{0:00}",
                "Forms a TranslationOf-relationship to another Document or DocumentVersion. Both have a " +
                "strong relationship."));

            // back to Document

            descDoc.Add(new FormDescReferenceElement(
                "DocumentedEntity", FormMultiplicity.ZeroToOne, defs.CD_DocumentedEntity?.GetSingleId(),
                "DocumentedEntity",
                "Identifies the Entity, which is subject to the Documentation."));

            // end

            return descDoc;
        }

    }
}
