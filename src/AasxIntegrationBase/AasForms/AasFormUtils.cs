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
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxIntegrationBase.AasForms
{
    public static class AasFormUtils
    {
        private static void RecurseExportAsTemplate(
            List<AasCore.Aas3_0_RC02.ISubmodelElement> smwc, FormDescListOfElement tels,
            AasCore.Aas3_0_RC02.Environment env = null, List<AasCore.Aas3_0_RC02.ConceptDescription> cds = null)
        {
            // access
            if (smwc == null || tels == null)
                return;

            // over all elems
            foreach (var smw in smwc)
                if (smw != null && smw != null)
                {
                    FormDescSubmodelElement tsme = null;
                    if (smw is AasCore.Aas3_0_RC02.Property p)
                    {
                        tsme = new FormDescProperty(
                            "" + p.IdShort, FormMultiplicity.One, p.SemanticId?.GetAsExactlyOneKey(),
                            "" + p.IdShort, valueType: AasCore.Aas3_0_RC02.Stringification.ToString(p.ValueType));
                    }
                    if (smw is AasCore.Aas3_0_RC02.MultiLanguageProperty mlp)
                    {
                        tsme = new FormDescMultiLangProp(
                            "" + mlp.IdShort, FormMultiplicity.One, mlp.SemanticId?.GetAsExactlyOneKey(),
                            "" + mlp.IdShort);
                    }
                    if (smw is AasCore.Aas3_0_RC02.File fl)
                    {
                        tsme = new FormDescFile(
                            "" + fl.IdShort, FormMultiplicity.One, fl.SemanticId?.GetAsExactlyOneKey(),
                            "" + fl.IdShort);
                    }
                    if (smw is AasCore.Aas3_0_RC02.ReferenceElement rf)
                    {
                        tsme = new FormDescReferenceElement(
                            "" + rf.IdShort, FormMultiplicity.One, rf.SemanticId?.GetAsExactlyOneKey(),
                            "" + rf.IdShort);
                    }
                    if (smw is AasCore.Aas3_0_RC02.RelationshipElement re)
                    {
                        tsme = new FormDescRelationshipElement(
                            "" + re.IdShort, FormMultiplicity.One, re.SemanticId?.GetAsExactlyOneKey(),
                            "" + re.IdShort);
                    }
                    if (smw is AasCore.Aas3_0_RC02.Capability cap)
                    {
                        tsme = new FormDescCapability(
                            "" + cap.IdShort, FormMultiplicity.One, cap.SemanticId?.GetAsExactlyOneKey(),
                            "" + cap.IdShort);
                    }
                    if (smw is AasCore.Aas3_0_RC02.SubmodelElementCollection smec)
                    {
                        tsme = new FormDescSubmodelElementCollection(
                            "" + smec.IdShort, FormMultiplicity.One, smec.SemanticId?.GetAsExactlyOneKey(),
                            "" + smec.IdShort);
                    }

                    if (tsme != null)
                    {
                        // take over directly
                        tsme.PresetCategory = smw.Category;

                        // Qualifers
                        var qs = smw.Qualifiers;

                        var q = qs?.FindQualifierOfType("FormTitle");
                        if (q != null)
                            tsme.FormTitle = "" + q.Value;

                        q = qs?.FindQualifierOfType("FormInfo");
                        if (q != null)
                            tsme.FormInfo = "" + q.Value;

                        q = qs?.FindQualifierOfType("FormUrl");
                        if (q != null)
                            tsme.FormUrl = "" + q.Value;

                        q = qs?.FindQualifierOfType("EditIdShort");
                        if (q != null)
                            tsme.FormEditIdShort = q.Value.Trim().ToLower() == "true";

                        q = qs?.FindQualifierOfType("EditDescription");
                        if (q != null)
                            tsme.FormEditDescription = q.Value.Trim().ToLower() == "true";

                        q = qs?.FindQualifierOfType("Multiplicity");
                        if (q != null)
                        {
                            foreach (var m in (FormMultiplicity[])Enum.GetValues(typeof(FormMultiplicity)))
                                if (("" + q.Value) == Enum.GetName(typeof(FormMultiplicity), m))
                                    tsme.Multiplicity = m;
                        }

                        q = qs?.FindQualifierOfType("PresetValue");
                        if (q != null && tsme is FormDescProperty)
                            (tsme as FormDescProperty).presetValue = "" + q.Value;

                        q = qs?.FindQualifierOfType("PresetMimeType");
                        if (q != null && tsme is FormDescFile)
                            (tsme as FormDescFile).presetMimeType = "" + q.Value;

                        q = qs?.FindQualifierOfType("FormChoices");
                        if (q != null && q.Value.HasContent() && tsme is FormDescProperty fdprop)
                        {
                            var choices = q.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            if (choices != null && choices.Length > 0)
                                fdprop.comboBoxChoices = choices;
                        }

                        // adopt presetIdShort
                        if (tsme.Multiplicity == FormMultiplicity.ZeroToMany ||
                            tsme.Multiplicity == FormMultiplicity.OneToMany)
                            tsme.PresetIdShort += "{0:00}";

                        // Ignore this element
                        q = qs?.FindQualifierOfType("FormIgnore");
                        if (q != null)
                            continue;
                    }

                    if (tsme != null)
                        tels.Add(tsme);

                    // in any case, check for CD
                    if (env != null && cds != null && smw?.SemanticId?.Keys != null)
                    {
                        var masterCd = env.FindConceptDescriptionByReference(smw?.SemanticId);
                        if (masterCd != null && masterCd.Id != null)
                        {
                            // already in cds?
                            var copyCd = cds.Where(cd => cd.Id.Equals(masterCd.Id)).First();
                            if (copyCd == null)
                            {
                                // add clone
                                cds.Add(
                                    new AasCore.Aas3_0_RC02.ConceptDescription(masterCd.Id, masterCd.Extensions, masterCd.Category, 
                                        masterCd.IdShort, masterCd.DisplayName, masterCd.Description, 
                                        masterCd.Checksum, masterCd.Administration, 
                                        masterCd.EmbeddedDataSpecifications, masterCd.IsCaseOf));
                            }
                        }
                    }

                    // recurse
                    if (smw is AasCore.Aas3_0_RC02.SubmodelElementCollection)
                        RecurseExportAsTemplate(
                            (smw as AasCore.Aas3_0_RC02.SubmodelElementCollection).Value,
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
            foreach (var aas in package.AasEnv.AssetAdministrationShells)
                foreach (var smref in aas.Submodels)
                {
                    // get Submodel
                    var sm = package.AasEnv.FindSubmodel(smref);
                    if (sm == null)
                        continue;

                    // make submodel template
                    var tsm = new FormDescSubmodel(
                        "Submodel",
                        sm.SemanticId.GetAsExactlyOneKey(),
                        sm.IdShort,
                        "");
                    tsm.SubmodelElements = new FormDescListOfElement();
                    templateArr.Add(tsm);

                    // ok, export all SubmodelElems
                    RecurseExportAsTemplate(sm.SubmodelElements, tsm.SubmodelElements);
                }

            // write
            var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(new[] { typeof(FormDescBase) });
            settings.Formatting = Newtonsoft.Json.Formatting.Indented;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(templateArr, templateArr.GetType(), settings);
            System.IO.File.WriteAllText(fn, json);
        }

        public static void ExportAsTemplate(AasCore.Aas3_0_RC02.Submodel sm, string fn)
        {
            // access
            if (fn == null || sm == null || sm.SubmodelElements == null)
                return;

            // make submodel template
            var templateArr = new List<FormDescSubmodel>();
            var tsm = new FormDescSubmodel(
                "Submodel",
                sm.SemanticId?.GetAsExactlyOneKey(),
                sm.IdShort,
                "");
            tsm.SubmodelElements = new FormDescListOfElement();
            templateArr.Add(tsm);

            // ok, export all SubmodelElems
            RecurseExportAsTemplate(sm.SubmodelElements, tsm.SubmodelElements);

            // write
            var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(new[] { typeof(FormDescBase) });
            settings.Formatting = Newtonsoft.Json.Formatting.Indented;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(templateArr, templateArr.GetType(), settings);
            System.IO.File.WriteAllText(fn, json);
        }

        [DisplayName("Record", noTypeLookup: true)]
        private class ExportAsGenericFormsOptions_OptionsRecord
        {
            public string FormTag = "";
            public string FormTitle = "";
            public FormDescSubmodel FormSubmodel = null;
            public List<AasCore.Aas3_0_RC02.ConceptDescription> ConceptDescriptions = null;
        }

        [DisplayName("Options", noTypeLookup: true)]
        private class ExportAsGenericFormsOptions_OptionsOverall
        {
            public List<ExportAsGenericFormsOptions_OptionsRecord> Records =
                new List<ExportAsGenericFormsOptions_OptionsRecord>();
        }

        public static void ExportAsGenericFormsOptions(
            AasCore.Aas3_0_RC02.Environment env, AasCore.Aas3_0_RC02.Submodel sm, string fn)
        {
            // access
            if (fn == null || env == null || sm == null || sm.SubmodelElements == null)
                return;

            // make submodel template
            var tsm = new FormDescSubmodel(
                "Submodel",
                sm.SemanticId?.GetAsExactlyOneKey(),
                sm.IdShort,
                "");
            tsm.SubmodelElements = new FormDescListOfElement();

            // will collect a list of CDs
            var cds = new List<AasCore.Aas3_0_RC02.ConceptDescription>();

            // ok, export all SubmodelElems into tsm
            RecurseExportAsTemplate(sm.SubmodelElements, tsm.SubmodelElements, env, cds);

            // fill out record
            var rec = new ExportAsGenericFormsOptions_OptionsRecord();

            rec.FormTag = "TBD";
            var q = sm.Qualifiers?.FindQualifierOfType("FormTag");
            if (q != null)
                rec.FormTag = "" + q.Value;

            rec.FormTitle = "TBD/" + sm.IdShort;
            q = sm.Qualifiers?.FindQualifierOfType("FormTitle");
            if (q != null)
                rec.FormTitle = "" + q.Value;

            rec.FormSubmodel = tsm;
            rec.ConceptDescriptions = cds;

            // fill out overall data
            var overall = new ExportAsGenericFormsOptions_OptionsOverall();
            overall.Records.Add(rec);

            // write
            var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(new[] { typeof(FormDescBase) });
            settings.Formatting = Newtonsoft.Json.Formatting.Indented;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(overall, overall.GetType(), settings);
            System.IO.File.WriteAllText(fn, json);
        }

    }
}
