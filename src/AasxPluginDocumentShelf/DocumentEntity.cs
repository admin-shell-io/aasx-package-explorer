/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using AasxIntegrationBase;
using AasxPredefinedConcepts;
using AdminShellNS;
using AnyUi;

namespace AasxPluginDocumentShelf
{
    /// <summary>
    /// Representation of a VDI2770 Document or so ..
    /// </summary>
    public class DocumentEntity
    {
        public delegate void DocumentEntityEvent(DocumentEntity e);
        public event DocumentEntityEvent DoubleClick = null;

        public delegate void MenuClickDelegate(DocumentEntity e, string menuItemHeader, object tag);
        public event MenuClickDelegate MenuClick = null;

        public event DocumentEntityEvent DragStart = null;

        public enum SubmodelVersion { Default= 0, V10 = 1, V11 = 2 }

        public SubmodelVersion SmVersion = SubmodelVersion.Default;

        public string Title = "";
        public string Organization = "";
        public string FurtherInfo = "";
        public string[] CountryCodes;
        public FileInfo DigitalFile, PreviewFile;
        public System.Windows.Controls.Viewbox ImgContainerWpf = null;
        public AnyUiImage ImgContainerAnyUi = null;
        public string ReferableHash = null;

        public AdminShell.SubmodelElementWrapperCollection SourceElementsDocument = null;
        public AdminShell.SubmodelElementWrapperCollection SourceElementsDocumentVersion = null;

        public string ImageReadyToBeLoaded = null; // adding Image to ImgContainer needs to be done by the GUI thread!!
        public string[] DeleteFilesAfterLoading = null;

        public enum DocRelationType { DocumentedEntity, RefersTo, BasedOn, Affecting, TranslationOf };
        public List<Tuple<DocRelationType, AdminShell.Reference>> Relations =
            new List<Tuple<DocRelationType, AdminShellV20.Reference>>();

        public class FileInfo
        {
            public string Path = "";
            public string MimeType = "";

            public FileInfo() { }

            public FileInfo(AdminShell.File file)
            {
                Path = file?.value;
                MimeType = file?.mimeType;
            }
        }

        public DocumentEntity() { }

        public DocumentEntity(string Title, string Organization, string FurtherInfo, string[] LangCodes = null)
        {
            this.Title = Title;
            this.Organization = Organization;
            this.FurtherInfo = FurtherInfo;
            this.CountryCodes = LangCodes;
        }

        public void RaiseDoubleClick()
        {
            if (DoubleClick != null)
                DoubleClick(this);
        }

        public void RaiseMenuClick(string menuItemHeader, object tag)
        {
            MenuClick?.Invoke(this, menuItemHeader, tag);
        }

        public void RaiseDragStart()
        {
            DragStart?.Invoke(this);
        }

        /// <summary>
        /// This function needs to be called as part of tick-Thread in STA / UI thread
        /// </summary>
        public BitmapImage LoadImageFromPath(string fn)
        {
            // be a bit suspicous ..
            if (!File.Exists(fn))
                return null;

            // convert here, as the tick-Thread in STA / UI thread
            try
            {
                var bi = new BitmapImage(new Uri(fn, UriKind.RelativeOrAbsolute));

                if (ImgContainerWpf != null)
                {
                    var img = new Image();
                    img.Source = bi;
                    ImgContainerWpf.Child = img;
                }

                if (ImgContainerAnyUi != null)
                {
                    ImgContainerAnyUi.Bitmap = bi;
                }

                return bi;
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }
            return null;
        }
    }

    public class ListOfDocumentEntity : List<DocumentEntity>
    {
        private static DocuShelfSemanticConfig _semConfigV10 = DocuShelfSemanticConfig.Singleton;

        //
        // Default
        //

        public static ListOfDocumentEntity ParseSubmodelForV10(
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel subModel, AasxPluginDocumentShelf.DocumentShelfOptions options,
            string defaultLang,
            int selectedDocClass, AasxLanguageHelper.LangEnum selectedLanguage)
        {
            // set a new list
            var its = new ListOfDocumentEntity();

            // look for Documents
            if (subModel?.submodelElements != null)
                foreach (var smcDoc in
                    subModel.submodelElements.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        _semConfigV10.SemIdDocument, AdminShellV20.Key.MatchMode.Relaxed))
                {
                    // access
                    if (smcDoc == null || smcDoc.value == null)
                        continue;

                    // look immediately for DocumentVersion, as only with this there is a valid List item
                    foreach (var smcVer in
                        smcDoc.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                            _semConfigV10.SemIdDocumentVersion, AdminShellV20.Key.MatchMode.Relaxed))
                    {
                        // access
                        if (smcVer == null || smcVer.value == null)
                            continue;

                        //
                        // try to lookup info in smcDoc and smcVer
                        //

                        // take the 1st title
                        var title =
                            "" +
                            smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(_semConfigV10.SemIdTitle,
                            AdminShellV20.Key.MatchMode.Relaxed)?.value;

                        // could be also a multi-language title
                        foreach (var mlp in
                            smcVer.value.FindAllSemanticIdAs<AdminShell.MultiLanguageProperty>(
                                _semConfigV10.SemIdTitle, AdminShellV20.Key.MatchMode.Relaxed))
                            if (mlp.value != null)
                                title = mlp.value.GetDefaultStr(defaultLang);

                        // have multiple opportunities for orga
                        var orga =
                            "" +
                            smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                _semConfigV10.SemIdOrganizationOfficialName, AdminShellV20.Key.MatchMode.Relaxed)?.value;
                        if (orga.Trim().Length < 1)
                            orga =
                                "" +
                                smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    _semConfigV10.SemIdOrganizationName, AdminShellV20.Key.MatchMode.Relaxed)?.value;

                        // class infos
                        var classId =
                            "" +
                            smcDoc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                _semConfigV10.SemIdDocumentClassId, AdminShellV20.Key.MatchMode.Relaxed)?.value;

                        // collect country codes
                        var countryCodesStr = new List<string>();
                        var countryCodesEnum = new List<AasxLanguageHelper.LangEnum>();
                        foreach (var cclp in
                            smcVer.value.FindAllSemanticIdAs<AdminShell.Property>(_semConfigV10.SemIdLanguage,
                            AdminShellV20.Key.MatchMode.Relaxed))
                        {
                            // language code
                            var candidate = "" + cclp.value;
                            if (candidate.Length < 1)
                                continue;

                            // convert to country codes and add
                            var le = AasxLanguageHelper.FindLangEnumFromLangCode(candidate);
                            if (le != AasxLanguageHelper.LangEnum.Any)
                            {
                                countryCodesEnum.Add(le);
                                countryCodesStr.Add(AasxLanguageHelper.GetCountryCodeFromEnum(le));
                            }
                        }

                        // evaluate, if in selection
                        var okDocClass =
                            selectedDocClass < 1 || classId.Trim().Length < 1 ||
                            classId.Trim()
                                .StartsWith(
                                    DefinitionsVDI2770.GetDocClass(
                                        (DefinitionsVDI2770.Vdi2770DocClass)selectedDocClass));

                        var okLanguage =
                            selectedLanguage == AasxLanguageHelper.LangEnum.Any ||
                            // make only exception, if no language not all (not only the preferred
                            // of LanguageSelectionToISO639String) are in the property
                            countryCodesStr.Count < 1 ||
                            countryCodesEnum.Contains(selectedLanguage);

                        if (!okDocClass || !okLanguage)
                            continue;

                        // further info
                        var further = "";
                        foreach (var fi in
                            smcVer.value.FindAllSemanticIdAs<AdminShell.Property>(
                                _semConfigV10.SemIdDocumentVersionIdValue))
                            further += "\u00b7 version: " + fi.value;
                        foreach (var fi in
                            smcVer.value.FindAllSemanticIdAs<AdminShell.Property>(_semConfigV10.SemIdDate,
                            AdminShellV20.Key.MatchMode.Relaxed))
                            further += "\u00b7 date: " + fi.value;
                        if (further.Length > 0)
                            further = further.Substring(2);

                        // construct entity
                        var ent = new DocumentEntity(title, orga, further, countryCodesStr.ToArray());
                        ent.ReferableHash = String.Format(
                            "{0:X14} {1:X14}", thePackage.GetHashCode(), smcDoc.GetHashCode());

                        // for updating data, set the source elements of this document entity
                        ent.SourceElementsDocument = smcDoc.value;
                        ent.SourceElementsDocumentVersion = smcVer.value;

                        // filename
                        var fl = smcVer.value.FindFirstSemanticIdAs<AdminShell.File>(
                            _semConfigV10.SemIdDigitalFile, AdminShellV20.Key.MatchMode.Relaxed);

                        ent.DigitalFile = new DocumentEntity.FileInfo(fl);

                        // add
                        ent.SmVersion = DocumentEntity.SubmodelVersion.Default;
                        its.Add(ent);
                    }
                }

            // ok
            return its;
        }

        //
        // V11
        //

        private static void SearchForRelations(
            AdminShell.SubmodelElementWrapperCollection smwc,
            DocumentEntity.DocRelationType drt,
            AdminShell.Reference semId,
            DocumentEntity intoDoc)
        {
            // access
            if (smwc == null || semId == null || intoDoc == null)
                return;

            foreach (var re in smwc.FindAllSemanticIdAs<AdminShell.ReferenceElement>(semId,
                AdminShellV20.Key.MatchMode.Relaxed))
            {
                // access 
                if (re.value == null || re.value.Count < 1)
                    continue;

                // be a bit picky
                if (re.value.Last.type.ToLower().Trim() != AdminShell.Key.Entity.ToLower())
                    continue;

                // add
                intoDoc.Relations.Add(new Tuple<DocumentEntity.DocRelationType, AdminShellV20.Reference>(
                    drt, re.value));
            }
        }

        public static ListOfDocumentEntity ParseSubmodelForV11(
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel subModel, AasxPredefinedConcepts.VDI2770v11 defs11,
            string defaultLang,
            int selectedDocClass, AasxLanguageHelper.LangEnum selectedLanguage)
        {
            // set a new list
            var its = new ListOfDocumentEntity();
            if (thePackage == null || subModel == null || defs11 == null)
                return its;

            // look for Documents
            if (subModel.submodelElements != null)
                foreach (var smcDoc in
                    subModel.submodelElements.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        defs11.CD_Document?.GetReference(), AdminShellV20.Key.MatchMode.Relaxed))
                {
                    // access
                    if (smcDoc == null || smcDoc.value == null)
                        continue;

                    // look immediately for DocumentVersion, as only with this there is a valid List item
                    foreach (var smcVer in
                        smcDoc.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                            defs11.CD_DocumentVersion?.GetReference(), AdminShellV20.Key.MatchMode.Relaxed))
                    {
                        // access
                        if (smcVer == null || smcVer.value == null)
                            continue;

                        //
                        // try to lookup info in smcDoc and smcVer
                        //

                        // take the 1st title
                        var title = "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                defs11.CD_Title?.GetReference(), AdminShellV20.Key.MatchMode.Relaxed)?.value;

                        // could be also a multi-language title
                        foreach (var mlp in
                            smcVer.value.FindAllSemanticIdAs<AdminShell.MultiLanguageProperty>(
                                defs11.CD_Title?.GetReference(), AdminShellV20.Key.MatchMode.Relaxed))
                            if (mlp.value != null)
                                title = mlp.value.GetDefaultStr(defaultLang);

                        // have multiple opportunities for orga
                        var orga = "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                defs11.CD_OrganizationOfficialName?.GetReference(),
                                AdminShellV20.Key.MatchMode.Relaxed)?.value;
                        if (orga.Trim().Length < 1)
                            orga = "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defs11.CD_OrganizationName?.GetReference(),
                                    AdminShellV20.Key.MatchMode.Relaxed)?.value;

                        // try find language
                        // collect country codes
                        var countryCodesStr = new List<string>();
                        var countryCodesEnum = new List<AasxLanguageHelper.LangEnum>();
                        foreach (var cclp in
                            smcVer.value.FindAllSemanticIdAs<AdminShell.Property>(defs11.CD_Language?.GetReference(),
                            AdminShellV20.Key.MatchMode.Relaxed))
                        {
                            // language code
                            var candidate = "" + cclp.value;
                            if (candidate.Length < 1)
                                continue;

                            // convert to country codes and add
                            var le = AasxLanguageHelper.FindLangEnumFromLangCode(candidate);
                            if (le != AasxLanguageHelper.LangEnum.Any)
                            {
                                countryCodesEnum.Add(le);
                                countryCodesStr.Add(AasxLanguageHelper.GetCountryCodeFromEnum(le));
                            }
                        }

                        var okLanguage =
                            (selectedLanguage == AasxLanguageHelper.LangEnum.Any ||
                            countryCodesEnum == null ||
                            // make only exception, if no language not all (not only the preferred
                            // of LanguageSelectionToISO639String) are in the property
                            countryCodesStr.Count < 1 ||
                            countryCodesEnum.Contains(selectedLanguage));

                        // try find a 2770 classification
                        var okDocClass = false;
                        foreach (var smcClass in
                            smcDoc.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                                defs11.CD_DocumentClassification?.GetReference(), AdminShellV20.Key.MatchMode.Relaxed))
                        {
                            // access
                            if (smcClass?.value == null)
                                continue;

                            // shall be a 2770 classification
                            var classSys = "" + smcClass.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defs11.CD_ClassificationSystem?.GetReference(),
                                    AdminShellV20.Key.MatchMode.Relaxed)?.value;
                            if (classSys.ToLower().Trim() != VDI2770v11.Vdi2770Sys.ToLower())
                                continue;

                            // class infos
                            var classId = "" + smcClass.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defs11.CD_ClassId?.GetReference(),
                                    AdminShellV20.Key.MatchMode.Relaxed)?.value;

                            // evaluate, if in selection
                            okDocClass = okDocClass ||
                                (classId.Trim().Length < 1 ||
                                classId.Trim()
                                    .StartsWith(
                                        DefinitionsVDI2770.GetDocClass(
                                            (DefinitionsVDI2770.Vdi2770DocClass)selectedDocClass)));

                        }

                        // success for selections?
                        if (!(selectedDocClass < 1 || okDocClass) || !okLanguage)
                            continue;

                        // further info
                        var further = "";
                        foreach (var fi in
                            smcVer.value.FindAllSemanticIdAs<AdminShell.Property>(
                                defs11.CD_DocumentVersionId?.GetReference()))
                            further += " \u00b7 version: " + fi.value;
                        foreach (var fi in
                            smcVer.value.FindAllSemanticIdAs<AdminShell.Property>(
                                defs11.CD_DocumentIdValue?.GetReference()))
                            further += " \u00b7 id: " + fi.value;
                        foreach (var fi in
                            smcVer.value.FindAllSemanticIdAs<AdminShell.Property>(defs11.CD_SetDate?.GetReference(),
                            AdminShellV20.Key.MatchMode.Relaxed))
                            further += " \u00b7 date: " + fi.value;
                        if (further.Length > 0)
                            further = further.Substring(2);

                        // construct entity
                        var ent = new DocumentEntity(title, orga, further, countryCodesStr.ToArray());
                        ent.ReferableHash = String.Format(
                            "{0:X14} {1:X14}", thePackage.GetHashCode(), smcDoc.GetHashCode());

                        // for updating data, set the source elements of this document entity
                        ent.SourceElementsDocument = smcDoc.value;
                        ent.SourceElementsDocumentVersion = smcVer.value;

                        // file informations
                        var fl = smcVer.value.FindFirstSemanticIdAs<AdminShell.File>(
                            defs11.CD_DigitalFile?.GetReference(), AdminShellV20.Key.MatchMode.Relaxed);
                        if (fl != null)
                            ent.DigitalFile = new DocumentEntity.FileInfo(fl);

                        fl = smcVer.value.FindFirstSemanticIdAs<AdminShell.File>(
                            defs11.CD_PreviewFile?.GetReference(), AdminShellV20.Key.MatchMode.Relaxed);
                        if (fl != null)
                            ent.PreviewFile = new DocumentEntity.FileInfo(fl);

                        // relations
                        SearchForRelations(smcVer.value, DocumentEntity.DocRelationType.DocumentedEntity,
                            defs11.CD_DocumentedEntity?.GetReference(), ent);
                        SearchForRelations(smcVer.value, DocumentEntity.DocRelationType.RefersTo,
                            defs11.CD_RefersTo?.GetReference(), ent);
                        SearchForRelations(smcVer.value, DocumentEntity.DocRelationType.BasedOn,
                            defs11.CD_BasedOn?.GetReference(), ent);
                        SearchForRelations(smcVer.value, DocumentEntity.DocRelationType.TranslationOf,
                            defs11.CD_TranslationOf?.GetReference(), ent);

                        // add
                        ent.SmVersion = DocumentEntity.SubmodelVersion.V11;
                        its.Add(ent);
                    }
                }

            // ok
            return its;
        }

    }
}
