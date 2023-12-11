﻿using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace Extensions
{
    public static class ExtendModelKind
    {
        public static void Validate(this ModelingKind modelingKind, AasValidationRecordList results, IReferable container)
        {
            // access
            if (results == null || container == null)
                return;

            // check
            if (modelingKind != ModelingKind.Template && modelingKind != ModelingKind.Instance)
            {
                // violation case
                results.Add(new AasValidationRecord(
                    AasValidationSeverity.SchemaViolation, container,
                    $"ModelingKind: enumeration value neither Template nor Instance",
                    () =>
                    {
                        modelingKind = ModelingKind.Instance;
                    }));
            }
        }
    }
}
