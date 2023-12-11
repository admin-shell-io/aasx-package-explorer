/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
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
