/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;

namespace AasxPluginExportTable
{
    public static class AasConvertHelper
    {
        public static void TakeOverSmeToSm(Aas.ISubmodelElement sme, Aas.ISubmodel sm)
        {
            // access
            if (sme == null || sm == null)
                return;

            // tedious, manual, nor elegant
            if (sme.Description != null)
                sm.Description = sme.Description.Copy();

            if (sme.IdShort.HasContent())
                sm.IdShort = sme.IdShort;

            if (sme.Category.HasContent())
                sm.Category = sme.Category;

            if (sme.SemanticId != null)
                sm.SemanticId = sme.SemanticId.Copy();

            if (sme.Qualifiers != null)
            {
                if (sm.Qualifiers == null)
                    sm.Qualifiers = new List<Aas.IQualifier>();
                sm.Qualifiers.AddRange(sme.Qualifiers.Copy());
            }
        }


    }
}
