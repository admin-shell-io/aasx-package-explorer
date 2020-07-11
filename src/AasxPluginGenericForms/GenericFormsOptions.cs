using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginGenericForms
{
    [DisplayName("Record")]
    public class GenericFormsOptionsRecord
    {
        /// <summary>
        /// Shall always contain 3-4 digit tag label, individual for each template
        /// </summary>
        public string FormTag = "";

        /// <summary>
        /// Shall always contain 3-4 digit tag title, individual for each template
        /// </summary>
        public string FormTitle = "";

        /// <summary>
        /// Full (recursive) description for Submodel to be generated
        /// </summary>
        public FormDescSubmodel FormSubmodel = null;

        /// <summary>
        /// A list with required concept descriptions, if appropriate.
        /// </summary>
        public AdminShell.ListOfConceptDescriptions ConceptDescriptions = null;
    }

    [DisplayName("Options")]
    public class GenericFormOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        //
        // Constants
        //

        //
        // Option fields
        //

        public List<GenericFormsOptionsRecord> Records = new List<GenericFormsOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static GenericFormOptions CreateDefault()
        {
            var opt = new GenericFormOptions();

            var rec = new GenericFormsOptionsRecord()
            {
                FormTag = "SMP",
                FormTitle = "Sample declaration of a GenericTemplate"
            };
            opt.Records.Add(rec);

            rec.FormSubmodel = new FormDescSubmodel(
                "Submodel Root",
                new AdminShell.Key("Submodel", false, "IRI", "www.exmaple.com/sms/1112"),
                "Example",
                "Information string");

            rec.FormSubmodel.Add(new FormDescProperty(
                formText: "Sample Property",
                multiplicity: FormMultiplicity.OneToMany,
                smeSemanticId: new AdminShell.Key("ConceptDescription", false, "IRI", "www.example.com/cds/1113"),
                presetIdShort: "SampleProp{0:0001}",
                valueType: "string",
                presetValue: "123"));

            return opt;
        }

        public GenericFormsOptionsRecord MatchRecordsForSemanticId(AdminShell.SemanticId sem)
        {
            // check for a record in options, that matches Submodel
            GenericFormsOptionsRecord res = null;
            if (Records != null)
                foreach (var rec in Records)
                    if (rec?.FormSubmodel?.KeySemanticId != null)
                        if (sem != null && sem.Matches(rec.FormSubmodel.KeySemanticId))
                        {
                            res = rec;
                            break;
                        }
            return res;
        }

        public override void Merge(AasxPluginOptionsBase options)
        {
            var mergeOptions = options as GenericFormOptions;
            if (mergeOptions == null || mergeOptions.Records == null)
                return;

            if (this.Records == null)
                this.Records = new List<GenericFormsOptionsRecord>();

            this.Records.AddRange(mergeOptions.Records);
        }

    }
}
