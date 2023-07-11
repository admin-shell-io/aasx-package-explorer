/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using System;
using System.Linq;
using Aas = AasCore.Aas3_0;

namespace AasxPackageLogic
{
    public static class CSVTools
    {
        public static void ImportCSVtoSubModel(
            string inputFn, Aas.Environment env, Aas.ISubmodel sm /* , AdminShell.SubmodelRef smref*/)
        {
            Aas.SubmodelElementCollection[] propGroup = new Aas.SubmodelElementCollection[10];
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
                    case "SubmodelElementCollection":
                        propGroup[i_propGroup] = new Aas.SubmodelElementCollection(idShort: rows[1]); ;
                        if (i_propGroup == 0)
                        {
                            sm.Add(propGroup[0]);
                            if (rows.Length > 3)
                            {
                                if (rows[7] != "") propGroup[0].SemanticId =
                                        ExtendReference.CreateFromKey(new Aas.Key(Aas.KeyTypes.GlobalReference, rows[7]));
                            }
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
                        var p = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: rows[1].Replace("-", "_"));
                        p.Value = rows[2];
                        if (rows.Length > 3)
                        {
                            p.ValueType = Aas.Stringification.DataTypeDefXsdFromString(rows[3]) ?? Aas.DataTypeDefXsd.String;
                            p.Category = rows[4];
                            if (rows[5] != "") p.AddDescription("en", rows[5]);
                            if (rows[6] != "") p.AddDescription("de", rows[6]);
                            if (rows[7] != "")
                                p.SemanticId = ExtendReference.CreateFromKey(new Aas.Key(Aas.KeyTypes.GlobalReference, rows[7]));
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
