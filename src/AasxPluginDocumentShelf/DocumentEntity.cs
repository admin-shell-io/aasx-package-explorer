/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using AasxIntegrationBase;
using AasxIntegrationBaseGdi;
using AasxPredefinedConcepts;
using AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
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

        public enum SubmodelVersion { Default = 0, V10 = 1, V11 = 2 }

        public SubmodelVersion SmVersion = SubmodelVersion.Default;

        public string Title = "";
        public string Organization = "";
        public string FurtherInfo = "";
        public string[] CountryCodes;
        public FileInfo DigitalFile, PreviewFile;

#if USE_WPF
        public System.Windows.Controls.Viewbox ImgContainerWpf = null;
#endif
        public AnyUiImage ImgContainerAnyUi = null;
        public string ReferableHash = null;

        public List<ISubmodelElement> SourceElementsDocument = null;
        public List<ISubmodelElement> SourceElementsDocumentVersion = null;

        public string ImageReadyToBeLoaded = null; // adding Image to ImgContainer needs to be done by the GUI thread!!
        public string[] DeleteFilesAfterLoading = null;

        public enum DocRelationType { DocumentedEntity, RefersTo, BasedOn, Affecting, TranslationOf };
        public List<Tuple<DocRelationType, Reference>> Relations =
            new List<Tuple<DocRelationType, Reference>>();

        public class FileInfo
        {
            public string Path = "";
            public string MimeType = "";

            public FileInfo() { }

            public FileInfo(AasCore.Aas3_0_RC02.File file)
            {
                Path = file?.Value;
                MimeType = file?.ContentType;
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
        public AnyUiBitmapInfo LoadImageFromPath(string fn)
        {
            // be a bit suspicous ..
            if (!System.IO.File.Exists(fn))
                return null;

            // convert here, as the tick-Thread in STA / UI thread
            try
            {
#if USE_WPF
                var bi = new BitmapImage(new Uri(fn, UriKind.RelativeOrAbsolute));

                if (ImgContainerWpf != null)
                {
                    var img = new Image();
                    img.Source = bi;
                    ImgContainerWpf.Child = img;
                }

                if (ImgContainerAnyUi != null)
                {
                    ImgContainerAnyUi.BitmapInfo = AnyUiHelper.CreateAnyUiBitmapInfo(bi);
                }
                return bi;
#else
                ImgContainerAnyUi.BitmapInfo = AnyUiGdiHelper.CreateAnyUiBitmapInfo(fn);
#endif
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
            Submodel subModel, AasxPluginDocumentShelf.DocumentShelfOptions options,
            string defaultLang,
            int selectedDocClass, AasxLanguageHelper.LangEnum selectedLanguage)
        {
            // set a new list
            var its = new ListOfDocumentEntity();

            // look for Documents
            if (subModel?.SubmodelElements != null)
                foreach (var smcDoc in
                    subModel.SubmodelElements.FindAllSemanticIdAs<SubmodelElementCollection>(
                        _semConfigV10.SemIdDocument, MatchMode.Relaxed))
                {
                    // access
                    if (smcDoc == null || smcDoc.Value == null)
                        continue;

                    // look immediately for DocumentVersion, as only with this there is a valid List item
                    foreach (var smcVer in
                        smcDoc.Value.FindAllSemanticIdAs<SubmodelElementCollection>(
                            _semConfigV10.SemIdDocumentVersion, MatchMode.Relaxed))
                    {
                        // access
                        if (smcVer == null || smcVer.Value == null)
                            continue;

                        //
                        // try to lookup info in smcDoc and smcVer
                        //

                        // take the 1st title
                        var title =
                            "" +
                            smcVer.Value.FindFirstSemanticIdAs<Property>(_semConfigV10.SemIdTitle,
                            MatchMode.Relaxed)?.Value;

                        // could be also a multi-language title
                        foreach (var mlp in
                            smcVer.Value.FindAllSemanticIdAs<MultiLanguageProperty>(
                                _semConfigV10.SemIdTitle, MatchMode.Relaxed))
                            if (mlp.Value != null)
                                title = mlp.Value.GetDefaultString(defaultLang);

                        // have multiple opportunities for orga
                        var orga =
                            "" +
                            smcVer.Value.FindFirstSemanticIdAs<Property>(
                                _semConfigV10.SemIdOrganizationOfficialName, MatchMode.Relaxed)?
                                .Value;
                        if (orga.Trim().Length < 1)
                            orga =
                                "" +
                                smcVer.Value.FindFirstSemanticIdAs<Property>(
                                    _semConfigV10.SemIdOrganizationName, MatchMode.Relaxed)?
                                    .Value;

                        // class infos
                        var classId =
                            "" +
                            smcDoc.Value.FindFirstSemanticIdAs<Property>(
                                _semConfigV10.SemIdDocumentClassId, MatchMode.Relaxed)?.Value;

                        // collect country codes
                        var countryCodesStr = new List<string>();
                        var countryCodesEnum = new List<AasxLanguageHelper.LangEnum>();
                        foreach (var cclp in
                            smcVer.Value.FindAllSemanticIdAs<Property>(_semConfigV10.SemIdLanguage,
                            MatchMode.Relaxed))
                        {
                            // language code
                            var candidate = "" + cclp.Value;
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
                            smcVer.Value.FindAllSemanticIdAs<Property>(
                                _semConfigV10.SemIdDocumentVersionIdValue))
                            further += "\u00b7 version: " + fi.Value;
                        foreach (var fi in
                            smcVer.Value.FindAllSemanticIdAs<Property>(_semConfigV10.SemIdDate,
                            MatchMode.Relaxed))
                            further += "\u00b7 date: " + fi.Value;
                        if (further.Length > 0)
                            further = further.Substring(2);

                        // construct entity
                        var ent = new DocumentEntity(title, orga, further, countryCodesStr.ToArray());
                        ent.ReferableHash = String.Format(
                            "{0:X14} {1:X14}", thePackage.GetHashCode(), smcDoc.GetHashCode());

                        // for updating data, set the source elements of this document entity
                        ent.SourceElementsDocument = smcDoc.Value;
                        ent.SourceElementsDocumentVersion = smcVer.Value;

                        // filename
                        var fl = smcVer.Value.FindFirstSemanticIdAs<File>(
                            _semConfigV10.SemIdDigitalFile, MatchMode.Relaxed);

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
            List<ISubmodelElement> smwc,
            DocumentEntity.DocRelationType drt,
            Reference semId,
            DocumentEntity intoDoc)
        {
            // access
            if (smwc == null || semId == null || intoDoc == null)
                return;

            foreach (var re in smwc.FindAllSemanticIdAs<ReferenceElement>(semId,
                MatchMode.Relaxed))
            {
                // access 
                if (re.Value == null || re.Value.Count() < 1)
                    continue;

                // be a bit picky
                if (re.Value.Last().Type != KeyTypes.Entity)
                    continue;

                // add
                intoDoc.Relations.Add(new Tuple<DocumentEntity.DocRelationType, Reference>(
                    drt, re.Value));
            }
        }

        public static ListOfDocumentEntity ParseSubmodelForV11(
            AdminShellPackageEnv thePackage,
            Submodel subModel, AasxPredefinedConcepts.VDI2770v11 defs11,
            string defaultLang,
            int selectedDocClass, AasxLanguageHelper.LangEnum selectedLanguage)
        {
            // set a new list
            var its = new ListOfDocumentEntity();
            if (thePackage == null || subModel == null || defs11 == null)
                return its;

            // look for Documents
            if (subModel.SubmodelElements != null)
                foreach (var smcDoc in
                    subModel.SubmodelElements.FindAllSemanticIdAs<SubmodelElementCollection>(
                        defs11.CD_Document?.GetReference(), MatchMode.Relaxed))
                {
                    // access
                    if (smcDoc == null || smcDoc.Value == null)
                        continue;

                    // look immediately for DocumentVersion, as only with this there is a valid List item
                    foreach (var smcVer in
                        smcDoc.Value.FindAllSemanticIdAs<SubmodelElementCollection>(
                            defs11.CD_DocumentVersion?.GetReference(), MatchMode.Relaxed))
                    {
                        // access
                        if (smcVer == null || smcVer.Value == null)
                            continue;

                        //
                        // try to lookup info in smcDoc and smcVer
                        //

                        // take the 1st title
                        var title = "" + smcVer.Value.FindFirstSemanticIdAs<Property>(
                                defs11.CD_Title?.GetReference(), MatchMode.Relaxed)?.Value;

                        // could be also a multi-language title
                        foreach (var mlp in
                            smcVer.Value.FindAllSemanticIdAs<MultiLanguageProperty>(
                                defs11.CD_Title?.GetReference(), MatchMode.Relaxed))
                            if (mlp.Value != null)
                                title = mlp.Value.GetDefaultString(defaultLang);

                        // some with sub title
                        var subTitle = "" + smcVer.Value.FindFirstSemanticIdAs<Property>(
                                defs11.CD_SubTitle?.GetReference(), MatchMode.Relaxed)?.Value;
                        foreach (var mlp in
                            smcVer.Value.FindAllSemanticIdAs<MultiLanguageProperty>(
                                defs11.CD_SubTitle?.GetReference(), MatchMode.Relaxed))
                            if (mlp.Value != null)
                                subTitle = mlp.Value.GetDefaultString(defaultLang);

                        if (subTitle.HasContent())
                            title += " \u2012 " + subTitle;

                        // have multiple opportunities for orga
                        var orga = "" + smcVer.Value.FindFirstSemanticIdAs<Property>(
                                defs11.CD_OrganizationOfficialName?.GetReference(),
                                MatchMode.Relaxed)?.Value;
                        if (orga.Trim().Length < 1)
                            orga = "" + smcVer.Value.FindFirstSemanticIdAs<Property>(
                                    defs11.CD_OrganizationName?.GetReference(),
                                    MatchMode.Relaxed)?.Value;

                        // try find language
                        // collect country codes
                        var countryCodesStr = new List<string>();
                        var countryCodesEnum = new List<AasxLanguageHelper.LangEnum>();
                        foreach (var cclp in
                            smcVer.Value.FindAllSemanticIdAs<Property>(defs11.CD_Language?.GetReference(),
                            MatchMode.Relaxed))
                        {
                            // language code
                            var candidate = "" + cclp.Value;
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
                            smcDoc.Value.FindAllSemanticIdAs<SubmodelElementCollection>(
                                defs11.CD_DocumentClassification?.GetReference(), MatchMode.Relaxed))
                        {
                            // access
                            if (smcClass?.Value == null)
                                continue;

                            // shall be a 2770 classification
                            var classSys = "" + smcClass.Value.FindFirstSemanticIdAs<Property>(
                                    defs11.CD_ClassificationSystem?.GetReference(),
                                    MatchMode.Relaxed)?.Value;
                            if (classSys.ToLower().Trim() != VDI2770v11.Vdi2770Sys.ToLower())
                                continue;

                            // class id
                            var classId = "" + smcClass.Value.FindFirstSemanticIdAs<Property>(
                                    defs11.CD_ClassId?.GetReference(),
                                    MatchMode.Relaxed)?.Value;

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
                            smcVer.Value.FindAllSemanticIdAs<Property>(
                                defs11.CD_DocumentVersionId?.GetReference()))
                            further += " \u00b7 version: " + fi.Value;
                        foreach (var fi in
                            smcVer.Value.FindAllSemanticIdAs<Property>(
                                defs11.CD_DocumentIdValue?.GetReference()))
                            further += " \u00b7 id: " + fi.Value;
                        foreach (var fi in
                            smcVer.Value.FindAllSemanticIdAs<Property>(defs11.CD_SetDate?.GetReference(),
                            MatchMode.Relaxed))
                            further += " \u00b7 date: " + fi.Value;
                        if (further.Length > 0)
                            further = further.Substring(2);

                        // construct entity
                        var ent = new DocumentEntity(title, orga, further, countryCodesStr.ToArray());
                        ent.ReferableHash = String.Format(
                            "{0:X14} {1:X14}", thePackage.GetHashCode(), smcDoc.GetHashCode());

                        // for updating data, set the source elements of this document entity
                        ent.SourceElementsDocument = smcDoc.Value;
                        ent.SourceElementsDocumentVersion = smcVer.Value;

                        // file informations
                        var fl = smcVer.Value.FindFirstSemanticIdAs<File>(
                            defs11.CD_DigitalFile?.GetReference(), MatchMode.Relaxed);
                        if (fl != null)
                            ent.DigitalFile = new DocumentEntity.FileInfo(fl);

                        fl = smcVer.Value.FindFirstSemanticIdAs<File>(
                            defs11.CD_PreviewFile?.GetReference(), MatchMode.Relaxed);
                        if (fl != null)
                            ent.PreviewFile = new DocumentEntity.FileInfo(fl);

                        // relations
                        SearchForRelations(smcVer.Value, DocumentEntity.DocRelationType.DocumentedEntity,
                            defs11.CD_DocumentedEntity?.GetReference(), ent);
                        SearchForRelations(smcVer.Value, DocumentEntity.DocRelationType.RefersTo,
                            defs11.CD_RefersTo?.GetReference(), ent);
                        SearchForRelations(smcVer.Value, DocumentEntity.DocRelationType.BasedOn,
                            defs11.CD_BasedOn?.GetReference(), ent);
                        SearchForRelations(smcVer.Value, DocumentEntity.DocRelationType.TranslationOf,
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
