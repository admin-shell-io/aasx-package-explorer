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
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxPackageLogic
{
    public static class CSVTools
    {
        public static void ImportCSVtoSubModel(
            string inputFn, AasCore.Aas3_0_RC02.Environment env, AasCore.Aas3_0_RC02.Submodel sm /* , AdminShell.SubmodelRef smref*/)
        {
            AasCore.Aas3_0_RC02.SubmodelElementCollection[] propGroup = new AasCore.Aas3_0_RC02.SubmodelElementCollection[10];
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

            sm.IdShort = inputFn.Split('\\').Last().Replace(".csv", "");

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
                    case "AasCore.Aas3_0_RC02.SubmodelElementCollection":
                        propGroup[i_propGroup] = new AasCore.Aas3_0_RC02.SubmodelElementCollection(idShort: rows[1]); ;
                        if (i_propGroup == 0)
                        {
                            sm.Add(propGroup[0]);
                            if (rows.Length > 3)
                            {
                                if (rows[7] != "") propGroup[0].SemanticId =
                                        ExtendReference.CreateFromKey(new AasCore.Aas3_0_RC02.Key(AasCore.Aas3_0_RC02.KeyTypes.GlobalReference, rows[7]));
                            }
                            propGroup[0].Kind = AasCore.Aas3_0_RC02.ModelingKind.Instance;
                        }
                        else
                        {
                            propGroup[i_propGroup - 1].Add(propGroup[i_propGroup]);
                        }
                        i_propGroup++;
                        break;
                    case "End-AasCore.Aas3_0_RC02.SubmodelElementCollection":
                        if (i_propGroup != 0)
                            i_propGroup--;
                        break;
                    case "AasCore.Aas3_0_RC02.Property":
                        var p = new AasCore.Aas3_0_RC02.Property(AasCore.Aas3_0_RC02.DataTypeDefXsd.String, idShort: rows[1].Replace("-", "_"));
                        p.Value = rows[2];
                        if (rows.Length > 3)
                        {
                            p.ValueType = AasCore.Aas3_0_RC02.Stringification.DataTypeDefXsdFromString(rows[3]) ?? AasCore.Aas3_0_RC02.DataTypeDefXsd.String;
                            p.Category = rows[4];
                            if (rows[5] != "") p.AddDescription("en", rows[5]);
                            if (rows[6] != "") p.AddDescription("de", rows[6]);
                            p.Kind = AasCore.Aas3_0_RC02.ModelingKind.Instance;
                            if (rows[7] != "")
                                p.SemanticId = ExtendReference.CreateFromKey(new AasCore.Aas3_0_RC02.Key(AasCore.Aas3_0_RC02.KeyTypes.GlobalReference, rows[7]));
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
