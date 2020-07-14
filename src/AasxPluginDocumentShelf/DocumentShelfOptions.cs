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

    public class DocumentShelfOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {


        //
        // Option fields
        //

        public List<AdminShell.Key> AllowSubmodelSemanticIds = new List<AdminShell.Key>();

        public AdminShell.Key SemIdDocumentation = null;
        public AdminShell.Key SemIdDocument = null;
        public AdminShell.Key SemIdDocumentIdValue = null;
        public AdminShell.Key SemIdDocumentClassId = null;
        public AdminShell.Key SemIdDocumentClassName = null;
        public AdminShell.Key SemIdDocumentClassificationSystem = null;
        public AdminShell.Key SemIdOrganizationName = null;
        public AdminShell.Key SemIdOrganizationOfficialName = null;
        public AdminShell.Key SemIdDocumentVersion = null;
        public AdminShell.Key SemIdLanguage = null;
        public AdminShell.Key SemIdTitle = null;
        public AdminShell.Key SemIdDate = null;
        public AdminShell.Key SemIdDocumentVersionIdValue = null;
        public AdminShell.Key SemIdDigitalFile = null;

        public AdminShell.Key SemIdDocumentId = null;
        public AdminShell.Key SemIdIsPrimaryDocumentId = null;
        public AdminShell.Key SemIdDocumentVersionId = null;
        public AdminShell.Key SemIdSummary = null;
        public AdminShell.Key SemIdKeywords = null;
        public AdminShell.Key SemIdStatusValue = null;
        public AdminShell.Key SemIdRole = null;
        public AdminShell.Key SemIdDomainId = null;
        public AdminShell.Key SemIdReferencedObject = null;

        public FormDescSubmodelElementCollection FormVdi2770 = null;

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static DocumentShelfOptions CreateDefault()
        {
            var opt = new DocumentShelfOptions();

            // use pre-definitions
            var preDefLib = new AasxPredefinedConcepts.DefinitionsVDI2770();
            var preDefs = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(preDefLib);

            opt.SemIdDocumentation = preDefs.SM_VDI2770_Documentation?.semanticId?.GetAsExactlyOneKey();
            if (opt.SemIdDocumentation != null)
                opt.AllowSubmodelSemanticIds.Add(opt.SemIdDocumentation);

            opt.SemIdDocument = preDefs.CD_VDI2770_Document?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentIdValue = preDefs.CD_VDI2770_DocumentIdValue?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentClassId = preDefs.CD_VDI2770_DocumentClassId?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentClassName = preDefs.CD_VDI2770_DocumentClassName?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentClassificationSystem =
                preDefs.CD_VDI2770_DocumentClassificationSystem?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdOrganizationName = preDefs.CD_VDI2770_OrganizationName?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdOrganizationOfficialName =
                preDefs.CD_VDI2770_OrganizationOfficialName?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentVersion = preDefs.CD_VDI2770_DocumentVersion?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdLanguage = preDefs.CD_VDI2770_Language?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdTitle = preDefs.CD_VDI2770_Title?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdDate = preDefs.CD_VDI2770_Date?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentVersionIdValue =
                preDefs.CD_VDI2770_DocumentVersionIdValue?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdDigitalFile = preDefs.CD_VDI2770_DigitalFile?.GetReference()?.GetAsExactlyOneKey();

            opt.FormVdi2770 = CreateVdi2770TemplateDesc(opt);

            /* new, Birgit */
            opt.SemIdDocumentId = preDefs.CD_VDI2770_DocumentId?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdIsPrimaryDocumentId =
                preDefs.CD_VDI2770_IsPrimaryDocumentId?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdDocumentVersionId = preDefs.CD_VDI2770_DocumentVersionId?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdSummary = preDefs.CD_VDI2770_Summary?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdKeywords = preDefs.CD_VDI2770_Keywords?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdStatusValue = preDefs.CD_VDI2770_StatusValue?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdRole = preDefs.CD_VDI2770_Role?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdDomainId = preDefs.CD_VDI2770_DomainId?.GetReference()?.GetAsExactlyOneKey();
            opt.SemIdReferencedObject = preDefs.CD_VDI2770_ReferencedObject?.GetReference()?.GetAsExactlyOneKey();

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

            // DocumentItem

            var descDoc = new FormDescSubmodelElementCollection(
                "Document", FormMultiplicity.ZeroToMany, opt.SemIdDocument, "Document{0:00}",
                "Each document item comprises a set of elements describing the information of a VDI 2770 Document " +
                "with directly attached DocumentVersion.");

            // Document

            descDoc.Add(new FormDescProperty(
                "DocumentId", FormMultiplicity.One, opt.SemIdDocumentId, "DocumentId",
                "The combination of DocumentId and DocumentVersionId shall be unqiue."));

            descDoc.Add(new FormDescProperty(
                "IsPrimary", FormMultiplicity.One, opt.SemIdIsPrimaryDocumentId, "IsPrimary",
                "True, if primary document id for the document."));

            var descDocClass = new FormDescProperty(
                "ClassId", FormMultiplicity.One, opt.SemIdDocumentClassId, "ClassId",
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
                "ClassName", FormMultiplicity.One, opt.SemIdDocumentClassName, "ClassName",
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
                    "ClassificationSystem", FormMultiplicity.One, opt.SemIdDocumentClassificationSystem,
                    "ClassificationSystem",
                    "This property is always set to VDI2770:2018", isReadOnly: true, presetValue: "VDI2770:2018"));

            descDoc.Add(new FormDescProperty(
                "ReferencedObject", FormMultiplicity.One, opt.SemIdReferencedObject, "ReferencedObject",
                "Reference to Asset or Entity, on which the Document is related to."));

            // DocumentVersion

            var descDocVer = new FormDescSubmodelElementCollection(
                "DocumentVersion", FormMultiplicity.OneToMany, opt.SemIdDocumentVersion, "DocumentVersion",
                "VDI2770 allows for multiple DocumentVersions for a document to be delivered.");
            descDoc.Add(descDocVer);

            descDocVer.Add(new FormDescProperty(
                "DocumentVersionId", FormMultiplicity.One, opt.SemIdDocumentVersionIdValue, "DocumentVersionId",
                "The combination of DocumentId and DocumentVersionId shall be unqiue."));

            descDocVer.Add(new FormDescProperty(
                "Languages", FormMultiplicity.ZeroToMany, opt.SemIdLanguage, "Language{0}",
                "List of languages used in the DocumentVersion. For most cases, " +
                "at least one language shall be given."));

            descDocVer.Add(new FormDescMultiLangProp(
                "Title", FormMultiplicity.One, opt.SemIdTitle, "Title",
                "Language dependent title of the document."));

            descDocVer.Add(new FormDescMultiLangProp(
                "Summary", FormMultiplicity.One, opt.SemIdSummary, "Summary",
                "Language dependent summary of the document."));

            descDocVer.Add(new FormDescMultiLangProp(
                "Keywords", FormMultiplicity.One, opt.SemIdKeywords, "Keywords",
                "Language dependent keywords of the document."));

            descDocVer.Add(new FormDescFile(
                "DigitalFile", FormMultiplicity.OneToMany, opt.SemIdDigitalFile, "DigitalFile{0:00}",
                "Digital file, which represents the Document and DocumentVersion. " +
                "A PDF/A format is required for textual representations."));

            descDocVer.Add(new FormDescProperty(
                "SetDate", FormMultiplicity.One, opt.SemIdDate, "SetDate",
                "Date when the document was introduced into the AAS or set to the status. Format is YYYY-MM-dd."));

            var descStatus = new FormDescProperty(
                "StatusValue", FormMultiplicity.One, opt.SemIdStatusValue, "StatusValue",
                "Each document version represents a point in time in the document life cycle. " +
                "This status value refers to the milestones in the document life cycle. " +
                "The following two statuses should be used for the application of this guideline: " +
                "InReview (under review), Released (released).");
            descDocVer.Add(descStatus);
            descStatus.comboBoxChoices = new[] { "InReview", "Released" };

            var descRole = new FormDescProperty(
                "Role", FormMultiplicity.One, opt.SemIdRole, "Role",
                "Define a role for the organisation according to the following selection list.");
            descDocVer.Add(descRole);
            descRole.comboBoxChoices = new[] { "Author", "Customer", "Supplier", "Manufacturer", "Responsible" };

            descDocVer.Add(new FormDescProperty(
                "OrganizationName", FormMultiplicity.One, opt.SemIdOrganizationName, "OrganizationName",
                "Common name for the organisation."));

            descDocVer.Add(new FormDescProperty(
                "OrganizationOfficialName", FormMultiplicity.One, opt.SemIdOrganizationOfficialName,
                "OrganizationOfficialName",
                "Official name for the organisation (which might be longer or include legal information)."));

            return descDoc;
        }

    }
}
