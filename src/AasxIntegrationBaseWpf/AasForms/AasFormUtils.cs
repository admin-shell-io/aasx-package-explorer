/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using AdminShellNS;

namespace AasxIntegrationBase.AasForms
{
    public static class AasFormUtils
    {
        private static void RecurseExportAsTemplate(
            AdminShell.SubmodelElementWrapperCollection smwc, FormDescListOfElement tels,
            AdminShell.AdministrationShellEnv env = null, AdminShell.ListOfConceptDescriptions cds = null)
        {
            // access
            if (smwc == null || tels == null)
                return;

            // over all elems
            foreach (var smw in smwc)
                if (smw != null && smw.submodelElement != null)
                {
                    FormDescSubmodelElement tsme = null;
                    if (smw.submodelElement is AdminShell.Property p)
                    {
                        tsme = new FormDescProperty(
                            "" + p.idShort, FormMultiplicity.One, p.semanticId?.GetAsExactlyOneKey(),
                            "" + p.idShort, valueType: p.valueType);
                    }
                    if (smw.submodelElement is AdminShell.MultiLanguageProperty mlp)
                    {
                        tsme = new FormDescMultiLangProp(
                            "" + mlp.idShort, FormMultiplicity.One, mlp.semanticId?.GetAsExactlyOneKey(),
                            "" + mlp.idShort);
                    }
                    if (smw.submodelElement is AdminShell.File fl)
                    {
                        tsme = new FormDescFile(
                            "" + fl.idShort, FormMultiplicity.One, fl.semanticId?.GetAsExactlyOneKey(),
                            "" + fl.idShort);
                    }
                    if (smw.submodelElement is AdminShell.ReferenceElement rf)
                    {
                        tsme = new FormDescReferenceElement(
                            "" + rf.idShort, FormMultiplicity.One, rf.semanticId?.GetAsExactlyOneKey(),
                            "" + rf.idShort);
                    }
                    if (smw.submodelElement is AdminShell.SubmodelElementCollection smec)
                    {
                        tsme = new FormDescSubmodelElementCollection(
                            "" + smec.idShort, FormMultiplicity.One, smec.semanticId?.GetAsExactlyOneKey(),
                            "" + smec.idShort);
                    }

                    if (tsme != null)
                    {
                        // take over directly
                        tsme.PresetCategory = smw.submodelElement.category;

                        // Qualifers
                        var qs = smw.submodelElement.qualifiers;

                        var q = qs?.FindType("FormTitle");
                        if (q != null)
                            tsme.FormTitle = "" + q.value;

                        q = qs?.FindType("FormInfo");
                        if (q != null)
                            tsme.FormInfo = "" + q.value;

                        q = qs?.FindType("EditIdShort");
                        if (q != null)
                            tsme.FormEditIdShort = q.value.Trim().ToLower() == "true";

                        q = qs?.FindType("EditDescription");
                        if (q != null)
                            tsme.FormEditDescription = q.value.Trim().ToLower() == "true";

                        q = qs?.FindType("Multiplicity");
                        if (q != null)
                        {
                            foreach (var m in (FormMultiplicity[])Enum.GetValues(typeof(FormMultiplicity)))
                                if (("" + q.value) == Enum.GetName(typeof(FormMultiplicity), m))
                                    tsme.Multiplicity = m;
                        }

                        q = qs?.FindType("PresetValue");
                        if (q != null && tsme is FormDescProperty)
                            (tsme as FormDescProperty).presetValue = "" + q.value;

                        q = qs?.FindType("PresetMimeType");
                        if (q != null && tsme is FormDescFile)
                            (tsme as FormDescFile).presetMimeType = "" + q.value;

                        q = qs?.FindType("FormChoices");
                        if (q != null && q.value.HasContent() && tsme is FormDescProperty fdprop)
                        {
                            var choices = q.value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            if (choices != null && choices.Length > 0)
                                fdprop.comboBoxChoices = choices;
                        }

                        // adopt presetIdShort
                        if (tsme.Multiplicity == FormMultiplicity.ZeroToMany ||
                            tsme.Multiplicity == FormMultiplicity.OneToMany)
                            tsme.PresetIdShort += "{0:00}";

                        // Ignore this element
                        q = qs?.FindType("FormIgnore");
                        if (q != null)
                            continue;
                    }

                    if (tsme != null)
                        tels.Add(tsme);

                    // in any case, check for CD
                    if (env != null && cds != null && smw?.submodelElement?.semanticId?.Keys != null)
                    {
                        var masterCd = env.FindConceptDescription(smw?.submodelElement?.semanticId?.Keys);
                        if (masterCd != null && masterCd.id != null)
                        {
                            // already in cds?
                            var copyCd = cds.Find(masterCd.id);
                            if (copyCd == null)
                            {
                                // add clone
                                cds.Add(new AdminShell.ConceptDescription(masterCd));
                            }
                        }
                    }

                    // recurse
                    if (smw.submodelElement is AdminShell.SubmodelElementCollection)
                        RecurseExportAsTemplate(
                            (smw.submodelElement as AdminShell.SubmodelElementCollection).value,
                            (tsme as FormDescSubmodelElementCollection).value, env, cds);
                }
        }

        public static void ExportAsTemplate(AdminShellPackageEnv package, string fn)
        {
            // access
            if (fn == null || package == null || package.AasEnv == null)
                return;

            // build templates
            var templateArr = new List<FormDescSubmodel>();
            foreach (var aas in package.AasEnv.AdministrationShells)
                foreach (var smref in aas.submodelRefs)
                {
                    // get Submodel
                    var sm = package.AasEnv.FindSubmodel(smref);
                    if (sm == null)
                        continue;

                    // make submodel template
                    var tsm = new FormDescSubmodel(
                        "Submodel",
                        sm.GetSemanticKey(),
                        sm.idShort,
                        "");
                    tsm.SubmodelElements = new FormDescListOfElement();
                    templateArr.Add(tsm);

                    // ok, export all SubmodelElems
                    RecurseExportAsTemplate(sm.submodelElements, tsm.SubmodelElements);
                }

            // write
            var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(new[] { typeof(FormDescBase) });
            settings.Formatting = Newtonsoft.Json.Formatting.Indented;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(templateArr, templateArr.GetType(), settings);
            File.WriteAllText(fn, json);
        }

        public static void ExportAsTemplate(AdminShell.Submodel sm, string fn)
        {
            // access
            if (fn == null || sm == null || sm.submodelElements == null)
                return;

            // make submodel template
            var templateArr = new List<FormDescSubmodel>();
            var tsm = new FormDescSubmodel(
                "Submodel",
                sm.semanticId?.GetAsExactlyOneKey(),
                sm.idShort,
                "");
            tsm.SubmodelElements = new FormDescListOfElement();
            templateArr.Add(tsm);

            // ok, export all SubmodelElems
            RecurseExportAsTemplate(sm.submodelElements, tsm.SubmodelElements);

            // write
            var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(new[] { typeof(FormDescBase) });
            settings.Formatting = Newtonsoft.Json.Formatting.Indented;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(templateArr, templateArr.GetType(), settings);
            File.WriteAllText(fn, json);
        }

        [DisplayName("Record", noTypeLookup: true)]
        private class ExportAsGenericFormsOptions_OptionsRecord
        {
            public string FormTag = "";
            public string FormTitle = "";
            public FormDescSubmodel FormSubmodel = null;
            public AdminShell.ListOfConceptDescriptions ConceptDescriptions = null;
        }

        [DisplayName("Options", noTypeLookup: true)]
        private class ExportAsGenericFormsOptions_OptionsOverall
        {
            public List<ExportAsGenericFormsOptions_OptionsRecord> Records =
                new List<ExportAsGenericFormsOptions_OptionsRecord>();
        }

        public static void ExportAsGenericFormsOptions(
            AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm, string fn)
        {
            // access
            if (fn == null || env == null || sm == null || sm.submodelElements == null)
                return;

            // make submodel template
            var tsm = new FormDescSubmodel(
                "Submodel",
                sm.semanticId?.GetAsExactlyOneKey(),
                sm.idShort,
                "");
            tsm.SubmodelElements = new FormDescListOfElement();

            // will collect a list of CDs
            var cds = new AdminShell.ListOfConceptDescriptions();

            // ok, export all SubmodelElems into tsm
            RecurseExportAsTemplate(sm.submodelElements, tsm.SubmodelElements, env, cds);

            // fill out record
            var rec = new ExportAsGenericFormsOptions_OptionsRecord();

            rec.FormTag = "TBD";
            var q = sm.qualifiers?.FindType("FormTag");
            if (q != null)
                rec.FormTag = "" + q.value;

            rec.FormTitle = "TBD/" + sm.idShort;
            q = sm.qualifiers?.FindType("FormTitle");
            if (q != null)
                rec.FormTitle = "" + q.value;

            rec.FormSubmodel = tsm;
            rec.ConceptDescriptions = cds;

            // fill out overall data
            var overall = new ExportAsGenericFormsOptions_OptionsOverall();
            overall.Records.Add(rec);

            // write
            var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(new[] { typeof(FormDescBase) });
            settings.Formatting = Newtonsoft.Json.Formatting.Indented;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(overall, overall.GetType(), settings);
            File.WriteAllText(fn, json);
        }

    }
}
