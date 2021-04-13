/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;

namespace AasxPluginSmdExporter
{
    public class EClass
    {
        public static Dictionary<string, EClass> InputEclassIds { get; set; }

        public static Dictionary<string, EClass> OutputEclassIds { get; set; }
        public static Dictionary<string, EClass> PortEclassIds { get; set; }

        public string Id { get; set; }

        public string Unit { get; set; }

        /// <summary>
        /// Initializes all necessary dictionaries and in case of success returns true.
        /// </summary>
        /// <returns></returns>
        public static bool InitEclass(Queue<string> log)
        {
            InputEclassIds = new Dictionary<string, EClass>();
            OutputEclassIds = new Dictionary<string, EClass>();
            PortEclassIds = new Dictionary<string, EClass>();

            foreach (var pair in IdTables.InputEclassUnits)
            {
                EClass eClassIn = new EClass();
                eClassIn.Id = pair.Key;
                eClassIn.Unit = pair.Value;

                InputEclassIds.Add(eClassIn.Id, eClassIn);

                if (!PortEclassIds.ContainsKey(eClassIn.Id))
                    PortEclassIds.Add(eClassIn.Id.Trim(), eClassIn);
            }

            foreach (var pair in IdTables.OutputEclassUnits)
            {
                EClass eClassOut = new EClass();
                eClassOut.Id = pair.Key;
                if (!eClassOut.Id.Equals("") && !eClassOut.Id.Equals("?"))
                {
                    eClassOut.Unit = pair.Value;
                    OutputEclassIds.Add(eClassOut.Id, eClassOut);

                    if (!PortEclassIds.ContainsKey(eClassOut.Id))
                        PortEclassIds.Add(eClassOut.Id.Trim(), eClassOut);
                }
            }
            return true;
        }

        /// <summary>
        /// Checks whether or not the two given strings are compatible eclass ids according to the csv data.
        /// </summary>
        /// <param name="inputId"></param>
        /// <param name="outputId"></param>
        /// <returns></returns>
        public static bool CheckEClassConnection(string inputId, string outputId)
        {

            inputId = inputId.Trim();
            outputId = outputId.Trim();

            if (inputId.Equals(outputId))
            {
                return true;
            }

            if (IdTables.CompatibleEclassIds.ContainsKey(inputId))
            {
                return IdTables.CompatibleEclassIds[inputId].Equals(outputId);
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// In case the given string is an eclass id and it is in the list the unit is returned
        /// </summary>
        /// <param name="eclassId"></param>
        /// <returns></returns>
        public static string GetUnitForEclassId(string eclassId)
        {
            eclassId = eclassId.Trim();
            if (PortEclassIds == null)
            {
                return "";
            }
            if (PortEclassIds.ContainsKey(eclassId))
            {
                return PortEclassIds[eclassId].Unit;
            }
            return "";
        }

    }
}
