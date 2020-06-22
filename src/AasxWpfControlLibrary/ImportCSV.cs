using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using AdminShellNS;

namespace AasxPackageExplorer
{
    public static class CSVTools
    {
        public static void ImportCSVtoSubModel(string inputFn, AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm, AdminShell.SubmodelRef smref)
        {
            AdminShell.SubmodelElementCollection[] propGroup = new AdminShell.SubmodelElementCollection[10];
            int i_propGroup = 0;

            var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(inputFn);
            parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
            parser.SetDelimiters(";");

            string[] rows = parser.ReadFields();
            if (rows == null || rows.Length < 3 ||
                rows[0] != "typeName" ||
                rows[1] != "idShort" ||
                rows[2] != "value")
            {
                return;
            }

            while (!parser.EndOfData)
            {
                rows = parser.ReadFields();
                if (rows == null || rows.Length < 1)
                    continue;

                switch (rows[0])
                {
                    case "SubmodelElementCollection":
                        propGroup[i_propGroup] = AdminShell.SubmodelElementCollection.CreateNew(rows[1]);
                        if (i_propGroup == 0)
                        {
                            sm.Add(propGroup[0]);
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
                        var p = AdminShell.Property.CreateNew(rows[1]);
                        p.valueType = "string";
                        p.value = rows[2];

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

