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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AdminShellNS;

namespace AasxPluginExportTable
{
    public class AasConvertHelper
    {
        public static void TakeOverSmeToSm(AdminShell.SubmodelElement sme, AdminShell.Submodel sm)
        {
            // access
            if (sme == null || sm == null)
                return;

            // tedious, manual, nor elegant
            if (sme.description != null)
                sm.description = sme.description;

            if (sme.idShort.HasContent())
                sm.idShort = sme.idShort;

            if (sme.category.HasContent())
                sm.category = sme.category;

            if (sme.semanticId != null)
                sm.semanticId = sme.semanticId;

            if (sme.qualifiers != null)
            {
                if (sm.qualifiers == null)
                    sm.qualifiers = new AdminShell.QualifierCollection();
                sm.qualifiers.AddRange(sme.qualifiers);
            }
        }

        public static void TakeOverSmToSme(AdminShell.Submodel sm, AdminShell.Submodel sme)
        {
            // access
            if (sme == null || sm == null)
                return;

            // tedious, manual, nor elegant
            if (sm.description != null)
                sme.description = sm.description;

            if (sm.idShort.HasContent())
                sme.idShort = sm.idShort;

            if (sm.category.HasContent())
                sme.category = sm.category;

            if (sm.semanticId != null)
                sme.semanticId = sm.semanticId;

            if (sm.qualifiers != null)
            {
                if (sme.qualifiers == null)
                    sme.qualifiers = new AdminShell.QualifierCollection();
                sme.qualifiers.AddRange(sm.qualifiers);
            }
        }
    }
}
