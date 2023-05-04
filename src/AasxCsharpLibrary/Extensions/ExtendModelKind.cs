using AdminShellNS;

namespace Extensions
{
    public static class ExtendModelKind
    {
        public static void Validate(this ModellingKind modelingKind, AasValidationRecordList results, IReferable container)
        {
            // access
            if (results == null || container == null)
                return;

            // check
            if (modelingKind != ModellingKind.Template && modelingKind != ModellingKind.Instance)
            {
                // violation case
                results.Add(new AasValidationRecord(
                    AasValidationSeverity.SchemaViolation, container,
                    $"ModelingKind: enumeration value neither Template nor Instance",
                    () =>
                    {
                        modelingKind = ModellingKind.Instance;
                    }));
            }
        }
    }
}
