using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminShellNS
{
    public enum AasValidationSeverity {
        Hint, Warning, SpecViolation, SchemaViolation
    }

    public class AasValidationRecord
    {
        public AasValidationSeverity Severity = AasValidationSeverity.Hint;
        public AdminShell.Referable Source = null;
        public string Message = "";

        public Action Fix = null;

        public AasValidationRecord(AasValidationSeverity Severity, AdminShell.Referable Source, 
            string Message, Action Fix = null)
        {
            this.Severity = Severity;
            this.Source = Source;
            this.Message = Message;
            this.Fix = Fix;
        }

        public override string ToString()
        {
            return $"[{Severity.ToString()}] in {""+Source?.ToString()}: {"" + Message}";
        }
    }

    public class AasValidationRecordList : List<AasValidationRecord>
    {
    }

#if dcdscdsc
    public class AdminShellValidate
    {
        private static void ValidateReferable(AasValidationRecordList results, AdminShell.Referable rf)
        {
            // access
            if (results == null || rf == null)
                return;

            // check
            if (rf.idShort == null || rf.idShort.Trim() == "")
                results.Add(new AasValidationRecord(
                    AasValidationSeverity.SpecViolation, rf,
                    AasValidationFinding.ReferableNoIdShort,
                    "Referable: missing idShort",
                    () => {
                        rf.idShort = "TO_FIX";
                    }));
        }

        private static void ValidateCD(AasValidationRecordList results, AdminShell.ConceptDescription cd)
        {
            // access
            if (results == null || cd == null)
                return;

            // check CD itself
            ValidateReferable(results, cd);

            // check IEC61360 spec
            var ds = cd?.embeddedDataSpecification.dataSpecificationContent?.dataSpecificationIEC61360;
            if (ds != null)
            {
                if (ds.preferredName == null || ds.preferredName.langString == null
                    || ds.preferredName.langString.Count < 1)
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SchemaViolation, cd, 
                        AasValidationFinding.CdMissingPreferredName, 
                        "ConceptDescription: missing preferredName",
                        () => {
                            ds.preferredName = new AdminShell.LangStringSetIEC61360("EN?",
                                AdminShellUtil.EvalToNonEmptyString("{0}", cd.idShort, "UNKNOWN"));
                        }));

                if (ds.shortName != null && ( ds.shortName.langString == null
                    || ds.shortName.langString.Count < 1))
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SchemaViolation, cd,
                        AasValidationFinding.CdEmptyShortName,
                        "ConceptDescription: existing shortName with missing langString",
                        () => {
                            ds.shortName = null;
                        }));

                if (ds.definition != null && (ds.definition.langString == null
                    || ds.definition.langString.Count < 1))
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SchemaViolation, cd,
                        AasValidationFinding.CdEmptyShortName,
                        "ConceptDescription: existing definition with missing langString",
                        () => {
                            ds.definition = null;
                        }));
            }
        }

        
    }

#endif
}
