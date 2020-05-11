using AdminShellNS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxIntegrationBase.AasForms
{
    public class AasFormUtils
    {
        static private void RecurseExportAsTemplate(AdminShell.SubmodelElementWrapperCollection smwc, FormDescListOfElement tels, 
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
                    if (smw.submodelElement is AdminShell.Property)
                    {
                        var p = (smw.submodelElement as AdminShell.Property);
                        tsme = new FormDescProperty("" + p.idShort, FormMultiplicity.One, p.semanticId?.GetAsExactlyOneKey(), "" + p.idShort, valueType: p.valueType /* , presetValue: p.value */);
                    }
                    if (smw.submodelElement is AdminShell.MultiLanguageProperty)
                    {
                        var mlp = (smw.submodelElement as AdminShell.MultiLanguageProperty);
                        tsme = new FormDescMultiLangProp("" + mlp.idShort, FormMultiplicity.One, mlp.semanticId?.GetAsExactlyOneKey(), "" + mlp.idShort);
                    }
                    if (smw.submodelElement is AdminShell.File)
                    {
                        var mlp = (smw.submodelElement as AdminShell.File);
                        tsme = new FormDescFile("" + mlp.idShort, FormMultiplicity.One, mlp.semanticId?.GetAsExactlyOneKey(), "" + mlp.idShort);
                    }
                    if (smw.submodelElement is AdminShell.SubmodelElementCollection)
                    {
                        var smec = (smw.submodelElement as AdminShell.SubmodelElementCollection);
                        tsme = new FormDescSubmodelElementCollection("" + smec.idShort, FormMultiplicity.One, smec.semanticId?.GetAsExactlyOneKey(), "" + smec.idShort);
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

                        q = qs?.FindType("Multiplicity");
                        if (q != null)
                        {
                            foreach (var m in (FormMultiplicity[]) Enum.GetValues(typeof(FormMultiplicity)))
                                if (("" + q.value) == Enum.GetName(typeof(FormMultiplicity), m))
                                    tsme.Multiplicity = m;
                        }

                        q = qs?.FindType("PresetValue");
                        if (q != null && tsme is FormDescProperty)
                            (tsme as FormDescProperty).presetValue = "" + q.value;

                        q = qs?.FindType("PresetMimeType");
                        if (q != null && tsme is FormDescFile)
                            (tsme as FormDescFile).presetMimeType = "" + q.value;

                        // adopt presetIdShort
                        if (tsme.Multiplicity == FormMultiplicity.ZeroToMany || tsme.Multiplicity == FormMultiplicity.OneToMany)
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
                        if (masterCd != null && masterCd.identification != null)
                        {
                            // already in cds?
                            var copyCd = cds.Find(masterCd.identification);
                            if (copyCd == null)
                            {
                                // add clone
                                cds.Add(new AdminShell.ConceptDescription(masterCd));
                            }
                        }
                    }

                    // recurse
                    if (smw.submodelElement is AdminShell.SubmodelElementCollection)
                        RecurseExportAsTemplate((smw.submodelElement as AdminShell.SubmodelElementCollection).value, (tsme as FormDescSubmodelElementCollection).value, env, cds);
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
                        sm.semanticId?.GetAsExactlyOneKey(),
                        sm.idShort,
                        "");
                    tsm.SubmodelElements = new FormDescListOfElement();
                    templateArr.Add(tsm);

                    // ok, export all SubmodelElems
                    RecurseExportAsTemplate(sm.submodelElements, tsm.SubmodelElements);
                }

            // write
            var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(new Type[] { typeof(FormDescBase) });
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
            var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(new Type[] { typeof(FormDescBase) });
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
            public List<ExportAsGenericFormsOptions_OptionsRecord> Records = new List<ExportAsGenericFormsOptions_OptionsRecord>();
        }

        public static void ExportAsGenericFormsOptions(AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm, string fn)
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
            var q = sm?.qualifiers?.FindType("FormTag");
            if (q != null)
                rec.FormTag = "" + q.value;

            rec.FormTitle = "TBD/" + sm.idShort;
            q = sm?.qualifiers?.FindType("FormTitle");
            if (q != null)
                rec.FormTitle = "" + q.value;

            rec.FormSubmodel = tsm;
            rec.ConceptDescriptions = cds;

            // fill out overall data
            var overall = new ExportAsGenericFormsOptions_OptionsOverall();
            overall.Records.Add(rec);

            // write
            var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(new Type[] { typeof(FormDescBase) });
            settings.Formatting = Newtonsoft.Json.Formatting.Indented;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(overall, overall.GetType(), settings);
            File.WriteAllText(fn, json);
        }

    }
}
