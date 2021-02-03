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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using AdminShellNS;
using Microsoft.VisualBasic.FileIO;

namespace AasxPackageExplorer
{
    public static class CSVTools
    {
        public static void ImportCSVtoSubModel(
            string inputFn, AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm, AdminShell.SubmodelRef smref)
        {
            AdminShell.SubmodelElementCollection[] propGroup = new AdminShell.SubmodelElementCollection[10];
            int i_propGroup = 0;

            var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(inputFn);
            parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
            parser.SetDelimiters(";");

            string[] rows = parser.ReadFields();
            if (rows == null)
            {
                throw new InvalidOperationException(
                    $"There were no fields read from the inputFn: {inputFn}");
            }

            if ((rows[0] != "typeName" ||
                rows[1] != "idShort" ||
                rows[2] != "value") ||
                (rows.Length > 3 &&
                (
                    rows[3] != "valueType" ||
                    rows[4] != "category" ||
                    rows[5] != "descriptionEN" ||
                    rows[6] != "descriptionDE"
                )))
            {
                return;
            }

            sm.idShort = inputFn.Split('\\').Last().Replace(".csv", "");

            while (!parser.EndOfData)
            {
                rows = parser.ReadFields();

                if (rows == null)
                {
                    throw new InvalidOperationException(
                        $"There were no fields read from inputFn: {inputFn}");
                }

                switch (rows[0])
                {
                    case "SubmodelElementCollection":
                        propGroup[i_propGroup] = AdminShell.SubmodelElementCollection.CreateNew(rows[1]);
                        if (i_propGroup == 0)
                        {
                            sm.Add(propGroup[0]);
                            if (rows.Length > 3)
                            {
                                if (rows[7] != "") propGroup[0].semanticId = new AdminShellV20.SemanticId(
                                     AdminShell.Reference.CreateNew(
                                         "ConceptDescription", false, "IRI", rows[7]));
                            }
                            propGroup[0].kind = AdminShellV20.ModelingKind.CreateAsInstance();
                        }
                        else
                        {
                            propGroup[i_propGroup - 1].Add(propGroup[i_propGroup]);
                        }
                        i_propGroup++;
                        break;
                    case "End-SubmodelElementCollection":
                        if (i_propGroup != 0)
                            i_propGroup--;
                        break;
                    case "Property":
                        var p = AdminShell.Property.CreateNew(rows[1].Replace("-", "_"));
                        p.value = rows[2];
                        if (rows.Length > 3)
                        {
                            p.valueType = rows[3];
                            p.category = rows[4];
                            if (rows[5] != "") p.AddDescription("en", rows[5]);
                            if (rows[6] != "") p.AddDescription("de", rows[6]);
                            p.kind = AdminShellV20.ModelingKind.CreateAsInstance();
                            if (rows[7] != "")
                                p.semanticId = new AdminShell.SemanticId(
                                    AdminShell.Reference.CreateNew(
                                        "ConceptDescription", false, "IRI", rows[7]));
                        }
                        if (i_propGroup == 0)
                        {
                            sm.Add(p);
                        }
                        else
                        {
                            propGroup[i_propGroup - 1].Add(p);
                        }
                        break;
                }
            }
        }
    }
}
