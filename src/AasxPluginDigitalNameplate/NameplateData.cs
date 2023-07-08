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
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using System.Threading.Tasks;

namespace AasxPluginDigitalNameplate
{
    /// <summary>
    /// Nameplate information collected from different versions
    /// </summary>
    public class NameplateData
    {
        public string URIOfTheProduct = "";
        public string ManufacturerName = "";
        public string ManufacturerProductDesignation = "";
        public List<string> ContactInformation = null;

        public string ManufacturerProductRoot = "";
        public string ManufacturerProductFamily = "";
        public string ManufacturerProductType = "";
        public string OrderCodeOfManufacturer = "";
        public string ProductArticleNumberOfManufacturer = "";
        public string SerialNumber = "";
        public string YearOfConstruction = "";
        public string DateOfManufacture = "";
        public string HardwareVersion = "";
        public string FirmwareVersion = "";
        public string SoftwareVersion = "";
        public string CountryOfOrigin = "";

        public string ExplSafetyStr = "(not analyzed)";
		
		public Aas.IFile CompanyLogo = null;

        public class MarkingInfo
        {
            public string Name = null;
            public Aas.IFile File = null;
            public List<string> AddText = null;
        }

        public List<MarkingInfo> Markings = null;

        public NameplateData() { }

        //
        // various
        //

        public static string TryGuessAssetId(
            AdminShellPackageEnv package,
            Aas.Submodel subModel)
        {
            // access
            if (package?.AasEnv == null || subModel == null)
                return null;

            // check all Submodels
            var aas = package.AasEnv.FindAasWithSubmodelId(subModel.Id);

            // give back
            return aas?.AssetInformation?.GlobalAssetId;
        }

        //
        // V1.0
        //

        public static NameplateData ParseSubmodelForV10(
            AdminShellPackageEnv thePackage,
            Aas.Submodel subModel, DigitalNameplateOptions options,
            string defaultLang = null)
        {
            // access 
            if (subModel == null || options == null)
                return null;

            // make result
            var res = new NameplateData();

            // shortcuts
            var defs = AasxPredefinedConcepts.ZveiNameplateV10.Static;
            var mm = MatchMode.Relaxed;

            // SME on top level
            res.URIOfTheProduct = "" + TryGuessAssetId(thePackage, subModel);

            res.ManufacturerName = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(defs.CD_ManNam?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.ManufacturerProductDesignation = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(defs.CD_ManProDes?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.ManufacturerProductFamily = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(defs.CD_ManProFam?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.SerialNumber = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IProperty>(defs.CD_SerNum?.GetSingleKey(), mm)?
                .Value;

            res.YearOfConstruction = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IProperty>(defs.CD_YeaOfCon?.GetSingleKey(), mm)?
                .Value;

            // find contact info?
            var smcContInf = subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Add?.GetSingleKey(), mm);
            if (smcContInf?.Value != null)
            {
                res.ContactInformation = new List<string>();

                Action<List<Aas.ISubmodelElement>, string, Aas.IKey> tryAdd = (coll, header, key) =>
                {
                    var st = coll?
                        .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(key, mm)?
                        .Value?.GetDefaultString(defaultLang);
                    if (st?.HasContent() == true)
                        res.ContactInformation.Add(("" + header) + st);
                };

                tryAdd(smcContInf?.Value, null, defs.CD_ZipCodOfPOBox?.GetSingleKey());
                tryAdd(smcContInf?.Value, null, defs.CD_POBox?.GetSingleKey());
                tryAdd(smcContInf?.Value, null, defs.CD_Str?.GetSingleKey());
                tryAdd(smcContInf?.Value, null, defs.CD_CitTow?.GetSingleKey());
                tryAdd(smcContInf?.Value, null, defs.CD_StaCou?.GetSingleKey());
                tryAdd(smcContInf?.Value, null, defs.CD_NatCod?.GetSingleKey());
                tryAdd(smcContInf?.Value, "VAT: ", defs.CD_VATNum?.GetSingleKey());
                tryAdd(smcContInf?.Value, null, defs.CD_AddOfAddLin?.GetSingleKey());

                // Phone

                var smc2 = smcContInf?.Value?
                    .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Pho?.GetSingleKey(), mm);
                if (smc2 != null)
                    tryAdd(smc2.Value, null, defs.CD_TelNum?.GetSingleKey());

                // Fax

                smc2 = smcContInf?.Value?
                    .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Fax?.GetSingleKey(), mm);
                if (smc2 != null)
                    tryAdd(smc2.Value, null, defs.CD_FaxNum?.GetSingleKey());

                // Email

                smc2 = smcContInf?.Value?
                    .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Ema?.GetSingleKey(), mm);
                if (smc2 != null)
                    tryAdd(smc2.Value, null, defs.CD_EmaAdd?.GetSingleKey());
            }

            // find markings?
            var smcMarkings = subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Markings?.GetSingleKey(), mm);
            if (smcMarkings?.Value != null)
            {
                res.Markings = new List<MarkingInfo>();

                foreach (var smcMark in smcMarkings.Value
                    .FindAllSemanticIdAs<Aas.SubmodelElementCollection>(defs.CD_Marking?.GetSingleKey(), mm))
                {
                    var mi = new MarkingInfo();
                    res.Markings.Add(mi);

                    mi.Name = "" + smcMark?.Value
                        .FindFirstSemanticIdAs<Aas.IProperty>(defs.CD_MarkingName?.GetSingleKey(), mm)?
                        .Value;

                    mi.File = smcMark?.Value
                        .FindFirstSemanticIdAs<Aas.IFile>(defs.CD_MarkingFile?.GetSingleKey(), mm);

                    mi.AddText = new List<string>();
                    foreach (var mat in smcMark?.Value
                        .FindAllSemanticIdAs<Aas.IProperty>(defs.CD_MarkingAdditionalText?.GetSingleKey(), mm))
                    {
                        mi.AddText.Add("" + mat?.Value);
                    }
                }
            }

            // done
            return res;
        }

        //
        // V20
        //

        public static NameplateData ParseSubmodelForV20(
            AdminShellPackageEnv thePackage,
            Aas.Submodel subModel, DigitalNameplateOptions options,
            string defaultLang = null)
        {
            // access 
            if (subModel == null || options == null)
                return null;

            // make result
            var res = new NameplateData();

            // shortcuts
            var defs = AasxPredefinedConcepts.DigitalNameplateV20.Static;
            var mm = MatchMode.Relaxed;

            // SME on top level
            res.URIOfTheProduct = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IProperty>(
                    defs.CD_URIOfTheProduct?.GetSingleKey(), mm)?
                .Value;

            res.ManufacturerName = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
                    defs.CD_ManufacturerName?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.ManufacturerProductDesignation = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
                    defs.CD_ManufacturerProductDesignation?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.ManufacturerProductRoot = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
                    defs.CD_ManufacturerProductRoot?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.ManufacturerProductFamily = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
                    defs.CD_ManufacturerProductFamily?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.ManufacturerProductType = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
                    defs.CD_ManufacturerProductType?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.OrderCodeOfManufacturer = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
                    defs.CD_OrderCodeOfManufacturer?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.ProductArticleNumberOfManufacturer = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
                    defs.CD_ProductArticleNumberOfManufacturer?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.SerialNumber = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IProperty>(defs.CD_SerialNumber?.GetSingleKey(), mm)?
                .Value;

            res.YearOfConstruction = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IProperty>(defs.CD_YearOfConstruction?.GetSingleKey(), mm)?
                .Value;

            res.DateOfManufacture = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IProperty>(defs.CD_DateOfManufacture?.GetSingleKey(), mm)?
                .Value;

            res.HardwareVersion = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
                    defs.CD_HardwareVersion?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.FirmwareVersion = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
                    defs.CD_FirmwareVersion?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.SoftwareVersion = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
                    defs.CD_SoftwareVersion?.GetSingleKey(), mm)?
                .Value?.GetDefaultString(defaultLang);

            res.CountryOfOrigin = "" + subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IProperty>(defs.CD_CountryOfOrigin?.GetSingleKey(), mm)?
                .Value;

            res.CompanyLogo = subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.IFile>(defs.CD_CompanyLogo?.GetSingleKey(), mm);

            // find contact info?
            var smcContInf = subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(
                    defs.CD_ContactInformation?.GetSingleKey(), mm);
            if (smcContInf?.Value != null)
            {
                res.ContactInformation = new List<string>();

                Action<List<Aas.ISubmodelElement>, string, Aas.IKey> tryAdd = (coll, header, key) =>
                {
                    var st = coll?
                        .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(key, mm)?
                        .Value?.GetDefaultString(defaultLang);
                    if (st?.HasContent() == true)
                        res.ContactInformation.Add(("" + header) + st);
                };

                tryAdd(smcContInf?.Value, null, defs.CD_ZipCodeOfPOBox?.GetSingleKey());
                tryAdd(smcContInf?.Value, null, defs.CD_POBox?.GetSingleKey());
                tryAdd(smcContInf?.Value, null, defs.CD_Street?.GetSingleKey());
                tryAdd(smcContInf?.Value, null, defs.CD_CityTown?.GetSingleKey());
                tryAdd(smcContInf?.Value, null, defs.CD_StateCounty?.GetSingleKey());
                tryAdd(smcContInf?.Value, null, defs.CD_NationalCode?.GetSingleKey());
                tryAdd(smcContInf?.Value, null, defs.CD_AddressOfAdditionalLink?.GetSingleKey());

                // Phone

                var smc2 = smcContInf?.Value?
                    .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Phone?.GetSingleKey(), mm);
                if (smc2 != null)
                    tryAdd(smc2.Value, null, defs.CD_TelephoneNumber?.GetSingleKey());

                // Fax

                smc2 = smcContInf?.Value?
                    .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Fax?.GetSingleKey(), mm);
                if (smc2 != null)
                    tryAdd(smc2.Value, null, defs.CD_FaxNumber?.GetSingleKey());

                // Email

                smc2 = smcContInf?.Value?
                    .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Email?.GetSingleKey(), mm);
                if (smc2 != null)
                    tryAdd(smc2.Value, null, defs.CD_EmailAddress?.GetSingleKey());
            }

            // find markings?
            var collMarkings = subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Markings?.GetSingleKey(), mm)?.Value;
            if (collMarkings == null)
                collMarkings = subModel.SubmodelElements
                .FindFirstSemanticIdAs<Aas.ISubmodelElementList>(defs.CD_Markings?.GetSingleKey(), mm)?.Value;
            if (collMarkings != null)
            {
                res.Markings = new List<MarkingInfo>();

                foreach (var smcMark in collMarkings
                    .FindAllSemanticIdAs<Aas.SubmodelElementCollection>(defs.CD_Marking?.GetSingleKey(), mm))
                {
                    var mi = new MarkingInfo();
                    res.Markings.Add(mi);

                    mi.Name = "" + smcMark?.Value
                        .FindFirstSemanticIdAs<Aas.IProperty>(defs.CD_MarkingName?.GetSingleKey(), mm)?
                        .Value;

                    mi.File = smcMark?.Value
                        .FindFirstSemanticIdAs<Aas.IFile>(defs.CD_MarkingFile?.GetSingleKey(), mm);

                    mi.AddText = new List<string>();
                    foreach (var mat in smcMark?.Value
                        .FindAllSemanticIdAs<Aas.IProperty>(defs.CD_MarkingAdditionalText?.GetSingleKey(), mm))
                    {
                        mi.AddText.Add("" + mat?.Value);
                    }
                }
            }

            // done
            return res;
        }
    }
}
