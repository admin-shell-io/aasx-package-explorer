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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AasxIntegrationBase;
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.ConceptModel;
using AdminShellNS;
using JetBrains.Annotations;

namespace AasxPluginTechnicalData
{
    public partial class TechDataHeaderControl : UserControl
    {
        public TechDataHeaderControl()
        {
            InitializeComponent();
        }

        public class ClassificationRecord
        {
            [JetBrains.Annotations.UsedImplicitly]
            public string System { get; set; }
            [JetBrains.Annotations.UsedImplicitly]
            public string Version { get; set; }
            [JetBrains.Annotations.UsedImplicitly]
            public string ClassTxt { get; set; }

            public ClassificationRecord() { }
            public ClassificationRecord(string system, string version, string classTxt)
            {
                this.System = system;
                this.Version = version;
                this.ClassTxt = classTxt;
            }
        }

        public class ProductImageRecord
        {
            [JetBrains.Annotations.UsedImplicitly]
            public BitmapImage ImgData { get; set; }

            public ProductImageRecord() { }
            public ProductImageRecord(BitmapImage imgData)
            {
                this.ImgData = imgData;
            }
        }

        public void SetContents(
            AdminShellPackageEnv package, ConceptModelZveiTechnicalData theDefs, string defaultLang,
            AdminShell.Submodel sm)
        {
            // access
            if (sm == null)
                return;

            // section General
            var smcGeneral = sm.submodelElements.FindFirstSemanticIdAs<AdminShell.SubmodelElementCollection>(
                theDefs.CD_GeneralInformation.GetSingleId());
            if (smcGeneral != null)
            {
                // Product info

                TextBoxProdDesig.Text =
                    "" +
                    smcGeneral.value.FindFirstSemanticId(
                        theDefs.CD_ManufacturerProductDesignation.GetSingleId(),
                        allowedTypes: AdminShell.SubmodelElement.PROP_MLP)?
                            .submodelElement?.ValueAsText(defaultLang);

                TextBoxProdCode.Text =
                    "" +
                    smcGeneral.value.FindFirstSemanticIdAs<AdminShell.Property>(
                        theDefs.CD_ManufacturerOrderCode.GetSingleId())?.value;
                TextBoxPartNumber.Text =
                    "" +
                    smcGeneral.value.FindFirstSemanticIdAs<AdminShell.Property>(
                        theDefs.CD_ManufacturerPartNumber.GetSingleId())?.value;

                // Manu data

                TextBoxManuName.Text =
                    "" +
                    smcGeneral.value.FindFirstSemanticIdAs<AdminShell.Property>(
                        theDefs.CD_ManufacturerName.GetSingleId())?.value;
                if (package != null)
                {
                    ImageManuLogo.Source = AasxWpfBaseUtils.LoadBitmapImageFromPackage(
                        package,
                        smcGeneral.value.FindFirstSemanticIdAs<AdminShell.File>(
                            theDefs.CD_ManufacturerLogo.GetSingleId())?.value
                        );
                }

                // Product Images

                var pil = new List<ProductImageRecord>();
                foreach (var pi in
                            smcGeneral.value.FindAllSemanticIdAs<AdminShell.File>(
                                theDefs.CD_ProductImage.GetSingleId()))
                {
                    var data = AasxWpfBaseUtils.LoadBitmapImageFromPackage(package, pi.value);
                    if (data != null)
                        pil.Add(new ProductImageRecord(data));
                }

                ItemsControlProductImages.ItemsSource = pil;
            }

            // also Section: Product Classifications
            var smcClassifications =
                sm.submodelElements.FindFirstSemanticIdAs<AdminShell.SubmodelElementCollection>(
                    theDefs.CD_ProductClassifications.GetSingleId());
            if (smcClassifications != null)
            {
                var clr = new List<ClassificationRecord>();
                foreach (var smc in
                        smcClassifications.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                            theDefs.CD_ProductClassificationItem.GetSingleId()))
                {
                    var sys = (
                        "" +
                        smc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            theDefs.CD_ProductClassificationSystem.GetSingleId())?.value).Trim();
                    var ver = (
                        "" +
                        smc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            theDefs.CD_ClassificationSystemVersion.GetSingleId())?.value).Trim();
                    var cls = (
                        "" +
                        smc.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            theDefs.CD_ProductClassId.GetSingleId())?.value).Trim();
                    if (sys != "" && cls != "")
                        clr.Add(new ClassificationRecord(sys, ver, cls));
                }

                ItemsControlClassifications.ItemsSource = clr;
            }
        }
    }
}
